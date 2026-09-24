using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.InventoryTest;
using UnityEngine;

// 소모품(3/4/5) 사용에 시간이 걸리게 한다 (ItemData.usageTime, 0 이하면 즉시 사용).
// 사용 중에는 상자 UI 같은 전체 잠금(SetMovementLocked)을 쓰지 않는다 - 이동/회전/시야는 그대로
// 되고, 달리기/구르기/사격/상호작용/인벤토리 열기만 개별적으로 막힌다 (PlayerController.IsUsingItem
// 을 CanFire/HandleDodge/HandleInteract/IsSprinting 이 직접 확인하고, PlayerInventoryToggle 도
// 인벤토리 열기를 막을 때 확인한다). 무기는 SetArmed(false) 로 시각적으로만 집어넣는다(실제 장착
// 데이터는 그대로 - 끝나면 원래 장착 상태로 되돌린다). 게이지는 기존 상호작용 게이지
// (InteractionPromptUI.progressSlider)를 그대로 재사용한다 (IsUsing/UseProgress01 을 같이 읽는다).
// ESC 로 취소 가능 - 취소하면 아이템은 소모되지 않고 효과도 적용되지 않는다.
//
// NaYeongMin 파일은 건드리지 않고 공개 API(PlayerInventoryService, InventoryTestBench)만 쓴다.
[RequireComponent(typeof(PlayerController))]
public class ItemUseController : MonoBehaviour
{
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private InventoryTestBench inventoryBench;
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private PlayerVitals playerVitals;

    private PlayerController player;
    private PlayerInputHandler input;
    private PlayerInventoryService peekService; // 조회 전용 - 실제 소모/효과는 inventoryBench 가 처리

    private int usingSlotIndex;
    private float useTimer;
    private float useDuration;

    public bool IsUsing { get; private set; }

    // UI 가 읽는 0~1 진행률
    public float UseProgress01
    {
        get { return useDuration > 0f ? Mathf.Clamp01(useTimer / useDuration) : 0f; }
    }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        TryGetComponent(out input);

        if (itemDatabase == null)
        {
            itemDatabase = FindAnyObjectByType<ItemDatabase>();
        }

        if (inventoryBench == null)
        {
            inventoryBench = FindAnyObjectByType<InventoryTestBench>();
        }

        if (weaponController == null)
        {
            weaponController = FindAnyObjectByType<WeaponController>();
        }

        if (playerVitals == null)
        {
            playerVitals = FindAnyObjectByType<PlayerVitals>();
        }

        // peekService 생성은 여기서 하지 않는다 - ItemDatabase 는 자기 Awake() 에서 CSV 를 읽어
        // Catalog 를 채우는데, 이 컴포넌트의 Awake() 가 그보다 먼저 실행되면 Catalog 가 아직 null 이라
        // PlayerInventoryService 생성자가 예외를 던진다. 실제로 필요할 때(EnsurePeekService) 만든다.
    }

    // itemDatabase.Catalog 가 준비된 뒤 딱 한 번만 생성한다 (그 전까진 매번 호출해도 안전하게 그냥 리턴)
    private void EnsurePeekService()
    {
        // 씬이 바뀌면 이전 씬의 ItemDatabase 가 파괴되므로, 참조가 죽었으면 지금 씬 것으로 다시 찾는다
        if (itemDatabase == null)
        {
            itemDatabase = FindAnyObjectByType<ItemDatabase>();
        }

        if (peekService != null || itemDatabase == null || itemDatabase.Catalog == null)
        {
            return;
        }

        peekService = new PlayerInventoryService(itemDatabase.Catalog);
    }

    private void OnEnable()
    {
        if (input != null)
        {
            input.QuickSlotUsed += HandleQuickSlotUsed;
            input.CancelPressed += HandleCancel;
        }
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.QuickSlotUsed -= HandleQuickSlotUsed;
            input.CancelPressed -= HandleCancel;
        }
    }

    private void Update()
    {
        if (!IsUsing)
        {
            return;
        }

        useTimer += Time.deltaTime;
        if (useTimer >= useDuration)
        {
            EndUse(applyResult: true);
        }
    }

    // 씬 전환 직후에는 이 씬에 있다가 곧 파괴되는 중복 벤치를 잡았을 수 있다 (PersistentUiRoot 참고).
    // 참조가 죽었으면 살아남은 벤치로 다시 찾는다.
    private void EnsureBench()
    {
        if (inventoryBench == null)
        {
            inventoryBench = FindAnyObjectByType<InventoryTestBench>();
        }
    }

    private void HandleQuickSlotUsed(int slotIndex)
    {
        EnsureBench();

        if (IsUsing || inventoryBench == null || player == null || player.IsControlLocked || player.IsReloading)
        {
            return;
        }

        // 슬롯이 비었거나, usageTime 이 없거나, 어차피 효과가 없을 아이템(예: 체력이 이미 꽉 참)이면
        // 게이지 없이 바로 처리한다 - 어차피 거부될 걸 미리 알면서 시간을 낭비시키지 않는다.
        // (효과 없음/거부 메시지는 UseItemQuickSlot 내부(NaYeongMin 쪽)가 알아서 띄운다.)
        if (!TryGetQuickSlotItem(slotIndex, out ItemData item) || item.usageTime <= 0f || !WouldHaveEffect(item))
        {
            inventoryBench.UseItemQuickSlot(slotIndex);
            return;
        }

        usingSlotIndex = slotIndex;
        useDuration = item.usageTime;
        useTimer = 0f;
        IsUsing = true;

        player.SetArmed(false); // 시각적으로만 집어넣는다 - 장착 데이터는 그대로 유지
    }

    // 이 아이템을 지금 써도 실제로 뭔가 변화가 있을지 미리 확인한다 (PlayerInventoryService.
    // UseRecoveryItem 이 나중에 거부할 걸 게이지 시작 전에 미리 걸러내기 위함).
    // 소비 아이템이 아니면(Special 등, 회복 로직 대상이 아님) 이 최적화를 적용하지 않고 그냥 진행한다.
    private bool WouldHaveEffect(ItemData item)
    {
        if (item.itemType != ItemType.Consumable || playerVitals == null)
        {
            return true;
        }

        bool canHeal = item.healthRecovery > 0f && playerVitals.Health < playerVitals.MaxHealth;
        bool canRestoreHunger = item.hungerRecovery > 0f && playerVitals.Hunger < playerVitals.MaxHunger;
        bool canRestoreWater = item.waterRecovery > 0f && playerVitals.Water < playerVitals.MaxWater;

        return canHeal || canRestoreHunger || canRestoreWater;
    }

    private void HandleCancel()
    {
        if (IsUsing)
        {
            EndUse(applyResult: false);
        }
    }

    private void EndUse(bool applyResult)
    {
        int slotIndex = usingSlotIndex;

        IsUsing = false;
        useTimer = 0f;
        useDuration = 0f;

        player.SetArmed(weaponController != null && weaponController.HasWeaponEquipped); // 원래 무기 상태로 복구

        if (applyResult && inventoryBench != null)
        {
            inventoryBench.UseItemQuickSlot(slotIndex);
        }
    }

    // 해당 퀵슬롯에 실제 아이템이 들어있는지 조회한다. 아직 usageTime 이 CSV 에서 안 읽히는
    // 상태여도(ItemData.usageTime 기본값 0) item 자체는 정상적으로 반환되므로, 위에서
    // item.usageTime <= 0f 체크가 즉시 사용 경로를 타서 지금까지와 동일하게 동작한다.
    private bool TryGetQuickSlotItem(int slotIndex, out ItemData item)
    {
        EnsurePeekService();

        item = null;

        if (peekService == null || inventoryBench == null)
        {
            return false;
        }

        PlayerInventoryData data = inventoryBench.PlayerData;
        if (data == null)
        {
            return false;
        }

        return peekService.TryGetItemQuickSlot(data, slotIndex, out _, out item) && item != null;
    }
}
