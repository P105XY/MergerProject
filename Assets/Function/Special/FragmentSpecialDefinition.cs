using UnityEngine;

namespace Mech.Function
{
    [CreateAssetMenu(menuName = "Special/Fragment Effect")]
    public sealed class FragmentSpecialDefinition : SpecialEffectDefinition
    {
        [SerializeField] private FragmentSpecialType effectType;
        public override string DisplayName => FragmentSpecialEffect.GetDisplayName(effectType);

        // 레시피나 장비에서 선택한 조합 공격의 독립적인 시전 효과를 만든다.
        public override ISpecialEffect CreateCast(in SpecialContext context)
        {
            return new FragmentSpecialEffect(effectType);
        }
    }
}
