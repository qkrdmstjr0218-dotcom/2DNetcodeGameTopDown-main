using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class BuildMasterReassembly
{
    public static string Main()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        const string path="Assets/Art/OperatorPrototype/operator-right-fullbody-master-v3.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.isReadable=true;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        // All boundaries share exactly the same coordinates; no generated repainting.
        const string folder="Assets/Art/OperatorPrototype/MasterRigV1";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Art/OperatorPrototype","MasterRigV1");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var cam=new GameObject("Master comparison camera").AddComponent<Camera>();cam.tag="MainCamera";cam.orthographic=true;cam.orthographicSize=.78f;cam.transform.position=new Vector3(0,0,-10);cam.backgroundColor=new Color(.045f,.06f,.085f);cam.clearFlags=CameraClearFlags.SolidColor;
        var mat=new Material(Shader.Find("Sprites/Default"));mat.mainTexture=tex;
        AssetDatabase.CreateAsset(mat,folder+"/MasterMaterial.mat");
        var original=new GameObject("01 Original artwork");var sr=original.AddComponent<SpriteRenderer>();
        var sprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),Vector2.one*.5f,1000,0,SpriteMeshType.FullRect);AssetDatabase.CreateAsset(sprite,folder+"/Original.asset");sr.sprite=sprite;original.transform.position=new Vector3(-.60f,0,0);
        var assembled=new GameObject("02 Reassembled parts");assembled.transform.position=new Vector3(.60f,0,0);
        // Normalized source positions (top-left coordinates); joint pivots do not affect rest pose.
        string[] names={"Upper body with arms and weapon","Pelvis","Near thigh","Far thigh","Near shin","Far shin","Near foot","Far foot"};
        float[,] bounds={{0,0,1,.45f},{0,.45f,1,.60f},{0,.60f,.51f,.72f},{.51f,.60f,1,.70f},{0,.72f,.51f,.86f},{.51f,.70f,1,.81f},{0,.86f,.51f,1},{.51f,.81f,1,1}};
        Vector2[] pivots={new Vector2(.48f,.43f),new Vector2(.50f,.49f),new Vector2(.46f,.59f),new Vector2(.57f,.59f),new Vector2(.41f,.70f),new Vector2(.61f,.69f),new Vector2(.33f,.86f),new Vector2(.62f,.81f)};
        for(int i=0;i<8;i++)
        {
            var pivot=new Vector3((pivots[i].x-.5f)*tex.width/1000f,(.5f-pivots[i].y)*tex.height/1000f,0);
            var node=new GameObject(names[i]);node.transform.SetParent(assembled.transform,false);node.transform.localPosition=pivot;
            var mesh=new Mesh();var vertices=new Vector3[4];var uv=new Vector2[4];
            float x0=bounds[i,0],y0=bounds[i,1],x1=bounds[i,2],y1=bounds[i,3];
            var p=new[]{new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1),new Vector2(x0,y1)};
            for(int j=0;j<4;j++){uv[j]=new Vector2(p[j].x,1-p[j].y);vertices[j]=new Vector3((p[j].x-.5f)*tex.width/1000f,(.5f-p[j].y)*tex.height/1000f,0)-pivot;}
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateBounds();mesh.name=names[i];AssetDatabase.CreateAsset(mesh,folder+"/Part"+i+".asset");
            node.AddComponent<MeshFilter>().sharedMesh=mesh;node.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }
        // Compare both renders at identical camera position before saving the side-by-side scene.
        cam.orthographicSize=.78f;original.transform.position=assembled.transform.position=Vector3.zero;
        var rt=new RenderTexture(1024,1536,24);cam.targetTexture=rt;
        Func<Color32[]> render=()=>{cam.Render();var prev=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();var data=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=prev;return data;};
        assembled.SetActive(false);var a=render();original.SetActive(false);assembled.SetActive(true);var b=render();int changed=0,maxDelta=0;
        for(int i=0;i<a.Length;i++){int d=Math.Max(Math.Abs(a[i].r-b[i].r),Math.Max(Math.Abs(a[i].g-b[i].g),Math.Abs(a[i].b-b[i].b)));maxDelta=Math.Max(maxDelta,d);if(d>2)changed++;}
        cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);
        original.SetActive(true);original.transform.position=new Vector3(-.6f,0,0);assembled.transform.position=new Vector3(.6f,0,0);cam.orthographicSize=.83f;
        if(changed>a.Length*.002f)throw new Exception("Reassembly mismatch "+changed+" pixels, max "+maxDelta);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,"Assets/Scenes/OperatorMasterReassembly.unity");
        return "Original left / reassembly right. Eight source-UV parts; differing pixels (>2)="+changed+"/"+a.Length+", max channel delta="+maxDelta+". Rest pose only; rectangular first-pass cuts are not final articulation masks.";
    }
}
