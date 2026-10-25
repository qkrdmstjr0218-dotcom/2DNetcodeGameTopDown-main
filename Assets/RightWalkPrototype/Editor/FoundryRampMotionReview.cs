using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;

[InitializeOnLoad]
public static partial class FoundryRampMotionReview
{
    const string Key="FoundryRampMotionReview";
    static string Folder=>SessionState.GetBool(Key+"upper",false)?"LocalTools/Analysis/UpperRoutes":SessionState.GetBool(Key+"layout",false)?"LocalTools/Analysis/FoundryLayout":"LocalTools/Analysis/RampMotion";
    static readonly List<string> results=new();
    static SideScrollNetworkPlayer player;static SideScrollMapPreview move;static Rigidbody2D body;
    static FoundryRampMotionReview(){EditorApplication.update+=Tick;}
    public static void Begin(bool baseline=false,bool focused=false,bool regressionOnly=false,bool layout=false,bool upper=false)
    {SessionState.SetBool(Key+"upper",upper);SessionState.SetBool(Key+"layout",layout);Directory.CreateDirectory(Folder);SessionState.SetBool(Key+"regression",regressionOnly);SessionState.SetBool(Key+"focused",focused);SessionState.SetBool(Key+"baseline",baseline);SessionState.SetBool(Key,true);File.WriteAllText(Folder+"/result.txt","RUNNING");EditorApplication.isPlaying=true;}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||Time.time<.5f)return;
        var s=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();if(s==null)return;
        SessionState.SetBool(Key,false);results.Clear();s.StartCoroutine(Guard(SessionState.GetBool(Key+"upper",false)?RunUpper(s):SessionState.GetBool(Key+"layout",false)?RunLayout(s):Run(s)));
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
    static IEnumerator Wait(Func<bool> test,string label,float seconds=10)
    {float end=Time.time+seconds;while(!test()&&Time.time<end)yield return null;if(!test())throw new Exception(label);}
    static Vector2 Surface(BoxCollider2D c,float x)
    {var a=c.transform.TransformPoint(c.offset+new Vector2(-c.size.x*.5f,c.size.y*.5f));var b=c.transform.TransformPoint(c.offset+c.size*.5f);return new Vector2(x,Mathf.LerpUnclamped(a.y,b.y,(x-a.x)/(b.x-a.x)));}
    static IEnumerator Cross(BoxCollider2D ramp,bool top,bool outward,bool sprint)
    {
        var a=(Vector2)ramp.transform.TransformPoint(ramp.offset+new Vector2(-ramp.size.x*.5f,ramp.size.y*.5f));
        var b=(Vector2)ramp.transform.TransformPoint(ramp.offset+ramp.size*.5f);
        var edge=top?(a.y>b.y?a:b):(a.y<b.y?a:b);float outSign=Mathf.Sign(edge.x-(a.x+b.x)*.5f);
        float direction=outward?outSign:-outSign;float approach=outward&&top&&ramp.usedByEffector?4f:outward?1.1f:.6f;float startX=edge.x-direction*approach;
        bool startsOnRamp=outward;var at=startsOnRamp?Surface(ramp,startX):new Vector2(startX,edge.y);
        string name=ramp.name+" "+(top?"top":"bottom")+" "+(outward?"out":"in")+" "+(sprint?"run":"walk");
        typeof(SideScrollNetworkPlayer).GetMethod("ResetSprintEnergy",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(player,null);
        move.SetInput(0);move.SetSpawnPosition(at+Vector2.up*.4f);yield return new WaitForSeconds(.4f);
        // A low one-way entrance must remain walk-through. Only enter its top from above.
        if(!move.Grounded||startsOnRamp&&move.GroundCollider!=ramp){results.Add("FAIL setup "+name+" "+body.position+" support="+(move.GroundCollider!=null?move.GroundCollider.name:"none"));yield break;}
        move.SetInput(direction,false,sprint);float end=Time.time+3.5f,stall=0,maxStall=0,minSpeed=100,air=0,maxAir=0;bool crossed=false;
        var trace=new List<string>{"time,x,y,vx,vy,ground,support"};
        while(Time.time<end)
        {
            yield return new WaitForFixedUpdate();float x=body.position.x;
            trace.Add(Time.time+","+x+","+body.position.y+","+body.linearVelocity.x+","+body.linearVelocity.y+","+move.Grounded+","+(move.GroundCollider!=null?move.GroundCollider.name:"none"));
            if(Mathf.Abs(x-edge.x)<.5f){float speed=Mathf.Abs(body.linearVelocity.x);minSpeed=Mathf.Min(minSpeed,speed);stall=speed<.4f?stall+Time.fixedDeltaTime:0;maxStall=Mathf.Max(maxStall,stall);}
            if(top&&outward&&outSign*(x-edge.x)>0){air=move.Grounded?0:air+Time.fixedDeltaTime;maxAir=Mathf.Max(maxAir,air);}
            if(direction*(x-edge.x)>.55f){crossed=true;break;}
        }
        move.SetInput(0);
        bool okay=crossed&&maxStall<.12f&&maxAir<.12f;
        results.Add((okay?"PASS ":"FAIL ")+name+" crossed="+crossed+" stall="+maxStall.ToString("F3")+" air="+maxAir.ToString("F3")+" minV="+minSpeed.ToString("F2")+" at="+body.position);
        File.WriteAllLines(Folder+"/"+name.Replace(' ','_')+".csv",trace);
        File.WriteAllLines(Folder+"/progress.txt",results);
    }
    static IEnumerator Run(SideScrollNetworkSession session)
    {
        session.StartSoloPractice();yield return Wait(()=>ArenaObjectiveMatch.PracticeRoom,"host");var match=ArenaObjectiveMatch.Instance;
        match.SetLocalReady(true);yield return Wait(()=>match.CanStart,"ready");if(!match.StartRound())throw new Exception("start");
        yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing,"playing",22);
        player=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>();move=player.GetComponent<SideScrollMapPreview>();body=player.GetComponent<Rigidbody2D>();
        player.GetComponentInChildren<RightFacingShotPreview>().readMouse=false;player.GetComponentInChildren<RightFacingAimRig>().mouseAim=false;
        for(int i=0;i<match.Patrols.Count;i++){var e=match.Patrols[i];e.Health=0;e.RespawnAt=match.Now+600;match.Patrols[i]=e;}
        match.TurretShots.Clear();var window=match.MedicalTransit.Value;window.NextAt=match.Now+600;match.MedicalTransit.Value=window;
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();
        foreach(var ramp in map.GetComponentsInChildren<BoxCollider2D>())
        {
            if(SessionState.GetBool(Key+"regression",false))break;
            if(Mathf.Abs(ramp.transform.up.x)<.01f||ramp.isTrigger)continue;
            if(SessionState.GetBool(Key+"focused",false)&&!ramp.name.StartsWith("Inner lower ")&&!ramp.name.StartsWith("Outer balcony access "))continue;
            // All high ends, in both directions/speeds; solid ramp low ends too.
            foreach(bool top in ramp.usedByEffector?new[]{true}:new[]{true,false})
            foreach(bool outward in SessionState.GetBool(Key+"baseline",false)?new[]{true}:new[]{true,false})
            foreach(bool sprint in SessionState.GetBool(Key+"baseline",false)?new[]{false}:new[]{false,true})
                yield return Cross(ramp,top,outward,sprint);
        }
        // Keep jump-through route selection and deliberate drop behaviour intact.
        move.SetInput(0);move.SetSpawnPosition(new Vector2(16,.04f));yield return new WaitForSeconds(.3f);
        move.SetInput(-1);yield return Wait(()=>body.position.x<13.5f,"walk below ramp");move.SetInput(0);
        results.Add((move.Grounded&&Mathf.Abs(body.position.y)<.2f?"PASS ":"FAIL ")+"walking below one-way ramp does not auto-mount");
        float crown=3*map.floorSpacing;move.SetSpawnPosition(new Vector2(-6,crown+.04f));yield return new WaitForSeconds(.3f);
        move.SetInput(0,true);yield return new WaitForSeconds(.2f);
        results.Add((!move.Grounded&&body.position.y>crown+.6f?"PASS ":"FAIL ")+"jump remains free of seam snap");
        move.SetSpawnPosition(new Vector2(1,crown+.04f));yield return new WaitForSeconds(.3f);
        player.GetComponent<CrouchTrial>().SetRequested(true);yield return new WaitForSeconds(.2f);move.SetInput(0,true);yield return new WaitForSeconds(.15f);player.GetComponent<CrouchTrial>().SetRequested(false);
        yield return Wait(()=>move.Grounded&&Mathf.Abs(body.position.y-2*map.floorSpacing)<.25f,"drop to next floor");results.Add("PASS deliberate 6.8 floor drop");
        session.DisconnectLocal();yield return new WaitForSeconds(.3f);
    }
}
