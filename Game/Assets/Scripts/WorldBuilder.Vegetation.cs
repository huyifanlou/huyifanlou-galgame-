using UnityEngine;
public partial class WorldBuilder
{
    void RefinedTree(Transform root,int variant,float seed)
    {
        int kind=variant==6||variant==7?1:0;
        NaturalTree.Build(root,kind,Mathf.RoundToInt(seed*1000),false,false,variant==5?"#999361":null);
        var col=root.gameObject.AddComponent<CapsuleCollider>();col.center=Vector3.up*1.35f;col.height=2.7f;col.radius=.25f;
    }
    void RefinedBush(Transform root,int seed)
    {var batch=new RegionMeshBatch();GardenDetail.Shrub(batch,Vector3.zero,seed);batch.Flush(root);}
}
