using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TopDownShooter.Diagnostics;
public static class RestoreStudyShinConnector
{
    public static string Main()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        var studies=UnityEngine.Object.FindObjectsByType<WalkSkeletonStudy>(FindObjectsSortMode.None);
        if(studies.Length==0)throw new Exception("Open study scene");
        const string path="Assets/Art/OperatorPrototype/operator-study-shin-connector-v5.asset";
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(sprite==null)
        {
            var old=studies[0].artParts[1];var rect=old.rect;var origin=rect.position+old.pivot;
            float bottom=old.texture.height-450f*old.texture.height/1024f;
            rect.height=rect.yMax-bottom;rect.y=bottom;
            sprite=Sprite.Create(old.texture,rect,(origin-rect.position)/rect.size,old.pixelsPerUnit,0,SpriteMeshType.FullRect);
            sprite.name="ShinWithLowerConnector";AssetDatabase.CreateAsset(sprite,path);
        }
        foreach(var s in studies)
        {
            s.artParts[1]=s.artParts[4]=sprite;s.ankleFollow=1;s.ankleMaxAngle=35;s.showRedrawn=false;
            EditorUtility.SetDirty(s);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "Restored lower connector to source y450; unchanged knee pivot/PPU; trial follow=1/max=35";
    }
}
