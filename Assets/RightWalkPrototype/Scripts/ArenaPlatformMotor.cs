using System.Collections.Generic;
using UnityEngine;

namespace RightWalkPrototype
{
    // Installed only on the owning player in the arena, never on the saved character.
    [DefaultExecutionOrder(-30)]
    public sealed class ArenaPlatformMotor : MonoBehaviour
    {
        public float maximumDrop=4.2f;
        private Rigidbody2D body;
        private BoxCollider2D shape;
        private SideScrollMapPreview movement;
        private float gravity;
        private RaycastHit2D flatJoin;
        private float flatJoinDirection;
        private readonly List<Collider2D> platforms=new List<Collider2D>();
        private readonly HashSet<Collider2D> passableCovers=new HashSet<Collider2D>();
        private readonly HashSet<Collider2D> ignored=new HashSet<Collider2D>();
        private readonly HashSet<Collider2D> admitted=new HashSet<Collider2D>();
        private readonly HashSet<Collider2D> dropping=new HashSet<Collider2D>();
        public bool IsIgnored(Collider2D collider)=>ignored.Contains(collider);
        private void Awake()
        {
            body=GetComponent<Rigidbody2D>();shape=GetComponent<BoxCollider2D>();movement=GetComponent<SideScrollMapPreview>();gravity=body.gravityScale;
            // A square foot catches the inflated side of a coplanar neighbouring
            // box (including a one-way/solid seam). Round only the tiny contact
            // corner; art and the separately calculated damage bounds stay intact.
            shape.edgeRadius=Mathf.Max(shape.edgeRadius,.025f);
            foreach(var effector in FindObjectsByType<PlatformEffector2D>(FindObjectsSortMode.None))
                if(effector.useOneWay)foreach(var collider in effector.GetComponents<Collider2D>())if(collider.usedByEffector)
                {platforms.Add(collider);if(collider.GetComponent<ArenaCover>()!=null)passableCovers.Add(collider);}
        }
        private static float SurfaceY(Collider2D collider,float x)
        {
            var box=(BoxCollider2D)collider;
            Vector3 a=box.transform.TransformPoint(box.offset+new Vector2(-box.size.x*.5f,box.size.y*.5f));
            Vector3 b=box.transform.TransformPoint(box.offset+new Vector2(box.size.x*.5f,box.size.y*.5f));
            return Mathf.Lerp(a.y,b.y,(x-a.x)/(b.x-a.x));
        }
        private void Ignore(Collider2D collider,bool value)
        {
            if(value==ignored.Contains(collider))return;
            Physics2D.IgnoreCollision(shape,collider,value);
            if(value)ignored.Add(collider);else ignored.Remove(collider);
        }
        private void FixedUpdate()
        {
            if(!movement.enabled){Restore();return;}
            foreach(var collider in platforms)
            {
                if(collider==null)continue;
                // Test the leading foot edge, not just the centre: the toe touches
                // the low end of a ramp before the player's centre reaches it.
                float surface=Mathf.Max(SurfaceY(collider,shape.bounds.min.x),SurfaceY(collider,shape.bounds.max.x));
                bool supported=movement.Grounded&&movement.GroundCollider==collider||
                    shape.IsTouching(collider)&&shape.bounds.min.y>=surface-.06f;
                bool slope=Mathf.Abs(collider.transform.up.x)>.01f;
                bool drop=dropping.Contains(collider);
                // A lower landing can still leave the torso intersecting the old
                // platform. Restoring that collision would eject the body upwards.
                bool landedElsewhere=movement.Grounded&&movement.GroundCollider!=null&&movement.GroundCollider!=collider;
                // A solid deck can hand support to the high end of its downhill
                // ramp before the foot leaves the deck. Never admit an uphill
                // route merely because its low end overlaps the floor below.
                bool downhillJoin=landedElsewhere&&shape.bounds.min.y>=collider.bounds.max.y-.06f&&
                    shape.bounds.max.x>=collider.bounds.min.x&&shape.bounds.min.x<=collider.bounds.max.x;
                // An air jump can return above this same surface before any
                // lower landing. Clear the deliberate pass-through only once
                // the whole rounded sole is safely above the leading edge.
                bool jumpedBackAbove=!movement.Grounded&&body.linearVelocity.y>.1f&&
                    shape.bounds.min.y-shape.edgeRadius>surface+.04f;
                float lowerSurface=Mathf.Min(SurfaceY(collider,shape.bounds.min.x),SurfaceY(collider,shape.bounds.max.x));
                bool clearBelow=shape.bounds.max.y+shape.edgeRadius<lowerSurface-.05f;
                bool clearSide=shape.bounds.max.x+shape.edgeRadius<collider.bounds.min.x-.03f||shape.bounds.min.x-shape.edgeRadius>collider.bounds.max.x+.03f;
                if(drop&&(jumpedBackAbove||clearBelow||clearSide))
                {dropping.Remove(collider);drop=false;}
                // Passing below a ramp must not make horizontal walking mount it.
                // Jump through, then land from above to choose that route.
                if(movement.Grounded&&movement.GroundCollider!=collider)admitted.Remove(collider);
                if(supported||downhillJoin)admitted.Add(collider);
                else if(body.linearVelocity.y>.1f||shape.bounds.min.y<surface-.15f)admitted.Remove(collider);
                else if(!movement.Grounded&&shape.bounds.min.y>surface+.04f)admitted.Add(collider);
                // Keep an admitted landing enabled through the contact margin;
                // do not disable it again just before physics resolves the foot.
                // Passable cover also admits horizontal entry from below its top.
                // Its collider stays enabled for weapon traces; only this body ignores it.
                bool below=(slope||ArenaObjectiveMatch.PracticeRoom||passableCovers.Contains(collider))&&!supported&&!admitted.Contains(collider);
                Ignore(collider,drop||below);
            }
        }
        public bool TryDrop(Collider2D support)
        {
            if(support==null||!platforms.Contains(support))return false;
            var bounds=shape.bounds;
            var hits=Physics2D.BoxCastAll(new Vector2(bounds.center.x,bounds.min.y-.08f),new Vector2(bounds.size.x*.85f,.04f),0,Vector2.down,maximumDrop,1);
            RaycastHit2D? landing=null;
            foreach(var hit in hits)
            {
                if(hit.collider!=support&&hit.distance<.15f&&!platforms.Contains(hit.collider))return false;
                if(hit.collider==support||hit.collider.isTrigger||hit.normal.y<.5f||hit.distance<.15f)continue;
                if(!landing.HasValue||hit.distance<landing.Value.distance)landing=hit;
            }
            var arena=RelayArenaPrototype.Instance;
            if(!landing.HasValue||arena==null||!arena.IsSafeLanding(landing.Value.point))return false;
            // Adjacent coplanar one-way pieces under the same sole must pass
            // together. Keep older pending drops until each body overlap clears.
            foreach(var platform in platforms)
            {
                if(platform==null||platform.bounds.max.x<bounds.min.x-shape.edgeRadius||platform.bounds.min.x>bounds.max.x+shape.edgeRadius)continue;
                float surface=SurfaceY(platform,bounds.center.x);
                if(platform!=support&&Mathf.Abs(surface-SurfaceY(support,bounds.center.x))>.12f)continue;
                dropping.Add(platform);admitted.Remove(platform);Ignore(platform,true);
            }
            body.gravityScale=gravity;body.linearVelocity=new Vector2(body.linearVelocity.x,-1);
            return true;
        }
        static bool FlatSolid(RaycastHit2D hit)=>hit.collider!=null&&!hit.collider.usedByEffector&&
            Mathf.Abs(hit.collider.transform.up.x)<.01f&&hit.normal.y>.99f;
        public void CompleteSolidSlopeJoin(ref RaycastHit2D floor,Collider2D previousSupport,bool grounded,bool jumping)
        {
            if(flatJoin.collider!=null)
            {
                float sole=shape.bounds.min.y-shape.edgeRadius;
                if(!grounded||jumping||body.linearVelocity.x*flatJoinDirection<=0||Mathf.Abs(sole-flatJoin.point.y)>.12f)
                    flatJoin=default;
                else
                {
                    // Retain this real flat hit until the narrow probe reaches
                    // it too. Otherwise a fast step can reselect the old slope
                    // and reapply its upward tangent on the following tick.
                    if(FlatSolid(floor))flatJoin=default;else floor=flatJoin;
                    if(body.linearVelocity.y>0)body.linearVelocity=new Vector2(body.linearVelocity.x,0);
                    return;
                }
            }
            if(!grounded||jumping||previousSupport==null||previousSupport.usedByEffector||
                Mathf.Abs(previousSupport.transform.up.x)<.01f||body.linearVelocity.y<=0)return;
            if(!FlatSolid(floor))
            {
                // The narrowed walking probe can miss the flat deck for one
                // frame after the full rounded toe reaches it. Probe that toe
                // only while following a solid incline, never during a jump.
                var bounds=shape.bounds;float sign=Mathf.Sign(body.linearVelocity.x);
                if(sign==0)return;
                float toe=(sign>0?bounds.max.x:bounds.min.x)+sign*(shape.edgeRadius+.01f+Mathf.Abs(body.linearVelocity.x)*Time.fixedDeltaTime);
                var ahead=Physics2D.Raycast(new Vector2(toe,bounds.min.y+.04f),Vector2.down,.2f,1);
                // A ray beginning inside the deck reports its origin, not the
                // top surface. Do not treat that as support above the foot.
                if(!FlatSolid(ahead)||ahead.point.y<ahead.collider.bounds.max.y-.005f)return;
                flatJoin=floor=ahead;flatJoinDirection=sign;
            }
            // A solid incline's upward tangent is walking support, not a jump.
            // Finish it as soon as the existing foot probe reaches the flat deck,
            // before upward velocity makes that same probe report airborne.
            body.linearVelocity=new Vector2(body.linearVelocity.x,0);
        }
        public void ApplyGroundVelocity(ref Vector2 velocity,RaycastHit2D floor,bool grounded,float input,bool jumping)
        {
            bool slope=grounded&&floor.collider!=null&&Mathf.Abs(floor.normal.x)>.01f;
            // On a steep incline the narrowed foot probe is farther from the
            // surface even while the full rounded collider is touching it.
            // Keep actual support still instead of re-enabling downhill gravity.
            bool settled=floor.distance<.04f||floor.collider!=null&&shape.IsTouching(floor.collider);
            body.gravityScale=slope&&settled&&!jumping&&Mathf.Abs(input)<.001f?0:gravity;
            if(!slope||jumping)return;
            if(Mathf.Abs(input)<.001f){if(settled)velocity=Vector2.zero;}
            else velocity.y=-floor.normal.x/floor.normal.y*velocity.x;
        }
        private void Restore()
        {
            if(body!=null)body.gravityScale=gravity;
            if(shape!=null)foreach(var collider in ignored)if(collider!=null)Physics2D.IgnoreCollision(shape,collider,false);
            ignored.Clear();admitted.Clear();dropping.Clear();
            flatJoin=default;flatJoinDirection=0;
        }
        public void ResetContactState(){Restore();}
        private void OnDisable(){Restore();}
    }
}
