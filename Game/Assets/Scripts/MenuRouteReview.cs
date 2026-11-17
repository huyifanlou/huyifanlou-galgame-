using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Exercise the same action dispatcher used by all visible menu buttons, in isolated saves.
public class MenuRouteReview:MonoBehaviour
{
    IEnumerator Settle()
    {yield return null;float end=Time.realtimeSinceStartup+8;while((ProgressManager.Loading||GetComponent<InteriorManager>().IsTransitioning)&&Time.realtimeSinceStartup<end)yield return null;if(ProgressManager.Loading)throw new Exception("Menu route load timed out");}
    IEnumerator Start()
    {
        yield return null;yield return null;
        var menu=StartMenu.Live;var p=GetComponent<ProgressManager>();var d=GetComponent<DialogueManager>();var q=GetComponent<QuestJournal>();
        if(p.LatestSaveSlot>=0)throw new Exception("Expected fresh isolated menu test directory");
        menu.Activate(StartMenu.Entry.Resume);yield return Settle();
        if(StartMenu.IsOpen||GetComponent<InteriorManager>().Current?.Id!="home"||d.Chapter!=0)throw new Exception("Start with no save failed");
        d.RestoreProgress(1,5,false,true);q.Completed.Add("clock");p.Save(1);
        string slot=Path.Combine(Application.persistentDataPath,"Saves-QA-Menu147-"+Args.Str("-qaid"),"slot-1.json");string original=File.ReadAllText(slot);
        menu.Open();menu.Activate(StartMenu.Entry.Resume);yield return Settle();
        if(d.Chapter!=1||d.Line!=5||!q.Completed.Contains("clock"))throw new Exception("Continue route failed");
        menu.Open();menu.Activate(StartMenu.Entry.NewGame);
        if(!StartMenu.IsOpen||!menu.PendingNewGame||d.Chapter!=1)throw new Exception("New game confirmation failed");
        menu.Activate(StartMenu.Entry.NewGame);yield return Settle();
        if(StartMenu.IsOpen||d.Chapter!=0||q.Completed.Count!=0||File.ReadAllText(slot)!=original)throw new Exception("New game route damaged state/manual save");
        menu.Open();menu.QueueEntry(StartMenu.Entry.Settings);yield return null;yield return null;
        if(!HelpSidebar.IsOpen||!StartMenu.IsOpen)throw new Exception("Settings route failed");
        yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("shots/148-title-settings-fix/settings-visible.png"));
        for(int i=0;i<6;i++)yield return new WaitForEndOfFrame();
        HelpSidebar.Live.Close();if(!StartMenu.IsOpen)throw new Exception("Settings return lost title screen");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("shots/147-ghost-hud-title/menu-route.png"));yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
        Debug.Log("[MenuRouteCheck] PASS start/no-save, continue, confirmed new game, manual preservation, settings/return; invoking genuine quit action");
        menu.Activate(StartMenu.Entry.Quit);
    }
}
