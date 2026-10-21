using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;

public static class FrontDownAimTrialBuilder
{
    private const string Root="Assets/RightWalkPrototype/";
    private const string Art=Root+"Art/FrontDownAim/";
    private const string Scene="Assets/RightWalkPrototype/Scenes/Archive/SideScrollFrontDownAimTrial.unity";
    [Serializable] private sealed class Report
    {
        public bool passed;
        public int poseSamples, mirroredSamples, raycastHits,bodyCursorSamples,behindMuzzleSamples,outsideBodyRejections;
        public float maxPixelError, referenceHeight, frontHeight, referenceGunLength, frontGunLength, pixelsPerUnit;
        public float gunLengthRatio,maxAspectError;
        public bool visualWarpApplied;
        public string scene;
    }
    [MenuItem("Tools/Right Walk Prototype/Build Front Down Aim Trial")]
    public static void Build()
    {
        if(Application.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play and save first.");
        string png=Art+"front-down-atlas-v30.png";AssetDatabase.ImportAsset(png,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(png);importer.textureType=TextureImporterType.Default;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;importer.SaveAndReimport();
        AssetDatabase.ImportAsset(Art+"front-down-pose-v30.json",ImportAssetOptions.ForceSynchronousImport);
        var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"FrontDownAim.mat");
        if(material==null){material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,Art+"FrontDownAim.mat");}
        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(png);EditorUtility.SetDirty(material);
        var scene=EditorSceneManager.OpenScene("Assets/RightWalkPrototype/Scenes/Archive/SideScrollTestMap.unity");
        EditorSceneManager.SaveScene(scene,Scene);
        var walker=UnityEngine.Object.FindFirstObjectByType<RightFacingWalker>();
        if(walker==null)throw new InvalidOperationException("The map has no sprite walker.");
        walker.GetComponent<SideScrollMapPreview>().SetRemoteGrounded(true);
        var aim=walker.GetComponentInChildren<RightFacingAimRig>();
        var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();
        Bounds reference=VisibleBounds(walker.gameObject);
        var weapon=aim.GetComponentInChildren<SpriteRenderer>();
        float oldGun=VisibleBounds(weapon.gameObject).size.x;
        var overlay=walker.GetComponent<FrontDownAimOverlay>();if(overlay==null)overlay=walker.gameObject.AddComponent<FrontDownAimOverlay>();
        overlay.poseData=AssetDatabase.LoadAssetAtPath<TextAsset>(Art+"front-down-pose-v30.json");overlay.material=material;
        overlay.Initialize();var data=overlay.Data;
        aim.maxAngle=90;
        float top=float.MaxValue;foreach(var layer in data.layers)if(layer.id=="head")top=layer.bounds[1];
        overlay.pixelsPerUnit=(data.origin[1]-top)/reference.size.y;
        overlay.soleHeight=reference.min.y-walker.transform.position.y;
        overlay.standingTrialOnly=true;aim.frontDownAim=overlay;aim.mouseAim=true;
        EditorUtility.SetDirty(aim);EditorUtility.SetDirty(overlay);EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        var report=new Report {scene=Scene,referenceHeight=reference.size.y,frontHeight=(data.origin[1]-top)/overlay.pixelsPerUnit,
            referenceGunLength=oldGun,pixelsPerUnit=overlay.pixelsPerUnit};
        foreach(var layer in data.layers)if(layer.id=="weapon")report.frontGunLength=(layer.bounds[3]-layer.bounds[1])/overlay.pixelsPerUnit;
        report.gunLengthRatio=report.frontGunLength/oldGun;
        string output=Path.GetFullPath("LocalTools/Analysis/FrontDownUnity");Directory.CreateDirectory(output);
        ValidateMath(data,report);
        ValidateRestoredGeometry(overlay,report);
        foreach(int sign in new[]{1,-1})
        {
            walker.GetComponent<SideFacingPreview>().SetFacing(sign);
            foreach(float degrees in new[]{0f,-17.5f,-35f})
            {
                overlay.ShowAngle(degrees*Mathf.Deg2Rad);
                Vector3 muzzle=overlay.MuzzlePosition;Vector2 ray=overlay.AimDirection;
                aim.SetAimTarget((Vector2)muzzle+ray*3);
                if(!overlay.IsActive||!aim.TargetWithinRange)throw new InvalidOperationException("Front aim did not activate.");
                float miss=Mathf.Abs(Vector3.Cross(aim.MuzzlePosition-muzzle,ray).z);
                if(miss>.0001f||Vector2.Dot(aim.AimDirection,ray)<.99999f)throw new InvalidOperationException("Muzzle and shot path disagree: sign="+sign+" angle="+degrees+" selected="+overlay.LocalAimDegrees+" miss="+miss);
                var target=new GameObject("Front aim validation target");target.transform.position=(Vector2)muzzle+ray*.25f;
                var collider=target.AddComponent<BoxCollider2D>();collider.size=Vector2.one*.08f;Physics2D.SyncTransforms();
                aim.TraceShot(aim.MuzzlePosition,aim.AimDirection,.5f,out Collider2D hit);
                if(hit!=collider)throw new InvalidOperationException("Front muzzle ray did not hit the validation target.");
                report.raycastHits++;UnityEngine.Object.DestroyImmediate(target);
                if(sign<0)report.mirroredSamples++;else report.poseSamples++;
            }
            ValidateBodyCursor(walker,aim,overlay,report);
            aim.ResetAim();if(overlay.IsActive)throw new InvalidOperationException("The old pose was not restored.");
        }
        walker.GetComponent<SideFacingPreview>().SetFacing(1);
        if(report.behindMuzzleSamples==0)throw new InvalidOperationException("No behind-muzzle cursor samples were tested.");
        report.passed=true;
        // SpriteRenderer changes are submitted on the next editor frame, unlike mesh edits.
        int stage=0;double next=EditorApplication.timeSinceStartup+.3;
        EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            if(EditorApplication.timeSinceStartup<next)return;
            next=EditorApplication.timeSinceStartup+.3;
            try
            {
                if(stage==0)
                {
                    overlay.enabled=false;aim.SetAimDirection(new Vector2(Mathf.Cos(55*Mathf.Deg2Rad),-Mathf.Sin(55*Mathf.Deg2Rad)));
                }
                else if(stage==1)
                {
                    Capture(camera,walker.transform,Path.Combine(output,"reference.png"));aim.ResetAim();overlay.enabled=true;
                    overlay.ShowAngle(0);
                }
                else if(stage==2){Capture(camera,walker.transform,Path.Combine(output,"front-down-90.png"));overlay.ShowAngle(-35*Mathf.Deg2Rad);}
                else if(stage==3)
                {
                    Capture(camera,walker.transform,Path.Combine(output,"front-down-55.png"));
                    aim.SetAimTarget(walker.transform.TransformPoint(new Vector3(.18f,1.55f)));
                    if(!overlay.IsActive)throw new InvalidOperationException("The torso cursor pose was lost.");
                }
                else
                {
                    Capture(camera,walker.transform,Path.Combine(output,"body-cursor.png"));aim.ResetAim();
                    Comparison(output);
                    File.WriteAllText(Path.Combine(output,"validation.json"),JsonUtility.ToJson(report,true));
                    Debug.Log("FRONT_DOWN_AIM_VALIDATED "+JsonUtility.ToJson(report));
                    EditorApplication.update-=tick;if(Application.isBatchMode)EditorApplication.Exit(0);
                }
                stage++;EditorApplication.QueuePlayerLoopUpdate();
            }
            catch(Exception error){EditorApplication.update-=tick;Debug.LogException(error);if(Application.isBatchMode)EditorApplication.Exit(1);}
        };
        EditorApplication.update+=tick;EditorApplication.QueuePlayerLoopUpdate();
    }
    private static void ValidateRestoredGeometry(FrontDownAimOverlay overlay,Report report)
    {
        overlay.ShowAngle(0);
        foreach(string id in new[]{"body","head"})
        {
            FrontDownAimMath.Layer layer=null;foreach(var candidate in overlay.Data.layers)if(candidate.id==id)layer=candidate;
            var mesh=overlay.transform.Find("Front Down "+id).GetComponent<MeshFilter>().sharedMesh;
            if(mesh.vertexCount!=4)throw new InvalidOperationException("The body/head deformation grid is still present.");
            var v=mesh.vertices;var b=layer.bounds;
            float x=Vector3.Distance(v[0],v[1])/(b[2]-b[0]),y=Vector3.Distance(v[1],v[2])/(b[3]-b[1]);
            report.maxAspectError=Mathf.Max(report.maxAspectError,Mathf.Abs(x-y));
        }
        if(report.maxAspectError>.0000001f)throw new InvalidOperationException("Body/head aspect ratio was not restored.");
        overlay.Deactivate();
    }
    private static void ValidateBodyCursor(RightFacingWalker walker,RightFacingAimRig aim,FrontDownAimOverlay overlay,Report report)
    {
        var body=walker.GetComponent<Collider2D>();Physics2D.SyncTransforms();
        foreach(float x in new[]{-.18f,0f,.18f})foreach(float y in new[]{.5f,.8f,1.05f,1.3f,1.55f})
        {
            Vector2 target=walker.transform.TransformPoint(new Vector3(x,y));
            if(!body.OverlapPoint(target))throw new InvalidOperationException("The body cursor sample is outside the player collider.");
            aim.SetAimTarget(target);Physics2D.SyncTransforms();
            if(!overlay.IsActive||!aim.TargetWithinRange||overlay.LocalAimDegrees< -90.001f||overlay.LocalAimDegrees> -54.999f)
                throw new InvalidOperationException("A cursor inside the body cancelled down aim: "+target);
            if(Vector2.Dot(target-(Vector2)aim.MuzzlePosition,aim.AimDirection)<0)report.behindMuzzleSamples++;
            var down=(Vector2)walker.transform.TransformVector(Vector3.down).normalized;
            if(Vector2.Dot(aim.AimDirection,down)<Mathf.Cos(35.01f*Mathf.Deg2Rad))throw new InvalidOperationException("The bullet ray turned backwards.");
            report.bodyCursorSamples++;
        }
        foreach(var local in new[]{new Vector2(2,1.5f),new Vector2(0,2.5f)})
        {
            aim.SetAimTarget(walker.transform.TransformPoint(local));
            if(overlay.IsActive)throw new InvalidOperationException("Down aim captured a normal side/up target.");
            report.outsideBodyRejections++;
        }
    }
    private static void Comparison(string output)
    {
        var result=new Texture2D(1280,800,TextureFormat.RGBA32,false);
        int x=0;foreach(string name in new[]{"reference.png","front-down-55.png"})
        {
            var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes(Path.Combine(output,name)));
            result.SetPixels(x,0,640,800,source.GetPixels());x+=640;UnityEngine.Object.DestroyImmediate(source);
        }
        result.Apply();File.WriteAllBytes(Path.Combine(output,"comparison.png"),result.EncodeToPNG());UnityEngine.Object.DestroyImmediate(result);
    }
    private static void ValidateMath(FrontDownAimMath.Data data,Report report)
    {
        var pose=new FrontDownAimMath.Pose(data.arms.Length);
        foreach(var sample in data.samples)
        {
            FrontDownAimMath.Evaluate(data,sample.degrees*Mathf.Deg2Rad,pose);
            report.maxPixelError=Mathf.Max(report.maxPixelError,Vector2.Distance(pose.muzzle,FrontDownAimMath.V(sample.muzzle)));
            for(int i=0;i<pose.arms.Length;i++)
            {
                var expected=sample.arms[i];var actual=pose.arms[i];
                report.maxPixelError=Mathf.Max(report.maxPixelError,Vector2.Distance(actual.wrist,FrontDownAimMath.V(expected.wrist)));
                var upper=actual.upper;var fore=actual.fore;
                float[] u={upper.a,upper.b,upper.c,upper.d,upper.e,upper.f},f={fore.a,fore.b,fore.c,fore.d,fore.e,fore.f};
                for(int j=0;j<6;j++)report.maxPixelError=Mathf.Max(report.maxPixelError,Mathf.Abs(u[j]-expected.upperMatrix[j]),Mathf.Abs(f[j]-expected.foreMatrix[j]));
            }
        }
        if(report.maxPixelError>.01f)throw new InvalidOperationException("Browser pose parity failed: "+report.maxPixelError);
    }
    private static Bounds VisibleBounds(GameObject root)
    {
        var textures=new Dictionary<string,Texture2D>();Bounds result=default;bool any=false;
        foreach(var r in root.GetComponentsInChildren<SpriteRenderer>())
        {
            if(!r.enabled||r.sprite==null)continue;
            var sprite=r.sprite;string path=AssetDatabase.GetAssetPath(sprite.texture);
            if(!textures.TryGetValue(path,out var tex)){tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes(path));textures.Add(path,tex);}
            var rect=sprite.rect;var pixels=tex.GetPixels32();int minX=(int)rect.xMax,minY=(int)rect.yMax,maxX=-1,maxY=-1;
            for(int y=(int)rect.y;y<(int)rect.yMax;y++)for(int x=(int)rect.x;x<(int)rect.xMax;x++)if(pixels[y*tex.width+x].a>=32)
            {minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
            if(maxX<0)continue;
            foreach(int x in new[]{minX,maxX+1})foreach(int y in new[]{minY,maxY+1})
            {
                var p=new Vector3((x-rect.x-sprite.pivot.x)/sprite.pixelsPerUnit,(y-rect.y-sprite.pivot.y)/sprite.pixelsPerUnit,0);
                if(r.flipX)p.x=-p.x;if(r.flipY)p.y=-p.y;p=r.transform.TransformPoint(p);
                if(!any){result=new Bounds(p,Vector3.zero);any=true;}else result.Encapsulate(p);
            }
        }
        foreach(var tex in textures.Values)UnityEngine.Object.DestroyImmediate(tex);
        if(!any)throw new InvalidOperationException("No visible reference pixels.");return result;
    }
    private static void Capture(Camera camera,Transform root,string path)
    {
        var oldPosition=camera.transform.position;float oldSize=camera.orthographicSize;var oldTarget=camera.targetTexture;
        var rt=RenderTexture.GetTemporary(640,800,24);var oldActive=RenderTexture.active;
        try
        {
            camera.orthographicSize=1.7f;camera.transform.position=new Vector3(root.position.x,root.position.y+1.4f,-10);
            RenderTexture.active=rt;GL.Clear(true,true,camera.backgroundColor);
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(640,800,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,640,800),0,0);image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
        }
        finally{camera.targetTexture=oldTarget;camera.orthographicSize=oldSize;camera.transform.position=oldPosition;RenderTexture.active=oldActive;RenderTexture.ReleaseTemporary(rt);}
    }
}
