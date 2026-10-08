using UnityEngine;
namespace TopDownShooter.Networking
{
    public sealed class SpawnPointMarker : MonoBehaviour
    {
        private void OnGUI()
        {
            if (Camera.main == null) return;
            Vector3 screen=Camera.main.WorldToScreenPoint(transform.position);
            if (screen.z <= 0) return;
            bool outside=screen.x<30 || screen.x>Screen.width-130 || screen.y<30 || screen.y>Screen.height-30;
            float x=Mathf.Clamp(screen.x-55,18,Screen.width-130);
            float y=Mathf.Clamp(Screen.height-screen.y-16,18,Screen.height-134);
            if (x<352 && y<402) x=365;
            Color before=GUI.color; GUI.color=new Color(1,0.8f,0.25f);
            GUI.Box(new Rect(x,y,120,30),(outside?"SPAWN > ":"SPAWN: ")+gameObject.name);
            GUI.color=before;
        }
    }
}
