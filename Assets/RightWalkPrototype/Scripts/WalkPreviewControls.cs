using UnityEngine;

namespace RightWalkPrototype
{
    public sealed class WalkPreviewControls : MonoBehaviour
    {
        public RightFacingWalker walker;
        public Camera followCamera;
        public float cameraHeight = .45f;
        public bool whiteBackground;
        private Vector3 origin;

        public void SetWhiteBackground(bool enabled)
        {
            whiteBackground = enabled;
            if (followCamera != null)
            {
                followCamera.clearFlags = CameraClearFlags.SolidColor;
                followCamera.backgroundColor = enabled ? Color.white : new Color(.055f, .075f, .1f);
            }
        }

        private void Start() { origin = walker.transform.position; SetWhiteBackground(whiteBackground); }

        private void LateUpdate()
        {
            if (followCamera == null || walker == null) return;
            var target = walker.transform.position + new Vector3(0, cameraHeight, -10);
            followCamera.transform.position = Vector3.Lerp(followCamera.transform.position, target,
                1 - Mathf.Exp(-8 * Time.deltaTime));
        }

        private void OnGUI()
        {
            if (walker == null) return;
            var facing=walker.GetComponent<SideFacingPreview>();
            GUILayout.BeginArea(new Rect(20, 20, 330, facing != null ? 320 : 240), GUI.skin.box);
            GUILayout.Label((facing != null ? "LEFT / RIGHT WALK / " : "RIGHT WALK / ") + (walker.frames == null ? 0 : walker.frames.Length) + "-FRAME SPRITE SHEET");
            GUILayout.Label(facing != null ? "A/D or arrows: move and turn." : "WASD / Arrow keys: move. Facing stays right.");
            if(facing!=null)
            {
                facing.turnWithMovement=GUILayout.Toggle(facing.turnWithMovement,"Face movement direction");
                GUILayout.BeginHorizontal();
                if(GUILayout.Button("Face left")){facing.SetFacing(-1);walker.GetComponentInChildren<RightFacingAimRig>().ResetAim();}
                if(GUILayout.Button("Face right")){facing.SetFacing(1);walker.GetComponentInChildren<RightFacingAimRig>().ResetAim();}
                GUILayout.EndHorizontal();
            }
            walker.autoPreview = GUILayout.Toggle(walker.autoPreview, "Preview walking in place");
            bool light = GUILayout.Toggle(whiteBackground, "White background (inspect sprite edges)");
            if (light != whiteBackground) SetWhiteBackground(light);
            var aim=walker.GetComponentInChildren<RightFacingAimRig>();
            if(aim != null)
            {
                bool enabled=GUILayout.Toggle(aim.mouseAim,facing != null ? "Mouse aim (left/right, +/-45 degrees)" : "Mouse aim (right side, +/-45 degrees)");
                if(enabled != aim.mouseAim) { if(enabled)aim.mouseAim=true;else aim.ResetAim(); }
                GUILayout.Label("Aim: " + aim.AimAngle.ToString("0.0") + " degrees");
            }
            GUILayout.Label("Speed: " + walker.moveSpeed.ToString("0.0"));
            walker.moveSpeed = GUILayout.HorizontalSlider(walker.moveSpeed, .3f, 3f);
            if (GUILayout.Button("Return to center")) walker.transform.position = origin;
            GUILayout.EndArea();
        }
    }
}

