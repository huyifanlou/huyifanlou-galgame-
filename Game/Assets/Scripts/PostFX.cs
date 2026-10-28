using UnityEngine;

// 写实向后处理栈（Built-in RP，无第三方包）：
//   泛光（HDR 线性预过滤 + 双级高斯）
//   SSAO（半分辨率深度采样 + 模糊）
//   ACES 色调映射 + 曝光
//   色彩分级 + 暗角
//   移轴景深默认关闭（写实模式不用微缩感）
// 由 WorldBuilder 运行时挂到主相机；TimeOfDay 可微调泛光/暗角/曝光。
[RequireComponent(typeof(Camera))]
public class PostFX : MonoBehaviour
{
    public float BloomIntensity = 0.08f;
    public float VigStrength = 0.18f;
    public float Threshold = 1.10f;   // 线性 HDR：只让 >1 的灯/窗户泛光
    public float DofMax = 0f;         // 写实模式关闭移轴景深；>0 时保留原有效果
    public float Saturation = 0.94f;
    public float Contrast = 1.00f;
    public float Exposure = 1.00f;
    public float AOSStrength = 0.30f; // 0 = 关闭 SSAO
    public float AORadius = 5.0f;     // AO 半分辨率像素
    public float AOThreshold = 0.55f; // 深度差阈值（米）

    Material _mat;

    void OnEnable()
    {
        var holder = Resources.Load<Material>("mat_holder/Campus_PostFX");
        var sh = holder != null ? holder.shader : Shader.Find("Campus/PostFX");
        if (sh == null) { Debug.LogWarning("[PostFX] 找不到 Campus/PostFX，已禁用"); enabled = false; return; }
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

        // SSAO：半分辨率深度 → 8 抽头邻域 → 两轮模糊
        RenderTexture ao = null, aoB = null;
        if (AOSStrength > 0.001f)
        {
            ao = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.R8);
            aoB = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.R8);
            _mat.SetVector("_AOTexel", new Vector4(1f / w, 1f / h, 0, 0));
            _mat.SetFloat("_AORadius", AORadius);
            _mat.SetFloat("_AOThreshold", AOThreshold);
            _mat.SetFloat("_AOStrength", AOSStrength);
            Graphics.Blit(src, ao, _mat, 3);
            for (int i = 0; i < 2; i++)
            {
                _mat.SetVector("_Dir", new Vector4(1f, 0f, 0, 0));
                Graphics.Blit(ao, aoB, _mat, 1);
                _mat.SetVector("_Dir", new Vector4(0f, 1f, 0, 0));
                Graphics.Blit(aoB, ao, _mat, 1);
            }
            _mat.SetTexture("_AOTex", ao);
        }
        else _mat.SetTexture("_AOTex", Texture2D.whiteTexture);

        // 移轴景深：写实模式默认跳过（省 3 轮模糊）
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
        Graphics.Blit(src, dst, _mat, 2);

        RenderTexture.ReleaseTemporary(bloomA); RenderTexture.ReleaseTemporary(bloomB);
        if (ao != null) RenderTexture.ReleaseTemporary(ao);
        if (aoB != null) RenderTexture.ReleaseTemporary(aoB);
        if (blurA != null) RenderTexture.ReleaseTemporary(blurA);
        if (blurB != null) RenderTexture.ReleaseTemporary(blurB);
    }
}
