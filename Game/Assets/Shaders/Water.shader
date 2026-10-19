Shader "Campus/Water"
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.08,0.20,0.26,1)
        _ShallowColor ("Shallow Color", Color) = (0.24,0.48,0.52,1)
        _FoamColor ("Foam Color", Color) = (0.88,0.94,0.95,1)
        _SpecPower ("Spec Power", Float) = 90
        _SpecIntensity ("Spec Intensity", Float) = 0.8
        _MainTex ("Water Detail", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _DeepColor, _ShallowColor, _FoamColor;
            float _SpecPower, _SpecIntensity;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _SunColor, _SkyColor, _SunDir;
            float _WaterAmbient;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD3;
                UNITY_FOG_COORDS(2)
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 N = normalize(i.worldNormal);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float fres = pow(1.0 - saturate(dot(N, V)), 4.0);

                // 细节贴图扰动法线（顺流滚动）→ 细碎波光
                float2 duv = i.uv + float2(_Time.y * 0.09, 0.0);
                float lumC = tex2D(_MainTex, duv).r;
                float lumX = tex2D(_MainTex, duv + float2(0.012, 0.0)).r;
                float lumY = tex2D(_MainTex, duv + float2(0.0, 0.012)).r;
                N = normalize(N + float3((lumC - lumX) * 3.6, 0.0, (lumC - lumY) * 3.6));

                float3 baseCol = lerp(_ShallowColor.rgb, _DeepColor.rgb, 0.55);
                float3 col = lerp(baseCol, _SkyColor.rgb, fres * 0.5);
                col *= 0.9 + lumC * 0.45;
                // 波面受光（正面迎光更亮，背光面压暗）
                float diff = saturate(dot(N, normalize(_SunDir.xyz)));
                col *= 0.86 + diff * 0.3;
                // 顺流白沫线（细节贴图亮纹）
                float streak = smoothstep(0.40, 0.52, lumC);
                col = lerp(col, _FoamColor.rgb, streak * 0.5);

                float3 L = normalize(_SunDir.xyz);
                float3 H = normalize(L + V);
                float spec = pow(saturate(dot(N, H)), _SpecPower) * _SpecIntensity;
                col += _SunColor.rgb * spec;

                float foam = saturate(i.color.r);
                col = lerp(col, _FoamColor.rgb, foam * 0.55);

                col *= _WaterAmbient;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
