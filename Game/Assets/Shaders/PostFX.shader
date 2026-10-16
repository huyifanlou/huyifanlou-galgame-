// 校园后处理栈（Built-in RP 图像效果，无第三方包）
// Pass 0：亮度预过滤 + 降采样（泛光源）
// Pass 1：可分离高斯模糊（_Dir 控制方向）
// Pass 2：最终合成——源 + 泛光 + 移轴景深 + 色彩分级 + 暗角
Shader "Campus/PostFX"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _BloomTex ("Bloom", 2D) = "black" {}
        _BlurTex ("Blur", 2D) = "black" {}
        _Threshold ("Bloom Threshold", Range(0, 2)) = 0.72
        _BloomIntensity ("Bloom Intensity", Range(0, 2)) = 0.55
        _FocusY ("Focus Center (uv.y)", Range(0, 1)) = 0.52
        _FocusBand ("Focus Band", Range(0, 0.5)) = 0.16
        _FocusFalloff ("Focus Falloff", Range(0.01, 0.6)) = 0.3
        _DofMax ("DOF Strength", Range(0, 1)) = 0.85
        _Saturation ("Saturation", Range(0, 2)) = 1.07
        _Contrast ("Contrast", Range(0.5, 2)) = 1.0
        _Tint ("Warm Tint", Color) = (1.03, 1.0, 0.96, 1)
        _VigInner ("Vignette Inner", Range(0, 1)) = 0.55
        _VigOuter ("Vignette Outer", Range(0, 2)) = 1.25
        _VigStrength ("Vignette Strength", Range(0, 1)) = 0.78
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        // ── Pass 0：亮度预过滤 ──
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Threshold;
            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed3 c = tex2D(_MainTex, i.uv).rgb;
                float l = dot(c, float3(0.299, 0.587, 0.114));
                return fixed4(c * smoothstep(_Threshold, _Threshold + 0.25, l), 1);
            }
            ENDCG
        }
        // ── Pass 1：可分离模糊（5 抽头高斯，_Dir 由 C# 设置）──
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float2 _Dir;
            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 t = _MainTex_TexelSize.xy * _Dir;
                fixed4 c = tex2D(_MainTex, i.uv) * 0.2270270270;
                c += tex2D(_MainTex, i.uv + t * 1.3846153846) * 0.3162162162;
                c += tex2D(_MainTex, i.uv - t * 1.3846153846) * 0.3162162162;
                c += tex2D(_MainTex, i.uv + t * 3.2307692308) * 0.0702702703;
                c += tex2D(_MainTex, i.uv - t * 3.2307692308) * 0.0702702703;
                return c;
            }
            ENDCG
        }
        // ── Pass 2：合成 ──
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
            float _Saturation, _Contrast, _VigInner, _VigOuter, _VigStrength;
            fixed4 _Tint;
            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 src = tex2D(_MainTex, i.uv);
                fixed3 bloom = tex2D(_BloomTex, i.uv).rgb;
                fixed3 blur = tex2D(_BlurTex, i.uv).rgb;
                // 移轴景深：以 _FocusY 为中心带，越远越模糊（微缩质感）
                float d = abs(i.uv.y - _FocusY);
                float dof = smoothstep(_FocusBand, _FocusBand + _FocusFalloff, d) * _DofMax;
                fixed3 col = lerp(src.rgb, blur, dof);
                col += bloom * _BloomIntensity;
                // 色彩分级
                float l = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(l.xxx, col, _Saturation);
                col = (col - 0.5) * _Contrast + 0.5;
                col *= _Tint.rgb;
                // 暗角
                float2 q = i.uv - 0.5;
                float vig = smoothstep(_VigOuter, _VigInner, length(q) * 1.4142);
                col *= lerp(_VigStrength, 1.0, vig);
                return fixed4(max(col, 0), src.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
