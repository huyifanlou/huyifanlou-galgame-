using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// 卡通渲染贴图库（2026-11-10 美术重做）
//
// 这是本轮「从 0 重做美术风格」的第二根技术支柱（第一根是 MatLib → Campus/Toon）。
//
// 上一版贴图是为「写实光照」画的：大量随机噪点、渐变、脏污、雨痕、玻璃反射 ——
// 这些在卡通渲染下是灾难：噪点会被 ramp 光照放大成脏斑，渐变会和阶梯光照打架，
// 最后画面既不像写实也不像卡通（本轮第一张对比图就是这个症状）。
//
// 卡通渲染的贴图原则：
//   · 平涂大色块（flat fill），一个面只有 1~3 个色阶
//   · 硬边分隔（窗框/墙裙/分格线用纯色实线，不做羽化）
//   · 高饱和、明度高（暗部交给 shader 的 _ShadowTint 统一压，贴图本身不画暗）
//   · 不用随机噪点做「材质感」，改用有规律的图案（砖缝、瓦垄、条纹）
//
// 像素密度体系完全保留（TexScale 的 160px/米基准），所以世界尺度不变。
// ============================================================================
public static class TextureLibrary
{
    // ── mulberry32（保留：少量地方仍需确定性随机，如瓦垄错位）──
    class Rand
    {
        uint a;
        public Rand(uint seed) { a = seed; }
        public double Next()
        {
            unchecked
            {
                a += 0x6D2B79F5u;
                uint t = a;
                t = (uint)((t ^ (t >> 15)) * (1u | t));
                t = (t + (uint)((t ^ (t >> 7)) * (61u | t))) ^ t;
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
        public int Int(int n) => (int)Math.Floor(Next() * n);
    }

    // ── 像素画布 ──
    class Px
    {
        public readonly int W, H, S;
        public readonly int PW, PH;
        public readonly Color[] Buf;
        public Px(int w, int h, int s = 1)
        {
            W = w; H = h; S = s; PW = w * s; PH = h * s;
            Buf = new Color[PW * PH];
            for (int i = 0; i < Buf.Length; i++) Buf[i] = new Color(0, 0, 0, 0);
        }

        public void Set(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= PW || y >= PH) return;
            int i = y * PW + x;
            if (c.a >= 0.999f) { Buf[i] = c; return; }
            var d = Buf[i];
            float a = c.a + d.a * (1 - c.a);
            if (a <= 0.0001f) { Buf[i] = new Color(0, 0, 0, 0); return; }
            Buf[i] = new Color((c.r * c.a + d.r * d.a * (1 - c.a)) / a, (c.g * c.a + d.g * d.a * (1 - c.a)) / a,
                (c.b * c.a + d.b * d.a * (1 - c.a)) / a, a);
        }

        public void FillRect(int x, int y, int w, int h, Color c) => FillRect((float)x, y, w, h, c);

        public void FillRect(float x, float y, float w, float h, Color c)
        {
            int x0 = Mathf.FloorToInt(x * S), x1 = Mathf.FloorToInt((x + w) * S);
            int y0 = Mathf.FloorToInt(y * S), y1 = Mathf.FloorToInt((y + h) * S);
            float inv = 1f / S;
            for (int yy = y0; yy < y1; yy++)
                for (int xx = x0; xx < x1; xx++)
                {
                    float cov = (Mathf.Min(x + w, (xx + 1) * inv) - Mathf.Max(x, xx * inv)) * (Mathf.Min(y + h, (yy + 1) * inv) - Mathf.Max(y, yy * inv)) * S * S;
                    if (cov <= 0.001f) continue;
                    var cc = c;
                    cc.a = c.a * Mathf.Min(1f, cov);
                    Set(xx, yy, cc);
                }
        }

        public void FillCircle(float cx, float cy, float r, Color c)
        {
            int x0 = Mathf.FloorToInt((cx - r) * S), x1 = Mathf.CeilToInt((cx + r) * S);
            int y0 = Mathf.FloorToInt((cy - r) * S), y1 = Mathf.CeilToInt((cy + r) * S);
            float inv = 1f / S;
            for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
            {
                float dx = (x + 0.5f) * inv - cx, dy = (y + 0.5f) * inv - cy;
                if (dx * dx + dy * dy <= r * r) Set(x, y, c);
            }
        }

        public static bool InRound(float px, float py, float x, float y, float w, float h, float r)
        {
            if (px < x || py < y || px > x + w || py > y + h) return false;
            float rx = Mathf.Max(x + r, Mathf.Min(px, x + w - r));
            float ry = Mathf.Max(y + r, Mathf.Min(py, y + h - r));
            float dx = px - rx, dy = py - ry;
            if (px >= x + r && px <= x + w - r) return true;
            if (py >= y + r && py <= y + h - r) return true;
            return dx * dx + dy * dy <= r * r + 0.0001f;
        }

        public void FillRound(float x, float y, float w, float h, float r, Color c)
        {
            int x0 = Mathf.FloorToInt(x * S), x1 = Mathf.FloorToInt((x + w) * S);
            int y0 = Mathf.FloorToInt(y * S), y1 = Mathf.FloorToInt((y + h) * S);
            float inv = 1f / S;
            for (int yy = y0; yy < y1; yy++) for (int xx = x0; xx < x1; xx++)
            {
                float px = (xx + 0.5f) * inv, py = (yy + 0.5f) * inv;
                if (InRound(px, py, x, y, w, h, r)) Set(xx, yy, c);
            }
        }

        public void ClearRound(float x, float y, float w, float h, float r)
        {
            int x0 = Mathf.FloorToInt(x * S), x1 = Mathf.FloorToInt((x + w) * S);
            int y0 = Mathf.FloorToInt(y * S), y1 = Mathf.FloorToInt((y + h) * S);
            float inv = 1f / S;
            for (int yy = y0; yy < y1; yy++) for (int xx = x0; xx < x1; xx++)
            {
                float px = (xx + 0.5f) * inv, py = (yy + 0.5f) * inv;
                if (InRound(px, py, x, y, w, h, r)) Buf[yy * PW + xx] = new Color(0, 0, 0, 0);
            }
        }

        public void Clear(int x, int y) { if (x >= 0 && y >= 0 && x < PW && y < PH) Buf[y * PW + x] = new Color(0, 0, 0, 0); }

        public void StrokeRound(float x, float y, float w, float h, float r, float lw, Color c)
        {
            int x0 = Mathf.FloorToInt(x * S), x1 = Mathf.FloorToInt((x + w) * S);
            int y0 = Mathf.FloorToInt(y * S), y1 = Mathf.FloorToInt((y + h) * S);
            float inv = 1f / S;
            for (int yy = y0; yy < y1; yy++) for (int xx = x0; xx < x1; xx++)
            {
                float px = (xx + 0.5f) * inv, py = (yy + 0.5f) * inv;
                bool outer = InRound(px, py, x, y, w, h, r);
                bool inner = InRound(px, py, x + lw, y + lw, w - 2 * lw, h - 2 * lw, Mathf.Max(0f, r - lw));
                if (outer && !inner) Set(xx, yy, c);
            }
        }

        public void StrokeRect(float x, float y, float w, float h, float lw, Color c) => StrokeRound(x, y, w, h, 0, lw, c);

        public void StrokeCircle(float cx, float cy, float r, float lw, Color c)
        {
            int x0 = Mathf.FloorToInt((cx - r - lw) * S), x1 = Mathf.CeilToInt((cx + r + lw) * S);
            int y0 = Mathf.FloorToInt((cy - r - lw) * S), y1 = Mathf.CeilToInt((cy + r + lw) * S);
            float inv = 1f / S;
            for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
            {
                float dx = (x + 0.5f) * inv - cx, dy = (y + 0.5f) * inv - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= r + lw && d >= r - lw) Set(x, y, c);
            }
        }
    }

    // ── 颜色解析 ──
    static Color C(string s)
    {
        if (s.StartsWith("rgba"))
        {
            var parts = s.Substring(5, s.Length - 6).Split(',');
            return new Color(ParseF(parts[0]) / 255f, ParseF(parts[1]) / 255f, ParseF(parts[2]) / 255f, ParseF(parts[3]));
        }
        ColorUtility.TryParseHtmlString(s, out var c);
        return c;
    }
    static float ParseF(string s) => float.Parse(s.Trim(), System.Globalization.CultureInfo.InvariantCulture);

    static Rand R;
    static Rand NewR() => new Rand(20261015);

    static Texture2D From(Px p)
    {
        var t = new Texture2D(p.PW, p.PH, TextureFormat.RGBA32, false);
        var flipped = new Color[p.Buf.Length];
        for (int y = 0; y < p.PH; y++)
            Array.Copy(p.Buf, y * p.PW, flipped, (p.PH - 1 - y) * p.PW, p.PW);
        t.SetPixels(flipped);
        t.Apply(false, false);
        return t;
    }

    // ========================================================================
    // 卡通贴图生成器
    // ========================================================================

    // 草地：平涂基色 + 少量规则草簇（不是随机噪点，是「有节奏的小色块」）
    // 尺寸保持 256px：与 TexScale.GrassPeriod=1.667m 配套 → 153.6px/米（基准 160 的容差带内）
    static Px Grass()
    {
        var p = new Px(256, 256);
        var baseCol = C("#7cb342");       // 高饱和草绿
        var lightCol = C("#8bc34a");
        var darkCol = C("#689f38");
        p.FillRect(0, 0, 256, 256, baseCol);
        // 规则草簇：每 32px 一个，错位排列（卡通草地的经典画法）
        for (int gy = 0; gy < 256; gy += 32)
            for (int gx = 0; gx < 256; gx += 32)
            {
                int ox = (gy / 32) % 2 == 0 ? 0 : 16;
                int x = (gx + ox) % 256;
                // 三根小草叶
                p.FillRect(x + 6, gy + 12, 3, 12, lightCol);
                p.FillRect(x + 13, gy + 8, 3, 16, darkCol);
                p.FillRect(x + 20, gy + 14, 3, 10, lightCol);
            }
        // 稀疏大色块做层次（不是噪点）
        p.FillRect(32, 160, 40, 32, darkCol);
        p.FillRect(168, 48, 48, 32, lightCol);
        p.FillRect(96, 208, 56, 24, darkCol);
        return p;
    }

    // 铺装石路：规则方砖 + 硬边缝
    // 尺寸保持 128px：与 TexScale.PathPeriod=0.8m 配套 → 160px/米（正好基准）
    static Px PathTex()
    {
        var p = new Px(128, 128);
        p.FillRect(0, 0, 128, 128, C("#cfc6b4"));   // 缝色（底色）
        Color a = C("#e0d7c4"), b = C("#d6ccb8");
        for (int by = 0; by < 2; by++)
            for (int bx = 0; bx < 2; bx++)
            {
                int ox = by % 2 == 0 ? 0 : 32;
                int x = (bx * 64 + ox) % 128;
                p.FillRect(x + 4, by * 64 + 4, 56, 56, (bx + by) % 2 == 0 ? a : b);
                // 高光边（卡通石板：左上一条亮线）
                p.FillRect(x + 4, by * 64 + 4, 56, 6, C("#ece5d6"));
                // 阴影边
                p.FillRect(x + 4, by * 64 + 54, 56, 6, C("#c4baa6"));
            }
        return p;
    }

    // 建筑立面密度（保留原函数语义）
    public static float FacadeDensity(float wMeters, float hMeters)
        => Mathf.Min(166f, Mathf.Min(2048f / Mathf.Max(0.5f, wMeters), 2048f / Mathf.Max(0.5f, hMeters)));

    // ========================================================================
    // 卡通建筑立面：这是本轮最重要的贴图
    // 上一版画了玻璃反射渐变、雨痕、脏污 —— 卡通渲染下全部去掉。
    // 现在的做法：平涂墙色 + 硬边窗框 + 纯色窗玻璃（两色：天空蓝 / 暖黄亮灯）+ 硬边墙裙。
    // ========================================================================
    static Px Building(string wall, string trim, string plinth, int cols, int rows, bool banner, float lit,
                       float wMeters, float hMeters)
    {
        // 逻辑画布：每层 128 高，每列 128 宽（比上一版的 256 更简洁，卡通不需要那么多细节）
        const int cell = 128;
        float aw = cols * cell, ah = rows * cell + 96f;
        float D = FacadeDensity(wMeters, hMeters);
        var p = new Px(Mathf.Max(8, Mathf.RoundToInt(wMeters * D)), Mathf.Max(8, Mathf.RoundToInt(hMeters * D)));
        float sx = p.W / aw, sy = p.H / ah;
        void Draw(float x, float y, float w2, float h2, Color c) => p.FillRect(x * sx, y * sy, w2 * sx, h2 * sy, c);

        // 墙体：纯平涂（唯一允许的「材质感」是每层一道硬边分格线）
        Draw(0, 0, aw, ah, C(wall));

        // 檐口（顶部横带）+ 墙裙（底部横带）
        Draw(0, 0, aw, 40, C(trim));
        Draw(0, 40, aw, 8, C("rgba(0,0,0,0.22)"));      // 檐下硬投影线
        Draw(0, ah - 56, aw, 56, C(plinth));
        Draw(0, ah - 56, aw, 6, C("rgba(255,255,255,0.20)"));   // 墙裙亮边

        // 楼层分格线（硬边）
        for (int r = 1; r < rows; r++)
        {
            Draw(0, r * cell + 40, aw, 6, C("rgba(0,0,0,0.12)"));
        }

        // 窗户：平涂矩形 + 硬边窗框，玻璃只有两种状态
        var glassDay = C("#a8d8ea");     // 白天：天空蓝（卡通玻璃是纯色，不是反射渐变）
        var glassLit = C("#ffd54f");     // 夜里亮灯：暖黄
        var frameCol = C(trim);
        for (int r = 0; r < rows; r++)
            for (int c2 = 0; c2 < cols; c2++)
            {
                bool isLit = R.Next() < lit;
                float wx = c2 * cell + 28, wy = r * cell + 52;
                float ww = 72, wh = 64;
                // 窗框（外凸硬边）
                Draw(wx - 6, wy - 6, ww + 12, wh + 12, frameCol);
                // 玻璃
                Draw(wx, wy, ww, wh, isLit ? glassLit : glassDay);
                if (!isLit)
                {
                    // 卡通玻璃高光：左上角一个纯色三角/矩形，不做渐变
                    Draw(wx + 4, wy + 4, 24, 20, C("rgba(255,255,255,0.55)"));
                    // 窗棂十字
                    Draw(wx + ww / 2 - 3, wy, 6, wh, frameCol);
                    Draw(wx, wy + wh / 2 - 3, ww, 6, frameCol);
                }
                else
                {
                    // 亮灯窗：暖黄 + 更亮的中心
                    Draw(wx + 8, wy + 8, ww - 16, wh - 16, C("#ffe082"));
                }
                // 窗台投影（硬边一条）
                Draw(wx - 8, wy + wh + 6, ww + 16, 5, C("rgba(0,0,0,0.18)"));
            }

        // 横匾（部分建筑）
        if (banner)
        {
            float by = cell + 16;
            Draw(24, by, aw - 48, 44, C("#e53935"));
            Draw(24, by, aw - 48, 6, C("rgba(255,255,255,0.30)"));
            for (float x = 40; x < aw - 60; x += 64) Draw(x, by + 12, 32, 20, C("#ffeb3b"));
        }
        return p;
    }

    // 门：平涂木色 + 硬边门框
    static Px Door()
    {
        var p = new Px(64, 96);
        p.FillRect(0, 0, 64, 96, C("#f5f0e6"));        // 门框外
        p.FillRect(6, 6, 52, 90, C("#8d6e63"));        // 门框
        p.FillRect(12, 12, 40, 84, C("#5d4037"));      // 门板
        // 门板分格（硬边）
        p.FillRect(16, 18, 32, 26, C("#4e342e"));
        p.FillRect(16, 50, 32, 26, C("#4e342e"));
        // 玻璃气窗
        p.FillRect(16, 18, 32, 10, C("#a8d8ea"));
        // 门把手（金色圆点）
        p.FillCircle(46, 62, 3, C("#ffd54f"));
        return p;
    }

    // 雨棚条纹：高对比双色
    static Px Stripes()
    {
        var p = new Px(32, 32);
        var a = C("#fff8e1"); var b = C("#ef5350");
        for (int x = 0; x < 32; x += 8)
            p.FillRect(x, 0, 4, 32, a);
        for (int x = 4; x < 32; x += 8)
            p.FillRect(x, 0, 4, 32, b);
        return p;
    }

    // 参数化双色条纹（遮阳棚/布幔/摊位顶棚共用；WorldBuilder 按店铺 id 取色）
    // 卡通渲染的条纹要「粗、纯、硬边」——上一版靠 R 随机抖动，这里改成严格等宽。
    static readonly Dictionary<string, Texture2D> StripeCache = new Dictionary<string, Texture2D>();

    public static Texture2D Stripes2(string colorA, string colorB)
    {
        string key = colorA + "|" + colorB;
        if (StripeCache.TryGetValue(key, out var hit)) return hit;
        var p = new Px(32, 32);
        var a = C(colorA); var b = C(colorB);
        for (int x = 0; x < 32; x += 8)
        {
            p.FillRect(x, 0, 4, 32, a);
            p.FillRect(x + 4, 0, 4, 32, b);
        }
        var t = From(p);
        t.filterMode = FilterMode.Trilinear;
        t.anisoLevel = 9;
        t.wrapMode = TextureWrapMode.Repeat;
        StripeCache[key] = t;
        return t;
    }

    // 自发光贴图：由源贴图按亮度阈值提纯（夜间窗户/招牌发光用）
    static readonly Dictionary<Texture2D, Texture2D> EmCache = new Dictionary<Texture2D, Texture2D>();

    public static Texture2D EmissionFrom(Texture2D src, int step = 4)
    {
        if (src == null) return null;
        if (EmCache.TryGetValue(src, out var hit)) return hit;
        var t = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        var px = new Color[src.width * src.height];
        for (int y = 0; y < src.height; y++)
            for (int x = 0; x < src.width; x++)
            {
                var c = src.GetPixel(x, y);
                float l = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                // 卡通渲染：阈值更硬（亮部才发光），避免整面墙微微泛光把画面糊掉
                float k = l > 0.72f ? 1f : 0f;
                px[y * src.width + x] = new Color(c.r * k, c.g * k, c.b * k, 1f);
            }
        t.SetPixels(px);
        t.Apply(false, false);
        t.filterMode = src.filterMode;
        t.wrapMode = src.wrapMode;
        EmCache[src] = t;
        return t;
    }

    // 花：平涂花朵（卡通花是纯色圆点）
    static Px Flower()
    {
        // 1.7m → 256px ≈ 150.6px/米（基准容差内）
        var p = new Px(256, 256);
        var petals = new[] { C("#ff8a80"), C("#ffd54f"), C("#f48fb1"), C("#fff59d"), C("#b39ddb") };
        for (int i = 0; i < 36; i++)
        {
            float cx = 16 + (i % 6) * 40, cy = 16 + (i / 6) * 40;
            p.FillRect(cx - 3, cy + 8, 6, 24, C("#66bb6a"));      // 茎
            p.FillCircle(cx, cy, 12, petals[i % 5]);               // 花瓣
            p.FillCircle(cx, cy, 5, C("#fffde7"));                 // 花心
        }
        return p;
    }

    static Px Dirt()
    {
        // 4.8m → 768px = 160px/米
        var p = new Px(768, 768);
        p.FillRect(0, 0, 768, 768, C("#a1887f"));
        for (int i = 0; i < 120; i++)
            p.FillRect((i * 137) % 740, (i * 211) % 740, 48, 32, C("#8d6e63"));
        for (int i = 0; i < 60; i++)
            p.FillRect((i * 97) % 750, (i * 173) % 750, 32, 24, C("#bcaaa4"));
        return p;
    }

    // 树叶：平涂绿 + 明暗两色块（卡通树叶是色块不是噪点）
    static Px Leaves()
    {
        var p = new Px(32, 32);
        p.FillRect(0, 0, 32, 32, C("#66bb6a"));
        p.FillRect(0, 0, 16, 16, C("#81c784"));
        p.FillRect(16, 16, 16, 16, C("#4caf50"));
        p.FillRect(8, 4, 8, 8, C("#a5d6a7"));
        return p;
    }

    static Px Concrete()
    {
        var p = new Px(32, 32);
        p.FillRect(0, 0, 32, 32, C("#cfd8dc"));
        p.FillRect(0, 0, 32, 2, C("#b0bec5"));
        p.FillRect(0, 16, 32, 2, C("#b0bec5"));
        return p;
    }

    static Px RainStreak()
    {
        var p = new Px(8, 32);
        p.FillRect(3, 0, 2, 24, C("rgba(200,230,255,0.75)"));
        return p;
    }

    static Px SnowFlake()
    {
        var p = new Px(8, 8);
        p.FillCircle(4, 4, 2.5f, C("#ffffff"));
        return p;
    }

    static Px SnowGround()
    {
        var p = new Px(32, 32);
        p.FillRect(0, 0, 32, 32, C("#f5f5f5"));
        p.FillRect(4, 4, 10, 8, C("#ffffff"));
        p.FillRect(18, 16, 12, 10, C("#e8eaf6"));
        return p;
    }

    // 河水：平涂蓝 + 白色波纹硬边（卡通水是纯色 + 几条白线）
    static Px Water()
    {
        var p = new Px(64, 64);
        p.FillRect(0, 0, 64, 64, C("#4fc3f7"));
        p.FillRect(0, 0, 64, 8, C("#81d4fa"));
        // 白色波纹（规则的横向短线）
        for (int i = 0; i < 4; i++)
        {
            int y = 12 + i * 14;
            p.FillRect(6 + (i % 2) * 10, y, 14, 3, C("rgba(255,255,255,0.70)"));
            p.FillRect(30 + (i % 2) * 6, y + 4, 10, 3, C("rgba(255,255,255,0.50)"));
        }
        return p;
    }

    static Px BridgeStone()
    {
        var p = new Px(64, 64);
        p.FillRect(0, 0, 64, 64, C("#bdbdbd"));
        for (int y = 0; y < 64; y += 16)
        {
            p.FillRect(0, y, 64, 2, C("#9e9e9e"));
            int ox = (y / 16) % 2 == 0 ? 0 : 16;
            for (int x = ox; x < 64; x += 32)
                p.FillRect(x, y, 2, 16, C("#9e9e9e"));
        }
        return p;
    }

    static Px Wood()
    {
        var p = new Px(64, 64);
        p.FillRect(0, 0, 64, 64, C("#a1887f"));
        for (int y = 0; y < 64; y += 16)
        {
            p.FillRect(0, y, 64, 2, C("#795548"));
            // 木纹：两条短横线（规则，不是噪点）
            p.FillRect(8, y + 6, 16, 2, C("rgba(121,85,72,0.45)"));
            p.FillRect(36, y + 10, 20, 2, C("rgba(121,85,72,0.35)"));
        }
        return p;
    }

    // 钟面：白盘 + 黑色刻度（卡通钟面要干净）
    static Px ClockFace()
    {
        var p = new Px(128, 128);
        p.FillRect(0, 0, 128, 128, C("#fafafa"));
        p.StrokeCircle(64, 64, 60, 3, C("#212121"));
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6f;
            float x = 64 + Mathf.Sin(a) * 50, y = 64 - Mathf.Cos(a) * 50;
            p.FillCircle(x, y, i % 3 == 0 ? 4f : 2.5f, C("#212121"));
        }
        // 时针分针（硬边矩形）
        p.FillRect(62, 34, 4, 32, C("#212121"));
        p.FillRect(62, 62, 26, 4, C("#212121"));
        p.FillCircle(64, 64, 4, C("#e53935"));
        return p;
    }

    static Px Track()
    {
        var p = new Px(32, 32);
        p.FillRect(0, 0, 32, 32, C("#ef6c5a"));
        p.FillRect(0, 0, 32, 2, C("rgba(255,255,255,0.25)"));
        return p;
    }

    // 池塘：平涂 + 波纹
    static Px Pond()
    {
        var p = new Px(64, 64);
        p.FillRect(0, 0, 64, 64, C("#4dd0e1"));
        p.FillRect(0, 0, 64, 12, C("#80deea"));
        for (int i = 0; i < 3; i++)
            p.FillRect(10 + i * 8, 20 + i * 14, 16, 3, C("rgba(255,255,255,0.55)"));
        return p;
    }

    // 卡通天空：平涂渐变（2~3 个色带，不是大气散射）
    static Px SkyDay()
    {
        var p = new Px(64, 128);
        var top = C("#4fc3f7");
        var mid = C("#81d4fa");
        var bot = C("#b3e5fc");
        p.FillRect(0, 0, 64, 44, top);
        p.FillRect(0, 44, 64, 44, mid);
        p.FillRect(0, 88, 64, 40, bot);
        // 卡通云：几个纯白圆团叠成的云（硬边）
        void Cloud(float cx, float cy, float s)
        {
            p.FillCircle(cx, cy, 7 * s, C("#ffffff"));
            p.FillCircle(cx + 9 * s, cy - 3 * s, 9 * s, C("#ffffff"));
            p.FillCircle(cx + 20 * s, cy + 1 * s, 7 * s, C("#ffffff"));
            p.FillRect(cx - 6 * s, cy + 1 * s, 32 * s, 7 * s, C("#ffffff"));
        }
        Cloud(8, 30, 0.9f);
        Cloud(38, 62, 1.2f);
        Cloud(14, 96, 0.8f);
        return p;
    }

    static Px SkyNight()
    {
        var p = new Px(64, 128);
        p.FillRect(0, 0, 64, 128, C("#1a237e"));
        p.FillRect(0, 0, 64, 40, C("#0d1b4c"));
        p.FillRect(0, 90, 64, 38, C("#283593"));
        // 星星：纯白圆点（规则分布 + 少量大星）
        for (int i = 0; i < 30; i++)
        {
            float x = (i * 23 + 7) % 64, y = (i * 37 + 11) % 128;
            p.FillCircle(x, y, i % 7 == 0 ? 1.6f : 1f, C("#ffffff"));
        }
        // 月亮
        p.FillCircle(48, 24, 8, C("#fff9c4"));
        p.FillCircle(52, 21, 7, C("#1a237e"));
        return p;
    }

    static Px LampGlow()
    {
        var p = new Px(32, 32);
        for (int r = 15; r >= 1; r--)
        {
            float a = 0.06f * (16 - r);
            p.FillCircle(16, 16, r, new Color(1f, 0.95f, 0.7f, a));
        }
        return p;
    }

    static Px Roof(string baseCol)
    {
        // 瓦屋顶：平涂基色 + 规则的瓦垄横线（卡通瓦片是规律的条纹）
        var p = new Px(32, 32);
        var b = C(baseCol);
        p.FillRect(0, 0, 32, 32, b);
        var dark = new Color(b.r * 0.75f, b.g * 0.75f, b.b * 0.75f, 1f);
        var light = new Color(Mathf.Min(1f, b.r * 1.25f), Mathf.Min(1f, b.g * 1.25f), Mathf.Min(1f, b.b * 1.25f), 1f);
        for (int y = 0; y < 32; y += 8)
        {
            p.FillRect(0, y, 32, 2, dark);
            p.FillRect(0, y + 2, 32, 2, light);
        }
        return p;
    }

    static Px Villa()
    {
        var p = new Px(32, 32);
        p.FillRect(0, 0, 32, 32, C("#e8d5b7"));
        p.FillRect(0, 0, 32, 3, C("#8d6e63"));
        return p;
    }

    static Px Court(string kind)
    {
        var p = new Px(64, 64);
        p.FillRect(0, 0, 64, 64, kind == "basket" ? C("#7e57c2") : C("#66bb6a"));
        p.StrokeRect(2, 2, 60, 60, 2, C("#ffffff"));
        p.FillRect(30, 0, 4, 64, C("#ffffff"));
        return p;
    }

    static Px Road(string kind)
    {
        if (kind == "mainstone")
        {
            // 主街石板：256px / 1.6m = 160px/米（4×4 大石板，单块 0.4m）
            var p = new Px(256, 256);
            p.FillRect(0, 0, 256, 256, C("#bdb2a0"));
            Color a = C("#cec3b0"), b = C("#c5b9a6");
            for (int by = 0; by < 4; by++)
                for (int bx = 0; bx < 4; bx++)
                {
                    int ox = by % 2 == 0 ? 0 : 32;
                    int x = (bx * 64 + ox) % 256;
                    p.FillRect(x + 4, by * 64 + 4, 56, 56, (bx + by) % 2 == 0 ? a : b);
                    // 卡通石板：左上亮边 / 右下暗边（硬边，不做渐变）
                    p.FillRect(x + 4, by * 64 + 4, 56, 5, C("#ece5d6"));
                    p.FillRect(x + 4, by * 64 + 55, 56, 5, C("#c4baa6"));
                }
            return p;
        }
        else
        {
            // 青砖：128px / 0.8m = 160px/米（4×4 小砖，单块 0.2m）
            var p = new Px(128, 128);
            p.FillRect(0, 0, 128, 128, C("#a8a396"));
            Color a = C("#bab5a8"), b = C("#b0ab9e");
            for (int by = 0; by < 4; by++)
                for (int bx = 0; bx < 4; bx++)
                {
                    int ox = by % 2 == 0 ? 0 : 16;
                    int x = (bx * 32 + ox) % 128;
                    p.FillRect(x + 2, by * 32 + 2, 28, 28, (bx + by) % 2 == 0 ? a : b);
                }
            return p;
        }
    }

    static Px RoadBand(bool vertical)
    {
        // 中线石带：贴图 96×160px，覆盖 0.6×1.0m → 两个方向都是 160px/米
        // 横带（roadband_h）把它转 90° 用，所以 TexScale 里两个登记项分别是 (0.6,1.0) 与 (1.0,0.6)
        var p = new Px(96, 160);
        p.FillRect(0, 0, 96, 160, C("#d7cfc0"));
        if (vertical)
        {
            p.FillRect(0, 0, 12, 160, C("#c0b7a6"));
            p.FillRect(84, 0, 12, 160, C("#c0b7a6"));
        }
        else
        {
            p.FillRect(0, 0, 96, 12, C("#c0b7a6"));
            p.FillRect(0, 148, 96, 12, C("#c0b7a6"));
        }
        return p;
    }

    // ========================================================================
    // 室内贴图（卡通化）
    // ========================================================================
    static Px FloorWood()
    {
        var p = new Px(64, 64);
        p.FillRect(0, 0, 64, 64, C("#c69c6d"));
        for (int y = 0; y < 64; y += 16)
        {
            p.FillRect(0, y, 64, 2, C("#a0784f"));
            int ox = (y / 16) % 2 == 0 ? 0 : 32;
            p.FillRect(ox, y, 2, 16, C("#a0784f"));
        }
        return p;
    }

    static Px WallPaper()
    {
        var p = new Px(32, 32);
        p.FillRect(0, 0, 32, 32, C("#fff3e0"));
        // 卡通壁纸：规则小花点
        for (int y = 8; y < 32; y += 16)
            for (int x = 8; x < 32; x += 16)
                p.FillCircle(x, y, 2, C("#ffcc80"));
        return p;
    }

    static Px FloorTile()
    {
        var p = new Px(32, 32);
        p.FillRect(0, 0, 32, 32, C("#e0e0e0"));
        p.FillRect(0, 0, 16, 16, C("#eeeeee"));
        p.FillRect(16, 16, 16, 16, C("#eeeeee"));
        p.FillRect(0, 15, 32, 2, C("#bdbdbd"));
        p.FillRect(15, 0, 2, 32, C("#bdbdbd"));
        return p;
    }

    static Px Ceiling()
    {
        var p = new Px(32, 32);
        p.FillRect(0, 0, 32, 32, C("#fafafa"));
        p.FillRect(0, 15, 32, 2, C("#e0e0e0"));
        p.FillRect(15, 0, 2, 32, C("#e0e0e0"));
        return p;
    }

    static Px Blackboard()
    {
        var p = new Px(64, 32);
        p.FillRect(0, 0, 64, 32, C("#37474f"));
        p.FillRect(0, 0, 64, 3, C("#8d6e63"));
        // 粉笔字迹：白色短横线（卡通黑板）
        p.FillRect(6, 10, 20, 2, C("rgba(255,255,255,0.7)"));
        p.FillRect(6, 16, 30, 2, C("rgba(255,255,255,0.5)"));
        p.FillRect(6, 22, 14, 2, C("rgba(255,255,255,0.6)"));
        return p;
    }

    static Px WallPaperBlue()
    {
        var p = new Px(32, 32);
        p.FillRect(0, 0, 32, 32, C("#e3f2fd"));
        for (int y = 8; y < 32; y += 16)
            for (int x = 8; x < 32; x += 16)
                p.FillCircle(x, y, 2, C("#90caf9"));
        return p;
    }

    static Px WallPaperWarm()
    {
        var p = new Px(32, 32);
        p.FillRect(0, 0, 32, 32, C("#fff8e1"));
        for (int y = 8; y < 32; y += 16)
            for (int x = 8; x < 32; x += 16)
                p.FillCircle(x, y, 2, C("#ffcc80"));
        return p;
    }

    static Px WallTile()
    {
        var p = new Px(32, 32);
        p.FillRect(0, 0, 32, 32, C("#eceff1"));
        p.FillRect(0, 0, 16, 16, C("#ffffff"));
        p.FillRect(16, 16, 16, 16, C("#ffffff"));
        p.FillRect(0, 15, 32, 2, C("#b0bec5"));
        p.FillRect(15, 0, 2, 32, C("#b0bec5"));
        return p;
    }

    static Px WoodPanel()
    {
        var p = new Px(32, 32);
        p.FillRect(0, 0, 32, 32, C("#8d6e63"));
        p.FillRect(2, 2, 28, 12, C("#a1887f"));
        p.FillRect(2, 18, 28, 12, C("#a1887f"));
        return p;
    }

    // ========================================================================
    // 缓存与分发
    // ========================================================================
    static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

    public static Texture2D Get(string name, bool pixel, bool repeat)
    {
        string key = name + (pixel ? "|p" : "|s") + (repeat ? "|r" : "|c");
        if (Cache.TryGetValue(key, out var t)) return t;
        R = NewR();
        Px px = name switch
        {
            "grass" => Grass(), "path" => PathTex(), "flower" => Flower(), "dirt" => Dirt(),
            "leaves" => Leaves(), "bark" => Bark(), "concrete" => Concrete(),
            "rain_streak" => RainStreak(), "snow_flake" => SnowFlake(), "snow_ground" => SnowGround(),
            "water" => Water(), "bridge_stone" => BridgeStone(), "wood" => Wood(), "clock" => ClockFace(),
            "track" => Track(), "pond" => Pond(), "sky_day" => SkyDay(), "sky_night" => SkyNight(),
            "lamp_glow" => LampGlow(), "roof_villa" => Roof("#5d4037"), "villa" => Villa(),
            "door" => Door(), "awning" => Stripes(),
            "court_basket" => Court("basket"), "court_tennis" => Court("tennis"),
            "road_mainstone" => Road("mainstone"), "road_cobble" => Road("cobble"),
            "roadband_v" => RoadBand(true), "roadband_h" => RoadBand(false),
            "in_floor" => FloorWood(), "in_wall" => WallPaper(), "in_tile" => FloorTile(),
            "in_ceil" => Ceiling(), "in_board" => Blackboard(),
            "in_wall2" => WallPaperBlue(), "in_wall3" => WallPaperWarm(),
            "in_walltile" => WallTile(), "in_panel" => WoodPanel(),
            _ => null,
        };
        if (px == null) return null;
        t = From(px);
        t.filterMode = pixel ? FilterMode.Point : FilterMode.Trilinear;
        t.anisoLevel = pixel ? 0 : 9;
        t.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        Cache[key] = t;
        return t;
    }

    static Px Bark()
    {
        var p = new Px(32, 32);
        p.FillRect(0, 0, 32, 32, C("#8d6e63"));
        for (int y = 0; y < 32; y += 8)
            p.FillRect(0, y, 32, 2, C("#6d4c41"));
        p.FillRect(10, 4, 3, 24, C("#6d4c41"));
        p.FillRect(22, 0, 3, 20, C("#6d4c41"));
        return p;
    }

    static WorldData _world;
    static WorldData Wd => _world ?? (_world = CampusData.Build());

    public static void SetWorld(WorldData w)
    {
        if (w != null && !ReferenceEquals(w, _world))
        {
            _world = w;
            Cache.Clear();
        }
    }

    public static Texture2D ByName(string name, bool pixel, bool repeat)
    {
        // 招牌（含中文，无法程序化重绘）：加载 PNG 后 2× 最近邻放大
        if (name.StartsWith("sign_"))
        {
            var src = Resources.Load<Texture2D>("tex/" + name);
            if (src == null) return null;
            if (!src.isReadable)
            {
                Debug.LogError($"[TextureLibrary] 招牌贴图不可读（{name}）：请跑「Campus/1b. 重设贴图导入（可读）」后重新构建");
                return src;
            }
            var up = new Texture2D(src.width * 2, src.height * 2, TextureFormat.RGBA32, false);
            for (int y = 0; y < src.height; y++)
                for (int x = 0; x < src.width; x++)
                {
                    var c = src.GetPixel(x, y);
                    up.SetPixel(x * 2, y * 2, c); up.SetPixel(x * 2 + 1, y * 2, c);
                    up.SetPixel(x * 2, y * 2 + 1, c); up.SetPixel(x * 2 + 1, y * 2 + 1, c);
                }
            up.Apply(false, false);
            return up;
        }
        if (name.StartsWith("wall_") || name.StartsWith("wing_"))
        {
            bool wing = name.StartsWith("wing_");
            string rest = name.Substring(5);
            bool night = rest.EndsWith("_night");
            string id = rest.Substring(0, rest.Length - (night ? 6 : 4));
            var b = Array.Find(Wd.BUILDINGS, x => x.id == id);
            if (b != null)
            {
                if (wing)
                    return BuildingWall(b.wall, b.trim, b.plinth, 3, 2, false, night ? 0.55f : 0.05f,
                        4.6f, b.size[1] * 0.72f);
                return BuildingWall(b.wall, b.trim, b.plinth, b.cols, b.rows, b.banner, night ? 0.55f : 0.05f,
                    b.size[0], b.size[1]);
            }
        }
        if (name.StartsWith("roof_"))
        {
            var b = Array.Find(Wd.BUILDINGS, x => "roof_" + x.id == name);
            if (b != null) return RoofTex(b.roof);
        }
        if (name.StartsWith("dist_") && int.TryParse(name.Substring(5), out var idx) && idx >= 0 && idx < Wd.DISTANT_BUILDINGS.Length)
        {
            var d = Wd.DISTANT_BUILDINGS[idx];
            return BuildingWall(d.wall, d.trim, d.plinth, Mathf.Max(2, Mathf.RoundToInt(d.w / 3f)),
                Mathf.Max(2, Mathf.RoundToInt(d.h / 1.7f)), false, 0.16f, d.w, d.h);
        }
        return Get(name, pixel, repeat);
    }

    public static Texture2D BuildingWall(string wall, string trim, string plinth, int cols, int rows, bool banner, float lit,
        float wMeters, float hMeters)
    {
        R = NewR();
        var t = From(Building(wall, trim, plinth, cols, rows, banner, lit, wMeters, hMeters));
        t.filterMode = FilterMode.Trilinear;
        t.anisoLevel = 9;
        t.wrapMode = TextureWrapMode.Repeat;
        return t;
    }

    public static Texture2D RoofTex(string col)
    {
        R = NewR();
        var t = From(Roof(col));
        t.filterMode = FilterMode.Trilinear;
        t.anisoLevel = 9;
        t.wrapMode = TextureWrapMode.Repeat;
        return t;
    }

    public static void DumpAll(string dir)
    {
        System.IO.Directory.CreateDirectory(dir);
        foreach (var n in new[] { "grass", "path", "flower", "dirt", "leaves", "bark", "concrete", "rain_streak", "snow_flake", "snow_ground", "track", "pond", "sky_day", "sky_night", "lamp_glow", "villa", "door", "awning", "court_basket", "court_tennis", "road_mainstone", "road_cobble", "roadband_v", "roadband_h", "water", "bridge_stone", "wood", "clock" })
        {
            var t = Get(n, true, true);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, n + ".png"), t.EncodeToPNG());
        }
        var rv = RoofTex("#5d4037");
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "roof_villa.png"), rv.EncodeToPNG());
        foreach (var id in new[] { "teach", "gym", "kiosk" })
        {
            var b = Array.Find(Wd.BUILDINGS, x => x.id == id);
            if (b != null)
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "wall_" + id + "_day.png"),
                    BuildingWall(b.wall, b.trim, b.plinth, b.cols, b.rows, b.banner, 0.05f, b.size[0], b.size[1]).EncodeToPNG());
        }
        Debug.Log("[TextureLibrary] 已导出对拍贴图 -> " + dir);
    }
}
