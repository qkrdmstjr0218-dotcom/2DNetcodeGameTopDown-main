using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;

public static class FoundryWestBridgeBuilder
{
    public const string RootName="West retracting crossing";
    static Transform root;static Sprite sprite;
    static readonly Color Steel=new(.20f,.28f,.33f),Amber=new(.96f,.63f,.23f);
    [MenuItem("Tools/Right Walk Prototype/Add Foundry West Crossing")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)throw new Exception("Requires saved Foundry edit mode");
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();
        if(map.transform.Find(RootName)!=null)return;
        var deck=map.transform.Find("Lower service bypass/Lower west approach");
        if(deck==null)throw new Exception("Lower approach missing");
        Undo.RegisterFullObjectHierarchyUndo(map.gameObject,"Add west retracting crossing");
        sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        UnityEngine.Object.DestroyImmediate(deck.gameObject);
        root=new GameObject(RootName).transform;root.SetParent(map.transform,false);
        Deck("West spawn approach",-36,-24);Deck("West centre approach",-20,-7);
        Box("West pit bed",new Vector2(-22,-3.9f),new Vector2(4,.5f),Steel,0,true);
        Box("West pit wall",new Vector2(-24.2f,-2),new Vector2(.4f,3.8f),Steel,0,true);
        Box("East pit wall",new Vector2(-19.8f,-2),new Vector2(.4f,3.8f),Steel,0,true);
        Box("Pit warning glow",new Vector2(-22,-3.4f),new Vector2(3.8f,.08f),new Color(.9f,.2f,.1f),1);
        var bridge=root.gameObject.AddComponent<ArenaRetractingBridge>();
        bridge.westLeaf=Box("West leaf",new Vector2(-23,-.09f),new Vector2(2,.18f),Amber,1,true).transform;
        bridge.eastLeaf=Box("East leaf",new Vector2(-21,-.09f),new Vector2(2,.18f),Amber,1,true).transform;
        bridge.warning=Box("Crossing status light",new Vector2(-22,2.55f),new Vector2(1.5f,.08f),new Color(.25f,.9f,.75f),1).GetComponent<SpriteRenderer>();
        bridge.status=Text("Crossing status","CROSSING SAFE",new Vector2(-22,2.9f),.03f,Amber);
        Text("Bypass direction","UPPER BYPASS  <   >",new Vector2(-22,4.05f),.025f,new Color(.25f,.8f,.76f));
        // Existing outer ramp (-35,0)->(-25,3.6), fixed service deck,
        // and inner ramp (-15,0)->(-5,3.6) form a permanent route around the pit.
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    static GameObject Box(string name,Vector2 at,Vector2 size,Color color,int order,bool solid=false)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=at;go.transform.localScale=new Vector3(size.x,size.y,1);
        var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;
        if(solid)go.AddComponent<BoxCollider2D>();return go;
    }
    static void Deck(string name,float left,float right)
    {
        var go=Box(name,new Vector2((left+right)*.5f,-.14f),new Vector2(right-left,.28f),Steel,0,true);
        Box("Solid edge",new Vector2((left+right)*.5f,-.01f),new Vector2(right-left,.035f),Amber,6).transform.SetParent(go.transform,true);
    }
    static TextMesh Text(string name,string value,Vector2 at,float size,Color color)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=at;
        var text=go.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=value;text.anchor=TextAnchor.MiddleCenter;text.fontSize=64;text.characterSize=size;text.color=color;
        var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=text.font.material;r.sortingOrder=6;return text;
    }
}
