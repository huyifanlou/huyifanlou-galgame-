using System;using System.Collections.Generic;using UnityEngine;

// 日常支线只是探索目标，不向原稿插入对白或人物设定。完成记录随游戏存档保存。
public class QuestJournal : MonoBehaviour
{
    public class Daily {public string id,title,place,description,room;public Daily(string i,string t,string p,string d,string r=null){id=i;title=t;place=p;description=d;room=r;}}
    public static readonly Daily[] Dailies={
        new Daily("clock","街角的钟声","钟楼广场","去钟楼广场走走，在塔前停留片刻。"),
        new Daily("sakura","樱色小径","樱花林","离开熟悉的街道，沿樱花林的小径散步。"),
        new Daily("library","书页之间","图书馆","走进图书馆，为忙碌的一天留一点安静的时间。","library")};
    WorldBuilder world;PlayerRig rig;DialogueManager story;ProgressManager progress;InteriorManager interiors;
    public readonly HashSet<string> Completed=new HashSet<string>();public string Tracked;int tab,selected;float dwell;string dwellId;Vector2 scroll;int resetTab=-1;
    public void Init(WorldBuilder w,PlayerRig r,DialogueManager d,ProgressManager p,InteriorManager im)
    {world=w;rig=r;story=d;progress=p;interiors=im;if(Args.Has("-quests")){ProgressManager.TrackingOpen=true;tab=Args.Has("-sidequests")?1:0;}}
    public string[] Done=>new List<string>(Completed).ToArray();
    public void Restore(string[] done,string tracked){Completed.Clear();if(done!=null)foreach(string id in done)if(Array.Exists(Dailies,q=>q.id==id))Completed.Add(id);Tracked=Array.Exists(Dailies,q=>q.id==tracked)?tracked:null;dwell=0;}
    public void Open(){resetTab=-1;ProgressManager.TrackingOpen=true;ProgressManager.MenuOpen=false;if(tab==0)selected=story.Chapter;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    public void ResetSide(){if(story.Replaying)story.ReturnFromReplay();Restore(null,null);progress.Save(0);GetComponent<TimeOfDay>().ShowToast("支线任务已重置，可重新探索。");}
    public void Track(string id){Tracked=id;progress.Save(0);}
    void Update()
    {
        if(ProgressManager.Loading||HelpSidebar.IsOpen||DialogueManager.Active||IntroScreen.Live)return;
        Daily near=null;
        foreach(var q in Dailies)
        {
            if(Completed.Contains(q.id))continue;
            if(q.room!=null){if(interiors.Current?.Id==q.room)near=q;}
            else if(!interiors.Indoor)foreach(var spot in world.W.SPOTS)if(spot.name==q.place&&Vector2.Distance(new Vector2(rig.Position.x,rig.Position.z),new Vector2(spot.x,spot.z))<5)near=q;
            if(near!=null)break;
        }
        if(near==null){dwell=0;dwellId=null;return;}if(dwellId!=near.id){dwellId=near.id;dwell=0;}
        dwell+=Time.unscaledDeltaTime;if(dwell<2)return;
        Completed.Add(near.id);dwell=0;progress.Save(0);GetComponent<TimeOfDay>().ShowToast("日常已完成 · "+near.title);
    }
    public void DrawHud()
    {
        GameUI.Begin(-10);
        GameUI.FloatingText(new Rect(28,104,300,24),"主线 · CHAPTER "+Mathf.Min(story.Chapter+1,story.ChapterCount).ToString("00"),13,new Color(.93f,.81f,.59f));
        GameUI.Fill(new Rect(28,137,2,50),new Color(.85f,.73f,.52f,.72f));
        GameUI.FloatingText(new Rect(43,134,300,56),story.Finished?"初遇，未完待续":progress.ObjectiveTitle,20,GameUI.Paper,true,true);
        GameUI.FloatingText(new Rect(43,196,295,26),story.Finished?"第一卷已完成":"›  "+progress.ObjectivePlace+(progress.ObjectiveIndoor?" · 室内":""),15,new Color(.84f,.87f,.88f));
        float amount=story.Finished?1:story.Line/(float)Mathf.Max(1,story.ChapterLength);
        GameUI.Fill(new Rect(43,233,220,1),new Color(.80f,.80f,.78f,.28f));GameUI.Fill(new Rect(43,233,220*amount,1),GameUI.Gold);
        if(GUI.Button(new Rect(24,100,330,140),GUIContent.none,GUIStyle.none))Open();
        if(Tracked!=null){var q=Array.Find(Dailies,x=>x.id==Tracked);if(q!=null){GameUI.FloatingText(new Rect(28,258,310,26),"◇  支线 · "+q.title,16,new Color(.75f,.86f,.91f));GameUI.FloatingText(new Rect(43,289,295,24),Completed.Contains(q.id)?"已完成":q.place,14,GameUI.Paper);if(GUI.Button(new Rect(24,252,330,66),GUIContent.none,GUIStyle.none)){tab=1;selected=Array.FindIndex(Dailies,x=>x.id==Tracked);Open();}}}
        GameUI.End();
    }
    public void DrawJournal()
    {
        GameUI.Begin(-300);GameUI.LightSheet=true;GameUI.Folio(new Rect(150,60,1300,780),tab==0?GameUI.Gold:GameUI.Mint);
        GameUI.Fill(new Rect(805,260,601,500),tab==0?new Color(.96f,.94f,.88f):new Color(.87f,.92f,.92f));
        GUI.Label(new Rect(192,83,700,25),"FIELD NOTES  /  VOL. 01",GameUI.Text(13,new Color(.40f,.32f,.27f)));
        GUI.Label(new Rect(192,113,700,52),"旅途手记",GameUI.Text(36,GameUI.Ink,true,true));
        if(GameUI.Button(new Rect(1290,99,110,40),"返回小镇"))ProgressManager.TrackingOpen=false;
        if(GameUI.Tab(new Rect(192,197,260,45),"主线任务",GameUI.Gold,tab==0)){tab=0;selected=story.Chapter;scroll=Vector2.zero;resetTab=-1;}
        if(GameUI.Tab(new Rect(467,197,260,45),"支线日常  "+Completed.Count+" / "+Dailies.Length,GameUI.Mint,tab==1)){tab=1;selected=0;scroll=Vector2.zero;resetTab=-1;}
        GameUI.Fill(new Rect(780,260,1,500),new Color(.26f,.3f,.32f));
        scroll=GUI.BeginScrollView(new Rect(190,265,555,480),scroll,new Rect(0,0,530,Mathf.Max(470,(tab==0?story.ChapterCount:Dailies.Length)*90)));
        int count=tab==0?story.ChapterCount:Dailies.Length;
        for(int i=0;i<count;i++)
        {
            bool on=selected==i;
            string status=tab==0?(i<story.Chapter?"已完成":i==story.Chapter?"进行中":"未开启"):(Completed.Contains(Dailies[i].id)?"已完成":Tracked==Dailies[i].id?"追踪中":"可探索");
            if(GameUI.Button(new Rect(0,i*90,522,76),"",on))selected=i;
            GameUI.Fill(new Rect(0,i*90+6,3,64),tab==0?GameUI.Gold:GameUI.Mint);
            GUI.Label(new Rect(19,i*90+13,430,28),tab==0?story.ChapterTitle(i):Dailies[i].title,GameUI.Text(21,GameUI.Paper,false,true));
            GUI.Label(new Rect(20,i*90+46,430,24),status,GameUI.Text(14,on?GameUI.Gold:GameUI.Muted));
        }
        GUI.EndScrollView();
        selected=Mathf.Clamp(selected,0,Mathf.Max(0,count-1));
        float x=824;
        GUI.Label(new Rect(x,280,520,25),tab==0?"MAIN STORY":"SIDE STORY",GameUI.Text(13,GameUI.Gold));
        string title=tab==0?story.ChapterTitle(selected):Dailies[selected].title;
        GUI.Label(new Rect(x,321,535,90),title,GameUI.Text(32,GameUI.Paper,true,true));
        if(tab==0)
        {
            bool current=selected==story.Chapter&&!story.Finished;
            if(GameUI.Button(new Rect(x,575,510,34),"回看本章 / 定位对话"))story.OpenArchive(selected);
            GUI.Label(new Rect(x,425,520,145),current?progress.ObjectiveTitle+"\n\n前往「"+progress.ObjectivePlace+"」"+(progress.ObjectiveIndoor?"，进入室内后继续。":"，继续这段故事。"):(selected<story.Chapter?"这段旅程已记入回顾。":"沿着当前主线前进，故事会在合适的时候展开。"),GameUI.Text(21,GameUI.Paper,true));
            GUI.Label(new Rect(x,610,500,32),current?"本章进度  "+Mathf.RoundToInt(100f*story.Line/Mathf.Max(1,story.ChapterLength))+"%":"第一卷 · 初遇",GameUI.Text(16,GameUI.Muted));
            if(current&&GameUI.Button(new Rect(x,689,510,52),"前往剧情地点并继续",false,true))progress.TravelStory();
        }
        else
        {
            var q=Dailies[selected];bool done=Completed.Contains(q.id);
            GUI.Label(new Rect(x,425,510,150),q.description+"\n\n目的地 · "+q.place,GameUI.Text(22,GameUI.Paper,true));
            GUI.Label(new Rect(x,607,520,32),done?"这份日常已收进手记。":"到达后停留片刻，即可完成。",GameUI.Text(17,done?GameUI.Mint:GameUI.Muted));
            if(!done)
            {
                if(GameUI.Button(new Rect(x,689,245,52),Tracked==q.id?"正在追踪":"追踪任务"))Track(q.id);
                if(GameUI.Button(new Rect(x+260,689,245,52),"前往地点",false,true))progress.TravelSide(q.place,q.room!=null);
            }
        }
        GameUI.Rule(192,782,1200,new Color(.23f,.28f,.31f));GUI.Label(new Rect(192,800,870,23),"主线讲述正在发生的故事，支线留给自由探索的你。",GameUI.Text(14,GameUI.Muted));
        if(resetTab==tab)GUI.Label(new Rect(192,763,870,25),tab==0?"确认后主线回到第一章与家中，支线和手动档保留。":"确认后清空支线完成与追踪记录，主线和手动档保留。",GameUI.Text(15,GameUI.Gold));
        if(GameUI.Button(new Rect(1100,790,300,35),(resetTab==tab?"确认重置":"重置")+(tab==0?"主线任务":"支线任务"))){if(resetTab==tab){resetTab=-1;if(tab==0)progress.ResetStory();else ResetSide();}else resetTab=tab;}
        GameUI.End();
    }
}
