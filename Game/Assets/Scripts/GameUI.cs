using UnityEngine;

// 统一 1600×900 设计坐标，等比缩放并留安全边；所有交互跟随 GUI.matrix。
public static class GameUI
{
    public static readonly Color Ink=new Color(.075f,.105f,.15f),Paper=new Color(.95f,.93f,.87f),Muted=new Color(.57f,.61f,.64f),Gold=new Color(.77f,.66f,.43f),Mint=new Color(.48f,.73f,.67f);
    static Font body,heading;static Matrix4x4 previous;static int previousDepth;
    static readonly System.Collections.Generic.Dictionary<string,GUIStyle> styles=new System.Collections.Generic.Dictionary<string,GUIStyle>();
    public static bool LightSheet;
    public static Font Font=>GameFonts.Map;
    public static GUIStyle Text(int size,Color color,bool wrap=false,bool title=false,TextAnchor align=TextAnchor.UpperLeft,bool story=false)
    {if(LightSheet){if(color==Paper)color=Ink;else if(color==Muted)color=new Color(.43f,.45f,.45f);else if(color==Gold)color=new Color(.49f,.38f,.22f);else if(color==Mint)color=new Color(.31f,.43f,.47f);}string key=story+"|"+(story?GameFonts.StoryChoice:0)+"|"+size+"|"+color+"|"+wrap+"|"+title+"|"+align;if(styles.TryGetValue(key,out var cached))return cached;var style=new GUIStyle(GUI.skin.label){font=story?GameFonts.Story:title?GameFonts.Heading:GameFonts.Map,fontSize=size,fontStyle=FontStyle.Normal,wordWrap=wrap,alignment=align};style.normal.textColor=color;styles[key]=style;return style;}
    public static void Begin(int depth=0)
    {previous=GUI.matrix;previousDepth=GUI.depth;float s=Mathf.Min(Screen.width/1600f,Screen.height/900f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*s)/2,(Screen.height-900*s)/2),Quaternion.identity,Vector3.one*s);GUI.depth=depth;GUI.color=Color.white;}
    public static void End(){LightSheet=false;GUI.color=Color.white;GUI.matrix=previous;GUI.depth=previousDepth;}
    public static Vector2 Pointer{get{float s=Mathf.Min(Screen.width/1600f,Screen.height/900f);return new Vector2((Input.mousePosition.x-(Screen.width-1600*s)/2)/s,(Screen.height-Input.mousePosition.y-(Screen.height-900*s)/2)/s);}}
    public static bool OverHud=>new Rect(0,0,1600,80).Contains(Pointer)||new Rect(24,100,330,218).Contains(Pointer)||new Rect(0,838,1600,62).Contains(Pointer);
    public static void Fill(Rect rect,Color color){GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
    public static void FloatingText(Rect rect,string text,int size,Color color,bool title=false,bool wrap=false)
    {var shadow=new Rect(rect.x+1,rect.y+1,rect.width,rect.height);GUI.Label(shadow,text,Text(size,new Color(.015f,.02f,.03f,.85f),wrap,title));GUI.Label(rect,text,Text(size,color,wrap,title));}
    public static bool FlatAction(Rect rect,string label)
    {bool hover=rect.Contains(Event.current.mousePosition);bool clicked=GUI.Button(rect,GUIContent.none,GUIStyle.none);FloatingText(new Rect(rect.x,rect.y+8,rect.width,rect.height-8),label,17,hover?new Color(.94f,.82f,.62f):Paper);if(hover)Rule(rect.x,rect.yMax-2,rect.width-14,new Color(.85f,.73f,.52f,.65f));return clicked;}
    public static Texture2D Fade(float top,float bottom)
    {var t=new Texture2D(1,64,TextureFormat.RGBA32,false,true){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};for(int i=0;i<64;i++)t.SetPixel(0,i,new Color(.035f,.05f,.08f,Mathf.Lerp(bottom,top,i/63f)));t.Apply(false,true);return t;}
    public static void Rule(float x,float y,float width,Color color){if(LightSheet&&color.r<.4f&&color.g<.4f)color=new Color(.73f,.73f,.69f,color.a);Fill(new Rect(x,y,width,1),color);}
    public static bool Button(Rect rect,string label,bool selected=false,bool primary=false)
    {
        bool hover=rect.Contains(Event.current.mousePosition);bool hit=GUI.Button(rect,GUIContent.none,GUIStyle.none);
        Fill(rect,LightSheet?(primary?new Color(.79f,.69f,.49f):selected?new Color(.77f,.81f,.82f):hover?new Color(.94f,.93f,.89f):new Color(.86f,.85f,.81f)):primary?Gold:selected?new Color(.18f,.25f,.29f):hover?new Color(.22f,.26f,.29f):new Color(.13f,.17f,.21f));
        GUI.Label(rect,label,Text(18,primary?Ink:Paper,false,false,TextAnchor.MiddleCenter));return hit;
    }
    public static void Panel(Rect rect)
    {Fill(new Rect(0,0,1600,900),new Color(.015f,.025f,.045f,.72f));Fill(rect,Ink);Rule(rect.x+30,rect.y+116,rect.width-60,new Color(.3f,.34f,.35f));}
    public static readonly Color Rose=new Color(.78f,.52f,.57f),Sky=new Color(.55f,.69f,.79f);
    public static void Folio(Rect rect,Color accent)
    {
        Fill(new Rect(0,0,1600,900),new Color(.025f,.035f,.045f,.60f));
        Fill(new Rect(rect.x+8,rect.y+10,rect.width,rect.height),new Color(0,0,0,.24f));
        Fill(rect,new Color(.89f,.88f,.84f));Fill(new Rect(rect.x,rect.y,rect.width,122),Paper);
        Fill(new Rect(rect.x,rect.y,6,122),accent);Fill(new Rect(rect.x,rect.y+120,rect.width,2),accent);
        Rule(rect.x+32,rect.y+rect.height-49,rect.width-64,new Color(.28f,.30f,.29f));
    }
    public static bool Tab(Rect r,string label,Color accent,bool selected)
    {
        bool hit=GUI.Button(r,GUIContent.none,GUIStyle.none);
        Fill(r,selected?new Color(.30f,.35f,.39f):new Color(.88f,.87f,.83f));Fill(new Rect(r.x,r.y,4,r.height),Gold);
        GUI.Label(new Rect(r.x+14,r.y,r.width-20,r.height),label,Text(18,selected?new Color(.97f,.96f,.92f):Ink,false,false,TextAnchor.MiddleLeft));return hit;
    }
}
