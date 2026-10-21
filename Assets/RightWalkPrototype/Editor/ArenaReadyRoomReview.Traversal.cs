using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;

public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Foundry Jump and Spawn Covers")]
    public static void BeginTraversalTuning(){BeginEntranceCovers();File.WriteAllText(folder+"/tuning-review.txt","tuning");}
    static IEnumerator FoundryTraversalClient(SideScrollNetworkPlayer own)
    {
        var stack=new Stack<IEnumerator>();stack.Push(FoundryTraversalChecks(own));string result="PASS";
        while(stack.Count>0)
        {
            bool more=false;object next=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){result="FAIL: "+failure;break;}
            if(!more){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        foundryDriving=false;own.GetComponent<SideScrollMapPreview>().SetInput(0);
        File.WriteAllText(folder+"/traversal-client.txt",result);
    }
    static IEnumerator FoundryTraversalChecks(SideScrollNetworkPlayer own)
    {
        foundryDriving=true;var move=own.GetComponent<SideScrollMapPreview>();var aim=own.GetComponentInChildren<RightFacingAimRig>();
        Require(Mathf.Approximately(move.jumpSpeed,7.5f),"Foundry owner uses jump speed 7.5 P"+own.PlayerNumber);
        foreach(int side in new[]{-1,1})
        {
            var cover=GameObject.Find("Lower outer cover "+side);var box=cover.GetComponent<BoxCollider2D>();var bounds=box.bounds;
            Require(box.usedByEffector&&cover.GetComponent<PlatformEffector2D>().useOneWay,"spawn cover is body-passable "+side);
            foreach(int dir in new[]{-1,1})
            {
                float from=bounds.center.x-dir*1.6f,to=bounds.center.x+dir*1.6f;
                yield return FoundryTraverse(own,new Vector2(from,0),new Vector2(to,0),false,"spawn-pass-"+side+"-"+dir);
                foundryDriving=true;
            }
            var outside=new Vector2(bounds.min.x-.4f,1.1f);
            aim.TraceShot(outside,Vector2.right,2.4f,out Collider2D hit,true);
            Require(hit==box,"normal and shotgun trace still hits dotted box "+side);
            var inside=new Vector2(bounds.center.x,1.1f);
            var stop=aim.TraceShot(inside,Vector2.right,1.2f,out hit,true);
            Require(hit==box&&Vector2.Distance(stop,inside)<.01f,"body-to-muzzle trace blocks firing out from inside "+side);
            Require(ArenaGrenadeBallistics.WorldTrace(inside,inside+Vector2.right*1.2f,.09f,out _),"grenade body-to-muzzle path also blocks inside cover "+side);
            move.SetInput(0);move.SetSpawnPosition(new Vector2(bounds.center.x,.04f));yield return Wait(()=>move.Grounded,"jump inside dotted cover "+side);
            move.SetInput(0,true);yield return Wait(()=>!move.Grounded,"single jump leaves floor "+side);
            yield return Wait(()=>move.Grounded&&move.GroundCollider==box,"single jump lands on 1.8 dotted box "+side,4);
            Require(Mathf.Abs(own.transform.position.y-1.8f)<.2f,"dotted box supports feet "+side);
            yield return DropAt(own,new Vector2(bounds.center.x,1.8f),"dotted-box-"+side);foundryDriving=true;
            move.SetInput(1);yield return new WaitForSecondsRealtime(.8f);move.SetInput(0);
            yield return CoverHop(own,new Vector2(side*21.6f,7.2f),side*23,9,"solid 1.8 upper box "+side);
        }
        move.SetInput(0);move.SetSpawnPosition(new Vector2(31,.04f));yield return new WaitForSecondsRealtime(1);
        if(own.PlayerNumber==1){ScreenCapture.CaptureScreenshot(folder+"/spawn-dotted-cover.png");yield return new WaitForSecondsRealtime(.25f);}
        foundryDriving=false;
    }
}
