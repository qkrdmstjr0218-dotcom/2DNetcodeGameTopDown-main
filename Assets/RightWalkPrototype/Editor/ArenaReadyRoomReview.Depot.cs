using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Foundry Depot")]
    public static void BeginDepot(){BeginFoundry();File.WriteAllText(folder+"/depot.txt","depot");}
    static IEnumerator CheckDepot()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4),"four ready-room peers",45);
        var map=RelayArenaPrototype.Instance.GetComponent<ArenaMapSettings>();var depot=map.transform.Find(FoundryDepotBuilder.RootName);
        Require(depot!=null&&depot.GetComponentsInChildren<Collider2D>().Length==4,"only four front crates collide; background stacks are decoration");
        Require(map.shotgunSupplies.Length==3&&map.shotgunSupplies[2]==new Vector2(0,4.15f),"central supply relocated without adding extra supply sources");
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");var m=ArenaObjectiveMatch.Instance;m.roundSeconds=180;Require(m.StartRound(),"start depot round");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.pickupsAvailable==3),"three supplies synchronized");
        var own=Player(1);var move=own.GetComponent<SideScrollMapPreview>();foundryDriving=true;
        foreach(int side in new[]{-1,1})
        {
            yield return CoverHop(own,new Vector2(side*4.6f,3.6f),side*3.45f,4,"depot outer step "+side);
            yield return CoverHop(own,new Vector2(side*3.3f,4),side*2.4f,4.4f,"depot cover top "+side);
            Require(!own.GetComponent<CrouchTrial>().IsCrouched,"standing clearance on depot cover "+side);
        }
        move.SetSpawnPosition(new Vector2(-1,3.64f));move.SetInput(0);yield return Wait(()=>move.Grounded,"central supply lane clear");
        Require(ArenaGrenadeBallistics.WorldTrace(new Vector2(-4.5f,4.2f),new Vector2(-1,4.2f),0,out var hit)&&Mathf.Abs(hit.x+3)<.05f,"low front crate blocks shots at cover height");
        yield return new WaitForSecondsRealtime(.4f);ScreenCapture.CaptureScreenshot(folder+"/central-depot.png");
        SetShotgun(2,0);Send(2,78);yield return Wait(()=>P(2).ammo==3&&All(4,p=>p.pickupsAvailable==2),"client collects central supply once and disappearance synchronizes",5);
        Send(2,78,true);yield return Wait(()=>P(2).position.x>4.4f,"client leaves supply");
        yield return Wait(()=>All(4,p=>p.pickupsAvailable==3),"central supply respawns after fifteen seconds",17);
        Require(P(2).ammo==3,"supply does not refill an absent player");
        foundryDriving=false;var state=m.State.Value;state.End=m.Now+.05;m.State.Value=state;
        yield return Wait(()=>All(4,p=>p.phase==2&&p.pickupsCount==0),"finish removes depot pickup");Require(m.ReturnToRoom(),"return after depot test");yield return Wait(()=>All(4,p=>p.phase==0),"ready room restored");
    }
}
