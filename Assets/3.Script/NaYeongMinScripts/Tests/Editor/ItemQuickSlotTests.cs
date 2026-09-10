using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using NUnit.Framework;

namespace Birdkov.NaYeongMin.Tests
{
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
}
