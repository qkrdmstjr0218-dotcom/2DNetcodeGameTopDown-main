using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using RightWalkPrototype;

[InitializeOnLoad]
public static class NetworkCombatEditorReview
{
    [Serializable] class Request {public string folder;}
    const string RequestPath="Assets/RightWalkPrototype/Tests/CombatReviewRequest.json";
    static bool started;
    static string folder;
    static double deadline;
    static NetworkCombatEditorReview(){EditorApplication.update+=Tick;}
    public static void Run(string name)
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Requires saved edit mode");
        string target=Path.GetFullPath("LocalTools/Analysis/NetworkCombat/"+name);
        if(Directory.Exists(target))throw new Exception("Use a fresh review name");
        Directory.CreateDirectory(target);File.WriteAllText(RequestPath,JsonUtility.ToJson(new Request{folder=target}));
        AssetDatabase.ImportAsset(RequestPath);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying){started=false;return;}
        if(!started)
        {
            if(EditorApplication.isCompiling||!File.Exists(RequestPath)||Time.time<.2f)return;
            var session=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();if(session==null)return;
            folder=JsonUtility.FromJson<Request>(File.ReadAllText(RequestPath)).folder;
            started=true;deadline=EditorApplication.timeSinceStartup+100;
            session.StartCoroutine(SideScrollNetworkCombatVerification.Run(session,Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor,folder));
        }
        if(!Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor)return;
        bool host=File.Exists(folder+"/host-result.txt"),client=File.Exists(folder+"/client-result.txt");
        if(host&&client||EditorApplication.timeSinceStartup>deadline)EditorApplication.isPlaying=false;
    }
}
