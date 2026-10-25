using System;
using UnityEngine;
namespace RightWalkPrototype
{
    // Scoped skin: other IMGUI components never inherit these styles.
    public static class ArenaUiTheme
    {
        static GUISkin skin;
        public static bool Button(string label,params GUILayoutOption[] options)
        {bool pressed=GUILayout.Button(label,options);if(pressed)ArenaAudio.Ui(ArenaAudio.Cue.UiClick);return pressed;}
        static Texture2D Tile(Color fill,Color edge)
        {
            var t=new Texture2D(4,4,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[16];for(int y=0;y<4;y++)for(int x=0;x<4;x++)pixels[y*4+x]=x==0||y==0||x==3||y==3?edge:fill;
            t.SetPixels(pixels);t.Apply();return t;
        }
        public static IDisposable Use()
        {
            if(skin==null)
            {
                skin=UnityEngine.Object.Instantiate(GUI.skin);skin.hideFlags=HideFlags.HideAndDontSave;
                if(RelayArenaPrototype.Instance!=null)skin.font=RelayArenaPrototype.Instance.readyRoomFont;
                var panel=Tile(new Color(.035f,.065f,.08f,.96f),new Color(.16f,.29f,.34f,.9f));
                var button=Tile(new Color(.08f,.17f,.20f),new Color(.23f,.44f,.49f));
                var hover=Tile(new Color(.1f,.26f,.29f),new Color(.35f,.83f,.85f));
                skin.label.fontSize=15;skin.label.normal.textColor=new Color(.83f,.91f,.93f);skin.label.padding=new RectOffset(2,2,3,3);
                skin.box.normal.background=panel;skin.box.border=new RectOffset(1,1,1,1);skin.box.padding=new RectOffset(16,16,14,14);
                skin.button.normal.background=button;skin.button.hover.background=hover;skin.button.active.background=hover;
                skin.button.normal.textColor=skin.button.hover.textColor=skin.button.active.textColor=new Color(.87f,.97f,.98f);
                skin.button.border=new RectOffset(1,1,1,1);skin.button.padding=new RectOffset(12,12,8,8);skin.button.fontSize=15;
                skin.textField.normal.background=panel;Static(skin.textField);skin.textField.focused.background=button;
                foreach(var state in new[]{skin.textField.normal,skin.textField.hover,skin.textField.active,skin.textField.focused,skin.textField.onNormal,skin.textField.onHover,skin.textField.onActive,skin.textField.onFocused}){state.scaledBackgrounds=new Texture2D[0];state.textColor=Color.white;}
                skin.textField.border=new RectOffset(1,1,1,1);
                skin.textField.padding=new RectOffset(10,10,8,8);skin.textField.fontSize=15;skin.textField.normal.textColor=skin.textField.focused.textColor=Color.white;
                skin.toggle.fontSize=13;skin.toggle.normal.textColor=new Color(.6f,.72f,.76f);
                skin.label.normal.background=null;skin.label.normal.scaledBackgrounds=new Texture2D[0];Static(skin.label);Static(skin.box);
            }
            var scope=new Scope(GUI.skin);GUI.skin=skin;return scope;
        }
        sealed class Scope:IDisposable{readonly GUISkin old;public Scope(GUISkin old){this.old=old;}public void Dispose(){GUI.skin=old;}}
        public static GUIStyle Static(GUIStyle style)
        {
            foreach(var state in new[]{style.hover,style.active,style.focused,style.onNormal,style.onHover,style.onActive,style.onFocused})
            {state.background=style.normal.background;state.scaledBackgrounds=style.normal.scaledBackgrounds;state.textColor=style.normal.textColor;}
            return style;
        }
        public static void DrawPortrait(Rect frame,Texture2D texture)
        {
            if(texture==null)return;
            // These four UI captures share the measured alpha bounds (5,53)..(128,160).
            Rect uv=texture.width==128&&texture.height==160?new Rect(5f/128,0,123f/128,107f/160):new Rect(0,0,1,1);
            float width=texture.width*uv.width,height=texture.height*uv.height,scale=Mathf.Min(frame.width/width,frame.height/height);
            GUI.DrawTextureWithTexCoords(new Rect(frame.center.x-width*scale/2,frame.center.y-height*scale/2,width*scale,height*scale),texture,uv,true);
        }
        public static GUIStyle Title=>Static(new GUIStyle(GUI.skin.label){fontSize=26,normal={textColor=new Color(.65f,.92f,.94f)}});
        public static GUIStyle Muted=>Static(new GUIStyle(GUI.skin.label){fontSize=13,wordWrap=true,normal={textColor=new Color(.55f,.69f,.73f)}});
    }
}
