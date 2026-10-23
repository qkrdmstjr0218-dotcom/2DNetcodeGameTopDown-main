using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Foundry Entrances")]
    public static void BeginEntrances(){BeginFoundry();File.WriteAllText(folder+"/entrances.txt","entrances");}
    static void ObserveEntrances(Peer peer,ArenaObjectiveMatch match)
    {
        var sceneMap=Object.FindFirstObjectByType<ArenaMapSettings>();peer.groundEntrances=sceneMap.groundEnemySpawns.Length;peer.turretEntrances=sceneMap.turretSpawns.Length;
        foreach(var e in match.Patrols)if(!e.Medical&&e.Health>0)
        {
            if(match.EnemyEntering(e))peer.entryCount++;
            foreach(var view in Object.FindObjectsByType<ArenaPatrolEnemy>(FindObjectsSortMode.None))if(view.Id==e.Id)
            {
                if(match.EnemyEntering(e)&&view.GetComponent<Collider2D>().enabled)peer.entryShapes++;
                if(view.transform.Find("Incoming enemy warning").gameObject.activeSelf)peer.entryWarnings++;
            }
        }
    }
    static ArenaObjectiveMatch.PatrolState EnemyStateById(ArenaObjectiveMatch m,int id)
    {foreach(var e in m.Patrols)if(e.Id==id)return e;throw new System.Exception("Missing enemy "+id);}
    static IEnumerator BalconyRoutes(SideScrollNetworkPlayer own)
    {
        foundryDriving=true;
        // Like existing dotted ramps, choose the uphill route by jumping onto it.
        foreach(int side in new[]{-1,1})
        {
            yield return FoundryTraverse(own,new Vector2(side*24.7f,7.2f),new Vector2(side*33,10.8f),true,"balcony-up-"+side);
            yield return FoundryTraverse(own,new Vector2(side*33,10.8f),new Vector2(side*24.7f,7.2f),false,"balcony-down-"+side);
        }
        foundryDriving=false;own.GetComponent<SideScrollMapPreview>().SetInput(0);
    }
    static IEnumerator BalconyClient(SideScrollNetworkPlayer own)
    {
        var routine=BalconyRoutes(own);var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(routine);string result="PASS";
        while(stack.Count>0)
        {
            object item=null;bool next=false;try{next=stack.Peek().MoveNext();if(next)item=stack.Peek().Current;}catch(System.Exception e){result="FAIL "+e.Message;break;}
            if(!next){stack.Pop();continue;}if(item is IEnumerator child)stack.Push(child);else yield return item;
        }
        foundryDriving=false;own.GetComponent<SideScrollMapPreview>().SetInput(0);File.WriteAllText(folder+"/balcony-client.txt",result);
    }
    static IEnumerator CheckEntrances()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4),"four peers waiting",45);
        var m=ArenaObjectiveMatch.Instance;var map=Object.FindFirstObjectByType<ArenaMapSettings>();
        Require(map.enemyEntrances&&map.groundEnemySpawns.Length==4&&map.droneSpawns.Length==2&&map.turretSpawns.Length==4,"dedicated enemy entrance data");
        Require(All(4,p=>p.groundEntrances==4&&p.turretEntrances==4),"all peers load expanded entrance scene data");
        foreach(var at in map.respawnPoints)Require(map.SpawnCandidateValid(at),"valid spawn with balcony "+at);
        Require(map.transform.Find(FoundrySpawnBuilder.RootName)==null,"no route blocking spawn covers");
        if(File.Exists(folder+"/expanded-combat.txt"))
        {
            yield return CheckAmmoPreselection();
            Send(4,113);yield return Wait(()=>File.Exists(folder+"/special-cycle-client.txt"),"participant special-only Q/E review",8);
            Require(Read(folder+"/special-cycle-client.txt")=="PASS","participant Q/E and normal release state pass");
        }
        int enemyTotal=map.groundEnemySpawns.Length+map.droneSpawns.Length+map.turretSpawns.Length;
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");m.roundSeconds=180;Require(m.StartRound(),"start entrances round");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.entryCount==enemyTotal&&p.entryWarnings==enemyTotal&&p.entryShapes==0),"all peers see configured warnings with disabled shot colliders",24);
        foreach(var e in m.Patrols){int hp=e.Health;m.DamagePatrolServer(e.Id,e.Generation,100);Require(EnemyStateById(m,e.Id).Health==hp,"pending enemy ignores damage "+e.Id);}
        RelayArenaPrototype.Instance.overview=true;ScreenCapture.CaptureScreenshot(folder+"/entry-warning.png");yield return new WaitForSecondsRealtime(.2f);
        yield return Wait(()=>All(4,p=>p.entryCount==0&&p.entryWarnings==0),"warning finishes on every peer",4);
        yield return Wait(()=>All(4,p=>p.grounded),"all player starts grounded");
        for(int i=1;i<=4;i++)Require(Vector2.Distance(P(i).position,FoundrySpawnBuilder.Starts[i-1])<.25f,"player starts at assigned arrival "+i);
        for(int i=0;i<m.Patrols.Count;i++)
        {
            var e=m.Patrols[i];Require(e.Health>0,"enemy activated "+e.Id);
            var homes=e.Flying?map.droneSpawns:e.Turret?map.turretSpawns:map.groundEnemySpawns;float nearest=float.MaxValue;foreach(var home in homes)nearest=Mathf.Min(nearest,Vector2.Distance(m.PatrolPosition(e),home));
            Require(nearest<1,"enemy emerged at dedicated entrance "+e.Id);
        }
        yield return new WaitForSecondsRealtime(.2f);ScreenCapture.CaptureScreenshot(folder+"/entry-active.png");RelayArenaPrototype.Instance.overview=false;
        // Normal ten-second respawn, including all three enemy kinds.
        int[] killed={0,map.groundEnemySpawns.Length,map.groundEnemySpawns.Length+map.droneSpawns.Length};int[] generations=new int[3];
        for(int n=0;n<killed.Length;n++){var e=m.Patrols[killed[n]];generations[n]=e.Generation;m.DamagePatrolServer(e.Id,e.Generation,100);Require(m.Patrols[killed[n]].Health==0,"active enemy can be destroyed "+e.Id);}
        yield return new WaitForSecondsRealtime(.5f);Require(m.Patrols[0].Health==0,"no instant reappearance");
        yield return Wait(()=>m.Patrols[0].Generation>generations[0]&&All(4,p=>p.entryCount==3&&p.entryWarnings==3&&p.entryShapes==0),"ground drone turret respawn through synchronized warnings",13);
        for(int n=0;n<killed.Length;n++)
        {
            var e=m.Patrols[killed[n]];Require(e.Generation==generations[n]+1&&e.FireAt==0&&e.Velocity==Vector2.zero,"entry resets attack and motion "+e.Id);
            m.DamagePatrolServer(e.Id,generations[n],100);m.DamagePatrolServer(e.Id,e.Generation,100);Require(m.Patrols[killed[n]].Health>0,"old generation and warning damage rejected "+e.Id);
        }
        yield return Wait(()=>All(4,p=>p.entryCount==0&&p.entryWarnings==0),"respawn warnings finish",4);
        // Isolate locomotion from combat while driving both actual owners.
        for(int i=0;i<m.Patrols.Count;i++){var e=m.Patrols[i];e.Health=0;e.RespawnAt=m.Now+300;m.Patrols[i]=e;}
        var med=m.MedicalTransit.Value;med.NextAt=m.Now+300;m.MedicalTransit.Value=med;
        if(File.Exists(folder+"/expanded-combat.txt"))
        {
            yield return SolidSlopeRest(Player(1));Send(4,114);
            yield return Wait(()=>File.Exists(folder+"/solid-rest-client.txt"),"participant solid slope review",35);
            Require(Read(folder+"/solid-rest-client.txt")=="PASS","participant stops remain stable on solid slopes");
        }
        yield return BalconyRoutes(Player(1));Send(4,112);
        yield return Wait(()=>File.Exists(folder+"/balcony-client.txt"),"participant climbs and descends both balconies",40);
        Require(Read(folder+"/balcony-client.txt")=="PASS","participant physical balcony traversal passes");
        RelayArenaPrototype.Instance.overview=true;ScreenCapture.CaptureScreenshot(folder+"/balcony-layout.png");yield return new WaitForSecondsRealtime(.2f);
        var state=m.State.Value;state.End=m.Now+.05;m.State.Value=state;yield return Wait(()=>All(4,p=>p.phase==2),"round ends");
        Require(m.ReturnToRoom(),"return ready room");yield return Wait(()=>All(4,p=>p.phase==0&&p.enemiesCount==0&&p.entryWarnings==0),"entries cleaned after return");
    }
}
