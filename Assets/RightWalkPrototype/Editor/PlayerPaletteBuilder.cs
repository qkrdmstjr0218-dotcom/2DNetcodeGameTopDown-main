using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;
public static class PlayerPaletteBuilder
{
    [Serializable] class Row {public string source;public string[] variants;}
    [Serializable] class Rows {public Row[] rows;}
    [MenuItem("Tools/Right Walk Prototype/Build Player Palette Library")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Requires stopped editor");
        const string manifest="Assets/RightWalkPrototype/Editor/PlayerPaletteSources.json";
        var rows=JsonUtility.FromJson<Rows>("{\"rows\":"+File.ReadAllText(manifest)+"}").rows;
        foreach(var row in rows)
        {
            var original=(TextureImporter)AssetImporter.GetAtPath(row.source);
            foreach(string path in row.variants)
            {
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var target=(TextureImporter)AssetImporter.GetAtPath(path);
                // Match original sampling/import dimensions. No sprite geometry is replaced.
                target.textureType=TextureImporterType.Default;target.sRGBTexture=original.sRGBTexture;
                target.alphaSource=original.alphaSource;target.alphaIsTransparency=original.alphaIsTransparency;
                target.mipmapEnabled=original.mipmapEnabled;target.npotScale=original.npotScale;
                target.wrapMode=original.wrapMode;target.filterMode=original.filterMode;target.anisoLevel=original.anisoLevel;
                target.isReadable=false;target.textureCompression=original.textureCompression;
                target.maxTextureSize=original.maxTextureSize;target.compressionQuality=original.compressionQuality;
                target.SetPlatformTextureSettings(original.GetDefaultPlatformTextureSettings());
                foreach(var platform in new[]{"Standalone","Android","iPhone","WebGL"})
                {var settings=original.GetPlatformTextureSettings(platform);if(settings.overridden)target.SetPlatformTextureSettings(settings);else target.ClearPlatformTextureSettings(platform);}
                target.SaveAndReimport();
            }
        }
        string resource="Assets/RightWalkPrototype/Resources/PlayerPaletteLibrary.asset";
        var library=AssetDatabase.LoadAssetAtPath<ArenaPlayerPaletteLibrary>(resource);
        if(library==null){library=ScriptableObject.CreateInstance<ArenaPlayerPaletteLibrary>();AssetDatabase.CreateAsset(library,resource);}
        library.spriteShader=Shader.Find("RightWalk/PlayerIdentitySprite");
        if(library.spriteShader==null)throw new Exception("Missing identity shader");
        library.entries=new ArenaPlayerPaletteLibrary.Entry[rows.Length];
        for(int i=0;i<rows.Length;i++)
        {
            var row=rows[i];var e=new ArenaPlayerPaletteLibrary.Entry{source=AssetDatabase.LoadAssetAtPath<Texture2D>(row.source),p2=AssetDatabase.LoadAssetAtPath<Texture2D>(row.variants[0]),p3=AssetDatabase.LoadAssetAtPath<Texture2D>(row.variants[1]),p4=AssetDatabase.LoadAssetAtPath<Texture2D>(row.variants[2])};
            if(e.source==null||e.p2==null||e.p3==null||e.p4==null||e.source.width!=e.p2.width||e.source.height!=e.p2.height)throw new Exception("Palette size/source mismatch: "+row.source);
            library.entries[i]=e;
        }
        EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();Debug.Log("Player palette library: "+rows.Length+" source textures");
    }
}
