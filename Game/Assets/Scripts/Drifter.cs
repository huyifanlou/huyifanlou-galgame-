using UnityEngine;

// 顺流漂木：漂到下游尽头后回到上游（保持河面一直有流动参照物）
public class Drifter : MonoBehaviour
{
    public float StartX = -103f;
    public float EndX = 103f;

    Rigidbody _rb;

    void Start() { _rb = GetComponent<Rigidbody>(); }

    void FixedUpdate()
    {
        if (_rb == null || WaterSim.I == null) return;
        if (transform.position.x <= EndX) return;
        var p = transform.position;
        p.x = StartX;
        p.z = WaterSim.I.River.offset + WaterSim.I.River.amp *
              Mathf.Sin((StartX + WaterSim.I.River.phase) / WaterSim.I.River.freq);
        p.y = WaterSim.I.Level - 0.1f;
        transform.position = p;
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }
}
