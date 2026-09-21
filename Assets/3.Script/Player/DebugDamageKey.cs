using UnityEngine;
using UnityEngine.InputSystem;

// 테스트용: 지정한 키(기본 K)를 누를 때마다 플레이어가 데미지를 받는다. 사망 처리 등을 확인할 때 쓴다.
// 실제 피격과 같은 경로(PlayerVitals.TakeDamage)를 타므로 방어구 감쇄도 그대로 적용된다.
// 에디터와 개발용 빌드에서만 동작한다 - 다 쓰면 이 컴포넌트를 지워도 된다.
//
// 세팅: 플레이어(PlayerVitals 와 같은 오브젝트)에 붙인다.
[RequireComponent(typeof(PlayerVitals))]
public class DebugDamageKey : MonoBehaviour
{
    [SerializeField] private Key damageKey = Key.K;
    [SerializeField] private float damage = 10f;

    private PlayerVitals vitals;

    private void Awake()
    {
        vitals = GetComponent<PlayerVitals>();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || vitals == null)
        {
            return;
        }

        if (keyboard[damageKey].wasPressedThisFrame)
        {
            vitals.TakeDamage(damage);
        }
    }
#endif
}
