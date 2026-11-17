using System.Collections.Generic;
using UnityEngine;

// 天气系统（雨 / 雪）：程序化粒子 + 地面湿滑 / 积雪 + 与光照、雾、天空联动
//   Y 打开天气设置；雨雪强度和颗粒尺寸独立保存，命令行 -weather 仍供验收。
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
    public float Wetness { get; private set; }
    public float SnowCover { get; private set; }
    public float RainStrength { get; private set; }=1;
    public float SnowStrength { get; private set; }=1;
    public float ParticleScale { get; private set; }=1;
    bool _preferencesDirty;float _saveAfter;
    static string PreferencesPrefix=>(Args.Has("-weatheruicheck")||Args.Has("-weatheruireload")||Args.Has("-progresscheck")||Args.Has("-progressreload")||Args.Has("-archivecheck")||Args.Has("-uicheck")||Args.Has("-uireload")||Args.Has("-renewalcheck")||Args.Has("-menuroutecheck")||Args.Has("-submissioncapture"))?"QA.Weather.":"Weather.";
    public float FogAmt => Mathf.Max(RainAmt, SnowAmt);

    // ── 供 TimeOfDay 每帧读取的调制系数 ──
    public float SunMul => 1f - 0.68f * RainAmt - 0.60f * SnowAmt;
    public float AmbMul => 1f - 0.14f * RainAmt + 0.22f * SnowAmt;
    public float ShadowMul => Mathf.Clamp01(1f - 0.55f * RainAmt - 0.85f * SnowAmt);
    public float SkyMul => 1f - 0.30f * RainAmt - 0.04f * SnowAmt;
    public float FogNearMul => 1f - 0.80f * RainAmt - 0.88f * SnowAmt;
    public float FogFarMul => 1f - 0.60f * RainAmt - 0.70f * SnowAmt;
    public Color FogTint => SnowAmt >= RainAmt
        ? new Color(0.80f, 0.83f, 0.88f)      // 雪：发白的雾
        : new Color(0.52f, 0.55f, 0.60f);     // 雨：发灰的雾
    public Color SunTint => new Color(0.78f, 0.81f, 0.86f);   // 雨雪天太阳偏灰蓝

    Transform _player;
    TimeOfDay _tod;
    ParticleSystem _rain, _snow;
    Material _rainMat, _snowMat;
    bool _wasIndoor;
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
                var shine = MatLib.SurfaceShineProperty(m);
                _wet.Add(m); _wetBase.Add(m.color); _wetGloss.Add(shine != null ? m.GetFloat(shine) : 0f);
            }
        if (frostMats != null)
            foreach (var m in frostMats)
            {
                if (m == null) continue;
                _frost.Add(m); _frostBase.Add(m.color);
            }

        _rain = MakeRain();
        _snow = MakeSnow();
        Configure(PlayerPrefs.GetFloat(PreferencesPrefix+"Rain",1),PlayerPrefs.GetFloat(PreferencesPrefix+"Snow",1),PlayerPrefs.GetFloat(PreferencesPrefix+"Size",1),false);
        var impacts=gameObject.AddComponent<RainSurfaceDetail>();impacts.Init(this,GetComponent<WorldBuilder>(),player);

        // 命令行：直接切到指定天气并给满强度（截图用）
        var w = Args.Str("-weather");
        if (!string.IsNullOrEmpty(w))
        {
            switch (w.ToLowerInvariant())
            {
                case "rain": Current = Kind.Rain; RainAmt = RainStrength; break;
                case "snow": Current = Kind.Snow; SnowAmt = SnowStrength; break;
                default: Current = Kind.Clear; break;
            }
            if (RainAmt > 0f) _rain.Play();
            if (SnowAmt > 0f) _snow.Play();
        }
        Wetness=RainAmt;SnowCover=SnowAmt;Apply();
        if(Args.Has("-weatherpanel"))HelpSidebar.Live?.OpenWeather();
        if(Args.Has("-weatheruicheck")||Args.Has("-weatheruireload"))gameObject.AddComponent<WeatherSettingsReview>();
    }

    public void Restore(Kind kind)
    {Current=kind;RainAmt=kind==Kind.Rain?RainStrength:0;SnowAmt=kind==Kind.Snow?SnowStrength:0;Wetness=RainAmt;SnowCover=SnowAmt;Apply();}

    void Update()
    {
        float tRain = Current == Kind.Rain ? RainStrength : 0f;
        float tSnow = Current == Kind.Snow ? SnowStrength : 0f;
        RainAmt = Mathf.MoveTowards(RainAmt, tRain, Time.deltaTime / 2f);
        SnowAmt = Mathf.MoveTowards(SnowAmt, tSnow, Time.deltaTime / 2.8f);
        Wetness=Mathf.MoveTowards(Wetness,RainAmt,Time.deltaTime/(RainAmt>Wetness?3:24));
        SnowCover=Mathf.MoveTowards(SnowCover,SnowAmt,Time.deltaTime/(SnowAmt>SnowCover?3.4f:30));
        Apply();
        if(_preferencesDirty&&Time.unscaledTime>=_saveAfter)FlushPreferences();
    }
    public void TransitionTo(Kind kind)
    {Current=kind;if(_tod!=null)_tod.ShowToast("天气："+(kind==Kind.Clear?"晴":kind==Kind.Rain?"雨":"雪"));}
    static float Setting(float value,float min,float max,float fallback)=>float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp(value,min,max);
    public void Configure(float rain,float snow,float size,bool remember=true)
    {
        RainStrength=Setting(rain,0,1,1);SnowStrength=Setting(snow,0,1,1);ParticleScale=Setting(size,.5f,2,1);
        ApplyParticleSize();
        if(remember){_preferencesDirty=true;_saveAfter=Time.unscaledTime+.35f;}
    }
    void ApplyParticleSize()
    {
        if(_rain!=null){var main=_rain.main;main.startSize=new ParticleSystem.MinMaxCurve(.045f*ParticleScale,.075f*ParticleScale);}
        if(_snow!=null){var main=_snow.main;main.startSize=new ParticleSystem.MinMaxCurve(.12f*ParticleScale,.26f*ParticleScale);}
    }
    public void FlushPreferences()
    {
        if(!_preferencesDirty)return;
        PlayerPrefs.SetFloat(PreferencesPrefix+"Rain",RainStrength);PlayerPrefs.SetFloat(PreferencesPrefix+"Snow",SnowStrength);PlayerPrefs.SetFloat(PreferencesPrefix+"Size",ParticleScale);PlayerPrefs.Save();_preferencesDirty=false;
    }
    void OnApplicationQuit(){FlushPreferences();}
    public void WriteSettings(ProgressSave save)
    {save.weatherSettingsVersion=1;save.rainStrength=RainStrength;save.snowStrength=SnowStrength;save.precipitationSize=ParticleScale;}
    public void ReadSettings(ProgressSave save)
    {Configure(save.weatherSettingsVersion>0?save.rainStrength:1,save.weatherSettingsVersion>0?save.snowStrength:1,save.weatherSettingsVersion>0?save.precipitationSize:1);}

    void Apply()
    {
        Shader.SetGlobalFloat("_WeatherRain",Wetness);
        Shader.SetGlobalFloat("_WeatherSnow",SnowCover);
        Shader.SetGlobalFloat("_PrecipitationRain",RainAmt);
        Shader.SetGlobalFloat("_PrecipitationSnow",SnowAmt);
        float gust=.75f+Mathf.Sin(Time.time*.43f)*.25f;
        Shader.SetGlobalVector("_WeatherWind",new Vector4(-1.8f*gust,.4f*gust,0,0));
        if(_rain!=null){var velocity=_rain.velocityOverLifetime;velocity.x=-1.6f*gust;}
        if(_snow!=null){var velocity=_snow.velocityOverLifetime;velocity.x=-.45f*gust;}
        if(Indoor&&!_wasIndoor){_rain.Clear();_snow.Clear();}
        _wasIndoor=Indoor;
        if (_tod != null)
            _tod.WeatherName = Current == Kind.Rain ? "雨" : Current == Kind.Snow ? "雪" : "";
        // 粒子：跟随玩家（世界空间模拟 → 已落下的雨丝留在世界里，观感自然）
        if (_player != null)
        {
            if (_rain != null) _rain.transform.position = _player.position + new Vector3(0f, 10f, 0f);
            if (_snow != null) _snow.transform.position = _player.position + new Vector3(0f, 6f, 0f);
        }
        float outMul = Indoor ? 0f : 1f;
        SetEmission(_rain, RainAmt * 3600f * outMul);
        SetEmission(_snow, SnowAmt * 1050f * outMul);

        // 湿地面：变暗 + 起光（雨天最明显的写实线索）
        for (int i = 0; i < _wet.Count; i++)
        {
            var m = _wet[i];
            // Surface shader keeps the original hue and handles wet microfacets per upward face.
            m.color = _wetBase[i];
            var shine = MatLib.SurfaceShineProperty(m);
            if (shine != null) m.SetFloat(shine, Mathf.Lerp(_wetGloss[i], 0.32f, RainAmt));
        }
        // 雨雪粒子亮度随昼夜（夜晚不要发光）
        float night = _tod != null ? _tod.Mix : 0f;
        if (_rainMat != null)
            _rainMat.SetColor("_TintColor", Color.Lerp(new Color(.78f,.84f,.89f,.78f),new Color(.46f,.54f,.65f,.72f),night));
        if (_snowMat != null)
            _snowMat.SetColor("_TintColor", Color.Lerp(new Color(.86f,.90f,.94f,.92f),new Color(.55f,.64f,.76f,.86f),night));
        // 雪：树冠/绿篱略覆白
        for (int i = 0; i < _frost.Count; i++)
            _frost[i].color = _frostBase[i];
        // 雪层：alpha 随雪量
        if (_snowLayer != null)
        {
            var c = _snowLayer.color;
            c.a = SnowAmt * 0.90f;
            _snowLayer.color = c;
        }
        // Retire the extra coplanar ground overlay; snow follows actual upward surface normals.
        if (_snowLayerR != null) _snowLayerR.enabled=false;
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
        main.startSize = new ParticleSystem.MinMaxCurve(.045f,.075f);
        main.startColor = new Color(.88f,.93f,.98f,.65f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 4800;
        var em = ps.emission; em.rateOverTime = 0f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(24f, 2f, 24f);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-1.6f);
        vel.y = new ParticleSystem.MinMaxCurve(-16f);
        vel.z = new ParticleSystem.MinMaxCurve(0f);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.lengthScale = 10f;
        r.minParticleSize=.0014f;r.maxParticleSize=.035f;
        r.velocityScale = 0f;
        _rainMat = new Material(Resources.Load<Shader>("Shaders/Precipitation")){name="ReadableRain"};
        _rainMat.mainTexture=PrecipitationTexture(false);
        r.material = _rainMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        var collision=ps.collision;collision.enabled=true;collision.type=ParticleSystemCollisionType.World;collision.mode=ParticleSystemCollisionMode.Collision3D;collision.quality=ParticleSystemCollisionQuality.Medium;collision.enableDynamicColliders=false;collision.lifetimeLoss=1;collision.radiusScale=.3f;
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
        main.startLifetime = 7.8f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(.12f,.26f);
        main.startColor = new Color(1f,1f,1f,.94f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 9000;
        var em = ps.emission; em.rateOverTime = 0f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(24f, 7f, 24f);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0.45f);
        vel.y = new ParticleSystem.MinMaxCurve(-1.3f);
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
        r.minParticleSize=.0016f;r.maxParticleSize=.020f;
        _snowMat = new Material(Resources.Load<Shader>("Shaders/Precipitation")){name="ReadableSnow"};
        _snowMat.mainTexture=PrecipitationTexture(true);
        r.material = _snowMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        ps.Play();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        return ps;
    }
    static Texture2D PrecipitationTexture(bool snow)
    {
        int w=snow?64:16,h=64;var tex=new Texture2D(w,h,TextureFormat.RGBA32,true){name=snow?"SoftSixArmSnow":"SoftRainFilament",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            float px=(x+.5f)/w*2-1,py=(y+.5f)/h*2-1,alpha;
            if(snow)
            {float r=Mathf.Sqrt(px*px+py*py),a=Mathf.Atan2(py,px);float core=Mathf.Exp(-r*r*13);float arm=Mathf.Pow(Mathf.Max(0,Mathf.Cos(a*6)),8)*Mathf.Exp(-r*r*6);alpha=Mathf.Clamp01(core+arm*.16f)*Mathf.Clamp01((1-r)*4);}
            else alpha=Mathf.Exp(-px*px*14)*Mathf.Pow(Mathf.Max(0,1-py*py),1.5f);
            tex.SetPixel(x,y,new Color(1,1,1,alpha));
        }
        tex.Apply();return tex;
    }
}
