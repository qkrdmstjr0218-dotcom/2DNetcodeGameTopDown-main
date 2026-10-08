using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unity.Netcode;
using TopDownShooter.Networking;

public static class PlaygroundSmoke
{
    private static void Check(bool condition,string name) { if(!condition) { System.IO.File.WriteAllText("Temp/playground-smoke.txt", "FAIL: "+name); throw new Exception(name); } }
    public static string Main() { NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>().StartCoroutine(Run()); return "Started smoke coroutine"; }
    private static IEnumerator Run()
    {
        var player=NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>();
        var body=player.GetComponent<Rigidbody2D>();
        var keyboard=Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
        var mouse=Mouse.current ?? InputSystem.AddDevice<Mouse>();
        try
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));
            yield return new WaitForSecondsRealtime(300 / 1000f);
            float walk=body.linearVelocity.magnitude;
            Check(Mathf.Abs(walk-5)<0.1f,"Walk speed / input: "+walk+", focused="+Application.isFocused+", move="+player.MoveInput);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D,Key.W));
            yield return new WaitForSecondsRealtime(100 / 1000f);
            Check(Mathf.Abs(body.linearVelocity.magnitude-5)<0.1f,"Diagonal normalization");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D,Key.LeftShift));
            yield return new WaitForSecondsRealtime(100 / 1000f);
            float sprint=body.linearVelocity.magnitude;
            Check(Mathf.Abs(sprint-8)<0.1f,"Sprint speed");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return new WaitForSecondsRealtime(100 / 1000f);
            Check(body.linearVelocity.sqrMagnitude<0.001f,"Immediate stop");
            body.position=new Vector2(-1,1);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D,Key.W));
            yield return new WaitForSecondsRealtime(650 / 1000f);
            Check(body.position.y<2.6f && body.position.x>0.8f,"Wall collision and sliding");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return new WaitForSecondsRealtime(70 / 1000f);
            body.position=new Vector2(0,-2);
            var targets=UnityEngine.Object.FindObjectsByType<PracticeTarget>(FindObjectsSortMode.None).OrderBy(t=>t.transform.position.x).ToArray();
            foreach(var target in targets) { target.Hits.Value=0; target.DamageTaken.Value=0; }
            player.SelectWeapon(0);
            yield return new WaitForSecondsRealtime(750 / 1000f);
            Vector2 screen=Camera.main.WorldToScreenPoint(new Vector3(7,-2,0));
            InputSystem.QueueStateEvent(mouse,new MouseState { position=screen, buttons=1 });
            uint before=player.ShotCount;
            yield return new WaitForSecondsRealtime(500 / 1000f);
            InputSystem.QueueStateEvent(mouse,new MouseState { position=screen });
            yield return new WaitForSecondsRealtime(350 / 1000f);
            uint autoShots=player.ShotCount-before;
            Check(autoShots>=3,"Holding LMB repeats shots");
            Check(targets[0].Hits.Value>0 && targets[1].Hits.Value==0,"Automatic stops at first target");
            foreach(var target in targets) target.Hits.Value=0;
            player.SelectWeapon(2);
            yield return new WaitForSecondsRealtime(150 / 1000f);
            screen=Camera.main.WorldToScreenPoint(new Vector3(7,-2,0));
            InputSystem.QueueStateEvent(mouse,new MouseState { position=screen,buttons=1 });
            yield return new WaitForSecondsRealtime(90 / 1000f);
            InputSystem.QueueStateEvent(mouse,new MouseState { position=screen });
            yield return new WaitForSecondsRealtime(500 / 1000f);
            Check(targets.All(t=>t.Hits.Value==1),"Rifle pierces exactly three targets without duplicate damage");
            player.SelectWeapon(1);
            yield return new WaitForSecondsRealtime(550 / 1000f);
            screen=Camera.main.WorldToScreenPoint(new Vector3(7,-2,0));
            InputSystem.QueueStateEvent(mouse,new MouseState { position=screen,buttons=1 });
            yield return new WaitForSecondsRealtime(45 / 1000f);
            int pellets=UnityEngine.Object.FindObjectsByType<NetworkProjectile>(FindObjectsSortMode.None).Count(p=>p.IsSpawned);
            InputSystem.QueueStateEvent(mouse,new MouseState { position=screen });
            Check(pellets==6,"Shotgun emits six pellets");
            yield return new WaitForSecondsRealtime(800 / 1000f);
            Check(!UnityEngine.Object.FindObjectsByType<NetworkProjectile>(FindObjectsSortMode.None).Any(p=>p.IsSpawned),"Projectiles return to pool / stop at walls or range");
            body.position=new Vector2(0,1);
            InputSystem.QueueStateEvent(mouse,new MouseState { position=(Vector2)Camera.main.WorldToScreenPoint(new Vector3(1,6,0)),buttons=1 });
            yield return new WaitForSecondsRealtime(800 / 1000f);
            InputSystem.QueueStateEvent(mouse,new MouseState());
            yield return new WaitForSecondsRealtime(200 / 1000f);
            Check(!UnityEngine.Object.FindObjectsByType<NetworkProjectile>(FindObjectsSortMode.None).Any(p=>p.IsSpawned && p.transform.position.y>3.3f),"High wall blocks bullets");
            string result="PASS: walk=5, diagonal=5, sprint=8, stop, wall slide, held fire="+autoShots+", 6 shotgun pellets, 3-target rifle, wall/range despawn.";
            System.IO.Directory.CreateDirectory("Temp"); System.IO.File.WriteAllText("Temp/playground-smoke.txt",result);
            Debug.Log(result);
        }
        finally
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.QueueStateEvent(mouse,new MouseState());
        }
    }
}


