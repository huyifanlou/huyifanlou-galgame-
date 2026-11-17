using System.Collections.Generic;using UnityEngine;

// 公用圆角网格；按实际尺寸缓存，UV 与法线独立于碰撞体。
public static class PrecisionMesh
{
    static readonly Dictionary<string,Mesh> cache=new Dictionary<string,Mesh>();
    public static Mesh StoneBox(Vector3 size,float uvMeters=1)=>Box(size,.028f,2,uvMeters);
    public static Mesh Box(Vector3 size,float radius=.018f,int steps=2,float uvMeters=1)
    {
        radius=Mathf.Min(radius,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.23f);
        if(radius<.001f)return MeshFactory.BoxUV(size,uvMeters);
        string key=size.ToString("F4")+"|"+radius.ToString("F4")+"|"+steps+"|"+uvMeters.ToString("F3");
        if(cache.TryGetValue(key,out var mesh))return mesh;
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();Vector3 half=size/2,inner=half-Vector3.one*radius;
        float[] Axis(float h){var list=new List<float>();for(int i=0;i<=steps;i++)list.Add(-h+radius*i/steps);for(int i=0;i<=steps;i++)list.Add(h-radius+radius*i/steps);return list.ToArray();}
        for(int face=0;face<6;face++)
        {
            Vector3 normal=face==0?Vector3.right:face==1?Vector3.left:face==2?Vector3.up:face==3?Vector3.down:face==4?Vector3.forward:Vector3.back;
            Vector3 a=face<2?Vector3.forward:Vector3.right,bdir=Vector3.Cross(normal,a);float ha=Mathf.Abs(Vector3.Dot(half,a)),hb=Mathf.Abs(Vector3.Dot(half,bdir));
            var aa=Axis(ha);var bb=Axis(hb);int start=vertices.Count;
            for(int j=0;j<bb.Length;j++)for(int i=0;i<aa.Length;i++)
            {var p=Vector3.Scale(normal,half)+a*aa[i]+bdir*bb[j];var core=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));var n=(p-core).normalized;vertices.Add(core+n*radius);normals.Add(n);uv.Add(new Vector2(aa[i]/uvMeters,bb[j]/uvMeters));}
            for(int j=0;j<bb.Length-1;j++)for(int i=0;i<aa.Length-1;i++)
            {int k=start+j*aa.Length+i;tris.AddRange(new[]{k,k+1,k+aa.Length+1,k,k+aa.Length+1,k+aa.Length});}
        }
        mesh=new Mesh{name="PrecisionBox_"+key};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();cache[key]=mesh;return mesh;
    }
    static Mesh cylinder;
    public static Mesh Cylinder
    {
        get{
            if(cylinder!=null)return cylinder;var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();const int sides=24;
            for(int i=0;i<sides;i++)
            {float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;var p=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var q=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));int k=v.Count;
             v.AddRange(new[]{p,p+Vector3.up,q+Vector3.up,q});n.AddRange(new[]{p,p,q,q});uv.AddRange(new[]{new Vector2(i/(float)sides,0),new Vector2(i/(float)sides,1),new Vector2((i+1)/(float)sides,1),new Vector2((i+1)/(float)sides,0)});t.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
             for(int cap=0;cap<2;cap++){k=v.Count;Vector3 up=Vector3.up*cap;v.AddRange(new[]{up,p+up,q+up});n.AddRange(new[]{cap==0?Vector3.down:Vector3.up,cap==0?Vector3.down:Vector3.up,cap==0?Vector3.down:Vector3.up});uv.AddRange(new[]{Vector2.zero,Vector2.zero,Vector2.zero});t.AddRange(cap==0?new[]{k,k+1,k+2}:new[]{k,k+2,k+1});}}
            cylinder=new Mesh{name="PrecisionCylinder24"};cylinder.SetVertices(v);cylinder.SetNormals(n);cylinder.SetUVs(0,uv);cylinder.SetTriangles(t,0);cylinder.RecalculateBounds();return cylinder;
        }
    }
}
