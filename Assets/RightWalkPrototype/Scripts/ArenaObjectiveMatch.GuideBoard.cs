using Unity.Netcode;
namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        readonly NetworkVariable<int> guideSelection=new(0);
        public int GuideTopic=>guideSelection.Value&3;
        public int GuideRevision=>guideSelection.Value;
        public bool SetGuideTopicServer(int topic)
        {
            if(!IsServer||!IsSpawned||!PracticeRoom||topic<0||topic>3)return false;
            guideSelection.Value=(int)(((uint)guideSelection.Value+4)&0x3ffffffc)|(topic&3);return true;
        }
    }
}
