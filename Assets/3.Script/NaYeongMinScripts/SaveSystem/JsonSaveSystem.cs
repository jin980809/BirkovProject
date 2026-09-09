using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Birdkov.NaYeongMin.SaveSystem
{
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
