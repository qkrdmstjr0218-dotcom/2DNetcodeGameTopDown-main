using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using RightWalkPrototype;

public static class SideScrollNetworkTrialBuilder
{
    public const string ScenePath="Assets/RightWalkPrototype/Scenes/Network/SideScrollNetworkTrial.unity";
    public const string PlayerPath="Assets/RightWalkPrototype/SideScrollNetworkPlayer.prefab";
    private const string CurrentScene="Assets/Characters/Current/Scenes/Character_Complete_Playground.unity";
    [MenuItem("Tools/Right Walk Prototype/Build Network Trial")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop play mode first.");
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var source=EditorSceneManager.OpenScene(CurrentScene);
        EditorSceneManager.SaveScene(source,ScenePath,true);
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var walker=Object.FindFirstObjectByType<RightFacingWalker>();
        var character=walker.gameObject;
        PrefabUtility.UnpackPrefabInstance(character,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        character.GetComponent<SideScrollMapPreview>().followCamera=null;
        character.GetComponentInChildren<RightFacingAimRig>().aimCamera=null;
        if(character.GetComponent<NetworkObject>()==null)character.AddComponent<NetworkObject>();
        character.AddComponent<SideScrollNetworkPlayer>();
        character.name="NetworkCharacter_Complete";
        var player=PrefabUtility.SaveAsPrefabAsset(character,PlayerPath);
        // Persist NGO's prefab identity after validation so virtual editors read the same hash.
        AssetDatabase.ImportAsset(PlayerPath,ImportAssetOptions.ForceUpdate);
        player=AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
        var identity=player.GetComponent<NetworkObject>();
        EditorUtility.SetDirty(identity);AssetDatabase.SaveAssetIfDirty(identity);
        Object.DestroyImmediate(character);
        // Local HP/tuning buttons belong to the preserved local trial, not network play.
        foreach(var hud in Object.FindObjectsByType<FrontDownAimPlaytestHud>(FindObjectsSortMode.None))Object.DestroyImmediate(hud);
        var go=new GameObject("Side Scroll Network Session");
        var transport=go.AddComponent<UnityTransport>();
        var manager=go.AddComponent<NetworkManager>();
        manager.NetworkConfig=new NetworkConfig {NetworkTransport=transport,PlayerPrefab=player,EnableSceneManagement=false};
        var session=go.AddComponent<SideScrollNetworkSession>();
        session.targetPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RightWalkPrototype/NetworkDamageTarget.prefab");
        if(session.targetPrefab==null)AddTargets();
        else manager.NetworkConfig.Prefabs.Add(new NetworkPrefab {Prefab=session.targetPrefab});
        EditorSceneManager.SaveScene(scene,ScenePath);
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Tools/Right Walk Prototype/Add Network Damage Targets")]
    public static void AddTargets()
    {
        const string path="Assets/RightWalkPrototype/Art/NetworkTarget.png";
        var texture=new Texture2D(96,128,TextureFormat.RGBA32,false);
        var pixels=new Color32[96*128];
        for(int y=0;y<128;y++)for(int x=0;x<96;x++)
        {
            Color32 color=new Color32(0,0,0,0);
            float radius=Vector2.Distance(new Vector2(x,y),new Vector2(48,78));
            if(y<42&&x>=43&&x<=53)color=new Color32(115,130,137,255);
            if(y<9&&x>=19&&x<=77)color=new Color32(49,62,69,255);
            if(radius<=43)color=new Color32(37,47,51,255);
            if(radius<=39)color=new Color32(220,233,229,255);
            if(radius<=30)color=new Color32(193,63,70,255);
            if(radius<=21)color=new Color32(220,233,229,255);
            if(radius<=12)color=new Color32(193,63,70,255);
            if(radius<=4)color=new Color32(37,47,51,255);
            pixels[y*96+x]=color;
        }
        texture.SetPixels32(pixels);texture.Apply();System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;
        importer.spritePixelsPerUnit=128;importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var target=new GameObject("Network practice target");
        target.AddComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        target.GetComponent<SpriteRenderer>().sortingOrder=5;
        var collider=target.AddComponent<BoxCollider2D>();collider.size=new Vector2(.65f,.66f);collider.offset=new Vector2(0,.11f);
        target.AddComponent<NetworkObject>();target.AddComponent<SideScrollDamageTarget>();
        var prefab=PrefabUtility.SaveAsPrefabAsset(target,"Assets/RightWalkPrototype/NetworkDamageTarget.prefab");Object.DestroyImmediate(target);
        var session=Object.FindFirstObjectByType<SideScrollNetworkSession>();session.targetPrefab=prefab;
        var manager=session.GetComponent<NetworkManager>();
        if(!manager.NetworkConfig.Prefabs.Contains(prefab))manager.NetworkConfig.Prefabs.Add(new NetworkPrefab {Prefab=prefab});
        EditorUtility.SetDirty(session);EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene);EditorSceneManager.SaveScene(session.gameObject.scene);
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Tools/Right Walk Prototype/Build Network Test Player")]
    public static void BuildPlayer()
    {
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{ScenePath},locationPathName="Builds/SideScrollNetworkCurrent/SideScrollNetworkCurrent.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.InvalidOperationException("Network test build failed: "+report.summary.result);
    }
}
