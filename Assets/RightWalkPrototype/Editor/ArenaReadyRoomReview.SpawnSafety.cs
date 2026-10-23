using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Foundry Spawn Safety")]
    public static void BeginSpawnSafety(){BeginFoundry();File.WriteAllText(folder+"/spawn-safety.txt","spawn");}
    static int NearestSpawn(ArenaMapSettings map,Vector2 at)
    {for(int i=0;i<map.respawnPoints.Length;i++)if(Vector2.Distance(map.respawnPoints[i],at)<.25f)return i;return -1;}
    static IEnumerator CheckSpawnSafety()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4),"four peers waiting",45);
        Require(All(4,p=>Mathf.Abs(p.walkSetting-2.8f)<.01f&&Mathf.Abs(p.runSetting-4.5f)<.01f),"every owner uses walk 2.8 and sprint 4.5");
        var m=ArenaObjectiveMatch.Instance;var map=Object.FindFirstObjectByType<ArenaMapSettings>();Require(map.respawnPoints.Length==8,"eight respawn candidates configured");
        var entryMethod=typeof(ArenaObjectiveMatch).GetMethod("BeginEnemyEntry",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        var args=new object[]{new ArenaObjectiveMatch.PatrolState{Angle=30},map};entryMethod.Invoke(m,args);
        var entry=(ArenaObjectiveMatch.PatrolState)args[0];Require(entry.Angle==0&&entry.EntrySeconds==map.enemyEntrySeconds&&entry.EntryUntil>m.Now,"sloped death pose resets at flat entrance");
        args=new object[]{new ArenaObjectiveMatch.PatrolState{Angle=30},null};entryMethod.Invoke(m,args);entry=(ArenaObjectiveMatch.PatrolState)args[0];
        Require(entry.Angle==30&&entry.EntrySeconds==0&&entry.EntryUntil==0,"legacy map without entrance settings keeps original spawn policy");
        Require(map.transform.Find(FoundrySpawnBuilder.RootName)==null,"rejected middle route covers stay removed");
        foreach(var at in map.respawnPoints)Require(map.SpawnCandidateValid(at),"supported unblocked spawn "+at);
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");m.roundSeconds=120;Require(m.StartRound(),"start spawn-safety match");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.hp==100&&p.grounded),"all owners arrive and stand on ground");
        for(int n=1;n<=4;n++)Require(Vector2.Distance(P(n).position,FoundrySpawnBuilder.Starts[n-1])<.25f,"owner reaches distributed start "+n);
        for(int a=1;a<=4;a++)for(int b=a+1;b<=4;b++)
        {
            Vector2 start=P(a).position+Vector3.up*1.2f,end=P(b).position+Vector3.up*1.2f;float distance=Vector2.Distance(start,end);
            Require(distance>=10,"starting pair is separated "+a+"/"+b);
            // Balconies separate arrivals without obstructing the existing middle route.
        }
        Require(ArenaGrenadeBallistics.WorldTrace(FoundrySpawnBuilder.Starts[2]+Vector2.up*1.2f,new Vector2(-19,7.8f),0,out _)&&ArenaGrenadeBallistics.WorldTrace(FoundrySpawnBuilder.Starts[3]+Vector2.up*1.2f,new Vector2(19,7.8f),0,out _),"upper starts are screened from nearby turrets");
        for(int i=0;i<m.Patrols.Count;i++){var e=m.Patrols[i];e.Health=0;e.RespawnAt=m.Now+300;m.Patrols[i]=e;}
        var transit=m.MedicalTransit.Value;transit.NextAt=m.Now+300;m.MedicalTransit.Value=transit;
        Send(1,111);yield return Wait(()=>Mathf.Abs(P(1).position.x+18)<.1f&&P(1).grounded,"host stands on flat walking test floor");
        Send(1,5);yield return Wait(()=>Mathf.Abs(P(1).velocityX-2.8f)<.08f,"actual walking velocity reaches 2.8",2);yield return new WaitForSecondsRealtime(.9f);
        Send(1,111);yield return Wait(()=>Mathf.Abs(P(1).position.x+18)<.1f,"reset flat sprint test");Send(1,24);
        yield return Wait(()=>Mathf.Abs(P(1).velocityX-4.5f)<.08f,"actual sprint velocity remains 4.5",2);yield return new WaitForSecondsRealtime(.9f);
        Send(1,110);yield return Wait(()=>Mathf.Abs(P(1).position.x-30)<.1f&&P(1).grounded,"host threatens old client spawn");
        var victim=Player(2);uint old=victim.LifeVersion;SetSupplyAmmo(victim,ArenaObjectiveMatch.SupplyAmmo.Shotgun,2);SetSupplyAmmo(victim,ArenaObjectiveMatch.SupplyAmmo.Rifle,1);SetSupplyAmmo(victim,ArenaObjectiveMatch.SupplyAmmo.Grenade,0);victim.GrantShieldServer(old);victim.ReceiveServerDamage(1000,old,null,true);
        yield return Wait(()=>P(2).hp==0,"client dies beside spawn camper");
        yield return Wait(()=>P(2).hp==100&&P(2).life>old&&P(2).grounded,"client automatically respawns on supported candidate",9);
        int index=NearestSpawn(map,P(2).position);Require(index>=0&&Vector2.Distance(P(2).position,new Vector2(31,.04f))>8,"respawn avoids camped original location");
        float best=float.NegativeInfinity;foreach(var at in map.respawnPoints)best=Mathf.Max(best,map.SpawnSafetyScore(at,victim));
        Require(map.SpawnSafetyScore(map.respawnPoints[index],victim)>=best-.2f,"server selected a highest-scoring available spawn");
        Require(P(2).ammo==2&&P(2).rifleAmmo==1&&P(2).grenadeAmmo==0&&P(2).normal==P(2).normalCapacity&&P(2).energy==100&&victim.Shield==0,"new respawn keeps special ammo and restores normal health stamina without shield");
        RelayArenaPrototype.Instance.overview=true;yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(folder+"/selected-spawn.png");yield return new WaitForSecondsRealtime(.15f);RelayArenaPrototype.Instance.overview=false;
        uint h=Player(1).LifeVersion,c=victim.LifeVersion;Player(1).ReceiveServerDamage(1000,h,null,true);victim.ReceiveServerDamage(1000,c,null,true);
        yield return Wait(()=>P(1).hp==0&&P(2).hp==0,"two players die together");
        yield return Wait(()=>P(1).hp==100&&P(2).hp==100&&P(1).life>h&&P(2).life>c&&P(1).grounded&&P(2).grounded,"both simultaneous respawns settle",9);
        Require(NearestSpawn(map,P(1).position)>=0&&NearestSpawn(map,P(2).position)>=0&&Vector2.Distance(P(1).position,P(2).position)>=8,"simultaneous respawns do not stack or land adjacent");
        var saved=map.respawnPoints;map.respawnPoints=new[]{new Vector2(26,-3)};Require(!map.TryChooseRespawn(victim,out _),"unsafe pit-only candidates are rejected");map.respawnPoints=saved;
        var state=m.State.Value;state.End=m.Now+.05;m.State.Value=state;yield return Wait(()=>All(4,p=>p.phase==2),"round finishes");Require(m.ReturnToRoom(),"return to waiting room");yield return Wait(()=>All(4,p=>p.phase==0),"ready room uses its original arrival layout");
    }
}
