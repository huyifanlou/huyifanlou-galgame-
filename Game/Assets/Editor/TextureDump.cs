using System.IO;
using UnityEditor;
using UnityEngine;

// C# 贴图对拍导出入口（Editor）：导出 TextureLibrary 生成的全部贴图供与 JS 版逐像素比对
public static class TextureDump
{
    [MenuItem("Campus/导出 C# 生成贴图（对拍用）")]
    public static void Dump()
    {
        var dir = Path.Combine(Application.dataPath, "..", "Build", "gen_tex");
        TextureLibrary.DumpAll(dir);
    }
    public static void DumpBatch() { Dump(); }
}
