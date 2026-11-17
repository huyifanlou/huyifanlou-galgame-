using System.Collections.Generic;using UnityEngine;
public static class VegetationPlanner
{
    public static bool Clear(WorldData w,float x,float z,float margin)
    {
        if(w.RIVER!=null&&Mathf.Abs(z-(w.RIVER.amp*Mathf.Sin((x+w.RIVER.phase)/w.RIVER.freq)+w.RIVER.offset))<w.RIVER.width/2+margin)return false;
        foreach(var b in w.BUILDINGS)if(Mathf.Abs(x-b.pos[0])<b.HalfWX+margin+.7f&&Mathf.Abs(z-b.pos[1])<b.HalfWZ+margin+.7f)return false;
        foreach(var r in w.ROADS){float along=r.axis=="x"?x:z,across=r.axis=="x"?z:x;if(along>r.from-margin&&along<r.to+margin&&Mathf.Abs(across-r.at)<r.width/2+(r.sidewalk?1.48f:0)+margin)return false;}
        foreach(var p in w.PROPS)if(p.type=="plaza")
        {float dx=x-p.x,dz=z-p.z,c=Mathf.Cos(p.ry),s=Mathf.Sin(p.ry);if(Mathf.Abs(dx*c-dz*s)<p.w/2+margin&&Mathf.Abs(dx*s+dz*c)<p.d/2+margin)return false;}
        foreach(var b in w.BLOCKERS)if(Mathf.Abs(x-b.x)<b.hw+margin&&Mathf.Abs(z-b.z)<b.hd+margin)return false;
        return true;
    }
    public static void Apply(WorldData w)
    {
        int old=w.TREES.Length,oldBush=w.BUSHES.Length;var trees=new List<TreeItem>();
        foreach(var t in w.TREES)
        {
            // 将旧渲染器中“隐藏但仍碰撞”的排除区移入共同数据层。
            if(Mathf.Abs(t.x-79)<13&&Mathf.Abs(t.z+57)<16)continue;
            if(!Clear(w,t.x,t.z,.80f))continue;
            bool duplicate=false;foreach(var a in trees)if((new Vector2(a.x-t.x,a.z-t.z)).sqrMagnitude<2.0f){duplicate=true;break;}
            if(!duplicate)trees.Add(t);
        }
        w.TREES=trees.ToArray();var bushes=new List<PosItem>();
        foreach(var b in w.BUSHES)
        {if(!Clear(w,b.x,b.z,.48f))continue;bool duplicate=false;foreach(var a in bushes)if(new Vector2(a.x-b.x,a.z-b.z).sqrMagnitude<.65f){duplicate=true;break;}if(!duplicate)bushes.Add(b);}
        w.BUSHES=bushes.ToArray();Debug.Log($"[Vegetation] 树 {old}→{trees.Count}，灌木 {oldBush}→{bushes.Count}；道路/铺装/建筑/设施避让统一，隐藏树空气碰撞退役");
    }
}
