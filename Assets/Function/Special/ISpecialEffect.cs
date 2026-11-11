namespace Mech.Function
{
    public interface ISpecialEffect
    {
        // 이번 시전의 효과를 시작하고 성공 여부를 반환한다.
        bool TryExecute(in SpecialContext context);
    }
}
