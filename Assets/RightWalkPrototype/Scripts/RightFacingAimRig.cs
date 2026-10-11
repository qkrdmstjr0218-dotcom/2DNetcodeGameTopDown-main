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
        public Camera aimCamera;
        public Transform head;
        public Vector3 headRestPosition;
        public Transform neck;
        public Transform neckSkin;
        [Range(0,1)] public float neckFollow = .2f;
        [Range(0,1)] public float headUpFollow = .9f;
        [Range(0,1)] public float headDownFollow = .45f;
        [Range(0,1)] public float headFollow = .75f;
        public bool mouseAim;
        [Range(0,45)] public float maxAngle = 45f;
        private Vector2? worldTarget;
        public bool HasAimTarget => mouseAim || worldTarget.HasValue;
        public Vector2 AimDirection => transform.TransformVector(Vector3.right).normalized;
        public Vector3 MuzzlePosition => muzzle != null ? muzzle.position : transform.position;
        public float AimAngle { get; private set; }
        public bool TargetWithinRange { get; private set; }
        public void SetAimTarget(Vector2 point) { worldTarget=point; mouseAim=false; ApplyTarget(point); }
        public void ResetAim() { worldTarget=null;mouseAim=false;AimAngle=0;transform.localRotation=Quaternion.identity; UpdatePose(); }
        private void LateUpdate()
        {
            if(mouseAim)
            {
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
            if(neck!=null)neck.localRotation=Quaternion.Euler(0,0,AimAngle*neckFollow);
            if(head!=null)head.localPosition=headRestPosition+Vector3.up*(2.5f/204.8f*Mathf.Clamp01(-AimAngle/30f));
            if(head!=null)head.localRotation=Quaternion.Euler(0,0,AimAngle*((AimAngle>=0 ? headUpFollow : headDownFollow)-(neck!=null ? neckFollow : 0f)));
            if(neckSkin!=null)neckSkin.GetComponent<RightFacingNeckSkin>().Rebuild();
            foreach(var arm in transform.parent.GetComponentsInChildren<RightFacingArmIK>())arm.Solve();
        }
        private void ApplyTarget(Vector2 point)
        {
            var facingPreview=GetComponentInParent<SideFacingPreview>();
            if(facingPreview!=null && facingPreview.turnWithAim)facingPreview.FaceTarget(point.x);
            // Solve in the unrotated parent space, including its horizontal reflection.
            var delta=(Vector2)transform.parent.InverseTransformVector(point-(Vector2)transform.position);
            float distance=delta.magnitude;
            if(distance<.001f||muzzle==null)return;
            var localMuzzle=transform.InverseTransformPoint(muzzle.position);
            // Account for the muzzle offset from the shoulder: aim the barrel line through target.
            float desired=Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg-
                Mathf.Asin(Mathf.Clamp(localMuzzle.y/distance,-1f,1f))*Mathf.Rad2Deg;
            desired=Mathf.DeltaAngle(0,desired);
            TargetWithinRange=delta.x>0 && Mathf.Abs(desired)<=maxAngle;
            AimAngle=Mathf.Clamp(desired,-maxAngle,maxAngle);
            transform.localRotation=Quaternion.Euler(0,0,AimAngle);
            UpdatePose();
        }
    }
}


