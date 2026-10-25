using System.Collections.Generic;
using UnityEngine;

// 室内进出与运行期查询：进入后把 PlayerRig 的碰撞 / 地面高度 / 边界切到当前房间，
// 同时关掉小地图与天气粒子；出来时还原。房间本身由 InteriorBuilder 一次性建好并隐藏。
public class InteriorManager : MonoBehaviour
{
    public bool Indoor => _cur != null;
    public Interior Current => _cur;
    public bool NearExit => _cur != null && _rig != null && _cur.NearExit(_rig.Position.x, _rig.Position.z);

    List<Interior> _rooms;
    Interior _cur;
    Vector3 _backPos;
    PlayerRig _rig;
    TimeOfDay _tod;
    Weather _wth;
    Minimap _mm;

    public void Init(WorldBuilder w, PlayerRig rig, TimeOfDay tod, Weather wth, Minimap mm)
    {
        _rig = rig; _tod = tod; _wth = wth; _mm = mm;
        _rooms = InteriorBuilder.Build(w.Root);
        foreach (var r in _rooms) r.Root.SetActive(false);

        // 验收参数：-indoor home|school 直接开在室内；-px/-pz 指定落点，-floor 1 指定二层
        var id = Args.Str("-indoor");
        if (string.IsNullOrEmpty(id)) return;
        var room = Find(id);
        if (room == null) { Debug.LogError("[Interior] 没有这个室内: " + id); return; }
        EnterRoom(room);
        var px = Args.F("-px"); var pz = Args.F("-pz");
        if (px.HasValue && pz.HasValue)
        {
            bool up = Args.Str("-floor") == "1";
            float fy = up ? room.FloorY[1] : room.FloorAt(px.Value, pz.Value, room.FloorY[0]);
            _rig.SetIndoorView(true, new Vector2(px.Value, pz.Value), fy);
        }
    }

    public Interior Find(string id) => _rooms == null ? null : _rooms.Find(r => r.Id == id);

    // 交互点名字 → 室内 id；没有内部的建筑返回 null
    static string ToId(string spotName)
        => spotName == "家" ? "home" : spotName == "教学楼" ? "school" : null;

    public bool CanEnter(string spotName) => ToId(spotName) != null && Find(ToId(spotName)) != null;

    public bool Enter(string spotName)
    {
        var id = ToId(spotName);
        if (id == null) return false;
        var room = Find(id);
        if (room == null) return false;
        EnterRoom(room);
        return true;
    }

    void EnterRoom(Interior room)
    {
        _backPos = _rig.Position;
        _cur = room;
        room.Root.SetActive(true);
        _rig.SetIndoorView(true, room.Spawn, room.FloorY[0]);
        if (_mm != null) _mm.Indoor = true;   // 室内也可看地图（传送禁用），不再整体关掉
        if (_wth != null) _wth.Indoor = true;
        if (_tod != null) _tod.Indoor = true;
        if (_tod != null)
        {
            _tod.Near = null;
            _tod.NearLabel = "按 F 离开「" + room.Name + "」";
            _tod.ShowToast("进入「" + room.Name + "」");
        }
    }

    public void Exit()
    {
        if (_cur == null) return;
        _cur.Root.SetActive(false);
        var back = _backPos;
        var name = _cur.Name;
        _cur = null;
        _rig.SetIndoorView(false, new Vector2(back.x, back.z), 0f);
        if (_mm != null) _mm.Indoor = false;
        if (_wth != null) _wth.Indoor = false;
        if (_tod != null) _tod.Indoor = false;
        if (_tod != null)
        {
            _tod.NearLabel = null;
            _tod.ShowToast("离开「" + name + "」");
        }
    }

    public float FloorAt(float x, float z, float y) => _cur == null ? 0f : _cur.FloorAt(x, z, y);
    public bool Collides(float x, float z, float y) => _cur != null && _cur.Collides(x, z, y);

    void Update()
    {
        if (_cur == null || _tod == null || _rig == null) return;
        _tod.NearLabel = NearExit
            ? "按 F 离开「" + _cur.Name + "」"
            : "「" + _cur.Name + "」　WASD 走动 · 滚轮缩放";
    }
}
