using System;
using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;

namespace Birdkov.NaYeongMin.Rng
{
    // 호출 측에서 ItemDatabase.Load() 후 Load(itemDatabase.Catalog)를 호출한다.
    // Awake 순서에 의존하지 않으며, 검증 실패 시 이전 테이블도 사용하지 않는다.
    public sealed class DropTableDatabase : MonoBehaviour
    {
        [SerializeField] private TextAsset dropTableCsv;

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

            Entries = entries.AsReadOnly();
        }
    }
}
