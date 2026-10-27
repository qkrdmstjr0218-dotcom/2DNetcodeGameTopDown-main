#if UNITY_EDITOR || ARENA_BUILD_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Netcode;
namespace RightWalkPrototype
{
    // Only included in explicitly instrumented verification builds, never normal players.
    public sealed class ArenaBuildPolishReview:MonoBehaviour
    {
        string folder;bool standalone;int oldFps,errorCount;readonly List<string> log=new(),trace=new();
        void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errorCount++;File.AppendAllText(folder+"/runtime-errors.txt",message+"\n"+stack+"\n");}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();int flag=Array.IndexOf(args,"--arena-build-review");
            if(flag<0||Application.isEditor)return;
            string output=flag+1<args.Length?args[flag+1]:Path.Combine(Application.persistentDataPath,"BuildReview");
            StartReview(output,true);
        }
        public static void StartReview(string output,bool player=false)
        {var review=new GameObject("Temporary build polish review").AddComponent<ArenaBuildPolishReview>();review.folder=output;review.standalone=player;Directory.CreateDirectory(output);File.WriteAllText(output+"/result.txt","RUNNING");Application.logMessageReceived+=review.OnLog;review.oldFps=Application.targetFrameRate;Application.targetFrameRate=30;review.StartCoroutine(review.Guard(review.Run()));}
        void Check(bool condition,string label){log.Add((condition?"PASS ":"FAIL ")+label);if(!condition)throw new Exception(label);}
        IEnumerator Frames(int n=3){for(int i=0;i<n;i++)yield return null;}
        IEnumerator Wait(Func<bool> test,string name,float seconds=15){float end=Time.realtimeSinceStartup+seconds;while(!test()&&Time.realtimeSinceStartup<end)yield return null;if(!test())throw new Exception("Timeout: "+name);}
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();Texture2D shot;
            if(standalone)
            {
                // A hidden Windows swapchain can yield a black ScreenCapture.
                // Render the actual game camera offscreen; this excludes IMGUI.
                var camera=Camera.main;var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
                var target=new RenderTexture(1366,768,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                shot=new Texture2D(1366,768,TextureFormat.RGB24,false);shot.ReadPixels(new Rect(0,0,1366,768),0,0);shot.Apply();
                camera.targetTexture=previousTarget;RenderTexture.active=previousActive;Destroy(target);
                int visible=0;foreach(var pixel in shot.GetPixels32())if(pixel.r+pixel.g+pixel.b>30)visible++;
                Check(visible>shot.width*shot.height/20,"offscreen game camera is nonblack: "+name);
            }
            else shot=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(folder+"/"+name+".png",shot.EncodeToPNG());Destroy(shot);
        }
        IEnumerator Guard(IEnumerator run)
        {
            var stack=new Stack<IEnumerator>();stack.Push(run);Exception failure=null;
            while(stack.Count>0)
            {
                bool next=false;object value=null;
                try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null)break;if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;
            }
            File.WriteAllText(folder+"/result.txt",(failure==null?"PASS":"ERROR "+failure)+"\n"+string.Join("\n",log));
            File.WriteAllLines(folder+"/ascent.csv",trace);Application.targetFrameRate=oldFps;Application.logMessageReceived-=OnLog;
            var session=FindFirstObjectByType<SideScrollNetworkSession>();if(session!=null)session.DisconnectLocal();
            if(standalone)Application.Quit(failure==null?0:1);
#if UNITY_EDITOR
            else UnityEditor.EditorApplication.isPlaying=false;
#endif
        }
        IEnumerator Jump(SideScrollMapPreview move,Rigidbody2D body,float target,float targetX,int side)
        {
            float start=body.position.y;int maxJumps=0;move.SetInput(Mathf.Abs(targetX-body.position.x)<.15f?0:Mathf.Sign(targetX-body.position.x),true);yield return new WaitForFixedUpdate();yield return new WaitForSeconds(.1f);
            float deadline=Time.time+2.5f;
            while(Time.time<deadline)
            {
                maxJumps=Mathf.Max(maxJumps,move.JumpCount);
                move.SetInput(Mathf.Abs(body.position.x-targetX)<.15f?0:Mathf.Sign(targetX-body.position.x));
                trace.Add(side+","+target+","+Time.time+","+body.position.x+","+body.position.y+","+body.linearVelocity.y+","+move.JumpCount+","+(move.GroundCollider!=null?move.GroundCollider.name:"air"));
                if(move.Grounded)break;yield return new WaitForFixedUpdate();
            }
            move.SetInput(0);Check(move.Grounded&&move.GroundCollider!=null&&Mathf.Abs(move.GroundCollider.bounds.max.y-target)<.05f&&maxJumps==1,"single jump "+side+" to "+target+" from root "+start);
            yield return new WaitForSeconds(.12f);
        }
        IEnumerator Run()
        {
            // NGO's generated serializer initializers also run AfterSceneLoad.
            // Let every initializer and scene Start register prefabs before hosting.
            yield return Frames();
            var arena=FindFirstObjectByType<RelayArenaPrototype>();
            Check(arena!=null&&arena.readyRoomFont!=null&&arena.readyRoomFont.HasCharacter('준')&&arena.readyRoomFont.HasCharacter('경'),"Korean font included");
            foreach(var shader in new[]{arena.ammoCursorShader,arena.shieldEffectShader,Resources.Load<Shader>("FoundryArt/HackingField"),Resources.Load<Shader>("FoundryArt/EnemySprite"),Shader.Find("Sprites/Default")})Check(shader!=null&&shader.isSupported,"supported shader "+(shader!=null?shader.name:"missing"));
            Check(Resources.Load<Texture2D>("FoundryArt/SupportMarker")!=null,"support marker resource included");
            int clips=0;foreach(ArenaAudio.Cue cue in Enum.GetValues(typeof(ArenaAudio.Cue)))
            {
                var clip=Resources.Load<AudioClip>("ArenaAudio/"+cue);Check(clip!=null,"audio resource "+cue);var pcm=new float[clip.samples*clip.channels];Check(clip.GetData(pcm,0),"audio PCM readable "+cue);clips++;
            }
            Check(clips==28,"all 28 cues included");
            var session=FindFirstObjectByType<SideScrollNetworkSession>();session.StartSoloPractice();yield return Wait(()=>ArenaObjectiveMatch.PracticeRoom,"solo host");
            var match=ArenaObjectiveMatch.Instance;match.SetLocalReady(true);yield return Wait(()=>match.CanStart,"ready");Check(match.StartRound(),"start round");yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing,"playing",25);
            var player=NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<SideScrollNetworkPlayer>();var move=player.GetComponent<SideScrollMapPreview>();var body=player.GetComponent<Rigidbody2D>();
            Check(FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"one audio listener");
            move.readKeyboard=false;move.SetInput(0);var shots=player.GetComponentInChildren<RightFacingShotPreview>();shots.readMouse=false;var aim=player.GetComponentInChildren<RightFacingAimRig>();aim.mouseAim=false;
            for(int i=0;i<match.Patrols.Count;i++){var s=match.Patrols[i];s.Health=0;s.RespawnAt=match.Now+600;match.Patrols[i]=s;}match.TurretShots.Clear();
            var transit=match.MedicalTransit.Value;transit.NextAt=match.Now+600;match.MedicalTransit.Value=transit;
            trace.Add("side,target,time,x,rootY,vy,jumps,support");
            foreach(int side in new[]{-1,1})
            {
                move.SetSpawnPosition(new Vector2(side*3.8f,13.64f));Physics2D.SyncTransforms();yield return new WaitForSeconds(.4f);Check(move.Grounded,"central upper ascent start "+side);
                float[] tops={15.3f,17f,18.7f,20.4f},xs={3.8f,2.1f,1.2f,1.2f};
                for(int rung=0;rung<4;rung++)yield return Jump(move,body,tops[rung],side*xs[rung],side);
                yield return Capture("third-floor-"+side);
                var crouch=player.GetComponent<CrouchTrial>();crouch.readKeyboard=false;crouch.SetRequested(true);yield return new WaitForSeconds(.18f);move.SetInput(0,true);yield return new WaitForFixedUpdate();move.SetInput(0);yield return new WaitForSeconds(.1f);crouch.SetRequested(false);
                yield return Wait(()=>move.Grounded,"drop to new rung",5);Check(move.GroundCollider.bounds.max.y<20.3f&&move.GroundCollider.bounds.max.y>=17,"deliberate drop lands on central lower rung "+side);
            }
            move.SetSpawnPosition(new Vector2(5.8f,13.64f));Physics2D.SyncTransforms();yield return new WaitForSeconds(.4f);move.SetInput(-1);yield return new WaitForSeconds(.3f);move.SetInput(0);Check(move.Grounded&&body.position.y<13.9f,"walk under central one-way rung without auto-mount");
            player.SetSpecialHeld(false);aim.SetAimTarget(player.transform.position+new Vector3(-5,1.3f,0));yield return new WaitForSeconds(.3f);int before=player.ReceivedShots;shots.Fire();yield return Frames();Check(player.ReceivedShots==before+1,"actual normal shot accepted with cyan tracer");yield return Capture("cyan-normal");
            var audio=ArenaAudio.Instance;bool audible=Application.isFocused&&ArenaAudio.EffectsVolume>0;
            foreach(var outcome in new[]{ArenaAudio.Cue.Victory,ArenaAudio.Cue.SharedVictory,ArenaAudio.Cue.DrawResult,ArenaAudio.Cue.Defeat})
            {
                var state=match.State.Value;state.End=match.Now+100;state.Phase=ArenaObjectiveMatch.Stage.Playing;match.State.Value=state;yield return Frames();
                match.Scores.Clear();match.Scores.Add(new ArenaObjectiveMatch.Score{Client=player.OwnerClientId,Number=1,Seconds=outcome==ArenaAudio.Cue.DrawResult?0:outcome==ArenaAudio.Cue.Defeat?5:10});
                if(outcome==ArenaAudio.Cue.SharedVictory||outcome==ArenaAudio.Cue.Defeat)match.Scores.Add(new ArenaObjectiveMatch.Score{Client=999,Number=2,Seconds=10});
                Check(ArenaAudio.ResultCue(match,player.OwnerClientId)==outcome,"result cue selection "+outcome);
                int endBefore=audio.PlayedCount(ArenaAudio.Cue.RoundEnd),cueBefore=audio.PlayedCount(outcome);state.Phase=ArenaObjectiveMatch.Stage.Ending;state.Version++;state.End=match.Now+ArenaObjectiveMatch.EndAnnouncementSeconds;match.State.Value=state;match.Result.Value=new Unity.Collections.FixedString128Bytes(outcome.ToString());
                yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Finished,"result presentation");yield return new WaitForSeconds(.2f);
                File.AppendAllText(folder+"/audio-timing.txt",outcome+" end="+endBefore+"->"+audio.PlayedCount(ArenaAudio.Cue.RoundEnd)+" outcome="+cueBefore+"->"+audio.PlayedCount(outcome)+" rejection="+audio.LastRejection+"\n");
                if(audible&&Application.isFocused)Check(audio.PlayedCount(ArenaAudio.Cue.RoundEnd)==endBefore+1&&audio.PlayedCount(outcome)==cueBefore+1,"common end and outcome sound once "+outcome);
                else log.Add("NOTE output-device/focus playback not asserted for "+outcome);
                yield return Capture("result-"+outcome);
            }
            Check(errorCount==0,"no runtime errors or exceptions during review");
        }
    }
}
#endif
