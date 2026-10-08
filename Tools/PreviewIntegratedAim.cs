using System;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;
using TopDownShooter.Networking;
public static class PreviewIntegratedAim
{
    public static string Main(bool showRays=false)
    {
        var player=NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>();
        var source=player.GetComponent<OperatorAimVisual>();
        var fields=typeof(OperatorAimVisual).GetFields(BindingFlags.NonPublic|BindingFlags.Instance);
        int[] sectors={0,1,2,3,4,5,6,7};
        for(int i=0;i<8;i++)
        {
            var go=new GameObject("Integrated sample "+i);go.SetActive(false);
            go.transform.position=new Vector3(48.2f+(i%4)*1.2f,i<4?.6f:-.6f,0);
            var v=go.AddComponent<OperatorAimVisual>();
            foreach(var f in fields) if(Attribute.IsDefined(f,typeof(SerializeField))) f.SetValue(v,f.GetValue(source));
            go.SetActive(true);
            float a=sectors[i]*45*Mathf.Deg2Rad;
            v.Pose(new Vector2(Mathf.Cos(a),Mathf.Sin(a)),player.CurrentWeapon,0,0);
            if(showRays)
            {
                var line=new GameObject("Aim ray "+i).AddComponent<LineRenderer>();
                line.positionCount=2;line.useWorldSpace=true;
                var direction=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                line.SetPosition(0,v.Muzzle);line.SetPosition(1,v.Muzzle+direction*.28f);
                line.startWidth=line.endWidth=.006f;line.sortingOrder=30;
                line.sharedMaterial=new Material(Shader.Find("Sprites/Default"));
                line.startColor=line.endColor=Color.cyan;
            }
        }
        var c=new GameObject("Integrated Gallery Camera").AddComponent<Camera>();
        c.orthographic=true;c.orthographicSize=1.3f;c.transform.position=new Vector3(50,.05f,-10);
        c.enabled=false;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.09f,.12f,.16f);
        return "Eight actual runtime SpriteRenderer poses prepared.";
    }
}
