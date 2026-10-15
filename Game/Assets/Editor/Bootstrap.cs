using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// 工程初始化 / 构建入口（命令行：Unity.exe -batchmode -projectPath <Game> -executeMethod Bootstrap.xxxBatch -quit）
public static class Bootstrap
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Campus/1. 初始化工程（贴图导入设置 + Main 场景）")]
    public static void SetupProject()
    {
        // ── 1) 贴图导入：像素无损 + mipmap（是否采样 mip 由运行时 filterMode 决定）──
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/tex" });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Default;
            ti.textureCompression = TextureImporterCompression.Uncompressed;   // 像素画严禁有损压缩
            ti.mipmapEnabled = true;
            ti.filterMode = FilterMode.Point;
            ti.anisoLevel = 9;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = 2048;
            ti.SaveAndReimport();
        }
        Debug.Log($"[Bootstrap] 贴图导入设置完成：{guids.Length} 张");

        // ── 1.5) Shader 占位：运行时用 Shader.Find 建材质，构建时若无引用会被剥离（全黑）──
        // 在 Resources 放一份材质占位 → 其 Shader 会被打进构建
        var holderDir = "Assets/Resources/mat_holder";
        Directory.CreateDirectory(holderDir);
        string[] shaderNames =
        {
            "Standard",
            "Unlit/Color",
            "Unlit/Texture",
            "Legacy Shaders/Particles/Alpha Blended",
            "Legacy Shaders/Particles/Additive",
            "Legacy Shaders/Transparent/Diffuse",
        };
        foreach (var sn in shaderNames)
        {
            var sh = Shader.Find(sn);
            if (sh == null) { Debug.LogError("[Bootstrap] 找不到 Shader: " + sn); continue; }
            var assetPath = holderDir + "/" + sn.Replace("/", "_").Replace(" ", "_") + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(assetPath) == null)
                AssetDatabase.CreateAsset(new Material(sh), assetPath);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Bootstrap] Shader 占位材质完成");

        // ── 2) 项目设置 ──
        PlayerSettings.productName = "校园叙事 RPG";
        PlayerSettings.companyName = "huyifanlou";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.runInBackground = true;
        PlayerSettings.colorSpace = ColorSpace.Gamma;   // 与 web 版 sRGB 直出观感一致（首屏后按截图校准）
        QualitySettings.antiAliasing = 4;
        QualitySettings.shadowDistance = 70f;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.vSyncCount = 0;

        // ── 3) Main 场景（相机 + Bootstrap 组件，世界由 WorldBuilder 运行时构建）──
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = MatLib.Hex("#10141c");
        cam.fieldOfView = 46f;
        cam.nearClipPlane = 0.5f;
        cam.farClipPlane = 260f;
        camGo.AddComponent<AudioListener>();

        var boot = new GameObject("Bootstrap");
        boot.AddComponent<WorldBuilder>();
        boot.AddComponent<TimeOfDay>();
        boot.AddComponent<PlayerRig>();
        boot.AddComponent<Hud>();
        boot.AddComponent<ShotMode>();

        // 环境：与 web 版一致——无天空盒、平坦环境光（数值由 TimeOfDay 逐帧驱动）
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.skybox = null;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = null;

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[Bootstrap] Main 场景已生成并加入 Build Settings");
    }

    public static void SetupProjectBatch() => SetupProject();

    // 构建 Windows 播放器（截图验收 / 本地演示；输出可双击运行）
    public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Build/Win/Campus.exe");
    public static void BuildWindowsBatch() => BuildWindows();

    // 构建 WebGL（部署上线用）
    public static void BuildWebGL() => Build(BuildTarget.WebGL, "Build/WebGL");
    public static void BuildWebGLBatch() => BuildWebGL();

    static void Build(BuildTarget target, string outPath)
    {
        var dir = Path.GetDirectoryName(outPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            target = target,
            locationPathName = outPath,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log($"[Bootstrap] 构建 {target}：{report.summary.result} → {outPath}（{report.summary.totalSize / 1024 / 1024}MB）");
        if (report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
