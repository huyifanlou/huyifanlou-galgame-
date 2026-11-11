using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// 材质 / 贴图库（2026-11-10 卡通渲染重做）
//
// 唯一材质出口：全工程（世界/室内/人物/UI）都从这里拿材质，所以只要这个文件改，
// 整个游戏的美术语言就统一变成卡通渲染。这是本轮「从 0 重做美术风格」的技术支点。
//
// 与上一版（Standard + 写实后处理）的区别：
//   · 全部改为 Shader "Campus/Toon"：3 阶 ramp、冷调暗面、硬边高光、反向外扩描边
//   · 不再有 _Metallic / _Glossiness（卡通渲染不用 PBR 参数），改成 _SpecularStrength
//   · 自发光统一走 _EmissionColor + _EMISSION 关键字（TimeOfDay 夜里开关）
//   · 新增 ToonProp / ToonOutline 等语义化入口，让世界构建器能按「这是什么东西」选参数
//
// 过滤策略保持上一版（像素风的锐利感靠 Point 过滤 + 像素贴图本身）：
//   大面积贴地贴面 → Trilinear + mip（远处不闪）
//   天空/精灵/透明贴片 → Point
// ============================================================================
public static class MatLib
{
    static readonly Dictionary<string, Texture2D> Cache = new();

    // 卡通渲染全局参数（可被 MatLib 各入口覆写）
    public static Color DefaultOutline = new Color(0.13f, 0.10f, 0.18f, 1f);
    public static Color ShadowTint = new Color(0.52f, 0.55f, 0.78f, 1f);

    // 描边默认近远距离（2026-11-10 第二轮新增，借鉴 scss 的做法）
    // 近处收细避免贴脸过粗，远处淡出避免远景糊成黑片
    public const float OutlineNearDefault = 0.5f;
    public const float OutlineFarDefault = 55f;

    // 共享的「全白」遮罩贴图（默认不遮任何部位，省一张贴图）
    static Texture2D _whiteMask;
    static Texture2D WhiteMask
    {
        get
        {
            if (_whiteMask == null)
            {
                _whiteMask = new Texture2D(4, 4, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
                var px = new Color[16];
                for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                _whiteMask.SetPixels(px);
                _whiteMask.Apply(false, false);
            }
            return _whiteMask;
        }
    }

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

    // ── 卡通渲染材质工厂 ──

    static Shader ToonShader
    {
        get
        {
            // 优先用 Resources 里的占位材质（保证 shader 被打进构建，见 Bootstrap.SetupProject）
            var holder = Resources.Load<Material>("mat_holder/Campus_Toon");
            var sh = holder != null ? holder.shader : Shader.Find("Campus/Toon");
            if (sh == null) Debug.LogError("[MatLib] 找不到 Campus/Toon，回退 Standard");
            return sh;
        }
    }

    static Material NewToon()
    {
        var sh = ToonShader;
        var m = sh != null && sh.name == "Campus/Toon"
            ? new Material(sh)
            : new Material(Shader.Find("Standard"));
        if (m.shader.name == "Campus/Toon")
        {
            m.SetColor("_ShadowTint", ShadowTint);
            m.SetColor("_OutlineColor", DefaultOutline);
            m.SetFloat("_OutlineWidth", 0.0035f);
            m.SetFloat("_OutlineNearDistance", OutlineNearDefault);
            m.SetFloat("_OutlineFarDistance", OutlineFarDefault);
            m.SetTexture("_OutlineMask", WhiteMask);
        }
        return m;
    }

    // 设置描边的近远距离（世界物体用默认值，人物/近景道具可以收紧）
    public static void SetOutlineRange(Material m, float near, float far)
    {
        if (m == null || m.shader.name != "Campus/Toon") return;
        m.SetFloat("_OutlineNearDistance", near);
        m.SetFloat("_OutlineFarDistance", far);
    }

    // 设置描边遮罩（逐部位控制描边强弱；贴图 R 通道 = 强度）
    public static void SetOutlineMask(Material m, Texture2D mask)
    {
        if (m == null || m.shader.name != "Campus/Toon") return;
        m.SetTexture("_OutlineMask", mask != null ? mask : WhiteMask);
    }

    // 开启「顶点色 R 通道当描边宽度倍率」
    // 程序化网格可以在生成时给薄片顶点标低值 → 薄片描边自动收细，解决外扩描边在薄片上消失/糊边的问题
    public static void EnableOutlineVertexColor(Material m, bool on)
    {
        if (m == null || m.shader.name != "Campus/Toon") return;
        if (on) m.EnableKeyword("_OUTLINE_VERTEXCOLOR");
        else m.DisableKeyword("_OUTLINE_VERTEXCOLOR");
    }

    // 受光卡通材质（世界建筑/地面/道具的主力入口）
    public static Material Lit(string texName, float tilingU = 1f, float tilingV = 1f, bool pixel = false)
    {
        var m = NewToon();
        m.mainTexture = Tex(texName, pixel, true);
        m.mainTextureScale = new Vector2(tilingU, tilingV);
        ApplySpecular(m, texName);
        return m;
    }

    // 卡通渲染的「材质性格」：给不同表面不同的高光强度与描边宽度
    // （上一版这里叫 SmoothnessOf 走 PBR 粗糙度，语义已变，函数名同步改掉）
    static void ApplySpecular(Material m, string tex)
    {
        float spec = 0.0f;
        float outline = 0.0035f;
        if (tex == "pond") { spec = 0.75f; outline = 0f; }                    // 水面：强镜面
        else if (tex == "water") { spec = 0.55f; outline = 0f; }              // 河水
        else if (tex == "bridge_stone") { spec = 0.05f; outline = 0.0030f; }  // 桥体条石
        else if (tex == "wood") { spec = 0.04f; outline = 0.0030f; }          // 廊桥木板
        else if (tex == "clock") { spec = 0.30f; outline = 0.0026f; }         // 钟面（金属感）
        else if (tex.StartsWith("road_")) { spec = 0.06f; outline = 0f; }     // 石板路：不描边（大面积）
        else if (tex == "path" || tex.StartsWith("roadband")) { spec = 0.04f; outline = 0f; }
        else if (tex == "door") { spec = 0.18f; outline = 0.0032f; }          // 漆面门
        else if (tex == "villa") { spec = 0.05f; outline = 0.0030f; }
        else if (tex.StartsWith("roof_")) { spec = 0.16f; outline = 0.0032f; } // 瓦面
        else if (tex.StartsWith("wall_") || tex.StartsWith("wing_") || tex.StartsWith("dist_"))
        { spec = 0.03f; outline = 0.0035f; }
        else if (tex == "track" || tex.StartsWith("court_")) { spec = 0.05f; outline = 0f; }
        else if (tex == "concrete") { spec = 0.03f; outline = 0f; }
        else if (tex == "awning") { spec = 0.06f; outline = 0.0030f; }
        else if (tex == "bark") { spec = 0.02f; outline = 0.0030f; }
        else if (tex == "leaves") { spec = 0.0f; outline = 0.0026f; }
        // 草地/泥土/花这类大面积地形不描边，否则远景会出现成片的黑色网格
        else if (tex == "grass" || tex == "dirt" || tex == "flower" || tex == "snow_ground") { outline = 0f; }

        if (m.shader.name == "Campus/Toon")
        {
            m.SetFloat("_SpecularStrength", spec);
            m.SetFloat("_OutlineWidth", outline);
            m.SetFloat("_SpecularSize", tex == "pond" || tex == "water" ? 0.06f : 0.20f);
        }
        else if (m.HasProperty("_Glossiness"))
        {
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Glossiness", spec);
        }
    }

    // 平滑着色的金属件（灯杆/栏杆）：高光更强，卡通金属反光很干脆
    public static Material Metal(string hex)
    {
        var m = LitColor(Hex(hex));
        if (m.shader.name == "Campus/Toon")
        {
            m.SetFloat("_SpecularStrength", 0.45f);
            m.SetFloat("_SpecularSize", 0.10f);
            m.SetFloat("_RimStrength", 0.30f);
            m.SetFloat("_OutlineWidth", 0.0030f);
        }
        return m;
    }

    // 纯色卡通材质（世界构建器里绝大多数色块都走这里）
    public static Material LitColor(Color c, float specular = 0f)
    {
        var m = NewToon();
        m.color = c;
        if (m.shader.name == "Campus/Toon")
        {
            m.SetFloat("_SpecularStrength", specular);
            m.SetFloat("_OutlineWidth", 0.0035f);
        }
        else
        {
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Glossiness", specular);
        }
        return m;
    }

    // 需要更强描边的物体（主角/NPC/近景道具）：描边更粗、更黑
    public static Material LitColorBold(Color c, float outline = 0.0055f)
    {
        var m = LitColor(c);
        if (m.shader.name == "Campus/Toon")
        {
            m.SetFloat("_OutlineWidth", outline);
            m.SetColor("_OutlineColor", new Color(0.09f, 0.07f, 0.13f, 1f));
        }
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

    // 像素小人精灵：卡通材质 + Cutout（alphaTest 0.4）；UV 由 mainTextureScale/Offset 驱动帧动画
    public static Material Sprite(string texName, float smoothness = 0f, float emissive = 0f)
    {
        var m = NewToon();
        m.mainTexture = Tex(texName, pixel: true, repeat: true);
        if (m.shader.name == "Campus/Toon")
        {
            m.SetFloat("_SpecularStrength", 0f);
            m.SetFloat("_OutlineWidth", 0f);        // 精灵不用反向外扩描边（会沿 UV 边界炸开）
            m.SetFloat("_RimStrength", 0.10f);
        }
        else
        {
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Glossiness", smoothness);
        }
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

    // 受光半透明（雪层这类叠在地形上的大面积薄层）
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

    // 大面片的不透明裁切（操场跑道这类「只画跑道面、其余透出地形草」的贴片）
    public static Material Cutout(string texName, float smoothness = 0f)
    {
        var m = NewToon();
        m.mainTexture = Tex(texName, pixel: false, repeat: true);
        if (m.shader.name == "Campus/Toon")
        {
            m.SetFloat("_OutlineWidth", 0f);
            m.SetFloat("_SpecularStrength", 0f);
        }
        else
        {
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Glossiness", smoothness);
        }
        ConfigureCutout(m);
        return m;
    }

    // 直接用 Texture2D 建材质（程序化参数化贴图：石拱桥每跨面片 等）
    public static Material LitTexture(Texture2D t, float tu = 1f, float tv = 1f, float smoothness = 0f)
    {
        var m = NewToon();
        m.mainTexture = t;
        m.mainTextureScale = new Vector2(tu, tv);
        if (m.shader.name == "Campus/Toon")
        {
            m.SetFloat("_SpecularStrength", smoothness);
            m.SetFloat("_OutlineWidth", 0.0032f);
        }
        else
        {
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Glossiness", smoothness);
        }
        return m;
    }

    public static Material CutoutTexture(Texture2D t, float tu = 1f, float tv = 1f, float smoothness = 0f)
    {
        var m = LitTexture(t, tu, tv, smoothness);
        if (m.shader.name == "Campus/Toon") m.SetFloat("_OutlineWidth", 0f);
        ConfigureCutout(m);
        return m;
    }

    // Cutout 模式（alphaTest 0.4）：卡通 shader 用 _ALPHATEST_ON 关键字 + _Cutoff；
    // Standard 需要 _Mode/_AlphaClip 一整套（构建里没有该变体时会静默失效 → 透明区渲染成黑块）
    static void ConfigureCutout(Material m)
    {
        m.EnableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Cutoff", 0.4f);
        if (m.shader.name == "Campus/Toon")
        {
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            m.SetInt("_ZWrite", 1);
            m.renderQueue = 2450;
            return;
        }
        m.SetFloat("_Mode", 1f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        m.SetInt("_ZWrite", 1);
        m.DisableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
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

    // ── 卡通渲染专用的全局参数（TimeOfDay 每帧写入 shader 全局变量）──
    public static void SetGlobals(Color ambient, float sunPower)
    {
        Shader.SetGlobalColor("_ToonAmbient", ambient);
        Shader.SetGlobalFloat("_ToonSunPower", sunPower);
    }
}
