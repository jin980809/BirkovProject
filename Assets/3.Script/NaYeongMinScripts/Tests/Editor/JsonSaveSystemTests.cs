using System;
using System.IO;
using Birdkov.NaYeongMin.SaveSystem;
using NUnit.Framework;

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
}
