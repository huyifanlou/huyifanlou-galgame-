using UnityEngine;

// 《我的世界》风格方块人：8×12×4 像素比例的方块头/身/四肢，
// 正面用方块像素画眼睛/嘴、块状头发与校服（金发、藏青外套、白衬衫红领带、深蓝裤）。
// 只做视觉与动画，不挂碰撞；碰撞仍由 Player 上的 CapsuleCollider 负责。
public class PlayerAvatar : MonoBehaviour
{
    // 1 像素 = 1.8m / 32px
    const float PX = 0.05625f;

    Transform _model, _body, _head, _legL, _legR, _armL, _armR;
    Material _skin, _skinD, _hair, _hairD, _coat, _coatD, _shirt, _tie, _pants, _shoe, _eye, _eyeW;
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
        _skinD = MatLib.LitColor(MatLib.Hex("#d9b98c"));
        _hair = MatLib.LitColor(MatLib.Hex("#eccb5a"));
        _hairD = MatLib.LitColor(MatLib.Hex("#c9a234"));
        _coat = MatLib.LitColor(MatLib.Hex("#46567f"));
        _coatD = MatLib.LitColor(MatLib.Hex("#333f5e"));
        _shirt = MatLib.LitColor(MatLib.Hex("#f4f1e6"));
        _tie = MatLib.LitColor(MatLib.Hex("#b23a44"));
        _pants = MatLib.LitColor(MatLib.Hex("#2e3550"));
        _shoe = MatLib.LitColor(MatLib.Hex("#23262e"));
        _eye = MatLib.LitColor(MatLib.Hex("#2b3a58"));
        _eyeW = MatLib.LitColor(MatLib.Hex("#f8f6ee"));

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
        // 像素眼睛：2×2 眼白 + 2×2 瞳孔，正面外 1mm
        foreach (float ex in new[] { -1.8f, 1.8f })
        {
            Block(_head, "eyeW", new Vector3(ex * PX, 0.6f * PX, 4.25f * PX),
                new Vector3(1.8f * PX, 1.8f * PX, 0.12f * PX), _eyeW);
            Block(_head, "eye", new Vector3(ex * PX, 0.6f * PX, 4.34f * PX),
                new Vector3(0.9f * PX, 1.8f * PX, 0.12f * PX), _eye);
        }
        Block(_head, "mouth", new Vector3(0f, -2.2f * PX, 4.25f * PX),
            new Vector3(1.8f * PX, 0.5f * PX, 0.12f * PX), _skinD);
        // 块状头发：顶、后、两侧、像素刘海
        Block(_head, "hairTop", new Vector3(0f, 4.6f * PX, 0f), new Vector3(8.4f * PX, 1.4f * PX, 8.4f * PX), _hair);
        Block(_head, "hairBack", new Vector3(0f, 0.2f * PX, -4.1f * PX), new Vector3(8f * PX, 8f * PX, 0.8f * PX), _hair);
        Block(_head, "hairSideL", new Vector3(-4.1f * PX, 0.2f * PX, 0f), new Vector3(0.8f * PX, 8f * PX, 8f * PX), _hair);
        Block(_head, "hairSideR", new Vector3(4.1f * PX, 0.2f * PX, 0f), new Vector3(0.8f * PX, 8f * PX, 8f * PX), _hair);
        for (int i = 0; i < 6; i++)
        {
            float fx = -2.9f * PX + i * 1.16f * PX;
            Block(_head, "fringe" + i, new Vector3(fx, 3.4f * PX, 4.25f * PX),
                new Vector3(1.12f * PX, 1.6f * PX, 0.16f * PX), i % 2 == 0 ? _hair : _hairD);
        }
        Block(_head, "sideburnL", new Vector3(-3.7f * PX, 2.2f * PX, 3.8f * PX),
            new Vector3(0.8f * PX, 2.6f * PX, 0.8f * PX), _hairD);
        Block(_head, "sideburnR", new Vector3(3.7f * PX, 2.2f * PX, 3.8f * PX),
            new Vector3(0.8f * PX, 2.6f * PX, 0.8f * PX), _hairD);

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

    static void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform c in go.transform) SetLayer(c.gameObject, layer);
    }
}