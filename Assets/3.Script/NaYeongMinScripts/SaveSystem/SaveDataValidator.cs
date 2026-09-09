using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;

namespace Birdkov.NaYeongMin.SaveSystem
{
    public static class SaveDataValidator
    {
        public static bool IsValid(PlayerSaveData data)
        {
            return data != null &&
                   data.saveVersion == 1 &&
                   IsContainerValid(
                       data.inventoryData?.inventory,
                       InventorySettings.InventoryWidth,
                       InventorySettings.InventoryHeight) &&
                   IsContainerValid(
                       data.inventoryData?.weaponSlots,
                       InventorySettings.WeaponSlotCount,
                       1) &&
                   IsContainerValid(
                       data.inventoryData?.quickSlots,
                       InventorySettings.QuickSlotCount,
                       1) &&
                   IsContainerValid(data.warehouseData);
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
