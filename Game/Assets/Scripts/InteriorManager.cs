using System.Collections.Generic;
using System.Collections;
using UnityEngine;

// 室内进出与运行期查询：进入后把 PlayerRig 的碰撞 / 地面高度 / 边界切到当前房间，
// 同时关掉小地图与天气粒子；出来时还原。房间本身由 InteriorBuilder 一次性建好并隐藏。
public class InteriorManager : MonoBehaviour
{
    public bool Indoor => _cur != null;
    public bool IsTransitioning { get; private set; }
    public Interior Current => _cur;
    public bool NearExit => _cur != null && _rig != null && _cur.NearExit(_rig.Position.x, _rig.Position.z);

    List<Interior> _rooms;
    Interior _cur;
    Vector3 _backPos;
    PlayerRig _rig;
    TimeOfDay _tod;
    Weather _wth;
    Minimap _mm;
    WorldBuilder _world;
    float _fade, _backYaw, _backFacing;

    public void Init(WorldBuilder w, PlayerRig rig, TimeOfDay tod, Weather wth, Minimap mm)
    {
        _rig = rig; _tod = tod; _wth = wth; _mm = mm; _world = w;
        _rooms = InteriorBuilder.Build(w.Root);
        foreach (var r in _rooms) r.Root.SetActive(false);

        // 验收参数：-indoor home|school 直接开在室内；-px/-pz 指定落点，-floor 1 指定二层
        var id = Args.Str("-indoor");
        bool defaultHome=string.IsNullOrEmpty(id)&&!Args.Has("-campus")&&!Args.Has("-px")&&!Args.Has("-pz")&&!Args.Has("-scenario")&&!Args.Has("-portalcheck")&&!Args.Has("-progresscheck")&&!Args.Has("-progressreload");
        if(defaultHome)id="home";
        if (string.IsNullOrEmpty(id))
        {
            if (Args.Has("-portalcheck")) StartCoroutine(CheckPortals());
            return;
        }
        var room = Find(id);
        if (room == null) { Debug.LogError("[Interior] 没有这个室内: " + id); return; }
        EnterRoom(room, true);
        if(defaultHome)_rig.SetIndoorView(true,new Vector2(room.Spawn.x,room.Spawn.y+1.2f),room.FloorY[0],Mathf.PI/2,0);
        var px = Args.F("-px"); var pz = Args.F("-pz");
        if (px.HasValue && pz.HasValue)
        {
            bool up = Args.Str("-floor") == "1";
            float fy = room.FloorY.Length > 1 && Args.Str("-floor") == "1" ? room.FloorY[1] : room.FloorY[0];
            _rig.SetIndoorView(true, new Vector2(px.Value, pz.Value), fy);
        }
    }

    public Interior Find(string id) => _rooms == null ? null : _rooms.Find(r => r.Id == id);

    // 交互点名字 → 室内 id；没有内部的建筑返回 null
    public static string ToId(string spotName) => spotName switch
    {
        "家" => "home",
        "教学楼" => "school",
        "图书馆" => "library",
        "餐厅" or "餐馆" => "restaurant",
        "超市" => "supermarket",
        "食堂" => "canteen",
        "宿舍楼" => "dorm",
        "礼堂" => "auditorium",
        "实验楼" => "labs",
        "奶茶店" => "milktea",
        "照相馆" => "photo",
        "书店" => "book",
        "便利店" => "conv",
        _ => null,
    };

    public bool CanEnter(string spotName) => ToId(spotName) != null && Find(ToId(spotName)) != null;

    public bool ExteriorDoor(string spotName, out Vector2 door, out Vector2 normal)
    {
        door = Vector2.zero; normal = Vector2.up;
        if (_world == null || _world.W == null) return false;
        if (spotName == "家")
        {
            var home = _world.W.VILLA;
            if (home == null) return false;
            normal = new Vector2(0f, home.face);
            door = new Vector2(home.x, home.z) + normal * home.d / 2f;
            return true;
        }
        foreach (var building in _world.W.BUILDINGS)
            if (building.name == spotName)
            {
                door = building.Door; normal = building.FrontNormal; return true;
            }
        return false;
    }

    public bool NearEntrance(string spotName, Vector3 position)
    {
        if (!ExteriorDoor(spotName, out var door, out var normal)) return false;
        var delta = new Vector2(position.x, position.z) - door;
        float front = Vector2.Dot(delta, normal);
        float side = Mathf.Abs(delta.x * normal.y - delta.y * normal.x);
        return front >= 0.4f && front <= 3.2f && side <= 1.9f;
    }

    public bool Enter(string spotName)
    {
        var id = ToId(spotName);
        if (id == null || Indoor || IsTransitioning || !NearEntrance(spotName, _rig.Position)) return false;
        var room = Find(id);
        if (room == null) return false;
        StartCoroutine(Transition(room));
        return true;
    }

    void EnterRoom(Interior room, bool fromArgs = false)
    {
        _backPos = _rig.Position;
        _backYaw = _rig.Yaw;
        if (ExteriorDoor(room.Name, out var door, out var normal))
        {
            var desired = door + normal * 1.55f;
            if (_rig.TryStandingPoint(desired, out var safe)) _backPos = new Vector3(safe.x, 0f, safe.y);
            else Debug.LogError("[Portal] 门外没有安全落点：" + room.Name);
            _backFacing = Mathf.Atan2(normal.x, normal.y);
        }
        else _backYaw = _rig.Yaw;
        _cur = room;
        room.Root.SetActive(true);
        Vector2 inward = (room.Spawn - room.Exit).normalized;
        var entry = room.Spawn;
        // 向门内走一步再落地，给第三人称镜头留下空间；家具/窄房间保留原安全点。
        for (float advance = 1.6f; advance >= 0.4f; advance -= 0.2f)
        {
            var candidate = room.Spawn + inward * advance;
            if (candidate.x != room.ClampX(candidate.x) || candidate.y != room.ClampZ(candidate.y)
                || room.Collides(candidate.x, candidate.y, room.FloorY[0])) continue;
            entry = candidate; break;
        }
        float yaw = fromArgs && (Args.Has("-at") || Args.Has("-yaw")) ? float.NaN : Mathf.Atan2(-inward.x, -inward.y);
        _rig.SetIndoorView(true, entry, room.FloorY[0], yaw);
        if (_mm != null) _mm.Indoor = true;   // 室内也可看地图（传送禁用），不再整体关掉
        if (_wth != null) _wth.Indoor = true;
        if (_tod != null) _tod.Indoor = true;
        if (_tod != null)
        {
            _tod.Near = null;
            _tod.NearLabel = "离开「" + room.Name + "」";
            _tod.ShowToast("进入「" + room.Name + "」");
        }
    }

    public void Exit()
    {
        if (_cur == null || IsTransitioning) return;
        StartCoroutine(Transition(null));
    }

    void ExitRoom()
    {
        if (_cur == null) return;
        _cur.Root.SetActive(false);
        var back = _backPos;
        var name = _cur.Name;
        _cur = null;
        _rig.SetIndoorView(false, new Vector2(back.x, back.z), 0f, _backYaw, _backFacing);
        if (_mm != null) _mm.Indoor = false;
        if (_wth != null) _wth.Indoor = false;
        if (_tod != null) _tod.Indoor = false;
        if (_tod != null)
        {
            _tod.NearLabel = null;
            _tod.ShowToast("离开「" + name + "」");
        }
    }

    IEnumerator Transition(Interior target)
    {
        IsTransitioning = true;
        for (float time = 0f; time < 0.16f; time += Time.unscaledDeltaTime)
        { _fade = Mathf.Clamp01(time / 0.16f); yield return null; }
        _fade = 1f;
        if (target == null) ExitRoom(); else EnterRoom(target);
        yield return null;
        for (float time = 0f; time < 0.20f; time += Time.unscaledDeltaTime)
        { _fade = 1f - Mathf.Clamp01(time / 0.20f); yield return null; }
        _fade = 0f; IsTransitioning = false;
    }

    void OnGUI()
    {
        if (_fade <= 0f) return;
        int oldDepth = GUI.depth; var oldColor = GUI.color;
        GUI.depth = -500; GUI.color = new Color(0.035f, 0.045f, 0.055f, _fade);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = oldColor; GUI.depth = oldDepth;
    }

    IEnumerator CheckPortals()
    {
        yield return null;
        var original = _rig.Position; int passed = 0, failed = 0;
        foreach (var spot in _world.W.SPOTS)
        {
            if (!CanEnter(spot.name)) continue;
            var room = Find(ToId(spot.name));
            if (!NearEntrance(spot.name, new Vector3(spot.x, 0f, spot.z)))
            { Debug.LogError("[PortalCheck] FAIL 交互点与实际门错位 " + spot.name); failed++; continue; }
            if (!ExteriorDoor(spot.name, out var door, out var normal)
                || !_rig.TryStandingPoint(door + normal * 1.55f, out var safe))
            { Debug.LogError("[PortalCheck] FAIL 门外落点 " + spot.name); failed++; continue; }
            _rig.SetIndoorView(false, safe, 0f);
            if (!NearEntrance(spot.name, _rig.Position))
            { Debug.LogError("[PortalCheck] FAIL 门前交互 " + spot.name); failed++; continue; }
            float outdoorYaw = _rig.Yaw;
            if (!Enter(spot.name)) { Debug.LogError("[PortalCheck] FAIL 进入请求 " + spot.name); failed++; continue; }
            float deadline = Time.realtimeSinceStartup + 3f;
            while (IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            if (IsTransitioning || _cur != room) { Debug.LogError("[PortalCheck] FAIL 进入过渡 " + spot.name); yield break; }
            var inside = _rig.Position;
            if(Args.Has("-lightingcheck"))RoomLightRig.Check(room);
            bool ok = !room.Collides(inside.x, inside.z, inside.y);
            if (Camera.main != null)
            {
                var cameraPoint = Camera.main.transform.position;
                ok &= !Physics.CheckSphere(cameraPoint, 0.12f, ~(1 << 2), QueryTriggerInteraction.Ignore)
                    && cameraPoint.y < room.CeilingY
                    && cameraPoint.x >= room.X1 && cameraPoint.x <= room.X2
                    && cameraPoint.z >= room.Z1 && cameraPoint.z <= room.Z2;
            }
            var inward = (room.Spawn - room.Exit).normalized;
            ok &= Mathf.Abs(Mathf.DeltaAngle(_rig.Yaw * Mathf.Rad2Deg,
                Mathf.Atan2(-inward.x, -inward.y) * Mathf.Rad2Deg)) < 0.1f;
            Exit();
            deadline = Time.realtimeSinceStartup + 3f;
            while (IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            if (IsTransitioning || Indoor) { Debug.LogError("[PortalCheck] FAIL 离开过渡 " + spot.name); yield break; }
            var outside = _rig.Position;
            if(Args.Has("-lightingcheck"))RoomLightRig.CheckExterior();
            ok &= Mathf.Abs(Mathf.DeltaAngle(_rig.Yaw * Mathf.Rad2Deg, outdoorYaw * Mathf.Rad2Deg)) < 0.1f;
            ok &= _rig.TryStandingPoint(new Vector2(outside.x, outside.z), out var checkedPoint)
                && Vector2.Distance(checkedPoint, new Vector2(outside.x, outside.z)) < 0.01f;
            if (ok) { passed++; Debug.Log("[PortalCheck] PASS " + room.Id + " 门内外落点、朝向、过渡与碰撞"); }
            else { failed++; Debug.LogError("[PortalCheck] FAIL " + room.Id); }
            yield return null;
        }
        _rig.SetIndoorView(false, new Vector2(original.x, original.z), 0f);
        Debug.Log($"[PortalCheck] 完成：通过 {passed}，失败 {failed}");
    }

    public float FloorAt(float x, float z, float y) => _cur == null ? 0f : _cur.FloorAt(x, z, y);
    public bool Collides(float x, float z, float y) => _cur != null && _cur.Collides(x, z, y);

    void Update()
    {
        if (_cur == null || _tod == null || _rig == null) return;
        _tod.NearLabel = NearExit
            ? "离开「" + _cur.Name + "」"
            : "「" + _cur.Name + "」";
    }
}
