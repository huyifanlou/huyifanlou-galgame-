using UnityEngine;
public partial class WorldBuilder
{
    GameObject SchoolFontSign(Transform parent,float width,float height,Vector3 position,float yaw)
    {
        var frame=MeshObj(parent,MeshFactory.BoxUV(new Vector3(width,height,.055f),1),position,DetailSurface.Mat("paint","#3e5354"),new Vector3(0,yaw,0));
        var text=new GameObject("SchoolGateTypography");text.transform.SetParent(frame.transform,false);text.transform.localPosition=new Vector3(0,0,.035f);text.transform.localRotation=Quaternion.Euler(0,180,0);
        var tm=text.AddComponent<TextMesh>();tm.font=GameFonts.Map;tm.fontSize=72;tm.characterSize=.10f;tm.anchor=TextAnchor.MiddleCenter;tm.text="学校";tm.color=MatLib.Hex("#eee9dc");
        var renderer=text.GetComponent<MeshRenderer>();renderer.sharedMaterial=new Material(Resources.Load<Shader>("Shaders/TextSign"));renderer.sharedMaterial.mainTexture=GameFonts.Map.material.mainTexture;GameFonts.Map.RequestCharactersInTexture(tm.text,72);
        var bounds=renderer.bounds;float scale=Mathf.Min(width*.75f/Mathf.Max(bounds.size.x,.001f),height*.72f/Mathf.Max(bounds.size.y,.001f));text.transform.localScale=Vector3.one*scale;
        text.AddComponent<SignAtlasSync>().font=GameFonts.Map;return frame;
    }
}
