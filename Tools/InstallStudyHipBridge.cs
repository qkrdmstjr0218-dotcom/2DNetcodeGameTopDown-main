using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TopDownShooter.Diagnostics;
public static class InstallStudyHipBridge
{
    public static string Main()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        const string path="Assets/Art/OperatorPrototype/operator-hip-bridge-v1.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Default;imp.isReadable=true;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=tex.GetPixels32();
        int x0=tex.width,x1=-1,y0=tex.height,y1=-1;
        for(int y=0;y<tex.height;y++)for(int x=0;x<tex.width;x++)if(pixels[y*tex.width+x].a>64){x0=Math.Min(x0,x);x1=Math.Max(x1,x);y0=Math.Min(y0,y);y1=Math.Max(y1,y);}
        if(x1<x0||pixels[0].a>16)throw new Exception("Invalid transparent art");
        var r=new Rect(x0,y0,x1-x0+1,y1-y0+1);
        float cut=r.height*.40f;
        var sprite=Sprite.Create(tex,new Rect(r.x,r.y+cut,r.width,r.height-cut),new Vector2(.5f,(r.height*.60f-cut)/(r.height-cut)),r.width/.26f,0,SpriteMeshType.FullRect);sprite.name="HipBridge";
        const string asset="Assets/Art/OperatorPrototype/operator-study-hip-bridge-v1.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Sprite>(asset);
        if(existing!=null){EditorUtility.CopySerialized(sprite,existing);UnityEngine.Object.DestroyImmediate(sprite);sprite=existing;}else AssetDatabase.CreateAsset(sprite,asset);
        foreach(var s in UnityEngine.Object.FindObjectsByType<WalkSkeletonStudy>(FindObjectsSortMode.None))
        {s.studyPelvis=sprite;s.pelvisArtPosition=new Vector3(0,.425f,0);s.standingPreview=s.animate;EditorUtility.SetDirty(s);}
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "Hip bridge installed, live sample starts standing; walking parameters retained";
    }
}
