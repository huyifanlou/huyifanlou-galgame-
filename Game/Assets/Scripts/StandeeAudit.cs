using System;using System.Collections;using UnityEngine;
// Explicit QA / author preview only; normal narrative keeps the original NPC anchors.
public class StandeeAudit : MonoBehaviour
{
 WorldBuilder world;PlayerRig rig;DialogueManager story;InteriorManager interiors;
 public void Init(WorldBuilder w,PlayerRig r,DialogueManager d,InteriorManager i){world=w;rig=r;story=d;interiors=i;StartCoroutine(Run());}
 IEnumerator Run()
 {
  yield return null;yield return null;
  if(Args.Has("-standeepreview"))
  {
   story.RestoreProgress(0,30,false,true);
   var progress=GetComponent<ProgressManager>();
   if(interiors.Indoor&&interiors.Current.Name!=progress.ObjectivePlace){interiors.Exit();while(interiors.IsTransitioning)yield return null;}
   if(!interiors.Indoor){var destination=progress.MapDestination;rig.Teleport(destination.x,destination.z+2.7f);}
   yield return null;yield return null;
   var portrait=world.Root.GetComponentInChildren<PortraitStandee>();if(portrait!=null){rig.LookAtWorld(portrait.transform.position+Vector3.up);Debug.Log("[StandeePreview] active story portrait "+portrait.Id+" at original quest anchor");}else throw new Exception("Story standee preview missing");
   yield break;
  }
  if(world.Player.GetComponentsInChildren<Renderer>(true).Length!=0)throw new Exception("Player visual model remains");
  if(FindObjectsOfType<SkinnedMeshRenderer>(true).Length!=0)throw new Exception("Skinned character remains");
  if(Resources.Load<GameObject>("MMD/fanlou/model")!=null||Resources.Load<GameObject>("Characters/fanlou/fanlou")!=null)throw new Exception("Retired model still shipped");
  if(GameInput.Find("View")!=null)throw new Exception("Third-person binding remains");
  var old=new ProgressSave{yaw=0,facing=0,distance=22,pitch=.7f,fpPitch=0,firstPerson=false};rig.RestoreView(old);yield return null;
  if((Camera.main.transform.position-rig.Position-Vector3.up*1.45f).magnitude>.01f)throw new Exception("Legacy save restored third-person camera");
  Debug.Log("[StandeeCheck] PASS no player body/shadow, no skinned models or old model resources, legacy save stays first person, View binding removed");
  int k=0;foreach(var def in CharacterDef.PortraitForms)
  {
   var parent=new GameObject("QA-portrait-"+def.id).transform;parent.SetParent(world.Root,false);float x=rig.Position.x+(k-3f)*1.35f,z=rig.Position.z-7;
   parent.position=new Vector3(x,world.GroundY(x,z),z);var portrait=PortraitStandee.Create(parent,def);var mesh=portrait.GetComponent<MeshFilter>().sharedMesh;var renderer=portrait.GetComponent<Renderer>();
   if(mesh.vertexCount!=4||mesh.triangles.Length!=6||!renderer.sharedMaterial.shader.isSupported||renderer.sharedMaterial.mainTexture==null||portrait.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Invalid standee "+def.id);
   k++;Debug.Log("[StandeeCheck] PASS "+def.label+" trimmed portrait, quad, alpha material, no blocking collider");
  }
  yield return null;
  Debug.Log("[StandeeCheck] PASS all seven portrait forms and fixed first-person presentation");
 }
}
