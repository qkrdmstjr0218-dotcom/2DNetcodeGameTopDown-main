using UnityEngine;
using UnityEngine.U2D.IK;
using TopDownShooter.Core;
namespace TopDownShooter.Networking
{
    public sealed class OperatorAimVisual : MonoBehaviour
    {
        [SerializeField] private Sprite[] poseFrames; // Retained for the earlier art study.
        [SerializeField] private Sprite[] torsoSprites, thighSprites, shinSprites, armSprites, weaponSprites;
        [SerializeField] private float[] thighAxisAngles, shinAxisAngles;
        [SerializeField] private Sprite[] integratedAimPoses;
        [SerializeField] private bool useIntegratedAim = true;
        [SerializeField] private Sprite[] rightWalkFrames;
        [SerializeField, Min(.1f)] private float walkCycleDistance = 2.2f;
        private float rightWalkPhase;
        [SerializeField] private bool useRightLegRig = true;
        [SerializeField] private Sprite[] calibratedLegParts;
        [SerializeField] private float[] calibratedLegAxes;
        private Sprite rightWalkUpperBody;
        private float rightWalkUpperOffset;
        [Tooltip("Local muzzle points authored for E, NE, N, NW, W, SW, S, SE whole-pose sprites.")]
        [SerializeField] private Vector2[] integratedMuzzlePoints = {
            new Vector2(.45f,.265f),new Vector2(.25f,.36f),new Vector2(.03f,.505f),new Vector2(-.26f,.36f),
            new Vector2(-.48f,.233f),new Vector2(-.38f,.15f),new Vector2(-.017f,.061f),new Vector2(.294f,.086f)};
        public bool UsesIntegratedAim => useIntegratedAim && integratedAimPoses != null && integratedAimPoses.Length == 8;
        public int AimSector { get; private set; }
        private sealed class LimbRig
        {
            public Transform root, joint, tip, target;
            public LimbSolver2D solver;
            public SpriteRenderer upper, lower, shoulder, hand, boot;
        }
        private LimbRig rightArm, leftArm, rightLeg, leftLeg;
        private SpriteRenderer bodyArt, gunArt, teamMark;
        private Transform gun;
        private Vector3 previousPosition;
        private float phase, recoil, strideBlend;
        private static Sprite pixel;
        private static Material material;
        public int FacingColumn { get; private set; }
        public int AnimationRow { get; private set; }
        public bool HasDrawnArt => UsesIntegratedAim || (torsoSprites != null && torsoSprites.Length == 3);
        public Vector3 Muzzle => gun != null ? gun.TransformPoint(new Vector3(.5f,0,0)) : transform.position;
        private Transform Bone(string name, Transform parent, Vector3 position)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=position; return go.transform;
        }
        private SpriteRenderer Art(string name,int order)
        {
            if(pixel==null) pixel=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
            if(material==null) material=new Material(Shader.Find("Sprites/Default"));
            var t=Bone(name,transform,Vector3.zero); var sr=t.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite=pixel; sr.sharedMaterial=material; sr.sortingOrder=order; return sr;
        }
        private LimbRig CreateLimb(string name,float upperLength,float lowerLength,bool arm)
        {
            var rig=new LimbRig();
            rig.root=Bone(name+" root",transform,Vector3.zero);
            rig.joint=Bone(name+" joint",rig.root,Vector3.right*upperLength);
            rig.tip=Bone(name+" effector",rig.joint,Vector3.right*lowerLength);
            rig.target=Bone(name+" target",transform,Vector3.zero);
            rig.solver=Bone(name+" IK",transform,Vector3.zero).gameObject.AddComponent<LimbSolver2D>();
            rig.solver.constrainRotation=false; var chain=rig.solver.GetChain(0);
            chain.effector=rig.tip; chain.target=rig.target; rig.solver.Initialize();
            rig.upper=Art(name+" upper art",26); rig.lower=Art(name+" lower art",26);
            if(arm) { rig.shoulder=Art(name+" shoulder art",26); rig.hand=Art(name+" glove art",28); }
            return rig;
        }
        private void Awake()
        {
            if(TryGetComponent<SpriteRenderer>(out var original)) original.enabled=false;
            bodyArt=Art("Drawn operator",20);
            if(UsesIntegratedAim)
            {
                bodyArt.sprite=integratedAimPoses[0];
                bodyArt.transform.localPosition=new Vector3(0,-.43f,0);
                gun=Bone("Integrated muzzle pivot",transform,new Vector3(0,.1f,0));
                teamMark=Art("Team foot marker",5);
                teamMark.transform.localPosition=new Vector3(0,-.48f,0);
                teamMark.transform.localScale=new Vector3(.2f,.02f,1);
                previousPosition=transform.position;
                if(useRightLegRig)
                {
                    bool calibrated=calibratedLegParts!=null && calibratedLegParts.Length==6;
                    rightLeg=CreateLimb("Right leg",calibrated?.125f:.16f,calibrated?.165f:.20f,false);
                    leftLeg=CreateLimb("Left leg",calibrated?.125f:.16f,calibrated?.165f:.20f,false);
                    if(calibrated)
                    {
                        rightLeg.boot=Art("Right boot art",19);leftLeg.boot=Art("Left boot art",11);
                        rightLeg.boot.enabled=leftLeg.boot.enabled=false;
                    }
                    var source=integratedAimPoses[0];var rect=source.rect;
                    float cut=Mathf.Floor(rect.height*.33f);
                    rightWalkUpperOffset=cut/source.pixelsPerUnit;
                    rightWalkUpperBody=Sprite.Create(source.texture,new Rect(rect.x,rect.y+cut,rect.width,rect.height-cut),new Vector2(source.pivot.x/rect.width,0),source.pixelsPerUnit,0,SpriteMeshType.FullRect);
                    rightLeg.upper.enabled=rightLeg.lower.enabled=leftLeg.upper.enabled=leftLeg.lower.enabled=false;
                }
                return;
            }
            if(HasDrawnArt) bodyArt.sprite=torsoSprites[0]; else bodyArt.enabled=false;
            rightArm=CreateLimb("Right arm",.18f,.23f,true); leftArm=CreateLimb("Left arm",.18f,.23f,true);
            rightLeg=CreateLimb("Right leg",.16f,.20f,false); leftLeg=CreateLimb("Left leg",.16f,.20f,false);
            gunArt=Art("Attached weapon",24); gun=gunArt.transform;
            teamMark=Art("Team foot marker",5); teamMark.transform.localPosition=new Vector3(0,-.48f,0); teamMark.transform.localScale=new Vector3(.2f,.02f,1);
            previousPosition=transform.position;
        }
        private void Segment(SpriteRenderer art,Vector3 a,Vector3 b,Sprite sprite,float thickness,int order)
        {
            a=transform.InverseTransformPoint(a); b=transform.InverseTransformPoint(b);
            Vector3 d=b-a; art.sprite=sprite!=null?sprite:pixel;
            art.transform.localPosition=(a+b)*.5f;
            art.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
            Vector2 size=art.sprite.bounds.size;
            art.transform.localScale=new Vector3(d.magnitude/size.x,thickness/size.y,1);
            art.sortingOrder=order;
        }
        private Sprite Pick(Sprite[] sprites,int index) => sprites!=null && index<sprites.Length?sprites[index]:null;
        private void LegSegment(SpriteRenderer art,Vector3 a,Vector3 b,Sprite sprite,float sourceAngle,int order)
        {
            a=transform.InverseTransformPoint(a); b=transform.InverseTransformPoint(b);
            Vector3 d=b-a; art.sprite=sprite!=null?sprite:pixel; art.transform.localPosition=(a+b)*.5f;
            art.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg-sourceAngle);
            art.transform.localScale=Vector3.one*d.magnitude; art.sortingOrder=order;
        }
        private void Solve(LimbRig rig,Vector2 origin,Vector2 goal,bool flip)
        {
            rig.root.localPosition=origin; rig.target.position=transform.TransformPoint(goal); rig.solver.flip=flip; rig.solver.UpdateIK(1);
        }
        private void DrawArm(LimbRig rig,Vector2 shoulder,Vector2 grip,bool flip,int order)
        {
            Solve(rig,shoulder,grip,flip);
            Segment(rig.upper,rig.root.position,rig.joint.position,Pick(armSprites,1),.055f,order);
            Segment(rig.lower,rig.joint.position,rig.tip.position,Pick(armSprites,2),.06f,order);
            rig.shoulder.sprite=Pick(armSprites,0)??pixel;
            rig.shoulder.transform.position=rig.root.position;
            rig.shoulder.transform.localRotation=rig.upper.transform.localRotation;
            rig.shoulder.transform.localScale=Vector3.one*.105f; rig.shoulder.sortingOrder=order+1;
            rig.hand.sprite=Pick(armSprites,3)??pixel; rig.hand.transform.position=rig.tip.position;
            rig.hand.transform.localRotation=gun.localRotation; rig.hand.transform.localScale=Vector3.one*.055f; rig.hand.sortingOrder=order+2;
            var tint=order<20?new Color(.72f,.76f,.83f):Color.white;
            rig.upper.color=tint; rig.lower.color=tint; rig.shoulder.color=tint; rig.hand.color=tint;
        }
        private void DrawLeg(LimbRig rig,bool right,float legPhase,bool mirrored)
        {
            bool side=FacingColumn==1;
            float lane=right?-.09f:.09f;
            float cosine=Mathf.Cos(legPhase),lift=Mathf.Max(0,-Mathf.Sin(legPhase));
            Vector2 hip=new Vector2(side?(right?.015f:-.015f):lane*1.2f,-.11f);
            float sign=mirrored?-1:1;
            Vector2 foot=side?new Vector2(sign*((right?.03f:-.03f)+cosine*.14f*strideBlend),-.43f+lift*.065f*strideBlend)
                :new Vector2(lane*1.2f,-.43f+cosine*.035f*strideBlend+lift*.04f*strideBlend);
            // Front/back knee flex is mostly depth, represented by foreshortening rather than a sideways bow.
            float total=side?.36f:Vector2.Distance(hip,foot)+.002f;
            rig.joint.localPosition=Vector3.right*(total*(.16f/.36f));
            rig.tip.localPosition=Vector3.right*(total*(.20f/.36f));
            Solve(rig,hip,foot,side?mirrored:right);
            int order=side?(right!=mirrored?18:10):18;
            float mirrorSign=side&&mirrored?-1:1;
            LegSegment(rig.upper,rig.root.position,rig.joint.position,Pick(thighSprites,FacingColumn),(thighAxisAngles!=null && thighAxisAngles.Length==3?thighAxisAngles[FacingColumn]:0)*mirrorSign,order);
            LegSegment(rig.lower,rig.joint.position,rig.tip.position,Pick(shinSprites,FacingColumn),(shinAxisAngles!=null && shinAxisAngles.Length==3?shinAxisAngles[FacingColumn]:0)*mirrorSign,order+1);
            // Left-side art is mirrored across the limb's long axis as well as the gait direction.
            rig.upper.flipY=side&&mirrored; rig.lower.flipY=side&&mirrored;
            rig.upper.color=order<18?new Color(.72f,.76f,.83f):Color.white; rig.lower.color=rig.upper.color;
        }
        private void DrawCalibratedLeg(LimbRig rig,bool near,float legPhase)
        {
            float lift=Mathf.Max(0,-Mathf.Sin(legPhase));
            Vector2 hip=new Vector2(near?.025f:-.025f,-.105f);
            Vector2 ankle=new Vector2((near?.025f:-.025f)+Mathf.Cos(legPhase)*.09f,-.375f+lift*.055f);
            Solve(rig,hip,ankle,false);
            int index=near?0:3,order=near?18:10;
            DrawFixedBone(rig.upper,rig.root.position,rig.joint.position,calibratedLegParts[index],calibratedLegAxes[index],order);
            DrawFixedBone(rig.lower,rig.joint.position,rig.tip.position,calibratedLegParts[index+1],calibratedLegAxes[index+1],order+1);
            rig.boot.sprite=calibratedLegParts[index+2];
            rig.boot.flipX=!near;
            rig.boot.transform.position=rig.tip.position;
            // Boot orientation is independent of shin rotation; support soles remain horizontal.
            rig.boot.transform.localRotation=Quaternion.Euler(0,0,lift*8f);
            rig.boot.transform.localScale=Vector3.one;rig.boot.sortingOrder=order+1;
        }
        private void DrawFixedBone(SpriteRenderer art,Vector3 start,Vector3 end,Sprite sprite,float sourceAngle,int order)
        {
            var d=transform.InverseTransformPoint(end)-transform.InverseTransformPoint(start);
            art.sprite=sprite;art.transform.position=start;
            art.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg-sourceAngle);
            // The imported sprite has the same fixed joint length as the IK bone.
            art.transform.localScale=Vector3.one;art.flipX=art.flipY=false;art.color=Color.white;art.sortingOrder=order;
        }
        public void Kick() { recoil=.055f; }
        private void OnDestroy()
        {
            if(rightWalkUpperBody!=null) Destroy(rightWalkUpperBody);
        }
        public void Pose(Vector2 direction,WeaponDefinitionSO weapon,float speed,ulong owner)
        {
            if(gun==null || weapon==null) return;
            if(direction.sqrMagnitude<.001f) direction=Vector2.down; direction.Normalize();
            if(UsesIntegratedAim)
            {
                float degrees=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
                AimSector=((Mathf.RoundToInt(degrees/45f)%8)+8)%8;
                FacingColumn=direction.y>.7072f?2:direction.y<-.7072f?0:1;
                AnimationRow=0; // Whole static poses: do not fabricate gait by deforming limbs.
                bodyArt.sprite=integratedAimPoses[AimSector];
                var motion=transform.position-previousPosition;previousPosition=transform.position;
                bool legWalk=useRightLegRig && rightLeg!=null && AimSector==0
                    && Mathf.Abs(motion.x)>.0001f && Mathf.Abs(motion.x)>=Mathf.Abs(motion.y);
                if(rightLeg!=null)
                    rightLeg.upper.enabled=rightLeg.lower.enabled=leftLeg.upper.enabled=leftLeg.lower.enabled=legWalk;
                if(rightLeg!=null && rightLeg.boot!=null)rightLeg.boot.enabled=leftLeg.boot.enabled=legWalk;
                if(legWalk)
                {
                    rightWalkPhase=Mathf.Repeat(rightWalkPhase+motion.x/Mathf.Max(.1f,walkCycleDistance),1);
                    strideBlend=1;
                    if(rightLeg.boot!=null)
                    {
                        DrawCalibratedLeg(rightLeg,true,rightWalkPhase*Mathf.PI*2);
                        DrawCalibratedLeg(leftLeg,false,rightWalkPhase*Mathf.PI*2+Mathf.PI);
                    }
                    else
                    {
                        DrawLeg(rightLeg,true,rightWalkPhase*Mathf.PI*2,false);
                        DrawLeg(leftLeg,false,rightWalkPhase*Mathf.PI*2+Mathf.PI,false);
                    }
                    bodyArt.sprite=rightWalkUpperBody;
                    AnimationRow=1+Mathf.FloorToInt(rightWalkPhase*4);
                }
                // First art trial: right-facing horizontal movement only; backward playback on retreat.
                bool horizontalWalk=!useRightLegRig && AimSector==0 && rightWalkFrames!=null && rightWalkFrames.Length==6
                    && Mathf.Abs(motion.x)>.0001f && Mathf.Abs(motion.x)>=Mathf.Abs(motion.y);
                if(horizontalWalk)
                {
                    rightWalkPhase=Mathf.Repeat(rightWalkPhase+motion.x/Mathf.Max(.1f,walkCycleDistance),1);
                    int frame=Mathf.Min(5,Mathf.FloorToInt(rightWalkPhase*6));
                    bodyArt.sprite=rightWalkFrames[frame];AnimationRow=frame+1;
                }
                else if(!legWalk) rightWalkPhase=0;
                recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*.7f);
                bodyArt.transform.localPosition=new Vector3(-direction.x*recoil,-.43f-direction.y*recoil,0);
                if(legWalk) bodyArt.transform.localPosition+=Vector3.up*rightWalkUpperOffset;
                gun.localRotation=Quaternion.Euler(0,0,degrees);
                gun.localScale=Vector3.one*(weapon.visualLength*.72f);
                Vector2 muzzle=integratedMuzzlePoints!=null && integratedMuzzlePoints.Length==8
                    ? integratedMuzzlePoints[AimSector] : new Vector2(0,.1f)+direction*.3f;
                // Artwork has a fixed barrel endpoint; weapon stats must not move that endpoint.
                gun.localPosition=(Vector3)(muzzle-direction*recoil-direction*(weapon.visualLength*.36f));
                teamMark.color=owner%2==0?Color.cyan:new Color(1,.65f,.2f);
                return;
            }
            Vector3 displacement=transform.position-previousPosition; previousPosition=transform.position;
            speed=Mathf.Clamp(Mathf.Max(speed,displacement.magnitude/Mathf.Max(Time.deltaTime,.001f)),0,8);
            FacingColumn=direction.y>.7072f?2:direction.y<-.7072f?0:1;
            bool mirrored=FacingColumn==1 && direction.x<0;
            bool backward=Vector2.Dot(displacement,direction)<-.001f;
            if(speed>.1f) phase+=Time.deltaTime*speed*.45f*(backward?-1:1);
            strideBlend=Mathf.MoveTowards(strideBlend,speed>.1f?1:0,Time.deltaTime*10);
            if(strideBlend<.001f) phase=0;
            AnimationRow=speed>.1f?1+((Mathf.FloorToInt(phase*4)%4+4)%4):0;
            if(HasDrawnArt) bodyArt.sprite=torsoSprites[FacingColumn];
            bodyArt.flipX=mirrored; bodyArt.transform.localPosition=new Vector3(0,-.165f,0);
            bodyArt.transform.localRotation=Quaternion.identity;
            DrawLeg(rightLeg,true,phase*Mathf.PI*2,mirrored); DrawLeg(leftLeg,false,phase*Mathf.PI*2+Mathf.PI,mirrored);
            recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*.7f);
            float angle=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
            float length=weapon.visualLength*.72f;
            Vector2 rear=new Vector2(0,.105f)-direction*recoil;
            gun.localPosition=rear+direction*length*.28f; gun.localRotation=Quaternion.Euler(0,0,angle);
            int weaponIndex=weapon.pellets>1?1:weapon.visualLength>.9f?2:0;
            gunArt.sprite=Pick(weaponSprites,weaponIndex)??pixel;
            bool gunFlip=direction.x<-.001f;
            gunArt.flipY=gunFlip;
            gun.localScale=new Vector3(length,length,1); gunArt.color=Color.white;
            gunArt.sortingOrder=FacingColumn==2?14:24;
            Vector2 rightShoulder=new Vector2(mirrored?.055f:-.055f,.13f);
            Vector2 leftShoulder=new Vector2(mirrored?.015f:-.015f,.14f);
            if(FacingColumn!=1) { rightShoulder.x=-.13f; leftShoulder.x=.13f; }
            Vector2 trigger=transform.InverseTransformPoint(gun.TransformPoint(new Vector3(-.14f,gunFlip?.14f:-.14f,0)));
            Vector2 support=transform.InverseTransformPoint(gun.TransformPoint(new Vector3(.18f,gunFlip?.06f:-.06f,0)));
            int rightOrder=FacingColumn==2?12:FacingColumn==1&&mirrored?12:26;
            int leftOrder=FacingColumn==2?12:FacingColumn==1&&!mirrored?12:26;
            DrawArm(rightArm,rightShoulder,trigger,direction.x>=0,rightOrder);
            DrawArm(leftArm,leftShoulder,support,direction.x>=0,leftOrder);
            teamMark.color=owner%2==0?Color.cyan:new Color(1,.65f,.2f);
        }
    }
}
