using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        struct KillNotice { public string Attacker,Victim;public int Number,VictimNumber;public bool Support;public float Until; }
        readonly System.Collections.Generic.List<KillNotice> killNotices=new();
        [Unity.Netcode.ClientRpc]
        void KillNoticeClientRpc(int version,Unity.Collections.FixedString64Bytes attacker,Unity.Collections.FixedString64Bytes victim,int number,int victimNumber,bool support)
        {
            if(State.Value.Version!=version||State.Value.Phase!=Stage.Playing)return;
            if(killNoticeVersion!=version){killNoticeVersion=version;killNotices.Clear();}
            killNotices.RemoveAll(n=>n.Until<=Time.unscaledTime);
            killNotices.Insert(0,new KillNotice{Attacker=attacker.ToString(),Victim=victim.ToString(),Number=number,VictimNumber=victimNumber,Support=support,Until=Time.unscaledTime+6});
            if(killNotices.Count>4)killNotices.RemoveRange(4,killNotices.Count-4);
        }
        public int KillNoticeCount=>killNotices.Count;
        int killNoticeVersion=-1;
        public void DrawKillNotices(float width)
        {
            if(killNoticeVersion!=State.Value.Version){killNoticeVersion=State.Value.Version;killNotices.Clear();}
            killNotices.RemoveAll(n=>n.Until<=Time.unscaledTime);
            float y=66+Scores.Count*25;
            for(int i=0;i<killNotices.Count;i++)
            {
                var n=killNotices[i];
                var measure=new GUIStyle(GUI.skin.label){fontSize=12,wordWrap=false,richText=false};ArenaUiTheme.Static(measure);
                float aw=Mathf.Min(126,measure.CalcSize(new GUIContent(n.Attacker)).x+4),vw=Mathf.Min(130,measure.CalcSize(new GUIContent(n.Victim)).x+4);
                float victimX=38+aw+46;
                float rowWidth=victimX+(n.Support?0:28)+vw+10;
                var r=new Rect(width-16-rowWidth,y+i*31,rowWidth,27);var old=GUI.color;
                GUI.color=new Color(1,1,1,Mathf.Clamp01(n.Until-Time.unscaledTime));
                ArenaHudSkin.Panel(r,ArenaPlayerAppearance.Accent(n.Number));
                var badge=new GUIStyle(GUI.skin.label){fontSize=12,wordWrap=false,richText=false};
                badge.normal.textColor=ArenaPlayerAppearance.Accent(n.Number);ArenaUiTheme.Static(badge);
                GUI.Label(new Rect(r.x+8,r.y+3,27,21),"P"+n.Number,badge);
                ArenaPlayerNames.CompactLabel(new Rect(r.x+38,r.y+3,aw,21),n.Attacker,12);
                var label=new GUIStyle(GUI.skin.label){fontSize=12,alignment=TextAnchor.MiddleCenter,wordWrap=false};ArenaUiTheme.Static(label);
                GUI.Label(new Rect(r.x+38+aw+4,r.y+3,34,21),n.Support?"격추":"처치",label);
                if(!n.Support)
                {
                    badge.normal.textColor=ArenaPlayerAppearance.Accent(n.VictimNumber);ArenaUiTheme.Static(badge);
                    GUI.Label(new Rect(r.x+victimX,r.y+3,27,21),"P"+n.VictimNumber,badge);
                }
                ArenaPlayerNames.CompactLabel(new Rect(r.x+victimX+(n.Support?0:28),r.y+3,vw,21),n.Victim,12);GUI.color=old;
            }
        }
        public const int MedicalDroneKillPoints=5;
        void RecordMedicalDroneKill(SideScrollNetworkPlayer attacker)
        {
            if(!IsServer||!IsSpawned||State.Value.Phase!=Stage.Playing||Now>=State.Value.End||attacker==null||
                !attacker.IsSpawned||attacker.NetworkManager!=NetworkManager||
                !attacker.IsBot&&!NetworkManager.ConnectedClients.ContainsKey(attacker.ParticipantId))return;
            KillNoticeClientRpc(State.Value.Version,attacker.Nickname,new Unity.Collections.FixedString64Bytes("지원 드론"),attacker.PlayerNumber,0,true);
            for(int i=0;i<Scores.Count;i++)if(Scores[i].Client==attacker.ParticipantId)
            {var score=Scores[i];score.DronePoints+=MedicalDroneKillPoints;Scores[i]=score;return;}
            Scores.Add(new Score{Client=attacker.ParticipantId,Number=attacker.PlayerNumber,Nickname=attacker.Nickname,DronePoints=MedicalDroneKillPoints});
        }
        [SerializeField,Min(0)] float playerKillPoints=5;
        readonly Unity.Netcode.NetworkVariable<float> killReward=new(5);
        public float PlayerKillReward=>killReward.Value;
        void UpdateKillRules(){if(IsServer&&killReward.Value!=Mathf.Max(0,playerKillPoints))killReward.Value=Mathf.Max(0,playerKillPoints);}
        internal void RecordPlayerKill(SideScrollNetworkPlayer attacker,SideScrollNetworkPlayer victim)
        {
            if(!IsServer||!IsSpawned||State.Value.Phase!=Stage.Playing||Now>=State.Value.End||attacker==null||victim==null||
                attacker==victim||!attacker.IsSpawned||!victim.IsSpawned||!victim.IsDead||attacker.NetworkManager!=NetworkManager||
                !attacker.IsBot&&!NetworkManager.ConnectedClients.ContainsKey(attacker.ParticipantId))return;
            UpdateKillRules();
            // Called only at a server-approved live -> dead transition. No score
            // is removed from the victim, including deaths inside an objective.
            KillNoticeClientRpc(State.Value.Version,attacker.Nickname,victim.Nickname,attacker.PlayerNumber,victim.PlayerNumber,false);
            for(int i=0;i<Scores.Count;i++)if(Scores[i].Client==attacker.ParticipantId)
            {var score=Scores[i];score.Kills++;score.KillPoints+=PlayerKillReward;Scores[i]=score;return;}
            Scores.Add(new Score{Client=attacker.ParticipantId,Number=attacker.PlayerNumber,Nickname=attacker.Nickname,Kills=1,KillPoints=PlayerKillReward});
        }
    }
}
