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

    // 맞은 지점을 찾을 때 총알 위치에서 이만큼 더 뒤에서 앞으로 쏜다 (총알이 표면을 살짝 지나친 뒤에 판정되므로)
    private const float ImpactRayMargin = 0.5f;

    // 이동 경로 검사(스윕)에서 한 번에 받아 둘 수 있는 접촉 수와, 이동 거리에 더해 주는 여유 거리
    private const int SweepBufferSize = 16;
    private const float SweepSkin = 0.05f;

    private Rigidbody rb;
    private Collider ownCollider;
    private BulletPool pool;
    private ImpactEffectPool impactPool;

    // 발사한 플레이어. 스윕에서 자기 자신의 다른 콜라이더에 맞지 않도록 구분하는 데 쓴다
    private PlayerController ownerPlayer;
    private readonly RaycastHit[] sweepHits = new RaycastHit[SweepBufferSize];

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

        // 피격 이펙트 풀도 마찬가지로 스스로 찾는다. 씬에 없으면 이펙트만 생략한다.
        impactPool = FindAnyObjectByType<ImpactEffectPool>();
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
    // bodyOrigin 은 발사한 플레이어의 몸통 위치(총구 높이)다. 총구가 벽을 넘어가 있을 때 총알이 벽 너머에서 생겨
    // 그대로 나가지 않도록, 몸통에서 총구까지 막는 것이 있는지 먼저 검사해서 있으면 그 벽에서 바로 맞은 것으로 처리한다.
    public void Launch(Vector3 direction, float speed, float damageAmount, float range, Collider ownerCollider, Vector3 bodyOrigin, IEnumerable<Collider> coversToIgnore = null)
    {
        startPosition = transform.position;
        damage = damageAmount;
        maxDistance = range;

        rb.linearVelocity = direction * speed;

        ownerPlayer = ownerCollider != null ? ownerCollider.GetComponentInParent<PlayerController>() : null;

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

        // 몸통 -> 총구 사이가 막혀 있으면 총알을 내보내지 않고 그 지점에서 바로 맞은 것으로 처리한다.
        // (붙어 있는 엄폐물과 자기 몸은 위에서 정한 대로 제외된다)
        Vector3 toMuzzle = transform.position - bodyOrigin;
        float toMuzzleDistance = toMuzzle.magnitude;
        if (toMuzzleDistance > 0.0001f &&
            TryFindSweepHit(bodyOrigin, toMuzzle / toMuzzleDistance, toMuzzleDistance, out RaycastHit blocked))
        {
            HitBySweep(blocked);
        }
    }

    private void Update()
    {
        if (maxDistance > 0f && Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            ReturnToPool();
        }
    }

    // 총알이 너무 빨라서(60m/s = 물리 한 번에 1.2m) 트리거의 겹침 판정만으로는 얇은 벽이나 좁은 적을 통째로
    // 건너뛰어 버린다. 그래서 물리가 총알을 움직이기 직전에, 이번 한 번에 움직일 거리만큼 앞쪽으로 선을 쏴서
    // 그 사이에 막는 것이 있는지 미리 검사한다. 있으면 그 지점에서 맞은 것으로 처리한다.
    // (트리거 판정은 그대로 두므로 겹침으로 잡히던 것은 지금처럼 잡힌다)
    private void FixedUpdate()
    {
        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;
        if (speed < 0.0001f)
        {
            return;
        }

        Vector3 direction = velocity / speed;
        float distance = speed * Time.fixedDeltaTime + SweepSkin;

        if (TryFindSweepHit(transform.position, direction, distance, out RaycastHit nearest))
        {
            HitBySweep(nearest);
        }
    }

    // origin 에서 direction 으로 distance 만큼 선을 쏴서, 맞은 것으로 볼 수 있는 대상 중 가장 가까운 것을 찾는다
    private bool TryFindSweepHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit nearest)
    {
        int hitCount = Physics.RaycastNonAlloc(
            origin, direction, sweepHits, distance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

        bool found = false;
        nearest = default;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit candidate = sweepHits[i];
            if (candidate.distance < nearestDistance && IsSweepTarget(candidate.collider))
            {
                nearest = candidate;
                nearestDistance = candidate.distance;
                found = true;
            }
        }

        return found;
    }

    // 스윕이 "맞았다"고 볼 수 있는 대상인지. 자기 몸, 이번 발에서 무시하기로 한 엄폐물, 다른 총알,
    // 대미지 대상이 아닌 트리거(상호작용 영역 등)는 건너뛴다.
    private bool IsSweepTarget(Collider other)
    {
        if (other == ownCollider || ignoredCovers.Contains(other))
        {
            return false;
        }

        if (ownerPlayer != null && other.GetComponentInParent<PlayerController>() == ownerPlayer)
        {
            return false;
        }

        if (other.GetComponentInParent<Projectile>() != null)
        {
            return false;
        }

        if (other.isTrigger && other.GetComponentInParent<IDamageable>() == null)
        {
            return false;
        }

        return true;
    }

    private void HitBySweep(RaycastHit hit)
    {
        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
            PlayHitOutline(hit.collider);
        }

        if (impactPool != null)
        {
            impactPool.Play(target != null, hit.point, hit.normal);
        }

        ReturnToPool();
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
            PlayHitOutline(other);
        }

        PlayImpactEffect(other, target != null);

        // 대미지 대상이 아니어도(벽 등) 부딪히면 소멸(반환)한다
        ReturnToPool();
    }

    // 맞은 대상(또는 그 부모)에 HitOutlineEffect 가 붙어 있으면 아웃라인 이펙트를 재생한다.
    // 대상 스크립트를 고칠 필요 없이 그 컴포넌트만 붙이면 된다.
    private void PlayHitOutline(Collider other)
    {
        HitOutlineEffect outline = other.GetComponentInParent<HitOutlineEffect>();
        if (outline != null)
        {
            outline.Play();
        }
    }

    // 맞은 자리에 피격 파티클을 낸다. 트리거로 판정하므로 접촉 정보가 없어서, 총알 뒤쪽에서 진행 방향으로
    // 맞은 콜라이더에 레이를 쏴서 맞은 지점과 면의 방향을 구한다. 못 구하면 총알 위치와 진행 반대 방향을 쓴다.
    private void PlayImpactEffect(Collider other, bool hitDamageable)
    {
        // 상호작용 영역 같은 트리거에 닿아서 사라지는 경우(대미지 대상도 아님)에는 이펙트를 내지 않는다
        if (impactPool == null || (other.isTrigger && !hitDamageable))
        {
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;
        if (speed < 0.0001f)
        {
            return;
        }

        Vector3 direction = velocity / speed;
        float backDistance = speed * Time.fixedDeltaTime + ImpactRayMargin;
        Ray ray = new Ray(transform.position - direction * backDistance, direction);

        Vector3 point = transform.position;
        Vector3 normal = -direction;
        if (other.Raycast(ray, out RaycastHit hit, backDistance * 2f))
        {
            point = hit.point;
            normal = hit.normal;
        }

        impactPool.Play(hitDamageable, point, normal);
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
