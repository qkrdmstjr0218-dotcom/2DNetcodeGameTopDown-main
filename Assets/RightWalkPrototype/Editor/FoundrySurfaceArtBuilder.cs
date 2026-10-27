using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RightWalkPrototype;
public static class FoundrySurfaceArtBuilder
{
    public const string Skin="Unified foundry surface";
    static readonly Color Steel=new(.20f,.28f,.33f),Inset=new(.10f,.18f,.23f),Bevel=new(.29f,.39f,.44f),Pass=new(.27f,.82f,.79f),Solid=new(.96f,.63f,.23f);
    public static string Geometry(ArenaMapSettings map)=>Newtonsoft.Json.JsonConvert.SerializeObject(map.GetComponentsInChildren<BoxCollider2D>(true).Select(c=>new{id=c.GetInstanceID(),matrix=Enumerable.Range(0,16).Select(i=>c.transform.localToWorldMatrix[i]).ToArray(),shape=EditorJsonUtility.ToJson(c),effector=c.GetComponent<PlatformEffector2D>()!=null?EditorJsonUtility.ToJson(c.GetComponent<PlatformEffector2D>()):""}));
    [MenuItem("Tools/Right Walk Prototype/Unify Foundry Surface and Hardware Art")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)throw new Exception("Requires saved Foundry edit mode");
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();string before=Geometry(map);
        Undo.RegisterFullObjectHierarchyUndo(map.gameObject,"Unify foundry art");ApplyVisuals(map);ArenaFoundryLandmarkArt.Apply(map);
        string after=Geometry(map);if(before!=after)throw new Exception("Decorative pass changed collider or effector state");
        System.IO.Directory.CreateDirectory("LocalTools/Analysis/MapArtUnity-2026-10-27");System.IO.File.WriteAllText("LocalTools/Analysis/MapArtUnity-2026-10-27/geometry-before.json",before);System.IO.File.WriteAllText("LocalTools/Analysis/MapArtUnity-2026-10-27/geometry-after.json",after);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    public static void ApplyVisuals(ArenaMapSettings map)
    {
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        foreach(var r in map.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name.EndsWith(" continuous inset")||r.name.EndsWith(" lower bevel")).ToArray())UnityEngine.Object.DestroyImmediate(r.gameObject);
        foreach(var collider in map.GetComponentsInChildren<BoxCollider2D>(true))
        {
            var t=collider.transform;var scale=t.lossyScale;float width=Mathf.Abs(collider.size.x*scale.x),height=Mathf.Abs(collider.size.y*scale.y);
            if(collider.GetComponent<ArenaCover>()!=null||height>.31f||height<.1f||width<.2f||t.GetComponent<SpriteRenderer>()==null)continue;
            var effector=collider.GetComponent<PlatformEffector2D>();bool pass=collider.usedByEffector&&effector!=null&&effector.useOneWay;
            Surface(t,sprite,collider.offset,width,height,pass);
        }
        // The main bridge's collision is attached only at runtime. Its art still follows each leaf.
        var prototype=map.GetComponent<RelayArenaPrototype>();if(prototype!=null)foreach(var leaf in new[]{prototype.bridgeLeft,prototype.bridgeRight})
        {
            if(leaf==null||leaf.GetComponent<BoxCollider2D>()!=null)continue;var r=leaf.GetComponent<SpriteRenderer>();if(r!=null)Surface(leaf,sprite,Vector2.zero,r.bounds.size.x,r.bounds.size.y,prototype.bridgeCollider.usedByEffector&&prototype.bridgeCollider.GetComponent<PlatformEffector2D>()!=null&&prototype.bridgeCollider.GetComponent<PlatformEffector2D>().useOneWay);
        }
        FoundryWallArtBuilder.ApplyVisuals(map);
    }
    static void Surface(Transform owner,Sprite sprite,Vector2 offset,float width,float height,bool pass)
    {
        var old=owner.Find(Skin);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        foreach(var r in owner.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.transform!=owner&&(r.name.Contains("dash")||r.name=="Solid edge")).ToArray())UnityEngine.Object.DestroyImmediate(r.gameObject);
        owner.GetComponent<SpriteRenderer>().color=Steel;
        var root=new GameObject(Skin).transform;root.SetParent(owner,false);root.localPosition=offset;var scale=owner.lossyScale;root.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1);
        void Part(string name,float x,float y,float w,float h,Color color,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=new Vector3(x,y,0);go.transform.localScale=new Vector3(w/sprite.bounds.size.x,h/sprite.bounds.size.y,1);var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;
        }
        float top=height*.5f;
        Part("Metal face inset",0,-.02f,Mathf.Max(.08f,width-.05f),Mathf.Min(.075f,height*.35f),Inset,3);
        Part("Metal lower bevel",0,-height*.5f+.025f,width,.025f,Bevel,3);
        if(width>.7f)foreach(float side in new[]{-1f,1f})Part("Surface fastening",side*(width*.5f-.14f),-.02f,.028f,.028f,Bevel,4);
        if(pass)
        {
            const float spacing=.32f,dash=.2f;int count=Mathf.Max(1,Mathf.FloorToInt((width-.04f)/spacing));float span=(count-1)*spacing+dash;float begin=-span*.5f;
            for(int i=0;i<count;i++)Part("Passable dash",begin+i*spacing+dash*.5f,top-.005f,Mathf.Min(dash,width),.035f,Pass,6);
        }
        else Part("Solid edge",0,top-.005f,width,.035f,Solid,6);
    }
}
