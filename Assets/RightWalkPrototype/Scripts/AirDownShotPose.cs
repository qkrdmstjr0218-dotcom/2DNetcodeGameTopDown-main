using UnityEngine;

namespace RightWalkPrototype
{
    [DefaultExecutionOrder(85), RequireComponent(typeof(RightFacingWalker))]
    public sealed class AirDownShotPose : MonoBehaviour
    {
        public Sprite tuckedLegs;
        private SideScrollMapPreview movement;
        private RightFacingWalker walker;
        private RightFacingHipLink hip;
        private SpriteRenderer legs;
        private bool active;
        private JumpLegTransitionRig transition;
        private RunWalkPreview gait;
        public int TakeoffFrame => transition!=null ? transition.TakeoffFrame : 0;
        public bool TakeoffRunning => transition!=null&&transition.TakeoffRunning;
        private void Awake()
        {
            walker=GetComponent<RightFacingWalker>();legs=GetComponent<SpriteRenderer>();
            hip=GetComponentInChildren<RightFacingHipLink>();movement=GetComponent<SideScrollMapPreview>();
            gait=GetComponent<RunWalkPreview>();
            transition=new JumpLegTransitionRig(transform,legs);
        }
        private void LateUpdate() { if(movement!=null&&movement.enabled)ApplyPose(); }
        public void ApplyPose()
        {
            ApplyPose(walker!=null ? walker.FrameIndex : 0,gait!=null&&gait.IsRunning);
        }
        public void ApplyNetworkPose(int frame, bool running, int takeoffFrame, bool takeoffRunning)
        { ApplyPose(frame,running,takeoffFrame,takeoffRunning); }
        private void ApplyPose(int frame, bool running, int takeoffFrame=-1, bool takeoffRunning=false)
        {
            if(movement==null||hip==null||legs==null||walker==null)return;
            if(transition!=null&&transition.Apply(movement.Grounded,frame,running,takeoffFrame,takeoffRunning))
            {
                active=true;legs.sprite=transition.Waist;
                hip.OverrideDropPixels=transition.DropPixels;hip.ApplyFrame(frame);
                GetComponent<RightFacingPelvisPreview>()?.UpdateGeometry();
                return;
            }
            if(!movement.Grounded&&tuckedLegs!=null)
            {
                active=true;legs.sprite=transition!=null&&transition.UseAlternatePose&&transition.AlternateAirPose!=null
                    ? transition.AlternateAirPose : tuckedLegs;
                // Both airborne phases share the fixed pelvis registration of the new tile.
                hip.OverrideDropPixels=4f;hip.ApplyFrame(walker.FrameIndex);
                GetComponent<RightFacingPelvisPreview>()?.UpdateGeometry();
            }
            else if(active)
            {
                active=false;hip.OverrideDropPixels=null;
                // Remote gait frames have already been selected by the network snapshot.
                if(walker.enabled&&walker.frames!=null&&walker.frames.Length>0)
                    legs.sprite=walker.frames[Mathf.Clamp(walker.FrameIndex,0,walker.frames.Length-1)];
                hip.ApplyFrame(walker.FrameIndex);
                GetComponent<RightFacingPelvisPreview>()?.UpdateGeometry();
            }
        }
        private void OnDisable(){if(hip!=null)hip.OverrideDropPixels=null;transition?.Hide();}
        private void OnDestroy(){transition?.Dispose();}
    }
}
