using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        public struct AmmoPickup:INetworkSerializable,IEquatable<AmmoPickup>
        {
            public int Id,Amount;public Vector2 Position;public bool Available;public double ReadyAt,Respawn;
            public void NetworkSerialize<T>(BufferSerializer<T> s) where T:IReaderWriter
            {s.SerializeValue(ref Id);s.SerializeValue(ref Amount);s.SerializeValue(ref Position);s.SerializeValue(ref Available);s.SerializeValue(ref ReadyAt);s.SerializeValue(ref Respawn);}
            public bool Equals(AmmoPickup p)=>Id==p.Id&&Amount==p.Amount&&Position==p.Position&&Available==p.Available&&ReadyAt==p.ReadyAt&&Respawn==p.Respawn;
        }
        public NetworkList<AmmoPickup> AmmoPickups;
        [SerializeField,Min(1)] int ammoPickupAmount=3;
        [SerializeField,Min(1)] float ammoPickupRespawnSeconds=15;
        int pickupVersion=-1,nextPickupId;
        readonly Dictionary<int,GameObject> pickupVisuals=new();
        Sprite pickupSprite;
        // Future enemy death code can spawn a one-use drop through this server-only entry.
        // Fixed test sources use exactly the same pickup state and grant path.
        public bool SpawnShotgunDropServer(Vector2 position,int amount)
        {
            if(!IsServer||!IsSpawned||!RoomFlow||State.Value.Phase!=Stage.Playing||amount<=0||!float.IsFinite(position.x)||!float.IsFinite(position.y)||AmmoPickups.Count>=64)return false;
            AddAmmoPickup(position,amount,0);return true;
        }
        void AddAmmoPickup(Vector2 position,int amount,double respawn)
        {AmmoPickups.Add(new AmmoPickup{Id=++nextPickupId,Amount=amount,Position=position,Available=true,Respawn=respawn});}
        bool PickupGameActive=>RoomFlow&&State.Value.Phase==Stage.Playing;
        void UpdateAmmoPickups()
        {
            if(IsServer)
            {
                if(!PickupGameActive){if(AmmoPickups.Count>0)AmmoPickups.Clear();pickupVersion=-1;}
                else
                {
                    if(pickupVersion!=State.Value.Version)
                    {
                        pickupVersion=State.Value.Version;AmmoPickups.Clear();
                        var map=RelayArenaPrototype.Instance!=null?RelayArenaPrototype.Instance.GetComponent<ArenaMapSettings>():null;
                        if(map!=null&&map.shotgunSupplies!=null)foreach(var at in map.shotgunSupplies)AddAmmoPickup(at,ammoPickupAmount,ammoPickupRespawnSeconds);
                        else {AddAmmoPickup(new Vector2(-8,.55f),ammoPickupAmount,ammoPickupRespawnSeconds);AddAmmoPickup(new Vector2(8,.55f),ammoPickupAmount,ammoPickupRespawnSeconds);}
                    }
                    var players=FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
                    Array.Sort(players,(a,b)=>a.PlayerNumber.CompareTo(b.PlayerNumber));
                    for(int i=AmmoPickups.Count-1;i>=0;i--)
                    {
                        var item=AmmoPickups[i];
                        if(!item.Available)
                        {if(Now<item.ReadyAt)continue;item.Available=true;item.ReadyAt=0;AmmoPickups[i]=item;}
                        foreach(var player in players)
                        {
                            if(!player.IsSpawned||player.IsDead||!player.HasCurrentLifePose||player.ShotgunRounds>=SideScrollNetworkPlayer.ShotgunCapacity)continue;
                            Vector2 centre=(Vector2)player.transform.position+Vector2.up*.65f;
                            if((centre-item.Position).sqrMagnitude>.65f*.65f)continue;
                            if(player.GrantShotgunAmmoServer(item.Amount)<=0)continue;
                            if(item.Respawn<=0)AmmoPickups.RemoveAt(i);
                            else{item.Available=false;item.ReadyAt=Now+item.Respawn;AmmoPickups[i]=item;}
                            break; // One server transaction, exactly one recipient.
                        }
                    }
                }
            }
            var remaining=new HashSet<int>();
            foreach(var item in AmmoPickups)
            {
                remaining.Add(item.Id);
                if(!pickupVisuals.TryGetValue(item.Id,out var view)){view=MakePickupVisual(item);pickupVisuals.Add(item.Id,view);}
                view.SetActive(PickupGameActive&&item.Available);
                view.transform.position=new Vector3(item.Position.x,item.Position.y+.035f*Mathf.Sin((float)Now*3),0);
            }
            var remove=new List<int>();foreach(var pair in pickupVisuals)if(!remaining.Contains(pair.Key)){Destroy(pair.Value);remove.Add(pair.Key);}
            foreach(int id in remove)pickupVisuals.Remove(id);
        }
        GameObject MakePickupVisual(AmmoPickup item)
        {
            if(pickupSprite==null)pickupSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            var root=new GameObject("Shotgun ammo +"+item.Amount);root.transform.SetParent(transform,false);
            void Box(string name,Vector2 offset,Vector2 size,Color color,int order)
            {
                var go=new GameObject(name);go.transform.SetParent(root.transform,false);go.transform.localPosition=offset;go.transform.localScale=size;
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=pickupSprite;sr.color=color;sr.sortingOrder=order;
            }
            Box("Case",Vector2.zero,new Vector2(.65f,.46f),new Color(.04f,.13f,.17f),40);
            Box("Top rim",new Vector2(0,.24f),new Vector2(.7f,.045f),new Color(.25f,.9f,.85f),41);
            Box("Bottom rim",new Vector2(0,-.24f),new Vector2(.7f,.045f),new Color(.25f,.9f,.85f),41);
            for(int j=-1;j<=1;j++){Box("Shell",new Vector2(j*.18f,0),new Vector2(.1f,.26f),new Color(1,.65f,.22f),42);Box("Shell cap",new Vector2(j*.18f,-.12f),new Vector2(.12f,.05f),new Color(1,.88f,.5f),43);}
            var label=new GameObject("Amount");label.transform.SetParent(root.transform,false);label.transform.localPosition=new Vector3(0,.45f,0);
            var text=label.AddComponent<TextMesh>();text.text="샷건 +"+item.Amount;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=40;text.characterSize=.035f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(1,.8f,.4f);var renderer=label.GetComponent<MeshRenderer>();renderer.sharedMaterial=text.font.material;renderer.sortingOrder=44;
            return root;
        }
        void ClearPickupVisuals()
        {foreach(var pair in pickupVisuals)if(pair.Value!=null)Destroy(pair.Value);pickupVisuals.Clear();if(pickupSprite!=null)Destroy(pickupSprite);pickupSprite=null;}
    }
}
