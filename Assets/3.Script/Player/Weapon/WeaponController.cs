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
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Collider ownerCollider; // 자기 자신과의 충돌 무시용, 비우면 자동 탐색

    [Header("탄약 매핑 (나중에 여기서 자유롭게 추가/수정)")]
    [SerializeField] private WeaponAmmoMapping[] ammoMappings = Array.Empty<WeaponAmmoMapping>();

    private PlayerController player;
    private PlayerNoise noise;

    private ItemData equippedWeapon;
    private int equippedSlotIndex = -1;
    private readonly int[] ammoInMagazine = new int[InventorySettings.WeaponQuickSlotCount];

    private bool fireHeld;
    private float nextFireReadyTime;

    public bool HasWeaponEquipped
    {
        get { return equippedWeapon != null; }
    }

    public int CurrentAmmo
    {
        get { return equippedSlotIndex >= 0 ? ammoInMagazine[equippedSlotIndex] : 0; }
    }

    private void Awake()
    {
        TryGetComponent(out player);
        TryGetComponent(out noise);

        if (inventoryBridge == null)
        {
            inventoryBridge = GetComponent<WeaponInventoryBridge>();
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
        if (inventoryBridge == null || !inventoryBridge.TryGetEquippedWeapon(slotIndex, out ItemData weapon))
        {
            equippedWeapon = null;
            equippedSlotIndex = -1;
        }
        else
        {
            equippedWeapon = weapon;
            equippedSlotIndex = slotIndex;
        }

        nextFireReadyTime = 0f; // 무기를 바꾸면 발사 쿨다운은 리셋

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

        int pellets = Mathf.Max(1, equippedWeapon.pelletCount);
        for (int i = 0; i < pellets; i++)
        {
            FireProjectile();
        }

        if (noise != null)
        {
            noise.EmitGunshotNoise();
        }
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null)
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

        Vector3 direction = ApplySpread(baseDirection, equippedWeapon.maxSpread);

        Projectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
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
