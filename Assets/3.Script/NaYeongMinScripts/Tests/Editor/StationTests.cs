using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Birdkov.NaYeongMin.Tests
{
    public class StationTests
    {
        private ItemCatalog catalog;
        private InventoryService inventory;
        [SetUp] public void SetUp()
        {
            catalog = new ItemCatalog(ItemCsvLoader.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/DataTeble/NaYeongMinCsvData/ItemData.csv").text));
            inventory = new InventoryService(catalog);
        }

        [TestCase(27001, 11001)] [TestCase(27002, 11002)] [TestCase(27003, 11003)] [TestCase(27004, 11004)]
        public void Craft_CombinesBagAndWarehouse(int mushroom, int ammo)
        {
            var player = new PlayerInventoryData(); var warehouse = new GridContainerData(5, 5);
            inventory.AddItem(player.inventory, mushroom, 2); inventory.AddItem(warehouse, mushroom, 3);
            inventory.AddItem(warehouse, 25001, 5);
            var service = new CraftingService(catalog);
            Assert.AreEqual(CraftResult.Success, service.Craft(player, mushroom, warehouse));
            Assert.AreEqual(20, service.Count(player.inventory, ammo));   // 탄약 1개 = 1발
            Assert.AreEqual(0, service.Count(warehouse, mushroom));
            Assert.AreEqual(0, service.Count(warehouse, 25001));
        }

        [Test] public void Craft_FullBagRestoresBothAndMetadata()
        {
            var player = new PlayerInventoryData(); var warehouse = new GridContainerData(5, 5);
            inventory.AddItem(player.inventory, 10001, 25);
            player.inventory.slots[0].remainingRounds = 7; player.inventory.slots[0].durabilityDamage = 23;
            inventory.AddItem(warehouse, 27001, 5); inventory.AddItem(warehouse, 25001, 5);
            string beforeBag = JsonUtility.ToJson(player); string beforeWarehouse = JsonUtility.ToJson(warehouse);
            Assert.AreEqual(CraftResult.NoSpace, new CraftingService(catalog).Craft(player, 27001, warehouse));
            Assert.AreEqual(beforeBag, JsonUtility.ToJson(player)); Assert.AreEqual(beforeWarehouse, JsonUtility.ToJson(warehouse));
        }

        [TestCase(10001, 500, 2)] [TestCase(10002, 600, 3)] [TestCase(10003, 900, 3)] [TestCase(10004, 800, 8)]
        public void Durability_PerShotAndRepair(int itemId, int max, int cost)
        {
            var player = new PlayerInventoryData { currency = 2 };
            var slot = new GridSlotData { itemId = itemId, amount = 1 };
            Assert.AreEqual(max, WeaponDurability.Remaining(slot));
            Assert.IsFalse(WeaponDurability.ApplyShot(slot)); Assert.AreEqual(max - cost, WeaponDurability.Remaining(slot));
            Assert.IsTrue(WeaponDurability.Repair(player, slot)); Assert.AreEqual(max, WeaponDurability.Remaining(slot));
            Assert.AreEqual(1, player.currency); Assert.IsFalse(WeaponDurability.Repair(player, slot));
            slot.durabilityDamage = max - cost; Assert.IsTrue(WeaponDurability.ApplyShot(slot));
            Assert.IsFalse(WeaponDurability.Repair(player, slot));
        }

        [Test] public void Durability_MoveAndJsonPreserveWear()
        {
            var data = new PlayerSaveData(); inventory.AddItem(data.inventoryData.inventory, 10001, 1);
            data.inventoryData.inventory.slots[0].durabilityDamage = 38;
            data.inventoryData.inventory.slots[0].remainingRounds = 6;
            inventory.MoveItem(data.inventoryData.inventory, 0, data.warehouseData, 0, 1);
            var loaded = JsonUtility.FromJson<PlayerSaveData>(JsonUtility.ToJson(data));
            Assert.AreEqual(38, loaded.warehouseData.slots[0].durabilityDamage);
            Assert.AreEqual(6, loaded.warehouseData.slots[0].remainingRounds);
            Assert.IsTrue(SaveDataValidator.IsValid(loaded));
        }

        [Test] public void Shop_BuySellAndFailureAreAtomic()
        {
            var player = new PlayerInventoryData { currency = 10 }; var shop = new ShopService(catalog);
            Assert.IsTrue(shop.Buy(player, 10001)); Assert.AreEqual(8, player.currency);
            Assert.IsTrue(shop.Sell(player, 0)); Assert.AreEqual(10, player.currency);
            Assert.IsFalse(shop.Buy(player, 27001)); Assert.IsFalse(shop.Buy(player, 20001));
            inventory.AddItem(player.inventory, 10001, 25);
            string before = JsonUtility.ToJson(player); Assert.IsFalse(shop.Buy(player, 10002));
            Assert.AreEqual(before, JsonUtility.ToJson(player));
            player.currency = 0; Assert.IsFalse(shop.Buy(player, 10001));
        }

        [TestCase(10001, MerchantKind.Weapons)]
        [TestCase(11001, MerchantKind.Weapons)]
        [TestCase(12001, MerchantKind.Weapons)]
        [TestCase(13001, MerchantKind.Weapons)]
        [TestCase(21001, MerchantKind.General)]
        [TestCase(22001, MerchantKind.General)]
        [TestCase(23001, MerchantKind.General)]
        [TestCase(25001, MerchantKind.General)]
        [TestCase(26001, MerchantKind.General)]
        public void Merchant_OnlyTradesAssignedCategory(int itemId, MerchantKind kind)
        {
            var player = new PlayerInventoryData { currency = 100 };
            var allowed = new ShopService(catalog, kind);
            var denied = new ShopService(catalog, kind == MerchantKind.Weapons ? MerchantKind.General : MerchantKind.Weapons);
            string before = JsonUtility.ToJson(player);
            Assert.IsFalse(denied.Buy(player, itemId)); Assert.AreEqual(before, JsonUtility.ToJson(player));
            Assert.IsTrue(allowed.Buy(player, itemId));
            before = JsonUtility.ToJson(player);
            Assert.IsFalse(denied.Sell(player, 0)); Assert.AreEqual(before, JsonUtility.ToJson(player));
            Assert.IsTrue(allowed.Sell(player, 0)); Assert.AreEqual(100, player.currency);
        }

        [Test] public void Merchant_CatalogPartitionExcludesNonTradeItems()
        {
            foreach (var item in ItemCsvLoader.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/DataTeble/NaYeongMinCsvData/ItemData.csv").text))
            {
                bool weapons = ShopService.Accepts(item, MerchantKind.Weapons);
                bool general = ShopService.Accepts(item, MerchantKind.General);
                Assert.IsFalse(weapons && general, item.displayName);
                Assert.AreEqual(ShopService.IsTradable(item), weapons || general, item.displayName);
            }
        }
    }
}
