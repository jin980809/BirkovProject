using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.SaveSystem;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System;

// 저장 복구, 창고, 회복 아이템 테스트.
namespace Birdkov.NaYeongMin.Tests
{
    public class JsonSaveSystemTests
    {
        private string saveDirectory;

        [SetUp]
        public void SetUp()
        {
            saveDirectory = Path.Combine(Path.GetTempPath(), "BirdkovNaYeongMinTests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(saveDirectory)) Directory.Delete(saveDirectory, true);
        }

        [Test]
        public void TryLoad_RecoversPreviousValidBackup_WhenMainIsCorrupted()
        {
            JsonSaveSystem saveSystem = new JsonSaveSystem(saveDirectory);
            PlayerSaveData first = new PlayerSaveData();
            first.inventoryData.inventory.slots[0].itemId = 10;
            first.inventoryData.inventory.slots[0].amount = 1;
            saveSystem.Save("player", first);

            PlayerSaveData second = new PlayerSaveData();
            second.inventoryData.inventory.slots[0].itemId = 20;
            second.inventoryData.inventory.slots[0].amount = 1;
            saveSystem.Save("player", second);
            File.WriteAllText(Path.Combine(saveDirectory, "player.json"), "{corrupted");

            bool loaded = saveSystem.TryLoad("player", out PlayerSaveData restored, out bool recovered);

            Assert.IsTrue(loaded);
            Assert.IsTrue(recovered);
            Assert.AreEqual(10, restored.inventoryData.inventory.slots[0].itemId);
        }

        [Test]
        public void Save_RejectsUnsafeSlotName()
        {
            JsonSaveSystem saveSystem = new JsonSaveSystem(saveDirectory);
            Assert.Throws<ArgumentException>(() => saveSystem.Save("../player", new PlayerSaveData()));
        }
    }

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

    public class RecoveryItemTests
    {
        private sealed class TestTarget : IRecoveryTarget
        {
            public bool accepted = true;
            public int calls;
            public float health, hunger, water;

            public bool TryApplyRecovery(float healthPercent, float hungerPercent, float waterPercent)
            {
                calls++;
                health = healthPercent;
                hunger = hungerPercent;
                water = waterPercent;
                return accepted;
            }
        }

        private ItemData item;
        private PlayerInventoryData player;
        private PlayerInventoryService service;
        private TestTarget target;

        [SetUp]
        public void SetUp()
        {
            item = new ItemData { itemId = 21001, itemType = ItemType.Consumable,
                healthRecovery = 20, hungerRecovery = 40, waterRecovery = 90, stackable = true, maxStack = 5 };
            service = new PlayerInventoryService(new ItemCatalog(new[] { item }));
            player = new PlayerInventoryData();
            service.AddToInventory(player, item.itemId, 2);
            service.AssignItemQuickSlot(player, 0, 0);
            target = new TestTarget();
        }

        [Test]
        public void Use_PassesAmountsAndConsumesOne()
        {
            Assert.AreEqual(InventoryResult.Success, service.UseRecoveryItem(player, 0, target));
            Assert.AreEqual(1, target.calls);
            Assert.AreEqual(20, target.health);
            Assert.AreEqual(40, target.hunger);
            Assert.AreEqual(90, target.water);
            Assert.AreEqual(1, player.inventory.slots[0].amount);
            Assert.AreEqual(0, player.itemQuickSlotIndices[0]);
        }

        [Test]
        public void Use_LastItemClearsQuickLink()
        {
            player.inventory.slots[0].amount = 1;
            Assert.AreEqual(InventoryResult.Success, service.UseRecoveryItem(player, 0, target));
            Assert.IsTrue(player.inventory.slots[0].IsEmpty());
            Assert.AreEqual(InventorySettings.UnassignedQuickSlot, player.itemQuickSlotIndices[0]);
        }

        [Test]
        public void Use_RejectedEffectDoesNotConsume()
        {
            target.accepted = false;
            Assert.AreEqual(InventoryResult.DestinationRejected, service.UseRecoveryItem(player, 0, target));
            Assert.AreEqual(2, player.inventory.slots[0].amount);
            Assert.AreEqual(0, player.itemQuickSlotIndices[0]);
        }

        [Test]
        public void Use_MissingTargetDoesNotConsume()
        {
            Assert.AreEqual(InventoryResult.DestinationRejected, service.UseRecoveryItem(player, 0, null));
            Assert.AreEqual(2, player.inventory.slots[0].amount);
        }

        [TestCase(ItemType.Weapon)]
        [TestCase(ItemType.Special)]
        [TestCase(ItemType.Material)]
        public void Use_NonConsumableDoesNotApply(ItemType itemType)
        {
            item.itemType = itemType;
            Assert.AreEqual(InventoryResult.DestinationRejected, service.UseRecoveryItem(player, 0, target));
            Assert.AreEqual(0, target.calls);
            Assert.AreEqual(2, player.inventory.slots[0].amount);
        }

        [TestCase(-1f)]
        [TestCase(101f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Use_InvalidRecoveryDoesNotApply(float amount)
        {
            item.healthRecovery = amount;
            Assert.AreEqual(InventoryResult.DestinationRejected, service.UseRecoveryItem(player, 0, target));
            Assert.AreEqual(0, target.calls);
            Assert.AreEqual(2, player.inventory.slots[0].amount);
        }

        [Test]
        public void Use_ZeroRecoveryDoesNotApply()
        {
            item.healthRecovery = item.hungerRecovery = item.waterRecovery = 0;
            Assert.AreEqual(InventoryResult.DestinationRejected, service.UseRecoveryItem(player, 0, target));
            Assert.AreEqual(0, target.calls);
        }

        [Test]
        public void Use_InvalidOrEmptySlotDoesNotApply()
        {
            Assert.AreEqual(InventoryResult.InvalidSlot, service.UseRecoveryItem(null, 0, target));
            Assert.AreEqual(InventoryResult.InvalidSlot, service.UseRecoveryItem(player, -1, target));
            Assert.AreEqual(InventoryResult.InvalidSlot, service.UseRecoveryItem(player, 25, target));
            Assert.AreEqual(InventoryResult.ItemNotFound, service.UseRecoveryItem(player, 1, target));
            Assert.AreEqual(0, target.calls);
        }
    }
}
