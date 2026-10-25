using System.Collections.Generic;
using UnityEngine;
namespace RightWalkPrototype
{
    [DefaultExecutionOrder(450)]
    public sealed class ArenaAudio:MonoBehaviour
    {
        public enum Cue{Normal,Shotgun,Rifle,GrenadeLaunch,Explosion,Impact,EnemyShot,EnemyBreak,Hurt,ShieldHit,Death,Jump,DoubleJump,Land,Step,PickupAmmo,PickupHealth,PickupShield,Select,UiClick,DryFire,Countdown,RoundStart,RoundEnd}
        public static ArenaAudio Instance{get;private set;}
        public static float EffectsVolume=>Instance!=null?Instance.effectsVolume:.65f;
        const int WorldVoices=12,TotalVoices=16;
        readonly Dictionary<Cue,AudioClip> clips=new();readonly Dictionary<long,float> cooldowns=new();readonly Dictionary<Cue,int> counts=new();
        readonly AudioSource[] voices=new AudioSource[TotalVoices];readonly float[] gains=new float[TotalVoices],began=new float[TotalVoices];readonly int[] priorities=new int[TotalVoices];
        float effectsVolume,findAt,stepAt;SideScrollNetworkPlayer local;SideScrollMapPreview motor;Rigidbody2D body;
        int hp,shield,jumps;float shieldTime,vertical;bool grounded,observing;uint life;SideScrollNetworkPlayer.AmmoKind selected;
        ArenaObjectiveMatch.Stage phase;bool hasPhase;int countdown=-1;
        public int PlayedCount(Cue cue)=>counts.TryGetValue(cue,out int value)?value:0;
        void Awake()
        {
            Instance=this;effectsVolume=Mathf.Clamp01(PlayerPrefs.GetFloat("RightWalk.SfxVolume",.65f));
            foreach(Cue cue in System.Enum.GetValues(typeof(Cue)))clips[cue]=Resources.Load<AudioClip>("ArenaAudio/"+cue);
            for(int i=0;i<voices.Length;i++){var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;source.spatialBlend=0;source.dopplerLevel=0;source.priority=i<WorldVoices?128:32;voices[i]=source;}
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
        public static void SetEffectsVolume(float value)
        {if(Instance==null)return;Instance.effectsVolume=Mathf.Clamp01(value);PlayerPrefs.SetFloat("RightWalk.SfxVolume",Instance.effectsVolume);PlayerPrefs.Save();Instance.Mix();}
        public static void Ui(Cue cue)=>Instance?.Play(cue,Vector2.zero,0,true);
        public static void World(Cue cue,Vector2 at,int emitter=0)=>Instance?.Play(cue,at,emitter,false);
        static int Priority(Cue cue)=>cue==Cue.Hurt||cue==Cue.ShieldHit||cue==Cue.Death?3:cue==Cue.Step||cue==Cue.Impact?0:1;
        public bool Play(Cue cue,Vector2 at,int emitter,bool ui)
        {
            if(!Application.isFocused||effectsVolume<=0||!clips.TryGetValue(cue,out var clip)||clip==null)return false;
            float gain=.45f,pan=0;
            if(!ui)
            {
                var camera=Camera.main;if(camera==null)return false;var delta=at-(Vector2)camera.transform.position;
                float distance=delta.magnitude;if(distance>=22)return false;
                gain*=Mathf.Clamp01(1-distance/24);pan=Mathf.Clamp(delta.x/Mathf.Max(1,camera.orthographicSize*camera.aspect)*.6f,-.75f,.75f);
                if(cue==Cue.Step)gain*=.23f;else if(cue==Cue.Impact||cue==Cue.EnemyShot)gain*=.5f;
            }
            else gain=.32f;
            long key=((long)emitter<<8)|(byte)cue;float now=Time.unscaledTime;
            if(cooldowns.TryGetValue(key,out float until)&&now<until)return false;
            int first=ui?WorldVoices:0,last=ui?TotalVoices:WorldVoices,slot=-1;
            for(int i=first;i<last;i++)if(!voices[i].isPlaying){slot=i;break;}
            int priority=Priority(cue);
            if(slot<0)for(int i=first;i<last;i++)if(priorities[i]<=priority&&(slot<0||priorities[i]<priorities[slot]||priorities[i]==priorities[slot]&&began[i]<began[slot]))slot=i;
            if(slot<0)return false;
            cooldowns[key]=now+(cue==Cue.Impact?.055f:cue==Cue.Step?.12f:cue==Cue.PickupHealth?.25f:cue==Cue.PickupShield?.2f:cue==Cue.PickupAmmo?.1f:.035f);
            // Bound long-running sessions even if many short-lived emitter IDs occur.
            if(cooldowns.Count>2048)cooldowns.Clear();
            voices[slot].Stop();voices[slot].clip=clip;voices[slot].pitch=1+(PlayedCount(cue)%3-1)*.018f;voices[slot].panStereo=pan;
            gains[slot]=gain;priorities[slot]=priority;began[slot]=now;
            counts[cue]=PlayedCount(cue)+1;Mix(slot);voices[slot].Play();return true;
        }
        void Mix(int reserved=-1)
        {
            float bound=0;for(int i=0;i<voices.Length;i++)if(voices[i].isPlaying||i==reserved)bound+=gains[i]*.45f;
            float scale=Mathf.Min(1,.75f/Mathf.Max(.001f,bound));
            for(int i=0;i<voices.Length;i++)if(voices[i].isPlaying||i==reserved)voices[i].volume=gains[i]*effectsVolume*scale;
        }
        void LateUpdate()
        {
            Mix();var match=ArenaObjectiveMatch.Instance;
            if(match!=null&&match.IsSpawned)
            {
                var current=match.State.Value.Phase;
                if(hasPhase&&phase!=current){if(phase==ArenaObjectiveMatch.Stage.Countdown&&current==ArenaObjectiveMatch.Stage.Playing)Ui(Cue.RoundStart);else if(current==ArenaObjectiveMatch.Stage.Finished)Ui(Cue.RoundEnd);countdown=-1;}
                phase=current;hasPhase=true;
                if(current==ArenaObjectiveMatch.Stage.Countdown&&match.State.Value.End>0){int n=Mathf.CeilToInt((float)(match.State.Value.End-match.Now));if(n>0&&n<=3&&n!=countdown){countdown=n;Ui(Cue.Countdown);}}
            }
            else hasPhase=false;
            if(local==null||!local.IsSpawned)
            {
                observing=false;if(Time.unscaledTime<findAt)return;findAt=Time.unscaledTime+.3f;
                foreach(var p in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsSpawned&&p.IsOwner){local=p;motor=p.GetComponent<SideScrollMapPreview>();body=p.GetComponent<Rigidbody2D>();break;}
            }
            if(local==null||!local.IsSpawned)return;
            if(!observing||life!=local.LifeVersion){Snapshot();observing=true;return;}
            bool gameplay=match!=null&&match.IsSpawned&&(match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing||ArenaTutorial.Active);
            var at=(Vector2)local.transform.position;
            if(gameplay)
            {
                if(local.Health<hp)Ui(local.IsDead?Cue.Death:Cue.Hurt);
                else if(local.Health>hp&&ArenaTutorial.Active)Ui(Cue.PickupHealth);
                if(local.Shield<shield&&shieldTime>Time.unscaledDeltaTime+.1f)Ui(Cue.ShieldHit);
            }
            if(!local.IsDead&&!RelayArenaPrototype.ControlsBlocked)
            {
                if(local.SelectedAmmo!=selected)Ui(Cue.Select);
                if(motor.JumpCount>jumps)World(motor.JumpCount>1?Cue.DoubleJump:Cue.Jump,at,local.GetInstanceID());
                if(motor.Grounded&&!grounded&&vertical<-1.3f)World(Cue.Land,at,local.GetInstanceID());
                if(motor.Grounded&&!motor.HitReacting&&Mathf.Abs(body.linearVelocity.x)>.5f&&Time.unscaledTime>=stepAt){World(Cue.Step,at,local.GetInstanceID());stepAt=Time.unscaledTime+(Mathf.Abs(body.linearVelocity.x)>3?.24f:.36f);}
            }
            Snapshot();
        }
        void Snapshot(){hp=local.Health;shield=local.Shield;shieldTime=local.ShieldRemaining;jumps=motor.JumpCount;grounded=motor.Grounded;vertical=body.linearVelocity.y;life=local.LifeVersion;selected=local.SelectedAmmo;}
    }
}
