using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaPlayerBot
    {
        ArenaPatrolEnemy enemy;
        Bounds targetBounds;
        float rememberedUntil;
        uint targetLife;
        int enemyGeneration;
        bool seekingPickup,visibleTarget;
        Vector2 grenadeVelocity;
        public SideScrollNetworkPlayer.AmmoKind PlannedAmmo{get;private set;}
        public int PickupId{get;private set;}
        public Vector2 Goal=>goal;
        public string TargetCategory=>target!=null?"플레이어":enemy!=null?"몹":"없음";
        bool HasTarget=>target!=null&&!target.IsDead&&target.IsSpawned&&target.LifeVersion==targetLife||enemy!=null&&enemy.Generation==enemyGeneration&&enemy.isActiveAndEnabled&&enemy.TryGetComponent<Collider2D>(out var c)&&c.enabled;
        void ClearDecision(){target=null;enemy=null;PickupId=0;seekingPickup=false;rememberedUntil=0;player.SetSpecialHeld(false);}
        bool ClearSight(Bounds bounds)=>!ArenaGrenadeBallistics.WorldTrace(player.ArenaHitBounds.center,bounds.center,0,out _);
        bool RefreshTarget()
        {
            if(!HasTarget)return false;
            var current=target!=null?target.ArenaHitBounds:enemy.GetComponent<Collider2D>().bounds;
            if(Vector2.Distance(player.ArenaHitBounds.center,current.center)>15)return false;
            visibleTarget=ClearSight(current);
            if(visibleTarget){targetBounds=current;rememberedUntil=Time.time+1.5f;}
            // A rifle may use a briefly remembered position, never a live wall-tracking position.
            return visibleTarget||Time.time<rememberedUntil;
        }
        void ChooseGoal(ArenaObjectiveMatch match,Vector2 foot)
        {
            var previousPlayer=target;var previousEnemy=enemy;
            bool retained=RefreshTarget();
            if(!retained){target=null;enemy=null;}
            bool retaliating=Retaliate();
            float best=retained?Vector2.Distance(player.ArenaHitBounds.center,targetBounds.center)-1:12;
            if(!retaliating)foreach(var p in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))
            {
                if(p==player||!p.IsSpawned||p.IsDead||p.SpawnProtected||!p.HasCurrentLifePose)continue;
                float value=Vector2.Distance(player.ArenaHitBounds.center,p.ArenaHitBounds.center);
                if(value>=best||!ClearSight(p.ArenaHitBounds))continue;
                target=p;targetLife=p.LifeVersion;enemy=null;targetBounds=p.ArenaHitBounds;best=value;visibleTarget=true;rememberedUntil=Time.time+1.5f;
            }
            if(!retaliating)foreach(var e in FindObjectsByType<ArenaPatrolEnemy>(FindObjectsSortMode.None))
            {
                if(e.Match!=match||!e.isActiveAndEnabled||!e.TryGetComponent<Collider2D>(out var shape)||!shape.enabled)continue;
                // Enemy damage colliders are disabled during entry/death/off-map flight.
                float value=Vector2.Distance(player.ArenaHitBounds.center,shape.bounds.center)+1.25f;
                if(value>=best||!ClearSight(shape.bounds))continue;
                enemy=e;enemyGeneration=e.Generation;target=null;targetBounds=shape.bounds;best=value;visibleTarget=true;rememberedUntil=Time.time+1.5f;
            }
            var region=match.Points[match.State.Value.Point].CaptureBounds;
            goal=new Vector2(region.center.x,region.min.y);Activity="해킹 구역 이동";
            if(HasTarget){goal=new Vector2(targetBounds.center.x,targetBounds.min.y);Activity=target!=null?"플레이어 교전":"몹 공격";}
            PickupId=0;seekingPickup=false;
            PrioritizeObjective(match,region);
            ChoosePickup(match,foot);
            PlannedAmmo=ChooseWeapon();
            if(target!=previousPlayer||enemy!=previousEnemy)shotAt=Mathf.Max(shotAt,Time.time+ReactionDelay);
        }
        SideScrollNetworkPlayer.AmmoKind ChooseWeapon()
        {
            var normal=SideScrollNetworkPlayer.AmmoKind.Normal;
            if(!HasTarget)return normal;
            float distance=Vector2.Distance(player.ArenaHitBounds.center,targetBounds.center);
            if(!visibleTarget)return player.RifleRounds>0?SideScrollNetworkPlayer.AmmoKind.Rifle:normal;
            if(distance<4.2f&&player.ShotgunRounds>0)return SideScrollNetworkPlayer.AmmoKind.InstantShotgun;
            if(distance>4.5f&&distance<11&&player.GrenadeRounds>0&&player.PlanBotGrenade(targetBounds,out grenadeVelocity))return SideScrollNetworkPlayer.AmmoKind.Grenade;
            if(player.RifleRounds>0&&(distance>8||enemy!=null&&distance>5||player.NormalRounds<2))return SideScrollNetworkPlayer.AmmoKind.Rifle;
            return normal;
        }
        void ChoosePickup(ArenaObjectiveMatch match,Vector2 foot)
        {
            if(navigation==null)return;
            float best=0;Vector2 chosen=default;int id=0;
            bool emergency=player.Health<=35;
            bool occupied=match.Points[match.State.Value.Point].Contains(player);
            void Consider(ArenaObjectiveMatch.AmmoPickup item,float need,bool healing)
            {
                if(need<=0||!item.Available||item.Falling||item.ExpiresAt>0&&item.ExpiresAt<=match.Now+2)return;
                float direct=Vector2.Distance(foot,item.Position);
                if(pursuingObjective&&!(emergency&&healing&&direct<5))return;
                if(direct>12||HasTarget&&!(emergency&&healing&&direct<6)||occupied&&!emergency&&direct>4)return;
                // Only divert to a reachable landing, and account for the ramp detour.
                var landing=item.Position-Vector2.up*.4f;
                var path=navigation.Route(foot,landing,false);if(path.Count==0)return;
                var end=navigation.Nodes[path[path.Count-1]].Point;if(Vector2.Distance(end,landing)>1.2f)return;
                float length=0;Vector2 previous=foot;
                foreach(int n in path){length+=Vector2.Distance(previous,navigation.Nodes[n].Point);previous=navigation.Nodes[n].Point;}
                if(length>18||item.ExpiresAt>0&&match.Now+length/2.8f+1>=item.ExpiresAt)return;
                float score=need/(2+length);if(score<=best)return;
                best=score;chosen=landing;id=healing?-item.Id:item.Id;
            }
            foreach(var item in match.HealthPickups)
            {
                float need=item.Shield?(player.Shield<25?30:0):player.Health<75?(emergency?160:75-player.Health):0;
                Consider(item,need,!item.Shield);
            }
            foreach(var item in match.AmmoPickups)
            {
                int capacity=item.Ammo==ArenaObjectiveMatch.SupplyAmmo.Rifle?SideScrollNetworkPlayer.RifleCapacity:item.Ammo==ArenaObjectiveMatch.SupplyAmmo.Grenade?SideScrollNetworkPlayer.GrenadeCapacity:SideScrollNetworkPlayer.ShotgunCapacity;
                int rounds=item.Ammo==ArenaObjectiveMatch.SupplyAmmo.Rifle?player.RifleRounds:item.Ammo==ArenaObjectiveMatch.SupplyAmmo.Grenade?player.GrenadeRounds:player.ShotgunRounds;
                Consider(item,rounds<capacity?20f*(capacity-rounds)/capacity:0,false);
            }
            if(id!=0){goal=chosen;PickupId=id;seekingPickup=true;Activity=emergency?"회복 아이템 확보":"아이템 파밍";}
        }
        void TickCombat()
        {
            if(!RefreshTarget()){target=null;enemy=null;player.SetSpecialHeld(false);return;}
            if(Time.time<shotAt)return;shotAt=Time.time+FireAttemptInterval;
            if(!visibleTarget&&PlannedAmmo!=SideScrollNetworkPlayer.AmmoKind.Rifle)return;
            if(PlannedAmmo==SideScrollNetworkPlayer.AmmoKind.Grenade&&!player.PlanBotGrenade(targetBounds,out grenadeVelocity))
                PlannedAmmo=player.NormalRounds>0?SideScrollNetworkPlayer.AmmoKind.Normal:player.RifleRounds>0?SideScrollNetworkPlayer.AmmoKind.Rifle:SideScrollNetworkPlayer.AmmoKind.Normal;
            if(PlannedAmmo==SideScrollNetworkPlayer.AmmoKind.InstantShotgun&&player.ShotgunRounds==0||PlannedAmmo==SideScrollNetworkPlayer.AmmoKind.Rifle&&player.RifleRounds==0||PlannedAmmo==SideScrollNetworkPlayer.AmmoKind.Grenade&&player.GrenadeRounds==0)PlannedAmmo=SideScrollNetworkPlayer.AmmoKind.Normal;
            var point=(Vector2)targetBounds.center+Vector2.up*(Mathf.Sin(Time.time*2+player.PlayerNumber)*AimError);
            player.BotFire(point,PlannedAmmo,grenadeVelocity);
        }
    }
}
