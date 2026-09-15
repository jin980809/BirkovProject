using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.Rng;
using Birdkov.NaYeongMin.Ui;
using NUnit.Framework;
using UnityEngine;

// UI 개편에 딸린 데이터 규칙 검증. 화면 조작이 아니라 컨테이너 규칙만 본다.
namespace Birdkov.NaYeongMin.Tests
{
    public class ContainerDropSettingsTests
    {
        private ItemCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            catalog = new ItemCatalog(new[]
            {
                new ItemData { itemId = 21001, itemType = ItemType.Consumable, displayName = "회복약", stackable = true, maxStack = 5 },
                new ItemData { itemId = 22001, itemType = ItemType.Consumable, displayName = "물", stackable = true, maxStack = 5 },
                new ItemData { itemId = 10001, itemType = ItemType.Weapon, displayName = "기관권총" }
            });
        }

        private static ContainerDropSettings Make(LootContainerSize size, params ContainerDropSettings.DropEntry[] entries)
        {
            ContainerDropSettings settings = ScriptableObject.CreateInstance<ContainerDropSettings>();
            SetPrivate(settings, "containerSize", size);
            SetPrivate(settings, "entries", entries);
            return settings;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            target.GetType()
                .GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static ContainerDropSettings.DropEntry Entry(int itemId, float chance, int min, int max)
        {
            return new ContainerDropSettings.DropEntry
            {
                itemId = itemId,
                chancePercent = chance,
                minAmount = min,
                maxAmount = max
            };
        }

        private sealed class AlwaysHit : IRandomSource
        {
            public double NextUnit() => 0.0;
            public int NextInclusive(int minimum, int maximum) => minimum;
        }

        private sealed class NeverHit : IRandomSource
        {
            public double NextUnit() => 0.999999;
            public int NextInclusive(int minimum, int maximum) => minimum;
        }

        [Test]
        public void Roll_OnlyProducesAllowedItems()
        {
            ContainerDropSettings settings = Make(LootContainerSize.Box2x4, Entry(21001, 100f, 1, 1));
            LootContainerData loot = ContainerLootRoller.Roll(settings, catalog, new AlwaysHit());

            foreach (GridSlotData slot in loot.loot.slots)
            {
                Assert.IsTrue(slot.IsEmpty() || slot.itemId == 21001);
            }
        }

        [Test]
        public void Roll_EmptyListGivesEmptyContainer()
        {
            ContainerDropSettings settings = Make(LootContainerSize.Box2x4);
            LootContainerData loot = ContainerLootRoller.Roll(settings, catalog, new AlwaysHit());

            Assert.IsTrue(loot.IsEmpty());
            Assert.AreEqual(8, loot.SlotCount);
        }

        [Test]
        public void Roll_NullSettingsGivesEmptyContainer()
        {
            LootContainerData loot = ContainerLootRoller.Roll(null, catalog, new AlwaysHit());

            Assert.IsTrue(loot.IsEmpty());
        }

        [Test]
        public void Roll_UnknownItemIdIsSkipped()
        {
            ContainerDropSettings settings = Make(LootContainerSize.Box2x4, Entry(99999, 100f, 1, 1));
            LootContainerData loot = ContainerLootRoller.Roll(settings, catalog, new AlwaysHit());

            Assert.IsTrue(loot.IsEmpty());
        }

        [Test]
        public void Roll_FailedChanceLeavesContainerEmpty()
        {
            ContainerDropSettings settings = Make(LootContainerSize.Box2x4, Entry(21001, 50f, 1, 1));
            LootContainerData loot = ContainerLootRoller.Roll(settings, catalog, new NeverHit());

            Assert.IsTrue(loot.IsEmpty());
        }

        // 용량을 넘는 항목은 기존 규칙대로 버린다.
        [Test]
        public void Roll_DiscardsBeyondCapacity()
        {
            List<ContainerDropSettings.DropEntry> entries = new List<ContainerDropSettings.DropEntry>();
            for (int index = 0; index < 12; index++)
            {
                entries.Add(Entry(21001, 100f, 1, 1));
            }

            ContainerDropSettings settings = Make(LootContainerSize.Box2x4, entries.ToArray());
            LootContainerData loot = ContainerLootRoller.Roll(settings, catalog, new AlwaysHit());

            int filled = 0;
            foreach (GridSlotData slot in loot.loot.slots)
            {
                if (!slot.IsEmpty())
                {
                    filled++;
                }
            }

            Assert.AreEqual(8, loot.SlotCount);
            Assert.AreEqual(8, filled);
        }

        [Test]
        public void Validate_ReportsBadIdAmountAndChance()
        {
            ContainerDropSettings settings = Make(
                LootContainerSize.Box2x4,
                Entry(99999, 50f, 1, 1),
                Entry(21001, 150f, 1, 1),
                Entry(21001, 50f, 3, 2),
                Entry(21001, 50f, 0, 1));

            List<string> problems = settings.Validate(catalog);

            Assert.AreEqual(4, problems.Count);
        }

        [Test]
        public void Validate_CleanSettingsHaveNoProblems()
        {
            ContainerDropSettings settings = Make(LootContainerSize.Box3x3, Entry(21001, 40f, 1, 3), Entry(22001, 10f, 1, 1));

            Assert.AreEqual(0, settings.Validate(catalog).Count);
        }
    }

    public class MapChestContainerTests
    {
        private ItemCatalog catalog;
        private GameObject host;
        private MapChestContainer chest;

        [SetUp]
        public void SetUp()
        {
            catalog = new ItemCatalog(new[]
            {
                new ItemData { itemId = 21001, itemType = ItemType.Consumable, displayName = "회복약", stackable = true, maxStack = 5 }
            });

            ContainerDropSettings settings = ScriptableObject.CreateInstance<ContainerDropSettings>();
            SetPrivate(settings, "containerSize", LootContainerSize.Box2x4);
            SetPrivate(settings, "entries", new[]
            {
                new ContainerDropSettings.DropEntry { itemId = 21001, chancePercent = 100f, minAmount = 2, maxAmount = 2 }
            });

            host = new GameObject("MapChestTest");
            chest = host.AddComponent<MapChestContainer>();
            SetPrivate(chest, "dropSettings", settings);
            SetPrivate(chest, "useFixedSeed", true);
            SetPrivate(chest, "seed", 1234);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        private static void SetPrivate(object target, string field, object value)
        {
            target.GetType()
                .GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        [Test]
        public void EnsureContents_RollsOnceOnly()
        {
            chest.EnsureContents(catalog);
            Assert.IsTrue(chest.Rolled);
            Assert.AreEqual(2, chest.Container.slots[0].amount);

            // 획득 후 재개방해도 다시 굴리지 않고 복구하지 않는다.
            chest.Container.slots[0].Clear();
            chest.EnsureContents(catalog);

            Assert.IsTrue(chest.Container.slots[0].IsEmpty());
        }

        [Test]
        public void Contents_AreIndependentPerObject()
        {
            GameObject other = new GameObject("MapChestTest2");
            MapChestContainer second = other.AddComponent<MapChestContainer>();
            SetPrivate(second, "dropSettings", chest.DropSettings);
            SetPrivate(second, "useFixedSeed", true);
            SetPrivate(second, "seed", 1234);

            chest.EnsureContents(catalog);
            second.EnsureContents(catalog);
            chest.Container.slots[0].Clear();

            Assert.IsTrue(chest.Container.slots[0].IsEmpty());
            Assert.AreEqual(2, second.Container.slots[0].amount);

            Object.DestroyImmediate(other);
        }
    }
}
