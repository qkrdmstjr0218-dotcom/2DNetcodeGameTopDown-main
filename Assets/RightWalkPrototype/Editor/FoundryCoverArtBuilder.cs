using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;
public static class FoundryCoverArtBuilder
{
    public const string ArtName="Foundry case artwork";
    public static void ApplyVisuals(ArenaMapSettings map)
    {
        foreach(var cover in map.GetComponentsInChildren<ArenaCover>())
        {
            var shape=cover.GetComponent<BoxCollider2D>();var size=shape.bounds.size;
            string name="Case_"+Mathf.RoundToInt(size.x*100)+"_"+Mathf.RoundToInt(size.y*100);
            if(shape.usedByEffector&&shape.TryGetComponent<PlatformEffector2D>(out var effector)&&effector.useOneWay)name+="_Pass";
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Resources/FoundryArt/"+name+".png");
            if(sprite==null)throw new Exception("Missing case proportions: "+name);
            var old=cover.transform.Find(ArtName);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach(var r in cover.GetComponentsInChildren<SpriteRenderer>(true))r.enabled=false;
            var go=new GameObject(ArtName);go.transform.SetParent(cover.transform,false);go.transform.localPosition=shape.offset;
            var art=go.AddComponent<SpriteRenderer>();art.sprite=sprite;art.sortingOrder=6;
            go.transform.localScale=new Vector3(shape.size.x/sprite.bounds.size.x,shape.size.y/sprite.bounds.size.y,1);
        }
    }
    [MenuItem("Tools/Right Walk Prototype/Apply Foundry Case Artwork")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)throw new Exception("Requires saved Foundry edit mode");
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();var before=FoundryArtBuilder.Geometry(map);
        Undo.RegisterFullObjectHierarchyUndo(map.gameObject,"Foundry case artwork");ApplyVisuals(map);
        if(before!=FoundryArtBuilder.Geometry(map))throw new Exception("Case artwork changed collider geometry");
        int passCount=0;
        foreach(var cover in map.GetComponentsInChildren<ArenaCover>())
        {
            var shape=cover.GetComponent<BoxCollider2D>();var art=cover.transform.Find(ArtName).GetComponent<SpriteRenderer>();
            if(Vector2.Distance(shape.bounds.min,art.bounds.min)>.002f||Vector2.Distance(shape.bounds.max,art.bounds.max)>.002f)throw new Exception("Case artwork bounds differ: "+cover.name);
            if(shape.usedByEffector&&shape.TryGetComponent<PlatformEffector2D>(out var effector)&&effector.useOneWay)
            {passCount++;if(!art.sprite.name.EndsWith("_Pass"))throw new Exception("One-way case missing dashed top: "+cover.name);}
        }
        System.IO.Directory.CreateDirectory("LocalTools/Analysis/FoundryArt");
        System.IO.File.WriteAllText("LocalTools/Analysis/FoundryArt/case-geometry.txt","PASS "+map.GetComponentsInChildren<ArenaCover>().Length+" cover transforms/colliders/materials unchanged\nPASS all visible XY case bounds match their existing colliders\nPASS "+passCount+" one-way cases use dashed-top artwork\n");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
}
