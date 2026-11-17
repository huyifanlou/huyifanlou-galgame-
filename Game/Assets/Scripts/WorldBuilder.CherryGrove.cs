using UnityEngine;

public partial class WorldBuilder
{
    public static bool InCherryGrove(float x,float z) => x>=5&&x<=67&&z>=-100&&z<=-63;
    Material BlossomMat(int variant)
    {
        string key="Blossom|"+variant;
        if(!_cache.TryGetValue(key,out var mat))
        {
            string hex=variant==11?"#e5bdc8":variant==12?"#cc94ad":variant==13?"#d7a8bd":"#dfb0c1";
            mat=MatLib.LitColor(MatLib.Hex(hex));mat.name="blossom_"+variant;
            mat.SetFloat("_OutlineWidth",0);mat.SetFloat("_RimStrength",.045f);mat.SetFloat("_RampSteps",4);mat.SetFloat("_RampSoft",.16f);mat.SetFloat("_VertexTint",1);
            mat.SetColor("_ShadowTint",new Color(.76f,.69f,.78f));
            _cache[key]=mat;LeafMats.Add(mat);
        }
        return mat;
    }
    void BuildCherryTree(Transform root,float x,float z,int variant,bool giant=false)
    {
        root.name=giant?"giant_cherry":"cherry_"+variant;
        float hash=Mathf.Repeat(x*.713f+z*.319f,1);
        root.localEulerAngles=new Vector3(0,hash*360,0);
        float scale=giant?1f:.93f+hash*.15f;root.localScale=Vector3.one*scale;
        NaturalTree.Build(root,2,Mathf.RoundToInt(x*713+z*319),false,giant,variant==12?"#d49bae":variant==11?"#edc6d1":"#e2b0c5");
        var collider=root.gameObject.AddComponent<CapsuleCollider>();collider.radius=giant?1.08f:.30f;collider.height=giant?3.2f:2.8f;collider.center=Vector3.up*collider.height/2;
        // 不挂 ProximityHide：树下近看不再整棵突然消失，阴影与树干始终同步。
    }
    void GiantCherry(Transform root)
    {
        BuildCherryTree(root,root.position.x,root.position.z,100,true);
        var details=NewGo("GrovePetals",Root).transform;
        var batch=new RegionMeshBatch();var random=new System.Random(109110);
        var petal=MeshFactory.Disc(1,.58f,7,1);
        for(int i=0;i<280;i++)
        {
            float a=(float)random.NextDouble()*Mathf.PI*2,rr=2.1f+Mathf.Sqrt((float)random.NextDouble())*8.3f;
            float x=root.position.x+Mathf.Cos(a)*rr,z=root.position.z+Mathf.Sin(a)*rr;
            if(NearGrovePath(x,z,.25f))continue;
            float s=.035f+(float)random.NextDouble()*.035f;
            batch.Add(SchoolMat(i%3==0?"#cba7b6":"#e1c4cc"),petal,new Vector3(x,GroundY(x,z)+.025f,z),new Vector3(s,1,s),Quaternion.Euler(0,a*Mathf.Rad2Deg,0));
        }
        // 接现有林道的步石落在实地上，避开巨樱树干碰撞。
        var stone=MeshFactory.Disc(.48f,.43f,12,1);
        for(int i=0;i<8;i++)
        {
            float x=Mathf.Lerp(39.3f,31.3f,i/7f),z=-74.5f+Mathf.Sin(i*.75f)*.27f;
            if(!NearGrovePath(x,z,.15f))batch.Add(SchoolMat("#a4a596"),stone,new Vector3(x,.026f,z),Vector3.one,Quaternion.Euler(0,i*39,0));
        }
        batch.Flush(details);
    }
    bool NearGrovePath(float x,float z,float pad)
    {
        bool Near(PropDef p)
        {
            if(p.type!="plaza")return false;
            float c=Mathf.Cos(p.ry),s=Mathf.Sin(p.ry),dx=x-p.x,dz=z-p.z;
            return Mathf.Abs(dx*c-dz*s)<p.w/2+pad&&Mathf.Abs(dx*s+dz*c)<p.d/2+pad;
        }
        foreach(var p in W.PROPS)if(Near(p))return true;
        foreach(var p in _residentialEntryPaths)if(Near(p))return true;
        return false;
    }
    void GroveUnderstory()
    {
        if(W.RIVER==null)return;var root=NewGo("GroveUnderstory",Root).transform;var batch=new RegionMeshBatch();int i=0;
        foreach(var plant in W.AZALEAS){if(!InCherryGrove(plant.x,plant.z)||NearGrovePath(plant.x,plant.z,.65f))continue;GardenDetail.Shrub(batch,new Vector3(plant.x,GroundY(plant.x,plant.z),plant.z),i++,i%3==0?"#c69aa9":"#dbb4bf");}
        batch.Flush(root);
    }
}
