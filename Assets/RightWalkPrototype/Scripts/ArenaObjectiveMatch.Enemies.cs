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
            public int Id,Generation,Health;public float Left,Right,Floor,Speed;public double Started,RespawnAt;
            public void NetworkSerialize<T>(BufferSerializer<T> s) where T:IReaderWriter
            {s.SerializeValue(ref Id);s.SerializeValue(ref Generation);s.SerializeValue(ref Health);s.SerializeValue(ref Left);s.SerializeValue(ref Right);s.SerializeValue(ref Floor);s.SerializeValue(ref Speed);s.SerializeValue(ref Started);s.SerializeValue(ref RespawnAt);}
            public bool Equals(PatrolState p)=>Id==p.Id&&Generation==p.Generation&&Health==p.Health&&Left==p.Left&&Right==p.Right&&Floor==p.Floor&&Speed==p.Speed&&Started==p.Started&&RespawnAt==p.RespawnAt;
        }
        public NetworkList<PatrolState> Patrols;
        [SerializeField,Min(1)] int patrolHealth=4;
        [SerializeField,Min(.1f)] float patrolSpeed=1.2f,patrolRespawnSeconds=10;
        [SerializeField,Range(0,1)] float patrolDropChance=.5f;
        [SerializeField,Min(1)] int patrolContactDamage=10;
        [SerializeField,Min(.1f)] float patrolContactInterval=1;
        int patrolVersion=-1,nextPatrolId;
        Sprite patrolSprite;
        readonly Dictionary<int,ArenaPatrolEnemy> patrolViews=new();
        readonly Dictionary<ulong,double> nextPatrolContact=new();
        public Vector3 PatrolPosition(PatrolState state)=>new Vector3(state.Left+Mathf.PingPong((float)Math.Max(0,Now-state.Started)*state.Speed,state.Right-state.Left),state.Floor,0);
        public float PatrolHealthFraction(int id){foreach(var p in Patrols)if(p.Id==id)return p.Health/(float)Mathf.Max(1,patrolHealth);return 0;}
        void AddPatrol(float left,float right,float floor=0)
        {Patrols.Add(new PatrolState{Id=++nextPatrolId,Generation=1,Health=patrolHealth,Left=left,Right=right,Floor=floor,Speed=patrolSpeed,Started=Now});}
        public void DamagePatrolServer(int id,int generation,int damage)
        {
            if(!IsServer||!IsSpawned||!PickupGameActive||damage<=0)return;
            for(int i=0;i<Patrols.Count;i++)
            {
                var state=Patrols[i];if(state.Id!=id||state.Generation!=generation||state.Health<=0)continue;
                state.Health=Mathf.Max(0,state.Health-damage);
                if(state.Health==0)
                {
                    state.RespawnAt=Now+Mathf.Max(.1f,patrolRespawnSeconds);
                    if(UnityEngine.Random.value<patrolDropChance)SpawnShotgunDropServer((Vector2)PatrolPosition(state)+Vector2.up*.55f,1);
                }
                Patrols[i]=state;return;
            }
        }
        void UpdatePatrols()
        {
            if(IsServer)
            {
                if(!PickupGameActive){if(Patrols.Count>0)Patrols.Clear();patrolVersion=-1;nextPatrolContact.Clear();}
                else
                {
                    if(patrolVersion!=State.Value.Version)
                    {
                        patrolVersion=State.Value.Version;Patrols.Clear();nextPatrolContact.Clear();
                        var map=RelayArenaPrototype.Instance!=null?RelayArenaPrototype.Instance.GetComponent<ArenaMapSettings>():null;
                        if(map!=null&&map.patrolRoutes!=null)foreach(var route in map.patrolRoutes)AddPatrol(route.left,route.right,route.floor);
                        else {AddPatrol(-16,-11);AddPatrol(18,21);}
                    }
                    var players=FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
                    for(int i=0;i<Patrols.Count;i++)
                    {
                        var state=Patrols[i];
                        if(state.Health<=0)
                        {if(Now<state.RespawnAt)continue;state.Generation++;state.Health=patrolHealth;state.Started=Now;state.RespawnAt=0;Patrols[i]=state;}
                        var bounds=new Bounds(PatrolPosition(state)+Vector3.up*.48f,new Vector3(.85f,.9f,.2f));
                        foreach(var p in players)
                        {
                            if(!p.IsSpawned||p.IsDead||!p.HasCurrentLifePose||nextPatrolContact.TryGetValue(p.OwnerClientId,out double next)&&Now<next||!bounds.Intersects(p.ArenaHitBounds))continue;
                            p.ReceiveServerDamage(patrolContactDamage,p.LifeVersion);p.PushFromPatrolServer(p.ArenaHitBounds.center.x-bounds.center.x);nextPatrolContact[p.OwnerClientId]=Now+Mathf.Max(.1f,patrolContactInterval);
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
                    var go=new GameObject("Patrol hazard "+state.Id);go.transform.SetParent(transform,false);view=go.AddComponent<ArenaPatrolEnemy>();view.Initialize(this,state.Id,patrolSprite);patrolViews.Add(state.Id,view);
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
