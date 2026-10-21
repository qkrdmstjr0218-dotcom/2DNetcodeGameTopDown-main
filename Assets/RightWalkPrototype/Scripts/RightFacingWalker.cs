using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RightWalkPrototype
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class RightFacingWalker : MonoBehaviour
    {
        public Sprite[] frames;
        [Tooltip("Optional relative time per frame. Empty uses equal time.")]
        public float[] frameWeights;
        [Min(.01f)] public float moveSpeed = 1.8f;
        [Tooltip("World distance per full gait cycle. Match sprite support-foot travel divided by stance fraction.")]
        [Min(.01f)] public float cycleDistance = 100f / 204.8f / .6f;
        [Min(.01f)] public float acceleration = 14f;
        [Tooltip("Time to ease the gait into the nearest two-foot planted pose after movement stops.")]
        [Min(.05f)] public float stopTransitionDuration = .24f;
        public bool readKeyboard = true;
        public bool autoPreview;
        public bool externalMovement;
        public int FrameIndex { get; private set; }
        public Vector2 Velocity { get; private set; }
        private SpriteRenderer art;
        private Vector2 requestedInput;
        private float phase;
        private bool settling;
        private float settleStart, settleDelta, settleElapsed;
        public float CyclePhase => phase;
        public bool IsSettling => settling;
        public bool HoldCycle { get; set; }
        public void ApplyNetworkFrame(int index, Vector2 velocity)
        {
            Velocity=velocity;
            if(frames!=null&&frames.Length>0)ShowFrame(Mathf.Clamp(index,0,frames.Length-1));
        }
        public void ResumeFromPlantedPose(int index)
        {
            phase = PlantedPhase(index);
            settling = false;
            HoldCycle = false;
            ShowFrame(index);
        }

        private void Awake()
        {
            art = GetComponent<SpriteRenderer>();
            ShowFrame(0);
        }

        // Allows a different input controller to drive this standalone prototype.
        public void SetMoveInput(Vector2 input)
        {
            readKeyboard = false;
            requestedInput = Vector2.ClampMagnitude(input, 1);
        }

        private Vector2 KeyboardInput()
        {
#if ENABLE_INPUT_SYSTEM
            var keys = Keyboard.current;
            if (keys == null) return Vector2.zero;
            return new Vector2(
                (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) -
                (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0),
                (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) -
                (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0));
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
            return Vector2.zero;
#endif
        }

        private void Update()
        {
            if (externalMovement) return;
            var input = Vector2.ClampMagnitude(readKeyboard ? KeyboardInput() : requestedInput, 1);
            StepMovement(input, Time.deltaTime);
        }

        private void StepMovement(Vector2 input, float deltaTime)
        {
            if (deltaTime <= 0) return;
            Velocity = Vector2.MoveTowards(Velocity, input * moveSpeed, acceleration * deltaTime);
            var facingPreview = GetComponent<SideFacingPreview>();
            if (facingPreview != null) facingPreview.FollowMovement(Velocity);
            var displacement = Velocity * deltaTime;
            transform.position += (Vector3)displacement;
            AdvancePose(displacement, deltaTime);
        }

        public void DriveAnimation(Vector2 displacement, Vector2 velocity, float deltaTime)
        {
            Velocity = velocity;
            var facingPreview = GetComponent<SideFacingPreview>();
            if (facingPreview != null) facingPreview.FollowMovement(velocity);
            AdvancePose(displacement, deltaTime);
        }

        private void AdvancePose(Vector2 displacement, float deltaTime)
        {
            if (HoldCycle) return;
            float distance = displacement.magnitude;
            // Preview animates in place. Real movement advances by actual distance travelled.
            if (Velocity.sqrMagnitude > .00000001f || autoPreview)
            {
                settling = false;
                float facingSign = transform.lossyScale.x < 0 ? -1 : 1;
                float sign = Velocity.x * facingSign < -.01f ? -1 : 1;
                float travel = distance > 0 ? distance : autoPreview ? moveSpeed * deltaTime : 0;
                phase = Mathf.Repeat(phase + sign * travel / Mathf.Max(.01f, cycleDistance), 1);
                if (frames != null && frames.Length > 0)
                    ShowFrame(FrameForPhase(phase));
            }
            else
            {
                if (frames == null || frames.Length == 0) return;
                if (!settling)
                {
                    // These poses have both boots on the ground in the accepted sheet.
                    // Choose by cyclic distance, including the wrap from the last frame.
                    float first = PlantedPhase(0);
                    float second = PlantedPhase(frames.Length / 2);
                    float toFirst = PhaseDelta(phase, first);
                    float toSecond = PhaseDelta(phase, second);
                    settleStart = phase;
                    settleDelta = Mathf.Abs(toFirst) <= Mathf.Abs(toSecond) ? toFirst : toSecond;
                    settleElapsed = 0;
                    settling = Mathf.Abs(settleDelta) > .00001f;
                }
                if (settling)
                {
                    settleElapsed += deltaTime;
                    float t = Mathf.Clamp01(settleElapsed / Mathf.Max(.05f, stopTransitionDuration));
                    phase = Mathf.Repeat(settleStart + settleDelta * Mathf.SmoothStep(0, 1, t), 1);
                    if (t >= 1) settling = false;
                }
                ShowFrame(FrameForPhase(phase));
            }
        }

        private static float PhaseDelta(float from, float to)
        {
            return Mathf.Repeat(to - from + .5f, 1) - .5f;
        }

        private float PlantedPhase(int index)
        {
            if (frameWeights == null || frameWeights.Length != frames.Length)
                return (index + .5f) / frames.Length;
            float total = 0, start = 0;
            for (int i = 0; i < frameWeights.Length; i++)
            {
                float weight = Mathf.Max(.0001f, frameWeights[i]);
                total += weight;
                if (i < index) start += weight;
            }
            return (start + .5f * Mathf.Max(.0001f, frameWeights[index])) / total;
        }

        private void ShowFrame(int index)
        {
            FrameIndex = index;
            if (art != null && frames != null && index < frames.Length) art.sprite = frames[index];
        }

        private int FrameForPhase(float value)
        {
            if (frameWeights == null || frameWeights.Length != frames.Length)
                return Mathf.Min(frames.Length - 1, Mathf.FloorToInt(Mathf.Repeat(value, 1) * frames.Length));
            float total = 0;
            for (int i = 0; i < frameWeights.Length; i++) total += Mathf.Max(.0001f, frameWeights[i]);
            float target = Mathf.Repeat(value, 1) * total;
            float end = 0;
            for (int i = 0; i < frameWeights.Length; i++)
            {
                end += Mathf.Max(.0001f, frameWeights[i]);
                if (target < end) return i;
            }
            return frames.Length - 1;
        }
    }
}
