using UnityEngine;

// 将真实铺装边界烘成草地的柔和接壤遮罩；不额外叠透明地面，不修改碰撞或道路布局。
public static class LandscapeSurface
{
    public static Material Create(WorldData world)
    {
        const int size=1024;float span=world.BOUND*2+8,half=span/2;var mask=new float[size*size];
        void Rect(float x,float z,float w,float d,float yaw)
        {
            float c=Mathf.Cos(yaw),s=Mathf.Sin(yaw),ex=Mathf.Abs(c)*w/2+Mathf.Abs(s)*d/2+1,ez=Mathf.Abs(s)*w/2+Mathf.Abs(c)*d/2+1;
            int x0=Mathf.Clamp(Mathf.FloorToInt((x-ex+half)/span*size),0,size-1),x1=Mathf.Clamp(Mathf.CeilToInt((x+ex+half)/span*size),0,size-1);
            int y0=Mathf.Clamp(Mathf.FloorToInt((z-ez+half)/span*size),0,size-1),y1=Mathf.Clamp(Mathf.CeilToInt((z+ez+half)/span*size),0,size-1);
            for(int py=y0;py<=y1;py++)for(int px=x0;px<=x1;px++)
            {
                float dx=(px+.5f)/size*span-half-x,dz=(py+.5f)/size*span-half-z;
                float lx=c*dx-s*dz,lz=s*dx+c*dz;
                float dist=new Vector2(Mathf.Max(0,Mathf.Abs(lx)-w/2),Mathf.Max(0,Mathf.Abs(lz)-d/2)).magnitude;
                float value=1-Mathf.SmoothStep(.02f,1,dist);int k=py*size+px;mask[k]=Mathf.Max(mask[k],value);
            }
        }
        foreach(var road in world.ROADS)
        {float width=road.width+(road.sidewalk?2.96f:0),len=road.to-road.from,mid=(road.from+road.to)/2;if(road.axis=="x")Rect(mid,road.at,len,width,0);else Rect(road.at,mid,width,len,0);}
        foreach(var prop in world.PROPS)if(prop.type=="plaza")Rect(prop.x,prop.z,prop.w,prop.d,prop.ry);
        // 树根周围以渐隐圆斑融入草地，不使用方形地面贴片。
        foreach(var tree in world.TREES)
        {
            float radius=.85f;int px0=Mathf.Clamp((int)((tree.x-radius+half)/span*size),0,size-1),px1=Mathf.Clamp((int)((tree.x+radius+half)/span*size)+1,0,size-1);
            int py0=Mathf.Clamp((int)((tree.z-radius+half)/span*size),0,size-1),py1=Mathf.Clamp((int)((tree.z+radius+half)/span*size)+1,0,size-1);
            for(int py=py0;py<=py1;py++)for(int px=px0;px<=px1;px++){float dx=(px+.5f)/size*span-half-tree.x,dz=(py+.5f)/size*span-half-tree.z;float fade=1-Mathf.SmoothStep(0,1,Mathf.Sqrt(dx*dx+dz*dz)/radius);int k=py*size+px;mask[k]=Mathf.Max(mask[k],fade*.8f);}
        }
        var pixels=new Color32[size*size];int marked=0;for(int i=0;i<pixels.Length;i++){byte v=(byte)Mathf.RoundToInt(mask[i]*255);pixels[i]=new Color32(v,v,v,255);if(v>0)marked++;}
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false,true){name="Landscape_PavingContact",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};texture.SetPixels32(pixels);texture.Apply(false,true);
        var material=new Material(DetailSurface.Mat("meadow","#87966d")){name="Landscape_Meadow"};material.SetTexture("_TerrainEdgeTex",texture);material.SetFloat("_TerrainSurface",1);material.SetFloat("_TerrainSpan",span);
        Debug.Log($"[LandscapeSurface] 草地接壤遮罩 {size}×{size}，跨度 {span:F0}m，有效像素 {marked}");return material;
    }
}
