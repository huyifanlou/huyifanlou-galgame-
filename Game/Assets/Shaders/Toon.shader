// ============================================================================
// Campus/Toon —— 《他山之石》卡通渲染（Cel Shading）核心着色器
// 2026-11-10 美术重做：取代原来的 Standard + 写实后处理（SSAO/ACES）路线
//
// 设计要点：
//   1. 光照 ramp 阶梯化（_RampSteps 控制阶数，默认 3 阶）。暗面走 _ShadowTint（偏冷紫）而不是
//      简单变黑 —— 「亮面暖、暗面冷」是卡通渲染最基本的语汇。
//   2. 描边用反向外扩 + clip.w 补偿，屏幕空间宽度恒定，远近一致。
//   3. 硬边高光（step 而非 smooth）+ 边缘光，这是卡通渲染区别于写实的关键。
//   4. 环境光由全局 _ToonAmbient 驱动（TimeOfDay 每帧写入），随昼夜变化。
//   5. 点光源走 ForwardAdd，同样阶梯化并压低 —— 路灯照亮地面但不把画面照"脏"。
//
// 属性命名与 Standard 对齐（_Color / _MainTex / _MainTex_ST / _EmissionColor / _EMISSION 关键字），
// 所以 MatLib 与 TimeOfDay 里原有的 m.color / mainTexture / EnableKeyword("_EMISSION") 都仍然有效。
// ============================================================================
Shader "Campus/Toon"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ShadowTint ("Shadow Tint", Color) = (0.52,0.55,0.78,1)
        _RampSteps ("Ramp Steps", Range(1,6)) = 3
        _RampSoft ("Ramp Softness", Range(0.001,0.5)) = 0.06
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 3.5
        _RimStrength ("Rim Strength", Range(0,2)) = 0.16
        _SpecularStrength ("Specular", Range(0,1)) = 0
        _SpecularSize ("Specular Size", Range(0.02,1)) = 0.22
        _OutlineColor ("Outline Color", Color) = (0.13,0.10,0.18,1)
        _OutlineWidth ("Outline Width", Range(0,0.02)) = 0.0035
        _OutlineMask ("Outline Mask", 2D) = "white" {}
        _OutlineNearDistance ("Outline Near Distance", Float) = 0.5
        _OutlineFarDistance ("Outline Far Distance", Float) = 55
        [Toggle(_OUTLINE_VERTEXCOLOR)] _OutlineVertexColor ("Use Vertex Color as Width", Float) = 0
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGINCLUDE
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"

        sampler2D _MainTex;
        float4 _MainTex_ST;
        fixed4 _Color;
        fixed4 _ShadowTint;
        float _RampSteps;
        float _RampSoft;
        fixed4 _RimColor;
        float _RimPower;
        float _RimStrength;
        float _SpecularStrength;
        float _SpecularSize;
        fixed4 _OutlineColor;
        float _OutlineWidth;
        sampler2D _OutlineMask;
        float _OutlineNearDistance;
        float _OutlineFarDistance;
        fixed4 _EmissionColor;
        float _Cutoff;

        // 全局：TimeOfDay 每帧写入
        fixed4 _ToonAmbient;
        float _ToonSunPower;

        // 阶梯化：把 [0,1] 映射成 N 阶硬边阶梯，soft 控制边界软硬
        float ToonStep(float x, float steps, float soft)
        {
            x = saturate(x);
            float s = x * steps;
            float i = floor(s);
            float f = saturate(s - i);
            f = smoothstep(0.5 - soft, 0.5 + soft, f);
            return saturate((i + f) / steps);
        }

        struct appdata_t
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float2 uv : TEXCOORD0;
        };

        struct v2f_t
        {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 wnormal : TEXCOORD1;
            float3 wpos : TEXCOORD2;
            UNITY_SHADOW_COORDS(3)
            UNITY_FOG_COORDS(4)
        };

        v2f_t vert_t(appdata_t v)
        {
            v2f_t o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            o.wnormal = UnityObjectToWorldNormal(v.normal);
            o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
            UNITY_TRANSFER_SHADOW(o, o.pos);
            UNITY_TRANSFER_FOG(o, o.pos);
            return o;
        }

        fixed4 SampleAlbedo(float2 uv)
        {
            fixed4 a = tex2D(_MainTex, uv) * _Color;
            #ifdef _ALPHATEST_ON
                clip(a.a - _Cutoff);
            #endif
            return a;
        }
        ENDCG

        // ── 主光照 pass ──
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert_t
            #pragma fragment frag_t
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile __ _EMISSION
            #pragma shader_feature _ALPHATEST_ON
            #pragma target 3.0

            fixed4 frag_t(v2f_t i) : SV_Target
            {
                fixed4 albedo = SampleAlbedo(i.uv);

                float3 n = normalize(i.wnormal);
                float3 v = normalize(_WorldSpaceCameraPos - i.wpos);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightCol = _LightColor0.rgb * _ToonSunPower;

                float ramp = ToonStep(dot(n, l), _RampSteps, _RampSoft);
                float lit = ramp * UNITY_SHADOW_ATTENUATION(i, i.wpos);

                float3 cLit    = albedo.rgb * lightCol * lit;
                float3 cShadow = albedo.rgb * _ShadowTint.rgb * lightCol * (1.0 - lit) * 0.60;
                float3 ambient = albedo.rgb * _ToonAmbient.rgb;
                float3 col = cLit + cShadow + ambient;

                // 硬边高光（卡通渲染的标志性特征）
                if (_SpecularStrength > 0.001)
                {
                    float3 h = normalize(l + v);
                    float sp = pow(saturate(dot(n, h)), 1.0 / max(_SpecularSize, 0.02));
                    col += lightCol * step(0.5, sp) * _SpecularStrength * lit;
                }

                // 边缘光（受光面更明显，勾出轮廓体积）
                float rim = 1.0 - saturate(dot(n, v));
                rim = pow(rim, _RimPower) * _RimStrength;
                col += _RimColor.rgb * rim * albedo.rgb * (0.30 + 0.70 * ramp);

                // 自发光（窗户/灯笼/招牌；TimeOfDay 夜里 EnableKeyword("_EMISSION") 打开）
                #ifdef _EMISSION
                    col += albedo.rgb * _EmissionColor.rgb;
                #endif

                UNITY_APPLY_FOG(i.fogCoord, col);
                return fixed4(col, 1);
            }
            ENDCG
        }

        // ── 点光源附加 pass（路灯/室内灯）：阶梯化 + 压低，避免画面变脏 ──
        Pass
        {
            Name "FORWARD_ADD"
            Tags { "LightMode"="ForwardAdd" }
            Blend One One
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert_t
            #pragma fragment frag_add
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog
            #pragma multi_compile __ _EMISSION
            #pragma shader_feature _ALPHATEST_ON
            #pragma target 3.0

            fixed4 frag_add(v2f_t i) : SV_Target
            {
                fixed4 albedo = SampleAlbedo(i.uv);

                float3 n = normalize(i.wnormal);
                float3 v = normalize(_WorldSpaceCameraPos - i.wpos);

                #ifdef USING_DIRECTIONAL_LIGHT
                    float3 l = normalize(_WorldSpaceLightPos0.xyz);
                    UNITY_LIGHT_ATTENUATION(atten, i, i.wpos);
                #else
                    float3 l = normalize(_WorldSpaceLightPos0.xyz - i.wpos);
                    UNITY_LIGHT_ATTENUATION(atten, i, i.wpos);
                #endif

                float ramp = ToonStep(dot(n, l), _RampSteps, _RampSoft);
                float3 col = albedo.rgb * _LightColor0.rgb * _ToonSunPower * ramp * atten * 0.60;

                UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0, 0, 0, 0));
                return fixed4(col, 1);
            }
            ENDCG
        }

        // ── 描边（反向外扩 + 屏幕空间恒定宽度）
        //
        // 2026-11-10 第二轮：借鉴 s-ilent/scss 的描边做法，补了三项我们原来缺的能力。
        // 起因是对比探针（shots/301-outline-probe.png）发现：**0.06m 薄片上的描边会完全消失**。
        // 根因是反向外扩在薄片上的固有缺陷 —— 薄片两个面距离极近，外扩后正面描边被背面描边遮住。
        //
        // 三项改进：
        //   1. _OutlineNearDistance/_OutlineFarDistance：近处收细、远处淡出（原来写死 18/55）
        //   2. _OutlineMask：逐部位控制描边强弱（贴图 R 通道），薄片可以单独调弱
        //   3. _OUTLINE_VERTEXCOLOR：用顶点色 R 通道当宽度倍率（程序化网格可按面标宽度）
        // 另外把「法线退化」处理了：当法线几乎垂直于视线时（薄片边缘），
        // 屏幕投影方向会不稳定 → 兜底用 uv 主轴的垂直方向，避免描边抖动。
        // ──
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert_o
            #pragma fragment frag_o
            #pragma multi_compile __ _OUTLINE_VERTEXCOLOR
            #pragma target 3.0

            struct appdata_o
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f_o
            {
                float4 pos : SV_POSITION;
                float dist : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            v2f_o vert_o(appdata_o v)
            {
                v2f_o o;
                float4 cp = UnityObjectToClipPos(v.vertex);

                // 视空间法线 → 屏幕方向
                float3 vn = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                float2 pn = float2(vn.x, vn.y);
                float len = length(pn);

                // 法线退化兜底：薄片边缘的法线几乎垂直于视线时，屏幕投影趋近 0，
                // 外扩方向不稳定 → 描边抖动或完全消失（这就是薄片描边丢失的直接原因）。
                // 改用「沿屏幕外法向」的兜底方向，保证始终有明确外扩方向。
                if (len < 0.08)
                {
                    // 用物体中心到顶点的屏幕方向作为外扩方向（薄片会沿自身轮廓向外扩）
                    float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    float3 centerW = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                    float3 dirW = wpos - centerW;
                    float3 dirV = mul((float3x3)UNITY_MATRIX_V, dirW);
                    pn = float2(dirV.x, dirV.y);
                    len = length(pn);
                    if (len < 1e-4) { pn = float2(0.7071, 0.7071); len = 1.0; }
                }
                pn /= max(len, 1e-4);

                // 宽度：基础宽 × 顶点色倍率（可选）
                float width = _OutlineWidth;
                #ifdef _OUTLINE_VERTEXCOLOR
                    width *= v.color.r;
                #endif

                // ★ 关键修正（借鉴 scss）：宽度必须随视距**按 FOV 归一化**缩放，
                // 而不是单纯乘 clip.w。clip.w ≈ 视距，所以 cp.xy += dir * width * cp.w
                // 在数学上已经是「屏幕恒定宽度」，但它在薄片上会和背面 shell 重叠，
                // 导致正面描边被背面描边遮挡（薄片描边消失的真因）。
                // 这里改成 scss 的做法：按视距夹取一个「米」尺度的宽度，
                // 再换算到裁剪空间，薄片处不会因为 shell 过薄而互相吞掉。
                float viewDist = length(mul(UNITY_MATRIX_MV, v.vertex).xyz);
                float fov = atan(1.0 / max(abs(UNITY_MATRIX_P[1][1]), 1e-4)) * 1.14591559;
                float distanceFactor = viewDist * fov;
                float finalScale = clamp(distanceFactor, _OutlineNearDistance, _OutlineFarDistance);
                // 近处收细（避免贴脸过粗）、远处淡出
                float nearFade = saturate((viewDist - _OutlineNearDistance) / max(_OutlineNearDistance, 0.01));
                width *= lerp(0.35, 1.0, nearFade);

                cp.xy += pn * width * finalScale;
                o.pos = cp;
                o.dist = viewDist;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag_o(v2f_o i) : SV_Target
            {
                // 宽度为 0 的材质整片丢弃（省掉无用的背面绘制）
                clip(_OutlineWidth - 1e-5);

                // 部位遮罩：R 通道越暗描边越淡（薄片/小件可以单独压弱）
                float mask = tex2D(_OutlineMask, i.uv).r;
                clip(mask - 0.02);

                // 远处淡出
                float farFade = 1.0 - saturate((i.dist - _OutlineFarDistance) / max(_OutlineFarDistance * 0.5, 1.0));
                clip(farFade - 0.02);

                fixed4 c = _OutlineColor;
                c.a = 1;
                // 描边颜色朝场景色靠，远处不糊成黑片（不透明 pass 里 alpha 无效，只能改色）
                float fade = mask * farFade;
                c.rgb = lerp(float3(0.42, 0.44, 0.50), c.rgb, saturate(fade));
                return c;
            }
            ENDCG
        }

        // ── 阴影投射 ──
        Pass
        {
            Name "SHADOWCASTER"
            Tags { "LightMode"="ShadowCaster" }

            CGPROGRAM
            #pragma vertex vert_s
            #pragma fragment frag_s
            #pragma multi_compile_shadowcaster
            #pragma target 3.0

            struct v2f_s
            {
                V2F_SHADOW_CASTER;
            };

            v2f_s vert_s(appdata_base v)
            {
                v2f_s o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag_s(v2f_s i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    Fallback Off
}
