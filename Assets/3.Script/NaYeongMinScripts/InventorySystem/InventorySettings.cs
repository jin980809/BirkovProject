namespace Birdkov.NaYeongMin.InventorySystem
{
    public static class InventorySettings
    {
        public const int InventoryWidth = 5;
        public const int InventoryHeight = 5;
        public const int WeaponSlotCount = 5;
        public const int EquippedWeaponSlotCount = 2;
        public const int QuickSlotCount = 3;
        public const int LootSlotCount = 8;
        public const int DefaultStackLimit = 5;
        public const int SingleItemStackLimit = 1;
        public const int DropObjectPoolSize = 30;

        public static bool IsEquippedWeaponSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < EquippedWeaponSlotCount;
        }
    }
}
