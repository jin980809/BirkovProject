using System;

namespace Birdkov.NaYeongMin.InventorySystem
{
    // 허브 창고(거점 보관함).
    // 연결 방법:
    //   1. 거점 창고 오브젝트의 상호작용(E) 담당이 Open()을 호출한다. 창을 닫을 때 Close()를 호출한다.
    //   2. Open() 이전에는 Store/Withdraw/MoveWithin 이 모두 InventoryResult.DestinationRejected 로 거부된다.
    //   3. UI는 좌측에 Warehouse, 우측에 PlayerInventoryData.inventory 를 배치한다.
    //   4. 창고 데이터는 PlayerSaveData.warehouseData 를 그대로 넘겨 저장/복구한다.
    //   5. 플레이어 사망(ClearOnDeath)은 창고를 건드리지 않는다. 창고 전리품은 유지된다.
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
