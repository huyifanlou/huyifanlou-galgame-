using System.Collections.Generic;
using UnityEngine;

// 凸四边形逐块相减，得到不重叠的铺装面；可处理斜向林道和学校十字路口。
public static class PavingUnion
{
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static float Area(List<Vector2> p)
    {float area=0;for(int i=0;i<p.Count;i++)area+=Cross(p[i],p[(i+1)%p.Count]);return Mathf.Abs(area)*.5f;}
    static List<Vector2> Clip(List<Vector2> polygon,Vector2 a,Vector2 b,bool inside)
    {
        var result=new List<Vector2>();
        for(int i=0;i<polygon.Count;i++)
        {
            var p=polygon[i];var q=polygon[(i+1)%polygon.Count];
            float dp=Cross(b-a,p-a),dq=Cross(b-a,q-a);
            bool pin=inside?dp>=0:dp<=0,qin=inside?dq>=0:dq<=0;
            if(pin)result.Add(p);
            if(pin!=qin)result.Add(Vector2.Lerp(p,q,dp/(dp-dq)));
        }
        return result;
    }
    static List<List<Vector2>> Subtract(List<Vector2> subject,List<Vector2> cutter)
    {
        var result=new List<List<Vector2>>();var remaining=subject;
        for(int i=0;i<cutter.Count && remaining.Count>=3;i++)
        {
            Vector2 a=cutter[i],b=cutter[(i+1)%cutter.Count];
            var outside=Clip(remaining,a,b,false);
            if(outside.Count>=3 && Area(outside)>.00001f)result.Add(outside);
            remaining=Clip(remaining,a,b,true);
        }
        return result;
    }
    public static Mesh Build(List<PropDef> paving)
    {
        var covered=new List<List<Vector2>>();var polygons=new List<List<Vector2>>();
        foreach(var p in paving)
        {
            float c=Mathf.Cos(p.ry),s=Mathf.Sin(p.ry);
            var quad=new List<Vector2>();
            foreach(var v in new[]{new Vector2(-p.w/2,-p.d/2),new Vector2(p.w/2,-p.d/2),new Vector2(p.w/2,p.d/2),new Vector2(-p.w/2,p.d/2)})
                quad.Add(new Vector2(p.x+v.x*c+v.y*s,p.z-v.x*s+v.y*c));
            var pieces=new List<List<Vector2>>{quad};
            foreach(var cutter in covered)
            {
                var next=new List<List<Vector2>>();foreach(var piece in pieces)next.AddRange(Subtract(piece,cutter));pieces=next;
                if(pieces.Count==0)break;
            }
            polygons.AddRange(pieces);covered.Add(quad);
        }
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        foreach(var polygon in polygons)
        {
            int start=vertices.Count;
            foreach(var p in polygon){vertices.Add(new Vector3(p.x,.010f,p.y));uv.Add(p/TexScale.PathPeriod);}
            for(int i=1;i<polygon.Count-1;i++)triangles.AddRange(new[]{start,start+i+1,start+i});
        }
        var mesh=new Mesh{name="PavingUnion",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
}
