using UnityEngine;

// 实验楼 78×52：中央 4.4m 走廊 + 五间差异化实验室
//   物理：光学导轨 / 单摆 / 电学台；化学：黑面实验台 / 双通风柜 / 试剂墙 / 安全站；
//   生物：显微镜台 / 组培架 / 水族箱 / 标本柜；计算机：5×6 机位 / 服务器柜；
//   创客：工具墙 / 3D 打印 / 机器人场地 / 激光切割。
// 南侧门厅做展柜与服务台，走廊两端储物柜/公告板。
public static partial class InteriorBuilder
{
    static void LabDoorAndSign(Transform p, Interior it, float doorX, float zWall, int dir,
                              string label, Material wall)
    {
        DormDoor(p, doorX, zWall, dir, CB("#8fb0c8"));
        WallSign(p, doorX + 1.28f, 2.58f, zWall, -dir, label, 1.55f, 0.40f, CB("#4a9ac8"), 0.028f);
    }

    // 教师区：讲台 + 白板（白板贴走廊墙，避开门口）
    static void LabTeacher(Transform p, Interior it, float x, float zWall, int dir, Material wood)
    {
        float zDesk = zWall + dir * 0.78f;
        Table(p, new Vector3(x, 0f, zDesk), 3.0f, 0.9f, 0.82f, wood);
        Reg(it, x, zDesk, 3.0f, 0.9f);
        // 椅子在讲台靠教室一侧，人要面向讲台与白板（Chair 靠背在局部 -Z：朝 +Z 取 0，朝 -Z 取 180）
        Chair(p, it, new Vector3(x, 0f, zDesk + dir * 0.76f), dir < 0 ? 0f : 180f, wood, true);
        float zBoard = zWall + dir * 0.13f;
        // 白板：外框倒角 + 板面内缩（原来是一块 5×1.2 的灰白板孤零零挂在墙上）
        PartC(p, new Vector3(x, 1.58f, zBoard + dir * 0.03f), new Vector3(5.24f, 1.44f, 0.05f), CB("#c98a4e"), 0.014f, 1f);
        Part(p, new Vector3(x, 1.58f, zBoard), new Vector3(5.0f, 1.20f, 0.03f), C("#fbfdf8"), 1f);
        PartC(p, new Vector3(x, 2.22f, zBoard + dir * 0.05f), new Vector3(5.2f, 0.10f, 0.10f), CB("#c98a4e"), 0.010f, 1f);
        PartC(p, new Vector3(x, 0.98f, zBoard + dir * 0.06f), new Vector3(4.6f, 0.06f, 0.14f), CB("#c98a4e"), 0.010f, 1f);
        // 白板笔：三支彩色（卡通近景里白板必须有颜色，否则就是一面白墙）
        Part(p, new Vector3(x + 1.7f, 1.12f, zBoard - dir * 0.06f), new Vector3(0.16f, 0.04f, 0.04f), C("#e0554a"), 1f);
        Part(p, new Vector3(x + 1.4f, 1.12f, zBoard - dir * 0.06f), new Vector3(0.16f, 0.04f, 0.04f), C("#4a9ac8"), 1f);
        Part(p, new Vector3(x + 1.1f, 1.12f, zBoard - dir * 0.06f), new Vector3(0.16f, 0.04f, 0.04f), C("#3f9a6a"), 1f);
    }

    // 实验台：台面 + 柜体 + 水槽（可选）+ 两侧圆凳
    static void LabBench(Transform p, Interior it, float x, float z, float w, float d,
                         Material top, Material body, Material accent, bool sink, bool gas)
    {
        // 卡通实验台：台面倒角 + 柜体倒角（比台面内缩）+ 前沿色条 + 柜门竖缝
        PartC(p, new Vector3(x, 0.88f, z), new Vector3(w, 0.08f, d), top, 0.016f, 1f);
        PartC(p, new Vector3(x, 0.42f, z), new Vector3(w - 0.24f, 0.78f, d - 0.18f), body, 0.026f, 1f);
        PartC(p, new Vector3(x, 0.84f, z + d * 0.5f - 0.03f), new Vector3(w - 0.10f, 0.05f, 0.05f), accent, 0.010f, 1f);
        for (int i = 0; i < Mathf.FloorToInt(w / 0.9f); i++)
            Part(p, new Vector3(x - w * 0.5f + 0.35f + i * 0.9f, 0.42f, z + d * 0.5f - 0.05f),
                 new Vector3(0.03f, 0.70f, 0.02f), Darker(body, 0.76f), 1f);
        if (sink)
        {
            PartC(p, new Vector3(x + w * 0.30f, 0.925f, z), new Vector3(0.46f, 0.03f, 0.34f), CB("#6a7480"), 0.008f, 1f);
            Part(p, new Vector3(x + w * 0.30f, 1.10f, z - 0.24f), new Vector3(0.05f, 0.36f, 0.05f), CB("#b8c8d8"), 1f);
            Part(p, new Vector3(x + w * 0.30f, 1.26f, z - 0.15f), new Vector3(0.05f, 0.05f, 0.22f), CB("#b8c8d8"), 1f);
        }
        if (gas)
            PartC(p, new Vector3(x - w * 0.30f, 0.99f, z + 0.10f), new Vector3(0.10f, 0.14f, 0.10f), CB("#f0b03c"), 0.008f, 1f);
        Reg(it, x, z, w, d);
        Stool(p, it, x - w * 0.25f, z - d * 0.5f - 0.50f, accent);
        Stool(p, it, x + w * 0.25f, z - d * 0.5f - 0.50f, accent);
    }

    static void Microscope(Transform p, float x, float y, float z)
    {
        var dark = CB("#4a5260");
        PartC(p, new Vector3(x, y + 0.035f, z), new Vector3(0.34f, 0.07f, 0.26f), dark, 0.010f, 0.6f);
        Part(p, new Vector3(x, y + 0.26f, z - 0.06f), new Vector3(0.07f, 0.42f, 0.07f), dark, 0.6f);
        PartC(p, new Vector3(x, y + 0.30f, z + 0.10f), new Vector3(0.24f, 0.04f, 0.18f), CB("#b8c8d8"), 0.008f, 0.6f);
        Part(p, new Vector3(x, y + 0.46f, z + 0.03f), new Vector3(0.08f, 0.26f, 0.08f), CB("#6a7480"), 0.6f);
        Part(p, new Vector3(x, y + 0.60f, z + 0.03f), new Vector3(0.12f, 0.05f, 0.12f), CB("#f0b03c"), 0.6f);
    }

    static void FumeHood(Transform p, Interior it, float x, float z, float face)
    {
        float zc = z;
        var body = CB("#c8d4dc");
        PartC(p, new Vector3(x, 1.30f, zc), new Vector3(2.4f, 2.6f, 1.0f), body, 0.030f, 1f);
        PartC(p, new Vector3(x, 0.98f, zc), new Vector3(2.16f, 0.08f, 0.84f), CB("#4a5260"), 0.014f, 1f);
        PartC(p, new Vector3(x, 1.55f, zc + face * 0.52f), new Vector3(2.10f, 1.20f, 0.04f), CB("#6aa8c8"), 0.014f, 1f);
        PartC(p, new Vector3(x, 2.24f, zc + face * 0.50f), new Vector3(2.2f, 0.10f, 0.06f), CB("#a8b8c4"), 0.012f, 1f);
        PartC(p, new Vector3(x, 2.30f, zc), new Vector3(0.46f, 0.06f, 0.32f), C("#eaf4ff"), 0.010f, 1f);
        PartC(p, new Vector3(x, 3.35f, zc), new Vector3(0.50f, 1.40f, 0.50f), CB("#b8c4d0"), 0.024f, 1f);
        Reg(it, x, zc, 2.4f, 1.0f);
        Lamp(p, new Vector3(x, 2.10f, zc - face * 0.30f), 3.2f, 0.8f, "#eaf4ff");
    }

    static void LabFloor(Transform p, float x0, float x1, float z0, float z1, Material mat, float cx, float cz)
    {
        Part(p, new Vector3((x0 + x1) * 0.5f, 0.044f, (z0 + z1) * 0.5f),
             new Vector3(x1 - x0 - 0.3f, 0.010f, z1 - z0 - 0.3f), mat, 1.8f);
        Part(p, new Vector3(cx, 0.056f, (z0 + z1) * 0.5f),
             new Vector3(1.8f, 0.010f, z1 - z0 - 0.6f), C("#c8d4dc"), 1f);
        Part(p, new Vector3((x0 + x1) * 0.5f, 0.056f, cz), new Vector3(x1 - x0 - 0.6f, 0.010f, 1.8f), C("#c8d4dc"), 1f);
    }

    static Interior Labs(Transform parent)
    {
        const float X = 39f, Z = 26f, W = 4.2f;
        var wall = T("in_wall3");
        var it = Shell(parent, "labs", "实验楼", LABS_X, X, Z, W, 1.8f, 2.8f,
                       wall, T("in_floor"), T("in_ceil"), wall, CB("#4a9ac8"), 0.62f);
        var p = it.Root.transform;
        float ox = LABS_X;
        // 卡通配色（docs/103）：原 #d8d2c6 台面 / #8a9aa6 柜体 / #8d99a6 金属全是中低明度灰，
        // 卡通渲染的冷调暗面会把它们压成一片灰绿。改成高明度冷白台面 + 饱和青蓝柜体。
        var benchTop = C("#f2f8fc");
        var benchBody = CB("#6fb0d0");
        var metal = CB("#b8c8d8");

        // ── 中央走廊 z=-2.2..2.2 ──
        Part(p, new Vector3(ox, 0.052f, 0f), new Vector3(76f, 0.012f, 3.0f), C("#c8d8e4"), 1.6f);
        Rug(p, new Vector3(ox - 18f, 0.062f, 0f), 34.0f, 1.8f, CB("#5fa8c8"));
        Rug(p, new Vector3(ox + 18f, 0.062f, 0f), 34.0f, 1.8f, CB("#5fa8c8"));

        // 走廊两侧墙（北 3 门 / 南 2 门 + 门厅开口）
        var northDoors = new[] {
            (x0: -38.88f, x1: -13.17f, door: -26.0f, label: "物理实验室"),
            (x0: -12.99f, x1: 12.81f, door: 0f, label: "生物实验室"),
            (x0: 12.99f, x1: 38.88f, door: 26.0f, label: "化学实验室"),
        };
        foreach (var r in northDoors)
            WallX(p, it, ox + r.x0, ox + r.x1, -2.2f, 0.02f, W,
                  ox + r.door - 0.9f, ox + r.door + 0.9f, 2.6f, wall, WALL_IN);
        WallX(p, it, ox - 38.88f, ox - 8.09f, 2.2f, 0.02f, W,
              ox - 24.4f, ox - 22.6f, 2.6f, wall, WALL_IN);
        WallX(p, it, ox + 8.09f, ox + 38.88f, 2.2f, 0.02f, W,
              ox + 22.6f, ox + 24.4f, 2.6f, wall, WALL_IN);
        foreach (var r in northDoors)
            LabDoorAndSign(p, it, ox + r.door, -2.2f, -1, r.label, wall);
        LabDoorAndSign(p, it, ox - 23.5f, 2.2f, 1, "计算机实验室", wall);
        LabDoorAndSign(p, it, ox + 23.5f, 2.2f, 1, "创客实验室", wall);

        // 北侧隔墙 / 南侧门厅墙
        WallZ(p, it, -25.88f, -2.2f, ox - 13.08f, 0.02f, W, 0f, 0f, 2.6f, wall, WALL_IN);
        WallZ(p, it, -25.88f, -2.2f, ox + 12.9f, 0.02f, W, 0f, 0f, 2.6f, wall, WALL_IN);
        WallZ(p, it, 2.2f, 25.88f, ox - 8f, 0.02f, W, 0f, 0f, 2.6f, wall, WALL_IN);
        WallZ(p, it, 2.2f, 25.88f, ox + 8f, 0.02f, W, 0f, 0f, 2.6f, wall, WALL_IN);

        // ═════════ 物理实验室（西北 25.7×23.6） ═════════
        {
            float x0 = -38.88f, x1 = -13.17f, cx = -26.0f, cz = -14.1f;
            var top = C("#f2f8fc");
            var body = CB("#6fb0d0");
            LabFloor(p, ox + x0, ox + x1, -25.88f, -2.29f, T("in_tile"), ox + cx, cz);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 2; j++)
                    LabBench(p, it, ox + cx + (j == 0 ? -6.5f : 6.5f), cz - 7f + i * 7f,
                             4.4f, 1.1f, top, body, CB("#4a9ac8"), true, false);
            LabTeacher(p, it, ox + cx + 4.5f, -2.2f, -1, InteriorWood());

            // 光学导轨
            float rz = -24.3f;
            PartC(p, new Vector3(ox + cx - 7.0f, 0.90f, rz), new Vector3(6.4f, 0.08f, 0.9f), CB("#4a5260"), 0.014f, 1f);
            PartC(p, new Vector3(ox + cx - 7.0f, 0.97f, rz), new Vector3(6.4f, 0.05f, 0.10f), CB("#b8c8d8"), 0.008f, 1f);
            for (int i = 0; i < 5; i++)
            {
                float lx = ox + cx - 9.7f + i * 1.35f;
                PartC(p, new Vector3(lx, 1.12f, rz), new Vector3(0.10f, 0.28f, 0.10f), metal, 0.008f, 0.6f);
                PartC(p, new Vector3(lx, 1.26f, rz), new Vector3(0.22f, 0.22f, 0.04f), CB("#5fb8e0"), 0.008f, 0.6f);
            }
            PartC(p, new Vector3(ox + cx - 10.7f, 1.20f, rz), new Vector3(0.30f, 0.24f, 0.30f), CB("#e0554a"), 0.010f, 0.6f);
            Reg(it, ox + cx - 7.0f, rz, 6.4f, 0.9f);

            // 单摆
            float px = ox + cx + 4.0f, pz = -23.7f;
            foreach (float sx in new[] { -1f, 1f })
                Part(p, new Vector3(px + sx * 1.2f, 1.55f, pz), new Vector3(0.12f, 3.1f, 0.12f), CB("#6a7480"), 1f);
            PartC(p, new Vector3(px, 3.02f, pz), new Vector3(2.6f, 0.10f, 0.12f), CB("#6a7480"), 0.010f, 1f);
            Part(p, new Vector3(px, 2.2f, pz), new Vector3(0.03f, 1.6f, 0.03f), C("#e8dcc0"), 1f);
            var bob = new GameObject("pendulumbob");
            bob.transform.SetParent(p, false);
            bob.transform.localPosition = new Vector3(px, 1.36f, pz);
            bob.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Sphere(0.24f, 10, 8);
            bob.AddComponent<MeshRenderer>().sharedMaterial = CB("#c8d4dc");
            Reg(it, px, pz, 2.6f, 0.5f);

            // 电学台 + 示波器
            PartC(p, new Vector3(ox + cx + 10.6f, 0.86f, -17.8f), new Vector3(3.0f, 0.08f, 1.2f), CB("#4a5260"), 0.014f, 1f);
            PartC(p, new Vector3(ox + cx + 10.6f, 0.42f, -17.8f), new Vector3(2.8f, 0.80f, 1.0f), body, 0.026f, 1f);
            Reg(it, ox + cx + 10.6f, -17.8f, 3.0f, 1.2f);
            for (int i = 0; i < 2; i++)
            {
                PartC(p, new Vector3(ox + cx + 9.8f + i * 1.6f, 1.18f, -17.8f),
                      new Vector3(0.56f, 0.40f, 0.30f), CB("#4a5260"), 0.014f, 1f);
                Part(p, new Vector3(ox + cx + 9.8f + i * 1.6f, 1.18f, -17.63f),
                     new Vector3(0.42f, 0.28f, 0.02f), C("#6ce0a8"), 1f);
                Part(p, new Vector3(ox + cx + 9.8f + i * 1.6f, 0.97f, -17.8f),
                     new Vector3(0.40f, 0.06f, 0.22f), CB("#f0b03c"), 1f);
            }

            // 器材柜（开放式，能看到器材）
            foreach (float sx in new[] { -1f, 1f })
            {
                float cxc = ox + cx + sx * 2.6f;
                PartC(p, new Vector3(cxc, 1.0f, -25.72f), new Vector3(2.4f, 2.0f, 0.06f), CB("#8a9aa6"), 0.012f, 1f);
                foreach (float sd in new[] { -1f, 1f })
                    PartC(p, new Vector3(cxc + sd * 1.17f, 1.0f, -25.45f), new Vector3(0.06f, 2.0f, 0.56f), CB("#8a9aa6"), 0.010f, 1f);
                PartC(p, new Vector3(cxc, 1.98f, -25.45f), new Vector3(2.4f, 0.06f, 0.56f), CB("#8a9aa6"), 0.012f, 1f);
                PartC(p, new Vector3(cxc, 0.02f, -25.45f), new Vector3(2.4f, 0.06f, 0.56f), CB("#8a9aa6"), 0.012f, 1f);
                for (int sh = 1; sh <= 2; sh++)
                    PartC(p, new Vector3(cxc, 0.10f + sh * 0.62f, -25.45f), new Vector3(2.3f, 0.04f, 0.52f), CB("#b8c8d8"), 0.008f, 1f);
                for (int itm = 0; itm < 10; itm++)
                {
                    float ix = cxc - 1.0f + (itm % 5) * 0.50f;
                    float iy = itm < 5 ? 0.21f : 0.90f;
                    PartC(p, new Vector3(ix, iy, -25.40f), new Vector3(0.28f, 0.30f, 0.30f),
                          CB(itm % 3 == 0 ? "#e08a3c" : itm % 3 == 1 ? "#4a9ac8" : "#b8c8d8"), 0.010f, 1f);
                }
                Reg(it, cxc, -25.45f, 2.4f, 0.56f);
            }
            Window(p, new Vector3(ox + cx - 10f, 2.55f, -25.72f), 4.0f, 1.5f, 0f);
            Window(p, new Vector3(ox + cx + 10f, 2.55f, -25.72f), 4.0f, 1.5f, 0f);
            Picture(p, new Vector3(ox + x1 - 0.10f, 2.15f, cz + 2.0f), 1.6f, 1.1f, 270f);
            Picture(p, new Vector3(ox + x1 - 0.10f, 2.15f, cz - 2.0f), 1.6f, 1.1f, 270f);

            // 中央演示台：等离子球 + 牛顿摆
            Table(p, new Vector3(ox + cx, 0f, -9.5f), 2.4f, 1.1f, 0.86f, InteriorWood());
            Reg(it, ox + cx, -9.5f, 2.4f, 1.1f);
            PartC(p, new Vector3(ox + cx - 0.62f, 0.96f, -9.5f), new Vector3(0.38f, 0.18f, 0.38f), CB("#4a5260"), 0.012f, 1f);
            var plasma = new GameObject("plasmaglobe");
            plasma.transform.SetParent(p, false);
            plasma.transform.localPosition = new Vector3(ox + cx - 0.62f, 1.32f, -9.5f);
            plasma.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Sphere(0.32f, 12, 8);
            plasma.AddComponent<MeshRenderer>().sharedMaterial = MatLib.UnlitColor(MatLib.Hex("#c07ae8"));
            Lamp(p, new Vector3(ox + cx - 0.62f, 1.35f, -9.5f), 4.5f, 0.9f, "#c88ae8");
            foreach (float sx in new[] { -1f, 1f })
                Part(p, new Vector3(ox + cx + 0.72f + sx * 0.55f, 1.10f, -9.5f),
                     new Vector3(0.06f, 0.64f, 0.06f), CB("#6a7480"), 1f);
            PartC(p, new Vector3(ox + cx + 1.27f, 1.42f, -9.5f), new Vector3(1.22f, 0.06f, 0.06f), CB("#b8c8d8"), 0.008f, 1f);
            for (int i = 0; i < 5; i++)
            {
                var ball = new GameObject("cradleball");
                ball.transform.SetParent(p, false);
                ball.transform.localPosition = new Vector3(ox + cx + 0.72f - 0.26f + i * 0.13f, 1.24f, -9.5f);
                ball.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Sphere(0.065f, 8, 6);
                ball.AddComponent<MeshRenderer>().sharedMaterial = CB("#c8d4dc");
                Part(p, new Vector3(ox + cx + 0.72f - 0.26f + i * 0.13f, 1.33f, -9.5f),
                     new Vector3(0.02f, 0.16f, 0.02f), C("#6a7480"), 0.5f);
            }
            foreach (float sx in new[] { -1f, 1f })
            {
                Lamp(p, new Vector3(ox + cx + sx * 6f, W - 0.35f, cz - 6f), 18f, 1.8f, "#eaf4ff");
                Lamp(p, new Vector3(ox + cx + sx * 6f, W - 0.35f, cz + 6f), 18f, 1.8f, "#eaf4ff");
            }
            Lamp(p, new Vector3(ox + cx + 4.5f, W - 0.35f, cz), 10f, 1.3f, "#eaf4ff");
        }

        // ═════════ 生物实验室（北中 25.8×23.6） ═════════
        {
            float x0 = -12.99f, x1 = 12.81f, cx = -0.09f, cz = -14.1f;
            var top = C("#f0fbf2");
            var body = CB("#6ac898");
            LabFloor(p, ox + x0, ox + x1, -25.88f, -2.29f, T("in_tile"), ox + cx, cz);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 2; j++)
                    LabBench(p, it, ox + cx + (j == 0 ? -6.5f : 6.5f), cz - 7f + i * 7f,
                             4.4f, 1.1f, top, body, CB("#3f9a6a"), true, false);
            LabTeacher(p, it, ox + cx + 4.5f, -2.2f, -1, InteriorWood());

            // 显微镜：每个实验台两台
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 2; j++)
                {
                    float bx = ox + cx + (j == 0 ? -6.5f : 6.5f);
                    float bz = cz - 7f + i * 7f;
                    Microscope(p, bx - 0.7f, 0.92f, bz);
                    Microscope(p, bx + 0.7f, 0.92f, bz);
                }

            // 组培架（东墙，带粉紫生长灯）
            float gx = ox + x1 - 0.55f;
            PartC(p, new Vector3(gx, 1.10f, cz - 1.5f), new Vector3(0.72f, 2.20f, 4.4f), CB("#8a9aa6"), 0.026f, 1f);
            Reg(it, gx, cz - 1.5f, 0.72f, 4.4f);
            for (int r = 0; r < 3; r++)
            {
                float ry2 = 0.45f + r * 0.65f;
                PartC(p, new Vector3(gx - 0.10f, ry2, cz - 1.5f), new Vector3(0.70f, 0.05f, 4.2f), CB("#c8d4dc"), 0.008f, 1f);
                for (int i = 0; i < 5; i++)
                    PartC(p, new Vector3(gx - 0.16f, ry2 + 0.16f, cz - 3.2f + i * 0.85f),
                          new Vector3(0.30f, 0.28f, 0.30f), CB("#5fc86a"), 0.010f, 1f);
                Part(p, new Vector3(gx - 0.10f, ry2 + 0.58f, cz - 1.5f), new Vector3(0.62f, 0.05f, 4.2f), C("#ffd0e8"), 1f);
            }
            Lamp(p, new Vector3(ox + x1 - 1.2f, 2.05f, cz - 2.6f), 4.5f, 1.5f, "#ff9ad0");
            Lamp(p, new Vector3(ox + x1 - 1.2f, 2.05f, cz - 0.2f), 4.5f, 1.5f, "#ff9ad0");

            // 水族箱：开放框架，水体和鱼都露出来（Glass 不透明，不能整面罩住）
            float ax = ox + cx + 6.5f, az = -25.1f;
            PartC(p, new Vector3(ax, 0.30f, az), new Vector3(3.2f, 0.60f, 1.0f), CB("#c98a4e"), 0.026f, 1f);
            PartC(p, new Vector3(ax, 0.86f, az - 0.16f), new Vector3(2.86f, 0.52f, 0.50f), CB("#4ab0d8"), 0.022f, 1f);
            for (int i = 0; i < 4; i++)
                Part(p, new Vector3(ax - 1.0f + i * 0.65f, 0.98f, az + 0.26f),
                     new Vector3(0.22f, 0.10f, 0.10f), C(i % 2 == 0 ? "#ffc04a" : "#ff8a3c"), 1f);
            foreach (float sx4 in new[] { -1f, 1f })
                foreach (float sz4 in new[] { -1f, 1f })
                    Part(p, new Vector3(ax + sx4 * 1.46f, 0.86f, az + sz4 * 0.40f),
                         new Vector3(0.06f, 1.05f, 0.06f), C("#8ad8f0"), 1f);
            Part(p, new Vector3(ax, 1.38f, az - 0.16f), new Vector3(3.0f, 0.05f, 0.60f), C("#8ad8f0"), 1f);
            Part(p, new Vector3(ax, 0.58f, az + 0.30f), new Vector3(3.0f, 0.05f, 0.26f), C("#8ad8f0"), 1f);
            Reg(it, ax, az, 3.2f, 1.0f);

            // 标本柜
            float sx2 = ox + cx - 6.5f, sz2 = -25.4f;
            PartC(p, new Vector3(sx2, 1.1f, sz2 - 0.28f), new Vector3(3.0f, 2.2f, 0.06f), CB("#a06a3c"), 0.014f, 1f);
            foreach (float sx4 in new[] { -1f, 1f })
                PartC(p, new Vector3(sx2 + sx4 * 1.47f, 1.1f, sz2), new Vector3(0.06f, 2.2f, 0.66f), CB("#a06a3c"), 0.012f, 1f);
            PartC(p, new Vector3(sx2, 2.17f, sz2), new Vector3(3.0f, 0.06f, 0.66f), CB("#a06a3c"), 0.014f, 1f);
            PartC(p, new Vector3(sx2, 0.03f, sz2), new Vector3(3.0f, 0.06f, 0.66f), CB("#a06a3c"), 0.014f, 1f);
            for (int r = 1; r <= 2; r++)
                PartC(p, new Vector3(sx2, 0.12f + r * 0.70f, sz2), new Vector3(2.9f, 0.04f, 0.60f), CB("#c98a4e"), 0.008f, 1f);
            for (int i = 0; i < 12; i++)
            {
                float jx = sx2 - 1.2f + (i % 6) * 0.48f;
                float jy = i < 6 ? 0.27f : 1.01f;
                PartC(p, new Vector3(jx, jy, sz2 + 0.04f), new Vector3(0.22f, 0.38f, 0.22f),
                      CB(i % 3 == 0 ? "#8ad86a" : i % 3 == 1 ? "#e0d060" : "#b8c8d8"), 0.010f, 1f);
            }
            Reg(it, sx2, sz2, 3.0f, 0.66f);

            Window(p, new Vector3(ox + cx - 10f, 2.55f, -25.72f), 4.0f, 1.5f, 0f);
            Window(p, new Vector3(ox + cx + 10f, 2.55f, -25.72f), 4.0f, 1.5f, 0f);
            Picture(p, new Vector3(ox + x0 + 0.10f, 2.2f, cz), 1.8f, 1.2f, 90f);
            foreach (float sx3 in new[] { -1f, 1f })
            {
                Lamp(p, new Vector3(ox + cx + sx3 * 6f, W - 0.35f, cz - 6f), 18f, 1.7f);
                Lamp(p, new Vector3(ox + cx + sx3 * 6f, W - 0.35f, cz + 6f), 18f, 1.7f);
            }
            Lamp(p, new Vector3(ox + cx + 4.5f, W - 0.35f, cz), 10f, 1.25f);
        }

        // ═════════ 化学实验室（东北 25.9×23.6） ═════════
        {
            float x0 = 12.99f, x1 = 38.88f, cx = 25.935f, cz = -14.1f;
            // 化学台面按规范是黑色耐酸碱面；卡通渲染下纯黑会变死块，用深蓝灰 + 亮描边
            var top = CB("#4a5260");
            var body = CB("#5fa8c8");
            LabFloor(p, ox + x0, ox + x1, -25.88f, -2.29f, T("in_tile"), ox + cx, cz);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 2; j++)
                    LabBench(p, it, ox + cx + (j == 0 ? -6.3f : 6.3f), cz - 7f + i * 7f,
                             4.4f, 1.1f, top, body, CB("#e08a3c"), true, true);
            LabTeacher(p, it, ox + cx + 4.5f, -2.2f, -1, InteriorWood());

            // 中央演示台：彩色试剂 + 酒精灯
            Table(p, new Vector3(ox + cx, 0f, -9.5f), 2.4f, 1.0f, 0.86f, CB("#4a5260"));
            Reg(it, ox + cx, -9.5f, 2.4f, 1.0f);
            PartC(p, new Vector3(ox + cx - 0.2f, 0.96f, -9.5f), new Vector3(1.5f, 0.06f, 0.34f), CB("#c98a4e"), 0.010f, 1f);
            for (int i = 0; i < 6; i++)
            {
                float bx = ox + cx - 0.85f + i * 0.28f;
                PartC(p, new Vector3(bx, 1.10f, -9.5f), new Vector3(0.18f, 0.26f, 0.18f),
                      CB(i % 3 == 0 ? "#e0554a" : i % 3 == 1 ? "#4a9ac8" : "#f0b03c"), 0.008f, 1f);
                Part(p, new Vector3(bx, 1.25f, -9.5f), new Vector3(0.14f, 0.05f, 0.14f), C("#fff6e2"), 1f);
            }
            PartC(p, new Vector3(ox + cx + 0.85f, 0.94f, -9.5f), new Vector3(0.28f, 0.10f, 0.28f), CB("#6a7480"), 0.010f, 1f);
            Part(p, new Vector3(ox + cx + 0.85f, 1.05f, -9.5f), new Vector3(0.10f, 0.18f, 0.10f), C("#ffb04a"), 1f);

            // 双通风柜（北外墙）
            FumeHood(p, it, ox + cx - 8f, -25.3f, 1f);
            FumeHood(p, it, ox + cx + 8f, -25.3f, 1f);

            // 试剂墙（西墙两组）
            foreach (float zc in new[] { -19.5f, -9.5f })
            {
                float rx = ox + x0 + 0.32f;
                PartC(p, new Vector3(rx + 0.18f, 1.15f, zc), new Vector3(0.05f, 2.30f, 5.2f), CB("#a06a3c"), 0.012f, 1f);
                for (int r = 0; r < 4; r++)
                {
                    float ry2 = 0.35f + r * 0.56f;
                    PartC(p, new Vector3(rx, ry2, zc), new Vector3(0.40f, 0.05f, 5.0f), CB("#c98a4e"), 0.008f, 1f);
                    for (int i = 0; i < 10; i++)
                        PartC(p, new Vector3(rx, ry2 + 0.15f, zc - 2.2f + i * 0.48f),
                              new Vector3(0.18f, 0.26f, 0.18f),
                              CB(i % 4 == 0 ? "#f0b03c" : i % 4 == 1 ? "#4ab0c0" : i % 4 == 2 ? "#c07ad8" : "#8ad86a"), 0.008f, 1f);
                }
                Reg(it, rx, zc, 0.40f, 5.2f);
            }

            // 安全站：洗眼器 + 淋浴 + 灭火器
            float ex = ox + cx - 11f;
            PartC(p, new Vector3(ex, 0.52f, -25.30f), new Vector3(0.50f, 1.04f, 0.50f), CB("#c8d4dc"), 0.016f, 1f);
            PartC(p, new Vector3(ex, 1.08f, -25.30f), new Vector3(0.40f, 0.12f, 0.40f), CB("#4ac86a"), 0.010f, 1f);
            Reg(it, ex, -25.30f, 0.50f, 0.50f);
            Part(p, new Vector3(ex + 1.2f, 1.55f, -25.72f), new Vector3(0.10f, 3.0f, 0.10f), metal, 1f);
            Part(p, new Vector3(ex + 1.2f, 3.02f, -25.58f), new Vector3(0.06f, 0.06f, 0.34f), metal, 1f);
            PartC(p, new Vector3(ex + 1.2f, 2.96f, -25.40f), new Vector3(0.28f, 0.05f, 0.28f), CB("#c8d4dc"), 0.008f, 1f);
            PartC(p, new Vector3(ox + cx + 11.8f, 0.45f, -25.55f), new Vector3(0.36f, 0.90f, 0.36f), CB("#e0554a"), 0.014f, 1f);
            Reg(it, ox + cx + 11.8f, -25.55f, 0.36f, 0.36f);

            // 周期表 + 通风管
            PartC(p, new Vector3(ox + cx - 7f, 1.65f, -2.33f), new Vector3(2.66f, 1.36f, 0.05f), CB("#c98a4e"), 0.012f, 1f);
            Part(p, new Vector3(ox + cx - 7f, 1.65f, -2.30f), new Vector3(2.6f, 1.30f, 0.02f), C("#fffaf0"), 1f);
            for (int i = 0; i < 40; i++)
            {
                float px = ox + cx - 8.2f + (i % 10) * 0.25f;
                float py = 2.10f - (i / 10) * 0.24f;
                Part(p, new Vector3(px, py, -2.28f), new Vector3(0.20f, 0.18f, 0.02f),
                     C(i % 3 == 0 ? "#f0b03c" : i % 3 == 1 ? "#4ab0c0" : "#c07ad8"), 1f);
            }
            PartC(p, new Vector3(ox + cx, 3.70f, cz), new Vector3(24f, 0.36f, 0.54f), CB("#b8c4d0"), 0.024f, 1f);
            foreach (float sx in new[] { -1f, 1f })
                PartC(p, new Vector3(ox + cx + sx * 8f, 3.35f, -25.3f), new Vector3(0.50f, 0.80f, 0.50f), CB("#b8c4d0"), 0.020f, 1f);

            Window(p, new Vector3(ox + cx - 4f, 2.55f, -25.72f), 4.0f, 1.5f, 0f);
            Window(p, new Vector3(ox + cx + 4f, 2.55f, -25.72f), 4.0f, 1.5f, 0f);
            foreach (float sx in new[] { -1f, 1f })
            {
                Lamp(p, new Vector3(ox + cx + sx * 6f, W - 0.35f, cz - 6f), 18f, 1.8f, "#fff1d6");
                Lamp(p, new Vector3(ox + cx + sx * 6f, W - 0.35f, cz + 6f), 18f, 1.8f, "#fff1d6");
            }
            Lamp(p, new Vector3(ox + cx + 4.5f, W - 0.35f, cz), 10f, 1.25f, "#fff1d6");
        }

        // ═════════ 计算机实验室（西南 30.8×23.6） ═════════
        {
            float x0 = -38.88f, x1 = -8.09f, cx = -23.485f, cz = 14.1f;
            LabFloor(p, ox + x0, ox + x1, 2.29f, 25.88f, C("#4a6070"), ox + cx, cz);
            var desk = CB("#7a90a8");
            var blue = CB("#4a9ac8");
            for (int r = 0; r < 5; r++)
            {
                float z = cz - 8f + r * 4f;
                Table(p, new Vector3(ox + cx, 0f, z), 24f, 0.92f, 0.74f, desk);
                Reg(it, ox + cx, z, 24f, 0.92f);
                for (int j = 0; j < 6; j++)
                {
                    float x = ox + cx - 10f + j * 4f;
                    // 机位：键盘 + 显示器 + 主机，全部倒角；屏幕用亮青（卡通屏幕要发光才有科技感）
                    PartC(p, new Vector3(x, 0.78f, z + 0.16f), new Vector3(0.48f, 0.04f, 0.18f), CB("#4a5260"), 0.008f, 0.6f);
                    Part(p, new Vector3(x, 0.92f, z - 0.06f), new Vector3(0.08f, 0.22f, 0.08f), CB("#4a5260"), 0.6f);
                    PartC(p, new Vector3(x, 1.10f, z - 0.06f), new Vector3(0.58f, 0.38f, 0.05f), CB("#3a4250"), 0.012f, 0.6f);
                    Part(p, new Vector3(x, 1.10f, z - 0.03f), new Vector3(0.50f, 0.30f, 0.02f), C("#6ad8f0"), 0.6f);
                    PartC(p, new Vector3(x, 0.30f, z - 0.18f), new Vector3(0.22f, 0.52f, 0.42f), CB("#4a5260"), 0.014f, 0.6f);
                    Stool(p, it, x, z + 0.82f, blue);
                }
            }
            LabTeacher(p, it, ox + cx + 4.5f, 2.2f, 1, CB("#4a5260"));

            // 服务器柜
            float svx = ox + cx - 5f, svz = 25.42f;
            PartC(p, new Vector3(svx, 1.0f, svz), new Vector3(1.9f, 2.0f, 0.72f), CB("#3a4250"), 0.026f, 1f);
            for (int i = 0; i < 6; i++)
                Part(p, new Vector3(svx - 0.7f + (i % 3) * 0.7f, 0.55f + (i / 3) * 0.75f, svz - 0.37f),
                     new Vector3(0.34f, 0.08f, 0.03f), C(i % 2 == 0 ? "#6ce89a" : "#5ad8f0"), 0.5f);
            Reg(it, svx, svz, 1.9f, 0.72f);

            // 蓝色灯带 / 窗 / 灯
            foreach (float sx in new[] { -1f, 1f })
                Part(p, new Vector3(ox + (sx < 0 ? x0 : x1) - sx * 0.10f, 2.75f, cz),
                     new Vector3(0.06f, 0.06f, 18f), MatLib.UnlitColor(MatLib.Hex("#5ad8f0")), 1f);
            Window(p, new Vector3(ox + cx - 11f, 2.55f, 25.72f), 4.0f, 1.5f, 0f);
            Window(p, new Vector3(ox + cx + 11f, 2.55f, 25.72f), 4.0f, 1.5f, 0f);
            foreach (float lx in new[] { -8f, 0f, 8f })
                Lamp(p, new Vector3(ox + cx + lx, W - 0.35f, cz), 18f, 1.8f, "#dcecff");
            Lamp(p, new Vector3(ox + cx + 4.5f, W - 0.35f, 4.2f), 10f, 1.25f, "#dcecff");
        }

        // ═════════ 创客实验室（东南 30.8×23.6） ═════════
        {
            float x0 = 8.09f, x1 = 38.88f, cx = 23.485f, cz = 14.1f;
            LabFloor(p, ox + x0, ox + x1, 2.29f, 25.88f, C("#a08868"), ox + cx, cz);
            var wood = InteriorWood();
            var orange = CB("#f0883c");

            // 两侧工具工作台 + 工具墙
            foreach (float sx in new[] { -1f, 1f })
            {
                float wx = ox + cx + sx * 12.2f;
                float wallX = ox + (sx < 0 ? x0 : x1);
                for (int i = 0; i < 2; i++)
                {
                    float wz = cz - 5f + i * 10f;
                    Table(p, new Vector3(wx, 0f, wz), 1.4f, 3.4f, 0.86f, wood);
                    Reg(it, wx, wz, 1.4f, 3.4f);
                    Stool(p, it, wx - sx * 0.9f, wz - 0.8f, orange);
                    Stool(p, it, wx - sx * 0.9f, wz + 0.8f, orange);
                    // 工具墙：外框倒角 + 板面内缩，工具用高饱和四色挂块
                    PartC(p, new Vector3(wallX - sx * 0.10f, 1.95f, wz), new Vector3(0.05f, 1.41f, 2.86f), CB("#c98a4e"), 0.012f, 1f);
                    Part(p, new Vector3(wallX - sx * 0.14f, 1.95f, wz), new Vector3(0.02f, 1.35f, 2.8f), C("#f8fbf4"), 1f);
                    for (int t = 0; t < 10; t++)
                    {
                        float tz = wz - 1.15f + (t % 5) * 0.55f;
                        float ty = 1.55f + (t / 5) * 0.55f;
                        Part(p, new Vector3(wallX - sx * 0.16f, ty, tz), new Vector3(0.06f, 0.34f, 0.14f),
                             C(t % 4 == 0 ? "#e0554a" : t % 4 == 1 ? "#4a9ac8" : t % 4 == 2 ? "#f0b03c" : "#4ac86a"), 1f);
                    }
                }
            }

            // 3D 打印机 3 台（北外墙）
            for (int i = 0; i < 3; i++)
            {
                float px = ox + cx - 6f + i * 6f;
                Table(p, new Vector3(px, 0f, 24.6f), 1.7f, 1.0f, 0.74f, wood);
                Reg(it, px, 24.6f, 1.7f, 1.0f);
                PartC(p, new Vector3(px, 1.28f, 24.6f), new Vector3(0.82f, 0.96f, 0.82f), CB("#3a4250"), 0.024f, 1f);
                Part(p, new Vector3(px, 1.30f, 24.17f), new Vector3(0.66f, 0.66f, 0.03f), C("#5ac8e0"), 1f);
                PartC(p, new Vector3(px, 0.98f, 24.6f), new Vector3(0.62f, 0.05f, 0.62f), CB("#b8c4d0"), 0.008f, 1f);
                PartC(p, new Vector3(px, 1.86f, 24.6f), new Vector3(0.34f, 0.10f, 0.34f), orange, 0.010f, 1f);
                PartC(p, new Vector3(px, 1.93f, 24.6f), new Vector3(0.26f, 0.10f, 0.26f), CB("#f0b03c"), 0.010f, 1f);
                PartC(p, new Vector3(px, 1.05f, 24.6f), new Vector3(0.22f, 0.14f, 0.22f),
                      i == 1 ? CB("#f0883c") : CB("#6a7480"), 0.010f, 1f);
            }

            // 机器人场地
            float ax = ox + cx, az = cz;
            Part(p, new Vector3(ax, 0.055f, az), new Vector3(7.2f, 0.02f, 7.2f), C("#dce4ec"), 1f);
            foreach (float sx in new[] { -1f, 1f })
            {
                PartC(p, new Vector3(ax + sx * 3.55f, 0.18f, az), new Vector3(0.18f, 0.30f, 7.2f), CB("#4a9ac8"), 0.014f, 1f);
                PartC(p, new Vector3(ax, 0.18f, az + sx * 3.55f), new Vector3(7.2f, 0.30f, 0.18f), CB("#4a9ac8"), 0.014f, 1f);
            }
            Reg(it, ax - 3.55f, az, 0.18f, 7.2f);
            Reg(it, ax + 3.55f, az, 0.18f, 7.2f);
            Reg(it, ax, az - 3.55f, 7.2f, 0.18f);
            Reg(it, ax, az + 3.55f, 7.2f, 0.18f);
            for (int i = 0; i < 3; i++)
            {
                float rx = ax - 1.8f + i * 1.8f;
                PartC(p, new Vector3(rx, 0.28f, az + 0.6f - i * 0.6f), new Vector3(0.55f, 0.35f, 0.45f), orange, 0.014f, 1f);
                PartC(p, new Vector3(rx, 0.48f, az + 0.6f - i * 0.6f), new Vector3(0.30f, 0.20f, 0.26f), CB("#3a4250"), 0.010f, 1f);
                Part(p, new Vector3(rx - 0.16f, 0.16f, az + 0.6f - i * 0.6f), new Vector3(0.12f, 0.16f, 0.42f), CB("#3a4250"), 0.5f);
                Part(p, new Vector3(rx + 0.16f, 0.16f, az + 0.6f - i * 0.6f), new Vector3(0.12f, 0.16f, 0.42f), CB("#3a4250"), 0.5f);
            }
            for (int i = 0; i < 3; i++)
            {
                var ball = new GameObject("ball");
                ball.transform.SetParent(p, false);
                ball.transform.localPosition = new Vector3(ax + 1.6f - i * 1.1f, 0.16f, az - 1.4f + i * 0.5f);
                ball.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Sphere(0.16f, 8, 6);
                ball.AddComponent<MeshRenderer>().sharedMaterial = CB(i % 2 == 0 ? "#f0b03c" : "#e0554a");
            }

            // 激光切割机（东墙）
            float lx2 = ox + x1 - 0.9f;
            Table(p, new Vector3(lx2, 0f, cz), 1.6f, 2.8f, 0.80f, wood);
            Reg(it, lx2, cz, 1.6f, 2.8f);
            PartC(p, new Vector3(lx2, 1.30f, cz), new Vector3(1.2f, 1.00f, 2.2f), CB("#e0e8ec"), 0.028f, 1f);
            Part(p, new Vector3(lx2 - 0.62f, 1.42f, cz), new Vector3(0.04f, 0.50f, 1.5f), Glass(), 1f);
            PartC(p, new Vector3(lx2, 1.86f, cz), new Vector3(0.50f, 0.22f, 0.50f), CB("#b8c4d0"), 0.014f, 1f);

            // 耗材架 + 教师区
            PartC(p, new Vector3(ox + cx, 1.85f, 25.66f), new Vector3(8.0f, 0.06f, 0.38f), CB("#c98a4e"), 0.012f, 1f);
            for (int i = 0; i < 12; i++)
                PartC(p, new Vector3(ox + cx - 3.6f + i * 0.65f, 1.98f, 25.66f),
                      new Vector3(0.36f, 0.24f, 0.26f),
                      CB(i % 4 == 0 ? "#e0554a" : i % 4 == 1 ? "#4a9ac8" : i % 4 == 2 ? "#f0b03c" : "#4ac86a"), 0.010f, 1f);
            LabTeacher(p, it, ox + cx + 4.5f, 2.2f, 1, wood);

            Window(p, new Vector3(ox + cx - 11f, 2.55f, 25.72f), 4.0f, 1.5f, 0f);
            Window(p, new Vector3(ox + cx + 11f, 2.55f, 25.72f), 4.0f, 1.5f, 0f);
            foreach (float lx in new[] { -8f, 0f, 8f })
                Lamp(p, new Vector3(ox + cx + lx, W - 0.35f, cz), 18f, 1.7f, "#ffe6c0");
            Lamp(p, new Vector3(ox + cx + 4.5f, W - 0.35f, 4.2f), 10f, 1.25f, "#ffe6c0");
        }

        // ═════════ 门厅（x=-8..8） ═════════
        Part(p, new Vector3(ox, 0.040f, 14.1f), new Vector3(14.4f, 0.012f, 22.4f), T("in_tile"), 1.8f);
        Part(p, new Vector3(ox, 0.062f, 22.6f), new Vector3(3.4f, 0.012f, 6.0f), CB("#5fa8c8"), 1f);
        PartC(p, new Vector3(ox - 4.6f, 0.49f, 8.0f), new Vector3(2.6f, 0.98f, 0.8f), CB("#4a90b0"), 0.026f, 1f);
        PartC(p, new Vector3(ox - 4.6f, 1.02f, 8.0f), new Vector3(2.66f, 0.06f, 0.86f), C("#f8fbf4"), 0.012f, 1f);
        Reg(it, ox - 4.6f, 8.0f, 2.6f, 0.8f);
        Part(p, new Vector3(ox - 4.6f, 1.16f, 7.75f), new Vector3(1.9f, 0.34f, 0.04f), CB("#3f9a6a"), 1f);
        SignText(p, new Vector3(ox - 4.6f, 1.16f, 7.78f), 180f, "实验楼服务台", 0.026f, MatLib.Hex("#fff2d6"));
        PartC(p, new Vector3(ox - 5.45f, 1.22f, 8.15f), new Vector3(0.55f, 0.34f, 0.05f), CB("#4a5260"), 0.010f, 1f);
        Part(p, new Vector3(ox - 5.45f, 1.22f, 8.12f), new Vector3(0.48f, 0.28f, 0.02f), C("#6ad8f0"), 1f);
        Part(p, new Vector3(ox - 5.45f, 1.03f, 8.15f), new Vector3(0.10f, 0.18f, 0.08f), CB("#4a5260"), 1f);
        Part(p, new Vector3(ox - 3.85f, 1.01f, 8.1f), new Vector3(0.42f, 0.04f, 0.30f), C("#fff6e2"), 1f);
        Chair(p, it, new Vector3(ox - 4.6f, 0f, 9.0f), 180f, CB("#4a90b0"), true);

        // 两侧展柜：火箭 / 地球仪 / 机器人
        foreach (float sx in new[] { -1f, 1f })
        {
            float gx = ox + sx * 7.2f;
            PartC(p, new Vector3(gx, 0.55f, 13.0f), new Vector3(0.8f, 1.10f, 2.6f), CB("#b8743c"), 0.026f, 1f);
            PartC(p, new Vector3(gx, 1.16f, 13.0f), new Vector3(0.64f, 0.06f, 2.4f), C("#f8fbf4"), 0.010f, 1f);
            foreach (float dz in new[] { -1.18f, 1.18f })
                foreach (float dx in new[] { -0.34f, 0.34f })
                    Part(p, new Vector3(gx + dx, 1.55f, 13.0f + dz), new Vector3(0.04f, 0.82f, 0.04f), C("#8ad8f0"), 1f);
            PartC(p, new Vector3(gx, 1.97f, 13.0f), new Vector3(0.76f, 0.05f, 2.56f), C("#8ad8f0"), 0.008f, 1f);
            Reg(it, gx, 13.0f, 0.8f, 2.6f);
            if (sx < 0)
            {
                PartC(p, new Vector3(gx, 1.52f, 12.6f), new Vector3(0.24f, 0.55f, 0.24f), C("#f2f8fc"), 0.010f, 1f);
                PartC(p, new Vector3(gx, 1.90f, 12.6f), new Vector3(0.18f, 0.30f, 0.18f), CB("#e0554a"), 0.010f, 1f);
                var globe = new GameObject("globe");
                globe.transform.SetParent(p, false);
                globe.transform.localPosition = new Vector3(gx, 1.42f, 13.6f);
                globe.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Sphere(0.28f, 12, 8);
                globe.AddComponent<MeshRenderer>().sharedMaterial = CB("#4a90c8");
            }
            else
            {
                PartC(p, new Vector3(gx, 1.35f, 12.6f), new Vector3(0.42f, 0.36f, 0.34f), CB("#f0883c"), 0.014f, 1f);
                PartC(p, new Vector3(gx, 1.60f, 12.6f), new Vector3(0.24f, 0.18f, 0.20f), CB("#3a4250"), 0.010f, 1f);
                Part(p, new Vector3(gx - 0.14f, 1.26f, 12.6f), new Vector3(0.08f, 0.24f, 0.30f), CB("#3a4250"), 1f);
                Part(p, new Vector3(gx + 0.14f, 1.26f, 12.6f), new Vector3(0.08f, 0.24f, 0.30f), CB("#3a4250"), 1f);
            }
        }

        // 中央种植岛 + 围凳（低矮灌木 + 原子模型雕塑，不挡视线）
        PartC(p, new Vector3(ox, 0.22f, 13.0f), new Vector3(3.6f, 0.44f, 3.6f), CB("#c8b8a0"), 0.040f, 1f);
        PartC(p, new Vector3(ox, 0.47f, 13.0f), new Vector3(3.2f, 0.08f, 3.2f), CB("#8a6a4a"), 0.012f, 1f);
        Reg(it, ox, 13.0f, 3.6f, 3.6f);
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                Plant(p, new Vector3(ox + sx * 1.05f, 0.50f, 13.0f + sz * 1.05f), 0.75f);
        PartC(p, new Vector3(ox, 0.73f, 13.0f), new Vector3(0.56f, 0.44f, 0.56f), CB("#6a7480"), 0.014f, 1f);
        Part(p, new Vector3(ox, 1.15f, 13.0f), new Vector3(0.06f, 0.85f, 0.06f), C("#b8c8d8"), 1f);
        var atomCore = new GameObject("atomcore");
        atomCore.transform.SetParent(p, false);
        atomCore.transform.localPosition = new Vector3(ox, 1.55f, 13.0f);
        atomCore.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Sphere(0.30f, 12, 8);
        atomCore.AddComponent<MeshRenderer>().sharedMaterial = MatLib.UnlitColor(MatLib.Hex("#f0c03c"));
        for (int i = 0; i < 3; i++)
        {
            var orbit = new GameObject("atomring");
            orbit.transform.SetParent(p, false);
            orbit.transform.localPosition = new Vector3(ox, 1.55f, 13.0f);
            orbit.transform.localEulerAngles = new Vector3(70f, i * 60f, 0f);
            orbit.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Ring(1.0f, 1.0f, 1.06f, 1.06f, 32, 1f);
            orbit.AddComponent<MeshRenderer>().sharedMaterial = MatLib.UnlitColor(MatLib.Hex("#8ad8f0"));
        }
        foreach (float sx in new[] { -1f, 1f })
        {
            PartC(p, new Vector3(ox + sx * 3.1f, 0.42f, 13.0f), new Vector3(0.55f, 0.10f, 2.2f), CB("#c98a4e"), 0.012f, 1f);
            for (int i = 0; i < 2; i++)
                Part(p, new Vector3(ox + sx * 3.1f, 0.20f, 13.0f + (i == 0 ? -0.8f : 0.8f)),
                     new Vector3(0.10f, 0.40f, 0.46f), CB("#a06a3c"), 1f);
            Reg(it, ox + sx * 3.1f, 13.0f, 0.55f, 2.2f);
        }
        foreach (float sz in new[] { 6f, 9.5f })
        {
            Picture(p, new Vector3(ox - 7.78f, 2.1f, sz), 1.4f, 0.9f, 90f);
            Picture(p, new Vector3(ox + 7.78f, 2.1f, sz), 1.4f, 0.9f, 270f);
        }

        // 荣誉/安全公告 + 长凳 + 绿植
        foreach (float sx in new[] { -1f, 1f })
        {
            PartC(p, new Vector3(ox + sx * 7.72f, 1.65f, 19.0f), new Vector3(0.05f, 1.26f, 2.46f), CB("#4a90b0"), 0.012f, 1f);
            Part(p, new Vector3(ox + sx * 7.68f, 1.65f, 19.0f), new Vector3(0.02f, 0.98f, 2.2f), C("#f8fbf4"), 1f);
        }
        PartC(p, new Vector3(ox + 6.7f, 0.28f, 17.0f), new Vector3(0.55f, 0.10f, 2.2f), CB("#c98a4e"), 0.012f, 1f);
        Part(p, new Vector3(ox + 6.7f, 0.14f, 17.0f), new Vector3(0.10f, 0.28f, 2.0f), CB("#a06a3c"), 1f);
        Reg(it, ox + 6.7f, 17.0f, 0.55f, 2.2f);
        Plant(p, new Vector3(ox - 6.6f, 0.02f, 24.6f), 1.1f);
        Plant(p, new Vector3(ox + 6.6f, 0.02f, 24.6f), 1.1f);
        HangingSign(p, ox, 3.45f, 2.2f, 1, "实验楼 · 实验室分布", 2.4f, 0.44f, CB("#4a90b0"), 0.030f);
        Lamp(p, new Vector3(ox, W - 0.35f, 5f), 13f, 1.2f);
        Lamp(p, new Vector3(ox, W - 0.35f, 10f), 18f, 1.5f);
        Lamp(p, new Vector3(ox, W - 0.35f, 17f), 18f, 1.5f);
        Lamp(p, new Vector3(ox, W - 0.35f, 24f), 16f, 1.35f);

        // ── 走廊：储物柜 / 公告板 / 饮水机 / 端头绿植 / 灯 ──
        foreach (float sx in new[] { -1f, 1f })
        {
            float lx = ox + sx * 35.0f;
            PartC(p, new Vector3(lx, 1.0f, -1.85f), new Vector3(3.0f, 2.0f, 0.50f), CB("#8a9aa6"), 0.026f, 1f);
            for (int i = 0; i < 6; i++)
                Part(p, new Vector3(lx - 1.25f + (i % 3) * 1.25f, 1.25f - (i / 3) * 0.55f, -1.58f),
                     new Vector3(1.10f, 0.05f, 0.03f), CB("#b8c8d8"), 1f);
            // 柜门缝：6 扇门，卡通描边把长柜切成格子
            for (int i = 0; i < 4; i++)
                Part(p, new Vector3(lx - 1.5f + i * 1.0f, 1.0f, -1.59f), new Vector3(0.03f, 1.90f, 0.02f), CB("#6a7480"), 1f);
            Reg(it, lx, -1.85f, 3.0f, 0.50f);
            Plant(p, new Vector3(ox + sx * 38.2f, 0.02f, 0.0f), 1.1f);
            PartC(p, new Vector3(ox + sx * 37.6f, 0.55f, 1.5f), new Vector3(0.50f, 1.10f, 0.50f), CB("#e0e8ec"), 0.016f, 1f);
            Reg(it, ox + sx * 37.6f, 1.5f, 0.50f, 0.50f);
        }
        PartC(p, new Vector3(ox - 13f, 1.65f, -1.84f), new Vector3(2.26f, 1.16f, 0.05f), CB("#4a90b0"), 0.012f, 1f);
        Part(p, new Vector3(ox - 13f, 1.65f, -1.80f), new Vector3(2.0f, 0.9f, 0.02f), C("#f8fbf4"), 1f);
        SignText(p, new Vector3(ox - 13f, 1.65f, -1.77f), 180f, "实验室安全守则", 0.026f, MatLib.Hex("#1a3a4a"));
        WallClock(p, new Vector3(ox + 13f, 2.55f, -1.84f), 0f);
        foreach (float lx in new[] { -36f, -27f, -18f, -9f, 0f, 9f, 18f, 27f, 36f })
            Lamp(p, new Vector3(ox + lx, W - 0.35f, 0f), 12f, 1.2f);
        return it;
    }
}
