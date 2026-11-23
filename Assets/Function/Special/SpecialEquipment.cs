using System;
using UnityEngine;

namespace Mech.Function
{
    [CreateAssetMenu(menuName = "Special/Equipment")]
    public sealed class SpecialEquipment : ScriptableObject
    {
        [SerializeField] private SpecialRecipe targetRecipe;
        [SerializeField] private SpecialEffectDefinition replacementEffect;
        [SerializeField] private int replacementPriority;
        [SerializeField] private SpecialModifierDefinition[] modifiers = Array.Empty<SpecialModifierDefinition>();

        public SpecialEffectDefinition ReplacementEffect => replacementEffect;
        public int ReplacementPriority => replacementPriority;

        // 장비가 모든 조합에 적용되거나 지정한 조합에 적용되는지 확인한다.
        public bool AppliesTo(SpecialRecipe recipe, FragmentCombination combination)
        {
            return targetRecipe == null || targetRecipe == recipe || (recipe == null && targetRecipe.Matches(combination));
        }

        // 장비에 설정된 보정을 순서대로 시전 효과에 적용한다.
        public void ApplyModifiers(ISpecialEffect effect)
        {
            foreach (SpecialModifierDefinition modifier in modifiers) {
                if (modifier != null) {
                    modifier.Apply(effect);
                }
            }
        }
    }
}
