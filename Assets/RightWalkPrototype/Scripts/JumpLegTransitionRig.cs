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
        private readonly State[] fallStart = new State[2], fallPose = new State[2];
        private readonly System.Collections.Generic.List<Mesh> fallMeshes=new System.Collections.Generic.List<Mesh>();
        public Vector2 FallHip { get; private set; }
        private Vector2 fallHipStart;
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
        // Used by a separate death trial instance, never by normal jump/landing Apply.
        public void PrepareFall(int frame,bool running,bool airborne,bool alternate)
        {
            if(!Ready)return;
            var frames=running?data.run:data.walk;var pose=frames[Mathf.Clamp(frame,0,frames.Length-1)];
            for(int side=0;side<2;side++)
            {
                fallStart[side]=Gait(pose,side);
                if(airborne)
                {
                    bool folded=side==1;var angles=alternate?(side==0?data.alternateAngles.far:data.alternateAngles.near):null;
                    fallStart[side]=new State {hip=new Vector2(side==0?251:253,254),thigh=angles!=null?angles[0]:folded?68:70,
                        shin=angles!=null?angles[1]:folded?132:102,boot=angles!=null?angles[2]:folded?14:12,bootOffset=new Vector2(2,0)};
                }
            }
            var mean=(fallStart[0].hip+fallStart[1].hip)*.5f;
            fallHipStart=new Vector2((mean.x-256)/Ppu,(499.2f-mean.y)/Ppu);FallHip=fallHipStart;
            root.name="Death original articulated legs";
            for(int side=0;side<2;side++)
            {
                var original=side==0?data.source.far:data.source.near;
                var thigh=V(original.knee)-V(original.hip);thigh.y=-thigh.y;
                var shin=V(original.ankle)-V(original.knee);shin.y=-shin.y;
                // Keep the top of the approved cover, but not its oval calf patch.
                // That patch overlapped the shin and drew a second curved seam.
                ClipFallPart(side*4,V(original.knee),-thigh.normalized,8f/Ppu);
                // The shin already contains its own joint. Keep it intact; only
                // add the upper cap, ending inside the black joint housing.
                var capAxis=V(data.source.near.ankle)-V(data.source.near.knee);capAxis.y=-capAxis.y;
                ClipFallPart(side*4+3,V(data.source.near.knee),-capAxis.normalized,8f/Ppu);
            }
        }
        // Same fixed-length two-bone solve and cuff socket registration as the run baker.
        public float CrouchLengthError { get; private set; }
        public float CrouchSocketError { get; private set; }
        private readonly LineRenderer[] crouchCloth=new LineRenderer[2];
        private readonly LineRenderer[] crouchFold=new LineRenderer[2];
        private Material crouchClothMaterial;
        private readonly State[] crouchPose=new State[2];
        private readonly float[] crouchDepth=new float[2];
        private readonly Vector2[] crouchGuardShift=new Vector2[2];
        public float CrouchSupportClearance { get; private set; }
        public float CrouchFootContactError { get; private set; }
        public void PrepareCrouch()
        {
            if(!Ready)return;
            root.name="Crouch original articulated legs";
            crouchClothMaterial=new Material(Shader.Find("Sprites/Default")){mainTexture=Texture2D.whiteTexture,hideFlags=HideFlags.DontSave};
            for(int side=0;side<2;side++)
            {
                var source=side==0?data.source.far:data.source.near;
                var thigh=V(source.knee)-V(source.hip);thigh.y=-thigh.y;
                // Keep the near thigh's full width through its knee socket.
                // An inset plus a diagonal trim removed the outer knee volume.
                // The separate guard still covers the end of the thigh.
                ClipFallPart(side*4,V(source.knee),-thigh.normalized,side==0?8f/Ppu:0f);
                var cuff=side==0?new Vector2(150.8079f,400.3056f):new Vector2(344.1662f,406.4092f);
                var inward=V(source.knee)-cuff;inward.y=-inward.y;
                ClipFallPart(side*4+1,cuff,inward.normalized,-4f/Ppu);
                foreach(var display in renderers[side*4+1].GetComponentsInChildren<MeshRenderer>())display.sortingOrder=side*8+1;
                // The folded boot sits behind the thigh, with the greave
                // still covering its cuff and the guard covering the joint.
                renderers[side*4+2].sortingOrder=side*8+2;
                renderers[side*4+1].sortingOrder=side*8+1;
                renderers[side*4+3].sortingOrder=side*8+5;
                foreach(var display in renderers[side*4].GetComponentsInChildren<MeshRenderer>())display.sortingOrder=side*8+4;
                // Keep the original guard's upper volume, with no circular
                // crop edge over the lower greave. Both parts share its rotation.
                renderers[side*4+3].sharedMaterial=crouchClothMaterial;
                var capAxis=V(data.source.near.ankle)-V(data.source.near.knee);capAxis.y=-capAxis.y;
                ClipFallPart(side*4+3,V(data.source.near.knee),-capAxis.normalized,-3f/Ppu);
                if(side==0)
                {
                    float registration=Angle(V(data.source.near.ankle)-V(data.source.near.knee))-Angle(V(source.ankle)-V(source.knee));
                    crouchGuardShift[side]=RotatePixels(new Vector2(205,337)-V(source.knee),registration)-(new Vector2(290,336)-V(data.source.near.knee));
                    foreach(var display in renderers[side*4+3].GetComponentsInChildren<MeshRenderer>())
                        display.transform.localPosition=new Vector3(crouchGuardShift[side].x/Ppu,-crouchGuardShift[side].y/Ppu,0);
                }
                var go=new GameObject("Crouch knee fabric underlap");go.transform.SetParent(root,false);
                var cloth=go.AddComponent<LineRenderer>();crouchCloth[side]=cloth;
                cloth.useWorldSpace=false;cloth.positionCount=2;cloth.startWidth=.16f;cloth.endWidth=.12f;
                cloth.numCapVertices=8;cloth.numCornerVertices=6;
                cloth.startColor=new Color32(24,28,30,255);cloth.endColor=new Color32(14,17,19,255);cloth.sharedMaterial=crouchClothMaterial;
                cloth.sortingLayerID=renderers[side*4].sortingLayerID;cloth.sortingOrder=side*8+3;
                var foldObject=new GameObject("Crouch inner knee fabric");foldObject.transform.SetParent(root,false);
                var fold=foldObject.AddComponent<LineRenderer>();crouchFold[side]=fold;
                fold.useWorldSpace=false;fold.positionCount=5;fold.startWidth=fold.endWidth=.035f;fold.numCapVertices=3;
                fold.startColor=new Color32(25,29,31,255);fold.endColor=new Color32(15,19,21,255);fold.sharedMaterial=crouchClothMaterial;
                fold.sortingLayerID=cloth.sortingLayerID;fold.sortingOrder=side*8+6;
            }
        }
        private static Vector2 RotatePixels(Vector2 v,float degrees)
        {
            float r=degrees*Mathf.Deg2Rad,c=Mathf.Cos(r),s=Mathf.Sin(r);
            return new Vector2(v.x*c-v.y*s,v.x*s+v.y*c);
        }
        // Opaque silhouette hulls from the unchanged source parts, in source pixels.
        private static Vector2[] ContactPoints(params float[] xy)
        {
            var points=new Vector2[xy.Length/2];
            for(int i=0;i<points.Length;i++)points[i]=new Vector2(xy[i*2],xy[i*2+1]);
            return points;
        }
        private static readonly Vector2[] BootContact=ContactPoints(314,453,323,407,324,405,326,403,328,402,340,401,374,401,422,438,424,440,428,446,429,448,429,452,426,456,425,457,421,460,413,464,410,465,406,466,401,467,392,468,387,468,374,467,333,463,327,462,324,461,319,459,317,458,314,455);
        private static readonly Vector2[] RearCalfContact=ContactPoints(130,383,150,353,166,337,179,325,204,320,209,320,221,331,233,343,233,347,224,358,171,418,169,420,167,420,165,419,135,391,130,386);
        private static readonly Vector2[] NearCalfContact=ContactPoints(277,325,278,324,284,321,302,313,309,310,314,308,318,307,320,307,321,308,333,328,366,391,366,401,364,402,355,406,330,417,325,419,322,419,294,381,292,378,289,373,281,357,277,332);
        private static readonly Vector2[] FarKneeContact=ContactPoints(166,337,179,325,204,320,209,320,221,331,233,343,233,347,224,358,204,373,176,347);
        private static readonly Vector2[] NearKneeContact=ContactPoints(277,325,278,324,284,321,302,313,309,310,314,308,318,307,320,307,321,308,333,328,337,336,339,341,339,342,338,343,332,346,289,365,286,366,285,365,281,357,277,332);
        private static readonly Vector2[] CrouchGuardContact=ContactPoints(276,322,278,319,305,291,309,292,311,293,317,297,324,304,326,307,330,315,331,318,331,324,330,325,287,344,282,346,277,328,276,324);
        public float CrouchContactError {get;private set;}
        public float CrouchNearKneeClearance {get;private set;}
        private static float MaxDown(Vector2[] outline,Vector2 pivot,float angle)
        {
            float result=float.NegativeInfinity;
            foreach(var p in outline)result=Mathf.Max(result,RotatePixels(p-pivot,angle).y);
            return result;
        }
        private float KneeDepth(int side,float angle)
        {
            var original=side==0?data.source.far:data.source.near;
            float raw=MaxDown(side==0?FarKneeContact:NearKneeContact,V(original.knee),angle-Angle(V(original.ankle)-V(original.knee)));
            float capRotation=angle-Angle(V(data.source.near.ankle)-V(data.source.near.knee));
            return Mathf.Max(raw,MaxDown(CrouchGuardContact,V(data.source.near.knee),capRotation)+RotatePixels(crouchGuardShift[side],capRotation).y);
        }
        private Vector2 KneelBootOffset(int side,float angle,float bootAngle)
        {
            var original=side==0?data.source.far:data.source.near;
            var socket=side==0?new Vector2(150.8079f,400.3056f):new Vector2(344.1662f,406.4092f);
            float rotation=angle-Angle(V(original.ankle)-V(original.knee));
            return Direction(angle)*lowerLength[side]+RotatePixels(socket-V(original.ankle),rotation)
                -RotatePixels(new Vector2(350,417)-V(data.source.near.ankle),bootAngle);
        }
        private Vector2 CrouchHip(int side)
        {
            // Register to the accepted walking pelvis, not the detached
            // cutout's original (forward and lower) artwork coordinates.
            // Set both crouching legs back slightly beneath the waist edge.
            return V((side==0?data.walk[0].far:data.walk[0].near).hip)-Vector2.right*8f;
        }
        private float CrouchSupportDepth(int side,float shinAngle,float bootAngle,Vector2 bootOffset)
        {
            var original=side==0?data.source.far:data.source.near;
            float shinRotation=shinAngle-Angle(V(original.ankle)-V(original.knee));
            return Mathf.Max(KneeDepth(side,shinAngle),
                MaxDown(side==0?RearCalfContact:NearCalfContact,V(original.knee),shinRotation));
        }
        private float CrouchToeAngle(int side,float shinAngle,float depth)
        {
            // Rotate around the registered cuff until the toe reaches the
            // floor in stance, and follows a low arc during recovery.
            float lo=90f,hi=180f;
            for(int i=0;i<18;i++)
            {
                float angle=(lo+hi)*.5f;
                float down=KneelBootOffset(side,shinAngle,angle).y+MaxDown(BootContact,V(data.source.near.ankle),angle);
                if(down>depth)lo=angle;else hi=angle;
            }
            return (lo+hi)*.5f;
        }
        private static Vector2 BootSupportPoint(float angle)
        {
            Vector2 result=BootContact[0];float depth=float.NegativeInfinity;
            float radians=angle*Mathf.Deg2Rad,sin=Mathf.Sin(radians),cos=Mathf.Cos(radians);
            foreach(var p in BootContact)
            {
                float y=(p.x-350)*sin+(p.y-417)*cos;
                if(y>depth){depth=y;result=p;}
            }
            return result;
        }
        private void PlaceCrouchFoot(int side,Vector2 knee,Vector2 toe,ref State pose)
        {
            var original=side==0?data.source.far:data.source.near;
            var socket=side==0?new Vector2(150.8079f,400.3056f):new Vector2(344.1662f,406.4092f);
            var cuff=socket-V(original.knee);float calf=cuff.magnitude;
            // Solve knee -> cuff -> ground contact, preserving both source
            // parts. A planted toe can then travel back relative to the hips.
            var previousContact=new Vector2(float.NaN,float.NaN);
            for(int i=0;i<10;i++)
            {
                var contact=BootSupportPoint(pose.boot);
                if(contact==previousContact)break;
                previousContact=contact;
                var foot=contact-new Vector2(350,417);
                var delta=toe-knee;float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(calf-foot.magnitude)+.001f,calf+foot.magnitude-.001f);
                float bend=Mathf.Acos(Mathf.Clamp((calf*calf+d*d-foot.sqrMagnitude)/(2*calf*d),-1,1))*Mathf.Rad2Deg;
                float cuffAngle=Angle(delta)+bend;var targetCuff=knee+Direction(cuffAngle)*calf;
                pose.shin=Mathf.DeltaAngle(0,cuffAngle-Angle(cuff)+Angle(V(original.ankle)-V(original.knee)));
                pose.boot=Mathf.DeltaAngle(0,Angle(toe-targetCuff)-Angle(foot));
            }
        }
        public Vector2 DrawCrouch(float phase,float motion,float drop,float floor,float cycleDistance)
        {
            if(!Ready)return new Vector2(0,drop);
            CrouchLengthError=0;CrouchSocketError=0;CrouchContactError=0;CrouchFootContactError=0;CrouchSupportClearance=float.PositiveInfinity;
            float ground=499.2f-floor*Ppu,poseDrop=drop;
            // Preserve the resting pose, including the accepted guard/boot contacts.
            // Walking uses a small fore/aft hip exchange and a low knee return;
            // the upper body keeps its resting height throughout the cycle.
            for(int side=0;side<2;side++)
            {
                float shinAngle=-165f;
                float bootAngle=127f;
                var idleBoot=KneelBootOffset(side,-165f,bootAngle);
                float idleDepth=CrouchSupportDepth(side,-165f,bootAngle,idleBoot);
                float vertical=ground-idleDepth-CrouchHip(side).y-drop*Ppu;
                float restThigh=Mathf.Asin(Mathf.Clamp(vertical/upperLength[side],-.98f,.98f))*Mathf.Rad2Deg;
                crouchPose[side]=new State{thigh=restThigh,shin=shinAngle,boot=bootAngle};
            }
            for(int side=0;side<2;side++)
            {
                var pose=crouchPose[side];var h=CrouchHip(side)+Vector2.up*(poseDrop*Ppu);pose.hip=h;
                var knee=h+Direction(pose.thigh)*upperLength[side];
                float toeDepth=ground-knee.y;
                pose.boot=CrouchToeAngle(side,pose.shin,toeDepth);
                var neutralBoot=knee+KneelBootOffset(side,pose.shin,pose.boot);
                var neutralToe=neutralBoot+RotatePixels(BootSupportPoint(pose.boot)-V(data.source.near.ankle),pose.boot);
                var wantedToe=new Vector2(neutralToe.x,ground);
                // Retain the resting guard contact and connected trailing boot.
                float kneeLow=h.y+upperLength[side]*.05f,kneeHigh=h.y+upperLength[side]*.9999f;
                for(int solve=0;solve<18;solve++)
                {
                    float kneeY=(kneeLow+kneeHigh)*.5f;
                    float vertical=kneeY-h.y;
                    knee=h+new Vector2(Mathf.Sqrt(Mathf.Max(0,upperLength[side]*upperLength[side]-vertical*vertical)),vertical);
                    pose.boot=127f;
                    PlaceCrouchFoot(side,knee,wantedToe,ref pose);
                    float bottom=knee.y+KneeDepth(side,pose.shin);
                    if(bottom>ground)kneeHigh=kneeY;else kneeLow=kneeY;
                }
                pose.thigh=Angle(knee-h);
                crouchPose[side]=pose;
            }
            if(motion>0)
            {
                for(int side=0;side<2;side++)
                {
                    float t=Mathf.Repeat(phase+(side==0?.5f:0),1);
                    const float stance=.62f;
                    float sweep;
                    float swing=0;
                    if(t<stance)sweep=-1+2*t/stance;
                    else
                    {
                        float u=(t-stance)/(1-stance);
                        float ease=u*u*u*(10+u*(-15+6*u));
                        swing=Mathf.Sin(Mathf.PI*u);swing*=swing;
                        // Carry the thigh forwards during recovery, matching
                        // the rearward stance velocity at both ends of the loop.
                        float tangent=2*(1-stance)/stance;
                        sweep=1+tangent*u+(-2-tangent)*ease;
                    }
                    var pose=crouchPose[side];
                    var hip=CrouchHip(side)+Vector2.up*(drop*Ppu);
                    // A small opposite hip exchange suggests pelvic rotation
                    // in side view without raising the torso or scaling the art.
                    hip.x+=(side==0?-2f:4f)-(side==0?10f:13f)*sweep;
                    float shin=(side==0?-181f:-174f)-(side==0?5f:3f)*sweep;
                    float depth=KneeDepth(side,shin);
                    float y=Mathf.Clamp(ground-depth-4.5f*swing-hip.y,
                        upperLength[side]*.05f,upperLength[side]*.9999f);
                    float x=Mathf.Sqrt(upperLength[side]*upperLength[side]-y*y);
                    pose.hip=Vector2.Lerp(pose.hip,hip,motion);
                    pose.thigh=Mathf.LerpAngle(pose.thigh,Angle(new Vector2(x,y)),motion);
                    pose.shin=Mathf.LerpAngle(pose.shin,shin,motion);
                    pose.boot=Mathf.LerpAngle(pose.boot,CrouchToeAngle(side,shin,depth),motion);
                    crouchPose[side]=pose;
                }
                // Interpolating limb rotations must not push a guard or cuff
                // through the floor during the start/stop blend.
                float correction=0;
                for(int side=0;side<2;side++)
                {
                    var pose=crouchPose[side];
                    var knee=pose.hip+Direction(pose.thigh)*upperLength[side];
                    var offset=KneelBootOffset(side,pose.shin,pose.boot);
                    float depth=Mathf.Max(CrouchSupportDepth(side,pose.shin,pose.boot,offset),
                        offset.y+MaxDown(BootContact,V(data.source.near.ankle),pose.boot));
                    correction=Mathf.Max(correction,knee.y+depth-ground);
                }
                poseDrop-=correction/Ppu;
                for(int side=0;side<2;side++)
                {
                    var pose=crouchPose[side];pose.hip.y-=correction;crouchPose[side]=pose;
                }
            }
            for(int side=0;side<2;side++)
            {
                var original=side==0?data.source.far:data.source.near;
                var pose=crouchPose[side];var h=pose.hip;
                var knee=h+Direction(pose.thigh)*upperLength[side];
                var ankle=knee+Direction(pose.shin)*lowerLength[side];
                var boot=knee+KneelBootOffset(side,pose.shin,pose.boot);pose.bootOffset=boot-ankle;
                crouchDepth[side]=CrouchSupportDepth(side,pose.shin,pose.boot,boot-knee);
                float toeBottom=boot.y+MaxDown(BootContact,V(data.source.near.ankle),pose.boot);
                if(ground-knee.y-KneeDepth(side,pose.shin)<.001f)CrouchFootContactError=Mathf.Max(CrouchFootContactError,Mathf.Abs(toeBottom-ground)/Ppu);
                CrouchContactError=Mathf.Max(CrouchContactError,Mathf.Max(0,(toeBottom-ground)/Ppu));
                float shinRotation=pose.shin-Angle(V(original.ankle)-V(original.knee));
                var socket=side==0?new Vector2(150.8079f,400.3056f):new Vector2(344.1662f,406.4092f);
                var calfSocket=ankle+RotatePixels(socket-V(original.ankle),shinRotation);
                var bootSocket=boot+RotatePixels(new Vector2(350,417)-V(data.source.near.ankle),pose.boot);
                CrouchSocketError=Mathf.Max(CrouchSocketError,Vector2.Distance(calfSocket,bootSocket)/Ppu);
                CrouchLengthError=Mathf.Max(CrouchLengthError,Mathf.Abs(Vector2.Distance(h,knee)-upperLength[side])/Ppu,Mathf.Abs(Vector2.Distance(knee,ankle)-lowerLength[side])/Ppu);
                float clearance=(ground-knee.y-crouchDepth[side])/Ppu;
                CrouchContactError=Mathf.Max(CrouchContactError,Mathf.Max(0,-clearance));
                CrouchSupportClearance=Mathf.Min(CrouchSupportClearance,clearance);
                if(side==1)CrouchNearKneeClearance=(ground-knee.y-KneeDepth(side,pose.shin))/Ppu;
                Draw(side,pose);
                var axis=(knee-h).normalized;var normal=new Vector2(-axis.y,axis.x);
                var clothStart=Vector2.Lerp(h,knee,.60f);var clothEnd=knee-axis*3;
                crouchCloth[side].SetPosition(0,new Vector3((clothStart.x-256)/Ppu,(499.2f-clothStart.y)/Ppu,0));
                crouchCloth[side].SetPosition(1,new Vector3((clothEnd.x-256)/Ppu,(499.2f-clothEnd.y)/Ppu,0));
                for(int i=0;i<5;i++)
                {
                    float across=(i-2)*5;
                    var p=knee-axis*(18+4*(1-Mathf.Abs(i-2)/2f))+normal*across;
                    crouchFold[side].SetPosition(i,new Vector3((p.x-256)/Ppu,(499.2f-p.y)/Ppu,-.001f));
                }
            }
            SetVisible(true);
            return new Vector2(0,poseDrop);
        }
        private void ClipFallPart(int index,Vector2 jointPixel,Vector2 inward,float inset)
        {
            var sprite=sprites[index];var pivot=sprite.pivot/Ppu;var size=sprite.rect.size/Ppu;
            var joint=new Vector2(jointPixel.x/Ppu,(sprite.texture.height-jointPixel.y)/Ppu)-pivot;
            var polygon=new[]{-pivot,new Vector2(size.x,0)-pivot,size-pivot,new Vector2(0,size.y)-pivot};
            var clipped=new System.Collections.Generic.List<Vector2>();
            for(int i=0;i<polygon.Length;i++)
            {
                var a=polygon[i];var b=polygon[(i+1)%polygon.Length];float da=Vector2.Dot(a-joint,inward)-inset,db=Vector2.Dot(b-joint,inward)-inset;
                if(da>=0)clipped.Add(a);
                if((da>=0)!=(db>=0))clipped.Add(Vector2.Lerp(a,b,da/(da-db)));
            }
            var vertices=new Vector3[clipped.Count];var uv=new Vector2[clipped.Count];var colors=new Color[clipped.Count];
            for(int i=0;i<clipped.Count;i++){vertices[i]=clipped[i];uv[i]=(clipped[i]+pivot)*Ppu/sprite.texture.width;colors[i]=Color.white;}
            var triangles=new int[(clipped.Count-2)*3];
            for(int i=1;i<clipped.Count-1;i++){int k=(i-1)*3;triangles[k]=0;triangles[k+1]=i;triangles[k+2]=i+1;}
            var mesh=new Mesh{name="Death clipped original limb",hideFlags=HideFlags.DontSave};mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();fallMeshes.Add(mesh);
            var go=new GameObject("Original limb without duplicate joint");go.transform.SetParent(renderers[index].transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();
            var material=new Material(renderers[index].sharedMaterial){mainTexture=sprite.texture,hideFlags=HideFlags.DontSave};renderer.sharedMaterial=material;
            renderer.sortingLayerID=renderers[index].sortingLayerID;renderer.sortingOrder=renderers[index].sortingOrder;renderers[index].enabled=false;
        }
        public Vector2 DrawFall(float progress,float floor,bool constrainToFloor)
        {
            if(!Ready)return fallHipStart;
            float t=Mathf.Clamp01(progress),u=1-t;var end=new Vector2(-.65f,floor+.38f);
            var hip=u*u*u*fallHipStart+3*u*u*t*(fallHipStart+new Vector2(-.05f,-.15f))
                +3*u*t*t*(end+new Vector2(.35f,.08f))+t*t*t*end;
            for(int side=0;side<2;side++)
            {
                var initial=fallStart[side];float phase=Mathf.SmoothStep(0,1,t);
                // Fold the rear leg instead of stacking both calf silhouettes.
                // Otherwise its neighbour's outline reads as a ring on white armour.
                float thigh=Mathf.LerpAngle(initial.thigh,side==0?-45:0,phase);
                float shin=Mathf.LerpAngle(initial.shin,side==0?70:20,phase);
                float restShin=Angle(V(data.source.near.ankle)-V(data.source.near.knee));
                fallPose[side]=new State {hip=new Vector2(256+hip.x*Ppu+(side==0?-1:1),499.2f-hip.y*Ppu),
                    thigh=thigh,shin=shin,boot=shin-restShin,bootOffset=Vector2.zero};
            }
            float lift=0;
            if(constrainToFloor)
            {
                Vector2[] outline={new Vector2(328,403),new Vector2(374,403),new Vector2(378,420),new Vector2(400,436),
                    new Vector2(420,442),new Vector2(428,453),new Vector2(427,463),new Vector2(401,469),new Vector2(369,467),new Vector2(345,459),new Vector2(315,458),new Vector2(316,448),new Vector2(322,429)};
                for(int side=0;side<2;side++)
                {
                    var s=fallPose[side];var ankle=s.hip+Direction(s.thigh)*upperLength[side]+Direction(s.shin)*lowerLength[side]+s.bootOffset;
                    var rotation=Quaternion.Euler(0,0,-s.boot);
                    foreach(var p in outline)
                    {
                        var offset=rotation*new Vector3((p.x-data.source.near.ankle[0])/Ppu,(data.source.near.ankle[1]-p.y)/Ppu,0);
                        lift=Mathf.Max(lift,floor-((499.2f-ankle.y)/Ppu+offset.y));
                    }
                }
            }
            for(int side=0;side<2;side++){var s=fallPose[side];s.hip.y-=lift*Ppu;Draw(side,s);}
            FallHip=hip+Vector2.up*lift;SetVisible(true);return FallHip;
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
            if(crouchClothMaterial!=null)UnityEngine.Object.Destroy(crouchClothMaterial);
            foreach(var mesh in fallMeshes)if(mesh!=null)UnityEngine.Object.Destroy(mesh);
            if(root!=null)foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))if(renderer.sharedMaterial!=null)UnityEngine.Object.Destroy(renderer.sharedMaterial);
            if(root != null) UnityEngine.Object.Destroy(root.gameObject);
            foreach(var sprite in sprites) if(sprite != null) UnityEngine.Object.Destroy(sprite);
            if(Waist != null) UnityEngine.Object.Destroy(Waist);
            if(AlternateAirPose != null) UnityEngine.Object.Destroy(AlternateAirPose);
        }
    }
}
