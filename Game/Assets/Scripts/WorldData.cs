using System;
using UnityEngine;

// world.json 数据模型（字段名与 JSON 键完全一致；Unity JsonUtility 反序列化）
// 数据来源：tools/export-assets.mjs 从 web v0.40 快照导出（固定种子 20261015）
[Serializable]
public class WorldData
{
    public float BOUND;
    public float GROUND_SIZE;
    public RoadDef[] ROADS;
    public PropDef[] PROPS;
    public BlockerDef[] BLOCKERS;
    public BuildingDef[] BUILDINGS;
    public GroundDef[] GROUNDS;
    public PondDef POND;
    public BranchPathDef[] BRANCH_PATHS;
    public FenceDef[] FENCES;
    public VillaDef VILLA;
    public TreeItem[] TREES;
    public PosItem[] BUSHES;
    public PosItem[] AZALEAS;
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
    public RiverDef RIVER;

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
    public string style;   // flat / gable / grand / collegiate / library / modern / dorm / hall / stand
    public bool wing, antenna, banner, roofProps, tank, awning, vent;

    // 朝向：1=+Z、-1=-Z、2=+X、-2=-X；以下按朝向换算世界占地与正门位置
    public bool FacesX => face == 2 || face == -2;
    public float HalfWX => FacesX ? size[2] / 2f : size[0] / 2f;
    public float HalfWZ => FacesX ? size[0] / 2f : size[2] / 2f;
    public float FrontX => face == 2 ? size[2] / 2f : face == -2 ? -size[2] / 2f : 0f;
    public float FrontZ => face == 1 ? size[2] / 2f : face == -1 ? -size[2] / 2f : 0f;
    public float Yaw => face == 2 ? 90f : face == -2 ? -90f : face == -1 ? 180f : 0f;
    public float FrontOffsetX(float extra) => FrontX + (face == 2 ? extra : face == -2 ? -extra : 0f);
    public float FrontOffsetZ(float extra) => FrontZ + (face == 1 ? extra : face == -1 ? -extra : 0f);
}

[Serializable] public class GroundDef { public string id, name, kind; public float[] pos; public float[] size; }
[Serializable] public class RoadDef { public string id, axis, kind; public float at, from, to, width; public bool fence; public bool sidewalk; }
[Serializable] public class PropDef
{
    public string type, kind;
    public float x, z, ry, w, d, r, len;
    public int n;
}
[Serializable] public class BlockerDef { public float x, z, hw, hd; }
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

// 河道（S 形中心线 z = amp·sin((x+phase)/freq) + offset；两端可延伸到界外）
[Serializable] public class RiverDef
{
    public float width;
    public float amp, freq, phase, offset;
    public float from, to;
    public BridgeDef[] bridges;
}
[Serializable] public class BridgeDef { public float x, width; public int arches; public bool covered; }
