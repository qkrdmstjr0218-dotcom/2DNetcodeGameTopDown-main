using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;

public static class FoundryTraversalTuningBuilder
{
    [MenuItem("Tools/Right Walk Prototype/Apply Foundry Jump and Spawn Covers")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)
            throw new Exception("Requires saved Foundry edit mode");
        var rules=UnityEngine.Object.FindFirstObjectByType<RelayArenaPrototype>();
        Undo.RecordObject(rules,"Tune Foundry jump");rules.jumpSpeed=7.5f;EditorUtility.SetDirty(rules);
        foreach(int side in new[]{-1,1})
        {
            var cover=rules.transform.Find("Lower outer cover "+side);
            if(cover==null)throw new Exception("Missing spawn approach cover "+side);
            Undo.RegisterFullObjectHierarchyUndo(cover.gameObject,"Make spawn cover passable");
            var shape=cover.GetComponent<BoxCollider2D>();shape.usedByEffector=true;
            var platform=cover.GetComponent<PlatformEffector2D>();
            if(platform==null)platform=cover.gameObject.AddComponent<PlatformEffector2D>();
            platform.useOneWay=true;platform.surfaceArc=160;platform.useSideFriction=false;platform.useSideBounce=false;
            var solid=cover.Find("Solid edge");if(solid!=null)UnityEngine.Object.DestroyImmediate(solid.gameObject);
            var status=cover.Find("Cover status");if(status!=null)status.GetComponent<SpriteRenderer>().color=new Color(.96f,.63f,.23f);
            var old=cover.Find("Passable outline");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var outline=new GameObject("Passable outline").transform;outline.SetParent(cover,false);
            var bounds=shape.bounds;var a=new Vector2(bounds.min.x,bounds.min.y);var b=new Vector2(bounds.max.x,bounds.max.y);
            var sprite=cover.GetComponent<SpriteRenderer>().sprite;
            // Match RelayArenaPrototype: only the walkable top is cyan-dashed;
            // the amber panel still identifies projectile-blocking cover.
            Dash(outline,sprite,new Vector2(a.x-.025f,b.y),new Vector2(b.x+.025f,b.y));
        }
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    static void Dash(Transform parent,Sprite sprite,Vector2 a,Vector2 b)
    {
        var delta=b-a;float length=delta.magnitude;
        for(float d=0;d<length-.001f;d+=.32f)
        {
            float span=Mathf.Min(.20f,length-d);
            var go=new GameObject("Passable dash");go.transform.position=a+delta.normalized*(d+span*.5f);
            go.transform.localScale=new Vector3(span,.035f,1);
            go.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);go.transform.SetParent(parent,true);
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=new Color(.25f,.8f,.76f);renderer.sortingOrder=6;
        }
    }
}
