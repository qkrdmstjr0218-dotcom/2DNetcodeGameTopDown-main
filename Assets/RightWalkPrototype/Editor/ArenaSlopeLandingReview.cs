using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;

[InitializeOnLoad]
public static class ArenaSlopeLandingReview
{
    const string Key="ArenaSlopeLandingReview",Folder="LocalTools/Analysis/SlopeLanding";
    static readonly List<string> results=new();
    static ArenaSlopeLandingReview(){EditorApplication.update+=Tick;}
    public static void Begin(bool baseline=false){Directory.CreateDirectory(Folder);SessionState.SetBool(Key+"baseline",baseline);SessionState.SetBool(Key,true);File.WriteAllText(Folder+"/result.txt","RUNNING");EditorApplication.isPlaying=true;}
    static void Tick()
    {if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||Time.time<.5f)return;var s=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();if(s==null)return;SessionState.SetBool(Key,false);results.Clear();s.StartCoroutine(Guard(Run(s)));}
    static IEnumerator Guard(IEnumerator run)
    {
        var stack=new Stack<IEnumerator>();stack.Push(run);
        while(stack.Count>0){bool next=false;object value=null;Exception error=null;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){File.WriteAllText(Folder+"/result.txt","ERROR "+error+"\n"+string.Join("\n",results));yield break;}if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}
        File.WriteAllText(Folder+"/result.txt",(results.Exists(x=>x.StartsWith("FAIL"))?"FAIL":"PASS")+"\n"+string.Join("\n",results));
    }
    static IEnumerator Wait(Func<bool> test,string label,float seconds=25){float until=Time.time+seconds;while(!test()&&Time.time<until)yield return null;if(!test())throw new Exception(label);}
    static float Surface(BoxCollider2D c,float x){var a=c.transform.TransformPoint(c.offset+new Vector2(-c.size.x/2,c.size.y/2));var b=c.transform.TransformPoint(c.offset+c.size/2);return Mathf.LerpUnclamped(a.y,b.y,(x-a.x)/(b.x-a.x));}
    static IEnumerator Run(SideScrollNetworkSession session)
    {
        session.StartSoloPractice();yield return Wait(()=>ArenaObjectiveMatch.PracticeRoom,"host");var match=ArenaObjectiveMatch.Instance;match.SetLocalReady(true);yield return Wait(()=>match.CanStart,"ready");if(!match.StartRound())throw new Exception("start");yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing,"playing");
        for(int i=0;i<match.Patrols.Count;i++){var e=match.Patrols[i];e.Health=0;e.RespawnAt=match.Now+600;match.Patrols[i]=e;}match.TurretShots.Clear();var transit=match.MedicalTransit.Value;transit.NextAt=match.Now+600;match.MedicalTransit.Value=transit;
        var p=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>();var move=p.GetComponent<SideScrollMapPreview>();var body=p.GetComponent<Rigidbody2D>();var shape=p.GetComponent<BoxCollider2D>();var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();
        bool baseline=SessionState.GetBool(Key+"baseline",false);
        foreach(int side in new[]{-1,1})foreach(string prefix in new[]{"Inner lower ","Crown access "})foreach(int input in new[]{0,side,-side})foreach(bool fast in baseline?new[]{false}:new[]{false,true})
        {
            var ramp=map.transform.Find(prefix+side).GetComponent<BoxCollider2D>();float x=prefix.StartsWith("Inner")?side*13.2f:side*16;
            move.SetInput(0);move.SetSpawnPosition(new Vector2(x,Surface(ramp,x)+(fast?.5f:prefix.StartsWith("Inner")?1.2f:2.5f)));move.SetInput(input);if(fast)body.linearVelocity=new Vector2(input*move.speed,-10);
            bool touched=false;float end=Time.time+2.5f,first=0,maxV=0,afterV=0;float landedX=0,lastX=body.position.x,maxStep=0;
            var trace=new List<string>{"time,x,y,vx,vy,touching,grounded"};
            while(Time.time<end)
            {
                yield return new WaitForFixedUpdate();bool contact=shape.IsTouching(ramp);float vx=Mathf.Abs(body.linearVelocity.x);
                trace.Add(Time.time+","+body.position.x+","+body.position.y+","+body.linearVelocity.x+","+body.linearVelocity.y+","+contact+","+move.Grounded);
                if(contact&&!touched){touched=true;first=Time.time;landedX=body.position.x;}
                if(touched){maxV=Mathf.Max(maxV,vx);maxStep=Mathf.Max(maxStep,Mathf.Abs(body.position.x-lastX));if(Time.time-first>.15f)afterV=Mathf.Max(afterV,vx);if(Time.time-first>.45f)break;}
                lastX=body.position.x;
            }
            float limit=input==0?.15f:move.speed+.35f;bool okay=touched&&maxV<=limit;
            string name=prefix.Trim()+"-"+side+"-input"+input+(fast?"-fast":"");
            results.Add((baseline?"BASELINE ":okay?"PASS ":"FAIL ")+name+" touched="+touched+" maxV="+maxV.ToString("F3")+" afterV="+afterV.ToString("F3")+" step="+maxStep.ToString("F3")+" drift="+Mathf.Abs(body.position.x-landedX).ToString("F3"));
            File.WriteAllLines(Folder+"/"+(baseline?"before-":"after-")+name+".csv",trace);
        }
        session.DisconnectLocal();yield return null;
    }
}
