using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RightWalkPrototype
{
    [DefaultExecutionOrder(110), RequireComponent(typeof(RightFacingAimRig))]
    public sealed partial class RightFacingShotPreview : MonoBehaviour
    {
        [Min(.01f)] public float shotInterval = .15f;
        [Min(.1f)] public float bulletSpeed = 15f;
        public bool readMouse = true;
        public System.Action<Vector2,Vector2> ShotRequested;
        private RightFacingAimRig aim;
        private CharacterDeathTrial life;
        private SideScrollNetworkPlayer player;
        private Material material;
        private float nextShot;
        private bool audioPrimaryHeld;
        private readonly List<Shot> shots = new List<Shot>();
        private readonly Stack<LineRenderer> pool = new Stack<LineRenderer>();
        private readonly List<Spark> sparks = new List<Spark>();
        private sealed class Spark
        {
            public LineRenderer visual;
            public Vector2 position, velocity;
            public float age, born, lifetime, length, width;
            public bool muzzle;
            public Color head,tail;
        }
        private sealed class Shot
        {
            public LineRenderer visual;
            public Vector2 position, direction;
            public float remaining, impactTime,travelled;
            public bool impacted;
            public bool authoritativePath, hitsAtEnd, shotgun;
        }
        private void Awake() { aim=GetComponent<RightFacingAimRig>();life=GetComponentInParent<CharacterDeathTrial>();player=GetComponentInParent<SideScrollNetworkPlayer>(); }
        public void Fire()
        {
            if(aim==null)aim=GetComponent<RightFacingAimRig>();
            if(!isActiveAndEnabled || !Application.isPlaying || (player!=null&&player.NormalAmmoEnabled?false:Time.time<nextShot) || RelayArenaPrototype.ControlsBlocked || life!=null&&life.IsDead)return;
            if(player!=null&&player.NormalAmmoEnabled){if(!player.TryLocalFire())return;}
            else nextShot=Time.time+Mathf.Max(.01f,player!=null?player.ActiveShotInterval:shotInterval);
            if(ShotRequested!=null){ShotRequested(aim.MuzzlePosition,aim.AimDirection);return;}
            ShowShot(aim.MuzzlePosition,aim.AimDirection,Mathf.Max(.1f,aim.previewAimRange),false,false);
        }
        public void ShowShot(Vector2 origin, Vector2 direction, float range, bool authoritativePath, bool hitsAtEnd,bool emitMuzzle=true,bool shotgun=false)
        {
            if(aim==null)aim=GetComponent<RightFacingAimRig>();
            var visual=AcquireVisual();
            var shot=new Shot {visual=visual,position=origin,direction=direction.normalized,remaining=Mathf.Max(0f,range),authoritativePath=authoritativePath,hitsAtEnd=hitsAtEnd,shotgun=shotgun};
            shots.Add(shot);
            Draw(shot);
            if(emitMuzzle){ArenaAudio.World(shotgun?ArenaAudio.Cue.Shotgun:ArenaAudio.Cue.Normal,origin,GetInstanceID());EmitMuzzle(shot.position,shot.direction);aim.KickRecoil();}
        }
        private LineRenderer AcquireVisual()
        {
            if(pool.Count>0){var reused=pool.Pop();reused.sortingOrder=30;reused.positionCount=2;reused.numCapVertices=0;reused.gameObject.SetActive(true);return reused;}
            if(material==null)material=new Material(Shader.Find("Sprites/Default"));
            var go=new GameObject("Preview bullet");
            var line=go.AddComponent<LineRenderer>();
            line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=2;
            line.startWidth=line.endWidth=.018f;line.sortingOrder=30;
            return line;
        }
        private void EmitMuzzle(Vector2 position, Vector2 direction)
        {
            var side=new Vector2(-direction.y,direction.x);
            AddSpark(position,direction*.8f,.045f,.19f,.065f,true);
            AddSpark(position,(direction+side*.55f)*.6f,.035f,.11f,.035f,true);
            AddSpark(position,(direction-side*.55f)*.6f,.035f,.11f,.035f,true);
        }
        private void AddSpark(Vector2 position, Vector2 velocity, float lifetime, float length, float width, bool muzzle,Color? head=null,Color? tail=null)
        {
            var spark=new Spark {visual=AcquireVisual(),position=position,velocity=velocity,born=Time.time,lifetime=lifetime,length=length,width=width,muzzle=muzzle,head=head??new Color(1f,.98f,.8f),tail=tail??new Color(1f,.55f,.1f,0)};
            sparks.Add(spark);
            DrawSpark(spark);
        }
        private void EmitImpact(Vector2 position, Vector2 incoming)
        {
            ArenaAudio.World(ArenaAudio.Cue.Impact,position,GetInstanceID());
            for(int i=0;i<6;i++)
            {
                float angle=(-75f+i*30f)*Mathf.Deg2Rad;
                var back=-incoming;
                var direction=new Vector2(back.x*Mathf.Cos(angle)-back.y*Mathf.Sin(angle),back.x*Mathf.Sin(angle)+back.y*Mathf.Cos(angle));
                AddSpark(position,direction*Random.Range(.65f,1.4f),Random.Range(.12f,.22f),.045f,.018f,false);
            }
            AddSpark(position,-incoming*.05f,.055f,.09f,.065f,false);
        }
        private void DrawSpark(Spark spark)
        {
            float fade=1f-spark.age/spark.lifetime;
            spark.visual.startWidth=spark.width*fade;spark.visual.endWidth=0f;
            var head=spark.head;head.a*=fade;spark.visual.startColor=head;
            spark.visual.endColor=spark.tail;
            var end=spark.position+spark.velocity.normalized*spark.length*fade;
            spark.visual.SetPosition(0,new Vector3(spark.position.x,spark.position.y,aim.MuzzlePosition.z));
            spark.visual.SetPosition(1,new Vector3(end.x,end.y,aim.MuzzlePosition.z));
        }
        // Sample each button once, then apply one mode state per frame.
        // Clearing SpecialHeld before restoring RMB would restart switch delay forever.
        private void ProcessFireInput(bool blocked,bool special,bool primary,int cycle)
        {
            bool pressed=primary&&!audioPrimaryHeld;audioPrimaryHeld=primary;
            if(blocked){if(player!=null)player.SetSpecialHeld(false);return;}
            if(player!=null){if(cycle!=0)player.CycleAmmo(cycle);player.SetSpecialHeld(special);}
            if(primary){if(pressed&&player!=null&&player.ActiveAmmoEmpty)ArenaAudio.Ui(ArenaAudio.Cue.DryFire);Fire();}
        }
        private void LateUpdate()
        {
            if(readMouse)
            {
                bool special=false,primary=false;int cycle=0;
#if ENABLE_INPUT_SYSTEM
                var k=Keyboard.current;var mouse=Mouse.current;
                if(k!=null){if(k.qKey.wasPressedThisFrame)cycle=-1;else if(k.eKey.wasPressedThisFrame)cycle=1;}
                if(mouse!=null){special=mouse.rightButton.isPressed;primary=mouse.leftButton.isPressed;}
#elif ENABLE_LEGACY_INPUT_MANAGER
                if(Input.GetKeyDown(KeyCode.Q))cycle=-1;else if(Input.GetKeyDown(KeyCode.E))cycle=1;
                special=Input.GetMouseButton(1);primary=Input.GetMouseButton(0);
#endif
                ProcessFireInput(RelayArenaPrototype.FireInputBlocked,special,primary,cycle);
            }
            UpdateGrenades();UpdateRifles();UpdateInstantTraces();
            for(int i=shots.Count-1;i>=0;i--)
            {
                var shot=shots[i];
                if(shot.impacted)
                {
                    shot.impactTime-=Time.deltaTime;
                    if(shot.impactTime<=0){Release(shot);shots.RemoveAt(i);}
                    continue;
                }
                float speed=shot.shotgun?SideScrollNetworkPlayer.ShotgunPelletSpeed:Mathf.Max(.1f,bulletSpeed);
                float step=Mathf.Min(shot.remaining,speed*Time.deltaTime);
                bool hit=false;
                if(shot.authoritativePath)shot.position+=shot.direction*step;
                else shot.position=aim.TraceShot(shot.position,shot.direction,step,out hit);
                shot.remaining-=step;shot.travelled+=step;
                if(shot.authoritativePath && shot.remaining<=.00001f)hit=shot.hitsAtEnd;
                if(hit){shot.impacted=true;shot.impactTime=.035f;EmitImpact(shot.position,shot.direction);}
                Draw(shot);
                if(!hit && shot.remaining<=0){Release(shot);shots.RemoveAt(i);}
            }
            for(int i=sparks.Count-1;i>=0;i--)
            {
                var spark=sparks[i];float elapsed=Mathf.Max(0,Time.time-spark.born-spark.age);spark.age=Time.time-spark.born;
                if(spark.age>=spark.lifetime){spark.visual.gameObject.SetActive(false);pool.Push(spark.visual);sparks.RemoveAt(i);continue;}
                if(!spark.muzzle){spark.velocity+=Vector2.down*2f*elapsed;spark.position+=spark.velocity*elapsed;}
                DrawSpark(spark);
            }
        }
        private void Draw(Shot shot)
        {
            shot.visual.startColor=shot.shotgun?new Color(1f,.8f,.35f):new Color(1f,1f,.85f);
            shot.visual.endColor=new Color(1f,.65f,.12f,0f);
            bool arena=RelayArenaPrototype.Instance!=null;
            shot.visual.numCapVertices=arena&&!shot.shotgun?6:0;
            shot.visual.startWidth=arena?(shot.shotgun?.045f:.075f):.032f;shot.visual.endWidth=.004f;
            float tail=arena?(shot.shotgun?.32f:.8f):.18f;
            var offset=shot.impacted?Vector2.up*.025f:-shot.direction*(arena?Mathf.Min(tail,shot.travelled):tail);
            shot.visual.SetPosition(0,new Vector3(shot.position.x,shot.position.y,aim.MuzzlePosition.z));
            shot.visual.SetPosition(1,new Vector3(shot.position.x+offset.x,shot.position.y+offset.y,aim.MuzzlePosition.z));
        }
        private void Release(Shot shot) {shot.visual.gameObject.SetActive(false);pool.Push(shot.visual);}
        private void OnDisable()
        {
            ClearGrenades();ClearRifles();ClearInstantTraces();
            foreach(var shot in shots)if(shot.visual!=null)Destroy(shot.visual.gameObject);
            shots.Clear();
            foreach(var spark in sparks)if(spark.visual!=null)Destroy(spark.visual.gameObject);
            sparks.Clear();
            foreach(var visual in pool)if(visual!=null)Destroy(visual.gameObject);
            pool.Clear();
            if(material!=null)Destroy(material);
        }
    }
}
