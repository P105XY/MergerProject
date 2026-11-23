using UnityEngine;

namespace Mech.Function
{
    public readonly struct SpecialContext
    {
        public PlayerBase Caster { get; }
        public FragmentCombination Combination { get; }
        public Vector2 Origin { get; }
        public int FacingDirection { get; }
        public Vector2 AttackDirection { get; }
        public float AttackAngle => Mathf.Atan2(AttackDirection.y, AttackDirection.x) * Mathf.Rad2Deg;

        // 좌우를 기준으로 시전자의 기존 조합과 위치를 기록한다.
        public SpecialContext(PlayerBase caster, FragmentCombination combination)
            : this(caster, combination, Vector2.right * caster.FacingDirection)
        {
        }

        // 시전 시점의 위치와 조준 벡터를 기록해 이후 입력 변화와 분리한다.
        public SpecialContext(PlayerBase caster, FragmentCombination combination, Vector2 attackDirection)
        {
            Caster = caster;
            Combination = combination;
            Origin = (Vector2)caster.transform.position;
            FacingDirection = caster.FacingDirection;
            AttackDirection = attackDirection.sqrMagnitude > 0f ? attackDirection.normalized : Vector2.right * FacingDirection;
        }
    }
}
