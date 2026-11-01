using UnityEngine;

// 时间系统 + 昼夜过渡：对位移植 web 版 store.js（时钟/速度）与 CampusScene 的 FX_DAY/FX_NIGHT 插值
// 清晨 5-7 点天亮、黄昏 17:30-20:00 入夜；晨昏暖色峰值 6:30 / 18:30
public class TimeOfDay : MonoBehaviour
{
    public float GameMinutes = 8f * 60f;
    public int DayNum = 1;
    public float TimeSpeed = 6f;   // 每真实秒推进的游戏分钟数（0=暂停）
    public string Toast = "";

    float _toastUntil;
    WorldBuilder _w;
    Light _dir, _up, _fill;
    Weather _wth;
    bool _winNight;
    float _cullTimer;
    bool _perf;
    float _perfClock;
    int _perfFrames;
    PostFX _post;
    Material _skybox;

    public float Hour => GameMinutes / 60f;
    public float Mix { get; private set; }   // 夜晚混合值（0=白天 1=夜晚）
    public string Near { get; set; }         // 面前可交互地点名（由 PlayerRig 写入，Hud 显示）
    public string NearLabel { get; set; }    // 直接给整句提示（室内用），优先于 Near
    public bool Indoor;                      // 室内：环境光提亮偏暖
    public string WeatherName = "";          // 天气标签（由 Weather 写入，Hud 显示）

    // —— web 版配色常量 ——
    static readonly Color DAY_HEMI_SKY = MatLib.Hex("#d8ecff");
    static readonly Color DAY_HEMI_GND = MatLib.Hex("#3f5a36");
    static readonly Color DAY_DIR = MatLib.Hex("#ffeecd");
    static readonly Color NIGHT_HEMI_SKY = MatLib.Hex("#31426b");
    static readonly Color NIGHT_HEMI_GND = MatLib.Hex("#141f19");
    static readonly Color NIGHT_DIR = MatLib.Hex("#9fb8e8");
    static readonly Color WARM_DIR = MatLib.Hex("#ff9a4a");
    static readonly Color WARM_FOG = MatLib.Hex("#e0b28e");
    static readonly Color WARM_SKY = MatLib.Hex("#ffcf9e");
    static readonly Color DAY_FOG = MatLib.Hex("#bccad8");
    static readonly Color NIGHT_FOG = MatLib.Hex("#0e1626");
    static readonly Color GLASS_OFF = MatLib.Hex("#d5dbe4");
    static readonly Color GLASS_ON = MatLib.Hex("#ffd98a");
    // 写实向（2026-10-18 第二版）：压低整体曝光 + 保住「直射 : 环境」比 → 不再晃眼、层次更厚
    // （上一版白天 1.00:0.20 太亮，白墙/路面对比不足；夜晚同步略降，靠路灯/窗户发光撑起可读性）
    const float DAY_AMB = 0.28f, DAY_HEMI = 0.16f, DAY_DIR_I = 0.95f;
    const float NIGHT_AMB = 0.22f, NIGHT_HEMI = 0.16f, NIGHT_DIR_I = 0.40f;
    const float DAY_FOG_NEAR = 70f, DAY_FOG_FAR = 300f;
    const float NIGHT_FOG_NEAR = 60f, NIGHT_FOG_FAR = 230f;

    public static float Smooth(float x) => x * x * (3f - 2f * x);

    public static float NightMixAt(float h)
    {
        if (h < 5f) return 1f;
        if (h < 7f) return 1f - Smooth((h - 5f) / 2f);
        if (h < 17.5f) return 0f;
        if (h < 20f) return Smooth((h - 17.5f) / 2.5f);
        return 1f;
    }

    public static float WarmAt(float h)
    {
        float w1 = Mathf.Max(0f, 1f - Mathf.Abs(h - 6.5f) / 1.8f);
        float w2 = Mathf.Max(0f, 1f - Mathf.Abs(h - 18.4f) / 2.2f);
        return Mathf.Min(1f, Mathf.Max(w1, w2));
    }

    public void Init(WorldBuilder w)
    {
        _w = w;
        _post = Camera.main != null ? Camera.main.GetComponent<PostFX>() : null;

        // 命令行：-time 18.5 定表并暂停；-speed N 指定速度
        var t = Args.F("-time");
        if (t.HasValue)
        {
            GameMinutes = ((t.Value % 24f) + 24f) % 24f * 60f;
            TimeSpeed = 0f;
        }
        var sp = Args.F("-speed");
        if (sp.HasValue) TimeSpeed = sp.Value;
        _perf = Args.Has("-fps") || Args.Has("-fpslog");   // -fpslog 只记录帧率，保持 60 帧上限
        if (Args.Has("-fps"))
        {
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }

        // 太阳（web: directionalLight position [14,20,10]、intensity 1.42、2048² 柔影）
        var sun = new GameObject("Sun");
        sun.transform.SetParent(transform, false);
        sun.transform.position = new Vector3(14f, 20f, 10f);
        sun.transform.LookAt(Vector3.zero);
        _dir = sun.AddComponent<Light>();
        _dir.type = LightType.Directional;
        _dir.color = DAY_DIR;
        _dir.intensity = DAY_DIR_I;
        _dir.shadows = Args.Has("-noshadow") ? LightShadows.None : LightShadows.Soft;
        _dir.shadowStrength = 0.95f;
        _dir.shadowBias = 0.02f;
        _dir.shadowNormalBias = 0.4f;
        _dir.shadowCustomResolution = 4096;

        // 半球光（web: hemisphereLight 天空/地面双色）替代：反向弱平行光承载天空色
        var up = new GameObject("SkyFill");
        up.transform.SetParent(transform, false);
        up.transform.rotation = Quaternion.Euler(70f, -30f, 0f);
        _up = up.AddComponent<Light>();
        _up.type = LightType.Directional;
        _up.color = DAY_HEMI_SKY;
        _up.intensity = DAY_HEMI * 0.55f;
        _up.shadows = LightShadows.None;

        // 北向补光：把背阴立面（城镇里大量朝北/朝西的沿街面、背光的人物正面）抬起来。
        // 注意：平行光是沿 transform.forward 射出的，要让「朝 -X/-Z 的面」受光，光线必须射向 +X/+Z
        // （上一版写的是 Euler(22,200,0)，光线和太阳同向，等于把主光又加了一遍，背光面一点没补到）
        var fill = new GameObject("NorthFill");
        fill.transform.SetParent(transform, false);
        fill.transform.rotation = Quaternion.Euler(26f, 22f, 0f);
        _fill = fill.AddComponent<Light>();
        _fill.type = LightType.Directional;
        _fill.color = DAY_HEMI_SKY;
        _fill.intensity = 0.22f;
        _fill.shadows = LightShadows.None;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;

        // 写实天空：内置程序化天空盒（太阳盘 + 大气渐变），替换原来的平涂昼穹；
        // 夜穹保留为透明星空层叠在天空盒上。
        _skybox = Resources.Load<Material>("mat_holder/Skybox_Procedural");
        if (_skybox != null)
        {
            RenderSettings.skybox = _skybox;
            RenderSettings.sun = _dir;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = 256;
            RenderSettings.reflectionIntensity = 1f;
            var cam = Camera.main;
            if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
            if (_w != null && _w.SkyDayGo != null) _w.SkyDayGo.SetActive(false);
            Debug.Log("[TimeOfDay] 程序化天空启用");
        }
        else Debug.LogWarning("[TimeOfDay] 程序化天空材质未找到，保留昼穹");

        Apply();
    }

    void Update()
    {
        // 时间推进
        if (TimeSpeed > 0f)
        {
            GameMinutes += Time.deltaTime * TimeSpeed;
            while (GameMinutes >= 1440f) { GameMinutes -= 1440f; DayNum++; }
        }

        // 0/1/2/3 速度 ｜ T +12h ｜ [ ] ±1h
        if (Input.GetKeyDown(KeyCode.Alpha0)) SetSpeed(0f);
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetSpeed(1f);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetSpeed(6f);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetSpeed(30f);
        if (Input.GetKeyDown(KeyCode.T)) Advance(720f);
        if (Input.GetKeyDown(KeyCode.LeftBracket)) Advance(-60f);
        if (Input.GetKeyDown(KeyCode.RightBracket)) Advance(60f);
        if (Input.GetKeyDown(KeyCode.F10)) CycleFrameMode();

        Apply();

        if (Toast != "" && Time.time > _toastUntil) Toast = "";
    }

    // 灯光剔除放在 LateUpdate：此时相机已跟到玩家最终位置（Update 首帧相机还在原点，会把整街灯误关）
    void LateUpdate()
    {
        if (_w == null || Camera.main == null) return;
        if (_perf)
        {
            _perfClock += Time.unscaledDeltaTime;
            _perfFrames++;
            if (_perfFrames >= 300)
            {
                Debug.Log($"[Perf] 近 300 帧平均 {_perfFrames / Mathf.Max(0.001f, _perfClock):F1} FPS");
                _perfClock = 0f;
                _perfFrames = 0;
            }
        }
        _cullTimer -= Time.deltaTime;
        if (_cullTimer > 0f) return;
        _cullTimer = 0.2f;
        var cp = Camera.main.transform.position;
        for (int i = 0; i < _w.LampLights.Count; i++)
        {
            var ll = _w.LampLights[i];
            bool near = (ll.transform.position - cp).sqrMagnitude < 38f * 38f;
            if (ll.enabled != near) ll.enabled = near;
        }
    }

    public void SetSpeed(float s)
    {
        TimeSpeed = s;
        ShowToast(s <= 0f ? "时间已暂停（0/1/2/3 切换速度）" : $"时间速度：{s}×（1 秒 = {s} 游戏分钟）");
    }

    public void Advance(float min)
    {
        GameMinutes += min;
        while (GameMinutes >= 1440f) { GameMinutes -= 1440f; DayNum++; }
        while (GameMinutes < 0f) { GameMinutes += 1440f; if (DayNum > 1) DayNum--; else { GameMinutes = 0f; break; } }
        ShowToast($"时间 {(min > 0f ? "+" : "")}{min / 60f} 小时（T 跳 12h ｜ [ ] ±1h）");
    }

    int _frameMode;   // 0 锁 60 ｜ 1 垂直同步 ｜ 2 不锁帧

    // 高刷屏（144/165Hz）上「锁 60 且关垂直同步」会让每帧显示 2 或 3 个刷新周期，画面看着一卡一卡；
    // 给一个运行时切换键，作者可以在自己屏幕上直接试出最顺的一档。
    void CycleFrameMode()
    {
        _frameMode = (_frameMode + 1) % 3;
        if (_frameMode == 0)
        {
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            ShowToast("帧率：锁定 60（默认）");
        }
        else if (_frameMode == 1)
        {
            QualitySettings.vSyncCount = 1; Application.targetFrameRate = 60;
            ShowToast("帧率：垂直同步 · 高刷屏更顺（约 55~60）");
        }
        else
        {
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            ShowToast("帧率：不锁帧（跟随刷新率，最跟手）");
        }
    }

    public void ShowToast(string msg)
    {
        Toast = msg;
        _toastUntil = Time.time + 2f;
    }

    // 逐帧应用昼夜（灯光/雾/天空/路灯/窗户）
    void Apply()
    {
        float h = Hour;
        float m = NightMixAt(h);
        float warm = WarmAt(h) * (1f - m * 0.65f);
        Mix = m;

        // 天气调制（雨/雪：压直射、抬/压环境、雾更近、日影变软）
        if (_wth == null) _wth = GetComponent<Weather>();
        float wFog = _wth != null ? _wth.FogAmt : 0f;
        float wSun = _wth != null ? _wth.SunMul : 1f;
        float wAmb = _wth != null ? _wth.AmbMul : 1f;
        float wShadow = _wth != null ? _wth.ShadowMul : 1f;

        // 环境光（ambient + 半球地面色）：0.28 的天空权重 → 阴影偏冷蓝（写实：天光主导阴影）
        Color gnd = Color.Lerp(DAY_HEMI_GND, NIGHT_HEMI_GND, m);
        Color sky = Color.Lerp(DAY_HEMI_SKY, NIGHT_HEMI_SKY, m);
        Color amb = Color.Lerp(sky, gnd, 0.28f);
        float ambI = Mathf.Lerp(DAY_AMB, NIGHT_AMB, m) * wAmb;
        // 三色环境光（天空/地平/地面）：阴影偏冷蓝，比平环境光更有体积
        Color ambSky, ambEq, ambGnd;
        if (Indoor)
        {
            // 室内：中性化、整体抬亮（室内灯多为暖白）
            Color n = Color.Lerp(amb, new Color(0.96f, 0.96f, 0.95f), 0.35f);
            float k = ambI * 2.1f;
            ambSky = n * k;
            ambEq = n * (k * 0.92f);
            ambGnd = n * (k * 0.84f);
        }
        else
        {
            ambSky = sky * ambI;
            ambEq = Color.Lerp(sky, gnd, 0.45f) * (ambI * 0.85f);
            ambGnd = gnd * (ambI * 0.90f);
        }
        RenderSettings.ambientSkyColor = new Color(ambSky.r, ambSky.g, ambSky.b, 1f);
        RenderSettings.ambientEquatorColor = new Color(ambEq.r, ambEq.g, ambEq.b, 1f);
        RenderSettings.ambientGroundColor = new Color(ambGnd.r, ambGnd.g, ambGnd.b, 1f);

        if (_dir != null)
        {
            _dir.intensity = Mathf.Lerp(DAY_DIR_I, NIGHT_DIR_I, m) * wSun;
            var baseDir = Color.Lerp(Color.Lerp(DAY_DIR, NIGHT_DIR, m), WARM_DIR, warm * 0.75f);
            _dir.color = _wth != null && wFog > 0.001f ? Color.Lerp(baseDir, _wth.SunTint, wFog * 0.75f) : baseDir;
            _dir.shadowStrength = Mathf.Lerp(0.85f, 0.22f, wFog) * Mathf.Lerp(1f, 0.85f, m);
        }
        if (_up != null)
        {
            _up.intensity = Mathf.Lerp(DAY_HEMI, NIGHT_HEMI, m) * 0.55f;
            _up.color = sky;
        }

        // 水面着色（Campus/Water 自定义着色器）
        if (_dir != null)
        {
            Shader.SetGlobalColor("_SunColor", _dir.color * _dir.intensity);
            Shader.SetGlobalVector("_SunDir", -_dir.transform.forward);
        }
        Shader.SetGlobalColor("_SkyColor", Color.Lerp(sky, WARM_SKY, warm * 0.5f) * Mathf.Lerp(0.75f, 0.2f, m) * (_wth != null ? _wth.SkyMul : 1f));
        Shader.SetGlobalFloat("_WaterAmbient", Mathf.Lerp(1f, 0.3f, m));
        if (_fill != null)
        {
            _fill.intensity = Mathf.Lerp(0.22f, 0.12f, m) * wAmb;
            _fill.color = sky;
        }

        // 雾（雨雪：更近、更灰/更白）
        var fogBase = Color.Lerp(Color.Lerp(DAY_FOG, NIGHT_FOG, m), WARM_FOG, warm * 0.45f);
        RenderSettings.fogColor = _wth != null && wFog > 0.001f ? Color.Lerp(fogBase, _wth.FogTint, wFog) : fogBase;
        float fogNearMul = _wth != null ? _wth.FogNearMul : 1f;
        float fogFarMul = _wth != null ? _wth.FogFarMul : 1f;
        RenderSettings.fogStartDistance = Mathf.Lerp(DAY_FOG_NEAR, NIGHT_FOG_NEAR, m) * fogNearMul;
        RenderSettings.fogEndDistance = Mathf.Lerp(DAY_FOG_FAR, NIGHT_FOG_FAR, m) * fogFarMul;

        // 程序化天空：太阳盘只在白天出现，晨昏染暖，夜晚低曝光
        if (_skybox != null)
        {
            _skybox.SetFloat("_SunDisk", m > 0.55f ? 0f : 2f);
            _skybox.SetFloat("_SunSize", 0.035f);
            _skybox.SetFloat("_SunSizeConvergence", 5f);
            _skybox.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.90f, 0.40f, m));
            _skybox.SetColor("_SkyTint", Color.Lerp(new Color(0.42f, 0.62f, 0.95f), WARM_SKY, warm * 0.65f));
            _skybox.SetColor("_GroundColor", Color.Lerp(RenderSettings.fogColor, gnd, 0.25f));
            _skybox.SetFloat("_Exposure", Mathf.Lerp(1.0f, 0.10f, m));
        }

        // 天空双穹：夜穹淡入 + 白天穹晨昏染暖；星空缓转、云缓漂
        if (_w != null)
        {
            float nightOp = Mathf.Clamp01((m - 0.35f) / 0.45f);
            if (_w.SkyNightMat != null) _w.SkyNightMat.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, nightOp * 0.5f));
            if (_w.SkyDayMat != null)
            {
                float w2 = warm * (1f - m * 0.65f) * 0.8f;
                _w.SkyDayMat.SetColor("_TintColor", Color.Lerp(Color.white, WARM_SKY, w2) * 0.42f * (_wth != null ? _wth.SkyMul : 1f));
                var off = _w.SkyDayMat.mainTextureOffset;
                _w.SkyDayMat.mainTextureOffset = new Vector2(off.x - Time.deltaTime * 0.0045f, off.y);
            }
            if (_w.SkyNightTr != null) _w.SkyNightTr.Rotate(0f, Time.deltaTime * 0.004f * Mathf.Rad2Deg, 0f);

            // 路灯：灯头玻璃变暖 + 地面光池淡入（web: opacity = m*0.68）
            if (_w.LampGlass != null)
            {
                _w.LampGlass.color = Color.Lerp(GLASS_OFF, GLASS_ON, m);
                _w.LampGlass.EnableKeyword("_EMISSION");
                _w.LampGlass.SetColor("_EmissionColor", new Color(1f, 0.72f, 0.4f) * (m * 0.85f));
            }
            if (_w.LampPool != null) _w.LampPool.SetColor("_TintColor", new Color(1f, 0.82f, 0.55f, m * 0.55f));
            for (int i = 0; i < _w.LampLights.Count; i++)
                _w.LampLights[i].intensity = m * (i < _w.LampLightPower.Count ? _w.LampLightPower[i] : 1f);

            // 后处理随昼夜：夜晚泛光更强（路灯/窗户晕开）、暗角略深
            if (_post != null)
            {
                // 写实档：白天几乎不泛光、暗角很轻；夜里只让灯/窗/招牌晕开
                _post.BloomIntensity = Mathf.Lerp(0.07f, 0.22f, m);
                _post.VigStrength = Mathf.Lerp(0.16f, 0.26f, m);
                _post.Threshold = Mathf.Lerp(1.10f, 0.85f, m);
            }

            // 店铺橱窗夜里透暖光（立面窗格由贴图渐变表现，不再覆盖玻璃片）
            if (_w.WindowGlassMat != null)
            {
                if (m > 0.15f)
                {
                    _w.WindowGlassMat.EnableKeyword("_EMISSION");
                    _w.WindowGlassMat.SetColor("_EmissionColor", new Color(0.55f, 0.32f, 0.12f) * (m * 0.7f));
                }
                else _w.WindowGlassMat.DisableKeyword("_EMISSION");
            }
            if (_w.WindowGlowMat != null)
                _w.WindowGlowMat.color = new Color(0.95f, 0.72f, 0.42f) * Mathf.Clamp01(m * 0.95f);
            bool glowOn = m >= 0.25f;
            for (int i = 0; i < _w.WindowGlowRenders.Count; i++)
                if (_w.WindowGlowRenders[i].enabled != glowOn) _w.WindowGlowRenders[i].enabled = glowOn;
            bool signGlowOn = m >= 0.25f;
            for (int i = 0; i < _w.SignGlowRenders.Count; i++)
                if (_w.SignGlowRenders[i].enabled != signGlowOn) _w.SignGlowRenders[i].enabled = signGlowOn;

            // 店铺招牌：夜里用自身贴图做自发光；门灯/骑楼灯笼亮起
            for (int i = 0; i < _w.SignMatRefs.Count; i++)
            {
                var sm = _w.SignMatRefs[i];
                if (m > 0.05f)
                {
                    sm.EnableKeyword("_EMISSION");
                    sm.SetTexture("_EmissionMap", sm.mainTexture);
                    sm.SetColor("_EmissionColor", new Color(1f, 0.82f, 0.55f) * (m * 0.9f));
                }
                else sm.DisableKeyword("_EMISSION");
            }
            if (_w.DoorLampMat != null)
            {
                if (m > 0.02f)
                {
                    _w.DoorLampMat.EnableKeyword("_EMISSION");
                    _w.DoorLampMat.SetColor("_EmissionColor", new Color(1f, 0.78f, 0.45f) * (m * 0.8f));
                }
                else _w.DoorLampMat.DisableKeyword("_EMISSION");
            }
            if (_w.LanternMat != null)
            {
                if (m > 0.02f)
                {
                    _w.LanternMat.EnableKeyword("_EMISSION");
                    _w.LanternMat.SetColor("_EmissionColor", new Color(1f, 0.32f, 0.18f) * (m * 0.9f));
                }
                else _w.LanternMat.DisableKeyword("_EMISSION");
            }
        }
    }
}
