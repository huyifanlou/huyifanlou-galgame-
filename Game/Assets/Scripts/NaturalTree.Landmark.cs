using System.Collections.Generic;
using UnityEngine;

public static partial class NaturalTree
{
    static void Landmark(Transform root,int seed)
    {
        root.gameObject.AddComponent<NaturalTreeMarker>().Kind=2;
        var batch=new RegionMeshBatch();var temporary=new List<Mesh>();var bark=DetailSurface.Mat("timber","#78634f");
        var light=Leaves("#e8bccd");var medium=Leaves("#d9a8be");var shade=Leaves("#c492ad");
        void Limb(Vector3[] p,float[] r,int sides=22)
        {var mesh=OrganicLimb.Build(p,r,sides,6);temporary.Add(mesh);batch.Add(bark,mesh,Vector3.zero,Vector3.one,Quaternion.identity);}
        Limb(new[]{Vector3.zero,new Vector3(.45f,2.3f,.1f),new Vector3(-.35f,5.2f,.35f),new Vector3(.65f,8.3f,0),new Vector3(-.5f,12.7f,.6f)},new[]{.95f,.76f,.56f,.31f,.055f},28);
        for(int i=0;i<8;i++){float a=i*2.39996f;var v=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Limb(new[]{v*1.03f+Vector3.up*.025f,v*.62f+Vector3.up*.25f,v*.18f+Vector3.up*1.05f},new[]{.11f,.17f,.06f},14);}
        Vector3[][] branches={
            new[]{new Vector3(.2f,2.8f,0),new Vector3(-3,4.4f,-.6f),new Vector3(-7.5f,6,-2),new Vector3(-12.5f,8.0f,-4)},
            new[]{new Vector3(-.1f,3.6f,.2f),new Vector3(3.5f,4.9f,1.4f),new Vector3(7.6f,6.0f,3.4f),new Vector3(12,8.6f,6)},
            new[]{new Vector3(-.3f,5.0f,.3f),new Vector3(-1.2f,7.2f,4),new Vector3(-3,8.4f,8),new Vector3(-5,10.3f,12)},
            new[]{new Vector3(0,4.4f,0),new Vector3(1.5f,5.8f,-3.4f),new Vector3(4,7.6f,-8),new Vector3(7,10.1f,-12)},
            new[]{new Vector3(.2f,6.8f,.2f),new Vector3(-3.8f,8.6f,-2),new Vector3(-7,10,-6),new Vector3(-11,11.3f,-9)},
            new[]{new Vector3(.4f,7.4f,0),new Vector3(4.2f,9.3f,0),new Vector3(8,10.8f,-2),new Vector3(12.5f,12.1f,-5)}};
        for(int i=0;i<branches.Length;i++)
        {
            var p=branches[i];Limb(p,new[]{.42f,.31f,.16f,.032f});
            for(int j=0;j<5;j++)
            {
                float t=.18f+j*.16f;var start=Vector3.Lerp(p[1],p[3],t);var direction=(p[3]-p[1]).normalized;var lateral=Vector3.Cross(direction,Vector3.up)*(j%2==0?1:-1);
                var elbow=start+lateral*(1.0f+j*.25f)+Vector3.up*.65f;var tip=elbow+direction*1.4f+Vector3.up*.4f;
                Limb(new[]{start,elbow,tip},new[]{.085f,.044f,.008f},14);
                batch.Add(medium,Spray(i+j,2),tip,Vector3.one*1.9f,Quaternion.Euler(j*17,i*63,20));
            }
        }
        // Overlapping ellipsoidal volumes, with low lateral lobes rather than a hemispherical roof.
        var centers=new[]{new Vector3(0,12.4f,0),new Vector3(-8.2f,8.3f,-2.8f),new Vector3(8.5f,8.8f,3.5f),new Vector3(-3,10.4f,8.4f),new Vector3(4.2f,10.8f,-8.0f),new Vector3(-7.5f,11.2f,-7),new Vector3(8.4f,12.1f,-3.8f)};
        var sizes=new[]{new Vector3(7.8f,4.1f,7),new Vector3(6.1f,3.3f,5.4f),new Vector3(6.0f,3.4f,5.6f),new Vector3(6.2f,3.4f,5.6f),new Vector3(6.7f,3.7f,5.5f),new Vector3(5.5f,3.1f,5.3f),new Vector3(5.2f,3.3f,5.1f)};
        var random=new System.Random(143017);float R()=>(float)random.NextDouble();
        for(int lobe=0;lobe<centers.Length;lobe++)for(int i=0;i<140;i++)
        {
            float y=1-2*(i+.5f)/140,a=i*2.39996f+lobe*.83f,r=Mathf.Sqrt(1-y*y),shell=.62f+R()*.38f;
            var offset=Vector3.Scale(new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r)*shell,sizes[lobe]);var p=centers[lobe]+offset;
            var mat=offset.y>1?light:offset.y<-.7f?shade:medium;
            batch.Add(mat,Spray(i+lobe,2),p,Vector3.one*(1.85f+R()*.45f),Quaternion.Euler(-30+R()*60,R()*360,-20+R()*40));
            if(i%9==0&&offset.y<0){var droop=p-Vector3.up*(.55f+R()*.6f);Limb(new[]{p,p+Vector3.down*.4f,droop},new[]{.012f,.008f,.003f},10);batch.Add(light,Spray(i,2),droop,Vector3.one*1.25f,Quaternion.Euler(65,i*41,0));}
        }
        batch.Flush(root);foreach(var mesh in temporary)Object.Destroy(mesh);
        Debug.Log("[LandmarkCherry] organic curved trunk / staggered low limbs / seven irregular crown volumes / 980 flower sprays; no radial inverted triangle");
    }
}
