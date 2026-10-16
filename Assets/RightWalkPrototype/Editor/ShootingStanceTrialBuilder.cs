using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;

public static class ShootingStanceTrialBuilder
{
    private const string Root = "Assets/RightWalkPrototype/";
    [MenuItem("Tools/Right Walk Prototype/Build Shooting Stance Trial")]
    public static void Build()
    {
        if (Application.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Stop Play and save first.");
        var first = LoadFrames(0);
        var second = LoadFrames(1);
        var scene = EditorSceneManager.OpenScene(Root + "SideScrollStrideTrial.unity");
        EditorSceneManager.SaveScene(scene, Root + "SideScrollStanceTrial.unity");
        var control = UnityEngine.Object.FindFirstObjectByType<SideScrollMapPreview>();
        var stance = control.gameObject.AddComponent<ShootingStanceTrial>();
        stance.UseStance = false;
        stance.firstPlantedTransition = first;
        stance.secondPlantedTransition = second;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static Sprite[] LoadFrames(int variant)
    {
        string png = Root + "Art/ShootingStanceTrial-" + variant + ".png";
        AssetDatabase.ImportAsset(png, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(png);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();
        string path = Root + "Art/ShootingStanceTrialFrames-" + variant + ".asset";
        var frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (frames.Length == 25) return frames;
        if (frames.Length != 0) throw new InvalidOperationException("Expected 25 stance samples.");
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(png);
        frames = new Sprite[25];
        for (int i = 0; i < frames.Length; i++)
        {
            var sprite = Sprite.Create(texture, new Rect(i % 5 * 512, (4 - i / 5) * 512, 512, 512),
                new Vector2(.5f, .025f), 204.8f, 0, SpriteMeshType.FullRect);
            sprite.name = "Stance_" + i.ToString("D2");
            if (i == 0) AssetDatabase.CreateAsset(sprite, path);
            else AssetDatabase.AddObjectToAsset(sprite, path);
            frames[i] = sprite;
            EditorUtility.SetDirty(sprite);
        }
        return frames;
    }
}
