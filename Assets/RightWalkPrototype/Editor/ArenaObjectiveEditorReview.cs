using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using RightWalkPrototype;

[InitializeOnLoad]
public static class ArenaObjectiveEditorReview
{
    [Serializable] class Request {public string folder;public bool boundsOnly,staminaOnly,joinsOnly,combatOnly,bridgeOnly,ammoOnly,reactionOnly,seamsOnly,seamsFollowup,timingOnly,reentryOnly;}
    [Serializable] class Command {public int id,mode,shooter=1;public Vector2 first,second,third=new Vector2(-19,.03f),direction=Vector2.right;public bool crouched,fire,revive;public float opening;public int ammoChoice=1;public bool special,flood,menu,resist,reactionWall;}
    [Serializable] class Peer
    {
        public bool fastPresented,sawInvalidPresentation;public bool connected,match,blocked,denied,grounded,hit,running,crouched,airPose,sawHostHit,sawClientHit;public int frame;public float vy;public string support;public bool grouped,bridgeAligned;public float leafWidth;public bool activeShotgun;public int rounds,selected,blasts,hostBlasts,clientBlasts;public float stamina,vx;public bool exhausted;public int number,version,phase,zone,count,hp,shots,roster;public ulong client;
        public double end,switchAt;public float score,total;public string result;public Vector3 position;
    }
    const string RequestPath="Assets/RightWalkPrototype/Tests/ObjectiveReviewRequest.json";
    static bool started,boundsOnly,staminaOnly,joinsOnly,combatOnly,bridgeOnly,ammoOnly,reactionOnly,seamsOnly,seamsFollowup,timingOnly,reentryOnly;static string folder;static double deadline;
    static readonly Dictionary<string,Peer> cache=new Dictionary<string,Peer>();
    static readonly List<string> passed=new List<string>();
    static ArenaObjectiveEditorReview(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/Right Walk Prototype/Verify Hacking Objective")]
    public static void Begin(){BeginReview(false);}
    [MenuItem("Tools/Right Walk Prototype/Verify Objective Bounds")]
    public static void BeginBounds(){BeginReview(true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Sprint Stamina")]
    public static void BeginStamina(){BeginReview(false,true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Ramp Joins")]
    public static void BeginJoins(){BeginReview(false,false,true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Close Combat")]
    public static void BeginCombat(){BeginReview(false,false,false,true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Retracting Bridge")]
    public static void BeginBridge(){BeginReview(false,false,false,false,true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Special Ammo")]
    public static void BeginAmmo(){BeginReview(false,false,false,false,false,true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Shotgun Reaction")]
    public static void BeginReaction(){BeginReview(false,false,false,false,false,false,true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Platform Seams")]
    public static void BeginSeams(){BeginReview(false,false,true,checkSeams:true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Platform Landing Followup")]
    public static void BeginSeamFollowup(){BeginReview(false,false,true,checkSeams:true,checkSeamFollowup:true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Shotgun Timing")]
    public static void BeginTiming(){BeginReview(false,checkReaction:true,checkTiming:true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Drop Jump Reentry")]
    public static void BeginDropReentry(){BeginReview(false,checkJoins:true,checkSeams:true,checkReentry:true);}
    static void BeginReview(bool checkBounds,bool checkStamina=false,bool checkJoins=false,bool checkCombat=false,bool checkBridge=false,bool checkAmmo=false,bool checkReaction=false,bool checkSeams=false,bool checkSeamFollowup=false,bool checkTiming=false,bool checkReentry=false)
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Requires saved edit mode");
        var r=new Request{folder=Path.GetFullPath("LocalTools/Analysis/NetworkCombat/objective-"+DateTime.UtcNow.ToString("HHmmss")),boundsOnly=checkBounds,staminaOnly=checkStamina,joinsOnly=checkJoins,combatOnly=checkCombat,bridgeOnly=checkBridge,ammoOnly=checkAmmo||checkReaction,reactionOnly=checkReaction,seamsOnly=checkSeams,seamsFollowup=checkSeamFollowup,timingOnly=checkTiming,reentryOnly=checkReentry};
        Directory.CreateDirectory(r.folder);File.WriteAllText(RequestPath,JsonUtility.ToJson(r));AssetDatabase.ImportAsset(RequestPath);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying){started=false;return;}
        if(started||EditorApplication.isCompiling||Time.time<.3f||!File.Exists(RequestPath))return;
        var session=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();if(session==null)return;
        started=true;var request=JsonUtility.FromJson<Request>(File.ReadAllText(RequestPath));folder=request.folder;boundsOnly=request.boundsOnly;staminaOnly=request.staminaOnly;joinsOnly=request.joinsOnly;combatOnly=request.combatOnly;bridgeOnly=request.bridgeOnly;ammoOnly=request.ammoOnly;reactionOnly=request.reactionOnly;seamsOnly=request.seamsOnly;seamsFollowup=request.seamsFollowup;timingOnly=request.timingOnly;reentryOnly=request.reentryOnly;cache.Clear();passed.Clear();deadline=EditorApplication.timeSinceStartup+180;
        bool main=Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor;
        session.useReadyRoom=false;session.StartLocal(main);session.StartCoroutine(Actor(session,main));if(main)session.StartCoroutine(Run(session));
    }
    static string Read(string path){using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var r=new StreamReader(f))return r.ReadToEnd();}
    static Peer[] Peers()
    {
        foreach(var path in Directory.GetFiles(folder,"peer-*.json"))try{var p=JsonUtility.FromJson<Peer>(Read(path));if(p!=null)cache[path]=p;}catch{}
        var result=new Peer[cache.Count];cache.Values.CopyTo(result,0);return result;
    }
    static Peer Number(int number){foreach(var p in Peers())if(p.number==number&&p.connected)return p;return null;}
    static SideScrollNetworkPlayer Player(int number){foreach(var p in UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.PlayerNumber==number)return p;return null;}
    static void Send(int id,int mode){File.WriteAllText(folder+"/command.json",JsonUtility.ToJson(new Command{id=id,mode=mode}));}
    static IEnumerator Actor(SideScrollNetworkSession session,bool main)
    {
        GameObject testReactionWall=null;string reentryRamp="";
        int revision=-1;bool placed=false,denied=false,rejoined=false,fired=false,jumped=false,sawHostHit=false,sawClientHit=false;float began=0,nextWrite=0;
        bool observedBaseline=false,observedImpact=false,observedDisplacement=false;int observedHealth=0;Vector3 observedStart=Vector3.zero;
        bool fastPresented=false,sawInvalidPresentation=false;
        string statusPath=folder+"/peer-"+System.Diagnostics.Process.GetCurrentProcess().Id+".json";
        while(EditorApplication.isPlaying)
        {
            if(File.Exists(folder+"/stop.txt")){if(!main)session.DisconnectLocal();yield break;}
            if(main&&EditorApplication.timeSinceStartup>deadline){File.WriteAllText(folder+"/result.txt","FAIL: review deadline");EditorApplication.isPlaying=false;yield break;}
            Command c=null;try{c=JsonUtility.FromJson<Command>(Read(folder+"/command.json"));}catch{}
            SideScrollNetworkPlayer own=null;foreach(var p in UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsOwner)own=p;
            var match=ArenaObjectiveMatch.Instance;
            if(joinsOnly&&own!=null&&!main){own.GetComponent<SideScrollMapPreview>().SetInput(0);own.GetComponent<CrouchTrial>().SetRequested(false);own.GetComponentInChildren<RightFacingShotPreview>().readMouse=false;}
            if(seamsOnly&&own!=null&&c!=null)
            {
                var move=own.GetComponent<SideScrollMapPreview>();
                if(revision!=c.id){revision=c.id;placed=fired=jumped=false;began=Time.time;reentryRamp="";}
                if(!placed){move.SetSpawnPosition(own.PlayerNumber==c.shooter?c.first:new Vector2(-20+own.PlayerNumber,.03f));placed=true;}
                own.GetComponent<CrouchTrial>().SetRequested(c.crouched&&own.PlayerNumber==c.shooter);
                move.SetInput(own.PlayerNumber==c.shooter&&c.mode==0&&Time.time-began>.4f?c.direction.x:0,false,c.special);
                if(own.PlayerNumber==c.shooter)
                {
                    if(c.mode==44)
                    {
                        var crouch=own.GetComponent<CrouchTrial>();
                        if(!fired)
                        {
                            crouch.SetRequested(true);
                            if(move.Grounded&&crouch.IsCrouched){reentryRamp=move.GroundCollider.name;move.SetInput(0,true);fired=true;began=Time.time;}
                        }
                        else
                        {
                            crouch.SetRequested(move.Grounded&&!jumped);
                            if(!jumped&&!move.Grounded&&Time.time-began>.14f)
                            {move.SetInput(0,true);jumped=true;File.WriteAllText(folder+"/reentry-jumped-"+c.id+"-P"+own.PlayerNumber+".txt",reentryRamp);}
                            if(jumped&&move.Grounded&&move.GroundCollider!=null&&move.GroundCollider.name!=reentryRamp)
                                File.WriteAllText(folder+"/reentry-lower-"+c.id+"-P"+own.PlayerNumber+".txt",move.GroundCollider.name);
                        }
                    }
                    if(c.mode==40&&!fired&&Time.time-began>.7f){move.ApplyShotgunPush(c.direction.normalized*SideScrollNetworkPlayer.ShotgunPush);fired=true;}
                    if((c.mode==41||c.mode==42)&&!fired&&Time.time-began>.65f){move.SetInput(0,true);fired=true;}
                    if(c.mode==41&&!jumped&&Time.time-began>1f){move.SetInput(0,true);jumped=true;}
                }
                own.GetComponentInChildren<RightFacingShotPreview>().readMouse=false;
                if(own.PlayerNumber==c.shooter)
                {
                    var b=own.GetComponent<Rigidbody2D>();var shape=own.GetComponent<BoxCollider2D>();
                    string path=folder+"/seam-"+c.id+"-P"+own.PlayerNumber+".csv";
                    if(!File.Exists(path))File.WriteAllText(path,"time,x,y,feet,vx,vy,grounded,support,contacts\n");
                    var contacts=new ContactPoint2D[16];int count=shape.GetContacts(contacts);string contact="";
                    for(int k=0;k<count;k++)contact+=contacts[k].collider.name+"/"+contacts[k].otherCollider.name+" n="+contacts[k].normal.ToString("F5")+" p="+contacts[k].point.ToString("F5")+" sep="+contacts[k].separation.ToString("F5")+";";
                    File.AppendAllText(path,FormattableString.Invariant($"{Time.time:R},{b.position.x:R},{b.position.y:R},{shape.bounds.min.y:R},{b.linearVelocity.x:R},{b.linearVelocity.y:R},{move.Grounded},{(move.GroundCollider!=null?move.GroundCollider.name:"none")},\"{contact}\"\n"));
                }
            }
            if(bridgeOnly&&own!=null&&c!=null)
            {
                var rules=UnityEngine.Object.FindFirstObjectByType<RelayArenaPrototype>(FindObjectsInactive.Include);
                var move=own.GetComponent<SideScrollMapPreview>();move.SetInput(0);own.GetComponent<CrouchTrial>().SetRequested(false);own.GetComponentInChildren<RightFacingShotPreview>().readMouse=false;
                if(revision!=c.id){revision=c.id;placed=false;}
                if(c.mode==30)
                {
                    rules.enabled=false;
                    typeof(RelayArenaPrototype).GetMethod("SetBridgeOpening",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(rules,new object[]{c.opening});
                    if(!placed){move.SetSpawnPosition(new Vector2(own.PlayerNumber==1?12.35f:own.PlayerNumber==2?15.65f:-20+own.PlayerNumber,.03f));placed=true;}
                }
                else if(c.mode==31){rules.enabled=true;if(!placed){move.SetSpawnPosition(new Vector2(-20+own.PlayerNumber,.03f));placed=true;}}
            }
            if((combatOnly||ammoOnly)&&own!=null)
            {
                var move=own.GetComponent<SideScrollMapPreview>();move.SetInput(0);var aim=own.GetComponentInChildren<RightFacingAimRig>();aim.mouseAim=false;aim.GetComponent<RightFacingShotPreview>().readMouse=false;
                if(c!=null&&c.id>0)
                {
                    if(revision!=c.id){revision=c.id;placed=fired=false;began=Time.time;observedBaseline=observedImpact=observedDisplacement=false;}
                    if(reactionOnly)
                    {
                        if(c.reactionWall&&testReactionWall==null){testReactionWall=new GameObject("Reaction wall");testReactionWall.transform.position=new Vector3(1.25f,1.5f,0);testReactionWall.AddComponent<BoxCollider2D>().size=new Vector2(.15f,3);Physics2D.SyncTransforms();}
                        if(!c.reactionWall&&testReactionWall!=null){UnityEngine.Object.Destroy(testReactionWall);testReactionWall=null;}
                    }
                    own.GetComponent<CrouchTrial>().SetRequested(c.crouched&&own.PlayerNumber<=2);
                    aim.SetAimDirection(own.PlayerNumber==c.shooter?c.direction:Vector2.up);
                    if(ammoOnly)
                    {
                        RelayArenaPrototype.Instance.SetMenuOpen(c.menu);
                        own.SelectAmmo((SideScrollNetworkPlayer.AmmoKind)c.ammoChoice);
                        own.SetSpecialHeld(c.special&&own.PlayerNumber==c.shooter);
                    }
                    else own.SetSpecialHeld(false);
                    if(reactionOnly&&c.resist&&own.PlayerNumber!=c.shooter&&own.GetComponent<SideScrollMapPreview>().HitReacting)
                    {move.SetInput(-c.direction.x,true,true);}
                    if(c.revive&&own.IsDead)own.RequestRevive();
                    if(!placed&&!own.IsDead){move.SetSpawnPosition(own.PlayerNumber==1?c.first:own.PlayerNumber==2?c.second:own.PlayerNumber==3?c.third:new Vector2(-16,.03f));placed=true;}
                    if(c.fire&&own.PlayerNumber==c.shooter&&!fired&&Time.time-began>.9f){
                        if(reactionOnly)File.WriteAllText(folder+"/fired-"+revision+".txt",DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        aim.GetComponent<RightFacingShotPreview>().Fire();
                        if(ammoOnly&&c.flood)for(int attempt=0;attempt<8;attempt++)typeof(SideScrollNetworkPlayer).GetMethod("FireShotgunServerRpc",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(own,new object[]{(Vector2)aim.MuzzlePosition,aim.AimDirection});
                        fired=true;if(ammoOnly)ScreenCapture.CaptureScreenshot(folder+"/ammo-shot-P"+own.PlayerNumber+"-"+revision+".png");if(!main&&!c.crouched)ScreenCapture.CaptureScreenshot(folder+"/client-overlap.png");}
                }
            }
            if(!ammoOnly&&!bridgeOnly&&!combatOnly&&!joinsOnly&&c!=null&&c.id>0&&own!=null&&match!=null&&match.IsSpawned)
            {
                if(revision!=c.id){revision=c.id;placed=rejoined=fired=jumped=false;began=Time.time;}
                var move=own.GetComponent<SideScrollMapPreview>();move.SetInput(0);own.GetComponent<CrouchTrial>().SetRequested(c.mode==23&&own.PlayerNumber==1);
                var aim=own.GetComponentInChildren<RightFacingAimRig>();aim.SetAimDirection(Vector2.up);aim.GetComponent<RightFacingShotPreview>().readMouse=false;
                if(c.mode==8&&!main)denied=!match.StartRound();
                bool occupy=(c.mode==1||c.mode==3||c.mode==7||c.mode==9)&&own.PlayerNumber==1||c.mode==2&&own.PlayerNumber<=2||(c.mode==5||c.mode==6)&&own.PlayerNumber==2;
                if(!placed&&!own.IsDead)
                {
                    var point=match.Points[match.State.Value.Point];var position=new Vector2(-19+3*(own.PlayerNumber-1),.03f);
                    if(occupy)position=(Vector2)point.transform.position+new Vector2(own.PlayerNumber==2?.3f:-.3f,.03f);
                    if(c.mode==4&&own.PlayerNumber==1)position=new Vector2(point.transform.position.x,.03f);
                    if(c.mode>=10&&c.mode<=16&&own.PlayerNumber==1)
                    {
                        position=(Vector2)point.transform.position+new Vector2(-.3f,.03f);
                        if(c.mode==10)position.y+=2;
                        if(c.mode==11)position.x=point.CaptureBounds.max.x+.07f;
                        if(c.mode==12)position.x=point.CaptureBounds.max.x-.07f;
                        if(c.mode==13)position.y=point.CaptureBounds.max.y+.63f;
                        if(c.mode==16)position.y=.03f;
                    }
                    if(c.mode==17&&own.PlayerNumber==2)position=(Vector2)point.transform.position+new Vector2(.3f,2.03f);
                    if(c.mode>=20&&own.PlayerNumber==(c.mode==26?2:1))position=new Vector2(-22,.03f);
                    move.SetSpawnPosition(position);placed=true;
                }
                if(c.mode==14&&own.PlayerNumber==1&&!jumped&&Time.time-began>.4f&&move.Grounded){move.SetInput(0,true);jumped=true;}
                if(c.mode>=20)
                {
                    if(own.PlayerNumber==1&&(c.mode==20||c.mode==22||c.mode==23||c.mode==25))move.SetInput(1,false,c.mode!=22);
                    if(own.PlayerNumber==2&&c.mode==26)move.SetInput(1,false,true);
                    if(own.PlayerNumber==1&&c.mode==24&&!jumped&&Time.time-began>.4f&&move.Grounded){move.SetInput(0,true);jumped=true;}
                    if(own.PlayerNumber==2&&c.mode==27&&own.IsDead)own.RequestRevive();
                }
                if(c.mode==7&&own.IsDead)own.RequestRevive();
                if(c.mode==9&&own.PlayerNumber==1&&!fired&&Time.time-began>.6f){aim.GetComponent<RightFacingShotPreview>().Fire();fired=true;}
                if(c.mode==6&&own.PlayerNumber==4&&!rejoined)
                {rejoined=true;session.DisconnectLocal();yield return new WaitForSecondsRealtime(1);session.StartLocal(false);}
            }
            if(reactionOnly&&own!=null)
            {
                foreach(int n in new[]{1,2})
                {
                    var watched=Player(n);if(watched==null||watched.IsOwner)continue;
                    fastPresented|=(uint)typeof(SideScrollNetworkPlayer).GetField("receivedHitPose",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(watched)>0;
                    sawInvalidPresentation|=Mathf.Abs(watched.transform.position.x)>100;
                }
                if(main&&c!=null&&c.fire&&Time.time-began>.8f)
                {
                    var target=Player(c.shooter==1?2:1);
                    if(target!=null)
                    {
                        if(!observedBaseline){observedBaseline=true;observedStart=target.transform.position;observedHealth=target.Health;}
                        if(!observedImpact&&target.Health<observedHealth)
                        {observedImpact=true;File.WriteAllText(folder+"/impact-visible-"+c.id+".txt",DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));}
                        if(!observedDisplacement&&Vector3.Distance(observedStart,target.transform.position)>.025f)
                        {observedDisplacement=true;File.WriteAllText(folder+"/motion-visible-"+c.id+".txt",DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));}
                    }
                }
                if(c!=null&&own.GetComponent<SideScrollMapPreview>().HitReacting)
                {
                    string path=folder+"/pushed-"+c.id+"-P"+own.PlayerNumber+".txt";
                    if(!File.Exists(path))File.WriteAllText(path,DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                sawHostHit|=Player(1)!=null&&Player(1).GetComponent<SideScrollMapPreview>().HitReacting;
                sawClientHit|=Player(2)!=null&&Player(2).GetComponent<SideScrollMapPreview>().HitReacting;
                if(c!=null&&c.id>0&&own.PlayerNumber<=2)
                {
                    var m=own.GetComponent<SideScrollMapPreview>();var b=own.GetComponent<Rigidbody2D>();var w=own.GetComponent<RightFacingWalker>();
                    var file=folder+"/reaction-"+c.id+"-P"+own.PlayerNumber+".csv";
                    if(!File.Exists(file))File.WriteAllText(file,"time,x,y,vx,vy,frame,phase,running,grounded,hit,crouched,stamina,airPose\n");
                    File.AppendAllText(file,string.Join(",",Time.time.ToString("R",System.Globalization.CultureInfo.InvariantCulture),own.transform.position.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture),own.transform.position.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture),b.linearVelocity.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture),b.linearVelocity.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture),w.FrameIndex,w.CyclePhase.ToString("R",System.Globalization.CultureInfo.InvariantCulture),own.GetComponent<RunWalkPreview>().IsRunning,m.Grounded,m.HitReacting,own.GetComponent<CrouchTrial>().IsCrouched,own.Stamina.ToString("R",System.Globalization.CultureInfo.InvariantCulture),UsesAirPose(own))+"\n");
                }
            }
            if(Time.realtimeSinceStartup>=nextWrite)
            {
                nextWrite=Time.realtimeSinceStartup+.1f;var manager=NetworkManager.Singleton;
                var p=new Peer{fastPresented=fastPresented,sawInvalidPresentation=sawInvalidPresentation,sawHostHit=sawHostHit,sawClientHit=sawClientHit,hit=own!=null&&own.GetComponent<SideScrollMapPreview>().HitReacting,running=own!=null&&own.GetComponent<RunWalkPreview>().IsRunning,crouched=own!=null&&own.GetComponent<CrouchTrial>().IsCrouched,frame=own!=null?own.GetComponent<RightFacingWalker>().FrameIndex:0,vy=own!=null?own.GetComponent<Rigidbody2D>().linearVelocity.y:0,airPose=own!=null&&UsesAirPose(own),rounds=own!=null?own.ShotgunRounds:0,selected=own!=null?(int)own.SelectedAmmo:0,activeShotgun=own!=null&&own.ShotgunActive,blasts=own!=null?own.ReceivedShotgunBlasts:0,hostBlasts=Player(1)!=null?Player(1).ReceivedShotgunBlasts:0,clientBlasts=Player(2)!=null?Player(2).ReceivedShotgunBlasts:0,bridgeAligned=BridgeAligned(out float width),leafWidth=width,grouped=CheckGroups(),connected=manager!=null&&manager.IsConnectedClient,match=match!=null&&match.IsSpawned,blocked=RelayArenaPrototype.ControlsBlocked,denied=denied,number=own!=null?own.PlayerNumber:0,client=own!=null?own.OwnerClientId:0,hp=own!=null?own.Health:0,shots=own!=null?own.ReceivedShots:0,position=own!=null?own.transform.position:Vector3.zero,grounded=own!=null&&own.GetComponent<SideScrollMapPreview>().Grounded,support=own!=null&&own.GetComponent<SideScrollMapPreview>().GroundCollider!=null?own.GetComponent<SideScrollMapPreview>().GroundCollider.name:"",stamina=own!=null?own.Stamina:0,exhausted=own!=null&&own.SprintExhausted,vx=own!=null?own.GetComponent<Rigidbody2D>().linearVelocity.x:0};
                if(p.match)
                {
                    var state=match.State.Value;p.version=state.Version;p.phase=(int)state.Phase;p.zone=state.Point;p.count=match.Occupants.Value;p.end=state.End;p.switchAt=state.Switch;p.roster=match.Scores.Count;p.score=own!=null?match.ScoreFor(own.OwnerClientId):0;p.result=match.Result.Value.ToString();
                    for(int i=0;i<match.Scores.Count;i++)p.total+=match.Scores[i].Seconds;
                }
                File.WriteAllText(statusPath,JsonUtility.ToJson(p));
            }
            yield return null;
        }
    }
    static IEnumerator Wait(Func<bool> ready,string label,float seconds=15)
    {
        float end=Time.realtimeSinceStartup+seconds;
        while(!ready()&&Time.realtimeSinceStartup<end)yield return null;
        if(!ready())throw new Exception(label);passed.Add(label);File.WriteAllLines(folder+"/progress.txt",passed);
    }
    static bool All(Func<Peer,bool> predicate){var peers=Peers();if(peers.Length!=4)return false;foreach(var p in peers)if(!predicate(p))return false;return true;}
    static IEnumerator Run(SideScrollNetworkSession session)
    {
        var stack=new Stack<IEnumerator>();stack.Push(reentryOnly?CheckDropReentry():timingOnly?CheckTiming():seamsOnly?CheckSeams():reactionOnly?CheckReaction():ammoOnly?CheckAmmo():bridgeOnly?CheckBridge():combatOnly?CheckCombat():joinsOnly?CheckJoins():staminaOnly?CheckStamina():boundsOnly?CheckBounds():Check());string result="PASS";
        while(stack.Count>0)
        {
            bool more=false;object next=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){result="FAIL: "+failure;break;}if(!more){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        File.WriteAllText(folder+"/result.txt",result+"\n"+string.Join("\n",passed));File.WriteAllText(folder+"/stop.txt","stop");
        yield return new WaitForSecondsRealtime(1);session.DisconnectLocal();yield return new WaitForSecondsRealtime(.5f);EditorApplication.isPlaying=false;
    }
    static IEnumerator Check()
    {
        yield return Wait(()=>All(p=>p.connected&&p.match&&p.phase==0),"four peers receive waiting objective",60);
        var match=ArenaObjectiveMatch.Instance;
        Send(1,8);yield return Wait(()=>All(p=>p.number==1||p.denied),"clients cannot start a round");
        if(match.State.Value.Phase!=ArenaObjectiveMatch.Stage.Waiting)throw new Exception("Client changed phase");
        match.roundSeconds=36;match.pointSeconds=7;match.warningSeconds=2;
        if(!match.StartRound())throw new Exception("Host start rejected");int version=match.State.Value.Version;
        Send(2,1);yield return Wait(()=>match.Occupants.Value==1&&Number(1)!=null&&Number(1).score>.4f,"sole occupant scores");
        int shots=Player(1).ReceivedShots;Send(3,9);yield return Wait(()=>Player(1).ReceivedShots>shots,"shooting remains available inside objective");
        Send(4,2);yield return Wait(()=>match.Occupants.Value==2,"two players contest");yield return new WaitForSeconds(.3f);
        float before=match.ScoreFor(Player(1).OwnerClientId);yield return new WaitForSeconds(.7f);
        if(Mathf.Abs(match.ScoreFor(Player(1).OwnerClientId)-before)>.02f)throw new Exception("Contested scoring continued");passed.Add("contested score frozen");
        Send(5,1);yield return Wait(()=>match.Occupants.Value==1,"contest ends");Player(1).ReceiveServerDamage(100,Player(1).LifeVersion);
        yield return Wait(()=>Player(1).IsDead&&match.Occupants.Value==0,"dead occupant cannot score or contest");
        Send(6,7);yield return Wait(()=>!Player(1).IsDead&&match.Occupants.Value==1,"owner revive returns to scoring");
        yield return Wait(()=>match.PreviewNext,"next site announced",10);ScreenCapture.CaptureScreenshot(folder+"/preview.png");yield return new WaitForSeconds(.3f);
        yield return Wait(()=>match.State.Value.Point==1,"server relocates objective",10);
        yield return Wait(()=>All(p=>p.zone==1&&p.version==version),"all peers agree on active site");
        Send(7,4);yield return new WaitForSeconds(.8f);if(match.Occupants.Value!=0)throw new Exception("Player on lower floor counted");passed.Add("lower floor under objective excluded");
        Send(8,5);yield return Wait(()=>Number(2)!=null&&Number(2).score>.3f,"client-owned player scores");
        var old=Number(4).client;Send(9,6);
        yield return Wait(()=>{var peer=Number(4);return peer!=null&&peer.client!=old&&peer.match&&peer.version==version&&peer.phase==1&&peer.roster==4&&peer.score<.05f;},"reconnected client receives live round",20);
        if(Number(4).score>.05f||Number(4).roster!=4)throw new Exception("Reconnect inherited score/duplicate roster");
        yield return Wait(()=>All(p=>p.phase==2&&p.blocked&&!string.IsNullOrEmpty(p.result)),"expiry result and controls replicated",35);
        float authoritativeTotal=0;for(int i=0;i<match.Scores.Count;i++)authoritativeTotal+=match.Scores[i].Seconds;
        yield return Wait(()=>All(p=>Mathf.Abs(p.total-authoritativeTotal)<.01f&&p.result==match.Result.Value.ToString()),"all score tables and winner agree with server");
        var owner=Player(1);int hp=owner.Health,shotsAfter=owner.ReceivedShots;
        owner.GetComponentInChildren<RightFacingShotPreview>().Fire();owner.ReceiveServerDamage(100,owner.LifeVersion);
        yield return new WaitForSeconds(.4f);
        if(owner.Health!=hp||owner.ReceivedShots!=shotsAfter)throw new Exception("Server accepted post-round combat");passed.Add("server rejects post-round fire and damage");
        float total=Number(1).total;yield return new WaitForSeconds(.5f);if(Mathf.Abs(total-Number(1).total)>.01f)throw new Exception("Scores changed after expiry");
        ScreenCapture.CaptureScreenshot(folder+"/result.png");yield return new WaitForSeconds(.3f);
        match.roundSeconds=2;match.StartRound();yield return Wait(()=>All(p=>p.version==version+1&&p.phase==1&&p.hp==100&&p.total<.02f),"restart resets scores and all lives");
        yield return Wait(()=>All(p=>p.phase==2&&p.result.Contains("무승부")),"zero-score tie handled",5);
        UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>().DisconnectLocal();
        yield return Wait(()=>All(p=>!p.match&&!p.blocked),"host shutdown clears objective and finished input gate",10);
    }
    static IEnumerator CheckBounds()
    {
        yield return Wait(()=>All(p=>p.connected&&p.match&&p.phase==0),"four peers ready for bounds review",60);
        var match=ArenaObjectiveMatch.Instance;match.roundSeconds=60;match.pointSeconds=20;match.warningSeconds=2;match.StartRound();
        Send(20,0);yield return Wait(()=>match.State.Value.Point==1,"west middle objective activates",24);
        var owner=Player(1);var move=owner.GetComponent<SideScrollMapPreview>();var point=match.Points[1];
        Send(21,10);
        yield return Wait(()=>move.Grounded&&move.GroundCollider!=null&&move.GroundCollider.name=="West upper ramp"&&match.Occupants.Value==1,"ramp inside visible area counts");
        float before=match.ScoreFor(owner.OwnerClientId);yield return new WaitForSeconds(.4f);
        if(match.ScoreFor(owner.OwnerClientId)<=before+.2f)throw new Exception("Ramp occupant not scoring");
        yield return Wait(()=>All(p=>p.zone==1&&p.count==1),"ramp occupancy replicated to all peers");
        ScreenCapture.CaptureScreenshot(folder+"/ramp-scoring.png");yield return new WaitForSeconds(.2f);
        Send(22,14);yield return Wait(()=>!move.Grounded&&owner.GetComponent<Rigidbody2D>().linearVelocity.y>1&&move.JumpCount==1&&match.Occupants.Value==0,"jump removes occupant");
        before=match.ScoreFor(owner.OwnerClientId);yield return new WaitForSeconds(.16f);
        if(Mathf.Abs(match.ScoreFor(owner.OwnerClientId)-before)>.02f)throw new Exception("Airborne score increased");
        yield return Wait(()=>move.Grounded&&match.Occupants.Value==1,"landing inside area restores scoring");
        Send(23,12);yield return Wait(()=>move.Grounded&&match.Occupants.Value==1&&owner.transform.position.x>point.CaptureBounds.max.x-.1f,"centre just inside side edge counts");
        Send(24,11);yield return Wait(()=>move.Grounded&&owner.transform.position.x>point.CaptureBounds.max.x,"outside centre setup");yield return new WaitForSeconds(.25f);
        if(match.Occupants.Value!=0)throw new Exception("Body overlap counted outside centre");passed.Add("centre outside edge excluded despite body overlap");
        Send(25,16);yield return Wait(()=>move.Grounded&&owner.transform.position.y<.1f,"lower floor setup");yield return new WaitForSeconds(.25f);
        if(match.Occupants.Value!=0)throw new Exception("Lower floor counted");passed.Add("lower floor excluded");
        var fixture=new GameObject("Review upper floor");fixture.transform.position=new Vector3(point.transform.position.x,point.CaptureBounds.max.y+.54f,0);fixture.AddComponent<BoxCollider2D>().size=new Vector2(2.6f,.12f);Physics2D.SyncTransforms();
        Send(26,13);yield return Wait(()=>move.Grounded&&move.GroundCollider!=null&&move.GroundCollider.name==fixture.name,"upper floor setup");yield return new WaitForSeconds(.25f);
        if(match.Occupants.Value!=0)throw new Exception("Above-area floor counted");passed.Add("floor above visible area excluded");UnityEngine.Object.Destroy(fixture);yield return null;
        Send(27,17);yield return Wait(()=>{var p=Number(2);return p!=null&&p.grounded&&p.support=="West upper ramp"&&match.Occupants.Value==1&&match.OccupantNumber.Value==2;},"client ramp occupant counts");
        float clientBefore=match.ScoreFor(Player(2).OwnerClientId);yield return new WaitForSeconds(.4f);
        if(match.ScoreFor(Player(2).OwnerClientId)<=clientBefore+.2f)throw new Exception("Remote ramp player not scoring");
        Player(2).ReceiveServerDamage(100,Player(2).LifeVersion);
        yield return Wait(()=>Player(2).IsDead&&match.Occupants.Value==0,"dead body on ramp excluded");
        yield return Wait(()=>All(p=>p.count==0),"final occupancy agrees on all peers");
    }

    static IEnumerator CheckStamina()
    {
        yield return Wait(()=>All(p=>p.connected&&p.match&&p.hp==100&&Mathf.Abs(p.stamina-100)<.01f),"four owners spawn with full stamina",60);
        var owner=Player(1);var move=owner.GetComponent<SideScrollMapPreview>();var body=owner.GetComponent<Rigidbody2D>();
        Send(40,20);yield return Wait(()=>owner.Stamina<95&&body.linearVelocity.x>3.3f,"host sprint consumes stamina");
        if(Player(2).Stamina<99)throw new Exception("Other player's stamina consumed");
        ScreenCapture.CaptureScreenshot(folder+"/stamina-running.png");yield return new WaitForSeconds(.2f);
        yield return Wait(()=>owner.SprintExhausted&&owner.Stamina<1&&Mathf.Abs(body.linearVelocity.x)<=move.speed+.08f,"exhaustion limits sprint to walking",8);
        ScreenCapture.CaptureScreenshot(folder+"/stamina-empty.png");yield return new WaitForSeconds(.2f);
        Send(41,21);yield return new WaitForSeconds(.2f);
        if(owner.Stamina>5)throw new Exception("Position reset refilled stamina or recovery delay missing");
        yield return Wait(()=>owner.Stamina>40&&!owner.SprintExhausted,"rest recovers stamina and unlocks sprint",6);
        float before=owner.Stamina;Send(42,22);yield return new WaitForSeconds(.6f);
        if(owner.Stamina<before-.2f||Mathf.Abs(body.linearVelocity.x)>move.speed+.08f)throw new Exception("Walking spends stamina or uses sprint speed");passed.Add("walking does not spend stamina");
        Send(43,23);yield return Wait(()=>owner.GetComponent<CrouchTrial>().IsCrouched&&Mathf.Abs(body.linearVelocity.x)<1.1f,"crouch prevents sprint");
        before=owner.Stamina;yield return new WaitForSeconds(.4f);if(owner.Stamina<before-.2f)throw new Exception("Crouch shift consumes stamina");passed.Add("crouch plus shift does not spend stamina");
        var rules=RelayArenaPrototype.Instance;float recovery=rules.staminaRecovery;rules.staminaRecovery=0;
        before=owner.Stamina;Send(44,24);yield return Wait(()=>!move.Grounded&&body.linearVelocity.y>1&&move.JumpCount==1,"jump without sprint launched");
        yield return new WaitForSeconds(.16f);if(owner.Stamina<before-.2f)throw new Exception("Jump consumed stamina");passed.Add("jump without sprint has no stamina cost");rules.staminaRecovery=recovery;
        before=owner.Stamina;Send(45,25);yield return Wait(()=>owner.Stamina<before-.5f&&owner.SprintWanted&&body.linearVelocity.x>3.3f,"sprint before menu pause");
        RelayArenaPrototype.Instance.SetMenuOpen(true);yield return new WaitForSeconds(.2f);before=owner.Stamina;yield return new WaitForSeconds(.4f);
        if(owner.Stamina<before-.2f||Mathf.Abs(body.linearVelocity.x)>.02f)throw new Exception("Menu continued sprint consumption");passed.Add("menu blocks sprint and drain");RelayArenaPrototype.Instance.SetMenuOpen(false);
        Send(46,26);yield return Wait(()=>{var p=Number(2);return p!=null&&p.stamina<95&&p.vx>3.3f;},"client sprint stamina replicated");
        var remote=Player(2);yield return Wait(()=>Mathf.Abs(remote.Stamina-Number(2).stamina)<2,"server and client stamina agree");
        remote.ReceiveServerDamage(100,remote.LifeVersion);yield return Wait(()=>remote.IsDead,"sprinting client killed");
        before=remote.Stamina;yield return new WaitForSeconds(.4f);if(Mathf.Abs(remote.Stamina-before)>.01f)throw new Exception("Dead player stamina changed");passed.Add("dead stamina frozen");
        Send(47,27);yield return Wait(()=>!remote.IsDead&&remote.Stamina>99&&!remote.SprintExhausted,"owner revive restores full stamina");
        Send(48,26);yield return Wait(()=>remote.Stamina<85,"client stamina partly spent before restart");
        Send(49,28);yield return new WaitForSeconds(.2f);ArenaObjectiveMatch.Instance.StartRound();
        yield return Wait(()=>All(p=>p.hp==100&&p.stamina>99&&!p.exhausted),"round start resets all stamina");
    }





    static bool UsesAirPose(SideScrollNetworkPlayer player)
    {
        var move=player.GetComponent<SideScrollMapPreview>();var air=player.GetComponent<AirDownShotPose>();
        return !move.Grounded&&!player.GetComponent<CrouchTrial>().IsCrouched&&air.isActiveAndEnabled&&
            (bool)typeof(AirDownShotPose).GetField("active",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(air);
    }
    static float Value(string[] row,int index)=>float.Parse(row[index],System.Globalization.CultureInfo.InvariantCulture);
    static IEnumerator ReactionCase(string label,Command command,bool wall=false)
    {
        command.resist=true;command.reactionWall=wall;command.fire=false;CombatCommand(command);
        yield return Wait(()=>Number(1)!=null&&Number(2)!=null&&Mathf.Abs(Number(1).position.x-command.first.x)<.05f&&Mathf.Abs(Number(2).position.x-command.second.x)<.05f,"reaction placement "+label);
        yield return new WaitForSeconds(.8f);
        int target=command.shooter==1?2:1;var victim=Player(target);float startY=Number(target).position.y,startX=Number(target).position.x;
        int hp=victim.Health,ammo=Player(command.shooter).ShotgunRounds;
        GameObject blocker=null;
        if(wall&&!reactionOnly){blocker=new GameObject("Reaction wall");blocker.transform.position=new Vector3(1.25f,1.5f,0);blocker.AddComponent<BoxCollider2D>().size=new Vector2(.15f,3);Physics2D.SyncTransforms();}
        command.fire=true;CombatCommand(command);int id=combatRevision;
        yield return Wait(()=>Number(target)!=null&&Number(target).hit,"owner enters reaction "+label);
        yield return new WaitForSeconds(.10f);ScreenCapture.CaptureScreenshot(folder+"/reaction-"+label+".png");
        yield return new WaitForSeconds(.85f);
        var rows=new List<string[]>();foreach(string line in Read(folder+"/reaction-"+id+"-P"+target+".csv").Split('\n')){var row=line.Trim().Split(',');if(row.Length==13&&row[9]=="True")rows.Add(row);}
        if(rows.Count<3)throw new Exception("Insufficient reaction samples "+label);
        float began=Value(rows[0],0),maxY=startY,maxX=0;bool air=false;int frame=-1;
        foreach(var row in rows)
        {
            maxY=Mathf.Max(maxY,Value(row,2));maxX=Mathf.Max(maxX,command.direction.x*(Value(row,1)-startX));
            if(Value(row,0)-began<.04f)continue;
            if(row[7]!="False")throw new Exception("Running during reaction "+label);
            if(frame<0)frame=int.Parse(row[5]);else if(frame!=int.Parse(row[5]))throw new Exception("Walking frame advanced during reaction "+label);
            if(!wall&&Mathf.Sign(command.direction.x)*Value(row,3)<4.5f)throw new Exception("Input cancelled shove "+label);
            if(Value(row,4)>.2f)throw new Exception("Jump bypassed level/downward hit lock "+label);
            if(Mathf.Abs(Value(row,11)-100)>.05f)throw new Exception("Forced motion used stamina "+label);
            air|=row[12]=="True";
        }
        if((!wall&&maxX<.7f)||maxY-startY>.1f)throw new Exception("Weak shove or unwanted hop "+label+": dy="+(maxY-startY)+" dx="+maxX);
        if(victim.Health>=hp||Player(command.shooter).ShotgunRounds!=ammo-1)throw new Exception("Reaction damage/ammo "+label);
        double firedAt=double.Parse(Read(folder+"/fired-"+id+".txt"),System.Globalization.CultureInfo.InvariantCulture);
        double pushedAt=double.Parse(Read(folder+"/pushed-"+id+"-P"+target+".txt"),System.Globalization.CultureInfo.InvariantCulture);
        File.AppendAllText(folder+"/reaction-latency.csv",label+","+((pushedAt-firedAt)/TimeSpan.TicksPerSecond).ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"\n");
        // Flight time is intentional. Measure the separate impact-to-visible
        // displacement delay on the host, where authoritative HP changes occur.
        double impactAt=double.Parse(Read(folder+"/impact-visible-"+id+".txt"),System.Globalization.CultureInfo.InvariantCulture);
        double movedAt=double.Parse(Read(folder+"/motion-visible-"+id+".txt"),System.Globalization.CultureInfo.InvariantCulture);
        double postImpact=(movedAt-impactAt)/TimeSpan.TicksPerSecond;
        File.AppendAllText(folder+"/post-impact-latency.csv",label+","+postImpact.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"\n");
        if(postImpact<-.02||postImpact>.15)throw new Exception("Post-impact presentation delay "+label+": "+postImpact);
        if(wall&&Number(target).position.x>1.1f)throw new Exception("Pushed through wall");
        if(blocker!=null)UnityEngine.Object.Destroy(blocker);
        yield return Wait(()=>Number(target)!=null&&!Number(target).hit&&Number(target).grounded,"landing clears reaction "+label);
        passed.Add("reaction motion/pose/input/stamina passed "+label);File.WriteAllLines(folder+"/progress.txt",passed);
    }
    static IEnumerator VerticalReactionCase(string label,int shooter,Vector2 direction)
    {
        foreach(var p in UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))p.ResetForArenaRound();
        yield return new WaitForSeconds(.4f);
        var command=AmmoCommand(shooter:shooter);command.first=new Vector2(0,.03f);command.second=new Vector2(0,3.23f);command.direction=direction;
        CombatCommand(command);yield return new WaitForSeconds(.8f);
        int target=shooter==1?2:1;float startY=Number(target).position.y;
        command.fire=true;CombatCommand(command);int id=combatRevision;
        yield return Wait(()=>Number(target)!=null&&Number(target).hit,"actual vertical shot hits "+label);
        yield return new WaitForSeconds(.13f);ScreenCapture.CaptureScreenshot(folder+"/reaction-"+label+".png");
        yield return new WaitForSeconds(.4f);
        float maxY=startY,maxVy=float.MinValue,minY=startY,maxX=0;
        foreach(string line in Read(folder+"/reaction-"+id+"-P"+target+".csv").Split('\n'))
        {
            var row=line.Trim().Split(',');if(row.Length!=13||row[9]!="True")continue;
            maxY=Mathf.Max(maxY,Value(row,2));minY=Mathf.Min(minY,Value(row,2));maxVy=Mathf.Max(maxVy,Value(row,4));maxX=Mathf.Max(maxX,Mathf.Abs(Value(row,3)));
        }
        if(Player(target).Health>=100||Player(shooter).ShotgunRounds!=5||maxX>.1f)throw new Exception("Vertical hit damage/ammo/horizontal direction "+label);
        if(direction.y>0&&(maxVy<4.5f||maxY-startY<.25f))throw new Exception("Upward shot failed to launch "+label);
        if(direction.y<0&&(maxY-startY>.08f||minY<-.16f))throw new Exception("Downward ground shot hopped or penetrated floor "+label);
        yield return Wait(()=>Number(target)!=null&&!Number(target).hit&&Number(target).grounded,"vertical landing "+label);
        passed.Add("signed vertical actual shot passed "+label);File.WriteAllLines(folder+"/progress.txt",passed);
    }
    static IEnumerator CheckTiming()
    {
        yield return Wait(()=>All(p=>p.connected&&p.match&&p.rounds==6),"four peers ready for timing",60);
        yield return ReactionCase("client-right",AmmoCommand());
        yield return ReactionCase("host-left",AmmoCommand(shooter:2));
        Player(2).ResetForArenaRound();yield return new WaitForSeconds(.4f);
        var range=AmmoCommand();range.first=new Vector2(-2.6f,.03f);range.second=new Vector2(2.6f,.03f);
        yield return ReactionCase("range-latency",range);
        yield return Wait(()=>All(p=>p.fastPresented),"all four peers received fast confirmed hit poses");
        var victim=Player(2);var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var transmit=typeof(SideScrollNetworkPlayer).GetMethod("FastHitPoseClientRpc",flags);
        var read=typeof(SideScrollNetworkPlayer).GetMethod("PresentationMotion",flags);
        uint sequence=(uint)typeof(SideScrollNetworkPlayer).GetField("receivedHitPose",flags).GetValue(victim);
        var pose=((NetworkVariable<SideScrollNetworkPlayer.MotionState>)typeof(SideScrollNetworkPlayer).GetField("motion",flags).GetValue(victim)).Value;
        var invalid=pose;invalid.LifeVersion=pose.LifeVersion-1;invalid.Position.x=500;
        transmit.Invoke(victim,new object[]{invalid,sequence+100});
        if(((SideScrollNetworkPlayer.MotionState)read.Invoke(victim,null)).Position.x>100)throw new Exception("Old life fast pose accepted");
        invalid=pose;invalid.Position.x=500;transmit.Invoke(victim,new object[]{invalid,1u});
        if(((SideScrollNetworkPlayer.MotionState)read.Invoke(victim,null)).Position.x>100)throw new Exception("Out of order fast pose accepted");
        yield return new WaitForSeconds(.2f);
        if(!All(p=>!p.sawInvalidPresentation))throw new Exception("Peer rendered stale fast pose");
        passed.Add("old life and stale fast pose sequence rejected");
        pose.Position.x+=.01f;pose.HitReaction=false;transmit.Invoke(victim,new object[]{pose,sequence+1});
        yield return new WaitForSeconds(.12f);
        var fallback=((NetworkVariable<SideScrollNetworkPlayer.MotionState>)typeof(SideScrollNetworkPlayer).GetField("motion",flags).GetValue(victim)).Value;
        if(((SideScrollNetworkPlayer.MotionState)read.Invoke(victim,null)).Position!=fallback.Position)throw new Exception("Fast pose did not expire to normal snapshot");
        passed.Add("fast pose expires to regular network state");File.WriteAllLines(folder+"/progress.txt",passed);
    }
    static IEnumerator CheckReaction()
    {
        yield return Wait(()=>All(p=>p.connected&&p.match&&p.rounds==6),"four peers ready for reaction",60);
        yield return ReactionCase("client-right",AmmoCommand());
        yield return ReactionCase("host-left",AmmoCommand(shooter:2));
        yield return Wait(()=>All(p=>p.sawHostHit&&p.sawClientHit),"all peers observe both hit reactions");
        Player(2).ResetForArenaRound();yield return new WaitForSeconds(.5f);
        yield return ReactionCase("crouch",AmmoCommand(crouched:true));
        Player(2).ResetForArenaRound();yield return new WaitForSeconds(.5f);
        yield return ReactionCase("wall",AmmoCommand(),true);
        Player(2).ResetForArenaRound();yield return new WaitForSeconds(.5f);
        var slope=AmmoCommand();slope.first=new Vector2(17,3.5f);slope.second=new Vector2(18.2f,3);slope.direction=new Vector2(1,-3.2f/9).normalized;
        yield return ReactionCase("slope",slope);
        Player(2).ResetForArenaRound();yield return new WaitForSeconds(.5f);
        var range=AmmoCommand();range.first=new Vector2(-2.6f,.03f);range.second=new Vector2(2.6f,.03f);
        yield return ReactionCase("range-latency",range);
        yield return VerticalReactionCase("upward-host",1,Vector2.up);
        yield return VerticalReactionCase("downward-client-ground",2,Vector2.down);
        foreach(var player in UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))player.ResetForArenaRound();yield return new WaitForSeconds(.4f);
        var falling=AmmoCommand();falling.first=new Vector2(18,8);falling.second=new Vector2(19,8);CombatCommand(falling);yield return new WaitForSeconds(.25f);
        var pushMethod=typeof(SideScrollNetworkPlayer).GetMethod("ShotgunPushClientRpc",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        pushMethod.Invoke(Player(2),new object[]{Vector2.down*SideScrollNetworkPlayer.ShotgunPush,Player(2).LifeVersion});
        yield return Wait(()=>Number(2)!=null&&Number(2).hit&&Number(2).vy< -4.5f,"client downward airborne velocity follows signed impulse",2);
        pushMethod.Invoke(Player(2),new object[]{Vector2.up*SideScrollNetworkPlayer.ShotgunPush,Player(2).LifeVersion});
        yield return Wait(()=>Number(2)!=null&&Number(2).hit&&Number(2).vy>4,"upward re-hit reverses downward velocity without adding speed",2);
        if(Number(2).vy>6.1f)throw new Exception("Vertical impulses stacked");
        yield return Wait(()=>Number(2)!=null&&Number(2).grounded&&!Number(2).hit,"vertical shove lands without floor penetration");
        if(Number(2).position.y<-.16f)throw new Exception("Vertical shove penetrated floor");
        // Two server-authorized pushes within the lock: no immunity, no additive speed.
        foreach(var player in UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))player.ResetForArenaRound();yield return new WaitForSeconds(.5f);
        var command=AmmoCommand();CombatCommand(command);yield return new WaitForSeconds(.5f);
        var victim=Player(2);var method=typeof(SideScrollNetworkPlayer).GetMethod("ShotgunPushClientRpc",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        method.Invoke(victim,new object[]{new Vector2(6,0),victim.LifeVersion});yield return new WaitForSeconds(.12f);
        method.Invoke(victim,new object[]{new Vector2(-6,0),victim.LifeVersion});yield return new WaitForSeconds(.13f);
        if(Number(2)==null||!Number(2).hit||Number(2).vx> -4.5f||Number(2).vx< -6.1f)throw new Exception("Repeated shove ignored or stacked speed");passed.Add("repeated hit refreshes reaction without speed stacking");
        uint oldLife=victim.LifeVersion;victim.ReceiveServerDamage(100,oldLife);yield return Wait(()=>Number(2)!=null&&Number(2).hp==0,"death interrupts reaction");
        var revive=AmmoCommand();revive.revive=true;CombatCommand(revive);yield return Wait(()=>Number(2)!=null&&Number(2).hp==100&&!Number(2).hit,"revive clears reaction");
        method.Invoke(victim,new object[]{new Vector2(6,0),oldLife});yield return new WaitForSeconds(.25f);
        if(Number(2).hit||Mathf.Abs(Number(2).vx)>.1f)throw new Exception("Old life push applied after revive");passed.Add("old life push rejected");
        Player(2).ResetForArenaRound();yield return new WaitForSeconds(.5f);
        yield return AmmoCase("normal shot has no new reaction",AmmoCommand(special:false),false,25);
        if(Number(2).hit||Mathf.Abs(Number(2).position.x-.65f)>.05f)throw new Exception("Normal shot caused reaction");
    }

    static IEnumerator AmmoCase(string label,Command command,bool shotgun,int damage,bool wall=false,bool push=false)
    {
        command.fire=false;CombatCommand(command);
        yield return Wait(()=>Number(1)!=null&&Number(2)!=null&&Mathf.Abs(Number(1).position.x-command.first.x)<.04f&&Mathf.Abs(Number(2).position.x-command.second.x)<.04f,"ammo placement "+label);
        yield return new WaitForSeconds(.6f);
        int shooter=command.shooter,target=shooter==1?2:1;var source=Player(shooter);var victim=Player(target);
        int rounds=source.ShotgunRounds,hp=victim.Health,shots=source.ReceivedShots,blasts=source.ReceivedShotgunBlasts,otherHp=Player(3).Health;
        GameObject blocker=null;
        if(wall){blocker=new GameObject("Ammo test blocker");blocker.transform.position=new Vector3(source.transform.position.x+command.direction.x*.32f,1.25f,0);blocker.AddComponent<BoxCollider2D>().size=new Vector2(.08f,2.5f);Physics2D.SyncTransforms();}
        command.fire=true;CombatCommand(command);
        yield return Wait(()=>source.ReceivedShots>shots,"ammo shot replicated "+label);
        yield return new WaitForSeconds(.14f);
        ScreenCapture.CaptureScreenshot(folder+"/ammo-path-"+combatRevision+".png");
        yield return new WaitForSeconds(.75f);
        if(blocker!=null)UnityEngine.Object.Destroy(blocker);
        if(victim.Health!=hp-damage||Player(3).Health!=otherHp)throw new Exception("Ammo damage "+label+": "+hp+" -> "+victim.Health+" expected "+damage);
        if(source.ShotgunRounds!=rounds-(shotgun?1:0)||source.ReceivedShots!=shots+1||source.ReceivedShotgunBlasts!=blasts+(shotgun?1:0))throw new Exception("Ammo use/rate "+label);
        if(push&&command.direction.x*(Number(target).position.x-(target==1?command.first.x:command.second.x))<.07f)throw new Exception("No owner knockback "+label);
        yield return Wait(()=>Number(shooter)!=null&&Number(shooter).rounds==source.ShotgunRounds&&Number(target)!=null&&Number(target).hp==victim.Health,"ammo/HP synchronized "+label);
        int host=Player(1).ReceivedShotgunBlasts,client=Player(2).ReceivedShotgunBlasts;
        yield return Wait(()=>All(p=>p.hostBlasts==host&&p.clientBlasts==client),"all peers agree on shotgun visuals "+label);
        passed.Add("ammo case passed "+label);File.WriteAllLines(folder+"/progress.txt",passed);
    }
    static Command AmmoCommand(int shooter=1,bool special=true,int choice=1,float gap=.65f,bool crouched=false)
    {return new Command{shooter=shooter,first=new Vector2(0,.03f),second=new Vector2(gap,.03f),direction=shooter==1?Vector2.right:Vector2.left,special=special,ammoChoice=choice,crouched=crouched};}
    static IEnumerator CheckAmmo()
    {
        yield return Wait(()=>All(p=>p.connected&&p.match&&p.rounds==6),"four peers start with six shotgun rounds",60);
        CombatCommand(AmmoCommand());yield return new WaitForSeconds(.5f);
        var source=Player(1);source.CycleAmmo(-1);if(source.SelectedAmmo!=SideScrollNetworkPlayer.AmmoKind.Normal)throw new Exception("Q selection");
        source.CycleAmmo(1);if(source.SelectedAmmo!=SideScrollNetworkPlayer.AmmoKind.Shotgun)throw new Exception("E selection");
        var remote=Player(2);var old=remote.SelectedAmmo;remote.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.Normal);if(remote.SelectedAmmo!=old)throw new Exception("Non-owner selection");
        passed.Add("Q/E selection and non-owner guard");
        yield return AmmoCase("right released uses basic",AmmoCommand(special:false),false,25);
        yield return AmmoCase("normal selected with right held",AmmoCommand(choice:0),false,25);
        foreach(var player in UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))player.ResetForArenaRound();yield return new WaitForSeconds(.5f);
        var flood=AmmoCommand();flood.flood=true;
        yield return AmmoCase("host point blank shotgun and server rate guard",flood,true,40,push:true);
        yield return AmmoCase("client left shotgun",AmmoCommand(shooter:2),true,40,push:true);
        yield return AmmoCase("barrel wall blocks all pellets",AmmoCommand(),true,0,wall:true);
        yield return AmmoCase("short range misses distant player",AmmoCommand(gap:7),true,0);
        var nearest=AmmoCommand(gap:2);nearest.third=new Vector2(3,.03f);
        yield return AmmoCase("pellets stop at nearest player",nearest,true,40,push:true);
        Player(2).ResetForArenaRound();yield return new WaitForSeconds(.5f);
        yield return AmmoCase("crouched point blank shotgun",AmmoCommand(crouched:true),true,40,push:true);
        yield return AmmoCase("last shotgun round",AmmoCommand(gap:7),true,0);
        yield return Wait(()=>Number(1)!=null&&Number(1).rounds==0&&!Number(1).activeShotgun,"empty automatically selects basic firing");
        yield return AmmoCase("empty while holding right fires basic",AmmoCommand(),false,25);
        CombatCommand(AmmoCommand());yield return new WaitForSeconds(.5f);ScreenCapture.CaptureScreenshot(folder+"/ammo-empty.png");yield return new WaitForSeconds(.2f);
        int count=source.ReceivedShots,ammo=source.ShotgunRounds;
        var menu=AmmoCommand();menu.menu=true;menu.fire=true;CombatCommand(menu);yield return new WaitForSeconds(1.3f);
        if(source.ReceivedShots!=count||source.ShotgunRounds!=ammo)throw new Exception("Menu allowed fire");passed.Add("menu blocks fire and spending");
        CombatCommand(AmmoCommand());yield return new WaitForSeconds(.4f);
        source.ReceiveServerDamage(100,source.LifeVersion);yield return Wait(()=>Number(1)!=null&&Number(1).hp==0,"shooter death synchronized");
        var dead=AmmoCommand();dead.fire=true;CombatCommand(dead);yield return new WaitForSeconds(1.3f);
        if(source.ReceivedShots!=count)throw new Exception("Dead shooter fired");
        var revive=AmmoCommand();revive.revive=true;CombatCommand(revive);
        yield return Wait(()=>Number(1)!=null&&Number(1).hp==100&&Number(1).rounds==6,"owner revive refills shotgun");
        var delayed=AmmoCommand(gap:3);delayed.fire=true;int before=source.ReceivedShotgunBlasts;CombatCommand(delayed);
        yield return Wait(()=>source.ReceivedShotgunBlasts>before,"shotgun launched before life reset");
        Player(2).ResetForArenaRound();yield return new WaitForSeconds(.6f);
        if(Player(2).Health!=100)throw new Exception("Old shotgun impact reached new life");passed.Add("old shotgun impact cannot damage new life");
        foreach(var player in UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))player.ResetForArenaRound();
        yield return Wait(()=>All(p=>p.hp==100&&p.rounds==6),"new round resets all ammo and HP");
        CombatCommand(AmmoCommand());yield return new WaitForSeconds(.4f);ScreenCapture.CaptureScreenshot(folder+"/ammo-ready.png");yield return new WaitForSeconds(.2f);
    }

    static bool BridgeAligned(out float width)
    {
        width=-1;var rules=UnityEngine.Object.FindFirstObjectByType<RelayArenaPrototype>(FindObjectsInactive.Include);if(rules==null)return false;
        width=rules.bridgeLeft.localScale.x;
        foreach(var leaf in new[]{rules.bridgeLeft,rules.bridgeRight})
        {
            var collider=leaf.GetComponentInChildren<BoxCollider2D>();if(collider==null)return false;
            if(width<=0){if(collider.enabled)return false;continue;}
            var bounds=leaf.GetComponent<SpriteRenderer>().bounds;
            if(!collider.enabled||Vector2.Distance(bounds.min,collider.bounds.min)>.001f||Vector2.Distance(bounds.max,collider.bounds.max)>.001f)return false;
        }
        return true;
    }
    static IEnumerator CheckBridge()
    {
        yield return Wait(()=>All(p=>p.connected&&p.match),"four peers ready for bridge",60);
        int id=1;var rules=RelayArenaPrototype.Instance;
        foreach(float opening in new[]{0f,.25f,.5f,.75f,1f,.75f,.5f,.25f,0f})
        {
            File.WriteAllText(folder+"/command.json",JsonUtility.ToJson(new Command{id=id++,mode=30,opening=opening}));
            float width=2*(1-opening);
            yield return Wait(()=>All(p=>p.bridgeAligned&&Mathf.Abs(p.leafWidth-width)<.001f),"visual and collision agree on all peers opening="+opening);
            yield return new WaitForSeconds(.9f);
            for(int number=1;number<=2;number++)
            {
                var peer=Number(number);if(peer==null)throw new Exception("Missing bridge owner");
                if(opening<1?(!peer.grounded||peer.position.y<-.3f):peer.position.y>-.3f)throw new Exception("Bridge support mismatch opening="+opening+" P"+number+" y="+peer.position.y);
            }
            var hit=Physics2D.Raycast(new Vector2(14,.1f),Vector2.down,.2f,1);
            if(opening==0?hit.collider==null:hit.collider!=null)throw new Exception("Centre gap collision mismatch "+opening);
            passed.Add("both edges supported until withdrawn; centre gap correct "+opening);File.WriteAllLines(folder+"/progress.txt",passed);
            rules.arenaCamera.transform.position=new Vector3(14,2.7f,-10);rules.arenaCamera.orthographicSize=3.4f;
            if(opening==.5f){ScreenCapture.CaptureScreenshot(folder+"/bridge-half-open.png");yield return new WaitForSeconds(.1f);}
        }
        File.WriteAllText(folder+"/command.json",JsonUtility.ToJson(new Command{id=id++,mode=31}));
        yield return Wait(()=>Number(1)!=null&&Number(2)!=null&&Number(1).position.x<0&&Number(2).position.x<0,"restore live bridge clock and players");
    }

    static bool CheckGroups()
    {
        var orders=new HashSet<int>();
        foreach(var player in UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))
        {
            var group=player.GetComponent<UnityEngine.Rendering.SortingGroup>();
            if(group==null||!group.enabled||!orders.Add(group.sortingOrder))return false;
            foreach(var nested in player.GetComponentsInChildren<UnityEngine.Rendering.SortingGroup>(true))
                if(nested!=group&&nested.sortAtRoot)return false;
            foreach(var sprite in player.GetComponentsInChildren<SpriteRenderer>(true))
            {
                // The leg rig has its own nested group; it must still belong to
                // this character's root group, including while crouched.
                bool inside=false;foreach(var parent in sprite.GetComponentsInParent<UnityEngine.Rendering.SortingGroup>(true))if(parent==group)inside=true;
                if(!inside)return false;
            }
        }
        return true;
    }
    static int combatRevision;
    static void CombatCommand(Command command){command.id=++combatRevision;File.WriteAllText(folder+"/command.json",JsonUtility.ToJson(command));}
    static IEnumerator CombatCase(string label,int shooter,Vector2 direction,Vector2 first,Vector2 second,bool crouched,bool shouldHit,bool wall=false,bool third=false)
    {
        var command=new Command{shooter=shooter,direction=direction,first=first,second=second,crouched=crouched,third=third?second+direction*1.5f:new Vector2(-19,.03f)};
        CombatCommand(command);yield return Wait(()=>Number(1)!=null&&Number(2)!=null&&Mathf.Abs(Number(1).position.x-first.x)<.04f&&Mathf.Abs(Number(2).position.x-second.x)<.04f,"placement "+label);
        yield return new WaitForSeconds(.7f);
        yield return Wait(()=>All(p=>p.grouped),"complete grouping in pose "+label);
        var source=Player(shooter);var victim=Player(shooter==1?2:1);var aim=source.GetComponentInChildren<RightFacingAimRig>();int hp=victim.Health,shots=source.ReceivedShots,thirdHp=Player(3).Health;
        GameObject blocker=null;
        if(wall){blocker=new GameObject("Close combat test blocker");blocker.transform.position=new Vector3(first.x+direction.x*.32f,1.25f,0);blocker.AddComponent<BoxCollider2D>().size=new Vector2(.08f,2.5f);Physics2D.SyncTransforms();}
        File.AppendAllText(folder+"/cases.txt",label+" hp="+hp+" muzzle="+aim.MuzzlePosition+" victim="+victim.transform.position+" grouped="+CheckGroups()+"\n");
        command.fire=true;CombatCommand(command);
        yield return Wait(()=>source.ReceivedShots>shots,"shot replicated "+label);
        yield return new WaitForSeconds(.65f);
        if(blocker!=null)UnityEngine.Object.Destroy(blocker);
        if(shouldHit?victim.Health!=hp-source.playerDamage:victim.Health!=hp)throw new Exception("Damage mismatch "+label+": "+hp+" -> "+victim.Health);
        if(Player(3).Health!=thirdHp)throw new Exception("Shot pierced nearest player "+label);
        yield return Wait(()=>Number(victim.PlayerNumber)!=null&&Number(victim.PlayerNumber).hp==victim.Health,"victim HP synchronized "+label);
        passed.Add("damage correct "+label);File.WriteAllLines(folder+"/progress.txt",passed);
    }
    static IEnumerator CheckCombat()
    {
        yield return Wait(()=>All(p=>p.connected&&p.match&&p.phase==0),"four peers ready for close combat",60);
        yield return CombatCase("host right point blank",1,Vector2.right,new Vector2(0,.03f),new Vector2(.65f,.03f),false,true);
        yield return Wait(()=>All(p=>p.grouped),"all peers group complete characters with unique order");
        ScreenCapture.CaptureScreenshot(folder+"/overlap-standing.png");yield return new WaitForSeconds(.2f);
        yield return CombatCase("client left point blank",2,Vector2.left,new Vector2(0,.03f),new Vector2(.65f,.03f),false,true);
        yield return CombatCase("host right crouched",1,Vector2.right,new Vector2(0,.03f),new Vector2(.65f,.03f),true,true);
        ScreenCapture.CaptureScreenshot(folder+"/overlap-crouched.png");yield return new WaitForSeconds(.2f);
        yield return CombatCase("client left crouched",2,Vector2.left,new Vector2(0,.03f),new Vector2(.65f,.03f),true,true);
        yield return CombatCase("barrel blocked before muzzle",1,Vector2.right,new Vector2(0,.03f),new Vector2(.65f,.03f),false,false,true);
        yield return CombatCase("target behind shooter",1,Vector2.right,new Vector2(0,.03f),new Vector2(-.65f,.03f),false,false);
        yield return CombatCase("normal range and nearest victim",1,Vector2.right,new Vector2(0,.03f),new Vector2(2,.03f),false,true,false,true);
        // Return both players to full health before the full-overlap check.
        Player(1).ResetForArenaRound();Player(2).ResetForArenaRound();yield return new WaitForSeconds(.5f);
        yield return CombatCase("full character overlap",1,Vector2.right,new Vector2(0,.03f),new Vector2(0,.03f),false,true);
        ScreenCapture.CaptureScreenshot(folder+"/overlap-full.png");yield return new WaitForSeconds(.2f);
        Player(2).ReceiveServerDamage(100,Player(2).LifeVersion);yield return Wait(()=>Number(2)!=null&&Number(2).hp==0,"overlap victim death synchronized");
        yield return new WaitForSeconds(.8f);yield return Wait(()=>All(p=>p.grouped),"death pose remains inside character group");
        ScreenCapture.CaptureScreenshot(folder+"/overlap-death.png");yield return new WaitForSeconds(.2f);
        CombatCommand(new Command{first=new Vector2(0,.03f),second=new Vector2(.65f,.03f),revive=true});
        yield return Wait(()=>All(p=>p.grouped)&&Number(2)!=null&&Number(2).hp==100,"revive preserves character grouping");
    }

    static IEnumerator JoinTraverse(SideScrollNetworkPlayer owner,int side,bool drop,bool upper)
    {
        string route=side<0?"West":"East",deck=upper?"Upper relay deck":route+" mezzanine",ramp=route+(upper?" upper ramp":" lower ramp");
        var move=owner.GetComponent<SideScrollMapPreview>();var crouch=owner.GetComponent<CrouchTrial>();var body=owner.GetComponent<Rigidbody2D>();var motor=owner.GetComponent<ArenaPlatformMotor>();
        crouch.SetRequested(false);move.SetInput(0);
        if(drop)
        {
            move.SetSpawnPosition(new Vector2(side*10.5f,5.4f));
            yield return Wait(()=>move.Grounded&&move.GroundCollider!=null&&move.GroundCollider.name==route+" upper ramp",route+" overlapping ramp setup");
            crouch.SetRequested(true);yield return Wait(()=>crouch.IsCrouched,"crouch before overlapping ramp drop "+route);move.SetInput(0,true);
            yield return Wait(()=>move.Grounded&&move.GroundCollider!=null&&move.GroundCollider.name==deck,"drop onto overlapping solid deck "+route);
            // Keep holding crouch while leaving the overlap, as in the reported route.
        }
        else
        {
            move.SetSpawnPosition(new Vector2(side*(upper?3f:11.5f),upper?6.43f:3.23f));
            yield return Wait(()=>move.Grounded&&move.GroundCollider!=null&&move.GroundCollider.name==deck,"solid deck setup "+route+" upper="+upper);
        }
        move.SetInput(side);float end=side*(upper?6.5f:15f),until=Time.realtimeSinceStartup+12;
        var samples=new List<string>{"time,x,y,feet,vx,vy,grounded,support,targetIgnored"};var target=GameObject.Find(ramp).GetComponent<Collider2D>();
        while(side*owner.transform.position.x<side*end&&Time.realtimeSinceStartup<until)
        {
            samples.Add(Time.time+","+owner.transform.position.x+","+owner.transform.position.y+","+owner.GetComponent<BoxCollider2D>().bounds.min.y+","+body.linearVelocity.x+","+body.linearVelocity.y+","+move.Grounded+","+(move.GroundCollider!=null?move.GroundCollider.name:"")+","+motor.IsIgnored(target));yield return null;
        }
        move.SetInput(0);yield return new WaitForSeconds(.3f);
        File.WriteAllLines(folder+"/join-"+route+"-drop-"+drop+"-upper-"+upper+".csv",samples);
        if(side*owner.transform.position.x<side*end-.1f||!move.Grounded||move.GroundCollider==null||move.GroundCollider.name!=ramp||owner.transform.position.y<(upper?4.5f:1.8f))
            throw new Exception("Fell through joined ramp: "+route+" drop="+drop+" upper="+upper+" position="+owner.transform.position+" support="+(move.GroundCollider!=null?move.GroundCollider.name:"none"));
        passed.Add("deck to ramp supported "+route+" drop="+drop+" upper="+upper);File.WriteAllLines(folder+"/progress.txt",passed);
    }
    static IEnumerator CheckDropReentry()
    {
        yield return Wait(()=>All(p=>p.connected&&p.match&&p.phase==0),"four peers ready for airborne drop reentry",60);
        foreach(int owner in new[]{1,2})foreach(bool upper in new[]{false,true})foreach(int side in new[]{-1,1})
        {
            string ramp=(side<0?"West":"East")+(upper?" upper ramp":" lower ramp");
            // Lower x17 is outside the retracting bridge/pit, so the safe-drop
            // command is available regardless of that bridge's current phase.
            CombatCommand(new Command{mode=44,shooter=owner,first=new Vector2(side*(upper?8:17),upper?6.3f:4),direction=Vector2.zero});int id=combatRevision;
            string tag=id+"-P"+owner;
            yield return Wait(()=>File.Exists(folder+"/reentry-jumped-"+tag+".txt"),"air jump after deliberate drop P"+owner+" "+ramp);
            yield return new WaitForSeconds(.35f);
            yield return Wait(()=>Number(owner)!=null&&Number(owner).grounded,"airborne reentry landing P"+owner+" "+ramp,2);
            var peer=Number(owner);
            if(peer.support!=ramp||File.Exists(folder+"/reentry-lower-"+tag+".txt"))throw new Exception("Did not re-land on original ramp before lower ground P"+owner+" expected="+ramp+" actual="+peer.support);
            passed.Add("drop air-jump lands on original ramp P"+owner+" "+ramp);File.WriteAllLines(folder+"/progress.txt",passed);
            if(owner==1&&!upper){ScreenCapture.CaptureScreenshot(folder+"/air-reentry-"+side+".png");yield return new WaitForSeconds(.15f);}
        }
        CombatCommand(new Command{shooter=0,direction=Vector2.zero});seamsOnly=false;yield return new WaitForSeconds(.3f);
        yield return CheckJoins();
    }
    static IEnumerator CheckSeams()
    {
        yield return Wait(()=>All(p=>p.connected&&p.match&&p.phase==0),"four peers ready for platform seams",60);
        int id=0;
        if(!seamsFollowup)foreach(int owner in new[]{1,2})foreach(int mode in new[]{1,0,2})foreach(int side in new[]{-1,1})foreach(int direction in new[]{-1,1})
        {
            float start=side*5-direction*1.2f,end=side*5+direction*.85f;
            CombatCommand(new Command{id=++id,shooter=owner,first=new Vector2(start,3.23f),direction=new Vector2(direction,0),crouched=mode==2,special=mode==1});
            yield return new WaitForSeconds(.65f);
            float deadline=Time.realtimeSinceStartup+7;
            while(Number(owner)!=null&&direction*(Number(owner).position.x-end)<0&&Time.realtimeSinceStartup<deadline)
            {
                if(Number(owner).position.y<2.95f)break;
                yield return null;
            }
            var p=Number(owner);
            if(p==null||p.position.y<2.95f||direction*(p.position.x-end)<0)
            {ScreenCapture.CaptureScreenshot(folder+"/seam-failure.png");yield return new WaitForSeconds(.15f);throw new Exception("Platform seam P"+owner+" mode="+mode+" side="+side+" direction="+direction+" pos="+(p!=null?p.position.ToString():"none"));}
            passed.Add("seam P"+owner+" mode="+mode+" side="+side+" direction="+direction);File.WriteAllLines(folder+"/progress.txt",passed);
        }
        foreach(int owner in new[]{1,2})
        {
            foreach(int side in new[]{-1,1})foreach(int direction in new[]{-1,1})
            {
                CombatCommand(new Command{shooter=owner,mode=40,first=new Vector2(side*5-direction*.35f,3.23f),direction=new Vector2(direction,0)});
                yield return Wait(()=>Number(owner)!=null&&Number(owner).hit,"hit launches at seam P"+owner+" side="+side+" direction="+direction);
                yield return new WaitForSeconds(1.1f);
                var p=Number(owner);
                if(p==null||!p.grounded||p.position.y<2.95f||direction*(p.position.x-side*5)<.3f)throw new Exception("Hit seam landing failed "+owner+" "+side+" "+direction+" "+(p!=null?p.position.ToString():"none"));
                passed.Add("hit lands across seam P"+owner+" side="+side+" direction="+direction);
                if(owner==1&&side==1&&direction==1){ScreenCapture.CaptureScreenshot(folder+"/seam-hit-landing.png");yield return new WaitForSeconds(.15f);}
            }
            CombatCommand(new Command{shooter=owner,mode=41,first=new Vector2(3.6f,1.23f),direction=Vector2.zero});
            yield return new WaitForSeconds(2.9f);
            yield return Wait(()=>Number(owner)!=null&&Number(owner).grounded&&Number(owner).support=="Atrium grated crossing","jump through and land above grate P"+owner,2);
            CombatCommand(new Command{shooter=owner,mode=42,crouched=true,first=new Vector2(0,3.23f),direction=Vector2.zero});
            yield return new WaitForSeconds(2);
            yield return Wait(()=>Number(owner)!=null&&Number(owner).grounded&&Number(owner).position.y<.2f,"intentional crouch drop reaches lower floor P"+owner,2);
            foreach(int side in new[]{-1,1})
            {
                CombatCommand(new Command{shooter=owner,mode=43,first=new Vector2(side*15,4),direction=Vector2.zero});
                yield return new WaitForSeconds(1.2f);
                yield return Wait(()=>Number(owner)!=null&&Number(owner).grounded&&Number(owner).support==(side<0?"West":"East")+" lower ramp","idle slope setup P"+owner+" side="+side);
                var p=Number(owner);Vector3 before=p.position;yield return new WaitForSeconds(1);
                p=Number(owner);if(!p.grounded||Vector3.Distance(before,p.position)>.02f)throw new Exception("Slope idle drift P"+owner+" side="+side);
                passed.Add("rounded sole stays still on slope P"+owner+" side="+side);
            }
        }
        CombatCommand(new Command{id=++id,shooter=0,direction=Vector2.zero});
        // The ramp review drives the host directly; stop the seam actor from
        // overwriting its crouch/input requests during that handoff.
        if(seamsFollowup){seamsOnly=false;yield return new WaitForSeconds(.3f);yield return CheckJoins();}
    }
    static IEnumerator CheckJoins()
    {
        yield return Wait(()=>All(p=>p.connected&&p.match&&p.phase==0),"four peers ready for ramp joins",60);
        var owner=Player(1);var move=owner.GetComponent<SideScrollMapPreview>();var crouch=owner.GetComponent<CrouchTrial>();
        owner.GetComponentInChildren<RightFacingAimRig>().SetAimDirection(Vector2.right);owner.GetComponentInChildren<RightFacingShotPreview>().readMouse=false;move.SetInput(0);
        foreach(int side in new[]{-1,1})
        {
            yield return JoinTraverse(owner,side,true,false);
            ScreenCapture.CaptureScreenshot(folder+"/joined-ramp-"+side+".png");yield return new WaitForSeconds(.2f);
            yield return JoinTraverse(owner,side,false,false);yield return JoinTraverse(owner,side,false,true);
            string route=side<0?"West":"East";
            move.SetSpawnPosition(new Vector2(side*20.3f,.03f));move.SetInput(0);yield return new WaitForSeconds(.3f);move.SetInput(-side);
            yield return Wait(()=>side*owner.transform.position.x<17.5f,"ramp underpass "+route);move.SetInput(0);
            if(owner.transform.position.y>.15f)throw new Exception("Auto-mounted ramp from below "+route);passed.Add("underpass stays on ground "+route);
            move.SetSpawnPosition(new Vector2(side*19,2.5f));move.SetInput(0);
            yield return Wait(()=>move.Grounded&&move.GroundCollider!=null&&move.GroundCollider.name==route+" lower ramp","short drop setup "+route);
            crouch.SetRequested(true);yield return Wait(()=>crouch.IsCrouched,"short drop crouch "+route);move.SetInput(0,true);
            yield return Wait(()=>move.Grounded&&owner.transform.position.y<.1f,"short drop reaches ground "+route);
            crouch.SetRequested(false);yield return Wait(()=>!crouch.IsCrouched,"short drop stand "+route);move.SetInput(0,true);
            yield return Wait(()=>move.Grounded&&move.GroundCollider!=null&&move.GroundCollider.name==route+" lower ramp","immediate jump reentry "+route,1.45f);
            move.SetSpawnPosition(new Vector2(side*21.8f,.03f));move.SetInput(0);yield return new WaitForSeconds(.3f);move.SetInput(-side,true);
            yield return Wait(()=>side*owner.transform.position.x<12.5f,"jump entry and ascent "+route,12);move.SetInput(0);yield return new WaitForSeconds(.3f);
            if(!move.Grounded||owner.transform.position.y<2.8f)throw new Exception("Ramp ascent height "+route);passed.Add("jump ascent reaches deck "+route);
        }
    }

}
