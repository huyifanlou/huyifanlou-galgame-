using UnityEngine;
// UI compatibility: no opening logo. The interactive title menu owns startup state.
public class IntroScreen:MonoBehaviour
{
    public static bool Live=>StartMenu.IsOpen;
    public void Init(){enabled=false;var menu=GetComponent<StartMenu>()??gameObject.AddComponent<StartMenu>();menu.Init();}
}
