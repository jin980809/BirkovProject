using System;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.Rng;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Birdkov.NaYeongMin.Tests
{
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
