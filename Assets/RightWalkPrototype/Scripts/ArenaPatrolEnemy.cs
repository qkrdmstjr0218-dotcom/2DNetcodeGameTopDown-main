using UnityEngine;
namespace RightWalkPrototype
{
    // Local shot collider/presentation; health, motion clock and damage belong to the match server.
    public sealed class ArenaPatrolEnemy:MonoBehaviour
    {
        public ArenaObjectiveMatch Match {get;private set;}
        public int Id {get;private set;}
        public int Generation {get;private set;}
        BoxCollider2D shape;int previousHealth;float flash;Sprite sprite;
        readonly System.Collections.Generic.List<SpriteRenderer> bodyParts=new();
        readonly System.Collections.Generic.List<Color> bodyColors=new();
        public void Initialize(ArenaObjectiveMatch match,int id,Sprite sprite)
        {
            Match=match;Id=id;this.sprite=sprite;shape=gameObject.AddComponent<BoxCollider2D>();shape.size=new Vector2(.85f,.9f);shape.offset=new Vector2(0,.48f);
            SpriteRenderer Box(string name,Vector2 at,Vector2 size,Color color)
            {var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.localPosition=at;go.transform.localScale=size;var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=color;sr.sortingOrder=10;bodyParts.Add(sr);bodyColors.Add(color);return sr;}
            Box("Hull",new Vector2(0,.54f),new Vector2(.85f,.65f),new Color(.55f,.23f,.17f));
            Box("Track",new Vector2(0,.14f),new Vector2(.98f,.22f),new Color(.12f,.17f,.19f));
            for(int i=-1;i<=1;i++)Box("Wheel",new Vector2(i*.3f,.14f),new Vector2(.15f,.14f),new Color(.43f,.49f,.49f));
            Box("Red sensor",new Vector2(0,.65f),new Vector2(.5f,.12f),new Color(1,.25f,.15f));
            Box("Warning",new Vector2(0,.36f),new Vector2(.55f,.06f),new Color(1,.75f,.25f));
        }
        public void Apply(ArenaObjectiveMatch.PatrolState state,bool visible,int maximum)
        {
            Generation=state.Generation;
            bool hit=state.Health<previousHealth&&previousHealth>0;
            if(hit)flash=.1f;
            if(hit&&state.Health==0&&visible)
            {
                var effect=new GameObject("Patrol destruction");effect.transform.SetParent(Match.transform,false);
                effect.transform.position=transform.position+Vector3.up*.48f;
                effect.AddComponent<ArenaPatrolBurst>().Initialize(Match,sprite);
            }
            if(state.Health>previousHealth)flash=0;
            previousHealth=state.Health;
            gameObject.SetActive(visible&&state.Health>0);shape.enabled=visible&&state.Health>0;
            if(!gameObject.activeSelf)return;
            transform.position=Match.PatrolPosition(state);
            flash=Mathf.Max(0,flash-Time.deltaTime);
            for(int i=0;i<bodyParts.Count;i++)bodyParts[i].color=flash>0?new Color(1,.95f,.8f):bodyColors[i];
            // Players pass through the shot collider; contact damage is decided by the server.
            foreach(var player in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))
                if(player.IsSpawned&&player.IsOwner&&player.TryGetComponent<Collider2D>(out var body))Physics2D.IgnoreCollision(shape,body,true);
        }
        void OnGUI()
        {
            if(Match==null||!Match.IsSpawned||!gameObject.activeInHierarchy||ArenaObjectiveMatch.TransitionActive||Camera.main==null)return;
            var p=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*1.12f);if(p.z<=0)return;
            float fraction=Match.PatrolHealthFraction(Id);var rect=new Rect(p.x-22,Screen.height-p.y,44,4);var color=GUI.color;
            GUI.color=new Color(.06f,.07f,.08f,.9f);GUI.DrawTexture(rect,Texture2D.whiteTexture);rect.width*=fraction;GUI.color=new Color(1,.45f,.25f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=color;
        }
    }
}
