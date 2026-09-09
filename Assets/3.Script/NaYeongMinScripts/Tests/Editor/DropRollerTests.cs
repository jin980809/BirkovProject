using System.Collections.Generic;
using Birdkov.NaYeongMin.Rng;
using NUnit.Framework;

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
}
