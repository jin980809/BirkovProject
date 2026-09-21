using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Birdkov.NaYeongMin.Tests
{
    // 임시 규칙: 헬멧/조끼 전 등급 최대 100, 피격당 2, 수리비 3G 고정. 0 이 되면 소멸.
    public class ArmorDurabilityTests
    {
        private const int HelmetId = 13001;
        private const int VestId = 12001;
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

        [TestCase(13001)] [TestCase(13002)] [TestCase(13003)]
        [TestCase(12001)] [TestCase(12002)] [TestCase(12003)]
        public void Armor_AllGradesShareTemporaryMaximum(int itemId)
        {
            var slot = new GridSlotData { itemId = itemId, amount = 1 };
            Assert.IsTrue(armor.IsArmor(catalog, slot));
            Assert.AreEqual(100, armor.Remaining(slot));
        }

        [Test] public void Armor_OneHitWearsEveryEquippedPiece()
        {
            var player = Equipped();
            armor.ApplyHit(catalog, player);
            Assert.AreEqual(98, armor.Remaining(player.equipmentSlots.slots[EquipmentSlots.Helmet]));
            Assert.AreEqual(98, armor.Remaining(player.equipmentSlots.slots[EquipmentSlots.Armor]));
            armor.ApplyHit(catalog, player);
            Assert.AreEqual(96, armor.Remaining(player.equipmentSlots.slots[EquipmentSlots.Helmet]));
            Assert.AreEqual(96, armor.Remaining(player.equipmentSlots.slots[EquipmentSlots.Armor]));
        }

        [Test] public void Armor_DestroyedWhenDurabilityHitsZero()
        {
            var player = Equipped();
            player.equipmentSlots.slots[EquipmentSlots.Helmet].durabilityDamage = 98;
            armor.ApplyHit(catalog, player);
            Assert.IsTrue(player.equipmentSlots.slots[EquipmentSlots.Helmet].IsEmpty());
            Assert.AreEqual(98, armor.Remaining(player.equipmentSlots.slots[EquipmentSlots.Armor]));
        }

        [Test] public void Armor_WeaponSlotsAreNotWorn()
        {
            var player = new PlayerInventoryData();
            player.equipmentSlots.slots[EquipmentSlots.PrimaryWeapon].itemId = WeaponId;
            player.equipmentSlots.slots[EquipmentSlots.PrimaryWeapon].amount = 1;
            armor.ApplyHit(catalog, player);
            Assert.AreEqual(0, player.equipmentSlots.slots[EquipmentSlots.PrimaryWeapon].durabilityDamage);
        }

        [Test] public void Armor_RepairCostsFixedGoldAndRestoresFully()
        {
            var player = Equipped(3);
            player.equipmentSlots.slots[EquipmentSlots.Armor].durabilityDamage = 40;
            GridSlotData vest = player.equipmentSlots.slots[EquipmentSlots.Armor];
            Assert.IsTrue(armor.CanRepair(catalog, player, vest));
            Assert.IsTrue(armor.Repair(catalog, player, vest));
            Assert.AreEqual(100, armor.Remaining(vest));
            Assert.AreEqual(0, player.currency);
            Assert.IsFalse(armor.Repair(catalog, player, vest));
        }

        [Test] public void Armor_RepairRejectedWithoutGold()
        {
            var player = Equipped(2);
            GridSlotData vest = player.equipmentSlots.slots[EquipmentSlots.Armor];
            vest.durabilityDamage = 40;
            Assert.IsFalse(armor.CanRepair(catalog, player, vest));
            Assert.IsFalse(armor.Repair(catalog, player, vest));
            Assert.AreEqual(60, armor.Remaining(vest));
        }

        [Test] public void Armor_TuningFieldsAreRespected()
        {
            var tuned = new ArmorDurability { maxDurability = 50, damagePerHit = 5, repairCost = 1 };
            var player = Equipped(1);
            Assert.AreEqual(50, tuned.Remaining(player.equipmentSlots.slots[EquipmentSlots.Helmet]));
            tuned.ApplyHit(catalog, player);
            Assert.AreEqual(45, tuned.Remaining(player.equipmentSlots.slots[EquipmentSlots.Helmet]));
            Assert.IsTrue(tuned.Repair(catalog, player, player.equipmentSlots.slots[EquipmentSlots.Helmet]));
            Assert.AreEqual(0, player.currency);
        }

        [Test] public void Armor_JsonRoundTripKeepsWear()
        {
            var data = new PlayerSaveData();
            data.inventoryData.equipmentSlots.slots[EquipmentSlots.Armor].itemId = VestId;
            data.inventoryData.equipmentSlots.slots[EquipmentSlots.Armor].amount = 1;
            armor.ApplyHit(catalog, data.inventoryData);
            var loaded = JsonUtility.FromJson<PlayerSaveData>(JsonUtility.ToJson(data));
            Assert.AreEqual(2, loaded.inventoryData.equipmentSlots.slots[EquipmentSlots.Armor].durabilityDamage);
            Assert.AreEqual(98, armor.Remaining(loaded.inventoryData.equipmentSlots.slots[EquipmentSlots.Armor]));
        }
    }
}
