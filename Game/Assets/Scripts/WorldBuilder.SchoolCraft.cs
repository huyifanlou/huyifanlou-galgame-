using UnityEngine;

public partial class WorldBuilder
{
    void SchoolCraftExterior(Transform root,RegionMeshBatch batch,BuildingDef b)
    {
        if(b.id!="s_teach")return;
        float bottom=-b.size[1]/2,front=b.size[2]/2,doorZ=front+b.EntranceExtra;
        var stone=DetailSurface.Mat("stone","#d4c6ad");var dark=DetailSurface.Mat("metal","#52635e");var brass=DetailSurface.Mat("metal","#ab9870");
        // 门廊柱础、柱头与侧槽都在已有柱体碰撞范围内。
        foreach(var post in SchoolExterior.Posts(b))
        {
            batch.Bevel(stone,post+new Vector3(0,bottom+.12f,0),new Vector3(.44f,.24f,.44f),.025f);
            batch.Bevel(stone,post+new Vector3(0,bottom+3.15f,0),new Vector3(.42f,.18f,.42f),.02f);
            foreach(int side in new[]{-1,1})batch.Box(brass,post+new Vector3(side*.106f,bottom+1.68f,.02f),new Vector3(.009f,2.60f,.08f));
        }
        // 檐底格栅与浅嵌灯，不增加点光源开销。
        var light=DetailSurface.Mat("paint","#e9d8af");
        for(int i=0;i<9;i++)batch.Bevel(dark,new Vector3(-2.32f+i*.58f,bottom+3.20f,doorZ+.94f),new Vector3(.035f,.065f,1.72f),.006f);
        foreach(int side in new[]{-1,1})
        {
            batch.Bevel(brass,new Vector3(side*1.72f,bottom+1.63f,doorZ+.19f),new Vector3(.44f,.70f,.07f),.015f);
            batch.Bevel(stone,new Vector3(side*1.72f,bottom+1.63f,doorZ+.235f),new Vector3(.37f,.62f,.025f),.009f);
            for(int i=0;i<5;i++)batch.Box(dark,new Vector3(side*1.72f,bottom+1.76f-i*.075f,doorZ+.251f),new Vector3(.25f,.014f,.005f));
            batch.Bevel(light,new Vector3(side*1.2f,bottom+3.155f,doorZ+.90f),new Vector3(.52f,.025f,.29f),.008f);
        }
        // 中央窗间留白改为有立柱、压顶的钟面构图，避开两侧真实窗洞。
        float start=bottom+4.65f,end=b.size[1]/2-.40f;
        foreach(int side in new[]{-1,1})
        {
            batch.Bevel(stone,new Vector3(side*1.27f,(start+end)/2,front+.20f),new Vector3(.16f,end-start,.16f),.012f);
            batch.Bevel(stone,new Vector3(side*1.27f,start,front+.23f),new Vector3(.26f,.18f,.22f),.012f);
        }
        batch.Bevel(stone,new Vector3(0,end,front+.20f),new Vector3(2.80f,.20f,.20f),.015f);
        Debug.Log("[SchoolCraft] 教学楼门廊柱础、檐底格栅、铜牌和钟面构图完成");
    }
}
