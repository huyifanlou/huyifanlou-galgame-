using UnityEngine;

// 《我的世界》风格男主方块人：樊楼。短金发、蓝眼、藏青西装校服。
// 作者给的参考皮肤只借五官精度；金发长裙是觉醒后的女体形态，不是默认男主。
// 只做视觉与动画，不挂碰撞；碰撞仍由 Player 上的 CapsuleCollider 负责。
public class PlayerAvatar : MonoBehaviour
{
    // 1 像素 = 1.8m / 32px
    const float PX = 0.05625f;

    Transform _model, _body, _head, _legL, _legR, _armL, _armR;
    Material _skin, _hair, _hairD, _coat, _coatD, _shirt;
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
        _skin = MatLib.LitColor(MatLib.Hex("#f8dfb9"));
        var hairTex = HairTexture();
        _hair = HairMat(hairTex, Color.white, 0.14f);
        _hairD = HairMat(hairTex, new Color(0.78f, 0.72f, 0.55f), 0.08f);
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
        var faceMat = MatLib.LitTexture(faceTex, 1f, 1f, 0f);
        faceMat.EnableKeyword("_EMISSION");
        faceMat.SetColor("_EmissionColor", new Color(0.15f, 0.14f, 0.13f));
        faceMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        FacePlate(_head, V(0f, 0f, 4.03f), S(8f, 8f, 0.10f), faceMat);

        // 短金发：锥形发盖 + 错层头顶 + 侧扫刘海 + 收窄鬓角/后颈
        HairVolume(_head, "hairCap", V(0f, 3.4f, -0.05f), 8.3f, 8.3f, 6.8f, 6.8f, 1.5f, new Vector3(-3f, 0f, 0f), _hair);
        BlockR(_head, "hairCrown", V(-0.3f, 4.9f, -0.7f), S(5.8f, 0.5f, 5.4f), new Vector3(0f, 0f, -4f), _hair);
        HairVolume(_head, "crownTuft", V(-0.8f, 4.7f, -1.0f), 0.6f, 0.7f, 0.15f, 0.35f, 0.8f, new Vector3(18f, 0f, -8f), _hairD);

        Block(_head, "fringeLip", V(0f, 3.55f, 3.70f), S(7.8f, 0.2f, 0.8f), _hair);
        // 两把斜扫锥形刘海：根部在侧分线，尖端扫向两鬓
        HairVolume(_head, "fringeLeft", V(0.8f, 3.55f, 3.90f), 1.2f, 0.5f, 0.2f, 0.35f, 4.5f, new Vector3(0f, 0f, 105f), _hair);
        HairVolume(_head, "fringeRight", V(2.0f, 3.55f, 3.90f), 1.0f, 0.5f, 0.2f, 0.35f, 2.0f, new Vector3(0f, 0f, -100f), _hairD);
        HairVolume(_head, "sideHairL", V(-4.15f, -0.9f, -0.5f), 0.6f, 5.2f, 0.9f, 5.8f, 4.8f, Vector3.zero, _hair);
        HairVolume(_head, "sideHairR", V(4.15f, -0.9f, -0.5f), 0.6f, 5.2f, 0.9f, 5.8f, 4.8f, Vector3.zero, _hair);
        HairVolume(_head, "sideburnL", V(-4.1f, -3.7f, 1.8f), 0.35f, 1.0f, 0.8f, 1.1f, 2.7f, Vector3.zero, _hairD);
        HairVolume(_head, "sideburnR", V(4.1f, -3.7f, 1.8f), 0.35f, 1.0f, 0.8f, 1.1f, 2.7f, Vector3.zero, _hairD);

        Block(_head, "backHair", V(0f, 0.85f, -4.25f), S(7.8f, 7.1f, 1.0f), _hair);
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
        m.SetColor("_EmissionColor", new Color(0.93f, 0.80f, 0.35f) * glow);
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
        Color b = MatLib.Hex("#eccb5a"), d = MatLib.Hex("#c9a234"), l = MatLib.Hex("#f7e28a");
        // 8 格一组的宽窄发丝，32 格正好平铺；不加横向纹理
        Color[] cols = new Color[S];
        for (int x = 0; x < S; x++)
        {
            int m = x % 8;
            if (m == 0 || m == 1) cols[x] = d;
            else if (m == 3) cols[x] = l;
            else if (m == 6) cols[x] = Color.Lerp(b, l, 0.35f);
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

    // 64×64 男版忧郁脸：短金发、低垂蓝眼、内高外低的眉、下弯嘴角
    static Texture2D FaceTexture()
    {
        const int S = 64;
        var t = new Texture2D(S, S, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[S * S];
        Color skin = MatLib.Hex("#f4dcc0"), skinS = MatLib.Hex("#e7c8a7");
        Color skinD = MatLib.Hex("#d6b28e"), skinL = MatLib.Hex("#fbe9d1");
        Color hair = MatLib.Hex("#eccb5a"), hairD = MatLib.Hex("#c9a234"), hairL = MatLib.Hex("#f7e28a");
        Color brow = MatLib.Hex("#6f5128"), lash = MatLib.Hex("#3f322a");
        Color eyeW = MatLib.Hex("#eef3f6"), iris = MatLib.Hex("#2a5f96");
        Color irisD = MatLib.Hex("#173e66"), irisL = MatLib.Hex("#3f86b4");
        Color pupil = MatLib.Hex("#0a1622");
        Color lip = MatLib.Hex("#b0786d"), lipD = MatLib.Hex("#97625a");

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
        void P(int x, int y, Color c) => R(x, y, 1, 1, c);

        R(0, 0, S, S, skin);
        // 侧光/下颌：大块平涂，不做细渐变
        R(0, 0, 4, S, skinD); R(S - 4, 0, 4, S, skinD);
        R(4, 0, 3, S, skinS); R(S - 7, 0, 3, S, skinS);
        R(0, 0, S, 2, skinS);
        R(7, 4, 5, 18, skinS); R(S - 12, 4, 5, 18, skinS);
        R(26, 2, 12, 2, skinS);
        P(31, 2, skinD); P(33, 2, skinD);

        // 短金发：高发际线 + 侧分，短碎发
        for (int x = 0; x < S; x++)
        {
            int yb = x < 32 ? 52 - x / 14 : 50 + (x - 32) / 18;
            R(x, yb, 1, S - yb, hair);
            P(x, yb, hairD);
            P(x, yb - 1, skinS);
            if (x % 11 == 3) P(x, yb + 4, hairL);
            if (x % 9 == 0) P(x, yb, hairD);
        }
        // 短刘海/鬓角
        void Lock(int x0, int w, int tip)
        {
            int mid = w / 2;
            for (int i = 0; i < w; i++)
            {
                int x = x0 + i;
                int y = tip + Mathf.Abs(i - mid) * 2;
                R(x, y, 1, S - y, hair);
                P(x, y, hairD);
                P(x, y - 1, skinS);
                if (i == mid) P(x, y + 6, hairL);
            }
        }
        Lock(4, 9, 48);
        Lock(17, 8, 50);
        Lock(36, 8, 49);
        Lock(51, 9, 48);
        R(0, 34, 2, 34, hair); R(S - 2, 34, 2, 34, hair);
        P(1, 34, hairD); P(S - 2, 34, hairD);

        // 忧郁眉：内端高、外端低
        for (int i = 0; i < 17; i++)
        {
            int by = 38 + (i * 4) / 16;
            R(10 + i, by, 1, 2, brow);
            R(53 - i, by, 1, 2, brow);
        }
        // 眉间淡纹
        R(31, 43, 1, 3, skinS); R(32, 43, 1, 3, skinS);

        void Eye(int x0, bool right)
        {
            // 眼白/眼睑：外眼角低、内眼角高
            for (int i = 0; i < 14; i++)
            {
                int x = x0 + i;
                int top = right ? 35 - (i * 2) / 13 : 33 + (i * 2) / 13;
                R(x, 26, 1, top - 26, eyeW);
                R(x, top, 1, 1, lash);
                P(x, top + 2, skinS);
                P(x, 25, skinS);
            }
            int outer = right ? x0 + 13 : x0;
            R(outer, 31, 1, 3, lash);

            // 低垂虹膜：下缘贴眼睑，上缘被眼睑压住
            R(x0 + 3, 26, 8, 1, eyeW);
            R(x0 + 3, 27, 8, 6, iris);
            R(x0 + 3, 27, 8, 1, irisL);
            R(x0 + 3, 31, 8, 2, irisD);
            P(x0 + 3, 29, irisD); P(x0 + 10, 29, irisD);
            P(x0 + 3, 30, irisD); P(x0 + 10, 30, irisD);
            R(x0 + 6, 28, 3, 4, pupil);
            R(x0 + 4, 30, 2, 2, Color.white);
            P(x0 + 10, 28, eyeW);
            P(x0 + 7, 29, irisL);

            // 眼下淡影
            R(x0 + 2, 23, 10, 1, skinS);
        }
        Eye(11, false);
        Eye(39, true);

        // 鼻影/鼻梁（2px 宽，平涂）
        R(31, 18, 2, 5, skinS);
        P(31, 18, skinD); P(32, 22, skinD);
        R(30, 22, 1, 5, skinL);

        // 下弯的嘴
        R(28, 16, 8, 1, lip);
        P(27, 17, lipD); P(36, 17, lipD);
        R(30, 15, 4, 1, skinS);

        // 面颊/下巴的少量平涂高光
        R(9, 21, 3, 1, skinL); R(52, 21, 3, 1, skinL);
        P(32, 3, skinL);

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
