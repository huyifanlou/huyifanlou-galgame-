using System.Collections.Generic;
using UnityEngine;

// 对话运行时（IMGUI，对齐 Hud/IntroScreen 的纯代码 UI 路线）。
// 入口：命令行 -scenario <章号> [-scenline <句序>]；世界里按 B 开/继续。
// 播放：台词带立绘卡+名字条；旁白/内心进对话框；演出居中斜体；结束卡大字。
// 点击/空格/回车推进（打字机未尽先补全），Esc 退出回世界。对话期间 PlayerRig 锁移动。
public class DialogueManager : MonoBehaviour
{
    public static bool Active { get; private set; }

    float SPEED => GameInput.TextSpeed;   // 打字机 字/秒
    List<ScenarioChapter> _chapters;
    int _ci, _li;
    TimeOfDay _tod;

    Font _font;
    Texture2D _white;
    readonly Dictionary<string, Texture2D> _portraits = new Dictionary<string, Texture2D>();
    GUIStyle _name, _body, _narr, _thought, _dir, _card, _scene, _cue, _meta, _hint;
    float _shown;
    bool _instant;
    float _fade;
    public bool Replaying {get;private set;}
    int _returnChapter,_returnLine;bool _returnActive,_returnLinked,_confirmReset;
    string _find="";
    bool _auto, _skip, _logOpen;
    float _autoTimer, _skipTimer;
    Vector2 _logScroll;
    Vector2 _textScroll;
    ScenarioLine _heightLine;float _heightScale,_heightCache;
    string _slotL,_slotR,_slotsScene;int _activeSide,_slotsChapter=-1;
    Vector2 _tocScroll;
    int _logChapter = -1;   // 剧情浏览的章节目录过滤：-1=全部章节
    bool _scrollToCur;   // 打开浏览后把当前句滚进视野（列表含后续句，不能滚到最底）
    GUIStyle _btn, _logEntry, _logHead, _logCur, _logFut, _logHeadFut, _toc, _tocOn;

    public string ChapterTitle(int chapter)=>_chapters[Mathf.Clamp(chapter,0,_chapters.Count-1)].title;
    public int Chapter => _ci;
    public int Line => _li;
    public int ChapterLength => Finished?0:_chapters[_ci].lines.Count;
    public int ChapterCount => _chapters.Count;
    public bool Finished => _ci >= _chapters.Count;
    public bool Linked = true;
    public ScenarioLine CurrentLine => Cur;
    public ScenarioLine RouteLine
    {
        get { if(Finished)return null; var lines=_chapters[_ci].lines;
            for(int i=0;i<lines.Count;i++)if(lines[i].type!=LineType.Card&&!string.IsNullOrEmpty(lines[i].location)&&!lines[i].location.StartsWith("黑屏"))return lines[i];return Cur; }
    }
    public string EncounterSpeaker
    {
        get { if(Finished)return null;var lines=_chapters[_ci].lines;
            for(int i=_li;i<lines.Count;i++){if(i>_li&&lines[i].scene!=lines[_li].scene)break;
                if(lines[i].speaker!="樊楼")foreach(var def in PlayerAvatar.Roster)if(def.label==lines[i].speaker)return def.label;}
            return null; }
    }
    public void ResumeStory() { if(!Finished)Open(); }
    public void PauseStory() { Active=false;_logOpen=false;_auto=false;_skip=false; }
    public bool ValidProgress(int chapter,int line) => chapter>=0&&chapter<=_chapters.Count&&
        (chapter==_chapters.Count?line==0:line>=0&&line<_chapters[chapter].lines.Count);
    public void RestoreProgress(int chapter,int line,bool active,bool linked)
    { Replaying=false;PauseStory();_ci=chapter;_li=line;Linked=linked;if(active&&!Finished)Open(); }

    ScenarioLine Cur => (_chapters != null && _ci >= 0 && _ci < _chapters.Count
                         && _li >= 0 && _li < _chapters[_ci].lines.Count) ? _chapters[_ci].lines[_li] : null;

    public void Init(TimeOfDay tod)
    {
        _tod = tod;
        _chapters = ScenarioData.LoadAll();
        if(Args.Has("-portraitpaircheck"))StartCoroutine(CheckPortraitPair());
        if(Args.Has("-portraitassetcheck"))StartCoroutine(PortraitAssets.Audit());
        Linked = !Args.Has("-scenario");
        _white = new Texture2D(1, 1); _white.SetPixel(0, 0, Color.white); _white.Apply();
        _font = GameFonts.Map;
        if(Args.Has("-archive"))OpenArchive();
        if(Args.Has("-archivecheck"))StartCoroutine(CheckArchive());
        if (Args.Has("-scenario") && _chapters.Count > 0)
        {
            int n = Mathf.Clamp(Args.I("-scenario") ?? 1, 1, _chapters.Count);
            _ci = n - 1;
            _li = Mathf.Clamp(Args.I("-scenline") ?? 0, 0, _chapters[_ci].lines.Count - 1);
            _instant = !string.IsNullOrEmpty(Args.Str("-shot"));
            Open();
            if (Args.Has("-scenlog")) ToggleLog();          // 验收钩子：开场即开回顾
            if (Args.Has("-scenauto")) _auto = true;        // 验收钩子：自动开
        }
    }

    void Open()
    {
        if (_chapters == null || _chapters.Count == 0 || Finished) return;
        Active = true; _fade = 0f;
        SeedSlots();
        ResetType();
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    void Close()
    {
        if(Replaying){ReturnFromReplay();return;}
        Active = false;
        if (_tod != null) _tod.ShowToast("剧情已暂停，进度已保留。");
    }

    void ResetType()
    {
        var c = Cur;_textScroll=Vector2.zero;
        if(c!=null&&(_slotsChapter!=_ci||_slotsScene!=c.scene))SeedSlots();
        UpdateSlots();
        _shown = (_instant || c == null || c.type == LineType.Card) ? (c == null ? 0 : c.text.Length) : 0f;
    }

    string ShownText(ScenarioLine c)
    {
        if (_instant || _shown >= c.text.Length) return c.text;
        return c.text.Substring(0, Mathf.Max(0, Mathf.FloorToInt(_shown)));
    }

    void Advance()
    {
        var c = Cur;
        if (c == null) { Close(); return; }
        if (!_instant && c.type != LineType.Card && _shown < c.text.Length) { _shown = c.text.Length; return; }
        int previousChapter=_ci;
        if(Replaying&&_li+1>=_chapters[_ci].lines.Count){ReturnFromReplay();return;}
        _li++;
        if (_li >= _chapters[_ci].lines.Count)
        {
            _ci++; _li = 0;
            if (_ci >= _chapters.Count) { Close(); GetComponent<ProgressManager>()?.SceneCompleted(); if (_tod != null) _tod.ShowToast("第一卷「初遇」播放完毕"); return; }
        }
        ResetType();
        if(Linked && _ci!=previousChapter)
        { PauseStory(); GetComponent<ProgressManager>()?.SceneCompleted(); }
    }

    public bool CheckTravelBoundary()
    {
        bool instant=_instant;_instant=true;
        // Every chapter must play through every scene, then stop at the next chapter.
        for(int chapter=0;chapter<_chapters.Count;chapter++)
        {
            RestoreProgress(chapter,0,true,true);
            int length=_chapters[chapter].lines.Count;
            for(int i=0;i<length-1;i++){Advance();if(!Active||Chapter!=chapter||Line!=i+1)throw new System.Exception("Chapter interrupted at scene "+chapter+" / "+i);}
            Advance();
            if(Active||Chapter!=chapter+1||Line!=0||ProgressManager.TrackingOpen)throw new System.Exception("Chapter boundary/menu failure "+chapter);
        }
        _instant=instant;RestoreProgress(1,0,false,true);
        Debug.Log("[ChapterCheck] PASS all eight chapters continuous across scenes, one node per chapter, no automatic journal");
        return true;
    }

    void SeedSlots()
    {
        _slotL=_slotR=null;_activeSide=0;_slotsChapter=_ci;_slotsScene=Cur?.scene;
        if(Cur==null)return;
        string latest=null,previous=null;
        for(int i=_li;i>=0&&previous==null;i--)
        {
            var line=_chapters[_ci].lines[i];if(line.scene!=Cur.scene)break;
            if(line.type!=LineType.Dialogue||ScenarioAppearance.Resource(line.speaker,line)==null)continue;
            if(latest==null)latest=line.speaker;else if(line.speaker!=latest)previous=line.speaker;
        }
        if(previous!=null){_slotL=previous;_slotR=latest;_activeSide=2;}
        else if(latest!=null){_slotL=latest;_activeSide=1;}
    }
    void UpdateSlots()
    {
        var c=Cur;if(c==null||c.type!=LineType.Dialogue||ScenarioAppearance.Resource(c.speaker,c)==null)return;
        string speaker=c.speaker;
        if(speaker==_slotL){_activeSide=1;return;}
        if(speaker==_slotR){_activeSide=2;return;}
        if(_slotL==null){_slotL=speaker;_activeSide=1;}
        else if(_slotR==null){_slotR=speaker;_activeSide=2;}
        else if(_activeSide==1){_slotR=speaker;_activeSide=2;}
        else{_slotL=speaker;_activeSide=1;}
    }
    System.Collections.IEnumerator CheckPortraitPair()
    {
        yield return null;yield return null;
        int chapter=_ci,line=_li;bool active=Active,linked=Linked,instant=_instant;_instant=true;
        int checkedLines=0;
        for(int ci=0;ci<_chapters.Count;ci++)
        {
            RestoreProgress(ci,0,true,false);
            for(int li=0;li<_chapters[ci].lines.Count;li++)
            {
                var current=Cur;
                if(_slotL!=null&&_slotL==_slotR)throw new System.Exception("Duplicate portrait pair");
                if(current.type==LineType.Dialogue&&ScenarioAppearance.Resource(current.speaker,current)!=null&&current.speaker!=_slotL&&current.speaker!=_slotR)throw new System.Exception("Speaker missing in restored portrait pair");
                checkedLines++;if(li+1<_chapters[ci].lines.Count)Advance();
            }
        }
        RestoreProgress(2,0,true,false);
        if(ScenarioAppearance.Resource("樊楼",Cur)!="Portraits/fanlou-female-cut")throw new System.Exception("Female appearance missing");
        int revert=_chapters[2].lines.FindIndex(l=>l.text.Contains("慢慢地，他的身体又变回了原样"));
        RestoreProgress(2,revert,true,false);
        if(ScenarioAppearance.Resource("樊楼",Cur)!="Portraits/fanlou-cut")throw new System.Exception("Male reversion missing");
        RestoreProgress(6,60,true,false);
        if(_slotL==null||_slotR==null)throw new System.Exception("Five-person scene did not restore two portrait slots");
        if(_chapters[4].lines.Exists(l=>l.protagonistPortraitId=="fanlou-female"))throw new System.Exception("Partial form replaced by full transformation");
        _instant=instant;RestoreProgress(chapter,line,active,linked);
        Debug.Log("[PortraitPairCheck] PASS "+checkedLines+" lines, restored left/right pair, speaker replacement, scene reset, history restore and female/male appearance");
    }

    void Update()
    {
        if(ProgressManager.MenuOpen || ProgressManager.TrackingOpen || ProgressManager.Loading || HelpSidebar.IsOpen)return;
        if(!Active&&!IntroScreen.Live&&!CharacterPicker.IsOpen&&GameInput.Down("Log")){OpenArchive();return;}
        if (Active)
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            float dt = Time.deltaTime;
            _fade = Mathf.Min(1f, _fade + dt * 4f);
            if (CharacterPicker.IsOpen) return;   // C 键人物菜单打开时冻结剧情推进（PlayerRig 负责开关菜单）
            if (GameInput.Cancel()) { if(Replaying)ReturnFromReplay();else if (_logOpen) _logOpen = false; else HelpSidebar.Live.OpenSettings(); return; }
            if(_logOpen&&GUI.GetNameOfFocusedControl()=="StoryFind")return;
            if (GameInput.Down("Log")) { ToggleLog(); return; }
            if (_logOpen) return;
            if (GameInput.Down("Auto")) { _auto = !_auto; _skip = false; _autoTimer = 0; Toast(_auto ? "自动对话 开" : "自动对话 关"); }
            if (GameInput.Down("Skip")) { _skip = !_skip; _auto = false; _skipTimer = 0; Toast(_skip ? "剧情跳过 开" : "剧情跳过 关"); }
            if (_logOpen) return;   // 浏览回顾时冻结推进

            var c = Cur;
            if (c == null) { Close(); return; }
            if (_skip)
            {
                _shown = c.text.Length;
                _skipTimer += dt;
                if (_skipTimer >= 0.06f) { _skipTimer = 0f; Advance(); }
                return;
            }
            if (!_instant && c.type != LineType.Card && _shown < c.text.Length) { _shown += dt * SPEED; _autoTimer = 0f; }
            else if (_auto)
            {
                _autoTimer += dt;
                if (_autoTimer >= 1.0f + c.text.Length * 0.02f) { _autoTimer = 0f; Advance(); }
            }
            else _autoTimer = 0f;

            bool key = GameInput.Down("Advance");
            if (key || ClickInBox()) Advance();
        }
        else if (!IntroScreen.Live && !CharacterPicker.IsOpen && _chapters != null && _chapters.Count > 0
                 && GameInput.Down("Story"))
        {
            var progress=GetComponent<ProgressManager>();
            if(progress!=null && Linked)progress.RequestStory();else Open();
        }
    }

    void Toast(string s) { if (_tod != null) _tod.ShowToast(s); }

    void ToggleLog()
    {
        if(_logOpen){if(Replaying)ReturnFromReplay();else _logOpen=false;}
        else OpenArchive();
    }
    public void OpenArchive(int chapter=-1)
    {
        if(ProgressManager.Loading)return;
        if(!Replaying){_returnChapter=_ci;_returnLine=_li;_returnActive=Active;_returnLinked=Linked;Replaying=true;}
        if(Finished){_ci=0;_li=0;}
        Active=true;_fade=1;_auto=_skip=false;_logOpen=true;_logChapter=chapter<0?_ci:Mathf.Clamp(chapter,0,_chapters.Count-1);
        _find="";_confirmReset=false;_scrollToCur=true;SeedSlots();ResetType();
        ProgressManager.MenuOpen=ProgressManager.TrackingOpen=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    public void ReturnFromReplay()
    {
        if(!Replaying)return;Replaying=false;_logOpen=false;_auto=_skip=false;_confirmReset=false;
        _ci=_returnChapter;_li=_returnLine;Linked=_returnLinked;Active=_returnActive;SeedSlots();ResetType();
    }
    System.Collections.IEnumerator CheckArchive()
    {
        yield return null;yield return null;
        int ch=_ci,li=_li;bool active=Active,linked=Linked;bool instant=_instant;_instant=true;
        for(int c=0;c<_chapters.Count;c++)
        {
            RestoreProgress(ch,li,active,linked);OpenArchive(c);JumpTo(c,0);
            if(!Active||!Replaying||_ci!=c||_li!=0)throw new System.Exception("Chapter replay did not start");
            int last=_chapters[c].lines.Count-1;JumpTo(c,last);Advance();
            if(Replaying||_ci!=ch||_li!=li||Active!=active||Linked!=linked)throw new System.Exception("Replay changed original story progress");
        }
        _instant=instant;Debug.Log("[ArchiveCheck] PASS eight chapter replay/line jumps/end return; original progress and linked state preserved");
        var progress=GetComponent<ProgressManager>();var quests=GetComponent<QuestJournal>();string done=string.Join("|",quests.Done),tracked=quests.Tracked;
        ReturnFromReplay();RestoreProgress(3,20,false,true);progress.ResetStory();yield return null;
        while(ProgressManager.Loading)yield return null;
        if(_ci!=0||_li!=0||!Linked||GetComponent<InteriorManager>().Current?.Id!="home"||string.Join("|",quests.Done)!=done||quests.Tracked!=tracked)throw new System.Exception("Story reset failed or changed side quests");
        Debug.Log("[ArchiveCheck] PASS main-story reset to chapter one/home, side progress preserved");
        if(Args.Has("-archive"))OpenArchive();
    }

    // 只有点对话框本体才推进，避免点到控制按钮/回顾面板时误推进
    bool ClickInBox()
    {
        if (!Input.GetMouseButtonDown(0)) return false;
        float W=1600,H=900;float bw=BoxW(W),bh=BoxH(H),bx=(W-bw)/2,by=H-bh-26;
        return new Rect(bx+35,by+40,bw-70,bh-110).Contains(GameUI.Pointer);

    }

    // 指定对话回退定位：跳到历史任一句重新播放
    void JumpTo(int ci, int li)
    {
        _ci = Mathf.Clamp(ci, 0, _chapters.Count - 1);
        _li = Mathf.Clamp(li, 0, _chapters[_ci].lines.Count - 1);
        _logOpen = false; _autoTimer = 0f; _skipTimer = 0f;
        SeedSlots();
        ResetType();
    }

    // ── UI ───────────────────────────────────────────────
    GUIStyle Mk(int size, TextAnchor anchor, Color c, bool wrap = false, bool italic = false, bool bold = false)
    {
        var s = new GUIStyle(GUI.skin.label)
        {
            fontSize = size, alignment = anchor, wordWrap = wrap,
            fontStyle = italic ? FontStyle.Italic : (bold ? FontStyle.Bold : FontStyle.Normal),
        };
        s.font = GameFonts.Story;
        s.normal.textColor = c;
        return s;
    }

    void EnsureStyles()
    {
        if (_body != null && _font==GameFonts.Story) return;
        _font=GameFonts.Story;
        _name = Mk(22, TextAnchor.MiddleCenter, new Color(0.97f, 0.97f, 0.99f), false, false, true);
        _body = Mk(24, TextAnchor.UpperLeft, new Color(0.96f, 0.96f, 0.98f), true);
        _narr = Mk(23, TextAnchor.UpperLeft, new Color(0.90f, 0.88f, 0.82f), true);
        _thought = Mk(23, TextAnchor.UpperLeft, new Color(0.74f, 0.85f, 0.99f), true, true);
        _dir = Mk(22, TextAnchor.MiddleCenter, new Color(0.82f, 0.84f, 0.88f, 0.95f), true, true);
        _card = Mk(46, TextAnchor.MiddleCenter, new Color(0.97f, 0.97f, 0.99f), false, false, true);
        _scene = Mk(17, TextAnchor.UpperLeft, new Color(0.92f, 0.94f, 0.97f));
        _cue = Mk(13, TextAnchor.UpperLeft, new Color(0.72f, 0.76f, 0.82f, 0.9f), true);
        _meta = Mk(14, TextAnchor.UpperRight, new Color(0.72f, 0.77f, 0.83f, 0.85f));
        _hint = Mk(13, TextAnchor.LowerCenter, new Color(0.70f, 0.75f, 0.80f, 0.75f));
        _btn = Mk(15, TextAnchor.MiddleCenter, new Color(0.92f, 0.94f, 0.97f), false, false, true);
        _logEntry = Mk(18, TextAnchor.UpperLeft, new Color(0.90f, 0.92f, 0.95f), true);
        _logHead = Mk(16, TextAnchor.UpperLeft, new Color(0.37f, 0.88f, 0.78f), false, false, true);
        _logCur = new GUIStyle(_logEntry) { fontStyle = FontStyle.Bold };
        _logCur.normal.textColor = new Color(0.45f, 0.95f, 0.85f);
        _logFut = Mk(18, TextAnchor.UpperLeft, new Color(0.76f, 0.80f, 0.85f), true);
        _logHeadFut = new GUIStyle(_logHead);
        _logHeadFut.normal.textColor = new Color(0.52f, 0.62f, 0.72f);
        _toc = Mk(14, TextAnchor.UpperLeft, new Color(0.80f, 0.84f, 0.90f), true);
        _toc.padding = new RectOffset(8, 8, 6, 6);
        _tocOn = new GUIStyle(_toc) { fontStyle = FontStyle.Bold };
        _tocOn.normal.textColor = new Color(0.45f, 0.95f, 0.85f);
    }

    void Fill(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, _white); }

    void Border(Rect r, float t, Color c)
    {
        Fill(new Rect(r.x, r.y, r.width, t), c);
        Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
        Fill(new Rect(r.x, r.y, t, r.height), c);
        Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
    }

    void OnGUI()
    {
        var cur = Cur;
        if (!Active || cur == null) return;
        if (CharacterPicker.IsOpen||HelpSidebar.IsOpen) return;   // 人物菜单打开时让位，避免压住选择卡片
        EnsureStyles();
        GameUI.Begin(-100);
        float W = 1600, H = 900, a = _fade;
        var accent = GameUI.Gold;

        Fill(new Rect(0, 0, W, H), new Color(0.02f, 0.03f, 0.05f, 0.32f * a));   // 压暗世界
        GUI.color = Color.white;

        DrawSceneTag(cur, W, H, a);
        switch (cur.type)
        {
            case LineType.Dialogue:
            case LineType.Narration:
            case LineType.Thought:
                DrawPortraits(cur, W, H, a);
                DrawBox(cur, W, H, a, accent);   // 立绘后画＝压在对话框上面
                break;
            case LineType.Direction:
                GUI.color = new Color(1, 1, 1, a);
                GUI.Label(new Rect(W * 0.15f, H * 0.34f, W * 0.7f, H * 0.32f), ShownText(cur), _dir);
                break;
            case LineType.Card:
                GUI.color = new Color(1, 1, 1, a);
                GUI.Label(new Rect(0, H * 0.40f, W, 90), cur.text, _card);
                break;
        }
        GUI.color = Color.white;
        DrawControls(W, H, a, accent);
        if (_logOpen) DrawLog(W, H, a);
        GameUI.End();
    }

    // 崩铁式控制条：回顾 / 自动 / 跳过（也可用 L / A / S）
    void DrawControls(float W,float H,float a,Color accent)
    {
        float y=H-70,x=W-866;
        ControlBtn(new Rect(x,y,110,35),"回顾",_logOpen,a,accent);x+=119;
        ControlBtn(new Rect(x,y,110,35),_auto?"自动中":"自动阅读",_auto,a,accent);x+=119;
        ControlBtn(new Rect(x,y,110,35),_skip?"快进中":"快进",_skip,a,accent);x+=119;
        ControlBtn(new Rect(x,y,110,35),Replaying?"返回主线":"暂离剧情",false,a,accent);x+=119;
        ControlBtn(new Rect(x,y,110,35),"设置",false,a,accent);x+=119;
        ControlBtn(new Rect(x,y,110,35),"继续  ›",true,a,accent);
    }
    void ControlBtn(Rect r,string label,bool on,float a,Color accent)
    {
        if(!GameUI.Button(r,label,on,label.StartsWith("继续")))return;
        if(label.StartsWith("回顾"))ToggleLog();
        else if(label.StartsWith("自动")){_auto=!_auto;_skip=false;_autoTimer=0;}
        else if(label.StartsWith("快进")){_skip=!_skip;_auto=false;_skipTimer=0;}
        else if(label.StartsWith("暂离")||label.StartsWith("返回主线"))Close();
        else if(label=="设置")HelpSidebar.Live.OpenSettings();
        else Advance();
    }

    // 剧情浏览：左栏章节目录（点章过滤右栏），右栏列该章全部句（含后续未播），点任一句向前/向后定位播放
    void DrawLog(float W, float H, float a)
    {
        Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.72f * a));
        float pw = Mathf.Min(1060, W * 0.82f), ph = H * 0.76f, px = (W - pw) / 2f, py = (H - ph) / 2f;
        Fill(new Rect(px, py, pw, ph), new Color(0.06f, 0.08f, 0.12f, 0.97f * a));
        Border(new Rect(px, py, pw, ph), 2, new Color(0.37f, 0.88f, 0.78f, 0.6f * a));
        GUI.color = new Color(0.95f, 0.97f, 0.99f, a);
        GUI.Label(new Rect(px + 24, py + 16, pw - 48, 30), "故事回顾 · 选择章节，重读某一个瞬间", _btn);
        GUI.color = Color.white;

        if(GameUI.Button(new Rect(px+pw-132,py+12,110,36),"返回进度")){ReturnFromReplay();return;}
        int chapter=Mathf.Clamp(_logChapter<0?_ci:_logChapter,0,_chapters.Count-1);
        GUI.Label(new Rect(px+24,py+55,92,30),"台词/行号",GameUI.Text(15,GameUI.Muted));
        GameUI.Fill(new Rect(px+115,py+55,295,30),new Color(.13f,.18f,.23f));
        GUI.SetNextControlName("StoryFind");
        _find=GUI.TextField(new Rect(px+115,py+55,295,30),_find,GameUI.Text(16,GameUI.Paper));
        if(GameUI.Button(new Rect(px+425,py+53,135,34),"从本章开头"))JumpTo(chapter,0);
        if(GameUI.Button(new Rect(px+578,py+53,125,34),"定位当前句")){_logChapter=_ci;_find="";_scrollToCur=true;}
        if(GameUI.Button(new Rect(px+720,py+53,155,34),_confirmReset?"确认重置主线":"重置主线"))
        {if(_confirmReset){ReturnFromReplay();GetComponent<ProgressManager>().ResetStory();return;}else _confirmReset=true;}
        if(_confirmReset){GUI.Label(new Rect(px+24,py+ph-25,800,23),"再次点击确认：主线从第一章开始；保留手动存档与支线。",GameUI.Text(14,GameUI.Gold));}
        float tocW = 200f;
        GUILayout.BeginArea(new Rect(px + 16, py + 100, tocW, ph - 135));
        _tocScroll = GUILayout.BeginScrollView(_tocScroll, false, false);
        if (GUILayout.Button("全部章节", _logChapter == -1 ? _tocOn : _toc)) { _logChapter = -1; _logScroll = Vector2.zero; }
        for (int ci = 0; ci < _chapters.Count; ci++)
        {
            string mark = ci == _ci ? "▶ " : "";
            if (GUILayout.Button(mark + _chapters[ci].title, _logChapter == ci ? _tocOn : _toc))
            {
                _logChapter = ci;
                _logScroll = Vector2.zero;
                if (ci == _ci) _scrollToCur = true;
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        Fill(new Rect(px + 16 + tocW + 6, py + 100, 1.5f, ph - 135), new Color(0.35f, 0.40f, 0.48f, 0.5f * a));
        GUI.color=Color.white;

        GUILayout.BeginArea(new Rect(px + 16 + tocW + 14, py + 100, pw - 32 - tocW - 14, ph - 135));
        _logScroll = GUILayout.BeginScrollView(_logScroll, false, true, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        int c0 = _logChapter == -1 ? 0 : _logChapter;
        int c1 = _logChapter == -1 ? _chapters.Count - 1 : _logChapter;
        for (int ci = c0; ci <= c1; ci++)
        {
            bool futureChap = ci > _ci;
            GUILayout.Label(_chapters[ci].title,
                futureChap ? _logHeadFut : _logHead);
            var lines = _chapters[ci].lines;
            for (int li = 0; li < lines.Count; li++)
            {
                var l = lines[li];
                if (l.type == LineType.Card) continue;
                if(!string.IsNullOrEmpty(_find))
                {bool match=int.TryParse(_find,out var number)?number==li+1:(l.text+(l.speaker??"")).Contains(_find);if(!match)continue;}
                string prefix = l.type == LineType.Dialogue && !string.IsNullOrEmpty(l.speaker) ? l.speaker + "：" : "";
                bool isCur = ci == _ci && li == _li;
                bool isFut = ci > _ci || (ci == _ci && li > _li);
                var st = isCur ? _logCur : isFut ? _logFut : _logEntry;
                bool clicked = GUILayout.Button((li+1).ToString("000")+"  "+prefix + l.text, st);
                if (isCur && _scrollToCur && Event.current.type==EventType.Repaint)
                {
                    var r = GUILayoutUtility.GetLastRect();
                    _logScroll.y = Mathf.Max(0f, r.y - 40f);
                    _scrollToCur = false;
                }
                if (clicked) JumpTo(ci, li);
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void DrawSceneTag(ScenarioLine cur,float W,float H,float a)
    {
        GUI.color=Color.white;
        GameUI.Fill(new Rect(46,20,720,128),new Color(.035f,.055f,.08f,.76f));
        GUI.Label(new Rect(66,34,1000,24),"VOL. 01  /  初遇",GameUI.Text(14,GameUI.Gold));
        GUI.Label(new Rect(66,70,1120,45),cur.scene,GameUI.Text(27,GameUI.Paper,false,true));
        if(!string.IsNullOrEmpty(cur.location))GUI.Label(new Rect(68,118,1060,24),cur.location.Split('｜')[0],GameUI.Text(15,GameUI.Muted));
        GameUI.Rule(68,155,48,GameUI.Gold);
    }

    Texture2D LoadPortrait(string res)
    {
        if (_portraits.TryGetValue(res, out var t)) return t;
        t = PortraitAssets.LoadResource(res);
        if (t != null) { t.filterMode = FilterMode.Bilinear; t.wrapMode = TextureWrapMode.Clamp; }
        _portraits[res] = t;
        return t;
    }

    // 环境色：昼夜/晨昏/室内给立绘乘一层光色，使其融入当前场景而非贴上去的亮片
    Color EnvTint()
    {
        if (_tod == null) return Color.white;
        float h = _tod.Hour;
        float m = TimeOfDay.NightMixAt(h), warm = TimeOfDay.WarmAt(h);
        Color c = Color.Lerp(new Color(1f, 0.98f, 0.95f), new Color(0.78f, 0.83f, 0.98f), m);
        c = Color.Lerp(c, new Color(1f, 0.85f, 0.70f), warm * 0.5f);
        float b = Mathf.Lerp(1f, 0.90f, m);   // 夜晚不再压太暗，整体提亮
        if (_tod.Indoor) { c = new Color(0.99f, 0.97f, 0.93f); b = 1f; }
        return new Color(c.r * b, c.g * b, c.b * b, 1f);
    }

    float BoxH(float H)
    {
        float scale=GameInput.TextScale,baseline=Mathf.Max(250f,H*.28f)*Mathf.Max(1,scale);
        if(_body==null||Cur==null)return baseline;
        if(_heightLine!=Cur||_heightScale!=scale)
        {var measure=new GUIStyle(_body){fontSize=Mathf.RoundToInt(26*scale)};
         _heightLine=Cur;_heightScale=scale;_heightCache=measure.CalcHeight(new GUIContent(Cur.text),BoxW(1600)-134)+130;}
        return Mathf.Clamp(Mathf.Max(baseline,_heightCache),baseline,H*.46f);
    }
    static float BoxW(float W) => W * 0.84f;   // 收窄框，给两侧 2.5× 立绘留出「不贴框」的边距

    void DrawPortraits(ScenarioLine cur,float W,float H,float a)
    {
        if(_slotL==null&&_slotR==null)return;
        if(cur.type==LineType.Dialogue&&ScenarioAppearance.Resource(cur.speaker,cur)==null)return;
        bool dialogue=cur.type==LineType.Dialogue;Color tint=EnvTint();
        DrawOne(_slotL,true,dialogue&&_activeSide==1,cur,tint,W,H,a);
        DrawOne(_slotR,false,dialogue&&_activeSide==2,cur,tint,W,H,a); // Draw each alpha sprite once, avoiding an opaque fringe.
    }
    void DrawOne(string speaker,bool left,bool active,ScenarioLine current,Color tint,float W,float H,float a)
    {
        var resource=ScenarioAppearance.Resource(speaker,current);if(resource==null)return;
        var texture=LoadPortrait(resource);if(texture==null)return;
        float height=H*.68f,width=height*texture.width/texture.height;
        float x=left?W*.06f:W-width-W*.06f;
        float k=active?1f:.62f;
        GUI.color=new Color(tint.r*k,tint.g*k,tint.b*k,a*(active?1f:.9f));
        GUI.DrawTexture(new Rect(x,H*.84f-height,width,height),texture,ScaleMode.ScaleToFit);
        GUI.color=Color.white;
    }

    void DrawBox(ScenarioLine cur,float W,float H,float a,Color accent)
    {
        float bw=BoxW(W),bh=BoxH(H),bx=(W-bw)/2,by=H-bh-26;
        GameUI.Fill(new Rect(bx+7,by+7,bw,bh),new Color(0,0,0,.3f));
        GameUI.Fill(new Rect(bx,by,bw,bh),GameUI.Paper);
        GameUI.Fill(new Rect(bx,by,4,bh),GameUI.Gold);
        string speaker=cur.type==LineType.Dialogue?cur.speaker:cur.type==LineType.Thought?"心声":"旁白";
        GameUI.Fill(new Rect(bx+40,by-24,220,45),GameUI.Ink);
        GUI.Label(new Rect(bx+55,by-20,190,36),speaker??"",GameUI.Text(24,GameUI.Gold,false,true,TextAnchor.MiddleCenter));
        GUI.Label(new Rect(bx+282,by+16,bw-340,27),cur.type==LineType.Thought?"INNER VOICE":cur.type==LineType.Narration?"NARRATION":"DIALOGUE",GameUI.Text(11,new Color(.43f,.46f,.46f)));
        var style=cur.type==LineType.Thought?_thought:cur.type==LineType.Narration?_narr:_body;
        style.fontSize=Mathf.RoundToInt(26*GameInput.TextScale);style.normal.textColor=GameUI.Ink;style.fontStyle=FontStyle.Normal;
        float available=bh-116,width=bw-112,height=Mathf.Max(available,style.CalcHeight(new GUIContent(ShownText(cur)),width-22));
        _textScroll=GUI.BeginScrollView(new Rect(bx+51,by+61,width,available),_textScroll,new Rect(0,0,width-20,height),false,height>available);
        GUI.Label(new Rect(0,0,width-22,height),ShownText(cur),style);GUI.EndScrollView();
        GameUI.Rule(bx+40,H-80,bw-80,new Color(.78f,.77f,.72f));
        GUI.Label(new Rect(bx+42,H-58,420,22),"STONES FROM ANOTHER HILL",GameUI.Text(10,new Color(.47f,.47f,.43f)));
    }
}
