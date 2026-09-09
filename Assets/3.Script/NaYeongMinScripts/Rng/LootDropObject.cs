using System;
using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;

namespace Birdkov.NaYeongMin.Rng
{
    public sealed class LootDropObject : MonoBehaviour
    {
        public LootContainerData Loot { get; private set; } = new LootContainerData();

        public event Action<LootDropObject> Emptied;

        public void SetLoot(LootContainerData source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Loot.Clear();
            int count = Math.Min(Loot.loot.slots.Count, source.loot.slots.Count);
            for (int index = 0; index < count; index++)
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
