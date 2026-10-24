using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────
// 室内场景：家 / 教学楼
//   全部程序化生成，建在世界之外的空地上（x ≈ 900 起），与室外共用贴图库与材质库。
//   进入时把玩家瞬移到室内落点，碰撞/地面高度改走本文件的 AABB 与楼层函数；
//   出口在门口，走近按 F 回到室外原位置。
//   分层：Solids 带楼层掩码（bit0=一层 bit1=二层），楼梯用高度插值，不做真物理。
//   建完会跑 Validate()：家具越界 / 插进墙的会在 Player.log 里逐条报出来。
// ─────────────────────────────────────────────────────────────
public class Interior
{
    public string Id;
    public string Name;
    public float X1, Z1, X2, Z2;      // 平面范围（世界坐标）
    public Vector2 Spawn;             // 室内落点
    public Vector2 Exit;              // 门口
    public float ExitRadius = 1.6f;
    public float[] FloorY;            // 各层地面高度
    public GameObject Root;

    public readonly List<Solid> Solids = new List<Solid>();
    public readonly List<Stair> Stairs = new List<Stair>();

    public int LevelOf(float y)
        => FloorY.Length > 1 && y > (FloorY[0] + FloorY[1]) * 0.5f ? 1 : 0;

    // 站在这点应该踩在多高：楼梯区域按坡度插值，其余按当前所在层
    public float FloorAt(float x, float z, float curY)
    {
        for (int i = 0; i < Stairs.Count; i++)
        {
            var s = Stairs[i];
            if (s.Contains(x, z)) return Mathf.Lerp(FloorY[s.From], FloorY[s.To], s.T(x, z));
        }
        return FloorY[LevelOf(curY)];
    }

    public bool Collides(float x, float z, float curY)
    {
        int bit = 1 << LevelOf(curY);
        for (int i = 0; i < Solids.Count; i++)
        {
            var s = Solids[i];
            if ((s.Mask & bit) == 0) continue;
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

// 碰撞盒（平面 AABB）：Mask 的 bit0 = 只挡一层，bit1 = 只挡二层，3 = 两层都挡
public struct Solid
{
    public float X1, Z1, X2, Z2;
    public int Mask;
    public bool IsWall;      // 墙体：校验时跳过（外墙本来就压在边界上）

    public Solid(float x1, float z1, float x2, float z2, int mask, bool isWall = false)
    {
        X1 = Mathf.Min(x1, x2); X2 = Mathf.Max(x1, x2);
        Z1 = Mathf.Min(z1, z2); Z2 = Mathf.Max(z1, z2);
        Mask = mask;
        IsWall = isWall;
    }
}

// 楼梯：(X1,Z1) 是低端，(X2,Z2) 是高端；AlongX 决定沿哪根轴爬
public struct Stair
{
    public float X1, Z1, X2, Z2;
    public int From, To;
    public bool AlongX;

    public bool Contains(float x, float z)
        => x > Mathf.Min(X1, X2) && x < Mathf.Max(X1, X2)
        && z > Mathf.Min(Z1, Z2) && z < Mathf.Max(Z1, Z2);

    public float T(float x, float z)
    {
        float den = AlongX ? (X2 - X1) : (Z2 - Z1);
        if (Mathf.Abs(den) < 0.001f) return 0f;
        return Mathf.Clamp01(((AlongX ? x - X1 : z - Z1)) / den);
    }
}

// ─────────────────────────────────────────────────────────────
public static class InteriorBuilder
{
    public const int F0 = 1, F1 = 2, BOTH = 3;
    const float HOME_X = 900f, SCHOOL_X = 1060f;
    const float SLAB = 0.18f;
    const float WALL_OUT = 0.24f;    // 外墙厚
    const float WALL_IN = 0.18f;     // 内墙厚

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
    // 视觉件：BoxUV 把尺寸烘进网格，贴图按米平铺，缩放保持 1（碰撞体尺寸才好算）
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

    // 实体件：登记 AABB（挡人）+ 保留碰撞体（相机避障用）
    static GameObject SolidPart(Transform p, Interior it, Vector3 pos, Vector3 size, Material mat,
                                int mask, float texMeters = 1f, bool isWall = false)
    {
        var go = Part(p, pos, size, mat, texMeters, default, true);
        it.Solids.Add(new Solid(pos.x - size.x * 0.5f, pos.z - size.z * 0.5f,
                                pos.x + size.x * 0.5f, pos.z + size.z * 0.5f, mask, isWall));
        return go;
    }

    // 轴向墙：两端必须共线
    static void Wall(Transform p, Interior it, float x1, float z1, float x2, float z2,
                     float y0, float h, Material mat, int mask, float thick = WALL_IN)
    {
        if (Mathf.Abs(x1 - x2) < 0.01f && Mathf.Abs(z1 - z2) < 0.01f) return;
        float w = Mathf.Max(Mathf.Abs(x2 - x1), thick);
        float d = Mathf.Max(Mathf.Abs(z2 - z1), thick);
        SolidPart(p, it, new Vector3((x1 + x2) * 0.5f, y0 + h * 0.5f, (z1 + z2) * 0.5f),
                  new Vector3(w, h, d), mat, mask, 1.7f, true);
    }

    // 沿 X 的墙，中间留门洞（门楣不挡人）
    static void WallX(Transform p, Interior it, float x1, float x2, float z, float y0, float h,
                      float doorA, float doorB, float doorH, Material mat, int mask, float thick = WALL_IN)
    {
        if (doorB <= doorA) { Wall(p, it, x1, z, x2, z, y0, h, mat, mask, thick); return; }
        Wall(p, it, x1, z, doorA, z, y0, h, mat, mask, thick);
        Wall(p, it, doorB, z, x2, z, y0, h, mat, mask, thick);
        float lintel = h - doorH;
        if (lintel > 0.05f)
            Part(p, new Vector3((doorA + doorB) * 0.5f, y0 + doorH + lintel * 0.5f, z),
                 new Vector3(doorB - doorA, lintel, thick), mat, 1.7f, default, true);
    }

    // 沿 Z 的墙，中间留门洞
    static void WallZ(Transform p, Interior it, float z1, float z2, float x, float y0, float h,
                      float doorA, float doorB, float doorH, Material mat, int mask, float thick = WALL_IN)
    {
        if (doorB <= doorA) { Wall(p, it, x, z1, x, z2, y0, h, mat, mask, thick); return; }
        Wall(p, it, x, z1, x, doorA, y0, h, mat, mask, thick);
        Wall(p, it, x, doorB, x, z2, y0, h, mat, mask, thick);
        float lintel = h - doorH;
        if (lintel > 0.05f)
            Part(p, new Vector3(x, y0 + doorH + lintel * 0.5f, (doorA + doorB) * 0.5f),
                 new Vector3(thick, lintel, doorB - doorA), mat, 1.7f, default, true);
    }

    // 楼板 / 天花板：顶面在 topY。topMat 给了就在顶上贴一层地面（否则上下同料）
    static void Slab(Transform p, float x1, float z1, float x2, float z2, float topY, Material mat,
                     Material topMat = null)
    {
        float w = Mathf.Abs(x2 - x1), d = Mathf.Abs(z2 - z1);
        if (w < 0.05f || d < 0.05f) return;
        Part(p, new Vector3((x1 + x2) * 0.5f, topY - SLAB * 0.5f, (z1 + z2) * 0.5f),
             new Vector3(w, SLAB, d), mat, 2f, default, true);
        if (topMat != null)
            Part(p, new Vector3((x1 + x2) * 0.5f, topY - 0.02f, (z1 + z2) * 0.5f),
                 new Vector3(w, 0.05f, d), topMat, 2f);
    }

    // 楼梯：一级一级往上垒实心块（视觉上是踏步，走上去靠 Stair 高度插值）
    static void Stairs(Transform p, Interior it, float x1, float z1, float x2, float z2,
                       int from, int to, bool alongX, Material mat)
    {
        it.Stairs.Add(new Stair { X1 = x1, Z1 = z1, X2 = x2, Z2 = z2, From = from, To = to, AlongX = alongX });
        int n = 16;
        float y0 = it.FloorY[from], y1 = it.FloorY[to];
        for (int i = 0; i < n; i++)
        {
            float t0 = (float)i / n, t1 = (float)(i + 1) / n;
            float top = Mathf.Lerp(y0, y1, t1);
            float h = Mathf.Max(0.06f, top - y0);
            if (alongX)
            {
                float a = Mathf.Lerp(x1, x2, t0), b = Mathf.Lerp(x1, x2, t1);
                Part(p, new Vector3((a + b) * 0.5f, y0 + h * 0.5f, (z1 + z2) * 0.5f),
                     new Vector3(Mathf.Abs(b - a) + 0.02f, h, Mathf.Abs(z2 - z1)), mat, 1.2f, default, true);
            }
            else
            {
                float a = Mathf.Lerp(z1, z2, t0), b = Mathf.Lerp(z1, z2, t1);
                Part(p, new Vector3((x1 + x2) * 0.5f, y0 + h * 0.5f, (a + b) * 0.5f),
                     new Vector3(Mathf.Abs(x2 - x1), h, Mathf.Abs(b - a) + 0.02f), mat, 1.2f, default, true);
            }
        }
    }

    // 扶手（斜杆）
    static void Rail(Transform p, Vector3 a, Vector3 b, Material mat, float thick = 0.07f)
    {
        var dir = b - a;
        float len = dir.magnitude;
        if (len < 0.05f) return;
        var go = Part(p, (a + b) * 0.5f, new Vector3(thick, thick, len), mat, 1f);
        go.transform.localRotation = Quaternion.LookRotation(dir / len, Vector3.up);
    }

    // 室内灯：点光 + 灯罩（不投影，省性能）
    static void Lamp(Transform p, Vector3 pos, float range, float intensity, string hex = "#ffe9c8")
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

    // 家具碰撞盒（只登记平面 AABB，挡人用）
    static void Reg(Interior it, float cx, float cz, float w, float d, int mask)
        => it.Solids.Add(new Solid(cx - w * 0.5f, cz - d * 0.5f, cx + w * 0.5f, cz + d * 0.5f, mask));

    static Material _glass;
    static Material Glass()
    {
        if (_glass == null) _glass = MatLib.UnlitColor(MatLib.Hex("#d8ecf6"));
        return _glass;
    }

    // 窗：玻璃面 + 四边框 + 中梃（贴在墙内表面）
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

    // 贴地薄片（地毯 / 地垫）
    static void Rug(Transform p, Vector3 pos, float w, float d, Material mat)
        => Part(p, pos, new Vector3(w, 0.02f, d), mat, 1f);

    // ── 家具小件 ──────────────────────────────────────────
    static void Chair(Transform p, Interior it, Vector3 seat, float ry, Material wood, int mask, bool solid = false)
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
        if (solid) it.Solids.Add(new Solid(seat.x - 0.28f, seat.z - 0.28f, seat.x + 0.28f, seat.z + 0.28f, mask));
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

    // ═════════════════════════════════════════════════════
    public static List<Interior> Build(Transform parent)
    {
        _cache.Clear();
        var list = new List<Interior> { Home(parent), School(parent) };
        foreach (var it in list) Validate(it);
        return list;
    }

    // 校验：家具不许越出外墙内表面，也不许插进内墙
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
                Debug.LogWarning($"[Interior:{it.Id}] 越界 x[{s.X1:F2},{s.X2:F2}] z[{s.Z1:F2},{s.Z2:F2}] " +
                                 $"边界 x[{bx1:F2},{bx2:F2}] z[{bz1:F2},{bz2:F2}]");
                bad++;
                continue;
            }
            foreach (var w in it.Solids)
            {
                if (!w.IsWall || (w.Mask & s.Mask) == 0) continue;
                float ox = Mathf.Min(s.X2, w.X2) - Mathf.Max(s.X1, w.X1);
                float oz = Mathf.Min(s.Z2, w.Z2) - Mathf.Max(s.Z1, w.Z1);
                if (ox > 0.06f && oz > 0.06f)
                {
                    Debug.LogWarning($"[Interior:{it.Id}] 插进墙 x[{s.X1:F2},{s.X2:F2}] z[{s.Z1:F2},{s.Z2:F2}] " +
                                     $"与墙 x[{w.X1:F2},{w.X2:F2}] z[{w.Z1:F2},{w.Z2:F2}] 重叠 {ox:F2}×{oz:F2}");
                    bad++;
                    break;
                }
            }
        }
        Debug.Log($"[Interior:{it.Id}] 校验完成：{it.Solids.Count} 个碰撞盒，问题 {bad} 处");
    }

    // ── 家：16×12 两层，一层 8 个空间（玄关/客厅/餐厅/厨房/走廊/客卫/储藏/家务+楼梯）──
    //    尺寸依据：双人卧室≥9㎡、起居室≥10㎡、厨房≥4㎡（住宅设计规范）；
    //    家具按人体工程学常规尺寸（沙发进深 0.9、电视柜深 0.5 高 0.6、餐桌 0.75 高…）
    static Interior Home(Transform parent)
    {
        float ox = HOME_X;
        var it = new Interior
        {
            Id = "home",
            Name = "家",
            X1 = ox - 8f, X2 = ox + 8f, Z1 = -6f, Z2 = 6f,
            Spawn = new Vector2(ox, -4.6f),
            Exit = new Vector2(ox, -5.3f),
            FloorY = new[] { 0.02f, 3.40f },
        };
        var root = new GameObject("Interior_home");
        root.transform.SetParent(parent, false);
        it.Root = root;
        var p = root.transform;

        var wall = T("in_wall");
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

        const float W = 3.2f;          // 净高
        const float top = 6.60f;       // 二层天花顶面 = 3.40 + 3.2
        float y1 = it.FloorY[1];
        float h0 = W - 0.02f;          // 一层墙高（到楼板底 3.22）
        float h1 = W - 0.02f;

        // 分区线（世界坐标）：x 西 -8 / 中 -1.6 / 3.4 / 东 8；z 南 -6 / 走廊 0..1.6 / 北 6
        const float XW = -8f, XA = -1.6f, XB = 3.4f, XE = 8f;
        const float ZS = -6f, ZM = 0f, ZC = 1.6f, ZN = 6f;

        // ── 地面 ──
        Part(p, new Vector3(ox, 0f, 0f), new Vector3(16f, 0.04f, 12f), floor, 2f, default, true);
        Part(p, new Vector3(ox + 2.2f, 0.05f, 3.8f), new Vector3(9.6f, 0.02f, 4.4f), tile, 1.4f);   // 厨房/卫生间区瓷砖

        // ── 外墙 ──
        WallX(p, it, ox + XW, ox + XE, ZS, 0f, top, ox - 1.0f, ox + 1.0f, 2.3f, wall, BOTH, WALL_OUT);
        WallX(p, it, ox + XW, ox + XE, ZN, 0f, top, ox + 1.2f, ox + 2.2f, 2.3f, wall, BOTH, WALL_OUT);  // 后门
        WallZ(p, it, ZS, ZN, ox + XW, 0f, top, 0f, 0f, 2.3f, wall, BOTH, WALL_OUT);
        WallZ(p, it, ZS, ZN, ox + XE, 0f, top, 0f, 0f, 2.3f, wall, BOTH, WALL_OUT);

        // ── 二层楼板（留楼梯洞 x 3.6..5.2 / z 1.8..5.6）──
        Slab(p, ox + XW, ZS, ox + XE, 1.8f, y1, ceil, floor);
        Slab(p, ox + XW, 1.8f, ox + 3.6f, 5.6f, y1, ceil, floor);
        Slab(p, ox + 5.2f, 1.8f, ox + XE, 5.6f, y1, ceil, floor);
        Slab(p, ox + XW, 5.6f, ox + XE, ZN, y1, ceil, floor);
        Slab(p, ox + XW, ZS, ox + XE, ZN, top, ceil);     // 二层天花

        // ── 一层隔墙 ──
        // 客厅 / 玄关餐厅：x=-1.6，门洞 z -1.4..-0.2
        WallZ(p, it, ZS, ZM, ox + XA, 0.02f, h0, -1.4f, -0.2f, 2.2f, wall, F0);
        // 餐厅 / 厨房：x=3.4，门洞 z -2.2..-1.0
        WallZ(p, it, ZS, ZM, ox + XB, 0.02f, h0, -2.2f, -1.0f, 2.2f, wall, F0);
        // 南带北墙 z=0：客厅段实墙，餐厅段与厨房段各开门
        WallX(p, it, ox + XW, ox + XA, ZM, 0.02f, h0, 0f, 0f, 2.2f, wall, F0);
        WallX(p, it, ox + XA, ox + XB, ZM, 0.02f, h0, ox - 1.2f, ox + 0.0f, 2.2f, wall, F0);
        WallX(p, it, ox + XB, ox + XE, ZM, 0.02f, h0, ox + 6.0f, ox + 7.2f, 2.2f, wall, F0);
        // 北带南墙 z=1.6：客卫 / 储藏 / 家务间各开门，楼梯间敞开
        WallX(p, it, ox + XW, ox - 5.4f, ZC, 0.02f, h0, ox - 7.4f, ox - 6.4f, 2.2f, wall, F0);
        WallX(p, it, ox - 5.4f, ox + XA, ZC, 0.02f, h0, ox - 4.6f, ox - 3.6f, 2.2f, wall, F0);
        WallX(p, it, ox + XA, ox + XB, ZC, 0.02f, h0, ox + 0.0f, ox + 1.2f, 2.2f, wall, F0);
        // 北带竖墙
        WallZ(p, it, ZC, ZN, ox - 5.4f, 0.02f, h0, 0f, 0f, 2.2f, wall, F0);
        WallZ(p, it, ZC, ZN, ox + XA, 0.02f, h0, 0f, 0f, 2.2f, wall, F0);
        WallZ(p, it, ZC, ZN, ox + XB, 0.02f, h0, 0f, 0f, 2.2f, wall, F0);

        // ── 楼梯：x 3.6..5.2，z 1.8(低) → 5.6(高)，宽 1.6 长 3.8 升 3.4 ──
        //    低端退进楼梯间（z=1.8），走廊 z 0..1.6 保持完整，不被楼梯占掉一半
        Stairs(p, it, ox + 3.6f, 1.8f, ox + 5.2f, 5.6f, 0, 1, false, woodDark);
        WallZ(p, it, 1.8f, 5.6f, ox + 5.2f, 0.02f, h0, 0f, 0f, 2.2f, wall, F0);   // 楼梯东侧挡墙（走出楼梯区会掉层）
        Lamp(p, new Vector3(ox + 6.6f, 2.95f, 3.6f), 7f, 1.1f);

        // ── 二层隔墙 ──
        WallZ(p, it, ZS, ZM, ox + XA, y1, h1, 0f, 0f, 2.2f, wall, F1);
        WallZ(p, it, ZS, ZM, ox + XB, y1, h1, 0f, 0f, 2.2f, wall, F1);
        WallX(p, it, ox + XW, ox + XA, ZM, y1, h1, ox - 6.7f, ox - 5.5f, 2.2f, wall, F1);   // 主卧门
        WallX(p, it, ox + XA, ox + XB, ZM, y1, h1, ox + 0.2f, ox + 1.4f, 2.2f, wall, F1);   // 起居厅门
        WallX(p, it, ox + XB, ox + XE, ZM, y1, h1, ox + 6.0f, ox + 7.2f, 2.2f, wall, F1);   // 次卧门
        WallX(p, it, ox + XW, ox + XA, ZC, y1, h1, ox - 3.0f, ox - 1.8f, 2.2f, wall, F1);   // 书房门
        WallX(p, it, ox + XA, ox + XB, ZC, y1, h1, ox - 1.2f, ox + 0.0f, 2.2f, wall, F1);   // 卫生间门
        WallZ(p, it, ZC, ZN, ox + XA, y1, h1, 0f, 0f, 2.2f, wall, F1);
        WallZ(p, it, ZC, ZN, ox + 1.4f, y1, h1, 0f, 0f, 2.2f, wall, F1);                    // 卫生间 / 衣帽间
        WallZ(p, it, ZC, ZN, ox + XB, y1, h1, 0f, 0f, 2.2f, wall, F1);

        // ── 一层窗（朝南外墙）──
        Window(p, new Vector3(ox - 6.4f, 1.75f, -5.83f), 1.8f, 1.4f, 0f);
        Window(p, new Vector3(ox - 4.0f, 1.75f, -5.83f), 1.8f, 1.4f, 0f);
        Window(p, new Vector3(ox + 1.6f, 1.75f, -5.83f), 1.6f, 1.4f, 0f);
        Window(p, new Vector3(ox + 5.9f, 1.75f, -5.83f), 1.4f, 1.2f, 0f);

        // ════════ 一层家具 ════════
        // 客厅 6.4×6.0：沙发靠西墙，电视柜靠东墙，中间地毯 + 茶几（沙发到电视 ≈3.4m）
        Rug(p, new Vector3(ox - 5.9f, 0.06f, -3.0f), 3.6f, 2.6f, fabric2);
        Part(p, new Vector3(ox - 7.35f, 0.42f, -3.0f), new Vector3(0.9f, 0.44f, 2.6f), fabric, 1.2f);      // 沙发座
        Part(p, new Vector3(ox - 7.75f, 0.62f, -3.0f), new Vector3(0.18f, 0.85f, 2.6f), fabric, 1.2f);     // 靠背
        Part(p, new Vector3(ox - 7.35f, 0.60f, -4.42f), new Vector3(0.9f, 0.55f, 0.18f), fabric, 1.2f);    // 扶手
        Part(p, new Vector3(ox - 7.35f, 0.60f, -1.58f), new Vector3(0.9f, 0.55f, 0.18f), fabric, 1.2f);
        Part(p, new Vector3(ox - 5.95f, 0.21f, -3.0f), new Vector3(1.2f, 0.42f, 0.6f), woodDark, 1f);      // 茶几
        Part(p, new Vector3(ox - 2.15f, 0.30f, -3.0f), new Vector3(0.5f, 0.60f, 2.0f), woodDark, 1f);      // 电视柜
        Part(p, new Vector3(ox - 1.95f, 1.25f, -3.0f), new Vector3(0.06f, 0.75f, 1.3f), screen, 1f);       // 电视（挂墙）
        Part(p, new Vector3(ox - 6.4f, 0.90f, -0.28f), new Vector3(2.0f, 1.8f, 0.35f), woodDark, 1f);      // 书架靠北墙
        Lamp(p, new Vector3(ox - 5.9f, 2.85f, -3.0f), 8f, 1.2f);
        Part(p, new Vector3(ox - 7.5f, 0.9f, -0.8f), new Vector3(0.22f, 1.8f, 0.22f), metal, 1f);          // 落地灯杆
        Part(p, new Vector3(ox - 7.5f, 1.86f, -0.8f), new Vector3(0.42f, 0.3f, 0.42f), C("#fff2d6"), 1f);

        // 玄关 5.0×2.8：鞋柜（进深 0.35）靠东墙，换鞋凳，穿衣镜
        Part(p, new Vector3(ox + 3.05f, 0.50f, -4.9f), new Vector3(0.35f, 1.0f, 1.6f), woodDark, 1f);      // 鞋柜
        Part(p, new Vector3(ox + 1.9f, 0.21f, -5.3f), new Vector3(0.8f, 0.42f, 0.35f), wood, 1f);          // 换鞋凳
        Part(p, new Vector3(ox + 3.2f, 0.85f, -3.6f), new Vector3(0.05f, 1.2f, 0.7f), C("#b8c6cc"), 1f);   // 穿衣镜
        Lamp(p, new Vector3(ox + 0.9f, 2.85f, -4.6f), 6f, 0.95f);

        // 餐厅 5.0×3.2：1.6×0.9 餐桌 + 4 椅（桌边留 0.9m 通行）
        // 餐桌靠东墙摆，西半留出「大门 → 走廊」的直线通道（原来桌子居中，正好卡在动线上）
        Table(p, new Vector3(ox + 2.4f, 0f, -1.6f), 1.6f, 0.9f, 0.75f, wood);
        Chair(p, it, new Vector3(ox + 2.4f, 0f, -2.5f), 0f, wood, F0, true);    // 北侧：靠背朝北，人朝桌子
        Chair(p, it, new Vector3(ox + 2.4f, 0f, -0.7f), 180f, wood, F0, true);  // 南侧：靠背朝南
        Chair(p, it, new Vector3(ox + 1.25f, 0f, -1.6f), 90f, wood, F0, true);  // 西侧：靠背朝西
        Lamp(p, new Vector3(ox + 0.9f, 2.80f, -1.6f), 7f, 1.1f);

        // 厨房 4.6×6.0：L 型台面（深 0.6 高 0.85）沿北墙 + 东墙
        Part(p, new Vector3(ox + 5.70f, 0.43f, -5.55f), new Vector3(4.2f, 0.86f, 0.6f), white, 1f);
        Part(p, new Vector3(ox + 5.70f, 0.88f, -5.55f), new Vector3(4.25f, 0.06f, 0.64f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox + 7.55f, 0.43f, -3.0f), new Vector3(0.6f, 0.86f, 4.4f), white, 1f);
        Part(p, new Vector3(ox + 7.55f, 0.88f, -3.0f), new Vector3(0.64f, 0.06f, 4.45f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox + 5.0f, 0.90f, -5.55f), new Vector3(0.7f, 0.04f, 0.5f), C("#8d949c"), 1f);   // 水槽
        Part(p, new Vector3(ox + 6.8f, 0.90f, -5.55f), new Vector3(0.8f, 0.04f, 0.5f), C("#3a3f47"), 1f);   // 灶台
        Part(p, new Vector3(ox + 3.9f, 0.95f, -4.2f), new Vector3(0.8f, 1.9f, 0.7f), metal, 1f);            // 冰箱靠西墙
        Part(p, new Vector3(ox + 5.75f, 2.10f, -5.70f), new Vector3(4.0f, 0.7f, 0.35f), white, 1f);         // 吊柜
        Lamp(p, new Vector3(ox + 5.75f, 2.85f, -3.2f), 8f, 1.15f);

        // 客卫 2.6×4.4
        Part(p, new Vector3(ox - 7.3f, 0.36f, 2.4f), new Vector3(0.42f, 0.72f, 0.7f), white, 1f);           // 马桶
        Part(p, new Vector3(ox - 7.45f, 0.78f, 2.1f), new Vector3(0.25f, 0.5f, 0.2f), white, 1f);
        Part(p, new Vector3(ox - 6.2f, 0.42f, 2.15f), new Vector3(1.0f, 0.84f, 0.5f), white, 1f);           // 洗手台
        Part(p, new Vector3(ox - 6.2f, 0.86f, 2.15f), new Vector3(1.05f, 0.06f, 0.55f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox - 6.2f, 1.70f, 1.72f), new Vector3(0.8f, 0.7f, 0.05f), C("#b8c6cc"), 1f);    // 镜子
        Rug(p, new Vector3(ox - 7.3f, 0.06f, 5.0f), 1.1f, 1.1f, C("#8d9aa6"));                              // 淋浴区
        Lamp(p, new Vector3(ox - 6.6f, 2.85f, 3.2f), 5f, 0.9f);

        // 储藏 3.8×4.4：三组架子
        for (int i = 0; i < 3; i++)
            Part(p, new Vector3(ox - 5.05f, 0.90f, 2.6f + i * 1.3f), new Vector3(0.4f, 1.8f, 1.1f), woodDark, 1f);
        Lamp(p, new Vector3(ox - 3.4f, 2.85f, 3.6f), 5f, 0.85f);

        // 家务间 5.0×4.4：洗衣机 + 洗衣池 + 后门
        Part(p, new Vector3(ox - 0.9f, 0.43f, 2.3f), new Vector3(0.6f, 0.86f, 0.6f), white, 1f);           // 洗衣机
        Part(p, new Vector3(ox - 0.9f, 0.88f, 2.3f), new Vector3(0.62f, 0.05f, 0.62f), C("#c9ced3"), 1f);
        Part(p, new Vector3(ox + 0.4f, 0.43f, 2.3f), new Vector3(0.6f, 0.86f, 0.6f), white, 1f);           // 洗衣池柜
        Lamp(p, new Vector3(ox + 0.9f, 2.85f, 4.2f), 6f, 0.9f);

        // ── 二层窗 ──
        Window(p, new Vector3(ox - 5.6f, y1 + 1.75f, -5.83f), 2.0f, 1.4f, 0f);
        Window(p, new Vector3(ox - 4.6f, y1 + 1.75f, 5.83f), 2.0f, 1.4f, 180f);
        Window(p, new Vector3(ox + 5.4f, y1 + 1.75f, -5.83f), 1.4f, 1.4f, 0f);
        Window(p, new Vector3(ox + 1.6f, y1 + 1.75f, -5.83f), 1.4f, 1.4f, 0f);

        // ════════ 二层家具 ════════
        // 主卧 6.4×6.0：1.8×2.0 双人床靠北墙，两侧各留 0.6 通行
        Part(p, new Vector3(ox - 3.0f, y1 + 0.22f, -1.05f), new Vector3(1.8f, 0.44f, 2.0f), woodDark, 1f);   // 床架（靠北墙）
        Part(p, new Vector3(ox - 3.0f, y1 + 0.52f, -1.05f), new Vector3(1.72f, 0.24f, 1.92f), C("#e8e4da"), 1f); // 床垫
        Part(p, new Vector3(ox - 3.0f, y1 + 0.66f, -1.55f), new Vector3(1.6f, 0.12f, 0.8f), C("#cfd6dd"), 1f); // 被
        Part(p, new Vector3(ox - 3.0f, y1 + 0.68f, -0.40f), new Vector3(1.1f, 0.14f, 0.35f), C("#f2eee4"), 1f); // 枕
        Part(p, new Vector3(ox - 3.0f, y1 + 0.70f, -0.03f), new Vector3(1.8f, 0.9f, 0.1f), woodDark, 1f);    // 床头板
        Part(p, new Vector3(ox - 4.25f, y1 + 0.25f, -0.30f), new Vector3(0.45f, 0.5f, 0.45f), woodDark, 1f); // 床头柜
        Part(p, new Vector3(ox - 2.05f, y1 + 0.25f, -0.30f), new Vector3(0.45f, 0.5f, 0.45f), woodDark, 1f);
        Part(p, new Vector3(ox - 7.55f, y1 + 1.10f, -4.2f), new Vector3(0.6f, 2.2f, 2.4f), woodDark, 1f);   // 衣柜靠西墙
        Part(p, new Vector3(ox - 2.6f, y1 + 0.75f, -5.2f), new Vector3(1.2f, 0.06f, 0.5f), wood, 1f);       // 梳妆台
        Part(p, new Vector3(ox - 2.6f, y1 + 0.36f, -5.2f), new Vector3(1.1f, 0.72f, 0.45f), wood, 1f);
        Lamp(p, new Vector3(ox - 4.6f, y1 + 2.85f, -3.0f), 8f, 1.15f);

        // 起居厅 5.0×6.0：小沙发 + 茶几 + 书架
        Part(p, new Vector3(ox + 1.6f, y1 + 0.42f, -4.9f), new Vector3(1.6f, 0.44f, 0.9f), fabric, 1.2f);
        Part(p, new Vector3(ox + 1.6f, y1 + 0.62f, -5.35f), new Vector3(1.6f, 0.85f, 0.18f), fabric, 1.2f);
        Part(p, new Vector3(ox + 1.6f, y1 + 0.21f, -3.6f), new Vector3(0.9f, 0.42f, 0.5f), woodDark, 1f);
        Part(p, new Vector3(ox + 3.15f, y1 + 0.90f, -2.0f), new Vector3(0.35f, 1.8f, 1.6f), woodDark, 1f);  // 书架靠东墙
        Lamp(p, new Vector3(ox + 1.6f, y1 + 2.85f, -3.6f), 7f, 1.05f);

        // 次卧 4.6×6.0：1.2×2.0 单人床 + 书桌 + 衣柜
        Part(p, new Vector3(ox + 4.6f, y1 + 0.22f, -1.05f), new Vector3(1.2f, 0.44f, 2.0f), woodDark, 1f);
        Part(p, new Vector3(ox + 4.6f, y1 + 0.52f, -1.05f), new Vector3(1.14f, 0.24f, 1.92f), C("#e8e4da"), 1f);
        Part(p, new Vector3(ox + 4.6f, y1 + 0.70f, -0.03f), new Vector3(1.2f, 0.85f, 0.1f), woodDark, 1f);
        Part(p, new Vector3(ox + 6.85f, y1 + 0.75f, -5.2f), new Vector3(1.2f, 0.06f, 0.5f), wood, 1f);      // 书桌
        Part(p, new Vector3(ox + 6.85f, y1 + 0.36f, -5.2f), new Vector3(1.1f, 0.72f, 0.45f), wood, 1f);
        Chair(p, it, new Vector3(ox + 6.85f, y1, -4.45f), 0f, wood, F1, true);
        Part(p, new Vector3(ox + 7.5f, y1 + 1.05f, -0.9f), new Vector3(0.6f, 2.1f, 1.4f), woodDark, 1f);    // 衣柜
        Lamp(p, new Vector3(ox + 5.8f, y1 + 2.85f, -3.2f), 7f, 1.05f);

        // 书房 6.4×4.4：书桌 + 书架 + 单人沙发
        Part(p, new Vector3(ox - 5.0f, y1 + 0.75f, 2.3f), new Vector3(1.6f, 0.06f, 0.7f), wood, 1f);
        Part(p, new Vector3(ox - 5.0f, y1 + 0.36f, 2.3f), new Vector3(1.5f, 0.72f, 0.6f), wood, 1f);
        Chair(p, it, new Vector3(ox - 5.0f, y1, 3.15f), 180f, wood, F1, true);
        Part(p, new Vector3(ox - 7.55f, y1 + 0.95f, 4.4f), new Vector3(0.35f, 1.9f, 2.2f), woodDark, 1f);    // 书架靠西墙
        Part(p, new Vector3(ox - 2.6f, y1 + 0.42f, 4.9f), new Vector3(0.9f, 0.44f, 0.9f), fabric, 1.2f);    // 单人沙发
        Part(p, new Vector3(ox - 2.6f, y1 + 0.62f, 5.32f), new Vector3(0.9f, 0.85f, 0.18f), fabric, 1.2f);
        Lamp(p, new Vector3(ox - 4.6f, y1 + 2.85f, 4.2f), 8f, 1.1f);

        // 卫生间 3.0×4.4：1.7 浴缸 + 洗手台 + 马桶
        Part(p, new Vector3(ox + 0.1f, y1 + 0.30f, 4.6f), new Vector3(1.7f, 0.6f, 0.8f), white, 1f);
        Part(p, new Vector3(ox + 0.1f, y1 + 0.52f, 4.6f), new Vector3(1.55f, 0.2f, 0.66f), C("#cfe3e8"), 1f);
        Part(p, new Vector3(ox - 0.9f, y1 + 0.42f, 2.2f), new Vector3(1.0f, 0.84f, 0.5f), white, 1f);
        Part(p, new Vector3(ox - 0.9f, y1 + 0.86f, 2.2f), new Vector3(1.05f, 0.06f, 0.55f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox - 0.9f, y1 + 1.70f, 1.78f), new Vector3(0.8f, 0.7f, 0.05f), C("#b8c6cc"), 1f);
        Part(p, new Vector3(ox + 0.95f, y1 + 0.36f, 2.2f), new Vector3(0.42f, 0.72f, 0.7f), white, 1f);
        Lamp(p, new Vector3(ox + 0.1f, y1 + 2.85f, 3.6f), 6f, 1.0f);

        // 衣帽间 2.0×4.4
        Part(p, new Vector3(ox + 2.85f, y1 + 1.05f, 3.6f), new Vector3(0.55f, 2.1f, 3.4f), woodDark, 1f);
        Lamp(p, new Vector3(ox + 2.4f, y1 + 2.85f, 3.6f), 5f, 0.85f);

        // ── 家具碰撞 ──
        Reg(it, ox - 7.35f, -3.0f, 0.95f, 2.6f, F0);    // 沙发
        Reg(it, ox - 5.95f, -3.0f, 1.2f, 0.6f, F0);     // 茶几
        Reg(it, ox - 2.15f, -3.0f, 0.5f, 2.0f, F0);     // 电视柜
        Reg(it, ox - 6.4f, -0.28f, 2.0f, 0.35f, F0);    // 书架
        Reg(it, ox + 3.05f, -4.9f, 0.35f, 1.6f, F0);    // 鞋柜
        Reg(it, ox + 2.4f, -1.6f, 1.6f, 0.9f, F0);      // 餐桌
        Reg(it, ox + 5.70f, -5.55f, 4.2f, 0.6f, F0);    // 北台面
        Reg(it, ox + 7.55f, -3.0f, 0.6f, 4.4f, F0);     // 东台面
        Reg(it, ox + 3.9f, -4.2f, 0.8f, 0.7f, F0);      // 冰箱
        Reg(it, ox - 7.3f, 2.4f, 0.42f, 0.7f, F0);      // 马桶
        Reg(it, ox - 6.2f, 2.15f, 1.0f, 0.5f, F0);      // 洗手台
        Reg(it, ox - 5.05f, 2.6f, 0.4f, 1.1f, F0);
        Reg(it, ox - 5.05f, 3.9f, 0.4f, 1.1f, F0);
        Reg(it, ox - 5.05f, 5.2f, 0.4f, 1.1f, F0);
        Reg(it, ox - 0.9f, 2.3f, 0.6f, 0.6f, F0);       // 洗衣机
        Reg(it, ox + 0.4f, 2.3f, 0.6f, 0.6f, F0);
        Reg(it, ox - 3.0f, -1.05f, 1.8f, 2.0f, F1);     // 床
        Reg(it, ox - 4.25f, -0.30f, 0.45f, 0.45f, F1);
        Reg(it, ox - 2.05f, -0.30f, 0.45f, 0.45f, F1);
        Reg(it, ox - 7.55f, -4.2f, 0.6f, 2.4f, F1);     // 衣柜
        Reg(it, ox - 2.6f, -5.2f, 1.2f, 0.5f, F1);      // 梳妆台
        Reg(it, ox + 1.6f, -4.9f, 1.6f, 0.9f, F1);      // 起居厅沙发
        Reg(it, ox + 1.6f, -3.6f, 0.9f, 0.5f, F1);
        Reg(it, ox + 3.15f, -2.0f, 0.35f, 1.6f, F1);    // 起居厅书架
        Reg(it, ox + 4.6f, -1.05f, 1.2f, 2.0f, F1);     // 单人床
        Reg(it, ox + 6.85f, -5.2f, 1.2f, 0.5f, F1);     // 书桌
        Reg(it, ox + 7.5f, -0.9f, 0.6f, 1.4f, F1);      // 衣柜
        Reg(it, ox - 5.0f, 2.3f, 1.6f, 0.7f, F1);       // 书房桌
        Reg(it, ox - 7.55f, 4.4f, 0.35f, 2.2f, F1);     // 书架
        Reg(it, ox - 2.6f, 4.9f, 0.9f, 0.9f, F1);       // 单人沙发
        Reg(it, ox + 0.1f, 4.6f, 1.7f, 0.8f, F1);       // 浴缸
        Reg(it, ox - 0.9f, 2.2f, 1.0f, 0.5f, F1);       // 洗手台
        Reg(it, ox + 0.95f, 2.2f, 0.42f, 0.7f, F1);     // 马桶
        Reg(it, ox + 2.85f, 3.6f, 0.55f, 3.4f, F1);     // 衣帽间柜

        return it;
    }

    // ── 教学楼：34×16 两层。教室按国标 9.0(开间)×7.2(进深)，
    //    走廊净宽 3.0（规范 ≥2.4），课桌成对、排距 0.9、纵向走道 0.55 ──
    static Interior School(Transform parent)
    {
        float ox = SCHOOL_X;
        var it = new Interior
        {
            Id = "school",
            Name = "教学楼",
            X1 = ox - 17f, X2 = ox + 17f, Z1 = -8f, Z2 = 8f,
            Spawn = new Vector2(ox, 5.2f),
            Exit = new Vector2(ox, 7.2f),
            FloorY = new[] { 0.02f, 4.02f },
        };
        var root = new GameObject("Interior_school");
        root.transform.SetParent(parent, false);
        it.Root = root;
        var p = root.transform;

        var wall = T("in_wall");
        var floor = T("in_tile");
        var ceil = T("in_ceil");
        var wood = T("wood");
        var board = T("in_board");
        var woodDark = C("#6f5540");
        var deskTop = C("#c9a878");
        var green = C("#4a6a52");

        const float W = 3.8f;
        const float top = 7.82f;       // 二层天花顶面 = 4.02 + 3.8
        float y1 = it.FloorY[1];
        float h0 = W - 0.02f;
        float h1 = W - 0.02f;

        // 教室开间 9.0：x -16.9..-7.9 / -7.7..1.3 / 1.5..10.5；东端 10.7..16.9 多功能教室
        // 走廊 z -0.6..2.4（净 3.0）
        const float ZC0 = -0.6f, ZC1 = 2.4f;

        // ── 地面 ──
        Part(p, new Vector3(ox, 0f, 0f), new Vector3(34f, 0.04f, 16f), floor, 2f, default, true);

        // ── 外墙 ──
        WallX(p, it, ox - 17f, ox + 17f, -8f, 0f, top, 0f, 0f, 2.7f, wall, BOTH, WALL_OUT);
        WallX(p, it, ox - 17f, ox + 17f, 8f, 0f, top, ox - 2.4f, ox + 2.4f, 2.7f, wall, BOTH, WALL_OUT);
        WallZ(p, it, -8f, 8f, ox - 17f, 0f, top, 0f, 0f, 2.7f, wall, BOTH, WALL_OUT);
        WallZ(p, it, -8f, 8f, ox + 17f, 0f, top, 0f, 0f, 2.7f, wall, BOTH, WALL_OUT);

        // ── 二层楼板（留楼梯洞 x -9.6..-4.2 / z 4.0..6.0）──
        Slab(p, ox - 17f, -8f, ox + 17f, 2.6f, y1, ceil, floor);
        Slab(p, ox - 17f, 2.6f, ox - 16.4f, 7.0f, y1, ceil, floor);
        Slab(p, ox - 13.8f, 2.6f, ox + 17f, 7.0f, y1, ceil, floor);
        Slab(p, ox - 17f, 7.0f, ox + 17f, 8f, y1, ceil, floor);
        Slab(p, ox - 17f, -8f, ox + 17f, 8f, top, ceil);

        // ── 走廊北墙（z=-0.6）：教室门 ──
        WallX(p, it, ox - 16.9f, ox - 12.2f, ZC0, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);
        WallX(p, it, ox - 10.8f, ox - 7.7f, ZC0, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);
        WallX(p, it, ox - 7.7f, ox - 1.2f, ZC0, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);   // B 西段
        WallX(p, it, ox + 0.0f, ox + 1.3f, ZC0, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);   // B 东段（门 -1.2..0.0）
        WallX(p, it, ox + 2.4f, ox + 10.5f, ZC0, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);
        WallX(p, it, ox + 11.6f, ox + 16.9f, ZC0, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);
        // 教室隔墙（贴到北外墙）
        WallZ(p, it, -7.9f, ZC0, ox - 7.8f, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);
        WallZ(p, it, -7.9f, ZC0, ox + 1.4f, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);
        WallZ(p, it, -7.9f, ZC0, ox + 10.6f, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);

        // ── 走廊南墙（z=2.4）：楼梯间(西端) / 多功能教室 / 门厅 / 办公室 / 图书角 ──
        WallX(p, it, ox - 16.9f, ox - 16.4f, ZC1, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);
        WallX(p, it, ox - 15.0f, ox - 9.0f, ZC1, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);   // 楼梯间门 -16.4..-15.0
        WallX(p, it, ox - 7.8f, ox - 3.4f, ZC1, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);    // 多功能教室门 -9.0..-7.8
        WallX(p, it, ox - 3.4f, ox + 3.4f, ZC1, 0.02f, h0, ox - 3.0f, ox + 3.0f, 2.6f, wall, F0);  // 门厅敞开
        WallX(p, it, ox + 3.4f, ox + 9.4f, ZC1, 0.02f, h0, ox + 5.6f, ox + 6.8f, 2.3f, wall, F0);  // 办公室门
        WallX(p, it, ox + 9.4f, ox + 16.9f, ZC1, 0.02f, h0, ox + 12.0f, ox + 13.2f, 2.3f, wall, F0);
        // 南侧竖墙
        WallZ(p, it, ZC1, 8f, ox - 12.0f, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);   // 楼梯间 / 多功能教室
        WallZ(p, it, ZC1, 8f, ox - 3.4f, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);
        WallZ(p, it, ZC1, 8f, ox + 3.4f, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);
        WallZ(p, it, ZC1, 8f, ox + 9.4f, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);

        // ── 楼梯：西端楼梯间，x -16.4..-13.8（宽 2.6），z 2.6(低) → 7.0(高) ──
        //    进门就在楼梯脚下，不再跟任何教室门隔着走廊对脸
        Stairs(p, it, ox - 16.4f, 2.6f, ox - 13.8f, 7.0f, 0, 1, false, woodDark);
        WallZ(p, it, 2.6f, 7.0f, ox - 13.8f, 0.02f, h0, 0f, 0f, 2.3f, wall, F0);   // 楼梯东侧挡墙
        Lamp(p, new Vector3(ox - 15.1f, 3.45f, 4.6f), 8f, 1.15f);

        // ── 二层：走廊 + 教室 ──
        WallX(p, it, ox - 16.9f, ox - 12.2f, ZC0, y1, h1, 0f, 0f, 2.3f, wall, F1);
        WallX(p, it, ox - 10.8f, ox - 7.7f, ZC0, y1, h1, 0f, 0f, 2.3f, wall, F1);
        WallX(p, it, ox - 7.7f, ox - 1.2f, ZC0, y1, h1, 0f, 0f, 2.3f, wall, F1);
        WallX(p, it, ox + 0.0f, ox + 1.3f, ZC0, y1, h1, 0f, 0f, 2.3f, wall, F1);
        WallX(p, it, ox + 2.4f, ox + 10.5f, ZC0, y1, h1, 0f, 0f, 2.3f, wall, F1);
        WallX(p, it, ox + 11.6f, ox + 16.9f, ZC0, y1, h1, 0f, 0f, 2.3f, wall, F1);
        WallZ(p, it, -7.9f, ZC0, ox - 7.8f, y1, h1, 0f, 0f, 2.3f, wall, F1);
        WallZ(p, it, -7.9f, ZC0, ox + 1.4f, y1, h1, 0f, 0f, 2.3f, wall, F1);
        WallZ(p, it, -7.9f, ZC0, ox + 10.6f, y1, h1, 0f, 0f, 2.3f, wall, F1);
        // 二层南墙（楼梯口以东）
        WallX(p, it, ox - 12.0f, ox + 16.9f, ZC1, y1, h1, 0f, 0f, 2.3f, wall, F1);

        // ── 门厅：值班台 + 公告栏 ──
        Part(p, new Vector3(ox + 1.2f, 0.54f, 6.6f), new Vector3(2.4f, 1.08f, 0.6f), woodDark, 1f);
        Part(p, new Vector3(ox + 1.2f, 1.10f, 6.6f), new Vector3(2.5f, 0.06f, 0.66f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox - 2.2f, 1.55f, 7.78f), new Vector3(2.6f, 1.3f, 0.08f), green, 1f);
        Lamp(p, new Vector3(ox, 3.35f, 5.4f), 10f, 1.3f);

        // ── 办公室 ──
        Table(p, new Vector3(ox + 6.6f, 0f, 6.4f), 1.8f, 0.9f, 0.76f, wood);
        Chair(p, it, new Vector3(ox + 6.6f, 0f, 7.1f), 180f, wood, F0, true);
        Part(p, new Vector3(ox + 9.1f, 0.95f, 4.4f), new Vector3(0.5f, 1.9f, 1.4f), woodDark, 1f);
        Lamp(p, new Vector3(ox + 6.6f, 3.35f, 5.6f), 8f, 1.0f);

        // ── 图书角 ──
        for (int i = 0; i < 3; i++)
            Part(p, new Vector3(ox + 12.6f + i * 1.4f, 0.95f, 5.6f), new Vector3(1.0f, 1.9f, 0.45f), woodDark, 1f);
        Table(p, new Vector3(ox + 13.6f, 0f, 3.4f), 1.6f, 0.9f, 0.76f, wood);
        Lamp(p, new Vector3(ox + 13.2f, 3.35f, 5.2f), 9f, 1.05f);

        // ── 六间教室（三层教室 + 东端多功能）──
        Classroom(p, it, ox - 12.4f, -4.3f, 9.0f, 7.3f, 0.02f, F0, board, deskTop, woodDark, green);
        Classroom(p, it, ox - 3.2f, -4.3f, 9.0f, 7.3f, 0.02f, F0, board, deskTop, woodDark, green);
        Classroom(p, it, ox + 6.0f, -4.3f, 9.0f, 7.3f, 0.02f, F0, board, deskTop, woodDark, green);
        Classroom(p, it, ox + 13.8f, -4.3f, 6.2f, 7.3f, 0.02f, F0, board, deskTop, woodDark, green);
        Classroom(p, it, ox - 12.4f, -4.3f, 9.0f, 7.3f, y1, F1, board, deskTop, woodDark, green);
        Classroom(p, it, ox - 3.2f, -4.3f, 9.0f, 7.3f, y1, F1, board, deskTop, woodDark, green);
        Classroom(p, it, ox + 6.0f, -4.3f, 9.0f, 7.3f, y1, F1, board, deskTop, woodDark, green);
        Classroom(p, it, ox + 13.8f, -4.3f, 6.2f, 7.3f, y1, F1, board, deskTop, woodDark, green);

        // ── 走廊灯 ──
        for (int i = -2; i <= 2; i++)
        {
            Lamp(p, new Vector3(ox + i * 6.4f, 3.35f, 0.9f), 10f, 1.15f);
            Lamp(p, new Vector3(ox + i * 6.4f, y1 + 3.35f, 0.9f), 10f, 1.15f);
        }

        // ── 家具碰撞 ──
        Reg(it, ox + 1.2f, 6.6f, 2.4f, 0.6f, F0);
        Reg(it, ox + 6.6f, 6.4f, 1.8f, 0.9f, F0);
        Reg(it, ox + 9.1f, 4.4f, 0.5f, 1.4f, F0);
        for (int i = 0; i < 3; i++) Reg(it, ox + 12.6f + i * 1.4f, 5.6f, 1.0f, 0.45f, F0);
        Reg(it, ox + 13.6f, 3.4f, 1.6f, 0.9f, F0);

        return it;
    }

    // 一间教室：国标 9.0×7.2 —— 黑板宽 4.0 高 1.2（下沿离地 0.9），
    // 第一排课桌前缘距黑板 2.2m，排距 0.9m，课桌成对（1.2m 一组），组间走道 0.55m
    static void Classroom(Transform p, Interior it, float cx, float cz, float w, float d, float fy,
                          int mask, Material board, Material desk, Material wood, Material green)
    {
        float zBack = cz - d * 0.5f;    // 黑板墙内表面
        float zFront = cz + d * 0.5f;   // 走廊侧内表面

        // 黑板（宽 4.0 × 高 1.2，下沿离地 0.9）+ 讲台
        Part(p, new Vector3(cx, fy + 1.50f, zBack + 0.06f), new Vector3(4.0f, 1.2f, 0.07f), board, 4.0f);
        Part(p, new Vector3(cx, fy + 0.40f, zBack + 0.75f), new Vector3(1.6f, 0.80f, 0.7f), wood, 1f);
        Part(p, new Vector3(cx, fy + 0.82f, zBack + 0.75f), new Vector3(1.7f, 0.05f, 0.76f), C("#d9d3c6"), 1f);

        // 课桌：3 组 × 2 排 × 2 人 = 12 座。教室进深 7.3，排满 4 排会像考场，后场留空
        //   椅子 ry=180：靠背翻到远离桌子一侧，人面朝黑板（原来 ry=0 靠背朝黑板，坐反了）
        const int groups = 3, rows = 2;
        for (int r = 0; r < rows; r++)
            for (int g = 0; g < groups; g++)
            {
                float gx = cx + (g - 1) * 2.4f;
                float z = zBack + 2.2f + 0.2f + r * 1.2f;
                for (int k = 0; k < 2; k++)
                {
                    float x = gx + (k == 0 ? -0.3f : 0.3f);
                    Part(p, new Vector3(x, fy + 0.72f, z), new Vector3(0.6f, 0.05f, 0.4f), desk, 1.0f);
                    for (int lg = 0; lg < 4; lg++)
                        Part(p, new Vector3(x + (lg % 2 == 0 ? -0.26f : 0.26f), fy + 0.36f,
                                            z + (lg < 2 ? -0.16f : 0.16f)),
                             new Vector3(0.04f, 0.72f, 0.04f), C("#8d8d94"), 0.5f);
                    Chair(p, it, new Vector3(x, fy, z + 0.55f), 180f, wood, mask, false);
                }
                // 一组两张桌合并成一个碰撞盒（含椅）
                it.Solids.Add(new Solid(gx - 0.62f, z - 0.2f, gx + 0.62f, z + 0.95f, mask));
            }


        // 侧墙板报
        Part(p, new Vector3(cx - w * 0.5f + 0.08f, fy + 1.65f, cz + 1.6f), new Vector3(0.05f, 1.2f, 3.6f), green, 3.6f);

        // 教室灯（两盏，挂在课桌区上方）
        Lamp(p, new Vector3(cx - 2.2f, fy + 3.35f, cz), 8f, 0.55f);
        Lamp(p, new Vector3(cx + 2.2f, fy + 3.35f, cz), 8f, 0.55f);
    }
}
