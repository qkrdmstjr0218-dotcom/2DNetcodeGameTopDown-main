using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;
public static class FoundryDepotSupplyBuilder
{
    public static void Configure(ArenaMapSettings map)
    {
        map.shotgunSupplies=Array.Empty<Vector2>();
        map.ammoSupplies=new[]{new ArenaMapSettings.AmmoSupply{position=new Vector2(-1.1f,4.02f),ammo=ArenaObjectiveMatch.SupplyAmmo.Shotgun},new ArenaMapSettings.AmmoSupply{position=new Vector2(0,4.02f),ammo=ArenaObjectiveMatch.SupplyAmmo.Rifle},new ArenaMapSettings.AmmoSupply{position=new Vector2(1.1f,4.02f),ammo=ArenaObjectiveMatch.SupplyAmmo.Grenade}};
    }
    [MenuItem("Tools/Right Walk Prototype/Upgrade Foundry Depot Supplies")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)throw new Exception("Requires saved Foundry edit mode");
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();var depot=map.transform.Find(FoundryDepotBuilder.RootName);
        if(depot==null)throw new Exception("Central depot required");
        Undo.RecordObject(map,"Set three typed depot supplies");Configure(map);EditorUtility.SetDirty(map);
        var pad=depot.Find("Supply pad");if(pad!=null)pad.gameObject.SetActive(false);
        if(depot.Find("Typed supply pads")==null)
        {
            var root=new GameObject("Typed supply pads").transform;root.SetParent(depot,false);Undo.RegisterCreatedObjectUndo(root.gameObject,"Add typed depot pads");
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
            foreach(var supply in map.ammoSupplies)
            {
                var go=new GameObject(ArenaObjectiveMatch.SupplyInitial(supply.ammo)+" supply pad");go.transform.SetParent(root,false);go.transform.localPosition=new Vector3(supply.position.x,3.64f,0);go.transform.localScale=new Vector3(.9f,.045f,1);
                var image=go.AddComponent<SpriteRenderer>();image.sprite=sprite;image.sortingOrder=3;image.color=supply.ammo==ArenaObjectiveMatch.SupplyAmmo.Shotgun?new Color(1,.82f,.18f):supply.ammo==ArenaObjectiveMatch.SupplyAmmo.Rifle?new Color(.75f,.45f,1):new Color(.65f,1,.3f);
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
}
