using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TopDownShooter.Diagnostics;
public static class CreateWalkSkeletonStudy
{
    public static string Main()
    {
        if(Application.isPlaying)throw new System.Exception("Stop Play first.");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var root=new GameObject("Walk skeleton study");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
        for(int i=0;i<8;i++)
        {
            var go=new GameObject("Phase "+i);go.transform.SetParent(root.transform);go.transform.localPosition=new Vector3((i%4)*.85f,0,(i/4)*1.05f);
            var s=go.AddComponent<WalkSkeletonStudy>();s.animate=false;s.cycle=i/8f;
        }
        var live=new GameObject("Animated comparison");live.transform.SetParent(root.transform);live.transform.localPosition=new Vector3(1.3f,0,-1.2f);live.AddComponent<WalkSkeletonStudy>();
        var cam=new GameObject("Study camera").AddComponent<Camera>();cam.transform.SetParent(root.transform);
        var center=new Vector3(1.25f,.22f,0);cam.transform.position=center+new Vector3(2.2f,3,-5);
        cam.transform.LookAt(center);cam.orthographic=true;cam.orthographicSize=1.55f;
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.07f,.09f,.13f);cam.tag="MainCamera";
        string path="Assets/Scenes/OperatorWalkSkeletonStudy.unity";
        EditorSceneManager.SaveScene(scene,path);EditorSceneManager.CloseScene(scene,true);
        return "Saved "+path+"; 8 fixed phases and one animated sample. Existing gameplay scene preserved.";
    }
}
