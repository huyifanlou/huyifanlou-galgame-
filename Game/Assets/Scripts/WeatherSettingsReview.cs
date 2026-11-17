using System;
using System.Collections;
using UnityEngine;

// QA settings and key bindings use separate preference keys from the author's game.
public sealed class WeatherSettingsReview:MonoBehaviour
{
    void LateUpdate()
    {if(Args.Has("-weatheruicheck")&&Input.GetKeyDown(GameInput.Find("Weather").Key))Debug.Log("[WeatherUIKey] actual bound key / panel "+(HelpSidebar.WeatherOpen?"open":"closed"));}
    IEnumerator Start()
    {
        yield return null;yield return null;
        var weather=GetComponent<Weather>();var settings=HelpSidebar.Live;
        if(Args.Has("-weatheruireload"))
        {
            if(Mathf.Abs(weather.RainStrength-.3f)>.001f||Mathf.Abs(weather.SnowStrength-.7f)>.001f||Mathf.Abs(weather.ParticleScale-1.6f)>.001f)throw new Exception("Weather preferences did not survive restart");
            settings.OpenWeather();Debug.Log("[WeatherUIReload] PASS persisted rain30/snow70/size160");yield break;
        }
        weather.Configure(.3f,.7f,1.6f);weather.TransitionTo(Weather.Kind.Rain);settings.OpenWeather();
        if(!HelpSidebar.WeatherOpen||Cursor.lockState!=CursorLockMode.None)throw new Exception("Weather modal did not release cursor");
        var position=GetComponent<PlayerRig>().Position;yield return new WaitForSeconds(1.2f);
        if((GetComponent<PlayerRig>().Position-position).sqrMagnitude>.001f)throw new Exception("Weather panel moved player");
        if(Mathf.Abs(weather.RainAmt-.3f)>.02f)throw new Exception("Rain strength did not drive actual weather");
        var save=new ProgressSave();weather.WriteSettings(save);var json=JsonUtility.ToJson(save);
        weather.Configure(1,1,1,false);weather.ReadSettings(JsonUtility.FromJson<ProgressSave>(json));
        if(Mathf.Abs(weather.RainStrength-.3f)>.001f||Mathf.Abs(weather.ParticleScale-1.6f)>.001f)throw new Exception("Weather save settings did not roundtrip");
        weather.ReadSettings(new ProgressSave());if(weather.RainStrength!=1||weather.ParticleScale!=1)throw new Exception("Old save weather defaults invalid");
        weather.Configure(0,0,.5f,false);weather.TransitionTo(Weather.Kind.Snow);yield return new WaitForSeconds(2.3f);
        if(weather.RainAmt>.01f||weather.SnowAmt>.01f)throw new Exception("Zero precipitation should stop emission");
        weather.Configure(float.NaN,float.PositiveInfinity,99,false);if(weather.RainStrength!=1||weather.SnowStrength!=1||weather.ParticleScale!=2)throw new Exception("Weather setting bounds invalid");
        settings.Close();if(HelpSidebar.IsOpen)throw new Exception("Weather modal did not close");
        weather.Configure(.3f,.7f,1.6f);weather.FlushPreferences();weather.Restore(Weather.Kind.Rain);settings.OpenWeather();
        Debug.Log("[WeatherUICheck] PASS modal/cursor, live strength/zero emission, save roundtrip/old save fallback, finite bounds, isolated preferences written");
    }
}
