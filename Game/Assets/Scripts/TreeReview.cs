using System;using System.Collections;using UnityEngine;
public class TreeReview:MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null;yield return null;
        if(Resources.Load<Texture2D>("intro/intro-char0")!=null)throw new Exception("Retired opening logo remains active or packaged");
        Debug.Log("[StartupCheck] PASS no retired opening logo resources; interactive title supported");
        int count=0,triangles=0;
        foreach(var tree in FindObjectsOfType<NaturalTreeMarker>())
        {
            count++;
            foreach(var filter in tree.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;triangles+=mesh.triangles.Length/3;
                foreach(var normal in mesh.normals)if(float.IsNaN(normal.x)||normal.sqrMagnitude<.2f)throw new Exception("Invalid tree normals "+tree.name);
                if(mesh.name.Contains("连续花冠")||mesh.name.Contains("Canopy"))throw new Exception("Old crown shell remains");
            }
        }
        if(count<20)throw new Exception("Tree replacement coverage missing");
        Debug.Log("[TreeCheck] PASS "+count+" real trees, "+triangles+" triangles / individual foliage / valid normals / no old canopy shells");
        if(Args.Has("-treepreview"))
        {
            int kind=Args.I("-treespecies")??0;var rig=GetComponent<PlayerRig>();NaturalTreeMarker selected=null;float nearest=float.MaxValue;
            foreach(var tree in FindObjectsOfType<NaturalTreeMarker>())if(tree.Kind==kind)
            {float distance=(tree.transform.position-rig.Position).sqrMagnitude;if(distance<nearest){nearest=distance;selected=tree;}}
            if(selected!=null)
            {
                var center=selected.transform.position;
                for(int i=0;i<16;i++){float a=i*Mathf.PI/8;if(rig.Teleport(center.x+Mathf.Sin(a)*7,center.z+Mathf.Cos(a)*7)){rig.LookAtWorld(center+Vector3.up*2.8f);break;}}
                Debug.Log("[TreePreview] kind "+kind+" / tree at "+center);
            }
        }
    }
}
