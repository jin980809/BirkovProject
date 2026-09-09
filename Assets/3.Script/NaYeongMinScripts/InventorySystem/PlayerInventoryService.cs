using System;

namespace Birdkov.NaYeongMin.InventorySystem
{
    public enum PlayerContainerType
    {
        Inventory,
        Weapon,
        Quick
    }

    public sealed class PlayerInventoryService
    {
        private readonly IItemCatalog itemCatalog;
        private readonly InventoryService inventoryService;

        public PlayerInventoryService(IItemCatalog itemCatalog)
        {
            this.itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));
            inventoryService = new InventoryService(itemCatalog);
        }

        public InventoryMoveResult AddToInventory(PlayerInventoryData playerData, int itemId, int amount)
        {
            if (playerData == null)
            {
                return new InventoryMoveResult(InventoryResult.InvalidSlot, 0, amount);
            }

            return inventoryService.AddItem(playerData.inventory, itemId, amount);
        }

        public InventoryMoveResult Move(
            PlayerInventoryData playerData,
            PlayerContainerType sourceType,
            int sourceIndex,
            PlayerContainerType destinationType,
            int destinationIndex,
            int amount)
        {
            if (playerData == null)
            {
                return new InventoryMoveResult(InventoryResult.InvalidSlot, 0, amount);
            }

            GridContainerData source = GetContainer(playerData, sourceType);
            GridContainerData destination = GetContainer(playerData, destinationType);

            if (!TryGetItem(source, sourceIndex, out ItemData sourceItem) ||
                !Accepts(destinationType, sourceItem))
            {
                return new InventoryMoveResult(InventoryResult.DestinationRejected, 0, amount);
            }

            GridSlotData destinationSlot = GetSlot(destination, destinationIndex);
            if (destinationSlot != null && !destinationSlot.IsEmpty())
            {
                if (!itemCatalog.TryGetItem(destinationSlot.itemId, out ItemData destinationItem) ||
                    !Accepts(sourceType, destinationItem))
                {
                    return new InventoryMoveResult(InventoryResult.DestinationRejected, 0, amount);
                }
            }

            return inventoryService.MoveItem(source, sourceIndex, destination, destinationIndex, amount);
        }

        public void ClearOnDeath(PlayerInventoryData playerData)
        {
            playerData?.ClearAll();
        }

        private bool TryGetItem(GridContainerData container, int index, out ItemData item)
        {
            item = null;
            GridSlotData slot = GetSlot(container, index);
            return slot != null && !slot.IsEmpty() && itemCatalog.TryGetItem(slot.itemId, out item);
        }

        private static GridSlotData GetSlot(GridContainerData container, int index)
        {
            return container != null && index >= 0 && index < container.slots.Count
                ? container.slots[index]
                : null;
        }

        private static GridContainerData GetContainer(PlayerInventoryData playerData, PlayerContainerType containerType)
        {
            return containerType switch
            {
                PlayerContainerType.Weapon => playerData.weaponSlots,
                PlayerContainerType.Quick => playerData.quickSlots,
                _ => playerData.inventory
            };
        }

        private static bool Accepts(PlayerContainerType containerType, ItemData item)
        {
            return containerType switch
            {
                PlayerContainerType.Weapon => item.itemType == ItemType.Weapon,
                PlayerContainerType.Quick => item.itemType == ItemType.Consumable || item.itemType == ItemType.Special,
                _ => true
            };
        }
    }
}
