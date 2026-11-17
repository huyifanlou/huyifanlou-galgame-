using System;using UnityEngine;

public static class GameInput
{
    public class Binding
    {
        public string id,label,group;public KeyCode original,alternate;
        public Binding(string i,string l,string g,KeyCode key,KeyCode alt){id=i;label=l;group=g;original=key;alternate=alt;}
        public KeyCode Key=>(KeyCode)PlayerPrefs.GetInt(Prefix+id,(int)original);
        public string Display=>Name(Key)+(alternate==KeyCode.None?"":" / "+Name(alternate));
    }
    public static string Prefix=>(Args.Has("-uicheck")||Args.Has("-uireload")||Args.Has("-weatheruicheck")||Args.Has("-weatheruireload")||Args.Has("-renewalcheck")||Args.Has("-menuroutecheck")||Args.Has("-submissioncapture"))?"QA.Input.":"Input.";
    public static readonly Binding[] Bindings={
        new Binding("Forward","向前移动","探索",KeyCode.W,KeyCode.UpArrow),
        new Binding("Back","向后移动","探索",KeyCode.S,KeyCode.DownArrow),
        new Binding("Left","向左移动","探索",KeyCode.A,KeyCode.LeftArrow),
        new Binding("Right","向右移动","探索",KeyCode.D,KeyCode.RightArrow),
        new Binding("Run","奔跑","探索",KeyCode.LeftShift,KeyCode.RightShift),
        new Binding("CameraLeft","向左转动镜头","探索",KeyCode.Q,KeyCode.None),
        new Binding("CameraRight","向右转动镜头","探索",KeyCode.E,KeyCode.None),
        new Binding("StoryInteract","与剧情点互动","交互",KeyCode.E,KeyCode.None),
        new Binding("Interact","进入 / 离开建筑","交互",KeyCode.F,KeyCode.None),
        new Binding("Map","小镇地图","全局",KeyCode.M,KeyCode.None),
        new Binding("Characters","人物图鉴","全局",KeyCode.C,KeyCode.None),
        new Binding("Journal","任务手记","全局",KeyCode.J,KeyCode.None),
        new Binding("Travel","前往主线并继续","探索",KeyCode.G,KeyCode.None),
        new Binding("Story","剧情指引 / 继续","探索",KeyCode.B,KeyCode.None),
        new Binding("Advance","推进对白","剧情",KeyCode.Space,KeyCode.Return),
        new Binding("Log","剧情回顾","剧情",KeyCode.L,KeyCode.None),
        new Binding("Auto","自动阅读","剧情",KeyCode.A,KeyCode.None),
        new Binding("Skip","快进对白","剧情",KeyCode.S,KeyCode.None),
        new Binding("QuickSave","快速保存","全局",KeyCode.F5,KeyCode.None),
        new Binding("QuickLoad","快速读取","全局",KeyCode.F9,KeyCode.None),
        new Binding("Saves","存档与读档","全局",KeyCode.F6,KeyCode.None),
        new Binding("Settings","设置","全局",KeyCode.H,KeyCode.None),
        new Binding("FrameRate","切换帧率模式","全局",KeyCode.F10,KeyCode.None),
        new Binding("Fullscreen","切换全屏","全局",KeyCode.F11,KeyCode.None),
        new Binding("PauseTime","暂停世界时间","时间与天气",KeyCode.Alpha0,KeyCode.None),
        new Binding("NormalTime","时间速度：正常","时间与天气",KeyCode.Alpha1,KeyCode.None),
        new Binding("FastTime","时间速度：加快","时间与天气",KeyCode.Alpha2,KeyCode.None),
        new Binding("VeryFastTime","时间速度：快速","时间与天气",KeyCode.Alpha3,KeyCode.None),
        new Binding("HalfDay","前进半天","时间与天气",KeyCode.T,KeyCode.None),
        new Binding("HourBack","后退一小时","时间与天气",KeyCode.LeftBracket,KeyCode.None),
        new Binding("HourNext","前进一小时","时间与天气",KeyCode.RightBracket,KeyCode.None),
        new Binding("Weather","天气设置 / 雨雪大小","时间与天气",KeyCode.Y,KeyCode.None),
        new Binding("Character1","查看立绘 1","人物图鉴",KeyCode.Alpha1,KeyCode.Keypad1),
        new Binding("Character2","查看立绘 2","人物图鉴",KeyCode.Alpha2,KeyCode.Keypad2),
        new Binding("Character3","查看立绘 3","人物图鉴",KeyCode.Alpha3,KeyCode.Keypad3),
        new Binding("Character4","查看立绘 4","人物图鉴",KeyCode.Alpha4,KeyCode.Keypad4),
        new Binding("Character5","查看立绘 5","人物图鉴",KeyCode.Alpha5,KeyCode.Keypad5),
        new Binding("Character6","查看立绘 6","人物图鉴",KeyCode.Alpha6,KeyCode.Keypad6),
    };
    static int cancelFrame=-1;
    public static bool CancelAvailable=>cancelFrame!=Time.frameCount;
    public static void ConsumeCancel(){cancelFrame=Time.frameCount;}
    public static bool Cancel(){if(!CancelAvailable||!Input.GetKeyDown(KeyCode.Escape))return false;ConsumeCancel();return true;}
    public static Binding Find(string id)=>Array.Find(Bindings,b=>b.id==id);
    public static bool Down(string id){if(StartMenu.IsOpen&&id!="Settings")return false;if(HelpSidebar.IsOpen&&id!="Settings"&&!(id=="Weather"&&HelpSidebar.WeatherOpen))return false;var b=Find(id);return b!=null&&(Input.GetKeyDown(b.Key)||(b.alternate!=KeyCode.None&&Input.GetKeyDown(b.alternate)));}
    public static bool Held(string id){if(HelpSidebar.IsOpen||StartMenu.IsOpen)return false;var b=Find(id);return b!=null&&(Input.GetKey(b.Key)||(b.alternate!=KeyCode.None&&Input.GetKey(b.alternate)));}
    public static bool Up(string id){var b=Find(id);return b!=null&&(Input.GetKeyUp(b.Key)||(b.alternate!=KeyCode.None&&Input.GetKeyUp(b.alternate)));}
    public static bool Bind(string id,KeyCode key,out string reason)
    {
        var target=Find(id);reason="";if(target==null||key==KeyCode.None||key==KeyCode.Escape||(int)key>=(int)KeyCode.Mouse0){reason="请选择键盘按键；返回键保留。";return false;}
        foreach(var b in Bindings)
        {if(b.id==id)continue;
         bool same=b.group==target.group||b.group=="全局"||target.group=="全局";
         bool intentional=(id=="StoryInteract"&&b.id=="CameraRight")||(id=="CameraRight"&&b.id=="StoryInteract");
         if(same&&!intentional&&(b.Key==key||b.alternate==key)){reason="该键已用于「"+b.label+"」。";return false;}}
        PlayerPrefs.SetInt(Prefix+id,(int)key);PlayerPrefs.Save();return true;
    }
    public static void Reset(){foreach(var b in Bindings)PlayerPrefs.DeleteKey(Prefix+b.id);PlayerPrefs.Save();}
    public static string Name(KeyCode key)
    {string s=key.ToString();if(s.StartsWith("Alpha"))return s.Substring(5);if(s.StartsWith("Keypad"))return "数字键盘 "+s.Substring(6);switch(key){case KeyCode.Space:return "空格";case KeyCode.Return:return "回车";case KeyCode.LeftShift:return "左 Shift";case KeyCode.RightShift:return "右 Shift";case KeyCode.UpArrow:return "↑";case KeyCode.DownArrow:return "↓";case KeyCode.LeftArrow:return "←";case KeyCode.RightArrow:return "→";case KeyCode.LeftBracket:return "[";case KeyCode.RightBracket:return "]";default:return s;}}
    public static float TextScale{get=>PlayerPrefs.GetFloat(Prefix+"TextScale",1);set{PlayerPrefs.SetFloat(Prefix+"TextScale",value);PlayerPrefs.Save();}}
    public static float TextSpeed{get=>PlayerPrefs.GetFloat(Prefix+"TextSpeed",48);set{PlayerPrefs.SetFloat(Prefix+"TextSpeed",value);PlayerPrefs.Save();}}
    public static void Check()
    {
        Reset();string reason;if(!Bind("Forward",KeyCode.R,out reason)||Find("Forward").Key!=KeyCode.R)throw new Exception("按键重映射失败");
        if(Bind("Back",KeyCode.R,out reason))throw new Exception("重叠按键未拦截");
        if(Bind("Map",KeyCode.Escape,out reason))throw new Exception("保留返回键未拦截");
        TextScale=1.25f;TextSpeed=72;
        Debug.Log("[UICheck] PASS 动作重映射、冲突拦截、保留返回键；QA 重映射 R 已写盘");
    }
}
