using System.Collections.Generic;
using UnityEngine;

// 门厅立柱的图形、玩家 AABB、相机碰撞共用同一尺寸。
public static class SchoolExterior
{
    public static bool Uses(BuildingDef b) => b.id.StartsWith("s_") || b.style=="collegiate" || b.style=="library" || b.style=="hall" || b.style=="modern" || b.style=="dorm" || b.style=="stand";
    public static float PorchWidth(BuildingDef b) => b.style=="library" ? Mathf.Min(8f,b.size[0]*.4f) : 11f;
    public static float PostHeight(BuildingDef b) => b.style=="library"?5.4f:b.style=="hall"?6.2f:b.style=="dorm"?2.95f:3.3f;
    public static IEnumerable<Vector3> Posts(BuildingDef b)
    {
        float front=b.size[2]/2;
        if(b.style=="library" || b.style=="hall")
        {
            int count=b.style=="library"?4:6;
            float half=PorchWidth(b)/2-.8f;
            for(int i=0;i<count;i++) yield return new Vector3(Mathf.Lerp(-half,half,i/(float)(count-1)),.45f,front+2.55f);
        }
        else if(b.style=="collegiate" || b.style=="modern" || b.style=="dorm")
        {
            float half=b.style=="collegiate"?2.4f:b.style=="modern"?2f:2.4f;
            yield return new Vector3(-half,0,front+b.EntranceExtra+1.55f);
            yield return new Vector3(half,0,front+b.EntranceExtra+1.55f);
        }
    }
}
