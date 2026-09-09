namespace Birdkov.NaYeongMin.InventorySystem
{
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
}
