using Birdkov.NaYeongMin.InventorySystem;
using NUnit.Framework;

namespace Birdkov.NaYeongMin.Tests
{
    public class RecoveryItemTests
    {
        private sealed class TestTarget : IRecoveryTarget
        {
            public bool accepted = true;
            public int calls;
            public float health, hunger, water;

            public bool TryApplyRecovery(float healthRecovery, float hungerRecovery, float waterRecovery)
            {
                calls++;
                health = healthRecovery;
                hunger = hungerRecovery;
                water = waterRecovery;
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
                healthRecovery = 6, hungerRecovery = 12, waterRecovery = 27, stackable = true, maxStack = 5 };
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
            Assert.AreEqual(6, target.health);
            Assert.AreEqual(12, target.hunger);
            Assert.AreEqual(27, target.water);
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
