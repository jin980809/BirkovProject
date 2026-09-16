using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShot : MonoBehaviour
{
    private Animator ani;

    [SerializeField] private EnemyShotData enemyShotData;

    [Header("총알 설정")]
    [SerializeField] private int damage = 10;

    [Header("연발")]
    [SerializeField] private int bulletsPerShot = 1;

    [Header("동시 발사")]
    [SerializeField] private int bulletsPerBurst = 1;

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

    private int fireCount;
    private int bulletCount;
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
            bulletsPerShot = enemyShotData.bulletsPerShot;
            bulletsPerBurst = enemyShotData.bulletsPerBurst;
            spreadAngle = enemyShotData.spreadAngle;
            fireInterval = enemyShotData.fireInterval;
            magazineSize = enemyShotData.magazineSize;
            reloadTime = enemyShotData.reloadTime;
        }

        fireIntervalWfs = new WaitForSeconds(fireInterval);
        reloadTimeWfs = new WaitForSeconds(reloadTime);

    }


    public IEnumerator Fire_co()
    {
        if (isReloading)
        {
            yield break;
        }

        // 탄창이 비어있으면 먼저 재장전
        if (currentMagazine <= 0)
        {
            yield return StartCoroutine(Reload_co());
        }

        // 이번 발사에서 쏠 총알 수
        fireCount = Mathf.Min(bulletsPerShot, currentMagazine);


        ani.SetBool("Shot", true);
        
        for (int i = 0; i < fireCount; i++)
        {
            bulletCount = Mathf.Min(bulletsPerBurst, currentMagazine);

            for (int j = 0; j < bulletCount; j++)
            {
                
                Fire();
                currentMagazine--;
            }

            if (i < fireCount - 1)
            {
                yield return new WaitForSeconds(fireInterval);
            }
        }
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


        yield return new WaitForSeconds(reloadTime);

        currentMagazine = magazineSize;

        isReloading = false;

        reloadImage.SetActive(false);
    }
}
