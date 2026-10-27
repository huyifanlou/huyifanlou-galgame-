using UnityEngine;

// 《我的世界》风格方块人：按作者给的参考皮肤（薇尔莉特）做浅金长发、蓝眼、
// 红发带、藏青外套 + 白衬衫 + 棕色皮束带、白裙白袜棕靴。
// 只做视觉与动画，不挂碰撞；碰撞仍由 Player 上的 CapsuleCollider 负责。
public class PlayerAvatar : MonoBehaviour
{
    // 1 像素 = 1.8m / 32px
    const float PX = 0.05625f;

    Transform _model, _body, _head, _hairBack, _legL, _legR, _armL, _armR;
    Material _skin, _hair, _hairD, _hairL, _coat, _coatD, _coatL;
    Material _blouse, _leather, _leatherD, _gold, _ribbon, _ribbonD;
    Material _stocking, _boot, _bootD, _sole, _glove;
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
        _skin = MatLib.LitColor(MatLib.Hex("#f8e2d2"));
        _hair = LitGlow("#f2c96a", 0.30f);
        _hairD = LitGlow("#c99a45", 0.18f);
        _hairL = LitGlow("#ffe9a0", 0.32f);
        _coat = MatLib.LitColor(MatLib.Hex("#3f4a70"));
        _coatD = MatLib.LitColor(MatLib.Hex("#343260"));
        _coatL = MatLib.LitColor(MatLib.Hex("#4b6480"));
        _blouse = MatLib.LitColor(MatLib.Hex("#f7f4f0"));
        _leather = MatLib.LitColor(MatLib.Hex("#7a564c"));
        _leatherD = MatLib.LitColor(MatLib.Hex("#5c4147"));
        _gold = MatLib.LitColor(MatLib.Hex("#d9bd7c"));
        _ribbon = MatLib.LitColor(MatLib.Hex("#a3364a"));
        _ribbonD = MatLib.LitColor(MatLib.Hex("#7d2335"));
        _stocking = MatLib.LitColor(MatLib.Hex("#f4f1ec"));
        _boot = MatLib.LitColor(MatLib.Hex("#6e575f"));
        _bootD = MatLib.LitColor(MatLib.Hex("#4f3a44"));
        _sole = MatLib.LitColor(MatLib.Hex("#8c8887"));
        _glove = MatLib.LitColor(MatLib.Hex("#f5f1d7"));

        // 髋部枢轴：腿 12px、身 12px、头 8px，总高 32px = 1.8m
        _model = Node(transform, "model", Vector3.zero);
        _body = Node(_model, "body", V(0f, 12f, 0f));

        // 身体 8×12×4：藏青外套打底，正面叠白衬衫/皮束带/红领结
        Block(_body, "jacket", V(0f, 6f, 0f), S(8f, 12f, 4f), _coat);
        Block(_body, "blouse", V(0f, 6.2f, 2.10f), S(3.6f, 7.6f, 0.14f), _blouse);
        Block(_body, "seamL", V(-1.88f, 6.2f, 2.10f), S(0.18f, 7.4f, 0.12f), _coatD);
        Block(_body, "seamR", V(1.88f, 6.2f, 2.10f), S(0.18f, 7.4f, 0.12f), _coatD);
        Block(_body, "collar", V(0f, 11.3f, 2.10f), S(4.2f, 0.85f, 0.16f), _coatD);
        Block(_body, "collarWhiteL", V(-0.95f, 10.5f, 2.22f), S(1.3f, 0.9f, 0.14f), _blouse);
        Block(_body, "collarWhiteR", V(0.95f, 10.5f, 2.22f), S(1.3f, 0.9f, 0.14f), _blouse);

        // 红领结 + 两条垂带
        Block(_body, "bowKnot", V(0f, 10.55f, 2.40f), S(0.8f, 0.8f, 0.20f), _ribbonD);
        BlockR(_body, "bowL", V(-1.0f, 10.55f, 2.34f), S(1.2f, 0.85f, 0.18f), new Vector3(0f, 0f, 12f), _ribbon);
        BlockR(_body, "bowR", V(1.0f, 10.55f, 2.34f), S(1.2f, 0.85f, 0.18f), new Vector3(0f, 0f, -12f), _ribbon);
        Block(_body, "bowTailL", V(-0.45f, 9.55f, 2.32f), S(0.45f, 1.3f, 0.16f), _ribbonD);
        Block(_body, "bowTailR", V(0.45f, 9.55f, 2.32f), S(0.45f, 1.3f, 0.16f), _ribbonD);

        // 棕色皮束带：胸带、双肩竖带、腰封，金色扣具
        Block(_body, "chestBand", V(0f, 8.2f, 2.20f), S(7.9f, 0.7f, 0.16f), _leatherD);
        Block(_body, "chestBuckle", V(0f, 8.2f, 2.32f), S(0.85f, 0.75f, 0.14f), _gold);
        Block(_body, "harnessL", V(-2.55f, 5.2f, 2.18f), S(0.55f, 5.6f, 0.16f), _leather);
        Block(_body, "harnessR", V(2.55f, 5.2f, 2.18f), S(0.55f, 5.6f, 0.16f), _leather);
        Block(_body, "waistBand", V(0f, 1.8f, 2.18f), S(7.9f, 1.4f, 0.18f), _leather);
        Block(_body, "waistBuckle", V(0f, 1.8f, 2.32f), S(1.1f, 1.2f, 0.16f), _gold);
        Block(_body, "button1", V(3.25f, 6.9f, 2.16f), S(0.45f, 0.45f, 0.14f), _gold);
        Block(_body, "button2", V(3.25f, 5.4f, 2.16f), S(0.45f, 0.45f, 0.14f), _gold);
        Block(_body, "button3", V(3.25f, 3.9f, 2.16f), S(0.45f, 0.45f, 0.14f), _gold);

        // 背面：斜跨皮带 + 金扣、后中缝、深色下摆
        BlockR(_body, "backStrap", V(0f, 5.4f, -2.18f), S(0.55f, 7.6f, 0.16f), new Vector3(0f, 0f, 32f), _leather);
        Block(_body, "backBuckle", V(0f, 5.2f, -2.34f), S(0.8f, 0.8f, 0.14f), _gold);
        Block(_body, "backSeam", V(0f, 6.2f, -2.10f), S(0.18f, 7.2f, 0.12f), _coatD);
        Block(_body, "hem", V(0f, 0.35f, 0f), S(8.05f, 0.7f, 4.05f), _coatD);

        // 头：8×8×8，中心在身体上方
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
        faceMat.SetColor("_EmissionColor", new Color(0.37f, 0.35f, 0.33f));
        faceMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        FacePlate(_head, V(0f, 0f, 4.03f), S(8f, 8f, 0.10f), faceMat);

        // 浅金长发：只压住头顶和发际线，露出额头与整张脸；两侧鬓发、背后长发 + 发尾
        Block(_head, "hairTop", V(0f, 4.4f, 0f), S(8.6f, 1.2f, 8.6f), _hair);
        Block(_head, "fringeLip", V(0f, 3.55f, 4.03f), S(8.2f, 0.6f, 0.5f), _hair);
        // 额前刘海交给 64×64 脸贴图；3D 只保留头顶、两侧鬓发与后发体积
        Block(_head, "hairShine", V(0.8f, 4.15f, 4.32f), S(3.2f, 0.16f, 0.06f), _hairL);
        Block(_head, "sideLockL", V(-4.38f, 0.2f, 1.9f), S(0.95f, 8.6f, 2.0f), _hair);
        Block(_head, "sideLockR", V(4.38f, 0.2f, 1.9f), S(0.95f, 8.6f, 2.0f), _hair);
        Block(_head, "sideTipL", V(-4.33f, -4.6f, 1.9f), S(0.85f, 1.6f, 1.8f), _hairD);
        Block(_head, "sideTipR", V(4.33f, -4.6f, 1.9f), S(0.85f, 1.6f, 1.8f), _hairD);

        _hairBack = Node(_head, "hairBack", V(0f, 4f, -4.4f));
        Block(_hairBack, "main", V(0f, -6.5f, 0f), S(8.2f, 13f, 1.3f), _hair);
        Block(_hairBack, "tail", V(0f, -13.2f, -0.75f), S(6.8f, 3.2f, 0.8f), _hairD);
        Block(_hairBack, "highlightL", V(-1.8f, -6.0f, -0.72f), S(0.5f, 9.5f, 0.08f), _hairL);
        Block(_hairBack, "highlightR", V(1.8f, -6.0f, -0.72f), S(0.5f, 9.5f, 0.08f), _hairL);
        Block(_hairBack, "edgeL", V(-3.9f, -6.0f, -0.7f), S(0.35f, 11f, 0.08f), _hairD);
        Block(_hairBack, "edgeR", V(3.9f, -6.0f, -0.7f), S(0.35f, 11f, 0.08f), _hairD);
        Block(_hairBack, "grooveL", V(-2.6f, -6.0f, -0.72f), S(0.28f, 10f, 0.08f), _hairD);
        Block(_hairBack, "grooveR", V(2.6f, -6.0f, -0.72f), S(0.28f, 10f, 0.08f), _hairD);

        // 脑后的红发带，挂在长发外侧
        Block(_hairBack, "ribbonKnot", V(0f, -7.0f, -0.95f), S(0.8f, 0.8f, 0.5f), _ribbonD);
        BlockR(_hairBack, "ribbonL", V(-1.2f, -7.0f, -0.9f), S(1.5f, 1.0f, 0.45f), new Vector3(0f, 0f, 14f), _ribbon);
        BlockR(_hairBack, "ribbonR", V(1.2f, -7.0f, -0.9f), S(1.5f, 1.0f, 0.45f), new Vector3(0f, 0f, -14f), _ribbon);
        Block(_hairBack, "ribbonTailL", V(-0.7f, -9.2f, -0.9f), S(0.5f, 2.8f, 0.35f), _ribbonD);
        Block(_hairBack, "ribbonTailR", V(0.7f, -9.2f, -0.9f), S(0.5f, 2.8f, 0.35f), _ribbonD);

        // 手臂 3×12×4（Alex 细臂）：白色泡泡袖 + 藏青袖 + 白袖口 + 白手套
        _armL = Node(_body, "armL", V(-5.6f, 12f, 0f));
        _armR = Node(_body, "armR", V(5.6f, 12f, 0f));
        Arm(_armL);
        Arm(_armR);

        // 腿 4×12×4：白袜 + 棕靴，白裙片挂在腿上随摆腿运动
        _legL = Node(_body, "legL", V(-2f, 0f, 0f));
        _legR = Node(_body, "legR", V(2f, 0f, 0f));
        Leg(_legL);
        Leg(_legR);

        SetLayer(_model.gameObject, 2);
    }

    void Arm(Transform p)
    {
        Block(p, "puff", V(0f, -1.0f, 0f), S(3.4f, 2.2f, 4.2f), _blouse);
        Block(p, "sleeve", V(0f, -4.9f, 0f), S(3f, 7.2f, 4f), _coatL);
        Block(p, "cuff", V(0f, -8.75f, 0f), S(3.15f, 0.7f, 4.15f), _blouse);
        Block(p, "glove", V(0f, -10.5f, 0f), S(3f, 2.8f, 4f), _glove);
    }

    void Leg(Transform p)
    {
        Block(p, "stocking", V(0f, -4f, 0f), S(4f, 8f, 4f), _stocking);
        Block(p, "bootCuff", V(0f, -7.95f, 0.05f), S(4.2f, 0.6f, 4.35f), _bootD);
        Block(p, "boot", V(0f, -9.4f, 0.15f), S(4.15f, 3.2f, 4.5f), _boot);
        Block(p, "sole", V(0f, -11.45f, 0.2f), S(4.2f, 1.1f, 4.6f), _sole);
        // 左右裙片前后错开 0.02px，避免中缝两片正面共面 z-fighting
        float zOff = p.localPosition.x > 0f ? 0.02f : -0.02f;
        Block(p, "skirt", V(0f, -1.4f, zOff), S(4.3f, 3f, 4.9f), _blouse);
        Block(p, "skirtHem", V(0f, -2.8f, zOff), S(4.35f, 0.5f, 4.95f), _coatD);
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
        // 裙片挂在腿上，摆幅比长裤略收，避免裙摆与身体互相穿模
        _legL.localRotation = Quaternion.Euler(sw * 34f, 0f, 0f);
        _legR.localRotation = Quaternion.Euler(-sw * 34f, 0f, 0f);
        _armL.localRotation = Quaternion.Euler(-sw * 32f + idle * 2.5f, 0f, 0f);
        _armR.localRotation = Quaternion.Euler(sw * 32f - idle * 2.5f, 0f, 0f);

        float bob = Mathf.Abs(Mathf.Sin(_phase)) * 1.0f * PX * 0.7f * _blend;
        float breath = Mathf.Sin(Time.time * 2.1f) * 0.15f * PX * (1f - _blend);
        _body.localPosition = new Vector3(0f, 12f * PX + bob + breath, 0f);
        _head.localRotation = Quaternion.Euler(idle * 1.5f, 0f, 0f);
        if (_hairBack != null)
            _hairBack.localRotation = Quaternion.Euler(-sw * 2.2f + idle * 0.8f, 0f, 0f);
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


    // 正面脸贴片：UV 1:1，避免 BoxUV 默认按 0.25m 平铺
    static void FacePlate(Transform p, Vector3 pos, Vector3 size, Material mat)
    {
        var go = new GameObject("face");
        go.layer = 2;
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.BoxUV(size, size.x, size.y);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // 64×64 像素脸：斜分碎刘海、明亮蓝眼、细眉、小弧笑唇
    static Texture2D FaceTexture()
    {
        const int S = 64;
        var t = new Texture2D(S, S, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[S * S];
        Color skin = MatLib.Hex("#ffeadf"), skinS = MatLib.Hex("#f7d7c9"), skinD = MatLib.Hex("#e8c0ae");
        Color skinL = MatLib.Hex("#fff9f0"), blush = MatLib.Hex("#f7c8c0");
        Color hair = MatLib.Hex("#f2c96a"), hairD = MatLib.Hex("#c99a45"), hairL = MatLib.Hex("#ffe9a0");
        Color hairS = MatLib.Hex("#a6782a");
        Color brow = MatLib.Hex("#8f6c56"), lash = MatLib.Hex("#6b4a44");
        Color eyeW = MatLib.Hex("#fdf8f6"), iris = MatLib.Hex("#4ab0e6"), irisD = MatLib.Hex("#2a7fbe");
        Color irisL = MatLib.Hex("#8fe6ff"), pupil = MatLib.Hex("#0a1b2c");
        Color lip = MatLib.Hex("#d18f87"), lipL = MatLib.Hex("#e8b3ab");

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
        R(24, 2, 16, 2, skinS);
        P(28, 3, skinD); P(36, 3, skinD);

        // 两侧鬓发
        R(0, 20, 5, S - 20, hair); R(S - 5, 20, 5, S - 20, hair);
        R(4, 20, 1, S - 20, hairD); R(S - 5, 20, 1, S - 20, hairD);
        R(0, 44, 1, S - 44, hairL); R(S - 1, 44, 1, S - 44, hairL);

        // 斜分刘海基底：压到眉上，左侧低、右侧高；发际线做连续暗边 + 皮肤投影
        for (int x = 0; x < S; x++)
        {
            int yb = Mathf.Max(44, 47 - x / 14);
            if (x >= 30 && x <= 34) yb += 1;
            R(x, yb, 1, S - yb, hair);
            P(x, yb, hairS);
            P(x, yb + 1, hairD);
            P(x, yb - 1, skinD);
            if (x % 11 == 4) P(x, yb + 5, hairL);
        }

        // 三绺主碎发：左右鬓角 + 中央一绺
        void Lock(int x0, int w, int tip)
        {
            int mid = w / 2;
            for (int i = 0; i < w; i++)
            {
                int x = x0 + i;
                int y = tip + Mathf.Abs(i - mid) * 2;
                R(x, y, 1, S - y, hair);
                P(x, y, hairD);
                if (i == mid) P(x, y + 6, hairL);
            }
        }
        Lock(2, 9, 38);
        Lock(28, 9, 42);
        Lock(54, 9, 39);

        // 中分线与发丝高光
        R(31, 50, 1, 14, hairD); R(32, 50, 1, 14, hairL);
        R(12, 54, 2, 9, hairD); R(14, 55, 1, 7, hairL);
        R(39, 55, 2, 8, hairL); R(41, 52, 1, 10, hairD);
        R(51, 53, 2, 10, hairD); R(59, 50, 2, 12, hairL);

        // 连续拱形眉：中段略厚、两端收细，柔和不压眼
        for (int i = 0; i < 17; i++)
        {
            int by = 41 + (i <= 8 ? i / 3 : (16 - i) / 3);
            R(10 + i, by, 1, 1, brow);
            R(53 - i, by, 1, 1, brow);
            if (i >= 4 && i <= 10)
            {
                R(10 + i, by + 1, 1, 1, brow);
                R(53 - i, by + 1, 1, 1, brow);
            }
        }

        void Eye(int x0, bool right)
        {
            int outer = right ? x0 + 15 : x0;
            R(x0, 26, 16, 12, eyeW);
            P(outer, 37, skinS); P(outer, 26, skinS);
            P(right ? x0 : x0 + 15, 37, skinS);

            // 蓝虹膜：10px 宽，上深下青，中央深色瞳孔 + 两块高光
            R(x0 + 3, 27, 10, 9, iris);
            R(x0 + 3, 27, 10, 2, irisL);
            R(x0 + 3, 34, 10, 2, irisD);
            P(x0 + 3, 30, irisD); P(x0 + 12, 30, irisD);
            P(x0 + 3, 31, irisD); P(x0 + 12, 31, irisD);
            R(x0 + 6, 28, 3, 6, pupil);
            R(x0 + 4, 32, 3, 2, Color.white);
            P(x0 + 11, 28, eyeW);
            P(x0 + 7, 29, irisL); P(x0 + 8, 29, irisL);

            // 上睫毛：内侧 1px、外侧 2px，收成柔和眼型
            R(x0, 37, 16, 1, lash);
            if (right) R(x0 + 10, 36, 6, 1, lash);
            else R(x0, 36, 6, 1, lash);
            if (right) { R(x0 + 15, 35, 3, 2, lash); P(x0 + 18, 36, lash); }
            else { R(x0 - 2, 35, 3, 2, lash); P(x0 - 3, 36, lash); }
            R(outer, 30, 1, 3, lash);
            // 上眼睑轻褶 + 下眼睑
            R(x0 + 2, 38, 12, 1, skinS);
            R(x0, 25, 16, 1, skinD);
            P(x0 + 3, 26, skinL); P(x0 + 12, 26, skinL);
        }
        Eye(10, false);
        Eye(38, true);

        // 鼻：鼻梁一点受光 + 鼻头阴影
        R(31, 22, 2, 4, skinS);
        P(30, 23, skinL); P(32, 25, skinD); P(31, 21, skinD);
        P(30, 27, skinL); P(30, 28, skinL);

        // 嘴：小弧笑唇，下移 2px 缩短下巴留白，并补下唇阴影
        R(26, 17, 12, 1, lip);
        P(25, 18, lip); P(38, 18, lip);
        R(28, 16, 8, 1, lipL);
        P(29, 15, skinS); P(35, 15, skinS);

        // 腮红 + 面颊高光
        R(8, 22, 3, 1, blush); R(53, 22, 3, 1, blush);
        P(7, 23, blush); P(11, 23, blush); P(52, 23, blush); P(56, 23, blush);
        R(8, 26, 3, 1, skinL); R(53, 26, 3, 1, skinL);

        // 下眼睑阴影 + 下巴
        R(10, 24, 16, 1, skinS); R(38, 24, 16, 1, skinS);
        R(26, 3, 12, 2, skinS);
        P(12, 4, skinS); P(51, 4, skinS);

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
