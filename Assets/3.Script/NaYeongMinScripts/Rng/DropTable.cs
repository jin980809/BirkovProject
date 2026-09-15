using Birdkov.NaYeongMin.InventorySystem;
using System.Collections.Generic;
using System.Globalization;
using System;

// 드롭 테이블 데이터 타입, DropTable.csv 파서, 난수 소스.
namespace Birdkov.NaYeongMin.Rng
{
        // ---- DropSourceType ----
    public enum DropSourceType
    {
        // Box 는 8.1.1 의 일반 상자(종합). 카테고리 상자 5종은 개별 확률이 기획 미확정이라 CSV 행이 아직 없다.
        Box,
        WeaponBox,
        AmmoBox,
        ArmorBox,
        RecoveryBox,
        FoodBox,
        BasicEnemy,
        HeavyEnemy,
        RangedEnemy
    }

    // 상자 종류별 규격과 프리팹 명칭. 기획서 8.1.1.
    public static class DropSources
    {
        public static LootContainerSize SizeOf(DropSourceType sourceType)
        {
            switch (sourceType)
            {
                case DropSourceType.WeaponBox: return LootContainerSize.Box3x5;  // Box_Gun
                case DropSourceType.AmmoBox: return LootContainerSize.Box4x1;    // Box_Ammo
                case DropSourceType.ArmorBox: return LootContainerSize.Box3x3;   // Box_Arm
                default: return LootContainerSize.Box2x4;                        // Box_Med / Box_Food / Box_Norm / 적 시체
            }
        }
    }

        // ---- DropTableEntry ----
    [Serializable]
    public class DropTableEntry
    {
        public DropSourceType sourceType;
        public string presetName;
        public int groupIndex;
        public int itemId = -1;
        public float groupDropChance;
        public float itemSelectionChance;
        public float finalDropChance;
        public int minAmount = 1;
        public int maxAmount = 1;
    }

        // ---- RandomSource ----
    public interface IRandomSource
    {
        double NextUnit();
        int NextInclusive(int minimum, int maximum);
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random random;

        public SystemRandomSource()
        {
            random = new Random();
        }

        public SystemRandomSource(int seed)
        {
            random = new Random(seed);
        }

        public double NextUnit()
        {
            return random.NextDouble();
        }

        public int NextInclusive(int minimum, int maximum)
        {
            if (maximum < minimum)
            {
                (minimum, maximum) = (maximum, minimum);
            }

            return random.Next(minimum, maximum + 1);
        }
    }

        // ---- DropTableCsvLoader ----
    // 드롭 테이블 CSV 로더.
    public static class DropTableCsvLoader
    {
        public static List<DropTableEntry> Parse(string csvText)
        {
            if (string.IsNullOrWhiteSpace(csvText))
            {
                throw new ArgumentException("Drop table CSV text is empty.", nameof(csvText));
            }

            string[] lines = csvText.Replace("\r\n", "\n").Split('\n');
            if (lines.Length < 2)
            {
                return new List<DropTableEntry>();
            }

            Dictionary<string, int> headers = CreateHeaderMap(lines[0]);
            RequireHeader(headers, "sourceType");
            RequireHeader(headers, "itemId");
            RequireHeader(headers, "finalDropChance");

            List<DropTableEntry> entries = new List<DropTableEntry>();
            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] cells = line.Split(',');
                int rowNumber = lineIndex + 1;

                if (!Enum.TryParse(Get(cells, headers, "sourceType"), true, out DropSourceType sourceType))
                {
                    throw new FormatException($"Invalid sourceType at drop table row {rowNumber}.");
                }

                if (!int.TryParse(
                        Get(cells, headers, "itemId"),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int itemId) || itemId < 0)
                {
                    throw new FormatException($"Invalid itemId at drop table row {rowNumber}.");
                }

                float chance = ParseFloat(Get(cells, headers, "finalDropChance"));
                if (chance < 0f || chance > 100f)
                {
                    throw new FormatException($"finalDropChance must be 0~100 at drop table row {rowNumber}.");
                }

                int minimum = Math.Max(1, ParseInt(Get(cells, headers, "minAmount"), 1));
                int maximum = Math.Max(minimum, ParseInt(Get(cells, headers, "maxAmount"), minimum));

                entries.Add(new DropTableEntry
                {
                    sourceType = sourceType,
                    itemId = itemId,
                    groupDropChance = chance,
                    itemSelectionChance = 100f,
                    finalDropChance = chance,
                    minAmount = minimum,
                    maxAmount = maximum
                });
            }

            return entries;
        }

        // 드롭 테이블이 참조하는 itemId 가 아이템 카탈로그에 모두 있는지 검사한다.
        // 반환값은 카탈로그에 없는 itemId 목록이며 비어 있어야 정상이다.
        public static List<int> FindMissingItemIds(IEnumerable<DropTableEntry> entries, IItemCatalog itemCatalog)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (itemCatalog == null)
            {
                throw new ArgumentNullException(nameof(itemCatalog));
            }

            List<int> missing = new List<int>();
            foreach (DropTableEntry entry in entries)
            {
                if (entry == null || itemCatalog.TryGetItem(entry.itemId, out _) || missing.Contains(entry.itemId))
                {
                    continue;
                }

                missing.Add(entry.itemId);
            }

            return missing;
        }

        private static Dictionary<string, int> CreateHeaderMap(string headerLine)
        {
            Dictionary<string, int> headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            string[] cells = headerLine.Split(',');
            for (int index = 0; index < cells.Length; index++)
            {
                string header = cells[index].Trim().TrimStart('﻿');
                if (!string.IsNullOrEmpty(header))
                {
                    headers[header] = index;
                }
            }

            return headers;
        }

        private static void RequireHeader(Dictionary<string, int> headers, string header)
        {
            if (!headers.ContainsKey(header))
            {
                throw new FormatException($"Required drop table header is missing: {header}");
            }
        }

        private static string Get(string[] cells, Dictionary<string, int> headers, string header)
        {
            return headers.TryGetValue(header, out int index) && index < cells.Length
                ? cells[index].Trim()
                : string.Empty;
        }

        private static int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
                ? result
                : fallback;
        }

        private static float ParseFloat(string value)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)
                ? result
                : 0f;
        }
    }
}
