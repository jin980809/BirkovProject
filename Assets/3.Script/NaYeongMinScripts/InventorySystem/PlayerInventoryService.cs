using System.Collections.Generic;
using System;

// 플레이어 인벤토리. 장비 착용, 퀵슬롯 매핑, 회복 사용, 사망 처리.
namespace Birdkov.NaYeongMin.InventorySystem
{
        // ---- IRecoveryTarget ----
    public interface IRecoveryTarget
    {
        // 절대 회복량. 실제 회복 적용 시에만 true. false이면 상태를 변경하지 않는다.
        // 동기 호출이며 인벤토리 변경/재진입은 금지. 최대치 판정은 구현 측 담당.
        // 인자는 최대치 대비 백분율(0~100)이다. 기획서 10.1 회복량 표기 기준.
        bool TryApplyRecovery(float healthPercent, float hungerPercent, float waterPercent);
    }

        // ---- PlayerInventoryService ----
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
            return amount >= 0 && amount <= 100 && !float.IsNaN(amount) && !float.IsInfinity(amount);
        }

        // 탄약 1박스에 든 발 수. CSV 의 magazineSize 를 박스 용량으로 읽는다. 기획서 9.2 : 1박스 = 20발.
        public int GetRoundsPerBox(int ammoItemId)
        {
            ItemData ammo;
            if (itemCatalog == null || !itemCatalog.TryGetItem(ammoItemId, out ammo) ||
                ammo.itemType != ItemType.Ammo || ammo.magazineSize <= 0)
            {
                return 0;
            }

            return ammo.magazineSize;
        }

        // 가방에 남은 총 발 수. 뜯다 만 박스의 잔탄까지 합산한다.
        public int GetAmmoRounds(PlayerInventoryData playerData, int ammoItemId)
        {
            int roundsPerBox = GetRoundsPerBox(ammoItemId);
            if (playerData == null || playerData.inventory == null || playerData.inventory.slots == null ||
                roundsPerBox <= 0)
            {
                return 0;
            }

            int total = 0;
            foreach (GridSlotData slot in playerData.inventory.slots)
            {
                if (slot.itemId != ammoItemId || slot.amount <= 0)
                {
                    continue;
                }

                total += slot.remainingRounds > 0
                    ? (slot.amount - 1) * roundsPerBox + slot.remainingRounds
                    : slot.amount * roundsPerBox;
            }

            return total;
        }

        // 발 단위로 소비한다. 반환값은 탄창에 채울 발 수.
        // 뜯다 만 박스를 먼저 쓰고, 새로 뜯은 박스에 남는 발은 같은 슬롯에 되돌린다. 기획 승인 b안.
        public int ConsumeAmmoRounds(PlayerInventoryData playerData, int ammoItemId, int requestedRounds)
        {
            int roundsPerBox = GetRoundsPerBox(ammoItemId);
            if (playerData == null || playerData.inventory == null || playerData.inventory.slots == null ||
                requestedRounds <= 0 || roundsPerBox <= 0)
            {
                return 0;
            }

            List<GridSlotData> slots = playerData.inventory.slots;
            int gained = 0;

            for (int index = 0; index < slots.Count && gained < requestedRounds; index++)
            {
                GridSlotData slot = slots[index];
                if (slot.itemId != ammoItemId || slot.remainingRounds <= 0)
                {
                    continue;
                }

                int taken = Math.Min(slot.remainingRounds, requestedRounds - gained);
                gained += taken;
                slot.remainingRounds -= taken;
                if (slot.remainingRounds == 0)
                {
                    DiscardOneBox(slot);
                }
            }

            for (int index = 0; index < slots.Count && gained < requestedRounds; index++)
            {
                GridSlotData slot = slots[index];
                while (slot.itemId == ammoItemId && slot.amount > 0 && slot.remainingRounds == 0 &&
                       gained < requestedRounds)
                {
                    int taken = Math.Min(roundsPerBox, requestedRounds - gained);
                    gained += taken;

                    if (taken < roundsPerBox)
                    {
                        slot.remainingRounds = roundsPerBox - taken;
                        break;
                    }

                    DiscardOneBox(slot);
                }
            }

            return gained;
        }

        // 다 쓴 박스 하나를 슬롯에서 덜어낸다. 마지막 박스였다면 슬롯을 비운다.
        private static void DiscardOneBox(GridSlotData slot)
        {
            slot.remainingRounds = 0;
            slot.amount--;
            if (slot.amount <= 0)
            {
                slot.Clear();
            }
        }

        public void ClearOnDeath(PlayerInventoryData playerData)
        {
            if (playerData == null) return;
            playerData.inventory.Clear();
            playerData.equipmentSlots.Clear();
            playerData.ClearItemQuickSlots();
            // 지푸라기는 사망해도 유지. 전체 초기화(ClearAll)와 분리한다.
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
