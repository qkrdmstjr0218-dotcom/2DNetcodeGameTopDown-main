using System.Collections;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using TopDownShooter.Networking;

public sealed class PlaygroundTestBoot : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var args=System.Environment.GetCommandLineArgs();
        if(args.Contains("--sf-smoke") || args.Contains("--sf-client-test") || args.Contains("--sf-host-test")) new GameObject("Automated playground check").AddComponent<PlaygroundTestBoot>();
    }
    private IEnumerator Start()
    {
        bool client=System.Environment.GetCommandLineArgs().Contains("--sf-client-test");
        yield return null;
        Object.FindFirstObjectByType<PlaygroundSessionUI>().StartLocal(!client, 7781);
        float deadline=Time.realtimeSinceStartup+20;
        while((NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null) && Time.realtimeSinceStartup<deadline) yield return null;
        if(NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null)
        { Debug.LogError("Test connection timed out."); Application.Quit(2); yield break; }
        yield return new WaitForSecondsRealtime(1);
        if(System.Environment.GetCommandLineArgs().Contains("--sf-host-test"))
        {
            deadline=Time.realtimeSinceStartup+35;
            bool upgradeApplied=false;
            while(Time.realtimeSinceStartup<deadline)
            {
                var targets=Object.FindObjectsByType<PracticeTarget>(FindObjectsSortMode.None);
                var peer=NetworkPlayerController.ActivePlayers.FirstOrDefault(p=>!p.IsOwner);
                if(peer!=null && !upgradeApplied)
                    upgradeApplied=peer.ApplyWeaponUpgrade(2,damageBonus:2,cooldownMultiplier:0.5f,unlimitedPiercing:true);
                if(peer!=null && targets.Length==3 && targets.All(t=>t.Hits.Value==1))
                {
                    bool ok=peer.SelectedWeapon==2 && Vector2.Distance(peer.transform.position,new Vector2(0,-2))<0.2f
                        && peer.CurrentStats.Damage==5 && peer.CurrentStats.Targets==0
                        && NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>().GetWeaponStats(2).Damage==3;
                    System.IO.File.WriteAllText("Temp/playground-host.txt",(ok?"PASS":"FAIL")+": two players; remote weapon="+peer.SelectedWeapon+", position="+peer.transform.position+", hits="+string.Join(",",targets.Select(t=>t.Hits.Value)));
                    yield return new WaitForSecondsRealtime(5);
                    Application.Quit(ok?0:5); yield break;
                }
                yield return null;
            }
            System.IO.File.WriteAllText("Temp/playground-host.txt","FAIL: client test timed out"); Application.Quit(6);
        }
        else if(!client)
        {
            PlaygroundSmoke.Main();
            yield return new WaitForSecondsRealtime(12);
            string report=System.IO.File.Exists("Temp/playground-smoke.txt")?System.IO.File.ReadAllText("Temp/playground-smoke.txt"):"FAIL: no smoke result";
            Debug.Log(report); Application.Quit(report.StartsWith("PASS")?0:3);
        }
        else
        {
            var p=NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>();
            var sync=p.GetComponent<Unity.Netcode.Components.NetworkTransform>();
            sync.Teleport(new Vector3(0,-2,0),Quaternion.identity,Vector3.one);
            p.SelectWeapon(2);
            yield return new WaitForSecondsRealtime(1);
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            typeof(NetworkPlayerController).GetField("localAim",flags).SetValue(p,Vector2.right);
            p.GetComponent<OperatorAimVisual>().Pose(Vector2.right,p.CurrentWeapon,0,p.OwnerClientId);
            typeof(NetworkPlayerController).GetMethod("HandleAttack",flags).Invoke(p,null);
            yield return new WaitForSecondsRealtime(1);
            var targets=Object.FindObjectsByType<PracticeTarget>(FindObjectsSortMode.None);
            bool success=targets.Length==3 && targets.All(t=>t.Hits.Value>0) && NetworkPlayerController.ActivePlayers.Count==2
                && p.CurrentStats.Damage==5 && p.CurrentStats.Targets==0 && Mathf.Abs(p.CurrentStats.Interval-0.25f)<0.001f;
            System.IO.File.WriteAllText("Temp/playground-client.txt",(success?"PASS":"FAIL")+": client connected, players="+NetworkPlayerController.ActivePlayers.Count+", target hits="+string.Join(",",targets.Select(t=>t.Hits.Value))+", upgrade damage="+p.CurrentStats.Damage+", penetration="+p.CurrentStats.Targets+", interval="+p.CurrentStats.Interval);
            yield return new WaitForSecondsRealtime(2);
            Application.Quit(success?0:4);
        }
    }
}
