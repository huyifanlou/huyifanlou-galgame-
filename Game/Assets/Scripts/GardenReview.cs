using System;
using System.Collections;
using UnityEngine;

public sealed class GardenReview:MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null;yield return null;
        var world=GetComponent<WorldBuilder>();var weather=GetComponent<Weather>();int spots=0,benches=0,parkFacing=0;
        foreach(var light in world.LampLights)if(light.type==LightType.Spot)
        {
            spots++;if(Vector3.Dot(light.transform.forward,Vector3.down)<.999f)throw new Exception("Street lamp beam is not downward");
            if(Mathf.Abs(light.transform.localPosition.x)>.001f||Mathf.Abs(light.transform.localPosition.z)>.001f)throw new Exception("Street diffuser/light axes differ");
            if(light.transform.parent.Find("pool")!=null)throw new Exception("Legacy floor glow remains");
        }
        if(spots!=world.W.LAMPS.Length)throw new Exception("Some street lamps were not rebuilt");
        foreach(var prop in world.W.PROPS)if(prop.type=="bench")
        {
            benches++;
            if(prop.x>10&&prop.x<58&&prop.z>-61&&prop.z<-19)
            {var towards=new Vector3(34-prop.x,0,-40-prop.z).normalized;var forward=Quaternion.Euler(0,prop.ry*Mathf.Rad2Deg,0)*Vector3.forward;if(Vector3.Dot(towards,forward)<.85f)throw new Exception("Park bench faces away from pond");parkFacing++;}
        }
        foreach(var renderer in world.Root.GetComponentsInChildren<MeshRenderer>())foreach(var m in renderer.sharedMaterials)
            if(m==null||m.shader==null||!m.shader.isSupported)throw new Exception("Unsupported world material at "+renderer.name);
        var original=weather.Current;bool wasIndoor=weather.Indoor;float originalHour=GetComponent<TimeOfDay>().GameMinutes;
        foreach(var kind in new[]{Weather.Kind.Rain,Weather.Kind.Snow,Weather.Kind.Clear})
        {
            weather.Restore(kind);yield return null;
            if(float.IsNaN(RenderSettings.fogColor.r)||RenderSettings.fogEndDistance<=RenderSettings.fogStartDistance)throw new Exception("Weather fog invalid");
            weather.Indoor=true;yield return null;
            foreach(var ps in weather.GetComponentsInChildren<ParticleSystem>())if(ps.name=="Rain"||ps.name=="Snow")if(ps.isEmitting||ps.particleCount>0)throw new Exception("Precipitation leaks indoors");
            weather.Indoor=false;
        }
        weather.Indoor=wasIndoor;weather.Restore(original);GetComponent<TimeOfDay>().GameMinutes=originalHour;
        Debug.Log($"[GardenCheck] PASS street lamps {spots}: aligned/downward/no fake pools; benches {benches}, pond facing {parkFacing}; weather rain/snow/clear and indoor particle clearing; supported materials");
    }
}
