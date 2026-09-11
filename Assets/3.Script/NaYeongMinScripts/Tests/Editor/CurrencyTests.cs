using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using NUnit.Framework;

namespace Birdkov.NaYeongMin.Tests
{
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
