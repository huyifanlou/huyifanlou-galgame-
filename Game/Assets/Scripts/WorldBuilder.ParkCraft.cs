using System.Collections.Generic;
using UnityEngine;

public partial class WorldBuilder
{
    void CraftedBench(Transform root)
    {
        // Front is +Z, back -Z, matching TownData and the collision footprint.
        var b=new RegionMeshBatch();var wood=DetailSurface.Mat("timber","#a38a66");var steel=DetailSurface.Mat("metal","#495d58");var bolt=DetailSurface.Mat("metal","#9b9b87");
        for(int i=0;i<5;i++)b.Bevel(wood,new Vector3(0,.47f,-.20f+i*.10f),new Vector3(1.80f,.075f,.086f),.014f);
        for(int i=0;i<4;i++)b.Bevel(wood,new Vector3(0,.66f+i*.115f,-.265f-i*.023f),new Vector3(1.80f,.09f,.065f),.013f);
        foreach(int s in new[]{-1,1})
        {
            float x=s*.72f;b.Cylinder(steel,new Vector3(x,.06f,-.22f),new Vector3(x,.98f,-.34f),.027f);
            b.Cylinder(steel,new Vector3(x,.06f,.22f),new Vector3(x,.43f,.15f),.028f);
            b.Cylinder(steel,new Vector3(x,.41f,-.24f),new Vector3(x,.41f,.23f),.027f);
            b.Cylinder(steel,new Vector3(x,.45f,.15f),new Vector3(x,.71f,.15f),.021f);
            b.Bevel(wood,new Vector3(x,.72f,-.035f),new Vector3(.075f,.055f,.47f),.025f);
            foreach(int z in new[]{-1,1}){b.Bevel(steel,new Vector3(x,.03f,z*.22f),new Vector3(.17f,.06f,.15f),.018f);b.Cylinder(bolt,new Vector3(x,.51f,z*.19f),new Vector3(x,.517f,z*.19f),.014f);}
        }
        b.Flush(root);root.gameObject.AddComponent<ParkCraftMarker>();
    }
    // A closed lathed stone profile, including the inside wall of each hollow bowl.
    static Mesh Basin(float r,float y)
    {
        var profile=new[]{new Vector2(r-.33f,y-.26f),new Vector2(r-.19f,y-.03f),new Vector2(r-.07f,y+.04f),new Vector2(r,y+.02f),new Vector2(r+.025f,y-.07f),new Vector2(r-.05f,y-.17f),new Vector2(r-.22f,y-.34f)};
        var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();const int sides=96;
        for(int p=0;p<profile.Length;p++)for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Cos(a)*profile[p].x,profile[p].y,Mathf.Sin(a)*profile[p].x));uv.Add(new Vector2(i/(float)sides*8,p*.15f));}
        for(int p=0;p<profile.Length;p++)for(int i=0;i<sides;i++){int a=p*(sides+1)+i,c=((p+1)%profile.Length)*(sides+1)+i;t.AddRange(new[]{a,a+1,c,a+1,c+1,c});}
        var mesh=new Mesh{name="HollowCarvedBasin"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    void CraftedFountain(Transform root,float radius)
    {
        root.gameObject.AddComponent<ParkCraftMarker>();var b=new RegionMeshBatch();var stone=DetailSurface.Mat("stone","#c4c1ad");var bronze=DetailSurface.Mat("metal","#918369");
        float r=radius>1?radius:4.2f;
        b.Cylinder(stone,Vector3.zero,Vector3.up*.16f,r);
        b.Add(stone,Basin(r,.74f),Vector3.zero,Vector3.one,Quaternion.identity);
        EllipseWater(root,r-.30f,r-.30f,.57f);
        b.Branch(stone,new Vector3(0,.16f,0),new Vector3(0,2.65f,0),.54f);
        b.Add(stone,Basin(2.28f,2.68f),Vector3.zero,Vector3.one,Quaternion.identity);
        EllipseWater(root,1.97f,1.97f,2.51f);
        b.Branch(stone,new Vector3(0,2.5f,0),new Vector3(0,4.25f,0),.33f);
        b.Add(stone,Basin(1.16f,4.29f),Vector3.zero,Vector3.one,Quaternion.identity);
        EllipseWater(root,.85f,.85f,4.12f);
        for(int i=0;i<24;i++){float a=i*Mathf.PI/12;var q=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);b.Bevel(bronze,new Vector3(Mathf.Cos(a)*(r-.05f),.45f,Mathf.Sin(a)*(r-.05f)),new Vector3(.04f,.09f,.04f),.008f);}
        b.Cylinder(bronze,new Vector3(0,4.12f,0),new Vector3(0,4.85f,0),.07f);
        b.Add(GardenDetail.Mat("#d3b5be"),GardenDetail.Flower,new Vector3(0,4.85f,0),Vector3.one*7,Quaternion.Euler(0,18,0));
        // Thin curved water jets replace opaque vertical white boxes.
        for(int tier=0;tier<2;tier++)for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6;var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));float start=tier==0?1.02f:2.12f,from=tier==0?4.28f:2.67f,to=tier==0?2.51f:.57f;
            Vector3 previous=radial*start+Vector3.up*from;
            for(int k=1;k<=14;k++){float t=k/14f;var p=radial*(start+t*.37f)+Vector3.up*(Mathf.Lerp(from,to,t*t)+Mathf.Sin(t*Mathf.PI)*.06f);b.Cylinder(LitTex("water"),previous,p,.015f);previous=p;}
        }
        b.Flush(root);Spray(root,new Vector3(0,4.20f,0),.18f,12,2.6f,1.1f,28,.045f);
    }
    void CraftedPlanter(Transform root,float w,float d,bool green=false)
    {
        w=Mathf.Max(w,1);d=Mathf.Max(d,1);var b=new RegionMeshBatch();var stone=DetailSurface.Mat("stone","#aaa998");
        b.Box(DetailSurface.Mat("soil","#655b48"),new Vector3(0,.075f,0),new Vector3(w-.24f,.15f,d-.24f));
        foreach(int s in new[]{-1,1}){b.Bevel(stone,new Vector3(0,.105f,s*(d/2-.07f)),new Vector3(w,.21f,.14f),.025f);b.Bevel(stone,new Vector3(s*(w/2-.07f),.105f,0),new Vector3(.14f,.21f,d-.14f),.025f);}
        int nx=Mathf.Max(1,Mathf.FloorToInt((w-.3f)/.75f)),nz=Mathf.Max(1,Mathf.FloorToInt((d-.3f)/.7f));
        for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)GardenDetail.Shrub(b,new Vector3((x-(nx-1)*.5f)*(w-.7f)/nx,.15f,(z-(nz-1)*.5f)*(d-.7f)/nz),x*7+z,green?null:(x+z)%3==0?"#e0cbd1":"#c89cab",.65f);
        b.Flush(root);root.gameObject.AddComponent<ParkCraftMarker>();
    }
    void CraftedDeck(Transform root,float rx,float rz)
    {
        var deck=NewGo("pond_deck",root).transform;deck.localPosition=new Vector3(-10,0,rz+9.5f);var b=new RegionMeshBatch();var wood=DetailSurface.Mat("timber","#9a8969");var steel=DetailSurface.Mat("metal","#53675e");
        for(int i=0;i<27;i++)b.Bevel(wood,new Vector3(-3.12f+i*.24f,.16f,0),new Vector3(.226f,.16f,3.2f),.008f);
        foreach(int s in new[]{-1,1})
        {
            b.Bevel(steel,new Vector3(s*3.1f,.09f,0),new Vector3(.12f,.16f,3.15f),.012f);
            b.Cylinder(steel,new Vector3(s*3.07f,1.05f,-1.5f),new Vector3(s*3.07f,1.05f,1.5f),.032f);
            for(int i=0;i<4;i++)b.Cylinder(steel,new Vector3(s*3.07f,.24f,-1.5f+i),new Vector3(s*3.07f,1.05f,-1.5f+i),.026f);
        }
        for(int i=0;i<7;i++)b.Cylinder(steel,new Vector3(-3+i,.24f,1.50f),new Vector3(-3+i,1.05f,1.50f),.026f);
        b.Cylinder(steel,new Vector3(-3.07f,1.05f,1.5f),new Vector3(3.07f,1.05f,1.5f),.032f);b.Flush(deck);deck.gameObject.AddComponent<ParkCraftMarker>();
    }
    void CraftedShore(Transform root,float rx,float rz)
    {
        var shore=NewGo("CutStonePondCoping",root).transform;var b=new RegionMeshBatch();const int segments=128;
        for(int i=0;i<segments;i++)
        {
            float a=(i+.5f)*Mathf.PI*2/segments;var tangent=new Vector3(-(rx+.325f)*Mathf.Sin(a),0,(rz+.325f)*Mathf.Cos(a));
            float length=tangent.magnitude*Mathf.PI*2/segments-.017f;
            b.Bevel(DetailSurface.Mat("stone",i%4==0?"#a9ad9b":"#b7b9a5"),new Vector3(Mathf.Cos(a)*(rx+.325f),.09f,Mathf.Sin(a)*(rz+.325f)),new Vector3(length,.18f,.65f),.025f,Mathf.Atan2(-tangent.z,tangent.x)*Mathf.Rad2Deg);
        }
        b.Flush(shore);shore.gameObject.AddComponent<ParkCraftMarker>();
    }
}
public class ParkCraftMarker:MonoBehaviour{}
