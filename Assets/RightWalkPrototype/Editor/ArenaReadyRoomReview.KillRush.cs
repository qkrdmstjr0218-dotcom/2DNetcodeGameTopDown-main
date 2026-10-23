using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Kill Score And Ground Rush")]
    public static void BeginKillRush(){BeginFoundry();File.WriteAllText(folder+"/kill-rush.txt","kill-rush");}
    static void KillRushCommand(SideScrollNetworkPlayer own,int action)
    {
        var move=own.GetComponent<SideScrollMapPreview>();
        if(action==87)move.SetSpawnPosition(new Vector2(own.PlayerNumber==1?-16:own.PlayerNumber==2?-19:31+own.PlayerNumber*.5f,.04f));
        if(action>=88&&action<=91)own.StartCoroutine(CreditShot(own,action));
        if(action==92)
        {var b=ArenaObjectiveMatch.Instance.Points[ArenaObjectiveMatch.Instance.State.Value.Point].CaptureBounds;move.SetSpawnPosition(new Vector2(b.center.x,b.min.y+.04f));}
        if(action==93)move.SetSpawnPosition(new Vector2(-18,.04f));
        if(action==94)move.SetSpawnPosition(ArenaObjectiveMatch.Instance.PatrolPosition(ArenaObjectiveMatch.Instance.Patrols[0])+Vector3.right*1.1f+Vector3.up*.04f);
        if(action==95)move.SetSpawnPosition(new Vector2(-14.4f,.04f));
    }
    static IEnumerator CreditShot(SideScrollNetworkPlayer own,int action)
    {
        var kind=action==88?SideScrollNetworkPlayer.AmmoKind.Normal:action==89?SideScrollNetworkPlayer.AmmoKind.InstantShotgun:action==90?SideScrollNetworkPlayer.AmmoKind.Rifle:SideScrollNetworkPlayer.AmmoKind.Grenade;
        own.SelectAmmo(kind);own.SetSpecialHeld(action!=88);
        yield return new WaitForSecondsRealtime(SideScrollNetworkPlayer.WeaponSwitchSeconds+.1f);
        var target=Player(1);own.GetComponentInChildren<RightFacingAimRig>().SetAimTarget(ReviewBounds(target).center);
        yield return null;own.GetComponentInChildren<RightFacingShotPreview>().Fire();
    }
    static int Kills(ArenaObjectiveMatch m,SideScrollNetworkPlayer p){foreach(var s in m.Scores)if(s.Client==p.OwnerClientId)return s.Kills;return 0;}
    static IEnumerator ResetCreditVictim(ArenaObjectiveMatch m)
    {
        var victim=Player(1);uint old=victim.LifeVersion;victim.ResetForArenaRound();
        yield return Wait(()=>P(1).life>old&&P(1).hp==100,"new victim life synchronized");Send(0,87);
        yield return Wait(()=>P(1).grounded&&Mathf.Abs(P(1).position.x+16)<.1f&&P(2).grounded&&Mathf.Abs(P(2).position.x+19)<.1f,"outside-zone shot row ready");
    }
    static IEnumerator CheckKillRush()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4),"four peers waiting",45);
        var m=ArenaObjectiveMatch.Instance;Player(1).ReceiveServerDamage(1000,Player(1).LifeVersion,Player(2));
        Require(!Player(1).IsDead&&Kills(m,Player(2))==0,"practice damage cannot award kill score");
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"all ready");m.roundSeconds=180;Require(m.StartRound(),"start kill-rush match");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.droneCount==2),"combat started");
        for(int i=0;i<m.Patrols.Count;i++){var e=m.Patrols[i];e.Health=0;e.RespawnAt=m.Now+300;m.Patrols[i]=e;}
        for(int i=0;i<m.Scores.Count;i++)if(m.Scores[i].Client==Player(1).OwnerClientId){var s=m.Scores[i];s.Seconds=7;m.Scores[i]=s;}
        for(int action=88;action<=91;action++)
        {
            yield return ResetCreditVictim(m);var victim=Player(1);uint life=victim.LifeVersion;
            Require(!m.Points[m.State.Value.Point].Contains(victim),"kill does not require objective proximity");
            victim.ReceiveServerDamage(99,life,Player(3));yield return Wait(()=>P(1).hp==1,"victim health setup synchronized");
            int before=Kills(m,Player(2));Send(2,action);
            yield return Wait(()=>Player(1).IsDead&&Kills(m,Player(2))==before+1,"client weapon credits final hit: "+action,5);
            Require(Kills(m,Player(3))==0,"nonlethal contributor receives no split score");
            Require(Mathf.Abs(m.ScoreFor(Player(1).OwnerClientId)-7)<.01f,"victim retains existing capture score");
            victim.ReceiveServerDamage(100,life,Player(3));Require(Kills(m,Player(3))==0&&Kills(m,Player(2))==before+1,"dead target cannot award a duplicate kill");
        }
        Require(Mathf.Abs(m.ScoreFor(Player(2).OwnerClientId)-20)<.01f,"four weapon kills total twenty points");
        yield return Wait(()=>All(4,p=>p.clientKills==4&&Mathf.Abs(p.clientScore-20)<.01f&&Mathf.Abs(p.hostScore-7)<.01f),"all peers receive totals and kill counts");
        yield return ResetCreditVictim(m);var host=Player(1);uint generation=host.LifeVersion;
        host.ReceiveServerDamage(100,generation-1,Player(3));Require(host.Health==100,"stale life damage cannot score");
        host.ReceiveServerDamage(100,generation,host);Require(host.IsDead&&Kills(m,host)==0,"self-kill has no reward");
        yield return ResetCreditVictim(m);host.ReceiveServerDamage(50,host.LifeVersion,Player(2));host.ReceiveServerDamage(100,host.LifeVersion);
        Require(host.IsDead&&Kills(m,Player(2))==4,"environmental final blow has no last-attacker reward");
        yield return ResetCreditVictim(m);Send(1,92);yield return Wait(()=>m.Points[m.State.Value.Point].Contains(host),"victim stands inside active objective");
        float saved=m.ScoreFor(host.OwnerClientId);host.ReceiveServerDamage(100,host.LifeVersion,Player(2));
        Require(Mathf.Abs(m.ScoreFor(host.OwnerClientId)-saved)<.001f&&Kills(m,Player(2))==5,"inside-zone death awards kill without stealing capture points");
        yield return ResetCreditVictim(m);Send(0,82);yield return Wait(()=>All(4,p=>p.position.x>31),"players leave rush setup");
        var ground=Graph(m,false);var robot=m.Patrols[0];robot.Health=4;robot.RespawnAt=0;robot.RushPhase=0;robot.Position=ground.Nodes[ground.Nearest(new Vector2(-12,0))].Point;robot.Angle=0;robot.Velocity=Vector2.zero;m.Patrols[0]=robot;ResetBrain(m,robot.Id);
        Send(1,93);yield return Wait(()=>P(1).grounded&&Mathf.Abs(P(1).position.x+18)<.1f,"host at chase distance");
        yield return Wait(()=>m.Patrols[0].Speed>2.4f&&m.Patrols[0].RushPhase==0&&m.Patrols[0].Position.x< -12.5f,"detected ground robot chases faster than walking player",3);
        yield return Wait(()=>m.Patrols[0].RushPhase==1,"close target triggers telegraphed rush",4);Vector2 windup=m.Patrols[0].Position;float direction=m.Patrols[0].Aim.x;
        yield return new WaitForSecondsRealtime(.15f);Require(Vector2.Distance(windup,m.Patrols[0].Position)<.02f,"robot stops during windup");
        ScreenCapture.CaptureScreenshot(folder+"/rush-warning.png");Send(1,94);
        yield return Wait(()=>m.Patrols[0].RushPhase==2,"rush starts after warning",2);
        Require(m.Patrols[0].Aim.x==direction&&direction<0,"moving target cannot reverse committed rush direction");
        yield return Wait(()=>m.Patrols[0].Position.x<windup.x-.4f,"rush moves quickly along committed direction",1);
        yield return Wait(()=>m.Patrols[0].RushPhase==3,"rush ends with recovery pause",2);Vector2 rest=m.Patrols[0].Position;
        yield return new WaitForSecondsRealtime(.3f);Require(Vector2.Distance(rest,m.Patrols[0].Position)<.02f&&Player(1).Health==100,"missed rush leaves punishable stationary opening");
        Send(0,82);yield return Wait(()=>All(4,p=>p.position.x>31),"players leave hit setup");
        robot=m.Patrols[0];robot.Position=ground.Nodes[ground.Nearest(new Vector2(-12,0))].Point;robot.RushPhase=0;robot.Angle=0;m.Patrols[0]=robot;ResetBrain(m,robot.Id);
        Send(2,95);yield return Wait(()=>P(2).grounded&&Mathf.Abs(P(2).position.x+14.4f)<.1f,"client in rush path");
        yield return Wait(()=>P(2).hp==90&&m.Patrols[0].RushPhase==3,"rush hits client once and stops on contact",4);
        yield return new WaitForSecondsRealtime(.3f);Require(Player(2).Health==90,"recovery does not repeat contact damage immediately");
        Send(0,82);var round=m.State.Value;round.End=m.Now+.05;m.State.Value=round;
        yield return Wait(()=>All(4,p=>p.phase==2),"match finishes");Require(m.Result.Value.ToString().Contains("P2"),"final ranking includes kill score");
        int kills=Kills(m,Player(2));Player(1).ReceiveServerDamage(100,Player(1).LifeVersion,Player(2));Require(Kills(m,Player(2))==kills,"finished match cannot add kill score");
        Require(m.ReturnToRoom(),"return to room");yield return Wait(()=>All(4,p=>p.phase==0),"everyone returns");
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"ready for next round");Require(m.StartRound(),"start fresh match");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.clientKills==0&&p.clientScore<1),"next match resets kill score");
        var config=new SerializedObject(m);config.FindProperty("playerKillPoints").floatValue=3;config.ApplyModifiedPropertiesWithoutUndo();
        yield return Wait(()=>All(4,p=>Mathf.Approximately(p.killReward,3)),"one server reward setting synchronizes HUD rule to all peers");
        Player(1).ReceiveServerDamage(100,Player(1).LifeVersion,Player(2));
        yield return Wait(()=>All(4,p=>p.clientKills==1&&Mathf.Abs(p.clientScore-3)<.01f),"changed shared reward is used for actual lethal damage");
    }
}
