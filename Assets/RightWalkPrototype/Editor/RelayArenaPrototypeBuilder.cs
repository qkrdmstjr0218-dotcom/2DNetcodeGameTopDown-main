using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RightWalkPrototype;

public static class RelayArenaPrototypeBuilder
{
    public const string ScenePath="Assets/RightWalkPrototype/Scenes/Network/RelayArenaPrototype.unity";
    private static Sprite sprite;
    private static Transform root;
    private static readonly Color steel=new Color(.20f,.29f,.34f);
    private static readonly Color trim=new Color(.42f,.60f,.63f);
    private static readonly Color amber=new Color(.96f,.63f,.23f);
    private static readonly Color cyan=new Color(.25f,.80f,.76f);

    [MenuItem("Tools/Right Walk Prototype/Create Relay Arena Prototype")]
    public static void Build()
    {
        if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)
            throw new System.InvalidOperationException("Requires saved edit mode.");
        if(System.IO.File.Exists(ScenePath))throw new System.InvalidOperationException("Arena already exists; edit the scene in place.");
        var source=EditorSceneManager.OpenScene(SideScrollNetworkTrialBuilder.ScenePath);
        EditorSceneManager.SaveScene(source,ScenePath,true);
        var scene=EditorSceneManager.OpenScene(ScenePath);
        foreach(var go in scene.GetRootGameObjects())
            if(go.GetComponent<Camera>()==null&&go.GetComponent<Light>()==null&&go.GetComponent<SideScrollNetworkSession>()==null)
                Object.DestroyImmediate(go);
        sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        root=new GameObject("Relay facility 쨌 prototype layout").transform;
        var rules=root.gameObject.AddComponent<RelayArenaPrototype>();rules.arenaCamera=Camera.main;
        rules.readyRoomFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/EliceDigitalBaeum_Regular.ttf");
        rules.weaponRange=16f;rules.shieldEffectShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/RightWalkPrototype/Art/UI/ShieldEffect.shader");
        rules.ammoCursorShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/RightWalkPrototype/Art/UI/AmmoCursor.shader");
        rules.hudPortraits=new Texture2D[4];for(int i=0;i<4;i++)rules.hudPortraits[i]=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RightWalkPrototype/Art/UI/portrait-"+(i+1)+".png");
        Camera.main.backgroundColor=new Color(.055f,.085f,.12f);Camera.main.orthographicSize=4.8f;
        Camera.main.transform.position=new Vector3(-8,2.7f,-10);

        // Architecture behind the playfield: no decorative element has collision.
        Box("Distant wall",new Vector2(0,4),new Vector2(54,24),new Color(.055f,.085f,.12f),-100);
        for(int i=-3;i<=3;i++)
        {
            float x=i*8;
            Box("Recessed wall bay "+i,new Vector2(x,4.5f),new Vector2(7.6f,15),new Color(.075f,.12f,.16f),-95);
            Box("Bay upright "+i,new Vector2(x-3.6f,4),new Vector2(.16f,17),new Color(.11f,.18f,.23f),-90);
            for(int j=0;j<3;j++)
                Box("Wall service panel",new Vector2(x,1+j*3.4f),new Vector2(6.7f,2.9f),new Color(.085f+.009f*j,.14f+.009f*j,.18f+.009f*j),-94);
            Beam("Background truss",new Vector2(x-3.3f,-1),new Vector2(x+3.3f,9),.09f,new Color(.12f,.20f,.24f),-89);
        }
        for(int i=0;i<3;i++)
            Box("Overhead service pipe",new Vector2(0,9.6f+i*.24f),new Vector2(48,.07f),new Color(.18f,.27f,.30f),-85);
        Box("Upper header",new Vector2(0,8.9f),new Vector2(11,.04f),cyan,-80);
        Text("Facility title","RELAY / 07",new Vector2(0,8.25f),.16f,new Color(.27f,.43f,.46f),-80);
        Text("Facility subtitle","TRANSFER HALL",new Vector2(0,7.65f),.055f,new Color(.25f,.37f,.41f),-80);

        // Ground and its only dangerous break. Upper routes remain available.
        Deck("West ground",-23,12,0,true,amber);
        Deck("East ground",16,23,0,true,amber);
        Box("Pit floor",new Vector2(14,-3.9f),new Vector2(4,.6f),steel,-5,true);
        Box("Pit west wall",new Vector2(11.8f,-2.15f),new Vector2(.4f,3.6f),steel,-5,true);
        Box("Pit east wall",new Vector2(16.2f,-2.15f),new Vector2(.4f,3.6f),steel,-5,true);
        Box("Pit red wash",new Vector2(14,-3.1f),new Vector2(3.6f,.18f),new Color(.7f,.19f,.13f),-4);
        for(int i=0;i<7;i++)Box("Pit warning stripe",new Vector2(12.25f+i*.55f,-3.5f),new Vector2(.22f,.09f),amber,-3);
        Box("Left arena boundary",new Vector2(-23.25f,4),new Vector2(.5f,16),steel,0,true);
        Box("Right arena boundary",new Vector2(23.25f,4),new Vector2(.5f,16),steel,0,true);

        Deck("West mezzanine",-13,-5,3.2f,true,cyan);
        Deck("East mezzanine",5,13,3.2f,true,cyan);
        Deck("Atrium grated crossing",-5,5,3.2f,false,cyan);
        Deck("Upper relay deck",-4,4,6.4f,true,amber);
        Ramp("West lower ramp",new Vector2(-22,0),new Vector2(-13,3.2f));
        Ramp("East lower ramp",new Vector2(22,0),new Vector2(13,3.2f));
        Ramp("West upper ramp",new Vector2(-12,3.2f),new Vector2(-4,6.4f));
        Ramp("East upper ramp",new Vector2(12,3.2f),new Vector2(4,6.4f));

        Cover("Ground west cover",-3.8f,0,1.2f);
        Cover("Ground east cover",5.1f,0,1.2f);
        Cover("West mezzanine cover",-8,3.2f,.65f);
        Cover("East mezzanine cover",8,3.2f,.65f);
        Cover("Relay pedestal",0,6.4f,1.2f);
        // Visual supports are behind the playable lane.
        foreach(float x in new[]{-12.8f,-5.2f,5.2f,12.8f})
        {
            Box("Mezzanine support",new Vector2(x,1.4f),new Vector2(.22f,2.7f),new Color(.13f,.22f,.27f),-30);
            Box("Support status light",new Vector2(x,1.8f),new Vector2(.07f,.45f),cyan,-29);
        }
        Text("West route sign","WEST / ACCESS",new Vector2(-17.5f,5.8f),.075f,cyan,-50);
        Text("East route sign","EAST / ACCESS",new Vector2(17.5f,5.8f),.075f,cyan,-50);
        Text("Atrium floor sign","ATRIUM",new Vector2(0,1.5f),.10f,new Color(.18f,.28f,.32f),-60);
        Text("Bridge warning sign","RETRACTING CROSSING",new Vector2(14,4.65f),.055f,amber,-50);
        for(int i=0;i<5;i++)
        {
            float x=11.6f+i*.14f;
            Box("Bridge approach marking",new Vector2(x,.015f),new Vector2(.06f,.035f),amber,2);
            Box("Bridge approach marking",new Vector2(16.4f+i*.14f,.015f),new Vector2(.06f,.035f),amber,2);
        }
        rules.bridgeLeft=Box("Retracting bridge 쨌 west leaf",new Vector2(13,-.09f),new Vector2(2,.18f),amber,1).transform;
        rules.bridgeRight=Box("Retracting bridge 쨌 east leaf",new Vector2(15,-.09f),new Vector2(2,.18f),amber,1).transform;
        rules.bridgeCollider=Box("Retracting bridge collision",new Vector2(14,-.09f),new Vector2(4,.18f),Color.clear,0,true).GetComponent<BoxCollider2D>();
        rules.warningLight=Box("Bridge state beacon",new Vector2(14,4.3f),new Vector2(1.5f,.08f),cyan,-40).GetComponent<SpriteRenderer>();

        var session=Object.FindFirstObjectByType<SideScrollNetworkSession>();session.targetPrefab=null;
        session.tutorialTargetPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RightWalkPrototype/NetworkDamageTarget.prefab");
        session.playerSpawns=new Transform[4];
        var positions=new[]{new Vector2(-8,.03f),new Vector2(8,.03f),new Vector2(-7,3.23f),new Vector2(7,3.23f)};
        for(int i=0;i<4;i++)
        {
            var spawn=new GameObject("Player "+(i+1)+" start").transform;spawn.SetParent(root);spawn.position=positions[i];
            session.playerSpawns[i]=spawn;
            Box("Spawn floor marker "+(i+1),positions[i]+Vector2.down*.005f,new Vector2(.7f,.025f),cyan,3);
        }
        ApplyTraversalLayout();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    [MenuItem("Tools/Right Walk Prototype/Apply Arena Traversal Layout")]
    public static void ApplyTraversalLayout()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.path!=ScenePath)throw new System.InvalidOperationException("Requires arena edit mode");
        var rules=Object.FindFirstObjectByType<RelayArenaPrototype>();
        var old=rules.transform.Find("Traversal surface markings");if(old!=null)Object.DestroyImmediate(old.gameObject);
        root=new GameObject("Traversal surface markings").transform;root.SetParent(rules.transform);
        sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        foreach(var collider in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            var marks=collider.transform.Find("Arena surface markings");if(marks!=null)Object.DestroyImmediate(marks.gameObject);
        }
        foreach(string name in new[]{"West mezzanine","East mezzanine","Upper relay deck"})
        {
            var go=GameObject.Find(name);go.GetComponent<BoxCollider2D>().usedByEffector=false;
            var e=go.GetComponent<PlatformEffector2D>();if(e!=null)Object.DestroyImmediate(e);
        }
        // Keep the low boxes below the central grate, with standing clearance.
        foreach(var pair in new[]{new[]{"Ground west cover","-3.6"},new[]{"Ground east cover","3.6"}})
        {
            var go=GameObject.Find(pair[0]);float dx=float.Parse(pair[1],System.Globalization.CultureInfo.InvariantCulture)-go.transform.position.x;
            foreach(var suffix in new[]{" inset"," cap"," status"})
            {
                var art=GameObject.Find(pair[0]+suffix).transform;
                if(!art.IsChildOf(go.transform))art.position+=Vector3.right*dx;
            }
            go.transform.position+=Vector3.right*dx;
        }
        Physics2D.SyncTransforms();
        foreach(string name in new[]{"Ground west cover","Ground east cover","West mezzanine cover","East mezzanine cover","Relay pedestal"})
        {
            var go=GameObject.Find(name);var c=go.GetComponent<BoxCollider2D>();c.usedByEffector=true;
            var e=go.GetComponent<PlatformEffector2D>();if(e==null)e=go.AddComponent<PlatformEffector2D>();
            e.useOneWay=true;e.useSideFriction=false;e.useSideBounce=false;e.surfaceArc=160;
            if(go.GetComponent<ArenaCover>()==null)go.AddComponent<ArenaCover>();
            bool solidTrial=name=="Ground west cover";
            if(solidTrial)
            {
                var position=go.transform.position;position.y=.9f;go.transform.position=position;
                var scale=go.transform.localScale;scale.y=1.8f;go.transform.localScale=scale;
                c.usedByEffector=false;Object.DestroyImmediate(e);Physics2D.SyncTransforms();
            }
            var b=c.bounds;
            foreach(var suffix in new[]{" inset"," cap"," status"})GameObject.Find(name+suffix).transform.SetParent(go.transform,true);
            GameObject.Find(name+" cap").GetComponent<SpriteRenderer>().enabled=false;
            if(solidTrial)
            {
                var marks=new GameObject("Arena surface markings").transform;marks.SetParent(go.transform,false);
                var cap=Box(name+" solid cap",new Vector2(b.center.x,b.max.y-.015f),new Vector2(b.size.x+.05f,.035f),amber,6);cap.transform.SetParent(marks,true);
            }
            else DashedEdge(name+" pass cap",new Vector2(b.min.x-.025f,b.max.y),new Vector2(b.max.x+.025f,b.max.y),go.transform);
        }
        foreach(var c in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            var edge=GameObject.Find(c.name+" edge");
            if(edge!=null)
            {
                if(c.usedByEffector){edge.GetComponent<SpriteRenderer>().enabled=false;DashedEdge(c.name+" pass edge",new Vector2(c.bounds.min.x,c.bounds.max.y),new Vector2(c.bounds.max.x,c.bounds.max.y),c.transform);}
                else {edge.GetComponent<SpriteRenderer>().enabled=true;edge.GetComponent<SpriteRenderer>().color=amber;}
                edge.transform.SetParent(c.transform,true);
            }
            if(c.name.EndsWith(" ramp"))
            {
                GameObject.Find(c.name+" rail").GetComponent<SpriteRenderer>().enabled=false;
                Vector2 a=c.transform.TransformPoint(new Vector2(-.5f,.5f)),b=c.transform.TransformPoint(new Vector2(.5f,.5f));
                DashedEdge(c.name+" pass rail",a,b,c.transform);
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    private static void DashedEdge(string name,Vector2 a,Vector2 b,Transform owner)
    {
        float length=Vector2.Distance(a,b);
        var marks=owner.Find("Arena surface markings");
        if(marks==null){marks=new GameObject("Arena surface markings").transform;marks.SetParent(owner,false);}
        for(float x=0;x<length;x+=.32f)
        {
            var segment=Beam(name,Vector2.Lerp(a,b,x/length),Vector2.Lerp(a,b,Mathf.Min(x+.20f,length)/length),.035f,cyan,6);
            segment.transform.SetParent(marks,true);
        }
    }
    private static GameObject Box(string name,Vector2 position,Vector2 size,Color color,int order,bool solid=false)
    {
        var go=new GameObject(name);go.transform.SetParent(root);go.transform.position=position;go.transform.localScale=new Vector3(size.x,size.y,1);
        var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;
        if(solid)go.AddComponent<BoxCollider2D>();return go;
    }
    private static void OneWay(GameObject go)
    {
        var collider=go.GetComponent<BoxCollider2D>();collider.usedByEffector=true;
        var effector=go.AddComponent<PlatformEffector2D>();effector.useOneWay=true;effector.useSideFriction=false;effector.useSideBounce=false;effector.surfaceArc=160;
    }
    private static void Deck(string name,float left,float right,float top,bool solid,Color accent)
    {
        var go=Box(name,new Vector2((left+right)*.5f,top-.16f),new Vector2(right-left,.32f),steel,0,true);
        if(!solid)OneWay(go);
        Box(name+" edge",new Vector2((left+right)*.5f,top-.015f),new Vector2(right-left,.035f),accent,1);
        for(float x=left+.25f;x<right;x+=1.2f)
            Box(name+" panel joint",new Vector2(x,top-.19f),new Vector2(.035f,.18f),new Color(.11f,.19f,.23f),1);
        if(!solid)for(float x=left+.15f;x<right;x+=.4f)
            Box("Grate aperture",new Vector2(x,top-.09f),new Vector2(.22f,.06f),new Color(.065f,.11f,.14f),2);
    }
    private static GameObject Beam(string name,Vector2 from,Vector2 to,float thickness,Color color,int order)
    {
        Vector2 delta=to-from;var go=Box(name,(from+to)*.5f,new Vector2(delta.magnitude,thickness),color,order);
        go.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        return go;
    }
    private static void Ramp(string name,Vector2 lower,Vector2 upper)
    {
        var delta=upper-lower;
        var go=Box(name,(lower+upper)*.5f+Vector2.down*.10f,new Vector2(delta.magnitude+.2f,.20f),steel,0,true);
        float angle=Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg;
        // Keep the effector's local up facing upwards on mirrored ramps.
        if(angle>90)angle-=180;
        go.transform.rotation=Quaternion.Euler(0,0,angle);OneWay(go);
        Beam(name+" rail",lower+Vector2.down*.015f,upper+Vector2.down*.015f,.04f,cyan,1);
        for(int i=0;i<9;i++)
        {
            Vector2 p=Vector2.Lerp(lower,upper,(i+.5f)/9f);
            Box(name+" tread",p+Vector2.down*.03f,new Vector2(.12f,.05f),trim,2);
        }
    }
    private static void Cover(string name,float x,float floor,float height)
    {
        var cover=Box(name,new Vector2(x,floor+height*.5f),new Vector2(1.25f,height),new Color(.27f,.34f,.36f),2,true);OneWay(cover);cover.AddComponent<ArenaCover>();
        Box(name+" inset",new Vector2(x,floor+height*.5f),new Vector2(1.05f,height*.65f),steel,3);
        Box(name+" cap",new Vector2(x,floor+height-.03f),new Vector2(1.3f,.06f),trim,4);
        Box(name+" status",new Vector2(x-.4f,floor+height*.5f),new Vector2(.05f,height*.4f),amber,4);
    }
    private static void Text(string name,string text,Vector2 position,float size,Color color,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(root);go.transform.position=position;
        var mesh=go.AddComponent<TextMesh>();mesh.text=text;mesh.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mesh.fontSize=64;mesh.characterSize=size;mesh.anchor=TextAnchor.MiddleCenter;mesh.color=color;
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=mesh.font.material;renderer.sortingOrder=order;
    }
}
