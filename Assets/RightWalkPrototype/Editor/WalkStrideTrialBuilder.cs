using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;

public static class WalkStrideTrialBuilder
{
    private const string Root = "Assets/RightWalkPrototype/";

    [MenuItem("Tools/Right Walk Prototype/Build Long Stride Trial")]
    public static void Build()
    {
        if (Application.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Stop Play and save the current scene first.");
        string png = Root + "Art/WalkStrideTrial.png";
        AssetDatabase.ImportAsset(png, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(png);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(png);
        string asset = Root + "Art/WalkStrideTrialFrames.asset";
        var frames = AssetDatabase.LoadAllAssetsAtPath(asset).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (frames.Length == 0)
        {
            frames = new Sprite[40];
            for (int i = 0; i < frames.Length; i++)
            {
                var sprite = Sprite.Create(texture, new Rect(i % 5 * 512, (7 - i / 5) * 512, 512, 512),
                    new Vector2(.5f, .025f), 204.8f, 0, SpriteMeshType.FullRect);
                sprite.name = "Stride_" + i.ToString("D2");
                if (i == 0) AssetDatabase.CreateAsset(sprite, asset);
                else AssetDatabase.AddObjectToAsset(sprite, asset);
                frames[i] = sprite;
                EditorUtility.SetDirty(sprite);
            }
        }
        if (frames.Length != 40) throw new InvalidOperationException("Expected forty trial frames.");
        var scene = EditorSceneManager.OpenScene(Root + "SideScrollTestMap.unity");
        // Save the copied map before applying experimental overrides.
        EditorSceneManager.SaveScene(scene, Root + "SideScrollStrideTrial.unity");
        var control = UnityEngine.Object.FindFirstObjectByType<SideScrollMapPreview>();
        control.speed = 1.6f;
        var walker = control.walker;
        walker.frames = frames;
        walker.frameWeights = Enumerable.Repeat(1f, 40).ToArray();
        walker.cycleDistance = 150f / 204.8f / .6f;
        var hip = walker.GetComponentInChildren<RightFacingHipLink>();
        hip.meanDropPixels = 4f;
        hip.dropAmplitudePixels = 6f;
        walker.GetComponent<SpriteRenderer>().sprite = frames[0];
        hip.ApplyFrame(0);
        walker.GetComponent<RightFacingPelvisPreview>().UpdateGeometry();
        PrefabUtility.RecordPrefabInstancePropertyModifications(walker);
        PrefabUtility.RecordPrefabInstancePropertyModifications(hip);
        PrefabUtility.RecordPrefabInstancePropertyModifications(walker.GetComponent<SpriteRenderer>());
        PrefabUtility.RecordPrefabInstancePropertyModifications(hip.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }
}
