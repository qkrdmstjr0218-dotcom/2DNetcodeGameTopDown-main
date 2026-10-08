using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using TopDownShooter.Networking;
public static class InstallRightWalk
{
    public static string Main(string version="v1",int columns=3,int rows=2)
    {
        if(Application.isPlaying) throw new Exception("Stop Play first.");
        string path="Assets/Art/OperatorPrototype/operator-right-walk-"+version+".png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var imp=(TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType=TextureImporterType.Default;imp.isReadable=true;imp.alphaIsTransparency=true;
        imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=4096;imp.SaveAndReimport();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=tex.GetPixels32();
        if(pixels[0].a>16) throw new Exception("Walk atlas needs transparent background.");
        int count=columns*rows;
        int cw=tex.width/columns,ch=tex.height/rows;var floors=new int[count];var heights=new int[count];
        for(int i=0;i<count;i++)
        {
            int bottom=(rows-1-i/columns)*ch,left=(i%columns)*cw,min=ch,max=-1;
            for(int y=0;y<ch;y++) for(int x=left;x<left+cw;x++)
                if(pixels[(bottom+y)*tex.width+x].a>64){min=Math.Min(min,y);max=Math.Max(max,y);}
            if(max<min) throw new Exception("Missing walk frame "+i);
            floors[i]=min;heights[i]=max-min+1;
        }
        // Shared cell origin and scale retain foot travel; do not re-center each frame on its feet.
        float baseline=(float)floors.Average(),ppu=(float)heights.Average()/.95f;
        string asset="Assets/Art/OperatorPrototype/operator-right-walk-"+version+".asset";
        if(AssetDatabase.LoadMainAssetAtPath(asset)!=null) throw new Exception("Already installed; create a new version instead.");
        var frames=new Sprite[count];
        for(int i=0;i<count;i++)
        {
            frames[i]=Sprite.Create(tex,new Rect((i%columns)*cw,(rows-1-i/columns)*ch,cw,ch),new Vector2(.5f,baseline/ch),ppu,0,SpriteMeshType.FullRect);
            if(i==0)AssetDatabase.CreateAsset(frames[i],asset);else AssetDatabase.AddObjectToAsset(frames[i],asset);
            frames[i].name="RightWalk_"+i;EditorUtility.SetDirty(frames[i]);
        }
        var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/SFPlayer.prefab");
        try
        {
            var so=new SerializedObject(root.GetComponent<OperatorAimVisual>());var a=so.FindProperty("rightWalkFrames");a.arraySize=count;
            for(int i=0;i<count;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=frames[i];
            so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/SFPlayer.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();return count+" right-facing walk frames installed; shared floor="+baseline+", ppu="+ppu;
    }
}
