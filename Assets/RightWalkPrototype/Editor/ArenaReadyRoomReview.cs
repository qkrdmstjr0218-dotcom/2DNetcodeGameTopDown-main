using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using RightWalkPrototype;

[InitializeOnLoad]
public static partial class ArenaReadyRoomReview
{
    const string Request="Assets/RightWalkPrototype/Tests/ReadyRoomReviewRequest.json";
    [Serializable] class Settings {public string folder;public bool spectatorOnly,normalOnly,practiceOnly,pickupsOnly,enemiesOnly,instantOnly,rifleOnly,grenadeOnly,foundryOnly;}
    [Serializable] class Command {public int id,target,action;public bool ready;public uint generation;}
    [Serializable] class Peer
    {
        public bool palettes;
        public bool online,relay;public string onlineRoom;
        public bool ammoHint;public string ammoHintText;
        public int entryCount,entryShapes,entryWarnings,groundEntrances,turretEntrances;
        public float westCycle,westWidth;
        public float walkSetting,runSetting;public bool objectivePreview;public int objectivePoint,objectiveNext;public int medStations,supplyS,supplyR,supplyG;public int shield,clientShield;public float shieldTime;public int medicCount,medicHealth,medicId;public Vector2 medicPosition,medicEntry,medicExit;public double medicNextAt;public bool medicWarning;public int clientKills;public float hostScore,clientScore,killReward;public bool warningAmmoOn,warningAmmoOff,warningHealthOn,warningHealthOff;public int droneCount,droneHealth,droneGeneration;public Vector2 dronePosition;public int turretCount,turretHealth,turretGeneration,turretShots;public double turretFireAt;
        public int healthCount,healthAvailable;
        public int role,phase=-1,roster,mask,hp,ammo,shots,spectator,climbed,normalCapacity,pickupsAvailable,pickupsCount,enemiesCount,enemyHealth,enemyGeneration,instantAmmo,instantShots,hostInstantCount,selectedAmmo,rifleAmmo,rifleShots,grenadeAmmo,grenadeShots,grenadeBursts,hostGrenadeBursts,grenadePushes;public Vector2 grenadeImpulse;public Vector2 hostGrenadePoint,predictedGrenade;public bool predictedGrenadeHit;public float[] instantAngles;public Vector3 enemyPosition;public uint life;public bool connected,denied,blocked,grounded,ammoCursor,osCursor,worldStamina,hitReacting,sawHit;public float energy,delay,countdown,normal,normalDelay,normalRate,shotInterval,velocityX,velocityY,lastHitX,lastHitY;public string notice,cursor,support;public Vector3 position;public double end;
    }
    static bool started,spectatorOnly,normalOnly,practiceOnly,pickupsOnly,enemiesOnly,instantOnly,rifleOnly,grenadeOnly,foundryOnly;static string folder;static int revision;
    static readonly List<string> passed=new();
    static readonly Dictionary<int,Peer> cache=new();
    static ArenaReadyRoomReview(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/Right Walk Prototype/Verify Ready Room")]
    public static void Begin(){BeginReview(false);}
    [MenuItem("Tools/Right Walk Prototype/Verify Respawn Spectator")]
    public static void BeginSpectator(){BeginReview(true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Normal Ammo")]
    public static void BeginNormal(){BeginReview(false,true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Ready Room Practice")]
    public static void BeginPractice(){BeginReview(false,false,true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Ammo Pickups")]
    public static void BeginPickups(){BeginReview(false,false,false,true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Patrol Enemies")]
    public static void BeginEnemies(){BeginReview(false,false,false,false,true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Instant Shotgun")]
    public static void BeginInstant(){BeginReview(false,false,false,false,false,true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Piercing Rifle")]
    public static void BeginRifle(){BeginReview(false,false,false,false,false,false,true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Grenade Launcher")]
    public static void BeginGrenade(){BeginReview(false,false,false,false,false,false,false,true);}
    static void BeginReview(bool focus,bool normal=false,bool practice=false,bool pickups=false,bool enemies=false,bool instant=false,bool rifle=false,bool grenade=false,bool foundry=false)
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Requires saved edit mode");
        folder=Path.GetFullPath("LocalTools/Analysis/NetworkCombat/room-"+DateTime.UtcNow.ToString("HHmmss"));Directory.CreateDirectory(folder);
        File.WriteAllText(Request,JsonUtility.ToJson(new Settings{folder=folder,spectatorOnly=focus,normalOnly=normal,practiceOnly=practice,pickupsOnly=pickups,enemiesOnly=enemies,instantOnly=instant,rifleOnly=rifle,grenadeOnly=grenade,foundryOnly=foundry}));AssetDatabase.ImportAsset(Request);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying){started=false;return;}
        if(started||EditorApplication.isCompiling||Time.time<.4f||!File.Exists(Request))return;
        var session=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();if(session==null)return;
        started=true;var settings=JsonUtility.FromJson<Settings>(File.ReadAllText(Request));folder=settings.folder;spectatorOnly=settings.spectatorOnly;normalOnly=settings.normalOnly;practiceOnly=settings.practiceOnly;pickupsOnly=settings.pickupsOnly;enemiesOnly=settings.enemiesOnly;instantOnly=settings.instantOnly;rifleOnly=settings.rifleOnly;grenadeOnly=settings.grenadeOnly;foundryOnly=settings.foundryOnly;revision=0;passed.Clear();cache.Clear();dropAmmoOn=dropAmmoOff=dropHealthOn=dropHealthOff=false;medicalTouch=false;
        bool main=Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor;session.useReadyRoom=!(File.Exists(folder+"/solo-performance.txt")||File.Exists(folder+"/entrance-cover.txt"));if(File.Exists(folder+"/online.txt"))session.StartCoroutine(OnlineBoot(session,main));else if(main&&File.Exists(folder+"/weapon-guides.txt"))session.StartSoloPractice();else session.StartLocal(main);session.StartCoroutine(Actor(session,main));if(main)session.StartCoroutine(Run(session));
    }
    static string Read(string path){using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var r=new StreamReader(f))return r.ReadToEnd();}
    static Peer P(int role){try{var peer=JsonUtility.FromJson<Peer>(Read(folder+"/peer-"+role+".json"));if(peer!=null&&peer.role==role)cache[role]=peer;}catch{}return cache.TryGetValue(role,out var found)?found:null;}
    static bool All(int count,Func<Peer,bool> check){for(int i=1;i<=count;i++){var p=P(i);if(p==null||!check(p))return false;}return true;}
    static SideScrollNetworkPlayer Player(int number){foreach(var p in UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsSpawned&&p.PlayerNumber==number)return p;return null;}
    static void Send(int target,int action,bool ready=false){File.WriteAllText(folder+"/command.json",JsonUtility.ToJson(new Command{id=++revision,target=target,action=action,ready=ready}));}
    static IEnumerator Actor(SideScrollNetworkSession session,bool main)
    {
        int role=0,last=-1,climbed=0,climbState=0,lastHostInstant=-1;float[] observedAngles=null;bool denied=false,climbing=false,firing=false,directed=false,trackingEnemy=false,followingEnemy=false,awaitingContact=false,sawHit=false;float sampleSpecialUntil=0;int sampleStopAmmo=-1;bool sampleRifle=false;float lastHitX=0,lastHitY=0,write=0,moveUntil=0,climbBegan=0,sprintUntil=0,walkDirection=1;
        while(EditorApplication.isPlaying)
        {
            if(File.Exists(folder+"/stop.txt")){if(!main)session.DisconnectLocal();yield break;}
            var manager=NetworkManager.Singleton;SideScrollNetworkPlayer own=null;
            foreach(var p in UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsOwner&&p.IsSpawned)own=p;
            if(role==0&&own!=null)role=own.PlayerNumber;
            var match=ArenaObjectiveMatch.Instance;
            if(own!=null)
            {
                own.GetComponentInChildren<RightFacingShotPreview>().readMouse=false;own.GetComponentInChildren<RightFacingAimRig>().mouseAim=false;
                if(!foundryDriving)own.GetComponent<SideScrollMapPreview>().SetInput(Time.realtimeSinceStartup<moveUntil?walkDirection:0,false,Time.realtimeSinceStartup<sprintUntil);
            }
            Command cmd=null;try{cmd=JsonUtility.FromJson<Command>(Read(folder+"/command.json"));}catch{}
            if(cmd!=null&&cmd.id!=last&&(cmd.target==0||cmd.target==role)&&role!=0)
            {
                last=cmd.id;
                if(cmd.action>=16&&cmd.action<=22&&cmd.action!=20)firing=false;
                if(cmd.action==60&&own!=null)session.StartCoroutine(FoundryClient(own));
                if(cmd.action==61&&own!=null)session.StartCoroutine(FoundryClient(own,true));
                if(cmd.action==62&&own!=null)session.StartCoroutine(FoundrySlopeClient(own));
                if(cmd.action==64&&own!=null)session.StartCoroutine(FoundryTraversalClient(own));
                if(cmd.action==65&&own!=null)session.StartCoroutine(FoundrySeamClient(own));
                if(cmd.action==66&&own!=null)session.StartCoroutine(WestBridgeClient(own));
                if(cmd.action>=70&&cmd.action<=74&&own!=null)session.StartCoroutine(TurretClient(own,cmd.action,cmd.ready));
                if(cmd.action>=75&&cmd.action<=77&&own!=null)session.StartCoroutine(ReadyCoverClient(own,cmd.action));
                if(cmd.action>=120&&cmd.action<=125&&own!=null)AppearanceCommand(own,cmd.action);
                if(cmd.action==119&&!main)session.StartCoroutine(OnlineRejoin(session));
                if(cmd.action==115)session.StartCoroutine(OnlineQueryReview(session,cmd.id));
                if(cmd.action==116)_=session.LeaveOnlineRoomAsync();
                if(cmd.action==117)session.StartCoroutine(OnlineRejoin(session));
                if(cmd.action==113&&own!=null)session.StartCoroutine(SpecialCycleClient(own));
                if(cmd.action==114&&own!=null)session.StartCoroutine(SolidRestClient(own));
                if(cmd.action==112&&own!=null)session.StartCoroutine(BalconyClient(own));
                if(cmd.action==110&&own!=null)own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(30,.04f));
                if(cmd.action==111&&own!=null)own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(-18,.04f));
                if(cmd.action==109&&own!=null)own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(-7,3.64f));
                if(cmd.action>=102&&cmd.action<=106&&own!=null)SupplyCommand(own,cmd.action);
                if(cmd.action==78&&own!=null)own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(cmd.ready?4.6f:0,3.64f));
                if((cmd.action==79||cmd.action==80)&&own!=null)own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(cmd.action==80?-16:-18,.04f));
                if(cmd.action>=97&&cmd.action<=100&&own!=null)MedicalCommand(own,cmd.action,cmd.ready);
                if(cmd.action>=87&&cmd.action<=95&&own!=null)KillRushCommand(own,cmd.action);
                if(cmd.action>=81&&cmd.action<=86&&own!=null)NavigationCommand(own,cmd.action);
                if(cmd.action==1&&match!=null)match.SetLocalReady(cmd.ready);
                if(cmd.action==2&&match!=null)denied=!match.StartRound();
                if(cmd.action==3)session.DisconnectLocal();
                if(cmd.action==4)session.StartLocal(false);
                if(cmd.action==5)moveUntil=Time.realtimeSinceStartup+.7f;
                if(cmd.action==7&&own!=null)own.GetComponent<SideScrollMapPreview>().SetInput(0,true);
                if(cmd.action==8&&own!=null)own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(role==1?93:107,.04f));
                if(cmd.action==9&&own!=null)own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(ArenaReadyRoom.Spawn(own.PlayerNumber));
                if(cmd.action==10)RelayArenaPrototype.Instance.CycleSpectator(1);
                if(cmd.action==11&&own!=null)own.RequestRevive();
                if(cmd.action==12){RelayArenaPrototype.Instance.overview=true;session.StartCoroutine(CaptureSpectator());}
                if(cmd.action==13&&own!=null){climbing=true;climbed=0;climbState=0;own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(role==1?93:107,.04f));}
                if(cmd.action==15&&own!=null){directed=false;own.SetSpecialHeld(false);own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Normal);firing=true;}
                if(cmd.action==23&&own!=null){directed=true;firing=true;own.SetSpecialHeld(false);}
                if(cmd.action==24){walkDirection=1;moveUntil=sprintUntil=Time.realtimeSinceStartup+.8f;}
                if((cmd.action==29||cmd.action==30||cmd.action==33)&&own!=null&&match.Patrols.Count>0)
                {
                    followingEnemy=false;trackingEnemy=true;firing=cmd.action!=33;
                    own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(match.PatrolPosition(match.Patrols[0])+Vector3.left*(cmd.action==29?4:2));
                    own.SelectAmmo(cmd.action==30?SideScrollNetworkPlayer.AmmoKind.Shotgun:SideScrollNetworkPlayer.AmmoKind.Normal);own.SetSpecialHeld(cmd.action==30);
                }
                if(cmd.action==35&&own!=null)
                {
                    firing=false;followingEnemy=false;awaitingContact=false;trackingEnemy=false;
                    float x=role==1?(cmd.ready?-6:-10):role==2?(cmd.ready?-10:-6):role==3?0:10;
                    own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(x,.04f));
                    own.GetComponentInChildren<RightFacingAimRig>().SetAimDirection(cmd.ready&&role==1?Vector2.left:Vector2.right);
                }
                if((cmd.action==36||cmd.action==37)&&own!=null)
                {
                    own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.InstantShotgun);own.SetSpecialHeld(true);
                    own.GetComponentInChildren<RightFacingAimRig>().SetAimDirection(cmd.action==37?Vector2.left:Vector2.right);
                    bool instantVisual=own.InstantShotgunActive;
                    session.StartCoroutine(FireAfterSwitch(own,main&&instantVisual,false));
                }
                if(cmd.action==42&&own!=null)
                {firing=false;followingEnemy=false;trackingEnemy=false;awaitingContact=false;own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(cmd.ready?6-(role-1)*4:-6+(role-1)*4,.04f));own.GetComponentInChildren<RightFacingAimRig>().SetAimDirection(cmd.ready&&role!=4?Vector2.left:Vector2.right);}
                if(cmd.action==43&&own!=null)
                {own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Rifle);own.SetSpecialHeld(true);session.StartCoroutine(FireAfterSwitch(own,false,main&&own.RifleActive));}
                if(cmd.action==46&&own!=null&&match.Patrols.Count>0)
                {
                    firing=false;followingEnemy=false;var target=match.PatrolPosition(match.Patrols[0]);
                    own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(target+Vector3.left*3);
                    own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Rifle);own.SetSpecialHeld(true);
                    own.GetComponentInChildren<RightFacingAimRig>().SetAimTarget((Vector2)target+Vector2.up*.45f);session.StartCoroutine(FireAfterSwitch(own,false,false));
                }
                if((cmd.action==47||cmd.action==48)&&own!=null)
                {firing=false;sampleSpecialUntil=Time.realtimeSinceStartup+3.4f;sampleRifle=cmd.action==48;int rounds=sampleRifle?own.RifleRounds:own.ShotgunRounds;sampleStopAmmo=rounds>0?Mathf.Max(0,rounds-2):-1;own.SelectAmmo(cmd.action==47?SideScrollNetworkPlayer.AmmoKind.InstantShotgun:SideScrollNetworkPlayer.AmmoKind.Rifle);}
                if(cmd.action==49&&own!=null){sampleSpecialUntil=0;SampleFireButtons(own,false,false);}
                if(cmd.action==50&&own!=null)
                {own.SetSpecialHeld(false);own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Normal);session.StartCoroutine(CaptureNormalProjectile(own));}
                if(cmd.action==51&&own!=null)
                {
                    firing=false;followingEnemy=false;trackingEnemy=false;awaitingContact=false;sampleSpecialUntil=0;
                    float x=cmd.ready?(role==4?-10:role==1?-6:role==2?-5:6):(role==1?-10:role==2?-6:role==3?-5:6);
                    own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(x,.04f));own.SetSpecialHeld(false);own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Normal);own.GetComponentInChildren<RightFacingAimRig>().SetAimTarget(new Vector2(-6,.9f));
                }
                if(cmd.action==54&&own!=null)
                {float x=role==1?-6:role==2?-2.2f:role==3?6:10;own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(x,.04f));own.GetComponentInChildren<RightFacingAimRig>().SetAimTarget(new Vector2(-2,1.3f));}
                if((cmd.action==52||cmd.action==53||cmd.action==55||cmd.action==56||cmd.action==57)&&own!=null)session.StartCoroutine(FireGrenadeReview(own,cmd.action,main));
                if(cmd.action==44&&own!=null){own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Rifle);}
                if(cmd.action==40&&own!=null){own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Normal);own.CycleAmmo(1);}
                if(cmd.action==41&&own!=null)own.CycleAmmo(1);
                if(cmd.action==39&&own!=null)
                {firing=false;own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(role==1?-6:role==2?-2:role==3?0:10,.04f));own.GetComponentInChildren<RightFacingAimRig>().SetAimDirection(Vector2.right);}
                if(cmd.action==34&&own!=null&&match.Patrols.Count>0)
                {followingEnemy=false;firing=false;awaitingContact=true;own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(match.PatrolPosition(match.Patrols[0])+Vector3.left*.15f);}
                if(cmd.action==31){firing=false;followingEnemy=true;}
                if(cmd.action==27&&own!=null)own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(cmd.ready?-8:8,.04f));
                if(cmd.action==28&&own!=null){followingEnemy=false;own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(0,.04f));}
                if(cmd.action==26&&own!=null){own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(role==1?95.8f:104.2f,.04f));walkDirection=role==1?-1:1;moveUntil=Time.realtimeSinceStartup+2.1f;}
                if(cmd.action==25&&own!=null)own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(role==1?98:role==2?102:106+role*.3f,.04f));
                if(cmd.action==16)firing=false;
                if(cmd.action==17&&main)
                {
                    var gameView=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");EditorWindow.GetWindow(gameView).Focus();
#if ENABLE_INPUT_SYSTEM
                    UnityEngine.InputSystem.Mouse.current?.WarpCursorPosition(new Vector2(Screen.width*.57f,Screen.height*.48f));
#endif
                    session.StartCoroutine(CaptureNormal("cursor-"+cmd.id));
                }
                if(cmd.action==18&&own!=null)
                {
                    var a=own.GetComponentInChildren<RightFacingAimRig>();
                    for(int i=0;i<5;i++){InvokeNormalShot(own,own.LifeVersion);typeof(SideScrollNetworkPlayer).GetMethod("FireServerRpc",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(own,new object[]{(Vector2)a.MuzzlePosition,a.AimDirection});}
                }
                if(cmd.action==19&&own!=null)InvokeNormalShot(own,cmd.generation);
                if(cmd.action==20&&own!=null){own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Shotgun);own.SetSpecialHeld(true);firing=true;}
                if(cmd.action==21&&own!=null){own.SetSpecialHeld(false);firing=false;}
                if(cmd.action==22)RelayArenaPrototype.Instance.SetMenuOpen(cmd.ready);
                if(cmd.action==6&&own!=null)
                {
                    own.GetComponentInChildren<RightFacingShotPreview>().Fire();
                    own.SetSpecialHeld(true);own.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Shotgun);own.GetComponentInChildren<RightFacingShotPreview>().Fire();own.SetSpecialHeld(false);
                }
            }
            if(own!=null&&Time.realtimeSinceStartup<sampleSpecialUntil&&(sampleStopAmmo<0||(sampleRifle?own.RifleRounds:own.ShotgunRounds)>sampleStopAmmo))SampleFireButtons(own,true,true);
            if(awaitingContact&&own!=null&&match!=null&&match.Patrols.Count>0)
            {
                var move=own.GetComponent<SideScrollMapPreview>();
                if(own.Health<100||move.HitReacting)awaitingContact=false;
                else move.SetSpawnPosition(match.PatrolPosition(match.Patrols[0])+Vector3.left*.15f);
            }
            if(followingEnemy&&own!=null&&match!=null&&match.Patrols.Count>0)own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(match.PatrolPosition(match.Patrols[0]));
            if(own!=null&&match!=null&&File.Exists(folder+"/medical.txt"))FollowMedicalForReview(own,match);
            if(own!=null&&match!=null&&File.Exists(folder+"/navigation.txt"))NavigationShoot(own,match);
            if(firing&&own!=null)
            {
                var aim=own.GetComponentInChildren<RightFacingAimRig>();
                if(trackingEnemy&&match!=null&&match.Patrols.Count>0)aim.SetAimTarget((Vector2)match.PatrolPosition(match.Patrols[0])+Vector2.up*.5f);
                else aim.SetAimDirection(directed?Vector2.right:Vector2.up);
                own.GetComponentInChildren<RightFacingShotPreview>().Fire();
            }
            if(climbing&&own!=null)
            {
                var move=own.GetComponent<SideScrollMapPreview>();var body=own.GetComponent<Rigidbody2D>();
                var ledge=GameObject.Find("Jump ledge "+(role==1?"West ":"East ")+(climbed+1));
                float dx=ledge.transform.position.x-own.transform.position.x;float horizontal=Mathf.Abs(dx)>.08f?Mathf.Sign(dx):0;move.SetInput(horizontal);
                if(climbState==0&&move.Grounded){move.SetInput(horizontal,true);climbState=1;climbBegan=Time.realtimeSinceStartup;}
                else if(climbState==1&&!move.Grounded&&body.linearVelocity.y<.2f&&move.JumpCount==1&&own.GetComponent<BoxCollider2D>().bounds.min.y<ledge.GetComponent<Collider2D>().bounds.max.y+.06f){move.SetInput(horizontal,true);climbState=2;}
                if(climbState>0&&Time.realtimeSinceStartup-climbBegan>.2f&&move.Grounded&&move.GroundCollider==ledge.GetComponent<Collider2D>())
                {climbed++;climbState=0;if(climbed==3){climbing=false;move.SetInput(0);}}
            }
            if(own!=null&&own.GetComponent<SideScrollMapPreview>().HitReacting)
            {sawHit=true;lastHitX=own.GetComponent<Rigidbody2D>().linearVelocity.x;lastHitY=own.GetComponent<Rigidbody2D>().linearVelocity.y;}
            var hostPlayer=Player(1);
            if(hostPlayer!=null&&hostPlayer.ReceivedInstantBlasts!=lastHostInstant)
            {var angles=ReadInstantAngles(hostPlayer);if(angles.Length==5){observedAngles=angles;lastHostInstant=hostPlayer.ReceivedInstantBlasts;}}
            if(role!=0&&Time.realtimeSinceStartup>=write)
            {
                write=Time.realtimeSinceStartup+.08f;
                var peer=new Peer{online=session.IsOnlineConnection,onlineRoom=session.OnlineRoomId,relay=manager!=null&&manager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().Protocol==Unity.Netcode.Transports.UTP.UnityTransport.ProtocolType.RelayUnityTransport,hostInstantCount=lastHostInstant,instantAngles=observedAngles,climbed=climbed,role=role,connected=manager!=null&&manager.IsConnectedClient,denied=denied,notice=session.ConnectionNotice,cursor=Cursor.lockState.ToString(),blocked=RelayArenaPrototype.ControlsBlocked};
                var crossing=UnityEngine.Object.FindFirstObjectByType<ArenaRetractingBridge>();if(crossing!=null){peer.westCycle=crossing.CycleTime;peer.westWidth=crossing.westLeaf.localScale.x;}
                if(own!=null){peer.palettes=File.Exists(folder+"/appearance.txt")&&AllPalettesVisible();peer.ammoHint=own.AmmoSelectionHintVisible;peer.ammoHintText=own.AmmoSelectionHintText;peer.grenadePushes=own.ReceivedGrenadePushes;peer.grenadeImpulse=own.LastGrenadePush;peer.grenadeAmmo=own.GrenadeRounds;peer.grenadeShots=own.ReceivedGrenadeShots;peer.grenadeBursts=own.ReceivedGrenadeBursts;peer.predictedGrenade=grenadePredicted;peer.predictedGrenadeHit=grenadePredictionHit;peer.rifleAmmo=own.RifleRounds;peer.rifleShots=own.ReceivedRifleShots;peer.selectedAmmo=(int)own.SelectedAmmo;peer.instantAmmo=own.InstantShotgunRounds;peer.instantShots=own.ReceivedInstantBlasts;peer.sawHit=sawHit;peer.lastHitX=lastHitX;peer.lastHitY=lastHitY;peer.hitReacting=own.GetComponent<SideScrollMapPreview>().HitReacting;peer.velocityX=own.GetComponent<Rigidbody2D>().linearVelocity.x;peer.velocityY=own.GetComponent<Rigidbody2D>().linearVelocity.y;peer.shotInterval=own.NormalShotInterval;peer.worldStamina=own.WorldStaminaVisible;peer.normal=own.NormalAmmo;peer.normalCapacity=own.NormalCapacity;peer.ammoCursor=own.AmmoCursorVisible;peer.osCursor=Cursor.visible;peer.position=own.transform.position;peer.life=own.LifeVersion;peer.walkSetting=own.GetComponent<SideScrollMapPreview>().speed;peer.runSetting=own.GetComponent<RunWalkPreview>().runSpeed;peer.hp=own.Health;peer.shield=own.Shield;peer.shieldTime=own.ShieldRemaining;peer.energy=own.Stamina;peer.ammo=own.ShotgunRounds;peer.grounded=own.GetComponent<SideScrollMapPreview>().Grounded;var ground=own.GetComponent<SideScrollMapPreview>().GroundCollider;peer.support=ground!=null?ground.name:"";if(RelayArenaPrototype.Instance.WantsSpectator){var target=RelayArenaPrototype.Instance.SpectatorTarget();peer.spectator=target!=null?target.PlayerNumber:0;}}
                foreach(var player in UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(player.IsSpawned){if(player.PlayerNumber==2)peer.clientShield=player.Shield;peer.shots+=player.ReceivedShots;if(player.PlayerNumber==1){peer.hostGrenadeBursts=player.ReceivedGrenadeBursts;peer.hostGrenadePoint=player.LastGrenadeBurst;}}
                if(match!=null&&match.IsSpawned){peer.enemiesCount=match.Patrols.Count;if(match.Patrols.Count>0){peer.enemyHealth=match.Patrols[0].Health;peer.enemyGeneration=match.Patrols[0].Generation;peer.enemyPosition=match.PatrolPosition(match.Patrols[0]);}peer.pickupsCount=match.AmmoPickups.Count;foreach(var pickup in match.AmmoPickups)if(pickup.Available)peer.pickupsAvailable++;peer.normalDelay=match.NormalAmmoRules.Delay;peer.normalRate=match.NormalAmmoRules.Rate;peer.delay=match.RespawnSeconds;peer.countdown=match.CountdownSeconds;peer.phase=(int)match.State.Value.Phase;peer.end=match.State.Value.End;peer.roster=match.ReadyPlayers.Count;foreach(var p in match.ReadyPlayers)if(p.Ready)peer.mask|=1<<(p.Number-1);}
                if(match!=null&&match.IsSpawned&&File.Exists(folder+"/kill-rush.txt"))foreach(var score in match.Scores)
                {if(score.Number==1)peer.hostScore=score.Total;if(score.Number==2){peer.clientScore=score.Total;peer.clientKills=score.Kills;}}
                if(match!=null&&match.IsSpawned&&File.Exists(folder+"/expiry.txt"))ObserveDropWarning(peer,match);
                if(match!=null&&match.IsSpawned){peer.objectivePoint=match.State.Value.Point;peer.objectiveNext=match.NextPoint;peer.objectivePreview=match.PreviewNext;peer.killReward=match.PlayerKillReward;peer.healthCount=match.HealthPickups.Count;foreach(var item in match.HealthPickups)if(item.Available)peer.healthAvailable++;peer.turretShots=match.TurretShots.Count;peer.medicNextAt=match.MedicalTransit.Value.NextAt;peer.medicEntry=match.MedicalTransit.Value.Entry;peer.medicExit=match.MedicalTransit.Value.Exit;peer.medicWarning=match.MedicalTransit.Value.Valid&&peer.medicNextAt>match.Now&&peer.medicNextAt-match.Now<=3;foreach(var e in match.Patrols)if(e.Medical){if(e.Health>0){peer.medicCount++;peer.medicHealth=e.Health;peer.medicId=e.Id;peer.medicPosition=e.Position;}}else if(e.Flying){peer.droneCount++;if(peer.droneCount==1){peer.dronePosition=match.PatrolPosition(e);peer.droneHealth=e.Health;peer.droneGeneration=e.Generation;}}else if(e.Turret){peer.turretCount++;if(peer.turretCount==1){peer.turretHealth=e.Health;peer.turretGeneration=e.Generation;peer.turretFireAt=e.FireAt;}}}
                if(match!=null){foreach(var item in match.AmmoPickups)if(item.Available){if(item.Ammo==ArenaObjectiveMatch.SupplyAmmo.Rifle)peer.supplyR++;else if(item.Ammo==ArenaObjectiveMatch.SupplyAmmo.Grenade)peer.supplyG++;else peer.supplyS++;}var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();var stations=map!=null?map.transform.Find(FoundryMedicalStationsBuilder.RootName):null;peer.medStations=stations!=null?stations.childCount:0;}
                if(match!=null&&match.IsSpawned&&File.Exists(folder+"/entrances.txt"))ObserveEntrances(peer,match);
                File.WriteAllText(folder+"/peer-"+role+".json",JsonUtility.ToJson(peer));
            }
            yield return null;
        }
    }
    static IEnumerator Wait(Func<bool> check,string label,float seconds=15)
    {
        float end=Time.realtimeSinceStartup+seconds;while(!check()&&Time.realtimeSinceStartup<end)yield return null;
        if(!check())throw new Exception(label);passed.Add(label);File.WriteAllLines(folder+"/progress.txt",passed);
    }
    static void Require(bool check,string label){if(!check)throw new Exception(label);passed.Add(label);}
    static IEnumerator Run(SideScrollNetworkSession session)
    {
        var stack=new Stack<IEnumerator>();stack.Push(File.Exists(folder+"/weapon-guides.txt")?CheckWeaponGuides():File.Exists(folder+"/appearance.txt")?CheckAppearance():File.Exists(folder+"/online.txt")?CheckOnlineRooms(session):File.Exists(folder+"/entrances.txt")?CheckEntrances():File.Exists(folder+"/spawn-safety.txt")?CheckSpawnSafety():File.Exists(folder+"/central-depot.txt")?CheckCentralDepot():File.Exists(folder+"/respawn-ammo.txt")?CheckRespawnAmmo():File.Exists(folder+"/random-objectives.txt")?CheckRandomObjectives():File.Exists(folder+"/supplies.txt")?CheckGeneralSupplies():File.Exists(folder+"/medical.txt")?CheckMedicalDrone():File.Exists(folder+"/kill-rush.txt")?CheckKillRush():File.Exists(folder+"/navigation.txt")?CheckEnemyNavigation():File.Exists(folder+"/expiry.txt")?CheckDropExpiration():File.Exists(folder+"/health.txt")?CheckHealthPickups():File.Exists(folder+"/depot.txt")?CheckDepot():File.Exists(folder+"/ready-cover.txt")?CheckReadyCover():File.Exists(folder+"/turrets.txt")?CheckTurrets():File.Exists(folder+"/west-bridge.txt")?CheckWestBridgeRound():File.Exists(folder+"/entrance-cover.txt")?CheckEntranceCovers():File.Exists(folder+"/selection-only.txt")?CheckAmmoPreselection():File.Exists(folder+"/solo-performance.txt")?CheckSoloPerformance():foundryOnly?CheckFoundry():grenadeOnly?CheckGrenade():rifleOnly?CheckRifle():instantOnly?CheckInstant():enemiesOnly?CheckEnemies():pickupsOnly?CheckPickups():practiceOnly?CheckPractice():normalOnly?CheckNormal():spectatorOnly?CheckSpectator():Check(session));string result="PASS";
        while(stack.Count>0)
        {
            bool more=false;object next=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){result="FAIL: "+failure;break;}if(!more){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        if(File.Exists(folder+"/online.txt"))
        {
            var cleanup=session.LeaveOnlineRoomAsync();float deadline=Time.realtimeSinceStartup+20;
            while(!cleanup.IsCompleted&&Time.realtimeSinceStartup<deadline)yield return null;
            if(!cleanup.IsCompleted)result+="\nOnline cleanup timed out";
        }
        File.WriteAllText(folder+"/result.txt",result+"\n"+string.Join("\n",passed));File.WriteAllText(folder+"/stop.txt","stop");
        yield return new WaitForSecondsRealtime(.6f);session.DisconnectLocal();yield return new WaitForSecondsRealtime(.6f);EditorApplication.isPlaying=false;
    }
    static IEnumerator CaptureSpectator(){yield return new WaitForSecondsRealtime(.2f);ScreenCapture.CaptureScreenshot(folder+"/spectator-client.png");}
    static IEnumerator CheckSpectator()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4),"four ready peers",45);yield return CheckLedges();Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");
        var match=ArenaObjectiveMatch.Instance;
        var prefab=new SerializedObject(UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>().arenaObjectivePrefab.GetComponent<ArenaObjectiveMatch>());
        Require(Mathf.Approximately(match.RespawnSeconds,prefab.FindProperty("respawnSeconds").floatValue)&&Mathf.Approximately(match.CountdownSeconds,prefab.FindProperty("countdownSeconds").floatValue),"server timings match prefab settings");
        yield return Wait(()=>All(4,p=>Mathf.Approximately(p.delay,match.RespawnSeconds)&&Mathf.Approximately(p.countdown,match.CountdownSeconds)),"all peers use server timings");
        match.roundSeconds=60;Require(match.StartRound(),"start followup round");yield return Wait(()=>All(4,p=>p.phase==1),"countdown complete");
        Player(2).ReceiveServerDamage(100,Player(2).LifeVersion);yield return Wait(()=>P(2).hp==0&&P(2).spectator>0,"dead client has spectator target");
        int target=P(2).spectator;Player(target).ReceiveServerDamage(100,Player(target).LifeVersion);yield return Wait(()=>P(2).hp==0&&P(2).spectator>0&&P(2).spectator!=target,"dead target automatically replaced");
        Send(2,12);yield return new WaitForSecondsRealtime(.35f);Require(!RelayArenaPrototype.Instance.overview,"host stays in normal view");
        foreach(var p in UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))p.ReceiveServerDamage(100,p.LifeVersion);
        yield return Wait(()=>All(4,p=>p.hp==0&&p.spectator==0),"all dead has no invalid spectator target");Send(0,11);yield return new WaitForSecondsRealtime(.3f);Require(Player(3).IsDead,"manual revive cannot skip delay");
        yield return new WaitForSecondsRealtime(match.RespawnSeconds*.55f);Require(Player(3).IsDead,"configured delay still active midway");
        yield return Wait(()=>All(4,p=>p.hp==100&&p.spectator==0&&p.ammo==6&&p.energy==100),"all automatically revive",match.RespawnSeconds+2);
        float x=P(2).position.x;Send(2,5);yield return Wait(()=>P(2).position.x>x+.6f,"revived client can move");
        Player(2).ReceiveServerDamage(100,Player(2).LifeVersion);var state=match.State.Value;state.End=match.Now+.1;match.State.Value=state;
        yield return Wait(()=>All(4,p=>p.phase==2),"round ends while player dead");yield return new WaitForSecondsRealtime(match.RespawnSeconds+.3f);Require(Player(2).IsDead,"finished match suppresses pending auto revive");
        Require(match.ReturnToRoom(),"return after death at end");yield return Wait(()=>All(4,p=>p.phase==0&&p.hp==100&&p.position.x>89),"return restores dead player");
    }
    static IEnumerator CheckLedges()
    {
        for(int role=1;role<=2;role++){Send(role,13);yield return Wait(()=>P(role).climbed==3&&P(role).position.y>4.2f,"three-tier climb P"+role,25);}
        ScreenCapture.CaptureScreenshot(folder+"/three-tier-ledges.png");yield return new WaitForSecondsRealtime(.25f);Send(0,9);yield return new WaitForSecondsRealtime(.4f);
    }
    static IEnumerator Check(SideScrollNetworkSession session)
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.position.x>89&&p.position.x<111&&p.hp==100&&p.mask==0),"four peers in ready room",45);
        var match=ArenaObjectiveMatch.Instance;Require(match.RoomFlow&&!match.StartRound(),"server refuses unready start");
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"ready synchronized on all peers");
        Send(2,1,false);yield return Wait(()=>All(4,p=>p.mask==13),"P2 cancels only own readiness");Require(!match.StartRound(),"cancel prevents host start");
        Send(2,2);yield return Wait(()=>P(2).denied,"client cannot start match");
        Vector3 before=P(2).position;Send(2,5);yield return Wait(()=>P(2).position.x>before.x+.6f&&P(2).phase==0,"ready room movement works");
        yield return new WaitForSecondsRealtime(.8f);Send(2,7);yield return Wait(()=>!P(2).grounded&&P(2).position.y>.7f,"ready room jump works");yield return Wait(()=>P(2).grounded,"room jump lands");
        yield return CheckLedges();
        Send(0,6);yield return new WaitForSecondsRealtime(.5f);
        Player(1).ReceiveServerDamage(100,Player(1).LifeVersion);
        Require(Player(1).Health==1,"room damage stops at one HP");yield return Wait(()=>All(4,p=>p.shots>0&&p.cursor=="None"&&!p.blocked),"room permits practice shots and leaves buttons accessible");
        ScreenCapture.CaptureScreenshot(folder+"/ready-room.png");yield return new WaitForSecondsRealtime(.25f);
        Send(2,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"ready restored");
        uint life=Player(1).LifeVersion;match.roundSeconds=20;Require(match.StartRound(),"host starts when everyone ready");Require(!match.StartRound(),"duplicate start refused");
        ScreenCapture.CaptureScreenshot(folder+"/transition.png");
        yield return Wait(()=>All(4,p=>p.phase==5&&p.position.x<30&&p.blocked),"all peers arrive before countdown");
        Player(1).ReceiveServerDamage(100,Player(1).LifeVersion);Require(Player(1).Health==100&&match.ScoreFor(Player(1).OwnerClientId)==0,"countdown rejects damage and scoring");
        ScreenCapture.CaptureScreenshot(folder+"/countdown.png");yield return new WaitForSecondsRealtime(match.CountdownSeconds*.25f);Require(match.State.Value.Phase==ArenaObjectiveMatch.Stage.Countdown,"configured countdown still active");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.life>life&&p.position.x<30&&p.hp==100&&p.energy==100&&p.ammo==6),"four peers arrive in arena with fresh life");
        Player(1).ReceiveServerDamage(25,Player(1).LifeVersion);Require(Player(1).Health==75,"combat damage enabled after transition");
        Send(1,6);yield return Wait(()=>All(4,p=>p.shots>0),"arena shots reach every peer");
        uint oldLife=Player(2).LifeVersion;Player(2).ReceiveServerDamage(100,oldLife);
        yield return Wait(()=>P(2).hp==0&&P(2).spectator>0,"dead client spectates living player");int oldTarget=P(2).spectator;
        Send(2,10);yield return Wait(()=>P(2).hp==0&&P(2).spectator!=oldTarget,"spectator can change target");
        yield return new WaitForSecondsRealtime(.7f);Require(Player(2).IsDead,"respawn is delayed");
        yield return Wait(()=>P(2).hp==100&&P(2).life>oldLife&&P(2).spectator==0&&P(2).energy==100&&P(2).ammo==6,"automatic revive restores control and resources",match.RespawnSeconds+2);
        Send(4,3);yield return Wait(()=>P(4)!=null&&!P(4).connected&&match.ReadyPlayers.Count==3,"departure removes room roster");
        Send(4,4);yield return Wait(()=>P(4)!=null&&!P(4).connected&&P(4).notice.Contains("진행"),"mid-round join refused with explanation");
        yield return Wait(()=>All(3,p=>p.phase==2&&p.blocked),"round ends on all remaining peers",25);Require(!match.StartRound(),"finished game cannot bypass ready room");
        ScreenCapture.CaptureScreenshot(folder+"/result.png");yield return new WaitForSecondsRealtime(.25f);
        Require(match.ReturnToRoom(),"host returns group to room");
        yield return Wait(()=>All(3,p=>p.phase==0&&p.mask==0&&p.position.x>89&&p.hp==100&&p.ammo==6&&p.energy==100),"return clears ready scores and restores players");Require(match.Scores.Count==0,"round scores cleared in waiting room");
        Send(4,4);yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4&&p.mask==0),"rejoin allowed only back in waiting room");
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"second round ready");match.roundSeconds=60;Require(match.StartRound(),"second round starts");
        yield return Wait(()=>All(4,p=>p.phase==1),"second arena transition");Send(4,3);yield return Wait(()=>match.ReadyPlayers.Count==3,"four to three continues");Send(3,3);yield return Wait(()=>match.ReadyPlayers.Count==2&&match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing,"two players continue");Send(2,3);
        yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Finished,"insufficient players ends match");Require(match.ReturnToRoom(),"remaining host returns");yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Waiting,"single host waiting");match.SetLocalReady(true);yield return new WaitForSecondsRealtime(.2f);Require(!match.StartRound(),"single ready host cannot start");
        Send(0,4);yield return Wait(()=>All(4,p=>p.connected&&p.roster==4&&p.phase==0&&p.mask==0),"all rejoin with readiness reset");
        ScreenCapture.CaptureScreenshot(folder+"/returned-room.png");yield return new WaitForSecondsRealtime(.25f);
        session.DisconnectLocal();yield return Wait(()=>P(2)!=null&&P(3)!=null&&P(4)!=null&&!P(2).connected&&!P(3).connected&&!P(4).connected&&P(2).notice.Contains("호스트")&&P(2).cursor=="None","host closes room and clients get notice");
    }
}
