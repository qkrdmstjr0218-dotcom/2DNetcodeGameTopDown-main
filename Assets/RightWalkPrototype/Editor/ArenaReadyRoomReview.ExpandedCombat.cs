using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Special Cycle and Expanded Enemies")]
    public static void BeginExpandedCombat(){BeginEntrances();File.WriteAllText(folder+"/expanded-combat.txt","expanded");}
    static void VerifySpecialCycle(SideScrollNetworkPlayer own)
    {
        own.SetSpecialHeld(false);own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.InstantShotgun);
        foreach(int direction in new[]{1,-1})
        {
            var expected=direction>0?new[]{3,4,2,3,4,2}:new[]{4,3,2,4,3,2};
            foreach(int kind in expected)
            {
                own.CycleAmmo(direction);Require((int)own.SelectedAmmo==kind,"special-only cycle "+direction+" -> "+kind);
                Require(own.ActiveFireKind==SideScrollNetworkPlayer.AmmoKind.Normal,"preselection leaves normal fire active");
            }
        }
        own.SetSpecialHeld(true);own.CycleAmmo(-1);
        Require(own.GrenadeActive,"held Q selects grenade from shotgun");own.CycleAmmo(1);Require(own.InstantShotgunActive,"held E wraps grenade to shotgun");
        own.SetSpecialHeld(false);Require(own.ActiveFireKind==SideScrollNetworkPlayer.AmmoKind.Normal&&own.SelectedAmmo==SideScrollNetworkPlayer.AmmoKind.InstantShotgun,"release restores normal without forgetting special choice");
    }
    static IEnumerator SpecialCycleClient(SideScrollNetworkPlayer own)
    {
        string result="PASS";try{VerifySpecialCycle(own);}catch(System.Exception e){result="FAIL "+e.Message;}
        File.WriteAllText(folder+"/special-cycle-client.txt",result);yield break;
    }
    static IEnumerator SolidRestClient(SideScrollNetworkPlayer own)
    {
        var stack=new Stack<IEnumerator>();stack.Push(SolidSlopeRest(own));string result="PASS";
        while(stack.Count>0)
        {
            object item=null;bool next=false;try{next=stack.Peek().MoveNext();if(next)item=stack.Peek().Current;}catch(System.Exception e){result="FAIL "+e.Message;break;}
            if(!next){stack.Pop();continue;}if(item is IEnumerator child)stack.Push(child);else yield return item;
        }
        foundryDriving=false;own.GetComponent<SideScrollMapPreview>().SetInput(0);File.WriteAllText(folder+"/solid-rest-client.txt",result);
    }
    static IEnumerator SolidSlopeRest(SideScrollNetworkPlayer own)
    {
        foundryDriving=true;var move=own.GetComponent<SideScrollMapPreview>();var body=own.GetComponent<Rigidbody2D>();
        var rows=new List<string>{"side,direction,sprint,phase,x,y,vx,vy,grounded,support"};float worstDrift=0;
        foreach(int side in new[]{-1,1})foreach(int direction in new[]{-1,1})foreach(bool sprint in new[]{false,true})
        {
            move.SetInput(0);move.SetSpawnPosition(new Vector2(side*5.3f,-1.4f+.3f));
            yield return Wait(()=>move.Grounded&&move.GroundCollider!=null&&!move.GroundCollider.usedByEffector,"solid slope settled "+side+"/"+direction+"/"+sprint,3);
            move.SetInput(direction,false,sprint);float until=Time.time+.42f;
            while(Time.time<until)
            {rows.Add(side+","+direction+","+sprint+",moving,"+body.position.x+","+body.position.y+","+body.linearVelocity.x+","+body.linearVelocity.y+","+move.Grounded+","+(move.GroundCollider!=null?move.GroundCollider.name:""));yield return null;}
            float expected=sprint?RelayArenaPrototype.Instance.runSpeed:RelayArenaPrototype.Instance.walkSpeed;
            File.WriteAllLines(folder+"/solid-rest-"+own.PlayerNumber+".csv",rows);
            Require(Mathf.Abs(body.linearVelocity.x-direction*expected)<.15f,"solid ramp reaches commanded horizontal speed "+side+"/"+direction+"/"+sprint+" ("+body.linearVelocity.x+")");
            move.SetInput(0);yield return new WaitForSecondsRealtime(.25f);Vector2 stopped=body.position;until=Time.realtimeSinceStartup+1;
            while(Time.realtimeSinceStartup<until)
            {
                worstDrift=Mathf.Max(worstDrift,Vector2.Distance(stopped,body.position));
                rows.Add(side+","+direction+","+sprint+",rest,"+body.position.x+","+body.position.y+","+body.linearVelocity.x+","+body.linearVelocity.y+","+move.Grounded+","+(move.GroundCollider!=null?move.GroundCollider.name:""));yield return null;
            }
        }
        File.WriteAllLines(folder+"/solid-rest-"+own.PlayerNumber+".csv",rows);File.WriteAllText(folder+"/solid-rest-summary-"+own.PlayerNumber+".txt","worstDrift="+worstDrift);
        Require(worstDrift<.04f,"solid slope idle drift below .04 ("+worstDrift+")");
        move.SetInput(0);foundryDriving=false;
    }
}
