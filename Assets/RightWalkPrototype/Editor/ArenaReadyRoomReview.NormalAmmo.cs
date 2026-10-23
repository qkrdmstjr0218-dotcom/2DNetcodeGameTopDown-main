using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using RightWalkPrototype;

public static partial class ArenaReadyRoomReview
{
    static void InvokeNormalShot(SideScrollNetworkPlayer player,uint generation)
    {
        var aim=player.GetComponentInChildren<RightFacingAimRig>();
        typeof(SideScrollNetworkPlayer).GetMethod("FireNormalAmmoServerRpc",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(player,new object[]{(Vector2)aim.MuzzlePosition,aim.AimDirection,generation});
    }
    static double LastNormal(SideScrollNetworkPlayer player)=>(double)typeof(SideScrollNetworkPlayer).GetField("lastNormalShot",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(player);
    static IEnumerator CaptureNormal(string label){yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(folder+"/"+label+".png");}
    static IEnumerator CheckNormal()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.normalCapacity>0&&p.normal==p.normalCapacity),"four peers full normal ammo",45);
        var match=ArenaObjectiveMatch.Instance;var rules=match.NormalAmmoRules;
        var prefab=new SerializedObject(UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>().arenaObjectivePrefab.GetComponent<ArenaObjectiveMatch>());
        Require(rules.Capacity==prefab.FindProperty("normalAmmoCapacity").intValue&&Mathf.Approximately(rules.Delay,prefab.FindProperty("normalAmmoRechargeDelay").floatValue)&&Mathf.Approximately(rules.Rate,prefab.FindProperty("normalAmmoRechargeRate").floatValue),"server ammo settings match prefab");
        Require(All(4,p=>p.normalDelay==rules.Delay&&p.normalRate==rules.Rate),"ammo settings synchronized");
        Send(1,18);yield return new WaitForSecondsRealtime(.3f);Require(Player(1).NormalAmmo<rules.Capacity&&Player(1).ReceivedShots>0,"waiting room accepts practice fire RPC");
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");match.roundSeconds=120;Require(match.StartRound(),"start normal ammo round");
        yield return Wait(()=>All(4,p=>p.phase==5),"countdown setup");int countdownShots=Player(2).ReceivedShots;Send(2,18);yield return new WaitForSecondsRealtime(.3f);Require(Player(2).NormalAmmo==rules.Capacity&&Player(2).ReceivedShots==countdownShots,"countdown rejects ammo use");
        yield return Wait(()=>All(4,p=>p.phase==1),"round begins");
        foreach(int owner in new[]{1,2})
        {
            var player=Player(owner);int before=player.ReceivedShots;Send(owner,15);
            yield return Wait(()=>player.NormalAmmo<.01f,"normal magazine empties P"+owner,6);Send(owner,16);
            Require(player.ReceivedShots-before==rules.Capacity,"one normal round per accepted shot P"+owner);
            Require(Player(owner==1?2:1).NormalAmmo>=rules.Capacity-.1f,"other player's ammo independent P"+owner);
            double last=LastNormal(player);int shots=player.ReceivedShots;Send(owner,18);yield return new WaitForSecondsRealtime(.25f);
            Require(player.ReceivedShots==shots&&player.NormalAmmo<.01f,"empty and old unlimited RPC paths rejected P"+owner);
            float sample=Mathf.Min(.55f,rules.Capacity/rules.Rate*.4f);
            yield return Wait(()=>match.Now>=last+rules.Delay+sample,"refill sample time P"+owner);
            float expected=(float)(match.Now-last-rules.Delay)*rules.Rate;
            Require(Mathf.Abs(player.NormalAmmo-expected)<.5f&&player.NormalAmmo>1,"gradual refill follows configured rate P"+owner);
            yield return Wait(()=>player.NormalAmmo>=rules.Capacity-.001f,"normal ammo fully recharges P"+owner,rules.Delay+rules.Capacity/rules.Rate+2);
            yield return Wait(()=>P(owner).normal>=rules.Capacity-.1f,"owner receives refilled state P"+owner);
        }
        Send(1,15);yield return Wait(()=>Player(1).NormalAmmo<=rules.Capacity-4,"partial ammo for UI");Send(1,16);Send(1,17);
        yield return Wait(()=>P(1).ammoCursor&&!P(1).osCursor,"ring replaces OS cursor in gameplay",5);
        yield return new WaitForSecondsRealtime(.4f);Send(1,22,true);yield return Wait(()=>P(1).osCursor&&!P(1).ammoCursor,"menu restores OS cursor");Send(1,22,false);
        yield return Wait(()=>Player(1).NormalAmmo>=rules.Capacity-.001f,"full before shotgun");
        Send(1,20);yield return Wait(()=>Player(1).ShotgunRounds==5,"shotgun still consumes its own round");Send(1,16);Send(1,17);yield return new WaitForSecondsRealtime(.4f);
        Require(Player(1).NormalAmmo==rules.Capacity,"shotgun does not spend normal rounds");
        Send(1,20);yield return Wait(()=>Player(1).ShotgunRounds==0,"shotgun capacity preserved",6);Send(1,16);yield return new WaitForSecondsRealtime(.8f);
        InvokeNormalShot(Player(1),Player(1).LifeVersion);Require(Player(1).NormalRounds==rules.Capacity-1,"normal ammunition remains usable after shotgun exhausted");Send(1,21);
        uint old=Player(2).LifeVersion;float atDeath=Player(2).NormalAmmo;
        Send(2,15);yield return Wait(()=>Player(2).NormalAmmo<=rules.Capacity-3,"spend ammo before death");Send(2,16);atDeath=Player(2).NormalAmmo;Player(2).ReceiveServerDamage(100,old);
        yield return new WaitForSecondsRealtime(.6f);Require(Player(2).IsDead&&Mathf.Abs(Player(2).NormalAmmo-atDeath)<.15f,"dead player cannot recharge");
        yield return Wait(()=>Player(2).LifeVersion>old&&!Player(2).IsDead&&Player(2).NormalAmmo==rules.Capacity,"normal automatic recharge reaches full after revive",match.RespawnSeconds+2);
        int prior=Player(2).ReceivedShots;File.WriteAllText(folder+"/command.json",JsonUtility.ToJson(new Command{id=++revision,target=2,action=19,generation=old}));yield return new WaitForSecondsRealtime(.3f);
        Require(Player(2).NormalAmmo==rules.Capacity&&Player(2).ReceivedShots==prior,"old-life shot does not spend fresh ammo");
        Send(1,15);yield return Wait(()=>Player(1).NormalAmmo<=rules.Capacity-3,"spend before finish");Send(1,16);
        var state=match.State.Value;state.End=match.Now+.1;match.State.Value=state;yield return Wait(()=>All(4,p=>p.phase==2),"round finishes");float ended=Player(1).NormalAmmo;
        Send(1,18);yield return new WaitForSecondsRealtime(rules.Delay+.5f);Require(Mathf.Abs(Player(1).NormalAmmo-ended)<.01f,"finished game blocks shooting and recharge");
        Require(match.ReturnToRoom(),"return to room");yield return Wait(()=>All(4,p=>p.phase==0&&p.normal==p.normalCapacity&&p.ammo==6),"return restores both ammo types");
    }
}
