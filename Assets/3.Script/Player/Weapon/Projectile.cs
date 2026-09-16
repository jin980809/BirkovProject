using System.Collections.Generic;
using UnityEngine;

// 순수 투사체(Rigidbody)로 날아가는 총알. BulletPool 로 재사용된다.
// 콜라이더는 Is Trigger 체크 - 물리적으로 튕기지 않고 지나가면서 판정만 한다.
// 적이 아니어도 IDamageable 을 구현한 대상이면 전부 맞는다 (범용).
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Tooltip("총알 궤적 이펙트. 비우면 자동 탐색")]
    [SerializeField] private TrailRenderer trail;

    private Rigidbody rb;
    private Collider ownCollider;
    private BulletPool pool;

    private float damage;
    private float maxDistance;
    private Vector3 startPosition;

    // Launch 때 무시 설정한 엄폐물 콜라이더들 - ReturnToPool 에서 다시 충돌하도록 되돌린다
    // (이번 한 발 동안만 무시해야 한다 - 계속 무시하면 나중에 그 엄폐물을 진짜로 쏴도 안 맞는다)
    private readonly List<Collider> ignoredCovers = new List<Collider>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        TryGetComponent(out ownCollider);

        if (trail == null)
        {
            trail = GetComponent<TrailRenderer>();
        }

        // 씬에 하나뿐인 BulletPool 을 스스로 찾는다 (Awake 는 재사용 시 다시 안 불림 - 한 번만 찾으면 됨)
        pool = FindAnyObjectByType<BulletPool>();
    }

    // 풀에서 빌려줄 때 호출: 위치/회전 세팅 + 활성화 + 이전 사용 흔적 제거
    public void PrepareForRent(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        gameObject.SetActive(true);

        if (trail != null)
        {
            trail.Clear(); // 이전 위치에서 여기로 선이 이어져 보이는 것 방지
        }
    }

    // direction 은 정규화된 방향, range 는 이 거리를 넘어가면 자동으로 풀에 반환한다 (0 이하면 무제한).
    // coversToIgnore 는 발사 시점에 플레이어가 붙어있던 엄폐물들 - 이번 발사체만 그 엄폐물들을
    // 무시하고 통과한다 (CoverObject.AttachedCoverColliders 를 WeaponController 가 넘겨준다).
    public void Launch(Vector3 direction, float speed, float damageAmount, float range, Collider ownerCollider, IEnumerable<Collider> coversToIgnore = null)
    {
        startPosition = transform.position;
        damage = damageAmount;
        maxDistance = range;

        rb.linearVelocity = direction * speed;

        // 발사한 쪽(플레이어) 콜라이더와는 충돌 판정을 하지 않는다
        if (ownCollider != null && ownerCollider != null)
        {
            Physics.IgnoreCollision(ownCollider, ownerCollider, true);
        }

        ignoredCovers.Clear();
        if (ownCollider != null && coversToIgnore != null)
        {
            foreach (Collider cover in coversToIgnore)
            {
                if (cover == null)
                {
                    continue;
                }

                Physics.IgnoreCollision(ownCollider, cover, true);
                ignoredCovers.Add(cover);
            }
        }
    }

    private void Update()
    {
        if (maxDistance > 0f && Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 펠릿이 여러 개인 무기(샷건 등)는 같은 지점에서 총알 여러 개가 동시에 스폰되는데,
        // 서로의 트리거 콜라이더가 겹쳐서 "부딪힌 대상"으로 잡히면 스폰되자마자 전부 풀로
        // 반환돼버린다 (pelletCount=1인 무기는 총알이 하나뿐이라 이 문제가 없었다).
        // 총알끼리는 서로 무시한다.
        if (other.GetComponentInParent<Projectile>() != null)
        {
            return;
        }

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
        }

        // 대미지 대상이 아니어도(벽 등) 부딪히면 소멸(반환)한다
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        rb.linearVelocity = Vector3.zero;

        if (ownCollider != null)
        {
            for (int i = 0; i < ignoredCovers.Count; i++)
            {
                if (ignoredCovers[i] != null)
                {
                    Physics.IgnoreCollision(ownCollider, ignoredCovers[i], false);
                }
            }
        }

        ignoredCovers.Clear();

        if (pool != null)
        {
            pool.Return(this);
        }
        else
        {
            Destroy(gameObject); // 풀 연결 없이 테스트할 때를 위한 안전장치
        }
    }
}
