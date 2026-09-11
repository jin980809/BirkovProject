using UnityEngine;

// 순수 투사체(Rigidbody)로 날아가는 총알.
// 콜라이더는 Is Trigger 체크 - 물리적으로 튕기지 않고 지나가면서 판정만 한다.
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    private Rigidbody rb;
    private Collider ownCollider;

    private float damage;
    private float maxDistance;
    private Vector3 startPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        TryGetComponent(out ownCollider);
    }

    // direction 은 정규화된 방향, range 는 이 거리를 넘어가면 자동 소멸한다 (0 이하면 무제한)
    public void Launch(Vector3 direction, float speed, float damageAmount, float range, Collider ownerCollider)
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
    }

    private void Update()
    {
        if (maxDistance > 0f && Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
        }

        // 대미지 대상이 아니어도(벽 등) 부딪히면 소멸한다
        Destroy(gameObject);
    }
}
