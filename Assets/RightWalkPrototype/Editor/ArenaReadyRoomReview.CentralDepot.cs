using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static partial class ArenaReadyRoomReview
{
    [MenuItem("Tools/Right Walk Prototype/Verify Central Typed Depot")]
    public static void BeginCentralDepot(){BeginFoundry();File.WriteAllText(folder+"/central-depot.txt","depot");}
    static string clearanceDiagnostic;
    [MenuItem("Tools/Right Walk Prototype/Verify Depot Slope Clearance")]
    public static void BeginDepotSlope(){BeginCentralDepot();File.WriteAllText(folder+"/depot-slope-only.txt","slope");}
    static Dictionary<int,GameObject> AmmoViews(ArenaObjectiveMatch m)=>(Dictionary<int,GameObject>)typeof(ArenaObjectiveMatch).GetField("pickupVisuals",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(m);
    static bool NoQuantityLabels(ArenaObjectiveMatch m)
    {foreach(var view in AmmoViews(m).Values)if(view.transform.Find("Amount")!=null)return false;return true;}
    static bool CaseClearsFloor(ArenaObjectiveMatch m,ArenaObjectiveMatch.AmmoPickup item)
    {
        if(!AmmoViews(m).TryGetValue(item.Id,out var view)){clearanceDiagnostic="no view";return false;}
        // Probe the bottom of the whole case. Probing its top rim can start inside
        // an overhead one-way deck and report that ceiling as a zero-distance floor.
        var sprites=view.GetComponentsInChildren<SpriteRenderer>();if(sprites.Length==0)return false;
        var b=sprites[0].bounds;foreach(var sprite in sprites)b.Encapsulate(sprite.bounds);
        foreach(float x in new[]{b.min.x,b.center.x,b.max.x})
            if(ArenaEnemyNavigation.FloorBelow(new Vector2(x,b.min.y+.02f),1,out var floor)&&b.min.y<floor.point.y+.005f){clearanceDiagnostic="case bottom="+b.min.y+" floor="+floor.point+" collider="+floor.collider.name+" item="+item.Position;return false;}
        return true;
    }
    static IEnumerator CheckDepotSlope(ArenaObjectiveMatch m)
    {
        m.SpawnSpecialAmmoDropServer(new Vector2(-8,3.2f),ArenaObjectiveMatch.SupplyAmmo.Grenade);int slope=m.AmmoPickups.Count-1;
        double deadline=m.Now+3;
        while(m.Now<deadline&&(m.AmmoPickups[slope].Falling||!CaseClearsFloor(m,m.AmmoPickups[slope])))yield return null;
        File.WriteAllText(folder+"/slope-diagnostic.txt","position="+m.AmmoPickups[slope].Position+" falling="+m.AmmoPickups[slope].Falling+" reason="+clearanceDiagnostic);
        Require(!m.AmmoPickups[slope].Falling&&CaseClearsFloor(m,m.AmmoPickups[slope]),"slope case clears terrain at both bottom corners: "+clearanceDiagnostic);
        // Observe a full bob cycle, including its lowest visual point.
        bool clear=true;double until=m.Now+2.2;while(m.Now<until){clear&=CaseClearsFloor(m,m.AmmoPickups[slope]);yield return null;}
        Require(clear,"sloped case remains clear through its entire bob cycle");
        Send(1,109);yield return Wait(()=>Mathf.Abs(P(1).position.x+7)<.1f,"host views slope case");yield return new WaitForSecondsRealtime(.25f);ScreenCapture.CaptureScreenshot(folder+"/slope-case-clearance.png");yield return null;
    }
    static IEnumerator CheckCentralDepot()
    {
        yield return Wait(()=>All(4,p=>p.connected&&p.phase==0&&p.roster==4),"four peers waiting",45);
        var m=ArenaObjectiveMatch.Instance;var map=Object.FindFirstObjectByType<ArenaMapSettings>();
        Require(map.shotgunSupplies.Length==0&&map.ammoSupplies.Length==3,"old map shotgun sources removed and replaced by three typed slots");
        foreach(var supply in map.ammoSupplies)Require(Mathf.Abs(supply.position.x)<=1.11f&&Mathf.Abs(supply.position.y-4.02f)<.01f,"fixed supply remains inside central depot");
        Send(0,1,true);yield return Wait(()=>All(4,p=>p.mask==15),"everyone ready");m.roundSeconds=120;Require(m.StartRound(),"start central depot match");
        yield return Wait(()=>All(4,p=>p.phase==1&&p.pickupsCount==3&&p.supplyS==1&&p.supplyR==1&&p.supplyG==1),"S R G appear simultaneously on every peer");
        for(int i=0;i<m.Patrols.Count;i++){var e=m.Patrols[i];e.Health=0;e.RespawnAt=m.Now+300;m.Patrols[i]=e;}
        var transit=m.MedicalTransit.Value;transit.NextAt=m.Now+300;m.MedicalTransit.Value=transit;
        if(File.Exists(folder+"/depot-slope-only.txt")){yield return CheckDepotSlope(m);yield break;}
        yield return Wait(()=>AmmoViews(m).Count==3,"depot views created");
        Require(NoQuantityLabels(m),"ammo cases have initials without quantity text");
        foreach(var item in m.AmmoPickups)Require(item.Amount==1&&item.Respawn==15&&item.ExpiresAt==0&&CaseClearsFloor(m,item),"fixed case gives one round and clears terrain");
        Send(1,106);yield return Wait(()=>Mathf.Abs(P(1).position.x-4.6f)<.1f,"host camera views expanded three-slot supply area");
        yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(folder+"/central-srg.png");yield return null;
        Send(2,104);yield return Wait(()=>Mathf.Abs(P(2).position.x+1.3f)<.1f,"full client reaches shotgun slot");yield return new WaitForSecondsRealtime(.3f);Require(P(2).ammo==6&&m.AmmoPickups[0].Available,"full ammunition preserves supply");
        SetSupplyAmmo(Player(2),ArenaObjectiveMatch.SupplyAmmo.Shotgun,0);SetSupplyAmmo(Player(2),ArenaObjectiveMatch.SupplyAmmo.Rifle,0);SetSupplyAmmo(Player(2),ArenaObjectiveMatch.SupplyAmmo.Grenade,0);
        yield return Wait(()=>P(2).ammo==1&&P(2).rifleAmmo==0&&P(2).grenadeAmmo==0&&All(4,p=>p.pickupsAvailable==2),"shotgun slot grants only one shotgun round");
        Send(2,102);yield return Wait(()=>P(2).rifleAmmo==1&&All(4,p=>p.pickupsAvailable==1),"rifle slot grants one rifle round independently");
        Send(2,103);yield return Wait(()=>P(2).grenadeAmmo==1&&All(4,p=>p.pickupsAvailable==0),"client can collect all three one-round supplies");
        foreach(var item in m.AmmoPickups)Require(item.ReadyAt>m.Now+12,"each used slot has regeneration delay");
        Send(2,82);yield return Wait(()=>P(2).position.x>31,"client leaves regeneration area");
        yield return Wait(()=>All(4,p=>p.pickupsAvailable==3&&p.supplyS==1&&p.supplyR==1&&p.supplyG==1),"all three slots regenerate after their fifteen-second cooldowns",18);
        Send(1,102);yield return Wait(()=>Mathf.Abs(P(1).position.x)<.1f,"host overlaps full rifle slot");
        SetSupplyAmmo(Player(2),ArenaObjectiveMatch.SupplyAmmo.Rifle,3);Send(2,102);yield return Wait(()=>Mathf.Abs(P(2).position.x)<.1f,"client overlaps full rifle slot");
        SetSupplyAmmo(Player(1),ArenaObjectiveMatch.SupplyAmmo.Rifle,0);SetSupplyAmmo(Player(2),ArenaObjectiveMatch.SupplyAmmo.Rifle,0);
        yield return Wait(()=>P(1).rifleAmmo+P(2).rifleAmmo==1&&All(4,p=>p.pickupsAvailable==2),"simultaneous contact grants exactly one rifle round");
        Require(Player(1).RifleRounds==1&&Player(2).RifleRounds==0,"server selects one stable recipient");
        Send(0,82);yield return Wait(()=>All(4,p=>p.position.x>31),"players leave drop-clearance tests");
        Require(m.SpawnSpecialAmmoDropServer(new Vector2(0,6),ArenaObjectiveMatch.SupplyAmmo.Rifle,99)&&m.SpawnSpecialAmmoDropServer(new Vector2(1.3f,6),ArenaObjectiveMatch.SupplyAmmo.Grenade,3)&&m.SpawnShotgunDropServer(new Vector2(-1.3f,6),3),"typed and old drop APIs use common one-round policy");
        yield return Wait(()=>m.AmmoPickups.Count==6&&!m.AmmoPickups[3].Falling&&!m.AmmoPickups[4].Falling&&!m.AmmoPickups[5].Falling,"all dropped cases settle on flat floor",4);
        foreach(var item in m.AmmoPickups)Require(item.Amount==1&&CaseClearsFloor(m,item),"every ammunition source gives one and stays above floor");
        Require(NoQuantityLabels(m),"dropped cases also omit quantity labels");
        yield return CheckDepotSlope(m);
        var state=m.State.Value;state.End=m.Now+.05;m.State.Value=state;yield return Wait(()=>All(4,p=>p.phase==2&&p.pickupsCount==0),"finish clears fixed supplies and drops");Require(m.ReturnToRoom(),"return after depot test");yield return Wait(()=>All(4,p=>p.phase==0&&p.ammo==6&&p.rifleAmmo==3&&p.grenadeAmmo==SideScrollNetworkPlayer.GrenadeCapacity),"ready room returns full practice ammunition");
    }
}
