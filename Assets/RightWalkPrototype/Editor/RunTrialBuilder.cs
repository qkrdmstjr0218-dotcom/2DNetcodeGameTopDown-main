using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;

public static class RunTrialBuilder
{
    private const string Root = "Assets/RightWalkPrototype/";
    [MenuItem("Tools/Right Walk Prototype/Build Run Trial")]
    public static void Build()
    {
        BuildVariant(false);
    }

    [MenuItem("Tools/Right Walk Prototype/Build Run Recovery Trial")]
    public static void BuildRecovery()
    {
        BuildVariant(true);
    }

    private static void BuildVariant(bool recovery)
    {
        if (Application.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Stop Play and save first.");
        string name = recovery ? "RunRecoveryTrial" : "RunTrial";
        string png = Root + "Art/" + name + ".png";
        AssetDatabase.ImportAsset(png, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(png);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();
        string path = Root + "Art/" + name + "Frames.asset";
        var frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (frames.Length == 0)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(png);
            frames = new Sprite[40];
            for (int i = 0; i < frames.Length; i++)
            {
                var s = Sprite.Create(texture, new Rect(i % 5 * 512, (7 - i / 5) * 512, 512, 512),
                    new Vector2(.5f, .025f), 204.8f, 0, SpriteMeshType.FullRect);
                s.name = "Run_" + i.ToString("D2");
                if (i == 0) AssetDatabase.CreateAsset(s, path);
                else AssetDatabase.AddObjectToAsset(s, path);
                frames[i] = s;
                EditorUtility.SetDirty(s);
            }
        }
        if (frames.Length != 40) throw new InvalidOperationException("Expected 40 run poses.");
        var scene = EditorSceneManager.OpenScene(Root + "SideScrollStrideTrial.unity");
        EditorSceneManager.SaveScene(scene, Root + (recovery ? "SideScrollRunRecoveryTrial.unity" : "SideScrollRunTrial.unity"));
        var control = UnityEngine.Object.FindFirstObjectByType<SideScrollMapPreview>();
        var run = control.gameObject.AddComponent<RunWalkPreview>();
        run.runFrames = frames;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }
}
