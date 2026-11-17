using UnityEngine;

// 入口细节统一从实际外墙坐标生成，保持中间通道净空（docs/108）。
public partial class WorldBuilder
{
    void ClockTowerDoor(Transform parent)
    {
        var batch=new RegionMeshBatch();PrecisionExteriorDoor(batch,new Vector3(0,.80f,2.33f),DetailSurface.Mat("stone","#bab19d"),DetailSurface.Mat("timber","#8c7255"),DetailSurface.Mat("glass","#657f87"),DetailSurface.Mat("metal","#b2a17e"));
        var root=NewGo("precision_clocktower_door",parent).transform;batch.Flush(root);
    }

    void WallPlanter(Transform parent, float x, float ground, float front)
    {
        // 外挑 <0.45m：完全处于建筑原有碰撞垫内，不产生新空气墙。
        Box(parent,new Vector3(x,ground+0.27f,front+0.18f),new Vector3(0.85f,0.48f,0.30f),ColMat("#b5aa92"));
        Box(parent,new Vector3(x,ground+0.53f,front+0.18f),new Vector3(0.93f,0.065f,0.34f),ColMat("#d2c7b3"));
        var batch=new RegionMeshBatch();GardenDetail.Shrub(batch,new Vector3(x,ground+.53f,front+.18f),Mathf.RoundToInt(x*71),"#cda8b4",.42f);batch.Flush(parent);
    }

    // 门口台阶与视觉体块共用尺寸，行走不再陷入台阶。
    float EntranceGroundY(float x,float z)
    {
        foreach(var b in W.BUILDINGS)
        {
            if(PavilionFootprint.Uses(b)||b.style=="plain"||b.style=="stand"||b.style=="shrine"||b.style=="library"||b.style=="hall") continue;
            float a=b.Yaw*Mathf.Deg2Rad, dx=x-b.pos[0], dz=z-b.pos[1];
            float lx=dx*Mathf.Cos(a)-dz*Mathf.Sin(a);
            float t=dx*Mathf.Sin(a)+dz*Mathf.Cos(a)-b.size[2]/2f-b.EntranceExtra;
            if(Mathf.Abs(lx)<=0.95f && t>=0.05f && t<=0.95f) return 0.16f;
        }
        var v=W.VILLA;
        if(v!=null && Mathf.Abs(x-v.x)<=1f)
        {
            float t=(z-v.z)*v.face-v.d/2f;
            if(t>=0.05f && t<=1.05f) return 0.16f;
        }
        if(v!=null && Mathf.Abs(x-(v.x+0.6f*v.face))<=2.1f)
        {
            float t=(z-v.z)*v.face-v.d/2f;
            if(t>=0.7f && t<=3.1f) return 0.028f;
        }
        return 0f;
    }
}
