using UnityEngine;
public static partial class InteriorBuilder
{
    static void ClassroomRenewal(Transform p,Interior it,float west,float east,float back,float front,float teacher)
    {
        var b=new RegionMeshBatch();var wood=DetailSurface.Mat("timber","#b29c79");var steel=DetailSurface.Mat("metal","#7e8c85");var paper=DetailSurface.Mat("linenwall","#e4dfcf");
        float width=east-west;
        int bays=Mathf.FloorToInt((width-1)/.72f);
        for(int i=0;i<bays;i++)
        {
            float x=west+.55f+(i+.5f)*(width-1.1f)/bays;
            b.Bevel(wood,new Vector3(x,.52f,back+.33f),new Vector3((width-1.1f)/bays-.025f,1.04f,.48f),.015f);
            b.Box(DetailSurface.Mat("paint",i%2==0?"#81978c":"#a8b2a0"),new Vector3(x,.53f,back+.585f),new Vector3((width-1.1f)/bays-.075f,.91f,.025f));
            b.Cylinder(steel,new Vector3(x+.16f,.53f,back+.61f),new Vector3(x+.16f,.53f,back+.64f),.018f);
            for(int book=0;book<3;book++)b.Bevel(DetailSurface.Mat("fabric",book==0?"#ad8a76":"#8d9d99"),new Vector3(x-.14f+book*.12f,1.12f+book*.035f,back+.31f),new Vector3(.25f,.055f,.30f),.006f);
        }
        Reg(it,(west+east)/2,back+.33f,width-1.1f,.48f);
        b.Bevel(wood,new Vector3((west+east)/2,2.30f,back+.08f),new Vector3(width*.63f,1.15f,.075f),.015f);
        for(int i=0;i<7;i++){float x=(west+east)/2+(i-3)*width*.072f;b.Box(paper,new Vector3(x,2.3f,back+.13f),new Vector3(width*.058f,.65f,.012f));for(int line=0;line<4;line++)b.Box(steel,new Vector3(x,2.5f-line*.13f,back+.142f),new Vector3(width*.042f,.012f,.006f));}
        b.Bevel(steel,new Vector3(teacher,.985f,front-.85f),new Vector3(.62f,.035f,.39f),.018f);
        b.Bevel(DetailSurface.Mat("glass","#455b62"),new Vector3(teacher,1.20f,front-.98f),new Vector3(.58f,.40f,.025f),.012f);
        foreach(float x in new[]{west+.17f,east-.17f})for(int i=0;i<4;i++)b.Bevel(steel,new Vector3(x,3.63f,back+1+i*2.8f),new Vector3(.055f,.16f,1.1f),.01f);
        b.Flush(p);
        SignText(p,new Vector3((west+east)/2,3.05f,back+.15f),180,"学习园地 · 班级记事",.035f,MatLib.Hex("#435951"));
    }
}
