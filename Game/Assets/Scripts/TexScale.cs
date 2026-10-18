using System.Collections.Generic;
using UnityEngine;

// 贴地/贴面纹理的世界尺度策略（密度铁律 · 单一来源）
//   密度 = 贴图物理像素 ÷ 覆盖的世界米数；基准 160px/米（10.4cm 格 ≈ 16.6px）
//   平铺次数一律由这里给出，WorldBuilder 与 WorldValidator 都取这一份：
//   改密度只动「本文件的周期」+「TextureLibrary 的贴图像素尺寸」，两侧脱钩会被校验器抓住。
public static class TexScale
{
    public const float Density = 160f;          // 基准密度（px/米）
    public const float GrassPeriod = 1.667f;    // 草地 256px → 153.6px/米（容差带内）
    public const float PathPeriod = 0.8f;       // 次路/岔路/广场/路口铺装 128px → 160px/米
    public const float MainstonePeriod = 1.6f;  // 主路石板 256px → 160px/米（4×4 石板，单块 0.4m）
    public const float CobblePeriod = 0.8f;     // 家属区小石路 128px → 160px/米
    public const float LeafMeters = 0.8f;       // 树冠叶簇 128px → 160px/米（BoxUV/Cone 按此平铺）
    public const float BarkMeters = 0.6f;       // 树皮 96px → 160px/米

    public static float RoadPeriod(string kind) => kind == "mainstone" ? MainstonePeriod : CobblePeriod;

    // 按周期精确平铺（不取整：取整会把实际密度带偏 ±4%，路宽 4.6m 时尤其明显）
    public static Vector2 Repeat(float w, float d, float period) => new Vector2(w / period, d / period);

    // 密度登记表：贴图名 → 整张贴图覆盖的 (宽, 高) 米数；校验器据此算 px/米
    // 「基准组」（grass/path/road_*）必须落在 160px/米 ±6%，其余为登记值并逐张打印，便于看还差多少
    public static readonly Dictionary<string, Vector2> Cover = new Dictionary<string, Vector2>
    {
        ["grass"] = new Vector2(GrassPeriod, GrassPeriod),
        ["path"] = new Vector2(PathPeriod, PathPeriod),
        ["road_mainstone"] = new Vector2(MainstonePeriod, MainstonePeriod),
        ["road_cobble"] = new Vector2(CobblePeriod, CobblePeriod),
        ["track"] = new Vector2(16f, 10f),          // 操场：整张铺满（尺寸取自 GROUNDS.playground）
        ["court_basket"] = new Vector2(15f, 9f),    // 篮球场 / 网球场：整张铺满
        ["court_tennis"] = new Vector2(15f, 8f),
        ["pond"] = new Vector2(7.2f, 7.2f),         // 池塘：(r + 0.6) × 2
        ["villa"] = new Vector2(8.4f, 5.4f),        // 别墅立面：箱体正面 w×h（贴图随面拉伸）
        ["door"] = new Vector2(1.5f, 2.1f),         // 大门面片（门洞）
        ["flower"] = new Vector2(1.7f, 1.7f),       // 花坛 / 土斑：整张铺满
        ["dirt"] = new Vector2(4.8f, 4.8f),
        ["roadband_v"] = new Vector2(0.6f, 1f),     // 中线石带：横 0.6m 拉伸一次、纵向 1m 一铺
        ["roadband_h"] = new Vector2(1f, 0.6f),
        ["leaves"] = new Vector2(LeafMeters, LeafMeters),   // 树冠（世界尺度 UV 箱体/锥体）
        ["bark"] = new Vector2(BarkMeters, BarkMeters),     // 树干
        ["concrete"] = new Vector2(2.4f, 2.4f),             // 矮墙/立柱：384px → 160px/米（含每 2.4m 砌筑缝）
    };
}
