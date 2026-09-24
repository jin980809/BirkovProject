using System;
using UnityEngine;

namespace Birdkov.NaYeongMin.InventorySystem
{
    // 기획서 5.2 방어구 행 + 6.7. 정본은 ItemData.csv 다.
    // 피격 1회당 장착 중인 모든 부위가 각자 durabilityCostPerHit 만큼 닳고, 0 이 되면 그 부위만 소멸한다.
    // 수리는 무기와 같은 1G 단위. repairAmountPerCurrency 만큼 회복한다.
    [Serializable]
    public sealed class ArmorDurability
    {
        [Tooltip("CSV 의 maxDurability 가 비어 있을 때만 쓰는 예비값")]
        [Min(1)] public int fallbackMaxDurability = 100;
        [Tooltip("CSV 의 durabilityCostPerHit 이 비어 있을 때만 쓰는 예비값")]
        [Min(1)] public int fallbackDamagePerHit = 1;

        private static ItemData Find(IItemCatalog catalog, GridSlotData slot)
        {
            ItemData item;
            return slot != null && !slot.IsEmpty() && catalog != null &&
                catalog.TryGetItem(slot.itemId, out item) ? item : null;
        }

        public bool IsArmor(IItemCatalog catalog, GridSlotData slot)
        {
            ItemData item = Find(catalog, slot);
            return item != null && item.itemType == ItemType.Equipment &&
                (item.equipmentSlotType == EquipmentSlotType.Helmet || item.equipmentSlotType == EquipmentSlotType.Armor);
        }

        public int Maximum(IItemCatalog catalog, GridSlotData slot)
        {
            ItemData item = Find(catalog, slot);
            if (item == null) return 0;
            return item.maxDurability > 0 ? item.maxDurability : Math.Max(1, fallbackMaxDurability);
        }

        public int DamagePerHit(IItemCatalog catalog, GridSlotData slot)
        {
            ItemData item = Find(catalog, slot);
            return item != null && item.durabilityCostPerHit > 0
                ? item.durabilityCostPerHit : Math.Max(1, fallbackDamagePerHit);
        }

        public int RepairPerCurrency(IItemCatalog catalog, GridSlotData slot)
        {
            ItemData item = Find(catalog, slot);
            return item != null ? item.repairAmountPerCurrency : 0;
        }

        public int Remaining(IItemCatalog catalog, GridSlotData slot)
        {
            int max = Maximum(catalog, slot);
            return max <= 0 ? 0 : Math.Max(0, max - Math.Max(0, slot.durabilityDamage));
        }

        public void ApplyHit(IItemCatalog catalog, PlayerInventoryData player)
        {
            if (player == null) return;
            foreach (GridSlotData slot in player.equipmentSlots.slots)
            {
                if (!IsArmor(catalog, slot)) continue;
                int cost = DamagePerHit(catalog, slot);
                if (Remaining(catalog, slot) <= cost) slot.Clear();
                else slot.durabilityDamage = Math.Max(0, slot.durabilityDamage) + cost;
            }
        }

        public bool CanRepair(IItemCatalog catalog, PlayerInventoryData player, GridSlotData slot)
        {
            return player != null && player.currency >= 1 && IsArmor(catalog, slot) &&
                Remaining(catalog, slot) > 0 && slot.durabilityDamage > 0 &&
                RepairPerCurrency(catalog, slot) > 0;
        }

        public bool Repair(IItemCatalog catalog, PlayerInventoryData player, GridSlotData slot)
        {
            if (!CanRepair(catalog, player, slot)) return false;
            slot.durabilityDamage = Math.Max(0, slot.durabilityDamage - RepairPerCurrency(catalog, slot));
            player.currency--;
            return true;
        }
    }
}
