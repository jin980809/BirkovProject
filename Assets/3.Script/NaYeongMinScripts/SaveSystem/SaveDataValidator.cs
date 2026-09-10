using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;

namespace Birdkov.NaYeongMin.SaveSystem
{
    public static class SaveDataValidator
    {
        // 2: 퀵슬롯이 컨테이너에서 가방 인덱스 매핑으로 바뀜.
        // 3: 무기 슬롯 5칸이 장비 슬롯 4칸(무기2 / 머리 / 몸)으로 바뀜.
        // 이전 버전 세이브는 로드하지 않는다. 마이그레이션은 만들지 않았다.
        public const int CurrentSaveVersion = 3;

        public static bool IsValid(PlayerSaveData data)
        {
            return data != null &&
                   data.saveVersion == CurrentSaveVersion &&
                   IsContainerValid(
                       data.inventoryData?.inventory,
                       InventorySettings.InventoryWidth,
                       InventorySettings.InventoryHeight) &&
                   IsContainerValid(
                       data.inventoryData?.equipmentSlots,
                       InventorySettings.EquipmentSlotCount,
                       1) &&
                   AreItemQuickSlotsValid(data.inventoryData) &&
                   IsContainerValid(data.warehouseData);
        }

        private static bool AreItemQuickSlotsValid(PlayerInventoryData inventoryData)
        {
            int[] indices = inventoryData?.itemQuickSlotIndices;
            if (indices == null || indices.Length != InventorySettings.ItemQuickSlotCount)
            {
                return false;
            }

            foreach (int index in indices)
            {
                if (index != InventorySettings.UnassignedQuickSlot &&
                    (index < 0 || index >= InventorySettings.InventorySlotCount))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsContainerValid(GridContainerData container, int expectedWidth, int expectedHeight)
        {
            return container != null &&
                   container.width == expectedWidth &&
                   container.height == expectedHeight &&
                   IsSlotListValid(container.slots, expectedWidth * expectedHeight);
        }

        private static bool IsContainerValid(GridContainerData container)
        {
            if (container == null || container.width < 0 || container.height < 0)
            {
                return false;
            }

            return IsSlotListValid(container.slots, container.width * container.height);
        }

        private static bool IsSlotListValid(List<GridSlotData> slots, int expectedCount)
        {
            if (slots == null || slots.Count != expectedCount)
            {
                return false;
            }

            foreach (GridSlotData slot in slots)
            {
                if (slot == null)
                {
                    return false;
                }

                bool empty = slot.itemId < 0 && slot.amount == 0;
                bool occupied = slot.itemId >= 0 && slot.amount > 0;
                if (!empty && !occupied)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
