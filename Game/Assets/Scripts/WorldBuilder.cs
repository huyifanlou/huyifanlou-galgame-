using System.Collections.Generic;
using UnityEngine;

// 世界构建器：把 world.json + 导出的 PNG 还原成 web v0.40 的校园（几何/材质配比逐条对位）
// 所有尺寸/坐标/颜色均取自 web 版 CampusScene.jsx 的实参；改动请同步 docs/01 与迁移说明。
public class WorldBuilder : MonoBehaviour
{
    public WorldData W { get; private set; }
    public Transform Root { get; private set; }
    public Transform Player { get; private set; }
    public Renderer PlayerShadow { get; private set; }
    public Material LampGlass { get; private set; }
    public Material LampPool { get; private set; }
    public Material SkyDayMat { get; private set; }
    public GameObject SkyDayGo { get; private set; }
    public Material SkyNightMat { get; private set; }
    public Transform SkyNightTr { get; private set; }
    // 昼夜双窗户：{material, 白天贴图名, 夜晚贴图名}——由 TimeOfDay 在夜里切换
    public readonly List<string[]> WindowMats = new List<string[]>();
    public readonly List<Material> WindowMatRefs = new List<Material>();
    public readonly List<Material> LeafMats = new List<Material>();   // 叶簇材质（天气：雪天覆白）
    public readonly List<Light> LampLights = new List<Light>();       // 路灯点光源（夜里由 TimeOfDay 点亮）
    public Material WindowGlowMat;                                    // 亮窗发光片的共享材质
    public readonly List<Renderer> WindowGlowRenders = new List<Renderer>();
    public Material WindowGlassMat;                                   // 店铺橱窗玻璃（夜里透暖光）
    public readonly List<Material> SignMatRefs = new List<Material>(); // 店铺招牌（夜里开自发光）
    public Material DoorLampMat;                                      // 门灯共享材质
    public Material LanternMat;                                       // 骑楼红灯笼（夜里发光）
    Material _warmLanternMat;                                         // 祭典暖白纸提灯（无光照常亮）
    public readonly List<Renderer> SignGlowRenders = new List<Renderer>(); // 夜间招牌自发光片
    public readonly List<float> LampLightPower = new List<float>();         // 点光源夜间功率（骑楼灯比路灯亮）
    public Transform WindowFXRoot;                                    // 窗玻璃/亮窗单独根，避免被建筑静态合批
    public Material GrassMat;                                        // 草地材质（天气：雨天变暗）
    public Vector3 Spawn = new Vector3(0f, 0f, 4f);

    readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();
    Mesh[,] _cherryCrowns;                  // 樱花树冠：[变体10-13, 0-2 色阶]，MC 式体素叶块
    Mesh[] _cherryBarks;                    // 樱树树干+枝条（每变体一份，体素原木块）
    Mesh[] _giantCrowns;                    // 中央巨樱叶块（3 色阶）
    Mesh _giantBark;                        // 中央巨樱树干+主枝
    Mesh _bloomCanopyA, _bloomCanopyB;      // 花坛小花团（不是树冠，保留低模圆团）
    const float CherryVox = 0.2f;           // 樱花体素边长（原 0.8m 的 25%）
    const int CherrySub = 4;                // 布局仍按 0.8m 逻辑块写，生成时细分成 0.2m 体素
    // 《我的世界》大型樱花分枝：起点高度 / 水平角 / 长度 / 上升格数（逻辑 0.8m 块）
    static readonly (int y, int ang, int len, int rise)[] GiantBranchDefs =
    {
        (5, 10, 10, 3), (6, 55, 8, 3), (7, 100, 9, 4), (9, 190, 8, 4), (10, 280, 8, 4)
    };
    // 冠团：与枝端冠一起叠成宽而蓬松的伞形冠（x / y / z / 半宽 / 盖顶）
    static readonly (int x, int y, int z, int hw, bool peak)[] GiantCanopyBlobs =
    {
        (4, 13, 3, 3, false), (-5, 14, 3, 3, false), (-4, 15, -5, 3, false),
        (4, 14, -4, 3, false), (0, 14, 0, 5, false),
        (0, 17, 0, 7, false), (5, 19, 2, 4, false), (-5, 20, 1, 4, false),
        (2, 20, -5, 4, false), (-2, 20, 4, 3, false),
        (6, 19, 0, 4, false), (-6, 19, 0, 4, false),
        (0, 19, 6, 4, false), (0, 19, -6, 4, false),
        (7, 22, 2, 3, false), (-7, 22, -2, 3, false),
        (2, 22, 7, 3, false), (-2, 22, -7, 3, false),
        (3, 22, 3, 3, false), (-4, 22, -2, 3, false), (0, 22, 0, 4, false),
        (2, 24, -2, 3, false), (-2, 24, 2, 2, false), (4, 24, 0, 2, false),
        (0, 27, 1, 2, false), (-1, 27, -2, 2, false), (3, 27, -2, 1, false),
        (0, 29, 0, 1, true)
    };
    static readonly string[] TREE_CROWNS = { "#3b6b3a", "#457a42", "#4f8044", "#5a8845", "#c08fa5", "#ad9155" };
    static readonly string[] BUSH_GREENS = { "#3a6a3a", "#345f34", "#427a40", "#2e5a2f" };
    static readonly string[] ROCK_COLS = { "#8d8d94", "#7b7b82", "#9aa0a6", "#6f6f76", "#84848b" };
    static readonly string[] REED_COLS = { "#7a8a4a", "#6a7a42", "#85925a" };
    static readonly string[] REED_COLS2 = { "#85925a", "#7a8a4a", "#6a7a42" };
    // 骑楼老街：这些店铺前加连续柱廊（数据仍是普通建筑，只在渲染时加构造）
    static readonly HashSet<string> ArcadeIds = new HashSet<string>
    { "o_silk", "o_tea", "o_grocery", "o_pharm", "o_post", "o_theater" };
    static readonly HashSet<string> ShopIds = new HashSet<string>
    { "o_silk", "o_tea", "o_grocery", "o_pharm", "o_post", "r_milktea", "r_market", "r_rest", "r_photo", "r_book", "r_conv" };
    static string ShopStripeA(string id)
    {
        switch (id)
        {
            case "o_silk": return "#8a4438";
            case "o_tea": return "#4a6a4a";
            case "o_grocery": return "#8a6a3c";
            case "o_pharm": return "#3f6a52";
            case "o_post": return "#3f5a6a";
            case "r_milktea": return "#c07aa8";
            case "r_market": return "#3f6a9a";
            case "r_rest": return "#c9553f";
            case "r_photo": return "#5a5a72";
            case "r_book": return "#8a5a3c";
            case "r_conv": return "#4a8a4a";
            default: return "#c9553f";
        }
    }

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
    // 樱花冠材质：叶簇贴图 × 花色 + 少量自发光，避免仰视时整团花冠发黑
    Material CherryMat(string hex)
    {
        string k = "CH|" + hex;
        if (!_cache.TryGetValue(k, out var m))
        {
            m = MatLib.Lit("leaves", 2.5f, 2.5f);
            m.color = MatLib.Hex(hex);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", MatLib.Hex(hex) * 0.30f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            _cache[k] = m;
            if (!LeafMats.Contains(m)) LeafMats.Add(m);
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

    // 圆柱（喷泉石盘/立柱用；pos 为圆柱中心，radius/height 为实际米数）
    GameObject Cyl(Transform p, Vector3 pos, float radius, float height, Material m, bool shadow = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
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
            if (p.type == "ride_wheel") grp.gameObject.AddComponent<ProximityHide>().Radius = 10f;
            else if (p.type == "ride_carousel") grp.gameObject.AddComponent<ProximityHide>().Radius = 6.5f;
            else if (p.type == "ride_tent") grp.gameObject.AddComponent<ProximityHide>().Radius = 9f;
            else if (p.type == "ride_train") grp.gameObject.AddComponent<ProximityHide>().Radius = 8f;
            else if (p.type == "ride_swing") grp.gameObject.AddComponent<ProximityHide>().Radius = 5f;
            else if (p.type == "ride_tower") grp.gameObject.AddComponent<ProximityHide>().Radius = 5f;
            else if (p.type == "ride_pendulum") grp.gameObject.AddComponent<ProximityHide>().Radius = 5f;
            switch (p.type)
            {
                case "plaza_round":
                    {
                        // 抬高到人行道/车道之上：原来圆盘比路面低，路口会以直边切掉圆盘；
                        // 同心石带 + 8 条放射线做出广场自己的铺装逻辑
                        const float top = 0.030f;
                        MeshObj(grp, MeshFactory.Disc(p.r, p.r, 72, TexScale.PathPeriod),
                            new Vector3(0, top, 0), LitTex("path"));
                        MeshObj(grp, MeshFactory.Ring(p.r - 0.9f, p.r - 0.9f, p.r, p.r, 72, 2.4f),
                            new Vector3(0, top + 0.004f, 0), LitTex("bridge_stone"));
                        MeshObj(grp, MeshFactory.Ring(6.6f, 6.6f, 7.0f, 7.0f, 64, 2.4f),
                            new Vector3(0, top + 0.003f, 0), LitTex("bridge_stone"));
                        MeshObj(grp, MeshFactory.Ring(4.9f, 4.9f, 5.2f, 5.2f, 56, 2.4f),
                            new Vector3(0, top + 0.003f, 0), LitTex("bridge_stone"));
                        float bandLen = p.r - 0.9f - 7.0f;
                        for (int i = 0; i < 8; i++)
                        {
                            float a = i * Mathf.PI / 4f;
                            float mid = (7.0f + (p.r - 0.9f)) * 0.5f;
                            MeshObj(grp, MeshFactory.Quad(0.22f, bandLen, 0.22f / 2.4f, bandLen / 2.4f),
                                new Vector3(Mathf.Cos(a) * mid, top + 0.003f, Mathf.Sin(a) * mid),
                                LitTex("bridge_stone"), new Vector3(0f, 90f - a * Mathf.Rad2Deg, 0f));
                        }
                    }
                    break;
                case "plaza":
                    {
                        // 世界对齐 UV：多块铺装相接时石纹连续；高度压到路面/环塘路之下，避免盖住其他地面
                        var mesh = MeshFactory.PlazaQuad(p.w, p.d, TexScale.PathPeriod, p.x, p.z, p.ry);
                        MeshObj(grp, mesh, new Vector3(0, 0.010f, 0), LitTex("path"));
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
                        Box(grp, new Vector3(0, 0.3f, 0), new Vector3(p.w - 0.3f, 0.22f, p.d - 0.3f), ColMat("#3f7239"));
                        Box(grp, new Vector3(0, 0.44f, 0), new Vector3(p.w - 0.6f, 0.14f, p.d - 0.6f),
                            ColMat(p.kind == "green" ? "#4a8743" : "#e8748a"));
                    }
                    break;
                case "fountain":
                    Fountain(grp, p.r);
                    break;
                case "sundial":
                    {
                        var stone = ColMat("#b8b0a2");
                        var bronze = ColMatGloss("#8a7040", 0.45f);
                        Box(grp, new Vector3(0f, 0.16f, 0f), new Vector3(1.9f, 0.32f, 1.9f), stone);
                        Box(grp, new Vector3(0f, 0.82f, 0f), new Vector3(0.62f, 1.0f, 0.62f), ColMat("#a8a094"));
                        Cylinder(grp, new Vector3(0f, 1.4f, 0f), 0.78f, 0.12f, bronze, new Vector3(18f, 0f, 0f));
                        Box(grp, new Vector3(0f, 1.75f, 0f), new Vector3(0.06f, 0.95f, 0.06f), bronze, new Vector3(32f, 0f, 0f));
                    }
                    break;
                case "lantern":
                    StoneLantern(grp);
                    break;
                case "komainu":
                    Komainu(grp);
                    break;
                case "ema":
                    EmaRack(grp);
                    break;
                case "omikuji":
                    OmikujiStand(grp);
                    break;
                case "temizuya":
                    TemizuyaBasin(grp);
                    break;
                case "sake":
                    SakeBarrels(grp);
                    break;
                case "offering":
                    OfferingBox(grp);
                    break;
                case "office":
                    ShrineOffice(grp);
                    break;
                case "festival_board":
                    FestivalBoard(grp);
                    break;
                case "nobori":
                    Nobori(grp, p.kind != "white");
                    break;
                case "chochin_string":
                    ChochinString(grp, p.len > 1f ? p.len : 10f, 7);
                    break;
                case "flowerbed":
                    FlowerBed(grp, p.w, p.d);
                    break;
                case "giant_cherry":
                    GiantCherry(grp);
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
                        Face(grp, 4.8f, 1.1f, new Vector3(0, 4.6f, -0.5f), LitTex("sign_gate"), 180f);
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
                        // 注连绳 + 纸垂 + 两盏大提灯
                        Box(grp, new Vector3(0f, 5.02f, 0f), new Vector3(hw * 2f - 2.0f, 0.18f, 0.24f), ColMat("#d8c8a0"));
                        for (int i = 0; i < 5; i++)
                        {
                            float sx = -hw + 2.0f + i * ((hw * 2f - 4.0f) / 4f);
                            Box(grp, new Vector3(sx, 4.72f, 0.03f), new Vector3(0.18f, 0.5f, 0.03f), ColMat("#f2efe6"), default, false);
                        }
                        foreach (int s in new[] { -1, 1 })
                            Chochin(grp, new Vector3(s * 3.0f, 5.55f, 0f), 0.28f, 0.6f, true);
                    }
                    break;
                case "stall":
                    {
                        // 夏日祭摊位：按 kind 变化棚布配色、货品、招牌（不再千篇一律）
                        string kind = string.IsNullOrEmpty(p.kind) ? "food" : p.kind;
                        string colA, colB;
                        switch (kind)
                        {
                            case "goldfish": colA = "#3f6a9a"; colB = "#e8e2d2"; break;
                            case "food": colA = "#c9553f"; colB = "#f2ead8"; break;
                            case "goods": colA = "#4a8a4a"; colB = "#f0ead6"; break;
                            case "candy": colA = "#c07aa8"; colB = "#f6eef2"; break;
                            case "drinks": colA = "#3f8a8a"; colB = "#eef2ee"; break;
                            case "grill": colA = "#8a4438"; colB = "#efe2cc"; break;
                            case "masks": colA = "#7a4a8a"; colB = "#f2ead8"; break;
                            case "games": colA = "#c9a24a"; colB = "#f4f0e2"; break;
                            case "ice": colA = "#5a8fc8"; colB = "#f2f6fa"; break;
                            case "lantern": colA = "#b8352f"; colB = "#f6ead6"; break;
                            case "takoyaki": colA = "#c9553f"; colB = "#f6ead6"; break;
                            case "yakisoba": colA = "#b8352f"; colB = "#f2ead8"; break;
                            case "apple": colA = "#c05a7a"; colB = "#f8ecf0"; break;
                            case "shooting": colA = "#4a6a3a"; colB = "#f0ead6"; break;
                            case "water": colA = "#3f7ab0"; colB = "#eef4f8"; break;
                            default: colA = "#c9553f"; colB = "#f2ead8"; break;
                        }
                        var cloth = MatLib.LitTexture(TextureLibrary.Stripes2(colA, colB), 1f, 1f, 0.1f);
                        var woodMat = LitTex("wood");
                        float cw = 2.2f, cd = 0.9f;

                        // 柜台 + 台面 + 立柱 + 棚布
                        MeshObj(grp, MeshFactory.BoxUV(new Vector3(cw, 0.9f, cd), 2f), new Vector3(0f, 0.45f, 0f), woodMat);
                        MeshObj(grp, MeshFactory.BoxUV(new Vector3(cw + 0.1f, 0.08f, cd + 0.1f), 2f), new Vector3(0f, 0.94f, 0f), LitTex("wood"));
                        foreach (int sx in new[] { -1, 1 })
                            foreach (int sz in new[] { -1, 1 })
                                MeshObj(grp, MeshFactory.BoxUV(new Vector3(0.08f, 2.7f, 0.08f), 1.2f),
                                    new Vector3(sx * 1.05f, 1.35f, sz * 0.95f), ColMat("#8a6a48"));
                        // 背板 + 侧帘：从背面看不透，像完整的屋台
                        Box(grp, new Vector3(0f, 1.3f, -1.06f), new Vector3(2.2f, 2.2f, 0.05f), ColMat(colB));
                        Box(grp, new Vector3(0f, 2.32f, -1.03f), new Vector3(2.2f, 0.26f, 0.03f), ColMat(colA));
                        Box(grp, new Vector3(-1.12f, 1.25f, -0.45f), new Vector3(0.04f, 1.9f, 1.1f), ColMat(colA));
                        Box(grp, new Vector3(1.12f, 1.25f, -0.45f), new Vector3(0.04f, 1.9f, 1.1f), ColMat(colA));
                        // 摊侧幟旗（面向街道）
                        Box(grp, new Vector3(1.45f, 0.08f, 0f), new Vector3(0.34f, 0.16f, 0.34f), ColMat("#8d8d94"));
                        Box(grp, new Vector3(1.45f, 1.8f, 0f), new Vector3(0.05f, 3.6f, 0.05f), ColMat("#6b5a42"));
                        Box(grp, new Vector3(1.73f, 3.55f, 0f), new Vector3(0.62f, 0.05f, 0.05f), ColMat("#6b5a42"));
                        Box(grp, new Vector3(1.73f, 2.45f, 0f), new Vector3(0.56f, 2.0f, 0.02f), ColMat(colA));
                        Box(grp, new Vector3(1.73f, 3.32f, 0.012f), new Vector3(0.56f, 0.22f, 0.012f), ColMat(colB));
                        Cylinder(grp, new Vector3(1.73f, 2.65f, 0.03f), 0.11f, 0.012f, ColMat(colB), new Vector3(90f, 0f, 0f));
                        // 顶棚样式：0 单坡 / 1 人字 / 2 弧形三片 / 3 双层+前帘
                        int cstyle;
                        switch (kind)
                        {
                            case "food": case "grill": case "ice": case "takoyaki": case "yakisoba": cstyle = 0; break;
                            case "goods": case "masks": case "games": case "shooting": case "apple": cstyle = 1; break;
                            case "drinks": case "goldfish": case "candy": case "water": cstyle = 2; break;
                            default: cstyle = 3; break;
                        }
                        switch (cstyle)
                        {
                            case 0:
                                MeshObj(grp, MeshFactory.BoxUV(new Vector3(2.5f, 0.07f, 2.2f), 1f), new Vector3(0f, 2.72f, 0.1f), cloth, new Vector3(8f, 0f, 0f));
                                break;
                            case 1:
                                MeshObj(grp, MeshFactory.BoxUV(new Vector3(2.6f, 0.07f, 1.45f), 1f), new Vector3(0f, 2.9f, -0.45f), cloth, new Vector3(-18f, 0f, 0f));
                                MeshObj(grp, MeshFactory.BoxUV(new Vector3(2.6f, 0.07f, 1.45f), 1f), new Vector3(0f, 2.9f, 0.65f), cloth, new Vector3(18f, 0f, 0f));
                                Box(grp, new Vector3(0f, 3.13f, 0.1f), new Vector3(2.7f, 0.1f, 0.25f), ColMat(colA));
                                break;
                            case 2:
                                MeshObj(grp, MeshFactory.BoxUV(new Vector3(2.5f, 0.06f, 0.95f), 1f), new Vector3(0f, 2.95f, 0.0f), cloth);
                                MeshObj(grp, MeshFactory.BoxUV(new Vector3(2.5f, 0.06f, 0.85f), 1f), new Vector3(0f, 2.78f, -0.78f), cloth, new Vector3(-22f, 0f, 0f));
                                MeshObj(grp, MeshFactory.BoxUV(new Vector3(2.5f, 0.06f, 0.85f), 1f), new Vector3(0f, 2.78f, 0.92f), cloth, new Vector3(22f, 0f, 0f));
                                break;
                            case 3:
                                MeshObj(grp, MeshFactory.BoxUV(new Vector3(2.5f, 0.07f, 2.2f), 1f), new Vector3(0f, 2.62f, 0.1f), cloth, new Vector3(6f, 0f, 0f));
                                MeshObj(grp, MeshFactory.BoxUV(new Vector3(2.05f, 0.06f, 1.45f), 1f), new Vector3(0f, 3.22f, -0.15f), cloth, new Vector3(-10f, 0f, 0f));
                                for (int i = 0; i < 3; i++)
                                    Box(grp, new Vector3(-0.72f + i * 0.72f, 2.26f, 1.02f), new Vector3(0.66f, 0.5f, 0.03f),
                                        ColMat(i % 2 == 0 ? colA : colB), default, false);
                                break;
                        }
                        // 前檐布幌
                        Box(grp, new Vector3(0f, 2.22f, 1.02f), new Vector3(2.1f, 0.38f, 0.03f), ColMat(colA), default, false);
                        Box(grp, new Vector3(0f, 2.22f, 1.04f), new Vector3(0.3f, 0.28f, 0.02f), ColMat(colB), default, false);
                        // 四角斜撑
                        foreach (int sx in new[] { -1, 1 })
                            foreach (int sz in new[] { -1, 1 })
                                Box(grp, new Vector3(sx * 0.94f, 2.42f, sz * 0.86f), new Vector3(0.06f, 0.72f, 0.06f),
                                    ColMat("#8a6a48"), new Vector3(0f, 0f, sx * 26f));
                        // 檐下一排纸提灯 + 前场菜单牌 + 摊后备货
                        Box(grp, new Vector3(0f, 2.66f, 0.92f), new Vector3(2.15f, 0.02f, 0.02f), ColMat("#3a2f28"), default, false);
                        for (int li = -1; li <= 1; li++)
                            Chochin(grp, new Vector3(li * 0.72f, 2.4f, 0.92f), 0.11f, 0.27f, li != 0);
                        Box(grp, new Vector3(-1.0f, 0.42f, 1.32f), new Vector3(0.05f, 0.84f, 0.05f), ColMat("#6b5a42"), new Vector3(0f, 0f, -10f));
                        Box(grp, new Vector3(-1.0f, 0.42f, 1.52f), new Vector3(0.05f, 0.84f, 0.05f), ColMat("#6b5a42"), new Vector3(0f, 0f, 10f));
                        Box(grp, new Vector3(-1.0f, 0.72f, 1.42f), new Vector3(0.56f, 0.66f, 0.03f), ColMat("#f2ead8"), new Vector3(10f, 0f, 0f));
                        Box(grp, new Vector3(-1.0f, 0.96f, 1.45f), new Vector3(0.56f, 0.12f, 0.035f), ColMat(colA), new Vector3(10f, 0f, 0f));
                        for (int mi = 0; mi < 3; mi++)
                            Box(grp, new Vector3(-1.0f, 0.84f - mi * 0.1f, 1.455f), new Vector3(0.36f, 0.025f, 0.01f), ColMat("#5a3a2a"), new Vector3(10f, 0f, 0f), false);
                        Box(grp, new Vector3(-1.24f, 0.25f, -0.78f), new Vector3(0.4f, 0.5f, 0.4f), ColMat("#4a6a8a"));
                        Box(grp, new Vector3(1.2f, 0.18f, -0.78f), new Vector3(0.38f, 0.36f, 0.38f), LitTex("wood"));
                        Box(grp, new Vector3(1.2f, 0.52f, -0.78f), new Vector3(0.3f, 0.28f, 0.3f), LitTex("wood"));
                        Cylinder(grp, new Vector3(-1.3f, 0.28f, -0.35f), 0.14f, 0.56f, MatMetal("#8a9099"));
                        // 小凳
                        MeshObj(grp, MeshFactory.BoxUV(new Vector3(0.4f, 0.42f, 0.4f), 2f), new Vector3(0.75f, 0.21f, 0.95f), woodMat);
                        // 相机避障/物理用整体碰撞
                        var stallCol = grp.gameObject.AddComponent<BoxCollider>();
                        stallCol.center = new Vector3(0f, 1.5f, 0f);
                        stallCol.size = new Vector3(2.7f, 3.1f, 2.5f);

                        // 货品
                        switch (kind)
                        {
                            case "takoyaki":
                                Box(grp, new Vector3(-0.35f, 1.02f, 0f), new Vector3(0.85f, 0.08f, 0.55f), ColMat("#2f2a28"));
                                for (int i = 0; i < 9; i++)
                                    Cylinder(grp, new Vector3(-0.6f + (i % 3) * 0.22f, 1.07f, -0.15f + (i / 3) * 0.15f), 0.055f, 0.04f, ColMat("#c98a3a"));
                                Box(grp, new Vector3(0.5f, 1.15f, 0f), new Vector3(0.12f, 0.3f, 0.12f), ColMat("#b8352f"));
                                Box(grp, new Vector3(0.25f, 1.03f, 0f), new Vector3(0.3f, 0.06f, 0.2f), ColMat("#e8dcc0"));
                                break;
                            case "yakisoba":
                                Box(grp, new Vector3(-0.3f, 1.02f, 0f), new Vector3(0.95f, 0.06f, 0.55f), ColMat("#3a3a3e"));
                                Box(grp, new Vector3(-0.3f, 1.08f, 0f), new Vector3(0.5f, 0.08f, 0.34f), ColMat("#e8c46a"));
                                Box(grp, new Vector3(0.2f, 1.06f, 0.05f), new Vector3(0.04f, 0.02f, 0.3f), MatMetal("#c9ced6"), new Vector3(0f, 20f, 0f));
                                Box(grp, new Vector3(0.45f, 1.18f, -0.05f), new Vector3(0.1f, 0.28f, 0.1f), ColMat("#8a2f2a"));
                                Box(grp, new Vector3(0.6f, 1.12f, 0.1f), new Vector3(0.1f, 0.2f, 0.1f), ColMat("#e8dcc0"));
                                break;
                            case "apple":
                                for (int i = 0; i < 5; i++)
                                {
                                    float ax = -0.55f + i * 0.28f;
                                    Box(grp, new Vector3(ax, 1.22f, 0f), new Vector3(0.02f, 0.36f, 0.02f), ColMat("#d8cbb4"));
                                    Cylinder(grp, new Vector3(ax, 1.48f, 0f), 0.1f, 0.16f, ColMatGloss("#c0392b", 0.45f));
                                }
                                Box(grp, new Vector3(0f, 1.02f, 0f), new Vector3(1.3f, 0.06f, 0.4f), LitTex("wood"));
                                break;
                            case "shooting":
                                for (int r = 0; r < 3; r++)
                                    for (int c = 0; c < 5; c++)
                                        Box(grp, new Vector3(-0.7f + c * 0.35f, 1.35f + r * 0.3f, -0.55f), new Vector3(0.16f, 0.16f, 0.16f),
                                            ColMat(r == 0 ? "#e8c46a" : (r == 1 ? "#c9553f" : "#5a8fc8")));
                                Box(grp, new Vector3(-0.3f, 1.08f, 0.2f), new Vector3(0.5f, 0.08f, 0.08f), LitTex("wood"), new Vector3(0f, 0f, -8f));
                                Box(grp, new Vector3(0.3f, 1.08f, 0.2f), new Vector3(0.5f, 0.08f, 0.08f), LitTex("wood"), new Vector3(0f, 0f, -8f));
                                Box(grp, new Vector3(0f, 1.6f, -0.75f), new Vector3(0.7f, 0.7f, 0.03f), ColMat("#f2efe6"));
                                Cylinder(grp, new Vector3(0f, 1.6f, -0.73f), 0.18f, 0.02f, ColMat("#c0392b"), new Vector3(90f, 0f, 0f));
                                break;
                            case "water":
                                Box(grp, new Vector3(-0.3f, 1.12f, 0f), new Vector3(0.9f, 0.3f, 0.55f), ColMat("#7ac0d8"));
                                for (int i = 0; i < 7; i++)
                                    MeshObj(grp, MeshFactory.Sphere(0.07f, 6, 4), new Vector3(-0.55f + (i % 4) * 0.16f, 1.22f + (i / 4) * 0.08f, -0.12f + (i % 2) * 0.12f),
                                        ColMat(i % 3 == 0 ? "#e8748a" : (i % 3 == 1 ? "#f2d857" : "#7ac0e0")), default, false);
                                for (int i = 0; i < 2; i++)
                                {
                                    Box(grp, new Vector3(0.45f + i * 0.25f, 1.15f, 0.05f), new Vector3(0.22f, 0.02f, 0.22f), MatMetal("#c9ced6"));
                                    Box(grp, new Vector3(0.56f + i * 0.25f, 1.25f, 0.05f), new Vector3(0.04f, 0.22f, 0.04f), MatMetal("#c9ced6"));
                                }
                                break;
                            case "goldfish":
                                Cylinder(grp, new Vector3(-0.45f, 1.1f, 0.05f), 0.38f, 0.3f, ColMat("#3f6a8a"));
                                for (int i = 0; i < 4; i++)
                                    Box(grp, new Vector3(-0.6f + i * 0.12f, 1.12f, 0.05f + (i % 2) * 0.08f), new Vector3(0.08f, 0.04f, 0.08f), ColMat("#e8803f"), default, false);
                                Box(grp, new Vector3(0.5f, 1.02f, 0.0f), new Vector3(0.3f, 0.05f, 0.2f), ColMat("#d8cbb4"));
                                break;
                            case "food":
                            case "grill":
                                Box(grp, new Vector3(-0.35f, 1.02f, 0.0f), new Vector3(0.9f, 0.16f, 0.55f), ColMat("#3a3a3e"));
                                for (int i = 0; i < 5; i++)
                                    Box(grp, new Vector3(-0.7f + i * 0.16f, 1.14f, 0.02f), new Vector3(0.05f, 0.04f, 0.42f), ColMat("#c9a24a"));
                                Box(grp, new Vector3(0.55f, 1.1f, 0.0f), new Vector3(0.25f, 0.22f, 0.35f), ColMat("#d8cbb4"));
                                break;
                            case "goods":
                            case "masks":
                                for (int i = 0; i < 3; i++)
                                {
                                    float gx = -0.55f + i * 0.55f;
                                    Box(grp, new Vector3(gx, 1.75f, -0.28f), new Vector3(0.36f, 0.44f, 0.06f),
                                        ColMat(i == 0 ? "#e8c46a" : (i == 1 ? "#c9553f" : "#5a8fc8")), default, false);
                                }
                                Box(grp, new Vector3(0.45f, 1.06f, 0.0f), new Vector3(0.5f, 0.2f, 0.4f), ColMat("#a87e52"));
                                break;
                            case "candy":
                                for (int i = 0; i < 3; i++)
                                {
                                    Cylinder(grp, new Vector3(-0.55f + i * 0.4f, 1.16f, 0.0f), 0.12f, 0.34f, ColMat("#e8e2d2"));
                                    Box(grp, new Vector3(-0.55f + i * 0.4f, 1.32f, 0.0f), new Vector3(0.2f, 0.05f, 0.2f),
                                        ColMat(i == 0 ? "#e8748a" : (i == 1 ? "#f2d857" : "#7ac0e0")));
                                }
                                Box(grp, new Vector3(0.62f, 1.45f, 0.0f), new Vector3(0.08f, 0.55f, 0.08f), ColMat("#f2efe6"));
                                Box(grp, new Vector3(0.62f, 1.75f, 0.0f), new Vector3(0.3f, 0.26f, 0.3f), ColMat("#f0b6cc"));
                                break;
                            case "drinks":
                                for (int i = 0; i < 4; i++)
                                    Cylinder(grp, new Vector3(-0.6f + i * 0.32f, 1.18f, 0.0f), 0.07f, 0.42f,
                                        ColMat(i == 0 ? "#7ac0e0" : (i == 1 ? "#c9553f" : (i == 2 ? "#e8c46a" : "#4a8a4a"))));
                                break;
                            case "ice":
                                Box(grp, new Vector3(-0.35f, 1.15f, 0.0f), new Vector3(0.9f, 0.4f, 0.55f), ColMat("#f2f6fa"));
                                Box(grp, new Vector3(-0.35f, 1.38f, 0.0f), new Vector3(0.95f, 0.08f, 0.6f), ColMat("#5a8fc8"));
                                Box(grp, new Vector3(0.55f, 1.3f, 0.0f), new Vector3(0.12f, 0.6f, 0.12f), ColMat("#d8cbb4"));
                                Box(grp, new Vector3(0.55f, 1.66f, 0.0f), new Vector3(0.28f, 0.34f, 0.06f), ColMat("#e8c46a"));
                                break;
                            case "lantern":
                                for (int i = 0; i < 3; i++)
                                {
                                    float lx = -0.6f + i * 0.6f;
                                    Cylinder(grp, new Vector3(lx, 2.1f, 0.55f), 0.14f, 0.34f, ColMat(i == 0 ? "#e8b24a" : (i == 1 ? "#c9553f" : "#e8e2d2")));
                                    Box(grp, new Vector3(lx, 2.32f, 0.55f), new Vector3(0.04f, 0.25f, 0.04f), ColMat("#3a2f28"));
                                }
                                break;
                            case "games":
                                Box(grp, new Vector3(0.45f, 1.4f, -0.2f), new Vector3(0.6f, 0.6f, 0.08f), ColMat("#f2efe6"));
                                Box(grp, new Vector3(0.45f, 1.4f, -0.15f), new Vector3(0.2f, 0.2f, 0.04f), ColMat("#c9553f"));
                                for (int i = 0; i < 3; i++)
                                    Box(grp, new Vector3(-0.7f + i * 0.25f, 1.06f, 0.0f), new Vector3(0.05f, 0.05f, 0.4f), ColMat("#8a6a48"));
                                break;
                        }
                        // 食摊前的小凳 + 摊边垃圾桶
                        if (kind == "takoyaki" || kind == "yakisoba" || kind == "grill" || kind == "food")
                            MeshObj(grp, MeshFactory.BoxUV(new Vector3(0.4f, 0.42f, 0.4f), 2f), new Vector3(-0.05f, 0.21f, 1.02f), woodMat);
                        Cylinder(grp, new Vector3(1.28f, 0.28f, 0.92f), 0.16f, 0.56f, ColMat("#4a5a52"));
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
                        // 吊舱：绕轮心公转但保持正立（与轮缘同速）
                        var ring = NewGo("gondolas", grp).transform;
                        ring.localPosition = new Vector3(0f, 9.5f, 0f);
                        for (int i = 0; i < 12; i++)
                        {
                            float a = i / 12f * Mathf.PI * 2f;
                            var car = NewGo("car", ring).transform;
                            car.localPosition = new Vector3(0f, Mathf.Cos(a) * R - 0.5f, Mathf.Sin(a) * R);
                            Box(car, Vector3.zero, new Vector3(0.95f, 0.8f, 0.9f),
                                ColMat(i % 3 == 0 ? "#e8c46a" : (i % 3 == 1 ? "#5a8fc8" : "#c9553f")));
                        }
                        ring.gameObject.AddComponent<GondolaRing>().Radius = R;
                        Box(grp, new Vector3(0f, 0.12f, 0f), new Vector3(6f, 0.24f, 7f), ColMat("#b8b0a2"));
                    }
                    break;
                case "ride_carousel":
                    {
                        Cylinder(grp, new Vector3(0f, 0.18f, 0f), 5.1f, 0.36f, ColMat("#b8ac96"));
                        Cylinder(grp, new Vector3(0f, 3.2f, 0f), 0.42f, 6.4f, ColMat("#8f8a80"));
                        var spin = NewGo("spin", grp).transform;
                        var carouselSpin = spin.gameObject.AddComponent<Spin>();
                        carouselSpin.Axis = Vector3.up;
                        carouselSpin.DegPerSec = 9f;
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
                        var swingSpin = spin.gameObject.AddComponent<Spin>();
                        swingSpin.Axis = Vector3.up;
                        swingSpin.DegPerSec = 16f;
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
        // 叙事游戏不需要失控帧率：硬锁 60。
        // 不能用 vSync=1：165Hz 等高分屏上垂直同步会让 Unity 忽略 targetFrameRate，直接按刷新率跑。
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        if (string.IsNullOrEmpty(Args.Str("-shot")))
            Screen.SetResolution(1600, 900, FullScreenMode.Windowed);   // 默认窗口，可拖动边缘调整；F11 切全屏
        QualitySettings.shadowDistance = 140f;

        var w = BuildWorld();
        if (w == null) return;
        // 后处理栈（泛光/移轴景深/色彩分级/暗角）——挂到主相机
        var cam = Camera.main;
        if (cam != null && cam.GetComponent<PostFX>() == null) cam.gameObject.AddComponent<PostFX>();
        if (cam != null && Args.Has("-noao")) cam.GetComponent<PostFX>().AOSStrength = 0f;
        if (cam != null && Args.Has("-nofx")) cam.GetComponent<PostFX>().enabled = false;

        var tod = GetComponent<TimeOfDay>();
        if (tod != null) tod.Init(this);
        var rig = GetComponent<PlayerRig>();
        if (rig != null) rig.Init(this);
        var mm = GetComponent<Minimap>();
        if (mm == null) mm = gameObject.AddComponent<Minimap>();
        mm.Init(this, rig);
        if (rig != null) rig.BindMinimap(mm);
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

        // 室内（家 / 教学楼）：一次建好并隐藏，走到门口按 F 进入
        var im = GetComponent<InteriorManager>();
        if (im == null) im = gameObject.AddComponent<InteriorManager>();
        if (rig != null) rig.BindInterior(im);   // 先绑定：im.Init 里 -indoor 会直接进房
        im.Init(this, rig, tod, wth, mm);
    }

    public WorldData BuildWorld()
    {
        // 默认进入小镇（V5 总平面）；-campus 回到原校园世界
        bool campus = Args.Has("-campus");
        W = campus ? CampusData.Build() : TownData.Build();
        if (W == null) return null;
        TextureLibrary.SetWorld(W);
        if (W.RIVER != null) Spawn = new Vector3(-17f, 0f, -4f);   // 钟楼广场
        Root = NewGo("World", transform).transform;

        WindowGlowMat = MatLib.UnlitColor(MatLib.Hex("#ffcf8a"));
        // 店铺橱窗玻璃（立面窗格由贴图渐变表现，这块只给橱窗用）
        WindowGlassMat = MatLib.LitColor(MatLib.Hex("#6f8798"), 0.50f);
        DoorLampMat = MatLib.LitColor(MatLib.Hex("#ffdf9a"), 0.40f);
        LanternMat = MatLib.LitColor(MatLib.Hex("#b8352f"), 0.20f);
        WindowFXRoot = NewGo("WindowFX", Root).transform;
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
        Debug.Log($"[Exterior] 亮窗 {WindowGlowRenders.Count} / 招牌 {SignGlowRenders.Count} / 静态合批已启用");
        Villa();
        Trees();
        ForestBelt();
        RailTunnels();
        Distant();
        SnowLayer();
        Rockeries();
        Rocks();
        Bushes();
        Azaleas();
        Reeds();
        PlayerRigObjects();
        if (Args.Has("-fps"))
        {
            var mrs = FindObjectsOfType<MeshRenderer>(true);
            long verts = 0;
            foreach (var mf in FindObjectsOfType<MeshFilter>(true))
                if (mf.sharedMesh != null) verts += mf.sharedMesh.vertexCount;
            var lights = FindObjectsOfType<Light>(true);
            int en = 0;
            foreach (var l in lights) if (l.enabled && l.gameObject.activeInHierarchy) en++;
            Debug.Log($"[Perf] 渲染器 {mrs.Length} / 顶点 {verts} / 灯光 {lights.Length}（启用 {en}）");
        }
        return W;
    }

    // 天空：双穹交叉淡化（内看球），夜穹初始透明
    void Sky()
    {
        var day = NewGo("SkyDay", Root);
        SkyDayGo = day;
        day.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Dome(420f, 32, 20);
        var dr = day.AddComponent<MeshRenderer>();
        SkyDayMat = MatLib.Fade("sky_day", new Color(0.5f, 0.5f, 0.5f, 1f));   // Legacy 粒子着色器自带 ×2 色彩倍增，0.5 抵消
        SkyDayMat.renderQueue = 3000;   // 昼穹先画
        dr.sharedMaterial = SkyDayMat;
        dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        dr.receiveShadows = false;

        var night = NewGo("SkyNight", Root);
        night.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Dome(418f, 32, 20);
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
            GrassMat = LitTinted("grass", "#aebd8f");
            var groundMesh = TerrainHoleMesh(TexScale.GrassPeriod, 0f);
            var groundGo = MeshObj(Root, groundMesh, Vector3.zero, GrassMat);
            var groundCol = groundGo.AddComponent<MeshCollider>();
            groundCol.sharedMesh = groundMesh;
        }
        else
        {
            var greps = TexScale.Repeat(gs, gs, TexScale.GrassPeriod);
            GrassMat = LitTinted("grass", "#aebd8f", greps.x, greps.y);
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

    // 环形山峰表：角度(°)、离中心距离、峰高、影响半径（高斯）——刻意高低不一，避免一圈等高土坡
    static readonly float[,] PEAKS =
    {
        {   9f, 236f, 30f, 54f },
        {  42f, 208f, 16f, 38f },
        {  74f, 252f, 40f, 60f },
        { 106f, 204f, 12f, 34f },
        { 137f, 240f, 27f, 48f },
        { 168f, 268f, 44f, 62f },
        { 199f, 214f, 15f, 36f },
        { 229f, 246f, 29f, 46f },
        { 259f, 258f, 36f, 54f },
        { 290f, 206f, 13f, 34f },
        { 318f, 238f, 24f, 44f },
        { 345f, 224f, 20f, 40f },
    };

    // 边界地貌高度：r ≤ 128 全平（可行走区必须平），128 起缓坡，外侧叠加一圈高低不一的山峰
    // 河谷（距河 13m 内）与铁路走廊（z≈104）各自压平，避免把水面/铁轨埋进山体
    float RimH(float x, float z)
    {
        float r = Mathf.Sqrt(x * x + z * z);
        float h = 0f;
        if (r > 128f)
        {
            float t = Mathf.Clamp01((r - 128f) / 66f);
            h = Mathf.SmoothStep(0f, 1f, t) * 6f;
            float t2 = Mathf.Clamp01((r - 196f) / 110f);
            h += Mathf.SmoothStep(0f, 1f, t2) * 5f;
            // 山峰：每个峰独立的高斯鼓包
            for (int i = 0; i < PEAKS.GetLength(0); i++)
            {
                float a = PEAKS[i, 0] * Mathf.Deg2Rad;
                float px = Mathf.Cos(a) * PEAKS[i, 1], pz = Mathf.Sin(a) * PEAKS[i, 1];
                float dx2 = x - px, dz2 = z - pz;
                float sd = PEAKS[i, 3];
                h += PEAKS[i, 2] * Mathf.Exp(-(dx2 * dx2 + dz2 * dz2) / (2f * sd * sd));
            }
            h *= 0.82f + 0.36f * Mathf.PerlinNoise(x * 0.011f + 11.3f, z * 0.011f + 5.7f);
            h *= 0.90f + 0.20f * Mathf.PerlinNoise(x * 0.031f + 31.7f, z * 0.031f + 17.3f);   // 第二层起伏，避免山包太规整
            if (W.RIVER != null)
            {
                float dz = Mathf.Abs(z - RZ(x));
                h *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((dz - 13f) / 24f));
            }
            // 铁路走廊：轨面两侧 9m 内压平，24m 外恢复；一直平到洞门口（120）
            if (Mathf.Abs(x) < 120f)
                h *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Mathf.Abs(z - 104f) - 9f) / 15f));
        }
        // 隧道口山体：洞门（±118）背后 2m 起坡，用 12m 的坡长堆到 10m 高，不做成直立的墙
        float ax = Mathf.Abs(x);
        if (ax > 120f)
        {
            float dx = x - Mathf.Sign(x) * 132f;
            float d2 = dx * dx + (z - 104f) * (z - 104f);
            h += 10f * Mathf.Exp(-d2 / (2f * 16f * 16f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((ax - 120f) / 9f));
        }
        return h;
    }

    // 河道处断开的地形（北片 + 南片合成一张网格；边界处抬升为缓丘）
    Mesh TerrainHoleMesh(float period, float y)
    {
        float gs = W.GROUND_SIZE > 10 ? W.GROUND_SIZE : 200f;
        float half = W.RIVER.width / 2f;
        var north = MeshFactory.StripH(x => -gs / 2f, x => RZ(x) - half, -gs / 2f, gs / 2f, 340, 120, period,
            (x, z) => y + RimH(x, z));
        var south = MeshFactory.StripH(x => RZ(x) + half, x => gs / 2f, -gs / 2f, gs / 2f, 340, 120, period,
            (x, z) => y + RimH(x, z));
        var ci = new CombineInstance[2];
        ci[0].mesh = north; ci[0].transform = Matrix4x4.identity;
        ci[1].mesh = south; ci[1].transform = Matrix4x4.identity;
        var m = new Mesh();
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.CombineMeshes(ci, true, false);
        return m;
    }

    void River()
    {
        var r = W.RIVER;
        if (r == null) return;
        var g = NewGo("River", Root).transform;
        float half = r.width / 2f;
        const int steps = 340;   // 河道延到 ±310m，采样相应加密

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
        // 木廊桥屋顶：低缓人字坡 + 暖色瓦、山墙封板、博风板、檐口梁
        const float ang = 0.30f;
        float half = (w + 1.2f) / 2f;
        float slope = half / Mathf.Cos(ang);
        float eaveY = 2.70f;
        float ridgeY = eaveY + half * Mathf.Tan(ang);
        float midY = (eaveY + ridgeY) / 2f;
        var bridgeRoof = MatLib.LitTexture(TextureLibrary.RoofTex("#6a4a3c"), 4f, 2f, 0.2f);
        // 右坡(+x)外端低 → 绕 Z 负角；左坡相反
        MeshObj(g, MeshFactory.BoxUV(new Vector3(slope, 0.2f, span + 1.2f), 4f, 2f),
            new Vector3(half / 2f, midY, 0f), bridgeRoof, new Vector3(0f, 0f, -ang * Mathf.Rad2Deg));
        MeshObj(g, MeshFactory.BoxUV(new Vector3(slope, 0.2f, span + 1.2f), 4f, 2f),
            new Vector3(-half / 2f, midY, 0f), bridgeRoof, new Vector3(0f, 0f, ang * Mathf.Rad2Deg));
        Box(g, new Vector3(0f, ridgeY + 0.1f, 0f), new Vector3(0.4f, 0.2f, span + 1.35f), ColMat("#3a2f2a"));
        // 山墙封板（两端，封住屋顶与横梁之间，入口上方）
        float gableH = ridgeY - 2.55f;
        foreach (int sz in new[] { -1, 1 })
        {
            var tri = NewGo("bridge_gable", g).transform;
            tri.localPosition = new Vector3(0f, 2.55f, sz * (span / 2f + 0.02f));
            MeshObj(tri, MeshFactory.Triangle(w + 1.2f, gableH), Vector3.zero, ColMat("#6a4a34"));
            // 博风板（沿两端斜边）
            Box(g, new Vector3(half / 2f, midY, sz * (span / 2f + 0.05f)), new Vector3(slope, 0.22f, 0.18f),
                ColMat("#4a3528"), new Vector3(0f, 0f, -ang * Mathf.Rad2Deg));
            Box(g, new Vector3(-half / 2f, midY, sz * (span / 2f + 0.05f)), new Vector3(slope, 0.22f, 0.18f),
                ColMat("#4a3528"), new Vector3(0f, 0f, ang * Mathf.Rad2Deg));
        }
        // 檐口梁 + 椽头
        foreach (int sgn in new[] { -1, 1 })
        {
            Box(g, new Vector3(sgn * half, eaveY, 0f), new Vector3(0.24f, 0.26f, span + 1.2f), ColMat("#6a4a34"));
            Box(g, new Vector3(sgn * (w / 2f - 0.22f), 2.58f, 0f), new Vector3(0.2f, 0.2f, span + 0.6f), ColMat("#7a5a3e"));
            for (float rz = -span / 2f + 0.6f; rz <= span / 2f; rz += 1.4f)
                Box(g, new Vector3(sgn * (half - 0.18f), eaveY - 0.12f, rz), new Vector3(0.45f, 0.1f, 0.12f), ColMat("#5a4030"));
        }
        // 顶棚（从桥下看是木色天花，不是黑色屋顶底面）
        MeshObj(g, MeshFactory.BoxUV(new Vector3(w - 0.2f, 0.06f, span), 2f), new Vector3(0f, 2.62f, 0f), LitTex("wood"));


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
            // 环塘步道（2.2m 宽）
            MeshObj(t, MeshFactory.Ring(rx + 3.2f, rz + 3.2f, rx + 5.4f, rz + 5.4f, 64, 2.4f), new Vector3(0f, 0.012f, 0f), LitTex("path"));
            // 睡莲
            for (int i = 0; i < 7; i++)
            {
                float a = i * 1.7f;
                float rr = 0.55f + (i % 3) * 0.12f;
                MeshObj(t, MeshFactory.Disc(rr, rr, 10, 2f),
                    new Vector3(Mathf.Cos(a) * (rx * 0.55f), 0.03f, Mathf.Sin(a) * (rz * 0.55f)),
                    ColMat(i % 2 == 0 ? "#4a8a48" : "#3f7a3f"), default, false, false);
            }
            // 池畔石灯笼
            foreach (int sgn in new[] { -1, 1 })
            {
                var lt = NewGo("pond_lantern", t).transform;
                lt.localPosition = new Vector3(sgn * (rx + 7.2f), 0f, 0f);
                Box(lt, new Vector3(0f, 0.14f, 0f), new Vector3(0.7f, 0.28f, 0.7f), ColMat("#8d8d94"));
                Box(lt, new Vector3(0f, 0.66f, 0f), new Vector3(0.28f, 0.8f, 0.28f), ColMat("#84848b"));
                Box(lt, new Vector3(0f, 1.14f, 0f), new Vector3(0.62f, 0.36f, 0.62f), ColMat("#8d8d94"));
                Box(lt, new Vector3(0f, 1.14f, 0.32f), new Vector3(0.3f, 0.22f, 0.04f), MatLib.UnlitColor(MatLib.Hex("#ffdf9a")), default, false);
                Box(lt, new Vector3(0f, 1.4f, 0f), new Vector3(0.76f, 0.16f, 0.76f), ColMat("#6f6f76"));
            }
            // 南岸木观景台
            var deck = NewGo("pond_deck", t).transform;
            deck.localPosition = new Vector3(-10f, 0f, rz + 9.5f);
            MeshObj(deck, MeshFactory.BoxUV(new Vector3(6.4f, 0.24f, 3.2f), 2f), new Vector3(0f, 0.12f, 0f), LitTex("wood"));
            foreach (int dx in new[] { -2.8f, 2.8f })
                foreach (int dz in new[] { -1.3f, 1.3f })
                    MeshObj(deck, MeshFactory.BoxUV(new Vector3(0.24f, 0.8f, 0.24f), 2f), new Vector3(dx, -0.3f, dz), LitTex("wood"));
            MeshObj(deck, MeshFactory.BoxUV(new Vector3(6.4f, 0.1f, 0.14f), 1.2f), new Vector3(0f, 0.85f, 1.55f), ColMat("#a87e52"));
            for (float dx = -3f; dx <= 3f; dx += 1f)
                Box(deck, new Vector3(dx, 0.55f, 1.55f), new Vector3(0.08f, 0.7f, 0.08f), ColMat("#8a6a48"));
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
        StaticBatchingUtility.Combine(g.gameObject);
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
        StaticBatchingUtility.Combine(g.gameObject);
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
            float gy = W.RIVER != null ? GroundY(l.x, l.z) : 0f;   // 人行道上不埋进地面
            lamp.localPosition = new Vector3(l.x, gy, l.z);
            lamp.localEulerAngles = new Vector3(0f, l.r * Mathf.Rad2Deg, 0f);
            Box(lamp, new Vector3(0f, 0.16f, 0f), new Vector3(0.42f, 0.32f, 0.42f), MatMetal("#4a4f58"));
            Box(lamp, new Vector3(0f, 1.85f, 0f), new Vector3(0.16f, 3.4f, 0.16f), MatMetal("#3a3f4a"));
            Box(lamp, new Vector3(0.42f, 3.48f, 0f), new Vector3(0.9f, 0.12f, 0.12f), MatMetal("#3a3f4a"));
            Box(lamp, new Vector3(0.8f, 3.36f, 0f), new Vector3(0.46f, 0.16f, 0.36f), MatMetal("#2f333c"));
            var glass = Box(lamp, new Vector3(0.8f, 3.26f, 0f), new Vector3(0.38f, 0.1f, 0.3f), LampGlass, default, false);
            glass.GetComponent<MeshRenderer>().sharedMaterial = LampGlass;
            var pool = NewGo("pool", lamp);
            pool.transform.localPosition = new Vector3(0.78f, 0.015f, 0f);
            MeshObj(pool.transform, MeshFactory.Quad(5.4f, 5.4f), Vector3.zero, LampPool, default, false, false);
            // 真实点光源（无阴影，夜里由 TimeOfDay 调亮度）
            var lg = new GameObject("light");
            lg.transform.SetParent(lamp, false);
            lg.transform.localPosition = new Vector3(0.8f, 3.15f, 0f);
            var li = lg.AddComponent<Light>();
            li.type = LightType.Point;
            li.color = MatLib.Hex("#ffcf8a");
            li.range = 7f;
            li.intensity = 0f;
            li.shadows = LightShadows.None;
            li.renderMode = LightRenderMode.Auto;
            LampLights.Add(li);
            LampLightPower.Add(1f);
        }
        StaticBatchingUtility.Combine(g.gameObject);
    }

    // ── 四栋建筑（平顶/坡顶/公共；含屋顶设备/侧翼/雨棚/招牌/大门等全部附加件）──
    void Buildings()
    {
        if (WindowFXRoot == null) WindowFXRoot = NewGo("WindowFX", Root).transform;
        var g = NewGo("Buildings", Root).transform;
        foreach (var b in W.BUILDINGS)
        {
            float w = b.size[0], h = b.size[1], d = b.size[2];
            var grp = NewGo("b_" + b.id, g).transform;
            grp.localPosition = new Vector3(b.pos[0], h / 2f, b.pos[1]);
            grp.localEulerAngles = new Vector3(0f, b.Yaw, 0f);
            var bodyCol = grp.gameObject.AddComponent<BoxCollider>();
            bodyCol.center = Vector3.zero;
            bodyCol.size = new Vector3(w, h, d);
            if (b.style == "shrine") { ShrineHall(grp, b); continue; }

            bool plain = b.style == "plain" || b.style == "stand";   // 摊亭/站台/看台这类：无窗立面
            var wallMat = plain ? ColMat(b.wall) : LitTex("wall_" + b.id + "_day");
            if (!plain) wallMat.name = "wall_" + b.id + "_day";   // 便于定位材质
            // 世界尺度 UV：贴图覆盖 w×h 米 → 正/背/侧四面同密度（侧面显示立面左侧裁切，不再被拉伸）
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(w, h, d), plain ? 2.4f : w, plain ? 2.4f : h), Vector3.zero, wallMat);
            if (!plain)
            {
                WindowMatRefs.Add(wallMat);
                WindowMats.Add(new[] { "wall_" + b.id + "_day", "wall_" + b.id + "_night" });
            }
            if (b.style == "stand") { Bleacher(grp, b, w, h, d); continue; }

            var tile = plain ? ColMat(b.roof) : LitTex("roof_" + b.id, 4f, 2f);

            if (plain)
            {
                Box(grp, new Vector3(0, h / 2f + 0.14f, 0), new Vector3(w + 0.35f, 0.28f, d + 0.35f), ColMat(b.roof));
            }
            else if (b.style == "gable" || b.style == "collegiate" || b.style == "library" || b.style == "hall" || b.style == "dorm")
            {
                // 双坡：檐低脊高；山墙三角从墙顶封到屋脊；檐口/博风板收边
                const float ang = 0.45f;
                const float overD = 0.7f;
                float halfD = d / 2f + overD;
                float slope = halfD / Mathf.Cos(ang);
                float eaveY = h / 2f + 0.32f;
                float ridgeY = eaveY + halfD * Mathf.Tan(ang);
                float midY = (eaveY + ridgeY) / 2f;
                MeshObj(grp, MeshFactory.BoxUV(new Vector3(w + 1.2f, 0.24f, slope), 4f, 2f),
                    new Vector3(0f, midY, halfD / 2f), tile, new Vector3(ang * Mathf.Rad2Deg, 0f, 0f));
                MeshObj(grp, MeshFactory.BoxUV(new Vector3(w + 1.2f, 0.24f, slope), 4f, 2f),
                    new Vector3(0f, midY, -halfD / 2f), tile, new Vector3(-ang * Mathf.Rad2Deg, 0f, 0f));
                Box(grp, new Vector3(0f, ridgeY + 0.1f, 0f), new Vector3(w + 1.4f, 0.22f, 0.4f), ColMat("#33363f"));
                float gH = ridgeY - h / 2f;
                foreach (int s2 in new[] { -1, 1 })
                {
                    var tri = NewGo("gable", grp).transform;
                    tri.localPosition = new Vector3(s2 * (w / 2f - 0.02f), h / 2f, 0f);
                    tri.localEulerAngles = new Vector3(0f, 90f, 0f);
                    MeshObj(tri, MeshFactory.Triangle(d + 2f * overD, gH), Vector3.zero, ColMat(b.wall));
                    // 博风板（沿两端斜边）
                    Box(grp, new Vector3(s2 * (w / 2f + 0.58f), midY, halfD / 2f), new Vector3(0.16f, 0.3f, slope),
                        ColMat(b.trim), new Vector3(ang * Mathf.Rad2Deg, 0f, 0f));
                    Box(grp, new Vector3(s2 * (w / 2f + 0.58f), midY, -halfD / 2f), new Vector3(0.16f, 0.3f, slope),
                        ColMat(b.trim), new Vector3(-ang * Mathf.Rad2Deg, 0f, 0f));
                }
                // 檐口封板 + 檐下 soffit（前后各一条，封住屋顶与墙之间的空隙）
                foreach (int sz in new[] { -1, 1 })
                {
                    Box(grp, new Vector3(0f, eaveY - 0.05f, sz * halfD), new Vector3(w + 1.2f, 0.2f, 0.2f), ColMat(b.trim));
                    Box(grp, new Vector3(0f, eaveY - 0.02f, sz * (d / 2f + overD / 2f)), new Vector3(w + 0.9f, 0.07f, overD), ColMat(b.trim));
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

            if (!plain)
            {
                // 四角壁柱（竖向划分）
                foreach (int sx in new[] { -1, 1 })
                    foreach (int sz in new[] { -1, 1 })
                        Box(grp, new Vector3(sx * (w / 2f - 0.16f), 0f, sz * (d / 2f - 0.16f)),
                            new Vector3(0.3f, h, 0.3f), ColMat(b.trim));
                // 层间线脚（四面一圈，增加水平层次）
                float floorH = h / Mathf.Max(1, b.rows);
                for (int f = 1; f < b.rows; f++)
                {
                    float yy = -h / 2f + f * floorH;
                    Box(grp, new Vector3(0f, yy, d / 2f + 0.05f), new Vector3(w + 0.12f, 0.12f, 0.1f), ColMat(b.trim));
                    Box(grp, new Vector3(0f, yy, -d / 2f - 0.05f), new Vector3(w + 0.12f, 0.12f, 0.1f), ColMat(b.trim));
                    Box(grp, new Vector3(w / 2f + 0.05f, yy, 0f), new Vector3(0.1f, 0.12f, d), ColMat(b.trim));
                    Box(grp, new Vector3(-w / 2f - 0.05f, yy, 0f), new Vector3(0.1f, 0.12f, d), ColMat(b.trim));
                }
                // 坡顶烟囱
                if (b.style == "gable")
                {
                    Box(grp, new Vector3(w * 0.3f, h / 2f + 1.15f, -d * 0.15f), new Vector3(0.66f, 1.5f, 0.66f), ColMat(b.plinth));
                    Box(grp, new Vector3(w * 0.3f, h / 2f + 1.95f, -d * 0.15f), new Vector3(0.84f, 0.14f, 0.84f), ColMat("#3a3a3e"));
                }
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

            // 校园新样式：学院风入口山墙 / 图书馆柱廊+钟亭 / 礼堂柱廊 / 实验楼玻璃门厅 / 宿舍阳台
            if (b.style == "collegiate") CollegiateFront(grp, b, w, h, d, wallMat);
            else if (b.style == "library")
            {
                Portico(grp, b, h, d, Mathf.Min(8f, w * 0.4f), 5.4f, 4, 1.8f);
                LibraryCupola(grp, b, h, d);
            }
            else if (b.style == "hall") Portico(grp, b, h, d, 11f, 6.2f, 6, 2.2f);
            else if (b.style == "modern") ModernFront(grp, b, w, h, d);
            else if (b.style == "dorm") DormDetail(grp, b, w, h, d);

            // 招牌 + 大门 + 台阶 + 门灯 + 雨落管 + 门前绿篱（纯色体块不配这些）
            if (plain) continue;
            // 校舍不挂名称门牌（作者要求）；校门“华清大学”横匾单独保留
            string signId = b.id.StartsWith("s_") ? null : SignIdFor(b.id);
            var signTex = string.IsNullOrEmpty(signId) ? null : TextureLibrary.ByName("sign_" + signId, false, false);
            if (signTex != null)
            {
                var signMat = MatLib.LitTexture(signTex, 1f, 1f, 0.12f);
                Face(grp, 3.3f, 0.83f, new Vector3(0, SignY(b), d / 2f + SignFront(b)), signMat);
                SignMatRefs.Add(signMat);
            }
            float entranceZ = d / 2f + EntranceExtra(b);
            Face(grp, 1.5f, 2.1f, new Vector3(0, -h / 2f + 1.08f, entranceZ + 0.035f), LitTex("door"));
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(1.9f, 0.16f, 0.9f), TexScale.PathPeriod), new Vector3(0, -h / 2f + 0.08f, entranceZ + 0.5f), LitTex("path"));   // 石阶（铺装同材质）
            Box(grp, new Vector3(-1.05f, -h / 2f + 2.32f, entranceZ + 0.06f), new Vector3(0.2f, 0.28f, 0.14f), DoorLampMat, default, false);
            Box(grp, new Vector3(1.05f, -h / 2f + 2.32f, entranceZ + 0.06f), new Vector3(0.2f, 0.28f, 0.14f), DoorLampMat, default, false);
            Box(grp, new Vector3(-(w / 2f - 0.35f), 0f, d / 2f + 0.08f), new Vector3(0.16f, h - 0.5f, 0.16f), ColMatGloss(b.plinth, 0.22f));
            Box(grp, new Vector3(w / 2f - 0.35f, 0f, d / 2f + 0.08f), new Vector3(0.16f, h - 0.5f, 0.16f), ColMatGloss(b.plinth, 0.22f));
            // 店铺：雨棚 + 悬挂小招牌；骑楼店铺用横匾
            if (ShopIds.Contains(b.id))
            {
                if (ArcadeIds.Contains(b.id))
                {
                    Box(grp, new Vector3(0f, -h / 2f + 3.75f, d / 2f + 1.15f), new Vector3(w * 0.8f, 0.5f, 0.14f), ColMat(b.trim));
                    Box(grp, new Vector3(0f, -h / 2f + 3.75f, d / 2f + 1.23f), new Vector3(w * 0.55f, 0.22f, 0.03f), ColMat("#c9a24a"), default, false);
                }
                else
                {
                    var awnMat = MatLib.LitTexture(TextureLibrary.Stripes2(ShopStripeA(b.id), "#f2ead8"), 1f, 1f, 0.1f);
                    float aw = Mathf.Max(2f, w * 0.62f);
                    MeshObj(grp, MeshFactory.BoxUV(new Vector3(aw, 0.07f, 1.3f), 1f),
                        new Vector3(0f, -h / 2f + 2.7f, d / 2f + 0.8f), awnMat, new Vector3(14f, 0f, 0f));
                    foreach (int sx in new[] { -1, 1 })
                        Box(grp, new Vector3(sx * (aw / 2f - 0.1f), -h / 2f + 2.32f, d / 2f + 1.32f),
                            new Vector3(0.06f, 0.72f, 0.06f), MatMetal("#8a9099"));
                }
                Box(grp, new Vector3(w / 2f - 0.5f, -h / 2f + 2.25f, d / 2f + 0.45f), new Vector3(0.08f, 0.85f, 0.5f), ColMat(b.trim));
                Box(grp, new Vector3(w / 2f - 0.5f, -h / 2f + 2.25f, d / 2f + 0.72f), new Vector3(0.05f, 0.5f, 0.3f), ColMat("#c9a24a"), default, false);

                // 店铺类型差异（茶楼/绸缎药铺/书店照相馆/超市便利店）
                string sid = b.id;
                if (sid == "o_tea" || sid == "r_rest")
                {
                    // 二层挑台 + 栏杆 + 红灯笼（茶楼/餐馆）
                    MeshObj(grp, MeshFactory.BoxUV(new Vector3(w * 0.8f, 0.16f, 0.85f), 2f),
                        new Vector3(0f, -h / 2f + 3.75f, d / 2f + 0.5f), LitTex("wood"));
                    for (float rx = -w * 0.34f; rx <= w * 0.36f; rx += Mathf.Max(1f, w * 0.17f))
                        Box(grp, new Vector3(rx, -h / 2f + 4.2f, d / 2f + 0.9f), new Vector3(0.08f, 0.9f, 0.08f), ColMat("#8a6a48"));
                    Box(grp, new Vector3(0f, -h / 2f + 4.65f, d / 2f + 0.9f), new Vector3(w * 0.75f, 0.1f, 0.12f), ColMat("#a87e52"));
                    Cylinder(grp, new Vector3(-w * 0.3f, -h / 2f + 3.0f, d / 2f + 0.75f), 0.16f, 0.42f, ColMat("#b8352f"));
                    Cylinder(grp, new Vector3(w * 0.3f, -h / 2f + 3.0f, d / 2f + 0.75f), 0.16f, 0.42f, ColMat("#b8352f"));
                }
                else if (sid == "o_silk" || sid == "o_pharm" || sid == "r_book" || sid == "r_photo")
                {
                    // 展示橱窗 + 格子（绸缎庄/药铺/书店/照相馆），避开门洞
                    foreach (int sx in new[] { -1, 1 })
                    {
                        Face(grp, Mathf.Max(1.4f, w * 0.26f), 1.7f,
                            new Vector3(sx * w * 0.3f, -h / 2f + 1.75f, d / 2f + 0.045f),
                            WindowGlassMat);
                        for (int gi = 0; gi < 4; gi++)
                            Box(grp, new Vector3(sx * w * 0.3f - w * 0.11f + gi * w * 0.073f, -h / 2f + 1.75f, d / 2f + 0.09f),
                                new Vector3(0.05f, 1.7f, 0.05f), ColMat(b.trim));
                    }
                    Box(grp, new Vector3(w / 2f - 0.5f, -h / 2f + 2.3f, d / 2f + 0.42f), new Vector3(0.08f, 1.1f, 0.5f), ColMat("#2f3a34"));
                }
                else if (sid == "r_market" || sid == "r_conv")
                {
                    // 横条招牌（超市/便利店，看板建筑）
                    Box(grp, new Vector3(0f, -h / 2f + 3.05f, d / 2f + 0.35f), new Vector3(w * 0.85f, 0.55f, 0.16f), ColMat("#3f7a52"));
                    Box(grp, new Vector3(0f, -h / 2f + 3.05f, d / 2f + 0.44f), new Vector3(w * 0.7f, 0.24f, 0.03f), ColMat("#f2efe6"), default, false);
                }
            }
            // 门前绿篱（神社/柱廊建筑不种）
            if (!b.id.StartsWith("k_") && b.style != "library" && b.style != "hall")
            {
                float hedgeW = Mathf.Max(1.6f, w * 0.32f);
                foreach (int sgn in new[] { -1, 1 })
                    MeshObj(grp, MeshFactory.BoxUV(new Vector3(hedgeW, 0.5f, 0.6f), TexScale.LeafMeters),
                        new Vector3(sgn * (w * 0.3f), -h / 2f + 0.27f, d / 2f + 0.42f), LitTinted("leaves", "#356b33"));
            }
            if (ArcadeIds.Contains(b.id)) Arcade(grp, b);
            if (b.style != "shrine") BuildWindowGlow(grp, b, w, h, d);
        }
        // 建筑静态合批（窗玻璃/亮窗已移到 WindowFX，不参与）
        StaticBatchingUtility.Combine(g.gameObject);
    }

    static string SignIdFor(string id)
    {
        switch (id)
        {
            case "s_teach": return "teach";
            case "s_lib": return "library";
            case "s_lab": return "labs";
            case "s_dorm": return "dorm";
            case "s_canteen": return "canteen";
            case "s_center": return "center";
            case "s_gate": return "guardN";
            default: return id;
        }
    }

    static float EntranceExtra(BuildingDef b)
    {
        if (b.style == "collegiate") return 1.04f;
        if (b.style == "modern") return 1.16f;
        return 0f;
    }

    static float SignFront(BuildingDef b)
    {
        switch (b.style)
        {
            case "library": return 3.22f;   // 柱廊山花
            case "collegiate": return 1.08f;
            case "modern": return 1.20f;
            case "dorm": return 0.96f;
            default: return 0.04f;
        }
    }

    static float SignY(BuildingDef b)
    {
        if (b.style == "library") return 0.55f;
        if (b.style == "collegiate" || b.style == "modern") return -b.size[1] / 2f + 4.0f;
        if (b.style == "dorm") return -b.size[1] / 2f + 3.4f;
        return b.size[1] * 0.28f;
    }

    // 学院风：红砖主体 + 石材入口凸出体 + 时钟山墙 + 雨棚
    void CollegiateFront(Transform grp, BuildingDef b, float w, float h, float d, Material wallMat)
    {
        float baseY = -h / 2f, front = d / 2f;
        float bayW = Mathf.Min(8f, w * 0.3f);
        const float bayD = 1.05f;
        MeshObj(grp, MeshFactory.BoxUV(new Vector3(bayW, h, bayD), w, h),
            new Vector3(0f, 0f, front + bayD / 2f), wallMat);
        Box(grp, new Vector3(0f, baseY + 0.45f, front + bayD + 0.03f),
            new Vector3(bayW + 0.4f, 0.9f, 0.14f), ColMat(b.plinth));
        foreach (int sx in new[] { -1, 1 })
        {
            Box(grp, new Vector3(sx * (bayW / 2f - 0.12f), 0f, front + bayD + 0.05f),
                new Vector3(0.34f, h, 0.18f), ColMat(b.trim));
            Box(grp, new Vector3(sx * (bayW / 2f - 0.02f), 0f, front + bayD - 0.08f),
                new Vector3(0.24f, h, 0.16f), ColMat(b.trim));
        }
        float floorH = h / Mathf.Max(1, b.rows);
        for (int f = 1; f < b.rows; f++)
            Box(grp, new Vector3(0f, baseY + f * floorH, front + bayD + 0.06f),
                new Vector3(bayW + 0.2f, 0.12f, 0.1f), ColMat(b.trim));
        float pedY = h / 2f + 0.06f;
        Box(grp, new Vector3(0f, pedY - 0.08f, front + bayD + 0.04f),
            new Vector3(bayW + 0.8f, 0.2f, 0.2f), ColMat(b.trim));
        MeshObj(grp, MeshFactory.Triangle(bayW + 0.6f, 1.9f),
            new Vector3(0f, pedY, front + bayD + 0.04f), ColMat(b.trim));
        Box(grp, new Vector3(0f, h / 2f - 2.2f, front + bayD + 0.02f),
            new Vector3(2.4f, 2.4f, 0.1f), ColMat(b.plinth));
        Face(grp, 2.0f, 2.0f, new Vector3(0f, h / 2f - 2.2f, front + bayD + 0.07f), LitTex("clock"));
        Box(grp, new Vector3(0f, baseY + 3.45f, front + bayD + 0.95f),
            new Vector3(bayW * 0.72f, 0.18f, 1.9f), ColMat(b.roof));
        foreach (int sx in new[] { -1, 1 })
            Box(grp, new Vector3(sx * (bayW * 0.3f), baseY + 2.0f, front + bayD + 1.55f),
                new Vector3(0.22f, 2.9f, 0.22f), ColMat(b.trim));
    }

    // 古典柱廊：台阶 + 柱列 + 檐部 + 三角山花（图书馆/礼堂共用）
    void Portico(Transform grp, BuildingDef b, float h, float d,
        float porticoW, float colH, int nCols, float pedH)
    {
        float baseY = -h / 2f, front = d / 2f;
        const float depth = 3f;
        var stone = ColMat(b.trim);
        var roofMat = LitTex("roof_" + b.id, 4f, 2f);
        for (int i = 0; i < 3; i++)
            Box(grp, new Vector3(0f, baseY + 0.1f + i * 0.13f, front + depth + 0.25f + i * 0.4f),
                new Vector3(porticoW + 1.7f - i * 0.24f, 0.24f, 0.44f), ColMat(b.plinth));
        Box(grp, new Vector3(0f, baseY + 0.34f, front + depth / 2f + 0.35f),
            new Vector3(porticoW, 0.22f, depth + 0.7f), stone);
        for (int i = 0; i < nCols; i++)
        {
            float x = Mathf.Lerp(-porticoW / 2f + 0.8f, porticoW / 2f - 0.8f,
                nCols == 1 ? 0.5f : i / (float)(nCols - 1));
            float cz = front + depth - 0.45f;
            Cyl(grp, new Vector3(x, baseY + 0.5f + colH / 2f, cz), 0.24f, colH, stone);
            Box(grp, new Vector3(x, baseY + 0.6f, cz), new Vector3(0.62f, 0.2f, 0.62f), stone);
            Box(grp, new Vector3(x, baseY + 0.5f + colH - 0.12f, cz), new Vector3(0.66f, 0.24f, 0.66f), stone);
        }
        float topY = baseY + 0.5f + colH;
        Box(grp, new Vector3(0f, topY + 0.3f, front + depth - 0.35f),
            new Vector3(porticoW + 0.8f, 0.6f, 1.0f), stone);
        MeshObj(grp, MeshFactory.Triangle(porticoW + 0.9f, pedH),
            new Vector3(0f, topY + 0.6f, front + depth + 0.16f), ColMat(b.trim));
        float ang = Mathf.Atan2(pedH, (porticoW + 0.9f) / 2f);
        float slope = (porticoW + 0.9f) / 2f / Mathf.Cos(ang) + 0.25f;
        foreach (int s in new[] { -1, 1 })
            Box(grp, new Vector3(s * (porticoW + 0.9f) / 4f, topY + 0.6f + pedH / 2f, front + depth / 2f + 0.55f),
                new Vector3(slope, 0.18f, depth + 0.6f), roofMat, new Vector3(0f, 0f, -s * ang * Mathf.Rad2Deg));
        foreach (int s in new[] { -1, 1 })
            Box(grp, new Vector3(s * (porticoW / 2f - 0.7f), topY - 0.18f, front + depth + 0.02f),
                new Vector3(0.18f, 0.26f, 0.12f), DoorLampMat, default, false);
        if (b.style == "hall")
        {
            // 礼堂山花校徽：金色圆盘 + 朱红圆心
            Cylinder(grp, new Vector3(0f, topY + 0.6f + pedH * 0.42f, front + depth + 0.2f),
                0.85f, 0.06f, ColMatGloss("#c9a24a", 0.5f), new Vector3(90f, 0f, 0f));
            Cylinder(grp, new Vector3(0f, topY + 0.6f + pedH * 0.42f, front + depth + 0.25f),
                0.5f, 0.08f, ColMat("#b8352f"), new Vector3(90f, 0f, 0f));
        }
    }

    // 图书馆钟亭：八角锥顶 + 四面钟
    void LibraryCupola(Transform grp, BuildingDef b, float h, float d)
    {
        float halfD = d / 2f + 0.7f;
        float ridgeY = h / 2f + 0.32f + halfD * Mathf.Tan(0.45f);
        var cu = NewGo("cupola", grp).transform;
        cu.localPosition = new Vector3(0f, ridgeY - 0.15f, 0f);
        Box(cu, new Vector3(0f, 0.6f, 0f), new Vector3(2.7f, 2.1f, 2.7f), ColMat(b.trim));
        Box(cu, new Vector3(0f, 1.72f, 0f), new Vector3(3.1f, 0.18f, 3.1f), ColMat(b.plinth));
        MeshObj(cu, MeshFactory.Cone(2.25f, 2.0f, 8, true, 4f),
            new Vector3(0f, 1.8f, 0f), LitTex("roof_" + b.id, 4f, 2f));
        Cylinder(cu, new Vector3(0f, 4.0f, 0f), 0.09f, 0.8f, MatMetal("#c9a24a"));
        foreach (var (dx, dz, ry) in new[] { (0f, 1f, 0f), (1f, 0f, 90f), (0f, -1f, 180f), (-1f, 0f, -90f) })
            Face(cu, 1.35f, 1.35f, new Vector3(dx * 1.37f, 0.7f, dz * 1.37f), LitTex("clock"), ry);
    }

    // 现代实验楼：玻璃门厅 + 水平遮阳带 + 屋顶设备
    void ModernFront(Transform grp, BuildingDef b, float w, float h, float d)
    {
        float baseY = -h / 2f, front = d / 2f;
        float tw = 3.4f, th = h * 0.86f, td = 1.1f;
        MeshObj(grp, MeshFactory.BoxUV(new Vector3(tw, th, td), 2f),
            new Vector3(0f, baseY + th / 2f, front + td / 2f), WindowGlassMat);
        foreach (int s in new[] { -1, 1 })
            Box(grp, new Vector3(s * (tw / 2f - 0.08f), baseY + th / 2f, front + td + 0.04f),
                new Vector3(0.16f, th, 0.1f), ColMat("#eef1f4"));
        float floorH = h / Mathf.Max(1, b.rows);
        for (int r = 1; r < b.rows; r++)
        {
            Box(grp, new Vector3(0f, baseY + floorH * r, front + td + 0.05f),
                new Vector3(tw, 0.12f, 0.08f), ColMat("#eef1f4"));
            Box(grp, new Vector3(0f, baseY + floorH * r + 0.35f, front + 0.42f),
                new Vector3(w * 0.92f, 0.1f, 0.8f), ColMat(b.trim));
        }
        Box(grp, new Vector3(0f, baseY + th + 0.1f, front + td / 2f),
            new Vector3(tw + 0.4f, 0.22f, td + 0.4f), ColMat("#dfe3e8"));
        Box(grp, new Vector3(0f, baseY + 3.4f, front + td + 0.9f),
            new Vector3(4.6f, 0.16f, 2.0f), ColMat("#dfe3e8"));
        foreach (int s in new[] { -1, 1 })
            Box(grp, new Vector3(s * 2.0f, baseY + 1.7f, front + td + 1.8f),
                new Vector3(0.12f, 3.2f, 0.12f), MatMetal("#9aa2ac"));
        Box(grp, new Vector3(-w * 0.25f, h / 2f + 0.85f, 0f), new Vector3(2.2f, 1.1f, 1.6f), MatMetal("#a8adb5"));
        Box(grp, new Vector3(w * 0.25f, h / 2f + 0.7f, 0f), new Vector3(1.2f, 0.8f, 1.2f), MatMetal("#8a9099"));
    }

    // 宿舍楼：两侧外挑阳台 + 空调机 + 入口雨棚
    void DormDetail(Transform grp, BuildingDef b, float w, float h, float d)
    {
        float baseY = -h / 2f, front = d / 2f;
        float floorH = h / Mathf.Max(1, b.rows);
        float bw = Mathf.Min(4.4f, w * 0.24f);
        for (int r = 1; r < b.rows; r++)
        {
            float y = baseY + floorH * r + 0.35f;
            foreach (int g in new[] { -1, 1 })
            {
                float bx = g * (w * 0.3f);
                Box(grp, new Vector3(bx, y, front + 0.5f), new Vector3(bw, 0.14f, 1.0f), ColMat(b.trim));
                Box(grp, new Vector3(bx, y + 0.52f, front + 0.98f), new Vector3(bw, 0.06f, 0.06f), ColMat("#6f757e"));
                for (int i = 0; i < 4; i++)
                    Box(grp, new Vector3(bx - bw / 2f + 0.08f + i * (bw - 0.16f) / 3f, y + 0.28f, front + 0.98f),
                        new Vector3(0.05f, 0.5f, 0.05f), ColMat("#6f757e"));
                foreach (int sx in new[] { -1, 1 })
                    Box(grp, new Vector3(bx + sx * bw / 2f, y + 0.28f, front + 0.5f),
                        new Vector3(0.05f, 0.5f, 0.95f), ColMat("#6f757e"));
                if (r % 2 == 1)
                    Box(grp, new Vector3(bx + bw * 0.3f, y + 0.25f, front + 0.25f),
                        new Vector3(0.6f, 0.45f, 0.4f), MatMetal("#b8bcc2"));
            }
        }
        Box(grp, new Vector3(0f, baseY + 2.95f, front + 1.0f), new Vector3(5.6f, 0.2f, 2.0f), ColMat(b.roof));
        foreach (int s in new[] { -1, 1 })
            Box(grp, new Vector3(s * 2.4f, baseY + 1.45f, front + 1.9f),
                new Vector3(0.2f, 3.0f, 0.2f), ColMat(b.trim));
    }

    // 看台：低平台 + 五级座席 + 背墙栏杆
    void Bleacher(Transform grp, BuildingDef b, float w, float h, float d)
    {
        float baseY = -h / 2f;
        Box(grp, new Vector3(0f, baseY + 1.2f, -d / 2f + 0.25f),
            new Vector3(w, 2.4f, 0.5f), ColMat(b.wall));
        int n = 5;
        for (int i = 0; i < n; i++)
        {
            float y = baseY + 0.3f + i * 0.38f;
            float z = d / 2f - 0.5f - i * (d - 1.0f) / (n - 1);
            Box(grp, new Vector3(0f, y, z), new Vector3(w - 0.5f, 0.38f, 0.8f),
                ColMat(i % 2 == 0 ? b.trim : b.wall));
        }
        foreach (int s in new[] { -1, 1 })
            Box(grp, new Vector3(s * (w / 2f - 0.2f), baseY + 0.9f, 0f),
                new Vector3(0.3f, 1.8f, d * 0.9f), ColMat(b.plinth));
        Box(grp, new Vector3(0f, baseY + 2.45f, -d / 2f + 0.25f),
            new Vector3(w, 0.1f, 0.1f), MatMetal("#7a8088"));
        for (float x = -w / 2f + 1f; x <= w / 2f - 1f; x += 3f)
            Box(grp, new Vector3(x, baseY + 2.2f, -d / 2f + 0.25f),
                new Vector3(0.08f, 0.5f, 0.08f), MatMetal("#7a8088"));
        Box(grp, new Vector3(0f, baseY + 0.1f, d / 2f - 0.1f),
            new Vector3(w, 0.2f, 0.3f), ColMat(b.plinth));
    }

    // ── 夜间亮窗：按立面窗格生成一层发光片（不依赖贴图/变体，稳定可见）──
    void BuildWindowGlow(Transform grp, BuildingDef b, float w, float h, float d)
    {
        float aw = b.cols * 256f, ah = b.rows * 256f + 320f;
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        int hash = b.id.GetHashCode() & 0x7fffffff;
        for (int r = 0; r < b.rows; r++)
            for (int c = 0; c < b.cols; c++)
            {
                if (((hash + c * 37 + r * 91) % 100) >= 55) continue;
                float u = (c * 256f + 136f) / aw;
                float v = 1f - (r * 256f + 160f) / ah;
                float px = (u - 0.5f) * w, py = (v - 0.5f) * h;
                if (r == 0 && Mathf.Abs(px) < 1.3f) continue;          // 门洞位置不放
                float ww = 160f / aw * w * 0.74f, hh = 160f / ah * h * 0.72f;
                // 拆成 2×2 四格玻璃，保留窗框/中梃
                for (int gx = 0; gx < 2; gx++)
                    for (int gy = 0; gy < 2; gy++)
                    {
                        float cx = px + (gx == 0 ? -0.25f : 0.25f) * ww;
                        float cy = py + (gy == 0 ? -0.25f : 0.25f) * hh;
                        float gwc = ww * 0.44f, ghc = hh * 0.44f;
                        int i0 = verts.Count;
                        verts.Add(new Vector3(cx - gwc / 2f, cy - ghc / 2f, d / 2f + 0.022f));
                        verts.Add(new Vector3(cx + gwc / 2f, cy - ghc / 2f, d / 2f + 0.022f));
                        verts.Add(new Vector3(cx + gwc / 2f, cy + ghc / 2f, d / 2f + 0.022f));
                        verts.Add(new Vector3(cx - gwc / 2f, cy + ghc / 2f, d / 2f + 0.022f));
                        uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f));
                        uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(0f, 1f));
                        tris.Add(i0); tris.Add(i0 + 1); tris.Add(i0 + 2);
                        tris.Add(i0); tris.Add(i0 + 2); tris.Add(i0 + 3);
                    }
            }
        if (verts.Count == 0) return;
        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        // 窗片挂到独立 FX 根：建筑本体可以静态合批，窗玻璃/亮窗夜里要单独开关
        var fx = NewGo("fx_" + b.id, WindowFXRoot).transform;
        fx.localPosition = grp.localPosition;
        fx.localEulerAngles = grp.localEulerAngles;

        var go = NewGo("win_glow", fx);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = WindowGlowMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        WindowGlowRenders.Add(mr);

        // 夜间招牌：用招牌贴图本身做一块 Unlit 发光片（比材质 Emission 更稳）；校舍不挂
        string signId = b.id.StartsWith("s_") ? null : SignIdFor(b.id);
        var signTex = string.IsNullOrEmpty(signId) ? null : TextureLibrary.ByName("sign_" + signId, false, false);
        if (signTex != null)
        {
            var sgMat = new Material(Shader.Find("Unlit/Texture"));
            sgMat.mainTexture = signTex;
            var sgGo = MeshObj(fx, MeshFactory.QuadZ(3.3f, 0.83f),
                new Vector3(0f, SignY(b), d / 2f + SignFront(b) + 0.03f), sgMat, default, false, false);
            SignGlowRenders.Add(sgGo.GetComponent<MeshRenderer>());
        }
        else if (ShopIds.Contains(b.id) && ArcadeIds.Contains(b.id))
        {
            // 小镇骑楼横匾没有招牌贴图：夜里用暖色发光条代替，撑起店面亮度
            var stripGo = MeshObj(fx, MeshFactory.QuadZ(w * 0.55f, 0.22f),
                new Vector3(0f, -h / 2f + 3.75f, d / 2f + 1.26f), WindowGlowMat, default, false, false);
            SignGlowRenders.Add(stripGo.GetComponent<MeshRenderer>());
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

        // 深出檐双坡屋顶：檐低脊高；山墙从墙顶封到脊；檐口/博风板收边
        const float ang = 0.45f;
        float over = 1.2f;
        float halfD = d / 2f + over;
        float slope = halfD / Mathf.Cos(ang);
        float eaveY = py + ph + 0.4f;
        float ridgeY = eaveY + halfD * Mathf.Tan(ang);
        float midY = (eaveY + ridgeY) / 2f;
        foreach (int sz in new[] { -1, 1 })
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(w + 2.4f, 0.28f, slope), 4f, 2f),
                new Vector3(0f, midY, sz * halfD / 2f), roof,
                new Vector3(sz * ang * Mathf.Rad2Deg, 0f, 0f));
        Box(grp, new Vector3(0f, ridgeY + 0.12f, 0f), new Vector3(w + 2.6f, 0.26f, 0.5f), ColMat("#2b2622"));
        float gH = ridgeY - (py + ph);
        foreach (int sx in new[] { -1, 1 })
        {
            var tri = NewGo("sg", grp).transform;
            tri.localPosition = new Vector3(sx * (w / 2f - 0.02f), py + ph, 0f);
            tri.localEulerAngles = new Vector3(0f, 90f, 0f);
            MeshObj(tri, MeshFactory.Triangle(d + 2f * over, gH), Vector3.zero, ColMat("#c8b48e"));
            // 博风板
            Box(grp, new Vector3(sx * (w / 2f + 1.16f), midY, halfD / 2f), new Vector3(0.2f, 0.32f, slope),
                ColMat("#8a6a48"), new Vector3(ang * Mathf.Rad2Deg, 0f, 0f));
            Box(grp, new Vector3(sx * (w / 2f + 1.16f), midY, -halfD / 2f), new Vector3(0.2f, 0.32f, slope),
                ColMat("#8a6a48"), new Vector3(-ang * Mathf.Rad2Deg, 0f, 0f));
        }
        foreach (int sz in new[] { -1, 1 })
        {
            Box(grp, new Vector3(0f, eaveY - 0.06f, sz * halfD), new Vector3(w + 2.4f, 0.22f, 0.22f), ColMat("#6a4a34"));
            Box(grp, new Vector3(0f, eaveY - 0.02f, sz * (d / 2f + over / 2f)), new Vector3(w + 2.0f, 0.07f, over), ColMat("#6a4a34"));
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
        // 铃绪 + 檐下祭典提灯
        Box(grp, new Vector3(0f, py + ph - 0.5f, d / 2f + 0.66f), new Vector3(0.05f, 1.6f, 0.05f), ColMat("#d8c8a0"));
        Cylinder(grp, new Vector3(0f, py + ph - 0.32f, d / 2f + 0.66f), 0.12f, 0.18f, ColMatGloss("#c9a24a", 0.35f));
        for (int i = 0; i < 4; i++)
        {
            float lx = -w * 0.33f + i * w * 0.22f;
            Chochin(grp, new Vector3(lx, eaveY - 0.55f, d / 2f + over * 0.6f), 0.16f, 0.4f, (i & 1) == 0);
        }
        AddWarmLight(grp, new Vector3(0f, eaveY - 0.6f, d / 2f + over * 0.8f), 9f, 1.2f);
    }

    // ── 骑楼：店铺前的连续柱廊 + 二层挑廊 + 灯笼/招牌 ──
    void Arcade(Transform grp, BuildingDef b)
    {
        float w = b.size[0], h = b.size[1], d = b.size[2];
        float baseY = -h / 2f;
        const float depth = 1.7f;
        float zf = d / 2f + depth / 2f;
        var stone = LitTex("concrete");

        // 一层柱列（中间留出入口开间，避免柱子挡住店门）
        for (float x = -w / 2f + 0.5f; x <= w / 2f - 0.4f; x += 2.2f)
        {
            if (Mathf.Abs(x) < 1.15f) continue;
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(0.34f, 3.2f, 0.34f), 2f),
                new Vector3(x, baseY + 1.6f, d / 2f + depth - 0.25f), stone);
        }
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
            Cylinder(grp, new Vector3(x, baseY + 2.8f, d / 2f + depth - 0.55f), 0.16f, 0.42f, LanternMat);
            Box(grp, new Vector3(x, baseY + 3.06f, d / 2f + depth - 0.55f), new Vector3(0.05f, 0.5f, 0.05f), ColMat("#3a2f28"));
            Box(grp, new Vector3(x + 1.4f, baseY + 2.35f, d / 2f + depth - 0.18f), new Vector3(0.06f, 0.7f, 0.06f), ColMat("#3a2f28"));
            Face(grp, 0.7f, 0.5f, new Vector3(x + 1.4f, baseY + 2.85f, d / 2f + depth - 0.14f), ColMat("#d8cbb4"), 90f);
        }
        // 骑楼檐下左右各一盏暖光，夜里把柱廊/店面抬起来（无阴影，避免额外阴影 pass）
        foreach (int ls in new[] { -1, 1 })
        {
            var lg = new GameObject("arcade_light");
            lg.transform.SetParent(grp, false);
            lg.transform.localPosition = new Vector3(ls * w * 0.26f, baseY + 2.75f, d / 2f + depth - 0.7f);
            var li = lg.AddComponent<Light>();
            li.type = LightType.Point;
            li.color = MatLib.Hex("#ffb878");
            li.range = 8.5f;
            li.intensity = 0f;
            li.shadows = LightShadows.None;
            LampLights.Add(li);
            LampLightPower.Add(1.4f);
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

    // ── 边界密林：134~262m 环带，6 种树（松/阔叶/杉/樱/落叶/灌木），每种合并成 1 个网格 ──
    void ForestBelt()
    {
        if (W.RIVER == null) return;
        var g = NewGo("ForestBelt", Root).transform;
        var cone = MeshFactory.Cone(1f, 1f, 8, false, 0f);
        var canopy = MeshFactory.Canopy(1f, 12, 8, 0.22f);
        var trunk = MeshFactory.BoxUV(new Vector3(1f, 1f, 1f), 2f);
        const int SP = 7;
        var lists = new List<CombineInstance>[SP];
        for (int i = 0; i < SP; i++) lists[i] = new List<CombineInstance>();
        var rnd = new System.Random(20261022);

        void Trunk(int sp, float x, float y, float z, float h, float frac)
        {
            float w = Mathf.Max(0.16f, h * 0.042f);
            lists[sp].Add(new CombineInstance
            {
                mesh = trunk,
                transform = Matrix4x4.TRS(new Vector3(x, y + h * frac * 0.5f, z), Quaternion.identity, new Vector3(w, h * frac, w))
            });
        }
        void Cone(int sp, float x, float y, float z, float rad, float hh)
        {
            lists[sp].Add(new CombineInstance
            {
                mesh = cone,
                transform = Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.identity, new Vector3(rad, hh, rad))
            });
        }
        void Ball(int sp, float x, float y, float z, float rad, float sy = 0.9f)
        {
            float rot = (float)rnd.NextDouble() * 360f;   // 每个冠随机转一圈，同一个网格也有不同轮廓
            lists[sp].Add(new CombineInstance
            {
                mesh = canopy,
                transform = Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(0f, rot, 0f), new Vector3(rad, rad * sy, rad))
            });
        }

        void Add(float x, float z, float h)
        {
            float y = RimH(x, z);
            float aDeg = Mathf.Atan2(z, x) * Mathf.Rad2Deg;
            bool shrineSide = Mathf.Abs(Mathf.DeltaAngle(aDeg, -38f)) < 26f;   // 神社一带多种樱
            float roll = (float)rnd.NextDouble();
            if (shrineSide) roll = roll * 0.72f;                                // 神社一带樱的比例翻倍
            if (roll < 0.30f)                                                   // 0 松
            {
                Trunk(0, x, y, z, h, 0.34f);
                Cone(0, x, y + h * 0.16f, z, h * 0.27f, h * 0.52f);
                Cone(0, x, y + h * 0.40f, z, h * 0.20f, h * 0.44f);
                Cone(0, x, y + h * 0.60f, z, h * 0.13f, h * 0.38f);
            }
            else if (roll < 0.54f)                                              // 1 阔叶
            {
                Trunk(1, x, y, z, h, 0.56f);
                Ball(1, x, y + h * 0.62f, z, h * 0.29f, 0.95f);
                Ball(1, x + h * 0.12f, y + h * 0.75f, z - h * 0.08f, h * 0.18f, 0.95f);
            }
            else if (roll < 0.74f)                                              // 2 杉（窄高）
            {
                Trunk(2, x, y, z, h, 0.22f);
                Cone(2, x, y + h * 0.12f, z, h * 0.17f, h * 0.80f);
                Cone(2, x, y + h * 0.66f, z, h * 0.10f, h * 0.34f);
            }
            else if (roll < 0.80f)                                              // 3 樱
            {
                Trunk(3, x, y, z, h, 0.50f);
                Ball(3, x, y + h * 0.62f, z, h * 0.26f, 0.84f);
                Ball(3, x - h * 0.13f, y + h * 0.74f, z + h * 0.07f, h * 0.16f, 0.84f);
            }
            else if (roll < 0.88f)                                              // 4 落叶乔木（细高、冠小）
            {
                Trunk(4, x, y, z, h, 0.68f);
                Ball(4, x, y + h * 0.76f, z, h * 0.20f, 1.05f);
                Ball(4, x + h * 0.09f, y + h * 0.87f, z, h * 0.12f, 1.0f);
            }
            else                                                                // 6 秋色阔叶（橙黄，打散一片绿）
            {
                Trunk(6, x, y, z, h, 0.54f);
                Ball(6, x, y + h * 0.62f, z, h * 0.28f, 0.92f);
                Ball(6, x - h * 0.11f, y + h * 0.74f, z + h * 0.09f, h * 0.17f, 0.92f);
            }
        }

        for (float r0 = 134f; r0 < 262f; r0 += 6.5f)
        {
            bool inner = r0 < 166f;
            float gap = inner ? 9.4f : (r0 < 216f ? 8.2f : 12f);
            int n = Mathf.Max(12, Mathf.RoundToInt(2f * Mathf.PI * r0 / gap));
            for (int k = 0; k < n; k++)
            {
                float a = (k + (float)rnd.NextDouble() * 0.8f) / n * Mathf.PI * 2f;
                float rr = r0 + ((float)rnd.NextDouble() - 0.5f) * 5.4f;
                float x = Mathf.Cos(a) * rr, z = Mathf.Sin(a) * rr;
                if (Mathf.Abs(z - RZ(x)) < 13f) continue;                                   // 河谷留空
                if (Mathf.Abs(x) < 122f && Mathf.Abs(z - 104f) < 6.5f) continue;            // 铁路走廊（到洞门为止）
                // 内圈按噪声留出林窗：径向对齐，能看到后面的第二圈与山体，边界才有纵深
                if (inner && Mathf.PerlinNoise(a * 2.6f + 3.1f, 0.7f) < 0.34f) continue;
                // 林线：18m 等高线以上不长树，山顶留空，山形才露得出来
                if (RimH(x, z) > 18f) continue;
                float h = inner ? 6.5f + (float)rnd.NextDouble() * 7f : 9f + (float)rnd.NextDouble() * 8f;
                Add(x, z, h);
            }
        }

        // 洞门上方补一丛树：否则洞口正上方是一整块光秃秃的绿坡
        foreach (int sgn in new[] { -1, 1 })
        {
            for (int i = 0; i < 18; i++)
            {
                float x = sgn * (125f + (float)rnd.NextDouble() * 18f);
                float z = 104f + ((float)rnd.NextDouble() - 0.5f) * 28f;
                if (Mathf.Abs(x) < 122f && Mathf.Abs(z - 104f) < 6.5f) continue;
                if (RimH(x, z) > 18f) continue;
                Add(x, z, 8f + (float)rnd.NextDouble() * 6f);
            }
        }

        // 5 灌木：贴着林缘的低矮丛，三球一丛，压住树干与草地的接缝
        for (float r0 = 131f; r0 < 152f; r0 += 2.4f)
        {
            int n = Mathf.Max(20, Mathf.RoundToInt(2f * Mathf.PI * r0 / 3.6f));
            for (int k = 0; k < n; k++)
            {
                if (rnd.NextDouble() < 0.52) continue;
                float a = (k + (float)rnd.NextDouble() * 0.9f) / n * Mathf.PI * 2f;
                float rr = r0 + ((float)rnd.NextDouble() - 0.5f) * 3.2f;
                float x = Mathf.Cos(a) * rr, z = Mathf.Sin(a) * rr;
                if (Mathf.Abs(z - RZ(x)) < 12f) continue;
                if (Mathf.Abs(x) < 140f && Mathf.Abs(z - 104f) < 8f) continue;
                float y = RimH(x, z);
                float rad = 0.62f + (float)rnd.NextDouble() * 0.45f;
                Ball(5, x, y + rad * 0.52f, z, rad, 0.66f);
                Ball(5, x + rad * 0.9f, y + rad * 0.36f, z + rad * 0.5f, rad * 0.66f, 0.66f);
                Ball(5, x - rad * 0.7f, y + rad * 0.30f, z + rad * 0.8f, rad * 0.55f, 0.66f);
            }
        }

        var cols = new[] { "#3d6b44", "#4f7d52", "#355f3c", "#b58a9a", "#517a55", "#4a7050", "#a88a4f" };
        var names = new[] { "pine", "broad", "fir", "cherry", "decid", "bush", "autumn" };
        for (int i = 0; i < SP; i++)
        {
            if (lists[i].Count == 0) continue;
            var m = new Mesh();
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.CombineMeshes(lists[i].ToArray(), true, true);
            m.RecalculateBounds();
            var go = NewGo(names[i], g);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = ColMat(cols[i]);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    // ── 铁路收口：两端石砌洞门 + 洞内暗腔，铁轨不再通向世界外 ──
    void RailTunnels()
    {
        if (W.RIVER == null) return;
        var g = NewGo("RailTunnels", Root).transform;
        var stone = LitTex("bridge_stone", 2.4f, 2.4f);
        var trim = ColMat("#b9b2a2");
        var dark = MatLib.UnlitColor(MatLib.Hex("#0b0e11"));
        foreach (int sgn in new[] { -1, 1 })
        {
            float x = sgn * 118f;
            // 洞门（带拱洞的墙面，双面）
            MeshObj(g, MeshFactory.ArchFace(10.4f, 6.6f, 1, 5.2f, 4.8f, 2.4f),
                new Vector3(x, 3.3f, 104f), stone, new Vector3(0f, 90f, 0f));
            // 洞内暗腔（从洞口往里看是黑的；退后 0.9m 避免与洞门共面打架）
            MeshObj(g, MeshFactory.BoxUV(new Vector3(6f, 4.9f, 4.8f), 2f),
                new Vector3(x + sgn * 3.9f, 2.45f, 104f), dark, default, false, false);
            // 两侧翼墙（把洞门与山体接上）
            foreach (int sz in new[] { -1, 1 })
                Box(g, new Vector3(x + sgn * 1.0f, 1.9f, 104f + sz * 5.4f),
                    new Vector3(2.2f, 3.8f, 1.0f), trim);
            // 洞顶压檐 + 洞门灯（东侧洞口常年背光，给一盏常亮的暖光才看得清石券）
            Box(g, new Vector3(x, 6.9f, 104f), new Vector3(1.3f, 0.55f, 10.8f), trim);
            Box(g, new Vector3(x - sgn * 0.55f, 6.1f, 104f), new Vector3(0.5f, 0.5f, 0.5f), MatLib.UnlitColor(MatLib.Hex("#ffd79a")));
            var li = NewGo("tunnel_light", g).AddComponent<Light>();
            li.type = LightType.Point;
            li.color = MatLib.Hex("#ffcf8a");
            li.range = 17f;
            li.intensity = 1.15f;
            li.shadows = LightShadows.None;
            li.transform.localPosition = new Vector3(x - sgn * 1.6f, 5.4f, 104f);
        }
    }

    // ── 树（0-5 常规冠色 / 6 松 / 7 柏 / 8 大树 / 9 柳 / 10-13 樱）──
    void Trees()
    {
        var g = NewGo("Trees", Root).transform;
        EnsureCherryCrowns();
        foreach (var t in W.TREES) Tree(g, t.x, t.z, t.v);
        // 小镇的边界树改由 ForestBelt 统一生成（更密、合并网格）；校园世界仍用数据里的环带
        if (W.RIVER == null) foreach (var t in W.BORDER_TREES) Tree(g, t.x, t.z, t.v);
    }

    void Tree(Transform parent, float x, float z, int v)
    {
        // 硬排除：神社前庭/参道口、河面与桥面，避免任何来源的树卡在建筑或水里
        if (Mathf.Abs(x - 79f) < 13f && Mathf.Abs(z + 57f) < 16f) return;
        if (W.RIVER != null && Mathf.Abs(z - RZ(x)) < 10f) return;
        var grp = NewGo("tree", parent).transform;
        grp.localPosition = new Vector3(x, 0f, z);
        grp.gameObject.AddComponent<ProximityHide>().Radius = 3.2f;
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
        else if (v >= 10 && v <= 13)
        {
            // 樱花：《我的世界》式体素树（0.2m 叶块/原木块），只做 90° 转向和轻微尺寸差
            float hash = Mathf.Repeat(x * 7.31f + z * 11.93f, 1f);
            grp.localEulerAngles = new Vector3(0f, Mathf.Floor(hash * 4f) * 90f, 0f);
            grp.localScale = Vector3.one * (0.92f + Mathf.Repeat(x * 3.17f + z * 5.71f, 1f) * 0.16f);
            for (int s = 0; s < 3; s++)
            {
                var m = _cherryCrowns[v - 10, s];
                if (m != null) MeshObj(grp, m, Vector3.zero, CherryMat(CherryHex(v, s)));
            }
            MeshObj(grp, _cherryBarks[v - 10], Vector3.zero, LitTinted("bark", "#6d4a2f"));
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

    string CherryHex(int v, int shade)
    {
        if (v == 11) return shade == 0 ? "#ee9fba" : shade == 1 ? "#f6c3d4" : "#df89a8";
        if (v == 12) return shade == 0 ? "#cf6f94" : shade == 1 ? "#e08fae" : "#b85b80";
        return shade == 0 ? "#e58fae" : shade == 1 ? "#f2b5ca" : "#d87b9c";
    }

    string GiantHex(int shade)
        => shade == 0 ? "#e88fac" : shade == 1 ? "#f3bccd" : "#d87b9c";

    // 树干块：树皮贴图 × 树色
    GameObject BarkBox(Transform p, Vector3 pos, Vector3 size, string hex)
        => MeshObj(p, MeshFactory.BoxUV(size, TexScale.BarkMeters), pos, LitTinted("bark", hex));

    // 松/柏的锥形树冠：锥面 UV 按周长/高度展开（密度同为 160px/米）
    void LeafCone(Transform p, float radius, float height, float baseY, string hex)
        => MeshObj(p, MeshFactory.Cone(radius, height, 6, true, TexScale.LeafMeters), new Vector3(0, baseY, 0), LitTinted("leaves", hex));

    void EnsureCherryCrowns()
    {
        if (_cherryCrowns != null) return;
        _cherryCrowns = new Mesh[4, 3];
        _cherryBarks = new Mesh[4];
        for (int v = 10; v <= 13; v++)
        {
            var leaves = CherryLeafBlocks(v);
            for (int s = 0; s < 3; s++) _cherryCrowns[v - 10, s] = BuildVoxelMesh(leaves, s);
            _cherryBarks[v - 10] = BuildCherryBark(v);
        }
        var giant = GiantLeafBlocks();
        _giantCrowns = new Mesh[3];
        for (int s = 0; s < 3; s++) _giantCrowns[s] = BuildVoxelMesh(giant, s);
        _giantBark = BuildGiantBark();
    }

    void EnsureBloomCanopies()
    {
        if (_bloomCanopyA == null) _bloomCanopyA = MeshFactory.Canopy(1f, 12, 8, 0.22f);
        if (_bloomCanopyB == null) _bloomCanopyB = MeshFactory.Canopy(1f, 12, 8, 0.32f, 5.3f);
    }

    // ── 樱花体素树：与《我的世界》一致，叶块/原木块 0.2m、轴对齐、按层分色 ──
    // 布局按 0.8m 逻辑块书写，Subdivide 统一细分成 4×4×4 个 0.2m 体素
    // 先按体素网格摆出树干/主枝，再用“底层缺角 + 中层满 + 顶层收分”的叶团铺冠；
    // 所有块都是等大立方体，不做任意角度旋转，避免“乱拼长方体”的观感。
    Vector3 VoxCenter(float x, float y, float z)
        => new Vector3(x * CherryVox, y * CherryVox + CherryVox * 0.5f, z * CherryVox);

    static Vector2Int BranchTip(int ang, int len)
    {
        float a = ang * Mathf.Deg2Rad;
        return new Vector2Int(Mathf.RoundToInt(Mathf.Cos(a) * len), Mathf.RoundToInt(Mathf.Sin(a) * len));
    }

    void AddVoxelCube(List<CombineInstance> list, Mesh cube, float x, float y, float z)
        => list.Add(new CombineInstance
        {
            mesh = cube,
            transform = Matrix4x4.TRS(VoxCenter(x, y, z), Quaternion.identity, Vector3.one)
        });

    // 叶团：直接按 0.2m 体素逐层堆，平面圆角矩形、上下各收 3 层，可选 0.8m 小盖顶
    void LeafBlob(HashSet<Vector3Int> set, int cx, int cy, int cz, int hw, int hd, bool peak)
    {
        int rx = (2 * hw + 1) * CherrySub / 2;
        int rz = (2 * hd + 1) * CherrySub / 2;
        int ox = cx * CherrySub + CherrySub / 2;
        int oz = cz * CherrySub + CherrySub / 2;
        int y0 = cy * CherrySub;
        int layers = peak ? 16 : 12;
        float tx = (2f * hw - 1f) / (2f * hw + 1f);   // 顶层收分比例，与旧的 hw-1 一致
        float tz = (2f * hd - 1f) / (2f * hd + 1f);
        for (int ly = 0; ly < layers; ly++)
        {
            float fx = 1f, fz = 1f;
            if (peak && ly >= 12) { fx = 0.34f; fz = 0.34f; }
            else if (ly > 8)
            {
                float u = (ly - 8) / 3f;   // 顶部 3 层按 0.2m 台阶收分
                fx = Mathf.Lerp(1f, tx, u);
                fz = Mathf.Lerp(1f, tz, u);
            }
            int mx = Mathf.Max(2, Mathf.RoundToInt(rx * fx));
            int mz = Mathf.Max(2, Mathf.RoundToInt(rz * fz));
            bool cut = ly < CherrySub || ly > 8;   // 底/顶层四角各缺一个 0.8m 逻辑块
            int y = y0 + ly;
            for (int ix = -mx; ix < mx; ix++)
                for (int iz = -mz; iz < mz; iz++)
                {
                    if (cut && mx > CherrySub && mz > CherrySub
                        && Mathf.Abs(ix + 0.5f) > mx - CherrySub
                        && Mathf.Abs(iz + 0.5f) > mz - CherrySub) continue;
                    set.Add(new Vector3Int(ox + ix, y, oz + iz));
                }
        }
    }

    HashSet<Vector3Int> CherryLeafBlocks(int v)
    {
        var set = new HashSet<Vector3Int>();
        if (v == 12)
        {
            // 八重樱：树干矮、冠幅最大，前后左右各探出一团
            LeafBlob(set, 0, 6, 0, 2, 1, true);
            LeafBlob(set, -2, 6, 0, 1, 1, false);
            LeafBlob(set, 2, 6, 0, 1, 1, false);
            LeafBlob(set, 0, 6, -2, 1, 1, false);
            LeafBlob(set, 0, 6, 2, 1, 1, false);
        }
        else if (v == 11)
        {
            // 白粉樱：树干高、冠窄，中央再叠一层形成层次
            LeafBlob(set, 0, 8, 0, 1, 1, true);
            LeafBlob(set, -2, 8, 0, 1, 1, false);
            LeafBlob(set, 2, 8, 0, 1, 1, false);
            LeafBlob(set, 0, 8, 2, 1, 1, false);
            LeafBlob(set, 0, 9, 0, 1, 1, false);
        }
        else if (v == 13)
        {
            // 垂枝樱：宽冠 + 四边低垂两格叶块
            LeafBlob(set, 0, 7, 0, 2, 1, true);
            LeafBlob(set, -2, 7, 0, 1, 1, false);
            LeafBlob(set, 2, 7, 0, 1, 1, false);
            LeafBlob(set, 0, 7, -2, 1, 1, false);
            LeafBlob(set, 0, 7, 2, 1, 1, false);
            foreach (var (dx, dz) in new[] { (-2, 0), (2, 0), (0, -2), (0, 2) })
            {
                set.Add(new Vector3Int(dx, 6, dz));
                set.Add(new Vector3Int(dx, 5, dz));
            }
        }
        else
        {
            // 染井吉野：宽冠、三向主枝对应三团侧冠
            LeafBlob(set, 0, 7, 0, 2, 1, true);
            LeafBlob(set, -2, 7, 0, 1, 1, false);
            LeafBlob(set, 2, 7, -1, 1, 1, false);
            LeafBlob(set, 0, 7, 2, 1, 1, false);
        }
        return set;
    }

    HashSet<Vector3Int> GiantLeafBlocks()
    {
        var set = new HashSet<Vector3Int>();
        // 《我的世界》大型樱花：直立粗干 + 五根不等高、不等长的分枝，
        // 枝端各顶一团冠，再用错落的冠团叠成宽而蓬松的伞形冠，不做同心分层
        foreach (var (by, ang, len, rise) in GiantBranchDefs)
        {
            var tip = BranchTip(ang, len);
            int hw = len >= 9 ? 4 : 3;
            LeafBlob(set, tip.x, by + rise + 1, tip.y, hw, hw, true);
        }
        // 填充冠团 + 顶冠：宽底收顶到 26.4m
        foreach (var (x, y, z, hw, peak) in GiantCanopyBlobs)
            LeafBlob(set, x, y, z, hw, hw, peak);
        // 树干穿过的叶块挖掉，避免共面闪烁
        for (int y = 0; y < 18 * CherrySub; y++)
            for (int x = -CherrySub; x < 2 * CherrySub; x++)
                for (int z = -CherrySub; z < 2 * CherrySub; z++)
                    set.Remove(new Vector3Int(x, y, z));
        return set;
    }

    // 0.8m 逻辑块 → 4×4×4 个 0.2m 体素，树的外形尺寸不变、组成单位缩到 25%
    HashSet<Vector3Int> Subdivide(HashSet<Vector3Int> logical)
    {
        var fine = new HashSet<Vector3Int>();
        foreach (var b in logical)
            for (int dx = 0; dx < CherrySub; dx++)
                for (int dy = 0; dy < CherrySub; dy++)
                    for (int dz = 0; dz < CherrySub; dz++)
                        fine.Add(new Vector3Int(b.x * CherrySub + dx, b.y * CherrySub + dy, b.z * CherrySub + dz));
        return fine;
    }

    // 六面都有邻居的体素画不出来，跳过可省掉树干/树冠内部的大量面
    bool Enclosed(HashSet<Vector3Int> set, Vector3Int b)
        => set.Contains(new Vector3Int(b.x - 1, b.y, b.z))
        && set.Contains(new Vector3Int(b.x + 1, b.y, b.z))
        && set.Contains(new Vector3Int(b.x, b.y - 1, b.z))
        && set.Contains(new Vector3Int(b.x, b.y + 1, b.z))
        && set.Contains(new Vector3Int(b.x, b.y, b.z - 1))
        && set.Contains(new Vector3Int(b.x, b.y, b.z + 1));

    // 每色阶合并成 1 个网格；叶块下方没有叶子则最深、上方没有叶子则最亮，中间用主色
    Mesh BuildVoxelMesh(HashSet<Vector3Int> blocks, int shade)
    {
        var cubes = new Mesh[4];
        for (int i = 0; i < 4; i++)
            cubes[i] = MeshFactory.BoxUV(new Vector3(CherryVox, CherryVox, CherryVox), TexScale.LeafMeters, TexScale.LeafMeters, i * 0.25f, (i % 2) * 0.5f);
        var list = new List<CombineInstance>();
        foreach (var b in blocks)
        {
            if (Enclosed(blocks, b)) continue;
            bool under = !blocks.Contains(new Vector3Int(b.x, b.y - 1, b.z));
            bool over = !blocks.Contains(new Vector3Int(b.x, b.y + 1, b.z));
            int s = under ? 2 : over ? 1 : 0;
            if (s != shade) continue;
            int v4 = ((b.x * 3 + b.y * 5 + b.z * 7) & 3);
            AddVoxelCube(list, cubes[v4], b.x, b.y, b.z);
        }
        if (list.Count == 0) return null;
        var m = new Mesh();
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;   // 0.2m 体素细分后单色阶超 6.5 万顶点
        m.CombineMeshes(list.ToArray(), true, true);
        m.RecalculateBounds();
        return m;
    }

    Mesh BuildBarkMesh(HashSet<Vector3Int> logs)
    {
        logs = Subdivide(logs);
        var cube = MeshFactory.BoxUV(new Vector3(CherryVox, CherryVox, CherryVox), TexScale.BarkMeters);
        var list = new List<CombineInstance>();
        foreach (var b in logs)
        {
            if (Enclosed(logs, b)) continue;
            AddVoxelCube(list, cube, b.x, b.y, b.z);
        }
        var m = new Mesh();
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;   // 巨樱粗干细分后顶点数同样超上限
        m.CombineMeshes(list.ToArray(), true, true);
        m.RecalculateBounds();
        return m;
    }

    // 樱树树干 + 三/四向主枝（原木块阶梯），冠底能看见分叉
    Mesh BuildCherryBark(int v)
    {
        var logs = new HashSet<Vector3Int>();
        int top = v == 12 ? 4 : v == 11 ? 6 : 5;
        for (int y = 0; y <= top; y++) logs.Add(new Vector3Int(0, y, 0));
        var dirs = v == 13
            ? new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }
            : new[] { (1, 0), (-1, 0), (0, 1) };
        foreach (var (dx, dz) in dirs)
        {
            logs.Add(new Vector3Int(dx, top, dz));
            logs.Add(new Vector3Int(dx * 2, top, dz * 2));
            logs.Add(new Vector3Int(dx * 2, top + 1, dz * 2));
        }
        return BuildBarkMesh(logs);
    }

    // 巨樱：3x3 逻辑块粗干（2.4m）+ 三层主枝 + 四角阶梯短枝
    Mesh BuildGiantBark()
    {
        var logs = new HashSet<Vector3Int>();
        // 树干 18 块高（14.4m），3x3 格宽（2.4m）
        for (int y = 0; y <= 17; y++)
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                    logs.Add(new Vector3Int(x, y, z));
        // MC 式分枝：从不同高度斜向伸出，近干 2x2、梢部 1x1，枝端托住叶团
        foreach (var (by, ang, len, rise) in GiantBranchDefs)
            for (int d = 2; d <= len; d++)
            {
                var p = BranchTip(ang, d);
                int y = by + Mathf.RoundToInt((d - 2) * rise / (float)(len - 2));
                logs.Add(new Vector3Int(p.x, y, p.y));
                if (d <= 5)
                {
                    logs.Add(new Vector3Int(p.x + 1, y, p.y));
                    logs.Add(new Vector3Int(p.x, y, p.y + 1));
                    logs.Add(new Vector3Int(p.x + 1, y, p.y + 1));
                }
            }
        return BuildBarkMesh(logs);
    }

    // ── 公园喷泉：三层石盘 +「樱之冠」五瓣花雕塑（参考 Bethesda Fountain 的水盘构图）──
    void Fountain(Transform grp, float r)
    {
        Cyl(grp, new Vector3(0, 0.30f, 0), r, 0.60f, ColMat("#c6bfae"));
        Cyl(grp, new Vector3(0, 0.80f, 0), r * 0.88f, 0.40f, ColMat("#bbb4a4"));
        MeshObj(grp, MeshFactory.Disc(r * 0.76f, r * 0.76f, 40, 4f), new Vector3(0, 1.01f, 0), LitTex("water"), default, false, true);
        Cyl(grp, new Vector3(0, 1.90f, 0), 1.15f, 1.80f, ColMat("#cbc4b4"));
        Cyl(grp, new Vector3(0, 2.95f, 0), 2.60f, 0.30f, ColMat("#c3bcac"));
        MeshObj(grp, MeshFactory.Disc(2.35f, 2.35f, 36, 4f), new Vector3(0, 3.11f, 0), LitTex("water"), default, false, true);
        Cyl(grp, new Vector3(0, 4.00f, 0), 0.72f, 1.80f, ColMat("#cbc4b4"));
        Cyl(grp, new Vector3(0, 5.00f, 0), 1.45f, 0.22f, ColMat("#d0c9b9"));
        MeshObj(grp, MeshFactory.Disc(1.25f, 1.25f, 32, 4f), new Vector3(0, 5.12f, 0), LitTex("water"), default, false, true);

        // 石柱托起五瓣樱花雕塑：花瓣尖端上翘、中心金蕊，远看是公园的粉色标志
        Cyl(grp, new Vector3(0, 5.42f, 0), 0.52f, 0.28f, ColMat("#c3bcac"));
        Cyl(grp, new Vector3(0, 6.18f, 0), 0.40f, 1.30f, ColMat("#cfc8b8"));
        Cyl(grp, new Vector3(0, 6.90f, 0), 0.62f, 0.16f, ColMat("#d9b25e"));
        var fl = NewGo("sakura_crown", grp).transform;
        fl.localPosition = new Vector3(0f, 7.10f, 0f);
        fl.localScale = Vector3.one * 1.12f;
        string petalA = "#f7c6d7", petalB = "#f2acc2", petalC = "#fbdde8", gold = "#e8c36a";
        Cyl(fl, new Vector3(0, 0.02f, 0), 0.72f, 0.26f, ColMat("#d9b25e"));
        Cyl(fl, new Vector3(0, 0.16f, 0), 0.46f, 0.20f, ColMat("#c99f45"));
        for (int i = 0; i < 7; i++)
        {
            float a = i * Mathf.PI * 2f / 7f;
            Box(fl, new Vector3(Mathf.Cos(a) * 0.30f, 0.34f, Mathf.Sin(a) * 0.30f),
                new Vector3(0.06f, 0.26f, 0.06f), ColMat(gold));
            Box(fl, new Vector3(Mathf.Cos(a) * 0.30f, 0.49f, Mathf.Sin(a) * 0.30f),
                new Vector3(0.10f, 0.08f, 0.10f), ColMat("#f6dfa0"));
        }
        for (int i = 0; i < 5; i++)
        {
            float aDeg = i * 72f + 18f;
            var pt = NewGo("petal", fl).transform;
            pt.localEulerAngles = new Vector3(-16f, aDeg, 0f);
            Box(pt, new Vector3(0f, 0f, 0.82f), new Vector3(1.05f, 0.14f, 0.85f), CherryMat(petalA));
            Box(pt, new Vector3(0f, 0.10f, 1.46f), new Vector3(0.82f, 0.12f, 0.66f), CherryMat(petalB));
            Box(pt, new Vector3(-0.24f, 0.20f, 1.92f), new Vector3(0.34f, 0.10f, 0.46f), CherryMat(petalC));
            Box(pt, new Vector3(0.24f, 0.20f, 1.92f), new Vector3(0.34f, 0.10f, 0.46f), CherryMat(petalC));
        }
        FountainPetals(grp);
        FountainStreams(grp);
        FountainSpray(grp);
    }

    // 雕塑周围缓缓飘落的花瓣
    void FountainPetals(Transform p)
    {
        var go = NewGo("petal_fall", p);
        go.transform.localPosition = new Vector3(0f, 7.2f, 0f);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true; main.prewarm = true; main.playOnAwake = true;
        main.startLifetime = 3.2f;
        main.startSpeed = 0.45f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.20f);
        main.startColor = new Color(0.99f, 0.80f, 0.88f, 0.95f);
        main.gravityModifier = 0.055f;
        main.maxParticles = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var em = ps.emission; em.rateOverTime = 9f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = 2.0f;
        sh.rotation = new Vector3(-90f, 0f, 0f);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.material = MatLib.Fade("snow_flake", new Color(0.99f, 0.80f, 0.88f, 0.90f));
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        ps.Play();
    }

    // 两层水盘的落水串（细白条 + 粒子，远看能把三层水盘串起来）
    void FountainStreams(Transform grp)
    {
        var wm = ColMat("#cfe8ee");
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            Box(grp, new Vector3(Mathf.Cos(a) * 1.30f, 4.10f, Mathf.Sin(a) * 1.30f),
                new Vector3(0.06f, 1.95f, 0.06f), wm, default, false);
        }
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6f;
            Box(grp, new Vector3(Mathf.Cos(a) * 2.40f, 2.05f, Mathf.Sin(a) * 2.40f),
                new Vector3(0.07f, 2.05f, 0.07f), wm, default, false);
        }
    }

    void FountainSpray(Transform p)
    {
        Spray(p, new Vector3(0, 5.13f, 0), 1.20f, 72f, 1.0f, 1.5f, 75, 0.13f);
        Spray(p, new Vector3(0, 3.12f, 0), 2.30f, 76f, 0.9f, 1.6f, 95, 0.13f);
        Spray(p, new Vector3(0, 5.48f, 0), 0.35f, 20f, 2.2f, 1.1f, 36, 0.10f);
    }

    void Spray(Transform p, Vector3 pos, float radius, float angle, float speed, float life, int rate, float size)
    {
        var go = NewGo("spray", p);
        go.transform.localPosition = pos;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true; main.prewarm = true; main.playOnAwake = true;
        main.startLifetime = life;
        main.startSpeed = speed;
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size * 1.3f);
        main.startColor = new Color(0.92f, 0.97f, 1f, 0.90f);
        main.gravityModifier = 0.65f;
        main.maxParticles = rate * 2;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var em = ps.emission; em.rateOverTime = rate;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = angle;
        sh.radius = radius;
        sh.rotation = new Vector3(-90f, 0f, 0f);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.material = MatLib.Fade("snow_flake", new Color(0.80f, 0.90f, 1f, 0.55f));
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        ps.Play();
    }

    // ── 祭典提灯 / 幟旗 / 狛犬 / 绘马 / 御神签 / 手水 / 酒樽 / 授与所 ──
    Material WarmLanternMat => _warmLanternMat ??= MatLib.UnlitColor(MatLib.Hex("#ffe0a8"));

    void Chochin(Transform grp, Vector3 pos, float r, float h, bool red)
    {
        Cylinder(grp, pos, r, h, red ? LanternMat : WarmLanternMat);
        Box(grp, pos + new Vector3(0f, h * 0.5f + 0.03f, 0f), new Vector3(r * 1.5f, 0.05f, r * 1.5f), ColMat("#2f2a28"), default, false);
        Box(grp, pos - new Vector3(0f, h * 0.5f + 0.03f, 0f), new Vector3(r * 1.5f, 0.05f, r * 1.5f), ColMat("#2f2a28"), default, false);
        Box(grp, pos + new Vector3(0f, h * 0.5f + 0.18f, 0f), new Vector3(0.018f, 0.3f, 0.018f), ColMat("#2f2a28"), default, false);
    }

    void AddWarmLight(Transform grp, Vector3 pos, float range, float power)
    {
        var go = NewGo("warm_light", grp);
        go.transform.localPosition = pos;
        var li = go.AddComponent<Light>();
        li.type = LightType.Point;
        li.color = MatLib.Hex("#ffb878");
        li.range = range;
        li.intensity = 0f;
        li.shadows = LightShadows.None;
        LampLights.Add(li);
        LampLightPower.Add(power);
    }

    void ChochinString(Transform grp, float len, int n)
    {
        float half = len * 0.5f;
        var wood = ColMat("#6b5a42");
        foreach (int s in new[] { -1, 1 })
        {
            Box(grp, new Vector3(s * half, 2.3f, 0f), new Vector3(0.12f, 4.6f, 0.12f), wood);
            Box(grp, new Vector3(s * half, 0.1f, 0f), new Vector3(0.34f, 0.2f, 0.34f), ColMat("#8d8d94"));
            Box(grp, new Vector3(s * (half - 0.25f), 4.45f, 0f), new Vector3(0.5f, 0.08f, 0.08f), wood);
        }
        const int segs = 6;
        var px = new float[segs + 1];
        var py = new float[segs + 1];
        for (int i = 0; i <= segs; i++)
        {
            float t = i / (float)segs;
            px[i] = -half + t * len;
            py[i] = 4.35f - 0.45f * (1f - Mathf.Pow(2f * t - 1f, 2f));
        }
        for (int i = 0; i < segs; i++)
        {
            float dx = px[i + 1] - px[i], dy = py[i + 1] - py[i];
            float l = Mathf.Sqrt(dx * dx + dy * dy);
            float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            Box(grp, new Vector3((px[i] + px[i + 1]) * 0.5f, (py[i] + py[i + 1]) * 0.5f, 0f),
                new Vector3(l, 0.025f, 0.025f), ColMat("#3a2f28"), new Vector3(0f, 0f, ang), false);
        }
        for (int i = 0; i < n; i++)
        {
            float t = (i + 0.5f) / n;
            float x = -half + t * len;
            float y = 4.35f - 0.45f * (1f - Mathf.Pow(2f * t - 1f, 2f));
            Chochin(grp, new Vector3(x, y - 0.38f, 0f), 0.14f, 0.34f, (i & 1) == 0);
            Box(grp, new Vector3(x, y - 0.12f, 0f), new Vector3(0.015f, 0.22f, 0.015f), ColMat("#3a2f28"), default, false);
        }
        AddWarmLight(grp, new Vector3(0f, 3.8f, 0f), 9f, 1.05f);
    }

    void Nobori(Transform grp, bool red)
    {
        var cloth = ColMat(red ? "#b8352f" : "#f2ead8");
        var cream = ColMat("#f2ead8");
        Box(grp, new Vector3(0f, 0.08f, 0f), new Vector3(0.4f, 0.16f, 0.4f), ColMat("#8d8d94"));
        Box(grp, new Vector3(0f, 1.85f, 0f), new Vector3(0.06f, 3.5f, 0.06f), ColMat("#6b5a42"));
        Cylinder(grp, new Vector3(0f, 3.62f, 0f), 0.05f, 0.12f, ColMatGloss("#c9a24a", 0.3f));
        Box(grp, new Vector3(0.3f, 3.5f, 0f), new Vector3(0.65f, 0.05f, 0.05f), ColMat("#6b5a42"));
        Box(grp, new Vector3(0.3f, 2.4f, 0f), new Vector3(0.56f, 2.1f, 0.02f), cloth);
        Box(grp, new Vector3(0.3f, 3.28f, 0.012f), new Vector3(0.56f, 0.22f, 0.012f), cream);
        Cylinder(grp, new Vector3(0.3f, 2.55f, 0.025f), 0.12f, 0.015f, cream, new Vector3(90f, 0f, 0f));
    }

    void Komainu(Transform grp)
    {
        var stone = ColMat("#9a958c");
        var dark = ColMat("#7a756c");
        Box(grp, new Vector3(0f, 0.12f, 0f), new Vector3(1.0f, 0.24f, 0.9f), dark);
        Box(grp, new Vector3(0f, 0.5f, 0f), new Vector3(0.78f, 0.5f, 0.7f), stone);
        Box(grp, new Vector3(0f, 0.95f, -0.05f), new Vector3(0.52f, 0.5f, 0.72f), stone);
        Box(grp, new Vector3(0f, 1.35f, 0.28f), new Vector3(0.46f, 0.4f, 0.4f), stone);
        foreach (int s in new[] { -1, 1 })
        {
            Box(grp, new Vector3(s * 0.17f, 1.6f, 0.22f), new Vector3(0.12f, 0.16f, 0.1f), stone);
            Box(grp, new Vector3(s * 0.18f, 0.72f, 0.28f), new Vector3(0.13f, 0.45f, 0.14f), stone);
        }
        Box(grp, new Vector3(0f, 1.28f, 0.5f), new Vector3(0.22f, 0.16f, 0.16f), stone);
        Box(grp, new Vector3(0f, 1.16f, 0.4f), new Vector3(0.3f, 0.22f, 0.03f), ColMat("#b8352f"), default, false);
        Box(grp, new Vector3(0f, 1.15f, -0.46f), new Vector3(0.14f, 0.34f, 0.14f), stone);
    }

    void EmaRack(Transform grp)
    {
        var wood = ColMat("#6b5a42");
        foreach (int s in new[] { -1, 1 })
            Box(grp, new Vector3(s * 1.05f, 1.1f, 0f), new Vector3(0.12f, 2.2f, 0.12f), wood);
        Box(grp, new Vector3(0f, 2.15f, 0f), new Vector3(2.3f, 0.12f, 0.12f), wood);
        foreach (int s in new[] { -1, 1 })
            Box(grp, new Vector3(0f, 2.3f, s * 0.26f), new Vector3(2.5f, 0.06f, 0.62f), ColMat("#3a3f4a"), new Vector3(s * 18f, 0f, 0f));
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 5; c++)
            {
                float x = -0.9f + c * 0.45f;
                float y = 1.05f + r * 0.36f;
                Box(grp, new Vector3(x, y, 0.09f), new Vector3(0.26f, 0.2f, 0.03f), ColMat(r == 1 ? "#e0c69a" : "#d8b98a"));
                Box(grp, new Vector3(x, y + 0.12f, 0.1f), new Vector3(0.02f, 0.06f, 0.02f), ColMat("#b8352f"), default, false);
                Box(grp, new Vector3(x, y, 0.11f), new Vector3(0.12f, 0.015f, 0.01f), ColMat("#5a3a2a"), default, false);
            }
        Box(grp, new Vector3(0f, 2.02f, 0.1f), new Vector3(0.62f, 0.24f, 0.03f), ColMat("#f2ead8"));
        Box(grp, new Vector3(0f, 2.02f, 0.12f), new Vector3(0.3f, 0.12f, 0.01f), ColMat("#b8352f"), default, false);
    }

    void OmikujiStand(Transform grp)
    {
        var wood = ColMat("#6b5a42");
        Box(grp, new Vector3(0f, 0.45f, 0f), new Vector3(1.5f, 0.9f, 0.65f), wood);
        foreach (int sx in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
                Box(grp, new Vector3(sx * 0.7f, 1.3f, sz * 0.3f), new Vector3(0.07f, 1.8f, 0.07f), wood);
        Box(grp, new Vector3(0f, 2.2f, 0f), new Vector3(1.7f, 0.08f, 0.9f), wood);
        foreach (int s in new[] { -1, 1 })
            Box(grp, new Vector3(0f, 2.35f, s * 0.25f), new Vector3(1.8f, 0.06f, 0.6f), ColMat("#3a3f4a"), new Vector3(s * 18f, 0f, 0f));
        for (int i = 0; i < 5; i++)
        {
            float x = -0.6f + i * 0.3f;
            Box(grp, new Vector3(x, 0.62f, 0.34f), new Vector3(0.22f, 0.16f, 0.02f), ColMat("#a87e52"));
            Box(grp, new Vector3(x, 0.62f, 0.355f), new Vector3(0.1f, 0.03f, 0.01f), ColMat("#f2ead8"), default, false);
        }
        Cylinder(grp, new Vector3(-0.45f, 1.0f, 0f), 0.12f, 0.22f, ColMat("#b8352f"));
        for (int i = 0; i < 6; i++)
            Box(grp, new Vector3(0.45f + (i % 3) * 0.1f, 0.95f + (i / 3) * 0.18f, 0.34f), new Vector3(0.06f, 0.24f, 0.01f), ColMat("#f6f2e8"), default, false);
        Box(grp, new Vector3(0f, 1.85f, 0.34f), new Vector3(1.1f, 0.28f, 0.03f), ColMat("#f2ead8"));
        Box(grp, new Vector3(0f, 1.85f, 0.355f), new Vector3(0.5f, 0.12f, 0.01f), ColMat("#b8352f"), default, false);
        Chochin(grp, new Vector3(0f, 1.95f, 0.55f), 0.1f, 0.24f, false);
    }

    void TemizuyaBasin(Transform grp)
    {
        var stone = ColMat("#9a958c");
        Box(grp, new Vector3(0f, 0.1f, 0f), new Vector3(1.9f, 0.2f, 1.1f), ColMat("#8d8d94"));
        Box(grp, new Vector3(0f, 0.35f, 0f), new Vector3(1.5f, 0.5f, 0.8f), stone);
        MeshObj(grp, MeshFactory.Quad(1.3f, 0.6f), new Vector3(0f, 0.61f, 0f), LitTex("water"));
        Cylinder(grp, new Vector3(0f, 1.15f, -0.45f), 0.08f, 1.2f, ColMat("#8a9a5a"));
        Cylinder(grp, new Vector3(0f, 1.2f, -0.1f), 0.05f, 0.8f, ColMat("#8a9a5a"), new Vector3(90f, 0f, 0f));
        Box(grp, new Vector3(0f, 0.9f, -0.02f), new Vector3(0.03f, 0.5f, 0.03f), ColMat("#7ac0d8"), default, false);
        for (int i = 0; i < 3; i++)
        {
            Box(grp, new Vector3(-0.35f + i * 0.35f, 0.75f, 0.28f), new Vector3(0.04f, 0.4f, 0.04f), ColMat("#a8b878"));
            Cylinder(grp, new Vector3(-0.35f + i * 0.35f, 0.96f, 0.28f), 0.07f, 0.06f, ColMat("#c8c890"));
        }
        for (int i = -2; i <= 2; i++)
            Box(grp, new Vector3(i * 0.55f, 0.8f, -0.62f), new Vector3(0.06f, 1.6f, 0.06f), ColMat("#8a9a5a"));
        foreach (float y in new[] { 0.55f, 1.25f })
            Box(grp, new Vector3(0f, y, -0.62f), new Vector3(2.3f, 0.06f, 0.06f), ColMat("#8a9a5a"));
        Box(grp, new Vector3(1.2f, 1.1f, 0.2f), new Vector3(0.6f, 0.36f, 0.03f), ColMat("#f2ead8"));
    }

    void SakeBarrels(Transform grp)
    {
        var wood = ColMat("#b98d5e");
        var dark = ColMat("#6b4a32");
        Box(grp, new Vector3(0f, 0.08f, 0f), new Vector3(1.5f, 0.16f, 1.1f), ColMat("#8d8d94"));
        foreach (var (x, y) in new[] { (-0.34f, 0.42f), (0.34f, 0.42f), (0f, 0.98f) })
        {
            Cylinder(grp, new Vector3(x, y, 0f), 0.3f, 0.55f, wood);
            Cylinder(grp, new Vector3(x, y + 0.2f, 0f), 0.31f, 0.05f, dark);
            Cylinder(grp, new Vector3(x, y - 0.2f, 0f), 0.31f, 0.05f, dark);
            Box(grp, new Vector3(x, y, 0.31f), new Vector3(0.26f, 0.3f, 0.02f), ColMat("#f2ead8"));
        }
        Cylinder(grp, new Vector3(0f, 0.95f, 0.34f), 0.03f, 1.2f, ColMat("#d8c8a0"), new Vector3(90f, 0f, 0f));
        for (int i = 0; i < 3; i++)
            Box(grp, new Vector3(-0.4f + i * 0.4f, 0.82f, 0.38f), new Vector3(0.12f, 0.3f, 0.02f), ColMat("#f2efe6"), default, false);
    }

    void OfferingBox(Transform grp)
    {
        var wood = ColMat("#6b4a32");
        Box(grp, new Vector3(0f, 0.4f, 0f), new Vector3(2.0f, 0.8f, 0.9f), wood);
        for (int i = 0; i < 7; i++)
            Box(grp, new Vector3(-0.75f + i * 0.25f, 0.82f, 0f), new Vector3(0.12f, 0.04f, 0.7f), ColMat("#8a6a48"));
        Box(grp, new Vector3(0f, 0.62f, 0.47f), new Vector3(1.4f, 0.22f, 0.03f), ColMat("#f2ead8"));
        Box(grp, new Vector3(0f, 0.62f, 0.49f), new Vector3(0.8f, 0.08f, 0.01f), ColMat("#b8352f"), default, false);
    }

    void ShrineOffice(Transform grp)
    {
        var wood = ColMat("#6b4a32");
        var cream = ColMat("#f2ead8");
        Box(grp, new Vector3(0f, 0.1f, 0f), new Vector3(3.0f, 0.2f, 2.4f), ColMat("#8d8d94"));
        Box(grp, new Vector3(0f, 0.5f, 0f), new Vector3(2.8f, 1.0f, 2.2f), LitTex("wood"));
        foreach (int sx in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
                Box(grp, new Vector3(sx * 1.3f, 1.5f, sz * 1.0f), new Vector3(0.1f, 2.0f, 0.1f), wood);
        Box(grp, new Vector3(0f, 1.6f, -1.05f), new Vector3(2.8f, 2.6f, 0.08f), ColMat("#7a5a42"));
        foreach (int sx in new[] { -1, 1 })
            Box(grp, new Vector3(sx * 1.35f, 1.6f, 0f), new Vector3(0.08f, 2.6f, 2.0f), ColMat("#7a5a42"));
        Box(grp, new Vector3(0f, 1.1f, 0.95f), new Vector3(2.6f, 0.14f, 0.35f), wood);
        foreach (int s in new[] { -1, 1 })
            Box(grp, new Vector3(0f, 2.85f, s * 0.6f), new Vector3(3.2f, 0.08f, 1.5f), ColMat("#3a3f4a"), new Vector3(s * 18f, 0f, 0f));
        Box(grp, new Vector3(0f, 3.0f, 0f), new Vector3(3.3f, 0.12f, 0.18f), ColMat("#2b2622"));
        Box(grp, new Vector3(0f, 2.3f, 1.08f), new Vector3(2.6f, 0.5f, 0.03f), cream);
        Box(grp, new Vector3(0f, 2.3f, 1.1f), new Vector3(0.45f, 0.3f, 0.01f), ColMat("#b8352f"), default, false);
        for (int r = 0; r < 2; r++)
            for (int c = 0; c < 5; c++)
                Box(grp, new Vector3(-1.0f + c * 0.5f, 1.4f + r * 0.4f, -0.9f), new Vector3(0.22f, 0.28f, 0.08f),
                    ColMat((r + c) % 2 == 0 ? "#c9553f" : "#e8c46a"));
        Box(grp, new Vector3(0f, 2.0f, 1.05f), new Vector3(1.0f, 0.3f, 0.03f), cream);
        Chochin(grp, new Vector3(-1.0f, 2.55f, 1.15f), 0.13f, 0.3f, true);
        Chochin(grp, new Vector3(1.0f, 2.55f, 1.15f), 0.13f, 0.3f, false);
    }

    void FestivalBoard(Transform grp)
    {
        var wood = ColMat("#6b5a42");
        foreach (int s in new[] { -1, 1 })
            Box(grp, new Vector3(s * 0.95f, 0.9f, 0f), new Vector3(0.1f, 1.8f, 0.1f), wood);
        Box(grp, new Vector3(0f, 1.35f, 0f), new Vector3(2.1f, 1.2f, 0.1f), ColMat("#3a3f4a"));
        Box(grp, new Vector3(0f, 1.35f, 0.06f), new Vector3(1.9f, 1.0f, 0.02f), ColMat("#f2ead8"));
        for (int i = 0; i < 3; i++)
        {
            Box(grp, new Vector3(-0.6f + i * 0.6f, 1.35f, 0.08f), new Vector3(0.45f, 0.7f, 0.01f),
                ColMat(i == 0 ? "#e8c46a" : (i == 1 ? "#c9553f" : "#5a8fc8")));
            Box(grp, new Vector3(-0.6f + i * 0.6f, 1.35f, 0.09f), new Vector3(0.3f, 0.08f, 0.005f), ColMat("#3a2f28"), default, false);
        }
        foreach (int s in new[] { -1, 1 })
            Box(grp, new Vector3(0f, 2.0f, s * 0.25f), new Vector3(2.3f, 0.06f, 0.55f), ColMat("#3a3f4a"), new Vector3(s * 18f, 0f, 0f));
        Box(grp, new Vector3(0f, 1.1f, 0.1f), new Vector3(0.8f, 0.2f, 0.02f), ColMat("#b8352f"), default, false);
    }

    void StoneLantern(Transform grp)
    {
        Box(grp, new Vector3(0, 0.12f, 0), new Vector3(0.72f, 0.24f, 0.72f), ColMat("#8d8d94"));
        Box(grp, new Vector3(0, 0.58f, 0), new Vector3(0.24f, 0.92f, 0.24f), ColMat("#84848b"));
        Box(grp, new Vector3(0, 1.14f, 0), new Vector3(0.56f, 0.34f, 0.56f), ColMat("#9c968c"));
        var glow = MatLib.UnlitColor(MatLib.Hex("#ffdf9a"));
        Box(grp, new Vector3(0, 1.14f, 0.29f), new Vector3(0.26f, 0.20f, 0.03f), glow, default, false);
        Box(grp, new Vector3(0, 1.14f, -0.29f), new Vector3(0.26f, 0.20f, 0.03f), glow, default, false);
        Box(grp, new Vector3(0.29f, 1.14f, 0), new Vector3(0.03f, 0.20f, 0.26f), glow, default, false);
        Box(grp, new Vector3(-0.29f, 1.14f, 0), new Vector3(0.03f, 0.20f, 0.26f), glow, default, false);
        Box(grp, new Vector3(0, 1.38f, 0), new Vector3(0.80f, 0.12f, 0.80f), ColMat("#6f6f76"));
        Box(grp, new Vector3(0, 1.52f, 0), new Vector3(0.28f, 0.16f, 0.28f), ColMat("#8d8d94"));
    }

    void FlowerBed(Transform grp, float w, float d)
    {
        EnsureBloomCanopies();
        if (w <= 0f) w = 5f;
        if (d <= 0f) d = 2.2f;
        float hw = w / 2f, hd = d / 2f;
        Box(grp, new Vector3(0, 0.05f, 0), new Vector3(w - 0.2f, 0.10f, d - 0.2f), ColMat("#5b4a3a"));
        Box(grp, new Vector3(0, 0.08f, -hd + 0.08f), new Vector3(w, 0.16f, 0.16f), ColMat("#b3ac9c"));
        Box(grp, new Vector3(0, 0.08f, hd - 0.08f), new Vector3(w, 0.16f, 0.16f), ColMat("#b3ac9c"));
        Box(grp, new Vector3(-hw + 0.08f, 0.08f, 0), new Vector3(0.16f, 0.16f, d), ColMat("#b3ac9c"));
        Box(grp, new Vector3(hw - 0.08f, 0.08f, 0), new Vector3(0.16f, 0.16f, d), ColMat("#b3ac9c"));
        string[] fc = { "#e8748a", "#f4b6c6", "#f5e6ec", "#d95f7a", "#f0c46a" };
        // 花丛：圆冠绿托 + 双色花团，12 丛两排，避免方块花
        for (int i = 0; i < 12; i++)
        {
            int row = i / 6;
            float fx = -hw + 0.62f + (i % 6) * (w - 1.24f) / 5f;
            float fz = (row == 0 ? -1f : 1f) * hd * 0.30f;
            float s = 0.78f + (i % 3) * 0.10f;
            var leaf = MeshObj(grp, _bloomCanopyA, new Vector3(fx, 0.16f, fz), LitTinted("leaves", "#4b7a46", 2.5f, 2.5f));
            leaf.transform.localScale = new Vector3(0.36f * s, 0.20f * s, 0.32f * s);
            var bloom = MeshObj(grp, _bloomCanopyB, new Vector3(fx + 0.03f, 0.29f, fz - 0.02f), CherryMat(fc[i % fc.Length]));
            bloom.transform.localScale = new Vector3(0.30f * s, 0.17f * s, 0.26f * s);
            bloom.transform.localEulerAngles = new Vector3(0f, i * 53f % 360f, 0f);
            if (i % 2 == 0)
            {
                var bloom2 = MeshObj(grp, _bloomCanopyA, new Vector3(fx - 0.17f, 0.24f, fz + 0.11f),
                    CherryMat(fc[(i + 2) % fc.Length]));
                bloom2.transform.localScale = new Vector3(0.19f * s, 0.12f * s, 0.17f * s);
            }
        }
    }

    // ── 樱花林中央巨樱：粗干/主枝/十几团花冠 + 落叶毯 + 接林道步石 ──
    void GiantCherry(Transform grp)
    {
        EnsureCherryCrowns();
        for (int s = 0; s < 3; s++)
            MeshObj(grp, _giantCrowns[s], Vector3.zero, CherryMat(GiantHex(s)));
        MeshObj(grp, _giantBark, Vector3.zero, LitTinted("bark", "#6d4a2f"));
        // 落叶毯：三色小瓣片合并成 3 个网格
        var petal = MeshFactory.Quad(0.22f, 0.11f);
        string[] pcs = { "#efb3c7", "#e29ab2", "#f6d2dd" };
        var rnd = new System.Random(20261030);
        for (int c = 0; c < pcs.Length; c++)
        {
            var list = new List<CombineInstance>();
            for (int i = 0; i < 110; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float rr = Mathf.Sqrt((float)rnd.NextDouble()) * 11.0f;
                list.Add(new CombineInstance
                {
                    mesh = petal,
                    transform = Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * rr, 0.012f, Mathf.Sin(a) * rr),
                        Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), Vector3.one)
                });
            }
            var m = new Mesh();
            m.CombineMeshes(list.ToArray(), true, true);
            m.RecalculateBounds();
            MeshObj(grp, m, Vector3.zero, ColMat(pcs[c]), default, false, false);
        }
        // 从林道通往树下的步石
        var stone = MeshFactory.Disc(0.55f, 0.5f, 12, 2.4f);
        var stones = new List<CombineInstance>();
        for (int i = 0; i < 7; i++)
        {
            float t = i / 6f;
            stones.Add(new CombineInstance
            {
                mesh = stone,
                transform = Matrix4x4.TRS(new Vector3(Mathf.Lerp(11.2f, 2.6f, t), 0.018f, Mathf.Sin(t * 5f) * 0.35f),
                    Quaternion.Euler(0f, t * 70f, 0f), Vector3.one)
            });
        }
        var sm = new Mesh();
        sm.CombineMeshes(stones.ToArray(), true, true);
        sm.RecalculateBounds();
        MeshObj(grp, sm, Vector3.zero, LitTex("bridge_stone"));
    }

    void Azaleas()
    {
        if (W.AZALEAS == null || W.AZALEAS.Length == 0) return;
        var g = NewGo("Azaleas", Root).transform;
        string[] fc = { "#d76b8f", "#e88ba8", "#f2aec4", "#f7e3ea", "#c45f80" };
        var leafMat = LitTinted("leaves", "#3f6b3c", 2.5f, 2.5f);
        for (int i = 0; i < W.AZALEAS.Length; i++)
        {
            var a = W.AZALEAS[i];
            var grp = NewGo("azalea", g).transform;
            grp.localPosition = new Vector3(a.x, 0f, a.z);
            grp.localEulerAngles = new Vector3(0f, (i * 47f) % 360f, 0f);
            float s = 0.75f + (i * 37 % 100) / 100f * 0.35f;
            string f = fc[i % fc.Length];
            // 方块花丛：绿托 + 两层花块（不用球冠）
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(0.95f * s, 0.44f * s, 0.85f * s), TexScale.LeafMeters),
                new Vector3(0f, 0.22f * s, 0f), leafMat, new Vector3(0f, 18f, 0f));
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(0.78f * s, 0.30f * s, 0.68f * s), TexScale.LeafMeters),
                new Vector3(0.02f, 0.47f * s, -0.02f), CherryMat(f), new Vector3(0f, 32f, 0f));
            MeshObj(grp, MeshFactory.BoxUV(new Vector3(0.46f * s, 0.20f * s, 0.42f * s), TexScale.LeafMeters),
                new Vector3(0.30f * s, 0.38f * s, -0.15f * s),
                CherryMat(i % 3 == 0 ? "#f7e3ea" : MatLib.Shade(f, 1.12f)), new Vector3(0f, 61f, 0f));
        }
        StaticBatchingUtility.Combine(g.gameObject);
    }

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
        StaticBatchingUtility.Combine(g.gameObject);
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
        StaticBatchingUtility.Combine(g.gameObject);
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

    // ── 角色（程序化 3D 低模 + 脚下圆影；位置由 PlayerRig 驱动）──
    void PlayerRigObjects()
    {
        var grp = NewGo("Player", Root).transform;
        grp.localPosition = Spawn;
        Player = grp;
        grp.gameObject.layer = 2;   // Ignore Raycast：相机避障射线不撞玩家自己
        var prb = grp.gameObject.AddComponent<Rigidbody>();
        prb.isKinematic = true;
        prb.useGravity = false;
        var pcap = grp.gameObject.AddComponent<CapsuleCollider>();
        pcap.center = new Vector3(0f, 0.9f, 0f);
        pcap.height = 1.8f;
        pcap.radius = 0.32f;

        PlayerAvatar.Create(grp);

        var sh = NewGo("shadow", grp).transform;
        sh.localPosition = new Vector3(0f, 0.036f, 0f);   // 高于抬高后的钟楼广场（0.030）
        sh.gameObject.layer = 2;
        MeshObj(sh, MeshFactory.Quad(0.84f, 0.84f), Vector3.zero, MatLib.Shadow(new Color(0f, 0f, 0f, 0.22f)), default, false, false);
        PlayerShadow = sh.GetComponentInChildren<Renderer>();
    }
}
