using System.Collections.Generic;
using UnityEngine;

// 右上角圆形小地图：按世界数据绘制俯视图；点击传送；M 放大/缩小
// 大地图：鼠标拖拽平移、滚轮缩放；空白处单击不传送，双击才传送
public class Minimap : MonoBehaviour
{
    WorldBuilder _w;
    PlayerRig _rig;
    Texture2D _mapBase, _map, _disc, _ring, _me;
    Color32[] _basePixels;
    Color[] _viewBuf;
    const int Res = 512;        // 屏幕显示用视图
    const int BaseRes = 1024;   // 完整小镇底图（缩放时从中采样）
    float _min, _max;
    bool _large;
    readonly List<(string name, float x, float z, bool major)> _dest =
        new List<(string name, float x, float z, bool major)>();
    int _pending = -1;
    public bool Indoor { get; set; }   // 室内：地图可看，传送禁用
    public bool LargeOpen => _large;   // 大地图是否打开（第一人称让位鼠标用）

    float _zoom = 1f, _viewCx, _viewCz;
    bool _dragging;
    Vector2 _dragLast, _lastClickPos;
    float _dragMoved, _lastClickTime = -99f;
    float _rectX, _rectTop, _rectSize;

    public void Init(WorldBuilder w, PlayerRig rig)
    {
        _w = w;
        _rig = rig;
        if (Args.Str("-map") == "1") _large = true;
        BuildBaseMap();
        MakeCircleTextures();
        MakeMeTexture();
        BuildDestinations();
        ResetView();
        // 验收参数：直接看放大后的地图（-mapzoom 2.5 -mapx X -mapz Z）
        if (Args.F("-mapzoom") is float z0)
        {
            _zoom = Mathf.Clamp(z0, 1f, 4f);
            if (Args.F("-mapx") is float mx) _viewCx = mx;
            if (Args.F("-mapz") is float mz) _viewCz = mz;
            RefreshView();
        }
    }

    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
    static Color White => new Color(1f, 1f, 1f, 1f);

    // 完整小镇底图：不烘焙圆形遮罩（遮罩在 RefreshView 里按视图加）
    void BuildBaseMap()
    {
        float b = _w.W.BOUND + 25f;   // 圆形地图留边，让神社/车站等边角地标落在圆内
        _min = -b;
        _max = b;
        var buf = new Color[BaseRes * BaseRes];
        var grass = Hex("#4a6b3d");
        for (int i = 0; i < buf.Length; i++) buf[i] = grass;

        int PX(float x) => Mathf.Clamp(Mathf.RoundToInt((x - _min) / (_max - _min) * (BaseRes - 1)), 0, BaseRes - 1);
        int PZ(float z) => Mathf.Clamp(Mathf.RoundToInt((z - _min) / (_max - _min) * (BaseRes - 1)), 0, BaseRes - 1);
        void SetPx(int x, int y, Color c)
        {
            if (x >= 0 && y >= 0 && x < BaseRes && y < BaseRes) buf[(BaseRes - 1 - y) * BaseRes + x] = c;
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
            for (int px = 0; px < BaseRes; px++)
            {
                float wx = Mathf.Lerp(_min, _max, px / (float)(BaseRes - 1));
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
            for (int y = 0; y < BaseRes; y++)
                for (int x = 0; x < BaseRes; x++)
                {
                    float wx = Mathf.Lerp(_min, _max, x / (float)(BaseRes - 1));
                    float wz = Mathf.Lerp(_min, _max, y / (float)(BaseRes - 1));
                    float dx = (wx - p.pos[0]) / rx, dz = (wz - p.pos[1]) / rz;
                    if (dx * dx + dz * dz <= 1f) SetPx(x, y, water);
                }
        }
        var edge = Hex("#3b322a");
        var fill = Hex("#d8c8a8");
        foreach (var bl in _w.W.BUILDINGS)
        {
            float hw = bl.HalfWX, hd = bl.HalfWZ;
            Rect(bl.pos[0] - hw - 0.8f, bl.pos[1] - hd - 0.8f, bl.pos[0] + hw + 0.8f, bl.pos[1] + hd + 0.8f, edge);
            Rect(bl.pos[0] - hw, bl.pos[1] - hd, bl.pos[0] + hw, bl.pos[1] + hd, fill);
        }
        var court = Hex("#4f8a6a");
        foreach (var g in _w.W.GROUNDS)
        {
            if (g.id == "playground") continue;
            Rect(g.pos[0] - g.size[0] / 2f, g.pos[1] - g.size[1] / 2f,
                 g.pos[0] + g.size[0] / 2f, g.pos[1] + g.size[1] / 2f, court);
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

        _mapBase = new Texture2D(BaseRes, BaseRes, TextureFormat.RGBA32, false);
        _mapBase.wrapMode = TextureWrapMode.Clamp;
        _mapBase.filterMode = FilterMode.Bilinear;
        _mapBase.SetPixels(buf);
        _mapBase.Apply();
        _basePixels = _mapBase.GetPixels32();
    }

    void ResetView()
    {
        _zoom = 1f;
        _viewCx = _viewCz = 0f;
        RefreshView();
    }

    void ClampView()
    {
        float half = (_max - _min) / _zoom * 0.5f;
        _viewCx = Mathf.Clamp(_viewCx, _min + half, _max - half);
        _viewCz = Mathf.Clamp(_viewCz, _min + half, _max - half);
    }

    // 从 1024 底图采样当前视图，再套圆形遮罩（保持圆形地图边缘不随缩放移动）
    void RefreshView()
    {
        ClampView();
        float full = _max - _min;
        float span = full / _zoom;
        if (_map == null)
        {
            _map = new Texture2D(Res, Res, TextureFormat.RGBA32, false);
            _map.wrapMode = TextureWrapMode.Clamp;
            _map.filterMode = FilterMode.Bilinear;
        }
        if (_viewBuf == null || _viewBuf.Length != Res * Res) _viewBuf = new Color[Res * Res];
        float c0 = (Res - 1) * 0.5f, r0 = Res * 0.5f - 2f;
        for (int y = 0; y < Res; y++)
        {
            float wz = _viewCz + (y / (float)(Res - 1) - 0.5f) * span;
            float v = Mathf.Clamp01((_max - wz) / full) * (BaseRes - 1);
            int y0 = Mathf.FloorToInt(v), y1 = Mathf.Min(y0 + 1, BaseRes - 1);
            float ty = v - y0;
            for (int x = 0; x < Res; x++)
            {
                float wx = _viewCx + (x / (float)(Res - 1) - 0.5f) * span;
                float u = Mathf.Clamp01((wx - _min) / full) * (BaseRes - 1);
                int x0 = Mathf.FloorToInt(u), x1 = Mathf.Min(x0 + 1, BaseRes - 1);
                float tx = u - x0;
                int r0i = y0 * BaseRes, r1i = y1 * BaseRes;
                Color c00 = _basePixels[r0i + x0], c10 = _basePixels[r0i + x1];
                Color c01 = _basePixels[r1i + x0], c11 = _basePixels[r1i + x1];
                Color c = Color.Lerp(Color.Lerp(c00, c10, tx), Color.Lerp(c01, c11, tx), ty);
                float d = Mathf.Sqrt((x - c0) * (x - c0) + (y - c0) * (y - c0));
                c.a *= Mathf.Clamp01((r0 + 2f - d) / 2f);
                _viewBuf[(Res - 1 - y) * Res + x] = c;
            }
        }
        _map.SetPixels(_viewBuf);
        _map.Apply(false);
    }

    void BuildDestinations()
    {
        var w = _w.W;
        BuildingDef Find(string id) => System.Array.Find(w.BUILDINGS, b => b.id == id);
        void AddB(string label, string id, bool major = false)
        {
            var b = Find(id);
            if (b != null) _dest.Add((label, b.pos[0] + b.FrontOffsetX(1.7f), b.pos[1] + b.FrontOffsetZ(1.7f), major));
        }
        if (w.VILLA != null)
            _dest.Add(("家", w.VILLA.x, w.VILLA.z + w.VILLA.d / 2f + 1.7f, true));

        // 建筑级传送点：把学校/老街/商店街拆成具体建筑门口
        AddB("教学楼", "s_teach", true);
        AddB("图书馆", "s_lib", true);
        AddB("食堂", "s_canteen", true);
        AddB("实验楼", "s_lab");
        AddB("礼堂", "s_hall");
        AddB("宿舍楼", "s_dorm");
        AddB("活动中心", "s_center");
        AddB("餐厅", "r_rest", true);
        AddB("超市", "r_market", true);
        AddB("奶茶店", "r_milktea");
        AddB("照相馆", "r_photo");
        AddB("书店", "r_book");
        AddB("便利店", "r_conv");
        AddB("绸缎庄", "o_silk");
        AddB("茶楼", "o_tea");
        AddB("杂货铺", "o_grocery");
        AddB("药铺", "o_pharm");
        AddB("戏院", "o_theater");
        AddB("邮局", "o_post");
        AddB("神社", "k_haiden", true);
        AddB("车站", "st_hall", true);
        AddB("游乐场", "f_ticket", true);
        AddB("鬼屋", "f_haunted");
        AddB("茶座", "f_teahouse");
        AddB("音乐亭", "p_pav");

        // 非建筑地标保留（方便去公园/钟楼）
        _dest.Add(("钟楼", -17f, -5f, true));
        _dest.Add(("公园", 34f, -24f, true));
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

    // 玩家自己的箭头（深色描边 + 暖黄填充，尖端朝上）
    void MakeMeTexture()
    {
        const int S = 40;
        _me = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var px = new Color[S * S];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(0, 0, 0, 0);

        void Tri(float apexY, float baseY, float halfBase, Color c)
        {
            for (int y = 0; y < S; y++)
            {
                float t = (y - apexY) / (baseY - apexY);
                if (t < 0f || t > 1f) continue;
                float hw = halfBase * t;
                int x0 = Mathf.CeilToInt(S / 2f - hw), x1 = Mathf.FloorToInt(S / 2f + hw);
                for (int x = x0; x <= x1; x++)
                {
                    if (x < 0 || x >= S) continue;
                    px[(S - 1 - y) * S + x] = c;
                }
            }
        }
        Tri(2.5f, 37f, 15.5f, new Color(0.13f, 0.11f, 0.09f, 0.96f));
        Tri(5.5f, 33f, 11.5f, new Color(1f, 0.84f, 0.45f, 1f));
        _me.SetPixels(px);
        _me.Apply();
        _me.filterMode = FilterMode.Bilinear;
        _me.wrapMode = TextureWrapMode.Clamp;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            _large = !_large;
            ResetView();
            if (_rig != null) _rig.UnlockCursor();
        }
    }

    float ViewSpan => (_max - _min) / _zoom;

    void Project(float wx, float wz, out float px, out float py)
    {
        float span = ViewSpan;
        px = _rectX + (wx - (_viewCx - span * 0.5f)) / span * _rectSize;
        py = _rectTop + (wz - (_viewCz - span * 0.5f)) / span * _rectSize;
    }

    void ZoomAt(Vector2 m, float wheel)
    {
        float oldSpan = ViewSpan;
        float nx = Mathf.Clamp01((m.x - _rectX) / _rectSize);
        float nz = Mathf.Clamp01((m.y - _rectTop) / _rectSize);
        float wx = _viewCx + (nx - 0.5f) * oldSpan;
        float wz = _viewCz + (nz - 0.5f) * oldSpan;
        float z = Mathf.Clamp(_zoom * Mathf.Pow(1.18f, wheel), 1f, 4f);
        if (Mathf.Abs(z - _zoom) < 0.0001f) return;
        _zoom = z;
        float span = ViewSpan;
        _viewCx = wx - (nx - 0.5f) * span;
        _viewCz = wz - (nz - 0.5f) * span;
        RefreshView();
    }

    int HitDest(Vector2 m)
    {
        int hit = -1;
        float best = _large ? 15f : 11f;
        for (int i = 0; i < _dest.Count; i++)
        {
            Project(_dest[i].x, _dest[i].z, out float dx, out float dy);
            float dd = (new Vector2(dx, dy) - m).magnitude;
            if (dd < best) { best = dd; hit = i; }
        }
        return hit;
    }

    // 空白处：第一次单击只记录，第二次（0.36s 内且位置相近）才传送
    void HandleClick(Vector2 m)
    {
        if (Indoor) return;
        int hit = HitDest(m);
        if (hit >= 0)
        {
            _pending = hit;
            _lastClickTime = -99f;
            return;
        }
        if (!_large) return;
        float now = Time.unscaledTime;
        bool dbl = now - _lastClickTime < 0.36f &&
                   (m - _lastClickPos).sqrMagnitude < 144f;
        if (!dbl)
        {
            _lastClickTime = now;
            _lastClickPos = m;
            return;
        }
        _lastClickTime = -99f;
        float span = ViewSpan;
        float wx = _viewCx + ((m.x - _rectX) / _rectSize - 0.5f) * span;
        float wz = _viewCz + ((m.y - _rectTop) / _rectSize - 0.5f) * span;
        _rig.Teleport(wx, wz);
    }

    void DrawPending()
    {
        float pw = 360f, ph = 150f;
        var pr = new Rect((Screen.width - pw) / 2f, (Screen.height - ph) / 2f, pw, ph);
        GUI.color = new Color(0.04f, 0.05f, 0.06f, 0.96f);
        GUI.DrawTexture(pr, Texture2D.whiteTexture);
        GUI.color = Color.white;
        var st = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
        st.normal.textColor = new Color(1f, 0.95f, 0.85f);
        GUI.Label(new Rect(pr.x, pr.y + 18f, pw, 30f), $"传送到「{_dest[_pending].name}」？", st);
        var sst = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter };
        sst.normal.textColor = new Color(0.82f, 0.8f, 0.74f);
        GUI.Label(new Rect(pr.x, pr.y + 50f, pw, 20f), "会传送到该建筑门前", sst);
        if (GUI.Button(new Rect(pr.x + 60f, pr.y + 92f, 100f, 36f), "确认"))
        {
            var d = _dest[_pending];
            _rig.Teleport(d.x, d.z);
            _pending = -1;
        }
        if (GUI.Button(new Rect(pr.x + 200f, pr.y + 92f, 100f, 36f), "取消")) _pending = -1;
    }

    void OnGUI()
    {
        if (_map == null || _rig == null) return;
        float size = _large ? Mathf.Min(Screen.width, Screen.height) * 0.82f : Mathf.Min(272f, Screen.height * 0.33f);
        float x = _large ? (Screen.width - size) / 2f : Screen.width - size - 20f;
        float y = _large ? (Screen.height - size) / 2f - 6f : 70f;
        float top = y + 20f;
        var mapRect = new Rect(x, top, size, size);
        _rectX = x; _rectTop = top; _rectSize = size;
        Vector2 center = new Vector2(x + size / 2f, top + size / 2f);
        float radius = size / 2f;
        Vector2 mousePos = Event.current.mousePosition;

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

        if (Indoor)
        {
            var tt = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            tt.normal.textColor = new Color(1f, 0.9f, 0.7f);
            GUI.Label(new Rect(0, 40f, Screen.width, 24f), "室内 · 地图仅查看（出门后再传送）", tt);
        }

        var title = new GUIStyle(GUI.skin.label) { fontSize = _large ? 14 : 11, alignment = TextAnchor.MiddleLeft };
        title.normal.textColor = new Color(1f, 0.95f, 0.85f);
        GUI.Label(new Rect(x, y, size, 18f),
            _large ? "拖拽平移 · 滚轮缩放 · 单击地标确认 · 双击空白传送"
                   : "小镇地图（M 放大 · 地标确认传送）", title);

        var small = new GUIStyle(GUI.skin.label) { fontSize = _large ? 12 : 10, alignment = TextAnchor.MiddleLeft };
        var usedLabels = new List<Rect>();

        for (int i = 0; i < _dest.Count; i++)
        {
            var d = _dest[i];
            Project(d.x, d.z, out float dx, out float dy);
            if ((new Vector2(dx, dy) - center).sqrMagnitude > (radius * 0.96f) * (radius * 0.96f)) continue;
            if (dx < mapRect.xMin || dx > mapRect.xMax || dy < mapRect.yMin || dy > mapRect.yMax) continue;
            GUI.color = d.major ? new Color(1f, 0.95f, 0.8f) : new Color(0.72f, 0.88f, 1f);
            float ms = d.major ? 10f : 8f;
            GUI.DrawTexture(new Rect(dx - ms * 0.5f, dy - ms * 0.5f, ms, ms), Texture2D.whiteTexture);
            GUI.color = d.major ? new Color(0.15f, 0.35f, 0.55f) : new Color(0.12f, 0.28f, 0.36f);
            GUI.DrawTexture(new Rect(dx - ms * 0.3f, dy - ms * 0.3f, ms * 0.6f, ms * 0.6f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (!_large) continue;
            bool hover = (new Vector2(dx, dy) - mousePos).sqrMagnitude < 100f;
            if (!d.major && _zoom < 2.0f && !hover) continue;
            var dl = new GUIStyle(small);
            dl.normal.textColor = d.major ? new Color(0.9f, 0.97f, 1f, 1f) : new Color(0.92f, 0.88f, 0.74f, 1f);
            // 上下交替，避免住宅街一排建筑的标签叠在一起
            float ly = i % 2 == 0 ? dy - 18f : dy + 3f;
            float lw = 78f;
            var lr = new Rect(dx + 7f, ly, lw, 16f);
            bool overlap = false;
            foreach (var u in usedLabels)
                if (lr.Overlaps(u)) { overlap = true; break; }
            if (overlap && !hover) continue;
            GUI.color = new Color(0.04f, 0.05f, 0.06f, 0.72f);
            GUI.DrawTexture(new Rect(lr.x - 2f, lr.y, lr.width + 4f, lr.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(lr, "▸" + d.name, dl);
            usedLabels.Add(lr);
        }

        // 玩家自己：随相机朝向旋转的箭头（始终压在地标之上）
        var pp = _rig.Position;
        Project(pp.x, pp.z, out float mx, out float mz);
        if ((new Vector2(mx, mz) - center).sqrMagnitude <= radius * radius)
        {
            float asz = Mathf.Clamp(size * 0.085f, 15f, 44f);
            var savedMtx = GUI.matrix;
            GUIUtility.RotateAroundPivot(-_rig.Yaw * Mathf.Rad2Deg, new Vector2(mx, mz));
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(mx - asz / 2f, mz - asz / 2f, asz, asz), _me);
            GUI.matrix = savedMtx;
            GUI.color = Color.white;
        }

        var e = Event.current;
        bool inside = mapRect.Contains(mousePos) &&
                      (mousePos - center).sqrMagnitude <= radius * radius;

        // 滚轮缩放（大地图、圆内）
        if (_large && e.type == EventType.ScrollWheel && inside)
        {
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) < 0.01f) wheel = -e.delta.y * 0.1f;
            if (Mathf.Abs(wheel) > 0.01f) ZoomAt(mousePos, wheel);
            e.Use();
            return;
        }

        if (_pending >= 0)
        {
            DrawPending();
            return;
        }

        if (_large && e.type == EventType.MouseDown && e.button == 0 && inside)
        {
            _dragging = true;
            _dragMoved = 0f;
            _dragLast = mousePos;
            e.Use();
        }
        if (_large && _dragging && e.type == EventType.MouseDrag)
        {
            var d = mousePos - _dragLast;
            _dragLast = mousePos;
            _dragMoved += d.magnitude;
            float span = ViewSpan;
            _viewCx -= d.x / size * span;
            _viewCz -= d.y / size * span;
            RefreshView();
            e.Use();
        }
        if (_large && _dragging && e.type == EventType.MouseUp && e.button == 0)
        {
            _dragging = false;
            if (!Indoor && _dragMoved < 4f && inside) HandleClick(mousePos);
            e.Use();
        }
        if (!Indoor && !_large && e.type == EventType.MouseDown && e.button == 0 && inside)
        {
            int hit = HitDest(mousePos);
            if (hit >= 0) { _pending = hit; e.Use(); }
        }

        if (Indoor) return;
    }
}
