using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 一个实体内按材质合并；不跨实体合并，保留区域排查与逐栋替换能力。
public sealed class RegionMeshBatch
{
    readonly Dictionary<Material, List<CombineInstance>> parts = new Dictionary<Material, List<CombineInstance>>();
    static Mesh cube, cylinder, taper, twig;
    public void Add(Material material, Mesh mesh, Vector3 position, Vector3 scale, Quaternion rotation)
    {
        if (!parts.TryGetValue(material, out var list)) parts[material] = list = new List<CombineInstance>();
        list.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(position, rotation, scale) });
    }
    public void Box(Material material, Vector3 position, Vector3 size, float yaw = 0f)
    {
        if (cube == null) cube = MeshFactory.BoxUV(Vector3.one, 1f);
        Add(material, cube, position, size, Quaternion.Euler(0,yaw,0));
    }
    public void Bevel(Material material,Vector3 position,Vector3 size,float radius=.018f,float yaw=0,int steps=2)
    {Add(material,PrecisionMesh.Box(size,radius,steps),position,Vector3.one,Quaternion.Euler(0,yaw,0));}
    public void Cylinder(Material material,Vector3 from,Vector3 to,float radius)
    {Add(material,PrecisionMesh.Cylinder,from,new Vector3(radius,(to-from).magnitude,radius),Quaternion.FromToRotation(Vector3.up,to-from));}
    public void Branch(Material material, Vector3 from, Vector3 to, float radius, bool tapered = true, bool pointed = false)
    {
        if (cylinder == null) cylinder = Frustum(1f, 1f, 12);
        if (taper == null) taper = Frustum(1f, 0.55f, 10);
        if (twig == null) twig = Frustum(1f, 0.035f, 10);
        Add(material, pointed ? twig : tapered ? taper : cylinder, from, new Vector3(radius,(to-from).magnitude,radius),
            Quaternion.FromToRotation(Vector3.up,to-from));
    }
    public void Flush(Transform parent)
    {
        foreach (var entry in parts)
        {
            var mesh = new Mesh { name = parent.name + "/" + entry.Key.name, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(entry.Value.ToArray(),true,true);
            mesh.RecalculateBounds();
            var go = new GameObject(entry.Key.name);
            go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = entry.Key;
        }
        parts.Clear();
    }

    // 闭合网格，面朝外；屋顶侧面不再依赖两片相交薄板。
    public static Mesh Gable(float width, float rise, float depth)
    {
        var p = new[] { new Vector3(-width/2,0,-depth/2),new Vector3(width/2,0,-depth/2),new Vector3(0,rise,-depth/2),
            new Vector3(-width/2,0,depth/2),new Vector3(width/2,0,depth/2),new Vector3(0,rise,depth/2) };
        return Closed(p,new[]{new[]{0,2,1},new[]{3,4,5},new[]{0,1,4,3},new[]{0,3,5,2},new[]{1,2,5,4}},new Vector3(0,rise/3,0));
    }
    static Mesh Frustum(float bottom, float top, int sides)
    {
        var p = new Vector3[sides*2];
        var faces = new List<int[]>();
        var cap0 = new int[sides]; var cap1 = new int[sides];
        for(int i=0;i<sides;i++)
        {
            float a=i*2*Mathf.PI/sides;
            p[i]=new Vector3(Mathf.Cos(a)*bottom,0,Mathf.Sin(a)*bottom);
            p[i+sides]=new Vector3(Mathf.Cos(a)*top,1,Mathf.Sin(a)*top);
            faces.Add(new[]{i,(i+1)%sides,(i+1)%sides+sides,i+sides});
            cap0[i]=i; cap1[i]=i+sides;
        }
        faces.Add(cap0);faces.Add(cap1);
        return Closed(p,faces.ToArray(),Vector3.up*.5f);
    }
    static Mesh Closed(Vector3[] points,int[][] faces,Vector3 center)
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
        foreach(var face in faces)
        {
            int start=vertices.Count;
            Vector3 normal=Vector3.Cross(points[face[1]]-points[face[0]],points[face[2]]-points[face[0]]);
            bool reverse=Vector3.Dot(normal,points[face[0]]-center)<0;
            foreach(int index in face) {vertices.Add(points[index]);uv.Add(new Vector2(points[index].x,points[index].y));}
            for(int i=1;i<face.Length-1;i++) {triangles.Add(start);triangles.Add(start+(reverse?i+1:i));triangles.Add(start+(reverse?i:i+1));}
        }
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
}
