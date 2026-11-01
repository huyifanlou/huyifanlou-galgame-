using UnityEngine;

// 学校区域三所室内（单层，均约为同类旧室内的 3 倍面积）：
//   宿舍楼 58×39：中央走廊 + 12 间四人间（四张上床下桌沿左右墙相对布置）
//                 + 2 间盥洗/洗衣房 + 门厅公共区
//   礼堂见 InteriorBuilder.Auditorium.cs（80×60）
//   实验楼见 InteriorBuilder.SchoolLabs.cs（78×52）
public static partial class InteriorBuilder
{
    const float DORM_X = 1540f, AUD_X = 1670f, LABS_X = 1800f;

    // 通用外壳：地板/天花/四面墙/出口门/门灯/门牌/墙纸
    static Interior Shell(Transform parent, string id, string name, float ox, float X, float Z, float W,
                          float doorHalf, float doorH, Material wall, Material floor, Material ceil,
                          Material paper, Material signPlate = null, float signH = 0.58f)
    {
        var it = new Interior
        {
            Id = id, Name = name,
            X1 = ox - X, X2 = ox + X, Z1 = -Z, Z2 = Z,
            Spawn = new Vector2(ox, Z - 2.6f), Exit = new Vector2(ox, Z - 1.5f),
            FloorY = new[] { 0.02f },
            ClearX1 = ox - doorHalf - 0.4f, ClearX2 = ox + doorHalf + 0.4f,
            ClearZ1 = Z - 6.0f, ClearZ2 = Z,
        };
        var root = new GameObject("Interior_" + id);
        root.transform.SetParent(parent, false);
        it.Root = root;
        var p = root.transform;

        Part(p, new Vector3(ox, 0f, 0f), new Vector3(X * 2f, 0.04f, Z * 2f), floor, 2f, default, true);
        CeilingSlab(p, ox, W, X * 2f, Z * 2f, ceil);
        WallX(p, it, ox - X, ox + X, -Z, 0f, W, 0f, 0f, doorH, wall);
        WallX(p, it, ox - X, ox + X, Z, 0f, W, ox - doorHalf, ox + doorHalf, doorH, wall);
        WallZ(p, it, -Z, Z, ox - X, 0f, W, 0f, 0f, doorH, wall);
        WallZ(p, it, -Z, Z, ox + X, 0f, W, 0f, 0f, doorH, wall);
        DoorFrame(p, it, ox, Z, doorHalf, doorH, -1);
        WallSign(p, ox, Mathf.Min(doorH + 0.55f, W - 0.42f), Z, -1, name, 2.4f, signH,
                 signPlate ?? C("#2f4a3a"), 0.040f);
        if (paper != null)
            PaperRoom(p, ox - X, ox + X, -Z, Z, W, paper, 1, ox - doorHalf, ox + doorHalf);
        return it;
    }

    static void LightGrid(Transform p, float x0, float x1, float z0, float z1, float y,
                          int nx, int nz, float range, float intensity)
    {
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                float x = nx == 1 ? (x0 + x1) * 0.5f : Mathf.Lerp(x0, x1, i / (float)(nx - 1));
                float z = nz == 1 ? (z0 + z1) * 0.5f : Mathf.Lerp(z0, z1, j / (float)(nz - 1));
                Lamp(p, new Vector3(x, y, z), range, intensity);
            }
    }

    // ── 宿舍楼 ────────────────────────────────────────────
    // 上床下桌：2m 长 × 1m 宽，床头在局部 -z；ry=0 时床宽沿 X、床长沿 Z（贴左右墙用）
    static void LoftBed(Transform p, Interior it, float x, float z, float ry,
                        Material frame, Material desk, Material mattress)
    {
        var g = new GameObject("loftbed").transform;
        g.SetParent(p, false);
        g.localPosition = new Vector3(x, 0f, z);
        g.localEulerAngles = new Vector3(0f, ry, 0f);

        for (int i = 0; i < 4; i++)
            Part(g, new Vector3(i % 2 == 0 ? -0.45f : 0.45f, 0.9f, i < 2 ? -0.95f : 0.95f),
                 new Vector3(0.08f, 1.8f, 0.08f), frame, 0.8f);
        Part(g, new Vector3(0f, 1.55f, 0f), new Vector3(1.0f, 0.12f, 2.0f), frame, 1f);
        Part(g, new Vector3(0f, 1.66f, 0f), new Vector3(0.92f, 0.12f, 1.86f), mattress, 1.2f);
        Part(g, new Vector3(0f, 1.77f, -0.68f), new Vector3(0.72f, 0.09f, 0.32f), C("#e8e2d4"), 1f);
        // 桌下书桌 + 桌腿
        Part(g, new Vector3(0f, 0.74f, 0f), new Vector3(0.92f, 0.06f, 1.72f), desk, 1f);
        for (int i = 0; i < 4; i++)
            Part(g, new Vector3(i % 2 == 0 ? -0.40f : 0.40f, 0.36f, i < 2 ? -0.76f : 0.76f),
                 new Vector3(0.06f, 0.72f, 0.06f), frame, 0.6f);
        // 梯子
        Part(g, new Vector3(0.55f, 0.35f, 0.62f), new Vector3(0.10f, 0.70f, 0.10f), frame, 0.6f);
        for (int i = 0; i < 3; i++)
            Part(g, new Vector3(0.55f, 0.55f + i * 0.34f, 0.48f + i * 0.10f),
                 new Vector3(0.34f, 0.05f, 0.10f), frame, 0.6f);
        TableLamp(g, new Vector3(0.18f, 0.77f, -0.55f), "#f0dfb8");

        bool swap = Mathf.Abs(Mathf.Sin(ry * Mathf.Deg2Rad)) > 0.5f;
        Reg(it, x, z, swap ? 2.0f : 1.0f, swap ? 1.0f : 2.0f);
    }

    // 室内门：门套 + 开向房间一侧的门扇（只做视觉，不参与碰撞，保证可通行）
    static void DormDoor(Transform p, float doorX, float zWall, int dir, Material leaf)
    {
        var fr = C("#6f5540");
        Part(p, new Vector3(doorX - 0.79f, 1.11f, zWall), new Vector3(0.14f, 2.22f, 0.30f), fr, 1f);
        Part(p, new Vector3(doorX + 0.79f, 1.11f, zWall), new Vector3(0.14f, 2.22f, 0.30f), fr, 1f);
        Part(p, new Vector3(doorX, 2.29f, zWall), new Vector3(1.72f, 0.14f, 0.30f), fr, 1f);
        Part(p, new Vector3(doorX - 0.74f, 1.05f, zWall + dir * 0.56f),
             new Vector3(0.06f, 2.06f, 0.94f), leaf, 1f);
        Part(p, new Vector3(doorX - 0.70f, 1.02f, zWall + dir * 0.98f),
             new Vector3(0.05f, 0.05f, 0.12f), C("#c8a05a"), 1f);
    }

    // 一间四人间：四张上床下桌，左墙两张 / 右墙两张相对，中间 4~6m 通道；
    // 近门侧衣柜，远窗侧沙发/茶几/电视/书架。
    static void DormRoom(Transform p, Interior it, float x0, float x1, float zWall, int dir,
                         string label, Material frame, Material desk, Material accent)
    {
        float doorX = (x0 + x1) * 0.5f;
        float zFace = zWall + dir * 0.09f;
        float zWin = dir < 0 ? -19.38f : 19.38f;
        float cx = (x0 + x1) * 0.5f;

        // 门套/开着的门/门牌
        DormDoor(p, doorX, zWall, dir, C("#8f6d50"));
        WallSign(p, doorX + 1.14f, 2.46f, zWall, -dir, label, 0.72f, 0.34f, C("#2f4a3a"), 0.026f);

        // 四张上床下桌：左两张、右两张
        float[] bz = { zFace + dir * 4.0f, zFace + dir * 6.6f };
        foreach (float z in bz)
        {
            LoftBed(p, it, x0 + 0.55f, z, 0f, frame, desk, C("#e8e2d4"));
            LoftBed(p, it, x1 - 0.55f, z, 0f, frame, desk, C("#e8e2d4"));
            Chair(p, it, new Vector3(x0 + 1.38f, 0f, z), 270f, frame, true);
            Chair(p, it, new Vector3(x1 - 1.38f, 0f, z), 90f, frame, true);
        }
        Rug(p, new Vector3(cx, 0.055f, zFace + dir * 5.3f), 4.2f, 4.0f, accent);

        // 门口衣柜两只（避开 1.4m 门洞）
        foreach (float sx in new[] { -1f, 1f })
        {
            float wx = doorX + sx * 1.58f;
            float wz = zFace + dir * 0.32f;
            Part(p, new Vector3(wx, 1.0f, wz), new Vector3(1.3f, 2.0f, 0.62f), frame, 1f);
            Part(p, new Vector3(wx, 1.0f, wz + dir * 0.325f), new Vector3(1.14f, 1.9f, 0.03f), C("#8a7a66"), 1f);
            Part(p, new Vector3(wx, 1.02f, wz + dir * 0.345f), new Vector3(0.03f, 0.07f, 0.11f), C("#c8a05a"), 1f);
            Reg(it, wx, wz, 1.3f, 0.62f);
        }

        // 远窗侧：沙发 + 茶几 + 电视 + 书架 + 绿植
        float zLiv = zFace + dir * 12.4f;
        Sofa(p, new Vector3(x0 + 0.55f, 0f, zLiv), 2.2f, 90f, accent);
        Reg(it, x0 + 0.55f, zLiv, 1.0f, 2.2f);
        Table(p, new Vector3(x0 + 2.55f, 0f, zLiv), 1.4f, 0.8f, 0.42f, desk);
        Reg(it, x0 + 2.55f, zLiv, 1.4f, 0.8f);
        TableLamp(p, new Vector3(x0 + 2.55f, 0.42f, zLiv + 0.2f), "#e8d6ae");
        Rug(p, new Vector3(cx + 0.3f, 0.056f, zLiv), 5.0f, 4.4f, C("#8a7a66"));

        Part(p, new Vector3(x1 - 0.30f, 0.30f, zLiv), new Vector3(0.52f, 0.60f, 2.5f), desk, 1f);
        Reg(it, x1 - 0.30f, zLiv, 0.52f, 2.5f);
        Part(p, new Vector3(x1 - 0.20f, 1.35f, zLiv), new Vector3(0.08f, 1.05f, 1.85f), C("#1f2126"), 1f);
        Part(p, new Vector3(x1 - 0.24f, 1.35f, zLiv), new Vector3(0.02f, 0.92f, 1.70f), C("#2a3f52"), 1f);
        Part(p, new Vector3(x1 - 0.60f, 1.05f, zLiv), new Vector3(0.05f, 0.08f, 0.50f), C("#3a3f47"), 1f);

        float zShelf = zFace + dir * 15.7f;
        ShelfWallZ(p, it, x1 - 0.28f, Mathf.Min(zShelf - 1.1f, zShelf + 1.1f),
                   Mathf.Max(zShelf - 1.1f, zShelf + 1.1f), -1, frame,
                   new[] { C("#8c4a3c"), C("#3f5c6b"), C("#8a7440"), C("#6b7a4a"), C("#a9a08a") }, 2.1f);
        Plant(p, new Vector3(x1 - 0.85f, 0.02f, zWin + dir * 0.75f), 1.0f);

        // 窗 / 挂画 / 主灯
        Window(p, new Vector3(cx, 2.25f, zWin - dir * 0.16f), 4.2f, 1.8f, 0f);
        Picture(p, new Vector3(x0 + 0.17f, 2.15f, zLiv), 1.4f, 0.9f, 90f);
        Lamp(p, new Vector3(cx, 3.22f, zFace + dir * 8.4f), 18f, 1.3f);
        Lamp(p, new Vector3(cx, 3.22f, zLiv), 12f, 0.9f);
    }

    // 盥洗/洗衣房：近门洗衣机+烘干机，中段厕所/淋浴，尽端洗手台+晾衣架
    static void DormWash(Transform p, Interior it, float x0, float x1, float zWall, int dir, string label)
    {
        float doorX = (x0 + x1) * 0.5f;
        float zFace = zWall + dir * 0.09f;
        float zWin = dir < 0 ? -19.38f : 19.38f;
        float cx = (x0 + x1) * 0.5f;

        DormDoor(p, doorX, zWall, dir, C("#7b8798"));
        WallSign(p, doorX + 1.14f, 2.46f, zWall, -dir, label, 0.72f, 0.34f, C("#3a4a62"), 0.026f);

        // 瓷砖地
        Part(p, new Vector3(cx, 0.036f, (zFace + zWin) * 0.5f),
             new Vector3(x1 - x0 - 0.2f, 0.012f, Mathf.Abs(zWin - zFace) - 0.2f), T("in_tile"), 1.6f);

        // 洗衣机 / 烘干机（近门两侧）
        for (int i = 0; i < 3; i++)
        {
            float z = zFace + dir * (1.15f + i * 0.95f);
            Part(p, new Vector3(x0 + 0.55f, 0.48f, z), new Vector3(0.80f, 0.96f, 0.82f), C("#d8dde0"), 1f);
            Part(p, new Vector3(x0 + 0.96f, 0.50f, z), new Vector3(0.03f, 0.52f, 0.52f), C("#3a4a58"), 1f);
            Part(p, new Vector3(x0 + 0.55f, 0.99f, z), new Vector3(0.86f, 0.06f, 0.88f), C("#b9c0c4"), 1f);
            Reg(it, x0 + 0.55f, z, 0.86f, 0.88f);

            Part(p, new Vector3(x1 - 0.55f, 0.48f, z), new Vector3(0.80f, 0.96f, 0.82f), C("#cdd4d8"), 1f);
            Part(p, new Vector3(x1 - 0.96f, 0.50f, z), new Vector3(0.03f, 0.52f, 0.52f), C("#4a4f58"), 1f);
            Part(p, new Vector3(x1 - 0.55f, 0.99f, z), new Vector3(0.86f, 0.06f, 0.88f), C("#aeb6ba"), 1f);
            Reg(it, x1 - 0.55f, z, 0.86f, 0.88f);
        }

        // 厕所隔间（西侧 4 间）
        for (int i = 0; i < 4; i++)
        {
            float zc = zFace + dir * (5.4f + i * 1.35f);
            Part(p, new Vector3(x0 + 1.45f, 1.0f, zc), new Vector3(0.05f, 2.0f, 1.30f), C("#8a929a"), 1f);
            Part(p, new Vector3(x0 + 0.50f, 0.22f, zc), new Vector3(0.62f, 0.42f, 0.70f), C("#eef2f2"), 1f);
            Part(p, new Vector3(x0 + 0.50f, 0.62f, zc - 0.32f), new Vector3(0.58f, 0.40f, 0.11f), C("#dfe6e6"), 1f);
            Reg(it, x0 + 0.50f, zc, 0.64f, 0.72f);
        }
        Part(p, new Vector3(x0 + 1.45f, 1.0f, zFace + dir * 7.5f), new Vector3(0.05f, 2.0f, 4.2f), C("#8a929a"), 1f);

        // 淋浴间（东侧 4 间）
        for (int i = 0; i < 4; i++)
        {
            float zc = zFace + dir * (5.4f + i * 1.35f);
            Part(p, new Vector3(x1 - 0.55f, 0.10f, zc), new Vector3(1.0f, 0.18f, 1.15f), C("#c8d0d4"), 1f);
            Part(p, new Vector3(x1 - 1.08f, 1.1f, zc), new Vector3(0.03f, 2.0f, 1.15f), Glass(), 1f);
            Part(p, new Vector3(x1 - 0.55f, 2.15f, zc), new Vector3(0.05f, 0.05f, 0.70f), C("#8d99a6"), 1f);
            Part(p, new Vector3(x1 - 0.55f, 2.02f, zc), new Vector3(0.22f, 0.04f, 0.22f), C("#dfe6dc"), 1f);
            Reg(it, x1 - 0.55f, zc, 1.02f, 1.17f);
        }
        Part(p, new Vector3(x1 - 1.08f, 1.1f, zFace + dir * 7.5f), new Vector3(0.03f, 2.0f, 4.2f), Glass(), 1f);

        // 尽端洗手台 + 镜子 + 晾衣架 + 长凳
        float zSink = zWin - dir * 0.55f;
        Part(p, new Vector3(cx, 0.84f, zSink), new Vector3(3.8f, 0.10f, 0.62f), C("#dfe6dc"), 1f);
        Part(p, new Vector3(cx, 0.42f, zSink), new Vector3(3.6f, 0.76f, 0.50f), C("#c8d0d4"), 1f);
        for (int i = 0; i < 3; i++)
        {
            float sx = cx - 1.2f + i * 1.2f;
            Part(p, new Vector3(sx, 0.90f, zSink), new Vector3(0.62f, 0.08f, 0.42f), C("#eef2f2"), 1f);
            Part(p, new Vector3(sx, 1.55f, zWin - dir * 0.14f), new Vector3(0.64f, 0.86f, 0.03f), Glass(), 1f);
        }
        Reg(it, cx, zSink, 3.8f, 0.62f);

        Part(p, new Vector3(cx, 0.24f, zFace + dir * 11.2f), new Vector3(2.0f, 0.10f, 0.52f), C("#8a6a4a"), 1f);
        Part(p, new Vector3(cx, 0.12f, zFace + dir * 11.2f), new Vector3(0.12f, 0.24f, 0.46f), C("#6f5540"), 1f);
        Reg(it, cx, zFace + dir * 11.2f, 2.0f, 0.52f);

        Part(p, new Vector3(cx, 1.05f, zFace + dir * 13.0f), new Vector3(2.2f, 0.06f, 0.06f), C("#8d99a6"), 1f);
        for (int i = 0; i < 2; i++)
            Part(p, new Vector3(cx - 1.0f + i * 2.0f, 1.05f, zFace + dir * 13.0f),
                 new Vector3(0.06f, 2.0f, 0.06f), C("#8d99a6"), 1f);
        for (int i = 0; i < 4; i++)
            Part(p, new Vector3(cx - 0.75f + i * 0.5f, 0.82f, zFace + dir * 13.0f),
                 new Vector3(0.34f, 0.46f, 0.03f), C("#c8d8e0"), 1f);

        Window(p, new Vector3(cx, 2.55f, zWin - dir * 0.16f), 3.2f, 1.2f, 0f);
        Lamp(p, new Vector3(cx, 3.22f, zFace + dir * 3.6f), 14f, 1.3f);
        Lamp(p, new Vector3(cx, 3.22f, zFace + dir * 12.0f), 14f, 1.3f);
    }

    static Interior Dorm(Transform parent)
    {
        const float X = 29f, Z = 19.5f, W = 3.6f;
        var wall = T("in_wall2");
        var it = Shell(parent, "dorm", "宿舍楼", DORM_X, X, Z, W, 1.6f, 2.6f,
                       wall, T("in_floor"), T("in_ceil"), wall, C("#6f4a32"), 0.60f);
        var p = it.Root.transform;
        var frame = C("#8a5a3c");
        var desk = C("#a87a52");
        var accent = C("#8a9a7a");

        // 中央走廊 z=-2..2；两端外墙
        Part(p, new Vector3(DORM_X, 0.044f, 0f), new Vector3(56.0f, 0.012f, 3.2f), C("#8a7a66"), 1.6f);
        Rug(p, new Vector3(DORM_X - 14f, 0.062f, 0f), 25.0f, 1.8f, C("#7a8a6a"));
        Rug(p, new Vector3(DORM_X + 14f, 0.062f, 0f), 25.0f, 1.8f, C("#7a8a6a"));

        // 北侧 8 间：7 间四人间 + 1 间盥洗/洗衣
        var north = new[] {
            (x0: -28.88f, x1: -21.8175f, door: -25.35f, label: "201"),
            (x0: -21.6375f, x1: -14.575f, door: -18.10f, label: "202"),
            (x0: -14.395f, x1: -7.3325f, door: -10.86f, label: "203"),
            (x0: -7.1525f, x1: -0.09f, door: -3.62f, label: "204"),
            (x0: 0.09f, x1: 7.1525f, door: 3.62f, label: "205"),
            (x0: 7.3325f, x1: 14.395f, door: 10.86f, label: "206"),
            (x0: 14.575f, x1: 21.6375f, door: 18.10f, label: "207"),
            (x0: 21.8175f, x1: 28.88f, door: 25.35f, label: "盥洗"),
        };
        foreach (var r in north)
        {
            WallX(p, it, DORM_X + r.x0, DORM_X + r.x1, -2f, 0.02f, W,
                  DORM_X + r.door - 0.7f, DORM_X + r.door + 0.7f, 2.2f, wall, WALL_IN);
            if (r.label == "盥洗")
                DormWash(p, it, DORM_X + r.x0, DORM_X + r.x1, -2f, -1, r.label);
            else
                DormRoom(p, it, DORM_X + r.x0, DORM_X + r.x1, -2f, -1, r.label, frame, desk, accent);
        }
        foreach (float x in new[] { -21.7275f, -14.485f, -7.2425f, 0f, 7.2425f, 14.485f, 21.7275f })
            WallZ(p, it, -19.38f, -2f, DORM_X + x, 0.02f, W, 0f, 0f, 2.4f, wall, WALL_IN);

        // 南侧 6 间：5 间四人间 + 1 间盥洗/洗衣；中间 8m 门厅
        var south = new[] {
            (x0: -28.88f, x1: -20.737f, door: -24.8085f, label: "盥洗"),
            (x0: -20.557f, x1: -12.414f, door: -16.4855f, label: "208"),
            (x0: -12.234f, x1: -4.09f, door: -8.162f, label: "209"),
            (x0: 4.09f, x1: 12.234f, door: 8.162f, label: "210"),
            (x0: 12.414f, x1: 20.557f, door: 16.4855f, label: "211"),
            (x0: 20.737f, x1: 28.88f, door: 24.8085f, label: "212"),
        };
        foreach (var r in south)
        {
            WallX(p, it, DORM_X + r.x0, DORM_X + r.x1, 2f, 0.02f, W,
                  DORM_X + r.door - 0.7f, DORM_X + r.door + 0.7f, 2.2f, wall, WALL_IN);
            if (r.label == "盥洗")
                DormWash(p, it, DORM_X + r.x0, DORM_X + r.x1, 2f, 1, r.label);
            else
                DormRoom(p, it, DORM_X + r.x0, DORM_X + r.x1, 2f, 1, r.label, frame, desk, accent);
        }
        foreach (float x in new[] { -20.647f, -12.324f, -4f, 4f, 12.324f, 20.647f })
            WallZ(p, it, 2.09f, 19.38f, DORM_X + x, 0.02f, W, 0f, 0f, 2.4f, wall, WALL_IN);

        // 门厅：接待桌 + 沙发/茶几 + 公告板 + 快递架 + 绿植
        float lz = 10.5f;
        Table(p, new Vector3(DORM_X - 2.6f, 0f, 6.6f), 2.4f, 0.8f, 0.78f, desk);
        Reg(it, DORM_X - 2.6f, 6.6f, 2.4f, 0.8f);
        Chair(p, it, new Vector3(DORM_X - 3.45f, 0f, 6.6f), 90f, frame, true);
        Part(p, new Vector3(DORM_X - 2.6f, 0.86f, 6.2f), new Vector3(0.9f, 0.04f, 0.30f), C("#e6e2d8"), 1f);
        Part(p, new Vector3(DORM_X - 2.6f, 1.02f, 6.34f), new Vector3(1.1f, 0.30f, 0.04f), C("#6f4a32"), 1f);
        SignText(p, new Vector3(DORM_X - 2.6f, 1.02f, 6.37f), 180f, "宿管值班", 0.024f, MatLib.Hex("#f3e9d2"));
        SignText(p, new Vector3(DORM_X - 2.6f, 1.02f, 6.31f), 0f, "宿管值班", 0.024f, MatLib.Hex("#f3e9d2"));

        Sofa(p, new Vector3(DORM_X + 3.25f, 0f, lz), 2.2f, 270f, C("#7a5a6a"));
        Reg(it, DORM_X + 3.25f, lz, 1.0f, 2.2f);
        Table(p, new Vector3(DORM_X + 1.55f, 0f, lz), 1.2f, 0.7f, 0.38f, desk);
        Reg(it, DORM_X + 1.55f, lz, 1.2f, 0.7f);
        Rug(p, new Vector3(DORM_X + 1.2f, 0.058f, lz), 4.6f, 4.2f, C("#8a6a5a"));

        // 公告板（西墙）
        Part(p, new Vector3(DORM_X - 3.86f, 1.65f, 4.4f), new Vector3(0.05f, 1.2f, 2.2f), C("#8a6a4a"), 1f);
        for (int i = 0; i < 6; i++)
        {
            float pz = 3.5f + (i % 3) * 0.6f;
            float py = 1.25f + (i / 3) * 0.55f;
            Part(p, new Vector3(DORM_X - 3.82f, py, pz), new Vector3(0.02f, 0.36f, 0.42f),
                 C(i % 2 == 0 ? "#e8e2d0" : "#c8d8c8"), 1f);
        }

        // 快递/储物架（西墙近门，避开净空）
        for (int i = 0; i < 2; i++)
        {
            float rz = 14.4f + i * 1.8f;
            Part(p, new Vector3(DORM_X - 3.55f, 0.9f, rz), new Vector3(0.55f, 1.8f, 1.6f), C("#6f5540"), 1f);
            for (int j = 0; j < 3; j++)
                Part(p, new Vector3(DORM_X - 3.55f, 0.28f + j * 0.56f, rz), new Vector3(0.50f, 0.04f, 1.5f), C("#8a7a66"), 1f);
            Reg(it, DORM_X - 3.55f, rz, 0.55f, 1.6f);
        }
        Part(p, new Vector3(DORM_X + 3.45f, 0.95f, 16.2f), new Vector3(0.85f, 1.9f, 0.8f), C("#c8ced4"), 1f);
        Part(p, new Vector3(DORM_X + 3.45f, 1.25f, 15.78f), new Vector3(0.62f, 0.75f, 0.03f), C("#3f6a8a"), 1f);
        Reg(it, DORM_X + 3.45f, 16.2f, 0.85f, 0.8f);
        Plant(p, new Vector3(DORM_X - 3.3f, 0.02f, 18.0f), 0.9f);
        Plant(p, new Vector3(DORM_X + 3.3f, 0.02f, 3.2f), 0.9f);
        Part(p, new Vector3(DORM_X, 0.06f, 18.6f), new Vector3(2.6f, 0.02f, 1.4f), C("#5a4a3a"), 1f);
        Rug(p, new Vector3(DORM_X, 0.072f, 16.0f), 5.2f, 3.4f, C("#6a3a3c"));

        // 门厅东墙：长凳 + 挂画 + 挂钟
        Part(p, new Vector3(DORM_X + 3.38f, 0.40f, 12.8f), new Vector3(0.55f, 0.10f, 1.8f), C("#8a6a4a"), 1f);
        for (int i = 0; i < 2; i++)
            Part(p, new Vector3(DORM_X + 3.38f, 0.20f, 12.8f + (i == 0 ? -0.75f : 0.75f)),
                 new Vector3(0.10f, 0.40f, 0.44f), C("#6f5540"), 1f);
        Reg(it, DORM_X + 3.38f, 12.8f, 0.55f, 1.8f);
        Picture(p, new Vector3(DORM_X + 3.86f, 2.10f, 7.6f), 1.5f, 0.95f, 270f);
        WallClock(p, new Vector3(DORM_X - 3.86f, 2.55f, 8.2f), 90f);

        // 走廊：公告板 / 挂钟 / 长凳 / 灭火器 / 端头绿植
        foreach (float sx in new[] { -1f, 1f })
        {
            float bx = DORM_X + sx * 7.5f;
            Part(p, new Vector3(bx, 1.62f, -1.84f), new Vector3(2.0f, 1.0f, 0.04f), C("#6f5540"), 1f);
            Part(p, new Vector3(bx, 1.62f, -1.81f), new Vector3(1.86f, 0.86f, 0.012f), C("#2f4a3a"), 1f);
            SignText(p, new Vector3(bx, 1.62f, -1.79f), 180f, "宿舍公告", 0.030f, MatLib.Hex("#f3e9d2"));
        }
        WallClock(p, new Vector3(DORM_X + 20.9f, 2.55f, -1.84f), 0f);        foreach (float bx in new[] { -10.6f, 10.6f })
        {
            Part(p, new Vector3(DORM_X + bx, 0.42f, 1.52f), new Vector3(2.0f, 0.08f, 0.48f), C("#8a6a4a"), 1f);
            Part(p, new Vector3(DORM_X + bx, 0.22f, 1.52f), new Vector3(0.10f, 0.44f, 0.42f), C("#6f5540"), 1f);
            Reg(it, DORM_X + bx, 1.52f, 2.0f, 0.48f);
        }
        foreach (float sx in new[] { -1f, 1f })
        {
            Part(p, new Vector3(DORM_X + sx * 28.3f, 0.85f, 1.55f), new Vector3(0.35f, 1.1f, 0.5f), C("#a83a32"), 1f);
            Reg(it, DORM_X + sx * 28.3f, 1.55f, 0.35f, 0.5f);
            Plant(p, new Vector3(DORM_X + sx * 27.8f, 0.02f, -1.2f), 1.2f);
        }

        LightGrid(p, DORM_X - 27f, DORM_X + 27f, 0f, 0f, W - 0.35f, 10, 1, 11f, 1.1f);
        Lamp(p, new Vector3(DORM_X, W - 0.35f, 8f), 14f, 1.3f);
        Lamp(p, new Vector3(DORM_X, W - 0.35f, 15f), 14f, 1.3f);
        return it;
    }
}
