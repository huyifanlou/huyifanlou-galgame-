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

    // 家具碰撞盒（只登记平面 AABB，挡人用）
    static void Reg(Interior it, float cx, float cz, float w, float d, int mask)
        => it.Solids.Add(new Solid(cx - w * 0.5f, cz - d * 0.5f, cx + w * 0.5f, cz + d * 0.5f, mask));

    static Material _glass;
    static Material Glass()
    {
        if (_glass == null) _glass = MatLib.UnlitColor(MatLib.Hex("#c4d8e4"));
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

    // 墙面板：贴在墙内表面的薄片，用来给每个房间换壁纸 / 做墙裙（纯视觉，不参与碰撞）
    static void Panel(Transform p, float x1, float z1, float x2, float z2, float y0, float h, Material mat)
    {
        float w = Mathf.Abs(x2 - x1), d = Mathf.Abs(z2 - z1);
        Part(p, new Vector3((x1 + x2) * 0.5f, y0 + h * 0.5f, (z1 + z2) * 0.5f),
             new Vector3(Mathf.Max(w, 0.05f), h, Mathf.Max(d, 0.05f)), mat, 1.1f);
    }

    // 墙面挂件（挂画/挂钟/黑板等）：沿墙展开，按洞口列表裁剪成多段
    //   axis: 0=沿 X（南/北墙） 1=沿 Z（西/东墙）；constAxis = 墙的世界坐标（z 或 x）
    //   holes: 每项 (洞起, 洞止)（沿展开轴）；y 范围与洞口重叠的整段被挖掉
    static void WallDeco(Transform p, float a0, float a1, float constAxis, float y0, float y1,
                         int axis, float thick, Material mat, float uvMeters,
                         (float, float)[] holes = null, Vector3 euler = default)
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
                     new Vector3(t - f, y1 - y0, thick), mat, uvMeters, euler);
            else
                Part(p, new Vector3(constAxis, (y0 + y1) * 0.5f, (f + t) * 0.5f),
                     new Vector3(thick, y1 - y0, t - f), mat, uvMeters, euler);
        }
    }

    // 给矩形房间的四面内墙整间贴皮（rect 用墙中心线）
    //   doors 按 (墙号, 起, 止) 三元组：0=南(z1) 1=北(z2) 2=西(x1) 3=东(x2)
    static void RoomSkin(Transform p, float x1, float z1, float x2, float z2, float y0, float h,
                         Material mat, params float[] doors)
    {
        const float OFF = 0.11f;   // 墙面到墙中心线的距离（外墙半厚 0.12 / 内墙 0.09，取中间值）
        for (int w = 0; w < 4; w++)
        {
            var gaps = new System.Collections.Generic.List<float[]>();
            for (int i = 0; i + 2 < doors.Length; i += 3)
                if ((int)doors[i] == w) gaps.Add(new[] { doors[i + 1], doors[i + 2] });
            gaps.Sort((a, b) => a[0].CompareTo(b[0]));

            float a0 = w < 2 ? x1 : z1, a1 = w < 2 ? x2 : z2;
            float cur = a0;
            for (int k = 0; k < gaps.Count; k++)
            {
                if (gaps[k][0] > cur) Seg(w, cur, gaps[k][0]);
                cur = Mathf.Max(cur, gaps[k][1]);
            }
            if (cur < a1) Seg(w, cur, a1);
        }

        void Seg(int ww, float f, float t)
        {
            if (t - f < 0.03f) return;
            switch (ww)
            {
                case 0: Panel(p, f, z1 + OFF, t, z1 + OFF, y0, h, mat); break;
                case 1: Panel(p, f, z2 - OFF, t, z2 - OFF, y0, h, mat); break;
                case 2: Panel(p, x1 + OFF, f, x1 + OFF, t, y0, h, mat); break;
                default: Panel(p, x2 - OFF, f, x2 - OFF, t, y0, h, mat); break;
            }
        }
    }

    // 踢脚线
    static void Skirt(Transform p, float x1, float z1, float x2, float z2, float y0)
        => Panel(p, x1, z1, x2, z2, y0, 0.12f, C("#5f4a38"));

    // 挂画：外框 + 画面 + 两块色块
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

    // 挂钟
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

    // 绿植：花盆 + 不规则叶团
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

    // 窗帘（窗两侧各一片）+ 帘杆
    static void Curtain(Transform p, float cx, float y, float cz, float w, float h, Material cloth)
    {
        Part(p, new Vector3(cx - w * 0.5f + 0.20f, y + h * 0.5f, cz), new Vector3(0.34f, h, 0.07f), cloth, 1f);
        Part(p, new Vector3(cx + w * 0.5f - 0.20f, y + h * 0.5f, cz), new Vector3(0.34f, h, 0.07f), cloth, 1f);
        Part(p, new Vector3(cx, y + h + 0.06f, cz), new Vector3(w + 0.4f, 0.06f, 0.09f), C("#6f5540"), 1f);
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

    // ── 家：30×20 两层（约 3.1× 旧面积），一层 8 区 + 宽楼梯，二层 7 区 ──
    //    净高 4.0m；楼梯间 x 10..15 专用（两侧条带封死，进楼即上楼梯）
    static Interior Home(Transform parent)
    {
        float ox = HOME_X;
        var it = new Interior
        {
            Id = "home",
            Name = "家",
            X1 = ox - 15f, X2 = ox + 15f, Z1 = -10f, Z2 = 10f,
            Spawn = new Vector2(ox, -8.4f),
            Exit = new Vector2(ox, -9.3f),
            FloorY = new[] { 0.02f, 4.40f },
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

        const float W = 4.0f;
        const float top = 8.40f;
        float y1 = it.FloorY[1];
        float h0 = W - 0.02f, h1 = W - 0.02f;

        // 分区（世界坐标偏移）
        const float XW = -15f, XA = -3f, XB = 4f, XE = 15f;
        const float ZS = -10f, ZM = 0f, ZC = 3f, ZN = 10f;

        // ── 地面 ──
        Part(p, new Vector3(ox, 0f, 0f), new Vector3(30f, 0.04f, 20f), floor, 2f, default, true);
        Part(p, new Vector3(ox + 6f, 0.05f, -5f), new Vector3(9f, 0.02f, 10f), tile, 1.4f);      // 餐厨区瓷砖
        Part(p, new Vector3(ox - 12f, 0.05f, 6.5f), new Vector3(6f, 0.02f, 7f), tile, 1.4f);     // 客卫瓷砖

        // ── 外墙 ──
        WallX(p, it, ox + XW, ox + XE, ZS, 0f, top, ox - 1.0f, ox + 1.0f, 2.6f, wall, BOTH, WALL_OUT);   // 大门
        WallX(p, it, ox + XW, ox + XE, ZN, 0f, top, ox + 2.0f, ox + 3.0f, 2.6f, wall, BOTH, WALL_OUT);   // 后门
        WallZ(p, it, ZS, ZN, ox + XW, 0f, top, 0f, 0f, 2.6f, wall, BOTH, WALL_OUT);
        WallZ(p, it, ZS, ZN, ox + XE, 0f, top, 0f, 0f, 2.6f, wall, BOTH, WALL_OUT);

        // ── 二层楼板（楼梯洞 x 10.8..14.2 / z 3.5..9.5）──
        Slab(p, ox + XW, ZS, ox + XE, 3.5f, y1, ceil, floor);
        Slab(p, ox + XW, 3.5f, ox + 10.8f, 9.5f, y1, ceil, floor);
        Slab(p, ox + 14.2f, 3.5f, ox + XE, 9.5f, y1, ceil, floor);
        Slab(p, ox + XW, 9.5f, ox + XE, ZN, y1, ceil, floor);
        Slab(p, ox + XW, ZS, ox + XE, ZN, top, ceil);

        // ── 一层隔墙 ──
        WallZ(p, it, ZS, ZM, ox + XA, 0.02f, h0, -2.8f, -1.4f, 2.4f, wall, F0);   // 客厅|玄关餐厅
        WallZ(p, it, ZS, ZM, ox + XB, 0.02f, h0, -4.4f, -3.0f, 2.4f, wall, F0);   // 餐厅|厨房
        WallX(p, it, ox + XW, ox + XA, ZM, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);    // 客厅北墙（实）
        WallX(p, it, ox + XA, ox + XB, ZM, 0.02f, h0, -1.6f, -0.4f, 2.4f, wall, F0);  // 玄关→走廊
        WallX(p, it, ox + XB, ox + XE, ZM, 0.02f, h0, 8.0f, 9.2f, 2.4f, wall, F0);    // 厨房→走廊
        // 走廊北墙 z=3：客卫/储藏/家务各开门，楼梯间 x 10..15 敞开
        WallX(p, it, ox + XW, ox - 9f, ZC, 0.02f, h0, -12.4f, -11.2f, 2.4f, wall, F0);
        WallX(p, it, ox - 9f, ox + XA, ZC, 0.02f, h0, -6.4f, -5.2f, 2.4f, wall, F0);
        WallX(p, it, ox + XA, ox + 10f, ZC, 0.02f, h0, 0.4f, 1.6f, 2.4f, wall, F0);
        // 北带竖墙
        WallZ(p, it, ZC, ZN, ox - 9f, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);
        WallZ(p, it, ZC, ZN, ox + XA, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);
        WallZ(p, it, ZC, ZN, ox + 10f, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);

        // ── 楼梯：楼梯间 x 10..15，梯身 x 10.8..14.2，z 3.5(低)→9.5(高)；两侧条带封死 ──
        Stairs(p, it, ox + 10.8f, 3.5f, ox + 14.2f, 9.5f, 0, 1, false, woodDark);
        WallZ(p, it, 3.5f, 9.5f, ox + 10.8f, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);   // 西挡墙（封条带）
        WallZ(p, it, 3.5f, 9.5f, ox + 14.2f, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);   // 东挡墙
        WallZ(p, it, 3.5f, 9.5f, ox + 10.8f, y1, h1, 0f, 0f, 2.4f, wall, F1);
        WallZ(p, it, 3.5f, 9.5f, ox + 14.2f, y1, h1, 0f, 0f, 2.4f, wall, F1);
        Lamp(p, new Vector3(ox + 12.5f, 3.6f, 6.5f), 9f, 1.15f);

        // ── 二层隔墙 ──
        WallZ(p, it, ZS, ZM, ox + XA, y1, h1, 0f, 0f, 2.4f, wall, F1);
        WallZ(p, it, ZS, ZM, ox + XB, y1, h1, 0f, 0f, 2.4f, wall, F1);
        WallX(p, it, ox + XW, ox + XA, ZM, y1, h1, -9.4f, -8.2f, 2.4f, wall, F1);   // 主卧门
        WallX(p, it, ox + XA, ox + XB, ZM, y1, h1, 0.2f, 1.4f, 2.4f, wall, F1);     // 起居厅门
        WallX(p, it, ox + XB, ox + XE, ZM, y1, h1, 9.0f, 10.2f, 2.4f, wall, F1);    // 次卧门
        WallX(p, it, ox + XW, ox - 6f, ZC, y1, h1, -10.4f, -9.2f, 2.4f, wall, F1);  // 书房门
        WallX(p, it, ox - 6f, ox + 0f, ZC, y1, h1, -3.4f, -2.2f, 2.4f, wall, F1);   // 卫生间门
        WallX(p, it, ox + 0f, ox + 5f, ZC, y1, h1, 2.0f, 3.2f, 2.4f, wall, F1);     // 衣帽间门
        WallZ(p, it, ZC, ZN, ox - 6f, y1, h1, 0f, 0f, 2.4f, wall, F1);
        WallZ(p, it, ZC, ZN, ox + 0f, y1, h1, 0f, 0f, 2.4f, wall, F1);
        WallZ(p, it, ZC, ZN, ox + 5f, y1, h1, 0f, 0f, 2.4f, wall, F1);

        // ── 墙面：整间贴皮 + 外墙内表面补贴（WallDeco 按窗/门裁剪）──
        var wallA = T("in_wall");
        var wallB = T("in_wall2");
        var wallC = T("in_wall3");
        var wallT = T("in_walltile");
        var wallP = T("in_panel");
        var cloth = C("#c9b79c");

        // 一层
        RoomSkin(p, ox + XW, ZS, ox + XA, ZM, 0.02f, 4.0f, wallA, 3, -2.8f, -1.4f);                     // 客厅
        RoomSkin(p, ox + XA, ZS, ox + XB, ZM, 0.02f, 4.0f, wallA,
                 0, ox - 1.0f, ox + 1.0f, 2, -2.8f, -1.4f, 3, -4.4f, -3.0f, 1, -1.6f, -0.4f);           // 玄关+餐厅
        RoomSkin(p, ox + XB, ZS, ox + XE, ZM, 0.02f, 4.0f, wallT,
                 2, -4.4f, -3.0f, 1, 8.0f, 9.2f);                                                        // 厨房
        RoomSkin(p, ox + XW, ZC, ox - 9f, ZN, 0.02f, 4.0f, wallT, 0, -12.4f, -11.2f);                    // 客卫
        RoomSkin(p, ox - 9f, ZC, ox + XA, ZN, 0.02f, 4.0f, wallC, 0, -6.4f, -5.2f);                      // 储藏
        RoomSkin(p, ox + XA, ZC, ox + 10f, ZN, 0.02f, 4.0f, wallC,
                 0, 0.4f, 1.6f, 1, -1.6f, -0.4f, 3, -2.8f, -1.4f, 2, -4.4f, -3.0f);                      // 家务（含过道面）
        RoomSkin(p, ox + 10f, ZC, ox + XE, ZN, 0.02f, 4.0f, wallA, 1, -1.6f, -0.4f, 0, 8.0f, 9.2f);      // 楼梯间
        RoomSkin(p, ox + XW, ZM, ox + XE, ZC, 0.02f, 4.0f, wallA,
                 0, -1.6f, -0.4f, 0, 8.0f, 9.2f,
                 1, -12.4f, -11.2f, 1, -6.4f, -5.2f, 1, 0.4f, 1.6f);                                     // 走廊
        // 外墙内表面
        WallDeco(p, ox + XW + 0.13f, ox - 1.2f, ZS + 0.13f, 0.02f, 4.02f, 0, 0.05f, wallA, 1.7f,
                 new[] { (ox - 12.9f, ox - 11.1f), (ox - 8.9f, ox - 7.1f) });
        WallDeco(p, ox + 1.2f, ox + XE - 0.13f, ZS + 0.13f, 0.02f, 4.02f, 0, 0.05f, wallT, 1.7f,
                 new[] { (ox - 0.3f, ox + 1.3f), (ox + 7.2f, ox + 8.8f), (ox + 11.2f, ox + 12.8f) });
        WallDeco(p, ZS + 0.13f, ZN - 0.13f, ox + XW + 0.13f, 0.02f, 4.02f, 1, 0.05f, wallA, 1.7f, null);
        WallDeco(p, ZS + 0.13f, ZN - 0.13f, ox + XE - 0.13f, 0.02f, 4.02f, 1, 0.05f, wallT, 1.7f, null);

        // 二层
        RoomSkin(p, ox + XW, ZS, ox + XA, ZM, y1, 4.0f, wallB, 1, -9.4f, -8.2f);                          // 主卧
        RoomSkin(p, ox + XA, ZS, ox + XB, ZM, y1, 4.0f, wallB, 1, 0.2f, 1.4f);                            // 起居厅
        RoomSkin(p, ox + XB, ZS, ox + XE, ZM, y1, 4.0f, wallC, 1, 9.0f, 10.2f);                           // 次卧
        RoomSkin(p, ox + XW, ZM, ox + XE, ZC, y1, 4.0f, wallB,
                 0, -9.4f, -8.2f, 0, 0.2f, 1.4f, 0, 9.0f, 10.2f,
                 1, -10.4f, -9.2f, 1, -3.4f, -2.2f, 1, 2.0f, 3.2f);                                       // 二层走廊
        RoomSkin(p, ox + XW, ZC, ox - 6f, ZN, y1, 4.0f, wallP, 0, -10.4f, -9.2f);                         // 书房
        RoomSkin(p, ox - 6f, ZC, ox + 0f, ZN, y1, 4.0f, wallT, 0, -3.4f, -2.2f);                          // 卫生间
        RoomSkin(p, ox + 0f, ZC, ox + 5f, ZN, y1, 4.0f, wallP, 0, 2.0f, 3.2f);                            // 衣帽间
        RoomSkin(p, ox + 5f, ZC, ox + XE, ZN, y1, 4.0f, wallB,
                 1, -1.6f, -0.4f, 0, 0.2f, 1.4f, 0, 9.0f, 10.2f);                                         // 楼梯厅
        WallDeco(p, ox + XW + 0.13f, ox + XE - 0.13f, ZS + 0.13f, y1, y1 + 4.02f, 0, 0.05f, wallB, 1.7f,
                 new[] { (ox - 10.1f, ox - 7.9f), (ox - 0.5f, ox + 1.5f), (ox + 8.7f, ox + 10.3f) });
        WallDeco(p, ZS + 0.13f, ZN - 0.13f, ox + XW + 0.13f, y1, y1 + 4.02f, 1, 0.05f, wallB, 1.7f, null);
        WallDeco(p, ZS + 0.13f, ZN - 0.13f, ox + XE - 0.13f, y1, y1 + 4.02f, 1, 0.05f, wallB, 1.7f, null);
        WallDeco(p, ox + XW + 0.13f, ox + XE - 0.13f, ZN - 0.13f, y1, y1 + 4.02f, 0, 0.05f, wallB, 1.7f,
                 new[] { (ox + 1.4f, ox + 2.6f) });
        Skirt(p, ox + XW + 0.18f, ZS, ox + XW + 0.18f, ZM, 0.02f);
        Skirt(p, ox + XW, ZS + 0.18f, ox + XA, ZS + 0.18f, 0.02f);
        Skirt(p, ox + XW, ZS + 0.18f, ox + XA, ZS + 0.18f, y1);

        // ── 装饰 ──
        Picture(p, new Vector3(ox + XW + 0.17f, 2.0f, -6.5f), 1.1f, 0.85f, 90f);
        Picture(p, new Vector3(ox + XW + 0.17f, 2.0f, -4.6f), 0.9f, 1.1f, 90f);
        WallClock(p, new Vector3(ox + XW + 0.17f, 2.9f, -2.0f), 90f);
        Plant(p, new Vector3(ox - 14.2f, 0.02f, -9.2f), 1.1f);
        Picture(p, new Vector3(ox + XA + 0.16f, 1.9f, -6.0f), 1.0f, 0.75f, 90f);
        Picture(p, new Vector3(ox + XW + 0.17f, y1 + 2.1f, -3.0f), 1.3f, 0.9f, 90f);
        Curtain(p, ox - 9f, y1 + 1.15f, ZS + 0.24f, 2.4f, 1.7f, cloth);
        Curtain(p, ox + 9.5f, y1 + 1.15f, ZS + 0.24f, 1.8f, 1.6f, cloth);
        Picture(p, new Vector3(ox - 10.5f, y1 + 2.1f, ZN - 0.17f), 1.2f, 0.85f, 180f);
        Plant(p, new Vector3(ox + 13.8f, y1, 4.2f), 1.0f);

        // 门外天光
        Part(p, new Vector3(ox, 1.3f, ZS - 0.34f), new Vector3(2.8f, 2.6f, 0.05f), Glass(), 1f);
        Part(p, new Vector3(ox + 2.5f, 1.3f, ZN + 0.34f), new Vector3(1.5f, 2.4f, 0.05f), Glass(), 1f);

        // ── 一层窗（南墙）──
        Window(p, new Vector3(ox - 12f, 2.0f, -9.74f), 1.8f, 1.5f, 0f);
        Window(p, new Vector3(ox - 8f, 2.0f, -9.74f), 1.8f, 1.5f, 0f);
        Window(p, new Vector3(ox + 0.5f, 2.0f, -9.74f), 1.5f, 1.5f, 0f);
        Window(p, new Vector3(ox + 8f, 2.0f, -9.74f), 1.5f, 1.5f, 0f);
        Window(p, new Vector3(ox + 12f, 2.0f, -9.74f), 1.5f, 1.5f, 0f);

        // ════════ 一层家具 ════════
        // 客厅 12×10
        Rug(p, new Vector3(ox - 9f, 0.06f, -5f), 4.5f, 3.2f, fabric2);
        Part(p, new Vector3(ox - 14.1f, 0.5f, -5f), new Vector3(1.0f, 0.5f, 3.4f), fabric, 1.2f);
        Part(p, new Vector3(ox - 14.65f, 0.75f, -5f), new Vector3(0.2f, 0.95f, 3.4f), fabric, 1.2f);
        Part(p, new Vector3(ox - 14.1f, 0.72f, -6.85f), new Vector3(1.0f, 0.62f, 0.2f), fabric, 1.2f);
        Part(p, new Vector3(ox - 14.1f, 0.72f, -3.15f), new Vector3(1.0f, 0.62f, 0.2f), fabric, 1.2f);
        Part(p, new Vector3(ox - 11.6f, 0.24f, -5f), new Vector3(1.4f, 0.46f, 0.7f), woodDark, 1f);
        Part(p, new Vector3(ox - 3.75f, 0.36f, -5f), new Vector3(0.55f, 0.72f, 2.8f), woodDark, 1f);
        Part(p, new Vector3(ox - 3.45f, 1.45f, -5f), new Vector3(0.07f, 0.95f, 1.8f), screen, 1f);
        Part(p, new Vector3(ox - 9f, 1.05f, -0.45f), new Vector3(3.0f, 2.1f, 0.4f), woodDark, 1f);
        Lamp(p, new Vector3(ox - 9f, 3.5f, -5f), 10f, 1.25f);
        Part(p, new Vector3(ox - 14.4f, 1.05f, -1.4f), new Vector3(0.07f, 2.0f, 0.07f), metal, 1f);
        Part(p, new Vector3(ox - 14.4f, 2.1f, -1.4f), new Vector3(0.36f, 0.3f, 0.36f), C("#fff2d6"), 1f);

        // 玄关
        Part(p, new Vector3(ox + 3.6f, 0.55f, -8.5f), new Vector3(0.42f, 1.1f, 2.0f), woodDark, 1f);
        Part(p, new Vector3(ox + 1.4f, 0.22f, -9.3f), new Vector3(0.9f, 0.44f, 0.4f), wood, 1f);
        Part(p, new Vector3(ox + 3.72f, 1.0f, -5.0f), new Vector3(0.06f, 1.4f, 0.8f), C("#b8c6cc"), 1f);
        Lamp(p, new Vector3(ox + 0.5f, 3.5f, -8.5f), 8f, 1.0f);

        // 餐厅 7×10
        Table(p, new Vector3(ox + 0.5f, 0f, -5f), 2.2f, 1.2f, 0.76f, wood);
        Chair(p, it, new Vector3(ox + 0.5f, 0f, -6.4f), 0f, wood, F0, true);
        Chair(p, it, new Vector3(ox + 0.5f, 0f, -3.6f), 180f, wood, F0, true);
        Chair(p, it, new Vector3(ox - 1.2f, 0f, -5f), 90f, wood, F0, true);
        Chair(p, it, new Vector3(ox + 2.2f, 0f, -5f), 270f, wood, F0, true);
        Lamp(p, new Vector3(ox + 0.5f, 3.45f, -5f), 8f, 1.1f);

        // 厨房 11×10：L 型台面
        Part(p, new Vector3(ox + 9.5f, 0.49f, -9.45f), new Vector3(9.0f, 0.98f, 0.66f), white, 1f);
        Part(p, new Vector3(ox + 9.5f, 1.0f, -9.45f), new Vector3(9.05f, 0.06f, 0.7f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox + 14.5f, 0.49f, -5.0f), new Vector3(0.7f, 0.98f, 8.0f), white, 1f);
        Part(p, new Vector3(ox + 14.5f, 1.0f, -5.0f), new Vector3(0.74f, 0.06f, 8.05f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox + 7.0f, 1.02f, -9.4f), new Vector3(0.8f, 0.05f, 0.55f), C("#8d949c"), 1f);
        Part(p, new Vector3(ox + 11.0f, 1.02f, -9.4f), new Vector3(0.9f, 0.05f, 0.55f), C("#3a3f47"), 1f);
        Part(p, new Vector3(ox + 5.2f, 1.1f, -9.1f), new Vector3(0.95f, 2.2f, 0.75f), metal, 1f);
        Part(p, new Vector3(ox + 9.5f, 2.6f, -9.6f), new Vector3(8.0f, 0.8f, 0.38f), white, 1f);
        Lamp(p, new Vector3(ox + 9.5f, 3.5f, -4f), 10f, 1.15f);

        // 客卫 6×7
        Part(p, new Vector3(ox - 14.4f, 0.4f, 4.4f), new Vector3(0.48f, 0.8f, 0.78f), white, 1f);
        Part(p, new Vector3(ox - 14.55f, 0.88f, 4.1f), new Vector3(0.28f, 0.55f, 0.22f), white, 1f);
        Part(p, new Vector3(ox - 12.0f, 0.47f, 4.2f), new Vector3(1.15f, 0.94f, 0.55f), white, 1f);
        Part(p, new Vector3(ox - 12.0f, 0.96f, 4.2f), new Vector3(1.2f, 0.07f, 0.6f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox - 12.0f, 1.9f, 3.75f), new Vector3(0.9f, 0.8f, 0.05f), C("#b8c6cc"), 1f);
        Rug(p, new Vector3(ox - 13.4f, 0.06f, 8.6f), 1.4f, 1.4f, C("#8d9aa6"));
        Lamp(p, new Vector3(ox - 12f, 3.5f, 6.5f), 7f, 0.9f);

        // 储藏 6×7：四组架子
        for (int i = 0; i < 4; i++)
            Part(p, new Vector3(ox - 8.4f, 1.05f, 4.0f + i * 1.55f), new Vector3(0.5f, 2.1f, 1.3f), woodDark, 1f);
        Lamp(p, new Vector3(ox - 6f, 3.5f, 6.5f), 7f, 0.85f);

        // 家务 13×7：洗衣机 + 洗衣池 + 烘干机
        Part(p, new Vector3(ox - 2.2f, 0.49f, 4.0f), new Vector3(0.68f, 0.98f, 0.68f), white, 1f);
        Part(p, new Vector3(ox - 2.2f, 1.0f, 4.0f), new Vector3(0.7f, 0.05f, 0.7f), C("#c9ced3"), 1f);
        Part(p, new Vector3(ox - 1.2f, 0.49f, 4.0f), new Vector3(0.68f, 0.98f, 0.68f), white, 1f);
        Part(p, new Vector3(ox + 1.5f, 0.49f, 4.0f), new Vector3(1.6f, 0.98f, 0.68f), white, 1f);
        Lamp(p, new Vector3(ox + 2f, 3.5f, 6.5f), 9f, 0.9f);

        // ── 二层窗 ──
        Window(p, new Vector3(ox - 9f, y1 + 2.0f, -9.74f), 2.2f, 1.5f, 0f);
        Window(p, new Vector3(ox + 0.5f, y1 + 2.0f, -9.74f), 1.5f, 1.5f, 0f);
        Window(p, new Vector3(ox + 9.5f, y1 + 2.0f, -9.74f), 1.6f, 1.5f, 0f);
        Window(p, new Vector3(ox - 10.5f, y1 + 2.0f, 9.74f), 2.0f, 1.5f, 180f);
        Window(p, new Vector3(ox - 3f, y1 + 2.0f, 9.74f), 1.2f, 1.5f, 180f);
        Window(p, new Vector3(ox + 12.5f, y1 + 2.0f, 9.74f), 1.4f, 1.5f, 180f);

        // ════════ 二层家具 ════════
        // 主卧 12×10
        Part(p, new Vector3(ox - 9f, y1 + 0.25f, -8.7f), new Vector3(2.0f, 0.5f, 2.2f), woodDark, 1f);
        Part(p, new Vector3(ox - 9f, y1 + 0.58f, -8.7f), new Vector3(1.9f, 0.26f, 2.1f), C("#e8e4da"), 1f);
        Part(p, new Vector3(ox - 9f, y1 + 0.74f, -9.3f), new Vector3(1.75f, 0.14f, 0.9f), C("#cfd6dd"), 1f);
        Part(p, new Vector3(ox - 9f, y1 + 0.76f, -8.1f), new Vector3(1.2f, 0.15f, 0.4f), C("#f2eee4"), 1f);
        Part(p, new Vector3(ox - 9f, y1 + 0.78f, -9.72f), new Vector3(2.0f, 1.0f, 0.12f), woodDark, 1f);
        Part(p, new Vector3(ox - 10.5f, y1 + 0.28f, -8.9f), new Vector3(0.5f, 0.56f, 0.5f), woodDark, 1f);
        Part(p, new Vector3(ox - 7.5f, y1 + 0.28f, -8.9f), new Vector3(0.5f, 0.56f, 0.5f), woodDark, 1f);
        Part(p, new Vector3(ox - 14.4f, y1 + 1.25f, -3.5f), new Vector3(0.7f, 2.5f, 3.0f), woodDark, 1f);
        Part(p, new Vector3(ox - 3.8f, y1 + 0.85f, -7.0f), new Vector3(0.55f, 1.7f, 1.2f), wood, 1f);
        Part(p, new Vector3(ox - 3.75f, y1 + 0.42f, -7.0f), new Vector3(0.45f, 0.84f, 1.1f), wood, 1f);
        Rug(p, new Vector3(ox - 9f, y1 + 0.04f, -5.5f), 3.4f, 2.4f, fabric2);
        Lamp(p, new Vector3(ox - 9f, y1 + 3.5f, -4f), 10f, 1.2f);

        // 起居厅 7×10
        Part(p, new Vector3(ox + 0.5f, y1 + 0.48f, -8.9f), new Vector3(2.0f, 0.5f, 1.0f), fabric, 1.2f);
        Part(p, new Vector3(ox + 0.5f, y1 + 0.72f, -9.4f), new Vector3(2.0f, 0.95f, 0.2f), fabric, 1.2f);
        Part(p, new Vector3(ox + 0.5f, y1 + 0.24f, -6.7f), new Vector3(1.0f, 0.46f, 0.55f), woodDark, 1f);
        Part(p, new Vector3(ox + 3.4f, y1 + 1.05f, -3.0f), new Vector3(0.4f, 2.1f, 1.9f), woodDark, 1f);
        Lamp(p, new Vector3(ox + 0.5f, y1 + 3.5f, -5f), 9f, 1.05f);

        // 次卧 11×10
        Part(p, new Vector3(ox + 6.5f, y1 + 0.25f, -8.8f), new Vector3(1.4f, 0.5f, 2.1f), woodDark, 1f);
        Part(p, new Vector3(ox + 6.5f, y1 + 0.58f, -8.8f), new Vector3(1.32f, 0.26f, 2.0f), C("#e8e4da"), 1f);
        Part(p, new Vector3(ox + 6.5f, y1 + 0.78f, -9.6f), new Vector3(1.4f, 0.95f, 0.1f), woodDark, 1f);
        Part(p, new Vector3(ox + 13.5f, y1 + 0.85f, -9.3f), new Vector3(2.0f, 0.06f, 0.6f), wood, 1f);
        Part(p, new Vector3(ox + 13.5f, y1 + 0.4f, -9.3f), new Vector3(1.85f, 0.8f, 0.5f), wood, 1f);
        Chair(p, it, new Vector3(ox + 13.5f, y1, -8.4f), 0f, wood, F1, true);
        Part(p, new Vector3(ox + 14.4f, y1 + 1.2f, -2.0f), new Vector3(0.7f, 2.4f, 2.0f), woodDark, 1f);
        Lamp(p, new Vector3(ox + 9.5f, y1 + 3.5f, -4.5f), 9f, 1.05f);

        // 书房 9×7
        Part(p, new Vector3(ox - 10.5f, y1 + 0.85f, 3.9f), new Vector3(2.2f, 0.06f, 0.8f), wood, 1f);
        Part(p, new Vector3(ox - 10.5f, y1 + 0.4f, 3.9f), new Vector3(2.1f, 0.8f, 0.7f), wood, 1f);
        Chair(p, it, new Vector3(ox - 10.5f, y1, 4.85f), 180f, wood, F1, true);
        Part(p, new Vector3(ox - 14.5f, y1 + 1.1f, 6.5f), new Vector3(0.4f, 2.2f, 3.0f), woodDark, 1f);
        Part(p, new Vector3(ox - 8f, y1 + 0.48f, 8.8f), new Vector3(1.1f, 0.5f, 1.0f), fabric, 1.2f);
        Part(p, new Vector3(ox - 8f, y1 + 0.72f, 9.3f), new Vector3(1.1f, 0.95f, 0.2f), fabric, 1.2f);
        Lamp(p, new Vector3(ox - 10.5f, y1 + 3.5f, 6.5f), 9f, 1.1f);

        // 卫生间 6×7
        Part(p, new Vector3(ox - 3f, y1 + 0.34f, 8.8f), new Vector3(2.2f, 0.68f, 1.0f), white, 1f);
        Part(p, new Vector3(ox - 3f, y1 + 0.6f, 8.8f), new Vector3(2.0f, 0.22f, 0.84f), C("#cfe3e8"), 1f);
        Part(p, new Vector3(ox - 5.3f, y1 + 0.47f, 4.2f), new Vector3(1.15f, 0.94f, 0.55f), white, 1f);
        Part(p, new Vector3(ox - 5.3f, y1 + 0.96f, 4.2f), new Vector3(1.2f, 0.07f, 0.6f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox - 0.8f, y1 + 0.4f, 4.2f), new Vector3(0.48f, 0.8f, 0.78f), white, 1f);
        Lamp(p, new Vector3(ox - 3f, y1 + 3.5f, 6f), 7f, 1.0f);

        // 衣帽间 5×7
        Part(p, new Vector3(ox + 4.4f, y1 + 1.2f, 6.5f), new Vector3(0.65f, 2.4f, 4.0f), woodDark, 1f);
        Part(p, new Vector3(ox + 0.7f, y1 + 1.2f, 6.5f), new Vector3(0.65f, 2.4f, 4.0f), woodDark, 1f);
        Lamp(p, new Vector3(ox + 2.5f, y1 + 3.5f, 6.5f), 6f, 0.85f);

        // ── 家具碰撞 ──
        Reg(it, ox - 14.1f, -5f, 1.1f, 3.5f, F0);
        Reg(it, ox - 11.6f, -5f, 1.4f, 0.7f, F0);
        Reg(it, ox - 3.75f, -5f, 0.55f, 2.8f, F0);
        Reg(it, ox - 9f, -0.45f, 3.0f, 0.4f, F0);
        Reg(it, ox + 3.6f, -8.5f, 0.42f, 2.0f, F0);
        Reg(it, ox + 1.4f, -9.3f, 0.9f, 0.4f, F0);
        Reg(it, ox + 0.5f, -5f, 2.2f, 1.2f, F0);
        Reg(it, ox + 9.5f, -9.45f, 9.0f, 0.66f, F0);
        Reg(it, ox + 14.5f, -5.0f, 0.7f, 8.0f, F0);
        Reg(it, ox + 5.2f, -9.2f, 0.95f, 0.8f, F0);
        Reg(it, ox - 14.4f, 4.4f, 0.5f, 0.8f, F0);
        Reg(it, ox - 12.0f, 4.2f, 1.15f, 0.55f, F0);
        for (int i = 0; i < 4; i++) Reg(it, ox - 8.4f, 4.0f + i * 1.55f, 0.5f, 1.3f, F0);
        Reg(it, ox - 2.2f, 4.0f, 0.7f, 0.7f, F0);
        Reg(it, ox - 1.2f, 4.0f, 0.7f, 0.7f, F0);
        Reg(it, ox + 1.5f, 4.0f, 1.6f, 0.7f, F0);
        Reg(it, ox - 9f, -8.7f, 2.0f, 2.2f, F1);
        Reg(it, ox - 10.5f, -8.9f, 0.5f, 0.5f, F1);
        Reg(it, ox - 7.5f, -8.9f, 0.5f, 0.5f, F1);
        Reg(it, ox - 14.4f, -3.5f, 0.7f, 3.0f, F1);
        Reg(it, ox - 3.8f, -7.0f, 0.55f, 1.2f, F1);
        Reg(it, ox + 0.5f, -8.9f, 2.0f, 1.0f, F1);
        Reg(it, ox + 0.5f, -6.7f, 1.0f, 0.55f, F1);
        Reg(it, ox + 3.4f, -3.0f, 0.4f, 1.9f, F1);
        Reg(it, ox + 6.5f, -8.8f, 1.4f, 2.1f, F1);
        Reg(it, ox + 13.5f, -9.3f, 2.0f, 0.6f, F1);
        Reg(it, ox + 14.4f, -2.0f, 0.7f, 2.0f, F1);
        Reg(it, ox - 10.5f, 3.9f, 2.2f, 0.8f, F1);
        Reg(it, ox - 14.5f, 6.5f, 0.4f, 3.0f, F1);
        Reg(it, ox - 8f, 8.8f, 1.1f, 1.0f, F1);
        Reg(it, ox - 3f, 8.8f, 2.2f, 1.0f, F1);
        Reg(it, ox - 5.3f, 4.2f, 1.15f, 0.55f, F1);
        Reg(it, ox - 0.8f, 4.2f, 0.5f, 0.8f, F1);
        Reg(it, ox + 4.4f, 6.5f, 0.65f, 4.0f, F1);
        Reg(it, ox + 0.7f, 6.5f, 0.65f, 4.0f, F1);

        return it;
    }

    // ── 教学楼：56×30 两层（约 3.1× 旧面积）。教室 12×12，走廊净宽 5m ──
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
            FloorY = new[] { 0.02f, 4.60f },
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

        const float W = 4.2f;
        const float top = 8.80f;
        float y1 = it.FloorY[1];
        float h0 = W - 0.02f, h1 = W - 0.02f;

        const float ZC0 = -2.5f, ZC1 = 2.5f;   // 走廊 z -2.5..2.5（净 5m）

        // ── 地面 ──
        Part(p, new Vector3(ox, 0f, 0f), new Vector3(56f, 0.04f, 30f), floor, 2f, default, true);

        // ── 外墙（北墙正中大门）──
        WallX(p, it, ox - 28f, ox + 28f, -15f, 0f, top, 0f, 0f, 2.8f, wall, BOTH, WALL_OUT);
        WallX(p, it, ox - 28f, ox + 28f, 15f, 0f, top, ox - 2.5f, ox + 2.5f, 2.8f, wall, BOTH, WALL_OUT);
        WallZ(p, it, -15f, 15f, ox - 28f, 0f, top, 0f, 0f, 2.8f, wall, BOTH, WALL_OUT);
        WallZ(p, it, -15f, 15f, ox + 28f, 0f, top, 0f, 0f, 2.8f, wall, BOTH, WALL_OUT);

        // ── 二层楼板（楼梯洞 x -26.5..-22 / z 2.5..13.5）──
        Slab(p, ox - 28f, -15f, ox + 28f, 2.5f, y1, ceil, floor);
        Slab(p, ox - 28f, 2.5f, ox - 26.5f, 13.5f, y1, ceil, floor);
        Slab(p, ox - 22f, 2.5f, ox + 28f, 13.5f, y1, ceil, floor);
        Slab(p, ox - 28f, 13.5f, ox + 28f, 15f, y1, ceil, floor);
        Slab(p, ox - 28f, -15f, ox + 28f, 15f, top, ceil);

        // ── 一层：教室隔墙 + 走廊墙 ──
        // 教室 A x -27.5..-15.5 / B x -15..-3 / C x -2.5..9.5 / 多功能 x 10..27.5（北带，z -14.5..-2.5）
        WallZ(p, it, -14.5f, ZC0, ox - 15.25f, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);
        WallZ(p, it, -14.5f, ZC0, ox - 2.75f, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);
        WallZ(p, it, -14.5f, ZC0, ox + 9.75f, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);
        // 走廊北墙（z=-2.5）教室门
        WallX(p, it, ox - 27.5f, ox - 19.5f, ZC0, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);
        WallX(p, it, ox - 18.0f, ox - 11.0f, ZC0, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);
        WallX(p, it, ox - 9.5f, ox - 2.5f, ZC0, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);
        WallX(p, it, ox - 1.0f, ox + 5.5f, ZC0, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);
        WallX(p, it, ox + 7.0f, ox + 14.0f, ZC0, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);
        WallX(p, it, ox + 15.5f, ox + 27.5f, ZC0, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);
        // 走廊南墙（z=2.5）：楼梯间门 / 门厅敞开 / 办公室门 / 图书角门
        WallX(p, it, ox - 27.5f, ox - 26.5f, ZC1, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);
        WallX(p, it, ox - 22.0f, ox - 20.5f, ZC1, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);
        WallX(p, it, ox - 20.5f, ox - 13.0f, ZC1, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);
        WallX(p, it, ox - 5.0f, ox + 11.0f, ZC1, 0.02f, h0, ox - 1.0f, ox + 1.0f, 2.7f, wall, F0);   // 门厅敞开（宽口）
        WallX(p, it, ox + 11.0f, ox + 16.5f, ZC1, 0.02f, h0, ox + 12.5f, ox + 13.7f, 2.4f, wall, F0); // 办公室门
        WallX(p, it, ox + 16.5f, ox + 27.5f, ZC1, 0.02f, h0, ox + 21.0f, ox + 22.2f, 2.4f, wall, F0); // 图书角门
        // 南带竖墙
        WallZ(p, it, ZC1, 15f, ox - 20.5f, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);   // 楼梯间|门厅
        WallZ(p, it, ZC1, 15f, ox + 11.0f, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);   // 门厅|办公室
        WallZ(p, it, ZC1, 15f, ox + 16.5f, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);   // 办公室|图书角
        // 楼梯间两侧条带封死（楼梯 x -26.5..-22）
        WallZ(p, it, ZC1, 13.5f, ox - 26.5f, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);
        WallZ(p, it, ZC1, 13.5f, ox - 22f, 0.02f, h0, 0f, 0f, 2.4f, wall, F0);

        // ── 楼梯：x -26.5..-22（宽 4.5），z 2.5(低，走廊侧)→13.5(高) ──
        Stairs(p, it, ox - 26.5f, 2.5f, ox - 22f, 13.5f, 0, 1, false, woodDark);
        WallZ(p, it, 2.5f, 13.5f, ox - 26.5f, y1, h1, 0f, 0f, 2.4f, wall, F1);
        WallZ(p, it, 2.5f, 13.5f, ox - 22f, y1, h1, 0f, 0f, 2.4f, wall, F1);
        Lamp(p, new Vector3(ox - 24.25f, 3.8f, 8f), 10f, 1.2f);

        // ── 二层：教室墙 + 走廊南墙（楼梯厅敞开）──
        WallZ(p, it, -14.5f, ZC0, ox - 15.25f, y1, h1, 0f, 0f, 2.4f, wall, F1);
        WallZ(p, it, -14.5f, ZC0, ox - 2.75f, y1, h1, 0f, 0f, 2.4f, wall, F1);
        WallZ(p, it, -14.5f, ZC0, ox + 9.75f, y1, h1, 0f, 0f, 2.4f, wall, F1);
        WallX(p, it, ox - 27.5f, ox - 19.5f, ZC0, y1, h1, 0f, 0f, 2.4f, wall, F1);
        WallX(p, it, ox - 18.0f, ox - 11.0f, ZC0, y1, h1, 0f, 0f, 2.4f, wall, F1);
        WallX(p, it, ox - 9.5f, ox - 2.5f, ZC0, y1, h1, 0f, 0f, 2.4f, wall, F1);
        WallX(p, it, ox - 1.0f, ox + 5.5f, ZC0, y1, h1, 0f, 0f, 2.4f, wall, F1);
        WallX(p, it, ox + 7.0f, ox + 14.0f, ZC0, y1, h1, 0f, 0f, 2.4f, wall, F1);
        WallX(p, it, ox + 15.5f, ox + 27.5f, ZC0, y1, h1, 0f, 0f, 2.4f, wall, F1);
        WallX(p, it, ox - 20.5f, ox + 27.5f, ZC1, y1, h1, 0f, 0f, 2.4f, wall, F1);
        // 二层南带隔墙（楼梯厅 | 活动室）
        WallX(p, it, ox - 20.5f, ox + 27.5f, 13.5f, y1, h1, 0f, 0f, 2.4f, wall, F1);

        // 门外天光
        Part(p, new Vector3(ox, 1.45f, 15.34f), new Vector3(5.4f, 2.8f, 0.05f), Glass(), 1f);

        // ── 门厅：值班台 + 公告栏 + 绿植 ──
        Part(p, new Vector3(ox + 1.5f, 0.6f, 12.8f), new Vector3(3.0f, 1.2f, 0.7f), woodDark, 1f);
        Part(p, new Vector3(ox + 1.5f, 1.23f, 12.8f), new Vector3(3.1f, 0.07f, 0.76f), C("#d9d3c6"), 1f);
        Part(p, new Vector3(ox - 8f, 1.75f, 14.88f), new Vector3(3.2f, 1.5f, 0.08f), green, 1f);
        Plant(p, new Vector3(ox - 19.2f, 0.02f, 13.8f), 1.2f);
        Lamp(p, new Vector3(ox, 3.9f, 9f), 12f, 1.35f);
        Lamp(p, new Vector3(ox - 14f, 3.9f, 9f), 11f, 1.2f);

        // ── 办公室 ──
        Table(p, new Vector3(ox + 12.5f, 0f, 12.5f), 2.0f, 1.0f, 0.76f, wood);
        Chair(p, it, new Vector3(ox + 12.5f, 0f, 13.4f), 180f, wood, F0, true);
        Table(p, new Vector3(ox + 9.5f, 0f, 6.0f), 1.8f, 0.9f, 0.76f, wood);
        Chair(p, it, new Vector3(ox + 9.5f, 0f, 6.9f), 180f, wood, F0, true);
        Part(p, new Vector3(ox + 15.9f, 1.05f, 5.0f), new Vector3(0.5f, 2.1f, 1.6f), woodDark, 1f);
        Lamp(p, new Vector3(ox + 12f, 3.9f, 8.5f), 10f, 1.05f);

        // ── 图书角 ──
        for (int i = 0; i < 4; i++)
            Part(p, new Vector3(ox + 26.9f, 1.05f, 4.5f + i * 1.7f), new Vector3(0.5f, 2.1f, 1.5f), woodDark, 1f);
        Table(p, new Vector3(ox + 21.5f, 0f, 6.0f), 1.8f, 0.9f, 0.76f, wood);
        Table(p, new Vector3(ox + 21.5f, 0f, 9.5f), 1.8f, 0.9f, 0.76f, wood);
        Lamp(p, new Vector3(ox + 22f, 3.9f, 8f), 10f, 1.05f);

        // ── 教室（三层 + 东端多功能）──
        Classroom(p, it, ox - 21.5f, -8.5f, 12f, 12f, 0.02f, F0, board, deskTop, woodDark, green, ox - 19.5f, ox - 18.0f, 4);
        Classroom(p, it, ox - 9.0f, -8.5f, 12f, 12f, 0.02f, F0, board, deskTop, woodDark, green, ox - 11.0f, ox - 9.5f, 4);
        Classroom(p, it, ox + 3.5f, -8.5f, 12f, 12f, 0.02f, F0, board, deskTop, woodDark, green, ox + 2.5f, ox - 1.0f, 4);
        Classroom(p, it, ox + 18.75f, -8.5f, 17.5f, 12f, 0.02f, F0, board, deskTop, woodDark, green, ox + 14.0f, ox + 15.5f, 6);
        Classroom(p, it, ox - 21.5f, -8.5f, 12f, 12f, y1, F1, board, deskTop, woodDark, green, ox - 19.5f, ox - 18.0f, 4);
        Classroom(p, it, ox - 9.0f, -8.5f, 12f, 12f, y1, F1, board, deskTop, woodDark, green, ox - 11.0f, ox - 9.5f, 4);
        Classroom(p, it, ox + 3.5f, -8.5f, 12f, 12f, y1, F1, board, deskTop, woodDark, green, ox + 2.5f, ox - 1.0f, 4);
        Classroom(p, it, ox + 18.75f, -8.5f, 17.5f, 12f, y1, F1, board, deskTop, woodDark, green, ox + 14.0f, ox + 15.5f, 6);

        // ── 走廊灯 ──
        for (int i = -3; i <= 3; i++)
        {
            Lamp(p, new Vector3(ox + i * 8.5f, 3.9f, 0f), 11f, 1.15f);
            Lamp(p, new Vector3(ox + i * 8.5f, y1 + 3.9f, 0f), 11f, 1.15f);
        }

        // ── 家具碰撞 ──
        Reg(it, ox + 1.5f, 12.8f, 3.0f, 0.7f, F0);
        Reg(it, ox + 12.5f, 12.5f, 2.0f, 1.0f, F0);
        Reg(it, ox + 9.5f, 6.0f, 1.8f, 0.9f, F0);
        Reg(it, ox + 15.9f, 5.0f, 0.5f, 1.6f, F0);
        for (int i = 0; i < 4; i++) Reg(it, ox + 26.9f, 4.5f + i * 1.7f, 0.5f, 1.5f, F0);
        Reg(it, ox + 21.5f, 6.0f, 1.8f, 0.9f, F0);
        Reg(it, ox + 21.5f, 9.5f, 1.8f, 0.9f, F0);

        return it;
    }

    // 一间教室：12×12（多功能 17.5×12）。黑板 5×1.3（下沿 1.0），
    // 第一排距黑板 2.8m，列距 ≤2.8 / 排距 ≤1.45（自适应），走道 ≥0.85m
    static void Classroom(Transform p, Interior it, float cx, float cz, float w, float d, float fy,
                          int mask, Material board, Material desk, Material wood, Material green,
                          float doorA = 0f, float doorB = 0f, int groups = 4)
    {
        float zBack = cz - d * 0.5f;
        float zFront = cz + d * 0.5f;

        Part(p, new Vector3(cx, fy + 1.7f, zBack + 0.06f), new Vector3(5.0f, 1.3f, 0.07f), board, 5.0f);
        var skirt = C("#8fae98");
        float sx1 = cx - w * 0.5f + 0.05f, sx2 = cx + w * 0.5f - 0.05f;
        Panel(p, sx1, zBack + 0.03f, sx2, zBack + 0.03f, fy, 1.1f, skirt);
        (float, float)[] doorHoles = doorB > doorA ? new[] { (doorA - 0.09f, doorB + 0.09f) } : null;
        WallDeco(p, sx1, sx2, zFront - 0.03f, fy, fy + 1.1f, 0, 0.05f, skirt, 1.1f, doorHoles);
        Panel(p, cx - w * 0.5f + 0.03f, cz - d * 0.5f + 0.05f, cx - w * 0.5f + 0.03f, cz + d * 0.5f - 0.05f, fy, 1.1f, skirt);
        Panel(p, cx + w * 0.5f - 0.03f, cz - d * 0.5f + 0.05f, cx + w * 0.5f - 0.03f, cz + d * 0.5f - 0.05f, fy, 1.1f, skirt);
        WallDeco(p, sx1, sx2, zFront + 0.02f, fy + 1.1f, fy + 4.0f, 0, 0.05f, T("in_wall"), 1.7f, doorHoles);
        WallClock(p, new Vector3(cx, fy + 2.9f, zBack + 0.18f), 0f);
        Part(p, new Vector3(cx, fy + 0.45f, zBack + 0.85f), new Vector3(2.0f, 0.9f, 0.8f), wood, 1f);
        Part(p, new Vector3(cx, fy + 0.93f, zBack + 0.85f), new Vector3(2.1f, 0.05f, 0.86f), C("#d9d3c6"), 1f);

        // 课桌：groups 列 × 5 排
        const int rows = 5;
        int gcount = Mathf.Max(1, groups);
        float colGap = gcount > 1 ? Mathf.Min(2.8f, (w - 2.2f) / (gcount - 1)) : 0f;
        float rowGap = Mathf.Min(1.45f, (d - 4.6f) / (rows - 1));
        for (int r = 0; r < rows; r++)
            for (int g = 0; g < gcount; g++)
            {
                float gx = cx + (g - (gcount - 1) * 0.5f) * colGap;
                float z = zBack + 2.9f + r * rowGap;
                for (int k = 0; k < 2; k++)
                {
                    float x = gx + (k == 0 ? -0.34f : 0.34f);
                    Part(p, new Vector3(x, fy + 0.74f, z), new Vector3(0.62f, 0.05f, 0.42f), desk, 1.0f);
                    for (int lg = 0; lg < 4; lg++)
                        Part(p, new Vector3(x + (lg % 2 == 0 ? -0.27f : 0.27f), fy + 0.37f,
                                            z + (lg < 2 ? -0.17f : 0.17f)),
                             new Vector3(0.04f, 0.74f, 0.04f), C("#8d8d94"), 0.5f);
                    Chair(p, it, new Vector3(x, fy, z + 0.66f), 180f, wood, mask, false);
                }
                it.Solids.Add(new Solid(gx - 0.68f, z - 0.21f, gx + 0.68f, z + 1.06f, mask));
            }

        // 侧墙板报
        Part(p, new Vector3(cx - w * 0.5f + 0.08f, fy + 1.85f, cz + 2.0f), new Vector3(0.05f, 1.3f, 4.2f), green, 4.2f);

        // 教室灯（四盏）
        Lamp(p, new Vector3(cx - w * 0.25f, fy + 3.85f, cz - 2f), 10f, 0.85f);
        Lamp(p, new Vector3(cx + w * 0.25f, fy + 3.85f, cz - 2f), 10f, 0.85f);
        Lamp(p, new Vector3(cx - w * 0.25f, fy + 3.85f, cz + 2f), 10f, 0.85f);
        Lamp(p, new Vector3(cx + w * 0.25f, fy + 3.85f, cz + 2f), 10f, 0.85f);
    }

}
