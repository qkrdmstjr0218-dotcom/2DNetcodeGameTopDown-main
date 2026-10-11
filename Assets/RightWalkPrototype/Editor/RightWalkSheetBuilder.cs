using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RightWalkPrototype;

public static class RightWalkSheetBuilder
{
    private const string Root="Assets/RightWalkPrototype";
    [MenuItem("Tools/Right Walk Prototype/Build Sprite Sheet Demo")]
    public static void Build()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
        if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save current scene first.");
        string png=Root+"/Art/RightWalkSheet-Legs-FinalContour-v72.png";
        AssetDatabase.ImportAsset(png,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(png);
        importer.textureType=TextureImporterType.Default;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=4096;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(png);
        float width=texture.width/5f,height=texture.height/4f;
        string path=Root+"/Art/RightWalkSheetFrames-Legs-FinalContour-v72.asset";
        var frames=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        if(frames.Length==0)
        {
            frames=new Sprite[20];
            for(int i=0;i<20;i++)
            {
                var s=Sprite.Create(texture,new Rect(i%5*width,(3-i/5)*height,width,height),
                    new Vector2(.5f,.025f),height/2.5f,0,SpriteMeshType.FullRect);
                if(i==0)AssetDatabase.CreateAsset(s,path);else AssetDatabase.AddObjectToAsset(s,path);
                s.name="Walk_"+i.ToString("D2");EditorUtility.SetDirty(s);frames[i]=s;
            }
        }
        if(frames.Length!=20)throw new InvalidOperationException("Expected twenty sprite frames.");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var character=new GameObject("Twenty Frame Sprite Walker");
        var renderer=character.AddComponent<SpriteRenderer>();renderer.sprite=frames[0];renderer.sortingOrder=10;
        renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/Character.mat");
        var walker=character.AddComponent<RightFacingWalker>();walker.frames=frames;
        // Uniform samples of a continuous cycle; neither leg has extra endpoint holds.
        walker.frameWeights=Enumerable.Repeat(1f,20).ToArray();
        walker.autoPreview=true;walker.moveSpeed=.8f;walker.cycleDistance=100f / frames[0].pixelsPerUnit / .6f;
        // v39: align the complete upper body to the accepted pelvis and leg placement.
        var hip=new GameObject("Pelvis and Waist Root");hip.transform.SetParent(character.transform,false);
        var hipLink=hip.AddComponent<RightFacingHipLink>();hipLink.walker=walker;
        Layer(hip.transform,"Pelvis Underlay","RightWalk-Pelvis-Underlay-v46",Vector3.zero,8,renderer.sharedMaterial);
        Layer(hip.transform,"Hip Clothing","RightWalk-Hip-Clothing-v43",Vector3.zero,11,renderer.sharedMaterial);
        var upperBody=new GameObject("Upper Body Alignment");upperBody.transform.SetParent(hip.transform,false);
        upperBody.transform.localPosition=new Vector3(10f/204.8f,0,0);
        var aim = new GameObject("Aim Rig");aim.transform.SetParent(upperBody.transform,false);
        var aimRig=aim.AddComponent<RightFacingAimRig>();
        var torsoLayer=Layer(upperBody.transform,"Torso and Hip Clothing","RightWalk-Torso-v42",Vector3.zero,12,renderer.sharedMaterial);
        var neckBase=Pixel(252,145);var headPivot=Pixel(249,132);aimRig.headFollow=.65f;aimRig.neckFollow=.2f;
        var neck=new GameObject("Neck Base Pivot");neck.transform.SetParent(upperBody.transform,false);neck.transform.localPosition=neckBase+new Vector3(-8f/204.8f,-4f/204.8f,0);aimRig.neck=neck.transform;
        var head=new GameObject("Head Pivot");head.transform.SetParent(neck.transform,false);head.transform.localPosition=headPivot-neckBase;
        var headFrontMaterial=HeadSplitMaterial("RightWalk-HeadFront-v88",false);
        var headRearMaterial=HeadSplitMaterial("RightWalk-HeadRear-v88",true);
        Layer(head.transform,"Head and Front Hair","RightWalk-HeadFace-v46",-headPivot,19,headFrontMaterial);
        Layer(head.transform,"Rear Hair behind Collar","RightWalk-HeadFace-v46",-headPivot,11,headRearMaterial);
        aimRig.head=head.transform;aimRig.headRestPosition=head.transform.localPosition;
        var skinObject=new GameObject("Neck Skin Connection");skinObject.transform.SetParent(neck.transform,false);var skin=skinObject.AddComponent<RightFacingNeckSkin>();skin.head=head.transform;skin.material=renderer.sharedMaterial;aimRig.neckSkin=skinObject.transform;skin.Rebuild();
        var aimPivot=Pixel(255,175);aim.transform.localPosition=aimPivot;
        // Hands and weapon share a pose so the fingers cannot drift off the grip.
        var weaponLayer=Layer(aim.transform,"Weapon and Gripping Hands","RightWalk-WeaponHands-v44",-aimPivot,17,renderer.sharedMaterial);
        weaponLayer.transform.localPosition+=new Vector3(-15f/204.8f,-24f/204.8f,0);
        var muzzle=new GameObject("Muzzle");muzzle.transform.SetParent(weaponLayer.transform,false);
        muzzle.transform.localPosition=Pixel(464.15f,143.84f);aimRig.muzzle=muzzle.transform;
        var nearGrip=new GameObject("Trigger Hand Wrist");nearGrip.transform.SetParent(weaponLayer.transform,false);nearGrip.transform.localPosition=Pixel(311.45f,166.33f);
        var farGrip=new GameObject("Support Hand Wrist");farGrip.transform.SetParent(weaponLayer.transform,false);farGrip.transform.localPosition=Pixel(392f,165.32f);
        CreateArm(upperBody.transform,"Near",Pixel(234,164),Pixel(265,194),Pixel(338,180),nearGrip.transform,18,renderer.sharedMaterial);
        CreateArm(upperBody.transform,"Far",Pixel(250,160),Pixel(313,184),Pixel(389,173),farGrip.transform,11,renderer.sharedMaterial);
        // The far upper-arm cutout includes a collar fragment; keep it behind neck skin.
        GameObject.Find("Far UpperArm").GetComponent<SpriteRenderer>().sortingOrder=8;
        // The white waist is baked into Legs (order 10), so the far forearm belongs behind it.
        GameObject.Find("Far Forearm").GetComponent<SpriteRenderer>().sortingOrder=9;
        Layer(upperBody.transform,"Collar Front","RightWalk-CollarFront-v45",Vector3.zero,16,renderer.sharedMaterial);
        var edgePath=Root+"/Art/LegEdge-v42.mat";
        var edgeMaterial=AssetDatabase.LoadAssetAtPath<Material>(edgePath);
        if(edgeMaterial==null){edgeMaterial=new Material(Shader.Find("RightWalk/LegEdge"));AssetDatabase.CreateAsset(edgeMaterial,edgePath);}
        renderer.sharedMaterial=edgeMaterial;
        var pelvisPreview=character.AddComponent<RightFacingPelvisPreview>();pelvisPreview.spreadPixels=3f;pelvisPreview.UpdateGeometry();
        PrefabUtility.SaveAsPrefabAsset(character,Root+"/RightWalkSheetWalker.prefab");
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";
        camera.orthographic=true;camera.orthographicSize=1.55f;camera.transform.position=new Vector3(0,1.1f,-10);
        camera.backgroundColor=new Color(.055f,.075f,.1f);camera.clearFlags=CameraClearFlags.SolidColor;
        camera.gameObject.AddComponent<AudioListener>();
        new GameObject("Main Light").AddComponent<Light>().type=LightType.Directional;
        var preview=new GameObject("Preview Controls").AddComponent<WalkPreviewControls>();
        preview.walker=walker;preview.followCamera=camera;preview.cameraHeight=1.1f;preview.SetWhiteBackground(true);
        EditorSceneManager.SaveScene(scene,Root+"/RightWalkSheetDemo.unity");AssetDatabase.SaveAssets();
    }
    private static Material HeadSplitMaterial(string name,bool rear)
    {
        string path=Root+"/Art/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("RightWalk/HeadSplit"));AssetDatabase.CreateAsset(material,path);}
        material.SetFloat("_RearLayer",rear?1f:0f);EditorUtility.SetDirty(material);
        return material;
    }
    private static Vector3 Pixel(float x,float y) { return new Vector3((x-256f)/204.8f,(499.2f-y)/204.8f,0); }
    private static GameObject Layer(Transform parent,string label,string asset,Vector3 offset,int order,Material material)
    {
        var obj=new GameObject(label);obj.transform.SetParent(parent,false);obj.transform.localPosition=offset;
        var r=obj.AddComponent<SpriteRenderer>();r.sprite=LoadLayer(asset);r.sortingOrder=order;r.sharedMaterial=material;return obj;
    }
    private static void CreateArm(Transform parent,string side,Vector3 shoulder,Vector3 elbow,Vector3 wrist,Transform grip,int order,Material material)
    {
        var arm=new GameObject(side+" Arm IK");arm.transform.SetParent(parent,false);
        // Register the near shoulder to the torso socket, while keeping its bone lengths.
        arm.transform.localPosition=side=="Near" ? new Vector3(-7f/204.8f,-4f/204.8f,0) : new Vector3(3f/204.8f,-3f/204.8f,0);
        var ik=arm.AddComponent<RightFacingArmIK>();ik.shoulder=shoulder;ik.referenceElbow=elbow;ik.referenceWrist=wrist;ik.grip=grip;
        ik.handContactOffset=Vector2.zero;
        string[] names={"UpperArm","Forearm","Hand"};Vector3[] points={shoulder,elbow,wrist};var nodes=new Transform[3];
        for(int i=0;i<3;i++){
            var node=new GameObject(side+" "+names[i]+" Joint");node.transform.SetParent(arm.transform,false);node.transform.localPosition=points[i];nodes[i]=node.transform;
            // The hands are already rendered with the weapon; this third node tracks the wrist.
            if(i<2)Layer(node.transform,side+" "+names[i],"RightWalk-"+side+names[i]+(i==1 ? "-v43" : side=="Near" ? "-v47" : "-v48"),-points[i],order,material);
        }
        ik.upperArm=nodes[0];ik.forearm=nodes[1];ik.hand=nodes[2];ik.Solve();
    }
    private static Sprite LoadLayer(string name)
    {
        string png=Root+"/Art/"+name+".png";
        AssetDatabase.ImportAsset(png,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(png);
        importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
        string path=Root+"/Art/"+name+".asset";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(sprite==null){var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(png);sprite=Sprite.Create(tex,new Rect(0,0,512,512),new Vector2(.5f,.025f),204.8f,0,SpriteMeshType.FullRect);AssetDatabase.CreateAsset(sprite,path);}
        return sprite;
    }

}









































