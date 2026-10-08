using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TopDownShooter.Diagnostics;
using TopDownShooter.Networking;
public static class ConnectStudyUpperBody
{
    public static string Main()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SFPlayer.prefab");
        var so=new SerializedObject(root.GetComponent<OperatorAimVisual>());
        var source=(Sprite)so.FindProperty("integratedAimPoses").GetArrayElementAtIndex(0).objectReferenceValue;
        const string path="Assets/Art/OperatorPrototype/operator-study-upper-pelvis-v1.asset";
        if(AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new Exception("Already installed");
        var r=source.rect;float cut=Mathf.Floor(r.height*.52f),bottom=Mathf.Floor(r.height*.38f);
        var upper=Sprite.Create(source.texture,new Rect(r.x,r.y+cut,r.width,r.height-cut),new Vector2(source.pivot.x/r.width,0),source.pixelsPerUnit,0,SpriteMeshType.FullRect);upper.name="AimingUpperBody";
        var pelvis=Sprite.Create(source.texture,new Rect(r.x,r.y+bottom,r.width,cut-bottom),new Vector2(source.pivot.x/r.width,.5f),source.pixelsPerUnit,0,SpriteMeshType.FullRect);pelvis.name="PelvisBridge";
        AssetDatabase.CreateAsset(upper,path);AssetDatabase.AddObjectToAsset(pelvis,path);
        foreach(var s in UnityEngine.Object.FindObjectsByType<WalkSkeletonStudy>(FindObjectsSortMode.None))
        {s.studyUpperBody=upper;s.studyPelvis=pelvis;s.showFullBody=true;s.showRedrawn=false;EditorUtility.SetDirty(s);}
        var cam=GameObject.Find("Study camera").GetComponent<Camera>();cam.orthographicSize=1.8f;
        var center=new Vector3(1.25f,.45f,0);cam.transform.position=center+new Vector3(2.2f,3,-5);cam.transform.LookAt(center);EditorUtility.SetDirty(cam);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "Connected original E aiming upper body and pelvis to study; gameplay prefab untouched";
    }
}
