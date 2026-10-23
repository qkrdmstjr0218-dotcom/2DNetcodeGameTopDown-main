using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;

public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Foundry Lower Seams")]
    public static void BeginLowerSeams(){BeginEntranceCovers();File.WriteAllText(folder+"/seam-review.txt","seams");}
    [MenuItem("Tools/Right Walk Prototype/Verify Foundry Lower Seams Two Peers")]
    public static void BeginTwoPeerSeams(){BeginLowerSeams();File.WriteAllText(folder+"/two-peer-seams.txt","two");}
    [MenuItem("Tools/Right Walk Prototype/Verify Foundry Running Seams Two Peers")]
    public static void BeginRunningSeams(){BeginTwoPeerSeams();File.WriteAllText(folder+"/running-seams.txt","running");}
    static IEnumerator FoundrySeamClient(SideScrollNetworkPlayer own)
    {
        var stack=new Stack<IEnumerator>();stack.Push(CheckBypassSeams(own));string result="PASS";
        while(stack.Count>0)
        {
            bool more=false;object next=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){result="FAIL: "+failure;break;}
            if(!more){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        foundryDriving=false;own.GetComponent<SideScrollMapPreview>().SetInput(0);File.WriteAllText(folder+"/seam-client.txt",result);
    }
    static IEnumerator CheckBypassSeams(SideScrollNetworkPlayer own)
    {
        foundryDriving=true;var move=own.GetComponent<SideScrollMapPreview>();var body=own.GetComponent<Rigidbody2D>();
        var crouch=own.GetComponent<CrouchTrial>();
        var rows=new List<string>{"side,uphill,crouched,x,y,vx,vy,grounded,support,sole,join"};float worstPop=0,minPeakSpeed=float.MaxValue;bool allReached=true;
        bool running=File.Exists(folder+"/running-seams.txt");
        foreach(int side in new[]{-1,1})foreach(bool seated in running?new[]{false}:new[]{false,true})foreach(bool uphill in new[]{false,true})
        {
            crouch.SetRequested(seated);move.SetInput(0);move.SetSpawnPosition(new Vector2(side*8,.04f));
            yield return Wait(()=>move.Grounded,"flat reference "+side+"/"+seated,3);
            yield return new WaitForSecondsRealtime(.2f);float flatY=body.position.y;
            float start=side*(uphill?5.4f:8),end=side*(uphill?8:5.4f),dir=Mathf.Sign(end-start);
            move.SetSpawnPosition(new Vector2(start,(uphill?-1.32f:0)+.04f));
            yield return Wait(()=>move.Grounded,"seam start "+side+"/"+seated+"/"+uphill,3);
            move.SetInput(dir,false,running);float until=Time.realtimeSinceStartup+6,peakSpeed=0,minJoinSpeed=float.MaxValue;
            while(dir*(end-body.position.x)>.04f&&Time.realtimeSinceStartup<until)
            {
                worstPop=Mathf.Max(worstPop,body.position.y-flatY);
                peakSpeed=Mathf.Max(peakSpeed,Mathf.Abs(body.linearVelocity.x));
                if(Mathf.Abs(body.position.x)>6.55f&&Mathf.Abs(body.position.x)<7.3f)minJoinSpeed=Mathf.Min(minJoinSpeed,Mathf.Abs(body.linearVelocity.x));
                rows.Add(side+","+uphill+","+seated+","+body.position.x+","+body.position.y+","+body.linearVelocity.x+","+body.linearVelocity.y+","+move.Grounded+","+(move.GroundCollider!=null?move.GroundCollider.name:"")+","+(own.GetComponent<BoxCollider2D>().bounds.min.y-own.GetComponent<BoxCollider2D>().edgeRadius)+","+((RaycastHit2D)typeof(ArenaPlatformMotor).GetField("flatJoin",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(own.GetComponent<ArenaPlatformMotor>())).collider);
                yield return null;
            }
            File.AppendAllText(folder+"/seam-speed-"+own.PlayerNumber+".txt",side+","+uphill+","+seated+",min="+minJoinSpeed+"\n");
            Require(minJoinSpeed>(seated?.6f:running?3.4f:1.8f),"no seam stall "+side+"/"+uphill+"/"+seated+" (min="+minJoinSpeed+")");
            allReached&=Mathf.Abs(body.position.x-end)<.4f;
            minPeakSpeed=Mathf.Min(minPeakSpeed,peakSpeed);
            move.SetInput(0);yield return new WaitForSecondsRealtime(.3f);
        }
        crouch.SetRequested(false);move.SetInput(0);yield return Wait(()=>!crouch.IsCrouched,"release crouch before seam jumps");
        foreach(int side in new[]{-1,1})
        {
            move.SetSpawnPosition(new Vector2(side*6.55f,-.30f));yield return Wait(()=>move.Grounded,"jump near seam start "+side);
            move.SetInput(side,true);yield return new WaitForSecondsRealtime(.18f);move.SetInput(0);
            Require(!move.Grounded&&body.position.y>.45f,"deliberate jump remains free near seam "+side);
            yield return Wait(()=>move.Grounded,"seam jump returns to floor "+side,4);
        }
        File.WriteAllLines(folder+"/seams-"+own.PlayerNumber+".csv",rows);File.WriteAllText(folder+"/seam-summary-"+own.PlayerNumber+".txt","allReached="+allReached+" maxRiseAboveFlat="+worstPop);
        Require(allReached,"walk/crouch traverses both upper ramp joins in both directions");
        if(running)Require(minPeakSpeed>3.4f,"every running route reaches sprint speed");
        Require(worstPop<.035f,"no ramp corner raises feet above adjoining floor (max="+worstPop+")");
        move.SetSpawnPosition(new Vector2(-26.5f,.04f));move.SetInput(0);
        if(own.PlayerNumber==1)
        {
            yield return new WaitForSecondsRealtime(1);ScreenCapture.CaptureScreenshot(folder+"/restored-cover.png");yield return new WaitForSecondsRealtime(.25f);
        }
        foundryDriving=false;
    }
}
