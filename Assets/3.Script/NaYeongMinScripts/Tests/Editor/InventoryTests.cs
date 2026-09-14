using Birdkov.NaYeongMin.InventorySystem;
using NUnit.Framework;
using System.Collections.Generic;

// 인벤토리, 장비 슬롯, 퀵슬롯, 화폐 테스트.
namespace Birdkov.NaYeongMin.Tests
{
    public class InventoryServiceTests
    {
        private ItemCatalog itemCatalog;

        [SetUp]
        public void SetUp()
        {
            itemCatalog = new ItemCatalog(new List<ItemData>
            {
                new ItemData { itemId = 1, itemType = ItemType.Material, maxStack = 99, stackable = true },
                new ItemData { itemId = 2, itemType = ItemType.Weapon, maxStack = 99, stackable = true },
                new ItemData { itemId = 3, itemType = ItemType.Special, maxStack = 1 }
            });
        }

        [Test]
        public void AddItem_UsesFiveItemStackLimit()
        {
            GridContainerData container = new GridContainerData(2, 1);
            InventoryMoveResult result = new InventoryService(itemCatalog).AddItem(container, 1, 7);

            Assert.AreEqual(InventoryResult.Success, result.Result);
            Assert.AreEqual(5, container.slots[0].amount);
            Assert.AreEqual(2, container.slots[1].amount);
        }

        [Test]
        public void AddItem_ForcesWeaponToSingleItemStacks()
        {
            GridContainerData container = new GridContainerData(2, 1);

            new InventoryService(itemCatalog).AddItem(container, 2, 2);

            Assert.AreEqual(1, container.slots[0].amount);
            Assert.AreEqual(1, container.slots[1].amount);
        }

        [Test]
        public void Move_RejectsNonWeaponIntoWeaponSlot()
        {
            PlayerInventoryData playerData = new PlayerInventoryData();
            playerData.inventory.slots[0].itemId = 1;
            playerData.inventory.slots[0].amount = 1;
            PlayerInventoryService service = new PlayerInventoryService(itemCatalog);

            InventoryMoveResult result = service.Move(
                playerData,
                PlayerContainerType.Inventory, 0,
                PlayerContainerType.Equipment, EquipmentSlots.PrimaryWeapon, 1);

            Assert.AreEqual(InventoryResult.DestinationRejected, result.Result);
        }

        [Test]
        public void Move_WithinInventorySucceeds()
        {
            PlayerInventoryData playerData = new PlayerInventoryData();
            playerData.inventory.slots[0].itemId = 1;
            playerData.inventory.slots[0].amount = 4;
            PlayerInventoryService service = new PlayerInventoryService(itemCatalog);

            InventoryMoveResult result = service.Move(
                playerData,
                PlayerContainerType.Inventory, 0,
                PlayerContainerType.Inventory, 9, 4);

            Assert.AreEqual(InventoryResult.Success, result.Result);
            Assert.AreEqual(4, playerData.inventory.slots[9].amount);
            Assert.IsTrue(playerData.inventory.slots[0].IsEmpty());
        }

        [Test]
        public void ClearOnDeath_ClearsBagAndEquipment()
        {
            PlayerInventoryData playerData = new PlayerInventoryData();
            playerData.inventory.slots[0].itemId = 1;
            playerData.inventory.slots[0].amount = 5;
            playerData.equipmentSlots.slots[EquipmentSlots.PrimaryWeapon].itemId = 2;
            playerData.equipmentSlots.slots[EquipmentSlots.PrimaryWeapon].amount = 1;

            new PlayerInventoryService(itemCatalog).ClearOnDeath(playerData);

            Assert.IsTrue(playerData.inventory.slots.TrueForAll(slot => slot.IsEmpty()));
            Assert.IsTrue(playerData.equipmentSlots.slots.TrueForAll(slot => slot.IsEmpty()));
        }
    }

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

    public class ItemQuickSlotTests
    {
        private const int ConsumableId = 21001;
        private const int SpecialId = 24002;
        private const int WeaponId = 30001;

        private PlayerInventoryService service;
        private PlayerInventoryData playerData;

        [SetUp]
        public void SetUp()
        {
            ItemCatalog catalog = new ItemCatalog(new List<ItemData>
            {
                new ItemData { itemId = ConsumableId, itemType = ItemType.Consumable, maxStack = 5, stackable = true },
                new ItemData { itemId = SpecialId, itemType = ItemType.Special, maxStack = 1 },
                new ItemData { itemId = WeaponId, itemType = ItemType.Weapon, maxStack = 1 }
            });

            service = new PlayerInventoryService(catalog);
            playerData = new PlayerInventoryData();
        }

        [Test]
        public void ItemQuickSlots_StartUnassigned()
        {
            Assert.AreEqual(InventorySettings.ItemQuickSlotCount, playerData.itemQuickSlotIndices.Length);
            foreach (int mapped in playerData.itemQuickSlotIndices)
            {
                Assert.AreEqual(InventorySettings.UnassignedQuickSlot, mapped);
            }
        }

        [Test]
        public void Assign_KeepsItemInBag()
        {
            service.AddToInventory(playerData, ConsumableId, 3);

            Assert.IsTrue(service.AssignItemQuickSlot(playerData, 0, 0));
            Assert.IsTrue(service.TryGetItemQuickSlot(playerData, 0, out int inventoryIndex, out ItemData item));

            Assert.AreEqual(0, inventoryIndex);
            Assert.AreEqual(ConsumableId, item.itemId);
            Assert.AreEqual(ConsumableId, playerData.inventory.slots[0].itemId);
            Assert.AreEqual(3, playerData.inventory.slots[0].amount);
        }

        [Test]
        public void Assign_AcceptsSpecialButRejectsWeapon()
        {
            service.AddToInventory(playerData, SpecialId, 1);
            playerData.inventory.slots[1].itemId = WeaponId;
            playerData.inventory.slots[1].amount = 1;

            Assert.IsTrue(service.AssignItemQuickSlot(playerData, 0, 0));
            Assert.IsFalse(service.AssignItemQuickSlot(playerData, 1, 1));
        }

        [Test]
        public void Assign_RejectsEmptySlotAndBadIndex()
        {
            Assert.IsFalse(service.AssignItemQuickSlot(playerData, 0, 0));
            Assert.IsFalse(service.AssignItemQuickSlot(playerData, 99, 0));
            Assert.IsFalse(service.AssignItemQuickSlot(playerData, 0, 99));
        }

        [Test]
        public void Assign_MovesMappingWhenSameBagSlotReused()
        {
            service.AddToInventory(playerData, ConsumableId, 1);
            service.AssignItemQuickSlot(playerData, 0, 0);

            service.AssignItemQuickSlot(playerData, 2, 0);

            Assert.AreEqual(InventorySettings.UnassignedQuickSlot, playerData.itemQuickSlotIndices[0]);
            Assert.AreEqual(0, playerData.itemQuickSlotIndices[2]);
        }

        [Test]
        public void Sanitize_EmptiesSlotWhenItemConsumed()
        {
            service.AddToInventory(playerData, ConsumableId, 1);
            service.AssignItemQuickSlot(playerData, 0, 0);

            // 아이템을 다 써서 가방 칸이 비면 퀵슬롯도 즉시 비어야 한다.
            playerData.inventory.slots[0].Clear();
            service.SanitizeItemQuickSlots(playerData);

            Assert.AreEqual(InventorySettings.UnassignedQuickSlot, playerData.itemQuickSlotIndices[0]);
            Assert.IsFalse(service.TryGetItemQuickSlot(playerData, 0, out _, out _));
        }

        [Test]
        public void Move_SanitizesStaleMapping()
        {
            service.AddToInventory(playerData, ConsumableId, 1);
            service.AssignItemQuickSlot(playerData, 0, 0);

            service.Move(playerData, PlayerContainerType.Inventory, 0, PlayerContainerType.Inventory, 7, 1);

            Assert.AreEqual(InventorySettings.UnassignedQuickSlot, playerData.itemQuickSlotIndices[0]);
        }

        [Test]
        public void ClearItemQuickSlot_Unassigns()
        {
            service.AddToInventory(playerData, ConsumableId, 1);
            service.AssignItemQuickSlot(playerData, 1, 0);

            Assert.IsTrue(service.ClearItemQuickSlot(playerData, 1));
            Assert.AreEqual(InventorySettings.UnassignedQuickSlot, playerData.itemQuickSlotIndices[1]);
        }
    }

    // 지푸라기는 가방 25칸을 차지하지 않는 재화다. 기획서 10.1 [지푸라기에 대해].
    public class CurrencyTests
    {
        private const int StrawId = 20001;
        private const int PotionId = 21001;

        private PlayerInventoryService service;
        private PlayerInventoryData playerData;

        [SetUp]
        public void SetUp()
        {
            ItemCatalog catalog = new ItemCatalog(new List<ItemData>
            {
                new ItemData { itemId = StrawId, itemType = ItemType.Currency, maxStack = 5, stackable = true },
                new ItemData { itemId = PotionId, itemType = ItemType.Consumable, maxStack = 5, stackable = true }
            });

            service = new PlayerInventoryService(catalog);
            playerData = new PlayerInventoryData();
        }

        [Test]
        public void Currency_StartsAtZero()
        {
            Assert.AreEqual(0, playerData.currency);
        }

        [Test]
        public void AddToInventory_CurrencyNeverTakesBagSlot()
        {
            InventoryMoveResult result = service.AddToInventory(playerData, StrawId, 17);

            Assert.AreEqual(InventoryResult.Success, result.Result);
            Assert.AreEqual(17, result.MovedAmount);
            Assert.AreEqual(17, playerData.currency);
            Assert.IsTrue(playerData.inventory.slots.TrueForAll(slot => slot.IsEmpty()));
        }

        [Test]
        public void AddToInventory_CurrencyAccumulates()
        {
            service.AddToInventory(playerData, StrawId, 5);
            service.AddToInventory(playerData, StrawId, 12);

            Assert.AreEqual(17, playerData.currency);
        }

        [Test]
        public void AddToInventory_CurrencyIgnoresBagCapacity()
        {
            // 가방을 가득 채워도 화폐는 계속 들어온다.
            for (int index = 0; index < InventorySettings.InventorySlotCount; index++)
            {
                playerData.inventory.slots[index].itemId = PotionId;
                playerData.inventory.slots[index].amount = 5;
            }

            InventoryMoveResult result = service.AddToInventory(playerData, StrawId, 30);

            Assert.AreEqual(InventoryResult.Success, result.Result);
            Assert.AreEqual(30, playerData.currency);
        }

        [Test]
        public void AddToInventory_RejectsNonPositiveCurrency()
        {
            Assert.AreEqual(InventoryResult.InvalidAmount, service.AddToInventory(playerData, StrawId, 0).Result);
            Assert.AreEqual(0, playerData.currency);
        }

        [Test]
        public void ClearOnDeath_WipesCurrency()
        {
            service.AddToInventory(playerData, StrawId, 40);

            service.ClearOnDeath(playerData);

            Assert.AreEqual(0, playerData.currency);
        }

        [Test]
        public void NonCurrencyStillUsesBag()
        {
            service.AddToInventory(playerData, PotionId, 3);

            Assert.AreEqual(0, playerData.currency);
            Assert.AreEqual(PotionId, playerData.inventory.slots[0].itemId);
            Assert.AreEqual(3, playerData.inventory.slots[0].amount);
        }
    }
}
