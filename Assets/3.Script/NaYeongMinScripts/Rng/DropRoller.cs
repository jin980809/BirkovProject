using System;
using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;

namespace Birdkov.NaYeongMin.Rng
{
    public sealed class DropRoller
    {
        private readonly IRandomSource randomSource;

        public DropRoller(IRandomSource randomSource)
        {
            this.randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
        }

        // sizePreset 은 결과를 담을 컨테이너 크기다. 적 사망 오브제는 기본값 Box2x4(8칸)를 쓴다.
        // 컨테이너 칸 수를 넘긴 당첨분은 생성하지 않고 파기한다.
        public LootContainerData Roll(
            IEnumerable<DropTableEntry> entries,
            DropSourceType sourceType,
            LootContainerSize sizePreset = LootContainerSize.Box2x4)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            LootContainerData loot = new LootContainerData(sizePreset);
            int lootIndex = 0;

            foreach (DropTableEntry entry in entries)
            {
                if (entry == null || entry.sourceType != sourceType || entry.itemId < 0)
                {
                    continue;
                }

                float chance = Math.Clamp(entry.finalDropChance, 0f, 100f);
                bool selected = randomSource.NextUnit() * 100d < chance;
                if (!selected)
                {
                    continue;
                }

                int minimum = Math.Max(1, entry.minAmount);
                int maximum = Math.Max(minimum, entry.maxAmount);
                int amount = randomSource.NextInclusive(minimum, maximum);

                if (lootIndex >= loot.SlotCount)
                {
                    continue;
                }

                GridSlotData slot = loot.loot.slots[lootIndex];
                slot.itemId = entry.itemId;
                slot.amount = amount;
                lootIndex++;
            }

            return loot;
        }
    }
}
