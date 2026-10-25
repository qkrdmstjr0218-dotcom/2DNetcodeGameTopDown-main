using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
[InitializeOnLoad]
public static class ArenaAudioReview
{
    const string Key="ArenaAudioReview",Folder="LocalTools/Analysis/Audio";
    static readonly List<string> results=new();
    static ArenaAudioReview(){EditorApplication.update+=Tick;}
    public static void ImportClips()
    {
        foreach(var guid in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/RightWalkPrototype/Resources/ArenaAudio"}))
        {
            var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));var s=importer.defaultSampleSettings;
            s.loadType=AudioClipLoadType.DecompressOnLoad;s.compressionFormat=AudioCompressionFormat.PCM;s.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;s.preloadAudioData=true;
            importer.defaultSampleSettings=s;importer.forceToMono=true;importer.loadInBackground=false;importer.SaveAndReimport();
        }
    }
    public static void Begin(){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    static void Tick(){if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||Time.time<.5f)return;var s=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();if(s==null)return;SessionState.SetBool(Key,false);results.Clear();s.StartCoroutine(Guard(Run(s)));}
    static IEnumerator Guard(IEnumerator run)
    {
        var stack=new Stack<IEnumerator>();stack.Push(run);
        while(stack.Count>0){bool next=false;object value=null;Exception error=null;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){File.WriteAllText(Folder+"/result.txt","ERROR "+error+"\n"+string.Join("\n",results));yield break;}if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}
        File.WriteAllText(Folder+"/result.txt",(results.Exists(x=>x.StartsWith("FAIL"))?"FAIL":"PASS")+"\n"+string.Join("\n",results));
    }
    static void Check(bool okay,string label){results.Add((okay?"PASS ":"FAIL ")+label);}
    static IEnumerator Wait(Func<bool> test,string label,float seconds=20){float end=Time.time+seconds;while(!test()&&Time.time<end)yield return null;if(!test())throw new Exception(label);}
    static IEnumerator Frames(int n=3){for(int i=0;i<n;i++)yield return null;}
    static IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"/"+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
    static IEnumerator Run(SideScrollNetworkSession session)
    {
        var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.Focus();yield return Frames();
        var size=view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);int oldSize=(int)size.GetValue(view);
        view.GetType().GetMethod("SetCustomResolution",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,new object[]{new Vector2(1366,768),"Audio and keycaps 1366"});
        foreach(ArenaAudio.Cue cue in Enum.GetValues(typeof(ArenaAudio.Cue)))
        {
            var clip=Resources.Load<AudioClip>("ArenaAudio/"+cue);if(clip==null)throw new Exception("missing "+cue);
            var pcm=new float[clip.samples*clip.channels];bool read=clip.GetData(pcm,0);float peak=0;double power=0;foreach(float value in pcm){peak=Mathf.Max(peak,Mathf.Abs(value));power+=value*value;}
            Check(read&&clip.channels==1&&peak<.425f&&power/pcm.Length>.000001,"PCM clip valid and bounded: "+cue);
        }
        var audio=ArenaAudio.Instance;bool savedKey=PlayerPrefs.HasKey("RightWalk.SfxVolume");float savedEffects=ArenaAudio.EffectsVolume;ArenaAudio.SetEffectsVolume(.65f);
        Check(audio!=null&&audio.GetComponents<AudioSource>().Length==16,"fixed pool has twelve world and four UI voices");
        session.StartSoloPractice();yield return Wait(()=>ArenaObjectiveMatch.PracticeRoom,"host");var match=ArenaObjectiveMatch.Instance;match.SetLocalReady(true);yield return Wait(()=>match.CanStart,"ready");if(!match.StartRound())throw new Exception("start");yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing,"playing");yield return Frames();
        Check(audio.PlayedCount(ArenaAudio.Cue.Countdown)>0&&audio.PlayedCount(ArenaAudio.Cue.RoundStart)==1,"countdown and round start observed once");
        for(int i=0;i<match.Patrols.Count;i++){var s=match.Patrols[i];s.Health=0;s.RespawnAt=match.Now+600;match.Patrols[i]=s;}match.TurretShots.Clear();var transit=match.MedicalTransit.Value;transit.NextAt=match.Now+600;match.MedicalTransit.Value=transit;
        var player=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>();var move=player.GetComponent<SideScrollMapPreview>();move.readKeyboard=false;move.SetInput(0);move.SetSpawnPosition(new Vector2(-.7f,6.84f));
        var shots=player.GetComponentInChildren<RightFacingShotPreview>();shots.readMouse=false;var aim=player.GetComponentInChildren<RightFacingAimRig>();aim.SetAimTarget(new Vector2(5,7.5f));yield return new WaitForSeconds(.9f);
        int normal=audio.PlayedCount(ArenaAudio.Cue.Normal),received=player.ReceivedShots;shots.Fire();yield return Frames();
        Check(player.ReceivedShots==received+1&&audio.PlayedCount(ArenaAudio.Cue.Normal)==normal+1,"real normal fire emits exactly one fire sound");
        foreach(var pair in new[]{(SideScrollNetworkPlayer.AmmoKind.InstantShotgun,ArenaAudio.Cue.Shotgun),(SideScrollNetworkPlayer.AmmoKind.Rifle,ArenaAudio.Cue.Rifle),(SideScrollNetworkPlayer.AmmoKind.Grenade,ArenaAudio.Cue.GrenadeLaunch)})
        {
            player.SelectAmmo(pair.Item1);player.SetSpecialHeld(true);yield return new WaitForSeconds(1.1f);
            int before=audio.PlayedCount(pair.Item2);received=player.ReceivedShots;shots.Fire();yield return Frames();
            Check(player.ReceivedShots==received+1&&audio.PlayedCount(pair.Item2)==before+1,"real special fire emits once: "+pair.Item1);
        }
        yield return new WaitForSeconds(.8f);Check(audio.PlayedCount(ArenaAudio.Cue.Explosion)>0&&audio.PlayedCount(ArenaAudio.Cue.Impact)>0,"real projectile impacts and grenade burst produce sound");
        player.SetSpecialHeld(false);move.SetInput(1);yield return new WaitForSeconds(.4f);move.SetInput(0,true);yield return Frames();move.SetInput(0);yield return new WaitForSeconds(.2f);move.SetInput(0,true);yield return Frames();move.SetInput(0);
        yield return Wait(()=>move.Grounded,"land",5);yield return Frames();
        Check(audio.PlayedCount(ArenaAudio.Cue.Step)>0&&audio.PlayedCount(ArenaAudio.Cue.Jump)>0&&audio.PlayedCount(ArenaAudio.Cue.DoubleJump)>0&&audio.PlayedCount(ArenaAudio.Cue.Land)>0,"actual motor steps, double jump and landing produce sound");
        int hurt=audio.PlayedCount(ArenaAudio.Cue.Hurt);player.ReceiveServerDamage(5,player.LifeVersion);yield return Frames();Check(audio.PlayedCount(ArenaAudio.Cue.Hurt)==hurt+1,"local damage produces one hurt sound");
        var bounds=(Bounds)typeof(SideScrollNetworkPlayer).GetProperty("ArenaHitBounds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(player);
        int heal=audio.PlayedCount(ArenaAudio.Cue.PickupHealth);match.HealthPickups.Add(new ArenaObjectiveMatch.AmmoPickup{Id=710001,Amount=5,Respawn=15,Position=bounds.center,Available=true});yield return Frames();Check(audio.PlayedCount(ArenaAudio.Cue.PickupHealth)==heal+1,"actual health pickup produces one sound");
        int shield=audio.PlayedCount(ArenaAudio.Cue.PickupShield);match.HealthPickups.Add(new ArenaObjectiveMatch.AmmoPickup{Id=710002,Amount=50,Shield=true,Respawn=15,Position=bounds.center,Available=true});yield return Frames();Check(audio.PlayedCount(ArenaAudio.Cue.PickupShield)==shield+1,"actual shield pickup produces one sound");
        int shieldHit=audio.PlayedCount(ArenaAudio.Cue.ShieldHit);player.ReceiveServerDamage(5,player.LifeVersion);yield return Frames();Check(audio.PlayedCount(ArenaAudio.Cue.ShieldHit)==shieldHit+1,"shield damage has a distinct sound");
        // Leave the fixed supply pad and spend a round before testing collection.
        move.SetSpawnPosition(new Vector2(-5.8f,6.84f));player.SelectAmmo(SideScrollNetworkPlayer.AmmoKind.InstantShotgun);player.SetSpecialHeld(true);yield return new WaitForSeconds(.6f);shots.Fire();yield return Frames();
        bounds=(Bounds)typeof(SideScrollNetworkPlayer).GetProperty("ArenaHitBounds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(player);
        int ammo=audio.PlayedCount(ArenaAudio.Cue.PickupAmmo);match.AmmoPickups.Add(new ArenaObjectiveMatch.AmmoPickup{Id=710003,Ammo=ArenaObjectiveMatch.SupplyAmmo.Shotgun,Amount=1,Respawn=15,Position=bounds.center,Available=true});yield return Frames();Check(audio.PlayedCount(ArenaAudio.Cue.PickupAmmo)==ammo+1,"actual special-ammo pickup produces one sound");
        ammo=audio.PlayedCount(ArenaAudio.Cue.PickupAmmo);match.AmmoPickups.Add(new ArenaObjectiveMatch.AmmoPickup{Id=710004,Ammo=ArenaObjectiveMatch.SupplyAmmo.Shotgun,Amount=1,Respawn=15,Position=bounds.center,Available=true});yield return new WaitForSeconds(.15f);
        Check(audio.PlayedCount(ArenaAudio.Cue.PickupAmmo)==ammo,"full inventory does not emit a pickup chime");
        match.AmmoPickups.Add(new ArenaObjectiveMatch.AmmoPickup{Id=710005,Ammo=ArenaObjectiveMatch.SupplyAmmo.Rifle,Amount=1,Position=(Vector2)bounds.center+Vector2.up*5,Available=true,ExpiresAt=match.Now+.1});yield return new WaitForSeconds(.2f);
        Check(audio.PlayedCount(ArenaAudio.Cue.PickupAmmo)==ammo,"drop expiration does not emit a pickup chime");
        Check(!audio.Play(ArenaAudio.Cue.Normal,new Vector2(10000,10000),8123,false),"distant world sound rejected");
        Check(audio.Play(ArenaAudio.Cue.UiClick,Vector2.zero,9001,true)&&!audio.Play(ArenaAudio.Cue.UiClick,Vector2.zero,9001,true),"duplicate event throttled");
        yield return Frames();
        for(int i=0;i<20;i++)audio.Play(ArenaAudio.Cue.Explosion,(Vector2)Camera.main.transform.position,10000+i,false);
        var sources=audio.GetComponents<AudioSource>();float bound=0;int active=0;foreach(var source in sources)if(source.isPlaying){active++;bound+=source.volume*.45f;}
        Check(active<=16&&bound<=.751f,"overlap remains within fixed voice and peak budget");
        var output=new float[512];float energy=0;for(int frame=0;frame<8;frame++){yield return null;foreach(var source in sources)if(source.isPlaying){source.GetOutputData(output,0);foreach(float v in output)energy+=v*v;}}
        Check(energy>0,"Unity audio source output contains a nonzero DSP signal");
        ArenaAudio.SetEffectsVolume(0);Check(!audio.Play(ArenaAudio.Cue.UiClick,Vector2.zero,7001,true),"effects mute prevents playback");ArenaAudio.SetEffectsVolume(.65f);
        yield return Capture("qe-and-audio");RelayArenaPrototype.Instance.SetMenuOpen(true);yield return Frames();yield return Capture("audio-settings");RelayArenaPrototype.Instance.SetMenuOpen(false);
        int death=audio.PlayedCount(ArenaAudio.Cue.Death);player.ReceiveServerDamage(999,player.LifeVersion,null,true);yield return Frames();Check(audio.PlayedCount(ArenaAudio.Cue.Death)==death+1,"death sound emitted once");
        var finish=match.State.Value;finish.End=match.Now+.1;match.State.Value=finish;yield return Wait(()=>ArenaObjectiveMatch.Ended,"finish");yield return Frames();Check(audio.PlayedCount(ArenaAudio.Cue.RoundEnd)==1,"round end sound emitted once");
        ArenaAudio.SetEffectsVolume(savedEffects);if(!savedKey){PlayerPrefs.DeleteKey("RightWalk.SfxVolume");PlayerPrefs.Save();}size.SetValue(view,oldSize);session.DisconnectLocal();yield return Frames();
    }
}
