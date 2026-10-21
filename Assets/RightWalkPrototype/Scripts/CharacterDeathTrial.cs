using System.Collections.Generic;
using UnityEngine;

namespace RightWalkPrototype
{
    [DefaultExecutionOrder(120), DisallowMultipleComponent]
    public sealed class CharacterDeathTrial : MonoBehaviour
    {
        // Original joints lead into the existing rest drawing at the matched scale.
        public Sprite middlePose,fallenPose,droppedWeapon;
        public int maxHealth=100;
        public float recoilDuration=.08f,buckleDuration=.18f,fallDuration=.34f;
        public int Health { get; private set; }
        public bool IsDead => Health<=0;
        public bool Grounded => grounded;
        public bool Settled => IsDead&&grounded&&(networkElapsed??Time.time-began)>=recoilDuration+buckleDuration+fallDuration;
        private readonly Dictionary<Behaviour,bool> controls=new Dictionary<Behaviour,bool>();
        private readonly Dictionary<Renderer,bool> visibility=new Dictionary<Renderer,bool>();
        private SideScrollMapPreview movement;
        private RightFacingShotPreview shots;
        private Rigidbody2D body;
        private BoxCollider2D shape;
        private RigidbodyConstraints2D constraints;
        private bool keyboard,mouse,grounded;
        private float began,sole;
        private Transform display;
        private NativeDeathRig rig;
        private float? networkElapsed;
        private bool? networkGrounded;
        public void SetNetworkPlayback(float elapsed,bool? isGrounded)
        { networkElapsed=Mathf.Max(0,elapsed);networkGrounded=isGrounded;if(isGrounded.HasValue)grounded=isGrounded.Value; }
        public void ApplyNetworkHealth(int health)
        {
            health=Mathf.Clamp(health,0,Mathf.Max(1,maxHealth));
            if(health>0)
            {
                if(IsDead)Revive(false);
                Health=health;networkElapsed=null;networkGrounded=null;
            }
            else if(!IsDead){Health=0;BeginDeath();}
        }
        private void Awake()
        {
            Health=Mathf.Max(1,maxHealth);movement=GetComponent<SideScrollMapPreview>();body=GetComponent<Rigidbody2D>();
            shape=GetComponent<BoxCollider2D>();shots=GetComponentInChildren<RightFacingShotPreview>();
        }
        public void ApplyDamage(int damage)
        {
            if(!isActiveAndEnabled||!Application.isPlaying||IsDead||damage<=0)return;
            Health=Mathf.Max(0,Health-damage);if(IsDead)BeginDeath();
        }
        private void Pause(Behaviour control){if(control!=null){controls[control]=control.enabled;control.enabled=false;}}
        private void BeginDeath()
        {
            GetComponent<CrouchTrial>()?.CancelForDeath();
            var hip=GetComponentInChildren<RightFacingHipLink>();var walker=GetComponent<RightFacingWalker>();var gait=GetComponent<RunWalkPreview>();
            if(hip==null||walker==null){Health=Mathf.Max(1,maxHealth);return;}
            var go=new GameObject("Death trial display");go.hideFlags=HideFlags.DontSave;display=go.transform;display.SetParent(transform,false);
            var air=GetComponent<AirDownShotPose>();
            rig=new NativeDeathRig(display,hip.transform,GetComponent<SpriteRenderer>(),walker.FrameIndex,gait!=null&&gait.IsRunning,!movement.Grounded,air!=null&&air.UseAlternatePose,fallenPose,droppedWeapon);
            if(!rig.Ready){rig.Dispose();Destroy(go);display=null;rig=null;Health=Mathf.Max(1,maxHealth);return;}
            var overlay=GetComponent<FrontDownAimOverlay>();sole=overlay!=null?overlay.soleHeight:shape.offset.y-shape.size.y*.5f;
            constraints=body.constraints;keyboard=movement.readKeyboard;if(shots!=null){mouse=shots.readMouse;shots.readMouse=false;}
            movement.SetInput(0);Pause(movement);Pause(walker);Pause(GetComponent<SideFacingPreview>());Pause(GetComponentInChildren<RightFacingAimRig>());
            Pause(GetComponent<AirDownShotPose>());Pause(overlay);Pause(GetComponent<FrontDownAimAirLegs>());Pause(GetComponent<FrontDownAimMovingLegs>());
            foreach(var r in GetComponentsInChildren<Renderer>(true))
            {
                if(r is LineRenderer||r.transform.IsChildOf(display))continue;
                visibility[r]=r.forceRenderingOff;r.forceRenderingOff=true;
            }
            // Keep the momentum at HP 0. Losing control must not stop a jump in mid-air.
            grounded=IsOnFloor();began=Time.time;rig.Pose(0,0,sole,grounded);
        }
        private bool IsOnFloor()
        {
            if(networkGrounded.HasValue)return networkGrounded.Value;
            var b=shape.bounds;return body.linearVelocity.y<=.1f&&Physics2D.BoxCast(new Vector2(b.center.x,b.min.y+.03f),new Vector2(b.size.x*.85f,.04f),0,Vector2.down,.08f,1).collider!=null;
        }
        private void FixedUpdate()
        {
            if(!IsDead)return;
            if(networkGrounded.HasValue){grounded=networkGrounded.Value;return;}
            grounded=IsOnFloor();
            if(grounded)body.linearVelocity=new Vector2(Mathf.MoveTowards(body.linearVelocity.x,0,8f*Time.fixedDeltaTime),body.linearVelocity.y);
        }
        private void LateUpdate()
        {
            if(!IsDead||display==null)return;
            foreach(var pair in visibility)if(pair.Key!=null)pair.Key.forceRenderingOff=true;
            float elapsed=networkElapsed ?? Time.time-began;
            float progress=Mathf.Clamp01((elapsed-recoilDuration)/Mathf.Max(.02f,buckleDuration+fallDuration));
            float recoil=7f*Mathf.SmoothStep(0,1,elapsed/Mathf.Max(.02f,recoilDuration));
            rig.Pose(progress,recoil,sole,grounded);
        }
        public void Revive()
        {
            var player=GetComponent<SideScrollNetworkPlayer>();
            if(player!=null&&player.IsSpawned){player.RequestRevive();return;}
            Revive(true);
        }
        private void Revive(bool resetPosition)
        {
            if(!IsDead)return;Health=Mathf.Max(1,maxHealth);
            networkElapsed=null;networkGrounded=null;
            if(rig!=null){rig.Dispose();rig=null;}
            if(display!=null){display.gameObject.SetActive(false);Destroy(display.gameObject);display=null;}
            foreach(var pair in visibility)if(pair.Key!=null)pair.Key.forceRenderingOff=pair.Value;visibility.Clear();
            foreach(var pair in controls)if(pair.Key!=null)pair.Key.enabled=pair.Value;controls.Clear();
            body.constraints=constraints;movement.SetInput(0);if(resetPosition)movement.ResetPosition();movement.readKeyboard=keyboard;if(shots!=null)shots.readMouse=mouse;
        }
        private void OnDisable(){if(display!=null&&IsDead)Revive(true);}
    }
}
