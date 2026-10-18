using UnityEngine;

namespace RightWalkPrototype
{
    // Original parts lead into the intact resting drawing without alpha overlays.
    internal sealed class NativeDeathRig
    {
        private readonly Transform upper,aim;
        private readonly Vector3 upperPosition;
        private readonly Quaternion upperRotation,aimRotation;
        private readonly float initialAim;
        private readonly RightFacingArmIK[] arms;
        private readonly Transform head,neck,restingHead;
        private readonly Vector3 headPosition;
        private readonly RightFacingNeckSkin neckSkin;
        private readonly SpriteRenderer[] headSprites;
        private readonly Color[] headColors;
        private readonly Mesh headMesh;
        private readonly Material headMaterial;
        private readonly JumpLegTransitionRig legs;
        private readonly Vector2 hipStart;
        private readonly RestingDeathPose resting;
        public bool Ready => legs!=null&&legs.Ready&&resting.Ready;
        public NativeDeathRig(Transform owner,Transform sourceUpper,SpriteRenderer source,int frame,bool running,bool airborne,bool alternate,Sprite referenceHead,Sprite looseWeapon)
        {
            var clone=Object.Instantiate(sourceUpper.gameObject,owner,false);clone.name="Death native upper body";clone.hideFlags=HideFlags.DontSave;
            upper=clone.transform;upperPosition=upper.localPosition;upperRotation=upper.localRotation;
            var aimComponent=clone.GetComponentInChildren<RightFacingAimRig>(true);aim=aimComponent.transform;aimRotation=aim.localRotation;
            var direction=owner.InverseTransformVector(aim.TransformVector(Vector3.right));initialAim=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
            arms=clone.GetComponentsInChildren<RightFacingArmIK>(true);
            foreach(var b in clone.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
            foreach(var r in clone.GetComponentsInChildren<Renderer>(true)){r.forceRenderingOff=false;r.enabled=true;}
            head=aimComponent.head;neck=aimComponent.neck;headPosition=head.localPosition;neckSkin=clone.GetComponentInChildren<RightFacingNeckSkin>(true);headSprites=head.GetComponentsInChildren<SpriteRenderer>(true);headColors=new Color[headSprites.Length];
            for(int i=0;i<headSprites.Length;i++)headColors[i]=headSprites[i].color;
            if(referenceHead!=null)
            {
                // Only the closed-eye head/hair region of the already reviewed drawing.
                // Body, limbs, rifle and their proportions remain the original rig.
                var points=new[]{new Vector2(5,520),new Vector2(155,478),new Vector2(175,375),new Vector2(280,322),
                    new Vector2(430,324),new Vector2(515,350),new Vector2(550,390),new Vector2(554,421),new Vector2(554,432),
                    new Vector2(560,443),new Vector2(565,458),new Vector2(573,480),new Vector2(580,495),new Vector2(583,509),
                    new Vector2(578,520),new Vector2(568,532),new Vector2(554,543),new Vector2(538,551),new Vector2(522,555),
                    new Vector2(521,568),new Vector2(520,583),new Vector2(522,594),new Vector2(512,595),new Vector2(500,599),
                    new Vector2(487,603),new Vector2(475,609),new Vector2(462,613),new Vector2(447,615),new Vector2(430,616),
                    new Vector2(410,617),new Vector2(391,619),new Vector2(375,624),new Vector2(359,630),new Vector2(344,637),
                    new Vector2(328,640),new Vector2(311,641),new Vector2(306,652),new Vector2(293,660),new Vector2(276,668),
                    new Vector2(258,679),new Vector2(220,691),new Vector2(170,703),new Vector2(70,710),new Vector2(10,700)};
                var vertices=new Vector3[points.Length];var uv=new Vector2[points.Length];
                for(int i=0;i<points.Length;i++)
                {
                    vertices[i]=new Vector3((points[i].x-570f)/650f,(550f-points[i].y)/650f,0);
                    uv[i]=new Vector2(points[i].x/referenceHead.texture.width,1-points[i].y/referenceHead.texture.height);
                }
                headMesh=new Mesh{name="Resting head region",hideFlags=HideFlags.DontSave};headMesh.vertices=vertices;headMesh.uv=uv;headMesh.triangles=Triangulate(vertices);headMesh.RecalculateBounds();
                headMaterial=new Material(Shader.Find("Sprites/Default")){name="Resting head pixels",hideFlags=HideFlags.DontSave,mainTexture=referenceHead.texture};headMaterial.color=new Color(1,1,1,0);
                var go=new GameObject("Death resting head");go.hideFlags=HideFlags.DontSave;restingHead=go.transform;restingHead.SetParent(owner,false);
                go.AddComponent<MeshFilter>().sharedMesh=headMesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=headMaterial;renderer.sortingOrder=24;
            }
            legs=new JumpLegTransitionRig(owner,source);legs.PrepareFall(frame,running,airborne,alternate);hipStart=legs.FallHip;
            SpriteRenderer weapon=null;foreach(var r in clone.GetComponentsInChildren<SpriteRenderer>(true))if(r.name=="Weapon and Gripping Hands")weapon=r;
            resting=new RestingDeathPose(owner,referenceHead,looseWeapon,weapon,restingHead);
        }
        public void Pose(float progress,float recoilAngle,float floor,bool onFloor)
        {
            resting.RestoreNative();
            var hip=legs.DrawFall(progress,floor,onFloor);float t=Mathf.SmoothStep(0,1,progress);
            float angle=Mathf.Lerp(recoilAngle,100f,t);var turn=Quaternion.Euler(0,0,angle);
            upper.localRotation=turn*upperRotation;
            upper.localPosition=(Vector3)hip-turn*((Vector3)hipStart-upperPosition);
            aim.localRotation=aimRotation*Quaternion.Euler(0,0,-angle+(15f-initialAim)*t);
            foreach(var arm in arms)arm.Solve();
            if(restingHead!=null)
            {
                // Use one opaque head at a time instead of superimposed hair/face silhouettes.
                float blend=progress>=.34f?1:0;
                head.localPosition=headPosition;
                restingHead.localRotation=Quaternion.Euler(0,0,angle-100f);
                // Register the drawing's neck attachment, not the skull pivot.
                var neckPoint=restingHead.parent.InverseTransformPoint(neck.position)+new Vector3(.025f,.035f,0);
                var drawingNeck=new Vector3((558f-570f)/650f,(550f-435f)/650f,0);
                restingHead.localPosition=neckPoint-restingHead.localRotation*drawingNeck;
                headMaterial.color=new Color(1,1,1,blend);
                float floorY=restingHead.parent.TransformPoint(new Vector3(0,floor,0)).y;
                // Keep the neck anchored when clearing hair from the floor; never
                // translate the skull independently away from the collar.
                for(int i=0;i<25&&onFloor;i++)
                {
                    float minY=float.PositiveInfinity;foreach(var v in headMesh.vertices)minY=Mathf.Min(minY,restingHead.TransformPoint(v).y);
                    if(minY>=floorY)break;
                    restingHead.localRotation*=Quaternion.Euler(0,0,-1f);
                    restingHead.localPosition=neckPoint-restingHead.localRotation*drawingNeck;
                }
                if(onFloor)resting.MatchHeadToLanding(progress,floor,neckPoint,drawingNeck);
                if(neckSkin!=null)
                {
                    var nativeBack=head.TransformPoint(new Vector3(-7f/204.8f,8f/204.8f,0));
                    var nativeFront=head.TransformPoint(new Vector3(19f/204.8f,-5f/204.8f,0));
                    var drawingBack=restingHead.TransformPoint(new Vector3((546f-570f)/650f,(550f-423f)/650f,0));
                    var drawingFront=restingHead.TransformPoint(new Vector3((557f-570f)/650f,(550f-445f)/650f,0));
                    neckSkin.RebuildWithAttachment(Vector3.Lerp(nativeBack,drawingBack,blend),Vector3.Lerp(nativeFront,drawingFront,blend));
                }
                for(int i=0;i<headSprites.Length;i++){var c=headColors[i];c.a*=1-blend;headSprites[i].color=c;}
            }
            resting.Apply(progress,floor,onFloor);
        }
        private static float Cross(Vector3 a,Vector3 b,Vector3 c){return (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);}
        private static int[] Triangulate(Vector3[] points)
        {
            var remaining=new System.Collections.Generic.List<int>();var result=new System.Collections.Generic.List<int>();float area=0;
            for(int i=0;i<points.Length;i++){remaining.Add(i);var b=points[(i+1)%points.Length];area+=points[i].x*b.y-b.x*points[i].y;}
            if(area<0)remaining.Reverse();
            while(remaining.Count>2)
            {
                bool found=false;
                for(int i=0;i<remaining.Count;i++)
                {
                    int a=remaining[(i+remaining.Count-1)%remaining.Count],b=remaining[i],c=remaining[(i+1)%remaining.Count];
                    if(Cross(points[a],points[b],points[c])<=.000001f)continue;bool occupied=false;
                    foreach(int p in remaining)if(p!=a&&p!=b&&p!=c&&Cross(points[a],points[b],points[p])>=0&&Cross(points[b],points[c],points[p])>=0&&Cross(points[c],points[a],points[p])>=0){occupied=true;break;}
                    if(occupied)continue;result.Add(a);result.Add(b);result.Add(c);remaining.RemoveAt(i);found=true;break;
                }
                if(!found)break;
            }
            return result.ToArray();
        }
        public void Dispose(){if(legs!=null)legs.Dispose();if(headMesh!=null)Object.Destroy(headMesh);if(headMaterial!=null)Object.Destroy(headMaterial);if(resting!=null)resting.Dispose();}
    }
}
