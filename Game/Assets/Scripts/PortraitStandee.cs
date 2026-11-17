using System;using System.Collections.Generic;using UnityEngine;
// A flat, alpha-trimmed portrait at the former character interaction anchor.
public class PortraitStandee : MonoBehaviour
{
 [Serializable]public class Entry{public string id;public Rect uv;}
 [Serializable]public class BoundsData{public Entry[] entries;}
 static Dictionary<string,Rect> bounds;static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
 public string Id{get;private set;}public float Height{get;private set;}
 Transform art;
 public static Rect ArtBounds(string id)
 {
  if(bounds==null){bounds=new Dictionary<string,Rect>();var json=Resources.Load<TextAsset>("PortraitStandeeBounds");if(json!=null)foreach(var e in JsonUtility.FromJson<BoundsData>(json.text).entries)bounds[e.id]=e.uv;}
  return bounds.TryGetValue(id,out var uv)?uv:new Rect(0,0,1,1);
 }
 public static PortraitStandee Create(Transform parent,CharacterDef definition)
 {
  string id=definition.id;var texture=PortraitAssets.LoadResource("Portraits/"+id+"-cut");if(texture==null)throw new Exception("缺少角色立绘："+id);
  var go=new GameObject("立绘立牌_"+definition.label);go.transform.SetParent(parent,false);go.layer=2;var standee=go.AddComponent<PortraitStandee>();standee.Id=id;
  float h=id=="liliya"?1.65f:id=="ziwei"?1.85f:id=="fanlou-female"?1.72f:1.8f;Rect uv=ArtBounds(id);float w=h*texture.width*uv.width/(texture.height*uv.height);
  var mesh=new Mesh{name="PortraitQuad-"+id};mesh.vertices=new[]{new Vector3(-w*.5f,.015f,0),new Vector3(w*.5f,.015f,0),new Vector3(w*.5f,h+.015f,0),new Vector3(-w*.5f,h+.015f,0)};mesh.uv=new[]{new Vector2(uv.xMin,uv.yMin),new Vector2(uv.xMax,uv.yMin),new Vector2(uv.xMax,uv.yMax),new Vector2(uv.xMin,uv.yMax)};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();
  if(!materials.TryGetValue(id,out var material)){material=new Material(Shader.Find("Campus/PortraitStandee")){name="Standee-"+id,mainTexture=texture};materials[id]=material;}
  go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;standee.Height=h;standee.art=go.transform;return standee;
 }
 // The artwork faces local -Z, like the camera image plane; this keeps left/right faithful.
 void LateUpdate(){var camera=Camera.main;if(camera==null||art==null)return;var delta=camera.transform.position-art.position;delta.y=0;if(delta.sqrMagnitude>.0001f)art.rotation=Quaternion.LookRotation(-delta);}
 void OnDestroy(){var filter=GetComponent<MeshFilter>();if(filter!=null&&filter.sharedMesh!=null)Destroy(filter.sharedMesh);}
}
