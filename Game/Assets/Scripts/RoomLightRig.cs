using UnityEngine;
using UnityEngine.Rendering;

// 室内：两盏有阴影的主灯 + 四个低能量解析补光区，替代几十次无阴影 ForwardAdd。
public sealed class RoomLightRig : MonoBehaviour
{
    static RoomLightRig active;
    readonly Vector4[] positions=new Vector4[4],colors=new Vector4[4];
    bool ready;
    string roomId;
    public void Configure(Interior room,Color tint)
    {
        roomId=room.Id;
        var original=GetComponentsInChildren<Light>(true);
        foreach(var light in original)light.enabled=false;
        float cx=(room.X1+room.X2)/2,cz=(room.Z1+room.Z2)/2,w=room.X2-room.X1,d=room.Z2-room.Z1;
        for(int i=0;i<4;i++)
        {
            float x=cx+(i%2==0?-1:1)*w*.25f,z=cz+(i<2?-1:1)*d*.25f;
            float radius=Mathf.Max(7,Mathf.Sqrt(w*w+d*d)*.58f);
            positions[i]=new Vector4(x,room.CeilingY-.60f,z,radius*radius);
            colors[i]=(Vector4)(Color.Lerp(tint,Color.white,.50f)*.13f);
        }
        for(int i=0;i<2;i++)
        {
            var go=new GameObject("RoomSoftKey_"+i);go.transform.SetParent(transform,false);
            var pos=new Vector3(cx+(i==0?-1:1)*w*.23f,room.CeilingY-.32f,cz+(i==0?-1:1)*d*.12f);
            go.transform.localPosition=pos;go.transform.localRotation=Quaternion.LookRotation(new Vector3(cx+(i==0?-1:1)*w*.13f,.4f,cz)-pos);
            var key=go.AddComponent<Light>();key.type=LightType.Spot;key.spotAngle=154;key.innerSpotAngle=115;
            key.range=Mathf.Sqrt(w*w+d*d)+8;key.color=Color.Lerp(tint,Color.white,.30f);key.intensity=1.15f;
            key.renderMode=LightRenderMode.ForcePixel;key.shadows=Args.Has("-noshadow")?LightShadows.None:LightShadows.Soft;key.shadowStrength=.76f;
            key.shadowResolution=LightShadowResolution.High;key.shadowBias=.025f;key.shadowNormalBias=.08f;key.shadowNearPlane=.15f;
        }
        ready=true;Debug.Log($"[RoomLightRig] {room.Id} 原灯 {original.Length} → 2 柔影主灯 + 4 解析补光区");
    }
    void OnEnable(){if(ready)Publish();}
    void Publish(){active=this;Shader.SetGlobalVectorArray("_RoomFillPositions",positions);Shader.SetGlobalVectorArray("_RoomFillColors",colors);}
    void OnDisable(){if(active==this){active=null;Shader.SetGlobalVectorArray("_RoomFillColors",new Vector4[4]);}}
    public static void Check(Interior room)
    {
        int enabled=0;foreach(var light in room.Root.GetComponentsInChildren<Light>(true))if(light.enabled&&light.gameObject.activeInHierarchy)enabled++;
        if(active==null||active.roomId!=room.Id||enabled!=2||Shader.GetGlobalFloat("_InteriorLighting")<.5f)throw new System.Exception("室内光照状态错误："+room.Id);
        var post=Camera.main.GetComponent<PostFX>();if(!Camera.main.allowHDR||post==null||post.Posterize>0||(Camera.main.depthTextureMode&DepthTextureMode.DepthNormals)==0)throw new System.Exception("HDR/AO/连续调色状态错误");
        Debug.Log("[LightingCheck] PASS "+room.Id+"：2 柔影主灯、房间补光绑定、HDR/深度法线/连续调色");
    }
    public static void CheckExterior()
    {
        if(active!=null||Shader.GetGlobalFloat("_InteriorLighting")>.5f)throw new System.Exception("出门后室内光照残留");
        foreach(var c in Shader.GetGlobalVectorArray("_RoomFillColors"))if(c.sqrMagnitude>.0001f)throw new System.Exception("出门后补光未清空");
    }
}
