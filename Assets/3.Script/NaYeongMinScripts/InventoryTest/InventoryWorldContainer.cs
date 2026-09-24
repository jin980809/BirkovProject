using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.Rng;
using UnityEngine;

namespace Birdkov.NaYeongMin.InventoryTest
{
    public enum InventoryWorldKind { Storage, Loot, MapChest, Shop, Crafting, Repair, PlayerDeath }

    // 허용 목록만 최초 한 번 추첨. 재개방으로 획득분이 복구되지 않는다.
    public sealed class InventoryWorldContainer : MonoBehaviour
    {
        public InventoryWorldKind kind;
        public string displayName = "상자";
        public ContainerDropSettings dropSettings;
        private LootContainerData contents;

        public LootContainerData GetContents(IItemCatalog catalog)
        {
            if (kind == InventoryWorldKind.PlayerDeath)
                return GetComponent<PlayerDeathContainer>()?.Contents;
            if (contents == null)
            {
                contents = ContainerLootRoller.Roll(dropSettings, catalog, null);
                if (kind == InventoryWorldKind.Loot)
                {
                    LootContainerData limited = new LootContainerData(LootContainerSize.Box2x4);
                    for (int index = 0; index < limited.SlotCount && index < contents.SlotCount; index++)
                        limited.loot.slots[index] = contents.loot.slots[index];
                    contents = limited;
                }
            }
            return contents;
        }

        public void Open(InventoryTestBench inventory)
        {
            if (inventory == null) return;
            switch (kind)
            {
                case InventoryWorldKind.Storage: inventory.OpenStorage(this); break;
                case InventoryWorldKind.Loot: inventory.OpenLoot(this); break;
                case InventoryWorldKind.MapChest: inventory.OpenMapChest(this); break;
                case InventoryWorldKind.Shop: inventory.OpenShop(transform); break;
                case InventoryWorldKind.Crafting: inventory.OpenCrafting(transform); break;
                case InventoryWorldKind.Repair: inventory.OpenRepair(transform); break;
                case InventoryWorldKind.PlayerDeath: inventory.OpenPlayerDeath(this); break;
            }
        }
    }
}
