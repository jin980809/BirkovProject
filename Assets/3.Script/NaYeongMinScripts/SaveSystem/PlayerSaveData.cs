using System;
using Birdkov.NaYeongMin.InventorySystem;

namespace Birdkov.NaYeongMin.SaveSystem
{
    [Serializable]
    public class PlayerSaveData
    {
        public int saveVersion = SaveDataValidator.CurrentSaveVersion;

        public PlayerInventoryData inventoryData = new PlayerInventoryData();

        // 허브 창고. WarehouseService 에 그대로 넘겨 사용한다.
        public GridContainerData warehouseData = new GridContainerData(
            InventorySettings.WarehouseWidth,
            InventorySettings.WarehouseHeight);
    }
}
