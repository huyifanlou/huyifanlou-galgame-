// Continuous HDR grading, restrained bloom and bilateral contact occlusion. Shader name retained for existing materials.
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
                if(any(c!=c))c=0;c=clamp(c,0,32);
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
            sampler2D _AOTex;float _AOStrength,_AODebug;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float _BloomIntensity, _FocusY, _FocusBand, _FocusFalloff, _DofMax;
            float _Exposure, _Saturation, _Contrast, _VigInner, _VigOuter, _VigStrength;
            float _Posterize, _PosterizeSteps;
            float4 _Tint;

            float4 frag(v2f_img i) : SV_Target
            {
                float4 src = tex2D(_MainTex, i.uv);
                float ao=tex2D(_AOTex,i.uv).r;
                if(_AODebug>.5)return float4(ao,ao,ao,1);
                float3 bloom = tex2D(_BloomTex, i.uv).rgb;
                float3 blur = tex2D(_BlurTex, i.uv).rgb;

                // 移轴景深（默认关闭）
                float d = abs(i.uv.y - _FocusY);
                float dof = smoothstep(_FocusBand, _FocusBand + _FocusFalloff, d) * _DofMax;
                float3 col = lerp(src.rgb, blur, dof);
                col*=lerp(1,ao,_AOStrength);

                // 泛光：卡通渲染里灯光晕开是「软糖感」的来源
                col += bloom * _BloomIntensity;

                // 曝光（不做 ACES —— 卡通要保色）
                col *= _Exposure;
                col=max(0,col);col=saturate(col/(1+max(0,col-.55)*.85));

                // 饱和度 / 对比度
                float l = dot(col, float3(0.2126, 0.7152, 0.0722));
                col = lerp(l.xxx, col, _Saturation);
                col = (col - 0.5) * _Contrast + 0.5;

                // 色调分离：轻度量化出赛璐珞质感（0 = 完全关闭）
                if (_Posterize > 0.001)
                {
                    float3 q = floor(col * _PosterizeSteps + 0.5) / _PosterizeSteps;
                    // 天空需要连续渐变；量化背景会放大成横跨屏幕的弧形色带。
                    float surface = 1.0 - step(0.999, Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv)));
                    col = lerp(col, q, _Posterize * 0.55 * surface);
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
        // Pass 3: stable screen-space contact occlusion.
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragAO
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _CameraDepthNormalsTexture;
            float _AORadius,_AOBias;
            float3 ViewPosition(float2 uv,float depth)
            {float2 ray=(uv*2-1)/float2(unity_CameraProjection._m00,unity_CameraProjection._m11);return float3(ray,-1)*(depth*_ProjectionParams.z);}
            float4 fragAO(v2f_img i):SV_Target
            {
                float depth;float3 n;DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,i.uv),depth,n);
                if(depth>.999)return 1;
                float3 p=ViewPosition(i.uv,depth);float eye=max(depth*_ProjectionParams.z,.1);
                float radius=min(.10,_AORadius*unity_CameraProjection._m11/(eye*2));float ao=0;
                [unroll]for(int k=0;k<12;k++)
                {
                    float angle=k*2.399963;float2 offset=float2(cos(angle),sin(angle))*radius*sqrt((k+.5)/12.0);offset.x*=_ScreenParams.y/_ScreenParams.x;
                    float2 uv=saturate(i.uv+offset);float sd;float3 sn;DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,uv),sd,sn);
                    float3 delta=ViewPosition(uv,sd)-p;float distance=length(delta);
                    float horizon=max(0,dot(n,delta)/max(distance,.0001)-_AOBias);
                    ao+=horizon*saturate(1-distance/_AORadius);
                }
                float value=saturate(1-ao/12*3.2);return float4(value,value,value,1);
            }
            ENDCG
        }
        // Pass 4: depth/normal aware bilateral filtering.
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragBilateral
            #include "UnityCG.cginc"
            sampler2D _MainTex,_CameraDepthNormalsTexture;float4 _MainTex_TexelSize;float2 _Dir;
            float4 fragBilateral(v2f_img i):SV_Target
            {
                float d;float3 n;DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,i.uv),d,n);
                float sum=tex2D(_MainTex,i.uv).r*2,weight=2;
                [unroll]for(int k=-2;k<=2;k++)
                {
                    if(k==0)continue;float2 uv=i.uv+_MainTex_TexelSize.xy*_Dir*k;float sd;float3 sn;DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,uv),sd,sn);
                    float w=exp(-abs(sd-d)*_ProjectionParams.z*8)*pow(saturate(dot(sn,n)),8)/(1+abs(k));
                    sum+=tex2D(_MainTex,uv).r*w;weight+=w;
                }
                float value=sum/weight;return float4(value,value,value,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
