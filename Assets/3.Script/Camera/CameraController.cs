using UnityEngine;

// Cinemachine 카메라가 따라갈 타깃(followTarget)을 매 프레임 움직인다.
//  - 기본: 플레이어 위치를 따라간다
//  - 마우스 커서 방향으로 카메라를 조금 당긴다 (커서까지 거리에 비례, 최대치 클램프)
//
// 이 스크립트는 Cinemachine 에 의존하지 않는다.
// 씬 세팅:
//  1) 빈 오브젝트 "CameraTarget" 을 만들고 이 스크립트를 붙인다 (followTarget 은 비워두면 자기 자신)
//  2) CinemachineCamera 의 Tracking Target = CameraTarget
//  3) 카메라 각도/높이/거리는 CinemachineFollow 의 Follow Offset 으로 맞춘다
//     (기울어진 탑다운: 예) Offset (0, 18, -13), 카메라 X 회전 약 55도)
public class CameraController : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform followTarget;

    [Header("마우스 오프셋")]
    [Tooltip("플레이어에서 커서까지 거리의 몇 배만큼 카메라를 당길지")]
    [SerializeField] private float mouseInfluence = 0.35f;
    [Tooltip("마우스 오프셋 최대 이동 거리")]
    [SerializeField] private float maxOffset = 4f;

    [Header("추적")]
    [Tooltip("타깃이 목표 위치로 따라붙는 감쇠 시간 (작을수록 빠름)")]
    [SerializeField] private float followSmoothTime = 0.15f;

    private Vector3 followVelocity;

    private void Awake()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
        }

        if (followTarget == null)
        {
            followTarget = transform;
        }
    }

    private void LateUpdate()
    {
        if (player == null || followTarget == null)
        {
            return;
        }

        Vector3 basePosition = player.transform.position;
        Vector3 desired = basePosition;

        if (player.HasAimPoint)
        {
            Vector3 toAim = player.AimWorldPoint - basePosition;
            toAim.y = 0f;

            Vector3 offset = toAim * mouseInfluence;
            if (offset.sqrMagnitude > maxOffset * maxOffset)
            {
                offset = offset.normalized * maxOffset;
            }

            desired = basePosition + offset;
        }

        followTarget.position = Vector3.SmoothDamp(
            followTarget.position, desired, ref followVelocity, followSmoothTime);
    }
}
