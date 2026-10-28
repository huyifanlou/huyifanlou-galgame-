using UnityEngine;

// 体育馆 / 实验楼
public static partial class InteriorBuilder
{
    // ── 体育馆：球场 + 南北看台 + 门厅 + 更衣/器材 ──
    static void BleachersX(Transform p, Interior it, float x0, float x1, float zFront, float dir)
    {
        var step = C("#b9c0c8");
        var seatA = C("#3f5c8a");
        var seatB = C("#d8a43c");
        for (int i = 0; i < 4; i++)
        {
            float z = zFront + dir * (0.6f + i * 1.0f);
            float h = 0.40f + i * 0.36f;
            Part(p, new Vector3((x0 + x1) * 0.5f, h * 0.5f, z),
                 new Vector3(x1 - x0, h, 1.0f), step, 1f);
            Part(p, new Vector3((x0 + x1) * 0.5f, h + 0.03f, z),
                 new Vector3(x1 - x0 - 0.15f, 0.06f, 0.80f), i % 2 == 0 ? seatA : seatB, 1f);
        }
        Reg(it, (x0 + x1) * 0.5f, zFront + dir * 2.0f, x1 - x0, 4.0f);
    }

    static void CourtRing(Transform p, float ox, float cz)
    {
        var line = C("#f2f0e6");
        for (int i = 0; i < 16; i++)
        {
            float a = i / 16f * Mathf.PI * 2f;
            Part(p, new Vector3(ox + Mathf.Cos(a) * 1.85f, 0.085f, cz + Mathf.Sin(a) * 1.85f),
                 new Vector3(0.72f, 0.02f, 0.06f), line, 1f,
                 new Vector3(0f, -a * Mathf.Rad2Deg + 90f, 0f));
        }
    }

    static Interior Gym(Transform parent)
    {
        const float X = 30f, Z = 18f, W = 7f;
        var it = Shell(parent, "gym", "体育馆", GYM_X, X, Z, W, 2.0f, 3.0f,
                       T("in_wall"), T("in_floor"), T("in_ceil"), null, C("#3a4a62"), 0.62f);
        var p = it.Root.transform;
        var wood = C("#c89a62");
        var line = C("#f2f0e6");
        var dark = C("#2f3338");
        var metal = C("#8d99a6");

        // 木地板球场 28×15
        Part(p, new Vector3(GYM_X, 0.055f, 0f), new Vector3(28f, 0.02f, 15f), T("wood"), 2f);
        for (int i = 0; i < 2; i++)
        {
            float sx = i == 0 ? -1f : 1f;
            Part(p, new Vector3(GYM_X + sx * 14f, 0.075f, 0f), new Vector3(0.06f, 0.02f, 15f), line, 1f);
            Part(p, new Vector3(GYM_X, 0.075f, sx * 7.5f), new Vector3(28f, 0.02f, 0.06f), line, 1f);
            Part(p, new Vector3(GYM_X + sx * 11.5f, 0.075f, 0f), new Vector3(0.06f, 0.02f, 5.8f), line, 1f);
            Part(p, new Vector3(GYM_X + sx * 9.0f, 0.075f, 0f), new Vector3(5f, 0.02f, 0.06f), line, 1f);
        }
        Part(p, new Vector3(GYM_X, 0.075f, 0f), new Vector3(0.06f, 0.02f, 15f), line, 1f);
        CourtRing(p, GYM_X, 0f);

        // 篮架
        foreach (float sx in new[] { -1f, 1f })
        {
            float px = GYM_X + sx * 15.2f;
            float bx = px - sx * 0.55f;
            float rx = px - sx * 1.15f;
            float fx = bx - sx * 0.06f;
            Part(p, new Vector3(px, 1.7f, 0f), new Vector3(0.22f, 3.4f, 0.22f), dark, 1f);
            Part(p, new Vector3((px + bx) * 0.5f, 2.75f, 0f), new Vector3(0.60f, 0.08f, 0.08f), dark, 1f);
            Part(p, new Vector3(bx, 2.75f, 0f), new Vector3(0.10f, 1.15f, 1.75f), C("#dfe6dc"), 1f);
            // 篮板橙色瞄准框
            Part(p, new Vector3(fx, 3.02f, 0f), new Vector3(0.02f, 0.04f, 0.55f), C("#c86a3c"), 1f);
            Part(p, new Vector3(fx, 2.48f, 0f), new Vector3(0.02f, 0.04f, 0.55f), C("#c86a3c"), 1f);
            Part(p, new Vector3(fx, 2.75f, 0.275f), new Vector3(0.02f, 0.58f, 0.04f), C("#c86a3c"), 1f);
            Part(p, new Vector3(fx, 2.75f, -0.275f), new Vector3(0.02f, 0.58f, 0.04f), C("#c86a3c"), 1f);
            // 篮筐 + 网
            Part(p, new Vector3(rx, 2.30f, 0f), new Vector3(0.08f, 0.08f, 0.65f), C("#d8782e"), 1f);
            Part(p, new Vector3((rx + bx) * 0.5f, 2.32f, 0f), new Vector3(0.50f, 0.05f, 0.08f), C("#d8782e"), 1f);
            foreach (float nz in new[] { -0.30f, 0.30f })
                Part(p, new Vector3(rx, 2.10f, nz), new Vector3(0.02f, 0.36f, 0.02f), C("#e8eef0"), 1f);
            foreach (float nx in new[] { -0.04f, 0.04f })
                Part(p, new Vector3(rx + nx, 2.10f, 0f), new Vector3(0.02f, 0.36f, 0.02f), C("#e8eef0"), 1f);
            Reg(it, px, 0f, 0.6f, 0.3f);
        }

        // 南北看台，中间留 5m 通道
        BleachersX(p, it, GYM_X - 12.5f, GYM_X - 2.6f, 10f, 1f);
        BleachersX(p, it, GYM_X + 2.6f, GYM_X + 12.5f, 10f, 1f);
        BleachersX(p, it, GYM_X - 12.5f, GYM_X - 2.6f, -10f, -1f);
        BleachersX(p, it, GYM_X + 2.6f, GYM_X + 12.5f, -10f, -1f);

        // 门厅：服务台 + 长椅 + 绿植 + 公告板
        Part(p, new Vector3(GYM_X - 10f, 0.48f, 15.6f), new Vector3(4.2f, 0.96f, 0.9f), C("#2f4a5a"), 1f);
        Part(p, new Vector3(GYM_X - 10f, 0.99f, 15.6f), new Vector3(4.3f, 0.07f, 0.96f), C("#dfe6dc"), 1f);
        Reg(it, GYM_X - 10f, 15.6f, 4.2f, 0.9f);
        Part(p, new Vector3(GYM_X - 10f, 1.28f, 15.05f), new Vector3(2.2f, 0.38f, 0.05f), C("#3a4a62"), 1f);
        SignText(p, new Vector3(GYM_X - 10f, 1.28f, 15.01f), 0f, "体育馆服务台", 0.026f, MatLib.Hex("#dfe6dc"));

        foreach (float bx in new[] { -7f, 7f })
        {
            Part(p, new Vector3(GYM_X + bx, 0.38f, 15.2f), new Vector3(3.2f, 0.12f, 0.55f), wood, 1f);
            Part(p, new Vector3(GYM_X + bx, 0.22f, 15.2f), new Vector3(0.12f, 0.44f, 0.48f), metal, 1f);
            Reg(it, GYM_X + bx, 15.2f, 3.2f, 0.55f);
        }
        Plant(p, new Vector3(GYM_X - 16f, 0.02f, 16.6f), 1.4f);
        Plant(p, new Vector3(GYM_X + 16f, 0.02f, 16.6f), 1.4f);

        // 西北更衣室 / 东北器材室
        WallZ(p, it, -Z, -12f, GYM_X - 20f, 0.02f, W, -15.6f, -14.4f, 2.3f, T("in_wall"), WALL_IN);
        WallX(p, it, GYM_X - X, GYM_X - 20f, -12f, 0.02f, W, 0f, 0f, 2.3f, T("in_wall"), WALL_IN);
        WallZ(p, it, -Z, -12f, GYM_X + 20f, 0.02f, W, -15.6f, -14.4f, 2.3f, T("in_wall"), WALL_IN);
        WallX(p, it, GYM_X + 20f, GYM_X + X, -12f, 0.02f, W, 0f, 0f, 2.3f, T("in_wall"), WALL_IN);

        // 更衣室：两排储物柜 + 长凳
        foreach (float lz in new[] { -16.2f, -13.4f })
        {
            Part(p, new Vector3(GYM_X - 29.3f, 1.0f, lz), new Vector3(0.55f, 2.0f, 2.0f), C("#4e5a6e"), 1f);
            for (int i = 0; i < 6; i++)
                Part(p, new Vector3(GYM_X - 28.98f, 0.55f + (i % 3) * 0.55f, lz - 0.66f + (i / 3) * 0.66f),
                     new Vector3(0.04f, 0.42f, 0.52f), C("#7b8798"), 1f);
            Reg(it, GYM_X - 29.3f, lz, 0.55f, 2.0f);
        }
        for (int i = 0; i < 3; i++)
        {
            float lx = GYM_X - 28.2f + i * 1.3f;
            Part(p, new Vector3(lx, 0.28f, -15.6f), new Vector3(1.0f, 0.14f, 0.55f), wood, 1f);
            Part(p, new Vector3(lx, 0.14f, -15.6f), new Vector3(0.10f, 0.28f, 0.48f), metal, 1f);
            Reg(it, lx, -15.6f, 1.0f, 0.55f);
        }
        SignText(p, new Vector3(GYM_X - 19.82f, 2.45f, -15f), 270f, "更衣室", 0.030f, MatLib.Hex("#dfe6dc"));

        // 器材室：货架 + 球筐
        for (int i = 0; i < 3; i++)
        {
            float sz = -16.5f + i * 1.6f;
            Part(p, new Vector3(GYM_X + 29.3f, 1.0f, sz), new Vector3(0.55f, 2.0f, 2.0f), C("#6f5540"), 1f);
            Reg(it, GYM_X + 29.3f, sz, 0.55f, 2.0f);
        }
        Part(p, new Vector3(GYM_X + 23.2f, 0.42f, -15.2f), new Vector3(1.2f, 0.8f, 1.2f), C("#8a6a4a"), 1f);
        Reg(it, GYM_X + 23.2f, -15.2f, 1.2f, 1.2f);
        SignText(p, new Vector3(GYM_X + 19.82f, 2.45f, -15f), 90f, "器材室", 0.030f, MatLib.Hex("#dfe6dc"));

        // 高窗 / 屋顶桁架 / 高灯 / 记分牌
        foreach (float sz in new[] { -6f, 0f, 6f })
        {
            Window(p, new Vector3(GYM_X - 29.80f, 4.9f, sz), 4.0f, 1.2f, 90f);
            Window(p, new Vector3(GYM_X + 29.80f, 4.9f, sz), 4.0f, 1.2f, -90f);
        }
        for (float tz = -12f; tz <= 12f; tz += 4f)
            Part(p, new Vector3(GYM_X, 6.55f, tz), new Vector3(58f, 0.12f, 0.12f), metal, 1f);
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 2; j++)
                Lamp(p, new Vector3(GYM_X - 18f + i * 12f, 6.15f, j == 0 ? -6f : 6f), 20f, 1.25f);

        Part(p, new Vector3(GYM_X, 4.85f, -17.78f), new Vector3(7.2f, 2.0f, 0.16f), dark, 1f);
        SignText(p, new Vector3(GYM_X, 4.85f, -17.68f), 180f, "主队  0 : 0  客队", 0.050f, MatLib.Hex("#ffcf6a"));
        Lamp(p, new Vector3(GYM_X - 10f, W - 0.35f, 15.5f), 9f, 1.0f);
        Lamp(p, new Vector3(GYM_X + 10f, W - 0.35f, 15.5f), 9f, 1.0f);
        return it;
    }

    // ── 实验楼：中央走廊 + 四间实验室 ──
    static void LabBench(Transform p, Interior it, float x, float z, Material top, Material body, Material accent)
    {
        Part(p, new Vector3(x, 0.86f, z), new Vector3(3.4f, 0.08f, 0.95f), top, 1f);
        Part(p, new Vector3(x, 0.42f, z), new Vector3(3.2f, 0.78f, 0.80f), body, 1f);
        Part(p, new Vector3(x + 1.25f, 0.93f, z + 0.20f), new Vector3(0.50f, 0.10f, 0.38f), accent, 1f);
        Part(p, new Vector3(x + 1.25f, 1.05f, z + 0.20f), new Vector3(0.06f, 0.18f, 0.06f), C("#8d99a6"), 1f);
        Reg(it, x, z, 3.4f, 0.95f);
        for (int i = 0; i < 2; i++)
            Stool(p, it, x - 1.0f + i * 2.0f, z - 0.85f, C("#5f6a78"));
    }

    static void LabRoom(Transform p, Interior it, float cx, float cz, bool west, Material wall,
                        bool fumeHood, string label, float doorZ)
    {
        var benchTop = C("#d8d2c6");
        var benchBody = C("#8a9aa6");
        var accent = C("#3f6a8a");
        var dark = C("#2f3338");

        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
                LabBench(p, it, cx - 4.8f + j * 9.6f, cz - 3.0f + i * 6.0f, benchTop, benchBody, accent);

        // 讲台 / 白板 / 储物柜 / 通风柜
        float innerX = west ? cx + 8.0f : cx - 8.0f;
        float outerX = west ? cx - 9.9f : cx + 9.9f;
        float boardX = west ? LABS_X - 1.95f : LABS_X + 1.95f;
        Table(p, new Vector3(innerX, 0f, cz - 4.8f), 2.4f, 0.9f, 0.82f, C("#8a6a4a"));
        Reg(it, innerX, cz - 4.8f, 2.4f, 0.9f);
        Part(p, new Vector3(boardX, 1.55f, cz - 4.8f), new Vector3(0.06f, 1.2f, 4.2f), C("#f2f0e6"), 1f);
        Part(p, new Vector3(boardX, 2.18f, cz - 4.8f), new Vector3(0.10f, 0.10f, 4.4f), C("#6f5540"), 1f);
        Part(p, new Vector3(outerX, 1.0f, cz + 4.6f), new Vector3(0.55f, 2.0f, 3.0f), C("#5f6a78"), 1f);
        Reg(it, outerX, cz + 4.6f, 0.55f, 3.0f);
        if (fumeHood)
        {
            Part(p, new Vector3(outerX, 1.25f, cz - 4.2f), new Vector3(0.95f, 2.5f, 2.0f), C("#c3c8cf"), 1f);
            Part(p, new Vector3(outerX + (west ? 0.50f : -0.50f), 1.35f, cz - 4.2f),
                 new Vector3(0.04f, 1.2f, 1.7f), Glass(), 1f);
            Reg(it, outerX, cz - 4.2f, 0.95f, 2.0f);
        }
        Window(p, new Vector3(outerX + (west ? 0.18f : -0.18f), 2.55f, cz + 1.0f),
               3.4f, 1.4f, west ? 90f : -90f);

        Lamp(p, new Vector3(cx, 3.35f, cz), 14f, 1.35f);

        // 门牌（走廊侧）
        float signX = west ? LABS_X - 1.68f : LABS_X + 1.68f;
        SignText(p, new Vector3(signX, 2.42f, doorZ), west ? 270f : 90f, label, 0.030f, MatLib.Hex("#e8f0f4"));
    }

    static Interior Labs(Transform parent)
    {
        const float X = 23f, Z = 15f, W = 3.8f;
        var wall = T("in_wall3");
        var it = Shell(parent, "labs", "实验楼", LABS_X, X, Z, W, 1.4f, 2.6f,
                       wall, T("in_floor"), T("in_ceil"), wall, C("#3d4654"), 0.58f);
        var p = it.Root.transform;
        var corridorMat = C("#b9c0c8");

        // 中央走廊：两侧各两间实验室
        foreach (float sx in new[] { -1f, 1f })
        {
            WallZ(p, it, -Z, 0f, LABS_X + sx * 1.8f, 0.02f, W, -7.7f, -6.3f, 2.2f, wall, WALL_IN);
            WallZ(p, it, 0f, Z, LABS_X + sx * 1.8f, 0.02f, W, 6.3f, 7.7f, 2.2f, wall, WALL_IN);
            WallX(p, it, LABS_X + sx * 1.8f, LABS_X + sx * X, 0f, 0.02f, W, 0f, 0f, 2.2f, wall, WALL_IN);
        }
        for (float x = -10f; x <= 10f; x += 5f)
            Part(p, new Vector3(LABS_X + x, 0.075f, 0f), new Vector3(0.12f, 0.02f, 2.8f), corridorMat, 1f);
        Rug(p, new Vector3(LABS_X, 0.06f, 10.5f), 2.4f, 4.5f, C("#6f8a9a"));
        Part(p, new Vector3(LABS_X - 1.72f, 1.6f, 3.0f), new Vector3(0.04f, 0.9f, 2.0f), C("#3d4654"), 1f);
        Part(p, new Vector3(LABS_X + 1.72f, 1.6f, -3.0f), new Vector3(0.04f, 0.9f, 2.0f), C("#3d4654"), 1f);
        foreach (var (sx, z, label) in new[] { (-1, -7f, "物理实验室"), (1, -7f, "化学实验室"), (-1, 7f, "生物实验室"), (1, 7f, "综合实验室") })
            SignText(p, new Vector3(LABS_X + sx * 1.68f, 2.42f, z), sx < 0 ? 270f : 90f, label, 0.030f, MatLib.Hex("#e8f0f4"));

        LabRoom(p, it, LABS_X - 12.4f, -7.5f, true, wall, false, "物理实验室", -7f);
        LabRoom(p, it, LABS_X + 12.4f, -7.5f, false, wall, true, "化学实验室", -7f);
        LabRoom(p, it, LABS_X - 12.4f, 7.5f, true, wall, false, "生物实验室", 7f);
        LabRoom(p, it, LABS_X + 12.4f, 7.5f, false, wall, false, "综合实验室", 7f);

        // 走廊尽头：储物柜/绿植/挂钟
        Part(p, new Vector3(LABS_X, 0.9f, -14.45f), new Vector3(2.4f, 1.8f, 0.5f), C("#5f6a78"), 1f);
        Reg(it, LABS_X, -14.45f, 2.4f, 0.5f);
        Plant(p, new Vector3(LABS_X - 20.5f, 0.02f, -13.8f), 1.1f);
        Plant(p, new Vector3(LABS_X + 20.5f, 0.02f, -13.8f), 1.1f);
        WallClock(p, new Vector3(LABS_X - 1.70f, 2.55f, 10.5f), 270f);
        LightGrid(p, LABS_X - X + 3f, LABS_X + X - 3f, -Z + 3f, Z - 3f, W - 0.35f, 4, 4, 14f, 1.15f);
        for (float lz = -12f; lz <= 12f; lz += 6f)
            Lamp(p, new Vector3(LABS_X, W - 0.35f, lz), 11f, 1.3f);
        return it;
    }
}
