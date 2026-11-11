using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// 贴地/贴面纹理的世界尺度策略（密度铁律 · 单一来源）
//   密度 = 贴图物理像素 ÷ 覆盖的世界米数；基准 160px/米（10.4cm 格 ≈ 16.6px）
//   平铺次数一律由这里给出，WorldBuilder 与 WorldValidator 都取这一份：
//   改密度只动「本文件的周期」+「TextureLibrary 的贴图像素尺寸」，两侧脱钩会被校验器抓住。
//
// 2026-11-10 卡通重做：贴图尺寸按卡通画法重定（不再用 256px 大噪点图），
// 周期随之调整以维持 160px/米。校验器 baselineTex 组必须全部落在 ±6% 内。
// ============================================================================
public static class TexScale
{
    public const float Density = 160f;          // 基准密度（px/米）

    // —— 基准组（校验器强制 ±6%）——
    public const float GrassPeriod = 1.6f;      // 草地 256px → 160px/米
    public const float PathPeriod = 0.8f;       // 次路/岔路/广场/路口铺装 128px → 160px/米
    public const float MainstonePeriod = 1.6f;  // 主路石板 256px → 160px/米（4×4 石板，单块 0.4m）
    public const float CobblePeriod = 0.8f;     // 家属区小石路 128px → 160px/米
    public const float FlowerPeriod = 1.6f;     // 花坛 256px → 160px/米
    public const float DirtPeriod = 4.8f;       // 土斑 768px → 160px/米
    public const float BandVPeriod = 0.6f;      // 中线石带纵向 96px → 160px/米
    public const float BandHPeriod = 0.6f;      // 中线石带横向 96px → 160px/米

    // —— 树/建筑件（登记项）——
    public const float LeafMeters = 0.2f;       // 树冠叶簇 32px → 160px/米
    public const float BarkMeters = 0.2f;       // 树皮 32px → 160px/米

    public static float RoadPeriod(string kind) => kind == "mainstone" ? MainstonePeriod : CobblePeriod;

    // 按周期精确平铺（不取整：取整会把实际密度带偏 ±4%，路宽 4.6m 时尤其明显）
    public static Vector2 Repeat(float w, float d, float period) => new Vector2(w / period, d / period);

    // 密度登记表：贴图名 → 整张贴图覆盖的 (宽, 高) 米数；校验器据此算 px/米
    // 「基准组」（grass/path/road_*）必须落在 160px/米 ±6%，其余为登记值并逐张打印
    public static readonly Dictionary<string, Vector2> Cover = new Dictionary<string, Vector2>
    {
        // —— 基准组 ——
        ["grass"] = new Vector2(GrassPeriod, GrassPeriod),
        ["path"] = new Vector2(PathPeriod, PathPeriod),
        ["road_mainstone"] = new Vector2(MainstonePeriod, MainstonePeriod),
        ["road_cobble"] = new Vector2(CobblePeriod, CobblePeriod),
        ["flower"] = new Vector2(FlowerPeriod, FlowerPeriod),
        ["dirt"] = new Vector2(DirtPeriod, DirtPeriod),
        ["roadband_v"] = new Vector2(0.6f, 1.0f),
        ["roadband_h"] = new Vector2(0.6f, 1.0f),

        // —— 登记组（受贴图上限/整张铺满限制）——
        ["track"] = new Vector2(16f, 10f),          // 操场：整张铺满
        ["court_basket"] = new Vector2(15f, 9f),
        ["court_tennis"] = new Vector2(15f, 8f),
        ["pond"] = new Vector2(7.2f, 7.2f),
        ["villa"] = new Vector2(8.4f, 5.4f),
        ["door"] = new Vector2(1.5f, 2.1f),
        ["leaves"] = new Vector2(LeafMeters, LeafMeters),
        ["bark"] = new Vector2(BarkMeters, BarkMeters),
        ["concrete"] = new Vector2(0.2f, 0.2f),     // 矮墙/立柱：32px → 160px/米
        ["water"] = new Vector2(0.4f, 0.4f),        // 河水：64px → 160px/米（无缝平铺）
        ["bridge_stone"] = new Vector2(0.4f, 0.4f), // 桥用条石：64px → 160px/米
        ["wood"] = new Vector2(0.4f, 0.4f),         // 廊桥木板：64px → 160px/米
        ["clock"] = new Vector2(0.8f, 0.8f),        // 钟面：128px → 160px/米
    };
}
