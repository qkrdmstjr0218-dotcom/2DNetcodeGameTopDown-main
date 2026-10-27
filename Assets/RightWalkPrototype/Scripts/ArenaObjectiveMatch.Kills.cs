using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        public const int MedicalDroneKillPoints=5;
        void RecordMedicalDroneKill(SideScrollNetworkPlayer attacker)
        {
            if(!IsServer||!IsSpawned||State.Value.Phase!=Stage.Playing||Now>=State.Value.End||attacker==null||
                !attacker.IsSpawned||attacker.NetworkManager!=NetworkManager||
                !NetworkManager.ConnectedClients.ContainsKey(attacker.OwnerClientId))return;
            for(int i=0;i<Scores.Count;i++)if(Scores[i].Client==attacker.OwnerClientId)
            {var score=Scores[i];score.DronePoints+=MedicalDroneKillPoints;Scores[i]=score;return;}
            Scores.Add(new Score{Client=attacker.OwnerClientId,Number=attacker.PlayerNumber,Nickname=attacker.Nickname,DronePoints=MedicalDroneKillPoints});
        }
        [SerializeField,Min(0)] float playerKillPoints=5;
        readonly Unity.Netcode.NetworkVariable<float> killReward=new(5);
        public float PlayerKillReward=>killReward.Value;
        void UpdateKillRules(){if(IsServer&&killReward.Value!=Mathf.Max(0,playerKillPoints))killReward.Value=Mathf.Max(0,playerKillPoints);}
        internal void RecordPlayerKill(SideScrollNetworkPlayer attacker,SideScrollNetworkPlayer victim)
        {
            if(!IsServer||!IsSpawned||State.Value.Phase!=Stage.Playing||Now>=State.Value.End||attacker==null||victim==null||
                attacker==victim||!attacker.IsSpawned||!victim.IsSpawned||!victim.IsDead||attacker.NetworkManager!=NetworkManager||
                !NetworkManager.ConnectedClients.ContainsKey(attacker.OwnerClientId))return;
            UpdateKillRules();
            // Called only at a server-approved live -> dead transition. No score
            // is removed from the victim, including deaths inside an objective.
            for(int i=0;i<Scores.Count;i++)if(Scores[i].Client==attacker.OwnerClientId)
            {var score=Scores[i];score.Kills++;score.KillPoints+=PlayerKillReward;Scores[i]=score;return;}
            Scores.Add(new Score{Client=attacker.OwnerClientId,Number=attacker.PlayerNumber,Nickname=attacker.Nickname,Kills=1,KillPoints=PlayerKillReward});
        }
    }
}
