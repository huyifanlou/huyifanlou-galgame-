using System.Collections.Generic;
using UnityEngine;

public static class ResidentialExterior
{
    public static bool Uses(BuildingDef b) => b.id.StartsWith("r_");
    public static BuildingDef Home(VillaDef v) => new BuildingDef {
        id="home",name="家",pos=new[]{v.x,v.z},size=new[]{v.w,v.h,v.d},face=v.face,
        wall="#d9cfb9",trim="#ede4d2",plinth="#a69d88",roof="#657069",cols=3,rows=2,style="gable" };

    // 从实际门口接向正前方街道的外侧人行道；不改原有建筑/道路坐标。
    public static List<PropDef> EntryPaths(WorldData world)
    {
        var result=new List<PropDef>();var buildings=new List<BuildingDef>();
        foreach(var b in world.BUILDINGS)if(Uses(b))buildings.Add(b);
        if(world.VILLA!=null)buildings.Add(Home(world.VILLA));
        foreach(var b in buildings)
        {
            var door=b.Door;var n=b.FrontNormal;float nearest=12f;
            foreach(var road in world.ROADS)
            {
                bool alongX=Mathf.Abs(n.x)>.5f;
                if(road.axis!=(alongX?"z":"x"))continue;
                float along=alongX?door.y:door.x;
                if(along<road.from||along>road.to)continue;
                float distance=(road.at-(alongX?door.x:door.y))*(alongX?n.x:n.y);
                if(distance<=0)continue;
                float edge=distance-road.width/2-(road.sidewalk?1.4f:0)+.18f;
                if(edge>.78f&&edge<nearest)nearest=edge;
            }
            if(nearest>=12)continue;
            const float start=.75f;
            var center=door+n*((start+nearest)/2);
            result.Add(new PropDef {type="plaza",kind="residential_entry",x=center.x,z=center.y,w=1.8f,d=nearest-start,ry=Mathf.Atan2(n.x,n.y)});
        }
        return result;
    }
    public static IEnumerable<BlockerDef> HomeSolids(VillaDef home)
    {
        foreach(int side in new[]{-1,1})
            yield return new BlockerDef {x=home.x+side*2.6f,z=home.z+home.face*(home.d/2+2.8f),hw=1.3f,hd=.07f};
        yield return new BlockerDef {x=home.x+3.1f,z=home.z+home.face*(home.d/2+1.15f),hw=.28f,hd=.28f};
    }
}
