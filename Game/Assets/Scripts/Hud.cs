using UnityEngine;
public class Hud:MonoBehaviour
{
    public const string BUILD_VERSION="u0.3";public TimeOfDay Time;Texture2D topFade,bottomFade;
    public void Init(TimeOfDay time){Time=time;topFade=GameUI.Fade(.68f,0);bottomFade=GameUI.Fade(0,.50f);}
    static string K(string action)=>GameInput.Name(GameInput.Find(action).Key);
    void OnGUI()
    {
        if(Time==null||Args.NoHud||IntroScreen.Live||DialogueManager.Active||CharacterPicker.IsOpen||HelpSidebar.IsOpen||ProgressManager.TrackingOpen||ProgressManager.MenuOpen)return;
        GameUI.Begin(-5);
        GUI.DrawTexture(new Rect(0,0,1600,84),topFade,ScaleMode.StretchToFill);
        GameUI.FloatingText(new Rect(26,12,300,34),"他山之石",25,GameUI.Paper,true);
        GameUI.FloatingText(new Rect(28,48,340,18),"STONES FROM ANOTHER HILL",10,GameUI.Gold);
        GameUI.FloatingText(new Rect(425,22,330,30),"第 "+Time.DayNum+" 天   "+Mathf.FloorToInt(Time.Hour).ToString("00")+":"+Mathf.FloorToInt(Time.GameMinutes%60).ToString("00")+"   "+(string.IsNullOrEmpty(Time.WeatherName)?"晴":Time.WeatherName),17,GameUI.Paper);
        if(GameUI.FlatAction(new Rect(810,15,155,43),"回顾  "+K("Log")))GetComponent<DialogueManager>().OpenArchive();
        if(GameUI.FlatAction(new Rect(982,15,120,43),"地图  "+K("Map")))GetComponent<Minimap>().Toggle();
        if(GameUI.FlatAction(new Rect(1124,15,120,43),"手记  "+K("Journal")))GetComponent<QuestJournal>().Open();
        if(GameUI.FlatAction(new Rect(1266,15,120,43),"存档  "+K("Saves")))GetComponent<ProgressManager>().OpenSaves();
        if(GameUI.FlatAction(new Rect(1408,15,165,43),"设置  Esc"))HelpSidebar.Live.OpenSettings();
        GUI.DrawTexture(new Rect(0,838,1600,62),bottomFade,ScaleMode.StretchToFill);
        string movement=K("Forward")+"/"+K("Left")+"/"+K("Back")+"/"+K("Right");
        GameUI.FloatingText(new Rect(26,857,1530,27),"移动 "+movement+"   鼠标环顾   交互 "+K("Interact")+"   剧情 "+K("Story")+"   手记 "+K("Journal")+"   图鉴 "+K("Characters")+"   天气 "+K("Weather")+"   Esc 设置",15,GameUI.Paper);
        var interiors=GetComponent<InteriorManager>();
        string prompt=interiors.Indoor?(interiors.NearExit?"离开「"+interiors.Current.Name+"」":null):Time.Near==null||!interiors.CanEnter(Time.Near)?null:"进入「"+Time.Near+"」";
        if(prompt!=null&&GameUI.FlatAction(new Rect(630,788,340,42),K("Interact")+"  "+prompt)){if(interiors.Indoor)interiors.Exit();else interiors.Enter(Time.Near);}
        if(!string.IsNullOrEmpty(Time.Toast))GameUI.FloatingText(new Rect(400,748,800,34),Time.Toast,17,GameUI.Paper,false,true);
        GameUI.End();
    }
}
