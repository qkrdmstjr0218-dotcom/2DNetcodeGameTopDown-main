using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace RightWalkPrototype
{
    [DefaultExecutionOrder(160)]
    public sealed partial class ArenaObjectiveMatch : NetworkBehaviour
    {
        public enum Stage:byte {Waiting,Playing,Finished,Starting,Returning,Countdown}
        public struct RoundState:INetworkSerializable,IEquatable<RoundState>
        {
            public Stage Phase;public int Version,Point;public double End,Switch,Warning;
            public void NetworkSerialize<T>(BufferSerializer<T> s) where T:IReaderWriter
            {byte p=(byte)Phase;s.SerializeValue(ref p);Phase=(Stage)p;s.SerializeValue(ref Version);s.SerializeValue(ref Point);s.SerializeValue(ref End);s.SerializeValue(ref Switch);s.SerializeValue(ref Warning);}
            public bool Equals(RoundState o)=>Phase==o.Phase&&Version==o.Version&&Point==o.Point&&End==o.End&&Switch==o.Switch&&Warning==o.Warning;
        }
        public struct Score:INetworkSerializable,IEquatable<Score>
        {
            public ulong Client;public int Number;public float Seconds;
            public void NetworkSerialize<T>(BufferSerializer<T> s) where T:IReaderWriter{s.SerializeValue(ref Client);s.SerializeValue(ref Number);s.SerializeValue(ref Seconds);}
            public bool Equals(Score o)=>Client==o.Client&&Number==o.Number&&Seconds==o.Seconds;
        }
        public float roundSeconds=180,pointSeconds=30,warningSeconds=5;
        public readonly NetworkVariable<RoundState> State=new(new RoundState{Phase=Stage.Waiting});
        public readonly NetworkVariable<int> Occupants=new(0),OccupantNumber=new(0);
        public readonly NetworkVariable<FixedString128Bytes> Result=new();
        public NetworkList<Score> Scores;
        public static ArenaObjectiveMatch Instance {get;private set;}
        public static bool Ended=>Instance!=null&&Instance.IsSpawned&&Instance.State.Value.Phase==Stage.Finished;
        public ArenaObjectivePoint[] Points {get;private set;}
        public double Now=>NetworkManager!=null?NetworkManager.ServerTime.Time:0;
        public int NextPoint=>(State.Value.Point+1)%Mathf.Max(1,Points.Length);
        public bool PreviewNext=>State.Value.Phase==Stage.Playing&&Now>=State.Value.Warning&&State.Value.Switch<State.Value.End;
        double lastTick,nextTick;
        void Awake(){Scores=new NetworkList<Score>();AmmoPickups=new NetworkList<AmmoPickup>();Patrols=new NetworkList<PatrolState>();}
        public override void OnNetworkSpawn()
        {
            Instance=this;
            var layout=FindFirstObjectByType<ArenaObjectiveLayout>();Points=layout!=null?layout.points:Array.Empty<ArenaObjectivePoint>();
            if(IsServer){Scores.Clear();State.Value=new RoundState{Phase=Stage.Waiting};Result.Value=default;lastTick=Now;nextTick=Now;}
            InitializeRoom();
            InitializeAmmoRules();pickupVersion=-1;patrolVersion=-1;
        }
        public override void OnNetworkDespawn()
        {ClearPatrolVisuals();ClearPickupVisuals();if(Points!=null)foreach(var p in Points)if(p!=null)p.Show(false,false,Color.white);if(Instance==this)Instance=null;}
        public bool StartRound()
        {
            if(!IsSpawned||!IsServer||Points.Length==0)return false;
            if(RoomFlow)
            {
                SynchronizeRoomRoster();
                if(!CanStart)return false;
                State.Value=new RoundState{Phase=Stage.Starting,Version=State.Value.Version+1,End=Now+1.5};
                ClearReady();return true;
            }
            return BeginCombat();
        }
        bool BeginCombat()
        {
            var players=FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
            if(players.Length==0)return false;
            double now=Now;var old=State.Value;
            State.Value=new RoundState{Phase=RoomFlow?Stage.Countdown:Stage.Playing,Version=old.Version+1,Point=0,End=RoomFlow?0:now+Mathf.Max(1,roundSeconds),Switch=now+Mathf.Max(1,pointSeconds),Warning=now+Mathf.Max(1,pointSeconds)-Mathf.Clamp(warningSeconds,0,Mathf.Max(1,pointSeconds))};
            if(RoomFlow){arrivals.Value=0;arrivalDeadline=now+15;}
            Scores.Clear();foreach(var p in players)if(p.IsSpawned){Scores.Add(new Score{Client=p.OwnerClientId,Number=p.PlayerNumber});p.ResetForArenaRound();}
            Result.Value=default;Occupants.Value=0;OccupantNumber.Value=0;lastTick=now;nextTick=now;
            RelayArenaPrototype.Instance?.SetMenuOpen(false);return true;
        }
        void Update()
        {
            if(!IsSpawned||Points==null)return;
            UpdateRoom();
            if(IsServer&&Now>=nextTick){ServerTick();nextTick=Now+.1;}
            UpdateAmmoPickups();UpdatePatrols();
            var state=State.Value;
            Color color=Occupants.Value>1?new Color(1,.65f,.25f):Occupants.Value==1?new Color(.35f,1,.65f):new Color(.25f,.85f,1);
            for(int i=0;i<Points.Length;i++)Points[i].Show(state.Phase==Stage.Playing&&i==state.Point,PreviewNext&&i==NextPoint,color);
        }
        void ServerTick()
        {
            var state=State.Value;if(state.Phase!=Stage.Playing)return;
            double now=Now,until=Math.Min(now,state.End);float dt=(float)Math.Max(0,until-lastTick);lastTick=until;
            var players=FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
            // Scores belong to a connection, so a reused P number cannot inherit them.
            for(int i=Scores.Count-1;i>=0;i--){bool found=false;foreach(var p in players)if(p.IsSpawned&&p.OwnerClientId==Scores[i].Client)found=true;if(!found)Scores.RemoveAt(i);}
            foreach(var p in players)if(p.IsSpawned){bool found=false;for(int i=0;i<Scores.Count;i++)if(Scores[i].Client==p.OwnerClientId)found=true;if(!found)Scores.Add(new Score{Client=p.OwnerClientId,Number=p.PlayerNumber});}
            bool switched=false;
            while(until>=state.Switch&&state.Switch<state.End)
            {state.Point=(state.Point+1)%Points.Length;state.Switch+=Mathf.Max(1,pointSeconds);state.Warning=state.Switch-Mathf.Clamp(warningSeconds,0,Mathf.Max(1,pointSeconds));switched=true;}
            int count=0;SideScrollNetworkPlayer sole=null;
            foreach(var p in players)if(Points[state.Point].Contains(p)){count++;sole=p;}
            Occupants.Value=count;OccupantNumber.Value=count==1?sole.PlayerNumber:0;
            // Discard the sub-tick crossing a relocation rather than score the wrong site.
            if(count==1&&!switched)for(int i=0;i<Scores.Count;i++)if(Scores[i].Client==sole.OwnerClientId){var score=Scores[i];score.Seconds+=dt;Scores[i]=score;break;}
            if(now>=state.End)
            {
                state.Phase=Stage.Finished;Occupants.Value=0;OccupantNumber.Value=0;
                float best=0;for(int i=0;i<Scores.Count;i++)best=Mathf.Max(best,Scores[i].Seconds);
                string winners="";int ties=0;
                for(int i=0;i<Scores.Count;i++)if(Mathf.Abs(Scores[i].Seconds-best)<.05f){winners+=(ties++>0?" / ":"")+"P"+Scores[i].Number;}
                Result.Value=new FixedString128Bytes(best<.05f?"획득 점수 없음 · 무승부":ties==1?winners+" 승리":winners+" 공동 1위");
            }
            State.Value=state;
        }
        public float ScoreFor(ulong client){for(int i=0;i<Scores.Count;i++)if(Scores[i].Client==client)return Scores[i].Seconds;return 0;}
        void OnGUI()
        {
            if(!IsSpawned||Points==null||Points.Length==0)return;
            if(RoomFlow&&(State.Value.Phase==Stage.Waiting||Transitioning)){DrawRoom();return;}
            if(RoomFlow&&State.Value.Phase==Stage.Countdown)
            {GUI.Label(new Rect(Screen.width*.5f-250,Screen.height*.5f-65,500,130),State.Value.End==0?"플레이어 도착을 기다립니다…":Mathf.Max(1,Mathf.CeilToInt((float)(State.Value.End-Now))).ToString(),new GUIStyle(GUI.skin.box){alignment=TextAnchor.MiddleCenter,fontSize=40});return;}
            var state=State.Value;
            if(RoomFlow&&state.Phase==Stage.Playing&&state.End-Now>roundSeconds-.7f)GUI.Label(new Rect(Screen.width*.5f-160,Screen.height*.5f-50,320,100),"시작!",new GUIStyle(GUI.skin.box){alignment=TextAnchor.MiddleCenter,fontSize=40});
            GUILayout.BeginArea(new Rect(Screen.width*.5f-210,91,420,125),GUI.skin.box);
            if(state.Phase==Stage.Waiting)
            {GUILayout.Label("해킹 구역 시험 · 구역 단독 점유 1초 = 1점");GUILayout.Label("구역 안에서 사격 가능 · 2명 이상이면 득점 중단");if(IsServer){if(GUILayout.Button("시험 경기 시작 (3분)"))StartRound();}else GUILayout.Label("호스트의 경기 시작을 기다립니다.");}
            else if(state.Phase==Stage.Playing)
            {
                int left=Mathf.Max(0,Mathf.CeilToInt((float)(state.End-Now)));
                GUILayout.Label("남은 시간 "+left/60+":"+(left%60).ToString("00")+" · "+Points[state.Point].displayName);
                GUILayout.Label(Occupants.Value>1?"경합 중 · 득점 중단":Occupants.Value==1?"P"+OccupantNumber.Value+" 득점 중":"구역으로 이동하세요");
                GUILayout.Label(PreviewNext?"곧 이동 → "+Points[NextPoint].displayName+" ("+Mathf.Max(0,Mathf.CeilToInt((float)(state.Switch-Now)))+"초)":"지점 이동까지 "+Mathf.Max(0,Mathf.CeilToInt((float)(state.Switch-Now)))+"초");
            }
            else GUILayout.Label("경기 종료 · "+Result.Value);
            GUILayout.EndArea();
            ArenaObjectiveGuide.Draw(this);
            if(state.Phase!=Stage.Waiting)
            {
                GUILayout.BeginArea(new Rect(Screen.width-230,185,215,Mathf.Min(230,50+Scores.Count*24)),GUI.skin.box);GUILayout.Label("점유 점수");
                var sorted=new Score[Scores.Count];for(int i=0;i<Scores.Count;i++)sorted[i]=Scores[i];Array.Sort(sorted,(a,b)=>a.Number.CompareTo(b.Number));
                foreach(var score in sorted)GUILayout.Label("P"+score.Number+"   "+score.Seconds.ToString("0.0")+"점");GUILayout.EndArea();
            }
            if(state.Phase==Stage.Finished&&(RelayArenaPrototype.Instance==null||!RelayArenaPrototype.Instance.MenuOpen))
            {
                GUILayout.BeginArea(new Rect(Screen.width*.5f-190,Screen.height*.5f-95,380,190),GUI.skin.box);GUILayout.Label("경기 종료");GUILayout.Label(Result.Value.ToString());
                if(IsServer){if(GUILayout.Button(RoomFlow?"대기방으로 돌아가기":"다시 시작")){if(RoomFlow)ReturnToRoom();else StartRound();}}else GUILayout.Label(RoomFlow?"방장이 대기방으로 돌아가기를 기다립니다.":"호스트가 다음 경기를 시작할 수 있습니다.");
                GUILayout.Label("ESC: 메뉴 / 연결 종료");GUILayout.EndArea();
            }
        }
    }
}
