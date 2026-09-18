using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShot : MonoBehaviour
{
    private Animator ani;

    [SerializeField] private EnemyShotData enemyShotData;
    [SerializeField] private ParticleSystem shotEffect;

    [Header("총알 설정")]
    [SerializeField] private int damage = 10;

    [Header("연발")]
    [SerializeField] private int minBulletsPerShot = 1;
    [SerializeField] private int maxBulletsPerShot = 1;
    private int rndBulletsPerShot;

    [Header("동시 발사")]
    [SerializeField] private int minBulletsPerBurst = 1;
    [SerializeField] private int maxBulletsPerBurst = 1;
    private int rndBulletsPerBurst;

    [Header("탄퍼짐 설정")]
    [SerializeField] private float spreadAngle = 5.0f;

    [Header("발사 설정")]
    [SerializeField] private float fireInterval = 0.1f;

    [Header("탄창")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private int currentMagazine;

    [Header("재장전")]
    [SerializeField] private float reloadTime = 2.0f;
    [SerializeField]private GameObject reloadImage;

    [Header("총구")]
    [SerializeField] private Transform firePoint;

    [Header("총알 풀")]
    [SerializeField] private EnemyBulletPool enemyBulletPool;

    //private int fireCount;
    //private int bulletCount;
    float randomY;
    private bool isReloading;
    Vector3 direction;
    Vector3 spreadDirection;
    Quaternion spreadRotation;
    private GameObject bullet;
    private EnemyBullet enemyBullet;

    WaitForSeconds fireIntervalWfs;
    WaitForSeconds reloadTimeWfs;

    


    private void Awake()
    {
        TryGetComponent(out ani);
        //초기 탄알
        currentMagazine = magazineSize;
    }
    private void Start()
    {
        if (enemyShotData != null)
        {
            damage = enemyShotData.damage;
            minBulletsPerShot = enemyShotData.minBulletsPerShot;
            maxBulletsPerShot = enemyShotData.maxBulletsPerShot + 1;
            minBulletsPerBurst = enemyShotData.minBulletsPerBurst;
            maxBulletsPerBurst = enemyShotData.maxBulletsPerBurst + 1;
            spreadAngle = enemyShotData.spreadAngle;
            fireInterval = enemyShotData.fireInterval;
            magazineSize = enemyShotData.magazineSize;
            reloadTime = enemyShotData.reloadTime;
        }

        fireIntervalWfs = new WaitForSeconds(fireInterval);
        reloadTimeWfs = new WaitForSeconds(reloadTime);
        shotEffect.Stop();
    }


    public IEnumerator Fire_co()
    {
        if (isReloading)
        {
            yield break;
        }

        // 이번 발사에서 쏠 총알 수
        rndBulletsPerShot = Random.Range(minBulletsPerShot, maxBulletsPerShot);

        //한번에 몇발 쏠 것인지
        rndBulletsPerBurst = Random.Range(minBulletsPerBurst, maxBulletsPerBurst);

        // 탄창이 비어있으면 먼저 재장전
        if (currentMagazine <= 0)
        {
            yield return StartCoroutine(Reload_co());
        }

        

        ani.SetBool("Shot", true);
        
        for (int i = 0; i < rndBulletsPerShot; i++)
        {
            

            
            for (int j = 0; j < rndBulletsPerBurst; j++)
            {
                
                Fire();
                
                currentMagazine--;
            }
            

            if (i < rndBulletsPerShot - 1)
            {
                
                yield return fireIntervalWfs;
                
            }
        }
        //shotEffect.Stop();
        ani.SetBool("Shot", false);
    }
    

    private void Fire()
    {
        bullet = enemyBulletPool.GetBullet();

        if (bullet == null)
        {
            return;
        }

        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = firePoint.rotation;

        bullet.TryGetComponent(out enemyBullet);

        if (enemyBullet != null)
        {
            spreadDirection = GetSpreadDirection();
            enemyBullet.Fire(spreadDirection, damage);
            shotEffect.Play();
            enemyBullet.Initialize(enemyBulletPool);
        }
    }
    private Vector3 GetSpreadDirection()
    {
        randomY = Random.Range(-spreadAngle, spreadAngle);
        spreadRotation = Quaternion.Euler(0f, randomY, 0f);

        direction = firePoint.rotation * spreadRotation * Vector3.forward;

        return direction.normalized;
    }
    private IEnumerator Reload_co()
    {
        if (isReloading)
        {
            yield break;
        }

        isReloading = true;

        reloadImage.SetActive(true);


        yield return reloadTimeWfs;

        currentMagazine = magazineSize;

        isReloading = false;

        reloadImage.SetActive(false);
    }
}
