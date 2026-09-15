
// 인벤토리 크기 상수, 장비 슬롯 배치 규칙, 상자 크기 프리셋.
namespace Birdkov.NaYeongMin.InventorySystem
{
        // ---- InventorySettings ----
    public static class InventorySettings
    {
        public const int InventoryWidth = 5;
        public const int InventoryHeight = 5;

        // 장비 슬롯 4칸. 배치 순서는 기획 시안과 같다: 무기 / 무기 / 머리 / 몸.
        public const int EquipmentSlotCount = 4;

        // 퀵슬롯은 두 종류다.
        public const int WeaponQuickSlotCount = 2;
        public const int ItemQuickSlotCount = 3;
        public const int UnassignedQuickSlot = -1;

        public const int LootSlotCount = 8;
        public const int DefaultStackLimit = 5;
        public const int SingleItemStackLimit = 1;
        public const int DropObjectPoolSize = 30;
        // 허브 창고. 기획서에 칸 수 규정이 없어 가로는 기획서 6.3 가방과 같은 5칸으로 맞추고,
        // 보관량은 원작(Escape from Duckov) 참조 120칸을 유지해 세로 24칸으로 둔다. UI 는 세로 스크롤한다.
        public const int WarehouseWidth = 5;
        public const int WarehouseHeight = 24;

        public const int InventorySlotCount = InventoryWidth * InventoryHeight;
    }

        // ---- EquipmentSlots ----
    // 장비 슬롯 4칸의 배치와 수용 규칙.
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

        // ---- LootContainerSize ----
    // 파밍 컨테이너 크기 프리셋. 기획서 8.1.1 상자 규격표 기준.
    // 무기 3x5 Box_Gun / 탄약 4x1 Box_Ammo / 방어구 3x3 Box_Arm / 회복약 2x4 Box_Med / 식량 2x4 Box_Food / 일반 2x4 Box_Norm
    // 적 사망 노란 오브제는 Box2x4(8칸)를 사용해 기획서 전리품 8칸 규칙과 일치시킨다.
    public enum LootContainerSize
    {
        Box2x4,
        Box3x3,
        Box3x5,
        Box4x1
    }

    public static class LootContainerSizes
    {
        public const LootContainerSize CorpseSize = LootContainerSize.Box2x4;

        public static void GetSize(LootContainerSize sizePreset, out int width, out int height)
        {
            switch (sizePreset)
            {
                case LootContainerSize.Box3x3:
                    width = 3;
                    height = 3;
                    break;
                case LootContainerSize.Box3x5:
                    width = 3;
                    height = 5;
                    break;
                case LootContainerSize.Box4x1:
                    width = 4;
                    height = 1;
                    break;
                default:
                    width = 2;
                    height = 4;
                    break;
            }
        }

        public static int GetSlotCount(LootContainerSize sizePreset)
        {
            GetSize(sizePreset, out int width, out int height);
            return width * height;
        }
    }
}
