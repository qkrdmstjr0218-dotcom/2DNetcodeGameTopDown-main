using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        [SerializeField,Min(.1f)] float groundChaseSpeed=2.8f,groundRushSpeed=6,groundRushRange=2.8f,
            groundRushWarning=.55f,groundRushSeconds=.55f,groundRushRecovery=.85f;
        void EndGroundRush(ref PatrolState state)
        {state.RushPhase=3;state.RushUntil=Now+groundRushRecovery;state.Speed=0;state.Velocity=Vector2.zero;}
        bool TickGroundRush(ref PatrolState state,EnemyBrain brain,ArenaEnemyNavigation graph)
        {
            if(state.RushPhase==1&&Now>=state.RushUntil)
            {
                state.RushPhase=2;state.RushUntil=Now+groundRushSeconds;
                brain.Path=graph.Route(state.Position,state.Position+state.Aim*(groundRushSpeed*groundRushSeconds),false);
                brain.Step=brain.Path.Count>1?1:0;brain.Last=brain.Path.Count>0?brain.Path[0]:-1;
            }
            if(state.RushPhase==2&&Now>=state.RushUntil)EndGroundRush(ref state);
            if(state.RushPhase==3&&Now>=state.RushUntil)
            {state.RushPhase=0;brain.Path.Clear();brain.Step=0;brain.Think=Now;}
            if(state.RushPhase!=1&&state.RushPhase!=3)return false;
            state.Speed=0;state.Velocity=Vector2.zero;return true;
        }
        bool TryStartGroundRush(ref PatrolState state,EnemyBrain brain)
        {
            var target=brain.Target;if(target==null||!target.IsSpawned||target.IsDead||!target.HasCurrentLifePose)return false;
            var bounds=target.ArenaHitBounds;float dx=bounds.center.x-state.Position.x;
            if(Mathf.Abs(dx)>groundRushRange||Mathf.Abs(bounds.min.y-state.Position.y)>.8f||
                ArenaGrenadeBallistics.WorldTrace(state.Position+Vector2.up*.48f,bounds.center,0,out _))return false;
            state.Aim=Vector2.right*(dx<0?-1:1);state.RushPhase=1;state.RushUntil=Now+groundRushWarning;
            state.Speed=0;state.Velocity=Vector2.zero;return true;
        }
    }
}
