using UnityEngine;

namespace RightWalkPrototype
{
    // Reference poses guide ankle trajectories; fixed-length IK prevents rubber legs.
    [RequireComponent(typeof(RightFacingWalker))]
    public sealed class ReferenceLegWalker : MonoBehaviour
    {
        public Vector2[] joints = new Vector2[64]; // pose, leg, hip/knee/ankle/toe
        public Material material;
        public bool smooth = true;
        public bool pinSupportFoot = true;
        public float pixelScale = .035f;
        private readonly LineRenderer[] parts = new LineRenderer[6];
        private readonly float[] thigh = new float[2], calf = new float[2], foot = new float[2];
        private readonly Vector3[] anchors = new Vector3[2];
        private readonly bool[] planted = new bool[2];
        private RightFacingWalker walker;
        public float MaxReachError { get; private set; }
        private Vector2 P(int pose, int leg, int joint) => joints[pose * 8 + leg * 4 + joint];
        private Vector2 Local(Vector2 p) => new Vector2((p.x - 30) * pixelScale, (102 - p.y) * pixelScale);
        private void Awake()
        {
            walker = GetComponent<RightFacingWalker>();
            GetComponent<SpriteRenderer>().enabled = false;
            for (int leg = 0; leg < 2; leg++)
            {
                for (int pose = 0; pose < 8; pose++)
                {
                    thigh[leg] += Vector2.Distance(P(pose, leg, 0), P(pose, leg, 1)) * pixelScale / 8;
                    calf[leg] += Vector2.Distance(P(pose, leg, 1), P(pose, leg, 2)) * pixelScale / 8;
                    foot[leg] += Vector2.Distance(P(pose, leg, 2), P(pose, leg, 3)) * pixelScale / 8;
                }
                for (int segment = 0; segment < 3; segment++)
                {
                    var line = new GameObject((leg == 0 ? "A" : "B") + " " + new[] { "Thigh", "Calf", "Foot" }[segment]).AddComponent<LineRenderer>();
                    line.transform.SetParent(transform, false);
                    line.useWorldSpace = false;
                    line.sharedMaterial = material;
                    line.positionCount = 2;
                    line.numCapVertices = 8;
                    line.startWidth = line.endWidth = segment == 0 ? .12f : segment == 1 ? .09f : .075f;
                    line.startColor = line.endColor = leg == 0 ? new Color(.25f,.65f,1) : new Color(1,.5f,.2f);
                    line.sortingOrder = leg == 0 ? 12 : 10;
                    parts[leg * 3 + segment] = line;
                }
            }
        }
        private void LateUpdate()
        {
            if (joints.Length != 64) return;
            float phase = walker.CyclePhase * 8;
            int i = Mathf.FloorToInt(phase) % 8, next = (i + 1) % 8;
            float t = smooth ? phase - Mathf.Floor(phase) : 0;
            MaxReachError = 0;
            for (int leg = 0; leg < 2; leg++)
            {
                var hip = Local(Vector2.Lerp(P(i,leg,0),P(next,leg,0),t));
                var ankle = Local(Vector2.Lerp(P(i,leg,2),P(next,leg,2),t));
                // A supports the first half, B the second. Pinning is a study approximation.
                bool support = leg == 0 ? walker.CyclePhase < .5f : walker.CyclePhase >= .5f;
                bool pin = pinSupportFoot && !walker.autoPreview && walker.Velocity.sqrMagnitude > .0001f && support;
                if (pin && !planted[leg]) anchors[leg] = transform.TransformPoint(ankle);
                if (pin) ankle = transform.InverseTransformPoint(anchors[leg]);
                planted[leg] = pin;
                var delta = ankle - hip;
                float requested = delta.magnitude;
                float distance = Mathf.Clamp(requested, Mathf.Abs(thigh[leg]-calf[leg])+.001f, thigh[leg]+calf[leg]-.001f);
                MaxReachError = Mathf.Max(MaxReachError, Mathf.Abs(requested-distance));
                var dir = requested > .0001f ? delta/requested : Vector2.down;
                ankle = hip + dir*distance;
                float along = (thigh[leg]*thigh[leg]-calf[leg]*calf[leg]+distance*distance)/(2*distance);
                float bend = Mathf.Sqrt(Mathf.Max(0,thigh[leg]*thigh[leg]-along*along));
                var knee = hip+dir*along+new Vector2(-dir.y,dir.x)*bend;
                float a0 = FootAngle(i,leg), a1 = FootAngle(next,leg);
                float angle = Mathf.LerpAngle(a0,a1,t)*Mathf.Deg2Rad;
                var toe = ankle + new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*foot[leg];
                Set(leg*3,hip,knee);Set(leg*3+1,knee,ankle);Set(leg*3+2,ankle,toe);
            }
        }
        private float FootAngle(int pose,int leg)
        {
            var d=Local(P(pose,leg,3))-Local(P(pose,leg,2));
            return Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;
        }
        private void Set(int index,Vector2 a,Vector2 b)
        { parts[index].SetPosition(0,a); parts[index].SetPosition(1,b); }
    }
}
