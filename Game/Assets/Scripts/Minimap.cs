using System.Collections.Generic;
using UnityEngine;

// 右上角圆形小地图：按世界数据绘制俯视图；点击传送；M 放大/缩小
public class Minimap : MonoBehaviour
{
    WorldBuilder _w;
    PlayerRig _rig;
    Texture2D _map, _disc, _ring;
    const int Res = 512;
    float _min, _max;
    bool _large;
    readonly List<(string name, float x, float z, Color col)> _marks =
        new List<(string name, float x, float z, Color col)>();

    public void Init(WorldBuilder w, PlayerRig rig)
    {
        _w = w;
        _rig = rig;
        if (Args.Str("-map") == "1") _large = true;
        BuildMap();
        MakeCircleTextures();
    }

    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
    static Color White => new Color(1f, 1f, 1f, 1f);

    void BuildMap()
    {
        float b = _w.W.BOUND + 25f;   // 圆形地图留边，让神社/车站等边角地标落在圆内
        _min = -b;
        _max = b;
        var buf = new Color[Res * Res];
        var grass = Hex("#4a6b3d");
        for (int i = 0; i < buf.Length; i++) buf[i] = grass;

        int PX(float x) => Mathf.Clamp(Mathf.RoundToInt((x - _min) / (_max - _min) * (Res - 1)), 0, Res - 1);
        int PZ(float z) => Mathf.Clamp(Mathf.RoundToInt((z - _min) / (_max - _min) * (Res - 1)), 0, Res - 1);
        void SetPx(int x, int y, Color c)
        {
            if (x >= 0 && y >= 0 && x < Res && y < Res) buf[(Res - 1 - y) * Res + x] = c;
        }
        void Rect(float x0, float z0, float x1, float z1, Color c)
        {
            int a = PX(Mathf.Min(x0, x1)), bb = PX(Mathf.Max(x0, x1));
            int c0 = PZ(Mathf.Min(z0, z1)), d0 = PZ(Mathf.Max(z0, z1));
            for (int y = c0; y <= d0; y++)
                for (int x = a; x <= bb; x++) SetPx(x, y, c);
        }

        var river = _w.W.RIVER;
        if (river != null)
        {
            var water = Hex("#4d7f8e");
            for (int px = 0; px < Res; px++)
            {
                float wx = Mathf.Lerp(_min, _max, px / (float)(Res - 1));
                float zc = river.amp * Mathf.Sin((wx + river.phase) / river.freq) + river.offset;
                int y0 = PZ(zc - river.width / 2f), y1 = PZ(zc + river.width / 2f);
                for (int y = y0; y <= y1; y++) SetPx(px, y, water);
            }
        }
        var road = Hex("#b9b2a2");
        foreach (var r in _w.W.ROADS)
        {
            if (r.axis == "z") Rect(r.at - r.width / 2f, r.from, r.at + r.width / 2f, r.to, road);
            else Rect(r.from, r.at - r.width / 2f, r.to, r.at + r.width / 2f, road);
        }
        if (_w.W.POND != null)
        {
            var p = _w.W.POND;
            float rx = p.r, rz = p.r * 0.7f;
            var water = Hex("#4d7f8e");
            for (int y = 0; y < Res; y++)
                for (int x = 0; x < Res; x++)
                {
                    float wx = Mathf.Lerp(_min, _max, x / (float)(Res - 1));
                    float wz = Mathf.Lerp(_min, _max, y / (float)(Res - 1));
                    float dx = (wx - p.pos[0]) / rx, dz = (wz - p.pos[1]) / rz;
                    if (dx * dx + dz * dz <= 1f) SetPx(x, y, water);
                }
        }
        var edge = Hex("#3b322a");
        var fill = Hex("#d8c8a8");
        foreach (var bl in _w.W.BUILDINGS)
        {
            float hw = bl.size[0] / 2f, hd = bl.size[2] / 2f;
            Rect(bl.pos[0] - hw - 0.8f, bl.pos[1] - hd - 0.8f, bl.pos[0] + hw + 0.8f, bl.pos[1] + hd + 0.8f, edge);
            Rect(bl.pos[0] - hw, bl.pos[1] - hd, bl.pos[0] + hw, bl.pos[1] + hd, fill);
        }
        var v = _w.W.VILLA;
        if (v != null)
        {
            Rect(v.x - v.w / 2f - 0.8f, v.z - v.d / 2f - 0.8f, v.x + v.w / 2f + 0.8f, v.z + v.d / 2f + 0.8f, edge);
            Rect(v.x - v.w / 2f, v.z - v.d / 2f, v.x + v.w / 2f, v.z + v.d / 2f, Hex("#e8d0b0"));
        }
        var ride = Hex("#b86a4e");
        var stall = Hex("#c9a24a");
        foreach (var p in _w.W.PROPS)
        {
            if (p.type.StartsWith("ride") || p.type == "kart")
                Rect(p.x - 1.6f, p.z - 1.6f, p.x + 1.6f, p.z + 1.6f, ride);
            else if (p.type == "stall")
                Rect(p.x - 0.9f, p.z - 0.9f, p.x + 0.9f, p.z + 0.9f, stall);
        }

        // 圆形遮罩：圆外透明
        float c0x = Res / 2f, c0y = Res / 2f, r0 = Res / 2f - 2f;
        for (int y = 0; y < Res; y++)
            for (int x = 0; x < Res; x++)
            {
                float d = Mathf.Sqrt((x - c0x) * (x - c0x) + (y - c0y) * (y - c0y));
                if (d <= r0) continue;
                float a = Mathf.Clamp01((r0 + 2f - d) / 2f);
                int i = (Res - 1 - y) * Res + x;
                var c = buf[i];
                buf[i] = new Color(c.r, c.g, c.b, c.a * a);
            }

        _map = new Texture2D(Res, Res, TextureFormat.RGBA32, false);
        _map.wrapMode = TextureWrapMode.Clamp;
        _map.filterMode = FilterMode.Bilinear;
        _map.SetPixels(buf);
        _map.Apply();

        _marks.Clear();
        _marks.Add(("学校", -31, -58, Hex("#d8c8a8")));
        _marks.Add(("钟楼", -14, -12, Hex("#e8c46a")));
        _marks.Add(("老街", -52, 27, Hex("#e0a070")));
        _marks.Add(("公园", 34, -40, Hex("#7ab0a0")));
        _marks.Add(("神社", 79, -78, Hex("#d08070")));
        _marks.Add(("游乐场", 50, 38, Hex("#c080c0")));
        _marks.Add(("车站", 76, 76, Hex("#c8b090")));
        _marks.Add(("家", -35.5f, 66, Hex("#f0d0a0")));
    }

    void MakeCircleTextures()
    {
        const int S = 256;
        _disc = new Texture2D(S, S, TextureFormat.RGBA32, false);
        _ring = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var d = new Color[S * S];
        var r = new Color[S * S];
        float c = S / 2f, rad = S / 2f - 2f;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dd = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                d[y * S + x] = dd <= rad ? new Color(0.05f, 0.06f, 0.07f, 0.92f) : new Color(0, 0, 0, 0);
                bool ring = dd > rad - 4f && dd <= rad;
                r[y * S + x] = ring ? new Color(1f, 0.93f, 0.8f, 0.9f) : new Color(0, 0, 0, 0);
            }
        _disc.SetPixels(d); _disc.Apply();
        _ring.SetPixels(r); _ring.Apply();
        _disc.filterMode = _ring.filterMode = FilterMode.Bilinear;
        _disc.wrapMode = _ring.wrapMode = TextureWrapMode.Clamp;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) _large = !_large;
    }

    void OnGUI()
    {
        if (_map == null || _rig == null) return;
        float size = _large ? Mathf.Min(Screen.width, Screen.height) * 0.82f : Mathf.Min(272f, Screen.height * 0.33f);
        float x = _large ? (Screen.width - size) / 2f : Screen.width - size - 20f;
        float y = _large ? (Screen.height - size) / 2f - 6f : 70f;
        float top = y + 20f;
        var mapRect = new Rect(x, top, size, size);
        Vector2 center = new Vector2(x + size / 2f, top + size / 2f);
        float radius = size / 2f;

        if (_large)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        GUI.color = White;
        GUI.DrawTexture(mapRect, _disc, ScaleMode.StretchToFill);
        GUI.DrawTexture(mapRect, _map, ScaleMode.StretchToFill);
        GUI.DrawTexture(mapRect, _ring, ScaleMode.StretchToFill);
        GUI.color = Color.white;

        var title = new GUIStyle(GUI.skin.label) { fontSize = _large ? 14 : 11, alignment = TextAnchor.MiddleLeft };
        title.normal.textColor = new Color(1f, 0.95f, 0.85f);
        GUI.Label(new Rect(x, y, size, 18f),
            _large ? "M 缩小 · 点击传送 · 按住 Alt 显示鼠标" : "小镇地图（M 放大 · 点击传送）", title);

        var small = new GUIStyle(GUI.skin.label) { fontSize = _large ? 12 : 10, alignment = TextAnchor.MiddleLeft };
        foreach (var m in _marks)
        {
            float mx = x + (m.x - _min) / (_max - _min) * size;
            float my = top + (m.z - _min) / (_max - _min) * size;
            if ((new Vector2(mx, my) - center).sqrMagnitude > (radius * 0.98f) * (radius * 0.98f)) continue;
            GUI.color = m.col;
            GUI.DrawTexture(new Rect(mx - 3.5f, my - 3.5f, 7f, 7f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float lw = _large ? 84f : 58f;
            float lx = mx + 6f;
            if (lx + lw > center.x + radius - 4f) lx = mx - 6f - lw;
            float ly = Mathf.Clamp(my - 8f, top, top + size - 14f);
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(lx - 2f, ly, lw + 4f, 14f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var label = new GUIStyle(small);
            label.normal.textColor = new Color(1f, 0.96f, 0.88f, 0.95f);
            GUI.Label(new Rect(lx, ly, lw, 14f), m.name, label);
        }

        var p = _rig.Position;
        float px = x + (p.x - _min) / (_max - _min) * size;
        float py = top + (p.z - _min) / (_max - _min) * size;
        var off = new Vector2(px, py) - center;
        if (off.sqrMagnitude > (radius - 6f) * (radius - 6f))
            off = off.normalized * (radius - 6f);
        px = center.x + off.x;
        py = center.y + off.y;
        GUI.color = new Color(0.15f, 0.1f, 0.05f);
        GUI.DrawTexture(new Rect(px - 4f, py - 4f, 8f, 8f), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.85f, 0.25f);
        GUI.DrawTexture(new Rect(px - 3f, py - 3f, 6f, 6f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && mapRect.Contains(e.mousePosition) &&
            (e.mousePosition - center).sqrMagnitude <= radius * radius)
        {
            float wx = Mathf.Lerp(_min, _max, (e.mousePosition.x - x) / size);
            float wz = Mathf.Lerp(_min, _max, (e.mousePosition.y - top) / size);
            if (_rig.Teleport(wx, wz)) e.Use();
        }
    }
}
