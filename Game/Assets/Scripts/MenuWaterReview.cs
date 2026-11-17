using System;
using System.Collections;
using UnityEngine;

public sealed class MenuWaterReview:MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null;yield return null;
        var fresh=new WaterMotionState();var aged=new WaterMotionState{Seconds=86400,X=-1000,Z=500};
        fresh.Step(.02f,1);double x=aged.X,z=aged.Z;aged.Step(.02f,1);
        if(Math.Abs((aged.X-x)-fresh.X)>1e-9||Math.Abs((aged.Z-z)-fresh.Z)>1e-9)throw new Exception("Water speed accelerates with session age");
        var settings=HelpSidebar.Live;settings.OpenWeather();settings.OpenSettings();
        if(!HelpSidebar.IsOpen||HelpSidebar.WeatherOpen||Cursor.lockState!=CursorLockMode.None)throw new Exception("Settings entry did not leave weather drawer");
        settings.Close();
        var before=Shader.GetGlobalVector("_WaterFlow");float clock=Shader.GetGlobalVector("_WaterMotion").x;
        yield return new WaitForSeconds(1.2f);
        var delta=Shader.GetGlobalVector("_WaterFlow")-before;float elapsed=Shader.GetGlobalVector("_WaterMotion").x-clock;
        if(elapsed<.8f||elapsed>1.4f||delta.magnitude>.04f||delta.magnitude<.01f)throw new Exception("Water shader motion rate invalid");
        var block=new MaterialPropertyBlock();int pond=0,bowls=0;
        foreach(var r in FindObjectsOfType<MeshRenderer>())if(r.name=="NaturalWaterSurface")
        {r.GetPropertyBlock(block);float flow=block.GetFloat("_FlowScale"),speed=block.GetFloat("_WaveSpeed");if(flow>.121f||speed>.551f)throw new Exception("Calm basin inherited fast river motion");if(flow>0)pond++;else bowls++;}
        if(pond!=1||bowls<3)throw new Exception("Missing water body motion profiles");
        Debug.Log($"[MenuMotionCheck] PASS normal settings entry/cursor; fresh vs24h equal velocity; shader drift {delta.magnitude:F4}m/{elapsed:F2}s; {pond} calm pond, {bowls} still bowls");
        while(true){yield return new WaitForEndOfFrame();if(Input.GetKeyDown(KeyCode.Escape))Debug.Log($"[MenuEscape] settings={HelpSidebar.IsOpen} weather={HelpSidebar.WeatherOpen} map={GetComponent<Minimap>().LargeOpen} dialogue={DialogueManager.Active}");}
    }
}
