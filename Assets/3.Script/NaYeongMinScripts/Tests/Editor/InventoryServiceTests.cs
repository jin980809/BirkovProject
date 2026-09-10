using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using NUnit.Framework;

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
}
