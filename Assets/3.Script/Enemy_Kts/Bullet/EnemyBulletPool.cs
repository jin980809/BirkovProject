using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBulletPool : MonoBehaviour
{
    [Header("풀링")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private int poolSize = 30;


    //[SerializeField] ParticleSystem partcleSystem;
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
    //총알 풀링 1개 생성
    private GameObject CreateBullet()
    {
        bullet = Instantiate(bulletPrefab, transform);

        bullet.SetActive(false);

        bulletPool.Enqueue(bullet);

        return bullet;
    }
    //파티클 풀링 1개 생성
    private GameObject CreateParticle()
    {
        particle = Instantiate(particlePrefab, transform);

        particle.SetActive(false);

        particlePool.Enqueue(particle);

        return particle;
    }
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
    //파티클 정보 가져가기
    public GameObject GetParticle()
    {
        // 사용 가능한 파티클이 없으면 하나 추가 생성
        if (particlePool.Count == 0)
        {
            CreateParticle();
        }

        particle = particlePool.Dequeue();

        particle.SetActive(true);

        return particle;
    }
    //총알 돌려받기 및 파티클 생성
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
    public void SpawnParticle()
    {

    }


    //파티클 돌려받기
    public void ReturnParticle(GameObject particle)
    {
        particle.SetActive(false);

        particle.transform.SetParent(transform);

        particlePool.Enqueue(particle);
    }
}
