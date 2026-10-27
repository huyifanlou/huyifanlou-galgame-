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
        // 开放式冷柜：背板 + 顶底 + 侧板，前方留空露商品，避免实心白盒/不透明玻璃
        Part(p, new Vector3(x + 0.32f, 1.10f, z), new Vector3(0.06f, 2.20f, w), white, 1f);
        Part(p, new Vector3(x, 2.18f, z), new Vector3(0.70f, 0.06f, w), white, 1f);
        Part(p, new Vector3(x, 0.02f, z), new Vector3(0.70f, 0.06f, w), white, 1f);
        foreach (float ez in new[] { z - w * 0.5f + 0.03f, z + w * 0.5f - 0.03f })
            Part(p, new Vector3(x, 1.10f, ez), new Vector3(0.70f, 2.20f, 0.06f), white, 1f);
        Part(p, new Vector3(x - 0.34f, 2.10f, z), new Vector3(0.05f, 0.08f, w - 0.10f), C("#b8c4c8"), 1f);
        Part(p, new Vector3(x - 0.34f, 0.30f, z), new Vector3(0.05f, 0.08f, w - 0.10f), C("#b8c4c8"), 1f);
        foreach (float ez in new[] { z - w * 0.5f + 0.08f, z + w * 0.5f - 0.08f })
            Part(p, new Vector3(x - 0.34f, 1.20f, ez), new Vector3(0.05f, 1.75f, 0.05f), C("#b8c4c8"), 1f);
        var cols = new[] { C("#7a3c32"), C("#3f5c6b"), C("#6b7a4a"), C("#d0a848"), C("#8a5a6a"), C("#c8c4ba") };
        for (int r = 0; r < 4; r++)
        {
            float y = 0.30f + r * 0.44f;
            Part(p, new Vector3(x - 0.18f, y - 0.03f, z), new Vector3(0.30f, 0.03f, w - 0.22f), C("#c9ced3"), 1f);
            for (int i = 0; i < 7; i++)
            {
                float bz = z - w * 0.5f + 0.16f + i * (w - 0.32f) / 6f;
                Part(p, new Vector3(x - 0.10f, y + 0.17f, bz),
                     new Vector3(0.16f, 0.30f, 0.13f), cols[(i + r) % cols.Length], 0.3f);
            }
        }
        Reg(it, x, z, 0.8f, w + 0.1f);
    }

}
