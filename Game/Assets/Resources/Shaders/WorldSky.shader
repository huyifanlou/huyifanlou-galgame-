Shader "Campus/WorldSky"
{
    Properties { _Night("Night",Range(0,1))=0 _Warm("Dusk",Range(0,1))=0 _SkyMul("Weather",Float)=1 }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "IgnoreProjector"="True" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float _Night,_Warm,_SkyMul;
            float _PrecipitationRain,_PrecipitationSnow;
            struct v2f { float4 pos:SV_POSITION; float3 ray:TEXCOORD0; };
            v2f vert(appdata_base v)
            {
                v2f o; o.ray=v.vertex.xyz;
                o.pos=UnityWorldToClipPos(v.vertex.xyz+_WorldSpaceCameraPos); return o;
            }
            float hash(float2 p) {return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 d=normalize(i.ray);float height=saturate(d.y);
                float3 horizon=lerp(float3(.78,.86,.87),float3(.89,.68,.57),_Warm);
                float3 zenith=lerp(float3(.34,.60,.75),float3(.47,.55,.69),_Warm*.6);
                float3 day=lerp(horizon,zenith,sqrt(height));
                float2 cp=d.xz/max(.12,d.y+.12)*3.5+float2(_Time.y*.002,0);
                float cloudNoise=noise(cp)*.60+noise(cp*2.1)*.28+noise(cp*4.2)*.12;
                float cloud=smoothstep(.55,.71,cloudNoise)*smoothstep(.035,.15,d.y)*(1-smoothstep(.38,.72,d.y));
                day=lerp(day,lerp(float3(.91,.94,.91),float3(.97,.80,.66),_Warm),cloud*.78);
                float3 night=lerp(float3(.14,.21,.31),float3(.035,.065,.14),sqrt(height));
                float2 starUV=float2(atan2(d.z,d.x)/6.283185+.5,asin(d.y)/3.141593+.5)*float2(260,130);
                float star=step(.989,hash(floor(starUV)))*(1-smoothstep(.035,.15,length(frac(starUV)-.5)))*smoothstep(.04,.25,d.y);
                night+=star*float3(.65,.72,.80)*(1-max(_PrecipitationRain,_PrecipitationSnow));
                float moon=1-smoothstep(.00017,.00030,1-dot(d,normalize(float3(-.42,.58,.7))));
                night+=moon*float3(.75,.74,.59)*(1-max(_PrecipitationRain,_PrecipitationSnow));
                float weather=max(_PrecipitationRain,_PrecipitationSnow);
                float overcast=smoothstep(.25,.7,cloudNoise)*.18+.80;
                float3 storm=lerp(float3(.55,.62,.68),float3(.32,.40,.49),sqrt(height));
                storm*=.80+cloudNoise*.25;
                float3 sky=lerp(day,night,_Night);
                sky=lerp(sky,storm*lerp(1,.16,_Night),weather*overcast);
                return fixed4(sky*_SkyMul,1);
            }
            ENDCG
        }
    }
}
