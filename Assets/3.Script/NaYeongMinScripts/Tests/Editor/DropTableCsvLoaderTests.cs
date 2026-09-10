using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.Rng;
using NUnit.Framework;

namespace Birdkov.NaYeongMin.Tests
{
    public class DropTableCsvLoaderTests
    {
        private const string ValidCsv =
            "sourceType,itemId,finalDropChance,minAmount,maxAmount,comment\n" +
            "Box,21001,40,1,4,\n" +
            "BasicEnemy,20001,100,1,5,\n" +
            "HeavyEnemy,20001,100,10,20,\n" +
            "RangedEnemy,24002,1,1,1,\n";

        [Test]
        public void Parse_ReadsEveryRow()
        {
            List<DropTableEntry> entries = DropTableCsvLoader.Parse(ValidCsv);

            Assert.AreEqual(4, entries.Count);
            Assert.AreEqual(DropSourceType.Box, entries[0].sourceType);
            Assert.AreEqual(21001, entries[0].itemId);
            Assert.AreEqual(40f, entries[0].finalDropChance);
            Assert.AreEqual(4, entries[0].maxAmount);
        }

        [Test]
        public void Parse_KeepsSourceSpecificAmountRanges()
        {
            List<DropTableEntry> entries = DropTableCsvLoader.Parse(ValidCsv);

            DropTableEntry basicWheat = entries.Find(entry =>
                entry.sourceType == DropSourceType.BasicEnemy && entry.itemId == 20001);
            DropTableEntry heavyWheat = entries.Find(entry =>
                entry.sourceType == DropSourceType.HeavyEnemy && entry.itemId == 20001);

            Assert.AreEqual(1, basicWheat.minAmount);
            Assert.AreEqual(5, basicWheat.maxAmount);
            Assert.AreEqual(10, heavyWheat.minAmount);
            Assert.AreEqual(20, heavyWheat.maxAmount);
        }

        [Test]
        public void Parse_RejectsChanceOutOfRange()
        {
            const string csv =
                "sourceType,itemId,finalDropChance,minAmount,maxAmount,comment\n" +
                "Box,21001,140,1,1,\n";

            Assert.Throws<System.FormatException>(() => DropTableCsvLoader.Parse(csv));
        }

        [Test]
        public void Parse_RejectsUnknownSourceType()
        {
            const string csv =
                "sourceType,itemId,finalDropChance,minAmount,maxAmount,comment\n" +
                "Boss,21001,10,1,1,\n";

            Assert.Throws<System.FormatException>(() => DropTableCsvLoader.Parse(csv));
        }

        [Test]
        public void FindMissingItemIds_ReportsUnknownReferences()
        {
            ItemCatalog catalog = new ItemCatalog(new List<ItemData>
            {
                new ItemData { itemId = 21001, itemType = ItemType.Consumable }
            });

            List<int> missing = DropTableCsvLoader.FindMissingItemIds(DropTableCsvLoader.Parse(ValidCsv), catalog);

            CollectionAssert.AreEquivalent(new[] { 20001, 24002 }, missing);
        }

        [Test]
        public void Roll_IsReproducibleWithFixedSeed()
        {
            List<DropTableEntry> entries = DropTableCsvLoader.Parse(ValidCsv);

            LootContainerData first = new DropRoller(new SystemRandomSource(1234))
                .Roll(entries, DropSourceType.Box);
            LootContainerData second = new DropRoller(new SystemRandomSource(1234))
                .Roll(entries, DropSourceType.Box);

            for (int index = 0; index < first.SlotCount; index++)
            {
                Assert.AreEqual(first.loot.slots[index].itemId, second.loot.slots[index].itemId);
                Assert.AreEqual(first.loot.slots[index].amount, second.loot.slots[index].amount);
            }
        }
    }
}
