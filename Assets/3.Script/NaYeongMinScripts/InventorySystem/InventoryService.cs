using System;

namespace Birdkov.NaYeongMin.InventorySystem
{
    public sealed class InventoryService
    {
        private readonly IItemCatalog itemCatalog;

        public InventoryService(IItemCatalog itemCatalog)
        {
            this.itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));
        }

        public InventoryMoveResult AddItem(GridContainerData container, int itemId, int amount)
        {
            if (container == null)
            {
                return new InventoryMoveResult(InventoryResult.InvalidSlot, 0, amount);
            }

            if (amount <= 0)
            {
                return new InventoryMoveResult(InventoryResult.InvalidAmount, 0, amount);
            }

            if (!itemCatalog.TryGetItem(itemId, out ItemData item))
            {
                return new InventoryMoveResult(InventoryResult.ItemNotFound, 0, amount);
            }

            int remaining = amount;
            int stackLimit = GetStackLimit(item);

            foreach (GridSlotData slot in container.slots)
            {
                if (slot.itemId != itemId || slot.amount >= stackLimit)
                {
                    continue;
                }

                int moved = Math.Min(stackLimit - slot.amount, remaining);
                slot.amount += moved;
                remaining -= moved;

                if (remaining == 0)
                {
                    return new InventoryMoveResult(InventoryResult.Success, amount, 0);
                }
            }

            foreach (GridSlotData slot in container.slots)
            {
                if (!slot.IsEmpty())
                {
                    continue;
                }

                int moved = Math.Min(stackLimit, remaining);
                slot.itemId = itemId;
                slot.amount = moved;
                remaining -= moved;

                if (remaining == 0)
                {
                    return new InventoryMoveResult(InventoryResult.Success, amount, 0);
                }
            }

            int movedAmount = amount - remaining;
            InventoryResult result = movedAmount == 0 ? InventoryResult.DestinationFull : InventoryResult.Partial;
            return new InventoryMoveResult(result, movedAmount, remaining);
        }

        public InventoryMoveResult MoveItem(
            GridContainerData source,
            int sourceIndex,
            GridContainerData destination,
            int destinationIndex,
            int amount)
        {
            if (!TryGetSlot(source, sourceIndex, out GridSlotData sourceSlot) ||
                !TryGetSlot(destination, destinationIndex, out GridSlotData destinationSlot))
            {
                return new InventoryMoveResult(InventoryResult.InvalidSlot, 0, amount);
            }

            if (amount <= 0 || sourceSlot.IsEmpty())
            {
                return new InventoryMoveResult(InventoryResult.InvalidAmount, 0, amount);
            }

            if (ReferenceEquals(source, destination) && sourceIndex == destinationIndex)
            {
                return new InventoryMoveResult(InventoryResult.Success, 0, 0);
            }

            if (!itemCatalog.TryGetItem(sourceSlot.itemId, out ItemData sourceItem))
            {
                return new InventoryMoveResult(InventoryResult.ItemNotFound, 0, amount);
            }

            int requested = Math.Min(amount, sourceSlot.amount);
            int stackLimit = GetStackLimit(sourceItem);

            if (destinationSlot.IsEmpty())
            {
                int moved = Math.Min(requested, stackLimit);
                destinationSlot.itemId = sourceSlot.itemId;
                destinationSlot.amount = moved;
                RemoveFromSlot(sourceSlot, moved);
                return CreateResult(requested, moved);
            }

            if (destinationSlot.itemId == sourceSlot.itemId)
            {
                int moved = Math.Min(requested, Math.Max(0, stackLimit - destinationSlot.amount));
                destinationSlot.amount += moved;
                RemoveFromSlot(sourceSlot, moved);
                return CreateResult(requested, moved);
            }

            if (requested != sourceSlot.amount)
            {
                return new InventoryMoveResult(InventoryResult.DestinationFull, 0, requested);
            }

            int destinationItemId = destinationSlot.itemId;
            int destinationAmount = destinationSlot.amount;
            destinationSlot.itemId = sourceSlot.itemId;
            destinationSlot.amount = sourceSlot.amount;
            sourceSlot.itemId = destinationItemId;
            sourceSlot.amount = destinationAmount;

            return new InventoryMoveResult(InventoryResult.Success, requested, 0);
        }

        public InventoryMoveResult RemoveItem(GridContainerData container, int slotIndex, int amount)
        {
            if (!TryGetSlot(container, slotIndex, out GridSlotData slot))
            {
                return new InventoryMoveResult(InventoryResult.InvalidSlot, 0, amount);
            }

            if (amount <= 0 || slot.IsEmpty())
            {
                return new InventoryMoveResult(InventoryResult.InvalidAmount, 0, amount);
            }

            int removed = Math.Min(amount, slot.amount);
            RemoveFromSlot(slot, removed);
            return CreateResult(amount, removed);
        }

        private static bool TryGetSlot(GridContainerData container, int index, out GridSlotData slot)
        {
            slot = null;
            if (container == null || index < 0 || index >= container.slots.Count)
            {
                return false;
            }

            slot = container.slots[index];
            return slot != null;
        }

        private static void RemoveFromSlot(GridSlotData slot, int amount)
        {
            slot.amount -= amount;
            if (slot.amount <= 0)
            {
                slot.Clear();
            }
        }

        private static InventoryMoveResult CreateResult(int requested, int moved)
        {
            int remaining = requested - moved;
            InventoryResult result = remaining == 0 ? InventoryResult.Success : InventoryResult.Partial;
            return new InventoryMoveResult(result, moved, remaining);
        }

        private static int GetStackLimit(ItemData item)
        {
            if (!item.stackable || item.itemType == ItemType.Weapon || item.itemType == ItemType.Special)
            {
                return InventorySettings.SingleItemStackLimit;
            }

            return Math.Clamp(item.maxStack, 1, InventorySettings.DefaultStackLimit);
        }
    }
}
