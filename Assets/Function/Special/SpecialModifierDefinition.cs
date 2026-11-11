using UnityEngine;

namespace Mech.Function
{
    public abstract class SpecialModifierDefinition : ScriptableObject, ISpecialModifier
    {
        // 호환되는 시전 효과만 변경하고 그 외 효과는 건너뛴다.
        public abstract void Apply(ISpecialEffect effect);
    }
}
