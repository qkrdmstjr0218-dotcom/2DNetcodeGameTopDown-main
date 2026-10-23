using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;
public static class FoundryMedicalStationsBuilder
{
    public const string RootName="Medical supply stations";
    [MenuItem("Tools/Right Walk Prototype/Add Foundry Medical Stations")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)throw new Exception("Requires saved Foundry edit mode");
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();BuildVisuals(map);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    public static void BuildVisuals(ArenaMapSettings map)
    {
        if(map==null||map.transform.Find(RootName)!=null)return;
        var root=new GameObject(RootName).transform;root.SetParent(map.transform,false);
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        var green=new Color(.25f,.8f,.45f);
        void Part(Transform parent,string name,Vector2 at,Vector2 size,Color color,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=at;go.transform.localScale=size;
            var image=go.AddComponent<SpriteRenderer>();image.sprite=sprite;image.color=color;image.sortingOrder=order;
        }
        if(map.healthSupplies==null)return;
        foreach(var at in map.healthSupplies)
        {
            var station=new GameObject("Medical station "+at.x).transform;station.SetParent(root,false);station.localPosition=new Vector3(at.x,at.y-.55f,0);
            Part(station,"Recess",new Vector2(0,1.35f),new Vector2(3.6f,2.7f),new Color(.045f,.12f,.115f),-75);
            Part(station,"Header",new Vector2(0,2.48f),new Vector2(3.3f,.045f),green,-60);
            Part(station,"Cabinet",new Vector2(1,.92f),new Vector2(.9f,1.8f),new Color(.18f,.30f,.3f),-65);
            Part(station,"Cabinet inset",new Vector2(1,.92f),new Vector2(.7f,1.55f),new Color(.08f,.17f,.19f),-64);
            Part(station,"Medical cross vertical",new Vector2(1,1.35f),new Vector2(.1f,.42f),green,-62);
            Part(station,"Medical cross horizontal",new Vector2(1,1.35f),new Vector2(.42f,.1f),green,-61);
            for(int i=0;i<3;i++)
            {
                Part(station,"Stored kit",new Vector2(-1,.3f+i*.48f),new Vector2(.8f,.35f),new Color(.29f,.42f,.39f),-65);
                Part(station,"Kit seal",new Vector2(-1,.3f+i*.48f),new Vector2(.08f,.22f),green,-64);
            }
            Part(station,"Recovery pad",new Vector2(0,.035f),new Vector2(1.2f,.045f),green,-2);
            var label=new GameObject("Medical sign");label.transform.SetParent(station,false);label.transform.localPosition=new Vector3(0,2.17f,0);
            var text=label.AddComponent<TextMesh>();text.text="MED / SUPPLY";text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=52;text.characterSize=.038f;text.anchor=TextAnchor.MiddleCenter;text.color=green;
            var ink=label.GetComponent<MeshRenderer>();ink.sharedMaterial=text.font.material;ink.sortingOrder=-58;
        }
    }
}
