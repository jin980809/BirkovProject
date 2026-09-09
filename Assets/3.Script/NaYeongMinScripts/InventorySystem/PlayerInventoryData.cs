using System;

namespace Birdkov.NaYeongMin.InventorySystem
{
    [Serializable]
    public class PlayerInventoryData
    {
        public GridContainerData inventory = new GridContainerData(InventorySettings.InventoryWidth, InventorySettings.InventoryHeight);
        public GridContainerData weaponSlots = new GridContainerData(InventorySettings.WeaponSlotCount, 1);
        public GridContainerData quickSlots = new GridContainerData(InventorySettings.QuickSlotCount, 1);

        public void ClearAll()
        {
            inventory.Clear();
            weaponSlots.Clear();
            quickSlots.Clear();
        }
    }
}
