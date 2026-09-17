using System;
using System.Collections.Generic;
using System.Reflection;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.Rng;
using NUnit.Framework;
using UnityEngine;

namespace Birdkov.NaYeongMin.Tests
{
    public class DropChanceOverrideTests
    {
        private GameObject host;
        private DropTableDatabase database;
        private TextAsset csv;
        private ItemCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("DropChanceOverrideTest");
            database = host.AddComponent<DropTableDatabase>();
            csv = new TextAsset("sourceType,itemId,finalDropChance,minAmount,maxAmount\nRangedEnemy,11003,5,1,2\nBasicEnemy,11003,1,1,1\n");
            SetField("dropTableCsv", csv);
            catalog = new ItemCatalog(new[] { new ItemData { itemId = 11003, displayName = "Test Ammo", itemType = ItemType.Ammo } });
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(csv);
        }

        private void SetField(string name, object value)
        {
            typeof(DropTableDatabase).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(database, value);
        }

        private DropTableDatabase.ChanceOverride Setting(float chance)
        {
            return new DropTableDatabase.ChanceOverride { sourceType = DropSourceType.RangedEnemy, itemId = 11003, chancePercent = chance };
        }

        [TestCase(0f)]
        [TestCase(5f)]
        [TestCase(100f)]
        public void Load_ChangesOnlySelectedChanceAndCanRestoreCsv(float chance)
        {
            SetField("chanceOverrides", new List<DropTableDatabase.ChanceOverride> { Setting(chance) });
            database.Load(catalog);
            Assert.AreEqual(chance, database.Entries[0].finalDropChance);
            Assert.AreEqual(1, database.Entries[0].minAmount);
            Assert.AreEqual(2, database.Entries[0].maxAmount);
            Assert.AreEqual(1f, database.Entries[1].finalDropChance);
            SetField("chanceOverrides", new List<DropTableDatabase.ChanceOverride>());
            database.Load(catalog);
            Assert.AreEqual(5f, database.Entries[0].finalDropChance);
        }

        [TestCase(-1f)]
        [TestCase(101f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Load_RejectsInvalidChance(float chance)
        {
            SetField("chanceOverrides", new List<DropTableDatabase.ChanceOverride> { Setting(chance) });
            Assert.Throws<InvalidOperationException>(() => database.Load(catalog));
            Assert.IsNull(database.Entries);
        }

        [Test]
        public void Load_RejectsDuplicateAndMissingTargets()
        {
            SetField("chanceOverrides", new List<DropTableDatabase.ChanceOverride> { Setting(5), Setting(10) });
            Assert.Throws<InvalidOperationException>(() => database.Load(catalog));
            var setting = Setting(5);
            setting.sourceType = DropSourceType.HeavyEnemy;
            SetField("chanceOverrides", new List<DropTableDatabase.ChanceOverride> { setting });
            Assert.Throws<InvalidOperationException>(() => database.Load(catalog));
            Assert.IsNull(database.Entries);
        }
    }
}
