using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShot : MonoBehaviour
{
    [Header("총알 설정")]
    [SerializeField] private int damage = 10;
    [SerializeField] private int bulletsPerShot = 1;

    [Header("발사 설정")]
    [SerializeField] private float fireInterval = 0.1f;

    [Header("탄창")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private int currentMagazine;

    [Header("재장전")]
    [SerializeField] private float reloadTime = 2.0f;

    [Header("총구")]
    [SerializeField] private Transform firePoint;

    [Header("총알 풀")]
    [SerializeField] private EnemyBulletPool enemyBulletPool;

    private bool isReloading;

    private void Awake()
    {
        //초기 탄알
        currentMagazine = magazineSize;
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
        int fireCount = Mathf.Min(bulletsPerShot, currentMagazine);

        for (int i = 0; i < fireCount; i++)
        {
            Fire();

            currentMagazine--;

            // 연사 간격
            if (i < fireCount - 1)
            {
                yield return new WaitForSeconds(fireInterval);
            }
        }
    }

    private void Fire()
    {
        GameObject bullet = enemyBulletPool.GetBullet();

        if (bullet == null)
        {
            return;
        }

        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = firePoint.rotation;

        EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();

        if (enemyBullet != null)
        {
            enemyBullet.Fire(firePoint.forward, damage);
        }
    }

    private IEnumerator Reload_co()
    {
        if (isReloading)
        {
            yield break;
        }

        isReloading = true;

        Debug.Log("재장전 시작");

        yield return new WaitForSeconds(reloadTime);

        currentMagazine = magazineSize;

        isReloading = false;

        Debug.Log("재장전 완료");
    }
}
