using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;

public static class FoundryEntranceCoverBuilder
{
    public const string RootName="Lower entrance cover stacks";
    static Sprite sprite;
    static readonly Color Edge=new(.96f,.63f,.23f),Steel=new(.20f,.28f,.33f);

    [MenuItem("Tools/Right Walk Prototype/Add Foundry Entrance Covers")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)
            throw new Exception("Requires saved Foundry edit mode");
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();
        if(map.transform.Find(RootName)!=null)return;
        if(map.transform.Find("Lower service bypass")==null)throw new Exception("Add the lower bypass first");
        sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        var root=new GameObject(RootName);root.transform.SetParent(map.transform,false);
        Undo.RegisterCreatedObjectUndo(root,"Add entrance cover stacks");
        foreach(int side in new[]{-1,1})
        {
            // Keep the full capture width (-1.4..1.4) and open ramp mouths.
            // Highest top .9 leaves 2.42 beneath the solid middle deck.
            Crate(root.transform,"Stack base "+side,side*2,.8f,0);
            Crate(root.transform,"Stack upper "+side,side*2,.8f,.45f);
            Crate(root.transform,"Outer step "+side,side*2.65f,.5f,0);
        }
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    static void Crate(Transform parent,string name,float x,float width,float floor)
    {
        var go=Part(parent,name,new Vector2(x,floor+.225f),new Vector2(width,.45f),new Color(.29f,.34f,.36f),2);
        go.AddComponent<BoxCollider2D>();go.AddComponent<ArenaCover>();
        Part(go.transform,"Inset",new Vector2(x,floor+.225f),new Vector2(width-.14f,.27f),Steel,3);
        Part(go.transform,"Top solid edge",new Vector2(x,floor+.44f),new Vector2(width,.025f),Edge,6);
        Part(go.transform,"Left solid edge",new Vector2(x-width*.5f+.0125f,floor+.225f),new Vector2(.025f,.45f),Edge,6);
        Part(go.transform,"Right solid edge",new Vector2(x+width*.5f-.0125f,floor+.225f),new Vector2(.025f,.45f),Edge,6);
        Part(go.transform,"Bottom solid edge",new Vector2(x,floor+.0125f),new Vector2(width,.025f),Edge,6);
    }
    static GameObject Part(Transform parent,string name,Vector2 at,Vector2 size,Color color,int order)
    {
        var go=new GameObject(name);go.transform.position=at;go.transform.localScale=new Vector3(size.x,size.y,1);
        go.transform.SetParent(parent,true);var renderer=go.AddComponent<SpriteRenderer>();
        renderer.sprite=sprite;renderer.color=color;renderer.sortingOrder=order;return go;
    }
}
