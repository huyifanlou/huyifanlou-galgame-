using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

// 剧本数据：把第一卷扩写稿的行标记语法解析成可播放的对话序列（docs/95 行语法）。
// 正文标签：【场景】【演出】【音效】【BGM】【旁白】【内心】【伏笔】【黑屏】；台词 角色：「…」；
// 行首 ※＝新写行（照显示），行尾 †＝说话人判定（剥掉），## 场 N｜标题 分场。
// 文末附录（台词分布/新增事实/拿不准/裁定/回退清单/待你定）一遇到即停止解析，绝不喂给玩家。
public enum LineType { Dialogue, Narration, Thought, Direction, Card }

public class ScenarioLine
{
    public LineType type;
    public string speaker;   // 仅 Dialogue 有
    public string text;
    public string scene;     // 来自 ## 分场标题
    public string location;  // 来自最近一条【场景】
    public string bgm;       // 最近一条【BGM】
    public string sfx;       // 最近一条【音效】
}

public class ScenarioChapter
{
    public string title;
    public string file;
    public List<ScenarioLine> lines = new List<ScenarioLine>();
}

public static class ScenarioData
{
    static readonly Regex TagRe = new Regex(@"^【([^】]+)】(.*)$", RegexOptions.Compiled);
    static readonly Regex DlgRe = new Regex(@"^(.+?)：「(.*)」\s*$", RegexOptions.Compiled);
    static readonly string[] Appendix = { "台词分布", "新增事实", "拿不准", "裁定", "回退清单", "待你定" };

    public static ScenarioChapter Parse(string txt, string file)
    {
        var ch = new ScenarioChapter { file = file, title = file };
        string scene = "", location = "", bgm = "", sfx = "";
        bool stop = false;
        foreach (var raw in txt.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("## "))
            {
                var h = line.Substring(3).Trim();
                if (Appendix.Any(k => h.Contains(k))) stop = true;   // 文末附录：之后全部丢弃
                else scene = h;                                       // 场 N / 幕前 / 幕尾 / 本章背景
                continue;
            }
            if (stop) continue;
            if (line.StartsWith("# ")) { ch.title = line.Substring(2).Trim(); continue; }
            if (line.StartsWith(">") || line.StartsWith("---")) continue;

            var s = line.TrimStart('※').Trim();
            if (s.EndsWith("†")) s = s.Substring(0, s.Length - 1).TrimEnd();

            var tm = TagRe.Match(s);
            if (tm.Success)
            {
                var tag = tm.Groups[1].Value;
                var body = tm.Groups[2].Value.Trim();
                switch (tag)
                {
                    case "伏笔": continue;                       // 不显示给玩家
                    case "场景": location = body; continue;      // 非阻塞上下文
                    case "BGM": bgm = body; continue;
                    case "音效": sfx = body; continue;
                    case "旁白": Emit(ch, LineType.Narration, null, body, scene, location, bgm, sfx); continue;
                    case "内心": Emit(ch, LineType.Thought, null, body, scene, location, bgm, sfx); continue;
                    case "黑屏": Emit(ch, LineType.Direction, null, "（黑屏）", scene, location, bgm, sfx); continue;
                    default: Emit(ch, LineType.Direction, null, body, scene, location, bgm, sfx); continue;
                }
            }
            if (s.StartsWith("**") && s.EndsWith("**") && s.Length > 4)
            {
                Emit(ch, LineType.Card, null, s.Substring(2, s.Length - 4).Trim(), scene, location, bgm, sfx);
                continue;
            }
            var dm = DlgRe.Match(s);
            if (dm.Success)
                Emit(ch, LineType.Dialogue, dm.Groups[1].Value.Trim(), dm.Groups[2].Value.Trim(), scene, location, bgm, sfx);
            // 其余（表格/清单/散文）一律跳过
        }
        return ch;
    }

    static void Emit(ScenarioChapter ch, LineType t, string sp, string text,
                     string scene, string loc, string bgm, string sfx)
    {
        if (string.IsNullOrEmpty(text)) return;
        ch.lines.Add(new ScenarioLine { type = t, speaker = sp, text = text, scene = scene, location = loc, bgm = bgm, sfx = sfx });
    }

    // 运行时从 StreamingAssets/Scenario 读全部章节（构建时由 Bootstrap.SyncScenario 从源目录同步）
    public static List<ScenarioChapter> LoadAll()
    {
        var dir = Path.Combine(Application.streamingAssetsPath, "Scenario");
        var list = new List<ScenarioChapter>();
        if (!Directory.Exists(dir))
        {
            Debug.LogWarning("[Scenario] 未找到 " + dir + "（先跑 Bootstrap.SyncScenarioBatch）");
            return list;
        }
        foreach (var f in Directory.GetFiles(dir, "*.md").OrderBy(x => x, System.StringComparer.Ordinal))
        {
            var txt = File.ReadAllText(f, Encoding.UTF8);
            list.Add(Parse(txt, Path.GetFileNameWithoutExtension(f)));
        }
        Debug.Log($"[Scenario] 载入 {list.Count} 章，共 {list.Sum(c => c.lines.Count)} 步" +
                  $"（台词 {list.Sum(c => c.lines.Count(l => l.type == LineType.Dialogue))}）");
        return list;
    }

    static Dictionary<string, string> _portrait;
    // 说话人 → 立绘 id。cut=true 取抠白底透明 PNG（-cut，对话用），否则白底 JPG（-dialogue，自选菜单缩略图用）。
    public static string PortraitResource(string speaker, bool cut = false)
    {
        if (string.IsNullOrEmpty(speaker)) return null;
        if (_portrait == null)
        {
            _portrait = new Dictionary<string, string>();
            foreach (var d in new[] { CharacterDef.FanLou, CharacterDef.JingYing, CharacterDef.JingNan,
                                      CharacterDef.LiLiYa, CharacterDef.YaoQingHan, CharacterDef.ZiWei })
            {
                var id = string.IsNullOrEmpty(d.id) ? "fanlou" : d.id;
                _portrait[d.label] = id;
            }
        }
        return _portrait.TryGetValue(speaker, out var pid) ? "Portraits/" + pid + (cut ? "-cut" : "-dialogue") : null;
    }
}