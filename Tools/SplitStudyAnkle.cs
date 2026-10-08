using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TopDownShooter.Diagnostics;
public static class SplitStudyAnkle
{
    // Split the existing sprite's render mesh; texture pixels remain untouched.
    private static Sprite Piece(Sprite source,string name,Vector2[] points,ushort[] triangles)
    {
        var rect=source.rect;var s=Sprite.Create(source.texture,rect,source.pivot/rect.size,source.pixelsPerUnit,0,SpriteMeshType.FullRect);
        var vertices=new Vector2[points.Length];float sx=source.texture.width/1536f,sy=source.texture.height/1024f;
        for(int i=0;i<points.Length;i++)
        {
            float x=Mathf.Clamp(points[i].x*sx,rect.xMin,rect.xMax);
            float y=Mathf.Clamp(source.texture.height-points[i].y*sy,rect.yMin,rect.yMax);
            vertices[i]=(new Vector2(x-rect.x,y-rect.y)-source.pivot)/source.pixelsPerUnit;
        }
        s.OverrideGeometry(vertices,triangles);s.name=name;return s;
    }
    public static string Main()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        const string path="Assets/Art/OperatorPrototype/operator-study-ankle-split-v4.asset";
        if(AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new Exception("Already installed");
        var studies=UnityEngine.Object.FindObjectsByType<WalkSkeletonStudy>(FindObjectsSortMode.None);
        var source=studies[0].artParts[2];
        var boot=Piece(source,"BootWithoutAnkleCover",new[]{new Vector2(964,395),new Vector2(1090,395),new Vector2(1110,355),new Vector2(1170,320),new Vector2(1355,320),new Vector2(1355,480),new Vector2(964,480)},new ushort[]{5,6,0,5,0,1,5,1,2,5,2,3,5,3,4});
        var ankle=Piece(source,"IndependentAnkleCover",new[]{new Vector2(964,205),new Vector2(1170,205),new Vector2(1170,320),new Vector2(1110,355),new Vector2(1090,395),new Vector2(964,395)},new ushort[]{0,1,2,0,2,3,0,3,4,0,4,5});
        AssetDatabase.CreateAsset(boot,path);AssetDatabase.AddObjectToAsset(ankle,path);
        foreach(var s in studies){s.artParts[2]=s.artParts[5]=boot;s.ankleCoverSprite=ankle;EditorUtility.SetDirty(s);}
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "Split boot render geometry into complementary foot/ankle meshes; shared ankle pivot and fixed scale";
    }
}
