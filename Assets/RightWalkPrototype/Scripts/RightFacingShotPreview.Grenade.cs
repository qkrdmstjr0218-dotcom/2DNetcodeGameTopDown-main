using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class RightFacingShotPreview
    {
        sealed class GrenadeFlight{public uint Id;public LineRenderer Body,Tail;public Vector2 Origin,Velocity;public double Began;public int Version;}
        sealed class GrenadeBlast{public LineRenderer Ring;public Vector2 Point;public float Began;}
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
            EndGrenade(id);var ring=AcquireVisual();ring.positionCount=33;ring.sortingOrder=32;grenadeBlasts.Add(new GrenadeBlast{Ring=ring,Point=point,Began=Time.time});
            AddSpark(point,Vector2.right*.1f,.16f,.45f,.4f,false,new Color(1,1,.8f),new Color(.8f,1,.2f,0));
            for(int i=0;i<10;i++){float a=i*Mathf.PI*.2f;AddSpark(point,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*3,.28f,.3f,.06f,false,new Color(1,.9f,.4f),new Color(.7f,1,.2f,0));}
        }
        void UpdateGrenades()
        {
            var match=ArenaObjectiveMatch.Instance;
            for(int i=grenades.Count-1;i>=0;i--)
            {var g=grenades[i];if(match==null||!match.IsSpawned||match.State.Value.Version!=g.Version||ArenaObjectiveMatch.CombatBlocked||NetworkManager.Singleton.ServerTime.Time-g.Began>ArenaGrenadeBallistics.MaximumLife+.5)EndGrenade(g.Id);else DrawGrenade(g);}
            for(int i=grenadeBlasts.Count-1;i>=0;i--)
            {
                var blast=grenadeBlasts[i];float age=Time.time-blast.Began;
                if(age>=.3f||ArenaObjectiveMatch.TransitionActive){ReturnGrenadeLine(blast.Ring);grenadeBlasts.RemoveAt(i);continue;}
                float fraction=Mathf.Clamp01(age/.3f);blast.Ring.startWidth=blast.Ring.endWidth=.06f*(1-fraction);blast.Ring.startColor=blast.Ring.endColor=new Color(.85f,1,.4f,1-fraction);
                float radius=ArenaGrenadeBallistics.BlastRadius*Mathf.Lerp(.2f,1,fraction);
                for(int j=0;j<=32;j++){float a=j*Mathf.PI/16;blast.Ring.SetPosition(j,(Vector3)(blast.Point+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius));}
            }
        }
        void ClearGrenades(){foreach(var g in grenades){Destroy(g.Body.gameObject);Destroy(g.Tail.gameObject);}grenades.Clear();foreach(var b in grenadeBlasts)Destroy(b.Ring.gameObject);grenadeBlasts.Clear();}
    }
}
