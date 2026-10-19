using UnityEngine;

// 河水流动：沿河道方向缓慢滚动贴图 U 轴
public class WaterFlow : MonoBehaviour
{
    public float Speed = 0.045f;   // UV 每秒滚动量（4m 贴图 → 约 0.18m/s）
    Material _mat;

    void Start()
    {
        var r = GetComponent<MeshRenderer>();
        if (r != null) _mat = r.material;   // 实例化，避免改到共享材质
    }

    void Update()
    {
        if (_mat == null) return;
        var off = _mat.mainTextureOffset;
        off.x -= Speed * Time.deltaTime;
        _mat.mainTextureOffset = off;
    }
}
