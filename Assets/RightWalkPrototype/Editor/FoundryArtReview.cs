using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using RightWalkPrototype;
[InitializeOnLoad]
public static class FoundryArtReview
{
    const string Key="FoundryArtReview",Folder="LocalTools/Analysis/FoundryArt";
    static readonly List<string> results=new();
    static FoundryArtReview(){EditorApplication.update+=Tick;}
    public static void Begin(){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    static void Tick()
    {if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||Time.time<.5f)return;var s=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();if(s==null)return;SessionState.SetBool(Key,false);results.Clear();s.StartCoroutine(Guard(Run(s)));}
    static IEnumerator Guard(IEnumerator run)
    {
        var stack=new Stack<IEnumerator>();stack.Push(run);
        while(stack.Count>0){bool next=false;object value=null;Exception error=null;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){File.WriteAllText(Folder+"/result.txt","ERROR "+error+"\n"+string.Join("\n",results));yield break;}if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}
        File.WriteAllText(Folder+"/result.txt",(results.Exists(x=>x.StartsWith("FAIL"))?"FAIL":"PASS")+"\n"+string.Join("\n",results));
    }
    static void Check(bool pass,string text){results.Add((pass?"PASS ":"FAIL ")+text);}
    static IEnumerator Wait(Func<bool> pass,string label,float seconds=20){float until=Time.time+seconds;while(!pass()&&Time.time<until)yield return null;if(!pass())throw new Exception(label);}
    static IEnumerator Frames(int n=3){for(int i=0;i<n;i++)yield return null;}
    static IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"/"+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
    static Dictionary<int,ArenaPatrolEnemy> Views(ArenaObjectiveMatch match)=>(Dictionary<int,ArenaPatrolEnemy>)typeof(ArenaObjectiveMatch).GetField("patrolViews",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(match);
    static IEnumerable<ArenaObjectiveMatch.PatrolState> States(ArenaObjectiveMatch match){for(int i=0;i<match.Patrols.Count;i++)yield return match.Patrols[i];}
    static IEnumerator Run(SideScrollNetworkSession session)
    {
        var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));var prop=game.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);int saved=(int)prop.GetValue(game);
        game.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(game,new object[]{new Vector2(1366,768),"Art review 1366"});game.Focus();
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();var art=map.transform.Find(FoundryArtBuilder.RootName);
        Check(art!=null&&art.GetComponentsInChildren<Collider2D>().Length==0,"map art exists without colliders");
        var backdrop=art.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.sprite!=null&&r.sprite.name=="DepotWall").ToArray();
        bool covered=true;for(float x=-35.5f;x<=35.5f;x+=4)for(float y=-3.5f;y<=27.5f;y+=2.5f)covered&=backdrop.Any(r=>r.bounds.Contains(new Vector3(x,y,0)));
        Check(backdrop.Length==18&&covered,"full background covers all floors, outer lanes and basement");
        Check(backdrop.Select(r=>r.sprite.texture).Distinct().Count()==1&&backdrop.All(r=>r.sortingOrder<0),"full background reuses one texture behind gameplay");
        Check(map.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="Grate opening"||r.name=="Deck joint").All(r=>!r.enabled),"no repeated interior dash decorations");
        Check(map.GetComponentsInChildren<ArenaCover>().Where(c=>map.ContainsCombatPosition(c.transform.position)).All(c=>c.transform.Find(FoundryCoverArtBuilder.ArtName)!=null),"every existing combat-map cover uses metal case artwork");
        session.StartSoloPractice();yield return Wait(()=>ArenaObjectiveMatch.PracticeRoom,"solo host");var match=ArenaObjectiveMatch.Instance;match.SetLocalReady(true);yield return Wait(()=>match.CanStart,"ready");if(!match.StartRound())throw new Exception("start");yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing,"playing");yield return new WaitForSeconds(1.7f);
        var views=Views(match);
        foreach(bool turret in new[]{false,true})
        {
            var states=States(match).Where(p=>!p.Medical&&p.Turret==turret&&!p.Flying).ToArray();
            Check(states.Length>0&&states.All(p=>views[p.Id].transform.Find("Enemy body/Security unit artwork")!=null),turret?"all live turrets use layered artwork":"all live robots use layered artwork");
        }
        Check(States(match).Where(p=>p.Flying&&!p.Medical).All(p=>views[p.Id].transform.Find("Enemy body/Security unit artwork")!=null),"live combat drones use layered artwork");
        Check(views.Values.All(v=>v.GetComponent<BoxCollider2D>().size==new Vector2(States(match).First(p=>p.Id==v.Id).Flying||States(match).First(p=>p.Id==v.Id).Turret?.85f:ArenaPatrolEnemy.GroundHitWidth,.9f)&&v.GetComponent<BoxCollider2D>().offset==new Vector2(0,.48f)),"ground shot collider matches widened hull; other enemy sizes unchanged");
        Check(States(match).Where(p=>p.Medical).All(p=>views[p.Id].transform.Find("Enemy body/SupportCargo")!=null&&views[p.Id].transform.Find("Enemy body/Aimed barrel")==null),"support drone keeps its own unarmed cargo appearance");
        int[] ids=new[]{States(match).First(p=>!p.Medical&&!p.Turret&&!p.Flying).Id,States(match).First(p=>p.Turret).Id,States(match).First(p=>p.Flying&&!p.Medical).Id};
        // Freeze AI positions for a legible art capture, not a claim of natural spawns.
        for(int i=0;i<match.Patrols.Count;i++)
        {
            var state=match.Patrols[i];int selected=Array.IndexOf(ids,state.Id);
            if(selected<0){state.Health=0;state.RespawnAt=match.Now+600;}
            else
            {
                // PingPong requires a positive route span, even at zero speed.
                state.Roaming=false;state.Left=selected==0?5.8f:selected==1?-5.8f:4.8f;state.Right=state.Left+.01f;state.Floor=selected==2?9.4f:map.floorSpacing;
                state.Aim=selected==0?Vector2.left:new Vector2(selected==1?1:-1,-.2f).normalized;state.Speed=0;state.NextAttack=match.Now+600;state.FireAt=0;state.RushPhase=0;state.EntryUntil=0;
            }
            match.Patrols[i]=state;
        }
        match.TurretShots.Clear();var transit=match.MedicalTransit.Value;transit.NextAt=match.Now+600;match.MedicalTransit.Value=transit;
        var player=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>();var movement=player.GetComponent<SideScrollMapPreview>();movement.SetInput(0);movement.SetSpawnPosition(new Vector2(-.7f,map.floorSpacing+.04f));
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(Screen.width*.75f,Screen.height*.5f)});yield return new WaitForSeconds(1.2f);
        Check(views[ids[0]].transform.Find("Enemy body/Security unit artwork").localScale.x<0,"robot bumper follows left-facing movement");
        var normalSensor=views[ids[0]].transform.Find("Enemy body/Security unit artwork/GroundSensor").GetComponent<SpriteRenderer>();
        Check(normalSensor.color.r>normalSensor.color.g*2,"normal robot sensor retains red danger color");
        foreach(int id in ids)Check(!float.IsNaN(views[id].transform.position.x)&&views[id].gameObject.activeSelf,"capture fixture has finite live position: "+id);
        yield return Capture("depot-gameplay");
        yield return GroundFacing(match,player,movement,ids[0]);
        yield return EnemyFollowups(match,player,movement,ids[0],ids[2]);
        foreach(int id in ids.Skip(1))
        {
            int index=Enumerable.Range(0,match.Patrols.Count).First(i=>match.Patrols[i].Id==id);
            bool okay=true,upright=true;foreach(float angle in new[]{0f,90f,180f,-90f,35f})
            {
                var s=match.Patrols[index];s.Aim=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));match.Patrols[index]=s;yield return Frames();
                var barrel=views[id].transform.Find("Enemy body/Aimed barrel");okay&=Vector2.Distance(barrel.TransformPoint(Vector3.right*.6f),match.TurretMuzzle(s))<.001f;
                upright&=barrel.TransformVector(Vector3.up).y>=-.001f;
            }
            Check(okay,"aimed barrel retains server muzzle at five angles: "+id);
            Check(upright,"barrel top stays upright at left/right/vertical aim: "+id);
        }
        foreach(int id in ids)
        {
            int index=Enumerable.Range(0,match.Patrols.Count).First(i=>match.Patrols[i].Id==id);var s=match.Patrols[index];s.Aim=Vector2.left;s.FireAt=match.Now+60;s.RushPhase=1;s.RushUntil=match.Now+60;match.Patrols[index]=s;
        }
        yield return Frames();yield return Capture("attack-warning");
        foreach(int id in ids)
        {
            int index=Enumerable.Range(0,match.Patrols.Count).First(i=>match.Patrols[i].Id==id);var s=match.Patrols[index];s.Health--;match.Patrols[index]=s;
            views[id].Apply(s,true,4);var part=views[id].GetComponentsInChildren<SpriteRenderer>().First(r=>r.name.Contains("Hull")||r.name=="TurretBase");var block=new MaterialPropertyBlock();part.GetPropertyBlock(block);
            Check(block.GetFloat("_HitFlash")==1&&part.sharedMaterial.shader.isSupported,"art hit flash enabled on supported sprite shader: "+id);
        }
        yield return Capture("hit-flash");yield return new WaitForSeconds(.2f);
        foreach(int id in ids)
        {
            int index=Enumerable.Range(0,match.Patrols.Count).First(i=>match.Patrols[i].Id==id);var s=match.Patrols[index];s.EntrySeconds=1.4f;s.EntryUntil=match.Now+1.4;match.Patrols[index]=s;
        }
        yield return Frames();Check(ids.All(id=>!views[id].GetComponent<BoxCollider2D>().enabled),"entrance still disables enemy colliders");yield return Capture("entry-warning");
        yield return new WaitForSeconds(1.5f);Check(ids.All(id=>views[id].GetComponent<BoxCollider2D>().enabled),"entrance completes with colliders restored");
        foreach(int id in ids){var s=States(match).First(p=>p.Id==id);match.DamagePatrolServer(id,s.Generation,999,player);}yield return Frames();Check(ids.All(id=>!views[id].gameObject.activeSelf),"server destruction hides layered artwork");
        yield return FullBackgroundScreens(match,player,movement);
        session.DisconnectLocal();yield return Frames();prop.SetValue(game,saved);
    }
    static IEnumerator FullBackgroundScreens(ArenaObjectiveMatch match,SideScrollNetworkPlayer player,SideScrollMapPreview movement)
    {
        for(int i=0;i<match.Patrols.Count;i++){var state=match.Patrols[i];state.Health=0;state.RespawnAt=match.Now+600;match.Patrols[i]=state;}
        movement.readKeyboard=false;player.GetComponentInChildren<RightFacingShotPreview>().readMouse=false;var aim=player.GetComponentInChildren<RightFacingAimRig>();aim.mouseAim=false;
        var points=new List<Vector2>{new(-28,.04f),new(28,.04f)};points.AddRange(match.Points.Select(p=>new Vector2(p.CaptureBounds.center.x,p.CaptureBounds.min.y+.04f)));
        for(int i=0;i<points.Count;i++)
        {
            movement.SetInput(0);movement.SetSpawnPosition(points[i]);aim.SetAimTarget((Vector3)points[i]+Vector3.right*5+Vector3.up*1.3f);yield return new WaitForSeconds(.65f);yield return Capture("full-background-"+i);
        }
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();var camera=Camera.main;var position=camera.transform.position;float size=camera.orthographicSize;var target=camera.targetTexture;var previous=RenderTexture.active;
        var rt=new RenderTexture(1366,768,24);camera.targetTexture=rt;camera.transform.position=new Vector3(map.cameraBounds.center.x,map.cameraBounds.center.y,position.z);camera.orthographicSize=Mathf.Max(map.cameraBounds.height/2,map.cameraBounds.width/2/camera.aspect);camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1366,768);image.ReadPixels(new Rect(0,0,1366,768),0,0);image.Apply();File.WriteAllBytes(Folder+"/full-map-overview.png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
        camera.targetTexture=target;camera.transform.position=position;camera.orthographicSize=size;RenderTexture.active=previous;rt.Release();UnityEngine.Object.Destroy(rt);
        Check(true,"captured full-map overview and eight gameplay landmarks");
    }
    static IEnumerator GroundFacing(ArenaObjectiveMatch match,SideScrollNetworkPlayer player,SideScrollMapPreview movement,int id)
    {
        int index=Enumerable.Range(0,match.Patrols.Count).First(i=>match.Patrols[i].Id==id);var original=match.Patrols[index];var view=Views(match)[id];var face=view.transform.Find("Enemy body/Security unit artwork");
        float floor=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>().floorSpacing;var trace=new List<string>{"direction,serverX,viewX,velocityX,staleAimX,facingX,angle,rush"};
        // Isolate ordinary pursuit. Otherwise the real AI immediately enters
        // charge at this distance and correctly replaces the deliberately stale aim.
        var rushRange=typeof(ArenaObjectiveMatch).GetField("groundRushRange",BindingFlags.NonPublic|BindingFlags.Instance);float savedRange=(float)rushRange.GetValue(match);rushRange.SetValue(match,0f);
        foreach(int direction in new[]{1,-1})
        {
            float robotX=direction>0?-9.5f:-6.5f,targetX=direction>0?-6.5f:-9.5f;
            movement.SetInput(0);movement.SetSpawnPosition(new Vector2(targetX,floor+.04f));
            var state=original;state.Roaming=true;state.Position=new Vector2(robotX,floor+.02f);state.Velocity=Vector2.zero;state.Angle=0;state.Aim=Vector2.left*direction;state.EntryUntil=0;state.RushPhase=0;state.NextAttack=match.Now+600;state.FireAt=0;
            typeof(ArenaObjectiveMatch).GetMethod("ResetEnemyBrain",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(match,new object[]{id});match.Patrols[index]=state;
            yield return Wait(()=>match.Patrols[index].Velocity.x*direction>.2f&&Mathf.Abs(match.Patrols[index].Angle)<.5f,"actual flat-ground pursuit starts: "+direction,5);
            bool aligned=true,travelled=false;float start=match.Patrols[index].Position.x;
            for(int sample=0;sample<6;sample++)
            {
                yield return new WaitForSeconds(.06f);state=match.Patrols[index];aligned&=face.localScale.x*state.Velocity.x>0;travelled|=(state.Position.x-start)*direction>.08f;
                trace.Add(direction+","+state.Position.x+","+view.transform.position.x+","+state.Velocity.x+","+state.Aim.x+","+face.localScale.x+","+state.Angle+","+state.RushPhase);
            }
            Check(aligned&&travelled&&state.Aim.x*direction<0,"actual pursuit faces travel despite stale opposite aim: "+direction);yield return Capture(direction>0?"ground-facing-right":"ground-facing-left");
            var held=face.localScale;state.Velocity=Vector2.zero;state.Aim=Vector2.right*direction;view.Apply(state,true,4);Check(face.localScale==held,"stationary robot retains last travel facing: "+direction);
            state.RushPhase=1;state.Aim=Vector2.right*direction;state.Velocity=Vector2.left*direction;view.Apply(state,true,4);Check(face.localScale.x*direction>0,"charge uses locked attack direction: "+direction);
            state.RushPhase=2;view.Apply(state,true,4);Check(face.localScale.x*direction>0,"rush uses locked attack direction: "+direction);
        }
        File.WriteAllLines(Folder+"/ground-facing.csv",trace);rushRange.SetValue(match,savedRange);match.Patrols[index]=original;movement.SetSpawnPosition(new Vector2(-.7f,floor+.04f));yield return Frames();
    }
    static IEnumerator EnemyFollowups(ArenaObjectiveMatch match,SideScrollNetworkPlayer player,SideScrollMapPreview movement,int groundId,int droneId)
    {
        bool enabled=match.enabled;match.enabled=false;var views=Views(match);var ground=views[groundId];var aim=player.GetComponentInChildren<RightFacingAimRig>();Physics2D.SyncTransforms();
        Vector2 root=ground.transform.position;aim.TraceShot(root+new Vector2(.5f,1.6f),Vector2.down,2,out Collider2D edge,true);
        Check(edge==ground.GetComponent<Collider2D>(),"shared server shot trace hits outer hull beyond old .425 half-width");
        aim.TraceShot(root+new Vector2(.68f,1.6f),Vector2.down,2,out Collider2D outside,true);Check(outside!=ground.GetComponent<Collider2D>(),"shot trace beyond widened hull still misses");
        int index=Enumerable.Range(0,match.Patrols.Count).First(i=>match.Patrols[i].Id==droneId);var original=match.Patrols[index];float floor=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>().floorSpacing;
        movement.SetInput(0);movement.SetSpawnPosition(new Vector2(-6.5f,floor+.04f));yield return new WaitForSeconds(.3f);
        var drone=original;drone.Roaming=false;drone.Left=-8;drone.Right=-7.99f;drone.Floor=floor+2.7f;drone.Speed=0;drone.EntryUntil=0;drone.FireAt=0;drone.BurstLeft=0;drone.NextAttack=match.Now;match.Patrols[index]=drone;match.TurretShots.Clear();
        var update=typeof(ArenaObjectiveMatch).GetMethod("UpdateTurret",BindingFlags.NonPublic|BindingFlags.Instance);double began=match.Now;
        update.Invoke(match,new object[]{index,match.Patrols[index],new[]{player}});var scheduled=match.Patrols[index];Check(scheduled.FireAt-began>=1.19,"combat drone schedules a longer first-shot warning");views[droneId].Apply(scheduled,true,4);yield return Capture("drone-first-warning");
        yield return new WaitForSeconds(.8f);update.Invoke(match,new object[]{index,match.Patrols[index],new[]{player}});Check(match.TurretShots.Count==0,"no combat-drone shot during first .8 seconds");
        yield return Wait(()=>match.Now>=scheduled.FireAt+.02,"first-shot warning elapsed",3);update.Invoke(match,new object[]{index,match.Patrols[index],new[]{player}});Check(match.TurretShots.Count==1,"combat drone fires once after full warning");
        double first=match.TurretShots.Count>0?match.TurretShots[0].Began:0,secondAt=match.Patrols[index].FireAt;
        yield return new WaitForSeconds(.13f);update.Invoke(match,new object[]{index,match.Patrols[index],new[]{player}});Check(match.TurretShots.Count==1,"follow-up shot waits for existing burst interval");
        yield return Wait(()=>match.Now>=secondAt+.01,"burst interval elapsed",2);update.Invoke(match,new object[]{index,match.Patrols[index],new[]{player}});Check(match.TurretShots.Count==2&&match.TurretShots[1].Began-first>=.299,"two-shot burst keeps its .3 second spacing");
        File.WriteAllText(Folder+"/drone-timing.txt","scheduled warning="+(scheduled.FireAt-began)+"\nfirst shot delay="+(first-began)+"\nburst gap="+(match.TurretShots.Count==2?match.TurretShots[1].Began-first:0));
        match.TurretShots.Clear();match.Patrols[index]=original;match.enabled=enabled;movement.SetSpawnPosition(new Vector2(-.7f,floor+.04f));yield return Frames();
    }
}
