using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkPlayer
    {
        public int GrantSpecialAmmoServer(ArenaObjectiveMatch.SupplyAmmo kind,int amount,uint generation)
        {
            var m=ArenaObjectiveMatch.Instance;
            if(!IsServer||!IsSpawned||IsDead||!HasCurrentLifePose||generation!=LifeVersion||amount<=0||m==null||!m.RoomFlow||m.State.Value.Phase!=ArenaObjectiveMatch.Stage.Playing)return 0;
            if(kind==ArenaObjectiveMatch.SupplyAmmo.Shotgun)return GrantShotgunAmmoServer(amount);
            if(kind==ArenaObjectiveMatch.SupplyAmmo.Rifle){int n=Mathf.Min(amount,RifleCapacity-RifleRounds);rifleRounds.Value+=n;return n;}
            if(kind==ArenaObjectiveMatch.SupplyAmmo.Grenade){int n=Mathf.Min(amount,GrenadeCapacity-GrenadeRounds);grenadeRounds.Value+=n;return n;}
            return 0;
        }
    }
}
