using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;

[InitializeOnLoad]
public static class FoundryFloorSpacingReview
{
    const string Folder="LocalTools/Analysis/FloorSpacing",Key="FoundryFloorSpacingReview";
    static readonly List<string> passed=new();
    static SideScrollNetworkPlayer player;static SideScrollMapPreview move;
    static FoundryFloorSpacingReview(){EditorApplication.update+=Tick;}
    public static void Begin(){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||Time.time<.5f)return;
        var s=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();if(s==null)return;
        SessionState.SetBool(Key,false);passed.Clear();s.StartCoroutine(Guard(Run(s)));
    }
    static IEnumerator Guard(IEnumerator run)
    {
        var stack=new Stack<IEnumerator>();stack.Push(run);
        while(stack.Count>0)
        {
            bool next=false;object value=null;Exception error=null;
            try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){error=e;}
            if(error!=null){File.WriteAllText(Folder+"/result.txt","FAIL: "+error+"\n"+string.Join("\n",passed));yield break;}
            if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;
        }
        File.WriteAllText(Folder+"/result.txt","PASS\n"+string.Join("\n",passed));
    }
    static void Check(bool okay,string label){if(!okay)throw new Exception(label);passed.Add(label);File.WriteAllLines(Folder+"/progress.txt",passed);}
    static IEnumerator Wait(Func<bool> test,string label,float seconds=8)
    {float end=Time.time+seconds;while(!test()&&Time.time<end)yield return null;Check(test(),label);}
    static IEnumerator Route(Vector2 start,Vector2 end)
    {
        move.SetInput(0);move.SetSpawnPosition(start+Vector2.up*.04f);
        yield return Wait(()=>move.Grounded,"route start "+start,3);
        float direction=Mathf.Sign(end.x-start.x);move.SetInput(direction,true,true);
        yield return Wait(()=>direction*(player.transform.position.x-end.x)>=0,"ramp traversed "+start+" -> "+end,10);
        move.SetInput(0);
        yield return Wait(()=>move.Grounded&&Mathf.Abs(player.transform.position.y-end.y)<.3f,"ramp destination grounded "+end,3);
    }
    static IEnumerator Run(SideScrollNetworkSession session)
    {
        session.StartSoloPractice();yield return Wait(()=>ArenaObjectiveMatch.PracticeRoom,"offline host waiting");
        var match=ArenaObjectiveMatch.Instance;match.SetLocalReady(true);yield return Wait(()=>match.CanStart,"ready");
        Check(match.StartRound(),"start actual arena");yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing,"arena playing",22);
        player=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>();move=player.GetComponent<SideScrollMapPreview>();move.SetInput(0);
        player.GetComponentInChildren<RightFacingAimRig>().mouseAim=false;player.GetComponentInChildren<RightFacingShotPreview>().readMouse=false;
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();
        float level=map.floorSpacing;Check(Mathf.Abs(level-6.8f)<.001f,"floor spacing 6.8");
        // Geometry fixtures suppress hostile attacks so knockback cannot invalidate a route.
        for(int i=0;i<match.Patrols.Count;i++){var e=match.Patrols[i];e.Health=0;e.RespawnAt=match.Now+300;match.Patrols[i]=e;}
        match.TurretShots.Clear();var window=match.MedicalTransit.Value;window.NextAt=match.Now+300;match.MedicalTransit.Value=window;
        foreach(var at in map.respawnPoints)Check(map.SpawnCandidateValid(at),"spawn safe "+at);
        // Position fixtures isolate each existing route; travel uses the real motor.
        foreach(int side in new[]{-1,1})
        {
            yield return Route(new Vector2(side*35,0),new Vector2(side*25,level));
            yield return Route(new Vector2(side*35,level),new Vector2(side*25,2*level));
            yield return Route(new Vector2(side*(31-level/.8f),2*level),new Vector2(side*31.4f,3*level));
        }
        move.SetSpawnPosition(new Vector2(1,3*level+.04f));yield return Wait(()=>move.Grounded,"crown grate grounded");
        player.GetComponent<CrouchTrial>().SetRequested(true);yield return new WaitForSeconds(.2f);move.SetInput(0,true);
        yield return new WaitForSeconds(.15f);player.GetComponent<CrouchTrial>().SetRequested(false);
        yield return Wait(()=>move.Grounded&&Mathf.Abs(player.transform.position.y-2*level)<.25f,"drop through new 6.8 floor gap");
        move.SetSpawnPosition(new Vector2(0,.04f));yield return new WaitForSeconds(.2f);yield return Wait(()=>move.Grounded&&Mathf.Abs(player.transform.position.y)<.2f,"jump test on central ground");
        var shape=player.GetComponent<BoxCollider2D>();float peak=shape.bounds.max.y;move.SetInput(0,true);float until=Time.time+.45f;
        while(Time.time<until){peak=Mathf.Max(peak,shape.bounds.max.y);yield return null;}
        move.SetInput(0,true);yield return new WaitForFixedUpdate();Check(move.JumpCount==2,"actual second jump accepted");until=Time.time+1.7f;
        while(Time.time<until){peak=Mathf.Max(peak,shape.bounds.max.y);yield return null;}
        Check(peak>4.52f&&peak<level-.38f,"double jump head clears new ceiling by at least .1: "+peak);
        RelayArenaPrototype.Instance.overview=true;yield return new WaitForSeconds(.3f);ScreenCapture.CaptureScreenshot(Folder+"/overview.png");yield return new WaitForSeconds(.2f);RelayArenaPrototype.Instance.overview=false;
        Check(map.ammoSupplies.Length==3&&Mathf.Abs(map.ammoSupplies[0].position.y-(level+.42f))<.01f,"supplies moved with middle floor");
        Check(Mathf.Abs(map.groundEnemySpawns[1].y-level)<.01f&&Mathf.Abs(map.turretSpawns[0].y-2*level)<.01f&&Mathf.Abs(map.droneSpawns[0].y-(3*level+2.5f))<.01f,"enemy spawn heights aligned");
        // Synchronous server fixtures isolate score ownership and lethal transitions.
        var score=match.Scores[0];int drops=match.HealthPickups.Count;
        var drone=new ArenaObjectiveMatch.PatrolState{Id=99001,Generation=1,Health=8,Medical=true,Roaming=true,Position=new Vector2(2,2)};
        match.Patrols.Add(drone);
        match.DamagePatrolServer(drone.Id,2,8,player);Check(match.Scores[0].DronePoints==score.DronePoints,"stale generation earns nothing");
        match.DamagePatrolServer(drone.Id,1,4,player);Check(match.Scores[0].DronePoints==score.DronePoints,"nonlethal hit earns nothing");
        match.DamagePatrolServer(drone.Id,1,4,player);var after=match.Scores[0];
        Check(after.DronePoints==score.DronePoints+5&&after.Kills==score.Kills&&after.KillPoints==score.KillPoints&&after.Total==score.Total+5,"drone killer +5 without player kill increment");
        match.DamagePatrolServer(drone.Id,1,8,player);Check(match.Scores[0].DronePoints==after.DronePoints&&match.HealthPickups.Count==drops+1,"repeat lethal damage no duplicate score or shield drop");
        drone.Id++;match.Patrols.Add(drone);match.DamagePatrolServer(drone.Id,1,8);Check(match.Scores[0].DronePoints==after.DronePoints,"unattributed kill earns nothing");
        drone.Id++;drone.Medical=false;match.Patrols.Add(drone);match.DamagePatrolServer(drone.Id,1,8,player);Check(match.Scores[0].DronePoints==after.DronePoints,"ordinary enemy earns no drone bounty");
        // Hold a medical flight in place only for two real rifle shots.
        drone.Id++;drone.Medical=true;drone.Position=new Vector2(8,2);drone.Speed=0;match.Patrols.Add(drone);
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var field=typeof(ArenaObjectiveMatch).GetField("activeMedicalTrip",flags);var trip=Activator.CreateInstance(field.FieldType,true);
        field.FieldType.GetField("Id").SetValue(trip,drone.Id);field.FieldType.GetField("Began").SetValue(trip,match.Now);field.FieldType.GetField("Tick").SetValue(trip,match.Now);
        ((List<Vector2>)field.FieldType.GetField("Points").GetValue(trip)).Add(new Vector2(9,2));field.SetValue(match,trip);
        move.SetSpawnPosition(new Vector2(0,.04f));yield return new WaitForSeconds(.2f);yield return Wait(()=>move.Grounded&&Mathf.Abs(player.transform.position.y)<.2f,"rifle shooter grounded");
        var aim=player.GetComponentInChildren<RightFacingAimRig>();var shots=player.GetComponentInChildren<RightFacingShotPreview>();
        player.SetSpecialHeld(true);player.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Rifle);aim.SetAimTarget(drone.Position+Vector2.up*.48f);
        yield return Wait(()=>player.FireWaitRemaining<=0,"rifle ready");yield return null;shots.Fire();yield return new WaitForSeconds(.5f);
        Check(match.Scores[0].DronePoints==after.DronePoints,"first actual rifle hit no bounty");
        yield return Wait(()=>player.FireWaitRemaining<=0,"second rifle ready");shots.Fire();
        yield return Wait(()=>match.Scores[0].DronePoints==after.DronePoints+5,"second actual rifle hit awards killer +5",3);
        session.DisconnectLocal();yield return new WaitForSeconds(.5f);
    }
}
