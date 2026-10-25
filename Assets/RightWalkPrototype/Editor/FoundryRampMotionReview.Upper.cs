using System;
using System.Collections;
using UnityEngine;
using RightWalkPrototype;

public static partial class FoundryRampMotionReview
{
    static IEnumerator SetBridgeCycle(ArenaRetractingBridge bridge,ArenaObjectiveMatch match,float cycle)
    {bridge.phaseOffset=(float)((cycle-match.Now%14+14)%14);yield return null;yield return new WaitForSeconds(.05f);}
    static IEnumerator WalkBridge(ArenaRetractingBridge bridge,bool right,bool sprint)
    {
        float direction=right?1:-1,start=right?bridge.left-.45f:bridge.right+.45f,goal=right?bridge.right+.4f:bridge.left-.4f;
        typeof(SideScrollNetworkPlayer).GetMethod("ResetSprintEnergy",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(player,null);
        move.SetInput(0);move.SetSpawnPosition(new Vector2(start,bridge.floor+.04f));yield return new WaitForSeconds(.3f);
        Require(move.Grounded,"bridge approach fixed deck");move.SetInput(direction,false,sprint);
        float stall=0,maxStall=0,end=Time.time+4;
        while(direction*(body.position.x-goal)<0&&Time.time<end)
        {
            yield return new WaitForFixedUpdate();
            if(Mathf.Min(Mathf.Abs(body.position.x-bridge.left),Mathf.Abs(body.position.x-bridge.right))<.3f)
            {stall=Mathf.Abs(body.linearVelocity.x)<.4f?stall+Time.fixedDeltaTime:0;maxStall=Mathf.Max(stall,maxStall);}
        }
        move.SetInput(0);Require(direction*(body.position.x-goal)>=0&&maxStall<.12f,"closed bridge crossed without stall "+bridge.name+" right="+right+" run="+sprint);
        yield return new WaitForSeconds(.4f);Require(move.Grounded&&Mathf.Abs(body.position.y-bridge.floor)<.6f,"bridge destination or connected ramp settled");
    }
    static IEnumerator RunUpper(SideScrollNetworkSession session)
    {
        session.StartSoloPractice();yield return Wait(()=>ArenaObjectiveMatch.PracticeRoom,"host");var match=ArenaObjectiveMatch.Instance;
        match.SetLocalReady(true);yield return Wait(()=>match.CanStart,"ready");Require(match.StartRound(),"start arena");
        yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing,"playing",22);
        player=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>();move=player.GetComponent<SideScrollMapPreview>();body=player.GetComponent<Rigidbody2D>();
        player.GetComponentInChildren<RightFacingAimRig>().mouseAim=false;player.GetComponentInChildren<RightFacingShotPreview>().readMouse=false;
        for(int i=0;i<match.Patrols.Count;i++){var e=match.Patrols[i];e.Health=0;e.RespawnAt=match.Now+600;match.Patrols[i]=e;}
        match.TurretShots.Clear();var window=match.MedicalTransit.Value;window.NextAt=match.Now+600;match.MedicalTransit.Value=window;
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();float upper=2*map.floorSpacing,crown=3*map.floorSpacing;
        float low=FoundryUpperRouteBuilder.LowerStepHeight(map),high=FoundryUpperRouteBuilder.UpperStepHeight(map);
        Require(Mathf.Abs((low-upper)-(high-low))<.001f&&Mathf.Abs((high-low)-(crown-high))<.001f,"central height divided into equal thirds");
        foreach(int side in new[]{-1,1})
        {
            move.SetInput(0);move.SetSpawnPosition(new Vector2(side*6.6f,upper+.04f));yield return new WaitForSeconds(.3f);
            move.SetInput(-side);yield return Wait(()=>side*body.position.x<3.5f,"walk under third-floor step");move.SetInput(0);yield return new WaitForSeconds(.2f);
            Require(move.Grounded&&Mathf.Abs(body.position.y-upper)<.25f,"new step does not block walking");
            move.SetSpawnPosition(new Vector2(side*6.6f,upper+.04f));yield return new WaitForSeconds(.3f);
            move.SetInput(-side,true);yield return new WaitForSeconds(.45f);move.SetInput(-side,true);
            yield return Wait(()=>side*body.position.x<4.55f,"jump toward raised third-floor step",3);move.SetInput(0);
            yield return Wait(()=>move.Grounded&&Mathf.Abs(body.position.y-FoundryUpperRouteBuilder.LowerStepHeight(map))<.25f,"land third-floor step",4);Require(true,"double jump reaches first third-height step "+side);
            move.SetInput(-side);yield return Wait(()=>side*body.position.x<3.6f,"walk toward inner step edge",2);move.SetInput(0);yield return new WaitForSeconds(.25f);
            move.SetInput(-side,true);yield return new WaitForSeconds(.45f);move.SetInput(-side,true);
            yield return Wait(()=>side*body.position.x<2.2f,"cross to central ascent step",3);move.SetInput(0);
            yield return Wait(()=>move.Grounded&&Mathf.Abs(body.position.y-FoundryUpperRouteBuilder.UpperStepHeight(map))<.25f,"land central ascent step",4);Require(true,"raised lower step connects to upper third-height step "+side);
            yield return DoubleJumpTo(crown);
        }
        var bridges=map.transform.Find(FoundryUpperRouteBuilder.RootName).GetComponentsInChildren<ArenaRetractingBridge>();
        Require(bridges.Length==2,"two upper bridges");
        foreach(var bridge in bridges)
        {
            Require(!bridge.lethalPit&&Mathf.Abs(bridge.floor-crown)<.01f&&bridge.right-bridge.left==6,"six-unit elevated nonlethal gap");float originalOffset=bridge.phaseOffset;
            foreach(bool right in new[]{true,false})foreach(bool sprint in new[]{false,true})
            {yield return SetBridgeCycle(bridge,match,1);yield return WalkBridge(bridge,right,sprint);}
            yield return SetBridgeCycle(bridge,match,8.2f);Require(!bridge.Retracted&&bridge.status.text.Contains("WARNING"),"warning before retraction");
            yield return SetBridgeCycle(bridge,match,11.1f);
            Require(bridge.westLeaf.localScale.x<.001f&&!bridge.westLeaf.GetComponent<BoxCollider2D>().enabled&&!bridge.eastLeaf.GetComponent<BoxCollider2D>().enabled,"fully open visuals and collision agree");
            int health=player.Health;move.SetInput(0);move.SetSpawnPosition(new Vector2(Mathf.Sign(bridge.left+bridge.right)*7.05f,crown+.04f));
            yield return Wait(()=>move.Grounded&&Mathf.Abs(body.position.y-upper)<.25f,"fall onto third floor",3);
            Require(player.Health==health&&!player.IsDead&&!bridge.UnsafeLanding(body.position),"upper fall returns safely "+bridge.name);
            yield return SetBridgeCycle(bridge,match,11.1f);
            move.SetSpawnPosition(new Vector2(bridge.left-.45f,crown+.04f));yield return new WaitForSeconds(.3f);move.SetInput(1,true);
            yield return new WaitForSeconds(1.1f);move.SetInput(0);
            Require(body.position.x<bridge.right&&body.position.y<crown-.5f,"single walking jump no longer bypasses open bridge "+bridge.name);
            yield return Wait(()=>move.Grounded&&body.position.y<crown-.5f,"land on lower return route",4);Require(player.Health==health,"failed gap jump returns without damage");
            yield return SetBridgeCycle(bridge,match,13.6f);Require(bridge.status.text=="CLOSING"&&bridge.westLeaf.GetComponent<BoxCollider2D>().enabled,"closing restores collision with visible leaf");
            bridge.phaseOffset=originalOffset;
        }
        foreach(var at in map.respawnPoints)Require(map.SpawnCandidateValid(at),"fixed respawn remains valid "+at);
        var ground=map.transform.Find(FoundryWestBridgeBuilder.RootName).GetComponent<ArenaRetractingBridge>();
        Require(ground.lethalPit&&ground.floor==0&&ground.UnsafeLanding(new Vector2(-22,-2.5f)),"legacy ground pit policy retained");
        Require(player.GetComponentInChildren<RightFacingShotPreview>().bulletSpeed==15,"normal bullet settings retained");
        RelayArenaPrototype.Instance.overview=true;yield return new WaitForSeconds(.3f);ScreenCapture.CaptureScreenshot(Folder+"/overview.png");yield return new WaitForSeconds(.2f);RelayArenaPrototype.Instance.overview=false;
        session.DisconnectLocal();yield return new WaitForSeconds(.3f);
    }
}
