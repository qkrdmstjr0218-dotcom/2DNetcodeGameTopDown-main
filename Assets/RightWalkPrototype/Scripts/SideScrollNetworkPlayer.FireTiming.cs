using Unity.Netcode;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkPlayer
    {
        public const float WeaponSwitchSeconds=.18f;
        readonly double[] serverFireReady=new double[5];
        readonly float[] localFireReady=new float[5];
        double serverSwitchReady;
        float localSwitchReady;
        AmmoKind serverFireKind,localFireKind;
        uint localTimingLife;
        public AmmoKind ActiveFireKind=>GrenadeActive?AmmoKind.Grenade:RifleActive?AmmoKind.Rifle:InstantShotgunActive?AmmoKind.InstantShotgun:ShotgunActive?AmmoKind.Shotgun:AmmoKind.Normal;
        public bool ActiveAmmoEmpty=>GrenadeActive?GrenadeRounds<=0:RifleActive?RifleRounds<=0:InstantShotgunActive?InstantShotgunRounds<=0:ShotgunActive?ShotgunRounds<=0:NormalAmmoEnabled&&NormalRounds<1;
        float IntervalFor(AmmoKind kind)=>kind==AmmoKind.Grenade?GrenadeInterval:kind==AmmoKind.Rifle?RifleInterval:kind==AmmoKind.Normal?NormalShotInterval:ShotgunInterval;
        void ResetFireTiming()
        {
            System.Array.Clear(serverFireReady,0,serverFireReady.Length);serverSwitchReady=0;serverFireKind=AmmoKind.Normal;
            if(IsOwner)ResetLocalFireTiming();
        }
        void ResetLocalFireTiming()
        {System.Array.Clear(localFireReady,0,localFireReady.Length);localSwitchReady=0;localFireKind=AmmoKind.Normal;localTimingLife=LifeVersion;}
        void NotifyFireMode()
        {
            if(!IsOwner||!IsSpawned||!NormalAmmoEnabled)return;
            if(localTimingLife!=LifeVersion)ResetLocalFireTiming();
            var kind=ActiveFireKind;
            // Q/E preselects the special round while normal fire remains active.
            // Only an effective firing-mode change starts the switch delay.
            if(kind==localFireKind)return;
            localFireKind=kind;localSwitchReady=Time.time+WeaponSwitchSeconds;
            ChangeFireModeServerRpc(kind,LifeVersion);
        }
        [ServerRpc]
        void ChangeFireModeServerRpc(AmmoKind kind,uint generation)
        {
            if(!NormalAmmoEnabled||IsDead||generation!=LifeVersion||ArenaObjectiveMatch.CombatBlocked||(int)kind>(int)AmmoKind.Grenade)return;
            if(kind!=serverFireKind)serverSwitchReady=NetworkManager.ServerTime.Time+WeaponSwitchSeconds;
            serverFireKind=kind;
        }
        bool ServerFireReady(AmmoKind kind,double now)
        {
            if(!NormalAmmoEnabled)return now+.01>=nextServerShot;
            if(serverFireKind!=kind){serverFireKind=kind;serverSwitchReady=now+WeaponSwitchSeconds;return false;}
            return now+.01>=serverSwitchReady&&now+.01>=serverFireReady[(int)kind];
        }
        void MarkServerFired(AmmoKind kind,double now)
        {if(NormalAmmoEnabled)serverFireReady[(int)kind]=now+IntervalFor(kind);else nextServerShot=now+IntervalFor(kind);}
        public bool TryLocalFire()
        {
            NotifyFireMode();
            var kind=ActiveFireKind;
            if(Time.time<Mathf.Max(localSwitchReady,localFireReady[(int)kind])||ActiveAmmoEmpty)return false;
            localFireReady[(int)kind]=Time.time+IntervalFor(kind);return true;
        }
        public float FireWaitRemaining
        {
            get{if(localTimingLife!=LifeVersion)return 0;return Mathf.Max(0,Mathf.Max(localSwitchReady,localFireReady[(int)ActiveFireKind])-Time.time);}
        }
        public float FireWaitProgress=>1-Mathf.Clamp01(FireWaitRemaining/Mathf.Max(WeaponSwitchSeconds,localFireReady[(int)ActiveFireKind]>localSwitchReady?IntervalFor(ActiveFireKind):WeaponSwitchSeconds));
        public bool FireCursorBlocked=>FireWaitRemaining>0||ActiveAmmoEmpty;
    }
}
