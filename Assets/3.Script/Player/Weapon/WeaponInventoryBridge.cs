using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;

// NaYeongMin 인벤토리 시스템과 무기 시스템을 잇는 어댑터.
// NaYeongMin 쪽 파일은 전혀 건드리지 않고, 공개 API(PlayerInventoryService 등)만 참조한다.
// (NaYeongMinScripts/README.txt: "연결 어댑터는 각 담당자 머지 후 추가한다")
public class WeaponInventoryBridge : MonoBehaviour
{
    [SerializeField] private ItemDatabase itemDatabase;

    private PlayerInventoryService playerInventoryService;
    private InventoryService gridService;
    private PlayerInventoryData playerData;

    private void Awake()
    {
        if (itemDatabase == null)
        {
            itemDatabase = FindAnyObjectByType<ItemDatabase>();
        }

        // InventoryTestBenchLink.cs 가 준비되는 대로 SetPlayerInventoryData 로 진짜 데이터를 주입한다.
        // 그 전까지(또는 벤치가 아예 없을 때) 쓰는 임시 빈 데이터.
        playerData = new PlayerInventoryData();
    }

    // ItemDatabase 는 자기 Awake() 에서 CSV 를 읽어 Catalog 를 채우는데, 이 컴포넌트의 Awake() 가
    // 그보다 먼저 실행되면 Catalog 가 아직 null 이라 PlayerInventoryService 생성자가 예외를 던진다.
    // 그래서 여기서 즉시 만들지 않고, 실제로 필요할 때(아래 두 공개 메서드) 딱 한 번만 만든다.
    private void EnsureServices()
    {
        // 씬이 바뀌면 이전 씬의 ItemDatabase 가 파괴되므로, 참조가 죽었으면 지금 씬 것으로 다시 찾는다
        if (itemDatabase == null)
        {
            itemDatabase = FindAnyObjectByType<ItemDatabase>();
        }

        if (playerInventoryService != null || itemDatabase == null || itemDatabase.Catalog == null)
        {
            return;
        }

        IItemCatalog catalog = itemDatabase.Catalog;
        playerInventoryService = new PlayerInventoryService(catalog);
        gridService = new InventoryService(catalog);
    }

    // 세이브에서 불러온 실제 데이터로 교체할 때 호출
    public void SetPlayerInventoryData(PlayerInventoryData data)
    {
        playerData = data;
    }

    // weaponSlotIndex: 0 = 주 무기, 1 = 보조 무기 (무기 퀵슬롯과 동일)
    public bool TryGetEquippedWeapon(int weaponSlotIndex, out ItemData weapon)
    {
        EnsureServices();

        weapon = null;
        return playerInventoryService != null &&
               playerInventoryService.TryGetWeaponQuickSlot(playerData, weaponSlotIndex, out _, out weapon);
    }

    // 무기가 들어있는 장비 슬롯 데이터 자체를 돌려준다 (없거나 잘못된 인덱스면 null).
    // WeaponController 가 이 슬롯의 remainingRounds 에 무기별 잔탄을 기록한다 - 슬롯 데이터는
    // 가방/창고/전리품으로 옮길 때 remainingRounds 까지 같이 옮겨지므로 잔탄이 무기를 따라다닌다.
    // weaponSlotIndex 0/1 은 장비 슬롯 EquipmentSlots.PrimaryWeapon/SecondaryWeapon 과 같은 번호다.
    public GridSlotData GetWeaponSlotData(int weaponSlotIndex)
    {
        GridSlotData slot = null;
        if (playerData != null && EquipmentSlots.IsWeaponSlot(weaponSlotIndex))
        {
            slot = playerData.equipmentSlots.slots[weaponSlotIndex];
        }

        return slot;
    }

    // 가방에 흩어진 같은 탄약 itemId 재고를 소모 없이 합산만 한다 - 재장전 게이지를 시작하기 전에
    // 실제로 탄약이 있는지 미리 확인하기 위함 (없으면 게이지를 아예 시작하지 않는다).
    public int PeekAmmoCount(int ammoItemId)
    {
        if (playerData == null)
        {
            return 0;
        }

        int total = 0;
        List<GridSlotData> slots = playerData.inventory.slots;
        for (int i = 0; i < slots.Count; i++)
        {
            GridSlotData slot = slots[i];
            if (slot.itemId == ammoItemId && !slot.IsEmpty())
            {
                total += slot.amount;
            }
        }

        return total;
    }

    // 가방에 흩어진 같은 탄약 itemId 재고를 합산해서 최대 amount 만큼 소모한다.
    // 반환값: 실제로 소모(확보)한 수량 (재고가 모자라면 그만큼만).
    public int ConsumeAmmo(int ammoItemId, int amount)
    {
        EnsureServices();

        if (gridService == null || playerData == null || amount <= 0)
        {
            return 0;
        }

        int remaining = amount;
        List<GridSlotData> slots = playerData.inventory.slots;

        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            GridSlotData slot = slots[i];
            if (slot.itemId != ammoItemId || slot.IsEmpty())
            {
                continue;
            }

            InventoryMoveResult result = gridService.RemoveItem(playerData.inventory, i, remaining);
            remaining -= result.MovedAmount;
        }

        return amount - remaining;
    }
}
