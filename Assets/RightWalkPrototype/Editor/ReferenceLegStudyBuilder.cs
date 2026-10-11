using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RightWalkPrototype;

public static class ReferenceLegStudyBuilder
{
    public static void Build()
    {
        if(Application.isPlaying) throw new System.InvalidOperationException("Stop Play first.");
        if(EditorSceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Save current scene before building.");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var body=new GameObject("Reference Leg Walker");
        body.AddComponent<SpriteRenderer>();
        var movement=body.AddComponent<RightFacingWalker>();
        movement.autoPreview=true;movement.moveSpeed=.7f;movement.cycleDistance=.9f;
        var rig=body.AddComponent<ReferenceLegWalker>();
        rig.material=AssetDatabase.LoadAssetAtPath<Material>("Assets/RightWalkPrototype/Art/Character.mat");
        body.GetComponent<SpriteRenderer>().sharedMaterial=rig.material;
        rig.joints=ReferenceLegPoses.Joints;
        PrefabUtility.SaveAsPrefabAsset(body,"Assets/RightWalkPrototype/ReferenceLegWalker.prefab");
        var camera=new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=1.4f;
        camera.transform.position=new Vector3(0,.45f,-10);
        camera.backgroundColor=new Color(.055f,.075f,.1f);camera.clearFlags=CameraClearFlags.SolidColor;
        camera.gameObject.AddComponent<AudioListener>();
        new GameObject("Main Light").AddComponent<Light>().type=LightType.Directional;
        var preview=new GameObject("Preview Controls").AddComponent<WalkPreviewControls>();
        preview.walker=movement;preview.followCamera=camera;
        var ground=new GameObject("Ground Reference").AddComponent<LineRenderer>();
        ground.sharedMaterial=rig.material;ground.positionCount=2;ground.SetPosition(0,new Vector3(-10,0));ground.SetPosition(1,new Vector3(10,0));
        ground.startWidth=ground.endWidth=.01f;ground.startColor=ground.endColor=Color.gray;
        EditorSceneManager.SaveScene(scene,"Assets/RightWalkPrototype/ReferenceLegStudy.unity");
        AssetDatabase.SaveAssets();
    }
}
