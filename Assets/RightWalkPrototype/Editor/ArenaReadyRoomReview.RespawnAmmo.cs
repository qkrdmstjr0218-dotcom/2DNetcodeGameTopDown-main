using System.Collections;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Respawn Ammunition")]
    public static void BeginRespawnAmmo(){BeginFoundry();File.WriteAllText(folder+"/respawn-ammo.txt","respawn");}
    static void SetNormalSupply(SideScrollNetworkPlayer p,float value)
    {((NetworkVariable<float>)typeof(SideScrollNetworkPlayer).GetField("normalAmmo",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(p)).Value=value;}
    static IEnumerator CheckRespawnAmmo()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4),"four peers waiting",45);
        var m=ArenaObjectiveMatch.Instance;Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");m.roundSeconds=120;Require(m.StartRound(),"start ammunition persistence match");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.hp==100&&p.ammo==6&&p.rifleAmmo==3&&p.grenadeAmmo==3),"new match starts all special types full");
        for(int i=0;i<m.Patrols.Count;i++){var e=m.Patrols[i];e.Health=0;e.RespawnAt=m.Now+300;m.Patrols[i]=e;}
        var rules=(NetworkVariable<ArenaObjectiveMatch.AmmoRules>)typeof(ArenaObjectiveMatch).GetField("ammoRules",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(m);var original=rules.Value;var paused=original;paused.Rate=0;rules.Value=paused;
        var host=Player(1);var client=Player(2);uint h=host.LifeVersion,c=client.LifeVersion;
        SetSupplyAmmo(host,ArenaObjectiveMatch.SupplyAmmo.Shotgun,2);SetSupplyAmmo(host,ArenaObjectiveMatch.SupplyAmmo.Rifle,1);SetSupplyAmmo(host,ArenaObjectiveMatch.SupplyAmmo.Grenade,0);SetNormalSupply(host,2);
        SetSupplyAmmo(client,ArenaObjectiveMatch.SupplyAmmo.Shotgun,0);SetSupplyAmmo(client,ArenaObjectiveMatch.SupplyAmmo.Rifle,2);SetSupplyAmmo(client,ArenaObjectiveMatch.SupplyAmmo.Grenade,1);SetNormalSupply(client,3);
        yield return Wait(()=>P(1).ammo==2&&P(1).rifleAmmo==1&&P(1).grenadeAmmo==0&&P(1).normal==2&&P(2).ammo==0&&P(2).rifleAmmo==2&&P(2).grenadeAmmo==1&&P(2).normal==3,"different predeath inventories synchronized");
        host.ReceiveServerDamage(1000,h);client.ReceiveServerDamage(1000,c);
        yield return Wait(()=>P(1).hp==0&&P(2).hp==0,"both owners dead");
        yield return new WaitForSecondsRealtime(1);
        Require(P(1).ammo==2&&P(2).ammo==0&&P(1).normal==2&&P(2).normal==3,"dead inventories do not refill");
        Require(client.GrantSpecialAmmoServer(ArenaObjectiveMatch.SupplyAmmo.Grenade,1,c)==0,"dead player cannot receive special ammunition");
        yield return Wait(()=>P(1).hp==100&&P(2).hp==100&&P(1).life>h&&P(2).life>c,"both owners automatically respawn",8);
        Require(P(1).ammo==2&&P(1).rifleAmmo==1&&P(1).grenadeAmmo==0&&P(1).normal==P(1).normalCapacity,"host respawn keeps specials and refills normal ammo");
        Require(P(2).ammo==0&&P(2).rifleAmmo==2&&P(2).grenadeAmmo==1&&P(2).normal==P(2).normalCapacity,"client respawn keeps specials and refills normal ammo");
        Require(P(1).energy==100&&P(2).energy==100&&host.Shield==0&&client.Shield==0,"health stamina and shield reset remain intact");
        Require(client.GrantSpecialAmmoServer(ArenaObjectiveMatch.SupplyAmmo.Grenade,1,c)==0,"old life cannot receive ammo after respawn");
        c=client.LifeVersion;client.ReceiveServerDamage(1000,c);
        yield return Wait(()=>P(2).hp==0,"repeat death begins");yield return Wait(()=>P(2).hp==100&&P(2).life>c,"repeat automatic respawn",8);
        Require(P(2).ammo==0&&P(2).rifleAmmo==2&&P(2).grenadeAmmo==1,"repeated deaths cannot replenish special ammo");
        SetNormalSupply(host,2);SetNormalSupply(client,3);rules.Value=original;yield return Wait(()=>P(1).normal>2&&P(2).normal>3,"normal automatic recharge resumes normally");
        var state=m.State.Value;state.End=m.Now+.05;m.State.Value=state;yield return Wait(()=>All(4,p=>p.phase==2),"round finishes");Require(m.ReturnToRoom(),"return to preparation room");
        yield return Wait(()=>All(4,p=>p.phase==0&&p.ammo==6&&p.rifleAmmo==3&&p.grenadeAmmo==3),"preparation room restores full practice inventories");
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"ready again");Require(m.StartRound(),"start next match");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.ammo==6&&p.rifleAmmo==3&&p.grenadeAmmo==3),"next match starts full rather than carrying old shortages");
        state=m.State.Value;state.End=m.Now+.05;m.State.Value=state;yield return Wait(()=>All(4,p=>p.phase==2),"second round finishes");
    }
}
