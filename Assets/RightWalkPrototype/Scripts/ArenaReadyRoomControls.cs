using System.Collections.Generic;
using UnityEngine;

namespace RightWalkPrototype
{
    // World-space guide pages follow the server-selected topic; shot targets add no movement colliders.
    public sealed partial class ArenaReadyRoomControls : MonoBehaviour
    {
        Sprite sprite;Font font;Material lineMaterial;
        readonly List<Mesh> meshes=new();
        static readonly Color Ink=new(.9f,.96f,.98f),Muted=new(.55f,.7f,.75f),Accent=new(.28f,.86f,.8f);
        static readonly Color Steel=new(.25f,.39f,.44f),Face=new(.085f,.145f,.18f);
        public static void Build(Transform parent,Sprite sprite,Font font)
        {
            var go=new GameObject("Ready room controls wall");go.transform.SetParent(parent,false);
            go.transform.position=new Vector3(100,0,0);
            var board=go.AddComponent<ArenaReadyRoomControls>();board.sprite=sprite;board.font=font;
            board.lineMaterial=new Material(Shader.Find("Sprites/Default")){mainTexture=Texture2D.whiteTexture};board.Draw();
        }
        void Box(string name,float x,float y,float w,float h,Color color,int order=-74)
        {
            var go=new GameObject(name);go.transform.SetParent(contentRoot!=null?contentRoot:transform,false);go.transform.localPosition=new Vector3(x,y,0);go.transform.localScale=new Vector3(w,h,1);
            var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;
        }
        void Polygon(string name,Vector2[] points,Color color,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(contentRoot!=null?contentRoot:transform,false);
            var vertices=new Vector3[points.Length];var colors=new Color[points.Length];var triangles=new int[(points.Length-2)*3];
            var vertexColor=QualitySettings.activeColorSpace==ColorSpace.Linear?color.linear:color;
            for(int i=0;i<points.Length;i++){vertices[i]=points[i];colors[i]=vertexColor;}
            for(int i=0;i<points.Length-2;i++){triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=i+2;}
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();meshes.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=lineMaterial;renderer.sortingOrder=order;
        }
        void Panel(string name,float x,float y,float w,float h,float cut,Color color,int order)
        {
            float l=x-w*.5f,r=x+w*.5f,b=y-h*.5f,t=y+h*.5f;
            Polygon(name,new[]{new Vector2(l+cut,t),new Vector2(r-cut,t),new Vector2(r,t-cut),new Vector2(r,b+cut),
                new Vector2(r-cut,b),new Vector2(l+cut,b),new Vector2(l,b+cut),new Vector2(l,t-cut)},color,order);
        }
        void Stroke(string name,Vector2[] points,Color color,float width,int order,bool loop=false)
        {
            var go=new GameObject(name);go.transform.SetParent(contentRoot!=null?contentRoot:transform,false);var line=go.AddComponent<LineRenderer>();
            line.sharedMaterial=lineMaterial;line.useWorldSpace=false;line.loop=loop;line.positionCount=points.Length;
            line.startWidth=line.endWidth=width;line.startColor=line.endColor=color;line.sortingOrder=order;line.numCornerVertices=3;line.numCapVertices=3;
            for(int i=0;i<points.Length;i++)line.SetPosition(i,points[i]);
        }
        TextMesh Text(string value,float x,float y,float size,Color color,bool centred=false)
        {
            var go=new GameObject(value);go.transform.SetParent(contentRoot!=null?contentRoot:transform,false);go.transform.localPosition=new Vector3(x,y,0);
            var text=go.AddComponent<TextMesh>();text.font=font;text.text=value;text.fontSize=64;text.characterSize=size;text.color=color;
            text.anchor=centred?TextAnchor.MiddleCenter:TextAnchor.MiddleLeft;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=font.material;renderer.sortingOrder=-65;return text;
        }
        void Key(string name,float x,float y,float width=.64f)
        {
            Panel(name+" key shadow",x,y-.055f,width+.04f,.77f,.075f,new Color(.025f,.045f,.06f),-73);
            Panel(name+" key rim",x,y,width,.72f,.07f,Steel,-72);
            Panel(name+" key face",x,y+.035f,width-.065f,.61f,.045f,new Color(.135f,.22f,.26f),-71);
            Box(name+" top bevel",x,y+.303f,width-.19f,.018f,new Color(.43f,.6f,.64f),-70);
            Text(name,x,y+.04f,name.Length>2?.055f:.09f,Ink,true);
        }
        void Mouse(float x,float y,bool right)
        {
            var body=new[]{new Vector2(-.24f,.57f),new Vector2(.24f,.57f),new Vector2(.36f,.46f),new Vector2(.4f,.22f),
                new Vector2(.4f,-.3f),new Vector2(.3f,-.49f),new Vector2(0,-.57f),new Vector2(-.3f,-.49f),new Vector2(-.4f,-.3f),new Vector2(-.4f,.22f),new Vector2(-.36f,.46f)};
            for(int i=0;i<body.Length;i++)body[i]+=new Vector2(x,y);
            string name=right?"Right mouse":"Left mouse";
            Polygon(name+" housing",body,new Color(.1f,.17f,.20f),-73);
            Stroke(name+" outline",body,Muted,.023f,-70,true);
            float side=right?1:-1;
            Polygon(name+" active button",new[]{new Vector2(x+side*.045f,y+.49f),new Vector2(x+side*.22f,y+.49f),
                new Vector2(x+side*.30f,y+.41f),new Vector2(x+side*.34f,y+.20f),new Vector2(x+side*.34f,y+.045f),new Vector2(x+side*.045f,y+.045f)},Accent,-72);
            Stroke(name+" seam",new[]{new Vector2(x,y+.54f),new Vector2(x,y+.025f)},Muted,.017f,-69);
            Stroke(name+" button base",new[]{new Vector2(x-.37f,y+.015f),new Vector2(x+.37f,y+.015f)},Steel,.017f,-69);
            Panel(name+" wheel inset",x,y+.27f,.15f,.25f,.035f,new Color(.025f,.055f,.065f),-68);
            Box(name+" wheel",x,y+.27f,.055f,.15f,Ink,-67);
            Box(name+" palm highlight",x,y-.34f,.18f,.025f,Steel,-70);
        }
        void Card(float x,float y,string title,string detail,string extra=null)
        {
            Panel(title+" card edge",x,y,5,1.4f,.1f,new Color(.18f,.29f,.33f),-76);
            Panel(title+" card face",x,y,4.96f,1.36f,.09f,Face,-75);
            Box(title+" accent",x-2.44f,y,.025f,.68f,Accent,-74);
            Box(title+" divider",x-.57f,y,.014f,.98f,new Color(.18f,.29f,.33f),-73);
            if(extra==null){Text(title,x-.35f,y+.27f,.065f,Ink);Text(detail,x-.35f,y-.18f,.038f,Muted);}
            else
            {
                Text(title,x-.35f,y+.35f,.065f,Ink);Text(detail,x-.35f,y-.13f,.034f,Muted);
                Text(extra,x-.35f,y-.48f,.031f,Muted);
            }
        }
        void AmmoType(string key,string label,float x,float y,Color color)
        {
            Text(key,x,y+.01f,.035f,color,true);Text(label,x+.38f,y,.034f,Muted);
        }
        void Draw()
        {
            Panel("Control board shadow",0,5.99f,16.38f,5.49f,.2f,new Color(.02f,.035f,.045f),-80);
            Panel("Control board frame",0,6.05f,16.3f,5.45f,.18f,Steel,-79);
            Panel("Control board backing",0,6.05f,16.18f,5.33f,.14f,new Color(.04f,.075f,.10f),-78);
            Box("Header rule",0,8f,15.35f,.018f,new Color(.18f,.3f,.34f),-76);
            heading=Text("조작 가이드",-6.85f,8.4f,.075f,Ink);
            Text("천장 위 표적은 관통 라이플로만 전환됩니다",-3.95f,8.38f,.031f,Muted);
            for(int i=0;i<4;i++)BuildTopic(i);BuildTargets();
            Text("2 관통 라이플 선택 → 우클릭 사격 · 천장을 관통해 주제를 선택하세요",-7.1f,3.8f,.036f,Accent);
        }
        void DrawControls()
        {
            Card(-5.2f,7.25f,"좌우 이동","S / ↓ 앉기");Key("A",-7.15f,7.25f);Key("D",-6.4f,7.25f);
            Card(0,7.25f,"사격","일반탄 자동 충전");Mouse(-1.55f,7.25f,false);
            Card(5.2f,7.25f,"특수탄 사용","우클릭을 누르는 동안");Mouse(3.65f,7.25f,true);
            Card(-5.2f,5.75f,"달리기","스태미나 소모");Key("Shift",-6.78f,5.75f,1.5f);
            Card(0,5.75f,"점프","공중에서 한 번 더","S/↓ + Space · 점선 내려가기");Key("Space",-1.58f,5.75f,1.5f);
            Card(5.2f,5.75f,ArenaUiTheme.SpecialAmmoSwitchLabel,"휠 순환 · 1 / 2 / 3 선택","선택한 탄종 유지");Key("Q",3.25f,5.75f);Key("E",4,5.75f);

        }
        void SupportNotice()
        {
            Panel("Support drone notice rim",-4,3.8f,7.6f,.8f,.09f,Steel,-76);
            Panel("Support drone notice face",-4,3.8f,7.55f,.75f,.07f,Face,-75);
            var icon=new GameObject("Support drone illustration");icon.transform.SetParent(transform,false);
            // Alpha union of the two 512px sprites: x41..471, y33..270.
            // Fit and centre visible pixels rather than the transparent 2x2 sprite canvas.
            const float scale=.62f,visibleCentreY=.408203125f;
            icon.transform.localPosition=new Vector3(-7,3.8f-visibleCentreY*scale,0);icon.transform.localScale=Vector3.one*scale;
            ArenaEnemyArt.BuildSupport(icon.transform,new List<SpriteRenderer>(),new List<Color>());
            foreach(var r in icon.GetComponentsInChildren<SpriteRenderer>())r.sortingOrder=-66;
            Text("지원 드론 · 비무장 보급 기체",-6.25f,3.98f,.038f,Accent);
            Text("격추 +5점 · 보호막 투하 · 파란 방패 표시",-6.25f,3.62f,.034f,Muted);
        }
        void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);if(lineMaterial!=null)Destroy(lineMaterial);}
    }
}
