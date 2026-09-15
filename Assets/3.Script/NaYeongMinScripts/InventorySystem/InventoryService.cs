using System;

// 컨테이너 종류를 가리지 않는 순수 인벤토리 로직. 추가·이동·제거·스택.
namespace Birdkov.NaYeongMin.InventorySystem
{
        // ---- InventoryResult ----
    public enum InventoryResult
    {
        Success,
        Partial,
        InvalidAmount,
        InvalidSlot,
        ItemNotFound,
        DestinationRejected,
        DestinationFull
    }

    public readonly struct InventoryMoveResult
    {
        public InventoryResult Result { get; }
        public int MovedAmount { get; }
        public int RemainingAmount { get; }

        public InventoryMoveResult(InventoryResult result, int movedAmount, int remainingAmount)
        {
            Result = result;
            MovedAmount = movedAmount;
            RemainingAmount = remainingAmount;
        }
    }

        // ---- InventoryService ----
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
                // 뜯다 만 박스는 슬롯을 통째로 옮길 때만 따라간다.
                destinationSlot.remainingRounds = moved == sourceSlot.amount ? sourceSlot.remainingRounds : 0;
                RemoveFromSlot(sourceSlot, moved);
                return CreateResult(requested, moved);
            }

            // 양쪽 다 미개봉일 때만 합친다. 뜯다 만 박스끼리는 합치지 않는다.
            if (destinationSlot.itemId == sourceSlot.itemId &&
                (destinationSlot.remainingRounds == 0 || sourceSlot.remainingRounds == 0))
            {
                if (destinationSlot.remainingRounds == 0 && sourceSlot.remainingRounds > 0 &&
                    requested != sourceSlot.amount)
                {
                    return new InventoryMoveResult(InventoryResult.DestinationFull, 0, requested);
                }

                int moved = Math.Min(requested, Math.Max(0, stackLimit - destinationSlot.amount));
                if (moved == sourceSlot.amount && sourceSlot.remainingRounds > 0)
                {
                    destinationSlot.remainingRounds = sourceSlot.remainingRounds;
                }

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
            int destinationRounds = destinationSlot.remainingRounds;
            destinationSlot.itemId = sourceSlot.itemId;
            destinationSlot.amount = sourceSlot.amount;
            destinationSlot.remainingRounds = sourceSlot.remainingRounds;
            sourceSlot.itemId = destinationItemId;
            sourceSlot.amount = destinationAmount;
            sourceSlot.remainingRounds = destinationRounds;

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
