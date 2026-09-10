using System;

namespace Birdkov.NaYeongMin.InventorySystem
{
    [Serializable]
    public class LootContainerData
    {
        public LootContainerSize sizePreset = LootContainerSize.Box2x4;
        public GridContainerData loot = new GridContainerData(2, 4);

        public LootContainerData()
        {
        }

        public LootContainerData(LootContainerSize sizePreset)
        {
            SetSize(sizePreset);
        }

        public int SlotCount => loot.slots.Count;

        // 크기 변경은 내용물을 비운다. 파밍 전에만 호출한다.
        public void SetSize(LootContainerSize sizePreset)
        {
            this.sizePreset = sizePreset;
            LootContainerSizes.GetSize(sizePreset, out int width, out int height);
            loot.SetSize(width, height);
        }

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
