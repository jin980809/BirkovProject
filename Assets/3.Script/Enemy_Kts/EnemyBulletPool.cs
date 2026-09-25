using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBulletPool : MonoBehaviour
{
    [Header("총알 풀링")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int bulletPoolSize = 30;

    [Header("도탄 파티클 풀링")]
    [SerializeField] private GameObject ricocheParticlePrefab;
    [SerializeField] private int ricocheParticlePoolSize = 30;

    [Header("사망 파티클 풀링")]
    [SerializeField] private GameObject dieParticlePrefab;
    [SerializeField] private int dieParticlePoolSize = 5;

    //[SerializeField] ParticleSystem partcleSystem;
    private GameObject bullet;
    private GameObject ricocheParticle;
    private GameObject dieParticle;
    private Queue<GameObject> bulletPool = new Queue<GameObject>();
    private Queue<GameObject> ricocheParticlePool = new Queue<GameObject>();
    private Queue<GameObject> dieParticlePool = new Queue<GameObject>();

    private void Awake()
    {
        for (int i = 0; i < bulletPoolSize; i++)
        {
            CreateBullet();
        }
        for (int i = 0; i < ricocheParticlePoolSize; i++)
        {
            CreateRicocheParticle();
        }
        for (int i = 0; i < dieParticlePoolSize; i++)
        {
            CreateDieParticle();
        }
    }
    //---------------------------------------풀링 생성-------------------------------------------
    //총알 풀링 1개 생성
    private GameObject CreateBullet()
    {
        bullet = Instantiate(bulletPrefab, transform);

        bullet.SetActive(false);

        bulletPool.Enqueue(bullet);

        return bullet;
    }
    //도탄 파티클 풀링 1개 생성
    private GameObject CreateRicocheParticle()
    {
        ricocheParticle = Instantiate(ricocheParticlePrefab, transform);

        ricocheParticle.SetActive(false);

        ricocheParticlePool.Enqueue(ricocheParticle);

        return ricocheParticle;
    }
    //사망 파티클 풀링 1개 생성
    private GameObject CreateDieParticle()
    {
        dieParticle = Instantiate(dieParticlePrefab, transform);

        dieParticle.SetActive(false);

        dieParticlePool.Enqueue(dieParticle);

        return dieParticle;
    }
   

    //--------------------------------------도탄 및 총알 관련-------------------------------------------
    //총알 정보 가져가기
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
    //도탄 파티클 정보 가져가기
    public GameObject GetRicocheParticle()
    {
        // 사용 가능한 파티클이 없으면 하나 추가 생성
        if (ricocheParticlePool.Count == 0)
        {
            CreateRicocheParticle();
        }

        ricocheParticle = ricocheParticlePool.Dequeue();

        ricocheParticle.SetActive(true);

        return ricocheParticle;
    }
    //총알 돌려받기 및 파티클 생성
    public void ReturnBullet(GameObject bullet)
    {
        bullet.SetActive(false);
        ricocheParticle = GetRicocheParticle();

        EnemyBullet enemyBullet;
        bullet.TryGetComponent(out enemyBullet);

        ricocheParticle.transform.position = bullet.transform.position;
        ricocheParticle.transform.rotation = Quaternion.LookRotation(-enemyBullet.direction);

        ricocheParticle.SetActive(true);

        bullet.transform.SetParent(transform);

        bulletPool.Enqueue(bullet);
    }
    //도탄 파티클 돌려받기
    public void ReturnRicocheParticle(GameObject particle)
    {
        particle.SetActive(false);

        particle.transform.SetParent(transform);

        ricocheParticlePool.Enqueue(particle);
    }

    public void PlayRicochetParticle(Vector3 position, Vector3 direction)
    {
        GameObject particle = GetRicocheParticle();

        particle.transform.position = position;
        particle.transform.rotation = Quaternion.LookRotation(-direction);

        particle.SetActive(true);
    }

    //----------------------------------------사망 관련---------------------------------------------

    public GameObject GetDieParticle(Transform transform)
    {
        // 사용 가능한 파티클이 없으면 하나 추가 생성
        if (dieParticlePool.Count == 0)
        {
            CreateDieParticle();
        }

        dieParticle = dieParticlePool.Dequeue();
        dieParticle.transform.position = transform.position;
        dieParticle.SetActive(true);

        return dieParticle;
    }

    public void ReturnDieParticle(GameObject particle)
    {
        particle.SetActive(false);
    
        particle.transform.SetParent(transform);

        dieParticlePool.Enqueue(particle);
    }
}
