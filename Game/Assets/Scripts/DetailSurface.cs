using System.Collections.Generic;using UnityEngine;

// 512px 平铺表面与高度细节：墙/木/织物不再只剩一块平涂色。
public static class DetailSurface
{
    static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
    static readonly Dictionary<string,Texture2D[]> textures=new Dictionary<string,Texture2D[]>();
    public static Material Mat(string kind,string color,bool world=true)
    {
        string key=kind+color+world;if(materials.TryGetValue(key,out var found))return found;
        var tex=Textures(kind);var mat=MatLib.LitTexture(tex[0],1,1,.08f);mat.name="Detail_"+kind+"_"+color.TrimStart('#');mat.color=MatLib.Hex(color);
        mat.SetTexture("_HeightTex",tex[1]);mat.SetFloat("_HeightStrength",kind=="brick"?.008f:kind=="tile"?.004f:(kind=="wood"||kind=="timber")?.002f:.0012f);
        mat.SetFloat("_WorldMapping",world?1:0);mat.SetFloat("_WorldScale",kind=="brick"?1.3f:kind=="roof"?1.6f:(kind=="wood"||kind=="timber")?.7f:1);
        mat.SetFloat("_RampSteps",6);mat.SetFloat("_RampSoft",.18f);
        mat.SetFloat("_SurfaceModel",1);mat.SetFloat("_Roughness",kind=="metal"?.36f:kind=="glass"?.20f:kind=="tile"||kind=="checker"?.50f:kind=="timber"?.66f:.90f);mat.SetFloat("_Metallic",kind=="metal"?.65f:0);
        if(kind=="meadow"){mat.SetFloat("_WorldScale",.25f);mat.SetFloat("_HeightStrength",.006f);mat.SetFloat("_Roughness",.94f);}
        if(kind=="oakwide"){mat.SetFloat("_WorldScale",.5f);mat.SetFloat("_Roughness",.74f);}
        if(kind=="stoneLarge"){mat.SetFloat("_WorldScale",.625f);mat.SetFloat("_Roughness",.54f);}
        if(kind=="octagon")mat.SetFloat("_Roughness",.58f);
        if(kind=="linenwall"||kind=="pinstripe"||kind=="botanical")mat.SetFloat("_HeightStrength",.0004f);
        mat.SetFloat("_OutlineWidth",.00065f);mat.SetFloat("_RimStrength",.025f);mat.SetFloat("_SpecularStrength",kind=="metal"?.28f:kind=="tile"?.15f:kind=="glass"?.34f:.045f);mat.SetFloat("_SpecularSoft",1);
        mat.SetColor("_ShadowTint",new Color(.70f,.71f,.74f));materials[key]=mat;return mat;
    }
    public static Material Interior(string name)
    {
        switch(name){case "in_floor":return Mat("wood","#b99b74");case "in_tile":return Mat("tile","#cfcabe");case "in_wall":return Mat("plaster","#ece6d9");case "in_wall2":return Mat("wallpaper","#cdd8d5");case "in_wall3":return Mat("wallpaper","#e7d9c4");case "in_walltile":return Mat("tile","#d4dddc");case "in_panel":return Mat("timber","#9b8265");case "in_ceil":return Mat("plaster","#f0eee8");default:return null;}
    }
    static Texture2D[] Textures(string kind)
    {
        if(textures.TryGetValue(kind,out var found))return found;const int size=512;var rgb=new Color32[size*size];var height=new Color32[size*size];
        float Edge(float x){float f=Mathf.Repeat(x,1);return Mathf.Min(f,1-f);}
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=x/(float)size,v=y/(float)size,noise=Mathf.PerlinNoise(u*128+11,v*128+17),level=.97f+(noise-.5f)*.035f,h=.5f;
            if(kind=="foliage")
            {float clusters=Mathf.PerlinNoise(u*14+3,v*14+7);level=.88f+clusters*.16f+(noise-.5f)*.035f;h=.5f+(clusters-.5f)*.08f;}
            else if(kind=="linenwall"||kind=="pinstripe"||kind=="botanical")
            {
                float thread=Mathf.Sin(x*2.0944f)*Mathf.Sin(y*2.0944f),slub=Mathf.PerlinNoise(u*90,v*5)-.5f;
                level=.975f+thread*.009f+slub*.025f;h=.5f+thread*.025f;
                if(kind=="pinstripe") {float stripe=Edge(u*4);if(stripe<.009f||Mathf.Abs(stripe-.035f)<.004f)level-=.045f;}
                if(kind=="botanical")
                {
                    float stem=.5f+Mathf.Sin(v*Mathf.PI*4)*.06f;float dx=Mathf.Repeat(u*2,1)-stem;
                    bool vine=Mathf.Abs(dx)<.004f;float row=Mathf.Floor(v*8),cy=(row+.5f)/8;
                    float side=((int)row%2)==0?1:-1;float lx=dx-side*.085f,ly=(v-cy)*7;
                    float leaf=(lx*7+ly*.6f)*(lx*7+ly*.6f)+ly*ly*2;
                    if(vine)level-=.12f;else if(leaf<.20f)level-=.10f;
                }
            }
            else if(kind=="oakwide")
            {
                float row=Mathf.Floor(v*10),along=u+row*.37f;bool seam=Edge(v*10)<.016f||Edge(along)<.003f;
                float grain=Mathf.Sin(v*820+Mathf.PerlinNoise(u*3,row*.5f)*4),tone=Mathf.PerlinNoise(row*2,Mathf.Floor(along)+5);
                level=seam?.86f:.94f+grain*.012f+tone*.035f;h=seam?.36f:.54f+grain*.012f;
            }
            else if(kind=="stoneLarge"||kind=="octagon")
            {
                float a=Mathf.Repeat(u*2,1),b=Mathf.Repeat(v*2,1),corner=Mathf.Abs(a-.5f)+Mathf.Abs(b-.5f);
                bool seam=Edge(u*2)<.006f||Edge(v*2)<.006f;
                level=seam?.86f:.96f+(Mathf.PerlinNoise(u*9,v*9)-.5f)*.045f;
                if(kind=="octagon"&&corner>.81f)level=.80f;
                h=seam?.37f:.54f;
            }
            else if(kind=="meadow")
            {float macro=Mathf.PerlinNoise(u*5+7,v*5+3),fiber=Mathf.PerlinNoise(u*150,v*200);level=.95f+(macro-.5f)*.055f+(fiber-.5f)*.025f;h=.5f;}
            else if(kind=="paving"||kind=="cobble")
            {float rows=kind=="paving"?4:3,columns=kind=="paving"?2:3,row=Mathf.Floor(v*rows),xx=u*columns+(row%2)*.5f;bool seam=Edge(xx)<.012f||Edge(v*rows)<.018f;float tone=Mathf.PerlinNoise(Mathf.Floor(xx)*3,row*5);level=seam?.84f:.94f+(tone-.5f)*.07f+(noise-.5f)*.015f;h=seam?.32f:.55f;}
            else if(kind=="brick")
            {float row=Mathf.Floor(v*6),ex=Edge(u*3+(row%2)*.5f),ey=Edge(v*6);bool seam=ex<.018f||ey<.035f;float tone=Mathf.PerlinNoise(Mathf.Floor(u*3+row*.5f)*2,row*7);
             level=seam?.84f:.94f+tone*.05f+(noise-.5f)*.025f;h=seam?.28f:.60f+(noise-.5f)*.05f;}
            else if(kind=="tile")
            {bool seam=Edge(u*2)<.009f||Edge(v*2)<.009f;level=seam?.87f:.97f+(noise-.5f)*.018f;h=seam?.36f:.56f;}
            else if(kind=="terrazzo")
            {float fleck=Mathf.PerlinNoise(u*180+32,v*180+9);level=.96f+(noise-.5f)*.035f-(fleck>.67f?.20f:0)+(fleck<.29f?.025f:0);h=.5f;}
            else if(kind=="parquet")
            {float cx=Mathf.Floor(u*2),cy=Mathf.Floor(v*2);bool turn=((int)(cx+cy)%2)==0;float a=turn?u:v,b=turn?v:u;bool seam=Edge(b*8)<.012f||Edge(a*2)<.006f;level=seam?.84f:.95f+Mathf.Sin(b*700+Mathf.Sin(a*12)*3)*.014f+(Mathf.PerlinNoise(cx+2,cy+7)-.5f)*.045f;h=seam?.34f:.54f;}
            else if(kind=="checker")
            {bool seam=Edge(u*2)<.008f||Edge(v*2)<.008f;bool alt=((int)Mathf.Floor(u*2)+(int)Mathf.Floor(v*2))%2==0;level=seam?.85f:alt?.97f:.89f;h=seam?.36f:.55f;}
            else if(kind=="smalltile"||kind=="vinyl")
            {float n=kind=="smalltile"?8:2;bool seam=Edge(u*n)<.015f||Edge(v*n)<.015f;level=seam?.78f:.96f+(noise-.5f)*.025f;h=seam?.32f:.52f;}
            else if(kind=="cork")
            {float fleck=Mathf.PerlinNoise(u*200,v*200);level=.93f+(fleck-.5f)*.24f+(noise-.5f)*.035f;h=.5f+(fleck-.5f)*.08f;}
            else if(kind=="carpet"||kind=="rug")
            {float weave=Mathf.Sin(x*2.0944f)*Mathf.Cos(y*2.0944f);level=.95f+weave*.012f+(noise-.5f)*.045f;h=.5f+weave*.035f;}
            else if(kind=="epoxy"||kind=="concrete")
            {level=.95f+(Mathf.PerlinNoise(u*8,v*8)-.5f)*(kind=="concrete"?.10f:.025f)+(noise-.5f)*.025f;h=.5f+(noise-.5f)*.015f;}
            else if(kind=="acoustic")
            {bool joint=Edge(u*3)<.015f;bool hole=Mathf.Pow(Mathf.Repeat(u*40,1)-.5f,2)+Mathf.Pow(Mathf.Repeat(v*40,1)-.5f,2)<.028f;level=joint?.68f:hole?.74f:.97f;h=hole?.25f:.55f;}
            else if(kind=="wood")
            {float row=Mathf.Floor(v*6);bool seam=Edge(v*6)<.018f||Edge(u*2+Mathf.Floor(row)*.37f)<.008f;float grain=Mathf.Sin(v*510+Mathf.PerlinNoise(u*5,row+1)*8);
             level=seam?.82f:.95f+grain*.018f+Mathf.PerlinNoise(u*2,row*8)*.035f;h=seam?.30f:.55f+grain*.025f;}
            else if(kind=="timber")
            {float wave=Mathf.Sin(u*730+Mathf.PerlinNoise(u*5,v*2)*5);level=.94f+wave*.035f+(noise-.5f)*.02f;h=.5f+wave*.035f;}
            else if(kind=="glass"){level=.985f+(noise-.5f)*.004f;h=.5f;}
            else if(kind=="fabric")
            {float weave=Mathf.Sin(x*Mathf.PI/2)*Mathf.Sin(y*Mathf.PI/2);level=.95f+weave*.025f+(noise-.5f)*.035f;h=.5f+weave*.12f;}
            else if(kind=="wallpaper")
            {float weave=Mathf.Sin(x*Mathf.PI/2)*Mathf.Sin(y*Mathf.PI/2);float diamond=Mathf.Abs(Mathf.Sin(u*4*Mathf.PI))+Mathf.Abs(Mathf.Sin(v*4*Mathf.PI));level=.97f+weave*.008f-(diamond>1.65f?.025f:0);h=.5f+weave*.06f;}
            else if(kind=="metal"){level=.96f+(Mathf.PerlinNoise(u*4,v*240)-.5f)*.045f;h=.5f+(noise-.5f)*.05f;}
            else if(kind=="roof"){bool seam=Edge(u*7)<.04f||Edge(v*7)<.022f;level=seam?.70f:.92f+Mathf.Cos(u*14*Mathf.PI)*.05f;h=seam?.1f:.5f+Mathf.Cos(u*14*Mathf.PI)*.25f;}
            byte c=(byte)Mathf.Clamp(Mathf.RoundToInt(level*255),0,255),hh=(byte)Mathf.RoundToInt(h*255);rgb[y*size+x]=new Color32(c,c,c,255);height[y*size+x]=new Color32(hh,hh,hh,255);
        }
        if(kind=="meadow")
        {
            var random=new System.Random(1611);
            for(int i=0;i<4400;i++)
            {
                float bx=(float)random.NextDouble()*size,by=(float)random.NextDouble()*size;
                float length=5+(float)random.NextDouble()*13,lean=((float)random.NextDouble()-.5f)*8;
                float tone=.80f+(float)random.NextDouble()*.25f;
                for(int t=0;t<(int)length;t++)
                {
                    float f=t/length;int xx=Mathf.RoundToInt(bx+lean*f*f),yy=Mathf.RoundToInt(by+t);
                    for(int k=-1;k<=1;k++)
                    {
                        if(f>.72f&&k!=0)continue;int x=(xx+k+size)%size,y=(yy+size)%size,index=y*size+x;
                        float brightness=tone+(k==0?.10f:-.055f)+f*.055f;
                        byte c=(byte)Mathf.Clamp(brightness*255,0,255);
                        rgb[index]=new Color32(c,(byte)Mathf.Min(255,c+4),(byte)Mathf.Max(0,c-8),255);
                        byte z=(byte)Mathf.RoundToInt((.48f+(1-f)*.23f*(k==0?1:.4f))*255);height[index]=new Color32(z,z,z,255);
                    }
                }
            }
        }
        Texture2D Make(Color32[] pixels,string label,bool linear){var t=new Texture2D(size,size,TextureFormat.RGBA32,true,linear){name="Detail512_"+kind+label,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=8};t.SetPixels32(pixels);t.Apply(true,true);return t;}
        found=new[]{Make(rgb,"_albedo",false),Make(height,"_height",true)};textures[kind]=found;return found;
    }
}
