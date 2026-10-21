using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkPlayer
    {
        public enum AmmoKind : byte { Normal, Shotgun, InstantShotgun, Rifle, Grenade }
        public const int ShotgunCapacity=6, ShotgunPellets=5, ShotgunPelletDamage=8;
        public const float ShotgunInterval=.7f, ShotgunRange=5f, ShotgunSpread=12f, ShotgunPush=6f, ShotgunHitSeconds=.28f, ShotgunPelletSpeed=18f;
        private readonly NetworkVariable<AmmoKind> selectedAmmo=new(AmmoKind.Shotgun,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<int> shotgunRounds=new(ShotgunCapacity);
        public AmmoKind SelectedAmmo=>selectedAmmo.Value;
        public int FlyingShotgunRounds=>shotgunRounds.Value;
        public int ShotgunRounds=>InstantShotgunEnabled?InstantShotgunRounds:FlyingShotgunRounds;
        public bool SpecialHeld {get;private set;}
        public bool SpecialAmmoEnabled=>RelayArenaPrototype.Instance!=null;
        public bool ShotgunActive=>SpecialAmmoEnabled&&SpecialHeld&&SelectedAmmo==AmmoKind.Shotgun&&FlyingShotgunRounds>0&&!IsDead;
        public int GrantShotgunAmmoServer(int amount)
        {
            if(!IsServer||!IsSpawned||IsDead||amount<=0||ArenaObjectiveMatch.CombatBlocked)return 0;
            int granted=Mathf.Min(amount,ShotgunCapacity-ShotgunRounds);
            if(InstantShotgunEnabled)instantShotgunRounds.Value+=granted;else shotgunRounds.Value+=granted;return granted;
        }
        public const float PatrolHitSeconds=.12f,PatrolPush=2.4f;
        internal void PushFromPatrolServer(float direction)
        {
            if(!IsServer||!IsSpawned||IsDead||ArenaObjectiveMatch.CombatBlocked)return;
            allowedHitPoseUntil=NetworkManager.ServerTime.Time+PatrolHitSeconds+.3;
            PatrolPushClientRpc(Vector2.right*Mathf.Sign(direction)*PatrolPush,LifeVersion);
        }
        [ClientRpc]
        void PatrolPushClientRpc(Vector2 impulse,uint generation)
        {
            if(IsOwner&&!IsDead&&LifeVersion==generation&&HasCurrentLifePose)
            {movement.ApplyHitPush(impulse,PatrolHitSeconds);PublishMotion();}
        }
        public int ReceivedShotgunBlasts {get;private set;}
        // A short hit-only presentation channel bypasses NetworkVariable tick
        // batching. Physics remains on the owner; these are resolved poses,
        // never predicted pushes through a wall.
        private MotionState fastHitPose;
        private uint sentHitPose,acceptedHitPose,receivedHitPose;
        private float fastHitReceived=-1,nextFastHitPose;
        private double allowedHitPoseUntil;
        private bool sentHitLastFrame;
        private void SendFastHitPose()
        {
            bool hit=!IsDead&&movement.HitReacting;
            if(hit&&Time.unscaledTime>=nextFastHitPose||!hit&&sentHitLastFrame)
            {
                nextFastHitPose=Time.unscaledTime+.016f;
                FastHitPoseServerRpc(CaptureMotion(),++sentHitPose);
            }
            sentHitLastFrame=hit;
        }
        [ServerRpc(Delivery=RpcDelivery.Unreliable)]
        private void FastHitPoseServerRpc(MotionState pose,uint sequence)
        {
            if(!IsSpawned||IsDead||pose.LifeVersion!=LifeVersion||NetworkManager.ServerTime.Time>allowedHitPoseUntil||
                unchecked((int)(sequence-acceptedHitPose))<=0||!float.IsFinite(pose.Position.x)||!float.IsFinite(pose.Position.y)||!float.IsFinite(pose.Position.z)||
                !float.IsFinite(pose.Velocity.x)||!float.IsFinite(pose.Velocity.y)||!float.IsFinite(pose.Aim.x)||!float.IsFinite(pose.Aim.y))return;
            acceptedHitPose=sequence;FastHitPoseClientRpc(pose,sequence);
        }
        [ClientRpc(Delivery=RpcDelivery.Unreliable)]
        private void FastHitPoseClientRpc(MotionState pose,uint sequence)
        {
            if(IsOwner||!IsSpawned||IsDead||pose.LifeVersion!=LifeVersion||unchecked((int)(sequence-receivedHitPose))<=0)return;
            receivedHitPose=sequence;fastHitPose=pose;fastHitReceived=Time.unscaledTime;
        }
        private MotionState PresentationMotion()=>fastHitReceived>=0&&Time.unscaledTime-fastHitReceived<.08f&&fastHitPose.LifeVersion==LifeVersion?fastHitPose:motion.Value;
        public void SetSpecialHeld(bool held){if(IsOwner){SpecialHeld=held&&!IsDead&&!RelayArenaPrototype.ControlsBlocked;NotifyFireMode();}}
        public void SelectAmmo(AmmoKind kind)
        {
            if(!IsSpawned||!IsOwner||IsDead||RelayArenaPrototype.ControlsBlocked||!SpecialAmmoEnabled)return;
            if(InstantShotgunEnabled&&kind==AmmoKind.Shotgun)kind=AmmoKind.InstantShotgun;
            if(kind==AmmoKind.Normal||kind==AmmoKind.Shotgun||kind==AmmoKind.InstantShotgun&&InstantShotgunEnabled||kind==AmmoKind.Rifle&&RifleEnabled||kind==AmmoKind.Grenade&&NormalAmmoEnabled){if(selectedAmmo.Value!=kind){selectedAmmo.Value=kind;NotifyFireMode(true);}}
        }
        public void CycleAmmo(int direction)
        {
            if(direction==0)return;
            if(RifleEnabled)
            {int index=SelectedAmmo==AmmoKind.Normal?0:SelectedAmmo==AmmoKind.Grenade?3:SelectedAmmo==AmmoKind.Rifle?2:1;index=(index+(direction>0?1:3))%4;SelectAmmo(index==0?AmmoKind.Normal:index==1?AmmoKind.InstantShotgun:index==2?AmmoKind.Rifle:AmmoKind.Grenade);}
            else SelectAmmo(SelectedAmmo==AmmoKind.Normal?AmmoKind.Shotgun:AmmoKind.Normal);
        }
        const double PracticeSupplyInterval=2;
        double nextPracticeSupply;
        void UpdatePracticeAmmo()
        {
            UpdateGrenadePracticeAmmo();UpdateRiflePracticeAmmo();UpdateInstantPracticeAmmo();
            if(InstantShotgunEnabled)return;
            if(!IsServer||!IsSpawned)return;
            double now=NetworkManager.ServerTime.Time;
            if(!ArenaObjectiveMatch.PracticeRoom||IsDead||ShotgunRounds>=ShotgunCapacity){nextPracticeSupply=now+PracticeSupplyInterval;return;}
            if(now<nextPracticeSupply)return;
            shotgunRounds.Value++;nextPracticeSupply=now+PracticeSupplyInterval;
        }
        private void ResetSpecialAmmo(){if(IsServer){shotgunRounds.Value=ShotgunCapacity;instantShotgunRounds.Value=ShotgunCapacity;rifleRounds.Value=RifleCapacity;grenadeRounds.Value=GrenadeCapacity;ResetNormalAmmo();ResetFireTiming();}}

        private struct PelletPath : INetworkSerializable
        {
            public Vector2 Origin,Direction;
            public float Range;
            public bool Hit;
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T:IReaderWriter
            {serializer.SerializeValue(ref Origin);serializer.SerializeValue(ref Direction);serializer.SerializeValue(ref Range);serializer.SerializeValue(ref Hit);}
        }
        private sealed class PelletImpact
        {
            public SideScrollNetworkPlayer Player;
            public SideScrollDamageTarget Target;public ArenaPatrolEnemy Enemy;
            public uint Life;
            public int Generation,Count;
            public float Delay=float.MaxValue;
        }
        [ServerRpc]
        private void FireShotgunServerRpc(Vector2 origin,Vector2 direction)
        {if(!InstantShotgunEnabled)FireSpreadServer(origin,direction,false,0);}
        void FireSpreadServer(Vector2 origin,Vector2 direction,bool instant,uint generation)
        {
            if(!IsServer||!IsSpawned||!SpecialAmmoEnabled||IsDead||ArenaObjectiveMatch.CombatBlocked)return;
            if(instant?(!InstantShotgunEnabled||generation!=LifeVersion||InstantShotgunRounds<=0):shotgunRounds.Value<=0)return;
            if(!float.IsFinite(origin.x)||!float.IsFinite(origin.y)||!float.IsFinite(direction.x)||!float.IsFinite(direction.y)||direction.sqrMagnitude<.9f||direction.sqrMagnitude>1.1f)return;
            Vector3 position=IsOwner?transform.position:motion.Value.LifeVersion==LifeVersion?motion.Value.Position:life.Value.SpawnPosition;
            if(Vector2.Distance(origin,position)>3f)return;
            double now=NetworkManager.ServerTime.Time;
            if(!ServerFireReady(instant?AmmoKind.InstantShotgun:AmmoKind.Shotgun,now))return;
            MarkServerFired(instant?AmmoKind.InstantShotgun:AmmoKind.Shotgun,now);
            if(instant){instantShotgunRounds.Value--;nextInstantPracticeSupply=now+PracticeSupplyInterval;}
            else{shotgunRounds.Value--;nextPracticeSupply=now+PracticeSupplyInterval;}
            direction.Normalize();
            var players=FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
            var paths=new PelletPath[ShotgunPellets];
            var impacts=new Dictionary<Object,PelletImpact>();
            for(int i=0;i<paths.Length;i++)
            {
                float degrees=Mathf.Lerp(-ShotgunSpread*.5f,ShotgunSpread*.5f,i/(float)(paths.Length-1));
                // Only the trial variant jitters. The server sends these exact rays to all peers.
                if(instant)degrees=Random.Range(Mathf.Max(-ShotgunSpread*.5f,degrees-.8f),Mathf.Min(ShotgunSpread*.5f,degrees+.8f));
                float angle=degrees*Mathf.Deg2Rad;
                Vector2 ray=new Vector2(direction.x*Mathf.Cos(angle)-direction.y*Mathf.Sin(angle),direction.x*Mathf.Sin(angle)+direction.y*Mathf.Cos(angle));
                float barrel=Mathf.Max(0,Vector2.Dot(origin-(Vector2)ServerHitBounds().center,ray));
                Vector2 start=origin-ray*barrel;
                Vector2 end=aim.TraceShot(start,ray,barrel+ShotgunRange,out Collider2D hit,true);
                float distance=Vector2.Distance(start,end);SideScrollNetworkPlayer victim=null;
                foreach(var candidate in players)
                {
                    if(candidate==this||!candidate.IsSpawned||candidate.IsDead)continue;
                    var bounds=candidate.ServerHitBounds();
                    bool intersects=bounds.IntersectRay(new Ray(start,ray),out float hitDistance);
                    if(bounds.Contains(new Vector3(start.x,start.y,bounds.center.z))){intersects=true;hitDistance=0;}
                    if(intersects&&hitDistance>=0&&hitDistance<distance){distance=hitDistance;victim=candidate;}
                }
                float travel=Mathf.Max(0,distance-barrel);
                paths[i]=new PelletPath{Origin=distance<barrel?start+ray*distance:origin,Direction=ray,Range=travel,Hit=victim!=null||hit!=null};
                var target=victim==null&&hit!=null?hit.GetComponentInParent<SideScrollDamageTarget>():null;
                var enemy=victim==null&&hit!=null?hit.GetComponentInParent<ArenaPatrolEnemy>():null;
                Object key=victim!=null?(Object)victim:enemy!=null?(Object)enemy:target;
                if(key==null)continue;
                if(!impacts.TryGetValue(key,out var impact))
                {impact=new PelletImpact{Player=victim,Target=target,Enemy=enemy,Life=victim!=null?victim.LifeVersion:0,Generation=enemy!=null?enemy.Generation:target!=null?target.Generation:0};impacts.Add(key,impact);}
                impact.Count++;impact.Delay=instant?0:Mathf.Min(impact.Delay,travel/ShotgunPelletSpeed);
            }
            if(instant)ShowInstantShotgunClientRpc(paths);else ShowShotgunClientRpc(paths);
            foreach(var impact in impacts.Values)StartCoroutine(ApplyShotgunImpact(impact,direction));
        }
        private IEnumerator ApplyShotgunImpact(PelletImpact impact,Vector2 direction)
        {
            if(impact.Delay>0)yield return new WaitForSeconds(impact.Delay);
            if(!IsServer||!IsSpawned||ArenaObjectiveMatch.CombatBlocked)yield break;
            if(impact.Player!=null)
            {
                var victim=impact.Player;
                if(!victim.IsSpawned||victim.IsDead||victim.LifeVersion!=impact.Life)yield break;
                victim.ReceiveServerDamage(impact.Count*ShotgunPelletDamage,impact.Life);
                if(!victim.IsDead)
                {
                    victim.allowedHitPoseUntil=NetworkManager.ServerTime.Time+ShotgunHitSeconds+.3;
                    victim.ShotgunPushClientRpc(direction.normalized*ShotgunPush,impact.Life);
                }
            }
            else if(impact.Enemy!=null)impact.Enemy.Match.DamagePatrolServer(impact.Enemy.Id,impact.Generation,impact.Count);
            else if(impact.Target!=null)impact.Target.ReceiveDamage(impact.Count,impact.Generation);
        }
        [ClientRpc]
        private void ShowShotgunClientRpc(PelletPath[] paths)
        {
            ReceivedShots++;ReceivedShotgunBlasts++;
            for(int i=0;i<paths.Length;i++)shots.ShowShot(paths[i].Origin,paths[i].Direction,paths[i].Range,true,paths[i].Hit,i==0,true);
        }
        [ClientRpc]
        private void ShotgunPushClientRpc(Vector2 impulse,uint generation)
        {
            if(IsServer&&!IsDead&&LifeVersion==generation)allowedHitPoseUntil=NetworkManager.ServerTime.Time+ShotgunHitSeconds+.3;
            if(IsOwner&&!IsDead&&LifeVersion==generation&&HasCurrentLifePose)
            {movement.ApplyShotgunPush(impulse);PublishMotion();}
        }
        private void DrawAmmoHud()
        {
            if(!SpecialAmmoEnabled)return;
            if(NormalAmmoEnabled){DrawNormalAmmoHud();return;}
            GUILayout.BeginArea(new Rect(Screen.width-315,Screen.height-140,300,125),GUI.skin.box);
            GUILayout.Label("탄환 선택 Q / E · 우클릭 유지: 선택탄");
            bool enabled=GUI.enabled;GUI.enabled=!IsDead&&!RelayArenaPrototype.ControlsBlocked;
            GUILayout.BeginHorizontal();
            GUILayout.Box((SelectedAmmo==AmmoKind.Normal?"▶ ":"")+"일반탄 ∞");
            GUILayout.Box((SelectedAmmo==AmmoKind.Shotgun?"▶ ":"")+"샷건 "+ShotgunRounds+" / "+ShotgunCapacity);
            GUILayout.EndHorizontal();GUI.enabled=enabled;
            GUILayout.Label("현재: "+(ShotgunActive?"샷건":"일반탄")+(SpecialHeld&&SelectedAmmo==AmmoKind.Shotgun&&ShotgunRounds==0?" · 샷건 고갈":""));
            GUILayout.Label("좌클릭 발사 · 우클릭 해제: 일반탄");
            GUILayout.EndArea();
        }
    }
}
