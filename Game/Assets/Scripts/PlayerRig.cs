using UnityEngine;

// 玩家控制 + 跟随相机：对位移植 web 版 CampusScene 的移动/碰撞/朝向/精灵帧/相机跟随
// 验收参数（对应 web 版 URL 参数）：-px -pz 出生点 ｜ -at x,z 视角对准该点 ｜ -yaw/-pitch/-dist 视角 ｜ -freeze 1 冻结朝向
public class PlayerRig : MonoBehaviour
{
    const float MOVE_SPEED = 6f;
    const float BOUND = 30f;

    WorldBuilder _w;
    TimeOfDay _time;
    Camera _cam;
    Transform _player, _sprite;
    Material _spriteMat;

    float _yaw;
    float _pitch = 0.38f;
    float _dist = 13f;
    float _yawTarget = float.NaN;
    float _facing = Mathf.PI;   // 世界角：0=+z 南，π=北；默认背对镜头
    bool _alignPaused, _camInit, _dragging;
    Vector3 _dragLast;
    string _simKey;
    int _simFrames = -1;

    public void Init(WorldBuilder w)
    {
        _w = w;
        _time = GetComponent<TimeOfDay>();
        _cam = Camera.main;
        _player = w.Player;
        _sprite = w.PlayerSprite.transform;
        _spriteMat = w.PlayerSprite.material;   // 实例化，供帧动画改 UV

        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = MatLib.Hex("#10141c");
        _cam.fieldOfView = 46f;
        _cam.nearClipPlane = 0.5f;
        _cam.farClipPlane = 260f;

        var spawn = new Vector2(0f, 4f);
        var px = Args.F("-px"); if (px.HasValue) spawn.x = px.Value;
        var pz = Args.F("-pz"); if (pz.HasValue) spawn.y = pz.Value;

        var at = Args.Str("-at");
        if (!string.IsNullOrEmpty(at))
        {
            var parts = at.Split(',');
            if (parts.Length == 2
                && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ax)
                && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var az))
                _yaw = Mathf.Atan2(spawn.x - ax, spawn.y - az);
        }
        var yaw = Args.F("-yaw"); if (yaw.HasValue) _yaw = yaw.Value;
        var pitch = Args.F("-pitch"); if (pitch.HasValue) _pitch = Mathf.Clamp(pitch.Value, 0.3f, 1.35f);
        var dist = Args.F("-dist"); if (dist.HasValue) _dist = Mathf.Clamp(dist.Value, 6f, 40f);
        if (Args.Str("-freeze") == "1") _alignPaused = true;

        // 验收：-sim d,40 → 前 40 帧模拟按住 D
        var sim = Args.Str("-sim");
        if (!string.IsNullOrEmpty(sim))
        {
            var sp = sim.Split(',');
            if (sp.Length == 2 && int.TryParse(sp[1], out var n)) { _simKey = sp[0].ToLowerInvariant(); _simFrames = n; }
        }

        _player.position = new Vector3(spawn.x, 0f, spawn.y);
        _facing = Mathf.PI;
        SnapCamera();
    }

    void SnapCamera()
    {
        if (_cam == null || _player == null) return;
        var p = _player.position;
        _cam.transform.position = new Vector3(
            p.x + Mathf.Sin(_yaw) * Mathf.Cos(_pitch) * _dist,
            p.y + Mathf.Sin(_pitch) * _dist,
            p.z + Mathf.Cos(_yaw) * Mathf.Cos(_pitch) * _dist);
        _cam.transform.LookAt(new Vector3(p.x, 1.1f, p.z));
        _camInit = true;
    }

    void Update()
    {
        if (_cam == null || _player == null) return;
        float dt = Time.deltaTime;
        var p = _player.position;

        // Q / E：90° 步进平滑旋转（旋转中保持"背对镜头"）
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _yawTarget = (float.IsNaN(_yawTarget) ? _yaw : _yawTarget) + Mathf.PI / 2f;
            _alignPaused = false;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            _yawTarget = (float.IsNaN(_yawTarget) ? _yaw : _yawTarget) - Mathf.PI / 2f;
            _alignPaused = false;
        }
        if (!float.IsNaN(_yawTarget))
        {
            _yaw += (_yawTarget - _yaw) * Mathf.Min(1f, dt * 7f);
            if (Mathf.Abs(_yawTarget - _yaw) < 0.01f) { _yaw = _yawTarget; _yawTarget = float.NaN; }
        }

        // 鼠标拖拽旋转 + 滚轮缩放
        if (Input.GetMouseButtonDown(0))
        {
            _dragging = true;
            _alignPaused = true;
            _dragLast = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(0)) _dragging = false;
        if (_dragging)
        {
            var d = Input.mousePosition - _dragLast;
            _dragLast = Input.mousePosition;
            _yaw -= d.x * 0.005f;
            _pitch = Mathf.Clamp(_pitch + d.y * 0.004f, 0.3f, 1.2f);
        }
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f) _dist = Mathf.Clamp(_dist + wheel, 6f, 22f);

        // 移动（WASD/方向键，相对镜头方向；X/Z 轴分开判定 → 沿墙滑动）
        // ⚠ Unity 是左手系：给定镜头朝向，屏幕右方 = 世界 -x（three.js 右手系为 +x），横移分量必须取反
        // 验收参数 -sim <键>,<帧数>：前 N 帧模拟按住某键（如 -sim d,40），用于移动方向自动验证
        bool simOn = _simFrames > 0 && Time.frameCount <= _simFrames;
        bool holdW = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || (simOn && _simKey == "w");
        bool holdS = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || (simOn && _simKey == "s");
        bool holdA = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || (simOn && _simKey == "a");
        bool holdD = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || (simOn && _simKey == "d");
        float f = (holdW ? 1f : 0f) - (holdS ? 1f : 0f);
        float r = (holdD ? 1f : 0f) - (holdA ? 1f : 0f);
        bool moving = f != 0f || r != 0f;
        float dirX = 0f, dirZ = 0f;
        if (moving)
        {
            dirX = -Mathf.Sin(_yaw) * f - Mathf.Cos(_yaw) * r;
            dirZ = -Mathf.Cos(_yaw) * f + Mathf.Sin(_yaw) * r;
            float len = Mathf.Sqrt(dirX * dirX + dirZ * dirZ);
            dirX /= len;
            dirZ /= len;
            float nx = Mathf.Clamp(p.x + dirX * MOVE_SPEED * dt, -BOUND, BOUND);
            if (!Collides(nx, p.z)) p.x = nx;
            float nz = Mathf.Clamp(p.z + dirZ * MOVE_SPEED * dt, -BOUND, BOUND);
            if (!Collides(p.x, nz)) p.z = nz;
            p.y = Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.05f;   // 走路小跳
        }
        else
        {
            p.y *= 0.8f;
        }
        _player.position = p;

        // 精灵朝向与帧（背对镜头=站立姿态；移动朝移动方向；拖拽时冻结）
        var camPos = _cam.transform.position;
        float toCam = Mathf.Atan2(camPos.x - p.x, camPos.z - p.z);
        if (moving)
        {
            _alignPaused = false;
            float turn = Mathf.Atan2(dirX, dirZ) - _facing;
            turn = Mathf.Atan2(Mathf.Sin(turn), Mathf.Cos(turn));
            _facing += turn * Mathf.Min(1f, dt * 10f);
        }
        else if (!_alignPaused)
        {
            float align = toCam + Mathf.PI - _facing;
            align = Mathf.Atan2(Mathf.Sin(align), Mathf.Cos(align));
            _facing += align * Mathf.Min(1f, dt * 4f);
        }
        _sprite.rotation = Quaternion.Euler(0f, toCam * Mathf.Rad2Deg, 0f);
        float d2 = _facing - (toCam + Mathf.PI);
        d2 = Mathf.Atan2(Mathf.Sin(d2), Mathf.Cos(d2));
        int row;
        if (Mathf.Abs(d2) < Mathf.PI / 4f) row = 1;                 // 背面朝镜头
        else if (Mathf.Abs(d2) > 3f * Mathf.PI / 4f) row = 0;       // 正面
        else row = d2 < 0f ? 3 : 2;                                 // 右侧面 / 左侧面
        int frame = moving ? 1 + (int)(Time.time * 8f) % 2 : 0;
        _spriteMat.mainTextureOffset = new Vector2(frame / 3f, (3 - row) / 4f);

        // 可交互地点检测（建筑正门 / 操场 / 池塘 / 家）
        string near = null;
        foreach (var s in _w.W.SPOTS)
        {
            float dx = p.x - s.x, dz = p.z - s.z;
            if (Mathf.Sqrt(dx * dx + dz * dz) < s.r) { near = s.name; break; }
        }
        if (_time != null)
        {
            _time.Near = near;
            if (Input.GetKeyDown(KeyCode.F) && !string.IsNullOrEmpty(near))
                _time.ShowToast($"「{near}」内部将在后续迭代开放");
        }

        // 跟随相机（球坐标；首帧已在 Init 吸附）
        float cx = p.x + Mathf.Sin(_yaw) * Mathf.Cos(_pitch) * _dist;
        float cz = p.z + Mathf.Cos(_yaw) * Mathf.Cos(_pitch) * _dist;
        float cy = p.y + Mathf.Sin(_pitch) * _dist;
        var cam = _cam.transform;
        if (!_camInit) cam.position = new Vector3(cx, cy, cz);
        else
        {
            float t = Mathf.Min(1f, dt * 5f);
            cam.position = Vector3.Lerp(cam.position, new Vector3(cx, cy, cz), t);
        }
        _camInit = true;
        cam.LookAt(new Vector3(p.x, 1.1f, p.z));
    }

    // 碰撞：建筑 AABB + 树干 + 栅栏 + 路灯 + 主角家（与 web 版逐条一致；边界 ±30 在移动处 clamp）
    bool Collides(float x, float z)
    {
        foreach (var b in _w.W.BUILDINGS)
            if (Mathf.Abs(x - b.pos[0]) < b.size[0] / 2f + 0.45f && Mathf.Abs(z - b.pos[1]) < b.size[2] / 2f + 0.45f) return true;
        foreach (var t in _w.W.TREES)
            if (Mathf.Abs(x - t.x) < 0.75f && Mathf.Abs(z - t.z) < 0.75f) return true;
        foreach (var fc in _w.W.FENCES)
        {
            if (fc.axis == "z")
            {
                if (Mathf.Abs(x - fc.at) < 0.3f && z > fc.from - 0.2f && z < fc.to + 0.2f) return true;
            }
            else if (Mathf.Abs(z - fc.at) < 0.3f && x > fc.from - 0.2f && x < fc.to + 0.2f) return true;
        }
        foreach (var l in _w.W.LAMPS)
            if (Mathf.Abs(x - l.x) < 0.4f && Mathf.Abs(z - l.z) < 0.4f) return true;
        var v = _w.W.VILLA;
        if (Mathf.Abs(x - v.x) < v.w / 2f + 0.45f && Mathf.Abs(z - v.z) < v.d / 2f + 0.45f) return true;
        return false;
    }
}
