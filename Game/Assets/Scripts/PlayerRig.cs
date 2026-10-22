using UnityEngine;

// 玩家控制 + 跟随相机：对位移植 web 版 CampusScene 的移动/碰撞/朝向/精灵帧/相机跟随
// 验收参数（对应 web 版 URL 参数）：-px -pz 出生点 ｜ -at x,z 视角对准该点 ｜ -yaw/-pitch/-dist 视角 ｜ -freeze 1 冻结朝向
public class PlayerRig : MonoBehaviour
{
    const float MOVE_SPEED = 6f;
    float _bound = 60f;

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
    bool _firstPerson;          // V 键切换：第一人称（贴头）/ 第三人称（球坐标跟随）
    float _fpPitch;             // 第一人称俯仰（上正下负）
    float _groundY;             // 脚底高度（人行道/桥面）
    Renderer _spriteR;
    Mesh _spriteMesh;
    Vector3 _dragLast;
    string _simKey;
    int _simFrames = -1;

    public void Init(WorldBuilder w)
    {
        _w = w;
        _bound = w.W.BOUND > 1f ? w.W.BOUND : 60f;
        _time = GetComponent<TimeOfDay>();
        _cam = Camera.main;
        _player = w.Player;
        _sprite = w.PlayerSprite.transform;
        _spriteR = w.PlayerSprite;
        _spriteMat = w.PlayerSprite.material;   // 实例化，供帧动画改 UV
        var mf = w.PlayerSprite.GetComponent<MeshFilter>();
        _spriteMesh = mf != null ? mf.sharedMesh : null;

        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = MatLib.Hex("#10141c");
        _cam.fieldOfView = 46f;
        _cam.nearClipPlane = 0.5f;
        _cam.farClipPlane = 560f;   // 覆盖天空穹顶（半径 420 + 玩家 128）与远景山脊，否则走远后天空出现黑幕

        var spawn = new Vector2(w.Spawn.x, w.Spawn.z);
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
        var dist = Args.F("-dist"); if (dist.HasValue) _dist = Mathf.Clamp(dist.Value, 6f, 140f);   // 验收俯视可放远（交互滚轮仍限 6-22）
        if (dist.HasValue && dist.Value > 22f && !pitch.HasValue) _pitch = Mathf.Max(_pitch, 1.0f);  // 远机位默认俯视（显式 -pitch 时尊重调用方）
        if (Args.Str("-freeze") == "1") _alignPaused = true;
        if (Args.Str("-fp") == "1") _firstPerson = true;   // 验收：以第一人称启动

        // 验收：-sim d,40 → 前 40 帧模拟按住 D
        var sim = Args.Str("-sim");
        if (!string.IsNullOrEmpty(sim))
        {
            var sp = sim.Split(',');
            if (sp.Length == 2 && int.TryParse(sp[1], out var n)) { _simKey = sp[0].ToLowerInvariant(); _simFrames = n; }
        }

        _player.position = new Vector3(spawn.x, 0f, spawn.y);
        _facing = Mathf.PI;
        ApplyViewVisibility();
        SnapCamera();
    }

    public Vector3 Position => _player != null ? _player.position : Vector3.zero;
    public float Yaw => _yaw;   // 相机朝向（小地图上的自己用）

    // 圆形边界：世界是圆的（小地图也是圆的），方形四角会跑进边界山体里
    Vector2 ClampRound(float x, float z, float margin = 0f)
    {
        float lim = _bound - margin;
        float r = Mathf.Sqrt(x * x + z * z);
        if (r <= lim) return new Vector2(x, z);
        float k = lim / r;
        return new Vector2(x * k, z * k);
    }

    // 传送（小地图点击）：落在阻挡里时向四周找最近的可站立点
    public bool Teleport(float x, float z)
    {
        if (_player == null || _w == null) return false;
        var c0 = ClampRound(x, z, 1.5f);
        x = c0.x; z = c0.y;
        if (Collides(x, z))
        {
            bool ok = false;
            for (float r = 1.5f; r <= 12f && !ok; r += 1.5f)
                for (int k = 0; k < 16 && !ok; k++)
                {
                    float a = k / 16f * Mathf.PI * 2f;
                    float nx = x + Mathf.Cos(a) * r, nz = z + Mathf.Sin(a) * r;
                    if (nx * nx + nz * nz > (_bound - 1.5f) * (_bound - 1.5f)) continue;
                    if (!Collides(nx, nz)) { x = nx; z = nz; ok = true; }
                }
            if (!ok) return false;
        }
        _groundY = _w.GroundY(x, z);
        _player.position = new Vector3(x, _groundY, z);
        _camInit = false;
        SnapCamera();
        if (_time != null) _time.ShowToast("已传送（M 隐藏地图）");
        return true;
    }

    // 第一人称隐藏角色精灵（影子保留，低头可见脚下）
    void ApplyViewVisibility()
    {
        if (_spriteR != null) _spriteR.enabled = !_firstPerson;
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

        // 鼠标拖拽旋转 + 滚轮缩放（恢复原来的操作手感）
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
            _yaw += d.x * 0.005f;
            if (_firstPerson)
                _fpPitch = Mathf.Clamp(_fpPitch + d.y * 0.004f, -1.1f, 1.1f);
            else
                _pitch = Mathf.Clamp(_pitch - d.y * 0.004f, 0.3f, 1.2f);
        }
        float wheel = Input.mouseScrollDelta.y;
        if (!_firstPerson && Mathf.Abs(wheel) > 0.01f) _dist = Mathf.Clamp(_dist + wheel, 6f, 22f);

        // F11：窗口 / 全屏切换（窗口可拖动边缘调整尺寸）
        if (Input.GetKeyDown(KeyCode.F11))
        {
            if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow)
                Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
            else
                Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
        }

        // V：第一 / 第三人称切换
        if (Input.GetKeyDown(KeyCode.V))
        {
            _firstPerson = !_firstPerson;
            ApplyViewVisibility();
            if (_time != null) _time.ShowToast(_firstPerson ? "第一人称视角（V 切回第三人称）" : "第三人称视角（V 切换）");
        }

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
            var cand = ClampRound(p.x + dirX * MOVE_SPEED * dt, p.z + dirZ * MOVE_SPEED * dt);
            if (!Collides(cand.x, p.z)) p.x = cand.x;
            if (!Collides(p.x, cand.y)) p.z = cand.y;
        }
        // 脚底跟随地面（人行道 12cm / 桥面 / 车行道），叠加走路小跳
        float gy = _w != null ? _w.GroundY(p.x, p.z) : 0f;
        _groundY = Mathf.Lerp(_groundY, gy, Mathf.Min(1f, dt * 12f));
        p.y = _groundY + (moving ? Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.05f : 0f);
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
        // 行映射：精灵表 QuadZ 做过 U 轴镜像（修招牌文字），左右两行需交换才能朝向正确
        else row = d2 < 0f ? 2 : 3;                                 // 左侧面 / 右侧面
        int frame = moving ? 1 + (int)(Time.time * 8f) % 3 : 0;   // 0 站立 + 3 帧行走周期
        // 帧 UV 直接写进四边形顶点：4×4 精灵表，逐帧取格（含 1/4 像素内缩防跨格采样）
        if (_spriteMesh != null)
        {
            float u0 = frame / 4f, v0 = (3 - row) / 4f;
            const float ix = 0.25f / 416f, iy = 0.25f / 608f;   // 精灵表 416×608（4× 精度）
            float ua = u0 + ix, ub = u0 + 0.25f - ix, va = v0 + iy, vb = v0 + 0.25f - iy;
            _spriteMesh.uv = new[]
            {
                new Vector2(ub, va), new Vector2(ua, va), new Vector2(ua, vb), new Vector2(ub, vb),
            };
        }

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
        // 相机：第一人称贴头 / 第三人称球坐标跟随
        if (_firstPerson)
        {
            _cam.transform.position = new Vector3(p.x, p.y + 1.45f, p.z);
            float cp = Mathf.Cos(_fpPitch);
            var fwd = new Vector3(-Mathf.Sin(_yaw) * cp, Mathf.Sin(_fpPitch), -Mathf.Cos(_yaw) * cp);
            _cam.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            _camInit = false;   // 切回第三人称时重新吸附，避免从头部漫长插值
            return;
        }
        float cx = p.x + Mathf.Sin(_yaw) * Mathf.Cos(_pitch) * _dist;
        float cz = p.z + Mathf.Cos(_yaw) * Mathf.Cos(_pitch) * _dist;
        float cy = p.y + Mathf.Sin(_pitch) * _dist;
        var cam = _cam.transform;
        // 相机避障：从头部向目标机位射线，撞到建筑/摊位就把相机拉到障碍前
        Vector3 desired = new Vector3(cx, cy, cz);
        Vector3 head = new Vector3(p.x, p.y + 1.35f, p.z);
        Vector3 ray = desired - head;
        float rayLen = ray.magnitude;
        if (rayLen > 0.05f &&
            Physics.Raycast(head, ray / rayLen, out var hit, rayLen, ~(1 << 2), QueryTriggerInteraction.Ignore))
        {
            // 与障碍保持 2.5m：既不会钻进摊位棚下，也不会贴着墙
            float safe = Mathf.Max(1.2f, hit.distance - 2.5f);
            desired = head + ray / rayLen * Mathf.Min(safe, rayLen);
        }
        if (!_camInit) cam.position = desired;
        else
        {
            float t = Mathf.Min(1f, dt * 5f);
            cam.position = Vector3.Lerp(cam.position, desired, t);
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
        // 道具实体（长椅/垃圾桶/公告栏/车架/旗杆/花坛/喷泉/球场附件/校门）
        foreach (var b in _w.W.BLOCKERS)
            if (Mathf.Abs(x - b.x) < b.hw + 0.35f && Mathf.Abs(z - b.z) < b.hd + 0.35f) return true;
        return false;
    }
}
