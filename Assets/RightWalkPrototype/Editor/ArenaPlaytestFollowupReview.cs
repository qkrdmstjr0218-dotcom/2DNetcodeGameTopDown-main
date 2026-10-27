using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unity.Netcode;
using RightWalkPrototype;

[InitializeOnLoad]
public static class ArenaPlaytestFollowupReview
{
    const string Key="ArenaPlaytestFollowupReview",Folder="LocalTools/Analysis/PlaytestFollowup-2026-10-26";
    static readonly List<string> results=new();
    static EditorWindow view;static PropertyInfo sizeProperty;static int oldSize;static GameObject dummy,wall;
    static ArenaPlaytestFollowupReview(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/Right Walk Prototype/Verify Playtest Followup Solo")]
    public static void Begin(){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||Time.time<.5f)return;
        var session=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();if(session==null)return;
        SessionState.SetBool(Key,false);results.Clear();session.StartCoroutine(Guard(Run(session),session));
    }
    static void Check(bool ok,string label){results.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(label);}
    static ArenaObjectiveMatch.Score[] Rows(params float[] totals)
    {var rows=new ArenaObjectiveMatch.Score[totals.Length];for(int i=0;i<rows.Length;i++)rows[i]=new ArenaObjectiveMatch.Score{Client=(ulong)i,Number=i+1,Seconds=totals[i]};return rows;}
    static void Case(string name,float[] totals,int[] ranks,string result)
    {
        var rows=Rows(totals);Array.Sort(rows,ArenaScoreRules.Compare);
        for(int i=0;i<rows.Length;i++)Check(ArenaScoreRules.Rank(rows,i)==ranks[i],name+" rank row "+i);
        Check(ArenaScoreRules.Result(rows)==result,name+" winner text");
    }
    public static void CheckScores()
    {
        Case("zero",new[]{0f,0,0,0},new[]{1,1,1,1},"획득 점수 없음 · 무승부");
        Case("winner",new[]{10f,20,5,0},new[]{1,2,3,4},"P2 승리");
        Case("shared first",new[]{10f,10,5,0},new[]{1,1,3,4},"P1 / P2 공동 1위");
        Case("shared second",new[]{20f,10,10,0},new[]{1,2,2,4},"P1 승리");
        Case("epsilon chain regression",new[]{10f,9.96f,9.92f,0},new[]{1,1,3,4},"P1 / P2 공동 1위");
        Case("rounding boundary",new[]{10.049f,10.051f,0,0},new[]{1,2,3,3},"P2 승리");
        Case("first visible point",new[]{.049f,.05f,0,0},new[]{1,2,2,2},"P2 승리");
        Case("four way",new[]{10f,10,10,10},new[]{1,1,1,1},"P1 / P2 / P3 / P4 공동 1위");
        bool consistent=true;for(int n=0;n<2000;n++)
        {
            float a=n*.001f,b=a+.0004f;var pair=Rows(a,b);Array.Sort(pair,ArenaScoreRules.Compare);
            bool sameText=ArenaScoreRules.Format(a)==ArenaScoreRules.Format(b);
            consistent&=sameText==(ArenaScoreRules.Rank(pair,1)==1);
        }
        Check(consistent,"2000 near-boundary score pairs have matching displayed values and ranks");
        var equal=Rows(10,10);equal[1].Seconds=0;equal[1].KillPoints=10;equal[1].Kills=2;
        Array.Sort(equal,ArenaScoreRules.Compare);Check(equal[0].Number==1&&ArenaScoreRules.Rank(equal,1)==1,"kills are not a hidden tiebreaker");
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/score-rules.txt",string.Join("\n",results));
    }
    static IEnumerator Frames(int n=3){for(int i=0;i<n;i++)yield return null;}
    static IEnumerator Wait(Func<bool> test,string name,float seconds=15)
    {float until=Time.time+seconds;while(!test()&&Time.time<until)yield return null;if(!test())throw new Exception("Timeout: "+name);}
    static IEnumerator Capture(string name)
    {yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
    static void Resolution(int width,int height)
    {view.GetType().GetMethod("SetCustomResolution",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,new object[]{new Vector2(width,height),"Playtest followup "+width});}
    static void Cleanup(SideScrollNetworkSession session)
    {
        if(dummy!=null){var net=dummy.GetComponent<NetworkObject>();if(net.IsSpawned)net.Despawn();else UnityEngine.Object.Destroy(dummy);}
        if(wall!=null)UnityEngine.Object.Destroy(wall);
        if(view!=null&&sizeProperty!=null)sizeProperty.SetValue(view,oldSize);
        if(Mouse.current!=null)InputSystem.QueueStateEvent(Mouse.current,new MouseState());
        if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        session.DisconnectLocal();EditorApplication.isPlaying=false;
    }
    static IEnumerator Guard(IEnumerator run,SideScrollNetworkSession session)
    {
        var stack=new Stack<IEnumerator>();stack.Push(run);
        while(stack.Count>0)
        {
            bool next=false;object value=null;Exception error=null;
            try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception ex){error=ex;}
            if(error!=null){File.WriteAllText(Folder+"/result.txt","ERROR "+error+"\n"+string.Join("\n",results));Cleanup(session);yield break;}
            if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;
        }
        File.WriteAllText(Folder+"/result.txt","PASS\n"+string.Join("\n",results));Cleanup(session);
    }
    static IEnumerator Run(SideScrollNetworkSession session)
    {
        CheckScores();
        view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.Focus();
        sizeProperty=view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);oldSize=(int)sizeProperty.GetValue(view);
        Resolution(1366,768);yield return Frames();session.StartSoloPractice();yield return Wait(()=>ArenaObjectiveMatch.PracticeRoom,"host");
        var match=ArenaObjectiveMatch.Instance;var player=NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<SideScrollNetworkPlayer>();
        Check(!match.IsReady(player.OwnerClientId)&&!match.CanStart,"unready host cannot start");yield return Capture("ready-before-1366");
        match.SetLocalReady(true);yield return Frames();Check(match.CanStart&&match.IsReady(player.OwnerClientId),"ready enables existing solo start rule");yield return Capture("ready-after-1366");
        match.SetLocalReady(false);yield return Frames();Check(!match.CanStart,"cancel restores unready state");
        Resolution(1920,1080);yield return Frames();yield return Capture("ready-before-1920");match.SetLocalReady(true);yield return Frames();yield return Capture("ready-after-1920");
        Check(match.StartRound(),"start unchanged");yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing,"playing");
        for(int i=0;i<match.Patrols.Count;i++){var state=match.Patrols[i];state.Health=0;state.RespawnAt=match.Now+600;match.Patrols[i]=state;}
        var movement=player.GetComponent<SideScrollMapPreview>();movement.readKeyboard=false;movement.SetInput(0);movement.SetSpawnPosition(new Vector2(-5.8f,6.84f));
        var shots=player.GetComponentInChildren<RightFacingShotPreview>();shots.readMouse=false;var aim=player.GetComponentInChildren<RightFacingAimRig>();aim.mouseAim=false;
        yield return Frames(8);
        Check(SideScrollNetworkPlayer.RifleDamage==30&&SideScrollNetworkPlayer.ShieldCapacity==50,"rifle 30 and shield cap 50");
        Check(player.GrantShieldServer(player.LifeVersion)&&player.Shield==50,"shield initial 50");player.ReceiveServerDamage(30,player.LifeVersion);
        Check(player.Health==100&&player.Shield==20,"shield absorbs 30 without HP loss");
        yield return new WaitForSeconds(.3f);Check(player.GrantShieldServer(player.LifeVersion)&&player.Shield==50&&player.ShieldRemaining>14.8f,"reacquire replenishes 50 and refreshes 15 seconds");
        Check(player.GrantShieldServer(player.LifeVersion)&&player.Shield==50,"full shield never stacks above 50");
        dummy=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RightWalkPrototype/SideScrollNetworkPlayer.prefab"));dummy.name="Review server-owned target (solo fixture)";
        dummy.GetComponent<NetworkObject>().Spawn();var target=dummy.GetComponent<SideScrollNetworkPlayer>();
        var targetMove=target.GetComponent<SideScrollMapPreview>();targetMove.readKeyboard=false;targetMove.SetInput(0);targetMove.SetSpawnPosition(new Vector2(4.2f,6.84f));
        dummy.GetComponentInChildren<RightFacingShotPreview>().readMouse=false;dummy.GetComponentInChildren<RightFacingAimRig>().mouseAim=false;yield return Frames(10);
        // A temporary solid wall demonstrates that the existing rifle path still ignores terrain.
        wall=new GameObject("Review solid wall");wall.transform.position=new Vector3(0,8,0);wall.AddComponent<BoxCollider2D>().size=new Vector2(.5f,3);
        Physics2D.SyncTransforms();player.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Rifle);player.SetSpecialHeld(true);
        yield return new WaitForSeconds(.6f); // Honour the existing effective-mode switch delay.
        for(int shot=1;shot<=3;shot++)
        {
            aim.SetAimTarget(target.transform.position+Vector3.up*1.05f);yield return Frames();shots.Fire();int hp=100-shot*30;
            File.AppendAllText(Folder+"/rifle-trace.txt","shot="+shot+" received="+player.ReceivedRifleShots+" ammo="+player.RifleRounds+" shooter="+player.transform.position+" muzzle="+aim.MuzzlePosition+" direction="+aim.AimDirection+" target="+target.transform.position+" health="+target.Health+"\n");
            yield return Wait(()=>target.Health==hp,"rifle hit "+shot,3);yield return new WaitForSeconds(1.25f);
            Check(target.Health==hp,"one rifle hit deals exactly 30 once through wall: "+shot);
        }
        Check(target.Health==10&&player.RifleRounds==0,"three-round rifle magazine leaves unshielded 100HP target at 10");
        dummy.GetComponent<NetworkObject>().Despawn();dummy=null;UnityEngine.Object.Destroy(wall);wall=null;
        // Freeze score updates for isolated four-row result presentation; this is not a four-peer test.
        var finish=match.State.Value;finish.Phase=ArenaObjectiveMatch.Stage.Finished;match.State.Value=finish;
        foreach(var row in new[]{Rows(10,9.96f,9.92f,0),Rows(0,0,0,0)})
        {
            match.Scores.Clear();foreach(var score in row)match.Scores.Add(score);match.Result.Value=new Unity.Collections.FixedString128Bytes(ArenaScoreRules.Result(row));
            yield return Frames();yield return Capture(row[0].Total==0?"results-draw-1920":"results-ties-1920");
        }
        Check(!match.IsReady(player.OwnerClientId),"combat start cleared readiness");
    }
}
