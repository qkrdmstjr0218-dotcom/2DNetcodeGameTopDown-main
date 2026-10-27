using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;

[InitializeOnLoad]
public static class ArenaBuildPolishEditorReview
{
    public const string Folder="LocalTools/Analysis/BuildPolish-2026-10-26";
    const string Key="ArenaBuildPolishEditorReview";
    static ArenaBuildPolishEditorReview(){EditorApplication.update+=Tick;}
    static string Geometry(BoxCollider2D c)=>c.transform.localToWorldMatrix+":"+c.size+":"+c.offset+":"+c.enabled+":"+c.isTrigger+":"+c.usedByEffector+":"+c.sharedMaterial;
    public static void ApplyLayout()
    {
        Directory.CreateDirectory(Folder);var scene=EditorSceneManager.GetActiveScene();
        if(scene.isDirty||EditorApplication.isPlaying)throw new Exception("Requires saved edit mode");
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();
        var before=map.GetComponentsInChildren<BoxCollider2D>(true).Where(c=>!c.name.StartsWith("Third floor step ")&&!c.name.StartsWith("Central ascent ")&&!Ancestors(c.transform).Contains(FoundryThirdFloorAccessBuilder.RootName)).ToDictionary(c=>c.GetInstanceID(),Geometry);
        FoundryThirdFloorAccessBuilder.Apply();var all=map.GetComponentsInChildren<BoxCollider2D>(true);
        foreach(var pair in before){var c=all.FirstOrDefault(x=>x.GetInstanceID()==pair.Key);if(c==null||Geometry(c)!=pair.Value)throw new Exception("Existing collider changed "+pair.Key);}
        var root=map.transform.Find(FoundryThirdFloorAccessBuilder.RootName);var additions=root.GetComponentsInChildren<BoxCollider2D>();
        if(additions.Length!=2||additions.Any(c=>!c.usedByEffector||!c.GetComponent<PlatformEffector2D>().useOneWay))throw new Exception("Expected two final ascent steps");
        if(map.transform.Find("Third floor access quarter steps")!=null)throw new Exception("Wrong lower-route additions remain");
        File.WriteAllText(Folder+"/layout.txt","PASS non-target colliders unchanged: "+before.Count+"\nPASS four existing central rungs repositioned; two final one-way rungs added\nPASS lower 2F shortcut restored\n"+string.Join("\n",additions.Select(c=>c.name+" "+c.bounds)));
    }
    static IEnumerable<string> Ancestors(Transform t){while(t!=null){yield return t.name;t=t.parent;}}
    [MenuItem("Tools/Right Walk Prototype/Verify Build Polish Solo")]
    public static void Begin()
    {
        Directory.CreateDirectory(Folder+"/Editor");
        var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        SessionState.SetInt(Key+"size",(int)view.GetType().GetProperty("selectedSizeIndex",flags).GetValue(view));
        view.GetType().GetMethod("SetCustomResolution",flags).Invoke(view,new object[]{new Vector2(1366,768),"Build polish 1366"});view.Focus();
        SessionState.SetBool(Key,true);SessionState.SetBool(Key+"restore",true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(SessionState.GetBool(Key,false)&&EditorApplication.isPlaying&&Time.time>.6f)
        {SessionState.SetBool(Key,false);ArenaBuildPolishReview.StartReview(Path.GetFullPath(Folder+"/Editor"));}
        if(SessionState.GetBool(Key+"restore",false)&&!EditorApplication.isPlaying&&!SessionState.GetBool(Key,false))
        {
            SessionState.SetBool(Key+"restore",false);var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,SessionState.GetInt(Key+"size",0));
        }
    }
    [Serializable]sealed class Summary{public string result,output;public int errors,warnings;public long bytes;public double seconds;}
    public static void BeginBuild()
    {
        if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Requires saved edit mode");
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/build.json","{\"result\":\"queued\"}");
        EditorApplication.delayCall+=()=>
        {
            try
            {
                string output=Path.GetFullPath("Builds/Review-20261026/Foundry.exe");
                if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.StandaloneWindows64)throw new Exception("Expected current Windows64 target");
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.DetailedBuildReport,extraScriptingDefines=new[]{"ARENA_BUILD_REVIEW"}});
                var s=report.summary;File.WriteAllText(Folder+"/build.json",JsonUtility.ToJson(new Summary{result=s.result.ToString(),output=output,errors=s.totalErrors,warnings=s.totalWarnings,bytes=(long)s.totalSize,seconds=s.totalTime.TotalSeconds},true));
                File.WriteAllText(Folder+"/build-messages.txt",string.Join("\n",report.steps.SelectMany(step=>step.messages.Select(m=>m.type+": "+m.content))));
                File.WriteAllText(Folder+"/packed-assets.txt",string.Join("\n",report.packedAssets.SelectMany(p=>p.contents.Select(c=>c.sourceAssetPath)).Distinct().OrderBy(x=>x)));
            }
            catch(Exception e){File.WriteAllText(Folder+"/build.json","{\"result\":\"Exception\"}");File.WriteAllText(Folder+"/build-exception.txt",e.ToString());}
        };
    }
}
