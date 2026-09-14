using System;
using UnityEngine;

// ItemData.csv 를 인스펙터에서 연결해 카탈로그를 만드는 컴포넌트.
namespace Birdkov.NaYeongMin.InventorySystem
{
    public sealed class ItemDatabase : MonoBehaviour
    {
        [SerializeField] private TextAsset itemDataCsv;

        public ItemCatalog Catalog { get; private set; }

        public void Load()
        {
            if (itemDataCsv == null)
            {
                throw new InvalidOperationException("ItemData CSV is not assigned.");
            }

            Catalog = new ItemCatalog(ItemCsvLoader.Parse(itemDataCsv.text));
        }

        private void Awake()
        {
            Load();
        }
    }
}
