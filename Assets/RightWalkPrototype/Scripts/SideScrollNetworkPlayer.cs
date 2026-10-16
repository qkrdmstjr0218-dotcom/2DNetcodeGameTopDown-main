using UnityEngine;
using Unity.Netcode;

namespace RightWalkPrototype
{
    [DefaultExecutionOrder(120)]
    public sealed class SideScrollNetworkPlayer : NetworkBehaviour
    {
        private readonly NetworkVariable<Vector3> position = new(default,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<Vector2> target = new(Vector2.right,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<int> frame = new(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> running = new(false,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> grounded = new(true,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<int> takeoffFrame = new(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> takeoffRunning = new(false,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
        private RightFacingAimRig aim;
        private RightFacingShotPreview shots;
        private SideScrollMapPreview movement;
        private RightFacingWalker walker;
        private RightFacingHipLink hip;
        private RunWalkPreview gait;
        private Rigidbody2D body;
        private Sprite[] walkFrames;
        private double nextServerShot;
        private float nextSnapshot;
        public int ReceivedShots { get; private set; }
        public override void OnNetworkSpawn()
        {
            aim=GetComponentInChildren<RightFacingAimRig>();shots=aim.GetComponent<RightFacingShotPreview>();
            walker=GetComponent<RightFacingWalker>();movement=GetComponent<SideScrollMapPreview>();
            hip=GetComponentInChildren<RightFacingHipLink>();gait=GetComponent<RunWalkPreview>();
            body=GetComponent<Rigidbody2D>();walkFrames=walker.frames;
            movement.enabled=IsOwner;movement.readKeyboard=IsOwner;walker.enabled=IsOwner;
            hip.enabled=IsOwner;aim.enabled=IsOwner;aim.mouseAim=IsOwner;shots.readMouse=IsOwner;
            body.simulated=IsOwner;
            if(IsOwner)
            {
                transform.position=new Vector3(OwnerClientId==0 ? -1.5f : 1.5f,0f,0f);
                position.Value=transform.position;
                shots.ShotRequested=RequestShot;
                movement.followCamera=Camera.main;
            }
            else transform.position=position.Value;
        }
        private void LateUpdate()
        {
            if(!IsSpawned)return;
            if(IsOwner)
            {
                if(Time.unscaledTime<nextSnapshot)return;
                nextSnapshot=Time.unscaledTime+.033f;
                position.Value=transform.position;
                target.Value=aim.AimDirection;
                frame.Value=walker.FrameIndex;running.Value=gait.IsRunning;grounded.Value=movement.Grounded;
                var airPose=GetComponent<AirDownShotPose>();
                if(airPose!=null){takeoffFrame.Value=airPose.TakeoffFrame;takeoffRunning.Value=airPose.TakeoffRunning;}
            }
            else
            {
                transform.position=Vector3.Lerp(transform.position,position.Value,1f-Mathf.Exp(-20f*Time.deltaTime));
                var frames=running.Value ? gait.runFrames : walkFrames;
                int index=Mathf.Clamp(frame.Value,0,frames.Length-1);
                GetComponent<SpriteRenderer>().sprite=frames[index];
                GetComponent<RightFacingPelvisPreview>().UpdateGeometry();
                hip.meanDropPixels=running.Value ? 8f : 1f;
                hip.dropAmplitudePixels=running.Value ? 10f : 1f;
                hip.phaseOffsetRadians=running.Value ? -Mathf.PI/2f : 0f;
                movement.SetRemoteGrounded(grounded.Value);
                GetComponent<AirDownShotPose>()?.ApplyNetworkPose(index,running.Value,takeoffFrame.Value,takeoffRunning.Value);
                hip.ApplyFrame(index);
                aim.SetAimDirection(target.Value);
            }
        }
        private void RequestShot(Vector2 origin, Vector2 direction) { FireServerRpc(origin,direction); }
        [ServerRpc]
        private void FireServerRpc(Vector2 origin, Vector2 direction)
        {
            if(!float.IsFinite(origin.x)||!float.IsFinite(origin.y)||!float.IsFinite(direction.x)||!float.IsFinite(direction.y))return;
            if(direction.sqrMagnitude<.9f||direction.sqrMagnitude>1.1f)return;
            if(Vector2.Distance(origin,transform.position)>3f)return;
            double now=NetworkManager.ServerTime.Time;
            if(now+.01<nextServerShot)return;
            nextServerShot=now+Mathf.Max(.01f,shots.shotInterval);
            direction.Normalize();
            // Server chooses the first obstruction; clients only render that approved path.
            var end=aim.TraceShot(origin,direction,Mathf.Max(.1f,aim.previewAimRange),out Collider2D hitCollider);
            float distance=Vector2.Distance(origin,end);
            ShowShotClientRpc(origin,direction,distance,hitCollider!=null);
            var damageTarget=hitCollider!=null ? hitCollider.GetComponentInParent<SideScrollDamageTarget>() : null;
            if(damageTarget!=null)StartCoroutine(ApplyImpact(damageTarget,damageTarget.Generation,distance/Mathf.Max(.1f,shots.bulletSpeed)));
        }
        private System.Collections.IEnumerator ApplyImpact(SideScrollDamageTarget damageTarget,int generation,float delay)
        {
            yield return new WaitForSeconds(delay);
            if(IsServer&&IsSpawned&&damageTarget!=null)damageTarget.ReceiveDamage(1,generation);
        }
        [ClientRpc]
        private void ShowShotClientRpc(Vector2 origin, Vector2 direction, float range, bool hit)
        {
            ReceivedShots++;
            shots.ShowShot(origin,direction,range,true,hit);
        }
        public override void OnNetworkDespawn()
        {
            if(shots!=null)shots.ShotRequested=null;
        }
    }
}
