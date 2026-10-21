using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;

// Additive update: never regenerate the existing upper routes or consoles.
public static class FoundryLowerBypassBuilder
{
    const string BypassName = "Lower service bypass";
    static readonly Color Steel = new(.20f,.28f,.33f), Edge = new(.96f,.63f,.23f);
    static Sprite sprite;
    static Transform root;

    [MenuItem("Tools/Right Walk Prototype/Add Foundry Lower Bypass")]
    public static void Apply()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.isDirty || scene.path != RelayFoundryBuilder.ScenePath)
            throw new Exception("Requires saved Foundry edit mode");
        var map = UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();
        if (map.transform.Find(BypassName) != null) { RepairSeams(); return; }
        var original = map.transform.Find("Lower west");
        if (original == null) throw new Exception("Original lower deck missing; inspect before applying");
        sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        Undo.RegisterFullObjectHierarchyUndo(map.gameObject,"Add lower service bypass");
        UnityEngine.Object.DestroyImmediate(original.gameObject);
        root = new GameObject(BypassName).transform;
        root.SetParent(map.transform,false);
        Deck("Lower west approach",-36,-7,0);
        Deck("Lower centre roof",-3,3,0);
        Deck("Lower east approach",7,24,0);
        Deck("Service floor",-3.6f,3.6f,-2.8f);
        Ramp("West solid service ramp",new Vector2(-7,0),new Vector2(-3.6f,-2.8f));
        Ramp("East solid service ramp",new Vector2(3.6f,-2.8f),new Vector2(7,0));
        Box("Service recess",new Vector2(0,-1.55f),new Vector2(7.2f,2.3f),new Color(.055f,.10f,.13f),-85);
        Box("Service guide light",new Vector2(0,-2.55f),new Vector2(5.8f,.045f),new Color(.22f,.52f,.56f),-70);
        // Standing clearance under the retained solid roof is 2.52 units.
        // Both entrances are open above the ramps; neither needs a jump or drop input.
        var label = new GameObject("Service route sign");label.transform.SetParent(root,false);
        label.transform.position=new Vector3(0,-1.25f,0);
        var text=label.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text="SERVICE / BYPASS";text.anchor=TextAnchor.MiddleCenter;text.fontSize=64;text.characterSize=.045f;text.color=new Color(.24f,.43f,.47f);
        var renderer=label.GetComponent<MeshRenderer>();renderer.sharedMaterial=text.font.material;renderer.sortingOrder=-60;
        // Keep the existing top bound and gameplay zoom; expose the service floor below.
        var bounds=map.cameraBounds;bounds.yMin=-5.7f;map.cameraBounds=bounds;map.overviewCentre=bounds.center;
        EditorUtility.SetDirty(map);Physics2D.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/Right Walk Prototype/Repair Foundry Lower Seams")]
    public static void RepairSeams()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)
            throw new Exception("Requires saved Foundry edit mode");
        var parent=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>().transform.Find(BypassName);
        if(parent==null)throw new Exception("Lower bypass missing");
        Undo.RegisterFullObjectHierarchyUndo(parent.gameObject,"Align lower ramp joins");
        foreach(var child in parent.GetComponentsInChildren<Transform>())
            if(child.name=="Service entrance marker")UnityEngine.Object.DestroyImmediate(child.gameObject);
        foreach(int side in new[]{-1,1})
        {
            var ramp=parent.Find(side<0?"West solid service ramp":"East solid service ramp");
            var a=side<0?new Vector2(-7,0):new Vector2(3.6f,-2.8f);
            var b=side<0?new Vector2(-3.6f,-2.8f):new Vector2(7,0);
            var delta=b-a;var normal=new Vector2(-delta.normalized.y,delta.normalized.x);
            ramp.position=(a+b)*.5f-normal*.1f;
            ramp.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
            ramp.localScale=new Vector3(delta.magnitude,.2f,1);
            var edge=ramp.Find("Solid edge");edge.localPosition=new Vector3(0,.5f,0);
            edge.localRotation=Quaternion.identity;edge.localScale=new Vector3(1,.175f,1);
        }
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }

    static GameObject Box(string name,Vector2 position,Vector2 size,Color color,int order,bool solid=false)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=position;
        go.transform.localScale=new Vector3(size.x,size.y,1);
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=color;renderer.sortingOrder=order;
        if(solid)go.AddComponent<BoxCollider2D>();return go;
    }
    static void Deck(string name,float left,float right,float top)
    {
        var go=Box(name,new Vector2((left+right)*.5f,top-.14f),new Vector2(right-left,.28f),Steel,0,true);
        Box("Solid edge",new Vector2((left+right)*.5f,top-.01f),new Vector2(right-left,.035f),Edge,6).transform.SetParent(go.transform,true);
    }
    static void Ramp(string name,Vector2 a,Vector2 b)
    {
        var delta=b-a;var normal=new Vector2(-delta.normalized.y,delta.normalized.x);
        float angle=Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg;
        // Extending a solid slope past its high endpoint makes a raised lip.
        var go=Box(name,(a+b)*.5f-normal*.1f,new Vector2(delta.magnitude,.2f),Steel,0,true);
        go.transform.rotation=Quaternion.Euler(0,0,angle);
        var line=Box("Solid edge",(a+b)*.5f,new Vector2(delta.magnitude,.035f),Edge,6);
        line.transform.rotation=go.transform.rotation;line.transform.SetParent(go.transform,true);
    }
}
