using UnityEngine;

// 老街东侧「商铺排」四间可进入室内（docs/82）：
//   奶茶店 30×20 · 照相馆 30×20 · 书店 32×22 · 便利店 32×22
// 分区按联网调研的现代门店动线（作者要求：室内尺度不得小于既有室内，故沿用「里比外大」约定）：
//   奶茶店 = 前场点单吧台（双层高差）+ 中场客座 + 后场开放操作间
//   照相馆 = 前台样片墙 → 化妆四工位 → 服装/更衣 → 后场拍摄棚（背景架 + 灯架）
//   书店   = 入口中岛+收银咖啡 → 中部书架岛回字环道 → 后场阅读长桌 + 通高书墙
//   便利店 = 前场三台收银+热食堂食 → 中部中岛货架 → 后墙冷柜带（回游动线）
public static partial class InteriorBuilder
{
    const float TEA_X = 1900f, PHOTO_X = 1980f, BOOK_X = 2070f, CONV_X = 2160f;

    // 双面中岛货架：沿 X 摆 len 米，h 一般 1.35（便利店）或 1.35（书店），货格用色块代替商品
    static void Gondola(Transform p, Interior it, float cx, float cz, float len, float h, Material frame, Material[] goods)
    {
        Part(p, new Vector3(cx, 0.09f, cz), new Vector3(len, 0.18f, 0.86f), frame, 1f);
        Part(p, new Vector3(cx, h - 0.03f, cz), new Vector3(len, 0.06f, 0.9f), frame, 1f);
        for (int i = 0; i <= 2; i++)
            Part(p, new Vector3(cx, 0.18f + i * (h - 0.24f) / 2f, cz), new Vector3(len - 0.1f, 0.05f, 0.8f), frame, 1f);
        foreach (int s in new[] { -1, 1 })
        {
            Part(p, new Vector3(cx - len / 2f + 0.05f, h / 2f, cz), new Vector3(0.1f, h, 0.9f), frame, 1f);
            Part(p, new Vector3(cx + len / 2f - 0.05f, h / 2f, cz), new Vector3(0.1f, h, 0.9f), frame, 1f);
        }
        for (int r = 0; r < 2; r++)   // 只摆两层货，顶层留空：否则货块会戳穿顶板
        {
            float y = 0.21f + r * (h - 0.36f) / 2f;
            int n = Mathf.FloorToInt((len - 0.4f) / 0.34f);
            for (int i = 0; i < n; i++)
            {
                if ((i + r * 3) % 9 == 4) continue;
                float gw = 0.2f + ((i * 5 + r) % 3) * 0.03f;
                float gh = 0.16f + ((i + r * 2) % 4) * 0.03f;
                float gx = cx - len / 2f + 0.24f + i * 0.34f;
                var m = goods[(i + r) % goods.Length];
                Part(p, new Vector3(gx, y + gh / 2f, cz + 0.2f), new Vector3(gw, gh, 0.22f), m, 0.4f);
                Part(p, new Vector3(gx + 0.14f, y + gh / 2f, cz - 0.2f), new Vector3(gw, gh, 0.22f), goods[(i + r * 2 + 1) % goods.Length], 0.4f);
            }
        }
        Reg(it, cx, cz, len, 0.9f);
    }

    // 冷藏柜带：沿 X 的 n 联玻璃门冷柜（后墙压轴，制造「走完全场」的回游动线）
    static void FridgeRun(Transform p, Interior it, float cx, float z, float len, int face, Material body, Material glow)
    {
        Part(p, new Vector3(cx, 1.0f, z - face * 0.35f), new Vector3(len, 2.0f, 0.7f), body, 1f);
        Part(p, new Vector3(cx, 2.05f, z), new Vector3(len + 0.16f, 0.14f, 0.86f), C("#8d99a6"), 1f);
        int n = Mathf.Max(2, Mathf.RoundToInt(len / 1.25f));
        for (int i = 0; i < n; i++)
        {
            float dx = cx - len / 2f + (i + 0.5f) * len / n;
            Part(p, new Vector3(dx, 1.05f, z + face * 0.03f), new Vector3(len / n - 0.12f, 1.7f, 0.04f), Glass(), 1f);   // 门要离柜面 1cm，共面会闪
            for (int s = 0; s < 3; s++)
                Part(p, new Vector3(dx, 0.45f + s * 0.5f, z + face * 0.1f), new Vector3(len / n - 0.3f, 0.05f, 0.16f), glow, 0.4f);
        }
        Reg(it, cx, z - face * 0.35f, len, 0.7f);
    }

    // 吧台：外柜 1.05m（挡操作台凌乱）+ 内台面 0.9m，高差 ≥0.25m 是奶茶店设计要点
    // 台面板下沉 2cm 压进柜体：原来板底正好等于柜顶（1.05）→ 两个共面一直在闪；
    // 内侧踏板也要离开柜体背面（原来后沿正好 = 柜背 2.15，柜台背后一条闪面缝）
    static void BarCounter(Transform p, Interior it, float cx, float cz, float len, Material body, Material top)
    {
        Part(p, new Vector3(cx, 0.525f, cz), new Vector3(len, 1.05f, 0.9f), body, 1f);
        Part(p, new Vector3(cx, 1.06f, cz), new Vector3(len + 0.1f, 0.06f, 0.96f), top, 1f);
        Part(p, new Vector3(cx, 0.72f, cz - 0.24f), new Vector3(len - 0.3f, 0.05f, 0.3f), top, 1f);
        Reg(it, cx, cz, len, 0.9f);
    }

    static Interior Room(Transform parent, string id, string name, float ox, float X, float Z, float W,
                         Material wall, Material plate, Material floorTex)
    {
        var it = Shell(parent, id, name, ox, X, Z, W, 1.5f, 2.7f,
                       wall, floorTex ?? T("in_floor"), T("in_ceil"), wall, plate, 0.58f);
        return it;
    }

    // ── 奶茶店 ──────────────────────────────────────────────
    static Interior MilkTea(Transform parent)
    {
        const float X = 15f, Z = 10f, W = 4.2f;
        var it = Room(parent, "milktea", "奶茶店", TEA_X, X, Z, W, T("in_wall2"), C("#4a6a52"), T("in_tile"));
        var p = it.Root.transform;
        float ox = TEA_X;
        var wood = T("wood");
        var woodDark = C("#6f5540");
        var cream = C("#e6ded0");
        var mint = C("#7fa89a");
        var ink = C("#33413a");

        LightGrid(p, ox - 11f, ox + 11f, -6f, 7f, W - 0.32f, 4, 3, 11f, 1.0f);
        for (int i = 0; i < 4; i++) Pendant(p, ox - 6f + i * 4f, 3.0f, 2.6f, 1.1f, "#d8a05a");

        // 前场：点单吧台（横，z=2.6）—— 顾客面在北（朝门），操作面在南（朝后场）
        BarCounter(p, it, ox - 2.5f, 2.6f, 11f, woodDark, cream);
        Part(p, new Vector3(ox + 3.6f, 0.45f, 3.9f), new Vector3(1.4f, 0.9f, 1.6f), woodDark, 1f);   // 取餐台（东北端）
        Part(p, new Vector3(ox + 3.6f, 0.92f, 3.9f), new Vector3(1.5f, 0.05f, 1.7f), cream, 1f);
        Reg(it, ox + 3.6f, 3.9f, 1.4f, 1.6f);
        for (int i = 0; i < 4; i++)
        {
            float mx = ox - 6.5f + i * 1.1f;
            // 菜单屏在顾客面一侧（吧台北缘），抬到 2.75 便于平视读、又不挡通道
            Part(p, new Vector3(mx, 2.75f, 3.5f), new Vector3(0.9f, 0.6f, 0.06f), ink, 0.5f);
            HangRods(p, mx, 3.05f, 3.5f, 0.9f);
        }
        Part(p, new Vector3(ox + 2.4f, 1.33f, 2.45f), new Vector3(0.6f, 0.5f, 0.35f), mint, 0.6f);               // 封口机（操作面，落在台面板内）
        // 展示柜：三面壳体、前面留空露甜点（不透明玻璃会把柜子糊成白盒）。
        // 整柜收在台面板范围内（台面 z 2.12~3.08），不再超出台缘。
        Part(p, new Vector3(ox - 4.2f, 1.37f, 2.30f), new Vector3(1.8f, 0.5f, 0.08f), C("#cfd8e2"), 1f);
        Part(p, new Vector3(ox - 4.2f, 1.62f, 2.60f), new Vector3(1.84f, 0.05f, 0.68f), C("#8d949c"), 1f);
        Part(p, new Vector3(ox - 4.2f, 1.11f, 2.60f), new Vector3(1.84f, 0.05f, 0.68f), C("#8d949c"), 1f);
        foreach (int s in new[] { -1, 1 })
            Part(p, new Vector3(ox - 4.2f + s * 0.87f, 1.37f, 2.60f), new Vector3(0.06f, 0.5f, 0.68f), C("#cfd8e2"), 1f);
        for (int i = 0; i < 4; i++)
            Part(p, new Vector3(ox - 4.85f + i * 0.44f, 1.26f, 2.72f), new Vector3(0.3f, 0.16f, 0.2f),
                 i % 2 == 0 ? C("#d8a05a") : C("#b06a4e"), 0.4f);
        for (int i = 0; i < 4; i++)
            Part(p, new Vector3(ox - 4.85f + i * 0.44f, 1.46f, 2.72f), new Vector3(0.3f, 0.16f, 0.2f),
                 i % 2 == 0 ? C("#e6d2a8") : C("#8a5a3c"), 0.4f);
        HangingSign(p, ox - 2.5f, 3.0f, 4.1f, 1, "点单台", 1.3f, 0.38f, C("#2f4a3a"), 0.026f);
        HangingSign(p, ox + 3.6f, 3.0f, 5.0f, 1, "取餐口", 1.3f, 0.38f, C("#8a5a3c"), 0.026f);

        // 后场：开放操作间（1.2m 半墙 + 玻璃出餐口），东端留 8.6m 通道
        Part(p, new Vector3(ox - 4f, 0.6f, -5.2f), new Vector3(20.8f, 1.2f, 0.14f), woodDark, 1f);
        Part(p, new Vector3(ox - 4f, 1.75f, -5.2f), new Vector3(20.8f, 0.9f, 0.05f), Glass(), 1f);
        Reg(it, ox - 4f, -5.2f, 20.8f, 0.2f);
        Part(p, new Vector3(ox - 1f, 0.45f, -8.4f), new Vector3(9f, 0.9f, 0.8f), C("#9aa2ac"), 1f);
        Part(p, new Vector3(ox - 1f, 0.92f, -8.4f), new Vector3(9.1f, 0.05f, 0.86f), cream, 1f);
        Reg(it, ox - 1f, -8.4f, 9f, 0.8f);
        ShelfWallX(p, it, -9.5f, ox - 6f, ox + 4f, 1, wood, new[] { mint, cream, C("#d8a05a"), C("#b06a4e") }, 2.0f, 0.8f);
        Part(p, new Vector3(ox + 9.5f, 0.9f, -8.0f), new Vector3(2.6f, 1.8f, 1.2f), C("#8d949c"), 1f);
        Reg(it, ox + 9.5f, -8.0f, 2.6f, 1.2f);
        HangingSign(p, ox + 1.5f, 3.0f, -4.6f, 1, "加料区", 1.2f, 0.36f, C("#3f5c6b"), 0.026f);
        HangingSign(p, ox + 6.2f, 3.0f, 4.4f, 1, "外卖自取", 1.4f, 0.36f, C("#8a5a3c"), 0.026f);

        // 中场客座：双人方桌 3×2（桌两侧椅子面对面）+ 西墙高脚吧台 + 东墙卡座
        // 排距留出 1.2m 横向走道，且不得压到吧台（z 2.15~3.05）与后场隔断（z -5.2）
        foreach (var (tx, tz) in new[] { (-10.0f, 0.2f), (-6.6f, 0.2f), (-3.2f, 0.2f),
                                          (-10.0f, -3.5f), (-6.6f, -3.5f), (-3.2f, -3.5f) })
        {
            Table(p, new Vector3(ox + tx, 0f, tz), 1.0f, 0.8f, 0.76f, wood);
            Reg(it, ox + tx, tz, 1.0f, 0.8f);
            Chair(p, it, new Vector3(ox + tx - 0.95f, 0f, tz), 90f, wood, true);    // 西侧，人朝东
            Chair(p, it, new Vector3(ox + tx + 0.95f, 0f, tz), 270f, wood, true);   // 东侧，人朝西
        }
        Part(p, new Vector3(ox - 13.9f, 0.55f, -1.0f), new Vector3(0.5f, 1.1f, 6.0f), wood, 1f);
        Part(p, new Vector3(ox - 13.9f, 1.12f, -1.0f), new Vector3(0.56f, 0.06f, 6.1f), cream, 1f);
        Reg(it, ox - 13.9f, -1.0f, 0.5f, 6.0f);
        foreach (float sz in new[] { -3.4f, -2.0f, -0.6f, 0.8f }) Stool(p, it, ox - 13.0f, sz, mint);
        foreach (float sz in new[] { -2.4f, 1.6f })
        {
            Sofa(p, new Vector3(ox + 13.6f, 0f, sz), 2.6f, 270f, C("#7d8a76"));
            Reg(it, ox + 13.6f, sz, 0.95f, 2.6f);
            Table(p, new Vector3(ox + 12.2f, 0f, sz), 1.2f, 0.9f, 0.76f, wood);
            Reg(it, ox + 12.2f, sz, 1.2f, 0.9f);
        }
        HangingSign(p, ox - 6.6f, 3.0f, -1.6f, 1, "堂食区", 1.2f, 0.36f, C("#4a6a52"), 0.026f);
        Plant(p, new Vector3(ox - 13.2f, 0.02f, 6.6f), 1.0f);
        Plant(p, new Vector3(ox + 13.2f, 0.02f, -4.4f), 1.0f);
        WallClock(p, new Vector3(ox, 3.1f, -Z + 0.16f), 0f);
        Window(p, new Vector3(ox - 6f, 2.3f, -Z + 0.08f), 3.0f, 1.6f, 0f);
        Window(p, new Vector3(ox + 6f, 2.3f, -Z + 0.08f), 3.0f, 1.6f, 0f);
        return it;
    }

    // ── 照相馆 ──────────────────────────────────────────────
    static Interior PhotoStudio(Transform parent)
    {
        const float X = 15f, Z = 10f, W = 4.5f;
        var it = Room(parent, "photo", "照相馆", PHOTO_X, X, Z, W, T("in_wall"), C("#3f5c6b"), T("in_floor"));
        var p = it.Root.transform;
        float ox = PHOTO_X;
        var wood = T("wood");
        var cream = C("#e6ded0");
        var ink = C("#3a3f47");
        var red = C("#a8443c");
        var blue = C("#3f6a8a");

        LightGrid(p, ox - 10f, ox + 10f, -7f, 6f, W - 0.34f, 4, 3, 12f, 1.0f);

        // 前场：接待前台 + 样片墙 + 等候沙发
        Part(p, new Vector3(ox - 8f, 0.45f, 6.4f), new Vector3(3.0f, 0.9f, 0.9f), wood, 1f);
        Part(p, new Vector3(ox - 8f, 0.93f, 6.4f), new Vector3(3.1f, 0.05f, 1.0f), cream, 1f);
        Reg(it, ox - 8f, 6.4f, 3.0f, 0.9f);
        // 前台牌：一块牌立在桌面正中、两面都有字（原来南北各立一块同样大小的牌，前后错开看着像多一层虚影）
        Part(p, new Vector3(ox - 8f, 1.09f, 6.4f), new Vector3(0.8f, 0.24f, 0.03f), C("#6f4a32"), 1f);
        SignText(p, new Vector3(ox - 8f, 1.09f, 6.42f), 180f, "前台接待", 0.019f, MatLib.Hex("#e6ded0"));
        SignText(p, new Vector3(ox - 8f, 1.09f, 6.38f), 0f, "前台接待", 0.019f, MatLib.Hex("#e6ded0"));
        Part(p, new Vector3(ox - 14.4f, 1.9f, 6.5f), new Vector3(0.06f, 2.4f, 6.0f), C("#cfc7b6"), 1f);
        for (int i = 0; i < 6; i++)
            Part(p, new Vector3(ox - 14.32f, 1.2f + (i / 3) * 1.1f, 4.6f + (i % 3) * 1.3f),
                 new Vector3(0.03f, 0.86f, 1.1f), i % 2 == 0 ? blue : cream, 0.5f);
        HangingSign(p, ox - 11.5f, 3.3f, 6.5f, 1, "样片墙", 1.2f, 0.36f, C("#3f5c6b"), 0.026f);
        Sofa(p, new Vector3(ox + 10.5f, 0f, 7.2f), 2.6f, 180f, C("#7a5a6a"));
        Reg(it, ox + 10.5f, 7.2f, 1.0f, 2.6f);
        Table(p, new Vector3(ox + 10.5f, 0f, 5.7f), 1.2f, 0.6f, 0.42f, wood);
        Reg(it, ox + 10.5f, 5.7f, 1.2f, 0.6f);
        // 选片区（靠前台，便于沟通）
        for (int i = 0; i < 2; i++)
        {
            Table(p, new Vector3(ox + 3.5f + i * 2.0f, 0f, 6.0f), 1.6f, 0.7f, 0.75f, wood);
            Reg(it, ox + 3.5f + i * 2.0f, 6.0f, 1.6f, 0.7f);
            Chair(p, it, new Vector3(ox + 3.5f + i * 2.0f, 0f, 5.1f), 0f, wood, true);    // 南侧，人朝北
            Chair(p, it, new Vector3(ox + 3.5f + i * 2.0f, 0f, 6.9f), 180f, wood, true);  // 北侧，人朝南
            Part(p, new Vector3(ox + 3.5f + i * 2.0f, 0.82f, 6.0f), new Vector3(0.5f, 0.35f, 0.36f), ink, 0.5f);   // 选片屏
        }
        HangingSign(p, ox + 4.5f, 3.2f, 4.4f, 1, "选片区", 1.1f, 0.34f, C("#8a5a3c"), 0.026f);

        // 中场：化妆四工位（西墙）+ 服装区/更衣室（东墙）
        for (int i = 0; i < 4; i++)
        {
            float mz = 2.4f - i * 1.9f;
            Part(p, new Vector3(ox - 13.3f, 0.38f, mz), new Vector3(1.2f, 0.76f, 0.6f), wood, 1f);
            Part(p, new Vector3(ox - 13.3f, 0.78f, mz), new Vector3(1.3f, 0.05f, 0.66f), cream, 1f);
            Part(p, new Vector3(ox - 14.3f, 1.6f, mz), new Vector3(0.05f, 1.2f, 0.8f), Glass(), 1f);
            Reg(it, ox - 13.6f, mz, 1.6f, 0.8f);
            Stool(p, it, ox - 12.4f, mz, red);
            for (int s = 0; s < 3; s++)
                Part(p, new Vector3(ox - 14.24f, 2.35f, mz - 0.25f + s * 0.25f), new Vector3(0.04f, 0.14f, 0.1f), C("#d8c8a8"), 0.4f);
        }
        HangingSign(p, ox - 11.0f, 3.2f, 0.4f, 1, "化妆间", 1.2f, 0.36f, C("#4a6a52"), 0.026f);
        // 服装架：一根挂杆 + 两端立柱 + 一排样衣（拿衣服 → 更衣 → 拍摄，动线自北向南）
        Part(p, new Vector3(ox + 14.2f, 1.62f, 3.0f), new Vector3(0.1f, 0.06f, 4.0f), C("#8d99a6"), 1f);
        foreach (int s in new[] { -1, 1 })
            Part(p, new Vector3(ox + 14.2f, 0.81f, 3.0f + s * 1.95f), new Vector3(0.1f, 1.62f, 0.1f), C("#8d99a6"), 1f);
        for (int i = 0; i < 11; i++)
            Part(p, new Vector3(ox + 14.18f, 1.15f, 1.25f + i * 0.34f), new Vector3(0.42f, 0.9f, 0.16f),
                 i % 3 == 0 ? red : (i % 3 == 1 ? blue : C("#6b7a4a")), 0.5f);
        Reg(it, ox + 14.2f, 3.0f, 0.6f, 4.1f);

        // 更衣隔间：两间贴东墙的开面隔间。正面只有帘子（不登记碰撞），
        // 只把背板和两侧板算成实体，否则整间被封成一块空气墙，人进不去。
        var board = C("#c9bfae");
        foreach (float fz in new[] { -1.0f, -3.2f })
        {
            Part(p, new Vector3(ox + 14.78f, 1.3f, fz), new Vector3(0.1f, 2.6f, 1.7f), board, 1f);
            Reg(it, ox + 14.78f, fz, 0.12f, 1.72f);
            foreach (int s in new[] { -1, 1 })
            {
                Part(p, new Vector3(ox + 13.9f, 1.3f, fz + s * 0.85f), new Vector3(1.76f, 2.6f, 0.08f), board, 1f);
                Reg(it, ox + 13.9f, fz + s * 0.85f, 1.78f, 0.1f);
            }
            // 帘杆 + 两片布帘：中间留空，看得见脚也进得去人
            Part(p, new Vector3(ox + 12.95f, 2.52f, fz), new Vector3(0.06f, 0.06f, 1.8f), C("#8d99a6"), 1f);
            foreach (int s in new[] { -1, 1 })
                Part(p, new Vector3(ox + 12.95f, 1.35f, fz + s * 0.55f), new Vector3(0.07f, 2.3f, 0.55f), C("#7a5a6a"), 1f);
            // 带框试衣镜（背板内面）+ 镜上小搁架 + 侧板挂钩
            Part(p, new Vector3(ox + 14.7f, 1.2f, fz), new Vector3(0.05f, 1.9f, 1.1f), C("#6f5540"), 1f);
            Part(p, new Vector3(ox + 14.66f, 1.16f, fz), new Vector3(0.03f, 1.72f, 0.92f), Glass(), 1f);
            Part(p, new Vector3(ox + 14.62f, 2.28f, fz), new Vector3(0.24f, 0.05f, 1.1f), C("#6f5540"), 1f);
            Part(p, new Vector3(ox + 13.95f, 1.62f, fz + 0.78f), new Vector3(0.5f, 0.05f, 0.06f), C("#8d99a6"), 1f);
            // 门口踏脚垫
            Rug(p, new Vector3(ox + 12.6f, 0.045f, fz), 0.7f, 1.4f, C("#6b5a52"));
        }
        HangingSign(p, ox + 11.8f, 3.2f, -2.1f, 1, "服装间", 1.2f, 0.36f, C("#8a5a3c"), 0.026f);

        // 后场：拍摄棚，三个背景位（背景纸宽 2.7m、横杆 2.8m，留出挂灯高度）
        for (int i = 0; i < 3; i++)
        {
            float bx = ox - 8f + i * 8f;
            Part(p, new Vector3(bx, 1.4f, -9.3f), new Vector3(2.7f, 2.8f, 0.12f), i == 1 ? cream : (i == 0 ? C("#8a9aa8") : C("#c8a89a")), 1f);
            Part(p, new Vector3(bx, 2.85f, -9.3f), new Vector3(3.1f, 0.12f, 0.3f), ink, 1f);
            foreach (int s in new[] { -1, 1 })
                Part(p, new Vector3(bx + s * 1.5f, 1.4f, -9.3f), new Vector3(0.1f, 2.8f, 0.1f), ink, 1f);
            Reg(it, bx, -9.3f, 3.1f, 0.4f);
            // 落地灯架：三脚 + 立杆 + 柔光箱
            foreach (int s in new[] { -1, 1 })
            {
                Part(p, new Vector3(bx + s * 1.1f, 0.9f, -6.6f), new Vector3(0.08f, 1.8f, 0.08f), C("#5a5f68"), 1f);
                Part(p, new Vector3(bx + s * 1.1f, 0.05f, -6.6f), new Vector3(0.7f, 0.1f, 0.7f), C("#4a4f58"), 1f);
                Part(p, new Vector3(bx + s * 1.1f, 1.85f, -6.6f), new Vector3(0.75f, 0.75f, 0.3f), cream, 0.6f);
                Reg(it, bx + s * 1.1f, -6.6f, 0.7f, 0.7f);
            }
        }
        Part(p, new Vector3(ox, 0.06f, -6.0f), new Vector3(20f, 0.02f, 6.5f), C("#c9c2b4"), 1.5f);   // 拍摄区地胶
        HangingSign(p, ox - 8f, 3.3f, -7.4f, 1, "拍摄一区", 1.4f, 0.36f, C("#3f5c6b"), 0.026f);
        HangingSign(p, ox, 3.3f, -7.4f, 1, "拍摄二区", 1.4f, 0.36f, C("#3f5c6b"), 0.026f);
        HangingSign(p, ox + 8f, 3.3f, -7.4f, 1, "证件照", 1.2f, 0.36f, C("#8a5a3c"), 0.026f);
        Plant(p, new Vector3(ox + 13.4f, 0.02f, 8.4f), 0.9f);
        return it;
    }

    // ── 书店 ────────────────────────────────────────────────
    static Interior Bookstore(Transform parent)
    {
        const float X = 16f, Z = 11f, W = 4.5f;
        var it = Room(parent, "book", "书店", BOOK_X, X, Z, W, T("in_wall2"), C("#6a3a3c"), T("in_floor"));
        var p = it.Root.transform;
        float ox = BOOK_X;
        var wood = T("wood");
        var woodDark = C("#6f5540");
        var cream = C("#e6ded0");
        var green = C("#4a6a52");
        var bookCols = new[] { C("#8c4a3c"), C("#3f5c6b"), C("#8a7440"), C("#6b7a4a"), C("#a9a08a"), C("#5b4a6b") };

        LightGrid(p, ox - 12f, ox + 12f, -8f, 8f, W - 0.34f, 5, 4, 12f, 0.95f);

        // 入口段：中岛新书台 + 收银咖啡一体台
        for (int i = 0; i < 2; i++)
        {
            Part(p, new Vector3(ox - 5.5f - i * 3.2f, 0.4f, 8.0f), new Vector3(1.2f, 0.8f, 1.2f), wood, 1f);
            Part(p, new Vector3(ox - 5.5f - i * 3.2f, 0.83f, 8.0f), new Vector3(1.3f, 0.05f, 1.3f), cream, 1f);
            for (int s = 0; s < 3; s++) BookStack(p, new Vector3(ox - 5.8f - i * 3.2f + s * 0.35f, 0.9f, 8.0f), 3);
            Reg(it, ox - 5.5f - i * 3.2f, 8.0f, 1.3f, 1.3f);
        }
        HangingSign(p, ox - 7.1f, 3.2f, 6.6f, 1, "新书速递", 1.4f, 0.36f, C("#8a5a3c"), 0.026f);
        Part(p, new Vector3(ox + 7f, 0.5f, 8.2f), new Vector3(4.0f, 1.0f, 1.2f), woodDark, 1f);
        Part(p, new Vector3(ox + 7f, 1.03f, 8.2f), new Vector3(4.1f, 0.05f, 1.3f), cream, 1f);
        Part(p, new Vector3(ox + 8.6f, 1.35f, 8.15f), new Vector3(0.7f, 0.6f, 0.6f), C("#9aa2ac"), 1f);   // 咖啡机
        Reg(it, ox + 7f, 8.2f, 4.0f, 1.2f);
        HangingSign(p, ox + 7f, 3.2f, 6.9f, 1, "收银台", 1.2f, 0.36f, C("#2f4a3a"), 0.026f);

        // 中部：5 排双面书架岛（中岛 ≤1.6m 保证视线通透），排间距 1.3m
        for (int r = 0; r < 5; r++)
            Gondola(p, it, ox - 1f, 3.4f - r * 2.2f, 9.0f, 1.35f, woodDark, bookCols);
        HangingSign(p, ox - 1f, 3.2f, 4.6f, 1, "畅销榜", 1.2f, 0.36f, C("#4a6a52"), 0.026f);

        // 后场：通高书墙（爆点）+ 阅读长桌 + 咖啡角 + 儿童区
        ShelfWallX(p, it, -Z + 0.45f, ox - 15f, ox + 2f, 1, woodDark, bookCols, 2.45f);
        ShelfWallZ(p, it, ox - X + 0.45f, -Z + 1f, -1f, 1, woodDark, bookCols, 2.45f);
        for (int i = 0; i < 3; i++)
        {
            float rz = -3.0f + i * 1.9f;
            Table(p, new Vector3(ox + 7.5f, 0f, rz), 2.4f, 0.9f, 0.75f, wood);
            Reg(it, ox + 7.5f, rz, 2.4f, 0.9f);
            BenchX(p, it, ox + 7.5f, rz - 0.85f, 2.4f);
            BenchX(p, it, ox + 7.5f, rz + 0.85f, 2.4f);
            TableLamp(p, new Vector3(ox + 7.5f, 0.79f, rz), "#e3d3a8");
        }
        Part(p, new Vector3(ox + 13.5f, 0.5f, 3.6f), new Vector3(3.6f, 1.0f, 1.2f), woodDark, 1f);
        Part(p, new Vector3(ox + 13.5f, 1.03f, 3.6f), new Vector3(3.7f, 0.05f, 1.3f), cream, 1f);
        Reg(it, ox + 13.5f, 3.6f, 3.6f, 1.2f);
        HangingSign(p, ox + 13.5f, 3.2f, 2.4f, 1, "咖啡角", 1.2f, 0.36f, C("#8a5a3c"), 0.026f);
        Gondola(p, it, ox + 12.5f, -8.4f, 5.0f, 1.0f, C("#8a9a7a"), bookCols);   // 儿童矮架
        HangingSign(p, ox + 12.5f, 2.6f, -7.2f, 1, "儿童区", 1.2f, 0.36f, C("#4a6a52"), 0.026f);
        Sofa(p, new Vector3(ox - 11f, 0f, -8.6f), 2.4f, 0f, C("#7a5a6a"));
        Reg(it, ox - 11f, -8.6f, 2.4f, 1.0f);
        Rug(p, new Vector3(ox - 9.5f, 0.055f, -7.0f), 4.6f, 3.4f, C("#8a6a5a"));
        Plant(p, new Vector3(ox + 15.0f, 0.02f, 9.6f), 1.0f);
        Plant(p, new Vector3(ox - 15.0f, 0.02f, 9.6f), 1.0f);
        WallClock(p, new Vector3(ox, 3.2f, -Z + 0.16f), 0f);
        return it;
    }

    // ── 便利店 ──────────────────────────────────────────────
    static Interior ConvStore(Transform parent)
    {
        const float X = 16f, Z = 11f, W = 4.2f;
        var it = Room(parent, "conv", "便利店", CONV_X, X, Z, W, T("in_wall"), C("#2f4a3a"), T("in_tile"));
        var p = it.Root.transform;
        float ox = CONV_X;
        var cream = C("#e6ded0");
        var steel = C("#9aa2ac");
        var green = C("#4a8743");
        var red = C("#c0503f");
        var goods = new[] { C("#d8a05a"), C("#7fa8b8"), C("#c0503f"), C("#8a7440"), C("#4a8743"), C("#e0d0a8") };

        LightGrid(p, ox - 12f, ox + 12f, -8f, 8f, W - 0.3f, 5, 4, 11f, 1.05f);

        // 前场：三台收银横在入口带（视线可同时管出入口与主通道）
        for (int i = 0; i < 3; i++)
        {
            float cx = ox - 9f + i * 3.0f;
            Part(p, new Vector3(cx, 0.4f, 7.4f), new Vector3(1.5f, 0.8f, 0.6f), steel, 1f);
            Part(p, new Vector3(cx, 0.83f, 7.4f), new Vector3(1.6f, 0.05f, 0.7f), cream, 1f);
            Part(p, new Vector3(cx - 0.35f, 1.05f, 7.4f), new Vector3(0.4f, 0.4f, 0.3f), C("#3a3f47"), 0.5f);
            Reg(it, cx, 7.4f, 1.6f, 0.7f);
        }
        HangingSign(p, ox - 9f, 2.9f, 6.2f, 1, "收银台", 1.2f, 0.36f, C("#2f4a3a"), 0.026f);
        // 热食台（关东煮 + 油炸）与即食堂食高脚桌
        Part(p, new Vector3(ox + 6f, 0.45f, 7.4f), new Vector3(3.6f, 0.9f, 0.8f), steel, 1f);
        Part(p, new Vector3(ox + 6f, 0.93f, 7.4f), new Vector3(3.7f, 0.05f, 0.9f), cream, 1f);
        for (int i = 0; i < 4; i++)
            Part(p, new Vector3(ox + 4.7f + i * 0.9f, 1.05f, 7.4f), new Vector3(0.7f, 0.2f, 0.7f), i % 2 == 0 ? green : red, 0.5f);
        Reg(it, ox + 6f, 7.4f, 3.6f, 0.8f);
        HangingSign(p, ox + 6f, 2.9f, 6.2f, 1, "热食区", 1.2f, 0.36f, C("#8a5a3c"), 0.026f);
        for (int i = 0; i < 3; i++)
        {
            float hx = ox + 10.2f + i * 1.7f;
            Part(p, new Vector3(hx, 0.53f, 5.4f), new Vector3(0.75f, 1.05f, 0.75f), C("#8a7a66"), 1f);
            Part(p, new Vector3(hx, 1.07f, 5.4f), new Vector3(0.85f, 0.05f, 0.85f), cream, 1f);
            Reg(it, hx, 5.4f, 0.85f, 0.85f);
            Stool(p, it, hx, 4.4f, red);
        }
        HangingSign(p, ox + 11.6f, 2.9f, 6.6f, 1, "即食堂食", 1.4f, 0.36f, C("#4a6a52"), 0.024f);

        // 中部：4 排中岛（1.35m，与靠墙 2.0m 形成天际线台阶），排间距 1.4m
        for (int r = 0; r < 4; r++)
            Gondola(p, it, ox - 2f, 2.4f - r * 2.4f, 11.0f, 1.35f, C("#7d848c"), goods);
        // 左右墙单面架 2.0m
        ShelfWallZ(p, it, ox - X + 0.45f, -6f, 3.0f, 1, C("#7d848c"), goods, 2.0f, 0.85f);
        ShelfWallZ(p, it, ox + X - 0.45f, -6f, 1.0f, -1, C("#7d848c"), goods, 2.0f, 0.85f);
        // 后墙冷柜带（回游动线的终点）
        FridgeRun(p, it, ox - 3f, -Z + 0.95f, 15.0f, 1, C("#cfd6dc"), C("#8fd0e0"));
        Part(p, new Vector3(ox + 7.5f, 0.45f, -Z + 0.65f), new Vector3(5.0f, 0.9f, 0.8f), C("#cfd6dc"), 1f);   // 卧式冷冻柜
        Part(p, new Vector3(ox + 7.5f, 0.92f, -Z + 0.65f), new Vector3(5.1f, 0.06f, 0.9f), C("#dfe6e6"), 1f);
        Reg(it, ox + 7.5f, -Z + 0.65f, 5.0f, 0.8f);
        HangingSign(p, ox - 3f, 2.9f, -8.4f, 1, "冷藏柜", 1.2f, 0.36f, C("#3f5c6b"), 0.026f);
        HangingSign(p, ox + 7.5f, 2.9f, -8.4f, 1, "冰品区", 1.2f, 0.36f, C("#3f5c6b"), 0.026f);
        // 杂志架 + 报刊墙 + 绿植
        Part(p, new Vector3(ox + 14.6f, 0.72f, 2.2f), new Vector3(0.5f, 1.45f, 1.4f), C("#b9b0a0"), 1f);
        Reg(it, ox + 14.6f, 2.2f, 0.5f, 1.4f);
        // 报刊架：收到西墙一角，避免和冷柜带（x 从 -10.5 起）叠在同一处
        ShelfWallX(p, it, -Z + 0.45f, ox - 15f, ox - 11.6f, 1, C("#7d848c"), goods, 1.6f, 0.9f);
        Plant(p, new Vector3(ox - 14.6f, 0.02f, 9.4f), 0.9f);
        Plant(p, new Vector3(ox + 14.6f, 0.02f, 9.4f), 0.9f);
        WallClock(p, new Vector3(ox, 3.1f, -Z + 0.16f), 0f);
        return it;
    }
}
