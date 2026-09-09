using System;

namespace Birdkov.NaYeongMin.InventorySystem
{
    [Serializable]
    public class GridSlotData
    {
        public int itemId = -1;
        public int amount;

        public bool IsEmpty()
        {
            return itemId < 0 || amount <= 0;
        }

        public void Clear()
        {
            itemId = -1;
            amount = 0;
        }
    }
}
