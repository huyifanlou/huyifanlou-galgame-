using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 校园世界数据（C# 权威版，2026-10-16 由 tools/source/campus.js 整体移植）
// 目标：全项目单一语言——数据/校验/渲染/构建不再依赖 Node/JS。
// 布点算法与原 JS 完全一致（同种子同一布局）；改布点后运行菜单「Campus/校验世界布点」。
public static class CampusData
{
    public const float BOUND = 60f;
    public const float GROUND_SIZE = 280f;

    // —— 确定性随机（与原 JS lcgRand 位级一致：uint 溢出回绕 + double 精度）——
    class Lcg
    {
        uint s;
        public Lcg(uint seed) { s = seed; }
        public double Next() { unchecked { s = s * 1664525u + 1013904223u; } return s / 4294967296.0; }
        public int NextInt(int max) { return (int)Math.Floor(Next() * max); }
    }
    static float R1(double v) => (float)(Math.Floor(v * 10.0 + 0.5) / 10.0);   // JS Math.round(v*10)/10

    // ── 路网（v4：全部两侧矮墙；三种路面）──
    static RoadDef R(string id, string axis, float at, float from, float to, float width, string kind)
        => new RoadDef { id = id, axis = axis, at = at, from = from, to = to, width = width, kind = kind, fence = true };

    static readonly RoadDef[] Roads =
    {
        R("vmain", "z", 0f, -17.5f, 38f, 4.6f, "mainstone"),
        R("vwest", "z", -34f, -30f, 44f, 4.0f, "slab"),
        R("veast", "z", 30f, -30f, 44f, 4.0f, "slab"),
        R("hnorth", "x", -30f, -34f, 34f, 4.6f, "slab"),
        R("hmain", "x", 0f, -58f, 58f, 4.6f, "mainstone"),
        R("hsouth", "x", 18f, -58f, 58f, 4.0f, "slab"),
        R("sring", "x", 44f, -34f, 34f, 4.0f, "cobble"),
    };

    // ── 岔路 ──
    static BranchPathDef Bp(float x, float z, float len) => new BranchPathDef { x = x, z = z, len = len };
    static readonly BranchPathDef[] Branches =
    {
        Bp(-16f, -5.4f, 6.2f), Bp(14f, -4.6f, 4.6f), Bp(20f, 4.6f, 4.6f), Bp(46f, -6.4f, 8.6f), Bp(-46f, -6.4f, 8.2f),
        Bp(44f, 14f, 4f), Bp(-46f, 14f, 4f), Bp(-15f, 21.4f, 3.2f), Bp(-20f, -34.4f, 4f), Bp(16f, -29.7f, 4.6f),
        Bp(-24f, -29.7f, 4.6f), Bp(-30.5f, 8.5f, 6f), Bp(-30.5f, -23f, 6f), Bp(26f, 10f, 6f), Bp(0f, 42f, 3f),
    };

    // ── 建筑 ──
    static BuildingDef Bd(string id, string name, float x, float z, float w, float h, float d, string wall, string trim, string plinth, string roof,
        int cols, int rows, int face, string style, bool wing = false, bool antenna = false, bool banner = false, bool roofProps = false, bool tank = false, bool awning = false, bool vent = false)
        => new BuildingDef { id = id, name = name, pos = new[] { x, z }, size = new[] { w, h, d }, wall = wall, trim = trim, plinth = plinth, roof = roof, cols = cols, rows = rows, face = face, style = style, wing = wing, antenna = antenna, banner = banner, roofProps = roofProps, tank = tank, awning = awning, vent = vent };

    static readonly BuildingDef[] Buildings =
    {
        Bd("teach", "教学楼", -16, -12, 10, 5, 6, "#d8c9a6", "#f2ecdf", "#9a8a70", "#7a4a3a", 6, 4, 1, "flat", wing: true, antenna: true, banner: true, roofProps: true),
        Bd("dorm", "宿舍楼", 14, -10, 8, 5.4f, 5, "#c98d6b", "#f0e2d0", "#8a6a52", "#46536b", 5, 4, 1, "gable", tank: true, banner: true),
        Bd("library", "图书馆", 0, -22, 12, 4.2f, 6, "#c9c3b6", "#e9e5da", "#8f8a80", "#3f5a4a", 7, 3, 1, "grand"),
        Bd("canteen", "食堂", 20, 10, 9, 3.4f, 5, "#e0b34e", "#fff3d6", "#a07838", "#a03c30", 5, 2, -1, "gable", awning: true, vent: true),
        Bd("gym", "体育馆", -20, -42, 16, 5.2f, 11, "#cfc9bd", "#eeeae0", "#8f8a80", "#4a5a6e", 8, 3, 1, "gym", banner: true),
        Bd("dorm2", "二号宿舍", 16, -24, 9, 5.4f, 5, "#bfa88c", "#efe6d6", "#87755c", "#5a4a42", 5, 4, -1, "gable", tank: true, banner: true),
        Bd("labs", "实验楼", -24, -23, 10, 5, 6, "#c3c8cf", "#eef1f4", "#828a94", "#3d4654", 5, 4, -1, "flat", roofProps: true, banner: true),
        Bd("center", "活动中心", 46, -14, 13, 6, 9, "#d3c2a2", "#f2ead8", "#94805e", "#7c4a38", 7, 4, 1, "grand"),
        Bd("arts", "艺术楼", 44, 9, 10, 5, 6, "#e0d5c3", "#f6f0e4", "#9c8f78", "#4e6a58", 5, 3, 1, "gable", awning: true),
        Bd("service", "生活服务楼", -46, 9, 11, 4.5f, 6, "#cbb894", "#f0e8d6", "#8d7a58", "#a05a3c", 6, 3, 1, "flat", roofProps: true),
        Bd("kiosk", "小卖部", -15, 25, 6, 3.2f, 4, "#e8c46a", "#fff6e0", "#a8843c", "#b8452f", 3, 2, -1, "gable", awning: true),
        Bd("guardN", "值班室", 57, 7, 4, 3, 4, "#d8d2c4", "#f2eee4", "#98917f", "#4a5568", 2, 2, -1, "flat"),
    };

    static readonly GroundDef[] Grounds =
    {
        new GroundDef { id = "playground", name = "操场", pos = new[] { -21f, 8.5f }, size = new[] { 16f, 10f } },
        new GroundDef { id = "basket", name = "篮球场", kind = "basket", pos = new[] { -2f, -44f }, size = new[] { 15f, 9f } },
        new GroundDef { id = "tennis", name = "网球场", kind = "tennis", pos = new[] { -40f, -42f }, size = new[] { 15f, 8f } },
    };

    static readonly float[] PondPos = { -26.5f, -14f };
    const float PondR = 3f;

    // ── 设施道具 ──
    static PropDef P(string type, float x, float z, float ry = 0f, string kind = null, float w = 0f, float d = 0f, float r = 0f, float len = 0f, int n = 0)
        => new PropDef { type = type, x = x, z = z, ry = ry, kind = kind, w = w, d = d, r = r, len = len, n = n };

    static readonly PropDef[] Props =
    {
        // 中心广场 + 旗杆 + 公告栏
        P("plaza", -7f, -6f, w: 6.5f, d: 5f), P("flag", -7f, -6f), P("board", -7f, -3.6f, ry: Mathf.PI),
        // 校门：东正门 / 西侧门
        P("gate", 58f, 0f, ry: -Mathf.PI / 2f, kind: "main"), P("gate", -58f, 0f, ry: Mathf.PI / 2f, kind: "side"),
        // 长椅
        P("bench", -26.5f, -7.4f, ry: Mathf.PI), P("bench", -22.8f, -14f, ry: -Mathf.PI / 2f), P("bench", -30.2f, -14f, ry: Mathf.PI / 2f),
        P("bench", 10f, 4.6f, ry: Mathf.PI), P("bench", -12f, 4.6f, ry: Mathf.PI), P("bench", 24f, 4.6f, ry: Mathf.PI),
        P("bench", -26f, 22.6f), P("bench", -22f, 31f, ry: Mathf.PI), P("bench", -2f, -50.5f, ry: Mathf.PI), P("bench", -40f, -48.5f, ry: Mathf.PI),
        // 垃圾桶
        P("bin", 11.5f, -4.6f), P("bin", -9.5f, -4.6f), P("bin", -22.5f, -4.6f), P("bin", -24.5f, -6.2f), P("bin", 4.6f, -4.6f), P("bin", -7.6f, -8.6f), P("bin", -22.5f, 21.5f),
        // 公告栏
        P("board", -11.5f, -4.4f, ry: Mathf.PI / 2f), P("board", 9f, -4.4f, ry: -Mathf.PI / 2f),
        // 自行车架
        P("bikerack", -12f, -6.8f, n: 5), P("bikerack", 5.4f, -6.2f, n: 5),
        // 花坛
        P("planter", -12.5f, 21.6f, w: 3.6f, d: 1.4f), P("planter", -17.5f, 21.6f, w: 3.6f, d: 1.4f),
        // 喷泉
        P("fountain", -26f, 26.5f, r: 2.2f),
        // 篮球架 / 网球网
        P("hoop", -8.5f, -44f, ry: Mathf.PI / 2f), P("hoop", 4.5f, -44f, ry: -Mathf.PI / 2f),
        P("netpost", -40f, -42f, len: 8f),
    };

    // ── 保留区（植被不得进入）──
    static List<(float x, float z, float hw, float hd)> KeepOut()
    {
        var k = new List<(float, float, float, float)>();
        foreach (var b in Buildings) k.Add((b.pos[0], b.pos[1], b.size[0] / 2f + 2.4f, b.size[2] / 2f + 2.4f));
        foreach (var r in Roads) k.Add(r.axis == "z"
            ? (r.at, (r.from + r.to) / 2f, r.width / 2f + 3.0f, (r.to - r.from) / 2f + 3.0f)
            : ((r.from + r.to) / 2f, r.at, (r.to - r.from) / 2f + 3.0f, r.width / 2f + 3.0f));
        foreach (var g in Grounds) k.Add((g.pos[0], g.pos[1], g.size[0] / 2f + 1.6f, g.size[1] / 2f + 1.6f));
        foreach (var p in Branches) k.Add((p.x, p.z, 2.6f, p.len / 2f + 1.2f));
        foreach (var p in Props) k.Add((p.x, p.z, 2.2f, 2.2f));
        k.Add((-26f, 27f, 8f, 7f));                        // 西南花园
        k.Add((0f, 34f, 4.2f + 2.4f, 3.5f + 5.4f));        // 主角家及前院
        return k;
    }

    static bool InKeep(float x, float z, List<(float x, float z, float hw, float hd)> keep)
    {
        if (Mathf.Sqrt((x + 26.5f) * (x + 26.5f) + (z + 14f) * (z + 14f)) < PondR + 1.8f) return true;   // 池塘
        if (Mathf.Sqrt(x * x + (z - 4f) * (z - 4f)) < 3.6f) return true;                                 // 出生点
        foreach (var k in keep)
            if (Mathf.Abs(x - k.x) < k.hw && Mathf.Abs(z - k.z) < k.hd) return true;
        return false;
    }

    static List<float[]> Scatter(int count, Lcg rnd, float bound, float minGap, List<float[]> others, List<(float, float, float, float)> keep)
    {
        var outp = new List<float[]>();
        int guard = 0;
        while (outp.Count < count && guard++ < count * 120)
        {
            float x = (float)(rnd.Next() * 2 - 1) * bound;
            float z = (float)(rnd.Next() * 2 - 1) * bound;
            if (InKeep(x, z, keep)) continue;
            bool ok = true;
            foreach (var p in others) if (Mathf.Sqrt((x - p[0]) * (x - p[0]) + (z - p[1]) * (z - p[1])) < minGap) { ok = false; break; }
            if (ok) foreach (var p in outp) if (Mathf.Sqrt((x - p[0]) * (x - p[0]) + (z - p[1]) * (z - p[1])) < minGap) { ok = false; break; }
            if (ok) outp.Add(new[] { R1(x), R1(z) });
        }
        return outp;
    }

    // ── 生成完整世界数据 ──
    public static WorldData Build()
    {
        var keep = KeepOut();
        var w = new WorldData
        {
            BOUND = BOUND,
            GROUND_SIZE = GROUND_SIZE,
            ROADS = Roads,
            BRANCH_PATHS = Branches,
            BUILDINGS = Buildings,
            GROUNDS = Grounds,
            PROPS = Props,
            POND = new PondDef { pos = PondPos, r = PondR },
            VILLA = new VillaDef { x = 0f, z = 34f, w = 8.4f, h = 5.4f, d = 7f, face = -1 },
        };

        // 树：散布 + 行道树 + 池畔柳树/花园桃花 + 空场补植
        var treeTypes = new[] { 0, 6, 2, 9, 5, 1, 8, 2, 7, 0, 10, 4, 0, 1, 5, 2, 6, 0, 2, 4, 8, 6, 2, 5, 0, 7, 0, 2, 10, 7, 2, 6, 8, 5, 9, 4, 0, 2 };
        var trees = new List<float[]>();
        var rnd = new Lcg(20261013);
        var sc = Scatter(48, rnd, 56f, 2.4f, new List<float[]>(), keep);
        for (int i = 0; i < sc.Count; i++) trees.Add(new[] { sc[i][0], sc[i][1], treeTypes[i % treeTypes.Length] });
        // 行道树（中横路两侧，间距 8m；避开保留区）——与原 JS 同序：先 +6.4 侧(树种0) 再 -6.4 侧(树种5)
        foreach (var (zoff, v) in new[] { (6.4f, 0), (-6.4f, 5) })
            for (float x = -56f; x <= 56f; x += 8f)
                if (!InKeep(x, zoff, keep)) trees.Add(new[] { R1(x), R1(zoff), (float)v });
        // 人工点景
        foreach (var t in new[] { new[] { -26.5f, -9.8f, 9f }, new[] { -30.8f, -11f, 9f }, new[] { -22.3f, -10.5f, 9f },
            new[] { -30f, 25.5f, 10f }, new[] { -21.5f, 25.5f, 10f },
            new[] { 44f, 26f, 0f }, new[] { 48f, 23f, 5f }, new[] { 40f, 29f, 2f }, new[] { -46f, 26f, 0f }, new[] { -50f, 23f, 10f }, new[] { -42f, 29f, 5f },
            new[] { -46f, -12f, 0f }, new[] { -43f, -18f, 2f }, new[] { -49f, -9f, 5f } })
            trees.Add(t);
        w.TREES = ToTreeItems(trees);

        var bushes = Scatter(44, rnd, 56f, 1.3f, trees, keep);
        w.BUSHES = ToPosItems(bushes);
        var flowers = Scatter(56, rnd, 56f, 1.1f, new List<float[]>(trees).Concat(bushes).ToList(), keep);
        w.FLOWERS = ToPosItems(flowers);
        w.DIRT_PATCHES = ToPosItems(Scatter(12, rnd, 56f, 4.5f, new List<float[]>(), keep));

        // 路灯（含避让门前通路的最终坐标）
        w.LAMPS = BuildLamps();

        // 围栏（矮墙）：沿所有道路两侧，交叉口/岔路/建筑门前自动留缺口
        w.FENCES = BuildFences();

        // 假山 / 边界环带 / 芦苇 / 小石头
        w.ROCKERIES = new[]
        {
            new RockeryItem { x = -44, z = 34, s = 1.4f }, new RockeryItem { x = 40, z = 38, s = 1.2f }, new RockeryItem { x = 46, z = -34, s = 1.5f },
            new RockeryItem { x = -48, z = -24, s = 1.1f }, new RockeryItem { x = -44, z = 50, s = 1.3f }, new RockeryItem { x = 56, z = 24, s = 1.0f },
            new RockeryItem { x = -22, z = 50, s = 1.2f }, new RockeryItem { x = 26, z = -52, s = 1.3f }, new RockeryItem { x = 12, z = 36, s = 1.1f },
            new RockeryItem { x = -30, z = 52, s = 1.2f }, new RockeryItem { x = 34, z = -46, s = 1.0f }, new RockeryItem { x = -56, z = 38, s = 1.15f },
            new RockeryItem { x = 56, z = -24, s = 1.25f }, new RockeryItem { x = -12, z = -52, s = 1.05f }, new RockeryItem { x = 56, z = 34, s = 1.1f },
            new RockeryItem { x = -56, z = -40, s = 1.2f },
        };
        w.SMALL_ROCKS = ToPosItems(Scatter(18, rnd, 56f, 2.6f, new List<float[]>(trees).Concat(bushes).Concat(flowers).ToList(), keep));
        w.DISTANT_BUILDINGS = BuildDistant();
        w.BORDER_TREES = BuildBorderTrees(w.DISTANT_BUILDINGS);
        w.BORDER_BUSHES = BuildBorderBushes(w.BORDER_TREES);
        w.BORDER_ROCKS = BuildBorderRocks();
        w.REEDS = BuildReeds();

        // 可交互点位
        var spots = new List<SpotDef>();
        foreach (var b in Buildings)
            spots.Add(new SpotDef { name = b.name, x = b.pos[0], z = b.pos[1] + b.face * (b.size[2] / 2f + 0.9f), r = 2.4f });
        spots.Add(new SpotDef { name = "家", x = 0f, z = 34f - 3.5f - 1.2f, r = 2.4f });
        foreach (var g in Grounds)
            spots.Add(new SpotDef { name = g.name, x = g.pos[0] + g.size[0] / 2f - 2.4f, z = g.pos[1], r = 2.6f });
        spots.Add(new SpotDef { name = "池塘", x = PondPos[0] + PondR + 1.2f, z = PondPos[1], r = 2.2f });
        spots.Add(new SpotDef { name = "中心广场", x = -7f, z = -6f, r = 2.6f });
        spots.Add(new SpotDef { name = "花园", x = -26f, z = 26.5f, r = 2.8f });
        w.SPOTS = spots.ToArray();

        // 碰撞体（道具实体）
        var bl = new List<BlockerDef>();
        foreach (var p in Props)
        {
            float hw = 0.5f, hd = 0.5f;
            switch (p.type)
            {
                case "plaza": continue;
                case "bench": hw = 1.0f; hd = 1.0f; break;
                case "bin": hw = 0.35f; hd = 0.35f; break;
                case "board": hw = 0.9f; hd = 0.35f; break;
                case "bikerack": hw = (p.n > 0 ? p.n : 5) * 0.35f; hd = 0.5f; break;
                case "flag": hw = 0.3f; hd = 0.3f; break;
                case "planter": hw = p.w / 2f; hd = p.d / 2f; break;
                case "fountain": hw = p.r; hd = p.r; break;
                case "hoop": hw = 0.4f; hd = 0.4f; break;
                case "netpost": hw = (p.len > 0 ? p.len : 8f) / 2f; hd = 0.3f; break;
                case "gate":
                    bool alongZ = p.ry == 0f || Mathf.Abs(p.ry) == Mathf.PI;
                    if (p.kind == "main") { hw = alongZ ? 4.2f : 0.9f; hd = alongZ ? 0.9f : 4.2f; }
                    else { hw = 4.4f; hd = 0.9f; }
                    break;
            }
            bl.Add(new BlockerDef { x = p.x, z = p.z, hw = hw, hd = hd });
        }
        w.BLOCKERS = bl.ToArray();
        return w;
    }

    // ── 路灯 ──
    static LampItem[] BuildLamps()
    {
        var l = new List<LampItem>();
        void Add(float x, float z, float r) => l.Add(new LampItem { x = x, z = z, r = r });
        foreach (var z in new[] { -14f, -4f, 6f, 14.2f, 24f }) Add(-3.7f, z, 0f);
        foreach (var z in new[] { -9f, 4.5f, 11f, 21f, 25.5f }) Add(3.7f, z, Mathf.PI);
        foreach (var x in new[] { -46f, -26f, -19f, 11f, 26f, 34f, 44f, 54f }) Add(x, -3.7f, -Mathf.PI / 2f);
        foreach (var x in new[] { -30.8f, -8f, 8f, 23.5f, 33.5f, 44f, 54f }) Add(x, 3.7f, Mathf.PI / 2f);
        foreach (var x in new[] { -28f, -18f, -8f, 13f, 18f, 28f }) Add(x, -33.7f, -Mathf.PI / 2f);
        foreach (var x in new[] { -12f, 6f, 24f }) Add(x, -26.3f, Mathf.PI / 2f);
        foreach (var x in new[] { -28f, -19.5f, -6f, 6f, 16f, 26f }) Add(x, 14.3f, -Mathf.PI / 2f);
        foreach (var x in new[] { -24f, 24f }) Add(x, 21.7f, Mathf.PI / 2f);
        foreach (var z in new[] { -24f, -12f, 6f, 14f }) Add(-37.3f, z, 0f);
        foreach (var z in new[] { -18f, -6f, 8f }) Add(-30.6f, z, Mathf.PI);
        foreach (var z in new[] { -24f, -12f, 6f, 14f }) Add(33.3f, z, Mathf.PI);
        foreach (var z in new[] { -18f, -6f, 8f }) Add(26.6f, z, 0f);
        return l.ToArray();
    }

    // ── 围栏（矮墙）自动生成 ──
    static FenceDef[] BuildFences()
    {
        var runs = new List<FenceDef>();
        const float GAP = 1.6f;
        float[] CollectGaps(string axis, float at, float from, float to)
        {
            var gaps = new List<float>();
            float lo = Mathf.Min(from, to), hi = Mathf.Max(from, to);
            foreach (var r in Roads)
            {
                if (r.axis == axis) continue;
                if (r.from - 2 <= at && at <= r.to + 2 && lo - 2 <= r.at && r.at <= hi + 2)
                {
                    gaps.Add(r.at - r.width / 2 - GAP); gaps.Add(r.at + r.width / 2 + GAP);
                }
            }
            foreach (var p in Branches)
            {
                if (axis == "x" && lo - 1 <= p.x && p.x <= hi + 1 && Mathf.Abs(p.z - at) <= p.len / 2 + 0.2f)
                {
                    gaps.Add(p.x - 2.2f); gaps.Add(p.x + 2.2f);
                }
                if (axis == "z" && lo - 1 <= p.z && p.z <= hi + 1 && Mathf.Abs(p.x - at) <= 1.65f)
                {
                    gaps.Add(p.z - 2.2f); gaps.Add(p.z + 2.2f);
                }
            }
            foreach (var b in Buildings)
            {
                float dz = b.face * (b.size[2] / 2 + 0.9f);
                if (axis == "x")
                {
                    float c = b.pos[0], p = b.pos[1] + dz;
                    bool crossed = b.face == 1 ? (at > p && at < p + 12) : (at < p && at > p - 12);
                    if (crossed && lo - 1 <= c && c <= hi + 1) { gaps.Add(c - 2.2f); gaps.Add(c + 2.2f); }
                }
                else
                {
                    float c = b.pos[1] + dz, p = b.pos[0];
                    bool crossed = b.face == 1 ? (at > p && at < p + 12) : (at < p && at > p - 12);
                    if (crossed && lo - 1 <= c && c <= hi + 1) { gaps.Add(c - 2.2f); gaps.Add(c + 2.2f); }
                }
            }
            return gaps.ToArray();
        }
        void AddRun(string axis, float at, float from, float to, float[] gaps)
        {
            // 缺口按【起点】成对排序（拍平排序会把配对打乱 → 缺口失效）
            var pairs = new List<(float gs, float ge)>();
            for (int i = 0; i + 1 < gaps.Length; i += 2) pairs.Add((gaps[i], gaps[i + 1]));
            pairs.Sort((p, q) => p.gs.CompareTo(q.gs));
            float cur = from;
            foreach (var (gs, ge) in pairs)
            {
                if (gs > cur + 1) runs.Add(new FenceDef { axis = axis, at = at, from = cur, to = gs });
                cur = Mathf.Max(cur, ge);
            }
            if (to > cur + 1) runs.Add(new FenceDef { axis = axis, at = at, from = cur, to = to });
        }
        foreach (var r in Roads)
        {
            if (!r.fence) continue;
            float off = r.width / 2 + 0.5f;
            var gaps = CollectGaps(r.axis, r.at, r.from, r.to);
            AddRun(r.axis, r.axis == "z" ? r.at - off : r.at - off, r.from, r.to, gaps);
            AddRun(r.axis, r.axis == "z" ? r.at + off : r.at + off, r.from, r.to, gaps);
        }
        return runs.ToArray();
    }

    // ── 边界环带 / 远景 / 芦苇 ──
    static DistantDef[] BuildDistant()
    {
        var palette = new (string wall, string roof, string trim, string plinth)[]
        {
            ("#c4b494", "#7a4a3a", "#e8e0d0", "#8f7f65"), ("#b8a98e", "#46536b", "#ddd5c8", "#7d6f58"),
            ("#a89a82", "#3f5a4a", "#d5cfc2", "#75684f"), ("#c9b897", "#a03c30", "#eee6d6", "#8a7a5c"),
            ("#cfc4ae", "#5a6273", "#f0e9dc", "#95876d"), ("#b0937a", "#7a4a3a", "#e3d6c4", "#84684f"),
        };
        var styles = new[] { "flat", "gable", "tower", "grand" };
        var rnd = new Lcg(20261020);
        var outp = new List<DistantDef>();
        const int N = 28;
        for (int i = 0; i < N; i++)
        {
            double a = (i / (double)N) * Math.PI * 2 + (rnd.NextInt(100) / 100.0 - 0.5) * 0.14;
            bool inner = i % 2 == 0;
            double r = inner ? 84 + rnd.NextInt(900) / 100.0 : 98 + rnd.NextInt(1100) / 100.0;
            string style = inner ? styles[rnd.NextInt(2)] : styles[rnd.NextInt(4)];
            float w2 = style == "tower" ? 8 + rnd.NextInt(4) : style == "grand" ? 15 + rnd.NextInt(6) : 9 + rnd.NextInt(8);
            float h2 = style == "tower" ? 13 + rnd.NextInt(8) : style == "grand" ? 6.5f + rnd.NextInt(3) : 5 + rnd.NextInt(4);
            float d2 = 7 + rnd.NextInt(5);
            var pal = palette[(i * 2 + rnd.NextInt(2)) % 6];
            outp.Add(new DistantDef
            {
                x = R1(Math.Sin(a) * r), z = R1(Math.Cos(a) * r),
                w = w2, h = h2, d = d2, style = style,
                wall = pal.wall, roof = pal.roof, trim = pal.trim, plinth = pal.plinth,
            });
        }
        return outp.ToArray();
    }

    static TreeItem[] BuildBorderTrees(DistantDef[] distant)
    {
        var outp = new List<TreeItem>();
        var rnd = new Lcg(20261015);
        int guard = 0;
        while (outp.Count < 110 && guard++ < 3200)
        {
            double a = rnd.NextInt(3600) / 3600.0 * Math.PI * 2;
            float r = 63f + rnd.NextInt(1500) / 100f;
            float x = R1(Math.Sin(a) * r), z = R1(Math.Cos(a) * r);
            bool blocked = false;
            foreach (var b in distant)
                if (Mathf.Abs(x - b.x) < b.w / 2 + 3 && Mathf.Abs(z - b.z) < b.d / 2 + 3) { blocked = true; break; }
            if (blocked) continue;
            foreach (var t in outp)
                if (Mathf.Sqrt((x - t.x) * (x - t.x) + (z - t.z) * (z - t.z)) < 1.8f) { blocked = true; break; }
            if (blocked) continue;
            int vv = rnd.NextInt(12);
            outp.Add(new TreeItem { x = x, z = z, v = vv < 6 ? vv : vv < 8 ? 6 : vv < 10 ? 7 : 8 });
        }
        return outp.ToArray();
    }

    static PosItem[] BuildBorderBushes(TreeItem[] borderTrees)
    {
        var outp = new List<PosItem>();
        var rnd = new Lcg(20261016);
        int guard = 0;
        while (outp.Count < 120 && guard++ < 3000)
        {
            double a = rnd.NextInt(3600) / 3600.0 * Math.PI * 2;
            float r = 62f + rnd.NextInt(1500) / 100f;
            float x = R1(Math.Sin(a) * r), z = R1(Math.Cos(a) * r);
            bool blocked = false;
            foreach (var t in borderTrees)
                if (Mathf.Sqrt((x - t.x) * (x - t.x) + (z - t.z) * (z - t.z)) < 1.8f) { blocked = true; break; }
            if (blocked) continue;
            foreach (var p in outp)
                if (Mathf.Sqrt((x - p.x) * (x - p.x) + (z - p.z) * (z - p.z)) < 1.4f) { blocked = true; break; }
            if (blocked) continue;
            outp.Add(new PosItem { x = x, z = z });
        }
        return outp.ToArray();
    }

    static PosItem[] BuildBorderRocks()
    {
        var outp = new List<PosItem>();
        var rnd = new Lcg(20261019);
        int guard = 0;
        while (outp.Count < 26 && guard++ < 1000)
        {
            double a = rnd.NextInt(3600) / 3600.0 * Math.PI * 2;
            float r = 64f + rnd.NextInt(1400) / 100f;
            outp.Add(new PosItem { x = R1(Math.Sin(a) * r), z = R1(Math.Cos(a) * r) });
        }
        return outp.ToArray();
    }

    static ReedItem[] BuildReeds()
    {
        var outp = new List<ReedItem>();
        var rnd = new Lcg(20261018);
        for (int i = 0; i < 18; i++)
        {
            double a = (i / 18.0) * Math.PI * 2 + (rnd.NextInt(100) / 100.0 - 0.5) * 0.35;
            double r = 3.75 + rnd.NextInt(50) / 100.0;
            outp.Add(new ReedItem
            {
                x = (float)Math.Round((PondPos[0] + Math.Cos(a) * r) * 100) / 100f,
                z = (float)Math.Round((PondPos[1] + Math.Sin(a) * r) * 100) / 100f,
                h = 0.7f + rnd.NextInt(70) / 100f,
                t = rnd.NextInt(3),
            });
        }
        return outp.ToArray();
    }

    static TreeItem[] ToTreeItems(List<float[]> l)
    {
        var outp = new TreeItem[l.Count];
        for (int i = 0; i < l.Count; i++) outp[i] = new TreeItem { x = l[i][0], z = l[i][1], v = (int)l[i][2] };
        return outp;
    }

    static PosItem[] ToPosItems(List<float[]> l)
    {
        var outp = new PosItem[l.Count];
        for (int i = 0; i < l.Count; i++) outp[i] = new PosItem { x = l[i][0], z = l[i][1] };
        return outp;
    }
}
