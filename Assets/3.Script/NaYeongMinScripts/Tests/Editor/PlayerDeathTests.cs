using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.InventoryTest;
using NUnit.Framework;
using UnityEngine;

namespace Birdkov.NaYeongMin.Tests
{
    public class PlayerDeathTests
    {
        private GameObject instance;
        private PlayerDeathContainer corpse;
        [SetUp] public void SetUp() { instance = new GameObject("CorpseTest"); corpse = instance.AddComponent<PlayerDeathContainer>(); }
        [TearDown] public void TearDown() { Object.DestroyImmediate(instance); }
        [Test] public void Capture_LeavesCurrencyWithPlayer()
        {
            var player = new PlayerInventoryData { currency = 50 };
            foreach (var slot in player.inventory.slots) { slot.itemId = 21001; slot.amount = 5; }
            foreach (var slot in player.equipmentSlots.slots) { slot.itemId = 10001; slot.amount = 1; slot.remainingRounds = 7; slot.durabilityDamage = 12; }
            Assert.IsTrue(corpse.Capture(player));
            Assert.AreEqual(5, corpse.Contents.loot.width); Assert.AreEqual(6, corpse.Contents.loot.height);
            Assert.AreEqual(30, corpse.Contents.SlotCount);
            new PlayerInventoryService(new ItemCatalog(new ItemData[0])).ClearOnDeath(player);
            Assert.AreEqual(7, corpse.Contents.loot.slots[25].remainingRounds);
            Assert.AreEqual(12, corpse.Contents.loot.slots[25].durabilityDamage);
            Assert.AreEqual(50, player.currency);
            Assert.IsTrue(corpse.Contents.loot.slots[29].IsEmpty());
            Assert.IsFalse(corpse.Contents.loot.slots.Exists(slot => slot.itemId == 20001));
        }
        [Test] public void Restore_ExcludesStraw()
        {
            Assert.IsTrue(corpse.SetItems(new[] { new GridSlotData { itemId = 20001, amount = 50 } }));
            Assert.IsTrue(corpse.Contents.IsEmpty());
        }
        [Test] public void Oversize_IsRejectedWithoutReplacingContents()
        {
            Assert.IsTrue(corpse.SetItems(new[] { new GridSlotData { itemId = 10001, amount = 1 } }));
            var slots = new GridSlotData[31]; for (int i = 0; i < slots.Length; i++) slots[i] = new GridSlotData { itemId = 21001, amount = 1 };
            Assert.IsFalse(corpse.SetItems(slots)); Assert.AreEqual(10001, corpse.Contents.loot.slots[0].itemId);
        }
        [Test] public void Export_IsIndependentAndEmptyContainerDoesNotRefill()
        {
            corpse.SetItems(new[] { new GridSlotData { itemId = 27001, amount = 5 } });
            var copy = corpse.CopyItems(); copy[0].Clear(); Assert.AreEqual(5, corpse.Contents.loot.slots[0].amount);
            instance.GetComponent<InventoryWorldContainer>().kind = InventoryWorldKind.PlayerDeath;
            corpse.Contents.Clear(); Assert.IsTrue(instance.GetComponent<InventoryWorldContainer>().GetContents(null).IsEmpty());
        }
    }
}
