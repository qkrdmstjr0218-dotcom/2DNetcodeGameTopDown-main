using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        public struct PatrolState:INetworkSerializable,IEquatable<PatrolState>
        {
            public int Id,Generation,Health;public float Left,Right,Floor,Speed;public double Started,RespawnAt,EntryUntil;public float EntrySeconds;
            public byte RushPhase;public double RushUntil;
            public bool Turret,Roaming,Flying,Medical;public Vector2 Position,Velocity;public float Angle;public Vector2 Aim;public double FireAt,NextAttack;public int BurstLeft;public ulong TargetClient;
            public void NetworkSerialize<T>(BufferSerializer<T> s) where T:IReaderWriter
            {s.SerializeValue(ref Id);s.SerializeValue(ref Generation);s.SerializeValue(ref Health);s.SerializeValue(ref Left);s.SerializeValue(ref Right);s.SerializeValue(ref Floor);s.SerializeValue(ref Speed);s.SerializeValue(ref Started);s.SerializeValue(ref RespawnAt);s.SerializeValue(ref EntryUntil);s.SerializeValue(ref EntrySeconds);s.SerializeValue(ref RushPhase);s.SerializeValue(ref RushUntil);s.SerializeValue(ref Roaming);s.SerializeValue(ref Flying);s.SerializeValue(ref Position);s.SerializeValue(ref Velocity);s.SerializeValue(ref Angle);s.SerializeValue(ref Medical);s.SerializeValue(ref Turret);s.SerializeValue(ref Aim);s.SerializeValue(ref FireAt);s.SerializeValue(ref NextAttack);s.SerializeValue(ref BurstLeft);s.SerializeValue(ref TargetClient);}
            public bool Equals(PatrolState p)=>Id==p.Id&&Generation==p.Generation&&Health==p.Health&&Left==p.Left&&Right==p.Right&&Floor==p.Floor&&Speed==p.Speed&&Started==p.Started&&RespawnAt==p.RespawnAt&&EntryUntil==p.EntryUntil&&EntrySeconds==p.EntrySeconds&&RushPhase==p.RushPhase&&RushUntil==p.RushUntil&&Roaming==p.Roaming&&Flying==p.Flying&&Position==p.Position&&Velocity==p.Velocity&&Angle==p.Angle&&Medical==p.Medical&&Turret==p.Turret&&Aim==p.Aim&&FireAt==p.FireAt&&NextAttack==p.NextAttack&&BurstLeft==p.BurstLeft&&TargetClient==p.TargetClient;
        }
        public NetworkList<PatrolState> Patrols;
        [SerializeField,Min(1)] int patrolHealth=4;
        [SerializeField,Min(.1f)] float patrolSpeed=1.2f,patrolRespawnSeconds=10;
        [SerializeField,Range(0,1)] float patrolDropChance=.5f;
        [SerializeField,Range(0,1)] float patrolHealthDropChance=.2f;
        [SerializeField,Min(1)] int patrolContactDamage=10;
        [SerializeField,Min(.1f)] float patrolContactInterval=1;
        int patrolVersion=-1,nextPatrolId;
        Sprite patrolSprite;
        readonly Dictionary<int,ArenaPatrolEnemy> patrolViews=new();
        readonly Dictionary<ulong,double> nextPatrolContact=new();
        public Vector3 PatrolPosition(PatrolState state)=>state.Roaming?(Vector3)state.Position:new Vector3(state.Turret?state.Left:state.Left+Mathf.PingPong((float)Math.Max(0,Now-state.Started)*state.Speed,state.Right-state.Left),state.Floor,0);
        public float PatrolHealthFraction(int id){foreach(var p in Patrols)if(p.Id==id)return p.Health/(float)Mathf.Max(1,p.Medical?medicalDroneHealth:patrolHealth);return 0;}
        void AddPatrol(float left,float right,float floor=0)
        {Patrols.Add(new PatrolState{Id=++nextPatrolId,Generation=1,Health=patrolHealth,Left=left,Right=right,Floor=floor,Speed=patrolSpeed,Started=Now});}
        public void DamagePatrolServer(int id,int generation,int damage,SideScrollNetworkPlayer attacker=null)
        {
            if(!IsServer||!IsSpawned||!PickupGameActive||damage<=0)return;
            for(int i=0;i<Patrols.Count;i++)
            {
                var state=Patrols[i];if(state.Id!=id||state.Generation!=generation||state.Health<=0||EnemyEntering(state))continue;
                if(state.Medical&&!MedicalInsideArena(state.Position))return;
                state.Health=Mathf.Max(0,state.Health-damage);
                if(state.Health>0)RecognizeAttacker(ref state,attacker);
                if(state.Health==0)
                {
                    ResetEnemyBrain(state.Id);
                    state.RespawnAt=Now+(state.Medical?.6f:Mathf.Max(.1f,patrolRespawnSeconds));
                    if(state.Medical){SpawnFallingShieldDropServer((Vector2)PatrolPosition(state)+Vector2.up*.35f);RecordMedicalDroneKill(attacker);}
                    else SpawnEnemyLoot((Vector2)PatrolPosition(state)+Vector2.up*.55f);
                }
                Patrols[i]=state;return;
            }
        }
        void SpawnEnemyLoot(Vector2 position)
        {
            float roll=UnityEngine.Random.value,heal=Mathf.Clamp01(patrolHealthDropChance);
            if(roll<heal)SpawnFallingHealthDropServer(position,healthPickupAmount);
            else if(roll<Mathf.Min(1,heal+Mathf.Clamp01(patrolDropChance)))SpawnSpecialAmmoDropServer(position,(SupplyAmmo)UnityEngine.Random.Range(0,3),1);
        }
        void UpdatePatrols()
        {
            if(IsServer)
            {
                if(!PickupGameActive){if(Patrols.Count>0)Patrols.Clear();ResetEnemyNavigation();ResetMedicalDrones();patrolVersion=-1;nextPatrolContact.Clear();}
                else
                {
                    if(patrolVersion!=State.Value.Version)
                    {
                        patrolVersion=State.Value.Version;Patrols.Clear();nextPatrolContact.Clear();
                        var map=RelayArenaPrototype.Instance!=null?RelayArenaPrototype.Instance.GetComponent<ArenaMapSettings>():null;
                        if(map!=null&&map.patrolRoutes!=null)foreach(var route in map.patrolRoutes)AddPatrol(route.left,route.right,route.floor);
                        else {AddPatrol(-16,-11);AddPatrol(18,21);}
                        if(map!=null&&map.roamingEnemies)InitializeEnemyNavigation(map);
                        if(map!=null&&map.turretSpawns!=null)foreach(var at in map.turretSpawns)
                            Patrols.Add(new PatrolState{Id=++nextPatrolId,Generation=1,Health=patrolHealth,Left=at.x,Right=at.x,Floor=at.y,Turret=true,Aim=Vector2.right,NextAttack=Now+1.5});
                        if(map!=null&&map.enemyEntrances)for(int i=0;i<Patrols.Count;i++){var e=Patrols[i];BeginEnemyEntry(ref e,map);Patrols[i]=e;}
                    }
                    UpdateMedicalSchedule();
                    var players=FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
                    for(int i=0;i<Patrols.Count;i++)
                    {
                        var state=Patrols[i];
                        if(state.Medical)
                        {
                            if(state.Health<=0){if(Now>=state.RespawnAt)Patrols.RemoveAt(i--);continue;}
                            if(!UpdateMedicalFlight(ref state)){Patrols.RemoveAt(i--);continue;}
                            if(!state.Equals(Patrols[i]))Patrols[i]=state;
                            continue; // Supply craft never attack or deal contact damage.
                        }
                        if(state.Health<=0)
                        {if(Now<state.RespawnAt)continue;state.Generation++;state.Health=patrolHealth;state.Started=Now;state.RespawnAt=0;state.FireAt=0;state.BurstLeft=0;state.NextAttack=Now+1.5;state.RushPhase=0;state.RushUntil=0;if(state.Roaming)state.Speed=state.Flying?DroneSpeed:patrolSpeed;ResetEnemyBrain(state.Id);if(state.Roaming){state.Position=EnemySpawn(state);state.Velocity=Vector2.zero;}var settings=RelayArenaPrototype.Instance!=null?RelayArenaPrototype.Instance.GetComponent<ArenaMapSettings>():null;BeginEnemyEntry(ref state,settings);Patrols[i]=state;}
                        if(EnemyEntering(state)){AdvanceEnemyEntry(ref state,false);Patrols[i]=state;continue;}
                        if(state.EntryUntil>0&&state.Started<=state.EntryUntil){AdvanceEnemyEntry(ref state,true);state.Started=Now;Patrols[i]=state;}
                        if(state.Roaming){UpdateRoamingEnemy(ref state,players);if(!state.Equals(Patrols[i]))Patrols[i]=state;}
                        if(state.Health<=0)continue;
                        if(state.Turret||state.Flying){UpdateTurret(i,state,players);continue;}
                        var radians=state.Angle*Mathf.Deg2Rad;var normal=new Vector3(-Mathf.Sin(radians),Mathf.Cos(radians));
                        var bounds=new Bounds(PatrolPosition(state)+normal*.48f,new Vector3(Mathf.Abs(Mathf.Cos(radians))*.85f+Mathf.Abs(Mathf.Sin(radians))*.9f,Mathf.Abs(Mathf.Sin(radians))*.85f+Mathf.Abs(Mathf.Cos(radians))*.9f,.2f));
                        foreach(var p in players)
                        {
                            if(!p.IsSpawned||p.IsDead||!p.HasCurrentLifePose||nextPatrolContact.TryGetValue(p.OwnerClientId,out double next)&&Now<next||!bounds.Intersects(p.ArenaHitBounds))continue;
                            p.ReceiveServerDamage(patrolContactDamage,p.LifeVersion);p.PushFromPatrolServer(p.ArenaHitBounds.center.x-bounds.center.x);nextPatrolContact[p.OwnerClientId]=Now+Mathf.Max(.1f,patrolContactInterval);
                            if(state.RushPhase==2){EndGroundRush(ref state);Patrols[i]=state;break;}
                        }
                    }
                }
            }
            var remaining=new HashSet<int>();
            foreach(var state in Patrols)
            {
                remaining.Add(state.Id);
                if(!patrolViews.TryGetValue(state.Id,out var view))
                {
                    if(patrolSprite==null)patrolSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
                    var go=new GameObject((state.Medical?"Medical drone ":state.Turret?"Turret ":state.Flying?"Combat drone ":"Patrol hazard ")+state.Id);go.transform.SetParent(transform,false);view=go.AddComponent<ArenaPatrolEnemy>();view.Initialize(this,state.Id,patrolSprite,state.Turret,state.Flying,state.Medical);patrolViews.Add(state.Id,view);
                }
                view.Apply(state,PickupGameActive,patrolHealth);
            }
            var remove=new List<int>();foreach(var pair in patrolViews)if(!remaining.Contains(pair.Key)){Destroy(pair.Value.gameObject);remove.Add(pair.Key);}foreach(int id in remove)patrolViews.Remove(id);
            Physics2D.SyncTransforms();
        }
        void ClearPatrolVisuals()
        {foreach(var effect in GetComponentsInChildren<ArenaPatrolBurst>(true))Destroy(effect.gameObject);foreach(var p in patrolViews)if(p.Value!=null)Destroy(p.Value.gameObject);patrolViews.Clear();if(patrolSprite!=null)Destroy(patrolSprite);patrolSprite=null;}
    }
}
