using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    static bool dropAmmoOn,dropAmmoOff,dropHealthOn,dropHealthOff;
    static void ObserveDropWarning(Peer peer,ArenaObjectiveMatch m)
    {
        void Observe(Unity.Netcode.NetworkList<ArenaObjectiveMatch.AmmoPickup> items,string name,ref bool on,ref bool off)
        {
            var views=(System.Collections.Generic.Dictionary<int,GameObject>)typeof(ArenaObjectiveMatch).GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(m);
            foreach(var item in items)if(item.ExpiresAt>0&&item.ExpiresAt-m.Now<3.7&&item.ExpiresAt-m.Now>1&&views.TryGetValue(item.Id,out var view))
            {on|=view.activeSelf;off|=!view.activeSelf;}
        }
        Observe(m.AmmoPickups,"pickupVisuals",ref dropAmmoOn,ref dropAmmoOff);Observe(m.HealthPickups,"healthPickupVisuals",ref dropHealthOn,ref dropHealthOff);
        peer.warningAmmoOn=dropAmmoOn;peer.warningAmmoOff=dropAmmoOff;peer.warningHealthOn=dropHealthOn;peer.warningHealthOff=dropHealthOff;
    }
    [MenuItem("Tools/Right Walk Prototype/Verify Drop Expiration")]
    public static void BeginDropExpiration(){BeginFoundry();File.WriteAllText(folder+"/expiry.txt","expiry");}
    static IEnumerator CheckDropExpiration()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4),"four ready-room peers",45);
        var m=ArenaObjectiveMatch.Instance;Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");m.roundSeconds=120;Require(m.StartRound(),"start expiration round");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.pickupsCount==3&&p.healthCount==2),"fixed supplies initialized");
        foreach(var p in m.AmmoPickups)Require(p.ExpiresAt==0,"fixed ammunition has no natural expiration");
        foreach(var p in m.HealthPickups)Require(p.ExpiresAt==0,"fixed health has no natural expiration");
        Require(m.SpawnShotgunDropServer(new Vector2(0,11.35f),1)&&m.SpawnHealthDropServer(new Vector2(1,11.35f),25),"both one-use drop types spawn");
        Require(m.AmmoPickups[3].ExpiresAt>m.Now+19&&m.HealthPickups[2].ExpiresAt>m.Now+19,"both drop types receive twenty-second deadlines");
        yield return Wait(()=>All(4,p=>p.pickupsCount==4&&p.healthCount==3),"drops synchronized to all peers");yield return new WaitForSecondsRealtime(5);
        Require(m.AmmoPickups.Count==4&&m.HealthPickups.Count==3,"uncollected drops remain before deadline");
        yield return Wait(()=>All(4,p=>p.warningAmmoOn&&p.warningAmmoOff&&p.warningHealthOn&&p.warningHealthOff),"both drop visuals blink on every peer before expiration",15);
        Require(m.AmmoPickups.Count==4&&m.HealthPickups.Count==3,"blinking does not remove collectible drops early");
        yield return Wait(()=>All(4,p=>p.pickupsCount==3&&p.healthCount==2),"expired ammunition and health disappear on all peers",22);
        Require(m.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing&&m.AmmoPickups[0].Available&&m.HealthPickups[0].Available,"fixed supplies remain in active match");
        var state=m.State.Value;state.End=m.Now+.05;m.State.Value=state;yield return Wait(()=>All(4,p=>p.phase==2),"expiration round ends");Require(m.ReturnToRoom(),"return after expiration test");yield return Wait(()=>All(4,p=>p.phase==0&&p.healthCount==0&&p.pickupsCount==0),"waiting room has no leftover drops");
    }
}
