namespace Birdkov.NaYeongMin.InventorySystem
{
    public sealed class ShopService
    {
        private readonly IItemCatalog catalog;
        private readonly InventoryService inventory;
        public ShopService(IItemCatalog catalog) { this.catalog = catalog; inventory = new InventoryService(catalog); }

        public static bool IsTradable(ItemData item)
        {
            return item != null && item.price > 0 && item.itemType != ItemType.Currency &&
                (item.itemId < 27001 || item.itemId > 27004);
        }

        public bool Buy(PlayerInventoryData player, int itemId)
        {
            if (player == null || !catalog.TryGetItem(itemId, out ItemData item) ||
                !IsTradable(item) || player.currency < item.price) return false;
            if (inventory.AddItem(player.inventory, itemId, 1).Result != InventoryResult.Success) return false;
            player.currency -= item.price;
            return true;
        }

        public bool Sell(PlayerInventoryData player, int index)
        {
            if (player?.inventory?.slots == null || index < 0 || index >= player.inventory.slots.Count) return false;
            GridSlotData slot = player.inventory.slots[index];
            if (slot.IsEmpty() || !catalog.TryGetItem(slot.itemId, out ItemData item) || !IsTradable(item) ||
                player.currency > int.MaxValue - item.price) return false;
            if (inventory.RemoveItem(player.inventory, index, 1).Result != InventoryResult.Success) return false;
            player.currency += item.price;
            return true;
        }
    }
}
