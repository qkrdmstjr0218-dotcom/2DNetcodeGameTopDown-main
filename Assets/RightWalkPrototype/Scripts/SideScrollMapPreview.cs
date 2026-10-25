using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace RightWalkPrototype
{
    [DefaultExecutionOrder(-20), RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class SideScrollMapPreview : MonoBehaviour
    {
        public RightFacingWalker walker;
        public Camera followCamera;
        public float speed = .8f, jumpSpeed = 5f;
        public bool readKeyboard = true;
        public bool allowDoubleJump;
        public bool Grounded { get; private set; }
        public int JumpCount { get; private set; }
        public Collider2D GroundCollider {get;private set;}
        public float? SpeedOverride { get; set; }
        public bool JumpInputBlocked { get; set; }
        public void SetRemoteGrounded(bool value) { Grounded=value; }
        public void SetNetworkState(bool isGrounded, int jumpCount,bool hitReaction=false)
        { Grounded=isGrounded;JumpCount=Mathf.Clamp(jumpCount,0,2);remoteHitReaction=hitReaction; }
        private Rigidbody2D body;
        private BoxCollider2D shape;
        private Vector2 lastPosition, spawn;
        private float input;
        private bool jumpRequested;
        private bool sprintRequested;
        private float pushedUntil;
        private bool remoteHitReaction;
        public bool HitReacting=>enabled?Time.time<pushedUntil:remoteHitReaction;
        public void ClearHitReaction(){pushedUntil=0;remoteHitReaction=false;}
        public void ApplyShotgunPush(Vector2 impulse)=>ApplyHitPush(impulse,SideScrollNetworkPlayer.ShotgunHitSeconds);
        public void ApplyHitPush(Vector2 impulse,float seconds)
        {
            if(!enabled)return;
            // A new hit replaces the shove; repeated hits never stack speed.
            // Follow the signed shot direction, including downward hits. A level
            // shot preserves natural vertical motion instead of making a jump.
            body.linearVelocity=new Vector2(impulse.x,Mathf.Abs(impulse.y)>.001f?impulse.y:body.linearVelocity.y);
            pushedUntil=Time.time+Mathf.Max(0,seconds);
            // A level hit on the floor is a shove, not an airborne transition.
            if(impulse.y>.001f||!Grounded){Grounded=false;GroundCollider=null;JumpCount=Mathf.Max(1,JumpCount);}
            jumpRequested=false;
        }
        private RunWalkPreview runMode;
        private SideScrollNetworkPlayer networkPlayer;
        private readonly RaycastHit2D[] floorHits=new RaycastHit2D[16];
        public void SetInput(float horizontal, bool jump = false, bool sprint = false)
        { readKeyboard=false; input=Mathf.Clamp(horizontal,-1,1);jumpRequested|=jump;sprintRequested=sprint; }
        private void Awake()
        {
            body=GetComponent<Rigidbody2D>();shape=GetComponent<BoxCollider2D>();
            runMode=GetComponent<RunWalkPreview>();networkPlayer=GetComponent<SideScrollNetworkPlayer>();
            spawn=body.position;lastPosition=transform.position;
            walker.externalMovement=true;walker.autoPreview=false;walker.readKeyboard=false;
        }
        private void Update()
        {
            if(readKeyboard&&!RelayArenaPrototype.LiveInputBlocked)
            {
#if ENABLE_INPUT_SYSTEM
                var k=Keyboard.current;
                if(k!=null){input=(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);jumpRequested|=k.spaceKey.wasPressedThisFrame;sprintRequested=k.leftShiftKey.isPressed||k.rightShiftKey.isPressed;}
#elif ENABLE_LEGACY_INPUT_MANAGER
                input=Input.GetAxisRaw("Horizontal");jumpRequested|=Input.GetKeyDown(KeyCode.Space);
                sprintRequested=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);
#endif
            }
            var position=body.position;
            if(position.y < -5) ResetPosition();
        }
        private void FixedUpdate()
        {
            var bounds=shape.bounds;
            var floorHit=Physics2D.BoxCast(new Vector2(bounds.center.x,bounds.min.y+.03f),new Vector2(bounds.size.x*.85f,.04f),0,Vector2.down,.08f,1);
            var arenaMotor=GetComponent<ArenaPlatformMotor>();
            if(arenaMotor!=null)
            {
                var filter=new ContactFilter2D();filter.SetLayerMask(1);filter.useTriggers=false;
                // Box bounds exclude edgeRadius. Probe from the rounded sole so
                // adding a small contact bevel keeps the slope settling margin.
                int count=Physics2D.BoxCast(new Vector2(bounds.center.x,bounds.min.y-shape.edgeRadius+.03f),new Vector2(bounds.size.x*.85f,.04f),0,Vector2.down,filter,floorHits,.08f);
                floorHit=default;float nearest=float.MaxValue;
                for(int i=0;i<count;i++)if(!arenaMotor.IsIgnored(floorHits[i].collider)&&floorHits[i].normal.y>=.5f&&floorHits[i].distance<nearest)
                {floorHit=floorHits[i];nearest=floorHit.distance;}
            }
            if(arenaMotor!=null&&!HitReacting)arenaMotor.CompleteSolidSlopeJoin(ref floorHit,GroundCollider,Grounded,jumpRequested);
            // On an incline, upward motion along the surface is still grounded.
            Grounded=floorHit.collider!=null&&floorHit.normal.y>=.5f&&Vector2.Dot(body.linearVelocity,floorHit.normal)<=.1f;
            GroundCollider=Grounded?floorHit.collider:null;
            if(Grounded)JumpCount=0;
            else if(JumpCount==0)JumpCount=1; // Walking off an edge leaves one air jump.
            var velocity=body.linearVelocity;
            bool pushed=HitReacting;
            bool blocked=pushed||RelayArenaPrototype.ControlsBlocked||readKeyboard&&RelayArenaPrototype.LiveInputBlocked;
            float horizontal=blocked?0:input;
            bool sprint=runMode!=null&&sprintRequested&&Mathf.Abs(horizontal)>.01f&&!SpeedOverride.HasValue;
            if(networkPlayer!=null)sprint=networkPlayer.ResolveSprint(sprint);
            float targetSpeed=SpeedOverride??(runMode!=null&&sprint?runMode.runSpeed:speed);
            if(blocked)jumpRequested=false;
            bool dropping=jumpRequested&&GetComponent<CrouchTrial>() is CrouchTrial crouch&&crouch.IsCrouched&&arenaMotor!=null&&arenaMotor.TryDrop(GroundCollider);
            if(dropping){ArenaTutorial.Instance?.RecordDrop(GroundCollider);Grounded=false;GroundCollider=null;JumpCount=1;velocity=body.linearVelocity;jumpRequested=false;}
            velocity.x=Mathf.MoveTowards(velocity.x,pushed?0:horizontal*targetSpeed,(pushed?3f:14f)*Time.fixedDeltaTime);
            if(blocked&&!pushed)velocity.x=0;
            bool jumping=jumpRequested&&!JumpInputBlocked&&(Grounded||allowDoubleJump&&JumpCount<2);
            if(jumpRequested && !JumpInputBlocked && (Grounded || allowDoubleJump&&JumpCount<2))
            {
                JumpCount=Grounded?1:2;velocity.y=jumpSpeed;Grounded=false;
            }
            if(arenaMotor!=null)arenaMotor.ApplyGroundVelocity(ref velocity,floorHit,Grounded,pushed&&Mathf.Abs(velocity.x)>.01f?Mathf.Sign(velocity.x):horizontal,jumping||dropping);
            jumpRequested=false;body.linearVelocity=velocity;
        }
        private void LateUpdate()
        {
            // Sample the interpolated pose before the hip and aim rigs update.
            Vector2 position=transform.position;
            if(runMode!=null)runMode.SelectGait(HitReacting?0:body.linearVelocity.x);
            if(HitReacting)walker.DriveAnimation(Vector2.zero,Vector2.zero,0);
            else walker.DriveAnimation(new Vector2(position.x-lastPosition.x,0),new Vector2(body.linearVelocity.x,0),Time.deltaTime);
            lastPosition=position;
            if(followCamera==null)return;
            var target=new Vector3(Mathf.Clamp(transform.position.x,-8,8),1.8f,-10);
            followCamera.transform.position=Vector3.Lerp(followCamera.transform.position,target,1-Mathf.Exp(-6*Time.deltaTime));
        }
        public void ResetPosition()
        {
            body.position=spawn;body.linearVelocity=Vector2.zero;lastPosition=spawn;JumpCount=0;jumpRequested=false;
            Grounded=false;GroundCollider=null;ClearHitReaction();
            GetComponent<ArenaPlatformMotor>()?.ResetContactState();
        }
        public void SetSpawnPosition(Vector2 position)
        {spawn=position;ResetPosition();transform.position=new Vector3(spawn.x,spawn.y,transform.position.z);}
        private void OnGUI()
        {
            if(ArenaCombatHud.UsesGameUi||ArenaObjectiveMatch.Ended||ArenaCombatHud.Active||SideScrollNetworkSession.LobbyVisible||ArenaObjectiveMatch.InReadyRoom)return;
            GUILayout.BeginArea(new Rect(15,15,310,180),GUI.skin.box);
            GUILayout.Label("횡스크롤 테스트 맵");
            GUILayout.Label(allowDoubleJump?"A/D 또는 ←/→: 이동 / Space: 점프·공중 1회 추가":"A/D 또는 ←/→: 이동 / Space: 점프");
            var aim=walker.GetComponentInChildren<RightFacingAimRig>();
            string downLabel=aim!=null&&aim.frontDownAim!=null ? "정지 하단 90도 시험 / 이동 "+aim.groundedDownAngle.ToString("F0")+"도" : "지상 아래 "+(aim!=null ? aim.groundedDownAngle.ToString("F0") : "60")+"도";
            if(aim!=null&&aim.frontDownAim!=null&&aim.frontDownAim.allowMovingDownAim)downLabel="정지·이동 하단 90도 시험";
            GUILayout.Label("마우스 조준: 위 90도 / "+downLabel);
            GUILayout.Label("속도: "+speed.ToString("F1"));
            if(runMode!=null)GUILayout.Label("Shift: 달리기 "+runMode.runSpeed.ToString("F1"));
            speed=GUILayout.HorizontalSlider(speed,.8f,RelayArenaPrototype.Instance!=null ? Mathf.Max(4f,RelayArenaPrototype.Instance.walkSpeed*1.5f) : (runMode!=null ? 2f : 2.4f));
            if(GUILayout.Button("시작 위치로 돌아가기"))ResetPosition();
            GUILayout.EndArea();
        }
    }
}
