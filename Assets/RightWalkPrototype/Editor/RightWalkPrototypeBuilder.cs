using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;

public static class RightWalkPrototypeBuilder
{
    private const string Root = "Assets/RightWalkPrototype";

    [MenuItem("Tools/Right Walk Prototype/Build Demo and Export")]
    public static void Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play before building.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string texturePath = Root + "/Art/RightWalk.png";
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Default;
        importer.isReadable = true;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        string framesPath = Root + "/Art/RightWalkFrames.asset";
        var frames = AssetDatabase.LoadAllAssetsAtPath(framesPath).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (frames.Length == 0)
        {
            frames = new Sprite[6];
            int cell = texture.width / 3;
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = Sprite.Create(texture, new Rect(i % 3 * cell, (1 - i / 3) * cell, cell, cell),
                    new Vector2(.5f, .069010414f), 481.05264f, 0, SpriteMeshType.FullRect);
                frames[i].name = "Walk_" + i;
                if (i == 0) AssetDatabase.CreateAsset(frames[i], framesPath);
                else AssetDatabase.AddObjectToAsset(frames[i], framesPath);
            }
        }
        if (frames.Length != 6) throw new InvalidOperationException("Expected six walk frames.");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var character = new GameObject("Right Facing Walker");
        var renderer = character.AddComponent<SpriteRenderer>();
        renderer.sprite = frames[0];
        string characterMaterialPath = Root + "/Art/Character.mat";
        var characterMaterial = AssetDatabase.LoadAssetAtPath<Material>(characterMaterialPath);
        if (characterMaterial == null)
        {
            characterMaterial = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(characterMaterial, characterMaterialPath);
        }
        renderer.sharedMaterial = characterMaterial;
        renderer.sortingOrder = 10;
        var walker = character.AddComponent<RightFacingWalker>();
        walker.frames = frames;
        PrefabUtility.SaveAsPrefabAsset(character, Root + "/RightFacingWalker.prefab");
        UnityEngine.Object.DestroyImmediate(character);
        character = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/RightFacingWalker.prefab"));
        walker = character.GetComponent<RightFacingWalker>();

        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 1.5f;
        camera.transform.position = new Vector3(0, .45f, -10);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.055f, .075f, .10f);
        camera.gameObject.AddComponent<AudioListener>();
        var light = new GameObject("Main Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(45, -30, 0);
        var preview = new GameObject("Preview Controls").AddComponent<WalkPreviewControls>();
        preview.walker = walker;
        preview.followCamera = camera;
        // Ground grid provides a fixed reference for movement and foot sliding.
        var gridMaterial = new Material(Shader.Find("Sprites/Default"));
        string materialPath = Root + "/Art/Grid.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) == null) AssetDatabase.CreateAsset(gridMaterial, materialPath);
        else { UnityEngine.Object.DestroyImmediate(gridMaterial); gridMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath); }
        for (int i = -8; i <= 8; i++)
        {
            MakeLine("Grid vertical " + i, new Vector3(i * .25f, -2), new Vector3(i * .25f, 3), gridMaterial);
            MakeLine("Grid horizontal " + i, new Vector3(-4, i * .25f), new Vector3(4, i * .25f), gridMaterial);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, Root + "/RightWalkDemo.unity");
        Export();
        Selection.activeGameObject = character;
        Debug.Log("[RightWalkPrototype] Demo, standalone prefab and package created.");
    }

    private static void MakeLine(string name, Vector3 a, Vector3 b, Material material)
    {
        var line = new GameObject(name).AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.startColor = line.endColor = new Color(.11f, .16f, .21f);
        line.startWidth = line.endWidth = .006f;
        line.positionCount = 2;
        line.SetPosition(0, a); line.SetPosition(1, b);
        line.sortingOrder = -10;
    }

    [MenuItem("Tools/Right Walk Prototype/Export Package")]
    public static void Export()
    {
        Directory.CreateDirectory("Exports");
        // Deliberately export only this folder; do not pull the game's settings or network packages in.
        AssetDatabase.ExportPackage(Root, "Exports/RightWalkPrototype.unitypackage", ExportPackageOptions.Recurse);
    }
}
