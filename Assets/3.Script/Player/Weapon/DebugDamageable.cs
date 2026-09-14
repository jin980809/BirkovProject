using UnityEngine;

// 테스트용: 아무 오브젝트에 붙이면 IDamageable 을 구현해서 총알에 맞는지 확인할 수 있다.
// 실제 게임 로직 아님 - 검증 끝나면 지운다.
public class DebugDamageable : MonoBehaviour, IDamageable
{
    public void TakeDamage(float amount)
    {
        Debug.Log(gameObject.name + " 이(가) " + amount + " 대미지를 입었습니다.", this);
    }
}
