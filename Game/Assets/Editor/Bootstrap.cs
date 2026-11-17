using System.IO;
using UnityEditor;
using UnityEditor.Build;
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
        SetupTextures();

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
            "Campus/Toon",              // 2026-11-10 卡通渲染主着色器
            "Campus/ToonPostFX",        // 卡通后处理
            "Campus/PostFX",            // 旧写实后处理（保留占位，便于回退对比）
            "Campus/Water",
            "Campus/TextSign",
            "Skybox/Procedural",
        };
        foreach (var sn in shaderNames)
        {
            var sh = Shader.Find(sn);
            if (sh == null) { Debug.LogError("[Bootstrap] 找不到 Shader: " + sn); continue; }
            var assetPath = holderDir + "/" + sn.Replace("/", "_").Replace(" ", "_") + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            // Standard 占位材质配成 Cutout：让 _ALPHATEST_ON 变体被编进构建
            // （运行时才 EnableKeyword 的材质，若构建里没有该变体，裁剪会静默失效 → 透明区渲染成黑块）
            if (sn == "Standard")
            {
                mat.SetFloat("_Mode", 1f);
                mat.SetFloat("_Cutoff", 0.4f);
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 2450;
                EditorUtility.SetDirty(mat);
            }
        }
        // 额外占位：Standard 的 _EMISSION 变体（夜间窗户/灯头发光）
        var emPath = holderDir + "/StandardEmission.mat";
        var em = AssetDatabase.LoadAssetAtPath<Material>(emPath);
        if (em == null)
        {
            em = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(em, emPath);
        }
        em.EnableKeyword("_EMISSION");
        em.SetColor("_EmissionColor", Color.white);
        em.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(em);

        // 卡通渲染的占位：让 _EMISSION 与 _ALPHATEST_ON 两个变体都被编进构建
        // （运行时才 EnableKeyword 的材质，若构建里没有该变体，会静默失效 → 夜里窗户不亮 / 精灵不裁剪）
        var toonPath = holderDir + "/Campus_Toon.mat";
        var toonMat = AssetDatabase.LoadAssetAtPath<Material>(toonPath);
        if (toonMat == null)
        {
            toonMat = new Material(Shader.Find("Campus/Toon"));
            AssetDatabase.CreateAsset(toonMat, toonPath);
        }
        toonMat.EnableKeyword("_EMISSION");
        toonMat.SetColor("_EmissionColor", Color.white);
        toonMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        toonMat.EnableKeyword("_ALPHATEST_ON");
        toonMat.SetFloat("_Cutoff", 0.4f);
        EditorUtility.SetDirty(toonMat);

        // 带 _ALPHATEST_ON 的第二份（精灵/Cutout 用）：Unity 的变体收集按材质关键字决定，
        // 一份材质没法同时保证两个变体都在，所以再来一份只开 ALPHATEST 的。
        var toonClipPath = holderDir + "/Campus_ToonCutout.mat";
        var toonClip = AssetDatabase.LoadAssetAtPath<Material>(toonClipPath);
        if (toonClip == null)
        {
            toonClip = new Material(Shader.Find("Campus/Toon"));
            AssetDatabase.CreateAsset(toonClip, toonClipPath);
        }
        toonClip.DisableKeyword("_EMISSION");
        toonClip.EnableKeyword("_ALPHATEST_ON");
        toonClip.SetFloat("_Cutoff", 0.4f);
        toonClip.renderQueue = 2450;
        EditorUtility.SetDirty(toonClip);

        AssetDatabase.SaveAssets();

        AssetDatabase.SaveAssets();
        Debug.Log("[Bootstrap] Shader 占位材质完成（Standard 已按 Cutout 变体配置，Toon 已备 EMISSION/ALPHATEST 变体）");

        // ── 2) 项目设置 ──
        PlayerSettings.productName = "他山之石";
        PlayerSettings.companyName = "huyifanlou";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.runInBackground = true;
        PlayerSettings.colorSpace = ColorSpace.Linear;   // 写实光照：线性空间 + HDR + ACES
        QualitySettings.antiAliasing = 4;
        QualitySettings.shadowDistance = 140f;
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

    // 应用图标（窗口/任务栏/桌面快捷方式共用）：Assets/icon.png → PlayerSettings
    [MenuItem("Campus/1c. 设置应用图标")]
    public static void SetAppIcon()
    {
        const string path = "Assets/icon.png";
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogError("[Bootstrap] 缺少 Assets/icon.png"); return; }
        ti.textureType = TextureImporterType.Default;
        ti.mipmapEnabled = false;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.maxTextureSize = 256;
        ti.isReadable = true;
        ti.SaveAndReimport();
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) { Debug.LogError("[Bootstrap] icon.png 导入失败"); return; }
        // 2022 的新 API（旧 SetIconsForTargetGroup 在批量模式下不落盘）
        // 平台图标槽位是 8 个（16~256），全部填同一张图，Windows 会按需要缩放
        PlayerSettings.SetIcons(NamedBuildTarget.Standalone,
            new[] { tex, tex, tex, tex, tex, tex, tex, tex }, IconKind.Application);
        AssetDatabase.SaveAssets();     // 批量模式下必须显式落盘，否则 ProjectSettings 不会写进去
        var back = PlayerSettings.GetIcons(NamedBuildTarget.Standalone, IconKind.Application);
        Debug.Log($"[Bootstrap] 应用图标已设置：回读 {back?.Length ?? 0} 张，首张={(back != null && back.Length > 0 && back[0] != null ? back[0].name : "null")}");
        WriteWindowsIcon(tex);
    }
    static void WriteWindowsIcon(Texture2D source)
    {
        int[] sizes={16,24,32,48,64,96,128,256};var images=new byte[sizes.Length][];
        for(int i=0;i<sizes.Length;i++)
        {
            int size=sizes[i];var icon=new Texture2D(size,size,TextureFormat.RGBA32,false);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)icon.SetPixel(x,y,source.GetPixelBilinear((x+.5f)/size,(y+.5f)/size));
            icon.Apply();images[i]=icon.EncodeToPNG();Object.DestroyImmediate(icon);
        }
        Directory.CreateDirectory("Build/Win");
        using(var file=File.Create("Build/Win/jingying-profile.ico"))using(var writer=new BinaryWriter(file))
        {
            writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;
            for(int i=0;i<sizes.Length;i++){writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(images[i].Length);writer.Write(offset);offset+=images[i].Length;}
            foreach(var bytes in images)writer.Write(bytes);
        }
        Debug.Log("[Bootstrap] Desktop icon generated: Build/Win/jingying-profile.ico / 8 sizes");
    }
    public static void SetAppIconBatch() => SetAppIcon();

    // 贴图导入设置（像素无损 + mipmap + 可读）
    //   isReadable 必须开：招牌 PNG 运行时靠 GetPixel 做 2× 最近邻放大（对齐全局密度），
    //   不可读时玩家构建会在 WorldBuilder 里抛异常、世界只建一半（2026-10-16 实测踩到）
    [MenuItem("Campus/1b. 重设贴图导入（可读）")]
    public static void SetupTextures()
    {
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
            ti.isReadable = true;
            ti.SaveAndReimport();
        }
        Debug.Log($"[Bootstrap] 贴图导入设置完成：{guids.Length} 张（含 isReadable）");
    }
    public static void SetupTexturesBatch() => SetupTextures();

    // 构建 Windows 播放器（截图验收 / 本地演示；输出可双击运行）
    public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Build/Win/Campus.exe");
    public static void BuildWindowsBatch() => BuildWindows();

    static void ApplyWindowSettings()
    {
        PlayerSettings.resizableWindow = true;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.colorSpace = ColorSpace.Linear;   // 每次构建都确保线性空间（写实光照）
        PlayerSettings.SplashScreen.show=false;
        SetAppIcon();
        Debug.Log($"[Bootstrap] Startup splash={PlayerSettings.SplashScreen.show}; heroine icon applied");
        var title=AssetImporter.GetAtPath("Assets/Resources/Title/JingyingDusk.png") as TextureImporter;
        if(title!=null){title.textureType=TextureImporterType.Default;title.mipmapEnabled=false;title.npotScale=TextureImporterNPOTScale.None;title.textureCompression=TextureImporterCompression.Uncompressed;title.maxTextureSize=2048;title.filterMode=FilterMode.Bilinear;title.SaveAndReimport();}
        SetupBundledFonts();
        SetupPortraitTextures();
        SyncScenario();
        AssetDatabase.SaveAssets();
    }

    // 剧本同步：把仓库 Scenario/第一卷-初遇 的 8 章正文拷进 StreamingAssets/Scenario（运行时 File.ReadAllText 读）。
    // 源文件不动，这里只是构建期拷贝；每次构建自动保鲜，也可单独跑 SyncScenarioBatch。
    [MenuItem("Campus/1d. 同步剧本到 StreamingAssets")]
    public static void SyncScenario()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        var src = Path.Combine(repoRoot, "Scenario", "第一卷-初遇");
        var dst = Path.Combine(Application.dataPath, "StreamingAssets", "Scenario");
        if (!Directory.Exists(src)) { Debug.LogWarning("[Bootstrap] 剧本源目录不存在：" + src); return; }
        Directory.CreateDirectory(dst);
        int n = 0;
        foreach (var f in Directory.GetFiles(src, "0*.md"))
        {
            File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
            n++;
        }
        AssetDatabase.Refresh();
        Debug.Log($"[Bootstrap] 剧本同步完成：{n} 章 → Assets/StreamingAssets/Scenario");
    }
    public static void SyncScenarioBatch() => SyncScenario();

    static void SetupBundledFonts()
    {
        foreach(string path in new[]{"Assets/Resources/Fonts/MapSans.ttf","Assets/Resources/Fonts/StorySerif.ttf","Assets/Resources/Fonts/MapHeading.ttf"})
        {var importer=AssetImporter.GetAtPath(path) as TrueTypeFontImporter;if(importer==null)throw new System.Exception("Missing font "+path);importer.fontTextureCase=FontTextureCase.Dynamic;importer.includeFontData=true;importer.fontRenderingMode=FontRenderingMode.Smooth;importer.SaveAndReimport();}
        Debug.Log("[Fonts] bundled map sans and selectable story serif imported");
    }

    // UI 立绘：保留透明边缘，禁止通用贴图的压缩、mipmap 与非等比缩放。
    // 在每次构建前统一执行，重新入库立绘也不会带回白边（docs/107）。
    static void SetupPortraitTextures()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Portraits" });
        int count = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith("-cut.png", System.StringComparison.OrdinalIgnoreCase)) continue;
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            bool changed = ti.mipmapEnabled || !ti.alphaIsTransparency
                || ti.textureCompression != TextureImporterCompression.Uncompressed
                || ti.npotScale != TextureImporterNPOTScale.None
                || ti.filterMode != FilterMode.Bilinear || ti.wrapMode != TextureWrapMode.Clamp;
            ti.textureType = TextureImporterType.Default;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.alphaIsTransparency = true;
            ti.maxTextureSize = 2048;
            if (changed) ti.SaveAndReimport();
            else AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            count++;
        }
        Debug.Log($"[Bootstrap] 透明立绘导入设置完成：{count} 张（无压缩 / 无 mipmap / 透明边缘）");
        StandeeAssets.BuildBatch(); // Updating artwork must also refresh UI/world alpha framing.
    }

    // 构建 WebGL（部署上线用）
    public static void BuildWebGL() => Build(BuildTarget.WebGL, "Build/WebGL");
    public static void BuildWebGLBatch() => BuildWebGL();

    static void Build(BuildTarget target, string outPath)
    {
        var dir = Path.GetDirectoryName(outPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        ApplyWindowSettings();
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
