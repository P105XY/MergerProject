using System;

namespace Mech.Function
{
    [Serializable]
    internal sealed class PlayerCheckpointState
    {
        public int CollectedA;
        public int CollectedB;
        public int CollectedC;
        public int ActiveA;
        public int ActiveB;
        public int ActiveC;
        public int SpecialUsesRemaining;
    }
}
