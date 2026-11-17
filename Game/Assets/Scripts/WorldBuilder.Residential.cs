using System.Collections.Generic;
using UnityEngine;

public partial class WorldBuilder
{
    public Material ResidenceGlassLit,ResidenceGlassDark;
    List<PropDef> _residentialEntryPaths=new List<PropDef>();
    Font _residenceSignFont;
    Material _residenceSignMaterial;

    void ResidenceSign(Transform building,BuildingDef definition,Vector3 position)
    {
        if(_residenceSignFont==null)
        {
            _residenceSignFont=GameFonts.Map;
            _residenceSignFont.RequestCharactersInTexture("奶茶店超市餐厅照相馆书便利",72,FontStyle.Normal);
            _residenceSignMaterial=new Material(Resources.Load<Shader>("Shaders/TextSign")){name="residence_sign_font"};
            _residenceSignMaterial.mainTexture=_residenceSignFont.material.mainTexture;
        }
        // 动态字形放在静态合批之外，字体图集变化时仍能刷新 UV。
        var go=NewGo("sign_"+definition.id,SignRoot);
        go.transform.position=building.TransformPoint(position);
        go.transform.rotation=building.rotation*Quaternion.Euler(0,180,0);
        var text=go.AddComponent<TextMesh>();text.font=_residenceSignFont;text.fontSize=72;
        text.characterSize=.075f;text.text=definition.name;text.anchor=TextAnchor.MiddleCenter;
        text.alignment=TextAlignment.Center;text.color=MatLib.Hex("#efe7d2");
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=_residenceSignMaterial;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows=false;
        go.AddComponent<SignAtlasSync>().font=_residenceSignFont;
    }

    void EnsureResidenceGlass()
    {
        if(ResidenceGlassLit!=null)return;
        ResidenceGlassLit=new Material(DetailSurface.Mat("glass","#6d8b93")){name="residence_glass_lit"};
        ResidenceGlassDark=new Material(DetailSurface.Mat("glass","#5b747c")){name="residence_glass_dark"};
        ResidenceGlassLit.EnableKeyword("_EMISSION");ResidenceGlassLit.SetColor("_EmissionColor",Color.black);
    }
    void BuildResidence(Transform root,BuildingDef b,bool home=false)=>BuildPrecisionArchitecture(root,b,home);

    void ResidenceHomeYard(Transform root,RegionMeshBatch batch,BuildingDef home)
    {
        float bottom=-home.size[1]/2,front=home.size[2]/2;
        batch.Box(SchoolMat("#beb8a5"),new Vector3(.6f,bottom+.014f,front+1.9f),new Vector3(4.2f,.028f,2.4f));
        var fence=SchoolMat("#829083");
        foreach(int side in new[]{-1,1})
        {
            float x=side*2.6f;
            for(int i=0;i<5;i++)batch.Box(fence,new Vector3(x-1.3f+i*.65f,bottom+.43f,front+2.8f),new Vector3(.12f,.86f,.14f));
            foreach(float y in new[]{.32f,.69f})batch.Box(fence,new Vector3(x,bottom+y,front+2.8f),new Vector3(2.6f,.09f,.10f));
            var collider=NewGo("fence_camera",root);collider.transform.localPosition=new Vector3(x,bottom+.43f,front+2.8f);
            collider.AddComponent<BoxCollider>().size=new Vector3(2.6f,.86f,.14f);
        }
        var stone=SchoolMat("#a8a393");float lx=3.1f,lz=front+1.15f;
        batch.Box(stone,new Vector3(lx,bottom+.12f,lz),new Vector3(.56f,.24f,.56f));
        batch.Box(stone,new Vector3(lx,bottom+.59f,lz),new Vector3(.24f,.72f,.24f));
        batch.Box(LampGlass,new Vector3(lx,bottom+1.04f,lz),new Vector3(.34f,.32f,.34f));
        foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})batch.Box(stone,new Vector3(lx+sx*.21f,bottom+1.04f,lz+sz*.21f),new Vector3(.08f,.40f,.08f));
        batch.Box(stone,new Vector3(lx,bottom+1.31f,lz),new Vector3(.66f,.16f,.66f));
        var lampCollider=NewGo("lantern_camera",root);lampCollider.transform.localPosition=new Vector3(lx,bottom+.70f,lz);
        lampCollider.AddComponent<BoxCollider>().size=new Vector3(.56f,1.4f,.56f);
    }

    float ResidentialPathHeight(float x,float z)
    {
        foreach(var p in _residentialEntryPaths)
        {
            float dx=x-p.x,dz=z-p.z,c=Mathf.Cos(p.ry),s=Mathf.Sin(p.ry);
            if(Mathf.Abs(dx*c-dz*s)<=p.w/2&&Mathf.Abs(dx*s+dz*c)<=p.d/2)return .01f;
        }
        return 0;
    }
}
