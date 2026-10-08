using System;
using UnityEngine;
using UnityEditor;
using TopDownShooter.Networking;
public static class InstallCalibratedLegs
{
    public static string Main()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first.");
        const string path="Assets/Art/OperatorPrototype/operator-leg-parts-v2.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var imp=(TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType=TextureImporterType.Default;imp.isReadable=true;imp.alphaIsTransparency=true;
        imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=4096;imp.SaveAndReimport();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=tex.GetPixels32();
        if(pixels[0].a>16)throw new Exception("Expected transparent alpha.");
        float sx=tex.width/1536f,sy=tex.height/1024f;
        Vector2[] start={new Vector2(330,178),new Vector2(747,176),new Vector2(1157,383),new Vector2(287,648),new Vector2(746,673),new Vector2(1275,888)};
        Vector2[] end={new Vector2(267,365),new Vector2(766,386),Vector2.zero,new Vector2(373,858),new Vector2(725,888),Vector2.zero};
        var parts=new Sprite[6];var axes=new float[6];int cw=tex.width/3,ch=tex.height/2;
        const string asset="Assets/Art/OperatorPrototype/operator-leg-parts-v2.asset";
        if(AssetDatabase.LoadMainAssetAtPath(asset)!=null)throw new Exception("Already installed; preserve version.");
        for(int i=0;i<6;i++)
        {
            int left=i%3*cw,bottom=(1-i/3)*ch,x0=tex.width,x1=-1,y0=tex.height,y1=-1;
            for(int y=bottom;y<bottom+ch;y++)for(int x=left;x<left+cw;x++)if(pixels[y*tex.width+x].a>64)
            {x0=Math.Min(x0,x);x1=Math.Max(x1,x);y0=Math.Min(y0,y);y1=Math.Max(y1,y);}
            if(x1<x0)throw new Exception("Missing part "+i);
            var origin=new Vector2(start[i].x*sx,tex.height-start[i].y*sy);
            float ppu;
            if(i%3==2)ppu=(x1-x0+1)/.17f;
            else
            {
                var tip=new Vector2(end[i].x*sx,tex.height-end[i].y*sy);var d=tip-origin;
                ppu=d.magnitude/(i%3==0?.125f:.165f);axes[i]=Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;
            }
            parts[i]=Sprite.Create(tex,new Rect(x0,y0,x1-x0+1,y1-y0+1),new Vector2((origin.x-x0)/(x1-x0+1),(origin.y-y0)/(y1-y0+1)),ppu,0,SpriteMeshType.FullRect);
            if(i==0)AssetDatabase.CreateAsset(parts[i],asset);else AssetDatabase.AddObjectToAsset(parts[i],asset);
            parts[i].name=new[]{"NearThigh","NearShin","NearBoot","FarThigh","FarShin","FarBoot"}[i];EditorUtility.SetDirty(parts[i]);
        }
        var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/SFPlayer.prefab");
        try
        {
            var so=new SerializedObject(root.GetComponent<OperatorAimVisual>());
            var a=so.FindProperty("calibratedLegParts");var angles=so.FindProperty("calibratedLegAxes");a.arraySize=angles.arraySize=6;
            for(int i=0;i<6;i++){a.GetArrayElementAtIndex(i).objectReferenceValue=parts[i];angles.GetArrayElementAtIndex(i).floatValue=axes[i];}
            so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/SFPlayer.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();return "Fixed-scale six leg parts installed; bones .125/.165, independent boots .17 wide.";
    }
}
