using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Recording-only route. All translation is normal PlayerRig movement. Door
// transitions require the same proximity checks as ordinary F interaction.
public partial class SubmissionTour
{
    public bool WalkFinished{get;private set;}
    Func<float,float,bool> blocked;
    struct Step {public int x,z;public float f;public int serial;}
    int K(int x,int z)=>(x+4096)*8192+z+4096;
    Vector2 Point(int k)=>new Vector2((k/8192-4096)*.5f,(k%8192-4096)*.5f);
    bool Solid(float x,float z)
    {
        if(rooms.Indoor){var a=rooms.Current;if(x<a.X1+.5f||x>a.X2-.5f||z<a.Z1+.5f||z>a.Z2-.5f)return true;}
        else
        {
            if(Mathf.Abs(x)>140||Mathf.Abs(z)>140)return true;
            if(Mathf.Abs(z-TownData.RiverZ(x))<8&&Mathf.Abs(x)>1.1f&&Mathf.Abs(x+45)>1.1f&&Mathf.Abs(x-28)>1.1f)return true;
            if(Mathf.Pow((x-34)/11.5f,2)+Mathf.Pow((z+23.5f)/8.5f,2)<1)return true;
        }
        return blocked(x,z);
    }
    List<Vector2> Path(Vector2 goal)
    {
        int sx=Mathf.RoundToInt(rig.Position.x*2),sz=Mathf.RoundToInt(rig.Position.z*2),gx=Mathf.RoundToInt(goal.x*2),gz=Mathf.RoundToInt(goal.y*2),start=K(sx,sz),end=K(gx,gz),serial=0;
        var open=new SortedSet<Step>(Comparer<Step>.Create((a,b)=>{int c=a.f.CompareTo(b.f);return c!=0?c:a.serial.CompareTo(b.serial);}));
        var cost=new Dictionary<int,float>{{start,0}};var previous=new Dictionary<int,int>();var closed=new HashSet<int>();var solid=new Dictionary<int,bool>();
        open.Add(new Step{x=sx,z=sz,serial=serial++,f=0});int best=start;float bestDistance=float.MaxValue;
        while(open.Count>0&&closed.Count<100000)
        {
            var n=open.Min;open.Remove(n);int id=K(n.x,n.z);if(!closed.Add(id))continue;float distance=Vector2.Distance(Point(id),goal);if(distance<bestDistance){bestDistance=distance;best=id;}if(distance<.65f){end=id;break;}
            for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++)
            {
                if(dx==0&&dz==0)continue;int x=n.x+dx,z=n.z+dz,k=K(x,z);if(closed.Contains(k))continue;
                bool occupied;if(!solid.TryGetValue(k,out occupied)){occupied=Solid(x*.5f,z*.5f);solid[k]=occupied;}if(occupied)continue;
                if(dx!=0&&dz!=0&&(Solid(n.x*.5f,z*.5f)||Solid(x*.5f,n.z*.5f)))continue;
                float price=1;
                if(!rooms.Indoor){price=1.8f;foreach(var road in GetComponent<WorldBuilder>().W.ROADS){float along=road.axis=="z"?z*.5f:x*.5f,across=road.axis=="z"?x*.5f:z*.5f;if(along>=road.from&&along<=road.to&&Mathf.Abs(across-road.at)<road.width*.5f+1.8f){price=1;break;}}}
                float next=cost[id]+(dx!=0&&dz!=0?.7071f:.5f)*price;if(cost.TryGetValue(k,out var old)&&next>=old)continue;cost[k]=next;previous[k]=id;
                open.Add(new Step{x=x,z=z,serial=serial++,f=next+Vector2.Distance(new Vector2(x*.5f,z*.5f),goal)});
            }
        }
        if(!previous.ContainsKey(end)){if(bestDistance>.9f)throw new Exception("Walking route unavailable, nearest="+bestDistance);end=best;}
        var result=new List<Vector2>();for(int k=end;k!=start;k=previous[k])result.Add(Point(k));result.Reverse();return result;
    }
    IEnumerator Walk(Vector2 goal,string label)
    {
        Mark("WALK BEGIN "+label);var path=Path(goal);int index=0;float since=Time.realtimeSinceStartup,lastMove=since;Vector3 last=rig.Position;
        while(index<path.Count)
        {
            Vector2 p=new Vector2(rig.Position.x,rig.Position.z);while(index<path.Count&&Vector2.Distance(p,path[index])<.55f)index++;if(index>=path.Count)break;
            Vector2 to=path[index]-p;Vector3 direction=new Vector3(to.x,0,to.y).normalized;
            Vector3 forward=Camera.main.transform.forward;forward.y=0;forward.Normalize();forward=Vector3.RotateTowards(forward,direction,2.3f*Time.unscaledDeltaTime,0);
            rig.LookAtWorld(rig.Position+Vector3.up*1.45f+forward*12);Motion=Vector3.Angle(forward,direction)<34?Vector2.up:Vector2.zero;
            if(Vector3.Distance(last,rig.Position)>.12f){last=rig.Position;lastMove=Time.realtimeSinceStartup;}
            if(Time.realtimeSinceStartup-lastMove>3.5f)throw new Exception("Walking blocked: "+label+" at "+rig.Position);
            if(Time.realtimeSinceStartup-since>100)throw new Exception("Walking timed out: "+label);
            yield return null;
        }
        Motion=Vector2.zero;Mark("WALK ARRIVED "+label);yield return Wait(.6f);
    }
    IEnumerator DoorIn(string name)
    {
        if(!rooms.ExteriorDoor(name,out var door,out var normal))throw new Exception("Missing door "+name);
        yield return Walk(door+normal*1.7f,name+" exterior door");rig.LookAtWorld(new Vector3(door.x,rig.Position.y+1.5f,door.y));yield return Wait(2);
        if(!rooms.NearEntrance(name,rig.Position)||!rooms.Enter(name))throw new Exception("Not physically at entrance "+name);yield return Settle();Mark("F ENTER "+name);
    }
    IEnumerator DoorOut()
    {
        yield return Walk(rooms.Current.Exit,rooms.Current.Id+" exit");if(!rooms.NearExit)throw new Exception("Not at indoor exit");yield return Wait(1);rooms.Exit();yield return Settle();Mark("F EXIT");
    }
    IEnumerator WalkPreview()
    {
        blocked=(Func<float,float,bool>)Delegate.CreateDelegate(typeof(Func<float,float,bool>),rig,typeof(PlayerRig).GetMethod("Collides",Flags));
        Mark("BORN AT HOME");yield return Wait(3);story.ResumeStory();
        for(int i=0;i<26;i++){yield return Wait(1.6f);Call(story,"Advance");}
        Put(story,"_auto",true);yield return Wait(7);Put(story,"_auto",false);story.OpenArchive();yield return Wait(3);Put(story,"_find","镜荧");yield return Wait(3);story.ReturnFromReplay();story.PauseStory();
        gallery.Toggle();Call(gallery,"Select",1);yield return Wait(3);Call(gallery,"Select",0);Put(gallery,"femaleForm",true);yield return Wait(3);gallery.Toggle();
        yield return DoorOut();rig.LookAtWorld(new Vector3(-34,2,70));yield return Wait(3);
        yield return Walk(new Vector2(-34,80),"residential street");yield return Walk(new Vector2(0,80),"main avenue");yield return Walk(new Vector2(0,-20),"stone bridge and school approach");
        yield return DoorIn("教学楼");yield return Walk(new Vector2(1038,-4.9f),"classroom aisle");rig.LookAtWorld(new Vector3(1038,1.5f,-13));yield return Wait(6);yield return DoorOut();
        yield return Walk(new Vector2(58,-43),"park pond");yield return MoveView(4,new Vector3(34,1.5f,-24));
        float start=Time.realtimeSinceStartup;while(Time.realtimeSinceStartup-start<10){clock.GameMinutes=Mathf.Lerp(14*60,22*60,(Time.realtimeSinceStartup-start)/10);yield return null;}
        yield return Wait(3);clock.GameMinutes=14*60;HelpSidebar.Live.OpenWeather();weather.Configure(.8f,1.1f,1.3f,false);weather.TransitionTo(Weather.Kind.Rain);yield return Wait(4);HelpSidebar.Live.Close();yield return Wait(6);weather.TransitionTo(Weather.Kind.Snow);yield return Wait(7);weather.TransitionTo(Weather.Kind.Clear);
        yield return Walk(new Vector2(54,-49),"sakura forest entrance");yield return Walk(new Vector2(44,-61),"sakura footpath");rig.LookAtWorld(new Vector3(29,7,-74.5f));yield return Wait(7);
        gallery.Toggle();Call(gallery,"Select",1);yield return Wait(2);gallery.DisplaySelected();yield return Wait(4);StandeeDisplay.Live.Clear();
        quests.Open();yield return Wait(3);ProgressManager.TrackingOpen=false;progress.OpenSaves();yield return Wait(3);ProgressManager.MenuOpen=false;
        Mark("WALK PREVIEW COMPLETE no map teleport / no interior pose jump");WalkFinished=true;
    }
}
