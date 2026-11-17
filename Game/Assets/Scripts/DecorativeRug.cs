using System.Collections.Generic;
using UnityEngine;

// 整张地毯使用独立 0..1 UV；边框和中心纹样不再随世界坐标重复成地板条缝。
public static class DecorativeRug
{
    static readonly Dictionary<string,Material> cache=new Dictionary<string,Material>();
    public static Material Material(string room,string tint,float width,float depth)
    {
        string key=room+tint+width.ToString("F2")+depth.ToString("F2");if(cache.TryGetValue(key,out var found))return found;
        int tw=Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(width*96)),128,1024),th=Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(depth*96)),128,1024);var pixels=new Color[tw*th];Color ground=MatLib.Hex(tint),cream=MatLib.Hex("#d8c9ad"),ink=Color.Lerp(ground,MatLib.Hex("#394a45"),.40f);
        for(int y=0;y<th;y++)for(int x=0;x<tw;x++)
        {
            float u=(x+.5f)/tw,v=(y+.5f)/th,px=(u-.5f)*width,pz=(v-.5f)*depth;
            float edge=Mathf.Min(u*width,Mathf.Min((1-u)*width,Mathf.Min(v*depth,(1-v)*depth)));
            float weave=Mathf.Sin(x*2.0944f)*Mathf.Cos(y*2.0944f),grain=Mathf.PerlinNoise(u*180,v*180)-.5f;
            Color color=ground*(.97f+weave*.016f+grain*.055f);
            if(edge<.045f)color=ink;
            else if(edge<.065f||Mathf.Abs(edge-.14f)<.012f)color=cream;
            else if(edge<.20f)color=Color.Lerp(ink,ground,.35f);
            if(edge>.24f)
            {
                if(room=="home")
                {
                    // 柔和的双层菱形章纹，留出大片素色织物。
                    float diamond=Mathf.Abs(px)/Mathf.Max(width*.27f,.2f)+Mathf.Abs(pz)/Mathf.Max(depth*.27f,.2f);
                    if(Mathf.Abs(diamond-1)<.035f||Mathf.Abs(diamond-.72f)<.025f)color=Color.Lerp(ground,cream,.63f);
                    if(diamond<.20f)color=Color.Lerp(ground,cream,.28f);
                }
                else if(room=="library"||room=="book")
                {
                    float a=Mathf.Abs(Mathf.Repeat(px+pz,.8f)-.4f),b=Mathf.Abs(Mathf.Repeat(px-pz,.8f)-.4f);
                    if(a<.014f||b<.014f)color=Color.Lerp(ground,cream,.22f);
                }
                else if(room=="dorm")
                {
                    float band=Mathf.Repeat(pz,.55f);if(band<.018f||Mathf.Abs(band-.07f)<.009f)color=Color.Lerp(ground,cream,.35f);
                }
                else if(room=="restaurant")
                {
                    float line=Mathf.Abs(Mathf.Repeat(px,.65f)-.325f);if(line<.012f)color=Color.Lerp(ground,ink,.24f);
                }
                else if(room=="milktea")
                {
                    float rings=Mathf.Abs(Mathf.Sqrt(px*px+pz*pz)-Mathf.Min(width,depth)*.23f);if(rings<.025f)color=Color.Lerp(ground,cream,.5f);
                }
            }
            pixels[y*tw+x]=color;
        }
        var texture=new Texture2D(tw,th,TextureFormat.RGBA32,true){name="RugPattern_"+key,filterMode=FilterMode.Trilinear,anisoLevel=8,wrapMode=TextureWrapMode.Clamp};texture.SetPixels(pixels);texture.Apply(true,true);
        found=new Material(DetailSurface.Mat("fabric",tint,false)){name="DecorativeRug_"+key};found.mainTexture=texture;found.color=Color.white;found.SetFloat("_Roughness",.98f);found.SetFloat("_HeightStrength",.0003f);found.SetFloat("_OutlineWidth",0);cache[key]=found;return found;
    }
}
