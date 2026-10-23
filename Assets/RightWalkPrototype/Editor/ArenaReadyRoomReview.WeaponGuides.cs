using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Weapon Aim Guides")]
    public static void BeginWeaponGuides(){BeginFoundry();File.WriteAllText(folder+"/weapon-guides.txt","guides");}
    static IEnumerator CheckWeaponGuides()
    {
        yield return Wait(()=>All(1,p=>p.connected&&p.phase==0&&p.roster==1),"offline solo host for local aim guides",60);
        var own=Player(1);var aim=own.GetComponentInChildren<RightFacingAimRig>();
#if ENABLE_INPUT_SYSTEM
        if(UnityEngine.InputSystem.Mouse.current!=null)UnityEngine.InputSystem.InputSystem.QueueDeltaStateEvent(UnityEngine.InputSystem.Mouse.current.position,new Vector2(Screen.width*.65f,Screen.height*.5f));
#endif
        yield return null;
        yield return CheckAmmoPreselection();
        Require(own.AmmoSelectionHintCountText==own.SelectedAmmoRounds.ToString(),"selection hint contains remaining rounds without capacity");
        own.SetSpecialHeld(false);own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.InstantShotgun);
        Require(!own.WeaponGuideVisible,"Q/E preselection alone has no special guide");
        own.SetSpecialHeld(true);aim.SetAimDirection(Vector2.up);
        Require(own.WeaponGuideVisible&&own.WeaponGuideState==1,"held shotgun shows switch-wait guide");
        foreach(var p in Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(!p.IsOwner)Require(!p.WeaponGuideVisible,"remote weapon guides stay private");
        yield return Wait(()=>own.FireWaitRemaining==0,"shotgun guide becomes ready",3);
        Vector2 origin=aim.MuzzlePosition;Vector2 direction=aim.AimDirection;
        var edgeA=own.ShotgunGuidePoint(-SideScrollNetworkPlayer.ShotgunSpread*.5f);var edgeB=own.ShotgunGuidePoint(SideScrollNetworkPlayer.ShotgunSpread*.5f);
        Require(Mathf.Abs(Vector2.Distance(origin,edgeA)-SideScrollNetworkPlayer.ShotgunRange)<.01f&&Mathf.Abs(Vector2.Distance(origin,edgeB)-SideScrollNetworkPlayer.ShotgunRange)<.01f,"fan reaches actual shotgun maximum range");
        Require(Mathf.Abs(Vector2.Angle(edgeA-origin,edgeB-origin)-SideScrollNetworkPlayer.ShotgunSpread)<.1f,"fan envelopes full server pellet spread including jitter");
        ScreenCapture.CaptureScreenshot(folder+"/shotgun-guide.png");yield return new WaitForSecondsRealtime(.15f);
        var wall=new GameObject("Aim guide temporary solid wall");wall.transform.position=origin+direction*2;var box=wall.AddComponent<BoxCollider2D>();box.size=new Vector2(2,.2f);Physics2D.SyncTransforms();
        try
        {
            Require(Vector2.Distance(origin,own.ShotgunGuidePoint(0))<2,"shotgun guide clips at solid wall");
            own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Rifle);
            Require(Vector2.Distance(origin,own.RifleGuideEnd)>SideScrollNetworkPlayer.RifleRange-.01f,"rifle guide passes through same solid wall to exact maximum range");
        }
        finally{Object.DestroyImmediate(wall);Physics2D.SyncTransforms();}
        aim.SetAimDirection(Vector2.right);
        yield return Wait(()=>own.FireWaitRemaining==0,"rifle guide becomes ready",3);
        Require(own.WeaponGuideState==0,"ready rifle uses normal purple guide");
        ScreenCapture.CaptureScreenshot(folder+"/rifle-guide.png");yield return new WaitForSecondsRealtime(.15f);
        own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.InstantShotgun);
        var ammo=(NetworkVariable<int>)typeof(SideScrollNetworkPlayer).GetField("instantShotgunRounds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(own);int saved=ammo.Value;
        ammo.Value=0;
        Require(own.WeaponGuideVisible&&own.WeaponGuideState==2&&own.AmmoSelectionHintCountText=="0","empty shotgun keeps red guide and bare zero count");
        ScreenCapture.CaptureScreenshot(folder+"/empty-guide-hint.png");yield return new WaitForSecondsRealtime(.15f);ammo.Value=saved;
        own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Grenade);Require(!own.WeaponGuideVisible,"grenade keeps separate ballistic preview");
        own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Rifle);own.SetSpecialHeld(false);Require(!own.WeaponGuideVisible,"release hides special guide");own.SetSpecialHeld(true);
        RelayArenaPrototype.Instance.SetMenuOpen(true);Require(!own.WeaponGuideVisible,"menu hides guide");RelayArenaPrototype.Instance.SetMenuOpen(false);
        RelayArenaPrototype.Instance.overview=true;Require(!own.WeaponGuideVisible,"map overview hides guide");RelayArenaPrototype.Instance.overview=false;
        var match=ArenaObjectiveMatch.Instance;Send(0,1,true);yield return Wait(()=>All(1,p=>p.mask==1),"guide peers ready");Require(match.StartRound(),"start guide lifecycle check");Require(!own.WeaponGuideVisible,"transition hides guide");
        yield return Wait(()=>All(1,p=>p.phase==1),"guide peers reach gameplay",20);
        own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Rifle);own.SetSpecialHeld(true);own.ReceiveServerDamage(999,own.LifeVersion);Require(own.IsDead&&!own.WeaponGuideVisible,"death hides guide");
        var state=match.State.Value;state.End=match.Now+.1;match.State.Value=state;yield return Wait(()=>All(1,p=>p.phase==2),"guide peers reach results");Require(!own.WeaponGuideVisible,"results hide guide");Require(match.ReturnToRoom(),"guide test returns to room");
        yield return Wait(()=>All(1,p=>p.phase==0),"guide peers returned to room");Require(!own.WeaponGuideVisible,"new life and room return require fresh special hold");
    }
}
