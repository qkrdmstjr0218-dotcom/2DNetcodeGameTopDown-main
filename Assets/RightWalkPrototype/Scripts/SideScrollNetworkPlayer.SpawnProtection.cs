using System.Collections.Generic;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkPlayer
    {
        public const float SpawnProtectionSeconds=2;
        public float SpawnProtectionRemaining
        {
            get
            {
                var match=ArenaObjectiveMatch.Instance;
                if(!IsSpawned||IsDead||match==null||!match.RoomFlow||match.State.Value.Phase!=ArenaObjectiveMatch.Stage.Playing||ArenaObjectiveMatch.CombatBlocked)return 0;
                return Mathf.Max(0,(float)(life.Value.ProtectedUntil-ArenaClock.Now(NetworkManager)));
            }
        }
        public bool SpawnProtected=>SpawnProtectionRemaining>0;
        internal void GrantSpawnProtectionServer()
        {
            var match=ArenaObjectiveMatch.Instance;
            if(!IsServer||!IsSpawned||IsDead||match==null||!match.RoomFlow||match.State.Value.Phase!=ArenaObjectiveMatch.Stage.Playing||ArenaObjectiveMatch.CombatBlocked)return;
            var value=life.Value;value.ProtectedUntil=ArenaClock.Now(NetworkManager)+SpawnProtectionSeconds;life.Value=value;
        }
        void EndSpawnProtectionServer()
        {
            if(!IsServer||life.Value.ProtectedUntil==0)return;
            var value=life.Value;value.ProtectedUntil=0;life.Value=value;
        }
        sealed class ProtectedRenderer {public Renderer Renderer;public bool OriginalForceOff;public float Alpha;public bool ShaderAlpha;}
        readonly List<ProtectedRenderer> protectedRenderers=new();
        float nextProtectionScan;
        MaterialPropertyBlock protectionBlock;
        static readonly int ProtectionAlphaId=Shader.PropertyToID("_SpawnProtectionAlpha");
        public float ProtectionVisualAlpha=>SpawnProtected?.60f+.30f*(.5f+.5f*Mathf.Cos((float)ArenaClock.Now(NetworkManager)*Mathf.PI*3)):1;
        void UpdateSpawnProtectionVisual()
        {
            if(!SpawnProtected){ClearSpawnProtectionVisual();return;}
            protectionBlock??=new MaterialPropertyBlock();
            if(Time.unscaledTime>=nextProtectionScan)
            {
                nextProtectionScan=Time.unscaledTime+.1f;
                foreach(var renderer in GetComponentsInChildren<Renderer>(true))
                {
                    // Never blink projectile lines, labels or the separately acquired shield.
                    if(renderer is not SpriteRenderer&&renderer is not MeshRenderer||renderer.GetComponent<TextMesh>()!=null||renderer.name=="Energy shield")continue;
                    bool known=false;foreach(var item in protectedRenderers)if(item.Renderer==renderer){known=true;break;}
                    if(!known)
                    {
                        bool shaderAlpha=renderer.sharedMaterial!=null&&renderer.sharedMaterial.HasProperty(ProtectionAlphaId);
                        renderer.GetPropertyBlock(protectionBlock);
                        float alpha=shaderAlpha?(protectionBlock.HasFloat(ProtectionAlphaId)?protectionBlock.GetFloat(ProtectionAlphaId):renderer.sharedMaterial.GetFloat(ProtectionAlphaId)):renderer is SpriteRenderer sr?sr.color.a:1;
                        protectedRenderers.Add(new ProtectedRenderer{Renderer=renderer,OriginalForceOff=renderer.forceRenderingOff,Alpha=alpha,ShaderAlpha=shaderAlpha});
                    }
                }
            }
            foreach(var item in protectedRenderers)ApplyProtectionAlpha(item,ProtectionVisualAlpha);
        }
        void ApplyProtectionAlpha(ProtectedRenderer item,float factor)
        {
            if(item.Renderer==null)return;
            if(item.ShaderAlpha){item.Renderer.GetPropertyBlock(protectionBlock);protectionBlock.SetFloat(ProtectionAlphaId,item.Alpha*factor);item.Renderer.SetPropertyBlock(protectionBlock);protectionBlock.Clear();}
            else if(item.Renderer is SpriteRenderer sr){var c=sr.color;c.a=item.Alpha*factor;sr.color=c;}
        }
        void ClearSpawnProtectionVisual()
        {
            if(protectedRenderers.Count==0)return;
            foreach(var item in protectedRenderers)ApplyProtectionAlpha(item,1);
            protectedRenderers.Clear();nextProtectionScan=0;
        }

        void OnDisable(){ClearSpawnProtectionVisual();}
    }
}
