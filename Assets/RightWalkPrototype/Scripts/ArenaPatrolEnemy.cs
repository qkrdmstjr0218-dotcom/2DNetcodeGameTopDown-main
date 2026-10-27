using UnityEngine;
namespace RightWalkPrototype
{
    // Local shot collider/presentation; health, motion clock and damage belong to the match server.
    public sealed class ArenaPatrolEnemy:MonoBehaviour
    {
        public const float GroundHitWidth=1.1f;
        public ArenaObjectiveMatch Match {get;private set;}
        public int Id {get;private set;}
        public int Generation {get;private set;}
        BoxCollider2D shape;int previousHealth;bool medical,artFlash,entryActive;float flash;Sprite sprite;
        Transform barrel,bodyRoot,entryMarker,artFacing;SpriteRenderer sensor,rushMarker;
        readonly System.Collections.Generic.List<SpriteRenderer> entryLights=new();
        readonly System.Collections.Generic.List<SpriteRenderer> bodyParts=new();
        readonly System.Collections.Generic.List<Color> bodyColors=new();
        public void Initialize(ArenaObjectiveMatch match,int id,Sprite sprite,bool turret=false,bool flying=false,bool medical=false)
        {
            Match=match;Id=id;this.sprite=sprite;this.medical=medical;
            bodyRoot=new GameObject("Enemy body").transform;bodyRoot.SetParent(transform,false);
            if(!medical)CreateEntryMarker(sprite,turret,flying);
            shape=gameObject.AddComponent<BoxCollider2D>();shape.size=new Vector2(!turret&&!flying?GroundHitWidth:.85f,.9f);shape.offset=new Vector2(0,.48f);
            if(medical&&ArenaEnemyArt.BuildSupport(bodyRoot,bodyParts,bodyColors))return;
            if(!medical&&ArenaEnemyArt.Build(bodyRoot,turret,flying,bodyParts,bodyColors,out barrel,out sensor,out rushMarker,out artFacing))return;
            SpriteRenderer Box(string name,Vector2 at,Vector2 size,Color color)
            {var go=new GameObject(name);go.transform.SetParent(bodyRoot,false);go.transform.localPosition=at;go.transform.localScale=size;var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=color;sr.sortingOrder=10+bodyParts.Count;bodyParts.Add(sr);bodyColors.Add(color);return sr;}
            if(flying)
            {
                Box("Flight hull",new Vector2(0,.52f),new Vector2(.75f,.4f),medical?new Color(.85f,.95f,.9f):new Color(.25f,.43f,.55f));
                Box("Flight arms",new Vector2(0,.65f),new Vector2(1.35f,.1f),new Color(.15f,.25f,.31f));
                for(int side=-1;side<=1;side+=2)
                {
                    Box("Rotor guard",new Vector2(side*.55f,.72f),new Vector2(.38f,.18f),new Color(.4f,.57f,.65f));
                    Box("Hover light",new Vector2(side*.55f,.53f),new Vector2(.22f,.08f),medical?new Color(.3f,1,.5f):new Color(.35f,.95f,1));
                }
                if(medical)
                {
                    Box("Medical kit",new Vector2(0,.23f),new Vector2(.55f,.4f),new Color(.9f,1,.94f));
                    Box("Shield plate",new Vector2(0,.23f),new Vector2(.27f,.3f),new Color(.25f,.5f,1));
                    Box("Shield inset",new Vector2(0,.25f),new Vector2(.15f,.18f),new Color(.85f,.95f,1));
                    return;
                }
                var pivot=new GameObject("Aimed barrel");barrel=pivot.transform;barrel.SetParent(bodyRoot,false);barrel.localPosition=Vector2.up*.68f;
                var tube=Box("Drone barrel",new Vector2(.28f,.68f),new Vector2(.55f,.12f),new Color(.15f,.2f,.25f));tube.transform.SetParent(barrel,true);
                sensor=Box("Drone sensor",new Vector2(0,.68f),new Vector2(.19f,.19f),new Color(1,.35f,.12f));
                return;
            }
            if(turret)
            {
                Box("Anchored base",new Vector2(0,.12f),new Vector2(1.15f,.24f),new Color(.15f,.2f,.24f));
                Box("Armour",new Vector2(0,.43f),new Vector2(.75f,.55f),new Color(.4f,.46f,.5f));
                Box("Hazard stripe",new Vector2(0,.35f),new Vector2(.7f,.07f),new Color(1,.7f,.2f));
                var pivot=new GameObject("Aimed barrel");barrel=pivot.transform;barrel.SetParent(bodyRoot,false);barrel.localPosition=Vector2.up*.68f;
                var tube=Box("Barrel",new Vector2(.32f,.68f),new Vector2(.7f,.17f),new Color(.23f,.28f,.32f));tube.transform.SetParent(barrel,true);
                sensor=Box("Charging sensor",new Vector2(0,.68f),new Vector2(.23f,.23f),new Color(.6f,.2f,.12f));
                return;
            }
            Box("Hull",new Vector2(0,.54f),new Vector2(.85f,.65f),new Color(.55f,.23f,.17f));
            Box("Track",new Vector2(0,.14f),new Vector2(.98f,.22f),new Color(.12f,.17f,.19f));
            for(int i=-1;i<=1;i++)Box("Wheel",new Vector2(i*.3f,.14f),new Vector2(.15f,.14f),new Color(.43f,.49f,.49f));
            sensor=Box("Red sensor",new Vector2(0,.65f),new Vector2(.5f,.12f),new Color(1,.25f,.15f));
            rushMarker=Box("Bumper charge",new Vector2(.38f,.5f),new Vector2(.08f,.32f),new Color(1,.8f,.2f));rushMarker.gameObject.SetActive(false);
            Box("Warning",new Vector2(0,.36f),new Vector2(.55f,.06f),new Color(1,.75f,.25f));
        }
        public void Apply(ArenaObjectiveMatch.PatrolState state,bool visible,int maximum)
        {
            Generation=state.Generation;
            bool entering=Match.EnemyEntering(state);entryActive=entering;
            bool hit=state.Health<previousHealth&&previousHealth>0;
            if(hit)flash=.1f;
            if(hit&&visible)ArenaAudio.World(state.Health==0?ArenaAudio.Cue.EnemyBreak:ArenaAudio.Cue.Impact,transform.position,GetInstanceID());
            if(hit&&state.Health==0&&visible)
            {
                var effect=new GameObject("Patrol destruction");effect.transform.SetParent(Match.transform,false);
                effect.transform.position=transform.position+Vector3.up*.48f;
                effect.AddComponent<ArenaPatrolBurst>().Initialize(Match,sprite);
            }
            if(state.Health>previousHealth)flash=0;
            previousHealth=state.Health;
            gameObject.SetActive(visible&&state.Health>0);shape.enabled=visible&&state.Health>0&&!entering&&(!medical||Match.MedicalInsideArena(state.Position));
            if(!gameObject.activeSelf)return;
            var target=Match.PatrolPosition(state);
            transform.position=!entering&&state.Roaming&&Vector3.Distance(transform.position,target)<2?Vector3.MoveTowards(transform.position,target,Mathf.Max(1.2f,state.Speed)*1.5f*Time.deltaTime):target;
            transform.rotation=Quaternion.Euler(0,0,state.Roaming&&!state.Flying?state.Angle:0);
            if(artFacing!=null)
            {
                // Ground navigation does not update weapon Aim during ordinary
                // pursuit. Follow actual travel; charge/rush keeps its locked aim.
                float direction=state.RushPhase==1||state.RushPhase==2||!state.Roaming?state.Aim.x:state.Velocity.x;
                if(Mathf.Abs(direction)>.02f)artFacing.localScale=new Vector3(direction<0?-1:1,1,1);
            }
            float entryShade=1,entryAlpha=1;
            if(entryMarker!=null)
            {
                bool ground=!state.Turret&&!state.Flying;
                entryMarker.gameObject.SetActive(entering&&!ground);
                if(ground)
                {
                    float emerge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.42f,.8f,ArenaObjectiveMatch.EntryProgress(state,Match.Now)));
                    bodyRoot.gameObject.SetActive(!entering||ArenaObjectiveMatch.EntryProgress(state,Match.Now)>=.42f);
                    bodyRoot.localPosition=Vector2.zero;bodyRoot.localScale=Vector3.one*(entering?Mathf.Lerp(.75f,1,emerge):1);
                    entryShade=entering?Mathf.Lerp(.45f,1,emerge):1;
                    entryAlpha=entering?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.42f,.55f,ArenaObjectiveMatch.EntryProgress(state,Match.Now))):1;
                }
                else if(state.Flying)
                {
                    float progress=ArenaObjectiveMatch.EntryProgress(state,Match.Now);
                    float emerge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,progress));
                    bodyRoot.gameObject.SetActive(!entering||progress>=.55f);bodyRoot.localPosition=Vector2.zero;
                    bodyRoot.localScale=Vector3.one*(entering?Mathf.Lerp(.75f,1,emerge):1);
                    entryShade=entering?Mathf.Lerp(.5f,1,emerge):1;
                    entryAlpha=entering?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,.78f,progress)):1;
                }
                else
                {
                    float remaining=(float)(state.EntryUntil-Match.Now);
                    float emerge=state.Turret?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,1,ArenaObjectiveMatch.EntryProgress(state,Match.Now))):Mathf.Clamp01(1-remaining/Mathf.Min(.4f,state.EntrySeconds*.4f));
                    bodyRoot.gameObject.SetActive(!entering||emerge>0);
                    Vector2 offset=state.Turret?Vector2.down*1.4f:Vector2.up*1.1f;
                    bodyRoot.localPosition=entering?Vector2.Lerp(offset,Vector2.zero,emerge):Vector2.zero;
                }
                foreach(var light in entryLights)light.color=Color.Lerp(new Color(1,.35f,.05f,.45f),new Color(1,.75f,.3f),Mathf.PingPong((float)Match.Now*5,1));
            }
            if(state.Turret)ArenaEnemyArt.SetEntryFloor(bodyParts,entering?transform.position.y:-10000);
            flash=Mathf.Max(0,flash-Time.deltaTime);
            for(int i=0;i<bodyParts.Count;i++)bodyParts[i].color=flash>0?new Color(1,.95f,.8f):bodyColors[i]*new Color(entryShade,entryShade,entryShade,entryAlpha);
            if(!medical&&artFlash!=(flash>0)){artFlash=flash>0;ArenaEnemyArt.SetFlash(bodyParts,artFlash);}
            if(rushMarker!=null)
            {
                bool rushing=state.RushPhase==1||state.RushPhase==2;rushMarker.gameObject.SetActive(rushing);
                if(artFacing==null)rushMarker.transform.localPosition=new Vector2((state.Aim.x<0?-1:1)*.38f,.5f);
                rushMarker.color=state.RushPhase==2?new Color(1,.94f,.72f):Color.Lerp(new Color(1,.5f,.12f),new Color(1,.95f,.55f),Mathf.PingPong((float)Match.Now*4,1));
                if(flash<=0&&state.RushPhase==1)sensor.color=Color.Lerp(new Color(1,.25f,.1f),Color.yellow,Mathf.PingPong((float)Match.Now*5,1));
            }
            if(barrel!=null)
            {
                barrel.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(state.Aim.y,state.Aim.x)*Mathf.Rad2Deg);
                barrel.localScale=new Vector3(1,state.Aim.x<0?-1:1,1);
                if(flash<=0)sensor.color=state.FireAt>0?Color.Lerp(new Color(1,.3f,.05f),new Color(1,1,.7f),Mathf.PingPong((float)Match.Now*10,1)):new Color(.6f,.2f,.12f);
            }
            // Players pass through the shot collider; contact damage is decided by the server.
            foreach(var player in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))
                if(player.IsSpawned&&player.IsOwner&&player.TryGetComponent<Collider2D>(out var body))Physics2D.IgnoreCollision(shape,body,true);
        }
        void CreateEntryMarker(Sprite markerSprite,bool turret,bool flying)
        {
            entryMarker=new GameObject("Incoming enemy warning").transform;entryMarker.SetParent(transform,false);entryMarker.gameObject.SetActive(false);
            void Light(Vector2 at,Vector2 size)
            {
                var go=new GameObject("Warning light");go.transform.SetParent(entryMarker,false);go.transform.localPosition=at;go.transform.localScale=size;
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=markerSprite;sr.sortingOrder=28;entryLights.Add(sr);
            }
            // Mounted amber indicators instead of a floating bracket/exclamation frame.
            if(flying)
            {
                Light(new Vector2(-.78f,.7f),new Vector2(.035f,.44f));Light(new Vector2(.78f,.7f),new Vector2(.035f,.44f));
                Light(new Vector2(0,1.36f),new Vector2(.42f,.035f));
            }
            else
            {
                Light(new Vector2(-.61f,.04f),new Vector2(.14f,.035f));Light(new Vector2(.61f,.04f),new Vector2(.14f,.035f));
                Light(new Vector2(0,.04f),new Vector2(.36f,.025f));
            }
            var label=new GameObject("Incoming label");label.transform.SetParent(entryMarker,false);label.transform.localPosition=Vector2.up*(flying?1.72f:1.25f);
            var text=label.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=turret?"TURRET / DEPLOY":flying?"DRONE / DEPLOY":"INCOMING";
            text.anchor=TextAnchor.MiddleCenter;text.fontSize=48;text.characterSize=.025f;text.color=new Color(1,.7f,.2f);
            var renderer=label.GetComponent<MeshRenderer>();renderer.sharedMaterial=text.font.material;renderer.sortingOrder=28;
        }
        void OnGUI()
        {
            if(Match==null||!Match.IsSpawned||entryActive||!gameObject.activeInHierarchy||!bodyRoot.gameObject.activeSelf||entryMarker!=null&&entryMarker.gameObject.activeSelf||ArenaObjectiveMatch.TransitionActive||Camera.main==null)return;
            var p=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*1.12f);if(p.z<=0)return;
            float fraction=Match.PatrolHealthFraction(Id);var rect=new Rect(p.x-22,Screen.height-p.y,44,4);var color=GUI.color;
            GUI.color=new Color(.06f,.07f,.08f,.9f);GUI.DrawTexture(rect,Texture2D.whiteTexture);rect.width*=fraction;GUI.color=medical?new Color(.3f,1,.5f):new Color(1,.45f,.25f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=color;
        }
    }
}
