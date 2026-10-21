using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using RightWalkPrototype;

[InitializeOnLoad]
public static class CoverTrialEditorReview
{
    [Serializable] class Request {public string folder;public bool measure=true;}
    [Serializable] class Command {public int id,shooter=1,shots;public bool crouched;public Vector2 target;public bool horizontal,customPositions;public Vector2 hostPosition,clientPosition;public float hostMove;}
    [Serializable] class Status {public int id,number,hp;public bool crouched,grounded;public Vector2 position,muzzle,direction;public float feet,head;}
    const string RequestPath="Assets/RightWalkPrototype/Tests/CoverReviewRequest.json";
    static bool started;static Request request;static Command command;
    static CoverTrialEditorReview(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/Right Walk Prototype/Measure Cover Heights")]
    public static void BeginMeasure(){Begin(true);}
    [MenuItem("Tools/Right Walk Prototype/Verify Solid Cover Trial")]
    public static void BeginTest(){Begin(false);}
    static void Begin(bool measure)
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Requires saved edit mode");
        var r=new Request{folder=Path.GetFullPath("LocalTools/Analysis/NetworkCombat/cover-"+DateTime.UtcNow.ToString("HHmmss")),measure=measure};
        Directory.CreateDirectory(r.folder);File.WriteAllText(RequestPath,JsonUtility.ToJson(r));AssetDatabase.ImportAsset(RequestPath);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying){started=false;return;}
        if(started||EditorApplication.isCompiling||Time.time<.3f||!File.Exists(RequestPath))return;
        started=true;request=JsonUtility.FromJson<Request>(File.ReadAllText(RequestPath));
        var session=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();
        bool main=Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor;
        session.useReadyRoom=false;session.StartLocal(main);session.StartCoroutine(Actor(session));if(main)session.StartCoroutine(Run(session));
    }
    static string Read(string path){using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var r=new StreamReader(f))return r.ReadToEnd();}
    static readonly System.Collections.Generic.Dictionary<int,Status> lastKnown=new System.Collections.Generic.Dictionary<int,Status>();
    static Status Get(int number)
    {
        try{var value=JsonUtility.FromJson<Status>(Read(request.folder+"/p"+number+".json"));if(value!=null&&value.number==number)lastKnown[number]=value;}catch{}
        return lastKnown.TryGetValue(number,out var result)?result:null;
    }
    static void Send(Command c){command=c;File.WriteAllText(request.folder+"/command.json",JsonUtility.ToJson(c));}
    static IEnumerator Actor(SideScrollNetworkSession session)
    {
        int revision=-1,sent=0;float began=0,next=0;
        while(true)
        {
            if(File.Exists(request.folder+"/stop.txt")){session.DisconnectLocal();yield break;}
            Command c=null;try{c=JsonUtility.FromJson<Command>(Read(request.folder+"/command.json"));}catch{}
            SideScrollNetworkPlayer own=null;foreach(var p in UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsOwner)own=p;
            if(c!=null&&c.id>0&&own!=null)
            {
                var move=own.GetComponent<SideScrollMapPreview>();var crouch=own.GetComponent<CrouchTrial>();var aim=own.GetComponentInChildren<RightFacingAimRig>();var shot=aim.GetComponent<RightFacingShotPreview>();
                shot.readMouse=false;move.SetInput(0);
                if(revision!=c.id)
                {
                    revision=c.id;sent=0;began=Time.time;next=0;
                    float x=own.PlayerNumber==1?(request.measure?-7:-10):own.PlayerNumber==2?-1.5f:own.PlayerNumber==3?18:-18;
                    move.SetSpawnPosition(c.customPositions&&own.PlayerNumber<=2?(own.PlayerNumber==1?c.hostPosition:c.clientPosition):new Vector2(x,.03f));
                }
                crouch.SetRequested(own.PlayerNumber==(request.measure?1:(c.shooter==1?2:1))&&c.crouched);
                if(own.PlayerNumber==1&&c.hostMove!=0)move.SetInput(c.hostMove);
                if(c.horizontal)aim.SetAimDirection(Vector2.right);else if(own.PlayerNumber==c.shooter)aim.SetAimTarget(c.target);else aim.SetAimDirection(c.shooter==1?Vector2.left:Vector2.right);
                if(own.PlayerNumber==c.shooter&&Time.time-began>.8f&&sent<c.shots&&Time.time>next){shot.Fire();sent++;next=Time.time+.25f;}
                var b=own.GetComponent<BoxCollider2D>().bounds;
                File.WriteAllText(request.folder+"/p"+own.PlayerNumber+".json",JsonUtility.ToJson(new Status{id=c.id,number=own.PlayerNumber,hp=own.Health,crouched=crouch.IsCrouched,grounded=move.Grounded,position=own.transform.position,muzzle=aim.MuzzlePosition,direction=aim.AimDirection,feet=b.min.y,head=b.max.y}));
            }
            yield return null;
        }
    }
    static IEnumerator Wait(Func<bool> ready,string label,float seconds=15)
    {float end=Time.realtimeSinceStartup+seconds;while(!ready()&&Time.realtimeSinceStartup<end)yield return null;if(!ready())throw new Exception(label);}
    static IEnumerator Run(SideScrollNetworkSession session)
    {
        var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(Check());string result="PASS";
        while(stack.Count>0)
        {
            bool more=false;object next=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){result="FAIL: "+failure;break;}
            if(!more){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        File.WriteAllText(request.folder+"/result.txt",result);
        File.WriteAllText(request.folder+"/stop.txt","stop");yield return new WaitForSecondsRealtime(1);session.DisconnectLocal();yield return new WaitForSecondsRealtime(.5f);EditorApplication.isPlaying=false;
    }
    static IEnumerator Check()
    {
        yield return Wait(()=>UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None).Length>=(request.measure?1:4),"peers connected",45);
        int measured=request.measure?1:2;
        Send(new Command{id=1,horizontal=true});yield return Wait(()=>Get(measured)!=null&&Get(measured).id==1&&Get(measured).grounded,"standing ready");yield return new WaitForSeconds(.6f);
        var standing=Get(measured);
        Send(new Command{id=2,horizontal=true,crouched=true});yield return Wait(()=>Get(measured)!=null&&Get(measured).id==2&&Get(measured).crouched&&Get(measured).grounded,"crouch ready");yield return new WaitForSeconds(.6f);
        File.WriteAllText(request.folder+"/heights.json","{\"standing\":"+JsonUtility.ToJson(standing)+",\"crouching\":"+JsonUtility.ToJson(Get(measured))+"}");
        if(request.measure)yield break;
        var cover=GameObject.Find("Ground west cover").GetComponent<BoxCollider2D>();
        if(cover.usedByEffector||Mathf.Abs(cover.bounds.max.y-1.8f)>.02f)throw new Exception("Solid cover trial configuration");
        Send(new Command{id=3,crouched=true,shots=1,target=new Vector2(-1.5f,1.2f)});
        yield return Wait(()=>Get(2)!=null&&Get(2).id==3&&Get(2).crouched,"hidden victim ready");
        yield return new WaitForSeconds(1.7f);
        if(Get(2).hp!=100)throw new Exception("Hidden victim took damage");
        Capture("crouched-protected");
        Send(new Command{id=4,shots=1,target=new Vector2(-1.5f,2.08f)});
        yield return Wait(()=>Get(2)!=null&&Get(2).id==4&&Get(2).hp==75,"standing exposed victim hit");
        Capture("standing-exposed");
        Send(new Command{id=5,crouched=true,shots=1,target=new Vector2(-1.5f,2.08f)});
        yield return Wait(()=>Get(2)!=null&&Get(2).id==5&&Get(2).crouched,"return to crouch");yield return new WaitForSeconds(1.7f);
        if(Get(2).hp!=75)throw new Exception("Shot over cover hit crouched victim");
        Send(new Command{id=6,shooter=2,crouched=true,shots=1,target=new Vector2(-5.6f,1.2f),customPositions=true,hostPosition=new Vector2(-5.6f,.03f),clientPosition=new Vector2(3,.03f)});
        yield return Wait(()=>Get(1)!=null&&Get(1).id==6&&Get(1).crouched,"reverse hidden host ready");yield return new WaitForSeconds(1.7f);
        if(Get(1).hp!=100)throw new Exception("Client shot hit hidden host");
        Send(new Command{id=7,shooter=2,shots=1,target=new Vector2(-5.6f,2.08f),customPositions=true,hostPosition=new Vector2(-5.6f,.03f),clientPosition=new Vector2(3,.03f)});
        yield return Wait(()=>Get(1)!=null&&Get(1).id==7&&Get(1).hp==75,"client hits exposed host over cover");
        Send(new Command{id=8,customPositions=true,hostPosition=new Vector2(-5.6f,.03f),clientPosition=new Vector2(-1.5f,.03f),hostMove=1,target=new Vector2(0,2)});
        yield return new WaitForSeconds(2);
        if(Get(1).position.x>-4.4f||Get(1).position.x<-5.4f)throw new Exception("Solid cover did not block walking");
        File.WriteAllText(request.folder+"/combat.txt","PASS: crouch fully protected, standing upper body exposed, host/client real shots both directions, body blocked by cover");
    }
    static void Capture(string name)
    {
        var camera=Camera.main;var pos=camera.transform.position;float size=camera.orthographicSize;
        var previous=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1400,700,24);
        camera.transform.position=new Vector3(-4.5f,1.5f,-10);camera.orthographicSize=3.5f;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var tex=new Texture2D(1400,700,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1400,700),0,0);tex.Apply();File.WriteAllBytes(request.folder+"/"+name+".png",tex.EncodeToPNG());
        camera.targetTexture=previous;camera.transform.position=pos;camera.orthographicSize=size;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(tex);
    }

}
