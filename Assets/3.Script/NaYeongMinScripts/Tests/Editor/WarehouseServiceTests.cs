using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using NUnit.Framework;

namespace Birdkov.NaYeongMin.Tests
{
    public class WarehouseServiceTests
    {
        private ItemCatalog itemCatalog;
        private PlayerInventoryData playerData;

        [SetUp]
        public void SetUp()
        {
            itemCatalog = new ItemCatalog(new List<ItemData>
            {
                new ItemData { itemId = 1, itemType = ItemType.Material, maxStack = 5, stackable = true }
            });

            playerData = new PlayerInventoryData();
            playerData.inventory.slots[0].itemId = 1;
            playerData.inventory.slots[0].amount = 3;
        }

        [Test]
        public void Warehouse_UsesConfiguredSize()
        {
            WarehouseService service = new WarehouseService(itemCatalog, null);

            Assert.AreEqual(InventorySettings.WarehouseWidth, service.Warehouse.width);
            Assert.AreEqual(InventorySettings.WarehouseHeight, service.Warehouse.height);
            Assert.AreEqual(
                InventorySettings.WarehouseWidth * InventorySettings.WarehouseHeight,
                service.Warehouse.slots.Count);
        }

        [Test]
        public void Store_RejectedWhileClosed()
        {
            WarehouseService service = new WarehouseService(itemCatalog, null);

            InventoryMoveResult result = service.Store(playerData, 0, 0, 3);

            Assert.IsFalse(service.IsOpen);
            Assert.AreEqual(InventoryResult.DestinationRejected, result.Result);
            Assert.AreEqual(3, playerData.inventory.slots[0].amount);
        }

        [Test]
        public void StoreAndWithdraw_WorkWhileOpen()
        {
            WarehouseService service = new WarehouseService(itemCatalog, null);
            service.Open();

            InventoryMoveResult stored = service.Store(playerData, 0, 0, 3);
            Assert.AreEqual(InventoryResult.Success, stored.Result);
            Assert.AreEqual(3, service.Warehouse.slots[0].amount);
            Assert.IsTrue(playerData.inventory.slots[0].IsEmpty());

            InventoryMoveResult withdrawn = service.Withdraw(playerData, 0, 4, 2);
            Assert.AreEqual(InventoryResult.Success, withdrawn.Result);
            Assert.AreEqual(2, playerData.inventory.slots[4].amount);
            Assert.AreEqual(1, service.Warehouse.slots[0].amount);
        }

        [Test]
        public void Close_BlocksFurtherMoves()
        {
            WarehouseService service = new WarehouseService(itemCatalog, null);
            service.Open();
            service.Close();

            InventoryMoveResult result = service.Store(playerData, 0, 0, 1);

            Assert.AreEqual(InventoryResult.DestinationRejected, result.Result);
        }

        [Test]
        public void OpenStateChanged_RaisedOncePerChange()
        {
            WarehouseService service = new WarehouseService(itemCatalog, null);
            int raisedCount = 0;
            service.OpenStateChanged += _ => raisedCount++;

            service.Open();
            service.Open();
            service.Close();

            Assert.AreEqual(2, raisedCount);
        }
    }
}
