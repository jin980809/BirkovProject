using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.Rng;
using Birdkov.NaYeongMin.InventoryTest;
using NUnit.Framework;
using UnityEngine;

// UI 개편에 딸린 데이터 규칙 검증. 화면 조작이 아니라 컨테이너 규칙만 본다.
namespace Birdkov.NaYeongMin.Tests
{
    public class SlotTransferRegressionTests
    {
        private GameObject root;
        private InventoryTestBench bench;
        private LootContainerData loot;
        private WarehouseService warehouse;

        private void Set(string name, object value) => typeof(InventoryTestBench).GetField(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(bench, value);
        private object Call(string name, params object[] args) => typeof(InventoryTestBench).GetMethod(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(bench, args);

        [SetUp] public void SetUp()
        {
            root = new GameObject("SlotTransferRegression");
            bench = root.AddComponent<InventoryTestBench>();
            bench.itemCsv = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/DataTeble/NaYeongMinCsvData/ItemData.csv");
            bench.useExternalTestSeeder = true;
            Call("Initialize");
            bench.ResetAll();
            loot = new LootContainerData(LootContainerSize.Box2x4);
            Set("loot", loot);
            var catalog = new ItemCatalog(ItemCsvLoader.Parse(bench.itemCsv.text));
            warehouse = new WarehouseService(catalog, bench.WarehouseData);
            Set("warehouseService", warehouse);
        }

        [TearDown] public void TearDown() { Object.DestroyImmediate(root); }

        private TestSlotView Slot(TestContainer container, int index = 0)
        {
            var node = new GameObject("Slot"); node.transform.SetParent(root.transform);
            var slot = node.AddComponent<TestSlotView>(); slot.bench = bench; slot.container = container; slot.index = index;
            return slot;
        }

        private void DoubleClick(TestSlotView slot)
        {
            slot.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(null)
                { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left, clickCount = 2 });
        }

        [TestCase(TestContainer.Loot)] [TestCase(TestContainer.Warehouse)]
        public void Currency_DragCreditsBalanceEvenWithFullBag(TestContainer source)
        {
            warehouse.Open();
            foreach (var slot in bench.PlayerData.inventory.slots) { slot.itemId = 10001; slot.amount = 1; }
            var external = source == TestContainer.Loot ? loot.loot : bench.WarehouseData;
            external.slots[0].itemId = 20001; external.slots[0].amount = 17;
            var result = (InventoryMoveResult)Call("Transfer", source, 0, TestContainer.Bag, 0);
            Assert.AreEqual(17, result.MovedAmount); Assert.AreEqual(17, bench.PlayerData.currency);
            Assert.IsTrue(external.slots[0].IsEmpty()); Assert.AreEqual(10001, bench.PlayerData.inventory.slots[0].itemId);
        }

        [TestCase(TestContainer.Loot)] [TestCase(TestContainer.Warehouse)]
        public void Currency_DoubleClickCreditsBalance(TestContainer source)
        {
            warehouse.Open();
            var external = source == TestContainer.Loot ? loot.loot : bench.WarehouseData;
            external.slots[0].itemId = 20001; external.slots[0].amount = 7;
            DoubleClick(Slot(source));
            Assert.AreEqual(7, bench.PlayerData.currency); Assert.IsTrue(external.slots[0].IsEmpty());
            Assert.IsTrue(bench.PlayerData.inventory.slots[0].IsEmpty());
        }

        [TestCase("lootPanel")] [TestCase("mapChestPanel")] [TestCase("warehousePanel")]
        public void DoubleClick_MovesBothDirectionsAndPreservesWear(string panelField)
        {
            var panel = new GameObject(panelField); panel.transform.SetParent(root.transform); Set(panelField, panel);
            warehouse.Open();
            var source = panelField == "warehousePanel" ? TestContainer.Warehouse : TestContainer.Loot;
            var external = source == TestContainer.Warehouse ? bench.WarehouseData : loot.loot;
            var item = bench.PlayerData.inventory.slots[0]; item.itemId = 10001; item.amount = 1; item.durabilityDamage = 23; item.remainingRounds = 7;
            DoubleClick(Slot(TestContainer.Bag));
            Assert.IsTrue(item.IsEmpty()); Assert.AreEqual(23, external.slots[0].durabilityDamage);
            DoubleClick(Slot(source));
            Assert.IsTrue(external.slots[0].IsEmpty()); Assert.AreEqual(23, item.durabilityDamage); Assert.AreEqual(7, item.remainingRounds);
        }

        [Test] public void Refresh_RecoversExistingBagCurrencyOnlyOnce()
        {
            var slot = bench.PlayerData.inventory.slots[0]; slot.itemId = 20001; slot.amount = 11;
            bench.Refresh(); bench.Refresh();
            Assert.AreEqual(11, bench.PlayerData.currency); Assert.IsTrue(slot.IsEmpty());
        }
    }

    public class ContainerDropSettingsTests
    {
        private ItemCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            catalog = new ItemCatalog(new[]
            {
                new ItemData { itemId = 21001, itemType = ItemType.Consumable, displayName = "회복약", stackable = true, maxStack = 5 },
                new ItemData { itemId = 22001, itemType = ItemType.Consumable, displayName = "물", stackable = true, maxStack = 5 },
                new ItemData { itemId = 10001, itemType = ItemType.Weapon, displayName = "기관권총" }
            });
        }

        private static ContainerDropSettings Make(LootContainerSize size, params ContainerDropSettings.DropEntry[] entries)
        {
            ContainerDropSettings settings = ScriptableObject.CreateInstance<ContainerDropSettings>();
            SetPrivate(settings, "containerSize", size);
            SetPrivate(settings, "entries", entries);
            return settings;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            target.GetType()
                .GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static ContainerDropSettings.DropEntry Entry(int itemId, float chance, int min, int max)
        {
            return new ContainerDropSettings.DropEntry
            {
                itemId = itemId,
                chancePercent = chance,
                minAmount = min,
                maxAmount = max
            };
        }

        private sealed class AlwaysHit : IRandomSource
        {
            public double NextUnit() => 0.0;
            public int NextInclusive(int minimum, int maximum) => minimum;
        }

        private sealed class NeverHit : IRandomSource
        {
            public double NextUnit() => 0.999999;
            public int NextInclusive(int minimum, int maximum) => minimum;
        }

        [Test]
        public void Roll_OnlyProducesAllowedItems()
        {
            ContainerDropSettings settings = Make(LootContainerSize.Box2x4, Entry(21001, 100f, 1, 1));
            LootContainerData loot = ContainerLootRoller.Roll(settings, catalog, new AlwaysHit());

            foreach (GridSlotData slot in loot.loot.slots)
            {
                Assert.IsTrue(slot.IsEmpty() || slot.itemId == 21001);
            }
        }

        [Test]
        public void Roll_EmptyListGivesEmptyContainer()
        {
            ContainerDropSettings settings = Make(LootContainerSize.Box2x4);
            LootContainerData loot = ContainerLootRoller.Roll(settings, catalog, new AlwaysHit());

            Assert.IsTrue(loot.IsEmpty());
            Assert.AreEqual(8, loot.SlotCount);
        }

        [Test]
        public void Roll_NullSettingsGivesEmptyContainer()
        {
            LootContainerData loot = ContainerLootRoller.Roll(null, catalog, new AlwaysHit());

            Assert.IsTrue(loot.IsEmpty());
        }

        [Test]
        public void Roll_UnknownItemIdIsSkipped()
        {
            ContainerDropSettings settings = Make(LootContainerSize.Box2x4, Entry(99999, 100f, 1, 1));
            LootContainerData loot = ContainerLootRoller.Roll(settings, catalog, new AlwaysHit());

            Assert.IsTrue(loot.IsEmpty());
        }

        [Test]
        public void Roll_FailedChanceLeavesContainerEmpty()
        {
            ContainerDropSettings settings = Make(LootContainerSize.Box2x4, Entry(21001, 50f, 1, 1));
            LootContainerData loot = ContainerLootRoller.Roll(settings, catalog, new NeverHit());

            Assert.IsTrue(loot.IsEmpty());
        }

        // 용량을 넘는 항목은 기존 규칙대로 버린다.
        [Test]
        public void Roll_DiscardsBeyondCapacity()
        {
            List<ContainerDropSettings.DropEntry> entries = new List<ContainerDropSettings.DropEntry>();
            for (int index = 0; index < 12; index++)
            {
                entries.Add(Entry(21001, 100f, 1, 1));
            }

            ContainerDropSettings settings = Make(LootContainerSize.Box2x4, entries.ToArray());
            LootContainerData loot = ContainerLootRoller.Roll(settings, catalog, new AlwaysHit());

            int filled = 0;
            foreach (GridSlotData slot in loot.loot.slots)
            {
                if (!slot.IsEmpty())
                {
                    filled++;
                }
            }

            Assert.AreEqual(8, loot.SlotCount);
            Assert.AreEqual(8, filled);
        }

        [Test]
        public void Validate_ReportsBadIdAmountAndChance()
        {
            ContainerDropSettings settings = Make(
                LootContainerSize.Box2x4,
                Entry(99999, 50f, 1, 1),
                Entry(21001, 150f, 1, 1),
                Entry(21001, 50f, 3, 2),
                Entry(21001, 50f, 0, 1));

            List<string> problems = settings.Validate(catalog);

            Assert.AreEqual(4, problems.Count);
        }

        [Test]
        public void Validate_CleanSettingsHaveNoProblems()
        {
            ContainerDropSettings settings = Make(LootContainerSize.Box3x3, Entry(21001, 40f, 1, 3), Entry(22001, 10f, 1, 1));

            Assert.AreEqual(0, settings.Validate(catalog).Count);
        }
    }

    // 상자는 최초 한 번만 추첨하고 재개방으로 획득분이 복구되지 않는다. 기획서 8.1.1.
    public class InventoryWorldContainerTests
    {
        private ItemCatalog catalog;
        private GameObject host;
        private InventoryWorldContainer chest;

        [SetUp]
        public void SetUp()
        {
            catalog = new ItemCatalog(new[]
            {
                new ItemData { itemId = 21001, itemType = ItemType.Consumable, displayName = "회복약", stackable = true, maxStack = 5 }
            });

            host = new GameObject("MapChestTest");
            chest = host.AddComponent<InventoryWorldContainer>();
            chest.kind = InventoryWorldKind.MapChest;
            chest.dropSettings = CreateSettings();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        private static ContainerDropSettings CreateSettings()
        {
            ContainerDropSettings settings = ScriptableObject.CreateInstance<ContainerDropSettings>();
            SetPrivate(settings, "containerSize", LootContainerSize.Box2x4);
            SetPrivate(settings, "entries", new[]
            {
                new ContainerDropSettings.DropEntry { itemId = 21001, chancePercent = 100f, minAmount = 2, maxAmount = 2 }
            });
            return settings;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            target.GetType()
                .GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        [Test]
        public void GetContents_RollsOnceOnly()
        {
            LootContainerData contents = chest.GetContents(catalog);
            Assert.AreEqual(2, contents.loot.slots[0].amount);

            // 획득 후 재개방해도 다시 굴리지 않고 복구하지 않는다.
            contents.loot.slots[0].Clear();

            Assert.IsTrue(chest.GetContents(catalog).loot.slots[0].IsEmpty());
        }

        [Test]
        public void Contents_AreIndependentPerObject()
        {
            GameObject other = new GameObject("MapChestTest2");
            InventoryWorldContainer second = other.AddComponent<InventoryWorldContainer>();
            second.kind = InventoryWorldKind.MapChest;
            second.dropSettings = CreateSettings();

            chest.GetContents(catalog).loot.slots[0].Clear();

            Assert.IsTrue(chest.GetContents(catalog).loot.slots[0].IsEmpty());
            Assert.AreEqual(2, second.GetContents(catalog).loot.slots[0].amount);

            Object.DestroyImmediate(other);
        }
    }
}
