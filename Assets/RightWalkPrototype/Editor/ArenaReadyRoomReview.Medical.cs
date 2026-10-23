using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    static bool medicalTouch;
    [MenuItem("Tools/Right Walk Prototype/Enable Foundry Medical Drone")]
    public static void EnableFoundryMedical()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)throw new System.Exception("Requires saved Foundry edit mode");
        var map=Object.FindFirstObjectByType<ArenaMapSettings>();Undo.RecordObject(map,"Enable medical supply drone");map.medicalSupplyDrones=true;
        EditorUtility.SetDirty(map);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    [MenuItem("Tools/Right Walk Prototype/Verify Medical Drone")]
    public static void BeginMedicalDrone(){BeginFoundry();File.WriteAllText(folder+"/medical.txt","medical");}
    [MenuItem("Tools/Right Walk Prototype/Capture Medical Warning")]
    public static void CaptureMedicalWarning(){BeginMedicalDrone();File.WriteAllText(folder+"/medical-capture.txt","capture");}
    [MenuItem("Tools/Right Walk Prototype/Verify Shield Combat")]
    public static void BeginShieldCombat(){BeginMedicalDrone();File.WriteAllText(folder+"/shield-combat.txt","combat");}
    static int MedicIndex(ArenaObjectiveMatch m){for(int i=0;i<m.Patrols.Count;i++)if(m.Patrols[i].Medical&&m.Patrols[i].Health>0)return i;return -1;}
    static void MedicalCommand(SideScrollNetworkPlayer own,int action,bool ready)
    {
        var m=ArenaObjectiveMatch.Instance;int index=MedicIndex(m);
        if(action==97)medicalTouch=ready;
        if(action==98){medicalTouch=false;own.StartCoroutine(ShootMedical(own));}
        if(action==99)
        {
            medicalTouch=false;foreach(var item in m.HealthPickups)if(item.Respawn==0){own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(item.Position+Vector2.up*.04f);break;}
        }
        if(action==100&&index>=0)
        {
            var drone=m.Patrols[index];float x=Mathf.Clamp(drone.Position.x-Mathf.Sign(drone.Velocity.x)*6,-31,31);
            if(ArenaEnemyNavigation.FloorBelow(new Vector2(x,drone.Position.y+.1f),25,out var floor))own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(floor.point+Vector2.up*.04f);
        }
    }
    static void FollowMedicalForReview(SideScrollNetworkPlayer own,ArenaObjectiveMatch m)
    {int index=MedicIndex(m);if(medicalTouch&&index>=0)own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(m.Patrols[index].Position+Vector2.down*.1f);}
    static IEnumerator ShootMedical(SideScrollNetworkPlayer own)
    {
        var m=ArenaObjectiveMatch.Instance;int index=MedicIndex(m);if(index<0)yield break;
        var drone=m.Patrols[index];float x=Mathf.Clamp(drone.Position.x-Mathf.Sign(drone.Velocity.x)*3,-31,31);
        if(ArenaEnemyNavigation.FloorBelow(new Vector2(x,drone.Position.y+.1f),25,out var floor))own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(floor.point+Vector2.up*.04f);
        own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Rifle);own.SetSpecialHeld(true);yield return new WaitForSecondsRealtime(.35f);
        float until=Time.realtimeSinceStartup+4;
        while(Time.realtimeSinceStartup<until&&(index=MedicIndex(m))>=0)
        {
            drone=m.Patrols[index];own.GetComponentInChildren<RightFacingAimRig>().SetAimTarget(drone.Position+Vector2.up*.48f);
            own.GetComponentInChildren<RightFacingShotPreview>().Fire();yield return null;
        }
        own.SetSpecialHeld(false);
    }
    static void ForceOppositeMedicalPlan(ArenaObjectiveMatch m,float originalDirection)
    {
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var map=Object.FindFirstObjectByType<ArenaMapSettings>();
        object plan=null;List<Vector2> points=null;Vector2 end=default;
        for(int i=0;i<24;i++)
        {
            var candidate=typeof(ArenaObjectiveMatch).GetMethod("PlanMedicalTrip",flags).Invoke(m,new object[]{map});if(candidate==null)continue;
            var type=candidate.GetType();var start=(Vector2)type.GetField("OutsideStart").GetValue(candidate);var exit=(Vector2)type.GetField("OutsideExit").GetValue(candidate);
            if(Mathf.Sign(exit.x-start.x)==originalDirection)continue;
            plan=candidate;points=(List<Vector2>)type.GetField("Points").GetValue(candidate);end=(Vector2)type.GetField("InnerExit").GetValue(candidate);break;
        }
        Require(plan!=null,"random planner can provide opposite-direction crossing");
        typeof(ArenaObjectiveMatch).GetField("plannedMedicalTrip",flags).SetValue(m,plan);
        // Only shorten the next wait in this fixture; real traversal and exit remain unchanged.
        m.MedicalTransit.Value=new ArenaObjectiveMatch.MedicalWindow{Valid=true,Entry=points[0],Exit=end,NextAt=m.Now+.4};
    }
    static IEnumerator CheckShieldCombat(ArenaObjectiveMatch m)
    {
        for(int i=0;i<m.Patrols.Count;i++){var e=m.Patrols[i];e.Health=0;e.RespawnAt=m.Now+300;m.Patrols[i]=e;}
        Send(0,35);yield return Wait(()=>Mathf.Abs(P(1).position.x+10)<.1f&&Mathf.Abs(P(2).position.x+6)<.1f&&P(1).grounded&&P(2).grounded,"combatants positioned on real terrain");
        var host=Player(1);Require(host.GrantShieldServer(host.LifeVersion),"host obtains protective shield");
        yield return Wait(()=>P(1).shield==50,"host shield visible");yield return new WaitForSecondsRealtime(.3f);
        ScreenCapture.CaptureScreenshot(folder+"/shield-visible.png");yield return new WaitForSecondsRealtime(.15f);
        int rounds=P(2).ammo;Send(2,37);
        yield return Wait(()=>P(2).ammo==rounds-1&&P(1).shield==10&&P(1).hp==100,"actual client shotgun consumes ammo and absorbs forty damage",5);
        yield return Wait(()=>P(1).sawHit&&P(1).position.x< -10.2f,"shield preserves shotgun hit reaction and directional knockback",3);
        yield return new WaitForSecondsRealtime(.35f);ScreenCapture.CaptureScreenshot(folder+"/shield-damaged.png");yield return null;
        host.ReceiveServerDamage(110,host.LifeVersion,Player(2));
        yield return Wait(()=>P(1).hp==0&&P(1).shield==0,"lethal overflow breaks shield and kills");
        Require(m.ScoreFor(Player(2).OwnerClientId)==m.PlayerKillReward,"shield overflow preserves kill attribution");
        var state=m.State.Value;state.End=m.Now+.05;m.State.Value=state;
        yield return Wait(()=>All(4,p=>p.phase==2&&p.shield==0),"round end clears shields");
    }
    static IEnumerator CheckMedicalDrone()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.medicCount==0),"four peers waiting without supply craft",45);
        var m=ArenaObjectiveMatch.Instance;Require(m.MedicalTransit.Value.NextAt==0,"no supply schedule in ready room");
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"all ready");m.roundSeconds=180;Require(m.StartRound(),"start medical-drone match");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.healthCount==2&&p.droneCount==2&&p.turretCount==ConfiguredTurretCount),"fixed healing and existing enemies retained");
        if(File.Exists(folder+"/shield-combat.txt")){yield return CheckShieldCombat(m);yield break;}
        Require(m.MedicalTransit.Value.Valid&&m.MedicalTransit.Value.NextAt-m.Now>=9&&m.MedicalTransit.Value.NextAt-m.Now<=16.1,"first random arrival is bounded at ten to sixteen seconds");
        var map=Object.FindFirstObjectByType<ArenaMapSettings>();var window=m.MedicalTransit.Value;float direction=Mathf.Sign(window.Exit.x-window.Entry.x);
        Require(Mathf.Abs(window.Exit.x-window.Entry.x)>map.cameraBounds.width-7,"planned supply route crosses the map");
        for(int i=0;i<m.Patrols.Count;i++){var e=m.Patrols[i];e.Health=0;e.RespawnAt=m.Now+300;m.Patrols[i]=e;}
        Send(0,82);yield return Wait(()=>All(4,p=>p.position.x>31),"players leave scheduled flight path");
        yield return Wait(()=>All(4,p=>p.medicWarning),"arrival direction warning synchronized",17);yield return null;ScreenCapture.CaptureScreenshot(folder+"/medical-warning.png");yield return new WaitForSecondsRealtime(.2f);if(File.Exists(folder+"/medical-capture.txt"))yield break;
        yield return Wait(()=>All(4,p=>p.medicCount==1&&p.medicHealth==8),"single medical drone appears on all peers",6);
        int index=MedicIndex(m),id=m.Patrols[index].Id;uint life=Player(2).LifeVersion;
        Require(m.MedicalTransit.Value.NextAt-m.Now>=24&&m.MedicalTransit.Value.NextAt-m.Now<=40.1,"following randomized arrival remains bounded");
        m.DamagePatrolServer(id,99,100);Require(m.Patrols[index].Health==8,"old generation cannot destroy medical drone");
        yield return Wait(()=>MedicIndex(m)>=0&&Mathf.Abs(m.Patrols[MedicIndex(m)].Position.x)<16&&Mathf.Abs(m.Patrols[MedicIndex(m)].Position.x)>6,"drone travels from edge into reachable interior",20);
        Send(1,100);yield return new WaitForSecondsRealtime(.6f);ScreenCapture.CaptureScreenshot(folder+"/medical-flight.png");yield return null;
        Send(2,97,true);yield return new WaitForSecondsRealtime(1);
        index=MedicIndex(m);Require(index>=0&&m.Patrols[index].FireAt==0&&m.Patrols[index].BurstLeft==0&&m.TurretShots.Count==0&&Player(2).Health==100,"supply craft has no shooting or contact damage");
        Require(Vector2.Distance(P(1).medicPosition,P(2).medicPosition)<.6f&&Vector2.Distance(P(1).medicPosition,P(4).medicPosition)<.6f,"supply motion agrees across all peers");
        Send(2,97,false);DropChance(m,1);int ammo=m.AmmoPickups.Count,health=m.HealthPickups.Count;float score=m.ScoreFor(Player(2).OwnerClientId);Send(2,98);
        yield return Wait(()=>m.HealthPickups.Count==health+1,"client rifle destroys medical drone and releases kit",7);
        Require(m.AmmoPickups.Count==ammo&&m.HealthPickups[health].Shield&&m.HealthPickups[health].Amount==50&&m.HealthPickups[health].Respawn==0,"medical craft drops one shield pack instead of ammunition");
        var kit=m.HealthPickups[health];float initialHeight=kit.Position.y;
        m.DamagePatrolServer(id,1,100);Require(m.HealthPickups.Count==health+1,"duplicate death cannot duplicate medical kit");
        yield return Wait(()=>m.HealthPickups.Count==health+1&&!m.HealthPickups[health].Falling,"kit falls and settles on terrain",4);
        kit=m.HealthPickups[health];Require(kit.Position.y<initialHeight&&ArenaEnemyNavigation.FloorBelow(kit.Position+Vector2.up*.02f,.5f,out var under)&&Mathf.Abs(kit.Position.y-under.point.y-.3f-.35f*Mathf.Abs(under.normal.x)/Mathf.Max(.3f,under.normal.y))<.03f,"settled kit rests just above actual floor");
        Require(kit.ExpiresAt>m.Now+16&&m.ScoreFor(Player(2).OwnerClientId)==score,"kit keeps finite lifetime and NPC kill gives no player-kill points");
        // Hold the next arrival only in this fixture while testing the real fifteen-second expiry.
        var deferred=m.MedicalTransit.Value;deferred.NextAt=m.Now+120;m.MedicalTransit.Value=deferred;
        Send(2,99);
        yield return Wait(()=>All(4,p=>p.clientShield==50)&&P(2).hp==100&&m.HealthPickups.Count==health,"full-health client collects shield, synchronized to four peers",4);
        Require(Player(2).LifeVersion==life&&P(2).shieldTime>13,"shield keeps life and starts fifteen-second duration");
        yield return new WaitForSecondsRealtime(.2f);ScreenCapture.CaptureScreenshot(folder+"/shield-active.png");yield return null;
        Player(2).ReceiveServerDamage(20,life,Player(1));
        yield return Wait(()=>All(4,p=>p.clientShield==30)&&P(2).hp==100,"twenty damage absorbed without HP loss");
        Player(2).ReceiveServerDamage(40,life,Player(1));
        yield return Wait(()=>All(4,p=>p.clientShield==0)&&P(2).hp==90,"shield breaks and excess ten reaches HP");
        Require(!Player(2).GrantShieldServer(life+1),"stale life cannot receive shield");
        Require(Player(2).GrantShieldServer(life),"grant shield for refresh check");
        Player(2).ReceiveServerDamage(10,life);yield return new WaitForSecondsRealtime(.25f);
        Require(Player(2).GrantShieldServer(life)&&Player(2).Shield==50&&Player(2).ShieldRemaining>14.8,"reacquisition refreshes fifty without stacking");
        yield return Wait(()=>All(4,p=>p.clientShield==0),"shield expires naturally on all peers",17);
        Player(2).ReceiveServerDamage(10,life);Require(Player(2).Health==80,"expired shield no longer absorbs damage");
        Player(2).GrantShieldServer(life);Player(2).ResetForArenaRound();
        yield return Wait(()=>P(2).hp==100&&P(2).shield==0&&P(2).life!=life,"new life clears protection");
        yield return Wait(()=>Player(2).HasCurrentLifePose,"new life pose acknowledged");
        Require(Player(2).GrantShieldServer(Player(2).LifeVersion),"grant before environmental death");
        Player(2).ReceiveServerDamage(Player(2).MaxHealth,Player(2).LifeVersion,null,true);
        yield return Wait(()=>P(2).hp==0&&All(4,p=>p.clientShield==0),"fatal environment bypasses protection");
        yield return Wait(()=>P(2).hp==100,"automatic respawn after environmental death",8);
        Send(0,82);yield return Wait(()=>All(4,p=>p.position.x>31),"players leave escape test");
        yield return Wait(()=>MedicIndex(m)<0,"destroyed supply craft is removed rather than enemy respawn",2);
        ForceOppositeMedicalPlan(m,direction);yield return Wait(()=>MedicIndex(m)>=0,"opposite-direction supply appears",3);
        index=MedicIndex(m);int second=m.Patrols[index].Id;Vector2 start=m.Patrols[index].Position;float farthest=0;bool harmless=true;double deadline=m.Now+38;
        while(m.Now<deadline)
        {
            index=MedicIndex(m);if(index<0||m.Patrols[index].Id!=second)break;
            harmless&=m.Patrols[index].Health==8&&m.Patrols[index].FireAt==0;
            farthest=Mathf.Max(farthest,(m.Patrols[index].Position.x-start.x)*-direction);yield return new WaitForSecondsRealtime(.3f);
        }
        Require(harmless,"unattacked supply stays harmless throughout crossing");
        Require(farthest>map.cameraBounds.width-5,"opposite flight reaches the far map edge");
        yield return Wait(()=>All(4,p=>p.medicCount==0),"escaped supply removed on all peers",2);
        Require(m.HealthPickups.Count==health&&m.AmmoPickups.Count==ammo,"escape creates no automatic loot");
        Require(Player(1).GrantShieldServer(Player(1).LifeVersion),"grant before round end");
        var round=m.State.Value;round.End=m.Now+.05;m.State.Value=round;yield return Wait(()=>All(4,p=>p.phase==2&&p.medicCount==0&&p.healthCount==0&&p.medicNextAt==0&&p.shield==0),"finish clears flight schedule and health kits");
        Require(m.ReturnToRoom(),"return after medical flight");yield return Wait(()=>All(4,p=>p.phase==0&&p.medicCount==0),"ready room remains free of supply drones");
    }
}
