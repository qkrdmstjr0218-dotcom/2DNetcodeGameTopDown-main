using System;
using Unity.Netcode;
using UnityEngine;

namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        public struct ReadyPlayer:INetworkSerializable,IEquatable<ReadyPlayer>
        {
            public ulong Client;public int Number;public bool Ready;public Unity.Collections.FixedString64Bytes Nickname;
            public void NetworkSerialize<T>(BufferSerializer<T> s) where T:IReaderWriter
            {s.SerializeValue(ref Client);s.SerializeValue(ref Number);s.SerializeValue(ref Ready);s.SerializeValue(ref Nickname);}
            public bool Equals(ReadyPlayer p)=>Client==p.Client&&Number==p.Number&&Ready==p.Ready&&Nickname.Equals(p.Nickname);
        }
        // A bounded snapshot also stays consistent when several clients join
        // during the same tick; no initial-list/add-event overlap is possible.
        public struct ReadyRoster:INetworkSerializable,IEquatable<ReadyRoster>
        {
            public int Count;ReadyPlayer a,b,c,d;
            public ReadyPlayer this[int i]
            {get=>i==0?a:i==1?b:i==2?c:d;set{if(i==0)a=value;else if(i==1)b=value;else if(i==2)c=value;else d=value;}}
            public void NetworkSerialize<T>(BufferSerializer<T> s) where T:IReaderWriter
            {s.SerializeValue(ref Count);s.SerializeValue(ref a);s.SerializeValue(ref b);s.SerializeValue(ref c);s.SerializeValue(ref d);}
            public bool Equals(ReadyRoster other)=>Count==other.Count&&a.Equals(other.a)&&b.Equals(other.b)&&c.Equals(other.c)&&d.Equals(other.d);
            public System.Collections.Generic.IEnumerator<ReadyPlayer> GetEnumerator(){for(int i=0;i<Count;i++)yield return this[i];}
        }
        readonly NetworkVariable<Unity.Collections.FixedString128Bytes> roomTitle=new();
        public string RoomTitle=>roomTitle.Value.ToString();
        bool markerGuide;int markerGuideVersion=-1;
        public static bool MarkerGuideOpen=>PracticeRoom&&Instance.markerGuide&&Instance.markerGuideVersion==Instance.State.Value.Version;
        readonly NetworkVariable<ReadyRoster> roomRoster=new();
        public ReadyRoster ReadyPlayers=>roomRoster.Value;
        readonly NetworkVariable<bool> roomFlow=new(false);
        readonly NetworkVariable<bool> soloPractice=new(false);
        public bool SoloPractice=>soloPractice.Value;
        int MinimumPlayers=>SoloPractice?1:2;
        int roomVersion=-1;
        int reportedArrivalVersion=-1;
        readonly NetworkVariable<int> arrivals=new(0);
        double arrivalDeadline;
        [SerializeField,Min(.1f)] float respawnSeconds;
        [SerializeField,Min(.1f)] float countdownSeconds;
        readonly NetworkVariable<float> respawnDuration=new(),countdownDuration=new();
        public float RespawnSeconds=>respawnDuration.Value;
        public float CountdownSeconds=>countdownDuration.Value;
        public bool RoomFlow=>roomFlow.Value;
        public bool Transitioning=>RoomFlow&&(State.Value.Phase==Stage.Starting||State.Value.Phase==Stage.Returning);
        public bool RoomView=>RoomFlow&&(State.Value.Phase==Stage.Waiting||State.Value.Phase==Stage.Starting||State.Value.Phase==Stage.Returning);
        public static bool CountingDown=>Instance!=null&&Instance.IsSpawned&&Instance.RoomFlow&&Instance.State.Value.Phase==Stage.Countdown;
        public static bool InReadyRoom=>Instance!=null&&Instance.IsSpawned&&Instance.RoomView;
        public static bool PracticeRoom=>Instance!=null&&Instance.IsSpawned&&Instance.RoomFlow&&Instance.State.Value.Phase==Stage.Waiting;
        public static bool TransitionActive=>Instance!=null&&Instance.IsSpawned&&Instance.Transitioning;
        public static bool CombatBlocked=>Ended||Instance!=null&&Instance.IsSpawned&&Instance.RoomFlow&&Instance.State.Value.Phase!=Stage.Playing&&!PracticeRoom;
        public static bool RoomUIContains(Vector2 screenPoint)
        {
            if(ArenaTutorial.Active)return ArenaTutorial.PointerOverUI(screenPoint);
            if(!PracticeRoom)return false;
            if(MarkerGuideOpen)return true;
            var p=new Vector2(screenPoint.x,Screen.height-screenPoint.y);
            float width=Mathf.Min(640,Screen.width-32);
            return new Rect(16,16,250,84+Instance.ReadyPlayers.Count*66).Contains(p)
                ||new Rect((Screen.width-width)*.5f,Screen.height-146,width,130).Contains(p)
                ;
        }
        public static bool RoomPointerOverUI
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var mouse=UnityEngine.InputSystem.Mouse.current;
                return mouse!=null&&RoomUIContains(mouse.position.ReadValue());
#elif ENABLE_LEGACY_INPUT_MANAGER
                return RoomUIContains(Input.mousePosition);
#else
                return false;
#endif
            }
        }
        public bool CanStart
        {
            get
            {
                if(ArenaTutorial.Active||!RoomFlow||State.Value.Phase!=Stage.Waiting||ReadyPlayers.Count<MinimumPlayers||ReadyPlayers.Count>4)return false;
                if(IsServer&&NetworkManager.ConnectedClients.Count+(SideScrollNetworkSession.Instance!=null?SideScrollNetworkSession.Instance.BotCount:0)!=ReadyPlayers.Count)return false;
                foreach(var p in ReadyPlayers)if(!p.Ready)return false;
                return true;
            }
        }
        public bool IsReady(ulong client){foreach(var p in ReadyPlayers)if(p.Client==client)return p.Ready;return false;}
        void InitializeRoom()
        {
            if(IsServer)
            {
                var session=FindFirstObjectByType<SideScrollNetworkSession>();
                soloPractice.Value=session!=null&&session.SoloPractice;
                UpdateRoomTitle();
                roomFlow.Value=RelayArenaPrototype.Instance!=null&&session!=null&&session.useReadyRoom;
                respawnDuration.Value=Mathf.Max(.1f,respawnSeconds);countdownDuration.Value=Mathf.Max(.1f,countdownSeconds);
                roomRoster.Value=default;
            }
        }
        void UpdateRoomTitle()
        {
            var session=SideScrollNetworkSession.Instance;
            string title=session!=null?session.OnlineRoomName:"";
            if(string.IsNullOrWhiteSpace(title))title=SoloPractice?"혼자 연습":"로컬 테스트";
            if(title.Length>32)title=title.Substring(0,32);
            if(title.Length>0&&char.IsHighSurrogate(title[title.Length-1]))title=title.Substring(0,title.Length-1);
            roomTitle.Value=new Unity.Collections.FixedString128Bytes(title);
        }
        void ClearReady(){var roster=ReadyPlayers;for(int i=0;i<roster.Count;i++){var p=roster[i];p.Ready=false;roster[i]=p;}roomRoster.Value=roster;}
        void SynchronizeRoomRoster()
        {
            UpdateRoomTitle();
            var players=FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
            Array.Sort(players,(a,b)=>a.PlayerNumber.CompareTo(b.PlayerNumber));
            var next=new ReadyRoster();var old=ReadyPlayers;
            foreach(var p in players)
            {
                if(!p.IsSpawned||!p.IsBot&&!NetworkManager.ConnectedClients.ContainsKey(p.ParticipantId)||next.Count>=4)continue;
                var entry=new ReadyPlayer{Client=p.ParticipantId,Number=p.PlayerNumber,Nickname=p.Nickname};
                foreach(var previous in old)if(previous.Client==entry.Client)entry.Ready=previous.Ready;
                next[next.Count++]=entry;
            }
            bool changed=next.Count!=old.Count;
            for(int i=0;i<Math.Min(next.Count,old.Count);i++)if(next[i].Client!=old[i].Client||next[i].Number!=old[i].Number)changed=true;
            for(int i=0;i<next.Count;i++)if(next[i].Client>ulong.MaxValue-5){var b=next[i];b.Ready=true;next[i]=b;}
            if(changed&&State.Value.Phase==Stage.Waiting)for(int i=0;i<next.Count;i++){var p=next[i];p.Ready=p.Client>ulong.MaxValue-5;next[i]=p;}
            if(!next.Equals(old))roomRoster.Value=next;
        }
        public void SetLocalReady(bool ready)
        {if(IsSpawned&&RoomFlow&&State.Value.Phase==Stage.Waiting)SetReadyServerRpc(ready);}
        [ServerRpc(RequireOwnership=false)]
        void SetReadyServerRpc(bool ready,ServerRpcParams rpc=default)
        {
            if(!RoomFlow||State.Value.Phase!=Stage.Waiting)return;
            SynchronizeRoomRoster();
            var roster=ReadyPlayers;
            for(int i=0;i<roster.Count;i++)if(roster[i].Client==rpc.Receive.SenderClientId)
            {var p=roster[i];p.Ready=ready;roster[i]=p;roomRoster.Value=roster;return;}
        }
        public bool ReturnToRoom()
        {
            if(!IsServer||!IsSpawned||!RoomFlow||State.Value.Phase!=Stage.Finished)return false;
            ClearReady();State.Value=new RoundState{Phase=Stage.Returning,Version=State.Value.Version+1,End=Now+1.2};return true;
        }
        void UpdateRoom()
        {
            if(!RoomFlow)return;
            if(roomVersion!=State.Value.Version)
            {roomVersion=State.Value.Version;RelayArenaPrototype.Instance?.SetMenuOpen(false);if(RelayArenaPrototype.Instance!=null)RelayArenaPrototype.Instance.overview=false;}
            if(RelayArenaPrototype.Instance!=null&&RoomView)RelayArenaPrototype.Instance.overview=false;
            if(State.Value.Phase==Stage.Countdown&&reportedArrivalVersion!=State.Value.Version)
                foreach(var p in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsSpawned&&p.IsLocalHuman&&p.HasCurrentLifePose&&CombatPosition(p.transform.position))
                {ReportArrivalServerRpc(p.LifeVersion,State.Value.Version);reportedArrivalVersion=State.Value.Version;break;}
            if(!IsServer)return;
            SynchronizeRoomRoster();
            if(State.Value.Phase==Stage.Countdown)foreach(var b in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(b.IsSpawned&&b.IsBot&&b.HasCurrentLifePose&&CombatPosition(b.transform.position))arrivals.Value|=1<<(b.PlayerNumber-1);
            var state=State.Value;
            if(state.Phase==Stage.Starting)
            {
                if(ReadyPlayers.Count<MinimumPlayers){EnterWaiting();return;}
                if(Now>=state.End)BeginCombat();
            }
            else if(state.Phase==Stage.Countdown)
            {
                if(ReadyPlayers.Count<MinimumPlayers||state.End==0&&Now>=arrivalDeadline){EnterWaiting();return;}
                int expected=0;foreach(var p in ReadyPlayers)expected|=1<<(p.Number-1);
                if(state.End==0&&(arrivals.Value&expected)==expected){state.End=Now+CountdownSeconds;State.Value=state;}
                else if(state.End>0&&Now>=state.End)
                {
                    double now=Now;state.Phase=Stage.Playing;state.Version++;state.End=now+Mathf.Max(1,roundSeconds);state.Switch=now+Mathf.Max(1,pointSeconds);state.Warning=state.Switch-Mathf.Clamp(warningSeconds,0,Mathf.Max(1,pointSeconds));State.Value=state;lastTick=now;nextTick=now;
                    foreach(var player in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(player.IsSpawned)player.GrantSpawnProtectionServer();
                }
            }
            else if(state.Phase==Stage.Returning&&Now>=state.End)EnterWaiting();
            else if(state.Phase==Stage.Playing&&ReadyPlayers.Count<MinimumPlayers)
            {
                FinishRound("참가자가 부족하여 경기를 종료했습니다.");
            }
        }
        static bool CombatPosition(Vector3 position)
        {
            var map=RelayArenaPrototype.Instance!=null?RelayArenaPrototype.Instance.GetComponent<ArenaMapSettings>():null;
            return map!=null?map.ContainsCombatPosition(position):position.x<30;
        }
        [ServerRpc(RequireOwnership=false)]
        void ReportArrivalServerRpc(uint life,int version,ServerRpcParams rpc=default)
        {
            if(!RoomFlow||State.Value.Phase!=Stage.Countdown||State.Value.Version!=version)return;
            foreach(var p in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsSpawned&&!p.IsBot&&p.ParticipantId==rpc.Receive.SenderClientId&&p.LifeVersion==life)arrivals.Value|=1<<(p.PlayerNumber-1);
        }
        void EnterWaiting()
        {
            markerGuide=false;killNotices.Clear();
            State.Value=new RoundState{Phase=Stage.Waiting,Version=State.Value.Version+1};
            ClearReady();Scores.Clear();Result.Value=default;Occupants.Value=0;OccupantNumber.Value=0;
            foreach(var p in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsSpawned)p.ResetForArenaRound();
        }
        void DrawRoomTitle(Rect rect)
        {
            var style=ArenaUiTheme.Static(new GUIStyle(GUI.skin.label){fontSize=13,richText=false,wordWrap=false,clipping=TextClipping.Clip});
            string full=RoomTitle,value=full;
            while(style.fontSize>9&&style.CalcSize(new GUIContent(value)).x>rect.width-4)style.fontSize--;
            if(style.CalcSize(new GUIContent(value)).x>rect.width-4)
            {
                while(value.Length>0&&style.CalcSize(new GUIContent(value+"…")).x>rect.width-4)value=value.Substring(0,value.Length-1);
                if(value.Length>0&&char.IsHighSurrogate(value[value.Length-1]))value=value.Substring(0,value.Length-1);
                value+="…";
            }
            GUI.Label(rect,new GUIContent(value,full),style);
        }
        void DrawRoom()
        {
            if(markerGuideVersion!=State.Value.Version){markerGuide=false;markerGuideVersion=State.Value.Version;}
            if(ArenaTutorial.Active||RelayArenaPrototype.Instance!=null&&RelayArenaPrototype.Instance.MenuOpen)return;
            if(Transitioning)
            {
                ArenaTransitionHud.Transition(State.Value.Phase==Stage.Returning);return;
            }
            using(ArenaUiTheme.Use())
            {
                float width=Mathf.Min(640,Screen.width-32);
                GUILayout.BeginArea(new Rect(16,16,250,84+ReadyPlayers.Count*66),GUI.skin.box);
                int readyCount=0;foreach(var entry in ReadyPlayers)if(entry.Ready)readyCount++;
                GUILayout.BeginHorizontal();GUILayout.Label(SoloPractice?"혼자 연습":"출격 대기",new GUIStyle(ArenaUiTheme.Title){fontSize=20});
                GUILayout.FlexibleSpace();GUILayout.Label("준비 "+readyCount+" / "+ReadyPlayers.Count,new GUIStyle(ArenaUiTheme.Muted){alignment=TextAnchor.MiddleRight,fontSize=12,wordWrap=false},GUILayout.Width(85));GUILayout.EndHorizontal();
                if(SoloPractice)GUILayout.Label("참가자",ArenaUiTheme.Muted,GUILayout.Height(22));else DrawRoomTitle(GUILayoutUtility.GetRect(218,22));
                var sorted=new ReadyPlayer[ReadyPlayers.Count];for(int i=0;i<sorted.Length;i++)sorted[i]=ReadyPlayers[i];Array.Sort(sorted,(a,b)=>a.Number.CompareTo(b.Number));
                foreach(var p in sorted)
                {
                    var row=GUILayoutUtility.GetRect(218,58);bool self=p.Client==NetworkManager.LocalClientId;
                    var c=GUI.color;GUI.color=self?new Color(.065f,.15f,.18f,.95f):new Color(.045f,.085f,.105f,.9f);GUI.DrawTexture(row,Texture2D.whiteTexture);GUI.color=c;
                    if(self){GUI.color=new Color(.65f,.95f,1);GUI.DrawTexture(new Rect(row.x,row.y,3,row.height),Texture2D.whiteTexture);GUI.color=c;}
                    ArenaHudSkin.PlayerBadge(new Rect(row.x+8,row.y+13,32,32),p.Number);
                    ArenaPlayerNames.CompactLabel(new Rect(row.x+50,row.y+2,160,25),ArenaPlayerNames.Name(p.Nickname),12);
                    bool bot=false;foreach(var player in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(player.IsSpawned&&player.ParticipantId==p.Client){bot=player.IsBot;break;}
                    string role=self?"본인":bot?"AI":"참가자";if(p.Client==Unity.Netcode.NetworkManager.ServerClientId)role+=" · 방장";
                    GUI.Label(new Rect(row.x+50,row.y+34,72,18),role,ArenaUiTheme.Static(new GUIStyle(GUI.skin.label){fontSize=11,wordWrap=false,padding=new RectOffset(),normal={textColor=self?new Color(.65f,.95f,1):new Color(.55f,.69f,.73f)}}));
                    ArenaHudSkin.ReadyBadge(new Rect(row.xMax-96,row.y+30,88,24),p.Ready);GUILayout.Space(8);
                }
                GUILayout.EndArea();
                GUILayout.BeginArea(new Rect((Screen.width-width)*.5f,Screen.height-146,width,130),GUI.skin.box);
                bool ready=IsReady(NetworkManager.LocalClientId);var enabled=GUI.enabled;
                GUI.enabled=enabled&&!markerGuide;
                GUILayout.BeginHorizontal();
                if(ArenaUiTheme.Button(ready?"준비 취소":"출격 준비",GUILayout.Height(38)))SetLocalReady(!ready);
                if(IsServer){GUI.enabled=enabled&&!markerGuide&&CanStart;if(ArenaUiTheme.Button(SoloPractice?"연습 시작":"경기 시작",GUILayout.Height(38)))StartRound();GUI.enabled=enabled&&!markerGuide;}
                if(ArenaUiTheme.Button("규칙 설명",GUILayout.Width(96),GUILayout.Height(38)))OpenRules();
                if(ArenaUiTheme.Button("로비로 나가기",GUILayout.Height(38)))RelayArenaPrototype.Instance?.RequestLeave();GUILayout.EndHorizontal();
                GUILayout.Label(SoloPractice?"조작을 연습한 뒤 준비하고 출발하세요.":ReadyPlayers.Count<MinimumPlayers?"다른 플레이어를 기다리는 중입니다.":CanStart?"모두 준비했습니다. 방장이 경기를 시작할 수 있습니다.":"준비를 마치면 방장이 경기를 시작합니다.",ArenaUiTheme.Muted);
                var session=SideScrollNetworkSession.Instance;
                if(session!=null&&session.CanEditBots)
                {
                    GUILayout.Space(6);GUILayout.BeginHorizontal();
                    GUILayout.Label("AI 연습 상대 "+session.BotCount+" / 3",ArenaUiTheme.Muted,GUILayout.Height(28));GUILayout.FlexibleSpace();
                    if(ArenaUiTheme.CompactButton("난이도: "+session.BotDifficultyName,GUILayout.Width(132),GUILayout.Height(28)))session.SetBotDifficulty((session.BotDifficulty+1)%3);
                    GUI.enabled=enabled&&!markerGuide&&session.BotCount<3;if(ArenaUiTheme.CompactButton("AI 추가",GUILayout.Width(100),GUILayout.Height(28)))session.AddPracticeBot();
                    GUI.enabled=enabled&&!markerGuide&&session.BotCount>0;if(ArenaUiTheme.CompactButton("AI 제거",GUILayout.Width(100),GUILayout.Height(28)))session.RemovePracticeBot();
                    GUILayout.EndHorizontal();
                }
                GUI.enabled=enabled;GUILayout.EndArea();
                if(markerGuide)DrawRules();
            }
        }
    }
}
