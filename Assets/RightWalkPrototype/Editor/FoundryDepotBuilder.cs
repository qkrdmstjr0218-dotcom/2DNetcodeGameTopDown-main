using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;

// Additive scene edit: keep the existing routes, hazards and hand-tuned geometry.
public static class FoundryDepotBuilder
{
    public const string RootName="Central ammunition depot";
    static Sprite sprite;
    static readonly Color Solid=new(.96f,.63f,.23f);
    [MenuItem("Tools/Right Walk Prototype/Add Foundry Central Depot")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)throw new Exception("Requires saved Foundry edit mode");
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();
        if(map.transform.Find(RootName)!=null)return;
        if((map.ammoSupplies==null||map.ammoSupplies.Length==0)&&(map.shotgunSupplies==null||map.shotgunSupplies.Length!=3||Vector2.Distance(map.shotgunSupplies[2],new Vector2(3,7.75f))>.01f))throw new Exception("Inspect existing supply placement before relocation");
        sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        var root=new GameObject(RootName).transform;root.SetParent(map.transform,false);Undo.RegisterCreatedObjectUndo(root.gameObject,"Add central supply depot");Undo.RecordObject(map,"Relocate central ammunition");
        Part(root,"Storage recess",new Vector2(0,5.15f),new Vector2(8.4f,2.8f),new Color(.055f,.09f,.105f),-78);
        for(int column=-1;column<=1;column++)
        for(int row=0;row<3;row++)
        {
            float x=column*1.22f+(row==1?.12f:0),y=3.98f+row*.68f;
            var crate=Part(root,"Background crate",new Vector2(x,y),new Vector2(1.08f,.6f),new Color(.14f,.21f,.23f),-62);
            Part(crate.transform,"Inset",new Vector2(x,y),new Vector2(.84f,.38f),new Color(.09f,.15f,.17f),-61);
            Part(crate.transform,"Shipping stripe",new Vector2(x-.32f,y),new Vector2(.05f,.38f),new Color(.27f,.31f,.25f),-60);
        }
        foreach(int side in new[]{-1,1})
        {
            Crate(root,"Depot cover "+side,side*2.4f,1.2f,.8f);
            Crate(root,"Depot step "+side,side*3.45f,.8f,.4f);
        }
        Part(root,"Supply pad",new Vector2(0,3.64f),new Vector2(1.2f,.045f),new Color(.25f,.85f,.8f),3);
        var label=new GameObject("Depot sign");label.transform.SetParent(root,false);label.transform.position=new Vector3(0,6.3f,0);
        var text=label.AddComponent<TextMesh>();text.text="AMMO / DEPOT";text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=64;text.characterSize=.045f;text.color=new Color(.35f,.65f,.63f);
        var renderer=label.GetComponent<MeshRenderer>();renderer.sharedMaterial=text.font.material;renderer.sortingOrder=-58;
        FoundryDepotSupplyBuilder.Configure(map);EditorUtility.SetDirty(map);
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    static void Crate(Transform root,string name,float x,float width,float height)
    {
        float floor=3.6f,y=floor+height*.5f;
        var crate=Part(root,name,new Vector2(x,y),new Vector2(width,height),new Color(.30f,.36f,.38f),2);crate.AddComponent<BoxCollider2D>();crate.AddComponent<ArenaCover>();
        Part(crate.transform,"Inset",new Vector2(x,y),new Vector2(width-.16f,height-.15f),new Color(.12f,.21f,.25f),3);
        Part(crate.transform,"Top solid edge",new Vector2(x,floor+height-.012f),new Vector2(width,.025f),Solid,6);
        foreach(int side in new[]{-1,1})Part(crate.transform,"Side solid edge",new Vector2(x+side*(width*.5f-.012f),y),new Vector2(.025f,height),Solid,6);
    }
    static GameObject Part(Transform parent,string name,Vector2 at,Vector2 size,Color color,int order)
    {
        var go=new GameObject(name);go.transform.position=at;go.transform.localScale=new Vector3(size.x,size.y,1);go.transform.SetParent(parent,true);
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=color;renderer.sortingOrder=order;return go;
    }
}
