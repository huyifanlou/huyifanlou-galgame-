using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────
// 室内场景：家 / 教学楼
//   全部程序化生成，建在世界之外的空地上（x ≈ 900 起），与室外共用贴图库与材质库。
//   进入时把玩家瞬移到室内落点，碰撞/地面高度改走本文件的 AABB 与楼层函数；
//   出口在门口，走近按 F 回到室外原位置。
//   分层：Solids 带楼层掩码（bit0=一层 bit1=二层），楼梯用高度插值，不做真物理。
// ─────────────────────────────────────────────────────────────
public class Interior
{
    public string Id;
    public string Name;
    public float X1, Z1, X2, Z2;      // 平面范围（世界坐标）
    public Vector2 Spawn;             // 室内落点
    public Vector2 Exit;              // 门口
    public float ExitRadius = 1.5f;
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

    public Solid(float x1, float z1, float x2, float z2, int mask)
    {
        X1 = Mathf.Min(x1, x2); X2 = Mathf.Max(x1, x2);
        Z1 = Mathf.Min(z1, z2); Z2 = Mathf.Max(z1, z2);
        Mask = mask;
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
    const float HOME_X = 900f, SCHOOL_X = 1040f;
    const float SLAB = 0.18f;

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
                                int mask, float texMeters = 1f)
    {
        var go = Part(p, pos, size, mat, texMeters, default, true);
        it.Solids.Add(new Solid(pos.x - size.x * 0.5f, pos.z - size.z * 0.5f,
                                pos.x + size.x * 0.5f, pos.z + size.z * 0.5f, mask));
        return go;
    }

    // 轴向墙：两端必须共线
    static void Wall(Transform p, Interior it, float x1, float z1, float x2, float z2,
                     float y0, float h, Material mat, int mask, float thick = 0.2f)
    {
        if (Mathf.Abs(x1 - x2) < 0.01f && Mathf.Abs(z1 - z2) < 0.01f) return;
        float w = Mathf.Max(Mathf.Abs(x2 - x1), thick);
        float d = Mathf.Max(Mathf.Abs(z2 - z1), thick);
        SolidPart(p, it, new Vector3((x1 + x2) * 0.5f, y0 + h * 0.5f, (z1 + z2) * 0.5f),
                  new Vector3(w, h, d), mat, mask, 1.7f);
    }

    // 沿 X 的墙，中间留门洞（门楣不挡人）
    static void WallX(Transform p, Interior it, float x1, float x2, float z, float y0, float h,
                      float doorA, float doorB, float doorH, Material mat, int mask, float thick = 0.2f)
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
                      float doorA, float doorB, float doorH, Material mat, int mask, float thick = 0.2f)
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
            Part(p, new Vector3((x1 + x2) * 0.5f, topY + 0.005f - 0.025f, (z1 + z2) * 0.5f),
                 new Vector3(w, 0.05f, d), topMat, 2f);
    }

    // 楼梯：一级一级往上垒实心块（视觉上是踏步，走上去靠 Stair 高度插值）
    static void Stairs(Transform p, Interior it, float x1, float z1, float x2, float z2,
                       int from, int to, bool alongX, Material mat)
    {
        it.Stairs.Add(new Stair { X1 = x1, Z1 = z1, X2 = x2, Z2 = z2, From = from, To = to, AlongX = alongX });
        int n = 14;
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

    // 贴地薄片（地毯 / 地垫）
    static void Rug(Transform p, Vector3 pos, float w, float d, Material mat)
    {
        Part(p, pos, new Vector3(w, 0.02f, d), mat, 1f);
    }

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
        if (solid) it.Solids.Add(new Solid(seat.x - 0.3f, seat.z - 0.3f, seat.x + 0.3f, seat.z + 0.3f, mask));
    }

    static void Table(Transform p, Vector3 top, float w, float d, float h, Material wood)
    {
        Part(p, new Vector3(top.x, top.y + h - 0.04f, top.z), new Vector3(w, 0.08f, d), wood, 1.1f);
        for (int i = 0; i < 4; i++)
            Part(p, new Vector3(top.x + (i % 2 == 0 ? -w * 0.5f + 0.12f : w * 0.5f - 0.12f),
                                top.y + (h - 0.08f) * 0.5f,
                                top.z + (i < 2 ? -d * 0.5f + 0.12f : d * 0.5f - 0.12f)),
                 new Vector3(0.08f, h - 0.08f, 0.08f), wood, 0.7f);
    }

    // ═════════════════════════════════════════════════════
    //  入口
    // ═════════════════════════════════════════════════════
    public static List<Interior> Build(Transform parent)
    {
        _cache.Clear();
        var list = new List<Interior> { Home(parent), School(parent) };
        return list;
    }

    // ── 家：12×10 两层小别墅 ───────────────────────────────
    static Interior Home(Transform parent)
    {
        float ox = HOME_X;
        var it = new Interior
        {
            Id = "home",
            Name = "家",
            X1 = ox - 6f, X2 = ox + 6f, Z1 = -5f, Z2 = 5f,
            Spawn = new Vector2(ox, -2.4f),
            Exit = new Vector2(ox, -4.4f),
            FloorY = new[] { 0.02f, 3.02f },
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
        var fabric2 = C("#b9a98c");
        var white = C("#e6e6e2");
        var metal = C("#9aa0a6");
        var screen = C("#1a1d22");

        const float W = 3.0f;      // 净高
        float top = 6.02f;         // 二层天花顶面 = 3.02 + 3.0

        // ── 地面 ──
        Part(p, new Vector3(ox, 0f, 0f), new Vector3(12f, 0.04f, 10f), floor, 2f, default, true);
        Part(p, new Vector3(ox + 3.2f, 0.045f, 2.6f), new Vector3(5.2f, 0.02f, 4.4f), tile, 1.4f);

        // ── 外墙（两层通高，南墙中间留门）──
        WallX(p, it, ox - 6f, ox + 6f, -5f, 0f, top, ox - 0.95f, ox + 0.95f, 2.2f, wall, BOTH);
        WallX(p, it, ox - 6f, ox + 6f, 5f, 0f, top, 0f, 0f, 2.2f, wall, BOTH);
        WallZ(p, it, -5f, 5f, ox - 6f, 0f, top, 0f, 0f, 2.2f, wall, BOTH);
        WallZ(p, it, -5f, 5f, ox + 6f, 0f, top, 0f, 0f, 2.2f, wall, BOTH);

        // ── 二层楼板（留楼梯洞 x 4.2..5.8 / z -2.2..2.0）──
        float slabTop = it.FloorY[1];
        Slab(p, ox - 6f, -5f, ox + 6f, -2.2f, slabTop, ceil, floor);
        Slab(p, ox - 6f, -2.2f, ox + 4.2f, 2.0f, slabTop, ceil, floor);
        Slab(p, ox + 5.8f, -2.2f, ox + 6f, 2.0f, slabTop, ceil, floor);
        Slab(p, ox - 6f, 2.0f, ox + 6f, 5f, slabTop, ceil, floor);
        // 二层天花
        Slab(p, ox - 6f, -5f, ox + 6f, 5f, top, ceil);

        // ── 一层隔墙：客厅 / 厨房 ──
        WallZ(p, it, -2.6f, 5f, ox + 0.5f, 0.02f, W - SLAB - 0.02f, 0.4f, 1.6f, 2.1f, wall, F0);

        // ── 楼梯：x 4.2..5.8，z 2.0(低) → -2.2(高) ──
        Stairs(p, it, ox + 4.2f, 2.0f, ox + 5.8f, -2.2f, 0, 1, false, woodDark);
        // 楼梯西侧挡墙 + 顶端封板（一层）
        WallZ(p, it, -2.2f, 2.0f, ox + 4.2f, 0.02f, W - SLAB - 0.02f, 0f, 0f, 2.1f, wall, F0);
        WallX(p, it, ox + 4.2f, ox + 5.8f, -2.2f, 0.02f, W - SLAB - 0.02f, 0f, 0f, 2.1f, wall, F0);
        Rail(p, new Vector3(ox + 4.25f, 1.05f, 2.0f), new Vector3(ox + 4.25f, 4.0f, -2.2f), woodDark);

        // ── 二层隔墙：起居厅 / 主卧 + 浴室 ──
        float y1 = it.FloorY[1];
        float h1 = W - 0.02f;
        WallX(p, it, ox - 6f, ox - 3.6f, 0.8f, y1, h1, 0f, 0f, 2.1f, wall, F1);
        WallX(p, it, ox - 2.4f, ox + 2.0f, 0.8f, y1, h1, 0f, 0f, 2.1f, wall, F1);
        WallX(p, it, ox + 3.2f, ox + 6f, 0.8f, y1, h1, 0f, 0f, 2.1f, wall, F1);
        WallZ(p, it, 0.8f, 5f, ox + 0.5f, y1, h1, 0f, 0f, 2.1f, wall, F1);

        // ── 客厅家具（西半）──
        Part(p, new Vector3(ox - 1.7f, 0.45f, -4.6f), new Vector3(0.9f, 0.9f, 0.4f), woodDark, 1f);       // 鞋柜
        Rug(p, new Vector3(ox - 4.2f, 0.05f, 1.0f), 4.0f, 3.0f, fabric2);
        Part(p, new Vector3(ox - 5.3f, 0.40f, 1.0f), new Vector3(1.0f, 0.72f, 2.6f), fabric, 1.2f);        // 沙发座
        Part(p, new Vector3(ox - 5.72f, 0.72f, 1.0f), new Vector3(0.22f, 0.62f, 2.6f), fabric, 1.2f);      // 靠背
        Part(p, new Vector3(ox - 5.35f, 0.62f, -0.34f), new Vector3(0.95f, 0.5f, 0.2f), fabric, 1.2f);     // 扶手
        Part(p, new Vector3(ox - 5.35f, 0.62f, 2.34f), new Vector3(0.95f, 0.5f, 0.2f), fabric, 1.2f);
        Table(p, new Vector3(ox - 3.9f, 0f, 1.0f), 1.4f, 0.8f, 0.42f, woodDark);                            // 茶几
        Part(p, new Vector3(ox - 4.2f, 0.30f, 4.52f), new Vector3(2.6f, 0.6f, 0.45f), woodDark, 1f);        // 电视柜
        Part(p, new Vector3(ox - 4.2f, 1.15f, 4.72f), new Vector3(1.5f, 0.85f, 0.06f), screen, 1f);         // 电视
        Part(p, new Vector3(ox - 5.55f, 0.92f, -2.6f), new Vector3(0.4f, 1.8f, 1.8f), woodDark, 1f);        // 书架
        for (int i = 0; i < 3; i++)
            Part(p, new Vector3(ox - 5.55f, 0.35f + i * 0.55f, -2.6f), new Vector3(0.46f, 0.04f, 1.7f), C("#4a3b2c"), 1f);
        Lamp(p, new Vector3(ox - 5.3f, 2.55f, 1.0f), 7.5f, 1.15f);
        Lamp(p, new Vector3(ox - 1.2f, 2.55f, -3.6f), 6.5f, 0.85f);

        // ── 厨房 / 餐厅（东半）──
        Part(p, new Vector3(ox + 3.1f, 0.45f, 4.62f), new Vector3(5.0f, 0.9f, 0.56f), white, 1f);           // 北侧台面柜
        Part(p, new Vector3(ox + 3.1f, 0.92f, 4.62f), new Vector3(5.05f, 0.06f, 0.62f), C("#d9d3c6"), 1f);  // 台面板
        Part(p, new Vector3(ox + 5.62f, 0.45f, 1.0f), new Vector3(0.56f, 0.9f, 2.4f), white, 1f);           // 东侧台面柜
        Part(p, new Vector3(ox + 5.62f, 0.92f, 1.0f), new Vector3(0.62f, 0.06f, 2.45f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox + 1.25f, 0.95f, 3.9f), new Vector3(0.78f, 1.9f, 0.72f), metal, 1f);          // 冰箱
        Part(p, new Vector3(ox + 1.25f, 1.55f, 3.55f), new Vector3(0.04f, 0.5f, 0.04f), C("#5c636d"), 1f);  // 冰箱把手
        Table(p, new Vector3(ox + 2.9f, 0f, 1.0f), 1.7f, 1.0f, 0.74f, wood);                                // 餐桌
        Chair(p, it, new Vector3(ox + 2.9f, 0f, 1.85f), 180f, wood, F0, true);
        Chair(p, it, new Vector3(ox + 2.9f, 0f, 0.15f), 0f, wood, F0, true);
        Chair(p, it, new Vector3(ox + 1.75f, 0f, 1.0f), 90f, wood, F0, true);
        Chair(p, it, new Vector3(ox + 4.05f, 0f, 1.0f), -90f, wood, F0, true);
        Lamp(p, new Vector3(ox + 2.9f, 2.35f, 1.0f), 7f, 1.1f);

        // ── 二层：主卧 ──
        float b = y1;
        Part(p, new Vector3(ox - 3.4f, b + 0.20f, 3.1f), new Vector3(2.0f, 0.40f, 2.2f), woodDark, 1f);      // 床架
        Part(p, new Vector3(ox - 3.4f, b + 0.50f, 3.1f), new Vector3(1.9f, 0.24f, 2.1f), C("#e8e4da"), 1f);  // 床垫
        Part(p, new Vector3(ox - 3.4f, b + 0.66f, 3.9f), new Vector3(1.7f, 0.14f, 0.5f), C("#cfd6dd"), 1f);  // 被子
        Part(p, new Vector3(ox - 3.4f, b + 0.70f, 2.25f), new Vector3(0.7f, 0.16f, 0.4f), C("#f2eee4"), 1f); // 枕
        Part(p, new Vector3(ox - 3.4f, b + 0.72f, 4.28f), new Vector3(2.0f, 0.9f, 0.12f), woodDark, 1f);     // 床头板
        Part(p, new Vector3(ox - 4.75f, b + 0.28f, 4.1f), new Vector3(0.5f, 0.56f, 0.45f), woodDark, 1f);    // 床头柜
        Part(p, new Vector3(ox - 5.55f, b + 1.05f, 2.0f), new Vector3(0.6f, 2.1f, 1.8f), woodDark, 1f);      // 衣柜
        Table(p, new Vector3(ox - 1.5f, b, 1.25f), 1.6f, 0.7f, 0.74f, wood);                                // 书桌
        Chair(p, it, new Vector3(ox - 1.5f, b, 2.05f), 180f, wood, F1, true);
        Part(p, new Vector3(ox - 1.5f, b + 0.80f, 1.35f), new Vector3(0.5f, 0.05f, 0.35f), C("#3a3f47"), 1f); // 书
        Lamp(p, new Vector3(ox - 3.2f, y1 + 2.5f, 3.0f), 7.5f, 1.15f);

        // ── 二层：浴室 ──
        Part(p, new Vector3(ox + 4.5f, b + 0.30f, 3.9f), new Vector3(1.8f, 0.6f, 0.85f), white, 1f);         // 浴缸
        Part(p, new Vector3(ox + 4.5f, b + 0.52f, 3.9f), new Vector3(1.6f, 0.2f, 0.68f), C("#cfe3e8"), 1f);  // 水面
        Part(p, new Vector3(ox + 1.5f, b + 0.42f, 4.55f), new Vector3(1.2f, 0.84f, 0.5f), white, 1f);       // 洗手台
        Part(p, new Vector3(ox + 1.5f, b + 0.88f, 4.55f), new Vector3(1.3f, 0.06f, 0.56f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox + 1.5f, b + 1.75f, 4.92f), new Vector3(1.0f, 0.8f, 0.05f), C("#b8c6cc"), 1f); // 镜子
        Part(p, new Vector3(ox + 5.55f, b + 0.32f, 1.5f), new Vector3(0.42f, 0.64f, 0.6f), white, 1f);      // 马桶
        Part(p, new Vector3(ox + 5.62f, b + 0.75f, 1.28f), new Vector3(0.3f, 0.5f, 0.22f), white, 1f);
        Lamp(p, new Vector3(ox + 3.6f, y1 + 2.5f, 3.4f), 6.5f, 1.0f);

        return it;
    }

    // ── 教学楼：24×12 两层，走廊 + 四间教室 + 办公室 ────────
    static Interior School(Transform parent)
    {
        float ox = SCHOOL_X;
        var it = new Interior
        {
            Id = "school",
            Name = "教学楼",
            X1 = ox - 12f, X2 = ox + 12f, Z1 = -6f, Z2 = 6f,
            Spawn = new Vector2(ox, 3.4f),
            Exit = new Vector2(ox, 5.3f),
            FloorY = new[] { 0.02f, 3.82f },
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

        const float W = 3.6f;
        float top = 7.42f;          // 二层天花顶面 = 3.82 + 3.6

        // ── 地面 ──
        Part(p, new Vector3(ox, 0f, 0f), new Vector3(24f, 0.04f, 12f), floor, 2f, default, true);

        // ── 外墙（南墙留双开门）──
        WallX(p, it, ox - 12f, ox + 12f, -6f, 0f, top, 0f, 0f, 2.6f, wall, BOTH);
        WallX(p, it, ox - 12f, ox + 12f, 6f, 0f, top, ox - 1.8f, ox + 1.8f, 2.6f, wall, BOTH);
        WallZ(p, it, -6f, 6f, ox - 12f, 0f, top, 0f, 0f, 2.6f, wall, BOTH);
        WallZ(p, it, -6f, 6f, ox + 12f, 0f, top, 0f, 0f, 2.6f, wall, BOTH);

        // ── 二层楼板（留楼梯洞 x -4.6..-3.0 / z 1.8..6）──
        float slabTop = it.FloorY[1];
        Slab(p, ox - 12f, -6f, ox + 12f, 1.8f, slabTop, ceil, floor);
        Slab(p, ox - 12f, 1.8f, ox - 4.6f, 6f, slabTop, ceil, floor);
        Slab(p, ox - 3.0f, 1.8f, ox + 12f, 6f, slabTop, ceil, floor);
        Slab(p, ox - 12f, -6f, ox + 12f, 6f, top, ceil);

        float y1 = it.FloorY[1];
        float h1 = W - 0.04f;

        // ── 一层：走廊北墙（教室门）+ 门厅/办公室 ──
        // 走廊 z 0.4..2.6
        WallX(p, it, ox - 12f, ox - 9.0f, 0.4f, 0.02f, W - SLAB - 0.02f, 0f, 0f, 2.2f, wall, F0);
        WallX(p, it, ox - 7.8f, ox - 1.0f, 0.4f, 0.02f, W - SLAB - 0.02f, 0f, 0f, 2.2f, wall, F0);
        WallX(p, it, ox + 1.0f, ox + 7.8f, 0.4f, 0.02f, W - SLAB - 0.02f, 0f, 0f, 2.2f, wall, F0);
        WallX(p, it, ox + 9.0f, ox + 12f, 0.4f, 0.02f, W - SLAB - 0.02f, 0f, 0f, 2.2f, wall, F0);
        // 教室隔墙
        WallZ(p, it, -6f, 0.4f, ox, 0.02f, W - SLAB - 0.02f, 0f, 0f, 2.2f, wall, F0);
        // 走廊南墙：门厅开口 x -4.6..3.2，办公室门 x 4.4..5.6
        WallX(p, it, ox - 12f, ox - 4.6f, 2.6f, 0.02f, W - SLAB - 0.02f, 0f, 0f, 2.2f, wall, F0);
        WallX(p, it, ox + 3.2f, ox + 4.4f, 2.6f, 0.02f, W - SLAB - 0.02f, 0f, 0f, 2.2f, wall, F0);
        WallX(p, it, ox + 5.6f, ox + 12f, 2.6f, 0.02f, W - SLAB - 0.02f, 0f, 0f, 2.2f, wall, F0);
        // 办公室东墙 / 门厅东墙
        WallZ(p, it, 2.6f, 6f, ox + 7.6f, 0.02f, W - SLAB - 0.02f, 0f, 0f, 2.2f, wall, F0);
        WallZ(p, it, 2.6f, 6f, ox + 3.2f, 0.02f, W - SLAB - 0.02f, 0f, 0f, 2.2f, wall, F0);

        // ── 楼梯：低端在门厅南侧 (z=5.6)，高端进走廊 (z=1.8)，宽 1.6 ──
        Stairs(p, it, ox - 4.6f, 5.6f, ox - 3.0f, 1.8f, 0, 1, false, woodDark);
        WallZ(p, it, 1.8f, 6f, ox - 3.0f, 0.02f, W - SLAB - 0.02f, 0f, 0f, 2.2f, wall, F0);
        WallX(p, it, ox - 4.6f, ox - 3.0f, 1.8f, 0.02f, W - SLAB - 0.02f, 0f, 0f, 2.2f, wall, F0);
        Rail(p, new Vector3(ox - 3.0f, 1.05f, 5.6f), new Vector3(ox - 3.0f, 4.85f, 1.8f), woodDark);

        // ── 二层：走廊 + 教室 C/D ──
        WallX(p, it, ox - 12f, ox - 9.0f, 0.4f, y1, h1, 0f, 0f, 2.2f, wall, F1);
        WallX(p, it, ox - 7.8f, ox - 1.0f, 0.4f, y1, h1, 0f, 0f, 2.2f, wall, F1);
        WallX(p, it, ox + 1.0f, ox + 7.8f, 0.4f, y1, h1, 0f, 0f, 2.2f, wall, F1);
        WallX(p, it, ox + 9.0f, ox + 12f, 0.4f, y1, h1, 0f, 0f, 2.2f, wall, F1);
        WallZ(p, it, -6f, 0.4f, ox, y1, h1, 0f, 0f, 2.2f, wall, F1);
        // 二层南侧：走廊墙（楼梯口以西留空）
        WallX(p, it, ox - 3.0f, ox + 12f, 2.6f, y1, h1, 0f, 0f, 2.2f, wall, F1);

        // ── 门厅家具 ──
        Part(p, new Vector3(ox + 1.6f, 0.52f, 4.6f), new Vector3(2.2f, 1.04f, 0.5f), woodDark, 1f);      // 值班台
        Part(p, new Vector3(ox + 1.6f, 1.06f, 4.6f), new Vector3(2.3f, 0.06f, 0.56f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox - 2.6f, 1.5f, 5.9f), new Vector3(2.6f, 1.2f, 0.08f), green, 1f);          // 公告栏
        Lamp(p, new Vector3(ox + 0.6f, 3.2f, 4.4f), 9f, 1.2f);

        // ── 办公室家具 ──
        Table(p, new Vector3(ox + 5.4f, 0f, 4.4f), 1.8f, 0.9f, 0.74f, wood);
        Chair(p, it, new Vector3(ox + 5.4f, 0f, 5.2f), 180f, wood, F0, true);
        Part(p, new Vector3(ox + 6.6f, 0.9f, 3.0f), new Vector3(0.5f, 1.8f, 1.2f), woodDark, 1f);        // 书柜
        Lamp(p, new Vector3(ox + 5.4f, 3.2f, 4.4f), 7f, 0.9f);

        // ── 四间教室 ──
        Classroom(p, it, ox - 6f, -2.6f, 11.6f, 6.0f, 0.02f, F0, board, deskTop, woodDark, green);
        Classroom(p, it, ox + 6f, -2.6f, 11.6f, 6.0f, 0.02f, F0, board, deskTop, woodDark, green);
        Classroom(p, it, ox - 6f, -2.6f, 11.6f, 6.0f, y1, F1, board, deskTop, woodDark, green);
        Classroom(p, it, ox + 6f, -2.6f, 11.6f, 6.0f, y1, F1, board, deskTop, woodDark, green);

        // ── 走廊灯 ──
        for (int i = -1; i <= 1; i++)
        {
            Lamp(p, new Vector3(ox + i * 8f, 3.15f, 1.5f), 9f, 1.1f);
            Lamp(p, new Vector3(ox + i * 8f, y1 + 3.15f, 1.5f), 9f, 1.1f);
        }

        return it;
    }

    // 一间教室：黑板 + 讲台 + 15 套课桌 + 后墙板报
    //   cx/cz = 教室中心，w/d = 教室尺寸；课桌按「列 1.7 / 排 1.7」摆，
    //   只把桌面板登记成碰撞盒（椅子可穿过），保证排与排之间走得过去
    static void Classroom(Transform p, Interior it, float cx, float cz, float w, float d, float fy,
                          int mask, Material board, Material desk, Material wood, Material green)
    {
        float zBack = cz - d * 0.5f;    // 北墙（黑板墙）内表面
        float zFront = cz + d * 0.5f;   // 南墙（靠走廊）内表面

        // 黑板 + 讲台
        Part(p, new Vector3(cx, fy + 1.45f, zBack + 0.10f), new Vector3(4.6f, 1.25f, 0.07f), board, 4.6f);
        Part(p, new Vector3(cx, fy + 0.42f, zBack + 0.80f), new Vector3(1.8f, 0.84f, 0.7f), wood, 1f);
        Part(p, new Vector3(cx, fy + 0.86f, zBack + 0.80f), new Vector3(1.9f, 0.05f, 0.76f), C("#d9d3c6"), 1f);

        // 课桌：5 列 × 3 排
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 5; c++)
            {
                float x = cx - 3.4f + c * 1.7f;
                float z = zBack + 1.75f + r * 1.6f;
                Part(p, new Vector3(x, fy + 0.72f, z), new Vector3(1.0f, 0.06f, 0.6f), desk, 1.1f);
                for (int k = 0; k < 4; k++)
                    Part(p, new Vector3(x + (k % 2 == 0 ? -0.44f : 0.44f), fy + 0.36f, z + (k < 2 ? -0.24f : 0.24f)),
                         new Vector3(0.05f, 0.72f, 0.05f), C("#8d8d94"), 0.6f);
                Chair(p, it, new Vector3(x, fy, z + 0.58f), 0f, wood, mask, false);
                it.Solids.Add(new Solid(x - 0.5f, z - 0.3f, x + 0.5f, z + 0.3f, mask));
            }

        // 后墙板报
        Part(p, new Vector3(cx, fy + 1.6f, zFront - 0.10f), new Vector3(4.0f, 1.2f, 0.06f), green, 4f);

        // 教室灯
        Lamp(p, new Vector3(cx - 2.6f, fy + 3.2f, cz), 8f, 1.0f);
        Lamp(p, new Vector3(cx + 2.6f, fy + 3.2f, cz), 8f, 1.0f);
    }
}
