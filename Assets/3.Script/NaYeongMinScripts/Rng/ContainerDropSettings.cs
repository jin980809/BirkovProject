using System;
using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;

// 컨테이너 개별 드롭 설정. 여기 적힌 itemId 만 나온다. 전역 드롭 테이블로 넘어가지 않는다.
namespace Birdkov.NaYeongMin.Rng
{
    [CreateAssetMenu(fileName = "ContainerDropSettings", menuName = "Birdkov/NaYeongMin/Container Drop Settings")]
    public sealed class ContainerDropSettings : ScriptableObject
    {
        [Serializable]
        public struct DropEntry
        {
            [Tooltip("ItemData.csv 의 itemId")]
            public int itemId;

            [Range(0f, 100f)]
            public float chancePercent;

            [Min(1)]
            public int minAmount;

            [Min(1)]
            public int maxAmount;
        }

        [SerializeField] private LootContainerSize containerSize = LootContainerSize.Box2x4;
        [SerializeField] private DropEntry[] entries = Array.Empty<DropEntry>();

        public LootContainerSize ContainerSize => containerSize;
        public IReadOnlyList<DropEntry> Entries => entries;

        // 잘못된 ID·수량·확률을 사람이 읽는 문장으로 돌려준다. 빈 목록은 오류가 아니다.
        public List<string> Validate(IItemCatalog itemCatalog)
        {
            List<string> problems = new List<string>();
            if (entries == null)
            {
                return problems;
            }

            for (int index = 0; index < entries.Length; index++)
            {
                DropEntry entry = entries[index];
                string where = name + " 항목 " + index + " (itemId " + entry.itemId + ")";

                if (itemCatalog != null && !itemCatalog.TryGetItem(entry.itemId, out _))
                {
                    problems.Add(where + " : ItemData.csv 에 없는 itemId 입니다.");
                }

                if (entry.chancePercent < 0f || entry.chancePercent > 100f || float.IsNaN(entry.chancePercent))
                {
                    problems.Add(where + " : 확률은 0 에서 100 사이여야 합니다.");
                }

                if (entry.minAmount < 1 || entry.maxAmount < 1)
                {
                    problems.Add(where + " : 수량은 1 이상이어야 합니다.");
                }
                else if (entry.maxAmount < entry.minAmount)
                {
                    problems.Add(where + " : 최대 수량이 최소 수량보다 작습니다.");
                }
            }

            return problems;
        }
    }

    // 허용 목록만으로 독립 추첨한다. 슬롯 수를 넘는 결과는 기존 규칙대로 버린다.
    public static class ContainerLootRoller
    {
        public static LootContainerData Roll(
            ContainerDropSettings settings,
            IItemCatalog itemCatalog,
            IRandomSource randomSource)
        {
            LootContainerSize size = settings != null ? settings.ContainerSize : LootContainerSize.Box2x4;
            LootContainerData loot = new LootContainerData(size);
            if (settings == null || settings.Entries == null || settings.Entries.Count == 0)
            {
                return loot;
            }

            IRandomSource random = randomSource ?? new SystemRandomSource();
            int nextSlot = 0;

            foreach (ContainerDropSettings.DropEntry entry in settings.Entries)
            {
                if (entry.chancePercent <= 0f || entry.chancePercent > 100f || float.IsNaN(entry.chancePercent) ||
                    entry.minAmount < 1 || entry.maxAmount < entry.minAmount)
                {
                    continue;
                }

                if (itemCatalog != null && !itemCatalog.TryGetItem(entry.itemId, out _))
                {
                    continue;
                }

                if (random.NextUnit() * 100.0 >= entry.chancePercent)
                {
                    continue;
                }

                int amount = random.NextInclusive(entry.minAmount, entry.maxAmount);

                // 기획서 10.3 : 칸을 넘겨 당첨된 아이템은 생성하지 않고 파기한다.
                // 목록은 끝까지 굴린다. 중간에 멈추면 뒤쪽 항목이 추첨 기회를 잃는다.
                if (nextSlot >= loot.SlotCount)
                {
                    continue;
                }

                loot.loot.slots[nextSlot].itemId = entry.itemId;
                loot.loot.slots[nextSlot].amount = amount;
                nextSlot++;
            }

            return loot;
        }
    }
}
