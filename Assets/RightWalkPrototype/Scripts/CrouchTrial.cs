using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RightWalkPrototype
{
    [DefaultExecutionOrder(105),DisallowMultipleComponent]
    public sealed class CrouchTrial : MonoBehaviour
    {
        public Texture2D lowerAtlas;
        public float bodyDrop=.50f,crouchSpeed=.65f,cycleDistance=.65f;
        public bool readKeyboard=true;
        public bool IsCrouched {get;private set;}
        public int FrameIndex {get;private set;}
        private bool requested;
        private SideScrollMapPreview movement;
        private CharacterDeathTrial death;
        private RightFacingWalker walker;
        private RightFacingHipLink hip;
        private RightFacingAimRig aim;
        private FrontDownAimOverlay down;
        private AirDownShotPose air;
        private RightFacingPelvisPreview pelvis;
        private SpriteRenderer original,underlay;
        private Rigidbody2D physicsBody;
        private BoxCollider2D shape;
        private Vector2 standingSize,standingOffset;
        private JumpLegTransitionRig legRig;
        private float motion;
        private Vector2 lastVisualPosition;
        private bool oldPelvisSuppressed;
        private bool airEnabled,downEnabled,originalHidden,underlayHidden,oldAimRestriction,oldJumpBlocked;
        private float? oldSpeed,oldDrop;
        private float phase;
        private float floor;
        private bool networkDriven;
        public float CyclePhase => phase;
        public float MotionAmount => motion;
        public void SetNetworkDriven(bool value){networkDriven=value;if(value)readKeyboard=false;}
        public void ApplyNetworkPose(bool crouched,float cyclePhase,float motionAmount)
        {
            if(crouched&&!IsCrouched)Enter();else if(!crouched&&IsCrouched)Exit();
            phase=Mathf.Repeat(cyclePhase,1);motion=Mathf.Clamp01(motionAmount);
        }
        public void SetRequested(bool value){readKeyboard=false;requested=value;}
        private void Awake()
        {
            movement=GetComponent<SideScrollMapPreview>();death=GetComponent<CharacterDeathTrial>();
            walker=GetComponent<RightFacingWalker>();hip=GetComponentInChildren<RightFacingHipLink>();
            aim=GetComponentInChildren<RightFacingAimRig>();down=GetComponent<FrontDownAimOverlay>();
            air=GetComponent<AirDownShotPose>();pelvis=GetComponent<RightFacingPelvisPreview>();
            original=GetComponent<SpriteRenderer>();physicsBody=GetComponent<Rigidbody2D>();shape=GetComponent<BoxCollider2D>();
            lastVisualPosition=transform.position;standingSize=shape.size;standingOffset=shape.offset;floor=down!=null?down.soleHeight:standingOffset.y-standingSize.y*.5f;
            foreach(var r in hip.GetComponentsInChildren<SpriteRenderer>(true))if(r.name=="Pelvis Underlay")underlay=r;
            legRig=new JumpLegTransitionRig(transform,original);legRig.PrepareCrouch();
        }
        private void Update()
        {
            if(networkDriven)return;
            if(readKeyboard&&RelayArenaPrototype.LiveInputBlocked)return;
            if(readKeyboard)
            {
#if ENABLE_INPUT_SYSTEM
                var keys=Keyboard.current;requested=keys!=null&&(keys.sKey.isPressed||keys.downArrowKey.isPressed||keys.leftCtrlKey.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
                requested=Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)||Input.GetKey(KeyCode.LeftControl);
#endif
            }
            if(death!=null&&death.IsDead){Exit();return;}
            if(legRig==null||!legRig.Ready||movement==null||!movement.enabled)return;
            if(!IsCrouched&&requested&&movement.Grounded)Enter();
            else if(IsCrouched&&(!movement.Grounded||!requested&&CanStand()))Exit();
        }
        private void Enter()
        {
            oldSpeed=movement.SpeedOverride;oldJumpBlocked=movement.JumpInputBlocked;oldDrop=hip.OverrideDropPixels;
            oldAimRestriction=aim.HorizontalAndUpOnly;airEnabled=air!=null&&air.enabled;
            downEnabled=down!=null&&down.enabled;
            originalHidden=original.forceRenderingOff;underlayHidden=underlay!=null&&underlay.forceRenderingOff;
            if(down!=null){down.Deactivate();down.enabled=false;}
            if(air!=null)air.enabled=false;
            if(pelvis!=null){oldPelvisSuppressed=pelvis.SuppressDisplay;pelvis.SuppressDisplay=true;pelvis.UpdateGeometry();}
            IsCrouched=true;phase=.2f;motion=0;FrameIndex=0;
            movement.SpeedOverride=crouchSpeed;movement.JumpInputBlocked=true;aim.HorizontalAndUpOnly=false;
            // Keep the foot height and enough headroom for the aiming torso.
            float colliderDrop=Mathf.Max(0,bodyDrop-.08f);
            shape.size=new Vector2(standingSize.x,standingSize.y-colliderDrop);
            shape.offset=standingOffset-Vector2.up*colliderDrop*.5f;
            ApplyHeight();
        }
        private void ApplyHeight()
        { ApplyHeight(new Vector2(0,bodyDrop)); }
        private void ApplyHeight(Vector2 offset)
        {
            hip.OverrideDropPixels=offset.y*204.8f;hip.ApplyFrame(walker.FrameIndex);
            hip.transform.localPosition+=Vector3.right*offset.x;
            original.forceRenderingOff=true;if(underlay!=null)underlay.forceRenderingOff=true;

        }
        private void LateUpdate()
        {
            Vector2 position=transform.position;
            float distance=position.x-lastVisualPosition.x;lastVisualPosition=position;
            if(!IsCrouched){legRig?.Hide();return;}
            float velocity=physicsBody.linearVelocity.x;
            if(!networkDriven&&!movement.HitReacting&&Mathf.Abs(velocity)>.025f&&Mathf.Abs(distance)<.5f)
                phase=Mathf.Repeat(phase+distance*(transform.localScale.x<0?-1:1)/Mathf.Max(.1f,cycleDistance),1);
            if(!networkDriven)motion=Mathf.MoveTowards(motion,!movement.HitReacting&&Mathf.Abs(velocity)>.025f?1:0,Time.deltaTime/.32f);
            FrameIndex=(int)(phase*40);
            ApplyHeight(legRig.DrawCrouch(phase,Mathf.SmoothStep(0,1,motion),bodyDrop,floor,cycleDistance));
        }
        private bool CanStand()
        {
            var scale=transform.lossyScale;
            var center=(Vector2)transform.TransformPoint(standingOffset);
            var size=Vector2.Scale(standingSize,new Vector2(Mathf.Abs(scale.x),Mathf.Abs(scale.y)))-Vector2.one*.025f;
            foreach(var hit in Physics2D.OverlapBoxAll(center,size,0,1))
            {
                if(hit.isTrigger||hit==shape||hit.transform.IsChildOf(transform))continue;
                if(Physics2D.GetIgnoreCollision(shape,hit))continue;
                var platform=hit.usedByEffector?hit.GetComponent<PlatformEffector2D>():null;
                // A one-way surface supports feet but cannot obstruct the head.
                if(platform!=null&&platform.enabled&&platform.useOneWay)continue;
                return false;
            }
            return true;
        }
        private void Exit()
        {
            if(!IsCrouched)return;
            IsCrouched=false;legRig.Hide();motion=0;
            shape.size=standingSize;shape.offset=standingOffset;
            movement.SpeedOverride=oldSpeed;movement.JumpInputBlocked=oldJumpBlocked;aim.HorizontalAndUpOnly=oldAimRestriction;
            hip.OverrideDropPixels=oldDrop;hip.ApplyFrame(walker.FrameIndex);
            original.forceRenderingOff=originalHidden;if(underlay!=null)underlay.forceRenderingOff=underlayHidden;
            if(air!=null)air.enabled=airEnabled;if(pelvis!=null){pelvis.SuppressDisplay=oldPelvisSuppressed;pelvis.UpdateGeometry();}if(down!=null)down.enabled=downEnabled;
        }
        public void CancelForDeath(){Exit();}
        private void OnDisable(){Exit();}
        private void OnDestroy(){legRig?.Dispose();}
        private void OnGUI()
        {
            if(networkDriven||ArenaObjectiveMatch.InReadyRoom)return;
            GUILayout.BeginArea(new Rect(15,215,310,94),GUI.skin.box);
            GUILayout.Label("앉기 시험 · S / ↓ / 왼쪽 Ctrl 유지");
            GUILayout.Label("A/D: 앉아서 이동 · 마우스/좌클릭: 좌우·위·비스듬한 아래 사격");
            GUILayout.Label(IsCrouched?"앉기: 측면 아래 조준 허용 · 점프·달리기 제한":"기존 서기·이동·점프");
            GUILayout.EndArea();
        }
    }
}

