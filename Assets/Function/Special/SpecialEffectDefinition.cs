using UnityEngine;

namespace Mech.Function
{
    public abstract class SpecialEffectDefinition : ScriptableObject
    {
        public virtual string DisplayName => name;

        // 공유 설정을 바탕으로 보정할 수 있는 새 시전 효과를 만든다.
        public abstract ISpecialEffect CreateCast(in SpecialContext context);
    }
}
