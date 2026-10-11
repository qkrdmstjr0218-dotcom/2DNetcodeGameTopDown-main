using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RightWalkPrototype;
public static class LeftRightWalkPreviewBuilder
{
    [MenuItem("Tools/Right Walk Prototype/Build Left Right Preview")]
    public static void Build()
    {
        if(Application.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new System.InvalidOperationException("Stop Play and save the current scene first.");
        EditorSceneManager.OpenScene("Assets/RightWalkPrototype/RightWalkSheetDemo.unity");
        var walker=Object.FindFirstObjectByType<RightFacingWalker>();
        var facing=walker.gameObject.AddComponent<SideFacingPreview>();
        facing.SetFacing(-1);
        walker.GetComponentInChildren<RightFacingAimRig>().ResetAim();
        PrefabUtility.SaveAsPrefabAsset(walker.gameObject,"Assets/RightWalkPrototype/LeftRightWalkPreview.prefab");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/RightWalkPrototype/LeftRightWalkPreview.unity");
        AssetDatabase.SaveAssets();
    }
}
