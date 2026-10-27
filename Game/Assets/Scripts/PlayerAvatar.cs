using UnityEngine;

// 《我的世界》风格男主方块人：樊楼。金色短发、蓝眼、男版水手校服。
// 金发长裙是觉醒后的女体形态，不是默认男主。
// 只做视觉与动画，不挂碰撞；碰撞仍由 Player 上的 CapsuleCollider 负责。
public class PlayerAvatar : MonoBehaviour
{
    // 1 像素 = 1.8m / 32px
    const float PX = 0.05625f;

    Transform _model, _body, _head, _legL, _legR, _armL, _armR;
    Material _skin, _hair, _hairD, _coat, _coatD, _shirt, _shirtD;
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
        _skin = MatLib.LitColor(MatLib.Hex("#f3d9bf"));
        _skin.EnableKeyword("_EMISSION");
        _skin.SetColor("_EmissionColor", new Color(0.55f, 0.45f, 0.36f) * 0.10f);
        _skin.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        var hairTex = HairTexture();
        _hair = HairMat(hairTex, Color.white, 0.08f);
        _hairD = HairMat(hairTex, new Color(0.86f, 0.78f, 0.60f), 0.04f);
        _coat = MatLib.LitColor(MatLib.Hex("#46577f"));
        _coatD = MatLib.LitColor(MatLib.Hex("#333f5e"));
        _shirt = MatLib.LitColor(MatLib.Hex("#f8faf4"));
        _shirtD = MatLib.LitColor(MatLib.Hex("#dfe2d8"));
        _shirt.EnableKeyword("_EMISSION");
        _shirt.SetColor("_EmissionColor", new Color(0.9f, 0.9f, 0.85f) * 0.26f);
        _shirt.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        _shirtD.EnableKeyword("_EMISSION");
        _shirtD.SetColor("_EmissionColor", new Color(0.9f, 0.9f, 0.85f) * 0.08f);
        _shirtD.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        _tie = MatLib.LitColor(MatLib.Hex("#c84450"));
        _tieD = MatLib.LitColor(MatLib.Hex("#9a2f3b"));
        _tie.EnableKeyword("_EMISSION");
        _tie.SetColor("_EmissionColor", new Color(0.55f, 0.16f, 0.18f) * 0.10f);
        _tie.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        _pants = MatLib.LitColor(MatLib.Hex("#26304a"));
        _shoe = MatLib.LitColor(MatLib.Hex("#20232b"));
        _sole = MatLib.LitColor(MatLib.Hex("#14161c"));
        _belt = MatLib.LitColor(MatLib.Hex("#1d1f27"));
        _silver = MatLib.LitColor(MatLib.Hex("#b9bec6"));
        _badgeBlue = MatLib.LitColor(MatLib.Hex("#2f5f92"));

        // 髋部枢轴：腿 12px、身 12px、头 8px，总高 32px = 1.8m
        _model = Node(transform, "model", Vector3.zero);
        _body = Node(_model, "body", V(0f, 12f, 0f));

        // 身体 8×12×4：白衬衫 + 藏青水手领 + 红领巾
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

        // 金色短发：锥形发盖 + 前后错层发顶 + 侧发/鬓角/短后颈（无猫耳）
        HairVolume(_head, "hairCap", V(0f, 3.4f, -0.05f), 8.3f, 8.3f, 6.8f, 6.8f, 1.5f, new Vector3(-3f, 0f, 0f), _hair);
        HairVolume(_head, "crownBack", V(0f, 4.4f, -0.8f), 6.8f, 5.2f, 5.0f, 4.0f, 1.4f, new Vector3(-6f, 0f, 0f), _hair);
        HairVolume(_head, "crownFront", V(0f, 4.3f, 1.0f), 6.2f, 3.0f, 4.8f, 2.2f, 0.9f, new Vector3(10f, 0f, 0f), _hair);

        // 侧发：三束错落尖尾 + 鬓角
        HairLock(_head, "sideL1", -4.02f, -2.8f, 1.6f, 1.0f, 1.2f, 6.9f, 4f, _hair);
        HairLock(_head, "sideL2", -4.02f, -3.1f, 0.2f, 1.1f, 1.2f, 7.1f, 0f, _hairD);
        HairLock(_head, "sideL3", -4.02f, -2.5f, -1.2f, 0.9f, 1.2f, 6.6f, -3f, _hair);
        HairLock(_head, "sideR1", 4.02f, -2.8f, 1.6f, 1.0f, 1.2f, 6.9f, -4f, _hair);
        HairLock(_head, "sideR2", 4.02f, -3.1f, 0.2f, 1.1f, 1.2f, 7.1f, 0f, _hairD);
        HairLock(_head, "sideR3", 4.02f, -2.5f, -1.2f, 0.9f, 1.2f, 6.6f, 3f, _hair);
        HairLock(_head, "sideburnL", -4.1f, -3.4f, 2.2f, 0.8f, 1.0f, 2.6f, 8f, _hairD);
        HairLock(_head, "sideburnR", 4.1f, -3.4f, 2.2f, 0.8f, 1.0f, 2.6f, -8f, _hairD);

        // 额前发际碎发：只在发际线上方，不压脸
        Taper(_head, "frontLockL", V(-3.1f, 4.05f, 3.95f), 1.3f, 0.6f, 0.3f, 0.35f, 1.3f, new Vector3(-16f, 0f, 14f), _hair);
        Taper(_head, "frontLockM", V(0.2f, 4.05f, 4.0f), 1.2f, 0.6f, 0.25f, 0.35f, 0.9f, new Vector3(-12f, 0f, -4f), _hair);
        Taper(_head, "frontLockR", V(3.0f, 4.05f, 3.95f), 1.3f, 0.6f, 0.3f, 0.35f, 1.3f, new Vector3(-16f, 0f, -14f), _hair);

        // 后脑短发：整块锥形后发 + 三束短尖尾（不做满头发排，避免木箱感）
        HairVolume(_head, "backMass", V(0f, -2.4f, -4.2f), 7.0f, 1.0f, 7.8f, 1.0f, 6.4f, Vector3.zero, _hair);
        Taper(_head, "napeTipL", V(-2.2f, -3.4f, -4.6f), 0.4f, 0.55f, 1.6f, 0.8f, 1.8f, new Vector3(0f, 0f, 8f), _hair);
        Taper(_head, "napeTipM", V(0f, -3.2f, -4.65f), 0.4f, 0.55f, 1.6f, 0.8f, 1.6f, Vector3.zero, _hairD);
        Taper(_head, "napeTipR", V(2.2f, -3.4f, -4.6f), 0.4f, 0.55f, 1.6f, 0.8f, 1.8f, new Vector3(0f, 0f, -8f), _hair);

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
        Block(p, "sleeve", V(0f, -4.2f, 0f), S(4f, 8.4f, 4f), _shirt);
        Block(p, "cuff", V(0f, -9.1f, 0f), S(4.05f, 1.4f, 4.05f), _coat);
        Block(p, "cuffStripe", V(0f, -9.62f, 0f), S(4.1f, 0.22f, 4.1f), _shirt);
        Block(p, "hand", V(0f, -10.9f, 0f), S(4f, 2.2f, 4f), _skin);
    }

    void Leg(Transform p)
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

    // 尖尾发束：下窄上宽，底部收成发尖
    static void HairLock(Transform p, string name, float x, float yBase, float z, float w, float d, float h, float tilt, Material mat)
        => Taper(p, name, V(x, yBase, z), w * 0.35f, d * 0.6f, w, d * 0.85f, h, new Vector3(0f, 0f, tilt), mat);

    static Material HairMat(Texture2D tex, Color tint, float glow)
    {
        var m = MatLib.LitTexture(tex, 1f, 1f, 0f);
        m.color = tint;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", new Color(0.95f, 0.78f, 0.42f) * glow);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        return m;
    }

    // 64×64 程序化发丝纹理：金色细竖纹，平铺后不会出现竹篮格
    static Texture2D HairTexture()
    {
        const int S = 64;
        var t = new Texture2D(S, S, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        var px = new Color32[S * S];
        Color b = MatLib.Hex("#ddb04f"), d = MatLib.Hex("#c99a3e"), l = MatLib.Hex("#efd58e");
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
    static Texture2D FaceTexture()
    {
        const int S = 64;
        var t = new Texture2D(S, S, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[S * S];
        Color skin = MatLib.Hex("#f3d9bf"), skinD = MatLib.Hex("#e2c1a2");
        Color hair = MatLib.Hex("#ddb04f"), hairL = MatLib.Hex("#efd58e"), hairD = MatLib.Hex("#c99a3e");
        Color dark = MatLib.Hex("#5a4326");
        Color eyeW = MatLib.Hex("#f4f2ec");
        Color iris = MatLib.Hex("#2f5f92"), irisL = MatLib.Hex("#6aa0c8"), irisHi = MatLib.Hex("#b8dcec");
        Color pupil = MatLib.Hex("#193a5e");
        Color mouth = MatLib.Hex("#8a5f55");

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

        // 眉行：先铺脸，再压深色眉；外圈留 1px 肤色
        R(8, 32, 48, 8, skin);
        R(0, 32, 8, 8, hair);
        R(56, 32, 8, 8, hair);
        R(9, 33, 14, 5, dark);
        R(41, 33, 14, 5, dark);

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

        // 嘴行：鼻影 + 6px 小嘴 + 下唇淡影
        R(0, 8, S, 8, skin);
        R(31, 13, 2, 1, skinD);
        R(29, 10, 6, 1, mouth);
        R(28, 9, 8, 1, skinD);

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
