using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TopDownShooter.Diagnostics;
public static class InstallStudyJointParts
{
    public static string Main()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        const string path="Assets/Art/OperatorPrototype/operator-joint-parts-v3.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var imp=(TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType=TextureImporterType.Default;imp.isReadable=true;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=tex.GetPixels32();
        if(pixels[0].a>16)throw new Exception("Missing transparent alpha");
        float sx=tex.width/1536f,sy=tex.height/1024f;
        Vector2[] starts={new Vector2(370,45),new Vector2(748,45),new Vector2(1057,355)};
        Vector2[] ends={new Vector2(344,397),new Vector2(747,425),Vector2.zero};
        var parts=new Sprite[3];var axes=new float[3];
        const string asset="Assets/Art/OperatorPrototype/operator-study-joint-parts-v3b.asset";
        if(AssetDatabase.LoadMainAssetAtPath(asset)!=null)throw new Exception("Already installed");
        for(int i=0;i<3;i++)
        {
            int left=Mathf.RoundToInt(new[]{235,648,964}[i]*sx),right=Mathf.RoundToInt(new[]{485,845,1355}[i]*sx),lo=tex.height/2,hi=tex.height;
            // The boot owns the ankle cuff; exclude the shin's lower decorative cuff.
            if(i==1)lo=Mathf.RoundToInt(tex.height-360*sy);
            int x0=tex.width,x1=-1,y0=tex.height,y1=-1;
            for(int y=lo;y<hi;y++)for(int x=left;x<right;x++)if(pixels[y*tex.width+x].a>64)
            {x0=Math.Min(x0,x);x1=Math.Max(x1,x);y0=Math.Min(y0,y);y1=Math.Max(y1,y);}
            if(x1<x0)throw new Exception("Empty part");
            var origin=new Vector2(starts[i].x*sx,tex.height-starts[i].y*sy);
            float ppu=(x1-x0+1)/.17f;
            if(i<2){var tip=new Vector2(ends[i].x*sx,tex.height-ends[i].y*sy);var d=tip-origin;ppu=d.magnitude/(i==0?.22f:.24f);axes[i]=Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;}
            parts[i]=Sprite.Create(tex,new Rect(x0,y0,x1-x0+1,y1-y0+1),new Vector2((origin.x-x0)/(x1-x0+1),(origin.y-y0)/(y1-y0+1)),ppu,0,SpriteMeshType.FullRect);
            parts[i].name=new[]{"ThighWithSingleKnee","ShinWithoutAnkleCuff","BootWithSingleAnkle"}[i];
            if(i==0)AssetDatabase.CreateAsset(parts[i],asset);else AssetDatabase.AddObjectToAsset(parts[i],asset);
        }
        foreach(var s in UnityEngine.Object.FindObjectsByType<WalkSkeletonStudy>(FindObjectsSortMode.None))
        {
            s.artParts=new Sprite[6];s.artAxes=new float[6];s.nativeStudyScale=true;s.nearToeYaw=0;
            for(int i=0;i<6;i++){s.artParts[i]=parts[i%3];s.artAxes[i]=axes[i%3];}
            EditorUtility.SetDirty(s);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "Installed same joint geometry for both legs; single knee/ankle ownership, native fixed scale";
    }
}
