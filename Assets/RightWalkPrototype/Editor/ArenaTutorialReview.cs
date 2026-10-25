using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using RightWalkPrototype;

// Single-editor integration review. Drives the real motor and weapon requests;
// only lesson selection and the explicitly labelled negative fixtures teleport.
[InitializeOnLoad]
public static partial class ArenaTutorialReview
{
    const string Queued="ArenaTutorialReview.Queued";
    const string Folder="LocalTools/Analysis/Tutorial";
    static ArenaTutorial tutorial;
    static SideScrollNetworkPlayer player;
    static SideScrollMapPreview move;
    static RightFacingAimRig aim;
    static RightFacingShotPreview shot;
    static readonly List<string> passed=new();
    static ArenaTutorialReview(){EditorApplication.update+=Tick;EditorApplication.update+=LiveTick;}
    [MenuItem("Tools/Right Walk Prototype/Verify Offline Tutorial")]
    public static void Begin()
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Requires saved edit mode");
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING");
        SessionState.SetBool(Queued,true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor||!SessionState.GetBool(Queued,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||Time.time<.5f)return;
        var session=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();if(session==null)return;
        SessionState.SetBool(Queued,false);passed.Clear();session.StartCoroutine(Guard(Run(session)));
    }
    static IEnumerator Guard(IEnumerator work)
    {
        var stack=new Stack<IEnumerator>();stack.Push(work);
        while(stack.Count>0)
        {
            object current=null;bool next=false;Exception failure=null;
            try{next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}
            catch(Exception e){failure=e;}
            if(failure!=null)
            {
                File.WriteAllText(Folder+"/result.txt","FAIL: "+failure+"\n"+string.Join("\n",passed));
                ScreenCapture.CaptureScreenshot(Folder+"/failure.png");yield break;
            }
            if(!next){stack.Pop();continue;}
            if(current is IEnumerator nested)stack.Push(nested);else yield return current;
        }
        File.WriteAllText(Folder+"/result.txt","PASS\n"+string.Join("\n",passed));
    }
    static void Check(bool okay,string label)
    {
        if(!okay)throw new Exception(label);
        passed.Add(label);File.WriteAllText(Folder+"/progress.txt",string.Join("\n",passed));
    }
    static IEnumerator Wait(Func<bool> condition,string label,float timeout=8)
    {
        float end=Time.time+timeout,wall=Time.realtimeSinceStartup+timeout*6;
        while(!condition()&&Time.time<end&&Time.realtimeSinceStartup<wall)yield return null;
        Check(condition(),label);
    }
    static IEnumerator Drive(float x,bool sprint=false)
    {
        move.SetInput(Mathf.Sign(x-player.transform.position.x),false,sprint);
        yield return Wait(()=>Mathf.Abs(player.transform.position.x-x)<.12f,"drive to "+x);
        move.SetInput(0);yield return new WaitForSeconds(.15f);
    }
    static IEnumerator Lesson(int lesson)
    {
        move.SetInput(0);player.GetComponent<CrouchTrial>().SetRequested(false);
        tutorial.EnterLesson(lesson);yield return new WaitForSeconds(.3f);
    }
    static IEnumerator Fire(SideScrollNetworkPlayer.AmmoKind kind,Vector2 point)
    {
        player.SetSpecialHeld(kind!=SideScrollNetworkPlayer.AmmoKind.Normal);player.SelectAmmo(kind);aim.SetAimTarget(point);
        yield return Wait(()=>player.FireWaitRemaining<=0,"weapon ready "+kind,3);
        yield return null;shot.Fire();yield return new WaitForSeconds(.3f);
    }
    static IEnumerator Capture(string name)
    {ScreenCapture.CaptureScreenshot(Folder+"/"+name+".png");yield return new WaitForSecondsRealtime(.2f);}
    static IEnumerator Run(SideScrollNetworkSession session)
    {
        session.StartTutorial();
        yield return Wait(()=>ArenaTutorial.Active&&UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>()!=null,"tutorial starts offline");
        tutorial=ArenaTutorial.Instance;player=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>();
        move=player.GetComponent<SideScrollMapPreview>();aim=player.GetComponentInChildren<RightFacingAimRig>();shot=player.GetComponentInChildren<RightFacingShotPreview>();
        Check(aim.mouseAim&&shot.readMouse&&move.readKeyboard,"fresh tutorial retains human mouse and keyboard input");
        tutorial.RetryLesson();yield return null;Check(aim.mouseAim,"retry preserves mouse aiming");
        aim.mouseAim=false;shot.readMouse=false;move.SetInput(0);
        yield return new WaitForSeconds(.4f);
        var match=ArenaObjectiveMatch.Instance;
        Check(session.SoloPractice&&match.SoloPractice&&ArenaObjectiveMatch.PracticeRoom&&match.ReadyPlayers.Count==1&&!match.CanStart,"solo waiting, match start blocked");
        Check(Unity.Services.Core.UnityServices.State==Unity.Services.Core.ServicesInitializationState.Uninitialized,"UGS remains uninitialized");
        Check(UnityEngine.Object.FindObjectsByType<ArenaPatrolEnemy>(FindObjectsSortMode.None).Length==0,"no arena enemies in tutorial");
        yield return Drive(204.3f);yield return Drive(210.3f,true);
        yield return Wait(()=>tutorial.LessonComplete,"walking and sprint lesson achieved");
        yield return Capture("01-movement");

        yield return Lesson(1);
        yield return Drive(208.5f);
        move.SetInput(0,true);yield return new WaitForSeconds(1.7f);
        Check(move.Grounded&&player.transform.position.y<.6f&&!tutorial.LessonComplete,"single jump cannot reach 3.2-high goal");
        move.SetInput(0,true);yield return new WaitForSeconds(.45f);move.SetInput(0,true);
        yield return Wait(()=>tutorial.LessonComplete,"double jump lands on required upper ledge");
        yield return Capture("02-double-jump");

        yield return Lesson(2);
        player.GetComponent<CrouchTrial>().SetRequested(true);yield return new WaitForSeconds(.2f);
        move.SetInput(1);
        yield return Wait(()=>player.transform.position.x>210.8f&&move.Grounded&&player.transform.position.y<.6f,"crouched walk off edge reaches floor");
        Check(!tutorial.LessonComplete,"walking off ledge is not crouch + jump drop");
        yield return Lesson(2);
        player.GetComponent<CrouchTrial>().SetRequested(true);yield return new WaitForSeconds(.2f);
        move.SetInput(0,true);yield return new WaitForSeconds(.12f);move.SetInput(0);
        player.GetComponent<CrouchTrial>().SetRequested(false);
        yield return Wait(()=>tutorial.LessonComplete,"crouch + jump drops through ledge to floor");
        yield return Capture("03-drop");

        yield return Lesson(3);
        yield return Fire(SideScrollNetworkPlayer.AmmoKind.Normal,tutorial.Target.transform.position);
        yield return Fire(SideScrollNetworkPlayer.AmmoKind.Rifle,tutorial.Target.transform.position);
        yield return new WaitForSeconds(4);
        Check(tutorial.Target.Health.Value==tutorial.Target.maxHealth&&!tutorial.LessonComplete,"wrong weapon kill restores normal target without awarding completion");
        for(int i=0;i<3;i++)yield return Fire(SideScrollNetworkPlayer.AmmoKind.Normal,tutorial.Target.transform.position);
        yield return Wait(()=>tutorial.Target.Health.Value==0,"normal shots destroy target");
        yield return Wait(()=>tutorial.LessonComplete,"normal ammo recharges fully",12);
        yield return Capture("04-normal");

        yield return Lesson(4);
        Check(tutorial.Target.GetComponent<SpriteRenderer>().bounds.min.y>=-.015f,"shotgun target artwork stands above the floor");
        yield return Fire(SideScrollNetworkPlayer.AmmoKind.InstantShotgun,(Vector2)player.transform.position+Vector2.up*10);
        yield return Fire(SideScrollNetworkPlayer.AmmoKind.Normal,tutorial.Target.transform.position);
        yield return Wait(()=>tutorial.Target.Hits.Value>0,"normal bullet arrives after missed shotgun");
        Check(tutorial.Target.Hits.Value>0&&Mathf.Abs(tutorial.Target.GetComponent<Rigidbody2D>().linearVelocity.x)<.01f&&!tutorial.LessonComplete,"missed shotgun followed by normal hit cannot push target");
        yield return Lesson(4);
        yield return Fire(SideScrollNetworkPlayer.AmmoKind.InstantShotgun,tutorial.Target.transform.position);
        yield return Wait(()=>tutorial.LessonComplete,"actual shotgun hit pushes target into pit");
        yield return Capture("05-shotgun");

        yield return Lesson(5);
        yield return Fire(SideScrollNetworkPlayer.AmmoKind.Normal,tutorial.Target.transform.position);
        Check(tutorial.Target.Health.Value==tutorial.Target.maxHealth&&!tutorial.LessonComplete,"normal bullet blocked by rifle enclosure");
        yield return Fire(SideScrollNetworkPlayer.AmmoKind.Rifle,tutorial.Target.transform.position);
        yield return Wait(()=>tutorial.LessonComplete,"rifle pierces enclosure and destroys target");
        yield return Capture("06-rifle");

        yield return Lesson(6);
        tutorial.Target.ReceiveDamage(99,tutorial.Target.Generation,player,SideScrollNetworkPlayer.AmmoKind.Normal);
        yield return null;
        Check(!tutorial.LessonComplete,"non-grenade target kill fixture cannot complete grenade lesson");
        yield return Lesson(6);
        Vector2 solution=default;bool found=false;var points=new List<Vector2>();
        for(float x=205;x<=215&&!found;x+=.25f)for(float y=2;y<=11;y+=.25f)
        {
            var candidate=new Vector2(x,y);aim.SetAimTarget(candidate);
            if(player.PredictGrenade(points,out var end)&&ArenaGrenadeBallistics.BlastDamage(end,tutorial.Target.GetComponent<Collider2D>().bounds,6,2)>=3)
            {solution=candidate;found=true;break;}
        }
        Check(found,"grenade arc solution exists from starting position");
        yield return Fire(SideScrollNetworkPlayer.AmmoKind.Grenade,solution);
        yield return Wait(()=>tutorial.Target.Health.Value==0,"actual grenade clears cover and destroys target",5);
        yield return Fire(SideScrollNetworkPlayer.AmmoKind.Grenade,(Vector2)player.transform.position+new Vector2(.5f,-.1f));
        yield return Wait(()=>player.Health<player.MaxHealth,"actual nearby grenade causes self damage",5);
        yield return new WaitForSeconds(1);
        Check(!tutorial.LessonComplete,"damaged player still needs medical pad");
        int damagedHealth=player.Health;yield return new WaitForSeconds(1);
        Check(player.Health==damagedHealth,"no automatic healing outside medical pad");
        yield return Drive(198);
        yield return Wait(()=>tutorial.LessonComplete,"medical pad restores health and completes grenade lesson",6);
        yield return Capture("07-grenade-medical");
        Check(tutorial.CompletedCount==7,"all seven lessons achieved");
        tutorial.RetryLesson();yield return new WaitForSeconds(.3f);
        Check(tutorial.CompletedCount==7&&!tutorial.LessonComplete&&player.Health==player.MaxHealth,"retry resets attempt and keeps completed history");
        tutorial.SkipLesson();yield return null;
        Check(tutorial.Lesson==7&&tutorial.CompletedCount==7,"completion screen retains earned count");
        yield return Capture("08-complete");
        session.TutorialToPractice();
        yield return Wait(()=>!ArenaTutorial.Active&&NetworkManager.Singleton.IsListening&&ArenaObjectiveMatch.PracticeRoom&&UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>()!=player,"tutorial switches to ordinary solo practice",12);
        Check(UnityEngine.Object.FindObjectsByType<SideScrollDamageTarget>(FindObjectsSortMode.None).Length==(session.targetPrefab==null?0:2),"ordinary waiting room target count restored");
        bool leftover=false;foreach(var go in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))leftover|=go.name.StartsWith("Offline tutorial lesson");
        Check(!leftover,"no tutorial geometry remains");
        session.DisconnectLocal();yield return Wait(()=>!NetworkManager.Singleton.IsListening&&!NetworkManager.Singleton.ShutdownInProgress,"disconnect cleans up",5);
        session.StartLocal(true);yield return Wait(()=>ArenaObjectiveMatch.PracticeRoom,"ordinary local host starts");
        Check(!session.SoloPractice&&!ArenaObjectiveMatch.Instance.SoloPractice&&!ArenaObjectiveMatch.Instance.CanStart,"ordinary host still requires two players");
        yield return new WaitForSeconds(.4f);
        var ordinary=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>();
        ordinary.ReceiveServerDamage(20,ordinary.LifeVersion);
        Check(ordinary.Health<ordinary.MaxHealth&&ordinary.GrantHealthServer(25,ordinary.LifeVersion)==0,"tutorial medical exception does not allow ordinary room healing");
        session.DisconnectLocal();yield return new WaitForSeconds(.5f);
        session.StartTutorial();yield return Wait(()=>ArenaTutorial.Active&&UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>()!=null,"tutorial can be entered again");
        tutorial=ArenaTutorial.Instance;yield return new WaitForSeconds(.4f);
        player=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>();move=player.GetComponent<SideScrollMapPreview>();move.SetInput(0);
        Check(tutorial.CompletedCount==0,"new tutorial visit clears completion history");
        uint life=player.LifeVersion;move.SetSpawnPosition(new Vector2(211,-2.6f));
        yield return Wait(()=>player.LifeVersion!=life&&Mathf.Abs(player.transform.position.x-200)<.1f,"fall fixture returns to same lesson checkpoint");
        Check(tutorial.Lesson==0&&tutorial.CompletedCount==0,"fall does not award completion");
        RelayArenaPrototype.Instance.SetMenuOpen(true);float before=player.transform.position.x;move.SetInput(1);
        yield return new WaitForSeconds(.3f);
        Check(Mathf.Abs(player.transform.position.x-before)<.02f,"menu blocks tutorial movement");
        RelayArenaPrototype.Instance.SetMenuOpen(false);move.SetInput(0);
        for(int i=0;i<ArenaTutorial.LessonCount;i++){tutorial.SkipLesson();yield return null;}
        Check(tutorial.Lesson==7&&tutorial.CompletedCount==0,"skipping all lessons never awards completion");
        session.DisconnectLocal();yield return new WaitForSeconds(.5f);
        Check(!ArenaTutorial.Active&&UnityEngine.Object.FindObjectsByType<SideScrollDamageTarget>(FindObjectsSortMode.None).Length==0,"exit from completion returns cleanly to lobby");
    }
}
