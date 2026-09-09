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
        public void Move_RejectsWrongDedicatedSlotTypes()
        {
            PlayerInventoryData playerData = new PlayerInventoryData();
            playerData.inventory.slots[0].itemId = 1;
            playerData.inventory.slots[0].amount = 1;
            PlayerInventoryService service = new PlayerInventoryService(itemCatalog);

            InventoryMoveResult weaponResult = service.Move(playerData, PlayerContainerType.Inventory, 0,
                PlayerContainerType.Weapon, 0, 1);
            InventoryMoveResult quickResult = service.Move(playerData, PlayerContainerType.Inventory, 0,
                PlayerContainerType.Quick, 0, 1);

            Assert.AreEqual(InventoryResult.DestinationRejected, weaponResult.Result);
            Assert.AreEqual(InventoryResult.DestinationRejected, quickResult.Result);
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
        public void ClearOnDeath_ClearsAllCarriedContainers()
        {
            PlayerInventoryData playerData = new PlayerInventoryData();
            playerData.inventory.slots[0].itemId = 1;
            playerData.inventory.slots[0].amount = 5;
            playerData.weaponSlots.slots[0].itemId = 2;
            playerData.weaponSlots.slots[0].amount = 1;
            playerData.quickSlots.slots[0].itemId = 3;
            playerData.quickSlots.slots[0].amount = 1;

            new PlayerInventoryService(itemCatalog).ClearOnDeath(playerData);

            Assert.IsTrue(playerData.inventory.slots.TrueForAll(slot => slot.IsEmpty()));
            Assert.IsTrue(playerData.weaponSlots.slots.TrueForAll(slot => slot.IsEmpty()));
            Assert.IsTrue(playerData.quickSlots.slots.TrueForAll(slot => slot.IsEmpty()));
        }
    }
}
