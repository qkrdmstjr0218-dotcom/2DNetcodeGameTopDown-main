using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        public const float TurretRange=9,TurretVerticalRange=3.25f,TurretWarning=.7f,TurretInterval=1.8f,TurretBurstInterval=.3f,TurretBulletSpeed=7,TurretBulletLife=(TurretRange-.6f)/TurretBulletSpeed,TurretBulletRadius=.1f;
        public const int TurretDamage=12,TurretBurstCount=3;
        public struct TurretShot:INetworkSerializable,IEquatable<TurretShot>
        {
            public int Id,Damage;public Vector2 Origin,Direction;public double Began;
            public void NetworkSerialize<T>(BufferSerializer<T> s) where T:IReaderWriter
            {s.SerializeValue(ref Id);s.SerializeValue(ref Damage);s.SerializeValue(ref Origin);s.SerializeValue(ref Direction);s.SerializeValue(ref Began);}
            public bool Equals(TurretShot p)=>Id==p.Id&&Damage==p.Damage&&Origin==p.Origin&&Direction==p.Direction&&Began==p.Began;
        }
        public NetworkList<TurretShot> TurretShots;
        int nextTurretShot;double lastTurretShotTick;
        Material turretBulletMaterial;
        readonly Dictionary<int,LineRenderer> turretBulletViews=new();
        public Vector2 TurretPivot(PatrolState state)=>(Vector2)PatrolPosition(state)+Vector2.up*.68f;
        public Vector2 TurretMuzzle(PatrolState state)=>TurretPivot(state)+state.Aim*.6f;
        SideScrollNetworkPlayer TurretTarget(PatrolState state,SideScrollNetworkPlayer[] players,bool locked)
        {
            Vector2 pivot=TurretPivot(state);SideScrollNetworkPlayer target=null;float nearest=state.Flying?DroneSight:TurretRange;
            foreach(var player in players)
            {
                if(!player.IsSpawned||player.IsDead||!player.HasCurrentLifePose||locked&&player.OwnerClientId!=state.TargetClient)continue;
                Vector2 to=player.ArenaHitBounds.center;float distance=Vector2.Distance(pivot,to);
                if(distance<nearest&&Mathf.Abs(to.y-pivot.y)<=(state.Flying?DroneSight:TurretVerticalRange)&&!ArenaGrenadeBallistics.WorldTrace(pivot,to,0,out _)){target=player;nearest=distance;}
            }
            return target;
        }
        void UpdateTurret(int index,PatrolState state,SideScrollNetworkPlayer[] players)
        {
            if(state.FireAt>0)
            {
                if(Now<state.FireAt)return;
                var tracked=TurretTarget(state,players,true);
                if(tracked==null)
                {state.FireAt=0;state.BurstLeft=0;state.NextAttack=Now+(state.Flying?2.2f:TurretInterval);Patrols[index]=state;return;}
                // Track between shots; each projectile keeps its own straight trajectory.
                state.Aim=((Vector2)tracked.ArenaHitBounds.center-TurretPivot(state)).normalized;
                TurretShots.Add(new TurretShot{Id=++nextTurretShot,Origin=TurretMuzzle(state),Direction=state.Aim,Began=Now,Damage=state.Flying?10:TurretDamage});
                state.BurstLeft--;
                state.FireAt=state.BurstLeft>0?Now+TurretBurstInterval:0;
                if(state.BurstLeft==0)state.NextAttack=Now+(state.Flying?2.2f:TurretInterval);
                Patrols[index]=state;return;
            }
            if(Now<state.NextAttack)return;
            Vector2 pivot=TurretPivot(state);var target=TurretTarget(state,players,false);
            if(target==null)return;
            state.Aim=((Vector2)target.ArenaHitBounds.center-pivot).normalized;
            state.TargetClient=target.OwnerClientId;state.BurstLeft=state.Flying?2:TurretBurstCount;state.FireAt=Now+TurretWarning;Patrols[index]=state;
        }
        void UpdateTurretShots()
        {
            if(IsServer)
            {
                if(!PickupGameActive){if(TurretShots.Count>0)TurretShots.Clear();}
                else
                {
                    var players=TurretShots.Count>0?FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None):Array.Empty<SideScrollNetworkPlayer>();
                    for(int i=TurretShots.Count-1;i>=0;i--)
                    {
                        var shot=TurretShots[i];float age=(float)Math.Max(0,Now-shot.Began);
                        float previous=(float)Math.Max(0,lastTurretShotTick-shot.Began);
                        Vector2 from=shot.Origin+shot.Direction*(Mathf.Min(previous,TurretBulletLife)*TurretBulletSpeed);
                        Vector2 to=shot.Origin+shot.Direction*(Mathf.Min(age,TurretBulletLife)*TurretBulletSpeed);
                        bool hit=ArenaGrenadeBallistics.WorldTrace(from,to,TurretBulletRadius,out var point);
                        float nearest=Vector2.Distance(from,point);SideScrollNetworkPlayer victim=null;
                        foreach(var player in players)
                        {
                            if(!player.IsSpawned||player.IsDead||!player.HasCurrentLifePose)continue;
                            var bounds=player.ArenaHitBounds;bounds.Expand(new Vector3(TurretBulletRadius*2,TurretBulletRadius*2,0));
                            var start=new Vector3(from.x,from.y,bounds.center.z);float distance;
                            if(bounds.Contains(start))distance=0;
                            else if(!bounds.IntersectRay(new Ray(start,shot.Direction),out distance))continue;
                            if(distance>=0&&distance<nearest){nearest=distance;victim=player;hit=true;}
                        }
                        if(victim!=null)victim.ReceiveServerDamage(shot.Damage,victim.LifeVersion);
                        if(hit||age>=TurretBulletLife)TurretShots.RemoveAt(i);
                    }
                }
                lastTurretShotTick=Now;
            }
            var remaining=new HashSet<int>();
            foreach(var shot in TurretShots)
            {
                if(!PickupGameActive)break;remaining.Add(shot.Id);
                if(!turretBulletViews.TryGetValue(shot.Id,out var line))
                {
                    if(turretBulletMaterial==null)turretBulletMaterial=new Material(Shader.Find("Sprites/Default"));
                    var go=new GameObject("Turret projectile");go.transform.SetParent(transform,false);line=go.AddComponent<LineRenderer>();
                    line.sharedMaterial=turretBulletMaterial;line.useWorldSpace=true;line.positionCount=2;line.numCapVertices=6;line.sortingOrder=30;
                    line.startWidth=.18f;line.endWidth=.035f;line.startColor=new Color(1,.8f,.4f);line.endColor=new Color(1,.2f,.05f,0);turretBulletViews.Add(shot.Id,line);
                }
                float travelled=Mathf.Clamp((float)(Now-shot.Began),0,TurretBulletLife)*TurretBulletSpeed;
                Vector2 at=shot.Origin+shot.Direction*travelled;
                line.SetPosition(0,at);line.SetPosition(1,at-shot.Direction*Mathf.Min(.45f,travelled));
            }
            var remove=new List<int>();foreach(var pair in turretBulletViews)if(!remaining.Contains(pair.Key)){Destroy(pair.Value.gameObject);remove.Add(pair.Key);}foreach(int id in remove)turretBulletViews.Remove(id);
        }
        void ClearTurretVisuals()
        {foreach(var line in turretBulletViews.Values)if(line!=null)Destroy(line.gameObject);turretBulletViews.Clear();if(turretBulletMaterial!=null)Destroy(turretBulletMaterial);turretBulletMaterial=null;}
    }
}
