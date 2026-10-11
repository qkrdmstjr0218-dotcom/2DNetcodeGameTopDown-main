using UnityEngine;
namespace RightWalkPrototype
{
    [DefaultExecutionOrder(110)]
    public sealed class RightFacingArmIK : MonoBehaviour
    {
        public Transform upperArm, forearm, hand, grip;
        public Vector2 shoulder, referenceElbow, referenceWrist;
        // Offset from the hand sprite's wrist pivot to its visible grip contact.
        public Vector2 handContactOffset;
        public bool Reachable { get; private set; }
        public float GripError { get; private set; }
        private void LateUpdate() { Solve(); }
        public void Solve()
        {
            if(grip==null || upperArm==null || forearm==null || hand==null)return;
            Vector3 wristTarget=grip.position-grip.TransformVector(handContactOffset);
            Vector2 target=transform.InverseTransformPoint(wristTarget);
            float first=Vector2.Distance(shoulder,referenceElbow);
            float second=Vector2.Distance(referenceElbow,referenceWrist);
            Vector2 delta=target-shoulder;float distance=delta.magnitude;
            if(distance<.00001f || first<.00001f || second<.00001f)return;
            Reachable=distance<=first+second && distance>=Mathf.Abs(first-second);
            float solvedDistance=Mathf.Clamp(distance,Mathf.Abs(first-second)+.00001f,first+second-.00001f);
            Vector2 direction=delta/distance;
            float along=(first*first-second*second+solvedDistance*solvedDistance)/(2*solvedDistance);
            float height=Mathf.Sqrt(Mathf.Max(0,first*first-along*along));
            Vector2 restTarget=referenceWrist-shoulder,restElbow=referenceElbow-shoulder;
            float bend=Mathf.Sign(restTarget.x*restElbow.y-restTarget.y*restElbow.x);
            Vector2 elbow=shoulder+direction*along+new Vector2(-direction.y,direction.x)*height*bend;
            Vector2 wrist=shoulder+direction*solvedDistance;
            upperArm.localPosition=shoulder;
            upperArm.localRotation=Quaternion.Euler(0,0,Vector2.SignedAngle(referenceElbow-shoulder,elbow-shoulder));
            forearm.localPosition=elbow;
            forearm.localRotation=Quaternion.Euler(0,0,Vector2.SignedAngle(referenceWrist-referenceElbow,wrist-elbow));
            hand.localPosition=wrist;
            hand.rotation=grip.rotation;
            GripError=Vector2.Distance(hand.TransformPoint(handContactOffset),grip.position);
        }
    }
}
