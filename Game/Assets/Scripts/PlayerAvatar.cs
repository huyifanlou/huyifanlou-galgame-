using UnityEngine;

// 程序化 3D 低模学生角色：替代原 4×4 像素精灵表。
// 外形按原精灵配色：金发、藏青校服、白衬衫、红领带、深蓝裤、黑鞋。
// 只做视觉与动画，不挂碰撞；碰撞仍由 Player 上的 CapsuleCollider 负责。
public class PlayerAvatar : MonoBehaviour
{
    Transform _model, _body, _torso, _head, _legL, _legR, _armL, _armR;
    Material _skin, _skinD, _hair, _hairD, _coat, _coatD, _shirt, _tie, _pants, _pantsD, _shoe, _belt, _eye, _eyeW;
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
        _skinD = MatLib.LitColor(MatLib.Hex("#e0c193"));
        _hair = MatLib.LitColor(MatLib.Hex("#eccb5a"));
        _hairD = MatLib.LitColor(MatLib.Hex("#c9a234"));
        _coat = MatLib.LitColor(MatLib.Hex("#46567f"));
        _coatD = MatLib.LitColor(MatLib.Hex("#333f5e"));
        _shirt = MatLib.LitColor(MatLib.Hex("#f4f1e6"));
        _tie = MatLib.LitColor(MatLib.Hex("#b23a44"));
        _pants = MatLib.LitColor(MatLib.Hex("#2e3550"));
        _pantsD = MatLib.LitColor(MatLib.Hex("#232838"));
        _shoe = MatLib.LitColor(MatLib.Hex("#23262e"));
        _belt = MatLib.LitColor(MatLib.Hex("#2a2d3a"));
        _eye = MatLib.LitColor(MatLib.Hex("#2b3a58"));
        _eyeW = MatLib.LitColor(MatLib.Hex("#f8f6ee"));

        _model = Node(transform, "model", Vector3.zero);
        _body = Node(_model, "body", new Vector3(0f, 0.82f, 0f));

        _legL = Node(_body, "legL", new Vector3(-0.095f, 0f, 0f));
        _legR = Node(_body, "legR", new Vector3(0.095f, 0f, 0f));
        Leg(_legL);
        Leg(_legR);

        _torso = Node(_body, "torso", Vector3.zero);
        Torso();
        _head = Node(_torso, "head", new Vector3(0f, 0.72f, 0f));
        Head();

        _armL = Node(_torso, "armL", new Vector3(-0.235f, 0.50f, 0f));
        _armR = Node(_torso, "armR", new Vector3(0.235f, 0.50f, 0f));
        Arm(_armL);
        Arm(_armR);

        SetLayer(_model.gameObject, 2);
    }

    // ── 结构 ─────────────────────────────────────────────
    void Leg(Transform p)
    {
        Box(p, "pant", new Vector3(0f, -0.37f, 0f), new Vector3(0.145f, 0.72f, 0.165f), _pants);
        Box(p, "cuff", new Vector3(0f, -0.72f, 0f), new Vector3(0.15f, 0.08f, 0.17f), _pantsD);
        Box(p, "shoe", new Vector3(0f, -0.785f, 0.03f), new Vector3(0.155f, 0.07f, 0.24f), _shoe);
        Box(p, "toe", new Vector3(0f, -0.80f, 0.12f), new Vector3(0.15f, 0.045f, 0.09f), _pantsD);
    }

    void Torso()
    {
        Box(_torso, "hip", new Vector3(0f, 0.03f, 0f), new Vector3(0.30f, 0.17f, 0.20f), _pants);
        Box(_torso, "belt", new Vector3(0f, 0.12f, 0f), new Vector3(0.32f, 0.05f, 0.21f), _belt);
        Box(_torso, "buckle", new Vector3(0f, 0.12f, 0.11f), new Vector3(0.055f, 0.045f, 0.02f), _skinD);
        Taper(_torso, "jacket", new Vector3(0f, 0.36f, 0f), 0.34f, 0.21f, 0.38f, 0.23f, 0.50f, _coat);
        Box(_torso, "hem", new Vector3(0f, 0.13f, 0f), new Vector3(0.355f, 0.06f, 0.22f), _coatD);
        Box(_torso, "shirt", new Vector3(0f, 0.50f, 0.108f), new Vector3(0.17f, 0.20f, 0.03f), _shirt);
        Box(_torso, "tieKnot", new Vector3(0f, 0.565f, 0.121f), new Vector3(0.075f, 0.06f, 0.03f), _tie);
        Taper(_torso, "tie", new Vector3(0f, 0.44f, 0.122f), 0.06f, 0.018f, 0.11f, 0.018f, 0.20f, _tie);
        Box(_torso, "lapelL", new Vector3(-0.088f, 0.50f, 0.105f), new Vector3(0.07f, 0.25f, 0.022f),
            _coatD, new Vector3(0f, 0f, -16f));
        Box(_torso, "lapelR", new Vector3(0.088f, 0.50f, 0.105f), new Vector3(0.07f, 0.25f, 0.022f),
            _coatD, new Vector3(0f, 0f, 16f));
        Box(_torso, "collar", new Vector3(0f, 0.615f, 0.09f), new Vector3(0.21f, 0.05f, 0.07f), _shirt);
        Box(_torso, "btn", new Vector3(0f, 0.30f, 0.118f), new Vector3(0.035f, 0.035f, 0.02f), _skinD);
        Ball(_torso, "shoulderL", new Vector3(-0.215f, 0.52f, 0f), 0.085f, _coat, new Vector3(1f, 0.8f, 1f));
        Ball(_torso, "shoulderR", new Vector3(0.215f, 0.52f, 0f), 0.085f, _coat, new Vector3(1f, 0.8f, 1f));
    }

    void Head()
    {
        Box(_head, "neck", new Vector3(0f, -0.02f, 0f), new Vector3(0.095f, 0.10f, 0.10f), _skin);
        Ball(_head, "face", new Vector3(0f, 0.10f, 0.015f), 0.155f, _skin, new Vector3(0.95f, 1.0f, 0.95f));
        Ball(_head, "hair", new Vector3(0f, 0.13f, -0.045f), 0.17f, _hair, new Vector3(1.0f, 0.96f, 1.0f));
        Box(_head, "hairNape", new Vector3(0f, 0.02f, -0.105f), new Vector3(0.23f, 0.17f, 0.08f), _hair);
        for (int i = -2; i <= 2; i++)
            Box(_head, "bang" + i, new Vector3(i * 0.055f, 0.175f, 0.112f - Mathf.Abs(i) * 0.012f),
                new Vector3(0.058f, 0.085f, 0.05f), _hair);
        Box(_head, "sideL", new Vector3(-0.145f, 0.05f, 0.02f), new Vector3(0.055f, 0.20f, 0.12f), _hair);
        Box(_head, "sideR", new Vector3(0.145f, 0.05f, 0.02f), new Vector3(0.055f, 0.20f, 0.12f), _hair);
        Box(_head, "cowlick", new Vector3(-0.035f, 0.24f, 0.02f), new Vector3(0.075f, 0.075f, 0.06f),
            _hairD, new Vector3(0f, 0f, -22f));

        Box(_head, "eyeWL", new Vector3(-0.058f, 0.085f, 0.145f), new Vector3(0.055f, 0.062f, 0.02f), _eyeW);
        Box(_head, "eyeWR", new Vector3(0.058f, 0.085f, 0.145f), new Vector3(0.055f, 0.062f, 0.02f), _eyeW);
        Box(_head, "pupilL", new Vector3(-0.058f, 0.082f, 0.158f), new Vector3(0.028f, 0.046f, 0.012f), _eye);
        Box(_head, "pupilR", new Vector3(0.058f, 0.082f, 0.158f), new Vector3(0.028f, 0.046f, 0.012f), _eye);
        Box(_head, "browL", new Vector3(-0.058f, 0.15f, 0.146f), new Vector3(0.06f, 0.018f, 0.02f), _hairD);
        Box(_head, "browR", new Vector3(0.058f, 0.15f, 0.146f), new Vector3(0.06f, 0.018f, 0.02f), _hairD);
        Box(_head, "mouth", new Vector3(0f, 0.025f, 0.15f), new Vector3(0.05f, 0.014f, 0.02f), _skinD);
        Box(_head, "nose", new Vector3(0f, 0.06f, 0.16f), new Vector3(0.022f, 0.03f, 0.02f), _skinD);
        Ball(_head, "earL", new Vector3(-0.148f, 0.09f, 0f), 0.035f, _skin, new Vector3(0.55f, 1f, 0.8f));
        Ball(_head, "earR", new Vector3(0.148f, 0.09f, 0f), 0.035f, _skin, new Vector3(0.55f, 1f, 0.8f));
    }

    void Arm(Transform p)
    {
        Box(p, "sleeve", new Vector3(0f, -0.14f, 0f), new Vector3(0.115f, 0.28f, 0.14f), _coatD);
        Box(p, "cuff", new Vector3(0f, -0.285f, 0f), new Vector3(0.115f, 0.045f, 0.14f), _coatD);
        Box(p, "fore", new Vector3(0f, -0.38f, 0f), new Vector3(0.095f, 0.18f, 0.105f), _skin);
        Ball(p, "hand", new Vector3(0f, -0.485f, 0.01f), 0.062f, _skin, new Vector3(0.9f, 0.8f, 0.9f));
    }

    // ── 动画 ─────────────────────────────────────────────
    public void Tick(bool moving, float facing, float dt)
    {
        if (_model == null) return;
        transform.localRotation = Quaternion.Euler(0f, facing * Mathf.Rad2Deg, 0f);
        _phase += dt * (moving ? 10.5f : 2.2f);
        _blend = Mathf.MoveTowards(_blend, moving ? 1f : 0f, dt * 6f);

        float sw = Mathf.Sin(_phase) * _blend;
        float idle = Mathf.Sin(Time.time * 1.6f) * (1f - _blend);
        _legL.localRotation = Quaternion.Euler(sw * 32f, 0f, 0f);
        _legR.localRotation = Quaternion.Euler(-sw * 32f, 0f, 0f);
        _armL.localRotation = Quaternion.Euler(-sw * 26f + idle * 3f, 0f, 0f);
        _armR.localRotation = Quaternion.Euler(sw * 26f - idle * 3f, 0f, 0f);

        float bob = Mathf.Abs(Mathf.Sin(_phase)) * 0.028f * _blend;
        float breath = Mathf.Sin(Time.time * 2.1f) * 0.006f * (1f - _blend);
        _body.localPosition = new Vector3(0f, 0.82f + bob + breath, 0f);
        _torso.localRotation = Quaternion.Euler(6f * _blend, 0f, 0f);
        _head.localRotation = Quaternion.Euler(-4f * _blend + idle * 2.2f, 0f, 0f);
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

    static void Box(Transform p, string name, Vector3 pos, Vector3 size, Material mat, Vector3 euler = default)
    {
        var go = new GameObject(name);
        go.layer = 2;
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.BoxUV(size, 0.4f);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void Taper(Transform p, string name, Vector3 pos, float bw, float bd, float tw, float td,
                      float h, Material mat)
    {
        var go = new GameObject(name);
        go.layer = 2;
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.TaperedBox(bw, bd, tw, td, h, 0.4f);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void Ball(Transform p, string name, Vector3 pos, float r, Material mat, Vector3 scale = default)
    {
        var go = new GameObject(name);
        go.layer = 2;
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        if (scale != Vector3.zero) go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Sphere(r, 10, 8);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform c in go.transform) SetLayer(c.gameObject, layer);
    }
}