using UnityEngine;

namespace RightWalkPrototype
{
    [DefaultExecutionOrder(50), RequireComponent(typeof(RightFacingWalker))]
    public sealed class ShootingStanceTrial : MonoBehaviour
    {
        public Sprite[] firstPlantedTransition;
        public Sprite[] secondPlantedTransition;
        public float transitionDuration = .4f;
        public bool UseStance;
        public float StanceProgress { get; private set; }
        private RightFacingWalker walker;
        private RightFacingHipLink hip;
        private SpriteRenderer art;
        private bool active;
        private int planted;

        private void Awake()
        {
            walker = GetComponent<RightFacingWalker>();
            hip = GetComponentInChildren<RightFacingHipLink>();
            art = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            bool moving = walker.Velocity.sqrMagnitude > .00000001f || walker.autoPreview;
            // Physical movement must never wait for the standing animation to rewind.
            if (active && (moving || !UseStance))
            {
                active = false;
                StanceProgress = 0;
                hip.OverrideDropPixels = null;
                walker.ResumeFromPlantedPose(planted);
                return;
            }
            if (!active)
            {
                if (!UseStance || moving || walker.IsSettling) return;
                planted = walker.FrameIndex < walker.frames.Length / 4 || walker.FrameIndex >= walker.frames.Length * 3 / 4
                    ? 0 : walker.frames.Length / 2;
                var sequence = planted == 0 ? firstPlantedTransition : secondPlantedTransition;
                if (sequence == null || sequence.Length < 2) return;
                active = true;
                walker.HoldCycle = true;
            }
            StanceProgress = Mathf.MoveTowards(StanceProgress, UseStance && !moving ? 1 : 0,
                Time.deltaTime / Mathf.Max(.05f, transitionDuration));
            var frames = planted == 0 ? firstPlantedTransition : secondPlantedTransition;
            int index = Mathf.RoundToInt(StanceProgress * (frames.Length - 1));
            art.sprite = frames[index];
            float progress = index / (float)(frames.Length - 1);
            hip.OverrideDropPixels = Mathf.Lerp(10, 4, Mathf.SmoothStep(0, 1, Mathf.Clamp01((progress - .5f) * 2)));
            if (StanceProgress == 0 && (moving || !UseStance))
            {
                active = false;
                hip.OverrideDropPixels = null;
                walker.ResumeFromPlantedPose(planted);
            }
        }

        private void OnDisable()
        {
            if (walker != null) walker.HoldCycle = false;
            if (hip != null) hip.OverrideDropPixels = null;
            active = false;
            StanceProgress = 0;
        }
    }
}
