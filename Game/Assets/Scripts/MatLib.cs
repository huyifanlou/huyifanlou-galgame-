using System.Collections.Generic;
using UnityEngine;

// 材质 / 贴图库：Resources/tex/*.png 运行时加载 + 运行时过滤设置
// 过滤策略（对齐 web 版观感，首屏后按截图迭代）：
//   Ground/Wall/Roof 等大面积贴地贴面 → Trilinear + 重叠 mip → 远处不闪烁不跳变
//   Sky/Player/Glow/透明贴片 → Point（无 mip）→ 像素锐利
public static class MatLib
{
    static readonly Dictionary<string, Texture2D> Cache = new();

    public static Texture2D Tex(string name, bool pixel = true, bool repeat = true)
    {
        string key = name + (pixel ? "|p" : "|s") + (repeat ? "|r" : "|c");
        if (Cache.TryGetValue(key, out var cached)) return cached;
        // 优先程序化生成（C# TextureLibrary）；招牌等含中文文字者回退 Resources PNG
        var t = TextureLibrary.ByName(name, pixel, repeat);
        if (t == null) t = Resources.Load<Texture2D>("tex/" + name);
        if (t == null) { Debug.LogError("[MatLib] 贴图缺失: " + name); return Texture2D.whiteTexture; }
        t.filterMode = pixel ? FilterMode.Point : FilterMode.Trilinear;
        t.anisoLevel = pixel ? 0 : 9;
        t.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        Cache[key] = t;
        return t;
    }

    // Lambert 风格的受光材质（Standard：金属 0 / 光滑度按材质类型给 → 保留纯漫反射观感，只加一层自然的受光响应）
    public static Material Lit(string texName, float tilingU = 1f, float tilingV = 1f, bool pixel = false)
    {
        var m = new Material(Shader.Find("Standard"));
        m.mainTexture = Tex(texName, pixel, true);
        m.mainTextureScale = new Vector2(tilingU, tilingV);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Glossiness", SmoothnessOf(texName));
        return m;
    }

    // 材质受光响应（写实化）：0=纯漫反射；smoothness 越大高光越紧。
    // 注意不用的金属度：本工程无环境反射源（无天空盒/反射探针），高金属度会发黑。
    public static float SmoothnessOf(string tex)
    {
        if (tex == "pond") return 0.55f;                                   // 水面
        if (tex == "water") return 0.38f;                                  // 河水（湿润但不刺眼）
        if (tex == "bridge_stone") return 0.12f;                           // 桥体条石
        if (tex == "wood") return 0.08f;                                   // 廊桥木板
        if (tex == "clock") return 0.12f;                                  // 钟面
        if (tex.StartsWith("road_")) return 0.13f;                         // 石板路
        if (tex == "path" || tex.StartsWith("roadband")) return 0.10f;     // 铺装/石带
        if (tex == "door") return 0.16f;                                   // 漆面门
        if (tex == "villa") return 0.10f;                                  // 木构/和纸
        if (tex.StartsWith("roof_")) return 0.20f;                         // 瓦面
        if (tex.StartsWith("wall_") || tex.StartsWith("wing_") || tex.StartsWith("dist_")) return 0.05f;
        if (tex == "track" || tex.StartsWith("court_")) return 0.07f;      // 塑胶跑道/球场
        if (tex == "concrete") return 0.06f;
        if (tex == "awning") return 0.10f;
        if (tex == "bark") return 0.04f;
        if (tex == "leaves") return 0.02f;
        return 0.02f;                                                      // 草/土/花等
    }

    // 平滑着色的金属件（灯杆/栏杆）：低金属度 + 较高光滑度 —— 保留漫反射底色，只在受光处出高光
    public static Material Metal(string hex) => LitColor(Hex(hex), 0.34f);

    public static Material LitColor(Color c, float smoothness = 0f)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Glossiness", smoothness);
        return m;
    }

    // 无光照纯色（门灯/石灯笼暖光块）
    public static Material UnlitColor(Color c)
    {
        var m = new Material(Shader.Find("Unlit/Color"));
        m.color = c;
        return m;
    }

    // 无光照贴图（天空穹顶白天层；tint 用于晨昏染色）
    public static Material Unlit(string texName, Color tint, bool clamp = false)
    {
        var m = new Material(Shader.Find("Unlit/Texture"));
        m.mainTexture = Tex(texName, pixel: true, repeat: !clamp);
        m.color = tint;
        return m;
    }

    // 半透明 + 可调色（天空夜晚层交叉淡化；Legacy Particles/Alpha Blended 为无光照 + 无雾 + ZWrite Off）
    public static Material Fade(string texName, Color tint)
    {
        var m = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
        m.mainTexture = Tex(texName, pixel: true, repeat: true);
        m.SetColor("_TintColor", tint);
        return m;
    }

    // 加色混合（路灯地面光池）
    public static Material Additive(string texName, Color tint, bool clamp = true)
    {
        var m = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        m.mainTexture = Tex(texName, pixel: true, repeat: !clamp);
        m.SetColor("_TintColor", tint);
        return m;
    }

    // 像素小人精灵：Standard Cutout（alphaTest 0.4，与 web 一致）；UV 由 mainTextureScale/Offset 驱动帧动画
    public static Material Sprite(string texName, float smoothness = 0f, float emissive = 0f)
    {
        var m = new Material(Shader.Find("Standard"));
        m.mainTexture = Tex(texName, pixel: true, repeat: true);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Glossiness", smoothness);
        ConfigureCutout(m);
        if (emissive > 0f)
        {
            // 角色专用：背光时不至于整张脸糊成黑的（自发光用主贴图调制，不额外提亮颜色）
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(emissive, emissive, emissive));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        return m;
    }

    // 受光半透明（雪层这类叠在地形上的大面积薄层）：Standard Transparent，参与光照与雾
    public static Material Transparent(string texName, float smoothness = 0f, float alpha = 1f)
    {
        var m = new Material(Shader.Find("Standard"));
        m.mainTexture = Tex(texName, pixel: false, repeat: true);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Glossiness", smoothness);
        m.color = new Color(1f, 1f, 1f, alpha);
        m.SetFloat("_Mode", 3f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
        return m;
    }

    // 大面片的不透明裁切    // 大面片的不透明裁切（操场跑道这类「只画跑道面、其余透出地形草」的贴片）：
    // 与 Sprite 同样的 Cutout 配置，但采样用 Trilinear（远处不闪）
    public static Material Cutout(string texName, float smoothness = 0f)
    {
        var m = new Material(Shader.Find("Standard"));
        m.mainTexture = Tex(texName, pixel: false, repeat: true);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Glossiness", smoothness);
        ConfigureCutout(m);
        return m;
    }

    // 直接用 Texture2D 建材质（程序化参数化贴图：石拱桥每跨面片 等）
    public static Material LitTexture(Texture2D t, float tu = 1f, float tv = 1f, float smoothness = 0f)
    {
        var m = new Material(Shader.Find("Standard"));
        m.mainTexture = t;
        m.mainTextureScale = new Vector2(tu, tv);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Glossiness", smoothness);
        return m;
    }

    public static Material CutoutTexture(Texture2D t, float tu = 1f, float tv = 1f, float smoothness = 0f)
    {
        var m = LitTexture(t, tu, tv, smoothness);
        ConfigureCutout(m);
        return m;
    }

    // Standard Cutout 模式（alphaTest 0.4）：2021+ 的 Standard 还需要 _AlphaClip 开关才真正裁剪
    static void ConfigureCutout(Material m)
    {
        m.SetFloat("_Mode", 1f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        m.SetInt("_ZWrite", 1);
        m.EnableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.SetFloat("_Cutoff", 0.4f);
        m.SetFloat("_AlphaClip", 1f);
        m.renderQueue = 2450;
    }

    // 半透明黑色（角色脚下圆影）
    public static Material Shadow(Color c)
    {
        var m = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
        m.color = c;
        return m;
    }

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }

    // 明暗调整：k<1 变暗（背光面）、k>1 提亮（受光面）——树冠双色调用
    public static string Shade(string hex, float k)
    {
        var c = Hex(hex);
        return "#" + ColorUtility.ToHtmlStringRGB(new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k)));
    }
}
