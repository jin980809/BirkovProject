using UnityEngine;
using UnityEngine.UI;

namespace Birdkov.NaYeongMin.InventoryTest
{
    public sealed partial class InventoryTestBench
    {
        public GameObject playerDeathPanel;
        public RectTransform playerDeathGrid;
        private bool playerDeathOpen;

        public void OpenPlayerDeath(InventoryWorldContainer source)
        {
            if (!IsReady || source == null || !source.isActiveAndEnabled || source.kind != InventoryWorldKind.PlayerDeath) return;
            var corpse = source.GetComponent<PlayerDeathContainer>();
            if (corpse == null) return;
            OpenInventory();
            if (playerDeathPanel == null)
            {
                playerDeathPanel = MakePanel("PlayerDeathPanel", RightX, HeaderBottom, ColumnWidth, SidePanelHeight, "분실물 / 30칸");
                playerDeathGrid = MakeRect("PlayerDeathGrid", playerDeathPanel.transform, 20, 60, 420, 550);
            }
            openedContainer = source;
            playerDeathOpen = true;
            loot = corpse.Contents;
            playerDeathPanel.SetActive(true);
            BuildLootGrid();
            Refresh();
            // 상세/우클릭 UI보다 뒤에서 그린다.
            playerDeathPanel.transform.SetAsFirstSibling();
        }
    }
}
