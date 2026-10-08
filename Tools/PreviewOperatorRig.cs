using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using TopDownShooter.Core;
using TopDownShooter.Networking;
public static class PreviewOperatorRig
{
    public static string Main()
    {
        var source=NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<OperatorAimVisual>();
        var fields=typeof(OperatorAimVisual).GetFields(BindingFlags.NonPublic|BindingFlags.Instance);
        var weapon=AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>("Assets/DATA/Weapons/Automatic.asset");
        float[] angles={0,45,180,-90,90,0},phases={0,.5f,0,.25f,.5f,.5f};
        for(int i=0;i<6;i++)
        {
            var go=new GameObject("Rig sample "+i); go.SetActive(false);
            go.transform.position=new Vector3(48.6f+(i%3)*1.4f,i<3?.65f:-.65f,0);
            var rig=go.AddComponent<OperatorAimVisual>();
            foreach(var field in fields) if(Attribute.IsDefined(field,typeof(SerializeField))) field.SetValue(rig,field.GetValue(source));
            go.SetActive(true);
            typeof(OperatorAimVisual).GetField("phase",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rig,phases[i]);
            typeof(OperatorAimVisual).GetField("strideBlend",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rig,1f);
            float angle=angles[i]*Mathf.Deg2Rad;
            rig.Pose(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)),weapon,5,(ulong)i);
        }
        var c=new GameObject("Rig Gallery Camera").AddComponent<Camera>(); c.orthographic=true;c.orthographicSize=1.45f;
        c.transform.position=new Vector3(50,0,-10);c.enabled=false;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.09f,.12f,.16f);
        return "6 samples: side stride phases, 45-degree layering, mirrored left, front/back foot lanes.";
    }
}
