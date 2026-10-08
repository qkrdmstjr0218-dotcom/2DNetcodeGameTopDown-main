using System;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;
using TopDownShooter.Networking;
public static class PreviewRightWalkFour
{
    public static string Main()
    {
        var p=NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>();
        var source=p.GetComponent<OperatorAimVisual>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        for(int i=0;i<4;i++)
        {
            var go=new GameObject("Four walk sample "+i);go.SetActive(false);
            go.transform.position=new Vector3(48.2f+i*1.2f,0,0);
            var v=go.AddComponent<OperatorAimVisual>();
            foreach(var f in typeof(OperatorAimVisual).GetFields(flags))if(Attribute.IsDefined(f,typeof(SerializeField)))f.SetValue(v,f.GetValue(source));
            go.SetActive(true);
            typeof(OperatorAimVisual).GetField("previousPosition",flags).SetValue(v,go.transform.position-Vector3.right*.01f);
            typeof(OperatorAimVisual).GetField("rightWalkPhase",flags).SetValue(v,(i+.1f)/4f-.01f/2.2f);
            v.Pose(Vector2.right,p.CurrentWeapon,5,0);
            if(v.AnimationRow!=i+1)throw new Exception("Wrong walk frame "+i);
        }
        var c=new GameObject("Four Walk Gallery Camera").AddComponent<Camera>();
        c.orthographic=true;c.orthographicSize=1.05f;c.transform.position=new Vector3(50,.1f,-10);
        c.enabled=false;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.09f,.12f,.16f);
        return "Four runtime samples ready; compare contact frames 1 and 3, passing frames 2 and 4 visually.";
    }
}
