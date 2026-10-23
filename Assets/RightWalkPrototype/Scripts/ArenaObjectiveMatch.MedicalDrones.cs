using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        public struct MedicalWindow:INetworkSerializable,IEquatable<MedicalWindow>
        {
            public bool Valid;public Vector2 Entry,Exit;public double NextAt;
            public void NetworkSerialize<T>(BufferSerializer<T> s) where T:IReaderWriter
            {s.SerializeValue(ref Valid);s.SerializeValue(ref Entry);s.SerializeValue(ref Exit);s.SerializeValue(ref NextAt);}
            public bool Equals(MedicalWindow o)=>Valid==o.Valid&&Entry==o.Entry&&Exit==o.Exit&&NextAt==o.NextAt;
        }
        public readonly NetworkVariable<MedicalWindow> MedicalTransit=new();
        [SerializeField,Min(1)] int medicalDroneHealth=8;
        [SerializeField,Min(.1f)] float medicalDroneSpeed=3.2f,medicalFirstMinimum=10,medicalFirstMaximum=16,
            medicalMinimumInterval=25,medicalMaximumInterval=40,medicalWarningSeconds=3,medicalMaximumFlightSeconds=35;
        sealed class MedicalTrip
        {
            public int Id,Step;public bool Entered;public Vector2 OutsideStart,OutsideExit,InnerExit;
            public List<Vector2> Points=new();public double Began,Tick,RepathAt;
        }
        MedicalTrip activeMedicalTrip,plannedMedicalTrip;
        int medicalVersion=-1;
        public bool MedicalInsideArena(Vector2 position)
        {
            var map=RelayArenaPrototype.Instance!=null?RelayArenaPrototype.Instance.GetComponent<ArenaMapSettings>():null;
            return map!=null&&position.x>map.cameraBounds.xMin+1&&position.x<map.cameraBounds.xMax-1&&position.y>=map.cameraBounds.yMin&&position.y<=map.cameraBounds.yMax;
        }
        void ResetMedicalDrones()
        {
            medicalVersion=-1;activeMedicalTrip=plannedMedicalTrip=null;
            if(MedicalTransit.Value.NextAt!=0)MedicalTransit.Value=default;
        }
        MedicalTrip PlanMedicalTrip(ArenaMapSettings map)
        {
            if(flightNavigation==null)flightNavigation=new ArenaEnemyNavigation(map,true);
            var graph=flightNavigation;if(graph.Nodes.Count==0)return null;
            var heights=new List<float>{1.6f};foreach(var point in Points)heights.Add(point.CaptureBounds.min.y+1.5f);
            int offset=UnityEngine.Random.Range(0,heights.Count),direction=UnityEngine.Random.value<.5f?1:-1;
            var area=map.cameraBounds;
            for(int i=0;i<heights.Count;i++)
            {
                float y=heights[(offset+i)%heights.Count];
                Vector2 wantEntry=new Vector2(direction>0?area.xMin+1.5f:area.xMax-1.5f,y);
                Vector2 wantExit=new Vector2(direction>0?area.xMax-1.5f:area.xMin+1.5f,y);
                int first=graph.Nearest(wantEntry);var entry=graph.Nodes[first].Point;
                if(Mathf.Abs(entry.x-wantEntry.x)>2)continue;
                var route=graph.Route(entry,wantExit,false);if(route.Count<2)continue;
                Vector2 end=graph.Nodes[route[route.Count-1]].Point;
                if(Mathf.Abs(end.x-wantExit.x)>2||(end.x-entry.x)*direction<area.width-7)continue;
                var trip=new MedicalTrip{OutsideStart=new Vector2(direction>0?area.xMin-2:area.xMax+2,entry.y),
                    OutsideExit=new Vector2(direction>0?area.xMax+2:area.xMin-2,end.y),InnerExit=end};
                foreach(int node in route)trip.Points.Add(graph.Nodes[node].Point);trip.Points.Add(trip.OutsideExit);
                float distance=Vector2.Distance(trip.OutsideStart,entry);for(int n=1;n<trip.Points.Count;n++)distance+=Vector2.Distance(trip.Points[n-1],trip.Points[n]);
                if(distance/Mathf.Max(.1f,medicalDroneSpeed)<medicalMaximumFlightSeconds-1)return trip;
            }
            return null;
        }
        void ScheduleMedical(ArenaMapSettings map,bool first)
        {
            plannedMedicalTrip=PlanMedicalTrip(map);
            float minimum=Mathf.Max(medicalWarningSeconds,first?medicalFirstMinimum:medicalMinimumInterval);
            float maximum=Mathf.Max(minimum,first?medicalFirstMaximum:medicalMaximumInterval);
            MedicalTransit.Value=plannedMedicalTrip==null?new MedicalWindow{NextAt=Now+5}:new MedicalWindow{
                Valid=true,Entry=plannedMedicalTrip.Points[0],Exit=plannedMedicalTrip.InnerExit,NextAt=Now+UnityEngine.Random.Range(minimum,maximum)};
        }
        void FinishMedicalTrip()
        {
            activeMedicalTrip=null;var window=MedicalTransit.Value;
            if(window.Valid&&window.NextAt<Now+medicalWarningSeconds){window.NextAt=Now+medicalWarningSeconds;MedicalTransit.Value=window;}
        }
        void UpdateMedicalSchedule()
        {
            var map=RelayArenaPrototype.Instance!=null?RelayArenaPrototype.Instance.GetComponent<ArenaMapSettings>():null;
            if(map==null||!map.medicalSupplyDrones){ResetMedicalDrones();return;}
            if(medicalVersion!=State.Value.Version){ResetMedicalDrones();medicalVersion=State.Value.Version;ScheduleMedical(map,true);}
            if(activeMedicalTrip!=null)
            {
                bool alive=false;foreach(var e in Patrols)if(e.Id==activeMedicalTrip.Id&&e.Health>0){alive=true;break;}
                if(!alive)FinishMedicalTrip();
            }
            if(Now<MedicalTransit.Value.NextAt||activeMedicalTrip!=null)return;
            if(plannedMedicalTrip==null){ScheduleMedical(map,false);return;}
            activeMedicalTrip=plannedMedicalTrip;activeMedicalTrip.Id=++nextPatrolId;activeMedicalTrip.Began=activeMedicalTrip.Tick=Now;
            Patrols.Add(new PatrolState{Id=activeMedicalTrip.Id,Generation=1,Health=medicalDroneHealth,Medical=true,Flying=true,Roaming=true,
                Position=activeMedicalTrip.OutsideStart,Speed=medicalDroneSpeed,Started=Now});
            plannedMedicalTrip=null;ScheduleMedical(map,false);
        }
        bool UpdateMedicalFlight(ref PatrolState state)
        {
            var trip=activeMedicalTrip;if(trip==null||trip.Id!=state.Id)return false;
            if(Now-trip.Began>=Mathf.Max(1,medicalMaximumFlightSeconds)){FinishMedicalTrip();return false;}
            if(Now-trip.Tick<.1)return true;
            float dt=Mathf.Min(.2f,(float)(Now-trip.Tick));trip.Tick=Now;
            Vector2 before=state.Position;float budget=state.Speed*dt;
            while(trip.Step<trip.Points.Count&&budget>0)
            {
                Vector2 target=trip.Points[trip.Step];bool leaving=trip.Step==trip.Points.Count-1;
                if(trip.Entered&&!leaving&&!flightNavigation.CanTraverse(new ArenaEnemyNavigation.Node(state.Position,Vector2.up),new ArenaEnemyNavigation.Node(target,Vector2.up)))
                {
                    if(Now>=trip.RepathAt)
                    {
                        trip.RepathAt=Now+.6;flightNavigation.Block(flightNavigation.Nearest(state.Position),flightNavigation.Nearest(target));
                        var route=flightNavigation.Route(state.Position,trip.InnerExit,false);
                        if(route.Count>1&&Vector2.Distance(flightNavigation.Nodes[route[route.Count-1]].Point,trip.InnerExit)<1)
                        {trip.Points.Clear();foreach(int n in route)trip.Points.Add(flightNavigation.Nodes[n].Point);trip.Points.Add(trip.OutsideExit);trip.Step=0;}
                    }
                    break;
                }
                float distance=Vector2.Distance(state.Position,target);bool reached=distance<=budget+.001f;
                state.Position=Vector2.MoveTowards(state.Position,target,budget);budget-=distance;
                if(!reached)break;
                trip.Step++;trip.Entered=true;
            }
            state.Velocity=(state.Position-before)/dt;state.Started=Now;
            if(trip.Step<trip.Points.Count)return true;
            FinishMedicalTrip();return false;
        }
        void DrawMedicalGuide()
        {
            if(State.Value.Phase!=Stage.Playing||RelayArenaPrototype.Instance!=null&&RelayArenaPrototype.Instance.MenuOpen||Camera.main==null)return;
            var window=MedicalTransit.Value;bool approaching=window.Valid&&window.NextAt>Now&&window.NextAt-Now<=medicalWarningSeconds;
            Vector2 world=window.Entry;bool exists=approaching;
            if(!approaching)foreach(var drone in Patrols)if(drone.Medical&&drone.Health>0){world=drone.Position+Vector2.up*.5f;exists=true;break;}
            if(!exists)return;
            var p=Camera.main.WorldToScreenPoint(world);
            if(!approaching&&p.z>0&&p.x>50&&p.x<Screen.width-50&&p.y>90&&p.y<Screen.height-90)return;
            var screen=new Vector2(p.x,Screen.height-p.y);var delta=screen-new Vector2(Screen.width*.5f,Screen.height*.5f);
            string arrow=Mathf.Abs(delta.x)>=Mathf.Abs(delta.y)?delta.x<0?"◀":"▶":delta.y<0?"▲":"▼";
            float minY=Mathf.Min(340,Screen.height*.45f),maxY=Mathf.Max(minY,Screen.height-200);
            float x=Mathf.Clamp(screen.x,90,Screen.width-90),y=Mathf.Clamp(screen.y,minY,maxY);
            var rect=new Rect(x-80,y-14,160,28);var old=GUI.color;GUI.color=new Color(.02f,.1f,.05f,.9f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;
            GUI.Label(rect,arrow+" "+(approaching?"보호막 드론 접근":"보호막 드론"),new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=14,normal={textColor=new Color(.4f,1,.6f)}});
        }
    }
}
