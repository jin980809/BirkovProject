using System;
using System.Collections.Generic;

namespace Birdkov.NaYeongMin.InventorySystem
{
    [Serializable]
    public class GridContainerData
    {
        public int width;
        public int height;
        public List<GridSlotData> slots = new List<GridSlotData>();

        public GridContainerData()
        {
        }

        public GridContainerData(int width, int height)
        {
            SetSize(width, height);
        }

        public void SetSize(int width, int height)
        {
            this.width = Math.Max(0, width);
            this.height = Math.Max(0, height);
            slots.Clear();

            int slotCount = this.width * this.height;
            for (int index = 0; index < slotCount; index++)
            {
                slots.Add(new GridSlotData());
            }
        }

        public void Clear()
        {
            foreach (GridSlotData slot in slots)
            {
                slot.Clear();
            }
        }
    }
}
