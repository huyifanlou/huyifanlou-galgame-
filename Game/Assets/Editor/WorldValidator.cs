using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 世界布点校验器（C# 版，2026-10-16 由 tools/validate-world.mjs 移植；JS 版已退役到 tools/legacy-js/）
// 用法：编辑菜单「Campus/校验世界布点」，或命令行 -executeMethod WorldValidator.RunBatch
public static class WorldValidator
{
    struct Rect { public float x, z, hw, hd; public string id; }
    static Rect RC(float x, float z, float hw, float hd, string id = null) => new Rect { x = x, z = z, hw = hw, hd = hd, id = id };
    static bool Overlap(Rect a, Rect b, float pad) =>
        Mathf.Abs(a.x - b.x) < a.hw + b.hw + pad && Mathf.Abs(a.z - b.z) < a.hd + b.hd + pad;

    // 正门点与外法向：face 1/-1 沿 Z，2/-2 沿 X（旧代码只算了 ±1，朝 ±X 的建筑会算错门前走廊）
    static void DoorOf(BuildingDef b, out float dx, out float dz, out float nx, out float nz)
    {
        nx = b.face == 2 ? 1f : b.face == -2 ? -1f : 0f;
        nz = b.face == 1 ? 1f : b.face == -1 ? -1f : 0f;
        float off = b.FacesX ? b.HalfWX : b.HalfWZ;
        dx = b.pos[0] + nx * (off + 0.9f);
        dz = b.pos[1] + nz * (off + 0.9f);
    }
    // 点到步道中心线的距离（减去半宽；步道是带朝向的长条 plaza 道具）
    static float PathDist(float px, float pz, PropDef p)
    {
        float hx = Mathf.Sin(p.ry) * p.d / 2f, hz = Mathf.Cos(p.ry) * p.d / 2f;
        float sx = p.x - hx, sz = p.z - hz, vx = 2 * hx, vz = 2 * hz;
        float wx = px - sx, wz = pz - sz;
        float t = Mathf.Clamp01((wx * vx + wz * vz) / Mathf.Max(0.0001f, vx * vx + vz * vz));
        float ddx = px - (sx + vx * t), ddz = pz - (sz + vz * t);
        return Mathf.Sqrt(ddx * ddx + ddz * ddz) - p.w / 2f;
    }
    // 门前走廊矩形：从正门向外 reach 米、横向 half 米
    static Rect DoorCorr(BuildingDef b, float reach, float half)
    {
        DoorOf(b, out float dx, out float dz, out float nx, out float nz);
        float ah = reach / 2f + 0.5f, cx = dx + nx * reach / 2f, cz = dz + nz * reach / 2f;
        return Mathf.Abs(nx) > 0.5f ? RC(cx, cz, ah, half) : RC(cx, cz, half, ah);
    }

    public static void RunBatch() { Run(); }

    // 校验对象＝游戏默认构建的世界（TownData）；旧校园数据集（-campus 启动时才用）走 RunCampus()
    [MenuItem("Campus/校验世界布点（当前世界）")]
    public static void Run() => Validate(TownData.Build());
    public static void RunCampus() => Validate(CampusData.Build());

    static void Validate(WorldData W)
    {
        var errors = new List<string>();
        var warns = new List<string>();

        // —— 轮廓集合 ——
        var bFoot = new List<Rect>();
        foreach (var b in W.BUILDINGS) bFoot.Add(RC(b.pos[0], b.pos[1], b.HalfWX, b.HalfWZ, b.id));
        var gFoot = new List<Rect>();
        foreach (var g in W.GROUNDS) gFoot.Add(RC(g.pos[0], g.pos[1], g.size[0] / 2, g.size[1] / 2, g.id));
        var roadB = new List<Rect>();
        foreach (var r in W.ROADS) roadB.Add(r.axis == "z"
            ? RC(r.at, (r.from + r.to) / 2, r.width / 2, (r.to - r.from) / 2, r.id)
            : RC((r.from + r.to) / 2, r.at, (r.to - r.from) / 2, r.width / 2, r.id));
        // 池塘实际是椭圆（rx=r+0.6，rz=0.7(r+0.6)）；按矩形判定会把东西两侧草坪误判成水面
        bool HitsPond(Rect b, float pad)
        {
            float rx = W.POND.r + 0.6f + pad, rz = (W.POND.r + 0.6f) * 0.7f + pad;
            float nx = Mathf.Clamp(W.POND.pos[0], b.x - b.hw, b.x + b.hw);
            float nz = Mathf.Clamp(W.POND.pos[1], b.z - b.hd, b.z + b.hd);
            float ex = (nx - W.POND.pos[0]) / rx, ez = (nz - W.POND.pos[1]) / rz;
            return ex * ex + ez * ez < 1f;
        }
        // 主角家的院落只在正门一侧（前院木篱在 d/2+3），背面按建筑本体算，避免误判邻街铺面
        float vySign = W.VILLA.face == 1 ? 1f : -1f;
        var villa = RC(W.VILLA.x, W.VILLA.z + vySign * 2.3f, W.VILLA.w / 2 + 1.2f, W.VILLA.d / 2 + 2.3f, "villa");
        const float villaPad = 0.5f;   // 主角家的院落矩形已含 4.6m 退让，再叠 1m 会把邻街铺面误判

        // 道具轮廓
        var pFoot = new List<Rect>();
        foreach (var p in W.PROPS)
        {
            if (p.type == "plaza" || p.type == "plaza_round" || p.type == "parking") continue;   // 铺装/圆形广场/停车场是地面，其上允许摆设施
            float hw = 0.5f, hd = 0.5f;
            switch (p.type)
            {
                case "bench": hw = 1.0f; hd = 0.6f; break;
                case "bin": hw = 0.35f; hd = 0.35f; break;
                case "board": hw = 0.9f; hd = 0.35f; break;
                case "bikerack": hw = (p.n > 0 ? p.n : 5) * 0.35f; hd = 0.5f; break;
                case "flag": hw = 0.3f; hd = 0.3f; break;
                case "planter": hw = p.w / 2; hd = p.d / 2; break;
                case "fountain": hw = p.r; hd = p.r; break;
                case "hoop": hw = 0.4f; hd = 0.4f; break;
                case "netpost": hw = (p.len > 0 ? p.len : 8f) / 2f; hd = 0.3f; break;
                case "gate":
                    bool alongZ = p.ry == 0f || Mathf.Abs(p.ry) == Mathf.PI;
                    if (p.kind == "main") { hw = alongZ ? 4.2f : 0.9f; hd = alongZ ? 0.9f : 4.2f; }
                    else { hw = 4.4f; hd = 0.9f; }
                    break;
            }
            pFoot.Add(RC(p.x, p.z, hw, hd, p.type + "@" + p.x + "," + p.z));
        }
        var lampF = new List<Rect>();
        foreach (var l in W.LAMPS) lampF.Add(RC(l.x, l.z, 0.15f, 0.15f, "lamp"));   // 灯柱实际半宽 0.08，取 0.15 更贴近实体

        // 1) 建筑间距（校园栋距 ≥5m；小镇沿街铺面是连排骑楼，只要求 ≥0.5m 不贴死）
        //    建筑 vs 场地/道路/池塘/家
        for (int i = 0; i < bFoot.Count; i++)
        {
            for (int j = i + 1; j < bFoot.Count; j++)
            {
                bool campusPair = bFoot[i].id.StartsWith("s_") && bFoot[j].id.StartsWith("s_");
                float pad = campusPair ? 5.0f : 0.5f;
                if (Overlap(bFoot[i], bFoot[j], pad))
                    errors.Add($"建筑过近：{bFoot[i].id} × {bFoot[j].id}（要求 ≥{(campusPair ? 5 : 0.5)}m）");
            }
            foreach (var g in gFoot)
            {
                // 看台按定义就贴着跑道边缘（docs/77），不适用「建筑压场地」退让
                if (bFoot[i].id == "s_stand" && g.id == "playground") continue;
                if (Overlap(bFoot[i], g, 1.5f)) errors.Add($"建筑压场地：{bFoot[i].id} × {g.id}");
            }
            foreach (var r in roadB) if (Overlap(bFoot[i], r, 1.0f)) errors.Add($"建筑压路：{bFoot[i].id} × {r.id}");
            if (HitsPond(bFoot[i], 1.0f)) errors.Add($"建筑压池塘：{bFoot[i].id}");
            if (Overlap(bFoot[i], villa, villaPad)) errors.Add($"建筑压主角家：{bFoot[i].id}");
        }
        // 2) 道路相互（正交交叉合法）
        for (int i = 0; i < roadB.Count; i++)
            for (int j = i + 1; j < roadB.Count; j++)
            {
                bool aV = roadB[i].hw < roadB[i].hd, bV = roadB[j].hw < roadB[j].hd;
                if (aV != bV) continue;
                if (Overlap(roadB[i], roadB[j], 0.5f)) errors.Add($"道路重叠：{roadB[i].id} × {roadB[j].id}");
            }
        // 3) 场地 vs 道路/池塘/家 + 相互
        for (int i = 0; i < gFoot.Count; i++)
        {
            foreach (var r in roadB) if (Overlap(gFoot[i], r, 1.0f)) errors.Add($"场压路：{gFoot[i].id} × {r.id}");
            if (HitsPond(gFoot[i], 0.5f)) errors.Add($"场地压池塘：{gFoot[i].id}");
            if (Overlap(gFoot[i], villa, 1.0f)) errors.Add($"场地压主角家：{gFoot[i].id}");
            for (int j = i + 1; j < gFoot.Count; j++)
                if (Overlap(gFoot[i], gFoot[j], 1.5f)) errors.Add($"场地重叠：{gFoot[i].id} × {gFoot[j].id}");
        }
        // 4) 道具 vs 建筑/道路/道具
        //    豁免（都是设计意图，不是布点错误）：
        //      · 步行街（无侧石：夏日祭参道、神社石阶）本来就用来摆摊位
        //      · 提灯串/幡是架空悬挂物，不占地面
        //      · 手水舍是敞亭，净手盘/绘马架/提灯/长凳就设在亭下
        //      · 旗杆落在看台中央的升旗台上（docs/77）
        var walkStreets = new HashSet<string>();
        foreach (var r in W.ROADS) if (!r.sidewalk) walkStreets.Add(r.id);
        var fairProps = new HashSet<string> { "stall", "festival_board", "crates" };
        var hungProps = new HashSet<string> { "chochin_string", "nobori" };
        var pavilionProps = new HashSet<string> { "temizuya", "ema", "omikuji", "offering", "sake", "lantern", "komainu", "bench" };
        for (int i = 0; i < pFoot.Count; i++)
        {
            string ptype = pFoot[i].id.Split('@')[0];
            foreach (var b in bFoot)
            {
                if (b.id == "k_chozu" && pavilionProps.Contains(ptype)) continue;
                if (b.id == "s_stand" && ptype == "flag") continue;
                if (Overlap(pFoot[i], b, 0.6f)) errors.Add($"道具压建筑：{pFoot[i].id} × {b.id}");
            }
            if (!pFoot[i].id.StartsWith("gate") && !hungProps.Contains(ptype))
                foreach (var r in roadB)
                {
                    if (fairProps.Contains(ptype) && walkStreets.Contains(r.id)) continue;
                    if (Overlap(pFoot[i], r, 0.4f)) errors.Add($"道具压路：{pFoot[i].id} × {r.id}");
                }
            for (int j = i + 1; j < pFoot.Count; j++)
                if (Overlap(pFoot[i], pFoot[j], 0.5f)) errors.Add($"道具重叠：{pFoot[i].id} × {pFoot[j].id}");
        }
        // 5) 围栏：不压路/建筑 + 门前通路不被阻断
        foreach (var f in W.FENCES)
        {
            var line = f.axis == "z" ? RC(f.at, (f.from + f.to) / 2, 0.2f, (f.to - f.from) / 2) : RC((f.from + f.to) / 2, f.at, (f.to - f.from) / 2, 0.2f);
            foreach (var r in roadB) if (Overlap(line, r, -0.2f)) errors.Add($"围栏压路：{f.axis}@{f.at} × {r.id}");
            foreach (var b in bFoot) if (Overlap(line, b, 0.2f)) errors.Add($"围栏压建筑：{f.axis}@{f.at} × {b.id}");
            foreach (var p in pFoot)
                if (!p.id.StartsWith("gate") && Overlap(p, line, 0.05f)) errors.Add($"道具压围栏：{p.id} × 围栏{f.axis}@{f.at}");
        }
        foreach (var b in W.BUILDINGS)
        {
            var corr = DoorCorr(b, 12f, 0.3f);
            foreach (var f in W.FENCES)
            {
                var line = f.axis == "z" ? RC(f.at, (f.from + f.to) / 2, 0.2f, (f.to - f.from) / 2) : RC((f.from + f.to) / 2, f.at, (f.to - f.from) / 2, 0.2f);
                if (Overlap(corr, line, 0f)) errors.Add($"门前被围栏阻断：{b.id}");
            }
        }
        // 6) 路灯：不贴路/不压建筑/不压围栏/不互相重叠/不挡门前通路
        for (int i = 0; i < lampF.Count; i++)
        {
            var l = lampF[i];
            for (int ri = 0; ri < roadB.Count; ri++)
            {
                var r = roadB[ri];
                bool vertical = r.hw < r.hd;
                float d = vertical ? Mathf.Abs(l.x - r.x) : Mathf.Abs(l.z - r.z);
                float half = vertical ? r.hw : r.hd;
                bool along = vertical ? (l.z > r.z - r.hd - 1 && l.z < r.z + r.hd + 1) : (l.x > r.x - r.hw - 1 && l.x < r.x + r.hw + 1);
                if (along && d < half + 0.3f) errors.Add($"路灯贴路：({l.x},{l.z}) × {r.id} 仅 {d:F2}m");
            }
            foreach (var b in bFoot) if (Overlap(l, b, 0.3f)) errors.Add($"路灯压建筑：({l.x},{l.z}) × {b.id}");
            foreach (var f in W.FENCES)
            {
                var line = f.axis == "z" ? RC(f.at, (f.from + f.to) / 2, 0.2f, (f.to - f.from) / 2) : RC((f.from + f.to) / 2, f.at, (f.to - f.from) / 2, 0.2f);
                if (Overlap(l, line, 0f)) errors.Add($"路灯压围栏：({l.x},{l.z})");
            }
            for (int j = i + 1; j < lampF.Count; j++)
                if (Overlap(l, lampF[j], 0.2f)) errors.Add($"路灯重叠：({l.x},{l.z}) × ({lampF[j].x},{lampF[j].z})");
            foreach (var b in W.BUILDINGS)
            {
                var corr = DoorCorr(b, 10f, 1.3f);
                if (Mathf.Abs(l.x - corr.x) < corr.hw + 0.3f && Mathf.Abs(l.z - corr.z) < corr.hd + 0.3f)
                    errors.Add($"路灯挡门：({l.x},{l.z}) 位于 {b.id} 门前通路");
            }
        }
        // 7) 植被：不压建筑/道路/场地/道具/池塘/家；树间距
        void CheckVeg(string label, float x, float z, float r, bool vsProp)
        {
            var f = RC(x, z, r, r);
            foreach (var b in bFoot) if (Overlap(f, b, 0.2f)) errors.Add($"{label}压建筑：({x},{z}) × {b.id}");
            foreach (var rr in roadB) if (Overlap(f, rr, 0.2f)) errors.Add($"{label}压路：({x},{z}) × {rr.id}");
            foreach (var g in gFoot) if (label != "土斑" && Overlap(f, g, -0.6f)) errors.Add($"{label}压场地：({x},{z}) × {g.id}");
            if (HitsPond(f, -0.5f)) errors.Add($"{label}压池塘：({x},{z})");
            if (Overlap(f, villa, -1.0f)) errors.Add($"{label}压主角家：({x},{z})");
            if (vsProp) foreach (var p in pFoot) if (Overlap(f, p, 0.2f)) errors.Add($"{label}压道具：({x},{z}) × {p.id}");
        }
        foreach (var t in W.TREES) CheckVeg("树", t.x, t.z, 0.9f, true);
        foreach (var t in W.BUSHES) CheckVeg("灌木", t.x, t.z, 0.6f, true);
        foreach (var t in W.FLOWERS) CheckVeg("花", t.x, t.z, 0.4f, false);
        foreach (var t in W.DIRT_PATCHES) CheckVeg("土斑", t.x, t.z, 2.4f, false);
        foreach (var t in W.SMALL_ROCKS) CheckVeg("小石头", t.x, t.z, 0.3f, false);
        for (int i = 0; i < W.TREES.Length; i++)
            for (int j = i + 1; j < W.TREES.Length; j++)
                if (Mathf.Sqrt(Mathf.Pow(W.TREES[i].x - W.TREES[j].x, 2) + Mathf.Pow(W.TREES[i].z - W.TREES[j].z, 2)) < 1.8f)
                    errors.Add($"树过近：({W.TREES[i].x},{W.TREES[i].z})");
        // 7b) 树冠与建筑净距：树心到建筑轮廓（主楼 + 东侧翼）≥ 2.15m —— 避免树冠插进立面
        foreach (var t in W.TREES)
            foreach (var b in W.BUILDINGS)
                for (int part = 0; part < (b.wing ? 2 : 1); part++)
                {
                    float cx = part == 0 ? b.pos[0] : b.pos[0] + b.size[0] / 2f + 2.3f;
                    float cz = part == 0 ? b.pos[1] : b.pos[1] + 0.6f;
                    float hw = part == 0 ? b.size[0] / 2f : 2.3f;
                    float hd = part == 0 ? b.size[2] / 2f : b.size[2] * 0.4f;
                    float dx = Mathf.Max(0f, Mathf.Abs(t.x - cx) - hw), dz = Mathf.Max(0f, Mathf.Abs(t.z - cz) - hd);
                    float dist = Mathf.Sqrt(dx * dx + dz * dz);
                    if (dist < 2.15f)
                        errors.Add($"树离建筑过近：({t.x},{t.z}) × {b.id}{(part == 1 ? "侧翼" : "")} 仅 {dist:F2}m（需 ≥2.15m）");
                }
        // 8) 假山
        foreach (var rk in W.ROCKERIES)
        {
            var f = RC(rk.x, rk.z, 1.5f * rk.s, 1.5f * rk.s, "rockery");
            foreach (var b in bFoot) if (Overlap(f, b, 0.5f)) errors.Add($"假山压建筑：({rk.x},{rk.z}) × {b.id}");
            foreach (var r in roadB) if (Overlap(f, r, 0.3f)) errors.Add($"假山压路：({rk.x},{rk.z}) × {r.id}");
            foreach (var g in gFoot) if (Overlap(f, g, 0.3f)) errors.Add($"假山压场地：({rk.x},{rk.z}) × {g.id}");
        }
        // 9) 边界植被不得侵入可行走区
        void CheckBorder(float x, float z, string label)
        {
            if (Mathf.Sqrt(x * x + z * z) < W.BOUND - 14f) errors.Add($"{label}侵入可行走区：({x},{z})");
        }
        foreach (var t in W.BORDER_TREES) CheckBorder(t.x, t.z, "边界树");
        foreach (var t in W.BORDER_BUSHES) CheckBorder(t.x, t.z, "边界灌木");
        foreach (var t in W.BORDER_ROCKS) CheckBorder(t.x, t.z, "边界石");
        // 10) 远景楼
        for (int i = 0; i < W.DISTANT_BUILDINGS.Length; i++)
        {
            var b = W.DISTANT_BUILDINGS[i];
            float rr = Mathf.Sqrt(b.x * b.x + b.z * b.z);
            if (rr < 78) errors.Add($"远景楼侵入内圈：{i} r={rr:F1}");
            for (int j = i + 1; j < W.DISTANT_BUILDINGS.Length; j++)
            {
                var c = W.DISTANT_BUILDINGS[j];
                if (Overlap(RC(b.x, b.z, b.w / 2, b.d / 2), RC(c.x, c.z, c.w / 2, c.d / 2), 2.0f)) errors.Add($"远景楼重叠：{i} × {j}");
            }
            foreach (var t in W.BORDER_TREES)
                if (Mathf.Abs(t.x - b.x) < b.w / 2 + 2 && Mathf.Abs(t.z - b.z) < b.d / 2 + 2) errors.Add($"边界树压远景楼：({t.x},{t.z}) × {i}");
        }
        // 11) 可达性：门 → 直面路 / 经步道（plaza 条带）/ 经岔路连上路
        foreach (var def in W.BUILDINGS)
        {
            DoorOf(def, out float dx, out float dzr, out _, out _);
            bool ok = false;
            foreach (var r in roadB)
            {
                bool vertical = r.hw < r.hd;
                float d = vertical ? Mathf.Abs(dx - r.x) - r.hw : Mathf.Abs(dzr - r.z) - r.hd;
                bool inSpan = vertical ? (dzr >= r.z - r.hd - 1 && dzr <= r.z + r.hd + 1) : (dx >= r.x - r.hw - 1 && dx <= r.x + r.hw + 1);
                if (inSpan && d <= 5f) { ok = true; break; }
            }
            if (!ok)
                foreach (var p in W.PROPS)
                {
                    if (p.type != "plaza") continue;
                    if (PathDist(dx, dzr, p) <= 1.5f) { ok = true; break; }
                }
            if (!ok)
            {
                var doorR = RC(dx, dzr, 1.6f, 2.2f);
                foreach (var p in W.BRANCH_PATHS)
                {
                    var pR = RC(p.x, p.z, 1.5f, p.len / 2);
                    if (!Overlap(doorR, pR, 2.0f)) continue;
                    bool touch = false;
                    foreach (var r in roadB)
                    {
                        bool vertical = r.hw < r.hd;
                        bool t = vertical
                            ? (Mathf.Abs(p.x - r.x) <= 1.5f + r.hw + 0.2f && p.z >= r.z - r.hd - 1 && p.z <= r.z + r.hd + 1)
                            : (Mathf.Abs(p.z - r.z) <= p.len / 2 + r.hd + 0.2f && p.x >= r.x - r.hw - 1 && p.x <= r.x + r.hw + 1);
                        if (t) { touch = true; break; }
                    }
                    if (touch) { ok = true; break; }
                }
            }
            // 公园草坪、神社院内、看台（从跑道上下）是开放地面，不设步道也算可达
            bool openPrecinct = def.id.StartsWith("f_") || def.id.StartsWith("k_") || def.id == "p_pav" || def.id == "s_stand";
            if (!ok && !openPrecinct) errors.Add($"可达性失败：{def.id}");
        }
        // 12) 贴地/贴面纹理密度（密度铁律：基准 160px/米；周期单一来源在 TexScale）
        //     基准组超出 ±6% 即报错 —— 专门抓「贴图像素尺寸」与「平铺周期」脱钩这类静默退化
        //     基准组 = 能一次铺满达到 160px/米 的贴地纹理；球场/操场/池塘受 2048 上限限制，列为登记项
        var baselineTex = new List<string> { "grass", "path", "road_mainstone", "road_cobble", "flower", "dirt", "roadband_v", "roadband_h" };
        var densityLog = new List<string>();
        foreach (var kv in TexScale.Cover)
        {
            var t = TextureLibrary.Get(kv.Key, true, true);
            if (t == null) { errors.Add($"密度登记表有未实现的贴图：{kv.Key}"); continue; }
            float dx = t.width / kv.Value.x, dy = t.height / kv.Value.y;
            bool isBase = baselineTex.Contains(kv.Key);
            densityLog.Add($"{kv.Key} {t.width}×{t.height}px / {kv.Value.x:0.###}×{kv.Value.y:0.###}m → {dx:F1}×{dy:F1}px/米" + (isBase ? "" : "（登记）"));
            if (isBase && (Mathf.Max(dx, dy) > TexScale.Density * 1.06f || Mathf.Min(dx, dy) < TexScale.Density * 0.94f))
                errors.Add($"贴地密度偏离基准：{kv.Key} {dx:F1}×{dy:F1}px/米（基准 {TexScale.Density} ±6%）");
        }
        Debug.Log("[WorldValidator] 贴地密度（px/米）：\n  " + string.Join("\n  ", densityLog));

        // 12b) 全场景密度审计（作者要求：花/土/树/操场……每个贴图的像素密度都要能对上基准）
        //      立面/侧翼/别墅/门/招牌/远景楼/树冠树干 —— 与贴地纹理同一把尺：160px/米
        float tolLo = TexScale.Density * 0.94f, tolHi = TexScale.Density * 1.06f;
        string Verdict(float px, float py)
        {
            float asp = Mathf.Max(px, py) / Mathf.Max(0.001f, Mathf.Min(px, py));
            bool uneven = asp > 1.15f, low = Mathf.Min(px, py) < tolLo;
            if (!uneven && !low) return (Mathf.Max(px, py) <= tolHi) ? "✅ 一致" : "✅ 偏密但清晰";
            var tags = new List<string>();
            if (uneven) tags.Add("横纵不等");
            if (low) tags.Add("偏低会糊");
            return "⚠ " + string.Join(" + ", tags);
        }
        var audit = new List<string>();
        foreach (var b in W.BUILDINGS)
        {
            float fd = TextureLibrary.FacadeDensity(b.size[0], b.size[1]);
            audit.Add($"立面 {b.id,-8} {fd,6:F1}×{fd,6:F1}px/米  {Verdict(fd, fd)}（四面同密度·BoxUV）");
            if (b.wing)
            {
                float dw = TextureLibrary.FacadeDensity(4.6f, b.size[1] * 0.72f);
                audit.Add($"侧翼 {b.id,-8} {dw,6:F1}×{dw,6:F1}px/米  {Verdict(dw, dw)}");
            }
        }
        {
            float dv = TextureLibrary.FacadeDensity(W.VILLA.w, W.VILLA.h);
            audit.Add($"别墅 villa    {dv,6:F1}×{dv,6:F1}px/米  {Verdict(dv, dv)}（四面同密度）");
        }
        audit.Add($"大门 door     {256f / 1.5f,6:F1}×{384f / 2.1f,6:F1}px/米  {Verdict(256f / 1.5f, 384f / 2.1f)}");
        audit.Add($"招牌 sign_*   {640f / 3.3f,6:F1}×{160f / 0.83f,6:F1}px/米  {Verdict(640f / 3.3f, 160f / 0.83f)}（文字贴图）");
        audit.Add($"树冠 leaves   {128f / TexScale.LeafMeters,6:F1}×{128f / TexScale.LeafMeters,6:F1}px/米  ✅ 一致（BoxUV/Cone 逐面）");
        audit.Add($"树干 bark     {96f / TexScale.BarkMeters,6:F1}×{96f / TexScale.BarkMeters,6:F1}px/米  ✅ 一致（BoxUV 逐面）");
        audit.Add($"人物 player   {104f / 1.12f,6:F1}×{152f / 1.64f,6:F1}px/米  {Verdict(104f / 1.12f, 152f / 1.64f)}（作者指定暂不动）");
        if (W.DISTANT_BUILDINGS.Length > 0)   // 当前世界没有远景楼（小镇用真实建筑+山体），示例审计要留空判
        {
            var d0 = W.DISTANT_BUILDINGS[0];
            float dd = TextureLibrary.FacadeDensity(d0.w, d0.h);
            audit.Add($"远景楼 dist  {dd,6:F1}×{dd,6:F1}px/米  {Verdict(dd, dd)}（仅 0 号楼示例·四面同密度）");
        }
        Debug.Log("[WorldValidator] 全场景密度审计（基准 " + TexScale.Density + "px/米，±6% 为一致）：\n  " + string.Join("\n  ", audit));
        // 13) 招牌 PNG 资产：程序化贴图之外只允许 sign_*.png（含中文文字，无法程序化重绘）
        //     且必须可读——运行时 2× 最近邻放大要取像素，不可读会让玩家构建抛异常、世界只建一半
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/tex" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var png = Path.GetFileNameWithoutExtension(path);
            if (!png.StartsWith("sign_")) { errors.Add($"非招牌 PNG 残留：{png}.png（应改程序化贴图）"); continue; }
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null && !ti.isReadable) errors.Add($"招牌 PNG 不可读：{png}.png（跑「Campus/1b. 重设贴图导入（可读）」）");
        }

        // —— 输出 ——
        var counts = $"建筑{W.BUILDINGS.Length} 场地{W.GROUNDS.Length} 道具{W.PROPS.Length} 路灯{W.LAMPS.Length} 树{W.TREES.Length} 灌木{W.BUSHES.Length} " +
                     $"花{W.FLOWERS.Length} 土斑{W.DIRT_PATCHES.Length} 假山{W.ROCKERIES.Length} 小石{W.SMALL_ROCKS.Length} 芦苇{W.REEDS.Length} " +
                     $"边界树{W.BORDER_TREES.Length} 边界灌木{W.BORDER_BUSHES.Length} 边界石{W.BORDER_ROCKS.Length} 远景楼{W.DISTANT_BUILDINGS.Length} " +
                     $"围栏段{W.FENCES.Length} 交互点{W.SPOTS.Length} 碰撞体{W.BLOCKERS.Length}";
        Debug.Log("[WorldValidator] 数量：" + counts);
        foreach (var wmsg in warns) Debug.LogWarning("[WorldValidator] ! " + wmsg);
        if (errors.Count > 0)
        {
            foreach (var e in errors) Debug.LogError("[WorldValidator] ✗ " + e);
            Debug.LogError($"[WorldValidator] 校验失败：{errors.Count} 处违规");
        }
        else Debug.Log("[WorldValidator] ✓ 校验通过：零违规");
    }

    // 导出与 assets/world.json 同构的 JSON（用于 C# 移植与原 JS 数据的对拍）
    [MenuItem("Campus/导出世界数据 JSON（对拍用）")]
    public static void DumpJson()
    {
        var W = CampusData.Build();
        var path = Path.Combine(Application.dataPath, "..", "Build", "world_csharp.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(W));
        Debug.Log("[WorldValidator] 已导出 " + path);
    }
    public static void DumpJsonBatch() { DumpJson(); }

    // ── 小镇总平面校验（docs/18 的四条净距规则，直接作用于 TownData）──
    [MenuItem("Campus/校验小镇布点")]
    public static void TownMenu() => ValidateTown(false);
    public static void TownBatch() => ValidateTown(true);

    static void ValidateTown(bool batch)
    {
        var W = TownData.Build();
        var errors = new List<string>();
        var seen = new HashSet<string>();
        void Err(string msg) { if (seen.Add(msg)) errors.Add(msg); }   // 同一条规则沿路径采样会重复报同一问题
        float RZ(float x) => W.RIVER.amp * Mathf.Sin((x + W.RIVER.phase) / W.RIVER.freq) + W.RIVER.offset;
        // 校园用地（围墙内的教学区）：临河退让按校园总平面另有约定
        bool CampusSite(float x, float z) => x < -48f && z < -18f;

        // 建筑轮廓（含主角家）：占地必须按朝向换算，否则朝 ±X 的校舍会整体转 90°
        var blds = new List<(string name, float x, float z, float hw, float hd, bool campus)>();
        foreach (var b in W.BUILDINGS)
            blds.Add((b.name, b.pos[0], b.pos[1], b.HalfWX, b.HalfWZ, CampusSite(b.pos[0], b.pos[1])));
        blds.Add(("主角家", W.VILLA.x, W.VILLA.z, W.VILLA.w / 2f, W.VILLA.d / 2f, false));

        // 1) 建筑 ↔ 道路：净距 ≥1m
        foreach (var b in blds)
            foreach (var r in W.ROADS)
            {
                float rx = r.axis == "z" ? r.at : (r.from + r.to) / 2f;
                float rz = r.axis == "z" ? (r.from + r.to) / 2f : r.at;
                float rhw = r.axis == "z" ? r.width / 2f : (r.to - r.from) / 2f;
                float rhd = r.axis == "z" ? (r.to - r.from) / 2f : r.width / 2f;
                if (Mathf.Abs(b.x - rx) < b.hw + rhw + 1f && Mathf.Abs(b.z - rz) < b.hd + rhd + 1f)
                    Err($"{b.name} × 道路 {r.id}（净距 <1m）");
            }

        // 1b) 建筑 ↔ 步道（3m 铺装条，含斜向）：沿中心线 1m 采样
        //     正门接驳段（门前走廊内）属于设计意图，不计入净距
        var corrs = new List<Rect>();
        foreach (var b in W.BUILDINGS) corrs.Add(DoorCorr(b, 8f, 2.6f));
        corrs.Add(RC(99999f, 99999f, 0.1f, 0.1f));       // 主角家门前另算
        for (int bi = 0; bi < blds.Count; bi++)
        {
            var b = blds[bi];
            foreach (var p in W.PROPS)
            {
                if (p.type != "plaza" || p.w > 3.5f) continue;
                int n = Mathf.Max(2, Mathf.CeilToInt(p.d));
                for (int i = 0; i <= n; i++)
                {
                    float t = i / (float)n;
                    float px = p.x + Mathf.Sin(p.ry) * (t - 0.5f) * p.d;
                    float pz = p.z + Mathf.Cos(p.ry) * (t - 0.5f) * p.d;
                    if (Mathf.Abs(px - b.x) >= b.hw + p.w / 2f + 1f || Mathf.Abs(pz - b.z) >= b.hd + p.w / 2f + 1f) continue;
                    if (Overlap(RC(px, pz, 0.05f, 0.05f), corrs[bi], 0f)) continue;
                    Err($"{b.name}@({b.x},{b.z}) × 步道@({p.x},{p.z}) w{p.w} len{p.d:F1} ry{p.ry:F2}（净距 <1m）");
                }
            }
        }

        // 2) 建筑 ↔ 河道：小镇要求离水边 ≥8m；校园北侧临河带按总平面只退 2m
        foreach (var b in blds)
        {
            float bank = W.RIVER.width / 2f + (b.campus ? 2f : 8f);
            float lo = Mathf.Min(RZ(b.x - b.hw), RZ(b.x), RZ(b.x + b.hw)) - bank;
            float hi = Mathf.Max(RZ(b.x - b.hw), RZ(b.x), RZ(b.x + b.hw)) + bank;
            if (!(b.z + b.hd < lo || b.z - b.hd > hi))
                Err($"{b.name}@({b.x},{b.z}) 距河道过近（要求离水边 ≥{(b.campus ? 2 : 8)}m，需 z<{lo:F1} 或 z>{hi:F1}，实际 {b.z - b.hd:F1}~{b.z + b.hd:F1}）");
        }

        // 3) 建筑 ↔ 建筑：间距 ≥0.5m
        for (int i = 0; i < blds.Count; i++)
            for (int j = i + 1; j < blds.Count; j++)
            {
                var a = blds[i]; var c = blds[j];
                if (Mathf.Abs(a.x - c.x) < a.hw + c.hw - 0.5f && Mathf.Abs(a.z - c.z) < a.hd + c.hd - 0.5f)
                    Err($"{a.name} × {c.name}（间距 <0.5m）");
            }

        // 4) 道路/步道不得压到河面（桥位除外）
        void CheckRiver(float x, float z, string name)
        {
            if (Mathf.Abs(z - RZ(x)) >= W.RIVER.width / 2f) return;
            foreach (var br in W.RIVER.bridges)
                if (Mathf.Abs(x - br.x) < br.width / 2f + 1f) return;
            errors.Add($"{name} 直接压到河面（无桥）");
        }
        foreach (var r in W.ROADS)
        {
            int n = Mathf.Max(2, Mathf.CeilToInt((r.to - r.from) / 2f));
            for (int i = 0; i <= n; i++)
            {
                float t = Mathf.Lerp(r.from, r.to, i / (float)n);
                if (r.axis == "z") CheckRiver(r.at, t, "道路 " + r.id);
                else CheckRiver(t, r.at, "道路 " + r.id);
            }
        }
        foreach (var p in W.PROPS)
        {
            if (p.type != "plaza" || p.w > 3.5f) continue;
            int n = Mathf.Max(2, Mathf.CeilToInt(p.d / 2f));
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                CheckRiver(p.x + Mathf.Sin(p.ry) * (t - 0.5f) * p.d, p.z + Mathf.Cos(p.ry) * (t - 0.5f) * p.d, "步道");
            }
        }

        foreach (var e in errors) Debug.LogError("[TownValidator] ✗ " + e);
        if (errors.Count == 0) Debug.Log($"[TownValidator] ✓ 小镇校验通过：建筑 {blds.Count} 栋 / 道路 {W.ROADS.Length} 条，零违规");
        else Debug.LogError($"[TownValidator] 小镇校验失败：{errors.Count} 处违规");
        if (batch) EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
    }
}
