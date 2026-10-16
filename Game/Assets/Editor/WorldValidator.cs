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

    public static void RunBatch() { Run(); }

    [MenuItem("Campus/校验世界布点")]
    public static void Run()
    {
        var W = CampusData.Build();
        var errors = new List<string>();
        var warns = new List<string>();

        // —— 轮廓集合 ——
        var bFoot = new List<Rect>();
        foreach (var b in W.BUILDINGS) bFoot.Add(RC(b.pos[0], b.pos[1], b.size[0] / 2, b.size[2] / 2, b.id));
        var gFoot = new List<Rect>();
        foreach (var g in W.GROUNDS) gFoot.Add(RC(g.pos[0], g.pos[1], g.size[0] / 2, g.size[1] / 2, g.id));
        var roadB = new List<Rect>();
        foreach (var r in W.ROADS) roadB.Add(r.axis == "z"
            ? RC(r.at, (r.from + r.to) / 2, r.width / 2, (r.to - r.from) / 2, r.id)
            : RC((r.from + r.to) / 2, r.at, (r.to - r.from) / 2, r.width / 2, r.id));
        var pond = RC(W.POND.pos[0], W.POND.pos[1], W.POND.r + 0.6f, W.POND.r + 0.6f, "pond");
        var villa = RC(W.VILLA.x, W.VILLA.z, W.VILLA.w / 2 + 1.2f, W.VILLA.d / 2 + 4.6f, "villa");

        // 道具轮廓
        var pFoot = new List<Rect>();
        foreach (var p in W.PROPS)
        {
            if (p.type == "plaza") continue;
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

        // 1) 建筑相互 ≥5m / 建筑 vs 场地/道路/池塘/家
        for (int i = 0; i < bFoot.Count; i++)
        {
            for (int j = i + 1; j < bFoot.Count; j++)
                if (Overlap(bFoot[i], bFoot[j], 5.0f)) errors.Add($"建筑过近：{bFoot[i].id} × {bFoot[j].id}（要求 ≥5m）");
            foreach (var g in gFoot) if (Overlap(bFoot[i], g, 1.5f)) errors.Add($"建筑压场地：{bFoot[i].id} × {g.id}");
            foreach (var r in roadB) if (Overlap(bFoot[i], r, 1.0f)) errors.Add($"建筑压路：{bFoot[i].id} × {r.id}");
            if (Overlap(bFoot[i], pond, 1.0f)) errors.Add($"建筑压池塘：{bFoot[i].id}");
            if (Overlap(bFoot[i], villa, 1.0f)) errors.Add($"建筑压主角家：{bFoot[i].id}");
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
            if (Overlap(gFoot[i], pond, 0.5f)) errors.Add($"场地压池塘：{gFoot[i].id}");
            if (Overlap(gFoot[i], villa, 1.0f)) errors.Add($"场地压主角家：{gFoot[i].id}");
            for (int j = i + 1; j < gFoot.Count; j++)
                if (Overlap(gFoot[i], gFoot[j], 1.5f)) errors.Add($"场地重叠：{gFoot[i].id} × {gFoot[j].id}");
        }
        // 4) 道具 vs 建筑/道路/道具
        for (int i = 0; i < pFoot.Count; i++)
        {
            foreach (var b in bFoot) if (Overlap(pFoot[i], b, 0.6f)) errors.Add($"道具压建筑：{pFoot[i].id} × {b.id}");
            if (!pFoot[i].id.StartsWith("gate"))
                foreach (var r in roadB) if (Overlap(pFoot[i], r, 0.4f)) errors.Add($"道具压路：{pFoot[i].id} × {r.id}");
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
            float dz = b.face * (b.size[2] / 2 + 0.9f);
            var corr = b.face == 1 ? RC(b.pos[0], b.pos[1] + dz + 6, 0.3f, 6.5f) : RC(b.pos[0], b.pos[1] + dz - 6, 0.3f, 6.5f);
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
                float dz = b.face * (b.size[2] / 2 + 0.9f);
                var corr = b.face == 1 ? RC(b.pos[0], b.pos[1] + dz + 5, 1.3f, 5.5f) : RC(b.pos[0], b.pos[1] + dz - 5, 1.3f, 5.5f);
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
            if (Overlap(f, pond, -0.5f)) errors.Add($"{label}压池塘：({x},{z})");
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
            if (Mathf.Sqrt(x * x + z * z) < W.BOUND - 2f) errors.Add($"{label}侵入可行走区：({x},{z})");
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
        // 11) 可达性：门 → (直面路 或 经岔路连上路)
        foreach (var def in W.BUILDINGS)
        {
            float dx = def.pos[0], dzr = def.pos[1] + def.face * (def.size[2] / 2 + 0.9f);
            bool ok = false;
            foreach (var r in roadB)
            {
                bool vertical = r.hw < r.hd;
                float d = vertical ? Mathf.Abs(dx - r.x) - r.hw : Mathf.Abs(dzr - r.z) - r.hd;
                bool inSpan = vertical ? (dzr >= r.z - r.hd - 1 && dzr <= r.z + r.hd + 1) : (dx >= r.x - r.hw - 1 && dx <= r.x + r.hw + 1);
                if (inSpan && d <= 5f) { ok = true; break; }
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
            if (!ok) errors.Add($"可达性失败：{def.id}");
        }
        // 12) 贴地/贴面纹理密度（密度铁律：基准 160px/米；周期单一来源在 TexScale）
        //     基准组超出 ±6% 即报错 —— 专门抓「贴图像素尺寸」与「平铺周期」脱钩这类静默退化
        var baselineTex = new List<string> { "grass", "path", "road_mainstone", "road_cobble" };
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
}
