using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;

public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Ammo Preselection")]
    public static void BeginPreselection()
    {
        BeginFoundry();File.WriteAllText(folder+"/selection-only.txt","selection");
    }
    static IEnumerator CheckAmmoPreselection()
    {
        yield return Wait(()=>Player(1)!=null&&ArenaObjectiveMatch.PracticeRoom,"host ready for ammo selection review");
        var own=Player(1);foundryDriving=true;
        own.GetComponent<SideScrollMapPreview>().SetInput(0);
        own.GetComponentInChildren<RightFacingAimRig>().SetAimDirection(Vector2.up);
        var preview=own.GetComponentInChildren<RightFacingShotPreview>();
        own.SetSpecialHeld(false);
        yield return new WaitForSecondsRealtime(.3f);
        var serverReady=typeof(SideScrollNetworkPlayer).GetField("serverSwitchReady",BindingFlags.Instance|BindingFlags.NonPublic);
        double before=(double)serverReady.GetValue(own);
        foreach(int direction in new[]{1,1,1,1,-1,-1,-1,-1})
        {
            own.CycleAmmo(direction);
            Require(own.ActiveFireKind==SideScrollNetworkPlayer.AmmoKind.Normal&&!own.FireCursorBlocked&&own.FireWaitRemaining==0,"normal Q/E preselection has no switch wait");
        }
        Require((double)serverReady.GetValue(own)==before,"preselection leaves server switch deadline unchanged");
        float normal=own.NormalAmmo;preview.Fire();
        Require(own.NormalAmmo<normal,"normal fires immediately after preselection");
        float remaining=own.FireWaitRemaining;
        own.CycleAmmo(1);own.CycleAmmo(-1);
        Require(own.FireWaitRemaining<=remaining+.001f&&own.FireCursorBlocked,"preselection preserves existing normal shot cooldown");
        normal=own.NormalAmmo;preview.Fire();Require(own.NormalAmmo==normal,"normal shot interval still enforced");
        yield return new WaitForSecondsRealtime(.3f);
        own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Rifle);own.SetSpecialHeld(true);
        int rifle=own.RifleRounds;preview.Fire();RifleShot(own,own.LifeVersion);
        Require(own.FireCursorBlocked&&own.RifleRounds==rifle,"actual special switch blocks local and server fire");
        yield return new WaitForSecondsRealtime(.24f);preview.Fire();
        Require(own.RifleRounds==rifle-1,"rifle fires after real switch wait");
        own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.InstantShotgun);
        Require(own.FireCursorBlocked,"held-special Q/E still has switch wait");
        yield return new WaitForSecondsRealtime(.24f);
        own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Rifle);
        Require(own.FireWaitRemaining>.5f,"rifle cooldown survives preselection round trip");
        own.SetSpecialHeld(false);Require(own.FireWaitRemaining>0,"releasing special still has switch wait");
        yield return new WaitForSecondsRealtime(.24f);normal=own.NormalAmmo;preview.Fire();
        Require(own.NormalAmmo<normal,"normal fires after release wait");
        foundryDriving=false;
    }
}
