using System.Collections.Generic;
using UnityEngine;

public static partial class InteriorBuilder
{
    static bool StyledChair(Transform root)
    {
        var p=InteriorMood.Current;bool cafe=p.Id=="milktea",dining=p.Id=="restaurant",reading=p.Id=="library"||p.Id=="book",studio=p.Id=="photo";
        if(!cafe&&!dining&&!reading&&!studio)return false;
        var batch=new RegionMeshBatch();var legs=DetailSurface.Mat(studio?"metal":"timber",studio?p.Metal:p.Wood);var cloth=DetailSurface.Mat("fabric",p.Chair);
        batch.Bevel(legs,new Vector3(0,.37f,0),new Vector3(.44f,.07f,.43f),.018f);
        batch.Bevel(cloth,new Vector3(0,.44f,.01f),new Vector3(.49f,.10f,.47f),.04f,0,3);
        batch.Bevel(cloth,new Vector3(0,.74f,-.19f),new Vector3(.48f,dining?.43f:.35f,.12f),.045f,0,3);
        foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            batch.Cylinder(legs,new Vector3(sx*.205f,.025f,sz*.19f),new Vector3(sx*.16f,.39f,sz*.15f),studio?.018f:.026f);
        if(cafe)
        {
            foreach(int side in new[]{-1,1})batch.Bevel(cloth,new Vector3(side*.216f,.58f,-.03f),new Vector3(.065f,.16f,.30f),.025f);
        }
        if(reading)
        {
            var stitch=DetailSurface.Mat("fabric","#bdb092");
            foreach(int side in new[]{-1,1})batch.Cylinder(stitch,new Vector3(side*.197f,.60f,-.123f),new Vector3(side*.197f,.86f,-.123f),.003f);
        }
        if(dining)foreach(float xx in new[]{-.12f,0,.12f})batch.Cylinder(legs,new Vector3(xx,.67f,-.125f),new Vector3(xx,.83f,-.125f),.006f);
        batch.Flush(root);return true;
    }
    static void StyleArchitecture(Interior room)
    {
        var p=InteriorMood.Current;var root=new GameObject("RoomStyle_"+p.Id).transform;root.SetParent(room.Root.transform,false);
        var batch=new RegionMeshBatch();float cx=(room.X1+room.X2)/2,cz=(room.Z1+room.Z2)/2,ceil=room.CeilingY;
        var wood=DetailSurface.Mat("timber",p.Wood);var metal=DetailSurface.Mat("metal",p.Metal);var accent=DetailSurface.Mat("fabric",p.Accent);
        if(p.Id=="library")
        {
            // 深木低护墙与分格，窗口下沿以上留给原窗和书架。
            foreach(float z in new[]{room.Z1+.20f,room.Z2-.20f})
            {
                float a=room.X1+.25f,b=room.X2-.25f;
                foreach(var range in new[]{new Vector2(a,cx-1.48f),new Vector2(cx+1.48f,b)})
                {
                    batch.Bevel(wood,new Vector3((range.x+range.y)/2,.51f,z),new Vector3(range.y-range.x,.98f,.065f),.012f);
                    batch.Bevel(metal,new Vector3((range.x+range.y)/2,1.025f,z),new Vector3(range.y-range.x,.035f,.09f),.008f);
                }
                for(float x=a+.25f;x<b;x+=1.2f)if(Mathf.Abs(x-cx)>1.55f)batch.Bevel(wood,new Vector3(x,.52f,z+(z<cz?.045f:-.045f)),new Vector3(.055f,.91f,.025f),.008f);
            }
        }
        else if(p.Id=="restaurant")
        {
            // 后场砖墙与铜色竖向分隔，餐厅不再使用住宅花墙纸。
            batch.Bevel(DetailSurface.Mat("brick","#b38c72"),new Vector3(cx,1.60f,room.Z2-.22f),new Vector3(8.8f,2.8f,.045f),.008f);
            foreach(int side in new[]{-1,1})batch.Bevel(metal,new Vector3(cx+side*4.45f,1.60f,room.Z2-.18f),new Vector3(.06f,2.85f,.07f),.01f);
            SignText(root,new Vector3(cx-2.2f,2.65f,room.Z2-.25f),0,"四季餐桌",.026f,MatLib.Hex("#eee0c7"));
        }
        else if(p.Id=="milktea")
        {
            MoodArch(batch,new Vector3(cx,0,room.Z1+.21f),3.4f,3.25f,DetailSurface.Mat("plaster","#c9a4a2"));
            SignText(root,new Vector3(cx,2.70f,room.Z1+.24f),180,"茶与片刻",.032f,MatLib.Hex("#fff3df"));
        }
        else if(p.Id=="photo")
        {
            // 黑色吊轨与可见的摄影筒灯，不新增实时灯光。
            for(int row=-1;row<=1;row++)
            {
                float z=cz+row*4.0f;batch.Bevel(metal,new Vector3(cx,ceil-.24f,z),new Vector3(room.X2-room.X1-.8f,.08f,.09f),.012f);
                for(int i=-2;i<=2;i++)
                {float x=cx+i*4;batch.Cylinder(metal,new Vector3(x,ceil-.24f,z),new Vector3(x,ceil-.46f,z),.025f);batch.Cylinder(metal,new Vector3(x,ceil-.48f,z),new Vector3(x+.12f,ceil-.70f,z+.1f),.09f);batch.Cylinder(DetailSurface.Mat("paint","#eee9dd"),new Vector3(x+.12f,ceil-.70f,z+.1f),new Vector3(x+.13f,ceil-.72f,z+.11f),.074f);}
            }
        }
        else if(p.Id=="auditorium")
        {
            // 大厅高处的吸音片和铜色节奏，避开门与低处功能区。
            foreach(float x in new[]{room.X1+.24f,room.X2-.24f})
                for(float z=room.Z1+4;z<room.Z2-3;z+=4)
                {batch.Bevel(accent,new Vector3(x,5.60f,z),new Vector3(.10f,2.0f,2.1f),.03f);batch.Bevel(metal,new Vector3(x+(x<cx?.07f:-.07f),5.6f,z),new Vector3(.025f,1.8f,.035f),.006f);}
        }
        else if(p.Id=="labs")
        {
            foreach(int side in new[]{-1,1})
            {batch.Bevel(metal,new Vector3(cx+side*2.0f,ceil-.16f,cz),new Vector3(.30f,.13f,room.Z2-room.Z1-.8f),.016f);batch.Cylinder(DetailSurface.Mat("metal","#729b9d"),new Vector3(cx+side*2.4f,ceil-.20f,room.Z1+.5f),new Vector3(cx+side*2.4f,ceil-.20f,room.Z2-.5f),.045f);}
        }
        else if(p.Id=="supermarket"||p.Id=="conv")
        {
            var diffuser=DetailSurface.Mat("paint","#ebf0de");
            int count=p.Id=="conv"?2:4;
            for(int i=0;i<count;i++)
            {float x=Mathf.Lerp(room.X1+3,room.X2-3,(i+.5f)/count);batch.Bevel(metal,new Vector3(x,ceil-.10f,cz),new Vector3(.20f,.08f,room.Z2-room.Z1-1),.012f);batch.Bevel(diffuser,new Vector3(x,ceil-.15f,cz),new Vector3(.12f,.02f,room.Z2-room.Z1-1.1f),.005f);}
        }
        else if(p.Id=="book")
        {
            for(int i=0;i<3;i++)
            {
                float x=cx+(i-1)*6;float y=Mathf.Min(ceil-.65f,3.1f),z=room.Z1+2;
                batch.Bevel(accent,new Vector3(x,y,z),new Vector3(1.75f,.48f,.045f),.015f);
                foreach(int side in new[]{-1,1})batch.Cylinder(metal,new Vector3(x+side*.70f,y+.24f,z),new Vector3(x+side*.70f,ceil,z),.008f);
                SignText(root,new Vector3(x,y,z+.03f),180,new[]{"文学","艺术","日常"}[i],.023f,MatLib.Hex("#eee3ce"));
            }
        }
        batch.Flush(root);
    }
    static void MoodArch(RegionMeshBatch batch,Vector3 at,float width,float height,Material material)
    {
        float r=width/2,spring=height-r;var points=new List<Vector3>{new Vector3(-r,.10f,0),new Vector3(r,.10f,0)};
        for(int i=0;i<=24;i++){float a=i*Mathf.PI/24;points.Add(new Vector3(Mathf.Cos(a)*r,spring+Mathf.Sin(a)*r,0));}
        var verts=new List<Vector3>{new Vector3(0,height/2,0)};verts.AddRange(points);var tris=new List<int>();
        for(int i=0;i<points.Count;i++){tris.Add(0);tris.Add(i+1);tris.Add((i+1)%points.Count+1);}
        // 双面薄壁是墙上装饰底板，避免不同朝向的室内把背面剔掉。
        int front=tris.Count;for(int i=0;i<front;i+=3){tris.Add(tris[i]);tris.Add(tris[i+2]);tris.Add(tris[i+1]);}
        var mesh=new Mesh{name="TeaArch"};mesh.SetVertices(verts);mesh.SetTriangles(tris,0);var normals=new Vector3[verts.Count];for(int i=0;i<normals.Length;i++)normals[i]=Vector3.forward;mesh.normals=normals;mesh.RecalculateBounds();batch.Add(material,mesh,at,Vector3.one,Quaternion.identity);
    }
}
