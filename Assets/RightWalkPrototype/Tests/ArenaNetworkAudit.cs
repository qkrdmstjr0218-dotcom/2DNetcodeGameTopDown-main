#if ARENA_NETWORK_AUDIT
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
namespace RightWalkPrototype
{
    // Explicit audit builds only. No file-command endpoint exists in normal builds.
    public sealed class ArenaNetworkAudit:MonoBehaviour
    {
        [Serializable] sealed class Command{public int id,target,amount,ammo;public string action,text;public float x,y;}
        [Serializable] sealed class Player{public int number,hp,shield,normal,rifle,grenade,shots,rifleShots,grenadeShots;public uint life;public string name;public float x,y;public bool dead,protectedNow;}
        [Serializable] sealed class Snapshot{public string role,phase,result,error,notice;public int ack,count,roster,version;public float time;public bool online,onlineBusy,cleanupDone;public Player[] players;public float[] scores;}
        public static bool PrivateRoom {get;private set;}
        bool relay,cleanupDone;
        string folder,role,error="";int ack;float next;SideScrollNetworkSession session;
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"--arena-network-audit");if(i<0||i+2>=a.Length)return;var r=new GameObject("Network audit fixture").AddComponent<ArenaNetworkAudit>();r.folder=a[i+1];r.role=a[i+2];r.relay=Array.IndexOf(a,"--arena-relay-audit")>=0;PrivateRoom=r.relay&&Array.IndexOf(a,"--arena-listed-audit")<0;Directory.CreateDirectory(r.folder);r.StartCoroutine(r.Run());}
        IEnumerator Run(){yield return null;yield return null;yield return null;Application.targetFrameRate=30;session=SideScrollNetworkSession.Instance;Application.logMessageReceived+=OnLog;if(relay){var task=ConnectRelayAsync();while(!task.IsCompleted)yield return null;if(task.IsFaulted)error=task.Exception.GetBaseException().ToString();}else session.StartLocal(role=="host");while(true){Tick();yield return null;}}
        async System.Threading.Tasks.Task ConnectRelayAsync()
        {
            await Unity.Services.Core.UnityServices.InitializeAsync();var auth=Unity.Services.Authentication.AuthenticationService.Instance;
            if(auth.IsSignedIn)auth.SignOut();auth.SwitchProfile("arena-audit-"+role+"-"+Guid.NewGuid().ToString("N").Substring(0,10));
            bool ok;string share=Path.Combine(folder,"relay-session-id.txt");
            if(role=="host")ok=await session.CreateOnlineRoomAsync(PrivateRoom?"Private connection audit":"연결 점검 1030");
            else
            {
                string query=Path.Combine(folder,"query-room-name.txt");
                if(File.Exists(query))
                {
                    if(!await session.RefreshOnlineRoomsAsync())throw new Exception("Audit room query failed");
                    string name=File.ReadAllText(query).Trim();var matches=session.OnlineRooms.Where(info=>info.Name==name).ToArray();
                    if(matches.Length!=1)throw new Exception("Expected one exact audit room name");
                    ok=await session.JoinOnlineRoomAsync(matches[0].Id);
                }
                else {float until=Time.realtimeSinceStartup+30;while(!File.Exists(share)&&Time.realtimeSinceStartup<until)await System.Threading.Tasks.Task.Yield();if(!File.Exists(share))throw new Exception("Host session ID not available");ok=await session.JoinOnlineRoomAsync(File.ReadAllText(share).Trim());}
            }
            if(!ok)throw new Exception(session.ConnectionNotice);
            var opened=(Unity.Services.Multiplayer.ISession)typeof(SideScrollNetworkSession).GetField("onlineSession",Flags).GetValue(session);
            if(opened.IsPrivate!=PrivateRoom){await session.LeaveOnlineRoomAsync();throw new Exception("Audit session privacy check failed");}
            opened.Deleted+=()=>File.WriteAllText(Path.Combine(folder,"session-deleted-"+role+".txt"),"Deleted event observed");
            if(role=="host")File.WriteAllText(share,session.OnlineRoomId);
        }
        IEnumerator CleanupRelay()
        {var task=CleanupRelayAsync();while(!task.IsCompleted)yield return null;if(task.IsFaulted)error=task.Exception.GetBaseException().ToString();else cleanupDone=true;}
        async System.Threading.Tasks.Task CleanupRelayAsync()
        {
            var previous=typeof(SideScrollNetworkSession).GetField("onlineSession",Flags).GetValue(session) as Unity.Services.Multiplayer.ISession;
            await session.LeaveOnlineRoomAsync();
            if(role=="host"&&previous!=null&&previous.State!=Unity.Services.Multiplayer.SessionState.Deleted)throw new Exception("Private host session deletion not confirmed");
            if(Unity.Services.Core.UnityServices.State==Unity.Services.Core.ServicesInitializationState.Initialized){var auth=Unity.Services.Authentication.AuthenticationService.Instance;if(auth.IsSignedIn)auth.SignOut();auth.ClearSessionToken();}
        }
        void OnLog(string m,string stack,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert){error=m;File.AppendAllText(Path.Combine(folder,role+"-errors.txt"),m+"\n"+stack+"\n");}}
        object Invoke(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Flags).Invoke(target,args);
        void Tick(){var manager=NetworkManager.Singleton;var match=ArenaObjectiveMatch.Instance;var players=FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None).Where(p=>p.IsSpawned).ToArray();var own=players.FirstOrDefault(p=>p.IsLocalHuman);
            if(own!=null){var motor=own.GetComponent<SideScrollMapPreview>();var shots=own.GetComponentInChildren<RightFacingShotPreview>();var aim=own.GetComponentInChildren<RightFacingAimRig>();if(motor!=null)motor.readKeyboard=false;if(shots!=null)shots.readMouse=false;if(aim!=null)aim.mouseAim=false;}
            string command=Path.Combine(folder,role+"-command.json");if(File.Exists(command))try{var c=JsonUtility.FromJson<Command>(File.ReadAllText(command));if(c!=null&&c.id>ack){Apply(c,own,match,players);ack=c.id;}}catch(IOException){}catch(UnauthorizedAccessException){}catch(Exception e){error=e.ToString();}
            if(Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;
            var s=new Snapshot{role=role,ack=ack,error=error,notice=session.ConnectionNotice,online=session.IsOnlineConnection,onlineBusy=session.OnlineBusy,cleanupDone=cleanupDone,count=players.Length,roster=match!=null?match.ReadyPlayers.Count:0,phase=match!=null?match.State.Value.Phase.ToString():"Disconnected",version=match!=null?match.State.Value.Version:0,result=match!=null?match.Result.Value.ToString():"",time=Time.unscaledTime,players=players.OrderBy(p=>p.PlayerNumber).Select(p=>new Player{number=p.PlayerNumber,hp=p.Health,shield=p.Shield,normal=p.NormalRounds,rifle=p.RifleRounds,grenade=p.GrenadeRounds,life=p.LifeVersion,name=p.Nickname.ToString(),x=p.transform.position.x,y=p.transform.position.y,dead=p.IsDead,protectedNow=p.SpawnProtected,shots=p.ReceivedShots,rifleShots=p.ReceivedRifleShots,grenadeShots=p.ReceivedGrenadeShots}).ToArray(),scores=match!=null?Enumerable.Range(0,match.Scores.Count).Select(i=>match.Scores[i].Total).ToArray():Array.Empty<float>()};
            string path=Path.Combine(folder,role+"-state.json");File.WriteAllText(path,JsonUtility.ToJson(s));}
        void Apply(Command c,SideScrollNetworkPlayer own,ArenaObjectiveMatch match,SideScrollNetworkPlayer[] players){
            if(c.action=="cleanup"){StartCoroutine(CleanupRelay());return;}
            if(c.action=="isolate"){match.Patrols.Clear();match.TurretShots.Clear();return;}
            if(c.action=="exit"){Application.Quit();return;}if(c.action=="disconnect"){session.DisconnectLocal();return;}
            if(c.action=="ready"){match.SetLocalReady(true);return;}if(c.action=="start"){match.roundSeconds=c.amount>0?c.amount:30;match.StartRound();return;}if(c.action=="return"){match.ReturnToRoom();return;}
            if(c.action=="damage"||c.action=="shield"){var p=players.First(p=>p.PlayerNumber==c.target);if(c.action=="damage")p.ReceiveServerDamage(c.amount,p.LifeVersion,null,true);else p.GrantShieldServer(p.LifeVersion);return;}
            if(own==null)return;
            if(c.action=="name"){Invoke(own,"SubmitNicknameRpc",new FixedString64Bytes(c.text));return;}
            if(c.action=="warp"){own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(c.x,c.y));return;}
            if(c.action=="aim"){own.GetComponentInChildren<RightFacingAimRig>().SetAimDirection(new Vector2(c.x,c.y).normalized);return;}
            if(c.action=="mode"){own.SelectAmmo((SideScrollNetworkPlayer.AmmoKind)c.ammo);own.SetSpecialHeld(c.ammo!=0);return;}
            if(c.action=="fire"||c.action=="stale"||c.action=="nan"||c.action=="rapid"){
                var aim=own.GetComponentInChildren<RightFacingAimRig>();var origin=(Vector2)aim.MuzzlePosition;var direction=aim.AimDirection;if(c.action=="nan")direction=new Vector2(float.NaN,0);uint life=c.action=="stale"?own.LifeVersion-1:own.LifeVersion;
                int repeats=c.action=="rapid"?8:1;for(int i=0;i<repeats;i++){if(c.ammo==3)Invoke(own,"FireRifleServerRpc",origin,direction,life);else if(c.ammo==4)Invoke(own,"FireGrenadeServerRpc",origin,direction,12f,life);else Invoke(own,"FireNormalAmmoServerRpc",origin,direction,life);}return;}
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
#endif

