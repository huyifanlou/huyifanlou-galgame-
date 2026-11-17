using System;
using System.Collections;
using UnityEngine;

// Exercise the actual timed switch path, not the instant screenshot/load shortcut.
public sealed class WeatherReview:MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null;yield return null;
        var weather=GetComponent<Weather>();var original=weather.Current;var indoor=weather.Indoor;
        weather.Indoor=false;weather.Restore(Weather.Kind.Clear);
        foreach(var kind in new[]{Weather.Kind.Rain,Weather.Kind.Snow})
        {
            weather.TransitionTo(kind);yield return new WaitForSeconds(4.8f);
            if(kind==Weather.Kind.Rain&&(weather.RainAmt<.95f||weather.Wetness<.90f))throw new Exception("Rain transition too slow");
            if(kind==Weather.Kind.Snow&&(weather.SnowAmt<.95f||weather.SnowCover<.90f))throw new Exception("Snow accumulation too slow");
            string name=kind==Weather.Kind.Rain?"Rain":"Snow";ParticleSystem system=null;
            foreach(var ps in GetComponentsInChildren<ParticleSystem>())if(ps.name==name)system=ps;
            if(system==null)throw new Exception("Missing precipitation system");
            var particles=new ParticleSystem.Particle[system.main.maxParticles];int count=system.GetParticles(particles),visible=0;
            for(int i=0;i<count;i++){var position=particles[i].position;if(position.y<GetComponent<WorldBuilder>().GroundY(position.x,position.z)+.02f)continue;var p=Camera.main.WorldToViewportPoint(position);if(p.z>1&&p.z<18&&p.x>0&&p.x<1&&p.y>0&&p.y<1)visible++;}
            if(visible<25)throw new Exception(name+" has too few camera-visible particles: "+visible);
            Debug.Log($"[WeatherCheck] {name}: active {count}, visible within18m {visible}, wet {weather.Wetness:F2}, snow {weather.SnowCover:F2}, fog {RenderSettings.fogStartDistance:F1}..{RenderSettings.fogEndDistance:F1}");
            if(!string.IsNullOrEmpty(Args.Str("-weathercheckdir")))
            {var path=System.IO.Path.GetFullPath(Args.Str("-weathercheckdir"));System.IO.Directory.CreateDirectory(path);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(path,name+"-transition.png"));yield return new WaitForEndOfFrame();}
        }
        weather.TransitionTo(Weather.Kind.Clear);yield return new WaitForSeconds(3);
        if(weather.SnowAmt>.02f||weather.SnowCover<.65f)throw new Exception("Clear should stop snowfall but preserve slow melt");
        weather.Indoor=true;yield return null;yield return null;
        foreach(var ps in GetComponentsInChildren<ParticleSystem>())if(ps.name=="Rain"||ps.name=="Snow")if(ps.isEmitting||ps.particleCount>0)throw new Exception("Precipitation leaks indoors");
        weather.Indoor=indoor;weather.Restore(original);
        Debug.Log("[WeatherCheck] PASS timed rain/snow transitions, camera-visible precipitation, lingering snow melt, indoor clearing");
    }
}
