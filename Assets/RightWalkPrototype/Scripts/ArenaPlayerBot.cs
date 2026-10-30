using System.Collections.Generic;
using UnityEngine;
namespace RightWalkPrototype
{
    // Prototype brain: only the solo host installs this component. No input-device injection.
    [DefaultExecutionOrder(-40)]
    public sealed partial class ArenaPlayerBot:MonoBehaviour
    {
        SideScrollNetworkPlayer player,target;
        SideScrollMapPreview motor;
        Rigidbody2D body;
        ArenaEnemyNavigation navigation;
        List<int> route=new();
        int step,version=-1;
        float thinkAt,shotAt,jumpAt,stuckAt;
        Vector2 lastFoot,goal;
        public int Decisions{get;private set;}
        public int JumpRequests{get;private set;}
        public string Activity{get;private set;}="대기";
        void Awake(){player=GetComponent<SideScrollNetworkPlayer>();motor=GetComponent<SideScrollMapPreview>();body=GetComponent<Rigidbody2D>();}
        Vector2 Foot=>new Vector2(transform.position.x,GetComponent<BoxCollider2D>().bounds.min.y);
        void Update()
        {
#if ARENA_PERFORMANCE_AUDIT
            using var audit=ArenaPerformanceCounters.Measure("Bot.Update");
#endif
            if(!player.IsSpawned||!player.IsServer||!player.IsBot)return;
            var match=ArenaObjectiveMatch.Instance;
            bool active=match!=null&&match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing&&!ArenaObjectiveMatch.Ended&&!player.IsDead;
            if(!active){motor.SetInput(0);player.ResolveSprint(false);Activity="대기";route.Clear();ClearDecision();return;}
            var map=RelayArenaPrototype.Instance.GetComponent<ArenaMapSettings>();
            if(navigation==null&&map!=null)navigation=new ArenaEnemyNavigation(map,false,motor.jumpSpeed,-Physics2D.gravity.y*body.gravityScale);
            Vector2 foot=Foot;
            if(version!=match.State.Value.Version){version=match.State.Value.Version;objective=-1;provokedUntil=0;nextDodge=Time.time+.8f;thinkAt=0;stuckAt=Time.time;lastFoot=foot;}
            if(Time.time>=thinkAt)
            {
                Decisions++;thinkAt=Time.time+ThinkInterval+(player.PlayerNumber%3)*.08f;
                ChooseGoal(match,foot);
                if(navigation!=null){route=navigation.Route(foot,goal,false);step=route.Count>1?1:0;}
            }
            Vector2 waypoint=goal;bool pathJump=false;
            if(navigation!=null&&step<route.Count)
            {
                while(step<route.Count&&Vector2.Distance(foot,navigation.Nodes[route[step]].Point)<.65f)step++;
                if(step<route.Count)
                {
                    // Retracting bridges may close after planning. Never walk a stale edge.
                    if(step>0&&!navigation.CanTraverse(navigation.Nodes[route[step-1]],navigation.Nodes[route[step]]))
                    {navigation.Block(route[step-1],route[step]);route.Clear();thinkAt=0;motor.SetInput(0);return;}
                    waypoint=navigation.Nodes[route[step]].Point;
                    if(step>0)pathJump=navigation.RequiresJump(navigation.Nodes[route[step-1]],navigation.Nodes[route[step]]);
                }
                else if(Vector2.Distance(foot,goal)>1.6f)waypoint=foot;
            }
            else if(navigation!=null&&route.Count==0)waypoint=foot;
            float dx=waypoint.x-foot.x;
            float move=Mathf.Abs(dx)>.22f?Mathf.Sign(dx):0;
            if(match.Points[match.State.Value.Point].Contains(player)&&!HasTarget&&!seekingPickup){move=0;Activity="해킹 중";}
            if(HasTarget&&!seekingPickup&&!pursuingObjective&&Vector2.Distance(player.ArenaHitBounds.center,targetBounds.center)<3.5f&&Mathf.Abs(targetBounds.min.y-Foot.y)<1.5f)move=0;
            if(Vector2.Distance(foot,lastFoot)>.35f){lastFoot=foot;stuckAt=Time.time;}
            bool stalled=move!=0&&Time.time-stuckAt>.8f;
            bool jump=Time.time>jumpAt&&(waypoint.y-foot.y>.5f||stalled||pathJump)&&
                (motor.Grounded||motor.JumpCount<2&&body.linearVelocity.y<.5f);
            if(!jump&&CombatDodge(move))jump=true;
            if(jump){JumpRequests++;jumpAt=Time.time+.45f;}
            // Drop through one-way flooring when the goal is directly below; solid floors still require ramps.
            if(goal.y<foot.y-2&&Mathf.Abs(goal.x-foot.x)<2&&motor.Grounded&&motor.GroundCollider!=null&&motor.GroundCollider.usedByEffector)
                GetComponent<ArenaPlatformMotor>().TryDrop(motor.GroundCollider);
            motor.SetInput(move,jump,Mathf.Abs(goal.x-foot.x)>5&&player.Stamina>30);
            TickCombat();
        }
        void OnDisable(){if(motor!=null)motor.SetInput(0);}
    }
}
