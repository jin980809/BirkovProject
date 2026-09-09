using System;
using UnityEngine;

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
