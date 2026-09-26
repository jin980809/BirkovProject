using Birdkov.NaYeongMin.Integration;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.InventoryTest;
using UnityEngine;

// InventoryTestBench(NaYeongMin 의 실제 데이터/UI)를 내 Player 시스템에 연결하는 어댑터.
// NaYeongMin 파일은 건드리지 않고 공개 API만 쓴다.
//
//  1) WeaponInventoryBridge 가 무기 퀵슬롯(1/2)을 조회할 때, 자기만의 임시 데이터가 아니라
//     이 벤치의 진짜 PlayerInventoryData(PlayerData 프로퍼티)를 보게 한다 - 그래야 실제로
//     인벤토리에 드래그해 넣은 무기가 1/2 키로 장착된다.
//  2) 소모품(3/4/5)을 쓸 때 실제 PlayerVitals(체력/허기/수분)에 적용되도록 IRecoveryTarget 을
//     구현해서 벤치에 등록한다 (BindRecoveryTarget).
//  3) 무기 퀵슬롯(1/2) 키를 눌렀을 때도 클릭했을 때와 똑같이 "선택됨" 표시가 뜨도록
//     inventoryBench.SelectWeapon() 을 같이 호출한다 (그 메서드는 클릭 시 호출되는 것과 동일 - UI 하이라이트만 갱신, 실제 장착은 WeaponController 가 따로 한다).
//
// 퀵슬롯(3/4/5) 키로 소모품을 실제로 쓰는 로직은 여기 없다 - ItemUseController.cs 가 담당한다
// (사용 시간(usageTime)만큼 게이지가 필요할 수 있어서, 즉시 호출이 아니라 별도 컴포넌트로 뺐다).
// InventoryTestBench.useStandaloneKeyboard 는 "단독 테스트에서만 사용, 팀원 입력 이벤트 연결 시
// 끌 것"이라고 주석에 명시돼 있으므로(실제로 그 플래그에 의존하면 안 됨) 인스펙터에서 꺼둬야 한다.
public class InventoryTestBenchLink : MonoBehaviour, IRecoveryTarget
{
    [SerializeField] private InventoryTestBench inventoryBench;
    [SerializeField] private WeaponInventoryBridge weaponInventoryBridge;
    [SerializeField] private PlayerArmorBridge armorBridge;
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private PlayerVitals playerVitals;
    [SerializeField] private PlayerInputHandler input;
    [SerializeField] private PlayerController player;
    [Tooltip("총기 내구도를 깎는 NaYeongMin 의 WeaponDurabilityBridge. 비우면 같은 오브젝트에서 찾고, 없으면 쓰지 않는다")]
    [SerializeField] private WeaponDurabilityBridge durabilityBridge;
    [Tooltip("피격 시 방어구 내구도를 깎는 NaYeongMin 의 ArmorDurabilityBridge. 비우면 같은 오브젝트에서 찾고, 없으면 쓰지 않는다")]
    [SerializeField] private ArmorDurabilityBridge armorDurabilityBridge;

    private bool didLinkWeaponData;
    private PlayerInventoryData linkedData;

    // 벤치는 Awake 가 아니라 여기서 찾는다 - 씬 전환으로 들어온 경우 이 씬에 있던 중복 UI 캔버스를
    // PersistentUiRoot 가 Awake 에서 비활성화하므로, 그 뒤인 Start 에서 찾아야 살아남은 벤치가 잡힌다.
    // (Awake 에서 찾으면 곧 파괴될 중복을 잡아 회복 아이템/무기 데이터 연결이 끊긴다)
    private void Start()
    {
        if (inventoryBench == null || !inventoryBench.gameObject.activeInHierarchy)
        {
            inventoryBench = (PersistentUiRoot.Find<InventoryTestBench>() ?? FindAnyObjectByType<InventoryTestBench>());
            didLinkWeaponData = false;
        }

        if (inventoryBench != null)
        {
            inventoryBench.BindRecoveryTarget(this);
        }
    }

    private void Awake()
    {

        if (weaponInventoryBridge == null)
        {
            weaponInventoryBridge = FindAnyObjectByType<WeaponInventoryBridge>();
        }

        if (armorBridge == null)
        {
            // 같은 오브젝트에서 먼저 찾는다 - PlayerVitals 도 같은 방식으로 찾으므로,
            // Player 에 PlayerArmorBridge 가 두 개 붙어 있어도 양쪽이 같은(첫 번째) 것을 쓴다.
            if (!TryGetComponent(out armorBridge))
            {
                armorBridge = FindAnyObjectByType<PlayerArmorBridge>();
            }
        }

        if (weaponController == null)
        {
            weaponController = FindAnyObjectByType<WeaponController>();
        }

        if (playerVitals == null)
        {
            playerVitals = FindAnyObjectByType<PlayerVitals>();
        }

        if (input == null)
        {
            input = FindAnyObjectByType<PlayerInputHandler>();
        }

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
        }

        if (durabilityBridge == null)
        {
            TryGetComponent(out durabilityBridge);
        }

        if (armorDurabilityBridge == null)
        {
            TryGetComponent(out armorDurabilityBridge);
        }

        DisableDurabilityInLobby();
    }

    // 로비용(WeaponController.IsLobby)이면 총기 내구도가 닳지 않게 WeaponDurabilityBridge 를 끈다.
    // 그 브리지는 발사 이벤트를 구독해서 내구도를 깎으므로, 꺼 두면 구독이 풀려 아무것도 닳지 않는다.
    private void DisableDurabilityInLobby()
    {
        if (durabilityBridge != null && weaponController != null && weaponController.IsLobby && durabilityBridge.enabled)
        {
            durabilityBridge.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (inventoryBench != null)
        {
            inventoryBench.BindRecoveryTarget(this);
        }

        if (input != null)
        {
            input.WeaponSelected += HandleWeaponSelected;
        }
    }

    private void OnDisable()
    {
        // 씬 전환으로 이 플레이어가 파괴되는 중이라면 연결을 끊지 않는다. 벤치는 씬을 넘어 살아있고,
        // 새 씬 플레이어가 이미 자기 자신을 등록했을 수 있어서 여기서 null 로 덮으면 회복이 안 먹는다.
        if (inventoryBench != null && gameObject.scene.isLoaded)
        {
            inventoryBench.BindRecoveryTarget(null);
        }

        if (input != null)
        {
            input.WeaponSelected -= HandleWeaponSelected;
        }
    }

    private void HandleWeaponSelected(int slotIndex)
    {
        // 실제 장착이 막히는 상황(재장전/상호작용/아이템 사용 등)에서는 UI 선택 표시도 바꾸지 않는다 -
        // 안 그러면 손에 든 무기는 그대로인데 퀵슬롯 하이라이트만 넘어가서 교체된 것처럼 보인다.
        if (inventoryBench != null && player != null && player.CanSwapWeapon)
        {
            inventoryBench.SelectWeapon(slotIndex); // 실제 장착은 WeaponController(PlayerController.HandleWeaponSelected)가 따로 처리, 여기선 UI 하이라이트만
        }
    }

    private void Update()
    {
        // WeaponDurabilityBridge / ArmorDurabilityBridge 둘 다 자기 벤치 참조를 스스로 다시 찾지 않는다.
        // 씬을 옮기면 그 씬에 있던 벤치가 중복으로 파괴되면서 참조가 죽어 내구도가 더는 안 닳으므로,
        // 살아있는 벤치를 여기서 계속 맞춰 준다.
        if (durabilityBridge != null && inventoryBench != null && durabilityBridge.inventoryBench != inventoryBench)
        {
            durabilityBridge.inventoryBench = inventoryBench;
        }

        if (armorDurabilityBridge != null && inventoryBench != null && armorDurabilityBridge.inventoryBench != inventoryBench)
        {
            armorDurabilityBridge.inventoryBench = inventoryBench;
        }

        DisableDurabilityInLobby();

        // 저장 불러오기나 초기화로 벤치의 PlayerInventoryData 가 새 객체로 바뀌면 그 데이터로 다시 연결한다.
        // 안 그러면 무기/방어구 브리지가 옛 데이터를 계속 봐서 불러온 장비가 장착되지 않는다.
        if (didLinkWeaponData && inventoryBench != null && inventoryBench.IsReady &&
            !ReferenceEquals(inventoryBench.PlayerData, linkedData))
        {
            didLinkWeaponData = false;
        }

        // InventoryTestBench 가 자기 데이터를 준비하는 타이밍이 이 스크립트의 초기화보다
        // 늦을 수 있어서, 준비될 때까지 매 프레임 확인하다가 딱 한 번만 연결한다.
        if (didLinkWeaponData || inventoryBench == null || weaponInventoryBridge == null)
        {
            return;
        }

        if (inventoryBench.IsReady)
        {
            didLinkWeaponData = true;
            linkedData = inventoryBench.PlayerData;
            weaponInventoryBridge.SetPlayerInventoryData(inventoryBench.PlayerData);

            if (armorBridge != null)
            {
                armorBridge.SetPlayerInventoryData(inventoryBench.PlayerData);
            }

            // WeaponController.Start() 는 이 데이터가 도착하기 전에 이미 EquipSlot(0) 을 시도해서
            // 실패했을 수 있으니, 진짜 데이터가 연결된 지금 다시 장착을 시도한다.
            if (weaponController != null)
            {
                weaponController.RefreshEquippedWeapon();
            }
        }
    }

    public bool TryApplyRecovery(float healthPercent, float hungerPercent, float waterPercent)
    {
        if (playerVitals == null || playerVitals.IsDead)
        {
            return false;
        }

        float health = playerVitals.Health;
        float hunger = playerVitals.Hunger;
        float water = playerVitals.Water;

        playerVitals.Heal(playerVitals.MaxHealth * healthPercent / 100f);
        playerVitals.RestoreHunger(playerVitals.MaxHunger * hungerPercent / 100f);
        playerVitals.RestoreWater(playerVitals.MaxWater * waterPercent / 100f);

        return playerVitals.Health > health || playerVitals.Hunger > hunger || playerVitals.Water > water;
    }
}
