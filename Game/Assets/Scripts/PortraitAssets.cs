using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

// One source for gallery, dialogue and world standees; QA verifies the actual imported alpha.
public static class PortraitAssets
{
    [Serializable] public class Entry { public string id,fileSha256,alphaSha256;public int width,height; }
    [Serializable] public class Manifest { public Entry[] entries; }
    static readonly Dictionary<string,Texture2D> cache=new Dictionary<string,Texture2D>();
    static Manifest manifest;
    static Manifest Index
    {
        get{if(manifest==null){var data=Resources.Load<TextAsset>("PortraitAssetManifest");if(data==null)throw new Exception("Missing portrait manifest");manifest=JsonUtility.FromJson<Manifest>(data.text);}return manifest;}
    }
    public static string Hash(byte[] bytes)
    {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    public static Texture2D LoadResource(string resource)
    {
        if(resource==null)return null;
        if(cache.TryGetValue(resource,out var cached))return cached;
        var texture=Resources.Load<Texture2D>(resource);
        if(texture==null)throw new Exception("Missing portrait resource: "+resource);
        texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
        cache[resource]=texture;
        string id=resource.Replace("Portraits/","").Replace("-cut","");
        foreach(var entry in Index.entries)if(entry.id==id)Debug.Log("[PortraitAsset] "+resource+" source SHA256="+entry.fileSha256);
        return texture;
    }
    public static IEnumerator Audit()
    {
        yield return null;yield return null;
        foreach(var entry in Index.entries)
        {
            var texture=LoadResource("Portraits/"+entry.id+"-cut");
            if(texture.width!=entry.width||texture.height!=entry.height)throw new Exception("Portrait dimensions changed during import: "+entry.id);
            var previous=RenderTexture.active;
            var target=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var copy=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false,true);
            try
            {
                Graphics.Blit(texture,target);RenderTexture.active=target;
                copy.ReadPixels(new Rect(0,0,target.width,target.height),0,0);copy.Apply();
                var pixels=copy.GetPixels32();var alpha=new byte[pixels.Length];
                for(int i=0;i<pixels.Length;i++)alpha[i]=pixels[i].a;
                if(Hash(alpha)!=entry.alphaSha256)throw new Exception("Runtime portrait alpha differs from installed PNG: "+entry.id);
                Debug.Log("[PortraitAssetCheck] PASS "+entry.id+" actual GPU alpha matches source PNG / "+entry.fileSha256);
            }
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.Destroy(copy);}
        }
        Debug.Log("[PortraitAssetCheck] PASS all seven imported portrait textures match installed PNG alpha");
    }
}
