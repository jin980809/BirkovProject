using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Birdkov.NaYeongMin.InventorySystem
{
    public static class ItemCsvLoader
    {
        public static List<ItemData> Parse(string csvText)
        {
            if (string.IsNullOrWhiteSpace(csvText)) throw new ArgumentException("CSV text is empty.", nameof(csvText));

            List<List<string>> rows = ParseRows(csvText);
            if (rows.Count < 2) return new List<ItemData>();

            Dictionary<string, int> headers = CreateHeaderMap(rows[0]);
            RequireHeader(headers, "itemId");
            RequireHeader(headers, "itemType");

            List<ItemData> items = new List<ItemData>();
            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                List<string> row = rows[rowIndex];
                if (IsEmpty(row)) continue;

                if (!int.TryParse(Get(row, headers, "itemId"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int itemId) || itemId < 0)
                    throw new FormatException($"Invalid itemId at CSV row {rowIndex + 1}.");
                if (!Enum.TryParse(Get(row, headers, "itemType"), true, out ItemType itemType))
                    throw new FormatException($"Invalid itemType at CSV row {rowIndex + 1}.");

                items.Add(new ItemData
                {
                    itemId = itemId,
                    itemType = itemType,
                    equipmentSlotType = ParseEnum(Get(row, headers, "equipmentSlotType"), EquipmentSlotType.None),
                    rarity = ParseEnum(Get(row, headers, "rarity"), ItemRarity.Common),
                    displayName = GetAny(row, headers, "displayName", "itemName"),
                    description = Get(row, headers, "description"),
                    iconKey = GetAny(row, headers, "iconKey", "iconPath"),
                    price = ParseInt(Get(row, headers, "price")),
                    weight = ParseFloat(Get(row, headers, "weight")),
                    stackable = ParseBool(Get(row, headers, "stackable")),
                    maxStack = Math.Max(1, ParseInt(Get(row, headers, "maxStack"), 1)),
                    gridWidth = Math.Max(1, ParseInt(GetAny(row, headers, "gridWidth", "width"), 1)),
                    gridHeight = Math.Max(1, ParseInt(GetAny(row, headers, "gridHeight", "height"), 1)),
                    attackDamage = ParseFloat(GetAny(row, headers, "attackDamage", "attackPower")),
                    reloadSpeed = ParseFloat(Get(row, headers, "reloadSpeed")),
                    fireRate = ParseFloat(Get(row, headers, "fireRate")),
                    magazineSize = ParseInt(Get(row, headers, "magazineSize")),
                    projectileSpeed = ParseFloat(GetAny(row, headers, "projectileSpeed", "bulletSpeed")),
                    automatic = ParseBool(GetAny(row, headers, "automatic", "isAutomatic")),
                    pelletCount = Math.Max(1, ParseInt(Get(row, headers, "pelletCount"), 1)),
                    maxSpread = ParseFloat(Get(row, headers, "maxSpread")),
                    range = ParseFloat(Get(row, headers, "range")),
                    maxDurability = ParseInt(Get(row, headers, "maxDurability")),
                    durabilityCostPerHit = ParseInt(Get(row, headers, "durabilityCostPerHit")),
                    repairAmountPerCurrency = ParseInt(Get(row, headers, "repairAmountPerCurrency")),
                    defense = ParseFloat(GetAny(row, headers, "defense", "defensePower")),
                    healthRecovery = ParseFloat(Get(row, headers, "healthRecovery")),
                    hungerRecovery = ParseFloat(Get(row, headers, "hungerRecovery")),
                    waterRecovery = ParseFloat(Get(row, headers, "waterRecovery"))
                });
            }

            return items;
        }

        private static Dictionary<string, int> CreateHeaderMap(List<string> headerRow)
        {
            Dictionary<string, int> headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < headerRow.Count; index++)
            {
                string header = headerRow[index].Trim().TrimStart('\uFEFF');
                if (!string.IsNullOrEmpty(header)) headers[header] = index;
            }
            return headers;
        }

        private static void RequireHeader(Dictionary<string, int> headers, string header)
        {
            if (!headers.ContainsKey(header)) throw new FormatException($"Required CSV header is missing: {header}");
        }

        private static string Get(List<string> row, Dictionary<string, int> headers, string header)
        {
            return headers.TryGetValue(header, out int index) && index < row.Count ? row[index].Trim() : string.Empty;
        }

        private static string GetAny(List<string> row, Dictionary<string, int> headers, params string[] names)
        {
            foreach (string name in names)
            {
                string value = Get(row, headers, name);
                if (!string.IsNullOrEmpty(value)) return value;
            }
            return string.Empty;
        }

        private static int ParseInt(string value, int fallback = 0)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : fallback;
        }

        private static float ParseFloat(string value)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : 0f;
        }

        private static bool ParseBool(string value) => bool.TryParse(value, out bool result) && result;

        private static T ParseEnum<T>(string value, T fallback) where T : struct
        {
            return Enum.TryParse(value, true, out T result) ? result : fallback;
        }

        private static bool IsEmpty(List<string> row)
        {
            foreach (string value in row) if (!string.IsNullOrWhiteSpace(value)) return false;
            return true;
        }

        private static List<List<string>> ParseRows(string csvText)
        {
            List<List<string>> rows = new List<List<string>>();
            List<string> row = new List<string>();
            StringBuilder field = new StringBuilder();
            bool quoted = false;

            for (int index = 0; index < csvText.Length; index++)
            {
                char character = csvText[index];
                if (quoted)
                {
                    if (character == '"' && index + 1 < csvText.Length && csvText[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else if (character == '"') quoted = false;
                    else field.Append(character);
                    continue;
                }

                if (character == '"') quoted = true;
                else if (character == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if (character == '\n')
                {
                    row.Add(field.ToString().TrimEnd('\r'));
                    rows.Add(row);
                    row = new List<string>();
                    field.Clear();
                }
                else field.Append(character);
            }

            if (quoted) throw new FormatException("CSV contains an unterminated quoted field.");
            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString().TrimEnd('\r'));
                rows.Add(row);
            }
            return rows;
        }
    }
}
