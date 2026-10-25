using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;

[InitializeOnLoad]
public static class ArenaVisualFollowupReview
{
    const string Key="ArenaVisualFollowupReview",Folder="LocalTools/Analysis/VisualFollowup";
    static readonly List<string> results=new();
    static ArenaVisualFollowupReview(){EditorApplication.update+=Tick;}
    public static void Begin()
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Requires saved edit mode");
        Directory.CreateDirectory(Folder);results.Clear();File.WriteAllText(Folder+"/result.txt","RUNNING");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||Time.time<.5f)return;
        var session=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();if(session==null)return;
        SessionState.SetBool(Key,false);session.StartCoroutine(Guard(Run(session)));
    }
    static IEnumerator Guard(IEnumerator run)
    {
        var stack=new Stack<IEnumerator>();stack.Push(run);
        while(stack.Count>0)
        {
            object value=null;bool next=false;Exception error=null;
            try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){error=e;}
            if(error!=null){File.WriteAllText(Folder+"/result.txt","FAIL "+error+"\n"+string.Join("\n",results));yield break;}
            if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;
        }
        File.WriteAllText(Folder+"/result.txt","PASS\n"+string.Join("\n",results));
    }
    static void Check(bool value,string label){if(!value)throw new Exception(label);results.Add(label);}
    static IEnumerator Frames(int n=3){while(n-->0)yield return null;}
    static IEnumerator Wait(Func<bool> condition,string label,float seconds=24)
    {float until=Time.realtimeSinceStartup+seconds;while(!condition()&&Time.realtimeSinceStartup<until)yield return null;Check(condition(),label);}
    static IEnumerator Capture(string name)
    {yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
    static Vector2 VisibleSize(Sprite sprite)
    {
        // Sprite.bounds includes transparent padding; measure the drawn alpha instead.
        var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));var pixels=image.GetPixels32();int left=image.width,right=0,bottom=image.height,top=0;
        for(int y=0;y<image.height;y++)for(int x=0;x<image.width;x++)if(pixels[y*image.width+x].a>16){left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
        UnityEngine.Object.Destroy(image);return new Vector2(right-left+1,top-bottom+1)/sprite.pixelsPerUnit;
    }
    static IEnumerator Run(SideScrollNetworkSession session)
    {
        var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));var size=game.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);int saved=(int)size.GetValue(game);
        game.GetType().GetMethod("SetCustomResolution",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(game,new object[]{new Vector2(1366,768),"Visual followup 1366"});game.Focus();
        session.StartSoloPractice();yield return Wait(()=>ArenaObjectiveMatch.InReadyRoom,"solo waiting room");var match=ArenaObjectiveMatch.Instance;
        match.SetLocalReady(true);yield return Frames();Check(match.StartRound(),"start accepted");yield return Frames();yield return Capture("transition-start");
        yield return Wait(()=>ArenaObjectiveMatch.CountingDown&&match.State.Value.End>0,"actual countdown");yield return new WaitForSeconds(.35f);yield return Capture("countdown");
        yield return Wait(()=>match.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing,"actual playing");yield return new WaitForSeconds(.9f);
        var player=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkPlayer>();var movement=player.GetComponent<SideScrollMapPreview>();movement.readKeyboard=false;player.GetComponentInChildren<RightFacingShotPreview>().readMouse=false;
        // Freeze simulation only for art/state fixtures below; actual transition above is unmodified.
        match.enabled=false;
        foreach(var view in UnityEngine.Object.FindObjectsByType<ArenaPatrolEnemy>(FindObjectsSortMode.None))view.gameObject.SetActive(false);
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();float floor=map.floorSpacing;movement.SetSpawnPosition(new Vector2(-.7f,floor+.04f));movement.SetInput(0);
        var fixture=new GameObject("Visual review fixture");
        var sprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
        ArenaPatrolEnemy Make(string name,int id,bool support)
        {var go=new GameObject(name);go.transform.SetParent(fixture.transform);var view=go.AddComponent<ArenaPatrolEnemy>();view.Initialize(match,id,sprite,false,support,support);return view;}
        var cargo=Make("Support design specimen",90001,true);var support=new ArenaObjectiveMatch.PatrolState{Id=90001,Generation=1,Health=6,Medical=true,Flying=true,Left=3,Right=3.01f,Floor=floor+2.7f,Position=new Vector2(3,floor+2.7f)};cargo.Apply(support,true,6);
        Check(cargo.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="SupportHull")&&cargo.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="SupportCargo"),"support has airframe and hanging shield cargo");
        Check(cargo.transform.Find("Enemy body/Aimed barrel")==null,"support has no gun");var collider=cargo.GetComponent<BoxCollider2D>();Check(collider.size==new Vector2(.85f,.9f)&&collider.offset==new Vector2(0,.48f),"support collider unchanged");
        var robot=Make("Charge design specimen",90002,false);var state=new ArenaObjectiveMatch.PatrolState{Id=90002,Generation=1,Health=4,Left=-4,Right=-3.99f,Floor=floor,Aim=Vector2.right};robot.Apply(state,true,4);
        var charge=robot.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="GroundCharge");Check(!charge.gameObject.activeSelf,"charge indicator hidden while idle");
        state.RushPhase=1;state.Aim=Vector2.left;robot.Apply(state,true,4);Check(charge.gameObject.activeSelf&&charge.transform.parent.localScale.x<0&&charge.transform.localPosition==Vector3.zero,"charge light integrated and mirrors with front bumper");
        Check(!robot.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Rush direction"),"no floating rush arrow");
        var make=typeof(ArenaObjectiveMatch).GetMethod("MakePickupVisual",BindingFlags.NonPublic|BindingFlags.Instance);
        for(int i=0;i<2;i++)
        {
            var item=new ArenaObjectiveMatch.AmmoPickup{Id=99000+i,Amount=i==0?25:50,Shield=i==1};var visual=(GameObject)make.Invoke(match,new object[]{item,true});visual.transform.SetParent(fixture.transform);visual.transform.position=ArenaObjectiveMatch.PickupDisplayPosition(new Vector2(i==0?4:5.1f,floor+.3f),match.Now,true);
            Check(visual.transform.Find("Amount")==null,"pickup has no amount label: "+i);var art=visual.GetComponentInChildren<SpriteRenderer>();var visible=VisibleSize(art.sprite);Check(Mathf.Abs(visible.x-visible.y)<.004f&&Mathf.Abs(visible.x-.7f)<.02f,"square pickup visual matches contact width: "+i);
        }
        yield return new WaitForSeconds(.4f);
        File.WriteAllLines(Folder+"/renderers.txt",fixture.GetComponentsInChildren<SpriteRenderer>(true).Select(r=>r.name+" parent="+r.transform.parent.name+" enabled="+r.enabled+" active="+r.gameObject.activeInHierarchy+" visible="+r.isVisible+" color="+r.color+" sprite="+r.sprite.name+" at="+r.transform.position+" order="+r.sortingOrder+" shader="+r.sharedMaterial.shader.name));
        yield return Capture("support-pickups-charge");
        state.RushPhase=2;robot.Apply(state,true,4);Check(charge.color.r>.9f&&charge.color.g>.85f,"rush phase has stronger warm charge light");
        int point=Enumerable.Range(0,match.Points.Length).OrderBy(i=>Mathf.Abs(match.Points[i].CaptureBounds.min.y-floor)).First();var site=match.Points[point];movement.SetSpawnPosition(new Vector2(site.CaptureBounds.center.x-.6f,site.CaptureBounds.min.y+.04f));yield return new WaitForSeconds(.3f);
        var before=site.CaptureBounds;var round=match.State.Value;round.Point=point;round.Upcoming=(point+1)%match.Points.Length;round.Warning=match.Now+9;round.Switch=match.Now+12;match.State.Value=round;
        for(int i=0;i<match.Points.Length;i++)match.Points[i].Show(i==point,false,new Color(.35f,1,.65f));
        var fields=match.Points.Select(p=>p.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r=>r.name=="Hacking hologram")).ToArray();
        Check(fields.All(r=>r!=null&&r.sharedMaterial.shader.isSupported),"all six sites have supported hologram presentation");
        Check(fields.All(r=>r.sharedMaterial==fields[0].sharedMaterial&&r.GetComponent<MeshFilter>().sharedMesh==fields[0].GetComponent<MeshFilter>().sharedMesh),"sites share one material and one quad");
        Check(fields.All(r=>r.GetComponentsInChildren<Collider2D>().Length==0)&&match.Points.All(p=>p.area.All(r=>!r.enabled)),"hologram has no collider and replaces old box graphics");
        yield return Capture("site-active");site.Show(false,true,ArenaObjectiveGuide.NextColor);yield return Capture("site-preview");site.Show(true,false,new Color(1,.65f,.25f));yield return Capture("site-contested");
        round=match.State.Value;round.Warning=match.Now-.1;round.Switch=match.Now+2.8;match.State.Value=round;
        bool contains=site.Contains(player),bright=false,dim=false;float min=1,max=0;
        for(int frame=0;frame<50;frame++)
        {
            float pulse=ArenaObjectivePoint.WarningPulse(match);min=Mathf.Min(min,pulse);max=Mathf.Max(max,pulse);
            for(int i=0;i<match.Points.Length;i++)match.Points[i].Show(i==point,false,new Color(.35f,1,.65f));
            if(!bright&&pulse>.92f){yield return Capture("site-warning-bright");bright=true;}if(!dim&&pulse<.36f){yield return Capture("site-warning-dim");dim=true;}yield return null;
        }
        Check(min<.4f&&max>.9f,"current site pulses before relocation");Check(site.CaptureBounds==before&&site.Contains(player)==contains,"pulse does not change capture bounds or membership");
        round=match.State.Value;round.Switch=match.Now+12;match.State.Value=round;Check(ArenaObjectivePoint.WarningPulse(match)==1,"ordinary active site does not blink");round.Switch=match.Now+1;round.End=match.Now+.5;match.State.Value=round;Check(ArenaObjectivePoint.WarningPulse(match)==1,"no relocation blink when round ends first");
        UnityEngine.Object.Destroy(fixture);UnityEngine.Object.Destroy(sprite);match.enabled=true;yield return Wait(()=>ArenaObjectiveMatch.Ended,"finish");Check(match.ReturnToRoom(),"return accepted");yield return Frames();yield return Capture("transition-return");yield return Wait(()=>ArenaObjectiveMatch.InReadyRoom,"return reaches waiting room");
        session.DisconnectLocal();yield return Frames();typeof(SideScrollNetworkSession).GetProperty("OnlineBusy").SetValue(session,true);yield return Frames();yield return Capture("lobby-connecting");typeof(SideScrollNetworkSession).GetProperty("OnlineBusy").SetValue(session,false);size.SetValue(game,saved);
    }
}
