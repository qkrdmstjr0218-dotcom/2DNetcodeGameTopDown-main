using UnityEngine;

namespace RightWalkPrototype
{
    // World-space wall art, with no colliders or input handling.
    public sealed class ArenaReadyRoomControls : MonoBehaviour
    {
        Sprite sprite;Font font;Material lineMaterial;
        static readonly Color Ink=new(.9f,.96f,.98f),Muted=new(.55f,.7f,.75f),Accent=new(.28f,.86f,.8f);
        public static void Build(Transform parent,Sprite sprite,Font font)
        {
            var go=new GameObject("Ready room controls wall");go.transform.SetParent(parent,false);
            go.transform.position=new Vector3(100,0,0);
            var board=go.AddComponent<ArenaReadyRoomControls>();board.sprite=sprite;board.font=font;
            board.lineMaterial=new Material(Shader.Find("Sprites/Default"));board.Draw();
        }
        void Box(string name,float x,float y,float w,float h,Color color,int order=-74)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(x,y,0);go.transform.localScale=new Vector3(w,h,1);
            var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;
        }
        void Text(string value,float x,float y,float size,Color color,bool centred=false)
        {
            var go=new GameObject(value);go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(x,y,0);
            var t=go.AddComponent<TextMesh>();t.font=font;t.text=value;t.fontSize=64;t.characterSize=size;t.color=color;
            t.anchor=centred?TextAnchor.MiddleCenter:TextAnchor.MiddleLeft;
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=font.material;r.sortingOrder=-70;
        }
        void Key(string name,float x,float y,float width=.65f)
        {
            Box(name+" key rim",x,y,width,.72f,Muted,-73);
            Box(name+" key face",x,y+.04f,width-.08f,.55f,new Color(.14f,.23f,.29f),-72);
            Text(name,x,y+.04f,name.Length>2?.075f:.115f,Ink,true);
        }
        void Mouse(float x,float y,bool right)
        {
            var go=new GameObject(right?"Right mouse button diagram":"Left mouse button diagram");go.transform.SetParent(transform,false);
            var line=go.AddComponent<LineRenderer>();line.sharedMaterial=lineMaterial;line.useWorldSpace=false;line.loop=true;line.positionCount=24;
            line.startWidth=line.endWidth=.035f;line.startColor=line.endColor=Muted;line.sortingOrder=-71;
            for(int i=0;i<24;i++){float a=i*Mathf.PI*2/24;line.SetPosition(i,new Vector3(x+Mathf.Cos(a)*.43f,y+Mathf.Sin(a)*.63f,0));}
            Box("Active mouse button",x+(right?.21f:-.21f),y+.22f,.33f,.43f,Accent,-72);
            Box("Mouse button seam",x,y+.27f,.025f,.57f,Muted,-71);
            Box("Mouse wheel",x,y+.24f,.09f,.22f,Ink,-70);
        }
        void Card(float x,float y,string title,string detail)
        {
            Box(title+" card",x,y,5,1.7f,new Color(.075f,.13f,.17f));
            Text(title,x-.32f,y+.38f,.075f,Ink);Text(detail,x-.32f,y-.08f,.045f,Muted);
        }
        void Draw()
        {
            Box("Control board frame",0,6.05f,16.3f,5.45f,new Color(.19f,.31f,.36f),-77);
            Box("Control board backing",0,6.05f,16.16f,5.31f,new Color(.04f,.075f,.10f),-76);
            Text("키보드와 마우스 · 바로 연습해 보세요",0,8.25f,.075f,Ink,true);
            Card(-5.2f,6.85f,"좌우 이동","S / ↓ 앉기");Key("A",-7.15f,6.85f);Key("D",-6.39f,6.85f);
            Card(0,6.85f,"사격","일반탄 자동충전");Mouse(-1.65f,6.85f,false);
            Card(5.2f,6.85f,"특수탄 사용","우클릭을 누르는 동안");Mouse(3.55f,6.85f,true);
            Card(-5.2f,4.85f,"달리기","스태미나 소모");Key("Shift",-6.85f,4.85f,1.55f);
            Card(0,4.85f,"점프","공중에서 한 번 더");Key("Space",-1.65f,4.85f,1.55f);
            Card(5.2f,4.85f,"특수탄 교체","선택은 유지됩니다");Key("Q",3.25f,4.85f);Key("E",4.01f,4.85f);
            Text("S 샷건 · R 관통 라이플 · G 유탄    |    실제 경기: 보급 / 적 드롭으로 특수탄 수급",0,3.65f,.045f,Accent,true);
        }
        void OnDestroy(){if(lineMaterial!=null)Destroy(lineMaterial);}
    }
}
