using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RightWalkPrototype
{
    // Air-only art. Approved lower-body pixels are registered below the unchanged torso.
    [DefaultExecutionOrder(80), DisallowMultipleComponent]
    public sealed class FrontDownAimAirLegs : MonoBehaviour
    {
        public Material approvedLegMaterial;
        // Registration from the approved review to the source body; no extra thigh shortening.
        public float reviewPixelsPerSourcePixel=.59f;
        public Vector2 reviewHipAnchor=new Vector2(560f,746.5f);
        public Vector4 reviewArtBounds=new Vector4(420,735,700,1180);
        public bool mirrorApprovedLegs;
        public bool selectPoseFromTakeoffFoot;
        public bool keepTakeoffLegFacing;
        public bool SelectedMirror => (selectPoseFromTakeoffFoot ? selectedMirror : mirrorApprovedLegs)
            ^ (movement!=null&&movement.allowDoubleJump&&movement.JumpCount>1);
        [Range(-45,45)] public float leftThighDegrees=4f,rightThighDegrees=-3f;
        [Range(-65,65)] public float leftShinDegrees=-22f,rightShinDegrees=12f;
        private Transform root;
        private readonly List<Mesh> meshes=new List<Mesh>();
        private Transform leftThigh,leftShin,rightThigh,rightShin;
        private Vector3 leftHip,leftKnee,rightHip,rightKnee;
        private FrontDownAimOverlay owner;
        private Transform approvedLegs;
        private Transform approvedLowerBody;
        private Mesh approvedMesh;
        private SideScrollMapPreview movement;
        private RightFacingWalker walker;
        private RunWalkPreview gait;
        private JumpLegTransitionRig.Data footData;
        private bool rememberedMirror, selectedMirror, wasAirborne;
        private bool appliedMirror;
        private float takeoffFacing=1f;
        private bool networkDriven;
        public bool TakeoffMirror => selectedMirror;
        public float TakeoffFacing => takeoffFacing;
        public void SetNetworkPose(bool mirror,float facing)
        { networkDriven=true;selectedMirror=mirror;takeoffFacing=facing<0?-1f:1f; }
        private void Awake()
        {
            movement=GetComponent<SideScrollMapPreview>();walker=GetComponent<RightFacingWalker>();
            gait=GetComponent<RunWalkPreview>();
            rememberedMirror=selectedMirror=mirrorApprovedLegs;
            takeoffFacing=transform.lossyScale.x<0?-1f:1f;
            if(!selectPoseFromTakeoffFoot)return;
            var asset=Resources.Load<TextAsset>("JumpTransition/Joints");
            if(asset!=null)footData=JsonUtility.FromJson<JumpLegTransitionRig.Data>(asset.text);
        }
        private void LateUpdate()
        {
            if(networkDriven)return;
            if(movement==null||walker==null)return;
            bool airborne=!movement.Grounded;
            if(selectPoseFromTakeoffFoot&&!airborne&&Mathf.Abs(walker.Velocity.x)>.03f&&footData!=null)
            {
                var frames=gait!=null&&gait.IsRunning ? footData.run : footData.walk;
                if(frames!=null&&frames.Length>0)
                {
                    var pose=frames[Mathf.Clamp(walker.FrameIndex,0,frames.Length-1)];
                    // Source gait faces +X. Near is the character's right leg;
                    // the unmirrored approved art raises the character's left leg.
                    float separation=pose.near.ankle[0]-pose.far.ankle[0];
                    if(Mathf.Abs(separation)>2f)rememberedMirror=separation>0;
                }
            }
            if(airborne&&!wasAirborne)
            {
                selectedMirror=rememberedMirror;
                takeoffFacing=transform.lossyScale.x<0?-1f:1f;
            }
            wasAirborne=airborne;
        }
        private void UpdateApprovedMirror()
        {
            bool mirror=SelectedMirror;
            if(approvedMesh==null||appliedMirror==mirror)return;
            float u0=mirror?1f:0f,u1=1f-u0;
            approvedMesh.uv=new[]{new Vector2(u0,1),new Vector2(u1,1),new Vector2(u1,0),new Vector2(u0,0)};
            appliedMirror=mirror;
        }
        private static Vector2[] Rect(float x0,float y0,float x1,float y1)
        { return new[]{new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1),new Vector2(x0,y1)}; }
        private Transform Piece(string name,Vector2[] points,Vector3 pivot,int order)
        {
            var layer=owner.Data.layers[0];foreach(var candidate in owner.Data.layers)if(candidate.id=="body")layer=candidate;
            var vertices=new Vector3[points.Length];var uv=new Vector2[points.Length];var colors=new Color[points.Length];
            var triangles=new int[(points.Length-2)*3];var b=layer.bounds;var a=layer.atlas;
            for(int i=0;i<points.Length;i++)
            {
                vertices[i]=owner.BodyPoint(points[i])-pivot;colors[i]=Color.white;
                uv[i]=new Vector2((a[0]+(points[i].x-b[0])/(b[2]-b[0])*a[2])/owner.Data.atlasSize[0],
                    1-(a[1]+(points[i].y-b[1])/(b[3]-b[1])*a[3])/owner.Data.atlasSize[1]);
                if(i<2)continue;int t=(i-2)*3;triangles[t]=0;triangles[t+1]=i-1;triangles[t+2]=i;
            }
            var mesh=new Mesh {name=name,hideFlags=HideFlags.DontSave};meshes.Add(mesh);
            mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();
            var go=new GameObject(name);go.hideFlags=HideFlags.DontSave;go.layer=gameObject.layer;go.transform.SetParent(root,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();
            r.sharedMaterial=owner.material;r.sortingOrder=order;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            return go.transform;
        }
        private void Initialize(FrontDownAimOverlay overlay)
        {
            owner=overlay;var go=new GameObject("Front air original leg pieces");go.hideFlags=HideFlags.DontSave;
            root=go.transform;root.SetParent(transform,false);go.SetActive(false);
            if(approvedLegMaterial!=null)
            {
                // Split at the same source row as the standing mesh, retaining its exact torso mapping.
                Piece("Air unchanged upper torso",Rect(21,1,1004,720),Vector3.zero,12);
                // Follow the pouch outlines so the old outer-thigh white plates cannot overlap the new ones.
                Piece("Air unchanged left hip pouch",new[]{new Vector2(270,720),new Vector2(411,720),new Vector2(396,747),new Vector2(383,763),new Vector2(372,771),new Vector2(353,774),new Vector2(334,770),new Vector2(317,760),new Vector2(303,749),new Vector2(270,740)},Vector3.zero,12);
                Piece("Air unchanged right hip pouch",new[]{new Vector2(617,720),new Vector2(770,720),new Vector2(770,740),new Vector2(750,750),new Vector2(735,762),new Vector2(717,771),new Vector2(696,775),new Vector2(677,771),new Vector2(662,760),new Vector2(646,739)},Vector3.zero,12);
                var lower=new GameObject("Air lower body facing");lower.hideFlags=HideFlags.DontSave;
                approvedLowerBody=lower.transform;approvedLowerBody.SetParent(root,false);
                Piece("Air unchanged center pelvis panel",Rect(440,720,605,820),Vector3.zero,12).SetParent(approvedLowerBody,false);
                // The review rifle occluded this cloth. Reuse the source cloth behind the new legs
                // so oblique aiming does not expose a rifle-shaped transparent hole at the crotch.
                Piece("Air original hidden upper thigh cloth",Rect(440,775,605,905),Vector3.zero,10).SetParent(approvedLowerBody,false);
                BuildApprovedLegs();
                approvedLegs.SetParent(approvedLowerBody,false);
                return;
            }
            leftHip=owner.BodyPoint(new Vector2(440,765));leftKnee=owner.BodyPoint(new Vector2(385,1000));
            rightHip=owner.BodyPoint(new Vector2(600,765));rightKnee=owner.BodyPoint(new Vector2(645,1000));
            Piece("Air torso and hip gear",Rect(21,1,1004,775),Vector3.zero,12);
            Piece("Air center pelvis panel",Rect(485,775,560,820),Vector3.zero,12);
            leftThigh=Piece("Air left thigh and knee",new[]{new Vector2(290,765),new Vector2(480,765),new Vector2(490,810),new Vector2(450,1045),new Vector2(270,1045)},leftHip,11);
            rightThigh=Piece("Air right thigh and knee",new[]{new Vector2(560,765),new Vector2(770,765),new Vector2(800,1045),new Vector2(590,1045),new Vector2(545,810)},rightHip,11);
            leftShin=Piece("Air left straight shin and boot",Rect(210,1015,500,1484),leftKnee,10);
            rightShin=Piece("Air right straight shin and boot",Rect(540,1015,860,1484),rightKnee,10);
        }
        private void BuildApprovedLegs()
        {
            var anchor=owner.BodyPoint(new Vector2(owner.Data.origin[0],775));
            float scale=reviewPixelsPerSourcePixel*owner.pixelsPerUnit;
            var points=Rect(reviewArtBounds.x,reviewArtBounds.y,reviewArtBounds.z,reviewArtBounds.w);
            var vertices=new Vector3[4];
            for(int i=0;i<4;i++)vertices[i]=anchor+new Vector3((points[i].x-reviewHipAnchor.x)/scale,(reviewHipAnchor.y-points[i].y)/scale,0);
            var mesh=new Mesh {name="Approved air legs",hideFlags=HideFlags.DontSave};meshes.Add(mesh);
            appliedMirror=SelectedMirror;approvedMesh=mesh;
            float u0=appliedMirror?1f:0f,u1=1f-u0;
            mesh.vertices=vertices;mesh.uv=new[]{new Vector2(u0,1),new Vector2(u1,1),new Vector2(u1,0),new Vector2(u0,0)};
            mesh.colors=new[]{Color.white,Color.white,Color.white,Color.white};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();
            var go=new GameObject("Approved air lower body");go.hideFlags=HideFlags.DontSave;go.layer=gameObject.layer;go.transform.SetParent(root,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=approvedLegMaterial;renderer.sortingOrder=11;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            approvedLegs=go.transform;
        }
        private static void Pose(Transform thigh,Transform shin,Vector3 hip,Vector3 knee,float thighAngle,float shinAngle)
        {
            var rotation=Quaternion.Euler(0,0,thighAngle);thigh.localPosition=hip;thigh.localRotation=rotation;
            shin.localPosition=hip+rotation*(knee-hip);shin.localRotation=Quaternion.Euler(0,0,shinAngle);
        }
        public void Show(bool visible,FrontDownAimOverlay overlay)
        {
            if(!visible){Hide();return;}if(root==null)Initialize(overlay);
            UpdateApprovedMirror();
            if(approvedLowerBody!=null)
            {
                // Cancel only the current parent's X reflection; torso/pouches/gun keep following aim.
                float facing=transform.lossyScale.x<0?-1f:1f;
                approvedLowerBody.localScale=new Vector3(keepTakeoffLegFacing?takeoffFacing*facing:1f,1,1);
            }
            if(approvedLegs==null)
            {
                Pose(leftThigh,leftShin,leftHip,leftKnee,leftThighDegrees,leftShinDegrees);
                Pose(rightThigh,rightShin,rightHip,rightKnee,rightThighDegrees,rightShinDegrees);
            }
            root.gameObject.SetActive(true);
        }
        public void Hide(){if(root!=null)root.gameObject.SetActive(false);}
        private void OnDisable(){Hide();}
        private void OnDestroy()
        {
            if(root!=null)Destroy(root.gameObject);foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
        }
    }
}
