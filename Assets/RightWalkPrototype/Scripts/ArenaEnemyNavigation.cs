using System.Collections.Generic;
using UnityEngine;
namespace RightWalkPrototype
{
    // Local navigation data is built once on the server. It is never a client authority.
    public sealed partial class ArenaEnemyNavigation
    {
        public struct Node { public Vector2 Point,Normal; public Node(Vector2 p,Vector2 n){Point=p;Normal=n;} }
        public readonly List<Node> Nodes=new();
        readonly List<List<int>> links=new();
        readonly bool flying;
        readonly float jumpSpeed,jumpGravity;
        struct ClosedEdge { public int A,B;public float Retry; }
        readonly List<ClosedEdge> closed=new();
        static readonly Collider2D[] overlaps=new Collider2D[64];
        static bool Terrain(Collider2D c)=>c!=null&&c.enabled&&!c.isTrigger&&c.GetComponentInParent<SideScrollNetworkPlayer>()==null&&
            c.GetComponentInParent<ArenaPatrolEnemy>()==null&&c.GetComponentInParent<SideScrollDamageTarget>()==null;
        static bool Solid(Collider2D c)=>Terrain(c)&&!c.usedByEffector;
        static bool ClearCircle(Vector2 point,float radius)
        {
            int count=Physics2D.OverlapCircleNonAlloc(point,radius,overlaps,1);
            for(int i=0;i<count;i++)if(Solid(overlaps[i]))return false;
            return count<overlaps.Length;
        }
        public static bool FlightClear(Vector2 foot)=>ClearCircle(foot+Vector2.up*.48f,.48f);
        static bool GroundClear(Vector2 foot,Vector2 normal)=>ClearCircle(foot+normal*.5f,.32f)&&ClearCircle(foot+normal*.8f,.3f);
        public static bool FloorBelow(Vector2 from,float distance,out RaycastHit2D floor)
        {
            floor=default;float nearest=float.MaxValue;
            foreach(var hit in Physics2D.RaycastAll(from,Vector2.down,distance,1))
                if(Terrain(hit.collider)&&hit.normal.y>.6f&&hit.distance<nearest){floor=hit;nearest=hit.distance;}
            return floor.collider!=null;
        }
        int Add(Vector2 at,Vector2 normal){Nodes.Add(new Node(at,normal));links.Add(new List<int>());return Nodes.Count-1;}
        void Link(int a,int b){links[a].Add(b);links[b].Add(a);}
        public ArenaEnemyNavigation(ArenaMapSettings map,bool air,float jumpSpeed=0,float jumpGravity=0)
        {
#if ARENA_PERFORMANCE_AUDIT
            using var audit=ArenaPerformanceCounters.Measure("Navigation.Build");
#endif
            flying=air;this.jumpSpeed=jumpSpeed;this.jumpGravity=jumpGravity;
            var area=map.cameraBounds;
            if(air)
            {
                var cells=new Dictionary<Vector2Int,int>();
                int width=Mathf.CeilToInt((area.width-2)/.75f),height=Mathf.CeilToInt((area.height-2)/.75f);
                for(int x=0;x<=width;x++)for(int y=0;y<=height;y++)
                {
                    var p=new Vector2(area.xMin+1+x*.75f,area.yMin+1+y*.75f);
                    if(FlightClear(p))cells[new Vector2Int(x,y)]=Add(p,Vector2.up);
                }
                foreach(var pair in cells)for(int dx=-1;dx<=1;dx++)for(int dy=0;dy<=1;dy++)
                {
                    if(dy==0&&dx<=0||!cells.TryGetValue(pair.Key+new Vector2Int(dx,dy),out int b))continue;
                    if(CanTraverse(Nodes[pair.Value],Nodes[b]))Link(pair.Value,b);
                }
            }
            else
            {
                foreach(var box in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
                {
                    if(!Terrain(box)||box.gameObject.layer!=0||Vector2.Dot(box.transform.up,Vector2.up)<.6f)continue;
                    Vector2 a=box.transform.TransformPoint(box.offset+new Vector2(-box.size.x*.5f,box.size.y*.5f));
                    Vector2 b=box.transform.TransformPoint(box.offset+new Vector2(box.size.x*.5f,box.size.y*.5f));
                    int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.55f));
                    for(int i=0;i<=steps;i++)
                    {
                        Vector2 at=Vector2.Lerp(a,b,i/(float)steps),normal=box.transform.up;
                        if(RelayArenaPrototype.Instance!=null&&!RelayArenaPrototype.Instance.IsSafeLanding(at))continue;
                        if(at.x<area.xMin+1||at.x>area.xMax-1||at.y<area.yMin||at.y>area.yMax||!GroundClear(at,normal))continue;
                        Add(at+normal*.02f,normal);
                    }
                }
                // Keep potential nodes for retracting leaves even if this round
                // begins while their colliders are disabled. Edges open only when
                // real floor and clearance are present.
                foreach(var bridge in Object.FindObjectsByType<ArenaRetractingBridge>(FindObjectsSortMode.None))
                    for(float x=bridge.left;x<=bridge.right+.01f;x+=.5f)Add(new Vector2(x,bridge.floor+.02f),Vector2.up);
                for(int a=0;a<Nodes.Count;a++)for(int b=a+1;b<Nodes.Count;b++)
                {
                    var delta=Nodes[b].Point-Nodes[a].Point;
                    if(delta.sqrMagnitude>.85f*.85f||Mathf.Abs(delta.y)>Mathf.Abs(delta.x)*1.15f+.08f)continue;
                    if(CanWalk(Nodes[a],Nodes[b]))Link(a,b);else closed.Add(new ClosedEdge{A=a,B=b,Retry=Time.realtimeSinceStartup+2});
                }
                ConnectJumpIslands();
            }
        }
        public bool CanTraverse(Node a,Node b)
        {
#if ARENA_PERFORMANCE_AUDIT
            using var audit=ArenaPerformanceCounters.Measure("Navigation.Traverse");
#endif
            if(flying)return FlightClear(b.Point)&&!ArenaGrenadeBallistics.WorldTrace(a.Point+Vector2.up*.48f,b.Point+Vector2.up*.48f,.47f,out _);
            return CanWalk(a,b)||CanJump(a,b);
        }
        bool CanWalk(Node a,Node b)
        {
            var delta=b.Point-a.Point;if(delta.sqrMagnitude>.85f*.85f||Mathf.Abs(delta.y)>Mathf.Abs(delta.x)*1.15f+.08f)return false;
            Vector2 middle=(a.Point+b.Point)*.5f;
            bool supported=false;
            foreach(var hit in Physics2D.RaycastAll(middle+Vector2.up*.25f,Vector2.down,.5f,1))
                if(Terrain(hit.collider)&&hit.normal.y>.6f&&Mathf.Abs(hit.point.y-middle.y)<.16f){supported=true;break;}
            return supported&&GroundClear(b.Point,b.Normal)&&!ArenaGrenadeBallistics.WorldTrace(a.Point+a.Normal*.53f,b.Point+b.Normal*.53f,.28f,out _);
        }
        public int Nearest(Vector2 point)
        {
            int best=-1;float distance=float.MaxValue;
            for(int i=0;i<Nodes.Count;i++){float d=(Nodes[i].Point-point).sqrMagnitude;if(d<distance){best=i;distance=d;}}
            return best;
        }
        public List<int> Route(Vector2 origin,Vector2 destination,bool wander)
        {
#if ARENA_PERFORMANCE_AUDIT
            using var audit=ArenaPerformanceCounters.Measure("Navigation.Route");
#endif
            for(int i=closed.Count-1;i>=0;i--)
            {
                var edge=closed[i];if(Time.realtimeSinceStartup<edge.Retry)continue;
                if(CanTraverse(Nodes[edge.A],Nodes[edge.B])&&CanTraverse(Nodes[edge.B],Nodes[edge.A])){Link(edge.A,edge.B);closed.RemoveAt(i);}
                else{edge.Retry=Time.realtimeSinceStartup+2;closed[i]=edge;}
            }
            var path=new List<int>();int start=Nearest(origin);if(start<0)return path;
            var parents=new int[Nodes.Count];for(int i=0;i<parents.Length;i++)parents[i]=-2;
            var reachable=new List<int>{start};parents[start]=-1;
            for(int head=0;head<reachable.Count;head++)foreach(int next in links[reachable[head]])
                if(parents[next]==-2){parents[next]=reachable[head];reachable.Add(next);}
            int end=start;float best=float.MaxValue;
            if(wander)
            {
                var far=reachable.FindAll(n=>Vector2.Distance(Nodes[n].Point,origin)>4);
                var choices=far.Count>0?far:reachable;end=choices[Random.Range(0,choices.Count)];
            }
            else foreach(int n in reachable){float d=(Nodes[n].Point-destination).sqrMagnitude;if(d<best){best=d;end=n;}}
            while(end>=0){path.Add(end);end=parents[end];}path.Reverse();return path;
        }
        public void Block(int a,int b)
        {if(a>=0&&b>=0&&links[a].Remove(b)){links[b].Remove(a);closed.Add(new ClosedEdge{A=a,B=b,Retry=Time.realtimeSinceStartup+2});}}
    }
}
