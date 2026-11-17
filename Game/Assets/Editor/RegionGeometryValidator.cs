using System;
using System.Collections.Generic;
using UnityEngine;

// 区域重建后的几何不变量：闭合、绕序、有限坐标，以及铺装并集的面积/覆盖关系。
public static class RegionGeometryValidator
{
    public static void RunBatch()
    {
        CheckClosed(RegionMeshBatch.Gable(8,2,3));
        // Foliage is an intentionally open, double-sided leaf surface, no longer a closed crown solid.
        for(int kind=0;kind<3;kind++)
        {var foliage=NaturalTree.Spray(0,kind);foreach(var normal in foliage.normals)if(normal.sqrMagnitude<.2f||float.IsNaN(normal.x))throw new Exception("Invalid leaf normal");}
        var cross=new List<PropDef>{new PropDef{x=0,z=0,w=4,d=8},new PropDef{x=0,z=0,w=4,d=8,ry=Mathf.PI/2}};
        var mesh=PavingUnion.Build(cross);float area=MeshArea(mesh);
        if(Mathf.Abs(area-48)>.001f)throw new Exception("铺装并集面积错误："+area+"，应为 48");
        var world=TownData.Build();var school=new List<PropDef>();var grove=new List<PropDef>();
        foreach(var p in world.PROPS)
            if(p.type=="plaza"&&p.z<0)(SceneEntityAudit.InSchool(p.x,p.z)?school:grove).Add(p);
        CheckPaving(school);CheckPaving(grove);
        var residential=new List<PropDef>();
        foreach(var p in world.PROPS)if(p.type=="plaza"&&SceneEntityAudit.InResidential(p.x,p.z))residential.Add(p);
        var entries=ResidentialExterior.EntryPaths(world);residential.AddRange(entries);
        CheckPaving(residential);
        foreach(var path in entries)
            for(int sample=0;sample<=24;sample++)
            {
                float along=(sample/24f-.5f)*path.d;
                float x=path.x+Mathf.Sin(path.ry)*along,z=path.z+Mathf.Cos(path.ry)*along;
                foreach(var blocker in world.BLOCKERS)
                    if(Mathf.Abs(x-blocker.x)<blocker.hw+.35f&&Mathf.Abs(z-blocker.z)<blocker.hd+.35f)
                        throw new Exception($"居民区入户路被实体挡住 ({x:F2},{z:F2})");
                foreach(var tree in world.TREES)
                    if(Mathf.Abs(x-tree.x)<.75f&&Mathf.Abs(z-tree.z)<.75f)throw new Exception("居民区入户路压树干");
            }
        Debug.Log($"[RegionGeometry] 居民区 {entries.Count} 条入户路中心线碰撞抽检通过");
        var avatarRoot=new GameObject("AvatarVisibilityCheck");
        try
        {
            var avatar=PlayerAvatar.Create(avatarRoot.transform);
            avatar.SetVisible(false);avatar.SetCharacter(CharacterDef.JingNan);
            foreach(var renderer in avatar.GetComponentsInChildren<Renderer>(true))
                if(renderer.gameObject.activeInHierarchy)throw new Exception("第一人称换人后模型重新出现");
            avatar.SetVisible(true);
            if(avatar.GetComponentsInChildren<Renderer>().Length!=0)throw new Exception("已删除的角色模型重新出现");
            Debug.Log("[RegionGeometry] 玩家仅保留身份，旧显示调用不会恢复角色建模：通过");
        }
        finally { UnityEngine.Object.DestroyImmediate(avatarRoot); }
        Debug.Log("[RegionGeometry] PASS：三类开放叶片法线有效、山墙闭合；铺装并集面积与真实路径覆盖一致，无重叠面。");
    }
    static void CheckClosed(Mesh mesh)
    {
        var vertices=mesh.vertices;var normals=mesh.normals;var triangles=mesh.triangles;
        var welded=new Dictionary<Vector3Int,List<int>>();var points=new List<Vector3>();var edges=new Dictionary<string,int>();var directions=new Dictionary<string,int>();
        double volume=0;
        int Vertex(int index)
        {
            var v=vertices[index];
            if(float.IsNaN(v.x)||float.IsInfinity(v.x)||float.IsNaN(v.y)||float.IsInfinity(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.z))throw new Exception(mesh.name+" 坐标非有限");
            // 按实际距离焊接；毫米取整会把相邻的细小三角形误合并成非流形边。
            var key=new Vector3Int(Mathf.FloorToInt(v.x*10000),Mathf.FloorToInt(v.y*10000),Mathf.FloorToInt(v.z*10000));
            for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)
                if(welded.TryGetValue(key+new Vector3Int(x,y,z),out var candidates))
                    foreach(int candidate in candidates)if((points[candidate]-v).sqrMagnitude<1e-10f)return candidate;
            int result=points.Count;points.Add(v);
            if(!welded.TryGetValue(key,out var bucket))welded[key]=bucket=new List<int>();
            bucket.Add(result);return result;
        }
        for(int t=0;t<triangles.Length;t+=3)
        {
            int a=triangles[t],b=triangles[t+1],c=triangles[t+2];
            Vector3 cross=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]);
            volume+=Vector3.Dot(vertices[a],Vector3.Cross(vertices[b],vertices[c]))/6.0;
            if(cross.sqrMagnitude>1e-12f && Vector3.Dot(cross,normals[a]+normals[b]+normals[c])<-.00001f)throw new Exception($"{mesh.name} 面绕序反向：三角形 {t/3}，面积 {cross.magnitude/2}，法线点积 {Vector3.Dot(cross.normalized,(normals[a]+normals[b]+normals[c]).normalized)}，位置 {vertices[a]} / {vertices[b]} / {vertices[c]}");
            int[] indices={Vertex(a),Vertex(b),Vertex(c)};
            if(indices[0]==indices[1]||indices[1]==indices[2]||indices[2]==indices[0])continue;
            for(int e=0;e<3;e++)
            {
                int x=indices[e],y=indices[(e+1)%3];string key=Mathf.Min(x,y)+":"+Mathf.Max(x,y);
                edges.TryGetValue(key,out int n);edges[key]=n+1;
                directions.TryGetValue(key,out int direction);directions[key]=direction+(x<y?1:-1);
            }
        }
        int bad=0;foreach(var count in edges.Values)if(count!=2)bad++;
        if(bad>0)throw new Exception(mesh.name+" 非闭合边 "+bad);
        foreach(var direction in directions.Values)if(direction!=0)throw new Exception(mesh.name+" 相邻面绕序不一致");
        if(volume<=0)throw new Exception(mesh.name+" 总体法线朝内，体积 "+volume);
        Debug.Log($"[RegionGeometry] {mesh.name}：{vertices.Length} 顶点，{triangles.Length/3} 三角形，闭合边检查通过");
    }
    static float MeshArea(Mesh mesh)
    {
        var v=mesh.vertices;var t=mesh.triangles;float area=0;
        for(int i=0;i<t.Length;i+=3)area+=Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]).magnitude*.5f;
        return area;
    }
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static void CheckPaving(List<PropDef> paving)
    {
        var mesh=PavingUnion.Build(paving);var vertices=mesh.vertices;var triangles=mesh.triangles;
        var bounds=mesh.bounds;int samples=0;
        for(float x=bounds.min.x+.137f;x<bounds.max.x;x+=.67f)for(float z=bounds.min.z+.213f;z<bounds.max.z;z+=.67f)
        {
            bool expected=false,nearEdge=false;
            foreach(var p in paving)
            {
                float c=Mathf.Cos(p.ry),s=Mathf.Sin(p.ry),dx=x-p.x,dz=z-p.z;
                float a=Mathf.Abs(dx*c-dz*s)-p.w/2,b=Mathf.Abs(dx*s+dz*c)-p.d/2;
                if(a<=0&&b<=0)expected=true;
                if(Mathf.Abs(a)<.0005f||Mathf.Abs(b)<.0005f)nearEdge=true;
            }
            if(nearEdge)continue;
            int covering=0;var q=new Vector2(x,z);
            for(int i=0;i<triangles.Length;i+=3)
            {
                Vector3 av=vertices[triangles[i]],bv=vertices[triangles[i+1]],cv=vertices[triangles[i+2]];
                var a=new Vector2(av.x,av.z);var b=new Vector2(bv.x,bv.z);var c=new Vector2(cv.x,cv.z);
                float d0=Cross(b-a,q-a),d1=Cross(c-b,q-b),d2=Cross(a-c,q-c);
                if(d0<-.000001f&&d1<-.000001f&&d2<-.000001f)covering++;
                else if(Mathf.Abs(d0)<.000001f||Mathf.Abs(d1)<.000001f||Mathf.Abs(d2)<.000001f){nearEdge=true;break;}
            }
            if(nearEdge)continue;
            if(covering!=(expected?1:0))throw new Exception($"铺装覆盖错误 ({x},{z}) 预期 {expected}，覆盖 {covering} 次");
            samples++;
        }
        Debug.Log($"[RegionGeometry] 铺装 {paving.Count} 块 → {triangles.Length/3} 三角面，面积 {MeshArea(mesh):F2}m²，覆盖抽检 {samples} 点通过");
    }
}
