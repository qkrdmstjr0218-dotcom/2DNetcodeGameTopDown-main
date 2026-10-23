using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        public bool EnemyEntering(PatrolState state)=>!state.Medical&&state.EntryUntil>Now;
        void BeginEnemyEntry(ref PatrolState state,ArenaMapSettings map)
        {
            state.EntrySeconds=map!=null&&map.enemyEntrances?Mathf.Max(.5f,map.enemyEntrySeconds):0;
            // Dedicated entrances are flat, even if the previous life died on a slope.
            if(state.EntrySeconds>0)state.Angle=0;
            state.EntryUntil=state.EntrySeconds>0?Now+state.EntrySeconds:0;
            state.Velocity=Vector2.zero;state.FireAt=0;state.BurstLeft=0;
            state.NextAttack=Now+state.EntrySeconds+1.5;
        }
    }
}
