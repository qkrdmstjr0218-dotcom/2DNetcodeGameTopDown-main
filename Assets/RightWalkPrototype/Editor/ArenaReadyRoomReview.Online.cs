using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using Unity.Services.Multiplayer;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    [Serializable] class OnlineResult {public bool success,listed,locked,joinable;public string error,phase;public int slots;}
    [MenuItem("Tools/Right Walk Prototype/Verify Internet Rooms")]
    public static void BeginOnlineRooms(){BeginFoundry();File.WriteAllText(folder+"/online.txt","online");}
    static IEnumerator OnlineBoot(SideScrollNetworkSession session,bool main)
    {
        Task<bool> task=null;bool success=false;
        if(main)
        {
            ScreenCapture.CaptureScreenshot(folder+"/online-lobby.png");yield return new WaitForSecondsRealtime(.2f);
            task=session.CreateOnlineRoomAsync("개발 검증 "+DateTime.UtcNow.ToString("HHmmss"));
            while(!task.IsCompleted)yield return null;
            success=task.Status==TaskStatus.RanToCompletion&&task.Result;
            if(success)File.WriteAllText(folder+"/online-room-id.txt",session.OnlineRoomId);
        }
        else
        {
            float until=Time.realtimeSinceStartup+70;
            while(!File.Exists(folder+"/online-room-id.txt")&&Time.realtimeSinceStartup<until&&!File.Exists(folder+"/stop.txt"))yield return null;
            if(File.Exists(folder+"/online-room-id.txt"))
            {
                string wanted=Read(folder+"/online-room-id.txt");
                while(Time.realtimeSinceStartup<until&&!success)
                {
                    task=session.RefreshOnlineRoomsAsync();while(!task.IsCompleted)yield return null;
                    ISessionInfo selected=null;foreach(var room in session.OnlineRooms)if(room.Id==wanted)selected=room;
                    if(selected!=null&&SideScrollNetworkSession.OnlineRoomJoinable(selected))
                    {task=session.JoinOnlineRoomAsync(selected.Id);while(!task.IsCompleted)yield return null;success=task.Status==TaskStatus.RanToCompletion&&task.Result;break;}
                    yield return new WaitForSecondsRealtime(2);
                }
            }
        }
        File.WriteAllText(folder+"/online-boot-"+System.Diagnostics.Process.GetCurrentProcess().Id+".json",JsonUtility.ToJson(new OnlineResult{success=success,error=session.ConnectionNotice}));
    }
    static void RequireNoOnlineBootError()
    {
        foreach(var file in Directory.GetFiles(folder,"online-boot-*.json"))
        {OnlineResult report=null;try{report=JsonUtility.FromJson<OnlineResult>(Read(file));}catch{}if(report!=null&&!report.success)throw new Exception("Internet boot failed: "+report.error);}
    }
    static IEnumerator OnlineQueryReview(SideScrollNetworkSession session,int id)
    {
        while(session.OnlineBusy)yield return null;
        var task=session.RefreshOnlineRoomsAsync();while(!task.IsCompleted)yield return null;
        var report=new OnlineResult{success=task.Status==TaskStatus.RanToCompletion&&task.Result,error=session.ConnectionNotice};
        var wanted=Read(folder+"/online-room-id.txt");
        foreach(var room in session.OnlineRooms)if(room.Id==wanted)
        {
            report.listed=true;report.locked=room.IsLocked;report.slots=room.AvailableSlots;
            report.phase=room.Properties.TryGetValue("phase",out var phase)?phase.Value:"";report.joinable=SideScrollNetworkSession.OnlineRoomJoinable(room);
        }
        File.WriteAllText(folder+"/online-query-"+id+".json",JsonUtility.ToJson(report));
    }
    static IEnumerator OnlineRejoin(SideScrollNetworkSession session)
    {
        while(session.OnlineBusy)yield return null;
        string wanted=Read(folder+"/online-room-id.txt");float until=Time.realtimeSinceStartup+20;
        while(Time.realtimeSinceStartup<until)
        {
            var task=session.RefreshOnlineRoomsAsync();while(!task.IsCompleted)yield return null;
            ISessionInfo selected=null;foreach(var room in session.OnlineRooms)if(room.Id==wanted)selected=room;
            if(selected!=null&&SideScrollNetworkSession.OnlineRoomJoinable(selected))
            {task=session.JoinOnlineRoomAsync(selected.Id);while(!task.IsCompleted)yield return null;yield break;}
            yield return new WaitForSecondsRealtime(2);
        }
    }
    static OnlineResult OnlineQuery(int id)=>JsonUtility.FromJson<OnlineResult>(Read(folder+"/online-query-"+id+".json"));
    static IEnumerator CheckOnlineRooms(SideScrollNetworkSession session)
    {
        float deadline=Time.realtimeSinceStartup+85;
        while(!All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.online&&p.relay&&!string.IsNullOrEmpty(p.onlineRoom))&&Time.realtimeSinceStartup<deadline)
        {RequireNoOnlineBootError();yield return null;}
        RequireNoOnlineBootError();Require(All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.online&&p.relay&&!string.IsNullOrEmpty(p.onlineRoom)),"four distinct peers connected through actual Relay");
        string roomId=session.OnlineRoomId;Require(All(4,p=>p.onlineRoom==roomId),"all peers joined same listed internet room without code");
        yield return CheckAmmoPreselection();yield return CheckAmmoHint();
        var m=ArenaObjectiveMatch.Instance;Send(4,115);int query=revision;
        yield return Wait(()=>File.Exists(folder+"/online-query-"+query+".json"),"client queries internet list",20);
        var report=OnlineQuery(query);Require(report.success&&report.listed&&report.slots==0&&!report.locked&&!report.joinable&&report.phase=="waiting","full waiting room visible with join disabled");
        Send(4,116);yield return Wait(()=>!P(4).connected&&m.ReadyPlayers.Count==3,"participant leaves internet room",20);
        yield return new WaitForSecondsRealtime(1);Send(4,115);query=revision;
        yield return Wait(()=>File.Exists(folder+"/online-query-"+query+".json"),"departed participant sees updated slots",20);
        report=OnlineQuery(query);Require(report.success&&report.listed&&report.slots==1&&report.joinable,"one freed internet slot becomes joinable");
        ScreenCapture.CaptureScreenshot(folder+"/online-ready-room.png");yield return new WaitForSecondsRealtime(.2f);
        Send(4,117);yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.relay),"client rejoins from room list",35);
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"internet peers ready");m.roundSeconds=120;Require(m.StartRound(),"host starts internet match");
        yield return Wait(()=>session.OnlinePublishedPhase=="playing","backend room locked for match",20);
        Send(4,115);query=revision;yield return Wait(()=>File.Exists(folder+"/online-query-"+query+".json"),"client reads playing-room status",20);
        report=OnlineQuery(query);Require(report.success&&report.listed&&report.locked&&!report.joinable&&report.phase=="playing","playing internet room locked and labelled");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.hp==100&&p.relay),"all Relay peers arrive in match",20);
        Send(2,5);Vector3 before=P(2).position;yield return new WaitForSecondsRealtime(.5f);Require(P(2).position.x>before.x+.1f,"actual participant movement crosses Relay");
        for(int i=0;i<m.Patrols.Count;i++){var e=m.Patrols[i];if(!e.Medical){e.Health=0;e.RespawnAt=m.Now+300;m.Patrols[i]=e;}}
        yield return Wait(()=>All(4,p=>p.medicCount==1&&p.medicHealth==8),"eight-health shield drone synchronized through Relay",22);
        yield return Wait(()=>MedicIndex(m)>=0&&Mathf.Abs(m.Patrols[MedicIndex(m)].Position.x)<16&&Mathf.Abs(m.Patrols[MedicIndex(m)].Position.x)>6,"shield drone enters reachable arena",20);
        int kits=m.HealthPickups.Count,rifle=P(2).rifleAmmo;Send(2,98);
        yield return Wait(()=>m.HealthPickups.Count==kits+1,"actual client rifle destroys eight-health shield drone",8);
        Require(m.HealthPickups[kits].Shield&&m.HealthPickups[kits].Amount==50&&P(2).rifleAmmo<=rifle-2,"two rifle hits retain the same shield reward");
        var state=m.State.Value;state.End=m.Now+.05;m.State.Value=state;yield return Wait(()=>All(4,p=>p.phase==2),"internet match result synchronized");
        Require(m.ReturnToRoom(),"return internet room to waiting");yield return Wait(()=>All(4,p=>p.phase==0)&&session.OnlinePublishedPhase=="waiting","same internet room reopened after match",20);
        Require(session.OnlineRoomId==roomId,"room identity retained over match");
        var closing=session.LeaveOnlineRoomAsync();while(!closing.IsCompleted)yield return null;
        yield return Wait(()=>All(4,p=>!p.connected&&string.IsNullOrEmpty(p.onlineRoom)),"host closes room and all clients return to lobby",25);
        ScreenCapture.CaptureScreenshot(folder+"/online-lobby-return.png");yield return new WaitForSecondsRealtime(.2f);
        Send(4,115);query=revision;yield return Wait(()=>File.Exists(folder+"/online-query-"+query+".json"),"former client refreshes lobby",20);
        report=OnlineQuery(query);Require(report.success&&!report.listed,"deleted room removed from real internet list");
        yield return Wait(()=>!session.OnlineBusy,"host lobby refresh finishes before recreation",15);
        var recreate=session.CreateOnlineRoomAsync("재입장 검증 "+DateTime.UtcNow.ToString("HHmmss"));while(!recreate.IsCompleted)yield return null;
        Require(recreate.Status==TaskStatus.RanToCompletion&&recreate.Result,"host can create another room after deletion");
        File.WriteAllText(folder+"/online-room-id.txt",session.OnlineRoomId);string next=session.OnlineRoomId;Send(0,119);
        yield return Wait(()=>All(4,p=>p.connected&&p.onlineRoom==next&&p.roster==4&&p.relay),"all clients can join a new room after host deletion",40);
        closing=session.LeaveOnlineRoomAsync();while(!closing.IsCompleted)yield return null;
        yield return Wait(()=>All(4,p=>!p.connected&&string.IsNullOrEmpty(p.onlineRoom)),"second test room cleaned",25);
    }
}
