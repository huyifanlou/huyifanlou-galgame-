using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Branches terminate in individual folded leaf/flower sprays, never a solid crown shell.
public static partial class NaturalTree
{
    static readonly Dictionary<int,Mesh> sprays=new Dictionary<int,Mesh>();
    static readonly Dictionary<string,Material> foliage=new Dictionary<string,Material>();
    static Material Leaves(string color)
    {
        if(foliage.TryGetValue(color,out var m))return m;
        m=MatLib.LitColor(MatLib.Hex(color));m.name="LeafSprays_"+color;m.SetFloat("_OutlineWidth",0);m.SetFloat("_VertexTint",1);m.SetFloat("_Roughness",.88f);m.SetFloat("_RimStrength",.035f);
        foliage[color]=m;return m;
    }
    public static Mesh Spray(int seed,int kind,bool distant=false)
    {
        int key=seed%4+kind*10+(distant?100:0);if(sprays.TryGetValue(key,out var mesh))return mesh;
        var random=new System.Random(13700+key);var v=new List<Vector3>();var uv=new List<Vector2>();var normals=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
        float R()=> (float)random.NextDouble();
        void Leaf(Vector3 center,Quaternion rot,float length,float width,float tone)
        {
            int b=v.Count;
            var points=new List<Vector3>();
            if(distant)points.AddRange(new[]{new Vector3(0,0,-length*.5f),new Vector3(-width*.65f,0,-length*.28f),new Vector3(-width,0,length*.05f),new Vector3(0,0,length*.5f),new Vector3(width,0,length*.05f),new Vector3(width*.65f,0,-length*.28f)});
            else for(int j=0;j<10;j++){float a=j*Mathf.PI*2/10;points.Add(new Vector3(Mathf.Sin(a)*width,Mathf.Sin(a)*Mathf.Sin(a)*length*.035f,Mathf.Cos(a)*length*.5f));}
            int ring=points.Count;points.Add(new Vector3(0,length*.15f,0));
            for(int i=0;i<points.Count;i++){v.Add(center+rot*points[i]);normals.Add(rot*new Vector3(points[i].x/width*.25f,1,points[i].z/length*.2f).normalized);uv.Add(new Vector2(points[i].x/width*.5f+.5f,points[i].z/length+.5f));float t=tone*(i==ring?1.06f:.93f);colors.Add(new Color(t,t,t,1));}
            for(int i=0;i<ring;i++){int j=(i+1)%ring;triangles.Add(b+i);triangles.Add(b+j);triangles.Add(b+ring);triangles.Add(b+ring);triangles.Add(b+j);triangles.Add(b+i);}

        }
        int count=distant?24:kind==2?18:32;
        for(int i=0;i<count;i++)
        {
            float a=i*2.39996f+R()*.5f,r=Mathf.Sqrt(R())*.46f;
            var center=new Vector3(Mathf.Cos(a)*r,(R()-.4f)*.44f,Mathf.Sin(a)*r);
            var rot=Quaternion.Euler(-35+R()*70,R()*360,-35+R()*70);float tone=.72f+R()*.32f;
            if(kind==2)
            {
                int petals=distant?3:5;for(int k=0;k<petals;k++){var turn=rot*Quaternion.Euler(0,k*360f/petals,0);Leaf(center+turn*Vector3.forward*.07f,turn,.16f,.047f,tone);}
            }
            else Leaf(center,rot,kind==1?.40f:.27f+R()*.13f,kind==1?.024f:.065f+R()*.035f,tone);
        }
        mesh=new Mesh{name="OrganicSpray_"+key};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.SetNormals(normals);mesh.RecalculateBounds();sprays[key]=mesh;return mesh;
    }
    public static void Build(Transform root,int kind,int seed,bool distant=false,bool giant=false,string color=null)
    {
        if(giant){Landmark(root,seed);return;}
        root.gameObject.AddComponent<NaturalTreeMarker>().Kind=kind;
        var random=new System.Random(seed&0x7fffffff);float R()=>(float)random.NextDouble();
        var batch=new RegionMeshBatch();var bark=DetailSurface.Mat("timber","#75644f");
        var leaf=Leaves(color??(kind==2?"#e2b0c5":kind==1?"#4f6d57":"#718653"));
        float scale=giant?2.05f:1f,height=kind==1?4.9f:kind==2?5.4f:4.25f;
        Vector3 P(Vector3 p)=>p*scale;
        void Branch(Vector3 a,Vector3 b,float r)=>batch.Branch(bark,P(a),P(b),r*scale,true);
        var lean=new Vector3((R()-.5f)*.24f,height, (R()-.5f)*.24f);
        float trunkRadius=giant?.46f:kind==2?.25f:.17f;
        var fork=Vector3.Lerp(Vector3.zero,lean,.38f)+Vector3.right*.10f;
        var bend=Vector3.Lerp(Vector3.zero,lean,.72f)+Vector3.forward*.12f;
        Mesh detailedTrunk=null;
        if(distant){Branch(Vector3.zero,fork,trunkRadius);Branch(fork,bend,trunkRadius*.55f);Branch(bend,lean,trunkRadius*.3025f);}
        else {detailedTrunk=OrganicLimb.Build(new[]{Vector3.zero,fork,bend,lean},new[]{trunkRadius,trunkRadius*.68f,trunkRadius*.36f,trunkRadius*.10f},18,5);batch.Add(bark,detailedTrunk,Vector3.zero,Vector3.one,Quaternion.identity);}
        for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f+R();Branch(new Vector3(Mathf.Cos(a)*.40f,.035f,Mathf.Sin(a)*.40f),Vector3.up*.45f,.07f);}
        int arms=distant?4:kind==2?11:9;
        for(int i=0;i<arms;i++)
        {
            float a=i*2.39996f+seed*.013f;var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            float y=kind==1?.9f+i*(height-1.5f)/arms:1.65f+i*(height-2.1f)/arms;
            float reach=kind==1?1.45f*(1-y/height)+.25f:kind==2?1.7f+R()*.45f:.95f+R()*.48f;
            var start=Vector3.Lerp(Vector3.zero,lean,y/height);var elbow=start+radial*reach*.55f+Vector3.up*.3f;
            var end=start+radial*reach+Vector3.up*(kind==1?.08f:.55f+R()*.35f);
            float taper=1-y/height*.6f;Branch(start,elbow,.065f*taper);Branch(elbow,end,.040f*taper);
            int twigs=distant?2:5;
            for(int j=0;j<twigs;j++)
            {
                var side=Vector3.Cross(radial,Vector3.up)*(j%2==0?1:-1);var tip=Vector3.Lerp(elbow,end,.35f+j*.22f)+side*(.26f+R()*.30f)+Vector3.up*(R()*.37f);
                Branch(elbow,tip,.018f);
                int tufts=distant?1:3;
                for(int k=0;k<tufts;k++)batch.Add(leaf,Spray(i+j+k,kind,distant),P(tip+radial*k*.22f+Vector3.up*k*.19f),Vector3.one*scale*(distant?1.8f:kind==2?1.35f:kind==1?1.10f:1.15f),Quaternion.Euler(R()*35,R()*360,R()*25));
            }
        }
        for(int i=0;i<(distant?2:5);i++)batch.Add(leaf,Spray(i,kind,distant),P(lean+new Vector3((R()-.5f)*.5f,-.30f+i*.09f,(R()-.5f)*.5f)),Vector3.one*scale*(distant?1.6f:1),Quaternion.Euler(0,R()*360,0));
        batch.Flush(root);if(detailedTrunk!=null)Object.Destroy(detailedTrunk);
    }
}
public class NaturalTreeMarker:MonoBehaviour{public int Kind;}
