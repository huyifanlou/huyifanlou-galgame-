using UnityEngine;

public partial class HelpSidebar
{
    public static bool WeatherOpen=>IsOpen&&Live!=null&&Live.tab==3;
    public void OpenWeather(){tab=3;Open();}
    static float WeatherSlider(float y,string label,float value,float min,float max)
    {
        GUI.Label(new Rect(1065,y,260,30),label,GameUI.Text(21,GameUI.Paper));
        GUI.Label(new Rect(1350,y,135,30),Mathf.RoundToInt(value*100)+"%",GameUI.Text(22,GameUI.Gold,false,false,TextAnchor.UpperRight));
        float step=max>1?.1f:.05f;
        if(GameUI.Button(new Rect(1065,y+37,32,32),"−"))value-=step;
        GUI.color=GameUI.Gold;value=GUI.HorizontalSlider(new Rect(1110,y+48,330,24),value,min,max);GUI.color=Color.white;
        if(GameUI.Button(new Rect(1455,y+37,32,32),"+"))value+=step;
        return Mathf.Round(Mathf.Clamp(value,min,max)*100)/100;
    }
    void DrawWeather()
    {
        var weather=GetComponent<Weather>();if(weather==null)return;
        // Keep the world visible while the existing settings modal owns input and cursor.
        GameUI.Fill(new Rect(1022,88,528,742),new Color(.01f,.02f,.03f,.25f));
        GameUI.Fill(new Rect(1030,80,520,740),new Color(.91f,.90f,.86f,.98f));
        GUI.Label(new Rect(1065,103,300,24),"WEATHER  /  天气与氛围",GameUI.Text(13,GameUI.Gold));
        GUI.Label(new Rect(1065,134,300,45),"随心调整天气",GameUI.Text(30,GameUI.Paper,true,true));
        if(GameUI.Button(new Rect(1420,103,90,36),"关闭"))Close();
        GUI.Label(new Rect(1065,183,440,26),"拖动雨量或雪量，在地图中实时预览。",GameUI.Text(16,GameUI.Muted));
        string[] kinds={"晴天","下雨","下雪"};
        for(int i=0;i<3;i++)if(GameUI.Tab(new Rect(1065+i*145,225,135,43),kinds[i],GameUI.Gold,(int)weather.Current==i))weather.TransitionTo((Weather.Kind)i);
        GameUI.Rule(1065,282,440,new Color(.25f,.30f,.33f));
        float rain=WeatherSlider(295,"雨量",weather.RainStrength,0,1);
        if(Mathf.Abs(rain-weather.RainStrength)>.001f)weather.TransitionTo(Weather.Kind.Rain);
        float[] levels={.2f,.55f,1};string[] rainNames={"小雨","中雨","大雨"},snowNames={"小雪","中雪","大雪"};
        for(int i=0;i<3;i++)if(GameUI.Button(new Rect(1065+i*145,375,135,32),rainNames[i],weather.Current==Weather.Kind.Rain&&Mathf.Abs(rain-levels[i])<.01f)){rain=levels[i];weather.TransitionTo(Weather.Kind.Rain);}
        float snow=WeatherSlider(430,"雪量",weather.SnowStrength,0,1);
        if(Mathf.Abs(snow-weather.SnowStrength)>.001f)weather.TransitionTo(Weather.Kind.Snow);
        for(int i=0;i<3;i++)if(GameUI.Button(new Rect(1065+i*145,510,135,32),snowNames[i],weather.Current==Weather.Kind.Snow&&Mathf.Abs(snow-levels[i])<.01f)){snow=levels[i];weather.TransitionTo(Weather.Kind.Snow);}
        GameUI.Rule(1065,558,440,new Color(.25f,.30f,.33f));
        float size=WeatherSlider(573,"雨丝 / 雪片大小",weather.ParticleScale,.5f,2);
        if(Mathf.Abs(rain-weather.RainStrength)>.001f||Mathf.Abs(snow-weather.SnowStrength)>.001f||Mathf.Abs(size-weather.ParticleScale)>.001f)weather.Configure(rain,snow,size);
        GUI.Label(new Rect(1065,657,440,53),"雨量和雪量也会改变云雾、光照与地表。\n颗粒大小只调整雨丝和雪片的视觉尺寸。",GameUI.Text(15,GameUI.Muted,true));
        if(GameUI.Button(new Rect(1065,724,205,42),"恢复默认"))weather.Configure(1,1,1);
        if(GameUI.Button(new Rect(1285,724,220,42),"返回全部设置")){weather.FlushPreferences();tab=2;}
        GUI.Label(new Rect(1065,782,440,25),weather.Indoor?"室内已遮挡雨雪；到室外查看天气效果。":"修改会自动保存，关闭面板后继续探索。",GameUI.Text(14,GameUI.Muted));
    }
}
