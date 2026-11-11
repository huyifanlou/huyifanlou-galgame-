using System.Collections.Generic;
using UnityEngine;

// 对话运行时（IMGUI，对齐 Hud/IntroScreen 的纯代码 UI 路线）。
// 入口：命令行 -scenario <章号> [-scenline <句序>]；世界里按 B 开/继续。
// 播放：台词带立绘卡+名字条；旁白/内心进对话框；演出居中斜体；结束卡大字。
// 点击/空格/回车推进（打字机未尽先补全），Esc 退出回世界。对话期间 PlayerRig 锁移动。
public class DialogueManager : MonoBehaviour
{
    public static bool Active { get; private set; }

    const float SPEED = 48f;   // 打字机 字/秒
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
    string _slotL, _slotR;   // 一左一右两个对话人物
    int _activeSide;         // 0=都暗 1=左亮 2=右亮
    bool _auto, _skip, _logOpen;
    float _autoTimer, _skipTimer;
    Vector2 _logScroll;
    Vector2 _tocScroll;
    int _logChapter = -1;   // 剧情浏览的章节目录过滤：-1=全部章节
    bool _scrollToCur;   // 打开浏览后把当前句滚进视野（列表含后续句，不能滚到最底）
    GUIStyle _btn, _logEntry, _logHead, _logCur, _logFut, _logHeadFut, _toc, _tocOn;

    ScenarioLine Cur => (_chapters != null && _ci >= 0 && _ci < _chapters.Count
                         && _li >= 0 && _li < _chapters[_ci].lines.Count) ? _chapters[_ci].lines[_li] : null;

    public void Init(TimeOfDay tod)
    {
        _tod = tod;
        _chapters = ScenarioData.LoadAll();
        _white = new Texture2D(1, 1); _white.SetPixel(0, 0, Color.white); _white.Apply();
        _font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "微软雅黑", "SimHei", "Arial" }, 24);
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
        if (_chapters == null || _chapters.Count == 0) return;
        Active = true; _fade = 0f;
        SeedSlots();
        ResetType();
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    void Close()
    {
        Active = false;
        if (_tod != null) _tod.ShowToast("剧情已暂停（B 继续）");
    }

    void ResetType()
    {
        var c = Cur;
        _shown = (_instant || c == null || c.type == LineType.Card) ? (c == null ? 0 : c.text.Length) : 0f;
        UpdateSlots();
    }

    // 跳转/开场时往回找最近两个不同说话人，保证一开就是「一左一右」而非单人
    void SeedSlots()
    {
        _slotL = _slotR = null; _activeSide = 0;
        string cur = null, prev = null;
        for (int ci = _ci; ci >= 0 && prev == null; ci--)
        {
            var lines = _chapters[ci].lines;
            for (int li = (ci == _ci ? _li : lines.Count - 1); li >= 0; li--)
            {
                var l = lines[li];
                if (l.type != LineType.Dialogue || ScenarioData.PortraitResource(l.speaker, true) == null) continue;
                if (cur == null) cur = l.speaker;
                else if (l.speaker != cur) { prev = l.speaker; break; }
            }
        }
        if (cur == null) return;
        if (prev != null) { _slotL = prev; _slotR = cur; _activeSide = 2; }
        else { _slotL = cur; _activeSide = 1; }
    }

    // 稳定左右位：新说话人顶替「当前暗的那一侧」，原说话人留下作暗色搭档
    void UpdateSlots()
    {
        var c = Cur;
        if (c == null || c.type != LineType.Dialogue || ScenarioData.PortraitResource(c.speaker, true) == null) return;
        string s = c.speaker;
        if (s == _slotL) { _activeSide = 1; return; }
        if (s == _slotR) { _activeSide = 2; return; }
        if (_slotL == null) { _slotL = s; _activeSide = 1; }
        else if (_slotR == null) { _slotR = s; _activeSide = 2; }
        else if (_activeSide == 1) { _slotR = s; _activeSide = 2; }
        else { _slotL = s; _activeSide = 1; }
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
        _li++;
        if (_li >= _chapters[_ci].lines.Count)
        {
            _ci++; _li = 0;
            if (_ci >= _chapters.Count) { Close(); if (_tod != null) _tod.ShowToast("第一卷「初遇」播放完毕"); return; }
        }
        ResetType();
    }

    void Update()
    {
        if (Active)
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            float dt = Time.deltaTime;
            _fade = Mathf.Min(1f, _fade + dt * 4f);
            if (CharacterPicker.IsOpen) return;   // C 键人物菜单打开时冻结剧情推进（PlayerRig 负责开关菜单）
            if (Input.GetKeyDown(KeyCode.Escape)) { if (_logOpen) _logOpen = false; else Close(); return; }
            if (Input.GetKeyDown(KeyCode.L)) { ToggleLog(); return; }
            if (Input.GetKeyDown(KeyCode.A)) { _auto = !_auto; _skip = false; _autoTimer = 0; Toast(_auto ? "自动对话 开" : "自动对话 关"); }
            if (Input.GetKeyDown(KeyCode.S)) { _skip = !_skip; _auto = false; _skipTimer = 0; Toast(_skip ? "剧情跳过 开" : "剧情跳过 关"); }
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

            bool key = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                       || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.DownArrow);
            if (key || ClickInBox()) Advance();
        }
        else if (!IntroScreen.Live && !CharacterPicker.IsOpen && _chapters != null && _chapters.Count > 0
                 && Input.GetKeyDown(KeyCode.B))
        {
            Open();
        }
    }

    void Toast(string s) { if (_tod != null) _tod.ShowToast(s); }

    void ToggleLog()
    {
        _logOpen = !_logOpen;
        if (_logOpen) { _scrollToCur = true; _logChapter = _ci; }   // 打开时定位到当前章当前句
    }

    // 只有点对话框本体才推进，避免点到控制按钮/回顾面板时误推进
    bool ClickInBox()
    {
        if (!Input.GetMouseButtonDown(0)) return false;
        float W = Screen.width, H = Screen.height;
        float bw = BoxW(W), bh = BoxH(H), bx = (W - bw) / 2f, by = H - bh - H * 0.035f;
        var m = new Vector2(Input.mousePosition.x, H - Input.mousePosition.y);
        return new Rect(bx, by, bw, bh).Contains(m);
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
        if (_font != null) s.font = _font;
        s.normal.textColor = c;
        return s;
    }

    void EnsureStyles()
    {
        if (_body != null) return;
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
        _logFut = Mk(18, TextAnchor.UpperLeft, new Color(0.52f, 0.56f, 0.64f), true);
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
        if (CharacterPicker.IsOpen) return;   // 人物菜单打开时让位，避免压住选择卡片
        EnsureStyles();
        float W = Screen.width, H = Screen.height, a = _fade;
        var accent = new Color(0.37f, 0.88f, 0.78f);

        Fill(new Rect(0, 0, W, H), new Color(0.02f, 0.03f, 0.05f, 0.62f * a));   // 压暗世界
        GUI.color = Color.white;

        DrawSceneTag(cur, W, H, a);
        GUI.color = new Color(0.72f, 0.77f, 0.83f, 0.85f * a);
        GUI.Label(new Rect(W - 320, H * 0.03f, 300, 24),
            $"第 {_ci + 1}/{_chapters.Count} 章 · 句 {_li + 1}/{_chapters[_ci].lines.Count}", _meta);
        GUI.color = Color.white;

        switch (cur.type)
        {
            case LineType.Dialogue:
            case LineType.Narration:
            case LineType.Thought:
                DrawBox(cur, W, H, a, accent);
                DrawPortraits(cur, W, H, a);   // 立绘后画＝压在对话框上面
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
    }

    // 崩铁式控制条：回顾 / 自动 / 跳过（也可用 L / A / S）
    void DrawControls(float W, float H, float a, Color accent)
    {
        float bw = 96, bh = 32, gap = 8, y = H * 0.03f + 30;
        float x = W - 20 - bw * 3 - gap * 2;
        ControlBtn(new Rect(x, y, bw, bh), "回顾 L", _logOpen, a, accent); x += bw + gap;
        ControlBtn(new Rect(x, y, bw, bh), _auto ? "自动 A · 开" : "自动 A", _auto, a, accent); x += bw + gap;
        ControlBtn(new Rect(x, y, bw, bh), _skip ? "跳过 S · 开" : "跳过 S", _skip, a, accent);
    }

    void ControlBtn(Rect r, string label, bool on, float a, Color accent)
    {
        if (GUI.Button(r, GUIContent.none, GUIStyle.none))
        {
            if (label.StartsWith("回顾")) ToggleLog();
            else if (label.StartsWith("自动")) { _auto = !_auto; _skip = false; _autoTimer = 0; Toast(_auto ? "自动对话 开" : "自动对话 关"); }
            else { _skip = !_skip; _auto = false; _skipTimer = 0; Toast(_skip ? "剧情跳过 开" : "剧情跳过 关"); }
        }
        Fill(r, on ? new Color(accent.r * 0.35f, accent.g * 0.35f, accent.b * 0.35f, 0.92f * a) : new Color(0.08f, 0.10f, 0.15f, 0.85f * a));
        Border(r, 1.5f, on ? new Color(accent.r, accent.g, accent.b, a) : new Color(0.35f, 0.40f, 0.48f, 0.6f * a));
        GUI.color = new Color(0.95f, 0.97f, 0.99f, a);
        GUI.Label(r, label, _btn);
        GUI.color = Color.white;
    }

    // 剧情浏览：左栏章节目录（点章过滤右栏），右栏列该章全部句（含后续未播），点任一句向前/向后定位播放
    void DrawLog(float W, float H, float a)
    {
        Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.72f * a));
        float pw = Mathf.Min(1060, W * 0.82f), ph = H * 0.76f, px = (W - pw) / 2f, py = (H - ph) / 2f;
        Fill(new Rect(px, py, pw, ph), new Color(0.06f, 0.08f, 0.12f, 0.97f * a));
        Border(new Rect(px, py, pw, ph), 2, new Color(0.37f, 0.88f, 0.78f, 0.6f * a));
        GUI.color = new Color(0.95f, 0.97f, 0.99f, a);
        GUI.Label(new Rect(px + 24, py + 16, pw - 48, 30), "剧情浏览 · 左栏章节目录 · 点击任一句定位播放（可跳转后续）· 滚轮浏览 · L/Esc 关闭", _btn);
        GUI.color = Color.white;

        float tocW = 200f;
        GUILayout.BeginArea(new Rect(px + 16, py + 56, tocW, ph - 72));
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
        Fill(new Rect(px + 16 + tocW + 6, py + 56, 1.5f, ph - 72), new Color(0.35f, 0.40f, 0.48f, 0.5f * a));

        GUILayout.BeginArea(new Rect(px + 16 + tocW + 14, py + 56, pw - 32 - tocW - 14, ph - 72));
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
                string prefix = l.type == LineType.Dialogue && !string.IsNullOrEmpty(l.speaker) ? l.speaker + "：" : "";
                bool isCur = ci == _ci && li == _li;
                bool isFut = ci > _ci || (ci == _ci && li > _li);
                var st = isCur ? _logCur : isFut ? _logFut : _logEntry;
                bool clicked = GUILayout.Button(prefix + l.text, st);
                if (isCur && _scrollToCur)
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

    void DrawSceneTag(ScenarioLine cur, float W, float H, float a)
    {
        float tx = W * 0.045f, ty = H * 0.03f;
        string head = $"第 {_ci + 1} 章 · {cur.scene}";
        var sz = _scene.CalcSize(new GUIContent(head));
        Fill(new Rect(tx - 12, ty - 7, sz.x + 24, sz.y + 14), new Color(0.05f, 0.07f, 0.11f, 0.72f * a));
        Fill(new Rect(tx - 12, ty - 7, 3, sz.y + 14), new Color(0.37f, 0.88f, 0.78f, 0.85f * a));
        GUI.color = new Color(0.92f, 0.94f, 0.97f, a);
        GUI.Label(new Rect(tx, ty, sz.x + 40, sz.y + 8), head, _scene);
        float y = ty + sz.y + 14;
        if (!string.IsNullOrEmpty(cur.location))
        {
            GUI.color = new Color(0.78f, 0.81f, 0.86f, 0.9f * a);
            GUI.Label(new Rect(tx, y, W * 0.55f, 44), cur.location, _cue);
            y += 40;
        }
        string cue = "";
        if (!string.IsNullOrEmpty(cur.bgm)) cue += "♪ " + cur.bgm;
        if (!string.IsNullOrEmpty(cur.sfx)) cue += (cue.Length > 0 ? "   " : "") + "🔊 " + cur.sfx;
        if (cue.Length > 0)
        {
            GUI.color = new Color(0.60f, 0.65f, 0.71f, 0.75f * a);
            GUI.Label(new Rect(tx, y, W * 0.55f, 30), cue, _cue);
        }
        GUI.color = Color.white;
    }

    Texture2D LoadPortrait(string res)
    {
        if (_portraits.TryGetValue(res, out var t)) return t;
        t = Resources.Load<Texture2D>(res);
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

    static float BoxH(float H) => Mathf.Max(176f, H * 0.24f);
    static float BoxW(float W) => W * 0.44f;   // 收窄框，给两侧 2.5× 立绘留出「不贴框」的边距

    // 立绘统一高度＝对话框高的 2.5 倍（不随原图 aspect 改变高度，只改宽度）
    float PortraitWidth(string speaker, float W, float H, out float ph)
    {
        ph = 0f;
        var res = ScenarioData.PortraitResource(speaker, true);
        if (res == null) return 0f;
        var tex = LoadPortrait(res);
        if (tex == null) return 0f;
        float aspect = tex.width / (float)tex.height;
        ph = BoxH(H) * 2.5f;
        return ph * aspect;
    }

    void DrawPortraits(ScenarioLine cur, float W, float H, float a)
    {
        if (_slotL == null && _slotR == null) return;
        bool dialogue = cur.type == LineType.Dialogue;
        Color tint = EnvTint();
        DrawOne(_slotR, false, dialogue && _activeSide == 2, tint, W, H, a);   // 暗的先画
        DrawOne(_slotL, true, dialogue && _activeSide == 1, tint, W, H, a);
        if (dialogue && _activeSide == 2) DrawOne(_slotR, false, true, tint, W, H, a);  // 亮的压最上
    }

    void DrawOne(string speaker, bool left, bool active, Color tint, float W, float H, float a)
    {
        if (speaker == null) return;
        var res = ScenarioData.PortraitResource(speaker, true);
        if (res == null) return;
        var tex = LoadPortrait(res);
        if (tex == null) return;
        float pw = PortraitWidth(speaker, W, H, out float ph);
        if (pw <= 0f) return;
        float bottom = H - H * 0.035f, py = bottom - ph;   // 底边对齐对话框底
        float bw = BoxW(W), bx = (W - bw) / 2f;
        // 水平居中于「框边↔页面边」的边距带，不贴框；夹取保证不出屏
        float px = left ? bx * 0.5f - pw * 0.5f : (bx + bw + W) * 0.5f - pw * 0.5f;
        px = left ? Mathf.Clamp(px, 4f, Mathf.Max(4f, bx - pw - 4f))
                  : Mathf.Clamp(px, bx + bw + 4f, Mathf.Max(bx + bw + 4f, W - pw - 4f));
        float k = active ? 1f : 0.62f;
        GUI.color = new Color(tint.r * k, tint.g * k, tint.b * k, a * (active ? 1f : 0.9f));
        GUI.DrawTexture(new Rect(px, py, pw, ph), tex, ScaleMode.ScaleToFit);
        GUI.color = Color.white;
    }

    void DrawBox(ScenarioLine cur, float W, float H, float a, Color accent)
    {
        float bw = BoxW(W), bh = BoxH(H), bx = (W - bw) / 2f, by = H - bh - H * 0.035f;
        Fill(new Rect(bx, by, bw, bh), new Color(0.05f, 0.07f, 0.11f, 0.88f * a));
        Fill(new Rect(bx, by, bw, 2), new Color(accent.r, accent.g, accent.b, 0.85f * a));

        bool hasName = cur.type == LineType.Dialogue && !string.IsNullOrEmpty(cur.speaker);
        if (hasName)
        {
            var sz = _name.CalcSize(new GUIContent(cur.speaker));
            float tw = sz.x + 40, th = 38, nx = bx + (bw - tw) / 2f, ny = by - th + 8;
            Fill(new Rect(nx, ny, tw, th), new Color(0.10f, 0.13f, 0.19f, 0.96f * a));
            Fill(new Rect(nx, ny + th - 3, tw, 3), new Color(accent.r, accent.g, accent.b, a));
            GUI.color = new Color(0.97f, 0.97f, 0.99f, a);
            GUI.Label(new Rect(nx, ny, tw, th), cur.speaker, _name);
        }

        // 立绘已不贴框，正文用满框宽（仅留常规内边距）
        float padL = 28, padR = 28;
        var style = cur.type == LineType.Thought ? _thought : cur.type == LineType.Narration ? _narr : _body;
        float top = hasName ? 20 : 28;
        GUI.color = new Color(1, 1, 1, a);
        GUI.Label(new Rect(bx + padL, by + top, bw - padL - padR, bh - top - 30), ShownText(cur), style);
        GUI.color = new Color(0.70f, 0.75f, 0.80f, 0.75f * a);
        GUI.Label(new Rect(bx, by + bh - 26, bw, 22), "点击 / 空格 继续 · Esc 退出 · H 按键侧栏", _hint);
        GUI.color = Color.white;
    }
}