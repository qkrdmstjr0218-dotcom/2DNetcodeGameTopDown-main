using UnityEngine;
namespace RightWalkPrototype
{
    [ExecuteAlways]
    public sealed class RightFacingNeckSkin : MonoBehaviour
    {
        public Transform head;
        public Material material;
        private Mesh mesh;
        private const int Rows=24, Columns=9;
        private Vector3 NeckPoint(float x,float y) { return new Vector3((x-252f)/204.8f,(145f-y)/204.8f,0); }
        private Vector3 HeadPoint(float x,float y)
        {
            return transform.InverseTransformPoint(head.TransformPoint(new Vector3((x-249f)/204.8f,(132f-y)/204.8f,0)));
        }
        private void OnEnable() { Rebuild(); }
        public void Rebuild()
        {
            if(head==null || material==null)return;
            var filter=GetComponent<MeshFilter>();if(filter==null)filter=gameObject.AddComponent<MeshFilter>();
            var renderer=GetComponent<MeshRenderer>();if(renderer==null)renderer=gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material;renderer.sortingOrder=9;
            if(mesh==null){mesh=new Mesh();mesh.name="Continuous neck skin";mesh.hideFlags=HideFlags.DontSave;}
            var vertices=new Vector3[(Rows+1)*Columns];var colors=new Color[vertices.Length];var uv=new Vector2[vertices.Length];
            var triangles=new int[Rows*(Columns-1)*6];
            var backBase=NeckPoint(240,145);var frontBase=NeckPoint(261,151);
            var backTop=HeadPoint(242,124);var frontTop=HeadPoint(268,137);
            for(int row=0;row<=Rows;row++)
            {
                float t=(float)row/Rows;
                float inv=1f-t;
                var backControl=backBase+new Vector3(4.5f,5f,0)/204.8f;
                var topControl=backTop-transform.InverseTransformVector(head.TransformVector(Vector3.up)).normalized*(5f/204.8f)+Vector3.right*(3.5f/204.8f);
                var back=inv*inv*inv*backBase+3f*inv*inv*t*backControl+3f*inv*t*t*topControl+t*t*t*backTop;
                var front=Vector3.Lerp(frontBase,frontTop,t);
                // Keep the collar attachment fixed while only the upper edge follows the skull.
                front.x-=Mathf.Sin(t*Mathf.PI)*.8f/204.8f;
                var across=(front-back).normalized;
                for(int col=0;col<Columns;col++)
                {
                    float width=(front-back).magnitude*204.8f;
                    float u=col<=1?0:col==2?Mathf.Min(1.2f/width,.15f):col==3?.45f:col==4?.8f:col==5?Mathf.Max(.8f,1f-1.1f/width):col==6?Mathf.Max(.8f,1f-.45f/width):1f;
                    int index=row*Columns+col;
                    vertices[index]=Vector3.Lerp(back,front,u);
                    if(col==0)vertices[index]-=across*.3f/204.8f;
                    if(col==Columns-1)vertices[index]+=across*.45f/204.8f;
                    var light=new Color(1f,.81f,.69f,1f);var shade=new Color(.94f,.60f,.49f,1f);
                    colors[index]=Color.Lerp(light,shade,Mathf.SmoothStep(0,1,u));
                    var edgeColor=Color.Lerp(new Color(.87f,.60f,.49f,1f),new Color(.81f,.54f,.43f,1f),Mathf.SmoothStep(0f,1f,t));
                    if(col==2)colors[index]=Color.Lerp(edgeColor,light,.7f);
                    if(col==1)colors[index]=edgeColor;
                    if(col==0){colors[index]=edgeColor;colors[index].a=0;}
                    // Soft under-jaw shade, confined to the front half of the skin.
                    float jawShade=Mathf.SmoothStep(0f,1f,Mathf.Clamp01((t-.3f)/.55f))*Mathf.SmoothStep(0f,1f,Mathf.Clamp01((u-.25f)/.65f));
                    if(col>=3)colors[index]=Color.Lerp(colors[index],new Color(.83f,.52f,.42f,1f),jawShade*.18f);
                    var frontEdge=new Color(.82f,.51f,.41f,1f);
                    if(col==6)colors[index]=Color.Lerp(colors[index],frontEdge,.35f);
                    if(col==7)colors[index]=frontEdge;
                    if(col==Columns-1){colors[index]=frontEdge;colors[index].a=0;}
                    uv[index]=Vector2.one*.5f;
                }
            }
            int k=0;
            for(int row=0;row<Rows;row++)for(int col=0;col<Columns-1;col++)
            {
                int a=row*Columns+col,b=a+1,c=a+Columns,d=c+1;
                triangles[k++]=a;triangles[k++]=c;triangles[k++]=b;
                triangles[k++]=b;triangles[k++]=c;triangles[k++]=d;
            }
            mesh.Clear();mesh.vertices=vertices;mesh.colors=colors;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateBounds();filter.sharedMesh=mesh;
        }
        private void OnDestroy()
        {
            if(mesh!=null){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}
        }
    }
}




