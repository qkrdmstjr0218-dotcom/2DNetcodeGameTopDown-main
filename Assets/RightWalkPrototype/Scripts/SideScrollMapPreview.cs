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
        public void SetRemoteGrounded(bool value) { Grounded=value; }
        private Rigidbody2D body;
        private BoxCollider2D shape;
        private Vector2 lastPosition, spawn;
        private float input;
        private bool jumpRequested;
        private bool sprintRequested;
        private RunWalkPreview runMode;
        public void SetInput(float horizontal, bool jump = false, bool sprint = false)
        { readKeyboard=false; input=Mathf.Clamp(horizontal,-1,1);jumpRequested|=jump;sprintRequested=sprint; }
        private void Awake()
        {
            body=GetComponent<Rigidbody2D>();shape=GetComponent<BoxCollider2D>();
            runMode=GetComponent<RunWalkPreview>();
            spawn=body.position;lastPosition=transform.position;
            walker.externalMovement=true;walker.autoPreview=false;walker.readKeyboard=false;
        }
        private void Update()
        {
            if(readKeyboard)
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
            Grounded=body.linearVelocity.y<=.1f&&Physics2D.BoxCast(new Vector2(bounds.center.x,bounds.min.y+.03f),new Vector2(bounds.size.x*.85f,.04f),0,Vector2.down,.08f,1).collider!=null;
            if(Grounded)JumpCount=0;
            else if(JumpCount==0)JumpCount=1; // Walking off an edge leaves one air jump.
            var velocity=body.linearVelocity;
            float targetSpeed=runMode!=null && sprintRequested ? runMode.runSpeed : speed;
            velocity.x=Mathf.MoveTowards(velocity.x,input*targetSpeed,14f*Time.fixedDeltaTime);
            if(jumpRequested && (Grounded || allowDoubleJump&&JumpCount<2))
            {
                JumpCount=Grounded?1:2;velocity.y=jumpSpeed;Grounded=false;
            }
            jumpRequested=false;body.linearVelocity=velocity;
        }
        private void LateUpdate()
        {
            // Sample the interpolated pose before the hip and aim rigs update.
            Vector2 position=transform.position;
            if(runMode!=null)runMode.SelectGait(body.linearVelocity.x);
            walker.DriveAnimation(new Vector2(position.x-lastPosition.x,0),new Vector2(body.linearVelocity.x,0),Time.deltaTime);
            lastPosition=position;
            if(followCamera==null)return;
            var target=new Vector3(Mathf.Clamp(transform.position.x,-8,8),1.8f,-10);
            followCamera.transform.position=Vector3.Lerp(followCamera.transform.position,target,1-Mathf.Exp(-6*Time.deltaTime));
        }
        public void ResetPosition()
        {
            body.position=spawn;body.linearVelocity=Vector2.zero;lastPosition=spawn;JumpCount=0;
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(15,15,310,180),GUI.skin.box);
            GUILayout.Label("횡스크롤 테스트 맵");
            GUILayout.Label(allowDoubleJump?"A/D 또는 ←/→: 이동 / Space: 점프·공중 1회 추가":"A/D 또는 ←/→: 이동 / Space: 점프");
            var aim=walker.GetComponentInChildren<RightFacingAimRig>();
            string downLabel=aim!=null&&aim.frontDownAim!=null ? "정지 하단 90도 시험 / 이동 "+aim.groundedDownAngle.ToString("F0")+"도" : "지상 아래 "+(aim!=null ? aim.groundedDownAngle.ToString("F0") : "60")+"도";
            if(aim!=null&&aim.frontDownAim!=null&&aim.frontDownAim.allowMovingDownAim)downLabel="정지·이동 하단 90도 시험";
            GUILayout.Label("마우스 조준: 위 90도 / "+downLabel);
            GUILayout.Label("속도: "+speed.ToString("F1"));
            if(runMode!=null)GUILayout.Label("Shift: 달리기 "+runMode.runSpeed.ToString("F1"));
            speed=GUILayout.HorizontalSlider(speed,.8f,runMode!=null ? 2f : 2.4f);
            if(GUILayout.Button("시작 위치로 돌아가기"))ResetPosition();
            GUILayout.EndArea();
        }
    }
}
