using System.Collections.Generic;
using UnityEngine;
namespace RightWalkPrototype
{
    public static class ArenaGrenadeBallistics
    {
        public const float Gravity=14,Radius=.09f,Step=1f/60,MaximumLife=3,MinimumSpeed=4,MaximumSpeed=18,BlastRadius=2;
        public static float Speed(float distance)=>Mathf.Clamp(4+distance*1.1f,MinimumSpeed,MaximumSpeed);
        // Cursor distance remains useful for short throws, but a nearby upper cursor
        // must not force a weak lob simply because the vertical screen space is short.
        public static float LaunchSpeed(float distance,Vector2 direction)
        {
            float upward=Mathf.Clamp01(direction.normalized.y);
            return Mathf.Max(Speed(distance),Mathf.Lerp(MinimumSpeed,12,upward*upward));
        }
        public static Vector2 At(Vector2 origin,Vector2 velocity,float time)=>origin+velocity*time+Vector2.down*(Gravity*.5f*time*time);
        static readonly RaycastHit2D[] hits=new RaycastHit2D[128];
        static bool IsWorld(Collider2D shape)
        {
            if(shape==null||shape.isTrigger||!shape.enabled||shape.GetComponentInParent<SideScrollNetworkPlayer>()!=null||shape.GetComponentInParent<ArenaPatrolEnemy>()!=null||shape.GetComponentInParent<SideScrollDamageTarget>()!=null)return false;
            return !(shape.usedByEffector&&shape.TryGetComponent<PlatformEffector2D>(out var platform)&&platform.useOneWay&&shape.GetComponent<ArenaCover>()==null);
        }
        public static bool WorldTrace(Vector2 from,Vector2 to,float radius,out Vector2 point)
        {
            Vector2 delta=to-from;float distance=delta.magnitude;point=to;if(distance<.00001f)return false;
            var filter=new ContactFilter2D();filter.SetLayerMask(Physics2D.DefaultRaycastLayers);filter.useTriggers=false;
            int count=radius>0?Physics2D.CircleCast(from,radius,delta/distance,filter,hits,distance):Physics2D.Raycast(from,delta/distance,filter,hits,distance);float nearest=distance;bool found=false;
            for(int i=0;i<count;i++)if(IsWorld(hits[i].collider)&&hits[i].distance<=nearest){nearest=hits[i].distance;found=true;}
            if(found)point=from+delta/distance*nearest;return found;
        }
        public sealed class Context
        {
            readonly SideScrollNetworkPlayer shooter;
            readonly SideScrollNetworkPlayer[] players;
            readonly ArenaPatrolEnemy[] enemies;
            readonly SideScrollDamageTarget[] targets;
            public Context(SideScrollNetworkPlayer source)
            {shooter=source;players=Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);enemies=Object.FindObjectsByType<ArenaPatrolEnemy>(FindObjectsSortMode.None);targets=Object.FindObjectsByType<SideScrollDamageTarget>(FindObjectsSortMode.None);}
            public bool Sweep(Vector2 from,Vector2 to,out Vector2 point)
            {
                bool found=WorldTrace(from,to,Radius,out point);float nearest=Vector2.Distance(from,point);Vector2 delta=to-from;float distance=delta.magnitude;if(distance<.00001f)return found;var direction=delta/distance;
                void Check(Bounds bounds)
                {
                    bounds.Expand(new Vector3(Radius*2,Radius*2,0));float hit;
                    if(bounds.Contains(new Vector3(from.x,from.y,bounds.center.z)))hit=0;
                    else if(!bounds.IntersectRay(new Ray(new Vector3(from.x,from.y,bounds.center.z),direction),out hit))return;
                    if(hit>=0&&hit<=nearest){nearest=hit;found=true;}
                }
                foreach(var p in players)if(p!=null&&p!=shooter&&p.IsSpawned&&!p.IsDead)Check(p.ArenaHitBounds);
                foreach(var e in enemies)if(e!=null&&e.isActiveAndEnabled&&e.TryGetComponent<Collider2D>(out var shape)&&shape.enabled)Check(shape.bounds);
                foreach(var t in targets)if(t!=null&&t.IsSpawned&&t.Health.Value>0&&t.TryGetComponent<Collider2D>(out var shape)&&shape.enabled)Check(shape.bounds);
                point=from+direction*nearest;return found;
            }
        }
        public static bool Predict(SideScrollNetworkPlayer source,Vector2 origin,Vector2 velocity,List<Vector2> points,out Vector2 end)
        {
            var context=new Context(source);points.Clear();points.Add(origin);
            if(context.Sweep(source.ArenaHitBounds.center,origin,out end)){points.Add(end);return true;}
            Vector2 previous=origin;int steps=Mathf.CeilToInt(MaximumLife/Step);
            for(int i=1;i<=steps;i++)
            {Vector2 next=At(origin,velocity,i*Step);bool hit=context.Sweep(previous,next,out end);if(i%3==0||hit||i==steps)points.Add(end);if(hit)return true;previous=next;}
            end=previous;return false;
        }
        public static float BlastDistance(Vector2 origin,Bounds bounds)=>Vector2.Distance(origin,bounds.ClosestPoint(new Vector3(origin.x,origin.y,bounds.center.z)));
        public static bool InBlast(Vector2 origin,Bounds bounds)=>BlastDistance(origin,bounds)<=BlastRadius;
        public static int BlastDamage(Vector2 origin,Bounds bounds,int maximum,int minimum)
        {
            float distance=BlastDistance(origin,bounds);
            return distance>BlastRadius?0:Mathf.RoundToInt(Mathf.Lerp(maximum,minimum,Mathf.Clamp01(distance/BlastRadius)));
        }
    }
}
