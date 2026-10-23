using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class RightFacingShotPreview
    {
        sealed class GrenadeFlight{public uint Id;public LineRenderer Body,Tail;public Vector2 Origin,Velocity;public double Began;public int Version;}
        const float GrenadeBlastVisualSeconds=.5f;
        sealed class GrenadeBlast{public LineRenderer Ring,Core,Fire;public LineRenderer[] Smoke;public Vector2 Point;public float Began;}
        readonly List<GrenadeFlight> grenades=new();readonly List<GrenadeBlast> grenadeBlasts=new();
        public void ShowGrenade(uint id,Vector2 origin,Vector2 velocity,double began,int version)
        {
            var g=new GrenadeFlight{Id=id,Origin=origin,Velocity=velocity,Began=began,Version=version,Body=AcquireVisual(),Tail=AcquireVisual()};g.Body.numCapVertices=6;g.Body.sortingOrder=31;grenades.Add(g);DrawGrenade(g);
            AddSpark(origin,velocity,.06f,.32f,.1f,true,new Color(.8f,1,.5f),new Color(.3f,.8f,.1f,0));aim.KickRecoil(1.5f);
        }
        void DrawGrenade(GrenadeFlight g)
        {
            float age=Mathf.Max(0,(float)(NetworkManager.Singleton.ServerTime.Time-g.Began));var p=ArenaGrenadeBallistics.At(g.Origin,g.Velocity,age);var direction=(g.Velocity+Vector2.down*ArenaGrenadeBallistics.Gravity*age).normalized;
            g.Body.startWidth=.13f;g.Body.endWidth=.09f;g.Body.startColor=new Color(.9f,1,.7f);g.Body.endColor=new Color(.45f,.9f,.15f);g.Body.SetPosition(0,(Vector3)(p+direction*.08f));g.Body.SetPosition(1,(Vector3)(p-direction*.07f));
            g.Tail.startWidth=.065f;g.Tail.endWidth=.005f;g.Tail.startColor=new Color(.65f,1,.3f,.6f);g.Tail.endColor=new Color(.3f,.7f,.1f,0);g.Tail.SetPosition(0,p);g.Tail.SetPosition(1,p-direction*.35f);
        }
        void ReturnGrenadeLine(LineRenderer line){line.gameObject.SetActive(false);pool.Push(line);}
        public void EndGrenade(uint id)
        {for(int i=grenades.Count-1;i>=0;i--)if(grenades[i].Id==id){ReturnGrenadeLine(grenades[i].Body);ReturnGrenadeLine(grenades[i].Tail);grenades.RemoveAt(i);}}
        public void ShowGrenadeBurst(uint id,Vector2 point)
        {
            if(aim==null)aim=GetComponent<RightFacingAimRig>();
            EndGrenade(id);
            LineRenderer Blob(int order){var line=AcquireVisual();line.numCapVertices=8;line.sortingOrder=order;return line;}
            var blast=new GrenadeBlast{Ring=AcquireVisual(),Core=Blob(34),Fire=Blob(33),Smoke=new LineRenderer[6],Point=point,Began=Time.time};
            blast.Ring.positionCount=49;blast.Ring.sortingOrder=32;
            for(int i=0;i<blast.Smoke.Length;i++)blast.Smoke[i]=Blob(28);
            grenadeBlasts.Add(blast);DrawGrenadeBlast(blast,0);
            for(int i=0;i<12;i++)
            {float a=(i+.5f)*Mathf.PI/6;AddSpark(point,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*5,.32f,.55f,.11f,false,new Color(.82f,1,.5f),new Color(.2f,.8f,.05f,0));}
        }
        void DrawGrenadeBlast(GrenadeBlast blast,float age)
        {
            void Blob(LineRenderer line,Vector2 at,float radius,Color color)
            {
                line.startWidth=line.endWidth=radius*2;line.startColor=line.endColor=color;
                line.SetPosition(0,new Vector3(at.x-.01f,at.y,aim.MuzzlePosition.z));line.SetPosition(1,new Vector3(at.x+.01f,at.y,aim.MuzzlePosition.z));
            }
            float flash=Mathf.Clamp01(1-age/.11f),fire=Mathf.Clamp01(1-age/.25f);
            Blob(blast.Core,blast.Point,.6f*flash,new Color(.88f,1,.72f,flash));
            Blob(blast.Fire,blast.Point,Mathf.Lerp(.5f,1f,Mathf.Clamp01(age/.25f)),new Color(.45f,1,.12f,fire*.72f));
            float ring=Mathf.Clamp01(age/.34f),radius=ArenaGrenadeBallistics.BlastRadius*Mathf.Lerp(.15f,1,1-(1-ring)*(1-ring));
            blast.Ring.startWidth=blast.Ring.endWidth=.15f*(1-ring);blast.Ring.startColor=blast.Ring.endColor=new Color(.65f,1,.25f,1-ring);
            for(int j=0;j<=48;j++){float a=j*Mathf.PI/24;blast.Ring.SetPosition(j,(Vector3)(blast.Point+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius));}
            float smoke=Mathf.Clamp01((age-.04f)/.46f),alpha=Mathf.Sin(smoke*Mathf.PI)*.23f;
            for(int i=0;i<blast.Smoke.Length;i++)
            {
                float a=(i+.5f)*Mathf.PI/3;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                Vector2 at=blast.Point+d*Mathf.Lerp(.12f,1.05f,smoke)+Vector2.up*(smoke*.3f);
                // Uneven green lobes break up the plain disc before fading to smoke.
                float hot=Mathf.Clamp01(1-age/.22f);
                var color=Color.Lerp(new Color(.18f,.25f,.18f,alpha),new Color(.5f,1,.12f,.6f),hot);
                Blob(blast.Smoke[i],at,Mathf.Lerp(.12f,.48f,smoke)*(i%2==0?1: .8f),color);
            }
        }
        void ReleaseGrenadeBlast(GrenadeBlast blast)
        {ReturnGrenadeLine(blast.Ring);ReturnGrenadeLine(blast.Core);ReturnGrenadeLine(blast.Fire);foreach(var line in blast.Smoke)ReturnGrenadeLine(line);}
        void UpdateGrenades()
        {
            var match=ArenaObjectiveMatch.Instance;
            for(int i=grenades.Count-1;i>=0;i--)
            {var g=grenades[i];if(match==null||!match.IsSpawned||match.State.Value.Version!=g.Version||ArenaObjectiveMatch.CombatBlocked||NetworkManager.Singleton.ServerTime.Time-g.Began>ArenaGrenadeBallistics.MaximumLife+.5)EndGrenade(g.Id);else DrawGrenade(g);}
            for(int i=grenadeBlasts.Count-1;i>=0;i--)
            {
                var blast=grenadeBlasts[i];float age=Time.time-blast.Began;
                if(age>=GrenadeBlastVisualSeconds||ArenaObjectiveMatch.TransitionActive){ReleaseGrenadeBlast(blast);grenadeBlasts.RemoveAt(i);continue;}
                DrawGrenadeBlast(blast,age);
            }
        }
        void ClearGrenades(){foreach(var g in grenades){Destroy(g.Body.gameObject);Destroy(g.Tail.gameObject);}grenades.Clear();foreach(var b in grenadeBlasts){Destroy(b.Ring.gameObject);Destroy(b.Core.gameObject);Destroy(b.Fire.gameObject);foreach(var line in b.Smoke)Destroy(line.gameObject);}grenadeBlasts.Clear();}
    }
}
