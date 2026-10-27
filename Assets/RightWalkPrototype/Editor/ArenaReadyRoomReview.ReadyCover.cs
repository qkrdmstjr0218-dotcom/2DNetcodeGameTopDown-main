using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Ready Room Cover")]
    public static void BeginReadyCover(){BeginReview(false);File.WriteAllText(folder+"/ready-cover.txt","cover");}
    static IEnumerator ReadyCoverClient(SideScrollNetworkPlayer own,int action)
    {
        if(action==75)
        {own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(own.PlayerNumber==1?98:own.PlayerNumber==2?102:own.PlayerNumber==3?94:106,.04f));yield break;}
        own.GetComponentInChildren<RightFacingAimRig>().SetAimDirection(own.PlayerNumber==1?Vector2.right:Vector2.left);
        own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Rifle);own.SetSpecialHeld(action==77);yield return new WaitForSecondsRealtime(.25f);SampleFireButtons(own,action==77,true);
    }
    static IEnumerator CheckReadyCover()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.grounded),"four peers standing clear of practice crate",45);
        var box=GameObject.Find("Rifle practice solid crate");Require(box!=null&&box.TryGetComponent<BoxCollider2D>(out var shape)&&!shape.isTrigger&&!shape.usedByEffector,"central practice crate is solid");
        Require(GameObject.Find("Rifle practice label")==null,"redundant piercing sign removed");
        Send(0,75);yield return Wait(()=>Mathf.Abs(P(1).position.x-98)<.1f&&Mathf.Abs(P(2).position.x-102)<.1f&&All(4,p=>p.grounded),"players positioned on both sides of crate");
        float ammo=P(1).normal;Send(1,76);yield return Wait(()=>P(1).normal<ammo,"normal practice shot accepted");yield return new WaitForSecondsRealtime(.5f);Require(Player(2).Health==100,"crate blocks normal bullet");
        Send(1,77);yield return Wait(()=>P(2).hp==70&&P(1).rifleAmmo==2,"host rifle pierces crate with current thirty damage",3);ScreenCapture.CaptureScreenshot(folder+"/ready-room-cover.png");
        Send(2,77);yield return Wait(()=>P(1).hp==70&&P(2).rifleAmmo==2,"client rifle pierces crate with current thirty damage",3);
        Require(All(4,p=>p.phase==0&&p.hp>0),"practice remains nonlethal in waiting room");
        Require(ArenaObjectiveMatch.TurretRange==9&&ArenaObjectiveMatch.TurretVerticalRange==3.25f,"turret range adjusted to nine with vertical limit preserved");
    }
}
