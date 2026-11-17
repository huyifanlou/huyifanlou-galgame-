using UnityEngine;

// 玩家控制 + 跟随相机：对位移植 web 版 CampusScene 的移动/碰撞/朝向/精灵帧/相机跟随
// 验收参数（对应 web 版 URL 参数）：-px -pz 出生点 ｜ -at x,z 视角对准该点 ｜ -yaw/-pitch/-dist 视角 ｜ -freeze 1 冻结朝向
public class PlayerRig : MonoBehaviour
{
    const float MOVE_SPEED = 6f;
    const float RUN_SPEED = 9.5f;
    const float FP_LOOK_SENS = 1.4f;   // 第一人称鼠标灵敏度（度 / 轴单位），嫌快调小
    float _bound = 60f;

    WorldBuilder _w;
    TimeOfDay _time;
    Camera _cam;
    Transform _player;
    PlayerAvatar _avatar;

    float _yaw;
    float _pitch = 0.38f;
    float _dist = 13f;
    float _distMin = 6f;        // 室内收到 3.2，避免镜头穿墙
    bool _distForced, _pitchForced;   // 命令行显式给了 -dist / -pitch（截图验收），进出室内不覆盖
    InteriorManager _im;        // 室内（家/教学楼）进出与碰撞
    Minimap _mm;                // 小地图（第一人称开图时让位鼠标）
    public CharacterPicker Picker;   // C 键人物自选菜单（WorldBuilder 绑定）
    InteriorManager Im => _im;
    bool _indoor;
    float _outdoorDist = 13f, _outdoorPitch = 0.38f;
    float _yawTarget = float.NaN;
    float _facing = Mathf.PI;   // 世界角：0=+z 南，π=北；默认背对镜头
    bool _alignPaused, _camInit, _dragging;
    bool _firstPerson=true;
    bool _cursorReleased;
    public bool FirstPerson=>true;          // V 键切换：第一人称（贴头）/ 第三人称（球坐标跟随）
    float _fpPitch;             // 第一人称俯仰（上正下负）
    float _lastMapClose;        // M 按下时刻，开图瞬间不抢回鼠标锁定
    float _groundY;             // 脚底高度（人行道/桥面）
    Vector3 _dragLast;
    string _simKey;
    int _simFrames = -1;
    bool _forceRun;

    public void Init(WorldBuilder w)
    {
        _w = w;
        _bound = w.W.BOUND > 1f ? w.W.BOUND : 60f;
        _time = GetComponent<TimeOfDay>();
        _cam = Camera.main;
        _player = w.Player;
        _avatar = w.Player != null ? w.Player.GetComponentInChildren<PlayerAvatar>() : null;
        if (Args.Has("-cycle") && _avatar != null) _avatar.Cycle();   // 验收钩子：启动即切一次人物

        // TimeOfDay 已启用程序化天空时保留 Skybox；否则退回原来的纯色背景
        _cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        _cam.backgroundColor = MatLib.Hex("#10141c");
        _cam.fieldOfView = 62f;
        _cam.nearClipPlane = 0.25f;
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
        var dist = Args.F("-dist"); if (dist.HasValue) _dist = Mathf.Clamp(dist.Value, 2.2f, 140f);   // 验收可拉近拍脸；交互滚轮仍限 6-22
        if (dist.HasValue && dist.Value > 22f && !pitch.HasValue) _pitch = Mathf.Max(_pitch, 1.0f);  // 远机位默认俯视（显式 -pitch 时尊重调用方）
        _distForced = dist.HasValue; _pitchForced = pitch.HasValue;
        if (Args.Str("-freeze") == "1") _alignPaused = true;
        if (true) // Fixed first-person mode, command-line flags cannot restore third person.
        {
            _firstPerson = true;                            // 验收：以第一人称启动
            var fpp = Args.F("-pitch");                     // 第一人称俯仰（负=往下看，室内看家具用）
            if (fpp.HasValue) _fpPitch = Mathf.Clamp(fpp.Value, -1.1f, 1.1f);
        }

        _forceRun = Args.Str("-run") == "1";   // 验收：-sim 时强制跑步动画

        // 验收：-sim d,40 → 前 40 帧模拟按住 D
        var sim = Args.Str("-sim");
        if (!string.IsNullOrEmpty(sim))
        {
            var sp = sim.Split(',');
            if (sp.Length == 2 && int.TryParse(sp[1], out var n)) { _simKey = sp[0].ToLowerInvariant(); _simFrames = n; }
        }

        if(_w.W.RIVER!=null&&ClosedAreas.Contains(spawn.x,spawn.y)){spawn=ClosedAreas.SafeGate;Debug.Log("[Closure] 启动落点移至施工区外");}
        _player.position = new Vector3(spawn.x, 0f, spawn.y);
        var faceArg = Args.F("-facing");   // 验收：指定朝向（度，0=+z 南），配合 -freeze 拍正面受光
        _facing = faceArg.HasValue ? faceArg.Value * Mathf.Deg2Rad : Mathf.PI;
        ApplyViewVisibility();
        SnapCamera();
    }

    public void SaveView(ProgressSave data)
    {data.yaw=_yaw;data.facing=_facing;data.distance=_dist;data.pitch=_pitch;data.fpPitch=_fpPitch;data.firstPerson=true;}
    public void RestoreView(ProgressSave data)
    {_yaw=data.yaw;_facing=data.facing;_dist=data.distance;_pitch=data.pitch;_fpPitch=data.fpPitch;
     _firstPerson=true;_yawTarget=float.NaN;ApplyViewVisibility();SnapCamera();}
    public void LookAtWorld(Vector3 point)
    {var delta=point-(Position+Vector3.up*1.45f);_yaw=Mathf.Atan2(-delta.x,-delta.z);_fpPitch=Mathf.Clamp(Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude),-1.1f,1.1f);_yawTarget=float.NaN;_firstPerson=true;SnapCamera();}
    public Vector3 Position => _player != null ? _player.position : Vector3.zero;
    public float Yaw => _yaw;   // 相机朝向（小地图上的自己用）

    public void BindInterior(InteriorManager im) { _im = im; }
    public void BindMinimap(Minimap mm) { _mm = mm; }

    // 进出室内：换机位距离 + 落点 + 脚底高度（室内是楼层函数，不是地形）
    public void SetIndoorView(bool on, Vector2 at, float floorY, float yaw = float.NaN, float facing = float.NaN)
    {
        if (on && !_indoor) { _outdoorDist = _dist; _outdoorPitch = _pitch; }
        _indoor = on;
        _yawTarget = float.NaN;
        _dragging = false;
        if (!float.IsNaN(yaw)) { _yaw = yaw; _facing = float.IsNaN(facing) ? yaw + Mathf.PI : facing; _fpPitch = 0f; }
        _cam.nearClipPlane = on ? 0.12f : 0.25f;
        if (on)
        {
            _distMin = 3.2f;
            if (!_distForced) _dist = 5.2f;
            if (!_pitchForced) _pitch = 0.26f;
            _groundY = floorY;
            _player.position = new Vector3(at.x, floorY, at.y);
            _alignPaused = !float.IsNaN(yaw);
        }
        else
        {
            _distMin = 6f;
            _dist = _outdoorDist;
            _pitch = _outdoorPitch;
            _alignPaused = !float.IsNaN(yaw);
            _groundY = _w != null ? _w.GroundY(at.x, at.y) : 0f;
            _player.position = new Vector3(at.x, _groundY, at.y);
        }
        _camInit = false;
        Physics.SyncTransforms();
        SnapCamera();
    }

    // Require a clear walking segment so a display cannot appear behind a wall.
    public bool CanPlacePortrait(Vector3 point)
    {
        if(!_indoor&&new Vector2(point.x,point.z).magnitude>_bound-1)return false;
        if(_indoor&&_im?.Current!=null)
        {var room=_im.Current;if(point.x<room.X1+.4f||point.x>room.X2-.4f||point.z<room.Z1+.4f||point.z>room.Z2-.4f)return false;}
        Vector3 start=Position;int steps=Mathf.CeilToInt(Vector2.Distance(new Vector2(start.x,start.z),new Vector2(point.x,point.z))/.18f);
        for(int i=1;i<=steps;i++){var sample=Vector3.Lerp(start,point,i/(float)steps);if(Collides(sample.x,sample.z))return false;}
        return true;
    }
    public bool TryStandingPoint(Vector2 desired, out Vector2 safe)
    {
        safe = ClampRound(desired.x, desired.y, 1.5f);
        if (!Collides(safe.x, safe.y)) return true;
        for (float radius = 0.35f; radius <= 5f; radius += 0.35f)
            for (int i = 0; i < 24; i++)
            {
                float angle = i * Mathf.PI / 12f;
                var point = ClampRound(desired.x + Mathf.Cos(angle) * radius, desired.y + Mathf.Sin(angle) * radius, 1.5f);
                if (Collides(point.x, point.y)) continue;
                safe = point; return true;
            }
        return false;
    }

    // 圆形边界：世界是圆的（小地图也是圆的），方形四角会跑进边界山体里
    Vector2 ClampRound(float x, float z, float margin = 0f)
    {
        float lim = _bound - margin;
        float r = Mathf.Sqrt(x * x + z * z);
        if (r <= lim) return new Vector2(x, z);
        float k = lim / r;
        return new Vector2(x * k, z * k);
    }

    // 室内按房间矩形夹取，室外按圆形边界
    Vector2 NextPoint(float x, float z)
    {
        if (_indoor && _im != null && _im.Current != null)
            return new Vector2(_im.Current.ClampX(x), _im.Current.ClampZ(z));
        return ClampRound(x, z);
    }

    // 传送（小地图点击）：落在阻挡里时向四周找最近的可站立点
    public bool Teleport(float x, float z)
    {
        if (_player == null || _w == null) return false;
        if(_w.W.RIVER!=null&&ClosedAreas.Contains(x,z,.4f)){_time?.ShowToast("游乐场设施维护，暂不开放。");return false;}
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
        if (_time != null) _time.ShowToast("已到达目的地");
        return true;
    }

    // 第一人称下打开 UI 时临时解锁鼠标（小地图调用）；关图后回锁
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _lastMapClose = float.NegativeInfinity;   // 开图期间不抢回锁定
    }

    // 第一人称隐藏角色模型（影子保留，低头可见脚下）
    void ApplyViewVisibility()
    {
        if (_avatar != null) _avatar.SetVisible(!_firstPerson);
    }

    Vector3 CameraAnchor(Vector3 p) => p + Vector3.up * (_indoor ? 1.5f : 1.1f);

    // 普通跟随和进出门吸附共用同一避障解算，过渡淡入期间也不会穿墙。
    Vector3 CameraTarget(Vector3 p)
    {
        // 室内自适应俯仰：走廊只有 1.6m 宽，机位一贴墙就满屏墙纸。
        // 先探一下当前俯仰下能退多远，太近就抬高机位改成俯视
        float pitchUse = _pitch;
        Vector3 head = new Vector3(p.x, p.y + (_indoor ? 1.5f : 1.1f), p.z);
        if (_indoor || !_distForced)
        {
            var probe = new Vector3(Mathf.Sin(_yaw) * Mathf.Cos(_pitch), Mathf.Sin(_pitch),
                                    Mathf.Cos(_yaw) * Mathf.Cos(_pitch)) * _dist;
            if (Physics.SphereCast(head, 0.18f, probe.normalized, out var ph, probe.magnitude, ~(1 << 2), QueryTriggerInteraction.Ignore)
                && ph.distance < (_indoor ? 2.8f : 5.5f))
                pitchUse = Mathf.Min(1.15f, _pitch + (_indoor ? 0.45f : 0.38f));
        }
        float cx = p.x + Mathf.Sin(_yaw) * Mathf.Cos(pitchUse) * _dist;
        float cz = p.z + Mathf.Cos(_yaw) * Mathf.Cos(pitchUse) * _dist;
        float cy = head.y + Mathf.Sin(pitchUse) * _dist;
        // 相机避障：从头部向目标机位射线，撞到建筑/摊位就把相机拉到障碍前
        Vector3 desired = new Vector3(cx, cy, cz);
        Vector3 ray = desired - head;
        float rayLen = ray.magnitude;
        if (rayLen > 0.05f &&
            Physics.SphereCast(head, 0.18f, ray / rayLen, out var hit, rayLen, ~(1 << 2), QueryTriggerInteraction.Ignore))
        {
            // 与障碍保持 2.5m：既不会钻进摊位棚下，也不会贴着墙
            // 室内墙多，留 2.5m 会把镜头怼到角色脸上，收到 0.35m
            float margin = _indoor ? 0.35f : 2.5f;
            float safe = Mathf.Max(_indoor ? 0.45f : 1.2f, hit.distance - margin);
            // 关键：绝不能超过障碍本身，否则镜头穿到墙外面（室内墙很近时会踩到）
            safe = Mathf.Min(safe, Mathf.Max(0.25f, hit.distance - 0.1f));
            desired = head + ray / rayLen * Mathf.Min(safe, rayLen);
        }
        return desired;
    }

    void FirstPersonCamera(Vector3 p)
    {
        _cam.transform.position = p + Vector3.up * 1.45f;
        float cp = Mathf.Cos(_fpPitch);
        _cam.transform.rotation = Quaternion.LookRotation(
            new Vector3(-Mathf.Sin(_yaw) * cp, Mathf.Sin(_fpPitch), -Mathf.Cos(_yaw) * cp), Vector3.up);
        _camInit = false;
    }

    void SnapCamera()
    {
        if (_cam == null || _player == null) return;
        var p = _player.position;
        _firstPerson=true;
        if (_firstPerson) { FirstPersonCamera(p); return; }
        _cam.transform.position = CameraTarget(p);
        _cam.transform.LookAt(CameraAnchor(p));
        _camInit = true;
    }

    void Update()
    {
        if (_cam == null || _player == null || IntroScreen.Live) return;
        if ((_im != null && _im.IsTransitioning)||ProgressManager.MenuOpen||ProgressManager.TrackingOpen||ProgressManager.Loading||HelpSidebar.IsOpen){_dragging=false;return;}

        // C：打开人物自选菜单（六人卡片，点选/数字键切换）。
        // 放在对话锁之前——剧情播放中也允许换人；DialogueManager 会在菜单打开时冻结推进。
        if (GameInput.Down("Characters"))
        {
            if (Picker != null) Picker.Toggle();
            else if (_avatar != null)
            {
                _avatar.Cycle();
                if (_time != null) _time.ShowToast("人物：" + _avatar.Label);
            }
            return;
        }

        if (DialogueManager.Active || CharacterPicker.IsOpen || ProgressManager.MenuOpen || ProgressManager.Loading) return;   // 对话/选人菜单期间锁移动与相机
        float dt = Time.deltaTime;
        var p = _player.position;

        // Q / E：90° 步进平滑旋转（旋转中保持"背对镜头"）
        if (GameInput.Down("CameraLeft"))
        {
            _yawTarget = (float.IsNaN(_yawTarget) ? _yaw : _yawTarget) + Mathf.PI / 2f;
            _alignPaused = false;
        }
        if (GameInput.Down("CameraRight") && !(GetComponent<ProgressManager>()?.CanInteract ?? false))
        {
            _yawTarget = (float.IsNaN(_yawTarget) ? _yaw : _yawTarget) - Mathf.PI / 2f;
            _alignPaused = false;
        }
        if (!float.IsNaN(_yawTarget))
        {
            _yaw += (_yawTarget - _yaw) * Mathf.Min(1f, dt * 7f);
            if (Mathf.Abs(_yawTarget - _yaw) < 0.01f) { _yaw = _yawTarget; _yawTarget = float.NaN; }
        }

        // ── 视角操作 ──
        // 第一人称：锁鼠标，直接按鼠标位移转视角（FPS 手感，灵敏度低）
        // 第三人称：按住左键拖拽转视角（原来的手感）
        bool mapOpen = _mm != null && _mm.enabled && _mm.LargeOpen;   // 大地图打开时鼠标交给地图
        if (_firstPerson)
        {
            if (!mapOpen && !_cursorReleased && Time.unscaledTime - _lastMapClose > 0.4f)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            _dragging = false;
            if (_cursorReleased&&Input.GetMouseButtonDown(0)&&!GameUI.OverHud)_cursorReleased=false;
            if (!mapOpen&&Cursor.lockState==CursorLockMode.Locked)
            {
                float mx = Input.GetAxis("Mouse X") * FP_LOOK_SENS;   // 度
                float my = Input.GetAxis("Mouse Y") * FP_LOOK_SENS;
                if (Mathf.Abs(mx) > 0.0001f || Mathf.Abs(my) > 0.0001f)
                {
                    _yaw += mx * Mathf.Deg2Rad;
                    _fpPitch = Mathf.Clamp(_fpPitch + my * Mathf.Deg2Rad, -1.1f, 1.1f);   // Mouse Y 向上为正 → 加
                    _alignPaused = false;
                }
            }
            // Escape belongs to the settings/menu router; opening it releases the cursor.
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (mapOpen)
            {
                _dragging = false;
            }
            else
            {
                if (Input.GetMouseButtonDown(0)&&!GameUI.OverHud)
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
                    _pitch = Mathf.Clamp(_pitch - d.y * 0.004f, 0.3f, 1.2f);
                }
                float wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.01f) _dist = Mathf.Clamp(_dist + wheel, _distMin, 22f);
            }
        }

        // F11：窗口 / 全屏切换（窗口可拖动边缘调整尺寸）
        if (GameInput.Down("Fullscreen"))
        {
            if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow)
                Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
            else
                Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
        }

        if (GameInput.Up("Map")) _lastMapClose = Time.unscaledTime;

        // C：打开人物自选菜单（六人卡片，点选/数字键切换）；无菜单组件时退回盲循环
        if (GameInput.Down("Characters"))
        {
            if (Picker != null) Picker.Toggle();
            else if (_avatar != null)
            {
                _avatar.Cycle();
                if (_time != null) _time.ShowToast("人物：" + _avatar.Label);
            }
        }

        // 移动（WASD/方向键，相对镜头方向；X/Z 轴分开判定 → 沿墙滑动）
        // ⚠ Unity 是左手系：给定镜头朝向，屏幕右方 = 世界 -x（three.js 右手系为 +x），横移分量必须取反
        // 验收参数 -sim <键>,<帧数>：前 N 帧模拟按住某键（如 -sim d,40），用于移动方向自动验证
        bool simOn = _simFrames > 0 && Time.frameCount <= _simFrames;
        bool holdW = GameInput.Held("Forward") || (simOn && _simKey == "w");
        bool holdS = GameInput.Held("Back") || (simOn && _simKey == "s");
        bool holdA = GameInput.Held("Left") || (simOn && _simKey == "a");
        bool holdD = GameInput.Held("Right") || (simOn && _simKey == "d");
        float f = (holdW ? 1f : 0f) - (holdS ? 1f : 0f) + SubmissionTour.Motion.y;
        float r = (holdD ? 1f : 0f) - (holdA ? 1f : 0f) + SubmissionTour.Motion.x;
        bool moving = f != 0f || r != 0f;
        bool running = moving && (GameInput.Held("Run") || _forceRun);
        float dirX = 0f, dirZ = 0f;
        if (moving)
        {
            dirX = -Mathf.Sin(_yaw) * f - Mathf.Cos(_yaw) * r;
            dirZ = -Mathf.Cos(_yaw) * f + Mathf.Sin(_yaw) * r;
            float len = Mathf.Sqrt(dirX * dirX + dirZ * dirZ);
            dirX /= len;
            dirZ /= len;
            float speed = running ? RUN_SPEED : MOVE_SPEED;
            var cand = NextPoint(p.x + dirX * speed * dt, p.z + dirZ * speed * dt);
            if (!Collides(cand.x, p.z)) p.x = cand.x;
            if (!Collides(p.x, cand.y)) p.z = cand.y;
        }
        // 脚底跟随地面（人行道 12cm / 桥面 / 车行道），叠加走路小跳；室内走楼层函数
        float gy = _indoor && _im != null ? _im.FloorAt(p.x, p.z, p.y)
                 : (_w != null ? _w.GroundY(p.x, p.z) : 0f);
        _groundY = Mathf.Lerp(_groundY, gy, Mathf.Min(1f, dt * 12f));
        float stepFreq = running ? 13.5f : 10f;
        float stepAmp = running ? 0.075f : 0.05f;
        p.y = _groundY + (moving ? Mathf.Abs(Mathf.Sin(Time.time * stepFreq)) * stepAmp : 0f);
        _player.position = p;
        // 验收：-sim 模拟行走时每 30 帧打一次坐标，便于确认楼梯/门能不能过去
        if (_simFrames > 0 && Time.frameCount % 30 == 0)
            Debug.Log($"[Sim] f={Time.frameCount} pos=({p.x:F2},{p.y:F2},{p.z:F2}) moving={moving}");

        // 角色朝向与 3D 骨骼动画（移动朝移动方向；站立时缓缓转向镜头后方）
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
        if (_avatar != null) _avatar.Tick(moving, running, _facing, dt);

        // 可交互地点检测（建筑正门 / 操场 / 池塘 / 家）
        // 取**最近**的那个：门前交互圈会互相重叠，按数组顺序取第一个会挑错门
        string near = null;
        float nearD2 = float.MaxValue;
        foreach (var s in _w.W.SPOTS)
        {
            float dx = p.x - s.x, dz = p.z - s.z;
            float dd = dx * dx + dz * dz;
            if (dd > s.r * s.r || dd >= nearD2) continue;
            if (_im != null && _im.CanEnter(s.name) && !_im.NearEntrance(s.name, p)) continue;
            nearD2 = dd;
            near = s.name;
        }
        if (_time != null)
        {
            _time.Near = near;
            if (GameInput.Down("Interact"))
            {
                if (_indoor)
                {
                    // 室内：站在门口按 F 出去
                    if (_im != null && _im.NearExit) { _im.Exit(); return; }
                }
                else if (!string.IsNullOrEmpty(near) && _im != null)
                {
                    if (!_im.Enter(near)) _time.ShowToast($"「{near}」暂未开放内部");
                    else return;
                }
            }
        }

        // 跟随相机（球坐标；首帧已在 Init 吸附）
        // 相机：第一人称贴头 / 第三人称球坐标跟随
        _firstPerson=true;
        if (_firstPerson) { FirstPersonCamera(p); return; }
        Vector3 head = CameraAnchor(p);
        var cam = _cam.transform;
        Vector3 desired = CameraTarget(p);
        if (!_camInit) cam.position = desired;
        else
        {
            float t = Mathf.Min(1f, dt * 5f);
            cam.position = Vector3.Lerp(cam.position, desired, t);
        }
        _camInit = true;
        cam.LookAt(new Vector3(p.x, p.y + (_indoor ? 1.5f : 1.1f), p.z));

        // 镜头被墙挤到脸上时藏掉角色模型，否则整个屏幕都是后脑勺
        if (_avatar != null)
            _avatar.SetVisible(!_firstPerson && (cam.position - head).sqrMagnitude > 4f);
    }

    // 碰撞：建筑 AABB + 树干 + 栅栏 + 路灯 + 主角家（与 web 版逐条一致；边界 ±30 在移动处 clamp）
    bool Collides(float x, float z)
    {
        // 室内：只查当前房间、当前楼层的墙与家具
        if (_indoor && _im != null && _im.Current != null)
            return _im.Collides(x, z, _player.position.y);
        if(_w.W.RIVER!=null&&ClosedAreas.Contains(x,z,.36f))return true;
        // 建筑按朝向换算占地（HalfWX/HalfWZ）；门厅凸出的部分也计入，保证能走到可见的门口
        const float pad = 0.45f;
        foreach (var b in _w.W.BUILDINGS)
        {
            if(b.style=="stand"){if(StandAccess.Blocks(b,x,z))return true;continue;}
            if(PavilionFootprint.Uses(b))continue;
            float x0 = b.pos[0] - b.HalfWX - pad, x1 = b.pos[0] + b.HalfWX + pad;
            float z0 = b.pos[1] - b.HalfWZ - pad, z1 = b.pos[1] + b.HalfWZ + pad;
            if (x > x0 && x < x1 && z > z0 && z < z1) return true;
            // 门厅只占中央 2.5 米，不再把整幅外墙前方延长成空气墙。
            float e=b.EntranceExtra;
            if(e>0)
            {
                float dx=x-b.pos[0],dz=z-b.pos[1];
                float lateral=Mathf.Abs(b.face)==2?dz:dx;
                float forward=b.face==2?dx:b.face==-2?-dx:b.face==-1?-dz:dz;
                float front=Mathf.Abs(b.face)==2?b.HalfWX:b.HalfWZ;
                if(Mathf.Abs(lateral)<1.25f+pad&&forward>front-pad&&forward<front+e+.12f+pad)return true;
            }
        }
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
