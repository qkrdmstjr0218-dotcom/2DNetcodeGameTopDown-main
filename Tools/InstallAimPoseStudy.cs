using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Import whole poses without deforming or splitting the artwork.
public static class InstallAimPoseStudy
{
    public static string Main(string version = "v1")
    {
        string path = "Assets/Art/OperatorPrototype/operator-aim-pose-study-" + version + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.isReadable = true;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var pixels = texture.GetPixels32();
        var bounds = new RectInt[3];
        int cell = texture.width / 3;
        for (int c = 0; c < 3; c++)
        {
            int xmin = texture.width, xmax = -1, ymin = texture.height, ymax = -1;
            for (int y = 0; y < texture.height; y++)
            for (int x = c * cell; x < (c == 2 ? texture.width : (c + 1) * cell); x++)
                if (pixels[y * texture.width + x].a > 64)
                { xmin = Math.Min(xmin,x); xmax = Math.Max(xmax,x); ymin = Math.Min(ymin,y); ymax = Math.Max(ymax,y); }
            if (xmax < xmin) throw new Exception("Empty pose cell " + c);
            bounds[c] = new RectInt(xmin,ymin,xmax-xmin+1,ymax-ymin+1);
        }
        string assetPath = "Assets/Art/OperatorPrototype/operator-aim-pose-study-" + version + ".asset";
        if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
            throw new Exception("Study already installed; preserve it before creating a new version.");
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            for (int c = 0; c < 3; c++)
            {
                var b = bounds[c];
                var sprite = Sprite.Create(texture,new Rect(b.x,b.y,b.width,b.height),new Vector2(.5f,0),b.height/2.5f,0,SpriteMeshType.FullRect);
                sprite.name = new [] {"Front_Aim", "Right_Aim", "Back_Aim"}[c];
                if (c == 0) AssetDatabase.CreateAsset(sprite,assetPath);
                else AssetDatabase.AddObjectToAsset(sprite,assetPath);
                var go = new GameObject(sprite.name);
                SceneManager.MoveGameObjectToScene(go,scene);
                go.transform.position = new Vector3((c-1)*2.2f,0,0);
                go.AddComponent<SpriteRenderer>().sprite = sprite;
            }
            var cameraObject = new GameObject("AimPoseStudyCamera");
            SceneManager.MoveGameObjectToScene(cameraObject,scene);
            cameraObject.transform.position = new Vector3(0,1.25f,-10);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 1.7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.085f,.105f,.15f);
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/OperatorAimPoseStudy.unity");
            AssetDatabase.SaveAssets();
        }
        finally
        {
            EditorSceneManager.CloseScene(scene,true);
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
        }
        return "Saved three whole static poses and separate OperatorAimPoseStudy scene. Gameplay prefab unchanged.";
    }
}
