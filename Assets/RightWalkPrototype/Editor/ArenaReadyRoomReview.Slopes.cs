using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;

public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Foundry Slope Stops")]
    public static void BeginSlopeStops()
    {
        BeginFoundry();File.WriteAllText(folder+"/slope-only.txt","slopes");
    }
    static IEnumerator FoundrySlopeClient(SideScrollNetworkPlayer own)
    {
        var stack=new Stack<IEnumerator>();stack.Push(FoundrySlopeStops(own));string result="PASS";
        while(stack.Count>0)
        {
            bool more=false;object next=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){result="FAIL: "+failure;break;}
            if(!more){stack.Pop();continue;}
            if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        foundryDriving=false;own.GetComponent<SideScrollMapPreview>().SetInput(0);
        File.WriteAllText(folder+"/slope-client.txt",result);
    }
    static IEnumerator FoundrySlopeStops(SideScrollNetworkPlayer own)
    {
        foundryDriving=true;var move=own.GetComponent<SideScrollMapPreview>();var body=own.GetComponent<Rigidbody2D>();
        var crouch=own.GetComponent<CrouchTrial>();var shape=own.GetComponent<BoxCollider2D>();
        var samples=new List<string>{"label,t,x,y,vx,vy,grounded,gravity,touching,probeDistance,frameMs"};
        float worst=0;
        foreach(int side in new[]{-1,1})foreach(bool low in new[]{true,false})foreach(bool seated in new[]{false,true})
        {
            string label=(low?"solid-service":"oneway-existing")+side+(seated?"-crouch":"-stand");
            float x=side*(low?5.3f:30),y=low?-1.4f:1.8f;
            crouch.SetRequested(seated);move.SetInput(0);move.SetSpawnPosition(new Vector2(x,y+.15f));
            yield return new WaitForSecondsRealtime(.8f);
            // Release movement in the middle of the incline, not on the adjoining deck.
            move.SetInput(side*.5f);yield return new WaitForSecondsRealtime(.15f);move.SetInput(0);
            yield return new WaitForSecondsRealtime(.25f);Vector2 start=body.position;
            float max=0,until=Time.realtimeSinceStartup+1.8f;
            while(Time.realtimeSinceStartup<until)
            {
                max=Mathf.Max(max,Vector2.Distance(start,body.position));
                var b=shape.bounds;
                var hit=Physics2D.BoxCast(new Vector2(b.center.x,b.min.y-shape.edgeRadius+.03f),new Vector2(b.size.x*.85f,.04f),0,Vector2.down,.08f,1);
                samples.Add(label+","+Time.time+","+body.position.x+","+body.position.y+","+body.linearVelocity.x+","+body.linearVelocity.y+","+move.Grounded+","+body.gravityScale+","+(hit.collider!=null&&shape.IsTouching(hit.collider))+","+hit.distance+","+Time.unscaledDeltaTime*1000);
                yield return null;
            }
            worst=Mathf.Max(worst,max);samples.Add("drift,"+label+","+max);
            if(!seated)
            {
                float before=body.position.y;move.SetInput(0,true);
                yield return new WaitForSecondsRealtime(.18f);
                Require(!move.Grounded&&body.position.y>before+.25f&&body.gravityScale>0,"jump releases slope hold "+label);
                yield return Wait(()=>move.Grounded,"jump lands on slope "+label,4);
            }
        }
        File.WriteAllLines(folder+"/slope-stops-"+own.PlayerNumber+".csv",samples);
        crouch.SetRequested(false);move.SetInput(0);foundryDriving=false;
        Require(worst<.03f,"slope idle drift under .03 (P"+own.PlayerNumber+", max="+worst+")");
    }
}
