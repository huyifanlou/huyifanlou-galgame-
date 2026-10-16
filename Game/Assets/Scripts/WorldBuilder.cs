using System.Collections.Generic;
using UnityEngine;

// 世界构建器：把 world.json + 导出的 PNG 还原成 web v0.40 的校园（几何/材质配比逐条对位）
// 所有尺寸/坐标/颜色均取自 web 版 CampusScene.jsx 的实参；改动请同步 docs/01 与迁移说明。
public class WorldBuilder : MonoBehaviour
{
    public WorldData W { get; private set; }
    public Transform Root { get; private set; }
    public Transform Player { get; private set; }
    public Renderer PlayerSprite { get; private set; }
    public Renderer PlayerShadow { get; private set; }
    public Material LampGlass { get; private set; }
    public Material LampPool { get; private set; }
    public Material SkyDayMat { get; private set; }
    public Material SkyNightMat { get; private set; }
    public Transform SkyNightTr { get; private set; }
    // 昼夜双窗户：{material, 白天贴图名, 夜晚贴图名}——由 TimeOfDay 在夜里切换
    public readonly List<string[]> WindowMats = new List<string[]>();
    public readonly List<Material> WindowMatRefs = new List<Material>();
    public Vector3 Spawn = new Vector3(0f, 0f, 4f);

    readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();
    static readonly string[] TREE_CROWNS = { "#3f7a3f", "#4f9448", "#5da04f", "#6fae4b", "#d98fb0", "#c9a24a" };
    static readonly string[] BUSH_GREENS = { "#3d7a3f", "#356e37", "#4a8a48", "#2f6231" };
    static readonly string[] ROCK_COLS = { "#8d8d94", "#7b7b82", "#9aa0a6", "#6f6f76", "#84848b" };
    static readonly string[] REED_COLS = { "#7a8a4a", "#6a7a42", "#85925a" };
    static readonly string[] REED_COLS2 = { "#85925a", "#7a8a4a", "#6a7a42" };

    // ── 材质缓存 ──────────────────────────────────────────────
    Material LitTex(string tex, float tu = 1f, float tv = 1f)
    {
        string k = "L|" + tex + "|" + tu + "|" + tv;
        if (!_cache.TryGetValue(k, out var m)) { m = MatLib.Lit(tex, tu, tv); _cache[k] = m; }
        return m;
    }
    Material ColMat(string hex)
    {
        string k = "C|" + hex;
        if (!_cache.TryGetValue(k, out var m)) { m = MatLib.LitColor(MatLib.Hex(hex)); _cache[k] = m; }
        return m;
    }

    // ── 原语 ──────────────────────────────────────────────────
    GameObject NewGo(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    GameObject Box(Transform p, Vector3 pos, Vector3 size, Material m, Vector3 euler = default, bool shadow = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);          // 碰撞自行计算（移植 web 版 AABB），不用物理
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = size;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = m;
        if (!shadow) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    GameObject MeshObj(Transform p, Mesh mesh, Vector3 pos, Material m, Vector3 euler = default, bool shadow = true, bool receive = true)
    {
        var go = NewGo("mesh", p);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = shadow ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = receive;
        return go;
    }

    // 水平贴地面片（对应 web 的 plane/circle 贴地网格）
    GameObject Flat(Transform p, float w, float d, float y, Material m, float tu = 1f, float tv = 1f, bool shadow = false)
        => MeshObj(p, MeshFactory.Quad(w, d, tu, tv), new Vector3(0, y, 0), m, default, shadow, shadow);

    // 竖直面片（招牌/大门/精灵）
    GameObject Face(Transform p, float w, float h, Vector3 pos, Material m, float ry = 0f, bool shadow = true)
        => MeshObj(p, MeshFactory.QuadZ(w, h), pos, m, new Vector3(0, ry, 0), shadow, shadow);

    // ── 入口 ──────────────────────────────────────────────────
    void Start()
    {
        var w = BuildWorld();
        if (w == null) return;
        var tod = GetComponent<TimeOfDay>();
        if (tod != null) tod.Init(this);
        var rig = GetComponent<PlayerRig>();
        if (rig != null) rig.Init(this);
        var hud = GetComponent<Hud>();
        if (hud != null) hud.Init(tod);
        var shot = GetComponent<ShotMode>();
        if (shot != null) shot.Init();
    }

    public WorldData BuildWorld()
    {
        W = WorldData.Load();
        if (W == null) return null;
        Root = NewGo("World", transform).transform;

        Sky();
        GroundAndPaths();
        Playground();
        Pond();
        Flowers();
        DirtPatches();
        Branches();
        Fences();
        Lamps();
        Buildings();
        Villa();
        Trees();
        Distant();
        Rockeries();
        Rocks();
        Bushes();
        Reeds();
        PlayerRigObjects();
        return W;
    }

    // 天空：双穹交叉淡化（内看球），夜穹初始透明
    void Sky()
    {
        var day = NewGo("SkyDay", Root);
        day.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Dome(170f, 24, 16);
        var dr = day.AddComponent<MeshRenderer>();
        SkyDayMat = MatLib.Fade("sky_day", new Color(0.5f, 0.5f, 0.5f, 1f));   // Legacy 粒子着色器自带 ×2 色彩倍增，0.5 抵消
        SkyDayMat.renderQueue = 3000;   // 昼穹先画
        dr.sharedMaterial = SkyDayMat;
        dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        dr.receiveShadows = false;

        var night = NewGo("SkyNight", Root);
        night.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Dome(168f, 24, 16);
        var nr = night.AddComponent<MeshRenderer>();
        SkyNightMat = MatLib.Fade("sky_night", new Color(0.5f, 0.5f, 0.5f, 0f));
        SkyNightMat.renderQueue = 3001;   // 夜穹后画（双穹同距，透明排序不确定，必须显式定序）
        nr.sharedMaterial = SkyNightMat;
        nr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        nr.receiveShadows = false;
        SkyNightTr = night.transform;
    }

    void GroundAndPaths()
    {
        Flat(Root, 200f, 200f, 0f, LitTex("grass"), 60f, 60f);
        // 竖路：4.6 × 54.5 @ (0, 0.012, 8.75)，贴图沿长度 14 次
        var v = NewGo("PathV", Root);
        v.transform.localPosition = new Vector3(0f, 0.012f, 8.75f);
        MeshObj(v.transform, MeshFactory.Quad(4.6f, 54.5f, 1f, 14f), Vector3.zero, LitTex("path"));
        // 横路：72 × 4.6 @ (0, 0.016, 0)，贴图横向 14 次
        var h = NewGo("PathH", Root);
        h.transform.localPosition = new Vector3(0f, 0.016f, 0f);
        MeshObj(h.transform, MeshFactory.Quad(72f, 4.6f, 14f, 1f), Vector3.zero, LitTex("path"));
    }

    void Playground()
    {
        var g = W.GROUNDS[0];
        var go = NewGo("Playground", Root);
        go.transform.localPosition = new Vector3(g.pos[0], 0.02f, g.pos[1]);
        MeshObj(go.transform, MeshFactory.Quad(g.size[0], g.size[1], 1f, 1f), Vector3.zero, LitTex("track"));
    }

    void Pond()
    {
        var p = W.POND;
        var g = NewGo("Pond", Root);
        g.transform.localPosition = new Vector3(p.pos[0], 0f, p.pos[1]);
        var t = g.transform;
        Flat(t, (p.r + 0.6f) * 2f, (p.r + 0.6f) * 2f, 0.012f, MatLib.Sprite("pond"));
        Box(t, new Vector3(p.r - 0.5f, 0.25f, 0.9f), new Vector3(0.9f, 0.5f, 0.7f), ColMat("#8d8d94"));
        Box(t, new Vector3(-1.7f, 0.2f, p.r - 0.4f), new Vector3(0.7f, 0.4f, 0.6f), ColMat("#7a7a82"));
    }

    void Flowers()
    {
        var g = NewGo("Flowers", Root).transform;
        foreach (var f in W.FLOWERS)
        {
            var go = NewGo("f", g);
            go.transform.localPosition = new Vector3(f.x, 0.012f, f.z);
            MeshObj(go.transform, MeshFactory.Quad(1.7f, 1.7f), Vector3.zero, LitTex("flower"));
        }
    }

    void DirtPatches()
    {
        var g = NewGo("Dirt", Root).transform;
        foreach (var d in W.DIRT_PATCHES)
        {
            var go = NewGo("d", g);
            go.transform.localPosition = new Vector3(d.x, 0.01f, d.z);
            MeshObj(go.transform, MeshFactory.Quad(4.8f, 4.8f), Vector3.zero, MatLib.Sprite("dirt"));
        }
    }

    void Branches()
    {
        var g = NewGo("Branches", Root).transform;
        foreach (var b in W.BRANCH_PATHS)
        {
            var go = NewGo("b", g);
            go.transform.localPosition = new Vector3(b.x, 0.014f, b.z);
            int rep = Mathf.Max(1, Mathf.RoundToInt(b.len / 2.6f));
            MeshObj(go.transform, MeshFactory.Quad(3f, b.len, 1f, rep), Vector3.zero, LitTex("path"));
        }
    }

    void Fences()
    {
        var g = NewGo("Fences", Root).transform;
        foreach (var f in W.FENCES)
        {
            int count = Mathf.Max(1, Mathf.FloorToInt((f.to - f.from) / 2.8f));
            var run = NewGo("run", g).transform;
            for (int i = 0; i <= count; i++)
            {
                float t = f.from + (f.to - f.from) * i / count;
                var pos = f.axis == "z" ? new Vector3(f.at, 0.48f, t) : new Vector3(t, 0.48f, f.at);
                Box(run, pos, new Vector3(0.14f, 0.96f, 0.14f), ColMat("#cfc9ba"));
            }
            float mid = (f.from + f.to) / 2f;
            foreach (float y in new[] { 0.52f, 0.86f })
            {
                var size = f.axis == "z" ? new Vector3(0.09f, 0.09f, f.to - f.from) : new Vector3(f.to - f.from, 0.09f, 0.09f);
                var pos = f.axis == "z" ? new Vector3(f.at, y, mid) : new Vector3(mid, y, f.at);
                Box(run, pos, size, ColMat("#d8d3c6"));
            }
        }
    }

    void Lamps()
    {
        LampGlass = MatLib.UnlitColor(MatLib.Hex("#d5dbe4"));
        LampPool = MatLib.Additive("lamp_glow", new Color(1f, 1f, 1f, 0f));
        var g = NewGo("Lamps", Root).transform;
        foreach (var l in W.LAMPS)
        {
            var lamp = NewGo("lamp", g).transform;
            lamp.localPosition = new Vector3(l.x, 0f, l.z);
            lamp.localEulerAngles = new Vector3(0f, l.r * Mathf.Rad2Deg, 0f);
            Box(lamp, new Vector3(0f, 0.16f, 0f), new Vector3(0.42f, 0.32f, 0.42f), ColMat("#4a4f58"));
            Box(lamp, new Vector3(0f, 1.85f, 0f), new Vector3(0.16f, 3.4f, 0.16f), ColMat("#3a3f4a"));
            Box(lamp, new Vector3(0.42f, 3.48f, 0f), new Vector3(0.9f, 0.12f, 0.12f), ColMat("#3a3f4a"));
            Box(lamp, new Vector3(0.8f, 3.36f, 0f), new Vector3(0.46f, 0.16f, 0.36f), ColMat("#2f333c"));
            var glass = Box(lamp, new Vector3(0.8f, 3.26f, 0f), new Vector3(0.38f, 0.1f, 0.3f), LampGlass, default, false);
            glass.GetComponent<MeshRenderer>().sharedMaterial = LampGlass;
            var pool = NewGo("pool", lamp);
            pool.transform.localPosition = new Vector3(0.78f, 0.02f, 0f);
            MeshObj(pool.transform, MeshFactory.Quad(4.2f, 4.2f), Vector3.zero, LampPool, default, false, false);
        }
    }

    // ── 四栋建筑（平顶/坡顶/公共；含屋顶设备/侧翼/雨棚/招牌/大门等全部附加件）──
    void Buildings()
    {
        var g = NewGo("Buildings", Root).transform;
        foreach (var b in W.BUILDINGS)
        {
            float w = b.size[0], h = b.size[1], d = b.size[2];
            var grp = NewGo("b_" + b.id, g).transform;
            grp.localPosition = new Vector3(b.pos[0], h / 2f, b.pos[1]);
            grp.localEulerAngles = new Vector3(0f, b.face == 1 ? 0f : 180f, 0f);

            var wallMat = LitTex("wall_" + b.id + "_day");
            Box(grp, Vector3.zero, new Vector3(w, h, d), wallMat);
            WindowMatRefs.Add(wallMat);
            WindowMats.Add(new[] { "wall_" + b.id + "_day", "wall_" + b.id + "_night" });

            var tile = LitTex("roof_" + b.id, 4f, 2f);

            if (b.style == "gable")
            {
                float angle = 0.45f;
                float slope = d / 2f / Mathf.Cos(angle) + 0.4f;
                float y = h / 2f + 0.3f + (d / 4f) * Mathf.Tan(angle) * 0.3f;
                Box(grp, new Vector3(0, y, d / 4f + 0.1f), new Vector3(w + 1f, 0.26f, slope), tile, new Vector3(angle * Mathf.Rad2Deg, 0, 0));
                Box(grp, new Vector3(0, y, -d / 4f - 0.1f), new Vector3(w + 1f, 0.26f, slope), tile, new Vector3(-angle * Mathf.Rad2Deg, 0, 0));
                Box(grp, new Vector3(0, y + (d / 4f) * Mathf.Tan(angle) * 0.65f, 0), new Vector3(w + 1.1f, 0.2f, 0.4f), ColMat("#33363f"));
                float gH = (d / 4f + 0.1f) * Mathf.Tan(angle) + 0.42f;
                foreach (int s in new[] { -1, 1 })
                {
                    var tri = NewGo("gable", grp).transform;
                    tri.localPosition = new Vector3(s * (w / 2f - 0.05f), h / 2f, 0f);
                    tri.localEulerAngles = new Vector3(0f, 90f, 0f);
                    MeshObj(tri, MeshFactory.Triangle(d + 0.2f, gH), Vector3.zero, ColMat(b.wall));
                }
            }
            else if (b.style == "grand")
            {
                Box(grp, new Vector3(0, -h / 2f + 0.25f, 0), new Vector3(w + 1.8f, 0.5f, d + 1.8f), ColMat(b.plinth));
                Box(grp, new Vector3(-(w / 2f - 0.9f), -h / 2f + h * 0.42f + 0.5f, d / 2f + 0.55f), new Vector3(0.5f, h * 0.84f, 0.5f), ColMat(b.trim));
                Box(grp, new Vector3(w / 2f - 0.9f, -h / 2f + h * 0.42f + 0.5f, d / 2f + 0.55f), new Vector3(0.5f, h * 0.84f, 0.5f), ColMat(b.trim));
                Box(grp, new Vector3(0, h / 2f + 0.18f, 0), new Vector3(w + 1.2f, 0.32f, d + 1.2f), ColMat(b.roof));
                Box(grp, new Vector3(0, h / 2f + 0.72f, 0), new Vector3(w * 0.55f, 0.8f, d * 0.55f), ColMat(b.roof));
            }
            else
            {
                Box(grp, new Vector3(0, h / 2f + 0.16f, 0), new Vector3(w + 0.7f, 0.32f, d + 0.7f), ColMat(b.roof));
                Box(grp, new Vector3(0, h / 2f + 0.42f, 0), new Vector3(w + 0.82f, 0.22f, d + 0.82f), ColMat(b.plinth));
            }

            if (b.roofProps)
            {
                var rp = NewGo("roofProps", grp).transform;
                rp.localPosition = new Vector3(0, h / 2f + 0.6f, 0);
                Box(rp, new Vector3(-w * 0.22f, 0f, -d * 0.12f), new Vector3(1.7f, 0.75f, 1.2f), ColMat("#a8adb5"));
                Box(rp, new Vector3(w * 0.24f, 0.03f, d * 0.1f), new Vector3(1f, 0.55f, 0.8f), ColMat("#9aa2ac"));
                Box(rp, new Vector3(w * 0.24f, -0.28f, d * 0.1f), new Vector3(1.1f, 0.16f, 0.9f), ColMat("#7d838c"));
            }
            if (b.vent)
            {
                var v = NewGo("vent", grp).transform;
                v.localPosition = new Vector3(w * 0.28f, h / 2f + 1.05f, 0f);
                Box(v, Vector3.zero, new Vector3(0.28f, 2f, 0.28f), ColMat("#8a9099"));
                Box(v, new Vector3(0, 1.05f, 0), new Vector3(0.62f, 0.22f, 0.62f), ColMat("#6f757e"));
            }
            if (b.wing)
            {
                float ww = 4.6f, wh = h * 0.72f;
                var wg = NewGo("wing", grp).transform;
                wg.localPosition = new Vector3(w / 2f + ww / 2f, 0f, 0.6f);
                var wingMat = LitTex("wing_" + b.id + "_day");
                Box(wg, Vector3.zero, new Vector3(ww, wh, d * 0.8f), wingMat);
                WindowMatRefs.Add(wingMat);
                WindowMats.Add(new[] { "wing_" + b.id + "_day", "wing_" + b.id + "_night" });
                Box(wg, new Vector3(0, wh / 2f + 0.14f, 0), new Vector3(ww + 0.5f, 0.28f, d * 0.8f + 0.5f), ColMat(b.roof));
            }
            if (b.antenna)
            {
                var a = NewGo("antenna", grp).transform;
                a.localPosition = new Vector3(-w / 4f, h / 2f + 0.9f, 0f);
                Box(a, Vector3.zero, new Vector3(0.14f, 1.7f, 0.14f), ColMat("#9aa2ac"));
                Box(a, new Vector3(0, 0.95f, 0), new Vector3(0.3f, 0.24f, 0.3f), ColMat("#c9553f"));
            }
            if (b.tank)
                Box(grp, new Vector3(w / 4f, h / 2f + 0.75f, 0f), new Vector3(1.7f, 0.95f, 1.3f), ColMat("#9aa2ac"));
            if (b.awning)
                Box(grp, new Vector3(0, -h / 2f + 2.3f, d / 2f + 0.55f), new Vector3(w * 0.62f, 0.24f, 1.3f), LitTex("awning"));

            // 招牌 + 大门 + 台阶 + 门灯 + 雨落管 + 门前绿篱
            Face(grp, 3.3f, 0.83f, new Vector3(0, h * 0.28f, d / 2f + 0.04f), LitTex("sign_" + b.id));
            Face(grp, 1.5f, 2.1f, new Vector3(0, -h / 2f + 1.08f, d / 2f + 0.035f), LitTex("door"));
            Box(grp, new Vector3(0, -h / 2f + 0.08f, d / 2f + 0.5f), new Vector3(1.9f, 0.16f, 0.9f), ColMat("#b8b0a2"));
            Box(grp, new Vector3(-1.05f, -h / 2f + 2.32f, d / 2f + 0.06f), new Vector3(0.2f, 0.28f, 0.14f), MatLib.UnlitColor(MatLib.Hex("#ffdf9a")), default, false);
            Box(grp, new Vector3(1.05f, -h / 2f + 2.32f, d / 2f + 0.06f), new Vector3(0.2f, 0.28f, 0.14f), MatLib.UnlitColor(MatLib.Hex("#ffdf9a")), default, false);
            Box(grp, new Vector3(-(w / 2f - 0.35f), 0f, d / 2f + 0.08f), new Vector3(0.16f, h - 0.5f, 0.16f), ColMat(b.plinth));
            Box(grp, new Vector3(w / 2f - 0.35f, 0f, d / 2f + 0.08f), new Vector3(0.16f, h - 0.5f, 0.16f), ColMat(b.plinth));
            Box(grp, new Vector3(-1.95f, -h / 2f + 0.32f, d / 2f + 0.55f), new Vector3(1.3f, 0.62f, 0.55f), ColMat("#3d7a37"));
            Box(grp, new Vector3(1.95f, -h / 2f + 0.32f, d / 2f + 0.55f), new Vector3(1.3f, 0.62f, 0.55f), ColMat("#3d7a37"));
        }
    }

    // ── 主角家：日式二层独栋（双层大屋檐 + 山墙端封 + 玄关雨棚 + 石灯笼 + 竹丛 + 砂石院 + 前院木篱）──
    void Villa()
    {
        var def = W.VILLA;
        float w = def.w, h = def.h, d = def.d;
        var tile = LitTex("roof_villa", 4f, 2f);
        var body = LitTex("villa");
        var grp = NewGo("Villa", Root).transform;
        grp.localPosition = new Vector3(def.x, h / 2f, def.z);
        grp.localEulerAngles = new Vector3(0f, def.face == 1 ? 0f : 180f, 0f);

        const float a1 = 0.46f, a2 = 0.52f;
        float slope1 = d / 2f / Mathf.Cos(a1) + 1.3f;
        float y1 = h / 2f + 0.28f;
        float slope2 = (d * 0.3f) / Mathf.Cos(a2) + 0.6f;
        float y2 = h / 2f + 1.5f;

        Box(grp, Vector3.zero, new Vector3(w, h, d), body);
        Box(grp, new Vector3(0, y1, d / 4f + 0.15f), new Vector3(w + 2.6f, 0.26f, slope1), tile, new Vector3(a1 * Mathf.Rad2Deg, 0, 0));
        Box(grp, new Vector3(0, y1, -d / 4f - 0.15f), new Vector3(w + 2.6f, 0.26f, slope1), tile, new Vector3(-a1 * Mathf.Rad2Deg, 0, 0));
        Box(grp, new Vector3(0, y1 + 0.6f, 0), new Vector3(w * 0.72f, 1.22f, d * 0.6f), body);

        foreach (int s in new[] { -1, 1 })
        {
            var tri = NewGo("vg", grp).transform;
            tri.localPosition = new Vector3(s * (w / 2f - 0.05f), h / 2f, 0f);
            tri.localEulerAngles = new Vector3(0f, 90f, 0f);
            MeshObj(tri, MeshFactory.Triangle(d + 0.2f, (d / 4f + 0.15f) * Mathf.Tan(a1) + 0.26f), Vector3.zero, ColMat("#e2dccc"));
        }
        foreach (int s in new[] { -1, 1 })
        {
            var tri = NewGo("vg2", grp).transform;
            tri.localPosition = new Vector3(s * (w * 0.36f - 0.05f), y1 + 1.22f, 0f);
            tri.localEulerAngles = new Vector3(0f, 90f, 0f);
            MeshObj(tri, MeshFactory.Triangle(d * 0.6f, (d * 0.12f + 0.05f) * Mathf.Tan(a2) + 0.34f), Vector3.zero, ColMat("#e2dccc"));
        }
        Box(grp, new Vector3(0, y2, d * 0.12f + 0.05f), new Vector3(w * 0.72f + 1.6f, 0.22f, slope2), tile, new Vector3(a2 * Mathf.Rad2Deg, 0, 0));
        Box(grp, new Vector3(0, y2, -d * 0.12f - 0.05f), new Vector3(w * 0.72f + 1.6f, 0.22f, slope2), tile, new Vector3(-a2 * Mathf.Rad2Deg, 0, 0));
        Box(grp, new Vector3(0, y2 + 0.42f, 0), new Vector3(w * 0.72f + 1.7f, 0.16f, 0.3f), ColMat("#2b3038"));

        // 玄关雨棚 + 木柱 + 石阶 + 砂石院
        Box(grp, new Vector3(0, -h / 2f + 2.5f, d / 2f + 0.85f), new Vector3(3f, 0.14f, 1.7f), tile, new Vector3(28.6f, 0, 0));
        Box(grp, new Vector3(-1.35f, -h / 2f + 1.2f, d / 2f + 1.35f), new Vector3(0.16f, 2.4f, 0.16f), ColMat("#8a6a48"));
        Box(grp, new Vector3(1.35f, -h / 2f + 1.2f, d / 2f + 1.35f), new Vector3(0.16f, 2.4f, 0.16f), ColMat("#8a6a48"));
        Box(grp, new Vector3(0, -h / 2f + 0.08f, d / 2f + 0.55f), new Vector3(2f, 0.16f, 1f), ColMat("#b8b0a2"));
        var gravel = NewGo("gravel", grp);
        gravel.transform.localPosition = new Vector3(0.6f, -h / 2f + 0.006f, d / 2f + 1.9f);
        MeshObj(gravel.transform, MeshFactory.Quad(4.2f, 2.4f), Vector3.zero, ColMat("#cfc9ba"));

        // 石灯笼（暖光窗）
        var lt = NewGo("lantern", grp).transform;
        lt.localPosition = new Vector3(2.9f, -h / 2f, d / 2f + 1.1f);
        Box(lt, new Vector3(0, 0.12f, 0), new Vector3(0.5f, 0.24f, 0.5f), ColMat("#8d8d94"));
        Box(lt, new Vector3(0, 0.55f, 0), new Vector3(0.22f, 0.62f, 0.22f), ColMat("#84848b"));
        Box(lt, new Vector3(0, 0.95f, 0), new Vector3(0.5f, 0.34f, 0.5f), ColMat("#8d8d94"));
        Box(lt, new Vector3(0, 0.95f, 0.26f), new Vector3(0.26f, 0.2f, 0.04f), MatLib.UnlitColor(MatLib.Hex("#ffdf9a")), default, false);
        Box(lt, new Vector3(0, 1.2f, 0), new Vector3(0.62f, 0.16f, 0.62f), ColMat("#6f6f76"));

        // 竹丛（三杆 + 枝叶）
        float[] bx = { -w / 2f - 0.8f, -w / 2f - 0.5f, -w / 2f - 1.05f };
        float[] bh = { 3.8f, 3.1f, 4.3f };
        string[] bc = { "#5a8a4a", "#4a7a3e", "#639455" };
        for (int i = 0; i < 3; i++)
        {
            var bg = NewGo("bamboo" + i, grp).transform;
            bg.localPosition = new Vector3(bx[i], -h / 2f, d / 2f - 1.2f + i * 0.6f);
            Box(bg, new Vector3(0, bh[i] / 2f, 0), new Vector3(0.12f, bh[i], 0.12f), ColMat(bc[i % 3]));
            Box(bg, new Vector3(0.3f, bh[i] - 0.5f, 0), new Vector3(0.6f, 0.16f, 0.16f), ColMat("#4a7a3e"));
            Box(bg, new Vector3(-0.28f, bh[i] - 0.9f, 0.1f), new Vector3(0.5f, 0.14f, 0.14f), ColMat("#5a8a4a"));
        }

        // 前院木篱（两横杆 + 立柱）
        foreach (float px in new[] { -2.6f, -1.2f, 0.2f, 1.6f, 3.0f })
            Box(grp, new Vector3(px, -h / 2f + 0.45f, d / 2f + 3f), new Vector3(0.14f, 0.9f, 0.14f), ColMat("#a87e52"));
        for (int i = 0; i < 2; i++)
        {
            float cx = i == 0 ? -1.75f : 2f;
            float len = i == 0 ? 2.1f : 2.4f;
            Box(grp, new Vector3(cx, -h / 2f + 0.72f, d / 2f + 3f), new Vector3(len, 0.1f, 0.08f), ColMat("#b98d5e"));
            Box(grp, new Vector3(cx, -h / 2f + 0.38f, d / 2f + 3f), new Vector3(len, 0.1f, 0.08f), ColMat("#b98d5e"));
        }
    }

    // ── 树（9 变体：0-5 常规冠色 / 6 松 / 7 柏 / 8 大树）──
    void Trees()
    {
        var g = NewGo("Trees", Root).transform;
        foreach (var t in W.TREES) Tree(g, t.x, t.z, t.v);
        foreach (var t in W.BORDER_TREES) Tree(g, t.x, t.z, t.v);
    }

    void Tree(Transform parent, float x, float z, int v)
    {
        var grp = NewGo("tree", parent).transform;
        grp.localPosition = new Vector3(x, 0f, z);
        if (v == 6)
        {
            Box(grp, new Vector3(0, 0.4f, 0), new Vector3(0.36f, 0.8f, 0.36f), ColMat("#6b4a2f"));
            Cone(grp, 1.05f, 1.2f, 1.15f - 0.6f, "#2f6231");
            Cone(grp, 0.85f, 1.1f, 1.85f - 0.55f, "#356e37");
            Cone(grp, 0.62f, 1.0f, 2.5f - 0.5f, "#3d7a3f");
        }
        else if (v == 7)
        {
            Box(grp, new Vector3(0, 0.3f, 0), new Vector3(0.3f, 0.6f, 0.3f), ColMat("#5c4630"));
            Cone(grp, 0.55f, 3.0f, 1.9f - 1.5f, "#284f2b");
        }
        else if (v == 8)
        {
            Box(grp, new Vector3(0, 0.8f, 0), new Vector3(0.44f, 1.6f, 0.44f), ColMat("#6d4a2f"));
            Box(grp, new Vector3(0, 2.1f, 0), new Vector3(2.4f, 1.5f, 2.4f), ColMat("#4f9448"));
            Box(grp, new Vector3(0, 3.05f, 0), new Vector3(1.6f, 0.9f, 1.6f), ColMat("#5da04f"));
        }
        else
        {
            bool tall = v == 1, wide = v == 2;
            float trunkH = tall ? 1.5f : 1f;
            Vector3 crown = wide ? new Vector3(2f, 1.4f, 2f) : tall ? new Vector3(1.4f, 1.8f, 1.4f) : new Vector3(1.5f, 1.3f, 1.5f);
            Box(grp, new Vector3(0, trunkH / 2f, 0), new Vector3(0.36f, trunkH, 0.36f), ColMat("#6d4a2f"));
            Box(grp, new Vector3(0, trunkH + crown.y / 2f - 0.1f, 0), crown, ColMat(TREE_CROWNS[v % TREE_CROWNS.Length]));
        }
    }

    void Cone(Transform p, float radius, float height, float baseY, string hex)
        => MeshObj(p, MeshFactory.Cone(radius, height, 6), new Vector3(0, baseY, 0), ColMat(hex));

    // ── 远景建筑（平顶/坡顶/塔楼/公共 四风格 + 基座压地线）──
    void Distant()
    {
        var g = NewGo("Distant", Root).transform;
        for (int i = 0; i < W.DISTANT_BUILDINGS.Length; i++)
        {
            var b = W.DISTANT_BUILDINGS[i];
            var grp = NewGo("d" + i, g).transform;
            grp.localPosition = new Vector3(b.x, b.h / 2f, b.z);
            var texName = "dist_" + i.ToString("D2");
            Box(grp, Vector3.zero, new Vector3(b.w, b.h, b.d), LitTex(texName));
            Box(grp, new Vector3(0, -b.h / 2f + 0.25f, 0), new Vector3(b.w + 0.5f, 0.5f, b.d + 0.5f), ColMat(b.plinth));
            if (b.style == "gable")
            {
                const float angle = 0.42f;
                float slope = b.d / 2f / Mathf.Cos(angle) + 0.3f;
                Box(grp, new Vector3(0, b.h / 2f + 0.18f, b.d / 4f), new Vector3(b.w + 0.6f, 0.22f, slope), ColMat(b.roof), new Vector3(angle * Mathf.Rad2Deg, 0, 0));
                Box(grp, new Vector3(0, b.h / 2f + 0.18f, -b.d / 4f), new Vector3(b.w + 0.6f, 0.22f, slope), ColMat(b.roof), new Vector3(-angle * Mathf.Rad2Deg, 0, 0));
            }
            else if (b.style == "tower")
            {
                Box(grp, new Vector3(0, b.h / 2f + 0.2f, 0), new Vector3(b.w + 0.6f, 0.4f, b.d + 0.6f), ColMat(b.roof));
                Box(grp, new Vector3(b.w / 4f, b.h / 2f + 1.5f, 0), new Vector3(0.16f, 2.2f, 0.16f), ColMat("#9aa2ac"));
                Box(grp, new Vector3(-b.w / 4f, b.h / 2f + 0.85f, 0), new Vector3(1.4f, 0.9f, 1.1f), ColMat("#8a9099"));
            }
            else if (b.style == "grand")
            {
                Box(grp, new Vector3(0, b.h / 2f + 0.18f, 0), new Vector3(b.w + 1f, 0.36f, b.d + 1f), ColMat(b.roof));
                Box(grp, new Vector3(0, b.h / 2f + 0.95f, 0), new Vector3(b.w * 0.5f, 1.1f, b.d * 0.6f), ColMat(b.roof));
            }
            else
            {
                Box(grp, new Vector3(0, b.h / 2f + 0.2f, 0), new Vector3(b.w + 0.7f, 0.4f, b.d + 0.7f), ColMat(b.roof));
                Box(grp, new Vector3(0, b.h / 2f + 0.52f, 0), new Vector3(b.w + 0.85f, 0.24f, b.d + 0.85f), ColMat(b.plinth));
            }
        }
    }

    // ── 假山（三变体 + 灌木点缀）──
    void Rockeries()
    {
        var g = NewGo("Rockeries", Root).transform;
        for (int i = 0; i < W.ROCKERIES.Length; i++)
        {
            var r = W.ROCKERIES[i];
            float s = r.s;
            int v = i % 3;
            var grp = NewGo("rk", g).transform;
            grp.localPosition = new Vector3(r.x, 0f, r.z);
            if (v == 1)
            {
                float[][] L = { new[] { 0f, 0.5f, 0f, 2f, 1f, 1.8f }, new[] { 0.2f, 1.35f, -0.1f, 1.5f, 0.8f, 1.3f }, new[] { -0.15f, 2.1f, 0.1f, 1f, 0.8f, 0.9f }, new[] { 0.05f, 2.75f, -0.05f, 0.6f, 0.6f, 0.55f } };
                for (int k = 0; k < L.Length; k++)
                    Box(grp, new Vector3(L[k][0] * s, L[k][1] * s, L[k][2] * s), new Vector3(L[k][3] * s, L[k][4] * s, L[k][5] * s), ColMat(ROCK_COLS[k % 4]), new Vector3(0, k * 0.7f * Mathf.Rad2Deg, 0));
                Box(grp, new Vector3(0.9f * s, 0.5f * s, 0.4f * s), new Vector3(0.8f * s, 0.55f * s, 0.8f * s), ColMat("#3f7a3f"));
            }
            else if (v == 2)
            {
                float[][] L = { new[] { 0f, 0.3f, 0f, 3.2f, 0.6f, 2.6f }, new[] { 1.5f, 0.25f, 0.8f, 1.8f, 0.5f, 1.5f }, new[] { -1.6f, 0.22f, -0.6f, 1.5f, 0.45f, 1.3f } };
                string[] cc = { "#8d8d94", "#9aa0a6", "#84848b" };
                for (int k = 0; k < L.Length; k++)
                    Box(grp, new Vector3(L[k][0] * s, L[k][1] * s, L[k][2] * s), new Vector3(L[k][3] * s, L[k][4] * s, L[k][5] * s), ColMat(cc[k % 3]), new Vector3(0, k * 1.1f * Mathf.Rad2Deg, 0));
                Box(grp, new Vector3(0.6f * s, 0.75f * s, 0.3f * s), new Vector3(0.85f * s, 0.6f * s, 0.85f * s), ColMat("#4a8a48"));
                Box(grp, new Vector3(-0.9f * s, 0.7f * s, 0.5f * s), new Vector3(0.7f * s, 0.5f * s, 0.7f * s), ColMat("#3d7a3f"));
                Box(grp, new Vector3(0.2f * s, 0.68f * s, -0.7f * s), new Vector3(0.6f * s, 0.45f * s, 0.6f * s), ColMat("#356e37"));
            }
            else
            {
                float[][] L = { new[] { 0f, 0.5f, 0f, 2.6f, 1f, 2.2f, 0f }, new[] { 1.6f, 0.4f, 0.6f, 1.6f, 0.8f, 1.4f, 0.4f }, new[] { -1.5f, 0.35f, -0.5f, 1.4f, 0.7f, 1.2f, -0.5f }, new[] { 0.4f, 1.15f, -0.3f, 1.2f, 0.6f, 1f, 0.9f }, new[] { -0.6f, 0.9f, 0.5f, 0.9f, 0.5f, 0.8f, -0.8f } };
                for (int k = 0; k < L.Length; k++)
                    Box(grp, new Vector3(L[k][0] * s, L[k][1] * s, L[k][2] * s), new Vector3(L[k][3] * s, L[k][4] * s, L[k][5] * s), ColMat(ROCK_COLS[k % 5]), new Vector3(0, L[k][6] * Mathf.Rad2Deg, 0));
                Box(grp, new Vector3(0.8f * s, 1f * s, 0.2f * s), new Vector3(0.9f * s, 0.7f * s, 0.9f * s), ColMat("#3f7a3f"));
                Box(grp, new Vector3(-1f * s, 0.75f * s, -0.1f * s), new Vector3(0.7f * s, 0.55f * s, 0.7f * s), ColMat("#356e37"));
            }
        }
    }

    // ── 小石头（校内 12 + 环带 22，索引连续）──
    void Rocks()
    {
        var g = NewGo("Rocks", Root).transform;
        var all = new List<PosItem>();
        all.AddRange(W.SMALL_ROCKS);
        all.AddRange(W.BORDER_ROCKS);
        for (int i = 0; i < all.Count; i++)
        {
            var grp = NewGo("rock", g).transform;
            grp.localPosition = new Vector3(all[i].x, 0f, all[i].z);
            Box(grp, new Vector3(0, 0.18f + (i % 2) * 0.1f, 0),
                new Vector3(0.5f + (i % 3) * 0.2f, 0.36f + (i % 2) * 0.18f, 0.45f + (i % 2) * 0.22f),
                ColMat(ROCK_COLS[i % 3]), new Vector3(0, (i * 1.3f) % 6.28f * Mathf.Rad2Deg, 0));
        }
    }

    // ── 灌木（四色 + 开花变体；校内与边界各自独立编号）──
    void Bushes()
    {
        var g = NewGo("Bushes", Root).transform;
        for (int i = 0; i < W.BUSHES.Length; i++) Bush(g, W.BUSHES[i].x, W.BUSHES[i].z, i);
        for (int i = 0; i < W.BORDER_BUSHES.Length; i++) Bush(g, W.BORDER_BUSHES[i].x, W.BORDER_BUSHES[i].z, i);
    }

    void Bush(Transform parent, float x, float z, int i)
    {
        var grp = NewGo("bush", parent).transform;
        grp.localPosition = new Vector3(x, 0f, z);
        Box(grp, new Vector3(0, 0.24f, 0), new Vector3(0.95f, 0.5f, 0.95f), ColMat(BUSH_GREENS[i % 4]));
        Box(grp, new Vector3(0.12f, 0.52f, -0.08f), new Vector3(0.55f, 0.36f, 0.55f), ColMat(BUSH_GREENS[(i + 1) % 4]));
        if (i % 6 == 0)
            Box(grp, new Vector3(0.1f, 0.72f, 0.1f), new Vector3(0.4f, 0.16f, 0.4f), ColMat("#c98099"));
    }

    // ── 池塘芦苇（池沿细杆丛，黄绿三色）──
    void Reeds()
    {
        var g = NewGo("Reeds", Root).transform;
        foreach (var r in W.REEDS)
        {
            var grp = NewGo("reed", g).transform;
            grp.localPosition = new Vector3(r.x, 0f, r.z);
            Box(grp, new Vector3(0, r.h / 2f, 0), new Vector3(0.12f, r.h, 0.12f), ColMat(REED_COLS[r.t]));
            Box(grp, new Vector3(0.16f, r.h * 0.4f, 0.1f), new Vector3(0.1f, r.h * 0.8f, 0.1f), ColMat(REED_COLS2[r.t]), new Vector3(0, 0.6f * Mathf.Rad2Deg, 0.12f * Mathf.Rad2Deg));
        }
    }

    // ── 角色（像素小人 + 脚下圆影；位置由 PlayerRig 驱动）──
    void PlayerRigObjects()
    {
        var grp = NewGo("Player", Root).transform;
        grp.localPosition = Spawn;
        Player = grp;

        var sprite = NewGo("sprite", grp).transform;
        sprite.localPosition = new Vector3(0f, 0.58f, 0f);
        var sm = MatLib.Sprite("player");
        sm.mainTextureScale = new Vector2(1f / 3f, 1f / 4f);
        sm.mainTextureOffset = new Vector2(0f, 3f / 4f);
        MeshObj(sprite, MeshFactory.QuadZ(1.12f, 1.64f), Vector3.zero, sm, default, false, false);
        PlayerSprite = sprite.GetComponentInChildren<Renderer>();

        var sh = NewGo("shadow", grp).transform;
        sh.localPosition = new Vector3(0f, 0.03f, 0f);
        MeshObj(sh, MeshFactory.Quad(0.84f, 0.84f), Vector3.zero, MatLib.Shadow(new Color(0f, 0f, 0f, 0.22f)), default, false, false);
        PlayerShadow = sh.GetComponentInChildren<Renderer>();
    }
}
