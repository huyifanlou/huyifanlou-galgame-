// 校园写实后处理栈（Built-in RP 图像效果，无第三方包）
// Pass 0：HDR 亮度预过滤（泛光源）
// Pass 1：可分离高斯模糊（_Dir 控制方向；也用于 AO 模糊）
// Pass 2：最终合成——泛光 + SSAO + ACES 色调映射 + 色彩分级 + 暗角
// Pass 3：SSAO（半分辨率深度采样，8 抽头）
Shader "Campus/PostFX"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _BloomTex ("Bloom", 2D) = "black" {}
        _BlurTex ("Blur", 2D) = "black" {}
        _AOTex ("AO", 2D) = "white" {}
        _Threshold ("Bloom Threshold", Range(0, 4)) = 1.05
        _BloomIntensity ("Bloom Intensity", Range(0, 2)) = 0.10
        _FocusY ("Focus Center (uv.y)", Range(0, 1)) = 0.52
        _FocusBand ("Focus Band", Range(0, 0.5)) = 0.16
        _FocusFalloff ("Focus Falloff", Range(0.01, 0.6)) = 0.3
        _DofMax ("DOF Strength", Range(0, 1)) = 0
        _Exposure ("Exposure", Range(0.2, 3)) = 1.15
        _Saturation ("Saturation", Range(0, 2)) = 1.03
        _Contrast ("Contrast", Range(0.5, 2)) = 1.03
        _Tint ("Warm Tint", Color) = (1.01, 1.0, 0.985, 1)
        _VigInner ("Vignette Inner", Range(0, 1)) = 0.55
        _VigOuter ("Vignette Outer", Range(0, 2)) = 1.25
        _VigStrength ("Vignette Strength", Range(0, 1)) = 0.78
        _AORadius ("AO Radius (half-res px)", Range(1, 12)) = 4
        _AOThreshold ("AO Depth Threshold (m)", Range(0.05, 2)) = 0.45
        _AOStrength ("AO Strength", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        // ── Pass 0：HDR 亮度预过滤 ──
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Threshold;
            float4 frag(v2f_img i) : SV_Target
            {
                float3 c = tex2D(_MainTex, i.uv).rgb;
                float l = max(max(c.r, c.g), c.b);
                float w = smoothstep(_Threshold, _Threshold + 0.5, l);
                return float4(c * w, 1);
            }
            ENDCG
        }
        // ── Pass 1：可分离高斯（5 抽头，_Dir 由 C# 设置）──
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float2 _Dir;
            float4 frag(v2f_img i) : SV_Target
            {
                float2 t = _MainTex_TexelSize.xy * _Dir;
                float4 c = tex2D(_MainTex, i.uv) * 0.2270270270;
                c += tex2D(_MainTex, i.uv + t * 1.3846153846) * 0.3162162162;
                c += tex2D(_MainTex, i.uv - t * 1.3846153846) * 0.3162162162;
                c += tex2D(_MainTex, i.uv + t * 3.2307692308) * 0.0702702703;
                c += tex2D(_MainTex, i.uv - t * 3.2307692308) * 0.0702702703;
                return c;
            }
            ENDCG
        }
        // ── Pass 2：最终合成（线性 HDR → ACES → 显示空间分级）──
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            sampler2D _BloomTex;
            sampler2D _BlurTex;
            sampler2D _AOTex;
            float _BloomIntensity, _FocusY, _FocusBand, _FocusFalloff, _DofMax;
            float _Exposure, _Saturation, _Contrast, _VigInner, _VigOuter, _VigStrength;
            float4 _Tint;

            // ACES 近似（Narkowicz）：压高光、保中间调，比 simple Reinhard 更“电影”
            float3 ACESFilm(float3 x)
            {
                const float a = 2.51, b = 0.03, c = 2.43, d = 0.59, e = 0.14;
                return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
            }

            float4 frag(v2f_img i) : SV_Target
            {
                float4 src = tex2D(_MainTex, i.uv);
                float3 bloom = tex2D(_BloomTex, i.uv).rgb;
                float3 blur = tex2D(_BlurTex, i.uv).rgb;
                float d = abs(i.uv.y - _FocusY);
                float dof = smoothstep(_FocusBand, _FocusBand + _FocusFalloff, d) * _DofMax;
                float3 col = lerp(src.rgb, blur, dof);
                col += bloom * _BloomIntensity;
                // SSAO 在色调映射前乘，暗部更自然
                float ao = tex2D(_AOTex, i.uv).r;
                col *= ao;
                // 线性 HDR → ACES
                col = ACESFilm(col * _Exposure);
                // 显示空间分级
                float l = dot(col, float3(0.2126, 0.7152, 0.0722));
                col = lerp(l.xxx, col, _Saturation);
                col = (col - 0.5) * _Contrast + 0.5;
                col *= _Tint.rgb;
                float2 q = i.uv - 0.5;
                float vig = smoothstep(_VigOuter, _VigInner, length(q) * 1.4142);
                col *= lerp(_VigStrength, 1.0, vig);
                return float4(saturate(col), src.a);
            }
            ENDCG
        }
        // ── Pass 3：SSAO（半分辨率，深度差）──
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float4 _AOTexel;
            float _AORadius, _AOThreshold, _AOStrength;
            float4 frag(v2f_img i) : SV_Target
            {
                float d = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv));
                float occ = 0;
                [unroll] for (int k = 0; k < 8; k++)
                {
                    float a = k * 2.39996323;
                    float r = sqrt((k + 0.5) / 8.0) * _AORadius;
                    float2 o = float2(cos(a), sin(a)) * r * _AOTexel.xy;
                    float sd = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv + o));
                    float diff = d - sd;
                    occ += saturate((diff - 0.04) / max(_AOThreshold, 0.001)) * step(0.1, sd);
                }
                occ /= 8.0;
                float fade = saturate(1.0 - d / 180.0);
                float ao = 1.0 - occ * _AOStrength * fade;
                return float4(ao, ao, ao, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
