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

    // Lambert 风格的受光材质（Standard：金属 0 / 光滑 0 → 近似漫反射）
    public static Material Lit(string texName, float tilingU = 1f, float tilingV = 1f, bool pixel = false)
    {
        var m = new Material(Shader.Find("Standard"));
        m.mainTexture = Tex(texName, pixel, true);
        m.mainTextureScale = new Vector2(tilingU, tilingV);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Glossiness", 0f);
        return m;
    }

    public static Material LitColor(Color c)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Glossiness", 0f);
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
    public static Material Sprite(string texName)
    {
        var m = new Material(Shader.Find("Standard"));
        m.mainTexture = Tex(texName, pixel: true, repeat: true);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Glossiness", 0f);
        // Cutout 模式
        m.SetFloat("_Mode", 1f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        m.SetInt("_ZWrite", 1);
        m.EnableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.SetFloat("_Cutoff", 0.4f);
        m.SetFloat("_AlphaClip", 1f);   // 2021+ 的 Standard 还需要 AlphaClip 开关才真正裁剪
        m.renderQueue = 2450;
        return m;
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
}
