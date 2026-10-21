using System;
using Unity.Netcode;
using UnityEngine;

namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        public struct ReadyPlayer:INetworkSerializable,IEquatable<ReadyPlayer>
        {
            public ulong Client;public int Number;public bool Ready;
            public void NetworkSerialize<T>(BufferSerializer<T> s) where T:IReaderWriter
            {s.SerializeValue(ref Client);s.SerializeValue(ref Number);s.SerializeValue(ref Ready);}
            public bool Equals(ReadyPlayer p)=>Client==p.Client&&Number==p.Number&&Ready==p.Ready;
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
        readonly NetworkVariable<ReadyRoster> roomRoster=new();
        public ReadyRoster ReadyPlayers=>roomRoster.Value;
        readonly NetworkVariable<bool> roomFlow=new(false);
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
            if(!PracticeRoom)return false;
            var p=new Vector2(screenPoint.x,Screen.height-screenPoint.y);
            float width=Mathf.Min(540,Screen.width-30);
            return new Rect((Screen.width-width)*.5f,15,width,180).Contains(p)
                ||new Rect((Screen.width-width)*.5f,Screen.height-120,width,105).Contains(p)
                ||new Rect(Screen.width-230,15,215,160).Contains(p);
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
                if(!RoomFlow||State.Value.Phase!=Stage.Waiting||ReadyPlayers.Count<2||ReadyPlayers.Count>4)return false;
                if(IsServer&&NetworkManager.ConnectedClients.Count!=ReadyPlayers.Count)return false;
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
                roomFlow.Value=RelayArenaPrototype.Instance!=null&&session!=null&&session.useReadyRoom;
                respawnDuration.Value=Mathf.Max(.1f,respawnSeconds);countdownDuration.Value=Mathf.Max(.1f,countdownSeconds);
                roomRoster.Value=default;
            }
        }
        void ClearReady(){var roster=ReadyPlayers;for(int i=0;i<roster.Count;i++){var p=roster[i];p.Ready=false;roster[i]=p;}roomRoster.Value=roster;}
        void SynchronizeRoomRoster()
        {
            var players=FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
            Array.Sort(players,(a,b)=>a.PlayerNumber.CompareTo(b.PlayerNumber));
            var next=new ReadyRoster();var old=ReadyPlayers;
            foreach(var p in players)
            {
                if(!p.IsSpawned||!NetworkManager.ConnectedClients.ContainsKey(p.OwnerClientId)||next.Count>=4)continue;
                var entry=new ReadyPlayer{Client=p.OwnerClientId,Number=p.PlayerNumber};
                foreach(var previous in old)if(previous.Client==entry.Client)entry.Ready=previous.Ready;
                next[next.Count++]=entry;
            }
            bool changed=next.Count!=old.Count;
            for(int i=0;i<Math.Min(next.Count,old.Count);i++)if(next[i].Client!=old[i].Client||next[i].Number!=old[i].Number)changed=true;
            if(changed&&State.Value.Phase==Stage.Waiting)for(int i=0;i<next.Count;i++){var p=next[i];p.Ready=false;next[i]=p;}
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
                foreach(var p in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsSpawned&&p.IsOwner&&p.HasCurrentLifePose&&CombatPosition(p.transform.position))
                {ReportArrivalServerRpc(p.LifeVersion,State.Value.Version);reportedArrivalVersion=State.Value.Version;break;}
            if(!IsServer)return;
            SynchronizeRoomRoster();
            var state=State.Value;
            if(state.Phase==Stage.Starting)
            {
                if(ReadyPlayers.Count<2){EnterWaiting();return;}
                if(Now>=state.End)BeginCombat();
            }
            else if(state.Phase==Stage.Countdown)
            {
                if(ReadyPlayers.Count<2||state.End==0&&Now>=arrivalDeadline){EnterWaiting();return;}
                int expected=0;foreach(var p in ReadyPlayers)expected|=1<<(p.Number-1);
                if(state.End==0&&(arrivals.Value&expected)==expected){state.End=Now+CountdownSeconds;State.Value=state;}
                else if(state.End>0&&Now>=state.End)
                {
                    double now=Now;state.Phase=Stage.Playing;state.Version++;state.End=now+Mathf.Max(1,roundSeconds);state.Switch=now+Mathf.Max(1,pointSeconds);state.Warning=state.Switch-Mathf.Clamp(warningSeconds,0,Mathf.Max(1,pointSeconds));State.Value=state;lastTick=now;nextTick=now;
                }
            }
            else if(state.Phase==Stage.Returning&&Now>=state.End)EnterWaiting();
            else if(state.Phase==Stage.Playing&&ReadyPlayers.Count<2)
            {
                state.Phase=Stage.Finished;State.Value=state;Occupants.Value=0;OccupantNumber.Value=0;
                Result.Value=new Unity.Collections.FixedString128Bytes("참가자가 부족하여 경기를 종료했습니다.");
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
            foreach(var p in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsSpawned&&p.OwnerClientId==rpc.Receive.SenderClientId&&p.LifeVersion==life)arrivals.Value|=1<<(p.PlayerNumber-1);
        }
        void EnterWaiting()
        {
            State.Value=new RoundState{Phase=Stage.Waiting,Version=State.Value.Version+1};
            ClearReady();Scores.Clear();Result.Value=default;Occupants.Value=0;OccupantNumber.Value=0;
            foreach(var p in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsSpawned)p.ResetForArenaRound();
        }
        void DrawRoom()
        {
            if(Transitioning)
            {
                int depth=GUI.depth;GUI.depth=-100;
                var color=GUI.color;GUI.color=new Color(.035f,.055f,.08f,1);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=color;
                GUI.Label(new Rect(Screen.width*.5f-180,Screen.height*.5f-25,360,50),State.Value.Phase==Stage.Starting?"전투 지역으로 이동합니다…":"대기방으로 돌아갑니다…",new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=22});GUI.depth=depth;return;
            }
            float width=Mathf.Min(540,Screen.width-30);
            GUILayout.BeginArea(new Rect((Screen.width-width)*.5f,15,width,180),GUI.skin.box);
            GUILayout.Label("준비 공간 · 해킹 구역 점유 · 2~4명 · 3분");
            var sorted=new ReadyPlayer[ReadyPlayers.Count];for(int i=0;i<sorted.Length;i++)sorted[i]=ReadyPlayers[i];Array.Sort(sorted,(a,b)=>a.Number.CompareTo(b.Number));
            foreach(var p in sorted)GUILayout.Label("P"+p.Number+(p.Client==Unity.Netcode.NetworkManager.ServerClientId?"  방장":"")+(p.Client==NetworkManager.LocalClientId?"  (나)":"")+"     "+(p.Ready?"준비 완료":"준비 중"));
            GUILayout.Label("A/D 이동 · Space 점프 · 사격 연습 · 체력은 최소 1 유지 · 참가 인원이 바뀌면 준비가 해제됩니다.");
            GUILayout.EndArea();
            GUILayout.BeginArea(new Rect((Screen.width-width)*.5f,Screen.height-120,width,105),GUI.skin.box);
            bool ready=IsReady(NetworkManager.LocalClientId);
            GUILayout.BeginHorizontal();if(GUILayout.Button(ready?"준비 취소":"준비",GUILayout.Height(32)))SetLocalReady(!ready);
            if(IsServer){bool enabled=GUI.enabled;GUI.enabled=CanStart;if(GUILayout.Button("경기 시작",GUILayout.Height(32)))StartRound();GUI.enabled=enabled;}
            if(GUILayout.Button(IsServer?"방 닫고 나가기":"방 나가기",GUILayout.Height(32)))FindFirstObjectByType<SideScrollNetworkSession>()?.DisconnectLocal();GUILayout.EndHorizontal();
            GUILayout.Label(ReadyPlayers.Count<2?"다른 플레이어를 기다립니다. 혼자서는 시작할 수 없습니다.":CanStart?(IsServer?"전원 준비 완료. 경기 시작을 눌러주세요.":"방장의 시작을 기다립니다."):"모두 준비하면 방장이 경기를 시작할 수 있습니다.");
            GUILayout.EndArea();
        }
    }
}
