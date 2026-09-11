using UnityEngine;
using UnityEngine.InputSystem;

// Cinemachine 카메라가 따라갈 타깃(followTarget)을 매 프레임 즉시 세팅한다.
//  - 기본: 플레이어(보간된 transform)를 따라간다
//  - 커서가 화면 중앙에서 벗어난 방향으로 타깃을 조금 당긴다
//    (화면 좌표 기준 → 카메라 위치와 무관 → 조준 레이캐스트와 피드백 루프 없음)
//  - 부드러움은 Cinemachine vcam 의 Follow 댐핑이 담당 (스크립트는 감쇠 안 함 → 이중 감쇠 없음)
//
// 이 스크립트는 Cinemachine 에 의존하지 않는다.
// 씬 세팅 (Cinemachine 3.x):
//  1) 빈 "CameraTarget" 오브젝트(최상위, 플레이어 자식 아님) + 이 스크립트
//  2) CinemachineCamera 의 Tracking Target = CameraTarget
//  3) CinemachineFollow 추가 → Follow Offset 으로 각도/거리 (예: (0, 13, -9)), Damping 0.2~0.4
//  4) vcam Transform 회전 X ~55°, yaw 0 고정 (Rotation 컴포넌트 안 붙임)
//  5) Main Camera CinemachineBrain → Update Method = Late Update
//  6) 플레이어 Rigidbody Interpolate ON
public class CameraController : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform followTarget;

    [Header("마우스 오프셋")]
    [Tooltip("커서가 화면 가장자리일 때 타깃이 당겨지는 최대 거리")]
    [SerializeField] private float maxMouseOffset = 4f;

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

        // 커서의 화면 중앙 대비 위치 (-1 ~ 1, 세로 기준 정규화)
        Vector2 mouse = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width, Screen.height) * 0.5f;

        float halfH = Screen.height * 0.5f;
        Vector2 norm = new Vector2(
            (mouse.x - Screen.width * 0.5f) / halfH,
            (mouse.y - halfH) / halfH);
        norm = Vector2.ClampMagnitude(norm, 1f);

        // 카메라 yaw 0 고정 → 화면 X = 월드 X, 화면 Y = 월드 Z
        Vector3 offset = new Vector3(norm.x, 0f, norm.y) * maxMouseOffset;

        // 즉시 세팅. 부드러움은 CinemachineFollow 댐핑이 담당.
        followTarget.position = player.transform.position + offset;
    }
}
