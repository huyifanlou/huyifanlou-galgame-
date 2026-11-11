// ============================================================================
// 卡通渲染后处理栈（Built-in RP，无第三方包）
// 2026-11-10 美术重做：取代原来的写实栈（SSAO + ACES 色调映射 + 移轴景深）
//
// 卡通渲染的后处理和写实是反着来的：
//   · 不要 SSAO —— 卡通暗面已由 ramp 阶梯化表达，再加 AO 会让暗部变脏
//   · 不要 ACES —— 电影色调映射会压平颜色，卡通要的是鲜艳、通透、高饱和
//   · 要的是：泛光（灯光晕开）+ 色彩量化（赛璐珞质感）+ 饱和度/对比度 + 暗角
//
// Pass 0：HDR 亮度预过滤（泛光源）
// Pass 1：可分离高斯模糊
// Pass 2：最终合成 —— 泛光 + 饱和度 + 对比度 + 色调分离 + 暗角
// ============================================================================
Shader "Campus/ToonPostFX"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _BloomTex ("Bloom", 2D) = "black" {}
        _BlurTex ("Blur", 2D) = "black" {}
        _Threshold ("Bloom Threshold", Range(0, 4)) = 0.95
        _BloomIntensity ("Bloom Intensity", Range(0, 2)) = 0.30
        _FocusY ("Focus Center (uv.y)", Range(0, 1)) = 0.52
        _FocusBand ("Focus Band", Range(0, 0.5)) = 0.16
        _FocusFalloff ("Focus Falloff", Range(0.01, 0.6)) = 0.3
        _DofMax ("DOF Strength", Range(0, 1)) = 0
        _Exposure ("Exposure", Range(0.2, 3)) = 1.02
        _Saturation ("Saturation", Range(0, 2)) = 1.18
        _Contrast ("Contrast", Range(0.5, 2)) = 1.06
        _Posterize ("Posterize", Range(0, 1)) = 0.35
        _PosterizeSteps ("Posterize Steps", Range(2, 32)) = 12
        _Tint ("Tint", Color) = (1.0, 1.0, 1.0, 1)
        _VigInner ("Vignette Inner", Range(0, 1)) = 0.60
        _VigOuter ("Vignette Outer", Range(0, 2)) = 1.30
        _VigStrength ("Vignette Strength", Range(0, 1)) = 0.86
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
        // ── Pass 2：最终合成（泛光 → 色调分级 → 色调分离 → 暗角）──
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            sampler2D _BloomTex;
            sampler2D _BlurTex;
            float _BloomIntensity, _FocusY, _FocusBand, _FocusFalloff, _DofMax;
            float _Exposure, _Saturation, _Contrast, _VigInner, _VigOuter, _VigStrength;
            float _Posterize, _PosterizeSteps;
            float4 _Tint;

            float4 frag(v2f_img i) : SV_Target
            {
                float4 src = tex2D(_MainTex, i.uv);
                float3 bloom = tex2D(_BloomTex, i.uv).rgb;
                float3 blur = tex2D(_BlurTex, i.uv).rgb;

                // 移轴景深（默认关闭）
                float d = abs(i.uv.y - _FocusY);
                float dof = smoothstep(_FocusBand, _FocusBand + _FocusFalloff, d) * _DofMax;
                float3 col = lerp(src.rgb, blur, dof);

                // 泛光：卡通渲染里灯光晕开是「软糖感」的来源
                col += bloom * _BloomIntensity;

                // 曝光（不做 ACES —— 卡通要保色）
                col *= _Exposure;

                // 饱和度 / 对比度
                float l = dot(col, float3(0.2126, 0.7152, 0.0722));
                col = lerp(l.xxx, col, _Saturation);
                col = (col - 0.5) * _Contrast + 0.5;

                // 色调分离：轻度量化出赛璐珞质感（0 = 完全关闭）
                if (_Posterize > 0.001)
                {
                    float3 q = floor(col * _PosterizeSteps + 0.5) / _PosterizeSteps;
                    col = lerp(col, q, _Posterize * 0.55);
                }

                col *= _Tint.rgb;

                // 暗角
                float2 q = i.uv - 0.5;
                float vig = smoothstep(_VigOuter, _VigInner, length(q) * 1.4142);
                col *= lerp(_VigStrength, 1.0, vig);

                return float4(saturate(col), src.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
