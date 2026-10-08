using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unity.Netcode;
using TopDownShooter.Networking;
public static class RightWalkInputSmoke
{
    public static string Main()
    {
        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>().StartCoroutine(Run());
        return "Started real input walk frame check";
    }
    private static IEnumerator Run()
    {
        var p=NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>();
        var v=p.GetComponent<OperatorAimVisual>();var rows=new HashSet<int>();
        var k=Keyboard.current;var m=Mouse.current;
        var point=Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*5);
        InputSystem.QueueStateEvent(m,new MouseState{position=new Vector2(point.x,point.y)});
        InputSystem.QueueStateEvent(k,new KeyboardState(Key.D));
        for(int i=0;i<12;i++){yield return new WaitForSecondsRealtime(.08f);if(v.AimSector==0)rows.Add(v.AnimationRow);}
        InputSystem.QueueStateEvent(k,new KeyboardState());
        yield return new WaitForSecondsRealtime(.15f);
        var rigField=typeof(OperatorAimVisual).GetField("useRightLegRig",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        int expected=(bool)rigField.GetValue(v)?4:6;
        bool ok=rows.Count>=expected && v.AnimationRow==0;
        string result=(ok?"PASS":"FAIL")+": real D input walk phases="+string.Join(",",rows)+", stopped row="+v.AnimationRow;
        System.IO.File.WriteAllText("Temp/right-walk-input.txt",result);
        if(!ok)throw new Exception(result);
    }
}
