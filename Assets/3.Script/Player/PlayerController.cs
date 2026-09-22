using System;
using System.Collections.Generic;
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
    [Tooltip("총구가 몸 옆에 있어서 생기는 총열-총알 방향 어긋남을 몸 회전으로 보정한다 (GetMuzzleAlignedFacing 참고)")]
    [SerializeField] private bool compensateMuzzleOffset = true;

    // 커서가 (총구 옆 거리 + 이 값) 보다 가까우면 보정하지 않는다 - asin 이 발산해 몸이 크게 튀는 것 방지
    private const float MinAimDistanceForMuzzleAlign = 0.3f;

    [Header("구르기")]
    [SerializeField] private float dodgeSpeed = 10f;
    [SerializeField] private float dodgeDuration = 0.35f;
    [SerializeField] private float dodgeCooldown = 0.8f;

    [Header("조준")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private LayerMask groundMask = ~0;
    [Tooltip("캐릭터 회전/시야/카메라용 조준점을 계산하는 수평면 높이(바닥 기준). 총알 방향은 발사 순간의 실제 총구(FirePoint) 높이로 따로 계산하므로 여기와 맞출 필요 없다.")]
    [SerializeField] private float aimHeightOffset = 0.5f;

    [Header("애니메이션")]
    [SerializeField] private Animator animator;
    [Tooltip("블렌드 파라미터 감쇠 시간 (클수록 부드럽고 반응 느림)")]
    [SerializeField] private float animDamp = 0.12f;

    [Header("발사 애니메이션 (Fire 레이어)")]
    [Tooltip("단발 발사 클립. 비우면 Animator 에서 이름(ShootSingleshotOneWeapon)으로 찾는다")]
    [SerializeField] private AnimationClip singleShotClip;
    [Tooltip("연사 무기로 쏘는 동안 반복 재생되는 발사 모션의 배속 (1 = 원본 속도). 발사 속도(fireRate)와는 무관하다")]
    [SerializeField, Min(0.01f)] private float autoFireAnimSpeed = 1f;

    private const string SingleShotClipName = "ShootSingleshotOneWeapon";
    private readonly int FireSingleStateHash = Animator.StringToHash("FireSingle");
    private readonly int FireAutoStateHash = Animator.StringToHash("FireAuto");
    private readonly int FireSpeedHash = Animator.StringToHash("FireSpeed");
    private readonly int DodgeTriggerHash = Animator.StringToHash("Dodge");

    private int fireLayerIndex = -1; // 상체 전용 발사 포즈 레이어 인덱스 (Awake 에서 이름으로 찾음, 없으면 -1)
    private float singleShotClipLength = 1f;
    private float singleShotVisibleUntil; // 단발 무기 - 이 시각까지 발사 포즈(클립 1회분)를 보여준다
    private bool wasAutoFiring;

    private Rigidbody rb;
    private PlayerInputHandler input;
    private PlayerVitals vitals; // 선택 - 있으면 스테미나로 달리기/구르기 게이트
    private WeaponController weapon; // 선택 - 있으면 발사/재장전/무기교체를 위임
    private PlayerInteraction interaction; // 선택 - 있으면 상호작용 중 이동/회전/사격을 막음
    private ItemUseController itemUse; // 선택 - 있으면 아이템 사용 중 달리기/구르기/사격/상호작용만 막음 (이동/회전/시야는 그대로)
    private PlayerExtraction extraction; // 선택 - 있으면 귀환(B) 게이지가 도는 동안 사격/구르기/상호작용/무기교체를 막음

    // 회전 상태
    private Vector3 facingDirection = Vector3.forward;

    // 조준점 (마우스 커서가 가리키는 월드 좌표) - 카메라/무기 시스템이 참조
    private Vector3 aimWorldPoint;
    private bool hasAimPoint;

    // 조준 레이가 실제 바닥 콜라이더 범위를 벗어났을 때 쓰는 평면 높이.
    // 플레이어 피벗(transform.position.y)은 콜라이더 중심이라 실제 바닥보다 위에 있으므로
    // 그대로 쓰면 안 되고, 시작 시 실제 바닥을 한 번 레이캐스트해서 구한 값을 쓴다.
    private float groundPlaneY;

    // 마지막 LateUpdate 에서 쓴 조준 레이 (TryGetAimPointAtHeight 가 총구 높이로 다시 교차시킬 때 쓴다)
    private Ray aimRay;
    private bool hasAimRay;

    // 달리기 중 방향 전환 틈(이동 입력이 잠깐 0 이 되는 구간)을 메우는 시간. 이 시간 안에는 계속 달리는 중으로 본다.
    private const float SprintInputGraceSeconds = 0.15f;
    private float lastMoveInputTime = -999f;

    // 지금 이 플레이어가 붙어있는 엄폐물들의 콜라이더 모음 (막는 것 + 감지용 트리거 전부, CoverObject 가 채운다).
    // WeaponController 가 발사할 때 이걸 읽어서 그 총알만 이 콜라이더들을 무시하게 만든다.
    private readonly HashSet<Collider> attachedCoverColliders = new HashSet<Collider>();

    public IReadOnlyCollection<Collider> AttachedCoverColliders
    {
        get { return attachedCoverColliders; }
    }

    // CoverObject.OnTriggerEnter/Exit 가 붙고 떨어질 때 부른다
    public void AttachCover(Collider[] colliders)
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            attachedCoverColliders.Add(colliders[i]);
        }
    }

    public void DetachCover(Collider[] colliders)
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            attachedCoverColliders.Remove(colliders[i]);
        }
    }

    // 구르기 상태
    private bool isDodging;
    private float dodgeEndTime;
    private float dodgeReadyTime;
    private Vector3 dodgeDirection;

    public bool IsDodging
    {
        get { return isDodging; }
    }

    // Shift 를 누른 채로 실제 이동 중 + 스테미나 여유가 있을 때만 달리기로 친다.
    // 아이템 사용/재장전 중에는 달리기 자체가 금지된다 (걷기는 가능).
    public bool IsSprinting
    {
        get
        {
            // 이동 입력이 "방금 전까지" 있었으면 달리는 중으로 본다.
            // 달리면서 방향을 바꿀 때(W 를 떼고 A 를 누르는 사이) 이동 입력이 한두 프레임 0 이 되는데,
            // 그 순간만 달리기가 풀리면 발사 버튼을 누르고 있던 경우 그 틈에 총이 한 발 나가버린다.
            // (회전 모드도 같이 튀어서 몸이 마우스 쪽으로 홱 돌아간다.)
            bool movingRecently = input.MoveInput.sqrMagnitude > 0.01f || Time.time - lastMoveInputTime <= SprintInputGraceSeconds;
            bool wantSprint = input.SprintHeld && movingRecently;
            return wantSprint && !IsUsingItem && !IsReloading && (vitals == null || vitals.CanSprint);
        }
    }

    // 상호작용 중인지 (상호작용 시스템이 없으면 항상 false)
    public bool IsInteracting
    {
        get { return interaction != null && interaction.IsInteracting; }
    }

    // 아이템 사용(게이지) 중인지 (그 시스템이 없으면 항상 false).
    // 상자 등의 IsControlLocked 와 달리 이동/회전/시야는 막지 않는다 - 달리기/구르기/사격/상호작용/
    // 인벤토리 열기만 개별적으로 막는다 (CanFire, HandleDodge, HandleInteract, PlayerInventoryToggle 참고).
    public bool IsUsingItem
    {
        get { return itemUse != null && itemUse.IsUsing; }
    }

    // 귀환(B) 게이지가 도는 중인지. 제자리에서만 가능하므로 이동 입력이 들어오면 PlayerExtraction 이 알아서 취소한다.
    public bool IsExtracting
    {
        get { return extraction != null && extraction.IsExtracting; }
    }

    // 재장전 중인지 (그 시스템이 없으면 항상 false). IsUsingItem 과 완전히 동일한 규칙 -
    // 이동/회전/시야는 그대로 두고, 달리기/구르기/사격/상호작용/무기교체/인벤토리 열기/다른
    // 퀵슬롯 사용만 개별적으로 막는다.
    public bool IsReloading
    {
        get { return weapon != null && weapon.IsReloading; }
    }

    // 상자 UI 등 외부 UI 가 SetMovementLocked 로 잠근 상태
    private bool movementLocked;

    // 줌(조준) 등에서 SetSpeedMultiplier 로 거는 이동속도 배율. 1 = 정상 속도
    private float speedMultiplier = 1f;

    // 이동/회전/사격이 전부 막혀야 하는 상태 (상호작용 중이거나, 외부 UI 가 잠갔거나)
    public bool IsControlLocked
    {
        get { return IsInteracting || movementLocked || IsDead; }
    }

    // 죽었는지. 죽으면 IsControlLocked 가 켜져서 사격/구르기/상호작용/무기교체/아이템 사용 등이 전부 막힌다.
    public bool IsDead
    {
        get { return vitals != null && vitals.IsDead; }
    }

    // movementLocked 가 실제로 바뀌는 순간(상자/인벤토리 UI 열기/닫기)에만 발생한다.
    // 상호작용 중(IsInteracting, 게이지 도는 동안)은 포함하지 않는다 - 그동안은 시야가
    // 계속 마우스를 따라가도 된다는 요구사항 때문에 일부러 나눴다. 이동/회전은 이미
    // IsControlLocked 를 직접 읽으니 구독할 필요 없고, PlayerVision 처럼 "UI 로 잠기는
    // 순간에만 방향을 고정해야 하는" 외부 시스템이 구독한다 (OnEnable/OnDisable 로 직접 구독).
    public event Action<bool> MovementLockChanged;

    // 상자 UI 등에서 호출한다 - 열려 있는 동안 이동/회전/사격을 막는다
    public void SetMovementLocked(bool locked)
    {
        if (locked == movementLocked)
        {
            return;
        }

        movementLocked = locked;
        MovementLockChanged?.Invoke(locked);
    }

    // 줌(조준) 등 이동속도에 배율을 걸어야 하는 시스템에서 호출한다. 1 = 정상 속도
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    // 달리는 중 / 구르는 중 / 상호작용 중 / 외부 UI 로 잠긴 중에는 사격 불가 (무기 시스템에서 이 값을 확인)
    public bool CanFire
    {
        get { return !IsSprinting && !isDodging && !IsControlLocked && !IsUsingItem && !IsReloading && !IsExtracting; }
    }

    // 무기 교체(1/2 키) 가능 여부. 재장전 / 상호작용 게이지 / 퀵슬롯 아이템 사용 / 상자·인벤토리 UI 중에는 불가.
    // 실제 장착(HandleWeaponSelected)과 인벤토리 UI 의 선택 표시(InventoryTestBenchLink)가 같은 조건을 쓰도록 한 곳에 둔다.
    public bool CanSwapWeapon
    {
        get { return !IsControlLocked && !IsUsingItem && !IsReloading && !IsExtracting; }
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

    public bool IsArmed
    {
        get { return isArmed; }
    }

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

        if (animator != null)
        {
            fireLayerIndex = animator.GetLayerIndex("Fire"); // 상체 전용 발사 포즈 레이어 (없으면 -1)
            singleShotClipLength = FindClipLength(singleShotClip, SingleShotClipName);
        }

        TryGetComponent(out vitals);
        TryGetComponent(out weapon);
        TryGetComponent(out interaction);
        TryGetComponent(out itemUse);
        TryGetComponent(out extraction);

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
        if (weapon != null)
        {
            weapon.ShotFired += HandleShotFired;
        }

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
        if (weapon != null)
        {
            weapon.ShotFired -= HandleShotFired;
        }

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
        // 달리는 중 방향 전환에서 이동 입력이 잠깐 0 이 되는 것을 걸러내기 위해 마지막 입력 시각을 기록한다 (IsSprinting 참고)
        if (input.MoveInput.sqrMagnitude > 0.01f)
        {
            lastMoveInputTime = Time.time;
        }

        // 트랜스폼/Rigidbody 는 FixedUpdate 에서만 건드린다.
        UpdateAnimator();

        if (vitals != null)
        {
            vitals.SetSprinting(IsSprinting); // 스테미나 소모/회복 판정용
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

        float speed = (IsSprinting ? sprintSpeed : moveSpeed) * speedMultiplier;

        Vector3 velocity = move * speed;
        velocity.y = VerticalVelocity();
        rb.linearVelocity = velocity;
    }

    // ---------- 구르기 ----------

    private void HandleDodge()
    {
        if (isDodging || Time.time < dodgeReadyTime || IsControlLocked || IsUsingItem || IsReloading || IsExtracting)
        {
            return; // 상자/인벤토리 UI, 상호작용, 아이템 사용, 재장전 중에는 구르기로 스테미나만 낭비되는 것 방지
        }

        if (vitals != null && !vitals.TryConsumeDodgeStamina())
        {
            return; // 스테미나 부족
        }

        // 이동 입력이 있으면 그 방향, 없으면 현재 바라보는 방향으로 구른다
        Vector3 move = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
        dodgeDirection = move.sqrMagnitude > 0.01f ? move.normalized : facingDirection;

        // 구르기 클립은 "몸 정면으로 구르는" 하나짜리라서, 누른 키 방향으로 몸을 즉시 돌려 세운다.
        // (평소엔 마우스를 보고 있어서 그대로 두면 옆/뒤로 구를 때 몸이 커서 쪽을 향한 채 미끄러진다.
        //  회전 속도 제한(rotationSpeed)으로 서서히 돌면 구르는 동안 방향이 맞지 않으므로 여기서는 스냅한다)
        // 구르는 동안엔 UpdateFacing 이 멈춰 있어서 이 방향이 유지되고, 끝나면 다시 마우스 쪽으로 돌아온다.
        facingDirection = dodgeDirection;
        Quaternion rollRotation = Quaternion.LookRotation(dodgeDirection, Vector3.up);
        rb.rotation = rollRotation;
        transform.rotation = rollRotation;

        isDodging = true;
        dodgeEndTime = Time.time + dodgeDuration;
        dodgeReadyTime = Time.time + dodgeCooldown;

        // 구르기 시작 순간에만 한 번 쏜다. Animator 에서 Any State → Roll(조건: Dodge 트리거)로 연결하고,
        // Roll → Idle 은 Has Exit Time 으로 클립이 끝나면 돌아오게 한다.
        if (animator != null)
        {
            animator.SetTrigger(DodgeTriggerHash);
        }

        // TODO: 무적 프레임
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
                    facingDirection = GetMuzzleAlignedFacing(toAim.normalized);
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

    // 총구가 몸 중심선에서 옆으로 떨어져 있어서(오른손), 몸 중심을 커서로 돌리면 총열 연장선이 커서 옆을
    // 지나간다. 그런데 총알은 총구에서 커서로 나가므로 총열과 총알 궤적이 비스듬하게 어긋나 보인다
    // (커서가 가까울수록, 화면 좌우로 쏠 때 특히 잘 보인다).
    // 그래서 몸을 총구의 옆 거리만큼 더 틀어서 "총구에서 몸 정면 방향으로 뻗은 선"이 정확히 커서를 지나게 한다:
    //   몸 방향 yaw = (몸 중심 -> 커서 yaw) - asin(총구 옆 거리 / 몸 중심~커서 거리)
    // 총구 옆 거리는 매 프레임 실제 FirePoint 위치로 잰다 - 무기/애니메이션이 달라도 자동으로 맞는다.
    // 커서가 총구 옆 거리보다 가까우면 수학적으로 맞출 수 없으므로 기존처럼 몸 중심 기준으로 돌린다.
    private Vector3 GetMuzzleAlignedFacing(Vector3 defaultDirection)
    {
        Vector3 result = defaultDirection;

        Transform muzzle = null;
        if (compensateMuzzleOffset && isArmed && weapon != null)
        {
            muzzle = weapon.CurrentShotPoint;
        }

        if (muzzle != null)
        {
            // 총알이 쓰는 것과 같은 조준점(총구 높이 평면)을 기준으로 맞춘다 (WeaponController.FireProjectile)
            Vector3 aimPoint = aimWorldPoint;
            if (TryGetAimPointAtHeight(muzzle.position.y, out Vector3 muzzleHeightAimPoint))
            {
                aimPoint = muzzleHeightAimPoint;
            }

            Vector3 toAim = aimPoint - transform.position;
            toAim.y = 0f;
            float distance = toAim.magnitude;

            // 몸 기준 로컬 좌표에서 총구의 x = 몸 정면 축에서 오른쪽(+)/왼쪽(-)으로 떨어진 거리
            Vector3 localMuzzle = Quaternion.Inverse(transform.rotation) * (muzzle.position - transform.position);
            float lateralOffset = localMuzzle.x;

            if (distance > Mathf.Abs(lateralOffset) + MinAimDistanceForMuzzleAlign)
            {
                float yawToAim = Mathf.Atan2(toAim.x, toAim.z) * Mathf.Rad2Deg;
                float correction = Mathf.Asin(lateralOffset / distance) * Mathf.Rad2Deg;
                result = Quaternion.Euler(0f, yawToAim - correction, 0f) * Vector3.forward;
            }
        }

        return result;
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

        // Idle/Walk/Run 은 이제 상태를 따로 두지 않고, 각 상태 자체가 UnArmed/Armed 클립을
        // IsArmedBlend(0~1)로 블렌드하는 블렌드 트리다 - 별도 레이어나 조건부 전이 없이
        // 이 값 하나로 무기 든 포즈/안 든 포즈가 갈린다.
        animator.SetFloat("IsArmedBlend", isArmed ? 1f : 0f);

        // Fire 레이어(상체 전용 마스크)로 발사 포즈를 덮어씌운다 - 하체(걷기/달리기)는 그대로 유지된다.
        if (fireLayerIndex >= 0 && weapon != null)
        {
            UpdateFireLayer();
        }
    }

    // 발사 상태(FireSingle/FireAuto)는 Animator 의 Entry 전이에 맡기지 않고 여기서 직접 Play 한다.
    // Entry 전이는 레이어에 처음 들어갈 때 한 번만 평가돼서, 게임 중 무기가 바뀌어도 상태가 안 바뀌기 때문이다.
    //  - 연사 무기: 쏘는 동안 FireAuto 를 autoFireAnimSpeed 배속으로 루프. 발사 속도에 맞추지 않는다 -
    //    초당 10발 같은 연사 속도에 반동 1회를 맞추면 모션이 너무 빨라 눈에 안 보이기 때문이다.
    //    (한 발 한 발의 느낌은 크로스헤어 블룸/총구 화염/소리 쪽이 담당한다)
    //    쏘기 시작하는 순간 클립을 처음부터 틀어서 첫 발과 첫 반동을 맞춘다.
    //  - 단발 무기: HandleShotFired 가 한 발마다 FireSingle 을 기본 속도로 처음부터 1회 재생하고,
    //    여기서는 그 클립 길이만큼만 레이어를 켠다.
    private void UpdateFireLayer()
    {
        bool autoFiring = weapon.IsAutoFiring;
        float weight = 0f;

        if (isDodging)
        {
            // 구르는 동안엔 상체(오른팔) 발사 포즈가 구르기 클립을 덮어쓰지 않게 끈다
            wasAutoFiring = false;
            animator.SetLayerWeight(fireLayerIndex, 0f);
            return;
        }

        if (autoFiring)
        {
            if (!wasAutoFiring)
            {
                animator.Play(FireAutoStateHash, fireLayerIndex, 0f);
            }

            animator.SetFloat(FireSpeedHash, autoFireAnimSpeed);
            weight = 1f;
        }
        else if (isArmed && Time.time < singleShotVisibleUntil)
        {
            weight = 1f;
        }

        wasAutoFiring = autoFiring;
        animator.SetLayerWeight(fireLayerIndex, weight);
    }

    // WeaponController.ShotFired - 실제로 한 발 나갈 때마다 호출된다 (연사 무기는 UpdateFireLayer 가 따로 처리)
    private void HandleShotFired()
    {
        if (animator != null && fireLayerIndex >= 0 && !weapon.IsAutomaticWeaponEquipped)
        {
            animator.SetFloat(FireSpeedHash, 1f);
            animator.Play(FireSingleStateHash, fireLayerIndex, 0f);
            singleShotVisibleUntil = Time.time + singleShotClipLength;
        }
    }

    // 인스펙터에 클립이 연결돼 있으면 그 길이, 없으면 Animator 에 들어있는 클립을 이름으로 찾는다
    private float FindClipLength(AnimationClip clip, string clipName)
    {
        float length = 0f;

        if (clip != null)
        {
            length = clip.length;
        }
        else if (animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip candidate in animator.runtimeAnimatorController.animationClips)
            {
                if (candidate != null && candidate.name == clipName)
                {
                    length = candidate.length;
                    break;
                }
            }
        }

        if (length <= 0f)
        {
            Debug.LogWarning("PlayerController: 발사 클립 '" + clipName + "' 을 찾지 못해 길이를 1초로 가정합니다. 인스펙터에 클립을 연결하세요.", this);
            length = 1f;
        }

        return length;
    }

    private bool TryGetAimPoint(out Vector3 point)
    {
        point = Vector3.zero;

        if (aimCamera == null)
        {
            return false;
        }

        // CrosshairUI 가 보여주는 반동 킥과 같은 값을 여기도 더한다 - 그래야 크로스헤어가 튄
        // 자리와 실제로 총알이 맞는 자리가 항상 일치한다 (WeaponController.RecoilKickOffset).
        Vector2 recoilKick = weapon != null ? weapon.RecoilKickOffset : Vector2.zero;
        Ray ray = aimCamera.ScreenPointToRay(input.LookScreenPosition + recoilKick);

        // 발사 시 실제 총구 높이로 다시 교차시킬 수 있게 이번 프레임 조준 레이를 저장해 둔다 (TryGetAimPointAtHeight).
        aimRay = ray;
        hasAimRay = true;

        // 이 조준점은 캐릭터 회전 / 시야 방향 / 카메라용이다. 실제 총알 방향은 WeaponController 가
        // 발사 순간의 총구(FirePoint) 높이로 TryGetAimPointAtHeight 를 다시 불러서 정한다.
        return RaycastHorizontalPlane(ray, groundPlaneY + aimHeightOffset, out point);
    }

    // 마지막으로 계산한 조준 레이(LateUpdate - 카메라가 다 움직이고 반동 킥까지 반영된 레이)를
    // 월드 높이 worldY 의 수평면과 교차시킨 점을 돌려준다.
    // 총알은 총구 높이의 수평면으로만 날아가므로(발사 방향 y = 0), 조준점도 "실제 총구 높이"에서 구해야
    // 화면의 커서 위치와 탄착 방향이 일치한다. 높이가 다르면 카메라가 비스듬할수록 원근 때문에 어긋난다.
    // 총구가 총 모델(FirePoint)에 붙어 있어 무기/애니메이션마다 높이가 달라지므로, 고정 오프셋 대신
    // WeaponController 가 발사할 때마다 그 순간의 총구 높이로 이 메서드를 부른다.
    public bool TryGetAimPointAtHeight(float worldY, out Vector3 point)
    {
        point = Vector3.zero;
        bool found = false;

        if (hasAimRay)
        {
            found = RaycastHorizontalPlane(aimRay, worldY, out point);
        }

        return found;
    }

    private bool RaycastHorizontalPlane(Ray ray, float worldY, out Vector3 point)
    {
        point = Vector3.zero;
        bool found = false;

        Plane plane = new Plane(Vector3.up, new Vector3(0f, worldY, 0f));
        if (plane.Raycast(ray, out float enter))
        {
            point = ray.GetPoint(enter);
            found = true;
        }

        return found;
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
        if (weapon != null && !IsControlLocked && !IsUsingItem)
        {
            weapon.TryReload(); // 이미 재장전 중이면 WeaponController.TryReload() 가 알아서 무시한다
        }
    }

    private void HandleInteract()
    {
        if (isDodging || interaction == null || IsControlLocked || IsUsingItem || IsReloading || IsExtracting)
        {
            return; // 구르는 중/이미 잠긴 중(상자 등)/아이템 사용/재장전 중에는 상호작용 시작 불가
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
        if (weapon != null && CanSwapWeapon)
        {
            weapon.SelectSlot(slotIndex); // 재장전 / 상호작용 / 아이템 사용 / 상자·인벤토리 UI 중에는 무기 교체 금지
        }
    }

    private void HandleQuickSlot(int slotIndex)
    {
        // 실제 처리는 ItemUseController.cs 가 이 이벤트를 직접 구독해서 담당한다 (PlayerController 는
        // NaYeongMin 인벤토리 타입을 몰라도 되게 하려고 여기서는 아무것도 안 한다).
    }
}
