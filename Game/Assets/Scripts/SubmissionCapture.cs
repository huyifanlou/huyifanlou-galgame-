using System.Collections;
using System.IO;
using UnityEngine;

// Competition capture only. Reads the game's own rendered frames; never records other windows.
public class SubmissionCapture:MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null;yield return null;
        Screen.SetResolution(1920,1080,FullScreenMode.Windowed);Application.runInBackground=true;
        yield return new WaitForSeconds(1);
        string dir=Path.GetFullPath(Args.Str("-capture-dir"));Directory.CreateDirectory(dir);
        string mode=Args.Str("-capture-mode")??"world";float seconds=Args.F("-capture-seconds")??20;
        int fps=mode.StartsWith("tour-")?20:10,index=0;float start=Time.realtimeSinceStartup,next=start;bool opened=false,closed=false;
        if(mode.StartsWith("tour-"))gameObject.AddComponent<SubmissionTour>().Begin(mode);
        while(Time.realtimeSinceStartup-start<seconds)
        {
            if(mode=="tour-walk"&&GetComponent<SubmissionTour>().WalkFinished)break;
            float t=Time.realtimeSinceStartup-start;
            if(mode=="title"&&t>8&&!opened){StartMenu.Live.QueueEntry(StartMenu.Entry.Settings);opened=true;}
            if(mode=="title"&&t>16&&!closed){HelpSidebar.Live.Close();closed=true;}
            yield return new WaitForEndOfFrame();
            if(Time.realtimeSinceStartup<next)continue;
            var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(dir,index.ToString("D05")+".jpg"),frame.EncodeToJPG(90));Destroy(frame);index++;next=start+index/(float)fps;
        }
        File.WriteAllText(Path.Combine(dir,"capture.json"),JsonUtility.ToJson(new Info{mode=mode,frames=index,seconds=Time.realtimeSinceStartup-start,fps=fps,width=Screen.width,height=Screen.height},true));
        Debug.Log("[SubmissionCapture] "+mode+" "+index+" real game frames");Application.Quit();
    }
    [System.Serializable]class Info{public string mode;public int frames,fps,width,height;public float seconds;}
}
