using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Optional submission-only operator. Invokes existing gameplay/UI actions and
// feeds movement into PlayerRig's normal collision path; never drives the desktop.
public class SubmissionTour : MonoBehaviour
{
    public static Vector2 Motion;
    PlayerRig rig; DialogueManager story; ProgressManager progress; InteriorManager rooms;
    TimeOfDay clock; Weather weather; CharacterPicker gallery; Minimap map; QuestJournal quests;
    float began;
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    void Put(object target,string field,object value)
    {var f=target.GetType().GetField(field,Flags);if(f==null)throw new Exception("Tour field missing: "+field);f.SetValue(target,value);}
    void Call(object target,string method,params object[] args)
    {var m=target.GetType().GetMethod(method,Flags);if(m==null)throw new Exception("Tour action missing: "+method);m.Invoke(target,args);}
    void Mark(string text)
    {Debug.Log($"[SubmissionTour] {Time.realtimeSinceStartup-began:F2}s {text}; player={rig.Position}; chapter={story.Chapter} line={story.Line}; room={rooms.Current?.Id}; weather={weather.Current}");}
    public void Begin(string mode)
    {
        rig=GetComponent<PlayerRig>();story=GetComponent<DialogueManager>();progress=GetComponent<ProgressManager>();rooms=GetComponent<InteriorManager>();
        clock=GetComponent<TimeOfDay>();weather=GetComponent<Weather>();gallery=GetComponent<CharacterPicker>();map=GetComponent<Minimap>();quests=GetComponent<QuestJournal>();
        began=Time.realtimeSinceStartup;clock.SetSpeed(0);GameInput.TextSpeed=48;GameInput.TextScale=1;
        StartCoroutine(Run(mode));
    }
    IEnumerator Wait(float seconds){yield return new WaitForSecondsRealtime(seconds);}
    IEnumerator Settle()
    {yield return null;float until=Time.realtimeSinceStartup+5;while((ProgressManager.Loading||rooms.IsTransitioning)&&Time.realtimeSinceStartup<until)yield return null;yield return new WaitForSecondsRealtime(.3f);}
    IEnumerator Outdoors(float x,float z)
    {story.PauseStory();if(CharacterPicker.IsOpen)gallery.Toggle();HelpSidebar.Live.Close();ProgressManager.MenuOpen=ProgressManager.TrackingOpen=false;if(map.LargeOpen)map.Toggle();progress.TravelMap(x,z);yield return Settle();}
    IEnumerator Room(string name)
    {story.PauseStory();ProgressManager.MenuOpen=ProgressManager.TrackingOpen=false;progress.TravelSide(name,true);yield return Settle();Mark("entered "+name);}
    IEnumerator MoveView(float seconds,Vector3 target,float walk=0,float side=0)
    {
        float start=Time.realtimeSinceStartup;Vector3 original=rig.Position;
        while(Time.realtimeSinceStartup-start<seconds)
        {
            float t=Time.realtimeSinceStartup-start;
            rig.LookAtWorld(target+Vector3.right*(Mathf.Sin(t/seconds*Mathf.PI)*1.2f));
            Motion=t<walk?new Vector2(side,side==0?1:0):Vector2.zero;
            yield return null;
        }
        Motion=Vector2.zero;Mark("native movement "+Vector3.Distance(original,rig.Position).ToString("F2")+"m");
    }
    int SpeakerLine(int chapter,string speaker)
    {var lines=ScenarioData.LoadAll()[chapter].lines;int index=lines.FindIndex(l=>l.type==LineType.Dialogue&&l.speaker==speaker);if(index<0)throw new Exception("No dialogue: "+speaker);return index;}
    void InspectRoom(float x,float z,Vector3 look)
    {
        var room=rooms.Current;if(room==null||x<room.X1||x>room.X2||z<room.Z1||z>room.Z2||room.Collides(x,z,0))throw new Exception("Unsafe indoor tour camera");
        rig.SetIndoorView(true,new Vector2(x,z),room.FloorAt(x,z,0));rig.LookAtWorld(look);Mark("interior inspection camera cut");
    }
    IEnumerator Run(string mode)
    {
        Mark("begin "+mode);
        switch(mode)
        {
            case "tour-title":yield return Title();break;
            case "tour-story":yield return Story();break;
            case "tour-gallery":yield return Gallery();break;
            case "tour-weather":yield return WeatherTour();break;
            case "tour-interiors":yield return Interiors();break;
            case "tour-landmarks":yield return Landmarks();break;
            case "tour-systems":yield return Systems();break;
            case "tour-settings":yield return Settings();break;
        }
        Motion=Vector2.zero;Mark("COMPLETE "+mode);
    }
    IEnumerator Title()
    {
        yield return Wait(6);StartMenu.Live.QueueEntry(StartMenu.Entry.Settings);Mark("title settings");yield return Wait(4);
        HelpSidebar.Live.Close();yield return Wait(2);StartMenu.Live.QueueEntry(StartMenu.Entry.Resume);yield return Settle();Mark("title continue / start");yield return Wait(5);
        StartMenu.Live.Open();StartMenu.Live.QueueEntry(StartMenu.Entry.NewGame);Mark("new game confirmation");yield return Wait(3);
        StartMenu.Live.QueueEntry(StartMenu.Entry.NewGame);yield return Settle();Mark("new game entered home");yield return Wait(2);
    }
    IEnumerator Story()
    {
        if(!rooms.Indoor){yield return Room("家");}
        int first=SpeakerLine(0,"镜荧");story.RestoreProgress(0,first,true,true);Mark("manual dialogue / portrait");
        yield return Wait(4);Call(story,"Advance");yield return Wait(2);Call(story,"Advance");yield return Wait(4);
        Put(story,"_auto",true);Mark("auto reading");yield return Wait(10);Put(story,"_auto",false);
        story.OpenArchive();Mark("chapter archive");yield return Wait(4);Put(story,"_find","镜荧");Mark("search character");yield return Wait(4);
        Put(story,"_find","");Put(story,"_logChapter",2);yield return Wait(3);
        Call(story,"JumpTo",2,SpeakerLine(2,"镜荧"));Put(story,"_logOpen",false);Mark("jump chapter / line");yield return Wait(6);
        story.ReturnFromReplay();Mark("return preserves main progress");yield return Wait(3);
        HelpSidebar.Live.OpenSettings();Put(HelpSidebar.Live,"tab",0);yield return Wait(3);
        GameFonts.StoryChoice=1;GameInput.TextScale=1.12f;yield return Wait(3);HelpSidebar.Live.Close();Mark("sans reading / larger text");yield return Wait(4);
        GameFonts.StoryChoice=0;GameInput.TextScale=1;Put(story,"_skip",true);Mark("skip");yield return Wait(2);Put(story,"_skip",false);
        story.PauseStory();Mark("pause preserves chapter / line");yield return Wait(4);
    }
    IEnumerator Gallery()
    {
        story.PauseStory();gallery.Toggle();Put(gallery,"preview",false);yield return Wait(3);
        foreach(int index in new[]{1,2,3,4,5,0}){Call(gallery,"Select",index);Mark("gallery portrait "+index);yield return Wait(2);}
        Put(gallery,"femaleForm",true);Mark("female form");yield return Wait(3);
        bool placed=gallery.DisplaySelected();Mark("display female standee="+placed);yield return Wait(2);
        if(placed)yield return MoveView(4,StandeeDisplay.Live.DisplayPosition+Vector3.up,2,1);
        StandeeDisplay.Live.Clear();Mark("recall standee");yield return Wait(2);
        gallery.Toggle();Call(gallery,"Select",1);yield return Wait(2);gallery.DisplaySelected();yield return Wait(2);
    }
    IEnumerator WeatherTour()
    {
        weather.Restore(Weather.Kind.Clear);clock.GameMinutes=12*60;Mark("clear daylight");yield return MoveView(5,new Vector3(34,1.4f,-24));
        float start=Time.realtimeSinceStartup;
        while(Time.realtimeSinceStartup-start<11){clock.GameMinutes=Mathf.Lerp(15*60,22*60,(Time.realtimeSinceStartup-start)/11);yield return null;}
        Mark("night lamps / sky");yield return Wait(3);clock.GameMinutes=14*60;
        HelpSidebar.Live.OpenWeather();weather.Configure(.45f,.85f,1.35f,false);yield return Wait(3);
        weather.TransitionTo(Weather.Kind.Rain);Mark("rain transition");yield return Wait(2);HelpSidebar.Live.Close();yield return MoveView(6,new Vector3(34,.6f,-24));
        HelpSidebar.Live.OpenWeather();weather.Configure(1.25f,1.2f,1.7f,false);yield return Wait(2);weather.TransitionTo(Weather.Kind.Snow);Mark("snow transition / intensity / size");yield return Wait(2);HelpSidebar.Live.Close();yield return MoveView(9,new Vector3(34,1,-24));
        Mark("snow cover / water / lighting");
    }
    IEnumerator Interiors()
    {
        InspectRoom(900,-6.7f,new Vector3(899,1.4f,3));yield return MoveView(5,new Vector3(899,1.4f,3));Mark("home oak / wallpaper / rug");
        yield return Room("教学楼");InspectRoom(1038,-4.9f,new Vector3(1038,1.5f,-13));yield return MoveView(8,new Vector3(1038,1.5f,-13),.3f);
        yield return Room("宿舍楼");InspectRoom(1514.5f,-7,new Vector3(1517.6f,1.4f,-8.7f));yield return MoveView(8,new Vector3(1517.6f,1.4f,-8.7f));
        yield return Room("图书馆");var room=rooms.Current;yield return MoveView(9,new Vector3((room.X1+room.X2)*.5f,1.5f,(room.Z1+room.Z2)*.5f));
        rooms.Exit();yield return Settle();Mark("interior exit restores exterior");yield return Wait(3);
    }
    IEnumerator Landmark(string label,float x,float z,Vector3 look,float walk=2)
    {yield return Outdoors(x,z);Mark("landmark "+label);yield return MoveView(8.8f,look,walk);}
    IEnumerator Landmarks()
    {
        yield return Landmark("giant sakura",54,-49,new Vector3(29,7,-74.5f));
        yield return Landmark("shrine approach",79,-53.5f,new Vector3(79,4,-72),1.1f);
        yield return Landmark("park fountain",34,-31,new Vector3(34,2,-40),.5f);
        var bridge=GetComponent<WorldBuilder>().W.SPOTS.First(s=>s.name=="廊桥");yield return Landmark("covered bridge",bridge.x,bridge.z,new Vector3(bridge.x,2,bridge.z-34),3);
        yield return Landmark("clock square",-17,3,new Vector3(-17,5,-12),1.2f);
        yield return Landmark("school stand",-28,-26.1f,new Vector3(-28,3.5f,-5),2.6f);
    }
    IEnumerator Systems()
    {
        map.Toggle();Mark("map markers");yield return Wait(1.3f);Put(map,"_zoom",1.8f);Put(map,"_viewCx",-30f);Put(map,"_viewCz",-30f);Call(map,"ClampView");Call(map,"RefreshView");Mark("map zoom / pan");yield return Wait(1.7f);map.Toggle();quests.Open();Mark("main journal");yield return Wait(3);
        Put(quests,"tab",1);Put(quests,"selected",2);quests.Track("library");Mark("side journal / tracking");yield return Wait(3);
        yield return Room("图书馆");yield return Wait(2.5f);quests.Open();Put(quests,"tab",1);Put(quests,"selected",2);Mark("side completed naturally="+quests.Completed.Contains("library"));yield return Wait(3);
        ProgressManager.TrackingOpen=false;progress.Save(1);progress.OpenSaves();Mark("manual save / slot captions");yield return Wait(3);
        ProgressManager.MenuOpen=false;progress.Load(1);yield return Settle();Mark("load restores room / story / side");yield return Wait(2);
        quests.Open();Put(quests,"tab",1);Put(quests,"resetTab",1);Mark("side reset confirmation");yield return Wait(2);quests.ResetSide();yield return Wait(2);
        Put(quests,"tab",0);Put(quests,"resetTab",0);Mark("main reset confirmation");yield return Wait(2);progress.ResetStory();yield return Settle();Mark("main reset home / manual slot retained");yield return Wait(2);
    }
    IEnumerator Settings()
    {
        HelpSidebar.Live.OpenSettings();Put(HelpSidebar.Live,"tab",1);Mark("bindings");yield return Wait(3);
        string reason;GameInput.Bind("Forward",KeyCode.R,out reason);Put(HelpSidebar.Live,"status","已保存新的按键。");Mark("rebind W to R");yield return Wait(3);
        GameInput.Reset();Put(HelpSidebar.Live,"tab",0);yield return Wait(2);GameFonts.StoryChoice=1;GameInput.TextScale=1.12f;GameInput.TextSpeed=65;Mark("reading controls");yield return Wait(3);
        Put(HelpSidebar.Live,"tab",2);Mark("display controls");yield return Wait(3);HelpSidebar.Live.Close();GameFonts.StoryChoice=0;GameInput.TextScale=1;GameInput.TextSpeed=48;yield return Wait(3);
    }
    void OnDisable(){Motion=Vector2.zero;}
}
