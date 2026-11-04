using System;

// 命令行验收参数解析（对应 web 版的 URL 查询参数）
// 用法示例：Campus.exe -time 18.5 -px 0 -pz 4 -at -21,7.5 -freeze 1 -shot shots/night.png
public static class Args
{
    static string[] A => Environment.GetCommandLineArgs();

    public static bool Has(string key) => Array.IndexOf(A, key) >= 0;

    // 干净画面（拍 logo 底图 / 宣传图用）：隐藏 HUD 与小地图
    static bool? _noHud;
    public static bool NoHud => _noHud ??= Has("-nohud");

    public static string Str(string key)
    {
        var a = A;
        int i = Array.IndexOf(a, key);
        if (i < 0 || i + 1 >= a.Length) return null;
        return a[i + 1];
    }

    public static float? F(string key)
    {
        var s = Str(key);
        if (string.IsNullOrEmpty(s)) return null;
        return float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : (float?)null;
    }

    public static int? I(string key)
    {
        var s = Str(key);
        if (string.IsNullOrEmpty(s)) return null;
        return int.TryParse(s, out var v) ? v : (int?)null;
    }
}
