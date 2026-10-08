using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TopDownShooter.Diagnostics;
public static class InstallLowerRedraw
{
    public static string Main()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        const string path="Assets/Art/OperatorPrototype/operator-lower-walk-redraw-v1.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Default;imp.isReadable=true;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=4096;imp.SaveAndReimport();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(tex.GetPixels32()[0].a>16)throw new Exception("Not transparent");
        int cw=tex.width/4,ch=tex.height/2;var frames=new Sprite[8];
        const string asset="Assets/Art/OperatorPrototype/operator-lower-walk-redraw-v1.asset";
        if(AssetDatabase.LoadMainAssetAtPath(asset)!=null)throw new Exception("Already installed");
        for(int i=0;i<8;i++)
        {
            frames[i]=Sprite.Create(tex,new Rect(i%4*cw,(1-i/4)*ch,cw,ch),new Vector2(.5f,.5f),ch/.72f,0,SpriteMeshType.FullRect);
            frames[i].name="UnifiedLowerWalk_"+i;if(i==0)AssetDatabase.CreateAsset(frames[i],asset);else AssetDatabase.AddObjectToAsset(frames[i],asset);
        }
        foreach(var s in UnityEngine.Object.FindObjectsByType<WalkSkeletonStudy>(FindObjectsSortMode.None)){s.redrawnFrames=frames;s.showRedrawn=true;s.showBones=false;EditorUtility.SetDirty(s);}
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "Installed 8 unified lower-body frames at shared pivot/scale; research scene only";
    }
}
