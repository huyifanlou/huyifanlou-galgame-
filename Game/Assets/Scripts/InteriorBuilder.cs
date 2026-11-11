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
    // 出口净空区（门内侧 4~5m、门宽范围）：家具不得压这条动线
    public float ClearX1, ClearX2, ClearZ1, ClearZ2;
    public bool HasClear => ClearX2 > ClearX1 && ClearZ2 > ClearZ1;

    public readonly List<Solid> Solids = new List<Solid>();

    // 可上人的台面（礼堂舞台）：StageY>0 时台面矩形内取台面高度；
    // RampZ2 是坡道低端、RampZ1 是坡道高端（通常朝 -Z 上台）。
    public float StageY, StageX1, StageX2, StageZ1, StageZ2;
    public float RampX1, RampX2, RampZ1, RampZ2;

    public float FloorAt(float x, float z, float curY)
    {
        if (StageY > 0f)
        {
            if (x > StageX1 && x < StageX2 && z > StageZ1 && z < StageZ2) return StageY;
            if (x > RampX1 && x < RampX2 && z > RampZ1 && z < RampZ2)
            {
                float t = Mathf.InverseLerp(RampZ2, RampZ1, z);
                return Mathf.Lerp(FloorY[0], StageY, t);
            }
        }
        return FloorY[0];
    }

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
public static partial class InteriorBuilder
{
    const float HOME_X = 900f, SCHOOL_X = 1060f;
    const float WALL_OUT = 0.24f;
    const float WALL_IN = 0.18f;

    static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();

    // 当前房间天花板底面高度（各房间层高不同，不能写死）：吊牌吊杆与吊灯都要接到这里
    static float _ceilY;

    // 天花板薄片：铺顶的同时记录本房间吊顶高度，供 HangingSign / Pendant 接顶
    static void CeilingSlab(Transform p, float ox, float W, float w, float d, Material ceil)
    {
        _ceilY = W - 0.06f;
        Part(p, new Vector3(ox, W - 0.03f, 0f), new Vector3(w, 0.06f, d), ceil, 2f);
    }

    // 吊杆长度上限：大层高房间（礼堂 8.5m）把悬挂物整体抬高，而不是挂一根五米的细杆
    const float MAX_ROD = 2.8f;

    // 悬挂件中心高度：杆长不超过 MAX_ROD。ceil>0 时按指定吊顶接顶（如礼堂门头梁底 4.0）。
    static float HangY(float y, float halfH, float ceil = 0f)
    {
        if (ceil <= 0f) ceil = _ceilY;
        return ceil > 0f ? Mathf.Max(y, ceil - MAX_ROD - halfH) : y;
    }

    // 两根吊杆：从悬挂件顶面接到天花板底；没有层高信息时退回 0.7m 短杆
    static void HangRods(Transform p, float x, float top, float z, float w, float ceil = 0f)
    {
        if (ceil <= 0f) ceil = _ceilY;
        float y1 = ceil > top + 0.05f ? ceil : top + 0.7f;
        Part(p, new Vector3(x - w * 0.35f, (top + y1) * 0.5f, z), new Vector3(0.02f, y1 - top, 0.02f), C("#6a7078"), 1f);
        Part(p, new Vector3(x + w * 0.35f, (top + y1) * 0.5f, z), new Vector3(0.02f, y1 - top, 0.02f), C("#6a7078"), 1f);
    }

    static Material T(string tex)
    {
        if (!_cache.TryGetValue("T" + tex, out var m)) { m = MatLib.Lit(tex); _cache["T" + tex] = m; }
        return m;
    }

    // 室内木作统一走这块平涂色（docs/103）：
    // TextureLibrary 的 wood 是 #a1887f 的灰褐木纹，写实风格下没问题，但卡通渲染的暗面会把它压成脏褐，
    // 家具在画面里就「消失」了。室内家具量小、离镜头近，用高饱和平涂色 + 粗描边更符合卡通语言；
    // 大面积的墙纸/地砖/天花仍然走贴图（保持墙面细节）。世界侧的木头不受影响（WorldBuilder 直接调 MatLib）。
    static Material InteriorWood()
    {
        if (!_cache.TryGetValue("IW", out var m)) { m = CB("#f5c26a"); _cache["IW"] = m; }
        return m;
    }

    static Material C(string hex)
    {
        if (!_cache.TryGetValue("C" + hex, out var m)) { m = MatLib.LitColor(MatLib.Hex(hex)); _cache["C" + hex] = m; }
        return m;
    }

    // 粗描边材质：卡通渲染靠轮廓线分辨形体，室内近景家具用默认 0.0035 描边会糊成一片，
    // 主要家具（桌/椅/柜/床/沙发/台面）走这个入口（docs/103）
    // ⚠ 只给「大块面」用。细杆/窄条/小商品必须走 C()：描边是屏幕空间恒定宽度，
    //   0.06m 的细杆配 0.0055 描边，黑色会吃掉杆本身，画面立刻变脏（Lead 已确认描边 18m 内满强度）。
    static Material CB(string hex)
    {
        if (!_cache.TryGetValue("B" + hex, out var m)) { m = MatLib.LitColorBold(MatLib.Hex(hex)); _cache["B" + hex] = m; }
        return m;
    }

    // 按部件最小尺寸自动收描边：卡通描边是屏幕空间恒定宽度，细窄件用满宽描边会糊成一团黑。
    // 规则：最小边 ≥0.30m 用 0.0055（主要家具），≥0.14m 用 0.0042，≥0.06m 用 0.0032，更细的用 0.0024。
    static readonly Dictionary<string, Material> _outlineCache = new Dictionary<string, Material>();
    static Material OutlineMat(string hex, float minDim)
    {
        float w = minDim >= 0.30f ? 0.0055f
                : minDim >= 0.14f ? 0.0042f
                : minDim >= 0.06f ? 0.0032f
                : 0.0024f;
        string key = hex + "|" + w.ToString("F4");
        if (_outlineCache.TryGetValue(key, out var m)) return m;
        m = MatLib.LitColor(MatLib.Hex(hex));
        if (m.shader.name == "Campus/Toon")
        {
            m.SetFloat("_OutlineWidth", w);
            m.SetColor("_OutlineColor", new Color(0.09f, 0.07f, 0.13f, 1f));
        }
        _outlineCache[key] = m;
        return m;
    }

    // 同材质的「暗一档」变体：卡通分件用（台面托边、靠背压条、柜门缝…），
    // 不新开调色板就能在同一件家具上做出硬边分层
    static readonly Dictionary<Material, Material> _darker = new Dictionary<Material, Material>();
    static Material Darker(Material src, float k = 0.78f)
    {
        if (src == null) return null;
        if (_darker.TryGetValue(src, out var m)) return m;
        m = new Material(src) { color = new Color(src.color.r * k, src.color.g * k, src.color.b * k, 1f) };
        _darker[src] = m;
        return m;
    }

    // ── 卡通倒角盒 ──────────────────────────────────────────
    // 卡通渲染要「大块面 + 硬边」。主家具如果就是一个立方体，外轮廓只有一圈方角描边，
    // 明暗 ramp 也只有一个台阶，看上去像贴纸。倒角后每个转角多出一条 45° 窄面，
    // 会吃到相邻的第三个光照台阶，边缘立刻「硬」起来 —— 这是 cel shading 里最省成本的体积做法。
    // 碰撞仍然按原来的 AABB 走（PartC 不注册 Solid，Solid 由调用点各自 Reg），所以不影响走位。
    static readonly Dictionary<string, Mesh> _chamfer = new Dictionary<string, Mesh>();

    static Mesh ChamferMesh(Vector3 size, float b, float texMeters)
    {
        string key = size.x.ToString("F3") + "|" + size.y.ToString("F3") + "|" + size.z.ToString("F3")
                   + "|" + b.ToString("F3") + "|" + texMeters.ToString("F2");
        if (_chamfer.TryGetValue(key, out var cached)) return cached;

        float[] half = { size.x * 0.5f, size.y * 0.5f, size.z * 0.5f };
        b = Mathf.Min(b, Mathf.Min(half[0], Mathf.Min(half[1], half[2])) * 0.45f);
        float[] inner = { half[0] - b, half[1] - b, half[2] - b };
        if (texMeters < 0.01f) texMeters = 1f;

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        // 绕序不手写：按外向法线自动修正，写错也不会出现黑面
        void Poly(Vector3 n, Vector3[] v)
        {
            bool flip = Vector3.Dot(Vector3.Cross(v[1] - v[0], v[2] - v[0]), n) < 0f;
            int baseI = verts.Count;
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            for (int i = 0; i < v.Length; i++)
            {
                verts.Add(v[i]);
                norms.Add(n);
                // UV 按法线主轴投影：每个面的像素密度与 MeshFactory.BoxUV 一致，不会拉花
                Vector2 uv = (ax >= ay && ax >= az) ? new Vector2(v[i].z, v[i].y)
                           : (ay >= az) ? new Vector2(v[i].x, v[i].z)
                           : new Vector2(v[i].x, v[i].y);
                uvs.Add(uv / texMeters);
            }
            for (int i = 1; i + 1 < v.Length; i++)
            {
                if (!flip) { tris.Add(baseI); tris.Add(baseI + i); tris.Add(baseI + i + 1); }
                else { tris.Add(baseI); tris.Add(baseI + i + 1); tris.Add(baseI + i); }
            }
        }

        // 6 个主面：法线轴取 ±half，另两轴取 ±inner
        for (int a = 0; a < 3; a++)
        {
            int u = (a + 1) % 3, w = (a + 2) % 3;
            for (int s = -1; s <= 1; s += 2)
            {
                var n = Vector3.zero; n[a] = s;
                var q = new Vector3[4];
                int k = 0;
                foreach (var (cu, cw) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) })
                {
                    var v = Vector3.zero;
                    v[a] = s * half[a];
                    v[u] = cu * inner[u];
                    v[w] = cw * inner[w];
                    q[k++] = v;
                }
                Poly(n, q);
            }
        }

        // 12 条棱：45° 窄面（两端各接相邻两个主面的内缩边，必须排成凸四边形）
        for (int a = 0; a < 3; a++)
            for (int bb = a + 1; bb < 3; bb++)
            {
                int c = 3 - a - bb;
                for (int sa = -1; sa <= 1; sa += 2)
                    for (int sb = -1; sb <= 1; sb += 2)
                    {
                        var n = new Vector3(0f, 0f, 0f);
                        n[a] = sa; n[bb] = sb;
                        n.Normalize();
                        Vector3 EdgeP(int sc, bool onA)
                        {
                            var v = Vector3.zero;
                            v[c] = sc * inner[c];
                            v[a] = sa * (onA ? half[a] : inner[a]);
                            v[bb] = sb * (onA ? inner[bb] : half[bb]);
                            return v;
                        }
                        Poly(n, new[] { EdgeP(-1, true), EdgeP(-1, false), EdgeP(1, false), EdgeP(1, true) });
                    }
            }

        // 8 个角：三角形
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    var n = new Vector3(sx, sy, sz).normalized;
                    Poly(n, new[]
                    {
                        new Vector3(sx * half[0], sy * inner[1], sz * inner[2]),
                        new Vector3(sx * inner[0], sy * half[1], sz * inner[2]),
                        new Vector3(sx * inner[0], sy * inner[1], sz * half[2]),
                    });
                }

        var m = new Mesh();
        m.SetVertices(verts);
        m.SetNormals(norms);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        _chamfer[key] = m;
        return m;
    }

    // 倒角部件：位置/尺寸语义与 Part 完全一致（同一套坐标），只是网格换成倒角盒
    // 描边宽度按部件最小尺寸自动收（见 OutlineMat）：大块面粗描边，细窄件细描边，
    // 避免「小物件全用 Bold → 近景一片黑」（Lead 反馈，描边 18m 内满强度）。
    static GameObject PartC(Transform p, Vector3 pos, Vector3 size, Material mat, float bevel = 0.022f,
                            float texMeters = 1f, Vector3 euler = default)
    {
        var go = new GameObject("pc");
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.AddComponent<MeshFilter>().sharedMesh = ChamferMesh(size, bevel, texMeters);
        go.AddComponent<MeshRenderer>().sharedMaterial = OutlineAdjusted(mat, size);
        return go;
    }

    // 把传入材质的描边宽度按部件尺寸重算（缓存按 颜色+宽度 去重，不会爆材质数）。
    // 只处理「无贴图的纯色卡通材质」：带贴图的材质（墙纸/地砖/木纹）颜色是白色，
    // 按颜色重建会把贴图丢掉，所以原样返回。
    static Material OutlineAdjusted(Material mat, Vector3 size)
    {
        if (mat == null || mat.shader == null || mat.shader.name != "Campus/Toon") return mat;
        if (mat.mainTexture != null) return mat;
        float minDim = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
        string hex = "#" + ColorUtility.ToHtmlStringRGB(mat.color);
        return OutlineMat(hex, minDim);
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
        r.sharedMaterial = OutlineAdjusted(mat, size);
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
        // 卡通吸顶灯：发光盘 + 一圈外框（外框比灯盘大 6cm，描边把灯从天花上抠出来）
        Part(p, pos + new Vector3(0f, 0.14f, 0f), new Vector3(0.42f, 0.06f, 0.42f), C("#d8d0c0"), 1f);
        Part(p, pos + new Vector3(0f, 0.10f, 0f), new Vector3(0.36f, 0.05f, 0.36f), C("#fff6e2"), 1f);
        Part(p, pos + new Vector3(0f, 0.30f, 0f), new Vector3(0.05f, 0.24f, 0.05f), C("#8d8d94"), 1f);
    }

    static void Reg(Interior it, float cx, float cz, float w, float d)
        => it.Solids.Add(new Solid(cx - w * 0.5f, cz - d * 0.5f, cx + w * 0.5f, cz + d * 0.5f));

    static Material _glass;
    static Material Glass()
    {
        // 卡通玻璃：高明度冷青，靠描边勾窗格；不再用低饱和灰蓝（卡通光照下会发脏）
        if (_glass == null) _glass = MatLib.UnlitColor(MatLib.Hex("#cfeaf6"));
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
        var fr = CB("#c98a4e");
        PartC(g, new Vector3(0f, h * 0.5f, 0.02f), new Vector3(w + 0.16f, 0.10f, 0.10f), fr, 0.014f, 1f);
        PartC(g, new Vector3(0f, -h * 0.5f, 0.02f), new Vector3(w + 0.16f, 0.10f, 0.10f), fr, 0.014f, 1f);
        PartC(g, new Vector3(-w * 0.5f, 0f, 0.02f), new Vector3(0.10f, h, 0.10f), fr, 0.014f, 1f);
        PartC(g, new Vector3(w * 0.5f, 0f, 0.02f), new Vector3(0.10f, h, 0.10f), fr, 0.014f, 1f);
        PartC(g, new Vector3(0f, 0f, 0.02f), new Vector3(0.07f, h, 0.10f), fr, 0.012f, 1f);
        // 窗台：比框外凸 4cm，卡通渲染里这一条亮面把窗从墙上「托」出来
        PartC(g, new Vector3(0f, -h * 0.5f - 0.07f, 0.06f), new Vector3(w + 0.24f, 0.08f, 0.20f), C("#e8dcc4"), 0.016f, 1f);
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
        => Panel(p, x1, z1, x2, z2, y0, 0.12f, C("#7a5c42"));

    // 挂画：外框倒角 + 卡纸 + 两块色块。卡通渲染靠「框比画芯外凸」勾边，
    // 画芯颜色改成高明度低饱和（原来 #b9c8c0/#8a9e94 在卡通光照下会发灰）
    static void Picture(Transform p, Vector3 pos, float w, float h, float ry)
    {
        var g = new GameObject("pic").transform;
        g.SetParent(p, false);
        g.localPosition = pos;
        g.localEulerAngles = new Vector3(0f, ry, 0f);
        PartC(g, Vector3.zero, new Vector3(w + 0.10f, h + 0.10f, 0.05f), C("#8a6242"), 0.014f, 1f);
        Part(g, new Vector3(0f, 0f, 0.030f), new Vector3(w, h, 0.02f), C("#eef4ec"), 1f);
        Part(g, new Vector3(-w * 0.12f, h * 0.10f, 0.044f), new Vector3(w * 0.5f, h * 0.45f, 0.012f), C("#5f9e8a"), 1f);
        Part(g, new Vector3(w * 0.22f, -h * 0.16f, 0.046f), new Vector3(w * 0.32f, h * 0.30f, 0.012f), C("#c8b06a"), 1f);
        Part(g, new Vector3(w * 0.24f, h * 0.24f, 0.048f), new Vector3(w * 0.22f, h * 0.22f, 0.012f), C("#e08a6a"), 1f);
    }

    static void WallClock(Transform p, Vector3 pos, float ry)
    {
        var g = new GameObject("clock").transform;
        g.SetParent(p, false);
        g.localPosition = pos;
        g.localEulerAngles = new Vector3(0f, ry, 0f);
        PartC(g, Vector3.zero, new Vector3(0.44f, 0.44f, 0.05f), C("#fffdf5"), 0.020f, 1f);
        // 外圈：比表盘大 3cm，卡通描边把钟从墙纸上「抠」出来
        Part(g, new Vector3(0f, 0f, -0.012f), new Vector3(0.50f, 0.50f, 0.02f), C("#e0724a"), 1f);
        Part(g, new Vector3(0f, 0.07f, 0.032f), new Vector3(0.03f, 0.15f, 0.012f), C("#33384a"), 1f);
        Part(g, new Vector3(0.05f, 0f, 0.034f), new Vector3(0.11f, 0.03f, 0.012f), C("#33384a"), 1f);
        Part(g, new Vector3(0f, 0f, 0.036f), new Vector3(0.05f, 0.05f, 0.014f), C("#e0724a"), 1f);
    }

    static void Plant(Transform p, Vector3 pos, float s = 1f)
    {
        PartC(p, pos + new Vector3(0f, 0.22f * s, 0f), new Vector3(0.34f * s, 0.44f * s, 0.34f * s), C("#b8663c"), 0.030f, 1f);
        // 盆口压条
        Part(p, pos + new Vector3(0f, 0.42f * s, 0f), new Vector3(0.38f * s, 0.05f * s, 0.38f * s), C("#8f4a2c"), 1f);
        var go = new GameObject("leaf");
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos + new Vector3(0f, 0.62f * s, 0f);
        go.transform.localScale = Vector3.one * s;
        go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Canopy(0.40f, 10, 6, 0.34f, 3.1f);
        go.AddComponent<MeshRenderer>().sharedMaterial = CB("#3f9a48");
        // 第二层叶簇：错开高度与半径，卡通双色叶团比单球更有体积
        var go2 = new GameObject("leaf2");
        go2.transform.SetParent(p, false);
        go2.transform.localPosition = pos + new Vector3(0.06f * s, 0.80f * s, -0.04f * s);
        go2.transform.localScale = Vector3.one * s * 0.66f;
        go2.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Canopy(0.40f, 9, 5, 0.30f, 7.7f);
        go2.AddComponent<MeshRenderer>().sharedMaterial = CB("#5fc05a");
    }

    static void Curtain(Transform p, float cx, float y, float cz, float w, float h, Material cloth)
    {
        Part(p, new Vector3(cx - w * 0.5f + 0.20f, y + h * 0.5f, cz), new Vector3(0.34f, h, 0.07f), cloth, 1f);
        Part(p, new Vector3(cx + w * 0.5f - 0.20f, y + h * 0.5f, cz), new Vector3(0.34f, h, 0.07f), cloth, 1f);
        Part(p, new Vector3(cx, y + h + 0.06f, cz), new Vector3(w + 0.4f, 0.06f, 0.09f), C("#6f5540"), 1f);
    }

    static void Rug(Transform p, Vector3 pos, float w, float d, Material mat)
        => Part(p, pos, new Vector3(w, 0.02f, d), mat, 1f);

    // 椅子：靠背在局部 -Z，所以 ry=0 人朝 +Z。摆在桌子南侧取 0、北侧 180、西侧 90、东侧 270。
    // 卡通造型（docs/103）：坐面倒角 + 靠背分两块（下横档 + 上靠板）+ 后腿通到靠背顶，
    // 这样从任何角度都能看到至少三条独立轮廓线，而不是一个「盒子 + 一片板」。
    static void Chair(Transform p, Interior it, Vector3 seat, float ry, Material wood, bool solid = false)
    {
        var g = new GameObject("chair").transform;
        g.SetParent(p, false);
        g.localPosition = seat;
        g.localEulerAngles = new Vector3(0f, ry, 0f);
        var edge = Darker(wood, 0.80f);
        PartC(g, new Vector3(0f, 0.44f, 0f), new Vector3(0.46f, 0.06f, 0.46f), wood, 0.018f, 0.8f);
        // 坐面下的托框：比坐面内缩 3cm，给卡通描边留一条暗缝
        PartC(g, new Vector3(0f, 0.39f, 0f), new Vector3(0.40f, 0.04f, 0.40f), edge, 0.012f, 0.8f);
        // 靠背：下横档 + 上靠板（两块之间留 3cm 缝，避免共面）
        PartC(g, new Vector3(0f, 0.60f, -0.19f), new Vector3(0.42f, 0.10f, 0.06f), edge, 0.012f, 0.8f);
        PartC(g, new Vector3(0f, 0.80f, -0.19f), new Vector3(0.42f, 0.30f, 0.06f), wood, 0.016f, 0.8f);
        // 靠背顶压条：比靠板外凸 5mm，描边勾出椅背顶端
        PartC(g, new Vector3(0f, 0.955f, -0.19f), new Vector3(0.44f, 0.04f, 0.07f), edge, 0.010f, 0.8f);
        for (int i = 0; i < 4; i++)
        {
            bool rear = i >= 2;
            Part(g, new Vector3(i % 2 == 0 ? -0.19f : 0.19f, rear ? 0.28f : 0.22f, i < 2 ? -0.19f : 0.19f),
                 new Vector3(0.05f, rear ? 0.56f : 0.44f, 0.05f), edge, 0.6f);
        }
        if (solid) Reg(it, seat.x, seat.z, 0.56f, 0.56f);
    }

    // 桌子：台面倒角 + 台面下托边（比台面内缩 2cm）+ 横撑，腿顶伸进台面 4cm
    static void Table(Transform p, Vector3 basePos, float w, float d, float h, Material wood)
    {
        var edge = Darker(wood, 0.80f);
        PartC(p, new Vector3(basePos.x, basePos.y + h - 0.04f, basePos.z),
              new Vector3(w, 0.08f, d), wood, 0.020f, 1.1f);
        // 托边：台面正下方一圈，卡通渲染里这圈暗面把台面「压」在家具上
        PartC(p, new Vector3(basePos.x, basePos.y + h - 0.10f, basePos.z),
              new Vector3(w - 0.10f, 0.06f, d - 0.10f), edge, 0.014f, 1.0f);
        for (int i = 0; i < 4; i++)
            Part(p, new Vector3(basePos.x + (i % 2 == 0 ? -w * 0.5f + 0.12f : w * 0.5f - 0.12f),
                                basePos.y + (h - 0.04f) * 0.5f,
                                basePos.z + (i < 2 ? -d * 0.5f + 0.12f : d * 0.5f - 0.12f)),
                 new Vector3(0.08f, h - 0.04f, 0.08f), wood, 0.7f);   // 腿顶伸进台面 4cm，不与板底共面
        // 横撑：长边两道，让桌腿不是四条孤立柱子
        foreach (int s in new[] { -1, 1 })
            Part(p, new Vector3(basePos.x, basePos.y + 0.16f, basePos.z + s * (d * 0.5f - 0.12f)),
                 new Vector3(w - 0.30f, 0.05f, 0.05f), edge, 0.7f);
    }

    // 靠东墙的书架：书脊朝 -x，两段书架之间正好夹住窗户
    static void ShelfRun(Transform p, Interior it, float x, float z0, float z1,
                         Material frame, Material[] books)
    {
        const float h = 2.10f, d = 0.50f;
        float len = z1 - z0, cz = (z0 + z1) * 0.5f;
        float face = x - d * 0.5f;
        var trim = Darker(frame, 0.76f);
        // 背板 + 顶板 + 底板 + 隔板全部换成倒角件：卡通描边能把每层格子分开
        PartC(p, new Vector3(x + d * 0.5f - 0.02f, h * 0.5f, cz), new Vector3(0.04f, h, len), frame, 0.010f, 1f);   // 背板
        PartC(p, new Vector3(x, 0.06f, z0 + 0.03f), new Vector3(d, 0.12f, 0.06f), frame, 0.010f, 0.7f);
        PartC(p, new Vector3(x, 0.06f, z1 - 0.03f), new Vector3(d, 0.12f, 0.06f), frame, 0.010f, 0.7f);
        PartC(p, new Vector3(x, h - 0.03f, cz), new Vector3(d, 0.06f, len), frame, 0.012f, 1f);                    // 顶板
        PartC(p, new Vector3(x, 0.06f, cz), new Vector3(d, 0.12f, len), frame, 0.012f, 1f);                        // 底板
        // 顶部压条：比顶板外凸 1cm，书架顶沿在卡通渲染里有一条独立亮线
        Part(p, new Vector3(x, h + 0.005f, cz), new Vector3(d + 0.02f, 0.03f, len + 0.02f), trim, 1f);
        for (int r = 1; r <= 3; r++)
            PartC(p, new Vector3(x, 0.12f + r * 0.48f, cz), new Vector3(d - 0.06f, 0.04f, len - 0.12f), trim, 0.008f, 1f);

        int n = Mathf.FloorToInt((len - 0.24f) / 0.17f);
        for (int r = 0; r < 4; r++)
        {
            float y = r == 0 ? 0.12f : 0.12f + r * 0.48f + 0.02f;
            for (int i = 0; i < n; i++)
            {
                if ((i + r * 5) % 11 == 7) continue;          // 偶尔留个空档
                float bw = 0.10f + ((i * 7 + r * 3) % 4) * 0.014f;
                float bh = 0.28f + ((i * 5 + r) % 4) * 0.022f;
                float bz = z1 - 0.14f - i * 0.17f - bw * 0.5f;
                Part(p, new Vector3(face + 0.14f, y + bh * 0.5f + 0.002f, bz),
                     new Vector3(0.24f, bh, bw), books[(i + r * 2) % books.Length], 0.5f);
            }
        }
        Reg(it, x, cz, d, len);
    }

    static Font _signFont;
    // 门牌文字：动态字体 + TextMesh（只用于室内门牌，不参与碰撞）
    static void SignText(Transform p, Vector3 pos, float ry, string s, float h, Color color)
    {
        if (_signFont == null)
            _signFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "微软雅黑", "SimHei", "Arial" }, 72);
        var go = new GameObject("signtext");
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = new Vector3(0f, ry, 0f);
        var tm = go.AddComponent<TextMesh>();
        tm.text = s;
        tm.font = _signFont;
        tm.fontSize = 72;
        tm.characterSize = h;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        var mr = go.GetComponent<MeshRenderer>();
        var signShader = Resources.Load<Shader>("Shaders/TextSign");
        if (signShader == null) signShader = Shader.Find("Campus/TextSign");
        if (signShader != null)
        {
            var mat = new Material(signShader);
            mat.mainTexture = _signFont.material.mainTexture;
            mat.SetFloat("_Cutoff", 0.42f);
            if (mr != null) mr.sharedMaterial = mat;
            go.AddComponent<SignAtlasSync>().font = _signFont;
        }
        else
        {
            // 兜底：着色器没进包时退回字体材质（可见，但会穿墙）
            Debug.LogWarning("[InteriorBuilder] TextSign 着色器缺失，门牌会穿透墙体");
            if (mr != null) mr.sharedMaterial = _signFont.material;
        }
    }

    // 沙发：各面深度互相错开 ≥2cm —— 同材质的共平面（坐垫/扶手/靠背原来同一圈坐标）会闪面
    // 卡通造型：底座倒角 + 坐垫分两段（中间留 4cm 缝）+ 靠背三块靠枕 + 扶手圆角，
    // 远看是「大块面」，近看每条缝都是一条硬描边。
    static void Sofa(Transform p, Vector3 pos, float len, float ry, Material fabric)
    {
        var g = new GameObject("sofa").transform;
        g.SetParent(p, false);
        g.localPosition = pos;
        g.localEulerAngles = new Vector3(0f, ry, 0f);
        var dark = Darker(fabric, 0.74f);
        PartC(g, new Vector3(0f, 0.11f, 0f), new Vector3(len - 0.16f, 0.22f, 0.78f), CB("#8a5a34"), 0.020f, 1f);  // 底座（内缩）
        // 坐垫：按长度切 2~3 段，段间留 4cm 缝（卡通分件的核心手法）
        int seg = len > 2.2f ? 3 : 2;
        float segW = (len - 0.28f - (seg - 1) * 0.04f) / seg;
        for (int i = 0; i < seg; i++)
        {
            float cx2 = (i - (seg - 1) * 0.5f) * (segW + 0.04f);
            PartC(g, new Vector3(cx2, 0.42f, 0.02f), new Vector3(segW, 0.44f, 0.86f), fabric, 0.028f, 1.2f);
            // 坐垫前沿压条：比坐垫前凸 1cm，勾出座面轮廓
            Part(g, new Vector3(cx2, 0.30f, 0.45f), new Vector3(segW - 0.02f, 0.10f, 0.02f), dark, 1.2f);
        }
        PartC(g, new Vector3(0f, 0.70f, -0.36f), new Vector3(len - 0.24f, 0.86f, 0.16f), fabric, 0.026f, 1.2f);  // 靠背
        // 靠枕：两块，比靠背前凸 6cm（原来靠背是一整片，正面看没有任何分层）
        for (int i = 0; i < 2; i++)
            PartC(g, new Vector3((i - 0.5f) * (len * 0.42f), 0.78f, -0.26f),
                  new Vector3(len * 0.36f, 0.44f, 0.14f), dark, 0.024f, 1.2f);
        foreach (int s in new[] { -1, 1 })
        {
            PartC(g, new Vector3(s * (len * 0.5f - 0.1f), 0.62f, 0f), new Vector3(0.2f, 0.62f, 0.94f), fabric, 0.030f, 1.2f);
            // 扶手顶压条
            Part(g, new Vector3(s * (len * 0.5f - 0.1f), 0.94f, 0f), new Vector3(0.22f, 0.04f, 0.90f), dark, 1.2f);
        }
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
        var list = new List<Interior> { Home(parent), School(parent), Library(parent), Restaurant(parent), Supermarket(parent), Canteen(parent), Dorm(parent), Auditorium(parent), Labs(parent),
            MilkTea(parent), PhotoStudio(parent), Bookstore(parent), ConvStore(parent) };
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
            if (it.HasClear)
            {
                float ox2 = Mathf.Min(s.X2, it.ClearX2) - Mathf.Max(s.X1, it.ClearX1);
                float oz2 = Mathf.Min(s.Z2, it.ClearZ2) - Mathf.Max(s.Z1, it.ClearZ1);
                if (ox2 > 0.01f && oz2 > 0.01f)
                {
                    Debug.LogWarning($"[Interior:{it.Id}] 出口遮挡 x[{s.X1:F2},{s.X2:F2}] z[{s.Z1:F2},{s.Z2:F2}]");
                    bad++;
                }
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
        var wood = InteriorWood();
        // 卡通配色（docs/103）：高饱和 + 高明度，暗面统一交给 Toon.shader 的 _ShadowTint 压。
        // 原来的 #6f5540（深褐）/ #7d8a76（灰绿）/ #8f8571（灰褐）在 cel shading 下会糊成一片脏灰。
        var woodDark = CB("#a06a3c");       // 原 #6f5540 → 明快胡桃
        var fabric = CB("#6fc0a8");         // 原 #7d8a76 → 薄荷青（沙发/书房）
        var fabric2 = C("#f2b95e");         // 原 #8f8571 → 明黄（地毯）
        var white = C("#fbf7ec");
        var metal = C("#b8c4d0");
        var screen = C("#2b2f3a");
        var cloth = C("#f5d78a");

        // ── 地面 ──
        Part(p, new Vector3(ox, 0f, 0f), new Vector3(30f, 0.04f, 20f), floor, 2f, default, true);
        Part(p, new Vector3(ox + 9.5f, 0.05f, -5f), new Vector3(11f, 0.02f, 10f), tile, 1.4f);
        Part(p, new Vector3(ox + 0.6f, 0.05f, 6f), new Vector3(5.4f, 0.02f, 8f), tile, 1.4f);
        Part(p, new Vector3(ox + 9.2f, 0.05f, 6f), new Vector3(11.6f, 0.02f, 8f), tile, 1.4f);

        // ── 天花 ──
        CeilingSlab(p, ox, W, 30f, 20f, ceil);

        // ── 外墙 ──
        WallX(p, it, ox - 15f, ox + 15f, -10f, 0f, W, ox - 1.2f, ox + 1.2f, 2.6f, wall);
        WallX(p, it, ox - 15f, ox + 15f, 10f, 0f, W, 0f, 0f, 2.6f, wall);
        WallZ(p, it, -10f, 10f, ox - 15f, 0f, W, 0f, 0f, 2.6f, wall);
        WallZ(p, it, -10f, 10f, ox + 15f, 0f, W, 0f, 0f, 2.6f, wall);

        // ── 内墙（仅 4 道；门洞 1.8m，1.2m 在 4m 层高下像一条缝）──
        WallX(p, it, ox - 15f, ox - 13.1f, 2f, 0.02f, W, 0f, 0f, 2.4f, wall, WALL_IN);
        WallX(p, it, ox - 11.3f, ox - 6.7f, 2f, 0.02f, W, 0f, 0f, 2.4f, wall, WALL_IN);
        WallX(p, it, ox - 4.9f, ox - 0.3f, 2f, 0.02f, W, 0f, 0f, 2.4f, wall, WALL_IN);
        WallX(p, it, ox + 1.5f, ox + 5.1f, 2f, 0.02f, W, 0f, 0f, 2.4f, wall, WALL_IN);
        WallX(p, it, ox + 6.9f, ox + 15f, 2f, 0.02f, W, 0f, 0f, 2.4f, wall, WALL_IN);
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

        // ── 出口：门扇 + 门廊 ──
        Porch(p, ox, -10f, 3.6f, -1f, 2.9f);
        DoorFrame(p, it, ox, -10f, 1.2f, 2.6f, 1);

        // ── 墙纸（外墙内表面全贴；内墙两面按房间贴）──
        WallDeco(p, ox - 14.86f, ox - 1.2f, -9.86f, 0.02f, W, 0, 0.05f, wallA, 1.7f);
        WallDeco(p, ox + 1.2f, ox + 14.86f, -9.86f, 0.02f, W, 0, 0.05f, wallT, 1.7f);
        WallDeco(p, -9.86f, 9.86f, ox - 14.86f, 0.02f, W, 1, 0.05f, wallA, 1.7f);
        WallDeco(p, -9.86f, 9.86f, ox + 14.86f, 0.02f, W, 1, 0.05f, wallT, 1.7f);
        WallDeco(p, ox - 14.86f, ox + 14.86f, 9.86f, 0.02f, W, 0, 0.05f, wallC, 1.7f);
        WallDeco(p, ox - 14.86f, ox + 14.86f, 1.89f, 0.02f, W, 0, 0.05f, wallA, 1.7f,
                 new[] { (ox - 13.28f, ox - 11.12f), (ox - 6.88f, ox - 4.72f), (ox - 0.48f, ox + 1.68f), (ox + 4.92f, ox + 7.08f) });
        WallDeco(p, ox - 14.86f, ox - 9.4f, 2.11f, 0.02f, W, 0, 0.05f, wallB, 1.7f,
                 new[] { (ox - 13.28f, ox - 11.12f) });
        WallDeco(p, ox - 9.4f, ox - 2.2f, 2.11f, 0.02f, W, 0, 0.05f, wallB, 1.7f,
                 new[] { (ox - 6.88f, ox - 4.72f) });
        WallDeco(p, ox - 2.2f, ox + 3.4f, 2.11f, 0.02f, W, 0, 0.05f, wallT, 1.7f,
                 new[] { (ox - 0.48f, ox + 1.68f) });
        WallDeco(p, ox + 3.4f, ox + 14.86f, 2.11f, 0.02f, W, 0, 0.05f, wallA, 1.7f,
                 new[] { (ox + 4.92f, ox + 7.08f) });
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
        // 电视柜：倒角柜体 + 台面压条 + 两扇柜门（卡通分件）
        PartC(p, new Vector3(ox - 14.5f, 0.36f, -4.0f), new Vector3(0.5f, 0.72f, 2.8f), woodDark, 0.024f, 1f);
        PartC(p, new Vector3(ox - 14.5f, 0.75f, -4.0f), new Vector3(0.56f, 0.06f, 2.86f), C("#d8b07a"), 0.014f, 1f);
        foreach (float gz in new[] { -5.05f, -4.3f, -3.55f, -2.8f })
            Part(p, new Vector3(ox - 14.24f, 0.36f, gz), new Vector3(0.03f, 0.56f, 0.66f), C("#8a5a34"), 1f);
        Sofa(p, new Vector3(ox - 11.3f, 0f, -4.0f), 3.2f, 270f, fabric);
        // 茶几：倒角 + 下层搁板（原来是一整块 0.7×0.46×1.3 的实心块）
        PartC(p, new Vector3(ox - 12.9f, 0.24f, -4.0f), new Vector3(0.7f, 0.46f, 1.3f), woodDark, 0.026f, 1f);
        PartC(p, new Vector3(ox - 12.9f, 0.48f, -4.0f), new Vector3(0.76f, 0.05f, 1.36f), C("#d8b07a"), 0.012f, 1f);
        Part(p, new Vector3(ox - 12.9f, 0.16f, -4.0f), new Vector3(0.56f, 0.04f, 1.1f), C("#8a5a34"), 1f);
        Part(p, new Vector3(ox - 12.86f, 0.60f, -4.0f), new Vector3(0.30f, 0.22f, 0.30f), C("#e05c5c"), 1f);
        // 单人扶手椅：靠背 + 两侧扶手 + 坐垫分开，别做成一整块
        PartC(p, new Vector3(ox - 9.0f, 0.42f, -4.0f), new Vector3(0.95f, 0.85f, 1.5f), fabric, 0.034f, 1.2f);
        PartC(p, new Vector3(ox - 8.9f, 0.92f, -4.0f), new Vector3(0.22f, 0.62f, 1.42f), Darker(fabric, 0.82f), 0.026f, 1.2f);
        Part(p, new Vector3(ox - 9.0f, 0.30f, -4.0f), new Vector3(1.02f, 0.16f, 1.58f), C("#5a4632"), 1f);
        // 书架：倒角柜体 + 三层隔板 + 书脊（原来是一整块 2.4×2.0×0.4 的木板）
        PartC(p, new Vector3(ox - 8.6f, 1.0f, 1.62f), new Vector3(2.4f, 2.0f, 0.4f), woodDark, 0.028f, 1f);
        for (int r = 0; r < 4; r++)
            Part(p, new Vector3(ox - 8.6f, 0.30f + r * 0.52f, 1.42f), new Vector3(2.24f, 0.05f, 0.05f), C("#d8b07a"), 1f);
        for (int r = 0; r < 4; r++)
            for (int i = 0; i < 6; i++)
                Part(p, new Vector3(ox - 9.7f + i * 0.42f, 0.42f + r * 0.52f, 1.44f),
                     new Vector3(0.30f, 0.34f, 0.16f),
                     C(new[] { "#e0554a", "#4a9ac8", "#f0b03c", "#6cc06a", "#a86ad8", "#e08a4a" }[(i + r * 2) % 6]), 0.4f);
        Lamp(p, new Vector3(ox - 11.3f, 3.55f, -4.0f), 11f, 1.3f);
        Part(p, new Vector3(ox - 14.3f, 1.1f, -1.2f), new Vector3(0.07f, 2.0f, 0.07f), metal, 1f);
        Part(p, new Vector3(ox - 14.3f, 2.15f, -1.2f), new Vector3(0.36f, 0.3f, 0.36f), C("#fff2d6"), 1f);
        Part(p, new Vector3(ox - 14.3f, 2.33f, -1.2f), new Vector3(0.42f, 0.06f, 0.42f), C("#e0a860"), 1f);

        // 玄关
        PartC(p, new Vector3(ox + 2.9f, 0.55f, -9.0f), new Vector3(0.45f, 1.1f, 1.6f), woodDark, 0.022f, 1f);
        PartC(p, new Vector3(ox + 2.9f, 1.12f, -9.0f), new Vector3(0.52f, 0.06f, 1.66f), C("#d8b07a"), 0.012f, 1f);
        PartC(p, new Vector3(ox - 2.6f, 0.22f, -9.25f), new Vector3(0.9f, 0.44f, 0.4f), wood, 0.022f, 1f);
        // 玄关镜：单层开放布局拆掉了原来的玄关隔墙，镜子不能留在原地悬空 → 改贴北墙（鞋柜台面上方）
        PartC(p, new Vector3(ox + 2.9f, 1.95f, -9.82f), new Vector3(1.0f, 1.5f, 0.04f), CB("#c98a4e"), 0.016f, 1f);
        Part(p, new Vector3(ox + 2.9f, 1.95f, -9.78f), new Vector3(0.86f, 1.36f, 0.03f), C("#b8dcf0"), 1f);
        Rug(p, new Vector3(ox, 0.06f, -8.9f), 1.8f, 1.2f, C("#f08a6a"));
        Lamp(p, new Vector3(ox, 3.55f, -8.6f), 8f, 1.0f);

        // 餐厅（中）
        Table(p, new Vector3(ox, 0f, -4.2f), 2.4f, 1.1f, 0.76f, wood);
        Chair(p, it, new Vector3(ox, 0f, -5.05f), 0f, wood, true);      // 南侧，人朝北
        Chair(p, it, new Vector3(ox, 0f, -3.35f), 180f, wood, true);    // 北侧，人朝南
        Chair(p, it, new Vector3(ox - 1.55f, 0f, -4.2f), 90f, wood, true);   // 西侧，人朝东
        Chair(p, it, new Vector3(ox + 1.55f, 0f, -4.2f), 270f, wood, true);  // 东侧，人朝西
        // 餐桌上：花瓶 + 果盘（空桌子在卡通渲染里就是一块平板）
        Part(p, new Vector3(ox, 0.86f, -4.2f), new Vector3(0.20f, 0.24f, 0.20f), C("#e0554a"), 0.4f);
        Part(p, new Vector3(ox, 1.04f, -4.2f), new Vector3(0.30f, 0.18f, 0.30f), C("#f0b03c"), 0.4f);
        Part(p, new Vector3(ox + 0.55f, 0.82f, -4.2f), new Vector3(0.34f, 0.10f, 0.34f), C("#e8f0f4"), 0.4f);
        Lamp(p, new Vector3(ox, 3.5f, -4.2f), 9f, 1.1f);

        // 厨房（东，x 4..15）：L 台面 + 中岛
        // 柜体倒角 + 台面比柜体外凸 3cm（原来柜体与台面同宽，卡通描边会糊成一片）
        PartC(p, new Vector3(ox + 9.5f, 0.49f, -9.35f), new Vector3(9.0f, 0.98f, 0.66f), white, 0.020f, 1f);
        PartC(p, new Vector3(ox + 9.5f, 1.0f, -9.35f), new Vector3(9.06f, 0.06f, 0.74f), C("#8fb0c8"), 0.014f, 1f);
        PartC(p, new Vector3(ox + 14.45f, 0.49f, -5.2f), new Vector3(0.66f, 0.98f, 6.4f), white, 0.020f, 1f);
        PartC(p, new Vector3(ox + 14.45f, 1.0f, -5.2f), new Vector3(0.74f, 0.06f, 6.46f), C("#8fb0c8"), 0.014f, 1f);
        // 柜门缝：每 1.1m 一道竖缝，厨房台面不再是整片白板
        for (int i = 0; i < 8; i++)
            Part(p, new Vector3(ox + 5.6f + i * 1.12f, 0.52f, -9.02f), new Vector3(0.03f, 0.86f, 0.02f), C("#c8d4dc"), 1f);
        for (int i = 0; i < 5; i++)
            Part(p, new Vector3(ox + 14.10f, 0.52f, -7.8f + i * 1.28f), new Vector3(0.02f, 0.86f, 0.03f), C("#c8d4dc"), 1f);
        Part(p, new Vector3(ox + 7.0f, 1.02f, -9.3f), new Vector3(0.8f, 0.05f, 0.55f), C("#a8b4bc"), 1f);
        Part(p, new Vector3(ox + 11.0f, 1.02f, -9.3f), new Vector3(0.9f, 0.05f, 0.55f), C("#3a4250"), 1f);
        // 冰箱：倒角 + 门缝 + 把手
        PartC(p, new Vector3(ox + 5.2f, 1.1f, -9.15f), new Vector3(0.95f, 2.2f, 0.75f), CB("#a8c8dc"), 0.028f, 1f);
        Part(p, new Vector3(ox + 5.2f, 1.62f, -8.76f), new Vector3(0.90f, 0.03f, 0.03f), C("#6a8aa0"), 1f);
        Part(p, new Vector3(ox + 5.2f, 1.20f, -8.74f), new Vector3(0.10f, 0.40f, 0.04f), C("#e8f2f8"), 1f);
        PartC(p, new Vector3(ox + 9.5f, 2.55f, -9.58f), new Vector3(8.0f, 0.8f, 0.38f), white, 0.018f, 1f);
        for (int i = 0; i < 6; i++)
            Part(p, new Vector3(ox + 6.6f + i * 1.15f, 2.55f, -9.38f), new Vector3(0.04f, 0.74f, 0.02f), C("#c8d4dc"), 1f);
        PartC(p, new Vector3(ox + 9.5f, 0.49f, -3.2f), new Vector3(5.6f, 0.98f, 1.25f), white, 0.022f, 1f);
        PartC(p, new Vector3(ox + 9.5f, 1.0f, -3.2f), new Vector3(5.66f, 0.06f, 1.32f), C("#8fb0c8"), 0.014f, 1f);
        for (int i = 0; i < 3; i++)
            PartC(p, new Vector3(ox + 7.9f + i * 1.6f, 0.36f, -1.9f), new Vector3(0.42f, 0.72f, 0.42f),
                  C(new[] { "#e0554a", "#4a9ac8", "#f0b03c" }[i]), 0.020f, 0.8f);
        Lamp(p, new Vector3(ox + 9.5f, 3.55f, -4f), 11f, 1.15f);

        // 主卧（x -15..-9.4, z 2..10）
        // 床：床头板 + 床架 + 床垫 + 被褥 + 枕头，五层分件（原来只有床架 + 一块床垫 + 一条被子）
        PartC(p, new Vector3(ox - 13.7f, 0.25f, 6.0f), new Vector3(2.2f, 0.5f, 2.0f), woodDark, 0.028f, 1f);
        PartC(p, new Vector3(ox - 13.7f, 0.58f, 6.0f), new Vector3(2.1f, 0.26f, 1.9f), C("#fbf7ec"), 0.026f, 1f);
        PartC(p, new Vector3(ox - 13.7f, 0.74f, 6.9f), new Vector3(1.9f, 0.14f, 0.85f), C("#7ab0e0"), 0.022f, 1f);  // 被子
        PartC(p, new Vector3(ox - 13.7f, 0.76f, 5.15f), new Vector3(1.2f, 0.15f, 0.4f), C("#ffd88a"), 0.020f, 1f);   // 枕头
        PartC(p, new Vector3(ox - 14.7f, 0.78f, 6.0f), new Vector3(0.12f, 1.0f, 2.0f), woodDark, 0.020f, 1f);         // 床头板
        PartC(p, new Vector3(ox - 13.7f, 0.28f, 4.35f), new Vector3(0.55f, 0.56f, 0.55f), C("#e08a4a"), 0.024f, 1f); // 床头柜
        PartC(p, new Vector3(ox - 13.7f, 0.28f, 7.65f), new Vector3(0.55f, 0.56f, 0.55f), C("#e08a4a"), 0.024f, 1f);
        Part(p, new Vector3(ox - 13.7f, 0.60f, 4.35f), new Vector3(0.60f, 0.05f, 0.60f), C("#f2b95e"), 1f);
        Part(p, new Vector3(ox - 13.7f, 0.60f, 7.65f), new Vector3(0.60f, 0.05f, 0.60f), C("#f2b95e"), 1f);
        // 衣柜：倒角柜体 + 门缝 + 把手（原来是一整块 0.65×2.5×2.4 的板）
        PartC(p, new Vector3(ox - 10.0f, 1.25f, 5.0f), new Vector3(0.65f, 2.5f, 2.4f), woodDark, 0.030f, 1f);
        Part(p, new Vector3(ox - 9.66f, 1.25f, 5.0f), new Vector3(0.03f, 2.40f, 0.04f), C("#8a5a34"), 1f);
        foreach (float hz in new[] { 4.35f, 5.65f })
            Part(p, new Vector3(ox - 9.65f, 1.20f, hz), new Vector3(0.04f, 0.34f, 0.05f), C("#f0d8a8"), 1f);
        Rug(p, new Vector3(ox - 11.6f, 0.04f, 6.0f), 2.6f, 3.4f, fabric2);
        Lamp(p, new Vector3(ox - 12f, 3.55f, 6.0f), 10f, 1.15f);

        // 书房（x -9.4..-2.2）
        PartC(p, new Vector3(ox - 5.8f, 0.85f, 9.2f), new Vector3(2.2f, 0.06f, 0.8f), wood, 0.016f, 1f);
        PartC(p, new Vector3(ox - 5.8f, 0.4f, 9.2f), new Vector3(2.1f, 0.8f, 0.7f), woodDark, 0.024f, 1f);
        Part(p, new Vector3(ox - 5.8f, 0.46f, 8.86f), new Vector3(1.9f, 0.06f, 0.03f), C("#d8b07a"), 1f);
        Chair(p, it, new Vector3(ox - 5.8f, 0f, 8.25f), 0f, wood, true);   // 书桌贴北墙，人朝北坐
        // 书架：倒角 + 隔板 + 书脊
        PartC(p, new Vector3(ox - 8.95f, 1.1f, 5.5f), new Vector3(0.4f, 2.2f, 2.6f), woodDark, 0.026f, 1f);
        for (int r = 0; r < 4; r++)
            Part(p, new Vector3(ox - 8.76f, 0.32f + r * 0.54f, 5.5f), new Vector3(0.04f, 0.05f, 2.4f), C("#d8b07a"), 1f);
        for (int r = 0; r < 4; r++)
            for (int i = 0; i < 7; i++)
                Part(p, new Vector3(ox - 8.74f, 0.46f + r * 0.54f, 4.5f + i * 0.33f),
                     new Vector3(0.14f, 0.32f, 0.26f),
                     C(new[] { "#e0554a", "#4a9ac8", "#f0b03c", "#6cc06a", "#a86ad8", "#e08a4a" }[(i + r * 3) % 6]), 0.4f);
        // 书桌旁的单人沙发 + 靠背
        PartC(p, new Vector3(ox - 3.4f, 0.48f, 4.6f), new Vector3(1.1f, 0.5f, 1.0f), fabric, 0.030f, 1.2f);
        PartC(p, new Vector3(ox - 3.4f, 0.72f, 5.1f), new Vector3(1.1f, 0.95f, 0.2f), Darker(fabric, 0.82f), 0.026f, 1.2f);
        Lamp(p, new Vector3(ox - 5.8f, 3.55f, 6.5f), 9f, 1.1f);

        // 卫生间（x -2.2..3.4）
        PartC(p, new Vector3(ox + 0.6f, 0.34f, 9.2f), new Vector3(2.2f, 0.68f, 1.0f), white, 0.022f, 1f);
        PartC(p, new Vector3(ox + 0.6f, 0.6f, 9.2f), new Vector3(2.0f, 0.22f, 0.84f), C("#8fd0e8"), 0.020f, 1f);
        PartC(p, new Vector3(ox - 1.65f, 0.47f, 4.5f), new Vector3(0.6f, 0.94f, 1.15f), white, 0.022f, 1f);
        Part(p, new Vector3(ox - 1.65f, 0.96f, 4.5f), new Vector3(0.65f, 0.07f, 1.2f), C("#c8d8e4"), 1f);
        PartC(p, new Vector3(ox + 2.8f, 0.4f, 4.5f), new Vector3(0.5f, 0.8f, 0.75f), white, 0.022f, 1f);
        Part(p, new Vector3(ox + 2.8f, 0.88f, 4.5f), new Vector3(0.55f, 0.07f, 0.8f), C("#c8d8e4"), 1f);
        Lamp(p, new Vector3(ox + 0.6f, 3.55f, 6.5f), 7f, 0.95f);

        // 家务间 / 洗衣房（x 3.4..15）
        // 洗衣机与洗衣池贴北墙一字排开、进门正对；原来摆在 z=3.5 房间中央，从门口看像一间空房
        foreach (float dx in new[] { 4.35f, 5.25f })
        {
            PartC(p, new Vector3(ox + dx, 0.55f, 9.45f), new Vector3(0.72f, 0.98f, 0.72f), CB("#e8f0f4"), 0.024f, 1f);
            Part(p, new Vector3(ox + dx, 0.55f, 9.08f), new Vector3(0.46f, 0.46f, 0.03f), C("#5a8ac0"), 1f);
            Part(p, new Vector3(ox + dx, 0.88f, 9.08f), new Vector3(0.6f, 0.12f, 0.03f), C("#a8c0d8"), 1f);
            Part(p, new Vector3(ox + dx, 1.2f, 9.3f), new Vector3(0.1f, 0.28f, 0.1f), C("#8fd0e8"), 1f);
        }
        PartC(p, new Vector3(ox + 6.7f, 0.55f, 9.45f), new Vector3(1.6f, 0.98f, 0.72f), white, 0.024f, 1f);
        Part(p, new Vector3(ox + 6.7f, 1.04f, 9.45f), new Vector3(1.64f, 0.06f, 0.76f), C("#c8d8e4"), 1f);
        Part(p, new Vector3(ox + 6.7f, 1.08f, 9.45f), new Vector3(1.1f, 0.03f, 0.5f), C("#a8b4bc"), 1f);
        Part(p, new Vector3(ox + 6.7f, 1.22f, 9.7f), new Vector3(0.06f, 0.3f, 0.06f), metal, 1f);
        Part(p, new Vector3(ox + 6.7f, 1.36f, 9.63f), new Vector3(0.06f, 0.06f, 0.2f), metal, 1f);
        PartC(p, new Vector3(ox + 4.8f, 2.55f, 9.66f), new Vector3(2.1f, 0.7f, 0.35f), white, 0.020f, 1f);
        Part(p, new Vector3(ox + 4.8f, 2.55f, 9.47f), new Vector3(2.14f, 0.74f, 0.02f), C("#c8d8e4"), 1f);
        Part(p, new Vector3(ox + 4.8f, 2.55f, 9.455f), new Vector3(0.02f, 0.7f, 0.01f), C("#a8a69c"), 1f);
        Part(p, new Vector3(ox + 4.15f, 2.48f, 9.44f), new Vector3(0.05f, 0.12f, 0.03f), metal, 1f);
        Part(p, new Vector3(ox + 5.45f, 2.48f, 9.44f), new Vector3(0.05f, 0.12f, 0.03f), metal, 1f);
        Part(p, new Vector3(ox + 6.7f, 2.1f, 9.67f), new Vector3(1.4f, 0.06f, 0.3f), wood, 1f);
        Part(p, new Vector3(ox + 6.25f, 2.24f, 9.67f), new Vector3(0.16f, 0.22f, 0.14f), C("#4a9ac8"), 1f);
        Part(p, new Vector3(ox + 6.7f, 2.24f, 9.67f), new Vector3(0.16f, 0.22f, 0.14f), C("#e08a4a"), 1f);
        Part(p, new Vector3(ox + 7.15f, 2.24f, 9.67f), new Vector3(0.16f, 0.22f, 0.14f), C("#6cc06a"), 1f);
        // 晾衣架 + 衣服
        Part(p, new Vector3(ox + 11.4f, 1.95f, 6.4f), new Vector3(2.0f, 0.05f, 0.05f), metal, 1f);
        Part(p, new Vector3(ox + 10.5f, 1.0f, 6.4f), new Vector3(0.06f, 1.9f, 0.06f), metal, 1f);
        Part(p, new Vector3(ox + 12.3f, 1.0f, 6.4f), new Vector3(0.06f, 1.9f, 0.06f), metal, 1f);
        Part(p, new Vector3(ox + 10.5f, 0.05f, 6.4f), new Vector3(0.1f, 0.06f, 0.7f), metal, 1f);
        Part(p, new Vector3(ox + 12.3f, 0.05f, 6.4f), new Vector3(0.1f, 0.06f, 0.7f), metal, 1f);
        Part(p, new Vector3(ox + 10.75f, 1.47f, 6.4f), new Vector3(0.36f, 0.9f, 0.03f), C("#6cc06a"), 1f);
        Part(p, new Vector3(ox + 11.2f, 1.47f, 6.4f), new Vector3(0.36f, 0.9f, 0.03f), C("#e08a4a"), 1f);
        Part(p, new Vector3(ox + 11.65f, 1.47f, 6.4f), new Vector3(0.36f, 0.9f, 0.03f), C("#4a9ac8"), 1f);
        Part(p, new Vector3(ox + 12.1f, 1.47f, 6.4f), new Vector3(0.36f, 0.9f, 0.03f), C("#f5d78a"), 1f);
        // 熨衣板 + 熨斗
        Part(p, new Vector3(ox + 4.15f, 0.82f, 6.6f), new Vector3(0.45f, 0.06f, 1.4f), C("#8fd0e8"), 1f);
        Part(p, new Vector3(ox + 4.15f, 0.42f, 6.15f), new Vector3(0.05f, 0.8f, 0.05f), metal, 1f);
        Part(p, new Vector3(ox + 4.15f, 0.42f, 7.05f), new Vector3(0.05f, 0.8f, 0.05f), metal, 1f);
        Part(p, new Vector3(ox + 4.15f, 0.9f, 6.2f), new Vector3(0.16f, 0.1f, 0.28f), C("#b8c4d0"), 1f);
        // 洗衣篮、热水器、拖把桶、储物箱
        PartC(p, new Vector3(ox + 8.15f, 0.27f, 9.3f), new Vector3(0.55f, 0.46f, 0.45f), CB("#e08a4a"), 0.022f, 1f);
        Part(p, new Vector3(ox + 8.15f, 0.52f, 9.3f), new Vector3(0.46f, 0.08f, 0.36f), C("#f5d78a"), 1f);
        PartC(p, new Vector3(ox + 14.5f, 0.86f, 3.2f), new Vector3(0.5f, 1.6f, 0.5f), white, 0.022f, 1f);
        Part(p, new Vector3(ox + 14.5f, 1.7f, 3.2f), new Vector3(0.34f, 0.08f, 0.34f), C("#c8d8e4"), 1f);
        Part(p, new Vector3(ox + 14.5f, 2.6f, 3.2f), new Vector3(0.06f, 1.8f, 0.06f), metal, 1f);
        Part(p, new Vector3(ox + 3.95f, 0.2f, 3.9f), new Vector3(0.36f, 0.4f, 0.36f), C("#4a9ac8"), 1f);
        Part(p, new Vector3(ox + 3.85f, 0.75f, 3.4f), new Vector3(0.04f, 1.4f, 0.04f), C("#b8663c"), 1f);
        Part(p, new Vector3(ox + 3.85f, 0.1f, 3.4f), new Vector3(0.14f, 0.2f, 0.3f), C("#f5d78a"), 1f);
        PartC(p, new Vector3(ox + 13.1f, 0.34f, 9.4f), new Vector3(0.62f, 0.55f, 0.6f), CB("#e08a4a"), 0.022f, 1f);
        Part(p, new Vector3(ox + 13.1f, 0.8f, 9.4f), new Vector3(0.46f, 0.38f, 0.44f), C("#f5d78a"), 1f);
        // 靠南墙的折叠整理台（门东侧，叠衣服/放洗衣篮）
        PartC(p, new Vector3(ox + 9.3f, 0.74f, 2.5f), new Vector3(1.6f, 0.06f, 0.65f), wood, 0.016f, 1f);
        Part(p, new Vector3(ox + 8.6f, 0.37f, 2.5f), new Vector3(0.08f, 0.74f, 0.08f), metal, 1f);
        Part(p, new Vector3(ox + 10.0f, 0.37f, 2.5f), new Vector3(0.08f, 0.74f, 0.08f), metal, 1f);
        Part(p, new Vector3(ox + 9.3f, 0.82f, 2.5f), new Vector3(0.5f, 0.1f, 0.42f), C("#f5d78a"), 1f);
        // 储物柜：倒角 + 门缝 + 把手（原来三块 1.4×2.2×0.5 的深褐大板并排）
        for (int i = 0; i < 3; i++)
        {
            PartC(p, new Vector3(ox + 14.1f, 1.1f, 5.0f + i * 1.55f), new Vector3(1.4f, 2.2f, 0.5f), woodDark, 0.028f, 1f);
            Part(p, new Vector3(ox + 13.86f, 1.1f, 5.0f + i * 1.55f), new Vector3(0.03f, 2.10f, 0.04f), C("#8a5a34"), 1f);
            Part(p, new Vector3(ox + 13.85f, 1.20f, 4.65f + i * 1.55f), new Vector3(0.04f, 0.30f, 0.05f), C("#f0d8a8"), 1f);
            Part(p, new Vector3(ox + 13.85f, 1.20f, 5.35f + i * 1.55f), new Vector3(0.04f, 0.30f, 0.05f), C("#f0d8a8"), 1f);
        }
        Lamp(p, new Vector3(ox + 9f, 3.55f, 6.5f), 10f, 0.9f);
        Lamp(p, new Vector3(ox + 5.2f, 3.55f, 8.2f), 7f, 0.85f);

        // ── 家具碰撞 ──
        Reg(it, ox - 14.5f, -4.0f, 0.5f, 2.8f);
        Reg(it, ox - 11.5f, -4.0f, 1.3f, 0.7f);
        Reg(it, ox - 11.3f, -4.0f, 1.0f, 3.3f);
        Reg(it, ox - 9.0f, -4.0f, 0.95f, 1.5f);
        Reg(it, ox - 8.6f, 1.62f, 2.4f, 0.4f);
        Reg(it, ox + 2.9f, -9.0f, 0.45f, 1.6f);
        Reg(it, ox - 2.6f, -9.25f, 0.9f, 0.4f);
        Reg(it, ox, -4.2f, 2.4f, 1.1f);
        foreach (var (cx2, cz2) in new[] { (0f, -5.05f), (0f, -3.35f), (-1.55f, -4.2f), (1.55f, -4.2f) })
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
        Reg(it, ox + 4.35f, 9.45f, 0.74f, 0.74f);
        Reg(it, ox + 5.25f, 9.45f, 0.74f, 0.74f);
        Reg(it, ox + 6.7f, 9.45f, 1.6f, 0.74f);
        Reg(it, ox + 8.15f, 9.3f, 0.6f, 0.5f);
        Reg(it, ox + 11.4f, 6.4f, 2.1f, 0.75f);
        Reg(it, ox + 4.15f, 6.6f, 0.55f, 1.5f);
        Reg(it, ox + 14.5f, 3.2f, 0.55f, 0.55f);
        Reg(it, ox + 3.95f, 3.9f, 0.4f, 0.4f);
        Reg(it, ox + 13.1f, 9.4f, 0.65f, 0.65f);
        Reg(it, ox + 9.3f, 2.5f, 1.65f, 0.7f);
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
        var wood = InteriorWood();
        var board = T("in_board");
        // 卡通配色（docs/103）：课桌原 #c9a878 偏灰黄、门套 #7a6248 偏暗，全部提亮提饱和
        var woodDark = CB("#a06a3c");
        var deskTop = CB("#f0c46a");
        var green = CB("#3f9a6a");

        const float W = 4.2f;
        const float ZC0 = -2.5f, ZC1 = 2.5f;

        // ── 地面 / 天花 ──
        Part(p, new Vector3(ox, 0f, 0f), new Vector3(56f, 0.04f, 30f), floor, 2f, default, true);
        CeilingSlab(p, ox, W, 56f, 30f, ceil);

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
        // 教室门洞补门楣：北墙是分五段建的，门洞通到顶，门牌会悬在半空
        foreach (var (da, db) in new[] { (ox - 19.6f, ox - 17.6f), (ox - 11.4f, ox - 9.4f),
                                          (ox - 0.8f, ox + 1.2f), (ox + 13.6f, ox + 15.6f) })
        {
            Part(p, new Vector3((da + db) * 0.5f, (2.5f + W) * 0.5f, ZC0),
                 new Vector3(db - da, W - 2.5f, WALL_OUT), wall, 1.7f);
            WallDeco(p, da, db, ZC0 + 0.11f, 2.5f, W, 0, 0.05f, wall, 1.7f);
        }

        // ── 南带：办公室 / 图书角墙（门厅开放；门洞 2.0m）──
        WallX(p, it, ox + 7.5f, ox + 12.0f, ZC1, 0.02f, W, 0f, 0f, 2.5f, wall);
        WallX(p, it, ox + 14.0f, ox + 20.6f, ZC1, 0.02f, W, 0f, 0f, 2.5f, wall);
        WallX(p, it, ox + 22.6f, ox + 28f, ZC1, 0.02f, W, 0f, 0f, 2.5f, wall);
        WallZ(p, it, ZC1, 15f, ox + 7.5f, 0.02f, W, 0f, 0f, 2.5f, wall);
        WallZ(p, it, ZC1, 15f, ox + 16.5f, 0.02f, W, 0f, 0f, 2.5f, wall);
        // 门楣 + 门套 + 门牌（门洞通到顶会缺门头，门牌也没地方挂）
        foreach (var (da, db) in new[] { (ox + 12.0f, ox + 14.0f), (ox + 20.6f, ox + 22.6f) })
        {
            Part(p, new Vector3((da + db) * 0.5f, (2.5f + W) * 0.5f, ZC1),
                 new Vector3(db - da, W - 2.5f, WALL_OUT), wall, 1.7f);
            WallDeco(p, da, db, ZC1 - 0.11f, 2.5f, W, 0, 0.05f, wallB, 1.7f);
        }
        var doorFrame = CB("#c98a4e");
        foreach (var (dc, label, plateMat) in new[] {
                     (13.0f, "办公室", CB("#3f9a6a")), (21.6f, "图书角", CB("#3f9a6a")) })
        {
            float wx = ox + dc;
            Part(p, new Vector3(wx - 1.0f, 1.25f, ZC1), new Vector3(0.14f, 2.5f, 0.34f), doorFrame, 1f);
            Part(p, new Vector3(wx + 1.0f, 1.25f, ZC1), new Vector3(0.14f, 2.5f, 0.34f), doorFrame, 1f);
            Part(p, new Vector3(wx, 2.56f, ZC1), new Vector3(2.14f, 0.16f, 0.34f), doorFrame, 1f);
            Part(p, new Vector3(wx, 2.81f, ZC1 - 0.19f), new Vector3(1.20f, 0.36f, 0.05f), plateMat, 1f);
            Part(p, new Vector3(wx, 2.81f, ZC1 - 0.222f), new Vector3(0.98f, 0.24f, 0.012f), C("#f2fbf4"), 1f);
            SignText(p, new Vector3(wx, 2.81f, ZC1 - 0.232f), 0f, label, 0.034f, MatLib.Hex("#12402a"));
        }

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

        // ── 出口：门扇 + 门廊 ──
        Porch(p, ox, 15f, 6.0f, 1f, 3.1f);
        DoorFrame(p, it, ox, 15f, 2.5f, 2.8f, -1);

        // ── 墙纸 ──
        WallDeco(p, ox - 27.86f, ox + 27.86f, -14.86f, 0.02f, W, 0, 0.05f, wallB, 1.7f);
        WallDeco(p, ox - 27.86f, ox + 27.86f, 14.86f, 0.02f, W, 0, 0.05f, wall, 1.7f);
        WallDeco(p, -14.86f, 14.86f, ox - 27.86f, 0.02f, W, 1, 0.05f, wallB, 1.7f);
        WallDeco(p, -14.86f, 14.86f, ox + 27.86f, 0.02f, W, 1, 0.05f, wallB, 1.7f);
        WallDeco(p, ox - 27.86f, ox + 27.86f, ZC0 + 0.11f, 0.02f, W, 0, 0.05f, wall, 1.7f,
                 new[] { (ox - 19.6f, ox - 17.6f), (ox - 11.4f, ox - 9.4f), (ox - 0.8f, ox + 1.2f), (ox + 13.6f, ox + 15.6f) });
        // 门厅那段（x -27.86~+7.5）本来就没有墙，墙皮要挖空，否则从大门看进去是一面假墙
        // 0.09 会让墙皮整个埋进 0.24 厚的墙里（差 5mm）→ 走廊南墙一直是素墙，改回 0.11
        WallDeco(p, ox - 27.86f, ox + 27.86f, ZC1 - 0.11f, 0.02f, W, 0, 0.05f, wallB, 1.7f,
                 new[] { (ox - 27.86f, ox + 7.5f), (ox + 12.0f, ox + 14.0f), (ox + 20.6f, ox + 22.6f) });
        // 房间侧也贴墙纸（原来只贴了走廊侧，办公室/图书角是素墙）
        WallDeco(p, ox + 7.5f, ox + 16.5f, ZC1 + 0.11f, 0.02f, W, 0, 0.05f, wall, 1.7f,
                 new[] { (ox + 12.0f, ox + 14.0f) });
        WallDeco(p, ox + 16.5f, ox + 27.86f, ZC1 + 0.11f, 0.02f, W, 0, 0.05f, wallB, 1.7f,
                 new[] { (ox + 20.6f, ox + 22.6f) });
        WallDeco(p, ZC1 + 0.11f, 14.86f, ox + 7.39f, 0.02f, W, 1, 0.05f, wall, 1.7f);
        WallDeco(p, ZC1 + 0.11f, 14.86f, ox + 7.61f, 0.02f, W, 1, 0.05f, wallB, 1.7f);
        WallDeco(p, ZC1 + 0.11f, 14.86f, ox + 16.39f, 0.02f, W, 1, 0.05f, wallB, 1.7f);
        WallDeco(p, ZC1 + 0.11f, 14.86f, ox + 16.61f, 0.02f, W, 1, 0.05f, wall, 1.7f);
        foreach (var xx in new[] { -15.25f, -2.75f, 9.75f })
        {
            WallDeco(p, -14.86f, ZC0 + 0.09f, ox + xx - 0.11f, 0.02f, W, 1, 0.05f, wallB, 1.7f);
            WallDeco(p, -14.86f, ZC0 + 0.09f, ox + xx + 0.11f, 0.02f, W, 1, 0.05f, wallB, 1.7f);
        }
        Skirt(p, ox - 27.86f, -14.84f, ox + 27.86f, -14.84f, 0.02f);
        Skirt(p, ox - 27.86f, 14.84f, ox + 27.86f, 14.84f, 0.02f);

        // ── 门厅 ──
        PartC(p, new Vector3(ox + 4.5f, 0.6f, 12.8f), new Vector3(3.0f, 1.2f, 0.7f), woodDark, 0.024f, 1f);
        PartC(p, new Vector3(ox + 4.5f, 1.23f, 12.8f), new Vector3(3.1f, 0.07f, 0.76f), C("#d8b07a"), 0.012f, 1f);
        // 公告栏：外框凸出 + 内白板（原来两块共面，远看像一块糊掉的绿板）
        PartC(p, new Vector3(ox - 8f, 1.75f, 14.79f), new Vector3(3.3f, 1.6f, 0.08f), green, 0.018f, 1f);
        Part(p, new Vector3(ox - 8f, 1.75f, 14.73f), new Vector3(3.1f, 1.4f, 0.02f), C("#f2fbf4"), 1f);
        for (int i = 0; i < 6; i++)
            Part(p, new Vector3(ox - 9.2f + (i % 3) * 1.15f, 2.10f - (i / 3) * 0.62f, 14.71f),
                 new Vector3(0.80f, 0.46f, 0.012f),
                 C(new[] { "#ffd88a", "#a8e0f0", "#ffb0b0", "#c8f0b8" }[i % 4]), 1f);
        Plant(p, new Vector3(ox - 19.2f, 0.02f, 13.8f), 1.2f);
        Plant(p, new Vector3(ox - 5.5f, 0.02f, 13.5f), 1.0f);
        Rug(p, new Vector3(ox, 0.06f, 13.9f), 3.4f, 1.6f, C("#f2b95e"));
        Lamp(p, new Vector3(ox, 3.9f, 11f), 12f, 1.35f);
        Lamp(p, new Vector3(ox - 14f, 3.9f, 8f), 11f, 1.2f);
        Lamp(p, new Vector3(ox + 14f, 3.9f, 8f), 11f, 1.2f);

        // ── 办公室 ──
        Table(p, new Vector3(ox + 12f, 0f, 12.4f), 2.0f, 1.0f, 0.76f, wood);
        Chair(p, it, new Vector3(ox + 12f, 0f, 13.3f), 180f, wood, true);
        Table(p, new Vector3(ox + 9.8f, 0f, 6.2f), 1.8f, 0.9f, 0.76f, wood);
        Chair(p, it, new Vector3(ox + 9.8f, 0f, 7.1f), 180f, wood, true);
        // 文件柜：倒角 + 抽屉缝 + 把手（原来两块 0.5×2.1×1.7 的深褐大板）
        foreach (float fz in new[] { 5.4f, 8.7f })
        {
            PartC(p, new Vector3(ox + 15.9f, 1.05f, fz), new Vector3(0.5f, 2.1f, 1.7f), woodDark, 0.026f, 1f);
            for (int i = 0; i < 3; i++)
                Part(p, new Vector3(ox + 15.64f, 0.52f + i * 0.62f, fz), new Vector3(0.03f, 0.03f, 1.56f), C("#8a5a34"), 1f);
            for (int i = 0; i < 3; i++)
                Part(p, new Vector3(ox + 15.63f, 0.52f + i * 0.62f, fz), new Vector3(0.04f, 0.16f, 0.40f), C("#f0d8a8"), 1f);
        }
        // 西墙白板 + 托槽
        Part(p, new Vector3(ox + 7.66f, 1.80f, 9.8f), new Vector3(0.06f, 1.15f, 3.0f), C("#f8fbf8"), 1f);
        Part(p, new Vector3(ox + 7.70f, 1.21f, 9.8f), new Vector3(0.14f, 0.05f, 2.9f), woodDark, 1f);
        Part(p, new Vector3(ox + 7.697f, 2.00f, 9.2f), new Vector3(0.012f, 0.40f, 0.60f), C("#e0554a"), 1f);
        Part(p, new Vector3(ox + 7.697f, 1.70f, 10.4f), new Vector3(0.012f, 0.30f, 0.45f), C("#4a9ac8"), 1f);
        // 桌面文件
        Part(p, new Vector3(ox + 12.5f, 0.79f, 12.3f), new Vector3(0.36f, 0.05f, 0.28f), C("#f2fbf4"), 0.6f);
        Part(p, new Vector3(ox + 12.46f, 0.845f, 12.32f), new Vector3(0.34f, 0.04f, 0.26f), C("#a8e0f0"), 0.6f);
        WallClock(p, new Vector3(ox + 12f, 2.95f, 14.83f), 180f);
        Plant(p, new Vector3(ox + 8.6f, 0.02f, 13.6f), 0.95f);
        Lamp(p, new Vector3(ox + 12f, 3.9f, 9f), 10f, 1.05f);

        // ── 图书角：书墙 + 阅读桌 ──
        //  房间 11.5×12.5m 只放两张课桌太空；书架贴东墙两段，正好避开两扇窗
        var shelfWood = CB("#c98a4e");
        var bookCols = new[] { C("#e0554a"), C("#4a9ac8"), C("#f0b03c"), C("#6cc06a"),
                               C("#a86ad8"), C("#e08a4a"), C("#4ac8b0") };
        ShelfRun(p, it, ox + 27.60f, 4.95f, 7.05f, shelfWood, bookCols);
        ShelfRun(p, it, ox + 27.60f, 9.95f, 14.30f, shelfWood, bookCols);
        Rug(p, new Vector3(ox + 21.5f, 0.07f, 7.75f), 3.8f, 5.8f, C("#6fc0a8"));
        Table(p, new Vector3(ox + 21.5f, 0f, 6.0f), 1.8f, 0.9f, 0.76f, wood);
        Table(p, new Vector3(ox + 21.5f, 0f, 9.5f), 1.8f, 0.9f, 0.76f, wood);
        foreach (var (cx2, cz2, ry2) in new[] {
                     (20.9f, 5.25f, 0f), (22.1f, 5.25f, 0f), (20.9f, 6.75f, 180f), (22.1f, 6.75f, 180f),
                     (20.9f, 8.75f, 0f), (22.1f, 8.75f, 0f), (20.9f, 10.25f, 180f), (22.1f, 10.25f, 180f) })
            Chair(p, it, new Vector3(ox + cx2, 0f, cz2), ry2, wood, true);
        // 桌面：书堆 + 阅读灯
        foreach (var (tx, tz) in new[] { (21.5f, 6.0f), (21.5f, 9.5f) })
        {
            Part(p, new Vector3(ox + tx - 0.45f, 0.79f, tz + 0.12f), new Vector3(0.34f, 0.06f, 0.24f), C("#e0554a"), 0.6f);
            Part(p, new Vector3(ox + tx - 0.45f, 0.845f, tz + 0.12f), new Vector3(0.30f, 0.05f, 0.22f), C("#4a9ac8"), 0.6f);
            Part(p, new Vector3(ox + tx - 0.45f, 0.895f, tz + 0.12f), new Vector3(0.26f, 0.04f, 0.20f), C("#f0b03c"), 0.6f);
            Part(p, new Vector3(ox + tx + 0.55f, 0.78f, tz + 0.15f), new Vector3(0.16f, 0.04f, 0.16f), woodDark, 0.5f);
            Part(p, new Vector3(ox + tx + 0.55f, 0.93f, tz + 0.15f), new Vector3(0.04f, 0.26f, 0.04f), C("#b8c4d0"), 1f);
            Part(p, new Vector3(ox + tx + 0.55f, 1.09f, tz + 0.15f), new Vector3(0.20f, 0.12f, 0.20f), C("#ffe9b0"), 1f);
            Part(p, new Vector3(ox + tx + 0.55f, 1.025f, tz + 0.15f), new Vector3(0.22f, 0.02f, 0.22f), C("#f0b03c"), 1f);
        }
        Plant(p, new Vector3(ox + 17.4f, 0.02f, 14.2f), 1.1f);
        Plant(p, new Vector3(ox + 23.8f, 0.02f, 3.4f), 0.9f);
        Picture(p, new Vector3(ox + 16.65f, 2.25f, 9.6f), 1.5f, 0.95f, 90f);
        WallClock(p, new Vector3(ox + 16.65f, 3.25f, 6.2f), 90f);
        Picture(p, new Vector3(ox + 24.6f, 2.35f, 14.83f), 1.8f, 1.05f, 180f);
        Lamp(p, new Vector3(ox + 22f, 3.9f, 8f), 10f, 1.05f);
        Lamp(p, new Vector3(ox + 18.6f, 3.9f, 11.5f), 10f, 1.0f);

        // ── 教室 ──
        // 侧墙贴面按真实墙纸面定位（cx±w/2 只是家具网格宽度，不是墙面）
        Classroom(p, it, ox - 21.4f, 12.3f, board, deskTop, wood, green,
                  ox - 27.835f, ox - 15.385f, ox - 19.6f, ox - 17.6f, -1.0f);
        Classroom(p, it, ox - 9.0f, 12.3f, board, deskTop, wood, green,
                  ox - 15.115f, ox - 2.885f, ox - 11.4f, ox - 9.4f, 2.5f);
        Classroom(p, it, ox + 3.6f, 12.3f, board, deskTop, wood, green,
                  ox - 2.615f, ox + 9.615f, ox - 0.8f, ox + 1.2f, 0.6f);
        Classroom(p, it, ox + 18.75f, 17.3f, board, deskTop, wood, green,
                  ox + 9.885f, ox + 27.835f, ox + 13.6f, ox + 15.6f);

        // ── 走廊墙面：1.2m 墙裙 + 压条 + 踢脚线 + 公告栏 ──
        //   56m 长、4.2m 高的空白大墙远看像挂了块布，加个墙裙立刻有"墙"的感觉
        var corSkirt = C("#8fd0c0");
        var corRail = C("#5fb0a0");
        var corFoot = C("#7a5c42");
        var cDoorS = new[] { (ox - 27.86f, ox + 7.5f),                                          // 门厅敞开段
                             (ox + 12.0f, ox + 14.0f), (ox + 20.6f, ox + 22.6f) };             // 办公室 / 图书角门
        var cDoorN = new[] { (ox - 19.6f, ox - 17.6f), (ox - 11.4f, ox - 9.4f),
                             (ox - 0.8f, ox + 1.2f), (ox + 13.6f, ox + 15.6f) };             // 北墙门洞
        // 南墙（z=2.5）
        WallDeco(p, ox - 27.86f, ox + 27.86f, ZC1 - 0.17f, 0.02f, 1.20f, 0, 0.04f, corSkirt, 1.2f, cDoorS);
        WallDeco(p, ox - 27.86f, ox + 27.86f, ZC1 - 0.18f, 1.20f, 1.27f, 0, 0.05f, corRail, 1.2f, cDoorS);
        WallDeco(p, ox - 27.86f, ox + 27.86f, ZC1 - 0.19f, 0.02f, 0.14f, 0, 0.06f, corFoot, 1.2f, cDoorS);
        // 北墙（z=-2.5）
        WallDeco(p, ox - 27.86f, ox + 27.86f, ZC0 + 0.17f, 0.02f, 1.20f, 0, 0.04f, corSkirt, 1.2f, cDoorN);
        WallDeco(p, ox - 27.86f, ox + 27.86f, ZC0 + 0.18f, 1.20f, 1.27f, 0, 0.05f, corRail, 1.2f, cDoorN);
        WallDeco(p, ox - 27.86f, ox + 27.86f, ZC0 + 0.19f, 0.02f, 0.14f, 0, 0.06f, corFoot, 1.2f, cDoorN);
        // 南墙 x<+7.5 是敞开的门厅、根本没有墙 → 公告栏统一挂到连续的北墙，位置避开四个门洞
        var notice = CB("#3f9a6a");
        var noticeCols = new[] { "#ffd88a", "#a8e0f0", "#ffb0b0" };
        foreach (float dx in new[] { -24f, -15.6f, -7.2f, 3.0f, 9.6f, 18f, 26.4f })
        {
            float bx = ox + dx;
            PartC(p, new Vector3(bx, 2.20f, ZC0 + 0.17f), new Vector3(1.74f, 0.99f, 0.05f), notice, 0.014f, 1.7f);
            Part(p, new Vector3(bx, 2.20f, ZC0 + 0.20f), new Vector3(1.50f, 0.75f, 0.02f), C("#f2fbf4"), 1.5f);
            // 板报内容：三块彩色小贴纸，让 56m 的走廊墙面有节奏
            // ⚠ 索引必须用非负种子：dx 有负值，C# 对负数取模返回负数，会直接越界（踩过）
            int seed = Mathf.Abs(Mathf.RoundToInt(dx * 10f)) / 3;
            for (int i = 0; i < 3; i++)
                Part(p, new Vector3(bx - 0.44f + i * 0.44f, 2.20f, ZC0 + 0.215f),
                     new Vector3(0.30f, 0.50f, 0.012f), C(noticeCols[(seed + i) % noticeCols.Length]), 1f);
        }
        foreach (float dx in new[] { -18.6f, -10.4f, 0.2f, 14.6f })   // 教室门牌（门楣上）
        {
            PartC(p, new Vector3(ox + dx, 2.75f, ZC0 + 0.17f), new Vector3(1.14f, 0.34f, 0.05f), CB("#3f9a6a"), 0.012f, 1f);
            Part(p, new Vector3(ox + dx, 2.75f, ZC0 + 0.20f), new Vector3(0.96f, 0.24f, 0.012f), C("#f2fbf4"), 1f);
        }
        // 踢脚线按真实墙段断开（原来整条横穿门洞和敞开式门厅，像悬空的门槛）
        foreach (var (sa, sb) in new[] { (ox + 7.5f, ox + 12.0f), (ox + 14.0f, ox + 20.6f), (ox + 22.6f, ox + 27.86f) })
            Skirt(p, sa, ZC1 - 0.20f, sb, ZC1 - 0.20f, 0.02f);
        foreach (var (sa, sb) in new[] { (ox - 27.86f, ox - 19.6f), (ox - 17.6f, ox - 11.4f),
                                         (ox - 9.4f, ox - 0.8f), (ox + 1.2f, ox + 13.6f), (ox + 15.6f, ox + 27.86f) })
            Skirt(p, sa, ZC0 + 0.20f, sb, ZC0 + 0.20f, 0.02f);

        // ── 走廊灯 ──
        for (int i = -3; i <= 3; i++)
            Lamp(p, new Vector3(ox + i * 8.5f, 3.9f, 0f), 11f, 1.15f);

        // ── 家具碰撞 ──
        Reg(it, ox + 4.5f, 12.8f, 3.0f, 0.7f);
        Reg(it, ox - 8f, 14.65f, 3.2f, 0.2f);
        Reg(it, ox + 12f, 12.4f, 2.0f, 1.0f);
        Reg(it, ox + 9.8f, 6.2f, 1.8f, 0.9f);
        Reg(it, ox + 15.9f, 5.4f, 0.5f, 1.7f);
        Reg(it, ox + 15.9f, 8.7f, 0.5f, 1.7f);
        Reg(it, ox + 21.5f, 6.0f, 1.8f, 0.9f);
        Reg(it, ox + 21.5f, 9.5f, 1.8f, 0.9f);

        return it;
    }

    // 一间教室：黑板在走廊侧内墙（避开北窗眩光），40/60 座
    static void Classroom(Transform p, Interior it, float cx, float w, Material board,
                          Material desk, Material wood, Material green,
                          float westFaceX, float eastFaceX,
                          float doorA = 0f, float doorB = 0f, float boardShift = 0f)
    {
        float zFront = -2.5f - 0.13f;   // 走廊墙教室侧表面（墙半厚 0.12 + 0.01）
        float zBack = -14.86f;
        float cz = (zFront + zBack) * 0.5f;
        float bx = cx + boardShift;     // 黑板/讲台/挂钟整体偏移，避开教室门

        // 黑板：框比板面外凸 6cm（原来黑板就是一块孤零零的深灰板，卡通描边贴墙糊成一条黑边）
        PartC(p, new Vector3(bx, 1.7f, zFront - 0.02f), new Vector3(5.24f, 1.52f, 0.06f), CB("#c98a4e"), 0.016f, 1f);
        Part(p, new Vector3(bx, 1.7f, zFront - 0.055f), new Vector3(5.0f, 1.3f, 0.03f), board, 5.0f);
        // 粉笔槽 + 板擦
        PartC(p, new Vector3(bx, 0.96f, zFront - 0.12f), new Vector3(5.10f, 0.07f, 0.14f), CB("#c98a4e"), 0.012f, 1f);
        Part(p, new Vector3(bx + 1.6f, 1.01f, zFront - 0.12f), new Vector3(0.26f, 0.06f, 0.10f), C("#e0554a"), 1f);
        var skirt = C("#7fd0a8");
        float sx1 = westFaceX + 0.03f, sx2 = eastFaceX - 0.03f;
        // 走廊那面墙的墙裙必须按门洞断开：否则一条 1.1m 高的绿带横在门口，进门像被挡住
        if (doorB > doorA)
        {
            float da = Mathf.Clamp(doorA - 0.12f, sx1, sx2), db = Mathf.Clamp(doorB + 0.12f, sx1, sx2);
            Panel(p, sx1, zFront - 0.04f, da, zFront - 0.04f, 0.02f, 1.1f, skirt);
            Panel(p, db, zFront - 0.04f, sx2, zFront - 0.04f, 0.02f, 1.1f, skirt);
        }
        else Panel(p, sx1, zFront - 0.04f, sx2, zFront - 0.04f, 0.02f, 1.1f, skirt);
        Panel(p, sx1, zBack + 0.03f, sx2, zBack + 0.03f, 0.02f, 1.1f, skirt);
        // 侧墙墙裙贴在真实墙纸面上（与墙纸留 5mm，既不埋进去也不悬空）
        Panel(p, westFaceX + 0.03f, zBack + 0.05f, westFaceX + 0.03f, zFront - 0.05f, 0.02f, 1.1f, skirt);
        Panel(p, eastFaceX - 0.03f, zBack + 0.05f, eastFaceX - 0.03f, zFront - 0.05f, 0.02f, 1.1f, skirt);
        WallClock(p, new Vector3(bx, 2.9f, zFront - 0.18f), 0f);
        // 讲台：倒角台体 + 台面压条
        PartC(p, new Vector3(bx, 0.45f, zFront - 0.85f), new Vector3(2.2f, 0.9f, 0.8f), wood, 0.028f, 1f);
        PartC(p, new Vector3(bx, 0.93f, zFront - 0.85f), new Vector3(2.3f, 0.05f, 0.86f), C("#d8b07a"), 0.012f, 1f);

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
                // 课桌：倒角桌面 + 桌斗（原来只有一块 0.62×0.05×0.42 的薄板和四条腿，近景像悬浮片）
                PartC(p, new Vector3(gx, 0.74f, z), new Vector3(0.62f, 0.05f, 0.42f), desk, 0.012f, 1.0f);
                PartC(p, new Vector3(gx, 0.66f, z + 0.02f), new Vector3(0.52f, 0.11f, 0.34f), Darker(desk, 0.80f), 0.010f, 1.0f);
                for (int lg = 0; lg < 4; lg++)
                    Part(p, new Vector3(gx + (lg % 2 == 0 ? -0.27f : 0.27f), 0.37f,
                                        z + (lg < 2 ? -0.17f : 0.17f)),
                         new Vector3(0.04f, 0.74f, 0.04f), C("#9aa4ae"), 0.5f);
                // 椅子放在桌子**远离黑板**的一侧（z 更小），人朝 +z 正好面向桌子与黑板
                Chair(p, it, new Vector3(gx, 0f, z - 0.62f), 0f, wood, false);
                it.Solids.Add(new Solid(gx - 0.34f, z - 0.92f, gx + 0.34f, z + 0.22f));
            }

        PartC(p, new Vector3(westFaceX + 0.03f, 1.85f, cz + 2.0f), new Vector3(0.05f, 1.3f, 4.2f), green, 0.012f, 4.2f);

        Lamp(p, new Vector3(cx - w * 0.25f, 3.85f, cz - 2.5f), 10f, 0.85f);
        Lamp(p, new Vector3(cx + w * 0.25f, 3.85f, cz - 2.5f), 10f, 0.85f);
        Lamp(p, new Vector3(cx - w * 0.25f, 3.85f, cz + 2.5f), 10f, 0.85f);
        Lamp(p, new Vector3(cx + w * 0.25f, 3.85f, cz + 2.5f), 10f, 0.85f);
    }
}

// 动态字体新增字形会重建图集，自建门牌材质要跟着换贴图（否则新字缺 UV/贴图对不上）
public class SignAtlasSync : MonoBehaviour
{
    public Font font;
    Material _mat;

    void Awake() => _mat = GetComponent<MeshRenderer>().sharedMaterial;

    void LateUpdate()
    {
        if (font == null || _mat == null) return;
        var tex = font.material.mainTexture;
        if (tex != null && _mat.mainTexture != tex) _mat.mainTexture = tex;
    }
}
