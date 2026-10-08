using System;
using System.IO;
using UnityEngine;
using TopDownShooter.Diagnostics;
public static class CaptureLowerWalkAtlas
{
    public static string Main()
    {
        if(!Application.isPlaying)throw new Exception("Run in Play");
        var s=GameObject.Find("Animated comparison").GetComponent<WalkSkeletonStudy>();
        bool animated=s.animate,bones=s.showBones;float old=s.cycle;
        var nodes=s.GetComponentsInChildren<Transform>();var layers=new int[nodes.Length];
        for(int i=0;i<nodes.Length;i++){layers[i]=nodes[i].gameObject.layer;nodes[i].gameObject.layer=31;}
        var c=new GameObject("Atlas capture camera").AddComponent<Camera>();
        var center=s.transform.position+new Vector3(.015f,.26f,0);
        c.transform.position=center+new Vector3(2.2f,3,-5);c.transform.LookAt(center);
        c.orthographic=true;c.orthographicSize=.36f;c.enabled=false;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=Color.clear;
        c.cullingMask=1<<31;
        // Waist proxy anchors the redraw; this is a rendered pose guide, not final art.
        var pelvis=GameObject.CreatePrimitive(PrimitiveType.Sphere);pelvis.name="Capture pelvis proxy";
        pelvis.layer=31;
        pelvis.transform.position=s.transform.position+Vector3.up*.44f;pelvis.transform.localScale=new Vector3(.15f,.09f,.16f);
        var mat=new Material(Shader.Find("Unlit/Color"));mat.color=new Color(.08f,.09f,.12f);pelvis.GetComponent<Renderer>().sharedMaterial=mat;
        var belt=GameObject.CreatePrimitive(PrimitiveType.Cube);belt.transform.position=s.transform.position+Vector3.up*.475f;belt.transform.localScale=new Vector3(.15f,.024f,.16f);
        belt.layer=31;
        var beltMat=new Material(Shader.Find("Unlit/Color"));beltMat.color=new Color(.8f,.85f,.9f);belt.GetComponent<Renderer>().sharedMaterial=beltMat;
        var rt=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32);rt.Create();c.targetTexture=rt;
        var atlas=new Texture2D(2048,1024,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
        try
        {
            s.animate=false;s.showBones=false;
            for(int i=0;i<8;i++)
            {
                s.SetPose(i/8f);c.Render();RenderTexture.active=rt;
                atlas.ReadPixels(new Rect(0,0,512,512),(i%4)*512,(1-i/4)*512,false);
            }
            atlas.Apply();string path="Assets/Art/OperatorPrototype/operator-lower-walk-render-guide-v1.png";
            File.WriteAllBytes(path,atlas.EncodeToPNG());return "Saved fixed-camera 8 phase guide: "+path;
        }
        finally
        {
            s.animate=animated;s.showBones=bones;s.SetPose(old);RenderTexture.active=previous;
            for(int i=0;i<nodes.Length;i++)nodes[i].gameObject.layer=layers[i];
            c.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(atlas);
            UnityEngine.Object.Destroy(c.gameObject);UnityEngine.Object.Destroy(pelvis);UnityEngine.Object.Destroy(belt);UnityEngine.Object.Destroy(mat);UnityEngine.Object.Destroy(beltMat);
        }
    }
}
