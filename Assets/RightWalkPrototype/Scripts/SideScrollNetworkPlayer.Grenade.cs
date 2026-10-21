using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkPlayer
    {
        public const int GrenadeCapacity=3,GrenadeDamage=40,GrenadeMinimumDamage=10,GrenadeEnemyDamage=3,GrenadeEnemyMinimumDamage=1;
        public const float GrenadeInterval=1,GrenadePushSpeed=2.2f,GrenadeHitSeconds=.14f;
        public int ReceivedGrenadePushes{get;private set;}
        public Vector2 LastGrenadePush{get;private set;}
        readonly NetworkVariable<int> grenadeRounds=new(GrenadeCapacity);
        double nextGrenadePracticeSupply;uint grenadeSequence;
        public bool GrenadeActive=>NormalAmmoEnabled&&SpecialHeld&&SelectedAmmo==AmmoKind.Grenade&&!IsDead;
        public int GrenadeRounds=>grenadeRounds.Value;
        public float GrenadeLaunchSpeed=>ArenaGrenadeBallistics.Speed(Vector2.Distance(aim.MuzzlePosition,aim.AimPoint));
        public int ReceivedGrenadeShots{get;private set;}
        public int ReceivedGrenadeBursts{get;private set;}
        public Vector2 LastGrenadeBurst{get;private set;}
        readonly List<Vector2> grenadePreview=new();
        void UpdateGrenadePracticeAmmo()
        {
            if(!IsServer||!IsSpawned||!NormalAmmoEnabled)return;
            double now=NetworkManager.ServerTime.Time;
            if(!ArenaObjectiveMatch.PracticeRoom||IsDead||GrenadeRounds>=GrenadeCapacity){nextGrenadePracticeSupply=now+PracticeSupplyInterval;return;}
            if(now>=nextGrenadePracticeSupply){grenadeRounds.Value++;nextGrenadePracticeSupply=now+PracticeSupplyInterval;}
        }
        public bool PredictGrenade(List<Vector2> points,out Vector2 end)=>ArenaGrenadeBallistics.Predict(this,aim.MuzzlePosition,aim.AimDirection*GrenadeLaunchSpeed,points,out end);
        public void DrawGrenadePreview(Camera camera)
        {
            if(!IsOwner||IsDead||RelayArenaPrototype.ControlsBlocked||ArenaObjectiveMatch.CombatBlocked||ArenaObjectiveMatch.RoomPointerOverUI||Event.current.type!=EventType.Repaint)return;
            bool hit=PredictGrenade(grenadePreview,out var end);var color=GUI.color;GUI.color=ActiveAmmoEmpty?new Color(1,.2f,.15f,.8f):new Color(.65f,1,.3f,.7f);
            for(int i=1;i<grenadePreview.Count;i++)
            {var a=camera.WorldToScreenPoint(grenadePreview[i-1]);var b=camera.WorldToScreenPoint(grenadePreview[i]);if(a.z>0&&b.z>0&&i%2==1)CursorLine(new Vector2(a.x,Screen.height-a.y),new Vector2(b.x,Screen.height-b.y),1.5f);}
            var at=camera.WorldToScreenPoint(end);if(hit&&at.z>0)
            {var edge=camera.WorldToScreenPoint(end+Vector2.right*ArenaGrenadeBallistics.BlastRadius);var centre=new Vector2(at.x,Screen.height-at.y);CursorArc(centre,Mathf.Abs(edge.x-at.x),-90,360,1);CursorArc(centre,4,0,360,2);}
            GUI.color=color;
        }
        [ServerRpc]
        void FireGrenadeServerRpc(Vector2 origin,Vector2 direction,float speed,uint generation)
        {
            if(!IsServer||!IsSpawned||!NormalAmmoEnabled||IsDead||generation!=LifeVersion||ArenaObjectiveMatch.CombatBlocked||GrenadeRounds<=0)return;
            if(!float.IsFinite(origin.x)||!float.IsFinite(origin.y)||!float.IsFinite(direction.x)||!float.IsFinite(direction.y)||!float.IsFinite(speed)||direction.sqrMagnitude<.9f||direction.sqrMagnitude>1.1f||speed<ArenaGrenadeBallistics.MinimumSpeed||speed>ArenaGrenadeBallistics.MaximumSpeed)return;
            Vector3 position=IsOwner?transform.position:motion.Value.LifeVersion==LifeVersion?motion.Value.Position:life.Value.SpawnPosition;if(Vector2.Distance(origin,position)>3)return;
            double now=NetworkManager.ServerTime.Time;if(!ServerFireReady(AmmoKind.Grenade,now))return;
            MarkServerFired(AmmoKind.Grenade,now);grenadeRounds.Value--;nextGrenadePracticeSupply=now+PracticeSupplyInterval;
            var match=ArenaObjectiveMatch.Instance;uint id=++grenadeSequence;Vector2 velocity=direction.normalized*speed;
            ShowGrenadeClientRpc(id,origin,velocity,now,match.State.Value.Version);
            StartCoroutine(TravelGrenade(id,origin,velocity,now,match,match.State.Value.Version));
        }
        IEnumerator TravelGrenade(uint id,Vector2 origin,Vector2 velocity,double began,ArenaObjectiveMatch match,int version)
        {
            var context=new ArenaGrenadeBallistics.Context(this);Vector2 previous=origin;
            if(context.Sweep(ArenaHitBounds.center,origin,out var barrel)){BurstGrenade(id,barrel,match);yield break;}
            int step=0,maximum=Mathf.CeilToInt(ArenaGrenadeBallistics.MaximumLife/ArenaGrenadeBallistics.Step);
            while(IsServer&&IsSpawned&&match!=null&&match.IsSpawned&&match.State.Value.Version==version&&!ArenaObjectiveMatch.CombatBlocked)
            {
                int target=Mathf.Min(maximum,Mathf.FloorToInt((float)(NetworkManager.ServerTime.Time-began)/ArenaGrenadeBallistics.Step));
                while(step<target)
                {var next=ArenaGrenadeBallistics.At(origin,velocity,++step*ArenaGrenadeBallistics.Step);if(context.Sweep(previous,next,out var point)){BurstGrenade(id,point,match);yield break;}previous=next;}
                if(step>=maximum){EndGrenadeClientRpc(id);yield break;}yield return null;
            }
            if(IsServer&&IsSpawned)EndGrenadeClientRpc(id);
        }
        void BurstGrenade(uint id,Vector2 point,ArenaObjectiveMatch match)
        {
            GrenadeBurstClientRpc(id,point);
            foreach(var target in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))
                if(target.IsSpawned&&!target.IsDead&&ArenaGrenadeBallistics.InBlast(point,target.ArenaHitBounds))
                {target.ReceiveServerDamage(ArenaGrenadeBallistics.BlastDamage(point,target.ArenaHitBounds,GrenadeDamage,GrenadeMinimumDamage),target.LifeVersion);target.PushFromGrenadeServer(point);}
            foreach(var e in FindObjectsByType<ArenaPatrolEnemy>(FindObjectsSortMode.None))
                if(e.Match==match&&e.isActiveAndEnabled&&e.TryGetComponent<Collider2D>(out var shape)&&shape.enabled&&ArenaGrenadeBallistics.InBlast(point,shape.bounds))match.DamagePatrolServer(e.Id,e.Generation,ArenaGrenadeBallistics.BlastDamage(point,shape.bounds,GrenadeEnemyDamage,GrenadeEnemyMinimumDamage));
            foreach(var t in FindObjectsByType<SideScrollDamageTarget>(FindObjectsSortMode.None))
                if(t.IsSpawned&&t.Health.Value>0&&t.TryGetComponent<Collider2D>(out var shape)&&shape.enabled&&ArenaGrenadeBallistics.InBlast(point,shape.bounds))t.ReceiveDamage(ArenaGrenadeBallistics.BlastDamage(point,shape.bounds,GrenadeEnemyDamage,GrenadeEnemyMinimumDamage),t.Generation);
        }
        void PushFromGrenadeServer(Vector2 centre)
        {
            if(!IsServer||!IsSpawned||IsDead||ArenaObjectiveMatch.CombatBlocked)return;
            Vector2 away=((Vector2)ArenaHitBounds.center-centre).normalized;
            allowedHitPoseUntil=NetworkManager.ServerTime.Time+GrenadeHitSeconds+.3;
            GrenadePushClientRpc(away*GrenadePushSpeed,LifeVersion);
        }
        [ClientRpc]void GrenadePushClientRpc(Vector2 impulse,uint generation)
        {
            if(!IsOwner||IsDead||LifeVersion!=generation||!HasCurrentLifePose)return;
            ReceivedGrenadePushes++;LastGrenadePush=impulse;
            movement.ApplyHitPush(impulse,GrenadeHitSeconds);PublishMotion();
        }
        [ClientRpc]void ShowGrenadeClientRpc(uint id,Vector2 origin,Vector2 velocity,double began,int version)
        {ReceivedShots++;ReceivedGrenadeShots++;shots.ShowGrenade(id,origin,velocity,began,version);}
        [ClientRpc]void GrenadeBurstClientRpc(uint id,Vector2 point)
        {ReceivedGrenadeBursts++;LastGrenadeBurst=point;shots.ShowGrenadeBurst(id,point);}
        [ClientRpc]void EndGrenadeClientRpc(uint id)=>shots.EndGrenade(id);
    }
}
