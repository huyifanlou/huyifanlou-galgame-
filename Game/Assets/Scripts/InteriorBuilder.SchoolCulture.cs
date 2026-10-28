using UnityEngine;

// 活动中心 / 艺术楼 / 生活服务楼
public static partial class InteriorBuilder
{
    // ── 活动中心：门厅 + 多功能厅（舞台/座席）+ 社团室 ──
    static void SeatRow(Transform p, Interior it, float x0, float x1, float z, int n, Material seat)
    {
        float span = x1 - x0;
        for (int i = 0; i < n; i++)
        {
            float x = x0 + (i + 0.5f) * span / n;
            Part(p, new Vector3(x, 0.44f, z), new Vector3(0.44f, 0.08f, 0.44f), seat, 0.8f);
            Part(p, new Vector3(x, 0.68f, z + 0.20f), new Vector3(0.44f, 0.50f, 0.06f), seat, 0.8f);
            Part(p, new Vector3(x, 0.20f, z), new Vector3(0.06f, 0.40f, 0.06f), C("#3a3f47"), 0.6f);
        }
        Reg(it, (x0 + x1) * 0.5f, z, span, 0.55f);
    }

    static Interior Center(Transform parent)
    {
        const float X = 25f, Z = 16f, W = 6f;
        var it = Shell(parent, "center", "活动中心", CENTER_X, X, Z, W, 1.8f, 2.8f,
                       T("in_wall"), T("in_floor"), T("in_ceil"), T("in_wall2"), C("#6a3a32"), 0.64f);
        var p = it.Root.transform;
        var wood = C("#8a6a4a");
        var darkWood = C("#5a3f2c");
        var red = C("#9a3a36");
        var seat = C("#4e6a7a");
        var accent = C("#c8a05a");

        // 舞台（北端）：台面 + 台阶 + 背景幕 + 讲台
        Part(p, new Vector3(CENTER_X, 0.28f, -13.7f), new Vector3(14f, 0.56f, 4.0f), darkWood, 1.5f);
        Reg(it, CENTER_X, -13.7f, 14f, 4.0f);
        for (int i = 0; i < 3; i++)
            Part(p, new Vector3(CENTER_X + (i - 1) * 2.4f, 0.10f + i * 0.14f, -11.85f),
                 new Vector3(2.4f, 0.20f + i * 0.14f, 0.45f), wood, 1f);
        Part(p, new Vector3(CENTER_X, 3.0f, -15.55f), new Vector3(12.4f, 4.8f, 0.06f), red, 1f);
        Curtain(p, CENTER_X, 0.56f, -15.65f, 12.5f, 4.6f, red);
        Part(p, new Vector3(CENTER_X + 4.6f, 1.15f, -13.0f), new Vector3(0.9f, 1.2f, 0.7f), wood, 1f);
        Part(p, new Vector3(CENTER_X + 4.6f, 1.05f, -12.55f), new Vector3(0.7f, 0.55f, 0.06f), C("#f2f0e6"), 1f);
        Reg(it, CENTER_X + 4.6f, -13.0f, 0.9f, 0.7f);
        foreach (float sx in new[] { -1f, 1f })
        {
            Lamp(p, new Vector3(CENTER_X + sx * 5.0f, 5.1f, -13.2f), 10f, 1.3f);
            Part(p, new Vector3(CENTER_X + sx * 5.0f, 5.45f, -13.2f), new Vector3(0.36f, 0.20f, 0.36f), C("#2f3338"), 1f);
        }

        // 观众席 6 排 × 两侧各 5 座，中间 2.4m 通道
        for (int r = 0; r < 6; r++)
        {
            float z = -8.0f + r * 1.45f;
            SeatRow(p, it, CENTER_X - 11.5f, CENTER_X - 1.2f, z, 5, seat);
            SeatRow(p, it, CENTER_X + 1.2f, CENTER_X + 11.5f, z, 5, seat);
        }

        // 西/东社团室（音乐、阅读/棋艺）
        foreach (float sx in new[] { -1f, 1f })
        {
            float wx = CENTER_X + sx * 20.5f;
            WallZ(p, it, -4f, 8f, CENTER_X + sx * 16f, 0.02f, W, 1.0f, 2.4f, 2.3f, T("in_wall"), WALL_IN);
            WallX(p, it, CENTER_X + sx * 16f, CENTER_X + sx * X, -4f, 0.02f, W, 0f, 0f, 2.3f, T("in_wall"), WALL_IN);
            WallX(p, it, CENTER_X + sx * 16f, CENTER_X + sx * X, 8f, 0.02f, W, 0f, 0f, 2.3f, T("in_wall"), WALL_IN);
            if (sx < 0)
            {
                // 音乐社团：立式钢琴 + 鼓 + 沙发
                Part(p, new Vector3(wx - 1.0f, 0.62f, 3.0f), new Vector3(2.0f, 1.24f, 0.7f), C("#2f3338"), 1f);
                Part(p, new Vector3(wx - 1.0f, 0.80f, 3.42f), new Vector3(1.8f, 0.10f, 0.30f), C("#f2f0e6"), 1f);
                Part(p, new Vector3(wx - 1.0f, 0.40f, 3.42f), new Vector3(1.5f, 0.30f, 0.16f), C("#dfe6dc"), 1f);
                Reg(it, wx - 1.0f, 3.0f, 2.0f, 0.7f);
                Part(p, new Vector3(wx + 1.8f, 0.45f, 4.4f), new Vector3(0.8f, 0.9f, 0.8f), C("#c86a3c"), 1f);
                Reg(it, wx + 1.8f, 4.4f, 0.8f, 0.8f);
                Sofa(p, new Vector3(wx - 1.6f, 0f, 6.2f), 2.0f, 90f, C("#7a5a6a"));
                Reg(it, wx - 1.6f, 6.2f, 1.0f, 2.0f);
                SignText(p, new Vector3(CENTER_X - 15.88f, 2.45f, 1.7f), 270f, "音乐社", 0.030f, MatLib.Hex("#f3e9d2"));
            }
            else
            {
                // 阅读/棋艺社团：书架 + 两张棋桌
                ShelfWallZ(p, it, CENTER_X + X - 0.45f, -2.8f, 6.8f, -1, C("#6f5540"),
                           new[] { C("#8c4a3c"), C("#3f5c6b"), C("#8a7440"), C("#6b7a4a") }, 2.1f);
                for (int i = 0; i < 2; i++)
                {
                    Table(p, new Vector3(wx - 1.4f, 0f, 1.0f + i * 3.4f), 1.4f, 1.4f, 0.74f, wood);
                    Reg(it, wx - 1.4f, 1.0f + i * 3.4f, 1.4f, 1.4f);
                    Chair(p, it, new Vector3(wx - 2.4f, 0f, 1.0f + i * 3.4f), 90f, wood, true);
                    Chair(p, it, new Vector3(wx - 0.4f, 0f, 1.0f + i * 3.4f), 270f, wood, true);
                }
                SignText(p, new Vector3(CENTER_X + 15.88f, 2.45f, 1.7f), 90f, "棋艺社", 0.030f, MatLib.Hex("#f3e9d2"));
            }
        }

        // 门厅：接待台 + 沙发 + 茶几 + 公告板
        Part(p, new Vector3(CENTER_X - 12.5f, 0.50f, 12.0f), new Vector3(4.0f, 1.0f, 0.9f), C("#6a3a32"), 1f);
        Part(p, new Vector3(CENTER_X - 12.5f, 1.03f, 12.0f), new Vector3(4.1f, 0.07f, 0.96f), C("#dfe6dc"), 1f);
        Reg(it, CENTER_X - 12.5f, 12.0f, 4.0f, 0.9f);
        SignText(p, new Vector3(CENTER_X - 12.5f, 1.30f, 11.52f), 0f, "活动中心服务台", 0.028f, MatLib.Hex("#dfe6dc"));
        Sofa(p, new Vector3(CENTER_X + 10.5f, 0f, 11.0f), 2.2f, 270f, C("#7a5a6a"));
        Reg(it, CENTER_X + 10.5f, 11.0f, 1.0f, 2.2f);
        Part(p, new Vector3(CENTER_X + 8.2f, 0.34f, 11.0f), new Vector3(1.1f, 0.08f, 0.7f), wood, 1f);
        Part(p, new Vector3(CENTER_X + 8.2f, 0.17f, 11.0f), new Vector3(0.9f, 0.34f, 0.55f), darkWood, 1f);
        Reg(it, CENTER_X + 8.2f, 11.0f, 1.1f, 0.7f);
        Rug(p, new Vector3(CENTER_X, 0.06f, 11.0f), 15f, 4.0f, C("#8a6a5a"));
        Part(p, new Vector3(CENTER_X, 1.7f, 15.82f), new Vector3(3.2f, 1.0f, 0.05f), C("#5a3f2c"), 1f);
        Picture(p, new Vector3(CENTER_X - 18f, 2.1f, 15.82f), 1.6f, 1.0f, 0f);
        Picture(p, new Vector3(CENTER_X + 18f, 2.1f, 15.82f), 1.6f, 1.0f, 0f);
        Plant(p, new Vector3(CENTER_X - 20.5f, 0.02f, 14.8f), 1.2f);
        Plant(p, new Vector3(CENTER_X + 20.5f, 0.02f, 14.8f), 1.2f);

        // 侧墙高窗 / 吊灯 / 顶部灯带
        foreach (float sz in new[] { -8f, 0f, 8f })
        {
            Window(p, new Vector3(CENTER_X - 24.80f, 3.8f, sz), 3.2f, 1.4f, 90f);
            Window(p, new Vector3(CENTER_X + 24.80f, 3.8f, sz), 3.2f, 1.4f, -90f);
        }
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 2; j++)
                Pendant(p, CENTER_X - 10f + i * 10f, W - 1.25f, j == 0 ? -6f : 4f, 1.1f, "#e0c8a0");
        for (int i = 0; i < 3; i++)
            foreach (float hz in new[] { -7f, 4f })
                Lamp(p, new Vector3(CENTER_X - 15f + i * 15f, W - 0.55f, hz), 15f, 1.35f);
        Lamp(p, new Vector3(CENTER_X - 10f, W - 0.35f, 12f), 12f, 1.1f);
        Lamp(p, new Vector3(CENTER_X + 10f, W - 0.35f, 12f), 12f, 1.1f);
        return it;
    }

    // ── 艺术楼：美术教室 / 音乐教室 / 两间琴房 / 舞蹈房 ──
    static void Easel(Transform p, Interior it, float x, float z, float ry, Material wood, Material canvas)
    {
        var g = new GameObject("easel").transform;
        g.SetParent(p, false);
        g.localPosition = new Vector3(x, 0f, z);
        g.localEulerAngles = new Vector3(0f, ry, 0f);
        for (int i = 0; i < 3; i++)
            Part(g, new Vector3(i == 0 ? -0.42f : i == 1 ? 0.42f : 0f, 0.72f, i == 2 ? 0.32f : -0.12f),
                 new Vector3(0.06f, 1.44f, 0.06f), wood, 0.7f);
        Part(g, new Vector3(0f, 1.12f, -0.10f), new Vector3(0.86f, 0.08f, 0.38f), wood, 0.8f);
        Part(g, new Vector3(0f, 1.52f, -0.10f), new Vector3(0.76f, 0.70f, 0.04f), canvas, 1f);
        Part(g, new Vector3(0f, 1.52f, -0.07f), new Vector3(0.64f, 0.58f, 0.02f), C("#b8c8b0"), 1f);
        Reg(it, x, z, 0.95f, 0.55f);
    }

    static void Piano(Transform p, Interior it, float x, float z, float ry, bool grand)
    {
        var g = new GameObject("piano").transform;
        g.SetParent(p, false);
        g.localPosition = new Vector3(x, 0f, z);
        g.localEulerAngles = new Vector3(0f, ry, 0f);
        if (grand)
        {
            Part(g, new Vector3(0f, 0.62f, 0f), new Vector3(2.6f, 0.35f, 1.5f), C("#1f2126"), 1f);
            Part(g, new Vector3(0f, 0.85f, -0.10f), new Vector3(2.2f, 0.06f, 1.1f), C("#2f3338"), 1f);
            Part(g, new Vector3(0f, 0.72f, 0.78f), new Vector3(2.2f, 0.10f, 0.32f), C("#f2f0e6"), 1f);
            Part(g, new Vector3(0f, 0.72f, 0.62f), new Vector3(1.9f, 0.04f, 0.18f), C("#dfe6dc"), 1f);
            for (int i = 0; i < 4; i++)
                Part(g, new Vector3(i % 2 == 0 ? -1.1f : 1.1f, 0.22f, i < 2 ? -0.6f : 0.6f),
                     new Vector3(0.10f, 0.44f, 0.10f), C("#1f2126"), 0.6f);
            Reg(it, x, z, 2.6f, 1.5f);
        }
        else
        {
            Part(g, new Vector3(0f, 0.65f, 0f), new Vector3(1.4f, 1.1f, 0.6f), C("#2f3338"), 1f);
            Part(g, new Vector3(0f, 0.78f, 0.34f), new Vector3(1.2f, 0.10f, 0.24f), C("#f2f0e6"), 1f);
            Reg(it, x, z, 1.4f, 0.6f);
        }
    }

    static void ArtRoom(Transform p, Interior it, float cx, float cz, float x0, float x1, float z0, float z1, bool west)
    {
        var wood = C("#8a6a4a");
        for (int i = 0; i < 6; i++)
        {
            float x = west ? x0 + 2.2f + (i % 3) * 2.0f : x1 - 2.2f - (i % 3) * 2.0f;
            float z = cz - 4.0f + (i / 3) * 7.0f;
            Easel(p, it, x, z, 20f, wood, C("#efe6d2"));
        }
        for (int i = 0; i < 2; i++)
        {
            float z = cz - 3.5f + i * 7.0f;
            Table(p, new Vector3(cx, 0f, z), 4.0f, 1.2f, 0.76f, wood);
            Reg(it, cx, z, 4.0f, 1.2f);
            for (int j = 0; j < 4; j++)
                Stool(p, it, cx - 1.5f + j * 1.0f, z - 1.0f, C("#7a9aa6"));
        }
        float outerX = west ? x0 + 0.55f : x1 - 0.55f;
        Part(p, new Vector3(outerX, 0.92f, cz + 5.0f), new Vector3(0.65f, 0.12f, 2.0f), C("#c3c8cf"), 1f);
        Reg(it, outerX, cz + 5.0f, 0.65f, 2.0f);
        for (int i = 0; i < 3; i++)
            Part(p, new Vector3(west ? x0 + 0.3f : x1 - 0.3f, 1.8f, cz - 4.0f + i * 1.2f),
                 new Vector3(0.45f, 1.0f, 0.9f), C("#d8d2c6"), 1f);
        Lamp(p, new Vector3(cx, 3.85f, cz), 11f, 1.2f);
    }

    static Interior Arts(Transform parent)
    {
        const float X = 23f, Z = 15f, W = 4.2f;
        var wall = T("in_wall2");
        var it = Shell(parent, "arts", "艺术楼", ARTS_X, X, Z, W, 1.4f, 2.6f,
                       wall, T("in_floor"), T("in_ceil"), wall, C("#4e6a58"), 0.58f);
        var p = it.Root.transform;
        var wood = C("#8a6a4a");

        // 走廊 + 北侧美术/音乐教室 + 南侧琴房/舞蹈房
        foreach (float sx in new[] { -1f, 1f })
        {
            WallZ(p, it, -Z, 2f, ARTS_X + sx * 1.8f, 0.02f, W, -5.7f, -4.3f, 2.2f, wall, WALL_IN);
            WallX(p, it, ARTS_X + sx * 1.8f, ARTS_X + sx * X, 2f, 0.02f, W, 0f, 0f, 2.2f, wall, WALL_IN);
            WallX(p, it, ARTS_X + sx * 1.8f, ARTS_X + sx * X, 4f, 0.02f, W, 0f, 0f, 2.2f, wall, WALL_IN);
        }
        // 东侧舞蹈房：走廊墙一个门洞
        WallZ(p, it, 4f, Z, ARTS_X + 1.8f, 0.02f, W, 6.3f, 7.7f, 2.2f, wall, WALL_IN);
        // 西侧琴房：走廊墙两个门洞（手工分段）
        WallZ(p, it, 4f, 6.4f, ARTS_X - 1.8f, 0.02f, W, 0f, 0f, 2.2f, wall, WALL_IN);
        Part(p, new Vector3(ARTS_X - 1.8f, (W + 2.2f) * 0.5f, 6.85f),
             new Vector3(WALL_IN, W - 2.2f, 0.9f), wall, 1f);
        WallZ(p, it, 7.3f, 10.2f, ARTS_X - 1.8f, 0.02f, W, 0f, 0f, 2.2f, wall, WALL_IN);
        Part(p, new Vector3(ARTS_X - 1.8f, (W + 2.2f) * 0.5f, 10.85f),
             new Vector3(WALL_IN, W - 2.2f, 0.9f), wall, 1f);
        WallZ(p, it, 11.3f, Z, ARTS_X - 1.8f, 0.02f, W, 0f, 0f, 2.2f, wall, WALL_IN);
        // 琴房隔墙 / 南侧房间北墙
        WallZ(p, it, 4f, Z, ARTS_X - 12f, 0.02f, W, 0f, 0f, 2.2f, wall, WALL_IN);

        // 美术教室（西北）
        ArtRoom(p, it, ARTS_X - 12.4f, -6.5f, ARTS_X - X, ARTS_X - 1.9f, -Z, 2f, true);
        SignText(p, new Vector3(ARTS_X - 1.68f, 2.42f, -5f), 270f, "美术教室", 0.030f, MatLib.Hex("#e8f0e4"));

        // 音乐教室（东北）
        Piano(p, it, ARTS_X + 12.0f, -8.0f, 0f, true);
        for (int i = 0; i < 6; i++)
            Chair(p, it, new Vector3(ARTS_X + 5.0f + (i % 3) * 2.4f, 0f, -3.5f + (i / 3) * 2.0f), 180f, wood, true);
        for (int i = 0; i < 3; i++)
        {
            Part(p, new Vector3(ARTS_X + 17.0f + i * 0.9f, 1.6f, -11.0f + i * 2.5f),
                 new Vector3(0.10f, 2.0f, 0.9f), C("#7a9aa6"), 1f);
        }
        Rug(p, new Vector3(ARTS_X + 12.0f, 0.06f, -6.0f), 10f, 5f, C("#6f8a7a"));
        SignText(p, new Vector3(ARTS_X + 1.68f, 2.42f, -5f), 90f, "音乐教室", 0.030f, MatLib.Hex("#e8f0e4"));

        // 琴房 1/2（西南），各一台立式钢琴/键盘
        for (int i = 0; i < 2; i++)
        {
            float cx = ARTS_X - 16.5f + i * 10.0f;
            Piano(p, it, cx, 6.5f, 0f, false);
            Stool(p, it, cx, 8.0f, C("#7a9aa6"));
            for (int j = 0; j < 3; j++)
                Part(p, new Vector3(cx - 3.0f + j * 3.0f, 1.5f, 13.3f),
                     new Vector3(2.0f, 1.0f, 0.10f), C("#7a9aa6"), 1f);
            Lamp(p, new Vector3(cx, 3.85f, 8.5f), 7f, 1.0f);
            SignText(p, new Vector3(ARTS_X - 1.68f, 2.42f, 7.0f + i * 4.0f), 270f,
                     "琴房 " + (i + 1), 0.030f, MatLib.Hex("#e8f0e4"));
        }

        // 舞蹈房（东南）：镜墙 + 把杆 + 地胶 + 音响
        Part(p, new Vector3(ARTS_X + 12.4f, 0.05f, 9.0f), new Vector3(21.2f, 0.02f, 9.5f), C("#b8a88a"), 1.5f);
        Part(p, new Vector3(ARTS_X + 22.65f, 1.55f, 9.0f), new Vector3(0.06f, 2.4f, 8.5f), Glass(), 1f);
        Part(p, new Vector3(ARTS_X + 22.60f, 0.30f, 9.0f), new Vector3(0.10f, 0.30f, 9.0f), C("#5a4432"), 1f);
        Part(p, new Vector3(ARTS_X + 22.60f, 2.80f, 9.0f), new Vector3(0.10f, 0.30f, 9.0f), C("#5a4432"), 1f);
        Part(p, new Vector3(ARTS_X + 21.6f, 1.05f, 9.0f), new Vector3(0.08f, 0.08f, 8.0f), C("#c3c8cf"), 1f);
        foreach (float bz in new[] { 5.5f, 9.0f, 12.5f })
            Part(p, new Vector3(ARTS_X + 21.95f, 1.0f, bz), new Vector3(0.35f, 0.05f, 0.05f), C("#c3c8cf"), 1f);
        Part(p, new Vector3(ARTS_X + 4.2f, 0.28f, 13.2f), new Vector3(2.6f, 0.14f, 0.55f), wood, 1f);
        Reg(it, ARTS_X + 4.2f, 13.2f, 2.6f, 0.55f);
        Part(p, new Vector3(ARTS_X + 20.0f, 0.50f, 4.5f), new Vector3(0.5f, 1.0f, 0.5f), C("#2f3338"), 1f);
        Reg(it, ARTS_X + 20.0f, 4.5f, 0.5f, 0.5f);
        SignText(p, new Vector3(ARTS_X + 1.68f, 2.42f, 7f), 90f, "舞蹈房", 0.030f, MatLib.Hex("#e8f0e4"));

        // 走廊：海报/绿植/长椅
        Rug(p, new Vector3(ARTS_X, 0.06f, 10.8f), 2.2f, 5.0f, C("#7a6a8a"));
        foreach (var (bx, bz, side) in new[] {
            (ARTS_X - 1.72f, 0.0f, 1f), (ARTS_X + 1.72f, 9.0f, -1f) })
        {
            Part(p, new Vector3(bx, 1.60f, bz), new Vector3(0.04f, 0.72f, 1.30f), C("#6f5540"), 1f);
            for (int i = 0; i < 3; i++)
                Part(p, new Vector3(bx + side * 0.03f, 1.52f + (i % 2) * 0.20f, bz - 0.40f + i * 0.40f),
                     new Vector3(0.01f, 0.32f, 0.28f),
                     i == 0 ? C("#dfe6dc") : i == 1 ? C("#c8a05a") : C("#8aaec8"), 1f);
        }
        Plant(p, new Vector3(ARTS_X - 20.5f, 0.02f, 14.0f), 1.0f);
        Plant(p, new Vector3(ARTS_X + 20.5f, 0.02f, 14.0f), 1.0f);
        LightGrid(p, ARTS_X - X + 3f, ARTS_X + X - 3f, -Z + 3f, Z - 3f, W - 0.35f, 4, 4, 14f, 1.25f);
        for (float lz = -8f; lz <= 12f; lz += 5f)
            Lamp(p, new Vector3(ARTS_X, W - 0.35f, lz), 10f, 1.2f);
        return it;
    }

    // ── 生活服务楼：洗衣 / 快递 / 理发 / 医务 / 休息 ──
    static void Washer(Transform p, Interior it, float x, float z, Material shellMat)
    {
        Part(p, new Vector3(x, 0.45f, z), new Vector3(0.78f, 0.9f, 0.74f), shellMat, 1f);
        Part(p, new Vector3(x, 0.48f, z + 0.38f), new Vector3(0.54f, 0.54f, 0.04f), C("#3a4a58"), 1f);
        Part(p, new Vector3(x, 0.48f, z + 0.405f), new Vector3(0.30f, 0.30f, 0.02f), C("#8ab8c8"), 1f);
        Part(p, new Vector3(x, 0.96f, z), new Vector3(0.84f, 0.08f, 0.80f), C("#b9c0c4"), 1f);
        Reg(it, x, z, 0.82f, 0.78f);
    }

    static void BarberChair(Transform p, Interior it, float x, float z, float ry)
    {
        var g = new GameObject("barber").transform;
        g.SetParent(p, false);
        g.localPosition = new Vector3(x, 0f, z);
        g.localEulerAngles = new Vector3(0f, ry, 0f);
        Part(g, new Vector3(0f, 0.18f, 0f), new Vector3(0.7f, 0.20f, 0.7f), C("#3a3f47"), 1f);
        Part(g, new Vector3(0f, 0.62f, 0f), new Vector3(0.62f, 0.24f, 0.62f), C("#7a3a3a"), 1f);
        Part(g, new Vector3(0f, 1.10f, -0.26f), new Vector3(0.62f, 0.85f, 0.14f), C("#7a3a3a"), 1f);
        Part(g, new Vector3(-0.38f, 0.76f, 0f), new Vector3(0.08f, 0.10f, 0.55f), C("#3a3f47"), 1f);
        Part(g, new Vector3(0.38f, 0.76f, 0f), new Vector3(0.08f, 0.10f, 0.55f), C("#3a3f47"), 1f);
        Reg(it, x, z, 0.8f, 0.8f);
    }

    static Interior Service(Transform parent)
    {
        const float X = 21f, Z = 14f, W = 3.4f;
        var wall = T("in_wall2");
        var it = Shell(parent, "service", "生活服务楼", SERVICE_X, X, Z, W, 1.4f, 2.6f,
                       wall, T("in_tile"), T("in_ceil"), wall, C("#8a4a32"), 0.58f);
        var p = it.Root.transform;
        var wood = C("#8a6a4a");
        var steel = C("#c3c8cf");
        var parcel = new[] { C("#b8946a"), C("#a07850"), C("#c8a878"), C("#8a6a4a") };

        // 洗衣区（西北）
        for (int i = 0; i < 3; i++)
            Washer(p, it, SERVICE_X - 19.5f + i * 2.0f, -12.8f, steel);
        for (int i = 0; i < 2; i++)
        {
            float x = SERVICE_X - 18.5f + i * 2.2f;
            Part(p, new Vector3(x, 1.39f, -12.8f), new Vector3(0.78f, 0.9f, 0.74f), steel, 1f);
            Part(p, new Vector3(x, 1.42f, -12.42f), new Vector3(0.54f, 0.54f, 0.04f), C("#3a4a58"), 1f);
        }
        Table(p, new Vector3(SERVICE_X - 13.0f, 0f, -12.2f), 2.4f, 0.9f, 0.80f, wood);
        Reg(it, SERVICE_X - 13.0f, -12.2f, 2.4f, 0.9f);
        Part(p, new Vector3(SERVICE_X - 19.2f, 0.45f, -8.6f), new Vector3(0.7f, 0.9f, 0.6f), steel, 1f);
        Reg(it, SERVICE_X - 19.2f, -8.6f, 0.7f, 0.6f);
        HangingSign(p, SERVICE_X - 16.0f, 2.55f, -10.0f, 1, "洗衣房", 1.5f, 0.40f, C("#3a4a62"), 0.026f);

        // 快递区（东北）
        ShelfWallZ(p, it, SERVICE_X + 20.5f, -12.8f, -4.0f, -1, C("#6f5540"), parcel, 2.1f);
        Part(p, new Vector3(SERVICE_X + 16.0f, 0.48f, -8.0f), new Vector3(4.0f, 0.96f, 1.0f), C("#6a4a32"), 1f);
        Part(p, new Vector3(SERVICE_X + 16.0f, 1.00f, -8.0f), new Vector3(4.1f, 0.07f, 1.06f), C("#dfe6dc"), 1f);
        Reg(it, SERVICE_X + 16.0f, -8.0f, 4.0f, 1.0f);
        Part(p, new Vector3(SERVICE_X + 15.0f, 1.06f, -8.0f), new Vector3(0.5f, 0.10f, 0.4f), C("#3a3f47"), 1f);
        for (int i = 0; i < 4; i++)
            Part(p, new Vector3(SERVICE_X + 13.5f + (i % 2) * 1.0f, 0.20f + (i / 2) * 0.45f, -7.2f),
                 new Vector3(0.8f, 0.4f, 0.55f), parcel[i], 0.8f);
        HangingSign(p, SERVICE_X + 16.0f, 2.55f, -10.0f, 1, "快递收发", 1.7f, 0.40f, C("#6a4a32"), 0.026f);

        // 理发区（西南）
        BarberChair(p, it, SERVICE_X - 16.5f, 5.5f, 90f);
        Part(p, new Vector3(SERVICE_X - 20.85f, 1.60f, 5.5f), new Vector3(0.05f, 1.5f, 1.3f), Glass(), 1f);
        Part(p, new Vector3(SERVICE_X - 20.88f, 1.60f, 5.5f), new Vector3(0.04f, 1.7f, 1.5f), C("#5a4432"), 1f);
        Part(p, new Vector3(SERVICE_X - 18.2f, 0.48f, 8.0f), new Vector3(1.0f, 0.96f, 2.0f), wood, 1f);
        Reg(it, SERVICE_X - 18.2f, 8.0f, 1.0f, 2.0f);
        Part(p, new Vector3(SERVICE_X - 16.0f, 0.30f, 9.8f), new Vector3(2.2f, 0.12f, 0.55f), wood, 1f);
        Part(p, new Vector3(SERVICE_X - 16.0f, 0.15f, 9.8f), new Vector3(0.10f, 0.30f, 0.48f), steel, 1f);
        Reg(it, SERVICE_X - 16.0f, 9.8f, 2.2f, 0.55f);
        HangingSign(p, SERVICE_X - 16.0f, 2.55f, 3.0f, 1, "理发", 1.2f, 0.40f, C("#7a3a3a"), 0.026f);

        // 医务室（东南）
        Part(p, new Vector3(SERVICE_X + 16.5f, 0.38f, 7.0f), new Vector3(1.0f, 0.16f, 2.0f), steel, 1f);
        Part(p, new Vector3(SERVICE_X + 16.5f, 0.52f, 7.0f), new Vector3(0.94f, 0.16f, 1.9f), C("#f2f0e6"), 1f);
        Part(p, new Vector3(SERVICE_X + 16.5f, 0.64f, 7.7f), new Vector3(0.6f, 0.12f, 0.3f), C("#e8e2d4"), 1f);
        for (int i = 0; i < 4; i++)
            Part(p, new Vector3(SERVICE_X + 16.5f + (i % 2 == 0 ? -0.42f : 0.42f), 0.18f, 7.0f + (i < 2 ? -0.85f : 0.85f)),
                 new Vector3(0.08f, 0.36f, 0.08f), steel, 0.6f);
        Reg(it, SERVICE_X + 16.5f, 7.0f, 1.0f, 2.0f);
        Curtain(p, SERVICE_X + 16.5f, 0f, 8.4f, 3.0f, 2.0f, C("#b8c8c0"));
        Part(p, new Vector3(SERVICE_X + 19.8f, 0.9f, 4.0f), new Vector3(0.65f, 1.8f, 1.2f), C("#c3c8cf"), 1f);
        Reg(it, SERVICE_X + 19.8f, 4.0f, 0.65f, 1.2f);
        Table(p, new Vector3(SERVICE_X + 15.0f, 0f, 3.2f), 2.0f, 0.9f, 0.76f, wood);
        Reg(it, SERVICE_X + 15.0f, 3.2f, 2.0f, 0.9f);
        Chair(p, it, new Vector3(SERVICE_X + 14.2f, 0f, 4.2f), 180f, wood, true);
        HangingSign(p, SERVICE_X + 16.5f, 2.55f, 10.0f, 1, "医务室", 1.5f, 0.40f, C("#4e6a58"), 0.026f);

        // 中央休息区 + 打印/咨询台
        Rug(p, new Vector3(SERVICE_X, 0.06f, 5.2f), 8.0f, 4.0f, C("#8a6a5a"));
        Sofa(p, new Vector3(SERVICE_X - 2.4f, 0f, 4.6f), 2.0f, 90f, C("#7a5a6a"));
        Reg(it, SERVICE_X - 2.4f, 4.6f, 1.0f, 2.0f);
        Sofa(p, new Vector3(SERVICE_X + 2.4f, 0f, 4.6f), 2.0f, 270f, C("#7a5a6a"));
        Reg(it, SERVICE_X + 2.4f, 4.6f, 1.0f, 2.0f);
        Part(p, new Vector3(SERVICE_X, 0.34f, 4.6f), new Vector3(1.5f, 0.08f, 0.8f), wood, 1f);
        Part(p, new Vector3(SERVICE_X, 0.17f, 4.6f), new Vector3(1.2f, 0.34f, 0.6f), C("#5a3f2c"), 1f);
        Reg(it, SERVICE_X, 4.6f, 1.5f, 0.8f);

        Part(p, new Vector3(SERVICE_X, 0.50f, -6.0f), new Vector3(3.4f, 1.0f, 0.9f), C("#4e6a58"), 1f);
        Part(p, new Vector3(SERVICE_X, 1.03f, -6.0f), new Vector3(3.5f, 0.07f, 0.96f), C("#dfe6dc"), 1f);
        Reg(it, SERVICE_X, -6.0f, 3.4f, 0.9f);
        Part(p, new Vector3(SERVICE_X, 1.30f, -6.48f), new Vector3(1.8f, 0.34f, 0.05f), C("#2f4a3a"), 1f);
        SignText(p, new Vector3(SERVICE_X, 1.30f, -6.52f), 0f, "打印 / 咨询", 0.026f, MatLib.Hex("#dfe6dc"));

        foreach (float sz in new[] { 0f, 7f })
        {
            Window(p, new Vector3(SERVICE_X - 20.80f, 2.25f, sz), 3.0f, 1.5f, 90f);
            Window(p, new Vector3(SERVICE_X + 20.80f, 2.25f, sz), 3.0f, 1.5f, -90f);
        }
        Plant(p, new Vector3(SERVICE_X - 20.0f, 0.02f, 13.0f), 1.1f);
        Plant(p, new Vector3(SERVICE_X + 20.0f, 0.02f, 13.0f), 1.1f);
        WallClock(p, new Vector3(SERVICE_X - 20.82f, 2.55f, 0f), 90f);
        LightGrid(p, SERVICE_X - X + 3f, SERVICE_X + X - 3f, -Z + 3f, Z - 3f, W - 0.35f, 4, 3, 15f, 1.55f);
        Lamp(p, new Vector3(SERVICE_X, W - 0.35f, 4.6f), 10f, 1.2f);
        Lamp(p, new Vector3(SERVICE_X, W - 0.35f, 10f), 10f, 1.2f);
        return it;
    }
}
