using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace RightWalkPrototype
{
    [DefaultExecutionOrder(100)]
    public sealed class RightFacingAimRig : MonoBehaviour
    {
        public Transform muzzle;
        public FrontDownAimOverlay frontDownAim;
        public Camera aimCamera;
        public Transform head;
        public Vector3 headRestPosition;
        public Transform neck;
        public Transform neckSkin;
        public Transform waistClothing;
        public SpriteRenderer supportForearm;
        public Vector2 upperBodyLeanPivot = new Vector2(-.02f,1.25f);
        [Range(0,1)] public float neckFollow = .2f;
        [Range(0,1)] public float headUpFollow = .9f;
        [Range(0,1)] public float headDownFollow = .45f;
        [Range(0,1)] public float headFollow = .75f;
        public bool mouseAim;
        [Range(0,90)] public float maxAngle = 90f;
        public float restAngle;
        [Range(0,90)] public float groundedDownAngle = 55f;
        [Range(0,12)] public float groundedDownLean = 6f;
        public Vector2 groundedDownGunOffset = new Vector2(.08f,.06f);
        [Min(.1f)] public float previewAimRange = 3f;
        public LayerMask aimHitMask = Physics2D.DefaultRaycastLayers;
        public bool AirborneDownAim { get; private set; }
        private SideScrollMapPreview movement;
        private bool hasLastAimTarget;
        private readonly RaycastHit2D[] aimHits = new RaycastHit2D[64];
        private Transform characterRoot;
        private Vector2? worldTarget;
        private bool directionTarget;
        private float recoilDistance;
        public void KickRecoil() { recoilDistance=Mathf.Min(.025f,recoilDistance+.018f); }
        private SpriteRenderer weaponRenderer;
        private SpriteRenderer headRenderer;
        private int weaponRestOrder;
        private bool renderingOrderCached;
        private bool bodyPoseCached;
        private Vector3 bodyRestPosition;
        private Quaternion bodyRestRotation;
        private Vector3 waistRestPosition;
        private Quaternion waistRestRotation;
        private bool aimPositionCached;
        private Vector3 aimRestPosition;
        private int supportForearmRestOrder;
        private bool supportOrderCached;
        private readonly System.Collections.Generic.Dictionary<SpriteRenderer,int> frontArmOrders = new System.Collections.Generic.Dictionary<SpriteRenderer,int>();
        public bool HasAimTarget => mouseAim || worldTarget.HasValue;
        public Vector2 AimDirection => frontDownAim!=null&&frontDownAim.IsActive ? frontDownAim.AimDirection : (Vector2)transform.TransformVector(Vector3.right).normalized;
        public Vector3 MuzzlePosition => frontDownAim!=null&&frontDownAim.IsActive ? frontDownAim.MuzzlePosition : muzzle != null ? muzzle.position : transform.position;
        public float AimAngle { get; private set; }
        public bool TargetWithinRange { get; private set; }
        public void SetAimTarget(Vector2 point) { directionTarget=false;worldTarget=point; mouseAim=false; ApplyTarget(point); }
        public void SetAimDirection(Vector2 direction)
        {
            if(direction.sqrMagnitude<.0001f)return;
            if(!bodyPoseCached)SetBodyLean(0f);
            if(!aimPositionCached)SetDownAimOffset(0f);
            var origin=transform.parent.parent.TransformPoint(bodyRestPosition+bodyRestRotation*aimRestPosition);
            directionTarget=true;mouseAim=false;worldTarget=(Vector2)origin+direction.normalized*1000f;
            ApplyTarget(worldTarget.Value);
        }
        public void ResetAim() { if(frontDownAim!=null)frontDownAim.Deactivate();worldTarget=null;mouseAim=false;hasLastAimTarget=false;AirborneDownAim=false;SetBodyLean(0f);SetDownAimOffset(0f);AimAngle=restAngle;transform.localRotation=Quaternion.Euler(0,0,restAngle); UpdatePose(); }
        private void SetDownAimOffset(float amount)
        {
            if(!aimPositionCached)
            {
                aimRestPosition=transform.localPosition;
                aimPositionCached=true;
            }
            float degrees=Mathf.Lerp(45f,90f,amount);
            Vector3 offset;
            if(degrees<=60f)offset=Vector3.Lerp(Vector3.zero,new Vector3(.10f,0f,0f),Mathf.SmoothStep(0f,1f,(degrees-45f)/15f));
            else if(degrees<=75f)offset=Vector3.Lerp(new Vector3(.10f,0f,0f),new Vector3(.225f,.06f,0f),Mathf.SmoothStep(0f,1f,(degrees-60f)/15f));
            else offset=Vector3.Lerp(new Vector3(.225f,.06f,0f),new Vector3(.45f,.10f,0f),Mathf.SmoothStep(0f,1f,(degrees-75f)/15f));
            transform.localPosition=aimRestPosition+offset;
        }
        private void SetBodyLean(float angle)
        {
            var body=transform.parent;
            if(!bodyPoseCached)
            {
                bodyRestPosition=body.localPosition;
                bodyRestRotation=body.localRotation;
                if(waistClothing!=null)
                {
                    waistRestPosition=waistClothing.localPosition;
                    waistRestRotation=waistClothing.localRotation;
                }
                bodyPoseCached=true;
            }
            // Rotate the connected upper body about the waist, not the sprite origin.
            var pivot=new Vector3(upperBodyLeanPivot.x,upperBodyLeanPivot.y,0f);
            body.localRotation=bodyRestRotation*Quaternion.Euler(0,0,angle);
            body.localPosition=bodyRestPosition+bodyRestRotation*pivot-body.localRotation*pivot;
            if(waistClothing!=null)
            {
                var waistPivot=bodyRestPosition+bodyRestRotation*pivot;
                var rotation=Quaternion.Euler(0,0,angle);
                waistClothing.localPosition=waistPivot+rotation*(waistRestPosition-waistPivot);
                waistClothing.localRotation=rotation*waistRestRotation;
            }
        }
        private void LateUpdate()
        {
            recoilDistance=Mathf.MoveTowards(recoilDistance,0f,Time.deltaTime*.3f);
            if(mouseAim)
            {
                directionTarget=false;
                var camera=aimCamera != null ? aimCamera : Camera.main;
                if(camera==null)return;
                Vector2 screen;
#if ENABLE_INPUT_SYSTEM
                if(Mouse.current==null)return; screen=Mouse.current.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
                screen=Input.mousePosition;
#else
                return;
#endif
                var ray=camera.ScreenPointToRay(screen);
                float hit;
                if(new Plane(Vector3.forward,transform.position).Raycast(ray,out hit))ApplyTarget(ray.GetPoint(hit));
            }
            else if(worldTarget.HasValue)ApplyTarget(worldTarget.Value);
        }
        private void UpdatePose()
        {
            if(!renderingOrderCached)
            {
                weaponRenderer=GetComponentInChildren<SpriteRenderer>();
                if(head!=null)
                    foreach(var renderer in head.GetComponentsInChildren<SpriteRenderer>())
                        if(headRenderer==null || renderer.sortingOrder>headRenderer.sortingOrder)
                            headRenderer=renderer;
                if(weaponRenderer!=null)weaponRestOrder=weaponRenderer.sortingOrder;
                frontArmOrders.Clear();
                foreach(var arm in transform.parent.GetComponentsInChildren<RightFacingArmIK>())
                {
                    foreach(var part in new[]{arm.upperArm,arm.forearm})
                    {
                        if(part==null)continue;
                        foreach(var renderer in part.GetComponentsInChildren<SpriteRenderer>())
                            if(renderer.sortingOrder>weaponRestOrder)
                                frontArmOrders[renderer]=renderer.sortingOrder;
                    }
                }
                renderingOrderCached=true;
            }
            bool aimingUp=AimAngle-restAngle>50f;
            if(supportForearm!=null)
            {
                if(!supportOrderCached){supportForearmRestOrder=supportForearm.sortingOrder;supportOrderCached=true;}
                supportForearm.sortingOrder=AirborneDownAim ? 15 : supportForearmRestOrder;
            }
            if(weaponRenderer!=null && headRenderer!=null)
                weaponRenderer.sortingOrder=aimingUp
                    ? Mathf.Max(weaponRestOrder,headRenderer.sortingOrder+1) : weaponRestOrder;
            foreach(var part in frontArmOrders)
                if(part.Key!=null)part.Key.sortingOrder=aimingUp && weaponRenderer!=null
                    ? weaponRenderer.sortingOrder+part.Value-weaponRestOrder : part.Value;
            // Keep the established head/neck pose while the arms aim vertically.
            float relativeAngle=Mathf.Clamp(AimAngle-restAngle,-45f,45f);
            if(neck!=null)neck.localRotation=Quaternion.Euler(0,0,relativeAngle*neckFollow);
            if(head!=null)head.localPosition=headRestPosition+Vector3.up*(2.5f/204.8f*Mathf.Clamp01(-relativeAngle/30f));
            if(head!=null)head.localRotation=Quaternion.Euler(0,0,relativeAngle*((relativeAngle>=0 ? headUpFollow : headDownFollow)-(neck!=null ? neckFollow : 0f)));
            if(neckSkin!=null)neckSkin.GetComponent<RightFacingNeckSkin>().Rebuild();
            foreach(var arm in transform.parent.GetComponentsInChildren<RightFacingArmIK>())arm.Solve();
        }
        private void ApplyTarget(Vector2 point)
        {
            hasLastAimTarget=true;
            if(movement==null)movement=GetComponentInParent<SideScrollMapPreview>();
            bool airborne=movement!=null && !movement.Grounded;
            var facingPreview=GetComponentInParent<SideFacingPreview>();
            if(!bodyPoseCached)SetBodyLean(0f);
            if(!aimPositionCached)SetDownAimOffset(0f);
            if(facingPreview!=null && facingPreview.turnWithAim)
            {
                // Use the unshifted shoulder line so pose offsets cannot delay the turn.
                var origin=transform.parent.parent.TransformPoint(new Vector3(0f,bodyRestPosition.y+aimRestPosition.y,0f));
                facingPreview.FaceAimTarget(point.x,origin.x);
            }
            var restingOrigin=transform.parent.parent.TransformPoint(bodyRestPosition+bodyRestRotation*aimRestPosition);
            var bodyDirection=transform.parent.parent.InverseTransformVector(point-(Vector2)restingOrigin);
            float bodyAngle=Mathf.Atan2(bodyDirection.y,bodyDirection.x)*Mathf.Rad2Deg;
            if(frontDownAim!=null&&frontDownAim.TryAim(point))
            {
                SetBodyLean(0f);SetDownAimOffset(0f);AimAngle=frontDownAim.LocalAimDegrees;
                TargetWithinRange=frontDownAim.TargetWithinRange;AirborneDownAim=airborne;
                transform.localRotation=Quaternion.Euler(0,0,AimAngle);return;
            }
            float lean=8f*Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(55f,90f,bodyAngle));
            if(movement!=null && !airborne)
                lean-=groundedDownLean*Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(35f,groundedDownAngle,-bodyAngle));
            SetBodyLean(lean);
            SetDownAimOffset(airborne ? Mathf.InverseLerp(45f,90f,-bodyAngle) : 0f);
            if(movement!=null && !airborne)
            {
                float downBlend=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(35f,groundedDownAngle,-bodyAngle));
                transform.localPosition+=new Vector3(groundedDownGunOffset.x,groundedDownGunOffset.y,0f)*downBlend;
            }
            // Solve in the unrotated parent space, including its horizontal reflection.
            transform.localPosition-=transform.localRotation*Vector3.right*recoilDistance;
            var delta=(Vector2)transform.parent.InverseTransformVector(point-(Vector2)transform.position);
            float distance=delta.magnitude;
            if(distance<.001f||muzzle==null)return;
            var localMuzzle=transform.InverseTransformPoint(muzzle.position);
            // Account for the muzzle offset from the shoulder: aim the barrel line through target.
            float desired=Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg-
                Mathf.Asin(Mathf.Clamp(localMuzzle.y/distance,-1f,1f))*Mathf.Rad2Deg;
            desired=Mathf.DeltaAngle(0,desired);
            if(directionTarget)desired=bodyAngle-lean;
            else if(airborne)
            {
                // Deep air aim follows the requested direction, not the translated gun pivot.
                float verticalBlend=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(55f,85f,-bodyAngle));
                desired=Mathf.LerpAngle(desired,bodyAngle-lean,verticalBlend);
            }
            // The ground limit is the barrel's angle, not its angle relative to the bent torso.
            float downLimit=movement!=null && !airborne ? Mathf.Min(maxAngle,groundedDownAngle)+Mathf.Min(0f,lean) : maxAngle;
            TargetWithinRange=(airborne || directionTarget || delta.x>=-.001f) && desired>=restAngle-downLimit-.01f && desired<=restAngle+maxAngle+.01f;
            AimAngle=Mathf.Clamp(desired,restAngle-downLimit,restAngle+maxAngle);
            AirborneDownAim=airborne && AimAngle<restAngle-groundedDownAngle;
            transform.localRotation=Quaternion.Euler(0,0,AimAngle);
            UpdatePose();
        }
        private void OnGUI()
        {
            if(!HasAimTarget || !hasLastAimTarget)return;
            var camera=aimCamera!=null ? aimCamera : Camera.main;
            if(camera==null)return;
            if(characterRoot==null)
            {
                var walker=GetComponentInParent<RightFacingWalker>();
                characterRoot=walker!=null ? walker.transform : transform.root;
            }
            var origin=(Vector2)MuzzlePosition;
            var direction=AimDirection;
            float range=Mathf.Max(.1f,previewAimRange);
            var point=TraceShot(origin,direction,range,out bool hitSomething);
            var end=camera.WorldToScreenPoint(point);
            if(end.z<=0)return;
            var previous=GUI.color;
            GUI.color=TargetWithinRange ? Color.white : new Color(1f,.75f,.15f);
            var to=new Vector2(end.x,Screen.height-end.y);
            DrawAimLine(to+new Vector2(-5,-5),to+new Vector2(5,5),2f);
            DrawAimLine(to+new Vector2(-5,5),to+new Vector2(5,-5),2f);
            GUI.color=previous;
        }
        public Vector2 TraceShot(Vector2 origin, Vector2 direction, float range, out bool hitSomething)
        {
            var point=TraceShot(origin,direction,range,out Collider2D collider);
            hitSomething=collider!=null;
            return point;
        }
        public Vector2 TraceShot(Vector2 origin, Vector2 direction, float range, out Collider2D hitCollider)
        {
            if(characterRoot==null)
            {
                var walker=GetComponentInParent<RightFacingWalker>();
                characterRoot=walker!=null ? walker.transform : transform.root;
            }
            direction=direction.normalized;
            var point=origin+direction*range;
            hitCollider=null;
            var filter=new ContactFilter2D();
            filter.SetLayerMask(aimHitMask);
            filter.useTriggers=false;
            int count=Physics2D.Raycast(origin,direction,filter,aimHits,range);
            float nearest=range;
            foreach(var hit in new System.ArraySegment<RaycastHit2D>(aimHits,0,count))
            {
                if(hit.collider==null || hit.collider.transform.IsChildOf(characterRoot))continue;
                if(hit.distance<=nearest){nearest=hit.distance;point=hit.point;hitCollider=hit.collider;}
            }
            return point;
        }
        private static void DrawAimLine(Vector2 from, Vector2 to, float thickness)
        {
            var delta=to-from;
            if(delta.sqrMagnitude<.0001f)return;
            var matrix=GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,from);
            GUI.DrawTexture(new Rect(from.x,from.y-thickness*.5f,delta.magnitude,thickness),Texture2D.whiteTexture);
            GUI.matrix=matrix;
        }
    }
}


