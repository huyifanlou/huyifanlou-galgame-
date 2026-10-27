using UnityEngine;

// 《我的世界》风格方块人：8×12×4 像素比例的方块头/身/四肢，
// 正面用方块像素画眼睛/嘴、块状头发与校服（金发、藏青外套、白衬衫红领带、深蓝裤）。
// 只做视觉与动画，不挂碰撞；碰撞仍由 Player 上的 CapsuleCollider 负责。
public class PlayerAvatar : MonoBehaviour
{
    // 1 像素 = 1.8m / 32px
    const float PX = 0.05625f;

    Transform _model, _body, _head, _legL, _legR, _armL, _armR;
    Material _skin, _hair, _hairD, _coat, _coatD, _shirt, _tie, _pants, _shoe;
    float _phase, _blend;
    bool _visible = true;

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
        _skin = MatLib.LitColor(MatLib.Hex("#f6dcae"));
        _hair = MatLib.LitColor(MatLib.Hex("#eccb5a"));
        _hairD = MatLib.LitColor(MatLib.Hex("#c9a234"));
        _coat = MatLib.LitColor(MatLib.Hex("#46567f"));
        _coatD = MatLib.LitColor(MatLib.Hex("#333f5e"));
        _shirt = MatLib.LitColor(MatLib.Hex("#f4f1e6"));
        _tie = MatLib.LitColor(MatLib.Hex("#b23a44"));
        _pants = MatLib.LitColor(MatLib.Hex("#2e3550"));
        _shoe = MatLib.LitColor(MatLib.Hex("#23262e"));

        // 髋部枢轴：腿 12px、身 12px、头 8px，总高 32px = 1.8m
        _model = Node(transform, "model", Vector3.zero);
        _body = Node(_model, "body", new Vector3(0f, 12f * PX, 0f));

        // 身体 8×12×4
        Block(_body, "jacket", new Vector3(0f, 6f * PX, 0f), new Vector3(8f * PX, 12f * PX, 4f * PX), _coat);
        // 像素前脸装饰：衬衫 V、红领带、腰带（贴在身体正面外 1~2mm）
        Block(_body, "shirt", new Vector3(0f, 7.5f * PX, 2.22f * PX), new Vector3(3.6f * PX, 5.2f * PX, 0.16f * PX), _shirt);
        Block(_body, "tieKnot", new Vector3(0f, 9.6f * PX, 2.30f * PX), new Vector3(1.3f * PX, 1f * PX, 0.16f * PX), _tie);
        Block(_body, "tie", new Vector3(0f, 7.8f * PX, 2.30f * PX), new Vector3(0.9f * PX, 3.6f * PX, 0.16f * PX), _tie);
        Block(_body, "belt", new Vector3(0f, 1.2f * PX, 2.22f * PX), new Vector3(7.6f * PX, 1f * PX, 0.16f * PX), _coatD);
        Block(_body, "hem", new Vector3(0f, 0.6f * PX, 0f), new Vector3(8.1f * PX, 1.2f * PX, 4.1f * PX), _coatD);

        // 头：8×8×8，中心在身体上方
        _head = Node(_body, "head", new Vector3(0f, 16f * PX, 0f));
        Block(_head, "head", Vector3.zero, new Vector3(8f * PX, 8f * PX, 8f * PX), _skin);
        // 正面 32×32 程序化像素脸（分层刘海、剑眉、眼神光、鼻影、微笑嘴角）
        var faceMat = MatLib.LitTexture(FaceTexture(), 1f, 1f, 0f);
        FacePlate(_head, new Vector3(0f, 0f, 4.03f * PX), new Vector3(8f * PX, 8f * PX, 0.10f * PX), faceMat);
        // 块状头发：顶、后、两侧 + 两撮呆毛（前刘海由脸贴图画）
        Block(_head, "hairTop", new Vector3(0f, 4.6f * PX, 0f), new Vector3(8.4f * PX, 1.4f * PX, 8.4f * PX), _hair);
        Block(_head, "hairBack", new Vector3(0f, 0.2f * PX, -4.1f * PX), new Vector3(8f * PX, 8f * PX, 0.8f * PX), _hair);
        Block(_head, "hairSideL", new Vector3(-4.1f * PX, 0.2f * PX, 0f), new Vector3(0.8f * PX, 8f * PX, 8f * PX), _hair);
        Block(_head, "hairSideR", new Vector3(4.1f * PX, 0.2f * PX, 0f), new Vector3(0.8f * PX, 8f * PX, 8f * PX), _hair);
        Block(_head, "tuftA", new Vector3(-1.6f * PX, 5.4f * PX, 0.8f * PX), new Vector3(1.4f * PX, 2.4f * PX, 1.2f * PX), _hair);
        Block(_head, "tuftB", new Vector3(1.9f * PX, 5.0f * PX, -0.4f * PX), new Vector3(1.0f * PX, 1.8f * PX, 1.0f * PX), _hairD);

        // 手臂 4×12×4：袖 9px + 手 3px
        _armL = Node(_body, "armL", new Vector3(-6f * PX, 12f * PX, 0f));
        _armR = Node(_body, "armR", new Vector3(6f * PX, 12f * PX, 0f));
        Arm(_armL);
        Arm(_armR);

        // 腿 4×12×4：裤 10px + 鞋 2px
        _legL = Node(_body, "legL", new Vector3(-2f * PX, 0f, 0f));
        _legR = Node(_body, "legR", new Vector3(2f * PX, 0f, 0f));
        Leg(_legL);
        Leg(_legR);

        SetLayer(_model.gameObject, 2);
    }

    void Arm(Transform p)
    {
        Block(p, "sleeve", new Vector3(0f, -4.5f * PX, 0f), new Vector3(4f * PX, 9f * PX, 4f * PX), _coatD);
        Block(p, "hand", new Vector3(0f, -10.5f * PX, 0f), new Vector3(4f * PX, 3f * PX, 4f * PX), _skin);
    }

    void Leg(Transform p)
    {
        Block(p, "pant", new Vector3(0f, -5f * PX, 0f), new Vector3(4f * PX, 10f * PX, 4f * PX), _pants);
        Block(p, "shoe", new Vector3(0f, -11f * PX, 0.4f * PX), new Vector3(4.1f * PX, 2f * PX, 4.6f * PX), _shoe);
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
        _visible = v;
        if (_model != null) _model.gameObject.SetActive(v);
    }

    // ── 小工具 ───────────────────────────────────────────
    static Transform Node(Transform parent, string name, Vector3 pos)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        return t;
    }

    static void Block(Transform p, string name, Vector3 pos, Vector3 size, Material mat)
    {
        var go = new GameObject(name);
        go.layer = 2;
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.BoxUV(size, 0.25f);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
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

    // 32×32 像素脸：分层刘海、剑眉、带高光的眼睛、鼻影、微笑嘴角
    static Texture2D FaceTexture()
    {
        const int S = 32;
        var t = new Texture2D(S, S, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[S * S];
        Color skin = MatLib.Hex("#f6dcae"), skinD = MatLib.Hex("#dcbb8d"), skinL = MatLib.Hex("#ffe6c0");
        Color hair = MatLib.Hex("#f7dc86"), hairD = MatLib.Hex("#d4ab3c"), hairL = MatLib.Hex("#fff0b0");
        Color brow = MatLib.Hex("#9c7a26"), eyeW = MatLib.Hex("#fbf9f2");
        Color iris = MatLib.Hex("#3f6fb0"), pupil = MatLib.Hex("#141a26");
        Color lash = MatLib.Hex("#5a4630"), lip = MatLib.Hex("#b0625a"), lipL = MatLib.Hex("#d89488");
        Color mole = MatLib.Hex("#6b4a3a");

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
        R(0, 0, 2, S, skinD); R(S - 2, 0, 2, S, skinD);
        R(0, 0, S, 2, skinD);

        // 刘海：逐列高度做斜向碎发，右侧额头逐渐露出
        for (int x = 0; x < S; x++)
        {
            int yb = 18 + x / 6;                 // 缓斜刘海，避免大块秃额
            if (yb > S - 2) yb = S - 2;
            R(x, yb, 1, S - yb, hair);
            if (x % 3 == 1) R(x, Mathf.Max(15, yb - 1), 1, 1, hairD);
            if (x % 4 == 2) R(x, Mathf.Max(16, yb - 2), 1, 1, hairL);
        }
        // 几缕扫过额头的碎发，打破整块皮肤
        R(22, 19, 1, 5, hairD); R(25, 18, 1, 6, hair);
        R(28, 20, 1, 5, hairD); R(30, 21, 1, 4, hair);
        P(24, 17, hairD); P(27, 16, hairL);

        // 剑眉（外端略下压，画在眼睛上方）
        R(6, 16, 9, 3, brow); R(17, 16, 9, 3, brow);
        P(5, 15, brow); P(26, 15, brow); P(14, 19, brow); P(17, 19, brow);

        void Eye(int x0, bool right)
        {
            R(x0, 9, 8, 7, eyeW);
            int ix = right ? x0 + 1 : x0 + 2;
            R(ix, 10, 4, 5, iris);
            R(ix + 1, 11, 2, 3, pupil);
            R(right ? ix + 2 : ix, 13, 2, 2, Color.white);
            R(x0, 9, 8, 1, skinD);                 // 下眼睑
            R(x0, 14, 8, 1, skinL);                // 上眼睑受光
            R(x0, 15, 8, 1, lash);                 // 上睫毛
            R(right ? x0 + 8 : x0 - 1, 12, 1, 4, lash);   // 外眼角
        }
        Eye(7, false);
        Eye(17, true);

        // 鼻影 + 高光、微笑嘴角、酒窝痣
        R(15, 5, 2, 6, skinD);
        P(14, 6, skinL); P(15, 10, skinD);
        R(12, 3, 8, 2, lip);
        P(11, 4, lip); P(20, 4, lip);
        R(12, 2, 8, 1, lipL);
        P(23, 6, mole);
        P(9, 5, skinL); P(22, 5, skinL);

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