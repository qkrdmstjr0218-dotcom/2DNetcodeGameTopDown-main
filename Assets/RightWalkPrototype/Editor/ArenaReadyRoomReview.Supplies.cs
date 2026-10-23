using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify General Supplies")]
    public static void BeginGeneralSupplies(){BeginFoundry();File.WriteAllText(folder+"/supplies.txt","supplies");}
    static void SupplyCommand(SideScrollNetworkPlayer own,int action)
    {own.GetComponent<SideScrollMapPreview>().SetSpawnPosition(action==105?new Vector2(18,.04f):new Vector2(action==103?1.3f:action==104?-1.3f:action==106?4.6f:0,3.64f));}
    static void SetSupplyAmmo(SideScrollNetworkPlayer player,ArenaObjectiveMatch.SupplyAmmo kind,int amount)
    {
        string field=kind==ArenaObjectiveMatch.SupplyAmmo.Rifle?"rifleRounds":kind==ArenaObjectiveMatch.SupplyAmmo.Grenade?"grenadeRounds":"instantShotgunRounds";
        ((NetworkVariable<int>)typeof(SideScrollNetworkPlayer).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(player)).Value=amount;
    }
    static void SupplyProbabilities(ArenaObjectiveMatch m,float health,float ammo)
    {var so=new SerializedObject(m);so.FindProperty("patrolHealthDropChance").floatValue=health;so.FindProperty("patrolDropChance").floatValue=ammo;so.ApplyModifiedPropertiesWithoutUndo();}
    static int KillSupplyEnemy(ArenaObjectiveMatch m,int index,int kind)
    {
        var e=m.Patrols[index];e.Health=4;e.Generation++;e.Position=new Vector2(0,6);e.Left=e.Right=0;e.Floor=6;e.RespawnAt=m.Now+300;m.Patrols[index]=e;
        SupplyProbabilities(m,kind<0?1:0,kind>=0&&kind<3?1:0);
        var previous=Random.state;
        if(kind>=0&&kind<3)for(int seed=0;seed<200;seed++){Random.InitState(seed);float roll=Random.value;if(Random.Range(0,3)==kind){Random.InitState(seed);break;}}
        m.DamagePatrolServer(e.Id,e.Generation,4);Random.state=previous;return e.Generation;
    }
    static IEnumerator CheckGeneralSupplies()=>CheckCentralDepot();
}
