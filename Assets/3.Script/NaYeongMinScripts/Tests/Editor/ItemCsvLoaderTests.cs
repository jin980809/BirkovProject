using Birdkov.NaYeongMin.InventorySystem;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
}
