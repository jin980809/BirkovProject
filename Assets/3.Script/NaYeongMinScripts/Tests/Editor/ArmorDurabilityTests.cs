using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Birdkov.NaYeongMin.Tests
{
    // 기획서 5.2 방어구 행이 정본. 수리는 무기와 같은 1G 단위.
    public class ArmorDurabilityTests
    {
        private const int HelmetId = 13001;   // 400 / 1G당 80
        private const int VestId = 12001;     // 450 / 1G당 90
        private const int WeaponId = 10001;

        private ItemCatalog catalog;
        private ArmorDurability armor;

        [SetUp] public void SetUp()
        {
            catalog = new ItemCatalog(ItemCsvLoader.Parse(
                AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/DataTeble/NaYeongMinCsvData/ItemData.csv").text));
            armor = new ArmorDurability();
        }

        private PlayerInventoryData Equipped(int currency = 0)
        {
            var player = new PlayerInventoryData { currency = currency };
            player.equipmentSlots.slots[EquipmentSlots.Helmet].itemId = HelmetId;
            player.equipmentSlots.slots[EquipmentSlots.Helmet].amount = 1;
            player.equipmentSlots.slots[EquipmentSlots.Armor].itemId = VestId;
            player.equipmentSlots.slots[EquipmentSlots.Armor].amount = 1;
            return player;
        }

        private GridSlotData Slot(int itemId) => new GridSlotData { itemId = itemId, amount = 1 };

        [TestCase(13001, 400, 1, 80)] [TestCase(13002, 500, 1, 75)] [TestCase(13003, 600, 1, 70)]
        [TestCase(12001, 450, 1, 90)] [TestCase(12002, 550, 1, 85)] [TestCase(12003, 650, 1, 80)]
        public void Armor_UsesCsvTable(int itemId, int max, int cost, int repair)
        {
            GridSlotData slot = Slot(itemId);
            Assert.IsTrue(armor.IsArmor(catalog, slot));
            Assert.AreEqual(max, armor.Maximum(catalog, slot));
            Assert.AreEqual(max, armor.Remaining(catalog, slot));
            Assert.AreEqual(cost, armor.DamagePerHit(catalog, slot));
            Assert.AreEqual(repair, armor.RepairPerCurrency(catalog, slot));
        }

        [Test] public void Armor_OneHitWearsEveryEquippedPiece()
        {
            var player = Equipped();
            armor.ApplyHit(catalog, player);
            Assert.AreEqual(399, armor.Remaining(catalog, player.equipmentSlots.slots[EquipmentSlots.Helmet]));
            Assert.AreEqual(449, armor.Remaining(catalog, player.equipmentSlots.slots[EquipmentSlots.Armor]));
            armor.ApplyHit(catalog, player);
            Assert.AreEqual(398, armor.Remaining(catalog, player.equipmentSlots.slots[EquipmentSlots.Helmet]));
            Assert.AreEqual(448, armor.Remaining(catalog, player.equipmentSlots.slots[EquipmentSlots.Armor]));
        }

        [Test] public void Armor_DestroyedWhenDurabilityHitsZero()
        {
            var player = Equipped();
            player.equipmentSlots.slots[EquipmentSlots.Helmet].durabilityDamage = 399;
            armor.ApplyHit(catalog, player);
            Assert.IsTrue(player.equipmentSlots.slots[EquipmentSlots.Helmet].IsEmpty());
            Assert.AreEqual(449, armor.Remaining(catalog, player.equipmentSlots.slots[EquipmentSlots.Armor]));
        }

        [Test] public void Armor_WeaponSlotsAreNotWorn()
        {
            var player = new PlayerInventoryData();
            player.equipmentSlots.slots[EquipmentSlots.PrimaryWeapon].itemId = WeaponId;
            player.equipmentSlots.slots[EquipmentSlots.PrimaryWeapon].amount = 1;
            armor.ApplyHit(catalog, player);
            Assert.AreEqual(0, player.equipmentSlots.slots[EquipmentSlots.PrimaryWeapon].durabilityDamage);
        }

        [Test] public void Armor_RepairSpendsOneGoldPerStep()
        {
            var player = Equipped(5);
            GridSlotData vest = player.equipmentSlots.slots[EquipmentSlots.Armor];
            vest.durabilityDamage = 200;
            Assert.IsTrue(armor.CanRepair(catalog, player, vest));
            Assert.IsTrue(armor.Repair(catalog, player, vest));
            Assert.AreEqual(110, vest.durabilityDamage);   // 1G 당 90 회복
            Assert.AreEqual(4, player.currency);
        }

        [Test] public void Armor_RepairNeverExceedsMaximum()
        {
            var player = Equipped(3);
            GridSlotData helmet = player.equipmentSlots.slots[EquipmentSlots.Helmet];
            helmet.durabilityDamage = 30;
            Assert.IsTrue(armor.Repair(catalog, player, helmet));
            Assert.AreEqual(0, helmet.durabilityDamage);
            Assert.AreEqual(400, armor.Remaining(catalog, helmet));
            Assert.AreEqual(2, player.currency);
            Assert.IsFalse(armor.Repair(catalog, player, helmet));
        }

        [Test] public void Armor_RepairRejectedWithoutGold()
        {
            var player = Equipped(0);
            GridSlotData vest = player.equipmentSlots.slots[EquipmentSlots.Armor];
            vest.durabilityDamage = 100;
            Assert.IsFalse(armor.CanRepair(catalog, player, vest));
            Assert.IsFalse(armor.Repair(catalog, player, vest));
            Assert.AreEqual(350, armor.Remaining(catalog, vest));
        }

        [Test] public void Armor_DestroyedPieceCannotBeRepaired()
        {
            var player = Equipped(9);
            GridSlotData helmet = player.equipmentSlots.slots[EquipmentSlots.Helmet];
            helmet.durabilityDamage = 400;
            Assert.IsFalse(armor.CanRepair(catalog, player, helmet));
        }

        [Test] public void Armor_FallbackUsedWhenCsvValuesMissing()
        {
            var bare = new ItemCatalog(new List<ItemData> {
                new ItemData { itemId = 19001, itemType = ItemType.Equipment,
                    equipmentSlotType = EquipmentSlotType.Helmet, maxStack = 1 } });
            var tuned = new ArmorDurability { fallbackMaxDurability = 60, fallbackDamagePerHit = 5 };
            GridSlotData slot = Slot(19001);
            Assert.AreEqual(60, tuned.Maximum(bare, slot));
            Assert.AreEqual(5, tuned.DamagePerHit(bare, slot));
            Assert.AreEqual(0, tuned.RepairPerCurrency(bare, slot));
        }

        [Test] public void Armor_JsonRoundTripKeepsWear()
        {
            var data = new PlayerSaveData();
            data.inventoryData.equipmentSlots.slots[EquipmentSlots.Armor].itemId = VestId;
            data.inventoryData.equipmentSlots.slots[EquipmentSlots.Armor].amount = 1;
            armor.ApplyHit(catalog, data.inventoryData);
            var loaded = JsonUtility.FromJson<PlayerSaveData>(JsonUtility.ToJson(data));
            Assert.AreEqual(1, loaded.inventoryData.equipmentSlots.slots[EquipmentSlots.Armor].durabilityDamage);
            Assert.AreEqual(449, armor.Remaining(catalog, loaded.inventoryData.equipmentSlots.slots[EquipmentSlots.Armor]));
        }
    }
}
