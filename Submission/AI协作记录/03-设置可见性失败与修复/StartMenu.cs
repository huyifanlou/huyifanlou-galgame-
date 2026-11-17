using UnityEngine;

public class StartMenu:MonoBehaviour
{
    public static bool IsOpen{get;private set;}
    Texture2D backdrop,shade;
    public enum Entry{Resume,NewGame,Settings,Quit}
    public bool PendingNewGame=>confirm;int selected;Vector2 lastPointer;
    public static StartMenu Live;bool confirm;int resumeSlot=-2;float refreshAfter;
    Entry? pendingEntry;
    public void Init()
    {
        Live=this;backdrop=Resources.Load<Texture2D>("Title/JingyingDusk");
        shade=new Texture2D(256,1,TextureFormat.RGBA32,false,true){name="TitleReadabilityGradient",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
        for(int i=0;i<256;i++)shade.SetPixel(i,0,new Color(.025f,.035f,.07f,.82f*(1-Mathf.SmoothStep(0,1,i/255f))));shade.Apply(false,true);
        bool debug=!string.IsNullOrEmpty(Args.Str("-shot"))||Args.Has("-px")||Args.Has("-pz")||Args.Has("-indoor")||Args.Has("-scenario")||Args.Has("-progresscheck")||Args.Has("-progressreload")||Args.Has("-portalcheck")||Args.Has("-archivecheck")||Args.Has("-uicheck")||Args.Has("-precisioncheck")||Args.Has("-renewalcheck")||Args.Has("-menumotioncheck")||Args.Has("-weatheruicheck")||Args.Has("-weatheruireload")||Args.Has("-campus");
        IsOpen=Args.Has("-startmenu")||!debug;
    }
    public void Open(){GetComponent<DialogueManager>().ReturnFromReplay();GetComponent<ProgressManager>().Save(0);GetComponent<DialogueManager>().PauseStory();var map=GetComponent<Minimap>();if(map.LargeOpen)map.Toggle();if(CharacterPicker.IsOpen)GetComponent<CharacterPicker>().Toggle();HelpSidebar.Live.Close();ProgressManager.MenuOpen=ProgressManager.TrackingOpen=false;confirm=false;selected=0;resumeSlot=-2;IsOpen=true;}
    void Update()
    {
        if(!IsOpen)return;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        if(pendingEntry.HasValue){var action=pendingEntry.Value;pendingEntry=null;Activate(action);return;}
        if(HelpSidebar.IsOpen)return;
        if(Input.GetKeyDown(KeyCode.DownArrow))selected=(selected+1)%4;
        if(Input.GetKeyDown(KeyCode.UpArrow))selected=(selected+3)%4;
        if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter)){Activate((Entry)selected);return;}
        if(GameInput.Down("Settings")||(Input.GetKeyDown(KeyCode.Escape)&&GameInput.CancelAvailable)){GameInput.ConsumeCancel();if(confirm)confirm=false;else Activate(Entry.Settings);}
    }
    public void Activate(Entry entry)
    {
        selected=(int)entry;
        switch(entry)
        {
            case Entry.Resume:Begin(false);break;
            case Entry.NewGame:if(GetComponent<ProgressManager>().LatestSaveSlot<0||confirm)Begin(true);else confirm=true;break;
            case Entry.Settings:HelpSidebar.Live.OpenSettings();break;
            case Entry.Quit:Debug.Log("[TitleRoute] Quit requested");Application.Quit();break;
        }
    }
    public void QueueEntry(Entry entry){pendingEntry=entry;}
    public void DrawBackdrop()
    {
        if(backdrop!=null)GUI.DrawTexture(new Rect(0,0,1600,900),backdrop,ScaleMode.ScaleAndCrop);
        else GameUI.Fill(new Rect(0,0,1600,900),new Color(.13f,.17f,.23f));
        GUI.DrawTexture(new Rect(0,0,1000,900),shade,ScaleMode.StretchToFill);
    }
    bool TitleEntry(Rect rect,string label,int index)
    {
        bool hover=rect.Contains(Event.current.mousePosition);
        if(hover&&(GameUI.Pointer-lastPointer).sqrMagnitude>1)selected=index;
        bool active=selected==index;
        bool clicked=GUI.Button(rect,GUIContent.none,GUIStyle.none);
        Color tint=active?new Color(.94f,.83f,.65f):new Color(.88f,.89f,.93f);
        GameUI.FloatingText(new Rect(rect.x,rect.y+15,22,30),active?"◆":"·",14,tint);
        GameUI.FloatingText(new Rect(rect.x+35,rect.y+6,rect.width-40,40),label,24,tint);
        GameUI.Rule(rect.x+35,rect.yMax-4,active?260:160,new Color(.85f,.77f,.66f,active?.6f:.15f));
        return clicked;
    }
    public void Begin(bool fresh)
    {IsOpen=false;var p=GetComponent<ProgressManager>();int slot=p.LatestSaveSlot;if(!fresh&&slot>=0)p.Load(slot);else p.NewGame();}
    void OnGUI()
    {
        if(!IsOpen||HelpSidebar.IsOpen)return;
        GameUI.Begin(-500);
        DrawBackdrop();
        if(Event.current.type==EventType.KeyDown)GUIUtility.keyboardControl=0;
        GUI.Label(new Rect(84,130,570,86),"他山之石",GameUI.Text(66,GameUI.Paper,false,true));
        GUI.Label(new Rect(90,231,535,33),"STONES FROM ANOTHER HILL",GameUI.Text(17,GameUI.Gold));
        GameUI.Rule(90,287,430,new Color(.77f,.66f,.43f,.55f));
        GUI.Label(new Rect(90,310,470,52),"从熟悉的家出发，走向未曾预料的故事。",GameUI.Text(17,new Color(.84f,.85f,.86f),true));
        var progress=GetComponent<ProgressManager>();if(resumeSlot== -2||Time.unscaledTime>refreshAfter){resumeSlot=progress.LatestSaveSlot;refreshAfter=Time.unscaledTime+2;}bool hasSave=resumeSlot>=0;
        if(TitleEntry(new Rect(90,398,430,58),hasSave?"继续游戏":"开始游戏",0))QueueEntry(Entry.Resume);
        if(TitleEntry(new Rect(90,478,430,55),confirm?"确认开启新旅程":"新游戏",1))QueueEntry(Entry.NewGame);
        if(TitleEntry(new Rect(90,553,430,55),"设置",2))QueueEntry(Entry.Settings);
        if(TitleEntry(new Rect(90,628,430,55),"退出游戏",3))QueueEntry(Entry.Quit);
        lastPointer=GameUI.Pointer;
        GUI.Label(new Rect(94,717,440,74),confirm?"新游戏将重置主线、支线与自动档。\n手动存档保留，再次点击确认。":"↑ / ↓ 选择    Enter 确认    Esc 设置",GameUI.Text(15,new Color(.82f,.83f,.85f),true));
        GUI.Label(new Rect(94,833,480,24),"HUYIFANLOU  /  STONES FROM ANOTHER HILL",GameUI.Text(11,new Color(.69f,.71f,.76f)));
        GameUI.End();
    }
}
