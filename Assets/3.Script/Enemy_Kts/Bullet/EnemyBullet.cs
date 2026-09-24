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

    public void Initialize(EnemyBulletPool pool)
    {
        enemyBulletPool = pool;
    }

    public void Fire(Vector3 direction, float damage)
    {
        this.direction = direction.normalized;
        this.damage = damage;

        lifeTimer = lifeTime;
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;

        lifeTimer -= Time.deltaTime;

        if (lifeTimer <= 0f)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        IDamageable target = other.GetComponent<IDamageable>();

        if (target != null)
        {
            target.TakeDamage(damage);
        }

        ReturnToPool();

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
