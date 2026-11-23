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
        public bool Matches(FragmentCombination combination)
        {
            return aCount == combination.ACount && bCount == combination.BCount && cCount == combination.CCount;
        }

        // 장비의 교체 효과를 먼저 선택하고 보정을 적용해 최종 효과를 실행한다.
        public bool TryExecute(in SpecialContext context, SpecialEquipment[] equippedItems)
        {
            return Execute(context, equippedItems, this);
        }

        // 별도 레시피 에셋이 없는 조합은 기본 조합 효과에 현재 장비를 적용한다.
        public static bool TryExecuteDefault(in SpecialContext context, SpecialEquipment[] equippedItems)
        {
            return Execute(context, equippedItems, null);
        }

        // 시전과 같은 효과를 선택해 HUD에 표시할 이름을 돌려준다.
        public static string GetEffectName(SpecialRecipe recipe, FragmentCombination combination, SpecialEquipment[] equippedItems)
        {
            SpecialEffectDefinition definition = SelectDefinition(recipe, combination, equippedItems);
            return definition != null ? definition.DisplayName : recipe == null ? FragmentSpecialEffect.GetDefaultName(combination) : "";
        }

        // 교체 우선순위가 가장 높은 장비의 효과나 레시피 기본 효과를 고른다.
        private static SpecialEffectDefinition SelectDefinition(SpecialRecipe recipe, FragmentCombination combination,
            SpecialEquipment[] equippedItems)
        {
            SpecialEquipment replacement = null;

            foreach (SpecialEquipment item in equippedItems) {
                if (item != null && item.AppliesTo(recipe, combination) && item.ReplacementEffect != null
                    && (replacement == null || item.ReplacementPriority > replacement.ReplacementPriority)) {
                    replacement = item;
                }
            }

            return replacement != null ? replacement.ReplacementEffect : recipe != null ? recipe.baseEffect : null;
        }

        // 레시피와 기본 조합 효과가 같은 장비 교체 및 보정 순서로 실행되도록 한다.
        private static bool Execute(in SpecialContext context, SpecialEquipment[] equippedItems, SpecialRecipe recipe)
        {
            SpecialEffectDefinition definition = SelectDefinition(recipe, context.Combination, equippedItems);
            ISpecialEffect effect = definition != null ? definition.CreateCast(context)
                : recipe == null ? FragmentSpecialEffect.Create(context.Combination) : null;

            if (effect == null) {
                return false;
            }

            foreach (SpecialEquipment item in equippedItems) {
                if (item != null && item.AppliesTo(recipe, context.Combination)) {
                    item.ApplyModifiers(effect);
                }
            }

            return effect.TryExecute(context);
        }
    }
}
