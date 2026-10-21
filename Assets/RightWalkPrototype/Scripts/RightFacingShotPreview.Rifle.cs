using System.Collections.Generic;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class RightFacingShotPreview
    {
        sealed class RifleFlight{public LineRenderer Visual,Glow,Tail;public Vector2 Origin,Direction;public float Age,Born,InitialAge;}
        readonly List<RifleFlight> rifles=new();
        public void ShowRifle(Vector2 origin,Vector2 direction,float age)
        {
            if(aim==null)aim=GetComponent<RightFacingAimRig>();
            var flight=new RifleFlight{Glow=AcquireVisual(),Tail=AcquireVisual(),Visual=AcquireVisual(),Origin=origin,Direction=direction.normalized,Age=Mathf.Max(0,age),InitialAge=Mathf.Max(0,age),Born=Time.time};
            flight.Glow.sortingOrder=29;flight.Visual.sortingOrder=31;flight.Glow.numCapVertices=6;flight.Visual.numCapVertices=6;flight.Tail.numCapVertices=2;
            rifles.Add(flight);DrawRifle(flight);EmitRifleMuzzle(origin,direction);aim.KickRecoil(2);
        }
        static readonly Color RifleWhite=new(1,.93f,1),RifleViolet=new(.78f,.3f,1,0);
        void EmitRifleMuzzle(Vector2 point,Vector2 direction)
        {
            var side=new Vector2(-direction.y,direction.x);
            AddSpark(point,direction,.065f,.42f,.13f,true,RifleWhite,RifleViolet);
            AddSpark(point,(direction+side*.7f),.055f,.22f,.06f,true,new Color(.9f,.6f,1),RifleViolet);
            AddSpark(point,(direction-side*.7f),.055f,.22f,.06f,true,new Color(.9f,.6f,1),RifleViolet);
        }
        public void ShowRifleImpact(Vector2 point,Vector2 direction)
        {
            if(aim==null)aim=GetComponent<RightFacingAimRig>();
            var side=new Vector2(-direction.y,direction.x);
            AddSpark(point,direction*.1f,.24f,.6f,.4f,false,RifleWhite,RifleViolet);
            AddSpark(point,side*.1f,.2f,.65f,.14f,false,RifleWhite,RifleViolet);
            AddSpark(point,-side*.1f,.2f,.65f,.14f,false,RifleWhite,RifleViolet);
            for(int i=0;i<8;i++)
            {
                float angle=(22.5f+i*45)*Mathf.Deg2Rad;
                var ray=direction*Mathf.Cos(angle)+side*Mathf.Sin(angle);
                AddSpark(point,ray*2.8f,.32f,.48f,.085f,false,new Color(.95f,.7f,1),RifleViolet);
            }
            AddSpark(point,direction*2.8f,.26f,.65f,.12f,false,RifleWhite,RifleViolet);
        }
        void DrawRifle(RifleFlight flight)
        {
            float distance=Mathf.Min(SideScrollNetworkPlayer.RifleRange,flight.Age*SideScrollNetworkPlayer.RifleSpeed);
            Vector2 head=flight.Origin+flight.Direction*distance;
            void Segment(LineRenderer line,float length,float width,float tailWidth,Color front,Color back)
            {
                line.startWidth=width;line.endWidth=tailWidth;line.startColor=front;line.endColor=back;
                Vector2 end=head-flight.Direction*Mathf.Min(length,distance);
                line.SetPosition(0,new Vector3(head.x,head.y,aim.MuzzlePosition.z));line.SetPosition(1,new Vector3(end.x,end.y,aim.MuzzlePosition.z));
            }
            Segment(flight.Glow,.5f,.18f,.04f,new Color(.8f,.35f,1,.3f),new Color(.65f,.2f,1,0));
            Segment(flight.Tail,2.1f,.055f,.005f,new Color(.85f,.45f,1,.8f),RifleViolet);
            Segment(flight.Visual,.42f,.1f,.02f,RifleWhite,new Color(.95f,.65f,1,.9f));
        }
        void ReleaseRifle(RifleFlight flight)
        {foreach(var line in new[]{flight.Visual,flight.Glow,flight.Tail}){line.gameObject.SetActive(false);pool.Push(line);}}
        void UpdateRifles()
        {
            for(int i=rifles.Count-1;i>=0;i--)
            {
                var flight=rifles[i];flight.Age=flight.InitialAge+Time.time-flight.Born;
                if(ArenaObjectiveMatch.CombatBlocked||flight.Age*SideScrollNetworkPlayer.RifleSpeed>=SideScrollNetworkPlayer.RifleRange)
                {ReleaseRifle(flight);rifles.RemoveAt(i);}else DrawRifle(flight);
            }
        }
        void ClearRifles(){foreach(var flight in rifles)foreach(var line in new[]{flight.Visual,flight.Glow,flight.Tail})if(line!=null)Destroy(line.gameObject);rifles.Clear();}
    }
}
