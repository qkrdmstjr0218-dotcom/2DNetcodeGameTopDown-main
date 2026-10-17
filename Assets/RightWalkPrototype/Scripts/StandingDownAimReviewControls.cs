using UnityEngine;

namespace RightWalkPrototype
{
    // Used only in the separate review scene; all poses still use the actual aim rig.
    public sealed class StandingDownAimReviewControls : MonoBehaviour
    {
        public RightFacingAimRig aim;
        public SideFacingPreview facing;
        [Min(.5f)] public float interval=1.2f;
        public bool autoCycle;
        private float next;
        private int phase;
        private string label="마우스 조준";

        private void Start() { if(autoCycle)Cycle();else UseMouse(); }
        private void Update()
        {
            if(autoCycle&&Time.unscaledTime>=next)Cycle();
        }
        private void Cycle()
        {
            int sign=phase<3?1:-1;
            switch(phase%3)
            {
                case 0: ApplySide(sign);break;
                case 1: ApplyFront(sign,-35*Mathf.Deg2Rad);break;
                default: ApplyFront(sign,0);break;
            }
            phase=(phase+1)%6;next=Time.unscaledTime+interval;
        }
        public void ShowSide(int sign) { autoCycle=false;ApplySide(sign); }
        public void ShowFront(int sign,float radians) { autoCycle=false;ApplyFront(sign,radians); }
        private void ApplySide(int sign)
        {
            facing.SetFacing(sign);aim.SetAimDirection(sign>0?Vector2.right:Vector2.left);
            label=sign>0?"오른쪽 측면":"왼쪽 측면";
        }
        private void ApplyFront(int sign,float radians)
        {
            facing.SetFacing(sign);
            var overlay=aim.frontDownAim;overlay.ShowAngle(radians);
            aim.SetAimTarget((Vector2)overlay.MuzzlePosition+overlay.AimDirection*3f);
            label=(sign>0?"오른쪽":"왼쪽")+" 정면 하단 "+Mathf.RoundToInt(-90-radians*Mathf.Rad2Deg)+"°";
        }
        public void UseMouse() { autoCycle=false;aim.ResetAim();aim.mouseAim=true;label="마우스 조준"; }
        public void BeginAuto() { autoCycle=true;phase=0;Cycle(); }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Mathf.Max(12,Screen.width-320),12,308,190),GUI.skin.box);
            GUILayout.Label("정지 하단 사격 의상 비교");GUILayout.Label(label);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("측면 →",GUILayout.Height(28)))ShowSide(1);
            if(GUILayout.Button("정면 ↓",GUILayout.Height(28)))ShowFront(1,0);
            if(GUILayout.Button("측면 ←",GUILayout.Height(28)))ShowSide(-1);
            GUILayout.EndHorizontal();GUILayout.BeginHorizontal();
            if(GUILayout.Button("하단 -55° →",GUILayout.Height(28)))ShowFront(1,-35*Mathf.Deg2Rad);
            if(GUILayout.Button("하단 -55° ←",GUILayout.Height(28)))ShowFront(-1,-35*Mathf.Deg2Rad);
            GUILayout.EndHorizontal();GUILayout.BeginHorizontal();
            if(GUILayout.Button("자동 연속 비교",GUILayout.Height(28)))BeginAuto();
            if(GUILayout.Button("마우스 조준",GUILayout.Height(28)))UseMouse();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
