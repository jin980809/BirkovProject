using System;

namespace Birdkov.NaYeongMin.InventorySystem
{
    [Serializable]
    public class LootContainerData
    {
        public GridContainerData loot = new GridContainerData(InventorySettings.LootSlotCount, 1);

        public bool IsEmpty()
        {
            foreach (GridSlotData slot in loot.slots)
            {
                if (!slot.IsEmpty())
                {
                    return false;
                }
            }

            return true;
        }

        public void Clear()
        {
            loot.Clear();
        }
    }
}
