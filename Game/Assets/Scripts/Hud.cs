using UnityEngine;

// HUD：IMGUI 纯代码界面（对齐 web 版布局：左上标题/操作、右上时间刻度、底部交互提示、右下版本号、toast）
// 迭代后再替换为正式 UI（UI Toolkit / UGUI）
public class Hud : MonoBehaviour
{
    public const string BUILD_VERSION = "u0.1";
    public TimeOfDay Time;

    Font _font;
    GUIStyle _title, _hint, _clock, _prompt, _ver, _toast;

    public void Init(TimeOfDay time)
    {
        Time = time;
        _font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "微软雅黑", "SimHei", "Arial" }, 16);
    }

    GUIStyle Mk(int size, TextAnchor anchor, Color color, bool bg = false)
    {
        var s = new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            alignment = anchor,
            wordWrap = false,
        };
        if (_font != null) s.font = _font;
        s.normal.textColor = color;
        if (bg)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.11f, 0.72f));
            tex.Apply();
            s.normal.background = tex;
            s.padding = new RectOffset(10, 10, 4, 4);
        }
        return s;
    }

    void OnGUI()
    {
        if (Time == null || Args.NoHud || IntroScreen.Live) return;
        if (_title == null)
        {
            _title = Mk(18, TextAnchor.UpperLeft, new Color(0.91f, 0.91f, 0.93f));
            _hint = Mk(14, TextAnchor.UpperLeft, new Color(0.85f, 0.87f, 0.9f, 0.85f));
            _clock = Mk(15, TextAnchor.UpperRight, new Color(0.92f, 0.93f, 0.95f));
            _prompt = Mk(16, TextAnchor.MiddleCenter, new Color(0.95f, 0.95f, 0.97f), true);
            _ver = Mk(12, TextAnchor.LowerRight, new Color(0.8f, 0.83f, 0.88f, 0.55f));
            _toast = Mk(15, TextAnchor.MiddleCenter, new Color(0.95f, 0.95f, 0.97f, 0.95f));
        }
        float W = Screen.width, H = Screen.height;
        if (DialogueManager.Active || CharacterPicker.IsOpen) return;

        // 左上：标题 + 操作说明
        GUI.Label(new Rect(16, 10, 900, 28), "他山之石", _title);
        GUI.Label(new Rect(16, 36, 1100, 24),
            "WASD 移动 ｜ Shift 跑 ｜ 拖拽 或 Q/E 视角 ｜ 滚轮 缩放 ｜ F11 全屏 ｜ F10 帧率 ｜ M 地图 ｜ V 视角 ｜ C 选人 ｜ B 剧情 ｜ F 交互 ｜ 0/1/2/3 时间 ｜ T +12h ｜ Y 天气 ｜ H 按键侧栏", _hint);

        // 右上：时间刻度（第 N 天 · ☀/🌙 HH:MM · 速度）
        float h = Time.Hour;
        string icon = h >= 5.5f && h < 18.5f ? "☀" : "🌙";
        int hh = Mathf.FloorToInt(h);
        int mm = Mathf.FloorToInt(Time.GameMinutes % 60f);
        string speed = Time.TimeSpeed <= 0f ? "暂停" : $"{Time.TimeSpeed}×";
        string wname = string.IsNullOrEmpty(Time.WeatherName) ? "" : " · " + Time.WeatherName;
        GUI.Label(new Rect(W - 420, 12, 400, 24), $"第 {Time.DayNum} 天 · {icon} {hh:D2}:{mm:D2} · {speed}{wname}", _clock);

        // 底部中央：交互提示（室内给整句，室外给「按 F 进入」）
        string prompt = !string.IsNullOrEmpty(Time.NearLabel) ? Time.NearLabel
            : (!string.IsNullOrEmpty(Time.Near) ? $"按 F 进入「{Time.Near}」" : null);
        if (prompt != null)
        {
            var size = _prompt.CalcSize(new GUIContent(prompt));
            GUI.Label(new Rect((W - size.x) / 2f, H - 74 - size.y, size.x, size.y), prompt, _prompt);
        }

        // 右下：版本号
        GUI.Label(new Rect(W - 260, H - 26, 246, 18), BUILD_VERSION + " · 2026-10-15", _ver);

        // toast
        if (!string.IsNullOrEmpty(Time.Toast))
            GUI.Label(new Rect(0, H - 126, W, 24), Time.Toast, _toast);
    }
}
