using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.SaveSystem;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

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
                healthRecovery = 6, hungerRecovery = 12, waterRecovery = 27, stackable = true, maxStack = 5 };   // 최대 30 기준 절대량
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
        [TestCase(31f)]
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

    public class CraftingServiceTests
    {
        private CraftingService craftingService;
        private PlayerInventoryService playerService;
        private PlayerInventoryData player;

        [SetUp]
        public void SetUp()
        {
            ItemData[] items =
            {
                new ItemData { itemId = 27001, itemType = ItemType.Material, displayName = "빨간 버섯", stackable = true, maxStack = 5 },
                new ItemData { itemId = 27002, itemType = ItemType.Material, displayName = "파란 버섯", stackable = true, maxStack = 5 },
                new ItemData { itemId = 25001, itemType = ItemType.Material, displayName = "화약", stackable = true, maxStack = 5 },
                new ItemData { itemId = 11001, itemType = ItemType.Ammo, displayName = "기관권총 총알", magazineSize = 20, stackable = true, maxStack = 160 },
                new ItemData { itemId = 11002, itemType = ItemType.Ammo, displayName = "샷건 총알", magazineSize = 20, stackable = true, maxStack = 100 },
                new ItemData { itemId = 21001, itemType = ItemType.Consumable, displayName = "회복약", stackable = true, maxStack = 5 }
            };

            ItemCatalog catalog = new ItemCatalog(items);
            craftingService = new CraftingService(catalog);
            playerService = new PlayerInventoryService(catalog);
            player = new PlayerInventoryData();
        }

        private void Give(int itemId, int amount)
        {
            playerService.AddToInventory(player, itemId, amount);
        }

        private int Count(int itemId)
        {
            return craftingService.Count(player.inventory, itemId);
        }

        [TestCase(27001, 11001)]
        [TestCase(27002, 11002)]
        public void Craft_MushroomDecidesAmmoType(int mushroomItemId, int expectedAmmoItemId)
        {
            Give(mushroomItemId, 5);
            Give(CraftingService.GunpowderItemId, 5);

            Assert.AreEqual(CraftResult.Success, craftingService.Craft(player, mushroomItemId));
            Assert.AreEqual(20, Count(expectedAmmoItemId));   // 탄약 1개 = 1발, 제작 1회 = 20발
            Assert.AreEqual(0, Count(mushroomItemId));
            Assert.AreEqual(0, Count(CraftingService.GunpowderItemId));
        }

        [Test]
        public void Craft_KeepsSurplusMaterials()
        {
            Give(27001, 7);
            Give(CraftingService.GunpowderItemId, 9);

            Assert.AreEqual(CraftResult.Success, craftingService.Craft(player, 27001));
            Assert.AreEqual(2, Count(27001));
            Assert.AreEqual(4, Count(CraftingService.GunpowderItemId));
        }

        [Test]
        public void Craft_NotEnoughMushroomDoesNotConsume()
        {
            Give(27001, 4);
            Give(CraftingService.GunpowderItemId, 5);

            Assert.AreEqual(CraftResult.NotEnoughMushroom, craftingService.Craft(player, 27001));
            Assert.AreEqual(4, Count(27001));
            Assert.AreEqual(5, Count(CraftingService.GunpowderItemId));
            Assert.AreEqual(0, Count(11001));
        }

        [Test]
        public void Craft_NotEnoughGunpowderDoesNotConsume()
        {
            Give(27001, 5);
            Give(CraftingService.GunpowderItemId, 4);

            Assert.AreEqual(CraftResult.NotEnoughGunpowder, craftingService.Craft(player, 27001));
            Assert.AreEqual(5, Count(27001));
            Assert.AreEqual(4, Count(CraftingService.GunpowderItemId));
        }

        [Test]
        public void Craft_UnknownRecipeRejected()
        {
            Give(21001, 5);
            Give(CraftingService.GunpowderItemId, 5);

            Assert.AreEqual(CraftResult.UnknownRecipe, craftingService.Craft(player, 21001));
            Assert.AreEqual(5, Count(21001));
        }

        // 재료를 빼면 칸이 생기므로 꽉 찬 가방에서도 제작이 성립해야 한다.
        [Test]
        public void Craft_FullBagStillWorksBecauseMaterialsFreeSlots()
        {
            Give(27001, 5);
            Give(CraftingService.GunpowderItemId, 5);
            Give(21001, (InventorySettings.InventorySlotCount - 2) * 5);

            Assert.IsFalse(player.inventory.slots.Exists(slot => slot.IsEmpty()));
            Assert.AreEqual(CraftResult.Success, craftingService.Craft(player, 27001));
            Assert.AreEqual(20, Count(11001));
        }

        // 재료가 여러 칸에 흩어져 있어도 합산해 소모한다.
        [Test]
        public void Craft_ConsumesAcrossSplitStacks()
        {
            Give(27001, 3);
            player.inventory.slots[5].itemId = 27001;
            player.inventory.slots[5].amount = 2;
            Give(CraftingService.GunpowderItemId, 5);

            Assert.AreEqual(5, Count(27001));
            Assert.AreEqual(CraftResult.Success, craftingService.Craft(player, 27001));
            Assert.AreEqual(0, Count(27001));
        }
    }

    public class AmmoRoundsTests
    {
        private const int AmmoItemId = 11001;
        private PlayerInventoryService service;
        private PlayerInventoryData player;

        [SetUp]
        public void SetUp()
        {
            ItemData[] items =
            {
                new ItemData { itemId = AmmoItemId, itemType = ItemType.Ammo, displayName = "기관권총 총알", magazineSize = 20, stackable = true, maxStack = 5 },
                new ItemData { itemId = 10001, itemType = ItemType.Weapon, displayName = "기관권총", magazineSize = 20 }
            };

            service = new PlayerInventoryService(new ItemCatalog(items));
            player = new PlayerInventoryData();
        }

        private GridSlotData AmmoSlot()
        {
            return player.inventory.slots.Find(slot => slot.itemId == AmmoItemId);
        }

        [Test]
        public void RoundsPerBox_ReadsMagazineSize()
        {
            Assert.AreEqual(20, service.GetRoundsPerBox(AmmoItemId));
            Assert.AreEqual(0, service.GetRoundsPerBox(10001));
        }

        // 20발 장전은 박스 1개만 소모한다. 발 수를 박스 수로 넘기면 400발이 사라진다.
        [Test]
        public void Consume_FullMagazineTakesOneBox()
        {
            service.AddToInventory(player, AmmoItemId, 3);

            Assert.AreEqual(20, service.ConsumeAmmoRounds(player, AmmoItemId, 20));
            Assert.AreEqual(2, AmmoSlot().amount);
            Assert.AreEqual(0, AmmoSlot().remainingRounds);
            Assert.AreEqual(40, service.GetAmmoRounds(player, AmmoItemId));
        }

        // b안: 뜯고 남은 발은 버리지 않고 같은 슬롯에 되돌린다.
        [Test]
        public void Consume_PartialBoxIsKeptInSlot()
        {
            service.AddToInventory(player, AmmoItemId, 2);

            Assert.AreEqual(5, service.ConsumeAmmoRounds(player, AmmoItemId, 5));
            Assert.AreEqual(2, AmmoSlot().amount);
            Assert.AreEqual(15, AmmoSlot().remainingRounds);
            Assert.AreEqual(35, service.GetAmmoRounds(player, AmmoItemId));
        }

        // 뜯다 만 박스를 먼저 비우고 나서 새 박스를 뜯는다.
        [Test]
        public void Consume_UsesPartialBoxFirst()
        {
            service.AddToInventory(player, AmmoItemId, 2);
            service.ConsumeAmmoRounds(player, AmmoItemId, 5);

            Assert.AreEqual(15, service.ConsumeAmmoRounds(player, AmmoItemId, 15));
            Assert.AreEqual(1, AmmoSlot().amount);
            Assert.AreEqual(0, AmmoSlot().remainingRounds);
            Assert.AreEqual(20, service.GetAmmoRounds(player, AmmoItemId));
        }

        // 잔탄 15 + 새 박스 5 = 20. 새 박스에는 15발이 남는다.
        [Test]
        public void Consume_SpansPartialAndNewBox()
        {
            service.AddToInventory(player, AmmoItemId, 3);
            service.ConsumeAmmoRounds(player, AmmoItemId, 5);

            Assert.AreEqual(20, service.ConsumeAmmoRounds(player, AmmoItemId, 20));
            Assert.AreEqual(2, AmmoSlot().amount);
            Assert.AreEqual(15, AmmoSlot().remainingRounds);
            Assert.AreEqual(35, service.GetAmmoRounds(player, AmmoItemId));
        }

        [Test]
        public void Consume_ShortStockReturnsWhatIsAvailable()
        {
            service.AddToInventory(player, AmmoItemId, 1);

            Assert.AreEqual(20, service.ConsumeAmmoRounds(player, AmmoItemId, 30));
            Assert.IsNull(AmmoSlot());
            Assert.AreEqual(0, service.GetAmmoRounds(player, AmmoItemId));
        }

        [Test]
        public void Consume_NoStockConsumesNothing()
        {
            Assert.AreEqual(0, service.ConsumeAmmoRounds(player, AmmoItemId, 20));
        }

        // 잔탄은 세이브에 실려야 한다.
        [Test]
        public void PartialBox_SurvivesJsonRoundTrip()
        {
            service.AddToInventory(player, AmmoItemId, 2);
            service.ConsumeAmmoRounds(player, AmmoItemId, 5);

            PlayerSaveData save = new PlayerSaveData { inventoryData = player };
            PlayerSaveData loaded = JsonUtility.FromJson<PlayerSaveData>(JsonUtility.ToJson(save));
            GridSlotData slot = loaded.inventoryData.inventory.slots.Find(s => s.itemId == AmmoItemId);

            Assert.AreEqual(5, loaded.saveVersion);
            Assert.AreEqual(2, slot.amount);
            Assert.AreEqual(15, slot.remainingRounds);
        }

        // 슬롯을 통째로 옮기면 잔탄도 따라간다.
        [Test]
        public void PartialBox_FollowsWholeSlotMove()
        {
            service.AddToInventory(player, AmmoItemId, 1);
            service.ConsumeAmmoRounds(player, AmmoItemId, 5);
            int from = player.inventory.slots.FindIndex(slot => slot.itemId == AmmoItemId);

            service.Move(player, PlayerContainerType.Inventory, from, PlayerContainerType.Inventory, 10, 1);

            Assert.AreEqual(15, player.inventory.slots[10].remainingRounds);
            Assert.AreEqual(0, player.inventory.slots[from].remainingRounds);
            Assert.AreEqual(15, service.GetAmmoRounds(player, AmmoItemId));
        }
    }
}
