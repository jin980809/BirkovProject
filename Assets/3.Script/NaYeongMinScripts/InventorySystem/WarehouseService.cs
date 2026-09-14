using System;

// 허브 창고. 거점 오브젝트와 상호작용해 Open 한 뒤에만 입출고가 된다.
namespace Birdkov.NaYeongMin.InventorySystem
{
    // 허브 창고(거점 보관함).
    public sealed class WarehouseService
    {
        private readonly InventoryService inventoryService;

        public WarehouseService(IItemCatalog itemCatalog, GridContainerData warehouse)
        {
            inventoryService = new InventoryService(itemCatalog);
            Warehouse = warehouse ?? new GridContainerData();

            if (Warehouse.slots.Count != Warehouse.width * Warehouse.height || Warehouse.slots.Count == 0)
            {
                Warehouse.SetSize(InventorySettings.WarehouseWidth, InventorySettings.WarehouseHeight);
            }
        }

        public GridContainerData Warehouse { get; }

        public bool IsOpen { get; private set; }

        public event Action<bool> OpenStateChanged;

        // 거점 창고 오브젝트와 상호작용했을 때만 호출한다.
        public void Open()
        {
            SetOpen(true);
        }

        public void Close()
        {
            SetOpen(false);
        }

        // 가방 -> 창고
        public InventoryMoveResult Store(
            PlayerInventoryData playerData,
            int inventoryIndex,
            int warehouseIndex,
            int amount)
        {
            if (!IsOpen || playerData == null)
            {
                return new InventoryMoveResult(InventoryResult.DestinationRejected, 0, amount);
            }

            return inventoryService.MoveItem(playerData.inventory, inventoryIndex, Warehouse, warehouseIndex, amount);
        }

        // 창고 -> 가방
        public InventoryMoveResult Withdraw(
            PlayerInventoryData playerData,
            int warehouseIndex,
            int inventoryIndex,
            int amount)
        {
            if (!IsOpen || playerData == null)
            {
                return new InventoryMoveResult(InventoryResult.DestinationRejected, 0, amount);
            }

            return inventoryService.MoveItem(Warehouse, warehouseIndex, playerData.inventory, inventoryIndex, amount);
        }

        // 창고 내부 정렬용 이동
        public InventoryMoveResult MoveWithin(int sourceIndex, int destinationIndex, int amount)
        {
            if (!IsOpen)
            {
                return new InventoryMoveResult(InventoryResult.DestinationRejected, 0, amount);
            }

            return inventoryService.MoveItem(Warehouse, sourceIndex, Warehouse, destinationIndex, amount);
        }

        private void SetOpen(bool value)
        {
            if (IsOpen == value)
            {
                return;
            }

            IsOpen = value;
            OpenStateChanged?.Invoke(value);
        }
    }
}
