using System;
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

    // 무기별 줌 FOV. 기본 FOV 보다 큰 값을 주면 조준할 때 오히려 화면이 넓어진다(줌아웃) -
    // 스나이퍼처럼 멀리 보는 무기에 쓴다. ItemData.csv 기준 10001 기관권총 / 10002 샷건 /
    // 10003 돌격소총 / 10004 스나이퍼.
    [Serializable]
    public struct WeaponZoomFov
    {
        public int weaponItemId;
        public float zoomedFov;

        [Tooltip("조준할 때 커서 주변만 보이게 한다 (스나이퍼 스코프)")]
        public bool useScope;

        [Tooltip("구멍 반지름 - 화면 높이 기준 비율. 0 이면 SniperScopeOverlay 의 기본값을 쓴다")]
        public float scopeRadius;
    }

    [Header("카메라 줌")]
    [SerializeField] private CinemachineVirtualCamera vcam;
    [SerializeField] private float zoomedFov = 40f;
    [Tooltip("여기에 넣은 무기는 위의 Zoomed Fov 대신 이 값을 쓴다 (기본 FOV 보다 크면 줌아웃)")]
    [SerializeField] private WeaponZoomFov[] weaponZoomFovs;
    [Tooltip("줌이 아닐 때의 기본 FOV. 비워두면(0) 시작 시 vcam 의 현재 값을 그대로 쓴다")]
    [SerializeField] private float normalFov;
    [SerializeField] private float fovLerpSharpness = 10f;

    private PlayerController player;
    private PlayerInputHandler input;
    private WeaponController weapon;
    private CrosshairUI crosshair;
    private SniperScopeOverlay scopeOverlay;

    public bool IsZoomed { get; private set; }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        TryGetComponent(out input);
        TryGetComponent(out weapon);
        crosshair = (PersistentUiRoot.Find<CrosshairUI>() ?? FindAnyObjectByType<CrosshairUI>(FindObjectsInactive.Include)); // 꺼져 있어도 찾는다 (인벤토리/사망 패널로 꺼진 경우)

        // 씬마다 연결하는 것을 잊기 쉬워서, 비어 있으면 그 씬의 가상 카메라를 찾는다.
        // (연결하지 않으면 줌 FOV 변화가 아예 일어나지 않는다)
        if (vcam == null)
        {
            vcam = FindAnyObjectByType<CinemachineVirtualCamera>();

            if (vcam == null)
            {
                Debug.LogWarning("PlayerZoom: 씬에서 CinemachineVirtualCamera 를 찾지 못해 줌 FOV 가 동작하지 않습니다.", this);
            }
        }

        if (vcam != null && normalFov <= 0f)
        {
            normalFov = vcam.m_Lens.FieldOfView;
        }
    }

    private void Update()
    {
        UpdateZoomState();

        // 스코프는 매 프레임 다시 판단한다. 줌 상태가 바뀌는 순간에만 갱신하면, 줌을 유지한 채
        // 무기를 바꿨을 때(퀵슬롯 1/2) 스코프가 그대로 남는다. FOV 도 원래 매 프레임 갱신한다.
        UpdateScopeOverlay();
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

    // 스코프 설정이 있는 무기로 조준하는 동안에만 커서 주변만 보이는 오버레이를 켠다.
    // 줌이 풀리는 모든 경우(우클릭 해제 / 스프린트 / 구르기 / 재장전 / UI 열림 / 사망)에 자동으로 꺼진다.
    private void UpdateScopeOverlay()
    {
        EnsureScopeOverlay();

        if (scopeOverlay == null)
        {
            return;
        }

        if (IsZoomed && TryGetWeaponZoom(out WeaponZoomFov setting) && setting.useScope)
        {
            scopeOverlay.Show(setting.scopeRadius);
        }
        else
        {
            scopeOverlay.Hide();
        }
    }

    // 오버레이는 씬을 넘어 유지되는 캔버스 안에 있다 (크로스헤어와 같은 방식으로 찾는다).
    private void EnsureScopeOverlay()
    {
        if (scopeOverlay == null)
        {
            scopeOverlay = PersistentUiRoot.Find<SniperScopeOverlay>();
        }
    }

    // 지금 든 무기에 지정한 줌 설정을 찾는다 (없으면 false).
    private bool TryGetWeaponZoom(out WeaponZoomFov setting)
    {
        setting = default;

        if (weapon == null || !weapon.HasWeaponEquipped)
        {
            return false;
        }

        int itemId = weapon.EquippedWeaponItemId;

        for (int i = 0; i < weaponZoomFovs.Length; i++)
        {
            if (weaponZoomFovs[i].weaponItemId == itemId)
            {
                setting = weaponZoomFovs[i];
                return true;
            }
        }

        return false;
    }

    private void UpdateCameraFov()
    {
        if (vcam == null)
        {
            return;
        }

        float targetFov = normalFov;

        if (IsZoomed)
        {
            targetFov = GetZoomedFov();
        }

        LensSettings lens = vcam.m_Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, 1f - Mathf.Exp(-fovLerpSharpness * Time.deltaTime));
        vcam.m_Lens = lens;
    }

    // 지금 든 무기에 따로 지정한 줌 FOV 가 있으면 그것을, 없으면 공통 zoomedFov 를 쓴다.
    private float GetZoomedFov()
    {
        if (TryGetWeaponZoom(out WeaponZoomFov setting) && setting.zoomedFov > 0f)
        {
            return setting.zoomedFov;
        }

        return zoomedFov;
    }
}
