using UnityEngine;

// 相机贴近时隐藏整棵树的渲染（防止相机钻进树冠看到一团黑），离开后恢复
public class ProximityHide : MonoBehaviour
{
    public float Radius = 3.2f;
    Renderer[] _rs;

    void Start()
    {
        _rs = GetComponentsInChildren<Renderer>(true);
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || _rs == null) return;
        bool hide = (transform.position - cam.transform.position).sqrMagnitude < Radius * Radius;
        for (int i = 0; i < _rs.Length; i++)
            if (_rs[i].enabled == hide) _rs[i].enabled = !hide;
    }
}
