using System.Collections.Generic;
using UnityEngine;

// 天气系统（雨 / 雪）：程序化粒子 + 地面湿滑 / 积雪 + 与光照、雾、天空联动
//   切换：Y 键 晴 → 雨 → 雪 ｜ 命令行 -weather rain|snow|clear（截图验收：直接给满强度，跳过渐变）
// 设计取向（写实）：
//   雨 —— 斜向雨丝、路面变暗起光（湿反射）、天光压暗、雾更近更灰、日影变软
//   雪 —— 慢速飘雪（带噪波漂移）、地面覆盖雪层、树冠/绿篱略覆白、雪面反光把环境光抬高、雾发白
public class Weather : MonoBehaviour
{
    public enum Kind { Clear, Rain, Snow }
    public Kind Current { get; private set; } = Kind.Clear;
    public bool Indoor;                     // 室内：不下雨雪（粒子停发，天气状态保留）
    public float RainAmt { get; private set; }
    public float SnowAmt { get; private set; }
    public float FogAmt => Mathf.Max(RainAmt, SnowAmt);

    // ── 供 TimeOfDay 每帧读取的调制系数 ──
    public float SunMul => 1f - 0.45f * RainAmt - 0.62f * SnowAmt;
    public float AmbMul => 1f + 0.06f * RainAmt + 0.32f * SnowAmt;     // 雪面反光抬高环境光
    public float ShadowMul => Mathf.Clamp01(1f - 0.55f * RainAmt - 0.85f * SnowAmt);
    public float SkyMul => 1f - 0.32f * RainAmt - 0.10f * SnowAmt;
    public float FogNearMul => 1f - 0.45f * RainAmt - 0.30f * SnowAmt;
    public float FogFarMul => 1f - 0.50f * RainAmt - 0.38f * SnowAmt;
    public Color FogTint => SnowAmt >= RainAmt
        ? new Color(0.80f, 0.83f, 0.88f)      // 雪：发白的雾
        : new Color(0.52f, 0.55f, 0.60f);     // 雨：发灰的雾
    public Color SunTint => new Color(0.78f, 0.81f, 0.86f);   // 雨雪天太阳偏灰蓝

    Transform _player;
    TimeOfDay _tod;
    ParticleSystem _rain, _snow;
    Material _rainMat, _snowMat;
    Renderer _snowLayerR;
    Material _snowLayer;
    readonly List<Material> _wet = new List<Material>();
    readonly List<Color> _wetBase = new List<Color>();
    readonly List<float> _wetGloss = new List<float>();
    readonly List<Material> _frost = new List<Material>();
    readonly List<Color> _frostBase = new List<Color>();

    public void Init(Transform player, TimeOfDay tod, List<Material> wetMats, List<Material> frostMats,
                     Renderer snowLayer, Material snowLayerMat)
    {
        _player = player; _tod = tod;
        _snowLayerR = snowLayer; _snowLayer = snowLayerMat;
        if (wetMats != null)
            foreach (var m in wetMats)
            {
                if (m == null) continue;
                _wet.Add(m); _wetBase.Add(m.color); _wetGloss.Add(m.GetFloat("_Glossiness"));
            }
        if (frostMats != null)
            foreach (var m in frostMats)
            {
                if (m == null) continue;
                _frost.Add(m); _frostBase.Add(m.color);
            }

        _rain = MakeRain();
        _snow = MakeSnow();

        // 命令行：直接切到指定天气并给满强度（截图用）
        var w = Args.Str("-weather");
        if (!string.IsNullOrEmpty(w))
        {
            switch (w.ToLowerInvariant())
            {
                case "rain": Current = Kind.Rain; RainAmt = 1f; break;
                case "snow": Current = Kind.Snow; SnowAmt = 1f; break;
                default: Current = Kind.Clear; break;
            }
            if (RainAmt > 0f) _rain.Play();
            if (SnowAmt > 0f) _snow.Play();
        }
        Apply();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Current = Current == Kind.Clear ? Kind.Rain : Current == Kind.Rain ? Kind.Snow : Kind.Clear;
            string name = Current == Kind.Clear ? "晴" : Current == Kind.Rain ? "雨" : "雪";
            if (_tod != null) _tod.ShowToast("天气：" + name);
        }

        float tRain = Current == Kind.Rain ? 1f : 0f;
        float tSnow = Current == Kind.Snow ? 1f : 0f;
        RainAmt = Mathf.MoveTowards(RainAmt, tRain, Time.deltaTime / 1.6f);
        SnowAmt = Mathf.MoveTowards(SnowAmt, tSnow, Time.deltaTime / 2.4f);
        Apply();
    }

    void Apply()
    {
        if (_tod != null)
            _tod.WeatherName = Current == Kind.Rain ? "雨" : Current == Kind.Snow ? "雪" : "";
        // 粒子：跟随玩家（世界空间模拟 → 已落下的雨丝留在世界里，观感自然）
        if (_player != null)
        {
            if (_rain != null) _rain.transform.position = _player.position + new Vector3(0f, 11f, 0f);
            if (_snow != null) _snow.transform.position = _player.position + new Vector3(0f, 9f, 0f);
        }
        float outMul = Indoor ? 0f : 1f;
        SetEmission(_rain, RainAmt * 1500f * outMul);   // 作者反馈雨点太多：4200 → 1500
        SetEmission(_snow, SnowAmt * 1600f * outMul);

        // 湿地面：变暗 + 起光（雨天最明显的写实线索）
        for (int i = 0; i < _wet.Count; i++)
        {
            var m = _wet[i];
            var dark = new Color(0.60f, 0.64f, 0.70f, _wetBase[i].a);
            m.color = Color.Lerp(_wetBase[i], dark, RainAmt);
            m.SetFloat("_Glossiness", Mathf.Lerp(_wetGloss[i], 0.58f, RainAmt));
        }
        // 雨雪粒子亮度随昼夜（夜晚不要发光）
        float night = _tod != null ? _tod.Mix : 0f;
        if (_rainMat != null)
            _rainMat.SetColor("_TintColor", Color.Lerp(new Color(0.50f, 0.53f, 0.60f, 0.50f), new Color(0.22f, 0.26f, 0.34f, 0.45f), night));
        if (_snowMat != null)
            _snowMat.SetColor("_TintColor", Color.Lerp(new Color(0.40f, 0.41f, 0.44f, 0.55f), new Color(0.22f, 0.24f, 0.28f, 0.50f), night));
        // 雪：树冠/绿篱略覆白
        for (int i = 0; i < _frost.Count; i++)
            _frost[i].color = Color.Lerp(_frostBase[i], new Color(0.86f, 0.89f, 0.93f, _frostBase[i].a), SnowAmt * 0.55f);
        // 雪层：alpha 随雪量
        if (_snowLayer != null)
        {
            var c = _snowLayer.color;
            c.a = SnowAmt * 0.90f;
            _snowLayer.color = c;
        }
        if (_snowLayerR != null && _snowLayerR.enabled != SnowAmt > 0.005f)
            _snowLayerR.enabled = SnowAmt > 0.005f;
    }

    static void SetEmission(ParticleSystem ps, float rate)
    {
        if (ps == null) return;
        var em = ps.emission;
        em.rateOverTime = rate;
        if (rate > 0.01f && !ps.isPlaying) ps.Play();
        if (rate <= 0.01f && ps.isEmitting) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    ParticleSystem MakeRain()
    {
        var go = new GameObject("Rain");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = false;
        main.startLifetime = 1.1f;
        main.startSpeed = 0f;
        main.startSize = 0.05f;
        main.startColor = new Color(0.78f, 0.85f, 0.95f, 0.55f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 2200;
        var em = ps.emission; em.rateOverTime = 0f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(34f, 1f, 34f);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-1.6f);
        vel.y = new ParticleSystem.MinMaxCurve(-16f);
        vel.z = new ParticleSystem.MinMaxCurve(0f);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.lengthScale = 26f;
        r.velocityScale = 0f;
        _rainMat = MatLib.Fade("rain_streak", new Color(0.50f, 0.53f, 0.60f, 0.50f));   // 粒子着色器自带 ×2，0.5 抵消
        r.material = _rainMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        ps.Play();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        return ps;
    }

    ParticleSystem MakeSnow()
    {
        var go = new GameObject("Snow");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = false;
        main.startLifetime = 9f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.34f);   // 5cm 的真实雪片在 13m 外不到 1px，放大到可读
        main.startColor = new Color(1f, 1f, 1f, 0.85f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 4200;
        var em = ps.emission; em.rateOverTime = 0f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(34f, 6f, 34f);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0.45f);
        vel.y = new ParticleSystem.MinMaxCurve(-1.15f);
        vel.z = new ParticleSystem.MinMaxCurve(0.2f);
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.75f;
        noise.frequency = 0.25f;
        noise.scrollSpeed = 0.12f;
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        _snowMat = MatLib.Fade("snow_flake", new Color(0.40f, 0.41f, 0.44f, 0.55f));   // 粒子着色器 ×2 → 有效约 0.8，低于泛光阈值
        r.material = _snowMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        ps.Play();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        return ps;
    }
}
