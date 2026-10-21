using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;
public static class RelayFoundryBuilder
{
    public const string ScenePath="Assets/RightWalkPrototype/Scenes/Network/RelayFoundryPrototype.unity";
    static Transform root;static Sprite sprite;
    static readonly Color Steel=new(.20f,.28f,.33f),Solid=new(.96f,.63f,.23f),Pass=new(.25f,.8f,.76f);
    [MenuItem("Tools/Right Walk Prototype/Create Relay Foundry Prototype")]
    public static void Build()
    {
        if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Requires saved edit mode");
        if(System.IO.File.Exists(ScenePath)&&EditorSceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open the Foundry scene explicitly before regenerating its blockout");
        if(!System.IO.File.Exists(ScenePath)){var source=EditorSceneManager.OpenScene(RelayArenaPrototypeBuilder.ScenePath);EditorSceneManager.SaveScene(source,ScenePath,true);}
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var previous=UnityEngine.Object.FindFirstObjectByType<RelayArenaPrototype>();UnityEngine.Object.DestroyImmediate(previous.gameObject);
        sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        root=new GameObject("Foundry 08 - traversal blockout").transform;
        var rules=root.gameObject.AddComponent<RelayArenaPrototype>();rules.arenaCamera=Camera.main;rules.pitLeft=24;rules.pitRight=28;
        var map=root.gameObject.AddComponent<ArenaMapSettings>();
        map.shotgunSupplies=new[]{new Vector2(-27,.55f),new Vector2(27,4.15f),new Vector2(3,7.75f)};
        map.patrolRoutes=new[]{new ArenaMapSettings.PatrolRoute{left=-12,right=-8,floor=0},new ArenaMapSettings.PatrolRoute{left=17,right=21,floor=3.6f}};
        Box("Backdrop",new Vector2(0,6),new Vector2(80,30),new Color(.045f,.065f,.09f),-100);
        for(int x=-32;x<=32;x+=8)
        {
            Box("Recessed bay",new Vector2(x,6),new Vector2(7.7f,19),new Color(.065f,.10f,.135f),-95);
            for(int i=0;i<4;i++)Box("Inset wall panel",new Vector2(x,1.6f+i*3.6f),new Vector2(7.2f,3.1f),new Color(.08f,.125f,.16f),-94);
            Box("Background column",new Vector2(x-3.8f,6),new Vector2(.14f,21),new Color(.11f,.18f,.21f),-90);
        }
        // A full lower route plus three connected upper loops; one optional hazard.
        Deck("Lower west",-36,24,0);Deck("Lower east",28,36,0);
        Deck("Middle west landing",-36,-25,3.6f,true);Deck("Middle west service",-25,-13,3.6f);
        Deck("Middle west hall",-13,-5,3.6f,true);Deck("Middle centre deck",-5,5,3.6f);
        Deck("Middle east hall",5,13,3.6f,true);Deck("Middle east service",13,25,3.6f);Deck("Middle east landing",25,36,3.6f,true);
        Deck("Upper west",-25,-16,7.2f);Deck("Upper west grate",-16,-6,7.2f,true);Deck("Upper core",-6,6,7.2f);
        Deck("Upper east grate",6,16,7.2f,true);Deck("Upper east",16,25,7.2f);
        Deck("Crown west",-10,-3,10.8f);Deck("Crown grate",-3,3,10.8f,true);Deck("Crown east",3,10,10.8f);
        for(int sign=-1;sign<=1;sign+=2)
        {
            Ramp("Outer lower "+sign,new Vector2(sign*35,0),new Vector2(sign*25,3.6f));
            Ramp("Inner lower "+sign,new Vector2(sign*15,0),new Vector2(sign*5,3.6f));
            Ramp("Outer upper "+sign,new Vector2(sign*35,3.6f),new Vector2(sign*25,7.2f));
            Ramp("Inner upper "+sign,new Vector2(sign*16,3.6f),new Vector2(sign*6,7.2f));
            Ramp("Crown access "+sign,new Vector2(sign*23,7.2f),new Vector2(sign*10,10.8f));
            // Staggered steps offer a shorter jumping route; main ramps require no double jump.
            Deck("Jump shortcut lower "+sign,sign<0?-12:9,sign<0?-9:12,1.8f,true);
            Deck("Jump shortcut upper "+sign,sign<0?-15:12,sign<0?-12:15,5.4f,true);
            Cover("Lower outer cover "+sign,sign<0?-28:29,0);
            Cover("Upper cover "+sign,sign*23,7.2f);
        }
        Cover("Upper core cover",0,7.2f);
        Box("Left boundary",new Vector2(-36.25f,7),new Vector2(.5f,22),Steel,0,true);
        Box("Right boundary",new Vector2(36.25f,7),new Vector2(.5f,22),Steel,0,true);
        Box("Hazard bed",new Vector2(26,-3.9f),new Vector2(4,.5f),Steel,0,true);
        Box("Hazard west wall",new Vector2(23.8f,-2),new Vector2(.4f,3.8f),Steel,0,true);
        Box("Hazard east wall",new Vector2(28.2f,-2),new Vector2(.4f,3.8f),Steel,0,true);
        Box("Hazard glow",new Vector2(26,-3.4f),new Vector2(3.8f,.08f),new Color(.9f,.2f,.1f),1);
        rules.bridgeLeft=Box("Bridge west leaf",new Vector2(25,-.09f),new Vector2(2,.18f),Solid,1).transform;
        rules.bridgeRight=Box("Bridge east leaf",new Vector2(27,-.09f),new Vector2(2,.18f),Solid,1).transform;
        rules.bridgeCollider=Box("Bridge collision",new Vector2(26,-.09f),new Vector2(4,.18f),Color.clear,0,true).GetComponent<BoxCollider2D>();
        rules.warningLight=Box("Bridge status",new Vector2(26,2.7f),new Vector2(1.5f,.08f),Pass,-20).GetComponent<SpriteRenderer>();
        Text("Bridge warning","RETRACTING BRIDGE",new Vector2(26,3.05f),.04f,Solid);
        Text("Title","FOUNDRY / 08",new Vector2(0,14.4f),.13f,new Color(.25f,.39f,.44f));
        Text("West sign","WEST / SERVICE",new Vector2(-24,9.5f),.055f,new Color(.24f,.46f,.5f));
        Text("East sign","EAST / TRANSFER",new Vector2(24,9.5f),.055f,new Color(.4f,.36f,.23f));
        Text("Lower sign","LOWER / BYPASS",new Vector2(0,1.7f),.06f,new Color(.20f,.31f,.35f));
        var session=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();session.playerSpawns=new Transform[4];
        var starts=new[]{new Vector2(-31,.04f),new Vector2(31,.04f),new Vector2(-30,3.64f),new Vector2(30,3.64f)};
        for(int i=0;i<4;i++)
        {
            var at=new GameObject("Player "+(i+1)+" combat start").transform;at.SetParent(root);at.position=starts[i];session.playerSpawns[i]=at;
            Box("Spawn pad "+(i+1),starts[i]-Vector2.up*.02f,new Vector2(1.2f,.035f),Pass,3);
        }
        MakeObjectives();Camera.main.transform.position=new Vector3(0,6.5f,-10);Camera.main.orthographicSize=21;
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        FoundryConsoleBuilder.Apply();
        FoundryLowerBypassBuilder.Apply();
        FoundryEntranceCoverBuilder.Apply();
        FoundryTraversalTuningBuilder.Apply();
        FoundryWestBridgeBuilder.Apply();
        Selection.activeGameObject=root.gameObject;
    }
    static GameObject Box(string name,Vector2 at,Vector2 size,Color color,int order,bool solid=false)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=at;go.transform.localScale=new Vector3(size.x,size.y,1);
        var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;
        if(solid)go.AddComponent<BoxCollider2D>();return go;
    }
    static void OneWay(GameObject go)
    {go.GetComponent<BoxCollider2D>().usedByEffector=true;var e=go.AddComponent<PlatformEffector2D>();e.useOneWay=true;e.surfaceArc=160;e.useSideFriction=false;e.useSideBounce=false;}
    static void Edge(GameObject owner,Vector2 a,Vector2 b,bool pass)
    {
        float length=Vector2.Distance(a,b),step=pass?.32f:length;
        for(float x=0;x<length-.001f;x+=step)
        {
            var from=Vector2.Lerp(a,b,x/length);var to=Vector2.Lerp(a,b,Mathf.Min(x+(pass?.2f:length),length)/length);var delta=to-from;
            var line=Box(pass?"Passable dash":"Solid edge",(from+to)*.5f,new Vector2(delta.magnitude,.035f),pass?Pass:Solid,6);
            line.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);line.transform.SetParent(owner.transform,true);
        }
    }
    static void Deck(string name,float left,float right,float top,bool pass=false)
    {
        var go=Box(name,new Vector2((left+right)*.5f,top-.14f),new Vector2(right-left,.28f),Steel,0,true);if(pass)OneWay(go);
        Edge(go,new Vector2(left,top-.01f),new Vector2(right,top-.01f),pass);
        for(float x=left+.3f;x<right;x+=.8f)Box(pass?"Grate opening":"Deck joint",new Vector2(x,top-.11f),new Vector2(pass?.3f:.035f,.075f),new Color(.055f,.095f,.12f),2).transform.SetParent(go.transform,true);
    }
    static void Ramp(string name,Vector2 lower,Vector2 upper)
    {
        if(lower.x>upper.x){var t=lower;lower=upper;upper=t;}
        var delta=upper-lower;float angle=Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg;
        var normal=new Vector2(-delta.normalized.y,delta.normalized.x);
        var go=Box(name,(lower+upper)*.5f-normal*.1f,new Vector2(delta.magnitude+.22f,.2f),Steel,0,true);
        go.transform.rotation=Quaternion.Euler(0,0,angle);OneWay(go);Edge(go,lower,upper,true);
    }
    static void Cover(string name,float x,float floor)
    {
        var go=Box(name,new Vector2(x,floor+.9f),new Vector2(1.2f,1.8f),new Color(.29f,.34f,.36f),2,true);go.AddComponent<ArenaCover>();
        Edge(go,new Vector2(x-.6f,floor+1.8f),new Vector2(x+.6f,floor+1.8f),false);
        Box("Cover inset",new Vector2(x,floor+.9f),new Vector2(.95f,1.25f),Steel,3).transform.SetParent(go.transform,true);
        Box("Cover status",new Vector2(x-.35f,floor+.9f),new Vector2(.05f,.65f),Solid,4).transform.SetParent(go.transform,true);
    }
    static void Text(string name,string value,Vector2 at,float size,Color color)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=at;var t=go.AddComponent<TextMesh>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=value;t.anchor=TextAnchor.MiddleCenter;t.characterSize=size;t.fontSize=64;t.color=color;
        var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=t.font.material;r.sortingOrder=-60;
    }
    static void MakeObjectives()
    {
        var layout=new GameObject("Foundry uplinks").AddComponent<ArenaObjectiveLayout>();layout.transform.SetParent(root,false);
        var positions=new[]{new Vector2(0,0),new Vector2(-22,3.6f),new Vector2(-17,7.2f),new Vector2(0,10.8f),new Vector2(17,7.2f),new Vector2(22,3.6f)};
        var names=new[]{"하단 중앙","서쪽 정비로","서쪽 상단","최상단 중계로","동쪽 상단","동쪽 수송로"};layout.points=new ArenaObjectivePoint[positions.Length];
        for(int i=0;i<positions.Length;i++)
        {
            var go=new GameObject("Uplink "+(char)('A'+i));go.transform.SetParent(layout.transform,false);go.transform.position=positions[i];var point=go.AddComponent<ArenaObjectivePoint>();point.displayName=names[i];point.halfWidth=1.4f;layout.points[i]=point;
            SpriteRenderer Part(string label,Vector2 at,Vector2 size){var part=Box(label,positions[i]+at,size,Color.cyan,-12);part.transform.SetParent(go.transform,true);part.GetComponent<SpriteRenderer>().enabled=false;return part.GetComponent<SpriteRenderer>();}
            point.area=new[]{Part("Zone fill",new Vector2(0,1.175f),new Vector2(2.8f,2.35f)),Part("Zone floor",new Vector2(0,.04f),new Vector2(2.8f,.045f)),Part("Zone left",new Vector2(-1.4f,1.175f),new Vector2(.025f,2.35f)),Part("Zone right",new Vector2(1.4f,1.175f),new Vector2(.025f,2.35f)),Part("Zone top",new Vector2(0,2.35f),new Vector2(2.8f,.025f))};
            var label=new GameObject("Uplink label");label.transform.SetParent(go.transform,false);label.transform.localPosition=new Vector3(-1.55f,2.1f,0);point.label=label.AddComponent<TextMesh>();point.label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");point.label.fontSize=48;point.label.characterSize=.06f;point.label.anchor=TextAnchor.MiddleRight;label.GetComponent<MeshRenderer>().sharedMaterial=point.label.font.material;label.GetComponent<MeshRenderer>().sortingOrder=-10;point.Show(false,false,Color.cyan);
        }
    }
}
