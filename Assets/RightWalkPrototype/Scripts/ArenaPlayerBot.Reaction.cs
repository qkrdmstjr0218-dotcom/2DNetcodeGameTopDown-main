using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaPlayerBot
    {
        SideScrollNetworkPlayer attacker;
        uint attackerLife;
        float provokedUntil,reactAt,nextDodge,detourUntil;
        Vector3 attackerPosition;
        int objective=-1;
        bool pursuingObjective;
        public int DodgeRequests{get;private set;}
        public bool PursuingObjective=>pursuingObjective;
        public void RememberAttacker(SideScrollNetworkPlayer source)
        {
            if(!player.IsServer||source==null||!source.IsSpawned||source.IsDead||source==player||Vector2.Distance(transform.position,source.transform.position)>18)return;
            bool sameAttacker=attacker==source&&attackerLife==source.LifeVersion&&Time.time<provokedUntil;
            attacker=source;attackerLife=source.LifeVersion;attackerPosition=source.ArenaHitBounds.center;
            provokedUntil=Time.time+3;if(!sameAttacker)reactAt=Time.time+ReactionDelay;thinkAt=reactAt;
        }
        bool Retaliate()
        {
            if(attacker==null||!attacker.IsSpawned||attacker.IsDead||attacker.LifeVersion!=attackerLife||Time.time>=provokedUntil||Vector2.Distance(transform.position,attackerPosition)>18)return false;
            if(Time.time<reactAt)return false;
            target=attacker;targetLife=attackerLife;enemy=null;visibleTarget=ClearSight(attacker.ArenaHitBounds);
            targetBounds=visibleTarget?attacker.ArenaHitBounds:new Bounds(attackerPosition,attacker.ArenaHitBounds.size);
            rememberedUntil=provokedUntil;return true;
        }
        void PrioritizeObjective(ArenaObjectiveMatch match,Bounds region)
        {
            if(objective!=match.State.Value.Point){objective=match.State.Value.Point;detourUntil=Time.time+3;}
            bool contested=match.Occupants.Value>0&&match.OccupantNumber.Value!=player.PlayerNumber;
            bool closeDanger=HasTarget&&Vector2.Distance(player.ArenaHitBounds.center,targetBounds.center)<4;
            pursuingObjective=!region.Contains(player.ArenaHitBounds.center)&&(contested||Time.time>=detourUntil)&&!closeDanger;
            if(pursuingObjective){goal=new Vector2(region.center.x,region.min.y);Activity="점령 구역 우선 이동";}
            if(match.Points[match.State.Value.Point].Contains(player))detourUntil=Time.time+3;
        }
        bool CombatDodge(float move)
        {
            if(Time.time<nextDodge||!motor.Grounded||motor.HitReacting||!HasTarget||!visibleTarget)return false;
            float distance=Vector2.Distance(player.ArenaHitBounds.center,targetBounds.center);
            if(distance<1.4f||distance>9)return false;
            float direction=move!=0?Mathf.Sign(move):Mathf.Sign(transform.position.x-targetBounds.center.x);
            var probe=Foot+Vector2.right*direction*1.2f+Vector2.up*.35f;
            if(!ArenaEnemyNavigation.FloorBelow(probe,1.5f,out var floor)||!RelayArenaPrototype.Instance.IsSafeLanding(floor.point))return false;
            nextDodge=Time.time+DodgeInterval+(player.PlayerNumber%3)*.25f;DodgeRequests++;return true;
        }
    }
}
