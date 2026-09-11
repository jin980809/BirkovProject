// 대미지를 받을 수 있는 대상(플레이어, 적 등)이 구현한다.
// 소음(IHearing)과 같은 방식: 무기 쪽은 이 인터페이스만 알고, 실제 구현은 각자 담당한다.
public interface IDamageable
{
    void TakeDamage(float amount);
}
