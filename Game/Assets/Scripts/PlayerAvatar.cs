using UnityEngine;

// 《我的世界》风格男主方块人：樊楼。短金发、蓝眼、藏青西装校服。
// 作者给的参考皮肤只借五官精度；金发长裙是觉醒后的女体形态，不是默认男主。
// 只做视觉与动画，不挂碰撞；碰撞仍由 Player 上的 CapsuleCollider 负责。
public class PlayerAvatar : MonoBehaviour
{
    // 1 像素 = 1.8m / 32px
    const float PX = 0.05625f;

    Transform _model, _body, _head, _legL, _legR, _armL, _armR;
    Material _skin, _hair, _hairD, _hairL, _coat, _coatD, _shirt;
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
        _hair = LitGlow("#eccb5a", 0.14f);
        _hairD = LitGlow("#c9a234", 0.08f);
        _hairL = LitGlow("#f7e28a", 0.16f);
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
        faceMat.SetColor("_EmissionColor", new Color(0.32f, 0.30f, 0.28f));
        faceMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        FacePlate(_head, V(0f, 0f, 4.03f), S(8f, 8f, 0.10f), faceMat);

        // 短金发：头顶、短后发、鬓角；前刘海由 64×64 脸贴图负责
        Block(_head, "hairTop", V(0f, 4.3f, 0f), S(8.6f, 1.5f, 8.6f), _hair);
        Block(_head, "fringeLip", V(0f, 3.55f, 4.03f), S(8.2f, 0.7f, 0.5f), _hair);
        Block(_head, "hairShine", V(0.8f, 4.2f, 4.32f), S(2.6f, 0.14f, 0.06f), _hairL);
        Block(_head, "hairBack", V(0f, 0f, -4.3f), S(8.2f, 8.8f, 1.2f), _hair);
        Block(_head, "nape", V(0f, -5.0f, -4.2f), S(6.8f, 1.8f, 1.0f), _hairD);
        Block(_head, "hairBackStrandL", V(-2.0f, -1.2f, -4.95f), S(0.3f, 5.5f, 0.08f), _hairD);
        Block(_head, "hairBackStrandR", V(2.0f, -1.2f, -4.95f), S(0.3f, 5.5f, 0.08f), _hairD);
        Block(_head, "hairBackLight", V(0.6f, -1.0f, -4.96f), S(0.5f, 5.0f, 0.08f), _hairL);
        Block(_head, "sideHairL", V(-4.2f, 0.3f, -0.4f), S(0.9f, 7f, 7f), _hair);
        Block(_head, "sideHairR", V(4.2f, 0.3f, -0.4f), S(0.9f, 7f, 7f), _hair);
        Block(_head, "sideburnL", V(-4.15f, -2.2f, 1.9f), S(0.9f, 3.8f, 1.2f), _hairD);
        Block(_head, "sideburnR", V(4.15f, -2.2f, 1.9f), S(0.9f, 3.8f, 1.2f), _hairD);

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
        _head.localRotation = Quaternion.Euler(idle * 1.5f, 0f, 0f);
    }

    public void SetVisible(bool v)
    {
        if (_model != null) _model.gameObject.SetActive(v);
    }

    // ── 小工具 ───────────────────────────────────────────
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z) * PX;
    static Vector3 S(float x, float y, float z) => new Vector3(x, y, z) * PX;

    static Material LitGlow(string hex, float glow)
    {
        var m = MatLib.LitColor(MatLib.Hex(hex));
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", MatLib.Hex(hex) * glow);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        return m;
    }

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

    // 64×64 男版脸：短金发斜刘海、直线眉、窄蓝眼、鼻影、轻微上扬的嘴角
    static Texture2D FaceTexture()
    {
        const int S = 64;
        var t = new Texture2D(S, S, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[S * S];
        Color skin = MatLib.Hex("#f8dfb9"), skinS = MatLib.Hex("#efd0a9"), skinD = MatLib.Hex("#dfb88e");
        Color skinL = MatLib.Hex("#fff0d4");
        Color hair = MatLib.Hex("#eccb5a"), hairD = MatLib.Hex("#c9a234"), hairL = MatLib.Hex("#f7e28a");
        Color hairEdge = MatLib.Hex("#a47a22");
        Color brow = MatLib.Hex("#6f5128"), lash = MatLib.Hex("#4a3a2c");
        Color eyeW = MatLib.Hex("#f8fbfd"), iris = MatLib.Hex("#2a5f96"), irisD = MatLib.Hex("#173e66");
        Color irisL = MatLib.Hex("#3f86b4"), pupil = MatLib.Hex("#0a1622");
        Color lip = MatLib.Hex("#b5786e"), lipD = MatLib.Hex("#9c5f57");

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
        R(0, 0, 3, S, skinD); R(S - 3, 0, 3, S, skinD);
        R(3, 0, 2, S, skinS); R(S - 5, 0, 2, S, skinS);
        R(0, 0, S, 2, skinS);
        // 下颌/下巴阴影，让男版脸更有棱角
        R(6, 4, 4, 16, skinS); R(S - 10, 4, 4, 16, skinS);
        R(26, 2, 12, 2, skinS);
        P(31, 2, skinD); P(33, 2, skinD);

        // 短金发：左侧低、右侧高，发际线压到眉上方
        for (int x = 0; x < S; x++)
        {
            int yb = x < 32 ? 51 - x / 12 : 48 + (x - 32) / 16;
            R(x, yb, 1, S - yb, hair);
            P(x, yb, hairEdge);
            P(x, yb - 1, skinS);
            if (x % 9 == 2) P(x, yb + 4, hairL);
            if (x % 7 == 0) P(x, yb, hairD);
        }
        // 两侧短鬓角
        R(0, 30, 2, 34, hair); R(S - 2, 30, 2, 34, hair);
        R(1, 30, 1, 34, hairD); R(S - 2, 30, 1, 34, hairD);

        // 四绺短碎发，尖端停在眉上方
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
                if (i == mid) P(x, y + 5, hairL);
            }
        }
        Lock(4, 8, 44);
        Lock(16, 7, 46);
        Lock(34, 7, 45);
        Lock(48, 8, 44);

        // 侧分线与发丝
        R(44, 51, 1, 13, hairEdge); R(45, 51, 1, 13, hairL);
        R(12, 55, 2, 8, hairD); R(24, 54, 1, 9, hairL);
        R(36, 55, 2, 8, hairD); R(52, 55, 2, 8, hairL);

        // 直线眉，外低内高，压近眼睛
        for (int i = 0; i < 17; i++)
        {
            int by = 38 + i / 10;
            R(10 + i, by, 1, 2, brow);
            R(53 - i, by, 1, 2, brow);
        }

        void Eye(int x0, bool right)
        {
            int outer = right ? x0 + 13 : x0;
            R(x0, 26, 14, 8, eyeW);
            P(outer, 33, skinS); P(outer, 26, skinS);

            // 深蓝虹膜：收窄眼型，压深颜色，保留小高光
            R(x0 + 3, 27, 8, 6, iris);
            R(x0 + 3, 27, 8, 2, irisL);
            R(x0 + 3, 31, 8, 2, irisD);
            P(x0 + 3, 28, irisD); P(x0 + 10, 28, irisD);
            P(x0 + 3, 29, irisD); P(x0 + 10, 29, irisD);
            R(x0 + 6, 28, 3, 4, pupil);
            R(x0 + 4, 30, 2, 2, Color.white);
            P(x0 + 10, 27, eyeW);
            P(x0 + 7, 28, irisL);

            // 平直上眼睑，不做女版外眼角上挑
            R(x0, 33, 14, 1, lash);
            R(outer, 30, 1, 3, lash);
            R(x0 + 2, 35, 10, 1, skinS);
            R(x0, 25, 14, 1, skinD);
        }
        Eye(11, false);
        Eye(39, true);

        // 鼻梁/鼻头
        R(31, 20, 2, 4, skinS);
        P(30, 21, skinL); P(32, 24, skinD); P(31, 19, skinD);
        P(30, 25, skinL); P(30, 26, skinL);

        // 嘴：12px 宽，轻微上扬
        R(26, 17, 12, 1, lip);
        P(38, 18, lip); P(25, 17, lipD);
        R(29, 16, 6, 1, skinS);

        // 面颊骨高光 + 下巴高光
        R(9, 24, 3, 1, skinL); R(52, 24, 3, 1, skinL);
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
