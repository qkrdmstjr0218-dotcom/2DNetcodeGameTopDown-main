using UnityEngine;
namespace RightWalkPrototype
{
    // Match the pelvis height baked into each accepted leg pose before solving aim/arms.
    [DefaultExecutionOrder(90)]
    public sealed class RightFacingHipLink : MonoBehaviour
    {
        public RightFacingWalker walker;
        public void ApplyFrame(int index)
        {
            float phase=index/20f;
            float drop=1f+Mathf.Cos(4f*Mathf.PI*phase);
            transform.localPosition=new Vector3(0,-drop/204.8f,0);
        }
        private void LateUpdate() { if(walker!=null)ApplyFrame(walker.FrameIndex); }
    }
}
