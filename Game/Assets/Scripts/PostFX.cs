using UnityEngine;

// 后处理栈驱动（Built-in RP，无第三方包）：泛光（双级高斯）+ 移轴景深 + 色彩分级 + 暗角
// 由 WorldBuilder 运行时挂到主相机；TimeOfDay 逐帧调整泛光/暗角强度（夜晚更亮）
[RequireComponent(typeof(Camera))]
public class PostFX : MonoBehaviour
{
    public float BloomIntensity = 0.45f;
    public float VigStrength = 0.70f;
    public float Threshold = 0.72f;
    public float DofMax = 0.7f;

    Material _mat;

    void OnEnable()
    {
        var holder = Resources.Load<Material>("mat_holder/Campus_PostFX");
        var sh = holder != null ? holder.shader : Shader.Find("Campus/PostFX");
        if (sh == null) { Debug.LogWarning("[PostFX] 找不到 Campus/PostFX，已禁用"); enabled = false; return; }
        _mat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
    }

    void OnDisable()
    {
        if (_mat != null) DestroyImmediate(_mat);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (_mat == null) { Graphics.Blit(src, dst); return; }
        int w = Mathf.Max(4, src.width / 2), h = Mathf.Max(4, src.height / 2);

        // 泛光：亮度预过滤 → 两轮可分离模糊（半分辨率，性能友好）
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

        // 景深模糊源：三轮模糊（移轴用）
        var blurA = RenderTexture.GetTemporary(w, h, 0, src.format);
        var blurB = RenderTexture.GetTemporary(w, h, 0, src.format);
        Graphics.Blit(src, blurA);
        for (int i = 0; i < 3; i++)
        {
            _mat.SetVector("_Dir", new Vector4(1.7f, 0f, 0, 0));
            Graphics.Blit(blurA, blurB, _mat, 1);
            _mat.SetVector("_Dir", new Vector4(0f, 1.7f, 0, 0));
            Graphics.Blit(blurB, blurA, _mat, 1);
        }

        // 合成
        _mat.SetTexture("_BloomTex", bloomA);
        _mat.SetTexture("_BlurTex", blurA);
        _mat.SetFloat("_BloomIntensity", BloomIntensity);
        _mat.SetFloat("_VigStrength", 1f - VigStrength);
        _mat.SetFloat("_DofMax", DofMax);
        Graphics.Blit(src, dst, _mat, 2);

        RenderTexture.ReleaseTemporary(bloomA); RenderTexture.ReleaseTemporary(bloomB);
        RenderTexture.ReleaseTemporary(blurA); RenderTexture.ReleaseTemporary(blurB);
    }
}
