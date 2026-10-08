using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TopDownShooter.Diagnostics;
public static class BuildMasterJointStudy
{
    private static Vector2 V(float x,float y)=>new Vector2(x,y);
    private static int[] Triangles(Vector2[] p)
    {
        var ids=new List<int>();for(int i=0;i<p.Length;i++)ids.Add(i);var result=new List<int>();
        Func<Vector2,Vector2,float> cross=(a,b)=>a.x*b.y-a.y*b.x;
        while(ids.Count>3)
        {
            bool found=false;
            for(int i=0;i<ids.Count;i++)
            {
                int a=ids[(i+ids.Count-1)%ids.Count],b=ids[i],c=ids[(i+1)%ids.Count];
                if(cross(p[b]-p[a],p[c]-p[b])<=0)continue;
                bool inside=false;foreach(int j in ids)if(j!=a&&j!=b&&j!=c&&cross(p[b]-p[a],p[j]-p[a])>=0&&cross(p[c]-p[b],p[j]-p[b])>=0&&cross(p[a]-p[c],p[j]-p[c])>=0){inside=true;break;}
                if(inside)continue;result.AddRange(new[]{a,c,b});ids.RemoveAt(i);found=true;break;
            }
            if(!found)throw new Exception("Invalid partition polygon");
        }
        result.AddRange(new[]{ids[0],ids[2],ids[1]});return result.ToArray();
    }
    public static string Main()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/OperatorMasterReassembly.unity",OpenSceneMode.Single);
        var root=GameObject.Find("02 Reassembled parts").transform;
        var tex=GameObject.Find("01 Original artwork").GetComponent<SpriteRenderer>().sprite.texture;
        const string folder="Assets/Art/OperatorPrototype/MasterJointV3";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Art/OperatorPrototype","MasterJointV3");
        // Shared contour paths follow the underside of knee plates and top of boot cuffs.
        // Rings stay whole; these are deliberately not straight cuts at joint height.
        var waist=new[]{V(0,.46f),V(.38f,.46f),V(.42f,.442f),V(.48f,.44f),V(.55f,.425f),V(.61f,.44f),V(1,.44f)};
        var hipNear=new[]{V(0,.615f),V(.36f,.615f),V(.40f,.592f),V(.43f,.56f),V(.46f,.553f),V(.51f,.572f)};
        var hipFar=new[]{V(.51f,.572f),V(.55f,.55f),V(.59f,.56f),V(.635f,.596f),V(1,.596f)};
        var kneeNear=new[]{V(0,.735f),V(.306f,.735f),V(.322f,.742f),V(.342f,.737f),V(.365f,.716f),V(.385f,.710f),V(.40f,.716f),V(.425f,.725f),V(.51f,.725f)};
        var kneeFar=new[]{V(.51f,.71f),V(.563f,.71f),V(.577f,.723f),V(.602f,.731f),V(.622f,.723f),V(.633f,.706f),V(.647f,.695f),V(.671f,.692f),V(1,.692f)};
        var ankleNear=new[]{V(0,.845f),V(.265f,.845f),V(.29f,.845f),V(.31f,.846f),V(.329f,.855f),V(.35f,.856f),V(.367f,.853f),V(.388f,.853f),V(.51f,.853f)};
        var ankleFar=new[]{V(.51f,.815f),V(.571f,.815f),V(.60f,.804f),V(.625f,.804f),V(.65f,.802f),V(.663f,.808f),V(.69f,.829f),V(.70f,.836f),V(1,.836f)};
        var hip=new List<Vector2>(hipNear);for(int i=1;i<hipFar.Length;i++)hip.Add(hipFar[i]);
        Func<Vector2[],Vector2[],Vector2[]> band=(top,bottom)=>{var p=new List<Vector2>(top);for(int i=bottom.Length-1;i>=0;i--)p.Add(bottom[i]);return p.ToArray();};
        var polys=new[]{band(new[]{V(0,0),V(1,0)},waist),band(waist,hip.ToArray()),band(hipNear,kneeNear),band(hipFar,kneeFar),band(kneeNear,ankleNear),band(kneeFar,ankleFar),band(ankleNear,new[]{V(0,1),V(.51f,1)}),band(ankleFar,new[]{V(.51f,1),V(1,1)})};
        Vector2[] pivots={V(.48f,.43f),V(.50f,.49f),V(.46f,.59f),V(.57f,.59f),V(.345f,.719f),V(.60f,.705f),V(.286f,.887f),V(.612f,.845f)};
        var parts=new Transform[8];for(int i=0;i<8;i++)parts[i]=root.GetChild(i);
        for(int i=0;i<8;i++)
        {
            var pivot=new Vector3((pivots[i].x-.5f)*tex.width/1000f,(.5f-pivots[i].y)*tex.height/1000f,0);
            parts[i].localPosition=pivot;
            var mesh=new Mesh();var verts=new Vector3[polys[i].Length];var uv=new Vector2[verts.Length];
            for(int j=0;j<verts.Length;j++){var p=polys[i][j];verts[j]=new Vector3((p.x-.5f)*tex.width/1000f,(.5f-p.y)*tex.height/1000f,0)-pivot;uv[j]=new Vector2(p.x,1-p.y);}
            mesh.vertices=verts;mesh.uv=uv;mesh.triangles=Triangles(polys[i]);mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,folder+"/Part"+i+".asset");parts[i].GetComponent<MeshFilter>().sharedMesh=mesh;
        }
        parts[4].SetParent(parts[2],true);parts[6].SetParent(parts[4],true);
        parts[5].SetParent(parts[3],true);parts[7].SetParent(parts[5],true);
        var study=root.gameObject.AddComponent<MasterJointStudy>();study.nearThigh=parts[2];study.farThigh=parts[3];study.nearShin=parts[4];study.farShin=parts[5];study.nearFoot=parts[6];study.farFoot=parts[7];
        study.partRenderers=new MeshRenderer[8];for(int i=0;i<8;i++)study.partRenderers[i]=parts[i].GetComponent<MeshRenderer>();
        var mat=new Material(Shader.Find("Sprites/Default"));mat.color=new Color(.11f,.12f,.14f);AssetDatabase.CreateAsset(mat,folder+"/TemporaryUnderlay.mat");
        for(int i=4;i<8;i++)
        {
            var node=new GameObject("Temporary joint underlay");node.layer=29;node.transform.SetParent(parts[i],false);
            var mesh=new Mesh();var v=new Vector3[25];v[0]=Vector3.zero;var triangles=new List<int>();
            for(int j=0;j<24;j++){float a=j*Mathf.PI*2/24;v[j+1]=new Vector3(Mathf.Cos(a)*.028f,Mathf.Sin(a)*.035f,0);triangles.AddRange(new[]{0,j+1,(j+1)%24+1});}
            mesh.vertices=v;mesh.triangles=triangles.ToArray();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,folder+"/Underlay"+i+".asset");node.AddComponent<MeshFilter>().sharedMesh=mesh;var r=node.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.sortingOrder=-1;
        }
        study.ApplyPose(0);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,"Assets/Scenes/OperatorMasterJointStudy.unity");
        return "Angled shared partitions, knee/ankle hierarchy and small rotation controls installed. Underlays are temporary geometry, not finished hidden artwork.";
    }
}
