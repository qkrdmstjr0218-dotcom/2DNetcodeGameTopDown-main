using UnityEngine;

namespace RightWalkPrototype
{
    // Movement/aim/fire stay manual. The checkbox is a reversible trial display option.
    public sealed class FrontDownAimPlaytestHud : MonoBehaviour
    {
        public FrontDownAimOverlay overlay;
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Mathf.Max(12,Screen.width-330),12,318,120),GUI.skin.box);
            GUILayout.Label("직접 조작 · 하단 사격 시험");
            GUILayout.Label("마우스: 조준 / 좌클릭 유지: 연사");
            if(overlay!=null)overlay.allowMovingDownAim=GUILayout.Toggle(overlay.allowMovingDownAim,"이동 중 정면 하단 사격 시험");
            GUILayout.Label(overlay!=null&&overlay.IsActive?(overlay.AirbornePoseActive?"공중 정면 하단 자세":overlay.MovingPoseActive?"하단 조준 + 기존 걷기·달리기":"지상 정면 하단 자세"):"기존 측면·이동·공중 자세");
            GUILayout.EndArea();
        }
    }
}
