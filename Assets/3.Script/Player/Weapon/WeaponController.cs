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

    [Header("테스트용 무기 (실제 CSV 총기 데이터 나오기 전 임시)")]
    [Tooltip("가방에 장착된 무기가 없을 때 이 임시 스펙으로 대신 장착한다")]
    [SerializeField] private bool useDebugWeapon;
    [SerializeField] private float debugFireRate = 5f;
    [SerializeField] private int debugMagazineSize = 12;
    [SerializeField] private float debugDamage = 10f;
    [SerializeField] private float debugProjectileSpeed = 40f;
    [SerializeField] private float debugRange = 30f;
    [SerializeField] private float debugMaxSpread = 3f;
    [SerializeField] private int debugPelletCount = 1;
    [SerializeField] private bool debugAutomatic = true;

    [Header("블룸 (연사할수록 퍼지고, 안 쏘면 다시 좁혀짐)")]
    [Tooltip("한 발 쏠 때마다 maxSpread 의 이 비율만큼 현재 퍼짐이 늘어난다")]
    [SerializeField] private float bloomGrowthFraction = 0.25f;
    [Tooltip("초당 maxSpread 의 이 비율만큼 현재 퍼짐이 줄어든다")]
    [SerializeField] private float bloomRecoverFraction = 1.5f;

    private PlayerController player;
    private PlayerNoise noise;

    private ItemData equippedWeapon;
    private int equippedSlotIndex = -1;
    private readonly int[] ammoInMagazine = new int[InventorySettings.WeaponQuickSlotCount];

    private bool fireHeld;
    private float nextFireReadyTime;
    private float currentSpreadDegrees; // 0(완전 정조준) ~ equippedWeapon.maxSpread

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
            if (equippedWeapon == null || equippedWeapon.maxSpread <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(currentSpreadDegrees / equippedWeapon.maxSpread);
        }
    }

    private void Awake()
    {
        TryGetComponent(out player);
        TryGetComponent(out noise);

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

    private void Update()
    {
        RecoverBloom();

        // 자동 사격 무기는 발사 버튼을 누르고 있는 동안 쿨다운마다 계속 나간다
        if (fireHeld && equippedWeapon != null && equippedWeapon.automatic)
        {
            TryFireOnce();
        }
    }

    // ---------- PlayerController 가 호출하는 진입점 ----------

    // slotIndex: 0 = 주 무기, 1 = 보조 무기
    public void EquipSlot(int slotIndex)
    {
        ItemData weapon = null;
        if (inventoryBridge != null)
        {
            inventoryBridge.TryGetEquippedWeapon(slotIndex, out weapon);
        }

        if (weapon == null && useDebugWeapon)
        {
            weapon = BuildDebugWeapon();
            ammoInMagazine[slotIndex] = weapon.magazineSize; // 테스트 편의상 가득 채워서 시작
        }

        equippedWeapon = weapon;
        equippedSlotIndex = weapon != null ? slotIndex : -1;
        nextFireReadyTime = 0f; // 무기를 바꾸면 발사 쿨다운은 리셋
        currentSpreadDegrees = 0f; // 이전 무기의 블룸은 안 이어받는다

        if (player != null)
        {
            player.SetArmed(equippedWeapon != null);
        }
    }

    // 실제 CSV에 총기 데이터가 없을 때 발사 로직만 검증하기 위한 임시 무기
    private ItemData BuildDebugWeapon()
    {
        return new ItemData
        {
            itemId = -1,
            displayName = "디버그 테스트 무기",
            itemType = ItemType.Weapon,
            fireRate = debugFireRate,
            magazineSize = debugMagazineSize,
            attackDamage = debugDamage,
            projectileSpeed = debugProjectileSpeed,
            range = debugRange,
            maxSpread = debugMaxSpread,
            pelletCount = Mathf.Max(1, debugPelletCount),
            automatic = debugAutomatic,
        };
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

    public void TryReload()
    {
        if (equippedWeapon == null || equippedSlotIndex < 0 || inventoryBridge == null)
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

        int gained = inventoryBridge.ConsumeAmmo(ammoItemId, needed);
        ammoInMagazine[equippedSlotIndex] += gained;
    }

    // ---------- 내부 ----------

    private void TryFireOnce()
    {
        if (equippedWeapon == null || equippedSlotIndex < 0 || player == null || firePoint == null)
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
        if (equippedWeapon == null || equippedWeapon.maxSpread <= 0f)
        {
            return;
        }

        float growth = equippedWeapon.maxSpread * bloomGrowthFraction;
        currentSpreadDegrees = Mathf.Min(equippedWeapon.maxSpread, currentSpreadDegrees + growth);
    }

    private void RecoverBloom()
    {
        if (equippedWeapon == null || equippedWeapon.maxSpread <= 0f || currentSpreadDegrees <= 0f)
        {
            return;
        }

        float recover = equippedWeapon.maxSpread * bloomRecoverFraction * Time.deltaTime;
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

        Vector3 direction = ApplySpread(baseDirection, currentSpreadDegrees);

        Projectile projectile = bulletPool.Rent(firePoint.position, Quaternion.LookRotation(direction));
        projectile.Launch(direction, equippedWeapon.projectileSpeed, equippedWeapon.attackDamage, equippedWeapon.range, ownerCollider);
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
