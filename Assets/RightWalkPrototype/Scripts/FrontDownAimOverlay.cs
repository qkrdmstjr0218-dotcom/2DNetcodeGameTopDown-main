using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RightWalkPrototype
{
    [DisallowMultipleComponent]
    public sealed class FrontDownAimOverlay : MonoBehaviour
    {
        public TextAsset poseData;
        public Material material;
        [Min(1)] public float pixelsPerUnit=540.3778f;
        public float soleHeight=.152f;
        public bool standingTrialOnly=true;
        public bool IsActive { get; private set; }
        public bool TargetWithinRange { get; private set; }
        public float LocalAimDegrees { get; private set; }
        public Vector3 MuzzlePosition => transform.TransformPoint(ToLocal(pose.muzzle));
        public Vector2 AimDirection => transform.TransformVector(new Vector3(pose.direction.x,-pose.direction.y,0)).normalized;
        public FrontDownAimMath.Data Data => data;
        private FrontDownAimMath.Data data;
        private FrontDownAimMath.Pose pose;
        private readonly Dictionary<string,Part> parts=new Dictionary<string,Part>();
        private readonly Dictionary<Renderer,bool> originalVisibility=new Dictionary<Renderer,bool>();
        private float headAmount;
        private bool initialized;
        private Collider2D bodyCollider;
        private sealed class Part
        {
            public FrontDownAimMath.Layer layer;
            public Mesh mesh;
            public MeshRenderer renderer;
            public readonly Vector3[] vertices=new Vector3[4];
        }
        public void Initialize()
        {
            if(initialized)return;
            if(poseData==null||material==null)throw new InvalidOperationException("Front down-aim art is not assigned.");
            data=JsonUtility.FromJson<FrontDownAimMath.Data>(poseData.text);
            if(data.version!=1||data.arms.Length!=2)throw new InvalidOperationException("Unsupported front down-aim data.");
            pose=new FrontDownAimMath.Pose(data.arms.Length);
            bodyCollider=GetComponent<Collider2D>();
            foreach(var renderer in GetComponentsInChildren<Renderer>(true))originalVisibility[renderer]=renderer.enabled;
            foreach(var layer in data.layers)
            {
                var go=new GameObject("Front Down "+layer.id);go.transform.SetParent(transform,false);
                go.hideFlags=HideFlags.DontSave;go.layer=gameObject.layer;
                var mesh=new Mesh {name=layer.id+" affine quad",hideFlags=HideFlags.DontSave};mesh.MarkDynamic();
                var uv=new Vector2[4];float x=layer.atlas[0]/data.atlasSize[0],y=1-layer.atlas[1]/data.atlasSize[1];
                float w=layer.atlas[2]/data.atlasSize[0],h=layer.atlas[3]/data.atlasSize[1];
                uv[0]=new Vector2(x,y);uv[1]=new Vector2(x+w,y);uv[2]=new Vector2(x+w,y-h);uv[3]=new Vector2(x,y-h);
                mesh.vertices=new Vector3[4];mesh.uv=uv;mesh.colors=new[]{Color.white,Color.white,Color.white,Color.white};mesh.triangles=new[]{0,1,2,0,2,3};
                go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                renderer.sortingOrder=Order(layer.id);renderer.enabled=false;
                parts.Add(layer.id,new Part {layer=layer,mesh=mesh,renderer=renderer});
            }
            initialized=true;
        }
        private static int Order(string id)
        {
            switch(id){case "body":return 10;case "trigger-fore":return 20;case "trigger-upper":return 21;
                case "support-fore":return 22;case "support-upper":return 23;case "head":return 24;
                case "trigger-hand":return 25;case "support-hand":return 26;default:return 27;}
        }
        private Vector3 ToLocal(Vector2 p) => new Vector3((p.x-data.origin[0])/pixelsPerUnit,(data.origin[1]-p.y)/pixelsPerUnit+soleHeight,0);
        public bool TryAim(Vector2 worldTarget)
        {
            if(!isActiveAndEnabled||poseData==null||material==null){Deactivate();return false;}
            if(standingTrialOnly)
            {
                var walker=GetComponent<RightFacingWalker>();var body=GetComponent<Rigidbody2D>();
                if((walker!=null&&Mathf.Abs(walker.Velocity.x)>.03f)||(body!=null&&Mathf.Abs(body.linearVelocity.x)>.03f))
                {Deactivate();return false;}
            }
            Initialize();var local=transform.InverseTransformPoint(worldTarget);
            var target=new Vector2(local.x*pixelsPerUnit+data.origin[0],data.origin[1]-(local.y-soleHeight)*pixelsPerUnit);
            float requested=FrontDownAimMath.RequestedAngle(data,target),limit=data.headAim.gunDegrees*Mathf.Deg2Rad;
            // A cursor inside the torso may sit behind the muzzle. Keep the pose without bending the bullet path backwards.
            bool insideBody=target.y>=data.headAim.pivot[1]&&bodyCollider!=null&&bodyCollider.OverlapPoint(worldTarget);
            if(!insideBody&&(requested< -limit-.0001f||requested>.0001f)){Deactivate();return false;}
            ShowAngle(Mathf.Clamp(requested,-limit,0),false);TargetWithinRange=true;return true;
        }
        public void ShowAngle(float radians,bool immediateHead=true)
        {
            Initialize();FrontDownAimMath.Evaluate(data,radians,pose);
            LocalAimDegrees=-90-radians*Mathf.Rad2Deg;
            float target=Mathf.Clamp01(-radians/(data.headAim.gunDegrees*Mathf.Deg2Rad));
            headAmount=immediateHead?target:Mathf.Lerp(headAmount,target,1-Mathf.Exp(-data.headAim.response*Time.deltaTime));
            bool wasActive=IsActive;SetVisible(true);
            if(!wasActive)UpdatePart("body",FrontDownAimMath.Affine.Identity);
            UpdatePart("weapon",pose.weapon);
            var h=FrontDownAimMath.Rotation(FrontDownAimMath.V(data.headAim.pivot),FrontDownAimMath.V(data.headAim.offset)*headAmount,-data.headAim.maxDegrees*Mathf.Deg2Rad*headAmount);
            UpdatePart("head",h);
            for(int i=0;i<data.arms.Length;i++)
            {
                string side=data.arms[i].side;UpdatePart(side+"-upper",pose.arms[i].upper);
                UpdatePart(side+"-fore",pose.arms[i].fore);UpdatePart(side+"-hand",pose.arms[i].hand);
            }
        }
        private void UpdatePart(string id,FrontDownAimMath.Affine matrix)
        {
            var part=parts[id];var b=part.layer.bounds;
            part.vertices[0]=ToLocal(matrix.Apply(new Vector2(b[0],b[1])));part.vertices[1]=ToLocal(matrix.Apply(new Vector2(b[2],b[1])));
            part.vertices[2]=ToLocal(matrix.Apply(new Vector2(b[2],b[3])));part.vertices[3]=ToLocal(matrix.Apply(new Vector2(b[0],b[3])));
            part.mesh.vertices=part.vertices;part.mesh.RecalculateBounds();
        }
        private void SetVisible(bool visible)
        {
            if(IsActive==visible)return;IsActive=visible;
            foreach(var pair in originalVisibility)if(pair.Key!=null)pair.Key.enabled=!visible&&pair.Value;
            foreach(var part in parts.Values)part.renderer.enabled=visible;
        }
        public void Deactivate(){SetVisible(false);headAmount=0;TargetWithinRange=false;}
        private void OnDisable(){Deactivate();}
        private void OnDestroy()
        {
            Deactivate();foreach(var part in parts.Values){Release(part.mesh);if(part.renderer!=null)Release(part.renderer.gameObject);}
            parts.Clear();
        }
        private static void Release(UnityEngine.Object value)
        { if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value); }
    }
}
