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
    // 骑楼老街：这些店铺前加连续柱廊（数据仍是普通建筑，只在渲染时加构造）
    static readonly HashSet<string> ArcadeIds = new HashSet<string>
    { "o_silk", "o_tea", "o_grocery", "o_pharm", "o_post", "o_theater" };

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
        if (W.RIVER != null)
        {
            // 雪层同样避开河道（水面不积雪）
            MeshObj(go.transform, TerrainHoleMesh(TexScale.GrassPeriod, 0.028f), Vector3.zero, _snowLayerMat, default, false, false);
        }
        else
        {
            float reps = gs / TexScale.GrassPeriod;
            MeshObj(go.transform, MeshFactory.Quad(gs, gs, reps, reps), new Vector3(0, 0.028f, 0), _snowLayerMat, default, false, false);
        }
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

    GameObject Cylinder(Transform p, Vector3 pos, float radius, float height, Material m, Vector3 euler = default)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = new Vector3(radius * 2f, height / 2f, radius * 2f);
        go.GetComponent<MeshRenderer>().sharedMaterial = m;
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
                case "parking":
                    {
                        var reps = TexScale.Repeat(p.w, p.d, TexScale.CobblePeriod);
                        MeshObj(grp, MeshFactory.Quad(p.w, p.d, reps.x, reps.y), new Vector3(0, 0.02f, 0), LitTex("road_cobble"));
                        for (int k = 0; k < 6; k++)
                        {
                            float bx = -p.w / 2f + 2.2f + k * 4f;
                            Box(grp, new Vector3(bx, 0.026f, 0f), new Vector3(0.08f, 0.012f, p.d * 0.42f), ColMat("#dedacd"), default, false);
                        }
                        foreach (int s2 in new[] { -1, 1 })
                        {
                            var car = NewGo("car", grp).transform;
                            car.localPosition = new Vector3(-6f + s2 * 3.2f, 0f, s2 * 2.5f);
                            Box(car, new Vector3(0, 0.55f, 0), new Vector3(4f, 0.9f, 1.8f), ColMat(s2 < 0 ? "#8a4438" : "#3f5a6a"));
                            Box(car, new Vector3(0.2f, 1.15f, 0), new Vector3(2.2f, 0.5f, 1.6f), ColMat("#cfd8e2"));
                        }
                    }
                    break;
                case "crates":
                    {
                        var reps = TexScale.Repeat(p.w, p.d, TexScale.CobblePeriod);
                        MeshObj(grp, MeshFactory.Quad(p.w, p.d, reps.x, reps.y), new Vector3(0, 0.018f, 0), LitTex("road_cobble"));
                        var rnd = new System.Random(3);
                        for (int i = 0; i < 7; i++)
                        {
                            float bx = -4 + (float)rnd.NextDouble() * 8f, bz = -4 + (float)rnd.NextDouble() * 8f;
                            float s2 = 1.1f + (float)rnd.NextDouble() * 0.5f;
                            var cgo = MeshObj(grp, MeshFactory.BoxUV(new Vector3(s2, s2, s2), 2f),
                                new Vector3(bx, 0.02f + s2 / 2f, bz), LitTex("wood"),
                                new Vector3(0f, (float)rnd.NextDouble() * 40f, 0f));
                            if (i < 3)
                            {
                                var cbc = cgo.AddComponent<BoxCollider>();
                                cbc.size = new Vector3(s2, s2, s2);
                                var crb = cgo.AddComponent<Rigidbody>();
                                crb.mass = 26f;
                            }
                        }
                        for (int i = 0; i < 3; i++)
                            Cylinder(grp, new Vector3(4.5f, 0.62f, -3f + i * 2.2f), 0.45f, 1.2f, MatMetal("#6f757e"));
                    }
                    break;
                case "clocktower":
                    {
                        // 三段收分石塔：基座拱门 → 塔身（四棱台）→ 钟楼层 + 四棱锥顶
                        var stone = LitTex("bridge_stone");
                        MeshObj(grp, MeshFactory.BoxUV(new Vector3(5.4f, 0.8f, 5.4f), 2.4f), new Vector3(0f, 0.4f, 0f), stone);
                        MeshObj(grp, MeshFactory.BoxUV(new Vector3(4.6f, 4.4f, 4.6f), 2.4f), new Vector3(0f, 3.0f, 0f), stone);
                        Face(grp, 1.7f, 2.7f, new Vector3(0f, 2.15f, 2.31f), ColMat("#26221f"));
                        MeshObj(grp, MeshFactory.BoxUV(new Vector3(2.3f, 0.28f, 0.3f), 2.4f), new Vector3(0f, 3.6f, 2.4f), stone);
                        MeshObj(grp, MeshFactory.BoxUV(new Vector3(5.3f, 0.3f, 5.3f), 2.4f), new Vector3(0f, 5.35f, 0f), stone);
                        MeshObj(grp, MeshFactory.TaperedBox(4.0f, 4.0f, 3.3f, 3.3f, 5.6f, 2.4f), new Vector3(0f, 5.5f, 0f), stone);
                        MeshObj(grp, MeshFactory.BoxUV(new Vector3(4.1f, 0.26f, 4.1f), 2.4f), new Vector3(0f, 11.2f, 0f), stone);
                        MeshObj(grp, MeshFactory.BoxUV(new Vector3(3.3f, 3.1f, 3.3f), 2.4f), new Vector3(0f, 12.85f, 0f), stone);
                        var clockMat = MatLib.LitTexture(MatLib.Tex("clock"), 1f, 1f, 0.12f);
                        foreach (var (px, pz, ry) in new[] { (0f, 1.66f, 0f), (0f, -1.66f, 180f), (1.66f, 0f, 90f), (-1.66f, 0f, -90f) })
                            Face(grp, 1.62f, 1.62f, new Vector3(px, 13.1f, pz), clockMat, ry);
                        // 钟楼下小阳台
                        MeshObj(grp, MeshFactory.BoxUV(new Vector3(3.9f, 0.12f, 3.9f), 2.4f), new Vector3(0f, 11.45f, 0f), stone);
                        for (float x = -1.7f; x <= 1.71f; x += 0.85f)
                            foreach (int sz in new[] { -1, 1 })
                                Box(grp, new Vector3(x, 11.85f, sz * 1.85f), new Vector3(0.08f, 0.75f, 0.08f), MatMetal("#4a4f58"));
                        MeshObj(grp, MeshFactory.BoxUV(new Vector3(4.1f, 0.24f, 4.1f), 2.4f), new Vector3(0f, 14.5f, 0f), stone);
                        MeshObj(grp, MeshFactory.Cone(3.0f, 3.6f, 4, true), new Vector3(0f, 14.62f, 0f), ColMat("#3d444e"), new Vector3(0f, 45f, 0f));
                        Box(grp, new Vector3(0f, 18.55f, 0f), new Vector3(0.12f, 1.3f, 0.12f), MatMetal("#9aa2ac"));
                        Box(grp, new Vector3(0.28f, 18.9f, 0f), new Vector3(0.55f, 0.06f, 0.06f), MatMetal("#c9a24a"));
                    }
                    break;
                case "railway":
                    {
                        float len = p.len > 1f ? p.len : 200f;
                        var reps = TexScale.Repeat(len, 4.4f, TexScale.CobblePeriod);
                        MeshObj(grp, MeshFactory.Quad(len, 4.4f, reps.x, reps.y), new Vector3(0, 0.016f, 0), LitTex("road_cobble"));
                        for (float x = -len / 2f + 1f; x <= len / 2f - 1f; x += 1.5f)
                            MeshObj(grp, MeshFactory.BoxUV(new Vector3(0.28f, 0.12f, 2.8f), 2f),
                                new Vector3(x, 0.05f, 0f), LitTex("wood"), default, false, false);
                        foreach (int s2 in new[] { -1, 1 })
                            Box(grp, new Vector3(0f, 0.14f, s2 * 0.72f), new Vector3(len, 0.14f, 0.1f), MatMetal("#5a5a60"), default, false);
                    }
                    break;
                case "torii":
                    {
                        // 明神鸟居：柱 + 贯 + 岛木 + 两端上翘的笠木
                        float hw = p.w > 0f ? p.w / 2f : 5f;
                        foreach (int s2 in new[] { -1, 1 })
                        {
                            Box(grp, new Vector3(s2 * (hw - 0.6f), 3.1f, 0f), new Vector3(0.55f, 6.2f, 0.55f), ColMat("#a8382e"));
                            Box(grp, new Vector3(s2 * (hw - 0.6f), 0.25f, 0f), new Vector3(0.72f, 0.5f, 0.72f), ColMat("#2f2a28"));
                        }
                        Box(grp, new Vector3(0f, 5.2f, 0f), new Vector3(hw * 2f - 1.2f, 0.34f, 0.55f), ColMat("#b3453a"));
                        Box(grp, new Vector3(0f, 6.25f, 0f), new Vector3(hw * 2f, 0.28f, 0.9f), ColMat("#8f2f28"));
                        Box(grp, new Vector3(0f, 6.5f, 0f), new Vector3(hw * 2f + 0.5f, 0.16f, 0.55f), ColMat("#3a2f2b"));
                        foreach (int s2 in new[] { -1, 1 })
                            Box(grp, new Vector3(s2 * (hw + 0.28f), 6.62f, 0f), new Vector3(1.1f, 0.22f, 0.85f),
                                ColMat("#3a2f2b"), new Vector3(0f, 0f, -s2 * 12f));
                        Box(grp, new Vector3(0f, 5.75f, 0f), new Vector3(0.7f, 0.62f, 0.18f), ColMat("#f2e6d4"));
                        MeshObj(grp, MeshFactory.BoxUV(new Vector3(1.3f, 0.16f, 2.6f), 2f), new Vector3(0f, 0.08f, 0f), LitTex("path"));
                    }
                    break;
                case "stall":
                    {
                        Box(grp, new Vector3(0, 0.45f, 0), new Vector3(2.2f, 0.9f, 0.9f), LitTex("wood"));
                        Box(grp, new Vector3(0, 0.95f, 0), new Vector3(2.3f, 0.08f, 1f), ColMat("#d8cbb4"));
                        foreach (int sx in new[] { -1, 1 })
                            foreach (int sz in new[] { -1, 1 })
                                Box(grp, new Vector3(sx * 1.05f, 1.35f, sz * 0.95f), new Vector3(0.08f, 2.7f, 0.08f), ColMat("#8a6a48"));
                        Box(grp, new Vector3(0, 2.72f, 0.1f), new Vector3(2.5f, 0.08f, 2.2f), LitTex("awning"), new Vector3(8f, 0f, 0f));
                        Box(grp, new Vector3(-0.5f, 1.05f, 0), new Vector3(0.5f, 0.2f, 0.4f), ColMat("#c9553f"));
                        Box(grp, new Vector3(0.55f, 1.05f, 0), new Vector3(0.4f, 0.2f, 0.35f), ColMat("#4a6a8a"));
                    }
                    break;
                case "ride_wheel":
                    {
                        var metal = MatMetal("#8a9099");
                        foreach (int sx in new[] { -1, 1 })
                            foreach (int sz in new[] { -1, 1 })
                                Box(grp, new Vector3(sx * 1.6f, 4.75f, sz * 2.6f), new Vector3(0.3f, 9.5f, 0.3f), metal);
                        Box(grp, new Vector3(0f, 9.5f, 0f), new Vector3(3.4f, 0.32f, 0.32f), metal);
                        var wheel = NewGo("wheel", grp).transform;
                        wheel.localPosition = new Vector3(0f, 9.5f, 0f);
                        wheel.gameObject.AddComponent<Spin>().DegPerSec = 6f;
                        const float R = 8.2f;
                        for (int i = 0; i < 16; i++)
                        {
                            float a = i / 16f * Mathf.PI * 2f;
                            Box(wheel, new Vector3(0f, Mathf.Cos(a) * R, Mathf.Sin(a) * R), new Vector3(0.2f, 3.3f, 0.2f),
                                ColMat("#c9553f"), new Vector3(a * Mathf.Rad2Deg + 90f, 0f, 0f));
                        }
                        for (int i = 0; i < 8; i++)
                        {
                            float a = i / 8f * Mathf.PI * 2f;
                            Box(wheel, new Vector3(0f, Mathf.Cos(a) * R / 2f, Mathf.Sin(a) * R / 2f), new Vector3(0.12f, R, 0.12f),
                                ColMat("#d8d2c4"), new Vector3(a * Mathf.Rad2Deg, 0f, 0f));
                        }
                        for (int i = 0; i < 12; i++)
                        {
                            float a = i / 12f * Mathf.PI * 2f;
                            Box(grp, new Vector3(0f, 9.5f + Mathf.Cos(a) * R - 0.5f, Mathf.Sin(a) * R), new Vector3(0.95f, 0.8f, 0.9f),
                                ColMat(i % 3 == 0 ? "#e8c46a" : (i % 3 == 1 ? "#5a8fc8" : "#c9553f")));
                        }
                        Box(grp, new Vector3(0f, 0.12f, 0f), new Vector3(6f, 0.24f, 7f), ColMat("#b8b0a2"));
                    }
                    break;
                case "ride_carousel":
                    {
                        Cylinder(grp, new Vector3(0f, 0.18f, 0f), 5.1f, 0.36f, ColMat("#b8ac96"));
                        Cylinder(grp, new Vector3(0f, 3.2f, 0f), 0.42f, 6.4f, ColMat("#8f8a80"));
                        var spin = NewGo("spin", grp).transform;
                        spin.gameObject.AddComponent<Spin>().DegPerSec = 9f;
                        MeshObj(spin, MeshFactory.Cone(5.4f, 2.4f, 16, true, 2f), new Vector3(0f, 4.1f, 0f), LitTex("awning"));
                        for (int i = 0; i < 8; i++)
                        {
                            float a = i / 8f * Mathf.PI * 2f;
                            float px = Mathf.Cos(a) * 3.5f, pz = Mathf.Sin(a) * 3.5f;
                            Box(spin, new Vector3(px, 1.9f, pz), new Vector3(0.1f, 3.8f, 0.1f), ColMat("#d8d2c4"));
                            Box(spin, new Vector3(px, 1.5f, pz), new Vector3(1.2f, 0.6f, 0.45f),
                                ColMat(i % 2 == 0 ? "#f2efe6" : "#c8a06a"), new Vector3(0f, -a * Mathf.Rad2Deg, 0f));
                        }
                    }
                    break;
                case "ride_swing":
                    {
                        Box(grp, new Vector3(0f, 0.2f, 0f), new Vector3(5f, 0.4f, 5f), ColMat("#b8b0a2"));
                        Box(grp, new Vector3(0f, 4.6f, 0f), new Vector3(0.7f, 9.2f, 0.7f), MatMetal("#8a9099"));
                        var spin = NewGo("spin", grp).transform;
                        spin.localPosition = new Vector3(0f, 9f, 0f);
                        spin.gameObject.AddComponent<Spin>().DegPerSec = 16f;
                        Cylinder(spin, Vector3.zero, 3.5f, 0.3f, MatMetal("#6f757e"));
                        for (int i = 0; i < 8; i++)
                        {
                            float a = i / 8f * Mathf.PI * 2f;
                            float px = Mathf.Cos(a) * 3.1f, pz = Mathf.Sin(a) * 3.1f;
                            Box(spin, new Vector3(px, -1.4f, pz), new Vector3(0.06f, 2.8f, 0.06f), MatMetal("#4a4f58"));
                            Box(spin, new Vector3(px, -2.9f, pz), new Vector3(0.7f, 0.12f, 0.5f), ColMat("#c9553f"));
                        }
                    }
                    break;
                case "ride_tower":
                    {
                        Box(grp, new Vector3(0f, 0.2f, 0f), new Vector3(4.2f, 0.4f, 4.2f), ColMat("#b8b0a2"));
                        Box(grp, new Vector3(0f, 8.4f, 0f), new Vector3(0.9f, 16.4f, 0.9f), MatMetal("#8a9099"));
                        for (int i = 0; i < 6; i++)
                            Box(grp, new Vector3(0f, 2.2f + i * 2.4f, 0f), new Vector3(1.05f, 0.18f, 1.05f), ColMat("#c9553f"));
                        Box(grp, new Vector3(0f, 16.9f, 0f), new Vector3(1.6f, 0.5f, 1.6f), ColMat("#4a4f58"));
                        Box(grp, new Vector3(0f, 5.2f, 0f), new Vector3(2.6f, 1.8f, 2.6f), ColMat("#c9553f"));
                        Box(grp, new Vector3(0f, 6.15f, 0f), new Vector3(2.4f, 0.14f, 2.4f), ColMat("#f2efe6"));
                    }
                    break;
                case "ride_train":
                    {
                        MeshObj(grp, MeshFactory.Ring(6.2f, 2.9f, 6.6f, 3.3f, 40, 2f), new Vector3(0f, 0.03f, 0f), MatMetal("#6f757e"));
                        for (int i = 0; i < 24; i++)
                        {
                            float a = i / 24f * Mathf.PI * 2f;
                            MeshObj(grp, MeshFactory.BoxUV(new Vector3(0.5f, 0.08f, 0.25f), 2f),
                                new Vector3(Mathf.Cos(a) * 6.4f, 0.05f, Mathf.Sin(a) * 3.1f), LitTex("wood"),
                                new Vector3(0f, -a * Mathf.Rad2Deg, 0f), false, false);
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            float a = i * 2.1f;
                            Box(grp, new Vector3(Mathf.Cos(a) * 6.4f, 0.5f, Mathf.Sin(a) * 3.1f), new Vector3(1.5f, 0.8f, 0.9f),
                                ColMat(i == 0 ? "#c9553f" : (i == 1 ? "#e8c46a" : "#5a8fc8")), new Vector3(0f, -a * Mathf.Rad2Deg, 0f));
                        }
                        Box(grp, new Vector3(7.8f, 0.6f, 0f), new Vector3(2.4f, 1.2f, 2f), ColMat("#d8cbb4"));
                        Box(grp, new Vector3(7.8f, 1.4f, 0f), new Vector3(2.8f, 0.14f, 2.4f), ColMat("#8a4438"));
                    }
                    break;
                case "ride_pendulum":
                    {
                        Box(grp, new Vector3(0f, 0.2f, 0f), new Vector3(6f, 0.4f, 4f), ColMat("#b8b0a2"));
                        foreach (int sx in new[] { -1, 1 })
                        {
                            Box(grp, new Vector3(sx * 2.2f, 3.9f, -0.9f), new Vector3(0.35f, 7.8f, 0.35f), MatMetal("#8a9099"), new Vector3(18f, 0f, 0f));
                            Box(grp, new Vector3(sx * 2.2f, 3.9f, 0.9f), new Vector3(0.35f, 7.8f, 0.35f), MatMetal("#8a9099"), new Vector3(-18f, 0f, 0f));
                        }
                        Box(grp, new Vector3(0f, 7.6f, 0f), new Vector3(5f, 0.3f, 0.3f), MatMetal("#6f757e"));
                        var arm = NewGo("arm", grp).transform;
                        arm.localPosition = new Vector3(0f, 7.4f, 0f);
                        arm.localEulerAngles = new Vector3(28f, 0f, 0f);
                        Box(arm, new Vector3(0f, -2.8f, 0f), new Vector3(0.35f, 5.6f, 0.35f), MatMetal("#8a9099"));
                        Box(arm, new Vector3(0f, -5.6f, 0f), new Vector3(2.2f, 1.6f, 1.6f), ColMat("#c9553f"));
                    }
                    break;
                case "ride_tent":
                    {
                        Cylinder(grp, new Vector3(0f, 2f, 0f), 6.4f, 4f, ColMat("#e6dcc6"));
                        MeshObj(grp, MeshFactory.Cone(7.2f, 5f, 16, true, 2f), new Vector3(0f, 4f, 0f), LitTex("awning"));
                        Box(grp, new Vector3(0f, 1.3f, 6.3f), new Vector3(2f, 2.6f, 0.3f), ColMat("#8a4438"));
                        Box(grp, new Vector3(0f, 9.2f, 0f), new Vector3(0.12f, 1.4f, 0.12f), MatMetal("#8a9099"));
                        Box(grp, new Vector3(0.3f, 9.7f, 0f), new Vector3(0.9f, 0.5f, 0.06f), ColMat("#c9553f"), default, false);
                    }
                    break;
                case "ride_cart":
                    {
                        Box(grp, new Vector3(0f, 0.95f, 0f), new Vector3(2.8f, 1.3f, 1.4f), ColMat("#f2efe6"));
                        Box(grp, new Vector3(0f, 0.55f, 0f), new Vector3(2.9f, 0.3f, 1.45f), ColMat("#c9553f"));
                        Box(grp, new Vector3(0f, 1.85f, 0.2f), new Vector3(3f, 0.08f, 1.9f), LitTex("awning"), new Vector3(10f, 0f, 0f));
                        foreach (int sx in new[] { -1, 1 })
                            foreach (int sz in new[] { -1, 1 })
                                Cylinder(grp, new Vector3(sx * 0.9f, 0.35f, sz * 0.75f), 0.35f, 0.18f, ColMat("#2f333c"), new Vector3(90f, 0f, 0f));
                    }
                    break;
                case "ride_stage":
                    {
                        MeshObj(grp, MeshFactory.BoxUV(new Vector3(6f, 0.6f, 4f), 2f), new Vector3(0f, 0.3f, 0f), LitTex("wood"));
                        Box(grp, new Vector3(0f, 2.2f, -1.9f), new Vector3(6f, 3.6f, 0.2f), ColMat("#3f4a54"));
                        foreach (int sx in new[] { -1, 1 })
                        {
                            Box(grp, new Vector3(sx * 2.4f, 1.7f, -1.7f), new Vector3(0.5f, 2.2f, 0.5f), ColMat("#2f333c"));
                            Box(grp, new Vector3(sx * 2.2f, 2.6f, 1.8f), new Vector3(1.2f, 0.18f, 0.5f), ColMat("#4a4f58"));
                        }
                        Box(grp, new Vector3(0f, 4.1f, -1.9f), new Vector3(6.2f, 0.2f, 0.5f), ColMat("#c9553f"));
                    }
                    break;
                case "kart":
                    {
                        float rx = (p.w > 0f ? p.w : 14f) / 2f, rz = (p.d > 0f ? p.d : 10f) / 2f;
                        MeshObj(grp, MeshFactory.Disc(rx, rz, 40, 4f), new Vector3(0f, 0.02f, 0f), ColMat("#3f4248"));
                        MeshObj(grp, MeshFactory.Ring(rx - 0.35f, rz - 0.35f, rx, rz, 48, 2f), new Vector3(0f, 0.03f, 0f), ColMat("#e8e3d6"));
                        for (int i = 0; i < 10; i++)
                        {
                            float a = i / 10f * Mathf.PI * 2f;
                            Box(grp, new Vector3(Mathf.Cos(a) * (rx - 0.9f), 0.25f, Mathf.Sin(a) * (rz - 0.9f)),
                                new Vector3(0.5f, 0.5f, 0.5f), ColMat("#2f333c"));
                        }
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
            LitTex("roadband_v"), LitTex("roadband_h"), LitTex("bridge_stone"), LitTex("wood"), LitTex("water"), GrassMat,
        };
        wth.Init(Player, tod, wet, LeafMats, _snowLayerR, _snowLayerMat);
    }

    public WorldData BuildWorld()
    {
        // 默认进入小镇（V5 总平面）；-campus 回到原校园世界
        bool campus = Args.Has("-campus");
        W = campus ? CampusData.Build() : TownData.Build();
        if (W == null) return null;
        TextureLibrary.SetWorld(W);
        if (W.RIVER != null) Spawn = new Vector3(-14f, 0f, -4f);   // 钟楼广场
        Root = NewGo("World", transform).transform;

        Sky();
        GroundAndPaths();
        River();
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
        if (W.RIVER != null)
        {
            // 小镇：地形在河道处断开（下沉河床），UV 由网格按世界坐标给出
            GrassMat = LitTex("grass");
            var groundMesh = TerrainHoleMesh(TexScale.GrassPeriod, 0f);
            var groundGo = MeshObj(Root, groundMesh, Vector3.zero, GrassMat);
            var groundCol = groundGo.AddComponent<MeshCollider>();
            groundCol.sharedMesh = groundMesh;
        }
        else
        {
            var greps = TexScale.Repeat(gs, gs, TexScale.GrassPeriod);
            GrassMat = LitTex("grass", greps.x, greps.y);
            Flat(Root, gs, gs, 0f, GrassMat, greps.x, greps.y);
        }
        // 路网：路面 + 路缘石 + 抬高人行道（12cm，人物可站上去）
        var g = NewGo("Roads", Root).transform;
        foreach (var r in W.ROADS)
        {
            // 与河相交的南北向道路在水面处分段（桥面另铺，不再浮在河上）
            var spans = new List<(float a, float b)> { (r.from, r.to) };
            if (W.RIVER != null && r.axis == "z")
            {
                float zc = RZ(r.at);
                if (r.from < zc - W.RIVER.width / 2f && r.to > zc + W.RIVER.width / 2f)
                {
                    spans.Clear();
                    spans.Add((r.from, zc - W.RIVER.width / 2f - 0.6f));
                    spans.Add((zc + W.RIVER.width / 2f + 0.6f, r.to));
                }
            }
            string tex = r.kind == "mainstone" ? "road_mainstone" : (r.kind == "cobble" ? "road_cobble" : "path");
            var roadMat = LitTex(tex);
            var crossGaps = r.sidewalk ? XRoadGaps(r) : new List<(float, float)>();

            foreach (var (a, b) in spans)
            {
                float len = b - a;
                if (len < 0.3f) continue;
                float mid = (a + b) / 2f;
                float y = r.axis == "z" ? 0.02f : 0.024f;
                var go = NewGo(r.id, g);
                var rep = TexScale.Repeat(r.width, len, TexScale.RoadPeriod(r.kind));
                var mesh = r.axis == "z"
                    ? MeshFactory.Quad(r.width, len, rep.x, rep.y)
                    : MeshFactory.Quad(len, r.width, rep.y, rep.x);
                MeshObj(go.transform, mesh,
                    r.axis == "z" ? new Vector3(r.at, y, mid) : new Vector3(mid, y, r.at), roadMat);

                if (r.sidewalk)
                {
                    // 人行道在路口处断开（按交叉路宽度留口）
                    foreach (var (wa, wb) in SplitByGaps(a, b, crossGaps))
                    {
                        float wlen = wb - wa;
                        if (wlen < 0.4f) continue;
                        float wmid = (wa + wb) / 2f;
                        foreach (int sgn in new[] { -1, 1 })
                        {
                            float edge = sgn * (r.width / 2f);
                            // 路缘石（12cm 高、18cm 宽）
                            var curbPos = r.axis == "z"
                                ? new Vector3(r.at + edge + sgn * 0.09f, 0.06f, wmid)
                                : new Vector3(wmid, 0.06f, r.at + edge + sgn * 0.09f);
                            var curbSize = r.axis == "z" ? new Vector3(0.18f, 0.12f, wlen) : new Vector3(wlen, 0.12f, 0.18f);
                            MeshObj(go.transform, MeshFactory.BoxUV(curbSize, 1.2f), curbPos, ColMatGloss("#c8c2b4", 0.12f));
                            // 人行道面（比路面高 12cm）
                            const float sw = 1.3f;
                            float off = edge + sgn * (0.18f + sw / 2f);
                            var wrep = TexScale.Repeat(sw, wlen, TexScale.PathPeriod);
                            var wmesh = r.axis == "z"
                                ? MeshFactory.Quad(sw, wlen, wrep.x, wrep.y)
                                : MeshFactory.Quad(wlen, sw, wrep.y, wrep.x);
                            MeshObj(go.transform, wmesh,
                                r.axis == "z" ? new Vector3(r.at + off, 0.12f, wmid) : new Vector3(wmid, 0.12f, r.at + off),
                                LitTex("path"));
                        }
                    }
                }
                else
                {
                    // 参道等整幅铺装：两侧压边条
                    foreach (int sgn in new[] { -1, 1 })
                    {
                        float off = sgn * (r.width / 2f - 0.09f);
                        var kmesh = r.axis == "z" ? MeshFactory.Quad(0.18f, len, 1f, rep.y) : MeshFactory.Quad(len, 0.18f, rep.y, 1f);
                        MeshObj(go.transform, kmesh,
                            r.axis == "z" ? new Vector3(r.at + off, y + 0.004f, mid) : new Vector3(mid, y + 0.004f, r.at + off),
                            ColMat("#bfb9ab"), default, false, false);
                    }
                }

                if (r.kind == "mainstone" && (r.width >= 8f || W.RIVER == null))
                {
                    // 中线石带（带宽 0.6m，纵向每 1m 一道接缝）
                    var bandMesh = r.axis == "z" ? MeshFactory.Quad(0.6f, len, 1f, len) : MeshFactory.Quad(len, 0.6f, len, 1f);
                    MeshObj(go.transform, bandMesh,
                        r.axis == "z" ? new Vector3(r.at, y + 0.006f, mid) : new Vector3(mid, y + 0.006f, r.at),
                        LitTex(r.axis == "z" ? "roadband_v" : "roadband_h"), default, false, false);
                }
            }
        }
    }

    // 横向道路在本路方向上的开口（人行道/路缘断开用）
    List<(float, float)> XRoadGaps(RoadDef r)
    {
        var gaps = new List<(float, float)>();
        foreach (var o in W.ROADS)
        {
            if (o.axis == r.axis) continue;
            if (r.at < o.from - 1f || r.at > o.to + 1f) continue;
            gaps.Add((o.at - o.width / 2f - 1.4f, o.at + o.width / 2f + 1.4f));
        }
        gaps.Sort((p, q) => p.Item1.CompareTo(q.Item1));
        return gaps;
    }

    // 从 [a,b] 中扣掉若干开口，返回剩余区间
    static List<(float, float)> SplitByGaps(float a, float b, List<(float, float)> gaps)
    {
        var outp = new List<(float, float)>();
        float cur = a;
        foreach (var (gs, ge) in gaps)
        {
            if (ge <= cur || gs >= b) continue;
            if (gs > cur + 0.3f) outp.Add((cur, gs));
            cur = Mathf.Max(cur, ge);
        }
        if (b > cur + 0.3f) outp.Add((cur, b));
        return outp;
    }

    // 可站立高度：桥面 > 人行道 12cm > 车行道 > 草地 0 —— 人物脚底跟随，不再穿进路缘
    public float GroundY(float x, float z)
    {
        if (W.RIVER != null)
            foreach (var b in W.RIVER.bridges)
            {
                float span = W.RIVER.width + 1.6f;
                if (Mathf.Abs(x - b.x) <= b.width / 2f + 0.2f && Mathf.Abs(z - RZ(b.x)) <= span / 2f + 1.2f)
                    return 0.05f;
            }
        foreach (var r in W.ROADS)
        {
            float along = r.axis == "z" ? z : x;
            float perp = r.axis == "z" ? x - r.at : z - r.at;
            if (along < r.from - 0.3f || along > r.to + 0.3f) continue;
            float ap = Mathf.Abs(perp);
            if (ap <= r.width / 2f + 0.03f) return r.axis == "z" ? 0.02f : 0.024f;
            if (!r.sidewalk || ap > r.width / 2f + 1.48f) continue;
            bool gap = false;
            foreach (var o in W.ROADS)
            {
                if (o.axis == r.axis) continue;
                if (r.at < o.from - 1f || r.at > o.to + 1f) continue;
                if (along > o.at - o.width / 2f - 1.4f && along < o.at + o.width / 2f + 1.4f) { gap = true; break; }
            }
            if (!gap) return 0.12f;
        }
        return 0f;
    }

    // ── 河道（小镇）：下沉河床 + 石护岸 + 滨河步道 + 三座桥 ──
    float RZ(float x) => W.RIVER == null ? 0f
        : W.RIVER.amp * Mathf.Sin((x + W.RIVER.phase) / W.RIVER.freq) + W.RIVER.offset;

    // 河道处断开的地形（北片 + 南片合成一张网格）
    Mesh TerrainHoleMesh(float period, float y)
    {
        float gs = W.GROUND_SIZE > 10 ? W.GROUND_SIZE : 200f;
        float half = W.RIVER.width / 2f;
        var north = MeshFactory.Strip(x => -gs / 2f, x => RZ(x) - half, -gs / 2f, gs / 2f, 220, period, y);
        var south = MeshFactory.Strip(x => RZ(x) + half, x => gs / 2f, -gs / 2f, gs / 2f, 220, period, y);
        var ci = new CombineInstance[2];
        ci[0].mesh = north; ci[0].transform = Matrix4x4.identity;
        ci[1].mesh = south; ci[1].transform = Matrix4x4.identity;
        var m = new Mesh();
        m.CombineMeshes(ci, true, false);
        return m;
    }

    void River()
    {
        var r = W.RIVER;
        if (r == null) return;
        var g = NewGo("River", Root).transform;
        float half = r.width / 2f;
        const int steps = 240;

        // 河床（水面由 WaterSim 每帧生成）
        MeshObj(g, MeshFactory.Strip(x => RZ(x) - half, x => RZ(x) + half, r.from, r.to, steps, 4f, -2.45f),
            Vector3.zero, ColMat("#2c3f42"), default, false, false);

        foreach (int sgn in new[] { -1, 1 })
        {
            float edge = sgn * half;
            // 石护岸（砌筑缝贴图；贴图底部勒脚正好在水线附近）——带碰撞体，浮木不会穿岸
            var wallMesh = MeshFactory.RibbonWall(x => RZ(x) + edge, r.from, r.to, steps, 0f, -2.45f, 2.4f);
            var wallGo = MeshObj(g, wallMesh, Vector3.zero, LitTex("concrete"));
            var wc = wallGo.AddComponent<MeshCollider>();
            wc.sharedMesh = wallMesh;
            // 岸顶压顶石（0.45m 宽，浅色条石）
            float a = sgn * half, b = sgn * (half + 0.45f);
            MeshObj(g, MeshFactory.Strip(x => RZ(x) + Mathf.Min(a, b), x => RZ(x) + Mathf.Max(a, b), r.from, r.to, steps, 2.4f, 0.022f),
                Vector3.zero, LitTex("bridge_stone"), default, false, true);
            // 滨河步道（距中心线 12.5m、宽 5m，随河蜿蜒）
            float c = sgn * 12.5f;
            MeshObj(g, MeshFactory.Strip(x => RZ(x) + c - 2.5f, x => RZ(x) + c + 2.5f, r.from, r.to, steps, TexScale.PathPeriod, 0.012f),
                Vector3.zero, LitTex("path"));
        }

        foreach (var b in r.bridges)
        {
            if (b.covered) CoveredBridge(g, b, r);
            else StoneBridge(g, b, r);
        }

        // 实时水面（高度场 + 顺流；桥墩/岸线反射与泡沫）
        var waterGo = NewGo("WaterSurface", g);
        var sim = waterGo.AddComponent<WaterSim>();
        sim.Build(r);

        // 物理浮物：系泊小船 + 顺流漂木
        FloatingProps(g, r);
    }

    // 小船与漂木（Rigidbody + 浮力 + 顺流）
    void FloatingProps(Transform parent, RiverDef r)
    {
        // 系泊小船：靠着南岸，随波起伏
        var boat = NewGo("boat", parent).transform;
        boat.localPosition = new Vector3(-26f, -1.55f, RZ(-26f) + 3.4f);
        boat.localEulerAngles = new Vector3(0f, 90f, 0f);
        MeshObj(boat, MeshFactory.BoxUV(new Vector3(1.5f, 0.42f, 4.4f), 2f), Vector3.zero, LitTex("wood"));
        MeshObj(boat, MeshFactory.BoxUV(new Vector3(1.15f, 0.12f, 3.9f), 2f), new Vector3(0f, 0.18f, 0f), ColMat("#5a4430"));
        foreach (int s2 in new[] { -1, 1 })
        {
            MeshObj(boat, MeshFactory.BoxUV(new Vector3(0.28f, 0.34f, 1.2f), 2f),
                new Vector3(0f, 0.24f, s2 * 1.1f), LitTex("wood"));
            MeshObj(boat, MeshFactory.BoxUV(new Vector3(2.1f, 0.12f, 0.24f), 2f),
                new Vector3(0f, 0.28f, s2 * 1.95f), LitTex("wood"));
        }
        var brb = boat.gameObject.AddComponent<Rigidbody>();
        brb.mass = 220f; brb.drag = 0.8f; brb.angularDrag = 1.4f;
        var bcol = boat.gameObject.AddComponent<BoxCollider>();
        bcol.size = new Vector3(1.5f, 0.42f, 4.4f);
        var buoy = boat.gameObject.AddComponent<Buoyant>(); buoy.FloatHeight = 0.36f;

        // 漂木（木箱）——顺流而下，到尽头回到上游
        var rnd = new System.Random(20261022);
        for (int i = 0; i < 4; i++)
        {
            float x = -92f + i * 42f;
            var crate = NewGo("drift_crate", parent).transform;
            float s2 = 0.55f + (float)rnd.NextDouble() * 0.25f;
            crate.localPosition = new Vector3(x, -1.75f, RZ(x) + (float)(rnd.NextDouble() * 6.0 - 3.0));
            crate.localEulerAngles = new Vector3(0f, (float)rnd.NextDouble() * 90f, 0f);
            MeshObj(crate, MeshFactory.BoxUV(new Vector3(s2, s2, s2), 2f), Vector3.zero, LitTex("wood"));
            var rb = crate.gameObject.AddComponent<Rigidbody>();
            rb.mass = 18f; rb.drag = 0.5f; rb.angularDrag = 1f;
            var bc = crate.gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(s2, s2, s2);
            crate.gameObject.AddComponent<Buoyant>().FloatHeight = 0.3f;
            crate.gameObject.AddComponent<Drifter>();
        }

        // 落叶（更轻、更多）
        for (int i = 0; i < 5; i++)
        {
            float x = -70f + i * 34f;
            var leaf = NewGo("drift_leaf", parent).transform;
            leaf.localPosition = new Vector3(x, -1.9f, RZ(x) + (float)(rnd.NextDouble() * 7.0 - 3.5));
            leaf.localEulerAngles = new Vector3(0f, (float)rnd.NextDouble() * 180f, 0f);
            MeshObj(leaf, MeshFactory.Quad(0.5f, 0.32f), Vector3.zero, ColMat(i % 2 == 0 ? "#6f9a4a" : "#c9a24a"), default, false, false);
            var rb = leaf.gameObject.AddComponent<Rigidbody>();
            rb.mass = 1.2f; rb.drag = 0.6f; rb.angularDrag = 0.8f;
            var bc = leaf.gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.5f, 0.08f, 0.32f);
            leaf.gameObject.AddComponent<Buoyant>().FloatHeight = 0.22f;
            leaf.gameObject.AddComponent<Drifter>();
        }
    }

    // 石拱桥：桥面 + 两侧透空拱券面片 + 桥墩 + 栏杆望柱
    void StoneBridge(Transform parent, BridgeDef b, RiverDef r)
    {
        var g = NewGo("stone_bridge", parent).transform;
        g.localPosition = new Vector3(b.x, 0f, RZ(b.x));
        float w = b.width;
        float span = r.width + 1.6f;                 // 覆盖 16m 水面 + 两端各 0.8m 桥台
        float bay = span / b.arches;                 // 每跨面片（含半墩）
        const float faceH = 2.42f;

        var deckGo = MeshObj(g, MeshFactory.BoxUV(new Vector3(w, 0.24f, span + 1.2f), 2.4f),
            new Vector3(0f, -0.07f, 0f), LitTex("bridge_stone"));
        var deckCol = deckGo.AddComponent<BoxCollider>();
        deckCol.size = new Vector3(w, 0.24f, span + 1.2f);

        // 真实拱券面：面片网格直接挖出半椭圆拱洞（无裁切贴图），内外两面都可见
        float holeW = bay * 0.62f, holeH = faceH * 0.86f;
        var faceMesh = MeshFactory.ArchFace(span, faceH, b.arches, holeW, holeH, 2.4f);
        foreach (int sgn in new[] { -1, 1 })
        {
            var face = NewGo("face", g);
            face.transform.localPosition = new Vector3(sgn * (w / 2f - 0.05f), -faceH / 2f + 0.02f, 0f);
            face.transform.localEulerAngles = new Vector3(0f, sgn * 90f, 0f);
            face.AddComponent<MeshFilter>().sharedMesh = faceMesh;
            var mr = face.AddComponent<MeshRenderer>();
            mr.sharedMaterial = LitTex("bridge_stone");
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
        // 拱券石（沿拱洞内缘的楔石，略出挑——侧面看是一圈明确的拱环）
        foreach (int sgn in new[] { -1, 1 })
        {
            float fx = sgn * (w / 2f - 0.02f);
            for (int k = 0; k < b.arches; k++)
            {
                float cz = -span / 2f + bay * (k + 0.5f);
                for (int i = 0; i <= 18; i++)
                {
                    float t = Mathf.PI * i / 18f;
                    float z = cz + Mathf.Cos(t) * (holeW / 2f + 0.13f);
                    float yb = -faceH + 0.02f + Mathf.Sin(t) * (holeH + 0.12f);
                    float phi = Mathf.Atan2(-Mathf.Cos(t) * holeH, -Mathf.Sin(t) * holeW / 2f) * Mathf.Rad2Deg;
                    Box(g, new Vector3(fx, yb, z), new Vector3(0.26f, 0.26f, 0.62f),
                        ColMatGloss("#b5ab9c", 0.10f), new Vector3(phi, 0f, 0f));
                }
            }
        }

        // 桥墩（每跨分界处，正好压住面片接缝）
        for (int k = 1; k < b.arches; k++)
        {
            float z = -span / 2f + k * bay;
            var pierGo = MeshObj(g, MeshFactory.BoxUV(new Vector3(w * 0.98f, 2.3f, 1.1f), 2.4f),
                new Vector3(0f, -1.3f, z), LitTex("bridge_stone"));
            var pierCol = pierGo.AddComponent<BoxCollider>();
            pierCol.size = new Vector3(w * 0.98f, 2.3f, 1.1f);
        }
        // 两端桥台
        foreach (int sgn in new[] { -1, 1 })
            MeshObj(g, MeshFactory.BoxUV(new Vector3(w + 0.6f, 1.1f, 2.2f), 2.4f),
                new Vector3(0f, -0.55f, sgn * (span / 2f + 0.6f)), LitTex("bridge_stone"));

        // 石栏杆 + 望柱 + 压顶
        foreach (int sgn in new[] { -1, 1 })
        {
            float x = sgn * (w / 2f - 0.16f);
            MeshObj(g, MeshFactory.BoxUV(new Vector3(0.32f, 0.85f, span + 1.2f), 2.4f),
                new Vector3(x, 0.48f, 0f), LitTex("bridge_stone"));
            MeshObj(g, MeshFactory.BoxUV(new Vector3(0.44f, 0.12f, span + 1.4f), 0.8f),
                new Vector3(x, 0.96f, 0f), ColMatGloss("#ddd6c8", 0.16f));
            for (int k = 0; k <= 4; k++)
            {
                float z = -(span + 1.2f) / 2f + (span + 1.2f) * k / 4f;
                MeshObj(g, MeshFactory.BoxUV(new Vector3(0.4f, 1.15f, 0.4f), 2.4f),
                    new Vector3(x, 0.62f, z), LitTex("bridge_stone"));
                MeshObj(g, MeshFactory.BoxUV(new Vector3(0.46f, 0.1f, 0.46f), 0.8f),
                    new Vector3(x, 1.22f, z), ColMatGloss("#ddd6c8", 0.16f));
            }
        }

        // 桥下水面阴影
        MeshObj(g, MeshFactory.Quad(w * 0.9f, span * 0.85f), new Vector3(0f, -1.94f, 0f),
            MatLib.Shadow(new Color(0f, 0f, 0f, 0.20f)), default, false, false);
    }

    // 木廊桥：木板桥面 + 木柱 + 双坡瓦顶（东段地标）
    void CoveredBridge(Transform parent, BridgeDef b, RiverDef r)
    {
        var g = NewGo("covered_bridge", parent).transform;
        g.localPosition = new Vector3(b.x, 0f, RZ(b.x));
        float w = b.width;
        float span = r.width + 1.6f;

        var wdeck = MeshObj(g, MeshFactory.BoxUV(new Vector3(w, 0.2f, span), 2f), new Vector3(0f, -0.05f, 0f), LitTex("wood"));
        var wdeckCol = wdeck.AddComponent<BoxCollider>();
        wdeckCol.size = new Vector3(w, 0.2f, span);
        foreach (int sx in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
                MeshObj(g, MeshFactory.BoxUV(new Vector3(0.3f, 2.4f, 0.3f), 2f),
                    new Vector3(sx * (w / 2f - 0.4f), -1.25f, sz * (span / 2f - 1.4f)), LitTex("wood"));

        const int n = 7;
        for (int k = 0; k <= n; k++)
        {
            float z = -span / 2f + span * k / n;
            foreach (int sx in new[] { -1, 1 })
                Box(g, new Vector3(sx * (w / 2f - 0.22f), 1.35f, z), new Vector3(0.2f, 2.6f, 0.2f), ColMat("#8a6a48"));
        }
        foreach (int sx in new[] { -1, 1 })
        {
            Box(g, new Vector3(sx * (w / 2f - 0.22f), 2.55f, 0f), new Vector3(0.26f, 0.26f, span), ColMat("#7a5a3e"));
            Box(g, new Vector3(sx * (w / 2f - 0.22f), 1.05f, 0f), new Vector3(0.16f, 0.14f, span), ColMat("#8a6a48"));
        }
        // 双坡瓦顶（沿桥长方向起脊）
        const float ang = 0.42f;
        float half = (w + 1.8f) / 2f;
        float slope = half / Mathf.Cos(ang);
        float roofY = 3.35f;
        MeshObj(g, MeshFactory.BoxUV(new Vector3(slope, 0.22f, span + 1.4f), 4f, 2f),
            new Vector3(-half / 2f, roofY, 0f), LitTex("roof_villa"), new Vector3(0f, 0f, -ang * Mathf.Rad2Deg));
        MeshObj(g, MeshFactory.BoxUV(new Vector3(slope, 0.22f, span + 1.4f), 4f, 2f),
            new Vector3(half / 2f, roofY, 0f), LitTex("roof_villa"), new Vector3(0f, 0f, ang * Mathf.Rad2Deg));
        Box(g, new Vector3(0f, roofY + half / 2f * Mathf.Tan(ang), 0f), new Vector3(0.4f, 0.24f, span + 1.6f), ColMat("#2b3038"));
        // 顶棚（从桥下看是木色天花，不是黑色屋顶底面）
        MeshObj(g, MeshFactory.BoxUV(new Vector3(w - 0.2f, 0.06f, span), 2f), new Vector3(0f, 2.62f, 0f), LitTex("wood"));

        MeshObj(g, MeshFactory.Quad(w * 0.9f, span * 0.8f), new Vector3(0f, -1.94f, 0f),
            MatLib.Shadow(new Color(0f, 0f, 0f, 0.20f)), default, false, false);
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
                MeshObj(c.transform, MeshFactory.Quad(rh.width + 3f, rv.width + 3f, creps.x, creps.y),
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
        if (W.RIVER != null)
        {
            // 小镇池塘：椭圆水面（世界 UV，与河水同密度）+ 石沿环 + 岸边点石
            float rx = p.r, rz = p.r * 0.7f;
            MeshObj(t, MeshFactory.Disc(rx, rz, 56, 4f), new Vector3(0f, 0.012f, 0f), LitTex("water"), default, false, true);
            MeshObj(t, MeshFactory.Ring(rx, rz, rx + 0.65f, rz + 0.65f, 56, 2.4f), new Vector3(0f, 0.024f, 0f), LitTex("bridge_stone"));
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f + 0.4f;
                Box(t, new Vector3(Mathf.Cos(a) * (rx + 1.1f), 0.18f, Mathf.Sin(a) * (rz + 1.1f)),
                    new Vector3(0.9f, 0.4f, 0.8f), ColMat("#8d8d94"), new Vector3(0f, a * Mathf.Rad2Deg, 0f));
            }
        }
        else
        {
            Flat(t, (p.r + 0.6f) * 2f, (p.r + 0.6f) * 2f, 0.012f, MatLib.Sprite("pond", 0.55f));
            Box(t, new Vector3(p.r - 0.5f, 0.25f, 0.9f), new Vector3(0.9f, 0.5f, 0.7f), ColMat("#8d8d94"));
            Box(t, new Vector3(-1.7f, 0.2f, p.r - 0.4f), new Vector3(0.7f, 0.4f, 0.6f), ColMat("#7a7a82"));
        }
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
            var bodyCol = grp.gameObject.AddComponent<BoxCollider>();
            bodyCol.center = Vector3.zero;
            bodyCol.size = new Vector3(w, h, d);
            if (b.style == "shrine") { ShrineHall(grp, b); continue; }

            bool plain = b.style == "plain";   // 摊亭/站台这类：无立面细节，纯色体块
            var wallMat = plain ? ColMat(b.wall) : LitTex("wall_" + b.id + "_day");
            // 世界尺度 UV：贴图覆盖 w×h 米 → 正/背/侧四面同密度（侧面显示立面左侧裁切，不再被拉伸）
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(w, h, d), plain ? 2.4f : w, plain ? 2.4f : h), Vector3.zero, wallMat);
            if (!plain)
            {
                WindowMatRefs.Add(wallMat);
                WindowMats.Add(new[] { "wall_" + b.id + "_day", "wall_" + b.id + "_night" });
            }

            var tile = plain ? ColMat(b.roof) : LitTex("roof_" + b.id, 4f, 2f);

            if (plain)
            {
                Box(grp, new Vector3(0, h / 2f + 0.14f, 0), new Vector3(w + 0.35f, 0.28f, d + 0.35f), ColMat(b.roof));
            }
            else if (b.style == "gable")
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
                // 平屋顶：深色防水屋面 + 四周女儿墙（不再是整块浅色板）
                Box(grp, new Vector3(0, h / 2f + 0.14f, 0), new Vector3(w + 0.5f, 0.28f, d + 0.5f), ColMat("#4a4e56"));
                float py = h / 2f + 0.42f;
                Box(grp, new Vector3(0, py, d / 2f + 0.12f), new Vector3(w + 0.7f, 0.42f, 0.24f), ColMat(b.plinth));
                Box(grp, new Vector3(0, py, -d / 2f - 0.12f), new Vector3(w + 0.7f, 0.42f, 0.24f), ColMat(b.plinth));
                Box(grp, new Vector3(w / 2f + 0.12f, py, 0), new Vector3(0.24f, 0.42f, d + 0.7f), ColMat(b.plinth));
                Box(grp, new Vector3(-w / 2f - 0.12f, py, 0), new Vector3(0.24f, 0.42f, d + 0.7f), ColMat(b.plinth));
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

            // 招牌 + 大门 + 台阶 + 门灯 + 雨落管 + 门前绿篱（纯色体块不配这些）
            if (plain) continue;
            var signTex = TextureLibrary.ByName("sign_" + b.id, false, false);
            if (signTex != null)
                Face(grp, 3.3f, 0.83f, new Vector3(0, h * 0.28f, d / 2f + 0.04f), MatLib.LitTexture(signTex, 1f, 1f, 0.12f));
            Face(grp, 1.5f, 2.1f, new Vector3(0, -h / 2f + 1.08f, d / 2f + 0.035f), LitTex("door"));
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(1.9f, 0.16f, 0.9f), TexScale.PathPeriod), new Vector3(0, -h / 2f + 0.08f, d / 2f + 0.5f), LitTex("path"));   // 石阶（铺装同材质）
            Box(grp, new Vector3(-1.05f, -h / 2f + 2.32f, d / 2f + 0.06f), new Vector3(0.2f, 0.28f, 0.14f), ColMatGloss("#ffdf9a", 0.4f), default, false);
            Box(grp, new Vector3(1.05f, -h / 2f + 2.32f, d / 2f + 0.06f), new Vector3(0.2f, 0.28f, 0.14f), ColMatGloss("#ffdf9a", 0.4f), default, false);
            Box(grp, new Vector3(-(w / 2f - 0.35f), 0f, d / 2f + 0.08f), new Vector3(0.16f, h - 0.5f, 0.16f), ColMatGloss(b.plinth, 0.22f));
            Box(grp, new Vector3(w / 2f - 0.35f, 0f, d / 2f + 0.08f), new Vector3(0.16f, h - 0.5f, 0.16f), ColMatGloss(b.plinth, 0.22f));
            // 门前绿篱：叶簇贴图（与树冠同材质体系），世界尺度 UV
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(1.3f, 0.62f, 0.55f), TexScale.LeafMeters), new Vector3(-1.95f, -h / 2f + 0.32f, d / 2f + 0.55f), LitTinted("leaves", "#3d7a37"));
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(1.3f, 0.62f, 0.55f), TexScale.LeafMeters), new Vector3(1.95f, -h / 2f + 0.32f, d / 2f + 0.55f), LitTinted("leaves", "#3d7a37"));
            if (ArcadeIds.Contains(b.id)) Arcade(grp, b);
        }
    }

    // ── 神社本殿/拜殿：基坛 + 柱网 + 深出檐双坡屋顶 + 斗栱/椽头 + 注连绳 ──
    void ShrineHall(Transform grp, BuildingDef b)
    {
        float w = b.size[0], h = b.size[1], d = b.size[2];
        float baseY = -h / 2f;
        var stone = LitTex("bridge_stone");
        var wood = ColMat("#7a4a34");
        var woodDark = ColMat("#5a3226");
        var roof = LitTex("roof_" + b.id);
        var shoji = LitTex("villa");

        // 基坛 + 木地板 + 正面三级踏步
        MeshObj(grp, MeshFactory.BoxUV(new Vector3(w + 2.4f, 0.55f, d + 2.4f), 2.4f), new Vector3(0f, baseY + 0.275f, 0f), stone);
        MeshObj(grp, MeshFactory.BoxUV(new Vector3(w + 1.6f, 0.2f, d + 1.6f), 2f), new Vector3(0f, baseY + 0.65f, 0f), LitTex("wood"));
        for (int i = 0; i < 3; i++)
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(3.4f, 0.16f, 0.5f), 2.4f),
                new Vector3(0f, baseY + 0.08f + i * 0.16f, d / 2f + 1.4f - i * 0.42f), stone);

        // 柱网
        float py = baseY + 0.75f;
        float ph = 2.9f;
        for (int ix = -1; ix <= 1; ix++)
            foreach (int iz in new[] { -1, 1 })
                MeshObj(grp, MeshFactory.BoxUV(new Vector3(0.36f, ph, 0.36f), 2f),
                    new Vector3(ix * (w / 2f - 0.7f), py + ph / 2f, iz * (d / 2f - 0.7f)), wood);

        // 背墙 / 侧墙 / 正面障子与门
        MeshObj(grp, MeshFactory.BoxUV(new Vector3(w - 1.4f, ph * 0.94f, 0.2f), 2f),
            new Vector3(0f, py + ph * 0.47f, -(d / 2f - 0.7f)), woodDark);
        foreach (int sx in new[] { -1, 1 })
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(0.2f, ph * 0.94f, d - 1.4f), 2f),
                new Vector3(sx * (w / 2f - 0.7f), py + ph * 0.47f, 0f), woodDark);
        foreach (int sx in new[] { -1, 1 })
            Face(grp, Mathf.Max(1.6f, w * 0.26f), ph * 0.78f, new Vector3(sx * (w * 0.24f), py + ph * 0.5f, d / 2f - 0.62f), shoji);
        Face(grp, 1.3f, 2.0f, new Vector3(0f, py + 1.0f, d / 2f - 0.6f), LitTex("door"));

        // 楣梁 + 斗栱
        MeshObj(grp, MeshFactory.BoxUV(new Vector3(w + 0.7f, 0.34f, d + 0.7f), 2f), new Vector3(0f, py + ph + 0.17f, 0f), wood);
        for (float x = -w / 2f + 0.8f; x <= w / 2f - 0.6f; x += 1.6f)
            foreach (int sz in new[] { -1, 1 })
                MeshObj(grp, MeshFactory.BoxUV(new Vector3(0.32f, 0.34f, 0.62f), 1.6f),
                    new Vector3(x, py + ph + 0.45f, sz * (d / 2f + 0.15f)), woodDark);

        // 深出檐双坡屋顶 + 山墙端封 + 椽头 + 金饰
        const float ang = 0.45f;
        float over = 1.5f;
        float halfD = d / 2f + over;
        float slope = halfD / Mathf.Cos(ang);
        float eaveY = py + ph + 0.9f;
        float ridgeY = eaveY + halfD * Mathf.Tan(ang);
        foreach (int sz in new[] { -1, 1 })
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(w + 3.2f, 0.28f, slope), 4f, 2f),
                new Vector3(0f, eaveY + halfD * Mathf.Tan(ang) * 0.5f, sz * halfD / 2f), roof,
                new Vector3(-sz * ang * Mathf.Rad2Deg, 0f, 0f));
        Box(grp, new Vector3(0f, ridgeY + 0.12f, 0f), new Vector3(w + 3.4f, 0.26f, 0.5f), ColMat("#2b2622"));
        float gH = halfD * Mathf.Tan(ang) + 0.28f;
        foreach (int sx in new[] { -1, 1 })
        {
            var tri = NewGo("sg", grp).transform;
            tri.localPosition = new Vector3(sx * (w / 2f + 1.5f), eaveY, 0f);
            tri.localEulerAngles = new Vector3(0f, 90f, 0f);
            MeshObj(tri, MeshFactory.Triangle(d + 3f, gH), Vector3.zero, ColMat("#c8b48e"));
        }
        for (float x = -w / 2f; x <= w / 2f; x += 0.9f)
            foreach (int sz in new[] { -1, 1 })
                Box(grp, new Vector3(x, eaveY - 0.08f, sz * (d / 2f + over * 0.72f)), new Vector3(0.12f, 0.12f, 0.52f), ColMat("#8a6a48"));
        foreach (int sx in new[] { -1, 1 })
            Box(grp, new Vector3(sx * (w / 2f + 1.6f), ridgeY + 0.28f, 0f), new Vector3(0.4f, 0.5f, 0.6f), ColMat("#c9a24a"));

        // 注连绳 + 纸垂
        Cylinder(grp, new Vector3(0f, py + ph - 0.12f, d / 2f + 0.55f), 0.12f, w * 0.62f, ColMat("#d8c8a0"), new Vector3(0f, 0f, 90f));
        for (int i = 0; i < 4; i++)
            Box(grp, new Vector3(-w * 0.2f + i * w * 0.13f, py + ph - 0.45f, d / 2f + 0.58f),
                new Vector3(0.18f, 0.55f, 0.03f), ColMat("#f2efe6"));
    }

    // ── 骑楼：店铺前的连续柱廊 + 二层挑廊 + 灯笼/招牌 ──
    void Arcade(Transform grp, BuildingDef b)
    {
        float w = b.size[0], h = b.size[1], d = b.size[2];
        float baseY = -h / 2f;
        const float depth = 1.7f;
        float zf = d / 2f + depth / 2f;
        var stone = LitTex("concrete");

        // 一层柱列
        for (float x = -w / 2f + 0.5f; x <= w / 2f - 0.4f; x += 2.2f)
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(0.34f, 3.2f, 0.34f), 2f),
                new Vector3(x, baseY + 1.6f, d / 2f + depth - 0.25f), stone);
        // 二层挑廊板 + 檐口小瓦顶
        MeshObj(grp, MeshFactory.BoxUV(new Vector3(w + 0.3f, 0.26f, depth), 2.4f),
            new Vector3(0f, baseY + 3.32f, zf), stone);
        MeshObj(grp, MeshFactory.BoxUV(new Vector3(w + 0.5f, 0.16f, 0.9f), 4f, 2f),
            new Vector3(0f, baseY + 3.52f, d / 2f + depth - 0.2f), LitTex("roof_" + b.id), new Vector3(-12f, 0f, 0f));
        // 栏杆
        for (float x = -w / 2f + 0.4f; x <= w / 2f - 0.3f; x += 1.1f)
            Box(grp, new Vector3(x, baseY + 3.85f, d / 2f + depth - 0.1f), new Vector3(0.08f, 0.8f, 0.08f), ColMat("#8a6a48"));
        MeshObj(grp, MeshFactory.BoxUV(new Vector3(w + 0.3f, 0.1f, 0.16f), 1.2f),
            new Vector3(0f, baseY + 4.28f, d / 2f + depth - 0.1f), ColMat("#a87e52"));
        // 灯笼 + 悬挂招牌
        for (float x = -w / 2f + 1.2f; x <= w / 2f - 1.0f; x += 3.1f)
        {
            Cylinder(grp, new Vector3(x, baseY + 2.8f, d / 2f + depth - 0.55f), 0.16f, 0.42f, ColMat("#b8352f"));
            Box(grp, new Vector3(x, baseY + 3.06f, d / 2f + depth - 0.55f), new Vector3(0.05f, 0.5f, 0.05f), ColMat("#3a2f28"));
            Box(grp, new Vector3(x + 1.4f, baseY + 2.35f, d / 2f + depth - 0.18f), new Vector3(0.06f, 0.7f, 0.06f), ColMat("#3a2f28"));
            Face(grp, 0.7f, 0.5f, new Vector3(x + 1.4f, baseY + 2.85f, d / 2f + depth - 0.14f), ColMat("#d8cbb4"), 90f);
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
        var villaCol = grp.gameObject.AddComponent<BoxCollider>();
        villaCol.center = Vector3.zero;
        villaCol.size = new Vector3(w, h, d);

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
        var trunkCol = grp.gameObject.AddComponent<CapsuleCollider>();
        trunkCol.center = new Vector3(0f, 1.6f, 0f);
        trunkCol.height = 3.2f;
        trunkCol.radius = 0.34f;
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
        var prb = grp.gameObject.AddComponent<Rigidbody>();
        prb.isKinematic = true;
        prb.useGravity = false;
        var pcap = grp.gameObject.AddComponent<CapsuleCollider>();
        pcap.center = new Vector3(0f, 0.9f, 0f);
        pcap.height = 1.8f;
        pcap.radius = 0.32f;

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
