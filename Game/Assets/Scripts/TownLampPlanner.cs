using System.Collections.Generic;
using UnityEngine;

// 沿道路外侧布置，候选逐个通过相同净空规则；世界与校验器共用最终数据。
public static class TownLampPlanner
{
    public static LampItem[] Rebuild(WorldData world,List<LampItem> sources)
    {
        var result=new List<LampItem>();int moved=0,removed=0;
        foreach(var source in sources)
        {
            var origin=new Vector2(source.x,source.z);var desired=origin;var along=Vector2.right;float yaw=source.r;
            float nearest=3f;
            foreach(var road in world.ROADS)
            {
                if(!road.sidewalk)continue;
                float a=road.axis=="z"?origin.y:origin.x,off=road.axis=="z"?origin.x-road.at:origin.y-road.at;
                float edge=Mathf.Abs(Mathf.Abs(off)-road.width/2f);
                if(a<road.from||a>road.to||edge>=nearest)continue;
                nearest=edge;float side=off>=0?1:-1;
                desired=road.axis=="z"?new Vector2(road.at+side*(road.width/2+1.95f),a):new Vector2(a,road.at+side*(road.width/2+1.95f));
                along=road.axis=="z"?Vector2.up:Vector2.right;
                yaw=road.axis=="z"?(side<0?0:Mathf.PI):(side<0?-Mathf.PI/2:Mathf.PI/2);
            }
            bool placed=false;
            foreach(float offset in new[]{0f,2f,-2f,4f,-4f,6f,-6f,8f,-8f})
            {
                var point=desired+along*offset;
                if(Problem(world,point,result)!=null)continue;
                result.Add(new LampItem{x=point.x,z=point.y,r=yaw});placed=true;
                if((point-origin).sqrMagnitude>.01f)moved++;break;
            }
            if(!placed)removed++;
        }
        Debug.Log($"[LampLayout] 全图 {sources.Count} → {result.Count}，移动 {moved}，移除无净空点 {removed}；净空问题 0");
        if(Args.Has("-lampcheck"))
        {
            int errors=0;
            for(int i=0;i<result.Count;i++)
            {var others=new List<LampItem>(result);others.RemoveAt(i);var l=result[i];string problem=Problem(world,new Vector2(l.x,l.z),others);
             if(problem!=null){errors++;Debug.LogError($"[LampCheck] {i} ({l.x},{l.z}) {problem}");}}
            System.IO.File.WriteAllText(Args.Str("-lampcheck"),JsonUtility.ToJson(new Layout{lamps=result.ToArray(),problems=errors},true));
            Debug.Log($"[LampCheck] {(errors==0?"PASS":"FAIL")} {result.Count} 个灯位，问题 {errors}");
        }
        return result.ToArray();
    }
    [System.Serializable] class Layout {public LampItem[] lamps;public int problems;}
    public static string Problem(WorldData w,Vector2 p,List<LampItem> placed)
    {
        if(p.magnitude>w.BOUND-4)return "边界";
        foreach(var road in w.ROADS)
        {float a=road.axis=="z"?p.y:p.x,off=road.axis=="z"?p.x-road.at:p.y-road.at;
         if(a>road.from-.6f&&a<road.to+.6f&&Mathf.Abs(off)<road.width/2+(road.sidewalk?1.78f:.55f))return "车道/人行道";}
        foreach(var b in w.BUILDINGS)
        {
            if(Mathf.Abs(p.x-b.pos[0])<b.HalfWX+.7f&&Mathf.Abs(p.y-b.pos[1])<b.HalfWZ+.7f)return "建筑";
            var delta=p-b.Door;float front=Vector2.Dot(delta,b.FrontNormal),side=Mathf.Abs(delta.x*b.FrontNormal.y-delta.y*b.FrontNormal.x);
            if(front>-.8f&&front<9&&side<2.4f)return "入户通道";
        }
        var home=w.VILLA;
        if(home!=null&&(Mathf.Abs(p.x-home.x)<home.w/2+.7f&&Mathf.Abs(p.y-home.z)<home.d/2+.7f||Mathf.Abs(p.x-home.x)<2.4f&&Mathf.Abs(p.y-(home.z+home.face*home.d/2))<9))return "家/入户通道";
        if(w.RIVER!=null&&Mathf.Abs(p.y-TownData.RiverZ(p.x))<15.35f)return "河道/滨河步道";
        if(w.POND!=null&&Vector2.Distance(p,new Vector2(w.POND.pos[0],w.POND.pos[1]))<w.POND.r+1)return "池塘";
        foreach(var tree in w.TREES)if(Vector2.Distance(p,new Vector2(tree.x,tree.z))<3.3f)return "树木";
        foreach(var block in w.BLOCKERS)if(Mathf.Abs(p.x-block.x)<block.hw+.6f&&Mathf.Abs(p.y-block.z)<block.hd+.6f)return "实体障碍";
        foreach(var fence in w.FENCES)
        {float a=fence.axis=="z"?p.y:p.x,off=fence.axis=="z"?p.x-fence.at:p.y-fence.at;
         if(a>fence.from-.5f&&a<fence.to+.5f&&Mathf.Abs(off)<.7f)return "围栏";}
        foreach(var bush in w.BUSHES)if(Vector2.Distance(p,new Vector2(bush.x,bush.z))<1.2f)return "灌木";
        foreach(var prop in w.PROPS)
        {
            var delta=p-new Vector2(prop.x,prop.z);float cs=Mathf.Cos(prop.ry),sn=Mathf.Sin(prop.ry);
            float x=delta.x*cs-delta.y*sn,z=delta.x*sn+delta.y*cs;
            if(prop.type=="plaza"&&prop.w>0&&prop.d>0&&Mathf.Abs(x)<prop.w/2+.48f&&Mathf.Abs(z)<prop.d/2+.48f)return "步道";
            if((prop.type=="bench"||prop.type=="sign")&&delta.magnitude<1.8f)return "道具";
        }
        foreach(var k in placed)if(Vector2.Distance(p,new Vector2(k.x,k.z))<5)return "双灯";
        return null;
    }
}
