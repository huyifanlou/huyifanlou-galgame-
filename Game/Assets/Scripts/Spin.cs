using UnityEngine;

// 游乐设施缓慢旋转（摩天轮/旋转木马/飞椅）
public class Spin : MonoBehaviour
{
    public Vector3 Axis = Vector3.right;   // 摩天轮绕 X 轴（轮面朝主街）
    public float DegPerSec = 8f;

    void Update()
    {
        transform.Rotate(Axis, DegPerSec * Time.deltaTime, Space.Self);
    }
}
