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

        public LootContainerData Roll(IEnumerable<DropTableEntry> entries, DropSourceType sourceType)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            LootContainerData loot = new LootContainerData();
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

                if (lootIndex >= InventorySettings.LootSlotCount)
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
