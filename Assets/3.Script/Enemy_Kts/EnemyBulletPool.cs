using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBulletPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int poolSize = 30;

    private Queue<GameObject> bulletPool = new Queue<GameObject>();

    private void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            CreateBullet();
        }
    }

    private GameObject CreateBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab, transform);

        bullet.SetActive(false);

        bulletPool.Enqueue(bullet);

        return bullet;
    }

    public GameObject GetBullet()
    {
        // 사용 가능한 총알이 없으면 하나 추가 생성
        if (bulletPool.Count == 0)
        {
            CreateBullet();
        }

        GameObject bullet = bulletPool.Dequeue();

        bullet.SetActive(true);

        return bullet;
    }

    public void ReturnBullet(GameObject bullet)
    {
        bullet.SetActive(false);

        bullet.transform.SetParent(transform);

        bulletPool.Enqueue(bullet);
    }
}
