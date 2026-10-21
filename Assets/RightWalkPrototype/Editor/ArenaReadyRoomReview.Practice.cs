using System.Collections;
using UnityEngine;
using RightWalkPrototype;

public static partial class ArenaReadyRoomReview
{
    static IEnumerator CheckPractice()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.hp==100),"four practice peers",45);
        var match=ArenaObjectiveMatch.Instance;
        Require(ArenaObjectiveMatch.PracticeRoom&&!ArenaObjectiveMatch.CombatBlocked,"waiting permits combat");
        Require(ArenaObjectiveMatch.RoomUIContains(new Vector2(Screen.width*.5f,45))
            &&ArenaObjectiveMatch.RoomUIContains(new Vector2(Screen.width-80,Screen.height-50))
            &&!ArenaObjectiveMatch.RoomUIContains(new Vector2(Screen.width*.5f,Screen.height*.5f)),"ready and exit controls block fire only under UI");
        Send(1,26);yield return Wait(()=>P(1).position.x<92&&P(1).position.y<.15f,"host crosses flat ledge from side");yield return new WaitForSecondsRealtime(.8f);
        Send(2,26);yield return Wait(()=>P(2).position.x>108&&P(2).position.y<.15f,"client crosses flat ledge from side");yield return new WaitForSecondsRealtime(.8f);
        yield return CheckLedges();
        Send(0,25);yield return new WaitForSecondsRealtime(.6f);
        int before=Player(1).ReceivedShots;Send(1,23);
        yield return Wait(()=>Player(2).Health<100,"host practice bullet hits client");Send(1,16);
        Require(Player(1).ReceivedShots>before&&Player(1).NormalAmmo<Player(1).NormalCapacity,"practice consumes ammunition");
        Player(2).ReceiveServerDamage(1000,Player(2).LifeVersion);Player(2).ReceiveServerDamage(1000,Player(2).LifeVersion);
        yield return Wait(()=>P(2).hp==1&&!Player(2).IsDead,"repeated lethal damage clamps at one HP on client");
        Player(1).ReceiveServerDamage(1000,Player(1).LifeVersion);Require(Player(1).Health==1&&!Player(1).IsDead,"host also cannot die in practice");
        Send(2,20);yield return Wait(()=>Player(2).ShotgunRounds<6,"client practice shotgun accepted");Send(2,21);
        yield return Wait(()=>P(2).ammo==6,"waiting shotgun replenishes automatically",5);
        yield return Wait(()=>Player(1).NormalAmmo==Player(1).NormalCapacity,"normal ammunition recharges in waiting room",8);
        Send(1,24);yield return Wait(()=>P(1).energy<92&&P(1).worldStamina,"owner overhead stamina appears during sprint");
        ScreenCapture.CaptureScreenshot(folder+"/practice-stamina.png");yield return new WaitForSecondsRealtime(.3f);
        yield return Wait(()=>P(1).energy==100&&!P(1).worldStamina,"overhead stamina hides when fully recovered",6);
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"practice does not prevent ready flags");
        Require(match.StartRound(),"host starts after practice");
        Require(ArenaObjectiveMatch.TransitionActive&&ArenaObjectiveMatch.CombatBlocked&&!Player(1).WorldStaminaVisible,"transition hides world HUD and blocks combat");
        yield return null;ScreenCapture.CaptureScreenshot(folder+"/transition-clean.png");yield return new WaitForSecondsRealtime(.3f);
        yield return Wait(()=>All(4,p=>p.phase==5&&p.hp==100&&p.ammo==6&&p.normal==p.normalCapacity),"arrival resets practice health and both ammunition types");
        int count=Player(1).ReceivedShots;Send(1,18);yield return new WaitForSecondsRealtime(.3f);Require(Player(1).ReceivedShots==count,"countdown still blocks direct firing");
        yield return Wait(()=>All(4,p=>p.phase==1),"all peers begin combat");
        Player(2).ReceiveServerDamage(1000,Player(2).LifeVersion);yield return Wait(()=>P(2).hp==0,"combat restores lethal damage");
        Player(1).GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(-1,.04f));yield return new WaitForSecondsRealtime(.3f);
        Send(1,20);yield return Wait(()=>Player(1).ShotgunRounds<6,"combat shotgun spent");Send(1,21);
        int rounds=Player(1).ShotgunRounds;yield return new WaitForSecondsRealtime(2.5f);Require(Player(1).ShotgunRounds==rounds,"combat does not receive practice supply");
        var state=match.State.Value;state.End=match.Now+.1;match.State.Value=state;
        yield return Wait(()=>All(4,p=>p.phase==2),"match ends");Require(match.ReturnToRoom(),"host returns to practice room");
        yield return Wait(()=>All(4,p=>p.phase==0&&p.hp==100&&p.ammo==6&&p.normal==p.normalCapacity&&p.mask==0),"return restores practice resources and readiness");
    }
}
