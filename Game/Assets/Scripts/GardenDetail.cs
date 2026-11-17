using System.Collections.Generic;
using UnityEngine;

// Each shrub is a branch-and-leaf assembly. Flowers are open cups with five folded petals.
public static class GardenDetail
{
    static Mesh flower;
    static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
    public static Material Mat(string color)
    {
        if(materials.TryGetValue(color,out var m))return m;
        m=MatLib.LitColor(MatLib.Hex(color));m.name="GardenLeaves_"+color;m.SetFloat("_VertexTint",1);m.SetFloat("_OutlineWidth",0);m.SetFloat("_Roughness",.83f);materials[color]=m;return m;
    }
    public static Mesh Flower
    {
        get
        {
            if(flower!=null)return flower;
            var v=new List<Vector3>();var n=new List<Vector3>();var colors=new List<Color>();var uv=new List<Vector2>();var tri=new List<int>();
            for(int p=0;p<5;p++)
            {
                var q=Quaternion.Euler(0,p*72,0);int b=v.Count;
                var points=new[]{new Vector3(0,0,.012f),new Vector3(-.045f,.018f,.055f),new Vector3(-.033f,.032f,.12f),new Vector3(.033f,.032f,.12f),new Vector3(.045f,.018f,.055f),new Vector3(0,.026f,.068f)};
                for(int k=0;k<6;k++){v.Add(q*points[k]);n.Add((q*new Vector3(0,1,-.18f)).normalized);uv.Add(new Vector2(points[k].x*4+.5f,points[k].z*6));float c=k==0?.70f:k==5?1:.90f;colors.Add(new Color(c,c,c,1));}
                for(int k=0;k<5;k++){int next=(k+1)%5;tri.Add(b+k);tri.Add(b+next);tri.Add(b+5);tri.Add(b+5);tri.Add(b+next);tri.Add(b+k);}
            }
            flower=new Mesh{name="FiveFoldedPetals"};flower.SetVertices(v);flower.SetNormals(n);flower.SetColors(colors);flower.SetUVs(0,uv);flower.SetTriangles(tri,0);flower.RecalculateBounds();return flower;
        }
    }
    public static void Shrub(RegionMeshBatch batch,Vector3 origin,int seed,string bloom=null,float scale=1)
    {
        var random=new System.Random(seed&0x7fffffff);float R()=>(float)random.NextDouble();
        var leaves=Mat(seed%3==0?"#7b8a62":"#617b52");var stem=DetailSurface.Mat("timber","#746b4c");
        for(int i=0;i<13;i++)
        {
            float a=i*2.39996f,r=Mathf.Sqrt(R())*.43f;var end=new Vector3(Mathf.Cos(a)*r,.32f+R()*.30f,Mathf.Sin(a)*r);
            batch.Branch(stem,origin+new Vector3(end.x*.18f,.025f,end.z*.18f)*scale,origin+end*scale,.012f*scale);
            batch.Add(leaves,NaturalTree.Spray(i,0),origin+end*scale,Vector3.one*.58f*scale,Quaternion.Euler(R()*30,R()*360,0));
            if(bloom!=null)
            for(int k=0;k<2;k++)
            {
                var pos=origin+(end+new Vector3((R()-.5f)*.22f,.07f+k*.05f,(R()-.5f)*.22f))*scale;
                var q=Quaternion.Euler(-25+R()*50,R()*360,-20+R()*40);
                batch.Add(Mat(bloom),Flower,pos,Vector3.one*scale*(.7f+R()*.4f),q);
                batch.Cylinder(Mat("#c7ad67"),pos,pos+q*Vector3.up*.036f*scale,.020f*scale);
            }
        }
    }
    public static void Wildflowers(RegionMeshBatch batch,Vector3 origin,int seed,int count=7)
    {
        for(int i=0;i<count;i++)
        {
            float a=i*2.39996f+seed,r=.12f+Mathf.Repeat(i*.137f,.34f);var top=origin+new Vector3(Mathf.Cos(a)*r,.24f+(i%4)*.065f,Mathf.Sin(a)*r);
            batch.Branch(Mat("#667c4e"),new Vector3(top.x,origin.y,top.z),top,.009f,false);
            batch.Add(Mat(i%3==0?"#e3d7c3":"#cc9fb0"),Flower,top,Vector3.one*(.65f+i%3*.12f),Quaternion.Euler(i*7,seed+i*51,12));
            batch.Add(Mat("#718455"),NaturalTree.Spray(i,0),Vector3.Lerp(origin,top,.45f),Vector3.one*.24f,Quaternion.Euler(0,i*79,20));
        }
    }
}
