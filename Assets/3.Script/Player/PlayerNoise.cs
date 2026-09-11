using System.Collections.Generic;
using UnityEngine;

// 플레이어가 달릴 때 일정 간격으로 소음을 발생시킨다.
// 반경 안의 IHearing 대상에게 OnHeardNoise(플레이어 위치) 를 호출한다.
// (총 발사 등 다른 소음원은 EmitNoise(radius) 를 직접 호출)
[RequireComponent(typeof(PlayerController))]
public class PlayerNoise : MonoBehaviour
{
    [Header("소음")]
    [Tooltip("달리는 중 소음 발생 간격 (초)")]
    [SerializeField] private float noiseInterval = 0.4f;
    [Tooltip("달릴 때 소음이 들리는 반경")]
    [SerializeField] private float sprintNoiseRadius = 8f;

    [Header("레이어")]
    [Tooltip("소음을 듣는 대상 레이어 (적 등)")]
    [SerializeField] private LayerMask hearingMask;
    [Tooltip("설정하면 이 레이어에 가려진 대상은 못 들음 (벽이 소리 차단). 비우면 반경만 사용")]
    [SerializeField] private LayerMask obstacleMask;

    private PlayerController player;
    private float nextNoiseTime;
    private readonly Collider[] buffer = new Collider[32];
    private readonly HashSet<IHearing> notified = new HashSet<IHearing>();

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (!player.IsSprinting || Time.time < nextNoiseTime)
        {
            return;
        }

        nextNoiseTime = Time.time + noiseInterval;
        EmitNoise(sprintNoiseRadius);
    }

    // 반경 안의 모든 IHearing 대상에게 소음을 알린다
    public void EmitNoise(float radius)
    {
        Vector3 origin = transform.position;

        int count = Physics.OverlapSphereNonAlloc(
            origin, radius, buffer, hearingMask, QueryTriggerInteraction.Ignore);

        notified.Clear();

        for (int i = 0; i < count; i++)
        {
            IHearing listener = buffer[i].GetComponentInParent<IHearing>();

            // 같은 대상이 콜라이더 여러 개면 한 번만 알린다
            if (listener == null || !notified.Add(listener))
            {
                continue;
            }

            if (obstacleMask.value != 0 && IsBlocked(origin, buffer[i]))
            {
                continue;
            }

            listener.OnHeardNoise(origin, 0);
        }
    }

    private bool IsBlocked(Vector3 origin, Collider listenerCollider)
    {
        Vector3 target = listenerCollider.bounds.center;
        return Physics.Linecast(origin, target, obstacleMask, QueryTriggerInteraction.Ignore);
    }
}
