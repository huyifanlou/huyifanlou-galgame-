using System;
using System.Collections.Generic;
using UnityEngine;

// 小镇世界数据（依据 docs/18《小镇总平面布局 V5》落地）
//   第 1 期：地形/水系/路网 + 建筑体块 + 地标（钟楼/车站/神社/游乐设施）
//   坐标约定：x 向东、z 向南；河中心线 z = 10·sin((x+40)/34) + 2，河宽 16m
//   用 -campus 启动可回到原校园世界，默认进入小镇
public static class TownData
{
    public const float BOUND = 128f;
    public const float GROUND_SIZE = 620f;

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
        // ── 学校区（西北）：图书馆收西轴线，教学/实验夹方院，生活区在西南，运动区隔中央街 ──
        // 学校区：校门在东北角主轴，图书馆压轴，教学/实验隔 30m 方院相对；
        // 宿舍食堂独立西北生活园，活动中心在北侧服务带，礼堂对准校门并远离跑道
        B("s_teach", "教学楼", -68, -86, 30, 16, 11, "#a9604a", "#efe3ce", "#7d6a56", "#3f4a54", 8, 5, 1, "collegiate"),
        B("s_lib", "图书馆", -94, -60, 22, 13, 12, "#d8cbb2", "#f5efe0", "#8f8270", "#4a6a58", 6, 3, 2, "library"),
        B("s_lab", "实验楼", -68, -42, 26, 14, 10, "#c3c8cf", "#eef1f4", "#828a94", "#3d4654", 7, 4, -1, "modern"),
        B("s_dorm", "宿舍楼", -111, -33, 18, 10, 9, "#b9775e", "#f0e4cf", "#80685a", "#5a4a42", 7, 4, 2, "dorm"),
        B("s_canteen", "食堂", -93, -31, 12, 8, 6.5f, "#e0b34e", "#fff3d6", "#a07838", "#a03c30", 4, 2, -2, "gable"),
        B("s_center", "活动中心", -70, -26, 20, 8, 8, "#b98a62", "#f2e6d4", "#8a6a52", "#4a5a52", 6, 2, -1, "gable"),
        B("s_hall", "礼堂", -24, -60, 16, 12, 8, "#d8cbb2", "#f6f0e4", "#9c8f78", "#3f5a4a", 5, 3, -2, "hall"),
        B("s_gate", "门卫", -54, -66, 6, 4, 4, "#d8d2c4", "#f2eee4", "#98917f", "#4a5568", 2, 1, 1, "gable"),
        B("s_stand", "看台", -28, -23, 24, 0.6f, 5, "#bdb6a8", "#e8e3d6", "#8e8a80", "#5a5a60", 6, 1, -1, "stand"),

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
        B("r_rest", "餐厅", -54, 55, 9, 7, 6, "#c8a06a", "#f0e2d0", "#8a6a52", "#5a4a42", 3, 2, -1, "gable"),
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
        B("r_apt1", "单元楼", -74, 93f, 12, 12, 7, "#c8bfae", "#efe9dc", "#8a8272", "#4a4a50", 4, 4, -1),
        B("r_apt2", "单元楼", -58, 93f, 12, 18, 7, "#bfb6a4", "#ece6d8", "#857c6c", "#3f4650", 4, 6, -1),
        B("r_apt3", "单元楼", -34, 93f, 14, 15, 7, "#c4bbaa", "#eee8da", "#887f6e", "#444a54", 5, 5, -1),
        B("r_apt4", "单元楼", -19.5f, 93f, 11, 12, 7, "#c8bfae", "#efe9dc", "#8a8272", "#4a4a50", 4, 4, -1),

        // ── 车站 ──
        B("st_hall", "站房", 76, 86, 20, 9, 9, W_BRICK, T_CREAM, P_DARK, R_SLATE, 5, 2, -1, "grand"),
        B("st_platform", "站台", 76, 98, 22, 1.2f, 4, "#bdb6a8", "#e8e3d6", "#8e8a80", "#5a5a60", 6, 1, -1, "plain"),

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
        R("ringS", "z", -84f, 8f, 88f, 5f, "slab"),               // 西环路南段
        R("market", "x", 27f, -74f, -6f, 8f, "mainstone"),        // 老街商街（旧石板）
        R("resN", "x", 49f, -74f, -16f, 4f, "slab"),              // 住宅北街
        R("resM", "x", 72f, -74f, -16f, 4f, "slab"),              // 住宅中街
        R("stSt", "x", 58f, 6f, 92f, 7f, "slab"),                 // 车站街
        R("stRd", "z", 58f, 58f, 96f, 6f, "slab"),                // 车站路
        R("parkRd", "z", 65f, -46f, -10f, 5f, "slab"),            // 公园联络路
        R("fest", "x", -46f, 62f, 94f, 12f, "mainstone", false),  // 夏日祭参道（整幅铺石，无侧石）
        R("shrineSteps", "z", 79f, -62f, -48f, 10f, "slab", false), // 神社石阶
    };

    // ── 场地（操场跑道 / 球场）──
    static readonly GroundDef[] Grounds =
    {
        new GroundDef { id = "playground", name = "操场", pos = new[] { -28f, -32f }, size = new[] { 24f, 12f } },
        // 篮球场移到体育区：礼堂南侧，与跑道分开，避免压生活园
        new GroundDef { id = "basket", name = "篮球场", kind = "basket", pos = new[] { -28f, -82f }, size = new[] { 14f, 8f } },
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
        float len = 0f, int n = 0, string kind = null)
        => new PropDef { type = type, x = x, z = z, ry = ry, w = w, d = d, r = r, len = len, n = n, kind = kind };

    static readonly PropDef[] Props =
    {
        // 校区 ↔ 公园 步道
        PL2(-45, -23.5f, 12, -23.5f), PL2(12, -23.5f, 12, -58), PL2(12, -58, 60, -58),
        PL2(36, -58, 32, -62), PL2(32, -62, 37, -66), PL2(37, -66, 40, -71),
        PL2(40, -71, 37, -76), PL2(37, -76, 36, -82),
        PL2(60, -74, 66, -74),                                    // 神社连林道
        PL2(28, -16, 28, 2), PL2(28, 19, 28, 26),                 // 廊桥南北接驳
        PL2(54, -46, 62, -46),                                    // 参道连园道
        // 校园步道：校门主轴（→图书馆）、教学/实验次轴、生活园环路、活动中心支路
        PL2(-87.5f, -60, -49.4f, -60, 5f),
        PL2(-68, -78, -68, -49, 4f),
        PL2(-102, -46, -83, -46, 3f),
        PL2(-102, -24, -102, -46, 3f),
        PL2(-83, -46, -83, -60, 3f),
        PL2(-51.5f, -44, -51.5f, -32.5f, 2.5f),
        PL2(-51.5f, -32.5f, -75, -32.5f, 2.5f),
        // 体育区：校门→礼堂、礼堂前庭→篮球场、中央街→操场/看台
        PL2(-49.4f, -60, -41, -60, 5f),
        PL2(-38, -60, -38, -82, 3f),
        PL2(-38, -82, -35, -82, 3f),
        PL2(-42.5f, -32, -40, -32, 3f),
        PL2(-42.5f, -23, -40, -23, 3f),
        // 住宅排间巷
        PL2(-74, 60.5f, -16, 60.5f),

        // 校园广场与配套（校门 / 方院 / 生活园 / 活动中心 / 体育区）
        P("plaza", -57f, -60, w: 16f, d: 14f),
        P("plaza", -37f, -60, w: 8f, d: 18f),
        P("gate", -49.4f, -60, ry: Mathf.PI / 2f, kind: "main"),
        P("flag", -62f, -65f),
        P("sundial", -76f, -66f),
        P("board", -58f, -54.5f), P("board", -80f, -70f),
        P("planter", -64f, -54f, w: 2.4f, d: 1f, kind: "green"),
        P("planter", -64f, -66f, w: 2.4f, d: 1f, kind: "green"),
        P("planter", -85f, -54f, w: 2.4f, d: 1f, kind: "green"),
        P("planter", -85f, -66f, w: 2.4f, d: 1f, kind: "green"),
        P("planter", -99f, -43f, w: 2.4f, d: 1f, kind: "green"),
        P("bench", -68f, -66f), P("bench", -78f, -56f, ry: Mathf.PI),
        P("bench", -84f, -66f, ry: Mathf.PI / 2f),
        P("bench", -84f, -70f, ry: Mathf.PI / 2f),
        P("bench", -98f, -39f, ry: Mathf.PI / 2f),
        P("bench", -40f, -54f, ry: Mathf.PI / 2f), P("bench", -40f, -66f, ry: Mathf.PI / 2f),
        P("bench", -24f, -89f),
        P("bin", -60f, -69f), P("bin", -80f, -54f), P("bin", -40.5f, -52f),
        P("bikerack", -80f, -74.5f, n: 5), P("bikerack", -60f, -74.5f, n: 5),
        P("bikerack", -77f, -52f, n: 5), P("bikerack", -57f, -52f, ry: Mathf.PI / 2f, n: 4),
        P("bikerack", -60f, -70.5f, ry: Mathf.PI / 2f, n: 4),
        // 体育区篮球场：礼堂南侧，篮架面向场心
        P("hoop", -35, -82, ry: Mathf.PI / 2f), P("hoop", -21, -82, ry: -Mathf.PI / 2f),

        // 广场 / 站前 / 停车
        P("plaza_round", -17, -12, r: 10f),                       // 钟楼广场
        P("plaza", 76, 78, w: 26, d: 6),                          // 站前广场
        P("parking", 20, 72, w: 24, d: 16),
        P("crates", 44, 74, w: 14, d: 14),
        P("clocktower", -17, -12),
        P("railway", 0, 104, len: 236),
        P("torii", 79, -63.4f, w: 10),

        // 游乐场设施
        P("ride_carousel", 31, 33), P("ride_swing", 42, 31), P("ride_wheel", 55, 32),
        P("ride_tower", 88, 38), P("ride_train", 23, 42), P("ride_pendulum", 60, 45),
        P("ride_tent", 76, 43), P("ride_cart", 39, 50), P("ride_stage", 47, 50),
        P("kart", 72, 28, w: 14, d: 10),

        // 夏日祭摊位：两排沿参道街面对面，中间 x74~84 留给神社参道
        P("stall", 62.6f, -52.8f, kind: "takoyaki"),
        P("stall", 67.4f, -52.8f, kind: "goldfish"),
        P("stall", 72.2f, -52.8f, kind: "masks"),
        P("stall", 86.0f, -52.8f, kind: "shooting"),
        P("stall", 90.6f, -52.8f, kind: "apple"),
        P("stall", 62.6f, -39.2f, ry: Mathf.PI, kind: "yakisoba"),
        P("stall", 67.4f, -39.2f, ry: Mathf.PI, kind: "drinks"),
        P("stall", 72.2f, -39.2f, ry: Mathf.PI, kind: "ice"),
        P("stall", 86.0f, -39.2f, ry: Mathf.PI, kind: "goods"),
        P("stall", 90.6f, -39.2f, ry: Mathf.PI, kind: "water"),

        // 神社区域：石灯笼 / 提灯串 / 狛犬 / 绘马 / 御神签 / 手水 / 赛钱箱 / 酒樽 / 授与所
        P("lantern", 72.3f, -56.0f), P("lantern", 85.7f, -56.0f),
        P("lantern", 72.3f, -60.0f), P("lantern", 85.7f, -60.0f),
        P("lantern", 71.8f, -66.5f), P("lantern", 86.2f, -66.5f),
        P("komainu", 72.4f, -61.8f), P("komainu", 85.6f, -61.8f),
        P("chochin_string", 79f, -52f, len: 10f),
        P("chochin_string", 79f, -57f, len: 10f),
        P("chochin_string", 65f, -46f, ry: Mathf.PI / 2f, len: 12f),
        P("chochin_string", 93f, -46f, ry: Mathf.PI / 2f, len: 12f),
        P("nobori", 70.5f, -50.5f, kind: "red"), P("nobori", 87.5f, -50.5f, kind: "white"),
        P("nobori", 70.5f, -60.0f, kind: "white"), P("nobori", 87.5f, -60.0f, kind: "red"),
        P("temizuya", 69f, -63.4f),
        P("ema", 72.0f, -68.2f, ry: Mathf.PI / 2f),
        P("omikuji", 86.0f, -68.2f, ry: -Mathf.PI / 2f),
        P("sake", 71.2f, -74.2f),
        P("offering", 79f, -64.6f),
        P("office", 87.5f, -70.5f, ry: -Mathf.PI / 2f),
        P("festival_board", 96.5f, -46f, ry: -Mathf.PI / 2f),
        P("bench", 71.5f, -66.0f, ry: Mathf.PI / 2f), P("bench", 86.5f, -66.0f, ry: -Mathf.PI / 2f),

        // 公园喷泉（三层石盘 +「樱之冠」花雕塑地标）
        P("fountain", 34, -40, r: 4.2f),
        // 池畔四角花坛与石灯笼
        P("flowerbed", 44f, -20f, w: 5.2f, d: 2.4f), P("flowerbed", 24f, -20f, w: 5.2f, d: 2.4f),
        P("flowerbed", 44f, -62f, w: 5.2f, d: 2.4f), P("flowerbed", 24f, -62f, w: 5.2f, d: 2.4f),
        P("lantern", 50f, -22f), P("lantern", 18f, -22f),
        P("lantern", 50f, -58f), P("lantern", 18f, -58f),
        // 樱花林：中央巨樱 + 两侧石灯笼/坐凳 + 林道灯笼
        P("giant_cherry", 29f, -74.5f),
        P("lantern", 24.5f, -70.5f), P("lantern", 24.5f, -78.5f),
        P("bench", 23.5f, -74.5f, ry: Mathf.PI / 2f),
        P("bench", 33.6f, -80.2f, ry: -0.6f),
        P("lantern", 30.5f, -63.5f), P("lantern", 42f, -68f),
        P("lantern", 35.4f, -70.2f),

        // 河畔长椅：贴着滨河步道外缘，面朝河（南岸 ry=π，北岸 ry=0）
        P("bench", -30f, RiverZ(-30f) + 15.8f, ry: Mathf.PI),
        P("bench", 12f, RiverZ(12f) - 15.8f, ry: 0f),
        P("bench", -62f, RiverZ(-62f) + 15.8f, ry: Mathf.PI),
        P("bench", 48f, RiverZ(48f) + 15.8f, ry: Mathf.PI),
        P("bench", 28f, RiverZ(28f) - 15.8f, ry: 0f),
        P("bench", -34f, RiverZ(-34f) + 15.8f, ry: Mathf.PI),
        // 公园长椅（绕池塘，面朝水面）
        P("bench", 34f, -59.5f, ry: 0f), P("bench", 56.5f, -40f, ry: -Mathf.PI / 2f),
        P("bench", 34f, -20.5f, ry: Mathf.PI), P("bench", 11.5f, -40f, ry: Mathf.PI / 2f),
        P("bench", 49.2f, -54.2f, ry: -0.7f), P("bench", 49.2f, -25.8f, ry: Mathf.PI + 0.7f),
        P("bench", 18.8f, -54.2f, ry: 0.7f), P("bench", 18.8f, -25.8f, ry: Mathf.PI - 0.7f),
        // 广场/车站/主街长椅
        P("bench", -7f, -23.5f, ry: Mathf.PI), P("bench", 68f, 65.5f, ry: Mathf.PI / 2f),
        P("bench", 84f, 65.5f, ry: -Mathf.PI / 2f), P("bench", -18f, 46.5f, ry: Mathf.PI),
    };

    // ── 保留区（植被不得进入）──
    static List<(float x, float z, float hw, float hd)> KeepOut()
    {
        var k = new List<(float, float, float, float)>();
        foreach (var b in Buildings) k.Add((b.pos[0], b.pos[1], b.HalfWX + 2.8f, b.HalfWZ + 2.8f));
        foreach (var r in Roads) k.Add(r.axis == "z"
            ? (r.at, (r.from + r.to) / 2f, r.width / 2f + 2.4f, (r.to - r.from) / 2f + 2.4f)
            : ((r.from + r.to) / 2f, r.at, (r.to - r.from) / 2f + 2.4f, r.width / 2f + 2.4f));
        foreach (var g in Grounds) k.Add((g.pos[0], g.pos[1], g.size[0] / 2f + 1.6f, g.size[1] / 2f + 1.6f));
        k.Add((-35.5f, 66f, 8f, 5f));                                  // 主角家
        k.Add((-70f, -60f, 20f, 8.5f));                                // 校门主轴/方院中带
        k.Add((-57f, -60f, 9f, 8f));                                   // 校门内广场
        k.Add((-37f, -60f, 5f, 10f));                                  // 礼堂前庭
        k.Add((-63.25f, -32.5f, 12.75f, 2.5f));                        // 活动中心前路
        // 校园围墙：沿新阶梯边界分段留空（0.6m 矮墙）
        k.Add((-49.4f, -60f, 1.6f, 42f));                              // 东墙 z -102..-18
        k.Add((-67.7f, -18f, 18.3f, 1.6f));                            // 北墙东段
        k.Add((-103f, -22f, 17f, 1.6f));                               // 北墙西段
        k.Add((-86f, -20f, 1.6f, 2f));                                 // 北墙台阶
        k.Add((-120f, -33f, 1.6f, 11f));                               // 西墙 1
        k.Add((-115f, -44f, 5f, 1.6f));                                // 西墙台阶 1
        k.Add((-110f, -54f, 1.6f, 10f));                               // 西墙 2
        k.Add((-107f, -64f, 3f, 1.6f));                                // 西墙台阶 2
        k.Add((-104f, -69f, 1.6f, 5f));                                // 西墙 3
        k.Add((-100f, -74f, 4f, 1.6f));                                // 西墙台阶 3
        k.Add((-96f, -77f, 1.6f, 3f));                                 // 西墙 4
        k.Add((-92f, -80f, 4f, 1.6f));                                 // 西墙台阶 4
        k.Add((-88f, -84f, 1.6f, 4f));                                 // 西墙 5
        k.Add((-84f, -88f, 4f, 1.6f));                                 // 西墙台阶 5
        k.Add((-80f, -92f, 1.6f, 4f));                                 // 西墙 6
        k.Add((-76f, -96f, 4f, 1.6f));                                 // 西墙台阶 6
        k.Add((-72f, -99f, 1.6f, 3f));                                 // 西墙 7
        k.Add((-60.7f, -102f, 11.3f, 1.6f));                           // 南墙
        k.Add((-17f, -12f, 10.5f, 11f));                               // 钟楼广场
        k.Add((76f, 68f, 15f, 5f));                                    // 站前广场
        k.Add((20f, 72f, 14f, 10f));                                   // 停车场
        k.Add((44f, 74f, 9f, 9f));                                     // 货场
        k.Add((55f, 32f, 8f, 8f));                                     // 摩天轮
        k.Add((76f, 43f, 9f, 7f));                                     // 马戏棚
        k.Add((31f, 33f, 7f, 7f));                                     // 旋转木马
        k.Add((72f, 28f, 9f, 7f));                                     // 卡丁车
        // 骑楼柱廊前（不放植被）
        k.Add((-62f, 22f, 16f, 2.4f));
        k.Add((-60f, 32.8f, 16f, 2.4f));
        k.Add((-25f, 32.8f, 15f, 2.4f));
        // 神社前庭 / 鸟居 / 石阶
        k.Add((79f, -57f, 12f, 15f));   // 神社前庭+参道口整体留空（含参道北侧）
        // 游乐场核心（游乐设施之间保持空地）
        k.Add((51f, 37f, 43f, 15f));
        // 铁路道床
        k.Add((0f, 92f, 105f, 3.0f));
        // 站前广场
        k.Add((76f, 68f, 14f, 4f));
        // 三座桥面（含引桥）
        k.Add((0f, RiverZ(0f), 6.5f, 10f));
        k.Add((-45f, RiverZ(-45f), 3.5f, 10f));
        k.Add((28f, RiverZ(28f), 4.2f, 10f));
        // 夏日祭摊位底部
        foreach (var p in Props)
            if (p.type == "stall") k.Add((p.x, p.z, 1.5f, 1.5f));
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

    // ── 校园围墙：扩至北侧河岸与西侧圆边界，阶梯收分；东墙留正门与东侧门 ──
    static FenceDef[] BuildSchoolWalls()
    {
        var runs = new List<FenceDef>();
        void W(string axis, float at, float from, float to)
        {
            if (to - from > 1.2f) runs.Add(new FenceDef { axis = axis, at = at, from = from, to = to });
        }
        // 东墙（沿中央街西侧）：正门 z -65..-55，东侧门 z -44..-34
        W("z", -49.4f, -102f, -65f);
        W("z", -49.4f, -55f, -44f);
        W("z", -49.4f, -34f, -18f);
        // 北墙：西段 z=-22，东段 z=-18（临河退让）
        W("x", -18f, -86f, -49.4f);
        W("x", -22f, -120f, -86f);
        W("z", -86f, -22f, -18f);
        // 西墙：随世界圆边界阶梯收分
        W("z", -120f, -44f, -22f);
        W("x", -44f, -120f, -110f);
        W("z", -110f, -64f, -44f);
        W("x", -64f, -110f, -104f);
        W("z", -104f, -74f, -64f);
        W("x", -74f, -104f, -96f);
        W("z", -96f, -80f, -74f);
        W("x", -80f, -96f, -88f);
        W("z", -88f, -88f, -80f);
        W("x", -88f, -88f, -80f);
        W("z", -80f, -96f, -88f);
        W("x", -96f, -80f, -72f);
        W("z", -72f, -102f, -96f);
        // 南墙
        W("x", -102f, -72f, -49.4f);
        return runs.ToArray();
    }

    static bool NearBuilding(float x, float z, float margin)
    {
        foreach (var b in Buildings)
            if (Mathf.Abs(x - b.pos[0]) < b.HalfWX + margin && Mathf.Abs(z - b.pos[1]) < b.HalfWZ + margin)
                return true;
        if (Mathf.Abs(x + 35.5f) < 6f + margin && Mathf.Abs(z - 66f) < 3f + margin) return true;
        return false;
    }

    static bool InKeep(float x, float z, List<(float x, float z, float hw, float hd)> keep, bool river = true)
    {
        if (river && Mathf.Abs(z - RiverZ(x)) < 14.5f) return true;                  // 河道 + 护岸
        float pdx = (x - 34f) / 21f, pdz = (z + 40f) / 15f;
        if (pdx * pdx + pdz * pdz < 1.25f) return true;                              // 池塘
        // 环塘步道（Pond() 里半径 rx+4.3 / rz+4.3、宽 2.2m）：植被留出通道
        float rdx = (x - 34f) / 24.3f, rdz = (z + 40f) / 18.3f;
        float rd = Mathf.Sqrt(rdx * rdx + rdz * rdz);
        if (Mathf.Abs(rd - 1f) < 0.12f) return true;
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
            GROUNDS = Grounds,
            PROPS = Props,
            POND = new PondDef { pos = new[] { 34f, -40f }, r = 20f },
            VILLA = new VillaDef { x = -35.5f, z = 66f, w = 12f, h = 6f, d = 6f, face = 1 },
            FENCES = BuildSchoolWalls(),
            DISTANT_BUILDINGS = new DistantDef[0],
            ROCKERIES = new RockeryItem[0],
            REEDS = new ReedItem[0],
            DIRT_PATCHES = new PosItem[0],
            SMALL_ROCKS = new PosItem[0],
            RIVER = new RiverDef
            {
                width = 16f, amp = 10f, freq = 34f, phase = 40f, offset = 2f, from = -310f, to = 310f,
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
            if (x * x + z * z > (BOUND - 3f) * (BOUND - 3f)) return;   // 圆形边界内才种（外侧是抬升的缓丘）
            if (InKeep(x, z, keep)) return;
            foreach (var t in trees)
                if (Mathf.Sqrt((x - t.x) * (x - t.x) + (z - t.z) * (z - t.z)) < gap) return;
            trees.Add(new TreeItem { x = R1(x), z = R1(z), v = v });
        }

        // 樱花林（公园北侧 x 8~66 / z -96~-68）：范围扩到原林区边界外，中央留 16m 空地给巨樱。
        // 用错排抖动网格而不是纯随机散点，保证树距均匀、不结块
        const float gx0 = 8f, gx1 = 65f, gz0 = -96f, gz1 = -68.8f, step = 6.8f;
        for (int row = 0; row * step + gz0 <= gz1 + 0.01f; row++)
        {
            float z0 = gz0 + row * step;
            float xOff = (row % 2) * step * 0.5f;
            for (float x0 = gx0 + xOff; x0 <= gx1; x0 += step)
            {
                float x = x0 + ((float)rnd.Next() * 2 - 1) * 2.0f;
                float z = z0 + ((float)rnd.Next() * 2 - 1) * 1.8f;
                if ((x - 29f) * (x - 29f) + (z + 74.5f) * (z + 74.5f) < 16.0f * 16.0f) continue;
                float roll = (float)rnd.Next();
                int v = roll < 0.45f ? 10 : roll < 0.68f ? 12 : roll < 0.88f ? 11 : 13;   // 粉/八重/浅粉/垂枝
                TryTree(x, z, v, 6.2f);
            }
        }
        // 少量常绿与秋色树压在新林地边缘，给樱花林做背景
        foreach (var (tx, tz, tv) in new[] { (10f, -72f, 0), (12f, -92f, 2), (62f, -92f, 0), (64f, -70f, 2), (48f, -94f, 5), (26f, -94f, 0), (8f, -86f, 1), (66f, -82f, 0) })
            TryTree(tx, tz, tv, 6.2f);
        // 公园点景树（围绕池塘；同样走保留区检查，避免踩到池沿/步道）
        foreach (var (tx, tz) in new[] { (20f, -52f), (28f, -55f), (44f, -54f), (52f, -48f), (54f, -32f), (16f, -40f), (16f, -28f), (48f, -56f) })
            TryTree(tx, tz, 0, 3.5f);
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
        // 校园点景树（教学西侧 / 图书馆南与西 / 生活园 / 方院外围）
        foreach (var (tx, tz, tv) in new[]
        {
            (-88f, -78f, 0), (-100f, -76f, 2), (-107.5f, -52f, 5),
            (-84f, -26f, 0), (-94f, -40f, 2), (-58f, -73f, 0), (-80f, -72f, 5),
            (-76f, -70f, 2), (-90f, -76f, 0),
        }) TryTree(tx, tz, tv, 4.5f);
        // 全镇散布
        for (int i = 0; i < 90; i++)
        {
            float x = (float)(rnd.Next() * 2 - 1) * 92f;
            float z = (float)(rnd.Next() * 2 - 1) * 92f;
            TryTree(x, z, rnd.NextInt(9), 4.5f);
        }
        // 最终过滤：神社前庭、建筑近旁、桥面一律不保留（不论树来自哪一批）
        var keptTrees = new List<TreeItem>();
        foreach (var t in trees)
        {
            if (Mathf.Abs(t.x - 79f) < 12f && Mathf.Abs(t.z + 57f) < 15f) continue;
            bool bad = false;
            foreach (var b in Buildings)
                if (Mathf.Abs(t.x - b.pos[0]) < b.HalfWX + 2.6f && Mathf.Abs(t.z - b.pos[1]) < b.HalfWZ + 2.6f) { bad = true; break; }
            if (!bad && Mathf.Abs(t.x + 35.5f) < 8.6f && Mathf.Abs(t.z - 66f) < 5.6f) bad = true;
            if (!bad)
                foreach (var br in w.RIVER.bridges)
                    if (Mathf.Abs(t.x - br.x) < br.width / 2f + 3f &&
                        Mathf.Abs(t.z - RiverZ(br.x)) < (w.RIVER.width + 1.6f) / 2f + 3f) { bad = true; break; }
            if (!bad && (t.x - 29f) * (t.x - 29f) + (t.z + 74.5f) * (t.z + 74.5f) < 16.0f * 16.0f) bad = true;   // 巨樱空地
            if (!bad)
                foreach (var p in Props)
                {
                    if (p.type != "flowerbed" && p.type != "lantern" && p.type != "bench") continue;
                    float hw = p.type == "flowerbed" ? 3.6f : p.type == "bench" ? 2.2f : 1.8f;
                    float hd = p.type == "flowerbed" ? 2.4f : 1.8f;
                    if (Mathf.Abs(t.x - p.x) < hw && Mathf.Abs(t.z - p.z) < hd) { bad = true; break; }
                }
            if (!bad) keptTrees.Add(t);
        }
        w.TREES = keptTrees.ToArray();

        // 灌木 / 花：成簇分布（避免沿建筑立面排成一条线），离建筑更远
        var bushes = new List<PosItem>();
        for (int c = 0; c < 200 && bushes.Count < 110; c++)
        {
            float cx = (float)(rnd.Next() * 2 - 1) * 92f, cz = (float)(rnd.Next() * 2 - 1) * 92f;
            if (InKeep(cx, cz, keep, false)) continue;
            if (Mathf.Abs(cz - RiverZ(cx)) < 15.5f) continue;
            if (NearBuilding(cx, cz, 5.5f)) continue;
            // 樱花林下改用杜鹃/花丛做地被，方块灌木不再进入林区（近看像绿箱子）
            if (cx > 5f && cx < 70f && cz > -98f && cz < -64f) continue;
            bool near = false;
            foreach (var t in trees) if (Mathf.Sqrt((cx - t.x) * (cx - t.x) + (cz - t.z) * (cz - t.z)) < 2.4f) { near = true; break; }
            if (near) continue;
            int n = 2 + rnd.NextInt(2);
            for (int k = 0; k < n && bushes.Count < 110; k++)
            {
                float x = cx + (float)(rnd.Next() * 2 - 1) * 1.7f;
                float z = cz + (float)(rnd.Next() * 2 - 1) * 1.7f;
                if (InKeep(x, z, keep, false)) continue;
                if (Mathf.Abs(z - RiverZ(x)) < 15.5f) continue;
                if (NearBuilding(x, z, 5.0f)) continue;
                if (x > 5f && x < 70f && z > -98f && z < -64f) continue;
                bool bad = false;
                foreach (var p in bushes) if (Mathf.Sqrt((x - p.x) * (x - p.x) + (z - p.z) * (z - p.z)) < 0.9f) { bad = true; break; }
                if (bad) continue;
                bushes.Add(new PosItem { x = R1(x), z = R1(z) });
            }
        }
        w.BUSHES = bushes.ToArray();

        bool NearParkProp(float x, float z, float d)
        {
            foreach (var p in Props)
            {
                if (p.type != "flowerbed" && p.type != "lantern" && p.type != "bench" && p.type != "giant_cherry") continue;
                if (Mathf.Abs(x - p.x) < d && Mathf.Abs(z - p.z) < d) return true;
            }
            return false;
        }

        // 杜鹃：樱花林下成片，公园池畔外圈再补几丛（避开步道、花坛、灯笼、长椅）
        var azaleas = new List<PosItem>();
        for (int c = 0; c < 150 && azaleas.Count < 54; c++)
        {
            float cx = 8f + (float)rnd.Next() * 58f;
            float cz = -96f + (float)rnd.Next() * 28f;
            if (InKeep(cx, cz, keep, false)) continue;
            if ((cx - 29f) * (cx - 29f) + (cz + 74.5f) * (cz + 74.5f) < 5.0f * 5.0f) continue;
            if (NearParkProp(cx, cz, 2.2f)) continue;
            int n = 2 + rnd.NextInt(2);
            for (int k = 0; k < n && azaleas.Count < 54; k++)
            {
                float x = cx + ((float)rnd.Next() * 2 - 1) * 1.2f;
                float z = cz + ((float)rnd.Next() * 2 - 1) * 1.2f;
                if (InKeep(x, z, keep, false)) continue;
                if ((x - 29f) * (x - 29f) + (z + 74.5f) * (z + 74.5f) < 4.5f * 4.5f) continue;
                if (NearParkProp(x, z, 1.8f)) continue;
                azaleas.Add(new PosItem { x = R1(x), z = R1(z) });
            }
        }
        for (int c = 0; c < 90 && azaleas.Count < 76; c++)
        {
            float a = (float)rnd.Next() * Mathf.PI * 2f;
            float x = 34f + Mathf.Cos(a) * (26f + (float)rnd.Next() * 5f);
            float z = -40f + Mathf.Sin(a) * (20f + (float)rnd.Next() * 3f);
            if (InKeep(x, z, keep, false)) continue;
            if (NearParkProp(x, z, 2.2f)) continue;
            azaleas.Add(new PosItem { x = R1(x), z = R1(z) });
        }
        w.AZALEAS = azaleas.ToArray();

        var flowers = new List<PosItem>();
        for (int c = 0; c < 260 && flowers.Count < 150; c++)
        {
            float cx = (float)(rnd.Next() * 2 - 1) * 92f, cz = (float)(rnd.Next() * 2 - 1) * 92f;
            if (InKeep(cx, cz, keep, false)) continue;
            if (Mathf.Abs(cz - RiverZ(cx)) < 15.5f) continue;
            if (NearBuilding(cx, cz, 4.5f)) continue;
            int n = 3 + rnd.NextInt(3);
            for (int k = 0; k < n && flowers.Count < 150; k++)
            {
                float x = cx + (float)(rnd.Next() * 2 - 1) * 1.2f;
                float z = cz + (float)(rnd.Next() * 2 - 1) * 1.2f;
                if (InKeep(x, z, keep, false)) continue;
                if (Mathf.Abs(z - RiverZ(x)) < 15.5f) continue;
                if (NearBuilding(x, z, 4.0f)) continue;
                bool bad = false;
                foreach (var p in bushes) if (Mathf.Sqrt((x - p.x) * (x - p.x) + (z - p.z) * (z - p.z)) < 1.2f) { bad = true; break; }
                if (bad) continue;
                foreach (var p in flowers) if (Mathf.Sqrt((x - p.x) * (x - p.x) + (z - p.z) * (z - p.z)) < 0.7f) { bad = true; break; }
                if (bad) continue;
                flowers.Add(new PosItem { x = R1(x), z = R1(z) });
            }
        }
        // 樱花林与公园草坪的定向花丛（补充林下色彩，避开巨樱根盘与步道）
        for (int i = 0; i < 60 && flowers.Count < 210; i++)
        {
            float x = 8f + (float)rnd.Next() * 58f;
            float z = -96f + (float)rnd.Next() * 28f;
            if (InKeep(x, z, keep, false)) continue;
            if ((x - 29f) * (x - 29f) + (z + 74.5f) * (z + 74.5f) < 4.8f * 4.8f) continue;
            if (NearParkProp(x, z, 1.2f)) continue;
            flowers.Add(new PosItem { x = R1(x), z = R1(z) });
        }
        for (int i = 0; i < 24 && flowers.Count < 234; i++)
        {
            float a = (float)rnd.Next() * Mathf.PI * 2f;
            float x = 34f + Mathf.Cos(a) * (26f + (float)rnd.Next() * 6f);
            float z = -40f + Mathf.Sin(a) * (20f + (float)rnd.Next() * 4f);
            if (InKeep(x, z, keep, false)) continue;
            if (NearParkProp(x, z, 1.2f)) continue;
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

        // 路灯：主街/中央街/老街/住宅/参道/神社/车站/站前/公园/滨河两岸
        var lamps = new List<LampItem>();
        void L(float x, float z, float r) => lamps.Add(new LampItem { x = x, z = z, r = r });
        // 主街两侧
        for (float z = -84f; z <= 90f; z += 14f)
        {
            if (Mathf.Abs(z - RiverZ(0)) < 14f) continue;
            L(-6.4f, z, 0f); L(6.4f, z, Mathf.PI);
        }
        // 中央街两侧
        for (float z = -88f; z <= 94f; z += 22f)
        {
            if (Mathf.Abs(z - RiverZ(-45f)) < 12f) continue;
            L(-48.9f, z, 0f); L(-41.1f, z, Mathf.PI);
        }
        // 老街商街两侧
        foreach (float x in new[] { -70f, -50f, -30f, -12f })
        {
            L(x, 22.3f, -Mathf.PI / 2f);
            L(x, 31.7f, Mathf.PI / 2f);
        }
        // 住宅北街/中街
        for (float x = -70f; x <= -20f; x += 16f)
        {
            L(x, 46.2f, -Mathf.PI / 2f); L(x, 51.8f, Mathf.PI / 2f);
            L(x, 69.2f, -Mathf.PI / 2f); L(x, 74.8f, Mathf.PI / 2f);
        }
        // 神社参道：不再用现代路灯，改由石灯笼/提灯串（Props）照明
        // 校园：主轴、生活街、活动中心、礼堂前庭、篮球场
        L(-58.5f, -63.5f, 0f); L(-50.6f, -56.5f, 0f);
        L(-70f, -63.5f, 0f); L(-70f, -56.5f, 0f);
        L(-86f, -63.5f, 0f); L(-86f, -56.5f, 0f);
        L(-99.8f, -27f, -Mathf.PI / 2f); L(-99.8f, -41f, -Mathf.PI / 2f);
        L(-98f, -43.8f, 0f); L(-88f, -43.8f, 0f);
        L(-50.6f, -38f, Mathf.PI);
        L(-41.5f, -54f, 0f); L(-41.5f, -66f, 0f);
        L(-36f, -70.5f, 0f); L(-36f, -81.5f, 0f);
        L(-38f, -44f, 0f); L(-18f, -44f, 0f);
        L(-66f, -70.5f, 0f);
        // 车站街两侧
        for (float x = 14f; x <= 82f; x += 18f)
        {
            L(x, 53.5f, -Mathf.PI / 2f);
            L(x, 62.5f, Mathf.PI / 2f);
        }
        // 站前广场
        L(70f, 78f, Mathf.PI / 2f); L(82f, 78f, Mathf.PI / 2f);
        // 公园（池塘四角；南北两盏略偏离轴线，避免挡住喷泉地标）
        L(28f, -56f, Mathf.PI / 2f); L(40f, -23.5f, -Mathf.PI / 2f);
        L(14f, -40f, 0f); L(54f, -40f, Mathf.PI);
        // 钟楼广场（贴新圆盘内缘，避开塔基）
        L(-10.5f, -19f, -Mathf.PI / 2f); L(-23.5f, -19f, -Mathf.PI / 2f);
        // 滨河两岸
        for (float x = -90f; x <= 90f; x += 22f)
        {
            L(x, RiverZ(x) + 15.7f, -Mathf.PI / 2f);
            L(x, RiverZ(x) - 15.7f, Mathf.PI / 2f);
        }
        // 路灯不得立在车行道上：两轮把压路的灯推到路缘外
        for (int pass = 0; pass < 2; pass++)
            foreach (var l in lamps)
                foreach (var r in Roads)
                {
                    if (r.axis == "z")
                    {
                        if (l.z < r.from - 0.5f || l.z > r.to + 0.5f) continue;
                        if (Mathf.Abs(l.x - r.at) >= r.width / 2f + 0.4f) continue;
                        float side = l.x >= r.at ? 1f : -1f;
                        l.x = r.at + side * (r.width / 2f + 0.55f);
                    }
                    else
                    {
                        if (l.x < r.from - 0.5f || l.x > r.to + 0.5f) continue;
                        if (Mathf.Abs(l.z - r.at) >= r.width / 2f + 0.4f) continue;
                        float side = l.z >= r.at ? 1f : -1f;
                        l.z = r.at + side * (r.width / 2f + 0.55f);
                    }
                }
        // 不在建筑门口放路灯（门前 2.8m 内）
        lamps.RemoveAll(l =>
        {
            foreach (var b in Buildings)
            {
                float dx = b.pos[0] + b.FrontOffsetX(0.9f);
                float dz = b.pos[1] + b.FrontOffsetZ(0.9f);
                if (Mathf.Abs(l.x - dx) < 2.8f && Mathf.Abs(l.z - dz) < 2.8f) return true;
            }
            if (Mathf.Abs(l.x + 35.5f) < 3f && Mathf.Abs(l.z - 69f) < 2.6f) return true;
            return false;
        });
        w.LAMPS = lamps.ToArray();

        // 边界林带
        var bt = new List<TreeItem>();
        for (int i = 0; i < 96; i++)
        {
            float a = i / 96f * Mathf.PI * 2f;
            float r = 116f + (float)rnd.Next() * 6f;
            bt.Add(new TreeItem { x = R1(Mathf.Cos(a) * r), z = R1(Mathf.Sin(a) * r), v = 6 + rnd.NextInt(3) });
        }
        var btKept = new List<TreeItem>();
        foreach (var t in bt)
        {
            if (Mathf.Abs(t.z - RiverZ(t.x)) < 12f) continue;
            if (Mathf.Abs(t.x - 79f) < 12f && Mathf.Abs(t.z + 57f) < 15f) continue;
            if (t.x < -48f && t.x > -101f && t.z < -24f && t.z > -101f) continue;   // 校园范围内不落边界林
            btKept.Add(t);
        }
        w.BORDER_TREES = btKept.ToArray();
        w.BORDER_BUSHES = new PosItem[0];
        w.BORDER_ROCKS = new PosItem[0];

        // 交互点
        var spots = new List<SpotDef>();
        foreach (var b in Buildings)
            spots.Add(new SpotDef
            {
                name = b.name, x = b.pos[0] + b.FrontOffsetX(1.2f), z = b.pos[1] + b.FrontOffsetZ(1.2f),
                r = Mathf.Clamp(b.size[0] / 2f, 2.2f, 4f)
            });
        spots.Add(new SpotDef { name = "家", x = -35.5f, z = 66f + 3f + 1.2f, r = 3f });
        spots.Add(new SpotDef { name = "操场", x = -28f, z = -32f, r = 3f });
        spots.Add(new SpotDef { name = "篮球场", x = -28f, z = -82f, r = 2.5f });
        spots.Add(new SpotDef { name = "钟楼广场", x = -17f, z = -5f, r = 3.5f });
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
            bl.Add(new BlockerDef { x = b.pos[0], z = b.pos[1], hw = b.HalfWX, hd = b.HalfWZ });
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
                case "bench":
                    {
                        bool rot = Mathf.Abs(Mathf.Sin(p.ry)) > 0.7f;
                        bl.Add(new BlockerDef { x = p.x, z = p.z, hw = rot ? 0.32f : 0.9f, hd = rot ? 0.9f : 0.32f });
                    }
                    break;
                case "bin": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 0.3f, hd = 0.3f }); break;
                case "board":
                    {
                        bool rot = Mathf.Abs(Mathf.Sin(p.ry)) > 0.7f;
                        bl.Add(new BlockerDef { x = p.x, z = p.z, hw = rot ? 0.22f : 0.95f, hd = rot ? 0.95f : 0.22f });
                    }
                    break;
                case "bikerack":
                    {
                        bool rot = Mathf.Abs(Mathf.Sin(p.ry)) > 0.7f;
                        float bw = (p.n > 0 ? p.n : 5) * 0.35f;
                        bl.Add(new BlockerDef { x = p.x, z = p.z, hw = rot ? 0.5f : bw, hd = rot ? bw : 0.5f });
                    }
                    break;
                case "planter":
                    {
                        bool rot = Mathf.Abs(Mathf.Sin(p.ry)) > 0.7f;
                        bl.Add(new BlockerDef { x = p.x, z = p.z, hw = (rot ? p.d : p.w) / 2f, hd = (rot ? p.w : p.d) / 2f });
                    }
                    break;
                case "flag": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 0.35f, hd = 0.35f }); break;
                case "hoop": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 0.25f, hd = 0.25f }); break;
                case "sundial": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 0.8f, hd = 0.8f }); break;
                case "gate":
                    {
                        float px = Mathf.Cos(p.ry), pz = -Mathf.Sin(p.ry);
                        float off = p.kind == "main" ? 3.6f : 4.2f;
                        float ph = p.kind == "main" ? 0.55f : 0.5f;
                        bl.Add(new BlockerDef { x = p.x + px * off, z = p.z + pz * off, hw = ph, hd = ph });
                        bl.Add(new BlockerDef { x = p.x - px * off, z = p.z - pz * off, hw = ph, hd = ph });
                    }
                    break;
                case "lantern": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 0.4f, hd = 0.4f }); break;
                case "komainu": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 0.55f, hd = 0.55f }); break;
                case "ema": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 1.3f, hd = 1.3f }); break;
                case "omikuji": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 0.6f, hd = 0.6f }); break;
                case "office": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 1.5f, hd = 1.5f }); break;
                case "temizuya": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 1.0f, hd = 0.8f }); break;
                case "sake": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 0.8f, hd = 0.8f }); break;
                case "offering": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 1.2f, hd = 0.6f }); break;
                case "festival_board": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 1.3f, hd = 0.5f }); break;
                case "chochin_string":
                    {
                        float hf = p.len * 0.5f;
                        float cs = Mathf.Cos(p.ry), sn = Mathf.Sin(p.ry);
                        bl.Add(new BlockerDef { x = p.x + hf * cs, z = p.z - hf * sn, hw = 0.18f, hd = 0.18f });
                        bl.Add(new BlockerDef { x = p.x - hf * cs, z = p.z + hf * sn, hw = 0.18f, hd = 0.18f });
                    }
                    break;
                case "fountain": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = p.r, hd = p.r }); break;
                case "giant_cherry": bl.Add(new BlockerDef { x = p.x, z = p.z, hw = 1.4f, hd = 1.4f }); break;
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
