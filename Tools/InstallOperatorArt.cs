using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using TopDownShooter.Networking;
public static class InstallOperatorArt
{
    public static string Main()
    {
        if(Application.isPlaying) throw new Exception("Stop play first.");
        const string path="Assets/Art/OperatorPrototype/operator-poses-v2.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Default; importer.isReadable=true;
        importer.alphaIsTransparency=true; importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.filterMode=FilterMode.Bilinear; importer.maxTextureSize=2048;
        importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path); var pixels=texture.GetPixels32();
        int cw=texture.width/3,ch=texture.height/4;
        var bounds=new RectInt[12]; var anchors=new float[3];
        for(int row=0;row<4;row++) for(int col=0;col<3;col++)
        {
            int left=col*cw,bottom=(3-row)*ch,minX=left+cw,minY=bottom+ch,maxX=left,maxY=bottom;
            for(int y=bottom;y<bottom+ch;y++) for(int x=left;x<left+cw;x++)
                if(pixels[y*texture.width+x].a>32) { minX=Math.Min(minX,x); maxX=Math.Max(maxX,x); minY=Math.Min(minY,y); maxY=Math.Max(maxY,y); }
            bounds[row*3+col]=new RectInt(minX,minY,maxX-minX+1,maxY-minY+1);
            if(row==0)
            {
                double sum=0,weight=0;
                for(int y=minY;y<minY+(maxY-minY)/2;y++) for(int x=minX;x<=maxX;x++)
                { var a=pixels[y*texture.width+x].a; if(a>32) { sum+=x*a; weight+=a; } }
                anchors[col]=(float)(sum/weight);
            }
        }
        float ppu=(float)bounds.Average(b=>b.height)/.95f;
        const string spritesPath="Assets/Art/OperatorPrototype/operator-sprites-v2.asset";
        AssetDatabase.DeleteAsset(spritesPath);
        var frames=new Sprite[12];
        for(int i=0;i<12;i++)
        {
            var b=bounds[i]; var pivot=new Vector2((anchors[i%3]-b.x)/b.width,0);
            frames[i]=Sprite.Create(texture,new Rect(b.x,b.y,b.width,b.height),pivot,ppu,0,SpriteMeshType.FullRect);
            frames[i].name=new[]{"Front","Right","Back"}[i%3]+"_"+i/3;
            if(i==0) AssetDatabase.CreateAsset(frames[i],spritesPath); else AssetDatabase.AddObjectToAsset(frames[i],spritesPath);
        }
        var prefab=PrefabUtility.LoadPrefabContents("Assets/Prefabs/SFPlayer.prefab");
        try
        {
            var so=new SerializedObject(prefab.GetComponent<OperatorAimVisual>()); var array=so.FindProperty("poseFrames"); array.arraySize=12;
            for(int i=0;i<12;i++) array.GetArrayElementAtIndex(i).objectReferenceValue=frames[i];
            so.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Prefabs/SFPlayer.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        AssetDatabase.SaveAssets();
        return "12 alpha-trimmed sprites installed, ppu="+ppu+", bounds="+string.Join(";",bounds.Select(b=>b.ToString()));
    }
}
