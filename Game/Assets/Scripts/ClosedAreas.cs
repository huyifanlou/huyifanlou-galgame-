using UnityEngine;
public static class ClosedAreas
{
    public const bool AmusementClosed=true;
    public const float X1=7.8f,X2=96f,Z1=27.5f,Z2=52.5f;
    public static readonly Vector2 SafeGate=new Vector2(6.5f,40);
    public static bool Contains(float x,float z,float pad=0)=>AmusementClosed&&x>X1-pad&&x<X2+pad&&z>Z1-pad&&z<Z2+pad;
}
