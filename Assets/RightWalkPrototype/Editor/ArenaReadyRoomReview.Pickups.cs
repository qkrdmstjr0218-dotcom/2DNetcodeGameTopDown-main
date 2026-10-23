using System.Collections;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using RightWalkPrototype;

public static partial class ArenaReadyRoomReview
{
    static void SetShotgun(int number,int value)
    {((NetworkVariable<int>)typeof(SideScrollNetworkPlayer).GetField(Player(number).InstantShotgunEnabled?"instantShotgunRounds":"shotgunRounds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(Player(number))).Value=value;}
    static IEnumerator CheckPickups()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.pickupsCount==0),"four peers waiting without combat pickups",45);
        var match=ArenaObjectiveMatch.Instance;Require(!match.SpawnShotgunDropServer(Vector2.zero,3),"drops rejected outside combat");
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");match.roundSeconds=120;Require(match.StartRound(),"start pickup round");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.pickupsAvailable==2),"two fixed sources synchronized");
        Send(1,27,true);yield return Wait(()=>P(1).position.x<-7.5f,"owner arrives at west supply");yield return new WaitForSecondsRealtime(.3f);
        Require(Player(1).ShotgunRounds==6&&match.AmmoPickups[0].Available,"full player does not consume pickup");
        ScreenCapture.CaptureScreenshot(folder+"/ammo-pickup.png");yield return new WaitForSecondsRealtime(.3f);
        Send(2,27,true);yield return Wait(()=>P(2).position.x<-7.5f,"second player overlaps same supply");yield return new WaitForSecondsRealtime(.3f);
        SetShotgun(1,0);SetShotgun(2,0);
        yield return Wait(()=>P(1).ammo+P(2).ammo==1&&All(4,p=>p.pickupsAvailable==1),"simultaneous contact grants exactly one one-round pickup");
        Require(Player(1).ShotgunRounds==1&&Player(2).ShotgunRounds==0,"stable server recipient and no duplicate grant");
        Send(1,28);yield return Wait(()=>P(1).position.x>-.5f,"first recipient leaves supply");
        yield return Wait(()=>P(2).ammo==1,"remaining player collects when fixed source respawns",18);
        Require(match.AmmoPickups.Count==2&&!match.AmmoPickups[0].Available,"fixed source returns to cooldown");
        Send(2,28);yield return Wait(()=>P(2).position.x>-.5f,"client moves to one-use drop test");
        Send(1,27,false);yield return Wait(()=>P(1).position.x>7.5f,"host leaves drop location");
        SetShotgun(2,5);Require(match.SpawnShotgunDropServer(new Vector2(0,.55f),3),"server can create future enemy-style one-use drop");
        yield return Wait(()=>P(2).ammo==6&&All(4,p=>p.pickupsCount==2),"one-use drop caps ammunition and disappears for all peers");
        Require(!match.SpawnShotgunDropServer(new Vector2(float.NaN,0),3),"invalid drop position rejected");
        SetShotgun(2,0);Player(2).ReceiveServerDamage(1000,Player(2).LifeVersion);
        Require(match.SpawnShotgunDropServer(new Vector2(0,.55f),3),"drop beside dead player");yield return new WaitForSecondsRealtime(.4f);
        Require(Player(2).IsDead&&Player(2).ShotgunRounds==0&&match.AmmoPickups.Count==3,"dead player cannot collect");
        var state=match.State.Value;state.End=match.Now+.1;match.State.Value=state;
        yield return Wait(()=>All(4,p=>p.phase==2&&p.pickupsCount==0),"match finish clears supplies and drops");
        Require(match.ReturnToRoom(),"return to waiting room");yield return Wait(()=>All(4,p=>p.phase==0&&p.pickupsCount==0&&p.ammo==6),"waiting room resources restored without combat supplies");
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"ready for new round");Require(match.StartRound(),"start second pickup round");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.pickupsAvailable==2&&p.pickupsCount==2),"new round resets both supplies");
    }
}
