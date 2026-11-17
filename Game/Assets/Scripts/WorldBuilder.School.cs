using System.Collections.Generic;
using UnityEngine;

public partial class WorldBuilder
{
    public Material SchoolGlassLit, SchoolGlassDark;
    Material SchoolMat(string hex)
    {
        string key="School|"+hex;
        if(!_cache.TryGetValue(key,out var material))
        {
            material=MatLib.LitColor(MatLib.Hex(hex));material.name="school_"+hex.TrimStart('#');
            material.SetFloat("_OutlineWidth",0);material.SetFloat("_RimStrength",0);material.SetFloat("_SpecularStrength",0);
            _cache[key]=material;
        }
        return material;
    }
    void BuildSchoolBuilding(Transform root,BuildingDef b)=>BuildPrecisionArchitecture(root,b);

    bool RegionalPlaza(PropDef p) => W.RIVER!=null && p.type=="plaza" && (p.z<0 || SceneEntityAudit.InResidential(p.x,p.z));
    void BuildRegionalPaving(Transform parent)
    {
        var school=new List<PropDef>();var grove=new List<PropDef>();var residential=new List<PropDef>();
        foreach(var p in W.PROPS)
            if(RegionalPlaza(p)) (SceneEntityAudit.InSchool(p.x,p.z)?school:SceneEntityAudit.InResidential(p.x,p.z)?residential:grove).Add(p);
        residential.AddRange(_residentialEntryPaths);
        foreach(var region in new[]{school,grove,residential})
        {
            if(region.Count==0)continue;
            var go=MeshObj(parent,PavingUnion.Build(region),Vector3.zero,region==grove?DetailSurface.Mat("cobble","#b8ad96"):LitTex("path"),default,false);
            go.name=region==school?"SchoolPaving":region==grove?"GrovePaving":"ResidentialPaving";
        }
    }
    bool TryGardenProp(PropDef p,Transform root)
    {
        if(!SceneEntityAudit.InSchool(p.x,p.z) && !InCherryGrove(p.x,p.z) && !SceneEntityAudit.InResidential(p.x,p.z))return false;
        var batch=new RegionMeshBatch();var stone=SchoolMat("#b5ad99");var trim=SchoolMat("#d8d0bc");var metal=SchoolMat("#52666a");var wood=SchoolMat("#9b7958");
        switch(p.type)
        {
            case "lantern":
                StoneLantern(root);return true;
            case "bench":
                CraftedBench(root);return true;
            case "bikerack":
                for(int i=0;i<Mathf.Max(1,p.n);i++)
                {
                    float x=(i-(p.n-1)/2f)*.7f;
                    foreach(int s in new[]{-1,1})batch.Box(metal,new Vector3(x,.32f,s*.34f),new Vector3(.055f,.64f,.055f));
                    batch.Box(metal,new Vector3(x,.64f,0),new Vector3(.055f,.055f,.73f));
                }
                break;
            case "board":
                foreach(int s in new[]{-1,1})batch.Box(metal,new Vector3(s*.72f,.88f,0),new Vector3(.10f,1.76f,.10f));
                batch.Box(wood,new Vector3(0,1.45f,0),new Vector3(1.8f,1.10f,.14f));
                batch.Box(SchoolMat("#817f68"),new Vector3(0,1.45f,.085f),new Vector3(1.61f,.91f,.03f));
                for(int i=0;i<4;i++)
                {
                    float x=-.56f+i*.37f;
                    batch.Box(SchoolMat(i%2==0?"#ebe4d0":"#d2d9cd"),new Vector3(x,1.45f,.112f),new Vector3(.29f,.56f,.02f));
                    for(int k=0;k<3;k++)batch.Box(metal,new Vector3(x,1.57f-k*.13f,.129f),new Vector3(.18f,.014f,.01f));
                }
                batch.Box(metal,new Vector3(0,2.06f,.02f),new Vector3(2,.12f,.46f));break;
            case "planter":
                batch.Box(stone,new Vector3(0,.22f,0),new Vector3(p.w,.44f,p.d));
                batch.Box(SchoolMat("#655b47"),new Vector3(0,.455f,0),new Vector3(p.w-.16f,.03f,p.d-.16f));
                foreach(int s in new[]{-1,1})
                {
                    batch.Box(trim,new Vector3(0,.48f,s*(p.d/2-.06f)),new Vector3(p.w,.12f,.12f));
                    batch.Box(trim,new Vector3(s*(p.w/2-.06f),.48f,0),new Vector3(.12f,.12f,p.d-.24f));
                }
                for(int i=0;i<4;i++)GardenDetail.Shrub(batch,new Vector3((i-1.5f)*(p.w-.55f)/3,.48f,0),i,null,.65f);
                break;
            case "bin":
                batch.Box(metal,new Vector3(0,.36f,0),new Vector3(.48f,.72f,.48f));
                batch.Box(wood,new Vector3(0,.77f,0),new Vector3(.55f,.10f,.55f));
                batch.Box(SchoolMat("#283e3e"),new Vector3(0,.55f,.25f),new Vector3(.30f,.13f,.035f));break;
            case "gate":
                if(p.kind!="main")return false;
                foreach(int s in new[]{-1,1})
                {
                    batch.Box(stone,new Vector3(s*3.6f,2.6f,0),new Vector3(.9f,5.2f,.9f));
                    batch.Box(trim,new Vector3(s*3.6f,.35f,0),new Vector3(1.05f,.70f,1.05f));
                    batch.Box(trim,new Vector3(s*3.6f,5.16f,0),new Vector3(1.12f,.24f,1.12f));
                }
                batch.Box(metal,new Vector3(0,5.55f,0),new Vector3(8.6f,.70f,.9f));
                batch.Box(wood,new Vector3(0,4.65f,0),new Vector3(4.9f,1.12f,.32f));
                SchoolFontSign(root,4.72f,.98f,new Vector3(0,4.65f,.175f),0);
                SchoolFontSign(root,4.72f,.98f,new Vector3(0,4.65f,-.175f),180);break;
            default:return false;
        }
        batch.Flush(root);return true;
    }
}
