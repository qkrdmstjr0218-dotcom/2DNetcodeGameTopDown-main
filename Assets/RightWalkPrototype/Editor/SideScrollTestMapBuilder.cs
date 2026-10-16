using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RightWalkPrototype;
public static class SideScrollTestMapBuilder
{
    private const string Root="Assets/RightWalkPrototype";
    [MenuItem("Tools/Right Walk Prototype/Build Side Scroll Test Map")]
    public static void Build()
    {
        if(Application.isPlaying || EditorSceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Stop Play and save first.");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/MapSolid.png");
        if(texture==null)
        {
            texture=new Texture2D(2,2);texture.SetPixels(new Color[]{Color.white,Color.white,Color.white,Color.white});texture.Apply();
            System.IO.File.WriteAllBytes(Root+"/Art/MapSolid.png",texture.EncodeToPNG());AssetDatabase.ImportAsset(Root+"/Art/MapSolid.png");
            var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/Art/MapSolid.png");importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=2;importer.SaveAndReimport();
        }
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/MapSolid.png");
        Block(sprite,"Background",new Vector2(0,3),new Vector2(40,16),new Color(.13f,.19f,.25f),-100,false);
        for(int i=-5;i<=5;i++)Block(sprite,"Distant column "+i,new Vector2(i*3,2),new Vector2(1.5f,5),new Color(.17f,.24f,.30f),-90,false);
        Block(sprite,"Ground",new Vector2(0,-.5f),new Vector2(30,1),new Color(.29f,.35f,.38f),-2,true);
        Block(sprite,"Ground edge",new Vector2(0,-.025f),new Vector2(30,.05f),new Color(.52f,.65f,.65f),-1,false);
        Block(sprite,"Left boundary",new Vector2(-12,2),new Vector2(.5f,4),new Color(.3f,.42f,.46f),0,true);
        Block(sprite,"Right boundary",new Vector2(12,2),new Vector2(.5f,4),new Color(.3f,.42f,.46f),0,true);
        Block(sprite,"Low step",new Vector2(2,.15f),new Vector2(1.5f,.3f),new Color(.36f,.48f,.50f),0,true);
        Block(sprite,"Platform",new Vector2(5,.55f),new Vector2(2,.25f),new Color(.36f,.48f,.50f),0,true);
        Block(sprite,"Left step",new Vector2(-5,.25f),new Vector2(2,.5f),new Color(.36f,.48f,.50f),0,true);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/LeftRightWalkPreview.prefab");
        var character=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        character.name="Side Scroll Test Character";character.transform.position=new Vector3(-1,.03f,0);character.layer=2;
        character.GetComponent<SideFacingPreview>().SetFacing(1);
        var walker=character.GetComponent<RightFacingWalker>();walker.externalMovement=true;walker.autoPreview=false;
        var body=character.AddComponent<Rigidbody2D>();body.freezeRotation=true;body.gravityScale=1.5f;body.interpolation=RigidbodyInterpolation2D.Interpolate;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
        // The accepted sheet's planted soles are about .152 world units above its pivot.
        var collider=character.AddComponent<BoxCollider2D>();collider.size=new Vector2(.48f,2.15f);collider.offset=new Vector2(0,1.23f);
        var physics=new PhysicsMaterial2D("Preview no friction");physics.friction=0;physics.bounciness=0;
        string mat=Root+"/Art/MapNoFriction.physicsMaterial2D";var existing=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(mat);if(existing==null){AssetDatabase.CreateAsset(physics,mat);existing=physics;}collider.sharedMaterial=existing;
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=3;camera.transform.position=new Vector3(-1,1.8f,-10);camera.backgroundColor=new Color(.13f,.19f,.25f);camera.clearFlags=CameraClearFlags.SolidColor;camera.gameObject.AddComponent<AudioListener>();
        new GameObject("Main Light").AddComponent<Light>().type=LightType.Directional;
        var control=character.AddComponent<SideScrollMapPreview>();control.walker=walker;control.followCamera=camera;
        walker.GetComponentInChildren<RightFacingAimRig>().mouseAim=true;
        walker.GetComponentInChildren<RightFacingAimRig>().maxAngle=90f;
        EditorSceneManager.SaveScene(scene,Root+"/SideScrollTestMap.unity");AssetDatabase.SaveAssets();
    }
    private static void Block(Sprite sprite,string name,Vector2 center,Vector2 size,Color color,int order,bool solid)
    {
        var go=new GameObject(name);go.transform.position=center;go.transform.localScale=new Vector3(size.x,size.y,1);
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=color;renderer.sortingOrder=order;
        if(solid)go.AddComponent<BoxCollider2D>();
    }
}
