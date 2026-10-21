using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Profiling;
using RightWalkPrototype;

public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Sample Foundry Performance")]
    public static void BeginPerformance()
    {
        BeginFoundry();File.WriteAllText(folder+"/performance-only.txt","performance");
    }
    [MenuItem("Tools/Right Walk Prototype/Sample Foundry Solo Performance")]
    public static void BeginSoloPerformance()
    {
        BeginFoundry();File.WriteAllText(folder+"/solo-performance.txt","solo");
    }
    static IEnumerator CheckSoloPerformance()
    {
        yield return Wait(()=>Player(1)!=null&&ArenaObjectiveMatch.Instance!=null&&ArenaObjectiveMatch.Instance.IsSpawned,"solo host spawned");
        Require(ArenaObjectiveMatch.Instance.StartRound(),"solo diagnostic starts without room flow");
        yield return Wait(()=>ArenaObjectiveMatch.Instance.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing,"solo diagnostic playing");
        yield return FoundryPerformanceSample(Player(1));
    }
    static IEnumerator FoundryPerformanceSample(SideScrollNetworkPlayer own)
    {
        foundryDriving=true;var move=own.GetComponent<SideScrollMapPreview>();var crouch=own.GetComponent<CrouchTrial>();
        var aim=own.GetComponentInChildren<RightFacingAimRig>();aim.SetAimDirection(Vector2.up);
        move.SetInput(0);move.SetSpawnPosition(new Vector2(-20,.04f));
        yield return new WaitForSecondsRealtime(2);
        var rows=new List<string>{"stage,frameMs,mainThreadMs,allocBytes,gc0,gc1,gc2,focused"};
        File.WriteAllText(folder+"/performance-environment.txt",
            "Unity "+Application.unityVersion+"\nScreen "+Screen.width+"x"+Screen.height+
            "\nRAM MB "+SystemInfo.systemMemorySize+"\nGPU "+SystemInfo.graphicsDeviceName+
            "\nGPU MB "+SystemInfo.graphicsMemorySize+"\nCPU "+SystemInfo.processorType+
            "\nvsync "+QualitySettings.vSyncCount+"\ntarget "+Application.targetFrameRate+
            "\nPlayers "+UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None).Length+
            "\nExisting review snapshots write every 80ms; includes editor/test overhead, not a standalone build benchmark. Solo mode bypasses ready room/countdown only in this opt-in fixture.");
        using(var main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1))
        using(var alloc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1))
        {
            foreach(string stage in new[]{"idle","run","shoot","crouch-move"})
            {
                crouch.SetRequested(stage=="crouch-move");float end=Time.realtimeSinceStartup+5;
                int direction=1;
                while(Time.realtimeSinceStartup<end)
                {
                    if(own.transform.position.x>-17)direction=-1;
                    if(own.transform.position.x<-22)direction=1;
                    move.SetInput(stage=="run"||stage=="crouch-move"?direction:0,false,stage=="run");
                    if(stage=="shoot")SampleFireButtons(own,false,true);
                    rows.Add(stage+","+(Time.unscaledDeltaTime*1000)+","+(main.Valid?main.LastValue/1000000.0:0)+","+(alloc.Valid?alloc.LastValue:0)+","+GC.CollectionCount(0)+","+GC.CollectionCount(1)+","+GC.CollectionCount(2)+","+Application.isFocused);
                    yield return null;
                }
            }
        }
        move.SetInput(0);crouch.SetRequested(false);foundryDriving=false;
        File.WriteAllLines(folder+"/performance.csv",rows);
        Require(rows.Count>20,"performance samples captured");
    }
}
