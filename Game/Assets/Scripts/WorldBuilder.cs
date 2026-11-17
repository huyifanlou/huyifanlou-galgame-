using System.Collections.Generic;
using UnityEngine;

// 世界构建器：把 world.json + 导出的 PNG 还原成 web v0.40 的校园（几何/材质配比逐条对位）
// 所有尺寸/坐标/颜色均取自 web 版 CampusScene.jsx 的实参；改动请同步 docs/01 与迁移说明。
public partial class WorldBuilder : MonoBehaviour
{
    public WorldData W { get; private set; }
    public Transform Root { get; private set; }
    public Transform Player { get; private set; }
    public Renderer PlayerShadow { get; private set; }
    public Material LampGlass { get; private set; }
    public Material LampPool { get; private set; }
    public Material WorldSkyMat { get; private set; }
    // 昼夜双窗户：{material, 白天贴图名, 夜晚贴图名}——由 TimeOfDay 在夜里切换
    public readonly List<Material> LeafMats = new List<Material>();   // 叶簇材质（天气：雪天覆白）
    public readonly List<Light> LampLights = new List<Light>();       // 路灯点光源（夜里由 TimeOfDay 点亮）
    public Material DoorLampMat;                                      // 门灯共享材质
    public Material LanternMat;                                       // 骑楼红灯笼（夜里发光）
    Material _warmLanternMat;                                         // 祭典暖白纸提灯（无光照常亮）
    public readonly List<float> LampLightPower = new List<float>();         // 点光源夜间功率（骑楼灯比路灯亮）
    public Transform SignRoot;                                    // 窗玻璃/亮窗单独根，避免被建筑静态合批
    public Material GrassMat;                                        // 草地材质（天气：雨天变暗）
    public Vector3 Spawn = new Vector3(0f, 0f, 4f);

    readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();

    static readonly string[] TREE_CROWNS = { "#3b6b3a", "#457a42", "#4f8044", "#5a8845", "#c08fa5", "#ad9155" };
    static readonly string[] BUSH_GREENS = { "#3a6a3a", "#345f34", "#427a40", "#2e5a2f" };
    static readonly string[] ROCK_COLS = { "#8d8d94", "#7b7b82", "#9aa0a6", "#6f6f76", "#84848b" };
    static readonly string[] REED_COLS = { "#7a8a4a", "#6a7a42", "#85925a" };
    static readonly string[] REED_COLS2 = { "#85925a", "#7a8a4a", "#6a7a42" };
    static readonly HashSet<string> ShopIds = new HashSet<string>
    { "o_silk", "o_tea", "o_grocery", "o_pharm", "o_post", "r_milktea", "r_market", "r_rest", "r_photo", "r_book", "r_conv" };
    // ── 材质缓存 ──────────────────────────────────────────────
    Material LitTex(string tex, float tu = 1f, float tv = 1f)
    {
        if(tex=="water")
        {
            if(!_cache.TryGetValue("ParkWater",out var water)){water=new Material(Shader.Find("Campus/Water")){name="NaturalPondWater"};water.SetFloat("_UseVertexFoam",0);water.SetColor("_DeepColor",MatLib.Hex("#376b61"));water.SetColor("_ShallowColor",MatLib.Hex("#7b9c7e"));_cache["ParkWater"]=water;}return water;
        }
        if(tex=="path")return DetailSurface.Mat("paving","#c2bca9");
        if(tex=="road_mainstone")return DetailSurface.Mat("cobble","#a1aaa3");
        if(tex=="road_cobble")return DetailSurface.Mat("cobble","#a3a59c");
        string k = "L|" + tex + "|" + tu + "|" + tv;
        if (!_cache.TryGetValue(k, out var m)) { m = MatLib.Lit(tex, tu, tv); _cache[k] = m; }
        return m;
    }
    // 钟面：圆盘以外透明，贴在塔身/柱廊上不再出现方形底板的“虚影”
    Material ClockMat()
    {
        if (!_cache.TryGetValue("clock_cut", out var m))
        {
            m = MatLib.CutoutTexture(MatLib.Tex("clock", true, false), 1f, 1f, 0.12f);
            _cache["clock_cut"] = m;
        }
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
    // 樱花冠材质（2026-11-10 卡通重做）：
    // 上一版是「leaves 贴图 × 粉色 tint」——但 leaves 贴图本身是深绿底，相乘后粉色被压成脏褐色
    // （这就是截图里樱花看起来像枯树的原因）。卡通渲染的樱花应该是「纯色大块粉」，
    // 所以改用 MatLib.LitColorBold（纯色 + 粗描边），并把暗面调暖，避免粉冠发灰。
    Material CherryMat(string hex)
    {
        string k = "CH|" + hex;
        if (!_cache.TryGetValue(k, out var m))
        {
            m = MatLib.LitColorBold(MatLib.Hex(hex), 0.0055f);
            // 樱花：暗面偏暖粉（不要冷紫，否则花瓣发灰）
            m.SetColor("_ShadowTint", new Color(0.86f, 0.62f, 0.72f, 1f));
            m.SetFloat("_RimStrength", 0.35f);
            m.SetColor("_RimColor", new Color(1f, 0.92f, 0.96f, 1f));
            // 轻微自发光：仰视/背光时花冠不会整团发黑
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", MatLib.Hex(hex) * 0.22f);
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
        if(m!=null&&m.name.StartsWith("Detail_")){go.transform.localScale=Vector3.one;go.GetComponent<MeshFilter>().sharedMesh=PrecisionMesh.Box(size,.022f,2);}
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = m;
        MatLib.ConfigureSurface(r, size);
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
        MatLib.ConfigureSurface(r, mesh.bounds.size);
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
        BuildRegionalPaving(g);
        foreach (var p in W.PROPS)
        {
            if (RegionalPlaza(p)) continue;
            var grp = NewGo(p.type, g).transform;
            grp.localPosition = new Vector3(p.x, 0f, p.z);
            grp.localEulerAngles = new Vector3(0f, p.ry * Mathf.Rad2Deg, 0f);
            if (TryGardenProp(p, grp)) continue;
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
                    CraftedBench(grp);
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
                    CraftedPlanter(grp,p.w,p.d,p.kind=="green");
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
                        SchoolFontSign(grp,4.8f,1.1f,new Vector3(0,4.6f,.5f),0);
                        SchoolFontSign(grp,4.8f,1.1f,new Vector3(0,4.6f,-.5f),180);
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
                        var stone = new Material(DetailSurface.Mat("brick","#c4bca9")) { name = "clocktower_stone" };
                        stone.SetFloat("_OutlineWidth", 0f);
                        stone.SetFloat("_RimStrength", 0f);
                        MeshObj(grp, PrecisionMesh.StoneBox(new Vector3(5.4f, 0.8f, 5.4f), 2.4f), new Vector3(0f, 0.4f, 0f), stone);
                        MeshObj(grp, PrecisionMesh.StoneBox(new Vector3(4.6f, 4.4f, 4.6f), 2.4f), new Vector3(0f, 3.0f, 0f), stone);
                        ClockTowerDoor(grp);
                        MeshObj(grp, PrecisionMesh.StoneBox(new Vector3(2.3f, 0.28f, 0.3f), 2.4f), new Vector3(0f, 3.6f, 2.4f), stone);
                        MeshObj(grp, PrecisionMesh.StoneBox(new Vector3(5.3f, 0.3f, 5.3f), 2.4f), new Vector3(0f, 5.35f, 0f), stone);
                        MeshObj(grp, MeshFactory.TaperedBox(4.0f, 4.0f, 3.3f, 3.3f, 5.6f, 2.4f), new Vector3(0f, 5.5f, 0f), stone);
                        MeshObj(grp, PrecisionMesh.StoneBox(new Vector3(4.1f, 0.26f, 4.1f), 2.4f), new Vector3(0f, 11.2f, 0f), stone);
                        MeshObj(grp, PrecisionMesh.StoneBox(new Vector3(3.3f, 3.1f, 3.3f), 2.4f), new Vector3(0f, 12.85f, 0f), stone);
                        var clockMat = ClockMat();
                        foreach (var (px, pz, ry) in new[] { (0f, 1.70f, 0f), (0f, -1.70f, 180f), (1.70f, 0f, 90f), (-1.70f, 0f, -90f) })
                            Face(grp, 1.62f, 1.62f, new Vector3(px, 13.1f, pz), clockMat, ry);
                        // 钟楼下小阳台
                        MeshObj(grp, PrecisionMesh.StoneBox(new Vector3(3.9f, 0.12f, 3.9f), 2.4f), new Vector3(0f, 11.45f, 0f), stone);
                        for (float x = -1.7f; x <= 1.71f; x += 0.85f)
                            foreach (int sz in new[] { -1, 1 })
                                Box(grp, new Vector3(x, 11.85f, sz * 1.85f), new Vector3(0.08f, 0.75f, 0.08f), MatMetal("#4a4f58"));
                        MeshObj(grp, PrecisionMesh.StoneBox(new Vector3(4.1f, 0.24f, 4.1f), 2.4f), new Vector3(0f, 14.5f, 0f), stone);
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
        gameObject.AddComponent<FontSystemDriver>();
        // 叙事游戏不需要失控帧率：硬锁 60。
        // 不能用 vSync=1：165Hz 等高分屏上垂直同步会让 Unity 忽略 targetFrameRate，直接按刷新率跑。
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        if (string.IsNullOrEmpty(Args.Str("-shot")))
            Screen.SetResolution(1600, 900, FullScreenMode.Windowed);   // 默认窗口，可拖动边缘调整；F11 切全屏
        QualitySettings.shadowDistance = 90f;QualitySettings.shadowCascades=4;QualitySettings.shadowCascade4Split=new Vector3(.08f,.22f,.50f);QualitySettings.pixelLightCount=4;

        var w = BuildWorld();
        if (w == null) return;
        // 后处理栈（泛光/移轴景深/色彩分级/暗角）——挂到主相机
        var cam = Camera.main;
        if (cam != null && cam.GetComponent<PostFX>() == null) cam.gameObject.AddComponent<PostFX>();
        if (cam != null && Args.Has("-noao")) cam.GetComponent<PostFX>().AOSStrength = 0f;
        if (cam != null && Args.Has("-nofx")) cam.GetComponent<PostFX>().enabled = false;

        // 开场 logo 要先于 HUD/小地图决定 Live，避免它们盖在 logo 上面
        var intro = GetComponent<IntroScreen>();
        if (intro == null) intro = gameObject.AddComponent<IntroScreen>();
        intro.Init();

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

        // 对话系统（-scenario / B 键）+ C 键人物自选菜单
        var dlg = GetComponent<DialogueManager>();
        if (dlg == null) dlg = gameObject.AddComponent<DialogueManager>();
        dlg.Init(tod);
        var picker = GetComponent<CharacterPicker>();
        if (picker == null) picker = gameObject.AddComponent<CharacterPicker>();
        picker.Init(Player != null ? Player.GetComponentInChildren<PlayerAvatar>() : null, tod);
        if (rig != null) rig.Picker = picker;
        // 按键侧边栏最后挂：OnGUI 压在剧情压暗层与人物菜单之上
        if (GetComponent<HelpSidebar>() == null) gameObject.AddComponent<HelpSidebar>();

        // 天气（雨/雪）：Y 键循环；-weather 直接指定
        var wth = GetComponent<Weather>();
        if (wth == null) wth = gameObject.AddComponent<Weather>();
        var wet = new List<Material>
        {
            LitTex("path"), LitTex("road_mainstone"), LitTex("road_cobble"),
            LitTex("track"), LitTex("court_basket"), LitTex("court_tennis"), LitTex("concrete"),
            LitTex("roadband_v"), LitTex("roadband_h"), LitTex("bridge_stone"), LitTex("wood"), GrassMat,
        };
        wth.Init(Player, tod, wet, LeafMats, _snowLayerR, _snowLayerMat);
        if(Args.Has("-menumotioncheck"))gameObject.AddComponent<MenuWaterReview>();
        if(Args.Has("-renewalcheck"))gameObject.AddComponent<RenewalReview>();
        if(Args.Has("-menuroutecheck"))gameObject.AddComponent<MenuRouteReview>();
        if(Args.Has("-submissioncapture"))gameObject.AddComponent<SubmissionCapture>();

        // 室内（家 / 教学楼）：一次建好并隐藏，走到门口按 F 进入
        var im = GetComponent<InteriorManager>();
        if (im == null) im = gameObject.AddComponent<InteriorManager>();
        if (rig != null) rig.BindInterior(im);   // 先绑定：im.Init 里 -indoor 会直接进房
        im.Init(this, rig, tod, wth, mm);
        var progress=gameObject.AddComponent<ProgressManager>();progress.Init(this, rig, tod, dlg, im);
        gameObject.AddComponent<QuestJournal>().Init(this,rig,dlg,progress,im);
        gameObject.AddComponent<StandeeDisplay>().Init(this,rig,im);
        if(Args.Has("-standeecheck")||Args.Has("-standeepreview"))gameObject.AddComponent<StandeeAudit>().Init(this,rig,dlg,im);
        if(Args.Has("-precisionaudit"))StartCoroutine(DumpPrecisionAfterFrame(im));
        if (Args.Has("-audit-school")) SceneEntityAudit.DumpSchool(this, Args.Str("-audit-school"));
        if (Args.Has("-audit-residential")) SceneEntityAudit.DumpResidential(this, Args.Str("-audit-residential"));
    }

    System.Collections.IEnumerator DumpPrecisionAfterFrame(InteriorManager im)
    {yield return new WaitForEndOfFrame();PrecisionAudit.Dump(this,im,Args.Str("-precisionaudit"));}

    public WorldData BuildWorld()
    {
        // 默认进入小镇（V5 总平面）；-campus 回到原校园世界
        bool campus = Args.Has("-campus");
        W = campus ? CampusData.Build() : TownData.Build();
        if (W == null) return null;
        TextureLibrary.SetWorld(W);
        _residentialEntryPaths = ResidentialExterior.EntryPaths(W);
        if (W.RIVER != null) Spawn = new Vector3(-17f, 0f, -4f);   // 钟楼广场
        Root = NewGo("World", transform).transform;

        DoorLampMat = MatLib.LitColor(MatLib.Hex("#ffdf9a"), 0.40f);
        LanternMat = MatLib.LitColor(MatLib.Hex("#b8352f"), 0.20f);
        SignRoot = NewGo("Signage", Root).transform;
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
        BuildAmusementClosure();
        Debug.Log("[Exterior] 实体立面与单层昼夜玻璃；旧立面覆盖路径已退役");
        Villa();
        Trees();
        ForestBelt();
        if(Args.Has("-treecheck"))gameObject.AddComponent<TreeReview>();
        if(Args.Has("-gardencheck"))gameObject.AddComponent<GardenReview>();
        if(Args.Has("-weathercheck"))gameObject.AddComponent<WeatherReview>();
        RailTunnels();
        Distant();
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

    // 单层天空：角度渐变和小尺度云，不再把 64×128 的云贴图放大成灰色巨片。
    void Sky()
    {
        var day = NewGo("WorldSky", Root);
        day.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Dome(420f, 32, 20);
        var dr = day.AddComponent<MeshRenderer>();
        WorldSkyMat = new Material(Resources.Load<Shader>("Shaders/WorldSky"));
        dr.sharedMaterial = WorldSkyMat;
        dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        dr.receiveShadows = false;

    }

    void GroundAndPaths()
    {
        float gs = W.GROUND_SIZE > 10 ? W.GROUND_SIZE : 200f;
        if (W.RIVER != null)
        {
            // 小镇：地形在河道处断开（下沉河床），UV 由网格按世界坐标给出
            GrassMat = LandscapeSurface.Create(W);
            var groundMesh = TerrainHoleMesh(TexScale.GrassPeriod, 0f);
            var groundGo = MeshObj(Root, groundMesh, Vector3.zero, GrassMat);
            var groundCol = groundGo.AddComponent<MeshCollider>();
            groundCol.sharedMesh = groundMesh;
        }
        else
        {
            var greps = TexScale.Repeat(gs, gs, TexScale.GrassPeriod);
            GrassMat = LandscapeSurface.Create(W);
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
            // 路口只保留横向路面；纵向面在同一位置切开，消除两套砖纹共面闪烁。
            if (r.axis == "z")
            {
                var cuts = new List<(float, float)>();
                foreach (var cross in W.ROADS)
                    if (cross.axis == "x" && r.at >= cross.from && r.at <= cross.to)
                        cuts.Add((cross.at - cross.width / 2f, cross.at + cross.width / 2f));
                cuts.Sort((a, b) => a.Item1.CompareTo(b.Item1));
                var clear = new List<(float, float)>();
                foreach (var span in spans) clear.AddRange(SplitByGaps(span.a, span.b, cuts));
                spans = clear;
            }
            string tex = r.kind == "mainstone" ? "road_mainstone" : (r.kind == "cobble" ? "road_cobble" : "path");
            var roadMat = LitTex(tex);
            var crossGaps = r.sidewalk ? XRoadGaps(r) : new List<(float, float)>();

            foreach (var (a, b) in spans)
            {
                float len = b - a;
                if (len < 0.3f) continue;
                float mid = (a + b) / 2f;
                float y = 0.024f;
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
        float entryHeight = Mathf.Max(EntranceGroundY(x, z), ResidentialPathHeight(x, z));
        if(StandAccess.TryHeight(W.BUILDINGS,x,z,out float standHeight))return Mathf.Max(entryHeight,standHeight);
        foreach(var pavilion in W.BUILDINGS)if(PavilionFootprint.Uses(pavilion)&&Mathf.Abs(x-pavilion.pos[0])<pavilion.HalfWX&&Mathf.Abs(z-pavilion.pos[1])<pavilion.HalfWZ)return Mathf.Max(.08f,entryHeight);
        if (W.RIVER != null)
            foreach (var b in W.RIVER.bridges)
            {
                float span = W.RIVER.width + 1.6f;
                if (Mathf.Abs(x - b.x) <= b.width / 2f + 0.2f && Mathf.Abs(z - RZ(b.x)) <= span / 2f + 1.2f)
                    return Mathf.Max(0.05f, entryHeight);
            }
        foreach (var r in W.ROADS)
        {
            float along = r.axis == "z" ? z : x;
            float perp = r.axis == "z" ? x - r.at : z - r.at;
            if (along < r.from - 0.3f || along > r.to + 0.3f) continue;
            float ap = Mathf.Abs(perp);
            if (ap <= r.width / 2f + 0.03f) return Mathf.Max(0.024f, entryHeight);
            if (!r.sidewalk || ap > r.width / 2f + 1.48f) continue;
            bool gap = false;
            foreach (var o in W.ROADS)
            {
                if (o.axis == r.axis) continue;
                if (r.at < o.from - 1f || r.at > o.to + 1f) continue;
                if (along > o.at - o.width / 2f - 1.4f && along < o.at + o.width / 2f + 1.4f) { gap = true; break; }
            }
            if (!gap) return Mathf.Max(0.12f, entryHeight);
        }
        // 图书馆/礼堂柱廊：台面 0.45m + 三级踏步（由内向外 0.48/0.35/0.22），可以直接走上门廊
        foreach (var b in W.BUILDINGS)
        {
            if (b.style != "library" && b.style != "hall") continue;
            float yaw = b.Yaw * Mathf.Deg2Rad;
            float dx = x - b.pos[0], dz = z - b.pos[1];
            float c = Mathf.Cos(yaw), s = Mathf.Sin(yaw);
            float lx = dx * c - dz * s;
            float lz = dx * s + dz * c;
            float t = lz - b.size[2] / 2f;   // 0 = 主墙，向正门外为正
            float pw = (b.style == "library" ? Mathf.Min(8f, b.size[0] * 0.4f) : 11f) / 2f;
            if (t >= 3.0f && t <= 4.35f && Mathf.Abs(lx) <= pw)
                return t < 3.47f ? 0.48f : t < 3.87f ? 0.35f : 0.22f;
            if (t >= 0f && t < 3.0f && Mathf.Abs(lx) <= pw) return 0.45f;
        }
        return entryHeight;
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
            if(field){SchoolTrack.Build(go.transform,def.size[0],def.size[1],DetailSurface.Mat("rubber","#a76255"));continue;}
            string tex = def.kind == "tennis" ? "court_tennis" : "court_basket";
            MeshObj(go.transform, MeshFactory.Quad(def.size[0], def.size[1], 1f, 1f), Vector3.zero,
                LitTex(tex));
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
            EllipseWater(t,rx,rz,.012f);
            CraftedShore(t,rx,rz);
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
                StoneLantern(lt);
            }
            CraftedDeck(t,rx,rz);
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
        var root=NewGo("Flowers",Root).transform;var batch=new RegionMeshBatch();int i=0;
        foreach(var f in W.FLOWERS){if(NearGrovePath(f.x,f.z,.4f))continue;GardenDetail.Wildflowers(batch,new Vector3(f.x,GroundY(f.x,f.z)+.015f,f.z),i++);}
        batch.Flush(root);
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
        if(LampGlass==null)LampGlass=MatLib.LitColor(MatLib.Hex("#d5dbe4"),.35f);LampGlass.name="StreetLampDiffuser";
        var root=NewGo("Lamps",Root).transform;
        foreach(var l in W.LAMPS)
        {
            var lamp=NewGo("lamp",root).transform;lamp.localPosition=new Vector3(l.x,W.RIVER!=null?GroundY(l.x,l.z):0,l.z);
            // Axially symmetric pendant: no reversed outreach arm, diffuser and light share one axis.
            var b=new RegionMeshBatch();var steel=DetailSurface.Mat("metal","#435953");var stone=DetailSurface.Mat("stone","#92988b");
            b.Bevel(stone,new Vector3(0,.12f,0),new Vector3(.38f,.24f,.38f),.035f);
            b.Branch(steel,new Vector3(0,.18f,0),new Vector3(0,3.45f,0),.067f);
            foreach(float y in new[]{.28f,.42f,3.25f})b.Cylinder(steel,new Vector3(0,y,0),new Vector3(0,y+.065f,0),.10f);
            b.Cylinder(steel,new Vector3(0,3.43f,0),new Vector3(0,3.68f,0),.24f);
            b.Cylinder(steel,new Vector3(0,3.69f,0),new Vector3(0,3.76f,0),.34f);
            b.Cylinder(LampGlass,new Vector3(0,3.405f,0),new Vector3(0,3.435f,0),.21f);
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));b.Cylinder(steel,radial*.215f+Vector3.up*3.44f,radial*.215f+Vector3.up*3.68f,.012f);}
            b.Flush(lamp);
            var lightGo=NewGo("shielded_downlight",lamp);lightGo.transform.localPosition=new Vector3(0,3.39f,0);lightGo.transform.localRotation=Quaternion.Euler(90,0,0);
            var li=lightGo.AddComponent<Light>();li.type=LightType.Spot;li.spotAngle=118;li.innerSpotAngle=72;li.range=10;li.color=MatLib.Hex("#ffdda5");li.intensity=0;li.shadows=LightShadows.None;li.shadowBias=.025f;li.shadowNormalBias=.12f;li.shadowCustomResolution=1024;li.renderMode=LightRenderMode.ForcePixel;
            LampLights.Add(li);LampLightPower.Add(2.7f);
        }
    }

    // ── 四栋建筑（平顶/坡顶/公共；含屋顶设备/侧翼/雨棚/招牌/大门等全部附加件）──
    void Buildings()
    {
        if (SignRoot == null) SignRoot = NewGo("Signage", Root).transform;
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
            if(PavilionFootprint.Uses(b)){bodyCol.enabled=false;Destroy(bodyCol);}
            if (b.style == "shrine") { ShrineHall(grp, b); continue; }
            if (SchoolExterior.Uses(b)) { BuildSchoolBuilding(grp, b); continue; }
            if (ResidentialExterior.Uses(b)) { BuildResidence(grp, b); continue; }

            if(b.id.StartsWith("f_")&&ClosedAreas.AmusementClosed)
            {Box(grp,Vector3.zero,new Vector3(w,h,d),DetailSurface.Mat("plaster",b.wall));Box(grp,new Vector3(0,h/2+.10f,0),new Vector3(w+.2f,.2f,d+.2f),DetailSurface.Mat("roof",b.roof));continue;}
            BuildPrecisionArchitecture(grp,b);
        }
    }

    // ── 神社本殿/拜殿：基坛 + 柱网 + 深出檐双坡屋顶 + 斗栱/椽头 + 注连绳 ──
    void ShrineHall(Transform grp, BuildingDef b)
    {
        float w = b.size[0], h = b.size[1], d = b.size[2];
        float baseY = -h / 2f;
        var stone = DetailSurface.Mat("stone","#a69f8e");
        var wood = DetailSurface.Mat("timber","#8c6047");
        var woodDark = DetailSurface.Mat("timber","#674535");
        var roof = DetailSurface.Mat("roof",b.roof);
        var shoji = DetailSurface.Mat("wallpaper","#dfd8c4");

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

    // 主角家与居民区共用实体门窗构造，避免保留第二套旧立面。
    void Villa()
    {
        var def=W.VILLA;if(def==null)return;
        var root=NewGo("Villa",Root).transform;
        root.localPosition=new Vector3(def.x,def.h/2,def.z);
        root.localEulerAngles=new Vector3(0,def.face==1?0:180,0);
        root.gameObject.AddComponent<BoxCollider>().size=new Vector3(def.w,def.h,def.d);
        BuildResidence(root,ResidentialExterior.Home(def),true);
    }

    // ── 边界密林：134~262m 环带，6 种树（松/阔叶/杉/樱/落叶/灌木），每种合并成 1 个网格 ──
    void ForestBelt()
    {
        if (W.RIVER == null) return;
        var g = NewGo("ForestBelt", Root).transform;
        var prototypes=new GameObject[4];
        for(int i=0;i<4;i++)
        {
            var prototype=NewGo("林带原型_"+i,g);NaturalTree.Build(prototype.transform,i==1?1:i==2?2:0,137+i,true,false,i==3?"#a29260":null);prototype.SetActive(false);prototypes[i]=prototype;
        }
        var groups=new Dictionary<string,List<CombineInstance>>();var materials=new Dictionary<string,Material>();
        var rnd=new System.Random(20261022);
        void Add(float x,float z,float h)
        {
            float roll=(float)rnd.NextDouble();int type=roll<.45f?1:roll<.77f?0:roll<.85f?2:3;
            float scale=h/(type==1?4.9f:type==2?5.4f:4.25f);
            var transform=Matrix4x4.TRS(new Vector3(x,RimH(x,z),z),Quaternion.Euler(0,(float)rnd.NextDouble()*360,0),Vector3.one*scale);
            foreach(var filter in prototypes[type].GetComponentsInChildren<MeshFilter>(true))
            {
                var mat=filter.GetComponent<Renderer>().sharedMaterial;
                string key=Mathf.FloorToInt(x/40)+"_"+Mathf.FloorToInt(z/40)+"_"+mat.name;
                if(!groups.TryGetValue(key,out var parts)){groups[key]=parts=new List<CombineInstance>();materials[key]=mat;}
                parts.Add(new CombineInstance{mesh=filter.sharedMesh,transform=transform*filter.transform.localToWorldMatrix});
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

        foreach(var entry in groups)
        {
            var mesh=new Mesh{name="林带枝叶_"+entry.Key,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(entry.Value.ToArray(),true,true);mesh.RecalculateBounds();
            var go=NewGo("林带分区_"+entry.Key,g);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=materials[entry.Key];renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        foreach(var prototype in prototypes){foreach(var filter in prototype.GetComponentsInChildren<MeshFilter>(true))Destroy(filter.sharedMesh);Destroy(prototype);}
        Debug.Log("[TreeRebuild] distant forest / shared branch-and-leaf prototypes / spatial batches "+groups.Count);
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
        foreach (var t in W.TREES) Tree(g, t.x, t.z, t.v);
        // 小镇的边界树改由 ForestBelt 统一生成（更密、合并网格）；校园世界仍用数据里的环带
        if (W.RIVER == null) foreach (var t in W.BORDER_TREES) Tree(g, t.x, t.z, t.v);
    }

    void Tree(Transform parent,float x,float z,int v)
    {
        var root=NewGo("tree",parent).transform;root.localPosition=new Vector3(x,0,z);
        if(v>=10&&v<=13){BuildCherryTree(root,x,z,v);return;}
        RefinedTree(root,v,x*.37f+z*.23f);
    }

    // 树冠块：世界尺度 UV（每面密度恒定 160px/米）+ 叶簇贴图 × 颜色
    GameObject LeafBox(Transform p, Vector3 pos, Vector3 size, string hex)
        => MeshObj(p, MeshFactory.BoxUV(size, TexScale.LeafMeters), pos, LeafMat(hex));

    // 卡通树叶材质（2026-11-10 重做）：
    // 上一版是「leaves 贴图 × tint」，但贴图是深绿底噪点，相乘后所有树都发暗发脏。
    // 卡通渲染的树冠应该是「纯色大块」——叶子的层次交给 shader 的 ramp 和冠团错落，
    // 而不是贴图噪点。所以改成纯色材质，只保留一点点叶簇贴图的不规则感。
    Material LeafMat(string hex)
    {
        string k = "LF|" + hex;
        if (!_cache.TryGetValue(k, out var m))
        {
            m = MatLib.LitColor(MatLib.Hex(hex), 0f);
            // 叶面：描边细一点，暗面偏冷绿（避免整团黑）
            m.SetFloat("_OutlineWidth", 0.0028f);
            m.SetColor("_ShadowTint", new Color(0.55f, 0.72f, 0.55f, 1f));
            m.SetFloat("_RimStrength", 0.22f);
            _cache[k] = m;
            if (!LeafMats.Contains(m)) LeafMats.Add(m);
        }
        return m;
    }

    // 树干块：树皮贴图 × 树色
    GameObject BarkBox(Transform p, Vector3 pos, Vector3 size, string hex)
        => MeshObj(p, MeshFactory.BoxUV(size, TexScale.BarkMeters), pos, LitTinted("bark", hex));

    // 松/柏的锥形树冠：锥面 UV 按周长/高度展开（密度同为 160px/米）
    void LeafCone(Transform p, float radius, float height, float baseY, string hex)
        => MeshObj(p, MeshFactory.Cone(radius, height, 6, true, TexScale.LeafMeters), new Vector3(0, baseY, 0), LeafMat(hex));



    // ── 公园喷泉：三层石盘 +「樱之冠」五瓣花雕塑（参考 Bethesda Fountain 的水盘构图）──
    void Fountain(Transform grp,float r)
    {CraftedFountain(grp,r);}

    // 雕塑周围缓缓飘落的花瓣


    // 两层水盘的落水串（细白条 + 粒子，远看能把三层水盘串起来）




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
        if(LampGlass==null)LampGlass=MatLib.LitColor(MatLib.Hex("#d5dbe4"),.35f);
        var b=new RegionMeshBatch();var stone=DetailSurface.Mat("stone","#969b8c");var cap=DetailSurface.Mat("stone","#787f73");
        b.Bevel(stone,new Vector3(0,.10f,0),new Vector3(.72f,.20f,.72f),.045f);
        b.Branch(stone,new Vector3(0,.2f,0),new Vector3(0,.95f,0),.17f);
        b.Bevel(cap,new Vector3(0,.95f,0),new Vector3(.62f,.12f,.62f),.025f);
        b.Bevel(LampGlass,new Vector3(0,1.15f,0),new Vector3(.40f,.30f,.40f),.018f);
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})b.Bevel(stone,new Vector3(x*.25f,1.15f,z*.25f),new Vector3(.09f,.36f,.09f),.014f);
        b.Add(cap,MeshFactory.HipRoof(.80f,.80f,.23f,.04f,1),new Vector3(0,1.34f,0),Vector3.one,Quaternion.identity);
        b.Cylinder(stone,new Vector3(0,1.55f,0),new Vector3(0,1.65f,0),.07f);b.Flush(grp);AddWarmLight(grp,new Vector3(0,1.15f,0),3.2f,.48f);
    }

    void FlowerBed(Transform grp,float w,float d)
    {CraftedPlanter(grp,w>0?w:5,d>0?d:2.2f);}

    void Azaleas()
    {
        GroveUnderstory();var root=NewGo("Azaleas",Root).transform;var batch=new RegionMeshBatch();int i=0;
        foreach(var plant in W.AZALEAS){if(InCherryGrove(plant.x,plant.z))continue;GardenDetail.Shrub(batch,new Vector3(plant.x,GroundY(plant.x,plant.z),plant.z),i++,i%3==0?"#e1c7d0":"#c69aa9");}
        batch.Flush(root);
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
        var root=NewGo("Rockeries",Root).transform;int seed=0;
        foreach(var r in W.ROCKERIES)
        {
            var g=NewGo("NaturalStoneGroup",root).transform;g.localPosition=new Vector3(r.x,0,r.z);var b=new RegionMeshBatch();
            for(int k=0;k<5;k++)
            {float a=k*2.39996f+seed;float size=(k==0?1.7f:.65f+(k%3)*.25f)*r.s;var at=new Vector3(k==0?0:Mathf.Cos(a)*1.25f*r.s,size*.28f,k==0?0:Mathf.Sin(a)*.86f*r.s);b.Add(DetailSurface.Mat("stone",k%2==0?"#9d9f8e":"#8e9586"),PrecisionMesh.Box(new Vector3(size,size*.68f,size*.77f),size*.17f,3),at,Vector3.one,Quaternion.Euler(k*7,seed*37+k*71,k*9));}
            GardenDetail.Shrub(b,new Vector3(.7f*r.s,.10f,-.4f*r.s),seed,null,.65f*r.s);b.Flush(g);seed++;
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

    void Bush(Transform parent,float x,float z,int i)
    {var root=NewGo("bush",parent).transform;root.localPosition=new Vector3(x,0,z);RefinedBush(root,i);}

    // ── 池塘芦苇（池沿细杆丛，黄绿三色）──
    void Reeds()
    {
        var root=NewGo("Reeds",Root).transform;var batch=new RegionMeshBatch();int seed=0;
        foreach(var reed in W.REEDS)
        {for(int i=0;i<7;i++){float a=i*2.39996f+seed;var start=new Vector3(reed.x+Mathf.Cos(a)*.16f,GroundY(reed.x,reed.z),reed.z+Mathf.Sin(a)*.16f);var top=start+new Vector3(Mathf.Cos(a)*.14f,reed.h*(.65f+i*.055f),Mathf.Sin(a)*.14f);batch.Branch(GardenDetail.Mat("#87916a"),start,top,.009f);batch.Branch(GardenDetail.Mat("#9f8c62"),top-Vector3.up*.17f,top+Vector3.up*.06f,.024f,false);batch.Add(GardenDetail.Mat("#7b8b59"),NaturalTree.Spray(i,1),Vector3.Lerp(start,top,.5f),Vector3.one*.42f,Quaternion.Euler(0,a*Mathf.Rad2Deg,0));}seed++;}
        batch.Flush(root);
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

        PlayerAvatar.Create(grp, CharacterDef.FromId(Args.Str("-avatar")));

        // First-person viewpoint has neither a body mesh nor a fake character shadow.

    }
}
