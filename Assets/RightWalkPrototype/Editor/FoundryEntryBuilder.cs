using UnityEditor;
using UnityEngine;
using RightWalkPrototype;

// Additive: existing travel lanes, crates and bridge geometry remain untouched.
public static class FoundryEntryBuilder
{
    public const string RootName="Outer spawn balconies and enemy entrances";
    static readonly Color Steel=new(.20f,.28f,.33f),Solid=new(.96f,.63f,.23f),Pass=new(.22f,.8f,.77f);
    public static void Configure(ArenaMapSettings map)
    {
        map.enemyEntrances=true;map.enemyEntrySeconds=1.4f;
        float level=map.floorSpacing;
        map.groundEnemySpawns=new[]{new Vector2(-11,0),new Vector2(10.5f,level),new Vector2(11,0),new Vector2(-10.5f,level)};
        map.patrolRoutes=System.Array.ConvertAll(map.groundEnemySpawns,at=>new ArenaMapSettings.PatrolRoute{left=Mathf.Min(at.x,at.x+(at.x<0?4:-4)),right=Mathf.Max(at.x,at.x+(at.x<0?4:-4)),floor=at.y});
        map.turretSpawns=new[]{new Vector2(-8,2*level),new Vector2(8,2*level),new Vector2(-9,0),new Vector2(9,0)};
        map.droneSpawns=new[]{new Vector2(-26.25f,3*level+2.5f),new Vector2(26.25f,3*level+2.5f)};
        var root=map.transform.Find(RootName);bool addBalconies=root==null;
        if(addBalconies){root=new GameObject(RootName).transform;root.SetParent(map.transform,false);}
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        GameObject Part(string name,Vector2 at,Vector2 size,Color color,int order,bool collision=false)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=at;go.transform.localScale=size;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=color;sr.sortingOrder=order;
            if(collision)go.AddComponent<BoxCollider2D>();return go;
        }
        void Line(string name,Vector2 a,Vector2 b,Color color)
        {
            var d=b-a;var go=Part(name,(a+b)*.5f,new Vector2(d.magnitude,.035f),color,6);
            go.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
        }
        void Label(string name,string value,Vector2 at,Color color)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=at;
            var t=go.AddComponent<TextMesh>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text=value;t.anchor=TextAnchor.MiddleCenter;t.characterSize=.035f;t.fontSize=48;t.color=color;
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=t.font.material;r.sortingOrder=-20;
        }
        if(addBalconies)foreach(int side in new[]{-1,1})
        {
            Part("Outer spawn balcony "+side,new Vector2(side*33.5f,3*level-.14f),new Vector2(5,.28f),Steel,0,true);
            Line("Balcony solid edge "+side,new Vector2(side*31,3*level),new Vector2(side*36,3*level),Solid);
            Vector2 a=new(side*(level>4.8f?31-level/.8f:25),2*level),b=new(side*31,3*level);if(a.x>b.x){var t=a;a=b;b=t;}
            var d=b-a;var normal=new Vector2(-d.normalized.y,d.normalized.x);
            var ramp=Part("Outer balcony access "+side,(a+b)*.5f-normal*.1f,new Vector2(d.magnitude,.2f),Steel,0,true);
            ramp.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
            ramp.GetComponent<BoxCollider2D>().usedByEffector=true;
            var effector=ramp.AddComponent<PlatformEffector2D>();effector.useOneWay=true;effector.surfaceArc=160;effector.useSideFriction=false;effector.useSideBounce=false;
            for(float x=0;x<d.magnitude;x+=.32f)Line("Balcony passable dash",Vector2.Lerp(a,b,x/d.magnitude),Vector2.Lerp(a,b,Mathf.Min(x+.2f,d.magnitude)/d.magnitude),Pass);
            Label("Balcony arrival sign "+side,"ARRIVAL / "+(side<0?"WEST":"EAST"),new Vector2(side*33,3*level+1.9f),Pass);
        }
        RepairBalconyEdges(map);
        // Replace only this builder's noncolliding entrance art, never the balconies or travel lanes.
        var names=new System.Collections.Generic.HashSet<string>{"Enemy entrance decorations","Ground enemy door","Door frame","Door seam","Ground entrance sign","Drone exit recess","Drone exit frame","Drone exit sign","Turret floor hatch","Turret hatch warning"};
        for(int i=root.childCount-1;i>=0;i--)if(names.Contains(root.GetChild(i).name))Object.DestroyImmediate(root.GetChild(i).gameObject);
        var decoration=new GameObject("Enemy entrance decorations").transform;decoration.SetParent(root,false);root=decoration;
        foreach(var at in map.groundEnemySpawns)
        {
            Part("Ground enemy door",at+Vector2.up*.65f,new Vector2(1.5f,1.3f),new Color(.11f,.16f,.2f),-40);
            for(int side=-1;side<=1;side+=2)Part("Door frame",at+new Vector2(side*.72f,.65f),new Vector2(.07f,1.3f),Steel,-35);
            Part("Door seam",at+Vector2.up*.65f,new Vector2(.025f,1.2f),new Color(.4f,.25f,.13f),-34);
            Label("Ground entrance sign","ROBOT / ACCESS",at+Vector2.up*1.55f,Solid);
        }
        foreach(var at in map.droneSpawns)
        {
            Part("Drone exit recess",at+Vector2.up*.48f,new Vector2(2,1.5f),new Color(.075f,.12f,.16f),-40);
            for(int side=-1;side<=1;side+=2)Part("Drone exit frame",at+new Vector2(side*.96f,.48f),new Vector2(.07f,1.5f),Steel,-35);
            Label("Drone exit sign","DRONE / EXIT",at+Vector2.up*1.45f,Solid);
        }
        if(map.turretSpawns!=null)foreach(var at in map.turretSpawns)
        {
            Part("Turret floor hatch",at+Vector2.down*.03f,new Vector2(1.6f,.08f),new Color(.08f,.12f,.14f),4);
            foreach(int side in new[]{-1,1})Part("Turret hatch warning",at+new Vector2(side*.7f,.015f),new Vector2(.18f,.035f),Solid,7);
        }
    }
    // Targeted migration for existing scenes: fill the half-unit end gap up to
    // the boundary's inner face; keep the access ramps and all spawn data.
    public static void RepairBalconyEdges(ArenaMapSettings map)
    {
        var root=map.transform.Find(RootName);if(root==null)return;
        foreach(int side in new[]{-1,1})
        {
            var deck=root.Find("Outer spawn balcony "+side);
            if(deck!=null){deck.position=new Vector3(side*33.5f,3*map.floorSpacing-.14f,deck.position.z);deck.localScale=new Vector3(5,.28f,1);}
            var edge=root.Find("Balcony solid edge "+side);
            if(edge!=null){edge.position=new Vector3(side*33.5f,3*map.floorSpacing,edge.position.z);edge.localScale=new Vector3(5,.035f,1);}
        }
    }
}
