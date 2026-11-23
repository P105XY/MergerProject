using System;

namespace Mech.Function
{
    [Serializable]
    public sealed class PlayerCheckpointState
    {
        public const int CurrentWeaponSystemVersion = 1;
        public int WeaponSystemVersion;
        public int WeaponGauge;
        public int HitsTowardFragment;
        public int CollectedA;
        public int CollectedB;
        public int CollectedC;
        public int ActiveA;
        public int ActiveB;
        public int ActiveC;
        public int SpecialUsesRemaining;
        public CollectibleType[] FragmentOrder;
    }
}
