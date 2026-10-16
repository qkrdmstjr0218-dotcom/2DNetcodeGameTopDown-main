using UnityEngine;
namespace RightWalkPrototype
{
    // Match the pelvis height baked into each accepted leg pose before solving aim/arms.
    [DefaultExecutionOrder(90)]
    public sealed class RightFacingHipLink : MonoBehaviour
    {
        public RightFacingWalker walker;
        public float meanDropPixels = 1f;
        public float dropAmplitudePixels = 1f;
        public float phaseOffsetRadians;
        public float? OverrideDropPixels { get; set; }
        public void ApplyFrame(int index)
        {
            int count=walker != null && walker.frames != null && walker.frames.Length > 0 ? walker.frames.Length : 20;
            float phase=index/(float)count;
            float drop=OverrideDropPixels ?? (meanDropPixels+dropAmplitudePixels*Mathf.Cos(4f*Mathf.PI*phase+phaseOffsetRadians));
            transform.localPosition=new Vector3(0,-drop/204.8f,0);
        }
        private void LateUpdate() { if(walker!=null)ApplyFrame(walker.FrameIndex); }
    }
}
