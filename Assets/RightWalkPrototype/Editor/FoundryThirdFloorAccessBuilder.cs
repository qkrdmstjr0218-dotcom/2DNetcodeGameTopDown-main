using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;

// Central 3F -> 4F ascent only. The lower 2F -> 3F shortcut stays unchanged.
public static class FoundryThirdFloorAccessBuilder
{
    public const string RootName="Central crown ascent final steps";
    public static void Configure(ArenaMapSettings map)
    {
        var previous=map.transform.Find(RootName);if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var root=new GameObject(RootName).transform;root.SetParent(map.transform,false);
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        if(sprite==null)throw new Exception("Missing deck sprite");
        GameObject Part(Transform parent,string name,Vector2 position,Vector2 size,Color color,int order)
        {
            var go=new GameObject(name);go.transform.position=position;go.transform.localScale=new Vector3(size.x,size.y,1);go.transform.SetParent(parent,true);
            var image=go.AddComponent<SpriteRenderer>();image.sprite=sprite;image.color=color;image.sortingOrder=order;return go;
        }
        void Dashes(Transform step,float x,float top,float width)
        {
            for(int i=step.childCount-1;i>=0;i--)if(step.GetChild(i).name=="Passable dash")UnityEngine.Object.DestroyImmediate(step.GetChild(i).gameObject);
            for(float d=-width*.5f;d<width*.5f-.01f;d+=.32f){float span=Mathf.Min(.2f,width*.5f-d);Part(step,"Passable dash",new Vector2(x+d+span*.5f,top),new Vector2(span,.035f),new Color(.22f,.8f,.77f),6);}
        }
        void MoveStep(Transform step,float x,float top,float width)
        {
            if(step==null)throw new Exception("Missing central ascent rung");step.position=new Vector3(x,top-.1f,0);step.localScale=new Vector3(width,.2f,1);Dashes(step,x,top,width);
        }
        foreach(int side in new[]{-1,1})
        {
            MoveStep(map.transform.Find(FoundryUpperRouteBuilder.RootName+"/Third floor step "+side),side*FoundryUpperRouteBuilder.LowerStepX,FoundryUpperRouteBuilder.LowerStepHeight(map),3);
            MoveStep(map.transform.Find(FoundryLayoutBuilder.RootName+"/Central ascent "+side),side*2.1f,FoundryUpperRouteBuilder.UpperStepHeight(map),FoundryUpperRouteBuilder.UpperStepWidth);
            float top=map.floorSpacing*2.75f,x=side*1.2f;
            var final=Part(root,"Crown ascent final "+side,new Vector2(x,top-.1f),new Vector2(1.8f,.2f),new Color(.2f,.28f,.33f),1);
            final.AddComponent<BoxCollider2D>().usedByEffector=true;var e=final.AddComponent<PlatformEffector2D>();e.useOneWay=true;e.surfaceArc=160;e.useSideFriction=false;e.useSideBounce=false;Dashes(final.transform,x,top,1.8f);
        }
    }
    [MenuItem("Tools/Right Walk Prototype/Refine Central Crown Ascent")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)throw new Exception("Requires saved Foundry edit mode");
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();Undo.RegisterFullObjectHierarchyUndo(map.gameObject,"Refine central crown ascent");Configure(map);
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
}
