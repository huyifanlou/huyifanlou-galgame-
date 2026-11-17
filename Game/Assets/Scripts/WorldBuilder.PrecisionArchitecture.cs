using System.Collections.Generic;using UnityEngine;
public partial class WorldBuilder
{
    class FacadeOpening{public float x,w,low,high;public bool door;}
    void BuildPrecisionArchitecture(Transform root,BuildingDef b,bool home=false)
    {
        if(PavilionFootprint.Uses(b)){PrecisionPavilion(root,b);return;}
        if(b.id=="st_platform"){PrecisionPlatform(root,b);return;}
        if(b.style=="stand"){PrecisionStand(root,b);return;}
        bool school=SchoolExterior.Uses(b),residential=home||ResidentialExterior.Uses(b),shop=ShopIds.Contains(b.id)||b.id.StartsWith("o_");
        var batch=new RegionMeshBatch();float w=b.size[0],h=b.size[1],d=b.size[2],bottom=-h/2,front=d/2;
        string wallKind=(school&&b.style=="collegiate")||b.id=="st_hall"||b.id.StartsWith("o_")?"brick":"plaster";
        var wall=DetailSurface.Mat(wallKind,b.wall);var trim=DetailSurface.Mat("stone",b.trim);var plinth=DetailSurface.Mat("stone",b.plinth);var roof=DetailSurface.Mat("roof",b.roof);
        var metal=DetailSurface.Mat("metal","#65716e");var wood=DetailSurface.Mat("timber","#826b50");
        EnsureResidenceGlass();if(SchoolGlassLit==null){SchoolGlassLit=new Material(DetailSurface.Mat("glass","#658b98")){name="precision_school_glass_lit"};SchoolGlassLit.EnableKeyword("_EMISSION");SchoolGlassLit.SetColor("_EmissionColor",Color.black);SchoolGlassDark=DetailSurface.Mat("glass","#526e78");}
        Material glass=school?SchoolGlassLit:ResidenceGlassLit,dark=school?SchoolGlassDark:ResidenceGlassDark;
        float floorH=h/Mathf.Max(1,b.rows);
        for(int face=0;face<4;face++)
        {
            float span=face%2==0?w:d,depth=face%2==0?d:w,yaw=face*90;var q=Quaternion.Euler(0,yaw,0);float z=depth/2;
            void Piece(Material mat,Vector3 pos,Vector3 size,bool bevel=false)
            {if(size.x<.005f||size.y<.005f||size.z<.005f)return;if(bevel)batch.Bevel(mat,q*pos,size,.012f,yaw);else batch.Box(mat,q*pos,size,yaw);}
            int cols=face%2==0?Mathf.Max(2,b.cols):Mathf.Max(2,Mathf.RoundToInt(span/3.4f));float cell=span/cols;
            for(int row=0;row<b.rows;row++)
            {
                float y0=bottom+row*floorH;var openings=new List<FacadeOpening>();
                for(int col=0;col<cols;col++)
                {
                    float x=-span/2+(col+.5f)*cell,ww=Mathf.Min(1.65f,cell*.55f),hh=Mathf.Min(1.65f,floorH*.52f);
                    if(face==0&&Mathf.Abs(x)<1.2f+ww/2)continue;
                    openings.Add(new FacadeOpening{x=x,w=ww,low=floorH*.56f-hh/2,high=floorH*.56f+hh/2});
                }
                if(face==0&&row==0)openings.Add(new FacadeOpening{x=0,w=1.86f,low=0,high=Mathf.Min(2.47f,floorH-.1f),door=true});
                openings.Sort((a,c)=>(a.x-a.w/2).CompareTo(c.x-c.w/2));float cursor=-span/2;
                foreach(var opening in openings)
                {
                    float left=opening.x-opening.w/2,right=opening.x+opening.w/2;
                    Piece(wall,new Vector3((cursor+left)/2,y0+floorH/2,z-.16f),new Vector3(left-cursor,floorH,.32f));
                    Piece(wall,new Vector3(opening.x,y0+opening.low/2,z-.16f),new Vector3(opening.w,opening.low,.32f));
                    Piece(wall,new Vector3(opening.x,y0+(opening.high+floorH)/2,z-.16f),new Vector3(opening.w,floorH-opening.high,.32f));cursor=right;
                    if(opening.door)continue;float cy=y0+(opening.low+opening.high)/2,hh=opening.high-opening.low,ww=opening.w;
                    Piece((row+face+Mathf.RoundToInt(opening.x))%5==0?dark:glass,new Vector3(opening.x,cy,z-.13f),new Vector3(ww-.06f,hh-.06f,.055f));
                    var gasket=DetailSurface.Mat("rubber","#3d4a49");
                    foreach(int side in new[]{-1,1})
                    {
                        Piece(gasket,new Vector3(opening.x+side*(ww/2-.035f),cy,z-.045f),new Vector3(.045f,hh,.07f));
                        Piece(trim,new Vector3(opening.x+side*(ww/2+.045f),cy,z+.035f),new Vector3(.09f,hh+.19f,.15f),true);
                        Piece(trim,new Vector3(opening.x,cy+side*(hh/2+.045f),z+.035f),new Vector3(ww,.09f,.15f),true);
                    }
                    Piece(trim,new Vector3(opening.x,cy,z-.025f),new Vector3(.045f,hh,.13f));
                    Piece(trim,new Vector3(opening.x,cy+hh*.18f,z-.025f),new Vector3(ww,.04f,.13f));
                    Piece(trim,new Vector3(opening.x,cy-hh/2-.14f,z+.12f),new Vector3(ww+.32f,.13f,.43f),true);
                    Piece(metal,new Vector3(opening.x+ww*.13f,cy-hh*.10f,z+.035f),new Vector3(.018f,.13f,.035f),true);
                    // Small sill supports and a projecting drip edge create readable contact shadows.
                    foreach(int side in new[]{-1,1})Piece(trim,new Vector3(opening.x+side*ww*.32f,cy-hh/2-.27f,z+.10f),new Vector3(.14f,.18f,.27f),true);
                    Piece(metal,new Vector3(opening.x,cy+hh/2+.13f,z+.12f),new Vector3(ww+.22f,.035f,.25f),true);
                }
                Piece(wall,new Vector3((cursor+span/2)/2,y0+floorH/2,z-.16f),new Vector3(span/2-cursor,floorH,.32f));
                if(row>0)Piece(trim,new Vector3(0,y0-.01f,z+.045f),new Vector3(span+.06f,.12f,.17f),true);
            }
            Piece(plinth,new Vector3(0,bottom+.18f,z+.02f),new Vector3(span,.36f,.16f));
            Piece(trim,new Vector3(0,h/2-.15f,z+.07f),new Vector3(span+.18f,.21f,.26f),true);
        }
        foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})batch.Bevel(trim,new Vector3(side*(w/2-.09f),0,end*(d/2-.09f)),new Vector3(.22f,h,.22f),.016f);
        for(int floor=0;floor<=b.rows;floor++)batch.Box(plinth,new Vector3(0,bottom+floor*floorH+.04f,0),new Vector3(w-.34f,.08f,d-.34f));
        bool pitched=b.style=="gable"||b.style=="collegiate"||b.style=="library"||b.style=="hall"||b.style=="dorm"||home;
        float eave=h/2+.04f,rise=Mathf.Min(d*.33f,h*.24f);
        if(pitched)
        {
            var roofMesh=MeshFactory.HipRoof(w,d,rise,.48f,2);batch.Add(roof,roofMesh,new Vector3(0,eave,0),Vector3.one,Quaternion.identity);
            var collider=NewGo("precision_roof_camera",root);collider.transform.localPosition=new Vector3(0,eave,0);collider.AddComponent<MeshCollider>().sharedMesh=roofMesh;
            foreach(int side in new[]{-1,1})
            {
                batch.Bevel(trim,new Vector3(0,eave-.08f,side*(d/2+.30f)),new Vector3(w+.90f,.18f,.24f),.025f);
                batch.Cylinder(metal,new Vector3(-w/2-.3f,eave+.02f,side*(d/2+.46f)),new Vector3(w/2+.3f,eave+.02f,side*(d/2+.46f)),.045f);
            }
            float ridge=Mathf.Max(.5f,w-d*.55f);for(float x=-ridge/2;x<=ridge/2;x+=.34f)batch.Bevel(roof,new Vector3(x,eave+rise+.045f,0),new Vector3(.31f,.12f,.26f),.03f);
        }
        else
        {
            batch.Bevel(roof,new Vector3(0,eave+.08f,0),new Vector3(w+.28f,.16f,d+.28f),.025f);
            foreach(int side in new[]{-1,1})
            {
                batch.Bevel(trim,new Vector3(0,eave+.28f,side*(d/2+.04f)),new Vector3(w+.32f,.32f,.18f),.018f);
                batch.Bevel(trim,new Vector3(side*(w/2+.04f),eave+.28f,0),new Vector3(.18f,.32f,d),.018f);
            }
            var vent=DetailSurface.Mat("metal","#8b9892");batch.Bevel(vent,new Vector3(-w*.23f,eave+.48f,-d*.15f),new Vector3(1.1f,.8f,.85f),.05f);
            for(int i=0;i<7;i++)batch.Box(metal,new Vector3(-w*.23f,eave+.22f+i*.082f,-d*.15f+.44f),new Vector3(.88f,.022f,.035f));
        }
        foreach(int side in new[]{-1,1})batch.Cylinder(metal,new Vector3(side*(w/2-.22f),bottom+.25f,-front-.10f),new Vector3(side*(w/2-.22f),eave,-front-.10f),.047f);
        // Repeated gutter brackets are actual geometry, not dark stripes in the wall texture.
        foreach(int side in new[]{-1,1})
        {
            float px=side*(w/2-.22f),pz=-front-.10f;
            for(float y=bottom+.7f;y<eave-.1f;y+=1.65f)batch.Bevel(metal,new Vector3(px,y,pz+.012f),new Vector3(.13f,.055f,.12f),.01f);
            batch.Cylinder(metal,new Vector3(px,bottom+.25f,pz),new Vector3(px,bottom+.12f,pz-.16f),.047f);
        }
        if(pitched)
        {
            int rafters=Mathf.CeilToInt(w/.80f);
            foreach(int side in new[]{-1,1})for(int i=0;i<=rafters;i++)batch.Bevel(wood,new Vector3(-w/2+i*w/rafters,eave-.16f,side*(d/2+.12f)),new Vector3(.065f,.15f,.40f),.008f);
        }
        if(wallKind=="brick")
        {
            for(float y=bottom+.55f;y<h/2-.35f;y+=.66f)foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})
            {batch.Bevel(trim,new Vector3(side*(w/2-.13f),y,end*(d/2+.025f)),new Vector3(.40f,.25f,.10f),.012f);batch.Bevel(trim,new Vector3(side*(w/2+.025f),y,end*(d/2-.13f)),new Vector3(.10f,.25f,.40f),.012f);}
        }
        float lift=b.style=="library"||b.style=="hall"?.45f:0;
        float doorZ=front+b.EntranceExtra;
        if(b.EntranceExtra>0)
        {
            foreach(int side in new[]{-1,1})batch.Bevel(trim,new Vector3(side*1.12f,bottom+1.45f,front+b.EntranceExtra/2),new Vector3(.22f,2.9f,b.EntranceExtra+.1f),.02f);
            batch.Bevel(trim,new Vector3(0,bottom+2.9f,front+b.EntranceExtra/2),new Vector3(2.5f,.22f,b.EntranceExtra+.1f),.02f);
            var vestibule=NewGo("precision_vestibule_camera",root);vestibule.transform.localPosition=new Vector3(0,bottom+1.45f,front+b.EntranceExtra/2);vestibule.AddComponent<BoxCollider>().size=new Vector3(2.5f,2.9f,b.EntranceExtra+.1f);
        }
        PrecisionExteriorDoor(batch,new Vector3(0,bottom+lift,doorZ),trim,wood,dark,metal);
        if(lift>0)
        {
            float pw=SchoolExterior.PorchWidth(b),ph=SchoolExterior.PostHeight(b);
            batch.Box(plinth,new Vector3(0,bottom+.225f,front+1.5f),new Vector3(pw,.45f,3));
            for(int i=0;i<3;i++){float top=.48f-i*.13f;batch.Bevel(plinth,new Vector3(0,bottom+top/2,front+3.25f+i*.4f),new Vector3(pw,top,.44f),.018f);}
            foreach(var post in SchoolExterior.Posts(b))
            {var start=post+Vector3.up*bottom;batch.Cylinder(trim,start,start+Vector3.up*ph,.23f);batch.Bevel(plinth,start+Vector3.up*.1f,new Vector3(.60f,.20f,.60f),.02f);batch.Bevel(trim,start+Vector3.up*(ph-.10f),new Vector3(.64f,.20f,.64f),.02f);}
            var porch=RegionMeshBatch.Gable(pw+.8f,1.5f,3.6f);var pos=new Vector3(0,bottom+.45f+ph+.35f,front+1.6f);batch.Add(roof,porch,pos,Vector3.one,Quaternion.identity);
            var pc=NewGo("precision_portico_camera",root);pc.transform.localPosition=pos;pc.AddComponent<MeshCollider>().sharedMesh=porch;
        }
        else
        {
            float half=shop?Mathf.Min(w*.42f,6):school?2.65f:1.5f,depth=school?1.9f:1.2f,height=school?SchoolExterior.PostHeight(b):2.65f;
            batch.Bevel(roof,new Vector3(0,bottom+height,doorZ+depth/2),new Vector3(half*2,.16f,depth),.035f);
            foreach(var post in SchoolExterior.Posts(b))batch.Bevel(trim,post+new Vector3(0,bottom+height/2,0),new Vector3(.20f,height,.20f),.012f);
            batch.Bevel(plinth,new Vector3(0,bottom+.08f,doorZ+.5f),new Vector3(1.9f,.16f,.9f),.012f);
            var pc=NewGo("precision_canopy_camera",root);pc.transform.localPosition=new Vector3(0,bottom+height,doorZ+depth/2);pc.AddComponent<BoxCollider>().size=new Vector3(half*2,.16f,depth);
        }
        foreach(var post in SchoolExterior.Posts(b))
        {var pc=NewGo("precision_post_camera",root);float ph=SchoolExterior.PostHeight(b);pc.transform.localPosition=post+new Vector3(0,bottom+ph/2,0);pc.AddComponent<BoxCollider>().size=new Vector3(.46f,ph,.46f);}
        if(shop||school||b.id=="st_hall")
        {
            float width=Mathf.Min(w*.62f,5),sy=bottom+Mathf.Min(h-.7f,school?3.95f:3.25f);
            batch.Bevel(DetailSurface.Mat("timber","#425e53"),new Vector3(0,sy,doorZ+.14f),new Vector3(width,.64f,.24f),.025f);
            ResidenceSign(root,b,new Vector3(0,sy,doorZ+.275f));
        }
        if(b.style=="collegiate")
        {
            float cy=h/2-1.30f;batch.Bevel(trim,new Vector3(0,cy,front+.15f),new Vector3(2.1f,2.1f,.24f),.04f);
            Face(root,1.78f,1.78f,new Vector3(0,cy,front+.285f),ClockMat(),0,false);
        }
        if(residential&&b.rows>=3)
        {
            for(int row=1;row<b.rows;row++)foreach(int side in new[]{-1,1})
            {float bx=side*w*.30f,y=bottom+row*floorH+.03f,bw=Mathf.Min(2.6f,w*.27f);batch.Bevel(trim,new Vector3(bx,y,front+.45f),new Vector3(bw,.12f,.98f),.018f);
             batch.Cylinder(metal,new Vector3(bx-bw/2,y+.88f,front+.86f),new Vector3(bx+bw/2,y+.88f,front+.86f),.025f);
             for(int i=0;i<7;i++){float x=bx-bw/2+i*bw/6;batch.Cylinder(metal,new Vector3(x,y+.1f,front+.86f),new Vector3(x,y+.88f,front+.86f),.014f);}}
        }
        if(home)ResidenceHomeYard(root,batch,b);
        SchoolCraftExterior(root,batch,b);
        batch.Flush(root);
    }
    void PrecisionExteriorDoor(RegionMeshBatch batch,Vector3 at,Material frame,Material wood,Material glass,Material metal)
    {
        batch.Bevel(wood,at+new Vector3(0,1.13f,.07f),new Vector3(1.78f,2.26f,.14f),.025f);
        foreach(int side in new[]{-1,1})
        {
            batch.Bevel(frame,at+new Vector3(side*.98f,1.2f,.12f),new Vector3(.16f,2.4f,.28f),.018f);
            batch.Bevel(glass,at+new Vector3(side*.42f,1.58f,.155f),new Vector3(.60f,.84f,.025f),.008f);
            batch.Bevel(metal,at+new Vector3(side*.13f,1.04f,.165f),new Vector3(.07f,.27f,.045f),.015f);
            batch.Cylinder(metal,at+new Vector3(side*.13f,1.1f,.19f),at+new Vector3(side*.13f,1.1f,.25f),.02f);
            batch.Cylinder(metal,at+new Vector3(side*.13f,1.1f,.25f),at+new Vector3(side*.25f,1.1f,.25f),.018f);
            batch.Bevel(wood,at+new Vector3(side*.42f,.44f,.16f),new Vector3(.63f,.5f,.035f),.012f);
        }
        batch.Bevel(frame,at+new Vector3(0,2.39f,.12f),new Vector3(2.12f,.25f,.28f),.018f);
        batch.Box(metal,at+new Vector3(0,1.13f,.155f),new Vector3(.025f,2.26f,.025f));
    }
    void PrecisionPlatform(Transform root,BuildingDef b)
    {
        var batch=new RegionMeshBatch();float bottom=-b.size[1]/2,w=b.size[0],d=b.size[2];var stone=DetailSurface.Mat("stone",b.wall);var steel=DetailSurface.Mat("metal","#677a7d");var roof=DetailSurface.Mat("roof",b.roof);
        batch.Bevel(stone,new Vector3(0,0,0),new Vector3(w,b.size[1],d),.04f);
        batch.Bevel(roof,new Vector3(0,bottom+3.8f,0),new Vector3(w+.7f,.20f,d+.7f),.04f);
        for(float x=-w/2+1;x<w/2;x+=4)foreach(int side in new[]{-1,1})batch.Cylinder(steel,new Vector3(x,bottom+b.size[1],side*(d/2-.30f)),new Vector3(x,bottom+3.7f,side*(d/2-.30f)),.065f);
        batch.Flush(root);
    }
    void PrecisionStand(Transform root,BuildingDef b)
    {
        var batch=new RegionMeshBatch();var stone=DetailSurface.Mat("stone",b.plinth);var metal=DetailSurface.Mat("metal","#697a7a");var seat=DetailSurface.Mat("paint","#659493");float bottom=-b.size[1]/2;
        for(int row=0;row<5;row++)
        {float z=b.size[2]/2-.6f-row*.75f,y=.30f+row*.38f;foreach(int side in new[]{-1,1})batch.Box(stone,new Vector3(side*(b.size[0]/4+.575f),bottom+y/2,z),new Vector3(b.size[0]/2-1.15f,y,.75f));
         for(float x=-b.size[0]/2+.5f;x<b.size[0]/2;x+=.65f)if(Mathf.Abs(x)>1.25f)batch.Bevel(seat,new Vector3(x,bottom+y+.08f,z),new Vector3(.48f,.14f,.48f),.045f,0,3);}
        for(int i=0;i<10;i++){float y=StandAccess.Step(i);batch.Box(stone,new Vector3(0,bottom+y/2,StandAccess.Front(b)-(i+.5f)*.375f),new Vector3(2.3f,y,.375f));}
        float rear=b.size[2]/2-3.975f;
        batch.Cylinder(metal,new Vector3(-b.size[0]/2,bottom+2.65f,rear),new Vector3(b.size[0]/2,bottom+2.65f,rear),.035f);
        for(float x=-b.size[0]/2;x<=b.size[0]/2+.01f;x+=b.size[0]/8)batch.Cylinder(metal,new Vector3(x,bottom+1.82f,rear),new Vector3(x,bottom+2.65f,rear),.026f);
        batch.Flush(root);
    }
}
