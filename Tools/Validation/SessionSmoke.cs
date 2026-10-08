using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using TopDownShooter.Networking;
public static class SessionSmoke
{
    public static string Main()
    {
        UnityEngine.Object.FindFirstObjectByType<PlaygroundSessionUI>().StartCoroutine(Run());
        return "Started session/wave validation";
    }
    private static IEnumerator Run()
    {
        var ui=UnityEngine.Object.FindFirstObjectByType<PlaygroundSessionUI>();
        NetworkManager.Singleton.Shutdown();
        yield return new WaitForSecondsRealtime(1);
        ui.StartLocal(true);
        yield return new WaitForSecondsRealtime(1);
        if(NetworkPlayerController.ActivePlayers.Count!=1) throw new Exception("Reconnect player count");
        var game=NetworkGameManager.Instance;
        game.BeginPracticeWaves();
        yield return new WaitForSecondsRealtime(9);
        if(EnemySpawner.Instance.AliveCount<1) throw new Exception("Solo wave did not spawn");
        var hp=NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkHealth>();
        hp.ApplyDamage(10000);
        yield return new WaitForSecondsRealtime(0.2f);
        game.RestartGame();
        yield return new WaitForSecondsRealtime(4);
        if(hp.IsDowned.Value || hp.CurrentHealth.Value!=hp.MaxHealth) throw new Exception("Solo restart health");
        NetworkManager.Singleton.Shutdown();
        yield return new WaitForSecondsRealtime(1);
        ui.StartLocal(true);
        yield return new WaitForSecondsRealtime(1);
        if(NetworkPlayerController.ActivePlayers.Count!=1 || EnemySpawner.Instance.AliveCount!=0) throw new Exception("Clean practice reconnect");
        System.IO.File.WriteAllText("Temp/playground-session.txt","PASS: leave/reconnect, solo wave spawn, down/restart, clean practice reset.");
        Debug.Log("PASS: session/wave validation");
    }
}
