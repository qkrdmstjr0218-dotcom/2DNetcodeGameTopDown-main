using UnityEngine;
namespace TopDownShooter.Diagnostics
{
    public sealed class MasterJointStudy:MonoBehaviour
    {
        public Transform nearThigh,farThigh,nearShin,farShin,nearFoot,farFoot;
        public bool animate=true,showFillers=false;
        public MeshRenderer[] partRenderers;
        public int inspectPart=-1;
        [Range(0,8)]public float kneeDegrees=4;
        [Range(0,6)]public float ankleDegrees=3;
        [Range(0,1)]public float phase;
        private void Update()
        {
            if(animate)phase=Mathf.Repeat(phase+Time.deltaTime*.35f,1);
            ApplyPose(phase);
        }
        public void ApplyPose(float p)
        {
            phase=p;float wave=Mathf.Sin(p*Mathf.PI*2);
            nearShin.localRotation=Quaternion.Euler(0,0,wave*kneeDegrees);
            farShin.localRotation=Quaternion.Euler(0,0,-wave*kneeDegrees);
            nearFoot.localRotation=Quaternion.Euler(0,0,-wave*ankleDegrees);
            farFoot.localRotation=Quaternion.Euler(0,0,wave*ankleDegrees);
            foreach(var r in GetComponentsInChildren<MeshRenderer>())if(r.name=="Temporary joint underlay")r.enabled=showFillers;
            if(partRenderers!=null)for(int i=0;i<partRenderers.Length;i++)partRenderers[i].enabled=inspectPart<0||inspectPart==i;
        }
        private void OnGUI()
        {
            GUI.Label(new Rect(15,15,700,28),"LEFT: ORIGINAL / RIGHT: SMALL JOINT TEST (not walking)");
            animate=GUI.Toggle(new Rect(15,45,150,25),animate,"ANIMATE");
            phase=GUI.HorizontalSlider(new Rect(15,78,260,25),phase,0,1);
            GUI.Label(new Rect(15,108,300,25),"Knee +/- "+kneeDegrees.ToString("F1")+" deg");
            kneeDegrees=GUI.HorizontalSlider(new Rect(15,138,260,25),kneeDegrees,0,8);
            GUI.Label(new Rect(15,165,300,25),"Ankle +/- "+ankleDegrees.ToString("F1")+" deg");
            ankleDegrees=GUI.HorizontalSlider(new Rect(15,195,260,25),ankleDegrees,0,6);
            showFillers=GUI.Toggle(new Rect(15,220,320,25),showFillers,"TEMP JOINT UNDERLAY");
            if(GUI.Button(new Rect(15,252,170,28),"REST POSE")){animate=false;phase=0;ApplyPose(0);}
            if(GUI.Button(new Rect(15,290,170,28),"NEXT PART")){inspectPart=(inspectPart+1)%8;animate=false;phase=0;ApplyPose(0);}
            if(GUI.Button(new Rect(195,290,140,28),"ALL PARTS")){inspectPart=-1;ApplyPose(phase);}
            if(inspectPart>=0&&partRenderers!=null)GUI.Label(new Rect(15,325,450,28),partRenderers[inspectPart].name);
        }
    }
}
