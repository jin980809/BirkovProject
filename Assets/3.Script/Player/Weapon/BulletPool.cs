using System.Collections.Generic;
using UnityEngine;

// 총알(Projectile) 오브젝트 풀. 미리 만들어두고 재사용해서 Instantiate/Destroy 비용을 줄인다.
// 다 빌려주고 모자라면 새로 만들어서 늘어난다 (총알이 없어서 발사가 씹히는 것보다 낫다).
// 생성되는 총알은 전부 이 오브젝트의 자식이 된다.
// 그래서 이 오브젝트는 반드시 최상위(움직이지 않는 곳)에 둬야 한다 - 플레이어 밑에 두면
// 날아가는 중인 총알이 플레이어를 따라 끌려간다.
public class BulletPool : MonoBehaviour
{
    [SerializeField] private Projectile prefab;
    [SerializeField] private int initialSize = 20;

    private readonly Queue<Projectile> available = new Queue<Projectile>();

    private void Awake()
    {
        if (GetComponentInParent<PlayerController>() != null)
        {
            Debug.LogWarning(
                "BulletPool: 플레이어 계층 밑에 있습니다. 여기서 생성되는 총알이 " +
                "플레이어를 따라 끌려갈 수 있으니 최상위(움직이지 않는 곳)로 옮기세요.", this);
        }

        for (int i = 0; i < initialSize; i++)
        {
            available.Enqueue(CreateInstance());
        }
    }

    // position/rotation 으로 옮기고 활성화해서 돌려준다.
    // 실제 속도/대미지는 받은 뒤 호출부가 Projectile.Launch() 로 세팅한다.
    public Projectile Rent(Vector3 position, Quaternion rotation)
    {
        Projectile projectile = available.Count > 0 ? available.Dequeue() : CreateInstance();
        projectile.PrepareForRent(position, rotation);
        return projectile;
    }

    public void Return(Projectile projectile)
    {
        projectile.gameObject.SetActive(false);
        available.Enqueue(projectile);
    }

    private Projectile CreateInstance()
    {
        Projectile projectile = Instantiate(prefab, transform);
        projectile.gameObject.SetActive(false);
        return projectile;
    }
}
