using System.Collections;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    static IEnumerator FireAfterSwitch(SideScrollNetworkPlayer own,bool instant,bool rifle)
    {yield return new WaitForSecondsRealtime(SideScrollNetworkPlayer.WeaponSwitchSeconds+.08f);int before=own.RifleRounds,shotgunBefore=own.ShotgunRounds;own.GetComponentInChildren<RightFacingShotPreview>().Fire();if(instant&&own.ShotgunRounds<shotgunBefore)yield return CaptureInstant();if(rifle&&own.RifleRounds<before)yield return CaptureRifle();}
    static IEnumerator CheckFireTiming()
    {
        var host=Player(1);var preview=host.GetComponentInChildren<RightFacingShotPreview>();
        host.gameObject.AddComponent<ArenaFireTimingCursorReview>().Player=host;
        Send(1,17);yield return new WaitForSecondsRealtime(.3f);
        host.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Rifle);host.SetSpecialHeld(true);
        int rifle=host.RifleRounds;preview.Fire();RifleShot(host,host.LifeVersion);
        Require(host.RifleRounds==rifle&&host.FireCursorBlocked,"switch delay rejects local and server fire");
        yield return new WaitForSecondsRealtime(.06f);ScreenCapture.CaptureScreenshot(folder+"/switch-wait.png");
        yield return new WaitForSecondsRealtime(.18f);preview.Fire();Require(host.RifleRounds==rifle-1,"rifle fires after short switch delay");
        host.SetSpecialHeld(false);float normal=host.NormalAmmo;preview.Fire();Require(host.NormalAmmo==normal,"release to normal also waits for switch");
        yield return new WaitForSecondsRealtime(.23f);preview.Fire();Require(host.NormalAmmo<normal,"normal fires before rifle cooldown finishes");
        host.SetSpecialHeld(true);yield return new WaitForSecondsRealtime(.23f);
        preview.Fire();RifleShot(host,host.LifeVersion);Require(host.RifleRounds==rifle-1&&host.FireCursorBlocked,"switching back preserves rifle cooldown locally and on server");
        ScreenCapture.CaptureScreenshot(folder+"/rifle-cooldown.png");
        yield return new WaitForSecondsRealtime(host.FireWaitRemaining+.06f);Require(!host.FireCursorBlocked,"reticle returns ready when cooldown ends");
        preview.Fire();Require(host.RifleRounds==rifle-2,"rifle fires again after its own interval");
        host.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.InstantShotgun);int ammo=host.ShotgunRounds;
        preview.Fire();Require(host.ShotgunRounds==ammo,"shotgun also respects switch delay");
        yield return new WaitForSecondsRealtime(.23f);preview.Fire();Require(host.ShotgunRounds==ammo-1,"shotgun fires while rifle is cooling down");
        yield return null;ScreenCapture.CaptureScreenshot(folder+"/shotgun-muzzle.png");
        host.SetSpecialHeld(false);yield return new WaitForSecondsRealtime(.23f);normal=host.NormalAmmo;preview.Fire();Require(host.NormalAmmo<normal,"normal fires while shotgun is cooling down");
        host.SetSpecialHeld(true);yield return new WaitForSecondsRealtime(.23f);preview.Fire();Require(host.ShotgunRounds==ammo-1,"switch back cannot reset shotgun cooldown");
        yield return new WaitForSecondsRealtime(host.FireWaitRemaining+.05f);preview.Fire();Require(host.ShotgunRounds==ammo-2,"shotgun fires again after its own interval");
        yield return new WaitForSecondsRealtime(.7f);
        Require(((IList)typeof(RightFacingShotPreview).GetField("shotgunFlashes",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(preview)).Count==0,"shotgun muzzle visuals release after their lifetime");
        yield return ResetRifleRow();
    }
    static void SampleFireButtons(SideScrollNetworkPlayer own,bool special,bool primary)
    {typeof(RightFacingShotPreview).GetMethod("ProcessFireInput",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(own.GetComponentInChildren<RightFacingShotPreview>(),new object[]{false,special,primary,0});}
    static IEnumerator CheckHeldSpecial()
    {
        yield return RifleRow();Send(1,50);yield return new WaitForSecondsRealtime(.65f);yield return ResetRifleRow();yield return RifleRow();Send(1,47);
        yield return Wait(()=>P(1).ammo==4,"host held mouse input fires shotgun twice after switch delay",4);
        ScreenCapture.CaptureScreenshot(folder+"/shotgun-yellow.png");
        yield return new WaitForSecondsRealtime(.35f);Send(1,49);yield return new WaitForSecondsRealtime(.1f);
        ((NetworkVariable<int>)typeof(SideScrollNetworkPlayer).GetField("instantShotgunRounds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(Player(1))).Value=0;
        float normalBefore=Player(1).NormalAmmo;Send(1,47);yield return new WaitForSecondsRealtime(.7f);
        Require(Player(1).NormalAmmo==normalBefore&&Player(1).InstantShotgunActive&&Player(1).FireCursorBlocked,"held empty shotgun cannot spend normal rounds");
        ScreenCapture.CaptureScreenshot(folder+"/empty-shotgun-red.png");Send(1,49);yield return new WaitForSecondsRealtime(.15f);
        yield return ResetRifleRow();Send(4,48);
        yield return Wait(()=>P(4).rifleAmmo==1,"client held mouse input fires rifle twice without resetting switch delay",4);
        yield return new WaitForSecondsRealtime(.4f);Send(4,49);yield return new WaitForSecondsRealtime(.15f);
        Require(P(4).rifleAmmo==1,"held rifle preserves its own fire interval");
        ((NetworkVariable<int>)typeof(SideScrollNetworkPlayer).GetField("rifleRounds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(Player(4))).Value=0;
        yield return Wait(()=>P(4).rifleAmmo==0,"client receives empty rifle magazine");
        normalBefore=P(4).normal;int shotsBefore=P(4).rifleShots;Send(4,48);yield return new WaitForSecondsRealtime(.8f);
        Require(P(4).normal==normalBefore&&P(4).rifleShots==shotsBefore&&P(4).selectedAmmo==3,"client held empty rifle neither fires nor falls back");
        Send(4,49);yield return new WaitForSecondsRealtime(.15f);yield return ResetRifleRow();
    }
    static IEnumerator CaptureNormalProjectile(SideScrollNetworkPlayer own)
    {own.GetComponentInChildren<RightFacingAimRig>().SetAimDirection(new Vector2(1,1));yield return new WaitForSecondsRealtime(.24f);SampleFireButtons(own,false,true);yield return new WaitForSecondsRealtime(.1f);ScreenCapture.CaptureScreenshot(folder+"/normal-projectile.png");}
    static IEnumerator CaptureRifle()
    {yield return null;ScreenCapture.CaptureScreenshot(folder+"/rifle-muzzle.png");float until=Time.realtimeSinceStartup+.4f;while(Player(2)!=null&&Player(2).Health==100&&Time.realtimeSinceStartup<until)yield return null;ScreenCapture.CaptureScreenshot(folder+"/rifle-impact.png");yield return new WaitForSecondsRealtime(.035f);ScreenCapture.CaptureScreenshot(folder+"/piercing-rifle.png");}
    static void RifleShot(SideScrollNetworkPlayer p,uint life,Vector2? origin=null,Vector2? direction=null)
    {var aim=p.GetComponentInChildren<RightFacingAimRig>();typeof(SideScrollNetworkPlayer).GetMethod("FireRifleServerRpc",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(p,new object[]{origin??(Vector2)aim.MuzzlePosition,direction??aim.AimDirection,life});}
    static IEnumerator RifleRow(bool reverse=false)
    {Send(0,42,reverse);yield return Wait(()=>Mathf.Abs(P(1).position.x-(reverse?6:-6))<.1f&&Mathf.Abs(P(4).position.x-(reverse?-6:6))<.1f,"piercing row positions applied");yield return new WaitForSecondsRealtime(.3f);}
    static IEnumerator ResetRifleRow()
    {var lives=new uint[4];for(int i=1;i<=4;i++){lives[i-1]=Player(i).LifeVersion;Player(i).ResetForArenaRound();}yield return Wait(()=>All(4,p=>p.hp==100&&p.rifleAmmo==SideScrollNetworkPlayer.RifleCapacity&&p.life>lives[p.role-1]),"rifle lives and magazines reset");yield return new WaitForSecondsRealtime(.3f);}
    static IEnumerator CheckRifle()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.rifleAmmo==SideScrollNetworkPlayer.RifleCapacity),"four peers have rifle ammunition",45);
        Send(1,40);yield return Wait(()=>P(1).selectedAmmo==2,"shotgun still selected through cycle");Send(1,41);yield return Wait(()=>P(1).selectedAmmo==3,"rifle is next selection");Send(1,41);yield return Wait(()=>P(1).selectedAmmo==4,"cycle includes grenade");Send(1,41);yield return Wait(()=>P(1).selectedAmmo==2,"cycle returns to shotgun without normal");
        var match=ArenaObjectiveMatch.Instance;Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");match.roundSeconds=120;Require(match.StartRound(),"start rifle round");yield return Wait(()=>All(4,p=>p.phase==1),"rifle round playing");yield return RifleRow();
        yield return CheckFireTiming();yield return CheckHeldSpecial();yield return RifleRow();
        var host=Player(1);host.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Rifle);host.SetSpecialHeld(true);yield return new WaitForSecondsRealtime(.23f);int count=host.ReceivedRifleShots;RifleShot(host,host.LifeVersion);
        Require(Player(2).Health==100&&Player(3).Health==100&&Player(4).Health==100,"rifle travels rather than hitting instantly");Require(host.RifleRounds==SideScrollNetworkPlayer.RifleCapacity-1&&host.ShotgunRounds==6&&host.NormalAmmo==host.NormalCapacity,"rifle spends independent ammunition");
        RifleShot(host,host.LifeVersion);Require(host.RifleRounds==SideScrollNetworkPlayer.RifleCapacity-1&&host.ReceivedRifleShots==count+1,"server refuses same-frame repeated rifle shot");
        yield return Wait(()=>P(2).hp==50&&P(3).hp==50&&P(4).hp==50,"one bullet penetrates two covers and three players",3);
        yield return new WaitForSecondsRealtime(.7f);Require(All(4,p=>p.role==1||p.hp==50),"one bullet deals damage only once per player");
        yield return ResetRifleRow();Send(1,46);yield return Wait(()=>match.Patrols[0].Health==0&&P(1).rifleAmmo==SideScrollNetworkPlayer.RifleCapacity-1,"rifle hits low patrol when aimed down",3);
        yield return ResetRifleRow();yield return RifleRow();count=host.ReceivedRifleShots;
        RifleShot(host,host.LifeVersion-1);RifleShot(host,host.LifeVersion,new Vector2(float.NaN,0));RifleShot(host,host.LifeVersion,null,Vector2.zero);
        Require(host.RifleRounds==SideScrollNetworkPlayer.RifleCapacity&&host.ReceivedRifleShots==count,"stale-life and invalid rifle requests rejected");
        Send(1,17);yield return new WaitForSecondsRealtime(.4f);Send(1,43);yield return Wait(()=>P(1).rifleAmmo==SideScrollNetworkPlayer.RifleCapacity-1,"selected rifle fires through normal controls");yield return new WaitForSecondsRealtime(.35f);
        yield return ResetRifleRow();yield return RifleRow(true);Send(4,43);yield return Wait(()=>P(1).hp==50&&P(2).hp==50&&P(3).hp==50&&P(4).rifleAmmo==SideScrollNetworkPlayer.RifleCapacity-1,"client rifle pierces to host and other clients",3);
        yield return ResetRifleRow();yield return RifleRow();
        ((NetworkVariable<int>)typeof(SideScrollNetworkPlayer).GetField("rifleRounds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(host)).Value=0;
        float normal=host.NormalAmmo;Send(1,43);yield return new WaitForSecondsRealtime(.5f);
        Require(host.NormalAmmo==normal&&host.RifleActive&&host.FireCursorBlocked,"empty rifle keeps special mode and blocks normal fallback");
        ScreenCapture.CaptureScreenshot(folder+"/empty-rifle-red.png");
        Require(host.ShotgunRounds==6,"empty rifle leaves shotgun ammunition intact");
        Send(1,49);yield return Wait(()=>!host.SpecialHeld&&host.FireWaitRemaining<=0,"release switch delay completed");SampleFireButtons(host,false,true);Require(host.NormalAmmo<normal,"releasing right mouse restores normal fire");
        yield return new WaitForSecondsRealtime(2.3f);Require(host.RifleRounds==0,"rifle does not regenerate during combat");
        var state=match.State.Value;state.End=match.Now+.1;match.State.Value=state;yield return Wait(()=>All(4,p=>p.phase==2),"rifle round finished");RifleShot(host,host.LifeVersion);Require(host.RifleRounds==0,"finished round rejects rifle fire");
        Require(match.ReturnToRoom(),"return after rifle round");yield return Wait(()=>All(4,p=>p.phase==0&&p.rifleAmmo==SideScrollNetworkPlayer.RifleCapacity&&p.ammo==6),"waiting room restores rifle and shotgun magazines");
        ((NetworkVariable<int>)typeof(SideScrollNetworkPlayer).GetField("rifleRounds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(host)).Value=SideScrollNetworkPlayer.RifleCapacity-1;
        yield return Wait(()=>P(1).rifleAmmo==SideScrollNetworkPlayer.RifleCapacity,"waiting room replenishes rifle",4);
    }
}
