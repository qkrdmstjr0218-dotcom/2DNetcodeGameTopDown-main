using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.U2D.IK;
using Unity.Netcode;
using UnityEditor;
using TopDownShooter.Networking;
public static class RigCorrectionSmoke
{
    static Transform Node(NetworkPlayerController p,string name) => p.GetComponentsInChildren<Transform>().Single(t=>t.name==name);
    static void Check(bool value,string name)
    { if(!value) { System.IO.File.WriteAllText("Temp/rig-correction.txt","FAIL: "+name); throw new Exception(name); } }
    public static string Main()
    { NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>().StartCoroutine(Run()); return "Started corrected rig / coop check"; }
    static IEnumerator Run()
    {
        var p=NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayerController>(); var rig=p.GetComponent<OperatorAimVisual>();
        p.enabled=false; Check(rig.HasDrawnArt,"Modular sprites loaded");
        foreach(int weapon in new[]{0,1,2}) foreach(float angle in new[]{0f,20f,45f,90f,180f,-90f})
        {
            p.SelectWeapon(weapon); var dir=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad)); rig.Pose(dir,p.CurrentWeapon,0,0);
            int body=p.transform.Find("Drawn operator").GetComponent<SpriteRenderer>().sortingOrder;
            int gun=p.transform.Find("Attached weapon").GetComponent<SpriteRenderer>().sortingOrder;
            if(angle<=45 && angle>=0) Check(gun>body,"Side gun in front at "+angle);
            if(angle==0) Check(p.transform.Find("Right arm upper art").GetComponent<SpriteRenderer>().sortingOrder>body && p.transform.Find("Left arm upper art").GetComponent<SpriteRenderer>().sortingOrder<body,"Right-facing arm depth");
            if(angle==180) Check(p.transform.Find("Right arm upper art").GetComponent<SpriteRenderer>().sortingOrder<body && p.transform.Find("Left arm upper art").GetComponent<SpriteRenderer>().sortingOrder>body,"Left-facing arm depth");
            foreach(var solver in p.GetComponentsInChildren<LimbSolver2D>())
                Check(solver.isValid && Vector3.Distance(solver.GetChain(0).target.position,solver.GetChain(0).effector.position)<.015f,"IK grip/foot reach: "+solver.name+" angle="+angle);
        }
        float minimum=1,maximum=-1;
        float strideDeadline=Time.realtimeSinceStartup+1.2f;
        while(Time.realtimeSinceStartup<strideDeadline)
        {
            yield return null; rig.Pose(Vector2.right,p.CurrentWeapon,5,0);
            float difference=Node(p,"Right leg effector").position.x-Node(p,"Left leg effector").position.x;
            minimum=Mathf.Min(minimum,difference); maximum=Mathf.Max(maximum,difference);
        }
        Check(minimum<-.08f && maximum>.08f,"Side stride alternates front/back");
        for(int i=0;i<10;i++)
        {
            yield return new WaitForSecondsRealtime(.05f); rig.Pose(Vector2.down,p.CurrentWeapon,5,0);
            var right=p.transform.InverseTransformPoint(Node(p,"Right leg effector").position);
            var left=p.transform.InverseTransformPoint(Node(p,"Left leg effector").position);
            Check(right.x<-.08f && left.x>.08f,"Front feet keep separate lanes");
        }
        var mate=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SFPlayer.prefab"),new Vector3(1.5f,-2,0),Quaternion.identity);
        var net=mate.GetComponent<NetworkObject>(); net.Spawn(); mate.GetComponent<NetworkPlayerController>().enabled=false;
        mate.GetComponent<PlayerInputHandler>().EnableInput(false);
        foreach(var camera in mate.GetComponentsInChildren<Unity.Cinemachine.CinemachineCamera>(true)) camera.enabled=false;
        var child=new GameObject("Friendly child collider"); child.transform.SetParent(mate.transform,false); child.AddComponent<CircleCollider2D>().isTrigger=true;
        var health=mate.GetComponent<NetworkHealth>(); int before=health.CurrentHealth.Value;
        p.GetComponent<Rigidbody2D>().position=new Vector2(0,-2); yield return new WaitForFixedUpdate();
        p.SelectWeapon(2); var targets=UnityEngine.Object.FindObjectsByType<PracticeTarget>(FindObjectsSortMode.None);
        foreach(var target in targets) target.Hits.Value=0;
        var flags=BindingFlags.Instance|BindingFlags.NonPublic; typeof(NetworkPlayerController).GetField("localAim",flags).SetValue(p,Vector2.right);
        rig.Pose(Vector2.right,p.CurrentWeapon,0,0); typeof(NetworkPlayerController).GetMethod("HandleAttack",flags).Invoke(p,null);
        yield return new WaitForSecondsRealtime(.7f);
        Check(health.CurrentHealth.Value==before && targets.All(t=>t.Hits.Value==1),"Friendly root/child pass-through without damage");
        health.ApplyDamage(10000); Check(health.IsDowned.Value,"Teammate downed for revive check");
        var game=NetworkGameManager.Instance; game.BeginPracticeWaves();
        for(int i=0;i<75;i++)
        {
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<NetworkEnemy>(FindObjectsSortMode.None))
            { enemy.GetComponent<EnemyAI>().enabled=false; enemy.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero; }
            yield return new WaitForSecondsRealtime(.1f);
        }
        foreach(var enemy in UnityEngine.Object.FindObjectsByType<NetworkEnemy>(FindObjectsSortMode.None)) if(enemy.IsSpawned) enemy.ReceiveDamage(10000,0);
        yield return new WaitForSecondsRealtime(1);
        Check(!health.IsDowned.Value && health.CurrentHealth.Value==health.MaxHealth,"Existing wave-clear teammate revival");
        game.StopPracticeWaves(); net.Despawn(); p.enabled=true;
        System.IO.File.WriteAllText("Temp/rig-correction.txt","PASS: side gun 0/20/45 degrees, separate arm depth both sides, official 4-limb IK target reach, alternating side stride, separate front foot lanes, no friendly fire through root/child colliders, existing wave-clear revive.");
    }
}
