using System;
using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;

namespace Birdkov.NaYeongMin.Rng
{
    public sealed class LootDropObject : MonoBehaviour
    {
        public LootContainerData Loot { get; private set; } = new LootContainerData();

        public event Action<LootDropObject> Emptied;

        // 원본의 크기 프리셋까지 그대로 따라간다. 상자 크기가 달라도 전량 전달된다.
        public void SetLoot(LootContainerData source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Loot.SetSize(source.sizePreset);
            for (int index = 0; index < source.loot.slots.Count; index++)
            {
                Loot.loot.slots[index].itemId = source.loot.slots[index].itemId;
                Loot.loot.slots[index].amount = source.loot.slots[index].amount;
            }
        }

        public void NotifyContentsChanged()
        {
            if (Loot.IsEmpty())
            {
                Emptied?.Invoke(this);
            }
        }

        public void Clear()
        {
            Loot.Clear();
        }
    }
}
