using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RightWalkPrototype;
public static class FoundryConsoleBuilder
{
    [MenuItem("Tools/Right Walk Prototype/Add Foundry Hacking Consoles")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)throw new Exception("Requires saved Foundry edit mode");
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        foreach(var point in UnityEngine.Object.FindFirstObjectByType<ArenaObjectiveLayout>().points)
        {
            if(point.console!=null)continue;
            var root=new GameObject("Hacking console - visual only");root.transform.SetParent(point.transform,false);
            var c=root.AddComponent<ArenaHackingConsole>();point.console=c;
            SpriteRenderer Part(string name,Vector2 at,Vector2 size,Color color,int order)
            {
                var part=new GameObject(name);part.transform.SetParent(root.transform,false);part.transform.localPosition=at;part.transform.localScale=new Vector3(size.x,size.y,1);
                var r=part.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;return r;
            }
            Part("Console foot",new Vector2(0,.08f),new Vector2(.7f,.16f),new Color(.22f,.29f,.33f),-6);
            Part("Console support",new Vector2(0,.34f),new Vector2(.28f,.5f),new Color(.17f,.23f,.27f),-6);
            Part("Console housing",new Vector2(0,.84f),new Vector2(.74f,.57f),new Color(.25f,.32f,.36f),-6);
            c.screen=Part("Console screen",new Vector2(0,.87f),new Vector2(.58f,.31f),Color.black,-4);
            c.status=Part("Console status",new Vector2(0,.65f),new Vector2(.45f,.045f),Color.black,-3);
            c.glow=Part("Console active glow",new Vector2(0,.85f),new Vector2(1.05f,.8f),Color.clear,-8);
            Part("Screen scan line",new Vector2(0,.85f),new Vector2(.38f,.025f),new Color(.7f,.9f,.85f,.45f),-3);
            point.Show(false,false,Color.cyan);EditorUtility.SetDirty(point);
        }
        var cover=GameObject.Find("Lower outer cover 1");if(cover!=null){var at=cover.transform.position;at.x=29;cover.transform.position=at;}
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
}
