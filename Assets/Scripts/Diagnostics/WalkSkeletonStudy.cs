using UnityEngine;
using UnityEngine.U2D.IK;
namespace TopDownShooter.Diagnostics
{
    // A separate motion study: fixed bones in the sagittal plane, viewed obliquely.
    public sealed class WalkSkeletonStudy : MonoBehaviour
    {
        public bool animate=true;
        [Range(0,1)] public float cycle;
        public float cyclesPerSecond=.8f;
        [Tooltip("Ground-plane toe adjustment for the blue leg; positive turns toward screen right in the study camera.")]
        [Range(-30,30)] public float nearToeYaw=0f;
        public Sprite[] artParts;
        public float[] artAxes;
        public bool nativeStudyScale;
        public Sprite ankleCoverSprite;
        [Range(0,1)] public float ankleFollow=.5f;
        [Range(0,35)] public float ankleMaxAngle=20f;
        [Range(-15,60)] public float ankleClockwiseOffset=50f;
        public Sprite[] redrawnFrames;
        public Sprite studyUpperBody,studyPelvis;
        public bool showFullBody=true;
        public bool standingPreview;
        public Vector3 pelvisArtPosition=new Vector3(0,.465f,0);
        private SpriteRenderer upperBodyArt,pelvisArt;
        private bool closeView;
        private Vector3 galleryCameraPosition;
        private float galleryCameraSize;
        private int galleryCameraMask;
        private void Start()
        {
            if(!animate||Camera.main==null)return;
            var camera=Camera.main;galleryCameraPosition=camera.transform.position;
            galleryCameraSize=camera.orthographicSize;galleryCameraMask=camera.cullingMask;
            foreach(var t in GetComponentsInChildren<Transform>())t.gameObject.layer=30;
            SetCloseView(true);
        }
        private void SetCloseView(bool value)
        {
            closeView=value;var camera=Camera.main;if(camera==null)return;
            camera.transform.position=value?transform.position+Vector3.up*.45f-camera.transform.forward*6:galleryCameraPosition;
            camera.orthographicSize=value?.7f:galleryCameraSize;
            camera.cullingMask=value?1<<30:galleryCameraMask;
        }
        public bool showRedrawn=false;
        private SpriteRenderer redrawn;
        private bool SheetActive=>showRedrawn&&redrawn!=null;
        public bool showArt=true,showBones=true;
        private SpriteRenderer[] art;
        private SpriteRenderer[] ankleCovers;
        private Sprite runtimeFoot,runtimeAnkle;
        private MeshRenderer[] footMeshes,coverMeshes;
        private LineRenderer[] ankleLinks;
        private LineRenderer[] rearAnkleLinks;
        private Mesh[] sleeveMeshes;
        private MeshRenderer[] sleeveRenderers;
        private Vector3 anklePivotOffset;
        private Vector3 PixelPoint(Sprite source,float x,float y)
        {
            return (new Vector2(x*source.texture.width/1536f-source.rect.x,source.texture.height-y*source.texture.height/1024f-source.rect.y)-source.pivot)/source.pixelsPerUnit;
        }
        private MeshRenderer SplitMesh(SpriteRenderer sr,bool cover)
        {
            var source=sr.sprite;var rect=source.rect;
            // Leave the entire yellow ring in the stationary boot; only the upper cuff moves.
            Vector2[] p=cover?new[]{new Vector2(964,205),new Vector2(1170,205),new Vector2(1170,320),new Vector2(1105,340),new Vector2(1085,300),new Vector2(964,300)}:new[]{new Vector2(964,300),new Vector2(1085,300),new Vector2(1105,340),new Vector2(1170,320),new Vector2(1355,320),new Vector2(1355,480),new Vector2(964,480)};
            var vertices=new Vector3[p.Length];var uv=new Vector2[p.Length];var colors=new Color[p.Length];
            for(int i=0;i<p.Length;i++)
            {
                float x=Mathf.Clamp(p[i].x*source.texture.width/1536f,rect.xMin,rect.xMax);
                float y=Mathf.Clamp(source.texture.height-p[i].y*source.texture.height/1024f,rect.yMin,rect.yMax);
                vertices[i]=(new Vector2(x-rect.x,y-rect.y)-source.pivot)/source.pixelsPerUnit;
                if(cover)vertices[i]-=anklePivotOffset;
                uv[i]=new Vector2(x/source.texture.width,y/source.texture.height);colors[i]=sr.color;
            }
            var mesh=new Mesh();mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;
            mesh.triangles=cover?new[]{0,1,2,0,2,3,0,3,4,0,4,5}:new[]{5,6,0,5,0,1,5,1,2,5,2,3,5,3,4};mesh.RecalculateBounds();
            var t=Node("Actual split mesh",sr.transform,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=t.gameObject.AddComponent<MeshRenderer>();var mat=new Material(Shader.Find("Sprites/Default"));mat.mainTexture=source.texture;renderer.sharedMaterial=mat;renderer.sortingOrder=sr.sortingOrder;
            sr.enabled=false;return renderer;
        }
        private void OnDestroy()
        {
            if(runtimeFoot!=null)Destroy(runtimeFoot);
            if(runtimeAnkle!=null)Destroy(runtimeAnkle);
            foreach(var filter in GetComponentsInChildren<MeshFilter>())
                if(filter.name=="Actual split mesh"||filter.name=="Filled ankle sleeve")
                {
                    Destroy(filter.sharedMesh);
                    var renderer=filter.GetComponent<MeshRenderer>();if(renderer!=null)Destroy(renderer.sharedMaterial);
                }
        }
        private class Leg { public Transform hip,knee,ankle,target; public LimbSolver2D solver; public LineRenderer line; }
        private Leg near,far;
        private LineRenderer pelvis;
        private Transform Node(string name,Transform parent,Vector3 position)
        { var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t; }
        private LineRenderer Line(string name,Color color,int count)
        {
            var l=Node(name,transform,Vector3.zero).gameObject.AddComponent<LineRenderer>();
            l.sharedMaterial=new Material(Shader.Find("Sprites/Default"));l.startColor=l.endColor=color;
            l.startWidth=l.endWidth=.025f;l.positionCount=count;l.useWorldSpace=true;l.sortingOrder=10;return l;
        }
        private Leg MakeLeg(string name,float lane,Color color)
        {
            var l=new Leg();l.hip=Node(name+" hip",transform,new Vector3(0,.42f,lane));
            l.knee=Node(name+" knee",l.hip,Vector3.right*.22f);
            l.ankle=Node(name+" ankle",l.knee,Vector3.right*.24f);
            l.target=Node(name+" target",transform,new Vector3(0,.02f,lane));
            l.solver=Node(name+" official IK",transform,Vector3.zero).gameObject.AddComponent<LimbSolver2D>();
            l.solver.constrainRotation=false;l.solver.flip=false;
            var chain=l.solver.GetChain(0);chain.effector=l.ankle;chain.target=l.target;l.solver.Initialize();
            l.line=Line(name,color,4);return l;
        }
        private void Awake()
        {
            near=MakeLeg("Near blue leg",-.055f,new Color(.15f,.75f,1));
            far=MakeLeg("Far orange leg",.055f,new Color(1,.55f,.15f));
            pelvis=Line("Pelvis",Color.white,2);SetPose(cycle);
            if(artParts!=null&&artParts.Length==6&&artAxes!=null&&artAxes.Length==6)
            {
                art=new SpriteRenderer[6];
                for(int i=0;i<6;i++)
                {
                    art[i]=Node("Art "+i,transform,Vector3.zero).gameObject.AddComponent<SpriteRenderer>();
                    // Keep the whole near leg above the far leg; cuff sits behind shin.
                    art[i].sprite=artParts[i];art[i].sortingOrder=(i<3?4:0)+new[]{2,1,0}[i%3];
                    // One-time calibration, never stretch a limb during its animation.
                    float scale=nativeStudyScale?1:i%3==0?.22f/.125f:i%3==1?.24f/.165f:1.5f;
                    art[i].transform.localScale=Vector3.one*scale;
                    if(nativeStudyScale&&i>=3)art[i].color=new Color(.8f,.84f,.9f);
                }
                if(ankleCoverSprite!=null)
                {
                    anklePivotOffset=PixelPoint(ankleCoverSprite,1170,320);
                    footMeshes=new[]{SplitMesh(art[2],false),SplitMesh(art[5],false)};
                    coverMeshes=new MeshRenderer[2];
                    ankleCovers=new SpriteRenderer[2];
                    ankleLinks=new LineRenderer[2];
                    rearAnkleLinks=new LineRenderer[2];
                    sleeveMeshes=new Mesh[2];sleeveRenderers=new MeshRenderer[2];
                    for(int i=0;i<2;i++)
                    {
                        var sr=Node(i==0?"Near ankle cover":"Far ankle cover",transform,Vector3.zero).gameObject.AddComponent<SpriteRenderer>();
                        sr.sprite=ankleCoverSprite;sr.sortingOrder=i==0?7:3;
                        if(i==1)sr.color=new Color(.8f,.84f,.9f);ankleCovers[i]=sr;
                        coverMeshes[i]=SplitMesh(sr,true);
                        ankleLinks[i]=Line(i==0?"Near ankle connector":"Far ankle connector",new Color(.07f,.08f,.1f),2);
                        ankleLinks[i].startWidth=.055f;ankleLinks[i].endWidth=.045f;
                        ankleLinks[i].numCapVertices=4;ankleLinks[i].sortingOrder=i==0?5:1;
                        rearAnkleLinks[i]=Line(i==0?"Near rear ankle sleeve":"Far rear ankle sleeve",new Color(.22f,.24f,.28f),2);
                        rearAnkleLinks[i].startWidth=.05f;rearAnkleLinks[i].endWidth=.055f;
                        rearAnkleLinks[i].numCapVertices=4;rearAnkleLinks[i].sortingOrder=i==0?3:-1;
                        var sleeve=Node("Filled ankle sleeve",transform,Vector3.zero);
                        var mesh=new Mesh();mesh.vertices=new Vector3[4];mesh.triangles=new[]{0,1,2,0,2,3};
                        sleeve.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;sleeveMeshes[i]=mesh;
                        var mr=sleeve.gameObject.AddComponent<MeshRenderer>();var material=new Material(Shader.Find("Sprites/Default"));material.color=new Color(.16f,.18f,.21f);mr.sharedMaterial=material;mr.sortingOrder=i==0?3:-1;sleeveRenderers[i]=mr;
                    }
                }
            }
            if(redrawnFrames!=null&&redrawnFrames.Length==8)
            {
                redrawn=Node("Unified lower body sheet",transform,new Vector3(.015f,.26f,0)).gameObject.AddComponent<SpriteRenderer>();
                redrawn.sortingOrder=8;
                if(Camera.main!=null)redrawn.transform.rotation=Camera.main.transform.rotation;
            }
            if(studyUpperBody!=null)
            {
                upperBodyArt=Node("Study aiming upper body",transform,new Vector3(0,.455f,0)).gameObject.AddComponent<SpriteRenderer>();
                upperBodyArt.sprite=studyUpperBody;upperBodyArt.sortingOrder=9;
                upperBodyArt.transform.localScale=Vector3.one*.85f;
                if(Camera.main!=null)upperBodyArt.transform.rotation=Camera.main.transform.rotation;
            }
            if(studyPelvis!=null)
            {
                pelvisArt=Node("Study pelvis art",transform,pelvisArtPosition).gameObject.AddComponent<SpriteRenderer>();
                pelvisArt.sprite=studyPelvis;pelvisArt.sortingOrder=3;
                pelvisArt.transform.localScale=Vector3.one*.85f;
                if(Camera.main!=null)pelvisArt.transform.rotation=Camera.main.transform.rotation;
            }
        }
        private void Update()
        {
            if(animate)cycle=Mathf.Repeat(cycle+Time.deltaTime*cyclesPerSecond,1);
            SetPose(cycle);
        }
        private void Draw(Leg l,float phase)
        {
            phase=Mathf.Repeat(phase,1);
            // Stance goes front to back; swing returns with a bent knee and raised foot.
            float u=(phase-.5f)*2;
            // Match stance velocity at both ends of swing instead of snapping direction.
            float swing=-u+6*u*u-4*u*u*u;
            float travel=phase<.5f?Mathf.Lerp(.11f,-.11f,phase*2):Mathf.LerpUnclamped(-.11f,.11f,swing);
            // Zero vertical velocity at takeoff and landing, modest ground clearance.
            float lift=phase<.5f?0:Mathf.Pow(Mathf.Sin(u*Mathf.PI),2)*.06f;
            if(standingPreview){travel=0;lift=0;}
            l.target.localPosition=new Vector3(travel,.02f+lift,l.hip.localPosition.z);
            l.solver.UpdateIK(1);
            l.line.SetPosition(0,l.hip.position);l.line.SetPosition(1,l.knee.position);
            l.line.SetPosition(2,l.ankle.position);
            // Both toes stay along the same ground direction, independent of shin angle.
            float yaw=l==near?nearToeYaw*Mathf.Deg2Rad:0;
            var toeDirection=transform.TransformDirection(new Vector3(Mathf.Cos(yaw),0,Mathf.Sin(yaw)));
            l.line.SetPosition(3,l.ankle.position+toeDirection*.10f);
        }
        public void SetPose(float value)
        {
            if(near==null)return;cycle=Mathf.Repeat(value,1);Draw(near,cycle);Draw(far,cycle+.5f);
            pelvis.SetPosition(0,near.hip.position);pelvis.SetPosition(1,far.hip.position);
            pelvis.enabled=near.line.enabled=far.line.enabled=showBones;
            if(art!=null)
            {
                DrawArt(near,0);DrawArt(far,3);
            }
            if(redrawn!=null)
            {
                redrawn.enabled=showArt&&showRedrawn;
                redrawn.sprite=redrawnFrames[Mathf.Min(7,Mathf.FloorToInt(cycle*8))];
            }
            if(upperBodyArt!=null)upperBodyArt.enabled=showArt&&showFullBody;
            if(pelvisArt!=null)pelvisArt.enabled=showArt&&showFullBody&&!SheetActive;
        }
        private void DrawArt(Leg l,int index)
        {
            for(int j=0;j<3;j++)
            {
                var sr=art[index+j];sr.enabled=showArt&&!SheetActive;
                Vector3 start=j==0?l.hip.position:j==1?l.knee.position:l.ankle.position;
                Vector3 end=j==0?l.knee.position:l.ankle.position;
                sr.transform.position=start;
                float angle=j==2?0:Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg-artAxes[index+j];
                sr.transform.rotation=Quaternion.Euler(0,0,angle);
                sr.flipX=!nativeStudyScale&&j==2&&index==3;
                if(j==2&&footMeshes!=null){sr.enabled=false;footMeshes[index==0?0:1].enabled=showArt&&!SheetActive;}
            }
            if(ankleCovers!=null)
            {
                var cover=ankleCovers[index==0?0:1];cover.enabled=showArt&&!SheetActive;
                cover.transform.position=l.ankle.position+anklePivotOffset;cover.transform.rotation=art[index+2].transform.rotation;
                var shin=l.ankle.position-l.knee.position;
                float lean=Mathf.DeltaAngle(-90,Mathf.Atan2(shin.y,shin.x)*Mathf.Rad2Deg);
                // Map the full shin lean range, avoiding a long plateau at the angle limit.
                float flex=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,70,-lean));
                cover.transform.rotation=Quaternion.Euler(0,0,-ankleMaxAngle*ankleFollow*flex-ankleClockwiseOffset);
                int legIndex=index==0?0:1;
                var shinEnd=art[index+1].transform.TransformPoint(PixelPoint(artParts[index+1],747,350));
                var cuffTop=cover.transform.TransformPoint(PixelPoint(ankleCoverSprite,1070,220)-anklePivotOffset);
                ankleLinks[legIndex].SetPosition(0,shinEnd);ankleLinks[legIndex].SetPosition(1,cuffTop);
                ankleLinks[legIndex].enabled=showArt&&!SheetActive;
                // Back of the boot also needs a sleeve; the front cuff link cannot cover it.
                var rearShin=art[index+1].transform.TransformPoint(PixelPoint(artParts[index+1],705,350));
                var rearBoot=art[index+2].transform.TransformPoint(PixelPoint(artParts[index+2],1010,325));
                rearAnkleLinks[legIndex].SetPosition(0,rearBoot);
                rearAnkleLinks[legIndex].SetPosition(1,rearShin);
                rearAnkleLinks[legIndex].enabled=showArt&&!SheetActive;
                var frontShin=art[index+1].transform.TransformPoint(PixelPoint(artParts[index+1],790,350));
                var cuffFront=cover.transform.TransformPoint(PixelPoint(ankleCoverSprite,1130,295)-anklePivotOffset);
                sleeveMeshes[legIndex].vertices=new[]{transform.InverseTransformPoint(rearShin),transform.InverseTransformPoint(frontShin),transform.InverseTransformPoint(cuffFront),transform.InverseTransformPoint(rearBoot)};
                sleeveMeshes[legIndex].RecalculateBounds();sleeveRenderers[legIndex].enabled=showArt&&!SheetActive;
                if(coverMeshes!=null){cover.enabled=false;coverMeshes[index==0?0:1].enabled=showArt&&!SheetActive;}
            }
        }
        private void OnGUI()
        {
            if(!animate)return;
            GUI.Label(new Rect(15,15,650,28),"WALK STUDY: BLUE = near leg / ORANGE = far leg / WHITE = pelvis");
            if(GUI.Button(new Rect(15,45,140,28),cyclesPerSecond==0?"PLAY":"PAUSE"))cyclesPerSecond=cyclesPerSecond==0?.8f:0;
            cycle=GUI.HorizontalSlider(new Rect(15,85,320,25),cycle,0,1);
            showArt=GUI.Toggle(new Rect(15,110,130,25),showArt,"ART");
            showBones=GUI.Toggle(new Rect(150,110,150,25),showBones,"BONES");
            showRedrawn=GUI.Toggle(new Rect(15,140,280,25),showRedrawn,"REDRAW / off = joint parts");
            GUI.Label(new Rect(15,170,320,25),"Ankle follow: "+ankleFollow.ToString("F2")+" (0 = fixed)");
            ankleFollow=GUI.HorizontalSlider(new Rect(15,200,260,25),ankleFollow,0,1);
            GUI.Label(new Rect(15,225,300,25),"Max cuff angle: "+ankleMaxAngle.ToString("F0"));
            ankleMaxAngle=GUI.HorizontalSlider(new Rect(15,255,260,25),ankleMaxAngle,0,35);
            GUI.Label(new Rect(15,280,300,25),"Extra clockwise: "+ankleClockwiseOffset.ToString("F0")+" deg");
            ankleClockwiseOffset=GUI.HorizontalSlider(new Rect(15,310,260,25),ankleClockwiseOffset,-15,60);
            showFullBody=GUI.Toggle(new Rect(15,340,270,25),showFullBody,"FULL BODY / off = legs only");
            if(GUI.Button(new Rect(15,370,220,28),closeView?"SHOW PHASE GALLERY":"SHOW SINGLE CHARACTER"))SetCloseView(!closeView);
            standingPreview=GUI.Toggle(new Rect(15,405,270,25),standingPreview,"STANDING / off = walking");
        }
    }
}

