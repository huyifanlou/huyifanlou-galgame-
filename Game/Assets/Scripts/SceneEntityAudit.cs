using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 区域实体清单：只在命令行显式要求时输出，避免把“无报错”当成视觉验收。
public static class SceneEntityAudit
{
    [Serializable] public class Entity
    {
        public string category, name, label;
        public Vector3 position;
        public int renderers, missingMaterials;
        public string[] shaders, materials;
    }
    [Serializable] public class Report
    {
        public string region;
        public int renderers, missingMaterials, oldFacadeMaterials, oldWindowOverlayGroups;
        public List<Entity> entities = new List<Entity>();
    }

    public static bool InSchool(float x, float z) => x >= -125f && x <= -8f && z >= -104f && z <= -18f;
    public static bool InResidential(float x,float z) => x>=-105f && x<=-8f && z>=38f && z<=103f;

    public static void DumpResidential(WorldBuilder world,string path)
    {
        var report=new Report {region="居民区：r_* 建筑、主角家、道具、路灯、树木"};
        void Add(string category,Transform root)
        {
            if(root==null)return;
            var entity=new Entity {category=category,name=root.name,position=root.position};
            var sign=root.GetComponentInChildren<TextMesh>();if(sign!=null)entity.label=sign.text;
            var shaders=new HashSet<string>();var materials=new HashSet<string>();
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                entity.renderers++;
                foreach(var material in renderer.sharedMaterials)
                {
                    if(material==null||material.shader==null||!material.shader.isSupported){entity.missingMaterials++;continue;}
                    shaders.Add(material.shader.name);materials.Add(material.name);
                    if(material.name.StartsWith("wall_r_")||material.name.StartsWith("wing_r_"))report.oldFacadeMaterials++;
                }
            }
            entity.shaders=new List<string>(shaders).ToArray();entity.materials=new List<string>(materials).ToArray();
            report.renderers+=entity.renderers;report.missingMaterials+=entity.missingMaterials;report.entities.Add(entity);
        }
        var buildings=world.Root.Find("Buildings");
        foreach(var b in world.W.BUILDINGS)
        {
            if(!b.id.StartsWith("r_"))continue;
            Add("建筑",buildings!=null?buildings.Find("b_"+b.id):null);
            var fx=world.SignRoot!=null?world.SignRoot.Find("fx_"+b.id):null;
            if(fx!=null){report.oldWindowOverlayGroups++;Add("旧夜窗覆盖",fx);}
            if(world.SignRoot!=null)Add("实体店招",world.SignRoot.Find("sign_"+b.id));
        }
        Add("家",world.Root.Find("Villa"));
        foreach(string name in new[]{"Props","Lamps","Trees"})
        {
            var root=world.Root.Find(name);if(root==null)continue;
            foreach(Transform child in root)
                if(InResidential(child.position.x,child.position.z)||child.name=="ResidentialPaving")Add(name,child);
        }
        var absolute=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(absolute));
        File.WriteAllText(absolute,JsonUtility.ToJson(report,true));
        Debug.Log($"[ResidentialAudit] 渲染实体 {report.renderers}，旧立面引用 {report.oldFacadeMaterials}，旧夜窗组 {report.oldWindowOverlayGroups}，缺失材质 {report.missingMaterials} → {absolute}");
    }

    public static void DumpSchool(WorldBuilder world, string path)
    {
        var report = new Report { region = "学校：九栋校舍、校内道具、路灯、植被" };
        void Add(string category, Transform root)
        {
            if (root == null) return;
            var item = new Entity { category = category, name = root.name, position = root.position };
            var shaders = new HashSet<string>(); var materials = new HashSet<string>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                item.renderers++;
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null || !material.shader.isSupported)
                    { item.missingMaterials++; continue; }
                    shaders.Add(material.shader.name); materials.Add(material.name);
                    if (material.name.StartsWith("wall_s_")) report.oldFacadeMaterials++;
                }
            }
            item.shaders = new List<string>(shaders).ToArray();
            item.materials = new List<string>(materials).ToArray();
            report.renderers += item.renderers; report.missingMaterials += item.missingMaterials;
            report.entities.Add(item);
        }
        var buildings = world.Root.Find("Buildings");
        foreach (var building in world.W.BUILDINGS)
        {
            if (!building.id.StartsWith("s_")) continue;
            Add("校舍", buildings != null ? buildings.Find("b_" + building.id) : null);
            var fx = world.SignRoot != null ? world.SignRoot.Find("fx_" + building.id) : null;
            if (fx != null) { report.oldWindowOverlayGroups++; Add("旧亮窗覆盖片", fx); }
        }
        foreach (string category in new[] { "Props", "Lamps", "Trees" })
        {
            var root = world.Root.Find(category);
            if (root == null) continue;
            foreach (Transform child in root)
                if (child.name == "SchoolPaving" || InSchool(child.position.x, child.position.z)) Add(category, child);
        }
        var absolute = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute));
        File.WriteAllText(absolute, JsonUtility.ToJson(report, true));
        Debug.Log($"[SchoolAudit] 实体 {report.entities.Count}，渲染实体 {report.renderers}，缺失材质 {report.missingMaterials}，旧立面材质 {report.oldFacadeMaterials}，旧亮窗覆盖组 {report.oldWindowOverlayGroups} → {absolute}");
    }
}
