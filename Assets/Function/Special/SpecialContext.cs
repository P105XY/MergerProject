using UnityEngine;

namespace Mech.Function
{
    public readonly struct SpecialContext
    {
        public PlayerBase Caster { get; }
        public FragmentCombination Combination { get; }
        public Vector2 Origin { get; }
        public int FacingDirection { get; }

        // 시전자와 조합, 시전 시점의 위치와 방향을 기록한다.
        internal SpecialContext(PlayerBase caster, FragmentCombination combination)
        {
            Caster = caster;
            Combination = combination;
            Origin = (Vector2)caster.transform.position;
            FacingDirection = caster.FacingDirection;
        }
    }
}
