using Cinemachine;
using UnityEngine;

// 총을 한 발 쏠 때마다 카메라를 짧게 흔든다 (Cinemachine Impulse).
//  - WeaponController.ShotFired 를 구독한다. 실제로 총알이 나간 발에만 흔들리고, 샷건 펠릿 수와는 무관하게 1회.
//  - 조준 반대 방향으로 살짝 밀리게 해서 반동 느낌을 낸다. 세기는 모든 무기 동일.
//  - follow target 을 직접 흔들면 vcam 의 Follow 댐핑이 흔들림을 뭉개므로, 댐핑이 끝난 최종 카메라에
//    얹히는 Impulse 를 쓴다.
//
// 세팅: 플레이어(PlayerController 와 같은 오브젝트)에 이 컴포넌트만 붙이면 된다.
//  - CinemachineImpulseSource 가 없으면 여기서 추가해서 설정한다.
//  - vcam 에 CinemachineImpulseListener 가 없으면 여기서 추가해서 설정한다.
[RequireComponent(typeof(PlayerController))]
public class WeaponCameraShake : MonoBehaviour
{
    [Tooltip("흔들림 세기 (카메라가 밀리는 거리, 대략 월드 단위)")]
    [SerializeField] private float shakeForce = 0.15f;
    [Tooltip("한 번 흔들리는 시간(초). 연사 무기는 짧아야 흔들림이 뭉개지지 않는다")]
    [SerializeField] private float shakeDuration = 0.12f;
    [Tooltip("흔들림을 받을 vcam. 비우면 씬에서 찾는다")]
    [SerializeField] private CinemachineVirtualCamera vcam;

    private PlayerController player;
    private WeaponController weapon;
    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        TryGetComponent(out weapon);

        if (!TryGetComponent(out impulseSource))
        {
            impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
        }

        // 런타임에 AddComponent 하면 에디터 Reset 기본값이 안 들어가서(Legacy 타입 = 신호 없음) 흔들리지 않는다.
        // 이미 붙어 있든 새로 붙였든 여기서 필요한 값을 맞춰 둔다.
        CinemachineImpulseDefinition definition = impulseSource.m_ImpulseDefinition;
        definition.m_ImpulseChannel = 1;
        definition.m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        definition.m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        definition.m_ImpulseDuration = shakeDuration;

        if (vcam == null)
        {
            vcam = FindAnyObjectByType<CinemachineVirtualCamera>();
        }

        EnsureListener();
    }

    private void OnEnable()
    {
        if (weapon != null)
        {
            weapon.ShotFired += HandleShotFired;
        }
    }

    private void OnDisable()
    {
        if (weapon != null)
        {
            weapon.ShotFired -= HandleShotFired;
        }
    }

    // vcam 에 Listener 가 없으면 추가한다. 런타임 AddComponent 는 Reset 기본값이 안 들어가서(채널 0, 게인 0)
    // 새로 붙인 경우에만 값을 직접 채운다. 에디터에서 미리 붙여 둔 Listener 는 그 설정을 그대로 쓴다.
    private void EnsureListener()
    {
        if (vcam != null && !vcam.TryGetComponent(out CinemachineImpulseListener listener))
        {
            listener = vcam.gameObject.AddComponent<CinemachineImpulseListener>();
            listener.m_ApplyAfter = CinemachineCore.Stage.Noise;
            listener.m_ChannelMask = 1;
            listener.m_Gain = 1f;
            listener.m_Use2DDistance = false;
            listener.m_UseCameraSpace = true;
            listener.m_ReactionSettings = new CinemachineImpulseListener.ImpulseReaction
            {
                m_AmplitudeGain = 1f,
                m_FrequencyGain = 1f,
                m_Duration = 1f
            };
        }
    }

    private void HandleShotFired()
    {
        // 조준 반대 방향(월드 XZ)을 카메라 기준 방향으로 바꿔서 넘긴다. Listener 가 카메라 공간(Use Camera Space)으로
        // 받으므로, 화면에서 보이는 방향 그대로 반동이 나간다 (카메라가 기울어져 있어도 앞뒤로 줌되지 않음).
        Vector3 kickDirection = transform.position - player.AimWorldPoint;
        kickDirection.y = 0f;
        if (kickDirection.sqrMagnitude < 0.0001f)
        {
            kickDirection = -transform.forward;
        }

        Vector3 cameraSpaceDirection = new Vector3(kickDirection.x, kickDirection.z, 0f);
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraSpaceDirection = mainCamera.transform.InverseTransformDirection(kickDirection);
            cameraSpaceDirection.z = 0f;
        }

        if (cameraSpaceDirection.sqrMagnitude > 0.0001f)
        {
            impulseSource.GenerateImpulse(cameraSpaceDirection.normalized * shakeForce);
        }
    }
}
