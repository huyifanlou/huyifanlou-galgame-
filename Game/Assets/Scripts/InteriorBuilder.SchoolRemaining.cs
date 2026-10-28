using UnityEngine;

// 学校区域剩余建筑室内（单层）：宿舍楼 / 二号宿舍 / 体育馆 / 实验楼 /
// 活动中心 / 艺术楼 / 生活服务楼 / 小卖部 / 值班室。
// 参考：四人间上床下桌 + 公共洗衣/卫浴；体育馆球场+看台+更衣/器材；
// 实验楼中央走廊+分科实验室；活动中心门厅+多功能厅+社团室；
// 艺术楼舞蹈/音乐/美术；生活服务楼洗衣/快递/理发/医务。
public static partial class InteriorBuilder
{
    const float DORM_X = 1540f, DORM2_X = 1600f, GYM_X = 1680f, LABS_X = 1760f;
    const float CENTER_X = 1830f, ARTS_X = 1900f, SERVICE_X = 1970f, KIOSK_X = 2030f, GUARD_X = 2060f;

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
        Part(p, new Vector3(ox, W - 0.03f, 0f), new Vector3(X * 2f, 0.06f, Z * 2f), ceil, 2f);
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

    // 上床下桌：2m 长 × 1m 宽，床头在局部 -z；ry=-90 床头朝 +x（东外墙）
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
        // 台阶/梯子
        Part(g, new Vector3(0.55f, 0.35f, 0.62f), new Vector3(0.10f, 0.70f, 0.10f), frame, 0.6f);
        for (int i = 0; i < 3; i++)
            Part(g, new Vector3(0.55f, 0.55f + i * 0.34f, 0.48f + i * 0.10f),
                 new Vector3(0.34f, 0.05f, 0.10f), frame, 0.6f);
        TableLamp(g, new Vector3(0.18f, 0.77f, -0.55f), "#f0dfb8");

        bool swap = Mathf.Abs(Mathf.Sin(ry * Mathf.Deg2Rad)) > 0.5f;
        Reg(it, x, z, swap ? 2.0f : 1.0f, swap ? 1.0f : 2.0f);
    }

    // 一间宿舍：床沿外侧长墙排开，中间桌椅/衣柜/地毯
    static void DormRoomFurniture(Transform p, Interior it, float x0, float x1, float z0, float z1,
                                  bool eastOuter, int beds, Material frame, Material desk, Material accent)
    {
        float outerX = eastOuter ? x1 : x0;
        float innerX = eastOuter ? x0 : x1;
        float ry = eastOuter ? -90f : 90f;
        float bedX = eastOuter ? x1 - 1.3f : x0 + 1.3f;
        float span = z1 - z0 - 2.4f;
        for (int i = 0; i < beds; i++)
        {
            float t = beds == 1 ? 0.5f : i / (float)(beds - 1);
            float z = z0 + 1.2f + span * t;
            LoftBed(p, it, bedX, z, ry, frame, desk, C("#e8e2d4"));
        }

        float cx = (x0 + x1) * 0.5f, cz = (z0 + z1) * 0.5f;
        float tableX = eastOuter ? x0 + 4.2f : x1 - 4.2f;
        Table(p, new Vector3(tableX, 0f, cz), 2.4f, 1.0f, 0.74f, desk);
        Reg(it, tableX, cz, 2.4f, 1.0f);
        Chair(p, it, new Vector3(tableX, 0f, cz - 0.82f), 0f, frame, true);
        Chair(p, it, new Vector3(tableX, 0f, cz + 0.82f), 180f, frame, true);
        TableLamp(p, new Vector3(tableX + 0.7f, 0.77f, cz + 0.1f));
        Rug(p, new Vector3(cx, 0.06f, cz), 5.4f, 3.8f, accent);

        // 衣柜靠内墙放，避开房门
        float wy = z0 + 1.2f;
        Part(p, new Vector3(innerX + (eastOuter ? 1.0f : -1.0f), 1.0f, wy),
             new Vector3(1.3f, 2.0f, 0.6f), frame, 1f);
        Part(p, new Vector3(innerX + (eastOuter ? 1.62f : -1.62f), 1.0f, wy),
             new Vector3(0.04f, 1.8f, 1.1f), C("#8a7a66"), 1f);
        Reg(it, innerX + (eastOuter ? 1.0f : -1.0f), wy, 1.3f, 0.6f);
        Plant(p, new Vector3(outerX + (eastOuter ? -0.8f : 0.8f), 0.02f, z1 - 0.7f), 0.8f);

        Window(p, new Vector3(outerX + (eastOuter ? -0.18f : 0.18f), 2.25f, cz),
               3.0f, 1.5f, eastOuter ? -90f : 90f);
        Picture(p, new Vector3(innerX + (eastOuter ? 0.16f : -0.16f), 2.2f, z1 - 1.4f),
                1.0f, 0.7f, eastOuter ? 90f : -90f);
    }

    static Interior DormInterior(Transform parent, string id, string name, float ox, int beds, bool warm)
    {
        const float X = 17f, Z = 11f, W = 3.4f;
        var wall = T(warm ? "in_wall2" : "in_wall3");
        var it = Shell(parent, id, name, ox, X, Z, W, 1.4f, 2.6f,
                       wall, T("in_floor"), T("in_ceil"), T(warm ? "in_wall2" : "in_wall3"),
                       C(warm ? "#6f4a32" : "#3a4a62"));
        var p = it.Root.transform;
        var frame = C(warm ? "#8a5a3c" : "#4e5a6e");
        var desk = C(warm ? "#a87a52" : "#7b8798");
        var accent = C(warm ? "#8a9a7a" : "#6f8a9a");
        var wallMat = wall;

        // 中部走廊：x=±1.8，两侧各两间宿舍；南端门厅 + 洗衣/洗漱间
        foreach (float sx in new[] { -1.8f, 1.8f })
        {
            WallZ(p, it, -Z, -2f, ox + sx, 0.02f, W, -6.6f, -5.4f, 2.2f, wallMat, WALL_IN);
            WallZ(p, it, -2f, 7f, ox + sx, 0.02f, W, 2.4f, 3.6f, 2.2f, wallMat, WALL_IN);
            WallX(p, it, ox + sx, ox + (sx < 0 ? -X : X), -2f, 0.02f, W, 0f, 0f, 2.2f, wallMat, WALL_IN);
            WallX(p, it, ox + sx, ox + (sx < 0 ? -X : X), 7f, 0.02f, W, 0f, 0f, 2.2f, wallMat, WALL_IN);
        }
        // 南门厅两角：洗衣房（西）/ 洗漱间（东）
        WallZ(p, it, 7f, Z, ox - 10f, 0.02f, W, 8.5f, 9.7f, 2.2f, wallMat, WALL_IN);
        WallZ(p, it, 7f, Z, ox + 10f, 0.02f, W, 8.5f, 9.7f, 2.2f, wallMat, WALL_IN);

        // 四间宿舍家具
        DormRoomFurniture(p, it, ox - X + 0.1f, ox - 1.9f, -Z + 0.1f, -2.1f, false, beds, frame, desk, accent);
        DormRoomFurniture(p, it, ox + 1.9f, ox + X - 0.1f, -Z + 0.1f, -2.1f, true, beds, frame, desk, accent);
        DormRoomFurniture(p, it, ox - X + 0.1f, ox - 1.9f, -1.9f, 6.9f, false, beds, frame, desk, accent);
        DormRoomFurniture(p, it, ox + 1.9f, ox + X - 0.1f, -1.9f, 6.9f, true, beds, frame, desk, accent);

        // 走廊地毯/公告板/绿植/房间号
        Rug(p, new Vector3(ox, 0.06f, -2.0f), 2.4f, 16.4f, accent);
        Part(p, new Vector3(ox - 1.72f, 1.65f, -2.0f), new Vector3(0.04f, 0.9f, 1.6f), C("#6f5540"), 1f);
        Part(p, new Vector3(ox + 1.72f, 1.65f, -2.0f), new Vector3(0.04f, 0.9f, 1.6f), C("#6f5540"), 1f);
        foreach (var (sx2, doorZ, label) in new[] {
            (-1, -6f, "401"), (1, -6f, "402"), (-1, 3f, "403"), (1, 3f, "404") })
        {
            SignText(p, new Vector3(ox + sx2 * 1.68f, 2.38f, doorZ),
                     sx2 < 0 ? 270f : 90f, label, 0.032f, MatLib.Hex("#f3e9d2"));
        }
        WallClock(p, new Vector3(ox - 6f, 2.55f, 10.82f), 0f);
        Plant(p, new Vector3(ox - 8.5f, 0.02f, 6.0f), 0.9f);
        Plant(p, new Vector3(ox + 8.5f, 0.02f, 6.0f), 0.9f);

        // 洗衣房：三台洗衣机 + 折叠台
        for (int i = 0; i < 3; i++)
        {
            float wx = ox - 15.6f, wz = 8.1f + i * 1.15f;
            Part(p, new Vector3(wx, 0.45f, wz), new Vector3(0.78f, 0.9f, 0.72f), C("#d8dde0"), 1f);
            Part(p, new Vector3(wx + 0.40f, 0.48f, wz), new Vector3(0.04f, 0.50f, 0.50f), C("#3a4a58"), 1f);
            Part(p, new Vector3(wx, 0.94f, wz), new Vector3(0.84f, 0.08f, 0.78f), C("#b9c0c4"), 1f);
            Reg(it, wx, wz, 0.82f, 0.76f);
        }
        Part(p, new Vector3(ox - 12.1f, 0.42f, 9.0f), new Vector3(1.6f, 0.84f, 0.8f), C("#e6e6e0"), 1f);
        Reg(it, ox - 12.1f, 9.0f, 1.6f, 0.8f);
        WallSign(p, ox - 13.6f, 2.45f, 7.0f, 1, "洗衣房", 1.2f, 0.4f, C("#3a4a62"), 0.024f);

        // 洗漱间：两个洗手台 + 两个隔间
        for (int i = 0; i < 2; i++)
        {
            float sx3 = ox + 12.0f + i * 2.0f;
            Part(p, new Vector3(sx3, 0.48f, 10.4f), new Vector3(1.1f, 0.84f, 0.6f), C("#d8dde0"), 1f);
            Part(p, new Vector3(sx3, 0.92f, 10.4f), new Vector3(1.2f, 0.06f, 0.66f), C("#b9c0c4"), 1f);
            Part(p, new Vector3(sx3, 1.35f, 10.80f), new Vector3(0.9f, 0.7f, 0.03f), Glass(), 1f);
            Reg(it, sx3, 10.4f, 1.2f, 0.66f);
        }
        foreach (float px2 in new[] { ox + 11.2f, ox + 14.0f })
        {
            Part(p, new Vector3(px2, 1.0f, 8.2f), new Vector3(1.2f, 2.0f, 0.08f), C("#7b8798"), 1f);
            Part(p, new Vector3(px2, 0.25f, 8.4f), new Vector3(0.8f, 0.5f, 0.55f), C("#e6e6e0"), 1f);
            Reg(it, px2, 8.4f, 0.8f, 0.55f);
        }
        WallSign(p, ox + 13.6f, 2.45f, 7.0f, 1, "洗漱间", 1.2f, 0.4f, C("#3a4a62"), 0.024f);

        LightGrid(p, ox - X + 3f, ox + X - 3f, -Z + 2f, Z - 2f, W - 0.35f, 3, 3, 13f, 1.1f);
        Lamp(p, new Vector3(ox, W - 0.35f, -4f), 11f, 1.15f);
        Lamp(p, new Vector3(ox, W - 0.35f, 4f), 11f, 1.15f);
        return it;
    }

    static Interior Dorm(Transform parent) => DormInterior(parent, "dorm", "宿舍楼", DORM_X, 4, true);
    static Interior Dorm2(Transform parent) => DormInterior(parent, "dorm2", "二号宿舍", DORM2_X, 2, false);

    // 小卖部
    static Interior Kiosk(Transform parent)
    {
        const float X = 8f, Z = 5f, W = 3f;
        var it = Shell(parent, "kiosk", "小卖部", KIOSK_X, X, Z, W, 1.1f, 2.3f,
                       T("in_wall2"), T("in_tile"), T("in_ceil"), T("in_wall2"),
                       C("#7a4a2a"), 0.48f);
        var p = it.Root.transform;
        var frame = C("#6f5540");
        var goods = new[] { C("#b84a3c"), C("#d8a43c"), C("#4a7a5a"), C("#3f5c8a"),
                            C("#8a4a6a"), C("#c86a3c"), C("#a8a08a") };
        ShelfWallZ(p, it, KIOSK_X - 7.55f, -3.8f, 2.6f, 1, frame, goods, 1.85f, 0.9f);
        ShelfWallZ(p, it, KIOSK_X + 7.55f, -3.8f, 1.2f, -1, frame, goods, 1.85f, 0.9f);
        ShelfWallX(p, it, -4.55f, KIOSK_X - 6.0f, KIOSK_X - 1.2f, 1, frame, goods, 1.85f, 0.9f);
        Cooler(p, it, KIOSK_X - 3.2f, -3.9f, 1.8f);
        ProduceTable(p, it, KIOSK_X + 0.8f, -2.6f, 2.2f, 1.0f);
        Checkout(p, it, KIOSK_X + 3.4f, 2.8f, -1);
        WallSign(p, KIOSK_X, 2.55f, -5.0f, 1, "饮料 / 零食", 2.2f, 0.42f, C("#7a4a2a"), 0.028f);
        Plant(p, new Vector3(KIOSK_X - 7.0f, 0.02f, 4.1f), 0.7f);
        Lamp(p, new Vector3(KIOSK_X, W - 0.25f, 0.2f), 9f, 1.2f);
        return it;
    }

    // 值班室
    static Interior Guard(Transform parent)
    {
        const float X = 5f, Z = 4f, W = 3f;
        var it = Shell(parent, "guard", "值班室", GUARD_X, X, Z, W, 1.0f, 2.3f,
                       T("in_wall"), T("in_tile"), T("in_ceil"), T("in_wall"),
                       C("#3a4a62"), 0.46f);
        var p = it.Root.transform;
        var wood = C("#6f5540");
        var dark = C("#2f3338");

        // 值班桌 + 两屏监视器 + 椅子
        Table(p, new Vector3(GUARD_X, 0f, -2.6f), 2.8f, 0.9f, 0.78f, wood);
        Reg(it, GUARD_X, -2.6f, 2.8f, 0.9f);
        for (int i = 0; i < 2; i++)
        {
            Part(p, new Vector3(GUARD_X - 0.65f + i * 1.3f, 1.10f, -2.75f),
                 new Vector3(0.95f, 0.55f, 0.06f), dark, 1f);
            Part(p, new Vector3(GUARD_X - 0.65f + i * 1.3f, 1.10f, -2.70f),
                 new Vector3(0.82f, 0.42f, 0.02f), C("#3f6a8a"), 1f);
            Part(p, new Vector3(GUARD_X - 0.65f + i * 1.3f, 0.80f, -2.78f),
                 new Vector3(0.10f, 0.20f, 0.10f), dark, 1f);
        }
        Chair(p, it, new Vector3(GUARD_X, 0f, -2.4f), 180f, wood, true);
        Part(p, new Vector3(GUARD_X + 0.8f, 0.84f, -2.45f), new Vector3(0.8f, 0.06f, 0.3f), C("#e6e6e0"), 1f);

        // 休息长椅 + 文件柜 + 饮水机
        Sofa(p, new Vector3(GUARD_X + 3.1f, 0f, 0.2f), 1.8f, 270f, C("#4e5a6e"));
        Reg(it, GUARD_X + 3.1f, 0.2f, 1.0f, 1.8f);
        Part(p, new Vector3(GUARD_X - 3.4f, 0.9f, -0.4f), new Vector3(1.4f, 1.8f, 0.6f), C("#6f5540"), 1f);
        Reg(it, GUARD_X - 3.4f, -0.4f, 1.4f, 0.6f);
        Part(p, new Vector3(GUARD_X + 2.0f, 0.52f, 3.45f), new Vector3(0.5f, 1.0f, 0.5f), C("#d8dde0"), 1f);
        Part(p, new Vector3(GUARD_X + 2.0f, 1.18f, 3.45f), new Vector3(0.42f, 0.32f, 0.42f), C("#8ab8d8"), 1f);
        Reg(it, GUARD_X + 2.0f, 3.45f, 0.5f, 0.5f);

        WallClock(p, new Vector3(GUARD_X - 4.82f, 2.35f, 0.0f), 90f);
        WallSign(p, GUARD_X, 2.35f, -4.0f, 1, "值班制度", 1.6f, 0.5f, C("#3a4a62"), 0.026f);
        Plant(p, new Vector3(GUARD_X - 4.2f, 0.02f, 3.2f), 0.7f);
        LightGrid(p, GUARD_X - 2.2f, GUARD_X + 2.2f, -2.2f, 2.2f, W - 0.3f, 2, 1, 8f, 1.1f);
        return it;
    }
}
