using UnityEngine;

// 连续 HDR 调色、接触遮蔽与受控泛光；AO 半分辨率双边滤波。
[RequireComponent(typeof(Camera))]
public class PostFX : MonoBehaviour
{
    public float BloomIntensity=.14f,Threshold=1.1f,Saturation=.98f,Contrast=1.02f,Exposure=1.08f;
    public float Posterize=0,PosterizeSteps=32,VigStrength=.08f,DofMax=0;
    public float AOSStrength=.70f,AORadius=.50f,AOThreshold=.035f;
    Material material;
    void OnEnable()
    {
        var holder=Resources.Load<Material>("mat_holder/Campus_ToonPostFX");var shader=holder!=null?holder.shader:Shader.Find("Campus/ToonPostFX");
        if(shader==null){enabled=false;return;}material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
        var camera=GetComponent<Camera>();camera.allowHDR=true;camera.depthTextureMode|=DepthTextureMode.Depth|DepthTextureMode.DepthNormals;
        if(Args.Has("-noao"))AOSStrength=0;
    }
    void OnDisable(){if(material!=null)DestroyImmediate(material);}
    void OnRenderImage(RenderTexture src,RenderTexture dst)
    {
        if(material==null){Graphics.Blit(src,dst);return;}
        int w=Mathf.Max(4,src.width/2),h=Mathf.Max(4,src.height/2);
        var bloomA=RenderTexture.GetTemporary(w,h,0,src.format);var bloomB=RenderTexture.GetTemporary(w,h,0,src.format);
        material.SetFloat("_Threshold",Threshold);Graphics.Blit(src,bloomA,material,0);
        for(int i=0;i<2;i++){material.SetVector("_Dir",new Vector4(1,0,0,0));Graphics.Blit(bloomA,bloomB,material,1);material.SetVector("_Dir",new Vector4(0,1,0,0));Graphics.Blit(bloomB,bloomA,material,1);}
        RenderTexture aoA=null,aoB=null;
        if(AOSStrength>.001f)
        {
            aoA=RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.RHalf,RenderTextureReadWrite.Linear);
            aoB=RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.RHalf,RenderTextureReadWrite.Linear);
            aoA.filterMode=aoB.filterMode=FilterMode.Bilinear;
            material.SetFloat("_AORadius",AORadius);material.SetFloat("_AOBias",AOThreshold);
            Graphics.Blit(src,aoA,material,3);
            material.SetVector("_Dir",new Vector4(1,0,0,0));Graphics.Blit(aoA,aoB,material,4);
            material.SetVector("_Dir",new Vector4(0,1,0,0));Graphics.Blit(aoB,aoA,material,4);
            material.SetTexture("_AOTex",aoA);
        }
        else material.SetTexture("_AOTex",Texture2D.whiteTexture);
        material.SetFloat("_AOStrength",AOSStrength);material.SetFloat("_AODebug",Args.Has("-aodebug")?1:0);
        material.SetTexture("_BloomTex",bloomA);material.SetTexture("_BlurTex",src);
        material.SetFloat("_BloomIntensity",BloomIntensity);material.SetFloat("_VigStrength",1-VigStrength);
        material.SetFloat("_DofMax",0);material.SetFloat("_Saturation",Saturation);material.SetFloat("_Contrast",Contrast);
        material.SetFloat("_Exposure",Exposure);material.SetFloat("_Posterize",0);material.SetFloat("_PosterizeSteps",32);
        Graphics.Blit(src,dst,material,2);
        RenderTexture.ReleaseTemporary(bloomA);RenderTexture.ReleaseTemporary(bloomB);
        if(aoA!=null){RenderTexture.ReleaseTemporary(aoA);RenderTexture.ReleaseTemporary(aoB);}
    }
}
