using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifeTime = 3f;

    private EnemyBulletPool enemyBulletPool;

    public Vector3 direction { get; private set; }
    private float damage;

    private float lifeTimer;

    // 이동 구간 검사에 쓰는 것들. 총알 레이어가 물리 설정(Layer Collision Matrix)에서 충돌하도록
    // 되어 있는 레이어만 검사한다.
    private int hitMask;
    private Rigidbody body;

    private void Awake()
    {
        TryGetComponent(out body);
        hitMask = BuildHitMask();
    }

    public void Initialize(EnemyBulletPool pool)
    {
        enemyBulletPool = pool;
    }

    public void Fire(Vector3 direction, float damage)
    {
        this.direction = direction.normalized;
        this.damage = damage;

        lifeTimer = lifeTime;

        // 이동은 transform 이 직접 한다. 물리에 맡기지 않으므로 kinematic 으로 두고 깨워 둔다
        // (풀에서 꺼낸 총알의 Rigidbody 가 잠들어 있으면 트리거가 발생하지 않는다).
        if (body != null)
        {
            body.isKinematic = true;
            body.WakeUp();
        }
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;

        // 트리거(OnTriggerEnter)에만 의존하지 않고 이번 프레임에 지나갈 구간을 직접 검사한다.
        // 총알이 빠르면 한 프레임에 콜라이더를 건너뛰어(터널링) 트리거가 아예 발생하지 않는다.
        // 콜라이더 안에서 시작한 레이는 그 콜라이더를 잡지 않으므로, 총구가 쏜 적의 몸통 안에 있어도
        // 자기 자신에게 맞지 않는다.
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, step, hitMask,
                QueryTriggerInteraction.Ignore))
        {
            // 적은 통과시킨다 (아군 오사 방지)
            if (hit.collider.GetComponentInParent<EnemyController>() == null)
            {
                transform.position = hit.point;
                ApplyHit(hit.collider);
                return;
            }
        }

        transform.position += direction * step;

        lifeTimer -= Time.deltaTime;

        if (lifeTimer <= 0f)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 적은 통과시킨다. 총구(firePoint)가 쏜 적의 몸통 콜라이더(반지름 0.8) 안에 있어서,
        // 그냥 두면 발사한 순간 자기 자신에게 맞고 사라져 총알이 총구를 벗어나지 못한다.
        // 다른 적에게도 맞지 않게 되므로 아군 오사도 함께 막힌다.
        if (other.GetComponentInParent<EnemyController>() != null)
        {
            return;
        }

        ApplyHit(other);
    }

    // 맞은 대상에게 데미지를 주고 풀로 돌아간다. 이동 구간 검사와 트리거 양쪽에서 쓴다.
    private void ApplyHit(Collider other)
    {
        IDamageable target = other.GetComponent<IDamageable>();

        if (target == null)
        {
            // 콜라이더가 자식에 있는 경우(플레이어)에도 찾도록 부모까지 본다 (CheckCloseTarget 과 동일)
            target = other.GetComponentInParent<IDamageable>();
        }

        if (target != null)
        {
            target.TakeDamage(damage);
        }

        ReturnToPool();
    }

    // 물리 설정에서 이 총알 레이어와 충돌하도록 되어 있는 레이어만 모은다.
    // (Layer Collision Matrix 를 그대로 따르므로 에디터에서 끈 조합은 검사하지 않는다)
    private int BuildHitMask()
    {
        int mask = 0;

        for (int layer = 0; layer < 32; layer++)
        {
            if (!Physics.GetIgnoreLayerCollision(gameObject.layer, layer))
            {
                mask |= 1 << layer;
            }
        }

        return mask;
    }

    private void ReturnToPool()
    {
        if (enemyBulletPool != null)
        {
            enemyBulletPool.ReturnBullet(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
