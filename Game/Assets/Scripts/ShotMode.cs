using System.Collections;
using UnityEngine;

// 截图验收（对应 web 版无头 Edge 截图流程）：
//   Campus.exe -shot shots/day.png -time 18.5 -px 0 -pz 4 -at -21,7.5 -freeze 1 -frames 12
// 截屏后自动退出（编辑器播放模式下则停止播放）
public class ShotMode : MonoBehaviour
{
    string _path;
    int _frames = 12;

    public void Init()
    {
        _path = Args.Str("-shot");
        if (string.IsNullOrEmpty(_path)) { enabled = false; return; }
        var f = Args.I("-frames");
        if (f.HasValue) _frames = f.Value;
        StartCoroutine(DoShot());
    }

    IEnumerator DoShot()
    {
        // 等待渲染稳定（天空/贴图加载 + 相机吸附帧）
        for (int i = 0; i < _frames; i++) yield return new WaitForEndOfFrame();
        // 记录玩家/相机世界坐标：让 -sim 之类的动态验收可以做数值断言（查 Player.log）
        var wb = GetComponent<WorldBuilder>();
        if (wb != null && wb.Player != null)
        {
            var cam = Camera.main;
            Debug.Log($"[ShotMode] player=({wb.Player.position.x:F2},{wb.Player.position.y:F2},{wb.Player.position.z:F2}) cam={(cam != null ? cam.transform.position.ToString("F2") : "null")}");
            var rig = GetComponent<PlayerRig>();
            if (rig != null) Debug.Log($"[ShotMode] yaw={rig.Yaw:F3}");
        }
        string abs = _path;
        if (!System.IO.Path.IsPathRooted(abs)) abs = System.IO.Path.GetFullPath(abs);
        var dir = System.IO.Path.GetDirectoryName(abs);
        if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
        ScreenCapture.CaptureScreenshot(abs);
        for (int i = 0; i < 6; i++) yield return new WaitForEndOfFrame();
        Debug.Log("[ShotMode] 截图已保存: " + abs);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(0);
#endif
    }
}
