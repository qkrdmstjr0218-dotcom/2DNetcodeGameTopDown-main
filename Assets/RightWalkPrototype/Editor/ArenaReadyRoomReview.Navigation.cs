using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    static bool navigationShooting;
    [MenuItem("Tools/Right Walk Prototype/Enable Foundry Roaming Enemies")]
    public static void EnableFoundryRoaming()
    {
        if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().path!=RelayFoundryBuilder.ScenePath||EditorSceneManager.GetActiveScene().isDirty)throw new System.Exception("Requires saved Foundry edit mode");
        var map=Object.FindFirstObjectByType<ArenaMapSettings>();Undo.RecordObject(map,"Enable roaming enemies");map.roamingEnemies=true;map.droneSpawns=new[]{new Vector2(-9,5),new Vector2(10,9)};
        EditorUtility.SetDirty(map);EditorSceneManager.MarkSceneDirty(map.gameObject.scene);EditorSceneManager.SaveScene(map.gameObject.scene);
    }
    [MenuItem("Tools/Right Walk Prototype/Verify Roaming Enemies")]
    public static void BeginEnemyNavigation(){BeginFoundry();File.WriteAllText(folder+"/navigation.txt","navigation");}
    static void NavigationCommand(SideScrollNetworkPlayer own,int action)
    {
        navigationShooting=action==85;var move=own.GetComponent<SideScrollMapPreview>();
        if(action==81)move.SetSpawnPosition(new Vector2(-16,.04f));
        if(action==82){navigationShooting=false;move.SetSpawnPosition(new Vector2(31+own.PlayerNumber*.5f,.04f));own.SetSpecialHeld(false);}
        if(action==83)move.SetSpawnPosition(new Vector2(-2.8f,-2.76f));
        if(action==84)move.SetSpawnPosition(new Vector2(-12,3.64f));
        if(action==86)move.SetSpawnPosition(new Vector2(-14,3.64f));
        if(action==85){own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Rifle);own.SetSpecialHeld(true);}
    }
    static void NavigationShoot(SideScrollNetworkPlayer own,ArenaObjectiveMatch match)
    {
        if(!navigationShooting)return;
        foreach(var e in match.Patrols)if(e.Flying&&e.Health>0)
        {own.GetComponentInChildren<RightFacingAimRig>().SetAimTarget((Vector2)match.PatrolPosition(e)+Vector2.up*.48f);SampleFireButtons(own,true,true);return;}
    }
    static Bounds ReviewBounds(SideScrollNetworkPlayer p)=>(Bounds)typeof(SideScrollNetworkPlayer).GetProperty("ArenaHitBounds",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(p);
    static ArenaEnemyNavigation Graph(ArenaObjectiveMatch m,bool flying)=>(ArenaEnemyNavigation)typeof(ArenaObjectiveMatch).GetField(flying?"flightNavigation":"groundNavigation",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m);
    static void ResetBrain(ArenaObjectiveMatch m,int id)=>typeof(ArenaObjectiveMatch).GetMethod("ResetEnemyBrain",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,new object[]{id});
    static int DroneIndex(ArenaObjectiveMatch m){for(int i=0;i<m.Patrols.Count;i++)if(m.Patrols[i].Flying)return i;return -1;}
    static IEnumerator CheckEnemyNavigation()
    {
        navigationShooting=false;
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4),"four peers waiting",45);
        var m=ArenaObjectiveMatch.Instance;Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"all ready");m.roundSeconds=180;Require(m.StartRound(),"start navigation match");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.droneCount==2&&p.droneHealth==4&&p.turretCount==ConfiguredTurretCount),"two combat drones and existing turrets synchronized");
        Send(0,82);yield return Wait(()=>All(4,p=>p.position.x>31&&p.grounded),"players leave AI test area");
        int drone=DroneIndex(m);var ground=Graph(m,false);var air=Graph(m,true);
        Require(ground.Nodes.Count>100&&air.Nodes.Count>300,"navigation derives walkable surfaces and free flight cells from actual geometry");
        var route=ground.Route(new Vector2(-8,0),new Vector2(0,-2.8f),false);
        Require(route.Count>5&&Vector2.Distance(ground.Nodes[route[route.Count-1]].Point,new Vector2(0,-2.8f))<1,"ground route descends into lower bypass");
        for(int i=1;i<route.Count;i++)Require(ground.CanTraverse(ground.Nodes[route[i-1]],ground.Nodes[route[i]]),"route edge has floor and body clearance");
        for(int i=1;i<m.Patrols.Count;i++){var s=m.Patrols[i];s.Health=0;s.RespawnAt=m.Now+100;m.Patrols[i]=s;}
        Vector2 wander=m.Patrols[0].Position;yield return new WaitForSecondsRealtime(2);
        Require(Vector2.Distance(wander,m.Patrols[0].Position)>.8f,"unengaged robot wanders beyond its spawn point");
        Send(1,81);yield return Wait(()=>P(1).position.x< -15.8f&&P(1).grounded,"host presents chase target");
        yield return Wait(()=>m.Patrols[0].Position.x< -12.8f,"ground robot leaves old patrol interval to chase player",5);
        Send(1,83);yield return Wait(()=>P(1).position.y< -2.6f&&P(1).grounded,"target reaches lower bypass");
        var robot=m.Patrols[0];robot.Position=ground.Nodes[ground.Nearest(new Vector2(-7.9f,0))].Point;robot.Angle=0;robot.RushPhase=0;m.Patrols[0]=robot;ResetBrain(m,robot.Id);
        yield return Wait(()=>m.Patrols[0].Position.y<-.6f&&m.Patrols[0].Position.x> -7,"robot follows terrain down solid ramp",7);
        Send(0,82);yield return Wait(()=>All(4,p=>p.position.x>31),"players leave before bridge setup");
        var bridge=Object.FindFirstObjectByType<ArenaRetractingBridge>();yield return Wait(()=>bridge.CycleTime>10.7f&&bridge.CycleTime<12,"bridge fully open for unsupported-floor check",16);
        robot=m.Patrols[0];robot.Position=new Vector2(-22,.02f);robot.Velocity=Vector2.zero;m.Patrols[0]=robot;ResetBrain(m,robot.Id);
        yield return Wait(()=>m.Patrols[0].Position.y<-.8f,"ground robot falls when floor disappears",2);
        yield return Wait(()=>m.Patrols[0].Health==0,"robot falling through pit despawns instead of hovering",3);
        robot=m.Patrols[0];robot.Health=0;robot.RespawnAt=m.Now+100;m.Patrols[0]=robot;
        var d=m.Patrols[drone];d.Health=4;d.RespawnAt=0;d.Position=air.Nodes[air.Nearest(new Vector2(-11,4.3f))].Point;d.FireAt=0;d.NextAttack=m.Now+.5;m.Patrols[drone]=d;ResetBrain(m,d.Id);
        Send(2,84);yield return Wait(()=>P(2).grounded&&Mathf.Abs(P(2).position.x+12)<.1f&&P(2).position.y>3.3,"client presents close drone target");
        yield return Wait(()=>Vector2.Distance(m.PatrolPosition(m.Patrols[drone])+Vector3.up*.48f,ReviewBounds(Player(2)).center)>=3,"drone retreats to firing distance",5);
        yield return Wait(()=>P(2).hp<100,"moving drone fires real damaging projectiles at client",6);
        float distance=Vector2.Distance(m.PatrolPosition(m.Patrols[drone])+Vector3.up*.48f,ReviewBounds(Player(2)).center);
        Require(distance<6,"drone remains near useful firing band");
        yield return Wait(()=>Vector2.Distance(P(1).dronePosition,P(2).dronePosition)<.5f&&Vector2.Distance(P(1).dronePosition,P(4).dronePosition)<.5f,"four peers agree on drone motion");
        var position=m.Patrols[drone].Position;yield return new WaitForSecondsRealtime(.4f);Require(m.Patrols[drone].Position.y>3.8f&&ArenaEnemyNavigation.FlightClear(m.Patrols[drone].Position),"drone hovers without gravity and stays outside solid geometry");
        Send(1,86);yield return Wait(()=>Mathf.Abs(P(1).position.x+14)<.1f&&P(1).grounded,"host camera observes drone");yield return new WaitForSecondsRealtime(.6f);
        ScreenCapture.CaptureScreenshot(folder+"/combat-drone.png");yield return null;
        DropChance(m,1);int count=m.AmmoPickups.Count,health=m.HealthPickups.Count,generation=m.Patrols[drone].Generation,id=m.Patrols[drone].Id;
        Send(2,85);yield return Wait(()=>All(4,p=>p.droneHealth==0),"client rifle destroys moving drone",5);Send(2,82);
        Require(m.AmmoPickups.Count==count+1&&m.AmmoPickups[m.AmmoPickups.Count-1].Amount==1,"combat drone uses one-round ammunition drop");
        Require(m.HealthPickups.Count==health,"combat drones do not drop medical kits");m.DamagePatrolServer(id,generation,100);Require(m.AmmoPickups.Count==count+1,"duplicate drone death cannot duplicate loot");
        yield return Wait(()=>All(4,p=>p.droneHealth==4&&p.droneGeneration>generation),"drone respawns with new generation",12);
        m.DamagePatrolServer(id,generation,100);Require(m.Patrols[drone].Health==4,"old drone generation cannot take delayed damage");
        var round=m.State.Value;round.End=m.Now+.05;m.State.Value=round;yield return Wait(()=>All(4,p=>p.phase==2&&p.droneCount==0&&p.turretCount==0&&p.turretShots==0),"finish removes all enemy kinds and bullets");
        Require(m.ReturnToRoom(),"return after AI review");yield return Wait(()=>All(4,p=>p.phase==0&&p.droneCount==0),"waiting room has no combat drones");
    }
}
