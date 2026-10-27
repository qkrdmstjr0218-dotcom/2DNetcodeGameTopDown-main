using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RightWalkPrototype;
public static class FoundryWallArtBuilder
{
    public const string Skin="Unified foundry wall";
    public const string PitSkin="Unified hazard bed";
    [MenuItem("Tools/Right Walk Prototype/Finish Foundry Vertical Walls")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)throw new Exception("Requires saved Foundry edit mode");
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();string before=FoundrySurfaceArtBuilder.Geometry(map);
        Undo.RegisterFullObjectHierarchyUndo(map.gameObject,"Finish foundry walls");ApplyVisuals(map);
        string after=FoundrySurfaceArtBuilder.Geometry(map);if(before!=after)throw new Exception("Wall art changed collision");
        const string folder="LocalTools/Analysis/MapWalls-2026-10-27";System.IO.Directory.CreateDirectory(folder);System.IO.File.WriteAllText(folder+"/geometry-before.json",before);System.IO.File.WriteAllText(folder+"/geometry-after.json",after);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    public static void ApplyVisuals(ArenaMapSettings map)
    {
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        foreach(var wall in map.GetComponentsInChildren<BoxCollider2D>(true))
        {
            var owner=wall.transform;var scale=owner.lossyScale;float width=Mathf.Abs(scale.x*wall.size.x),height=Mathf.Abs(scale.y*wall.size.y);
            if(width>=1||height<3||owner.GetComponent<SpriteRenderer>()==null||wall.GetComponent<ArenaCover>()!=null)continue;
            var old=owner.Find(Skin);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            owner.GetComponent<SpriteRenderer>().color=new Color(.20f,.28f,.33f);
            var root=new GameObject(Skin).transform;root.SetParent(owner,false);root.localPosition=wall.offset;root.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1);
            void Part(string name,float x,float y,float w,float h,Color color,int order=3)
            {
                var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=new Vector3(x,y,0);go.transform.localScale=new Vector3(w/sprite.bounds.size.x,h/sprite.bounds.size.y,1);
                var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;
            }
            var dark=new Color(.10f,.18f,.23f);var rim=new Color(.31f,.42f,.47f);
            Part("Wall panel recess",0,0,width-.065f,height-.08f,dark);
            foreach(float side in new[]{-1f,1f})Part("Wall vertical bevel",side*(width*.5f-.018f),0,.025f,height-.04f,rim,4);
            int count=Mathf.Max(1,Mathf.CeilToInt(height/3.4f));float step=height/count;
            for(int i=0;i<count;i++)
            {
                float y=-height*.5f+(i+.5f)*step;
                Part("Wall armour panel",0,y,width-.12f,step-.10f,new Color(.17f,.25f,.30f));
                Part("Wall panel inner bevel",-width*.5f+.085f,y,.015f,step-.17f,new Color(.25f,.35f,.40f),4);
            }
            for(int i=1;i<count;i++)
            {
                float y=-height*.5f+i*step;
                Part("Wall panel joint",0,y,width-.055f,.035f,rim,4);
                foreach(float side in new[]{-1f,1f})Part("Wall fixing bolt",side*(width*.5f-.07f),y+.065f,.032f,.032f,rim,5);
            }
            foreach(float side in new[]{-1f,1f})
            {
                Part("Wall end cap",0,side*(height*.5f-.035f),width,.07f,rim,4);
                Part("Wall caution insert",0,side*(height*.5f-.16f),width-.16f,.045f,new Color(.83f,.62f,.28f),5);
            }
        }
        ApplyPitBeds(map);
    }
    public static void ApplyPitBeds(ArenaMapSettings map)
    {
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        foreach(var bed in map.GetComponentsInChildren<BoxCollider2D>(true).Where(c=>c.name=="Hazard bed"||c.name=="West pit bed"))
        {
            var owner=bed.transform;var scale=owner.lossyScale;float width=Mathf.Abs(scale.x*bed.size.x),height=Mathf.Abs(scale.y*bed.size.y);
            var old=owner.Find(PitSkin);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            owner.GetComponent<SpriteRenderer>().color=new Color(.20f,.28f,.33f);
            var root=new GameObject(PitSkin).transform;root.SetParent(owner,false);root.localPosition=bed.offset;root.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1);
            void Part(string name,float x,float y,float w,float h,Color color,int order=3)
            {
                var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=new Vector3(x,y,0);go.transform.localScale=new Vector3(w/sprite.bounds.size.x,h/sprite.bounds.size.y,1);
                var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;
            }
            var rim=new Color(.31f,.42f,.47f);
            Part("Hazard bed recessed face",0,0,width-.08f,height-.10f,new Color(.10f,.18f,.23f));
            for(int i=0;i<3;i++)Part("Hazard bed armour panel",(i-1)*width/3,0,width/3-.065f,height-.16f,new Color(.17f,.25f,.30f));
            foreach(float side in new[]{-1f,1f})
            {
                Part("Hazard bed horizontal bevel",0,side*(height*.5f-.02f),width,.035f,rim,4);
                Part("Hazard bed end cap",side*(width*.5f-.035f),0,.07f,height,rim,4);
                foreach(float y in new[]{-.12f,.12f})Part("Hazard bed fixing bolt",side*(width*.5f-.17f),y,.035f,.035f,rim,5);
                Part("Hazard bed caution inlay",side*(width*.5f-.4f),.11f,.3f,.035f,new Color(.83f,.62f,.28f),5);
            }
        }
    }
}
