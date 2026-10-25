using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    static Vector2 grenadePredicted;static bool grenadePredictionHit;
    static IEnumerator FireGrenadeReview(SideScrollNetworkPlayer own,int action,bool main)
    {
        var a=own.GetComponentInChildren<RightFacingAimRig>();
        if(action==53)a.SetAimTarget((Vector2)own.transform.position+Vector2.down*2);
        if(action==55&&ArenaObjectiveMatch.Instance.Patrols.Count>0)
        {var p=ArenaObjectiveMatch.Instance.PatrolPosition(ArenaObjectiveMatch.Instance.Patrols[0]);own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(p+Vector3.left*3);a.SetAimTarget((Vector2)p+Vector2.up*.45f);}
        if(action==57)a.SetAimTarget((Vector2)own.transform.position+Vector2.up*20);
        own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Grenade);own.SetSpecialHeld(true);yield return new WaitForSecondsRealtime(.25f);
        if(action==55&&ArenaObjectiveMatch.Instance.Patrols.Count>0)a.SetAimTarget((Vector2)ArenaObjectiveMatch.Instance.PatrolPosition(ArenaObjectiveMatch.Instance.Patrols[0])+Vector2.up*.45f);
        yield return null;grenadePredictionHit=own.PredictGrenade(new List<Vector2>(),out grenadePredicted);
        if(main){if(own.GetComponent<ArenaFireTimingCursorReview>()==null)own.gameObject.AddComponent<ArenaFireTimingCursorReview>().Player=own;ScreenCapture.CaptureScreenshot(folder+"/grenade-preview.png");}
        if(action==56)
        {float until=Time.realtimeSinceStartup+.7f;while(Time.realtimeSinceStartup<until){SampleFireButtons(own,true,true);yield return null;}if(main)ScreenCapture.CaptureScreenshot(folder+"/grenade-empty.png");yield break;}
        int bursts=own.ReceivedGrenadeBursts;SampleFireButtons(own,true,true);
        yield return new WaitForSecondsRealtime(.1f);if(main)ScreenCapture.CaptureScreenshot(folder+"/grenade-flight.png");
        float end=Time.realtimeSinceStartup+3;while(own.ReceivedGrenadeBursts==bursts&&Time.realtimeSinceStartup<end)yield return null;
        if(main&&own.ReceivedGrenadeBursts>bursts)
        {
            ScreenCapture.CaptureScreenshot(folder+"/grenade-blast.png");
            yield return new WaitForSecondsRealtime(.1f);ScreenCapture.CaptureScreenshot(folder+"/grenade-wave.png");
            yield return new WaitForSecondsRealtime(.16f);ScreenCapture.CaptureScreenshot(folder+"/grenade-smoke.png");
        }
    }
    static void GrenadeShot(SideScrollNetworkPlayer p,Vector2 origin,Vector2 direction,float speed,uint life)
    {typeof(SideScrollNetworkPlayer).GetMethod("FireGrenadeServerRpc",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(p,new object[]{origin,direction,speed,life});}
    static void SetGrenades(SideScrollNetworkPlayer p,int amount)
    {((NetworkVariable<int>)typeof(SideScrollNetworkPlayer).GetField("grenadeRounds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(p)).Value=amount;}
    static IEnumerator GrenadeLayout(bool reverse=false)
    {Send(0,51,reverse);yield return Wait(()=>All(4,p=>Mathf.Abs(p.position.x-(reverse?(p.role==4?-10:p.role==1?-6:p.role==2?-5:6):(p.role==1?-10:p.role==2?-6:p.role==3?-5:6)))<.12f&&p.grounded),"grenade positions settled");yield return new WaitForSecondsRealtime(.3f);}
    static IEnumerator GrenadeReset()
    {yield return ResetRifleRow();yield return Wait(()=>All(4,p=>p.grenadeAmmo==SideScrollNetworkPlayer.GrenadeCapacity),"all grenade magazines reset");}
    static Bounds GrenadeBounds(int number)=>(Bounds)typeof(SideScrollNetworkPlayer).GetMethod("ServerHitBounds",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(Player(number),null);
    static IEnumerator CheckGrenade()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.grenadeAmmo==SideScrollNetworkPlayer.GrenadeCapacity),"four peers have grenade ammunition",45);
        var host=Player(1);host.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Rifle);host.CycleAmmo(1);Require(host.SelectedAmmo==SideScrollNetworkPlayer.AmmoKind.Grenade,"rifle cycles to grenade");host.CycleAmmo(1);Require(host.SelectedAmmo==SideScrollNetworkPlayer.AmmoKind.Normal,"grenade cycles back to normal");
        Require(ArenaGrenadeBallistics.Speed(1)<ArenaGrenadeBallistics.Speed(10)&&ArenaGrenadeBallistics.Speed(1000)==ArenaGrenadeBallistics.MaximumSpeed,"mouse distance changes speed within cap");
        float Range(float speed){float v=speed/Mathf.Sqrt(2);return v*(v+Mathf.Sqrt(v*v+2*ArenaGrenadeBallistics.Gravity*1.4f))/ArenaGrenadeBallistics.Gravity;}
        float oldRange=Range(4+6*.7f),newRange=Range(ArenaGrenadeBallistics.Speed(6));Require(newRange>oldRange*1.4f,"same on-screen aim distance has greater ballistic reach");System.IO.File.WriteAllText(folder+"/grenade-speed-comparison.txt","distance6 speed "+ArenaGrenadeBallistics.Speed(6)+";45deg range old="+oldRange+" new="+newRange+" max="+ArenaGrenadeBallistics.MaximumSpeed);
        var sample=new Bounds(Vector3.zero,Vector3.zero);
        Require(ArenaGrenadeBallistics.BlastDamage(Vector2.zero,sample,40,10)==40&&ArenaGrenadeBallistics.BlastDamage(Vector2.right,sample,40,10)==25&&ArenaGrenadeBallistics.BlastDamage(Vector2.right*2,sample,40,10)==10&&ArenaGrenadeBallistics.BlastDamage(Vector2.right*2.01f,sample,40,10)==0,"blast damage falls from 40 through 25 to 10 and excludes outside");
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");var match=ArenaObjectiveMatch.Instance;match.roundSeconds=120;Require(match.StartRound(),"start grenade round");yield return Wait(()=>All(4,p=>p.phase==1),"grenade round playing");
        yield return GrenadeLayout();int bursts=host.ReceivedGrenadeBursts;var secondBounds=GrenadeBounds(2);var thirdBounds=GrenadeBounds(3);Send(1,52);yield return Wait(()=>P(1).grenadeAmmo==SideScrollNetworkPlayer.GrenadeCapacity-1,"host actual input spends grenade");
        yield return Wait(()=>P(2).hp<100&&P(3).hp<100,"one impact damages two nearby players",4);Require(P(1).hp==100&&P(4).hp==100,"outside blast radius is unharmed");
        yield return Wait(()=>P(2).grenadePushes>0&&P(3).grenadePushes>0,"both damaged owners receive brief grenade reaction");Require(P(2).grenadeImpulse.x>0&&P(3).grenadeImpulse.x>0&&Mathf.Abs(P(2).grenadeImpulse.magnitude-SideScrollNetworkPlayer.GrenadePushSpeed)<.01f,"grenade reaction points away from explosion and stays small");Require(P(1).grenadePushes==0&&P(4).grenadePushes==0,"outside blast receives no reaction");yield return Wait(()=>!P(2).hitReacting&&!P(3).hitReacting,"brief grenade reaction ends");
        Vector2 point=host.LastGrenadeBurst;int secondHp=100-ArenaGrenadeBallistics.BlastDamage(point,secondBounds,40,10),thirdHp=100-ArenaGrenadeBallistics.BlastDamage(point,thirdBounds,40,10);Require(Player(2).Health==secondHp&&Player(3).Health==thirdHp&&secondHp<thirdHp,"actual multi-target damage follows distance falloff");yield return Wait(()=>All(4,p=>p.hostGrenadeBursts==bursts+1&&Vector2.Distance(p.hostGrenadePoint,point)<.02f),"all peers share one explosion position");Require(P(1).predictedGrenadeHit&&Vector2.Distance(P(1).predictedGrenade,point)<.16f,"predicted first collision matches server explosion");
        yield return new WaitForSecondsRealtime(.4f);Require(Player(2).Health==secondHp&&host.ReceivedGrenadeBursts==bursts+1,"no repeated splash or cluster");
        yield return new WaitForSecondsRealtime(.2f);
        var preview=host.GetComponentInChildren<RightFacingShotPreview>();
        Require(((IList)typeof(RightFacingShotPreview).GetField("grenadeBlasts",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(preview)).Count==0,"expanded blast visuals release after their lifetime");
        yield return GrenadeReset();yield return GrenadeLayout(true);Send(4,52);yield return Wait(()=>P(1).hp<100&&P(2).hp<100&&P(4).grenadeAmmo==2,"client grenade damages host and another client",4);
        yield return GrenadeReset();yield return GrenadeLayout();int selfPushes=host.ReceivedGrenadePushes;Send(1,53);yield return Wait(()=>P(1).hp<100,"close explosion damages shooter",4);Require(host.ReceivedGrenadePushes==selfPushes+1,"self explosion also gives brief reaction");
        yield return GrenadeReset();Send(0,54);yield return Wait(()=>Mathf.Abs(P(1).position.x+6)<.12f&&Mathf.Abs(P(2).position.x+2.2f)<.12f&&All(4,p=>p.grounded),"cover test positions settled");yield return new WaitForSecondsRealtime(.3f);bursts=host.ReceivedGrenadeBursts;int protectedPushes=P(2).grenadePushes;Send(1,52);
        yield return Wait(()=>host.ReceivedGrenadeBursts>bursts,"grenade hits solid cover",4);point=host.LastGrenadeBurst;
        Require(Vector2.Distance(point,((Bounds)typeof(SideScrollNetworkPlayer).GetMethod("ServerHitBounds",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(Player(2),null)).ClosestPoint(new Vector3(point.x,point.y,0)))<ArenaGrenadeBallistics.BlastRadius,"covered player is geometrically in splash radius");System.IO.File.WriteAllText(folder+"/cover-debug.txt",point+" hp "+Player(1).Health+"/"+Player(2).Health+" blocked="+ArenaGrenadeBallistics.WorldTrace(point,Player(2).transform.position+Vector3.up*1.3f,0,out _));yield return Wait(()=>P(2).hp<100&&P(2).grenadePushes>protectedPushes,"blast damages and briefly pushes player behind solid cover");Require(Player(1).Health<100&&Player(2).Health>=60,"covered player receives distance-scaled splash without wall shielding");
        yield return GrenadeReset();var patrol=match.Patrols[0];patrol.Health=4;patrol.Generation++;patrol.Speed=0;patrol.RespawnAt=0;match.Patrols[0]=patrol;yield return new WaitForSecondsRealtime(.2f);Send(1,55);yield return Wait(()=>host.GrenadeRounds==2&&match.Patrols[0].Health<4&&match.Patrols[0].Health>=0,"grenade splash damages patrol enemy",4);
        yield return GrenadeReset();yield return GrenadeLayout();SetGrenades(host,0);float normal=host.NormalAmmo;int shots=host.ReceivedGrenadeShots;Send(1,56);yield return new WaitForSecondsRealtime(1.1f);Require(host.GrenadeActive&&host.ActiveAmmoEmpty&&host.ReceivedGrenadeShots==shots&&host.NormalAmmo==normal,"empty held grenade cannot fire or fall back");
        Send(1,49);yield return Wait(()=>!host.SpecialHeld&&host.FireWaitRemaining<=0,"grenade release switch completes");SampleFireButtons(host,false,true);Require(host.NormalAmmo<normal,"releasing grenade restores normal fire");
        yield return GrenadeReset();yield return GrenadeLayout();host.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Grenade);host.SetSpecialHeld(true);yield return new WaitForSecondsRealtime(.25f);var a=host.GetComponentInChildren<RightFacingAimRig>();shots=host.ReceivedGrenadeShots;
        GrenadeShot(host,a.MuzzlePosition,a.AimDirection,10,host.LifeVersion-1);GrenadeShot(host,a.MuzzlePosition,a.AimDirection,float.NaN,host.LifeVersion);GrenadeShot(host,a.MuzzlePosition,a.AimDirection,100,host.LifeVersion);Require(host.GrenadeRounds==SideScrollNetworkPlayer.GrenadeCapacity&&host.ReceivedGrenadeShots==shots,"stale life and invalid grenade speed rejected");
        GrenadeShot(host,a.MuzzlePosition,a.AimDirection,10,host.LifeVersion);GrenadeShot(host,a.MuzzlePosition,a.AimDirection,10,host.LifeVersion);Require(host.GrenadeRounds==2&&host.ReceivedGrenadeShots==shots+1,"server grenade interval rejects duplicate request");yield return new WaitForSecondsRealtime(1.2f);
        yield return GrenadeReset();yield return GrenadeLayout();bursts=host.ReceivedGrenadeBursts;host.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Grenade);host.SetSpecialHeld(true);yield return new WaitForSecondsRealtime(.25f);GrenadeShot(host,host.GetComponentInChildren<RightFacingAimRig>().MuzzlePosition,Vector2.up,14,host.LifeVersion);Require(host.GrenadeRounds==2,"upward grenade launched before finish");var state=match.State.Value;state.End=match.Now+.01;match.State.Value=state;yield return Wait(()=>All(4,p=>p.phase==2),"grenade round ends");yield return new WaitForSecondsRealtime(.4f);Require(host.ReceivedGrenadeBursts==bursts,"finished match cancels in-flight grenade");
        Require(match.ReturnToRoom(),"return after grenade round");yield return Wait(()=>All(4,p=>p.phase==0&&p.grenadeAmmo==SideScrollNetworkPlayer.GrenadeCapacity),"waiting room restores grenade magazines");SetGrenades(host,2);yield return Wait(()=>P(1).grenadeAmmo==SideScrollNetworkPlayer.GrenadeCapacity,"waiting room replenishes grenades",4);
    }
}
