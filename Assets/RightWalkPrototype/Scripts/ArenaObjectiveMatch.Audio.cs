using Unity.Netcode;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        // Listen to existing replicated list deltas, including add/consume in one
        // frame. No new packets or changes to inventory/gameplay state.
        void ListenPickupAudio(){AmmoPickups.OnListChanged+=AmmoAudio;HealthPickups.OnListChanged+=HealthAudio;}
        void StopPickupAudio(){AmmoPickups.OnListChanged-=AmmoAudio;HealthPickups.OnListChanged-=HealthAudio;}
        void AmmoAudio(NetworkListEvent<AmmoPickup> change)=>PickupAudio(change,false);
        void HealthAudio(NetworkListEvent<AmmoPickup> change)=>PickupAudio(change,true);
        void PickupAudio(NetworkListEvent<AmmoPickup> change,bool healing)
        {
            if(!IsClient||!PickupGameActive)return;var item=change.Value;
            bool consumed=change.Type==NetworkListEvent<AmmoPickup>.EventType.Value&&change.PreviousValue.Available&&!item.Available;
            if(change.Type==NetworkListEvent<AmmoPickup>.EventType.RemoveAt||change.Type==NetworkListEvent<AmmoPickup>.EventType.Remove)
            {
                bool contact=false;
                if(item.Falling)foreach(var player in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))
                    if(player.IsSpawned&&!player.IsDead&&PickupBodyContact(player.ArenaHitBounds,item.Position,healing,Now)){contact=true;break;}
                consumed=item.Available&&(item.ExpiresAt<=0||Now<item.ExpiresAt)&&(!item.Falling||contact);
            }
            if(consumed)ArenaAudio.World(!healing?ArenaAudio.Cue.PickupAmmo:item.Shield?ArenaAudio.Cue.PickupShield:ArenaAudio.Cue.PickupHealth,item.Position,GetInstanceID());
        }
    }
}
