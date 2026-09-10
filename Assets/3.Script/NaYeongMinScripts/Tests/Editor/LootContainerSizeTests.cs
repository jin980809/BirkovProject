using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.Rng;
using NUnit.Framework;

namespace Birdkov.NaYeongMin.Tests
{
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
        [TestCase(LootContainerSize.Box4x1, 4)]
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
                .Roll(entries, DropSourceType.Box, LootContainerSize.Box4x1);

            Assert.AreEqual(4, loot.SlotCount);
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
}
