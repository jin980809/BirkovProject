using System;

namespace Birdkov.NaYeongMin.InventorySystem
{
    [Serializable]
    public class PlayerInventoryData
    {
        // 가방 25칸. 무기와 보호구도 착용 전에는 여기에 보관된다.
        public GridContainerData inventory = new GridContainerData(InventorySettings.InventoryWidth, InventorySettings.InventoryHeight);

        // 장비 슬롯 4칸. EquipmentSlots 의 상수로 접근한다.
        public GridContainerData equipmentSlots = new GridContainerData(InventorySettings.EquipmentSlotCount, 1);

        // 아이템 퀵슬롯 3칸. 가방 슬롯 인덱스를 가리키는 매핑이며 -1 은 미지정이다.
        // 무기 퀵슬롯 2칸은 equipmentSlots 0,1 을 그대로 비추는 링크라 별도 데이터가 없다.
        public int[] itemQuickSlotIndices = CreateEmptyItemQuickSlots();

        public static int[] CreateEmptyItemQuickSlots()
        {
            int[] indices = new int[InventorySettings.ItemQuickSlotCount];
            for (int index = 0; index < indices.Length; index++)
            {
                indices[index] = InventorySettings.UnassignedQuickSlot;
            }

            return indices;
        }

        public void ClearItemQuickSlots()
        {
            if (itemQuickSlotIndices == null || itemQuickSlotIndices.Length != InventorySettings.ItemQuickSlotCount)
            {
                itemQuickSlotIndices = CreateEmptyItemQuickSlots();
                return;
            }

            for (int index = 0; index < itemQuickSlotIndices.Length; index++)
            {
                itemQuickSlotIndices[index] = InventorySettings.UnassignedQuickSlot;
            }
        }

        public void ClearAll()
        {
            inventory.Clear();
            equipmentSlots.Clear();
            ClearItemQuickSlots();
        }
    }
}
