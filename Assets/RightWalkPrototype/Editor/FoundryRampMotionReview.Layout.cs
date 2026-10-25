using System;
using System.Collections;
using UnityEngine;
using RightWalkPrototype;

public static partial class FoundryRampMotionReview
{
    static void Require(bool okay,string label)
    {if(!okay)throw new Exception(label);results.Add("PASS "+label);System.IO.File.WriteAllLines(Folder+"/progress.txt",results);}
    static IEnumerator DoubleJumpTo(float floor)
    {
        move.SetInput(0,true);yield return new WaitForSeconds(.45f);move.SetInput(0,true);
        yield return Wait(()=>move.Grounded&&Mathf.Abs(body.position.y-floor)<.25f,"landing at "+floor,5);
        Require(true,"actual double jump lands at "+floor+" x="+body.position.x);
    }
    static IEnumerator RunLayout(SideScrollNetworkSession session)
    {
        session.StartSoloPractice();yield return Wait(()=>ArenaObjectiveMatch.PracticeRoom,"host");var match=ArenaObjectiveMatch.Instance;
        match.SetLocalReady(true);yield return Wait(()=>match.CanStart,"ready");Require(match.StartRound(),"start arena");
        yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing,"playing",22);
        player=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>();move=player.GetComponent<SideScrollMapPreview>();body=player.GetComponent<Rigidbody2D>();
        var aim=player.GetComponentInChildren<RightFacingAimRig>();aim.mouseAim=false;player.GetComponentInChildren<RightFacingShotPreview>().readMouse=false;
        Require(match.Patrols.Count==10,"ten ordinary enemies retained");
        for(int i=0;i<match.Patrols.Count;i++){var e=match.Patrols[i];e.Health=0;e.RespawnAt=match.Now+600;match.Patrols[i]=e;}
        match.TurretShots.Clear();var window=match.MedicalTransit.Value;window.NextAt=match.Now+600;match.MedicalTransit.Value=window;
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();
        foreach(int side in new[]{-1,1})
        {
            // Reproduce the two jump-through routes that solid crates had sealed.
            move.SetInput(0);move.SetSpawnPosition(new Vector2(side*30,3.6f));yield return new WaitForSeconds(.4f);
            Require(move.Grounded&&move.GroundCollider.name.StartsWith("Outer lower"),"middle entry starts on real lower ramp");
            move.SetInput(-side);yield return Wait(()=>side*body.position.x<28.05f,"approach former middle crate",3);move.SetInput(0);yield return new WaitForSeconds(.25f);
            yield return DoubleJumpTo(map.floorSpacing);
            Require(Mathf.Abs(body.position.x-side*28)<.7f,"jump-through middle landing clear "+side);
            move.SetInput(0);move.SetSpawnPosition(new Vector2(side*13.5f,1.5f*map.floorSpacing+.04f));yield return new WaitForSeconds(.3f);
            Require(move.Grounded&&move.GroundCollider.name.StartsWith("Jump shortcut upper"),"upper entry starts on shortcut ledge");
            yield return DoubleJumpTo(2*map.floorSpacing);
            Require(Mathf.Abs(body.position.x-side*13.5f)<.3f,"jump-through upper deck clear "+side);
        }
        foreach(var at in map.respawnPoints)Require(map.SpawnCandidateValid(at),"respawn clear "+at);
        var points=UnityEngine.Object.FindFirstObjectByType<ArenaObjectiveLayout>().points;
        foreach(var point in points)
        {
            Require(Mathf.Abs(point.CaptureBounds.size.x-4)<.01f&&Mathf.Abs(point.CaptureBounds.size.y-2.35f)<.01f,"capture width4 height2.35 "+point.displayName);
            move.SetInput(0);move.SetSpawnPosition((Vector2)point.transform.position+new Vector2(1.8f,.04f));yield return new WaitForSeconds(.35f);
            Require(move.Grounded&&point.Contains(player),"new capture edge accepts grounded player "+point.displayName);
            move.SetSpawnPosition((Vector2)point.transform.position+new Vector2(3.5f,.04f));yield return new WaitForSeconds(.35f);
            Require(!point.Contains(player),"outside capture remains rejected "+point.displayName);
        }
        foreach(var at in System.Linq.Enumerable.Concat(map.groundEnemySpawns,map.turretSpawns))
        {
            var overlaps=Physics2D.OverlapBoxAll(at+Vector2.up*.75f,new Vector2(1.1f,1.3f),0,1);
            foreach(var c in overlaps)Require(c.isTrigger||c.GetComponentInParent<SideScrollNetworkPlayer>()!=null,"enemy body clearance "+at+" vs "+c.name);
            foreach(var point in points)if(Mathf.Abs(point.transform.position.y-at.y)<.1f)Require(Mathf.Abs(point.transform.position.x-at.x)>5,"entrance away from capture "+at);
        }
        foreach(int side in new[]{-1,1})
        {
            move.SetInput(0);move.SetSpawnPosition(new Vector2(side*5.5f,-.9f));yield return new WaitForSeconds(.4f);
            Require(move.Grounded,"lower capture approach starts on service ramp");
            move.SetInput(-side,true);yield return new WaitForSeconds(.45f);move.SetInput(-side,true);
            yield return Wait(()=>side*body.position.x<2.5f,"jump across lower capture lip",3);move.SetInput(0);
            yield return Wait(()=>move.Grounded&&body.position.y>-.2f,"land at lower capture entry",4);
            move.SetInput(-side);yield return Wait(()=>side*body.position.x<1.2f,"enter lower capture",3);move.SetInput(0);yield return new WaitForSeconds(.4f);
            Require(points[0].Contains(player),"lower capture reachable from pit "+side);
        }
        foreach(int side in new[]{-1,1})
        {
            move.SetInput(0);move.SetSpawnPosition(new Vector2(side*4.5f,2*map.floorSpacing+.04f));yield return new WaitForSeconds(.3f);
            Require(move.Grounded,"central route starts on actual upper floor");
            yield return DoubleJumpTo(FoundryUpperRouteBuilder.LowerStepHeight(map));
            move.SetInput(-side);yield return Wait(()=>side*body.position.x<3.6f,"approach inner ascent",2);move.SetInput(0);yield return new WaitForSeconds(.25f);
            move.SetInput(-side,true);yield return new WaitForSeconds(.45f);move.SetInput(-side,true);yield return Wait(()=>side*body.position.x<2.2f,"jump to raised ascent",3);move.SetInput(0);
            yield return Wait(()=>move.Grounded&&Mathf.Abs(body.position.y-FoundryUpperRouteBuilder.UpperStepHeight(map))<.25f,"land raised ascent",4);
            yield return DoubleJumpTo(3*map.floorSpacing);
        }
        // The photographed central cover also reaches the intermediate platform.
        move.SetSpawnPosition(new Vector2(0,2*map.floorSpacing+2.44f));yield return new WaitForSeconds(.3f);
        move.SetInput(1,true);yield return new WaitForSeconds(.45f);move.SetInput(1,true);yield return Wait(()=>body.position.x>2,"jump from core cover toward step",2);move.SetInput(0);
        yield return Wait(()=>move.Grounded&&Mathf.Abs(body.position.y-FoundryUpperRouteBuilder.UpperStepHeight(map))<.25f,"core cover to intermediate platform",4);
        Require(true,"core cover double jump to raised platform");
        yield return new WaitForSeconds(.2f);ScreenCapture.CaptureScreenshot(Folder+"/central-ascent.png");yield return new WaitForSeconds(.2f);
        move.SetInput(0);move.SetSpawnPosition(new Vector2(0,map.floorSpacing+.04f));yield return new WaitForSeconds(.3f);
        foreach(int side in new[]{-1,1})
        {
            var origin=new Vector2(0,map.floorSpacing+1.4f);var end=aim.TraceShot(origin,Vector2.right*side,8,out bool hit);
            Require(hit&&Mathf.Abs(Mathf.Abs(end.x)-1.7f)<.1f,"depot cover blocks actual standing-height normal trace "+side);
        }
        Require(player.GetComponentInChildren<RightFacingShotPreview>().bulletSpeed==15,"normal bullet speed retained");
        RelayArenaPrototype.Instance.overview=true;yield return new WaitForSeconds(.3f);ScreenCapture.CaptureScreenshot(Folder+"/overview.png");yield return new WaitForSeconds(.2f);RelayArenaPrototype.Instance.overview=false;
        session.DisconnectLocal();yield return new WaitForSeconds(.3f);
    }
}
