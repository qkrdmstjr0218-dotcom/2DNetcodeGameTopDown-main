using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Health Pickups")]
    public static void BeginHealth(){BeginFoundry();File.WriteAllText(folder+"/health.txt","health");}
    static IEnumerator CheckHealthPickups()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.healthCount==0),"four peers waiting without health packs",45);
        var m=ArenaObjectiveMatch.Instance;Require(!m.SpawnHealthDropServer(Vector2.zero,25),"health drops rejected outside combat");
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");m.roundSeconds=180;Require(m.StartRound(),"start health pickup round");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.healthCount==2&&p.healthAvailable==2&&p.pickupsCount==3),"two health packs and unchanged ammunition supplies synchronized");
        Send(1,79);yield return Wait(()=>Mathf.Abs(P(1).position.x+18)<.15f&&P(1).grounded,"host arrives at west health pack");yield return new WaitForSecondsRealtime(.4f);
        Require(Player(1).Health==100&&m.HealthPickups[0].Available,"full health leaves pack untouched");ScreenCapture.CaptureScreenshot(folder+"/health-pickup.png");
        var host=Player(1);uint life=host.LifeVersion;int ammo=host.ShotgunRounds;
        host.ReceiveServerDamage(60,life);yield return Wait(()=>P(1).hp==65&&All(4,p=>p.healthAvailable==1),"host heals exactly twenty-five and pickup disappears for all peers");
        Require(host.LifeVersion==life&&host.ShotgunRounds==ammo&&host.Stamina==100,"healing preserves life generation ammunition and stamina");
        Require(m.HealthPickups[0].Respawn==20&&m.HealthPickups[0].ReadyAt>m.Now+18,"fixed pack uses twenty-second respawn");
        Send(1,80);yield return Wait(()=>P(1).position.x> -16.2f,"host leaves fixed pack");
        var client=Player(2);client.ReceiveServerDamage(10,client.LifeVersion);yield return Wait(()=>P(2).hp==90,"client receives injury before healing test");Require(m.SpawnHealthDropServer(new Vector2(0,4.15f),25),"one-use health drop created through shared path");Send(2,78);
        yield return Wait(()=>P(2).hp==100&&Mathf.Abs(P(2).position.x)<.15f&&m.HealthPickups.Count==2&&All(4,p=>p.healthCount==2),"client heals only to maximum and one-use drop is removed");
        host.ReceiveServerDamage(15,host.LifeVersion);client.ReceiveServerDamage(50,client.LifeVersion);Send(1,78);yield return Wait(()=>P(1).hp==50&&P(2).hp==50&&Mathf.Abs(P(1).position.x)<.15f&&P(1).grounded,"both injured players overlap drop position");
        Require(m.SpawnHealthDropServer(new Vector2(0,4.15f),25),"simultaneous pickup drop spawned");yield return Wait(()=>P(1).hp+P(2).hp==125&&All(4,p=>p.healthCount==2),"simultaneous contact grants one twenty-five health transaction");
        Require(host.Health==75&&client.Health==50,"server chooses one stable recipient");
        Require(host.GrantHealthServer(25,host.LifeVersion-1)==0&&host.GrantHealthServer(-1,host.LifeVersion)==0,"old-life and invalid healing rejected");
        Require(!m.SpawnHealthDropServer(new Vector2(float.NaN,0),25),"invalid drop position rejected");
        Send(1,80);yield return Wait(()=>P(1).position.x< -15,"host leaves one-use drop position");
        client.ReceiveServerDamage(1000,client.LifeVersion);Require(client.GrantHealthServer(25,client.LifeVersion)==0,"health does not resurrect dead player");Require(m.SpawnHealthDropServer(new Vector2(0,4.15f),25),"health drop beside corpse");yield return new WaitForSecondsRealtime(.4f);
        Require(P(2).hp==0&&m.HealthPickups.Count==3&&m.HealthPickups[2].Available,"corpse cannot consume health pack");
        yield return Wait(()=>All(4,p=>p.healthAvailable==3),"fixed health pack respawns after twenty seconds",22);
        Require(host.Health==75,"absent host receives no automatic healing");
        var state=m.State.Value;state.End=m.Now+.05;m.State.Value=state;yield return Wait(()=>All(4,p=>p.phase==2&&p.healthCount==0),"finish clears fixed and dropped health packs");
        Require(!m.SpawnHealthDropServer(Vector2.zero,25)&&host.GrantHealthServer(25,host.LifeVersion)==0,"finished round rejects healing and drops");
        Require(m.ReturnToRoom(),"return after healing test");yield return Wait(()=>All(4,p=>p.phase==0&&p.healthCount==0&&p.hp==100),"ready room restores health normally without packs");
    }
}
