using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RightWalkPrototype;

public static class ReferenceCharacterBuilder
{
    private const string Root="Assets/RightWalkPrototype";
    private static Sprite Clone(Sprite source,Texture2D texture,string name,string path,bool first)
    {
        var r=source.rect;
        var s=Sprite.Create(texture,r,new Vector2(source.pivot.x/r.width,source.pivot.y/r.height),source.pixelsPerUnit,0,SpriteMeshType.FullRect);
        s.name=name;
        if(first)AssetDatabase.CreateAsset(s,path);else AssetDatabase.AddObjectToAsset(s,path);
        s.name=name;EditorUtility.SetDirty(s);
        return s;
    }
    public static void Build()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop play first");
        if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save scene first");
        var names=new[]{"NearThigh","NearShin","NearBoot","FarThigh","FarShin","FarBoot"};
        var original=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/OperatorPrototype/operator-leg-parts-v2.asset").OfType<Sprite>().ToArray();
        string legTexturePath=Root+"/Art/CharacterLegParts.png",upperTexturePath=Root+"/Art/CharacterUpperAtlas.png";
        if(AssetDatabase.LoadMainAssetAtPath(legTexturePath)==null)AssetDatabase.CopyAsset("Assets/Art/OperatorPrototype/operator-leg-parts-v2.png",legTexturePath);
        if(AssetDatabase.LoadMainAssetAtPath(upperTexturePath)==null)AssetDatabase.CopyAsset("Assets/Art/OperatorPrototype/operator-aim-eight-v1.png",upperTexturePath);
        var legTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(legTexturePath);
        var upperTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(upperTexturePath);
        string partsPath=Root+"/Art/CharacterParts.asset";
        var sprites=AssetDatabase.LoadAllAssetsAtPath(partsPath).OfType<Sprite>().ToArray();
        if(sprites.Length>0 && !sprites.Any(s=>s.name=="NearThigh"))
        {
            var first=AssetDatabase.LoadAssetAtPath<Sprite>(partsPath);first.name="NearThigh";EditorUtility.SetDirty(first);AssetDatabase.SaveAssets();
        }
        if(sprites.Length==0)
        {
            for(int i=0;i<6;i++)Clone(original.First(s=>s.name==names[i]),legTexture,names[i],partsPath,i==0);
            var upper=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/OperatorPrototype/operator-study-upper-pelvis-v1.asset");
            var pelvis=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/OperatorPrototype/operator-study-upper-pelvis-v1.asset").OfType<Sprite>().First(s=>s.name=="PelvisBridge");
            Clone(upper,upperTexture,"UpperBody",partsPath,false);Clone(pelvis,upperTexture,"Pelvis",partsPath,false);
            AssetDatabase.SaveAssets();
            sprites=AssetDatabase.LoadAllAssetsAtPath(partsPath).OfType<Sprite>().ToArray();
        }
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var character=new GameObject("Right Walking Character");character.AddComponent<SpriteRenderer>();
        var walker=character.AddComponent<RightFacingWalker>();walker.autoPreview=true;walker.moveSpeed=.6f;walker.cycleDistance=1.1f;
        var rig=character.AddComponent<ReferenceLegWalker>();rig.joints=ReferenceLegPoses.Joints;
        rig.material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/Character.mat");
        character.GetComponent<SpriteRenderer>().sharedMaterial=rig.material;
        var art=character.AddComponent<ReferenceCharacterArt>();art.material=rig.material;
        art.legSprites=names.Select(n=>sprites.First(s=>s.name==n)).ToArray();
        art.upperBody=sprites.First(s=>s.name=="UpperBody");art.pelvis=sprites.First(s=>s.name=="Pelvis");
        PrefabUtility.SaveAsPrefabAsset(character,Root+"/RightWalkingCharacter.prefab");
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";
        camera.orthographic=true;camera.orthographicSize=1.6f;camera.transform.position=new Vector3(0,1.1f,-10);
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.075f,.1f);
        camera.gameObject.AddComponent<AudioListener>();new GameObject("Main Light").AddComponent<Light>().type=LightType.Directional;
        var preview=new GameObject("Preview Controls").AddComponent<WalkPreviewControls>();preview.walker=walker;preview.followCamera=camera;
        preview.cameraHeight=1.1f;
        EditorSceneManager.SaveScene(scene,"Assets/RightWalkPrototype/Scenes/Archive/RightWalkingCharacterDemo.unity");AssetDatabase.SaveAssets();
    }
}
