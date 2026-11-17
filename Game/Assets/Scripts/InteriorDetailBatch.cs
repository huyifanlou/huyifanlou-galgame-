using System.Collections.Generic;using UnityEngine;using UnityEngine.Rendering;
public static class InteriorDetailBatch
{
    public static void Combine(Interior room)
    {
        var groups=new Dictionary<Material,List<CombineInstance>>();var removed=new List<MeshRenderer>();long triangles=0;
        foreach(var renderer in room.Root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mesh=renderer.GetComponent<MeshFilter>();if(mesh==null||mesh.sharedMesh==null||renderer.GetComponent<TextMesh>()!=null||renderer.sharedMaterials.Length!=1)continue;
            var material=renderer.sharedMaterial;if(material==null)continue;if(!groups.TryGetValue(material,out var list))groups[material]=list=new List<CombineInstance>();
            list.Add(new CombineInstance{mesh=mesh.sharedMesh,transform=room.Root.transform.worldToLocalMatrix*renderer.localToWorldMatrix});triangles+=mesh.sharedMesh.triangles.Length/3;removed.Add(renderer);
        }
        var root=new GameObject("PrecisionInteriorMeshes");root.transform.SetParent(room.Root.transform,false);
        foreach(var item in groups)
        {var mesh=new Mesh{name=room.Id+"_"+item.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(item.Value.ToArray(),true,true);mesh.RecalculateBounds();mesh.UploadMeshData(true);var go=new GameObject(item.Key.name);go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=item.Key;}
        foreach(var renderer in removed)
        {
            renderer.enabled=false;var go=renderer.gameObject;Object.Destroy(renderer);Object.Destroy(go.GetComponent<MeshFilter>());
            if(go.GetComponent<Collider>()==null&&go.transform.childCount==0)Object.Destroy(go);else go.name="collision_"+go.name;
        }
        Debug.Log($"[PrecisionInterior] {room.Id} 原零件 {removed.Count} → 合并材质组 {groups.Count}，三角形 {triangles}，层高 {room.CeilingY:F2}");
    }
}
