using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaEnemyNavigation
    {
        void ConnectJumpIslands()
        {
            if(jumpSpeed<=0||jumpGravity<=0)return;
            var islands=new int[Nodes.Count];for(int i=0;i<islands.Length;i++)islands[i]=-1;
            int island=0;var queue=new System.Collections.Generic.List<int>();
            for(int root=0;root<Nodes.Count;root++)if(islands[root]<0)
            {queue.Clear();queue.Add(root);islands[root]=island;for(int at=0;at<queue.Count;at++)foreach(int next in links[queue[at]])if(islands[next]<0){islands[next]=island;queue.Add(next);}island++;}
            // Normal walking already connects a surface. Only bridge separate
            // surfaces, avoiding quadratic arc probes and needless flat jumps.
            for(int a=0;a<Nodes.Count;a++)for(int b=a+1;b<Nodes.Count;b++)
                if(islands[a]!=islands[b]&&JumpCandidate(Nodes[a],Nodes[b]))
                {if(CanJump(Nodes[a],Nodes[b])&&CanJump(Nodes[b],Nodes[a]))Link(a,b);else closed.Add(new ClosedEdge{A=a,B=b,Retry=Time.realtimeSinceStartup+2});}
        }
        bool JumpCandidate(Node a,Node b)
        {
            var delta=b.Point-a.Point;
            return !flying&&jumpSpeed>0&&jumpGravity>0&&Mathf.Abs(delta.x)>=.3f&&Mathf.Abs(delta.x)<=3.2f&&Mathf.Abs(delta.y)<=3.1f&&a.Normal.y>.6f&&b.Normal.y>.6f;
        }
        public bool RequiresJump(Node a,Node b)=>JumpCandidate(a,b)&&!CanWalk(a,b);
        bool CanJump(Node a,Node b)
        {
            if(!JumpCandidate(a,b)||!GroundClear(b.Point,b.Normal))return false;
            if(!FloorBelow(a.Point+Vector2.up*.15f,.3f,out var start)||!FloorBelow(b.Point+Vector2.up*.15f,.3f,out var end)||Mathf.Abs(start.point.y-a.Point.y)>.12f||Mathf.Abs(end.point.y-b.Point.y)>.12f)return false;
            float rise=b.Point.y-a.Point.y,apex=jumpSpeed*jumpSpeed/(2*jumpGravity);
            bool twice=rise>apex-.2f||Mathf.Abs(b.Point.x-a.Point.x)>2.4f;
            float apexTime=twice?jumpSpeed/jumpGravity:0,secondRise=twice?rise-apex:rise;
            float discriminant=jumpSpeed*jumpSpeed-2*jumpGravity*secondRise;if(discriminant<0)return false;
            float duration=apexTime+(jumpSpeed+Mathf.Sqrt(discriminant))/jumpGravity;
            if(duration<=0)return false;
            Vector2 previous=a.Point;
            for(int i=1;i<=10;i++)
            {
                float t=duration*i/10,vertical=twice&&t>apexTime?apex+jumpSpeed*(t-apexTime)-jumpGravity*(t-apexTime)*(t-apexTime)*.5f:jumpSpeed*t-jumpGravity*t*t*.5f;
                var at=new Vector2(Mathf.Lerp(a.Point.x,b.Point.x,i/10f),a.Point.y+vertical);
                if(ArenaGrenadeBallistics.WorldTrace(previous+Vector2.up*.32f,at+Vector2.up*.32f,.27f,out _)||ArenaGrenadeBallistics.WorldTrace(previous+Vector2.up*.95f,at+Vector2.up*.95f,.27f,out _))return false;
                previous=at;
            }
            return true;
        }
    }
}
