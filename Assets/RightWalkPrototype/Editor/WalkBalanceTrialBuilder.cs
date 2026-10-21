using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;

public static class WalkBalanceTrialBuilder
{
    [MenuItem("Tools/Right Walk Prototype/Build Walk Balance Trial")]
    public static void Build()
    {
        if (Application.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Stop Play and save first.");
        var scene = EditorSceneManager.OpenScene("Assets/RightWalkPrototype/Scenes/Archive/SideScrollStrideTrial.unity");
        EditorSceneManager.SaveScene(scene, "Assets/RightWalkPrototype/Scenes/Archive/SideScrollWalkBalanceTrial.unity");
        var walker = UnityEngine.Object.FindFirstObjectByType<RightFacingWalker>();
        // The upper-body sprites use a ground-level origin; rotate around the pelvis instead.
        PrefabUtility.UnpackPrefabInstance(walker.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        var hip = walker.GetComponentInChildren<RightFacingHipLink>();
        var upper = hip.transform.Find("Upper Body Alignment");
        var pivot = new GameObject("Walk balance - pelvis pivot").transform;
        pivot.SetParent(hip.transform, false);
        pivot.localPosition = new Vector3((252f - 256f) / 204.8f, (499.2f - 250f) / 204.8f, 0);
        upper.SetParent(pivot, true);
        pivot.localRotation = Quaternion.Euler(0, 0, -3f);
        var aim = walker.GetComponentInChildren<RightFacingAimRig>();
        aim.restAngle = 3f;
        aim.ResetAim();
        aim.mouseAim = true;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
