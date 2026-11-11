using UnityEngine;

namespace Mech.Function
{
    [CreateAssetMenu(menuName = "Special/Recipe")]
    public sealed class SpecialRecipe : ScriptableObject
    {
        [SerializeField, Min(0)] private int aCount;
        [SerializeField, Min(0)] private int bCount;
        [SerializeField, Min(0)] private int cCount;
        [SerializeField] private SpecialEffectDefinition baseEffect;

        // 활성 조합과 레시피의 A, B, C 개수가 각각 일치하는지 확인한다.
        internal bool Matches(FragmentCombination combination)
        {
            return aCount == combination.ACount && bCount == combination.BCount && cCount == combination.CCount;
        }

        // 장비의 교체 효과를 먼저 선택하고 보정을 적용해 최종 효과를 실행한다.
        internal bool TryExecute(in SpecialContext context, SpecialEquipment[] equippedItems)
        {
            SpecialEquipment replacement = null;

            foreach (SpecialEquipment item in equippedItems) {
                if (item != null && item.AppliesTo(this) && item.ReplacementEffect != null
                    && (replacement == null || item.ReplacementPriority > replacement.ReplacementPriority)) {
                    replacement = item;
                }
            }

            SpecialEffectDefinition definition = replacement != null ? replacement.ReplacementEffect : baseEffect;

            if (definition == null) {
                return false;
            }

            ISpecialEffect effect = definition.CreateCast(context);

            if (effect == null) {
                return false;
            }

            foreach (SpecialEquipment item in equippedItems) {
                if (item != null && item.AppliesTo(this)) {
                    item.ApplyModifiers(effect);
                }
            }

            return effect.TryExecute(context);
        }
    }
}
