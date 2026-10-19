using UnityEngine;

// 浮力（弹簧跟随水面）：小船/木箱/落叶按水面高度上下浮动，并叠加顺流拖拽。
// 用弹簧而不是"排水体积"是为了稳定：FloatHeight = 物体中心的目标高度（相对静水面），
// 刚度抵消重力后实际会低约 g/k，故取值略高于期望吃水。
[RequireComponent(typeof(Rigidbody))]
public class Buoyant : MonoBehaviour
{
    public float FloatHeight = 0.28f;    // 目标中心高度（相对水面）
    public float Stiffness = 60f;
    public float Damping = 7f;
    public float WaterDrag = 1.6f;

    Rigidbody _rb;
    WaterSim _water;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _water = WaterSim.I;
    }

    void FixedUpdate()
    {
        if (_water == null) return;
        Vector3 p = _rb.position;
        float surf = _water.SampleHeight(p.x, p.z);
        float velY = _rb.velocity.y;
        float ay = (surf + FloatHeight - p.y) * Stiffness - velY * Damping;
        ay = Mathf.Clamp(ay, -30f, 80f);
        _rb.AddForce(Vector3.up * (ay * _rb.mass), ForceMode.Force);

        Vector3 cur = _water.SampleCurrent(p.x, p.z);
        Vector3 vel = _rb.velocity;
        _rb.AddForce(new Vector3(cur.x - vel.x, 0f, cur.z - vel.z) * (WaterDrag * _rb.mass), ForceMode.Force);
    }
}
