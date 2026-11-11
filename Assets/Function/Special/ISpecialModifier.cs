namespace Mech.Function
{
    public interface ISpecialModifier
    {
        // 지원하는 효과에 이번 시전의 수치나 동작 보정을 적용한다.
        void Apply(ISpecialEffect effect);
    }
}
