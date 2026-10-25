using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Profiling;
using RightWalkPrototype;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

public static partial class ArenaTutorialReview
{
    const string LiveKey="ArenaTutorialReview.Live",LiveFolder="LocalTools/Analysis/TutorialInput";
    public static void BeginInputReview()
    {Directory.CreateDirectory(LiveFolder);File.WriteAllText(LiveFolder+"/result.txt","RUNNING");SessionState.SetBool(LiveKey,true);EditorApplication.isPlaying=true;}
    static void LiveTick()
    {
        if(!Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor||!SessionState.GetBool(LiveKey,false)||!EditorApplication.isPlaying||Time.time<.5f)return;
        var session=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();if(session==null)return;
        SessionState.SetBool(LiveKey,false);session.StartCoroutine(LiveRun(session));
    }
    static IEnumerator LiveRun(SideScrollNetworkSession session)
    {
        session.StartTutorial();yield return new WaitForSeconds(.5f);
        var own=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>();var t=ArenaTutorial.Instance;
        var a=own.GetComponentInChildren<RightFacingAimRig>();
        var lines=new List<string>{"fresh mouseAim="+a.mouseAim};
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
        yield return new WaitForSeconds(.3f);
        lines.Add("gameplayFocus="+RelayArenaPrototype.HasGameplayFocus);
#if ENABLE_INPUT_SYSTEM
        if(RelayArenaPrototype.HasGameplayFocus&&Mouse.current!=null)
        {
            t.EnterLesson(3);yield return new WaitForSeconds(.2f);
            int shotsBefore=own.ReceivedShots;
            int held=0,blocked=0,focus=0,ui=0;
            // Queue mouse device states; normal LateUpdate reads them. No Fire(),
            // SetAimTarget(), or private ProcessFireInput invocation in this check.
            float until=Time.time+1;
            while(Time.time<until)
            {
                Vector2 at=Camera.main.WorldToScreenPoint(t.Target.transform.position);
                InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=at}.WithButton(MouseButton.Left));yield return null;
                if(Mouse.current.leftButton.isPressed)held++;if(RelayArenaPrototype.FireInputBlocked)blocked++;if(RelayArenaPrototype.HasGameplayFocus)focus++;if(ArenaObjectiveMatch.RoomPointerOverUI)ui++;
            }
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=Camera.main.WorldToScreenPoint(t.Target.transform.position)});
            yield return new WaitForSeconds(.5f);
            lines.Add("live mouse normal shots="+(own.ReceivedShots-shotsBefore)+", target HP="+t.Target.Health.Value+", mouseAim="+a.mouseAim);
            lines.Add("mouse held frames="+held+", blocked="+blocked+", focus="+focus+", UI="+ui+", readMouse="+own.GetComponentInChildren<RightFacingShotPreview>().readMouse);
            lines.Add(own.ReceivedShots>shotsBefore&&t.Target.Health.Value==0&&a.mouseAim?"LIVE INPUT PASS":"LIVE INPUT FAIL");
            t.EnterLesson(4);yield return new WaitForSeconds(.4f);
            lines.Add("shotgun target art minimum Y="+t.Target.GetComponent<SpriteRenderer>().bounds.min.y);
            ScreenCapture.CaptureScreenshot(LiveFolder+"/shotgun-ready.png");
            own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.InstantShotgun);int blasts=own.ReceivedInstantBlasts;
            until=Time.time+.65f;
            while(Time.time<until)
            {
                Vector2 at=Camera.main.WorldToScreenPoint(t.Target.transform.position);
                InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=at}.WithButton(MouseButton.Left).WithButton(MouseButton.Right));yield return null;
            }
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=Camera.main.WorldToScreenPoint(own.transform.position+Vector3.right*4)});
            yield return new WaitForSeconds(2);
            lines.Add("live RMB+LMB shotgun blasts="+(own.ReceivedInstantBlasts-blasts)+", lessonComplete="+t.LessonComplete);
            lines.Add(own.ReceivedInstantBlasts>blasts&&t.LessonComplete?"LIVE SPECIAL INPUT PASS":"LIVE SPECIAL INPUT FAIL");
        }
        else lines.Add("LIVE INPUT NOT RUN: GameView did not retain application focus");
#endif
        foreach(int lesson in new[]{0,1,2,3,4,5,6}){t.EnterLesson(lesson);yield return null;lines.Add("lesson "+lesson+" mouseAim="+a.mouseAim);}
        var processes=Process.GetProcessesByName("Unity");var cpu=new Dictionary<int,double>();
        foreach(var p in processes)try{cpu[p.Id]=p.TotalProcessorTime.TotalMilliseconds;}catch{}
        var frames=new List<float>();var mainTimes=new List<float>();long allocated=0;float began=Time.realtimeSinceStartup;
        using(var main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1))
        using(var alloc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1))
        {
            while(Time.realtimeSinceStartup-began<6)
            {frames.Add(Time.unscaledDeltaTime*1000);if(main.Valid)mainTimes.Add(main.LastValue/1000000f);if(alloc.Valid)allocated+=alloc.LastValue;yield return null;}
        }
        frames.Sort();mainTimes.Sort();
        lines.Add("6s tutorial (Editor, no Relay): frames="+frames.Count+", median="+frames[frames.Count/2]+"ms, p95="+frames[(int)(frames.Count*.95f)]+"ms, max="+frames[frames.Count-1]+"ms");
        if(mainTimes.Count>0)lines.Add("Main thread median="+mainTimes[mainTimes.Count/2]+"ms; allocated="+allocated);
        foreach(var p in processes)try{if(cpu.TryGetValue(p.Id,out double start))lines.Add("Unity PID "+p.Id+" CPU ms="+(p.TotalProcessorTime.TotalMilliseconds-start));p.Dispose();}catch{}
        ScreenCapture.CaptureScreenshot(LiveFolder+"/input.png");
        File.WriteAllLines(LiveFolder+"/result.txt",lines);session.DisconnectLocal();
    }
}
