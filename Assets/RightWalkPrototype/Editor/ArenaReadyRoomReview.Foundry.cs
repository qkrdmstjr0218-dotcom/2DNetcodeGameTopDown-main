using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    static bool foundryDriving;
    [MenuItem("Tools/Right Walk Prototype/Verify Relay Foundry")]
    public static void BeginFoundry(){if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=RelayFoundryBuilder.ScenePath)throw new Exception("Open Foundry first");BeginReview(false,false,false,false,false,false,false,false,true);}
    [MenuItem("Tools/Right Walk Prototype/Capture Relay Foundry Preview")]
    public static void CaptureFoundry(){BeginFoundry();File.WriteAllText(folder+"/capture-only.txt","capture");}
    [MenuItem("Tools/Right Walk Prototype/Verify Foundry Drop Through")]
    public static void BeginFoundryDrops(){BeginFoundry();File.WriteAllText(folder+"/drop-only.txt","drop");}
    static IEnumerator CaptureFoundryViews()
    {
        foundryDriving=true;var rules=RelayArenaPrototype.Instance;var own=Player(1);var move=own.GetComponent<SideScrollMapPreview>();
        move.SetInput(0);move.SetSpawnPosition(new Vector2(0,10.84f));yield return new WaitForSecondsRealtime(2.5f);
        ScreenCapture.CaptureScreenshot(folder+"/foundry-upper-play.png");yield return new WaitForSecondsRealtime(.2f);
        rules.overview=true;yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(folder+"/foundry-overview.png");yield return new WaitForSecondsRealtime(.2f);rules.overview=false;
        var match=ArenaObjectiveMatch.Instance;var map=rules.GetComponent<ArenaMapSettings>();
        foreach(var at in map.shotgunSupplies)
        {
            SetShotgun(1,0);move.SetSpawnPosition(at-Vector2.up*.51f);move.SetInput(0);
            yield return Wait(()=>own.ShotgunRounds==3,"accessible map supply "+at,4);
        }
        yield return Wait(()=>All(4,p=>p.pickupsAvailable==0),"all peers observe three collected supplies");foundryDriving=false;
    }
    static IEnumerator FoundryClient(SideScrollNetworkPlayer own,bool drops=false)
    {
        var stack=new Stack<IEnumerator>();stack.Push(drops?CheckFoundryDrops(own):FoundryRoutes(own,false));string result="PASS";
        while(stack.Count>0)
        {
            bool more=false;object next=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){result="FAIL: "+failure;break;}if(!more){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        foundryDriving=false;own.GetComponent<SideScrollMapPreview>().SetInput(0);File.WriteAllText(folder+(drops?"/foundry-drop-client.txt":"/foundry-client.txt"),result);
    }
    static IEnumerator FoundryTraverse(SideScrollNetworkPlayer own,Vector2 start,Vector2 end,bool jump,string label)
    {
        var move=own.GetComponent<SideScrollMapPreview>();var body=own.GetComponent<Rigidbody2D>();move.SetInput(0);move.SetSpawnPosition(start+Vector2.up*.04f);
        yield return Wait(()=>move.Grounded,"settled "+label,3);float dir=Mathf.Sign(end.x-start.x);move.SetInput(dir,jump,true);
        var samples=new List<string>{"t,x,y,grounded,support"};float until=Time.realtimeSinceStartup+12;
        while(Time.realtimeSinceStartup<until&&(dir>0?own.transform.position.x<end.x:own.transform.position.x>end.x))
        {
            move.SetInput(dir,false,true);samples.Add(Time.time+","+own.transform.position.x+","+own.transform.position.y+","+move.Grounded+","+(move.GroundCollider!=null?move.GroundCollider.name:""));yield return null;
        }
        move.SetInput(0);yield return new WaitForSecondsRealtime(.4f);
        File.WriteAllLines(folder+"/route-"+own.PlayerNumber+"-"+label+".csv",samples);
        Require(Mathf.Abs(own.transform.position.x-end.x)<.9f&&Mathf.Abs(own.transform.position.y-end.y)<.22f&&move.Grounded,"traverse "+label);
        var at=own.transform.position;yield return new WaitForSecondsRealtime(.25f);Require(Vector2.Distance(at,own.transform.position)<.03f,"idle stable "+label);
    }
    static IEnumerator FoundryRoutes(SideScrollNetworkPlayer own,bool full)
    {
        foundryDriving=true;
        int[] sides=full?new[]{-1,1}:new[]{1};
        foreach(int s in sides)
        {
            yield return FoundryTraverse(own,new Vector2(s*35,0),new Vector2(s*24.4f,3.6f),true,"outer-lower-"+s);
            yield return FoundryTraverse(own,new Vector2(s*35,3.6f),new Vector2(s*24.4f,7.2f),true,"outer-upper-"+s);
            if(!full)continue;
            yield return FoundryTraverse(own,new Vector2(s*15,0),new Vector2(s*4.4f,3.6f),true,"inner-lower-"+s);
            yield return FoundryTraverse(own,new Vector2(s*16,3.6f),new Vector2(s*5.4f,7.2f),true,"inner-upper-"+s);
            yield return FoundryTraverse(own,new Vector2(s*20,7.2f),new Vector2(s*9.4f,10.8f),true,"crown-"+s);
        }
        if(full)
        {
            yield return FoundryTraverse(own,new Vector2(-21,3.6f),new Vector2(-34,3.6f),false,"middle-seam-west");
            yield return FoundryTraverse(own,new Vector2(21,3.6f),new Vector2(34,3.6f),false,"middle-seam-east");
            var move=own.GetComponent<SideScrollMapPreview>();
            foreach(int sign in new[]{-1,1})foreach(int tier in new[]{0,1})
            {
                float x=sign*(tier==0?10.5f:13.5f),floor=tier*3.6f,top=floor+1.8f;
                move.SetInput(0);move.SetSpawnPosition(new Vector2(x,floor+.04f));yield return Wait(()=>move.Grounded,"shortcut start "+sign+"/"+tier);
                move.SetInput(0,true);yield return new WaitForSecondsRealtime(.25f);move.SetInput(0,true);
                yield return Wait(()=>move.Grounded&&Mathf.Abs(own.transform.position.y-top)<.2f,"shortcut jump landing "+sign+"/"+tier,4);
            }
            move.SetInput(0);move.SetSpawnPosition(new Vector2(0,10.84f));yield return Wait(()=>move.Grounded,"top grate landing");
            Require(own.GetComponent<ArenaPlatformMotor>().TryDrop(move.GroundCollider),"top grate accepts safe drop");
            yield return Wait(()=>move.Grounded&&own.transform.position.y<9.5f,"drop lands on upper core or cover");
        }
        foundryDriving=false;own.GetComponent<SideScrollMapPreview>().SetInput(0);
    }
    static IEnumerator CheckFoundry()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4),"four peers in foundry ready room",45);
        var rules=RelayArenaPrototype.Instance;var match=ArenaObjectiveMatch.Instance;var map=rules.GetComponent<ArenaMapSettings>();
        Require(map!=null&&match.Points.Length==6,"foundry data and six uplinks loaded");
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");match.roundSeconds=300;Require(match.StartRound(),"foundry match starts");
        yield return Wait(()=>All(4,p=>p.phase==1),"all four arrivals including x31 finish countdown",22);
        yield return Wait(()=>All(4,p=>p.pickupsCount==3&&p.enemiesCount==2),"map supplies and patrols agree on four peers");
        if(File.Exists(folder+"/drop-only.txt"))
        {
            yield return CheckFoundryDrops(Player(1));Send(4,61);yield return Wait(()=>File.Exists(folder+"/foundry-drop-client.txt"),"client drop checks complete",45);Require(Read(folder+"/foundry-drop-client.txt")=="PASS","client drop return and re-entry pass");
            yield return CheckFoundryConsoles();yield break;
        }
        if(File.Exists(folder+"/capture-only.txt")){yield return CaptureFoundryViews();yield break;}
        var starts=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>().playerSpawns;
        for(int i=1;i<=4;i++)Require(Vector2.Distance(P(i).position,starts[i-1].position)<.2f,"combat spawn "+i);
        // Remove patrol interference only in this opt-in traversal fixture.
        for(int i=0;i<match.Patrols.Count;i++){var enemy=match.Patrols[i];enemy.Health=0;enemy.RespawnAt=match.Now+300;match.Patrols[i]=enemy;}
        rules.overview=true;yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(folder+"/foundry-overview.png");yield return new WaitForSecondsRealtime(.1f);rules.overview=false;
        var host=Player(1);yield return FoundryRoutes(host,true);yield return CheckFoundryDrops(host);Send(4,60);yield return Wait(()=>File.Exists(folder+"/foundry-client.txt"),"client routes completed",55);Require(Read(folder+"/foundry-client.txt")=="PASS","client outer routes reach upper tier");
        foundryDriving=true;var move=host.GetComponent<SideScrollMapPreview>();
        foreach(var point in match.Points)
        {move.SetSpawnPosition(point.transform.position+Vector3.up*.04f);move.SetInput(0);yield return Wait(()=>move.Grounded&&point.Contains(host),"feet score in "+point.displayName,4);}
        var state=match.State.Value;state.Point=3;state.Switch=match.Now+30;state.Warning=state.Switch-5;match.State.Value=state;move.SetSpawnPosition(new Vector2(0,10.84f));
        float score=match.ScoreFor(host.OwnerClientId);yield return Wait(()=>match.ScoreFor(host.OwnerClientId)>score+.2f,"top objective actually awards score",4);
        yield return new WaitForSecondsRealtime(.2f);ScreenCapture.CaptureScreenshot(folder+"/foundry-upper-play.png");yield return new WaitForSecondsRealtime(.2f);
        Require(rules.arenaCamera.transform.position.y>9&&Mathf.Abs(rules.arenaCamera.orthographicSize-4.8f)<.01f,"camera follows top tier without shrinking character");
        host.ReceiveServerDamage(100,host.LifeVersion);yield return Wait(()=>host.IsDead,"foundry player death");yield return Wait(()=>!host.IsDead&&host.HasCurrentLifePose&&Vector2.Distance(host.transform.position,starts[0].position)<.3f,"automatic respawn returns to foundry spawn",10);
        move.SetSpawnPosition(new Vector2(26,-2.6f));yield return Wait(()=>host.IsDead,"bridge pit applies fall death",3);
        state=match.State.Value;state.End=match.Now+.05;match.State.Value=state;yield return Wait(()=>All(4,p=>p.phase==2),"all peers receive foundry results");Require(match.ReturnToRoom(),"foundry returns to ready room");
        yield return Wait(()=>All(4,p=>p.phase==0&&p.position.x>89&&p.hp==100&&p.mask==0),"return restores ready room and resources");foundryDriving=false;
    }
}
