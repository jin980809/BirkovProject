namespace Birdkov.NaYeongMin.InventorySystem
{
    public static class InventorySettings
    {
        public const int InventoryWidth = 5;
        public const int InventoryHeight = 5;

        // 장비 슬롯 4칸. 배치 순서는 기획 시안과 같다: 무기 / 무기 / 머리 / 몸.
        public const int EquipmentSlotCount = 4;

        // 퀵슬롯은 두 종류다.
        //  - 무기 퀵슬롯 2칸(Alpha 1,2)은 장비 무기 슬롯 0,1 을 그대로 비추는 링크다. 별도 데이터가 없다.
        //  - 아이템 퀵슬롯 3칸(Alpha 3,4,5)은 가방 슬롯 인덱스를 가리키는 매핑이다.
        public const int WeaponQuickSlotCount = 2;
        public const int ItemQuickSlotCount = 3;
        public const int UnassignedQuickSlot = -1;

        public const int LootSlotCount = 8;
        public const int DefaultStackLimit = 5;
        public const int SingleItemStackLimit = 1;
        public const int DropObjectPoolSize = 30;

        // 허브 창고. 기획서에 칸 수 규정이 없어 원작(Escape from Duckov) 참조로 잠정 확정했다.
        // 원작은 확장형이며 만렙 기준 120칸으로 알려져 있다. 본 프로젝트는 확장 시스템이 없으므로
        // 만렙 용량을 고정값으로 쓴다. 10 x 12 = 120칸. 확정 시 이 상수만 수정하면 된다.
        public const int WarehouseWidth = 10;
        public const int WarehouseHeight = 12;

        public const int InventorySlotCount = InventoryWidth * InventoryHeight;
    }
}
