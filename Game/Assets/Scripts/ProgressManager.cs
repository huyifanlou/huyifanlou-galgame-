using System;
using System.Collections;
using System.IO;
using UnityEngine;

[Serializable] public class ProgressSave
{
    public int version=1,chapter,line,day,weather;
    public int weatherSettingsVersion;
    public float rainStrength,snowStrength,precipitationSize;
    public string savedAt,room,avatar,world;
    public Vector3 position;
    public float minutes,speed,yaw,facing,distance,pitch,fpPitch;
    public bool dialogue,linked,firstPerson;
    public string[] completedSide;public string trackedSide;
}

// 存档与空间化剧情入口：不改原剧本文字，不生成额外剧情。
public class ProgressManager : MonoBehaviour
{
    public static bool MenuOpen,Loading,TrackingOpen;
    WorldBuilder world;PlayerRig rig;TimeOfDay clock;DialogueManager dialogue;InteriorManager interiors;
    Font font;GUIStyle buttonStyle;readonly string[] captions=new string[4];GameObject encounter;string encounterKey;string status="";float statusUntil;
    string DirectoryPath => Path.Combine(Application.persistentDataPath,Args.Has("-menuroutecheck")?"Saves-QA-Menu147-"+System.Text.RegularExpressions.Regex.Replace(Args.Str("-qaid")??"default","[^a-zA-Z0-9_-]",""):(Args.Has("-progresscheck")||Args.Has("-progressreload")||Args.Has("-uicheck")||Args.Has("-uireload")||Args.Has("-precisioncheck")||Args.Has("-archivecheck")||Args.Has("-portraitpaircheck")||Args.Has("-portraitassetcheck")||Args.Has("-displaycheck")||Args.Has("-standeecheck")||Args.Has("-renewalcheck")||Args.Has("-submissioncapture"))?"Saves-QA":"Saves");
    string Slot(int slot)=>Path.Combine(DirectoryPath,"slot-"+slot+".json");
    public void Init(WorldBuilder w,PlayerRig r,TimeOfDay t,DialogueManager d,InteriorManager im)
    { world=w;rig=r;clock=t;dialogue=d;interiors=im;MenuOpen=Loading=TrackingOpen=false;
      font=GameFonts.Map;
      if(Args.Has("-progresscheck"))StartCoroutine(Check());
      if(Args.Has("-precisioncheck"))StartCoroutine(CheckClosure());
      if(Args.Has("-progressreload"))StartCoroutine(CheckReload());
      if(Args.Has("-savemenu")){MenuOpen=true;RefreshCaptions();} }
    void Tell(string message){status=message;statusUntil=Time.unscaledTime+5;clock.ShowToast(message);}
    public void SceneCompleted(){TrackingOpen=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;Tell("本章已完成，下一章已更新至任务手记。");Save(0);}
    public string ObjectivePlace=>Target();
    public bool ObjectiveIndoor=>RequiresInterior();
    public string ObjectiveTitle=>dialogue.Finished?"第一卷已完成":dialogue.ChapterTitle(dialogue.Chapter);
    public bool HasObjective=>dialogue.Linked&&!dialogue.Finished;
    public Vector3 MapDestination
    {
        get {foreach(var spot in world.W.SPOTS)if(spot.name==Target())return new Vector3(spot.x,world.GroundY(spot.x,spot.z),spot.z);return world.Spawn;}
    }
    string Target()
    {
        if((dialogue.CurrentLine?.scene??"").Contains("幕前"))return "家";
        string location=(dialogue.RouteLine?.location??"").Split('｜')[0];
        if(location.Contains("学校南边"))return "钟楼广场";
        if(location.Contains("家")||location.Contains("别墅")||location.Contains("穿衣镜")||location.Contains("客厅")||location.Contains("客房")||location.Contains("玄关")||location.Contains("餐厅")||location.Contains("同一间屋")||location.Contains("四间房")||location.Contains("上学日"))return "家";
        if(location.Contains("超市"))return "超市";
        if(location.Contains("图书馆"))return "图书馆";
        if(location.Contains("操场")||location.Contains("测试区")||location.Contains("集合区"))return "操场";
        if(location.Contains("校门")||location.Contains("学校集合")||location.Contains("车队")||location.Contains("车厢"))return "门卫";
        if(location.Contains("教")||location.Contains("会议")||location.Contains("校长")||location.Contains("学校"))return "教学楼";
        return "家";
    }
    Vector3 Anchor()
    {
        if(interiors.Indoor && interiors.Current.Name==Target())
        {
            var room=interiors.Current;
            for(int i=0;i<16;i++)
            {var p=new Vector3(room.Spawn.x+(i%4-1)*1.2f,rig.Position.y,room.Spawn.y+2+(i/4)*1.2f);
             if(p.x>room.X1+.6f&&p.x<room.X2-.6f&&p.z>room.Z1+.6f&&p.z<room.Z2-.6f&&!room.Collides(p.x,p.z,p.y))return p;}
            return new Vector3(room.Spawn.x,rig.Position.y,room.Spawn.y);
        }
        return MapDestination;
    }
    bool RequiresInterior()
    {
        string location=(dialogue.RouteLine?.location??"").Split('｜')[0];
        return (Target()=="家"&&!location.Contains("门前"))||Target()=="超市"||Target()=="图书馆"||location.Contains("教室")||location.Contains("走廊")||location.Contains("会议")||location.Contains("办公室");
    }
    public bool CanInteract=>dialogue.Linked&&!DialogueManager.Active&&!MenuOpen&&!TrackingOpen&&!Loading&&Near();
    bool Near()=>!dialogue.Finished && (!RequiresInterior()||interiors.Indoor) && (!interiors.Indoor||interiors.Current.Name==Target()) &&
        new Vector2(rig.Position.x-Anchor().x,rig.Position.z-Anchor().z).sqrMagnitude<3.4f*3.4f;
    public void RequestStory()
    {if(dialogue.Finished){Tell("第一卷已结束，可在旅程记录中重新开始");MenuOpen=true;RefreshCaptions();return;}
     if(Near()){TrackingOpen=false;dialogue.ResumeStory();}else {TrackingOpen=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;Tell("下一段剧情：前往「"+Target()+"」，可在任务手记中直接前往。");}}
    public void TravelStory()
    {
        if(Loading||interiors.IsTransitioning||!HasObjective)return;
        StartCoroutine(Travel());
    }
    public void TravelMap(float x,float z)
    {if(world.W.RIVER!=null&&ClosedAreas.Contains(x,z)){Tell("游乐场设施维护，暂不开放。");return;}if(!Loading&&!interiors.IsTransitioning)StartCoroutine(Travel(false,new Vector2(x,z)));}
    public void TravelSide(string place,bool inside)
    {if(!Loading&&!interiors.IsTransitioning)StartCoroutine(Travel(false,null,place,inside));}
    IEnumerator Travel(bool continueStory=true,Vector2? mapPoint=null,string place=null,bool enterPlace=false)
    {
        Loading=true;MenuOpen=TrackingOpen=false;dialogue.PauseStory();
        string destination=place??Target();bool inside=continueStory?RequiresInterior():enterPlace;
        Vector3 outside=MapDestination;if(place!=null)foreach(var spot in world.W.SPOTS)if(spot.name==place)outside=new Vector3(spot.x,0,spot.z);
        if(interiors.Indoor&&(!inside||interiors.Current.Name!=destination))
        {interiors.Exit();while(interiors.IsTransitioning)yield return null;}
        if(inside&&!interiors.Indoor)
        {
            if(!interiors.ExteriorDoor(destination,out var door,out var normal)||!rig.Teleport(door.x+normal.x*1.55f,door.y+normal.y*1.55f)||!interiors.Enter(destination))
            {Loading=false;Tell("该地点暂不可达，请按指引步行前往");yield break;}
            while(interiors.IsTransitioning)yield return null;
        }
        if(inside)
        {var room=interiors.Current;var point=new Vector3(room.Spawn.x,room.FloorY[0],room.Spawn.y);rig.SetIndoorView(true,new Vector2(point.x,point.z),room.FloorAt(point.x,point.z,rig.Position.y));}
        else if(!rig.Teleport(mapPoint?.x??outside.x,mapPoint?.y??outside.z)){Loading=false;Tell("没有安全落点，请步行靠近");yield break;}
        yield return null;
        Loading=false;if(continueStory){dialogue.ResumeStory();Save(0);}else Tell("已到达目的地。");
    }
    void Update()
    {
        if(Loading||IntroScreen.Live||HelpSidebar.IsOpen)return;
        if(!MenuOpen&&!CharacterPicker.IsOpen&&!DialogueManager.Active&&GameInput.Down("Journal"))
        {TrackingOpen=!TrackingOpen;if(TrackingOpen)GetComponent<QuestJournal>()?.Open();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        if(!MenuOpen&&!CharacterPicker.IsOpen&&!DialogueManager.Active&&GameInput.Down("Travel")){TravelStory();return;}
        if(TrackingOpen&&GameInput.Cancel())TrackingOpen=false;
        if(!CharacterPicker.IsOpen&&GameInput.Down("Saves")){TrackingOpen=false;MenuOpen=!MenuOpen;if(MenuOpen)RefreshCaptions();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        if(MenuOpen&&GameInput.Cancel())MenuOpen=false;
        if(GameInput.Down("QuickSave"))Save(1);
        if(GameInput.Down("QuickLoad"))Load(1);
        if(!MenuOpen&&!CharacterPicker.IsOpen&&!DialogueManager.Active&&dialogue.Linked&&Near()&&GameInput.Down("StoryInteract"))RequestStory();
        bool visible=dialogue.Linked&&!dialogue.Finished&&!DialogueManager.Active&&!MenuOpen&&(!RequiresInterior()||interiors.Indoor)&&(!interiors.Indoor||interiors.Current.Name==Target());
        string key=Target()+"|"+dialogue.EncounterSpeaker+"|"+(interiors.Current?.Id??"outside");
        if(key!=encounterKey)
        {
            if(encounter!=null)Destroy(encounter);encounter=null;encounterKey=key;
            string speaker=dialogue.EncounterSpeaker;
            if(speaker!=null)
            {encounter=new GameObject("剧情角色_"+speaker);encounter.transform.SetParent(world.Root,false);
             foreach(var definition in PlayerAvatar.Roster)if(definition.label==speaker){PortraitStandee.Create(encounter.transform,definition);break;}}
        }
        if(encounter!=null){encounter.SetActive(visible);encounter.transform.position=Anchor();
            var direction=rig.Position-encounter.transform.position;direction.y=0;
            if(direction.sqrMagnitude>.01f)encounter.transform.rotation=Quaternion.LookRotation(direction);}
    }
    ProgressSave Capture()
    {
        var result=new ProgressSave{world=Args.Has("-campus")?"campus":"town",savedAt=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),chapter=dialogue.Chapter,line=dialogue.Line,
            position=rig.Position,room=interiors.Current?.Id,avatar=world.Player.GetComponentInChildren<PlayerAvatar>().Id,
            weather=(int)GetComponent<Weather>().Current,day=clock.DayNum,minutes=clock.GameMinutes,speed=clock.TimeSpeed,dialogue=DialogueManager.Active,linked=dialogue.Linked};
        GetComponent<Weather>().WriteSettings(result);
        var journal=GetComponent<QuestJournal>();result.completedSide=journal?.Done;result.trackedSide=journal?.Tracked;
        rig.SaveView(result);return result;
    }
    public void Save(int slot)
    {
        if(dialogue.Replaying){Tell("回看不覆盖主线进度，返回进度后可存档。");return;}
        if(Loading||interiors.IsTransitioning){Tell("切换场景结束后再存档");return;}
        try{Directory.CreateDirectory(DirectoryPath);string path=Slot(slot),temp=path+".tmp";
            File.WriteAllText(temp,JsonUtility.ToJson(Capture(),true));
            if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
            RefreshCaptions();Tell(slot==0?"剧情进度已自动保存":"已保存至存档 "+slot);}
        catch(Exception error){Debug.LogWarning("[Save] "+error.Message);Tell("保存失败，原存档保留");}
    }
    ProgressSave Read(int slot)
    {
        var value=JsonUtility.FromJson<ProgressSave>(File.ReadAllText(Slot(slot)));
        if(value==null||value.version!=1||value.world!=(Args.Has("-campus")?"campus":"town")||!dialogue.ValidProgress(value.chapter,value.line)||value.day<1||value.weather<0||value.weather>2||
           !Finite(value.position.x)||!Finite(value.position.y)||!Finite(value.position.z)||
           !Finite(value.yaw)||!Finite(value.facing)||!Finite(value.pitch)||!Finite(value.fpPitch)||
           !Finite(value.distance)||value.distance<2||value.distance>140||!Finite(value.minutes)||value.minutes<0||value.minutes>=1440||
           !Finite(value.speed)||value.speed<0||value.speed>1000)throw new Exception("无效或不兼容的存档");
        if(!string.IsNullOrEmpty(value.room))
        {var room=interiors.Find(value.room);if(room==null||value.position.x<room.X1+.3f||value.position.x>room.X2-.3f||
             value.position.z<room.Z1+.3f||value.position.z>room.Z2-.3f)throw new Exception("室内落点无效");}
        else if(new Vector2(value.position.x,value.position.z).magnitude>world.W.BOUND)throw new Exception("室外落点无效");
        return value;
    }
    static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
    public void Load(int slot)
    {
        if(Loading||interiors.IsTransitioning)return;
        try{StartCoroutine(Restore(Read(slot)));}catch(Exception error){Tell("无法读取存档："+error.Message);}
    }
    IEnumerator Restore(ProgressSave data)
    {
        Loading=true;MenuOpen=TrackingOpen=false;dialogue.PauseStory();
        if(interiors.Indoor){interiors.Exit();while(interiors.IsTransitioning)yield return null;}
        if(!string.IsNullOrEmpty(data.room))
        {
            var room=interiors.Find(data.room);
            if(interiors.ExteriorDoor(room.Name,out var door,out var normal))rig.Teleport(door.x+normal.x*1.55f,door.y+normal.y*1.55f);
            if(!interiors.Enter(room.Name)){Loading=false;Tell("无法恢复室内场景");yield break;}
            while(interiors.IsTransitioning)yield return null;
            var safe=room.Collides(data.position.x,data.position.z,data.position.y)?room.Spawn:new Vector2(data.position.x,data.position.z);rig.SetIndoorView(true,safe,room.FloorAt(safe.x,safe.y,data.position.y));
        }
        else if(world.W.RIVER!=null&&ClosedAreas.Contains(data.position.x,data.position.z))rig.Teleport(ClosedAreas.SafeGate.x,ClosedAreas.SafeGate.y);
        else rig.Teleport(data.position.x,data.position.z);
        GetComponent<Weather>().ReadSettings(data);GetComponent<Weather>().Restore((Weather.Kind)data.weather);
        clock.DayNum=data.day;clock.GameMinutes=data.minutes;clock.TimeSpeed=data.speed;
        world.Player.GetComponentInChildren<PlayerAvatar>().SetCharacter(CharacterDef.FromId(data.avatar));
        rig.RestoreView(data);dialogue.RestoreProgress(data.chapter,data.line,data.dialogue,data.linked);
        GetComponent<QuestJournal>()?.Restore(data.completedSide,data.trackedSide);
        Loading=false;Tell("存档已恢复");
    }
    void RefreshCaptions()
    {
        for(int i=0;i<4;i++)
        {captions[i]=i==0?"自动存档":"存档 "+i;
         try{var data=Read(i);captions[i]+="  "+data.savedAt+"\n第 "+Mathf.Min(data.chapter+1,dialogue.ChapterCount)+" 章 · "+(string.IsNullOrEmpty(data.room)?"室外":interiors.Find(data.room)?.Name);}
         catch{captions[i]+="  空 / 不可用";}}
    }
    public void OpenSaves(){MenuOpen=true;TrackingOpen=false;RefreshCaptions();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    public int LatestSaveSlot
    {get{int best=-1;string stamp="";for(int i=0;i<4;i++)try{var data=Read(i);if(best<0||string.CompareOrdinal(data.savedAt,stamp)>0){best=i;stamp=data.savedAt;}}catch{}return best;}}
    public void NewGame(){if(!Loading&&!interiors.IsTransitioning)StartCoroutine(RestartStory(true));}
    public void ResetStory(){if(!Loading&&!interiors.IsTransitioning)StartCoroutine(RestartStory());}
    IEnumerator RestartStory(bool fresh=false)
    {
        dialogue.ReturnFromReplay();dialogue.RestoreProgress(0,0,false,true);MenuOpen=TrackingOpen=false;
        if(fresh){GetComponent<QuestJournal>().Restore(null,null);clock.DayNum=1;clock.GameMinutes=480;clock.TimeSpeed=1;GetComponent<Weather>().Restore(Weather.Kind.Clear);world.Player.GetComponentInChildren<PlayerAvatar>().SetCharacter(CharacterDef.FromId("fanlou"));}
        yield return Travel(false,null,"家",true);Save(0);Tell(fresh?"新旅程已开始，欢迎回家。":"主线已重置至第一章，手动存档和支线保留。");
    }
    void OnGUI()
    {
        if(font==null||IntroScreen.Live||Args.NoHud||HelpSidebar.IsOpen||DialogueManager.Active||CharacterPicker.IsOpen)return;
        var journal=GetComponent<QuestJournal>();
        if(!MenuOpen&&!TrackingOpen&&dialogue.Linked)journal?.DrawHud();
        if(TrackingOpen&&!MenuOpen){journal?.DrawJournal();return;}
        if(!MenuOpen)return;
        GameUI.Begin(-400);GameUI.Panel(new Rect(290,95,1020,710));
        GUI.Label(new Rect(334,119,800,24),"MEMORIES  /  YOUR JOURNEY",GameUI.Text(13,GameUI.Gold));
        GUI.Label(new Rect(334,153,800,48),"旅程记录",GameUI.Text(34,GameUI.Paper,false,true));
        if(GameUI.Button(new Rect(1150,137,105,40),"返回"))MenuOpen=false;
        for(int i=0;i<4;i++)
        {
            float y=238+i*112;GameUI.Fill(new Rect(330,y,938,95),new Color(.11f,.15f,.19f));
            GUI.Label(new Rect(352,y+16,600,65),captions[i]??"",GameUI.Text(19,GameUI.Paper,true));
            if(i>0&&GameUI.Button(new Rect(1010,y+26,100,40),"保存"))Save(i);
            if(GameUI.Button(new Rect(1120,y+26,118,40),"读取",false,true))Load(i);
        }
        if(GameUI.Button(new Rect(330,711,245,43),"章节回顾 / 剧情重置"))dialogue.OpenArchive();
        if(Time.unscaledTime<statusUntil)GUI.Label(new Rect(600,716,660,50),status,GameUI.Text(16,GameUI.Muted,true));
        GameUI.End();
    }
    IEnumerator CheckClosure()
    {
        yield return null;var original=Capture();var previous=rig.Position;
        if(rig.Teleport(50,40)||(rig.Position-previous).magnitude>.01f)throw new Exception("封闭区直接传送未拦截");
        TravelMap(50,40);yield return null;
        if(interiors.Current?.Id!=original.room&&!(interiors.Current==null&&string.IsNullOrEmpty(original.room)))throw new Exception("被拒绝的传送改变了房间");
        var old=JsonUtility.FromJson<ProgressSave>(JsonUtility.ToJson(original));old.room="";old.position=new Vector3(50,0,40);
        yield return Restore(old);
        if(interiors.Indoor||ClosedAreas.Contains(rig.Position.x,rig.Position.z)||Vector2.Distance(new Vector2(rig.Position.x,rig.Position.z),ClosedAreas.SafeGate)>.4f)throw new Exception("封闭区域旧档落点未迁移");
        yield return Restore(original);Debug.Log("[ClosureCheck] PASS 直接传送、室内地图传送拒绝、旧档落点迁移与原房间恢复");
    }
    IEnumerator CheckReload()
    {
        yield return null;var data=Read(1);yield return Restore(data);
        if(dialogue.Chapter!=data.chapter||dialogue.Line!=data.line||(rig.Position-data.position).magnitude>.3f)throw new Exception("跨进程恢复失败");
        Debug.Log("[ProgressReload] PASS 从独立进程恢复持久文件");
        if(Args.Has("-savemenu")){MenuOpen=true;RefreshCaptions();}
    }
    IEnumerator Check()
    {
        yield return null;var original=Capture();Save(1);
        if(interiors.Indoor){interiors.Exit();while(interiors.IsTransitioning)yield return null;}
        rig.Teleport(-17,-4);dialogue.RestoreProgress(dialogue.ChapterCount,0,false,true);
        yield return Restore(Read(1));
        if(dialogue.Chapter!=original.chapter||dialogue.Line!=original.line||(rig.Position-original.position).magnitude>.3f)throw new Exception("存读档往返失败");
        if(!dialogue.CheckTravelBoundary())throw new Exception("分场返回探索失败");
        TrackingOpen=false;dialogue.PauseStory();
        if(interiors.Indoor){interiors.Exit();while(interiors.IsTransitioning)yield return null;}
        yield return Travel();
        if(!DialogueManager.Active)throw new Exception("地点互动未开启剧情");
        File.WriteAllText(Slot(2),"broken");bool rejected=false;try{Read(2);}catch{rejected=true;}if(!rejected)throw new Exception("损坏档未拦截");
        dialogue.RestoreProgress(dialogue.ChapterCount,0,false,true);RequestStory();MenuOpen=false;
        foreach(string id in new[]{"home","school","supermarket","library"})
        {
            var room=interiors.Find(id);var fixture=JsonUtility.FromJson<ProgressSave>(JsonUtility.ToJson(original));
            fixture.room=id;fixture.position=new Vector3(room.Spawn.x,room.FloorY[0],room.Spawn.y);fixture.dialogue=true;
            yield return Restore(fixture);Save(3);var saved=Read(3);yield return Restore(saved);
            if(interiors.Current?.Id!=id||(rig.Position-fixture.position).magnitude>.3f||!DialogueManager.Active)throw new Exception("室内存读档恢复失败 "+id);
            Debug.Log("[ProgressCheck] 室内往返 PASS "+id);
        }
        dialogue.RestoreProgress(0,0,false,true);yield return Travel();
        if(interiors.Current?.Id!="home"||!DialogueManager.Active)throw new Exception("主线跨室内传送失败");
        if(!dialogue.CheckTravelBoundary())throw new Exception("传送后分场失败");
        yield return Travel();
        if(!DialogueManager.Active)throw new Exception("章节入口传送失败");
        yield return Travel(false,new Vector2(-17,-5));
        if(interiors.Indoor||DialogueManager.Active)throw new Exception("自由地图传送失败");
        var quests=GetComponent<QuestJournal>();quests.Restore(null,null);
        yield return Travel(false,null,"图书馆",true);yield return new WaitForSecondsRealtime(2.2f);
        if(!quests.Completed.Contains("library"))throw new Exception("探索支线未完成");
        Debug.Log("[QuestCheck] PASS 前往图书馆并实际停留后完成支线");
        quests.Restore(new[]{"clock"},"sakura");Save(3);quests.Restore(null,null);
        yield return Restore(Read(3));
        if(!quests.Completed.Contains("clock")||quests.Tracked!="sakura")throw new Exception("支线进度存读失败");
        yield return Restore(original);
        Debug.Log("[QuestCheck] PASS 支线完成与追踪状态存读，旧档空字段兼容");
        Debug.Log("[ProgressCheck] PASS 存读档往返、主线室内外传送与续读、自由地图传送、损坏档拒绝、通关后入口安全；目录 "+DirectoryPath);
    }
}
