using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkSession
    {
        public const string OnlineRoomKind="foundry-score-20261028-v7";
        public const string OnlineModeName="해킹 구역 점령",OnlineMapName="주조 시설 08";
        public static SideScrollNetworkSession Instance {get;private set;}
        public static bool LobbyVisible=>Instance!=null&&Instance.useReadyRoom&&RelayArenaPrototype.Instance!=null
            &&NetworkManager.Singleton!=null&&!NetworkManager.Singleton.IsListening;
        public bool OnlineBusy {get;private set;}
        public bool IsOnlineConnection=>onlineConnection;
        public string OnlinePublishedPhase=>publishedPhase;
        public string OnlineRoomId=>onlineSession?.Id??"";
        public string OnlineRoomName=>onlineSession?.Name??"";
        public IReadOnlyList<ISessionInfo> OnlineRooms=>onlineRooms;
        readonly List<ISessionInfo> onlineRooms=new();
        ISession onlineSession;
        bool onlineConnection,onlineLeaving,browserOpened,publishing;
        int onlineGeneration;
        float nextRoomQuery,nextPhasePublish,nextAllowedRoomQuery;
        bool onlineListCurrent;
        string publishedPhase="";
        void Awake(){Instance=this;}
        void PrepareConnection(bool online)
        {
            var manager=NetworkManager.Singleton;
            if(manager==null||manager.IsListening||manager.ShutdownInProgress)throw new InvalidOperationException("연결 종료 후 다시 시도해 주세요.");
            bool room=useReadyRoom&&arenaObjectivePrefab!=null&&RelayArenaPrototype.Instance!=null;
            manager.NetworkConfig.ConnectionApproval=room;
            manager.ConnectionApprovalCallback=room?ApproveRoomConnection:null;
            manager.NetworkConfig.ConnectionData=online?Encoding.UTF8.GetBytes(OnlineRoomKind):Array.Empty<byte>();
            onlineConnection=online;ConnectionNotice="";connectedToHost=false;intentionalDisconnect=false;
        }
        async Task EnsureOnlineServices()
        {
            await UnityServices.InitializeAsync();
            if(AuthenticationService.Instance.IsSignedIn)return;
#if UNITY_EDITOR
            // Virtual editors share the project, but must not share one anonymous player.
            AuthenticationService.Instance.SwitchProfile("arena-editor-"+System.Diagnostics.Process.GetCurrentProcess().Id);
#endif
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        public async Task<bool> RefreshOnlineRoomsAsync()
        {
            if(OnlineBusy||onlineLeaving)return false;
            OnlineBusy=true;browserOpened=true;nextRoomQuery=Time.unscaledTime+5;
            try
            {
                float delay=nextAllowedRoomQuery-Time.unscaledTime;
                if(delay>0)await Task.Delay(TimeSpan.FromSeconds(delay));
                if(this==null||!Application.isPlaying)return false;
                await EnsureOnlineServices();nextAllowedRoomQuery=Time.unscaledTime+1.5f;
                var results=await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions{Count=30,
                    FilterOptions=new List<FilterOption>{new(FilterField.StringIndex1,OnlineRoomKind,FilterOperation.Equal)}});
                if(this==null||!Application.isPlaying)return false;
                onlineRooms.Clear();if(results.Sessions!=null)onlineRooms.AddRange(results.Sessions);onlineListCurrent=true;
                ConnectionNotice=onlineRooms.Count==0?"열린 방이 없습니다. 새 방을 만들어 주세요.":"";return true;
            }
            catch(Exception e){onlineListCurrent=false;nextAllowedRoomQuery=Time.unscaledTime+3;OnlineError("방 목록을 불러오지 못했습니다",e);return false;}
            finally{OnlineBusy=false;nextRoomQuery=Time.unscaledTime+5;}
        }
        public Task<bool> CreateOnlineRoomAsync(string name)=>ConnectOnlineAsync(null,name);
        public Task<bool> JoinOnlineRoomAsync(string id)=>ConnectOnlineAsync(id,null);
        async Task<bool> ConnectOnlineAsync(string id,string name)
        {
            var manager=NetworkManager.Singleton;
            if(OnlineBusy||onlineLeaving||onlineSession!=null||manager==null||manager.IsListening||manager.ShutdownInProgress)return false;
            OnlineBusy=true;int generation=++onlineGeneration;ISession opened=null;
            try
            {
                SoloPractice=false;PrepareConnection(true);ConnectionNotice=id==null?"방을 만드는 중…":"방에 입장하는 중…";
                await EnsureOnlineServices();
                if(this==null||generation!=onlineGeneration||!Application.isPlaying)return false;
                if(id==null)
                {
                    name=string.IsNullOrWhiteSpace(name)?"해킹 점령전":name.Trim();if(name.Length>32)name=name.Substring(0,32);
                    opened=await MultiplayerService.Instance.CreateSessionAsync(new SessionOptions{Name=name,MaxPlayers=4,Type=OnlineRoomKind,IsPrivate=false,
                        SessionProperties=new Dictionary<string,SessionProperty>{
                            {"arena",new(OnlineRoomKind,VisibilityPropertyOptions.Public,PropertyIndex.String1)},
                            {"mode",new(OnlineModeName)},{"map",new(OnlineMapName)},{"phase",new("waiting")}}}.WithRelayNetwork());
                }
                else
                {
                    if(string.IsNullOrWhiteSpace(id))throw new ArgumentException("입장할 방을 선택해 주세요.");
                    opened=await MultiplayerService.Instance.JoinSessionByIdAsync(id,new JoinSessionOptions{Type=OnlineRoomKind});
                }
                if(this==null||generation!=onlineGeneration||!Application.isPlaying){await CloseOnlineSession(opened);return false;}
                onlineSession=opened;opened.Deleted+=OnlineSessionClosed;opened.RemovedFromSession+=OnlineSessionClosed;
                publishedPhase=id==null?"waiting":"";browserOpened=false;ConnectionNotice="";
                RelayArenaPrototype.Instance?.SetMenuOpen(false);return true;
            }
            catch(Exception e)
            {
                if(opened!=null)await CloseOnlineSession(opened);
                if(manager!=null&&manager.IsListening)manager.Shutdown();
                string reason=manager!=null?manager.DisconnectReason:"";
                OnlineError(string.IsNullOrEmpty(reason)?"접속하지 못했습니다":reason,e);onlineConnection=false;return false;
            }
            finally{OnlineBusy=false;}
        }
        static async Task CloseOnlineSession(ISession session)
        {
            if(session==null||session.State==SessionState.Deleted)return;
            try{if(session.IsHost)await session.AsHost().DeleteAsync();else await session.LeaveAsync();}
            catch(SessionException e) when(e.Error==SessionError.SessionNotFound||e.Error==SessionError.SessionDeleted){}

        }
        public async Task LeaveOnlineRoomAsync(bool intentional=true)
        {
            if(onlineLeaving)return;
            onlineLeaving=true;intentionalDisconnect=intentional;++onlineGeneration;OnlineBusy=true;
            var manager=sessionManager!=null?sessionManager:NetworkManager.Singleton;
            var session=onlineSession;onlineSession=null;
            if(session!=null){session.Deleted-=OnlineSessionClosed;session.RemovedFromSession-=OnlineSessionClosed;}
            try{await CloseOnlineSession(session);}
            catch(Exception e){OnlineError("방 정리를 완료하지 못했습니다",e);}
            finally
            {
                if(manager!=null&&manager.IsListening)manager.Shutdown();
                onlineConnection=false;onlineLeaving=false;OnlineBusy=false;publishedPhase="";browserOpened=true;nextRoomQuery=Time.unscaledTime+1;
                if(intentional)ConnectionNotice="방에서 나왔습니다.";
            }
        }
        System.Collections.IEnumerator WaitForOnlineClosure()
        {
            // Host deletion stops Relay before its Lobby deletion event arrives.
            // Let the SDK observe that event before trying to remove a vanished membership.
            yield return new WaitForSecondsRealtime(1.5f);
            if(onlineConnection&&!onlineLeaving&&!intentionalDisconnect)_=LeaveOnlineRoomAsync(false);
        }
        void OnlineSessionClosed()
        {if(onlineLeaving)return;ConnectionNotice="방장이 방을 종료했거나 연결이 종료되었습니다.";_=LeaveOnlineRoomAsync(false);}
        void OnlineError(string context,Exception e)
        {
            if(this==null)return;
            ConnectionNotice=context+".\n"+e.Message;
            Debug.LogWarning("[Arena online] "+context+": "+e.ToString());
        }
        void Update()
        {
            if(!LobbyVisible||!Application.isFocused)UpdateLobbyIme(false);
            if(onlineSession!=null&&onlineSession.IsHost&&!onlineLeaving&&!publishing&&Time.unscaledTime>=nextPhasePublish)_=PublishOnlinePhase();
            if(LobbyVisible&&browserOpened&&!OnlineBusy&&!onlineLeaving&&Time.unscaledTime>=nextRoomQuery)_=RefreshOnlineRoomsAsync();
        }
        async Task PublishOnlinePhase()
        {
            var session=onlineSession;if(session==null||!session.IsHost)return;
            var match=ArenaObjectiveMatch.Instance;
            string phase=match!=null&&match.IsSpawned&&match.State.Value.Phase==ArenaObjectiveMatch.Stage.Waiting?"waiting":
                match!=null&&match.State.Value.Phase==ArenaObjectiveMatch.Stage.Finished?"results":"playing";
            if(phase==publishedPhase)return;
            publishing=true;
            try
            {
                var host=session.AsHost();host.IsLocked=phase!="waiting";host.SetProperty("phase",new SessionProperty(phase));
                await host.SavePropertiesAsync();if(onlineSession==session)publishedPhase=phase;
            }
            catch(Exception e)
            {
                OnlineError("방 상태 갱신을 재시도합니다",e);
                // Re-read membership after a leave/rejoin event races the SDK's cached lobby.
                // Keep the NGO approval gate active while the next publish retries.
                if(onlineSession==session&&!onlineLeaving)
                    try{await session.RefreshAsync();}catch(Exception refresh){OnlineError("방 상태 재조회에 실패했습니다",refresh);}
            }
            finally{publishing=false;nextPhasePublish=Time.unscaledTime+2;}
        }
        void CleanupOnlineOnDestroy()
        {
            if(Instance==this)Instance=null;
            ++onlineGeneration;
            if(onlineSession!=null)_=LeaveOnlineRoomAsync();
        }
    }
}

