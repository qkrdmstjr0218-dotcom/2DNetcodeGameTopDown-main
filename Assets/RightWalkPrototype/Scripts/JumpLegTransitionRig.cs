using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace RightWalkPrototype
{
    // Original cutout pieces are visible only during takeoff/landing transitions.
    public sealed class JumpLegTransitionRig
    {
        [Serializable] public class Leg { public float[] hip, knee, ankle, boot; public float bootAngle, contactY; }
        [Serializable] public class Pose { public float hipY; public Leg far, near; }
        [Serializable] public class Angles { public float[] far, near; }
        [Serializable] public class Data { public Pose[] walk, run; public Pose source; public Angles alternateAngles, alternateBootOffsets; }
        private struct State { public Vector2 hip, bootOffset; public float thigh, shin, boot; }
        private readonly Data data;
        private readonly Transform root;
        private readonly SpriteRenderer[] renderers = new SpriteRenderer[8];
        private readonly Sprite[] sprites = new Sprite[8];
        private readonly State[] start = new State[2], target = new State[2], current = new State[2];
        private readonly float[] upperLength = new float[2], lowerLength = new float[2];
        private bool wasAirborne, initialized, transitioning;
        private bool wasSecondJump, firstAlternatePose;
        private float began, duration;
        public Sprite Waist { get; private set; }
        public Sprite AlternateAirPose { get; private set; }
        public bool UseAlternatePose { get; private set; }
        public int TakeoffFrame { get; private set; }
        public bool TakeoffRunning { get; private set; }
        public float DropPixels { get; private set; }
        public bool Ready
        {
            get
            {
                if(data==null||Waist==null||data.walk==null||data.run==null)return false;
                foreach(var sprite in sprites)if(sprite==null)return false;
                return true;
            }
        }
        private const float Ppu = 204.8f;

        public JumpLegTransitionRig(Transform owner, SpriteRenderer source)
        {
            var asset = Resources.Load<TextAsset>("JumpTransition/Joints");
            if(asset == null) return;
            data = JsonUtility.FromJson<Data>(asset.text);
            var obj = new GameObject("Jump transition original leg parts");
            root = obj.transform; root.SetParent(owner, false);
            var group = obj.AddComponent<SortingGroup>();
            group.sortingLayerID = source.sortingLayerID; group.sortingOrder = source.sortingOrder;
            for(int side = 0; side < 2; side++)
            {
                Leg leg = side == 0 ? data.source.far : data.source.near;
                upperLength[side] = Vector2.Distance(V(leg.hip), V(leg.knee));
                lowerLength[side] = Vector2.Distance(V(leg.knee), V(leg.ankle));
                string prefix = side == 0 ? "far" : "near";
                string[] names = { prefix+"Thigh", prefix+"Shin", "Boot", side==0 ? "FarKnee" : "Knee" };
                Vector2[] pivots = { V(leg.hip), V(leg.knee), V(data.source.near.ankle), V(data.source.near.knee) };
                for(int part = 0; part < 4; part++)
                {
                    int index = side*4+part;
                    var child = new GameObject(prefix+" "+names[part]); child.transform.SetParent(root, false);
                    var r = child.AddComponent<SpriteRenderer>(); renderers[index] = r;
                    sprites[index] = MakeSprite(names[part], pivots[part]); r.sprite = sprites[index];
                    r.sharedMaterial = source.sharedMaterial; r.sortingOrder = index;
                }
            }
            Waist = MakeSprite("Waist", new Vector2(256, 499.2f));
            AlternateAirPose = MakeSprite("AlternateAir", new Vector2(256, 499.2f));
            SetVisible(false);
        }
        private static Vector2 V(float[] v) => new Vector2(v[0], v[1]);
        private static float Angle(Vector2 v) => Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg;
        private static Vector2 Direction(float angle) => new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
        private Sprite MakeSprite(string name, Vector2 pivot)
        {
            var texture = Resources.Load<Texture2D>("JumpTransition/"+name);
            return texture == null ? null : Sprite.Create(texture, new Rect(0,0,texture.width,texture.height),
                new Vector2(pivot.x/512f,1-pivot.y/512f),Ppu,0,SpriteMeshType.FullRect);
        }
        private State Gait(Pose pose, int side)
        {
            Leg leg = side == 0 ? pose.far : pose.near;
            return new State { hip = V(leg.hip), thigh = Angle(V(leg.knee)-V(leg.hip)),
                shin = Angle(V(leg.ankle)-V(leg.knee)), boot = leg.bootAngle,
                bootOffset = V(leg.boot)-V(leg.ankle) };
        }
        public bool Apply(bool grounded, int frame, bool running, int takeoffFrame=-1, bool takeoffRunning=false, bool secondJump=false)
        {
            if(!Ready) return false;
            Pose[] frames = running ? data.run : data.walk;
            Pose gait = frames[Mathf.Clamp(frame,0,frames.Length-1)];
            bool airborne = !grounded;
            if(!initialized) { initialized = true; wasAirborne = false; }
            if(airborne != wasAirborne || airborne&&secondJump!=wasSecondJump)
            {
                if(airborne&&!wasAirborne)
                {
                    TakeoffFrame=takeoffFrame>=0 ? takeoffFrame : frame;
                    TakeoffRunning=takeoffFrame>=0 ? takeoffRunning : running;
                    var startFrames=TakeoffRunning ? data.run : data.walk;
                    gait=startFrames[Mathf.Clamp(TakeoffFrame,0,startFrames.Length-1)];
                }
                // Image Y increases downwards: the lower sole is the support candidate.
                // Resolve two-foot contact deterministically using the rear foot.
                bool nearSupport = Mathf.Abs(gait.near.contactY-gait.far.contactY)>2f
                    ? gait.near.contactY>gait.far.contactY
                    : gait.near.ankle[0]<gait.far.ankle[0];
                if(airborne)
                {
                    if(!wasAirborne)firstAlternatePose=nearSupport;
                    nearSupport=firstAlternatePose^secondJump;
                    UseAlternatePose=nearSupport;
                }
                bool nearFolded = !nearSupport;
                for(int side = 0; side < 2; side++)
                {
                    start[side] = transitioning ? current[side] : wasAirborne ? target[side] : Gait(gait,side);
                    bool folded = (side == 1) == nearFolded;
                    float[] alternate = nearSupport && data.alternateAngles!=null
                        ? side==0 ? data.alternateAngles.far : data.alternateAngles.near : null;
                    float[] bootOffset = nearSupport && data.alternateBootOffsets!=null
                        ? side==0 ? data.alternateBootOffsets.far : data.alternateBootOffsets.near : null;
                    target[side] = airborne ? new State { hip = new Vector2(side == 0 ? 251 : 253,254),
                        thigh = alternate!=null ? alternate[0] : folded ? 68 : 70,
                        shin = alternate!=null ? alternate[1] : folded ? 132 : 102,
                        boot = alternate!=null ? alternate[2] : folded ? 14 : 12,
                        bootOffset = bootOffset!=null ? V(bootOffset) : new Vector2(2,0) }
                        : Gait(gait,side);
                }
                began = Time.time; duration = airborne ? .16f : .10f;
                transitioning = true; wasAirborne = airborne;wasSecondJump=secondJump;
            }
            if(!transitioning) { SetVisible(false); return false; }
            float t = Mathf.Clamp01((Time.time-began)/duration);
            float eased = t*t*(3-2*t);
            for(int side = 0; side < 2; side++)
            {
                // Landing destination follows the current gait rather than a stale frame.
                if(grounded) target[side] = Gait(gait,side);
                current[side] = new State { hip = Vector2.Lerp(start[side].hip,target[side].hip,eased),
                    thigh = Mathf.LerpAngle(start[side].thigh,target[side].thigh,eased),
                    shin = Mathf.LerpAngle(start[side].shin,target[side].shin,eased),
                    bootOffset = Vector2.Lerp(start[side].bootOffset,target[side].bootOffset,eased),
                    boot = Mathf.LerpAngle(start[side].boot,target[side].boot,eased) };
                Draw(side,current[side]);
            }
            DropPixels = (current[0].hip.y+current[1].hip.y)*.5f-250f;
            if(t >= 1) { transitioning = false; SetVisible(false); return false; }
            SetVisible(true); return true;
        }
        private void Draw(int side, State state)
        {
            Leg original = side == 0 ? data.source.far : data.source.near;
            Vector2 knee = state.hip+Direction(state.thigh)*upperLength[side];
            Vector2 ankle = knee+Direction(state.shin)*lowerLength[side];
            float thighRotation = state.thigh-Angle(V(original.knee)-V(original.hip));
            float shinRotation = state.shin-Angle(V(original.ankle)-V(original.knee));
            Place(side*4,state.hip,thighRotation);
            Place(side*4+1,knee,shinRotation);
            Place(side*4+2,ankle+state.bootOffset,state.boot);
            Place(side*4+3,knee,state.shin-Angle(V(data.source.near.ankle)-V(data.source.near.knee)));
        }
        private void Place(int index, Vector2 point, float rotation)
        {
            var t = renderers[index].transform;
            t.localPosition = new Vector3((point.x-256)/Ppu,(499.2f-point.y)/Ppu,0);
            t.localRotation = Quaternion.Euler(0,0,-rotation);
        }
        private void SetVisible(bool visible) { if(root != null) root.gameObject.SetActive(visible); }
        public void Hide() { SetVisible(false); initialized = false; transitioning = false; }
        public void Dispose()
        {
            if(root != null) UnityEngine.Object.Destroy(root.gameObject);
            foreach(var sprite in sprites) if(sprite != null) UnityEngine.Object.Destroy(sprite);
            if(Waist != null) UnityEngine.Object.Destroy(Waist);
            if(AlternateAirPose != null) UnityEngine.Object.Destroy(AlternateAirPose);
        }
    }
}
