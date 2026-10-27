using UnityEngine;

// 小镇公共室内（单层，扩大版）：图书馆 / 餐厅 / 超市 / 食堂
// 三处旧室内面积约翻倍（每边约 ×1.4），重新分区；食堂参考现代高校食堂的取餐线 + 多座位组合。
public static partial class InteriorBuilder
{
    const float CANTEEN_X = 1460f;

    static void Column(Transform p, Interior it, float x, float z, float h, Material mat)
    {
        Part(p, new Vector3(x, h * 0.5f, z), new Vector3(0.48f, h, 0.48f), mat, 1f);
        Reg(it, x, z, 0.48f, 0.48f);
    }

    // 四人桌：桌面 + 四椅，餐厅/食堂通用
    static void Table4(Transform p, Interior it, float x, float z, float w, float d, Material wood)
    {
        Table(p, new Vector3(x, 0f, z), w, d, 0.76f, wood);
        Reg(it, x, z, w, d);
        Chair(p, it, new Vector3(x - 0.35f, 0f, z - d * 0.5f - 0.34f), 0f, wood, true);
        Chair(p, it, new Vector3(x + 0.35f, 0f, z - d * 0.5f - 0.34f), 0f, wood, true);
        Chair(p, it, new Vector3(x - 0.35f, 0f, z + d * 0.5f + 0.34f), 180f, wood, true);
        Chair(p, it, new Vector3(x + 0.35f, 0f, z + d * 0.5f + 0.34f), 180f, wood, true);
    }

    static void BenchX(Transform p, Interior it, float x, float z, float len)
    {
        var wood = T("wood");
        Part(p, new Vector3(x, 0.44f, z), new Vector3(len, 0.06f, 0.38f), wood, 1f);
        Part(p, new Vector3(x, 0.22f, z), new Vector3(len - 0.24f, 0.44f, 0.10f), C("#6f5540"), 1f);
        Reg(it, x, z, len, 0.38f);
    }

    // 食堂取餐档口：台面 + 玻璃防溅板 + 餐盘 + 上方菜单牌
    static void ServingStation(Transform p, Interior it, float x, float z, float w,
                               string label, Material body, Material top)
    {
        Part(p, new Vector3(x, 0.55f, z), new Vector3(w, 1.10f, 1.0f), body, 1f);
        Part(p, new Vector3(x, 1.12f, z), new Vector3(w + 0.12f, 0.06f, 1.06f), top, 1f);
        // 取餐口只保留金属防溅框，不用不透明玻璃挡住菜品
        Part(p, new Vector3(x, 1.56f, z + 0.47f),
             new Vector3(w, 0.04f, 0.04f), C("#9aa0a6"), 1f);
        foreach (float px in new[] { x - w * 0.5f + 0.05f, x, x + w * 0.5f - 0.05f })
            Part(p, new Vector3(px, 1.30f, z + 0.47f),
                 new Vector3(0.04f, 0.52f, 0.04f), C("#9aa0a6"), 1f);
        for (int i = 0; i < 4; i++)
            Part(p, new Vector3(x - w * 0.36f + i * w * 0.24f, 1.18f, z - 0.10f),
                 new Vector3(w * 0.18f, 0.10f, 0.46f),
                 i % 2 == 0 ? C("#b85a3c") : C("#6b7a4a"), 0.4f);
        Part(p, new Vector3(x, 1.02f, z + 0.57f), new Vector3(w, 0.04f, 0.08f), C("#9aa0a6"), 1f);
        HangingSign(p, x, 2.80f, z - 0.18f, 1, label, w * 0.72f, 0.45f, C("#2f4a3a"), 0.030f);
        Reg(it, x, z, w, 1.0f);
    }

    // ═════════════════════════════════════════════════════
    static Interior Library(Transform parent)
    {
        float ox = LIB_X;
        const float X = 16f, Z = 10f, W = 4.8f;
        var it = new Interior
        {
            Id = "library", Name = "图书馆",
            X1 = ox - X, X2 = ox + X, Z1 = -Z, Z2 = Z,
            Spawn = new Vector2(ox, 8.4f), Exit = new Vector2(ox, 9.4f),
            FloorY = new[] { 0.02f },
            ClearX1 = ox - 1.6f, ClearX2 = ox + 1.6f, ClearZ1 = 5.0f, ClearZ2 = 10f,
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

        Part(p, new Vector3(ox, 0f, 0f), new Vector3(32f, 0.04f, 20f), T("in_floor"), 2f, default, true);
        Part(p, new Vector3(ox, W - 0.03f, 0f), new Vector3(32f, 0.06f, 20f), T("in_ceil"), 2f);
        WallX(p, it, ox - X, ox + X, -Z, 0f, W, 0f, 0f, 2.7f, wall);
        WallX(p, it, ox - X, ox + X, Z, 0f, W, ox - 1.3f, ox + 1.3f, 2.7f, wall);
        WallZ(p, it, -Z, Z, ox - X, 0f, W, 0f, 0f, 2.7f, wall);
        WallZ(p, it, -Z, Z, ox + X, 0f, W, 0f, 0f, 2.7f, wall);
        DoorFrame(p, ox, Z, 1.3f, 2.7f, -1);
        WallSign(p, ox, 3.55f, Z, -1, "图书馆", 2.8f, 0.66f, C("#2f4a3a"), 0.040f);
        PaperRoom(p, ox - X, ox + X, -Z, Z, W, wall, 1, ox - 1.3f, ox + 1.3f);

        // 木格栅吊顶 + 线性灯带
        for (float z = -9f; z <= 9f; z += 1.5f)
            Part(p, new Vector3(ox, W - 0.16f, z), new Vector3(31.4f, 0.08f, 0.16f), woodDark, 1f);
        foreach (float lx in new[] { -11f, -4f, 4f, 11f })
            Part(p, new Vector3(ox + lx, W - 0.24f, 0f), new Vector3(0.10f, 0.04f, 16.0f), C("#fff2d6"), 1f);

        Window(p, new Vector3(ox - 9f, 2.8f, -9.80f), 3.2f, 2.2f, 0f);
        Window(p, new Vector3(ox + 9f, 2.8f, -9.80f), 3.2f, 2.2f, 0f);
        Window(p, new Vector3(ox - 9f, 2.6f, 9.80f), 2.8f, 1.9f, 180f);
        Window(p, new Vector3(ox + 9f, 2.6f, 9.80f), 2.8f, 1.9f, 180f);
        Window(p, new Vector3(ox + 15.80f, 2.9f, -2f), 2.4f, 1.5f, -90f);
        Window(p, new Vector3(ox - 15.80f, 2.9f, 6.4f), 2.2f, 1.4f, 90f);

        // 书墙：西 11m、东 5.4m、北墙两翼 + 中央主书墙
        ShelfWallZ(p, it, ox - 15.58f, -8.4f, 2.6f, 1, panel, books, 2.25f);
        ShelfWallZ(p, it, ox + 15.58f, -8.4f, -3.0f, -1, panel, books, 2.25f);
        ShelfWallX(p, it, -9.60f, ox - 15f, ox - 11f, 1, panel, books, 2.25f);
        ShelfWallX(p, it, -9.60f, ox + 11f, ox + 15f, 1, panel, books, 2.25f);
        ShelfWallX(p, it, -9.60f, ox - 4f, ox + 4f, 1, panel, books, 2.25f);
        ShelfWallZ(p, it, ox + 15.58f, 3.2f, 8.2f, -1, panel, books, 2.10f);

        // 中央阅读大厅：四张长桌 + 24 椅；入口正中补一张新书展示台，避免进门只看到空毯
        Rug(p, new Vector3(ox, 0.06f, 0.1f), 14.5f, 10.0f, sage);
        Table(p, new Vector3(ox - 4.5f, 0f, 5.4f), 3.2f, 1.0f, 0.90f, wood);
        Reg(it, ox - 4.5f, 5.4f, 3.2f, 1.0f);
        BookStack(p, new Vector3(ox - 5.3f, 0.93f, 5.4f), 3);
        BookStack(p, new Vector3(ox - 4.5f, 0.93f, 5.45f), 4);
        BookStack(p, new Vector3(ox - 3.7f, 0.93f, 5.35f), 2);
        foreach (float tx in new[] { -6f, 6f })
            foreach (float tz in new[] { -2.2f, 2.4f })
            {
                Table(p, new Vector3(ox + tx, 0f, tz), 3.8f, 1.1f, 0.76f, wood);
                Reg(it, ox + tx, tz, 3.8f, 1.1f);
                foreach (float dx in new[] { -1.25f, 0f, 1.25f })
                {
                    Chair(p, it, new Vector3(ox + tx + dx, 0f, tz - 0.82f), 0f, wood, true);
                    Chair(p, it, new Vector3(ox + tx + dx, 0f, tz + 0.82f), 180f, wood, true);
                }
                BookStack(p, new Vector3(ox + tx - 1.3f, 0.79f, tz + 0.14f), 3);
                BookStack(p, new Vector3(ox + tx + 0.5f, 0.79f, tz - 0.12f), 2);
                TableLamp(p, new Vector3(ox + tx + 1.3f, 0.78f, tz + 0.20f));
                Pendant(p, ox + tx, 3.35f, tz, 1.1f, "#e0d0b0");
            }

        // 中岛展示台
        foreach (float tx in new[] { -9.8f, 9.0f })
        {
            Table(p, new Vector3(ox + tx, 0f, -3.6f), 2.6f, 0.9f, 1.00f, wood);
            Reg(it, ox + tx, -3.6f, 2.6f, 0.9f);
            BookStack(p, new Vector3(ox + tx - 0.5f, 1.03f, -3.6f), 3);
            BookStack(p, new Vector3(ox + tx + 0.5f, 1.03f, -3.55f), 4);
        }

        // 借阅台 / 新书台 / 还书箱
        Part(p, new Vector3(ox - 9.2f, 0.52f, 7.35f), new Vector3(5.2f, 1.04f, 1.1f), woodDark, 1f);
        Part(p, new Vector3(ox - 9.2f, 1.08f, 7.35f), new Vector3(5.3f, 0.07f, 1.16f), cream, 1f);
        Part(p, new Vector3(ox - 13.1f, 0.52f, 5.95f), new Vector3(1.1f, 1.04f, 2.6f), woodDark, 1f);
        Part(p, new Vector3(ox - 13.1f, 1.08f, 5.95f), new Vector3(1.16f, 0.07f, 2.66f), cream, 1f);
        Reg(it, ox - 9.2f, 7.35f, 5.2f, 1.1f);
        Reg(it, ox - 13.1f, 5.95f, 1.1f, 2.6f);
        Part(p, new Vector3(ox - 9.2f, 1.18f, 6.79f), new Vector3(2.2f, 0.34f, 0.05f), C("#2f4a3a"), 1f);
        SignText(p, new Vector3(ox - 9.2f, 1.18f, 6.76f), 0f, "借阅台", 0.030f, MatLib.Hex("#1f3428"));
        Part(p, new Vector3(ox - 5.5f, 0.42f, 7.5f), new Vector3(0.8f, 0.84f, 0.8f), C("#8a6a4a"), 1f);
        Part(p, new Vector3(ox - 5.5f, 0.86f, 7.5f), new Vector3(0.7f, 0.06f, 0.7f), C("#3f5c6b"), 1f);
        Reg(it, ox - 5.5f, 7.5f, 0.8f, 0.8f);

        Table(p, new Vector3(ox + 4.2f, 0f, 8.0f), 3.2f, 1.0f, 0.72f, wood);
        Reg(it, ox + 4.2f, 8.0f, 3.2f, 1.0f);
        BookStack(p, new Vector3(ox + 3.2f, 0.75f, 8.0f), 3);
        BookStack(p, new Vector3(ox + 4.2f, 0.75f, 8.04f), 4);
        BookStack(p, new Vector3(ox + 5.1f, 0.75f, 7.96f), 2);
        HangingSign(p, ox + 4.2f, 2.60f, 7.55f, -1, "新书", 1.3f, 0.40f, C("#4a6a52"), 0.026f);

        // 东侧休闲角
        Rug(p, new Vector3(ox + 11.8f, 0.06f, 1.5f), 5.2f, 4.2f, C("#8f8571"));
        Sofa(p, new Vector3(ox + 9.7f, 0f, 1.5f), 1.6f, 90f, C("#8f8571"));
        Sofa(p, new Vector3(ox + 13.5f, 0f, 1.5f), 1.6f, 270f, C("#8f8571"));
        Reg(it, ox + 9.7f, 1.5f, 1.0f, 1.6f);
        Reg(it, ox + 13.5f, 1.5f, 1.0f, 1.6f);
        Part(p, new Vector3(ox + 11.6f, 0.36f, 1.5f), new Vector3(1.05f, 0.08f, 0.68f), wood, 1f);
        Part(p, new Vector3(ox + 11.6f, 0.18f, 1.5f), new Vector3(0.82f, 0.36f, 0.50f), woodDark, 1f);
        Reg(it, ox + 11.6f, 1.5f, 1.05f, 0.68f);
        BookStack(p, new Vector3(ox + 11.6f, 0.43f, 1.5f), 2);
        TableLamp(p, new Vector3(ox + 12.9f, 0.02f, 3.0f));

        // 儿童阅读角（东南）
        Rug(p, new Vector3(ox + 10.8f, 0.06f, 6.6f), 5.4f, 3.8f, C("#a8b8a0"));
        Table(p, new Vector3(ox + 10.0f, 0f, 6.5f), 1.8f, 0.9f, 0.58f, wood);
        Reg(it, ox + 10.0f, 6.5f, 1.8f, 0.9f);
        BookStack(p, new Vector3(ox + 9.6f, 0.61f, 6.5f), 2);
        Stool(p, it, ox + 8.9f, 6.1f, C("#c8a05a"));
        Stool(p, it, ox + 11.1f, 6.1f, C("#7a9aa6"));
        Stool(p, it, ox + 9.2f, 7.1f, C("#a86a6a"));
        Stool(p, it, ox + 10.9f, 7.2f, C("#6b7a4a"));

        // 西侧安静阅读角
        Sofa(p, new Vector3(ox - 13.0f, 0f, 3.6f), 1.6f, 90f, C("#7d8a76"));
        Reg(it, ox - 13.0f, 3.6f, 1.0f, 1.6f);
        Part(p, new Vector3(ox - 11.8f, 0.36f, 3.6f), new Vector3(0.9f, 0.08f, 0.62f), wood, 1f);
        Part(p, new Vector3(ox - 11.8f, 0.18f, 3.6f), new Vector3(0.7f, 0.36f, 0.46f), woodDark, 1f);
        Reg(it, ox - 11.8f, 3.6f, 0.9f, 0.62f);
        TableLamp(p, new Vector3(ox - 14.8f, 0.02f, 2.9f));

        // 墙饰、绿植、挂钟
        Picture(p, new Vector3(ox + 15.83f, 2.2f, 0.5f), 1.4f, 1.0f, 270f);
        Picture(p, new Vector3(ox - 15.83f, 2.2f, 4.4f), 1.3f, 0.95f, 90f);
        Picture(p, new Vector3(ox + 0.0f, 3.85f, -9.83f), 1.8f, 0.8f, 0f);
        WallClock(p, new Vector3(ox - 4.5f, 3.15f, 9.82f), 0f);
        Plant(p, new Vector3(ox - 15.0f, 0.02f, -9.2f), 1.2f);
        Plant(p, new Vector3(ox + 15.0f, 0.02f, -9.2f), 1.0f);
        Plant(p, new Vector3(ox + 15.0f, 0.02f, 8.9f), 1.0f);
        Plant(p, new Vector3(ox - 15.0f, 0.02f, 9.0f), 0.9f);
        Lamp(p, new Vector3(ox - 10.5f, 4.45f, -5.0f), 12f, 1.15f);
        Lamp(p, new Vector3(ox + 10.5f, 4.45f, -5.0f), 12f, 1.15f);
        Lamp(p, new Vector3(ox - 10.5f, 4.45f, 5.0f), 12f, 1.15f);
        Lamp(p, new Vector3(ox + 10.5f, 4.45f, 5.0f), 12f, 1.15f);
        Lamp(p, new Vector3(ox, 4.45f, 0.0f), 13f, 1.15f);
        return it;
    }

    // ═════════════════════════════════════════════════════
    static Interior Restaurant(Transform parent)
    {
        float ox = REST_X;
        const float X = 15f, Z = 10f, W = 4.5f;
        var it = new Interior
        {
            Id = "restaurant", Name = "餐厅",
            X1 = ox - X, X2 = ox + X, Z1 = -Z, Z2 = Z,
            Spawn = new Vector2(ox, -8.2f), Exit = new Vector2(ox, -9.2f),
            FloorY = new[] { 0.02f },
            ClearX1 = ox - 1.6f, ClearX2 = ox + 1.6f, ClearZ1 = -10f, ClearZ2 = -5f,
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

        Part(p, new Vector3(ox, 0f, 0f), new Vector3(30f, 0.04f, 20f), T("in_floor"), 2f, default, true);
        Part(p, new Vector3(ox, 0.05f, 8.4f), new Vector3(30f, 0.02f, 3.2f), T("in_tile"), 1.4f);
        Part(p, new Vector3(ox, W - 0.03f, 0f), new Vector3(30f, 0.06f, 20f), T("in_ceil"), 2f);
        WallX(p, it, ox - X, ox + X, -Z, 0f, W, ox - 1.2f, ox + 1.2f, 2.6f, wall);
        WallX(p, it, ox - X, ox + X, Z, 0f, W, 0f, 0f, 2.6f, wall);
        WallZ(p, it, -Z, Z, ox - X, 0f, W, 0f, 0f, 2.6f, wall);
        WallZ(p, it, -Z, Z, ox + X, 0f, W, 0f, 0f, 2.6f, wall);
        DoorFrame(p, ox, -Z, 1.2f, 2.6f, 1);
        WallSign(p, ox, 3.35f, -Z, 1, "餐厅", 2.3f, 0.62f, C("#5a3a30"), 0.038f);
        PaperRoom(p, ox - X, ox + X, -Z, Z, W, wall, 0, ox - 1.2f, ox + 1.2f);

        for (float z = -8f; z <= 8f; z += 2.4f)
            Part(p, new Vector3(ox, W - 0.14f, z), new Vector3(29.4f, 0.10f, 0.14f), C("#4a3a30"), 1f);
        Window(p, new Vector3(ox - 7f, 2.2f, -9.80f), 3.2f, 1.9f, 0f);
        Window(p, new Vector3(ox + 7f, 2.2f, -9.80f), 3.2f, 1.9f, 0f);
        Window(p, new Vector3(ox + 14.80f, 2.8f, -2f), 2.4f, 1.4f, -90f);
        Window(p, new Vector3(ox + 14.80f, 2.8f, 3f), 2.4f, 1.4f, -90f);
        Window(p, new Vector3(ox - 14.80f, 2.8f, -4f), 2.2f, 1.3f, 90f);
        Window(p, new Vector3(ox - 14.80f, 2.8f, 4f), 2.2f, 1.3f, 90f);

        // 入口迎宾 / 等位 / 衣帽
        Part(p, new Vector3(ox + 4.4f, 0.55f, -8.35f), new Vector3(1.5f, 1.10f, 0.65f), woodDark, 1f);
        Part(p, new Vector3(ox + 4.4f, 1.12f, -8.35f), new Vector3(1.6f, 0.06f, 0.70f), brass, 1f);
        Part(p, new Vector3(ox + 4.4f, 1.22f, -8.20f), new Vector3(0.45f, 0.30f, 0.04f), cream, 1f);
        Reg(it, ox + 4.4f, -8.35f, 1.5f, 0.65f);
        Sofa(p, new Vector3(ox - 5.2f, 0f, -8.5f), 2.4f, 0f, green);
        Reg(it, ox - 5.2f, -8.5f, 2.4f, 0.95f);
        Part(p, new Vector3(ox + 7.4f, 1.05f, -9.4f), new Vector3(0.06f, 2.0f, 0.06f), C("#6a7078"), 1f);
        Part(p, new Vector3(ox + 7.4f, 1.95f, -9.4f), new Vector3(0.8f, 0.06f, 0.06f), C("#6a7078"), 1f);
        foreach (float hx in new[] { -0.32f, 0.32f })
            Part(p, new Vector3(ox + 7.4f + hx, 1.78f, -9.4f), new Vector3(0.04f, 0.25f, 0.04f), brass, 1f);

        // 西墙卡座 + 4 张双人桌
        Sofa(p, new Vector3(ox - 14.2f, 0f, 0f), 8.4f, 90f, fabric);
        Reg(it, ox - 14.2f, 0f, 0.95f, 8.4f);
        foreach (float tz in new[] { -3.2f, -0.8f, 1.6f, 4.0f })
        {
            Table(p, new Vector3(ox - 12.8f, 0f, tz), 1.2f, 0.9f, 0.76f, wood);
            Reg(it, ox - 12.8f, tz, 1.2f, 0.9f);
            Chair(p, it, new Vector3(ox - 11.4f, 0f, tz), 270f, wood, true);
            TableLamp(p, new Vector3(ox - 12.8f, 0.78f, tz + 0.22f));
            Pendant(p, ox - 12.8f, 2.85f, tz, 1.1f, "#d8c8a8");
        }

        // 中部两排四人桌 + 中央长桌
        foreach (float tx in new[] { -7.8f, 2.6f })
            foreach (float tz in new[] { -3.5f, 0f, 3.5f })
            {
                Table4(p, it, ox + tx, tz, 1.5f, 1.0f, wood);
                TableLamp(p, new Vector3(ox + tx, 0.78f, tz + 0.18f));
                Pendant(p, ox + tx, 2.75f, tz, 1.25f, "#d8c8a8");
            }
        Rug(p, new Vector3(ox + 2.6f, 0.06f, 0f), 7.4f, 10.0f, C("#5a4a3e"));
        Rug(p, new Vector3(ox - 7.8f, 0.06f, 0f), 6.4f, 10.0f, C("#6b5a48"));
        Table(p, new Vector3(ox - 2.2f, 0f, 3.3f), 5.0f, 1.2f, 0.76f, wood);
        Reg(it, ox - 2.2f, 3.3f, 5.0f, 1.2f);
        foreach (float dx in new[] { -2f, -1f, 0f, 1f, 2f })
        {
            Chair(p, it, new Vector3(ox - 2.2f + dx, 0f, 2.45f), 0f, wood, true);
            Chair(p, it, new Vector3(ox - 2.2f + dx, 0f, 4.15f), 180f, wood, true);
        }
        TableLamp(p, new Vector3(ox - 2.2f, 0.78f, 3.5f));
        Pendant(p, ox - 2.2f, 2.75f, 3.3f, 1.2f, "#d8c8a8");

        // 东侧包厢：木格栅隔断 + 两张长桌
        for (float sz = -7.2f; sz <= -0.5f; sz += 0.32f)
            Part(p, new Vector3(ox + 8.0f, 1.05f, sz), new Vector3(0.06f, 2.10f, 0.06f), woodDark, 1f);
        Part(p, new Vector3(ox + 8.0f, 2.12f, -3.85f), new Vector3(0.10f, 0.06f, 6.7f), woodDark, 1f);
        Reg(it, ox + 8.0f, -3.85f, 0.14f, 6.7f);
        foreach (float tz in new[] { -5.4f, -1.6f })
        {
            Table(p, new Vector3(ox + 11.2f, 0f, tz), 2.6f, 1.1f, 0.76f, wood);
            Reg(it, ox + 11.2f, tz, 2.6f, 1.1f);
            foreach (float dx in new[] { -0.85f, 0f, 0.85f })
            {
                Chair(p, it, new Vector3(ox + 11.2f + dx, 0f, tz - 0.88f), 0f, wood, true);
                Chair(p, it, new Vector3(ox + 11.2f + dx, 0f, tz + 0.88f), 180f, wood, true);
            }
            Pendant(p, ox + 11.2f, 2.80f, tz, 1.2f, "#d8c8a8");
        }

        // 南侧开放厨房 + 长吧台
        Part(p, new Vector3(ox, 0.49f, 8.3f), new Vector3(27.0f, 0.98f, 1.0f), woodDark, 1f);
        Part(p, new Vector3(ox, 1.01f, 8.3f), new Vector3(27.2f, 0.07f, 1.08f), cream, 1f);
        Reg(it, ox, 8.3f, 27.0f, 1.0f);
        for (float sx = -13.2f; sx <= 13.2f; sx += 0.55f)
            Part(p, new Vector3(ox + sx, 0.45f, 7.78f), new Vector3(0.07f, 0.82f, 0.06f), C("#7a6248"), 1f);
        Part(p, new Vector3(ox + 2.2f, 1.06f, 8.72f), new Vector3(3.0f, 0.08f, 0.65f), C("#2f3338"), 1f);
        foreach (float bx in new[] { 1.2f, 2.2f, 3.2f })
            Part(p, new Vector3(ox + bx, 1.12f, 8.72f), new Vector3(0.42f, 0.06f, 0.42f), C("#8d8d94"), 1f);
        Part(p, new Vector3(ox + 2.2f, 2.55f, 8.85f), new Vector3(3.6f, 0.55f, 0.75f), C("#3a3f47"), 1f);
        Part(p, new Vector3(ox - 10.0f, 1.15f, 8.75f), new Vector3(1.7f, 2.3f, 0.8f), C("#c9ced3"), 1f);
        Part(p, new Vector3(ox - 10.0f, 1.75f, 9.15f), new Vector3(8.0f, 0.07f, 0.4f), wood, 1f);
        foreach (float px in new[] { -6.5f, -5.4f, -4.3f })
            Part(p, new Vector3(ox + px, 1.80f, 9.15f), new Vector3(0.34f, 0.06f, 0.26f), cream, 1f);
        // 酒架（东墙）
        Part(p, new Vector3(ox + 14.55f, 1.2f, -5.5f), new Vector3(0.5f, 2.4f, 3.6f), woodDark, 1f);
        for (int r = 0; r < 4; r++)
        {
            Part(p, new Vector3(ox + 14.30f, 0.45f + r * 0.50f, -5.5f),
                 new Vector3(0.06f, 0.04f, 3.4f), wood, 1f);
            for (int i = 0; i < 8; i++)
                Part(p, new Vector3(ox + 14.24f, 0.62f + r * 0.50f, -7.05f + i * 0.40f),
                     new Vector3(0.10f, 0.30f, 0.12f), i % 3 == 0 ? C("#5b4a6b") : C("#3f5c6b"), 0.3f);
        }
        Reg(it, ox + 14.55f, -5.5f, 0.5f, 3.6f);
        foreach (float sx in new[] { 5.2f, 7.0f, 8.8f, 10.6f, 12.4f })
            Stool(p, it, ox + sx, 7.05f, C("#8a6a4a"));

        // 菜单板 / 墙饰 / 绿植 / 挂钟
        Part(p, new Vector3(ox - 11.0f, 2.15f, -9.84f), new Vector3(1.6f, 1.2f, 0.05f), C("#2f3338"), 1f);
        Part(p, new Vector3(ox - 11.0f, 2.15f, -9.81f), new Vector3(1.4f, 1.0f, 0.012f), C("#3f5c4a"), 1f);
        SignText(p, new Vector3(ox - 11.0f, 2.40f, -9.79f), 180f, "今日推荐", 0.028f, MatLib.Hex("#dfe6dc"));
        foreach (float ly in new[] { 2.10f, 1.95f, 1.80f })
            Part(p, new Vector3(ox - 11.0f, ly, -9.79f), new Vector3(0.95f, 0.025f, 0.01f), cream, 0.2f);
        Picture(p, new Vector3(ox + 14.83f, 2.2f, 5.8f), 1.3f, 0.9f, 270f);
        Picture(p, new Vector3(ox - 14.83f, 2.3f, 6.2f), 1.2f, 0.9f, 90f);
        WallClock(p, new Vector3(ox - 13.5f, 2.9f, -9.82f), 180f);
        Plant(p, new Vector3(ox - 14.2f, 0.02f, -9.1f), 1.15f);
        Plant(p, new Vector3(ox + 14.1f, 0.02f, 7.8f), 1.0f);
        Plant(p, new Vector3(ox + 9.0f, 0.02f, -9.1f), 0.9f);
        Lamp(p, new Vector3(ox - 7.0f, 4.15f, 2.5f), 11f, 1.05f);
        Lamp(p, new Vector3(ox + 5.0f, 4.15f, -3.0f), 11f, 1.05f);
        Lamp(p, new Vector3(ox + 11.0f, 4.15f, -3.0f), 11f, 1.05f);
        Lamp(p, new Vector3(ox - 4.0f, 4.15f, -6.0f), 11f, 1.05f);
        return it;
    }

    // ═════════════════════════════════════════════════════
    static Interior Supermarket(Transform parent)
    {
        float ox = MART_X;
        const float X = 13f, Z = 8.5f, W = 4.0f;
        var it = new Interior
        {
            Id = "supermarket", Name = "超市",
            X1 = ox - X, X2 = ox + X, Z1 = -Z, Z2 = Z,
            Spawn = new Vector2(ox, -6.8f), Exit = new Vector2(ox, -7.8f),
            FloorY = new[] { 0.02f },
            ClearX1 = ox - 1.6f, ClearX2 = ox + 1.6f, ClearZ1 = -8.5f, ClearZ2 = -2.5f,
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
        var wood = T("wood");
        var woodDark = C("#6f5540");
        var cream = C("#dfe6dc");
        var products = new[] { C("#8c4a3c"), C("#3f5c6b"), C("#6b7a4a"),
                               C("#d0a848"), C("#8a5a6a"), C("#c8c4ba"),
                               C("#b06a4e"), C("#7a9aa6") };

        Part(p, new Vector3(ox, 0f, 0f), new Vector3(26f, 0.04f, 17f), T("in_tile"), 2f, default, true);
        Part(p, new Vector3(ox, 0.05f, 0f), new Vector3(2.4f, 0.02f, 14.0f), C("#d6d0c4"), 1f);
        Part(p, new Vector3(ox, W - 0.03f, 0f), new Vector3(26f, 0.06f, 17f), T("in_ceil"), 2f);
        WallX(p, it, ox - X, ox + X, -Z, 0f, W, ox - 1.3f, ox + 1.3f, 2.6f, wall);
        WallX(p, it, ox - X, ox + X, Z, 0f, W, 0f, 0f, 2.6f, wall);
        WallZ(p, it, -Z, Z, ox - X, 0f, W, 0f, 0f, 2.6f, wall);
        WallZ(p, it, -Z, Z, ox + X, 0f, W, 0f, 0f, 2.6f, wall);
        DoorFrame(p, ox, -Z, 1.3f, 2.6f, 1);
        WallSign(p, ox, 3.30f, -Z, 1, "超市", 2.3f, 0.62f, C("#2f4a3a"), 0.040f);
        PaperRoom(p, ox - X, ox + X, -Z, Z, W, wall, 0, ox - 1.3f, ox + 1.3f);

        for (float lx = -9f; lx <= 9f; lx += 6f)
            foreach (float lz in new[] { -3.8f, 3.0f })
                Part(p, new Vector3(ox + lx, W - 0.20f, lz), new Vector3(0.16f, 0.05f, 7.0f), C("#fff2d6"), 1f);
        Window(p, new Vector3(ox - 8f, 1.95f, -8.30f), 2.6f, 1.7f, 0f);
        Window(p, new Vector3(ox + 8f, 1.95f, -8.30f), 2.6f, 1.7f, 0f);

        // 收银 + 购物篮/车
        Checkout(p, it, ox - 8.7f, -5.6f, 1);
        Checkout(p, it, ox + 8.7f, -5.6f, 1);
        HangingSign(p, ox - 8.7f, 2.95f, -4.7f, -1, "收银", 1.3f, 0.40f, C("#2f4a3a"), 0.027f);
        HangingSign(p, ox + 8.7f, 2.95f, -4.7f, -1, "收银", 1.3f, 0.40f, C("#2f4a3a"), 0.027f);
        foreach (float bx in new[] { -3.3f, 3.3f })
            for (int i = 0; i < 3; i++)
                Part(p, new Vector3(ox + bx, 0.16f + i * 0.14f, -6.6f),
                     new Vector3(0.44f, 0.12f, 0.32f), i % 2 == 0 ? C("#c85a42") : C("#d0a848"), 0.5f);
        foreach (float cx in new[] { 4.2f, 4.8f })
        {
            Part(p, new Vector3(ox + cx, 0.55f, -6.7f), new Vector3(0.42f, 0.55f, 0.62f), metal, 1f);
            Part(p, new Vector3(ox + cx, 0.55f, -6.7f), new Vector3(0.34f, 0.42f, 0.52f), C("#d8d8d2"), 1f);
            Part(p, new Vector3(ox + cx, 0.20f, -6.7f), new Vector3(0.46f, 0.06f, 0.66f), dark, 1f);
        }

        // 生鲜：入口左侧岛台（不压出口中线）+ 西侧台
        ProduceTable(p, it, ox - 4.8f, -2.8f, 6.0f, 1.5f);
        ProduceTable(p, it, ox - 10.0f, -0.3f, 2.6f, 1.2f);
        HangingSign(p, ox - 4.8f, 2.95f, -1.7f, -1, "生鲜", 1.4f, 0.40f, C("#4a6a52"), 0.027f);

        // 西墙：熟食柜 + 烘焙柜 + 后架
        Part(p, new Vector3(ox - 11.4f, 0.55f, -2.8f), new Vector3(1.8f, 1.10f, 3.0f), wood, 1f);
        Part(p, new Vector3(ox - 11.4f, 1.14f, -2.8f), new Vector3(1.9f, 0.06f, 3.1f), C("#d9d3c6"), 1f);
        foreach (float bz in new[] { -3.7f, -2.8f, -1.9f })
            Part(p, new Vector3(ox - 11.38f, 1.28f, bz), new Vector3(0.32f, 0.12f, 0.45f), C("#9a4a42"), 0.4f);
        Reg(it, ox - 11.4f, -2.8f, 1.8f, 3.0f);
        Part(p, new Vector3(ox - 11.4f, 0.55f, 3.8f), new Vector3(1.9f, 1.10f, 3.2f), wood, 1f);
        Part(p, new Vector3(ox - 11.4f, 1.14f, 3.8f), new Vector3(2.0f, 0.06f, 3.3f), C("#d9d3c6"), 1f);
        foreach (float bz in new[] { 2.9f, 3.8f, 4.7f })
            Part(p, new Vector3(ox - 11.38f, 1.28f, bz), new Vector3(0.32f, 0.12f, 0.45f), C("#c08a4a"), 0.4f);
        Reg(it, ox - 11.4f, 3.8f, 1.9f, 3.2f);
        Part(p, new Vector3(ox - 12.55f, 1.35f, 4.0f), new Vector3(0.45f, 2.7f, 5.8f), woodDark, 1f);
        for (int r = 0; r < 3; r++)
            Part(p, new Vector3(ox - 12.30f, 0.55f + r * 0.52f, 4.0f),
                 new Vector3(0.06f, 0.04f, 5.5f), wood, 1f);
        Reg(it, ox - 12.55f, 4.0f, 0.45f, 5.8f);
        HangingSign(p, ox - 11.4f, 2.95f, 3.2f, -1, "烘焙", 1.4f, 0.40f, C("#8a5a3c"), 0.027f);
        Pendant(p, ox - 11.4f, 2.65f, 3.8f, 1.0f, "#e0d0b0");

        // 两排双面货架，中央留出视线轴直达品牌墙（第一版中排挡住了品牌墙）
        var shelfFrame = C("#c8c4ba");
        foreach (float sx in new[] { ox - 6.0f, ox + 6.0f })
        {
            ShelfWallZ(p, it, sx - 0.25f, 0.8f, 6.4f, -1, shelfFrame, products, 1.90f, 0.58f);
            ShelfWallZ(p, it, sx + 0.25f, 0.8f, 6.4f, 1, shelfFrame, products, 1.90f, 0.58f);
        }
        HangingSign(p, ox - 6.0f, 2.95f, 0.2f, -1, "零食", 1.4f, 0.38f, C("#4a6a52"), 0.027f);
        HangingSign(p, ox + 6.0f, 2.95f, 0.2f, -1, "粮油", 1.4f, 0.38f, C("#8a5a3c"), 0.027f);

        // 东墙冷藏柜 ×4
        foreach (float cz in new[] { -1.0f, 1.2f, 3.4f, 5.6f })
            Cooler(p, it, ox + 12.45f, cz);
        HangingSign(p, ox + 11.7f, 2.95f, -1.8f, -1, "饮料", 1.4f, 0.40f, C("#3f5c6b"), 0.027f);

        // 南墙品牌墙 / 员工门 / 整箱货架
        Part(p, new Vector3(ox, 2.28f, 8.34f), new Vector3(11.0f, 1.7f, 0.04f), C("#4a6a52"), 1f);
        Part(p, new Vector3(ox, 2.28f, 8.31f), new Vector3(10.5f, 1.42f, 0.012f), cream, 1f);
        SignText(p, new Vector3(ox, 2.28f, 8.30f), 0f, "新鲜每一天", 0.048f, MatLib.Hex("#2f4a3a"));
        for (int i = 0; i < 6; i++)
            Part(p, new Vector3(ox - 3.2f + i * 1.3f, 1.68f, 8.29f),
                 new Vector3(0.22f, 0.22f, 0.01f), products[(i + 1) % products.Length], 0.2f);
        Part(p, new Vector3(ox - 7.4f, 1.35f, 8.30f), new Vector3(1.4f, 2.7f, 0.06f), dark, 1f);
        Part(p, new Vector3(ox - 7.4f, 1.35f, 8.26f), new Vector3(0.10f, 0.12f, 0.03f), metal, 1f);
        HangingSign(p, ox - 7.4f, 2.95f, 7.6f, -1, "员工", 1.1f, 0.36f, C("#3f5c6b"), 0.024f);
        for (int r = 0; r < 2; r++)
            Part(p, new Vector3(ox + 4.0f, 0.55f + r * 1.05f, 8.05f),
                 new Vector3(6.0f, 0.08f, 0.5f), wood, 1f);
        Reg(it, ox + 4.0f, 8.05f, 6.0f, 0.5f);

        Picture(p, new Vector3(ox + 12.83f, 1.9f, -5.0f), 1.2f, 0.7f, 270f);
        Part(p, new Vector3(ox - 12.83f, 2.4f, -6.5f), new Vector3(0.04f, 0.5f, 1.2f), green, 1f);
        Part(p, new Vector3(ox - 12.80f, 2.4f, -6.5f), new Vector3(0.01f, 0.34f, 0.9f), cream, 1f);
        Lamp(p, new Vector3(ox - 7.0f, 3.75f, -3.0f), 12f, 1.10f);
        Lamp(p, new Vector3(ox + 7.0f, 3.75f, -3.0f), 12f, 1.10f);
        Lamp(p, new Vector3(ox - 7.0f, 3.75f, 4.0f), 12f, 1.10f);
        Lamp(p, new Vector3(ox + 7.0f, 3.75f, 4.0f), 12f, 1.10f);
        Lamp(p, new Vector3(ox, 3.75f, 0.5f), 12f, 1.05f);
        return it;
    }

    // ═════════════════════════════════════════════════════
    static Interior Canteen(Transform parent)
    {
        float ox = CANTEEN_X;
        const float X = 14f, Z = 9f, W = 4.2f;
        var it = new Interior
        {
            Id = "canteen", Name = "食堂",
            X1 = ox - X, X2 = ox + X, Z1 = -Z, Z2 = Z,
            Spawn = new Vector2(ox, 7.3f), Exit = new Vector2(ox, 8.3f),
            FloorY = new[] { 0.02f },
            ClearX1 = ox - 1.6f, ClearX2 = ox + 1.6f, ClearZ1 = 4.0f, ClearZ2 = 9f,
        };
        var root = new GameObject("Interior_canteen");
        root.transform.SetParent(parent, false);
        it.Root = root;
        var p = root.transform;

        var wall = T("in_wall");
        var wood = T("wood");
        var woodDark = C("#6f5540");
        var green = C("#4a6a52");
        var teal = C("#3f6b72");
        var terracotta = C("#b06a4e");
        var cream = C("#dfe6dc");
        var fabric = C("#7d8a76");

        Part(p, new Vector3(ox, 0f, 0f), new Vector3(28f, 0.04f, 18f), T("in_tile"), 2f, default, true);
        Part(p, new Vector3(ox, 0.05f, 0f), new Vector3(2.4f, 0.02f, 16.0f), C("#d6d0c4"), 1f);
        Part(p, new Vector3(ox, W - 0.03f, 0f), new Vector3(28f, 0.06f, 18f), T("in_ceil"), 2f);
        WallX(p, it, ox - X, ox + X, -Z, 0f, W, 0f, 0f, 2.6f, wall);
        WallX(p, it, ox - X, ox + X, Z, 0f, W, ox - 1.3f, ox + 1.3f, 2.6f, wall);
        WallZ(p, it, -Z, Z, ox - X, 0f, W, 0f, 0f, 2.6f, wall);
        WallZ(p, it, -Z, Z, ox + X, 0f, W, 0f, 0f, 2.6f, wall);
        DoorFrame(p, ox, Z, 1.3f, 2.6f, -1);
        WallSign(p, ox, 3.30f, Z, -1, "食堂", 2.2f, 0.62f, C("#2f4a3a"), 0.040f);
        PaperRoom(p, ox - X, ox + X, -Z, Z, W, wall, 1, ox - 1.3f, ox + 1.3f);

        // 吸音顶 + 线性灯
        for (float ax = -10f; ax <= 10f; ax += 5f)
            for (float az = -5f; az <= 5f; az += 5f)
                Part(p, new Vector3(ox + ax, W - 0.18f, az), new Vector3(3.4f, 0.06f, 2.0f), C("#d8d2c4"), 1f);
        for (float lz = -5f; lz <= 5f; lz += 5f)
            Part(p, new Vector3(ox, W - 0.30f, lz), new Vector3(24f, 0.04f, 0.10f), C("#fff2d6"), 1f);
        Window(p, new Vector3(ox - 11.3f, 2.35f, -8.80f), 2.4f, 1.8f, 0f);
        Window(p, new Vector3(ox + 11.3f, 2.35f, -8.80f), 2.4f, 1.8f, 0f);
        Window(p, new Vector3(ox + 13.80f, 2.4f, -3f), 2.4f, 1.5f, -90f);
        Window(p, new Vector3(ox + 13.80f, 2.4f, 3f), 2.4f, 1.5f, -90f);
        Window(p, new Vector3(ox - 13.80f, 2.4f, -3f), 2.4f, 1.5f, 90f);
        Window(p, new Vector3(ox - 13.80f, 2.4f, 3f), 2.4f, 1.5f, 90f);

        // 北侧取餐线：三个档口 + 后厨设备
        ServingStation(p, it, ox - 8f, -7.9f, 4.2f, "热菜", woodDark, cream);
        ServingStation(p, it, ox, -7.9f, 4.2f, "面档", woodDark, cream);
        ServingStation(p, it, ox + 8f, -7.9f, 4.2f, "饮品", woodDark, cream);
        foreach (float sx in new[] { -8f, 0f, 8f })
        {
            Part(p, new Vector3(ox + sx, 2.55f, -8.65f), new Vector3(3.2f, 0.50f, 0.60f), C("#3a3f47"), 1f);
            Part(p, new Vector3(ox + sx, 1.08f, -8.55f), new Vector3(3.4f, 0.06f, 0.55f), C("#8d8d94"), 1f);
            Part(p, new Vector3(ox + sx, 1.75f, -8.65f), new Vector3(3.6f, 0.07f, 0.35f), wood, 1f);
        }
        Part(p, new Vector3(ox + 12.1f, 0.55f, -6.9f), new Vector3(1.5f, 1.10f, 0.9f), C("#3f5c4a"), 1f);
        Part(p, new Vector3(ox + 12.1f, 1.12f, -6.9f), new Vector3(1.6f, 0.06f, 0.96f), cream, 1f);
        Reg(it, ox + 12.1f, -6.9f, 1.5f, 0.9f);
        HangingSign(p, ox + 12.1f, 2.95f, -5.9f, 1, "收银", 1.2f, 0.38f, C("#2f4a3a"), 0.025f);

        // 入口：轻食柜 / 餐具回收 / 调料水吧
        Part(p, new Vector3(ox - 12.8f, 1.10f, 7.2f), new Vector3(0.8f, 2.20f, 2.2f), C("#e8e8e4"), 1f);
        Part(p, new Vector3(ox - 12.35f, 1.15f, 7.2f), new Vector3(0.02f, 1.8f, 2.0f), Glass(), 1f);
        Reg(it, ox - 12.8f, 7.2f, 0.8f, 2.2f);
        HangingSign(p, ox - 11.9f, 2.95f, 6.3f, 1, "轻食", 1.2f, 0.38f, C("#4a6a52"), 0.025f);

        Part(p, new Vector3(ox + 7.2f, 0.50f, 7.0f), new Vector3(2.4f, 1.0f, 1.0f), woodDark, 1f);
        Part(p, new Vector3(ox + 7.2f, 1.02f, 7.0f), new Vector3(2.5f, 0.06f, 1.06f), cream, 1f);
        for (int i = 0; i < 3; i++)
            Part(p, new Vector3(ox + 6.5f + i * 0.7f, 1.12f, 7.0f), new Vector3(0.5f, 0.10f, 0.6f), C("#8d949c"), 0.3f);
        Reg(it, ox + 7.2f, 7.0f, 2.4f, 1.0f);
        HangingSign(p, ox + 7.2f, 2.95f, 6.1f, 1, "餐具回收", 1.7f, 0.40f, C("#8a5a3c"), 0.025f);

        Part(p, new Vector3(ox - 4.8f, 0.50f, 6.2f), new Vector3(2.6f, 1.0f, 0.8f), wood, 1f);
        Part(p, new Vector3(ox - 4.8f, 1.02f, 6.2f), new Vector3(2.7f, 0.06f, 0.86f), cream, 1f);
        Part(p, new Vector3(ox - 4.8f, 1.16f, 6.2f), new Vector3(0.18f, 0.22f, 0.18f), C("#7fa6b8"), 0.4f);
        Reg(it, ox - 4.8f, 6.2f, 2.6f, 0.8f);
        HangingSign(p, ox - 4.8f, 2.95f, 5.3f, 1, "调料 · 水", 1.6f, 0.40f, C("#3f5c6b"), 0.025f);

        // 西墙卡座 ×3
        foreach (float tz in new[] { -4.5f, 0f, 4.5f })
        {
            Sofa(p, new Vector3(ox - 13.2f, 0f, tz), 2.4f, 90f, fabric);
            Reg(it, ox - 13.2f, tz, 0.95f, 2.4f);
            Table(p, new Vector3(ox - 11.9f, 0f, tz), 1.2f, 0.9f, 0.76f, wood);
            Reg(it, ox - 11.9f, tz, 1.2f, 0.9f);
            Chair(p, it, new Vector3(ox - 10.7f, 0f, tz), 270f, wood, true);
        }

        // 中央长桌 ×2（长凳）
        foreach (var (tx, tz) in new[] { (-4.5f, -1.8f), (4.5f, 1.4f) })
        {
            Table(p, new Vector3(ox + tx, 0f, tz), 6.0f, 1.2f, 0.76f, wood);
            Reg(it, ox + tx, tz, 6.0f, 1.2f);
            BenchX(p, it, ox + tx, tz - 0.95f, 6.0f);
            BenchX(p, it, ox + tx, tz + 0.95f, 6.0f);
            for (int i = 0; i < 3; i++)
                Part(p, new Vector3(ox + tx - 1.6f + i * 1.6f, 0.79f, tz),
                     new Vector3(0.5f, 0.05f, 0.34f), i % 2 == 0 ? cream : terracotta, 0.4f);
        }

        // 四人桌 ×4 + 东侧窗边吧台
        Table4(p, it, ox - 8.0f, 3.2f, 1.4f, 1.0f, wood);
        Table4(p, it, ox - 5.0f, 4.4f, 1.4f, 1.0f, wood);
        Table4(p, it, ox + 2.5f, 4.2f, 1.4f, 1.0f, wood);
        Table4(p, it, ox + 8.0f, 4.2f, 1.4f, 1.0f, wood);
        Part(p, new Vector3(ox + 13.25f, 0.55f, -1.5f), new Vector3(0.5f, 1.10f, 5.5f), wood, 1f);
        Part(p, new Vector3(ox + 13.25f, 1.12f, -1.5f), new Vector3(0.56f, 0.06f, 5.6f), cream, 1f);
        Reg(it, ox + 13.25f, -1.5f, 0.5f, 5.5f);
        foreach (float sz in new[] { -3.6f, -2.2f, -0.8f, 0.6f })
            Stool(p, it, ox + 12.45f, sz, C("#8a6a4a"));

        // 文化墙 / 绿植 / 挂钟
        Part(p, new Vector3(ox - 13.83f, 2.4f, 0f), new Vector3(0.04f, 1.6f, 3.6f), green, 1f);
        Part(p, new Vector3(ox - 13.80f, 2.4f, 0f), new Vector3(0.01f, 1.35f, 3.3f), cream, 1f);
        SignText(p, new Vector3(ox - 13.79f, 2.4f, 0f), 270f, "好好吃饭", 0.040f, MatLib.Hex("#2f4a3a"));
        Picture(p, new Vector3(ox + 13.83f, 2.2f, 6.2f), 1.3f, 0.9f, 270f);
        WallClock(p, new Vector3(ox + 4.5f, 3.05f, 8.82f), 0f);
        Plant(p, new Vector3(ox - 13.0f, 0.02f, -8.1f), 1.2f);
        Plant(p, new Vector3(ox + 13.0f, 0.02f, -8.1f), 1.0f);
        Plant(p, new Vector3(ox + 13.2f, 0.02f, 8.0f), 1.0f);
        Lamp(p, new Vector3(ox - 8f, 3.95f, 2.0f), 12f, 1.10f);
        Lamp(p, new Vector3(ox + 8f, 3.95f, 2.0f), 12f, 1.10f);
        Lamp(p, new Vector3(ox - 3f, 3.95f, -4.0f), 12f, 1.10f);
        Lamp(p, new Vector3(ox + 3f, 3.95f, -4.0f), 12f, 1.10f);
        return it;
    }
}
