using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using RightWalkPrototype;

// Compiled before play. No MCP/CodeDom compilation can stall a live transport.
[InitializeOnLoad]
public static class RelayArenaEditorReview
{
    [Serializable] private class Request {public string folder;public int players=4;public bool downhillOnly;}
    [Serializable] private class Actor {public int number,hp;public bool owner,dead,controls;public Vector3 position;public float range;}
    [Serializable] private class Peer {public bool connected;public Actor[] players;}
    private const string RequestPath="Assets/RightWalkPrototype/Tests/ArenaReviewRequest.json";
    private static bool started;
    private static double deadline;
    static RelayArenaEditorReview(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/Right Walk Prototype/Verify Arena Controls")]
    public static void Begin(){BeginReview(false);}
    [MenuItem("Tools/Right Walk Prototype/Verify Arena Downhill")]
    public static void BeginDownhill(){BeginReview(true);}
    static void BeginReview(bool downhillOnly)
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Requires saved edit mode");
        var request=new Request{folder=Path.GetFullPath("LocalTools/Analysis/NetworkCombat/arena-controls-"+DateTime.UtcNow.ToString("HHmmss")),downhillOnly=downhillOnly};
        Directory.CreateDirectory(request.folder);File.WriteAllText(RequestPath,JsonUtility.ToJson(request));
        AssetDatabase.ImportAsset(RequestPath);EditorApplication.isPlaying=true;
    }
    private static void Tick()
    {
        if(!EditorApplication.isPlaying){started=false;return;}
        if(started||EditorApplication.isCompiling||Time.time<.3f||!File.Exists(RequestPath))return;
        var session=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();if(session==null)return;
        var request=JsonUtility.FromJson<Request>(File.ReadAllText(RequestPath));
        started=true;deadline=EditorApplication.timeSinceStartup+180;
        bool main=Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor;
        session.useReadyRoom=false;session.StartLocal(main);session.StartCoroutine(Snapshots(session,request));
        if(main)session.StartCoroutine(Check(session,request));
    }
    private static IEnumerator Snapshots(SideScrollNetworkSession session,Request request)
    {
        string path=request.folder+"/peer-"+System.Diagnostics.Process.GetCurrentProcess().Id+".json";
        while(EditorApplication.isPlaying)
        {
            var manager=NetworkManager.Singleton;
            var players=UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
            var peer=new Peer{connected=manager!=null&&manager.IsConnectedClient,players=new Actor[players.Length]};
            for(int i=0;i<players.Length;i++)
            {
                var p=players[i];peer.players[i]=new Actor{number=p.PlayerNumber,hp=p.Health,owner=p.IsOwner,dead=p.GetComponent<CharacterDeathTrial>().IsDead,
                    controls=p.GetComponent<SideScrollMapPreview>().enabled,position=p.transform.position,range=p.GetComponentInChildren<RightFacingAimRig>().previewAimRange};
            }
            if(!File.Exists(request.folder+"/stop-clients.txt"))File.WriteAllText(path,JsonUtility.ToJson(peer));
            else if(!Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor){session.DisconnectLocal();yield break;}
            if(Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor&&EditorApplication.timeSinceStartup>deadline)
            {File.WriteAllText(request.folder+"/network-result.txt","FAIL: editor review deadline");EditorApplication.isPlaying=false;yield break;}
            yield return new WaitForSecondsRealtime(.25f);
        }
    }
    private static IEnumerator Check(SideScrollNetworkSession session,Request request)
    {
        float until=Time.realtimeSinceStartup+30;
        while(UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None).Length<request.players&&Time.realtimeSinceStartup<until)yield return null;
        if(UnityEngine.Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None).Length!=request.players)
        {File.WriteAllText(request.folder+"/network-result.txt","FAIL: initial participant count");EditorApplication.isPlaying=false;yield break;}
        yield return RelayArenaVerification.Run(request.folder,request.downhillOnly);
        yield return new WaitForSecondsRealtime(1);
        bool passed=File.ReadAllText(request.folder+"/result.txt").StartsWith("PASS");
        string[] peers=Directory.GetFiles(request.folder,"peer-*.json");
        passed&=peers.Length==request.players;
        foreach(string path in peers)
        {
            Peer peer=null;
            try
            {
                using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
                using(var reader=new StreamReader(stream))peer=JsonUtility.FromJson<Peer>(reader.ReadToEnd());
            }
            catch{}
            if(peer==null||!peer.connected||peer.players.Length!=request.players){passed=false;continue;}
            int owners=0;var ids=new System.Collections.Generic.HashSet<int>();
            foreach(var actor in peer.players)
            {
                if(actor.owner)owners++;
                if(actor.number<1||actor.number>request.players||actor.hp!=100||actor.dead||actor.controls!=actor.owner||Mathf.Abs(actor.range-24)>.01f||!ids.Add(actor.number))passed=false;
            }
            if(owners!=1)passed=false;
        }
        File.WriteAllText(request.folder+"/network-result.txt",passed?"PASS: four peers remain connected; HP/native death restored; own controls only; unique P numbers; range24 on all peers":"FAIL: peer snapshot/controls/range check");
        File.WriteAllText(request.folder+"/stop-clients.txt","stop");
        yield return new WaitForSecondsRealtime(1);NetworkManager.Singleton.Shutdown();
        yield return new WaitForSecondsRealtime(.5f);EditorApplication.isPlaying=false;
    }
}
