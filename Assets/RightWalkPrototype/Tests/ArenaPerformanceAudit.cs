#if ARENA_PERFORMANCE_AUDIT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace RightWalkPrototype
{
    // Explicit measurement builds only; normal players contain neither counters nor runner.
    internal static class ArenaPerformanceCounters
    {
        [Serializable] internal sealed class Row {public string name;public long calls;public double totalMs,maxMs;}
        static readonly Dictionary<string,Row> rows=new();
        public static bool Enabled;
        public readonly struct Scope:IDisposable
        {
            readonly string name;readonly long started;
            public Scope(string name){this.name=name;started=Enabled?Stopwatch.GetTimestamp():0;}
            public void Dispose(){if(started==0)return;double ms=(Stopwatch.GetTimestamp()-started)*1000.0/Stopwatch.Frequency;if(!rows.TryGetValue(name,out var row)){row=new Row{name=name};rows.Add(name,row);}row.calls++;row.totalMs+=ms;row.maxMs=Math.Max(row.maxMs,ms);}
        }
        public static Scope Measure(string name)=>new(name);
        public static void Reset(){rows.Clear();}
        public static Row[] Snapshot(){var result=new Row[rows.Count];int i=0;foreach(var row in rows.Values)result[i++]=new Row{name=row.name,calls=row.calls,totalMs=row.totalMs,maxMs=row.maxMs};return result;}
    }
    public sealed class ArenaPerformanceAudit:MonoBehaviour
    {
        [Serializable] sealed class Result {public string status,graphics,version;public int width,height,targetFps;public Case[] cases;public string[] errors;}
        [Serializable] sealed class Case {public int bots,frames,decisions,jumps;public double seconds,fps,medianMs,p95Ms,p99Ms,maxMs,cpuMsPerFrame;public long workingSetBytes,unityAllocatedBytes,monoUsedBytes;public ArenaPerformanceCounters.Row[] warmup,counters;}
        readonly List<Case> cases=new();readonly List<string> errors=new();string folder;bool soak;SideScrollNetworkSession session;
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot()
        {var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--arena-performance-audit");if(at<0||at+1>=args.Length)return;var runner=new GameObject("Performance audit fixture").AddComponent<ArenaPerformanceAudit>();runner.folder=args[at+1];runner.soak=Array.IndexOf(args,"--arena-soak-audit")>=0;Directory.CreateDirectory(runner.folder);runner.StartCoroutine(runner.Guard(runner.Run()));}
        void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(type+": "+text+"\n"+stack);}
        IEnumerator Wait(Func<bool> test,string message,float seconds=25){double end=Time.realtimeSinceStartupAsDouble+seconds;while(!test()&&Time.realtimeSinceStartupAsDouble<end)yield return null;if(!test())throw new Exception("Timeout: "+message);}
        void Progress(string text)=>File.AppendAllText(Path.Combine(folder,"progress.txt"),text+"\n");
        IEnumerator Run()
        {
            yield return null;yield return null;yield return null;AudioListener.volume=0;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Application.runInBackground=true;
            session=SideScrollNetworkSession.Instance;Application.logMessageReceived+=Log;
            foreach(int count in soak?new[]{3,3,3,3,3}:new[]{0,1,3})
            {
                Progress("START bots="+count);UnityEngine.Random.InitState(20261030);session.StartSoloPractice();yield return Wait(()=>session.CanEditBots,"solo waiting");typeof(SideScrollNetworkSession).GetField("botDifficulty",Flags).SetValue(session,1);
                var own=Array.Find(FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None),p=>p.IsLocalHuman);own.GetComponent<SideScrollMapPreview>().readKeyboard=false;own.GetComponentInChildren<RightFacingShotPreview>().readMouse=false;own.GetComponentInChildren<RightFacingAimRig>().mouseAim=false;
                for(int i=0;i<count;i++)if(!session.AddPracticeBot())throw new Exception("Cannot add bot");
                for(int i=0;i<10;i++)yield return null;
                var match=ArenaObjectiveMatch.Instance;match.roundSeconds=soak?180:45;match.pointSeconds=soak?30:10;match.warningSeconds=3;match.SetLocalReady(true);yield return null;
                ArenaPerformanceCounters.Reset();ArenaPerformanceCounters.Enabled=true;if(!match.StartRound())throw new Exception("Cannot start match");yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing,"playing");yield return new WaitForSecondsRealtime(8);
                var row=new Case{bots=count,warmup=ArenaPerformanceCounters.Snapshot()};ArenaPerformanceCounters.Reset();var frames=new List<double>(2400);var process=Process.GetCurrentProcess();double beforeCpu=process.TotalProcessorTime.TotalMilliseconds;double began=Time.realtimeSinceStartupAsDouble;
                while(Time.realtimeSinceStartupAsDouble-began<(soak?160:30)&&match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing){yield return null;frames.Add(Time.unscaledDeltaTime*1000.0);}
                row.seconds=Time.realtimeSinceStartupAsDouble-began;row.frames=frames.Count;row.fps=frames.Count/row.seconds;row.cpuMsPerFrame=(process.TotalProcessorTime.TotalMilliseconds-beforeCpu)/Math.Max(1,frames.Count);ArenaPerformanceCounters.Enabled=false;row.counters=ArenaPerformanceCounters.Snapshot();frames.Sort();row.medianMs=frames[frames.Count/2];row.p95Ms=frames[(int)((frames.Count-1)*.95)];row.p99Ms=frames[(int)((frames.Count-1)*.99)];row.maxMs=frames[frames.Count-1];process.Refresh();row.workingSetBytes=process.WorkingSet64;row.unityAllocatedBytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();row.monoUsedBytes=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
                foreach(var bot in FindObjectsByType<ArenaPlayerBot>(FindObjectsSortMode.None)){row.decisions+=bot.Decisions;row.jumps+=bot.JumpRequests;}
                cases.Add(row);Progress("MEASURED bots="+count+" fps="+row.fps.ToString("F2")+" cpuMs="+row.cpuMsPerFrame.ToString("F2"));Write("RUNNING");yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Finished,"finished",20);if(!match.ReturnToRoom())throw new Exception("Cannot return");yield return Wait(()=>session.CanEditBots,"returned");session.DisconnectLocal();yield return Wait(()=>Unity.Netcode.NetworkManager.Singleton!=null&&!Unity.Netcode.NetworkManager.Singleton.IsListening&&!Unity.Netcode.NetworkManager.Singleton.ShutdownInProgress,"disconnected");yield return null;
            }
        }
        void Write(string status)=>File.WriteAllText(Path.Combine(folder,"result.json"),JsonUtility.ToJson(new Result{status=status,graphics=SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName,version=Application.unityVersion,width=Screen.width,height=Screen.height,targetFps=Application.targetFrameRate,cases=cases.ToArray(),errors=errors.ToArray()},true));
        IEnumerator Guard(IEnumerator run)
        {
            var stack=new Stack<IEnumerator>();stack.Push(run);Exception failure=null;while(stack.Count>0){bool next=false;object value=null;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){failure=e;}if(failure!=null)break;if(!next){stack.Pop();continue;}if(value is IEnumerator child)stack.Push(child);else yield return value;}
            ArenaPerformanceCounters.Enabled=false;Application.logMessageReceived-=Log;if(failure!=null)errors.Add(failure.ToString());Write(failure==null&&errors.Count==0?"PASS":"FAIL");Progress("DONE");session?.DisconnectLocal();Application.Quit(failure==null&&errors.Count==0?0:1);
        }
    }
}
#endif
