using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using NUnit.Framework;

namespace Birdkov.NaYeongMin.Tests
{
    public class EquipmentSlotTests
    {
        private const int PistolId = 30001;
        private const int ShotgunId = 30002;
        private const int HelmetId = 31001;
        private const int ArmorId = 31002;
        private const int PotionId = 21001;

        private PlayerInventoryService service;
        private PlayerInventoryData playerData;

        [SetUp]
        public void SetUp()
        {
            ItemCatalog catalog = new ItemCatalog(new List<ItemData>
            {
                new ItemData { itemId = PistolId, itemType = ItemType.Weapon, displayName = "기관권총", maxStack = 1 },
                new ItemData { itemId = ShotgunId, itemType = ItemType.Weapon, displayName = "샷건", maxStack = 1 },
                new ItemData { itemId = HelmetId, itemType = ItemType.Equipment, equipmentSlotType = EquipmentSlotType.Helmet, maxStack = 1 },
                new ItemData { itemId = ArmorId, itemType = ItemType.Equipment, equipmentSlotType = EquipmentSlotType.Armor, maxStack = 1 },
                new ItemData { itemId = PotionId, itemType = ItemType.Consumable, maxStack = 5, stackable = true }
            });

            service = new PlayerInventoryService(catalog);
            playerData = new PlayerInventoryData();
        }

        private void PutInBag(int slotIndex, int itemId, int amount = 1)
        {
            playerData.inventory.slots[slotIndex].itemId = itemId;
            playerData.inventory.slots[slotIndex].amount = amount;
        }

        [Test]
        public void EquipmentSlots_HaveFourSlotsInPlannedOrder()
        {
            Assert.AreEqual(4, playerData.equipmentSlots.slots.Count);
            Assert.AreEqual(EquipmentSlotType.PrimaryGun, EquipmentSlots.GetSlotType(EquipmentSlots.PrimaryWeapon));
            Assert.AreEqual(EquipmentSlotType.SecondaryGun, EquipmentSlots.GetSlotType(EquipmentSlots.SecondaryWeapon));
            Assert.AreEqual(EquipmentSlotType.Helmet, EquipmentSlots.GetSlotType(EquipmentSlots.Helmet));
            Assert.AreEqual(EquipmentSlotType.Armor, EquipmentSlots.GetSlotType(EquipmentSlots.Armor));
        }

        [Test]
        public void Equip_BothWeaponSlotsEmpty_AlwaysFillsPrimaryFirst()
        {
            PutInBag(0, PistolId);

            // 보조 무기 칸에 놓아도 둘 다 비어 있으면 주 무기 칸으로 들어간다.
            service.EquipFromInventory(playerData, 0, EquipmentSlots.SecondaryWeapon);

            Assert.AreEqual(PistolId, playerData.equipmentSlots.slots[EquipmentSlots.PrimaryWeapon].itemId);
            Assert.IsTrue(playerData.equipmentSlots.slots[EquipmentSlots.SecondaryWeapon].IsEmpty());
        }

        [Test]
        public void Equip_SecondWeaponGoesToRequestedSlot()
        {
            PutInBag(0, PistolId);
            PutInBag(1, ShotgunId);

            service.EquipFromInventory(playerData, 0, EquipmentSlots.PrimaryWeapon);
            service.EquipFromInventory(playerData, 1, EquipmentSlots.SecondaryWeapon);

            Assert.AreEqual(PistolId, playerData.equipmentSlots.slots[EquipmentSlots.PrimaryWeapon].itemId);
            Assert.AreEqual(ShotgunId, playerData.equipmentSlots.slots[EquipmentSlots.SecondaryWeapon].itemId);
        }

        [Test]
        public void Equip_RejectsOccupiedSlot()
        {
            // 기획 확정: 장비 슬롯이 비어 있다는 전제에서만 안착한다. 교환하지 않는다.
            PutInBag(0, PistolId);
            service.EquipFromInventory(playerData, 0, EquipmentSlots.PrimaryWeapon);
            PutInBag(0, ShotgunId);

            InventoryMoveResult result = service.EquipFromInventory(playerData, 0, EquipmentSlots.PrimaryWeapon);

            Assert.AreEqual(InventoryResult.DestinationRejected, result.Result);
            Assert.AreEqual(PistolId, playerData.equipmentSlots.slots[EquipmentSlots.PrimaryWeapon].itemId);
            Assert.AreEqual(ShotgunId, playerData.inventory.slots[0].itemId);
        }

        [Test]
        public void Equip_RejectsOccupiedArmorSlot()
        {
            PutInBag(0, HelmetId);
            service.EquipFromInventory(playerData, 0, EquipmentSlots.Helmet);
            PutInBag(3, HelmetId);

            InventoryMoveResult result = service.EquipFromInventory(playerData, 3, EquipmentSlots.Helmet);

            Assert.AreEqual(InventoryResult.DestinationRejected, result.Result);
            Assert.AreEqual(HelmetId, playerData.inventory.slots[3].itemId);
        }

        [Test]
        public void Unequip_ThenEquip_AllowsSwappingGear()
        {
            // 바꿔 끼우려면 먼저 빼야 한다.
            PutInBag(0, PistolId);
            service.EquipFromInventory(playerData, 0, EquipmentSlots.PrimaryWeapon);
            PutInBag(0, ShotgunId);

            service.UnequipToInventory(playerData, EquipmentSlots.PrimaryWeapon, 1);
            InventoryMoveResult result = service.EquipFromInventory(playerData, 0, EquipmentSlots.PrimaryWeapon);

            Assert.AreEqual(InventoryResult.Success, result.Result);
            Assert.AreEqual(ShotgunId, playerData.equipmentSlots.slots[EquipmentSlots.PrimaryWeapon].itemId);
            Assert.AreEqual(PistolId, playerData.inventory.slots[1].itemId);
        }

        [Test]
        public void WeaponQuickSlots_MirrorEquippedWeapons()
        {
            PutInBag(0, PistolId);
            PutInBag(1, ShotgunId);
            service.EquipFromInventory(playerData, 0, EquipmentSlots.PrimaryWeapon);
            service.EquipFromInventory(playerData, 1, EquipmentSlots.SecondaryWeapon);

            Assert.IsTrue(service.TryGetWeaponQuickSlot(playerData, 0, out int firstSlot, out ItemData first));
            Assert.IsTrue(service.TryGetWeaponQuickSlot(playerData, 1, out int secondSlot, out ItemData second));

            Assert.AreEqual(EquipmentSlots.PrimaryWeapon, firstSlot);
            Assert.AreEqual(PistolId, first.itemId);
            Assert.AreEqual(EquipmentSlots.SecondaryWeapon, secondSlot);
            Assert.AreEqual(ShotgunId, second.itemId);
        }

        [Test]
        public void DestroyEquipped_EmptiesSlotAndWeaponQuickSlot()
        {
            PutInBag(0, PistolId);
            service.EquipFromInventory(playerData, 0, EquipmentSlots.PrimaryWeapon);

            Assert.IsTrue(service.DestroyEquipped(playerData, EquipmentSlots.PrimaryWeapon));

            Assert.IsTrue(playerData.equipmentSlots.slots[EquipmentSlots.PrimaryWeapon].IsEmpty());
            Assert.IsFalse(service.TryGetWeaponQuickSlot(playerData, 0, out _, out _));
        }

        [Test]
        public void Equip_RejectsWrongSlotType()
        {
            PutInBag(0, HelmetId);
            PutInBag(1, PistolId);
            PutInBag(2, PotionId);

            InventoryMoveResult helmetIntoWeapon = service.EquipFromInventory(playerData, 0, EquipmentSlots.PrimaryWeapon);
            InventoryMoveResult weaponIntoHelmet = service.EquipFromInventory(playerData, 1, EquipmentSlots.Helmet);
            InventoryMoveResult potionIntoArmor = service.EquipFromInventory(playerData, 2, EquipmentSlots.Armor);

            Assert.AreEqual(InventoryResult.DestinationRejected, helmetIntoWeapon.Result);
            Assert.AreEqual(InventoryResult.DestinationRejected, weaponIntoHelmet.Result);
            Assert.AreEqual(InventoryResult.DestinationRejected, potionIntoArmor.Result);
        }

        [Test]
        public void Equip_ProtectiveGearGoesToMatchingSlot()
        {
            PutInBag(0, HelmetId);
            PutInBag(1, ArmorId);

            service.EquipFromInventory(playerData, 0, EquipmentSlots.Helmet);
            service.EquipFromInventory(playerData, 1, EquipmentSlots.Armor);

            Assert.AreEqual(HelmetId, playerData.equipmentSlots.slots[EquipmentSlots.Helmet].itemId);
            Assert.AreEqual(ArmorId, playerData.equipmentSlots.slots[EquipmentSlots.Armor].itemId);
        }

        [Test]
        public void AddToInventory_NeverAutoEquips()
        {
            service.AddToInventory(playerData, HelmetId, 1);
            service.AddToInventory(playerData, PistolId, 1);

            Assert.IsTrue(playerData.equipmentSlots.slots.TrueForAll(slot => slot.IsEmpty()));
            Assert.AreEqual(HelmetId, playerData.inventory.slots[0].itemId);
            Assert.AreEqual(PistolId, playerData.inventory.slots[1].itemId);
        }

        [Test]
        public void Unequip_ReturnsGearToBagWithoutLink()
        {
            PutInBag(0, HelmetId);
            service.EquipFromInventory(playerData, 0, EquipmentSlots.Helmet);

            service.UnequipToInventory(playerData, EquipmentSlots.Helmet, 5);

            Assert.IsTrue(playerData.equipmentSlots.slots[EquipmentSlots.Helmet].IsEmpty());
            Assert.AreEqual(HelmetId, playerData.inventory.slots[5].itemId);
        }

        [Test]
        public void ClearOnDeath_EmptiesBagEquipmentAndQuickSlots()
        {
            PutInBag(0, PistolId);
            PutInBag(1, PotionId, 3);
            service.EquipFromInventory(playerData, 0, EquipmentSlots.PrimaryWeapon);
            service.AssignItemQuickSlot(playerData, 0, 1);

            service.ClearOnDeath(playerData);

            Assert.IsTrue(playerData.inventory.slots.TrueForAll(slot => slot.IsEmpty()));
            Assert.IsTrue(playerData.equipmentSlots.slots.TrueForAll(slot => slot.IsEmpty()));
            foreach (int mapped in playerData.itemQuickSlotIndices)
            {
                Assert.AreEqual(InventorySettings.UnassignedQuickSlot, mapped);
            }
        }
    }
}
