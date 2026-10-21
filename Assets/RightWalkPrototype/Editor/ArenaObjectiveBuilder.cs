using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Netcode;
using RightWalkPrototype;

public static class ArenaObjectiveBuilder
{
    const string PrefabPath="Assets/RightWalkPrototype/ArenaObjectiveMatch.prefab";
    [MenuItem("Tools/Right Walk Prototype/Add Hacking Objective Trial")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.path!=RelayArenaPrototypeBuilder.ScenePath)throw new InvalidOperationException("Requires arena edit mode");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if(prefab==null)
        {
            var source=new GameObject("Arena Objective Match");source.AddComponent<NetworkObject>();source.AddComponent<ArenaObjectiveMatch>();
            PrefabUtility.SaveAsPrefabAsset(source,PrefabPath);UnityEngine.Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(PrefabPath,ImportAssetOptions.ForceUpdate);prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            EditorUtility.SetDirty(prefab.GetComponent<NetworkObject>());AssetDatabase.SaveAssets();
        }
        var session=UnityEngine.Object.FindFirstObjectByType<SideScrollNetworkSession>();session.arenaObjectivePrefab=prefab;
        var layout=UnityEngine.Object.FindFirstObjectByType<ArenaObjectiveLayout>();
        if(layout==null)
        {
            var root=new GameObject("Hacking objective candidates");layout=root.AddComponent<ArenaObjectiveLayout>();
            root.transform.SetParent(UnityEngine.Object.FindFirstObjectByType<RelayArenaPrototype>().transform);
            // Fixed routes first: centre -> west -> upper -> east.
            var positions=new[]{new Vector2(0,0),new Vector2(-10.5f,3.2f),new Vector2(2.5f,6.4f),new Vector2(10.5f,3.2f)};
            var names=new[]{"하단 중앙","서쪽 중단","중앙 상단","동쪽 중단"};layout.points=new ArenaObjectivePoint[4];
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
            for(int i=0;i<4;i++)
            {
                var go=new GameObject("Uplink "+(char)('A'+i));go.transform.SetParent(root.transform);go.transform.position=positions[i];
                var point=go.AddComponent<ArenaObjectivePoint>();point.displayName=names[i];point.halfWidth=i==0?1.4f:1f;layout.points[i]=point;
                float w=point.halfWidth*2,h=2.35f;
                point.area=new[]{Part(go.transform,"Zone fill",new Vector2(0,h*.5f),new Vector2(w,h),sprite),Part(go.transform,"Zone floor",new Vector2(0,.04f),new Vector2(w,.045f),sprite),Part(go.transform,"Zone left",new Vector2(-w*.5f,h*.5f),new Vector2(.025f,h),sprite),Part(go.transform,"Zone right",new Vector2(w*.5f,h*.5f),new Vector2(.025f,h),sprite),Part(go.transform,"Zone top",new Vector2(0,h),new Vector2(w,.025f),sprite)};
                var label=new GameObject("Uplink label");label.transform.SetParent(go.transform,false);label.transform.localPosition=new Vector3(0,2.55f,0);
                point.label=label.AddComponent<TextMesh>();point.label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");point.label.fontSize=48;point.label.characterSize=.06f;point.label.anchor=TextAnchor.MiddleCenter;
                var renderer=label.GetComponent<MeshRenderer>();renderer.sharedMaterial=point.label.font.material;renderer.sortingOrder=-10;
                point.Show(false,false,Color.cyan);
            }
        }
        foreach(var point in layout.points)
        {
            point.label.transform.localPosition=new Vector3(-point.halfWidth-.15f,2.1f,0);
            point.label.anchor=TextAnchor.MiddleRight;
        }
        EditorUtility.SetDirty(session);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    static SpriteRenderer Part(Transform root,string name,Vector2 position,Vector2 size,Sprite sprite)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=new Vector3(size.x,size.y,1);
        var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=-12;r.enabled=false;return r;
    }
}
