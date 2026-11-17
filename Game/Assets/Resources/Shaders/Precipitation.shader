Shader "Campus/Precipitation"
{
    Properties {_MainTex("Particle",2D)="white"{} _TintColor("Tint",Color)=(.86,.90,.94,.85)}
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _MainTex_ST,_TintColor;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            struct vertexInput {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct vertexOutput {float4 pos:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float4 screen:TEXCOORD1;UNITY_FOG_COORDS(2)};
            vertexOutput vert(vertexInput v)
            {
                vertexOutput o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.color=v.color;
                o.screen=ComputeScreenPos(o.pos);COMPUTE_EYEDEPTH(o.screen.z);UNITY_TRANSFER_FOG(o,o.pos);return o;
            }
            fixed4 frag(vertexOutput i):SV_Target
            {
                float4 color=tex2D(_MainTex,i.uv)*i.color*_TintColor;
                float scene=LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen)));
                color.a*=saturate((scene-i.screen.z)*4)*smoothstep(.18,.70,i.screen.z);
                // Fog uses the actual scene haze. Black additive fog made white flakes look grey.
                UNITY_APPLY_FOG(i.fogCoord,color);return color;
            }
            ENDCG
        }
    }
}
