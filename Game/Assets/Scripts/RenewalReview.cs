using System;
using System.Collections;
using System.IO;
using UnityEngine;
public class RenewalReview:MonoBehaviour
{
    IEnumerator WaitForTravel()
    {yield return null;float deadline=Time.realtimeSinceStartup+8;while((ProgressManager.Loading||GetComponent<InteriorManager>().IsTransitioning)&&Time.realtimeSinceStartup<deadline)yield return null;if(ProgressManager.Loading)throw new Exception("Travel did not finish");}
    IEnumerator Start()
    {
        yield return null;yield return null;
        if(StartMenu.IsOpen){float minutes=GetComponent<TimeOfDay>().GameMinutes;yield return new WaitForSeconds(.5f);if(GetComponent<TimeOfDay>().GameMinutes!=minutes)throw new Exception("Title screen advanced world clock");}
        foreach(char c in "他山之石镜荧樊楼瑶清寒紫薇莉莉雅景楠任务设置继续游戏图鉴床梯")if(!GameFonts.Map.HasCharacter(c)||!GameFonts.Serif.HasCharacter(c))throw new Exception("Missing bundled glyph "+c);
        GameFonts.StoryChoice=0;if(GameFonts.Story==GameFonts.Map)throw new Exception("Story/map fonts not separated");GameFonts.StoryChoice=1;if(GameFonts.Story!=GameFonts.Map)throw new Exception("Story font selector failed");GameFonts.StoryChoice=0;
        foreach(var text in FindObjectsOfType<TextMesh>(true))if(text.font!=GameFonts.Map)throw new Exception("Unmigrated world text font "+text.name);
        var w=GetComponent<WorldBuilder>();var rig=GetComponent<PlayerRig>();var stand=Array.Find(w.W.BUILDINGS,b=>b.style=="stand");var rot=Quaternion.Euler(0,stand.Yaw,0);var start=new Vector3(stand.pos[0],0,stand.pos[1])+rot*new Vector3(0,0,StandAccess.Front(stand)+.6f);
        if(!rig.Teleport(start.x,start.z))throw new Exception("Stand entry blocked");
        var top=new Vector3(stand.pos[0],0,stand.pos[1])+rot*new Vector3(0,0,StandAccess.Rear(stand)+.20f);
        if(!rig.CanPlacePortrait(top))throw new Exception("Stand central walking route blocked");
        float last=0;for(int i=0;i<100;i++){var p=Vector3.Lerp(start,top,i/99f);float y=w.GroundY(p.x,p.z);if(y+.001f<last)throw new Exception("Stand floor height drops while ascending");last=y;}
        if(last<1.8f)throw new Exception("Stand top does not reach highest tier");
        var interior=GetComponent<InteriorManager>();var school=interior.Find("school");int desks=0,beds=0;
        foreach(var t in school.Root.GetComponentsInChildren<Transform>(true))if(t.name=="StudyDesk")desks++;
        foreach(var t in interior.Find("dorm").Root.GetComponentsInChildren<Transform>(true))if(t.name=="loftbed")beds++;
        if(desks!=68||beds!=48)throw new Exception("School/dorm furniture coverage invalid "+desks+"/"+beds);
        var progress=GetComponent<ProgressManager>();var story=GetComponent<DialogueManager>();var journal=GetComponent<QuestJournal>();
        if(StartMenu.IsOpen){ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetFullPath("shots/145-start-hud-fonts-school"),"title-qa.png"));yield return new WaitForEndOfFrame();}
        StartMenu.Live.Begin(true);yield return WaitForTravel();if(interior.Current?.Id!="home"||story.Chapter!=0||journal.Completed.Count!=0)throw new Exception("New game did not reset and return home");
        journal.Completed.Add("clock");journal.Tracked="clock";progress.Save(1);string manual=Path.Combine(Application.persistentDataPath,"Saves-QA/slot-1.json"),original=File.ReadAllText(manual);
        story.RestoreProgress(2,5,false,true);progress.ResetStory();yield return WaitForTravel();if(story.Chapter!=0||!journal.Completed.Contains("clock"))throw new Exception("Main reset damaged side quests");
        int chapter=story.Chapter,line=story.Line;journal.ResetSide();if(journal.Completed.Count!=0||journal.Tracked!=null||story.Chapter!=chapter||story.Line!=line)throw new Exception("Side reset damaged main story");
        if(File.ReadAllText(manual)!=original)throw new Exception("Reset damaged manual save");
        progress.Load(1);yield return WaitForTravel();if(!journal.Completed.Contains("clock"))throw new Exception("Save continuation lost side state");
        Debug.Log("[RenewalCheck] PASS bundled glyphs/font selection; stand walk route and ten tier heights; 68 desks/48 mirrored lofts; new game at home; independent resets/manual preservation/continue load");
    }
}
