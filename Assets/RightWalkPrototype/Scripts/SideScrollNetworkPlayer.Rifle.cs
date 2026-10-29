using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkPlayer
    {
        public const int RifleCapacity=3,RifleDamage=30,RifleTargetDamage=4;
        public const float RifleRange=30,RifleSpeed=48,RifleInterval=1.2f;
        readonly NetworkVariable<int> rifleRounds=new(RifleCapacity);
        double nextRiflePracticeSupply;
        public bool RifleEnabled=>NormalAmmoEnabled;
        public int RifleRounds=>rifleRounds.Value;
        public bool RifleActive=>RifleEnabled&&SpecialHeld&&SelectedAmmo==AmmoKind.Rifle&&!IsDead;
        public float ActiveShotInterval=>GrenadeActive?GrenadeInterval:RifleActive?RifleInterval:ShotgunActive||InstantShotgunActive?ShotgunInterval:NormalShotInterval;
        public int ReceivedRifleShots {get;private set;}
        void UpdateRiflePracticeAmmo()
        {
            if(!IsServer||!IsSpawned||!RifleEnabled)return;
            double now=ArenaClock.Now(NetworkManager);
            if(!ArenaObjectiveMatch.PracticeRoom||IsDead||RifleRounds>=RifleCapacity){nextRiflePracticeSupply=now+PracticeSupplyInterval;return;}
            if(now<nextRiflePracticeSupply)return;
            rifleRounds.Value++;nextRiflePracticeSupply=now+PracticeSupplyInterval;
        }
        [ServerRpc]
        void FireRifleServerRpc(Vector2 origin,Vector2 direction,uint generation)
        {
            if(!IsServer||!IsSpawned||!RifleEnabled||IsDead||generation!=LifeVersion||ArenaObjectiveMatch.CombatBlocked||RifleRounds<=0)return;
            if(!float.IsFinite(origin.x)||!float.IsFinite(origin.y)||!float.IsFinite(direction.x)||!float.IsFinite(direction.y)||direction.sqrMagnitude<.9f||direction.sqrMagnitude>1.1f)return;
            Vector3 position=IsOwner?transform.position:motion.Value.LifeVersion==LifeVersion?motion.Value.Position:life.Value.SpawnPosition;
            if(Vector2.Distance(origin,position)>3)return;
            double now=ArenaClock.Now(NetworkManager);if(!ServerFireReady(AmmoKind.Rifle,now))return;
            MarkServerFired(AmmoKind.Rifle,now);rifleRounds.Value--;nextRiflePracticeSupply=now+PracticeSupplyInterval;
            direction.Normalize();float barrel=Mathf.Max(0,Vector2.Dot(origin-(Vector2)ServerHitBounds().center,direction));
            int guide=GuideHit(origin,direction,RifleRange,out float guideDistance,true);if(guide>=0)StartCoroutine(ApplyGuideImpact(guide,guideDistance/RifleSpeed));
            var match=ArenaObjectiveMatch.Instance;ShowRifleClientRpc(origin,direction,now);
            StartCoroutine(TravelRifle(origin-direction*barrel,direction,barrel,now,match,match.State.Value.Version));
        }
        static bool RifleIntersects(Bounds bounds,Vector2 start,Vector2 direction,float length)
        {
            if(bounds.Contains(new Vector3(start.x,start.y,bounds.center.z)))return true;
            return bounds.IntersectRay(new Ray(start,direction),out float distance)&&distance>=0&&distance<=length;
        }
        IEnumerator TravelRifle(Vector2 start,Vector2 direction,float barrel,double began,ArenaObjectiveMatch match,int version)
        {
            var hitPlayers=new HashSet<ulong>();var hitPatrols=new HashSet<int>();var hitTargets=new HashSet<ulong>();float travelled=0;
            while(IsServer&&IsSpawned&&match!=null&&match.IsSpawned&&match.State.Value.Version==version&&!ArenaObjectiveMatch.CombatBlocked)
            {
                float next=barrel+Mathf.Min(RifleRange,(float)(ArenaClock.Now(NetworkManager)-began)*RifleSpeed);
                Vector2 from=start+direction*travelled;float step=Mathf.Max(0,next-travelled);
                // Sweep only the distance travelled this frame. Walls never stop this bullet.
                foreach(var target in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))
                {
                    if(target==this||!target.IsSpawned||target.IsDead||hitPlayers.Contains(target.NetworkObjectId)||!RifleIntersects(target.ServerHitBounds(),from,direction,step))continue;
                    hitPlayers.Add(target.NetworkObjectId);target.ReceiveServerDamage(RifleDamage,target.LifeVersion,this);ShowRifleImpactClientRpc((Vector2)target.ServerHitBounds().center,direction);
                }
                foreach(var enemy in FindObjectsByType<ArenaPatrolEnemy>(FindObjectsSortMode.None))
                {
                    if(enemy.Match!=match||hitPatrols.Contains(enemy.Id)||!enemy.TryGetComponent<Collider2D>(out var shape)||!shape.enabled||!RifleIntersects(shape.bounds,from,direction,step))continue;
                    hitPatrols.Add(enemy.Id);match.DamagePatrolServer(enemy.Id,enemy.Generation,RifleTargetDamage,this);ShowRifleImpactClientRpc(shape.bounds.center,direction);
                }
                foreach(var target in FindObjectsByType<SideScrollDamageTarget>(FindObjectsSortMode.None))
                {
                    if(!target.IsSpawned||target.Health.Value<=0||hitTargets.Contains(target.NetworkObjectId)||!target.TryGetComponent<Collider2D>(out var shape)||!RifleIntersects(shape.bounds,from,direction,step))continue;
                    hitTargets.Add(target.NetworkObjectId);target.ReceiveDamage(RifleTargetDamage,target.Generation,this,AmmoKind.Rifle);ShowRifleImpactClientRpc(shape.bounds.center,direction);
                }
                travelled=next;if(travelled>=barrel+RifleRange)yield break;yield return null;
            }
        }
        [ClientRpc]
        void ShowRifleClientRpc(Vector2 origin,Vector2 direction,double began)
        {ReceivedShots++;ReceivedRifleShots++;shots.ShowRifle(origin,direction,(float)(ArenaClock.Now(NetworkManager)-began));}
        [ClientRpc]
        void ShowRifleImpactClientRpc(Vector2 point,Vector2 direction)=>shots.ShowRifleImpact(point,direction);
    }
}
