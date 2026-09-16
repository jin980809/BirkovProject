using System.Collections.Generic;

// 총알 자체제작. 기획서 5.3 : 같은 종류 버섯 5 + 화약 5 -> 해당 탄종 1박스.
namespace Birdkov.NaYeongMin.InventorySystem
{
    public enum CraftResult
    {
        Success,
        InvalidData,
        UnknownRecipe,
        NotEnoughMushroom,
        NotEnoughGunpowder,
        NoSpace
    }

    public sealed class CraftingService
    {
        public const int GunpowderItemId = 25001;
        public const int MushroomCost = 5;
        public const int GunpowderCost = 5;
        public const int CraftedBoxAmount = 1;

        // 투입한 버섯이 결과 탄종을 결정한다. 별도 탄종 선택 UI 는 두지 않는다.
        private static readonly int[] mushroomItemIds = { 27001, 27002, 27003, 27004 };
        private static readonly int[] ammoItemIds = { 11001, 11002, 11003, 11004 };

        private readonly IItemCatalog itemCatalog;
        private readonly InventoryService inventoryService;

        public CraftingService(IItemCatalog itemCatalog)
        {
            this.itemCatalog = itemCatalog;
            inventoryService = new InventoryService(itemCatalog);
        }

        public static IList<int> MushroomItemIds
        {
            get { return mushroomItemIds; }
        }

        public static bool TryGetAmmoItemId(int mushroomItemId, out int ammoItemId)
        {
            for (int index = 0; index < mushroomItemIds.Length; index++)
            {
                if (mushroomItemIds[index] == mushroomItemId)
                {
                    ammoItemId = ammoItemIds[index];
                    return true;
                }
            }

            ammoItemId = -1;
            return false;
        }

        // 재료 보유량만 본다. 가방 공간은 재료를 빼야 결정되므로 Craft 가 실제로 시도하고 실패 시 되돌린다.
        public CraftResult CanCraft(PlayerInventoryData playerData, int mushroomItemId)
        {
            if (itemCatalog == null || playerData == null || playerData.inventory == null ||
                playerData.inventory.slots == null)
            {
                return CraftResult.InvalidData;
            }

            int ammoItemId;
            ItemData ignored;
            if (!TryGetAmmoItemId(mushroomItemId, out ammoItemId) ||
                !itemCatalog.TryGetItem(ammoItemId, out ignored) ||
                !itemCatalog.TryGetItem(GunpowderItemId, out ignored))
            {
                return CraftResult.UnknownRecipe;
            }

            if (Count(playerData.inventory, mushroomItemId) < MushroomCost)
            {
                return CraftResult.NotEnoughMushroom;
            }

            if (Count(playerData.inventory, GunpowderItemId) < GunpowderCost)
            {
                return CraftResult.NotEnoughGunpowder;
            }

            return CraftResult.Success;
        }

        // 재료를 먼저 빼서 칸을 확보한 뒤 결과를 넣는다.
        // 지급에 실패하면 가방을 통째로 되돌려 재료와 결과 모두 변하지 않게 한다.
        public CraftResult Craft(PlayerInventoryData playerData, int mushroomItemId)
        {
            CraftResult check = CanCraft(playerData, mushroomItemId);
            if (check != CraftResult.Success)
            {
                return check;
            }

            int ammoItemId;
            TryGetAmmoItemId(mushroomItemId, out ammoItemId);

            GridContainerData inventory = playerData.inventory;
            List<GridSlotData> snapshot = Snapshot(inventory);

            bool consumed = Consume(inventory, mushroomItemId, MushroomCost) &&
                            Consume(inventory, GunpowderItemId, GunpowderCost);

            if (!consumed ||
                inventoryService.AddItem(inventory, ammoItemId, CraftedBoxAmount).Result != InventoryResult.Success)
            {
                Restore(inventory, snapshot);
                return CraftResult.NoSpace;
            }

            return CraftResult.Success;
        }

        public int Count(GridContainerData container, int itemId)
        {
            if (container == null || container.slots == null)
            {
                return 0;
            }

            int total = 0;
            foreach (GridSlotData slot in container.slots)
            {
                if (slot != null && slot.itemId == itemId && slot.amount > 0)
                {
                    total += slot.amount;
                }
            }

            return total;
        }

        private bool Consume(GridContainerData container, int itemId, int amount)
        {
            int remaining = amount;
            for (int index = 0; index < container.slots.Count && remaining > 0; index++)
            {
                if (container.slots[index].itemId != itemId)
                {
                    continue;
                }

                remaining -= inventoryService.RemoveItem(container, index, remaining).MovedAmount;
            }

            return remaining == 0;
        }

        private static List<GridSlotData> Snapshot(GridContainerData container)
        {
            List<GridSlotData> copy = new List<GridSlotData>(container.slots.Count);
            foreach (GridSlotData slot in container.slots)
            {
                copy.Add(new GridSlotData { itemId = slot.itemId, amount = slot.amount });
            }

            return copy;
        }

        private static void Restore(GridContainerData container, List<GridSlotData> snapshot)
        {
            for (int index = 0; index < container.slots.Count && index < snapshot.Count; index++)
            {
                container.slots[index].itemId = snapshot[index].itemId;
                container.slots[index].amount = snapshot[index].amount;
            }
        }
    }
}
