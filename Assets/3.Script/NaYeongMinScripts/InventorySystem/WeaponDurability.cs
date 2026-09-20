using System;

namespace Birdkov.NaYeongMin.InventorySystem
{
    // 기획서 5.2. 정본은 ItemData.csv 의 maxDurability / durabilityCostPerHit / repairAmountPerCurrency 다.
    // Catalog 가 연결되지 않은 환경(단위 테스트 등)에서는 기획서 5.2 표 값으로 동작한다.
    public static class WeaponDurability
    {
        public static IItemCatalog Catalog { get; set; }

        private static ItemData Find(int itemId)
        {
            ItemData item;
            return Catalog != null && Catalog.TryGetItem(itemId, out item) ? item : null;
        }

        public static int Maximum(int itemId)
        {
            ItemData item = Find(itemId);
            if (item != null && item.maxDurability > 0) return item.maxDurability;
            switch (itemId) { case 10001: return 100; case 10002: return 120;
                case 10003: return 180; case 10004: return 160; default: return 0; }
        }

        private static int ShotCost(int itemId)
        {
            ItemData item = Find(itemId);
            if (item != null && item.durabilityCostPerHit > 0) return item.durabilityCostPerHit;
            return itemId == 10001 ? 2 : itemId == 10004 ? 8 : 3;
        }

        private static int RepairPerCurrency(int itemId)
        {
            ItemData item = Find(itemId);
            if (item != null && item.repairAmountPerCurrency > 0) return item.repairAmountPerCurrency;
            return itemId == 10001 ? 100 : itemId == 10002 ? 60 : itemId == 10003 ? 50 : 40;
        }

        public static int Remaining(GridSlotData slot)
        {
            return slot == null || slot.IsEmpty() ? 0 : Math.Max(0, Maximum(slot.itemId) - Math.Max(0, slot.durabilityDamage));
        }

        // 파괴 시점의 장착 해제는 발사 이벤트가 끝난 뒤 브리지에서 처리한다.
        public static bool ApplyShot(GridSlotData slot)
        {
            if (slot == null || slot.IsEmpty() || Maximum(slot.itemId) == 0) return false;
            slot.durabilityDamage = Math.Min(Maximum(slot.itemId), Math.Max(0, slot.durabilityDamage) + ShotCost(slot.itemId));
            return Remaining(slot) == 0;
        }

        // 기획서 5.2: 1G 단위로만 수리하고 최대 내구도를 넘지 않는다.
        public static bool Repair(PlayerInventoryData player, GridSlotData slot)
        {
            if (player == null || player.currency < 1 || Remaining(slot) <= 0 || slot.durabilityDamage <= 0) return false;
            slot.durabilityDamage = Math.Max(0, slot.durabilityDamage - RepairPerCurrency(slot.itemId));
            player.currency--;
            return true;
        }
    }
}
