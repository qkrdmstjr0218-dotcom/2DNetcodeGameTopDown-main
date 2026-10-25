using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;
public static class FoundryArtBuilder
{
    public const string RootName="Foundry art - full map";
    const string LegacyRoot="Foundry art - central depot sample";
    const string Folder="Assets/RightWalkPrototype/Resources/FoundryArt/";
    static readonly Color Steel=new(.20f,.28f,.33f),Edge=new(.72f,.77f,.78f),Pass=new(.44f,.71f,.75f);
    public static void ImportTextures()
    {
        foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Folder.TrimEnd('/')}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=path.EndsWith("DepotWall.png")?80:256;importer.spritePivot=Vector2.one*.5f;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Bilinear;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();
        }
    }
    public static string Geometry(ArenaMapSettings map)
    {
        return string.Join("\n",map.GetComponentsInChildren<BoxCollider2D>(true).Select(c=>c.GetInstanceID()+":"+c.transform.localToWorldMatrix+":"+c.size+":"+c.offset+":"+c.enabled+":"+c.usedByEffector+":"+c.isTrigger+":"+c.sharedMaterial));
    }
    [MenuItem("Tools/Right Walk Prototype/Apply Full Foundry Background")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)throw new Exception("Requires saved Foundry edit mode");
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();string before=Geometry(map);
        var wall=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"DepotWall.png");var solid=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RightWalkPrototype/Art/MapSolid.png");
        if(wall==null||solid==null)throw new Exception("Import art textures first");
        Undo.RegisterFullObjectHierarchyUndo(map.gameObject,"Full Foundry background");
        foreach(string name in new[]{RootName,LegacyRoot}){var old=map.transform.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
        var root=new GameObject(RootName).transform;root.SetParent(map.transform,false);
        SpriteRenderer Part(string name,Vector2 at,Vector2 size,Color color,int order,Sprite sprite)
        {
            var go=new GameObject(name);go.transform.position=at;go.transform.SetParent(root,true);
            var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;
            go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);return r;
        }
        // Six bands include the basement and the space above the highest deck.
        // Reuse the approved texture; all panels remain behind gameplay geometry.
        for(int row=-1;row<=4;row++)for(int column=-1;column<=1;column++)
        {
            var tint=row<0?new Color(.79f,.85f,.9f):row>=3?new Color(.88f,.94f,1):new Color(.91f,.94f,.96f);
            if(column!=0)tint=new Color(tint.r*.95f,tint.g*.95f,tint.b*.95f,1);
            if(row==1&&column==0)tint=Color.white;
            Part(row==1&&column==0?"Depot wall artwork":"Backdrop band "+row+" bay "+column,new Vector2(column*26,(row+.5f)*map.floorSpacing),new Vector2(26,map.floorSpacing),tint,-50,wall);
        }
        string[] decks={"Middle west hall","Middle centre deck","Middle east hall","Upper west grate","Upper core","Upper east grate"};
        foreach(string name in decks)
        {
            var deck=map.transform.Find(name);if(deck==null)throw new Exception("Missing deck "+name);
            var collider=deck.GetComponent<BoxCollider2D>();var b=collider.bounds;deck.GetComponent<SpriteRenderer>().color=Steel;
            foreach(var r in deck.GetComponentsInChildren<SpriteRenderer>())
            {
                if(r.name=="Solid edge")r.color=Edge;
                else if(r.name=="Passable dash")r.color=Pass;
            }
            Part(name+" continuous inset",new Vector2(b.center.x,b.center.y+.015f),new Vector2(b.size.x,.075f),new Color(.10f,.18f,.23f),3,solid);
            Part(name+" lower bevel",new Vector2(b.center.x,b.min.y+.03f),new Vector2(b.size.x,.025f),new Color(.25f,.33f,.38f),3,solid);
        }
        // Repeated black slots can be mistaken for the pass-through edge. Hide
        // decoration only, even outside the sample; never remove collider owners.
        foreach(var r in map.GetComponentsInChildren<SpriteRenderer>(true))
            if(r.name=="Grate opening"||r.name=="Deck joint")r.enabled=false;
        FoundryCoverArtBuilder.ApplyVisuals(map);
        foreach(var t in map.GetComponentsInChildren<TextMesh>())if(t.name=="Depot sign"){t.GetComponent<MeshRenderer>().sortingOrder=-40;t.color=new Color(.42f,.58f,.62f);}
        if(before!=Geometry(map))throw new Exception("Art changed collider geometry");
        if(root.GetComponentsInChildren<Collider2D>(true).Length!=0)throw new Exception("Art has colliders");
        System.IO.Directory.CreateDirectory("LocalTools/Analysis/FoundryArt");
        System.IO.File.WriteAllText("LocalTools/Analysis/FoundryArt/geometry.txt","PASS collider transforms/shapes/materials/enabled/effector flags unchanged\nPASS art colliders=0\n");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
}
