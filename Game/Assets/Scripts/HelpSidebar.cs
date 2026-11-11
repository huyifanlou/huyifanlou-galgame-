using UnityEngine;

// 按键功能侧边栏（H 开关）。挂在 DialogueManager/CharacterPicker 之后创建，保证 OnGUI 最后画、不被剧情压暗层盖住。
// 世界与剧情播放中都可用；集合当前全部键位（与 PlayerRig/TimeOfDay/Weather/Minimap/DialogueManager 的实际绑定一致）。
public class HelpSidebar : MonoBehaviour
{
    Font _font;
    GUIStyle _title, _head, _row;
    bool _open;
    Vector2 _scroll;

    void Update()
    {
        if (Args.Has("-help")) _open = true;   // 验收钩子：开场即开侧边栏
        if (!Args.NoHud && !IntroScreen.Live && Input.GetKeyDown(KeyCode.H)) _open = !_open;
    }

    void OnGUI()
    {
        if (!_open || Args.NoHud || IntroScreen.Live) return;
        GUI.depth = -100;   // depth 越大越先画（实测 100 会被小地图盖）；负值最后画、压在小地图/剧情/菜单之上
        if (_title == null)
        {
            _font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "微软雅黑", "SimHei", "Arial" }, 16);
            _title = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = false };
            _title.normal.textColor = new Color(0.95f, 0.97f, 0.99f);
            _head = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, wordWrap = false };
            _head.normal.textColor = new Color(0.37f, 0.88f, 0.78f);
            _row = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            _row.normal.textColor = new Color(0.88f, 0.90f, 0.93f);
            if (_font != null) { _title.font = _font; _head.font = _font; _row.font = _font; }
        }
        float W = Screen.width, H = Screen.height;
        float pw = Mathf.Min(360f, W * 0.34f), px = W - pw - 12, py = 56f, ph = H - py - 16f;
        GUI.color = new Color(0.05f, 0.07f, 0.11f, 0.94f);
        GUI.DrawTexture(new Rect(px, py, pw, ph), Texture2D.whiteTexture);
        GUI.color = new Color(0.37f, 0.88f, 0.78f, 0.8f);
        GUI.DrawTexture(new Rect(px, py, 2f, ph), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(px + 16, py + 12, pw - 32, 26), "按键功能 · H 关闭", _title);

        GUILayout.BeginArea(new Rect(px + 16, py + 44, pw - 32, ph - 56));
        _scroll = GUILayout.BeginScrollView(_scroll, false, true);
        Group("移动 / 视角", new[]
        {
            "WASD / 方向键　移动",
            "Shift　跑",
            "鼠标拖拽　转视角",
            "Q / E　左右转视角",
            "滚轮　缩放距离",
            "V　第一 / 第三人称",
        });
        Group("世界交互", new[]
        {
            "F　交互 / 进入室内",
            "M　地图（拖拽平移 · 滚轮缩放 · 双击空白传送 · 点标记确认）",
            "C　人物自选菜单（1–6 切换）",
        });
        Group("时间 / 天气", new[]
        {
            "0 / 1 / 2 / 3　时间速度（暂停 / 1× / 6× / 30×）",
            "T　+12 小时",
            "[ / ]　−1 / +1 小时",
            "Y　切换天气",
        });
        Group("剧情", new[]
        {
            "B　开 / 继续剧情",
            "点击 / 空格 / 回车　推进（打字机未尽先补全）",
            "L　剧情浏览（左侧章节目录 · 点句定位）",
            "A　自动对话开关",
            "S　剧情跳过开关",
            "Esc　关闭浏览 / 退出剧情",
        });
        Group("系统", new[]
        {
            "F10　帧率三档（锁60 / 垂直同步 / 不锁帧）",
            "F11　全屏",
            "H　本侧栏开关",
            "开场 logo：空格 / 点击 跳过",
        });
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        GUI.color = Color.white;
    }

    void Group(string head, string[] rows)
    {
        GUILayout.Space(6);
        GUILayout.Label(head, _head);
        foreach (var r in rows) GUILayout.Label(r, _row);
    }
}