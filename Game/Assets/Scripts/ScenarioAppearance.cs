using UnityEngine;

// Only the protagonist's story appearance; portrait count is the restored two-slot dialogue UI.
public static class ScenarioAppearance
{
    public static void Apply(ScenarioChapter chapter)
    {
        bool female=chapter.file.StartsWith("03");
        foreach(var line in chapter.lines)
        {
            if(chapter.file.StartsWith("01")&&line.text.Contains("画面从模糊到清楚"))female=true;
            if(chapter.file.StartsWith("03")&&line.text.Contains("慢慢地，他的身体又变回了原样"))female=false;
            line.protagonistPortraitId=female?"fanlou-female":"fanlou";
        }
    }
    public static string Resource(string speaker,ScenarioLine line)
    {
        if(speaker=="樊楼")return "Portraits/"+(line?.protagonistPortraitId??"fanlou")+"-cut";
        return ScenarioData.PortraitResource(speaker,true);
    }
}
