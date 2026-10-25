using System.Collections.Generic;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class RightFacingShotPreview
    {
        const float InstantTraceLifetime=.16f;
        sealed class InstantTrace{public LineRenderer Visual;public float Age;}
        sealed class ShotgunFlash{public LineRenderer Glow,Flame,Core;public Vector2 Direction;public float Began;}
        readonly List<InstantTrace> instantTraces=new();
        readonly List<ShotgunFlash> shotgunFlashes=new();
        public void ShowInstantPellet(Vector2 origin,Vector2 direction,float range,bool hit,bool muzzle)
        {
            if(aim==null)aim=GetComponent<RightFacingAimRig>();
            var line=AcquireVisual();var end=origin+direction.normalized*range;
            line.SetPosition(0,new Vector3(origin.x,origin.y,aim.MuzzlePosition.z));line.SetPosition(1,new Vector3(end.x,end.y,aim.MuzzlePosition.z));
            var trace=new InstantTrace{Visual=line};instantTraces.Add(trace);DrawInstantTrace(trace);
            if(hit)EmitImpact(end,direction);
            if(muzzle){ArenaAudio.World(ArenaAudio.Cue.Shotgun,origin,GetInstanceID());EmitShotgunMuzzle(direction);aim.KickRecoil();}
        }
        void EmitShotgunMuzzle(Vector2 direction)
        {
            // One compact flash per shell; follow the barrel while the shooter moves.
            var flash=new ShotgunFlash{Glow=AcquireVisual(),Flame=AcquireVisual(),Core=AcquireVisual(),Direction=direction.normalized,Began=Time.time};
            flash.Glow.sortingOrder=31;flash.Flame.sortingOrder=32;flash.Core.sortingOrder=33;
            shotgunFlashes.Add(flash);DrawShotgunFlash(flash,0);
        }
        void DrawShotgunFlash(ShotgunFlash flash,float age)
        {
            float fade=Mathf.Clamp01(1-age/.1f),size=Mathf.Lerp(.65f,1,fade);
            var origin=aim.MuzzlePosition;var direction=(Vector3)flash.Direction;
            void Lobe(LineRenderer line,float length,float width,Color color)
            {
                line.numCapVertices=8;line.startWidth=width*size;line.endWidth=width*.25f*size;
                color.a*=fade;line.startColor=color;var end=color;end.a=0;line.endColor=end;
                line.SetPosition(0,origin+direction*.04f);line.SetPosition(1,origin+direction*(length*size));
            }
            Lobe(flash.Glow,.8f,.6f,new Color(1,.55f,.12f,.22f));
            Lobe(flash.Flame,.72f,.35f,new Color(1,.72f,.2f,.9f));
            Lobe(flash.Core,.4f,.2f,new Color(1,1,.85f));
        }
        void DrawInstantTrace(InstantTrace trace)
        {
            float fade=Mathf.Clamp01(1-trace.Age/InstantTraceLifetime);
            trace.Visual.startWidth=.018f*fade;trace.Visual.endWidth=.055f*fade;
            trace.Visual.startColor=new Color(1,.5f,.12f,fade*.2f);trace.Visual.endColor=new Color(1,.98f,.8f,fade);
        }
        void UpdateInstantTraces()
        {
            for(int i=shotgunFlashes.Count-1;i>=0;i--)
            {
                var flash=shotgunFlashes[i];float age=Time.time-flash.Began;
                if(age>=.1f||ArenaObjectiveMatch.TransitionActive)
                {ReturnGrenadeLine(flash.Glow);ReturnGrenadeLine(flash.Flame);ReturnGrenadeLine(flash.Core);shotgunFlashes.RemoveAt(i);}
                else DrawShotgunFlash(flash,age);
            }
            for(int i=instantTraces.Count-1;i>=0;i--)
            {
                var trace=instantTraces[i];trace.Age+=Time.deltaTime;
                if(trace.Age>=InstantTraceLifetime){trace.Visual.gameObject.SetActive(false);pool.Push(trace.Visual);instantTraces.RemoveAt(i);}
                else DrawInstantTrace(trace);
            }
        }
        void ClearInstantTraces()
        {
            foreach(var trace in instantTraces)if(trace.Visual!=null)Destroy(trace.Visual.gameObject);instantTraces.Clear();
            foreach(var flash in shotgunFlashes){Destroy(flash.Glow.gameObject);Destroy(flash.Flame.gameObject);Destroy(flash.Core.gameObject);}shotgunFlashes.Clear();
        }
    }
}
