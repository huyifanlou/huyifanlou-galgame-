// Continuous surface lighting, triplanar materials and soft-shadow support. Legacy ramp remains available only when SurfaceModel is 0.
Shader "Campus/Toon"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _HeightTex ("Surface Height", 2D) = "gray" {}
        _HeightStrength ("Surface Relief", Float) = 0
        _WorldMapping ("World Mapping", Float) = 0
        _WorldScale ("World Scale", Float) = 1
        _SpecularSoft ("Soft Specular", Range(0,1)) = 0
        _SurfaceModel ("Continuous Surface Lighting", Range(0,1)) = 0
        _Roughness ("Roughness", Range(0.08,1)) = 0.7
        _Metallic ("Metallic", Range(0,1)) = 0
        _TerrainEdgeTex ("Terrain Edge Mask", 2D) = "black" {}
        _TerrainSurface ("Terrain Surface", Float) = 0
        _Color ("Tint", Color) = (1,1,1,1)
        _VertexTint ("Vertex Tint", Range(0,1)) = 0
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

        sampler2D _HeightTex;float _HeightStrength,_WorldMapping,_WorldScale,_SpecularSoft,_SurfaceModel,_Roughness,_Metallic;
        float _InteriorLighting;
        float _WeatherRain,_WeatherSnow;
        float4 _SurfaceSky,_SurfaceGround;
        float4 _RoomFillPositions[4],_RoomFillColors[4];
        sampler2D _TerrainEdgeTex;float _TerrainSurface,_TerrainSpan;
        sampler2D _MainTex;
        float4 _MainTex_ST;
        fixed4 _Color;
        float _VertexTint;
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
            fixed4 color : COLOR;
        };

        struct v2f_t
        {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 wnormal : TEXCOORD1;
            float3 wpos : TEXCOORD2;
            fixed4 color : COLOR;
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
            o.color = v.color;
            UNITY_TRANSFER_SHADOW(o, o.pos);
            UNITY_TRANSFER_FOG(o, o.pos);
            return o;
        }

        float2 SurfaceUV(float2 uv,float3 pos,float3 normal)
        {
            if(_WorldMapping<.5)return uv;
            float3 a=abs(normal);return (a.y>=a.x&&a.y>=a.z?pos.xz:a.x>=a.z?pos.zy:pos.xy)*_WorldScale;
        }
        float3 SafeDirection(float3 direction){return direction*rsqrt(max(dot(direction,direction),.0000001));}
        float3 SurfaceNormal(float3 normal,float3 pos,float2 uv)
        {
            float3 n=SafeDirection(normal);if(_HeightStrength<.00001)return n;
            float3 weights=pow(abs(n),4);weights/=max(dot(weights,1),.0001);
            float height=_WorldMapping>.5?dot(float3(tex2D(_HeightTex,pos.zy*_WorldScale).r,tex2D(_HeightTex,pos.xz*_WorldScale).r,tex2D(_HeightTex,pos.xy*_WorldScale).r),weights):tex2D(_HeightTex,uv).r;
            float3 dx=ddx(pos),dy=ddy(pos),r1=cross(dy,n),r2=cross(n,dx);
            float det=dot(dx,r1);float3 gradient=(r1*ddx(height)+r2*ddy(height))*sign(det)/max(abs(det),.000001);
            return SafeDirection(n-gradient*_HeightStrength);
        }
        fixed4 SampleAlbedo(float2 uv)
        {
            fixed4 a = tex2D(_MainTex, uv) * _Color;
            #ifdef _ALPHATEST_ON
                clip(a.a - _Cutoff);
            #endif
            return a;
        }
        fixed4 SampleSurface(float2 uv,float3 pos,float3 normal)
        {
            if(_WorldMapping<.5)return SampleAlbedo(uv);
            float3 weights=pow(abs(SafeDirection(normal)),4);weights/=max(dot(weights,1),.0001);
            float4 color=(tex2D(_MainTex,pos.zy*_WorldScale)*weights.x+tex2D(_MainTex,pos.xz*_WorldScale)*weights.y+tex2D(_MainTex,pos.xy*_WorldScale)*weights.z)*_Color;
            if(_TerrainSurface>.5){float edge=tex2D(_TerrainEdgeTex,pos.xz/max(_TerrainSpan,1)+.5).r;color.rgb*=lerp(float3(1,1,1),float3(.88,.79,.64),edge*.65);}
            return color;
        }
        float3 WeatherSurface(float3 color,float3 pos,float3 normal)
        {
            float exposure=(1-_InteriorLighting)*step(max(abs(pos.x),abs(pos.z)),151);
            float up=smoothstep(.25,.85,normal.y);
            float patch=.5+.5*sin(pos.x*3.1+sin(pos.z*2.7))*sin(pos.z*4.7+sin(pos.x*1.8));
            float pooled=smoothstep(.25,.75,patch)*up*lerp(1,.15,saturate(_TerrainSurface+_VertexTint));
            color*=1-_WeatherRain*exposure*(.10+up*.20+pooled*.16);
            float snow=_WeatherSnow*exposure*up*lerp(smoothstep(.08,.65,patch),1,_WeatherSnow);
            return lerp(color,float3(.81,.86,.89)*( .92+patch*.08),snow*.94);
        }
        float3 SurfaceSpecular(float3 n,float3 l,float3 v,float3 albedo)
        {
            float3 h=SafeDirection(l+v);float wet=_WeatherRain*(1-_InteriorLighting)*smoothstep(.45,.95,n.y);
            wet*=lerp(1,.25,saturate(_TerrainSurface+_VertexTint));
            float rough=max(lerp(_Roughness,.18,wet*.92),.15);
            rough=lerp(rough,.9,_WeatherSnow*(1-_InteriorLighting)*smoothstep(.25,.85,n.y));
            float exponent=lerp(150,5,rough*rough);
            float highlight=pow(max(.0001,saturate(dot(n,h))),exponent)*(exponent+2)/32;
            float3 f0=lerp(float3(.035,.035,.035),albedo,_Metallic);
            float3 fresnel=f0+(1-f0)*pow(max(.0001,1-saturate(dot(v,h))),5);
            return fresnel*highlight*(1-rough*.35);
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
                float2 surfaceUV=SurfaceUV(i.uv,i.wpos,i.wnormal);
                fixed4 albedo = SampleSurface(i.uv,i.wpos,i.wnormal);
                albedo.rgb *= lerp(1.0, i.color.rgb, _VertexTint);
                albedo.rgb=WeatherSurface(albedo.rgb,i.wpos,i.wnormal);

                float3 n = SurfaceNormal(i.wnormal,i.wpos,surfaceUV);
                float3 v = normalize(_WorldSpaceCameraPos - i.wpos);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightCol = _LightColor0.rgb * _ToonSunPower;

                float ramp = ToonStep(dot(n, l), _RampSteps, _RampSoft);
                float lit = ramp * UNITY_SHADOW_ATTENUATION(i, i.wpos);

                float3 cLit    = albedo.rgb * lightCol * lit;
                float3 cShadow = albedo.rgb * _ShadowTint.rgb * lightCol * (1.0 - lit) * 0.60;
                float3 ambient = albedo.rgb * _ToonAmbient.rgb;
                float3 col = cLit + cShadow + ambient;
                if(_SurfaceModel>.5)
                {
                    float shadow=UNITY_SHADOW_ATTENUATION(i,i.wpos);
                    float diffuse=saturate(dot(n,l));
                    float3 hemi=lerp(_SurfaceGround.rgb,_SurfaceSky.rgb,saturate(n.y*.5+.5));
                    col=albedo.rgb*hemi+_LightColor0.rgb*shadow*(albedo.rgb*diffuse*(1-_Metallic*.75)+SurfaceSpecular(n,l,v,albedo.rgb)*diffuse);
                    [unroll]for(int j=0;j<4;j++)
                    {
                        float3 delta=_RoomFillPositions[j].xyz-i.wpos;float dist2=dot(delta,delta);float falloff=saturate(1-dist2/max(_RoomFillPositions[j].w,1));falloff*=falloff;
                        col+=albedo.rgb*_RoomFillColors[j].rgb*falloff*(.25+.75*saturate(dot(n,SafeDirection(delta))))*_InteriorLighting;
                    }
                }

                float wetFace=_WeatherRain*(1-_InteriorLighting)*smoothstep(.6,.95,i.wnormal.y)*lerp(1,.12,saturate(_TerrainSurface+_VertexTint));
                float pools=smoothstep(.22,.72,.5+.5*sin(i.wpos.x*3.1+sin(i.wpos.z*2.7))*sin(i.wpos.z*4.7+sin(i.wpos.x*1.8)));
                col=lerp(col,col*.75+_SurfaceSky.rgb*.30,wetFace*pools*(.18+.45*pow(1-saturate(dot(n,v)),3)));
                // 硬边高光（卡通渲染的标志性特征）
                if (_SpecularStrength > 0.001 && _SurfaceModel<.5)
                {
                    float3 h = normalize(l + v);
                    float sp = pow(saturate(dot(n, h)), 1.0 / max(_SpecularSize, 0.02));
                    col += lightCol * lerp(step(0.5, sp),smoothstep(.08,.65,sp),_SpecularSoft) * _SpecularStrength * lit;
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
                if(any(col!=col)||any(abs(col)>64))col=albedo.rgb*_ToonAmbient.rgb;
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
                float2 surfaceUV=SurfaceUV(i.uv,i.wpos,i.wnormal);
                fixed4 albedo = SampleSurface(i.uv,i.wpos,i.wnormal);
                albedo.rgb *= lerp(1.0, i.color.rgb, _VertexTint);
                albedo.rgb=WeatherSurface(albedo.rgb,i.wpos,i.wnormal);

                float3 n = SurfaceNormal(i.wnormal,i.wpos,surfaceUV);
                float3 v = normalize(_WorldSpaceCameraPos - i.wpos);

                #ifdef USING_DIRECTIONAL_LIGHT
                    float3 l = normalize(_WorldSpaceLightPos0.xyz);
                    UNITY_LIGHT_ATTENUATION(atten, i, i.wpos);
                #else
                    float3 l = normalize(_WorldSpaceLightPos0.xyz - i.wpos);
                    UNITY_LIGHT_ATTENUATION(atten, i, i.wpos);
                #endif

                float ramp = ToonStep(dot(n, l), _RampSteps, _RampSoft);
                float3 col = albedo.rgb * _LightColor0.rgb * ramp * atten * 0.60;
                if(_SurfaceModel>.5){float diffuse=saturate(dot(n,l));col=_LightColor0.rgb*atten*(albedo.rgb*diffuse*(1-_Metallic*.75)+SurfaceSpecular(n,l,v,albedo.rgb)*diffuse);}

                UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0, 0, 0, 0));
                if(any(col!=col)||any(abs(col)>64))col=0;
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
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert_o
            #pragma fragment frag_o
            #pragma multi_compile __ _OUTLINE_VERTEXCOLOR
            #pragma multi_compile_fog
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
                UNITY_FOG_COORDS(2)
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

                width *= 1.0 - smoothstep(_OutlineFarDistance * 0.40, _OutlineFarDistance, viewDist);
                pn.x *= _ScreenParams.y / max(_ScreenParams.x, 1.0);
                cp.xy += pn * width * finalScale;
                o.pos = cp;
                o.dist = viewDist;
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o, o.pos);
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
                float farFade = 1.0 - smoothstep(_OutlineFarDistance * 0.40, _OutlineFarDistance, i.dist);
                clip(farFade - 0.02);

                fixed4 c = _OutlineColor;
                c.a = 1;
                // 描边颜色朝场景色靠，远处不糊成黑片（不透明 pass 里 alpha 无效，只能改色）
                UNITY_APPLY_FOG(i.fogCoord, c);
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
