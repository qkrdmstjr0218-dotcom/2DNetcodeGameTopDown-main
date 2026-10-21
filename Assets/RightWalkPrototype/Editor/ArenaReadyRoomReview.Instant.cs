using System.Collections;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    static float[] ReadInstantAngles(SideScrollNetworkPlayer player)
    {
        if(player==null)return System.Array.Empty<float>();
        var preview=player.GetComponentInChildren<RightFacingShotPreview>();
        var traces=(IList)typeof(RightFacingShotPreview).GetField("instantTraces",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(preview);
        var angles=new float[traces.Count];
        for(int i=0;i<traces.Count;i++){var line=(LineRenderer)traces[i].GetType().GetField("Visual").GetValue(traces[i]);var d=line.GetPosition(1)-line.GetPosition(0);angles[i]=Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;}
        return angles;
    }
    static bool SameAngles(float[] a,float[] b)
    {if(a==null||b==null||a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(Mathf.Abs(a[i]-b[i])>.01f)return false;return true;}
    static IEnumerator CaptureInstant(){yield return null;ScreenCapture.CaptureScreenshot(folder+"/instant-shotgun.png");}
    static void InstantShot(SideScrollNetworkPlayer player,uint generation)
    {var aim=player.GetComponentInChildren<RightFacingAimRig>();typeof(SideScrollNetworkPlayer).GetMethod("FireInstantShotgunServerRpc",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(player,new object[]{(Vector2)aim.MuzzlePosition,aim.AimDirection,generation});}
    static IEnumerator PlaceInstant(bool swap=false)
    {Send(0,35,swap);yield return Wait(()=>Mathf.Abs(P(1).position.x-(swap?-6:-10))<.1f&&Mathf.Abs(P(2).position.x-(swap?-10:-6))<.1f,"comparison positions applied");yield return new WaitForSecondsRealtime(.3f);yield return PrepareInstant();}
    static IEnumerator PrepareInstant()
    {var host=Player(1);host.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.InstantShotgun);host.SetSpecialHeld(true);yield return new WaitForSecondsRealtime(SideScrollNetworkPlayer.WeaponSwitchSeconds+.06f);}
    static IEnumerator ResetInstantPair()
    {uint life=Player(2).LifeVersion;Player(1).ResetForArenaRound();Player(2).ResetForArenaRound();yield return Wait(()=>P(1).hp==100&&P(2).hp==100&&P(2).life>life,"comparison lives reset");yield return new WaitForSecondsRealtime(.3f);}
    static IEnumerator CheckInstant()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.instantAmmo==6),"four peers have independent trial ammunition",45);
        Require(All(4,p=>p.selectedAmmo==2),"default selection is the current instant shotgun");
        Send(1,40);yield return Wait(()=>P(1).selectedAmmo==2,"cycle selects current shotgun");Send(1,41);yield return Wait(()=>P(1).selectedAmmo==3,"cycle includes rifle");Send(1,41);yield return Wait(()=>P(1).selectedAmmo==4,"cycle includes grenade");Send(1,41);yield return Wait(()=>P(1).selectedAmmo==0,"cycle returns to normal ammunition");
        var match=ArenaObjectiveMatch.Instance;Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");match.roundSeconds=120;Require(match.StartRound(),"start comparison round");yield return Wait(()=>All(4,p=>p.phase==1),"comparison round playing");
        yield return PlaceInstant();
        var host=Player(1);int shots=host.ReceivedInstantBlasts;InstantShot(host,host.LifeVersion);
        Require(Player(2).Health==60&&host.InstantShotgunRounds==5&&host.FlyingShotgunRounds==6&&host.NormalAmmo==host.NormalCapacity,"instant damage occurs in server call without changing original ammunition");
        var firstAngles=ReadInstantAngles(host);Require(firstAngles.Length==5,"instant renders five complete paths");
        for(int i=0;i<firstAngles.Length;i++)Require(Mathf.Abs(firstAngles[i])<=6.01f&&Mathf.Abs(firstAngles[i]-Mathf.Lerp(-6,6,i/4f))<=.81f,"jitter remains in original cone pellet "+i);
        yield return new WaitForSecondsRealtime(.1f);Require(ReadInstantAngles(host).Length==5,"trace remains visible beyond original 0.09 seconds");ScreenCapture.CaptureScreenshot(folder+"/instant-linger.png");
        yield return Wait(()=>All(4,p=>p.hostInstantCount==shots+1&&SameAngles(p.instantAngles,firstAngles)),"all peers render identical server-randomized directions");
        InstantShot(host,host.LifeVersion);Require(host.InstantShotgunRounds==5&&host.ReceivedInstantBlasts==shots+1,"instant fire respects shotgun cooldown and cannot double-hit");
        yield return Wait(()=>P(2).hp==60&&P(1).instantAmmo==5,"instant hit synchronized to participant");
        yield return ResetInstantPair();yield return PlaceInstant();
        var aim=host.GetComponentInChildren<RightFacingAimRig>();typeof(SideScrollNetworkPlayer).GetMethod("FireShotgunServerRpc",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(host,new object[]{(Vector2)aim.MuzzlePosition,aim.AimDirection});
        Require(Player(2).Health==100&&host.FlyingShotgunRounds==6&&host.InstantShotgunRounds==6,"legacy flying RPC disabled in current game");
        yield return new WaitForSecondsRealtime(.3f);Require(Player(2).Health==100,"legacy request cannot spend hidden ammunition");
        yield return ResetInstantPair();yield return PlaceInstant(true);
        Send(2,36);yield return Wait(()=>P(1).hp==60&&P(2).instantAmmo==5,"participant can fire instant shotgun at host");
        yield return ResetInstantPair();Send(0,39);yield return Wait(()=>Mathf.Abs(P(1).position.x+6)<.1f&&Mathf.Abs(P(2).position.x+2)<.1f,"wall comparison positions applied");yield return new WaitForSecondsRealtime(.3f);
        yield return PrepareInstant();InstantShot(host,host.LifeVersion);Require(host.InstantShotgunRounds==5&&Player(2).Health==100,"solid cover stops instant pellets");yield return new WaitForSecondsRealtime(.2f);Require(Player(2).Health==100,"no delayed damage through cover");
        yield return ResetInstantPair();yield return PlaceInstant();
        shots=host.ReceivedInstantBlasts;InstantShot(host,host.LifeVersion-1);Require(host.InstantShotgunRounds==6&&host.ReceivedInstantBlasts==shots,"old-life instant request rejected");
        Send(1,17);yield return new WaitForSecondsRealtime(.4f);Send(1,36);yield return Wait(()=>P(1).instantAmmo==5&&P(1).hostInstantCount>shots,"selected trial weapon consumes own rounds");Require(!SameAngles(firstAngles,P(1).instantAngles),"successive trial shots have different spread");yield return new WaitForSecondsRealtime(.3f);
        ((NetworkVariable<int>)typeof(SideScrollNetworkPlayer).GetField("instantShotgunRounds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(host)).Value=0;
        yield return new WaitForSecondsRealtime(.8f);float normal=host.NormalAmmo;Send(1,36);yield return new WaitForSecondsRealtime(.5f);Require(host.NormalAmmo==normal&&host.InstantShotgunActive&&host.FireCursorBlocked,"empty shotgun blocks fire without normal fallback");Require(host.FlyingShotgunRounds==6,"fallback does not consume archived flying ammunition");
        var state=match.State.Value;state.End=match.Now+.1;match.State.Value=state;yield return Wait(()=>All(4,p=>p.phase==2),"comparison round finished");int rounds=host.InstantShotgunRounds;InstantShot(host,host.LifeVersion);Require(host.InstantShotgunRounds==rounds,"finished match refuses trial fire");
        Require(match.ReturnToRoom(),"return from comparison");yield return Wait(()=>All(4,p=>p.phase==0&&p.instantAmmo==6&&p.ammo==6),"both special magazines reset in waiting room");
        ((NetworkVariable<int>)typeof(SideScrollNetworkPlayer).GetField("instantShotgunRounds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(host)).Value=5;
        yield return Wait(()=>P(1).instantAmmo==6&&P(1).ammo==6,"waiting room independently replenishes trial ammunition",4);
    }
}
