using System.Linq;
using UnityEngine;

// 原按键侧栏升级为统一设置；键位从 GameInput 直接读取，修改即时生效并持久保存。
public partial class HelpSidebar : MonoBehaviour
{
    public static bool IsOpen {get;private set;}
    public static HelpSidebar Live;int tab=1;Vector2 scroll;string capture,status="";int openedFrame;
    void Awake(){Live=this;IsOpen=false;}
    void Start(){if(Args.Has("-uicheck"))GameInput.Check();if(Args.Has("-uireload")){if(GameInput.Find("Forward").Key!=KeyCode.R||Mathf.Abs(GameInput.TextScale-1.25f)>.01f)throw new System.Exception("按键跨进程恢复失败");Debug.Log("[UIReload] PASS 重启恢复重映射 R");}if(Args.Has("-settings")||Args.Has("-help"))Open();}
    public void Open(){IsOpen=true;capture=null;openedFrame=Time.frameCount;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    public void OpenSettings(){if(tab==3)tab=0;Open();if(Args.Has("-menumotioncheck"))Debug.Log("[MenuCheck] settings open");}
    public void Close(){GetComponent<Weather>()?.FlushPreferences();IsOpen=false;capture=null;}
    void LateUpdate()
    {
        if((IntroScreen.Live&&!IsOpen)||Args.NoHud)return;
        bool escape=Input.GetKeyDown(KeyCode.Escape);
        if(IsOpen){if(capture==null&&Time.frameCount>openedFrame&&(GameInput.Down("Settings")||(WeatherOpen&&GameInput.Down("Weather")))){Close();return;}Cursor.lockState=CursorLockMode.None;Cursor.visible=true;if(escape&&GameInput.CancelAvailable){GameInput.ConsumeCancel();if(capture!=null)capture=null;else Close();}return;}
        bool free=!DialogueManager.Active&&!CharacterPicker.IsOpen&&!ProgressManager.MenuOpen&&!ProgressManager.TrackingOpen;
        if(free&&!ProgressManager.Loading&&!(GetComponent<Minimap>()?.LargeOpen??false)&&GameInput.Down("Weather")){OpenWeather();return;}
        if(!ProgressManager.Loading&&(GameInput.Down("Settings")||(escape&&GameInput.CancelAvailable&&free&&!(GetComponent<Minimap>()?.LargeOpen??false)))){GameInput.ConsumeCancel();OpenSettings();}
    }
    void OnGUI()
    {
        if(!IsOpen||Args.NoHud)return;
        GameUI.Begin(-600);GameUI.LightSheet=true;
        if(StartMenu.IsOpen)StartMenu.Live.DrawBackdrop();
        if(tab==3){DrawWeather();GameUI.End();return;}
        GameUI.Folio(new Rect(170,70,1260,760),GameUI.Gold);
        GameUI.Fill(new Rect(190,195,230,584),new Color(.89f,.87f,.81f));
        GUI.Label(new Rect(210,94,700,28),"PREFERENCES  /  STONES FROM ANOTHER HILL",GameUI.Text(13,new Color(.40f,.30f,.29f)));
        GUI.Label(new Rect(210,124,700,48),"设置",GameUI.Text(34,GameUI.Ink,true,true));
        if(GameUI.Button(new Rect(1280,105,105,38),"关闭"))Close();
        string[] tabs={"阅读体验","按键绑定","画面","天气"};
        for(int i=0;i<tabs.Length;i++)if(GameUI.Tab(new Rect(210,215+i*64,190,48),tabs[i],new[]{GameUI.Rose,GameUI.Gold,GameUI.Sky,GameUI.Mint}[i],tab==i)){tab=i;capture=null;scroll=Vector2.zero;}
        GUI.Label(new Rect(214,702,172,90),"偏好自动保存。\nEsc 打开或关闭设置。",GameUI.Text(15,new Color(.38f,.40f,.38f),true));
        GameUI.Fill(new Rect(433,195,1,584),new Color(.23f,.28f,.31f));
        if(tab==1)DrawBindings();else if(tab==0)DrawReading();else DrawDisplay();
        GameUI.End();
    }
    void DrawBindings()
    {
        GUI.Label(new Rect(470,199,850,36),capture==null?"选择按键，即可重新绑定":"正在等待新的按键…",GameUI.Text(23,GameUI.Paper));
        GUI.Label(new Rect(470,240,850,32),status.Length>0?status:"点击右侧修改主按键；备用键保留，同一场景的冲突会被提示。",GameUI.Text(15,GameUI.Muted));
        scroll=GUI.BeginScrollView(new Rect(465,285,900,425),scroll,new Rect(0,0,872,GameInput.Bindings.Length*47+7*47));
        float y=0;string group="";
        foreach(var binding in GameInput.Bindings.OrderBy(b=>System.Array.IndexOf(new[]{"探索","交互","剧情","全局","时间与天气","人物图鉴"},b.group)))
        {
            if(group!=binding.group){group=binding.group;GUI.Label(new Rect(8,y+8,500,30),group,GameUI.Text(17,GameUI.Gold));y+=47;}
            GUI.Label(new Rect(8,y+9,485,30),binding.label,GameUI.Text(18,GameUI.Paper));
            if(GameUI.Button(new Rect(515,y+2,316,37),capture==binding.id?"请按新键…":binding.Display,capture==binding.id)){capture=binding.id;status="";openedFrame=Time.frameCount;}
            GameUI.Rule(8,y+44,823,new Color(.16f,.21f,.25f));y+=47;
        }
        GUI.EndScrollView();
        GUI.Label(new Rect(475,724,650,46),"固定第一人称：鼠标环顾 · Esc 打开设置\n地图：拖拽平移 · 点地标确认 · 双击空白传送",GameUI.Text(14,GameUI.Muted,true));
        if(GameUI.Button(new Rect(1170,737,190,40),"恢复默认按键")){GameInput.Reset();capture=null;status="已恢复默认按键。";}
        if(capture!=null&&Time.frameCount>openedFrame&&Event.current.type==EventType.KeyDown&&Event.current.keyCode!=KeyCode.None&&Event.current.keyCode!=KeyCode.Escape)
        {string reason;if(GameInput.Bind(capture,Event.current.keyCode,out reason)){status="已保存新的按键。";capture=null;}else status=reason;Event.current.Use();}
    }
    void DrawReading()
    {
        GUI.Label(new Rect(480,210,800,42),"让故事以舒服的速度发生。",GameUI.Text(27,GameUI.Paper,true,true));
        GUI.Label(new Rect(480,265,180,30),"主线阅读字体",GameUI.Text(17,GameUI.Gold));
        if(GameUI.Button(new Rect(680,256,260,36),"思源宋体 · 叙事",GameFonts.StoryChoice==0))GameFonts.StoryChoice=0;
        if(GameUI.Button(new Rect(955,256,260,36),"思源黑体 · 清晰",GameFonts.StoryChoice==1))GameFonts.StoryChoice=1;
        GUI.Label(new Rect(480,298,500,36),"对白字号",GameUI.Text(20,GameUI.Gold));
        float scale=GUI.HorizontalSlider(new Rect(485,350,600,24),GameInput.TextScale,.85f,1.25f);if(Mathf.Abs(scale-GameInput.TextScale)>.002f)GameInput.TextScale=scale;
        GUI.Label(new Rect(1140,335,150,36),Mathf.RoundToInt(scale*100)+"%",GameUI.Text(24,GameUI.Paper));
        GUI.Label(new Rect(480,415,500,36),"文字显示速度",GameUI.Text(20,GameUI.Gold));
        float speed=GUI.HorizontalSlider(new Rect(485,467,600,24),GameInput.TextSpeed,20,100);if(Mathf.Abs(speed-GameInput.TextSpeed)>.2f)GameInput.TextSpeed=speed;
        GUI.Label(new Rect(1140,452,150,36),Mathf.RoundToInt(speed)+" 字 / 秒",GameUI.Text(20,GameUI.Paper));
        GameUI.Fill(new Rect(480,560,850,150),GameUI.Paper);GUI.Label(new Rect(510,585,790,106),"这个世界上，有很多想不到的事。\n而故事，从今天开始。",GameUI.Text(Mathf.RoundToInt(26*scale),GameUI.Ink,true,false,TextAnchor.UpperLeft,true));
    }
    void DrawDisplay()
    {
        GUI.Label(new Rect(480,210,700,48),"画面与显示",GameUI.Text(28,GameUI.Paper,true,true));
        GameUI.Fill(new Rect(466,285,872,112),new Color(.85f,.88f,.88f));
        GameUI.Fill(new Rect(466,405,872,112),new Color(.92f,.89f,.83f));
        GUI.Label(new Rect(480,308,500,32),"显示模式",GameUI.Text(21,GameUI.Gold));
        if(GameUI.Button(new Rect(1000,295,300,46),Screen.fullScreen?"全屏 · 切换为窗口":"窗口 · 切换为全屏"))
        {if(Screen.fullScreen)Screen.SetResolution(1600,900,FullScreenMode.Windowed);else Screen.SetResolution(Display.main.systemWidth,Display.main.systemHeight,FullScreenMode.FullScreenWindow);}
        if(GameUI.Button(new Rect(480,610,820,48),"返回开始界面"))StartMenu.Live.Open();
        GUI.Label(new Rect(480,424,500,32),"帧率模式",GameUI.Text(21,GameUI.Gold));
        if(GameUI.Button(new Rect(1000,411,300,46),QualitySettings.vSyncCount>0?"垂直同步":Application.targetFrameRate<0?"不限制帧率":"锁定 "+Application.targetFrameRate))GetComponent<TimeOfDay>()?.CycleFrameMode();
    }
}
