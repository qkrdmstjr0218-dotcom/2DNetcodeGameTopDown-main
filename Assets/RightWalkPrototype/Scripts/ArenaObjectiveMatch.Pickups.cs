using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        public enum SupplyAmmo:byte { Shotgun,Rifle,Grenade }
        public struct AmmoPickup:INetworkSerializable,IEquatable<AmmoPickup>
        {
            public SupplyAmmo Ammo;public int Id,Amount;public Vector2 Position;public bool Available;public double ReadyAt,Respawn,ExpiresAt;public bool Falling,Shield;public float FallSpeed;
            public void NetworkSerialize<T>(BufferSerializer<T> s) where T:IReaderWriter
            {s.SerializeValue(ref Ammo);s.SerializeValue(ref Id);s.SerializeValue(ref Amount);s.SerializeValue(ref Position);s.SerializeValue(ref Available);s.SerializeValue(ref ReadyAt);s.SerializeValue(ref Respawn);s.SerializeValue(ref ExpiresAt);s.SerializeValue(ref Shield);s.SerializeValue(ref Falling);s.SerializeValue(ref FallSpeed);}
            public bool Equals(AmmoPickup p)=>Ammo==p.Ammo&&Id==p.Id&&Amount==p.Amount&&Position==p.Position&&Available==p.Available&&ReadyAt==p.ReadyAt&&Respawn==p.Respawn&&ExpiresAt==p.ExpiresAt&&Shield==p.Shield&&Falling==p.Falling&&FallSpeed==p.FallSpeed;
        }
        public NetworkList<AmmoPickup> AmmoPickups;
        public const int AmmoPickupAmount=1;
        [SerializeField,Min(1)] float ammoPickupRespawnSeconds=15;
        [SerializeField,Min(1)] float droppedItemLifetimeSeconds=20;
        [SerializeField,Min(0)] float droppedItemWarningSeconds=4;
        int pickupVersion=-1,nextPickupId;
        readonly Dictionary<int,GameObject> pickupVisuals=new();
        Sprite pickupSprite;
        // Future enemy death code can spawn a one-use drop through this server-only entry.
        // Fixed test sources use exactly the same pickup state and grant path.
        public bool SpawnShotgunDropServer(Vector2 position,int amount)
        {
            if(!IsServer||!IsSpawned||!RoomFlow||State.Value.Phase!=Stage.Playing||amount<=0||!float.IsFinite(position.x)||!float.IsFinite(position.y)||AmmoPickups.Count>=64)return false;
            AddAmmoPickup(position,amount,0,SupplyAmmo.Shotgun,true);return true;
        }
        void AddAmmoPickup(Vector2 position,int amount,double respawn,SupplyAmmo kind=SupplyAmmo.Shotgun,bool falling=false)
        {AmmoPickups.Add(new AmmoPickup{Id=++nextPickupId,Ammo=kind,Falling=falling,Amount=AmmoPickupAmount,Position=position,Available=true,Respawn=respawn,ExpiresAt=respawn<=0?Now+Mathf.Max(1,droppedItemLifetimeSeconds):0});}
        public bool SpawnSpecialAmmoDropServer(Vector2 position,SupplyAmmo kind,int amount=1)
        {
            if(!IsServer||!IsSpawned||!PickupGameActive||amount<=0||kind>SupplyAmmo.Grenade||!float.IsFinite(position.x)||!float.IsFinite(position.y)||AmmoPickups.Count>=64)return false;
            AddAmmoPickup(position,amount,0,kind,true);return true;
        }
        double ammoDropTick;
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
                        if(map!=null&&map.ammoSupplies!=null&&map.ammoSupplies.Length>0)foreach(var source in map.ammoSupplies)AddAmmoPickup(source.position,AmmoPickupAmount,ammoPickupRespawnSeconds,source.ammo);
                        else if(map!=null&&map.shotgunSupplies!=null)foreach(var at in map.shotgunSupplies)AddAmmoPickup(at,AmmoPickupAmount,ammoPickupRespawnSeconds);
                        else {AddAmmoPickup(new Vector2(-8,.55f),AmmoPickupAmount,ammoPickupRespawnSeconds);AddAmmoPickup(new Vector2(8,.55f),AmmoPickupAmount,ammoPickupRespawnSeconds);}
                    }
                    bool moveDrops=Now-ammoDropTick>=.05;float dt=Mathf.Min(.15f,(float)(Now-ammoDropTick));if(moveDrops)ammoDropTick=Now;
                    var players=FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
                    Array.Sort(players,(a,b)=>a.PlayerNumber.CompareTo(b.PlayerNumber));
                    for(int i=AmmoPickups.Count-1;i>=0;i--)
                    {
                        var item=AmmoPickups[i];
                        if(item.ExpiresAt>0&&Now>=item.ExpiresAt){AmmoPickups.RemoveAt(i);continue;}
                        if(item.Falling&&moveDrops){if(!AdvanceFallingDrop(ref item,dt,.42f)){AmmoPickups.RemoveAt(i);continue;}AmmoPickups[i]=item;}
                        if(!item.Available)
                        {if(Now<item.ReadyAt)continue;item.Available=true;item.ReadyAt=0;AmmoPickups[i]=item;}
                        foreach(var player in players)
                        {
                            if(!player.IsSpawned||player.IsDead||!player.HasCurrentLifePose)continue;
                            Vector2 centre=(Vector2)player.transform.position+Vector2.up*.65f;
                            if((centre-item.Position).sqrMagnitude>.65f*.65f)continue;
                            if(player.GrantSpecialAmmoServer(item.Ammo,item.Amount,player.LifeVersion)<=0)continue;
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
                view.SetActive(PickupGameActive&&item.Available&&PickupVisualVisible(item));
                view.transform.position=new Vector3(item.Position.x,item.Position.y+.035f*Mathf.Sin((float)Now*3),0);
            }
            var remove=new List<int>();foreach(var pair in pickupVisuals)if(!remaining.Contains(pair.Key)){Destroy(pair.Value);remove.Add(pair.Key);}
            foreach(int id in remove)pickupVisuals.Remove(id);
        }
        bool PickupVisualVisible(AmmoPickup item)
        {
            double remaining=item.ExpiresAt-Now;
            if(item.ExpiresAt<=0||droppedItemWarningSeconds<=0||remaining>droppedItemWarningSeconds)return true;
            // Shared expiration clock keeps the case and label blinking together
            // on every peer. Collection remains independent of this visual toggle.
            return remaining>0&&((long)Math.Floor((droppedItemWarningSeconds-remaining)/.2)%2)==0;
        }
        GameObject MakePickupVisual(AmmoPickup item,bool healing=false)
        {
            if(pickupSprite==null)pickupSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            var root=new GameObject((healing?item.Shield?"Shield +":"Health +":SupplyInitial(item.Ammo)+" ammo +")+item.Amount);root.transform.SetParent(transform,false);
            void Box(string name,Vector2 offset,Vector2 size,Color color,int order)
            {
                var go=new GameObject(name);go.transform.SetParent(root.transform,false);go.transform.localPosition=offset;go.transform.localScale=size;
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=pickupSprite;sr.color=color;sr.sortingOrder=order;
            }
            var rim=item.Shield?new Color(.35f,.65f,1):healing?new Color(.3f,.95f,.5f):SupplyColor(item.Ammo);
            Box("Case",Vector2.zero,new Vector2(.65f,healing?.46f:.65f),healing?new Color(.85f,.95f,.9f):new Color(.04f,.13f,.17f),40);
            Box("Top rim",new Vector2(0,healing?.24f:.34f),new Vector2(.7f,.045f),rim,41);
            Box("Bottom rim",new Vector2(0,healing?-.24f:-.34f),new Vector2(.7f,.045f),rim,41);
            if(item.Shield)
            {Box("Shield plate",Vector2.zero,new Vector2(.3f,.32f),rim,42);Box("Shield inset",new Vector2(0,.025f),new Vector2(.18f,.19f),new Color(.9f,.95f,1),43);}
            else if(healing)
            {Box("Cross vertical",Vector2.zero,new Vector2(.1f,.32f),new Color(.08f,.65f,.25f),42);Box("Cross horizontal",Vector2.zero,new Vector2(.32f,.1f),new Color(.08f,.65f,.25f),42);}
            else
            {
                Box("Left rim",new Vector2(-.34f,0),new Vector2(.045f,.7f),rim,41);Box("Right rim",new Vector2(.34f,0),new Vector2(.045f,.7f),rim,41);
                var letter=new GameObject("Ammo initial");letter.transform.SetParent(root.transform,false);var mark=letter.AddComponent<TextMesh>();mark.text=SupplyInitial(item.Ammo);mark.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");mark.fontSize=72;mark.characterSize=.075f;mark.anchor=TextAnchor.MiddleCenter;mark.color=rim;
                var ink=letter.GetComponent<MeshRenderer>();ink.sharedMaterial=mark.font.material;ink.sortingOrder=43;
            }
            if(!healing)return root;
            var label=new GameObject("Amount");label.transform.SetParent(root.transform,false);label.transform.localPosition=new Vector3(0,.45f,0);
            var text=label.AddComponent<TextMesh>();text.text=item.Shield?"보호막 +"+item.Amount:(healing?"회복 +":SupplyInitial(item.Ammo)+" +")+item.Amount;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=40;text.characterSize=.035f;text.anchor=TextAnchor.MiddleCenter;text.color=rim;var renderer=label.GetComponent<MeshRenderer>();renderer.sharedMaterial=text.font.material;renderer.sortingOrder=44;
            return root;
        }
        public static Color SupplyColor(SupplyAmmo kind)=>kind==SupplyAmmo.Rifle?new Color(.75f,.45f,1):kind==SupplyAmmo.Grenade?new Color(.65f,1,.3f):new Color(1,.82f,.18f);
        public static string SupplyInitial(SupplyAmmo kind)=>kind==SupplyAmmo.Rifle?"R":kind==SupplyAmmo.Grenade?"G":"S";
        void ClearPickupVisuals()
        {ClearHealthPickupVisuals();foreach(var pair in pickupVisuals)if(pair.Value!=null)Destroy(pair.Value);pickupVisuals.Clear();if(pickupSprite!=null)Destroy(pickupSprite);pickupSprite=null;}
    }
}
