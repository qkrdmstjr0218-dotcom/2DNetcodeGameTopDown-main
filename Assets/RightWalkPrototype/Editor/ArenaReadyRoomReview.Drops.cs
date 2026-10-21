using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    static IEnumerator DropAt(SideScrollNetworkPlayer own,Vector2 at,string label)
    {
        foundryDriving=true;var move=own.GetComponent<SideScrollMapPreview>();var crouch=own.GetComponent<CrouchTrial>();
        crouch.SetRequested(false);move.SetInput(0);move.SetSpawnPosition(at+Vector2.up*.04f);yield return Wait(()=>move.Grounded,"drop start "+label,3);
        crouch.SetRequested(true);yield return Wait(()=>crouch.IsCrouched,"crouch before drop "+label,2);
        move.SetInput(0,true);float until=Time.realtimeSinceStartup+2.2f;float minimum=own.transform.position.y,maximumReturn=0;
        var rows=new List<string>{"t,x,y,vy,grounded,support"};
        while(Time.realtimeSinceStartup<until)
        {
            float y=own.transform.position.y;minimum=Mathf.Min(minimum,y);if(minimum<at.y-.25f)maximumReturn=Mathf.Max(maximumReturn,y-minimum);
            rows.Add(Time.time+","+own.transform.position.x+","+y+","+own.GetComponent<Rigidbody2D>().linearVelocity.y+","+move.Grounded+","+(move.GroundCollider!=null?move.GroundCollider.name:""));yield return null;
        }
        File.WriteAllLines(folder+"/drop-"+own.PlayerNumber+"-"+label+".csv",rows);
        Require(move.Grounded&&own.transform.position.y<at.y-.3f&&maximumReturn<.25f,"drop remains below original surface "+label);
        crouch.SetRequested(false);yield return new WaitForSecondsRealtime(.25f);
        Require(!crouch.IsCrouched,"can stand below original platform "+label);foundryDriving=false;
    }
    static IEnumerator CheckFoundryConsoles()
    {
        var match=ArenaObjectiveMatch.Instance;
        Require(match.Points.Length==6,"six console candidates");
        foreach(var p in match.Points)Require(p.console!=null&&p.console.GetComponentInChildren<Collider2D>()==null,"console landmark has no gameplay collision "+p.displayName);
        var state=match.State.Value;state.Point=0;state.Warning=match.Now-1;state.Switch=match.Now+5;match.State.Value=state;
        yield return new WaitForSecondsRealtime(.2f);
        Require(match.Points[0].console.glow.color.a>.1f&&match.Points[1].console.glow.color.a>0&&match.Points[2].console.glow.color.a==0,"active next and inactive console states differ");
        foundryDriving=true;var own=Player(1);own.GetComponent<SideScrollMapPreview>().SetInput(0);own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(1.5f,.04f));
        yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(folder+"/console-active.png");yield return new WaitForSecondsRealtime(.2f);
        own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(-20.5f,3.64f));yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(folder+"/console-next.png");yield return new WaitForSecondsRealtime(.2f);
        var box=GameObject.Find("Lower outer cover 1").GetComponent<BoxCollider2D>();Require(box.bounds.min.x>RelayArenaPrototype.Instance.pitRight,"east box is fully outside opening bridge");foundryDriving=false;
    }
    static IEnumerator CheckFoundryDrops(SideScrollNetworkPlayer own)
    {
        yield return DropAt(own,new Vector2(9,7.2f),"east-upper-over-ramp");
        yield return DropAt(own,new Vector2(-9,7.2f),"west-upper-over-ramp");
        yield return DropAt(own,new Vector2(10,3.6f),"east-middle-over-ramp");
        yield return DropAt(own,new Vector2(-10,3.6f),"west-middle-over-ramp");
        foundryDriving=true;var move=own.GetComponent<SideScrollMapPreview>();var crouch=own.GetComponent<CrouchTrial>();
        crouch.SetRequested(false);move.SetSpawnPosition(new Vector2(9,7.24f));move.SetInput(0);yield return Wait(()=>move.Grounded,"chain drop start");
        for(int i=0;i<2;i++)
        {
            float before=own.transform.position.y;crouch.SetRequested(true);yield return Wait(()=>crouch.IsCrouched,"chain crouch "+i);move.SetInput(0,true);
            yield return Wait(()=>move.Grounded&&own.transform.position.y<before-.3f,"chain lower landing "+i,3);yield return new WaitForSecondsRealtime(.25f);
        }
        yield return new WaitForSecondsRealtime(1.8f);Require(own.transform.position.y<4,"two overlapping pending drops stay below");
        crouch.SetRequested(false);yield return new WaitForSecondsRealtime(.2f);
        yield return DropAt(own,new Vector2(9,7.2f),"air-jump-return-setup");foundryDriving=true;
        move.SetInput(0,true);yield return Wait(()=>move.Grounded&&own.transform.position.y>6.95f,"air jump can land on original grate again",4);
        crouch.SetRequested(false);move.SetInput(0);
        move.SetSpawnPosition(new Vector2(-9,7.24f));yield return Wait(()=>move.Grounded,"midair return start");crouch.SetRequested(true);yield return Wait(()=>crouch.IsCrouched,"midair return crouch");move.SetInput(0,true);
        yield return Wait(()=>!move.Grounded,"drop is airborne before any lower landing");crouch.SetRequested(false);yield return Wait(()=>!crouch.IsCrouched,"airborne drop releases crouch");move.SetInput(0,true);
        yield return Wait(()=>move.Grounded&&own.transform.position.y>6.95f,"air jump before landing returns onto original grate",4);move.SetInput(0);foundryDriving=false;
    }
}
