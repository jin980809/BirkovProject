using System;
using System.Reflection;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.SaveSystem;
using Birdkov.NaYeongMin.Rng;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Birdkov.NaYeongMin.Tests
{
    // 기본 어셈블리의 팀 API를 테스트에서만 리플렉션으로 접근한다.
    public class IntegrationTests
    {
        private GameObject player, ui;
        private Component vitals, input, bridge, bench;
        private const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static object Get(object target, string name) => target.GetType().GetField(name, flags).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, flags).SetValue(target, value);
        private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, flags).Invoke(target, args);

        [SetUp]
        public void SetUp()
        {
            player = new GameObject("IntegrationTestPlayer");
            ui = new GameObject("IntegrationTestUi");
            vitals = player.AddComponent(Type.GetType("PlayerVitals, Assembly-CSharp", true));
            input = player.AddComponent(Type.GetType("PlayerInputHandler, Assembly-CSharp", true));
            bridge = player.AddComponent(Type.GetType("Birdkov.NaYeongMin.Integration.PlayerInventoryBridge, Assembly-CSharp", true));
            bench = ui.AddComponent(Type.GetType("Birdkov.NaYeongMin.InventoryTest.InventoryTestBench, Birdkov.NaYeongMin.Test", true));
            Set(bench, "itemCsv", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/DataTeble/NaYeongMinCsvData/ItemData.csv"));
            Call(bench, "Initialize");
            Call(bench, "ResetAll");
            Call(vitals, "Awake");
            Set(bridge, "playerVitals", vitals);
            Set(bridge, "playerInput", input);
            Set(bridge, "inventoryBench", bench);
            Call(bridge, "OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            if (bridge != null) Call(bridge, "OnDisable");
            UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(ui);
        }

        // 회복약 21001 의 CSV 값에서 기대치를 계산한다. 수치가 바뀌어도 환산 계약만 검증한다.
        private float ExpectedHealthAfterPotion(float damage)
        {
            ItemCatalog catalog = (ItemCatalog)Get(bench, "catalog");
            catalog.TryGetItem(21001, out ItemData potion);
            float maxHealth = (float)Get(vitals, "maxHealth");
            return Mathf.Min(maxHealth, maxHealth - damage + maxHealth * PlayerInventoryService.RecoveryPercent(potion.healthRecovery) / 100f);
        }

        [Test]
        public void Recovery_UsesCsvPercent_AndConsumesOnce()
        {
            Call(vitals, "TakeDamage", 40f);
            PlayerSaveData data = (PlayerSaveData)Get(bench, "data");
            int before = data.inventoryData.inventory.slots[0].amount;
            float expected = ExpectedHealthAfterPotion(40f);
            Call(bench, "UseBagItem", 0);
            Assert.AreEqual(expected, Get(vitals, "health"));
            Assert.AreEqual(before - 1, data.inventoryData.inventory.slots[0].amount);
        }

        [Test]
        public void Recovery_FullOrDead_DoesNotConsume()
        {
            PlayerSaveData data = (PlayerSaveData)Get(bench, "data");
            int before = data.inventoryData.inventory.slots[0].amount;
            Call(bench, "UseBagItem", 0);
            Assert.AreEqual(before, data.inventoryData.inventory.slots[0].amount);
            Set(vitals, "health", 0f);
            Call(bench, "UseBagItem", 0);
            Assert.AreEqual(before, data.inventoryData.inventory.slots[0].amount);
        }

        [Test]
        public void DeathEvent_ClearsCarriedItems_PreservesWarehouseAndCurrency()
        {
            PlayerSaveData data = (PlayerSaveData)Get(bench, "data");
            int stored = data.warehouseData.slots[0].amount;
            int currency = data.inventoryData.currency;
            Call(vitals, "TakeDamage", 100f);
            Assert.IsTrue(data.inventoryData.inventory.slots.TrueForAll(slot => slot.IsEmpty()));
            Assert.AreEqual(stored, data.warehouseData.slots[0].amount);
            Assert.AreEqual(currency, data.inventoryData.currency);
        }

        [Test]
        public void InputEvents_UseQuickSlotOnce_AndSelectWeapon()
        {
            PlayerSaveData data = (PlayerSaveData)Get(bench, "data");
            ((PlayerInventoryService)Get(bench, "playerService")).AssignItemQuickSlot(data.inventoryData, 0, 0);
            Call(vitals, "TakeDamage", 40f);
            float expected = ExpectedHealthAfterPotion(40f);
            ((Action<int>)Get(input, "QuickSlotUsed"))(0);
            ((Action<int>)Get(input, "WeaponSelected"))(1);
            Assert.AreEqual(expected, Get(vitals, "health"));
            Assert.AreEqual(3, data.inventoryData.inventory.slots[0].amount);
            Assert.AreEqual(1, Get(bench, "selectedWeapon"));
            Assert.AreEqual(false, Get(bench, "useStandaloneKeyboard"));
        }

        [Test]
        public void Disable_UnsubscribesInputAndDeath()
        {
            Call(bridge, "OnDisable");
            Assert.IsNull(Get(input, "QuickSlotUsed"));
            Assert.IsNull(Get(input, "InventoryToggled"));
            Assert.IsNull(Get(input, "InteractPressed"));
            Assert.IsNull(Get(vitals, "Died"));
            Assert.AreEqual(true, Get(bench, "useStandaloneKeyboard"));
        }

        [Test]
        public void EnemyReceiver_DropsOncePerLife()
        {
            GameObject runtimeObject = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/2.Model/Prefabs/NaYeongMin/LootRuntime.prefab"));
            try
            {
                LootRuntime runtime = runtimeObject.GetComponent<LootRuntime>();
                runtime.Initialize();
                Assert.IsTrue(runtime.IsReady);
                EnemyLootReceiver receiver = player.AddComponent<EnemyLootReceiver>();
                Set(receiver, "lootRuntime", runtime);
                Assert.IsTrue(receiver.NotifyDeath());
                Assert.IsFalse(receiver.NotifyDeath());
                Assert.IsTrue(receiver.DeathHandled);
                receiver.ResetForSpawn();
                Assert.IsTrue(receiver.NotifyDeath());
            }
            finally { UnityEngine.Object.DestroyImmediate(runtimeObject); }
        }
    }
}
