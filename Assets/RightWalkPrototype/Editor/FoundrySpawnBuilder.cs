using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;
public static class FoundrySpawnBuilder
{
    public const string RootName="Spawn sightline covers";
    public static readonly Vector2[] Starts={new(-31,.04f),new(31,.04f),new(-33,10.84f),new(33,10.84f)};
    public static void Configure(ArenaMapSettings map,SideScrollNetworkSession session)
    {
        FoundryEntryBuilder.Configure(map);
        var rules=map.GetComponent<RelayArenaPrototype>();rules.walkSpeed=2.8f;
        var starts=(Vector2[])Starts.Clone();for(int i=2;i<4;i++)starts[i].y=3*map.floorSpacing+.04f;
        map.respawnPoints=new[]{starts[0],starts[1],starts[2],starts[3],new Vector2(-FoundryUpperRouteBuilder.FixedSpawnX,3*map.floorSpacing+.04f),new Vector2(FoundryUpperRouteBuilder.FixedSpawnX,3*map.floorSpacing+.04f),new Vector2(-18,map.floorSpacing+.04f),new Vector2(18,map.floorSpacing+.04f)};
        for(int i=0;i<Mathf.Min(4,session.playerSpawns.Length);i++)
        {
            if(session.playerSpawns[i]!=null)session.playerSpawns[i].position=starts[i];
            var pad=map.transform.Find("Spawn pad "+(i+1));if(pad!=null)pad.position=starts[i]-Vector2.up*.02f;
        }
        // Superseded: spawn safety must not obstruct the existing middle route.
        var oldCovers=map.transform.Find(RootName);
        if(oldCovers!=null)UnityEngine.Object.DestroyImmediate(oldCovers.gameObject);
    }
    [MenuItem("Tools/Right Walk Prototype/Upgrade Foundry Player Spawns")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)throw new Exception("Requires saved Foundry edit mode");
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();var session=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();
        Configure(map,session);EditorUtility.SetDirty(map);EditorUtility.SetDirty(map.GetComponent<RelayArenaPrototype>());
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
}
