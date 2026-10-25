using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using RightWalkPrototype;

[InitializeOnLoad]
public static class ArenaCombatHudReview
{
    const string Key="ArenaCombatHudReview",Folder="LocalTools/Analysis/CombatHud";
    static readonly List<string> results=new();
    static bool debugPaused;static double resumeAt;
    static ArenaCombatHudReview(){EditorApplication.update+=Tick;}
    public static void Begin(){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    static void Tick()
    {
        if(debugPaused&&EditorApplication.timeSinceStartup>=resumeAt){debugPaused=false;EditorApplication.isPaused=false;}
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||Time.time<.5f)return;
        var session=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();if(session==null)return;
        SessionState.SetBool(Key,false);results.Clear();session.StartCoroutine(Guard(Run(session)));
    }
    static IEnumerator Guard(IEnumerator run)
    {
        var stack=new Stack<IEnumerator>();stack.Push(run);
        while(stack.Count>0)
        {
            bool next=false;object value=null;Exception error=null;
            try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){error=e;}
            if(error!=null){File.WriteAllText(Folder+"/result.txt","ERROR "+error+"\n"+string.Join("\n",results));yield break;}
            if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;
        }
        File.WriteAllText(Folder+"/result.txt",(results.Exists(x=>x.StartsWith("FAIL"))?"FAIL":"PASS")+"\n"+string.Join("\n",results));
    }
    static IEnumerator Wait(Func<bool> test,string label,float seconds=15)
    {float until=Time.time+seconds;while(!test()&&Time.time<until)yield return null;if(!test())throw new Exception(label);}
    static void Check(bool okay,string label){results.Add((okay?"PASS ":"FAIL ")+label);}
    static void Keys(params Key[] keys){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys));}
    static IEnumerator Frames(int count=3){for(int i=0;i<count;i++)yield return null;}
    static IEnumerator Capture(string name)
    {yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
    static bool PermanentFloorAt(ArenaMapSettings map,Vector2 point)
    {
        var arena=RelayArenaPrototype.Instance;
        foreach(var hit in Physics2D.RaycastAll(point+Vector2.up*.03f,Vector2.down,.1f))
        {
            var c=hit.collider;if(c==null||!c.enabled||c.isTrigger||c.usedByEffector||c.GetComponentInParent<ArenaCover>()!=null||c.GetComponentInParent<SideScrollNetworkPlayer>()!=null||c.GetComponentInParent<ArenaPatrolEnemy>()!=null)continue;
            var bridge=c.GetComponentInParent<ArenaRetractingBridge>();if(bridge!=null&&(c.transform.IsChildOf(bridge.westLeaf)||c.transform.IsChildOf(bridge.eastLeaf)))continue;
            if(c.transform.IsChildOf(arena.bridgeLeft)||c.transform.IsChildOf(arena.bridgeRight))continue;
            if(c.transform.IsChildOf(map.transform)&&hit.normal.y>.6f&&Mathf.Abs(hit.point.y-point.y)<.025f)return true;
        }
        return false;
    }
    static IEnumerator CheckCoverSupport(ArenaObjectiveMatch match,SideScrollNetworkPlayer player)
    {
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();var arena=RelayArenaPrototype.Instance;
        foreach(var cover in map.GetComponentsInChildren<ArenaCover>())
        {
            var bounds=cover.GetComponent<BoxCollider2D>().bounds;bool support=true;
            foreach(float x in new[]{bounds.min.x+.03f,bounds.center.x,bounds.max.x-.03f})support&=PermanentFloorAt(map,new Vector2(x,bounds.min.y));
            Check(support,"cover footprint has permanent floor: "+cover.name);
            bool overBridge=Mathf.Abs(bounds.min.y)<.03f&&bounds.min.x<arena.pitRight&&bounds.max.x>arena.pitLeft;
            foreach(var bridge in map.GetComponentsInChildren<ArenaRetractingBridge>())overBridge|=Mathf.Abs(bounds.min.y-bridge.floor)<.03f&&bounds.min.x<bridge.right&&bounds.max.x>bridge.left;
            Check(!overBridge,"cover avoids every retracting span: "+cover.name);
        }
        var west=map.transform.Find("West retracting crossing").GetComponent<ArenaRetractingBridge>();float offset=west.phaseOffset;west.phaseOffset=(float)((10.8-match.Now%14+14)%14);
        player.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(-24.8f,.04f));yield return new WaitForSeconds(.5f);
        Check(west.Retracted,"west bridge open for support inspection");yield return Capture("west-cover-open-bridge");west.phaseOffset=offset;
    }
    static IEnumerator CheckRampRobots(ArenaObjectiveMatch match,SideScrollNetworkPlayer player)
    {
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();var graph=new ArenaEnemyNavigation(map,false);
        int index=-1;for(int i=0;i<match.Patrols.Count;i++)if(match.Patrols[i].Roaming&&!match.Patrols[i].Flying&&!match.Patrols[i].Medical&&!match.Patrols[i].Turret){index=i;break;}
        if(index<0)throw new Exception("ground robot unavailable");
        foreach(int side in new[]{-1,1})
        {
            var origin=new Vector2(side*17,.02f);var goal=new Vector2(side*12,2.04f);
            var path=graph.Route(origin,goal,false);Check(path.Count>1&&Vector2.Distance(graph.Nodes[path[path.Count-1]].Point,goal)<.6f,"ground graph reaches inner ramp "+side);
            player.GetComponent<SideScrollMapPreview>().SetSpawnPosition(goal+Vector2.up*.2f);yield return new WaitForSeconds(.5f);
            var enemy=match.Patrols[index];enemy.Health=4;enemy.Roaming=true;enemy.Flying=false;enemy.Position=origin;enemy.Angle=0;enemy.EntryUntil=0;enemy.RespawnAt=match.Now+600;enemy.RushPhase=0;enemy.RushUntil=0;enemy.NextAttack=match.Now+600;enemy.FireAt=match.Now+600;enemy.Velocity=Vector2.zero;
            typeof(ArenaObjectiveMatch).GetMethod("ResetEnemyBrain",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(match,new object[]{enemy.Id});match.Patrols[index]=enemy;
            var trace=new List<string>{"time,x,y"};double until=match.Now+10;
            while(match.Now<until){var current=match.Patrols[index];trace.Add(match.Now+","+current.Position.x+","+current.Position.y);if(current.Position.y>.9f&&Mathf.Abs(current.Position.x)<13.8f)break;yield return new WaitForSeconds(.1f);}
            var final=match.Patrols[index];Check(final.Position.y>.9f&&Mathf.Abs(final.Position.x)<13.8f,"actual robot climbs past former crate obstruction "+side+" at "+final.Position);
            File.WriteAllLines(Folder+"/robot-ramp-"+side+".csv",trace);yield return Capture("robot-ramp-"+side);
            final.Health=0;final.RespawnAt=match.Now+600;match.Patrols[index]=final;
        }
    }
    static IEnumerator CheckSupportMarker(ArenaObjectiveMatch match)
    {
        bool enabled=match.enabled;match.enabled=false;
        var views=(Dictionary<int,ArenaPatrolEnemy>)typeof(ArenaObjectiveMatch).GetField("patrolViews",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(match);
        const int id=90001;var sprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
        var go=new GameObject("Support guide interpolation fixture");go.transform.SetParent(match.transform,false);var view=go.AddComponent<ArenaPatrolEnemy>();view.Initialize(match,id,sprite,false,true,true);views.Add(id,view);
        int index=match.Patrols.Count;var state=new ArenaObjectiveMatch.PatrolState{Id=id,Generation=1,Health=8,Roaming=true,Flying=true,Medical=true,Position=new Vector2(0,10),Speed=3.2f};match.Patrols.Add(state);view.Apply(state,true,8);
        bool followed=true,interpolated=false;var trace=new List<string>{"frame,stateX,viewX,markerX"};
        for(int i=0;i<10;i++)
        {
            if(i%3==0){state.Position+=Vector2.right*.5f;match.Patrols[index]=state;}
            view.Apply(state,true,8);bool found=match.TrySupportGuideWorld(out var marker);var rendered=(Vector2)view.transform.TransformPoint(new Vector3(0,ArenaObjectiveMatch.SupportGuideHeight,0));
            followed&=found&&Vector2.Distance(marker,rendered)<.0001f&&marker.y>view.transform.position.y+1.3f;interpolated|=Mathf.Abs(marker.x-state.Position.x)>.05f;trace.Add(i+","+state.Position.x+","+view.transform.position.x+","+marker.x);yield return null;
        }
        Check(followed&&interpolated,"support marker follows rendered drone on every frame between server snapshots");File.WriteAllLines(Folder+"/support-marker-tracking.csv",trace);
        var camera=Camera.main;var savedCamera=camera.transform.position;var centre=camera.ViewportToWorldPoint(new Vector3(.5f,.5f,-savedCamera.z));
        var centredGuide=centre+Vector3.up*(ArenaObjectiveMatch.SupportGuideHeight-.5f);
        Check(ArenaCombatHud.SupportBodyVisible(centredGuide),"visible support body suppresses redundant marker");
        Check(!ArenaCombatHud.SupportBodyVisible(centredGuide+Vector3.right*camera.orthographicSize*camera.aspect*3),"offscreen support retains shield direction marker");
        var behind=centredGuide;behind.z=savedCamera.z-1;Check(!ArenaCombatHud.SupportBodyVisible(behind),"support behind camera does not count as visible");
        match.Patrols.RemoveAt(index);views.Remove(id);UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(sprite);match.enabled=enabled;
    }
    static void CheckStaticText()
    {
        var skin=(GUISkin)typeof(ArenaUiTheme).GetField("skin",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
        Check(skin!=null&&skin.label.hover.background==skin.label.normal.background&&skin.label.hover.textColor==skin.label.normal.textColor&&skin.box.hover.background==skin.box.normal.background,"plain labels and panels have no hover highlight");
        var custom=ArenaUiTheme.Static(new GUIStyle(skin.label){normal={textColor=Color.cyan}});Check(custom.normal.textColor==custom.hover.textColor&&custom.normal.textColor==custom.active.textColor,"colored headings also remain static on hover");
        Check(skin.button.normal.background!=skin.button.hover.background,"interactive buttons retain hover feedback");
        Check(skin.textField.hover.background==skin.textField.normal.background&&skin.textField.active.background==skin.textField.normal.background&&skin.textField.focused.scaledBackgrounds.Length==0,"room name field uses flat hover and focus without inherited bevel textures");
    }
    static IEnumerator Run(SideScrollNetworkSession session)
    {
        var initialView=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
        var initialIndex=initialView.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
        int savedIndex=(int)initialIndex.GetValue(initialView);
        initialView.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).Invoke(initialView,new object[]{new Vector2(1366,768),"HUD review 1366"});yield return new WaitForSeconds(.4f);
        yield return Capture("lobby");CheckStaticText();
        session.StartSoloPractice();yield return Wait(()=>ArenaObjectiveMatch.PracticeRoom,"host");var match=ArenaObjectiveMatch.Instance;
        yield return new WaitForSeconds(.5f);Check(UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>().GrenadeRounds==4,"ready room starts with four grenades");yield return Capture("ready-room");
        var roomArena=RelayArenaPrototype.Instance;roomArena.RequestLeave();yield return Frames();roomArena.CancelLeave();Check(!roomArena.MenuOpen&&!roomArena.LeaveConfirmationOpen&&Unity.Netcode.NetworkManager.Singleton.IsListening,"cancel from ready room closes dialog without opening settings");yield return Capture("ready-cancel-restored");
        match.SetLocalReady(true);yield return Wait(()=>match.CanStart,"ready");if(!match.StartRound())throw new Exception("start");
        yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Countdown,"countdown",15);yield return Frames();Check(ArenaCombatHud.UsesGameUi&&!ArenaCombatHud.Active,"modern UI policy remains active before combat");
        initialView.Focus();var staged=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>();var stagedAim=staged.GetComponentInChildren<RightFacingAimRig>();
        // The room camera is still travelling from x100 immediately after the spawn.
        // Wait until the character is framed before comparing two on-screen mouse targets.
        yield return Wait(()=>Mathf.Abs(Camera.main.transform.position.x-staged.transform.position.x)<4,"countdown camera frames player",2);
        InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(Screen.width*.85f,Screen.height*.8f)});yield return Frames(3);var firstAim=stagedAim.AimDirection;
        InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(Screen.width*.1f,Screen.height*.8f),buttons=1});int stagedShots=staged.ReceivedShots;float stagedX=staged.transform.position.x;Keys(UnityEngine.InputSystem.Key.D,UnityEngine.InputSystem.Key.Space);yield return Frames(3);
        results.Add("INFO countdown aim first="+firstAim+" second="+stagedAim.AimDirection+" mouse="+stagedAim.mouseAim+" owner="+staged.IsOwner+" block="+RelayArenaPrototype.AimInputBlocked+" focus="+RelayArenaPrototype.HasGameplayFocus+" player="+staged.transform.position+" camera="+Camera.main.transform.position);
        Check(Vector2.Angle(firstAim,stagedAim.AimDirection)>20&&!RelayArenaPrototype.AimInputBlocked,"countdown aim follows actual mouse input");
        Check(RelayArenaPrototype.ControlsBlocked&&RelayArenaPrototype.FireInputBlocked&&staged.ReceivedShots==stagedShots&&Mathf.Abs(staged.transform.position.x-stagedX)<.04f,"countdown keeps firing and movement locked");Keys();InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(Screen.width*.8f,Screen.height*.65f)});yield return Capture("countdown-clean");
        yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing,"playing",25);
        var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.Focus();yield return Frames();
        var player=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>();var arena=RelayArenaPrototype.Instance;
        // Quiet solo fixture for readable captures; no enemy production settings change.
        for(int i=0;i<match.Patrols.Count;i++){var e=match.Patrols[i];e.Health=0;e.RespawnAt=match.Now+600;match.Patrols[i]=e;}
        match.TurretShots.Clear();var transit=match.MedicalTransit.Value;transit.NextAt=match.Now+600;match.MedicalTransit.Value=transit;
        yield return CheckSupportMarker(match);
        yield return CheckCoverSupport(match,player);
        yield return CheckRampRobots(match,player);
        player.ResetForArenaRound();yield return Frames();
        player.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(0,6.84f));
        InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(Screen.width*.65f,Screen.height*.5f)});Keys();yield return Frames(12);
        Check(ArenaCombatHud.Active&&!ArenaCombatHud.TabVisible,"compact HUD active in solo combat");
        Check(player.GrenadeRounds==4&&SideScrollNetworkPlayer.GrenadeCapacity==4,"combat starts with four grenades");
        Check(Mathf.Abs(arena.weaponRange-16)<.001f&&Mathf.Abs(player.GetComponentInChildren<RightFacingAimRig>().previewAimRange-16)<.001f,"normal server and visual range uses sixteen");
        Check(arena.hudPortraits.Length==4&&Array.TrueForAll(arena.hudPortraits,p=>p!=null),"four actual palette portraits assigned");
        yield return new WaitForSeconds(1.2f);yield return Capture("battle");
        match.enabled=false;for(int i=2;i<=4;i++)match.Scores.Add(new ArenaObjectiveMatch.Score{Client=(ulong)(100+i),Number=i,Seconds=i*12,Kills=i,KillPoints=i*5,DronePoints=i*5});
        var ranked=ArenaCombatHud.RankedScores(match);Check(ranked.Length==4&&ranked[0].Number==4,"current ranking orders score totals correctly");yield return Frames();yield return Capture("ranking-four-fixture");
        Keys(UnityEngine.InputSystem.Key.Tab);yield return Frames();yield return Capture("portraits-four-fixture");Keys();yield return Frames();while(match.Scores.Count>1)match.Scores.RemoveAt(match.Scores.Count-1);match.enabled=true;

        Keys(UnityEngine.InputSystem.Key.Tab);yield return Frames();
        Check(ArenaCombatHud.TabVisible&&RelayArenaPrototype.FireInputBlocked&&!arena.overview,"held physical Tab opens panel and blocks firing without overview camera");
        Check(!player.AmmoCursorVisible&&!player.WeaponGuideVisible,"crosshair and range guides hidden behind Tab");
        float size=arena.arenaCamera.orthographicSize;
        yield return Capture("tab");
        int shots=player.ReceivedShots;
        InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(Screen.width*.65f,Screen.height*.5f),buttons=1});yield return Frames(12);
        Check(player.ReceivedShots==shots,"Tab blocks held primary fire");
        Keys();yield return Frames(8);Check(!ArenaCombatHud.TabVisible&&player.ReceivedShots==shots,"Tab release closes panel without firing through held mouse");
        Check(Mathf.Abs(arena.arenaCamera.orthographicSize-size)<.001f,"Tab preserves gameplay camera zoom");
        InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(Screen.width*.65f,Screen.height*.5f)});yield return Frames();
        InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(Screen.width*.65f,Screen.height*.5f),buttons=1});yield return Frames(8);
        Check(player.ReceivedShots>shots,"new primary press fires after closing Tab");
        InputSystem.QueueStateEvent(Mouse.current,new MouseState());Keys(UnityEngine.InputSystem.Key.E);yield return Frames();Keys();yield return Frames();
        var selected=player.SelectedAmmo;Check(ArenaCombatHud.Slot(selected)==2,"E preserves existing special selection cycle");
        InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(Screen.width*.65f,Screen.height*.5f),buttons=2});yield return Frames(4);
        Check(player.WeaponGuideVisible&&player.RifleActive,"RMB rifle guide remains available with compact cursor");
        Check(arena.ammoCursorShader!=null&&arena.ammoCursorShader.isSupported,"analytic ammo cursor shader supported");
        foreach(var kind in new[]{SideScrollNetworkPlayer.AmmoKind.InstantShotgun,SideScrollNetworkPlayer.AmmoKind.Rifle,SideScrollNetworkPlayer.AmmoKind.Grenade})
        {
            player.SelectAmmo(kind);yield return new WaitForSeconds(.25f);
            Check(player.SpecialHeld&&player.SelectedAmmo==kind&&player.AmmoCursorVisible,"segmented cursor active for "+kind);yield return Capture("cursor-"+kind);
        }
        var grenades=(Unity.Netcode.NetworkVariable<int>)typeof(SideScrollNetworkPlayer).GetField("grenadeRounds",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(player);
        int restoreGrenades=grenades.Value;grenades.Value=0;yield return Frames();Check(player.ActiveAmmoEmpty,"empty special round state");yield return Capture("cursor-empty");Check(player.GrantSpecialAmmoServer(ArenaObjectiveMatch.SupplyAmmo.Grenade,1,player.LifeVersion)==1&&player.GrenadeRounds==1,"grenade supply still grants one round");
        Check(player.GrantSpecialAmmoServer(ArenaObjectiveMatch.SupplyAmmo.Grenade,99,player.LifeVersion)==3&&player.GrenadeRounds==4,"server caps grenade inventory at four");
        Check(player.GrantSpecialAmmoServer(ArenaObjectiveMatch.SupplyAmmo.Grenade,1,player.LifeVersion)==0,"full grenade inventory rejects supply");grenades.Value=restoreGrenades;
        player.SelectAmmo(selected);yield return Frames();

        InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return Frames();
        var sizeProperty=view.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
        int oldSize=(int)sizeProperty.GetValue(view);
        var setSize=view.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
        setSize.Invoke(view,new object[]{new Vector2(1366,768),"HUD review 1366"});yield return new WaitForSeconds(.6f);
        Check(Screen.width==1366&&Screen.height==768,"actual 1366x768 render size");
        InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(Screen.width*.65f,Screen.height*.5f)});yield return Frames();
        yield return Capture("battle-1366");Keys(UnityEngine.InputSystem.Key.Tab);yield return Frames();yield return Capture("tab-1366");Keys();yield return Frames();
        sizeProperty.SetValue(view,oldSize);yield return Frames();
        Keys(UnityEngine.InputSystem.Key.Tab);yield return Frames();arena.SetMenuOpen(true);yield return Frames();Check(!ArenaCombatHud.TabVisible,"ESC menu takes priority over Tab");
        arena.SetMenuOpen(false);Keys();yield return Frames();
        Check(player.SelectedAmmo==selected,"special selection remembered across information panel");
        foreach(var point in match.Points)Check(point.label==null||!point.label.gameObject.activeSelf,"world site text hidden: "+point.name);
        player.ReceiveServerDamage(Mathf.Max(0,player.Health-90),player.LifeVersion,null);
        Check(player.GrantShieldServer(player.LifeVersion),"shield granted for visual and clock test");yield return new WaitForSeconds(.5f);yield return Capture("shield-and-world-bars");
        // Display-only critical state fixture; restore health, shield and end time.
        double savedEnd=match.State.Value.End;var urgent=match.State.Value;urgent.End=match.Now+9;match.State.Value=urgent;
        player.ReceiveServerDamage(player.Shield+Mathf.Max(0,player.Health-22),player.LifeVersion,null);yield return Frames();
        Check(player.Health==22&&player.Shield==0,"critical HUD fixture uses actual health and shield values");yield return Capture("instrument-critical");
        player.GrantHealthServer(68,player.LifeVersion);player.GrantShieldServer(player.LifeVersion);urgent=match.State.Value;urgent.End=savedEnd;match.State.Value=urgent;yield return Frames();
        var originalWindow=match.MedicalTransit.Value;var markerWindow=originalWindow;markerWindow.Valid=true;markerWindow.Entry=new Vector2(-36,10);markerWindow.NextAt=match.Now+2.5;match.MedicalTransit.Value=markerWindow;
        setSize.Invoke(view,new object[]{new Vector2(1920,1080),"HUD review 1920"});yield return new WaitForSeconds(.5f);yield return Capture("hud-large-1920");Check(ArenaCombatHud.UiScale>1,"HUD scales up on high resolution screens");
        setSize.Invoke(view,new object[]{new Vector2(1366,768),"HUD review 1366"});yield return new WaitForSeconds(.4f);match.MedicalTransit.Value=originalWindow;

        double gameBefore=match.Now;float shieldBefore=player.ShieldRemaining;double netBefore=Unity.Netcode.NetworkManager.Singleton.ServerTime.Time;
        debugPaused=true;resumeAt=EditorApplication.timeSinceStartup+2;EditorApplication.isPaused=true;yield return Frames(4);
        Check(match.Now-gameBefore<.8&&shieldBefore-player.ShieldRemaining<.8,"Editor solo pause does not skip match or shield time on resume");
        Check(Unity.Netcode.NetworkManager.Singleton.ServerTime.Time-netBefore>1.5,"pause test reproduces NGO time catch-up while gameplay time stays stable");
        var soloProperty=typeof(SideScrollNetworkSession).GetProperty("SoloPractice");soloProperty.SetValue(session,false);Check(Math.Abs(ArenaClock.Now(Unity.Netcode.NetworkManager.Singleton)-Unity.Netcode.NetworkManager.Singleton.ServerTime.Time)<.0001,"ordinary sessions retain exact NGO server clock");soloProperty.SetValue(session,true);
        arena.SetMenuOpen(true);float savedVolume=AudioListener.volume;bool savedVolumeKey=PlayerPrefs.HasKey("RightWalk.MasterVolume");arena.SetMasterVolume(.35f);Check(Mathf.Abs(AudioListener.volume-.35f)<.001f&&Mathf.Abs(PlayerPrefs.GetFloat("RightWalk.MasterVolume")-.35f)<.001f,"master volume applies and saves");arena.SetMasterVolume(savedVolume);if(!savedVolumeKey)PlayerPrefs.DeleteKey("RightWalk.MasterVolume");yield return Frames();yield return Capture("settings");arena.RequestLeave();yield return Frames();Check(arena.LeaveConfirmationOpen&&Unity.Netcode.NetworkManager.Singleton.IsListening,"request leave waits for confirmation");yield return Capture("leave-confirmation");arena.CancelLeave();Check(!arena.LeaveConfirmationOpen&&arena.MenuOpen&&Unity.Netcode.NetworkManager.Singleton.IsListening,"cancel from settings returns to previous settings without disconnect");arena.SetMenuOpen(false);

        var finish=match.State.Value;finish.End=match.Now+.15;match.State.Value=finish;yield return Wait(()=>ArenaObjectiveMatch.Ended,"finish");yield return Frames();yield return Capture("results");
        var originalResult=match.Result.Value;for(int i=2;i<=4;i++)match.Scores.Add(new ArenaObjectiveMatch.Score{Client=(ulong)(100+i),Number=i,Seconds=i*12,Kills=i,KillPoints=i*5,DronePoints=i*5});match.Result.Value=new Unity.Collections.FixedString128Bytes("배치 검사 · P4 승리");yield return Frames();yield return Capture("results-four-fixture");while(match.Scores.Count>1)match.Scores.RemoveAt(match.Scores.Count-1);match.Result.Value=originalResult;
        match.ReturnToRoom();yield return Wait(()=>ArenaObjectiveMatch.InReadyRoom,"return room",15);yield return Frames();
        Check(!ArenaCombatHud.Active&&!ArenaCombatHud.TabVisible,"ready room retains its existing controls UI");
        arena.RequestLeave();arena.ConfirmLeave();yield return Frames();Check(!ArenaCombatHud.Active&&!Unity.Netcode.NetworkManager.Singleton.IsListening,"confirmed leave disconnects to lobby");
        initialIndex.SetValue(initialView,savedIndex);
    }
}
