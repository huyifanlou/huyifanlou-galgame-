using UnityEngine;

public static class GameFonts
{
    static Font map,serif,heading;
    public static Font Map=>map??(map=Load("MapSans"));
    public static Font Serif=>serif??(serif=Load("StorySerif"));
    public static Font Heading=>heading??(heading=Load("MapHeading"));
    static Font Load(string name){var font=Resources.Load<Font>("Fonts/"+name);if(font==null)throw new System.Exception("Missing bundled font "+name);return font;}
    public static int StoryChoice
    {get=>Mathf.Clamp(Args.I("-storyfont")??PlayerPrefs.GetInt(GameInput.Prefix+"StoryFont",0),0,1);set{PlayerPrefs.SetInt(GameInput.Prefix+"StoryFont",Mathf.Clamp(value,0,1));PlayerPrefs.Save();}}
    public static Font Story=>StoryChoice==0?Serif:Map;
}
[DefaultExecutionOrder(-9000)]
public class FontSystemDriver:MonoBehaviour
{void OnGUI(){GUI.skin.font=GameFonts.Map;}}
