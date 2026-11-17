using System.Collections.Generic;
using UnityEngine;

// Parallel-transport frames avoid twisting seams; fluted rings give bark real silhouette detail.
public static class OrganicLimb
{
    public static Mesh Build(Vector3[] controls,float[] radii,int sides=18,int subdivisions=5)
    {
        int steps=(controls.Length-1)*subdivisions;var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
        Vector3 At(float t)
        {float f=Mathf.Clamp(t,0,controls.Length-1);int k=Mathf.Min(controls.Length-2,Mathf.FloorToInt(f));float u=f-k;var a=controls[Mathf.Max(0,k-1)];var b=controls[k];var c=controls[k+1];var d=controls[Mathf.Min(controls.Length-1,k+2)];return .5f*((2*b)+(-a+c)*u+(2*a-5*b+4*c-d)*u*u+(-a+3*b-3*c+d)*u*u*u);}
        Vector3 right=Vector3.right;float length=0;Vector3 previous=At(0);
        for(int i=0;i<=steps;i++)
        {
            float t=i/(float)subdivisions;var p=At(t);var tangent=(At(t+.02f)-At(t-.02f)).normalized;
            right=Vector3.ProjectOnPlane(right,tangent).normalized;if(right.sqrMagnitude<.1f)right=Vector3.Cross(tangent,Vector3.forward).normalized;var up=Vector3.Cross(tangent,right).normalized;
            int k=Mathf.Min(controls.Length-2,Mathf.FloorToInt(t));float radius=Mathf.Lerp(radii[k],radii[k+1],t-k);length+=Vector3.Distance(p,previous);previous=p;
            for(int j=0;j<=sides;j++){float a=j*Mathf.PI*2/sides;var normal=right*Mathf.Cos(a)+up*Mathf.Sin(a);float flute=1+.045f*Mathf.Sin(a*5+t*1.3f);v.Add(p+normal*radius*flute);n.Add(normal);uv.Add(new Vector2(j/(float)sides*2,length*.5f));}
        }
        for(int i=0;i<steps;i++)for(int j=0;j<sides;j++){int a=i*(sides+1)+j,b=a+sides+1;tri.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
        for(int end=0;end<2;end++){int center=v.Count,row=end==0?0:steps*(sides+1);v.Add(controls[end==0?0:controls.Length-1]);n.Add((At(end==0?.02f:controls.Length-1)-At(end==0?0:controls.Length-1-.02f)).normalized*(end==0?-1:1));uv.Add(Vector2.zero);for(int j=0;j<sides;j++)tri.AddRange(end==0?new[]{center,row+j+1,row+j}:new[]{center,row+j,row+j+1});}
        var mesh=new Mesh{name="CurvedFlutedBranch"};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateBounds();return mesh;
    }
}
