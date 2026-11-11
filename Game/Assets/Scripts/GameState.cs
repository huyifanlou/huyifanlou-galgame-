using UnityEngine;

// ─────────────────────────────────────────────────────────────
// 玩法系统 · 迭代 2：日程经营
//   一天分 4 个时段（早晨 / 上午 / 下午 / 夜晚），每个行动消耗 1 个时段；
//   夜晚结束自动睡觉进入下一天。时间由行动推进（TimeOfDay.TimeSpeed = 0）。
//   行动按"面前的地点"触发：走到教学楼按 R 上课、便利店按 R 打工、家按 R 休息…
//   键位：R 行动 ｜ N 睡觉（需在家或室内）
// ─────────────────────────────────────────────────────────────
public class GameState : MonoBehaviour
{
    public static GameState I;

    public TimeOfDay Time;

    // ── 存档数据（后续接 PlayerPrefs / 文件）──
    public int Day = 1;
    public int Slot;                 // 0 早晨 1 上午 2 下午 3 夜晚
    public float Stamina = 80f;      // 体力
    public float Spirit = 70f;       // 精神
    public float Study = 10f;        // 学识
    public float Charm = 10f;        // 人气
    public int Money = 1200;

    static readonly string[] SlotName = { "早晨", "上午", "下午", "夜晚" };
    static readonly float[] SlotHour = { 7f, 11f, 15f, 19f };

    // ── 行动表：Spot 对应 WorldData.SPOTS 里的地点名 ──
    struct Act
    {
        public string Spot, Label;
        public float Sta, Spi, Stu, Cha;
        public int Money;
        public Act(string spot, string label, float sta, float spi, float stu, float cha, int money)
        {
            Spot = spot; Label = label; Sta = sta; Spi = spi; Stu = stu; Cha = cha; Money = money;
        }
    }

    static readonly Act[] Acts =
    {
        new Act("教学楼", "上课",     -8f, -8f,  6f,  1f,    0),
        new Act("图书馆", "自习",     -5f, -2f,  4f,  0f,    0),
        new Act("实验楼", "做实验",   -6f, -4f,  5f,  0f,    0),
        new Act("操场",   "运动",      8f, -4f,  0f,  2f,    0),
        new Act("便利店", "打工",    -10f, -3f,  0f,  1f,   80),
        new Act("餐馆",   "打工",    -12f, -4f,  0f,  2f,  100),
        new Act("书店",   "看书",     -2f, -1f,  3f,  0f,  -30),
        new Act("家",     "休息",     12f,  8f,  0f,  0f,    0),
        new Act("公园",   "散步",     -2f,  8f,  0f,  2f,    0),
        new Act("樱花林", "散步",     -2f,  9f,  0f,  3f,    0),
        new Act("河畔",   "散步",     -2f,  7f,  0f,  2f,    0),
        new Act("廊桥",   "散步",     -2f,  7f,  0f,  2f,    0),
        new Act("池塘",   "钓鱼",     -4f,  6f,  0f,  1f,   30),
        new Act("游乐场", "玩耍",     -6f, 10f,  0f,  4f,  -60),
        new Act("摩天轮", "坐摩天轮", -2f, 12f,  0f,  3f,  -40),
        new Act("钟楼广场", "闲逛",   -3f,  5f,  0f,  3f,    0),
        new Act("鸟居",   "参拜",     -2f,  8f,  1f,  1f,    0),
        new Act("石拱桥", "看河",     -1f,  5f,  0f,  1f,    0),
    };

    static bool TryFind(string spot, out Act a)
    {
        a = default;
        if (string.IsNullOrEmpty(spot)) return false;
        for (int i = 0; i < Acts.Length; i++)
            if (Acts[i].Spot == spot) { a = Acts[i]; return true; }
        return false;
    }

    // ── 初始化 ──
    public void Init(TimeOfDay time)
    {
        I = this;
        Time = time;
        if (Time != null) Time.TimeSpeed = 0f;   // 日程制：时间由行动推进，不自动流逝
        ApplyClock();
    }

    void ApplyClock()
    {
        if (Time == null) return;
        Time.DayNum = Day;
        Time.GameMinutes = SlotHour[Mathf.Clamp(Slot, 0, 3)] * 60f;
    }

    // ── 行动 ──
    public bool HasAction(string spot) => TryFind(spot, out _);

    public string PromptFor(string spot)
    {
        if (!TryFind(spot, out var a)) return null;
        if (Stamina + a.Sta <= 0f) return "体力不足 · 按 N 睡觉";
        return $"按 R {a.Label}   体力{a.Sta:+0;-0} 精神{a.Spi:+0;-0} 学识{a.Stu:+0;-0} 人气{a.Cha:+0;-0}"
             + (a.Money != 0 ? $" ¥{a.Money:+0;-0}" : "");
    }

    public void DoAction(string spot)
    {
        if (!TryFind(spot, out var a)) { Toast("这里没什么可做的"); return; }
        if (Stamina + a.Sta <= 0f) { Toast("体力见底了，按 N 睡一觉"); return; }

        Stamina = Mathf.Clamp(Stamina + a.Sta, 0f, 100f);
        Spirit = Mathf.Clamp(Spirit + a.Spi, 0f, 100f);
        Study = Mathf.Clamp(Study + a.Stu, 0f, 100f);
        Charm = Mathf.Clamp(Charm + a.Cha, 0f, 100f);
        Money += a.Money;

        Toast($"{a.Label}完成 · {SlotName[Slot]}结束");
        Slot++;
        if (Slot > 3) Sleep();
        else ApplyClock();
    }

    // ── 睡觉：进入下一天 ──
    public void TrySleep()
    {
        bool atHome = Time != null && (Time.Near == "家" || Time.Indoor);
        if (!atHome) { Toast("要睡觉得先回家（或进屋）"); return; }
        Sleep();
    }

    void Sleep()
    {
        Day++;
        Slot = 0;
        Stamina = Mathf.Clamp(Stamina + 35f, 0f, 100f);
        Spirit = Mathf.Clamp(Spirit + 20f, 0f, 100f);
        ApplyClock();
        Toast($"第 {Day} 天 · 早晨");
    }

    void Toast(string s)
    {
        if (Time != null) Time.ShowToast(s);
        Debug.Log("[Game] " + s);
    }

    // ── HUD ──
    Font _font;
    GUIStyle _head, _label;
    Texture2D _white;

    void EnsureStyles()
    {
        if (_white == null)
        {
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
        }
        if (_font == null)
            _font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "微软雅黑", "SimHei", "Arial" }, 16);
        if (_head == null)
        {
            _head = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.UpperLeft };
            _label = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
            if (_font != null) { _head.font = _font; _label.font = _font; }
            _head.normal.textColor = new Color(0.95f, 0.95f, 0.97f);
            _label.normal.textColor = new Color(0.88f, 0.9f, 0.93f);
        }
    }

    void OnGUI()
    {
        if (Time == null) return;
        EnsureStyles();

        const float x = 16f, y = 72f, w = 236f;
        GUI.color = new Color(0.06f, 0.08f, 0.11f, 0.72f);
        GUI.DrawTexture(new Rect(x - 10f, y - 8f, w + 20f, 146f), _white);
        GUI.color = Color.white;

        GUI.Label(new Rect(x, y - 2f, w, 22f), $"第 {Day} 天 · {SlotName[Slot]}", _head);
        Bar(x, y + 26f, w, "体力", Stamina, new Color(0.93f, 0.46f, 0.40f));
        Bar(x, y + 48f, w, "精神", Spirit, new Color(0.44f, 0.72f, 0.93f));
        Bar(x, y + 70f, w, "学识", Study, new Color(0.95f, 0.82f, 0.42f));
        Bar(x, y + 92f, w, "人气", Charm, new Color(0.62f, 0.86f, 0.56f));
        GUI.Label(new Rect(x, y + 114f, w, 20f), $"¥ {Money}", _head);

        // 底部行动提示（比 F 提示高一行，避免重叠）
        string tip = PromptFor(Time.Near);
        if (tip != null)
        {
            var size = _label.CalcSize(new GUIContent(tip));
            var r = new Rect((Screen.width - size.x) * 0.5f - 12f, Screen.height - 108f, size.x + 24f, 26f);
            GUI.color = new Color(0.06f, 0.08f, 0.11f, 0.72f);
            GUI.DrawTexture(r, _white);
            GUI.color = Color.white;
            GUI.Label(new Rect(r.x + 12f, r.y, size.x, 26f), tip, _label);
        }
    }

    void Bar(float x, float y, float w, string label, float v, Color c)
    {
        GUI.Label(new Rect(x, y, 44f, 18f), label, _label);
        var bg = new Rect(x + 46f, y + 5f, w - 100f, 9f);
        GUI.color = new Color(1f, 1f, 1f, 0.16f);
        GUI.DrawTexture(bg, _white);
        GUI.color = c;
        GUI.DrawTexture(new Rect(bg.x, bg.y, bg.width * Mathf.Clamp01(v / 100f), bg.height), _white);
        GUI.color = Color.white;
        GUI.Label(new Rect(x + w - 50f, y, 50f, 18f), Mathf.RoundToInt(v).ToString(), _label);
    }
}
