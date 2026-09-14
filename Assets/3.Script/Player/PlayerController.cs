using System;
using UnityEngine;

// 플레이어 이동 / 회전 / 구르기 처리
// 입력은 PlayerInputHandler 에서 받아온다 (같은 GameObject 에 붙어 있어야 함).
//
// 회전 규칙:
//  - 기본: 마우스 포인터 방향으로 회전
//  - 달리는 중(Shift + 이동): 키보드 입력 방향으로 회전
//  - Shift 를 떼면 다시 마우스 방향 회전
//  - 달리는 중에는 사격 불가
//
// 조준 계산(UpdateAimPoint/UpdateFacing)은 Update 가 아니라 LateUpdate 에서 한다.
// 카메라(CameraController → CinemachineBrain)가 이번 프레임에 다 움직인 "뒤"에
// 그 최종 카메라 위치로 화면→월드 레이를 쏴야 하기 때문이다.
// (Update 에서 계산하면 아직 이번 프레임 카메라 이동이 반영 안 된, 한 프레임 뒤처진
//  카메라 위치를 쓰게 되고, 카메라가 지면에 얕은 각도로 기울어져 있을수록
//  그 위치 오차가 조준점에서 크게 증폭된다.)
// 실행 순서는 [DefaultExecutionOrder] 로 강제한다: CameraController(-100) → CinemachineBrain(기본 0)
// → PlayerController(+100).
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    // 회전 기준 모드
    private enum RotationMode
    {
        Move, // 이동 방향을 바라본다
        Aim,  // 마우스 포인터를 바라본다
    }

    [Header("이동")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;

    [Header("회전")]
    [SerializeField] private float rotationSpeed = 720f; // 초당 회전 각도(deg)

    [Header("구르기")]
    [SerializeField] private float dodgeSpeed = 10f;
    [SerializeField] private float dodgeDuration = 0.35f;
    [SerializeField] private float dodgeCooldown = 0.8f;

    [Header("조준")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private LayerMask groundMask = ~0;
    [Tooltip("총구 높이(WeaponController의 firePoint 로컬 Y와 맞춰야 함). 조준점을 이 높이의 수평면에서 계산한다.")]
    [SerializeField] private float aimHeightOffset = 0.5f;

    [Header("애니메이션")]
    [SerializeField] private Animator animator;
    [Tooltip("블렌드 파라미터 감쇠 시간 (클수록 부드럽고 반응 느림)")]
    [SerializeField] private float animDamp = 0.12f;

    private Rigidbody rb;
    private PlayerInputHandler input;
    private PlayerVitals vitals; // 선택 - 있으면 스테미나로 달리기/구르기 게이트
    private WeaponController weapon; // 선택 - 있으면 발사/재장전/무기교체를 위임
    private PlayerInteraction interaction; // 선택 - 있으면 상호작용 중 이동/회전/사격을 막음

    // 회전 상태
    private Vector3 facingDirection = Vector3.forward;

    // 조준점 (마우스 커서가 가리키는 월드 좌표) - 카메라/무기 시스템이 참조
    private Vector3 aimWorldPoint;
    private bool hasAimPoint;

    // 조준 레이가 실제 바닥 콜라이더 범위를 벗어났을 때 쓰는 평면 높이.
    // 플레이어 피벗(transform.position.y)은 콜라이더 중심이라 실제 바닥보다 위에 있으므로
    // 그대로 쓰면 안 되고, 시작 시 실제 바닥을 한 번 레이캐스트해서 구한 값을 쓴다.
    private float groundPlaneY;

    // 구르기 상태
    private bool isDodging;
    private float dodgeEndTime;
    private float dodgeReadyTime;
    private Vector3 dodgeDirection;

    public bool IsDodging
    {
        get { return isDodging; }
    }

    // Shift 를 누른 채로 실제 이동 중 + 스테미나 여유가 있을 때만 달리기로 친다
    public bool IsSprinting
    {
        get
        {
            bool wantSprint = input.SprintHeld && input.MoveInput.sqrMagnitude > 0.01f;
            return wantSprint && (vitals == null || vitals.CanSprint);
        }
    }

    // 상호작용 중인지 (상호작용 시스템이 없으면 항상 false)
    public bool IsInteracting
    {
        get { return interaction != null && interaction.IsInteracting; }
    }

    // 상자 UI 등 외부 UI 가 SetMovementLocked 로 잠근 상태
    private bool movementLocked;

    // 이동/회전/사격이 전부 막혀야 하는 상태 (상호작용 중이거나, 외부 UI 가 잠갔거나)
    public bool IsControlLocked
    {
        get { return IsInteracting || movementLocked; }
    }

    // IsControlLocked 가 실제로 바뀌는 순간(예: 상호작용 시작/취소, 상자 UI 열기/닫기)에만 발생한다.
    // 이동/회전은 이미 IsControlLocked 를 직접 읽으니 구독할 필요 없고, PlayerVision 처럼
    // "잠기는 순간에 한 번 반응해야 하는" 외부 시스템이 구독한다 (OnEnable/OnDisable 로 직접 구독).
    public event Action<bool> ControlLockChanged;
    private bool wasControlLocked;

    // 상자 UI 등에서 호출한다 - 열려 있는 동안 이동/회전/사격을 막는다
    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
    }

    // 달리는 중 / 구르는 중 / 상호작용 중 / 외부 UI 로 잠긴 중에는 사격 불가 (무기 시스템에서 이 값을 확인)
    public bool CanFire
    {
        get { return !IsSprinting && !isDodging && !IsControlLocked; }
    }

    // 마우스 커서가 가리키는 월드 좌표
    public Vector3 AimWorldPoint
    {
        get { return aimWorldPoint; }
    }

    public bool HasAimPoint
    {
        get { return hasAimPoint; }
    }

    // 무기 장착 여부 (무기 시스템에서 SetArmed 로 설정)
    private bool isArmed;

    public void SetArmed(bool value)
    {
        isArmed = value;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // 넘어짐(X/Z 회전)만 막고 Y축 회전은 MoveRotation 으로 직접 돌린다
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        TryGetComponent(out vitals);
        TryGetComponent(out weapon);
        TryGetComponent(out interaction);

        facingDirection = transform.forward;
        aimWorldPoint = transform.position + transform.forward;
        groundPlaneY = FindGroundPlaneY();

        if (!TryGetComponent(out input))
        {
            Debug.LogError("PlayerController: 같은 오브젝트에 PlayerInputHandler 가 필요합니다.", this);
        }
    }

    private void OnEnable()
    {
        if (input == null)
        {
            return;
        }

        input.FirePressed += HandleFireStart;
        input.FireReleased += HandleFireStop;
        input.DodgePressed += HandleDodge;
        input.ReloadPressed += HandleReload;
        input.InteractPressed += HandleInteract;
        input.CancelPressed += HandleCancel;
        input.InventoryToggled += HandleInventory;
        input.WeaponSelected += HandleWeaponSelected;
        input.QuickSlotUsed += HandleQuickSlot;
    }

    private void OnDisable()
    {
        if (input == null)
        {
            return;
        }

        input.FirePressed -= HandleFireStart;
        input.FireReleased -= HandleFireStop;
        input.DodgePressed -= HandleDodge;
        input.ReloadPressed -= HandleReload;
        input.InteractPressed -= HandleInteract;
        input.CancelPressed -= HandleCancel;
        input.InventoryToggled -= HandleInventory;
        input.WeaponSelected -= HandleWeaponSelected;
        input.QuickSlotUsed -= HandleQuickSlot;
    }

    private void Update()
    {
        // 트랜스폼/Rigidbody 는 FixedUpdate 에서만 건드린다.
        UpdateAnimator();

        if (vitals != null)
        {
            vitals.SetSprinting(IsSprinting); // 스테미나 소모/회복 판정용
        }

        bool locked = IsControlLocked;
        if (locked != wasControlLocked)
        {
            wasControlLocked = locked;
            ControlLockChanged?.Invoke(locked);
        }
    }

    private void LateUpdate()
    {
        // 카메라가 이번 프레임에 다 움직인 뒤에 그 최종 위치로 조준 레이를 계산한다.
        UpdateAimPoint();
        UpdateFacing();
    }

    private void FixedUpdate()
    {
        // Y축 회전은 전적으로 MoveRotation 이 담당한다.
        // 벽 접촉 등에서 생긴 물리 각속도가 누적돼 회전이 이상해지는 것을 막는다.
        rb.angularVelocity = Vector3.zero;

        if (vitals != null && vitals.IsDead)
        {
            rb.linearVelocity = new Vector3(0f, VerticalVelocity(), 0f);
            return; // TODO: 사망 처리 (입력 차단, 사망 애니 등)
        }

        if (IsControlLocked)
        {
            // 상호작용 중이거나 외부 UI(상자 등)가 잠근 동안에는 이동/회전 다 멈춘다
            // (구르기 중 진입은 없음 - 구르는 동안엔 상호작용 못 함)
            rb.linearVelocity = new Vector3(0f, VerticalVelocity(), 0f);
            return;
        }

        if (isDodging)
        {
            UpdateDodge();
        }
        else
        {
            UpdateMovement();
        }

        ApplyRotation();
    }

    // ---------- 이동 ----------

    // 충돌로 생긴 위쪽 속도는 버리고, 중력에 의한 낙하만 유지한다
    // (캡슐끼리 비비면 분리 방향에 위쪽 성분이 생겨 플레이어가 떠오르는 것 방지)
    private float VerticalVelocity()
    {
        return Mathf.Min(rb.linearVelocity.y, 0f);
    }

    private void UpdateMovement()
    {
        Vector3 move = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        float speed = IsSprinting ? sprintSpeed : moveSpeed;

        Vector3 velocity = move * speed;
        velocity.y = VerticalVelocity();
        rb.linearVelocity = velocity;
    }

    // ---------- 구르기 ----------

    private void HandleDodge()
    {
        if (isDodging || Time.time < dodgeReadyTime)
        {
            return;
        }

        if (vitals != null && !vitals.TryConsumeDodgeStamina())
        {
            return; // 스테미나 부족
        }

        // 이동 입력이 있으면 그 방향, 없으면 현재 바라보는 방향으로 구른다
        Vector3 move = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
        dodgeDirection = move.sqrMagnitude > 0.01f ? move.normalized : facingDirection;

        isDodging = true;
        dodgeEndTime = Time.time + dodgeDuration;
        dodgeReadyTime = Time.time + dodgeCooldown;

        // TODO: 구르기 애니메이션, 무적 프레임
    }

    private void UpdateDodge()
    {
        if (Time.time >= dodgeEndTime)
        {
            isDodging = false;
            // 구르기 끝나면 수평 속도 제거 (미끄러짐 방지)
            rb.linearVelocity = new Vector3(0f, VerticalVelocity(), 0f);
            return;
        }

        Vector3 velocity = dodgeDirection * dodgeSpeed;
        velocity.y = VerticalVelocity();
        rb.linearVelocity = velocity;
    }

    // ---------- 회전 ----------

    private void UpdateAimPoint()
    {
        Vector3 point;
        if (TryGetAimPoint(out point))
        {
            aimWorldPoint = point;
            hasAimPoint = true;
        }
        else
        {
            hasAimPoint = false;
        }
    }

    private void UpdateFacing()
    {
        if (isDodging)
        {
            return; // 구르는 동안에는 방향 고정
        }

        if (GetRotationMode() == RotationMode.Aim)
        {
            if (hasAimPoint)
            {
                Vector3 toAim = aimWorldPoint - transform.position;
                toAim.y = 0f;
                if (toAim.sqrMagnitude > 0.01f)
                {
                    facingDirection = toAim.normalized;
                }
            }
        }
        else
        {
            Vector3 move = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
            if (move.sqrMagnitude > 0.01f)
            {
                facingDirection = move.normalized;
            }
        }
    }

    // 달리는 중이면 이동 방향, 아니면 마우스 방향
    private RotationMode GetRotationMode()
    {
        return IsSprinting ? RotationMode.Move : RotationMode.Aim;
    }

    private void ApplyRotation()
    {
        if (facingDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion target = Quaternion.LookRotation(facingDirection, Vector3.up);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, target, rotationSpeed * Time.fixedDeltaTime));
    }

    // ---------- 애니메이션 ----------

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        // 파라미터는 "측정된 물리 값" 이 아니라 "입력 의도" 로 만든다.
        // 측정 속도는 정지 상태에서도 0이 아니라(바닥 접촉 노이즈) 파라미터가 계속 흔들린다.
        // 이동 입력을 캐릭터가 바라보는 방향 기준으로 변환한다.
        // 걷기(조준 모드)에서는 캐릭터가 마우스를 보고 있으므로
        //   마우스 쪽으로 걸으면 +Y(FWD), 반대면 -Y(BWD), 옆이면 ±X 가 된다.
        Vector3 worldMove = IsControlLocked
            ? Vector3.zero // 잠긴 동안에는 입력이 있어도 실제로는 멈춰 있으므로 애니메이션도 정지 취급
            : new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
        bool moving = worldMove.sqrMagnitude > 0.01f;

        Vector3 localMove = transform.InverseTransformDirection(worldMove);

        float targetSpeed = 0f;
        if (moving)
        {
            targetSpeed = IsSprinting ? 1f : moveSpeed / Mathf.Max(sprintSpeed, 0.01f);
        }

        // 정지 중엔 감쇠 없이 0으로 딱 고정 (감쇠는 목표에 정확히 도달 못 해서 잔떨림이 남는다)
        float damp = moving ? animDamp : 0f;

        animator.SetFloat("MoveX", localMove.x, damp, Time.deltaTime);
        animator.SetFloat("MoveY", localMove.z, damp, Time.deltaTime);
        animator.SetFloat("Speed", targetSpeed, damp, Time.deltaTime);
        animator.SetBool("IsArmed", isArmed);
    }

    private bool TryGetAimPoint(out Vector3 point)
    {
        point = Vector3.zero;

        if (aimCamera == null)
        {
            return false;
        }

        Ray ray = aimCamera.ScreenPointToRay(input.LookScreenPosition);

        // 조준점은 "실제 바닥 높이"가 아니라 "총구 높이"의 수평면에서 계산한다.
        // 총알은 총구 높이의 수평면으로만 날아가므로(baseDirection.y = 0), 조준점도 같은 높이여야
        // 화면상 커서 위치와 실제 탄착 방향이 일치한다. 두 높이가 다르면 카메라가 비스듬할수록
        // (수직 90도가 아닐수록) 원근 때문에 화면에서 어긋나 보인다.
        Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, groundPlaneY + aimHeightOffset, 0f));
        float enter;
        if (aimPlane.Raycast(ray, out enter))
        {
            point = ray.GetPoint(enter);
            return true;
        }

        return false;
    }

    // 실제 바닥 콜라이더의 Y 값을 한 번 구해서 캐싱한다.
    // 플레이어 피벗(transform.position.y)은 콜라이더 중심이라 바닥보다 위에 있어서 그대로 쓰면 안 된다.
    private float FindGroundPlaneY()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out hit, 20f, groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }

        return transform.position.y; // 못 찾으면 기존 방식으로 대체
    }

    // ---------- 다른 시스템 연결 지점 ----------

    private void HandleFireStart()
    {
        if (!CanFire || weapon == null)
        {
            return; // 달리는 중 / 구르는 중에는 사격 불가
        }

        weapon.TryFire();
    }

    private void HandleFireStop()
    {
        if (weapon != null)
        {
            weapon.StopFiring();
        }
    }

    private void HandleReload()
    {
        if (weapon != null)
        {
            weapon.TryReload();
        }
    }

    private void HandleInteract()
    {
        if (isDodging || interaction == null)
        {
            return; // 구르는 중에는 상호작용 시작 불가
        }

        interaction.TryStartInteract();
    }

    private void HandleCancel()
    {
        if (interaction != null)
        {
            interaction.CancelInteract(); // 진행 중인 상호작용을 취소하고 원상태로 되돌린다 (ESC)
        }
    }

    private void HandleInventory()
    {
        // TODO: 인벤토리 열기 / 닫기
    }

    private void HandleWeaponSelected(int slotIndex)
    {
        if (weapon != null)
        {
            weapon.EquipSlot(slotIndex);
        }
    }

    private void HandleQuickSlot(int slotIndex)
    {
        // TODO: 소모성 아이템 사용 (0~2 = 3, 4, 5번 키)
    }
}
