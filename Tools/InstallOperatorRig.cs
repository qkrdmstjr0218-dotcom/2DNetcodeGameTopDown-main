using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using TopDownShooter.Networking;
public static class InstallOperatorRig
{
    static string assetPath="Assets/Art/OperatorPrototype/operator-rig-sprites-v1.asset";
    static bool first;
    static Texture2D Import(string path)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var i=(TextureImporter)AssetImporter.GetAtPath(path); i.textureType=TextureImporterType.Default;
        i.isReadable=true; i.alphaIsTransparency=true; i.mipmapEnabled=false; i.textureCompression=TextureImporterCompression.Uncompressed;
        i.maxTextureSize=2048; i.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Rect Bounds(Texture2D t,RectInt area)
    {
        var p=t.GetPixels32(); int minX=area.xMax,minY=area.yMax,maxX=area.x,maxY=area.y;
        for(int y=area.y;y<area.yMax;y++) for(int x=area.x;x<area.xMax;x++)
            if(p[y*t.width+x].a>96) { minX=Math.Min(minX,x); minY=Math.Min(minY,y); maxX=Math.Max(maxX,x); maxY=Math.Max(maxY,y); }
        return new Rect(minX,minY,maxX-minX+1,maxY-minY+1);
    }
    static Sprite Make(Texture2D t,Rect b,string name,Vector2 center,float ppu)
    {
        var s=Sprite.Create(t,b,new Vector2((center.x-b.x)/b.width,(center.y-b.y)/b.height),ppu,0,SpriteMeshType.FullRect); s.name=name;
        if(first) { AssetDatabase.CreateAsset(s,assetPath); first=false; } else AssetDatabase.AddObjectToAsset(s,assetPath);
        return s;
    }
    static Vector2 Point(float x,float topY) => new Vector2(x,1024-topY);
    static void Array(SerializedObject so,string name,Sprite[] sprites)
    { var a=so.FindProperty(name); a.arraySize=sprites.Length; for(int j=0;j<sprites.Length;j++) a.GetArrayElementAtIndex(j).objectReferenceValue=sprites[j]; }
    static void Floats(SerializedObject so,string name,float[] values)
    { var a=so.FindProperty(name); a.arraySize=values.Length; for(int j=0;j<values.Length;j++) a.GetArrayElementAtIndex(j).floatValue=values[j]; }
    public static string Main()
    {
        if(Application.isPlaying) throw new Exception("Stop play before installing art.");
        var body=Import("Assets/Art/OperatorPrototype/operator-modular-body-v1.png");
        var parts=Import("Assets/Art/OperatorPrototype/operator-equipment-v1.png");
        if(body.width!=1536 || body.height!=1024 || parts.width!=1536 || parts.height!=1024) throw new Exception("Atlas layout needs rechecking.");
        AssetDatabase.DeleteAsset(assetPath); first=true;
        var torsos=new Sprite[3]; var thighs=new Sprite[3]; var shins=new Sprite[3]; var arms=new Sprite[4]; var weapons=new Sprite[3];
        var thighAngles=new float[3]; var shinAngles=new float[3];
        var hips=new[]{Point(223,604),Point(681,608),Point(1121,608)};
        var knees=new[]{Point(465,619),Point(938,626),Point(1388,606)};
        var shinKnees=new[]{Point(220,817),Point(692,821),Point(1272,802)};
        var feet=new[]{Point(366,969),Point(908,943),Point(1272,977)};
        for(int j=0;j<3;j++)
        {
            var b=Bounds(body,new RectInt(j*512,512,512,512));
            var pixels=body.GetPixels32(); double sum=0,weight=0;
            for(int y=(int)b.y;y<b.y+b.height*.25f;y++) for(int x=(int)b.x;x<b.xMax;x++)
            { int alpha=pixels[y*body.width+x].a; if(alpha>96) { sum+=x*alpha; weight+=alpha; } }
            torsos[j]=Make(body,b,"Torso "+j,new Vector2((float)(sum/weight),b.y),b.height/.62f);
            b=Bounds(body,new RectInt(j*512,280,512,232)); var d=knees[j]-hips[j];
            thighs[j]=Make(body,b,"Thigh "+j,(hips[j]+knees[j])*.5f,d.magnitude); thighAngles[j]=Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;
            b=Bounds(body,new RectInt(j*512,0,512,280)); d=feet[j]-shinKnees[j];
            shins[j]=Make(body,b,"Shin boot "+j,(shinKnees[j]+feet[j])*.5f,d.magnitude); shinAngles[j]=Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;
        }
        for(int j=0;j<4;j++)
        { var b=Bounds(parts,new RectInt(j*384,512,384,512)); arms[j]=Make(parts,b,"Arm part "+j,b.center,b.width); }
        var weaponAreas=new[]{new RectInt(0,0,384,512),new RectInt(384,0,384,512),new RectInt(768,0,480,512)};
        var boreY=new[]{1024-702f,1024-710f,1024-729f};
        for(int j=0;j<3;j++) { var b=Bounds(parts,weaponAreas[j]); weapons[j]=Make(parts,b,"Weapon "+j,new Vector2(b.center.x,boreY[j]),b.width); }
        var go=PrefabUtility.LoadPrefabContents("Assets/Prefabs/SFPlayer.prefab");
        try
        {
            var so=new SerializedObject(go.GetComponent<OperatorAimVisual>());
            Array(so,"torsoSprites",torsos); Array(so,"thighSprites",thighs); Array(so,"shinSprites",shins); Array(so,"armSprites",arms); Array(so,"weaponSprites",weapons);
            Floats(so,"thighAxisAngles",thighAngles); Floats(so,"shinAxisAngles",shinAngles);
            so.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(go,"Assets/Prefabs/SFPlayer.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(go); }
        AssetDatabase.SaveAssets(); return "Installed 16 modular sprites, official four-limb IK rig.";
    }
}
