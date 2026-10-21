using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;

public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Foundry West Bridge")]
    public static void BeginWestBridge(){BeginFoundry();File.WriteAllText(folder+"/west-bridge.txt","west");}
    static IEnumerator CheckWestBridgeRound()
    {
        yield return Wait(()=>All(2,p=>p.connected&&p.phase==0&&p.roster==2),"two ready-room peers",35);
        Send(0,1,true);yield return Wait(()=>All(2,p=>p.mask==3),"both players ready",10);
        var match=ArenaObjectiveMatch.Instance;match.roundSeconds=300;
        Require(match.StartRound(),"normal room start");
        yield return Wait(()=>All(2,p=>p.phase==1),"normal countdown complete",15);
        var own=Player(1);yield return Wait(()=>own.HasCurrentLifePose,"normal combat life applied",5);
        yield return CheckWestBridge(own);
    }
    static IEnumerator WestBridgeClient(SideScrollNetworkPlayer own)
    {
        var stack=new Stack<IEnumerator>();stack.Push(WestBridgeChecks(own));string result="PASS";
        while(stack.Count>0)
        {
            bool more=false;object next=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){result="FAIL: "+failure;break;}
            if(!more){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        foundryDriving=false;own.GetComponent<SideScrollMapPreview>().SetInput(0);File.WriteAllText(folder+"/west-bridge-client.txt",result);
    }
    static IEnumerator WestBridgeChecks(SideScrollNetworkPlayer own)
    {
        foundryDriving=true;var bridge=UnityEngine.Object.FindFirstObjectByType<ArenaRetractingBridge>();
        var move=own.GetComponent<SideScrollMapPreview>();var rules=RelayArenaPrototype.Instance;
        Require(bridge!=null&&rules.jumpSpeed==7.5f,"west bridge exists; accepted jump preserved");
        Require(!rules.IsSafeLanding(new Vector2(-22,-3.6f))&&rules.IsSafeLanding(new Vector2(0,-2.8f)),"pit excluded from safe drops; service bypass remains safe");
        foreach(int direction in new[]{1,-1})
        {
            move.SetInput(0);move.SetSpawnPosition(new Vector2(direction>0?-25:-19,.04f));
            yield return Wait(()=>bridge.CycleTime<4,"safe natural cycle crossing",16);
            yield return FoundryTraverse(own,new Vector2(direction>0?-25:-19,0),new Vector2(direction>0?-19:-25,0),false,"west-bridge-"+direction);
        }
        move.SetInput(0);move.SetSpawnPosition(new Vector2(-25,.04f));
        yield return Wait(()=>bridge.CycleTime>8&&bridge.CycleTime<9.5f,"warning phase",16);
        Require(bridge.status.text.Contains("WARNING"),"visible warning precedes opening");
        yield return Wait(()=>bridge.CycleTime>10.7f&&bridge.CycleTime<12,"fully open phase",5);
        Require(!bridge.westLeaf.GetComponent<Collider2D>().enabled&&!bridge.eastLeaf.GetComponent<Collider2D>().enabled,"fully retracted leaves have no invisible collision");
        Require(Physics2D.Raycast(new Vector2(-22,.1f),Vector2.down,1).collider==null,"open pit has no retained floor");
        ScreenCapture.CaptureScreenshot(folder+"/west-open-"+own.PlayerNumber+".png");yield return new WaitForSecondsRealtime(.25f);
        // Permanent route uses the existing outer slope, fixed middle deck,
        // then returns to the floor on the inner side of the new pit.
        yield return FoundryTraverse(own,new Vector2(-35,0),new Vector2(-24.4f,3.6f),true,"west-bypass-up");
        yield return FoundryTraverse(own,new Vector2(-24.4f,3.6f),new Vector2(-12.4f,3.6f),false,"west-bypass-deck");
        yield return DropAt(own,new Vector2(-12,3.6f),"west-bypass-drop");foundryDriving=true;
        yield return FoundryTraverse(own,new Vector2(-12,1.08f),new Vector2(-16,0),false,"west-bypass-down");
        Require(own.transform.position.x>bridge.right&&!own.IsDead,"bypass exits alive on inner side");
        move.SetSpawnPosition(new Vector2(-22,.04f));move.SetInput(0);
        yield return Wait(()=>own.IsDead,"actual open-bridge fall receives server death",18);
        yield return Wait(()=>!own.IsDead&&own.HasCurrentLifePose,"automatic respawn after west pit",10);
        Require(own.transform.position.y>-.2f,"respawn outside pit");
        foundryDriving=false;
    }
    static IEnumerator CheckWestBridge(SideScrollNetworkPlayer own)
    {
        yield return Wait(()=>P(2)!=null&&P(2).connected&&P(2).phase==1,"two peers in west bridge review",35);
        var bridge=UnityEngine.Object.FindFirstObjectByType<ArenaRetractingBridge>();
        foreach(float phase in new[]{2f,8.5f,10.3f,11.5f,13.7f})
        {
            yield return Wait(()=>bridge.CycleTime>phase&&bridge.CycleTime<phase+.2f,"shared phase sample "+phase,16);
            var peer=P(2);float delta=Mathf.Abs(peer.westCycle-bridge.CycleTime);delta=Mathf.Min(delta,14-delta);
            Require(delta<.3f&&Mathf.Abs(peer.westWidth-bridge.westLeaf.localScale.x)<1.05f,"shared clock/leaf at phase "+phase);
        }
        // Extend only this test round to allow sequential owner input checks.
        var state=ArenaObjectiveMatch.Instance.State.Value;state.End=ArenaObjectiveMatch.Instance.Now+300;ArenaObjectiveMatch.Instance.State.Value=state;
        yield return WestBridgeChecks(own);Send(2,66);
        yield return Wait(()=>File.Exists(folder+"/west-bridge-client.txt"),"participant west bridge traversal/death/respawn",100);
        Require(Read(folder+"/west-bridge-client.txt")=="PASS","participant passes west bridge checks");
        var rules=RelayArenaPrototype.Instance;
        Require(rules.pitLeft==24&&rules.pitRight==28,"existing east crossing retained");
        own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(-26,.04f));
        rules.overview=true;yield return new WaitForSecondsRealtime(.5f);
        ScreenCapture.CaptureScreenshot(folder+"/west-overview.png");yield return new WaitForSecondsRealtime(.25f);rules.overview=false;
    }
}
