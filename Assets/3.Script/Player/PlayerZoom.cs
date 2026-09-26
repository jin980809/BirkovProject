using Cinemachine;
using UnityEngine;

// 우클릭 홀드 = 조준 줌. 스프린트 중이 아닐 때만 가능하다 (가만히 서 있거나 걷는 중엔 OK).
// 홀드 중에는: 카메라 FOV 축소, 이동속도 배율 적용, 무기 최대 퍼짐(반동) 배율 적용, 크로스헤어 중앙점 색 변경.
//
// 시작/취소를 별도 이벤트로 다루지 않고 매 프레임 조건(우클릭 홀드 && 스프린트 아님 && ...)을
// 그대로 다시 계산한다. 그래서 줌 중에 스프린트를 시작하면 자동으로 줌이 풀리고,
// 스프린트를 멈췄을 때 우클릭을 계속 누르고 있었다면 자동으로 다시 줌 상태로 돌아간다.
[RequireComponent(typeof(PlayerController))]
public class PlayerZoom : MonoBehaviour
{
    [Header("이동속도")]
    [Tooltip("줌 중 이동속도 배율")]
    [SerializeField, Range(0.1f, 1f)] private float zoomMoveSpeedMultiplier = 0.5f;

    [Header("반동 / 퍼짐")]
    [Tooltip("줌 중 무기 최대 퍼짐(maxSpread) 배율 - 작을수록 정확해짐")]
    [SerializeField, Range(0.1f, 1f)] private float zoomSpreadMultiplier = 0.5f;

    [Header("카메라 줌")]
    [SerializeField] private CinemachineVirtualCamera vcam;
    [SerializeField] private float zoomedFov = 40f;
    [Tooltip("줌이 아닐 때의 기본 FOV. 비워두면(0) 시작 시 vcam 의 현재 값을 그대로 쓴다")]
    [SerializeField] private float normalFov;
    [SerializeField] private float fovLerpSharpness = 10f;

    private PlayerController player;
    private PlayerInputHandler input;
    private WeaponController weapon;
    private CrosshairUI crosshair;

    public bool IsZoomed { get; private set; }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        TryGetComponent(out input);
        TryGetComponent(out weapon);
        crosshair = (PersistentUiRoot.Find<CrosshairUI>() ?? FindAnyObjectByType<CrosshairUI>(FindObjectsInactive.Include)); // 꺼져 있어도 찾는다 (인벤토리/사망 패널로 꺼진 경우)

        if (vcam != null && normalFov <= 0f)
        {
            normalFov = vcam.m_Lens.FieldOfView;
        }
    }

    private void Update()
    {
        UpdateZoomState();
        UpdateCameraFov();
    }

    private void UpdateZoomState()
    {
        if (input == null || player == null)
        {
            return;
        }

        bool wantsZoom = input.ZoomHeld;
        bool canZoom = wantsZoom && !player.IsSprinting && !player.IsDodging && !player.IsControlLocked && !player.IsUsingItem && !player.IsReloading;

        if (canZoom == IsZoomed)
        {
            return;
        }

        IsZoomed = canZoom;

        player.SetSpeedMultiplier(IsZoomed ? zoomMoveSpeedMultiplier : 1f);

        if (weapon != null)
        {
            weapon.SetAimSpreadMultiplier(IsZoomed ? zoomSpreadMultiplier : 1f);
        }

        if (crosshair != null)
        {
            crosshair.SetZoomVisual(IsZoomed);
        }
    }

    private void UpdateCameraFov()
    {
        if (vcam == null)
        {
            return;
        }

        float targetFov = IsZoomed ? zoomedFov : normalFov;
        LensSettings lens = vcam.m_Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, 1f - Mathf.Exp(-fovLerpSharpness * Time.deltaTime));
        vcam.m_Lens = lens;
    }
}
