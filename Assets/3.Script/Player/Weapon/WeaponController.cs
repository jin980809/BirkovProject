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

    private ItemData equippedWeapon;
    private int equippedSlotIndex = -1;
    private readonly int[] ammoInMagazine = new int[InventorySettings.WeaponQuickSlotCount];

    // 재장전 진행 상태. ItemUseController 의 아이템 사용과 같은 방식(interaction 슬라이더 재사용,
    // 진행 중엔 걷기/시야 회전만 가능)으로 처리한다 - PlayerController.IsReloading 이 이 값을 그대로 비춘다.
    private bool isReloading;
    private float reloadTimer;
    private float reloadDuration;
    private int reloadAmmoItemId;
    private int reloadNeeded;

    public bool IsReloading
    {
        get { return isReloading; }
    }

    // UI 가 읽는 0~1 진행률 (InteractionPromptUI 가 상호작용/아이템 사용과 같은 슬라이더로 보여준다)
    public float ReloadProgress01
    {
        get { return reloadDuration > 0f ? Mathf.Clamp01(reloadTimer / reloadDuration) : 0f; }
    }

    // 슬롯별로 마지막에 장착됐던 무기의 itemId. 같은 무기를 다시 선택했을 뿐인데 탄창이
    // 매번 가득 채워지는 걸(무한 재장전 악용) 막기 위해, 슬롯의 무기가 실제로 "바뀌었을 때"만
    // 새로 채운다 (아래 EquipSlot 참고).
    private readonly int[] lastEquippedItemId = new int[InventorySettings.WeaponQuickSlotCount];

    private bool fireHeld;
    private float nextFireReadyTime;
    private float currentSpreadDegrees; // 0(완전 정조준) ~ EffectiveMaxSpread

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

    public int CurrentAmmo
    {
        get { return equippedSlotIndex >= 0 ? ammoInMagazine[equippedSlotIndex] : 0; }
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

        for (int i = 0; i < lastEquippedItemId.Length; i++)
        {
            lastEquippedItemId[i] = -1; // -1 = 아직 이 슬롯에 아무것도 장착된 적 없음 (실제 itemId 는 항상 양수)
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
        RecoverBloom();
        UpdateReload();

        // 자동 사격 무기는 발사 버튼을 누르고 있는 동안 쿨다운마다 계속 나간다
        if (fireHeld && !isReloading && equippedWeapon != null && equippedWeapon.automatic)
        {
            TryFireOnce();
        }

        RefreshDebugDisplay();
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
        reloadTimer = 0f;
        reloadDuration = 0f;
        // 취소하면 지금까지 모은 진행도는 버리고, 탄약은 소모/획득 없이 원래 상태 그대로 둔다
    }

    private void CompleteReload()
    {
        isReloading = false;
        reloadTimer = 0f;
        reloadDuration = 0f;

        if (equippedWeapon == null || equippedSlotIndex < 0 || inventoryBridge == null)
        {
            return;
        }

        int gained = inventoryBridge.ConsumeAmmo(reloadAmmoItemId, reloadNeeded);
        ammoInMagazine[equippedSlotIndex] += gained;
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
        ItemData weapon = null;
        if (inventoryBridge != null)
        {
            inventoryBridge.TryGetEquippedWeapon(slotIndex, out weapon);
        }

        // 이 슬롯에 실제로 "새 무기"가 들어왔을 때만 탄창을 채운다 - 인스턴스별 잔탄을 저장하는
        // 시스템이 아직 없어서(내구도 미구현과 같은 이유) 처음 장착 시엔 가득 채워서 시작하지만,
        // 같은 무기를 다시 선택했을 뿐이면 이미 쏜 만큼 줄어든 잔탄을 그대로 유지해야 한다
        // (안 그러면 슬롯을 껐다 켰다 하는 것만으로 탄창이 무한 리필된다).
        int newItemId = weapon != null ? weapon.itemId : -1;
        if (weapon != null && lastEquippedItemId[slotIndex] != newItemId)
        {
            ammoInMagazine[slotIndex] = weapon.magazineSize;
        }
        lastEquippedItemId[slotIndex] = newItemId;

        equippedWeapon = weapon;
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

        int needed = equippedWeapon.magazineSize - ammoInMagazine[equippedSlotIndex];
        if (needed <= 0)
        {
            return;
        }

        if (!TryGetAmmoItemId(equippedWeapon.itemId, out int ammoItemId))
        {
            return; // 이 무기의 탄약 매핑이 아직 설정되지 않음
        }

        reloadAmmoItemId = ammoItemId;
        reloadNeeded = needed;
        reloadDuration = equippedWeapon.reloadSpeed;
        reloadTimer = 0f;

        if (reloadDuration <= 0f)
        {
            CompleteReload(); // reloadSpeed 가 0 이하면 즉시 완료 (기존과 동일한 동작)
            return;
        }

        isReloading = true;
    }

    // ---------- 내부 ----------

    private void TryFireOnce()
    {
        if (isReloading || equippedWeapon == null || equippedSlotIndex < 0 || player == null || firePoint == null)
        {
            return;
        }

        if (Time.time < nextFireReadyTime)
        {
            return;
        }

        if (ammoInMagazine[equippedSlotIndex] <= 0)
        {
            return; // TODO: 빈 탄창 소리
        }

        float interval = equippedWeapon.fireRate > 0f ? 1f / equippedWeapon.fireRate : 0f;
        nextFireReadyTime = Time.time + interval;

        ammoInMagazine[equippedSlotIndex]--;

        // 이번 발사는 지금까지 쌓인 퍼짐(currentSpreadDegrees) 을 그대로 쓰고,
        // 다음 발사를 위한 증가는 쏜 뒤에 적용한다
        int pellets = Mathf.Max(1, equippedWeapon.pelletCount);
        for (int i = 0; i < pellets; i++)
        {
            FireProjectile();
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

    private void FireProjectile()
    {
        if (bulletPool == null)
        {
            return;
        }

        Vector3 baseDirection = player.AimWorldPoint - firePoint.position;
        baseDirection.y = 0f;
        if (baseDirection.sqrMagnitude < 0.0001f)
        {
            baseDirection = firePoint.forward;
        }
        baseDirection.Normalize();

        // 펠릿이 여러 개인 무기(샷건 등)는 한 발에 여러 알이 원래 부채꼴로 퍼져 나가야 하므로,
        // 연사 블룸 누적치(currentSpreadDegrees)와 상관없이 매번 무기 자체의 최대 퍼짐(maxSpread)
        // 범위에서 각자 독립적으로 흩어지게 한다. 첫 발엔 블룸이 0이라 이걸 안 하면 펠릿들이
        // 전부 완전히 같은 방향으로 나가 서로 겹쳐 보인다. 펠릿이 1개인 무기는 기존처럼 블룸을 쓴다.
        float spreadDegrees = equippedWeapon.pelletCount > 1 ? EffectiveMaxSpread : currentSpreadDegrees;
        Vector3 direction = ApplySpread(baseDirection, spreadDegrees);

        Projectile projectile = bulletPool.Rent(firePoint.position, Quaternion.LookRotation(direction));

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
