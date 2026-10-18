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
    public readonly List<Material> LeafMats = new List<Material>();   // 叶簇材质（天气：雪天覆白）
    public Material GrassMat;                                        // 草地材质（天气：雨天变暗）
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
    // 带色纹理（树冠/树皮这类「贴图 × 颜色」的材质）
    Material LitTinted(string tex, string hex, float tu = 1f, float tv = 1f)
    {
        string k = "T|" + tex + "|" + hex + "|" + tu + "|" + tv;
        if (!_cache.TryGetValue(k, out var m))
        {
            m = MatLib.Lit(tex, tu, tv);
            m.color = MatLib.Hex(hex);
            _cache[k] = m;
            if (tex == "leaves" && !LeafMats.Contains(m)) LeafMats.Add(m);
        }
        return m;
    }
    // 带光滑度的纯色（雨落管/台阶等小件）
    Material ColMatGloss(string hex, float smoothness)
    {
        string k = "G|" + hex + "|" + smoothness;
        if (!_cache.TryGetValue(k, out var m)) { m = MatLib.LitColor(MatLib.Hex(hex), smoothness); _cache[k] = m; }
        return m;
    }
    // 金属件（低金属度 + 光滑度，受光处出高光）
    Material MatMetal(string hex)
    {
        string k = "M|" + hex;
        if (!_cache.TryGetValue(k, out var m)) { m = MatLib.Metal(hex); _cache[k] = m; }
        return m;
    }
    // 不透明裁切贴片（操场跑道：只画跑道面，内场/边缘透出地形草）
    Material MatCutout(string tex, float smoothness = 0f)
    {
        string k = "K|" + tex + "|" + smoothness;
        if (!_cache.TryGetValue(k, out var m)) { m = MatLib.Cutout(tex, smoothness); _cache[k] = m; }
        return m;
    }

    // ── 积雪层（半透明薄层，Alpha 由天气驱动；雪停时禁用渲染器避免全屏 overdraw）──
    Material _snowLayerMat;
    Renderer _snowLayerR;
    void SnowLayer()
    {
        float gs = W.GROUND_SIZE > 10 ? W.GROUND_SIZE : 200f;
        var go = NewGo("SnowLayer", Root);
        _snowLayerMat = MatLib.Transparent("snow_ground", 0.18f, 0f);
        float reps = gs / TexScale.GrassPeriod;
        MeshObj(go.transform, MeshFactory.Quad(gs, gs, reps, reps), new Vector3(0, 0.028f, 0), _snowLayerMat, default, false, false);
        _snowLayerR = go.GetComponentInChildren<Renderer>();
        if (_snowLayerR != null) _snowLayerR.enabled = false;
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

    // ── 设施道具（长椅 / 垃圾桶 / 公告栏 / 车架 / 旗杆 / 花坛 / 喷泉 / 球场附件 / 校门 / 广场）──
    void Props()
    {
        var g = NewGo("Props", Root).transform;
        foreach (var p in W.PROPS)
        {
            var grp = NewGo(p.type, g).transform;
            grp.localPosition = new Vector3(p.x, 0f, p.z);
            grp.localEulerAngles = new Vector3(0f, p.ry * Mathf.Rad2Deg, 0f);
            switch (p.type)
            {
                case "plaza":
                    {
                        var prep = TexScale.Repeat(p.w, p.d, TexScale.PathPeriod);
                        MeshObj(grp, MeshFactory.Quad(p.w, p.d, prep.x, prep.y),
                            new Vector3(0, 0.018f, 0), LitTex("path"));
                    }
                    break;
                case "flag":
                    Box(grp, new Vector3(0, 0.25f, 0), new Vector3(0.9f, 0.5f, 0.9f), ColMat("#b8b0a2"));
                    Box(grp, new Vector3(0, 4.2f, 0), new Vector3(0.14f, 8f, 0.14f), ColMat("#d8dce2"));
                    Box(grp, new Vector3(0.78f, 7.3f, 0), new Vector3(1.45f, 0.9f, 0.06f), ColMat("#c0392b"));
                    Box(grp, new Vector3(0.78f, 7.3f, 0.02f), new Vector3(0.6f, 0.5f, 0.03f), MatLib.UnlitColor(MatLib.Hex("#f5d04e")), default, false);
                    break;
                case "bench":
                    Box(grp, new Vector3(0, 0.45f, 0), new Vector3(1.8f, 0.09f, 0.5f), ColMat("#a87e52"));
                    Box(grp, new Vector3(0, 0.75f, -0.24f), new Vector3(1.8f, 0.55f, 0.08f), ColMat("#b98d5e"));
                    Box(grp, new Vector3(-0.75f, 0.22f, 0), new Vector3(0.12f, 0.44f, 0.44f), ColMat("#6b6b70"));
                    Box(grp, new Vector3(0.75f, 0.22f, 0), new Vector3(0.12f, 0.44f, 0.44f), ColMat("#6b6b70"));
                    break;
                case "bin":
                    {
                        var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        var cc = cyl.GetComponent<Collider>();
                        if (cc != null) Destroy(cc);
                        cyl.transform.SetParent(grp, false);
                        cyl.transform.localPosition = new Vector3(0f, 0.36f, 0f);
                        cyl.transform.localScale = new Vector3(0.5f, 0.36f, 0.5f);
                        cyl.GetComponent<MeshRenderer>().sharedMaterial = ColMat("#4a5a52");
                        Box(grp, new Vector3(0, 0.75f, 0), new Vector3(0.56f, 0.06f, 0.56f), ColMat("#3a4a44"));
                    }
                    break;
                case "board":
                    Box(grp, new Vector3(-0.7f, 0.85f, 0), new Vector3(0.1f, 1.7f, 0.1f), ColMat("#6b5a42"));
                    Box(grp, new Vector3(0.7f, 0.85f, 0), new Vector3(0.1f, 1.7f, 0.1f), ColMat("#6b5a42"));
                    Box(grp, new Vector3(0, 1.4f, 0.03f), new Vector3(1.8f, 1.05f, 0.09f), ColMat("#3a3f4a"));
                    Box(grp, new Vector3(0, 1.4f, 0.09f), new Vector3(1.6f, 0.85f, 0.02f), ColMat("#f2efe6"));
                    break;
                case "bikerack":
                    {
                        int n = p.n > 0 ? p.n : 5;
                        for (int k = 0; k < n; k++)
                        {
                            float bx = (k - (n - 1) / 2f) * 0.7f;
                            Box(grp, new Vector3(bx, 0.3f, 0), new Vector3(0.06f, 0.6f, 0.9f), ColMat("#9aa2ac"));
                        }
                        Box(grp, new Vector3(0, 0.58f, 0), new Vector3(n * 0.7f, 0.06f, 0.06f), ColMat("#8a9099"));
                    }
                    break;
                case "planter":
                    {
                        float hw = p.w / 2f, hd = p.d / 2f;
                        Box(grp, new Vector3(0, 0.16f, -hd + 0.1f), new Vector3(p.w, 0.32f, 0.2f), ColMat("#9c9484"));
                        Box(grp, new Vector3(0, 0.16f, hd - 0.1f), new Vector3(p.w, 0.32f, 0.2f), ColMat("#9c9484"));
                        Box(grp, new Vector3(-hw + 0.1f, 0.16f, 0), new Vector3(0.2f, 0.32f, p.d), ColMat("#9c9484"));
                        Box(grp, new Vector3(hw - 0.1f, 0.16f, 0), new Vector3(0.2f, 0.32f, p.d), ColMat("#9c9484"));
                        Box(grp, new Vector3(0, 0.3f, 0), new Vector3(p.w - 0.3f, 0.22f, p.d - 0.3f), ColMat("#4a8743"));
                        Box(grp, new Vector3(0, 0.44f, 0), new Vector3(p.w - 0.6f, 0.14f, p.d - 0.6f), ColMat("#e8748a"));
                    }
                    break;
                case "fountain":
                    {
                        var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        var rc = rim.GetComponent<Collider>();
                        if (rc != null) Destroy(rc);
                        rim.transform.SetParent(grp, false);
                        rim.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                        rim.transform.localScale = new Vector3(p.r * 2f, 0.25f, p.r * 2f);
                        rim.GetComponent<MeshRenderer>().sharedMaterial = ColMat("#9c9484");
                        var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        var wc = water.GetComponent<Collider>();
                        if (wc != null) Destroy(wc);
                        water.transform.SetParent(grp, false);
                        water.transform.localPosition = new Vector3(0f, 0.42f, 0f);
                        water.transform.localScale = new Vector3(p.r * 1.7f, 0.04f, p.r * 1.7f);
                        water.GetComponent<MeshRenderer>().sharedMaterial = ColMat("#4d82bd");
                        Box(grp, new Vector3(0, 0.75f, 0), new Vector3(0.4f, 0.7f, 0.4f), ColMat("#8d8d94"));
                        Box(grp, new Vector3(0, 1.15f, 0), new Vector3(0.9f, 0.16f, 0.9f), ColMat("#a9adb2"));
                    }
                    break;
                case "hoop":
                    Box(grp, new Vector3(0, 1.6f, 0), new Vector3(0.16f, 3.2f, 0.16f), ColMat("#5a6270"));
                    Box(grp, new Vector3(0, 3.15f, 0.42f), new Vector3(1.3f, 0.85f, 0.08f), ColMat("#f2efe6"));
                    Box(grp, new Vector3(0, 2.68f, 0.62f), new Vector3(0.5f, 0.06f, 0.5f), ColMat("#c0392b"));
                    break;
                case "netpost":
                    Box(grp, new Vector3(-p.len / 2f, 0.6f, 0), new Vector3(0.1f, 1.2f, 0.1f), ColMat("#3a3f4a"));
                    Box(grp, new Vector3(p.len / 2f, 0.6f, 0), new Vector3(0.1f, 1.2f, 0.1f), ColMat("#3a3f4a"));
                    Box(grp, new Vector3(0, 0.95f, 0), new Vector3(p.len, 0.8f, 0.04f), ColMat("#e6e6e2"));
                    break;
                case "gate":
                    if (p.kind == "main")
                    {
                        Box(grp, new Vector3(-3.6f, 2.6f, 0), new Vector3(0.9f, 5.2f, 0.9f), ColMat("#c9c3b6"));
                        Box(grp, new Vector3(3.6f, 2.6f, 0), new Vector3(0.9f, 5.2f, 0.9f), ColMat("#c9c3b6"));
                        Box(grp, new Vector3(0, 5.55f, 0), new Vector3(8.6f, 0.7f, 0.9f), ColMat("#8f8a80"));
                        Face(grp, 4.8f, 1.1f, new Vector3(0, 4.6f, 0.5f), LitTex("sign_gate"));
                    }
                    else
                    {
                        // 侧门：无匾额，双柱 + 横梁 + 红白挡杆
                        Box(grp, new Vector3(-4.2f, 2f, 0), new Vector3(0.8f, 4f, 0.8f), ColMat("#c9c3b6"));
                        Box(grp, new Vector3(4.2f, 2f, 0), new Vector3(0.8f, 4f, 0.8f), ColMat("#c9c3b6"));
                        Box(grp, new Vector3(0, 4.15f, 0), new Vector3(9.2f, 0.5f, 0.7f), ColMat("#9aa2ac"));
                        Box(grp, new Vector3(-2f, 1.15f, 0), new Vector3(4.4f, 0.16f, 0.16f), ColMat("#c9553f"));
                        Box(grp, new Vector3(2f, 1.15f, 0), new Vector3(4.4f, 0.16f, 0.16f), ColMat("#efe9dd"));
                    }
                    break;
                case "arch":
                    // 已废弃（家属区拱门按作者要求删除）；保留空实现以防旧数据引用
                    break;
            }
        }
    }

    // ── 入口 ──────────────────────────────────────────────────
    void Start()
    {
        var w = BuildWorld();
        if (w == null) return;
        // 后处理栈（泛光/移轴景深/色彩分级/暗角）——挂到主相机
        var cam = Camera.main;
        if (cam != null && cam.GetComponent<PostFX>() == null) cam.gameObject.AddComponent<PostFX>();

        var tod = GetComponent<TimeOfDay>();
        if (tod != null) tod.Init(this);
        var rig = GetComponent<PlayerRig>();
        if (rig != null) rig.Init(this);
        var hud = GetComponent<Hud>();
        if (hud != null) hud.Init(tod);
        var shot = GetComponent<ShotMode>();
        if (shot != null) shot.Init();

        // 天气（雨/雪）：Y 键循环；-weather 直接指定
        var wth = GetComponent<Weather>();
        if (wth == null) wth = gameObject.AddComponent<Weather>();
        var wet = new List<Material>
        {
            LitTex("path"), LitTex("road_mainstone"), LitTex("road_cobble"),
            LitTex("track"), LitTex("court_basket"), LitTex("court_tennis"), LitTex("concrete"),
            LitTex("roadband_v"), LitTex("roadband_h"), GrassMat,
        };
        wth.Init(Player, tod, wet, LeafMats, _snowLayerR, _snowLayerMat);
    }

    public WorldData BuildWorld()
    {
        W = CampusData.Build();   // C# 权威数据（2026-10-16 起：全项目单语言，不再读 world.json）
        if (W == null) return null;
        Root = NewGo("World", transform).transform;

        Sky();
        GroundAndPaths();
        Grounds();
        Pond();
        Flowers();
        DirtPatches();
        Branches();
        Fences();
        Lamps();
        Props();
        Buildings();
        Villa();
        Trees();
        Distant();
        SnowLayer();
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
        float gs = W.GROUND_SIZE > 10 ? W.GROUND_SIZE : 200f;
        var greps = TexScale.Repeat(gs, gs, TexScale.GrassPeriod);
        GrassMat = LitTex("grass", greps.x, greps.y);
        Flat(Root, gs, gs, 0f, GrassMat, greps.x, greps.y);
        // 路网：竖路 y=0.012、横路 y=0.016（交叉处横路在上，与 web 版一致）
        var g = NewGo("Roads", Root).transform;
        foreach (var r in W.ROADS)
        {
            float len = r.to - r.from;
            float mid = (r.from + r.to) / 2f;
            var go = NewGo(r.id, g);
            go.transform.localPosition = r.axis == "z" ? new Vector3(r.at, 0.012f, mid) : new Vector3(mid, 0.016f, r.at);
            // 贴图密度：周期取自 TexScale（主路石板 1.6m / 次路 0.8m，均 160px/米），精确平铺不取整
            var rep = TexScale.Repeat(r.width, len, TexScale.RoadPeriod(r.kind));
            float repW = rep.x, repL = rep.y;
            var mesh = r.axis == "z" ? MeshFactory.Quad(r.width, len, repW, repL) : MeshFactory.Quad(len, r.width, repL, repW);
            string tex = r.kind == "mainstone" ? "road_mainstone" : (r.kind == "cobble" ? "road_cobble" : "path");
            MeshObj(go.transform, mesh, Vector3.zero, LitTex(tex));
            // 路缘石（两侧压边条，场景更精致）
            foreach (int sgn in new[] { -1, 1 })
            {
                float off = sgn * (r.width / 2f - 0.09f);
                var kerb = NewGo("kerb", go.transform);
                kerb.transform.localPosition = r.axis == "z" ? new Vector3(off, 0.004f, 0f) : new Vector3(0.004f * 0f, 0.004f, off);
                var kmesh = r.axis == "z" ? MeshFactory.Quad(0.18f, len, 1f, repL) : MeshFactory.Quad(len, 0.18f, repL, 1f);
                MeshObj(kerb.transform, kmesh, Vector3.zero, ColMat("#bfb9ab"));
            }
            if (r.kind == "mainstone")
            {
                // 中线石带（浅色石条）：带宽 0.6m 一次铺满、纵向每 1m 一铺（贴图 96×160 = 160px/米，接缝每 1m）
                var bandMesh = r.axis == "z" ? MeshFactory.Quad(0.6f, len, 1f, len) : MeshFactory.Quad(len, 0.6f, len, 1f);
                MeshObj(go.transform, bandMesh, new Vector3(0, 0.006f, 0), LitTex(r.axis == "z" ? "roadband_v" : "roadband_h"), default, false, false);
            }
        }
    }

    // 场地：操场（跑道贴图）/ 篮球场 / 网球场 + 路口铺装
    void Grounds()
    {
        // 交叉口铺装（统一衔接：十字处加一整块铺装，消除两条路交叠的贴图接缝）
        var rn = NewGo("Crossings", Root).transform;
        foreach (var rv in W.ROADS)
        {
            if (rv.axis != "z") continue;
            foreach (var rh in W.ROADS)
            {
                if (rh.axis != "x") continue;
                if (rv.from > rh.at + 0.1f || rv.to < rh.at - 0.1f || rh.from > rv.at + 0.1f || rh.to < rv.at - 0.1f) continue;
                var c = NewGo("cross", rn);
                c.transform.localPosition = new Vector3(rv.at, 0.021f, rh.at);
                var creps = TexScale.Repeat(rh.width, rv.width, TexScale.PathPeriod);
                MeshObj(c.transform, MeshFactory.Quad(rh.width, rv.width, creps.x, creps.y),
                    Vector3.zero, LitTex("path"));
            }
        }
        var g = NewGo("Grounds", Root).transform;
        foreach (var def in W.GROUNDS)
        {
            var go = NewGo(def.id, g);
            go.transform.localPosition = new Vector3(def.pos[0], 0.02f, def.pos[1]);
            // 操场：透明裁切贴片（内场透出地形草，边缘没有「贴上去的方片」接缝）；球场仍是实心面
            bool field = def.id == "playground";
            string tex = field ? "track" : (def.kind == "tennis" ? "court_tennis" : "court_basket");
            MeshObj(go.transform, MeshFactory.Quad(def.size[0], def.size[1], 1f, 1f), Vector3.zero,
                field ? MatCutout(tex) : LitTex(tex));
        }
    }

    void Pond()
    {
        var p = W.POND;
        var g = NewGo("Pond", Root);
        g.transform.localPosition = new Vector3(p.pos[0], 0f, p.pos[1]);
        var t = g.transform;
        Flat(t, (p.r + 0.6f) * 2f, (p.r + 0.6f) * 2f, 0.012f, MatLib.Sprite("pond", 0.55f));
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
            // 花坛：cutout 材质（边缘抖动透明，与地形草自然过渡）
            MeshObj(go.transform, MeshFactory.Quad(1.7f, 1.7f), Vector3.zero, MatCutout("flower"));
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
            var breps = TexScale.Repeat(3f, b.len, TexScale.PathPeriod);   // 此前 u 取整 4 次 → 170.7px/米，偏密
            MeshObj(go.transform, MeshFactory.Quad(3f, b.len, breps.x, breps.y), Vector3.zero, LitTex("path"));
        }
    }

    void Fences()
    {
        // 矮墙（替代白色栅栏）：连续墙体 + 压顶石 + 间隔立柱——分区明确且不遮挡视线
        var g = NewGo("Walls", Root).transform;
        foreach (var f in W.FENCES)
        {
            float len = f.to - f.from;
            float mid = (f.from + f.to) / 2f;
            var run = NewGo("wall", g).transform;
            var wallSize = f.axis == "z" ? new Vector3(0.26f, 0.6f, len) : new Vector3(len, 0.6f, 0.26f);
            var wallPos = f.axis == "z" ? new Vector3(f.at, 0.3f, mid) : new Vector3(mid, 0.3f, f.at);
            // 写实化：混凝土贴图 + 世界尺度 UV（2.4m 一铺 = 每 2.4m 一道砌筑缝；墙脚勒脚带）
            MeshObj(run, MeshFactory.BoxUV(wallSize, 2.4f), wallPos, LitTex("concrete"));
            // 压顶：浅色压顶石（高 12cm、外挑 6cm）——和墙身形成明显两段色，远处也读得出「墙」
            var capSize = f.axis == "z" ? new Vector3(0.38f, 0.12f, len) : new Vector3(len, 0.12f, 0.38f);
            var capPos = f.axis == "z" ? new Vector3(f.at, 0.66f, mid) : new Vector3(mid, 0.66f, f.at);
            MeshObj(run, MeshFactory.BoxUV(capSize, 0.8f), capPos, ColMatGloss("#e9e3d6", 0.20f));
            int count = Mathf.Max(1, Mathf.FloorToInt(len / 5.6f));
            for (int i = 0; i <= count; i++)
            {
                float t = f.from + len * i / count;
                var pos = f.axis == "z" ? new Vector3(f.at, 0.44f, t) : new Vector3(t, 0.44f, f.at);
                MeshObj(run, MeshFactory.BoxUV(new Vector3(0.4f, 0.88f, 0.4f), 2.4f), pos, LitTinted("concrete", "#d4cec1"));
                // 立柱压顶石（浅色小方帽 → 远处也能看出"墙柱"节奏）
                MeshObj(run, MeshFactory.BoxUV(new Vector3(0.46f, 0.09f, 0.46f), 0.8f),
                    pos + new Vector3(0f, 0.485f, 0f), ColMatGloss("#e9e3d6", 0.20f));
            }
        }
    }

    void Lamps()
    {
        // 灯罩玻璃：用受光材质（阴天/雨雪里不会凭空发白），夜里由 TimeOfDay 改成暖色 + 泛光
        LampGlass = MatLib.LitColor(MatLib.Hex("#d5dbe4"), 0.35f);
        LampPool = MatLib.Additive("lamp_glow", new Color(1f, 1f, 1f, 0f));
        var g = NewGo("Lamps", Root).transform;
        foreach (var l in W.LAMPS)
        {
            var lamp = NewGo("lamp", g).transform;
            lamp.localPosition = new Vector3(l.x, 0f, l.z);
            lamp.localEulerAngles = new Vector3(0f, l.r * Mathf.Rad2Deg, 0f);
            Box(lamp, new Vector3(0f, 0.16f, 0f), new Vector3(0.42f, 0.32f, 0.42f), MatMetal("#4a4f58"));
            Box(lamp, new Vector3(0f, 1.85f, 0f), new Vector3(0.16f, 3.4f, 0.16f), MatMetal("#3a3f4a"));
            Box(lamp, new Vector3(0.42f, 3.48f, 0f), new Vector3(0.9f, 0.12f, 0.12f), MatMetal("#3a3f4a"));
            Box(lamp, new Vector3(0.8f, 3.36f, 0f), new Vector3(0.46f, 0.16f, 0.36f), MatMetal("#2f333c"));
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
            // 世界尺度 UV：贴图覆盖 w×h 米 → 正/背/侧四面同密度（侧面显示立面左侧裁切，不再被拉伸）
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(w, h, d), w, h), Vector3.zero, wallMat);
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
            else if (b.style == "gym")
            {
                // 大跨度体育馆：主体 + 半圆筒拱顶（圆柱沿 x 轴，下半埋入主体）
                var arch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                var ac = arch.GetComponent<Collider>();
                if (ac != null) Destroy(ac);
                arch.transform.SetParent(grp, false);
                arch.transform.localPosition = new Vector3(0f, h / 2f, 0f);
                arch.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
                arch.transform.localScale = new Vector3(d, w / 2f, d);
                arch.GetComponent<MeshRenderer>().sharedMaterial = ColMat(b.roof);
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
                Box(rp, new Vector3(-w * 0.22f, 0f, -d * 0.12f), new Vector3(1.7f, 0.75f, 1.2f), MatMetal("#a8adb5"));
                Box(rp, new Vector3(w * 0.24f, 0.03f, d * 0.1f), new Vector3(1f, 0.55f, 0.8f), MatMetal("#9aa2ac"));
                Box(rp, new Vector3(w * 0.24f, -0.28f, d * 0.1f), new Vector3(1.1f, 0.16f, 0.9f), MatMetal("#7d838c"));
            }
            if (b.vent)
            {
                var v = NewGo("vent", grp).transform;
                v.localPosition = new Vector3(w * 0.28f, h / 2f + 1.05f, 0f);
                Box(v, Vector3.zero, new Vector3(0.28f, 2f, 0.28f), MatMetal("#8a9099"));
                Box(v, new Vector3(0, 1.05f, 0), new Vector3(0.62f, 0.22f, 0.62f), MatMetal("#6f757e"));
            }
            if (b.wing)
            {
                float ww = 4.6f, wh = h * 0.72f;
                var wg = NewGo("wing", grp).transform;
                // 侧翼贴着地面（原点放地面，避免盒子悬空——此前从楼体半高继承导致浮空 0.7m）
                wg.localPosition = new Vector3(w / 2f + ww / 2f, -h / 2f, 0.6f);
                var wingMat = LitTex("wing_" + b.id + "_day");
                MeshObj(wg, MeshFactory.BoxUV(new Vector3(ww, wh, d * 0.8f), ww, wh), new Vector3(0, wh / 2f, 0), wingMat);
                WindowMatRefs.Add(wingMat);
                WindowMats.Add(new[] { "wing_" + b.id + "_day", "wing_" + b.id + "_night" });
                Box(wg, new Vector3(0, wh + 0.14f, 0), new Vector3(ww + 0.5f, 0.28f, d * 0.8f + 0.5f), ColMat(b.roof));
            }
            if (b.antenna)
            {
                var a = NewGo("antenna", grp).transform;
                a.localPosition = new Vector3(-w / 4f, h / 2f + 0.9f, 0f);
                Box(a, Vector3.zero, new Vector3(0.14f, 1.7f, 0.14f), MatMetal("#9aa2ac"));
                Box(a, new Vector3(0, 0.95f, 0), new Vector3(0.3f, 0.24f, 0.3f), ColMat("#c9553f"));
            }
            if (b.tank)
                Box(grp, new Vector3(w / 4f, h / 2f + 0.75f, 0f), new Vector3(1.7f, 0.95f, 1.3f), MatMetal("#9aa2ac"));
            if (b.awning)
                Box(grp, new Vector3(0, -h / 2f + 2.3f, d / 2f + 0.55f), new Vector3(w * 0.62f, 0.24f, 1.3f), LitTex("awning"));

            // 招牌 + 大门 + 台阶 + 门灯 + 雨落管 + 门前绿篱
            Face(grp, 3.3f, 0.83f, new Vector3(0, h * 0.28f, d / 2f + 0.04f), LitTex("sign_" + b.id));
            Face(grp, 1.5f, 2.1f, new Vector3(0, -h / 2f + 1.08f, d / 2f + 0.035f), LitTex("door"));
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(1.9f, 0.16f, 0.9f), TexScale.PathPeriod), new Vector3(0, -h / 2f + 0.08f, d / 2f + 0.5f), LitTex("path"));   // 石阶（铺装同材质）
            Box(grp, new Vector3(-1.05f, -h / 2f + 2.32f, d / 2f + 0.06f), new Vector3(0.2f, 0.28f, 0.14f), ColMatGloss("#ffdf9a", 0.4f), default, false);
            Box(grp, new Vector3(1.05f, -h / 2f + 2.32f, d / 2f + 0.06f), new Vector3(0.2f, 0.28f, 0.14f), ColMatGloss("#ffdf9a", 0.4f), default, false);
            Box(grp, new Vector3(-(w / 2f - 0.35f), 0f, d / 2f + 0.08f), new Vector3(0.16f, h - 0.5f, 0.16f), ColMatGloss(b.plinth, 0.22f));
            Box(grp, new Vector3(w / 2f - 0.35f, 0f, d / 2f + 0.08f), new Vector3(0.16f, h - 0.5f, 0.16f), ColMatGloss(b.plinth, 0.22f));
            // 门前绿篱：叶簇贴图（与树冠同材质体系），世界尺度 UV
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(1.3f, 0.62f, 0.55f), TexScale.LeafMeters), new Vector3(-1.95f, -h / 2f + 0.32f, d / 2f + 0.55f), LitTinted("leaves", "#3d7a37"));
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(1.3f, 0.62f, 0.55f), TexScale.LeafMeters), new Vector3(1.95f, -h / 2f + 0.32f, d / 2f + 0.55f), LitTinted("leaves", "#3d7a37"));
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

        MeshObj(grp, MeshFactory.BoxUV(new Vector3(w, h, d), w, h), Vector3.zero, body);
        Box(grp, new Vector3(0, y1, d / 4f + 0.15f), new Vector3(w + 2.6f, 0.26f, slope1), tile, new Vector3(a1 * Mathf.Rad2Deg, 0, 0));
        Box(grp, new Vector3(0, y1, -d / 4f - 0.15f), new Vector3(w + 2.6f, 0.26f, slope1), tile, new Vector3(-a1 * Mathf.Rad2Deg, 0, 0));
        MeshObj(grp, MeshFactory.BoxUV(new Vector3(w * 0.72f, 1.22f, d * 0.6f), w, h), new Vector3(0, y1 + 0.6f, 0), body);

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

        // 前院木篱（两横杆 + 立柱；正中留出进出家门的口子，不与门前通路冲突）
        foreach (float px in new[] { -3.0f, -2.0f, -1.0f, 1.0f, 2.0f, 3.0f })
            Box(grp, new Vector3(px, -h / 2f + 0.45f, d / 2f + 3f), new Vector3(0.14f, 0.9f, 0.14f), ColMat("#a87e52"));
        for (int i = 0; i < 2; i++)
        {
            float cx = i == 0 ? -2f : 2f;
            float len = 2.2f;
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
            BarkBox(grp, new Vector3(0, 0.4f, 0), new Vector3(0.36f, 0.8f, 0.36f), "#6b4a2f");
            LeafCone(grp, 1.05f, 1.2f, 1.15f - 0.6f, "#2f6231");
            LeafCone(grp, 0.85f, 1.1f, 1.85f - 0.55f, "#356e37");
            LeafCone(grp, 0.62f, 1.0f, 2.5f - 0.5f, "#3d7a3f");
        }
        else if (v == 7)
        {
            BarkBox(grp, new Vector3(0, 0.3f, 0), new Vector3(0.3f, 0.6f, 0.3f), "#5c4630");
            LeafCone(grp, 0.55f, 3.0f, 1.9f - 1.5f, "#284f2b");
        }
        else if (v == 8)
        {
            BarkBox(grp, new Vector3(0, 0.8f, 0), new Vector3(0.44f, 1.6f, 0.44f), "#6d4a2f");
            LeafBox(grp, new Vector3(0, 2.1f, 0), new Vector3(2.4f, 1.5f, 2.4f), "#4f9448");
            LeafBox(grp, new Vector3(0, 3.05f, 0), new Vector3(1.6f, 0.9f, 1.6f), "#5da04f");
        }
        else if (v == 9)
        {
            // 柳树：高干 + 三层浅绿垂冠（池畔）
            BarkBox(grp, new Vector3(0, 1.5f, 0), new Vector3(0.4f, 3f, 0.4f), "#7a6a45");
            LeafBox(grp, new Vector3(0, 2.6f, 0), new Vector3(2.4f, 0.7f, 2.4f), "#7fa84f");
            LeafBox(grp, new Vector3(0, 3.4f, 0), new Vector3(2.8f, 0.6f, 2.8f), "#8fb35a");
            LeafBox(grp, new Vector3(0, 4.05f, 0), new Vector3(1.9f, 0.6f, 1.9f), "#9cc06a");
        }
        else if (v == 10)
        {
            // 桃花：中干 + 粉色团冠（花园）
            BarkBox(grp, new Vector3(0, 0.9f, 0), new Vector3(0.36f, 1.8f, 0.36f), "#6d4a2f");
            LeafBox(grp, new Vector3(0, 2.3f, 0), new Vector3(2f, 1.7f, 2f), "#e8a8bd");
            LeafBox(grp, new Vector3(0, 3.25f, 0), new Vector3(1.3f, 0.8f, 1.3f), "#f0bccd");
        }
        else
        {
            bool tall = v == 1, wide = v == 2;
            float trunkH = tall ? 1.5f : 1f;
            Vector3 crown = wide ? new Vector3(2f, 1.4f, 2f) : tall ? new Vector3(1.4f, 1.8f, 1.4f) : new Vector3(1.5f, 1.3f, 1.5f);
            BarkBox(grp, new Vector3(0, trunkH / 2f, 0), new Vector3(0.36f, trunkH, 0.36f), "#6d4a2f");
            // 双色调树冠：暗底 + 主色 + 亮顶（立体受光感）+ 叶簇纹理
            string baseHex = TREE_CROWNS[v % TREE_CROWNS.Length];
            LeafBox(grp, new Vector3(0, trunkH + crown.y * 0.3f - 0.1f, 0), new Vector3(crown.x * 0.98f, crown.y * 0.6f, crown.z * 0.98f), MatLib.Shade(baseHex, 0.78f));
            LeafBox(grp, new Vector3(0, trunkH + crown.y * 0.74f - 0.1f, 0), new Vector3(crown.x * 0.92f, crown.y * 0.5f, crown.z * 0.92f), baseHex);
            LeafBox(grp, new Vector3(0, trunkH + crown.y - 0.18f, 0), new Vector3(crown.x * 0.6f, crown.y * 0.28f, crown.z * 0.6f), MatLib.Shade(baseHex, 1.2f));
        }
    }

    // 树冠块：世界尺度 UV（每面密度恒定 160px/米）+ 叶簇贴图 × 颜色
    GameObject LeafBox(Transform p, Vector3 pos, Vector3 size, string hex)
        => MeshObj(p, MeshFactory.BoxUV(size, TexScale.LeafMeters), pos, LitTinted("leaves", hex));

    // 树干块：树皮贴图 × 树色
    GameObject BarkBox(Transform p, Vector3 pos, Vector3 size, string hex)
        => MeshObj(p, MeshFactory.BoxUV(size, TexScale.BarkMeters), pos, LitTinted("bark", hex));

    // 松/柏的锥形树冠：锥面 UV 按周长/高度展开（密度同为 160px/米）
    void LeafCone(Transform p, float radius, float height, float baseY, string hex)
        => MeshObj(p, MeshFactory.Cone(radius, height, 6, true, TexScale.LeafMeters), new Vector3(0, baseY, 0), LitTinted("leaves", hex));

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
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(b.w, b.h, b.d), b.w, b.h), Vector3.zero, LitTex(texName));
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
        string g0 = BUSH_GREENS[i % 4];
        Box(grp, new Vector3(0, 0.24f, 0), new Vector3(0.95f, 0.5f, 0.95f), ColMat(MatLib.Shade(g0, 0.82f)));
        Box(grp, new Vector3(0.12f, 0.52f, -0.08f), new Vector3(0.55f, 0.36f, 0.55f), ColMat(BUSH_GREENS[(i + 1) % 4]));
        Box(grp, new Vector3(0.06f, 0.66f, 0.04f), new Vector3(0.3f, 0.14f, 0.3f), ColMat(MatLib.Shade(BUSH_GREENS[(i + 1) % 4], 1.16f)));
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
        // 精灵帧 UV 由 PlayerRig 逐帧写入四边形顶点（不依赖材质 ST，避免被覆盖导致采样跨格）
        MeshObj(sprite, MeshFactory.QuadZ(1.12f, 1.64f), Vector3.zero, sm, default, false, false);
        PlayerSprite = sprite.GetComponentInChildren<Renderer>();

        var sh = NewGo("shadow", grp).transform;
        sh.localPosition = new Vector3(0f, 0.03f, 0f);
        MeshObj(sh, MeshFactory.Quad(0.84f, 0.84f), Vector3.zero, MatLib.Shadow(new Color(0f, 0f, 0f, 0.22f)), default, false, false);
        PlayerShadow = sh.GetComponentInChildren<Renderer>();
    }
}
