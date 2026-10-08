using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Netcode;
using TopDownShooter.Core;
using TopDownShooter.Networking;
public static class ImprovePlayground
{
    private static void Reference(UnityEngine.Object target,string name,UnityEngine.Object value)
    { var s=new SerializedObject(target); s.FindProperty(name).objectReferenceValue=value; s.ApplyModifiedPropertiesWithoutUndo(); }
    public static string Main()
    {
        if(Application.isPlaying) throw new Exception("Stop Play before editing assets.");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/SFPlayground.unity");
        var pixel=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Prototype/WhitePixel.asset").OfType<Sprite>().First();
        var enemy=PrefabUtility.LoadPrefabContents("Assets/Prefabs/EnemyNet.prefab");
        GameObject prefab;
        try
        {
            enemy.name="SFEnemy";
            enemy.transform.localScale=Vector3.one*0.65f;
            var sr=enemy.GetComponent<SpriteRenderer>(); sr.sprite=pixel; sr.color=new Color(0.8f,0.27f,0.23f); sr.sortingOrder=12;
            sr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Prototype/Unlit.mat");
            enemy.GetComponent<CircleCollider2D>().radius=0.45f;
            enemy.GetComponent<CircleCollider2D>().sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Art/Prototype/Slide.physicsMaterial2D");
            prefab=PrefabUtility.SaveAsPrefabAsset(enemy,"Assets/Prefabs/SFEnemy.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(enemy); }
        string configPath="Assets/DATA/Config/SFEnemyConfig.asset";
        var config=AssetDatabase.LoadAssetAtPath<EnemyConfigSO>(configPath);
        if(config==null) { config=ScriptableObject.CreateInstance<EnemyConfigSO>(); AssetDatabase.CreateAsset(config,configPath); }
        Reference(config,"enemyPrefab",prefab.GetComponent<NetworkEnemy>());
        string gamePath="Assets/DATA/Config/SFGameConfig.asset";
        var gameConfig=AssetDatabase.LoadAssetAtPath<GameConfigSO>(gamePath);
        if(gameConfig==null)
        { gameConfig=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameConfigSO>("Assets/DATA/Config/New Game Config SO.asset")); AssetDatabase.CreateAsset(gameConfig,gamePath); }
        Reference(gameConfig,"enemyConfig",config);
        Reference(UnityEngine.Object.FindFirstObjectByType<NetworkGameManager>(),"gameConfig",gameConfig);
        Reference(UnityEngine.Object.FindFirstObjectByType<EnemySpawner>(),"enemyConfig",config);
        var list=AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>("Assets/DATA/Config/SFNetworkPrefabs.asset");
        if(!list.PrefabList.Any(p=>p.Prefab==prefab)) list.Add(new NetworkPrefab { Prefab=prefab });
        EditorUtility.SetDirty(list);
        foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if(t.name!="Spawn1" && t.name!="Spawn2") continue;
            if(t.GetComponent<SpawnPointMarker>()==null) t.gameObject.AddComponent<SpawnPointMarker>();
            foreach(var marker in t.GetComponentsInChildren<SpriteRenderer>(true))
            { marker.enabled=true; marker.color=new Color(1,0.75f,0.15f); }
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        return "Enemies: 0.65-unit sprite / 0.2925-unit collider radius; spawn labels and arrows enabled.";
    }
}
