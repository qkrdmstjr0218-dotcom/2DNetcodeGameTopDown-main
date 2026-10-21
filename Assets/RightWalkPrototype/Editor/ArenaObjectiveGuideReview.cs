using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;

[InitializeOnLoad]
public static class ArenaObjectiveGuideReview
{
    const string Request="LocalTools/Analysis/ObjectiveGuideReview.txt";
    static bool started;
    static ArenaObjectiveGuideReview(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/Right Walk Prototype/Verify Objective Guidance")]
    public static void Begin()
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Requires saved edit mode");
        string folder=Path.GetFullPath("LocalTools/Analysis/NetworkCombat/guide-"+DateTime.UtcNow.ToString("HHmmss"));
        Directory.CreateDirectory(folder);File.WriteAllText(Request,folder);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying){started=false;return;}
        if(started||Time.time<.4f||!File.Exists(Request)||!Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor)return;
        var session=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();if(session==null)return;
        started=true;session.useReadyRoom=false;session.StartLocal(true);session.StartCoroutine(Run(session,File.ReadAllText(Request)));
    }
    static IEnumerator Run(SideScrollNetworkSession session,string folder)
    {
        float until=Time.realtimeSinceStartup+15;SideScrollNetworkPlayer owner=null;
        while(Time.realtimeSinceStartup<until)
        {
            foreach(var p in UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsOwner)owner=p;
            if(owner!=null&&ArenaObjectiveMatch.Instance!=null&&ArenaObjectiveMatch.Instance.IsSpawned)break;
            yield return null;
        }
        if(owner==null){File.WriteAllText(folder+"/result.txt","FAIL: no owner");File.Delete(Request);EditorApplication.isPlaying=false;yield break;}
        var move=owner.GetComponent<SideScrollMapPreview>();move.SetInput(0);
        owner.GetComponentInChildren<RightFacingShotPreview>().readMouse=false;
        var match=ArenaObjectiveMatch.Instance;var arena=RelayArenaPrototype.Instance;
        match.roundSeconds=300;match.pointSeconds=100;match.StartRound();
        for(int i=0;i<6;i++)
        {
            var s=match.State.Value;s.Point=i==0?3:i==1?1:2;s.End=match.Now+200;s.Switch=match.Now+90;s.Warning=match.Now+80;
            if(i==3||i==5){s.Point=1;s.Switch=match.Now+5;s.Warning=match.Now-1;}
            match.State.Value=s;
            move.SetSpawnPosition(i==0?new Vector2(-20,.03f):i==1?new Vector2(20,.03f):i==2||i==5?new Vector2(0,6.43f):new Vector2(20,.03f));
            arena.SetMenuOpen(i==4);
            yield return new WaitForSecondsRealtime(1.3f);
            ScreenCapture.CaptureScreenshot(folder+"/guide-"+i+".png");yield return new WaitForSecondsRealtime(.3f);
        }
        arena.SetMenuOpen(false);arena.overview=true;yield return new WaitForSecondsRealtime(.4f);
        ScreenCapture.CaptureScreenshot(folder+"/overview.png");yield return new WaitForSecondsRealtime(.3f);
        var state=match.State.Value;state.Phase=ArenaObjectiveMatch.Stage.Finished;match.State.Value=state;arena.overview=false;
        yield return new WaitForSecondsRealtime(.4f);ScreenCapture.CaptureScreenshot(folder+"/finished.png");yield return new WaitForSecondsRealtime(.3f);
        File.WriteAllText(folder+"/result.txt","CAPTURED: current right/left/on-screen, next warning, menu, overview, finished. Visual review required.");
        File.Delete(Request);session.DisconnectLocal();yield return new WaitForSecondsRealtime(.4f);EditorApplication.isPlaying=false;
    }
}
