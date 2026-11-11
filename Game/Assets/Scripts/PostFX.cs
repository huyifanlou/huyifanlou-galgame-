using UnityEngine;

// ============================================================================
// 卡通渲染后处理栈（Built-in RP，无第三方包）
// 2026-11-10 美术重做：取代原来的写实栈（SSAO + ACES 色调映射 + 移轴景深）
//
// 卡通渲染的后处理和写实是反着来的：
//   · 不要 SSAO —— 卡通暗面已经由 ramp 阶梯化表达，再加 AO 会让暗部变脏
//   · 不要 ACES —— 电影色调映射会压平颜色，卡通要的是鲜艳、通透、高饱和
//   · 不要移轴景深 —— 除非要做 HD-2D 的微缩模型感（那要单独开）
//   · 要的是：泛光（灯光晕开）+ 色彩量化（可选，做「赛璐珞」质感）+ 饱和度/对比度
//
// Pass 0：HDR 亮度预过滤（泛光源）
// Pass 1：可分离高斯模糊
// Pass 2：最终合成 —— 泛光 + 饱和度 + 对比度 + 色调分离 + 暗角
// ============================================================================
[RequireComponent(typeof(Camera))]
public class PostFX : MonoBehaviour
{
    [Header("泛光")]
    public float BloomIntensity = 0.30f;
    public float Threshold = 0.95f;    // 卡通渲染：让受光面也开始微微泛光，画面更"透"

    [Header("色彩")]
    public float Saturation = 1.18f;   // 卡通要鲜艳
    public float Contrast = 1.06f;
    public float Exposure = 1.02f;

    [Header("色调分离（赛璐珞质感，0=关闭）")]
    [Range(0f, 1f)] public float Posterize = 0.35f;
    [Range(2f, 32f)] public float PosterizeSteps = 12f;

    [Header("暗角")]
    public float VigStrength = 0.14f;

    [Header("移轴景深（HD-2D 微缩感，默认关闭）")]
    public float DofMax = 0f;

    // 兼容旧接口（TimeOfDay 仍在写这两个字段，语义保留）
    public float AOSStrength = 0f;
    public float AORadius = 5.0f;
    public float AOThreshold = 0.55f;

    Material _mat;

    void OnEnable()
    {
        var holder = Resources.Load<Material>("mat_holder/Campus_ToonPostFX");
        var sh = holder != null ? holder.shader : Shader.Find("Campus/ToonPostFX");
        if (sh == null) { Debug.LogWarning("[PostFX] 找不到 Campus/ToonPostFX，已禁用"); enabled = false; return; }
        _mat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
        var cam = GetComponent<Camera>();
        if (cam != null) cam.depthTextureMode |= DepthTextureMode.Depth;
    }

    void OnDisable()
    {
        if (_mat != null) DestroyImmediate(_mat);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (_mat == null) { Graphics.Blit(src, dst); return; }
        int w = Mathf.Max(4, src.width / 2), h = Mathf.Max(4, src.height / 2);

        // 泛光：亮度预过滤 → 两轮可分离模糊（半分辨率）
        var bloomA = RenderTexture.GetTemporary(w, h, 0, src.format);
        var bloomB = RenderTexture.GetTemporary(w, h, 0, src.format);
        _mat.SetFloat("_Threshold", Threshold);
        Graphics.Blit(src, bloomA, _mat, 0);
        for (int i = 0; i < 2; i++)
        {
            _mat.SetVector("_Dir", new Vector4(1f, 0f, 0, 0));
            Graphics.Blit(bloomA, bloomB, _mat, 1);
            _mat.SetVector("_Dir", new Vector4(0f, 1f, 0, 0));
            Graphics.Blit(bloomB, bloomA, _mat, 1);
        }

        // 移轴景深：默认关闭（卡通渲染不用微缩感）
        RenderTexture blurA = null, blurB = null;
        if (DofMax > 0.01f)
        {
            blurA = RenderTexture.GetTemporary(w, h, 0, src.format);
            blurB = RenderTexture.GetTemporary(w, h, 0, src.format);
            Graphics.Blit(src, blurA);
            for (int i = 0; i < 3; i++)
            {
                _mat.SetVector("_Dir", new Vector4(1.7f, 0f, 0, 0));
                Graphics.Blit(blurA, blurB, _mat, 1);
                _mat.SetVector("_Dir", new Vector4(0f, 1.7f, 0, 0));
                Graphics.Blit(blurB, blurA, _mat, 1);
            }
            _mat.SetTexture("_BlurTex", blurA);
        }
        else _mat.SetTexture("_BlurTex", src);

        // 最终合成：所有 uniform 必须在 Blit 前设置
        _mat.SetTexture("_BloomTex", bloomA);
        _mat.SetFloat("_BloomIntensity", BloomIntensity);
        _mat.SetFloat("_VigStrength", 1f - VigStrength);
        _mat.SetFloat("_DofMax", DofMax > 0.01f ? DofMax : 0f);
        _mat.SetFloat("_Saturation", Saturation);
        _mat.SetFloat("_Contrast", Contrast);
        _mat.SetFloat("_Exposure", Exposure);
        _mat.SetFloat("_Posterize", Posterize);
        _mat.SetFloat("_PosterizeSteps", PosterizeSteps);
        Graphics.Blit(src, dst, _mat, 2);

        RenderTexture.ReleaseTemporary(bloomA); RenderTexture.ReleaseTemporary(bloomB);
        if (blurA != null) RenderTexture.ReleaseTemporary(blurA);
        if (blurB != null) RenderTexture.ReleaseTemporary(blurB);
    }
}
