using UnityEngine;

// 摩天轮吊舱：绕轮心公转，但保持正立（不随轮缘翻转）
public class GondolaRing : MonoBehaviour
{
    public float Radius = 8.2f;
    public float DegPerSec = 6f;

    float _ang;

    void Update()
    {
        _ang += DegPerSec * Mathf.Deg2Rad * Time.deltaTime;
        int n = transform.childCount;
        for (int i = 0; i < n; i++)
        {
            float a = _ang + i * Mathf.PI * 2f / n;
            var c = transform.GetChild(i);
            c.localPosition = new Vector3(0f, Mathf.Cos(a) * Radius - 0.5f, Mathf.Sin(a) * Radius);
            c.localRotation = Quaternion.identity;
        }
    }
}
