using System;
using UnityEngine;

// world.json 数据模型（字段名与 JSON 键完全一致；Unity JsonUtility 反序列化）
// 数据来源：tools/export-assets.mjs 从 web v0.40 快照导出（固定种子 20261015）
[Serializable]
public class WorldData
{
    public BuildingDef[] BUILDINGS;
    public GroundDef[] GROUNDS;
    public PondDef POND;
    public BranchPathDef[] BRANCH_PATHS;
    public FenceDef[] FENCES;
    public VillaDef VILLA;
    public TreeItem[] TREES;
    public PosItem[] BUSHES;
    public PosItem[] FLOWERS;
    public PosItem[] DIRT_PATCHES;
    public LampItem[] LAMPS;
    public DistantDef[] DISTANT_BUILDINGS;
    public RockeryItem[] ROCKERIES;
    public TreeItem[] BORDER_TREES;
    public PosItem[] BORDER_BUSHES;
    public PosItem[] SMALL_ROCKS;
    public PosItem[] BORDER_ROCKS;
    public ReedItem[] REEDS;
    public SpotDef[] SPOTS;

    public static WorldData Load()
    {
        var ta = Resources.Load<TextAsset>("world");
        if (ta == null) { Debug.LogError("[WorldData] Resources/world.json 缺失——先运行 tools/export-assets.mjs"); return null; }
        return JsonUtility.FromJson<WorldData>(ta.text);
    }
}

[Serializable] public class BuildingDef
{
    public string id; public string name;
    public float[] pos;    // [x, z]
    public float[] size;   // [w, h, d]
    public string wall, trim, plinth, roof;
    public int cols, rows, face;
    public string style;   // flat / gable / grand
    public bool wing, antenna, banner, roofProps, tank, awning, vent;
}

[Serializable] public class GroundDef { public string id, name; public float[] pos; public float[] size; }
[Serializable] public class PondDef { public float[] pos; public float r; }
[Serializable] public class BranchPathDef { public float x, z, len; }
[Serializable] public class FenceDef { public string axis; public float at, from, to; }
[Serializable] public class VillaDef { public float x, z, w, h, d; public int face; }
[Serializable] public class TreeItem { public float x, z; public int v; }
[Serializable] public class PosItem { public float x, z; }
[Serializable] public class LampItem { public float x, z, r; }
[Serializable] public class DistantDef { public float x, z, w, h, d; public string style, wall, roof, trim, plinth; }
[Serializable] public class RockeryItem { public float x, z, s; }
[Serializable] public class ReedItem { public float x, z, h; public int t; }
[Serializable] public class SpotDef { public string name; public float x, z, r; }
