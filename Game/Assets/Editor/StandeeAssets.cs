using System;using System.IO;using UnityEditor;using UnityEngine;
public static class StandeeAssets
{
 public static void BuildBatch()
 {
  var data=new PortraitStandee.BoundsData{entries=new PortraitStandee.Entry[CharacterDef.PortraitForms.Length]};
  var manifest=new PortraitAssets.Manifest{entries=new PortraitAssets.Entry[CharacterDef.PortraitForms.Length]};int i=0;
  foreach(var character in CharacterDef.PortraitForms)
  {
   var path="Assets/Resources/Portraits/"+character.id+"-cut.png";var bytes=File.ReadAllBytes(path);var t=new Texture2D(2,2);t.LoadImage(bytes);var pixels=t.GetPixels32();int x0=t.width,y0=t.height,x1=0,y1=0;
   for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)if(pixels[y*t.width+x].a>16){x0=Mathf.Min(x0,x);y0=Mathf.Min(y0,y);x1=Mathf.Max(x1,x);y1=Mathf.Max(y1,y);}
   if(x0>=x1||y0>=y1)throw new Exception("Empty standee "+character.id);
   var alpha=new byte[pixels.Length];for(int k=0;k<pixels.Length;k++)alpha[k]=pixels[k].a;
   manifest.entries[i]=new PortraitAssets.Entry{id=character.id,fileSha256=PortraitAssets.Hash(bytes),alphaSha256=PortraitAssets.Hash(alpha),width=t.width,height=t.height};
   data.entries[i++]=new PortraitStandee.Entry{id=character.id,uv=new Rect(x0/(float)t.width,y0/(float)t.height,(x1-x0+1f)/t.width,(y1-y0+1f)/t.height)};
   UnityEngine.Object.DestroyImmediate(t);
  }
  File.WriteAllText("Assets/Resources/PortraitStandeeBounds.json",JsonUtility.ToJson(data,true));
  File.WriteAllText("Assets/Resources/PortraitAssetManifest.json",JsonUtility.ToJson(manifest,true));
  AssetDatabase.Refresh();Debug.Log("[StandeeAssets] PASS "+i+" portrait alpha bounds and source/alpha SHA256 manifest");
 }
}
