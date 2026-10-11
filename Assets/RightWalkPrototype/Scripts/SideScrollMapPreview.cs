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
        public float speed = 1.6f, jumpSpeed = 5f;
        public bool readKeyboard = true;
        public bool Grounded { get; private set; }
        private Rigidbody2D body;
        private BoxCollider2D shape;
        private Vector2 lastPosition, spawn;
        private float input;
        private bool jumpRequested;
        public void SetInput(float horizontal, bool jump = false)
        { readKeyboard=false; input=Mathf.Clamp(horizontal,-1,1);jumpRequested|=jump; }
        private void Awake()
        {
            body=GetComponent<Rigidbody2D>();shape=GetComponent<BoxCollider2D>();
            spawn=lastPosition=body.position;
            walker.externalMovement=true;walker.autoPreview=false;walker.readKeyboard=false;
        }
        private void Update()
        {
            if(readKeyboard)
            {
#if ENABLE_INPUT_SYSTEM
                var k=Keyboard.current;
                if(k!=null){input=(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);jumpRequested|=k.spaceKey.wasPressedThisFrame;}
#elif ENABLE_LEGACY_INPUT_MANAGER
                input=Input.GetAxisRaw("Horizontal");jumpRequested|=Input.GetKeyDown(KeyCode.Space);
#endif
            }
            var position=body.position;
            walker.DriveAnimation(new Vector2(position.x-lastPosition.x,0),new Vector2(body.linearVelocity.x,0),Time.deltaTime);
            lastPosition=position;
            if(position.y < -5) ResetPosition();
        }
        private void FixedUpdate()
        {
            var bounds=shape.bounds;
            Grounded=Physics2D.BoxCast(new Vector2(bounds.center.x,bounds.min.y+.03f),new Vector2(bounds.size.x*.85f,.04f),0,Vector2.down,.08f,1).collider!=null;
            var velocity=body.linearVelocity;
            velocity.x=Mathf.MoveTowards(velocity.x,input*speed,14f*Time.fixedDeltaTime);
            if(jumpRequested && Grounded)velocity.y=jumpSpeed;
            jumpRequested=false;body.linearVelocity=velocity;
        }
        private void LateUpdate()
        {
            if(followCamera==null)return;
            var target=new Vector3(Mathf.Clamp(transform.position.x,-8,8),1.8f,-10);
            followCamera.transform.position=Vector3.Lerp(followCamera.transform.position,target,1-Mathf.Exp(-6*Time.deltaTime));
        }
        public void ResetPosition()
        {
            body.position=spawn;body.linearVelocity=Vector2.zero;lastPosition=spawn;
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(15,15,310,180),GUI.skin.box);
            GUILayout.Label("횡스크롤 테스트 맵");
            GUILayout.Label("A/D 또는 ←/→: 이동 / Space: 점프");
            GUILayout.Label("마우스: 좌우 방향 및 ±45도 조준");
            GUILayout.Label("점프는 임시 동작이며 전용 그림은 없습니다.");
            GUILayout.Label("속도: "+speed.ToString("F1"));
            speed=GUILayout.HorizontalSlider(speed,.8f,2.4f);
            if(GUILayout.Button("시작 위치로 돌아가기"))ResetPosition();
            GUILayout.EndArea();
        }
    }
}
