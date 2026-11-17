Shader "Campus/Water"
{
    Properties
    {
        _DeepColor("Deep Color",Color)=(.18,.36,.32,1)
        _ShallowColor("Shallow Color",Color)=(.42,.57,.44,1)
        _FoamColor("Foam Color",Color)=(.78,.83,.69,1)
        _SpecPower("Spec Power",Float)=110
        _SpecIntensity("Spec Intensity",Float)=.24
        _MainTex("Water Detail",2D)="white" {}
        _UseVertexFoam("River Edge Foam",Float)=1
        _ShoreEllipse("Shore Center / Radii",Vector)=(0,0,1,1)
        _ShoreMode("Ellipse Shore",Float)=0
        _WaveSpeed("Wave Speed",Float)=1
        _FlowScale("Flow Scale",Float)=1
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "Queue"="Geometry"}
        CGINCLUDE
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"
        float4 _DeepColor,_ShallowColor,_FoamColor,_ShoreEllipse;
        float _SpecPower,_SpecIntensity,_UseVertexFoam,_ShoreMode;
        float _WaveSpeed,_FlowScale;float4 _WaterMotion,_WaterFlow;
        float4 _SurfaceSky,_SurfaceGround,_SkyColor,_SunDir,_WeatherWind;
        float _WaterAmbient,_PrecipitationRain,_PrecipitationSnow;
        float hash21(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
        float noise21(float2 p)
        {
            float2 g=floor(p),f=frac(p);f=f*f*(3-2*f);
            return lerp(lerp(hash21(g),hash21(g+float2(1,0)),f.x),lerp(hash21(g+float2(0,1)),hash21(g+1),f.x),f.y);
        }
        float shoreDistance(float3 p)
        {
            float2 radius=max(_ShoreEllipse.zw,float2(.05,.05));
            return max(0,(1-length((p.xz-_ShoreEllipse.xy)/radius))*min(radius.x,radius.y));
        }
        float rainRing(float2 p)
        {
            p*=1.75;float2 cell=floor(p),f=frac(p);
            float seed=hash21(cell),phase=frac(_Time.y*1.65+seed*7.31);
            float2 center=.25+float2(seed,hash21(cell+13))*.5;
            float distance=length(f-center)-phase*.52;
            return sin(distance*48)*exp(-abs(distance)*25)*(1-phase)*smoothstep(.05,.16,phase);
        }
        float waveHeight(float2 p)
        {
            float2 drift=_WaterFlow.xy*_FlowScale;
            float t=_WaterMotion.x*_WaveSpeed;
            float2 q=p+drift;float warp=noise21(q*.43)*2;
            float wave=sin(q.x*3.3+q.y*1.6+warp+t*.12)*.003;
            wave+=sin(q.x*-2.2+q.y*5.7-warp-t*.09)*.002;
            wave+=sin(q.x*8.5+q.y*6.3+warp*2+t*.18)*.0012;
            wave*=1+_PrecipitationRain*.8;
            return wave+_PrecipitationRain*rainRing(p)*.007;
        }
        float3 waterNormal(float3 normal,float3 pos)
        {
            float height=waveHeight(pos.xz),eps=.025;
            float dx=(waveHeight(pos.xz+float2(eps,0))-height)/eps;
            float dz=(waveHeight(pos.xz+float2(0,eps))-height)/eps;
            return normalize(normal+float3(-dx,0,-dz)*saturate(normal.y));
        }
        struct appWater {float4 vertex:POSITION;float3 normal:NORMAL;float4 color:COLOR;};
        struct waterVarying
        {
            float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float4 color:TEXCOORD2;
            UNITY_SHADOW_COORDS(3) UNITY_FOG_COORDS(4)
        };
        waterVarying vert(appWater v)
        {
            waterVarying o;float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
            float3 normal=UnityObjectToWorldNormal(v.normal);
            float edge=_ShoreMode>.5?smoothstep(0,.32,shoreDistance(world)):1;
            world.y+=waveHeight(world.xz)*.65*edge*saturate(normal.y);
            o.pos=UnityWorldToClipPos(world);o.world=world;o.normal=normal;o.color=v.color;
            UNITY_TRANSFER_SHADOW(o,o.pos);UNITY_TRANSFER_FOG(o,o.pos);return o;
        }
        ENDCG
        Pass
        {
            Tags {"LightMode"="ForwardBase"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma target 3.0
            fixed4 frag(waterVarying i):SV_Target
            {
                float3 n=waterNormal(normalize(i.normal),i.world);
                float3 v=normalize(_WorldSpaceCameraPos-i.world),l=normalize(_SunDir.xyz);
                float shore=_ShoreMode>.5?shoreDistance(i.world):3;
                float grain=noise21((i.world.xz+_WaterFlow.xy*_FlowScale)*1.7);
                float depth=smoothstep(.05,3.0,shore);
                float3 base=lerp(_ShallowColor.rgb,_DeepColor.rgb,saturate(depth*.90+grain*.10));
                base=lerp(base,base*float3(.85,.93,1.05),_PrecipitationSnow*.45);
                float shadow=UNITY_SHADOW_ATTENUATION(i,i.world);
                float diffuse=saturate(dot(n,l));
                // The same hemisphere palette and real sun shadows used on stone and foliage.
                float3 col=base*(_SurfaceSky.rgb*.78+_SurfaceGround.rgb*.22+_LightColor0.rgb*(.25+.58*diffuse)*shadow);
                float fres=pow(1-saturate(dot(n,v)),4);
                col=lerp(col,_SkyColor.rgb,fres*.20);
                float micro=noise21((i.world.xz+_WaterFlow.xy*_FlowScale)*float2(1.8,8));
                float fine=smoothstep(.60,.77,micro)*smoothstep(.12,.90,saturate(n.y));
                col+=_FoamColor.rgb*fine*.028*_WaterAmbient*(1-_PrecipitationRain*.35);
                float3 h=normalize(l+v);
                float spec=pow(saturate(dot(n,h)),_SpecPower)*_SpecIntensity;
                col+=_LightColor0.rgb*spec*shadow*(.45+micro*.55);
                float foam=_ShoreMode>.5?(1-smoothstep(.035,.25+grain*.16,shore))*(.45+grain*.55):saturate(i.color.r)*_UseVertexFoam;
                col=lerp(col,_FoamColor.rgb*(_SurfaceSky.rgb+_LightColor0.rgb*.4*shadow),foam*.40);
                float rings=abs(rainRing(i.world.xz));
                col+=_FoamColor.rgb*rings*_PrecipitationRain*.11*_WaterAmbient;
                UNITY_APPLY_FOG(i.fogCoord,col);return fixed4(col,1);
            }
            ENDCG
        }
        Pass
        {
            Tags {"LightMode"="ForwardAdd"}
            Blend One One ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragAdd
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog
            #pragma target 3.0
            fixed4 fragAdd(waterVarying i):SV_Target
            {
                float3 n=waterNormal(normalize(i.normal),i.world);
                float3 l=normalize(UnityWorldSpaceLightDir(i.world)),v=normalize(_WorldSpaceCameraPos-i.world),h=normalize(l+v);
                UNITY_LIGHT_ATTENUATION(atten,i,i.world);
                float spec=pow(saturate(dot(n,h)),85)*.30;
                float3 color=_LightColor0.rgb*atten*(.024+spec)*saturate(dot(n,l));
                UNITY_APPLY_FOG_COLOR(i.fogCoord,color,fixed4(0,0,0,0));return fixed4(color,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
