using System.Collections.Generic;using UnityEngine;
public static class PavilionFootprint
{
    public static bool Uses(BuildingDef b)=>b.id=="p_pav"||b.id=="k_chozu";
    public static IEnumerable<Vector2> Posts(BuildingDef b)
    {foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})yield return new Vector2(x*(b.size[0]/2-.25f),z*(b.size[2]/2-.25f));}
}
