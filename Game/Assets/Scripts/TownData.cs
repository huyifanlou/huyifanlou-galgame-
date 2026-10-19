using System;
using System.Collections.Generic;
using UnityEngine;

// 小镇世界数据（依据 docs/18《小镇总平面布局 V5》落地）
//   第 1 期：地形/水系/路网 + 建筑体块 + 地标（钟楼/车站/神社/游乐设施）
//   坐标约定：x 向东、z 向南；河中心线 z = 10·sin((x+40)/34) + 2，河宽 16m
//   用 -campus 启动可回到原校园世界，默认进入小镇
public static class TownData
{
    public const float BOUND = 95f;
    public const float GROUND_SIZE = 220f;

    public static float RiverZ(float x) => 10f * Mathf.Sin((x + 40f) / 34f) + 2f;

    // 确定性随机（与校园数据同一算法，保证每次构建完全一致）
    class Lcg
    {
        uint s;
        public Lcg(uint seed) { s = seed; }
        public double Next() { unchecked { s = s * 1664525u + 1013904223u; } return s / 4294967296.0; }
        public int NextInt(int max) => (int)Math.Floor(Next() * max);
    }
    static float R1(double v) => (float)(Math.Floor(v * 10.0 + 0.5) / 10.0);

    // ── 建筑速写 ──
    static BuildingDef B(string id, string name, float x, float z, float w, float h, float d,
        string wall, string trim, string plinth, string roof, int cols, int rows,
        int face = 1, string style = "flat")
        => new BuildingDef
        {
            id = id, name = name, pos = new[] { x, z }, size = new[] { w, h, d },
            wall = wall, trim = trim, plinth = plinth, roof = roof, cols = cols, rows = rows,
            face = face, style = style
        };

    // 近代配色
    const string W_MILK = "#e6dcc6", T_WHITE = "#f6f1e4", P_STONE = "#9c907a", R_TILE = "#6a4a3c";
    const string W_BRICK = "#b3705a", T_CREAM = "#f2e6d4", P_DARK = "#7c5a48", R_SLATE = "#3f4a54";
    const string W_GREY = "#c8c4ba", T_PALE = "#efece2", P_GREY = "#8e8a80", R_GREEN = "#3f5a4a";
    const string W_WOOD = "#c09a6a", T_BEIGE = "#efe2cc", P_WOOD = "#8a6a4a";
    const string R_DARK = "#4a3a34", W_SHRINE = "#a8624a";
    const string TP = "#f2e4d6", SPT = "#6a3a2a";

    static readonly BuildingDef[] Buildings =
    {
        // ── 学校区（西北）：最上一栋宿舍楼 ──
        B("s_dorm", "宿舍楼", -61, -78, 22, 13, 9, W_MILK, T_WHITE, P_STONE, R_TILE, 7, 4),
        B("s_teach", "教学楼", -67, -62, 24, 16, 10, W_GREY, T_PALE, P_GREY, R_GREEN, 8, 5, 1, "grand"),
        B("s_lib", "图书馆", -60, -46, 16, 11, 10, "#d3c2a2", "#f2ead8", "#94805e", "#7c4a38", 5, 3, 1, "grand"),
        B("s_lab", "实验楼", -24, -78, 18, 14, 9, "#c3c8cf", "#eef1f4", "#828a94", "#3d4654", 6, 4),
        B("s_canteen", "食堂", -30, -64, 18, 8, 10, "#e0b34e", "#fff3d6", "#a07838", "#a03c30", 6, 2, 1, "gable"),
        B("s_hall", "礼堂", -24, -52, 16, 12, 8, "#e0d5c3", "#f6f0e4", "#9c8f78", "#4e6a58", 5, 3, 1, "grand"),
        B("s_gate", "门卫", -52, -32, 5, 3.5f, 4, "#d8d2c4", "#f2eee4", "#98917f", "#4a5568", 2, 1),
        B("s_stand", "看台", -28, -28, 24, 2.4f, 3, "#bdb6a8", "#e8e3d6", "#8e8a80", "#5a5a60", 6, 1, 1, "plain"),

        // ── 公园 ──
        B("p_pav", "音乐亭", 56, -32, 6, 5, 6, W_MILK, T_WHITE, P_STONE, R_TILE, 2, 1, 1, "gable"),

        // ── 神社（樱花林右侧）──
        B("k_honden", "本殿", 79, -80, 14, 7, 9, W_SHRINE, TP, SPT, R_DARK, 4, 1, 1, "shrine"),
        B("k_haiden", "拜殿", 79, -70, 10, 5, 6, "#b06a4e", TP, SPT, R_DARK, 3, 1, 1, "shrine"),
        B("k_chozu", "手水舍", 69, -66, 4, 3, 3, W_SHRINE, TP, SPT, R_DARK, 1, 1, 1, "gable"),

        // ── 骑楼老街 ──
        B("o_silk", "绸缎庄", -68, 18.5f, 10, 8, 5, W_BRICK, T_CREAM, P_DARK, R_SLATE, 3, 2, 1, "gable"),
        B("o_tea", "茶楼", -56, 18.5f, 10, 8, 5, W_BRICK, T_CREAM, P_DARK, R_SLATE, 3, 2, 1, "gable"),
        B("o_grocery", "杂货铺", -68, 36, 10, 7.5f, 5, W_WOOD, T_BEIGE, P_WOOD, R_TILE, 3, 2, -1, "gable"),
        B("o_pharm", "药铺", -54, 36, 10, 7.5f, 5, W_WOOD, T_BEIGE, P_WOOD, R_TILE, 3, 2, -1, "gable"),
        B("o_theater", "戏院", -32, 38, 14, 11, 8, "#b56a4a", "#f0ddd0", "#7a4a38", "#5a3a30", 4, 2, -1, "grand"),
        B("o_post", "邮局", -18, 36, 12, 8, 5, "#d4c8b4", "#f4eee2", "#8a7e6a", "#4a5568", 4, 2, -1),

        // ── 河东住宅 第1排：店铺（面朝北街）──
        B("r_milktea", "奶茶店", -75, 55, 9, 7, 6, "#e8d9c0", "#f8f2e6", P_STONE, "#7a4a3a", 3, 2, -1),
        B("r_market", "超市", -64.5f, 55, 9, 7.5f, 6, "#cfc9bd", "#efece2", P_GREY, "#4a5568", 3, 2, -1),
        B("r_rest", "餐馆", -54, 55, 9, 7, 6, "#c8a06a", "#f0e2d0", "#8a6a52", "#5a4a42", 3, 2, -1, "gable"),
        B("r_photo", "照相馆", -37, 55, 9, 7, 6, "#d8cbb4", "#f6f0e2", "#948a74", "#4a5568", 3, 2, -1),
        B("r_book", "书店", -26, 55, 9, 7, 6, "#cbb894", "#f0e8d6", "#8d7a58", "#a05a3c", 3, 2, -1),
        B("r_conv", "便利店", -17.5f, 55, 7, 6.5f, 6, "#d4c4a8", "#f4ecdc", "#907e62", "#5a4a42", 2, 2, -1),

        // ── 第2排（主角家在中间，由 VILLA 单独生成）──
        B("r_court1", "合院", -75, 66, 10, 5, 6, W_MILK, T_WHITE, P_STONE, R_TILE, 3, 1, 1, "gable"),
        B("r_two1", "两层小楼", -64.5f, 66, 8, 6.5f, 6, W_WOOD, T_BEIGE, P_WOOD, R_TILE, 3, 2, 1, "gable"),
        B("r_flat1", "平房小院", -54, 66, 9, 4.5f, 6, W_GREY, T_PALE, P_GREY, R_SLATE, 3, 1, 1),
        B("r_two2", "两层小楼", -22, 66, 8, 6.5f, 6, W_WOOD, T_BEIGE, P_WOOD, R_TILE, 3, 2, 1, "gable"),

        // ── 第3排 ──
        B("r_court2", "合院", -75, 78, 10, 5, 6, W_MILK, T_WHITE, P_STONE, R_TILE, 3, 1, -1, "gable"),
        B("r_two3", "两层小楼", -64.5f, 78, 8, 6.5f, 6, W_WOOD, T_BEIGE, P_WOOD, R_TILE, 3, 2, -1, "gable"),
        B("r_flat2", "平房小院", -54, 78, 9, 4.5f, 6, W_GREY, T_PALE, P_GREY, R_SLATE, 3, 1, -1),
        B("r_flat3", "平房小院", -35, 78, 10, 4.5f, 6, W_GREY, T_PALE, P_GREY, R_SLATE, 3, 1, -1),
        B("r_two4", "两层小楼", -22, 78, 8, 6.5f, 6, W_WOOD, T_BEIGE, P_WOOD, R_TILE, 3, 2, -1, "gable"),

        // ── 第4排：单元楼（4/6/5/4 层）──
        B("r_apt1", "单元楼", -74, 87.5f, 12, 12, 7, "#c8bfae", "#efe9dc", "#8a8272", "#4a4a50", 4, 4, -1),
        B("r_apt2", "单元楼", -58, 87.5f, 12, 18, 7, "#bfb6a4", "#ece6d8", "#857c6c", "#3f4650", 4, 6, -1),
        B("r_apt3", "单元楼", -34, 87.5f, 14, 15, 7, "#c4bbaa", "#eee8da", "#887f6e", "#444a54", 5, 5, -1),
        B("r_apt4", "单元楼", -19.5f, 87.5f, 11, 12, 7, "#c8bfae", "#efe9dc", "#8a8272", "#4a4a50", 4, 4, -1),

        // ── 车站 ──
        B("st_hall", "站房", 76, 76, 20, 9, 9, W_BRICK, T_CREAM, P_DARK, R_SLATE, 5, 2, -1, "grand"),
        B("st_platform", "站台", 76, 84, 22, 1.2f, 4, "#bdb6a8", "#e8e3d6", "#8e8a80", "#5a5a60", 6, 1, -1, "plain"),

        // ── 游乐场（体块：其余设施用专门道具）──
        B("f_ticket", "售票处", 11, 36, 5, 3.2f, 8, "#e8c46a", "#fff6e0", "#a8843c", "#b8452f", 2, 1, 1, "plain"),
        B("f_bumper", "碰碰车", 19, 32, 10, 3.6f, 8, "#cfd8e2", "#eef2f6", "#8a929c", "#4a5a6e", 4, 1, 1, "plain"),
        B("f_haunted", "鬼屋", 37, 43, 8, 6.5f, 6, "#5a4a5e", "#cbb8d4", "#3f3448", "#2f2a3a", 3, 2, 1, "gable"),
        B("f_teahouse", "茶座", 47, 44, 6, 3.4f, 5, "#d8c8a8", "#f6eedc", "#94805e", "#6a4a3c", 2, 1, 1, "plain"),
        B("f_games", "游戏摊", 14, 49, 8, 3, 4, "#e0b34e", "#fff3d6", "#a07838", "#a03c30", 4, 1, 1, "plain"),
        B("f_food", "小吃街", 27, 50, 14, 3, 4, "#d8a05a", "#ffeed2", "#98703c", "#8a4438", 6, 1, 1, "plain"),
    };

    // ── 道路（axis=z 为南北向、at=x；axis=x 为东西向、at=z）──
    static RoadDef R(string id, string axis, float at, float from, float to, float width, string kind, bool walk = true)
        => new RoadDef { id = id, axis = axis, at = at, from = from, to = to, width = width, kind = kind, fence = false, sidewalk = walk };

    static readonly RoadDef[] Roads =
    {
        R("main", "z", 0f, -98f, 98f, 11f, "mainstone"),          // 主街（石板）
        R("central", "z", -45f, -92f, 98f, 5f, "slab"),           // 中央街（青砖）
        R("ringN", "z", -84f, -76f, -16f, 5f, "slab"),            // 西环路北段
        R("ringS", "z", -84f, 8f, 88f, 5f, "slab"),               // 西环路南段
        R("market", "x", 27f, -74f, -6f, 8f, "mainstone"),        // 老街商街（旧石板）
        R("resN", "x", 49f, -74f, -16f, 4f, "slab"),              // 住宅北街
        R("resM", "x", 72f, -74f, -16f, 4f, "slab"),              // 住宅中街
        R("stSt", "x", 58f, 6f, 92f, 7f, "slab"),                 // 车站街
        R("stRd", "z", 58f, 58f, 88f, 6f, "slab"),                // 车站路
        R("parkRd", "z", 65f, -46f, -10f, 5f, "slab"),            // 公园联络路
        R("fest", "x", -46f, 62f, 94f, 12f, "mainstone", false),  // 夏日祭参道（整幅铺石，无侧石）
        R("shrineSteps", "z", 79f, -62f, -48f, 10f, "slab", false), // 神社石阶
    };

    // ── 步道（任意方向铺装条，旋转到走向；宽度 3m）──
    static PropDef PL2(float x0, float z0, float x1, float z1, float width = 3f)
    {
        float dx = x1 - x0, dz = z1 - z0;
        return new PropDef
        {
            type = "plaza",
            x = (x0 + x1) / 2f, z = (z0 + z1) / 2f,
            w = width, d = Mathf.Sqrt(dx * dx + dz * dz),
            ry = Mathf.Atan2(dx, dz),
        };
    }
    static PropDef P(string type, float x, float z, float ry = 0f, float w = 0f, float d = 0f, float r = 0f,
        float len = 0f, int n = 0)
        => new PropDef { type = type, x = x, z = z, ry = ry, w = w, d = d, r = r, len = len, n = n };

    static readonly PropDef[] Props =
    {
        // 校区 ↔ 公园 步道
        PL2(-45, -23.5f, 12, -23.5f), PL2(12, -23.5f, 12, -58), PL2(12, -58, 60, -58),
        PL2(36, -58, 32, -62), PL2(32, -62, 37, -66), PL2(37, -66, 40, -71),
        PL2(40, -71, 37, -76), PL2(37, -76, 36, -82),
        PL2(60, -74, 66, -74),                                    // 神社连林道
        PL2(28, -16, 28, 2), PL2(28, 19, 28, 26),                 // 廊桥南北接驳
        PL2(54, -46, 62, -46),                                    // 参道连园道
        // 学校内部支路
        PL2(-84, -71, -48, -71), PL2(-84, -54, -45, -54), PL2(-41.5f, -45.5f, -14, -45.5f),
        // 住宅排间巷
        PL2(-74, 60.5f, -16, 60.5f),

        // 广场 / 站前 / 停车
        P("plaza", -15, -12, w: 18, d: 20),                       // 钟楼广场
        P("plaza", 76, 68, w: 26, d: 6),                          // 站前广场
        P("parking", 20, 72, w: 24, d: 16),
        P("crates", 44, 74, w: 14, d: 14),
        P("clocktower", -14, -12),
        P("railway", 0, 92, len: 212),
        P("torii", 79, -63.4f, w: 10),

        // 游乐场设施
        P("ride_carousel", 31, 33), P("ride_swing", 42, 31), P("ride_wheel", 55, 32),
        P("ride_tower", 88, 38), P("ride_train", 23, 42), P("ride_pendulum", 60, 45),
        P("ride_tent", 76, 43), P("ride_cart", 39, 50), P("ride_stage", 47, 50),
        P("kart", 72, 28, w: 14, d: 10),

        // 夏日祭摊位（参道两侧）
        P("stall", 67, -52.5f), P("stall", 73, -52.5f), P("stall", 79, -52.5f), P("stall", 85, -52.5f), P("stall", 91, -52.5f),
        P("stall", 67, -39.5f), P("stall", 73, -39.5f), P("stall", 79, -39.5f), P("stall", 85, -39.5f), P("stall", 91, -39.5f),

        // 公园喷泉
        P("fountain", 34, -40, r: 4.2f),

        // 河畔长椅
        P("bench", -30, -2.5f, ry: Mathf.PI), P("bench", 12, -2.5f, ry: Mathf.PI),
        P("bench", -62, 20.5f, ry: Mathf.PI), P("bench", 48, 20.5f, ry: Mathf.PI),
    };

    // ── 保留区（植被不得进入）──
    static List<(float x, float z, float hw, float hd)> KeepOut()
    {
        var k = new List<(float, float, float, float)>();
        foreach (var b in Buildings) k.Add((b.pos[0], b.pos[1], b.size[0] / 2f + 2.2f, b.size[2] / 2f + 2.2f));
        foreach (var r in Roads) k.Add(r.axis == "z"
            ? (r.at, (r.from + r.to) / 2f, r.width / 2f + 2.4f, (r.to - r.from) / 2f + 2.4f)
            : ((r.from + r.to) / 2f, r.at, (r.to - r.from) / 2f + 2.4f, r.width / 2f + 2.4f));
        k.Add((-35.5f, 66f, 8f, 5f));                                  // 主角家
        k.Add((-28f, -37f, 14f, 8f));                                  // 操场
        k.Add((-15f, -12f, 11f, 12f));                                 // 钟楼广场
        k.Add((76f, 68f, 15f, 5f));                                    // 站前广场
        k.Add((20f, 72f, 14f, 10f));                                   // 停车场
        k.Add((44f, 74f, 9f, 9f));                                     // 货场
        k.Add((55f, 32f, 8f, 8f));                                     // 摩天轮
        k.Add((76f, 43f, 9f, 7f));                                     // 马戏棚
        k.Add((31f, 33f, 7f, 7f));                                     // 旋转木马
        k.Add((72f, 28f, 9f, 7f));                                     // 卡丁车
        return k;
    }

    // 点是否离步道（prop plaza）太近
    static bool NearPath(float x, float z, float margin)
    {
        foreach (var p in Props)
        {
            if (p.type != "plaza") continue;
            float dx = x - p.x, dz = z - p.z;
            float c = Mathf.Cos(-p.ry), s = Mathf.Sin(-p.ry);
            float lx = dx * c - dz * s, lz = dx * s + dz * c;   // 转到步道局部坐标
            if (Mathf.Abs(lx) < p.w / 2f + margin && Mathf.Abs(lz) < p.d / 2f + margin) return true;
        }
        return false;
    }

    static bool InKeep(float x, float z, List<(float x, float z, float hw, float hd)> keep, bool river = true)
    {
        if (river && Mathf.Abs(z - RiverZ(x)) < 14.5f) return true;                  // 河道 + 护岸
        float pdx = (x - 34f) / 21f, pdz = (z + 40f) / 15f;
        if (pdx * pdx + pdz * pdz < 1.25f) return true;                              // 池塘
        foreach (var k in keep)
            if (Mathf.Abs(x - k.x) < k.hw && Mathf.Abs(z - k.z) < k.hd) return true;
        return NearPath(x, z, 1.4f);
    }

    // ── 生成完整世界数据 ──
    public static WorldData Build()
    {
        var w = new WorldData
        {
            BOUND = BOUND,
            GROUND_SIZE = GROUND_SIZE,
            ROADS = Roads,
            BRANCH_PATHS = new BranchPathDef[0],
            BUILDINGS = Buildings,
            GROUNDS = new[]
            {
                new GroundDef { id = "playground", name = "操场", pos = new[] { -28f, -37f }, size = new[] { 24f, 12f } },
            },
            PROPS = Props,
            POND = new PondDef { pos = new[] { 34f, -40f }, r = 20f },
            VILLA = new VillaDef { x = -35.5f, z = 66f, w = 12f, h = 6f, d = 6f, face = 1 },
            FENCES = new FenceDef[0],
            DISTANT_BUILDINGS = new DistantDef[0],
            ROCKERIES = new RockeryItem[0],
            REEDS = new ReedItem[0],
            DIRT_PATCHES = new PosItem[0],
            SMALL_ROCKS = new PosItem[0],
            RIVER = new RiverDef
            {
                width = 16f, amp = 10f, freq = 34f, phase = 40f, offset = 2f, from = -110f, to = 110f,
                bridges = new[]
                {
                    new BridgeDef { x = 0f, width = 11f, arches = 3 },
                    new BridgeDef { x = -45f, width = 5f, arches = 2 },
                    new BridgeDef { x = 28f, width = 6.5f, arches = 1, covered = true },
                }
            },
        };

        var keep = KeepOut();
        var rnd = new Lcg(20261020);
        var trees = new List<TreeItem>();

        void TryTree(float x, float z, int v, float gap = 2.6f)
        {
            if (Mathf.Abs(x) > BOUND - 3f || Mathf.Abs(z) > BOUND - 3f) return;
            if (InKeep(x, z, keep)) return;
            foreach (var t in trees)
                if (Mathf.Sqrt((x - t.x) * (x - t.x) + (z - t.z) * (z - t.z)) < gap) return;
            trees.Add(new TreeItem { x = R1(x), z = R1(z), v = v });
        }

        // 樱花林（公园北侧 x 14~60 / z -84~-66）
        for (int i = 0; i < 26; i++)
            TryTree(16f + (float)rnd.Next() * 42f, -82f + (float)rnd.Next() * 14f, 10, 3.0f);
        // 公园点景树（围绕池塘）
        foreach (var (tx, tz) in new[] { (20f, -52f), (28f, -55f), (44f, -54f), (52f, -48f), (54f, -32f), (16f, -40f), (16f, -28f), (48f, -56f) })
            trees.Add(new TreeItem { x = R1(tx), z = R1(tz), v = 0 });
        for (int i = 0; i < 10; i++)
            TryTree(14f + (float)rnd.Next() * 46f, -56f + (float)rnd.Next() * 30f, 2 + rnd.NextInt(2), 4f);
        // 河畔柳树（两岸每 11m 一株，避开桥位；树冠退到步道外侧）
        for (float x = -90f; x <= 90f; x += 11f)
            foreach (int sgn in new[] { -1, 1 })
            {
                bool nearBridge = false;
                foreach (var br in w.RIVER.bridges) if (Mathf.Abs(x - br.x) < 9f) nearBridge = true;
                if (nearBridge) continue;
                TryTree(x + (float)rnd.Next() * 3.5f, RiverZ(x) + sgn * (17f + (float)rnd.Next() * 2f), 9, 3f);
            }
        // 滨河内侧点景树（更靠水，少量）
        for (float x = -84f; x <= 84f; x += 24f)
            foreach (int sgn in new[] { -1, 1 })
            {
                bool nearBridge = false;
                foreach (var br in w.RIVER.bridges) if (Mathf.Abs(x - br.x) < 10f) nearBridge = true;
                if (nearBridge) continue;
                if (rnd.Next() < 0.45) continue;
                TryTree(x, RiverZ(x) + sgn * 10.6f, 5, 2.5f);
            }
        // 主街行道树（避开河段）
        for (float z = -78f; z <= 86f; z += 13f)
        {
            if (Mathf.Abs(z - RiverZ(0)) < 14f) continue;
            foreach (int sgn in new[] { -1, 1 })
                TryTree(sgn * 8.7f, z, sgn < 0 ? 0 : 5, 2.2f);
        }
        // 站前绿地点景
        TryTree(12f, 86f, 0); TryTree(22f, 86f, 5); TryTree(32f, 86f, 2);
        // 全镇散布
        for (int i = 0; i < 90; i++)
        {
            float x = (float)(rnd.Next() * 2 - 1) * 92f;
            float z = (float)(rnd.Next() * 2 - 1) * 92f;
            TryTree(x, z, rnd.NextInt(9), 4.5f);
        }
        w.TREES = trees.ToArray();

        // 灌木 / 花
        var bushes = new List<PosItem>();
        for (int i = 0; i < 700 && bushes.Count < 110; i++)
        {
            float x = (float)(rnd.Next() * 2 - 1) * 92f, z = (float)(rnd.Next() * 2 - 1) * 92f;
            if (InKeep(x, z, keep, false)) continue;
            if (Mathf.Abs(z - RiverZ(x)) < 15.5f) continue;
            bool near = false;
            foreach (var t in trees) if (Mathf.Sqrt((x - t.x) * (x - t.x) + (z - t.z) * (z - t.z)) < 2.2f) { near = true; break; }
            if (near) continue;
            bushes.Add(new PosItem { x = R1(x), z = R1(z) });
        }
        w.BUSHES = bushes.ToArray();

        var flowers = new List<PosItem>();
        for (int i = 0; i < 900 && flowers.Count < 150; i++)
        {
            float x = (float)(rnd.Next() * 2 - 1) * 92f, z = (float)(rnd.Next() * 2 - 1) * 92f;
            if (InKeep(x, z, keep, false)) continue;
            if (Mathf.Abs(z - RiverZ(x)) < 15.5f) continue;
            bool near = false;
            foreach (var p in bushes) if (Mathf.Sqrt((x - p.x) * (x - p.x) + (z - p.z) * (z - p.z)) < 1.5f) { near = true; break; }
            if (near) continue;
            flowers.Add(new PosItem { x = R1(x), z = R1(z) });
        }
        w.FLOWERS = flowers.ToArray();

        // 芦苇：池塘沿 + 河岸（两岸交替）
        var reeds = new List<ReedItem>();
        for (int i = 0; i < 26; i++)
        {
            float a = i / 26f * Mathf.PI * 2f;
            reeds.Add(new ReedItem
            {
                x = (float)Math.Round((34 + Mathf.Cos(a) * 20.6f) * 100) / 100f,
                z = (float)Math.Round((-40 + Mathf.Sin(a) * 14.6f) * 100) / 100f,
                h = 0.7f + rnd.NextInt(70) / 100f, t = rnd.NextInt(3),
            });
        }
        for (float x = -96f; x <= 96f; x += 6f)
        {
            bool nearBridge = false;
            foreach (var br in w.RIVER.bridges) if (Mathf.Abs(x - br.x) < 7f) nearBridge = true;
            if (nearBridge) continue;
            int sgn = Mathf.RoundToInt(x / 6f) % 2 == 0 ? -1 : 1;
            if (rnd.Next() < 0.45) continue;
            reeds.Add(new ReedItem { x = R1(x), z = R1(RiverZ(x) + sgn * 8.6f), h = 0.55f + rnd.NextInt(50) / 100f, t = rnd.NextInt(3) });
        }
        w.REEDS = reeds.ToArray();

        // 路灯（主街 / 老街 / 滨河 / 车站街）
        var lamps = new List<LampItem>();
        for (float z = -80f; z <= 86f; z += 18f)
        {
            if (Mathf.Abs(z - RiverZ(0)) < 14f) continue;
            lamps.Add(new LampItem { x = -6.4f, z = z, r = 0f });
            lamps.Add(new LampItem { x = 6.4f, z = z, r = Mathf.PI });
        }
        foreach (float x in new[] { -70f, -50f, -30f, -12f })
        {
            lamps.Add(new LampItem { x = x, z = 22.3f, r = -Mathf.PI / 2f });
            lamps.Add(new LampItem { x = x, z = 31.7f, r = Mathf.PI / 2f });
        }
        for (float x = -90f; x <= 90f; x += 26f)
        {
            if (Mathf.Abs(RiverZ(x)) > 60f) continue;
            lamps.Add(new LampItem { x = x, z = RiverZ(x) + 15.6f, r = -Mathf.PI / 2f });
        }
        for (float x = 14f; x <= 84f; x += 18f)
            lamps.Add(new LampItem { x = x, z = 53.6f, r = Mathf.PI / 2f });
        w.LAMPS = lamps.ToArray();

        // 边界林带
        var bt = new List<TreeItem>();
        for (int i = 0; i < 96; i++)
        {
            float a = i / 96f * Mathf.PI * 2f;
            float r = 101f + (float)rnd.Next() * 6f;
            bt.Add(new TreeItem { x = R1(Mathf.Cos(a) * r), z = R1(Mathf.Sin(a) * r), v = 6 + rnd.NextInt(3) });
        }
        w.BORDER_TREES = bt.ToArray();
        w.BORDER_BUSHES = new PosItem[0];
        w.BORDER_ROCKS = new PosItem[0];

        // 交互点
        var spots = new List<SpotDef>();
        foreach (var b in Buildings)
            spots.Add(new SpotDef
            {
                name = b.name, x = b.pos[0], z = b.pos[1] + b.face * (b.size[2] / 2f + 1.2f),
                r = Mathf.Clamp(b.size[0] / 2f, 2.2f, 4f)
            });
        spots.Add(new SpotDef { name = "家", x = -35.5f, z = 66f + 3f + 1.2f, r = 3f });
        spots.Add(new SpotDef { name = "操场", x = -28f, z = -31f, r = 3f });
        spots.Add(new SpotDef { name = "钟楼广场", x = -14f, z = -6f, r = 3.5f });
        spots.Add(new SpotDef { name = "公园", x = 34f, z = -24f, r = 4f });
        spots.Add(new SpotDef { name = "池塘", x = 34f, z = -23.5f, r = 3f });
        spots.Add(new SpotDef { name = "樱花林", x = 36f, z = -66f, r = 4f });
        spots.Add(new SpotDef { name = "鸟居", x = 79f, z = -63f, r = 3f });
        spots.Add(new SpotDef { name = "游乐场", x = 19f, z = 44f, r = 4f });
        spots.Add(new SpotDef { name = "摩天轮", x = 55f, z = 38f, r = 3.5f });
        spots.Add(new SpotDef { name = "河畔", x = -14f, z = -2f, r = 3f });
        spots.Add(new SpotDef { name = "石拱桥", x = 0f, z = RiverZ(0) + 11f, r = 3f });
        spots.Add(new SpotDef { name = "廊桥", x = 28f, z = RiverZ(28) + 11f, r = 3f });
        w.SPOTS = spots.ToArray();

        // 碰撞：建筑 / 主角家 / 河面（桥位留口）/ 主要设施
        var bl = new List<BlockerDef>();
        foreach (var b in Buildings)
            bl.Add(new BlockerDef { x = b.pos[0], z = b.pos[1], hw = b.size[0] / 2f, hd = b.size[2] / 2f });
        bl.Add(new BlockerDef { x = -35.5f, z = 66f, hw = 6f, hd = 3f });
        for (float x = -104f; x <= 104f; x += 1.6f)
        {
            bool bridged = false;
            foreach (var br in w.RIVER.bridges)
                if (Mathf.Abs(x - br.x) < br.width / 2f - 0.2f) { bridged = true; break; }
            if (bridged) continue;
            bl.Add(new BlockerDef { x = x, z = RiverZ(x), hw = 0.9f, hd = 8.0f });
        }
        foreach (var p in Props)
        {
            switch (p.type)
            {
                case "clocktower": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 2.2f, hd = 2.2f }); break;
                case "ride_carousel": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 5f, hd = 5f }); break;
                case "ride_wheel": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 3f, hd = 3f }); break;
                case "ride_tent": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 7f, hd = 5f }); break;
                case "ride_tower": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 2.5f, hd = 2.5f }); break;
                case "ride_pendulum": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 4f, hd = 3.5f }); break;
                case "ride_swing": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 4f, hd = 4f }); break;
                case "ride_stage": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 3f, hd = 2f }); break;
                case "crates": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 6f, hd = 6f }); break;
                case "stall": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 1.3f, hd = 1.3f }); break;
                case "fountain": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = p.r, hd = p.r }); break;
                case "torii":
                    bl.Add(new BlockerDef { x = p.x - p.w / 2f + 0.5f, z = p.z, hw = 0.45f, hd = 0.45f });
                    bl.Add(new BlockerDef { x = p.x + p.w / 2f - 0.5f, z = p.z, hw = 0.45f, hd = 0.45f });
                    break;
            }
        }
        w.BLOCKERS = bl.ToArray();
        return w;
    }
}
