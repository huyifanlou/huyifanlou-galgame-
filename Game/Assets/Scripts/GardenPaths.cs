using System.Collections.Generic;
using UnityEngine;

// Rebuild path data before vegetation, collision and lamp planning, never as a visual overlay.
public static class GardenPaths
{
    static PropDef Strip(Vector2 a,Vector2 b,float width)
    {var d=b-a;return new PropDef{type="plaza",x=(a.x+b.x)/2,z=(a.y+b.y)/2,w=width,d=d.magnitude+.8f,ry=Mathf.Atan2(d.x,d.y),kind="gardenpath"};}
    public static PropDef[] Rebuild(PropDef[] source,RoadDef[] roads)
    {
        var list=new List<PropDef>();
        foreach(var p in source)
        {
            bool grove=p.type=="plaza"&&p.w<=3.1f&&p.x>=31&&p.x<=41&&p.z< -58&&p.z> -83;
            bool approach=p.type=="plaza"&&Mathf.Abs(p.x+16.5f)<.1f&&Mathf.Abs(p.z+17.5f)<.1f;
            if(!grove&&!approach)list.Add(p);
        }
        void Curve(Vector2[] points,float fromWidth,float toWidth)
        {
            for(int k=0;k<points.Length-1;k++)
            {
                var a=points[Mathf.Max(0,k-1)];var b=points[k];var c=points[k+1];var d=points[Mathf.Min(points.Length-1,k+2)];
                Vector2 At(float t)=>.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
                int n=Mathf.CeilToInt(Vector2.Distance(b,c)/.75f);for(int i=0;i<n;i++){float t=i/(float)n;list.Add(Strip(At(t),At((i+1f)/n),Mathf.Lerp(fromWidth,toWidth,(k+t)/(points.Length-1))));}
            }
        }
        Curve(new[]{new Vector2(36,-58),new Vector2(32,-62),new Vector2(37,-66),new Vector2(40,-71),new Vector2(37,-76),new Vector2(36,-82)},3.3f,2.2f);
        Curve(new[]{new Vector2(-45,-17.5f),new Vector2(-28,-15.9f),new Vector2(-8,-17.5f),new Vector2(4,-18.2f),new Vector2(12,-17.5f)},3.2f,2.8f);
        var candidates=new List<PropDef>(list);
        foreach(var pair in new[]{new[]{new Vector2(-45,-17.5f),new Vector2(-28,-15.9f)},new[]{new Vector2(-8,-17.5f),new Vector2(4,-18.2f)},new[]{new Vector2(4,-18.2f),new Vector2(12,-17.5f)}}){var candidate=Strip(pair[0],pair[1],3.2f);candidate.kind=null;candidates.Add(candidate);}
        var original=candidates.ToArray();int mouths=0;
        foreach(var p in original)
        {
            if(p.type!="plaza"||p.kind=="gardenpath"||p.w>5.5f||p.d<4)continue;
            var forward=new Vector2(Mathf.Sin(p.ry),Mathf.Cos(p.ry));
            foreach(int side in new[]{-1,1})
            {
                var end=new Vector2(p.x,p.z)+forward*(p.d/2)*side;var away=-forward*side;
                foreach(var road in roads)
                {
                    float across=road.axis=="x"?end.y:end.x,delta=road.axis=="x"?away.y:away.x,along=road.axis=="x"?end.x:end.y;
                    float shoulder=road.width/2+(road.sidewalk?1.48f:0);
                    if(Mathf.Abs(delta)<.35f||Mathf.Abs(across-road.at)>shoulder+2||along<road.from||along>road.to)continue;
                    float farAcross=across+delta*p.d,sign=Mathf.Sign(farAcross-road.at);
                    if(Mathf.Abs(farAcross-road.at)<shoulder+.7f)continue;
                    float distance=(road.at+sign*shoulder-across)/delta;
                    if(distance< -2||distance>p.d-1)continue;
                    var mouth=end+away*distance;float reach=Mathf.Min(2.5f,p.d-distance);
                    for(int i=0;i<8;i++){var strip=Strip(mouth+away*i*reach/8,mouth+away*(i+1)*reach/8,Mathf.Lerp(p.w+1.5f,p.w,(i+.5f)/8));strip.kind="pathmouth";list.Add(strip);}
                    mouths++;break;
                }
            }
        }
        Debug.Log($"[GardenPaths] replaced old grove/approach strips, curved samples and {mouths} tapered street mouths");return list.ToArray();
    }
}
