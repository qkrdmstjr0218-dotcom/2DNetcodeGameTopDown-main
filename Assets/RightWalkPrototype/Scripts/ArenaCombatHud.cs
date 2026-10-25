using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RightWalkPrototype
{
    // Local presentation only. Never renders actors into the tactical map.
    [DefaultExecutionOrder(300)]
    public sealed class ArenaCombatHud : MonoBehaviour
    {
        public Texture2D[] portraits;
        public static ArenaCombatHud Instance { get; private set; }
        public static bool UsesGameUi => Instance!=null;
        public static bool Active => Instance!=null && !ArenaTutorial.Active && ArenaObjectiveMatch.Instance!=null
            && ArenaObjectiveMatch.Instance.IsSpawned && ArenaObjectiveMatch.Instance.RoomFlow
            && ArenaObjectiveMatch.Instance.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing;
        public static bool TabVisible => Active && Instance.tab;
        bool tab;
        static Vector2 hackingMarkerPosition;
        static int hackingMarkerFrame=-1;
        static Texture2D supportMarker;
        ArenaMapSettings map;
        BoxCollider2D[] terrain;
        GUIStyle text,small,large,ammoInitial,ammoCount,worldNumber,worldPlayer,timer,keycap,ammoHint;
        SideScrollNetworkPlayer localPlayer;
        float healthTrail=-1,trailUntil,lastHealth;uint trailLife;SideScrollNetworkPlayer trailPlayer;
        static readonly Color Cyan=new Color(.3f,.86f,.94f), Back=new Color(.025f,.055f,.07f,.92f);
        static readonly Color[] AmmoColors={new Color(.55f,.85f,.93f),new Color(1,.83f,.25f),new Color(.77f,.52f,1),new Color(.63f,.95f,.34f)};
        void Awake(){Instance=this;map=GetComponent<ArenaMapSettings>();}
        void Start(){terrain=GetComponentsInChildren<BoxCollider2D>();}
        void OnDestroy(){if(Instance==this)Instance=null;}
        void LateUpdate()
        {
            if(!Active)return;var p=Local();if(p==null)return;float hp=p.Health/(float)p.MaxHealth;
            if(trailPlayer!=p||trailLife!=p.LifeVersion||healthTrail<0){trailPlayer=p;trailLife=p.LifeVersion;healthTrail=lastHealth=hp;}
            if(hp<lastHealth){healthTrail=Mathf.Max(healthTrail,lastHealth);trailUntil=Time.unscaledTime+.16f;}
            if(hp>=healthTrail||hp>lastHealth)healthTrail=hp;
            else if(Time.unscaledTime>=trailUntil)healthTrail=Mathf.MoveTowards(healthTrail,hp,Time.unscaledDeltaTime*1.5f);
            lastHealth=hp;
        }
        public void TickInput()
        {
            bool held=false;
#if ENABLE_INPUT_SYSTEM
            held=Keyboard.current!=null&&Keyboard.current.tabKey.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            held=Input.GetKey(KeyCode.Tab);
#endif
            tab=Active&&RelayArenaPrototype.HasGameplayFocus&&!RelayArenaPrototype.Instance.MenuOpen&&held;
        }
        static void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        static void Bar(Rect r,float value,Color c){Fill(r,new Color(.08f,.13f,.16f,.95f));r.width*=Mathf.Clamp01(value);Fill(r,c);}
        static void Line(Vector2 a,Vector2 b,Color c,float width=1)
        {var old=GUI.matrix;GUI.matrix=old*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);Fill(new Rect(0,-width/2,Vector2.Distance(a,b),width),c);GUI.matrix=old;}
        static void Outline(Rect r,Color c){Fill(new Rect(r.x,r.y,r.width,1),c);Fill(new Rect(r.x,r.yMax-1,r.width,1),c);Fill(new Rect(r.x,r.y,1,r.height),c);Fill(new Rect(r.xMax-1,r.y,1,r.height),c);}
        static void Diamond(Vector2 at,Color c,float radius=6,float width=1){Line(at+Vector2.up*radius,at+Vector2.right*radius,c,width);Line(at+Vector2.right*radius,at+Vector2.down*radius,c,width);Line(at+Vector2.down*radius,at+Vector2.left*radius,c,width);Line(at+Vector2.left*radius,at+Vector2.up*radius,c,width);}
        static void HackingSymbol(Vector2 at,Color color,float size=1)
        {
            Diamond(at,color,15*size,Mathf.Max(1,1.6f*size));
            Fill(new Rect(at.x-5*size,at.y-4*size,10*size,8*size),color);
            Fill(new Rect(at.x-3*size,at.y-2*size,6*size,2*size),new Color(.92f,1,1,color.a));
            for(int sign=-1;sign<=1;sign+=2)
            {
                Line(at+new Vector2(sign*7,-3)*size,at+new Vector2(sign*10,-3)*size,color,Mathf.Max(1,1.5f*size));
                Line(at+new Vector2(sign*7,3)*size,at+new Vector2(sign*10,3)*size,color,Mathf.Max(1,1.5f*size));
            }
        }
        public static bool SupportBodyVisible(Vector3 guideWorld)
        {
            if(Camera.main==null)return false;
            var point=Camera.main.WorldToViewportPoint(guideWorld+Vector3.up*(.5f-ArenaObjectiveMatch.SupportGuideHeight));
            return point.z>0&&point.x>.025f&&point.x<.975f&&point.y>.05f&&point.y<.95f;
        }
        public static float UiScale=>Mathf.Clamp(Screen.height/900f,1,1.25f)*Mathf.Min(1,Screen.width/1000f,Screen.height/600f);
        void Styles()
        {
            if(text!=null)return;
            var font=RelayArenaPrototype.Instance.readyRoomFont;
            text=new GUIStyle(GUI.skin.label){font=font,fontSize=15,alignment=TextAnchor.MiddleLeft,normal={textColor=new Color(.87f,.94f,.95f)}};
            small=new GUIStyle(text){fontSize=12};large=new GUIStyle(text){fontSize=22,alignment=TextAnchor.MiddleCenter};
            ammoInitial=new GUIStyle(large){alignment=TextAnchor.MiddleLeft};ammoCount=new GUIStyle(small){fontSize=14,alignment=TextAnchor.MiddleRight};
            keycap=new GUIStyle(small){fontSize=13,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(0,0,0,0)};ammoHint=new GUIStyle(keycap){fontSize=12,normal={textColor=new Color(.6f,.75f,.8f)}};
            worldNumber=new GUIStyle(text){fontSize=16,richText=true};worldPlayer=new GUIStyle(text){fontSize=20};timer=new GUIStyle(large){fontSize=30};
            foreach(var style in new[]{text,small,large,ammoInitial,ammoCount,worldNumber,worldPlayer,timer,keycap,ammoHint})ArenaUiTheme.Static(style);
        }
        SideScrollNetworkPlayer Local()
        {if(localPlayer!=null&&localPlayer.IsSpawned&&localPlayer.IsOwner)return localPlayer;foreach(var p in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsSpawned&&p.IsOwner){localPlayer=p;return p;}return null;}
        void OnGUI()
        {
            if(Event.current.type!=EventType.Repaint||!Active||RelayArenaPrototype.Instance.MenuOpen)return;
            Styles();var p=Local();if(p==null)return;
            var matrix=GUI.matrix;float scale=UiScale;
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));float w=Screen.width/scale,h=Screen.height/scale;
            if(tab)DrawTab(w,h,p);else
            {
                var identity=ArenaPlayerAppearance.Accent(p.PlayerNumber);ArenaHudSkin.Panel(new Rect(16,16,236,77),identity);
                Fill(new Rect(24,22,27,16),new Color(identity.r,identity.g,identity.b,.10f));
                GUI.Label(new Rect(26,19,30,21),"P"+p.PlayerNumber,text);
                GUI.Label(new Rect(58,19,184,21),p.Health+" / "+p.MaxHealth+(p.Shield>0?"  (+"+p.Shield+")":""),new GUIStyle(text){alignment=TextAnchor.MiddleRight});
                float health=p.Health/(float)p.MaxHealth;
                ArenaHudSkin.Meter(new Rect(26,53,216,7),health,health<=.25f?new Color(1,.43f,.34f):identity,healthTrail);
                ArenaHudSkin.Meter(new Rect(26,44,216,4),p.Shield/(float)SideScrollNetworkPlayer.ShieldCapacity,new Color(.4f,.65f,1));
                ArenaHudSkin.Meter(new Rect(26,66,216,4),p.Stamina/p.MaximumStamina,p.SprintExhausted?new Color(1,.65f,.3f):Cyan);
                int seconds=Mathf.Max(0,Mathf.CeilToInt((float)(ArenaObjectiveMatch.Instance.State.Value.End-ArenaObjectiveMatch.Instance.Now)));
                var clockRect=new Rect(w/2-60,28,120,48);var clockColor=seconds<=10?new Color(1,.48f,.36f):seconds<=30?new Color(1,.76f,.38f):new Color(.65f,.83f,.87f);
                ArenaHudSkin.Panel(clockRect,clockColor);var led=clockColor;led.a=seconds<=10?.65f+.35f*Mathf.Sin(Time.unscaledTime*4)*Mathf.Sin(Time.unscaledTime*4):.75f;
                Fill(new Rect(clockRect.x+35,clockRect.y+1,50,2),led);timer.normal.textColor=clockColor;ArenaUiTheme.Static(timer);GUI.Label(clockRect,seconds/60+":"+(seconds%60).ToString("00"),timer);
                ArenaHudSkin.Panel(new Rect(16,h-42,46,26),Cyan);GUI.Label(new Rect(22,h-42,38,26),"Tab",small);
                DrawAmmo(w,h,p);DrawRanking(w,p);
            }
            GUI.matrix=matrix;
        }
        public static ArenaObjectiveMatch.Score[] RankedScores(ArenaObjectiveMatch match)
        {
            var rows=new ArenaObjectiveMatch.Score[match.Scores.Count];for(int i=0;i<rows.Length;i++)rows[i]=match.Scores[i];
            Array.Sort(rows,(a,b)=>{int score=b.Total.CompareTo(a.Total);return score!=0?score:a.Number.CompareTo(b.Number);});return rows;
        }
        void DrawRanking(float w,SideScrollNetworkPlayer local)
        {
            var rows=RankedScores(ArenaObjectiveMatch.Instance);float x=w-208;ArenaHudSkin.Panel(new Rect(x,16,192,34+rows.Length*25),Cyan);
            GUI.Label(new Rect(x+12,20,166,23),"현재 순위",small);int rank=1;
            for(int i=0;i<rows.Length;i++)
            {
                var row=rows[i];if(i>0&&Mathf.Abs(row.Total-rows[i-1].Total)>=.05f)rank=i+1;float y=47+i*25;
                if(row.Client==local.OwnerClientId)Fill(new Rect(x+5,y+4,2,14),ArenaPlayerAppearance.Accent(row.Number));
                GUI.Label(new Rect(x+13,y,105,23),rank+"위   P"+row.Number,text);
                GUI.Label(new Rect(x+108,y,70,23),row.Total.ToString("0.0"),new GUIStyle(text){alignment=TextAnchor.MiddleRight});
            }
        }
        public static int Slot(SideScrollNetworkPlayer.AmmoKind kind)=>kind==SideScrollNetworkPlayer.AmmoKind.Normal?0:kind==SideScrollNetworkPlayer.AmmoKind.Rifle?2:kind==SideScrollNetworkPlayer.AmmoKind.Grenade?3:1;
        public static void DrawPracticeAmmo(SideScrollNetworkPlayer p)
        {if(Instance==null||RelayArenaPrototype.Instance.MenuOpen||Event.current.type!=EventType.Repaint)return;Instance.Styles();Instance.DrawAmmo(Screen.width,Screen.height,p);}
        void DrawAmmo(float w,float h,SideScrollNetworkPlayer p)
        {
            const string initials="NSRG";
            var q=new Rect(w-238,h-91,22,18);var e=new Rect(w-44,h-91,22,18);
            ArenaHudSkin.Panel(q,new Color(.45f,.62f,.69f));ArenaHudSkin.Panel(e,new Color(.45f,.62f,.69f));GUI.Label(q,"Q",keycap);GUI.Label(e,"E",keycap);
            float centre=(q.x+e.xMax)*.5f;GUI.Label(new Rect(centre-46,h-91,92,18),"특수탄 전환",ammoHint);
            Line(new Vector2(q.xMax+6,h-82),new Vector2(centre-52,h-82),new Color(.24f,.39f,.45f));Line(new Vector2(centre+52,h-82),new Vector2(e.x-6,h-82),new Color(.24f,.39f,.45f));
            for(int i=0;i<4;i++)
            {
                int rounds=i==0?p.NormalRounds:i==1?p.ShotgunRounds:i==2?p.RifleRounds:p.GrenadeRounds;
                int capacity=i==0?p.NormalCapacity:i==1?SideScrollNetworkPlayer.ShotgunCapacity:i==2?SideScrollNetworkPlayer.RifleCapacity:SideScrollNetworkPlayer.GrenadeCapacity;
                var r=new Rect(w-312+i*74,h-68,68,50);ArenaHudSkin.Panel(r,AmmoColors[i]);
                if(i>0&&Slot(p.SelectedAmmo)==i){Fill(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),new Color(AmmoColors[i].r,AmmoColors[i].g,AmmoColors[i].b,.08f));Outline(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),AmmoColors[i]);}
                if((p.SpecialHeld?Slot(p.SelectedAmmo):0)==i)Fill(new Rect(r.x,r.y,r.width,3),AmmoColors[i]);
                var at=new Vector2(r.x+15,r.y+19);
                ammoInitial.normal.textColor=AmmoColors[i];ArenaUiTheme.Static(ammoInitial);GUI.Label(new Rect(r.x+9,r.y+3,30,25),initials[i].ToString(),ammoInitial);
                var glyph=AmmoColors[i];if(rounds==0)glyph=new Color(.54f,.33f,.34f);ArenaHudSkin.AmmoGlyph(new Rect(r.x+42,r.y+7,18,17),i,glyph);
                Line(new Vector2(r.x+8,r.y+29),new Vector2(r.xMax-8,r.y+29),new Color(.21f,.33f,.39f,.8f));
                ammoCount.normal.textColor=rounds==0?new Color(1,.43f,.37f):new Color(.9f,.95f,.96f);ArenaUiTheme.Static(ammoCount);GUI.Label(new Rect(r.x+5,r.y+30,57,17),rounds+" / "+capacity,ammoCount);
            }
        }
        Vector2 MapPoint(Vector2 world,Rect r)=>new Vector2(r.x+(world.x-map.cameraBounds.xMin)/map.cameraBounds.width*r.width,r.yMax-(world.y-map.cameraBounds.yMin)/map.cameraBounds.height*r.height);
        void DrawMap(Rect r)
        {
            Fill(r,new Color(.04f,.085f,.105f));
            if(map==null||terrain==null)return;
            float fit=Mathf.Min(r.width/map.cameraBounds.width,r.height/map.cameraBounds.height);
            r=new Rect(r.center.x-map.cameraBounds.width*fit/2,r.center.y-map.cameraBounds.height*fit/2,map.cameraBounds.width*fit,map.cameraBounds.height*fit);
            var local=r;
            foreach(var c in terrain)
            {
                if(c==null||!c.enabled||c.isTrigger||c.GetComponentInParent<ArenaReadyRoomControls>()!=null)continue;
                Vector2 a=c.transform.TransformPoint(c.offset+new Vector2(-c.size.x/2,c.size.y/2));
                Vector2 b=c.transform.TransformPoint(c.offset+c.size/2);
                if(a.x<map.cameraBounds.xMin-1||a.x>map.cameraBounds.xMax+1||a.y<map.cameraBounds.yMin-1||a.y>map.cameraBounds.yMax+1)continue;
                var color=c.usedByEffector?Cyan:new Color(.7f,.63f,.42f);
                Line(MapPoint(a,local),MapPoint(b,local),color,2);
                if(!c.usedByEffector){Vector2 d=c.transform.TransformPoint(c.offset-new Vector2(c.size.x/2,c.size.y/2));Vector2 e=c.transform.TransformPoint(c.offset+new Vector2(c.size.x/2,-c.size.y/2));Line(MapPoint(a,local),MapPoint(d,local),color);Line(MapPoint(b,local),MapPoint(e,local),color);}
            }
            var match=ArenaObjectiveMatch.Instance;
            if(match.Points.Length>0)HackingSymbol(MapPoint(match.Points[match.State.Value.Point].CaptureBounds.center,local),Cyan,.5f);

        }
        void DrawTab(float w,float h,SideScrollNetworkPlayer local)
        {
            Fill(new Rect(0,0,w,h),new Color(.008f,.02f,.028f,.83f));
            float width=Mathf.Min(1100,w-64),height=Mathf.Min(650,h-64);var panel=new Rect((w-width)/2,(h-height)/2,width,height);Fill(panel,Back);
            GUI.Label(new Rect(panel.x+24,panel.y+16,250,34),"전장 현황",large);
            float mapWidth=width*.59f;
            DrawMap(new Rect(panel.x+24,panel.y+72,mapWidth-36,height-145));
            float x=panel.x+mapWidth,y=panel.y+66,rowWidth=width-mapWidth-24;
            GUI.Label(new Rect(x+65,y,105,24),"참가자",small);GUI.Label(new Rect(x+rowWidth-130,y,78,24),"점수",small);GUI.Label(new Rect(x+rowWidth-43,y,36,24),"처치",small);
            var match=ArenaObjectiveMatch.Instance;var sorted=new ArenaObjectiveMatch.Score[match.Scores.Count];
            for(int i=0;i<sorted.Length;i++)sorted[i]=match.Scores[i];
            Array.Sort(sorted,(a,b)=>{int score=b.Total.CompareTo(a.Total);return score!=0?score:a.Number.CompareTo(b.Number);});
            var players=FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
            for(int i=0;i<sorted.Length;i++)
            {
                var score=sorted[i];float top=y+32+i*84;var r=new Rect(x,top,rowWidth,76);Fill(r,new Color(.07f,.12f,.145f,.9f));
                if(score.Client==local.OwnerClientId)Outline(r,ArenaPlayerAppearance.Accent(score.Number));
                int portrait=score.Number-1;
                if(portraits!=null&&portrait>=0&&portrait<portraits.Length&&portraits[portrait]!=null)ArenaUiTheme.DrawPortrait(new Rect(x+5,top+4,54,68),portraits[portrait]);
                GUI.Label(new Rect(x+65,top+7,105,24),"P"+score.Number+(score.Client==local.OwnerClientId?" · 나":""),text);
                foreach(var p in players)if(p.IsSpawned&&p.OwnerClientId==score.Client)
                {Bar(new Rect(x+65,top+41,92,5),p.Health/(float)p.MaxHealth,ArenaPlayerAppearance.Accent(score.Number));GUI.Label(new Rect(x+65,top+49,100,20),p.Health+" / "+p.MaxHealth,small);break;}
                GUI.Label(new Rect(x+rowWidth-130,top+22,78,28),score.Total.ToString("0.0"),text);
                GUI.Label(new Rect(x+rowWidth-43,top+22,36,28),score.Kills.ToString(),text);
            }
            GUI.Label(new Rect(panel.x+24,panel.yMax-53,width-48,28),"단독 해킹 1초 = 1점     ·     플레이어 처치 +5     ·     지원 드론 격추 +5",text);
            GUI.Label(new Rect(panel.x+24,panel.yMax-27,width-48,22),"Tab을 놓으면 닫기",small);
        }
        public static void DrawWorldPlayer(SideScrollNetworkPlayer p)
        {
            if(TabVisible||p.IsDead||RelayArenaPrototype.Instance.MenuOpen)return;
            var crouch=p.GetComponent<CrouchTrial>();var at=Camera.main.WorldToScreenPoint(p.transform.position+Vector3.up*(crouch!=null&&crouch.IsCrouched?1.95f:2.45f));if(at.z<=0)return;
            Instance.Styles();float y=Screen.height-at.y;var accent=ArenaPlayerAppearance.Accent(p.PlayerNumber);
            var matrix=GUI.matrix;GUIUtility.ScaleAroundPivot(new Vector2(UiScale,UiScale),new Vector2(at.x,y));
            string stats=p.Health+(p.Shield>0?" (+"+p.Shield+")":"");
            float numberWidth=Instance.worldNumber.CalcSize(new GUIContent(stats)).x+4;
            float width=30+6+84+8+numberWidth,left=at.x-width*.5f,barX=left+36;
            Instance.worldPlayer.normal.textColor=new Color(0,0,0,.85f);ArenaUiTheme.Static(Instance.worldPlayer);GUI.Label(new Rect(left+1,y-9,32,26),"P"+p.PlayerNumber,Instance.worldPlayer);
            Instance.worldPlayer.normal.textColor=accent;ArenaUiTheme.Static(Instance.worldPlayer);GUI.Label(new Rect(left,y-10,32,26),"P"+p.PlayerNumber,Instance.worldPlayer);
            if(p.Shield>0)ArenaHudSkin.Meter(new Rect(barX,y-7,84,4),p.Shield/(float)SideScrollNetworkPlayer.ShieldCapacity,new Color(.4f,.65f,1));
            ArenaHudSkin.Meter(new Rect(barX,y,84,6),p.Health/(float)p.MaxHealth,accent);
            if(p.IsOwner)ArenaHudSkin.Meter(new Rect(barX,y+10,84,4),p.Stamina/p.MaximumStamina,p.SprintExhausted?new Color(1,.6f,.2f):Cyan);
            Instance.worldNumber.normal.textColor=new Color(0,0,0,.85f);ArenaUiTheme.Static(Instance.worldNumber);GUI.Label(new Rect(barX+93,y-9,numberWidth,26),stats,Instance.worldNumber);
            Instance.worldNumber.normal.textColor=Color.white;ArenaUiTheme.Static(Instance.worldNumber);GUI.Label(new Rect(barX+92,y-10,numberWidth,26),p.Health+(p.Shield>0?" <color=#89BBFF>(+"+p.Shield+")</color>":""),Instance.worldNumber);
            GUI.matrix=matrix;
        }
        public static void DrawMarker(Vector3 world,bool support=false,bool liveSupport=false)
        {
            if(!Active||TabVisible||RelayArenaPrototype.Instance.MenuOpen||Camera.main==null)return;
            if(support&&liveSupport&&SupportBodyVisible(world))return;
            var p=Camera.main.WorldToScreenPoint(world);var at=new Vector2(p.x,Screen.height-p.y);
            float scale=UiScale,top=Mathf.Max(110,112*scale);
            Rect safe=new Rect(42*scale,top,Screen.width-84*scale,Screen.height-top-110*scale);bool outside=p.z<0||!safe.Contains(at);
            var delta=at-safe.center;if(p.z<0)delta=-delta;
            if(outside){float t=Mathf.Min(safe.width*.5f/Mathf.Max(.001f,Mathf.Abs(delta.x)),safe.height*.5f/Mathf.Max(.001f,Mathf.Abs(delta.y)));at=safe.center+delta*t;}
            int scoreCount=ArenaObjectiveMatch.Instance.Scores.Count;float rankBottom=(16+34+scoreCount*25+24)*scale;
            if(at.x>Screen.width-224*scale&&at.y<rankBottom)at.y=rankBottom;
            if(!support){hackingMarkerPosition=at;hackingMarkerFrame=Time.frameCount;}
            else if(hackingMarkerFrame==Time.frameCount&&Mathf.Abs(at.x-hackingMarkerPosition.x)<42*scale&&Mathf.Abs(at.y-hackingMarkerPosition.y)<50*scale)
            {
                float minimum=Mathf.Max(safe.yMin+16*scale,at.x>Screen.width-224*scale?rankBottom:0);
                float above=hackingMarkerPosition.y-50*scale;
                at.y=above>=minimum?above:Mathf.Min(safe.yMax-16*scale,hackingMarkerPosition.y+50*scale);
            }
            var color=support?new Color(.35f,.65f,1):Cyan;
            if(!support)color.a*=ArenaObjectivePoint.WarningPulse(ArenaObjectiveMatch.Instance);
            var matrix=GUI.matrix;GUIUtility.ScaleAroundPivot(new Vector2(scale,scale),at);

            if(support)
            {
                if(supportMarker==null)supportMarker=Resources.Load<Texture2D>("FoundryArt/SupportMarker");
                if(supportMarker!=null){var old=GUI.color;GUI.color=Color.white;GUI.DrawTexture(new Rect(at.x-14,at.y-15.75f,28,31.5f),supportMarker);GUI.color=old;}
                else{Line(at+new Vector2(-10,-10),at+new Vector2(10,-10),color,2.4f);Line(at+new Vector2(-10,-10),at+new Vector2(-8,5),color,2.4f);Line(at+new Vector2(10,-10),at+new Vector2(8,5),color,2.4f);Line(at+new Vector2(-8,5),at+new Vector2(0,13),color,2.4f);Line(at+new Vector2(8,5),at+new Vector2(0,13),color,2.4f);}
            }
            else {HackingSymbol(at,color);var match=ArenaObjectiveMatch.Instance;Bar(new Rect(at.x-32,at.y+21,64,4),(float)(match.State.Value.Switch-match.Now)/Mathf.Max(1,match.pointSeconds),color);}
            if(outside){var d=delta.normalized;var side=new Vector2(-d.y,d.x);var tip=at+d*27;Line(tip-d*8,tip,color,2);Line(tip,tip-d*8+side*5,color,2);Line(tip,tip-d*8-side*5,color,2);}
            GUI.matrix=matrix;
        }
    }
}
