namespace Birdkov.NaYeongMin.InventorySystem
{
    // 장비 슬롯 4칸의 배치와 수용 규칙.
    // 0 주 무기 / 1 보조 무기 / 2 머리 보호구 / 3 몸 보호구.
    // 무기 슬롯 0,1 은 무기 퀵슬롯 1,2 와 그대로 링크된다.
    public static class EquipmentSlots
    {
        public const int PrimaryWeapon = 0;
        public const int SecondaryWeapon = 1;
        public const int Helmet = 2;
        public const int Armor = 3;

        public static bool IsValidSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < InventorySettings.EquipmentSlotCount;
        }

        public static bool IsWeaponSlot(int slotIndex)
        {
            return slotIndex == PrimaryWeapon || slotIndex == SecondaryWeapon;
        }

        public static EquipmentSlotType GetSlotType(int slotIndex)
        {
            switch (slotIndex)
            {
                case PrimaryWeapon:
                    return EquipmentSlotType.PrimaryGun;
                case SecondaryWeapon:
                    return EquipmentSlotType.SecondaryGun;
                case Helmet:
                    return EquipmentSlotType.Helmet;
                case Armor:
                    return EquipmentSlotType.Armor;
                default:
                    return EquipmentSlotType.None;
            }
        }

        // 무기 슬롯은 무기만, 보호구 슬롯은 해당 부위로 지정된 장비만 받는다.
        public static bool Accepts(int slotIndex, ItemData item)
        {
            if (item == null || !IsValidSlot(slotIndex))
            {
                return false;
            }

            if (IsWeaponSlot(slotIndex))
            {
                return item.itemType == ItemType.Weapon;
            }

            return item.itemType == ItemType.Equipment && item.equipmentSlotType == GetSlotType(slotIndex);
        }
    }
}
