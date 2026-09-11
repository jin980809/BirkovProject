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
            Debug.LogError("WeaponInventoryBridge: ItemDatabase 가 필요합니다.", this);
            return;
        }

        IItemCatalog catalog = itemDatabase.Catalog;
        playerInventoryService = new PlayerInventoryService(catalog);
        gridService = new InventoryService(catalog);

        // TODO: 세이브/로드 시스템이 실제 PlayerInventoryData 를 넘겨주면 SetPlayerInventoryData 로 교체한다
        playerData = new PlayerInventoryData();
    }

    // 세이브에서 불러온 실제 데이터로 교체할 때 호출
    public void SetPlayerInventoryData(PlayerInventoryData data)
    {
        playerData = data;
    }

    // weaponSlotIndex: 0 = 주 무기, 1 = 보조 무기 (무기 퀵슬롯과 동일)
    public bool TryGetEquippedWeapon(int weaponSlotIndex, out ItemData weapon)
    {
        weapon = null;
        return playerInventoryService != null &&
               playerInventoryService.TryGetWeaponQuickSlot(playerData, weaponSlotIndex, out _, out weapon);
    }

    // 가방에 흩어진 같은 탄약 itemId 재고를 합산해서 최대 amount 만큼 소모한다.
    // 반환값: 실제로 소모(확보)한 수량 (재고가 모자라면 그만큼만).
    public int ConsumeAmmo(int ammoItemId, int amount)
    {
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
