using UnityEngine;

namespace RightWalkPrototype
{
    [RequireComponent(typeof(SideScrollMapPreview))]
    public sealed class RunWalkPreview : MonoBehaviour
    {
        public Sprite[] runFrames;
        public float runSpeed = 2.4f;
        public float runCycleDistance = 130f / 204.8f / .4f;
        public bool backwardRunUsesWalk;
        public bool IsRunning { get; private set; }
        private bool atRunSpeed;
        private Sprite[] walkFrames;
        private float walkDistance, walkMean, walkAmplitude, walkOffset;
        private RightFacingWalker walker;
        private RightFacingHipLink hip;
        private SideScrollMapPreview control;

        private void Awake()
        {
            control = GetComponent<SideScrollMapPreview>();
            walker = control.walker;
            hip = GetComponentInChildren<RightFacingHipLink>();
            walkFrames = walker.frames;
            walkDistance = walker.cycleDistance;
            walkMean = hip.meanDropPixels;
            walkAmplitude = hip.dropAmplitudePixels;
            walkOffset = hip.phaseOffsetRadians;
        }

        public void SelectGait(float velocity)
        {
            if (walkFrames == null || runFrames == null || runFrames.Length != walkFrames.Length) return;
            float threshold = (control.speed + runSpeed) * .5f;
            // Keep speed hysteresis independent of the displayed sheet: backward sprint
            // uses walk art, but still travels at the unchanged sprint speed.
            atRunSpeed = Mathf.Abs(velocity) > threshold + (atRunSpeed ? -.05f : .05f);
            float facingSign = transform.lossyScale.x < 0 ? -1f : 1f;
            bool backward = velocity * facingSign < -.01f;
            bool running = atRunSpeed && !(backwardRunUsesWalk && backward);
            ApplyNetworkGait(running);
        }

        public void ApplyNetworkGait(bool running)
        {
            if (running == IsRunning) return;
            IsRunning = running;
            // Both sheets share the same phase count; preserve phase through the speed change.
            walker.frames = running ? runFrames : walkFrames;
            walker.cycleDistance = running ? runCycleDistance : walkDistance;
            hip.meanDropPixels = running ? 8 : walkMean;
            hip.dropAmplitudePixels = running ? 10 : walkAmplitude;
            hip.phaseOffsetRadians = running ? -Mathf.PI / 2 : walkOffset;
        }
    }
}
