namespace Birdkov.NaYeongMin.InventorySystem
{
    public enum MerchantKind { All, Weapons, General }

    public sealed class ShopService
    {
        private readonly IItemCatalog catalog;
        private readonly InventoryService inventory;
        private readonly MerchantKind merchantKind;
        public ShopService(IItemCatalog catalog, MerchantKind merchantKind = MerchantKind.All)
        { this.catalog = catalog; this.merchantKind = merchantKind; inventory = new InventoryService(catalog); }

        public static bool Accepts(ItemData item, MerchantKind merchantKind)
        {
            if (!IsTradable(item)) return false;
            bool equipment = item.itemType == ItemType.Weapon || item.itemType == ItemType.Ammo || item.itemType == ItemType.Equipment;
            return merchantKind == MerchantKind.All || (merchantKind == MerchantKind.Weapons ? equipment : merchantKind == MerchantKind.General && !equipment);
        }

        public static bool IsTradable(ItemData item)
        {
            return item != null && item.price > 0 && item.itemType != ItemType.Currency &&
                (item.itemId < 27001 || item.itemId > 27004);
        }

        // 거래 1회 수량. 탄약은 발 단위라 CSV magazineSize(20발) 묶음으로 사고판다. 나머지는 1개.
        public static int TradeAmount(ItemData item) =>
            item != null && item.itemType == ItemType.Ammo && item.magazineSize > 0 ? item.magazineSize : 1;

        public bool Buy(PlayerInventoryData player, int itemId)
        {
            if (player == null || !catalog.TryGetItem(itemId, out ItemData item) ||
                !Accepts(item, merchantKind) || player.currency < item.price) return false;
            // 칸이 모자라 일부만 들어가면 통째로 되돌린다.
            var before = new System.Collections.Generic.List<GridSlotData>();
            foreach (GridSlotData s in player.inventory.slots)
                before.Add(new GridSlotData { itemId = s.itemId, amount = s.amount, remainingRounds = s.remainingRounds, durabilityDamage = s.durabilityDamage });
            if (inventory.AddItem(player.inventory, itemId, TradeAmount(item)).Result != InventoryResult.Success)
            {
                for (int i = 0; i < before.Count; i++)
                {
                    GridSlotData s = player.inventory.slots[i];
                    s.itemId = before[i].itemId; s.amount = before[i].amount;
                    s.remainingRounds = before[i].remainingRounds; s.durabilityDamage = before[i].durabilityDamage;
                }
                return false;
            }
            player.currency -= item.price;
            return true;
        }

        public bool Sell(PlayerInventoryData player, int index)
        {
            if (player?.inventory?.slots == null || index < 0 || index >= player.inventory.slots.Count) return false;
            GridSlotData slot = player.inventory.slots[index];
            if (slot.IsEmpty() || !catalog.TryGetItem(slot.itemId, out ItemData item) || !Accepts(item, merchantKind) ||
                player.currency > int.MaxValue - item.price || slot.amount < TradeAmount(item)) return false;
            if (inventory.RemoveItem(player.inventory, index, TradeAmount(item)).Result != InventoryResult.Success) return false;
            player.currency += item.price;
            return true;
        }
    }
}
