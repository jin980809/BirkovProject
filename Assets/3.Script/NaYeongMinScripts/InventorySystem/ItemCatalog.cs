using System;
using System.Collections.Generic;

namespace Birdkov.NaYeongMin.InventorySystem
{
    public interface IItemCatalog
    {
        bool TryGetItem(int itemId, out ItemData itemData);
    }

    public sealed class ItemCatalog : IItemCatalog
    {
        private readonly Dictionary<int, ItemData> items = new Dictionary<int, ItemData>();

        public int Count => items.Count;

        public ItemCatalog(IEnumerable<ItemData> itemData)
        {
            if (itemData == null)
            {
                throw new ArgumentNullException(nameof(itemData));
            }

            foreach (ItemData item in itemData)
            {
                if (item == null || item.itemId < 0)
                {
                    throw new ArgumentException("Item data contains an invalid item.", nameof(itemData));
                }

                if (!items.TryAdd(item.itemId, item))
                {
                    throw new ArgumentException($"Duplicate itemId: {item.itemId}", nameof(itemData));
                }
            }
        }

        public bool TryGetItem(int itemId, out ItemData itemData)
        {
            return items.TryGetValue(itemId, out itemData);
        }
    }
}
