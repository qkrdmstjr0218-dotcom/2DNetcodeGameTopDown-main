using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Turrets")]
    public static void BeginTurrets(){BeginFoundry();File.WriteAllText(folder+"/turrets.txt","turrets");}
    static IEnumerator TurretClient(SideScrollNetworkPlayer own,int action,bool client)
    {
        if(action==70||action==73||action==74)
        {
            bool target=own.PlayerNumber==(client?2:1);
            own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(target?new Vector2(action==74?-19+ArenaObjectiveMatch.TurretRange+1:-21,action==73?3.6f:7.2f):new Vector2(31+own.PlayerNumber*.5f,0));
            yield break;
        }
        var aim=own.GetComponentInChildren<RightFacingAimRig>();aim.SetAimTarget(new Vector2(-19,7.7f));
        if(action==71)
        {own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.InstantShotgun);own.SetSpecialHeld(true);yield return new WaitForSecondsRealtime(.25f);SampleFireButtons(own,true,true);}
        if(action==72){own.SetSpecialHeld(false);yield return new WaitForSecondsRealtime(.25f);SampleFireButtons(own,false,true);}
    }
    static int ConfiguredTurretCount=>Object.FindFirstObjectByType<ArenaMapSettings>()?.turretSpawns?.Length??0;
    static int TurretIndex(){for(int i=0;i<ArenaObjectiveMatch.Instance.Patrols.Count;i++)if(ArenaObjectiveMatch.Instance.Patrols[i].Turret)return i;return -1;}
    static IEnumerator CheckTurrets()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.turretCount==0),"four peers waiting without turrets",45);
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");var m=ArenaObjectiveMatch.Instance;m.roundSeconds=180;Require(m.StartRound(),"start turret round");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.turretCount==ConfiguredTurretCount&&p.turretHealth==4),"configured turrets synchronized to four peers");
        int index=TurretIndex();var turret=m.Patrols[index];int id=turret.Id,generation=turret.Generation;
        // Isolate the west turret so patrol contacts cannot masquerade as projectile damage.
        DropChance(m,0);for(int i=0;i<m.Patrols.Count;i++)if(i!=index){var e=m.Patrols[i];e.Health=0;e.RespawnAt=m.Now+120;m.Patrols[i]=e;}
        Send(0,70);yield return Wait(()=>Mathf.Abs(P(1).position.x+21)<.15f&&P(1).grounded&&P(2).position.x>30,"host placed on turret floor");
        yield return Wait(()=>All(4,p=>p.turretFireAt>0),"warning deadline synchronized");
        int hp=Player(1).Health;Require(m.Patrols[index].FireAt-m.Now>0&&hp==100,"warning precedes damage");ScreenCapture.CaptureScreenshot(folder+"/turret-warning.png");
        yield return Wait(()=>P(1).hp==64,"host receives three-shot burst",4);Require(Player(1).Health==hp-ArenaObjectiveMatch.TurretDamage*ArenaObjectiveMatch.TurretBurstCount,"burst contains exactly three damaging projectiles");
        Send(0,70,true);yield return Wait(()=>P(2).position.x< -20&&P(2).grounded&&P(1).position.x>30,"client becomes sole target");
        yield return Wait(()=>P(2).hp==64,"client receives three-shot burst",5);
        // A collider inserted during the warning must stop the subsequent projectile.
        turret=m.Patrols[index];turret.FireAt=0;turret.BurstLeft=0;turret.NextAttack=m.Now+.2;m.Patrols[index]=turret;
        yield return Wait(()=>m.Patrols[index].FireAt>m.Now+.1,"next burst warning begins");
        var wall=new GameObject("Turret test cover");wall.transform.position=new Vector3(-20,8,0);wall.AddComponent<BoxCollider2D>().size=new Vector2(.3f,2);Physics2D.SyncTransforms();
        yield return new WaitForSecondsRealtime(1.1f);Require(Player(2).Health==64,"solid cover blocks projectile");
        yield return new WaitForSecondsRealtime(1.8f);Require(m.Patrols[index].FireAt==0&&Player(2).Health==64,"cover prevents reacquisition");Object.Destroy(wall);yield return null;
        yield return Wait(()=>m.Patrols[index].FireAt>m.Now+.2,"reacquires after cover removal");Send(0,73,true);
        yield return Wait(()=>P(2).position.y<6,"target leaves firing envelope before burst");yield return new WaitForSecondsRealtime(1.2f);Require(Player(2).Health==64,"leaving vertical envelope cancels burst");
        turret=m.Patrols[index];turret.FireAt=0;turret.BurstLeft=0;turret.NextAttack=m.Now+60;m.Patrols[index]=turret;
        Send(0,70);yield return Wait(()=>P(1).position.x< -20&&P(1).grounded,"host ready for tracking check");
        turret=m.Patrols[index];turret.FireAt=0;turret.BurstLeft=0;turret.NextAttack=m.Now+.1;m.Patrols[index]=turret;
        var counter=typeof(ArenaObjectiveMatch).GetField("nextTurretShot",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        int shots=(int)counter.GetValue(m);yield return Wait(()=>(int)counter.GetValue(m)==shots+1,"first tracking shot fires");var firstAim=m.Patrols[index].Aim;
        Player(1).GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(-21,8.9f));
        yield return Wait(()=>(int)counter.GetValue(m)==shots+2,"second tracking shot fires");
        Require(Vector2.Angle(firstAim,m.Patrols[index].Aim)>3,"second shot updates direction toward moving target");
        yield return Wait(()=>m.Patrols[index].BurstLeft==0,"tracking burst completes");
        turret=m.Patrols[index];turret.FireAt=0;turret.BurstLeft=0;turret.NextAttack=m.Now+60;m.Patrols[index]=turret;
        yield return new WaitForSecondsRealtime(1);Require(m.TurretShots.Count==0,"short-range projectiles expire");
        Send(0,74);yield return Wait(()=>Mathf.Abs(P(1).position.x-(-19+ArenaObjectiveMatch.TurretRange+1))<.15f&&P(1).grounded,"target beyond detection range");
        turret=m.Patrols[index];turret.NextAttack=m.Now;m.Patrols[index]=turret;shots=(int)counter.GetValue(m);
        yield return new WaitForSecondsRealtime(1);Require((int)counter.GetValue(m)==shots&&m.Patrols[index].FireAt==0,"out-of-range target is not acquired");
        Require(ArenaObjectiveMatch.TurretBulletSpeed*ArenaObjectiveMatch.TurretBulletLife+.6f<=ArenaObjectiveMatch.TurretRange+.001f,"projectile travel is capped at shortened range");
        turret=m.Patrols[index];turret.NextAttack=m.Now+60;m.Patrols[index]=turret;
        Send(0,70);yield return Wait(()=>P(1).position.x< -20&&P(1).grounded,"host returns to fire at turret");Send(1,72);
        yield return Wait(()=>All(4,p=>p.turretHealth==3),"normal bullet damages turret through existing enemy path",4);
        Send(0,70,true);yield return Wait(()=>P(2).position.x< -20&&P(2).grounded,"client in firing position");
        DropChance(m,1);int pickups=m.AmmoPickups.Count;Send(2,71);
        yield return Wait(()=>All(4,p=>p.turretHealth==0),"client shotgun destroys turret on all peers",4);
        Require(m.AmmoPickups.Count==pickups+1&&m.AmmoPickups[m.AmmoPickups.Count-1].Amount==1,"turret drops one shotgun round on shared probability path");
        m.DamagePatrolServer(id,generation,100);Require(m.AmmoPickups.Count==pickups+1,"duplicate kill cannot duplicate drop");
        foreach(var view in m.GetComponentsInChildren<ArenaPatrolEnemy>(true))if(view.Id==id)Require(!view.gameObject.activeSelf&&!view.GetComponent<Collider2D>().enabled,"destroyed turret hides and disables its shot collider");
        yield return Wait(()=>All(4,p=>p.turretHealth==4&&p.turretGeneration>generation),"turret respawns after ten seconds with new generation",12);
        m.DamagePatrolServer(id,generation,100);Require(m.Patrols[index].Health==4,"old generation damage rejected");
        var state=m.State.Value;state.End=m.Now+.05;m.State.Value=state;
        yield return Wait(()=>All(4,p=>p.phase==2&&p.turretCount==0&&p.turretShots==0),"finish removes turrets and bullets");
        Require(m.ReturnToRoom(),"return after turret round");yield return Wait(()=>All(4,p=>p.phase==0&&p.turretCount==0),"ready room remains free of turrets");
    }
}
