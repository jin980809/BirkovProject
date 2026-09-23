using UnityEngine;

// 엄폐물: 체력이 있고 데미지를 받으면 깎인다(모든 총알 - 플레이어 자신 것 포함). 0이 되면
// 부서진다 - 오브젝트 자체는 SetActive(false) 하지 않고, breakEffect 를 제외한 하위 오브젝트와
// 콜라이더만 꺼서 더 이상 막지도/감지되지도/보이지도 않게 한다 (Destroy 하지 않는다).
//
// 플레이어가 붙어있는 동안에는 플레이어가 쏘는 총알이 이 엄폐물을 무시하고 통과한다
// (엄폐물 너머의 적을 쏠 수 있게). "붙어있다"는 트리거 콜라이더로 자동 감지한다.
// 같은 동안 attachOutline(HitOutlineEffect)이 있으면 아웃라인을 켜서 "지금 이 엄폐물에 붙어있다"고 표시한다.
//
// 에디터 설정: 콜라이더 2개가 필요하고, 계층 구조는 이렇게 둔다.
//  1) 부모(이 스크립트가 붙은 오브젝트): 플레이어 근접 감지용 콜라이더 (Is Trigger 체크,
//     막는 콜라이더보다 살짝 크게) - 필드 연결 불필요, OnTriggerEnter/Exit 가 알아서 반응한다
//  2) 자식: 실제로 총알/이동을 막는 콜라이더 (Is Trigger 체크 안 함) - blockingCollider 에 연결
//     (부모든 자식이든 상관없이 이 오브젝트 아래 어디에 있어도 찾는다)
//
// 붙었을 때 무시해야 하는 콜라이더는 blockingCollider "만"이 아니라 이 오브젝트(자식 포함)의
// 콜라이더 전부(감지용 트리거 포함)다 - 총알(Projectile)은 트리거든 뭐든 뭔가에 닿으면 무조건
// 소멸하기 때문에, 감지용 트리거를 무시 목록에서 빼먹으면 총알이 막는 콜라이더에 닿기도
// 전에 그 감지용 트리거에서 먼저 사라져버린다.
[RequireComponent(typeof(Collider))]
public class CoverObject : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [Tooltip("실제로 총알/이동을 막는 콜라이더 (Is Trigger 체크 안 함, 보통 자식 오브젝트). 데미지 판정 기준으로도 쓰인다")]
    [SerializeField] private Collider blockingCollider;
    [Tooltip("플레이어가 감지 트리거에 붙어있는 동안 켜 둘 아웃라인. 비우면 같은 오브젝트에서 찾고, 없으면 표시하지 않는다")]
    [SerializeField] private HitOutlineEffect attachOutline;
    [Tooltip("부서질 때 재생할 이펙트 오브젝트 (하위 자식). 비우면 이펙트 없이 그냥 꺼진다")]
    [SerializeField] private GameObject breakEffect;

    // Projectile 이 "이 콜라이더가 진짜 맞는 판정 대상인지"를 가릴 때 쓴다 - 감지용 트리거는
    // blockingCollider 가 아니므로 이걸로 구분해서 항상 투명하게 통과시킨다 (붙었는지 여부와 무관하게).
    public Collider BlockingCollider
    {
        get { return blockingCollider; }
    }

    private float health;
    private Collider[] allColliders; // blockingCollider + 감지용 트리거 등, 부모/자식 통틀어 이 오브젝트 아래 콜라이더 전부

    // 지금 붙어있는 플레이어 (붙은 동안만 값이 있음) - OnDisable 때 트리거 이벤트 없이도 떼어낼 대상을 알기 위해 기억해 둔다
    private PlayerController attachedPlayer;

    private void Awake()
    {
        health = maxHealth;
        allColliders = GetComponentsInChildren<Collider>(true); // 부모(감지용)와 자식(막는 콜라이더)을 전부 포함

        if (blockingCollider == null)
        {
            blockingCollider = FindBlockingCollider();
        }

        if (attachOutline == null)
        {
            attachOutline = GetComponent<HitOutlineEffect>();
        }
    }

    // blockingCollider 를 인스펙터에서 안 채웠을 때의 안전장치. 감지용 콜라이더는 항상 Is Trigger 체크가
    // 되어 있으므로, 트리거가 아닌 첫 콜라이더를 막는 콜라이더로 본다.
    private Collider FindBlockingCollider()
    {
        for (int i = 0; i < allColliders.Length; i++)
        {
            if (!allColliders[i].isTrigger)
            {
                return allColliders[i];
            }
        }

        return null;
    }

    private void OnDisable()
    {
        // 꺼지거나(파괴 대신) 씬 전환 등으로 비활성화되면 "붙어있음" 목록에서도 빠진다
        RemoveFromAttached();
    }

    public void TakeDamage(float amount)
    {
        if (health <= 0f || amount <= 0f)
        {
            return;
        }

        health = Mathf.Max(0f, health - amount);

        if (health <= 0f)
        {
            Break();
        }
    }

    // 부서지면 오브젝트 자체는 끄지 않고, breakEffect 를 제외한 하위 오브젝트와 콜라이더만 꺼서
    // 더 이상 막지도/감지되지도/보이지도 않게 한다. 부모(이 오브젝트)가 계속 켜져 있으므로
    // breakEffect 는 부모에서 떼어낼 필요 없이 자식으로 둔 채로 끝까지 재생된다.
    private void Break()
    {
        RemoveFromAttached(); // 콜라이더를 끄기 전에 먼저 명시적으로 떼어낸다 (OnTriggerExit 에 기대지 않는다)

        Transform breakEffectTransform = breakEffect != null ? breakEffect.transform : null;

        for (int i = 0; i < allColliders.Length; i++)
        {
            Collider collider = allColliders[i];
            if (breakEffectTransform != null && collider.transform.IsChildOf(breakEffectTransform))
            {
                continue;
            }

            collider.enabled = false;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == breakEffectTransform)
            {
                continue;
            }

            child.gameObject.SetActive(false);
        }

        PlayBreakEffect();
    }

    private void PlayBreakEffect()
    {
        if (breakEffect == null)
        {
            return;
        }

        breakEffect.SetActive(true);

        ParticleSystem[] particles = breakEffect.GetComponentsInChildren<ParticleSystem>(true);
        float maxLifetime = 0f;

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];
            particle.Clear(true);
            particle.Play(true);

            ParticleSystem.MainModule main = particle.main;
            float lifetime = main.duration + main.startLifetime.constantMax;
            if (lifetime > maxLifetime)
            {
                maxLifetime = lifetime;
            }
        }

        // CFX_AutoDestructShuriken 등 자체 정리 스크립트가 없으면 여기서 대신 정리한다
        if (breakEffect.GetComponentInChildren<CFX_AutoDestructShuriken>() == null)
        {
            Destroy(breakEffect, maxLifetime > 0f ? maxLifetime : 3f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            attachedPlayer = other.GetComponentInParent<PlayerController>();
            if (attachedPlayer != null)
            {
                attachedPlayer.AttachCover(allColliders);

                if (attachOutline != null)
                {
                    attachOutline.SetHeld(true);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RemoveFromAttached();
        }
    }

    private void RemoveFromAttached()
    {
        if (attachOutline != null)
        {
            attachOutline.SetHeld(false);
        }

        if (allColliders == null || attachedPlayer == null)
        {
            return;
        }

        attachedPlayer.DetachCover(allColliders);
        attachedPlayer = null;
    }
}
