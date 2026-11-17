using UnityEngine;
// Narrative identities only. Character meshes and their generators have been retired.
public class CharacterDef
{
 public string id,label;
 CharacterDef(string id,string label){this.id=id;this.label=label;}
 public static CharacterDef FanLou=>new CharacterDef("fanlou","樊楼");
 public static CharacterDef FanLouFemale=>new CharacterDef("fanlou-female","樊楼（女体）");
 public static CharacterDef JingYing=>new CharacterDef("jingying","镜荧");
 public static CharacterDef JingNan=>new CharacterDef("jingnan","景楠");
 public static CharacterDef LiLiYa=>new CharacterDef("liliya","莉莉雅");
 public static CharacterDef YaoQingHan=>new CharacterDef("yaoqinghan","瑶清寒");
 public static CharacterDef ZiWei=>new CharacterDef("ziwei","紫薇");
 public static CharacterDef FromId(string id){foreach(var d in PortraitForms)if(d.id==id)return d;return FanLou;}
 public static readonly CharacterDef[] PortraitForms={FanLou,JingYing,JingNan,LiLiYa,YaoQingHan,ZiWei,FanLouFemale};
}
// Retained as save-data compatibility: the player has an identity but no visual body.
public class PlayerAvatar : MonoBehaviour
{
 CharacterDef definition;
 public static readonly CharacterDef[] Roster={CharacterDef.FanLou,CharacterDef.JingYing,CharacterDef.JingNan,CharacterDef.LiLiYa,CharacterDef.YaoQingHan,CharacterDef.ZiWei};
 public static PlayerAvatar Create(Transform parent,CharacterDef def=null){var go=new GameObject("PlayerIdentity");go.transform.SetParent(parent,false);var identity=go.AddComponent<PlayerAvatar>();identity.definition=def??CharacterDef.FanLou;return identity;}
 public string Id=>definition?.id??"fanlou";public string Label=>definition?.label??"樊楼";
 public void SetCharacter(CharacterDef def){definition=def??CharacterDef.FanLou;}
 public void SetVisible(bool visible){} // No renderer exists, including when restoring an old save.
 public void Tick(bool moving,bool running,float facing,float dt){}
 public void Cycle(){} // Model selection no longer exists.
}
