using System;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;
using TopDownShooter.Networking;
public static class PreviewRightWalk
{
    public static string Main()
    {
        var p=NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>();
        var source=p.GetComponent<OperatorAimVisual>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        for(int i=0;i<6;i++)
        {
            var go=new GameObject("Right walk sample "+i);go.SetActive(false);
            go.transform.position=new Vector3(48.6f+(i%3)*1.4f,i<3?.6f:-.6f,0);
            var v=go.AddComponent<OperatorAimVisual>();
            foreach(var f in typeof(OperatorAimVisual).GetFields(flags))if(Attribute.IsDefined(f,typeof(SerializeField)))f.SetValue(v,f.GetValue(source));
            go.SetActive(true);
            typeof(OperatorAimVisual).GetField("previousPosition",flags).SetValue(v,go.transform.position-Vector3.right*.01f);
            typeof(OperatorAimVisual).GetField("rightWalkPhase",flags).SetValue(v,(i+.1f)/6f-.01f/2.2f);
            v.Pose(Vector2.right,p.CurrentWeapon,5,0);
            if(v.AnimationRow!=i+1)throw new Exception("Wrong walk frame "+i);
        }
        // Stop and non-horizontal motion must restore the standing pose.
        var enabled=p.enabled;p.enabled=false;
        try
        {
            typeof(OperatorAimVisual).GetField("previousPosition",flags).SetValue(source,p.transform.position);
            source.Pose(Vector2.right,p.CurrentWeapon,0,p.OwnerClientId);
            if(source.AnimationRow!=0)throw new Exception("Stop did not restore idle.");
            typeof(OperatorAimVisual).GetField("previousPosition",flags).SetValue(source,p.transform.position+Vector3.right*.02f);
            source.Pose(Vector2.right,p.CurrentWeapon,5,p.OwnerClientId);
            if(source.AnimationRow!=6)throw new Exception("Backward cycle did not reverse.");
        }
        finally{p.enabled=enabled;}
        var c=new GameObject("Right Walk Gallery Camera").AddComponent<Camera>();
        c.orthographic=true;c.orthographicSize=1.3f;c.transform.position=new Vector3(50,.05f,-10);
        c.enabled=false;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.09f,.12f,.16f);
        return "PASS: six walk frames, stop to idle, backward playback. Gait art is a first visual prototype.";
    }
}
