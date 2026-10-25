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
        private Vector2 commandedVelocity;
        private bool landingControl;
        private RaycastHit2D flatJoin;
        private float flatJoinDirection;
        private Collider2D lastSlope;
        private float lastSlopeAt=float.NegativeInfinity;
        private readonly List<Collider2D> platforms=new List<Collider2D>();
        private readonly HashSet<Collider2D> passableCovers=new HashSet<Collider2D>();
        private readonly HashSet<Collider2D> ignored=new HashSet<Collider2D>();
        private readonly HashSet<Collider2D> admitted=new HashSet<Collider2D>();
        private readonly HashSet<Collider2D> dropping=new HashSet<Collider2D>();
        public bool IsIgnored(Collider2D collider)=>ignored.Contains(collider);
        private void Awake()
        {
            body=GetComponent<Rigidbody2D>();shape=GetComponent<BoxCollider2D>();movement=GetComponent<SideScrollMapPreview>();gravity=body.gravityScale;
            var map=FindFirstObjectByType<ArenaMapSettings>();
            if(map!=null)maximumDrop=Mathf.Max(maximumDrop,map.floorSpacing+.6f);
            // A square foot catches the inflated side of a coplanar neighbouring
            // box (including a one-way/solid seam). Round only the tiny contact
            // corner; art and the separately calculated damage bounds stay intact.
            shape.edgeRadius=Mathf.Max(shape.edgeRadius,.025f);
            RefreshPlatforms();
        }
        public void RefreshPlatforms()
        {
            foreach(var c in ignored)if(c!=null&&shape!=null)Physics2D.IgnoreCollision(shape,c,false);
            platforms.Clear();passableCovers.Clear();ignored.Clear();admitted.Clear();dropping.Clear();flatJoin=default;
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
            if(movement.HitReacting)ReleaseSolidJoin();
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
        private void ReleaseSolidJoin()
        {
            flatJoin=default;
        }
        public void CompleteSolidSlopeJoin(ref RaycastHit2D floor,Collider2D previousSupport,bool grounded,bool jumping)
        {
            var bounds=shape.bounds;
            float sole=bounds.min.y-shape.edgeRadius;
            if(flatJoin.collider!=null)
            {
                if(!grounded||jumping||body.linearVelocity.x*flatJoinDirection<=0||Mathf.Abs(sole-flatJoin.point.y)>.16f)
                    ReleaseSolidJoin();
                else
                {
                    // Keep support until the entire rounded foot clears the ramp,
                    // not just until the narrower ground probe first sees the deck.
                    floor=flatJoin;
                    float lift=flatJoin.point.y+.03f-sole;
                    if(lift>0)body.position+=Vector2.up*lift;
                    if(body.linearVelocity.y>0)body.linearVelocity=new Vector2(body.linearVelocity.x,0);
                    var deck=flatJoin.collider.bounds;
                    bool fullyOnDeck=flatJoinDirection>0?bounds.min.x-shape.edgeRadius>deck.min.x+.02f:
                        bounds.max.x+shape.edgeRadius<deck.max.x-.02f;
                    if(fullyOnDeck)ReleaseSolidJoin();
                    return;
                }
            }
            // The probe can briefly see the flat one-way deck inside the torso
            // before the rounded sole reaches its edge. Retain only the recent
            // grounded ramp contact to complete this same ascending junction.
            if((previousSupport==null||Mathf.Abs(previousSupport.transform.up.x)<.01f)&&Time.fixedTime-lastSlopeAt<=.12f)
                previousSupport=lastSlope;
            bool recent=previousSupport==lastSlope&&Time.fixedTime-lastSlopeAt<=.12f;
            if((!grounded&&!recent)||jumping||movement.HitReacting||previousSupport==null||IsIgnored(previousSupport)||
                Mathf.Abs(previousSupport.transform.up.x)<.01f||body.linearVelocity.y<=0)return;
            float sign=Mathf.Sign(body.linearVelocity.x);
            if(sign==0)return;
            var ahead=floor;
            if(FlatSolid(ahead))ahead.point=new Vector2(ahead.point.x,ahead.collider.bounds.max.y);
            else
            {
                float toe=(sign>0?bounds.max.x:bounds.min.x)+sign*(shape.edgeRadius+.01f+Mathf.Abs(body.linearVelocity.x)*Time.fixedDeltaTime);
                // Start above the contact margin so a slightly low rounded toe
                // cannot report an inside-collider ray hit on the deck's side.
                ahead=Physics2D.Raycast(new Vector2(toe,sole+.2f),Vector2.down,.36f,1);
            }
            if(!FlatSolid(ahead)||ahead.point.y<ahead.collider.bounds.max.y-.005f||
                Mathf.Abs(ahead.point.y-previousSupport.bounds.max.y)>.03f||
                Mathf.Abs(sole-ahead.point.y)>.16f)return;
            flatJoin=floor=ahead;flatJoinDirection=sign;
            // Clear the rounded foot and the physics contact margin together;
            // merely reaching y=0 still lets the deck's corner push sideways.
            float step=ahead.point.y+.03f-sole;
            if(step>0)body.position+=Vector2.up*step;
            body.linearVelocity=new Vector2(body.linearVelocity.x,0);
        }
        public void ApplyGroundVelocity(ref Vector2 velocity,RaycastHit2D floor,bool grounded,float input,bool jumping)
        {
            commandedVelocity=velocity;landingControl=!jumping&&!movement.HitReacting&&velocity.y<-.1f&&(floor.collider==null||!shape.IsTouching(floor.collider));
            bool slope=grounded&&floor.collider!=null&&Mathf.Abs(floor.normal.x)>.01f;
            if(jumping||movement.HitReacting){lastSlope=null;lastSlopeAt=float.NegativeInfinity;}
            else if(slope){lastSlope=floor.collider;lastSlopeAt=Time.fixedTime;}
            // On a steep incline the narrowed foot probe is farther from the
            // surface even while the full rounded collider is touching it.
            // Keep actual support still instead of re-enabling downhill gravity.
            bool settled=floor.distance<.04f||floor.collider!=null&&shape.IsTouching(floor.collider);
            // Once supported, one-way and solid slopes use the same controlled
            // tangent motion. Air, jump/drop and knockback still use normal gravity.
            bool controlledSlope=slope&&settled&&!jumping&&!movement.HitReacting;
            body.gravityScale=controlledSlope?0:gravity;
            if(!slope||jumping)return;
            if(Mathf.Abs(input)<.001f){if(settled)velocity=Vector2.zero;}
            else velocity.y=-floor.normal.x/floor.normal.y*velocity.x;
        }
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if(!landingControl||!movement.enabled||movement.HitReacting)return;
            var floor=collision.collider;
            if(!(floor is BoxCollider2D)||floor.isTrigger||IsIgnored(floor)||floor.GetComponentInParent<SideScrollNetworkPlayer>()!=null||floor.GetComponentInParent<ArenaPatrolEnemy>()!=null)return;
            Vector2 normal=floor.transform.up;if(normal.y<.6f||Mathf.Abs(normal.x)<.01f)return;
            bool top=false;for(int i=0;i<collision.contactCount;i++)if(Vector2.Dot(collision.GetContact(i).normal,normal)>.97f){top=true;break;}
            if(!top)return;
            // A falling body's vertical impact is otherwise converted to a lateral
            // impulse before the next ground probe. Preserve the commanded air x,
            // not that solver-generated shove, and begin tangent control now.
            body.linearVelocity=new Vector2(commandedVelocity.x,-normal.x/normal.y*commandedVelocity.x);
            body.gravityScale=0;landingControl=false;
        }
        private void Restore()
        {
            if(body!=null)body.gravityScale=gravity;
            if(shape!=null)foreach(var collider in ignored)if(collider!=null)Physics2D.IgnoreCollision(shape,collider,false);
            ignored.Clear();admitted.Clear();dropping.Clear();
            flatJoin=default;flatJoinDirection=0;landingControl=false;commandedVelocity=Vector2.zero;
            lastSlope=null;lastSlopeAt=float.NegativeInfinity;
        }
        public void ResetContactState(){Restore();}
        private void OnDisable(){Restore();}
    }
}
