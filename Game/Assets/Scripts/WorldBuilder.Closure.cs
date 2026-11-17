using UnityEngine;
public partial class WorldBuilder
{
    void BuildAmusementClosure()
    {
        if(W.RIVER==null||!ClosedAreas.AmusementClosed)return;
        var root=NewGo("Amusement_ConstructionClosure",Root).transform;var batch=new RegionMeshBatch();
        var steel=DetailSurface.Mat("metal","#697c78");var panel=DetailSurface.Mat("metal","#45605a");var yellow=DetailSurface.Mat("paint","#e2bc55");var dark=DetailSurface.Mat("paint","#333d3b");
        void Side(Vector2 a,Vector2 b)
        {
            Vector2 d=b-a;int count=Mathf.CeilToInt(d.magnitude/2.8f);float yaw=Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;
            for(int i=0;i<count;i++)
            {
                var p=Vector2.Lerp(a,b,(i+.5f)/count);float len=d.magnitude/count;
                batch.Bevel(panel,new Vector3(p.x,1.02f,p.y),new Vector3(len-.04f,1.86f,.10f),.012f,-yaw);
                foreach(float y in new[]{.20f,1.82f})batch.Bevel(steel,new Vector3(p.x,y,p.y),new Vector3(len,.065f,.13f),.009f,-yaw);
                var q=Vector2.Lerp(a,b,i/(float)count);batch.Bevel(steel,new Vector3(q.x,1.02f,q.y),new Vector3(.13f,2.04f,.13f),.012f);
            }
            var collider=new GameObject("construction_barrier");collider.transform.SetParent(root,false);collider.transform.localPosition=new Vector3((a.x+b.x)/2,1,(a.y+b.y)/2);collider.transform.localEulerAngles=new Vector3(0,-yaw,0);collider.AddComponent<BoxCollider>().size=new Vector3(d.magnitude,2,.16f);
        }
        Side(new Vector2(ClosedAreas.X1,ClosedAreas.Z1),new Vector2(ClosedAreas.X2,ClosedAreas.Z1));Side(new Vector2(ClosedAreas.X1,ClosedAreas.Z2),new Vector2(ClosedAreas.X2,ClosedAreas.Z2));
        Side(new Vector2(ClosedAreas.X1,ClosedAreas.Z1),new Vector2(ClosedAreas.X1,ClosedAreas.Z2));Side(new Vector2(ClosedAreas.X2,ClosedAreas.Z1),new Vector2(ClosedAreas.X2,ClosedAreas.Z2));
        foreach(float x in new[]{19f,49f,79f})
        {
            for(int i=0;i<9;i++)batch.Bevel(i%2==0?yellow:dark,new Vector3(x-2.4f+i*.6f,1.14f,ClosedAreas.Z2+.10f),new Vector3(.57f,.34f,.04f),.005f);
            var sign=new BuildingDef{id="closure_"+x,name="设施维护 · 暂停开放"};ResidenceSign(root,sign,new Vector3(x,1.58f,ClosedAreas.Z2+.13f));
        }
        batch.Flush(root);Debug.Log("[Closure] 游乐场封闭：围挡、封条、碰撞、传送与地图共用边界");
    }
}
