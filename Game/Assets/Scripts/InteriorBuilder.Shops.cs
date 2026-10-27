using UnityEngine;

// 小镇公共室内（单层）：图书馆 / 餐厅 / 超市
// 参考现代公共空间：
//   图书馆 —— 暖木、木格栅吊顶、整墙书墙、长桌 + 休闲阅读角、绿植
//   餐厅   —— 卡座、开放厨房/吧台、吊灯、酒架、暖色木作
//   超市   —— 生鲜/烘焙/冷藏/货架/收银分区，成列货架与清晰动线
public static partial class InteriorBuilder
{
    const float LIB_X = 1220f, REST_X = 1300f, MART_X = 1380f;

    // ── 通用小件 ──────────────────────────────────────────

    // 沿 X 的靠墙书架：face=+1 书脊朝 +z（靠北墙），face=-1 朝 -z（靠南墙）
    static void ShelfWallX(Transform p, Interior it, float z, float x0, float x1, int face,
                           Material frame, Material[] books, float h = 2.10f, float productScale = 1f)
    {
        const float d = 0.50f;
        float len = x1 - x0, cx = (x0 + x1) * 0.5f;
        Part(p, new Vector3(cx, h * 0.5f, z - face * (d * 0.5f - 0.02f)),
             new Vector3(len, h, 0.04f), frame, 1f);
        Part(p, new Vector3(x0 + 0.03f, 0.06f, z), new Vector3(0.06f, 0.12f, d), frame, 0.7f);
        Part(p, new Vector3(x1 - 0.03f, 0.06f, z), new Vector3(0.06f, 0.12f, d), frame, 0.7f);
        Part(p, new Vector3(cx, h - 0.03f, z), new Vector3(len, 0.06f, d), frame, 1f);
        Part(p, new Vector3(cx, 0.06f, z), new Vector3(len, 0.12f, d), frame, 1f);
        for (int r = 1; r <= 3; r++)
            Part(p, new Vector3(cx, 0.12f + r * 0.48f, z),
                 new Vector3(len - 0.12f, 0.04f, d - 0.06f), frame, 1f);

        float step = 0.17f * (productScale < 0.9f ? 0.76f : 1f);
        int n = Mathf.FloorToInt((len - 0.24f) / step);
        for (int r = 0; r < 4; r++)
        {
            float y = r == 0 ? 0.12f : 0.12f + r * 0.48f + 0.02f;
            for (int i = 0; i < n; i++)
            {
                if ((i + r * 5) % 11 == 7) continue;
                float bw = (0.10f + ((i * 7 + r * 3) % 4) * 0.014f) * productScale;
                float bh = (0.28f + ((i * 5 + r) % 4) * 0.022f) * productScale;
                float bx = x1 - 0.14f - i * step - bw * 0.5f;
                Part(p, new Vector3(bx, y + bh * 0.5f + 0.002f, z + face * 0.14f),
                     new Vector3(bw, bh, 0.24f * productScale), books[(i + r * 2) % books.Length], 0.5f);
            }
        }
        Reg(it, cx, z, len, d);
    }

    // 沿 Z 的靠墙书架：face=+1 书脊朝 +x（靠西墙），face=-1 朝 -x（靠东墙）
    static void ShelfWallZ(Transform p, Interior it, float x, float z0, float z1, int face,
                           Material frame, Material[] books, float h = 2.10f, float productScale = 1f)
    {
        const float d = 0.50f;
        float len = z1 - z0, cz = (z0 + z1) * 0.5f;
        Part(p, new Vector3(x - face * (d * 0.5f - 0.02f), h * 0.5f, cz),
             new Vector3(0.04f, h, len), frame, 1f);
        Part(p, new Vector3(x, 0.06f, z0 + 0.03f), new Vector3(d, 0.12f, 0.06f), frame, 0.7f);
        Part(p, new Vector3(x, 0.06f, z1 - 0.03f), new Vector3(d, 0.12f, 0.06f), frame, 0.7f);
        Part(p, new Vector3(x, h - 0.03f, cz), new Vector3(d, 0.06f, len), frame, 1f);
        Part(p, new Vector3(x, 0.06f, cz), new Vector3(d, 0.12f, len), frame, 1f);
        for (int r = 1; r <= 3; r++)
            Part(p, new Vector3(x, 0.12f + r * 0.48f, cz),
                 new Vector3(d - 0.06f, 0.04f, len - 0.12f), frame, 1f);

        float step = 0.17f * (productScale < 0.9f ? 0.76f : 1f);
        int n = Mathf.FloorToInt((len - 0.24f) / step);
        for (int r = 0; r < 4; r++)
        {
            float y = r == 0 ? 0.12f : 0.12f + r * 0.48f + 0.02f;
            for (int i = 0; i < n; i++)
            {
                if ((i + r * 5) % 11 == 7) continue;
                float bw = (0.10f + ((i * 7 + r * 3) % 4) * 0.014f) * productScale;
                float bh = (0.28f + ((i * 5 + r) % 4) * 0.022f) * productScale;
                float bz = z1 - 0.14f - i * step - bw * 0.5f;
                Part(p, new Vector3(x + face * 0.14f, y + bh * 0.5f + 0.002f, bz),
                     new Vector3(0.24f * productScale, bh, bw), books[(i + r * 2) % books.Length], 0.5f);
            }
        }
        Reg(it, x, cz, d, len);
    }

    static void BookStack(Transform p, Vector3 pos, int n = 3)
    {
        var cols = new[] { C("#8c4a3c"), C("#3f5c6b"), C("#8a7440"),
                           C("#6b7a4a"), C("#a9a08a"), C("#5b4a6b") };
        for (int i = 0; i < n; i++)
            Part(p, pos + new Vector3(0f, i * 0.055f, 0f),
                 new Vector3(0.42f, 0.05f, 0.30f), cols[i % cols.Length], 0.5f);
    }

    static void TableLamp(Transform p, Vector3 pos, string shade = "#e3d3a8")
    {
        Part(p, pos, new Vector3(0.18f, 0.04f, 0.18f), C("#6f5540"), 0.5f);
        Part(p, pos + new Vector3(0f, 0.18f, 0f), new Vector3(0.035f, 0.34f, 0.035f), C("#8d8d94"), 1f);
        Part(p, pos + new Vector3(0f, 0.38f, 0f), new Vector3(0.28f, 0.16f, 0.28f), C(shade), 1f);
        Part(p, pos + new Vector3(0f, 0.30f, 0f), new Vector3(0.30f, 0.02f, 0.30f), C("#8a7440"), 1f);
    }

    // 吊灯（暖光）
    static void Pendant(Transform p, float x, float y, float z, float drop, string shade = "#d8c8a8")
    {
        Part(p, new Vector3(x, y + drop * 0.5f, z), new Vector3(0.025f, drop, 0.025f), C("#2f3338"), 1f);
        Part(p, new Vector3(x, y, z), new Vector3(0.52f, 0.20f, 0.52f), C(shade), 1f);
        Part(p, new Vector3(x, y + 0.13f, z), new Vector3(0.42f, 0.03f, 0.42f), C("#fff2d6"), 1f);
        var go = new GameObject("pendantlight");
        go.transform.SetParent(p, false);
        go.transform.localPosition = new Vector3(x, y - 0.10f, z);
        var li = go.AddComponent<Light>();
        li.type = LightType.Point;
        li.range = 6.5f;
        li.intensity = 0.9f;
        li.color = MatLib.Hex("#ffdcae");
        li.shadows = LightShadows.None;
    }

    static void Stool(Transform p, Interior it, float x, float z, Material seat)
    {
        Part(p, new Vector3(x, 0.62f, z), new Vector3(0.40f, 0.06f, 0.40f), seat, 0.8f);
        Part(p, new Vector3(x, 0.31f, z), new Vector3(0.06f, 0.62f, 0.06f), C("#7a7f86"), 1f);
        Part(p, new Vector3(x, 0.03f, z), new Vector3(0.30f, 0.06f, 0.30f), C("#5a6068"), 1f);
        Reg(it, x, z, 0.42f, 0.42f);
    }

    // 墙牌：face=-1 装在 z=max 的南墙（朝室内 -z），face=+1 装在北墙
    static void WallSign(Transform p, float x, float y, float wallZ, int face, string label,
                         float w = 2.2f, float h = 0.62f, Material plate = null, float textH = 0.030f)
    {
        plate ??= C("#2f4a3a");
        float z = wallZ + face * 0.16f;
        Part(p, new Vector3(x, y, z), new Vector3(w, h, 0.05f), plate, 1f);
        Part(p, new Vector3(x, y, z + face * 0.035f),
             new Vector3(w - 0.20f, h - 0.14f, 0.012f), C("#dfe6dc"), 1f);
        SignText(p, new Vector3(x, y, z + face * 0.055f), face > 0 ? 180f : 0f,
                 label, textH, MatLib.Hex("#1f3428"));
    }

    // 吊牌（挂在空中，朝 face 方向）
    static void HangingSign(Transform p, float x, float y, float z, int face, string label,
                            float w = 1.7f, float h = 0.42f, Material plate = null, float textH = 0.028f)
    {
        plate ??= C("#2f4a3a");
        Part(p, new Vector3(x, y, z), new Vector3(w, h, 0.05f), plate, 1f);
        Part(p, new Vector3(x, y, z + face * 0.035f),
             new Vector3(w - 0.16f, h - 0.12f, 0.012f), C("#dfe6dc"), 1f);
        SignText(p, new Vector3(x, y, z + face * 0.055f), face > 0 ? 180f : 0f,
                 label, textH, MatLib.Hex("#1f3428"));
        Part(p, new Vector3(x - w * 0.35f, y + h * 0.5f + 0.35f, z),
             new Vector3(0.02f, 0.7f, 0.02f), C("#6a7078"), 1f);
        Part(p, new Vector3(x + w * 0.35f, y + h * 0.5f + 0.35f, z),
             new Vector3(0.02f, 0.7f, 0.02f), C("#6a7078"), 1f);
    }

    // 墙纸 + 踢脚线（doorSide：0 北墙、1 南墙、2 西墙、3 东墙）
    static void PaperRoom(Transform p, float x1, float x2, float z1, float z2, float h,
                          Material paper, int doorSide, float dA, float dB)
    {
        var hx = new[] { (dA, dB) };
        var hz = new[] { (dA, dB) };
        WallDeco(p, x1 + 0.11f, x2 - 0.11f, z1 + 0.11f, 0.02f, h, 0, 0.05f, paper, 1.7f,
                 doorSide == 0 ? hx : null);
        WallDeco(p, x1 + 0.11f, x2 - 0.11f, z2 - 0.11f, 0.02f, h, 0, 0.05f, paper, 1.7f,
                 doorSide == 1 ? hx : null);
        WallDeco(p, z1 + 0.11f, z2 - 0.11f, x1 + 0.11f, 0.02f, h, 1, 0.05f, paper, 1.7f,
                 doorSide == 2 ? hz : null);
        WallDeco(p, z1 + 0.11f, z2 - 0.11f, x2 - 0.11f, 0.02f, h, 1, 0.05f, paper, 1.7f,
                 doorSide == 3 ? hz : null);

        void RunX(float a0, float a1, float z, bool door)
        {
            if (!door) { Skirt(p, a0, z, a1, z, 0.02f); return; }
            if (dA - 0.08f > a0) Skirt(p, a0, z, dA - 0.08f, z, 0.02f);
            if (dB + 0.08f < a1) Skirt(p, dB + 0.08f, z, a1, z, 0.02f);
        }
        void RunZ(float a0, float a1, float x, bool door)
        {
            if (!door) { Skirt(p, x, a0, x, a1, 0.02f); return; }
            if (dA - 0.08f > a0) Skirt(p, x, a0, x, dA - 0.08f, 0.02f);
            if (dB + 0.08f < a1) Skirt(p, x, dB + 0.08f, x, a1, 0.02f);
        }
        RunX(x1 + 0.16f, x2 - 0.16f, z1 + 0.16f, doorSide == 0);
        RunX(x1 + 0.16f, x2 - 0.16f, z2 - 0.16f, doorSide == 1);
        RunZ(z1 + 0.16f, z2 - 0.16f, x1 + 0.16f, doorSide == 2);
        RunZ(z1 + 0.16f, z2 - 0.16f, x2 - 0.16f, doorSide == 3);
    }

    static void DoorFrame(Transform p, float x, float z, float halfW, float h, int face)
    {
        var fr = C("#6f5540");
        Part(p, new Vector3(x - halfW - 0.09f, h * 0.5f, z), new Vector3(0.18f, h, 0.34f), fr, 1f);
        Part(p, new Vector3(x + halfW + 0.09f, h * 0.5f, z), new Vector3(0.18f, h, 0.34f), fr, 1f);
        Part(p, new Vector3(x, h + 0.09f, z), new Vector3(halfW * 2f + 0.36f, 0.18f, 0.34f), fr, 1f);
        Part(p, new Vector3(x, 0.06f, z + face * 0.55f),
             new Vector3(halfW * 2f - 0.1f, 0.04f, 0.9f), C("#8d9aa6"), 1f);
    }

    // 收银台（长轴沿 X；face=+1 顾客在 +z 侧）
    static void Checkout(Transform p, Interior it, float x, float z, int face)
    {
        var white = C("#e6e6e2");
        var stone = C("#d9d3c6");
        Part(p, new Vector3(x, 0.48f, z), new Vector3(2.4f, 0.92f, 0.85f), white, 1f);
        Part(p, new Vector3(x, 0.96f, z), new Vector3(2.5f, 0.07f, 0.9f), stone, 1f);
        Part(p, new Vector3(x - face * 0.12f, 1.00f, z + face * 0.20f),
             new Vector3(1.5f, 0.035f, 0.28f), C("#3a3f47"), 1f);
        Part(p, new Vector3(x + 0.82f, 1.20f, z - face * 0.10f),
             new Vector3(0.46f, 0.30f, 0.30f), C("#2f3338"), 1f);
        Part(p, new Vector3(x + 0.82f, 1.38f, z - face * 0.22f),
             new Vector3(0.38f, 0.22f, 0.02f), C("#a8c0c8"), 1f);
        Part(p, new Vector3(x - 0.78f, 1.04f, z - face * 0.15f),
             new Vector3(0.24f, 0.08f, 0.18f), C("#8d8d94"), 1f);
        Part(p, new Vector3(x - 0.78f, 1.14f, z - face * 0.15f),
             new Vector3(0.16f, 0.12f, 0.02f), C("#d8e4e8"), 1f);
        Reg(it, x, z, 2.5f, 0.9f);
    }

    // 生鲜台：木台 + 三个斜口货箱 + 彩色果蔬块
    static void ProduceTable(Transform p, Interior it, float x, float z, float w, float d)
    {
        var wood = C("#8a6a4a");
        Part(p, new Vector3(x, 0.42f, z), new Vector3(w, 0.84f, d), wood, 1f);
        Part(p, new Vector3(x, 0.87f, z), new Vector3(w + 0.08f, 0.06f, d + 0.08f), C("#b08a5e"), 1f);
        var cols = new[] { C("#7a3c32"), C("#5f7a3f"), C("#c07a32"), C("#9a5a6a"), C("#d0a848") };
        int n = Mathf.Max(2, Mathf.FloorToInt(w / 0.95f));
        for (int i = 0; i < n; i++)
        {
            float cx = x - w * 0.5f + (i + 0.5f) * w / n;
            Part(p, new Vector3(cx, 0.96f, z), new Vector3(w / n - 0.10f, 0.16f, d - 0.16f), C("#9c7a52"), 0.7f);
            Part(p, new Vector3(cx, 1.06f, z), new Vector3(w / n - 0.18f, 0.08f, d - 0.26f), cols[i % cols.Length], 0.4f);
            for (int k = 0; k < 3; k++)
                Part(p, new Vector3(cx - 0.18f + k * 0.18f, 1.14f, z + (k % 2 == 0 ? -0.10f : 0.10f)),
                     new Vector3(0.13f, 0.13f, 0.13f), cols[(i + k) % cols.Length], 0.3f);
        }
        Reg(it, x, z, w, d);
    }

    // 冷藏柜（沿东墙，正面朝 -x）
    static void Cooler(Transform p, Interior it, float x, float z, float w = 1.7f)
    {
        var white = C("#e8e8e4");
        Part(p, new Vector3(x, 1.10f, z), new Vector3(0.70f, 2.20f, w), white, 1f);
        Part(p, new Vector3(x - 0.355f, 1.15f, z), new Vector3(0.02f, 1.80f, w - 0.18f), Glass(), 1f);
        Part(p, new Vector3(x - 0.36f, 2.10f, z), new Vector3(0.05f, 0.08f, w - 0.10f), C("#b8c4c8"), 1f);
        var cols = new[] { C("#7a3c32"), C("#3f5c6b"), C("#6b7a4a"), C("#d0a848"), C("#8a5a6a"), C("#c8c4ba") };
        for (int r = 0; r < 4; r++)
        {
            float y = 0.30f + r * 0.44f;
            Part(p, new Vector3(x - 0.18f, y - 0.03f, z), new Vector3(0.30f, 0.03f, w - 0.22f), C("#c9ced3"), 1f);
            for (int i = 0; i < 5; i++)
            {
                float bz = z - w * 0.5f + 0.22f + i * (w - 0.44f) / 4f;
                Part(p, new Vector3(x - 0.10f, y + 0.16f, bz),
                     new Vector3(0.16f, 0.26f, 0.12f), cols[(i + r) % cols.Length], 0.3f);
            }
        }
        Reg(it, x, z, 0.8f, w + 0.1f);
    }

    // ═════════════════════════════════════════════════════
    static Interior Library(Transform parent)
    {
        float ox = LIB_X;
        const float W = 4.6f, ZL = -7f, ZH = 7f;
        var it = new Interior
        {
            Id = "library", Name = "图书馆",
            X1 = ox - 11f, X2 = ox + 11f, Z1 = ZL, Z2 = ZH,
            Spawn = new Vector2(ox, 5.2f), Exit = new Vector2(ox, 6.25f),
            FloorY = new[] { 0.02f },
        };
        var root = new GameObject("Interior_library");
        root.transform.SetParent(parent, false);
        it.Root = root;
        var p = root.transform;

        var wall = T("in_wall3");
        var wood = T("wood");
        var woodDark = C("#6f5540");
        var panel = C("#7b5a3f");
        var sage = C("#7d8a76");
        var cream = C("#dfe6dc");
        var books = new[] { C("#8c4a3c"), C("#3f5c6b"), C("#6b7a4a"),
                            C("#8a7440"), C("#5b4a6b"), C("#a9a08a"), C("#b06a4e") };

        Part(p, new Vector3(ox, 0f, 0f), new Vector3(22f, 0.04f, 14f), T("in_floor"), 2f, default, true);
        Part(p, new Vector3(ox, W - 0.03f, 0f), new Vector3(22f, 0.06f, 14f), T("in_ceil"), 2f);

        WallX(p, it, ox - 11f, ox + 11f, ZL, 0f, W, 0f, 0f, 2.7f, wall);
        WallX(p, it, ox - 11f, ox + 11f, ZH, 0f, W, ox - 1.3f, ox + 1.3f, 2.7f, wall);
        WallZ(p, it, ZL, ZH, ox - 11f, 0f, W, 0f, 0f, 2.7f, wall);
        WallZ(p, it, ZL, ZH, ox + 11f, 0f, W, 0f, 0f, 2.7f, wall);
        DoorFrame(p, ox, ZH, 1.3f, 2.7f, -1);
        WallSign(p, ox, 3.38f, ZH, -1, "图书馆", 2.6f, 0.62f, C("#2f4a3a"), 0.038f);
        PaperRoom(p, ox - 11f, ox + 11f, ZL, ZH, W, wall, 1, ox - 1.3f, ox + 1.3f);

        // 木格栅吊顶：现代图书馆的暖木顶 + 线性灯
        for (float z = -6f; z <= 6f; z += 1.5f)
            Part(p, new Vector3(ox, W - 0.16f, z), new Vector3(21.4f, 0.08f, 0.16f), woodDark, 1f);
        for (int i = -1; i <= 1; i++)
            Part(p, new Vector3(ox + i * 6.4f, W - 0.22f, 0f), new Vector3(0.10f, 0.04f, 10.0f), C("#fff2d6"), 1f);

        // 窗：北面两扇大窗 + 南面两扇 + 东西高窗
        Window(p, new Vector3(ox - 6.0f, 2.7f, -6.80f), 3.2f, 2.2f, 0f);
        Window(p, new Vector3(ox + 6.0f, 2.7f, -6.80f), 3.2f, 2.2f, 0f);
        Window(p, new Vector3(ox - 6.0f, 2.5f, 6.80f), 2.8f, 1.9f, 180f);
        Window(p, new Vector3(ox + 6.0f, 2.5f, 6.80f), 2.8f, 1.9f, 180f);
        Window(p, new Vector3(ox + 10.80f, 2.9f, 1.8f), 2.2f, 1.4f, -90f);
        Window(p, new Vector3(ox - 10.80f, 2.9f, 4.6f), 2.2f, 1.4f, 90f);

        // 书墙：西墙整面、东墙北段、北墙两翼 + 北墙中央主书墙（从门口能一眼看到）
        ShelfWallZ(p, it, ox - 10.58f, -6.4f, 1.6f, 1, panel, books, 2.20f);
        ShelfWallZ(p, it, ox + 10.58f, -5.9f, -1.2f, -1, panel, books, 2.20f);
        ShelfWallX(p, it, -6.60f, ox - 10.35f, ox - 7.85f, 1, panel, books, 2.20f);
        ShelfWallX(p, it, -6.60f, ox + 7.85f, ox + 10.35f, 1, panel, books, 2.20f);
        ShelfWallX(p, it, -6.60f, ox - 2.4f, ox + 2.4f, 1, panel, books, 2.20f);

        // 北窗下阅读长凳（退到主书墙前 0.4m，不能插进书架）
        Part(p, new Vector3(ox, 0.28f, -5.95f), new Vector3(4.6f, 0.50f, 0.72f), wood, 1f);
        Part(p, new Vector3(ox, 0.57f, -5.95f), new Vector3(4.5f, 0.10f, 0.66f), C("#b9c8c0"), 1.2f);
        Reg(it, ox, -5.95f, 4.6f, 0.72f);

        // 中央阅读区长毯 + 长桌 ×2
        Rug(p, new Vector3(ox, 0.06f, 0.45f), 6.4f, 5.2f, C("#6f7d68"));
        foreach (float tz in new[] { -1.3f, 2.2f })
        {
            Table(p, new Vector3(ox, 0f, tz), 3.8f, 1.1f, 0.76f, wood);
            Reg(it, ox, tz, 3.8f, 1.1f);
            foreach (float dx in new[] { -1.25f, 0f, 1.25f })
            {
                Chair(p, it, new Vector3(ox + dx, 0f, tz - 0.80f), 0f, wood, true);
                Chair(p, it, new Vector3(ox + dx, 0f, tz + 0.80f), 180f, wood, true);
            }
            BookStack(p, new Vector3(ox - 1.3f, 0.84f, tz + 0.14f), 3);
            BookStack(p, new Vector3(ox + 0.4f, 0.84f, tz - 0.12f), 2);
            TableLamp(p, new Vector3(ox + 1.3f, 0.80f, tz + 0.20f));
            Pendant(p, ox, 3.30f, tz, 1.0f, "#e0d0b0");
        }

        // 借阅台（西南角，L 形）+ 还书箱 + 书车
        Part(p, new Vector3(ox - 6.8f, 0.52f, 5.35f), new Vector3(4.0f, 1.04f, 1.0f), woodDark, 1f);
        Part(p, new Vector3(ox - 6.8f, 1.08f, 5.35f), new Vector3(4.1f, 0.07f, 1.06f), cream, 1f);
        Part(p, new Vector3(ox - 8.55f, 0.52f, 3.95f), new Vector3(1.0f, 1.04f, 2.0f), woodDark, 1f);
        Part(p, new Vector3(ox - 8.55f, 1.08f, 3.95f), new Vector3(1.06f, 0.07f, 2.06f), cream, 1f);
        Reg(it, ox - 6.8f, 5.35f, 4.0f, 1.0f);
        Reg(it, ox - 8.55f, 3.95f, 1.0f, 2.0f);
        Part(p, new Vector3(ox - 6.8f, 1.18f, 4.82f), new Vector3(2.0f, 0.34f, 0.05f), C("#2f4a3a"), 1f);
        SignText(p, new Vector3(ox - 6.8f, 1.18f, 4.79f), 0f, "借阅台", 0.030f, MatLib.Hex("#1f3428"));
        Part(p, new Vector3(ox - 4.5f, 0.42f, 5.6f), new Vector3(0.7f, 0.84f, 0.7f), C("#8a6a4a"), 1f);
        Part(p, new Vector3(ox - 4.5f, 0.86f, 5.6f), new Vector3(0.6f, 0.06f, 0.6f), C("#3f5c6b"), 1f);
        Reg(it, ox - 4.5f, 5.6f, 0.7f, 0.7f);

        // 东侧休闲阅读角：双人沙发对坐 + 茶几 + 地毯
        Rug(p, new Vector3(ox + 7.4f, 0.06f, 4.2f), 4.6f, 3.4f, sage);
        Sofa(p, new Vector3(ox + 5.9f, 0f, 4.2f), 1.5f, 90f, C("#8f8571"));
        Sofa(p, new Vector3(ox + 9.1f, 0f, 4.2f), 1.5f, 270f, C("#8f8571"));
        Reg(it, ox + 5.9f, 4.2f, 1.0f, 1.5f);
        Reg(it, ox + 9.1f, 4.2f, 1.0f, 1.5f);
        Part(p, new Vector3(ox + 7.5f, 0.36f, 4.2f), new Vector3(0.95f, 0.08f, 0.65f), wood, 1f);
        Part(p, new Vector3(ox + 7.5f, 0.18f, 4.2f), new Vector3(0.75f, 0.36f, 0.48f), woodDark, 1f);
        Reg(it, ox + 7.5f, 4.2f, 0.95f, 0.65f);
        BookStack(p, new Vector3(ox + 7.5f, 0.44f, 4.2f), 2);
        TableLamp(p, new Vector3(ox + 8.8f, 0.02f, 5.35f));
        Plant(p, new Vector3(ox + 10.2f, 0.02f, 5.8f), 1.1f);

        // 新书展示台（进门东侧）
        Table(p, new Vector3(ox + 4.4f, 0f, 5.35f), 2.3f, 0.9f, 0.72f, wood);
        Reg(it, ox + 4.4f, 5.35f, 2.3f, 0.9f);
        BookStack(p, new Vector3(ox + 3.8f, 0.80f, 5.35f), 3);
        BookStack(p, new Vector3(ox + 4.6f, 0.80f, 5.42f), 4);
        BookStack(p, new Vector3(ox + 5.2f, 0.80f, 5.28f), 2);
        HangingSign(p, ox + 4.4f, 2.55f, 5.05f, -1, "新书", 1.2f, 0.38f, C("#4a6a52"), 0.026f);

        // 墙饰、绿植、挂钟
        Picture(p, new Vector3(ox + 10.83f, 2.2f, -3.6f), 1.4f, 1.0f, 270f);
        Picture(p, new Vector3(ox - 10.83f, 2.2f, 3.4f), 1.3f, 0.95f, 90f);
        WallClock(p, new Vector3(ox + 3.5f, 3.15f, 6.82f), 0f);
        Plant(p, new Vector3(ox - 10.15f, 0.02f, -6.2f), 1.2f);
        Plant(p, new Vector3(ox + 10.1f, 0.02f, -6.2f), 1.0f);
        Plant(p, new Vector3(ox - 10.1f, 0.02f, 6.1f), 1.0f);
        Lamp(p, new Vector3(ox - 5.2f, 4.3f, -3.2f), 12f, 1.25f);
        Lamp(p, new Vector3(ox + 5.2f, 4.3f, -3.2f), 12f, 1.25f);
        Lamp(p, new Vector3(ox - 5.2f, 4.3f, 3.2f), 12f, 1.25f);
        Lamp(p, new Vector3(ox + 5.2f, 4.3f, 3.2f), 12f, 1.25f);
        return it;
    }

    // ═════════════════════════════════════════════════════
    static Interior Restaurant(Transform parent)
    {
        float ox = REST_X;
        const float W = 4.2f, ZL = -7f, ZH = 7f;
        var it = new Interior
        {
            Id = "restaurant", Name = "餐厅",
            X1 = ox - 10f, X2 = ox + 10f, Z1 = ZL, Z2 = ZH,
            Spawn = new Vector2(ox, -5.4f), Exit = new Vector2(ox, -6.25f),
            FloorY = new[] { 0.02f },
        };
        var root = new GameObject("Interior_restaurant");
        root.transform.SetParent(parent, false);
        it.Root = root;
        var p = root.transform;

        var wall = T("in_wall3");
        var wood = T("wood");
        var woodDark = C("#5c4636");
        var green = C("#3f5c4a");
        var rust = C("#a85a42");
        var cream = C("#dfe6dc");
        var brass = C("#b08a4e");
        var fabric = C("#7d6a58");

        Part(p, new Vector3(ox, 0f, 0f), new Vector3(20f, 0.04f, 14f), T("in_floor"), 2f, default, true);
        Part(p, new Vector3(ox, 0.05f, 5.7f), new Vector3(20f, 0.02f, 2.6f), T("in_tile"), 1.4f);
        Part(p, new Vector3(ox, W - 0.03f, 0f), new Vector3(20f, 0.06f, 14f), T("in_ceil"), 2f);

        WallX(p, it, ox - 10f, ox + 10f, ZL, 0f, W, ox - 1.2f, ox + 1.2f, 2.6f, wall);
        WallX(p, it, ox - 10f, ox + 10f, ZH, 0f, W, 0f, 0f, 2.6f, wall);
        WallZ(p, it, ZL, ZH, ox - 10f, 0f, W, 0f, 0f, 2.6f, wall);
        WallZ(p, it, ZL, ZH, ox + 10f, 0f, W, 0f, 0f, 2.6f, wall);
        DoorFrame(p, ox, ZL, 1.2f, 2.6f, 1);
        WallSign(p, ox, 3.28f, ZL, 1, "餐厅", 2.2f, 0.60f, C("#5a3a30"), 0.036f);
        PaperRoom(p, ox - 10f, ox + 10f, ZL, ZH, W, wall, 0, ox - 1.2f, ox + 1.2f);

        // 吊顶木梁 + 吊灯
        for (float z = -5.5f; z <= 5.5f; z += 2.2f)
            Part(p, new Vector3(ox, W - 0.14f, z), new Vector3(19.4f, 0.10f, 0.14f), C("#4a3a30"), 1f);

        // 窗
        Window(p, new Vector3(ox - 5.6f, 2.15f, -6.80f), 3.0f, 1.9f, 0f);
        Window(p, new Vector3(ox + 5.6f, 2.15f, -6.80f), 3.0f, 1.9f, 0f);
        Window(p, new Vector3(ox - 9.80f, 2.7f, -4.2f), 2.2f, 1.3f, 90f);
        Window(p, new Vector3(ox - 9.80f, 2.7f, 2.2f), 2.2f, 1.3f, 90f);

        // 北墙入口区：迎宾台 + 等位长椅 + 衣帽架
        Part(p, new Vector3(ox + 3.6f, 0.55f, -5.75f), new Vector3(1.35f, 1.10f, 0.60f), woodDark, 1f);
        Part(p, new Vector3(ox + 3.6f, 1.12f, -5.75f), new Vector3(1.45f, 0.06f, 0.66f), brass, 1f);
        Part(p, new Vector3(ox + 3.6f, 1.22f, -5.62f), new Vector3(0.42f, 0.30f, 0.04f), cream, 1f);
        Reg(it, ox + 3.6f, -5.75f, 1.35f, 0.60f);
        Sofa(p, new Vector3(ox - 4.2f, 0f, -5.85f), 1.8f, 0f, green);
        Reg(it, ox - 4.2f, -5.85f, 1.8f, 0.95f);
        Part(p, new Vector3(ox + 7.8f, 1.05f, -6.4f), new Vector3(0.06f, 2.0f, 0.06f), C("#6a7078"), 1f);
        Part(p, new Vector3(ox + 7.8f, 1.95f, -6.4f), new Vector3(0.7f, 0.06f, 0.06f), C("#6a7078"), 1f);
        foreach (float hx in new[] { -0.28f, 0.28f })
            Part(p, new Vector3(ox + 7.8f + hx, 1.78f, -6.4f), new Vector3(0.04f, 0.25f, 0.04f), brass, 1f);

        // 西墙卡座：长沙发 + 三张双人桌 + 三盏吊灯
        Sofa(p, new Vector3(ox - 9.38f, 0f, 0.6f), 5.2f, 90f, fabric);
        Reg(it, ox - 9.38f, 0.6f, 0.95f, 5.2f);
        foreach (float tz in new[] { -1.5f, 0.6f, 2.7f })
        {
            Table(p, new Vector3(ox - 8.15f, 0f, tz), 1.0f, 0.85f, 0.76f, wood);
            Reg(it, ox - 8.15f, tz, 1.0f, 0.85f);
            Chair(p, it, new Vector3(ox - 7.0f, 0f, tz), 270f, wood, true);
            TableLamp(p, new Vector3(ox - 8.15f, 0.80f, tz + 0.22f));
            Pendant(p, ox - 8.15f, 2.85f, tz, 1.1f, "#d8c8a8");
        }

        // 南侧开放厨房 + 吧台
        Part(p, new Vector3(ox, 0.49f, 6.0f), new Vector3(17.2f, 0.98f, 0.9f), woodDark, 1f);
        Part(p, new Vector3(ox, 1.01f, 6.0f), new Vector3(17.4f, 0.07f, 0.98f), cream, 1f);
        Reg(it, ox, 6.0f, 17.2f, 0.9f);
        for (float sx = -8.2f; sx <= 8.2f; sx += 0.55f)
            Part(p, new Vector3(ox + sx, 0.45f, 5.54f), new Vector3(0.07f, 0.82f, 0.06f), C("#7a6248"), 1f);
        // 后厨设备：灶台 / 抽油烟机 / 水槽 / 冷柜 / 出餐架
        Part(p, new Vector3(ox + 2.2f, 1.06f, 6.35f), new Vector3(2.8f, 0.08f, 0.65f), C("#2f3338"), 1f);
        foreach (float bx in new[] { 1.2f, 2.2f, 3.2f })
            Part(p, new Vector3(ox + bx, 1.12f, 6.35f), new Vector3(0.42f, 0.06f, 0.42f), C("#8d8d94"), 1f);
        Part(p, new Vector3(ox + 2.2f, 2.55f, 6.45f), new Vector3(3.4f, 0.55f, 0.75f), C("#3a3f47"), 1f);
        Part(p, new Vector3(ox + 2.2f, 2.95f, 6.55f), new Vector3(1.1f, 0.55f, 0.55f), C("#5a6068"), 1f);
        Part(p, new Vector3(ox - 3.2f, 0.55f, 6.45f), new Vector3(1.8f, 0.9f, 0.8f), C("#9aa0a6"), 1f);
        Part(p, new Vector3(ox - 3.2f, 0.96f, 6.45f), new Vector3(1.5f, 0.05f, 0.6f), C("#3a3f47"), 1f);
        Part(p, new Vector3(ox - 7.6f, 1.15f, 6.4f), new Vector3(1.5f, 2.3f, 0.8f), C("#c9ced3"), 1f);
        Part(p, new Vector3(ox - 7.55f, 1.15f, 5.99f), new Vector3(1.35f, 2.0f, 0.03f), C("#8d949c"), 1f);
        Part(p, new Vector3(ox - 0.6f, 1.75f, 6.72f), new Vector3(6.4f, 0.07f, 0.4f), wood, 1f);
        foreach (float px in new[] { -2.8f, -1.7f, -0.6f, 0.5f, 1.6f })
            Part(p, new Vector3(ox + px, 1.84f, 6.72f), new Vector3(0.34f, 0.06f, 0.26f), cream, 1f);
        // 酒架（东墙）
        Part(p, new Vector3(ox + 9.55f, 1.2f, 1.5f), new Vector3(0.5f, 2.4f, 3.2f), woodDark, 1f);
        for (int r = 0; r < 4; r++)
        {
            Part(p, new Vector3(ox + 9.30f, 0.45f + r * 0.50f, 1.5f),
                 new Vector3(0.06f, 0.04f, 3.0f), wood, 1f);
            for (int i = 0; i < 7; i++)
                Part(p, new Vector3(ox + 9.24f, 0.62f + r * 0.50f, 0.25f + i * 0.40f),
                     new Vector3(0.10f, 0.30f, 0.12f), i % 3 == 0 ? C("#5b4a6b") : C("#3f5c6b"), 0.3f);
        }
        Reg(it, ox + 9.55f, 1.5f, 0.5f, 3.2f);

        // 吧台高脚凳
        foreach (float sx in new[] { -4.2f, -1.4f, 1.4f, 4.2f })
            Stool(p, it, ox + sx, 4.95f, C("#8a6a4a"));

        // 中部散桌 ×3（每桌 4 椅）+ 地毯；西侧再放一张共享长桌，填住进门左半边
        Rug(p, new Vector3(ox + 4.2f, 0.06f, 0.4f), 7.0f, 5.2f, C("#5a4a3e"));
        Rug(p, new Vector3(ox - 3.8f, 0.06f, 1.2f), 4.8f, 3.2f, C("#6b5a48"));
        Table(p, new Vector3(ox - 3.8f, 0f, 1.2f), 3.2f, 1.0f, 0.76f, wood);
        Reg(it, ox - 3.8f, 1.2f, 3.2f, 1.0f);
        foreach (float cxx in new[] { -4.9f, -3.8f, -2.7f })
        {
            Chair(p, it, new Vector3(ox + cxx, 0f, 0.35f), 0f, wood, true);
            Chair(p, it, new Vector3(ox + cxx, 0f, 2.05f), 180f, wood, true);
        }
        TableLamp(p, new Vector3(ox - 3.8f, 0.80f, 1.35f));
        Pendant(p, ox - 3.8f, 2.75f, 1.2f, 1.25f, "#d8c8a8");
        foreach (var (tx, tz) in new[] { (2.3f, -2.2f), (6.2f, 0.4f), (3.4f, 2.5f) })
        {
            Table(p, new Vector3(ox + tx, 0f, tz), 1.5f, 1.0f, 0.76f, wood);
            Reg(it, ox + tx, tz, 1.5f, 1.0f);
            Chair(p, it, new Vector3(ox + tx - 0.38f, 0f, tz - 0.82f), 0f, wood, true);
            Chair(p, it, new Vector3(ox + tx + 0.38f, 0f, tz - 0.82f), 0f, wood, true);
            Chair(p, it, new Vector3(ox + tx - 0.38f, 0f, tz + 0.82f), 180f, wood, true);
            Chair(p, it, new Vector3(ox + tx + 0.38f, 0f, tz + 0.82f), 180f, wood, true);
            TableLamp(p, new Vector3(ox + tx, 0.80f, tz + 0.18f));
            Pendant(p, ox + tx, 2.75f, tz, 1.25f, "#d8c8a8");
        }

        // 墙饰、菜单、绿植
        Part(p, new Vector3(ox + 9.82f, 2.05f, -4.8f), new Vector3(0.05f, 1.3f, 2.0f), C("#2f3338"), 1f);
        Part(p, new Vector3(ox + 9.80f, 2.05f, -4.8f), new Vector3(0.02f, 1.1f, 1.8f), C("#3f5c4a"), 1f);
        foreach (float ly in new[] { 1.65f, 1.95f, 2.25f, 2.55f })
            Part(p, new Vector3(ox + 9.78f, ly, -4.8f), new Vector3(0.01f, 0.035f, 1.3f), cream, 1f);
        Picture(p, new Vector3(ox + 9.82f, 2.2f, 4.3f), 1.3f, 0.9f, 270f);
        Picture(p, new Vector3(ox - 9.82f, 2.3f, 5.7f), 1.2f, 0.9f, 90f);
        // 北墙今日推荐菜单（避开两扇窗）
        Part(p, new Vector3(ox - 8.4f, 2.15f, -6.84f), new Vector3(1.5f, 1.2f, 0.05f), C("#2f3338"), 1f);
        Part(p, new Vector3(ox - 8.4f, 2.15f, -6.81f), new Vector3(1.3f, 1.0f, 0.012f), C("#3f5c4a"), 1f);
        SignText(p, new Vector3(ox - 8.4f, 2.40f, -6.79f), 180f, "今日推荐", 0.028f, MatLib.Hex("#dfe6dc"));
        foreach (float ly in new[] { 2.10f, 1.95f, 1.80f })
            Part(p, new Vector3(ox - 8.4f, ly, -6.79f), new Vector3(0.9f, 0.025f, 0.01f), cream, 0.2f);
        WallClock(p, new Vector3(ox + 7.4f, 2.9f, -6.82f), 180f);
        Plant(p, new Vector3(ox - 9.2f, 0.02f, -6.1f), 1.15f);
        Plant(p, new Vector3(ox + 9.1f, 0.02f, 5.9f), 1.0f);
        Plant(p, new Vector3(ox + 0.6f, 0.02f, -6.0f), 0.9f);
        Lamp(p, new Vector3(ox - 4.5f, 3.9f, 2.0f), 11f, 1.10f);
        Lamp(p, new Vector3(ox + 4.5f, 3.9f, -3.0f), 11f, 1.10f);
        Lamp(p, new Vector3(ox + 5.5f, 3.9f, 3.8f), 11f, 1.10f);
        return it;
    }

    // ═════════════════════════════════════════════════════
    static Interior Supermarket(Transform parent)
    {
        float ox = MART_X;
        const float W = 3.8f, ZL = -6f, ZH = 6f;
        var it = new Interior
        {
            Id = "supermarket", Name = "超市",
            X1 = ox - 9f, X2 = ox + 9f, Z1 = ZL, Z2 = ZH,
            Spawn = new Vector2(ox, -4.25f), Exit = new Vector2(ox, -5.25f),
            FloorY = new[] { 0.02f },
        };
        var root = new GameObject("Interior_supermarket");
        root.transform.SetParent(parent, false);
        it.Root = root;
        var p = root.transform;

        var wall = T("in_wall2");
        var white = C("#e6e6e2");
        var metal = C("#9aa0a6");
        var dark = C("#2f3338");
        var green = C("#4a6a52");
        var cream = C("#dfe6dc");
        var wood = T("wood");
        var woodDark = C("#6f5540");
        var products = new[] { C("#8c4a3c"), C("#3f5c6b"), C("#6b7a4a"),
                               C("#d0a848"), C("#8a5a6a"), C("#c8c4ba"),
                               C("#b06a4e"), C("#7a9aa6") };

        Part(p, new Vector3(ox, 0f, 0f), new Vector3(18f, 0.04f, 12f), T("in_tile"), 2f, default, true);
        Part(p, new Vector3(ox, 0.05f, 0f), new Vector3(2.2f, 0.02f, 10.4f), C("#d6d0c4"), 1f);
        Part(p, new Vector3(ox, W - 0.03f, 0f), new Vector3(18f, 0.06f, 12f), T("in_ceil"), 2f);

        WallX(p, it, ox - 9f, ox + 9f, ZL, 0f, W, ox - 1.3f, ox + 1.3f, 2.6f, wall);
        WallX(p, it, ox - 9f, ox + 9f, ZH, 0f, W, 0f, 0f, 2.6f, wall);
        WallZ(p, it, ZL, ZH, ox - 9f, 0f, W, 0f, 0f, 2.6f, wall);
        WallZ(p, it, ZL, ZH, ox + 9f, 0f, W, 0f, 0f, 2.6f, wall);
        DoorFrame(p, ox, ZL, 1.3f, 2.6f, 1);
        WallSign(p, ox, 3.25f, ZL, 1, "超市", 2.2f, 0.62f, C("#2f4a3a"), 0.038f);
        PaperRoom(p, ox - 9f, ox + 9f, ZL, ZH, W, wall, 0, ox - 1.3f, ox + 1.3f);

        // 线性灯带
        for (float lx = -5f; lx <= 5f; lx += 5f)
            foreach (float lz in new[] { -2.2f, 1.8f })
                Part(p, new Vector3(ox + lx, W - 0.20f, lz), new Vector3(0.16f, 0.05f, 6.4f), C("#fff2d6"), 1f);

        // 前窗
        Window(p, new Vector3(ox - 6.0f, 1.95f, -5.80f), 2.4f, 1.6f, 0f);
        Window(p, new Vector3(ox + 6.0f, 1.95f, -5.80f), 2.4f, 1.6f, 0f);

        // 收银区：两台收银台靠两侧，入口到生鲜的中央视线保持通透
        Checkout(p, it, ox - 6.6f, -3.55f, 1);
        Checkout(p, it, ox + 6.6f, -3.55f, 1);
        HangingSign(p, ox - 6.6f, 2.95f, -2.65f, -1, "收银", 1.3f, 0.40f, C("#2f4a3a"), 0.027f);
        HangingSign(p, ox + 6.6f, 2.95f, -2.65f, -1, "收银", 1.3f, 0.40f, C("#2f4a3a"), 0.027f);
        foreach (float bx in new[] { -1.9f, 1.9f })
            for (int i = 0; i < 3; i++)
                Part(p, new Vector3(ox + bx, 0.16f + i * 0.14f, -4.55f),
                     new Vector3(0.44f, 0.12f, 0.32f), i % 2 == 0 ? C("#c85a42") : C("#d0a848"), 0.5f);
        // 购物车
        foreach (float cx in new[] { 2.7f, 3.25f })
        {
            Part(p, new Vector3(ox + cx, 0.55f, -4.55f), new Vector3(0.42f, 0.55f, 0.62f), C("#9aa0a6"), 1f);
            Part(p, new Vector3(ox + cx, 0.55f, -4.55f), new Vector3(0.34f, 0.42f, 0.52f), C("#d8d8d2"), 1f);
            Part(p, new Vector3(ox + cx, 0.20f, -4.55f), new Vector3(0.46f, 0.06f, 0.66f), dark, 1f);
        }

        // 生鲜区：入口正前方中央岛台（进门第一眼），西侧再补一组
        ProduceTable(p, it, ox, -1.35f, 4.2f, 1.35f);
        ProduceTable(p, it, ox - 6.2f, 1.15f, 2.5f, 1.2f);
        HangingSign(p, ox, 2.95f, -0.45f, -1, "生鲜", 1.4f, 0.40f, C("#4a6a52"), 0.027f);

        // 烘焙区（西墙南段）：玻璃柜 + 后架 + 暖光
        Part(p, new Vector3(ox - 7.7f, 0.55f, 3.75f), new Vector3(1.9f, 1.10f, 2.6f), wood, 1f);
        Part(p, new Vector3(ox - 7.7f, 1.14f, 3.75f), new Vector3(2.0f, 0.06f, 2.7f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox - 7.7f, 1.42f, 3.75f), new Vector3(1.84f, 0.55f, 2.54f), Glass(), 1f);
        foreach (float bz in new[] { 3.0f, 3.75f, 4.5f })
            Part(p, new Vector3(ox - 7.68f, 1.28f, bz), new Vector3(0.32f, 0.12f, 0.40f), C("#c08a4a"), 0.4f);
        Reg(it, ox - 7.7f, 3.75f, 1.9f, 2.6f);
        Part(p, new Vector3(ox - 8.55f, 1.35f, 3.75f), new Vector3(0.5f, 2.7f, 2.8f), woodDark, 1f);
        for (int r = 0; r < 3; r++)
            Part(p, new Vector3(ox - 8.30f, 0.55f + r * 0.52f, 3.75f),
                 new Vector3(0.06f, 0.04f, 2.5f), wood, 1f);
        Reg(it, ox - 8.55f, 3.75f, 0.5f, 2.8f);
        HangingSign(p, ox - 7.7f, 2.95f, 3.2f, -1, "烘焙", 1.4f, 0.40f, C("#8a5a3c"), 0.027f);
        Pendant(p, ox - 7.7f, 2.65f, 3.75f, 1.0f, "#e0d0b0");

        // 双面货架 ×2（沿 Z 成列，中央生鲜岛后侧；商品用 0.62 尺度，避免像书墙）
        var shelfFrame = C("#c8c4ba");
        ShelfWallZ(p, it, ox - 2.55f, 0.6f, 4.5f, -1, shelfFrame, products, 1.90f, 0.62f);
        ShelfWallZ(p, it, ox - 2.05f, 0.6f, 4.5f, 1, shelfFrame, products, 1.90f, 0.62f);
        ShelfWallZ(p, it, ox + 2.25f, 0.6f, 4.5f, -1, shelfFrame, products, 1.90f, 0.62f);
        ShelfWallZ(p, it, ox + 2.75f, 0.6f, 4.5f, 1, shelfFrame, products, 1.90f, 0.62f);
        HangingSign(p, ox - 2.3f, 2.95f, 0.15f, -1, "零食", 1.4f, 0.38f, C("#4a6a52"), 0.027f);
        HangingSign(p, ox + 2.5f, 2.95f, 0.15f, -1, "日用", 1.4f, 0.38f, C("#3f5c6b"), 0.027f);

        // 东墙冷藏柜 ×3
        foreach (float cz in new[] { -0.6f, 1.4f, 3.4f })
            Cooler(p, it, ox + 8.42f, cz);
        HangingSign(p, ox + 8.0f, 2.95f, 0.4f, -1, "饮料", 1.4f, 0.40f, C("#3f5c6b"), 0.027f);

        // 南墙品牌墙：填补从入口正对的空白，别让视野尽头只剩一堵墙
        Part(p, new Vector3(ox, 2.28f, 5.84f), new Vector3(8.6f, 1.7f, 0.04f), C("#4a6a52"), 1f);
        Part(p, new Vector3(ox, 2.28f, 5.81f), new Vector3(8.2f, 1.42f, 0.012f), cream, 1f);
        SignText(p, new Vector3(ox, 2.28f, 5.80f), 0f, "新鲜每一天", 0.045f, MatLib.Hex("#2f4a3a"));
        for (int i = 0; i < 5; i++)
            Part(p, new Vector3(ox - 2.6f + i * 1.3f, 1.70f, 5.79f),
                 new Vector3(0.22f, 0.22f, 0.01f), products[(i + 1) % products.Length], 0.2f);

        // 南墙：员工门 + 整箱货架
        Part(p, new Vector3(ox - 6.6f, 1.35f, 5.82f), new Vector3(1.2f, 2.7f, 0.06f), dark, 1f);
        Part(p, new Vector3(ox - 6.6f, 1.35f, 5.78f), new Vector3(0.10f, 0.12f, 0.03f), metal, 1f);
        HangingSign(p, ox - 6.6f, 2.95f, 5.2f, -1, "员工", 1.1f, 0.36f, C("#3f5c6b"), 0.024f);
        for (int r = 0; r < 2; r++)
            Part(p, new Vector3(ox + 4.0f, 0.55f + r * 1.05f, 5.60f),
                 new Vector3(5.0f, 0.08f, 0.5f), wood, 1f);
        Reg(it, ox + 4.0f, 5.60f, 5.0f, 0.5f);

        // 墙面装饰与安全标识
        Picture(p, new Vector3(ox + 8.83f, 1.9f, -3.9f), 1.2f, 0.7f, 270f);
        Part(p, new Vector3(ox - 8.83f, 2.4f, -4.5f), new Vector3(0.04f, 0.5f, 1.2f), C("#4a6a52"), 1f);
        Part(p, new Vector3(ox - 8.80f, 2.4f, -4.5f), new Vector3(0.01f, 0.34f, 0.9f), cream, 1f);
        Lamp(p, new Vector3(ox - 4.5f, 3.55f, -1.0f), 11f, 1.10f);
        Lamp(p, new Vector3(ox + 4.5f, 3.55f, -1.0f), 11f, 1.10f);
        Lamp(p, new Vector3(ox - 4.5f, 3.55f, 3.0f), 11f, 1.10f);
        Lamp(p, new Vector3(ox + 4.5f, 3.55f, 3.0f), 11f, 1.10f);
        Lamp(p, new Vector3(ox, 3.55f, -1.5f), 11f, 1.05f);
        Lamp(p, new Vector3(ox, 3.55f, 3.5f), 11f, 1.05f);
        return it;
    }
}
