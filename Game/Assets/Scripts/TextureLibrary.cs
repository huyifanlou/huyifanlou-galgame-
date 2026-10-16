using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 程序化贴图库（C# 权威版，2026-10-16 由 tools/source/textures.js 整体移植）
// - 与 JS 版同一随机种子（mulberry32(20261015)）与同一绘制流程 → 像素级一致（Editor 侧对拍验证）
// - 带中文文字的招牌贴图保留 PNG（canvas fillText 无法直接移植到像素缓冲）
public static class TextureLibrary
{
    // ── mulberry32（与 JS 位级一致：uint 回绕 + 低 32 位乘法）──
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

    // ── 像素画布：以缓冲实现 Canvas2D 常用原语（source-over alpha 混合）──
    class Px
    {
        public readonly int W, H;
        public readonly Color[] Buf;
        public Px(int w, int h) { W = w; H = h; Buf = new Color[w * h]; for (int i = 0; i < Buf.Length; i++) Buf[i] = new Color(0, 0, 0, 0); }

        public void Set(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            int i = y * W + x;
            if (c.a >= 0.999f) { Buf[i] = c; return; }
            var d = Buf[i];
            float a = c.a + d.a * (1 - c.a);
            if (a <= 0.0001f) { Buf[i] = new Color(0, 0, 0, 0); return; }
            Buf[i] = new Color((c.r * c.a + d.r * d.a * (1 - c.a)) / a, (c.g * c.a + d.g * d.a * (1 - c.a)) / a,
                (c.b * c.a + d.b * d.a * (1 - c.a)) / a, a);
        }

        public void FillRect(int x, int y, int w, int h, Color c) => FillRect((float)x, y, w, h, c);

        // 支持浮点坐标（与 canvas fillRect 一致：按覆盖率做抗锯齿混合）
        public void FillRect(float x, float y, float w, float h, Color c)
        {
            int x0 = Mathf.FloorToInt(x), x1 = Mathf.FloorToInt(x + w);
            int y0 = Mathf.FloorToInt(y), y1 = Mathf.FloorToInt(y + h);
            for (int yy = y0; yy < y1; yy++)
                for (int xx = x0; xx < x1; xx++)
                {
                    float cov = (Mathf.Min(x + w, xx + 1) - Mathf.Max(x, xx)) * (Mathf.Min(y + h, yy + 1) - Mathf.Max(y, yy));
                    if (cov <= 0.001f) continue;
                    if (cov >= 0.999f || c.a < 0.999f) { var cc = c; cc.a = c.a * Mathf.Min(1f, cov); Set(xx, yy, cc); }
                    else { var cc = c; cc.a = c.a * cov; Set(xx, yy, cc); }
                }
        }

        public void FillCircle(float cx, float cy, float r, Color c)
        {
            int x0 = Mathf.FloorToInt(cx - r), x1 = Mathf.CeilToInt(cx + r);
            int y0 = Mathf.FloorToInt(cy - r), y1 = Mathf.CeilToInt(cy + r);
            for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                if (dx * dx + dy * dy <= r * r) Set(x, y, c);
            }
        }

        public static bool InRound(float px, float py, float x, float y, float w, float h, float r)
        {
            if (px < x || py < y || px > x + w || py > y + h) return false;
            float rx = Mathf.Max(x + r, Mathf.Min(px, x + w - r));
            float ry = Mathf.Max(y + r, Mathf.Min(py, y + h - r));
            float dx = px - rx, dy = py - ry;
            // 在圆角半径内：到最近角心距离 ≤ r（角心到边为直段，天然满足）
            if (px >= x + r && px <= x + w - r) return true;
            if (py >= y + r && py <= y + h - r) return true;
            return dx * dx + dy * dy <= r * r + 0.0001f;
        }

        public void FillRound(float x, float y, float w, float h, float r, Color c)
        {
            for (int yy = Mathf.FloorToInt(y); yy <= Mathf.CeilToInt(y + h); yy++)
                for (int xx = Mathf.FloorToInt(x); xx <= Mathf.CeilToInt(x + w); xx++)
                    if (InRound(xx + 0.5f, yy + 0.5f, x, y, w, h, r)) Set(xx, yy, c);
        }

        public void StrokeRound(float x, float y, float w, float h, float r, float lw, Color c)
        {
            for (int yy = Mathf.FloorToInt(y - lw); yy <= Mathf.CeilToInt(y + h + lw); yy++)
                for (int xx = Mathf.FloorToInt(x - lw); xx <= Mathf.CeilToInt(x + w + lw); xx++)
                {
                    float px = xx + 0.5f, py = yy + 0.5f;
                    if (!InRound(px, py, x - lw / 2, y - lw / 2, w + lw, h + lw, r + lw / 2)) continue;
                    if (InRound(px, py, x + lw / 2, y + lw / 2, w - lw, h - lw, Mathf.Max(1, r - lw / 2))) continue;
                    Set(xx, yy, c);
                }
        }

        public void StrokeRect(float x, float y, float w, float h, float lw, Color c) => StrokeRound(x, y, w, h, 0, lw, c);

        public void StrokeCircle(float cx, float cy, float r, float lw, Color c)
        {
            int x0 = Mathf.FloorToInt(cx - r - lw), x1 = Mathf.CeilToInt(cx + r + lw);
            int y0 = Mathf.FloorToInt(cy - r - lw), y1 = Mathf.CeilToInt(cy + r + lw);
            for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
            {
                float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                if (Mathf.Abs(d - r) <= lw / 2f) Set(x, y, c);
            }
        }
    }

    // ── 颜色解析（支持 #rrggbb 与 rgba(r,g,b,a)）──
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

    // ── 贴图生成 ──
    static Texture2D From(Px p)
    {
        var t = new Texture2D(p.W, p.H, TextureFormat.RGBA32, false);
        // 注意：Unity 的 SetPixels 行序自下而上，而画布缓冲行 0 是顶行（与 canvas 一致）→ 必须翻转
        var flipped = new Color[p.Buf.Length];
        for (int y = 0; y < p.H; y++)
            Array.Copy(p.Buf, y * p.W, flipped, (p.H - 1 - y) * p.W, p.W);
        t.SetPixels(flipped);
        t.Apply(false, false);
        return t;
    }

    static Px Grass()
    {
        var p = new Px(128, 128);
        for (int y = 0; y < 128; y += 4) for (int x = 0; x < 128; x += 4)
            p.FillRect(x, y, 4, 4, C((x + y) % 8 == 0 ? "#54904b" : "#4a8743"));
        var cols = new[] { "#3f7239", "#5da054", "#437c3c", "#61a85a" };
        for (int i = 0; i < 100; i++)
        {
            var c = C(cols[R.Int(4)]);
            int rx = R.Int(122), ry = R.Int(122), w = R.Int(2) == 1 ? 4 : 6, h = R.Int(2) == 1 ? 6 : 4;
            p.FillRect(rx, ry, w, h, c);
        }
        var pcols = new[] { "#48853f", "#5c9c53", "#437c3c" };
        for (int i = 0; i < 18; i++)
        {
            float a = 0.35f + (float)R.Next() * 0.2f;
            var c = C(pcols[R.Int(3)]); c.a = a;
            p.FillRect(R.Int(112), R.Int(112), 8 + R.Int(7), 8 + R.Int(7), c);
        }
        // 细化：2px 细颗粒碎草与小石子（地面细节层次，与草地同密度）
        var fine = new[] { "#3f7239", "#5da054", "#61a85a", "#437c3c" };
        for (int i = 0; i < 260; i++)
            p.FillRect(R.Int(126), R.Int(126), 2, R.Int(2) == 0 ? 2 : 4, C(fine[R.Int(4)]));
        for (int i = 0; i < 26; i++)
            p.FillRect(R.Int(124), R.Int(124), 2, 2, C(R.Int(2) == 0 ? "#6fae4b" : "#356e37"));
        for (int i = 0; i < 10; i++)
            p.FillRect(R.Int(124), R.Int(124), 2, 2, C("rgba(210,205,190,0.35)"));
        return p;
    }

    static Px PathTex()
    {
        var p = new Px(64, 64);
        p.FillRect(0, 0, 64, 64, C("#a49c8d"));
        var cols = new[] { "#c1b9ac", "#b8b0a3", "#bdb4a6", "#b3ab9d" };
        for (int by = 0; by < 2; by++) for (int bx = 0; bx < 2; bx++)
        {
            p.FillRect(bx * 32 + 2, by * 32 + 2, 28, 28, C(cols[R.Int(4)]));
            for (int i = 0; i < 10; i++) p.FillRect(bx * 32 + 2 + R.Int(28), by * 32 + 2 + R.Int(28), 4, 2, C("rgba(122,114,102,0.45)"));
            for (int i = 0; i < 6; i++) p.FillRect(bx * 32 + 2 + R.Int(28), by * 32 + 2 + R.Int(28), 2, 2, C("rgba(230,226,216,0.5)"));
        }
        return p;
    }

    static Px Building(string wall, string trim, string plinth, int cols, int rows, bool banner, float lit)
    {
        const int cell = 64;   // 2× 密度（原 32）：立面约 40px/世界单位，与草地同密度
        int W = cols * cell, H = rows * cell + 80;
        var p = new Px(W, H);
        p.FillRect(0, 0, W, H, C(wall));
        int noise = (int)Math.Floor((W * (double)H) / 96);
        for (int i = 0; i < noise; i++)
        {
            p.FillRect(R.Int(W), R.Int(H), 12, 6, C("rgba(0,0,0,0.05)"));
            p.FillRect(R.Int(W), R.Int(H), 6, 6, C("rgba(255,255,255,0.05)"));
            p.FillRect(R.Int(W), R.Int(H), 2, 2, C(R.Int(2) == 0 ? "rgba(0,0,0,0.06)" : "rgba(255,255,255,0.05)"));
        }
        // 细砖缝（新细节层次）
        for (int y = 16; y < H; y += 16) p.FillRect(0, y, W, 1, C("rgba(0,0,0,0.035)"));
        p.FillRect(0, 0, W, 16, C(trim));
        p.FillRect(0, 16, W, 6, C("rgba(0,0,0,0.12)"));
        for (int r = 1; r < rows; r++)
        {
            p.FillRect(0, r * cell + 24, W, 14, C(trim));
            p.FillRect(0, r * cell + 38, W, 6, C("rgba(0,0,0,0.15)"));
        }
        p.FillRect(0, H - 48, W, 48, C(plinth));
        p.FillRect(0, H - 18, W, 18, C("rgba(0,0,0,0.18)"));
        p.FillRect(0, H - 12, W, 4, C("rgba(255,255,255,0.06)"));
        for (int r = 0; r < rows; r++) for (int c2 = 0; c2 < cols; c2++)
        {
            bool isLit = R.Next() < lit;
            int kind = R.Int(10);
            int wx = c2 * cell + 14, wy = r * cell + 20;
            p.FillRect(wx - 6, wy - 6, 52, 52, C(trim));
            p.FillRect(wx, wy, 40, 40, C(isLit ? "#ffd98a" : "#2c3548"));
            if (isLit) p.FillRect(wx, wy, 40, 6, C("#ffe9b8"));
            else
            {
                if (R.Next() < 0.35)
                {
                    p.FillRect(wx + 4, wy + 4, 14, 6, C("rgba(170,195,225,0.30)"));
                    p.FillRect(wx + 8, wy + 10, 10, 6, C("rgba(170,195,225,0.30)"));
                }
                // 十字窗棂（新细节）
                p.FillRect(wx + 19, wy, 2, 40, C(trim));
                p.FillRect(wx, wy + 19, 40, 2, C(trim));
            }
            if (kind <= 1 && !isLit) p.FillRect(wx, wy, 40, 14, C("#a08b80"));
            if (kind == 2 && !isLit)
            {
                p.FillRect(wx, wy + 42, 40, 10, C("#3f6e35"));
                for (int i = 0; i < 5; i++) p.FillRect(wx + 4 + i * 8, wy + 38, 4, 6, C("#4a8743"));
            }
            p.FillRect(wx, wy + 34, 40, 6, C("rgba(0,0,0,0.3)"));
            p.FillRect(wx, wy + 52, 40, 6, C("rgba(0,0,0,0.22)"));
            p.FillRect(wx, wy + 46, 40, 4, C("rgba(255,255,255,0.05)"));
            if (kind == 3)
            {
                p.FillRect(wx + 6, wy + 52, 26, 20, C("#9aa2ac"));
                for (int i = 0; i < 3; i++) p.FillRect(wx + 8, wy + 55 + i * 5, 22, 2, C("rgba(0,0,0,0.15)"));
                p.FillRect(wx + 6, wy + 66, 26, 6, C("rgba(0,0,0,0.25)"));
            }
            if (r > 0 && kind == 4 && !isLit)
            {
                p.FillRect(wx - 6, wy + 54, 52, 4, C("#cfc9ba"));
                for (int bxx = 0; bxx < 12; bxx++) p.FillRect(wx - 4 + bxx * 4, wy + 42, 2, 12, C("#b8b0a2"));
            }
        }
        if (banner)
        {
            int by = cell + 88;
            p.FillRect(24, by + 52, W - 48, 6, C("rgba(0,0,0,0.18)"));
            p.FillRect(24, by, W - 48, 52, C("#c0392b"));
            p.FillRect(24, by, W - 48, 3, C("rgba(255,255,255,0.12)"));
            for (int x = 52; x < W - 76; x += 44) p.FillRect(x, by + 14, 26, 24, C("#f5d04e"));
        }
        return p;
    }

    static Px Door()
    {
        var p = new Px(32, 48);
        p.FillRect(0, 0, 32, 48, C("#efe7d8"));
        p.FillRect(3, 3, 26, 45, C("#6b4a34"));
        p.FillRect(6, 7, 20, 12, C("#ffd98a"));
        p.FillRect(15, 7, 2, 12, C("#5a3c2a"));
        p.FillRect(6, 12, 20, 2, C("#5a3c2a"));
        p.FillRect(6, 24, 8, 17, C("#54382a"));
        p.FillRect(18, 24, 8, 17, C("#54382a"));
        p.FillRect(15, 24, 2, 18, C("#3a2519"));
        p.FillRect(11, 34, 2, 2, C("#d8b54a"));
        p.FillRect(19, 34, 2, 2, C("#d8b54a"));
        p.FillRect(3, 45, 26, 3, C("#8a6a4a"));
        return p;
    }

    static Px Stripes()
    {
        var p = new Px(16, 16);
        var a = C("#efe9dd"); var b = C("#c9553f");
        for (int x = 0; x < 16; x++) p.FillRect(x, 0, 1, 16, x % 4 < 2 ? a : b);
        return p;
    }

    static Px Flower()
    {
        var p = new Px(64, 64);
        for (int y = 0; y < 64; y += 4) for (int x = 0; x < 64; x += 4)
            p.FillRect(x, y, 4, 4, C((x + y) % 8 == 0 ? "#54904b" : "#4a8743"));
        var colors = new[] { "#e8748a", "#f2d857", "#f4f2ea", "#c58ae0" };
        for (int i = 0; i < 18; i++)
        {
            int x = 2 + R.Int(56), y = 2 + R.Int(54);
            p.FillRect(x, y, 4, 4, C(colors[R.Int(colors.Length)]));
            p.FillRect(x + 1, y + 4, 2, 2, C("#3f7239"));
        }
        return p;
    }

    static Px Dirt()
    {
        var p = new Px(96, 96);
        var cols = new[] { "#8a6f4f", "#7d6448", "#938060" };
        for (int y = 4; y < 92; y += 2) for (int x = 4; x < 92; x += 2)
        {
            double d = Math.Sqrt((x - 48.0) * (x - 48.0) + (y - 48.0) * (y - 48.0)) + (R.Next() * 7 - 3.5);
            double solid = d < 24 ? 1 : d < 29 ? (R.Next() < 0.5 ? 1 : 0) : d < 34 ? (R.Next() < 0.12 ? 1 : 0) : 0;
            if (solid > 0) p.FillRect(x, y, 2, 2, C(cols[R.Int(3)]));
        }
        return p;
    }

    static Px Track()
    {
        var p = new Px(512, 320);
        void GrassFill(int x0, int y0, int x1, int y1)
        {
            for (int y = y0; y < y1; y += 4) for (int x = x0; x < x1; x += 4)
                p.FillRect(x, y, 4, 4, C((x + y) % 8 == 0 ? "#54904b" : "#4a8743"));
        }
        GrassFill(0, 0, 512, 320);
        p.FillRound(12, 12, 488, 296, 92, C("#b26240"));
        var noise = new[] { "#a8593a", "#bd6f4a", "#9e5335", "#c47a52" };
        for (int i = 0; i < 900; i++)
        {
            int x = R.Int(512), y = R.Int(320);
            if (!Px.InRound(x + 0.5f, y + 0.5f, 12, 12, 488, 296, 92)) continue;
            p.FillRect(x, y, 4, 4, C(noise[R.Int(4)]));
        }
        p.StrokeRound(24, 24, 464, 272, 82, 4, C("rgba(244,240,226,0.9)"));
        p.StrokeRound(40, 40, 432, 240, 70, 4, C("rgba(244,240,226,0.9)"));
        p.FillRound(48, 48, 416, 224, 64, C("#4a8743"));
        for (int y = 48; y < 272; y += 4) for (int x = 48; x < 464; x += 4)
            p.FillRect(x, y, 4, 4, C((x + y) % 8 == 0 ? "#54904b" : "#4a8743"));
        var blades = new[] { "#3f7239", "#5da054", "#437c3c" };
        for (int i = 0; i < 130; i++)
        {
            int x = 48 + R.Int(416), y = 48 + R.Int(224);
            p.FillRect(x, y, R.Int(2) == 1 ? 6 : 4, R.Int(2) == 1 ? 6 : 4, C(blades[R.Int(3)]));
        }
        p.FillRect(254, 52, 4, 216, C("rgba(244,240,226,0.9)"));
        p.StrokeCircle(256, 160, 28, 4, C("rgba(244,240,226,0.9)"));
        return p;
    }

    static Px Pond()
    {
        var p = new Px(256, 256);
        p.FillCircle(128, 128, 128, C("#8d8d94"));
        var stone = new[] { "#9aa0a6", "#83838a", "#a9adb2", "#7b7b82" };
        for (int i = 0; i < 16; i++)
        {
            float a = (i / 16f) * Mathf.PI * 2;
            p.FillRect(Mathf.RoundToInt(118 + Mathf.Cos(a) * 118 - 10), Mathf.RoundToInt(128 + Mathf.Sin(a) * 118 - 10), 20, 20, C(stone[R.Int(4)]));
        }
        for (int i = 0; i < 420; i++)
        {
            float a = (float)R.Next() * Mathf.PI * 2, rr = 108 + (float)R.Next() * 18;
            p.FillRect(Mathf.RoundToInt(128 + Mathf.Cos(a) * rr - 2), Mathf.RoundToInt(128 + Mathf.Sin(a) * rr - 2), 4, 4, C(R.Int(2) == 0 ? "rgba(0,0,0,0.12)" : "rgba(255,255,255,0.10)"));
        }
        p.FillCircle(128, 128, 106, C("#3e6ea6"));
        var wave = new[] { "#4d82bd", "#5a8fc8", "#4878b0" };
        for (int i = 0; i < 60; i++)
        {
            double a = R.Next() * Math.PI * 2, rr = Math.Sqrt(R.Next()) * 98;
            p.FillRect((float)(128 + Math.Cos(a) * rr - 8), (float)(128 + Math.Sin(a) * rr), 12 + R.Int(10), 3, C(wave[R.Int(3)]));
        }
        for (int i = 0; i < 26; i++)
        {
            double a = R.Next() * Math.PI * 2, rr = Math.Sqrt(R.Next()) * 96;
            p.FillRect((float)(128 + Math.Cos(a) * rr), (float)(128 + Math.Sin(a) * rr), 3, 3, C("rgba(190,215,240,0.75)"));
        }
        p.FillCircle(136, 122, 34, C("#4d82bd"));
        for (int i = 0; i < 16; i++) p.FillRect(110 + R.Int(52), 96 + R.Int(52), 3, 2, C("rgba(190,215,240,0.6)"));
        foreach (var (lx, ly, lr) in new[] { (86, 156, 11), (168, 92, 9), (150, 172, 8) })
        {
            p.FillCircle(lx, ly, lr, C("#3f7a37"));
            p.FillRect(lx - 2, ly - lr + 2, 4, lr * 2 - 4, C("#2f5f2a"));
            p.FillRect(lx - lr + 3, ly - lr + 3, 6, 5, C("#5da04f"));
        }
        return p;
    }

    static Px SkyDay()
    {
        var p = new Px(2048, 1024);
        var bands = new[] { "#3f86cf", "#4b93d6", "#599edc", "#68aae2", "#79b9e8", "#8cc6ee", "#9fd2f2", "#b2dcf5", "#c0e3f6", "#cfe8f8" };
        float step = 512f / bands.Length;
        for (int i = 0; i < bands.Length; i++) p.FillRect(0, (int)Math.Floor(i * step), 2048, (int)Math.Ceiling(step) + 1, C(bands[i]));
        p.FillRect(0, 512, 2048, 512, C(bands[bands.Length - 1]));
        var CLOUD = new[] { (3, 0, 10, 3), (1, 2, 16, 3), (0, 4, 20, 3), (2, 6, 14, 3), (6, 8, 7, 2) };
        void DrawCloud(int cx, int cy, int s)
        {
            int S = s * 2;
            foreach (var (x, y, w, h) in CLOUD) p.FillRect(cx + x * S, cy + Mathf.RoundToInt((y + 1.55f) * S) + 2, w * S, Mathf.RoundToInt(h * S * 0.5f), C("#b8cee0"));
            foreach (var (x, y, w, h) in CLOUD) p.FillRect(cx + x * S, cy + (y + 1) * S, w * S, h * S, C("#c9dcee"));
            foreach (var (x, y, w, h) in CLOUD) p.FillRect(cx + x * S, cy + y * S, w * S, h * S, C("#ffffff"));
            foreach (var (x, y, w, h) in CLOUD) p.FillRect(cx + x * S, cy + y * S, w * S, 3, C("#f2f8fd"));
            for (int i = 0; i < 10; i++)
                p.FillRect(cx + (2 + R.Int(16)) * S, cy + 11 * S + R.Int(4), 4, 3, C("rgba(201,220,238,0.85)"));
        }
        for (int i = 0; i < 9; i++)
        {
            int s = 2 + R.Int(3), cx = R.Int(2048), cy = Math.Min(460 + R.Int(40), 504 - 22 * s);
            foreach (int off in new[] { -2048, 0, 2048 }) DrawCloud(cx + off, cy, s);
        }
        for (int i = 0; i < 5; i++)
        {
            int s = 2 + R.Int(2), cx = R.Int(2048), cy = 400 + R.Int(60);
            foreach (int off in new[] { -2048, 0, 2048 }) DrawCloud(cx + off, cy, s);
        }
        return p;
    }

    static Px SkyNight()
    {
        var p = new Px(2048, 1024);
        var bands = new[] { "#04060e", "#060a15", "#080f1e", "#0b1427", "#0f1b31", "#14223c", "#192a47", "#203352" };
        float step = 512f / bands.Length;
        for (int i = 0; i < bands.Length; i++) p.FillRect(0, (int)Math.Floor(i * step), 2048, (int)Math.Ceiling(step) + 1, C(bands[i]));
        p.FillRect(0, 512, 2048, 512, C(bands[bands.Length - 1]));
        var star = new[] { "#ffffff", "#dfe8ff", "#cfe0ff", "#ffeecf" };
        for (int i = 0; i < 380; i++)
        {
            bool big = R.Next() < 0.1;
            p.FillRect(R.Int(2048), 476 + R.Int(36), big ? 2 : 1, big ? 2 : 1, C(star[R.Int(4)]));
        }
        for (int i = 0; i < 220; i++) p.FillRect(R.Int(2048), 400 + R.Int(76), 1, 1, C(star[R.Int(3)]));
        for (int i = 0; i < 12; i++)
        {
            int x = 80 + R.Int(2048 - 160), y = 432 + R.Int(64);
            p.FillRect(x - 1, y, 3, 1, C("#ffffff"));
            p.FillRect(x, y - 1, 1, 3, C("#ffffff"));
            p.FillRect(x, y, 1, 1, C("#eef4ff"));
        }
        int mx = 1490, my = 500, Rr = 20, B = 3;
        for (int yy = -Rr; yy < Rr; yy += B) for (int xx = -Rr; xx < Rr; xx += B)
            if (Mathf.Sqrt((xx + B / 2f) * (xx + B / 2f) + (yy + B / 2f) * (yy + B / 2f)) <= Rr) p.FillRect(mx + xx, my + yy, B, B, C("#eef2df"));
        p.FillRect(mx - 12, my - 8, 9, 9, C("#d3dcc4"));
        p.FillRect(mx + 3, my + 2, 9, 6, C("#d3dcc4"));
        p.FillRect(mx - 3, my + 9, 6, 6, C("#d3dcc4"));
        p.FillRect(mx - 11, my - 7, 3, 3, C("#c2ccb2"));
        p.FillRect(mx + 5, my + 3, 4, 3, C("#c2ccb2"));
        return p;
    }

    static Px LampGlow()
    {
        var p = new Px(64, 64);
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
        {
            double d = Math.Sqrt((x - 31.5) * (x - 31.5) + (y - 31.5) * (y - 31.5)) / 32.0;
            if (d > 1) continue;
            double a = Math.Floor((1 - d) * 7 + 0.5) / 7 * 0.85;   // JS Math.round = floor(x+0.5)
            if (a <= 0.01) continue;
            p.FillRect(x, y, 1, 1, new Color(1f, 214 / 255f, 140 / 255f, (float)a));
        }
        return p;
    }

    static Px Roof(string baseCol)
    {
        var p = new Px(32, 16);
        p.FillRect(0, 0, 32, 16, C(baseCol));
        for (int x = 0; x < 32; x += 8)
        {
            p.FillRect(x, 0, 2, 16, C("rgba(0,0,0,0.22)"));
            p.FillRect(x + 2, 0, 1, 16, C("rgba(255,255,255,0.07)"));
        }
        p.FillRect(0, 7, 32, 1, C("rgba(0,0,0,0.18)"));
        p.FillRect(0, 8, 32, 1, C("rgba(255,255,255,0.06)"));
        return p;
    }

    static Px Villa()
    {
        var p = new Px(256, 192);
        p.FillRect(0, 0, 256, 100, C("#ece5d6"));
        p.FillRect(0, 0, 256, 4, C("#d9d2c2"));
        p.FillRect(0, 92, 256, 8, C("rgba(0,0,0,0.12)"));
        foreach (int wx in new[] { 36, 156 })
        {
            p.FillRect(wx - 4, 26, 68, 60, C("#8a6a48"));
            p.FillRect(wx, 30, 60, 52, C("#f0efe6"));
            for (int i = 1; i < 4; i++) p.FillRect(wx + i * 15, 30, 2, 52, C("#8a6a48"));
            for (int j = 1; j < 3; j++) p.FillRect(wx, 30 + j * 17, 60, 2, C("#8a6a48"));
            p.FillRect(wx, 80, 60, 2, C("rgba(0,0,0,0.15)"));
        }
        var wood = new[] { "#b98d5e", "#ad8153", "#c2955f", "#a87e52" };
        for (int x = 0; x < 256; x += 8)
        {
            p.FillRect(x, 100, 8, 92, C(wood[(x / 8) % 4]));
            p.FillRect(x, 100, 1, 92, C("rgba(0,0,0,0.14)"));
        }
        void Shoji(int sx, int sw, int sy, int sh)
        {
            p.FillRect(sx - 3, sy - 3, sw + 6, sh + 6, C("#6b503a"));
            p.FillRect(sx, sy, sw, sh, C("#f2efe2"));
            for (int i = 1; i < (int)Math.Floor(sw / 14f); i++) p.FillRect(sx + i * 14, sy, 2, sh, C("#7a5a3e"));
            for (int j = 1; j < (int)Math.Floor(sh / 14f); j++) p.FillRect(sx, sy + j * 14, sw, 2, C("#7a5a3e"));
        }
        Shoji(104, 48, 108, 76);
        Shoji(16, 34, 116, 56);
        Shoji(196, 34, 116, 56);
        for (int i = 0; i < 90; i++)
            p.FillRect(R.Int(256), 104 + R.Int(84), 2, 1, C(i % 2 == 0 ? "rgba(0,0,0,0.08)" : "rgba(255,255,255,0.06)"));
        for (int i = 0; i < 60; i++)
            p.FillRect(R.Int(256), R.Int(88), 2, 2, C(i % 2 == 0 ? "rgba(0,0,0,0.04)" : "rgba(255,255,255,0.06)"));
        return p;
    }

    static Px Character()
    {
        const int CW = 52, CH = 76;   // 2× 精度（逻辑格 26×38 的 2 倍像素密度，可画更细细节）
        var p = new Px(CW * 4, CH * 4);   // 4 列：站立 + 3 帧行走周期
        string hair = "#eccb5a", hairD = "#c9a234", hairL = "#f6dd82", skin = "#f6dcae", skinD = "#e0c193";
        string eyeW = "#f8f6ee", eye = "#2b3a58", coat = "#46567f", coatD = "#333f5e", coatL = "#55668f";
        string shirt = "#f4f1e6", tie = "#b23a44", tieD = "#8c2c34", btn = "#d8d4c8", belt = "#2a2d3a", buckle = "#c9c2ae";
        string pants = "#2e3550", pantsD = "#232838", shoe = "#23262e", shoeD = "#3a3f4a", line = "#262031";

        var ops = new List<(int x, int y, int w, int h, string col)>();
        void r(int x, int y, int w, int h, string col) => ops.Add((x * 2, y * 2, w * 2, h * 2, col));   // 逻辑格坐标（自动 ×2）
        void r2(int x, int y, int w, int h, string col) => ops.Add((x, y, w, h, col));                  // 2× 精度细节坐标

        int bodyStart = 0, bobY = 0, bobX = 0;   // 躯干起点 + 行走起伏/前倾

        void Paint(bool side, bool up, int frame)
        {
            ops.Clear();
            // 起伏先算：腿根随身体上下，脚掌始终贴地（逻辑格单位；施加到 op 时换算 2×）
            int bobL = frame == 2 ? -1 : (frame == 1 || frame == 3) ? 2 : 0;
            bobY = bobL * 2;
            bobX = frame >= 1 && side ? 2 : 0;
            int legTop = 21 + bobL;
            int baseH = 8 - bobL;
            if (side)
            {
                // 侧身：腿以髋部为轴摆动——上段贴髋（始终在外套轮廓内）、下段前/后错位、鞋跟下段
                // （此前整条腿平移导致迈步时脱离身体轮廓，看起来"腿和身体分离"）
                int fwd = frame == 1 ? 4 : frame == 2 ? 2 : 0;
                int bwd = frame == 3 ? 4 : frame == 2 ? 2 : 0;
                r(12, legTop, 4, 4, pants);                          // 前腿上段（贴髋）
                r(12 + fwd / 2, legTop + 4, 4, baseH - 4, pants);    // 前腿下段（错位）
                r(14 + fwd - 2, 29, 6, 2, shoe);
                r(8, legTop, 4, 4, pantsD);                          // 后腿上段（贴髋）
                r(8 - bwd / 2, legTop + 4, 4, baseH - 4, pantsD);    // 后腿下段（错位）
                r(6 - bwd, 29, 6, 2, shoeD);
            }
            else
            {
                bool liftA = frame == 1, liftB = frame == 3;
                r(7, legTop, 4, liftA ? baseH - 2 : baseH, pants);
                r(12, legTop, 4, liftB ? baseH - 2 : baseH, pants);
                r(10, legTop, 1, baseH, pantsD);
                r(5, liftA ? 27 : 29, 6, 2, shoe); r(11, liftB ? 27 : 29, 6, 2, shoe);
            }
            bodyStart = ops.Count;
            if (side)
            {
                // 侧身手臂随步伐前后摆动（0 站立 / 1 前迈 / 2 并步 / 3 后摆）
                int sw = frame == 1 ? 3 : frame == 3 ? 0 : 1;
                r(8, 9, 8, 4, coat); r(8, 13, 7, 5, coat); r(8, 18, 6, 3, coat);
                r(8, 9, 1, 12, coatD); r(14, 10 + sw, 3, 8, coat); r(14, 18 + sw, 3, 2, coatD);
                r(14, 20 + sw, 3, 2, skin); r(8, 19, 6, 2, belt);
            }
            else
            {
                r(5, 9, 13, 4, coat); r(6, 13, 11, 5, coat); r(7, 18, 9, 3, coat);
                r(5, 9, 1, 4, coatD); r(6, 13, 1, 5, coatD); r(7, 18, 1, 3, coatD);
                r(17, 9, 1, 4, coatL); r(16, 13, 1, 5, coatL);
                if (up) r(11, 9, 1, 10, coatD);
                else
                {
                    r(10, 9, 4, 5, shirt); r(11, 9, 2, 2, tie); r(11, 11, 2, 5, tie); r(11, 12, 1, 4, tieD);
                    r(9, 9, 1, 3, coatD); r(14, 9, 1, 3, coatD); r(11, 17, 1, 1, btn);
                }
                r(7, 19, 9, 2, belt); r(11, 19, 2, 2, buckle);
                r(3, 9, 2, 8, coat); r(18, 9, 2, 8, coat);
                r(3, 17, 2, 2, coatD); r(18, 17, 2, 2, coatD);
                r(3, 19, 2, 2, skin); r(18, 19, 2, 2, skin);
            }
            r(11, 8, 2, 1, skinD);
            if (up)
            {
                r(8, 0, 8, 2, hair); r(7, 1, 10, 3, hair); r(6, 2, 12, 6, hair); r(6, 8, 12, 1, hairD); r(9, 1, 4, 1, hairL);
            }
            else if (side)
            {
                r(6, 0, 8, 2, hair); r(5, 1, 8, 5, hair); r(4, 3, 4, 5, hair);
                r(11, 3, 6, 5, skin); r(11, 3, 6, 1, hair);
                r(13, 5, 1, 1, eyeW); r(13, 6, 1, 1, eye); r(16, 6, 1, 1, skinD); r(13, 7, 2, 1, skinD);
                r(6, 1, 3, 1, hairL);
            }
            else
            {
                r(8, 0, 8, 2, hair); r(7, 1, 10, 2, hair); r(6, 2, 12, 4, hair);
                r(6, 5, 2, 3, hair); r(16, 5, 2, 3, hair); r(9, 1, 4, 1, hairL);
                r(8, 4, 8, 5, skin); r(8, 4, 8, 1, hair);
                r(9, 4, 1, 1, hairD); r(11, 4, 1, 1, hairD); r(13, 4, 1, 1, hairD); r(15, 4, 1, 1, hairD);
                r(9, 5, 2, 1, hairD); r(13, 5, 2, 1, hairD);
                r(9, 6, 2, 2, eyeW); r(13, 6, 2, 2, eyeW);
                r(10, 6, 1, 2, eye); r(13, 6, 1, 2, eye);
                r(11, 7, 2, 1, skinD); r(11, 8, 2, 1, skinD);
            }
            // —— 2× 精度细节：眼神高光 / 发丝 / 衣缝纽扣 / 嘴 / 鞋底 ——
            if (up) { }
            else if (side)
            {
                r2(26, 12, 1, 1, "#ffffff");
                r2(12, 4, 1, 6, hairD); r2(8, 2, 4, 1, hairL);
                r2(30, 26, 1, 8, coatD);
            }
            else
            {
                r2(20, 12, 1, 1, "#ffffff"); r2(26, 12, 1, 1, "#ffffff");
                r2(18, 4, 1, 6, hairD); r2(28, 3, 1, 7, hairD); r2(20, 2, 3, 1, hairL);
                r2(23, 26, 1, 8, coatD); r2(23, 34, 1, 1, btn);
                r2(22, 17, 3, 1, "#c0906f");
                r2(10, frame == 1 ? 55 : 61, 12, 1, "#14161c");
                r2(22, frame == 3 ? 55 : 61, 12, 1, "#14161c");
            }
        }

        var rows = new[] { "down", "up", "left", "right" };
        for (int ri = 0; ri < 4; ri++)
        {
            for (int f = 0; f < 4; f++)
            {
                int ox = f * CW, oy = ri * CH;
                string dir = rows[ri];
                bool side = dir == "left" || dir == "right";
                bool isUp = dir == "up";
                Paint(side, isUp, f);
                // 描边层（躯干/头按起伏偏移，腿不动）
                for (int oi = 0; oi < ops.Count; oi++)
                {
                    var (x, y, w, h, _) = ops[oi];
                    int dy = oi >= bodyStart ? bobY : 0;
                    int dx = oi >= bodyStart ? bobX : 0;
                    int px = dir == "left" ? ox + CW - 2 - x - w : ox + x;
                    p.FillRect(px + dx, oy + y + dy, w + 2, h + 2, C(line));
                }
                // 颜色层
                for (int oi = 0; oi < ops.Count; oi++)
                {
                    var (x, y, w, h, col) = ops[oi];
                    int dy = oi >= bodyStart ? bobY : 0;
                    int dx = oi >= bodyStart ? bobX : 0;
                    int px = dir == "left" ? ox + CW - 1 - x - w : ox + x + 1;
                    p.FillRect(px + dx, oy + y + 1 + dy, w, h, C(col));
                }
            }
        }
        return p;
    }

    static Px Court(string kind)
    {
        var p = new Px(512, 320);
        p.FillRect(0, 0, 512, 320, C(kind == "basket" ? "#2f6b52" : "#3b6b4a"));
        for (int i = 0; i < 900; i++)
        {
            int x = R.Int(512), y = R.Int(320);
            p.FillRect(x, y, 4, 4, C(R.Int(2) == 0 ? "rgba(255,255,255,0.05)" : "rgba(0,0,0,0.06)"));
        }
        p.FillRect(0, 0, 512, 10, C("rgba(0,0,0,0.18)"));
        p.FillRect(0, 310, 512, 10, C("rgba(0,0,0,0.18)"));
        p.FillRect(0, 0, 10, 320, C("rgba(0,0,0,0.18)"));
        p.FillRect(502, 0, 10, 320, C("rgba(0,0,0,0.18)"));
        p.StrokeRect(24, 24, 464, 272, 4, C("rgba(246,244,236,0.92)"));
        if (kind == "basket")
        {
            p.StrokeCircle(256, 160, 30, 4, C("rgba(246,244,236,0.92)"));
            p.FillRect(254, 24, 4, 272, C("rgba(246,244,236,0.92)"));
            p.StrokeRect(24, 108, 46, 104, 4, C("rgba(246,244,236,0.92)"));
            p.StrokeRect(442, 108, 46, 104, 4, C("rgba(246,244,236,0.92)"));
        }
        else
        {
            p.FillRect(254, 24, 4, 272, C("rgba(246,244,236,0.92)"));
            p.StrokeRect(60, 120, 100, 80, 4, C("rgba(246,244,236,0.92)"));
            p.StrokeRect(352, 120, 100, 80, 4, C("rgba(246,244,236,0.92)"));
        }
        return p;
    }

    static Px Road(string kind)
    {
        var p = new Px(64, 64);
        if (kind == "mainstone")
        {
            p.FillRect(0, 0, 64, 64, C("#b3aca0"));
            var cols = new[] { "#c6bfb2", "#bdb6a9", "#c2bbae", "#b8b1a4" };
            for (int by = 0; by < 2; by++) for (int bx = 0; bx < 2; bx++)
            {
                p.FillRect(bx * 32 + 1, by * 32 + 1, 30, 30, C(cols[R.Int(4)]));
                for (int i = 0; i < 12; i++)
                    p.FillRect(bx * 32 + 2 + R.Int(28), by * 32 + 2 + R.Int(28), 3, 2, C(R.Int(2) == 0 ? "rgba(140,132,120,0.25)" : "rgba(235,231,222,0.35)"));
            }
        }
        else
        {
            p.FillRect(0, 0, 64, 64, C("#a89c8a"));
            var cols = new[] { "#b8ac97", "#b0a48f", "#c0b4a0", "#a89c88" };
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
            {
                p.FillRect(x * 8 + 1, y * 8 + 1, 6, 6, C(cols[R.Int(4)]));
                if (R.Int(3) == 0) p.FillRect(x * 8 + 2, y * 8 + 3, 3, 2, C("rgba(120,110,95,0.25)"));
            }
        }
        return p;
    }

    static Px RoadBand(bool vertical)
    {
        var p = vertical ? new Px(8, 32) : new Px(32, 8);
        p.FillRect(0, 0, p.W, p.H, C("#cfc7b8"));
        if (vertical)
        {
            p.FillRect(0, 0, 1, 32, C("rgba(150,142,128,0.55)"));
            p.FillRect(7, 0, 1, 32, C("rgba(150,142,128,0.55)"));
            p.FillRect(0, 15, 8, 1, C("rgba(150,142,128,0.4)"));
        }
        else
        {
            p.FillRect(0, 0, 32, 1, C("rgba(150,142,128,0.55)"));
            p.FillRect(0, 7, 32, 1, C("rgba(150,142,128,0.55)"));
            p.FillRect(15, 0, 1, 8, C("rgba(150,142,128,0.4)"));
        }
        return p;
    }

    // ── 缓存与入口（名称与现有 PNG 文件名一致）──
    static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

    public static Texture2D Get(string name, bool pixel, bool repeat)
    {
        string key = name + (pixel ? "|p" : "|s") + (repeat ? "|r" : "|c");
        if (Cache.TryGetValue(key, out var t)) return t;
        R = NewR();   // 每张贴图独立种子：与 JS 导出器逐张重建的调用序列一致
        Px px = name switch
        {
            "grass" => Grass(), "path" => PathTex(), "flower" => Flower(), "dirt" => Dirt(),
            "track" => Track(), "pond" => Pond(), "sky_day" => SkyDay(), "sky_night" => SkyNight(),
            "lamp_glow" => LampGlow(), "roof_villa" => Roof("#3a434e"), "villa" => Villa(),
            "player" => Character(), "door" => Door(), "awning" => Stripes(),
            "court_basket" => Court("basket"), "court_tennis" => Court("tennis"),
            "road_mainstone" => Road("mainstone"), "road_cobble" => Road("cobble"),
            "roadband_v" => RoadBand(true), "roadband_h" => RoadBand(false),
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

    // 名称分发：固定名 + 参数化名（建筑/边翼/屋面/远景外墙）；找不到返回 null 由调用方回退 Resources（招牌 PNG）
    static WorldData _world;
    static WorldData Wd => _world ?? (_world = CampusData.Build());

    public static Texture2D ByName(string name, bool pixel, bool repeat)
    {
        if (name.StartsWith("wall_") || name.StartsWith("wing_"))
        {
            var parts = name.Split('_');               // wall_teach_day / wing_teach_day
            if (parts.Length >= 3)
            {
                string id = parts[1];
                bool night = parts[2] == "night";
                var b = Array.Find(Wd.BUILDINGS, x => x.id == id);
                if (b != null)
                {
                    bool wing = parts[0] == "wing";
                    return BuildingWall(b.wall, b.trim, b.plinth, wing ? 3 : b.cols, wing ? 2 : b.rows,
                        !wing && b.banner, night ? 0.55f : 0.05f);
                }
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
            return BuildingWall(d.wall, d.trim, d.plinth, Mathf.Max(2, Mathf.RoundToInt(d.w / 3f)), Mathf.Max(2, Mathf.RoundToInt(d.h / 1.7f)), false, 0.16f);
        }
        return Get(name, pixel, repeat);
    }

    // 建筑/远景外墙需先按参数生成（非固定名）
    public static Texture2D BuildingWall(string wall, string trim, string plinth, int cols, int rows, bool banner, float lit)
    {
        R = NewR();
        var t = From(Building(wall, trim, plinth, cols, rows, banner, lit));
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

    // ── 对拍：导出全部生成贴图为 PNG（Editor 调用）──
    public static void DumpAll(string dir)
    {
        Directory.CreateDirectory(dir);
        foreach (var n in new[] { "grass", "path", "flower", "dirt", "track", "pond", "sky_day", "sky_night", "lamp_glow", "villa", "player", "door", "awning", "court_basket", "court_tennis", "road_mainstone", "road_cobble", "roadband_v", "roadband_h" })
        {
            var t = Get(n, true, true);
            File.WriteAllBytes(Path.Combine(dir, n + ".png"), t.EncodeToPNG());
        }
        var rv = RoofTex("#3a434e");
        File.WriteAllBytes(Path.Combine(dir, "roof_villa.png"), rv.EncodeToPNG());
        Debug.Log("[TextureLibrary] 已导出对拍贴图 -> " + dir);
    }
}
