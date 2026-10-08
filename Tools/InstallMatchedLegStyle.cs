using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TopDownShooter.Diagnostics;
public static class InstallMatchedLegStyle
{
    public static string Main()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        const string path="Assets/Art/OperatorPrototype/operator-joint-parts-matched-v1.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var imp=(TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType=TextureImporterType.Default;imp.isReadable=true;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var studies=UnityEngine.Object.FindObjectsByType<WalkSkeletonStudy>(FindObjectsSortMode.None);
        if(studies.Length==0)throw new Exception("Study scene not open");
        var old=studies[0];
        if(tex.width!=old.artParts[0].texture.width||tex.height!=old.artParts[0].texture.height)throw new Exception("Atlas dimensions changed");
        const string asset="Assets/Art/OperatorPrototype/operator-study-matched-parts-v1.asset";
        if(AssetDatabase.LoadMainAssetAtPath(asset)!=null)throw new Exception("Already installed");
        var parts=new Sprite[6];
        for(int i=0;i<6;i++)
        {
            var source=old.artParts[i];var r=source.rect;
            parts[i]=Sprite.Create(tex,r,new Vector2(source.pivot.x/r.width,source.pivot.y/r.height),source.pixelsPerUnit,0,SpriteMeshType.FullRect);
            parts[i].name="MatchedPart"+i;
            if(i==0)AssetDatabase.CreateAsset(parts[i],asset);else AssetDatabase.AddObjectToAsset(parts[i],asset);
        }
        var cuff=old.ankleCoverSprite;var cr=cuff.rect;
        var cover=Sprite.Create(tex,cr,new Vector2(cuff.pivot.x/cr.width,cuff.pivot.y/cr.height),cuff.pixelsPerUnit,0,SpriteMeshType.FullRect);
        cover.name="MatchedCuff";AssetDatabase.AddObjectToAsset(cover,asset);
        foreach(var s in studies){s.artParts=parts;s.ankleCoverSprite=cover;EditorUtility.SetDirty(s);}
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "Installed matched texture on "+studies.Length+" studies; rects, pivots, bone lengths and ankle values preserved";
    }
}
