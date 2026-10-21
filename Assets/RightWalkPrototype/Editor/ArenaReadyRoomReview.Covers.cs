using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;

public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Foundry Entrance Covers")]
    public static void BeginEntranceCovers(){BeginFoundry();File.WriteAllText(folder+"/entrance-cover.txt","covers");}
    [MenuItem("Tools/Right Walk Prototype/Capture Foundry Entrance Covers")]
    public static void CaptureEntranceCovers(){BeginEntranceCovers();File.WriteAllText(folder+"/capture-cover.txt","capture");}
    static IEnumerator CoverHop(SideScrollNetworkPlayer own,Vector2 start,float end,float top,string label)
    {
        var move=own.GetComponent<SideScrollMapPreview>();move.SetInput(0);move.SetSpawnPosition(start+Vector2.up*.04f);
        yield return Wait(()=>move.Grounded,"hop start "+label,3);
        float direction=Mathf.Sign(end-start.x);move.SetInput(direction,true);
        float until=Time.realtimeSinceStartup+3;
        while(direction*(end-own.transform.position.x)>.06f&&Time.realtimeSinceStartup<until)yield return null;
        move.SetInput(0);yield return new WaitForSecondsRealtime(.5f);
        Require(move.Grounded&&Mathf.Abs(own.transform.position.x-end)<.35f&&Mathf.Abs(own.transform.position.y-top)<.2f,"single jump reaches "+label+" at "+own.transform.position);
    }
    static IEnumerator EntranceGap(SideScrollNetworkPlayer own,int side,bool entering)
    {
        var move=own.GetComponent<SideScrollMapPreview>();
        move.SetInput(0);move.SetSpawnPosition(new Vector2(side*(entering?9:2.65f),(entering?0:.45f)+.04f));
        yield return Wait(()=>move.Grounded,"gap start "+side+"/"+entering,3);
        float dir=entering?-side:side,target=side*(entering?2.65f:7.8f);
        move.SetInput(dir,false,true);
        if(entering)yield return Wait(()=>Mathf.Abs(own.transform.position.x)<8.4f,"approach ramp mouth",2);
        // Stay below the existing solid middle-deck lip: an early air jump
        // hits its side/underside instead of carrying across the opening.
        move.SetInput(dir,true,true);
        if(entering)yield return Wait(()=>own.GetComponent<Rigidbody2D>().linearVelocity.y<-.1f&&own.transform.position.y<-.5f,"late air jump below middle ceiling",2);
        else yield return new WaitForSecondsRealtime(.6f);
        move.SetInput(dir,true,true);
        float until=Time.realtimeSinceStartup+3;
        while(dir*(target-own.transform.position.x)>.06f&&Time.realtimeSinceStartup<until)yield return null;
        move.SetInput(0);yield return new WaitForSecondsRealtime(.6f);
        Require(move.Grounded&&Mathf.Abs(own.transform.position.x-target)<.45f&&own.transform.position.y>(entering?.25f:-.2f),"air-jump route crosses entrance "+side+"/"+entering+" at "+own.transform.position);
    }
    static IEnumerator CheckEntranceCovers()
    {
        yield return Wait(()=>Player(1)!=null&&ArenaObjectiveMatch.Instance!=null&&ArenaObjectiveMatch.Instance.IsSpawned,"cover review host spawned");
        Require(ArenaObjectiveMatch.Instance.StartRound(),"cover diagnostic starts");
        yield return Wait(()=>ArenaObjectiveMatch.Instance.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing,"cover diagnostic playing");
        var own=Player(1);yield return Wait(()=>own.HasCurrentLifePose,"round spawn applied before traversal");
        var move=own.GetComponent<SideScrollMapPreview>();foundryDriving=true;
        if(File.Exists(folder+"/seam-review.txt"))
        {
            bool two=File.Exists(folder+"/two-peer-seams.txt");
            if(two)yield return Wait(()=>P(2)!=null&&P(2).connected&&P(2).phase==1,"two peers for seam review",35);
            yield return CheckBypassSeams(own);
            if(two){Send(2,65);yield return Wait(()=>File.Exists(folder+"/seam-client.txt"),"client seam review complete",65);Require(Read(folder+"/seam-client.txt")=="PASS","client seam traversal and jump pass");}
            yield break;
        }
        if(File.Exists(folder+"/tuning-review.txt"))
        {
            yield return Wait(()=>P(2)!=null&&P(2).connected&&P(2).phase==1,"two peers in traversal review",35);
            yield return FoundryTraversalChecks(own);Send(2,64);
            yield return Wait(()=>File.Exists(folder+"/traversal-client.txt"),"client traversal checks complete",60);
            Require(Read(folder+"/traversal-client.txt")=="PASS","client jump and passable cover checks pass");
            yield return CheckFoundryDrops(own);yield break;
        }
        if(File.Exists(folder+"/capture-cover.txt")){yield return CaptureEntranceCoverViews(own);foundryDriving=false;yield break;}
        var group=GameObject.Find(FoundryEntranceCoverBuilder.RootName);
        Require(group!=null&&group.GetComponentsInChildren<ArenaCover>().Length==6,"two stacks contain six solid crates");
        var aim=own.GetComponentInChildren<RightFacingAimRig>();
        foreach(int side in new[]{-1,1})
        {
            var hitAt=aim.TraceShot(new Vector2(0,.7f),Vector2.right*side,3.5f,out Collider2D hit,true);
            Require(hit!=null&&hit.GetComponent<ArenaCover>()!=null&&hit.transform.IsChildOf(group.transform)&&Mathf.Abs(Mathf.Abs(hitAt.x)-1.6f)<.03f,"normal trace hits entrance cover "+side);
            aim.TraceShot(new Vector2(0,1.2f),Vector2.right*side,2.9f,out hit,true);
            Require(hit==null,"aim above low cover remains clear "+side);
            yield return CoverHop(own,new Vector2(side*1.15f,0),side*2,.9f,"inside stack "+side);
            var roof=GameObject.Find("Middle centre deck").GetComponent<Collider2D>();
            Require(Physics2D.Distance(own.GetComponent<BoxCollider2D>(),roof).distance>0,"standing on stack clears solid ceiling "+side);
            var at=own.transform.position;yield return new WaitForSecondsRealtime(.8f);
            Require(Vector2.Distance(at,own.transform.position)<.03f,"stack standing stable "+side);
            yield return CoverHop(own,new Vector2(side*2.65f,.45f),side*2,.9f,"outer step to stack "+side);
            yield return EntranceGap(own,side,true);yield return EntranceGap(own,side,false);
        }
        // The bypass remains walkable beneath the stacks, without jump input.
        yield return FoundryTraverse(own,new Vector2(-7.8f,0),new Vector2(7.8f,0),false,"cover-bypass-west-east");
        yield return FoundryTraverse(own,new Vector2(7.8f,0),new Vector2(-7.8f,0),false,"cover-bypass-east-west");
        move.SetInput(0);move.SetSpawnPosition(new Vector2(0,.04f));yield return Wait(()=>move.Grounded,"centre objective ground");
        Require(ArenaObjectiveMatch.Instance.Points[0].Contains(own),"centre capture remains accessible");
        yield return CaptureEntranceCoverViews(own);
        yield return new WaitForSecondsRealtime(.3f);foundryDriving=false;
    }
    static IEnumerator CaptureEntranceCoverViews(SideScrollNetworkPlayer own)
    {
        var move=own.GetComponent<SideScrollMapPreview>();move.SetInput(0);move.SetSpawnPosition(new Vector2(0,.04f));
        own.GetComponentInChildren<RightFacingAimRig>().SetAimDirection(Vector2.right);
        RelayArenaPrototype.Instance.overview=false;
        yield return new WaitForSecondsRealtime(.8f);ScreenCapture.CaptureScreenshot(folder+"/entrance-cover-play.png");
        // CaptureScreenshot resolves at frame end; don't switch the camera in that frame.
        yield return new WaitForSecondsRealtime(.25f);
        RelayArenaPrototype.Instance.overview=true;yield return new WaitForSecondsRealtime(.3f);
        ScreenCapture.CaptureScreenshot(folder+"/entrance-cover-overview.png");yield return new WaitForSecondsRealtime(.25f);
        RelayArenaPrototype.Instance.overview=false;
    }
}
