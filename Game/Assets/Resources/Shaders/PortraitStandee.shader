Shader "Campus/PortraitStandee"
{
 Properties{_MainTex("Portrait",2D)="white"{} _Cutoff("Alpha cutoff",Range(0,1))=.25}
 SubShader
 {
  Tags{"Queue"="AlphaTest" "RenderType"="TransparentCutout"} Cull Off ZWrite On
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "UnityCG.cginc"
   sampler2D _MainTex;float _Cutoff;
   struct v2f{float4 position:SV_POSITION;float2 uv:TEXCOORD0;UNITY_FOG_COORDS(1)};
   v2f vert(appdata_base v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;UNITY_TRANSFER_FOG(o,o.position);return o;}
   fixed4 frag(v2f i):SV_Target{fixed4 c=tex2D(_MainTex,i.uv);clip(c.a-_Cutoff);c.a=1;UNITY_APPLY_FOG(i.fogCoord,c);return c;}
   ENDCG
  }
  Pass
  {
   Tags{"LightMode"="ShadowCaster"}
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_shadowcaster
   #include "UnityCG.cginc"
   sampler2D _MainTex;float _Cutoff;
   struct v2f{V2F_SHADOW_CASTER;float2 uv:TEXCOORD1;};
   v2f vert(appdata_base v){v2f o;o.uv=v.texcoord;TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);return o;}
   float4 frag(v2f i):SV_Target{clip(tex2D(_MainTex,i.uv).a-_Cutoff);SHADOW_CASTER_FRAGMENT(i)}
   ENDCG
  }
 }
}
