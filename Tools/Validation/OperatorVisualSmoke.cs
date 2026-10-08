using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unity.Netcode;
using TopDownShooter.Networking;
public static class OperatorVisualSmoke
{
    public static string Main()
    {
        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>().StartCoroutine(Run());
        return "Started drawn rig check";
    }
    private static IEnumerator Run()
    {
        var p=NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>();
        var v=p.GetComponent<OperatorAimVisual>(); var k=Keyboard.current;
        var rows=new HashSet<int>();
        InputSystem.QueueStateEvent(k,new KeyboardState(Key.D));
        for(int i=0;i<8;i++) { yield return new WaitForSecondsRealtime(.1f); rows.Add(v.AnimationRow); }
        InputSystem.QueueStateEvent(k,new KeyboardState());
        yield return new WaitForSecondsRealtime(.15f);
        bool ok=v.HasDrawnArt && rows.Contains(1) && rows.Contains(2) && rows.Contains(3) && rows.Contains(4) && v.AnimationRow==0;
        var directions=new[]{Vector2.down,Vector2.right,Vector2.up,Vector2.left};
        var expected=new[]{0,1,2,1};
        p.enabled=false;
        for(int w=0;w<3;w++)
        {
            p.SelectWeapon(w);
            for(int i=0;i<4;i++)
            {
                v.Pose(directions[i],p.CurrentWeapon,0,p.OwnerClientId);
                var gun=p.transform.Find("Attached weapon");
                ok &= v.FacingColumn==expected[i] && Vector2.Dot(((Vector2)v.Muzzle-(Vector2)gun.position).normalized,directions[i])>.999f;
            }
        }
        p.enabled=true;
        System.IO.File.WriteAllText("Temp/operator-visual.txt",(ok?"PASS":"FAIL")+": modular drawn rig, moving cycle phases="+string.Join(",",rows)+", idle reset, four facing choices, three attached weapons, muzzle alignment.");
        if(!ok) throw new Exception("Drawn rig validation failed");
    }
}
