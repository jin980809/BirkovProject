using System;
using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;

namespace Birdkov.NaYeongMin.InventoryTest
{
    [RequireComponent(typeof(InventoryWorldContainer))]
    public sealed class PlayerDeathContainer : MonoBehaviour
    {
        [SerializeField] private LootContainerData contents = new LootContainerData(LootContainerSize.PlayerDeath5x6);
        public LootContainerData Contents => contents;
        public event Action<PlayerDeathContainer> ContentsChanged;

        private void Reset()
        {
            var container = GetComponent<InventoryWorldContainer>();
            container.kind = InventoryWorldKind.PlayerDeath;
            container.displayName = "분실물";
        }

        // 팀원 저장/복원용 경계. JSON/파일/씬 전환 처리는 호출자 담당.
        // 30칸 초과는 폐기하지 않고 거부. 원본 슬롯 참조는 공유하지 않는다.
        public bool SetItems(IEnumerable<GridSlotData> items)
        {
            if (items == null) return false;
            var replacement = new LootContainerData(LootContainerSize.PlayerDeath5x6);
            int index = 0;
            foreach (var slot in items)
            {
                if (slot == null || slot.IsEmpty()) continue;
                if (slot.itemId == 20001) continue; // 지푸라기는 사망 보관/복원 대상이 아니다.
                if (index >= replacement.SlotCount || slot.remainingRounds < 0 || slot.durabilityDamage < 0) return false;
                replacement.loot.slots[index++] = Copy(slot);
            }
            // 열린 UI도 같은 컨테이너를 유지하도록 내용만 교체한다.
            contents.loot = replacement.loot;
            contents.sizePreset = replacement.sizePreset;
            NotifyChanged();
            return true;
        }

        public bool Capture(PlayerInventoryData player)
        {
            if (player?.inventory?.slots == null || player.equipmentSlots?.slots == null) return false;
            var slots = new List<GridSlotData>(player.inventory.slots);
            slots.AddRange(player.equipmentSlots.slots);
            return SetItems(slots);
        }

        public List<GridSlotData> CopyItems()
        {
            var result = new List<GridSlotData>();
            foreach (var slot in contents.loot.slots) result.Add(Copy(slot));
            return result;
        }

        public void NotifyChanged() { ContentsChanged?.Invoke(this); }

        private static GridSlotData Copy(GridSlotData slot)
        {
            return new GridSlotData { itemId = slot.itemId, amount = slot.amount,
                remainingRounds = slot.remainingRounds, durabilityDamage = slot.durabilityDamage };
        }
    }
}
