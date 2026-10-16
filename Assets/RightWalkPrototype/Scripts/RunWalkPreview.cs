using UnityEngine;

namespace RightWalkPrototype
{
    [RequireComponent(typeof(SideScrollMapPreview))]
    public sealed class RunWalkPreview : MonoBehaviour
    {
        public Sprite[] runFrames;
        public float runSpeed = 2.4f;
        public float runCycleDistance = 130f / 204.8f / .4f;
        public bool IsRunning { get; private set; }
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
            bool running = Mathf.Abs(velocity) > threshold + (IsRunning ? -.05f : .05f);
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
