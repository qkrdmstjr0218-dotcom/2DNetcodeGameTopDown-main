using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        public NetworkList<AmmoPickup> HealthPickups;
        [SerializeField,Min(1)] int healthPickupAmount=25;
        [SerializeField,Min(1)] float healthPickupRespawnSeconds=20;
        int healthPickupVersion=-1,nextHealthPickupId;
        double healthDropTick;
        readonly Dictionary<int,GameObject> healthPickupVisuals=new();
        public bool SpawnHealthDropServer(Vector2 position,int amount)
        {
            if(!IsServer||!IsSpawned||!PickupGameActive||amount<=0||!float.IsFinite(position.x)||!float.IsFinite(position.y)||HealthPickups.Count>=64)return false;
            AddHealthPickup(position,amount,0);return true;
        }
        void AddHealthPickup(Vector2 position,int amount,double respawn,bool falling=false,bool shield=false)
        {HealthPickups.Add(new AmmoPickup{Id=++nextHealthPickupId,Amount=amount,Position=position,Available=true,Respawn=respawn,Falling=falling,Shield=shield,ExpiresAt=respawn<=0?Now+Mathf.Max(1,droppedItemLifetimeSeconds):0});}
        bool SpawnFallingShieldDropServer(Vector2 position)
        {
            if(!IsServer||!IsSpawned||!PickupGameActive||!float.IsFinite(position.x)||!float.IsFinite(position.y)||HealthPickups.Count>=64)return false;
            AddHealthPickup(position,SideScrollNetworkPlayer.ShieldCapacity,0,true,true);return true;
        }
        public bool SpawnFallingHealthDropServer(Vector2 position,int amount)
        {
            if(!IsServer||!IsSpawned||!PickupGameActive||amount<=0||!float.IsFinite(position.x)||!float.IsFinite(position.y)||HealthPickups.Count>=64)return false;
            AddHealthPickup(position,amount,0,true);return true;
        }
        bool AdvanceFallingDrop(ref AmmoPickup item,float dt,float clearance=.3f)
        {
            item.FallSpeed+=14*dt;var next=item.Position+Vector2.down*(item.FallSpeed*dt);
            if(ArenaEnemyNavigation.FloorBelow(item.Position+Vector2.up*.02f,item.Position.y-next.y+clearance+.02f,out var floor))
            {item.Position=floor.point+Vector2.up*(clearance+.35f*Mathf.Abs(floor.normal.x)/Mathf.Max(.3f,floor.normal.y));item.Falling=false;item.FallSpeed=0;}
            else item.Position=next;
            return item.Position.y>=-5&&(RelayArenaPrototype.Instance==null||RelayArenaPrototype.Instance.IsSafeLanding(item.Position));
        }
        void UpdateHealthPickups()
        {
            if(IsServer)
            {
                if(!PickupGameActive){if(HealthPickups.Count>0)HealthPickups.Clear();healthPickupVersion=-1;}
                else
                {
                    if(healthPickupVersion!=State.Value.Version)
                    {
                        healthPickupVersion=State.Value.Version;HealthPickups.Clear();
                        var map=RelayArenaPrototype.Instance!=null?RelayArenaPrototype.Instance.GetComponent<ArenaMapSettings>():null;
                        if(map!=null&&map.healthSupplies!=null)foreach(var at in map.healthSupplies)AddHealthPickup(at,healthPickupAmount,healthPickupRespawnSeconds);
                    }
                    bool moveDrops=Now-healthDropTick>=.05;float fallDt=Mathf.Min(.15f,(float)(Now-healthDropTick));if(moveDrops)healthDropTick=Now;
                    var players=FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
                    Array.Sort(players,(a,b)=>a.PlayerNumber.CompareTo(b.PlayerNumber));
                    for(int i=HealthPickups.Count-1;i>=0;i--)
                    {
                        var item=HealthPickups[i];
                        if(item.ExpiresAt>0&&Now>=item.ExpiresAt){HealthPickups.RemoveAt(i);continue;}
                        if(item.Falling&&moveDrops)
                        {
                            if(!AdvanceFallingDrop(ref item,fallDt)){HealthPickups.RemoveAt(i);continue;}
                            HealthPickups[i]=item;
                        }
                        if(!item.Available){if(Now<item.ReadyAt)continue;item.Available=true;item.ReadyAt=0;HealthPickups[i]=item;}
                        foreach(var player in players)
                        {
                            if(!player.IsSpawned||player.IsDead||!player.HasCurrentLifePose||(!item.Shield&&player.Health>=player.MaxHealth))continue;
                            if(((Vector2)player.transform.position+Vector2.up*.65f-item.Position).sqrMagnitude>.65f*.65f)continue;
                            if(item.Shield){if(!player.GrantShieldServer(player.LifeVersion))continue;}
                            else if(player.GrantHealthServer(item.Amount,player.LifeVersion)<=0)continue;
                            if(item.Respawn<=0)HealthPickups.RemoveAt(i);
                            else{item.Available=false;item.ReadyAt=Now+item.Respawn;HealthPickups[i]=item;}
                            break;
                        }
                    }
                }
            }
            var remaining=new HashSet<int>();
            foreach(var item in HealthPickups)
            {
                remaining.Add(item.Id);
                if(!healthPickupVisuals.TryGetValue(item.Id,out var view)){view=MakePickupVisual(item,true);healthPickupVisuals.Add(item.Id,view);}
                view.SetActive(PickupGameActive&&item.Available&&PickupVisualVisible(item));view.transform.position=new Vector3(item.Position.x,item.Position.y+.035f*Mathf.Sin((float)Now*3),0);
            }
            var remove=new List<int>();foreach(var pair in healthPickupVisuals)if(!remaining.Contains(pair.Key)){Destroy(pair.Value);remove.Add(pair.Key);}foreach(int id in remove)healthPickupVisuals.Remove(id);
        }
        void ClearHealthPickupVisuals(){foreach(var view in healthPickupVisuals.Values)if(view!=null)Destroy(view);healthPickupVisuals.Clear();}
    }
}
