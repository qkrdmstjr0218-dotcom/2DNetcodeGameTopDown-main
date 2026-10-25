using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;

public static class FoundryUpperRouteBuilder
{
    public const string RootName="Upper route moving bridges";
    public const float BridgeWidth=6,FixedSpawnX=9.625f;
    public static float LowerStepHeight(ArenaMapSettings map)=>map.floorSpacing*(2+1f/3);
    public static float UpperStepHeight(ArenaMapSettings map)=>map.floorSpacing*(2+2f/3);
    static Sprite sprite;static Transform root;
    static readonly Color Steel=new(.20f,.28f,.33f),Edge=new(.96f,.63f,.23f),Pass=new(.22f,.8f,.77f);
    static GameObject Part(Transform parent,string name,Vector2 at,Vector2 size,Color color,int order,bool solid=false)
    {
        var go=new GameObject(name);go.transform.position=at;go.transform.localScale=new Vector3(size.x,size.y,1);go.transform.SetParent(parent,true);
        var image=go.AddComponent<SpriteRenderer>();image.sprite=sprite;image.color=color;image.sortingOrder=order;
        if(solid)go.AddComponent<BoxCollider2D>();return go;
    }
    static TextMesh Label(Transform parent,string name,string text,Vector2 at,float size)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=at;var t=go.AddComponent<TextMesh>();
        t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=text;t.anchor=TextAnchor.MiddleCenter;t.fontSize=48;t.characterSize=size;t.color=Pass;
        var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=t.font.material;r.sortingOrder=-10;return t;
    }
    static void Dashes(Transform parent,float x,float y,float width)
    {
        for(float d=-width*.5f;d<width*.5f-.01f;d+=.32f)
        {float span=Mathf.Min(.2f,width*.5f-d);Part(parent,"Passable dash",new Vector2(x+d+span*.5f,y),new Vector2(span,.035f),Pass,6);}
    }
    [MenuItem("Tools/Right Walk Prototype/Add Upper Route Bridges")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)throw new Exception("Requires saved Foundry edit mode");
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();if(Mathf.Abs(map.floorSpacing-6.8f)>.001f)throw new Exception("Requires 6.8 layout");
        Undo.RegisterFullObjectHierarchyUndo(map.gameObject,"Upper route bridges");
        sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        var old=map.transform.Find(RootName);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        root=new GameObject(RootName).transform;root.SetParent(map.transform,false);float crown=3*map.floorSpacing;
        foreach(int side in new[]{-1,1})
        {
            // Open six of the original seven units; retain the ramp arrival.
            var outer=map.transform.Find(side<0?"Crown west":"Crown east");
            outer.position=new Vector3(side*FixedSpawnX,crown-.14f,0);outer.localScale=new Vector3(.75f,.28f,1);
            var inner=Part(root,"Crown inner fixed "+side,new Vector2(side*3.125f,crown-.14f),new Vector2(.25f,.28f),Steel,0,true);
            Part(inner.transform,"Solid edge",new Vector2(side*3.125f,crown),new Vector2(.25f,.035f),Edge,6);
            var go=new GameObject("Crown bridge "+side);go.transform.SetParent(root,false);
            var bridge=go.AddComponent<ArenaRetractingBridge>();bridge.left=side<0?-9.25f:3.25f;bridge.right=bridge.left+BridgeWidth;
            bridge.floor=crown;bridge.lethalPit=false;bridge.detour="3F ROUTE";bridge.phaseOffset=side<0?7:0;
            bridge.westLeaf=Part(go.transform,"West moving leaf",new Vector2(bridge.left+1.5f,crown-.09f),new Vector2(3,.18f),Edge,1,true).transform;
            bridge.eastLeaf=Part(go.transform,"East moving leaf",new Vector2(bridge.right-1.5f,crown-.09f),new Vector2(3,.18f),Edge,1,true).transform;
            bridge.warning=Part(go.transform,"Retraction warning",new Vector2(side*6.25f,crown+1.1f),new Vector2(1.5f,.05f),Pass,-2).GetComponent<SpriteRenderer>();
            bridge.status=Label(go.transform,"Crossing status","CROSSING SAFE",new Vector2(side*6.25f,crown+1.45f),.022f);
            Label(go.transform,"Bridge name",side<0?"4F / WEST":"4F / EAST",new Vector2(side*6.25f,crown+1.85f),.026f);
            // Low one-way steps enrich the central third floor without making walls.
            float y=LowerStepHeight(map),x=side*4.5f;
            var step=Part(root,"Third floor step "+side,new Vector2(x,y-.1f),new Vector2(3,.2f),Steel,1,true);
            step.GetComponent<BoxCollider2D>().usedByEffector=true;var effector=step.AddComponent<PlatformEffector2D>();
            effector.useOneWay=true;effector.surfaceArc=160;effector.useSideFriction=false;effector.useSideBounce=false;
            Dashes(step.transform,x,y,3);
            var ascent=map.transform.Find(FoundryLayoutBuilder.RootName+"/Central ascent "+side);
            if(ascent!=null)
            {
                float high=UpperStepHeight(map);ascent.position=new Vector3(side*2.1f,high-.1f,0);ascent.localScale=new Vector3(1.6f,.2f,1);
                for(int i=ascent.childCount-1;i>=0;i--)if(ascent.GetChild(i).name=="Passable dash")UnityEngine.Object.DestroyImmediate(ascent.GetChild(i).gameObject);
                Dashes(ascent,side*2.1f,high,1.6f);
            }
        }
        if(map.respawnPoints!=null)for(int i=0;i<map.respawnPoints.Length;i++)
        {var p=map.respawnPoints[i];if(Mathf.Abs(p.y-crown-.04f)<.01f&&(Mathf.Abs(Mathf.Abs(p.x)-9)<.01f||Mathf.Abs(Mathf.Abs(p.x)-FixedSpawnX)<.01f)){p.x=Mathf.Sign(p.x)*FixedSpawnX;map.respawnPoints[i]=p;}}
        EditorUtility.SetDirty(map);
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
}
