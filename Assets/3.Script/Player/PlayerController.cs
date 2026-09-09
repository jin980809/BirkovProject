using UnityEngine;

// 플레이어 이동 / 회전 / 구르기 처리
// 입력은 PlayerInputHandler 에서 받아온다 (같은 GameObject 에 붙어 있어야 함).
//
// 회전 규칙:
//  - 기본: 마우스 포인터 방향으로 회전
//  - 달리는 중(Shift + 이동): 키보드 입력 방향으로 회전
//  - Shift 를 떼면 다시 마우스 방향 회전
//  - 달리는 중에는 사격 불가
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

    private Rigidbody rb;
    private PlayerInputHandler input;

    // 회전 상태
    private Vector3 facingDirection = Vector3.forward;

    // 조준점 (마우스 커서가 가리키는 월드 좌표) - 카메라/무기 시스템이 참조
    private Vector3 aimWorldPoint;
    private bool hasAimPoint;

    // 구르기 상태
    private bool isDodging;
    private float dodgeEndTime;
    private float dodgeReadyTime;
    private Vector3 dodgeDirection;

    public bool IsDodging
    {
        get { return isDodging; }
    }

    // Shift 를 누른 채로 실제 이동 중일 때만 달리기로 친다
    public bool IsSprinting
    {
        get { return input.SprintHeld && input.MoveInput.sqrMagnitude > 0.01f; }
    }

    // 달리는 중에는 사격 불가 (무기 시스템에서 이 값을 확인)
    public bool CanFire
    {
        get { return !IsSprinting && !isDodging; }
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        facingDirection = transform.forward;
        aimWorldPoint = transform.position + transform.forward;

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
        input.InventoryToggled -= HandleInventory;
        input.WeaponSelected -= HandleWeaponSelected;
        input.QuickSlotUsed -= HandleQuickSlot;
    }

    private void Update()
    {
        UpdateAimPoint();
        UpdateFacing();
        ApplyRotation();
    }

    private void FixedUpdate()
    {
        if (isDodging)
        {
            UpdateDodge();
        }
        else
        {
            UpdateMovement();
        }
    }

    // ---------- 이동 ----------

    private void UpdateMovement()
    {
        Vector3 move = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        float speed = IsSprinting ? sprintSpeed : moveSpeed;

        Vector3 velocity = move * speed;
        velocity.y = rb.linearVelocity.y; // 중력에 의한 수직 속도는 유지
        rb.linearVelocity = velocity;
    }

    // ---------- 구르기 ----------

    private void HandleDodge()
    {
        if (isDodging || Time.time < dodgeReadyTime)
        {
            return;
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
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Vector3 velocity = dodgeDirection * dodgeSpeed;
        velocity.y = rb.linearVelocity.y;
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
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
    }

    private bool TryGetAimPoint(out Vector3 point)
    {
        point = Vector3.zero;

        if (aimCamera == null)
        {
            return false;
        }

        Ray ray = aimCamera.ScreenPointToRay(input.LookScreenPosition);

        // 먼저 실제 지형 콜라이더와 충돌 검사
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 200f, groundMask, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            return true;
        }

        // 충돌이 없으면 플레이어 높이의 수평면과 교차점을 사용
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
        float enter;
        if (groundPlane.Raycast(ray, out enter))
        {
            point = ray.GetPoint(enter);
            return true;
        }

        return false;
    }

    // ---------- 다른 시스템 연결 지점 ----------

    private void HandleFireStart()
    {
        if (!CanFire)
        {
            return; // 달리는 중 / 구르는 중에는 사격 불가
        }

        // TODO: 무기 발사 시작
    }

    private void HandleFireStop()
    {
        // TODO: 무기 발사 중지 (자동 사격용)
    }

    private void HandleReload()
    {
        // TODO: 재장전
    }

    private void HandleInteract()
    {
        // TODO: 상호작용 (루팅, 문 등)
    }

    private void HandleInventory()
    {
        // TODO: 인벤토리 열기 / 닫기
    }

    private void HandleWeaponSelected(int slotIndex)
    {
        // TODO: 무기 교체 (0 = 1번 슬롯, 1 = 2번 슬롯)
    }

    private void HandleQuickSlot(int slotIndex)
    {
        // TODO: 소모성 아이템 사용 (0~2 = 3, 4, 5번 키)
    }
}
