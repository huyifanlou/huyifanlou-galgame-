Shader "Campus/TextSign"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Coverage", Range(0,1)) = 0.42
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest+20" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        Cull Off
        ZWrite On
        ZTest LEqual
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 _Color;
            float _Cutoff;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; UNITY_FOG_COORDS(1) };
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color;
                UNITY_TRANSFER_FOG(o,o.vertex); return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                clip(tex2D(_MainTex,i.uv).a*i.color.a-_Cutoff);
                fixed4 c=fixed4(i.color.rgb,1); UNITY_APPLY_FOG(i.fogCoord,c); return c;
            }
            ENDCG
        }
    }
}
