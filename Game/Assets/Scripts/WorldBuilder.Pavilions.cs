using UnityEngine;
public partial class WorldBuilder
{
    void PrecisionPavilion(Transform root,BuildingDef b)
    {
        bool basin=b.id=="k_chozu";float w=b.size[0],d=b.size[2],bottom=-b.size[1]/2,height=b.size[1]-.55f;
        var timber=DetailSurface.Mat("timber",basin?"#936c4c":"#9c8466");var stone=DetailSurface.Mat("stone","#b4ad9d");var roof=DetailSurface.Mat("roof",b.roof);var metal=DetailSurface.Mat("metal","#77786a");var batch=new RegionMeshBatch();
        batch.Bevel(stone,new Vector3(0,bottom+.04f,0),new Vector3(w,.08f,d),.018f);
        foreach(var point in PavilionFootprint.Posts(b))
        {
            batch.Bevel(stone,new Vector3(point.x,bottom+.12f,point.y),new Vector3(.38f,.24f,.38f),.025f);
            batch.Cylinder(timber,new Vector3(point.x,bottom+.18f,point.y),new Vector3(point.x,bottom+height,point.y),basin?.09f:.12f);
            var col=NewGo("pavilion_post",root);col.transform.localPosition=new Vector3(point.x,bottom+height/2,point.y);col.AddComponent<BoxCollider>().size=new Vector3(.38f,height,.38f);
        }
        foreach(int side in new[]{-1,1})
        {batch.Bevel(timber,new Vector3(0,bottom+height,side*(d/2-.22f)),new Vector3(w+.25f,.18f,.19f),.025f);batch.Bevel(timber,new Vector3(side*(w/2-.22f),bottom+height,0),new Vector3(.19f,.18f,d+.25f),.025f);}
        var cap=MeshFactory.HipRoof(w,d,Mathf.Min(w,d)*.22f,.4f,2);var capPosition=new Vector3(0,bottom+height+.1f,0);batch.Add(roof,cap,capPosition,Vector3.one,Quaternion.identity);
        var roofCollider=NewGo("pavilion_roof_camera",root);roofCollider.transform.localPosition=capPosition;roofCollider.AddComponent<MeshCollider>().sharedMesh=cap;
        if(basin)
        {
            float z=-.45f;batch.Bevel(stone,new Vector3(0,bottom+.48f,z),new Vector3(1.8f,.8f,.74f),.045f);
            batch.Bevel(DetailSurface.Mat("stone","#8c928c"),new Vector3(0,bottom+.89f,z),new Vector3(1.86f,.10f,.80f),.025f);
            batch.Bevel(DetailSurface.Mat("glass","#5a8785"),new Vector3(0,bottom+.948f,z),new Vector3(1.6f,.014f,.57f),.004f);
            foreach(int side in new[]{-1,1})
            {batch.Cylinder(metal,new Vector3(side*.57f,bottom+.9f,z-.31f),new Vector3(side*.57f,bottom+1.16f,z-.31f),.025f);batch.Cylinder(metal,new Vector3(side*.57f,bottom+1.16f,z-.31f),new Vector3(side*.57f,bottom+1.16f,z+.02f),.025f);}
            var col=NewGo("washbasin_camera",root);col.transform.localPosition=new Vector3(0,bottom+.5f,z);col.AddComponent<BoxCollider>().size=new Vector3(1.86f,1,.8f);
        }
        else
        {
            float z=-d/2+.65f;batch.Bevel(timber,new Vector3(0,bottom+.48f,z),new Vector3(w-.8f,.09f,.5f),.025f);
            foreach(int side in new[]{-1,1})batch.Bevel(timber,new Vector3(side*(w/2-.7f),bottom+.25f,z),new Vector3(.14f,.44f,.40f),.02f);
            batch.Bevel(timber,new Vector3(0,bottom+.85f,z-.2f),new Vector3(w-.85f,.48f,.08f),.025f);
            var col=NewGo("pavilion_bench",root);col.transform.localPosition=new Vector3(0,bottom+.55f,z);col.AddComponent<BoxCollider>().size=new Vector3(w-.8f,1.1f,.6f);
        }
        batch.Flush(root);ResidenceSign(root,b,new Vector3(0,bottom+height-.28f,d/2-.09f));
    }
}
