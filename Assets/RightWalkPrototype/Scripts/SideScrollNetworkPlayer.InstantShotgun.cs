using Unity.Netcode;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkPlayer
    {
        readonly NetworkVariable<int> instantShotgunRounds=new(ShotgunCapacity);
        double nextInstantPracticeSupply;
        public bool InstantShotgunEnabled=>NormalAmmoEnabled;
        public int InstantShotgunRounds=>instantShotgunRounds.Value;
        public bool InstantShotgunActive=>InstantShotgunEnabled&&SpecialHeld&&SelectedAmmo==AmmoKind.InstantShotgun&&!IsDead;
        public bool SpecialShotActive=>GrenadeActive||ShotgunActive||InstantShotgunActive||RifleActive;
        public string ActiveAmmoName=>GrenadeActive?"유탄":RifleActive?"관통 라이플":InstantShotgunActive?"샷건":ShotgunActive?"샷건":"일반탄";
        public string SelectedAmmoName=>SelectedAmmo==AmmoKind.Grenade?"유탄":SelectedAmmo==AmmoKind.Rifle?"관통 라이플":SelectedAmmo==AmmoKind.InstantShotgun?"샷건":SelectedAmmo==AmmoKind.Shotgun?"샷건":"일반탄";
        public int ReceivedInstantBlasts {get;private set;}
        void UpdateInstantPracticeAmmo()
        {
            if(!IsServer||!IsSpawned||!InstantShotgunEnabled)return;
            double now=ArenaClock.Now(NetworkManager);
            if(!ArenaObjectiveMatch.PracticeRoom||IsDead||InstantShotgunRounds>=ShotgunCapacity){nextInstantPracticeSupply=now+PracticeSupplyInterval;return;}
            if(now<nextInstantPracticeSupply)return;
            instantShotgunRounds.Value++;nextInstantPracticeSupply=now+PracticeSupplyInterval;
        }
        [ServerRpc]
        void FireInstantShotgunServerRpc(Vector2 origin,Vector2 direction,uint generation)=>FireSpreadServer(origin,direction,true,generation);
        [ClientRpc]
        void ShowInstantShotgunClientRpc(PelletPath[] paths)
        {
            ReceivedShots++;ReceivedInstantBlasts++;ReceivedShotgunBlasts++;
            for(int i=0;i<paths.Length;i++)shots.ShowInstantPellet(paths[i].Origin,paths[i].Direction,paths[i].Range,paths[i].Hit,i==0);
        }
    }
}
