using UnityEngine;

// 开场 logo（F-1 版式）：Unity splash 之后播 3.0s（2.6s 内容 + 0.4s 淡出），空格/鼠标可跳过。
// 素材是烘好的 PNG（隶书在别人机器上不一定有，运行时取系统字体会掉方框，见 docs/91）。
// 截图验收：-shot 默认跳过开场；加 -intro <秒> 冻结到指定时刻再拍。
public class IntroScreen : MonoBehaviour
{
    public static bool Live { get; private set; }

    const float DW = 1600f, DH = 900f;      // 设计分辨率
    const float FADE_OUT = 2.6f, END = 3.0f;

    Texture2D _ridge, _sub, _stone, _white;
    readonly Texture2D[] _chars = new Texture2D[4];
    float _t, _freeze = -1f;
    bool _ready;

    public void Init()
    {
        _freeze = Args.F("-intro") ?? -1f;
        bool shot = !string.IsNullOrEmpty(Args.Str("-shot"));
        if (Args.Has("-nointro") || (shot && _freeze < 0f)) { enabled = false; return; }

        for (int i = 0; i < 4; i++) _chars[i] = Resources.Load<Texture2D>($"intro/intro-char{i}");
        _ridge = Resources.Load<Texture2D>("intro/intro-ridge");
        _sub = Resources.Load<Texture2D>("intro/intro-sub");
        _stone = Resources.Load<Texture2D>("intro/intro-stone");
        if (_chars[0] == null || _ridge == null) { Debug.LogWarning("[Intro] 开场素材缺失，跳过"); enabled = false; return; }

        _white = new Texture2D(1, 1); _white.SetPixel(0, 0, Color.white); _white.Apply();
        _ready = true; Live = true;
    }

    void Update()
    {
        if (!_ready) return;
        if (_freeze >= 0f) { _t = _freeze; return; }
        _t += Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) _t = Mathf.Max(_t, FADE_OUT);
        if (_t >= END) { Live = false; enabled = false; }
    }

    void OnGUI()
    {
        if (!_ready) return;
        float s = Mathf.Min(Screen.width / DW, Screen.height / DH);
        float ox = (Screen.width - DW * s) * 0.5f, oy = (Screen.height - DH * s) * 0.5f;
        Rect R(float x, float y, float w, float h) =>
            new Rect(ox + x * s, oy + y * s, w * s, h * s);

        float outA = 1f - Mathf.Clamp01((_t - FADE_OUT) / (END - FADE_OUT));
        GUI.color = new Color(0.02f, 0.027f, 0.047f, outA);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _white);
        GUI.color = Color.white;

        // 0.35→0.95 山脊自下方浮起
        float ra = Ease((_t - 0.35f) / 0.60f);
        GUI.color = new Color(1, 1, 1, ra * 0.85f * outA);
        GUI.DrawTexture(R(0, DH - 360 + (1 - ra) * 26, DW, 360), _ridge);

        // 0.90 起逐字淡入（每字间隔 90ms，各 0.28s）
        for (int i = 0; i < 4; i++)
        {
            if (_chars[i] == null) continue;
            float a = Ease((_t - (0.90f + i * 0.09f)) / 0.28f);
            if (a <= 0f) continue;
            GUI.color = new Color(1, 1, 1, a * outA);
            GUI.DrawTexture(R(485 + i * 210 - 150, 300 - 150 + (1 - a) * 14, 300, 300), _chars[i]);
        }

        // 1.60→1.95 青色横线由左划出
        float la = Ease((_t - 1.60f) / 0.35f);
        if (la > 0f)
        {
            GUI.color = new Color(0.37f, 0.88f, 0.78f, la * 0.28f * outA);
            GUI.DrawTexture(R(675, 446, 250 * la, 11), _white);
            GUI.color = new Color(0.37f, 0.88f, 0.78f, la * outA);
            GUI.DrawTexture(R(675, 450, 250 * la, 3), _white);
        }

        // 2.10→2.45 英文副标与青石点出现
        float sa = Ease((_t - 2.10f) / 0.35f);
        if (sa > 0f)
        {
            GUI.color = new Color(1, 1, 1, sa * outA);
            GUI.DrawTexture(R(100, 456, 1400, 80), _sub);
            if (_stone != null) GUI.DrawTexture(R(768, 182, 64, 64), _stone);
        }
        GUI.color = Color.white;
    }

    static float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
