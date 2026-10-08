using System;
using UnityEngine;
using UnityEditor;
using TopDownShooter.Networking;
public static class InstallIntegratedAim
{
    public static string Main(string version="v1", bool diagonalsOnly=false)
    {
        if(Application.isPlaying) throw new Exception("Stop Play before updating the player prefab.");
        string path="Assets/Art/OperatorPrototype/operator-aim-eight-"+version+".png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Default; importer.isReadable=true;
        importer.alphaIsTransparency=true; importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.maxTextureSize=4096;
        importer.SaveAndReimport();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path); var pixels=tex.GetPixels32();
        int cw=tex.width/4,ch=tex.height/2; var rects=new RectInt[8];
        for(int i=0;i<8;i++)
        {
            int cx=i%4,cy=1-i/4,minX=tex.width,maxX=-1,minY=tex.height,maxY=-1;
            for(int y=cy*ch;y<(cy+1)*ch;y++) for(int x=cx*cw;x<(cx+1)*cw;x++)
                if(pixels[y*tex.width+x].a>64) { minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y); }
            if(maxX<minX) throw new Exception("Empty atlas cell "+i);
            rects[i]=new RectInt(minX,minY,maxX-minX+1,maxY-minY+1);
        }
        string asset="Assets/Art/OperatorPrototype/operator-aim-eight-"+version+".asset";
        if(AssetDatabase.LoadMainAssetAtPath(asset)!=null) throw new Exception("Preserve installed atlas before replacement.");
        var sprites=new Sprite[8];
        for(int i=0;i<8;i++)
        {
            var b=rects[i]; double sum=0,weight=0;
            // Center on the feet, not on a projecting barrel or ponytail.
            for(int y=b.y;y<b.y+Math.Max(1,b.height/5);y++) for(int x=b.x;x<b.xMax;x++)
            { var a=pixels[y*tex.width+x].a; if(a>64) { sum+=x*a;weight+=a; } }
            float feetX=weight>0?(float)(sum/weight):b.center.x;
            sprites[i]=Sprite.Create(tex,new Rect(b.x,b.y,b.width,b.height),new Vector2((feetX-b.x)/b.width,0),b.height/.95f,0,SpriteMeshType.FullRect);
            sprites[i].name=new[]{"E","NE","N","NW","W","SW","S","SE"}[i];
            if(i==0) AssetDatabase.CreateAsset(sprites[i],asset); else AssetDatabase.AddObjectToAsset(sprites[i],asset);
        }
        var prefab=PrefabUtility.LoadPrefabContents("Assets/Prefabs/SFPlayer.prefab");
        try
        {
            var so=new SerializedObject(prefab.GetComponent<OperatorAimVisual>());
            var a=so.FindProperty("integratedAimPoses");a.arraySize=8;
            for(int i=0;i<8;i++)
            {
                if(diagonalsOnly && i!=1 && i!=3) continue;
                var old=a.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                if(diagonalsOnly && old!=null)
                {
                    // Keep character scale even when the corrected barrel extends above the head.
                    var b=rects[i]; var pivot=sprites[i].pivot;
                    float ppu=old.pixelsPerUnit*((float)tex.width/old.texture.width);
                    sprites[i]=Sprite.Create(tex,new Rect(b.x,b.y,b.width,b.height),new Vector2(pivot.x/b.width,0),ppu,0,SpriteMeshType.FullRect);
                    sprites[i].name=i==1?"NE_Corrected":"NW_Corrected";
                    AssetDatabase.AddObjectToAsset(sprites[i],asset);
                }
                a.GetArrayElementAtIndex(i).objectReferenceValue=sprites[i];
            }
            so.FindProperty("useIntegratedAim").boolValue=true;
            so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Prefabs/SFPlayer.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        AssetDatabase.SaveAssets();return "Installed eight whole aiming poses; previous modular rig remains selectable in Inspector.";
    }
}
