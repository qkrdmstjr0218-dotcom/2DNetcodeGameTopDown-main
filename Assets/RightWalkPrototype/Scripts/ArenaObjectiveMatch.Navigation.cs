using System.Collections.Generic;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        public const float DroneSight=9,DroneSpeed=2.2f,DroneNear=3,DroneFar=5;
        sealed class EnemyBrain
        {
            public List<int> Path=new();public int Step,Last=-1;public double Think,Tick,LostUntil;
            public SideScrollNetworkPlayer Target;public Vector2 Goal;
        }
        readonly Dictionary<int,EnemyBrain> enemyBrains=new();
        ArenaEnemyNavigation groundNavigation,flightNavigation;
        void ResetEnemyNavigation(){enemyBrains.Clear();groundNavigation=null;flightNavigation=null;}
        void ResetEnemyBrain(int id){enemyBrains.Remove(id);}
        Vector2 EnemySpawn(PatrolState state)
        {
            var graph=state.Flying?flightNavigation:groundNavigation;var origin=new Vector2(state.Left,state.Floor);
            int node=graph!=null?graph.Nearest(origin):-1;return node>=0?graph.Nodes[node].Point:origin;
        }
        void InitializeEnemyNavigation(ArenaMapSettings map)
        {
            ResetEnemyNavigation();groundNavigation=new ArenaEnemyNavigation(map,false);flightNavigation=new ArenaEnemyNavigation(map,true);
            for(int i=0;i<Patrols.Count;i++)
            {
                var state=Patrols[i];
                if(map.groundEnemySpawns!=null&&i<map.groundEnemySpawns.Length){var at=map.groundEnemySpawns[i];state.Left=at.x;state.Right=at.x;state.Floor=at.y;}
                state.Roaming=true;state.Position=EnemySpawn(state);state.Started=Now;Patrols[i]=state;
            }
            if(map.droneSpawns!=null)foreach(var at in map.droneSpawns)
            {
                var state=new PatrolState{Id=++nextPatrolId,Generation=1,Health=patrolHealth,Left=at.x,Right=at.x,Floor=at.y,Speed=DroneSpeed,Roaming=true,Flying=true,Started=Now,Aim=Vector2.right,NextAttack=Now+1.5};
                state.Position=EnemySpawn(state);Patrols.Add(state);
            }
        }
        SideScrollNetworkPlayer RoamingTarget(PatrolState state,EnemyBrain brain,SideScrollNetworkPlayer[] players)
        {
            Vector2 centre=state.Position+Vector2.up*.48f;float nearest=state.Flying?DroneSight:8;SideScrollNetworkPlayer found=null;
            foreach(var p in players)
            {
                if(!p.IsSpawned||p.IsDead||!p.HasCurrentLifePose)continue;
                float distance=Vector2.Distance(centre,p.ArenaHitBounds.center);
                if(distance<nearest&&!ArenaGrenadeBallistics.WorldTrace(centre,p.ArenaHitBounds.center,0,out _)){found=p;nearest=distance;}
            }
            if(found!=null){brain.Target=found;brain.LostUntil=Now+3;}
            if(brain.Target==null||!brain.Target.IsSpawned||brain.Target.IsDead||!brain.Target.HasCurrentLifePose||Now>brain.LostUntil||Vector2.Distance(centre,brain.Target.ArenaHitBounds.center)>13)brain.Target=null;
            return brain.Target;
        }
        Vector2 DroneGoal(PatrolState state,SideScrollNetworkPlayer target)
        {
            Vector2 centre=target.ArenaHitBounds.center,at=state.Position+Vector2.up*.48f,radial=at-centre;
            float distance=radial.magnitude;
            if(distance>=DroneNear&&distance<=DroneFar&&!ArenaGrenadeBallistics.WorldTrace(at,centre,0,out _))return state.Position;
            Vector2 best=centre+(radial.sqrMagnitude>.01f?radial.normalized:Vector2.up)*4-Vector2.up*.48f;float cost=float.MaxValue;
            for(int i=0;i<16;i++)
            {
                float angle=i*Mathf.PI/8;Vector2 candidate=centre+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*4;
                if(!ArenaEnemyNavigation.FlightClear(candidate-Vector2.up*.48f)||ArenaGrenadeBallistics.WorldTrace(candidate,centre,0,out _))continue;
                float d=(candidate-at).sqrMagnitude;if(d<cost){cost=d;best=candidate-Vector2.up*.48f;}
            }
            return best;
        }
        void UpdateRoamingEnemy(ref PatrolState state,SideScrollNetworkPlayer[] players)
        {
            var graph=state.Flying?flightNavigation:groundNavigation;if(graph==null)return;
            if(!enemyBrains.TryGetValue(state.Id,out var brain)){brain=new EnemyBrain{Tick=Now,Think=Now};enemyBrains.Add(state.Id,brain);}
            if(Now-brain.Tick<.1)return;
            float dt=Mathf.Min(.2f,(float)(Now-brain.Tick));brain.Tick=Now;
            if(!state.Flying&&!ArenaEnemyNavigation.FloorBelow(state.Position+Vector2.up*.08f,.2f,out _))
            {
                brain.Path.Clear();brain.Step=0;brain.Think=Now;state.Angle=0;state.RushPhase=0;
                state.Velocity=new Vector2(0,Mathf.Min(0,state.Velocity.y)-14*dt);
                var next=state.Position+state.Velocity*dt;
                if(ArenaEnemyNavigation.FloorBelow(state.Position+Vector2.up*.08f,state.Position.y-next.y+.1f,out var landed))
                {state.Position=landed.point+Vector2.up*.02f;state.Velocity=Vector2.zero;}
                else state.Position=next;
                if(state.Position.y< -5||RelayArenaPrototype.Instance!=null&&!RelayArenaPrototype.Instance.IsSafeLanding(state.Position)){state.Health=0;state.RespawnAt=Now+Mathf.Max(.1f,patrolRespawnSeconds);}
                state.Started=Now;return;
            }
            if(!state.Flying&&TickGroundRush(ref state,brain,graph)){state.Started=Now;return;}
            if(state.RushPhase!=2&&Now>=brain.Think)
            {
                brain.Think=Now+.6;var target=RoamingTarget(state,brain,players);
                var goal=target!=null?(state.Flying?DroneGoal(state,target):new Vector2(target.ArenaHitBounds.center.x,target.ArenaHitBounds.min.y)):state.Position;
                if(target!=null||brain.Step>=brain.Path.Count)
                {
                    brain.Goal=goal;brain.Path=graph.Route(state.Position,goal,target==null);brain.Step=brain.Path.Count>1?1:0;brain.Last=brain.Path.Count>0?brain.Path[0]:-1;
                }
            }
            if(!state.Flying&&state.RushPhase==0&&TryStartGroundRush(ref state,brain)){state.Started=Now;return;}
            if(!state.Flying)state.Speed=state.RushPhase==2?groundRushSpeed:brain.Target!=null?groundChaseSpeed:patrolSpeed;
            Vector2 before=state.Position;float budget=state.Speed*dt;
            while(brain.Step<brain.Path.Count&&budget>0)
            {
                int node=brain.Path[brain.Step];var next=graph.Nodes[node];
                if(state.RushPhase==2&&(next.Point.x-state.Position.x)*state.Aim.x<-.03f){EndGroundRush(ref state);break;}
                var current=new ArenaEnemyNavigation.Node(state.Position,new Vector2(-Mathf.Sin(state.Angle*Mathf.Deg2Rad),Mathf.Cos(state.Angle*Mathf.Deg2Rad)));
                if(!graph.CanTraverse(current,next))
                {graph.Block(brain.Last,node);brain.Path.Clear();brain.Step=0;brain.Think=Now;if(state.RushPhase==2)EndGroundRush(ref state);break;}
                float distance=Vector2.Distance(state.Position,next.Point);bool reached=distance<=budget+.001f;state.Position=Vector2.MoveTowards(state.Position,next.Point,budget);budget-=distance;
                state.Angle=state.Flying?0:Mathf.Atan2(-next.Normal.x,next.Normal.y)*Mathf.Rad2Deg;
                if(reached){brain.Last=node;brain.Step++;}else break;
            }
            state.Velocity=(state.Position-before)/dt;if(state.RushPhase==2&&brain.Step>=brain.Path.Count)EndGroundRush(ref state);state.Started=Now;
        }
    }
}
