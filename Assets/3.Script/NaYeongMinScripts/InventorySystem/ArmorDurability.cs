using System;
using UnityEngine;

namespace Birdkov.NaYeongMin.InventorySystem
{
    [Serializable]
    public sealed class ArmorDurability
    {
        [Min(1)] public int maxDurability = 100;
        [Min(1)] public int damagePerHit = 2;
        [Min(1)] public int repairCost = 3;

        public bool IsArmor(IItemCatalog catalog, GridSlotData slot)
        {
            ItemData item;
            return slot != null && !slot.IsEmpty() && catalog != null &&
                catalog.TryGetItem(slot.itemId, out item) && item.itemType == ItemType.Equipment &&
                (item.equipmentSlotType == EquipmentSlotType.Helmet || item.equipmentSlotType == EquipmentSlotType.Armor);
        }

        public int Remaining(GridSlotData slot) => slot == null || slot.IsEmpty() ? 0 :
            Math.Max(0, Math.Max(1, maxDurability) - Math.Max(0, slot.durabilityDamage));

        public void ApplyHit(IItemCatalog catalog, PlayerInventoryData player)
        {
            if (player == null) return;
            foreach (GridSlotData slot in player.equipmentSlots.slots)
            {
                if (!IsArmor(catalog, slot)) continue;
                int remaining = Remaining(slot);
                if (remaining <= Math.Max(1, damagePerHit)) slot.Clear();
                else slot.durabilityDamage = Math.Max(1, maxDurability) - remaining + Math.Max(1, damagePerHit);
            }
        }

        public bool CanRepair(IItemCatalog catalog, PlayerInventoryData player, GridSlotData slot) =>
            player != null && IsArmor(catalog, slot) && Remaining(slot) > 0 &&
            slot.durabilityDamage > 0 && player.currency >= Math.Max(1, repairCost);

        public bool Repair(IItemCatalog catalog, PlayerInventoryData player, GridSlotData slot)
        {
            if (!CanRepair(catalog, player, slot)) return false;
            slot.durabilityDamage = 0;
            player.currency -= Math.Max(1, repairCost);
            return true;
        }
    }
}
