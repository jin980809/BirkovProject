using Birdkov.NaYeongMin.InventorySystem;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using UnityEngine;

// 세이브 데이터 구조와 검증, 임시 파일을 거치는 안전 저장과 백업 복구.
namespace Birdkov.NaYeongMin.SaveSystem
{
        // ---- PlayerSaveData ----
    [Serializable]
    public class PlayerSaveData
    {
        public int saveVersion = SaveDataValidator.CurrentSaveVersion;

        public PlayerInventoryData inventoryData = new PlayerInventoryData();

        // 허브 창고. WarehouseService 에 그대로 넘겨 사용한다.
        public GridContainerData warehouseData = new GridContainerData(
            InventorySettings.WarehouseWidth,
            InventorySettings.WarehouseHeight);
    }

        // ---- SaveDataValidator ----
    public static class SaveDataValidator
    {
        // 2: 퀵슬롯이 컨테이너에서 가방 인덱스 매핑으로 바뀜. 3: 장비 슬롯. 4: 지푸라기 분리. 5: 탄약 잔탄.
        public const int CurrentSaveVersion = 5;

        public static bool IsValid(PlayerSaveData data)
        {
            return data != null &&
                   data.saveVersion == CurrentSaveVersion &&
                   IsContainerValid(
                       data.inventoryData?.inventory,
                       InventorySettings.InventoryWidth,
                       InventorySettings.InventoryHeight) &&
                   IsContainerValid(
                       data.inventoryData?.equipmentSlots,
                       InventorySettings.EquipmentSlotCount,
                       1) &&
                   AreItemQuickSlotsValid(data.inventoryData) &&
                   data.inventoryData.currency >= 0 &&
                   IsContainerValid(data.warehouseData);
        }

        private static bool AreItemQuickSlotsValid(PlayerInventoryData inventoryData)
        {
            int[] indices = inventoryData?.itemQuickSlotIndices;
            if (indices == null || indices.Length != InventorySettings.ItemQuickSlotCount)
            {
                return false;
            }

            foreach (int index in indices)
            {
                if (index != InventorySettings.UnassignedQuickSlot &&
                    (index < 0 || index >= InventorySettings.InventorySlotCount))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsContainerValid(GridContainerData container, int expectedWidth, int expectedHeight)
        {
            return container != null &&
                   container.width == expectedWidth &&
                   container.height == expectedHeight &&
                   IsSlotListValid(container.slots, expectedWidth * expectedHeight);
        }

        private static bool IsContainerValid(GridContainerData container)
        {
            if (container == null || container.width < 0 || container.height < 0)
            {
                return false;
            }

            return IsSlotListValid(container.slots, container.width * container.height);
        }

        private static bool IsSlotListValid(List<GridSlotData> slots, int expectedCount)
        {
            if (slots == null || slots.Count != expectedCount)
            {
                return false;
            }

            foreach (GridSlotData slot in slots)
            {
                if (slot == null)
                {
                    return false;
                }

                bool empty = slot.itemId < 0 && slot.amount == 0;
                bool occupied = slot.itemId >= 0 && slot.amount > 0;
                if (!empty && !occupied)
                {
                    return false;
                }
            }

            return true;
        }
    }

        // ---- JsonSaveSystem ----
    public sealed class JsonSaveSystem
    {
        private readonly string saveDirectory;

        public JsonSaveSystem(string saveDirectory = null)
        {
            this.saveDirectory = string.IsNullOrWhiteSpace(saveDirectory)
                ? Path.Combine(Application.persistentDataPath, "Saves")
                : saveDirectory;
        }

        public void Save(string slotName, PlayerSaveData data)
        {
            ValidateSlotName(slotName);
            if (!SaveDataValidator.IsValid(data))
            {
                throw new ArgumentException("Save data is invalid.", nameof(data));
            }

            Directory.CreateDirectory(saveDirectory);
            GetPaths(slotName, out string mainPath, out string backupPath, out string temporaryPath);
            string json = JsonUtility.ToJson(data, true);

            try
            {
                WriteDurable(temporaryPath, json);

                if (File.Exists(mainPath))
                {
                    File.Replace(temporaryPath, mainPath, backupPath, true);
                }
                else
                {
                    File.Move(temporaryPath, mainPath);
                    File.Copy(mainPath, backupPath, true);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        public bool TryLoad(string slotName, out PlayerSaveData data, out bool recoveredFromBackup)
        {
            ValidateSlotName(slotName);
            GetPaths(slotName, out string mainPath, out string backupPath, out string temporaryPath);
            recoveredFromBackup = false;

            if (TryRead(mainPath, out data))
            {
                return true;
            }

            if (!TryRead(backupPath, out data))
            {
                data = null;
                return false;
            }

            Directory.CreateDirectory(saveDirectory);
            try
            {
                File.Copy(backupPath, temporaryPath, true);
                if (File.Exists(mainPath))
                {
                    File.Replace(temporaryPath, mainPath, null, true);
                }
                else
                {
                    File.Move(temporaryPath, mainPath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

            recoveredFromBackup = true;
            return true;
        }

        private static void WriteDurable(string path, string content)
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes(content);
            using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }

        private static bool TryRead(string path, out PlayerSaveData data)
        {
            data = null;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                data = JsonUtility.FromJson<PlayerSaveData>(json);
                return SaveDataValidator.IsValid(data);
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is ArgumentException)
            {
                data = null;
                return false;
            }
        }

        private void GetPaths(string slotName, out string mainPath, out string backupPath, out string temporaryPath)
        {
            mainPath = Path.Combine(saveDirectory, $"{slotName}.json");
            backupPath = Path.Combine(saveDirectory, $"{slotName}.backup.json");
            temporaryPath = Path.Combine(saveDirectory, $"{slotName}.tmp");
        }

        private static void ValidateSlotName(string slotName)
        {
            if (string.IsNullOrWhiteSpace(slotName))
            {
                throw new ArgumentException("Save slot name is empty.", nameof(slotName));
            }

            foreach (char character in slotName)
            {
                if (!char.IsLetterOrDigit(character) && character != '-' && character != '_')
                {
                    throw new ArgumentException("Save slot name contains an invalid character.", nameof(slotName));
                }
            }
        }
    }
}
