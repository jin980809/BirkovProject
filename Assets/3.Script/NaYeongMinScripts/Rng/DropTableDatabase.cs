using System;
using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;

// DropTable.csv 를 로드하고 아이템 참조 무결성을 검사하는 컴포넌트.
namespace Birdkov.NaYeongMin.Rng
{
    // 호출 측에서 ItemDatabase.Load() 후 Load(itemDatabase.Catalog)를 호출한다.
    // Awake 순서에 의존하지 않으며, 검증 실패 시 이전 테이블도 사용하지 않는다.
    public sealed class DropTableDatabase : MonoBehaviour
    {
        [SerializeField] private TextAsset dropTableCsv;

        [Serializable]
        public sealed class ChanceOverride
        {
            public DropSourceType sourceType = DropSourceType.RangedEnemy;
            [Min(0)] public int itemId = 11003;
            [Range(0f, 100f)] public float chancePercent = 5f;
        }

        [Header("씬별 드롭 확률 조정 (CSV 원본 유지)")]
        [Tooltip("비우면 CSV 그대로 사용. 기존 소스/아이템의 확률만 변경하며 수량은 유지합니다. 편집 모드에서 변경 후 Play하면 적용됩니다.")]
        [SerializeField] private List<ChanceOverride> chanceOverrides = new List<ChanceOverride>();

        public IReadOnlyList<DropTableEntry> Entries { get; private set; }

        public void Load(IItemCatalog itemCatalog)
        {
            Entries = null;
            if (itemCatalog == null)
            {
                throw new ArgumentNullException(nameof(itemCatalog));
            }

            if (dropTableCsv == null)
            {
                throw new InvalidOperationException("DropTable CSV is not assigned.");
            }

            List<DropTableEntry> entries = DropTableCsvLoader.Parse(dropTableCsv.text);
            List<int> missing = DropTableCsvLoader.FindMissingItemIds(entries, itemCatalog);
            if (missing.Count > 0)
            {
                throw new InvalidOperationException("DropTable contains unknown item IDs: " + string.Join(", ", missing));
            }

            ApplyChanceOverrides(entries);
            Entries = entries.AsReadOnly();
        }

        private void ApplyChanceOverrides(List<DropTableEntry> entries)
        {
            if (chanceOverrides == null) return;
            HashSet<string> keys = new HashSet<string>();
            foreach (ChanceOverride setting in chanceOverrides)
            {
                if (setting == null || !Enum.IsDefined(typeof(DropSourceType), setting.sourceType) ||
                    setting.itemId < 0 || float.IsNaN(setting.chancePercent) ||
                    float.IsInfinity(setting.chancePercent) || setting.chancePercent < 0f || setting.chancePercent > 100f)
                    throw new InvalidOperationException("드롭 확률 조정값은 유효한 소스/아이템 ID와 0~100%가 필요합니다.");

                string key = setting.sourceType + "/" + setting.itemId;
                if (!keys.Add(key))
                    throw new InvalidOperationException("중복 드롭 확률 조정: " + key);

                DropTableEntry entry = entries.Find(value =>
                    value.sourceType == setting.sourceType && value.itemId == setting.itemId);
                if (entry == null)
                    throw new InvalidOperationException("CSV에 없는 드롭 확률 조정 대상: " + key);

                entry.finalDropChance = setting.chancePercent;
                entry.groupDropChance = setting.chancePercent;
            }
        }
    }
}
