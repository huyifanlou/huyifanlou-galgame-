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
    // S：像素密度倍数（物理像素 = 逻辑 × S）。绘制代码一律写「逻辑坐标」（= 世界尺度配方），
    //    提密度只改构造参数的 S，配方逐字不动 —— 与「改密度只动 tiling/贴图尺寸」同一原则。
    class Px
    {
        public readonly int W, H, S;
        public readonly int PW, PH;   // 物理像素尺寸（= W×S / H×S）
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

        // 支持浮点坐标（与 canvas fillRect 一致：按覆盖率做抗锯齿混合）
        public void FillRect(float x, float y, float w, float h, Color c)
        {
            int x0 = Mathf.FloorToInt(x * S), x1 = Mathf.FloorToInt((x + w) * S);
            int y0 = Mathf.FloorToInt(y * S), y1 = Mathf.FloorToInt((y + h) * S);
            float inv = 1f / S;
            for (int yy = y0; yy < y1; yy++)
                for (int xx = x0; xx < x1; xx++)
                {
                    // 覆盖率在逻辑尺度上算（物理像素 = 1/S 个逻辑像素），再乘 S² 归一化回单像素
                    float cov = (Mathf.Min(x + w, (xx + 1) * inv) - Mathf.Max(x, xx * inv)) * (Mathf.Min(y + h, (yy + 1) * inv) - Mathf.Max(y, yy * inv)) * S * S;
                    if (cov <= 0.001f) continue;
                    if (cov >= 0.999f || c.a < 0.999f) { var cc = c; cc.a = c.a * Mathf.Min(1f, cov); Set(xx, yy, cc); }
                    else { var cc = c; cc.a = c.a * cov; Set(xx, yy, cc); }
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
            // 在圆角半径内：到最近角心距离 ≤ r（角心到边为直段，天然满足）
            if (px >= x + r && px <= x + w - r) return true;
            if (py >= y + r && py <= y + h - r) return true;
            return dx * dx + dy * dy <= r * r + 0.0001f;
        }

        public void FillRound(float x, float y, float w, float h, float r, Color c)
        {
            float inv = 1f / S;
            for (int yy = Mathf.FloorToInt(y * S); yy <= Mathf.CeilToInt((y + h) * S); yy++)
                for (int xx = Mathf.FloorToInt(x * S); xx <= Mathf.CeilToInt((x + w) * S); xx++)
                    if (InRound((xx + 0.5f) * inv, (yy + 0.5f) * inv, x, y, w, h, r)) Set(xx, yy, c);
        }

        // 擦除成透明（挖空）：用于「透出下层地形」的贴片（操场跑道内场）
        public void ClearRound(float x, float y, float w, float h, float r)
        {
            float inv = 1f / S;
            for (int yy = Mathf.FloorToInt(y * S); yy <= Mathf.CeilToInt((y + h) * S); yy++)
                for (int xx = Mathf.FloorToInt(x * S); xx <= Mathf.CeilToInt((x + w) * S); xx++)
                    if (InRound((xx + 0.5f) * inv, (yy + 0.5f) * inv, x, y, w, h, r) && xx >= 0 && yy >= 0 && xx < PW && yy < PH)
                        Buf[yy * PW + xx] = new Color(0, 0, 0, 0);
        }

        public void Clear(int x, int y)
        {
            if (x < 0 || y < 0 || x >= PW || y >= PH) return;
            Buf[y * PW + x] = new Color(0, 0, 0, 0);
        }

        public void StrokeRound(float x, float y, float w, float h, float r, float lw, Color c)
        {
            float inv = 1f / S;
            for (int yy = Mathf.FloorToInt((y - lw) * S); yy <= Mathf.CeilToInt((y + h + lw) * S); yy++)
                for (int xx = Mathf.FloorToInt((x - lw) * S); xx <= Mathf.CeilToInt((x + w + lw) * S); xx++)
                {
                    float px = (xx + 0.5f) * inv, py = (yy + 0.5f) * inv;
                    if (!InRound(px, py, x - lw / 2, y - lw / 2, w + lw, h + lw, r + lw / 2)) continue;
                    if (InRound(px, py, x + lw / 2, y + lw / 2, w - lw, h - lw, Mathf.Max(1, r - lw / 2))) continue;
                    Set(xx, yy, c);
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
        var t = new Texture2D(p.PW, p.PH, TextureFormat.RGBA32, false);
        // 注意：Unity 的 SetPixels 行序自下而上，而画布缓冲行 0 是顶行（与 canvas 一致）→ 必须翻转
        var flipped = new Color[p.Buf.Length];
        for (int y = 0; y < p.PH; y++)
            Array.Copy(p.Buf, y * p.PW, flipped, (p.PH - 1 - y) * p.PW, p.PW);
        t.SetPixels(flipped);
        t.Apply(false, false);
        return t;
    }

    static Px Grass()
    {
        var p = new Px(256, 256);   // 密度翻倍（256px 覆盖 1.67m ≈ 153px/米）
        for (int y = 0; y < 256; y += 8) for (int x = 0; x < 256; x += 8)
            p.FillRect(x, y, 8, 8, C((x + y) % 16 == 0 ? "#657a4b" : "#5d7245"));
        var cols = new[] { "#54703f", "#6d8450", "#597344", "#788e58" };
        for (int i = 0; i < 260; i++)
        {
            var c = C(cols[R.Int(4)]);
            int rx = R.Int(244), ry = R.Int(244), w = R.Int(2) == 1 ? 8 : 12, h = R.Int(2) == 1 ? 12 : 8;
            p.FillRect(rx, ry, w, h, c);
        }
        var pcols = new[] { "#556f45", "#6a8454", "#4f6a41" };
        for (int i = 0; i < 36; i++)
        {
            float a = 0.35f + (float)R.Next() * 0.2f;
            var c = C(pcols[R.Int(3)]); c.a = a;
            p.FillRect(R.Int(224), R.Int(224), 16 + R.Int(14), 16 + R.Int(14), c);
        }
        // 细颗粒碎草与小石子（随密度翻倍，颗粒 1-2px ≈ 0.7-1.3cm）
        var fine = new[] { "#54703f", "#6d8450", "#788e58", "#597344" };
        for (int i = 0; i < 900; i++)
            p.FillRect(R.Int(250), R.Int(250), 2, R.Int(2) == 0 ? 4 : 2, C(fine[R.Int(4)]));
        for (int i = 0; i < 90; i++)
            p.FillRect(R.Int(248), R.Int(248), 2, 2, C(R.Int(2) == 0 ? "#82905c" : "#49603c"));
        for (int i = 0; i < 40; i++)
            p.FillRect(R.Int(248), R.Int(248), R.Int(2) + 1, 2, C("rgba(210,205,190,0.35)"));
        // 写实化：草叶丝（1px 竖向短线）+ 团簇阴影（暗团 + 亮顶）+ 少量枯黄点
        var blades = new[] { "#6f8450", "#4d6539", "#7f9459", "#5d7245" };
        for (int i = 0; i < 420; i++)
            p.FillRect(R.Int(252), R.Int(250), 1, 3 + R.Int(3), C(blades[R.Int(4)]));
        for (int i = 0; i < 26; i++)
        {
            int bx = R.Int(226), by = R.Int(226), bw = 10 + R.Int(14), bh = 8 + R.Int(10);
            p.FillRect(bx, by, bw, bh, C("rgba(24,36,18,0.16)"));
            p.FillRect(bx, by, bw, 3, C("rgba(150,168,110,0.20)"));
        }
        for (int i = 0; i < 70; i++)
            p.FillRect(R.Int(252), R.Int(252), 2, 2, C("rgba(196,176,96,0.30)"));
        return p;
    }

    static Px PathTex()
    {
        var p = new Px(128, 128);   // 密度翻倍（128px 覆盖 0.8m ≈ 160px/米）
        p.FillRect(0, 0, 128, 128, C("#a49c8d"));
        var cols = new[] { "#c1b9ac", "#b8b0a3", "#bdb4a6", "#b3ab9d" };
        for (int by = 0; by < 2; by++) for (int bx = 0; bx < 2; bx++)
        {
            p.FillRect(bx * 64 + 4, by * 64 + 4, 56, 56, C(cols[R.Int(4)]));
            // 写实化：石块倒角 —— 左上受光、右下背光（2px ≈ 1.2cm）
            p.FillRect(bx * 64 + 4, by * 64 + 4, 56, 2, C("rgba(255,255,255,0.14)"));
            p.FillRect(bx * 64 + 4, by * 64 + 4, 2, 56, C("rgba(255,255,255,0.09)"));
            p.FillRect(bx * 64 + 6, by * 64 + 58, 54, 2, C("rgba(0,0,0,0.16)"));
            p.FillRect(bx * 64 + 58, by * 64 + 6, 2, 54, C("rgba(0,0,0,0.12)"));
            for (int i = 0; i < 40; i++) p.FillRect(bx * 64 + 4 + R.Int(56), by * 64 + 4 + R.Int(56), 8, 4, C("rgba(122,114,102,0.45)"));
            for (int i = 0; i < 24; i++) p.FillRect(bx * 64 + 4 + R.Int(56), by * 64 + 4 + R.Int(56), 4, 4, C("rgba(230,226,216,0.5)"));
            for (int i = 0; i < 10; i++) p.FillRect(bx * 64 + 4 + R.Int(56), by * 64 + 4 + R.Int(56), 2, 2, C(R.Int(2) == 0 ? "rgba(100,92,80,0.35)" : "rgba(245,242,235,0.45)"));
        }
        return p;
    }

    // 立面目标密度：≤166px/米（基准 160 ±6% 内），且单张不超过 2048 → D = min(166, 2048/宽, 2048/高)
    public static float FacadeDensity(float wMeters, float hMeters)
        => Mathf.Min(166f, Mathf.Min(2048f / Mathf.Max(0.5f, wMeters), 2048f / Mathf.Max(0.5f, hMeters)));

    // 建筑立面：按「世界比例」绘制 —— 美术坐标沿用原 256 格布局，Draw() 按目标像素各自缩放，
    // 于是世界观感与旧版一致，但像素密度恒定（宽/高同密度）。配合 MeshFactory.BoxUV 贴到盒子上，
    // 正/背/侧四面都是一个密度（侧面显示立面的左侧裁切，不再被拉伸）。
    static Px Building(string wall, string trim, string plinth, int cols, int rows, bool banner, float lit,
                       float wMeters, float hMeters)
    {
        const int cell = 256;
        float aw = cols * cell, ah = rows * cell + 320f;      // 原美术画布（逻辑）
        float D = FacadeDensity(wMeters, hMeters);
        var p = new Px(Mathf.Max(8, Mathf.RoundToInt(wMeters * D)), Mathf.Max(8, Mathf.RoundToInt(hMeters * D)));
        float sx = p.W / aw, sy = p.H / ah;
        void Draw(float x, float y, float w2, float h2, Color c) => p.FillRect(x * sx, y * sy, w2 * sx, h2 * sy, c);

        Draw(0, 0, aw, ah, C(wall));
        int noise = (int)Math.Floor((aw * (double)ah) / 1536);
        for (int i = 0; i < noise; i++)
        {
            Draw(R.Int((int)aw), R.Int((int)ah), 48, 24, C("rgba(0,0,0,0.05)"));
            Draw(R.Int((int)aw), R.Int((int)ah), 24, 24, C("rgba(255,255,255,0.05)"));
            Draw(R.Int((int)aw), R.Int((int)ah), 8, 8, C(R.Int(2) == 0 ? "rgba(0,0,0,0.06)" : "rgba(255,255,255,0.05)"));
        }
        for (float y = 64; y < ah; y += 64) Draw(0, y, aw, 4, C("rgba(0,0,0,0.035)"));
        Draw(0, 0, aw, 64, C(trim));
        Draw(0, 64, aw, 24, C("rgba(0,0,0,0.12)"));
        for (int r = 1; r < rows; r++)
        {
            Draw(0, r * cell + 96, aw, 56, C(trim));
            Draw(0, r * cell + 152, aw, 24, C("rgba(0,0,0,0.15)"));
        }
        Draw(0, ah - 192, aw, 192, C(plinth));
        Draw(0, ah - 72, aw, 72, C("rgba(0,0,0,0.18)"));
        Draw(0, ah - 48, aw, 16, C("rgba(255,255,255,0.06)"));
        for (int r = 0; r < rows; r++) for (int c2 = 0; c2 < cols; c2++)
        {
            bool isLit = R.Next() < lit;
            int kind = R.Int(10);
            float wx = c2 * cell + 56, wy = r * cell + 80;
            Draw(wx - 24, wy - 24, 208, 208, C(trim));
            Draw(wx, wy, 160, 160, C(isLit ? "#ffd98a" : "#5d7286"));
            if (isLit) Draw(wx, wy, 160, 24, C("#ffe9b8"));
            else
            {
                // 日间玻璃：上亮下暗的天空反射渐变 + 斜向高光（线性空间里纯深蓝会读成死黑）
                Draw(wx, wy, 160, 36, C("rgba(198,216,230,0.30)"));
                Draw(wx, wy + 36, 160, 28, C("rgba(180,200,218,0.16)"));
                Draw(wx, wy + 120, 160, 40, C("rgba(16,24,34,0.20)"));
                Draw(wx + 20, wy + 12, 30, 110, C("rgba(220,234,244,0.18)"));
                Draw(wx + 58, wy + 26, 12, 96, C("rgba(220,234,244,0.10)"));
                if (R.Next() < 0.35)
                {
                    Draw(wx + 16, wy + 16, 56, 24, C("rgba(180,205,230,0.22)"));
                    Draw(wx + 32, wy + 40, 40, 24, C("rgba(180,205,230,0.18)"));
                }
                Draw(wx + 76, wy, 8, 160, C(trim));
                Draw(wx, wy + 76, 160, 8, C(trim));
            }
            if (kind <= 1 && !isLit) Draw(wx, wy, 160, 56, C("#a08b80"));
            if (kind == 2 && !isLit)
            {
                Draw(wx, wy + 168, 160, 40, C("#3f6e35"));
                for (int i = 0; i < 5; i++) Draw(wx + 16 + i * 32, wy + 152, 16, 24, C("#4a8743"));
            }
            Draw(wx, wy + 136, 160, 24, C("rgba(0,0,0,0.3)"));
            Draw(wx, wy + 208, 160, 24, C("rgba(0,0,0,0.22)"));
            Draw(wx, wy + 184, 160, 16, C("rgba(255,255,255,0.05)"));
            if (kind == 3)
            {
                Draw(wx + 24, wy + 208, 104, 80, C("#9aa2ac"));
                for (int i = 0; i < 3; i++) Draw(wx + 32, wy + 220 + i * 20, 88, 8, C("rgba(0,0,0,0.15)"));
                Draw(wx + 24, wy + 264, 104, 24, C("rgba(0,0,0,0.25)"));
            }
            if (r > 0 && kind == 4 && !isLit)
            {
                Draw(wx - 24, wy + 216, 208, 16, C("#cfc9ba"));
                for (int bxx = 0; bxx < 12; bxx++) Draw(wx - 16 + bxx * 16, wy + 168, 8, 48, C("#b8b0a2"));
            }
        }
        if (banner)
        {
            float by = cell + 352;
            Draw(96, by + 208, aw - 192, 24, C("rgba(0,0,0,0.18)"));
            Draw(96, by, aw - 192, 208, C("#c0392b"));
            Draw(96, by, aw - 192, 12, C("rgba(255,255,255,0.12)"));
            for (float x = 208; x < aw - 304; x += 176) Draw(x, by + 56, 104, 96, C("#f5d04e"));
        }
        // 写实化：立面下段渐脏（雨水/泥沙沉积）+ 随机窗下雨痕（低对比，避免显脏）
        for (int i = 0; i < 12; i++)
            Draw(0, ah - 360 + i * 30, aw, 30, new Color(0.16f, 0.13f, 0.10f, 0.010f + i * 0.0045f));
        for (int r = 0; r < rows; r++) for (int c2 = 0; c2 < cols; c2++)
            if (R.Next() < 0.35)
                Draw(c2 * cell + 70 + R.Int(120), r * cell + 232, 8 + R.Int(12), 40 + R.Int(120), C("rgba(45,38,30,0.055)"));
        return p;
    }

    static Px Door()
    {
        var p = new Px(128, 192, 2);   // 物理 256×384，铺在门洞 1.5×2.1m 上 ≈ 171×183px/米（略高于基准，更清晰）
        p.FillRect(0, 0, 128, 192, C("#efe7d8"));
        p.FillRect(12, 12, 104, 180, C("#6b4a34"));
        p.FillRect(24, 28, 80, 48, C("#ffd98a"));
        p.FillRect(60, 28, 8, 48, C("#5a3c2a"));
        p.FillRect(24, 48, 80, 8, C("#5a3c2a"));
        p.FillRect(24, 96, 32, 68, C("#54382a"));
        p.FillRect(72, 96, 32, 68, C("#54382a"));
        p.FillRect(60, 96, 8, 72, C("#3a2519"));
        p.FillRect(44, 136, 8, 8, C("#d8b54a"));
        p.FillRect(76, 136, 8, 8, C("#d8b54a"));
        p.FillRect(12, 180, 104, 12, C("#8a6a4a"));
        // 新细节：门板凹槽 + 门牌 + 气窗竖棂
        p.FillRect(28, 104, 24, 4, C("rgba(0,0,0,0.18)"));
        p.FillRect(76, 104, 24, 4, C("rgba(0,0,0,0.18)"));
        p.FillRect(28, 116, 24, 4, C("rgba(0,0,0,0.18)"));
        p.FillRect(76, 116, 24, 4, C("rgba(0,0,0,0.18)"));
        p.FillRect(52, 18, 24, 12, C("#c9c3b6"));
        p.FillRect(40, 28, 4, 48, C("rgba(90,60,42,0.5)"));
        p.FillRect(84, 28, 4, 48, C("rgba(90,60,42,0.5)"));
        return p;
    }

    static Px Stripes()
    {
        var p = new Px(64, 64);   // 密度 ×4（条纹世界宽度不变）
        var a = C("#efe9dd"); var b = C("#c9553f");
        for (int x = 0; x < 64; x++) p.FillRect(x, 0, 1, 64, x % 16 < 8 ? a : b);
        return p;
    }

    static Px Flower()
    {
        // 透明底：只画花与茎叶，草完全透出地形（同颗粒/同色调）——
        // 之前自带一块草地，导致「浅色方块 + 硬边」；现在看起来就是草地上开的一片花。
        var p = new Px(128, 128, 2);   // 覆盖 1.7m ≈ 150.6px/米；花头 6 逻辑 px ≈ 8cm
        var colors = new[] { "#e8748a", "#f2d857", "#f4f2ea", "#c58ae0", "#f08a5a" };
        var leaves = new[] { "#3f7239", "#4a8743", "#5da054", "#356e37" };
        for (int i = 0; i < 34; i++)
        {
            // 中心略密、边缘略疏（不出现"方块边界"的手感）
            float cx = 64 + (float)(R.Next() * 2 - 1) * 56, cy = 64 + (float)(R.Next() * 2 - 1) * 56;
            float d = Mathf.Sqrt((cx - 64) * (cx - 64) + (cy - 64) * (cy - 64));
            if (d > 56 || R.Next() < d / 95f) continue;
            int x = Mathf.RoundToInt(cx), y = Mathf.RoundToInt(cy);
            p.FillRect(x - 1, y + 4, 8, 5, C(leaves[R.Int(4)]));   // 叶丛
            p.FillRect(x + 2, y + 7, 2, 3, C("#2f5f2a"));          // 茎影
            var c = C(colors[R.Int(colors.Length)]);
            p.FillRect(x, y, 6, 6, c);                              // 花头（8cm）
            p.FillRect(x + 1, y + 1, 2, 2, C("rgba(255,255,255,0.65)"));
            p.FillRect(x - 1, y + 2, 1, 2, c);                      // 花瓣外扩
            p.FillRect(x + 6, y + 2, 1, 2, c);
            p.FillRect(x + 2, y - 1, 2, 1, c);
        }
        return p;
    }

    static Px Dirt()
    {
        var p = new Px(96, 96, 8);   // 物理 768×768，覆盖 4.8m = 160px/米（精确对齐基准）
        var cols = new[] { "#8a6f4f", "#7d6448", "#938060" };
        for (int y = 4; y < 92; y += 2) for (int x = 4; x < 92; x += 2)
        {
            double d = Math.Sqrt((x - 48.0) * (x - 48.0) + (y - 48.0) * (y - 48.0)) + (R.Next() * 7 - 3.5);
            double solid = d < 24 ? 1 : d < 29 ? (R.Next() < 0.5 ? 1 : 0) : d < 34 ? (R.Next() < 0.12 ? 1 : 0) : 0;
            if (solid > 0) p.FillRect(x, y, 2, 2, C(cols[R.Int(3)]));
        }
        // 写实化：外缘干土亮边 + 小石子（浅）与深色土块（低对比，不脏）
        for (int i = 0; i < 30; i++)
        {
            double a = R.Next() * Math.PI * 2, rr = 22 + R.Next() * 12;
            p.FillRect((float)(48 + Math.Cos(a) * rr), (float)(48 + Math.Sin(a) * rr), 2 + R.Int(2), 2, C("rgba(214,196,160,0.45)"));
        }
        for (int i = 0; i < 26; i++)
        {
            double a = R.Next() * Math.PI * 2, rr = R.Next() * 26;
            p.FillRect((float)(48 + Math.Cos(a) * rr), (float)(48 + Math.Sin(a) * rr), 2, 2,
                C(R.Int(2) == 0 ? "rgba(232,226,214,0.75)" : "rgba(70,54,36,0.45)"));
        }
        return p;
    }

    // 叶簇（树冠用，可被材质 color 染色）：近白底 + 暗色叶隙团簇 —— 世界尺度 0.8m = 160px/米
    static Px Leaves()
    {
        var p = new Px(128, 128);
        p.FillRect(0, 0, 128, 128, C("#fafaf6"));
        for (int i = 0; i < 340; i++)
        {
            int x = R.Int(124), y = R.Int(124);
            p.FillRect(x, y, 3 + R.Int(6), 3 + R.Int(5), C(R.Int(2) == 0 ? "rgba(0,0,0,0.115)" : "rgba(0,0,0,0.065)"));
        }
        for (int i = 0; i < 150; i++)
            p.FillRect(R.Int(126), R.Int(126), 1, 2, C("rgba(0,0,0,0.15)"));
        for (int i = 0; i < 70; i++)
            p.FillRect(R.Int(126), R.Int(126), 1, 1, C("rgba(255,255,255,0.40)"));
        return p;
    }

    // 混凝土矮墙（矮墙/立柱用）：384px 覆盖 2.4m = 160px/米 —— 关键特征做到「分米级」，
    // 远处也读得出来：砌筑缝每 2.4m、墙脚深色勒脚带、细骨料颗粒、气孔、污渍/裂纹。
    static Px Concrete()
    {
        var p = new Px(384, 384);   // 2.4m 见方（160px/米）
        p.FillRect(0, 0, 384, 384, C("#c6bfb0"));
        // 骨料颗粒（1~5px ≈ 0.6~3cm）
        for (int i = 0; i < 2400; i++)
            p.FillRect(R.Int(380), R.Int(380), 1 + R.Int(4), 1 + R.Int(3),
                C(R.Int(3) == 0 ? "rgba(255,255,255,0.14)" : (R.Int(2) == 0 ? "rgba(90,84,74,0.12)" : "rgba(150,144,132,0.14)")));
        // 气孔
        for (int i = 0; i < 320; i++)
            p.FillRect(R.Int(380), R.Int(380), 1, 1, C("rgba(70,64,56,0.20)"));
        // 砌筑缝（每 2.4m 一道，宽 6px ≈ 3.75cm；带一条亮边模拟倒角）
        p.FillRect(0, 0, 6, 384, C("rgba(70,64,56,0.30)"));
        p.FillRect(6, 0, 2, 384, C("rgba(255,255,255,0.12)"));
        // 墙脚勒脚带（贴图底部 28px ≈ 0.18m：颜色更深 + 顶边一条分界线）
        p.FillRect(0, 384 - 28, 384, 28, new Color(0.46f, 0.43f, 0.38f, 0.55f));
        p.FillRect(0, 384 - 30, 384, 2, C("rgba(60,56,48,0.30)"));
        for (int i = 0; i < 60; i++)
            p.FillRect(R.Int(378), 384 - 26 + R.Int(24), 2 + R.Int(3), 2, C("rgba(52,54,36,0.22)"));   // 苔点
        // 雨痕/污渍（竖向拖尾）+ 细裂纹
        for (int i = 0; i < 10; i++)
        {
            int x = R.Int(370), w = 6 + R.Int(18), h = 40 + R.Int(120);
            p.FillRect(x, R.Int(60), w, h, C("rgba(70,64,54,0.07)"));
        }
        for (int i = 0; i < 5; i++)
        {
            int x = 20 + R.Int(330), y = 20 + R.Int(320);
            for (int k = 0; k < 12; k++) p.FillRect(x + k * 2 + R.Int(2), y + R.Int(3) + k * 2, 2, 1, C("rgba(80,74,66,0.16)"));
        }
        return p;
    }

    // 雨丝（粒子用：极窄的柔边竖线，纵向渐隐）
    static Px RainStreak()
    {
        var p = new Px(16, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 16; x++)
            {
                float d = Mathf.Abs(x - 7.5f) / 8f;
                float fade = y < 6 ? y / 6f : (y > 58 ? (64 - y) / 6f : 1f);
                float a = Mathf.Clamp01(1f - d) * fade * 0.5f;
                if (a > 0.02f) p.FillRect(x, y, 1, 1, new Color(0.80f, 0.87f, 0.95f, a));
            }
        return p;
    }

    // 雪花（柔边圆点）
    static Px SnowFlake()
    {
        var p = new Px(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float d = Mathf.Sqrt((x - 15.5f) * (x - 15.5f) + (y - 15.5f) * (y - 15.5f)) / 15.5f;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * 0.95f;
                if (a > 0.02f) p.FillRect(x, y, 1, 1, new Color(1f, 1f, 1f, a));
            }
        return p;
    }

    // 积雪地面（叠在地形上的雪层；轻微起伏的冷调阴影 + 雪粒高光）——覆盖 1.667m（与草地同周期）
    static Px SnowGround()
    {
        var p = new Px(256, 256);
        p.FillRect(0, 0, 256, 256, C("#eef1f5"));
        for (int i = 0; i < 240; i++)
        {
            int x = R.Int(246), y = R.Int(246);
            p.FillRect(x, y, 8 + R.Int(16), 5 + R.Int(9), C(R.Int(2) == 0 ? "rgba(198,208,222,0.32)" : "rgba(255,255,255,0.55)"));
        }
        for (int i = 0; i < 260; i++)
            p.FillRect(R.Int(254), R.Int(254), 2, 2, C(R.Int(3) == 0 ? "rgba(255,255,255,0.85)" : "rgba(206,215,227,0.5)"));
        for (int i = 0; i < 40; i++)
            p.FillRect(R.Int(252), R.Int(252), 1, 1, C("rgba(255,255,255,1)"));
        return p;
    }

    // 树皮（可染色）：近白底 + 竖向沟槽/裂纹/苔点 —— 世界尺度 0.6m = 160px/米
    static Px Bark()
    {
        var p = new Px(96, 96);
        p.FillRect(0, 0, 96, 96, C("#f2ece4"));
        for (int i = 0; i < 26; i++)
        {
            int x = R.Int(90), w = 2 + R.Int(4), y = R.Int(60), h = 24 + R.Int(56);
            p.FillRect(x, y, w, h, C(R.Int(3) == 0 ? "rgba(255,255,255,0.30)" : "rgba(0,0,0,0.20)"));
        }
        for (int i = 0; i < 30; i++)
            p.FillRect(R.Int(92), R.Int(92), 1, 3 + R.Int(6), C("rgba(0,0,0,0.14)"));
        for (int i = 0; i < 18; i++)
            p.FillRect(R.Int(94), R.Int(94), 2, 2, C("rgba(60,90,45,0.22)"));
        return p;
    }

    static Px Track()
    {
        // 只画跑道面 + 场地线，底色透明、内场挖空 → 直接透出地形草地：
        // 颗粒（地形草 5.2cm 格）、色调、接缝都与周围一致，不再像「贴上去的一块方片」
        var p = new Px(512, 320, 4);   // 物理 2048×1280（撞 2048 上限），覆盖操场 16×10m = 128px/米
        p.FillRound(12, 12, 488, 296, 92, C("#b26240"));
        var noise = new[] { "#a8593a", "#bd6f4a", "#9e5335", "#c47a52" };
        for (int i = 0; i < 900; i++)
        {
            int x = R.Int(512), y = R.Int(320);
            if (!Px.InRound(x + 0.5f, y + 0.5f, 12, 12, 488, 296, 92)) continue;
            p.FillRect(x, y, 4, 4, C(noise[R.Int(4)]));
        }
        p.ClearRound(48, 48, 416, 224, 64);                            // 内场挖空（透出地形草）
        p.StrokeRound(24, 24, 464, 272, 82, 4, C("rgba(244,240,226,0.9)"));   // 外道线
        p.StrokeRound(40, 40, 432, 240, 70, 4, C("rgba(244,240,226,0.9)"));   // 内道线
        p.FillRect(254, 52, 4, 216, C("rgba(244,240,226,0.9)"));              // 中线
        p.StrokeCircle(256, 160, 28, 4, C("rgba(244,240,226,0.9)"));          // 中圈
        return p;
    }

    static Px Pond()
    {
        var p = new Px(256, 256, 4);   // 物理 1024×1024，覆盖 (r+0.6)×2 = 7.2m ≈ 142px/米
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
        // 写实化：浅水亮边 → 深水暗心的径向渐变 + 水线高光
        for (int r = 102; r > 30; r -= 6)
            p.FillCircle(128, 128, r, new Color(0.05f, 0.14f, 0.30f, 0.045f));
        p.StrokeCircle(128, 128, 103, 3, new Color(0.78f, 0.88f, 0.97f, 0.20f));
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
        var bands = new[] { "#3f86cf", "#4b93d6", "#599edc", "#68aae2", "#79b9e8", "#8cc6ee", "#9fd2f2", "#a9d2ef", "#b2d8f2", "#b8dcf3" };
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
        // 按世界比例绘制（美术坐标沿用 256×192 布局）：世界 8.4×5.4m → D=166 → 1394×896px，密度恒定
        const float aw = 256f, ah = 192f;
        float D = FacadeDensity(Wd.VILLA.w, Wd.VILLA.h);
        var p = new Px(Mathf.RoundToInt(Wd.VILLA.w * D), Mathf.RoundToInt(Wd.VILLA.h * D));
        float sx = p.W / aw, sy = p.H / ah;
        void Draw(float x, float y, float w2, float h2, Color c) => p.FillRect(x * sx, y * sy, w2 * sx, h2 * sy, c);

        Draw(0, 0, 256, 100, C("#ece5d6"));
        Draw(0, 0, 256, 4, C("#d9d2c2"));
        Draw(0, 92, 256, 8, C("rgba(0,0,0,0.12)"));
        foreach (int wx in new[] { 36, 156 })
        {
            Draw(wx - 4, 26, 68, 60, C("#8a6a48"));
            Draw(wx, 30, 60, 52, C("#f0efe6"));
            for (int i = 1; i < 4; i++) Draw(wx + i * 15, 30, 2, 52, C("#8a6a48"));
            for (int j = 1; j < 3; j++) Draw(wx, 30 + j * 17, 60, 2, C("#8a6a48"));
            Draw(wx, 80, 60, 2, C("rgba(0,0,0,0.15)"));
        }
        var wood = new[] { "#b98d5e", "#ad8153", "#c2955f", "#a87e52" };
        for (int x = 0; x < 256; x += 8)
        {
            Draw(x, 100, 8, 92, C(wood[(x / 8) % 4]));
            Draw(x, 100, 1, 92, C("rgba(0,0,0,0.14)"));
        }
        void Shoji(int sx0, int sw, int sy0, int sh)
        {
            Draw(sx0 - 3, sy0 - 3, sw + 6, sh + 6, C("#6b503a"));
            Draw(sx0, sy0, sw, sh, C("#f2efe2"));
            for (int i = 1; i < (int)Math.Floor(sw / 14f); i++) Draw(sx0 + i * 14, sy0, 2, sh, C("#7a5a3e"));
            for (int j = 1; j < (int)Math.Floor(sh / 14f); j++) Draw(sx0, sy0 + j * 14, sw, 2, C("#7a5a3e"));
        }
        Shoji(104, 48, 108, 76);
        Shoji(16, 34, 116, 56);
        Shoji(196, 34, 116, 56);
        for (int i = 0; i < 90; i++)
            Draw(R.Int(256), 104 + R.Int(84), 2, 1, C(i % 2 == 0 ? "rgba(0,0,0,0.08)" : "rgba(255,255,255,0.06)"));
        for (int i = 0; i < 60; i++)
            Draw(R.Int(256), R.Int(88), 2, 2, C(i % 2 == 0 ? "rgba(0,0,0,0.04)" : "rgba(255,255,255,0.06)"));
        return p;
    }

    static Px Court(string kind)
    {
        var p = new Px(512, 320, 4);   // 物理 2048×1280（撞 2048 上限）：15m 宽 → 136.5px/米；再高需分块
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
        // mainstone：逻辑 128（4×4 石板，间距 32）→ 物理 256px，覆盖 1.6m ≈ 160px/米
        // （石板仍是 0.4m、单块颗粒配方逐字不动，只是排列从 2×2 变 4×4、重复周期 0.8→1.6m）
        bool main = kind == "mainstone";
        var p = new Px(128, 128, main ? 2 : 1);
        if (main)
        {
            p.FillRect(0, 0, 128, 128, C("#b3aca0"));
            var cols = new[] { "#c6bfb2", "#bdb6a9", "#c2bbae", "#b8b1a4" };
            for (int by = 0; by < 4; by++) for (int bx = 0; bx < 4; bx++)
            {
                p.FillRect(bx * 32 + 1, by * 32 + 1, 30, 30, C(cols[R.Int(4)]));
                // 写实化：石板倒角（逻辑 1px = 物理 2px ≈ 1.2cm；左上亮、右下暗）
                p.FillRect(bx * 32 + 1, by * 32 + 1, 30, 1, C("rgba(255,255,255,0.13)"));
                p.FillRect(bx * 32 + 1, by * 32 + 1, 1, 30, C("rgba(255,255,255,0.08)"));
                p.FillRect(bx * 32 + 2, by * 32 + 30, 29, 1, C("rgba(0,0,0,0.16)"));
                p.FillRect(bx * 32 + 30, by * 32 + 2, 1, 29, C("rgba(0,0,0,0.12)"));
                for (int i = 0; i < 48; i++)
                    p.FillRect(bx * 32 + 2 + R.Int(28), by * 32 + 2 + R.Int(28), 3, 2, C(R.Int(2) == 0 ? "rgba(140,132,120,0.25)" : "rgba(235,231,222,0.35)"));
                for (int i = 0; i < 14; i++)
                    p.FillRect(bx * 32 + 2 + R.Int(28), by * 32 + 2 + R.Int(28), 1, 1, C(R.Int(2) == 0 ? "rgba(120,112,100,0.3)" : "rgba(245,242,235,0.4)"));
            }
        }
        else
        {
            p.FillRect(0, 0, 128, 128, C("#a89c8a"));
            var cols = new[] { "#b8ac97", "#b0a48f", "#c0b4a0", "#a89c88" };
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
            {
                p.FillRect(x * 8 + 2, y * 8 + 2, 12, 12, C(cols[R.Int(4)]));
                p.FillRect(x * 8 + 2, y * 8 + 2, 12, 1, C("rgba(255,255,255,0.12)"));   // 写实化：小石顶受光
                if (R.Int(3) == 0) p.FillRect(x * 8 + 4, y * 8 + 6, 6, 4, C("rgba(120,110,95,0.25)"));
            }
        }
        return p;
    }

    static Px RoadBand(bool vertical)
    {
        // 逻辑画布按世界尺度画：0.6m（带宽）× 1m（每铺一次）@160px/米 = 96×160
        // （旧版 8×32 是横纵不等密度的拉伸版：13.3×32px/米，边线世界宽度 7.5cm、接缝 3.1cm 保持不变）
        var p = vertical ? new Px(96, 160) : new Px(160, 96);
        p.FillRect(0, 0, p.W, p.H, C("#cfc7b8"));
        var edge = C("rgba(150,142,128,0.55)");
        var joint = C("rgba(150,142,128,0.4)");
        if (vertical)
        {
            p.FillRect(0, 0, 12, 160, edge);      // 两侧边线（沿路纵向）
            p.FillRect(84, 0, 12, 160, edge);
            p.FillRect(0, 78, 96, 5, joint);      // 接缝（每 1m 一道）
        }
        else
        {
            p.FillRect(0, 0, 160, 12, edge);
            p.FillRect(0, 84, 160, 12, edge);
            p.FillRect(78, 0, 5, 96, joint);
        }
        return p;
    }

    // ── 小镇新增：河水 / 桥用条石 / 木板 / 钟面 ──
    // 河水（4m 见方 @160px/米）：深青绿底 + 顺流水纹 + 横向微波 + 逆光闪点；所有细节 9 次平铺绘制保证无缝
    static Px Water()
    {
        var p = new Px(640, 640);
        p.FillRect(0, 0, 640, 640, C("#4d7f8e"));
        void Wrap(float x, float y, float w, float h, Color c)
        {
            for (int ox = -1; ox <= 1; ox++)
                for (int oy = -1; oy <= 1; oy++)
                    p.FillRect(x + ox * 640, y + oy * 640, w, h, c);
        }
        for (int i = 0; i < 34; i++)
            Wrap(R.Int(600), R.Int(600), 28 + R.Int(90), 18 + R.Int(60),
                C(R.Int(2) == 0 ? "rgba(30,72,84,0.20)" : "rgba(104,148,158,0.13)"));
        for (int i = 0; i < 460; i++)
            Wrap(R.Int(560), R.Int(636), 18 + R.Int(64), 1 + R.Int(2),
                C(R.Int(3) == 0 ? "rgba(214,232,234,0.34)" : "rgba(32,72,84,0.18)"));
        for (int i = 0; i < 80; i++)
            Wrap(R.Int(636), R.Int(600), 1, 16 + R.Int(46), C("rgba(130,170,178,0.16)"));
        for (int i = 0; i < 110; i++)
            Wrap(R.Int(638), R.Int(638), 2, 1, C("rgba(214,232,234,0.30)"));
        return p;
    }

    // 桥用条石（2.4m 见方，条石 0.8×0.4m）：暖灰石灰岩 + 倒角 + 水线潮湿痕（贴图底部）
    static Px BridgeStone()
    {
        var p = new Px(384, 384);
        p.FillRect(0, 0, 384, 384, C("#8f877a"));
        var cols = new[] { "#a89f92", "#a1988a", "#b0a798", "#988f82" };
        const int bw = 128, bh = 64;
        for (int row = 0; row < 6; row++)
            for (int col = -1; col < 5; col++)
            {
                float x = col * bw + (row % 2) * (bw / 2);
                float y = row * bh;
                p.FillRect(x + 2, y + 2, bw - 4, bh - 4, C(cols[R.Int(4)]));
                p.FillRect(x + 2, y + 2, bw - 4, 2, C("rgba(255,255,255,0.10)"));
                p.FillRect(x + 2, y + bh - 4, bw - 4, 2, C("rgba(0,0,0,0.16)"));
                for (int i = 0; i < 10; i++)
                    p.FillRect(x + 4 + R.Int(bw - 12), y + 4 + R.Int(bh - 12), 4 + R.Int(6), 2,
                        C(R.Int(2) == 0 ? "rgba(90,84,74,0.16)" : "rgba(226,220,208,0.18)"));
            }
        for (int i = 0; i < 30; i++)
            p.FillRect(R.Int(380), 300 + R.Int(80), 3 + R.Int(8), 2, C("rgba(60,78,64,0.14)"));
        return p;
    }

    // 木板（2m 见方，板宽 0.25m）：廊桥桥面 / 栏杆
    static Px Wood()
    {
        var p = new Px(320, 320);
        p.FillRect(0, 0, 320, 320, C("#8a6a4a"));
        var cols = new[] { "#96754f", "#8a6a48", "#9d7c55", "#82633f" };
        for (int b = 0; b < 8; b++)
        {
            float x = b * 40;
            p.FillRect(x + 1, 0, 38, 320, C(cols[R.Int(4)]));
            p.FillRect(x, 0, 2, 320, C("rgba(60,42,28,0.45)"));
            p.FillRect(x + 2, 0, 1, 320, C("rgba(255,240,220,0.06)"));
            for (int i = 0; i < 12; i++)
                p.FillRect(x + 3 + R.Int(34), R.Int(310), 1 + R.Int(2), 12 + R.Int(40),
                    C(R.Int(2) == 0 ? "rgba(60,44,30,0.18)" : "rgba(230,206,176,0.10)"));
            if (b % 2 == 0) p.FillRect(x + 6 + R.Int(24), R.Int(300), 4, 3, C("rgba(52,38,26,0.45)"));
        }
        for (int i = 0; i < 4; i++)
            p.FillRect(0, 60 + i * 70, 320, 8, C("rgba(50,36,24,0.18)"));
        return p;
    }

    // 钟面（1.2m 见方 @160px/米）：深色外圈 + 罗马刻度 + 10:10 指针
    static Px ClockFace()
    {
        var p = new Px(192, 192);
        // 圆盘外保持透明（材质走 Cutout），塔身/钟亭上只看到圆形钟面
        p.FillCircle(96, 96, 92, C("#3a2f28"));
        p.FillCircle(96, 96, 84, C("#f7f2e6"));
        p.StrokeCircle(96, 96, 88, 3, C("#b08a4a"));
        for (int i = 0; i < 12; i++)
        {
            float a = i / 12f * Mathf.PI * 2;
            float x = 96 + Mathf.Sin(a) * 70, y = 96 - Mathf.Cos(a) * 70;
            p.FillRect(x - 3, y - 3, 6, 6, C(i % 3 == 0 ? "#2f2620" : "#6b5a48"));
        }
        void Line(float x0, float y0, float x1, float y1, float w, Color c)
        {
            const int n = 30;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                p.FillRect(Mathf.Lerp(x0, x1, t) - w / 2, Mathf.Lerp(y0, y1, t) - w / 2, w, w, c);
            }
        }
        Line(96, 96, 118, 52, 6, C("#2f2620"));
        Line(96, 96, 78, 34, 5, C("#2f2620"));
        p.FillCircle(96, 96, 7, C("#2f2620"));
        p.FillCircle(96, 96, 3, C("#b08a4a"));
        return p;
    }

    // 从夜景立面提取“亮窗”做自发光贴图（降采样，避免黄昏时卡顿）
    static readonly Dictionary<Texture2D, Texture2D> EmCache = new Dictionary<Texture2D, Texture2D>();
    public static Texture2D EmissionFrom(Texture2D src, int step = 4)
    {
        if (src == null) return null;
        if (EmCache.TryGetValue(src, out var cached)) return cached;
        int w = Mathf.Max(1, src.width / step), h = Mathf.Max(1, src.height / step);
        var px = src.GetPixels();
        var outp = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var c = px[Mathf.Min(src.height - 1, y * step) * src.width + Mathf.Min(src.width - 1, x * step)];
                bool lit = c.r > 0.92f && c.g > 0.70f && c.b < 0.68f && c.r > c.b * 1.35f;
                outp[y * w + x] = lit ? c : Color.black;
            }
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.SetPixels(outp);
        t.Apply(false, false);
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;
        EmCache[src] = t;
        return t;
    }

    // 参数化条纹棚布（夏日祭摊位配色）：颜色对 + 缓存
    static readonly Dictionary<string, Texture2D> StripeCache = new Dictionary<string, Texture2D>();
    public static Texture2D Stripes2(string colorA, string colorB)
    {
        string key = colorA + "|" + colorB;
        if (StripeCache.TryGetValue(key, out var t)) return t;
        var p = new Px(64, 64);
        var a = C(colorA);
        var b = C(colorB);
        for (int x = 0; x < 64; x++) p.FillRect(x, 0, 1, 64, x % 16 < 8 ? a : b);
        t = From(p);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Repeat;
        StripeCache[key] = t;
        return t;
    }

    // ── 室内贴图（家 / 教学楼）──
    // 木地板：横向长条 + 错缝 + 细木纹。320px ≈ 2m，Repeat 后接缝自然
    static Px FloorWood()
    {
        var p = new Px(320, 320);
        var cols = new[] { "#b3906a", "#a8855f", "#bd9a73", "#9d7b57" };
        for (int r = 0; r < 8; r++)
        {
            float y = r * 40;
            p.FillRect(0, y, 320, 38, C(cols[R.Int(4)]));
            p.FillRect(0, y + 38, 320, 2, C("rgba(72,50,32,0.55)"));
            for (int i = 0; i < 16; i++)
                p.FillRect(R.Int(316), y + 3 + R.Int(30), 8 + R.Int(40), 1,
                    C(R.Int(2) == 0 ? "rgba(96,68,42,0.22)" : "rgba(240,224,196,0.12)"));
            float jx = r % 2 == 0 ? 96 + R.Int(60) : 208 + R.Int(60);
            p.FillRect(jx, y, 2, 38, C("rgba(72,50,32,0.5)"));
        }
        return p;
    }

    // 壁纸：米色底 + 细竖条 + 轻噪点（256px ≈ 1.6m）
    static Px WallPaper()
    {
        var p = new Px(256, 256);
        p.FillRect(0, 0, 256, 256, C("#ddd6c8"));
        for (int x = 0; x < 256; x += 48)   // 原来是 12px，太密像布纹
        {
            p.FillRect(x, 0, 2, 256, C("rgba(178,168,150,0.16)"));
            p.FillRect(x + 2, 0, 2, 256, C("rgba(255,252,244,0.12)"));
        }
        for (int i = 0; i < 90; i++)
            p.FillRect(R.Int(256), R.Int(256), 1, 1, C("rgba(150,140,124,0.12)"));
        return p;
    }

    // 瓷砖：64px 方格，浅灰双色错缝（卫生间 / 走廊）
    static Px FloorTile()
    {
        var p = new Px(256, 256);
        p.FillRect(0, 0, 256, 256, C("#c9cdd0"));
        for (int ty = 0; ty < 4; ty++)
            for (int tx = 0; tx < 4; tx++)
                p.FillRect(tx * 64 + 1, ty * 64 + 1, 62, 62,
                    C((tx + ty) % 2 == 0 ? "#dfe3e6" : "#d2d6d9"));
        return p;
    }

    // 天花板：白色微颗粒 + 板缝
    static Px Ceiling()
    {
        var p = new Px(256, 256);
        p.FillRect(0, 0, 256, 256, C("#e8e6e1"));
        for (int i = 0; i < 220; i++)
            p.FillRect(R.Int(256), R.Int(256), 2, 2, C("rgba(206,202,196,0.35)"));
        p.FillRect(0, 127, 256, 2, C("rgba(190,186,180,0.55)"));
        p.FillRect(127, 0, 2, 256, C("rgba(190,186,180,0.55)"));
        return p;
    }

    // 黑板：深绿板面 + 粉笔灰痕 + 木框
    static Px Blackboard()
    {
        var p = new Px(320, 160);
        p.FillRect(0, 0, 320, 160, C("#2f4a3a"));
        for (int i = 0; i < 160; i++)
            p.FillRect(R.Int(320), R.Int(160), 2 + R.Int(10), 1, C("rgba(220,226,214,0.05)"));
        p.FillRect(0, 0, 320, 6, C("#8a6a44"));
        p.FillRect(0, 154, 320, 6, C("#8a6a44"));
        p.FillRect(0, 0, 6, 160, C("#8a6a44"));
        p.FillRect(314, 0, 6, 160, C("#8a6a44"));
        return p;
    }

    // 墙纸②：淡蓝灰 + 细竖条（卧室/起居厅）
    static Px WallPaperBlue()
    {
        var p = new Px(256, 256);
        p.FillRect(0, 0, 256, 256, C("#b3c8d6"));
        for (int x = 0; x < 256; x += 56)   // 原来是 10px，太密像布纹
        {
            p.FillRect(x, 0, 2, 256, C("rgba(150,172,184,0.15)"));
            p.FillRect(x + 2, 0, 2, 256, C("rgba(255,255,255,0.11)"));
        }
        for (int i = 0; i < 70; i++)
            p.FillRect(R.Int(256), R.Int(256), 1, 1, C("rgba(255,255,255,0.20)"));
        return p;
    }

    // 墙纸③：暖褐 + 小花点（次卧/书房/储藏）
    static Px WallPaperWarm()
    {
        var p = new Px(256, 256);
        p.FillRect(0, 0, 256, 256, C("#cbb49a"));
        for (int y = 14; y < 256; y += 30)
            for (int x = 14; x < 256; x += 30)
                p.FillRect(x + R.Int(6), y + R.Int(6), 3, 3, C("rgba(146,116,90,0.35)"));
        for (int i = 0; i < 50; i++)
            p.FillRect(R.Int(256), R.Int(256), 1, 1, C("rgba(255,250,240,0.18)"));
        return p;
    }

    // 墙砖：白瓷砖 + 灰缝（厨房/卫生间）
    static Px WallTile()
    {
        var p = new Px(256, 256);
        p.FillRect(0, 0, 256, 256, C("#9aa0a6"));
        for (int ty = 0; ty < 4; ty++)
            for (int tx = 0; tx < 4; tx++)
                p.FillRect(tx * 64 + 2, ty * 64 + 2, 60, 60,
                    C((tx + ty) % 2 == 0 ? "#eef0f2" : "#e4e7ea"));
        return p;
    }

    // 木饰面：竖向窄板（书房/衣帽间）
    static Px WoodPanel()
    {
        var p = new Px(256, 256);
        var cols = new[] { "#96754f", "#8a6a48", "#9d7c55", "#82633f" };
        for (int b = 0; b < 8; b++)
        {
            float x = b * 32;
            p.FillRect(x + 1, 0, 30, 256, C(cols[R.Int(4)]));
            p.FillRect(x, 0, 2, 256, C("rgba(60,42,28,0.5)"));
            for (int i = 0; i < 8; i++)
                p.FillRect(x + 3 + R.Int(26), R.Int(250), 1, 10 + R.Int(30), C("rgba(60,44,30,0.16)"));
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
            "leaves" => Leaves(), "bark" => Bark(), "concrete" => Concrete(),
            "rain_streak" => RainStreak(), "snow_flake" => SnowFlake(), "snow_ground" => SnowGround(),
            "water" => Water(), "bridge_stone" => BridgeStone(), "wood" => Wood(), "clock" => ClockFace(),
            "track" => Track(), "pond" => Pond(), "sky_day" => SkyDay(), "sky_night" => SkyNight(),
            "lamp_glow" => LampGlow(), "roof_villa" => Roof("#3a434e"), "villa" => Villa(),
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

    // 名称分发：固定名 + 参数化名（建筑/边翼/屋面/远景外墙）；找不到返回 null 由调用方回退 Resources（招牌 PNG）
    static WorldData _world;
    static WorldData Wd => _world ?? (_world = CampusData.Build());

    // 当前世界（小镇/校园）：立面与屋面贴图按当前世界的建筑数据生成
    public static void SetWorld(WorldData w)
    {
        if (w != null && !ReferenceEquals(w, _world))
        {
            _world = w;
            Cache.Clear();          // 同一次运行切换世界时避免拿到另一世界的立面贴图
        }
    }

    public static Texture2D ByName(string name, bool pixel, bool repeat)
    {
        // 招牌（含中文，无法程序化重绘）：加载 PNG 后 2× 最近邻放大，与全局密度对齐
        if (name.StartsWith("sign_"))
        {
            var src = Resources.Load<Texture2D>("tex/" + name);
            if (src == null) return null;
            if (!src.isReadable)
            {
                // 导入设置没开 Read/Write：放大要取像素会抛异常 → 宁可退回 1× 原图（糊一点）也不让世界建一半
                Debug.LogError($"[TextureLibrary] 招牌贴图不可读（{name}）：请在编辑器跑「Campus/1b. 重设贴图导入（可读）」后重新构建");
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
            // 名字格式 wall_<id>_day / wing_<id>_night —— id 自身可能含下划线（小镇的 s_dorm / r_apt2）
            bool wing = name.StartsWith("wing_");
            string rest = name.Substring(wing ? 5 : 5);
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

    // 建筑/远景外墙需先按参数生成（非固定名）。wMeters/hMeters = 被贴面的世界尺寸（决定像素密度与画布比例）
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

    // ── 对拍：导出全部生成贴图为 PNG（Editor 调用）──
    public static void DumpAll(string dir)
    {
        Directory.CreateDirectory(dir);
        foreach (var n in new[] { "grass", "path", "flower", "dirt", "leaves", "bark", "concrete", "rain_streak", "snow_flake", "snow_ground", "track", "pond", "sky_day", "sky_night", "lamp_glow", "villa", "door", "awning", "court_basket", "court_tennis", "road_mainstone", "road_cobble", "roadband_v", "roadband_h", "water", "bridge_stone", "wood", "clock" })
        {
            var t = Get(n, true, true);
            File.WriteAllBytes(Path.Combine(dir, n + ".png"), t.EncodeToPNG());
        }
        var rv = RoofTex("#3a434e");
        File.WriteAllBytes(Path.Combine(dir, "roof_villa.png"), rv.EncodeToPNG());
        // 立面（按世界比例生成的整张贴图）——核对密度与美术比例
        foreach (var id in new[] { "teach", "gym", "kiosk" })
        {
            var b = Array.Find(Wd.BUILDINGS, x => x.id == id);
            if (b != null)
                File.WriteAllBytes(Path.Combine(dir, "wall_" + id + "_day.png"),
                    BuildingWall(b.wall, b.trim, b.plinth, b.cols, b.rows, b.banner, 0.05f, b.size[0], b.size[1]).EncodeToPNG());
        }
        Debug.Log("[TextureLibrary] 已导出对拍贴图 -> " + dir);
    }
}
