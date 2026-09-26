using System;
using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;

// 드롭 테이블로 아이템별 독립 추첨. 컨테이너 칸 수를 넘기면 파기한다.
namespace Birdkov.NaYeongMin.Rng
{
    public sealed class DropRoller
    {
        private readonly IRandomSource randomSource;
        private readonly IItemCatalog itemCatalog;

        // itemCatalog 를 주면 헬멧·조끼를 부위당 한 개로 제한한다 (없으면 제한 없이 굴린다).
        public DropRoller(IRandomSource randomSource, IItemCatalog itemCatalog = null)
        {
            this.randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
            this.itemCatalog = itemCatalog;
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
            ArmorSlotLimit armorLimit = new ArmorSlotLimit();

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

                // 같은 부위의 방어구가 이미 나왔으면 건너뛴다 (헬멧 2개 방지)
                if (!armorLimit.CanTake(itemCatalog, entry.itemId))
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
