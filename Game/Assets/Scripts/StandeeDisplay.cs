using UnityEngine;
// One temporary display portrait, independent from narrative anchors and save data.
public class StandeeDisplay : MonoBehaviour
{
 public static StandeeDisplay Live{get;private set;}
 WorldBuilder world;PlayerRig rig;InteriorManager interiors;GameObject displayed;Interior room;Vector3 playerOrigin;
 public string CurrentId{get;private set;}public bool IsShowing=>displayed!=null;public Vector3 DisplayPosition=>displayed!=null?displayed.transform.position:Vector3.zero;
 public void Init(WorldBuilder w,PlayerRig r,InteriorManager im){world=w;rig=r;interiors=im;Live=this;if(Args.Has("-displaycheck"))StartCoroutine(Check());}
 public bool Show(CharacterDef character)
 {
  if(DialogueManager.Active||ProgressManager.Loading||interiors.IsTransitioning){GetComponent<TimeOfDay>().ShowToast("暂离剧情后，可以在面前展示立牌。");return false;}
  Vector3 forward=Camera.main.transform.forward;forward.y=0;forward=forward.sqrMagnitude>.01f?forward.normalized:Vector3.forward;
  Vector3 location=Vector3.zero;bool found=false;
  foreach(float distance in new[]{2.4f,2.0f,2.8f,1.6f})
  {
   foreach(float angle in new[]{0f,-25f,25f,-50f,50f,-75f,75f,90f,-90f})
   {
    Vector3 direction=Quaternion.Euler(0,angle,0)*forward;Vector3 point=rig.Position+direction*distance;
    float y=interiors.Indoor?interiors.Current.FloorAt(point.x,point.z,rig.Position.y):world.GroundY(point.x,point.z);point.y=y;
    if(Mathf.Abs(y-rig.Position.y)>.2f||!rig.CanPlacePortrait(point))continue;
    Vector3 sideways=Vector3.Cross(Vector3.up,direction)*.42f;
    if(!rig.CanPlacePortrait(point+sideways)||!rig.CanPlacePortrait(point-sideways))continue;
    if(Physics.Linecast(Camera.main.transform.position,point+Vector3.up,~(1<<2),QueryTriggerInteraction.Ignore))continue;
    location=point;found=true;break;
   }
   if(found)break;
  }
  if(!found){GetComponent<TimeOfDay>().ShowToast("附近空间不足，换个开阔位置再展示吧。");return false;}
  Clear();displayed=new GameObject("图鉴展示立牌");displayed.transform.SetParent(world.Root,false);displayed.transform.position=location;PortraitStandee.Create(displayed.transform,character);
  CurrentId=character.id;room=interiors.Current;playerOrigin=rig.Position;rig.LookAtWorld(location+Vector3.up*.95f);GetComponent<TimeOfDay>().ShowToast("已展示「"+character.label+"」的立牌");return true;
 }
 public void Clear(){if(displayed!=null){displayed.SetActive(false);Destroy(displayed);}displayed=null;CurrentId=null;}
 void Update(){if(IsShowing&&(DialogueManager.Active||ProgressManager.Loading||interiors.IsTransitioning||interiors.Current!=room||Vector3.Distance(rig.Position,playerOrigin)>14))Clear();}
 System.Collections.IEnumerator Check()
 {
  yield return null;yield return null;
  var story=GetComponent<DialogueManager>();int chapter=story.Chapter,line=story.Line;bool linked=story.Linked;
  foreach(var def in CharacterDef.PortraitForms)
  {
   if(!Show(def))throw new System.Exception("No display placement: "+def.id);yield return null;
   int count=0;foreach(Transform t in world.Root)if(t.name=="图鉴展示立牌"&&t.gameObject.activeSelf)count++;
   if(count!=1||displayed.GetComponentInChildren<PortraitStandee>().Id!=def.id||!rig.CanPlacePortrait(DisplayPosition))throw new System.Exception("Display replacement/binding failure");
   if(Vector3.Dot(displayed.GetComponentInChildren<PortraitStandee>().transform.right,Camera.main.transform.right)<.95f)throw new System.Exception("Portrait display is mirrored");
   Debug.Log("[DisplayCheck] PASS "+def.id+" placement / one active display / correct portrait");
  }
  if(world.W.BLOCKERS.Length>0){var b=world.W.BLOCKERS[0];if(rig.CanPlacePortrait(new Vector3(b.x,rig.Position.y,b.z)))throw new System.Exception("Blocked path accepted");}
  if(rig.CanPlacePortrait(new Vector3(world.W.BOUND+5,0,0)))throw new System.Exception("Outside world placement accepted");
  Clear();yield return null;if(IsShowing)throw new System.Exception("Recall failed");
  Show(CharacterDef.JingYing);story.ResumeStory();yield return null;yield return null;
  if(IsShowing)throw new System.Exception("Display remained during dialogue");story.PauseStory();
  Show(CharacterDef.JingYing);ProgressManager.Loading=true;yield return null;yield return null;ProgressManager.Loading=false;
  if(IsShowing)throw new System.Exception("Display remained during travel/load");
  if(story.Chapter!=chapter||story.Line!=line||story.Linked!=linked)throw new System.Exception("Display changed narrative progress");
  if(!Show(CharacterDef.JingYing))throw new System.Exception("Re-show failed");
  Debug.Log("[DisplayCheck] PASS wall rejection, recall, dialogue/travel cleanup, no narrative progress changes");
 }
 void OnDestroy(){Clear();if(Live==this)Live=null;}
}

