using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;

// Targeted layout revision; retain the existing routes and combat rules.
public static class FoundryLayoutBuilder
{
    public const string RootName="Foundry traversal and cover revision";
    static Sprite sprite;static Transform root;
    static readonly Color Steel=new(.23f,.31f,.34f),Edge=new(.96f,.63f,.23f),Pass=new(.22f,.8f,.77f);
    static GameObject Part(Transform parent,string name,Vector2 at,Vector2 size,Color color,int order)
    {
        var go=new GameObject(name);go.transform.position=at;go.transform.localScale=new Vector3(size.x,size.y,1);go.transform.SetParent(parent,true);
        var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;return go;
    }
    static void Crate(Transform parent,string name,float x,float floor,float width,float height)
    {
        var at=new Vector2(x,floor+height*.5f);var go=Part(parent,name,at,new Vector2(width,height),Steel,2);
        go.AddComponent<BoxCollider2D>();go.AddComponent<ArenaCover>();
        Part(go.transform,"Inset",at,new Vector2(width-.16f,height-.18f),new Color(.11f,.19f,.22f),3);
        Part(go.transform,"Top solid edge",new Vector2(x,floor+height-.0125f),new Vector2(width,.025f),Edge,6);
        foreach(int side in new[]{-1,1})Part(go.transform,"Side edge",new Vector2(x+side*(width*.5f-.0125f),at.y),new Vector2(.025f,height),Edge,6);
    }
    [MenuItem("Tools/Right Walk Prototype/Revise Foundry Traversal And Cover")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)throw new Exception("Requires saved Foundry edit mode");
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();if(Mathf.Abs(map.floorSpacing-6.8f)>.001f)throw new Exception("Requires 6.8 Foundry layout");
        Undo.RegisterFullObjectHierarchyUndo(map.gameObject,"Foundry traversal and cover revision");
        sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        var old=map.transform.Find(RootName);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        root=new GameObject(RootName).transform;root.SetParent(map.transform,false);
        foreach(int side in new[]{-1,1})
        {
            // Upper rung of the two-step ascent, beneath the crown grate.
            float y=FoundryUpperRouteBuilder.UpperStepHeight(map),x=side*2.1f;
            var platform=Part(root,"Central ascent "+side,new Vector2(x,y-.1f),new Vector2(1.6f,.2f),Steel,1);
            platform.AddComponent<BoxCollider2D>().usedByEffector=true;var effector=platform.AddComponent<PlatformEffector2D>();
            effector.useOneWay=true;effector.surfaceArc=160;effector.useSideFriction=false;effector.useSideBounce=false;
            for(float d=-.8f;d<.79f;d+=.32f)Part(platform.transform,"Passable dash",new Vector2(x+d+.1f,y),new Vector2(Mathf.Min(.2f,.8f-d),.035f),Pass,6);
            // Keep hard cover on the solid lower route. One-way decks above
            // ramps/shortcut ledges must remain open for jump-through entry.
            Crate(root,"Lower route cover "+side,side<0?-26f:21f,0,1,2.4f);
            var intersecting=map.transform.Find("Upper cover "+side);if(intersecting!=null)UnityEngine.Object.DestroyImmediate(intersecting.gameObject);
            foreach(string prefix in new[]{"Lower outer cover "})
            {
                var cover=map.transform.Find(prefix+side);if(cover==null)continue;
                float floor=prefix.StartsWith("Lower")?0:2*map.floorSpacing;
                var scale=cover.localScale;scale.y=2.4f;cover.localScale=scale;cover.position=new Vector3(cover.position.x,floor+1.2f,cover.position.z);
            }
        }
        var core=map.transform.Find("Upper core cover");if(core!=null){var s=core.localScale;s.y=2.4f;core.localScale=s;core.position=new Vector3(0,2*map.floorSpacing+1.2f,core.position.z);}
        var depot=map.transform.Find(FoundryDepotBuilder.RootName);
        var entrance=map.transform.Find(FoundryEntranceCoverBuilder.RootName);
        if(entrance!=null)
        {
            for(int i=entrance.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(entrance.GetChild(i).gameObject);
            // Retain low steps here: a tall wall would seal the pit-to-console entry.
            foreach(int side in new[]{-1,1})Crate(entrance,"Capture edge step "+side,side*2.5f,0,.8f,.9f);
        }
        foreach(int side in new[]{-1,1})
        {
            foreach(string prefix in new[]{"Depot cover ","Depot step "}){var item=depot.Find(prefix+side);if(item!=null)UnityEngine.Object.DestroyImmediate(item.gameObject);}
            Crate(depot,"Depot cover "+side,side*2.4f,map.floorSpacing,1.4f,2.4f);
            Crate(depot,"Depot step "+side,side*3.5f,map.floorSpacing,.8f,1.2f);
        }
        foreach(var point in UnityEngine.Object.FindFirstObjectByType<ArenaObjectiveLayout>().points)
        {
            point.halfWidth=2;
            foreach(var r in point.area)
            {
                var t=r.transform;var p=t.localPosition;var s=t.localScale;
                if(r.name=="Zone left")p.x=-2;else if(r.name=="Zone right")p.x=2;else s.x=4;
                t.localPosition=p;t.localScale=s;
            }
            if(point.label!=null){var p=point.label.transform.localPosition;p.x=-2.15f;point.label.transform.localPosition=p;}
            EditorUtility.SetDirty(point);
        }
        // Same ten enemies; relocate entrances, do not increase combat density.
        FoundryEntryBuilder.Configure(map);EditorUtility.SetDirty(map);
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        FoundryUpperRouteBuilder.Apply();
    }
}
