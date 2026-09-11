using System;

namespace Birdkov.NaYeongMin.InventorySystem
{
    // 퀵슬롯은 컨테이너가 아니므로 이 enum 에 없다.
    public enum PlayerContainerType
    {
        Inventory,
        Equipment
    }

    public sealed class PlayerInventoryService
    {
        private readonly IItemCatalog itemCatalog;
        private readonly InventoryService inventoryService;

        public PlayerInventoryService(IItemCatalog itemCatalog)
        {
            this.itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));
            inventoryService = new InventoryService(itemCatalog);
        }

        // 획득은 항상 가방으로만 들어간다. 자동 착용은 하지 않는다.
        public InventoryMoveResult AddToInventory(PlayerInventoryData playerData, int itemId, int amount)
        {
            if (playerData == null)
            {
                return new InventoryMoveResult(InventoryResult.InvalidSlot, 0, amount);
            }

            // 화폐는 가방 칸을 쓰지 않고 보유 수치에 바로 합산한다. 기획서 10.1 [지푸라기에 대해].
            if (itemCatalog.TryGetItem(itemId, out ItemData currencyItem) && currencyItem.itemType == ItemType.Currency)
            {
                if (amount <= 0)
                {
                    return new InventoryMoveResult(InventoryResult.InvalidAmount, 0, amount);
                }

                playerData.currency += amount;
                return new InventoryMoveResult(InventoryResult.Success, amount, 0);
            }

            InventoryMoveResult result = inventoryService.AddItem(playerData.inventory, itemId, amount);
            SanitizeItemQuickSlots(playerData);
            return result;
        }

        // 장비 착용 -------------------------------------------------------------
        // 가방 -> 장비 슬롯.
        // 기획 확정: 대상 장비 슬롯이 비어 있다는 전제에서만 안착한다.
        // 슬롯이 차 있으면 DestinationRejected 이며 교환하지 않는다.
        // 바꿔 끼우려면 UnequipToInventory 로 먼저 빼야 한다.
        // 무기 슬롯 두 칸이 모두 비어 있으면 어느 칸에 놓든 주 무기(0번)로 들어간다.
        public InventoryMoveResult EquipFromInventory(
            PlayerInventoryData playerData,
            int inventoryIndex,
            int equipmentSlotIndex)
        {
            if (playerData == null || !EquipmentSlots.IsValidSlot(equipmentSlotIndex))
            {
                return new InventoryMoveResult(InventoryResult.InvalidSlot, 0, 1);
            }

            if (!TryGetItem(playerData.inventory, inventoryIndex, out ItemData item))
            {
                return new InventoryMoveResult(InventoryResult.ItemNotFound, 0, 1);
            }

            int targetIndex = ResolveWeaponTargetSlot(playerData, equipmentSlotIndex);
            if (!EquipmentSlots.Accepts(targetIndex, item))
            {
                return new InventoryMoveResult(InventoryResult.DestinationRejected, 0, 1);
            }

            if (!playerData.equipmentSlots.slots[targetIndex].IsEmpty())
            {
                return new InventoryMoveResult(InventoryResult.DestinationRejected, 0, 1);
            }

            InventoryMoveResult result = inventoryService.MoveItem(
                playerData.inventory, inventoryIndex, playerData.equipmentSlots, targetIndex, 1);
            SanitizeItemQuickSlots(playerData);
            return result;
        }

        // 장비 슬롯 -> 가방.
        public InventoryMoveResult UnequipToInventory(
            PlayerInventoryData playerData,
            int equipmentSlotIndex,
            int inventoryIndex)
        {
            if (playerData == null || !EquipmentSlots.IsValidSlot(equipmentSlotIndex))
            {
                return new InventoryMoveResult(InventoryResult.InvalidSlot, 0, 1);
            }

            InventoryMoveResult result = inventoryService.MoveItem(
                playerData.equipmentSlots, equipmentSlotIndex, playerData.inventory, inventoryIndex, 1);
            SanitizeItemQuickSlots(playerData);
            return result;
        }

        // 내구도 0으로 파괴된 장비를 슬롯에서 없앤다. 대체 아이템은 넣지 않는다.
        // 무기 슬롯이면 링크된 무기 퀵슬롯도 자동으로 비게 된다.
        public bool DestroyEquipped(PlayerInventoryData playerData, int equipmentSlotIndex)
        {
            if (playerData == null || !EquipmentSlots.IsValidSlot(equipmentSlotIndex))
            {
                return false;
            }

            GridSlotData slot = playerData.equipmentSlots.slots[equipmentSlotIndex];
            if (slot.IsEmpty())
            {
                return false;
            }

            slot.Clear();
            return true;
        }

        public bool TryGetEquipped(PlayerInventoryData playerData, int equipmentSlotIndex, out ItemData item)
        {
            item = null;
            return playerData != null &&
                   EquipmentSlots.IsValidSlot(equipmentSlotIndex) &&
                   TryGetItem(playerData.equipmentSlots, equipmentSlotIndex, out item);
        }

        // 무기 퀵슬롯 -----------------------------------------------------------
        // 장비 무기 슬롯을 그대로 비추는 링크다. 별도 지정이나 해제가 없다.
        public bool TryGetWeaponQuickSlot(
            PlayerInventoryData playerData,
            int weaponQuickIndex,
            out int equipmentSlotIndex,
            out ItemData item)
        {
            equipmentSlotIndex = InventorySettings.UnassignedQuickSlot;
            item = null;

            if (weaponQuickIndex < 0 || weaponQuickIndex >= InventorySettings.WeaponQuickSlotCount)
            {
                return false;
            }

            if (!TryGetEquipped(playerData, weaponQuickIndex, out item))
            {
                return false;
            }

            equipmentSlotIndex = weaponQuickIndex;
            return true;
        }

        // 아이템 퀵슬롯 ---------------------------------------------------------
        // 가방 슬롯 하나를 퀵슬롯 번호에 연결한다. 아이템은 가방에 그대로 남는다.
        public bool AssignItemQuickSlot(PlayerInventoryData playerData, int quickIndex, int inventoryIndex)
        {
            if (!IsValidItemQuickIndex(playerData, quickIndex) ||
                !TryGetItem(playerData.inventory, inventoryIndex, out ItemData item) ||
                !IsQuickUsable(item))
            {
                return false;
            }

            for (int index = 0; index < playerData.itemQuickSlotIndices.Length; index++)
            {
                if (index != quickIndex && playerData.itemQuickSlotIndices[index] == inventoryIndex)
                {
                    playerData.itemQuickSlotIndices[index] = InventorySettings.UnassignedQuickSlot;
                }
            }

            playerData.itemQuickSlotIndices[quickIndex] = inventoryIndex;
            return true;
        }

        public bool ClearItemQuickSlot(PlayerInventoryData playerData, int quickIndex)
        {
            if (!IsValidItemQuickIndex(playerData, quickIndex))
            {
                return false;
            }

            playerData.itemQuickSlotIndices[quickIndex] = InventorySettings.UnassignedQuickSlot;
            return true;
        }

        public bool TryGetItemQuickSlot(
            PlayerInventoryData playerData,
            int quickIndex,
            out int inventoryIndex,
            out ItemData item)
        {
            inventoryIndex = InventorySettings.UnassignedQuickSlot;
            item = null;

            if (!IsValidItemQuickIndex(playerData, quickIndex))
            {
                return false;
            }

            int mapped = playerData.itemQuickSlotIndices[quickIndex];
            if (mapped == InventorySettings.UnassignedQuickSlot ||
                !TryGetItem(playerData.inventory, mapped, out item) ||
                !IsQuickUsable(item))
            {
                item = null;
                return false;
            }

            inventoryIndex = mapped;
            return true;
        }

        // 아이템이 소모되어 사라지면 그 퀵슬롯은 즉시 비운다.
        public void SanitizeItemQuickSlots(PlayerInventoryData playerData)
        {
            if (playerData == null)
            {
                return;
            }

            if (playerData.itemQuickSlotIndices == null ||
                playerData.itemQuickSlotIndices.Length != InventorySettings.ItemQuickSlotCount)
            {
                playerData.itemQuickSlotIndices = PlayerInventoryData.CreateEmptyItemQuickSlots();
                return;
            }

            for (int index = 0; index < playerData.itemQuickSlotIndices.Length; index++)
            {
                int mapped = playerData.itemQuickSlotIndices[index];
                if (mapped == InventorySettings.UnassignedQuickSlot)
                {
                    continue;
                }

                if (!TryGetItem(playerData.inventory, mapped, out ItemData item) || !IsQuickUsable(item))
                {
                    playerData.itemQuickSlotIndices[index] = InventorySettings.UnassignedQuickSlot;
                }
            }
        }

        // 가방과 장비 슬롯 사이 일반 이동 ---------------------------------------
        public InventoryMoveResult Move(
            PlayerInventoryData playerData,
            PlayerContainerType sourceType,
            int sourceIndex,
            PlayerContainerType destinationType,
            int destinationIndex,
            int amount)
        {
            if (playerData == null)
            {
                return new InventoryMoveResult(InventoryResult.InvalidSlot, 0, amount);
            }

            if (destinationType == PlayerContainerType.Equipment)
            {
                return sourceType == PlayerContainerType.Inventory
                    ? EquipFromInventory(playerData, sourceIndex, destinationIndex)
                    : new InventoryMoveResult(InventoryResult.DestinationRejected, 0, amount);
            }

            if (sourceType == PlayerContainerType.Equipment)
            {
                return UnequipToInventory(playerData, sourceIndex, destinationIndex);
            }

            InventoryMoveResult result = inventoryService.MoveItem(
                playerData.inventory, sourceIndex, playerData.inventory, destinationIndex, amount);
            SanitizeItemQuickSlots(playerData);
            return result;
        }

        public InventoryResult UseRecoveryItem(PlayerInventoryData playerData, int inventoryIndex, IRecoveryTarget target)
        {
            if (playerData?.inventory?.slots == null || inventoryIndex < 0 ||
                inventoryIndex >= playerData.inventory.slots.Count)
            {
                return InventoryResult.InvalidSlot;
            }

            if (!TryGetItem(playerData.inventory, inventoryIndex, out ItemData item))
            {
                return InventoryResult.ItemNotFound;
            }

            if (target == null || item.itemType != ItemType.Consumable ||
                !IsValidRecovery(item.healthRecovery) || !IsValidRecovery(item.hungerRecovery) ||
                !IsValidRecovery(item.waterRecovery) ||
                (item.healthRecovery == 0 && item.hungerRecovery == 0 && item.waterRecovery == 0))
            {
                return InventoryResult.DestinationRejected;
            }

            if (!target.TryApplyRecovery(item.healthRecovery, item.hungerRecovery, item.waterRecovery))
            {
                return InventoryResult.DestinationRejected;
            }

            InventoryMoveResult result = inventoryService.RemoveItem(playerData.inventory, inventoryIndex, 1);
            SanitizeItemQuickSlots(playerData);
            return result.Result;
        }

        private static bool IsValidRecovery(float amount)
        {
            return amount >= 0 && !float.IsNaN(amount) && !float.IsInfinity(amount);
        }

        public void ClearOnDeath(PlayerInventoryData playerData)
        {
            playerData?.ClearAll();
        }

        public static bool IsQuickUsable(ItemData item)
        {
            return item != null && (item.itemType == ItemType.Consumable || item.itemType == ItemType.Special);
        }

        // ----------------------------------------------------------------------
        private static int ResolveWeaponTargetSlot(PlayerInventoryData playerData, int equipmentSlotIndex)
        {
            if (!EquipmentSlots.IsWeaponSlot(equipmentSlotIndex))
            {
                return equipmentSlotIndex;
            }

            bool primaryEmpty = playerData.equipmentSlots.slots[EquipmentSlots.PrimaryWeapon].IsEmpty();
            bool secondaryEmpty = playerData.equipmentSlots.slots[EquipmentSlots.SecondaryWeapon].IsEmpty();

            return primaryEmpty && secondaryEmpty ? EquipmentSlots.PrimaryWeapon : equipmentSlotIndex;
        }

        private static bool IsValidItemQuickIndex(PlayerInventoryData playerData, int quickIndex)
        {
            return playerData?.itemQuickSlotIndices != null &&
                   quickIndex >= 0 &&
                   quickIndex < playerData.itemQuickSlotIndices.Length;
        }

        private bool TryGetItem(GridContainerData container, int index, out ItemData item)
        {
            item = null;
            GridSlotData slot = GetSlot(container, index);
            return slot != null && !slot.IsEmpty() && itemCatalog.TryGetItem(slot.itemId, out item);
        }

        private static GridSlotData GetSlot(GridContainerData container, int index)
        {
            return container != null && index >= 0 && index < container.slots.Count
                ? container.slots[index]
                : null;
        }
    }
}
