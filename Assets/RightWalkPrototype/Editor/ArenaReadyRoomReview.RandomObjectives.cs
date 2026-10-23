using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Random Objectives")]
    public static void BeginRandomObjectives(){BeginFoundry();File.WriteAllText(folder+"/random-objectives.txt","random");}
    static IEnumerator CheckRandomObjectives()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4),"four peers waiting",45);
        var m=ArenaObjectiveMatch.Instance;Require(m.Points.Length==6,"Foundry has six objective candidates");
        var choose=typeof(ArenaObjectiveMatch).GetMethod("PickNextPoint",BindingFlags.NonPublic|BindingFlags.Instance);var previous=Random.state;
        for(int current=0;current<m.Points.Length;current++)
        {
            var seen=new HashSet<int>();bool valid=true;
            for(int seed=0;seed<64;seed++){Random.InitState(seed);int next=(int)choose.Invoke(m,new object[]{current});valid&=next>=0&&next<m.Points.Length&&next!=current;seen.Add(next);}
            Require(valid&&seen.Count==m.Points.Length-1,"all other candidates reachable from "+current);
        }
        Random.state=previous;
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");m.roundSeconds=120;m.pointSeconds=2;m.warningSeconds=.8f;Require(m.StartRound(),"start random objective match");
        yield return Wait(()=>All(4,p=>p.phase==1),"all peers playing");
        for(int i=0;i<m.Patrols.Count;i++){var e=m.Patrols[i];e.Health=0;e.RespawnAt=m.Now+300;m.Patrols[i]=e;}
        Require(m.State.Value.Point>=0&&m.State.Value.Point<6,"first objective chosen from all candidates");
        Send(0,82);yield return Wait(()=>All(4,p=>p.position.x>31),"players leave objective transitions");
        for(int step=0;step<6;step++)
        {
            yield return Wait(()=>All(4,p=>p.objectivePreview),"next objective preview visible on all peers "+step,4);
            int active=m.State.Value.Point,next=m.NextPoint;Require(active!=next,"next site differs from active site "+step);
            Require(All(4,p=>p.objectivePoint==active&&p.objectiveNext==next),"preview site agrees on all peers "+step);
            yield return Wait(()=>All(4,p=>p.objectivePoint==next),"announced site becomes active "+step,3);
            Require(m.NextPoint!=next&&m.NextPoint>=0&&m.NextPoint<6,"following choice excludes the new active site "+step);
        }
        yield return Wait(()=>All(4,p=>p.medicCount==1&&p.medicHealth==8),"shield drone uses eight health",8);
        yield return Wait(()=>MedicIndex(m)>=0&&m.MedicalInsideArena(m.Patrols[MedicIndex(m)].Position),"shield drone enters damageable arena",8);
        int index=MedicIndex(m),id=m.Patrols[index].Id;ArenaPatrolEnemy target=null;
        foreach(var view in Object.FindObjectsByType<ArenaPatrolEnemy>(FindObjectsSortMode.None))if(view.Id==id)target=view;
        Require(target!=null&&target.GetComponent<Collider2D>().enabled,"live drone collider available");
        var bounds=target.GetComponent<Collider2D>().bounds;var centre=(Vector2)bounds.center;
        Require(ArenaGrenadeBallistics.BlastDamage(centre,bounds,SideScrollNetworkPlayer.GrenadeEnemyDamage,SideScrollNetworkPlayer.GrenadeEnemyMinimumDamage)==6,"enemy central blast is six");
        var edge=new Bounds(new Vector3(centre.x+ArenaGrenadeBallistics.BlastRadius,centre.y,0),Vector3.zero);
        Require(ArenaGrenadeBallistics.BlastDamage(centre,edge,SideScrollNetworkPlayer.GrenadeEnemyDamage,SideScrollNetworkPlayer.GrenadeEnemyMinimumDamage)==2,"enemy edge blast is two");
        Require(SideScrollNetworkPlayer.GrenadeDamage==40&&SideScrollNetworkPlayer.GrenadeMinimumDamage==10,"player blast values remain forty to ten");
        var burst=typeof(SideScrollNetworkPlayer).GetMethod("BurstGrenade",BindingFlags.NonPublic|BindingFlags.Instance);int kits=m.HealthPickups.Count;
        // Exercise the real server explosion resolution, without projectile flight/input setup.
        burst.Invoke(Player(2),new object[]{(uint)9001,centre,m});
        yield return Wait(()=>All(4,p=>p.medicHealth==2),"one central explosion leaves two shield-drone health",3);
        centre=target.GetComponent<Collider2D>().bounds.center;burst.Invoke(Player(2),new object[]{(uint)9002,centre,m});
        yield return Wait(()=>All(4,p=>p.medicCount==0)&&m.HealthPickups.Count==kits+1,"second central explosion destroys drone and drops one shield pack",3);
        Require(m.HealthPickups[kits].Shield,"grenade destruction retains shield reward");
        var state=m.State.Value;state.End=m.Now+.05;m.State.Value=state;
        yield return Wait(()=>All(4,p=>p.phase==2),"round ends normally");Require(m.ReturnToRoom(),"return after random objectives");yield return Wait(()=>All(4,p=>p.phase==0),"ready room restored");
    }
}
