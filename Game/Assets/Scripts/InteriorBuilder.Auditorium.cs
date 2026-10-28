using UnityEngine;

// 礼堂 80×60、净高 8.5m：门厅（售票/寄存/卫生间）+ 观众厅（12 排四区座椅，
// 中央 4m 通道 + 两侧通道）+ 舞台（台口/红幕/乐池台阶/三角钢琴/讲台）
// + 后台走廊 + 四间化妆间。
public static partial class InteriorBuilder
{
    static void AudSeatRun(Transform p, Interior it, float x0, float x1, float z, int n,
                           Material seat, Material back, Material baseMat)
    {
        float span = x1 - x0;
        Part(p, new Vector3((x0 + x1) * 0.5f, 0.29f, z), new Vector3(span, 0.14f, 0.72f), baseMat, 1f);
        for (int i = 0; i < n; i++)
        {
            float x = x0 + span * (i + 0.5f) / n;
            Part(p, new Vector3(x, 0.47f, z), new Vector3(span / n - 0.07f, 0.10f, 0.50f), seat, 0.8f);
            Part(p, new Vector3(x, 0.73f, z + 0.22f), new Vector3(span / n - 0.07f, 0.48f, 0.07f), back, 0.8f);
        }
        Reg(it, (x0 + x1) * 0.5f, z, span, 0.74f);
    }

    static void GrandPiano(Transform p, Interior it, float x, float y, float z, float ry)
    {
        var g = new GameObject("grandpiano").transform;
        g.SetParent(p, false);
        g.localPosition = new Vector3(x, y, z);
        g.localEulerAngles = new Vector3(0f, ry, 0f);
        var black = C("#1f2126");
        Part(g, new Vector3(0f, 0.24f, 0f), new Vector3(2.6f, 0.48f, 1.6f), C("#24262c"), 1f);
        Part(g, new Vector3(0f, 0.53f, -0.05f), new Vector3(2.5f, 0.10f, 1.5f), black, 1f);
        Part(g, new Vector3(0f, 0.60f, 0.30f), new Vector3(2.2f, 0.05f, 0.55f), C("#f2f0e6"), 1f);
        Part(g, new Vector3(0f, 0.58f, 0.02f), new Vector3(2.0f, 0.05f, 0.22f), C("#dfe6dc"), 1f);
        Part(g, new Vector3(0f, 0.72f, -0.55f), new Vector3(2.2f, 0.30f, 0.55f), black, 1f);
        for (int i = 0; i < 3; i++)
            Part(g, new Vector3(i == 0 ? -1.0f : i == 1 ? 1.0f : 0f, 0.10f,
                 i == 2 ? -0.6f : 0.55f), new Vector3(0.12f, 0.22f, 0.12f), black, 0.6f);
        Reg(it, x, z, 2.7f, 1.7f);
    }

    static void DressingRoom(Transform p, Interior it, float x0, float x1, float z0, float z1,
                             float doorX, string label, Material wall, int lightCount = 2)
    {
        float cx = (x0 + x1) * 0.5f;
        float cz = (z0 + z1) * 0.5f;
        var wood = C("#8a6a4a");
        var cloth = C("#9a3a36");

        // 化妆台贴南墙，三面镜子 + 灯泡
        float counterX = cx;
        Part(p, new Vector3(counterX, 0.80f, z1 - 0.32f), new Vector3(4.2f, 0.10f, 0.58f), C("#efe6d2"), 1f);
        Part(p, new Vector3(counterX, 0.40f, z1 - 0.32f), new Vector3(4.0f, 0.74f, 0.48f), wood, 1f);
        Reg(it, counterX, z1 - 0.32f, 4.2f, 0.58f);
        for (int i = 0; i < 3; i++)
        {
            float mx = counterX - 1.45f + i * 1.45f;
            Part(p, new Vector3(mx, 1.55f, z1 - 0.10f), new Vector3(0.88f, 0.92f, 0.04f), Glass(), 1f);
            Part(p, new Vector3(mx, 1.55f, z1 - 0.07f), new Vector3(0.98f, 1.02f, 0.02f), C("#6f5540"), 1f);
            for (int b = -1; b <= 1; b += 2)
                Part(p, new Vector3(mx + b * 0.52f, 1.55f, z1 - 0.04f), new Vector3(0.12f, 0.88f, 0.05f), C("#fff2d6"), 1f);
        }
        for (int i = 0; i < 3; i++)
            Chair(p, it, new Vector3(counterX - 1.1f + i * 1.1f, 0f, z1 - 1.05f), 0f, wood, true);

        // 服装架：一根横杆 + 一排彩色衣片
        Part(p, new Vector3(x1 - 0.60f, 1.80f, cz + 0.4f), new Vector3(0.06f, 0.06f, 3.4f), C("#8d99a6"), 1f);
        for (int i = 0; i < 2; i++)
            Part(p, new Vector3(x1 - 0.60f, 0.9f, cz + 0.4f + (i == 0 ? -1.6f : 1.6f)),
                 new Vector3(0.06f, 1.8f, 0.06f), C("#8d99a6"), 1f);
        var cloths = new[] { "#7a3a3a", "#3f5c6b", "#8a7440", "#4a6a4a", "#6b4a6b", "#a05a3c", "#8a8a7a" };
        for (int i = 0; i < 7; i++)
            Part(p, new Vector3(x1 - 0.60f, 1.18f, cz - 1.0f + i * 0.42f),
                 new Vector3(0.10f, 1.18f, 0.30f), C(cloths[i]), 1f);

        // 长凳 + 小桌 + 绿植
        Part(p, new Vector3(cx + 0.6f, 0.26f, cz + 1.0f), new Vector3(2.2f, 0.10f, 0.55f), wood, 1f);
        for (int i = 0; i < 2; i++)
            Part(p, new Vector3(cx + 0.6f + (i == 0 ? -0.9f : 0.9f), 0.13f, cz + 1.0f),
                 new Vector3(0.10f, 0.26f, 0.48f), C("#6f5540"), 1f);
        Reg(it, cx + 0.6f, cz + 1.0f, 2.2f, 0.55f);
        Table(p, new Vector3(x0 + 1.5f, 0f, cz - 1.2f), 1.4f, 0.8f, 0.74f, wood);
        Reg(it, x0 + 1.5f, cz - 1.2f, 1.4f, 0.8f);
        TableLamp(p, new Vector3(x0 + 1.5f, 0.77f, cz - 1.2f), "#e8d6ae");
        Rug(p, new Vector3(cx, 0.06f, cz), 6.0f, 4.4f, C("#7a3a3a"));
        Plant(p, new Vector3(x0 + 0.9f, 0.02f, z0 + 1.2f), 0.9f);

        for (int i = 0; i < lightCount; i++)
            Lamp(p, new Vector3(cx - 2.5f + i * 5.0f, 3.05f, cz), 11f, 1.05f);
    }

    static void AudRestroom(Transform p, Interior it, float x0, float x1, float z0, float z1,
                            bool westSide, float doorX, float zDoor, string label)
    {
        float cx = (x0 + x1) * 0.5f;
        float outerX = westSide ? x0 : x1;
        float innerDir = westSide ? 1f : -1f;
        var tile = T("in_tile");
        Part(p, new Vector3(cx, 0.036f, (z0 + z1) * 0.5f),
             new Vector3(x1 - x0 - 0.2f, 0.012f, z1 - z0 - 0.2f), tile, 1.6f);

        // 4 个独立厕所隔间：外墙 + 5 道侧板 + 前板留 1.1m 门洞
        const float stallW = 1.45f, stallDepth = 1.55f;
        float frontX = outerX + innerDir * stallDepth;
        for (int i = 0; i < 4; i++)
        {
            float zc = z0 + 0.55f + stallW * (i + 0.5f);
            float side = i % 2 == 0 ? 1f : -1f;
            float panelZ = zc + side * (stallW * 0.5f - 0.175f);
            Part(p, new Vector3(frontX, 1.0f, panelZ), new Vector3(0.05f, 2.0f, 0.35f), C("#8a929a"), 1f);
            Part(p, new Vector3(frontX, 2.02f, zc), new Vector3(0.07f, 0.06f, stallW), C("#7a828a"), 1f);
            Part(p, new Vector3(frontX + innerDir * 0.03f, 1.10f, zc + side * 0.55f),
                 new Vector3(0.08f, 0.24f, 0.12f), C(i % 2 == 0 ? "#5a9a6a" : "#9a5a5a"), 1f);
            Reg(it, frontX, panelZ, 0.06f, 0.36f);
            // 半开的隔间门（贴侧板收进隔间）
            Part(p, new Vector3(frontX - innerDir * 0.325f, 0.96f, zc + side * 0.375f),
                 new Vector3(0.65f, 1.90f, 0.04f), C("#9aa4ae"), 1f);
            Part(p, new Vector3(frontX - innerDir * 0.55f, 1.02f, zc + side * 0.395f),
                 new Vector3(0.05f, 0.06f, 0.10f), C("#c8a05a"), 1f);
            // 马桶：水箱贴外墙，座体朝室内
            Part(p, new Vector3(outerX + innerDir * 0.22f, 0.62f, zc),
                 new Vector3(0.20f, 0.45f, 0.55f), C("#eef2f2"), 1f);
            Part(p, new Vector3(outerX + innerDir * 0.62f, 0.22f, zc),
                 new Vector3(0.55f, 0.42f, 0.50f), C("#eef2f2"), 1f);
            Part(p, new Vector3(outerX + innerDir * 0.72f, 0.44f, zc),
                 new Vector3(0.48f, 0.05f, 0.44f), C("#dfe6e6"), 1f);
            Reg(it, outerX + innerDir * 0.55f, zc, 0.90f, 0.56f);
        }
        // 5 道侧隔板（含两端）
        for (int i = 0; i <= 4; i++)
        {
            float zb = z0 + 0.55f + i * stallW;
            Part(p, new Vector3(outerX + innerDir * (stallDepth * 0.5f), 1.0f, zb),
                 new Vector3(stallDepth - 0.06f, 2.0f, 0.05f), C("#8a929a"), 1f);
            Reg(it, outerX + innerDir * (stallDepth * 0.5f), zb, stallDepth - 0.04f, 0.06f);
        }

        // 洗手台贴南墙 + 镜子
        float counterX = westSide ? x1 - 2.6f : x0 + 2.6f;
        Part(p, new Vector3(counterX, 0.82f, z1 - 0.35f), new Vector3(4.6f, 0.10f, 0.60f), C("#dfe6dc"), 1f);
        Part(p, new Vector3(counterX, 0.41f, z1 - 0.35f), new Vector3(4.4f, 0.76f, 0.50f), C("#c8d0d4"), 1f);
        Reg(it, counterX, z1 - 0.35f, 4.6f, 0.60f);
        for (int i = 0; i < 4; i++)
        {
            float sx = counterX - 1.65f + i * 1.1f;
            Part(p, new Vector3(sx, 0.90f, z1 - 0.35f), new Vector3(0.58f, 0.08f, 0.40f), C("#eef2f2"), 1f);
            Part(p, new Vector3(sx, 1.58f, z1 - 0.10f), new Vector3(0.62f, 0.92f, 0.03f), Glass(), 1f);
        }

        WallSign(p, doorX, 3.0f, zDoor, 1, label, 1.1f, 0.36f, C("#2f4a3a"), 0.026f);
        Lamp(p, new Vector3(cx, 3.05f, (z0 + z1) * 0.5f), 12f, 1.1f);
        Lamp(p, new Vector3(counterX, 3.05f, z1 - 1.6f), 9f, 0.9f);
    }

    // 双面吊牌：门厅侧（+Z）和观众厅侧（-Z）都能读，避免只能看背面镜像
    static void TwoWayHangingSign(Transform p, float x, float y, float z, string label,
                                  float w, float h, Material plate, float textH)
    {
        Part(p, new Vector3(x, y, z), new Vector3(w, h, 0.06f), plate, 1f);
        foreach (float f in new[] { -1f, 1f })
        {
            Part(p, new Vector3(x, y, z + f * 0.039f), new Vector3(w - 0.16f, h - 0.12f, 0.012f), C("#dfe6dc"), 1f);
            SignText(p, new Vector3(x, y, z + f * 0.061f), f > 0 ? 180f : 0f, label, textH, MatLib.Hex("#1f3428"));
        }
        foreach (float sx in new[] { -1f, 1f })
            Part(p, new Vector3(x + sx * w * 0.35f, y + h * 0.5f + 0.35f, z),
                 new Vector3(0.02f, 0.7f, 0.02f), C("#6a7078"), 1f);
    }

    static Interior Auditorium(Transform parent)
    {
        const float X = 40f, Z = 30f, W = 8.5f;
        var wall = T("in_wall2");
        var red = C("#8f2f32");
        var darkRed = C("#6a2428");
        var gold = C("#c8a05a");
        var wood = C("#8a6a4a");
        var darkWood = C("#5a3f2c");
        var seat = C("#8f3336");
        var seatBack = C("#6f2428");
        var baseMat = C("#3a2a2a");

        var it = Shell(parent, "auditorium", "礼堂", AUD_X, X, Z, W, 2.0f, 3.2f,
                       wall, T("in_floor"), T("in_ceil"), wall, C("#6a3a32"), 0.66f);
        var p = it.Root.transform;
        float ox = AUD_X;

        // 舞台可上人：台面 0.99m；台前三步台阶用隐形坡道衔接；台口两侧补挡墙
        it.StageY = 0.99f;
        it.StageX1 = ox - 12.9f; it.StageX2 = ox + 12.9f;
        it.StageZ1 = -25.9f; it.StageZ2 = -15.9f;
        it.RampX1 = ox - 3.0f; it.RampX2 = ox + 3.0f;
        it.RampZ1 = -15.9f; it.RampZ2 = -14.4f;

        // 门外侧门牌（Shell 的门牌在门内，外侧补一块，避免看到镜像）
        WallSign(p, ox, 3.95f, 30f, 1, "礼堂", 2.6f, 0.68f, C("#6a3a32"), 0.040f);

        // ── 地面：观众厅红毯 + 通道 / 门厅地毯 ──
        Part(p, new Vector3(ox, 0.050f, -1.0f), new Vector3(68f, 0.014f, 29.0f), C("#5a2a2c"), 2f);
        foreach (float ax in new[] { -36f, -14f, 0f, 14f, 36f })
            Part(p, new Vector3(ox + ax, 0.064f, -1.0f), new Vector3(2.2f, 0.012f, 29.0f), C("#8a6a5a"), 1f);
        Part(p, new Vector3(ox, 0.050f, 22f), new Vector3(66f, 0.014f, 15.0f), C("#6a3a3c"), 2f);
        Part(p, new Vector3(ox, 0.064f, 22f), new Vector3(5.0f, 0.012f, 15.0f), C("#a88a6a"), 1f);

        // ── 观众厅与门厅之间的隔墙 z=14，中间 12m 大开口 + 两侧 4m 出口 ──
        Wall(p, it, ox - 39.88f, 14f, ox - 28f, 14f, 0.02f, W, wall);
        Wall(p, it, ox - 24f, 14f, ox - 6f, 14f, 0.02f, W, wall);
        Wall(p, it, ox + 6f, 14f, ox + 24f, 14f, 0.02f, W, wall);
        Wall(p, it, ox + 28f, 14f, ox + 39.88f, 14f, 0.02f, W, wall);
        foreach (var (a, b) in new[] { (-28f, -24f), (-6f, 6f), (24f, 28f) })
        {
            Part(p, new Vector3(ox + (a + b) * 0.5f, 6.25f, 14f),
                 new Vector3(b - a, 4.5f, 0.22f), wall, 1.7f);
            Part(p, new Vector3(ox + (a + b) * 0.5f, 4.05f, 14.06f),
                 new Vector3(b - a + 0.3f, 0.14f, 0.34f), gold, 1f);
        }
        foreach (var (a, b) in new[] { (-28f, -24f), (24f, 28f) })
            TwoWayHangingSign(p, ox + (a + b) * 0.5f, 3.7f, 14f, "出口", 1.2f, 0.34f, C("#2f4a3a"), 0.026f);
        TwoWayHangingSign(p, ox, 3.75f, 14f, "观众厅", 2.0f, 0.44f, C("#6a3a32"), 0.034f);

        // ── 舞台：台面/台口/红幕/背景幕/台阶/讲台/钢琴 ──
        Part(p, new Vector3(ox, 0.48f, -21f), new Vector3(26f, 0.92f, 10f), wood, 2f);
        // 台口两侧挡墙：只留中央 6m 楼梯口，玩家从台阶坡道走上舞台
        Wall(p, it, ox - 12.9f, -16f, ox - 3f, -16f, 0.02f, 0.97f, darkWood);
        Wall(p, it, ox + 3f, -16f, ox + 12.9f, -16f, 0.02f, 0.97f, darkWood);
        Part(p, new Vector3(ox, 0.955f, -21f), new Vector3(25.8f, 0.06f, 9.8f), C("#c89a62"), 2f);
        for (int i = 0; i < 3; i++)
            Part(p, new Vector3(ox, 0.45f - i * 0.15f, -15.72f + i * 0.42f),
                 new Vector3(6.0f, 0.90f - i * 0.30f, 0.42f), wood, 1f);

        // 台口柱 + 横梁
        foreach (float sx in new[] { -1f, 1f })
        {
            Part(p, new Vector3(ox + sx * 13.4f, 3.6f, -15.3f), new Vector3(0.85f, 7.2f, 0.85f), darkRed, 1f);
            Part(p, new Vector3(ox + sx * 13.4f, 0.25f, -15.3f), new Vector3(1.2f, 0.5f, 1.2f), darkWood, 1f);
            Part(p, new Vector3(ox + sx * 13.4f, 7.0f, -15.3f), new Vector3(1.15f, 0.35f, 1.15f), gold, 1f);
            Reg(it, ox + sx * 13.4f, -15.3f, 0.9f, 0.9f);
        }
        Part(p, new Vector3(ox, 7.45f, -15.42f), new Vector3(28f, 1.0f, 0.9f), darkRed, 1f);
        Part(p, new Vector3(ox, 7.0f, -15.38f), new Vector3(27.2f, 0.12f, 0.98f), gold, 1f);

        // 背景幕（离后墙 0.5m，两侧留演员通道）+ 金环校徽
        Part(p, new Vector3(ox, 3.75f, -25.45f), new Vector3(19.0f, 5.7f, 0.06f), darkRed, 1f);
        Part(p, new Vector3(ox, 4.15f, -25.38f), new Vector3(3.4f, 3.4f, 0.04f), gold, 1f);
        Part(p, new Vector3(ox, 4.15f, -25.35f), new Vector3(2.7f, 2.7f, 0.03f), darkRed, 1f);
        Part(p, new Vector3(ox, 4.15f, -25.32f), new Vector3(1.5f, 1.5f, 0.02f), gold, 1f);

        // 两侧幕布（多褶）+ 顶部檐幕
        var curtainMat = C("#a83236");
        foreach (float sx in new[] { -1f, 1f })
            for (int i = 0; i < 5; i++)
            {
                float x = ox + sx * (8.4f + i * 1.0f);
                float depth = -15.85f - (i % 2) * 0.12f;
                Part(p, new Vector3(x, 3.6f, depth), new Vector3(0.55f, 5.4f, 0.12f), curtainMat, 1f);
                Part(p, new Vector3(x, 0.98f, depth), new Vector3(0.55f, 0.16f, 0.14f), gold, 1f);
            }
        Part(p, new Vector3(ox, 6.55f, -15.80f), new Vector3(25.6f, 0.95f, 0.14f), curtainMat, 1f);
        Part(p, new Vector3(ox, 6.08f, -15.78f), new Vector3(25.6f, 0.10f, 0.16f), gold, 1f);

        // 舞台侧幕
        foreach (float sx in new[] { -1f, 1f })
            for (int i = 0; i < 3; i++)
                Part(p, new Vector3(ox + sx * 12.6f, 3.4f, -25.0f + i * 3.0f),
                     new Vector3(0.18f, 5.0f, 1.2f), C("#4a1f22"), 1f);

        // 讲台 / 三角钢琴 / 舞台灯
        Part(p, new Vector3(ox - 8.5f, 1.52f, -18.6f), new Vector3(0.9f, 1.15f, 0.65f), darkWood, 1f);
        Part(p, new Vector3(ox - 8.5f, 1.52f, -18.24f), new Vector3(0.66f, 0.50f, 0.04f), C("#efe6d2"), 1f);
        Part(p, new Vector3(ox - 8.5f, 1.02f, -18.6f), new Vector3(0.98f, 0.10f, 0.72f), wood, 1f);
        Reg(it, ox - 8.5f, -18.6f, 0.95f, 0.70f);
        GrandPiano(p, it, ox + 8.3f, 0.96f, -21.4f, -25f);
        foreach (float lx in new[] { -10f, -6f, -2f, 2f, 6f, 10f })
        {
            Lamp(p, new Vector3(ox + lx, 7.15f, -17.0f), 15f, 1.5f, "#ffe1b0");
            Part(p, new Vector3(ox + lx, 7.45f, -17.0f), new Vector3(0.30f, 0.26f, 0.30f), C("#2f3338"), 1f);
        }
        Part(p, new Vector3(ox, 7.62f, -17.0f), new Vector3(24f, 0.12f, 0.12f), C("#3a3f47"), 1f);

        // 舞台后墙 z=-26：两个 2m 演员出入口
        Wall(p, it, ox - 13f, -26f, ox - 8f, -26f, 0.02f, W, wall);
        Wall(p, it, ox - 6f, -26f, ox + 6f, -26f, 0.02f, W, wall);
        Wall(p, it, ox + 8f, -26f, ox + 13f, -26f, 0.02f, W, wall);
        foreach (float sx in new[] { -1f, 1f })
            Part(p, new Vector3(ox + sx * 7f, 6.3f, -26f), new Vector3(2.0f, 4.4f, 0.24f), wall, 1.7f);
        // 舞台侧墙
        Wall(p, it, ox - 13f, -26f, ox - 13f, -16f, 0.02f, W, wall);
        Wall(p, it, ox + 13f, -26f, ox + 13f, -16f, 0.02f, W, wall);
        // 舞台与门厅之间的两侧墙（台口外）
        Wall(p, it, ox - 39.88f, -16f, ox - 13f, -16f, 0.02f, W, wall);
        Wall(p, it, ox + 13f, -16f, ox + 39.88f, -16f, 0.02f, W, wall);

        // ── 观众席：12 排 × 四区（中央 4m 通道） ──
        for (int r = 0; r < 12; r++)
        {
            float z = -12.6f + r * 1.7f;
            AudSeatRun(p, it, ox - 32f, ox - 16f, z, 16, seat, seatBack, baseMat);
            AudSeatRun(p, it, ox - 12f, ox - 2f, z, 10, seat, seatBack, baseMat);
            AudSeatRun(p, it, ox + 2f, ox + 12f, z, 10, seat, seatBack, baseMat);
            AudSeatRun(p, it, ox + 16f, ox + 32f, z, 16, seat, seatBack, baseMat);
        }

        // 侧墙木饰面 + 壁灯
        foreach (float sx in new[] { -1f, 1f })
        {
            for (int i = 0; i < 4; i++)
            {
                float z = -12f + i * 7.0f;
                Part(p, new Vector3(ox + sx * 39.78f, 2.2f, z), new Vector3(0.12f, 4.2f, 5.6f), darkWood, 1f);
                Part(p, new Vector3(ox + sx * 39.62f, 3.1f, z), new Vector3(0.14f, 0.24f, 0.60f), C("#ffe2b0"), 1f);
                Lamp(p, new Vector3(ox + sx * 39.3f, 3.25f, z), 10f, 0.8f, "#ffdcae");
            }
        }

        // 调音/灯光台（后区东侧）
        Table(p, new Vector3(ox + 25f, 0f, 9.6f), 3.4f, 1.0f, 0.86f, C("#2f3338"));
        Reg(it, ox + 25f, 9.6f, 3.4f, 1.0f);
        for (int i = 0; i < 3; i++)
            Part(p, new Vector3(ox + 24.0f + i * 1.0f, 1.20f, 9.7f), new Vector3(0.78f, 0.46f, 0.05f), C("#1f2126"), 1f);
        Chair(p, it, new Vector3(ox + 25f, 0f, 10.6f), 180f, C("#2f3338"), true);

        // ── 后台走廊 + 四间化妆间 ──
        Wall(p, it, ox - 39.88f, -26f, ox - 39.0f, -26f, 0.02f, W, wall);
        // 西侧化妆间北墙（带门）
        WallX(p, it, ox - 39.88f, ox - 23.5f, -26f, 0.02f, W, ox - 31.7f, ox - 30.3f, 2.6f, wall, WALL_IN);
        WallX(p, it, ox - 23.5f, ox - 13f, -26f, 0.02f, W, ox - 18.7f, ox - 17.3f, 2.6f, wall, WALL_IN);
        WallX(p, it, ox + 13f, ox + 23.5f, -26f, 0.02f, W, ox + 17.3f, ox + 18.7f, 2.6f, wall, WALL_IN);
        WallX(p, it, ox + 23.5f, ox + 39.88f, -26f, 0.02f, W, ox + 30.3f, ox + 31.7f, 2.6f, wall, WALL_IN);
        // 化妆间分隔墙
        WallZ(p, it, -26f, -16f, ox - 23.5f, 0.02f, W, 0f, 0f, 2.6f, wall, WALL_IN);
        WallZ(p, it, -26f, -16f, ox + 23.5f, 0.02f, W, 0f, 0f, 2.6f, wall, WALL_IN);
        DressingRoom(p, it, ox - 39.88f, ox - 23.59f, -25.91f, -16.09f, ox - 31.0f, "化妆间 A", wall);
        DressingRoom(p, it, ox - 23.41f, ox - 13.09f, -25.91f, -16.09f, ox - 18.0f, "化妆间 B", wall);
        DressingRoom(p, it, ox + 13.09f, ox + 23.41f, -25.91f, -16.09f, ox + 18.0f, "化妆间 C", wall);
        DressingRoom(p, it, ox + 23.59f, ox + 39.88f, -25.91f, -16.09f, ox + 31.0f, "化妆间 D", wall);
        foreach (var (dx, label, face) in new[] {
            (ox - 31f, "化妆间 A", -1), (ox - 18f, "化妆间 B", -1),
            (ox + 18f, "化妆间 C", -1), (ox + 31f, "化妆间 D", -1) })
            WallSign(p, dx + 1.0f, 2.55f, -26f, face, label, 1.1f, 0.36f, C("#6a3a32"), 0.026f);
        // 后台走廊：道具桌 + 衣箱 + 灭火器
        for (int i = 0; i < 3; i++)
        {
            float bx = ox - 20f + i * 20f;
            Part(p, new Vector3(bx, 0.42f, -28.2f), new Vector3(2.6f, 0.84f, 0.9f), C("#6f5540"), 1f);
            Reg(it, bx, -28.2f, 2.6f, 0.9f);
            Part(p, new Vector3(bx, 0.30f, -29.2f), new Vector3(1.0f, 0.60f, 0.7f), C("#5a4a3a"), 1f);
            Reg(it, bx, -29.2f, 1.0f, 0.7f);
        }
        Lamp(p, new Vector3(ox - 20f, 3.25f, -28f), 14f, 1.1f);
        Lamp(p, new Vector3(ox + 20f, 3.25f, -28f), 14f, 1.1f);

        // 门厅：售票/寄存/沙发/海报/吊灯/卫生间
        // 售票台：长边沿 Z，牌面朝中央通道（+X），离侧墙/南墙更远
        Table(p, new Vector3(ox - 18f, 0f, 23.5f), 1.0f, 4.0f, 0.95f, darkWood);
        Reg(it, ox - 18f, 23.5f, 1.0f, 4.0f);
        Part(p, new Vector3(ox - 18f, 1.00f, 23.5f), new Vector3(1.06f, 0.08f, 4.1f), C("#dfe6dc"), 1f);
        Part(p, new Vector3(ox - 17.47f, 1.12f, 23.5f), new Vector3(0.04f, 0.38f, 2.6f), C("#4a2a28"), 1f);
        SignText(p, new Vector3(ox - 17.44f, 1.12f, 23.5f), 270f, "售 票 处", 0.032f, MatLib.Hex("#f3e9d2"));
        Chair(p, it, new Vector3(ox - 19.3f, 0f, 23.5f), 90f, darkWood, true);

        // 衣帽寄存：长边沿 Z，牌面朝中央通道（-X）；衣架在桌后
        Table(p, new Vector3(ox + 18f, 0f, 23.5f), 1.0f, 4.0f, 0.95f, darkWood);
        Reg(it, ox + 18f, 23.5f, 1.0f, 4.0f);
        Part(p, new Vector3(ox + 18f, 1.00f, 23.5f), new Vector3(1.06f, 0.08f, 4.1f), C("#dfe6dc"), 1f);
        Part(p, new Vector3(ox + 17.47f, 1.12f, 23.5f), new Vector3(0.04f, 0.38f, 2.6f), C("#4a2a28"), 1f);
        SignText(p, new Vector3(ox + 17.44f, 1.12f, 23.5f), 90f, "衣帽寄存", 0.032f, MatLib.Hex("#f3e9d2"));
        Chair(p, it, new Vector3(ox + 19.3f, 0f, 23.5f), 270f, darkWood, true);
        Part(p, new Vector3(ox + 20.0f, 1.80f, 23.5f), new Vector3(0.06f, 0.06f, 3.4f), C("#8d99a6"), 1f);
        foreach (float sz in new[] { -1.6f, 1.6f })
            Part(p, new Vector3(ox + 20.0f, 0.90f, 23.5f + sz), new Vector3(0.06f, 1.8f, 0.06f), C("#8d99a6"), 1f);
        for (int i = 0; i < 8; i++)
            Part(p, new Vector3(ox + 20.0f, 1.12f, 22.4f + i * 0.32f),
                 new Vector3(0.30f, 0.85f, 0.16f), C(i % 2 == 0 ? "#5a4a3a" : "#3f5c6b"), 1f);
        Reg(it, ox + 20.0f, 23.5f, 0.36f, 3.4f);

        Sofa(p, new Vector3(ox - 13f, 0f, 21.5f), 2.4f, 90f, C("#7a5a5a"));
        Reg(it, ox - 13f, 21.5f, 1.0f, 2.4f);
        Table(p, new Vector3(ox - 10.5f, 0f, 21.5f), 1.3f, 0.75f, 0.40f, wood);
        Reg(it, ox - 10.5f, 21.5f, 1.3f, 0.75f);
        Sofa(p, new Vector3(ox + 13f, 0f, 21.5f), 2.4f, 270f, C("#5a6a7a"));
        Reg(it, ox + 13f, 21.5f, 1.0f, 2.4f);
        Table(p, new Vector3(ox + 10.5f, 0f, 21.5f), 1.3f, 0.75f, 0.40f, wood);
        Reg(it, ox + 10.5f, 21.5f, 1.3f, 0.75f);
        Rug(p, new Vector3(ox - 11.5f, 0.070f, 21.5f), 5.5f, 4.6f, C("#8a5a4a"));
        Rug(p, new Vector3(ox + 11.5f, 0.070f, 21.5f), 5.5f, 4.6f, C("#5a6a7a"));

        // 海报（隔墙大厅侧）
        foreach (float px in new[] { -31f, -15f, 15f, 31f })
        {
            Picture(p, new Vector3(ox + px, 2.3f, 14.17f), 1.6f, 2.2f, 180f);
            Part(p, new Vector3(ox + px, 2.3f, 14.14f), new Vector3(1.7f, 2.3f, 0.02f), C("#2f3338"), 1f);
        }
        WallClock(p, new Vector3(ox + 6.5f, 3.0f, 29.80f), 180f);
        Part(p, new Vector3(ox - 4.6f, 2.15f, 29.80f), new Vector3(2.6f, 1.4f, 0.05f), C("#6f5540"), 1f);
        SignText(p, new Vector3(ox - 4.6f, 2.15f, 29.76f), 0f, "观 众 须 知", 0.032f, MatLib.Hex("#f3e9d2"));

        // 门厅吊灯 / 卫生间
        for (int i = 0; i < 3; i++)
        {
            Pendant(p, ox - 14f + i * 14f, 6.2f, 20.0f, 1.4f, "#e0c8a0");
            Pendant(p, ox - 14f + i * 14f, 6.2f, 26.0f, 1.4f, "#e0c8a0");
        }
        // 西 / 东卫生间
        WallZ(p, it, 18f, 29.88f, ox - 28f, 0.02f, W, 0f, 0f, 2.6f, wall, WALL_IN);
        WallX(p, it, ox - 39.88f, ox - 28f, 18f, 0.02f, W, ox - 34.7f, ox - 33.3f, 2.6f, wall, WALL_IN);
        WallZ(p, it, 18f, 29.88f, ox + 28f, 0.02f, W, 0f, 0f, 2.6f, wall, WALL_IN);
        WallX(p, it, ox + 28f, ox + 39.88f, 18f, 0.02f, W, ox + 33.3f, ox + 34.7f, 2.6f, wall, WALL_IN);
        AudRestroom(p, it, ox - 39.88f, ox - 28.09f, 18.09f, 29.88f, true, ox - 34f, 18f, "男卫生间");
        AudRestroom(p, it, ox + 28.09f, ox + 39.88f, 18.09f, 29.88f, false, ox + 34f, 18f, "女卫生间");

        // 入口窗 + 绿植
        foreach (float sx in new[] { -1f, 1f })
        {
            Window(p, new Vector3(ox + sx * 16f, 3.9f, 29.70f), 4.2f, 2.0f, 0f);
            Window(p, new Vector3(ox + sx * 9f, 3.9f, 29.70f), 3.0f, 2.0f, 0f);
        }
        Plant(p, new Vector3(ox - 26f, 0.02f, 29.0f), 1.3f);
        Plant(p, new Vector3(ox + 26f, 0.02f, 29.0f), 1.3f);
        Plant(p, new Vector3(ox - 6.5f, 0.02f, 15.2f), 1.0f);
        Plant(p, new Vector3(ox + 6.5f, 0.02f, 15.2f), 1.0f);

        // 观众厅顶灯
        for (int i = 0; i < 3; i++)
            foreach (float lz in new[] { -12f, -3f, 6f })
                Lamp(p, new Vector3(ox - 20f + i * 20f, W - 0.55f, lz), 24f, 1.35f);
        foreach (float lx in new[] { -12f, 0f, 12f })
            Lamp(p, new Vector3(ox + lx, 6.4f, 16.5f), 12f, 1.15f);
        WallClock(p, new Vector3(ox - 6.5f, 3.1f, 14.18f), 0f);

        // 顶部桁架
        foreach (float bz in new[] { -18f, -8f, 2f, 12f })
            Part(p, new Vector3(ox, W - 0.45f, bz), new Vector3(78f, 0.28f, 0.28f), C("#3a3f47"), 1f);
        return it;
    }
}
