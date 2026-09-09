using System;
using Birdkov.NaYeongMin.InventorySystem;

namespace Birdkov.NaYeongMin.SaveSystem
{
    [Serializable]
    public class PlayerSaveData
    {
        public int saveVersion = 1;
        public PlayerInventoryData inventoryData = new PlayerInventoryData();
        public GridContainerData warehouseData = new GridContainerData();
    }
}
