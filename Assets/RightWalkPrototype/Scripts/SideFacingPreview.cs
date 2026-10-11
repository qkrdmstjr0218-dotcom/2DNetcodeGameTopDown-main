using UnityEngine;
namespace RightWalkPrototype
{
    // Added only to the separate left/right experiment scene.
    public sealed class SideFacingPreview : MonoBehaviour
    {
        public bool turnWithMovement = true;
        public bool turnWithAim = true;
        public int FacingSign => transform.localScale.x < 0 ? -1 : 1;
        public void SetFacing(int sign)
        {
            var scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (sign < 0 ? -1 : 1);
            transform.localScale = scale;
        }
        public void FaceTarget(float worldX)
        {
            float delta = worldX - transform.position.x;
            if (Mathf.Abs(delta) > .04f) SetFacing(delta < 0 ? -1 : 1);
        }
        public void FollowMovement(Vector2 velocity)
        {
            var aim = GetComponentInChildren<RightFacingAimRig>();
            if (!turnWithMovement || (aim != null && aim.HasAimTarget && turnWithAim)) return;
            if (Mathf.Abs(velocity.x) > .01f) SetFacing(velocity.x < 0 ? -1 : 1);
        }
    }
}
