using System.Collections.Generic;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class RightFacingShotPreview
    {
        const float InstantTraceLifetime=.16f;
        sealed class InstantTrace{public LineRenderer Visual;public float Age;}
        readonly List<InstantTrace> instantTraces=new();
        public void ShowInstantPellet(Vector2 origin,Vector2 direction,float range,bool hit,bool muzzle)
        {
            if(aim==null)aim=GetComponent<RightFacingAimRig>();
            var line=AcquireVisual();var end=origin+direction.normalized*range;
            line.SetPosition(0,new Vector3(origin.x,origin.y,aim.MuzzlePosition.z));line.SetPosition(1,new Vector3(end.x,end.y,aim.MuzzlePosition.z));
            var trace=new InstantTrace{Visual=line};instantTraces.Add(trace);DrawInstantTrace(trace);
            if(hit)EmitImpact(end,direction);
            if(muzzle){EmitMuzzle(origin,direction);AddSpark(origin,direction,.055f,.45f,.12f,true);aim.KickRecoil();}
        }
        void DrawInstantTrace(InstantTrace trace)
        {
            float fade=Mathf.Clamp01(1-trace.Age/InstantTraceLifetime);
            trace.Visual.startWidth=.018f*fade;trace.Visual.endWidth=.055f*fade;
            trace.Visual.startColor=new Color(1,.5f,.12f,fade*.2f);trace.Visual.endColor=new Color(1,.98f,.8f,fade);
        }
        void UpdateInstantTraces()
        {
            for(int i=instantTraces.Count-1;i>=0;i--)
            {
                var trace=instantTraces[i];trace.Age+=Time.deltaTime;
                if(trace.Age>=InstantTraceLifetime){trace.Visual.gameObject.SetActive(false);pool.Push(trace.Visual);instantTraces.RemoveAt(i);}
                else DrawInstantTrace(trace);
            }
        }
        void ClearInstantTraces(){foreach(var trace in instantTraces)if(trace.Visual!=null)Destroy(trace.Visual.gameObject);instantTraces.Clear();}
    }
}
