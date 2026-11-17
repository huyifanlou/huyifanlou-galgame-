using System;using System.Collections.Generic;using System.IO;using UnityEngine;
public static class PrecisionAudit
{
    [Serializable] public class Region{public string name;public int buildings,renderers;public long triangles;}
    [Serializable] public class Room{public string id;public int renderers,exitDoors,colliders;public float ceiling;public long triangles;}
    [Serializable] public class Report{public int legacyFacades,oldNightOverlays,missingMaterials,invalidBounds,duplicateBuildings,closedInteractionPoints;public Region[] regions;public Room[] rooms;}
    public static void Dump(WorldBuilder w,InteriorManager interiors,string path)
    {
        var report=new Report();var ids=new HashSet<string>();var regions=new Dictionary<string,Region>();
        string Category(string id)=>id.StartsWith("s_")?"学校":id.StartsWith("r_")?"居民区":id.StartsWith("o_")?"老街":id.StartsWith("st_")?"车站":id.StartsWith("k_")?"神社":id.StartsWith("f_")?"游乐场封闭保留":"公园";
        long Triangles(Transform t){long n=0;foreach(var f in t.GetComponentsInChildren<MeshFilter>(true)){var r=f.GetComponent<Renderer>();if(r==null||!r.enabled||f.sharedMesh==null)continue;for(int i=0;i<f.sharedMesh.subMeshCount;i++)n+=f.sharedMesh.GetIndexCount(i)/3;}return n;}
        var buildings=w.Root.Find("Buildings");foreach(var b in w.W.BUILDINGS)
        {
            if(!ids.Add(b.id))report.duplicateBuildings++;string category=Category(b.id);if(!regions.TryGetValue(category,out var region))regions[category]=region=new Region{name=category};region.buildings++;
            var root=buildings.Find("b_"+b.id);if(root==null){report.invalidBounds++;continue;}region.renderers+=root.GetComponentsInChildren<MeshRenderer>(true).Length;region.triangles+=Triangles(root);
        }
        foreach(var r in w.Root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if(!r.enabled)continue;
            var bounds=r.bounds;if(float.IsNaN(bounds.center.x)||float.IsNaN(bounds.size.y))report.invalidBounds++;
            foreach(var m in r.sharedMaterials)
            {if(m==null||m.shader==null||!m.shader.isSupported){report.missingMaterials++;continue;}if(m.name.StartsWith("wall_")||m.name.StartsWith("wing_"))report.legacyFacades++;}
        }
        if(w.SignRoot!=null)foreach(Transform child in w.SignRoot)if(child.name.StartsWith("fx_"))report.oldNightOverlays++;
        foreach(var spot in w.W.SPOTS)if(Array.Exists(w.W.BUILDINGS,b=>b.id.StartsWith("f_")&&b.name==spot.name)||spot.name=="游乐场"||spot.name=="摩天轮")report.closedInteractionPoints++;
        var rooms=new List<Room>();foreach(string id in new[]{"home","school","library","restaurant","supermarket","canteen","dorm","auditorium","labs","milktea","photo","book","conv"})
        {var it=interiors.Find(id);int doors=0;foreach(var t in it.Root.GetComponentsInChildren<Transform>(true))if(t.name=="PrecisionExitDoor")doors++;rooms.Add(new Room{id=id,renderers=it.Root.GetComponentsInChildren<MeshRenderer>(true).Length,exitDoors=doors,ceiling=it.CeilingY,triangles=Triangles(it.Root.transform),colliders=it.Solids.Count});}
        report.regions=new List<Region>(regions.Values).ToArray();report.rooms=rooms.ToArray();var absolute=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(absolute));File.WriteAllText(absolute,JsonUtility.ToJson(report,true));
        int errors=report.legacyFacades+report.oldNightOverlays+report.missingMaterials+report.invalidBounds+report.duplicateBuildings+report.closedInteractionPoints;
        Debug.Log($"[PrecisionAudit] {(errors==0?"PASS":"FAIL")} 旧立面 {report.legacyFacades}，旧夜窗 {report.oldNightOverlays}，重复建筑 {report.duplicateBuildings}，封闭区交互点 {report.closedInteractionPoints}，材质/边界异常 {report.missingMaterials+report.invalidBounds} → {absolute}");
    }
}
