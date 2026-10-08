using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Cinemachine;
using TopDownShooter.Core;
using TopDownShooter.Networking;

public static class BuildPlayground
{
    private static Sprite pixel;
    private static Material material;
    private static PhysicsMaterial2D frictionless;
    private static void Field(UnityEngine.Object target, string field, Action<SerializedProperty> set)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null) throw new Exception("Missing property: "+field);
        set(property); serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\','/');
        Folder(parent); AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path));
    }
    private static WeaponDefinitionSO Weapon(string name, float interval, int damage, int pellets, int targets, float speed, float range, float spread, float sprintSpread, float length, Color color)
    {
        string path = "Assets/DATA/Weapons/"+name+".asset";
        var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(path);
        if (weapon == null) { weapon = ScriptableObject.CreateInstance<WeaponDefinitionSO>(); AssetDatabase.CreateAsset(weapon,path); }
        weapon.displayName=name; weapon.interval=interval; weapon.damage=damage; weapon.pellets=pellets;
        weapon.targets=targets; weapon.speed=speed; weapon.range=range; weapon.spread=spread;
        weapon.sprintSpread=sprintSpread; weapon.visualLength=length; weapon.color=color;
        EditorUtility.SetDirty(weapon); return weapon;
    }
    private static GameObject Rect(string name, Vector2 point, Vector2 size, Color color, int order, bool wall=false)
    {
        var go = new GameObject(name); go.transform.position=point; go.transform.localScale=size;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite=pixel; sr.sharedMaterial=material; sr.color=color; sr.sortingOrder=order;
        if (wall) { go.layer=8; go.AddComponent<BoxCollider2D>().sharedMaterial=frictionless; go.AddComponent<ProjectileWall>(); }
        return go;
    }
    public static string Main()
    {
        if (Application.isPlaying) throw new Exception("Stop Play before building.");
        Folder("Assets/DATA/Weapons"); Folder("Assets/Art/Prototype");
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Prototype/WhitePixel.asset");
        if (texture == null)
        {
            texture = new Texture2D(1,1); texture.SetPixel(0,0,Color.white); texture.Apply();
            AssetDatabase.CreateAsset(texture,"Assets/Art/Prototype/WhitePixel.asset");
            var sprite = Sprite.Create(texture,new Rect(0,0,1,1),new Vector2(0.5f,0.5f),1);
            sprite.name="WhitePixel"; AssetDatabase.AddObjectToAsset(sprite,texture);
        }
        pixel = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Prototype/WhitePixel.asset").OfType<Sprite>().First();
        material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Prototype/Unlit.mat");
        if (material == null) { material=new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(material,"Assets/Art/Prototype/Unlit.mat"); }
        frictionless = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Art/Prototype/Slide.physicsMaterial2D");
        if (frictionless == null) { frictionless=new PhysicsMaterial2D { friction=0, bounciness=0 }; AssetDatabase.CreateAsset(frictionless,"Assets/Art/Prototype/Slide.physicsMaterial2D"); }
        var weapons = new[] {
            Weapon("Automatic",0.12f,1,1,1,24,16,1.5f,6,0.7f,new Color(0.4f,0.85f,1)),
            Weapon("Shotgun",0.7f,1,6,1,22,9,14,7,0.8f,new Color(1,0.65f,0.22f)),
            Weapon("Piercing Rifle",0.5f,3,1,3,36,22,0.4f,4,1,new Color(0.8f,0.6f,1)) };
        var player = PrefabUtility.LoadPrefabContents("Assets/Prefabs/PlayerNet.prefab");
        GameObject sfPlayer;
        try
        {
            player.name="SFPlayer";
            var controller=player.GetComponent<NetworkPlayerController>();
            Field(controller,"weapons",p=> { p.arraySize=3; for(int i=0;i<3;i++) p.GetArrayElementAtIndex(i).objectReferenceValue=weapons[i]; });
            Field(controller,"moveSpeed",p=>p.floatValue=5);
            Field(controller,"sprintMultiplier",p=>p.floatValue=1.6f);
            if (player.GetComponent<OperatorAimVisual>()==null) player.AddComponent<OperatorAimVisual>();
            player.GetComponent<CircleCollider2D>().radius=0.28f;
            player.GetComponent<CircleCollider2D>().sharedMaterial=frictionless;
            player.GetComponent<Rigidbody2D>().collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            Field(player.GetComponent<NetworkTransform>(),"SyncScaleX",p=>p.boolValue=false);
            Field(player.GetComponent<NetworkTransform>(),"SyncScaleY",p=>p.boolValue=false);
            Field(player.GetComponent<NetworkTransform>(),"SyncScaleZ",p=>p.boolValue=false);
            foreach(var camera in player.GetComponentsInChildren<CinemachineCamera>(true)) camera.Lens.OrthographicSize=7;
            sfPlayer=PrefabUtility.SaveAsPrefabAsset(player,"Assets/Prefabs/SFPlayer.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        foreach(var go in scene.GetRootGameObjects()) if(go.name=="Triangle") UnityEngine.Object.DestroyImmediate(go);
        foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None)) canvas.gameObject.SetActive(false);
        var game=UnityEngine.Object.FindFirstObjectByType<NetworkGameManager>();
        Field(game,"practiceMode",p=>p.boolValue=true);
        Field(game,"minimumPlayers",p=>p.intValue=1);
        var manager=UnityEngine.Object.FindFirstObjectByType<NetworkManager>();
        manager.NetworkConfig.PlayerPrefab=sfPlayer;
        var list=AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>("Assets/DATA/Config/SFNetworkPrefabs.asset");
        if(list!=null) AssetDatabase.DeleteAsset("Assets/DATA/Config/SFNetworkPrefabs.asset");
        list=ScriptableObject.CreateInstance<NetworkPrefabsList>();
        foreach(string path in new[]{"Assets/Prefabs/SFPlayer.prefab","Assets/Prefabs/EnemyNet.prefab","Assets/Prefabs/ProjectileNet.prefab","Assets/Prefabs/Particle System.prefab"})
            list.Add(new NetworkPrefab { Prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path) });
        AssetDatabase.CreateAsset(list,"Assets/DATA/Config/SFNetworkPrefabs.asset");
        manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear(); manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(list);
        new GameObject("Playground Session UI").AddComponent<PlaygroundSessionUI>();
        Rect("Training floor",Vector2.zero,new Vector2(28,20),new Color(0.08f,0.12f,0.17f),-100);
        for(int i=-14;i<=14;i++) Rect("Grid X "+i,new Vector2(i,0),new Vector2(0.018f,20),new Color(0.14f,0.2f,0.27f),-99);
        for(int i=-10;i<=10;i++) Rect("Grid Y "+i,new Vector2(0,i),new Vector2(28,0.018f),new Color(0.14f,0.2f,0.27f),-99);
        Color wall=new Color(0.28f,0.37f,0.46f);
        Rect("Boundary North",new Vector2(0,10),new Vector2(28,0.6f),wall,3,true);
        Rect("Boundary South",new Vector2(0,-10),new Vector2(28,0.6f),wall,3,true);
        Rect("Boundary East",new Vector2(14,0),new Vector2(0.6f,20),wall,3,true);
        Rect("Boundary West",new Vector2(-14,0),new Vector2(0.6f,20),wall,3,true);
        Rect("Slide test horizontal",new Vector2(1,3),new Vector2(6,0.55f),wall,3,true);
        Rect("Slide test vertical",new Vector2(-4,1),new Vector2(0.55f,4),wall,3,true);
        Rect("Range stop",new Vector2(10,-2),new Vector2(0.55f,3),wall,3,true);
        for(int i=0;i<3;i++)
        {
            var target=Rect("Practice target "+(i+1),new Vector2(3+i*2,-2),Vector2.one*0.7f,new Color(1,0.32f,0.25f),4);
            target.AddComponent<BoxCollider2D>(); target.AddComponent<NetworkObject>(); target.AddComponent<PracticeTarget>();
        }
        foreach(var spawn in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if(spawn.name=="Spawn1") spawn.position=new Vector3(-10,-6,0);
            if(spawn.name=="Spawn2") spawn.position=new Vector3(10,6,0);
            if(spawn.name=="Spawn1" || spawn.name=="Spawn2") foreach(var marker in spawn.GetComponentsInChildren<SpriteRenderer>(true)) marker.enabled=false;
        }
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        tags.FindProperty("layers").GetArrayElementAtIndex(8).stringValue="HighWall"; tags.ApplyModifiedPropertiesWithoutUndo();
        Camera.main.backgroundColor=new Color(0.04f,0.06f,0.1f); Camera.main.orthographicSize=7;
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/SFPlayground.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/SFPlayground.unity",true),new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity",false)};
        PlayerSettings.runInBackground=true;
        AssetDatabase.SaveAssets();
        return "Created SFPlayground, SFPlayer, three weapon assets, walls and three network practice targets.";
    }
}
