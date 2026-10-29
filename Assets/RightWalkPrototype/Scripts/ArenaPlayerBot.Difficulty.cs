namespace RightWalkPrototype
{
    public sealed partial class ArenaPlayerBot
    {
        int Difficulty=>SideScrollNetworkSession.Instance!=null?SideScrollNetworkSession.Instance.BotDifficulty:1;
        public float ReactionDelay=>Difficulty==0?.45f:Difficulty==2?.12f:.18f;
        public float AimError=>Difficulty==0?.5f:Difficulty==2?.10f:.24f;
        public float ThinkInterval=>Difficulty==0?.9f:Difficulty==2?.4f:.6f;
        public float FireAttemptInterval=>Difficulty==0?.48f:Difficulty==2?.12f:.24f;
        public float DodgeInterval=>Difficulty==0?3.2f:Difficulty==2?1.35f:1.7f;
    }
}
