using System.Collections.Generic;
using UnityEngine;

// 플레이어가 걷기 / 달리기·구르기 / 사격할 때 소음을 발생시킨다.
// 반경 안의 IHearing 대상에게 OnHeardNoise(위치, 소음종류) 를 호출한다.
// 소음 종류: 0 = 걷기, 1 = 달리기/구르기, 2 = 총소리
[RequireComponent(typeof(PlayerController))]
public class PlayerNoise : MonoBehaviour
{
    private const int NoiseTypeWalk = 0;
    private const int NoiseTypeSprintOrDodge = 1;
    private const int NoiseTypeGunshot = 2;

    // 총소리 기즈모를 이 시간(초) 동안만 잠깐 보여준다 (총소리는 순간 이벤트라 상태가 없음)
    private const float GunshotGizmoDuration = 0.3f;

    [Header("걷기")]
    [SerializeField] private float walkNoiseInterval = 0.6f;
    [SerializeField] private float walkNoiseRadius = 4f;

    [Header("달리기 / 구르기")]
    [SerializeField] private float sprintNoiseInterval = 0.35f;
    [SerializeField] private float sprintNoiseRadius = 8f;
    [Tooltip("구르기 1회당 소음 반경")]
    [SerializeField] private float dodgeNoiseRadius = 10f;

    [Header("사격")]
    [Tooltip("무기 시스템에서 발사할 때 EmitGunshotNoise() 를 호출한다")]
    [SerializeField] private float gunshotNoiseRadius = 25f;

    [Header("레이어")]
    [Tooltip("소음을 듣는 대상 레이어 (적 등)")]
    [SerializeField] private LayerMask hearingMask;
    [Tooltip("설정하면 이 레이어에 가려진 대상은 못 들음 (벽이 소리 차단). 비우면 반경만 사용")]
    [SerializeField] private LayerMask obstacleMask;

    private PlayerController player;
    private PlayerInputHandler input;

    private float nextFootstepTime;
    private bool wasDodging;
    private float gunshotGizmoTimer; // 기즈모 표시용 - 총소리 후 잠깐만 켜둠

    private readonly Collider[] buffer = new Collider[32];
    private readonly HashSet<IHearing> notified = new HashSet<IHearing>();

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        TryGetComponent(out input);
    }

    private void Update()
    {
        if (gunshotGizmoTimer > 0f)
        {
            gunshotGizmoTimer -= Time.deltaTime;
        }

        // 구르기 시작 순간(상승 엣지)에 주기와 무관하게 한 번만 소음을 낸다
        bool isDodging = player.IsDodging;
        if (isDodging && !wasDodging)
        {
            EmitNoise(dodgeNoiseRadius, NoiseTypeSprintOrDodge);
        }
        wasDodging = isDodging;

        if (isDodging)
        {
            return; // 구르는 동안은 발소리 대신 구르기 소음만
        }

        UpdateFootstepNoise();
    }

    private void UpdateFootstepNoise()
    {
        if (input == null || Time.time < nextFootstepTime)
        {
            return;
        }

        bool moving = input.MoveInput.sqrMagnitude > 0.01f;
        if (!moving)
        {
            return;
        }

        if (player.IsSprinting)
        {
            nextFootstepTime = Time.time + sprintNoiseInterval;
            EmitNoise(sprintNoiseRadius, NoiseTypeSprintOrDodge);
        }
        else
        {
            nextFootstepTime = Time.time + walkNoiseInterval;
            EmitNoise(walkNoiseRadius, NoiseTypeWalk);
        }
    }

    // 무기 시스템에서 발사할 때 호출
    public void EmitGunshotNoise()
    {
        gunshotGizmoTimer = GunshotGizmoDuration;
        EmitNoise(gunshotNoiseRadius, NoiseTypeGunshot);
    }

    // 반경 안의 모든 IHearing 대상에게 소음을 알린다
    public void EmitNoise(float radius, int noiseType)
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

            listener.OnHeardNoise(origin, noiseType);
        }
    }

    private bool IsBlocked(Vector3 origin, Collider listenerCollider)
    {
        Vector3 target = listenerCollider.bounds.center;
        return Physics.Linecast(origin, target, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    // 재생 중 현재 상태에 해당하는 반경 하나만 보여준다 (여러 개를 한꺼번에 그리지 않는다)
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || player == null)
        {
            return; // 재생 중이 아니면 현재 상태를 알 수 없다
        }

        float radius;
        if (gunshotGizmoTimer > 0f)
        {
            radius = gunshotNoiseRadius;
        }
        else if (player.IsDodging)
        {
            radius = dodgeNoiseRadius;
        }
        else if (player.IsSprinting)
        {
            radius = sprintNoiseRadius;
        }
        else if (input != null && input.MoveInput.sqrMagnitude > 0.01f)
        {
            radius = walkNoiseRadius;
        }
        else
        {
            return; // 소음을 내지 않는 상태 - 표시 안 함
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
