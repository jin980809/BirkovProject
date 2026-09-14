using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.Rng;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 드롭 추첨, 상자 크기, 오브젝트 풀링 테스트.
namespace Birdkov.NaYeongMin.Tests
{
    public class DropRollerTests
    {
        [Test]
        public void Roll_KeepsFirstEightSuccessfulIndependentRolls()
        {
            List<DropTableEntry> entries = new List<DropTableEntry>();
            for (int index = 0; index < 10; index++)
            {
                entries.Add(new DropTableEntry
                {
                    sourceType = DropSourceType.BasicEnemy,
                    itemId = index,
                    finalDropChance = 100f
                });
            }

            var loot = new DropRoller(new FixedRandomSource(0d)).Roll(entries, DropSourceType.BasicEnemy);

            Assert.AreEqual(8, loot.loot.slots.FindAll(slot => !slot.IsEmpty()).Count);
            for (int index = 0; index < 8; index++) Assert.AreEqual(index, loot.loot.slots[index].itemId);
        }

        [Test]
        public void Roll_AppliesEachFinalChanceIndependently()
        {
            DropTableEntry never = new DropTableEntry
                { sourceType = DropSourceType.Box, itemId = 1, finalDropChance = 0f };
            DropTableEntry always = new DropTableEntry
                { sourceType = DropSourceType.Box, itemId = 2, finalDropChance = 100f };

            var loot = new DropRoller(new FixedRandomSource(0.5d)).Roll(
                new[] { never, always }, DropSourceType.Box);

            Assert.AreEqual(2, loot.loot.slots[0].itemId);
            Assert.AreEqual(1, loot.loot.slots[0].amount);
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly double value;

            public FixedRandomSource(double value)
            {
                this.value = value;
            }

            public double NextUnit() => value;
            public int NextInclusive(int minimum, int maximum) => minimum;
        }
    }

    public class LootContainerSizeTests
    {
        private sealed class AlwaysHitRandomSource : IRandomSource
        {
            public double NextUnit()
            {
                return 0d;
            }

            public int NextInclusive(int minimum, int maximum)
            {
                return minimum;
            }
        }

        [TestCase(LootContainerSize.Box2x4, 8)]
        [TestCase(LootContainerSize.Box3x3, 9)]
        [TestCase(LootContainerSize.Box3x5, 15)]
        [TestCase(LootContainerSize.Box4x2, 8)]
        public void SizePreset_CreatesExpectedSlotCount(LootContainerSize sizePreset, int expectedSlotCount)
        {
            LootContainerData container = new LootContainerData(sizePreset);

            Assert.AreEqual(expectedSlotCount, container.SlotCount);
            Assert.AreEqual(expectedSlotCount, LootContainerSizes.GetSlotCount(sizePreset));
        }

        [Test]
        public void DefaultContainer_KeepsEightSlots()
        {
            Assert.AreEqual(InventorySettings.LootSlotCount, new LootContainerData().SlotCount);
        }

        [Test]
        public void Roll_DiscardsEntriesBeyondContainerSize()
        {
            List<DropTableEntry> entries = new List<DropTableEntry>();
            for (int index = 0; index < 10; index++)
            {
                entries.Add(new DropTableEntry
                {
                    sourceType = DropSourceType.Box,
                    itemId = index + 1,
                    finalDropChance = 100f,
                    minAmount = 1,
                    maxAmount = 1
                });
            }

            LootContainerData loot = new DropRoller(new AlwaysHitRandomSource())
                .Roll(entries, DropSourceType.Box, LootContainerSize.Box4x2);

            Assert.AreEqual(8, loot.SlotCount);
            Assert.IsTrue(loot.loot.slots.TrueForAll(slot => !slot.IsEmpty()));
        }

        [Test]
        public void SetSize_ClearsPreviousContents()
        {
            LootContainerData container = new LootContainerData(LootContainerSize.Box3x5);
            container.loot.slots[0].itemId = 1;
            container.loot.slots[0].amount = 1;

            container.SetSize(LootContainerSize.Box3x3);

            Assert.AreEqual(9, container.SlotCount);
            Assert.IsTrue(container.IsEmpty());
        }
    }

    public class LootDropPoolTests
    {
        [Test]
        public void Prefab_CanRentThirtyDropObjects()
        {
            LootDropPool prefab = AssetDatabase.LoadAssetAtPath<LootDropPool>(
                "Assets/2.Model/Prefabs/NaYeongMin/LootDropPool.prefab");
            LootDropPool pool = Object.Instantiate(prefab);
            HashSet<LootDropObject> rented = new HashSet<LootDropObject>();

            for (int index = 0; index < InventorySettings.DropObjectPoolSize; index++)
            {
                rented.Add(pool.Rent(Vector3.zero, Quaternion.identity, new LootContainerData()));
            }

            Assert.AreEqual(InventorySettings.DropObjectPoolSize, rented.Count);
            Assert.IsNull(pool.Rent(Vector3.zero, Quaternion.identity, new LootContainerData()));
            foreach (LootDropObject instance in rented)
            {
                pool.Return(instance);
                Assert.AreSame(instance, pool.Rent(Vector3.zero, Quaternion.identity, new LootContainerData()));
                break;
            }
            Object.DestroyImmediate(pool.gameObject);
        }
    }
}
