using System.Collections.Generic;
using UnityEngine;

// 每个室内独立的材料语言；构建期间选择，不依赖当前玩家所在房间。
public static class InteriorMood
{
    public sealed class Palette
    {
        public string Id,Floor,FloorColor,Wall,WallColor,Wood,Accent,Metal,Chair,Light;
        public Palette(string id,string floor,string fc,string wall,string wc,string wood,string accent,string metal,string chair,string light)
        {Id=id;Floor=floor;FloorColor=fc;Wall=wall;WallColor=wc;Wood=wood;Accent=accent;Metal=metal;Chair=chair;Light=light;}
    }
    static readonly Dictionary<string,Palette> palettes=new Dictionary<string,Palette>{
        {"home",new Palette("home","oakwide","#c2a482","linenwall","#eee0cb","#b59875","#b98568","#8c806a","#a7ad8a","#ffe5c2")},
        {"school",new Palette("school","terrazzo","#d4d4c8","plaster","#e8e8df","#baa080","#8ba69e","#768c89","#9cae9d","#f0f3e9")},
        {"library",new Palette("library","parquet","#a28a6c","pinstripe","#e0d5bd","#79664d","#526c60","#a58f63","#677865","#ffe6bb")},
        {"restaurant",new Palette("restaurant","octagon","#d1bba4","plaster","#d7c4ad","#8b634c","#9b5d4a","#b09562","#84534d","#ffdbad")},
        {"supermarket",new Palette("supermarket","terrazzo","#dfe5dc","tile","#e3e7de","#bbc3ad","#77966f","#8b9b91","#77946f","#f5fff2")},
        {"canteen",new Palette("canteen","smalltile","#d8ddd6","tile","#e7e9dc","#c1af8b","#b09759","#8d9b95","#bba76f","#fff2ce")},
        {"dorm",new Palette("dorm","cork","#c1ab8c","pinstripe","#e5decf","#b29978","#a18eaa","#8b8b85","#a18eaa","#ffebcd")},
        {"auditorium",new Palette("auditorium","carpet","#77524e","acoustic","#beab91","#8a6850","#9c655d","#aa946b","#85564e","#ffe1b7")},
        {"labs",new Palette("labs","epoxy","#becdcf","tile","#e0e9e6","#9fbbb6","#6b929a","#809795","#77979d","#eafaff")},
        {"milktea",new Palette("milktea","stoneLarge","#e2d4c4","linenwall","#f0e1d3","#c2a286","#b98288","#b1a078","#b48f8f","#ffe9d0")},
        {"photo",new Palette("photo","concrete","#bbbdbb","plaster","#d6d7d2","#858c89","#4f666d","#566367","#687d83","#f3f6ff")},
        {"book",new Palette("book","parquet","#b19b7c","linenwall","#e4dac5","#a48861","#9b7955","#95866c","#9c896b","#ffe6bf")},
        {"conv",new Palette("conv","vinyl","#d6e1df","plaster","#e7ede8","#a7bdb7","#648d87","#819994","#7c9b93","#f1fff9")}
    };
    public static Palette Current=palettes["home"];
    public static void Begin(string id)=>Current=palettes[id];
    public static bool ServiceDoor=>Current.Id=="labs"||Current.Id=="photo"||Current.Id=="supermarket"||Current.Id=="conv"||Current.Id=="canteen";
    public static Material DoorFrame=>DetailSurface.Mat(ServiceDoor?"metal":"timber",ServiceDoor?Current.Metal:Current.Wood);
    public static Material DoorBody=>DetailSurface.Mat(ServiceDoor?"paint":"timber",ServiceDoor?Current.FloorColor:Current.Wood);
    public static Material Surface(string name)
    {
        var p=Current;
        switch(name)
        {
            case "in_floor":return DetailSurface.Mat(p.Floor,p.FloorColor);
            case "in_tile":return p.Id=="home"||p.Id=="dorm"?DetailSurface.Mat("smalltile","#d9d9cd"):DetailSurface.Mat(p.Floor,p.FloorColor);
            case "in_wall":return DetailSurface.Mat(p.Wall,p.WallColor);
            case "in_wall2":return DetailSurface.Mat(p.Id=="home"?"botanical":p.Wall,p.Id=="home"?"#d7dfcb":p.WallColor);
            case "in_wall3":return DetailSurface.Mat(p.Id=="home"?"linenwall":p.Wall,p.Id=="home"?"#e5cfc1":p.WallColor);
            case "in_panel":return DetailSurface.Mat("timber",p.Wood);
            case "in_walltile":return DetailSurface.Mat("smalltile",p.Id=="home"?"#ded8c6":p.WallColor);
            case "in_ceil":return DetailSurface.Mat("plaster",p.Id=="photo"?"#8b9392":p.Id=="auditorium"?"#b1a38d":"#ece9df");
            default:return null;
        }
    }
    // 只替换原来反复使用的装潢色；保留书籍、商品、警示和文字的语义色。
    public static string Paint(string hex)
    {
        switch(hex.ToLowerInvariant())
        {
            case "#c98a4e":case "#b8743c":case "#a06a3c":return Current.Wood;
            case "#6fc0a8":case "#8fd0c0":case "#7fd0a8":case "#5fb0a0":return Current.Accent;
            case "#7a8ae0":case "#f2a0c0":case "#c07ad8":return Current.Chair;
            default:return hex;
        }
    }
    public static void LightRoom(Interior room)
    {
        var p=Current;foreach(var li in room.Root.GetComponentsInChildren<Light>(true))
        {li.color=MatLib.Hex(p.Light);if(p.Id=="restaurant"||p.Id=="library"||p.Id=="book")li.intensity*=.86f;}
        Debug.Log($"[InteriorMood] {p.Id} 地面={p.Floor} 墙面={p.Wall} 木作={p.Wood} 软装={p.Accent}");
        room.Root.AddComponent<RoomLightRig>().Configure(room,MatLib.Hex(p.Light));
    }
}
