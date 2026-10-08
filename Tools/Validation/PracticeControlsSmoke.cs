using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;
using TopDownShooter.Networking;
public static class PracticeControlsSmoke
{
    private static void Check(bool pass,string name)
    { if(!pass) { System.IO.File.WriteAllText("Temp/practice-controls.txt","FAIL: "+name); throw new Exception(name); } }
    public static string Main()
    {
        UnityEngine.Object.FindFirstObjectByType<PlaygroundSessionUI>().StartCoroutine(Run());
        return "Started one-wave/stop/upgrade validation";
    }
    private static IEnumerator Run()
    {
        var game=NetworkGameManager.Instance;
        game.BeginPracticeWaves();
        yield return new WaitForSecondsRealtime(8);
        var enemies=UnityEngine.Object.FindObjectsByType<NetworkEnemy>(FindObjectsSortMode.None).Where(e=>e.IsSpawned).ToArray();
        Check(enemies.Length==5 && game.AliveEnemyCount==5 && game.PendingSpawns==0,"Live enemy count");
        Check(Mathf.Abs(enemies[0].GetComponent<CircleCollider2D>().radius*enemies[0].transform.lossyScale.x-0.2925f)<0.01f,"Enlarged hitbox");
        foreach(var enemy in enemies) enemy.ReceiveDamage(10000,0);
        yield return new WaitForSecondsRealtime(9);
        Check(!game.PracticeWaveActive && game.AliveEnemyCount==0 && EnemySpawner.Instance.AliveCount==0,"No automatic next wave");
        game.BeginPracticeWaves();
        yield return new WaitForSecondsRealtime(0.2f);
        game.StopPracticeWaves();
        yield return new WaitForSecondsRealtime(6);
        Check(game.AliveEnemyCount==0 && !game.PracticeWaveActive && game.PendingSpawns==0,"Stop during countdown");
        game.BeginPracticeWaves();
        yield return new WaitForSecondsRealtime(5.7f);
        Check(game.AliveEnemyCount>0,"Spawn before stop");
        game.StopPracticeWaves();
        yield return new WaitForSecondsRealtime(2);
        Check(game.AliveEnemyCount==0 && EnemySpawner.Instance.AliveCount==0 && game.PendingSpawns==0,"Stop nested spawn and clear existing enemies");
        game.BeginPracticeWaves();
        yield return new WaitForSecondsRealtime(8);
        enemies=UnityEngine.Object.FindObjectsByType<NetworkEnemy>(FindObjectsSortMode.None).Where(e=>e.IsSpawned).ToArray();
        var player=NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>();
        for(int i=0;i<enemies.Length;i++)
        {
            enemies[i].GetComponent<EnemyAI>().enabled=false;
            var body=enemies[i].GetComponent<Rigidbody2D>(); body.linearVelocity=Vector2.zero; body.position=new Vector2(2+i*1.5f,-5);
        }
        Check(player.ApplyWeaponUpgrade(2,damageBonus:100,cooldownMultiplier:0.5f,unlimitedPiercing:true),"Server upgrade application");
        player.SelectWeapon(2);
        Check(player.CurrentStats.Damage==103 && player.CurrentStats.Targets==0 && Mathf.Abs(player.CurrentStats.Interval-0.25f)<0.001f,"Runtime upgrade stats");
        Check(player.CurrentWeapon.damage==3 && player.CurrentWeapon.targets==3,"Shared definition unchanged");
        player.GetComponent<Rigidbody2D>().position=new Vector2(0,-5);
        player.GetComponent<NetworkHealth>().ResetState();
        yield return new WaitForFixedUpdate(); // Physics positions must reach transforms before choosing a muzzle.
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        typeof(NetworkPlayerController).GetField("localAim",flags).SetValue(player,Vector2.right);
        player.GetComponent<OperatorAimVisual>().Pose(Vector2.right,player.CurrentWeapon,0,player.OwnerClientId);
        typeof(NetworkPlayerController).GetMethod("HandleAttack",flags).Invoke(player,null);
        yield return new WaitForSecondsRealtime(0.5f);
        Check(enemies.All(e=>!e.IsSpawned),"Unlimited piercing through five enemies: survivors="+enemies.Count(e=>e.IsSpawned)+", shots="+player.ShotCount);
        game.StopPracticeWaves();
        System.IO.File.WriteAllText("Temp/practice-controls.txt","PASS: enlarged enemy collider, live count, one wave only, stop during countdown/spawning, clear/reset, per-player upgrade base separation, five-enemy unlimited penetration.");
        Debug.Log("PASS: practice controls and upgrades");
    }
}
