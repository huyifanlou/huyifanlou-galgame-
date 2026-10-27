using UnityEngine;

// 《我的世界》风格男主方块人：樊楼。短金发、蓝眼、藏青西装校服。
// 作者给的参考皮肤只借五官精度；金发长裙是觉醒后的女体形态，不是默认男主。
// 只做视觉与动画，不挂碰撞；碰撞仍由 Player 上的 CapsuleCollider 负责。
public class PlayerAvatar : MonoBehaviour
{
    // 1 像素 = 1.8m / 32px
    const float PX = 0.05625f;

    Transform _model, _body, _head, _legL, _legR, _armL, _armR;
    Material _skin, _hair, _hairD, _earInner, _coat, _coatD, _shirt;
    Material _tie, _tieD, _pants, _shoe, _sole, _belt, _silver, _badgeBlue;
    float _phase, _blend;

    public static PlayerAvatar Create(Transform parent)
    {
        var go = new GameObject("Avatar");
        go.transform.SetParent(parent, false);
        go.layer = 2;
        var av = go.AddComponent<PlayerAvatar>();
        av.Build();
        return av;
    }

    void Build()
    {
        _skin = MatLib.LitColor(MatLib.Hex("#ffe9dd"));
        var hairTex = HairTexture();
        _hair = HairMat(hairTex, Color.white, 0.10f);
        _hairD = HairMat(hairTex, new Color(0.80f, 0.75f, 0.68f), 0.06f);
        _earInner = MatLib.LitColor(MatLib.Hex("#c8b09c"));
        _coat = MatLib.LitColor(MatLib.Hex("#46567f"));
        _coatD = MatLib.LitColor(MatLib.Hex("#333f5e"));
        _shirt = MatLib.LitColor(MatLib.Hex("#f4f1e6"));
        _tie = MatLib.LitColor(MatLib.Hex("#b23a44"));
        _tieD = MatLib.LitColor(MatLib.Hex("#8a2a34"));
        _pants = MatLib.LitColor(MatLib.Hex("#2e3550"));
        _shoe = MatLib.LitColor(MatLib.Hex("#23262e"));
        _sole = MatLib.LitColor(MatLib.Hex("#171920"));
        _belt = MatLib.LitColor(MatLib.Hex("#2a2d36"));
        _silver = MatLib.LitColor(MatLib.Hex("#b8bcc4"));
        _badgeBlue = MatLib.LitColor(MatLib.Hex("#3f5f9a"));

        // 髋部枢轴：腿 12px、身 12px、头 8px，总高 32px = 1.8m
        _model = Node(transform, "model", Vector3.zero);
        _body = Node(_model, "body", V(0f, 12f, 0f));

        // 身体 8×12×4：藏青西装 + 白衬衫 + 红领带
        Block(_body, "jacket", V(0f, 6f, 0f), S(8f, 12f, 4f), _coat);
        Block(_body, "shirt", V(0f, 6.4f, 2.10f), S(3.2f, 7.2f, 0.14f), _shirt);
        Block(_body, "collarNavy", V(0f, 10.9f, 2.10f), S(4.0f, 0.6f, 0.16f), _coatD);
        Block(_body, "collarWhiteL", V(-0.85f, 10.25f, 2.22f), S(1.1f, 0.8f, 0.14f), _shirt);
        Block(_body, "collarWhiteR", V(0.85f, 10.25f, 2.22f), S(1.1f, 0.8f, 0.14f), _shirt);
        BlockR(_body, "lapelL", V(-1.75f, 9.45f, 2.16f), S(1.2f, 3.0f, 0.14f), new Vector3(0f, 0f, 16f), _coatD);
        BlockR(_body, "lapelR", V(1.75f, 9.45f, 2.16f), S(1.2f, 3.0f, 0.14f), new Vector3(0f, 0f, -16f), _coatD);

        // 红领带 + 领带夹
        Block(_body, "tieKnot", V(0f, 9.8f, 2.34f), S(1.0f, 0.8f, 0.16f), _tieD);
        Block(_body, "tie", V(0f, 7.8f, 2.28f), S(0.9f, 3.4f, 0.16f), _tie);
        Block(_body, "tieTip", V(0f, 5.75f, 2.28f), S(0.5f, 0.7f, 0.16f), _tieD);
        Block(_body, "tieClip", V(0f, 7.2f, 2.30f), S(1.1f, 0.25f, 0.14f), _silver);

        // 腰带、银扣、校徽、西装扣
        Block(_body, "belt", V(0f, 1.4f, 2.18f), S(7.9f, 0.9f, 0.18f), _belt);
        Block(_body, "buckle", V(0f, 1.4f, 2.30f), S(0.9f, 0.9f, 0.16f), _silver);
        Block(_body, "badge", V(-2.8f, 8.3f, 2.16f), S(0.7f, 0.9f, 0.14f), _silver);
        Block(_body, "badgeCore", V(-2.8f, 8.3f, 2.24f), S(0.3f, 0.4f, 0.10f), _badgeBlue);
        Block(_body, "button1", V(2.75f, 6.6f, 2.16f), S(0.4f, 0.4f, 0.14f), _silver);
        Block(_body, "button2", V(2.75f, 5.1f, 2.16f), S(0.4f, 0.4f, 0.14f), _silver);
        Block(_body, "button3", V(2.75f, 3.6f, 2.16f), S(0.4f, 0.4f, 0.14f), _silver);

        // 下摆、后中缝、后开衩
        Block(_body, "hem", V(0f, 0.3f, 0f), S(8.05f, 0.6f, 4.05f), _coatD);
        Block(_body, "backSeam", V(0f, 6.2f, -2.10f), S(0.18f, 7.2f, 0.12f), _coatD);
        Block(_body, "backVent", V(0f, 0.9f, -2.08f), S(2.4f, 1.2f, 0.10f), _coatD);

        // 头：8×8×8
        _head = Node(_body, "head", V(0f, 16f, 0f));
        Block(_head, "head", Vector3.zero, S(8f, 8f, 8f), _skin);
        var faceTex = FaceTexture();
        if (Args.Has("-dumpface"))
        {
            var dumpPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "face-debug.png");
            System.IO.File.WriteAllBytes(dumpPath, faceTex.EncodeToPNG());
            Debug.Log("[Avatar] face dumped: " + dumpPath);
        }
        var faceMat = new Material(Shader.Find("Unlit/Texture"));
        faceMat.mainTexture = faceTex;
        FacePlate(_head, V(0f, 0f, 4.03f), S(8f, 8f, 0.10f), faceMat);

        // 短金发：锥形发盖 + 错层头顶 + 侧扫刘海 + 收窄鬓角/后颈
        HairVolume(_head, "hairCap", V(0f, 3.4f, -0.05f), 8.3f, 8.3f, 6.8f, 6.8f, 1.5f, new Vector3(-3f, 0f, 0f), _hair);
        BlockR(_head, "hairCrown", V(-0.2f, 4.9f, -0.7f), S(5.2f, 0.4f, 5.0f), new Vector3(0f, 0f, -4f), _hair);

        // 猫耳（奶油色）+ 深奶油内耳
        HairVolume(_head, "earL", V(-2.85f, 4.4f, 0.5f), 2.8f, 1.7f, 0.4f, 0.5f, 3.0f, new Vector3(0f, 0f, 14f), _hair);
        HairVolume(_head, "earR", V(2.85f, 4.4f, 0.5f), 2.8f, 1.7f, 0.4f, 0.5f, 3.0f, new Vector3(0f, 0f, -14f), _hair);
        HairVolume(_head, "earInnerL", V(-2.85f, 4.75f, 1.36f), 1.0f, 0.10f, 0.18f, 0.04f, 1.5f, new Vector3(0f, 0f, 14f), _earInner);
        HairVolume(_head, "earInnerR", V(2.85f, 4.75f, 1.36f), 1.0f, 0.10f, 0.18f, 0.04f, 1.5f, new Vector3(0f, 0f, -14f), _earInner);

        // 刘海直接由正面贴图的顶部发排表现，不再叠 3D 楔块
        HairVolume(_head, "sideHairL", V(-4.15f, -0.9f, -0.5f), 0.6f, 5.2f, 0.9f, 5.8f, 4.8f, Vector3.zero, _hair);
        HairVolume(_head, "sideHairR", V(4.15f, -0.9f, -0.5f), 0.6f, 5.2f, 0.9f, 5.8f, 4.8f, Vector3.zero, _hair);
        HairVolume(_head, "sideburnL", V(-4.1f, -3.7f, 1.8f), 0.35f, 1.0f, 0.8f, 1.1f, 2.7f, Vector3.zero, _hairD);
        HairVolume(_head, "sideburnR", V(4.1f, -3.7f, 1.8f), 0.35f, 1.0f, 0.8f, 1.1f, 2.7f, Vector3.zero, _hairD);

        Block(_head, "backHair", V(0f, 0.85f, -4.25f), S(7.8f, 7.1f, 1.0f), _hairD);
        // 后脑错落发缕：打破平板的木箱感
        HairVolume(_head, "backStrandL", V(-2.55f, -1.2f, -4.74f), 1.7f, 0.55f, 1.0f, 0.40f, 3.7f, Vector3.zero, _hair);
        HairVolume(_head, "backStrandR", V(2.55f, -1.6f, -4.74f), 1.7f, 0.55f, 1.0f, 0.40f, 3.3f, Vector3.zero, _hair);
        HairVolume(_head, "backStrandM", V(0f, -2.3f, -4.80f), 1.4f, 0.50f, 0.9f, 0.38f, 2.9f, Vector3.zero, _hairD);
        HairVolume(_head, "backLower", V(0f, -5.4f, -4.1f), 4.8f, 0.9f, 6.4f, 0.95f, 2.8f, Vector3.zero, _hairD);
        Block(_head, "napeCap", V(0f, -5.45f, -4.09f), S(4.8f, 0.2f, 0.88f), _hairD);

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
        Block(p, "sleeve", V(0f, -4.5f, 0f), S(4f, 9f, 4f), _coat);
        Block(p, "cuff", V(0f, -8.8f, 0f), S(4.1f, 0.8f, 4.1f), _shirt);
        Block(p, "hand", V(0f, -10.6f, 0f), S(4f, 2.8f, 4f), _skin);
    }

    void Leg(Transform p)
    {
        Block(p, "pant", V(0f, -5f, 0f), S(4f, 10f, 4f), _pants);
        Block(p, "shoe", V(0f, -11f, 0.3f), S(4.2f, 2f, 4.6f), _shoe);
        Block(p, "sole", V(0f, -11.75f, 0.3f), S(4.25f, 0.5f, 4.65f), _sole);
    }

    // ── 动画 ─────────────────────────────────────────────
    public void Tick(bool moving, float facing, float dt)
    {
        if (_model == null) return;
        transform.localRotation = Quaternion.Euler(0f, facing * Mathf.Rad2Deg, 0f);
        _phase += dt * (moving ? 9.5f : 2.0f);
        _blend = Mathf.MoveTowards(_blend, moving ? 1f : 0f, dt * 6f);

        float sw = Mathf.Sin(_phase) * _blend;
        float idle = Mathf.Sin(Time.time * 1.6f) * (1f - _blend);
        _legL.localRotation = Quaternion.Euler(sw * 42f, 0f, 0f);
        _legR.localRotation = Quaternion.Euler(-sw * 42f, 0f, 0f);
        _armL.localRotation = Quaternion.Euler(-sw * 36f + idle * 2.5f, 0f, 0f);
        _armR.localRotation = Quaternion.Euler(sw * 36f - idle * 2.5f, 0f, 0f);

        float bob = Mathf.Abs(Mathf.Sin(_phase)) * 1.0f * PX * 0.7f * _blend;
        float breath = Mathf.Sin(Time.time * 2.1f) * 0.15f * PX * (1f - _blend);
        _body.localPosition = new Vector3(0f, 12f * PX + bob + breath, 0f);
        _head.localRotation = Quaternion.Euler(2.5f + idle * 1.5f, 0f, 0f);
    }

    public void SetVisible(bool v)
    {
        if (_model != null) _model.gameObject.SetActive(v);
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

    // 锥形发块：pivot 在下端，向上变宽/变窄，用来做有坡度的发盖、鬓角、后颈
    static void HairVolume(Transform p, string name, Vector3 basePos, float bw, float bd, float tw, float td, float hPx, Vector3 euler, Material mat)
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

    static Material HairMat(Texture2D tex, Color tint, float glow)
    {
        var m = MatLib.LitTexture(tex, 1f, 1f, 0f);
        m.color = tint;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", new Color(0.88f, 0.82f, 0.72f) * glow);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        return m;
    }

    // 64×64 程序化发丝纹理：细竖纹，平铺后不会出现竹篮格
    static Texture2D HairTexture()
    {
        const int S = 64;
        var t = new Texture2D(S, S, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        var px = new Color32[S * S];
        Color b = MatLib.Hex("#d5bda4"), d = MatLib.Hex("#bda58d"), l = MatLib.Hex("#e9d5ba");
        // 16 格一组的宽窄发丝，64 格正好平铺；不加横向纹理
        Color[] cols = new Color[S];
        for (int x = 0; x < S; x++)
        {
            int m = x % 16;
            if (m == 0 || m == 1) cols[x] = d;
            else if (m == 4) cols[x] = l;
            else if (m == 9) cols[x] = Color.Lerp(b, l, 0.45f);
            else if (m == 13) cols[x] = Color.Lerp(b, d, 0.35f);
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

    // 64×64 参考皮肤脸：奶油猫耳短发、蓝眼、深色眉/睫块（8×8 皮肤网格放大 8 倍）
    static Texture2D FaceTexture()
    {
        const int S = 64, C = 8;
        var t = new Texture2D(S, S, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[S * S];
        Color skin = MatLib.Hex("#ffe9dd");
        Color hair = MatLib.Hex("#d5bda4"), hairL = MatLib.Hex("#e9d5ba"), hairD = MatLib.Hex("#bda58d");
        Color dark = MatLib.Hex("#50483f");
        Color eyeW = MatLib.Hex("#f4f0ea");
        Color iris = MatLib.Hex("#2a5f96"), irisL = MatLib.Hex("#4f9ac2");

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

        // 参考皮肤 8×8 正面，逐格放大到 64×64；绿色瞳孔改为蓝色
        R(0, 56, 2 * C, C, hair); R(2 * C, 56, 4 * C, C, hairL); R(6 * C, 56, 2 * C, C, hair);
        R(0, 48, 2 * C, C, hair); R(2 * C, 48, 4 * C, C, hairL); R(6 * C, 48, 2 * C, C, hair);
        R(0, 40, 2 * C, C, hair); R(2 * C, 40, 3 * C, C, hairL); R(5 * C, 40, 2 * C, C, hair); R(7 * C, 40, C, C, hairD);
        R(0, 32, C, C, hairD); R(C, 32, 3 * C, C, hair); R(4 * C, 32, 2 * C, C, skin); R(6 * C, 32, C, C, hair); R(7 * C, 32, C, C, hairD);
        R(0, 24, C, C, hairD); R(C, 24, 2 * C, C, dark); R(3 * C, 24, 2 * C, C, skin); R(5 * C, 24, 2 * C, C, dark); R(7 * C, 24, C, C, hairD);
        R(0, 16, C, C, dark); R(C, 16, C, C, eyeW); R(2 * C, 16, C, C, iris); R(3 * C, 16, 2 * C, C, skin); R(5 * C, 16, C, C, iris); R(6 * C, 16, C, C, eyeW); R(7 * C, 16, C, C, dark);
        R(0, 8, C, C, hair); R(C, 8, C, C, eyeW); R(2 * C, 8, C, C, irisL); R(3 * C, 8, 2 * C, C, skin); R(5 * C, 8, C, C, irisL); R(6 * C, 8, C, C, eyeW); R(7 * C, 8, C, C, hair);
        R(0, 0, C, C, hair); R(C, 0, 6 * C, C, skin); R(7 * C, 0, C, C, hairD);

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
