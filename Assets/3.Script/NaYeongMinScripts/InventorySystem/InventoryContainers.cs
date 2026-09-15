using System.Collections.Generic;
using System;

// 저장되는 인벤토리 데이터 구조. 로직은 서비스 쪽에 있다.
namespace Birdkov.NaYeongMin.InventorySystem
{
        // ---- GridSlotData ----
    [Serializable]
    public class GridSlotData
    {
        public int itemId = -1;
        public int amount;

        // 탄약 전용. 이 슬롯의 박스 중 하나가 뜯다 만 박스일 때 그 잔탄 수다. 0 이면 전부 미개봉.
        public int remainingRounds;

        public bool IsEmpty()
        {
            return itemId < 0 || amount <= 0;
        }

        public void Clear()
        {
            itemId = -1;
            amount = 0;
            remainingRounds = 0;
        }
    }

        // ---- GridContainerData ----
    [Serializable]
    public class GridContainerData
    {
        public int width;
        public int height;
        public List<GridSlotData> slots = new List<GridSlotData>();

        public GridContainerData()
        {
        }

        public GridContainerData(int width, int height)
        {
            SetSize(width, height);
        }

        public void SetSize(int width, int height)
        {
            this.width = Math.Max(0, width);
            this.height = Math.Max(0, height);
            slots.Clear();

            int slotCount = this.width * this.height;
            for (int index = 0; index < slotCount; index++)
            {
                slots.Add(new GridSlotData());
            }
        }

        public void Clear()
        {
            foreach (GridSlotData slot in slots)
            {
                slot.Clear();
            }
        }
    }

        // ---- LootContainerData ----
    [Serializable]
    public class LootContainerData
    {
        public LootContainerSize sizePreset = LootContainerSize.Box2x4;
        public GridContainerData loot = new GridContainerData(2, 4);

        public LootContainerData()
        {
        }

        public LootContainerData(LootContainerSize sizePreset)
        {
            SetSize(sizePreset);
        }

        public int SlotCount => loot.slots.Count;

        // 크기 변경은 내용물을 비운다. 파밍 전에만 호출한다.
        public void SetSize(LootContainerSize sizePreset)
        {
            this.sizePreset = sizePreset;
            LootContainerSizes.GetSize(sizePreset, out int width, out int height);
            loot.SetSize(width, height);
        }

        public bool IsEmpty()
        {
            foreach (GridSlotData slot in loot.slots)
            {
                if (!slot.IsEmpty())
                {
                    return false;
                }
            }

            return true;
        }

        public void Clear()
        {
            loot.Clear();
        }
    }

        // ---- PlayerInventoryData ----
    [Serializable]
    public class PlayerInventoryData
    {
        // 가방 25칸. 무기와 보호구도 착용 전에는 여기에 보관된다.
        public GridContainerData inventory = new GridContainerData(InventorySettings.InventoryWidth, InventorySettings.InventoryHeight);

        // 장비 슬롯 4칸. EquipmentSlots 의 상수로 접근한다.
        public GridContainerData equipmentSlots = new GridContainerData(InventorySettings.EquipmentSlotCount, 1);

        // 지푸라기(화폐)는 가방 25칸을 차지하지 않는다. 전리품에서 획득하면
        // 가방 슬롯을 거치지 않고 이 수치에 즉시 합산된다. 기획서 10.1 [지푸라기에 대해].
        public int currency;

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

        // 사망 시 전리품은 모두 소멸한다. 지푸라기도 함께 사라진다. 창고는 별도라 유지된다.
        public void ClearAll()
        {
            inventory.Clear();
            equipmentSlots.Clear();
            ClearItemQuickSlots();
            currency = 0;
        }
    }
}
