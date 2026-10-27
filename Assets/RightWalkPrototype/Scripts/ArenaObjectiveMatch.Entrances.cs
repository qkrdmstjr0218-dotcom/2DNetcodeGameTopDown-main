using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        public bool EnemyEntering(PatrolState state)=>!state.Medical&&state.EntryUntil>Now;
        public static float EntryProgress(PatrolState state,double now)=>state.EntrySeconds<=0?1:(float)(1-(state.EntryUntil-now)/state.EntrySeconds);
        Vector2 GroundEntryOrigin(PatrolState state)=>new Vector2(state.Left,state.Floor+.02f);
        Vector2 GroundEntryExit(PatrolState state)
        {
            var origin=GroundEntryOrigin(state);float direction=state.Left<0?1:-1;
            for(int i=0;i<2;i++)
            {
                var exit=origin+Vector2.right*(direction*.9f);
                if(groundNavigation!=null&&groundNavigation.CanTraverse(new ArenaEnemyNavigation.Node(origin,Vector2.up),new ArenaEnemyNavigation.Node(exit,Vector2.up)))return exit;
                direction=-direction;
            }
            return origin;
        }
        void AdvanceEnemyEntry(ref PatrolState state,bool complete)
        {
            if(state.Medical||state.Turret||state.Flying||!state.Roaming)return;
            float move=complete?1:Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,EntryProgress(state,Now)));
            state.Position=Vector2.Lerp(GroundEntryOrigin(state),GroundEntryExit(state),move);state.Velocity=Vector2.zero;
        }
        void BeginEnemyEntry(ref PatrolState state,ArenaMapSettings map)
        {
            state.EntrySeconds=map!=null&&map.enemyEntrances?Mathf.Max(.5f,map.enemyEntrySeconds):0;
            // Dedicated entrances are flat, even if the previous life died on a slope.
            if(state.EntrySeconds>0)state.Angle=0;
            state.EntryUntil=state.EntrySeconds>0?Now+state.EntrySeconds:0;
            if(state.EntrySeconds>0&&!state.Turret&&!state.Flying&&state.Roaming)state.Position=GroundEntryOrigin(state);
            state.Velocity=Vector2.zero;state.FireAt=0;state.BurstLeft=0;
            state.NextAttack=Now+state.EntrySeconds+1.5;
        }
    }
}
