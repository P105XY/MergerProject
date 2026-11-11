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

        internal SpecialEffectDefinition ReplacementEffect => replacementEffect;
        internal int ReplacementPriority => replacementPriority;

        // 장비가 모든 조합에 적용되거나 지정한 조합에 적용되는지 확인한다.
        internal bool AppliesTo(SpecialRecipe recipe)
        {
            return targetRecipe == null || targetRecipe == recipe;
        }

        // 장비에 설정된 보정을 순서대로 시전 효과에 적용한다.
        internal void ApplyModifiers(ISpecialEffect effect)
        {
            foreach (SpecialModifierDefinition modifier in modifiers) {
                if (modifier != null) {
                    modifier.Apply(effect);
                }
            }
        }
    }
}
