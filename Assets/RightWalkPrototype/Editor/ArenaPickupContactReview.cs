using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using RightWalkPrototype;

[InitializeOnLoad]
public static class ArenaPickupContactReview
{
    const string Key="ArenaPickupContactReview",Folder="LocalTools/Analysis/PickupContact";
    static ArenaPickupContactReview(){EditorApplication.update+=Tick;}
    public static void Begin(){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING\n");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    static void Tick()
    {
        if(!Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor||!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||Time.time<.5f)return;
        var s=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();if(s==null)return;
        SessionState.SetBool(Key,false);s.StartCoroutine(Guard(Run(s)));
    }
    static IEnumerator Guard(IEnumerator work)
    {
        while(true)
        {
            bool next=false;object current=null;Exception error=null;
            try{next=work.MoveNext();if(next)current=work.Current;}catch(Exception e){error=e;}
            if(error!=null){File.AppendAllText(Folder+"/result.txt","FAIL: "+error);yield break;}
            if(!next)break;yield return current;
        }
        File.AppendAllText(Folder+"/result.txt","PASS\n");
    }
    static void Check(bool value,string label){if(!value)throw new Exception(label);File.AppendAllText(Folder+"/result.txt",label+"\n");}
    static Bounds Body(SideScrollNetworkPlayer p)=>(Bounds)typeof(SideScrollNetworkPlayer).GetProperty("ArenaHitBounds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(p);
    static void Grenades(SideScrollNetworkPlayer p,int n)=>((NetworkVariable<int>)typeof(SideScrollNetworkPlayer).GetField("grenadeRounds",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(p)).Value=n;
    static IEnumerator Run(SideScrollNetworkSession session)
    {
        var body=new Bounds(new Vector3(0,1.1f,0),new Vector3(.5f,2.2f,.2f));
        foreach(bool healing in new[]{false,true})
        {
            Check(ArenaObjectiveMatch.PickupBodyContact(body,new Vector2(0,2.3f),healing,0),"upper body contact "+healing);
            Check(ArenaObjectiveMatch.PickupBodyContact(body,new Vector2(0,-.1f),healing,0),"foot contact "+healing);
            Check(!ArenaObjectiveMatch.PickupBodyContact(body,new Vector2(0,2.7f),healing,0),"separated above head rejected "+healing);
            Check(!ArenaObjectiveMatch.PickupBodyContact(body,new Vector2(.8f,1.1f),healing,0),"separated sideways rejected "+healing);
        }
        var crouched=new Bounds(new Vector3(0,.75f,0),new Vector3(.5f,1.5f,.2f));
        Check(!ArenaObjectiveMatch.PickupBodyContact(crouched,new Vector2(0,2),true,0),"crouched head height respected");
        session.StartSoloPractice();yield return new WaitForSeconds(.5f);
        int ledges=0;foreach(var c in UnityEngine.Object.FindObjectsByType<PlatformEffector2D>(FindObjectsSortMode.None))if(c.name.StartsWith("Jump ledge"))ledges++;
        Check(ledges==4,"waiting room retains only two levels per side");
        ScreenCapture.CaptureScreenshot(Folder+"/waiting-room.png");yield return new WaitForSeconds(.2f);
        var match=ArenaObjectiveMatch.Instance;match.SetLocalReady(true);yield return new WaitForSeconds(.2f);Check(match.StartRound(),"start solo arena");
        float end=Time.time+25;while(match.State.Value.Phase!=ArenaObjectiveMatch.Stage.Playing&&Time.time<end)yield return null;
        Check(match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing,"arena ready");yield return new WaitForSeconds(.3f);
        var p=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>();p.GetComponent<SideScrollMapPreview>().SetInput(0);
        var centre=(Vector2)Body(p).center+Vector2.up*.4f;
        Check(Vector2.Distance((Vector2)p.transform.position+Vector2.up*.65f,centre)>.65f,"fixture is outside old foot-centred pickup radius");
        Grenades(p,0);match.AmmoPickups.Add(new ArenaObjectiveMatch.AmmoPickup{Id=999001,Ammo=ArenaObjectiveMatch.SupplyAmmo.Grenade,Amount=1,Position=centre+Vector2.right*1.5f,Available=true,ExpiresAt=match.Now+10});
        yield return new WaitForSeconds(.2f);Check(p.GrenadeRounds==0,"no remote collection");
        var item=match.AmmoPickups[match.AmmoPickups.Count-1];item.Position=centre;match.AmmoPickups[match.AmmoPickups.Count-1]=item;
        yield return new WaitForSeconds(.2f);Check(p.GrenadeRounds==1,"upper body touching ammo grants one round");
        p.ReceiveServerDamage(40,p.LifeVersion);int hp=p.Health;
        match.HealthPickups.Add(new ArenaObjectiveMatch.AmmoPickup{Id=999002,Amount=25,Position=centre,Available=true,ExpiresAt=match.Now+10});
        yield return new WaitForSeconds(.2f);Check(p.Health==hp+25,"upper body touching health grants 25");
        match.HealthPickups.Add(new ArenaObjectiveMatch.AmmoPickup{Id=999003,Amount=50,Shield=true,Position=centre,Available=true,ExpiresAt=match.Now+10});
        yield return new WaitForSeconds(.2f);Check(p.Shield==50,"upper body touching shield grants 50");
        Grenades(p,SideScrollNetworkPlayer.GrenadeCapacity);
        match.AmmoPickups.Add(new ArenaObjectiveMatch.AmmoPickup{Id=999004,Ammo=ArenaObjectiveMatch.SupplyAmmo.Grenade,Amount=1,Position=centre,Available=true,ExpiresAt=match.Now+10});
        yield return new WaitForSeconds(.2f);bool remains=false;foreach(var a in match.AmmoPickups)remains|=a.Id==999004&&a.Available;
        Check(remains,"full ammo leaves pickup available");
        // Fixed display row, separate from the body-contact tests above.
        var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
        var sizeIndex=view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);int savedIndex=(int)sizeIndex.GetValue(view);
        view.GetType().GetMethod("SetCustomResolution",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,new object[]{new Vector2(1366,768),"Pickup art 1366"});view.Focus();
        for(int i=0;i<match.Patrols.Count;i++){var e=match.Patrols[i];e.Health=0;e.RespawnAt=match.Now+600;match.Patrols[i]=e;}match.TurretShots.Clear();
        var transit=match.MedicalTransit.Value;transit.NextAt=match.Now+600;match.MedicalTransit.Value=transit;
        float floor=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>().floorSpacing;
        p.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(-.7f,floor+.04f));
        for(int i=0;i<3;i++)match.AmmoPickups.Add(new ArenaObjectiveMatch.AmmoPickup{Id=999100+i,Ammo=(ArenaObjectiveMatch.SupplyAmmo)i,Amount=1,Position=new Vector2(-3.4f+i*1.7f,floor+3.3f),Available=true,ExpiresAt=match.Now+60});
        match.HealthPickups.Add(new ArenaObjectiveMatch.AmmoPickup{Id=999103,Amount=25,Position=new Vector2(1.7f,floor+3.3f),Available=true,ExpiresAt=match.Now+60});
        match.HealthPickups.Add(new ArenaObjectiveMatch.AmmoPickup{Id=999104,Amount=50,Shield=true,Position=new Vector2(3.4f,floor+3.3f),Available=true,ExpiresAt=match.Now+60});
        yield return new WaitForSeconds(1);
        int styled=0;foreach(var r in match.GetComponentsInChildren<SpriteRenderer>())if(r.name=="Pickup artwork")styled++;
        Check(styled>=5,"five pickup kinds use upgraded artwork");
        yield return new WaitForEndOfFrame();var screenshot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"/pickup-art.png",screenshot.EncodeToPNG());UnityEngine.Object.Destroy(screenshot);
        p.GetComponent<SideScrollMapPreview>().SetSpawnPosition(new Vector2(-29.2f,.04f));yield return new WaitForSeconds(1);
        yield return new WaitForEndOfFrame();screenshot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"/passable-case.png",screenshot.EncodeToPNG());UnityEngine.Object.Destroy(screenshot);
        sizeIndex.SetValue(view,savedIndex);session.DisconnectLocal();yield return new WaitForSeconds(.3f);
    }
}
