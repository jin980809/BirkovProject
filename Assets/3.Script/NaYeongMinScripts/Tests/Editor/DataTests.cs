using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.Rng;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

// CSV 파싱과 드롭 테이블 참조 무결성 테스트.
namespace Birdkov.NaYeongMin.Tests
{
    public class ItemCsvLoaderTests
    {
        [Test]
        public void Parse_SupportsQuotedCommaAndConfiguredStackLimit()
        {
            const string csv = "itemId,itemType,displayName,description,maxStack\n1,Material,철 조각,\"짧고, 단단함\",5";

            var items = ItemCsvLoader.Parse(csv);

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("짧고, 단단함", items[0].description);
            Assert.AreEqual(5, items[0].maxStack);
        }

        [Test]
        public void ProjectCsv_HasValidSchema()
        {
            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/DataTeble/NaYeongMinCsvData/ItemData.csv");

            Assert.IsNotNull(csv);
            Assert.DoesNotThrow(() => ItemCsvLoader.Parse(csv.text));
        }
    }

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

    public class DropTableDatabaseTests
    {
        private GameObject testObject;
        private DropTableDatabase database;
        private TextAsset csv;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("DropTableDatabaseTest");
            database = testObject.AddComponent<DropTableDatabase>();
            csv = new TextAsset("sourceType,itemId,finalDropChance,minAmount,maxAmount\nBox,21001,100,1,1\n");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(testObject);
            UnityEngine.Object.DestroyImmediate(csv);
        }

        private void AssignCsv()
        {
            SerializedObject serialized = new SerializedObject(database);
            serialized.FindProperty("dropTableCsv").objectReferenceValue = csv;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ItemCatalog CreateCatalog()
        {
            return new ItemCatalog(new[] { new ItemData { itemId = 21001 } });
        }

        [Test]
        public void Load_RejectsMissingCsv()
        {
            Assert.Throws<InvalidOperationException>(() => database.Load(CreateCatalog()));
            Assert.IsNull(database.Entries);
        }

        [Test]
        public void Load_ReadsValidatedRows()
        {
            AssignCsv();
            database.Load(CreateCatalog());
            Assert.AreEqual(1, database.Entries.Count);
            Assert.AreEqual(21001, database.Entries[0].itemId);
        }

        [Test]
        public void Load_ClearsPreviousRowsWhenReferencesAreMissing()
        {
            AssignCsv();
            database.Load(CreateCatalog());
            Assert.Throws<InvalidOperationException>(() => database.Load(new ItemCatalog(Array.Empty<ItemData>())));
            Assert.IsNull(database.Entries);
        }

        [Test]
        public void Load_RejectsMissingCatalog()
        {
            AssignCsv();
            Assert.Throws<ArgumentNullException>(() => database.Load(null));
            Assert.IsNull(database.Entries);
        }
    }
}
