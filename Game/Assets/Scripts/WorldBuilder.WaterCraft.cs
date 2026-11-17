using UnityEngine;

public partial class WorldBuilder
{
    void EllipseWater(Transform root,float rx,float rz,float y)
    {
        // Radial rings supply enough vertices for small GPU waves; the shore stays anchored.
        const int rings=24,segments=128;
        var vertices=new Vector3[1+rings*(segments+1)];var uv=new Vector2[vertices.Length];var normals=new Vector3[vertices.Length];var colors=new Color[vertices.Length];
        for(int i=0;i<vertices.Length;i++){normals[i]=Vector3.up;colors[i]=Color.black;}
        for(int ring=1;ring<=rings;ring++)for(int i=0;i<=segments;i++)
        {float a=i*Mathf.PI*2/segments,r=ring/(float)rings;int n=1+(ring-1)*(segments+1)+i;vertices[n]=new Vector3(Mathf.Cos(a)*rx*r,0,Mathf.Sin(a)*rz*r);uv[n]=new Vector2(vertices[n].x,vertices[n].z)*.25f;}
        var triangles=new int[segments*3+(rings-1)*segments*6];int t=0;
        for(int i=0;i<segments;i++){triangles[t++]=0;triangles[t++]=i+2;triangles[t++]=i+1;}
        for(int ring=1;ring<rings;ring++)for(int i=0;i<segments;i++)
        {int a=1+(ring-1)*(segments+1)+i,b=a+segments+1;triangles[t++]=a;triangles[t++]=a+1;triangles[t++]=b;triangles[t++]=a+1;triangles[t++]=b+1;triangles[t++]=b;}
        var mesh=new Mesh{name="ShoreBoundWaterGrid"};mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();var bounds=mesh.bounds;bounds.Expand(.12f);mesh.bounds=bounds;
        var go=MeshObj(root,mesh,new Vector3(0,y,0),LitTex("water"),default,false,true);go.name="NaturalWaterSurface";
        var renderer=go.GetComponent<MeshRenderer>();renderer.receiveShadows=true;
        var block=new MaterialPropertyBlock();var center=go.transform.position;block.SetVector("_ShoreEllipse",new Vector4(center.x,center.z,rx,rz));block.SetFloat("_ShoreMode",1);block.SetFloat("_WaveSpeed",rx>6?.35f:.55f);block.SetFloat("_FlowScale",rx>6?.12f:0);renderer.SetPropertyBlock(block);
    }
}
