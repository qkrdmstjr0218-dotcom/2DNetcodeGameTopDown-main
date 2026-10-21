#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace RightWalkPrototype
{
    // Opt-in integration driver. Both actors use the real input, shot RPC and revive paths.
    public static class SideScrollNetworkCombatVerification
    {
        [Serializable] public class Command
        {
            public int id,hostShots,clientShots;
            public bool clientCrouch,hostCrouch,rawClientShot,clientRevive,hostJump,reconnect,practice;
            public float targetHeight=1.1f,hostMove,clientMove;
        }
        [Serializable] public class Status
        {
            public int id,hostHP,clientHP,hostNativeHP,clientNativeHP;
            public int deadTargets;
            public bool hostDead,clientDead,localControls,remoteControls,clientCrouch,hostCrouch;
            public ulong owner;
            public uint hostLife,clientLife;
        }
        static string output;
        static Command command;
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        public static IEnumerator Run(SideScrollNetworkSession session,bool host,string folder)
        {
            output=folder;Directory.CreateDirectory(folder);
            yield return null;session.StartLocal(host);
            var stack=new Stack<IEnumerator>();stack.Push(host?Host(session):Client(session));
            while(stack.Count>0)
            {
                object next=null;bool more=false;Exception failure=null;
                try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}
                catch(Exception e){failure=e;}
                if(failure!=null)
                {File.WriteAllText(Path.Combine(folder,(host?"host":"client")+"-result.txt"),"FAIL: "+failure);yield break;}
                if(!more){stack.Pop();continue;}
                if(next is IEnumerator nested)stack.Push(nested);else yield return next;
            }
        }
        static SideScrollNetworkPlayer Player(bool host)
        {
            foreach(var p in UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))
                if((p.OwnerClientId==0)==host)return p;
            return null;
        }
        static IEnumerator Until(Func<bool> condition,string label,float seconds=8)
        {
            float deadline=Time.realtimeSinceStartup+seconds;
            while(!condition()&&Time.realtimeSinceStartup<deadline)yield return null;
            if(!condition())throw new Exception("Timed out: "+label);
        }
        static Status ReadClient()
        {try{return JsonUtility.FromJson<Status>(ReadShared(Path.Combine(output,"client-status.json")));}catch{return null;}}
        static string ReadShared(string path)
        {using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var reader=new StreamReader(file))return reader.ReadToEnd();}
        static bool Both(int hostHP,int clientHP)
        {
            var s=ReadClient();var h=Player(true);var c=Player(false);
            return h!=null&&c!=null&&s!=null&&s.id==command.id&&h.Health==hostHP&&c.Health==clientHP
                &&s.hostHP==hostHP&&s.clientHP==clientHP&&s.hostNativeHP==hostHP&&s.clientNativeHP==clientHP;
        }
        static void Send(Command value)
        {
            command=value;File.WriteAllText(Path.Combine(output,"command.json"),JsonUtility.ToJson(value));
        }
        static IEnumerator Host(SideScrollNetworkSession session)
        {
            session.StartCoroutine(Actor(true,session));
            yield return Until(()=>Player(true)!=null&&Player(false)!=null,"two players",30);
            Send(new Command{id=1});yield return Until(()=>Both(100,100),"initial HP");
            yield return new WaitForSeconds(.5f);
            Send(new Command{id=2,hostShots=1});yield return Until(()=>Both(100,75),"host shot damages client");
            Capture("host-nonfatal",Player(false));
            var wall=new GameObject("Combat verification cover");wall.transform.position=new Vector3(.5f,1.5f,0);
            wall.AddComponent<BoxCollider2D>().size=new Vector2(.12f,3);Physics2D.SyncTransforms();
            Send(new Command{id=3,hostShots=1});yield return new WaitForSeconds(1);
            if(!Both(100,75))throw new Exception("Cover failed to block player damage");
            UnityEngine.Object.Destroy(wall);yield return null;
            Send(new Command{id=4,clientCrouch=true,hostShots=1,targetHeight=2.15f});
            yield return Until(()=>Both(100,75)&&Player(false).GetComponent<CrouchTrial>().IsCrouched,"client crouch replicated");
            yield return new WaitForSeconds(1);
            if(!Both(100,75)||!Player(false).GetComponent<CrouchTrial>().IsCrouched)throw new Exception("Crouch upper hitbox did not shrink");
            Send(new Command{id=5,clientCrouch=true,hostShots=3,targetHeight=1});
            yield return Until(()=>Both(100,0)&&Player(false).GetComponent<CharacterDeathTrial>().IsDead,"crouched client death");
            yield return new WaitForSeconds(1);Capture("host-client-dead",Player(false));
            var s=ReadClient();if(s.localControls||s.clientDead==false||!s.hostDead&&Player(true).IsDead)throw new Exception("Dead client controls/state");
            uint oldLife=Player(false).LifeVersion;int shots=Player(false).ReceivedShots;
            Player(false).GetComponent<CharacterDeathTrial>().Revive();
            yield return new WaitForSeconds(.3f);
            if(!Both(100,0))throw new Exception("Non-owner legacy revive changed another player");
            foreach(var hud in UnityEngine.Object.FindObjectsByType<FrontDownAimPlaytestHud>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(hud.isActiveAndEnabled)throw new Exception("Local trial HUD visible in network scene");
            Send(new Command{id=6,rawClientShot=true,clientShots=2});yield return new WaitForSeconds(1);
            if(!Both(100,0)||Player(false).ReceivedShots!=shots)throw new Exception("Server accepted a dead player's shot");
            Send(new Command{id=7,clientRevive=true});yield return Until(()=>Both(100,100)&&Player(false).LifeVersion>oldLife,"client revive");
            yield return new WaitForSeconds(.4f);s=ReadClient();
            if(!s.localControls||s.remoteControls||Player(false).GetComponent<BoxCollider2D>().size.y<2)throw new Exception("Revive ownership/collider restore");
            Player(false).ReceiveServerDamage(25,oldLife);yield return new WaitForSeconds(.25f);
            if(!Both(100,100))throw new Exception("Old-life impact damaged a revived player");
            Send(new Command{id=8,clientShots=3});yield return Until(()=>Both(25,100),"client shots damage host");
            Send(new Command{id=9,hostJump=true,clientShots=1});
            yield return Until(()=>Both(0,100)&&Player(true).GetComponent<CharacterDeathTrial>().IsDead,"airborne host death");
            float deathY=Player(true).transform.position.y;
            if(deathY<.05f)throw new Exception("Expected death while airborne");
            yield return new WaitForSeconds(1);
            if(Player(true).transform.position.y>=deathY||!Player(true).GetComponent<CharacterDeathTrial>().Grounded)throw new Exception("Dead airborne owner did not fall/land");
            Capture("host-host-dead",Player(true));
            Send(new Command{id=10,reconnect=true});
            yield return Until(()=>ReadClient()!=null&&ReadClient().owner>1&&Both(0,100)&&ReadClient().hostDead&&File.Exists(Path.Combine(output,"capture-10.txt")),"late join dead host",20);
            yield return new WaitForSeconds(.7f);
            Player(true).RequestRevive();yield return Until(()=>Both(100,100)&&!ReadClient().hostDead,"host revive");
            Send(new Command{id=11,hostCrouch=true,clientCrouch=true,hostMove=1,clientMove=-1});
            yield return Until(()=>ReadClient()!=null&&ReadClient().id==11&&ReadClient().hostCrouch&&ReadClient().clientCrouch&&Player(true).GetComponent<CrouchTrial>().MotionAmount>.8f,"motion after revive");
            Capture("host-revived-motion",Player(true));
            Send(new Command{id=12,practice=true,hostShots=3,clientShots=3});
            yield return Until(()=>ReadClient()!=null&&ReadClient().deadTargets==2
                &&Array.TrueForAll(UnityEngine.Object.FindObjectsByType<SideScrollDamageTarget>(FindObjectsSortMode.None),target=>target.Health.Value==0&&target.Hits.Value==3),"practice target regression");
            File.WriteAllText(Path.Combine(output,"host-result.txt"),"PASS: server HP both directions; wall blocks; crouch hitbox; crouched client death; server rejects dead-owner RPC; client revive/ownership restore; stale-life damage ignored; airborne host death/fall/landing; late join dead host; host revive; crouch movement restored");
        }
        static IEnumerator Client(SideScrollNetworkSession session)
        {
            yield return Until(()=>Player(true)!=null&&Player(false)!=null,"client connected",30);
            var own=Player(false);int health=own.Health;
            own.ReceiveServerDamage(25,own.LifeVersion);
            if(own.Health!=health)throw new Exception("Client wrote server HP");
            session.StartCoroutine(Actor(false,session));
            float deadline=Time.realtimeSinceStartup+80,next=0;int captured=-1;
            while(Time.realtimeSinceStartup<deadline)
            {
                var h=Player(true);var c=Player(false);
                if(h!=null&&c!=null&&command!=null&&Time.realtimeSinceStartup>=next)
                {
                    next=Time.realtimeSinceStartup+.05f;
                    var status=new Status {id=command.id,hostHP=h.Health,clientHP=c.Health,
                        hostNativeHP=h.GetComponent<CharacterDeathTrial>().Health,clientNativeHP=c.GetComponent<CharacterDeathTrial>().Health,
                        hostDead=h.GetComponent<CharacterDeathTrial>().IsDead,clientDead=c.GetComponent<CharacterDeathTrial>().IsDead,
                        localControls=c.GetComponent<SideScrollMapPreview>().enabled,remoteControls=h.GetComponent<SideScrollMapPreview>().enabled,
                        owner=c.OwnerClientId,hostLife=h.LifeVersion,clientLife=c.LifeVersion,
                        hostCrouch=h.GetComponent<CrouchTrial>().IsCrouched,clientCrouch=c.GetComponent<CrouchTrial>().IsCrouched};
                    foreach(var target in UnityEngine.Object.FindObjectsByType<SideScrollDamageTarget>(FindObjectsSortMode.None))if(target.Health.Value==0&&target.Hits.Value==3)status.deadTargets++;
                    File.WriteAllText(Path.Combine(output,"client-status.json"),JsonUtility.ToJson(status));
                    bool ready=command.id==5&&status.clientDead||command.id==9&&status.hostDead||command.id==10&&status.hostDead&&status.owner>1||command.id==11&&status.hostCrouch&&status.clientCrouch;
                    if(ready&&captured!=command.id)
                    {captured=command.id;yield return new WaitForSeconds(.8f);Capture("client-stage-"+captured,command.id==5?c:h);File.WriteAllText(Path.Combine(output,"capture-"+captured+".txt"),"PASS");}
                }
                if(File.Exists(Path.Combine(output,"host-result.txt")))
                {
                    string result=File.ReadAllText(Path.Combine(output,"host-result.txt"));
                    File.WriteAllText(Path.Combine(output,"client-result.txt"),result.StartsWith("PASS")?"PASS: server HP immutable on client; both HP and native death/restore replicated; late join dead host; movement after revive":"FAIL: host verification failed");yield break;
                }
                yield return null;
            }
            throw new Exception("Client verification timed out");
        }
        static IEnumerator Actor(bool host,SideScrollNetworkSession session)
        {
            int revision=-1,sent=0;float began=0,nextShot=0;bool jumped=false,revived=false,reconnected=false;
            while(true)
            {
                Command incoming=null;try{incoming=JsonUtility.FromJson<Command>(ReadShared(Path.Combine(output,"command.json")));}catch{}
                if(incoming!=null)command=incoming;
                if(command==null){yield return null;continue;}
                var own=Player(host);var other=Player(!host);
                if(own==null||other==null){yield return null;continue;}
                var move=own.GetComponent<SideScrollMapPreview>();var crouch=own.GetComponent<CrouchTrial>();
                var aim=own.GetComponentInChildren<RightFacingAimRig>();var shot=aim.GetComponent<RightFacingShotPreview>();
                shot.readMouse=false;
                if(revision!=command.id)
                {
                    revision=command.id;began=Time.time;sent=0;nextShot=0;jumped=revived=reconnected=false;
                    if(!own.IsDead){move.SetInput(0);move.SetSpawnPosition(new Vector2(host?-1.5f:1.5f,0));Physics2D.SyncTransforms();}
                }
                if(!own.IsDead)
                {move.SetInput(host?command.hostMove:command.clientMove);crouch.SetRequested(host?command.hostCrouch:command.clientCrouch);
                    aim.SetAimTarget(command.practice?new Vector2(host?-3:3,1.2f):(Vector2)other.transform.position+Vector2.up*command.targetHeight);}
                float t=Time.time-began;
                if(host&&command.hostJump&&t>.3f&&move.Grounded&&!jumped){move.SetInput(0,true);jumped=true;}
                if(!host&&command.clientRevive&&own.IsDead&&!revived){revived=true;own.GetComponent<CharacterDeathTrial>().Revive();}
                if(!host&&command.reconnect&&!reconnected)
                {reconnected=true;NetworkManager.Singleton.Shutdown();yield return new WaitForSecondsRealtime(1);session.StartLocal(false);}
                int count=host?command.hostShots:command.clientShots;
                if(t>(command.hostJump?.48f:.35f)&&sent<count&&Time.time>=nextShot)
                {
                    if(!host&&command.rawClientShot)typeof(SideScrollNetworkPlayer).GetMethod("FireServerRpc",Private).Invoke(own,new object[]{(Vector2)aim.MuzzlePosition,Vector2.left});
                    else shot.Fire();
                    sent++;nextShot=Time.time+.17f;
                }
                yield return null;
            }
        }
        static void Capture(string name,SideScrollNetworkPlayer player)
        {
            var camera=Camera.main;var p=camera.transform.position;var size=camera.orthographicSize;
            var old=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(720,600,24);
            try
            {
                camera.orthographicSize=1.6f;camera.transform.position=new Vector3(player.transform.position.x,player.transform.position.y+1.2f,-10);
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(720,600,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,720,600),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());UnityEngine.Object.Destroy(image);
            }
            finally{camera.targetTexture=old;camera.transform.position=p;camera.orthographicSize=size;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}
        }
    }
}
#endif
