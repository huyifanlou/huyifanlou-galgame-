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
    Light _dir, _up;
    bool _winNight;

    public float Hour => GameMinutes / 60f;
    public float Mix { get; private set; }   // 夜晚混合值（0=白天 1=夜晚）
    public string Near { get; set; }         // 面前可交互地点名（由 PlayerRig 写入，Hud 显示）

    // —— web 版配色常量 ——
    static readonly Color DAY_HEMI_SKY = MatLib.Hex("#d8ecff");
    static readonly Color DAY_HEMI_GND = MatLib.Hex("#3f5a36");
    static readonly Color DAY_DIR = MatLib.Hex("#fff4da");
    static readonly Color NIGHT_HEMI_SKY = MatLib.Hex("#31426b");
    static readonly Color NIGHT_HEMI_GND = MatLib.Hex("#141f19");
    static readonly Color NIGHT_DIR = MatLib.Hex("#9fb8e8");
    static readonly Color WARM_DIR = MatLib.Hex("#ff9a4a");
    static readonly Color WARM_FOG = MatLib.Hex("#e0b28e");
    static readonly Color WARM_SKY = MatLib.Hex("#ffcf9e");
    static readonly Color DAY_FOG = MatLib.Hex("#cfe3f4");
    static readonly Color NIGHT_FOG = MatLib.Hex("#0e1626");
    static readonly Color GLASS_OFF = MatLib.Hex("#d5dbe4");
    static readonly Color GLASS_ON = MatLib.Hex("#ffd98a");
    const float DAY_AMB = 0.34f, DAY_HEMI = 0.14f, DAY_DIR_I = 0.95f;
    const float NIGHT_AMB = 0.22f, NIGHT_HEMI = 0.12f, NIGHT_DIR_I = 0.45f;
    const float DAY_FOG_NEAR = 62f, DAY_FOG_FAR = 158f;
    const float NIGHT_FOG_NEAR = 48f, NIGHT_FOG_FAR = 138f;

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

        // 命令行：-time 18.5 定表并暂停；-speed N 指定速度
        var t = Args.F("-time");
        if (t.HasValue)
        {
            GameMinutes = ((t.Value % 24f) + 24f) % 24f * 60f;
            TimeSpeed = 0f;
        }
        var sp = Args.F("-speed");
        if (sp.HasValue) TimeSpeed = sp.Value;

        // 太阳（web: directionalLight position [14,20,10]、intensity 1.42、2048² 柔影）
        var sun = new GameObject("Sun");
        sun.transform.SetParent(transform, false);
        sun.transform.position = new Vector3(14f, 20f, 10f);
        sun.transform.LookAt(Vector3.zero);
        _dir = sun.AddComponent<Light>();
        _dir.type = LightType.Directional;
        _dir.color = DAY_DIR;
        _dir.intensity = DAY_DIR_I;
        _dir.shadows = LightShadows.Soft;
        _dir.shadowStrength = 0.9f;
        _dir.shadowBias = 0.02f;
        _dir.shadowNormalBias = 0.4f;
        _dir.shadowCustomResolution = 2048;

        // 半球光（web: hemisphereLight 天空/地面双色）替代：反向弱平行光承载天空色
        var up = new GameObject("SkyFill");
        up.transform.SetParent(transform, false);
        up.transform.rotation = Quaternion.Euler(70f, -30f, 0f);
        _up = up.AddComponent<Light>();
        _up.type = LightType.Directional;
        _up.color = DAY_HEMI_SKY;
        _up.intensity = DAY_HEMI * 0.55f;
        _up.shadows = LightShadows.None;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;

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

        Apply();

        if (Toast != "" && Time.time > _toastUntil) Toast = "";
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

        // 环境光（ambient + 半球地面色）
        Color gnd = Color.Lerp(DAY_HEMI_GND, NIGHT_HEMI_GND, m);
        Color sky = Color.Lerp(DAY_HEMI_SKY, NIGHT_HEMI_SKY, m);
        Color amb = Color.Lerp(sky, gnd, 0.45f);
        float ambI = Mathf.Lerp(DAY_AMB, NIGHT_AMB, m);
        RenderSettings.ambientLight = new Color(amb.r * ambI, amb.g * ambI, amb.b * ambI, 1f);

        if (_dir != null)
        {
            _dir.intensity = Mathf.Lerp(DAY_DIR_I, NIGHT_DIR_I, m);
            _dir.color = Color.Lerp(Color.Lerp(DAY_DIR, NIGHT_DIR, m), WARM_DIR, warm * 0.75f);
        }
        if (_up != null)
        {
            _up.intensity = Mathf.Lerp(DAY_HEMI, NIGHT_HEMI, m) * 0.55f;
            _up.color = sky;
        }

        // 雾
        RenderSettings.fogColor = Color.Lerp(Color.Lerp(DAY_FOG, NIGHT_FOG, m), WARM_FOG, warm * 0.45f);
        RenderSettings.fogStartDistance = Mathf.Lerp(DAY_FOG_NEAR, NIGHT_FOG_NEAR, m);
        RenderSettings.fogEndDistance = Mathf.Lerp(DAY_FOG_FAR, NIGHT_FOG_FAR, m);

        // 天空双穹：夜穹淡入 + 白天穹晨昏染暖；星空缓转、云缓漂
        if (_w != null)
        {
            float nightOp = Mathf.Clamp01((m - 0.35f) / 0.45f);
            if (_w.SkyNightMat != null) _w.SkyNightMat.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, nightOp * 0.5f));
            if (_w.SkyDayMat != null)
            {
                float w2 = warm * (1f - m * 0.65f) * 0.8f;
                _w.SkyDayMat.SetColor("_TintColor", Color.Lerp(Color.white, WARM_SKY, w2) * 0.5f);
                var off = _w.SkyDayMat.mainTextureOffset;
                _w.SkyDayMat.mainTextureOffset = new Vector2(off.x - Time.deltaTime * 0.0045f, off.y);
            }
            if (_w.SkyNightTr != null) _w.SkyNightTr.Rotate(0f, Time.deltaTime * 0.004f * Mathf.Rad2Deg, 0f);

            // 路灯：灯头玻璃变暖 + 地面光池淡入（web: opacity = m*0.68）
            if (_w.LampGlass != null) _w.LampGlass.color = Color.Lerp(GLASS_OFF, GLASS_ON, m);
            if (_w.LampPool != null) _w.LampPool.SetColor("_TintColor", new Color(1f, 1f, 1f, m * 0.68f));

            // 建筑窗户：昼夜双贴图（阈值 0.45，跨阈值才切换）
            bool night = m > 0.45f;
            if (night != _winNight)
            {
                _winNight = night;
                for (int i = 0; i < _w.WindowMatRefs.Count; i++)
                {
                    var def = _w.WindowMats[i];
                    _w.WindowMatRefs[i].mainTexture = MatLib.Tex(night ? def[1] : def[0], pixel: false);
                }
            }
        }
    }
}
