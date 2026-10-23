using UnityEngine;
using Unity.Netcode;

namespace RightWalkPrototype
{
    [DefaultExecutionOrder(130)]
    public sealed partial class SideScrollNetworkPlayer : NetworkBehaviour
    {
        // One snapshot keeps stance, jump and aiming state from different ticks from mixing.
        public struct MotionState : INetworkSerializable, System.IEquatable<MotionState>
        {
            public uint LifeVersion;
            public Vector3 Position;
            public Vector2 Velocity,Aim;
            public int Frame,TakeoffFrame,JumpCount;
            public float CrouchPhase,CrouchMotion,FrontDegrees;
            public bool HitReaction,Running,Grounded,TakeoffRunning,Crouched,FrontDown,FacingLeft,AirMirror,AirFacingLeft;
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T:IReaderWriter
            {
                serializer.SerializeValue(ref LifeVersion);
                serializer.SerializeValue(ref Position);serializer.SerializeValue(ref Velocity);serializer.SerializeValue(ref Aim);
                serializer.SerializeValue(ref Frame);serializer.SerializeValue(ref TakeoffFrame);serializer.SerializeValue(ref JumpCount);
                serializer.SerializeValue(ref CrouchPhase);serializer.SerializeValue(ref CrouchMotion);serializer.SerializeValue(ref FrontDegrees);
                serializer.SerializeValue(ref HitReaction);serializer.SerializeValue(ref Running);serializer.SerializeValue(ref Grounded);serializer.SerializeValue(ref TakeoffRunning);
                serializer.SerializeValue(ref Crouched);serializer.SerializeValue(ref FrontDown);serializer.SerializeValue(ref FacingLeft);
                serializer.SerializeValue(ref AirMirror);serializer.SerializeValue(ref AirFacingLeft);
            }
            public bool Equals(MotionState other) => LifeVersion==other.LifeVersion&&Position.Equals(other.Position)&&Velocity.Equals(other.Velocity)&&Aim.Equals(other.Aim)
                &&Frame==other.Frame&&TakeoffFrame==other.TakeoffFrame&&JumpCount==other.JumpCount
                &&CrouchPhase.Equals(other.CrouchPhase)&&CrouchMotion.Equals(other.CrouchMotion)&&FrontDegrees.Equals(other.FrontDegrees)
                &&HitReaction==other.HitReaction&&Running==other.Running&&Grounded==other.Grounded&&TakeoffRunning==other.TakeoffRunning
                &&Crouched==other.Crouched&&FrontDown==other.FrontDown&&FacingLeft==other.FacingLeft
                &&AirMirror==other.AirMirror&&AirFacingLeft==other.AirFacingLeft;
        }
        public struct LifeState : INetworkSerializable,System.IEquatable<LifeState>
        {
            public int Health;
            public uint Generation;
            public double DeathTime;
            public Vector3 SpawnPosition;
            public MotionState FatalPose;
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T:IReaderWriter
            {
                serializer.SerializeValue(ref Health);serializer.SerializeValue(ref Generation);
                serializer.SerializeValue(ref DeathTime);serializer.SerializeValue(ref SpawnPosition);serializer.SerializeValue(ref FatalPose);
            }
            public bool Equals(LifeState other)=>Health==other.Health&&Generation==other.Generation&&DeathTime.Equals(other.DeathTime)
                &&SpawnPosition.Equals(other.SpawnPosition)&&FatalPose.Equals(other.FatalPose);
        }
        private readonly NetworkVariable<MotionState> motion = new(new MotionState {Grounded=true,Aim=Vector2.right},NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<LifeState> life = new(new LifeState {Health=100});
        private readonly NetworkVariable<int> playerNumber = new(0);
        public int PlayerNumber=>playerNumber.Value;
        private readonly NetworkVariable<float> sprintEnergy=new(100);
        private readonly NetworkVariable<bool> sprintExhausted=new(false);
        private readonly NetworkVariable<bool> sprintWanted=new(false,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
        private float lastSprintUse;
        public float Stamina=>sprintEnergy.Value;
        public float MaximumStamina=>RelayArenaPrototype.Instance!=null?Mathf.Max(1,RelayArenaPrototype.Instance.staminaMaximum):100;
        public bool SprintExhausted=>sprintExhausted.Value;
        public bool SprintWanted=>sprintWanted.Value;
        public bool ResolveSprint(bool requested)
        {
            if(RelayArenaPrototype.Instance==null)return requested;
            if(!IsSpawned||!IsOwner)return false;
            requested&=!IsDead;
            if(sprintWanted.Value!=requested)sprintWanted.Value=requested;
            return requested&&!sprintExhausted.Value&&sprintEnergy.Value>.01f;
        }
        private void ResetSprintEnergy()
        {
            if(!IsServer)return;
            sprintEnergy.Value=MaximumStamina;sprintExhausted.Value=false;lastSprintUse=Time.time;
        }
        private void UpdateSprintEnergy()
        {
            var rules=RelayArenaPrototype.Instance;
            if(!IsServer||rules==null||IsDead||ArenaObjectiveMatch.Ended)return;
            bool current=IsOwner||motion.Value.LifeVersion==LifeVersion;
            float vx=IsOwner?body.linearVelocity.x:motion.Value.Velocity.x;
            bool reacting=IsOwner?movement.HitReacting:motion.Value.HitReaction;
            bool consuming=current&&!reacting&&sprintWanted.Value&&!sprintExhausted.Value&&Mathf.Abs(vx)>movement.speed+.05f;
            float value=sprintEnergy.Value;
            if(consuming)
            {
                value=Mathf.Max(0,value-Mathf.Max(0,rules.staminaDrain)*Time.deltaTime);lastSprintUse=Time.time;
                if(value<=.001f)sprintExhausted.Value=true;
            }
            else if(Time.time-lastSprintUse>=Mathf.Max(0,rules.staminaRecoveryDelay))
            {
                value=Mathf.Min(MaximumStamina,value+Mathf.Max(0,rules.staminaRecovery)*Time.deltaTime);
                if(sprintExhausted.Value&&value>=Mathf.Clamp(rules.staminaResume,1,MaximumStamina))sprintExhausted.Value=false;
            }
            sprintEnergy.Value=value;
        }
        [Min(1)] public int playerDamage=25;
        public int Health=>life.Value.Health;
        public int MaxHealth=>death!=null?Mathf.Max(1,death.maxHealth):100;
        public bool IsDead=>Health<=0;
        public uint LifeVersion=>life.Value.Generation;
        public bool HasCurrentLifePose=>appliedLifeVersion==LifeVersion;
        public float RespawnRemaining=>IsDead&&ArenaObjectiveMatch.Instance!=null&&ArenaObjectiveMatch.Instance.RoomFlow&&ArenaObjectiveMatch.Instance.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing?Mathf.Max(0,ArenaObjectiveMatch.Instance.RespawnSeconds-(float)(NetworkManager.ServerTime.Time-life.Value.DeathTime)):0;
        private CharacterDeathTrial death;
        private Vector2 standingSize,standingOffset;
        private uint appliedLifeVersion;
        private RightFacingAimRig aim;
        private RightFacingShotPreview shots;
        private SideScrollMapPreview movement;
        private RightFacingWalker walker;
        private RunWalkPreview gait;
        private CrouchTrial crouch;
        private AirDownShotPose air;
        private FrontDownAimOverlay front;
        private FrontDownAimAirLegs airLegs;
        private SideFacingPreview facing;
        private Rigidbody2D body;
        private UnityEngine.Rendering.SortingGroup characterSorting;
        private double nextServerShot;
        private float nextSnapshot;
        public int ReceivedShots { get; private set; }
        public override void OnNetworkSpawn()
        {
            if(IsServer)
            {
                var used=new System.Collections.Generic.HashSet<int>();
                foreach(var player in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))
                    if(player!=this&&player.IsSpawned)used.Add(player.PlayerNumber);
                int number=1;while(used.Contains(number))number++;
                playerNumber.Value=number;
            }
            aim=GetComponentInChildren<RightFacingAimRig>();shots=aim.GetComponent<RightFacingShotPreview>();
            // Keep every pose's parts together without changing their internal order.
            characterSorting=GetComponent<UnityEngine.Rendering.SortingGroup>();
            if(characterSorting==null)characterSorting=gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
            if(GetComponent<ArenaPlayerAppearance>()==null)gameObject.AddComponent<ArenaPlayerAppearance>();
            SetCharacterOrder(0,PlayerNumber);playerNumber.OnValueChanged+=SetCharacterOrder;
            walker=GetComponent<RightFacingWalker>();movement=GetComponent<SideScrollMapPreview>();
            gait=GetComponent<RunWalkPreview>();crouch=GetComponent<CrouchTrial>();air=GetComponent<AirDownShotPose>();
            front=GetComponent<FrontDownAimOverlay>();airLegs=GetComponent<FrontDownAimAirLegs>();facing=GetComponent<SideFacingPreview>();
            body=GetComponent<Rigidbody2D>();
            death=GetComponent<CharacterDeathTrial>();
            var shape=GetComponent<BoxCollider2D>();standingSize=shape.size;standingOffset=shape.offset;
            if(IsServer){life.Value=new LifeState {Health=MaxHealth,Generation=1,SpawnPosition=SpawnPosition()};ResetSprintEnergy();ResetSpecialAmmo();}
            appliedLifeVersion=LifeVersion;
            movement.enabled=IsOwner;movement.readKeyboard=IsOwner;walker.enabled=IsOwner;
            aim.mouseAim=IsOwner;aim.showAimPreview=IsOwner;shots.readMouse=IsOwner;
            crouch.SetNetworkDriven(!IsOwner);crouch.readKeyboard=IsOwner;air.SetNetworkDriven(!IsOwner);
            // Facing comes from the snapshot, including when the barrel points straight down.
            if(!IsOwner&&facing!=null){facing.turnWithMovement=false;facing.turnWithAim=false;}
            body.simulated=IsOwner;
            var arena=FindFirstObjectByType<RelayArenaPrototype>();
            if(arena!=null)
            {
                aim.previewAimRange=arena.weaponRange;
                movement.speed=arena.walkSpeed;movement.jumpSpeed=arena.jumpSpeed;
                gait.runSpeed=arena.runSpeed;crouch.crouchSpeed=arena.crouchSpeed;
                if(IsOwner&&GetComponent<ArenaPlatformMotor>()==null)gameObject.AddComponent<ArenaPlatformMotor>();
            }
            if(IsOwner)
            {
                movement.SetSpawnPosition(SpawnPosition());
                shots.ShotRequested=RequestShot;
                movement.followCamera=arena!=null?null:Camera.main;
                PublishMotion();
            }
            else{transform.position=motion.Value.LifeVersion==LifeVersion?motion.Value.Position:life.Value.SpawnPosition;ApplyRemoteMotion();}
        }
        private Vector3 SpawnPosition()
        {
            if(ArenaTutorial.Active)return ArenaTutorial.Instance.Checkpoint;
            var session=FindFirstObjectByType<SideScrollNetworkSession>();
            var match=ArenaObjectiveMatch.Instance;
            if(session!=null&&session.useReadyRoom&&session.arenaObjectivePrefab!=null&&RelayArenaPrototype.Instance!=null&&(match==null||match.RoomView))return ArenaReadyRoom.Spawn(PlayerNumber);
            if(session!=null&&session.playerSpawns!=null&&session.playerSpawns.Length>0)
            {
                var spawn=session.playerSpawns[Mathf.Max(0,PlayerNumber-1)%session.playerSpawns.Length];
                if(spawn!=null)return spawn.position;
            }
            return new Vector3(OwnerClientId==0?-1.5f:1.5f,0,0);
        }
        // Remote inputs are installed before the normal LateUpdate pose pipeline runs.
        private void Update()
        {
            if(!IsSpawned)return;
            var match=ArenaObjectiveMatch.Instance;
            if(IsOwner&&InstantShotgunEnabled&&SelectedAmmo==AmmoKind.Shotgun)SelectAmmo(AmmoKind.InstantShotgun);
            if(IsServer&&IsDead&&match!=null&&match.RoomFlow&&match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing&&RespawnRemaining<=0)ResetArenaLife(false);
            if(IsOwner&&ArenaObjectiveMatch.Instance!=null&&ArenaObjectiveMatch.Instance.RoomFlow)aim.showAimPreview=!ArenaObjectiveMatch.InReadyRoom||ArenaObjectiveMatch.PracticeRoom;
            if(appliedLifeVersion!=LifeVersion)
            {
                death.ApplyNetworkHealth(Health);movement.ClearHitReaction();fastHitReceived=-1;appliedLifeVersion=LifeVersion;
                if(IsOwner){sprintWanted.Value=false;SpecialHeld=false;}
                if(IsOwner){movement.SetSpawnPosition(life.Value.SpawnPosition);walker.ResumeFromPlantedPose(0);PublishMotion();}
                else transform.position=life.Value.SpawnPosition;
            }
            if(!IsOwner)ApplyRemoteMotion();
            UpdateSprintEnergy();UpdateShield();
            UpdateNormalAmmo();UpdatePracticeAmmo();
            if(IsDead)death.SetNetworkPlayback((float)(NetworkManager.ServerTime.Time-life.Value.DeathTime),IsOwner?(bool?)null:motion.Value.Grounded);
        }
        private void LateUpdate()
        {
            if(!IsSpawned)return;
            // Begin death after the normal rigs have installed the fatal pose this frame.
            death.ApplyNetworkHealth(Health);
            if(IsOwner)SendFastHitPose();
            if(!IsOwner||!movement.HitReacting&&Time.unscaledTime<nextSnapshot)return;
            nextSnapshot=Time.unscaledTime+.033f;PublishMotion();
        }
        private MotionState CaptureMotion()
        {
            return new MotionState {
                LifeVersion=LifeVersion,
                // Do not send the Rigidbody's visually interpolated (older)
                // transform during a hit. Send its resolved physics position.
                Position=!IsDead&&movement.HitReacting?new Vector3(body.position.x,body.position.y,transform.position.z):transform.position,Velocity=body.linearVelocity,Aim=aim.AimDirection,
                Frame=walker.FrameIndex,HitReaction=!IsDead&&movement.HitReacting,Running=gait.IsRunning,Grounded=death.IsDead?death.Grounded:movement.Grounded,JumpCount=movement.JumpCount,
                TakeoffFrame=air.TakeoffFrame,TakeoffRunning=air.TakeoffRunning,
                Crouched=crouch.IsCrouched,CrouchPhase=crouch.CyclePhase,CrouchMotion=crouch.MotionAmount,
                FrontDown=front.IsActive,FrontDegrees=front.LocalAimDegrees,FacingLeft=transform.localScale.x<0,
                AirMirror=airLegs.TakeoffMirror,AirFacingLeft=airLegs.TakeoffFacing<0
            };
        }
        private void PublishMotion(){motion.Value=CaptureMotion();}
        private void ApplyRemoteMotion()
        {
            var state=PresentationMotion();
            if(state.LifeVersion!=LifeVersion)return;
            // Display confirmed shove positions directly; ordinary movement
            // keeps its smoothing. Never extrapolate a hit through terrain.
            transform.position=state.HitReaction||Vector3.Distance(transform.position,state.Position)>2f ? state.Position
                : Vector3.Lerp(transform.position,state.Position,1f-Mathf.Exp(-20f*Time.deltaTime));
            if(death.IsDead){movement.SetNetworkState(state.Grounded,state.JumpCount,state.HitReaction);return;}
            if(IsDead)state=life.Value.FatalPose;
            facing?.SetFacing(state.FacingLeft?-1:1);
            movement.SetNetworkState(state.Grounded,state.JumpCount,state.HitReaction);
            gait.ApplyNetworkGait(state.HitReaction?false:state.Running);walker.ApplyNetworkFrame(state.Frame,state.HitReaction?Vector2.zero:state.Velocity);
            crouch.ApplyNetworkPose(state.Crouched,state.CrouchPhase,state.CrouchMotion);
            air.SetNetworkPose(state.Frame,state.Running,state.TakeoffFrame,state.TakeoffRunning);
            airLegs.SetNetworkPose(state.AirMirror,state.AirFacingLeft?-1:1);
            aim.SetNetworkAim(state.Aim,state.FrontDown,state.FrontDegrees);
        }
        private void RequestShot(Vector2 origin, Vector2 direction)
        {
            if(!IsSpawned||!IsOwner||IsDead)return;
            if(GrenadeActive)FireGrenadeServerRpc(origin,direction,GrenadeLaunchSpeed,LifeVersion);
            else if(RifleActive)FireRifleServerRpc(origin,direction,LifeVersion);
            else if(InstantShotgunActive)FireInstantShotgunServerRpc(origin,direction,LifeVersion);
            else if(ShotgunActive)FireShotgunServerRpc(origin,direction);
            else if(NormalAmmoEnabled){if(NormalAmmo>=1)FireNormalAmmoServerRpc(origin,direction,LifeVersion);}
            else FireServerRpc(origin,direction);
        }
        private void SetCharacterOrder(int previous,int number){characterSorting.sortingOrder=10+number;}
        [ServerRpc]
        private void FireServerRpc(Vector2 origin, Vector2 direction)
        {
            // Preserved for the older unlimited trial scenes and review tools.
            if(NormalAmmoEnabled)return;
            FireNormalServer(origin,direction);
        }
        [ServerRpc]
        private void FireNormalAmmoServerRpc(Vector2 origin,Vector2 direction,uint generation)
        {
            if(!NormalAmmoEnabled||generation!=LifeVersion)return;
            FireNormalServer(origin,direction);
        }
        private void FireNormalServer(Vector2 origin,Vector2 direction)
        {
            if(!IsServer||!IsSpawned)return;
            if(IsDead||ArenaObjectiveMatch.CombatBlocked)return;
            if(!float.IsFinite(origin.x)||!float.IsFinite(origin.y)||!float.IsFinite(direction.x)||!float.IsFinite(direction.y))return;
            if(direction.sqrMagnitude<.9f||direction.sqrMagnitude>1.1f)return;
            Vector3 serverPosition=IsOwner?transform.position:motion.Value.LifeVersion==LifeVersion?motion.Value.Position:life.Value.SpawnPosition;
            if(Vector2.Distance(origin,serverPosition)>3f)return;
            double now=NetworkManager.ServerTime.Time;
            if(!ServerFireReady(AmmoKind.Normal,now))return;
            if(!SpendNormalAmmo(now))return;
            MarkServerFired(AmmoKind.Normal,now);
            direction.Normalize();
            // Check the barrel's path too: a close body or wall can be behind the
            // muzzle, but must still intercept the shot. Keep the muzzle range.
            float barrel=Mathf.Max(0,Vector2.Dot(origin-(Vector2)ServerHitBounds().center,direction));
            Vector2 start=origin-direction*barrel;
            var end=aim.TraceShot(start,direction,barrel+Mathf.Max(.1f,aim.previewAimRange),out Collider2D hitCollider,true);
            float distance=Vector2.Distance(start,end);
            SideScrollNetworkPlayer victim=null;
            foreach(var candidate in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))
            {
                if(candidate==this||!candidate.IsSpawned||candidate.IsDead)continue;
                var bounds=candidate.ServerHitBounds();
                bool intersects=bounds.IntersectRay(new Ray(start,direction),out float hitDistance);
                if(bounds.Contains(new Vector3(start.x,start.y,bounds.center.z))){intersects=true;hitDistance=0;}
                if(intersects&&hitDistance>=0&&hitDistance<distance)
                {distance=hitDistance;victim=candidate;}
            }
            // An obstruction inside the barrel gets its impact there, without
            // drawing a bullet backwards or a muzzle flash beyond the obstacle.
            Vector2 visualOrigin=distance<barrel?start+direction*distance:origin;
            float travel=Mathf.Max(0,distance-barrel);
            ShowShotClientRpc(visualOrigin,direction,travel,hitCollider!=null||victim!=null);
            if(victim!=null)
            {StartCoroutine(ApplyPlayerImpact(victim,victim.LifeVersion,travel/Mathf.Max(.1f,shots.bulletSpeed)));return;}
            var enemy=hitCollider!=null?hitCollider.GetComponentInParent<ArenaPatrolEnemy>():null;
            if(enemy!=null){StartCoroutine(ApplyPatrolImpact(enemy.Match,enemy.Id,enemy.Generation,1,travel/Mathf.Max(.1f,shots.bulletSpeed)));return;}
            var damageTarget=hitCollider!=null ? hitCollider.GetComponentInParent<SideScrollDamageTarget>() : null;
            if(damageTarget!=null)StartCoroutine(ApplyImpact(damageTarget,damageTarget.Generation,travel/Mathf.Max(.1f,shots.bulletSpeed)));
        }
        internal Bounds ArenaHitBounds=>ServerHitBounds();
        private Bounds ServerHitBounds()
        {
            var state=IsOwner?CaptureMotion():motion.Value;
            bool current=state.LifeVersion==LifeVersion;
            Vector3 position=current?state.Position:life.Value.SpawnPosition;
            var size=standingSize;var offset=standingOffset;
            if(current&&state.Crouched)
            {float drop=Mathf.Max(0,crouch.bodyDrop-.08f);size.y-=drop;offset.y-=drop*.5f;}
            var scale=transform.lossyScale;
            return new Bounds(position+new Vector3(offset.x*scale.x,offset.y*scale.y,0),new Vector3(size.x*Mathf.Abs(scale.x),size.y*Mathf.Abs(scale.y),.2f));
        }
        private System.Collections.IEnumerator ApplyPatrolImpact(ArenaObjectiveMatch match,int id,int generation,int damage,float delay)
        {
            if(delay>0)yield return new WaitForSeconds(delay);
            if(IsServer&&IsSpawned&&match!=null)match.DamagePatrolServer(id,generation,damage);
        }
        private System.Collections.IEnumerator ApplyPlayerImpact(SideScrollNetworkPlayer victim,uint generation,float delay)
        {
            yield return new WaitForSeconds(delay);
            if(IsServer&&IsSpawned&&victim!=null)victim.ReceiveServerDamage(playerDamage,generation,this);
        }
        public void ReceiveServerDamage(int damage,uint generation,SideScrollNetworkPlayer attacker=null,bool bypassShield=false)
        {
            if(!IsServer||!IsSpawned||IsDead||damage<=0||generation!=LifeVersion||ArenaObjectiveMatch.CombatBlocked)return;
            if(!bypassShield)damage=AbsorbShield(damage);
            if(damage<=0)return;
            var state=life.Value;state.Health=Mathf.Max(ArenaObjectiveMatch.PracticeRoom?1:0,state.Health-damage);
            if(state.Health==0){state.FatalPose=IsOwner?CaptureMotion():motion.Value;state.DeathTime=NetworkManager.ServerTime.Time;}
            life.Value=state;
            if(state.Health==0)ArenaObjectiveMatch.Instance?.RecordPlayerKill(attacker,this);
        }
        public int GrantHealthServer(int amount,uint generation)
        {
            var match=ArenaObjectiveMatch.Instance;
            if(!IsServer||!IsSpawned||IsDead||!HasCurrentLifePose||amount<=0||generation!=LifeVersion||match==null||!match.RoomFlow||(match.State.Value.Phase!=ArenaObjectiveMatch.Stage.Playing&&!(ArenaTutorial.Active&&ArenaObjectiveMatch.PracticeRoom)))return 0;
            int healed=Mathf.Min(amount,Mathf.Max(0,MaxHealth-Health));if(healed==0)return 0;
            var state=life.Value;state.Health+=healed;life.Value=state;return healed;
        }
        public void RequestRevive(){if(IsSpawned&&IsOwner&&IsDead)ReviveServerRpc();}
        [ServerRpc]
        private void ReviveServerRpc()
        {
            if(ArenaObjectiveMatch.Instance!=null&&ArenaObjectiveMatch.Instance.RoomFlow)return;
            if(!IsDead||ArenaObjectiveMatch.CombatBlocked)return;
            life.Value=new LifeState {Health=MaxHealth,Generation=LifeVersion+1,SpawnPosition=SpawnPosition()};
            nextServerShot=0;ResetSprintEnergy();ResetSpecialAmmo();
        }
        public void ResetForArenaRound()=>ResetArenaLife(true);
        void ResetArenaLife(bool refillAmmo)
        {
            if(!IsServer||!IsSpawned)return;
            Vector3 spawn=SpawnPosition();
            var map=RelayArenaPrototype.Instance!=null?RelayArenaPrototype.Instance.GetComponent<ArenaMapSettings>():null;
            if(!refillAmmo&&map!=null&&map.TryChooseRespawn(this,out var chosen))spawn=chosen;
            life.Value=new LifeState{Health=MaxHealth,Generation=LifeVersion+1,SpawnPosition=spawn};nextServerShot=0;ResetSprintEnergy();
            if(refillAmmo)ResetSpecialAmmo();else{ResetNormalAmmo();ResetFireTiming();}
        }
        private void OnGUI()
        {
            if(!IsSpawned||Camera.main==null||ArenaObjectiveMatch.TransitionActive)return;
            DrawWorldStamina();DrawShieldHud();DrawAmmoSelectionHint();
            if(ArenaObjectiveMatch.InReadyRoom&&!ArenaTutorial.Active)
            {
                var at=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*2.45f);
                if(at.z>0)GUI.Label(new Rect(at.x-75,Screen.height-at.y,150,25),"P"+PlayerNumber+(IsOwner?" (나)":"")+" · "+(ArenaObjectiveMatch.Instance.IsReady(OwnerClientId)?"준비 완료":"준비 중"),new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,normal={textColor=ArenaPlayerAppearance.Accent(PlayerNumber)}});
                if(IsOwner)DrawAmmoHud();
                return;
            }
            var point=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*(crouch.IsCrouched?1.95f:2.45f));
            if(point.z>0)
            {
                var rect=new Rect(point.x-38,Screen.height-point.y,76,7);var color=GUI.color;
                GUI.color=new Color(.08f,.08f,.08f,.9f);GUI.DrawTexture(rect,Texture2D.whiteTexture);
                rect.width*=Health/(float)MaxHealth;GUI.color=ArenaPlayerAppearance.Accent(PlayerNumber);
                GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=color;
                GUI.color=ArenaPlayerAppearance.Accent(PlayerNumber);
                GUI.Label(new Rect(point.x-38,Screen.height-point.y-23,28,22),"P"+PlayerNumber);
                GUI.color=color;
                GUI.Label(new Rect(point.x-10,Screen.height-point.y-23,100,22),Health+" / "+MaxHealth);
            }
            if(!IsOwner)return;
            bool showStamina=RelayArenaPrototype.Instance!=null;float panelHeight=showStamina?130:85;
            GUILayout.BeginArea(new Rect(15,Screen.height-panelHeight-15,245,panelHeight),GUI.skin.box);
            GUILayout.Label("내 P"+PlayerNumber+" · HP: "+Health+" / "+MaxHealth);
            if(showStamina)
            {
                GUILayout.Label("스태미나 "+Stamina.ToString("0")+" / "+MaximumStamina.ToString("0")+(SprintExhausted?" · 회복 중":""));
                var bar=GUILayoutUtility.GetRect(220,10);var oldColor=GUI.color;
                GUI.color=new Color(.08f,.1f,.12f,.95f);GUI.DrawTexture(bar,Texture2D.whiteTexture);
                bar.width*=Mathf.Clamp01(Stamina/MaximumStamina);GUI.color=SprintExhausted?new Color(1,.65f,.25f):new Color(.25f,.8f,.9f);GUI.DrawTexture(bar,Texture2D.whiteTexture);GUI.color=oldColor;
            }
            if(IsDead){GUILayout.Label("쓰러짐 · 이동/사격 불가");if(ArenaObjectiveMatch.Instance!=null&&ArenaObjectiveMatch.Instance.RoomFlow)GUILayout.Label(ArenaObjectiveMatch.Ended?"경기가 종료되었습니다.":"자동 부활까지 "+RespawnRemaining.ToString("0.0")+"초");else if(GUILayout.Button("복귀 (시험)"))RequestRevive();}
            GUILayout.EndArea();
            DrawAmmoHud();
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
            ClearShieldVisual();
            playerNumber.OnValueChanged-=SetCharacterOrder;
            if(shots!=null)shots.ShotRequested=null;
        }
    }
}
