using UnityEngine;

// 教学楼专用近景资产；只替换旧部件，不在其上叠一套模型。
public static partial class InteriorBuilder
{
    static void CheckSchoolCraft(Interior room)
    {
        int desks=0,boards=0;
        foreach(var t in room.Root.GetComponentsInChildren<Transform>(true)){if(t.name=="StudyDesk")desks++;if(t.name=="TeachingBoard")boards++;}
        if(desks!=68||boards!=4)throw new System.Exception("学校精修实体数量异常："+desks+" 桌 / "+boards+" 黑板");
        foreach(float dx in new[]{-18.6f,-10.4f,.2f,14.6f})
            for(int step=0;step<=100;step++)
            {float z=Mathf.Lerp(1f,-10f,step/100f);if(room.Collides(SCHOOL_X+dx,z,.02f))throw new System.Exception($"教室门口过道被挡：{dx:F1}, {z:F2}");}
        foreach(float dx in new[]{-22.4f,-6.5f,4.2f,18.75f})if(!room.Collides(SCHOOL_X+dx,-3.48f,.02f))throw new System.Exception("讲台碰撞缺失");
        Debug.Log("[SchoolCraftCheck] PASS 68 张新课桌、4 块黑板；4 条进门过道 404 点无阻挡；4 个讲台有碰撞");
    }
    static void StudyDesk(Transform parent,float x,float z,int variant)
    {
        var root=new GameObject("StudyDesk").transform;root.SetParent(parent,false);root.localPosition=new Vector3(x,0,z);
        var batch=new RegionMeshBatch();var oak=DetailSurface.Mat("timber","#baa080");var edge=DetailSurface.Mat("rubber","#766957");
        var steel=DetailSurface.Mat("metal","#7f918f");var cap=DetailSurface.Mat("rubber","#424b49");
        batch.Bevel(edge,new Vector3(0,.733f,0),new Vector3(.64f,.048f,.44f),.014f);
        batch.Bevel(oak,new Vector3(0,.757f,0),new Vector3(.617f,.022f,.417f),.009f);
        // 桌斗保留朝椅子一侧的真实开口。
        batch.Bevel(steel,new Vector3(0,.60f,.015f),new Vector3(.52f,.018f,.34f),.005f);
        foreach(int side in new[]{-1,1})
        {
            batch.Bevel(steel,new Vector3(side*.254f,.66f,.015f),new Vector3(.018f,.13f,.34f),.005f);
            foreach(int end in new[]{-1,1})
            {
                var foot=new Vector3(side*.256f,.03f,end*.164f);batch.Cylinder(steel,foot,foot+Vector3.up*.68f,.018f);
                batch.Bevel(cap,new Vector3(foot.x,.025f,foot.z),new Vector3(.047f,.045f,.047f),.008f);
            }
            batch.Cylinder(steel,new Vector3(side*.256f,.26f,-.164f),new Vector3(side*.256f,.26f,.164f),.012f);
        }
        batch.Bevel(steel,new Vector3(0,.66f,.18f),new Vector3(.52f,.13f,.018f),.005f);
        batch.Cylinder(steel,new Vector3(-.256f,.32f,.164f),new Vector3(.256f,.32f,.164f),.012f);
        if(variant%3!=0)
        {
            StudyBook(batch,new Vector3(-.08f,.773f,.025f),variant%2==0?"#738b8b":"#b48a75");
            var ink=DetailSurface.Mat("paint","#344958");
            batch.Cylinder(ink,new Vector3(.16f,.789f,-.11f),new Vector3(.19f,.789f,.035f),.005f);
            batch.Cylinder(steel,new Vector3(.159f,.789f,-.115f),new Vector3(.156f,.789f,-.13f),.003f);
        }
        if(variant%5==1)
        {
            var cloth=DetailSurface.Mat("fabric","#687b86");var seam=DetailSurface.Mat("fabric","#475963");
            batch.Bevel(cloth,new Vector3(.246f,.45f,-.025f),new Vector3(.13f,.24f,.20f),.035f);
            batch.Bevel(seam,new Vector3(.316f,.43f,-.025f),new Vector3(.016f,.12f,.15f),.006f);
            foreach(float zz in new[]{-.075f,.025f})batch.Cylinder(seam,new Vector3(.28f,.53f,zz),new Vector3(.26f,.68f,zz),.009f);
        }
        batch.Flush(root);
    }
    static void StudyBook(RegionMeshBatch batch,Vector3 at,string color)
    {
        var cover=DetailSurface.Mat("fabric",color);var paper=DetailSurface.Mat("paper","#e9e4d7");var ink=DetailSurface.Mat("paint","#afb4ac");
        batch.Bevel(cover,at+Vector3.up*.004f,new Vector3(.245f,.008f,.30f),.003f);
        batch.Bevel(paper,at+new Vector3(.005f,.016f,0),new Vector3(.228f,.018f,.283f),.003f);
        batch.Bevel(cover,at+Vector3.up*.029f,new Vector3(.245f,.008f,.30f),.003f);
        batch.Bevel(cover,at+new Vector3(-.119f,.017f,0),new Vector3(.009f,.026f,.30f),.003f);
        for(int i=0;i<3;i++)batch.Box(ink,at+new Vector3(.008f,.009f+i*.005f,-.142f),new Vector3(.216f,.0008f,.001f));
        batch.Box(paper,at+new Vector3(.023f,.034f,.045f),new Vector3(.12f,.001f,.074f));
        for(int i=0;i<3;i++)batch.Box(ink,at+new Vector3(.023f,.035f,.023f+i*.02f),new Vector3(.085f,.001f,.002f));
    }
    static void TeachingBoard(Transform parent,float x,float wallZ)
    {
        var root=new GameObject("TeachingBoard").transform;root.SetParent(parent,false);root.localPosition=new Vector3(x,0,wallZ);
        var batch=new RegionMeshBatch();var frame=DetailSurface.Mat("metal","#89978f");var green=DetailSurface.Mat("plaster","#304f49");var chalk=DetailSurface.Mat("paper","#e3e3cc");
        batch.Bevel(frame,new Vector3(0,1.83f,-.035f),new Vector3(5.28f,1.60f,.075f),.018f);
        batch.Bevel(green,new Vector3(0,1.83f,-.084f),new Vector3(5.08f,1.40f,.025f),.008f);
        batch.Bevel(frame,new Vector3(0,1.035f,-.16f),new Vector3(5.30f,.045f,.22f),.01f);
        for(int i=0;i<4;i++)batch.Cylinder(chalk,new Vector3(-1.9f+i*.14f,1.065f,-.17f),new Vector3(-1.82f+i*.14f,1.065f,-.17f),.009f);
        batch.Bevel(DetailSurface.Mat("fabric","#b08b65"),new Vector3(1.9f,1.075f,-.16f),new Vector3(.22f,.06f,.10f),.015f);
        // 图像区的坐标轴、函数折线，与文字在同一板面前留出厚度。
        void Line(float x1,float y1,float x2,float y2){batch.Cylinder(chalk,new Vector3(x1,y1,-.103f),new Vector3(x2,y2,-.103f),.006f);}
        Line(.60f,1.34f,2.22f,1.34f);Line(.82f,1.22f,.82f,2.33f);
        for(int i=0;i<12;i++){float a=-.4f+i*.115f,b=a+.115f;Line(1.30f+a,1.48f+a*a*.8f,1.30f+b,1.48f+b*b*.8f);}
        batch.Flush(root);
        SignText(root,new Vector3(-1.10f,2.23f,-.109f),0,"数学  ·  函数与图像",.021f,MatLib.Hex("#e3e3cc"));
        SignText(root,new Vector3(-1.12f,1.78f,-.109f),0,"y = ax² + bx + c\n先观察，再推理。",.018f,MatLib.Hex("#d5d8c4"));
    }
    static void SchoolDoorTrim(Transform parent,float center,float z,string label)
    {
        var wood=DetailSurface.Mat("timber","#8a7761");var batch=new RegionMeshBatch();
        foreach(int side in new[]{-1,1})batch.Bevel(wood,new Vector3(center+side*1.035f,1.25f,z),new Vector3(.10f,2.5f,.34f),.012f);
        batch.Bevel(wood,new Vector3(center,2.49f,z),new Vector3(2.17f,.12f,.34f),.012f);
        // 门洞保持敞开；不加横穿通道的门扇或碰撞。
        batch.Flush(parent);SignText(parent,new Vector3(center,2.75f,z+.24f),180,label,.024f,MatLib.Hex("#314e49"));
    }
    static void SchoolNoticeContent(Transform parent,float x,float z,int index)
    {
        var batch=new RegionMeshBatch();var paper=DetailSurface.Mat("paper","#e7dfca");var pin=DetailSurface.Mat("metal","#aa8b61");var ink=DetailSurface.Mat("paint","#8b9a94");
        for(int i=0;i<3;i++)
        {
            float px=x-.45f+i*.45f;batch.Bevel(paper,new Vector3(px,2.16f,z),new Vector3(.36f,.56f,.009f),.002f);
            batch.Cylinder(pin,new Vector3(px,2.40f,z+.006f),new Vector3(px,2.40f,z+.012f),.016f);
            for(int j=0;j<5;j++)batch.Box(ink,new Vector3(px,2.23f-j*.063f,z+.006f),new Vector3(j==4?.16f:.26f,.009f,.001f));
        }
        batch.Flush(parent);
        string[] titles={"校园公告","社团招新","阅读计划","值日安排","课程提示","校园活动","安全提示"};
        SignText(parent,new Vector3(x,2.55f,z+.015f),180,titles[index%titles.Length],.015f,MatLib.Hex("#3e5952"));
    }
    static void SchoolCraftLobby(Transform parent,float ox)
    {
        var batch=new RegionMeshBatch();var stone=DetailSurface.Mat("stone","#c6c0ae");var brass=DetailSurface.Mat("metal","#a39773");var oak=DetailSurface.Mat("timber","#93836a");
        // 门厅嵌地导向：薄条位于地板上方，避开出口地毯，不形成新的高度台阶。
        foreach(int side in new[]{-1,1})batch.Box(brass,new Vector3(ox+side*2.25f,.024f,7.3f),new Vector3(.025f,.004f,9.0f));
        // 前台原外形保留，正面增加内凹分格、石质台面、接缝与桌上物件。
        batch.Bevel(stone,new Vector3(ox+4.5f,1.28f,12.8f),new Vector3(3.18f,.055f,.81f),.014f);
        for(int i=0;i<13;i++)batch.Bevel(oak,new Vector3(ox+3.15f+i*.225f,.64f,12.432f),new Vector3(.095f,.96f,.035f),.008f);
        batch.Bevel(brass,new Vector3(ox+4.5f,.15f,12.426f),new Vector3(2.98f,.04f,.04f),.006f);
        StudyBook(batch,new Vector3(ox+5.35f,1.312f,12.80f),"#6c8781");
        batch.Bevel(DetailSurface.Mat("metal","#465b5d"),new Vector3(ox+3.65f,1.325f,12.8f),new Vector3(.26f,.035f,.18f),.01f);
        batch.Cylinder(brass,new Vector3(ox+3.65f,1.34f,12.8f),new Vector3(ox+3.65f,1.55f,12.8f),.015f);
        batch.Bevel(DetailSurface.Mat("paint","#e8e1cd"),new Vector3(ox+3.65f,1.60f,12.8f),new Vector3(.48f,.22f,.03f),.009f);
        batch.Flush(parent);SignText(parent,new Vector3(ox+3.65f,1.60f,12.778f),0,"来访登记",.015f,MatLib.Hex("#455c56"));
        // 主入口两侧竖向木饰面只占南墙连续墙段。
        foreach(int side in new[]{-1,1})
        {
            float x=ox+side*3.12f;PartC(parent,new Vector3(x,1.60f,14.75f),new Vector3(.32f,3.10f,.08f),oak,.015f);
        }
    }
}
