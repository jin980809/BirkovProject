using System.Collections.Generic;
using UnityEngine;

// 엄폐물: 체력이 있고 데미지를 받으면 깎인다(모든 총알 - 플레이어 자신 것 포함). 0이 되면
// 꺼진다(SetActive(false)) - Destroy 하지 않는다.
//
// 플레이어가 붙어있는 동안에는 플레이어가 쏘는 총알이 이 엄폐물을 무시하고 통과한다
// (엄폐물 너머의 적을 쏠 수 있게). "붙어있다"는 트리거 콜라이더로 자동 감지한다.
//
// 에디터 설정: 이 오브젝트에 콜라이더 2개가 필요하다.
//  1) 실제로 총알/이동을 막는 콜라이더 (Is Trigger 체크 안 함) - blockingCollider 에 연결
//  2) 플레이어 근접 감지용 콜라이더 (Is Trigger 체크, 1번보다 살짝 크게) - 필드 연결 불필요,
//     같은 오브젝트에 있으면 OnTriggerEnter/Exit 가 알아서 반응한다
//
// 붙었을 때 무시해야 하는 콜라이더는 blockingCollider "만"이 아니라 이 오브젝트의 콜라이더
// 전부(2번 감지용 트리거 포함)다 - 총알(Projectile)은 트리거든 뭐든 뭔가에 닿으면 무조건
// 소멸하기 때문에, 감지용 트리거를 무시 목록에서 빼먹으면 총알이 막는 콜라이더에 닿기도
// 전에 그 감지용 트리거에서 먼저 사라져버린다.
[RequireComponent(typeof(Collider))]
public class CoverObject : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [Tooltip("실제로 총알/이동을 막는 콜라이더 (Is Trigger 체크 안 함). 데미지 판정 기준으로도 쓰인다")]
    [SerializeField] private Collider blockingCollider;

    private float health;
    private Collider[] allColliders; // blockingCollider + 감지용 트리거 등 이 오브젝트의 콜라이더 전부

    // 지금 플레이어가 붙어있는 엄폐물들의 콜라이더 모음 (막는 것 + 감지용 트리거 전부).
    // WeaponController 가 발사할 때 이걸 읽어서 그 총알만 이 콜라이더들을 무시하게 만든다.
    private static readonly HashSet<Collider> attachedCoverColliders = new HashSet<Collider>();

    public static IReadOnlyCollection<Collider> AttachedCoverColliders
    {
        get { return attachedCoverColliders; }
    }

    private void Awake()
    {
        health = maxHealth;
        allColliders = GetComponents<Collider>();

        if (blockingCollider == null)
        {
            blockingCollider = GetComponent<Collider>();
        }
    }

    private void OnDisable()
    {
        // 꺼지거나(파괴 대신) 씬 전환 등으로 비활성화되면 "붙어있음" 목록에서도 빠진다
        RemoveFromAttached();
    }

    public void TakeDamage(float amount)
    {
        if (health <= 0f || amount <= 0f)
        {
            return;
        }

        health = Mathf.Max(0f, health - amount);

        if (health <= 0f)
        {
            gameObject.SetActive(false); // Destroy 하지 않는다
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            for (int i = 0; i < allColliders.Length; i++)
            {
                attachedCoverColliders.Add(allColliders[i]);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RemoveFromAttached();
        }
    }

    private void RemoveFromAttached()
    {
        if (allColliders == null)
        {
            return;
        }

        for (int i = 0; i < allColliders.Length; i++)
        {
            attachedCoverColliders.Remove(allColliders[i]);
        }
    }
}
