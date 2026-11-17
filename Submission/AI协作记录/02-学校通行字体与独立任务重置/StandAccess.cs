using UnityEngine;
public static class StandAccess
{
    public static Vector3 Local(BuildingDef b,float x,float z)=>Quaternion.Euler(0,-b.Yaw,0)*new Vector3(x-b.pos[0],0,z-b.pos[1]);
    public static float Front(BuildingDef b)=>b.size[2]/2-.225f;
    public static float Rear(BuildingDef b)=>Front(b)-3.75f;
    public static float Step(int i)=>i==0?.15f:i==1?.30f:.30f+(i-1)*.19f;
    public static bool TryHeight(BuildingDef[] buildings,float x,float z,out float height)
    {
        foreach(var b in buildings)if(b.style=="stand")
        {var p=Local(b,x,z);if(Mathf.Abs(p.x)<b.size[0]/2&&p.z<=Front(b)&&p.z>=Rear(b)){int i=Mathf.Clamp(Mathf.FloorToInt((Front(b)-p.z)/.375f),0,9);height=Mathf.Abs(p.x)<1.15f?Step(i):.30f+(i/2)*.38f;return true;}}
        height=0;return false;
    }
    public static bool Blocks(BuildingDef b,float x,float z)
    {
        var p=Local(b,x,z);if(Mathf.Abs(p.x)>b.size[0]/2+.32f||p.z>Front(b)+.32f||p.z<Rear(b)-.32f)return false;
        if(p.z<Rear(b)+.12f||Mathf.Abs(p.x)>b.size[0]/2-.20f)return true;
        return Mathf.Abs(p.x)>1.0f;
    }
}
