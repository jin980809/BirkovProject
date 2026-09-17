using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBulletPool : MonoBehaviour
{
    [Header("총알 풀링")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int poolSize = 30;

    [Header("파티클 풀링")]
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private int particlePoolSize = 30;
    [SerializeField] ParticleSystem partcleSystem;
    private GameObject bullet;
    private GameObject particle;
    private Queue<GameObject> bulletPool = new Queue<GameObject>();
    private Queue<GameObject> particlePool = new Queue<GameObject>();

    private void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            CreateBullet();
            CreateParticle();
        }
    }

    private GameObject CreateBullet()
    {
        bullet = Instantiate(bulletPrefab, transform);

        bullet.SetActive(false);

        bulletPool.Enqueue(bullet);

        return bullet;
    }

    private GameObject CreateParticle()
    {
        particle = Instantiate(particlePrefab, transform);

        particle.SetActive(false);

        particlePool.Enqueue(particle);

        return particle;
    }

    public GameObject GetBullet()
    {
        // 사용 가능한 총알이 없으면 하나 추가 생성
        if (bulletPool.Count == 0)
        {
            CreateBullet();
        }

        bullet = bulletPool.Dequeue();

        bullet.SetActive(true);

        return bullet;
    }
    public GameObject GetParticle()
    {
        if (particlePool.Count == 0)
        {
            CreateParticle();
        }

        particle = particlePool.Dequeue();

        particle.SetActive(true);

        return particle;
    }

    public void ReturnBullet(GameObject bullet)
    {
        bullet.SetActive(false);
        particle = GetParticle();

        EnemyBullet enemyBullet;
        bullet.TryGetComponent(out enemyBullet);

        particle.transform.position = bullet.transform.position;
        particle.transform.rotation = Quaternion.LookRotation(-enemyBullet.direction);

        particle.SetActive(true);

        bullet.transform.SetParent(transform);

        bulletPool.Enqueue(bullet);
    }

    public void ParticleReturn(GameObject particle)
    {
        particle.SetActive(false);

        particle.transform.SetParent(transform);

        particlePool.Enqueue(particle);
    }
}
