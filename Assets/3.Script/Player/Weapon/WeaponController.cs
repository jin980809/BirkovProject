using System;
using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;

// 장착된 무기 선택 / 발사 / 재장전.
// PlayerController 가 입력 이벤트(발사/재장전/무기교체)를 받아서 이 컴포넌트로 위임한다.
//
// 연사속도 기준: ItemData.fireRate 를 사용한다.
// 내구도: 아직 다루지 않는다 (NaYeongMin 쪽에 인스턴스별 내구도 저장소가 없음).
[RequireComponent(typeof(PlayerController))]
public class WeaponController : MonoBehaviour
{
    // 무기 itemId 별로 어떤 탄약 itemId 를 쓰는지.
    // ItemData 에 전용 필드가 아직 없어서 임시로 여기서 설정한다.
    // 나중에 ItemData 에 탄약 필드가 생기면 이 매핑은 지우고 그쪽 값을 쓰면 된다.
    [Serializable]
    public struct WeaponAmmoMapping
    {
        public int weaponItemId;
        public int ammoItemId;
    }

    [Header("연결")]
    [SerializeField] private WeaponInventoryBridge inventoryBridge;
    [Tooltip("장착한 총 모델에 FirePoint 가 없을 때 대신 쓰는 발사 위치 (보통은 WeaponVisual 이 스폰한 총 프리팹의 FirePoint 를 쓴다)")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private BulletPool bulletPool;
    [SerializeField] private Collider ownerCollider; // 자기 자신과의 충돌 무시용, 비우면 자동 탐색

    [Header("탄약 매핑 (나중에 여기서 자유롭게 추가/수정)")]
    [SerializeField] private WeaponAmmoMapping[] ammoMappings = Array.Empty<WeaponAmmoMapping>();

    [Header("블룸 (연사할수록 퍼지고, 안 쏘면 다시 좁혀짐)")]
    [Tooltip("한 발 쏠 때마다 maxSpread 의 이 비율만큼 현재 퍼짐이 늘어난다")]
    [SerializeField] private float bloomGrowthFraction = 0.25f;
    [Tooltip("초당 maxSpread 의 이 비율만큼 현재 퍼짐이 줄어든다")]
    [SerializeField] private float bloomRecoverFraction = 1.5f;

    [Header("반동 킥 (크로스헤어 + 실제 조준점을 같이 흔든다)")]
    [Tooltip("무기의 maxSpread(도) 1당 한 발에 튀는 화면 픽셀 거리 - 무기마다 maxSpread 가 다르므로 반동 세기가 자동으로 무기에 비례한다")]
    [SerializeField] private float recoilKickPixelsPerSpreadDegree = 2f;
    [Tooltip("튄 뒤 원래 자리로 돌아오는 빠르기 (클수록 빠르게 진정됨)")]
    [SerializeField] private float recoilRecoverySharpness = 10f;

    private Vector2 recoilKickOffset;

    // 크로스헤어(시각)와 PlayerController 의 조준 계산(실제 탄착)이 똑같은 값을 봐야 서로 어긋나지
    // 않는다 - 그래서 이 오프셋을 여기 한 곳에서만 계산하고 양쪽이 그대로 읽어간다.
    public Vector2 RecoilKickOffset
    {
        get { return recoilKickOffset; }
    }

    [Header("디버그 - 현재 장착 무기 (읽기 전용, Play 중에만 갱신됨)")]
    [SerializeField] private string debugWeaponName;
    [SerializeField] private float debugAttackDamage;
    [SerializeField] private float debugFireRate;
    [SerializeField] private int debugMagazineSize;
    [SerializeField] private float debugProjectileSpeed;
    [SerializeField] private float debugRange;
    [SerializeField] private float debugMaxSpread;
    [SerializeField] private int debugCurrentAmmo;

    private PlayerController player;
    private PlayerNoise noise;
    private PlayerInputHandler input;
    private WeaponVisual weaponVisual; // 선택 - 있으면 장착한 총 모델의 FirePoint 에서 발사한다

    private ItemData equippedWeapon;
    private int equippedSlotIndex = -1;

    // 장착 무기가 들어있는 장비 슬롯 데이터. 잔탄은 여기(remainingRounds)에 저장한다.
    // 인스턴스별 ID 가 없어서 무기 개체를 구분할 방법이 슬롯 데이터뿐인데, 인벤토리 이동(MoveItem)이
    // remainingRounds 를 같이 옮겨주므로 가방/창고/전리품으로 옮겼다가 다시 껴도 잔탄이 그대로 따라온다.
    // 무기에서 remainingRounds 는 "탄창에서 비운 발 수"로 쓴다 - 0 이면 가득 찬 상태라서, 드롭/지급으로
    // 새로 생긴 무기(기본값 0)는 자동으로 가득 찬 탄창으로 시작한다.
    // (탄약 아이템에서의 원래 뜻 "뜯다 만 박스의 잔탄, 0 = 미개봉"과도 충돌하지 않는다 - 무기와 탄약은 슬롯을 공유하지 않는다)
    private GridSlotData equippedWeaponSlot;

    // 재장전 진행 상태. ItemUseController 의 아이템 사용과 같은 방식(interaction 슬라이더 재사용,
    // 진행 중엔 걷기/시야 회전만 가능)으로 처리한다 - PlayerController.IsReloading 이 이 값을 그대로 비춘다.
    private bool isReloading;
    private float reloadTimer;
    private float reloadDuration;
    private int reloadAmmoItemId;
    private int reloadNeeded;

    // 펠릿이 여러 개인 무기(샷건 등)는 한 사이클(게이지 1회)에 총알 1개씩만 장전하고, 탄창이 꽉 차거나
    // 탄약이 떨어질 때까지 사이클을 자동으로 이어간다. 매 사이클이 끝날 때마다 그 결과를 바로
    // 탄창에 반영하기 때문에, ESC 로 취소해도 이미 끝난 사이클만큼은 그대로 남고 진행 중이던
    // 사이클만 버려진다 (완전히 새로 시작하는 다른 무기들과 다른 점).
    private bool reloadOneAtATime;

    public bool IsReloading
    {
        get { return isReloading; }
    }

    // UI 가 읽는 0~1 진행률 (InteractionPromptUI 가 상호작용/아이템 사용과 같은 슬라이더로 보여준다)
    public float ReloadProgress01
    {
        get { return reloadDuration > 0f ? Mathf.Clamp01(reloadTimer / reloadDuration) : 0f; }
    }

    private bool fireHeld;
    private float nextFireReadyTime;
    private float currentSpreadDegrees; // 0(완전 정조준) ~ EffectiveMaxSpread

    // 실제로 한 발 나갈 때마다 발생한다 (펠릿이 여러 개여도 한 번). PlayerController 가 단발 무기
    // 발사 애니메이션을 한 발마다 처음부터 재생하는 데 쓴다.
    public event Action ShotFired;

    // PlayerController.UpdateFireLayer() 가 아래 값들로 Fire 레이어(상체 전용 마스크)를 제어한다.
    public bool IsAutomaticWeaponEquipped
    {
        get { return equippedWeapon != null && equippedWeapon.automatic; }
    }

    // 연사 무기로 지금 계속 쏘고 있는 중인지 (쏘는 동안 반동 루프를 보여준다).
    // 사격이 막힌 동안(달리기/인벤토리/아이템 사용 등)과 탄창이 빈 동안은 false.
    public bool IsAutoFiring
    {
        get
        {
            return IsAutomaticWeaponEquipped && fireHeld && !isReloading && CurrentAmmo > 0 &&
                   player != null && player.CanFire;
        }
    }

    // 줌(조준) 등에서 SetAimSpreadMultiplier 로 거는 배율. 1 = 정상, 0.5 면 최대 퍼짐이 절반으로 줄어듦
    private float aimSpreadMultiplier = 1f;

    // 실제 블룸 계산/크로스헤어가 쓰는 "유효" 최대 퍼짐 (무기 원본 maxSpread * 조준 배율)
    private float EffectiveMaxSpread
    {
        get { return equippedWeapon != null ? equippedWeapon.maxSpread * aimSpreadMultiplier : 0f; }
    }

    public void SetAimSpreadMultiplier(float multiplier)
    {
        aimSpreadMultiplier = multiplier;
    }

    public bool HasWeaponEquipped
    {
        get { return equippedWeapon != null; }
    }

    // 지금 장착 중인 무기의 itemId (없으면 -1). WeaponVisual 이 어떤 모델을 보여줄지 결정할 때 쓴다.
    public int EquippedWeaponItemId
    {
        get { return equippedWeapon != null ? equippedWeapon.itemId : -1; }
    }

    public int CurrentAmmo
    {
        get
        {
            int ammo = 0;
            if (equippedWeapon != null && equippedWeaponSlot != null)
            {
                int spentRounds = Mathf.Clamp(equippedWeaponSlot.remainingRounds, 0, equippedWeapon.magazineSize);
                ammo = equippedWeapon.magazineSize - spentRounds;
            }

            return ammo;
        }
    }

    // 잔탄을 장비 슬롯 데이터에 기록한다 (remainingRounds = 비운 발 수, CurrentAmmo 참고)
    private void SetCurrentAmmo(int ammo)
    {
        if (equippedWeapon != null && equippedWeaponSlot != null)
        {
            int clampedAmmo = Mathf.Clamp(ammo, 0, equippedWeapon.magazineSize);
            equippedWeaponSlot.remainingRounds = equippedWeapon.magazineSize - clampedAmmo;
        }
    }

    // 크로스헤어 UI 가 읽는 값. 0(정조준) ~ 1(그 무기의 최대 퍼짐)
    public float CurrentSpreadNormalized
    {
        get
        {
            float effectiveMax = EffectiveMaxSpread;
            if (effectiveMax <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(currentSpreadDegrees / effectiveMax);
        }
    }

    // 크로스헤어 UI 가 무기별로 최대 반경을 다르게 잡기 위해 참조하는 값 (도 단위). 무기가 없으면 0.
    // 줌(조준) 배율이 걸려 있으면 그만큼 줄어든 값이 나온다 - 크로스헤어도 같이 좁아지게 하기 위해서다.
    public float EquippedMaxSpreadDegrees
    {
        get { return EffectiveMaxSpread; }
    }

    private void Awake()
    {
        TryGetComponent(out player);
        TryGetComponent(out noise);
        TryGetComponent(out input);
        TryGetComponent(out weaponVisual);

        if (inventoryBridge == null)
        {
            inventoryBridge = GetComponent<WeaponInventoryBridge>();
        }

        if (bulletPool == null)
        {
            // BulletPool 은 이제 Player 가 아니라 별도 최상위 오브젝트에 있으므로 씬에서 찾는다
            bulletPool = FindAnyObjectByType<BulletPool>();
        }

        if (ownerCollider == null)
        {
            TryGetComponent(out ownerCollider);
        }
    }

    private void Start()
    {
        EquipSlot(0); // 시작 시 주 무기 슬롯을 기본으로 시도
    }

    private void OnEnable()
    {
        if (input != null)
        {
            input.CancelPressed += HandleCancelReload;
        }
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.CancelPressed -= HandleCancelReload;
        }
    }

    private void Update()
    {
        // 장착 중인 무기가 인벤토리 조작(드래그로 빼기/바꿔 끼우기, 사망 초기화 등)으로 슬롯에서 바뀌었으면
        // 그 슬롯을 다시 조회한다. 그러지 않으면 이미 빠진 무기로 계속 쏘면서 잔탄을 엉뚱한 슬롯
        // (새로 들어온 다른 무기나 빈 슬롯)에 기록하게 된다.
        if (equippedWeapon != null && (equippedWeaponSlot == null || equippedWeaponSlot.itemId != equippedWeapon.itemId))
        {
            EquipSlot(equippedSlotIndex);
        }

        RecoverBloom();
        UpdateReload();
        UpdateRecoilKick();

        // 자동 사격 무기는 발사 버튼을 누르고 있는 동안 쿨다운마다 계속 나간다
        if (fireHeld && !isReloading && equippedWeapon != null && equippedWeapon.automatic)
        {
            TryFireOnce();
        }

        RefreshDebugDisplay();
    }

    private void UpdateRecoilKick()
    {
        recoilKickOffset = Vector2.Lerp(recoilKickOffset, Vector2.zero, 1f - Mathf.Exp(-recoilRecoverySharpness * Time.deltaTime));
    }

    private void UpdateReload()
    {
        if (!isReloading)
        {
            return;
        }

        reloadTimer += Time.deltaTime;
        if (reloadTimer >= reloadDuration)
        {
            CompleteReload();
        }
    }

    private void HandleCancelReload()
    {
        if (!isReloading)
        {
            return;
        }

        isReloading = false;
        reloadOneAtATime = false;
        reloadTimer = 0f;
        reloadDuration = 0f;
        // 취소해도 이미 끝난 사이클(한 발씩 장전 방식이면 이미 채워진 발들)은 그대로 두고,
        // 지금 진행 중이던 사이클의 진행도만 버린다 - CompleteReload() 가 사이클이 끝날 때마다
        // 바로바로 탄창에 반영하기 때문에 여기서 되돌릴 게 없다.
    }

    private void CompleteReload()
    {
        reloadTimer = 0f;

        if (equippedWeapon == null || equippedSlotIndex < 0 || inventoryBridge == null)
        {
            isReloading = false;
            reloadOneAtATime = false;
            reloadDuration = 0f;
            return;
        }

        int gained = inventoryBridge.ConsumeAmmo(reloadAmmoItemId, reloadNeeded);
        SetCurrentAmmo(CurrentAmmo + gained);

        bool magazineFull = CurrentAmmo >= equippedWeapon.magazineSize;
        bool ranOutOfAmmo = gained <= 0;

        if (reloadOneAtATime && !magazineFull && !ranOutOfAmmo)
        {
            // 아직 덜 찼고 탄약도 남아있으면 다음 한 발 장전 사이클을 바로 이어간다 (게이지가
            // 끊기지 않고 계속 돈다) - reloadDuration 은 그대로, 타이머만 리셋한다.
            reloadNeeded = 1;
            return;
        }

        isReloading = false;
        reloadOneAtATime = false;
        reloadDuration = 0f;
    }

    // 인스펙터에서 현재 장착 무기 스펙 + 잔탄을 바로 확인할 수 있게 한다 (읽기 전용 디버그용)
    private void RefreshDebugDisplay()
    {
        if (equippedWeapon == null)
        {
            debugWeaponName = "(없음)";
            debugAttackDamage = 0f;
            debugFireRate = 0f;
            debugMagazineSize = 0;
            debugProjectileSpeed = 0f;
            debugRange = 0f;
            debugMaxSpread = 0f;
            debugCurrentAmmo = 0;
            return;
        }

        debugWeaponName = equippedWeapon.displayName;
        debugAttackDamage = equippedWeapon.attackDamage;
        debugFireRate = equippedWeapon.fireRate;
        debugMagazineSize = equippedWeapon.magazineSize;
        debugProjectileSpeed = equippedWeapon.projectileSpeed;
        debugRange = equippedWeapon.range;
        debugMaxSpread = equippedWeapon.maxSpread;
        debugCurrentAmmo = CurrentAmmo;
    }

    // ---------- PlayerController 가 호출하는 진입점 ----------

    // InventoryTestBenchLink 가 진짜 인벤토리 데이터를 나중에 주입한 뒤 호출한다.
    // Start() 의 EquipSlot(0) 은 그 데이터가 도착하기 전에 실행돼서 빈 데이터로 실패하므로,
    // 데이터가 준비된 뒤 지금 장착 중이던(또는 기본 0번) 슬롯을 다시 조회해서 갱신한다.
    public void RefreshEquippedWeapon()
    {
        EquipSlot(equippedSlotIndex >= 0 ? equippedSlotIndex : 0);
    }

    // slotIndex: 0 = 주 무기, 1 = 보조 무기
    public void EquipSlot(int slotIndex)
    {
        // 재장전 도중 무기가 바뀌면 진행 중이던 재장전은 버린다 (다른 무기 탄창에 채워지는 것 방지)
        HandleCancelReload();

        ItemData weapon = null;
        GridSlotData weaponSlot = null;
        if (inventoryBridge != null && inventoryBridge.TryGetEquippedWeapon(slotIndex, out weapon))
        {
            weaponSlot = inventoryBridge.GetWeaponSlotData(slotIndex);
        }

        // 잔탄은 슬롯 데이터(remainingRounds)에 들어 있으므로 여기서 채우거나 초기화하지 않는다.
        // 같은 무기를 다시 선택하든, 다른 무기로 바꿨다가 돌아오든 그 무기가 쏜 만큼 줄어든 채로 유지된다.
        equippedWeapon = weapon;
        equippedWeaponSlot = weaponSlot;
        equippedSlotIndex = weapon != null ? slotIndex : -1;
        nextFireReadyTime = 0f; // 무기를 바꾸면 발사 쿨다운은 리셋
        currentSpreadDegrees = 0f; // 이전 무기의 블룸은 안 이어받는다

        if (player != null)
        {
            player.SetArmed(equippedWeapon != null);
        }
    }

    public void TryFire()
    {
        fireHeld = true;
        TryFireOnce();
    }

    public void StopFiring()
    {
        fireHeld = false;
    }

    // 실제 탄약 소모/충전은 게이지가 다 찬 뒤 CompleteReload() 에서 처리한다.
    // (ItemUseController 의 아이템 사용과 동일한 패턴 - PlayerController.IsReloading 이 이 상태를
    // 비추고, 진행 중엔 걷기/시야 회전만 가능하도록 CanFire/HandleDodge/HandleInteract 등이 확인한다)
    public void TryReload()
    {
        if (isReloading || equippedWeapon == null || equippedSlotIndex < 0 || inventoryBridge == null)
        {
            return;
        }

        int needed = equippedWeapon.magazineSize - CurrentAmmo;
        if (needed <= 0)
        {
            return;
        }

        if (!TryGetAmmoItemId(equippedWeapon.itemId, out int ammoItemId))
        {
            return; // 이 무기의 탄약 매핑이 아직 설정되지 않음
        }

        if (inventoryBridge.PeekAmmoCount(ammoItemId) <= 0)
        {
            return; // 인벤토리에 해당 탄약이 하나도 없으면 게이지를 아예 시작하지 않는다
        }

        // 펠릿이 여러 개인 무기(샷건 등)는 한 사이클에 총알 1개씩만 장전하고, CompleteReload() 가
        // 매번 그 결과를 바로 반영하며 자동으로 다음 발을 이어간다 (게이지 회전당 한 발).
        reloadOneAtATime = equippedWeapon.pelletCount > 1;
        reloadAmmoItemId = ammoItemId;
        reloadNeeded = reloadOneAtATime ? 1 : needed;
        reloadDuration = equippedWeapon.reloadSpeed;
        reloadTimer = 0f;

        if (reloadDuration <= 0f)
        {
            // reloadSpeed 가 0 이하면 즉시 완료 (기존과 동일한 동작). 한 발씩 장전 방식이면
            // CompleteReload() 가 다음 사이클로 이어가라고 reloadOneAtATime 을 계속 true 로 남겨두므로,
            // 게이지를 기다릴 필요 없이 여기서 바로 다 채워질 때까지 반복한다.
            do
            {
                CompleteReload();
            } while (reloadOneAtATime);

            return;
        }

        isReloading = true;
    }

    // ---------- 내부 ----------

    private void TryFireOnce()
    {
        if (isReloading || equippedWeapon == null || equippedSlotIndex < 0 || player == null)
        {
            return;
        }

        Transform shotPoint = GetShotPoint();
        if (shotPoint == null)
        {
            return;
        }

        // 발사 버튼을 누르고 있는 동안 Update 가 매 프레임 이 메서드를 호출하므로, 누른 순간뿐 아니라
        // 매 발마다 사격 가능 여부를 다시 확인한다 (누른 채로 인벤토리 열기/아이템 사용/달리기에 들어가면 멈춘다).
        // fireHeld 는 건드리지 않으므로, 막힌 상태가 풀렸을 때 계속 누르고 있었다면 다시 쏘기 시작한다.
        if (!player.CanFire)
        {
            return;
        }

        if (Time.time < nextFireReadyTime)
        {
            return;
        }

        if (CurrentAmmo <= 0)
        {
            return; // TODO: 빈 탄창 소리
        }

        float interval = equippedWeapon.fireRate > 0f ? 1f / equippedWeapon.fireRate : 0f;
        nextFireReadyTime = Time.time + interval;

        SetCurrentAmmo(CurrentAmmo - 1);

        // 크로스헤어와 실제 조준점(PlayerController)이 같은 방향으로 같이 튄다 - 무기의
        // maxSpread 가 클수록(퍼짐이 큰 무기일수록) 반동도 세진다.
        recoilKickOffset += UnityEngine.Random.insideUnitCircle.normalized * (equippedWeapon.maxSpread * recoilKickPixelsPerSpreadDegree);

        if (ShotFired != null)
        {
            ShotFired();
        }

        // 이번 발사는 지금까지 쌓인 퍼짐(currentSpreadDegrees) 을 그대로 쓰고,
        // 다음 발사를 위한 증가는 쏜 뒤에 적용한다
        int pellets = Mathf.Max(1, equippedWeapon.pelletCount);
        for (int i = 0; i < pellets; i++)
        {
            FireProjectile(shotPoint);
        }

        GrowBloom();

        if (noise != null)
        {
            noise.EmitGunshotNoise();
        }
    }

    private void GrowBloom()
    {
        float effectiveMax = EffectiveMaxSpread;
        if (effectiveMax <= 0f)
        {
            return;
        }

        float growth = effectiveMax * bloomGrowthFraction;
        currentSpreadDegrees = Mathf.Min(effectiveMax, currentSpreadDegrees + growth);
    }

    private void RecoverBloom()
    {
        float effectiveMax = EffectiveMaxSpread;
        if (effectiveMax <= 0f || currentSpreadDegrees <= 0f)
        {
            return;
        }

        float recover = effectiveMax * bloomRecoverFraction * Time.deltaTime;
        currentSpreadDegrees = Mathf.Max(0f, currentSpreadDegrees - recover);
    }

    // 장착한 총 모델의 FirePoint(프리팹 루트 WeaponModel 에 연결)를 우선 쓰고, 없으면 인스펙터의 기본 firePoint 를 쓴다
    private Transform GetShotPoint()
    {
        Transform point = firePoint;

        if (weaponVisual != null && equippedWeapon != null)
        {
            Transform modelFirePoint = weaponVisual.GetFirePoint(equippedWeapon.itemId);
            if (modelFirePoint != null)
            {
                point = modelFirePoint;
            }
        }

        return point;
    }

    private void FireProjectile(Transform shotPoint)
    {
        if (bulletPool == null)
        {
            return;
        }

        Vector3 baseDirection = player.AimWorldPoint - shotPoint.position;
        baseDirection.y = 0f;
        if (baseDirection.sqrMagnitude < 0.0001f)
        {
            baseDirection = shotPoint.forward;
        }
        baseDirection.Normalize();

        // 펠릿이 여러 개인 무기(샷건 등)는 한 발에 여러 알이 원래 부채꼴로 퍼져 나가야 하므로,
        // 연사 블룸 누적치(currentSpreadDegrees)와 상관없이 매번 무기 자체의 최대 퍼짐(maxSpread)
        // 범위에서 각자 독립적으로 흩어지게 한다. 첫 발엔 블룸이 0이라 이걸 안 하면 펠릿들이
        // 전부 완전히 같은 방향으로 나가 서로 겹쳐 보인다. 펠릿이 1개인 무기는 기존처럼 블룸을 쓴다.
        float spreadDegrees = equippedWeapon.pelletCount > 1 ? EffectiveMaxSpread : currentSpreadDegrees;
        Vector3 direction = ApplySpread(baseDirection, spreadDegrees);

        Projectile projectile = bulletPool.Rent(shotPoint.position, Quaternion.LookRotation(direction));

        // 지금 플레이어가 붙어있는 엄폐물이 있으면, 이번 총알은 그것들을 무시하고 통과한다
        // (엄폐물 너머의 적을 쏠 수 있게 - CoverObject.cs 참고)
        projectile.Launch(direction, equippedWeapon.projectileSpeed, equippedWeapon.attackDamage, equippedWeapon.range, ownerCollider, CoverObject.AttachedCoverColliders);
    }

    private Vector3 ApplySpread(Vector3 direction, float maxSpreadDegrees)
    {
        if (maxSpreadDegrees <= 0f)
        {
            return direction;
        }

        float angle = UnityEngine.Random.Range(-maxSpreadDegrees, maxSpreadDegrees);
        return Quaternion.Euler(0f, angle, 0f) * direction;
    }

    private bool TryGetAmmoItemId(int weaponItemId, out int ammoItemId)
    {
        for (int i = 0; i < ammoMappings.Length; i++)
        {
            if (ammoMappings[i].weaponItemId == weaponItemId)
            {
                ammoItemId = ammoMappings[i].ammoItemId;
                return true;
            }
        }

        ammoItemId = -1;
        return false;
    }
}
