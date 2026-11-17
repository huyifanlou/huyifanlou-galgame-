using UnityEngine;
// Stadium-shaped strips, with an open grass infield rather than a rectangular decal.
public static class SchoolTrack
{
    public static void Build(Transform parent,float width,float depth,Material rubber)
    {
        float radius=depth*.5f,straight=(width-depth)*.5f,inner=radius-2.5f;
        Vector3 Point(float angle,float r)=>new Vector3(Mathf.Cos(angle)*r+(Mathf.Cos(angle)>=0?straight:-straight),0,Mathf.Sin(angle)*r);
        void Strip(string name,float r0,float r1,float y,Material mat)
        {
            const int n=160;var vertices=new Vector3[(n+1)*2];var uv=new Vector2[vertices.Length];var triangles=new int[n*6];
            for(int i=0;i<=n;i++){float a=(i+.5f)*Mathf.PI*2/n;for(int k=0;k<2;k++){var p=Point(a,k==0?r0:r1);p.y=y;vertices[i*2+k]=p;uv[i*2+k]=new Vector2(p.x,p.z);}}
            for(int i=0;i<n;i++){int a=i*2,t=i*6;triangles[t]=a;triangles[t+1]=a+2;triangles[t+2]=a+1;triangles[t+3]=a+1;triangles[t+4]=a+2;triangles[t+5]=a+3;}
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }
        Strip("连续塑胶跑道",inner,radius,0,rubber);
        var white=MatLib.LitColor(MatLib.Hex("#eee9d9"));
        for(int i=0;i<=4;i++){float r=Mathf.Lerp(inner,radius,i/4f);Strip("跑道线_"+i,r-.025f,r+.025f,.008f,white);}
        var finish=new GameObject("起终点线");finish.transform.SetParent(parent,false);finish.transform.localPosition=new Vector3(0,.012f,(inner+radius)*.5f);
        finish.AddComponent<MeshFilter>().sharedMesh=MeshFactory.Quad(.065f,radius-inner);finish.AddComponent<MeshRenderer>().sharedMaterial=white;
        Debug.Log("[SchoolTrack] stadium mesh / four lanes / open grass infield / no rectangular track plane");
    }
}
