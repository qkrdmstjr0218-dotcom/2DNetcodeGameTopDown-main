using System.Collections;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    static void DropChance(ArenaObjectiveMatch match,float value)
    {var so=new SerializedObject(match);so.FindProperty("patrolDropChance").floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static IEnumerator CheckEnemies()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.enemiesCount==0),"four peers waiting without enemies",45);
        var match=ArenaObjectiveMatch.Instance;
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");match.roundSeconds=120;Require(match.StartRound(),"start enemy round");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.enemiesCount==2&&p.enemyHealth==4),"two living patrols synchronized");
        var prefab=new SerializedObject(UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>().arenaObjectivePrefab.GetComponent<ArenaObjectiveMatch>());
        Require(All(4,p=>Mathf.Approximately(p.shotInterval,prefab.FindProperty("normalShotInterval").floatValue)),"all peers share prefab normal fire interval");
        var enemy=match.Patrols[0];int id=enemy.Id,gen=enemy.Generation;float before=P(1).enemyPosition.x;
        yield return new WaitForSecondsRealtime(.7f);Require(P(1).enemyPosition.x!=before&&All(4,p=>p.enemyPosition.x>=-16&&p.enemyPosition.x<=-11),"patrol moves inside shared route");
        Require(Mathf.Abs(P(1).enemyPosition.x-P(2).enemyPosition.x)<.4f,"client and server patrol clocks agree");
        Send(1,33);yield return new WaitForSecondsRealtime(.8f);ScreenCapture.CaptureScreenshot(folder+"/patrol-alive.png");yield return new WaitForSecondsRealtime(.25f);
        Send(2,34);yield return Wait(()=>P(2).hp==90&&P(2).sawHit&&P(2).lastHitX<-1,"contact pushes client outward immediately",3);
        Require(P(2).lastHitY<.5f,"contact shove does not add upward jump");
        yield return new WaitForSecondsRealtime(.22f);Require(!P(2).hitReacting,"short contact input lock releases");
        Send(2,28);yield return Wait(()=>P(2).position.x>-.5f,"client leaves first shove");
        yield return new WaitForSecondsRealtime(1);Send(2,31);yield return Wait(()=>P(2).hp==80,"contact damages participant");yield return new WaitForSecondsRealtime(.25f);Require(Player(2).Health==80,"contact damage has cooldown");
        yield return Wait(()=>P(2).hp==70,"contact damage repeats after interval",2);Send(2,28);yield return Wait(()=>P(2).position.x>-.5f,"participant leaves hazard");
        DropChance(match,1);double began=match.Now;int shots=Player(1).ReceivedShots;Send(1,29);
        yield return Wait(()=>Player(1).ReceivedShots>shots,"first normal shot accepted");yield return new WaitForSecondsRealtime(.06f);ScreenCapture.CaptureScreenshot(folder+"/normal-tracer.png");yield return null;
        yield return Wait(()=>match.Patrols[0].Health<4&&match.Patrols[0].Health>0,"nonfatal shot produces hit response");
        ScreenCapture.CaptureScreenshot(folder+"/patrol-hit-tracer.png");yield return null;
        yield return Wait(()=>All(4,p=>p.enemyHealth==0),"host normal bullets destroy moving patrol",10);Send(1,16);
        Require(match.Now-began>=match.NormalAmmoRules.ShotInterval*3-.02&&Player(1).ReceivedShots-shots>=4,"normal kill respects slower server fire interval");
        yield return Wait(()=>UnityEngine.Object.FindObjectsByType<ArenaPatrolBurst>(FindObjectsSortMode.None).Length>0,"destruction burst spawned locally",1);
        ScreenCapture.CaptureScreenshot(folder+"/patrol-destruction.png");yield return null;
        int drops=match.AmmoPickups.Count;Require(drops==3&&match.AmmoPickups[2].Amount==1&&match.AmmoPickups[2].Respawn==0,"forced drop is one shotgun round and one-use");
        match.DamagePatrolServer(id,gen,10);Require(match.AmmoPickups.Count==drops,"duplicate destruction does not duplicate drop");
        Require(UnityEngine.Object.FindObjectsByType<ArenaPatrolEnemy>(FindObjectsSortMode.None).Length==1,"destroyed hazard disables visible shot collider");
        yield return Wait(()=>All(4,p=>p.enemyHealth==4&&p.enemyGeneration>gen),"disabled patrol respawns with new generation",12);
        match.DamagePatrolServer(id,gen,100);Require(match.Patrols[0].Health==4,"old-generation delayed damage rejected");
        DropChance(match,0);Send(2,30);yield return Wait(()=>All(4,p=>p.enemyHealth==0),"client shotgun destroys patrol",8);Send(2,21);
        Require(match.AmmoPickups.Count==drops&&Player(2).ShotgunRounds<6,"zero drop chance and shotgun consumption respected");
        var state=match.State.Value;state.End=match.Now+.1;match.State.Value=state;
        yield return Wait(()=>All(4,p=>p.phase==2&&p.enemiesCount==0&&p.pickupsCount==0),"finish clears enemies and drops");
        match.DamagePatrolServer(id,gen+1,100);Require(match.Patrols.Count==0,"finished match refuses enemy damage");
        Require(match.ReturnToRoom(),"return after patrol test");yield return Wait(()=>All(4,p=>p.phase==0&&p.enemiesCount==0),"waiting room has no patrol hazards");
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready again");Require(match.StartRound(),"start new enemy round");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.enemiesCount==2&&p.enemyHealth==4),"new round restores both enemies");
        Require(match.Patrols[0].Id!=id,"new round has distinct enemy identity");
    }
}
