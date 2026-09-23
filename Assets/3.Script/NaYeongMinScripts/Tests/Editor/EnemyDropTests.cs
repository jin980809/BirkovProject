using System.Collections.Generic;
using System.Linq;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.Rng;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Birdkov.NaYeongMin.Tests
{
    // 기획서 10.3 (2026-09-23 표). 적은 자기 무기와 탄약을 100% 떨어뜨리고, 입고 나온 방어구도 떨어뜨린다.
    public class EnemyDropTests
    {
        private List<DropTableEntry> entries;

        [SetUp] public void SetUp()
        {
            entries = DropTableCsvLoader.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/DataTeble/NaYeongMinCsvData/DropTable.csv").text);
        }

        private float Chance(DropSourceType source, int itemId)
        {
            DropTableEntry entry = entries.FirstOrDefault(e => e.sourceType == source && e.itemId == itemId);
            return entry == null ? 0f : entry.finalDropChance;
        }

        [TestCase(DropSourceType.BasicEnemy, 10001, 11001)]
        [TestCase(DropSourceType.HeavyEnemy, 10002, 11002)]
        [TestCase(DropSourceType.RangedEnemy, 10003, 11003)]
        public void Enemy_DropsOwnWeaponAndAmmoAlways(DropSourceType source, int weapon, int ammo)
        {
            Assert.AreEqual(100f, Chance(source, weapon));
            Assert.AreEqual(100f, Chance(source, ammo));
            Assert.AreEqual(100f, Chance(source, 20001));
        }

        [Test] public void Enemy_WeaponColumnsMatchTable()
        {
            Assert.AreEqual(0f, Chance(DropSourceType.BasicEnemy, 10002));
            Assert.AreEqual(0f, Chance(DropSourceType.BasicEnemy, 10004));
            Assert.AreEqual(0f, Chance(DropSourceType.HeavyEnemy, 10001));
            Assert.AreEqual(50f, Chance(DropSourceType.HeavyEnemy, 10004));
            Assert.AreEqual(50f, Chance(DropSourceType.HeavyEnemy, 11004));
            Assert.AreEqual(0f, Chance(DropSourceType.RangedEnemy, 10001));
            Assert.AreEqual(5f, Chance(DropSourceType.RangedEnemy, 10004));
            Assert.AreEqual(5f, Chance(DropSourceType.RangedEnemy, 11004));
        }

        [TestCase(DropSourceType.BasicEnemy, 50f, 10f, 1f)]
        [TestCase(DropSourceType.HeavyEnemy, 10f, 20f, 5f)]
        [TestCase(DropSourceType.RangedEnemy, 10f, 10f, 5f)]
        public void Enemy_ArmorColumnsMatchTable(DropSourceType source, float lv1, float lv2, float lv3)
        {
            Assert.AreEqual(lv1, Chance(source, 13001)); Assert.AreEqual(lv1, Chance(source, 12001));
            Assert.AreEqual(lv2, Chance(source, 13002)); Assert.AreEqual(lv2, Chance(source, 12002));
            Assert.AreEqual(lv3, Chance(source, 13003)); Assert.AreEqual(lv3, Chance(source, 12003));
        }

        [Test] public void NormalChest_HasWeaponAmmoAndArmorRows()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ContainerDropSettings>(
                "Assets/DataTeble/NaYeongMinDropSettings/Chest_Normal.asset");
            var chances = settings.Entries.ToDictionary(e => e.itemId, e => e.chancePercent);
            Assert.AreEqual(5f, chances[10001]); Assert.AreEqual(0.5f, chances[10004]);
            Assert.AreEqual(5f, chances[11001]); Assert.AreEqual(0.5f, chances[11004]);
            Assert.AreEqual(4f, chances[13001]); Assert.AreEqual(0.5f, chances[12003]);
            Assert.AreEqual(30f, chances[20001]);
        }

        [Test] public void GuaranteedItems_GoFirstAndAreNeverDiscarded()
        {
            var rolled = new LootContainerData(LootContainerSize.Box2x4);
            for (int i = 0; i < rolled.SlotCount; i++) { rolled.loot.slots[i].itemId = 21001; rolled.loot.slots[i].amount = 1; }
            var method = typeof(LootRuntime).GetMethod("PutFirst",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = (LootContainerData)method.Invoke(null, new object[] { rolled, new List<int> { 13002, 12003 }, LootContainerSize.Box2x4 });
            Assert.AreEqual(13002, result.loot.slots[0].itemId);
            Assert.AreEqual(12003, result.loot.slots[1].itemId);
            Assert.AreEqual(1, result.loot.slots[0].amount);
            Assert.AreEqual(21001, result.loot.slots[2].itemId);
            Assert.AreEqual(8, result.SlotCount);
        }

        [Test] public void Receiver_ReadsWornArmorFromSpawnedModels()
        {
            var enemy = new GameObject("Enemy");
            try
            {
                var head = new GameObject("HeadGear"); head.transform.SetParent(enemy.transform);
                new GameObject("2LvHelmet(Clone)").transform.SetParent(head.transform);
                var belly = new GameObject("Belly"); belly.transform.SetParent(enemy.transform);
                new GameObject("3LvArmor(Clone)").transform.SetParent(belly.transform);
                new GameObject("1LvHelmet").transform.SetParent(enemy.transform);   // 스폰본이 아니면 무시
                var receiver = enemy.AddComponent<EnemyLootReceiver>();
                receiver.CaptureWornArmor();
                CollectionAssert.AreEquivalent(new[] { 13002, 12003 }, receiver.WornItemIds.ToArray());
            }
            finally { Object.DestroyImmediate(enemy); }
        }

        [Test] public void Receiver_NoArmorWhenNoneSpawned()
        {
            var enemy = new GameObject("Enemy");
            try
            {
                var receiver = enemy.AddComponent<EnemyLootReceiver>();
                receiver.CaptureWornArmor();
                Assert.AreEqual(0, receiver.WornItemIds.Count);
            }
            finally { Object.DestroyImmediate(enemy); }
        }
    }

    // 탄약은 발 단위. 아이템 1개 = 1발. 거래·제작·드롭은 CSV magazineSize(20발) 묶음.
    public class AmmoUnitTests
    {
        private ItemCatalog catalog;
        private List<DropTableEntry> entries;

        [SetUp] public void SetUp()
        {
            catalog = new ItemCatalog(ItemCsvLoader.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/DataTeble/NaYeongMinCsvData/ItemData.csv").text));
            entries = DropTableCsvLoader.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/DataTeble/NaYeongMinCsvData/DropTable.csv").text);
        }

        private int Count(PlayerInventoryData player, int itemId) => new CraftingService(catalog).Count(player.inventory, itemId);

        [Test] public void Shop_BuyAmmoGivesTwentyRounds()
        {
            var player = new PlayerInventoryData { currency = 10 };
            Assert.IsTrue(new ShopService(catalog).Buy(player, 11001));
            Assert.AreEqual(20, Count(player, 11001));
            Assert.AreEqual(8, player.currency);
        }

        [Test] public void Shop_NonAmmoStillOne()
        {
            var player = new PlayerInventoryData { currency = 10 };
            Assert.IsTrue(new ShopService(catalog).Buy(player, 21001));
            Assert.AreEqual(1, Count(player, 21001));
        }

        [Test] public void Shop_SellAmmoTakesTwentyRounds()
        {
            var player = new PlayerInventoryData();
            new InventoryService(catalog).AddItem(player.inventory, 11001, 25);
            var shop = new ShopService(catalog);
            Assert.IsTrue(shop.Sell(player, 0));
            Assert.AreEqual(5, Count(player, 11001));
            Assert.AreEqual(2, player.currency);
            Assert.IsFalse(shop.Sell(player, 0));
            Assert.AreEqual(5, Count(player, 11001));
        }

        [Test] public void Shop_BuyRollsBackWhenBagFull()
        {
            var player = new PlayerInventoryData { currency = 10 };
            foreach (var slot in player.inventory.slots) { slot.itemId = 10001; slot.amount = 1; }
            Assert.IsFalse(new ShopService(catalog).Buy(player, 11001));
            Assert.AreEqual(10, player.currency);
            Assert.AreEqual(0, Count(player, 11001));
        }

        [TestCase(DropSourceType.BasicEnemy, 11001, 40)]
        [TestCase(DropSourceType.HeavyEnemy, 11002, 40)]
        [TestCase(DropSourceType.HeavyEnemy, 11004, 20)]
        [TestCase(DropSourceType.RangedEnemy, 11003, 20)]
        [TestCase(DropSourceType.RangedEnemy, 11004, 20)]
        public void Enemy_AmmoDropIsInRounds(DropSourceType source, int itemId, int rounds)
        {
            DropTableEntry entry = entries.First(e => e.sourceType == source && e.itemId == itemId);
            Assert.AreEqual(rounds, entry.minAmount);
            Assert.AreEqual(rounds, entry.maxAmount);
        }

        [Test] public void Craft_GivesTwentyRounds()
        {
            Assert.AreEqual(20, new CraftingService(catalog).CraftedAmount(11001));
            Assert.AreEqual(20, new CraftingService(catalog).CraftedAmount(11004));
        }
    }
}
