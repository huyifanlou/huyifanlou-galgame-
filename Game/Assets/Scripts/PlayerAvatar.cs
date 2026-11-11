using UnityEngine;

// 角色定义：调色板 + 发型 + 服装 + 脸参数。樊楼为默认（与 docs/55 定稿一致）。
// 镜荧/景楠 为第一卷 NPC 样张确认稿（docs/96 首批；docs/97 修订：镜荧红眼中分、景楠立领夹克）。
public class CharacterDef
{
    public Color skinBody, skinFace, skinFaceD;
    public Color hair, hairL, hairD, hairTintD;
    public Color coat, coatD, shirt, shirtD, tie, tieD, pants, shoe, sole, belt, silver, badgeBlue;
    public Color brow, eyeW, iris, irisL, irisHi, pupil, mouth;
    public Color dress, dressD, dressTrim, dressAccent;
    public Color lace, accentD;
    public string outfit = "sailor";   // sailor | dress | jacket | maid | school | darkops
    public string hairStyle = "short"; // short | long
    public string face = "default";    // default | vampire
    public bool fangs;
    public bool male, female, elfEars, cap, headband, bangs;
    public string id, label;

    static Color H(string s) => MatLib.Hex(s);

    // ── 调色板口径（docs/104 卡通材质化）──
    // 卡通渲染的亮面由 albedo × 太阳直接给足，暗面由 _ShadowTint 统一压成一块「色阶」而不是连续渐变。
    // 所以这里把上一版写实口径的调色板整体「提亮 + 提纯」：灰调加饱和度、深色抬明度（纯黑在
    // 3 阶 ramp 下会糊成一坨，看不出结构）。人设辨识位（镜荧白发红眼 / 景楠黑发 / 樊楼金发 /
    // 莉莉雅绿发精灵 / 瑶清寒黑长直紫瞳 / 紫薇灰发大檐帽）保持原色相不动。

    public static CharacterDef FanLou => new CharacterDef
    {
        skinBody = H("#e0cab6"), skinFace = H("#e0cab6"), skinFaceD = H("#c9b39e"),
        hair = H("#f0c74e"), hairL = H("#ffe487"), hairD = H("#cf9d33"), hairTintD = new Color(0.90f, 0.82f, 0.64f),
        coat = H("#4a63a2"), coatD = H("#3a4f85"), shirt = H("#ffffff"), shirtD = H("#e4e9f4"),
        tie = H("#e0455a"), tieD = H("#b32f44"), pants = H("#2e3d68"), shoe = H("#2b3142"),
        sole = H("#1e222c"), belt = H("#262b38"), silver = H("#cdd2da"), badgeBlue = H("#3d7cc4"),
        brow = H("#6a4f2c"), eyeW = H("#ffffff"), iris = H("#3d7cc4"), irisL = H("#6fb2e8"),
        irisHi = H("#cfe9ff"), pupil = H("#1e4a80"), mouth = H("#a86a5e"),
        outfit = "sailor", hairStyle = "short", face = "default", fangs = false,
        id = "", label = "樊楼",
    };

    public static CharacterDef JingNan => new CharacterDef
    {
        skinBody = H("#e0cab6"), skinFace = H("#e0cab6"), skinFaceD = H("#c9b39e"),
        hair = H("#2b2f3a"), hairL = H("#48505f"), hairD = H("#1a1d26"), hairTintD = new Color(0.62f, 0.66f, 0.74f),
        coat = H("#3a5560"), coatD = H("#2c434d"), shirt = H("#f7f6f2"), shirtD = H("#dfe2da"),
        tie = H("#e0455a"), tieD = H("#b32f44"), pants = H("#2e3d68"), shoe = H("#eef0f2"),
        sole = H("#48505f"), belt = H("#262b38"), silver = H("#cdd2da"), badgeBlue = H("#3d7cc4"),
        brow = H("#2f2a26"), eyeW = H("#ffffff"), iris = H("#7a5b3e"), irisL = H("#a0805e"),
        irisHi = H("#e0ccb4"), pupil = H("#2a1e14"), mouth = H("#a86a5e"),
        outfit = "jacket", hairStyle = "short", face = "default", fangs = false,
        id = "jingnan", label = "景楠",
    };

    public static CharacterDef JingYing => new CharacterDef
    {
        skinBody = H("#e0cab6"), skinFace = H("#e0cab6"), skinFaceD = H("#c9b39e"),
        // 白发在卡通渲染下极易过曝（亮面 = albedo × 太阳 1.15 × ramp，再加环境光/边缘光，
        // albedo 接近 1.0 会直接截断成纯白、发丝纹理全丢）。三档拉开对比、整体压一档留高光余量。
        hair = H("#dde5f0"), hairL = H("#f4f8ff"), hairD = H("#aab5c8"), hairTintD = new Color(0.82f, 0.86f, 0.92f),
        coat = H("#2c3652"), coatD = H("#202a42"), shirt = H("#2c3652"), shirtD = H("#202a42"),
        tie = H("#3d7cc4"), tieD = H("#2c3652"), pants = H("#202a42"), shoe = H("#2b3142"),
        sole = H("#1e222c"), belt = H("#3d7cc4"), silver = H("#d5dae2"), badgeBlue = H("#3d7cc4"),
        brow = H("#9aa4b6"), eyeW = H("#ffffff"), iris = H("#d4303f"), irisL = H("#f4666f"),
        irisHi = H("#ffd4d8"), pupil = H("#6a0f1e"), mouth = H("#a86a5e"),
        dress = H("#2c3652"), dressD = H("#202a42"), dressTrim = H("#d5dae2"), dressAccent = H("#3d7cc4"),
        lace = H("#ffffff"), accentD = H("#2f5c96"),
        outfit = "dress", hairStyle = "long", face = "vampire", fangs = true,
        id = "jingying", label = "镜荧",
    };

    // 莉莉雅：精灵女仆（偏绿发·蓝瞳·尖耳·白褶发箍·黑白女仆·红领结·白围裙·童稚）docs/98
    public static CharacterDef LiLiYa => new CharacterDef
    {
        skinBody = H("#e0cab6"), skinFace = H("#e0cab6"), skinFaceD = H("#c9b39e"),
        hair = H("#93d18c"), hairL = H("#cbf0b6"), hairD = H("#6ba865"), hairTintD = new Color(0.78f, 0.88f, 0.74f),
        coat = H("#3a3e48"), coatD = H("#262a32"), shirt = H("#ffffff"), shirtD = H("#e4e9f4"),
        tie = H("#e0455a"), tieD = H("#b32f44"), pants = H("#2e3d68"), shoe = H("#2b3142"),
        sole = H("#1e222c"), belt = H("#262b38"), silver = H("#cdd2da"), badgeBlue = H("#3d7cc4"),
        brow = H("#6d7d5c"), eyeW = H("#ffffff"), iris = H("#4a95dc"), irisL = H("#7cbdf0"),
        irisHi = H("#cfe9ff"), pupil = H("#1e4a80"), mouth = H("#a86a5e"),
        dress = H("#3a3e48"), dressD = H("#262a32"), dressTrim = H("#f7f6f2"), dressAccent = H("#e0455a"),
        lace = H("#ffffff"), accentD = H("#eef0f2"),
        outfit = "maid", hairStyle = "long", face = "default", fangs = false,
        female = true, elfEars = true, headband = true,
        id = "liliya", label = "莉莉雅",
    };

    // 瑶清寒：冷感校服（黑长直侧扫刘海·紫瞳·白长袖遮腕·藏青背心·藏青百褶裙·红领结蓝宝石）docs/98
    public static CharacterDef YaoQingHan => new CharacterDef
    {
        skinBody = H("#e0cab6"), skinFace = H("#e0cab6"), skinFaceD = H("#c9b39e"),
        hair = H("#2b2f3a"), hairL = H("#48505f"), hairD = H("#1a1d26"), hairTintD = new Color(0.62f, 0.66f, 0.74f),
        coat = H("#43588a"), coatD = H("#33456f"), shirt = H("#ffffff"), shirtD = H("#e4e9f4"),
        tie = H("#e0455a"), tieD = H("#b32f44"), pants = H("#2e3d68"), shoe = H("#2b3142"),
        sole = H("#1e222c"), belt = H("#262b38"), silver = H("#cdd2da"), badgeBlue = H("#3d7cc4"),
        brow = H("#3f3a46"), eyeW = H("#ffffff"), iris = H("#9166c8"), irisL = H("#bda0ec"),
        irisHi = H("#e4d8ff"), pupil = H("#4a2f6e"), mouth = H("#a86a5e"),
        dress = H("#43588a"), dressD = H("#33456f"), dressTrim = H("#e0455a"), dressAccent = H("#3d7cc4"),
        lace = H("#ffffff"), accentD = H("#b32f44"),
        outfit = "school", hairStyle = "long", face = "default", fangs = false,
        female = true, bangs = true,
        id = "yaoqinghan", label = "瑶清寒",
    };

    // 紫薇：暗部队长（男性·灰白发·大檐帽·金边立领藏青军装·勋章·白手套·胡茬疤痕）docs/98
    public static CharacterDef ZiWei => new CharacterDef
    {
        skinBody = H("#e2ccb2"), skinFace = H("#e2ccb2"), skinFaceD = H("#cdb69c"),
        hair = H("#c9ced8"), hairL = H("#e2e6ec"), hairD = H("#9aa0aa"), hairTintD = new Color(0.80f, 0.82f, 0.88f),
        coat = H("#36466e"), coatD = H("#24304e"), shirt = H("#f7f6f2"), shirtD = H("#dfe2da"),
        tie = H("#e8bb52"), tieD = H("#c09232"), pants = H("#24304e"), shoe = H("#1e222c"),
        sole = H("#14161c"), belt = H("#262b38"), silver = H("#e8bb52"), badgeBlue = H("#4a72b0"),
        brow = H("#54545e"), eyeW = H("#f4f6f8"), iris = H("#7e7e88"), irisL = H("#a0a0aa"),
        irisHi = H("#d8d8e0"), pupil = H("#30303a"), mouth = H("#906864"),
        dress = H("#36466e"), dressD = H("#24304e"), dressTrim = H("#e8bb52"), dressAccent = H("#c94a4a"),
        lace = H("#f7f6f2"), accentD = H("#c09232"),
        outfit = "darkops", hairStyle = "short", face = "default", fangs = false,
        male = true, cap = true,
        id = "ziwei", label = "紫薇",
    };

    public static CharacterDef FromId(string id)
    {
        switch (id)
        {
            case "jingying": return JingYing;
            case "jingnan": return JingNan;
            case "liliya": return LiLiYa;
            case "yaoqinghan": return YaoQingHan;
            case "ziwei": return ZiWei;
            default: return FanLou;
        }
    }
}

// 《我的世界》风格方块人。默认男主樊楼；-avatar 可切换镜荧/景楠 供验收。
// 金发长裙是觉醒后的女体形态，不是默认男主。
// 只做视觉与动画，不挂碰撞；碰撞仍由 Player 上的 CapsuleCollider 负责。
public class PlayerAvatar : MonoBehaviour
{
    // 1 像素 = 1.8m / 32px
    const float PX = 0.05625f;

    Transform _model, _body, _head, _legL, _legR, _armL, _armR;
    Material _skin, _hair, _hairD, _hairThin, _coat, _coatD, _shirt, _shirtD;
    Material _tie, _tieD, _pants, _shoe, _sole, _belt, _silver, _badgeBlue;
    Material _dress, _dressD, _dressTrim, _dressAccent;
    Material _lace, _accentD, _gem;
    CharacterDef _def;
    float _phase, _blend, _runBlend;

    public static PlayerAvatar Create(Transform parent, CharacterDef def = null)
    {
        var go = new GameObject("Avatar");
        go.transform.SetParent(parent, false);
        go.layer = 2;
        var av = go.AddComponent<PlayerAvatar>();
        av._def = def ?? CharacterDef.FanLou;
        av.Build();
        return av;
    }

    void Build()
    {
        var d = _def;
        // 卡通材质口径（docs/104）：
        //   · 人物是近景主体，全部走 LitColorBold（描边 0.0060）——方块人的轮廓靠描边才立得住；
        //   · 人物 _ShadowTint 单独调成「暖 + 浅」：世界那套冷紫暗面用在皮肤上会发青发脏；
        //   · 脸部贴片单独去描边（见下，反向外扩会沿脸片 UV 边界炸开一圈黑框）。
        _skin = SkinMat(d.skinBody);
        var hairTex = HairTexture(d);
        _hair = HairMat(hairTex, Color.white, OutlineBold);
        _hairD = HairMat(hairTex, d.hairTintD, OutlineMid);
        _hairThin = HairMat(hairTex, Color.white, OutlineFine);   // 薄发片（侧/后发壳）
        _coat = ClothMat(d.coat);
        _coatD = ClothMat(d.coatD);
        _shirt = ClothMat(d.shirt);
        _shirtD = ClothMat(d.shirtD);
        _tie = ClothMid(d.tie);
        _tieD = ClothMid(d.tieD);
        _pants = ClothMat(d.pants);
        _shoe = ClothMat(d.shoe);
        _sole = ClothMat(d.sole);
        _belt = ClothMid(d.belt);
        _silver = ClothFine(d.silver);
        _badgeBlue = ClothFine(d.badgeBlue);
        _dress = ClothMat(d.dress);
        _dressD = ClothMat(d.dressD);
        _dressTrim = ClothMid(d.dressTrim);
        _dressAccent = ClothMid(d.dressAccent);
        _lace = ClothMat(d.lace);
        _accentD = ClothFine(d.accentD);
        _gem = ClothFine(d.iris);   // 宝石：细描边 + 强高光，别把高光糊掉
        if (_gem.shader.name == "Campus/Toon") _gem.SetFloat("_SpecularStrength", 0.55f);

        // 髋部枢轴：腿 12px、身 12px、头 8px，总高 32px = 1.8m
        _model = Node(transform, "model", Vector3.zero);
        _body = Node(_model, "body", V(0f, 12f, 0f));

        // 身体：水手校服（樊楼/景楠）或晚礼服（镜荧）
        if (_def.outfit == "sailor")
        {
        Block(_body, "shirtBody", V(0f, 6f, 0f), S(8f, 12f, 4f), _shirt);

        // 水手领：后披肩两道白边 + 肩带 + 前 V 领 + 白边
        Block(_body, "collarBack", V(0f, 9.9f, -2.08f), S(8.1f, 4.2f, 0.12f), _coat);
        Block(_body, "collarBackStripe1", V(0f, 9.05f, -2.20f), S(8.1f, 0.30f, 0.06f), _shirt);
        Block(_body, "collarBackStripe2", V(0f, 9.65f, -2.20f), S(8.1f, 0.30f, 0.06f), _shirt);
        Block(_body, "collarWrap", V(0f, 11.45f, 0f), S(8.1f, 0.9f, 4.1f), _coat);
        Block(_body, "collarTop", V(0f, 11.45f, 2.07f), S(8f, 0.9f, 0.12f), _coat);
        Block(_body, "collarTopStripe", V(0f, 11.05f, 2.14f), S(8f, 0.22f, 0.06f), _shirt);
        BlockR(_body, "collarV_L", V(-2.0f, 9.0f, 2.08f), S(1.4f, 5.0f, 0.12f), new Vector3(0f, 0f, 34f), _coat);
        BlockR(_body, "collarV_R", V(2.0f, 9.0f, 2.08f), S(1.4f, 5.0f, 0.12f), new Vector3(0f, 0f, -34f), _coat);
        BlockR(_body, "collarVStripe_L", V(-1.45f, 9.0f, 2.18f), S(0.45f, 5.0f, 0.06f), new Vector3(0f, 0f, 34f), _shirt);
        BlockR(_body, "collarVStripe_R", V(1.45f, 9.0f, 2.18f), S(0.45f, 5.0f, 0.06f), new Vector3(0f, 0f, -34f), _shirt);

        // 红领巾：结 + 两条锥形飘带 + 银扣
        Block(_body, "scarfKnot", V(0f, 10.0f, 2.34f), S(1.4f, 1.0f, 0.18f), _tie);
        Block(_body, "scarfPin", V(0f, 10.6f, 2.42f), S(0.5f, 0.28f, 0.10f), _silver);
        Taper(_body, "scarfTailL", V(-0.45f, 9.5f, 2.30f), 0.9f, 0.16f, 0.28f, 0.10f, 2.8f, new Vector3(0f, 0f, 194f), _tie);
        Taper(_body, "scarfTailR", V(0.45f, 9.5f, 2.30f), 0.9f, 0.16f, 0.28f, 0.10f, 2.8f, new Vector3(0f, 0f, 166f), _tieD);

        // 衬衫门襟、胸袋、校徽、腰带
        Block(_body, "placket", V(0f, 4.9f, 2.06f), S(0.28f, 6.4f, 0.06f), _shirtD);
        Block(_body, "pocket", V(-2.5f, 5.8f, 2.06f), S(1.7f, 1.5f, 0.08f), _shirt);
        Block(_body, "pocketSeam", V(-2.5f, 6.55f, 2.11f), S(1.7f, 0.12f, 0.04f), _shirtD);
        Block(_body, "badge", V(-2.5f, 6.0f, 2.12f), S(0.7f, 0.8f, 0.08f), _silver);
        Block(_body, "badgeCore", V(-2.5f, 6.0f, 2.17f), S(0.3f, 0.4f, 0.06f), _badgeBlue);
        Block(_body, "beltWrap", V(0f, 1.3f, 0f), S(8.1f, 0.8f, 4.1f), _belt);
        Block(_body, "buckle", V(0f, 1.3f, 2.12f), S(0.9f, 0.6f, 0.10f), _silver);
        Block(_body, "buckleCore", V(0f, 1.3f, 2.18f), S(0.4f, 0.3f, 0.06f), _coatD);

        // 背部中缝
        Block(_body, "backSeam", V(0f, 4.8f, -2.06f), S(0.16f, 7.2f, 0.06f), _shirtD);
        }
        else if (_def.outfit == "dress")
        {
            // 晚礼服（镜荧·docs/97 修订）：蕾丝领 / 红宝石胸扣 / 银扣 / 腰封结 / 双层裙摆 / 后背蝴蝶结
            Block(_body, "dressBodice", V(0f, 6f, 0f), S(8f, 12f, 4f), _dress);
            Block(_body, "laceCollar", V(0f, 11.7f, 2.06f), S(8f, 0.6f, 0.12f), _lace);
            Block(_body, "broochSet", V(0f, 9.9f, 2.10f), S(1.2f, 0.6f, 0.10f), _dressTrim);
            Block(_body, "broochGem", V(0f, 9.3f, 2.12f), S(1.2f, 0.6f, 0.08f), _gem);
            Block(_body, "clasp1", V(0f, 6.5f, 2.06f), S(1.0f, 0.5f, 0.08f), _dressTrim);
            Block(_body, "clasp2", V(0f, 4.5f, 2.06f), S(1.0f, 0.5f, 0.08f), _dressTrim);
            Block(_body, "seamL", V(-3.9f, 6f, 2.06f), S(0.2f, 7f, 0.06f), _dressD);
            Block(_body, "seamR", V(3.9f, 6f, 2.06f), S(0.2f, 7f, 0.06f), _dressD);
            Block(_body, "sash", V(0f, 2.2f, 0f), S(8.1f, 1.6f, 4.1f), _dressAccent);
            Block(_body, "sashKnot", V(-1.5f, 2.2f, 2.10f), S(1.0f, 1.6f, 0.12f), _accentD);
            BlockR(_body, "sashTail1", V(-1.5f, 0f, 2.26f), S(0.5f, 3f, 0.08f), new Vector3(-2.86f, 0f, 0f), _accentD);
            BlockR(_body, "sashTail2", V(-1.0f, 0.3f, 2.24f), S(0.5f, 2f, 0.08f), new Vector3(-2.86f, 0f, 0f), _accentD);
            Taper(_body, "skirt", V(0f, -8f, 0f), 11f, 5f, 8f, 4f, 10f, Vector3.zero, _dress);
            Taper(_body, "skirtTier", V(0f, -3.3f, 0f), 9.75f, 4.7f, 9.55f, 4.6f, 0.6f, Vector3.zero, _dressTrim);
            Block(_body, "skirtHem", V(0f, -7.8f, 0f), S(11f, 0.5f, 5f), _dressAccent);
            Block(_body, "bowL", V(-1.5f, -2f, -2.6f), S(1.0f, 1.0f, 0.12f), _dressAccent);
            Block(_body, "bowR", V(1.5f, -2f, -2.6f), S(1.0f, 1.0f, 0.12f), _dressAccent);
            Block(_body, "bowKnot", V(0f, -2f, -2.65f), S(1.0f, 1.0f, 0.14f), _accentD);
            Block(_body, "bowTailL", V(-0.75f, -4.5f, -2.7f), S(0.5f, 3f, 0.08f), _accentD);
            Block(_body, "bowTailR", V(0.75f, -4.2f, -2.7f), S(0.5f, 2.4f, 0.08f), _accentD);
            Block(_body, "backSeam", V(0f, 4.8f, -2.06f), S(0.16f, 7.2f, 0.06f), _dressD);
        }
        else if (_def.outfit == "jacket")
        {
            // 立领休闲夹克（景楠·docs/97）：完全非校服，与樊楼水手服切分
            Block(_body, "jackBody", V(0f, 6f, 0f), S(8f, 12f, 4f), _coat);
            Block(_body, "collarStand", V(0f, 11.6f, 0f), S(6f, 0.8f, 4.1f), _coatD);
            Block(_body, "teeNotch", V(0f, 11.6f, 2.06f), S(1f, 0.8f, 0.12f), _shirt);
            Block(_body, "teeV", V(0f, 10.8f, 2.06f), S(1f, 0.8f, 0.10f), _shirt);
            Block(_body, "zip", V(-0.25f, 5.5f, 2.06f), S(0.5f, 9f, 0.08f), _silver);
            Block(_body, "pocketFlapL", V(-2.5f, 6f, 2.06f), S(2f, 0.5f, 0.10f), _coatD);
            Block(_body, "pocketFlapR", V(2.5f, 6f, 2.06f), S(2f, 0.5f, 0.10f), _coatD);
            Block(_body, "pocketSideL", V(-3.5f, 4.5f, 2.06f), S(0.3f, 2.5f, 0.08f), _coatD);
            Block(_body, "pocketSideR", V(3.5f, 4.5f, 2.06f), S(0.3f, 2.5f, 0.08f), _coatD);
            Block(_body, "jackHem", V(0f, 0.4f, 0f), S(8.1f, 0.8f, 4.1f), _coatD);
            Block(_body, "collarBack", V(0f, 11.6f, -2.06f), S(6f, 0.8f, 0.12f), _coatD);
            Block(_body, "backSeam", V(0f, 6f, -2.06f), S(0.3f, 9f, 0.06f), _coatD);
        }
        else if (_def.outfit == "maid")
        {
            // 精灵女仆（莉莉雅·docs/98）：黑连衣裙 + 白翻领 + 红领结 + 白围裙 + 白褶裙摆
            Block(_body, "dressBodice", V(0f, 6f, 0f), S(8f, 12f, 4f), _dress);
            Block(_body, "collarL", V(-1.6f, 11.2f, 1.9f), S(2.2f, 1.4f, 0.5f), _lace);
            Block(_body, "collarR", V(1.6f, 11.2f, 1.9f), S(2.2f, 1.4f, 0.5f), _lace);
            Block(_body, "ribbonKnot", V(0f, 10.4f, 2.1f), S(1.2f, 0.9f, 0.16f), _tie);
            Block(_body, "ribbonTailL", V(-0.4f, 9.2f, 2.1f), S(0.4f, 1.6f, 0.10f), _tieD);
            Block(_body, "ribbonTailR", V(0.4f, 9.2f, 2.1f), S(0.4f, 1.6f, 0.10f), _tieD);
            Block(_body, "apronBib", V(0f, 7f, 2.06f), S(4.4f, 5f, 0.14f), _lace);
            Block(_body, "apronWaist", V(0f, 3.4f, 2.08f), S(6f, 0.8f, 0.14f), _lace);
            Block(_body, "apronKnot", V(0f, 3.4f, 2.16f), S(1.2f, 0.9f, 0.12f), _dressTrim);
            Taper(_body, "skirt", V(0f, -7f, 0f), 9.5f, 4.6f, 7.5f, 4f, 8f, Vector3.zero, _dress);
            Block(_body, "frillHem", V(0f, -6.6f, 0f), S(9.8f, 0.7f, 4.8f), _lace);
            Block(_body, "backSeam", V(0f, 6f, -2.06f), S(0.16f, 7f, 0.06f), _dressD);
        }
        else if (_def.outfit == "school")
        {
            // 冷感校服（瑶清寒·docs/98）：白衬衫 + 藏青背心 + 红领结蓝宝石 + 藏青百褶裙
            Block(_body, "shirtBody", V(0f, 6f, 0f), S(8f, 12f, 4f), _shirt);
            Block(_body, "vestFrontL", V(-2f, 6f, 2.06f), S(2.6f, 9f, 0.14f), _coat);
            Block(_body, "vestFrontR", V(2f, 6f, 2.06f), S(2.6f, 9f, 0.14f), _coat);
            Block(_body, "vestBack", V(0f, 6f, -2.06f), S(7.4f, 9f, 0.14f), _coat);
            BlockR(_body, "collarV_L", V(-1.4f, 9.4f, 2.12f), S(1.2f, 3.6f, 0.08f), new Vector3(0f, 0f, 30f), _shirt);
            BlockR(_body, "collarV_R", V(1.4f, 9.4f, 2.12f), S(1.2f, 3.6f, 0.08f), new Vector3(0f, 0f, -30f), _shirt);
            Block(_body, "ribbonKnot", V(0f, 10.2f, 2.2f), S(1.4f, 0.9f, 0.16f), _tie);
            Block(_body, "ribbonBowL", V(-0.9f, 10.4f, 2.2f), S(0.8f, 0.7f, 0.12f), _tie);
            Block(_body, "ribbonBowR", V(0.9f, 10.4f, 2.2f), S(0.8f, 0.7f, 0.12f), _tie);
            Block(_body, "ribbonGem", V(0f, 10.2f, 2.3f), S(0.5f, 0.5f, 0.08f), _badgeBlue);
            Block(_body, "ribbonTailL", V(-0.3f, 9f, 2.2f), S(0.35f, 1.8f, 0.08f), _tieD);
            Block(_body, "ribbonTailR", V(0.3f, 9f, 2.2f), S(0.35f, 1.8f, 0.08f), _tieD);
            Taper(_body, "skirt", V(0f, -6f, 0f), 10f, 4.6f, 7.6f, 4f, 7f, Vector3.zero, _coat);
            Block(_body, "skirtHem", V(0f, -5.6f, 0f), S(10.2f, 0.4f, 4.8f), _coatD);
            Block(_body, "backSeam", V(0f, 6f, -2.12f), S(0.16f, 7f, 0.06f), _shirtD);
        }
        else if (_def.outfit == "darkops")
        {
            // 暗部军装（紫薇·docs/98）：藏青大衣 + 金边立领 + 肩章 + 勋章 + 排扣 + 腰带
            Block(_body, "coatBody", V(0f, 6f, 0f), S(8f, 12f, 4f), _coat);
            Block(_body, "collarStand", V(0f, 11.6f, 0f), S(6.4f, 1f, 4.2f), _coatD);
            Block(_body, "collarGold", V(0f, 11.9f, 0f), S(6.5f, 0.3f, 4.25f), _silver);
            Block(_body, "collarFront", V(0f, 11.4f, 2.08f), S(2f, 1.2f, 0.14f), _coatD);
            Block(_body, "placket", V(0f, 5.5f, 2.08f), S(0.5f, 9f, 0.08f), _coatD);
            for (int i = 0; i < 4; i++) Block(_body, "btn" + i, V(0f, 8.5f - i * 2f, 2.14f), S(0.4f, 0.4f, 0.06f), _silver);
            Block(_body, "epauletteL", V(-4f, 11f, 0f), S(1.4f, 0.5f, 4f), _silver);
            Block(_body, "epauletteR", V(4f, 11f, 0f), S(1.4f, 0.5f, 4f), _silver);
            Block(_body, "medalBar", V(-2.4f, 8.6f, 2.1f), S(2f, 0.5f, 0.1f), _dressAccent);
            Block(_body, "medal1", V(-3f, 7.8f, 2.12f), S(0.6f, 0.6f, 0.08f), _silver);
            Block(_body, "medal2", V(-2f, 7.8f, 2.12f), S(0.6f, 0.6f, 0.08f), _dressAccent);
            Block(_body, "beltWrap", V(0f, 1.6f, 0f), S(8.1f, 1f, 4.1f), _belt);
            Block(_body, "buckle", V(0f, 1.6f, 2.12f), S(1f, 0.8f, 0.1f), _silver);
            Block(_body, "backSeam", V(0f, 6f, -2.06f), S(0.2f, 8f, 0.06f), _coatD);
        }

        // 头：8×8×8
        _head = Node(_body, "head", V(0f, 16f, 0f));
        Block(_head, "head", Vector3.zero, S(8f, 8f, 8f), _skin);
        var faceTex = _def.face == "vampire" ? FaceTextureVampire(_def) : FaceTexture(_def);
        if (Args.Has("-dumpface"))
        {
            var dumpPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "face-debug.png");
            System.IO.File.WriteAllBytes(dumpPath, faceTex.EncodeToPNG());
            Debug.Log("[Avatar] face dumped: " + dumpPath);
        }
        var faceMat = FaceMat(faceTex);
        FacePlate(_head, V(0f, 0f, 4.03f), S(8f, 8f, 0.10f), faceMat);

        // 发壳：short=完整短发壳；long=侧/后壳下垂过肩（镜荧）
        // long 侧壳置于胸前一层（z 2.1~3.4），与手臂/袖子零相交，修两侧穿模
        bool lng = _def.hairStyle == "long";
        float sideY = lng ? -16f : -3.4f;
        float sideH = lng ? 20f : 7.4f;
        float hemY = lng ? -16.05f : -3.45f;
        float sideZ = lng ? 2.7f : -0.2f;
        float sideBd = lng ? 1.2f : 6.2f;
        float sideTd = lng ? 1.4f : 6.8f;
        float hemD = lng ? 1.3f : 6.3f;
        // 长发垂缕收在臂内缘(x=4)以内：摆臂扫不到；短发壳仍贴头侧
        float sideX = lng ? 3.5f : 4.05f;
        // 发壳分「厚/薄」两套材质：hairCap/hairCrown/hairBrim 是有体积的厚壳（粗描边）；
        // sideShell/backShell 只有 0.6~0.9px 厚，粗描边会在脸两侧叠成一道黑竖条（黑发角色最明显），
        // 所以薄片一律走 _hairThin（细描边）。
        HairVolume(_head, "hairCap", V(0f, 3.4f, -0.05f), 8.5f, 8.5f, 7.0f, 7.0f, 1.5f, new Vector3(-3f, 0f, 0f), _hair);
        HairVolume(_head, "hairCrown", V(0f, 4.5f, -0.5f), 7.0f, 5.6f, 5.4f, 4.4f, 1.2f, new Vector3(-6f, 0f, 0f), _hair);
        Block(_head, "hairBrim", V(0f, 4.05f, 3.85f), S(8.3f, 0.5f, 0.55f), _hair);
        if (lng)
        {
            // 头侧发壳只罩头段：缺了它侧视会露出头侧皮肤（垂缕已前移不罩头侧）
            HairVolume(_head, "sideHeadL", V(-4.05f, -3.4f, -0.2f), 0.6f, 6.2f, 0.95f, 6.8f, 7.4f, Vector3.zero, _hairThin);
            HairVolume(_head, "sideHeadR", V(4.05f, -3.4f, -0.2f), 0.6f, 6.2f, 0.95f, 6.8f, 7.4f, Vector3.zero, _hairThin);
        }
        HairVolume(_head, "sideShellL", V(-sideX, sideY, sideZ), 0.6f, sideBd, 0.95f, sideTd, sideH, Vector3.zero, _hairThin);
        HairVolume(_head, "sideShellR", V(sideX, sideY, sideZ), 0.6f, sideBd, 0.95f, sideTd, sideH, Vector3.zero, _hairThin);
        HairVolume(_head, "backShell", V(0f, sideY, -4.1f), 7.4f, 0.9f, 8.3f, 0.9f, sideH, Vector3.zero, _hairThin);

        // 发壳底边：一条深色收边，保持完整轮廓
        Block(_head, "backHem", V(0f, hemY, -4.5f), S(7.5f, 0.3f, 0.18f), _hairD);
        Block(_head, "sideHemL", V(-sideX, hemY, sideZ), S(0.9f, 0.3f, hemD), _hairD);
        Block(_head, "sideHemR", V(sideX, hemY, sideZ), S(0.9f, 0.3f, hemD), _hairD);

        // 精灵尖耳（莉莉雅）：从头侧向外上方挑出，两段收尖
        if (_def.elfEars)
        {
            BlockR(_head, "earL", V(-4.3f, 0.8f, -0.2f), S(1.6f, 0.5f, 1.6f), new Vector3(0f, 0f, 30f), _skin);
            BlockR(_head, "earR", V(4.3f, 0.8f, -0.2f), S(1.6f, 0.5f, 1.6f), new Vector3(0f, 0f, -30f), _skin);
            BlockR(_head, "earTipL", V(-5.2f, 1.9f, -0.2f), S(1.1f, 0.35f, 1.1f), new Vector3(0f, 0f, 42f), _skin);
            BlockR(_head, "earTipR", V(5.2f, 1.9f, -0.2f), S(1.1f, 0.35f, 1.1f), new Vector3(0f, 0f, -42f), _skin);
        }

        // 女仆白褶发箍（莉莉雅）：压在发际前缘的一圈白色荷叶边
        if (_def.headband)
        {
            Block(_head, "headband", V(0f, 4.1f, 3.2f), S(7.6f, 1.1f, 0.5f), _lace);
            Block(_head, "headbandTop", V(0f, 4.7f, 1.2f), S(7.2f, 0.6f, 4f), _lace);
            for (int i = -3; i <= 3; i++) Block(_head, "hbRuffle" + i, V(i * 1.05f, 4.7f, 3.5f), S(0.7f, 0.7f, 0.3f), _lace);
        }

        // 大檐帽（紫薇）：帽身 + 金帽墙 + 前檐 + 帽徽
        if (_def.cap)
        {
            Block(_head, "capTop", V(0f, 4.7f, -0.2f), S(8.4f, 1.4f, 7.8f), _coatD);
            Block(_head, "capBand", V(0f, 3.8f, -0.1f), S(8.5f, 0.8f, 8f), _coat);
            Block(_head, "capGold", V(0f, 3.5f, -0.1f), S(8.6f, 0.3f, 8.1f), _silver);
            Block(_head, "visor", V(0f, 3.5f, 4.7f), S(7.6f, 0.4f, 2.6f), _coatD);
            Block(_head, "capEmblem", V(0f, 4.1f, 3.9f), S(1f, 0.8f, 0.2f), _silver);
        }

        // 手臂 4×12×4：西装袖 9px + 白袖口 + 手
        _armL = Node(_body, "armL", V(-6f, 12f, 0f));
        _armR = Node(_body, "armR", V(6f, 12f, 0f));
        Arm(_armL);
        Arm(_armR);

        // 腿 4×12×4：深蓝长裤 + 黑鞋
        _legL = Node(_body, "legL", V(-2f, 0f, 0f));
        _legR = Node(_body, "legR", V(2f, 0f, 0f));
        Leg(_legL);
        Leg(_legR);

        SetLayer(_model.gameObject, 2);
    }

    void Arm(Transform p)
    {
        if (_def.outfit == "sailor")
        {
            Block(p, "sleeve", V(0f, -4.2f, 0f), S(4f, 8.4f, 4f), _shirt);
            Block(p, "cuff", V(0f, -9.1f, 0f), S(4.05f, 1.4f, 4.05f), _coat);
            Block(p, "cuffStripe", V(0f, -9.62f, 0f), S(4.1f, 0.22f, 4.1f), _shirt);
            Block(p, "hand", V(0f, -10.9f, 0f), S(4f, 2.2f, 4f), _skin);
        }
        else if (_def.outfit == "dress")
        {
            Block(p, "sleeve", V(0f, -4.2f, 0f), S(4f, 8.4f, 4f), _dress);
            Block(p, "cuff", V(0f, -9.1f, 0f), S(4.05f, 1.4f, 4.05f), _dressTrim);
            Block(p, "cuffLace", V(0f, -9.9f, 0f), S(4.1f, 0.5f, 4.1f), _lace);
            Block(p, "hand", V(0f, -10.9f, 0f), S(4f, 2.2f, 4f), _skin);
        }
        else if (_def.outfit == "jacket")
        {
            Block(p, "sleeve", V(0f, -4.2f, 0f), S(4f, 8.4f, 4f), _coat);
            Block(p, "cuffStrap", V(0f, -9.1f, 0f), S(4.05f, 1.4f, 4.05f), _coatD);
            Block(p, "strapButton", V(0f, -9.1f, 2.05f), S(0.5f, 0.5f, 0.08f), _silver);
            Block(p, "hand", V(0f, -10.9f, 0f), S(4f, 2.2f, 4f), _skin);
        }
        else if (_def.outfit == "maid")
        {
            Block(p, "sleeve", V(0f, -4.2f, 0f), S(4f, 8.4f, 4f), _dress);
            Block(p, "cuff", V(0f, -9.1f, 0f), S(4.2f, 1.6f, 4.2f), _lace);
            Block(p, "hand", V(0f, -10.9f, 0f), S(4f, 2.2f, 4f), _skin);
        }
        else if (_def.outfit == "school")
        {
            Block(p, "sleeve", V(0f, -5f, 0f), S(4f, 10f, 4f), _shirt);
            Block(p, "cuff", V(0f, -9.6f, 0f), S(4.1f, 0.8f, 4.1f), _shirtD);
            Block(p, "hand", V(0f, -10.9f, 0f), S(4f, 2.2f, 4f), _skin);
        }
        else if (_def.outfit == "darkops")
        {
            Block(p, "sleeve", V(0f, -4.2f, 0f), S(4f, 8.4f, 4f), _coat);
            Block(p, "cuff", V(0f, -9.1f, 0f), S(4.1f, 1.2f, 4.1f), _silver);
            Block(p, "glove", V(0f, -10.9f, 0f), S(4f, 2.4f, 4f), _lace);
        }
    }

    void Leg(Transform p)
    {
        if (_def.outfit == "sailor")
        {
            Block(p, "pant", V(0f, -5f, 0f), S(4f, 10f, 4f), _pants);
            float sx = p.localPosition.x < 0f ? -2.0f : 2.0f;
            Block(p, "pantSeam", V(sx, -5f, 0f), S(0.1f, 10f, 3.8f), _coatD);
            Block(p, "crease", V(0f, -5f, 2.03f), S(0.12f, 9.6f, 0.06f), _coatD);
            Block(p, "shoe", V(0f, -11f, 0.3f), S(4.2f, 2f, 4.6f), _shoe);
            Block(p, "lace1", V(0f, -10.6f, 2.62f), S(1.6f, 0.12f, 0.06f), _silver);
            Block(p, "lace2", V(0f, -11.1f, 2.62f), S(1.6f, 0.12f, 0.06f), _silver);
            Block(p, "sole", V(0f, -11.75f, 0.3f), S(4.25f, 0.5f, 4.65f), _sole);
        }
        else if (_def.outfit == "dress")
        {
            Block(p, "tights", V(0f, -9.5f, 0f), S(4f, 5f, 4f), _dressD);
            Block(p, "shoe", V(0f, -11f, 0.3f), S(4.2f, 2f, 4.6f), _shoe);
            Block(p, "sole", V(0f, -11.75f, 0.3f), S(4.25f, 0.5f, 4.65f), _sole);
        }
        else if (_def.outfit == "jacket")
        {
            Block(p, "pant", V(0f, -5f, 0f), S(4f, 10f, 4f), _pants);
            Block(p, "shoe", V(0f, -11f, 0.3f), S(4.2f, 2f, 4.6f), _shoe);
            Block(p, "sole", V(0f, -11.75f, 0.3f), S(4.25f, 0.5f, 4.65f), _sole);
        }
        else if (_def.outfit == "maid")
        {
            Block(p, "leg", V(0f, -5f, 0f), S(3.4f, 8f, 3.4f), _skin);
            Block(p, "sock", V(0f, -9.5f, 0f), S(3.7f, 3f, 3.7f), _lace);
            Block(p, "shoe", V(0f, -11f, 0.3f), S(4f, 1.8f, 4.4f), _shoe);
            Block(p, "sole", V(0f, -11.75f, 0.3f), S(4.05f, 0.4f, 4.45f), _sole);
        }
        else if (_def.outfit == "school")
        {
            Block(p, "leg", V(0f, -5f, 0f), S(3.4f, 8f, 3.4f), _skin);
            Block(p, "shoe", V(0f, -11f, 0.3f), S(4f, 1.8f, 4.4f), _shoe);
            Block(p, "sole", V(0f, -11.75f, 0.3f), S(4.05f, 0.4f, 4.45f), _sole);
        }
        else if (_def.outfit == "darkops")
        {
            Block(p, "pant", V(0f, -5f, 0f), S(4f, 10f, 4f), _pants);
            Block(p, "boot", V(0f, -10.4f, 0.2f), S(4.3f, 3f, 4.8f), _shoe);
            Block(p, "sole", V(0f, -11.75f, 0.3f), S(4.35f, 0.5f, 4.85f), _sole);
        }
    }

    // ── 动画 ─────────────────────────────────────────────
    public void Tick(bool moving, bool running, float facing, float dt)
    {
        if (_model == null) return;
        transform.localRotation = Quaternion.Euler(0f, facing * Mathf.Rad2Deg, 0f);
        _blend = Mathf.MoveTowards(_blend, moving ? 1f : 0f, dt * 6f);
        _runBlend = Mathf.MoveTowards(_runBlend, moving && running ? 1f : 0f, dt * 5f);
        _phase += dt * (moving ? Mathf.Lerp(9.5f, 13.5f, _runBlend) : 2.0f);

        float sw = Mathf.Sin(_phase) * _blend;
        float idle = Mathf.Sin(Time.time * 1.6f) * (1f - _blend);
        float stride = 42f + 14f * _runBlend;
        float armSwing = 36f + 16f * _runBlend;
        _legL.localRotation = Quaternion.Euler(sw * stride, 0f, 0f);
        _legR.localRotation = Quaternion.Euler(-sw * stride, 0f, 0f);
        _armL.localRotation = Quaternion.Euler(-sw * armSwing + idle * 2.5f, 0f, 0f);
        _armR.localRotation = Quaternion.Euler(sw * armSwing - idle * 2.5f, 0f, 0f);

        float bob = Mathf.Abs(Mathf.Sin(_phase)) * (1f + 0.7f * _runBlend) * PX * 0.7f * _blend;
        float breath = Mathf.Sin(Time.time * 2.1f) * 0.15f * PX * (1f - _blend);
        _body.localPosition = new Vector3(0f, 12f * PX + bob + breath, 0f);
        _body.localRotation = Quaternion.Euler(7f * _runBlend, 0f, 0f);
        _head.localRotation = Quaternion.Euler(2.5f + idle * 1.5f - 2f * _runBlend, 0f, 0f);
    }

    public void SetVisible(bool v)
    {
        if (_model != null) _model.gameObject.SetActive(v);
    }

    public string Label => _def.label;
    public string Id => string.IsNullOrEmpty(_def.id) ? "fanlou" : _def.id;

    // 六人名单（C 键自选菜单与循环共用同一顺序）
    public static readonly CharacterDef[] Roster =
    {
        CharacterDef.FanLou, CharacterDef.JingYing, CharacterDef.JingNan,
        CharacterDef.LiLiYa, CharacterDef.YaoQingHan, CharacterDef.ZiWei,
    };

    // 运行时循环切换人物模型（-cycle 验收钩子），不重启进程
    public void Cycle()
    {
        int i = 0;
        for (int k = 0; k < Roster.Length; k++) if (Roster[k].id == _def.id) i = k;
        SetCharacter(Roster[(i + 1) % Roster.Length]);
    }

    // 自选菜单点选：直接换到指定角色
    public void SetCharacter(CharacterDef def)
    {
        if (def == null) return;
        if (_model != null) DestroyImmediate(_model.gameObject);
        _def = def;
        Build();
        Debug.Log("[Avatar] select → " + _def.label);
    }

    // ── 小工具 ───────────────────────────────────────────
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z) * PX;
    static Vector3 S(float x, float y, float z) => new Vector3(x, y, z) * PX;

    static Transform Node(Transform parent, string name, Vector3 pos)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        return t;
    }

    static Transform Block(Transform p, string name, Vector3 pos, Vector3 size, Material mat)
    {
        var go = new GameObject(name);
        go.layer = 2;
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.BoxUV(size, 0.25f);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go.transform;
    }

    static Transform BlockR(Transform p, string name, Vector3 pos, Vector3 size, Vector3 euler, Material mat)
    {
        var t = Block(p, name, pos, size, mat);
        t.localRotation = Quaternion.Euler(euler);
        return t;
    }

    // 锥形块：pivot 在下端，向上变宽/变窄；发盖/鬓角/后颈和领巾飘带共用
    static void Taper(Transform p, string name, Vector3 basePos, float bw, float bd, float tw, float td, float hPx, Vector3 euler, Material mat)
    {
        var go = new GameObject(name);
        go.layer = 2;
        go.transform.SetParent(p, false);
        go.transform.localPosition = basePos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.AddComponent<MeshFilter>().sharedMesh =
            MeshFactory.TaperedBox(bw * PX, bd * PX, tw * PX, td * PX, hPx * PX, 0.25f);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void HairVolume(Transform p, string name, Vector3 basePos, float bw, float bd, float tw, float td, float hPx, Vector3 euler, Material mat)
        => Taper(p, name, basePos, bw, bd, tw, td, hPx, euler, mat);


    // ── 人物材质工厂（docs/104）─────────────────────────────
    // 三个人物专属的暗面色，都比世界默认的冷紫（MatLib.ShadowTint 0.52,0.55,0.78）更暖更浅：
    //   skin  偏暖粉 —— 皮肤暗面绝不能发青发紫，否则脸像尸斑；
    //   cloth 中性微暖 —— 衣服暗面保留一点冷调做对比，但压不到脏；
    //   hair  偏冷浅 —— 白发/金发/黑发的暗面都要保住「亮阶」，纯压黑会让发丝纹理全糊。
    static readonly Color SkinShadow = new Color(0.86f, 0.72f, 0.68f, 1f);
    static readonly Color ClothShadow = new Color(0.74f, 0.68f, 0.76f, 1f);
    static readonly Color HairShadow = new Color(0.72f, 0.70f, 0.80f, 1f);

    // 描边分三档（屏幕空间恒定宽度，NDC 值；1600px 宽下 0.001 ≈ 0.8px）：
    //   粗 Bold 0.0060 → 约 4.8px：身体/头/发壳/四肢/裙摆这些「大块面」，方块人靠它立住轮廓；
    //   中 Mid  0.0040 → 约 3.2px：领子/腰带/围裙边/中号配饰；
    //   细 Fine 0.0018 → 约 1.4px：纽扣/勋章/校徽/领带/宝石。这些小件本身在屏幕上只有几个像素，
    //                              用粗描边会被描边整个吃掉、糊成黑点（方块人小配件多的通病）。
    const float OutlineBold = 0.0060f;
    const float OutlineMid = 0.0040f;
    const float OutlineFine = 0.0018f;

    // 人物描边颜色：比 MatLib.LitColorBold 默认的 (0.09,0.07,0.13) 稍浅、更偏靛蓝。
    // 近黑描边在方块人身上会把相邻部件的分件线糊成一片死黑（尤其躯干+手臂+腿的接缝），
    // 带一点色相的深靛蓝既能勾轮廓，又和暖色皮肤/衣服形成「冷边暖面」的卡通对比。
    static readonly Color CharOutlineColor = new Color(0.15f, 0.12f, 0.22f, 1f);

    // 皮肤/脸部自发光强度：卡通渲染的暗面是一整块色阶，人物一旦站在帽檐阴影 / 背光里，
    // 整张脸会塌成一块灰（docs/99 记的「紫薇帽檐在正脸投阴影」就是这个问题）。
    // 给皮肤一点点自发光，让脸在任何机位都保得住「亮阶」，同时不至于像发光贴纸。
    //
    // ⚠ 这个值本身不是肤色发白的根因。真正的原因是**脸片 albedo 的 R 通道本来就贴着 255**，
    // 受光后（实测有效系数 k≈1.07）R/G 双通道同时截断，暖调 R−G 差被抹平成 0。
    // 所以修法以「降 albedo 留余量」为主，自发光只做小幅配合（0.085→0.050→0.040）。
    // 复核判据：脸颊 avg 的 R−G 必须 ≥10，且 R 不应顶到 255。
    const float SkinGlow = 0.040f;

    static Material CharacterMat(Color c, Color shadow, float outline, float spec = 0f, float rim = 0.22f)
    {
        var m = MatLib.LitColorBold(c, outline);
        if (m.shader.name == "Campus/Toon")
        {
            m.SetColor("_ShadowTint", shadow);
            m.SetColor("_OutlineColor", CharOutlineColor);
            m.SetFloat("_SpecularStrength", spec);
            m.SetFloat("_RimStrength", rim);   // 边缘光：方块人靠它把轮廓从背景里拎出来
        }
        return m;
    }

    static Material SkinMat(Color c)
    {
        var m = CharacterMat(c, SkinShadow, OutlineBold);
        // 自发光用主色调制（shader 里是 albedo * _EmissionColor），所以皮肤只是「抬暗面」不变色
        if (m.shader.name == "Campus/Toon")
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(SkinGlow, SkinGlow, SkinGlow));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        return m;
    }

    static Material ClothMat(Color c) => CharacterMat(c, ClothShadow, OutlineBold);

    static Material ClothMid(Color c) => CharacterMat(c, ClothShadow, OutlineMid);

    static Material ClothFine(Color c) => CharacterMat(c, ClothShadow, OutlineFine, rim: 0.10f);

    // 发壳：发丝纹理 + 描边。tint=white 是主发色，tint=hairTintD 是收边深发。
    // 描边宽度按「这块发片有多厚」分档：厚壳（发盖/发冠）用粗，薄片（侧/后发壳）用细。
    static Material HairMat(Texture2D tex, Color tint, float outline)
    {
        var m = CharacterMat(Color.white, HairShadow, outline);
        m.mainTexture = tex;
        m.mainTextureScale = Vector2.one;
        m.color = tint;
        return m;
    }

    // 脸片材质：必须 _OutlineWidth = 0。
    // 反向外扩描边是把顶点沿法线在屏幕上推开，脸片是一块 8×8 的薄盒子，四面法线全部朝外扩，
    // 结果就是在脸周围糊出一圈黑框（基线图 shots/210-baseline-* 里脸四周的黑色矩形就是这么来的）。
    // 脸的轮廓交给「头块自身的描边」去勾，脸片只负责画五官。
    static Material FaceMat(Texture2D tex)
    {
        var m = MatLib.LitTexture(tex, 1f, 1f, 0f);
        if (m.shader.name == "Campus/Toon")
        {
            m.SetFloat("_OutlineWidth", 0f);
            m.SetFloat("_SpecularStrength", 0f);
            m.SetFloat("_RimStrength", 0f);          // 脸片不需要边缘光（会沿 UV 边缘提亮一条）
            m.SetColor("_ShadowTint", SkinShadow);
            // 脸比身体更需要「保亮」：帽檐阴影 / 背光时整张脸是一整块暗阶，
            // 卡通渲染下会变成一坨灰，五官直接看不清。自发光略高于皮肤本体。
            // 0.03：只负责「抬最暗一阶」，不再参与推高亮面（亮面发白是 albedo 顶格导致的，见 SkinGlow 注释）
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(0.030f, 0.028f, 0.026f));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        return m;
    }


    // 64×64 程序化发丝纹理：金色细竖纹，平铺后不会出现竹篮格
    static Texture2D HairTexture(CharacterDef def)
    {
        const int S = 64;
        var t = new Texture2D(S, S, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        var px = new Color32[S * S];
        Color b = def.hair, d = def.hairD, l = def.hairL;
        // 16 格一组的宽窄发丝，64 格正好平铺；不加横向纹理
        Color[] cols = new Color[S];
        for (int x = 0; x < S; x++)
        {
            int m = x % 16;
            if (m == 0) cols[x] = d;
            else if (m == 4) cols[x] = l;
            else if (m == 7) cols[x] = Color.Lerp(b, l, 0.35f);
            else if (m == 9) cols[x] = Color.Lerp(b, l, 0.45f);
            else if (m == 13) cols[x] = Color.Lerp(b, d, 0.25f);
            else cols[x] = b;
        }
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
                px[y * S + x] = cols[x];
        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    // 正面脸贴片：UV 1:1
    static void FacePlate(Transform p, Vector3 pos, Vector3 size, Material mat)
    {
        var go = new GameObject("face");
        go.layer = 2;
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.BoxUV(size, size.x, size.y);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // 64×64 男主脸：金色短发、蓝眼、深眉、小嘴（8×8 格子放大 + 64×64 细节点缀）
    static Texture2D FaceTexture(CharacterDef d)
    {
        const int S = 64;
        var t = new Texture2D(S, S, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[S * S];
        Color skin = d.skinFace, skinD = d.skinFaceD;
        Color hair = d.hair, hairL = d.hairL, hairD = d.hairD;
        Color dark = d.brow;
        Color eyeW = d.eyeW;
        Color iris = d.iris, irisL = d.irisL, irisHi = d.irisHi;
        Color pupil = d.pupil;
        Color mouth = d.mouth;

        void R(int x, int y, int w, int h, Color c)
        {
            var c32 = (Color32)c;
            for (int yy = 0; yy < h; yy++)
                for (int xx = 0; xx < w; xx++)
                {
                    int ax = x + xx, ay = y + yy;
                    if (ax < 0 || ay < 0 || ax >= S || ay >= S) continue;
                    px[ay * S + ax] = c32;
                }
        }

        // 顶部两排头发 + 金色发丝
        R(0, 48, S, 16, hair);
        for (int x = 0; x < S; x += 8)
        {
            R(x, 48, 2, 16, hairD);
            R(x + 3, 48, 1, 16, hairL);
        }
        R(24, 48, 8, 8, hairL);

        // 刘海行：两侧发、中间露额；发际线下压 1px 阴影
        R(0, 40, 16, 8, hair);
        R(16, 40, 24, 8, skin);
        R(40, 40, 24, 8, hair);
        R(56, 40, 8, 8, hairD);
        for (int x = 0; x < 16; x += 8) R(x, 40, 2, 8, hairD);
        for (int x = 40; x < 56; x += 8) R(x, 40, 2, 8, hairD);
        R(16, 39, 24, 1, skinD);

        // 侧扫刘海（瑶清寒）：发盖住额头，斜分留一道额缝
        if (d.bangs)
        {
            R(16, 40, 24, 8, hair);
            R(30, 40, 3, 8, skin);
            for (int x = 16; x < 40; x += 6) R(x, 40, 1, 8, hairD);
            R(16, 39, 24, 1, skinD);
        }

        // 眉行：先铺脸，再压深色眉；外圈留 1px 肤色
        R(8, 32, 48, 8, skin);
        R(0, 32, 8, 8, hair);
        R(56, 32, 8, 8, hair);
        if (d.female)
        {
            // 女眼（对齐镜荧·女主）：不画眉（童稚）、大虹膜 + 4×4 瞳孔 + 单点高光，占格同男主、位置 y24~40
            // 不加粗睫毛线（作者「脸上有黑边」：齐刘海下 16px 深色睫毛带会糊成黑条）
            R(0, 16, 8, 24, hair);
            R(56, 16, 8, 24, hair);
            R(8, 16, 48, 24, skin);
            foreach (int ex in new[] { 8, 40 })
            {
                R(ex, 28, 3, 8, eyeW);
                R(ex + 13, 28, 3, 8, eyeW);
                R(ex + 3, 28, 10, 8, iris);
                R(ex + 6, 30, 4, 4, pupil);
                R(ex + 4, 32, 3, 3, irisHi);
                R(ex, 24, 3, 4, eyeW);
                R(ex + 13, 24, 3, 4, eyeW);
                R(ex + 3, 24, 10, 4, irisL);
            }
        }
        else
        {
            R(9, 33, 14, 5, dark);
            R(41, 33, 14, 5, dark);
            if (d.male) { R(8, 32, 16, 5, dark); R(40, 32, 16, 5, dark); } // 男性更粗更低平直眉

            // 眼睛上行：眼白 + 深蓝虹膜 + 瞳孔 + 左上高光
            R(0, 24, 8, 8, hair);
            R(8, 24, 8, 8, eyeW);
            R(16, 24, 8, 8, iris);
            R(24, 24, 16, 8, skin);
            R(40, 24, 8, 8, iris);
            R(48, 24, 8, 8, eyeW);
            R(56, 24, 8, 8, hair);
            R(19, 25, 2, 2, pupil);
            R(43, 25, 2, 2, pupil);
            R(17, 27, 2, 2, irisHi);
            R(41, 27, 2, 2, irisHi);

            // 眼睛下行：亮蓝下虹膜 + 眼白
            R(0, 16, 8, 8, hair);
            R(8, 16, 8, 8, eyeW);
            R(16, 16, 8, 8, irisL);
            R(24, 16, 16, 8, skin);
            R(40, 16, 8, 8, irisL);
            R(48, 16, 8, 8, eyeW);
            R(56, 16, 8, 8, hair);
        }

        // 嘴行：鼻影 + 6px 小嘴 + 下唇淡影
        R(0, 8, S, 8, skin);
        R(31, 13, 2, 1, skinD);
        R(29, 10, 6, 1, mouth);
        R(28, 9, 8, 1, skinD);

        // 下巴
        R(0, 0, S, 8, skin);

        // 男性（紫薇）：下巴胡茬 + 上唇须 + 右颊疤
        // 卡通渲染下胡茬用 Lerp(skin,hairD,0.5) 会压成一块脏灰，这里改成「暖褐 + 低对比」（0.26），
        // 只做「这个人有胡茬」的色块暗示，不抢五官。
        if (d.male)
        {
            Color stub = Color.Lerp(skin, MatLib.Hex("#8a7058"), 0.26f);
            R(18, 0, 28, 6, stub);
            R(26, 12, 12, 1, stub);
            R(50, 18, 2, 6, Color.Lerp(skin, mouth, 0.55f));
        }

        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    // 64×64 镜荧脸：中分八字刘海、红眼（占格同他人、瞳孔/高光像素大一档）、童稚、小嘴 + 两颗小虎牙
    static Texture2D FaceTextureVampire(CharacterDef d)
    {
        const int S = 64;
        var t = new Texture2D(S, S, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[S * S];
        Color skin = d.skinFace, skinD = d.skinFaceD;
        Color hair = d.hair, hairL = d.hairL, hairD = d.hairD;
        Color eyeW = d.eyeW, iris = d.iris, irisL = d.irisL, pupil = d.pupil;
        Color mouth = d.mouth, fang = Color.white;

        void R(int x, int y, int w, int h, Color c)
        {
            var c32 = (Color32)c;
            for (int yy = 0; yy < h; yy++)
                for (int xx = 0; xx < w; xx++)
                {
                    int ax = x + xx, ay = y + yy;
                    if (ax < 0 || ay < 0 || ax >= S || ay >= S) continue;
                    px[ay * S + ax] = c32;
                }
        }

        // 顶排发丝 + 中分八字刘海（额头收窄：row1 缝 8px / row2 缝 16px，作者「额头露太多」）
        R(0, 56, S, 8, hair);
        for (int x = 0; x < S; x += 8)
        {
            R(x, 56, 2, 8, hairD);
            R(x + 3, 56, 1, 8, hairL);
        }
        R(0, 48, 28, 8, hair);
        R(36, 48, 28, 8, hair);
        R(28, 48, 8, 8, skin);
        R(24, 48, 4, 8, hairD);
        R(36, 48, 4, 8, hairD);
        R(0, 40, 24, 8, hair);
        R(40, 40, 24, 8, hair);
        R(24, 40, 16, 8, skin);
        R(16, 40, 8, 8, hairD);
        R(40, 40, 8, 8, hairD);

        // 脸：两侧鬓发 + 中间肤色；不画眉（童稚感）
        R(0, 8, 8, 32, hair);
        R(56, 8, 8, 32, hair);
        R(8, 8, 48, 32, skin);

        // v6 参考重做眼：占格同他人 2×2（y24~39 对齐网格）、深maroon上睫毛线 + 整片红虹膜 + 中央暗瞳
        // + 左上单点白高光（两眼同光源）+ 细眼白角 + 浅红下睑；参考 shots/97-refs/ref1/ref2
        Color lash = MatLib.Hex("#5a2a30");
        foreach (int ex in new[] { 8, 40 })
        {
            R(ex, 36, 16, 4, lash);
            R(ex, 28, 3, 8, eyeW);
            R(ex + 13, 28, 3, 8, eyeW);
            R(ex + 3, 28, 10, 8, iris);
            R(ex + 6, 30, 4, 4, pupil);
            R(ex + 4, 32, 3, 3, Color.white);
            R(ex, 24, 3, 4, eyeW);
            R(ex + 13, 24, 3, 4, eyeW);
            R(ex + 3, 24, 10, 4, irisL);
        }

        // 鼻影（缩小）+ 小嘴 + 嘴角两颗小虎牙
        R(30, 18, 4, 2, skinD);
        R(29, 10, 6, 2, mouth);
        R(27, 8, 2, 2, fang);
        R(35, 8, 2, 2, fang);

        // 下巴
        R(0, 0, S, 8, skin);

        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    static void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform c in go.transform) SetLayer(c.gameObject, layer);
    }
}
