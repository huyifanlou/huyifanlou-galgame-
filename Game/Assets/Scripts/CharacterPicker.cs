using System.Collections.Generic;
using UnityEngine;

// C 键人物图鉴：六人立绘卡片，点击或按数字键查看完整立绘。
// 取代旧的 C 键盲循环；当前角色高亮，开关有淡入。开场期间不响应；对话期间也可开（由 DialogueManager 冻结推进）。
public class CharacterPicker : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    public static CharacterPicker Live { get; private set; }   // 对话/世界任意时刻由 PlayerRig 统一取用

    static readonly string[] Desc =
    {
        "男主 · 金发蓝瞳 · 灰衬衫外套\n可查看觉醒后的女体形态",
        "女主 · 白发红瞳 · 礼服血族",
        "挚友 · 黑发棕瞳 · 立领夹克",
        "精灵女仆 · 绿发蓝瞳 · 尖耳",
        "冷感少女 · 黑长直紫瞳 · 校服",
        "暗部队长 · 灰发大檐帽 · 军装",
    };

    PlayerAvatar _avatar;
    TimeOfDay _tod;
    Font _font;
    Texture2D _white;
    readonly Dictionary<string, Texture2D> _thumbs = new Dictionary<string, Texture2D>();
    GUIStyle _title, _sub, _cardName, _cardDesc, _badge, _foot, _num;
    float _anim;
    int _hover = -1;
    int selected;bool preview,femaleForm;
    CharacterDef SelectedDefinition=>selected==0&&femaleForm?CharacterDef.FanLouFemale:PlayerAvatar.Roster[selected];

    void Awake() { Live = this; }

    public void Init(PlayerAvatar avatar, TimeOfDay tod)
    {
        _avatar = avatar; _tod = tod;
        _white = new Texture2D(1, 1); _white.SetPixel(0, 0, Color.white); _white.Apply();
        _font = GameFonts.Map;
        if (Args.Has("-picker")) Open();   // 验收钩子：启动即开菜单截图
    }

    System.Collections.IEnumerator Start()
    {
        yield return null;
        var index=Args.I("-gallerycharacter");
        if(index.HasValue){Open();Select(Mathf.Clamp(index.Value-1,0,5));femaleForm=selected==0&&Args.Str("-galleryform")=="female";if(Args.Has("-gallerydisplay"))DisplaySelected();}
    }
    public bool DisplaySelected()
    {if(StandeeDisplay.Live!=null&&StandeeDisplay.Live.Show(SelectedDefinition)){Close();return true;}return false;}
    public void Toggle() { if (IsOpen) Close(); else Open(); }

    void Open()
    {
        if (_avatar == null || IntroScreen.Live) return;
        IsOpen = true; _anim = 0f;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    void Close() { IsOpen = false; }

    void Select(int i)
    {
        if (_avatar == null || i < 0 || i >= PlayerAvatar.Roster.Length) return;
        selected=i;preview=true;
    }

    void Update()
    {
        if (!IsOpen||HelpSidebar.IsOpen) return;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        _anim = Mathf.Min(1f, _anim + Time.deltaTime * 5f);
        if (GameInput.Cancel()) { if(preview)preview=false;else Close(); return; }
        // C 的开/关由 PlayerRig 统一处理（Toggle），这里不再响应，避免同帧双切换
        for (int i = 0; i < 6; i++)
            if (GameInput.Down("Character"+(i+1))) { Select(i); return; }
    }

    GUIStyle Mk(int size, TextAnchor anchor, Color c, bool wrap = false, bool bold = false)
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = anchor, wordWrap = wrap, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal };
        if (_font != null) s.font = _font;
        s.normal.textColor = c;
        return s;
    }

    void EnsureStyles()
    {
        if (_title != null) return;
        _title = Mk(30, TextAnchor.MiddleCenter, new Color(0.97f, 0.97f, 0.99f), false, true);
        _sub = Mk(15, TextAnchor.MiddleCenter, new Color(0.72f, 0.77f, 0.83f, 0.9f));
        _cardName = Mk(24, TextAnchor.UpperLeft, new Color(0.97f, 0.97f, 0.99f), false, true);
        _cardDesc = Mk(13, TextAnchor.UpperLeft, new Color(0.74f, 0.79f, 0.85f), true);
        _badge = Mk(13, TextAnchor.MiddleCenter, new Color(0.05f, 0.07f, 0.11f), false, true);
        _num = Mk(15, TextAnchor.MiddleCenter, new Color(0.95f, 0.96f, 0.98f), false, true);
        _foot = Mk(14, TextAnchor.MiddleCenter, new Color(0.70f, 0.75f, 0.80f, 0.85f));
    }

    void Fill(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, _white); }
    void Border(Rect r, float t, Color c)
    {
        Fill(new Rect(r.x, r.y, r.width, t), c); Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
        Fill(new Rect(r.x, r.y, t, r.height), c); Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
    }

    Texture2D Thumb(string id)
    {
        var res = "Portraits/" + (string.IsNullOrEmpty(id) ? "fanlou" : id) + "-cut";
        if (_thumbs.TryGetValue(res, out var t)) return t;
        t = PortraitAssets.LoadResource(res);
        if (t != null) { t.filterMode = FilterMode.Bilinear; t.wrapMode = TextureWrapMode.Clamp; }
        _thumbs[res] = t;
        return t;
    }

    void OnGUI()
    {
        if (!IsOpen||HelpSidebar.IsOpen) return;
        EnsureStyles();
        if(preview)
        {
            GameUI.Begin(-400);GameUI.Panel(new Rect(280,35,1040,830));var tex=Thumb(SelectedDefinition.id);
            if(tex!=null)GUI.DrawTexture(new Rect(320,65,610,740),tex,ScaleMode.ScaleToFit);
            GUI.Label(new Rect(950,165,320,60),SelectedDefinition.label,GameUI.Text(36,GameUI.Paper,true,true));
            GUI.Label(new Rect(950,245,310,110),femaleForm&&selected==0?"樊楼的觉醒形态\n金发蓝瞳 · 粉白魔法少女\n星心饰扣 · 元气招呼":Desc[selected],GameUI.Text(22,GameUI.Muted,true));
            if(selected==0)
            {
                if(GameUI.Button(new Rect(950,370,145,48),"男体",!femaleForm))femaleForm=false;
                if(GameUI.Button(new Rect(1110,370,145,48),"女体",femaleForm))femaleForm=true;
            }
            var display=StandeeDisplay.Live;
            if(display!=null)
            {
                GUI.Label(new Rect(950,455,310,65),DialogueManager.Active?"暂离剧情后，可将立牌放到面前观看。":"把立牌放到面前，可以走近观看。",GameUI.Text(17,GameUI.Muted,true));
                bool previousEnabled=GUI.enabled;GUI.enabled=!DialogueManager.Active;
                if(GameUI.Button(new Rect(980,545,260,52),display.CurrentId==SelectedDefinition.id?"移到面前":"展示立牌",false,true))
                {DisplaySelected();}
                GUI.enabled=previousEnabled;
                if(display.IsShowing&&GameUI.Button(new Rect(980,610,260,52),"收起展示立牌")){display.Clear();Close();}
            }
            if(GameUI.Button(new Rect(980,735,260,52),"返回人物图鉴"))preview=false;GameUI.End();return;
        }
        float W = Screen.width, H = Screen.height, a = _anim;
        var accent = new Color(0.37f, 0.88f, 0.78f);
        float s = Mathf.Min(W / 1600f, H / 900f);
        float pw = 1060 * s, ph = 600 * s;
        float px = (W - pw) / 2f, py = (H - ph) / 2f + (1f - a) * 24f;

        Fill(new Rect(0, 0, W, H), new Color(0.02f, 0.03f, 0.05f, 0.66f * a));
        Fill(new Rect(px + 8, py + 10, pw, ph), new Color(0, 0, 0, 0.4f * a));
        Fill(new Rect(px, py, pw, ph), new Color(0.07f, 0.09f, 0.14f, 0.97f * a));
        Border(new Rect(px, py, pw, ph), 2, new Color(accent.r, accent.g, accent.b, 0.6f * a));
        Fill(new Rect(px, py, pw, 3), new Color(accent.r, accent.g, accent.b, a));

        GUI.color = new Color(1, 1, 1, a);
        GUI.Label(new Rect(px, py + 22 * s, pw, 40 * s), "人物图鉴", _title);
        GUI.color = new Color(0.72f, 0.77f, 0.83f, 0.9f * a);
        GUI.Label(new Rect(px, py + 60 * s, pw, 22 * s), "查看人物立绘与简介", _sub);
        GUI.color = Color.white;

        float margin = 26 * s, gap = 18 * s, top = 100 * s, bottom = 46 * s;
        float cw = (pw - 2 * margin - 2 * gap) / 3f;
        float ch = (ph - top - bottom - 2 * margin - gap) / 2f;
        _hover = -1;
        var mouse = Event.current.mousePosition;
        for (int i = 0; i < 6; i++)
        {
            int col = i % 3, row = i / 3;
            var rect = new Rect(px + margin + col * (cw + gap), py + top + margin + row * (ch + gap), cw, ch);
            bool hover = rect.Contains(mouse);
            if (hover) _hover = i;
            bool current = selected==i;

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) Select(i);

            Fill(rect, hover ? new Color(0.13f, 0.17f, 0.24f, a) : new Color(0.10f, 0.12f, 0.18f, a));
            Border(rect, current ? 3 : (hover ? 2 : 1),
                current ? new Color(accent.r, accent.g, accent.b, a)
                        : new Color(0.30f, 0.35f, 0.43f, (hover ? 0.9f : 0.5f) * a));

            // 立绘缩略图（深底卡：立绘已抠透明，白底会显得没剪干净）
            float pad = 12 * s, th = ch - 2 * pad, tw = th * 0.6667f;
            var tr = new Rect(rect.x + pad, rect.y + pad, tw, th);
            Fill(tr, new Color(0.10f, 0.13f, 0.19f, a));
            var tex = Thumb(PlayerAvatar.Roster[i].id);
            GUI.color = new Color(1, 1, 1, a);
            if (tex != null) GUI.DrawTexture(tr, tex, ScaleMode.ScaleToFit);
            Border(tr, 1, new Color(0.37f, 0.88f, 0.78f, 0.35f * a));

            // 序号徽标
            var nr = new Rect(rect.x + pad, rect.y + pad, 26 * s, 26 * s);
            Fill(nr, new Color(0.05f, 0.07f, 0.11f, 0.85f * a));
            GUI.color = new Color(0.95f, 0.96f, 0.98f, a);
            GUI.Label(nr, (i + 1).ToString(), _num);

            // 名字 + 简介
            float tx = tr.xMax + 14 * s, twd = rect.xMax - pad - tx;
            GUI.color = new Color(0.97f, 0.97f, 0.99f, a);
            GUI.Label(new Rect(tx, rect.y + pad + 4 * s, twd, 30 * s), PlayerAvatar.Roster[i].label, _cardName);
            GUI.color = new Color(0.74f, 0.79f, 0.85f, a);
            GUI.Label(new Rect(tx, rect.y + pad + 38 * s, twd, ch - 2 * pad - 60 * s), Desc[i], _cardDesc);

            if (current)
            {
                var br = new Rect(tx, rect.yMax - pad - 24 * s, 64 * s, 24 * s);
                Fill(br, new Color(accent.r, accent.g, accent.b, a));
                GUI.color = new Color(0.05f, 0.07f, 0.11f, a);
                GUI.Label(br, "查看", _badge);
            }
            GUI.color = Color.white;
        }

        GUI.color = new Color(0.70f, 0.75f, 0.80f, 0.85f * a);
        GUI.Label(new Rect(px, py + ph - 38 * s, pw, 24 * s), "点击查看完整立绘", _foot);
        GUI.color = Color.white;
    }
}
