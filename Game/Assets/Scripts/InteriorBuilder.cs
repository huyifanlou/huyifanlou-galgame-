using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────
// 室内场景：家 / 教学楼（单层，开放布局，按 docs/36 方案 A 定稿）
//   全部程序化生成，建在世界之外（x ≈ 900 起），与室外共用贴图/材质库。
//   进入时把玩家瞬移到室内落点，碰撞走本文件的 AABB；出口在门口，按 F 回室外。
//   单层无楼梯：FloorY 只有一个元素。
//   家 30×20（客厅+餐厅+厨房开放大空间 + 北侧 4 小间，内墙仅 4 道）
//   教学楼 56×30（北带 4 间教室 + 5m 走廊 + 南带门厅/办公室/图书角）
// ─────────────────────────────────────────────────────────────
public class Interior
{
    public string Id;
    public string Name;
    public float X1, Z1, X2, Z2;
    public Vector2 Spawn;
    public Vector2 Exit;
    public float ExitRadius = 1.7f;
    public float[] FloorY;
    public GameObject Root;

    public readonly List<Solid> Solids = new List<Solid>();

    public float FloorAt(float x, float z, float curY) => FloorY[0];

    public bool Collides(float x, float z, float curY)
    {
        for (int i = 0; i < Solids.Count; i++)
        {
            var s = Solids[i];
            if (x > s.X1 - 0.32f && x < s.X2 + 0.32f && z > s.Z1 - 0.32f && z < s.Z2 + 0.32f) return true;
        }
        return false;
    }

    public bool NearExit(float x, float z)
    {
        float dx = x - Exit.x, dz = z - Exit.y;
        return dx * dx + dz * dz < ExitRadius * ExitRadius;
    }

    public float ClampX(float x) => Mathf.Clamp(x, X1 + 0.45f, X2 - 0.45f);
    public float ClampZ(float z) => Mathf.Clamp(z, Z1 + 0.45f, Z2 - 0.45f);
}

// 碰撞盒（平面 AABB）
public struct Solid
{
    public float X1, Z1, X2, Z2;
    public bool IsWall;

    public Solid(float x1, float z1, float x2, float z2, bool isWall = false)
    {
        X1 = Mathf.Min(x1, x2); X2 = Mathf.Max(x1, x2);
        Z1 = Mathf.Min(z1, z2); Z2 = Mathf.Max(z1, z2);
        IsWall = isWall;
    }
}

// ─────────────────────────────────────────────────────────────
public static class InteriorBuilder
{
    const float HOME_X = 900f, SCHOOL_X = 1060f;
    const float WALL_OUT = 0.24f;
    const float WALL_IN = 0.18f;

    static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();

    static Material T(string tex)
    {
        if (!_cache.TryGetValue("T" + tex, out var m)) { m = MatLib.Lit(tex); _cache["T" + tex] = m; }
        return m;
    }

    static Material C(string hex)
    {
        if (!_cache.TryGetValue("C" + hex, out var m)) { m = MatLib.LitColor(MatLib.Hex(hex)); _cache["C" + hex] = m; }
        return m;
    }

    // ── 原语 ──────────────────────────────────────────────
    static GameObject Part(Transform p, Vector3 pos, Vector3 size, Material mat, float texMeters = 1f,
                           Vector3 euler = default, bool collider = false)
    {
        var go = new GameObject("p");
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.BoxUV(size, texMeters);
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        if (collider)
        {
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
        }
        return go;
    }

    static GameObject SolidPart(Transform p, Interior it, Vector3 pos, Vector3 size, Material mat,
                                float texMeters = 1f, bool isWall = false)
    {
        var go = Part(p, pos, size, mat, texMeters, default, true);
        it.Solids.Add(new Solid(pos.x - size.x * 0.5f, pos.z - size.z * 0.5f,
                                pos.x + size.x * 0.5f, pos.z + size.z * 0.5f, isWall));
        return go;
    }

    static void Wall(Transform p, Interior it, float x1, float z1, float x2, float z2,
                     float y0, float h, Material mat, float thick = WALL_IN)
    {
        if (Mathf.Abs(x1 - x2) < 0.01f && Mathf.Abs(z1 - z2) < 0.01f) return;
        float w = Mathf.Max(Mathf.Abs(x2 - x1), thick);
        float d = Mathf.Max(Mathf.Abs(z2 - z1), thick);
        SolidPart(p, it, new Vector3((x1 + x2) * 0.5f, y0 + h * 0.5f, (z1 + z2) * 0.5f),
                  new Vector3(w, h, d), mat, 1.7f, true);
    }

    // 沿 X 的墙，中间留门洞（门楣不挡人）
    static void WallX(Transform p, Interior it, float x1, float x2, float z, float y0, float h,
                      float doorA, float doorB, float doorH, Material mat, float thick = WALL_OUT)
    {
        if (doorB <= doorA) { Wall(p, it, x1, z, x2, z, y0, h, mat, thick); return; }
        Wall(p, it, x1, z, doorA, z, y0, h, mat, thick);
        Wall(p, it, doorB, z, x2, z, y0, h, mat, thick);
        float lintel = h - doorH;
        if (lintel > 0.05f)
            Part(p, new Vector3((doorA + doorB) * 0.5f, y0 + doorH + lintel * 0.5f, z),
                 new Vector3(doorB - doorA, lintel, thick), mat, 1.7f, default, true);
    }

    // 沿 Z 的墙，中间留门洞
    static void WallZ(Transform p, Interior it, float z1, float z2, float x, float y0, float h,
                      float doorA, float doorB, float doorH, Material mat, float thick = WALL_OUT)
    {
        if (doorB <= doorA) { Wall(p, it, x, z1, x, z2, y0, h, mat, thick); return; }
        Wall(p, it, x, z1, x, doorA, y0, h, mat, thick);
        Wall(p, it, x, doorB, x, z2, y0, h, mat, thick);
        float lintel = h - doorH;
        if (lintel > 0.05f)
            Part(p, new Vector3(x, y0 + doorH + lintel * 0.5f, (doorA + doorB) * 0.5f),
                 new Vector3(thick, lintel, doorB - doorA), mat, 1.7f, default, true);
    }

    static void Lamp(Transform p, Vector3 pos, float range, float intensity, string hex = "#fff1de")
    {
        var go = new GameObject("lamp");
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = range;
        l.intensity = intensity;
        l.color = MatLib.Hex(hex);
        l.shadows = LightShadows.None;
        Part(p, pos + new Vector3(0f, 0.14f, 0f), new Vector3(0.34f, 0.09f, 0.34f), C("#fff6e2"), 1f);
        Part(p, pos + new Vector3(0f, 0.30f, 0f), new Vector3(0.05f, 0.24f, 0.05f), C("#8d8d94"), 1f);
    }

    static void Reg(Interior it, float cx, float cz, float w, float d)
        => it.Solids.Add(new Solid(cx - w * 0.5f, cz - d * 0.5f, cx + w * 0.5f, cz + d * 0.5f));

    static Material _glass;
    static Material Glass()
    {
        if (_glass == null) _glass = MatLib.UnlitColor(MatLib.Hex("#c4d8e4"));
        return _glass;
    }

    // 窗：玻璃 + 四边框 + 中梃
    static void Window(Transform p, Vector3 pos, float w, float h, float ry)
    {
        var g = new GameObject("win").transform;
        g.SetParent(p, false);
        g.localPosition = pos;
        g.localEulerAngles = new Vector3(0f, ry, 0f);
        Part(g, Vector3.zero, new Vector3(w, h, 0.05f), Glass(), 1f);
        var fr = C("#8a7a66");
        Part(g, new Vector3(0f, h * 0.5f, 0.02f), new Vector3(w + 0.14f, 0.09f, 0.1f), fr, 1f);
        Part(g, new Vector3(0f, -h * 0.5f, 0.02f), new Vector3(w + 0.14f, 0.09f, 0.1f), fr, 1f);
        Part(g, new Vector3(-w * 0.5f, 0f, 0.02f), new Vector3(0.09f, h, 0.1f), fr, 1f);
        Part(g, new Vector3(w * 0.5f, 0f, 0.02f), new Vector3(0.09f, h, 0.1f), fr, 1f);
        Part(g, new Vector3(0f, 0f, 0.02f), new Vector3(0.06f, h, 0.1f), fr, 1f);
    }

    static void Panel(Transform p, float x1, float z1, float x2, float z2, float y0, float h, Material mat)
    {
        float w = Mathf.Abs(x2 - x1), d = Mathf.Abs(z2 - z1);
        Part(p, new Vector3((x1 + x2) * 0.5f, y0 + h * 0.5f, (z1 + z2) * 0.5f),
             new Vector3(Mathf.Max(w, 0.05f), h, Mathf.Max(d, 0.05f)), mat, 1.1f);
    }

    // 墙面薄片：沿墙展开，按洞口裁剪成多段
    static void WallDeco(Transform p, float a0, float a1, float constAxis, float y0, float y1,
                         int axis, float thick, Material mat, float uvMeters,
                         (float, float)[] holes = null)
    {
        float cur = a0;
        if (holes != null)
            foreach (var (ha, hb) in holes)
            {
                if (hb <= cur) continue;
                if (ha > cur) Seg(cur, Mathf.Min(ha, a1));
                cur = Mathf.Max(cur, hb);
                if (cur >= a1) break;
            }
        if (cur < a1) Seg(cur, a1);

        void Seg(float f, float t)
        {
            if (t - f < 0.03f) return;
            if (axis == 0)
                Part(p, new Vector3((f + t) * 0.5f, (y0 + y1) * 0.5f, constAxis),
                     new Vector3(t - f, y1 - y0, thick), mat, uvMeters);
            else
                Part(p, new Vector3(constAxis, (y0 + y1) * 0.5f, (f + t) * 0.5f),
                     new Vector3(thick, y1 - y0, t - f), mat, uvMeters);
        }
    }

    static void Skirt(Transform p, float x1, float z1, float x2, float z2, float y0)
        => Panel(p, x1, z1, x2, z2, y0, 0.12f, C("#5f4a38"));

    static void Picture(Transform p, Vector3 pos, float w, float h, float ry)
    {
        var g = new GameObject("pic").transform;
        g.SetParent(p, false);
        g.localPosition = pos;
        g.localEulerAngles = new Vector3(0f, ry, 0f);
        Part(g, Vector3.zero, new Vector3(w + 0.10f, h + 0.10f, 0.05f), C("#6f5540"), 1f);
        Part(g, new Vector3(0f, 0f, 0.035f), new Vector3(w, h, 0.02f), C("#b9c8c0"), 1f);
        Part(g, new Vector3(-w * 0.12f, h * 0.10f, 0.05f), new Vector3(w * 0.5f, h * 0.45f, 0.01f), C("#8a9e94"), 1f);
        Part(g, new Vector3(w * 0.22f, -h * 0.16f, 0.05f), new Vector3(w * 0.32f, h * 0.3f, 0.01f), C("#a8b8ae"), 1f);
    }

    static void WallClock(Transform p, Vector3 pos, float ry)
    {
        var g = new GameObject("clock").transform;
        g.SetParent(p, false);
        g.localPosition = pos;
        g.localEulerAngles = new Vector3(0f, ry, 0f);
        Part(g, Vector3.zero, new Vector3(0.44f, 0.44f, 0.05f), C("#f4f1e8"), 1f);
        Part(g, new Vector3(0f, 0.07f, 0.04f), new Vector3(0.03f, 0.15f, 0.012f), C("#3a3f47"), 1f);
        Part(g, new Vector3(0.05f, 0f, 0.04f), new Vector3(0.11f, 0.03f, 0.012f), C("#3a3f47"), 1f);
    }

    static void Plant(Transform p, Vector3 pos, float s = 1f)
    {
        Part(p, pos + new Vector3(0f, 0.22f * s, 0f), new Vector3(0.34f * s, 0.44f * s, 0.34f * s), C("#8a5a3c"), 1f);
        var go = new GameObject("leaf");
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos + new Vector3(0f, 0.60f * s, 0f);
        go.transform.localScale = Vector3.one * s;
        go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Canopy(0.40f, 10, 6, 0.34f, 3.1f);
        go.AddComponent<MeshRenderer>().sharedMaterial = C("#3f7a3f");
    }

    static void Curtain(Transform p, float cx, float y, float cz, float w, float h, Material cloth)
    {
        Part(p, new Vector3(cx - w * 0.5f + 0.20f, y + h * 0.5f, cz), new Vector3(0.34f, h, 0.07f), cloth, 1f);
        Part(p, new Vector3(cx + w * 0.5f - 0.20f, y + h * 0.5f, cz), new Vector3(0.34f, h, 0.07f), cloth, 1f);
        Part(p, new Vector3(cx, y + h + 0.06f, cz), new Vector3(w + 0.4f, 0.06f, 0.09f), C("#6f5540"), 1f);
    }

    static void Rug(Transform p, Vector3 pos, float w, float d, Material mat)
        => Part(p, pos, new Vector3(w, 0.02f, d), mat, 1f);

    static void Chair(Transform p, Interior it, Vector3 seat, float ry, Material wood, bool solid = false)
    {
        var g = new GameObject("chair").transform;
        g.SetParent(p, false);
        g.localPosition = seat;
        g.localEulerAngles = new Vector3(0f, ry, 0f);
        Part(g, new Vector3(0f, 0.44f, 0f), new Vector3(0.46f, 0.06f, 0.46f), wood, 0.8f);
        Part(g, new Vector3(0f, 0.70f, -0.20f), new Vector3(0.46f, 0.52f, 0.06f), wood, 0.8f);
        for (int i = 0; i < 4; i++)
            Part(g, new Vector3(i % 2 == 0 ? -0.19f : 0.19f, 0.22f, i < 2 ? -0.19f : 0.19f),
                 new Vector3(0.05f, 0.44f, 0.05f), wood, 0.6f);
        if (solid) Reg(it, seat.x, seat.z, 0.56f, 0.56f);
    }

    static void Table(Transform p, Vector3 basePos, float w, float d, float h, Material wood)
    {
        Part(p, new Vector3(basePos.x, basePos.y + h - 0.04f, basePos.z), new Vector3(w, 0.08f, d), wood, 1.1f);
        for (int i = 0; i < 4; i++)
            Part(p, new Vector3(basePos.x + (i % 2 == 0 ? -w * 0.5f + 0.12f : w * 0.5f - 0.12f),
                                basePos.y + (h - 0.08f) * 0.5f,
                                basePos.z + (i < 2 ? -d * 0.5f + 0.12f : d * 0.5f - 0.12f)),
                 new Vector3(0.08f, h - 0.08f, 0.08f), wood, 0.7f);
    }

    static void Sofa(Transform p, Vector3 pos, float len, float ry, Material fabric)
    {
        var g = new GameObject("sofa").transform;
        g.SetParent(p, false);
        g.localPosition = pos;
        g.localEulerAngles = new Vector3(0f, ry, 0f);
        Part(g, new Vector3(0f, 0.11f, 0f), new Vector3(len - 0.12f, 0.22f, 0.86f), C("#6f5540"), 1f);  // 底座（原来沙发悬空 20cm）
        Part(g, new Vector3(0f, 0.42f, 0f), new Vector3(len, 0.44f, 0.95f), fabric, 1.2f);
        Part(g, new Vector3(0f, 0.68f, -0.38f), new Vector3(len, 0.85f, 0.18f), fabric, 1.2f);
        Part(g, new Vector3(-len * 0.5f + 0.1f, 0.62f, 0f), new Vector3(0.2f, 0.62f, 0.95f), fabric, 1.2f);
        Part(g, new Vector3(len * 0.5f - 0.1f, 0.62f, 0f), new Vector3(0.2f, 0.62f, 0.95f), fabric, 1.2f);
    }

    // 出口门廊：室外地面 + 顶 + 柱 + 暖灯（出口不再是空白）
    static void Porch(Transform p, float cx, float doorZ, float w, float sign, float h)
    {
        float zFloor = doorZ + sign * 1.1f;
        Part(p, new Vector3(cx, 0.02f, zFloor), new Vector3(w + 1.4f, 0.05f, 2.2f), T("in_tile"), 1.4f);
        Part(p, new Vector3(cx, h + 0.1f, zFloor), new Vector3(w + 1.6f, 0.22f, 2.5f), C("#8f8471"), 1f);
        float px = w / 2f + 0.45f;
        foreach (int s2 in new[] { -1, 1 })
            Part(p, new Vector3(cx + s2 * px, h * 0.5f, doorZ + sign * 1.9f), new Vector3(0.28f, h, 0.28f), C("#e6dcc6"), 1f);
        var l = new GameObject("porchlight");
        l.transform.SetParent(p, false);
        l.transform.localPosition = new Vector3(cx, h - 0.25f, doorZ + sign * 0.6f);
        var li = l.AddComponent<Light>();
        li.type = LightType.Point;
        li.range = 7f;
        li.intensity = 0.9f;
        li.color = MatLib.Hex("#ffcf8a");
        li.shadows = LightShadows.None;
        Part(p, new Vector3(cx, h - 0.35f, doorZ + sign * 0.6f), new Vector3(0.4f, 0.16f, 0.4f), C("#ffdf9a"), 1f);
    }

    // ═════════════════════════════════════════════════════
    public static List<Interior> Build(Transform parent)
    {
        _cache.Clear();
        var list = new List<Interior> { Home(parent), School(parent) };
        foreach (var it in list) Validate(it);
        return list;
    }

    static void Validate(Interior it)
    {
        float bx1 = it.X1 + WALL_OUT * 0.5f + 0.02f, bx2 = it.X2 - WALL_OUT * 0.5f - 0.02f;
        float bz1 = it.Z1 + WALL_OUT * 0.5f + 0.02f, bz2 = it.Z2 - WALL_OUT * 0.5f - 0.02f;
        int bad = 0;
        foreach (var s in it.Solids)
        {
            if (s.IsWall) continue;
            if (s.X1 < bx1 || s.X2 > bx2 || s.Z1 < bz1 || s.Z2 > bz2)
            {
                Debug.LogWarning($"[Interior:{it.Id}] 越界 x[{s.X1:F2},{s.X2:F2}] z[{s.Z1:F2},{s.Z2:F2}]");
                bad++;
                continue;
            }
            foreach (var w in it.Solids)
            {
                if (!w.IsWall) continue;
                float ox2 = Mathf.Min(s.X2, w.X2) - Mathf.Max(s.X1, w.X1);
                float oz2 = Mathf.Min(s.Z2, w.Z2) - Mathf.Max(s.Z1, w.Z1);
                if (ox2 > 0.06f && oz2 > 0.06f)
                {
                    Debug.LogWarning($"[Interior:{it.Id}] 插墙 x[{s.X1:F2},{s.X2:F2}] z[{s.Z1:F2},{s.Z2:F2}]");
                    bad++;
                    break;
                }
            }
        }
        Debug.Log($"[Interior:{it.Id}] 校验完成：{it.Solids.Count} 个碰撞盒，问题 {bad} 处");
    }

    // ── 家：30×20 单层（方案 A）──
    static Interior Home(Transform parent)
    {
        float ox = HOME_X;
        var it = new Interior
        {
            Id = "home",
            Name = "家",
            X1 = ox - 15f, X2 = ox + 15f, Z1 = -10f, Z2 = 10f,
            Spawn = new Vector2(ox, -8.3f),
            Exit = new Vector2(ox, -9.2f),
            FloorY = new[] { 0.02f },
        };
        var root = new GameObject("Interior_home");
        root.transform.SetParent(parent, false);
        it.Root = root;
        var p = root.transform;

        const float W = 4.0f;
        var wall = T("in_wall");
        var wallA = T("in_wall");
        var wallB = T("in_wall2");
        var wallC = T("in_wall3");
        var wallT = T("in_walltile");
        var floor = T("in_floor");
        var tile = T("in_tile");
        var ceil = T("in_ceil");
        var wood = T("wood");
        var woodDark = C("#6f5540");
        var fabric = C("#7d8a76");
        var fabric2 = C("#8f8571");
        var white = C("#e6e6e2");
        var metal = C("#9aa0a6");
        var screen = C("#1a1d22");
        var cloth = C("#c9b79c");

        // ── 地面 ──
        Part(p, new Vector3(ox, 0f, 0f), new Vector3(30f, 0.04f, 20f), floor, 2f, default, true);
        Part(p, new Vector3(ox + 9.5f, 0.05f, -5f), new Vector3(11f, 0.02f, 10f), tile, 1.4f);
        Part(p, new Vector3(ox + 0.6f, 0.05f, 6f), new Vector3(5.4f, 0.02f, 8f), tile, 1.4f);

        // ── 天花 ──
        Part(p, new Vector3(ox, W - 0.03f, 0f), new Vector3(30f, 0.06f, 20f), ceil, 2f);

        // ── 外墙 ──
        WallX(p, it, ox - 15f, ox + 15f, -10f, 0f, W, ox - 1.2f, ox + 1.2f, 2.6f, wall);
        WallX(p, it, ox - 15f, ox + 15f, 10f, 0f, W, 0f, 0f, 2.6f, wall);
        WallZ(p, it, -10f, 10f, ox - 15f, 0f, W, 0f, 0f, 2.6f, wall);
        WallZ(p, it, -10f, 10f, ox + 15f, 0f, W, 0f, 0f, 2.6f, wall);

        // ── 内墙（仅 4 道）──
        WallX(p, it, ox - 15f, ox - 12.8f, 2f, 0.02f, W, 0f, 0f, 2.4f, wall, WALL_IN);
        WallX(p, it, ox - 11.6f, ox - 6.4f, 2f, 0.02f, W, 0f, 0f, 2.4f, wall, WALL_IN);
        WallX(p, it, ox - 5.2f, ox + 0.0f, 2f, 0.02f, W, 0f, 0f, 2.4f, wall, WALL_IN);
        WallX(p, it, ox + 1.2f, ox + 5.4f, 2f, 0.02f, W, 0f, 0f, 2.4f, wall, WALL_IN);
        WallX(p, it, ox + 6.6f, ox + 15f, 2f, 0.02f, W, 0f, 0f, 2.4f, wall, WALL_IN);
        WallZ(p, it, 2f, 10f, ox - 9.4f, 0.02f, W, 0f, 0f, 2.4f, wall, WALL_IN);
        WallZ(p, it, 2f, 10f, ox - 2.2f, 0.02f, W, 0f, 0f, 2.4f, wall, WALL_IN);
        WallZ(p, it, 2f, 10f, ox + 3.4f, 0.02f, W, 0f, 0f, 2.4f, wall, WALL_IN);

        // ── 窗（落在无家具墙段；贴在墙内侧 0.06）──
        Window(p, new Vector3(ox - 11.6f, 2.0f, -9.80f), 2.8f, 1.5f, 0f);
        Window(p, new Vector3(ox - 7.6f, 2.0f, -9.80f), 2.8f, 1.5f, 0f);
        Window(p, new Vector3(ox + 10.2f, 2.0f, -9.80f), 2.4f, 1.5f, 0f);
        Window(p, new Vector3(ox + 14.80f, 2.0f, -4.8f), 2.4f, 1.5f, -90f);
        Window(p, new Vector3(ox - 11.4f, 2.0f, 9.80f), 2.0f, 1.5f, 180f);
        Window(p, new Vector3(ox - 3.6f, 2.0f, 9.80f), 1.6f, 1.5f, 180f);
        Window(p, new Vector3(ox + 2.7f, 2.6f, 9.80f), 1.0f, 1.0f, 180f);
        Window(p, new Vector3(ox + 11.7f, 2.0f, 9.80f), 1.4f, 1.5f, 180f);

        // ── 出口：玻璃门扇（带框）+ 门廊 ──
        Window(p, new Vector3(ox, 1.35f, -9.81f), 2.2f, 2.5f, 0f);
        Porch(p, ox, -10f, 3.6f, -1f, 2.9f);

        // ── 墙纸（外墙内表面全贴；内墙两面按房间贴）──
        WallDeco(p, ox - 14.86f, ox - 1.2f, -9.86f, 0.02f, W, 0, 0.05f, wallA, 1.7f);
        WallDeco(p, ox + 1.2f, ox + 14.86f, -9.86f, 0.02f, W, 0, 0.05f, wallT, 1.7f);
        WallDeco(p, -9.86f, 9.86f, ox - 14.86f, 0.02f, W, 1, 0.05f, wallA, 1.7f);
        WallDeco(p, -9.86f, 9.86f, ox + 14.86f, 0.02f, W, 1, 0.05f, wallT, 1.7f);
        WallDeco(p, ox - 14.86f, ox + 14.86f, 9.86f, 0.02f, W, 0, 0.05f, wallC, 1.7f);
        WallDeco(p, ox - 14.86f, ox + 14.86f, 1.89f, 0.02f, W, 0, 0.05f, wallA, 1.7f,
                 new[] { (ox - 12.98f, ox - 11.42f), (ox - 6.58f, ox - 5.02f), (ox - 0.18f, ox + 1.38f), (ox + 5.22f, ox + 6.78f) });
        WallDeco(p, ox - 14.86f, ox - 9.4f, 2.11f, 0.02f, W, 0, 0.05f, wallB, 1.7f,
                 new[] { (ox - 12.98f, ox - 11.42f) });
        WallDeco(p, ox - 9.4f, ox - 2.2f, 2.11f, 0.02f, W, 0, 0.05f, wallB, 1.7f,
                 new[] { (ox - 6.58f, ox - 5.02f) });
        WallDeco(p, ox - 2.2f, ox + 3.4f, 2.11f, 0.02f, W, 0, 0.05f, wallT, 1.7f,
                 new[] { (ox - 0.18f, ox + 1.38f) });
        WallDeco(p, ox + 3.4f, ox + 14.86f, 2.11f, 0.02f, W, 0, 0.05f, wallA, 1.7f,
                 new[] { (ox + 5.22f, ox + 6.78f) });
        WallDeco(p, 2.09f, 9.86f, ox - 9.51f, 0.02f, W, 1, 0.05f, wallB, 1.7f);
        WallDeco(p, 2.09f, 9.86f, ox - 9.29f, 0.02f, W, 1, 0.05f, wallB, 1.7f);
        WallDeco(p, 2.09f, 9.86f, ox - 2.31f, 0.02f, W, 1, 0.05f, wallB, 1.7f);
        WallDeco(p, 2.09f, 9.86f, ox - 2.09f, 0.02f, W, 1, 0.05f, wallT, 1.7f);
        WallDeco(p, 2.09f, 9.86f, ox + 3.33f, 0.02f, W, 1, 0.05f, wallT, 1.7f);
        WallDeco(p, 2.09f, 9.86f, ox + 3.51f, 0.02f, W, 1, 0.05f, wallA, 1.7f);
        Skirt(p, ox - 14.86f, -9.84f, ox + 14.86f, -9.84f, 0.02f);
        Skirt(p, ox - 14.86f, 9.84f, ox + 14.86f, 9.84f, 0.02f);
        Skirt(p, ox - 14.84f, -9.84f, ox - 14.84f, 9.84f, 0.02f);
        Skirt(p, ox + 14.84f, -9.84f, ox + 14.84f, 9.84f, 0.02f);

        // ── 装饰 ──
        Picture(p, new Vector3(ox - 14.83f, 2.1f, -6.6f), 1.2f, 0.9f, 90f);
        Picture(p, new Vector3(ox - 14.83f, 2.1f, -2.6f), 1.0f, 0.75f, 90f);
        WallClock(p, new Vector3(ox - 14.83f, 3.1f, -6.6f), 90f);
        Plant(p, new Vector3(ox - 14.2f, 0.02f, -9.1f), 1.1f);
        Plant(p, new Vector3(ox + 3.0f, 0.02f, -7.4f), 1.0f);
        Picture(p, new Vector3(ox - 14.78f, 2.6f, 6.0f), 1.3f, 0.9f, 90f);
        Curtain(p, ox - 11.4f, 1.25f, 9.55f, 2.4f, 1.7f, cloth);
        Picture(p, new Vector3(ox - 2.35f, 2.2f, 6.0f), 1.0f, 0.75f, 270f);

        // ════════ 家具 ════════
        // 客厅（x -15..-4, z -10..2）
        Rug(p, new Vector3(ox - 12.4f, 0.06f, -4.0f), 3.4f, 2.6f, fabric2);
        Part(p, new Vector3(ox - 14.5f, 1.15f, -4.0f), new Vector3(0.12f, 1.5f, 2.6f), screen, 1f);
        Part(p, new Vector3(ox - 14.5f, 0.36f, -4.0f), new Vector3(0.5f, 0.72f, 2.8f), woodDark, 1f);
        Sofa(p, new Vector3(ox - 11.3f, 0f, -4.0f), 3.2f, 270f, fabric);
        Part(p, new Vector3(ox - 12.9f, 0.24f, -4.0f), new Vector3(0.7f, 0.46f, 1.3f), woodDark, 1f);
        Part(p, new Vector3(ox - 9.0f, 0.42f, -4.0f), new Vector3(0.95f, 0.85f, 1.5f), fabric, 1.2f);
        Part(p, new Vector3(ox - 8.6f, 1.0f, 1.62f), new Vector3(2.4f, 2.0f, 0.4f), woodDark, 1f);
        Lamp(p, new Vector3(ox - 11.3f, 3.55f, -4.0f), 11f, 1.3f);
        Part(p, new Vector3(ox - 14.3f, 1.1f, -1.2f), new Vector3(0.07f, 2.0f, 0.07f), metal, 1f);
        Part(p, new Vector3(ox - 14.3f, 2.15f, -1.2f), new Vector3(0.36f, 0.3f, 0.36f), C("#fff2d6"), 1f);

        // 玄关
        Part(p, new Vector3(ox + 2.9f, 0.55f, -9.0f), new Vector3(0.45f, 1.1f, 1.6f), woodDark, 1f);
        Part(p, new Vector3(ox - 2.6f, 0.22f, -9.25f), new Vector3(0.9f, 0.44f, 0.4f), wood, 1f);
        Part(p, new Vector3(ox + 3.62f, 1.0f, -6.8f), new Vector3(0.06f, 1.4f, 0.8f), C("#b8c6cc"), 1f);
        Rug(p, new Vector3(ox, 0.06f, -8.9f), 1.8f, 1.2f, C("#8d9aa6"));
        Lamp(p, new Vector3(ox, 3.55f, -8.6f), 8f, 1.0f);

        // 餐厅（中）
        Table(p, new Vector3(ox, 0f, -4.2f), 2.4f, 1.1f, 0.76f, wood);
        Chair(p, it, new Vector3(ox, 0f, -5.05f), 0f, wood, true);      // 南侧，人朝北
        Chair(p, it, new Vector3(ox, 0f, -3.35f), 180f, wood, true);    // 北侧，人朝南
        Chair(p, it, new Vector3(ox - 1.55f, 0f, -4.2f), 90f, wood, true);   // 西侧，人朝东
        Chair(p, it, new Vector3(ox + 1.55f, 0f, -4.2f), 270f, wood, true);  // 东侧，人朝西
        Lamp(p, new Vector3(ox, 3.5f, -4.2f), 9f, 1.1f);

        // 厨房（东，x 4..15）：L 台面 + 中岛
        Part(p, new Vector3(ox + 9.5f, 0.49f, -9.35f), new Vector3(9.0f, 0.98f, 0.66f), white, 1f);
        Part(p, new Vector3(ox + 9.5f, 1.0f, -9.35f), new Vector3(9.05f, 0.06f, 0.7f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox + 14.45f, 0.49f, -5.2f), new Vector3(0.66f, 0.98f, 6.4f), white, 1f);
        Part(p, new Vector3(ox + 14.45f, 1.0f, -5.2f), new Vector3(0.7f, 0.06f, 6.45f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox + 7.0f, 1.02f, -9.3f), new Vector3(0.8f, 0.05f, 0.55f), C("#8d949c"), 1f);
        Part(p, new Vector3(ox + 11.0f, 1.02f, -9.3f), new Vector3(0.9f, 0.05f, 0.55f), C("#3a3f47"), 1f);
        Part(p, new Vector3(ox + 5.2f, 1.1f, -9.15f), new Vector3(0.95f, 2.2f, 0.75f), metal, 1f);
        Part(p, new Vector3(ox + 9.5f, 2.55f, -9.58f), new Vector3(8.0f, 0.8f, 0.38f), white, 1f);
        Part(p, new Vector3(ox + 9.5f, 0.49f, -3.2f), new Vector3(5.6f, 0.98f, 1.25f), white, 1f);
        Part(p, new Vector3(ox + 9.5f, 1.0f, -3.2f), new Vector3(5.7f, 0.06f, 1.3f), C("#d9d3c6"), 1f);
        for (int i = 0; i < 3; i++)
            Part(p, new Vector3(ox + 7.9f + i * 1.6f, 0.36f, -1.9f), new Vector3(0.42f, 0.72f, 0.42f), C("#b98d5e"), 0.8f);
        Lamp(p, new Vector3(ox + 9.5f, 3.55f, -4f), 11f, 1.15f);

        // 主卧（x -15..-9.4, z 2..10）
        Part(p, new Vector3(ox - 13.7f, 0.25f, 6.0f), new Vector3(2.2f, 0.5f, 2.0f), woodDark, 1f);
        Part(p, new Vector3(ox - 13.7f, 0.58f, 6.0f), new Vector3(2.1f, 0.26f, 1.9f), C("#e8e4da"), 1f);
        Part(p, new Vector3(ox - 13.7f, 0.74f, 6.9f), new Vector3(1.9f, 0.14f, 0.85f), C("#cfd6dd"), 1f);
        Part(p, new Vector3(ox - 13.7f, 0.76f, 5.15f), new Vector3(1.2f, 0.15f, 0.4f), C("#f2eee4"), 1f);
        Part(p, new Vector3(ox - 14.7f, 0.78f, 6.0f), new Vector3(0.12f, 1.0f, 2.0f), woodDark, 1f);
        Part(p, new Vector3(ox - 13.7f, 0.28f, 4.35f), new Vector3(0.55f, 0.56f, 0.55f), woodDark, 1f);
        Part(p, new Vector3(ox - 13.7f, 0.28f, 7.65f), new Vector3(0.55f, 0.56f, 0.55f), woodDark, 1f);
        Part(p, new Vector3(ox - 10.0f, 1.25f, 5.0f), new Vector3(0.65f, 2.5f, 2.4f), woodDark, 1f);
        Rug(p, new Vector3(ox - 11.6f, 0.04f, 6.0f), 2.6f, 3.4f, fabric2);
        Lamp(p, new Vector3(ox - 12f, 3.55f, 6.0f), 10f, 1.15f);

        // 书房（x -9.4..-2.2）
        Part(p, new Vector3(ox - 5.8f, 0.85f, 9.2f), new Vector3(2.2f, 0.06f, 0.8f), wood, 1f);
        Part(p, new Vector3(ox - 5.8f, 0.4f, 9.2f), new Vector3(2.1f, 0.8f, 0.7f), wood, 1f);
        Chair(p, it, new Vector3(ox - 5.8f, 0f, 8.25f), 180f, wood, true);
        Part(p, new Vector3(ox - 8.95f, 1.1f, 5.5f), new Vector3(0.4f, 2.2f, 2.6f), woodDark, 1f);
        Part(p, new Vector3(ox - 3.4f, 0.48f, 4.6f), new Vector3(1.1f, 0.5f, 1.0f), fabric, 1.2f);
        Part(p, new Vector3(ox - 3.4f, 0.72f, 5.1f), new Vector3(1.1f, 0.95f, 0.2f), fabric, 1.2f);
        Lamp(p, new Vector3(ox - 5.8f, 3.55f, 6.5f), 9f, 1.1f);

        // 卫生间（x -2.2..3.4）
        Part(p, new Vector3(ox + 0.6f, 0.34f, 9.2f), new Vector3(2.2f, 0.68f, 1.0f), white, 1f);
        Part(p, new Vector3(ox + 0.6f, 0.6f, 9.2f), new Vector3(2.0f, 0.22f, 0.84f), C("#cfe3e8"), 1f);
        Part(p, new Vector3(ox - 1.65f, 0.47f, 4.5f), new Vector3(0.6f, 0.94f, 1.15f), white, 1f);
        Part(p, new Vector3(ox - 1.65f, 0.96f, 4.5f), new Vector3(0.65f, 0.07f, 1.2f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox + 2.8f, 0.4f, 4.5f), new Vector3(0.5f, 0.8f, 0.75f), white, 1f);
        Part(p, new Vector3(ox + 2.8f, 0.88f, 4.5f), new Vector3(0.55f, 0.07f, 0.8f), C("#d9d3c6"), 1f);
        Lamp(p, new Vector3(ox + 0.6f, 3.55f, 6.5f), 7f, 0.95f);

        // 家务间（x 3.4..15）
        Part(p, new Vector3(ox + 4.4f, 0.49f, 3.5f), new Vector3(0.72f, 0.98f, 0.72f), white, 1f);
        Part(p, new Vector3(ox + 4.4f, 1.0f, 3.5f), new Vector3(0.74f, 0.05f, 0.74f), C("#c9ced3"), 1f);
        Part(p, new Vector3(ox + 5.5f, 0.49f, 3.5f), new Vector3(0.72f, 0.98f, 0.72f), white, 1f);
        Part(p, new Vector3(ox + 7.4f, 0.49f, 3.5f), new Vector3(1.6f, 0.98f, 0.72f), white, 1f);
        for (int i = 0; i < 3; i++)
            Part(p, new Vector3(ox + 14.1f, 1.1f, 5.0f + i * 1.55f), new Vector3(1.4f, 2.2f, 0.5f), woodDark, 1f);
        Lamp(p, new Vector3(ox + 9f, 3.55f, 6.5f), 10f, 0.9f);

        // ── 家具碰撞 ──
        Reg(it, ox - 14.5f, -4.0f, 0.5f, 2.8f);
        Reg(it, ox - 11.5f, -4.0f, 1.3f, 0.7f);
        Reg(it, ox - 11.3f, -4.0f, 1.0f, 3.3f);
        Reg(it, ox - 9.0f, -4.0f, 0.95f, 1.5f);
        Reg(it, ox - 8.6f, 1.62f, 2.4f, 0.4f);
        Reg(it, ox + 2.9f, -9.0f, 0.45f, 1.6f);
        Reg(it, ox - 2.6f, -9.25f, 0.9f, 0.4f);
        Reg(it, ox, -4.2f, 2.4f, 1.1f);
        foreach (var (cx2, cz2) in new[] { (0f, -5.6f), (0f, -2.8f), (-1.9f, -4.2f), (1.9f, -4.2f), (-1.9f, -5.6f), (1.9f, -5.6f) })
            Reg(it, ox + cx2, cz2, 0.56f, 0.56f);
        Reg(it, ox + 9.5f, -9.35f, 9.0f, 0.66f);
        Reg(it, ox + 14.45f, -5.2f, 0.66f, 6.4f);
        Reg(it, ox + 5.2f, -9.15f, 0.95f, 0.75f);
        Reg(it, ox + 9.5f, -3.2f, 5.6f, 1.25f);
        Reg(it, ox - 13.7f, 6.0f, 2.2f, 2.0f);
        Reg(it, ox - 13.7f, 4.35f, 0.55f, 0.55f);
        Reg(it, ox - 13.7f, 7.65f, 0.55f, 0.55f);
        Reg(it, ox - 10.0f, 5.0f, 0.65f, 2.4f);
        Reg(it, ox - 5.8f, 9.2f, 2.2f, 0.8f);
        Reg(it, ox - 8.95f, 5.5f, 0.4f, 2.6f);
        Reg(it, ox - 3.4f, 4.6f, 1.1f, 1.0f);
        Reg(it, ox + 0.6f, 9.2f, 2.2f, 1.0f);
        Reg(it, ox - 1.65f, 4.5f, 0.65f, 1.2f);
        Reg(it, ox + 2.8f, 4.5f, 0.55f, 0.8f);
        Reg(it, ox + 4.4f, 3.5f, 0.74f, 0.74f);
        Reg(it, ox + 5.5f, 3.5f, 0.74f, 0.74f);
        Reg(it, ox + 7.4f, 3.5f, 1.6f, 0.74f);
        for (int i = 0; i < 3; i++) Reg(it, ox + 14.1f, 5.0f + i * 1.55f, 1.4f, 0.5f);

        return it;
    }

    // ── 教学楼：56×30 单层（方案 A）──
    static Interior School(Transform parent)
    {
        float ox = SCHOOL_X;
        var it = new Interior
        {
            Id = "school",
            Name = "教学楼",
            X1 = ox - 28f, X2 = ox + 28f, Z1 = -15f, Z2 = 15f,
            Spawn = new Vector2(ox, 11f),
            Exit = new Vector2(ox, 13.9f),
            FloorY = new[] { 0.02f },
        };
        var root = new GameObject("Interior_school");
        root.transform.SetParent(parent, false);
        it.Root = root;
        var p = root.transform;

        var wall = T("in_wall");
        var wallB = T("in_wall2");
        var floor = T("in_tile");
        var ceil = T("in_ceil");
        var wood = T("wood");
        var board = T("in_board");
        var woodDark = C("#6f5540");
        var deskTop = C("#c9a878");
        var green = C("#4a6a52");

        const float W = 4.2f;
        const float ZC0 = -2.5f, ZC1 = 2.5f;

        // ── 地面 / 天花 ──
        Part(p, new Vector3(ox, 0f, 0f), new Vector3(56f, 0.04f, 30f), floor, 2f, default, true);
        Part(p, new Vector3(ox, W - 0.03f, 0f), new Vector3(56f, 0.06f, 30f), ceil, 2f);

        // ── 外墙（南墙正中 5m 大门）──
        WallX(p, it, ox - 28f, ox + 28f, -15f, 0f, W, 0f, 0f, 2.8f, wall);
        WallX(p, it, ox - 28f, ox + 28f, 15f, 0f, W, ox - 2.5f, ox + 2.5f, 2.8f, wall);
        WallZ(p, it, -15f, 15f, ox - 28f, 0f, W, 0f, 0f, 2.8f, wall);
        WallZ(p, it, -15f, 15f, ox + 28f, 0f, W, 0f, 0f, 2.8f, wall);

        // ── 教室隔墙 ──
        WallZ(p, it, -15f, ZC0, ox - 15.25f, 0.02f, W, 0f, 0f, 2.5f, wall);
        WallZ(p, it, -15f, ZC0, ox - 2.75f, 0.02f, W, 0f, 0f, 2.5f, wall);
        WallZ(p, it, -15f, ZC0, ox + 9.75f, 0.02f, W, 0f, 0f, 2.5f, wall);

        // ── 走廊北墙（z=-2.5）：教室门 ──
        WallX(p, it, ox - 28f, ox - 19.6f, ZC0, 0.02f, W, 0f, 0f, 2.5f, wall);
        WallX(p, it, ox - 17.6f, ox - 11.4f, ZC0, 0.02f, W, 0f, 0f, 2.5f, wall);
        WallX(p, it, ox - 9.4f, ox - 0.8f, ZC0, 0.02f, W, 0f, 0f, 2.5f, wall);
        WallX(p, it, ox + 1.2f, ox + 13.6f, ZC0, 0.02f, W, 0f, 0f, 2.5f, wall);
        WallX(p, it, ox + 15.6f, ox + 28f, ZC0, 0.02f, W, 0f, 0f, 2.5f, wall);

        // ── 南带：办公室 / 图书角墙（门厅开放）──
        WallX(p, it, ox + 7.5f, ox + 12.4f, ZC1, 0.02f, W, 0f, 0f, 2.5f, wall);
        WallX(p, it, ox + 13.6f, ox + 21.0f, ZC1, 0.02f, W, 0f, 0f, 2.5f, wall);
        WallX(p, it, ox + 22.2f, ox + 28f, ZC1, 0.02f, W, 0f, 0f, 2.5f, wall);
        WallZ(p, it, ZC1, 15f, ox + 7.5f, 0.02f, W, 0f, 0f, 2.5f, wall);
        WallZ(p, it, ZC1, 15f, ox + 16.5f, 0.02f, W, 0f, 0f, 2.5f, wall);

        // ── 窗 ──
        Window(p, new Vector3(ox - 24f, 2.1f, -14.80f), 3.0f, 1.7f, 180f);
        Window(p, new Vector3(ox - 19f, 2.1f, -14.80f), 3.0f, 1.7f, 180f);
        Window(p, new Vector3(ox - 11.5f, 2.1f, -14.80f), 3.0f, 1.7f, 180f);
        Window(p, new Vector3(ox - 6.5f, 2.1f, -14.80f), 3.0f, 1.7f, 180f);
        Window(p, new Vector3(ox + 2.0f, 2.1f, -14.80f), 3.0f, 1.7f, 180f);
        Window(p, new Vector3(ox + 7.0f, 2.1f, -14.80f), 3.0f, 1.7f, 180f);
        Window(p, new Vector3(ox + 14.75f, 2.1f, -14.80f), 3.5f, 1.7f, 180f);
        Window(p, new Vector3(ox + 20.25f, 2.1f, -14.80f), 3.5f, 1.7f, 180f);
        Window(p, new Vector3(ox + 25.25f, 2.1f, -14.80f), 2.5f, 1.7f, 180f);
        Window(p, new Vector3(ox + 11f, 2.1f, 14.80f), 3.0f, 1.7f, 0f);
        Window(p, new Vector3(ox + 20f, 2.1f, 14.80f), 3.0f, 1.7f, 0f);
        Window(p, new Vector3(ox + 27.80f, 2.1f, -9.5f), 2.4f, 1.7f, -90f);
        Window(p, new Vector3(ox + 27.80f, 2.1f, 3.5f), 2.4f, 1.7f, -90f);
        Window(p, new Vector3(ox + 27.80f, 2.1f, 8.5f), 2.4f, 1.7f, -90f);

        // ── 出口：玻璃门扇 + 门廊 ──
        Window(p, new Vector3(ox, 1.45f, 14.81f), 4.4f, 2.7f, 0f);
        Porch(p, ox, 15f, 6.0f, 1f, 3.1f);

        // ── 墙纸 ──
        WallDeco(p, ox - 27.86f, ox + 27.86f, -14.86f, 0.02f, W, 0, 0.05f, wallB, 1.7f);
        WallDeco(p, ox - 27.86f, ox + 27.86f, 14.86f, 0.02f, W, 0, 0.05f, wall, 1.7f);
        WallDeco(p, -14.86f, 14.86f, ox - 27.86f, 0.02f, W, 1, 0.05f, wallB, 1.7f);
        WallDeco(p, -14.86f, 14.86f, ox + 27.86f, 0.02f, W, 1, 0.05f, wallB, 1.7f);
        WallDeco(p, ox - 27.86f, ox + 27.86f, ZC0 + 0.11f, 0.02f, W, 0, 0.05f, wall, 1.7f,
                 new[] { (ox - 19.6f, ox - 17.6f), (ox - 11.4f, ox - 9.4f), (ox - 0.8f, ox + 1.2f), (ox + 13.6f, ox + 15.6f) });
        WallDeco(p, ox - 27.86f, ox + 27.86f, ZC1 - 0.09f, 0.02f, W, 0, 0.05f, wallB, 1.7f,
                 new[] { (ox + 12.4f, ox + 13.6f), (ox + 21.0f, ox + 22.2f) });
        WallDeco(p, ZC1 + 0.09f, 14.86f, ox + 7.41f, 0.02f, W, 1, 0.05f, wall, 1.7f);
        WallDeco(p, ZC1 + 0.09f, 14.86f, ox + 7.59f, 0.02f, W, 1, 0.05f, wallB, 1.7f);
        WallDeco(p, ZC1 + 0.09f, 14.86f, ox + 16.41f, 0.02f, W, 1, 0.05f, wallB, 1.7f);
        WallDeco(p, ZC1 + 0.09f, 14.86f, ox + 16.59f, 0.02f, W, 1, 0.05f, wall, 1.7f);
        foreach (var xx in new[] { -15.25f, -2.75f, 9.75f })
        {
            WallDeco(p, -14.86f, ZC0 + 0.09f, ox + xx - 0.11f, 0.02f, W, 1, 0.05f, wallB, 1.7f);
            WallDeco(p, -14.86f, ZC0 + 0.09f, ox + xx + 0.11f, 0.02f, W, 1, 0.05f, wallB, 1.7f);
        }
        Skirt(p, ox - 27.86f, -14.84f, ox + 27.86f, -14.84f, 0.02f);
        Skirt(p, ox - 27.86f, 14.84f, ox + 27.86f, 14.84f, 0.02f);

        // ── 门厅 ──
        Part(p, new Vector3(ox + 4.5f, 0.6f, 12.8f), new Vector3(3.0f, 1.2f, 0.7f), woodDark, 1f);
        Part(p, new Vector3(ox + 4.5f, 1.23f, 12.8f), new Vector3(3.1f, 0.07f, 0.76f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox - 8f, 1.75f, 14.65f), new Vector3(3.2f, 1.5f, 0.08f), green, 1f);
        Plant(p, new Vector3(ox - 19.2f, 0.02f, 13.8f), 1.2f);
        Plant(p, new Vector3(ox - 5.5f, 0.02f, 13.5f), 1.0f);
        Rug(p, new Vector3(ox, 0.06f, 13.9f), 3.4f, 1.6f, C("#8d9aa6"));
        Lamp(p, new Vector3(ox, 3.9f, 11f), 12f, 1.35f);
        Lamp(p, new Vector3(ox - 14f, 3.9f, 8f), 11f, 1.2f);
        Lamp(p, new Vector3(ox + 14f, 3.9f, 8f), 11f, 1.2f);

        // ── 办公室 ──
        Table(p, new Vector3(ox + 12f, 0f, 12.4f), 2.0f, 1.0f, 0.76f, wood);
        Chair(p, it, new Vector3(ox + 12f, 0f, 13.3f), 180f, wood, true);
        Table(p, new Vector3(ox + 9.8f, 0f, 6.2f), 1.8f, 0.9f, 0.76f, wood);
        Chair(p, it, new Vector3(ox + 9.8f, 0f, 7.1f), 180f, wood, true);
        Part(p, new Vector3(ox + 15.9f, 1.05f, 5.4f), new Vector3(0.5f, 2.1f, 1.7f), woodDark, 1f);
        Lamp(p, new Vector3(ox + 12f, 3.9f, 9f), 10f, 1.05f);

        // ── 图书角 ──
        for (int i = 0; i < 4; i++)
            Part(p, new Vector3(ox + 26.2f, 1.05f, 4.6f + i * 1.9f), new Vector3(0.5f, 2.1f, 1.5f), woodDark, 1f);
        Table(p, new Vector3(ox + 21.5f, 0f, 6.0f), 1.8f, 0.9f, 0.76f, wood);
        Table(p, new Vector3(ox + 21.5f, 0f, 9.5f), 1.8f, 0.9f, 0.76f, wood);
        Lamp(p, new Vector3(ox + 22f, 3.9f, 8f), 10f, 1.05f);

        // ── 教室 ──
        Classroom(p, it, ox - 21.4f, 12.3f, board, deskTop, wood, green, ox - 19.6f, ox - 17.6f, -1.0f);
        Classroom(p, it, ox - 9.0f, 12.3f, board, deskTop, wood, green, ox - 11.4f, ox - 9.4f, 2.5f);
        Classroom(p, it, ox + 3.6f, 12.3f, board, deskTop, wood, green, ox - 0.8f, ox + 1.2f, 0.6f);
        Classroom(p, it, ox + 18.75f, 17.3f, board, deskTop, wood, green, ox + 13.6f, ox + 15.6f);

        // ── 走廊灯 ──
        for (int i = -3; i <= 3; i++)
            Lamp(p, new Vector3(ox + i * 8.5f, 3.9f, 0f), 11f, 1.15f);

        // ── 家具碰撞 ──
        Reg(it, ox + 4.5f, 12.8f, 3.0f, 0.7f);
        Reg(it, ox - 8f, 14.65f, 3.2f, 0.2f);
        Reg(it, ox + 12f, 12.4f, 2.0f, 1.0f);
        Reg(it, ox + 9.8f, 6.2f, 1.8f, 0.9f);
        Reg(it, ox + 15.9f, 5.4f, 0.5f, 1.7f);
        for (int i = 0; i < 4; i++) Reg(it, ox + 26.2f, 4.6f + i * 1.9f, 0.5f, 1.5f);
        Reg(it, ox + 21.5f, 6.0f, 1.8f, 0.9f);
        Reg(it, ox + 21.5f, 9.5f, 1.8f, 0.9f);

        return it;
    }

    // 一间教室：黑板在走廊侧内墙（避开北窗眩光），40/60 座
    static void Classroom(Transform p, Interior it, float cx, float w, Material board,
                          Material desk, Material wood, Material green,
                          float doorA = 0f, float doorB = 0f, float boardShift = 0f)
    {
        float zFront = -2.5f - 0.13f;   // 走廊墙教室侧表面（墙半厚 0.12 + 0.01）
        float zBack = -14.86f;
        float cz = (zFront + zBack) * 0.5f;
        float bx = cx + boardShift;     // 黑板/讲台/挂钟整体偏移，避开教室门

        Part(p, new Vector3(bx, 1.7f, zFront - 0.07f), new Vector3(5.0f, 1.3f, 0.07f), board, 5.0f);
        var skirt = C("#8fae98");
        float sx1 = cx - w * 0.5f + 0.05f, sx2 = cx + w * 0.5f - 0.05f;
        // 走廊那面墙的墙裙必须按门洞断开：否则一条 1.1m 高的绿带横在门口，进门像被挡住
        if (doorB > doorA)
        {
            float da = Mathf.Clamp(doorA - 0.12f, sx1, sx2), db = Mathf.Clamp(doorB + 0.12f, sx1, sx2);
            Panel(p, sx1, zFront - 0.04f, da, zFront - 0.04f, 0.02f, 1.1f, skirt);
            Panel(p, db, zFront - 0.04f, sx2, zFront - 0.04f, 0.02f, 1.1f, skirt);
        }
        else Panel(p, sx1, zFront - 0.04f, sx2, zFront - 0.04f, 0.02f, 1.1f, skirt);
        Panel(p, sx1, zBack + 0.03f, sx2, zBack + 0.03f, 0.02f, 1.1f, skirt);
        // 侧墙墙裙要让到墙皮（±0.11）外面，否则被墙皮盖住看不见
        Panel(p, cx - w * 0.5f + 0.19f, zBack + 0.05f, cx - w * 0.5f + 0.19f, zFront - 0.05f, 0.02f, 1.1f, skirt);
        Panel(p, cx + w * 0.5f - 0.19f, zBack + 0.05f, cx + w * 0.5f - 0.19f, zFront - 0.05f, 0.02f, 1.1f, skirt);
        WallClock(p, new Vector3(bx, 2.9f, zFront - 0.18f), 0f);
        Part(p, new Vector3(bx, 0.45f, zFront - 0.85f), new Vector3(2.2f, 0.9f, 0.8f), wood, 1f);
        Part(p, new Vector3(bx, 0.93f, zFront - 0.85f), new Vector3(2.3f, 0.05f, 0.86f), C("#d9d3c6"), 1f);

        // 12 座均匀铺满：宽教室 4 列×3 排，标准教室 3 列×4 排；行列按房间尺寸拉伸
        int cols = w > 15f ? 4 : 3;
        int rows = 12 / cols;
        float colGap = Mathf.Min(3.2f, (w - 2.6f) / Mathf.Max(1, cols - 1));
        // 让门口正对两条列之间的过道，否则进门第一眼就是一张桌子
        float gridShift = 0f;
        if (doorB > doorA)
        {
            float rel = (doorA + doorB) * 0.5f - cx;
            int k = Mathf.RoundToInt(rel / colGap + (cols - 1) * 0.5f - 0.5f);
            float aisle = (-(cols - 1) * 0.5f + 0.5f + k) * colGap;
            gridShift = Mathf.Clamp(rel - aisle, -1.8f, 1.8f);
        }
        float zTop = zFront - 3.0f, zBot = zBack + 2.6f;
        for (int r = 0; r < rows; r++)
            for (int g = 0; g < cols; g++)
            {
                float gx = cx + gridShift + (g - (cols - 1) * 0.5f) * colGap;
                float z = Mathf.Lerp(zTop, zBot, rows > 1 ? (float)r / (rows - 1) : 0.5f);
                Part(p, new Vector3(gx, 0.74f, z), new Vector3(0.62f, 0.05f, 0.42f), desk, 1.0f);
                for (int lg = 0; lg < 4; lg++)
                    Part(p, new Vector3(gx + (lg % 2 == 0 ? -0.27f : 0.27f), 0.37f,
                                        z + (lg < 2 ? -0.17f : 0.17f)),
                         new Vector3(0.04f, 0.74f, 0.04f), C("#8d8d94"), 0.5f);
                // 椅子放在桌子**远离黑板**的一侧（z 更小），人朝 +z 正好面向桌子与黑板
                Chair(p, it, new Vector3(gx, 0f, z - 0.62f), 0f, wood, false);
                it.Solids.Add(new Solid(gx - 0.34f, z - 0.92f, gx + 0.34f, z + 0.22f));
            }

        Part(p, new Vector3(cx - w * 0.5f + 0.08f, 1.85f, cz + 2.0f), new Vector3(0.05f, 1.3f, 4.2f), green, 4.2f);

        Lamp(p, new Vector3(cx - w * 0.25f, 3.85f, cz - 2.5f), 10f, 0.85f);
        Lamp(p, new Vector3(cx + w * 0.25f, 3.85f, cz - 2.5f), 10f, 0.85f);
        Lamp(p, new Vector3(cx - w * 0.25f, 3.85f, cz + 2.5f), 10f, 0.85f);
        Lamp(p, new Vector3(cx + w * 0.25f, 3.85f, cz + 2.5f), 10f, 0.85f);
    }
}
