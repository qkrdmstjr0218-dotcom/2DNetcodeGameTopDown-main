using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RightWalkPrototype
{
    [DefaultExecutionOrder(110), RequireComponent(typeof(RightFacingAimRig))]
    public sealed class RightFacingShotPreview : MonoBehaviour
    {
        [Min(.01f)] public float shotInterval = .15f;
        [Min(.1f)] public float bulletSpeed = 12f;
        public bool readMouse = true;
        public System.Action<Vector2,Vector2> ShotRequested;
        private RightFacingAimRig aim;
        private CharacterDeathTrial life;
        private Material material;
        private float nextShot;
        private readonly List<Shot> shots = new List<Shot>();
        private readonly Stack<LineRenderer> pool = new Stack<LineRenderer>();
        private readonly List<Spark> sparks = new List<Spark>();
        private sealed class Spark
        {
            public LineRenderer visual;
            public Vector2 position, velocity;
            public float age, lifetime, length, width;
            public bool muzzle;
        }
        private sealed class Shot
        {
            public LineRenderer visual;
            public Vector2 position, direction;
            public float remaining, impactTime;
            public bool impacted;
            public bool authoritativePath, hitsAtEnd;
        }
        private void Awake() { aim=GetComponent<RightFacingAimRig>();life=GetComponentInParent<CharacterDeathTrial>(); }
        public void Fire()
        {
            if(aim==null)aim=GetComponent<RightFacingAimRig>();
            if(!isActiveAndEnabled || !Application.isPlaying || Time.time<nextShot || life!=null&&life.IsDead)return;
            nextShot=Time.time+Mathf.Max(.01f,shotInterval);
            if(ShotRequested!=null){ShotRequested(aim.MuzzlePosition,aim.AimDirection);return;}
            ShowShot(aim.MuzzlePosition,aim.AimDirection,Mathf.Max(.1f,aim.previewAimRange),false,false);
        }
        public void ShowShot(Vector2 origin, Vector2 direction, float range, bool authoritativePath, bool hitsAtEnd)
        {
            if(aim==null)aim=GetComponent<RightFacingAimRig>();
            var visual=AcquireVisual();
            var shot=new Shot {visual=visual,position=origin,direction=direction.normalized,remaining=Mathf.Max(0f,range),authoritativePath=authoritativePath,hitsAtEnd=hitsAtEnd};
            shots.Add(shot);
            Draw(shot);
            EmitMuzzle(shot.position,shot.direction);
            aim.KickRecoil();
        }
        private LineRenderer AcquireVisual()
        {
            if(pool.Count>0){var reused=pool.Pop();reused.gameObject.SetActive(true);return reused;}
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
        private void AddSpark(Vector2 position, Vector2 velocity, float lifetime, float length, float width, bool muzzle)
        {
            var spark=new Spark {visual=AcquireVisual(),position=position,velocity=velocity,lifetime=lifetime,length=length,width=width,muzzle=muzzle};
            sparks.Add(spark);
            DrawSpark(spark);
        }
        private void EmitImpact(Vector2 position, Vector2 incoming)
        {
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
            spark.visual.startColor=new Color(1f,.98f,.8f,fade);
            spark.visual.endColor=new Color(1f,.55f,.1f,0f);
            var end=spark.position+spark.velocity.normalized*spark.length*fade;
            spark.visual.SetPosition(0,new Vector3(spark.position.x,spark.position.y,aim.MuzzlePosition.z));
            spark.visual.SetPosition(1,new Vector3(end.x,end.y,aim.MuzzlePosition.z));
        }
        private void LateUpdate()
        {
            if(readMouse)
            {
#if ENABLE_INPUT_SYSTEM
                if(Mouse.current!=null && Mouse.current.leftButton.isPressed)Fire();
#elif ENABLE_LEGACY_INPUT_MANAGER
                if(Input.GetMouseButton(0))Fire();
#endif
            }
            for(int i=shots.Count-1;i>=0;i--)
            {
                var shot=shots[i];
                if(shot.impacted)
                {
                    shot.impactTime-=Time.deltaTime;
                    if(shot.impactTime<=0){Release(shot);shots.RemoveAt(i);}
                    continue;
                }
                float step=Mathf.Min(shot.remaining,Mathf.Max(.1f,bulletSpeed)*Time.deltaTime);
                bool hit=false;
                if(shot.authoritativePath)shot.position+=shot.direction*step;
                else shot.position=aim.TraceShot(shot.position,shot.direction,step,out hit);
                shot.remaining-=step;
                if(shot.authoritativePath && shot.remaining<=.00001f)hit=shot.hitsAtEnd;
                if(hit){shot.impacted=true;shot.impactTime=.035f;EmitImpact(shot.position,shot.direction);}
                Draw(shot);
                if(!hit && shot.remaining<=0){Release(shot);shots.RemoveAt(i);}
            }
            for(int i=sparks.Count-1;i>=0;i--)
            {
                var spark=sparks[i];spark.age+=Time.deltaTime;
                if(spark.age>=spark.lifetime){spark.visual.gameObject.SetActive(false);pool.Push(spark.visual);sparks.RemoveAt(i);continue;}
                if(!spark.muzzle){spark.velocity+=Vector2.down*2f*Time.deltaTime;spark.position+=spark.velocity*Time.deltaTime;}
                DrawSpark(spark);
            }
        }
        private void Draw(Shot shot)
        {
            shot.visual.startColor=new Color(1f,1f,.85f);
            shot.visual.endColor=new Color(1f,.65f,.12f,0f);
            shot.visual.startWidth=.032f;shot.visual.endWidth=.004f;
            var offset=shot.impacted ? Vector2.up*.025f : -shot.direction*.18f;
            shot.visual.SetPosition(0,new Vector3(shot.position.x,shot.position.y,aim.MuzzlePosition.z));
            shot.visual.SetPosition(1,new Vector3(shot.position.x+offset.x,shot.position.y+offset.y,aim.MuzzlePosition.z));
        }
        private void Release(Shot shot) {shot.visual.gameObject.SetActive(false);pool.Push(shot.visual);}
        private void OnDisable()
        {
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
