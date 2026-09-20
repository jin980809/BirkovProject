using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;
using UnityEngine.UI;

namespace Birdkov.NaYeongMin.InventoryTest
{
    // 기획서 5.1 상점 / 5.3 총알 제작대. UI 는 하이어라키에서 만든 것을 쓰고,
    // 참조가 비어 있을 때만 예전처럼 코드로 패널을 만든다.
    public sealed partial class InventoryTestBench
    {
        [Header("개인 씬 테스트 지급")]
        public bool seedExtendedTestStock;

        // 행 색은 인스펙터의 colors 에서 온다. 살 수 있으면 녹색, 못 사면 적갈색.

        private Transform serviceAnchor;
        private GameObject shopPanel;
        private readonly List<ItemData> shopItems = new List<ItemData>();
        private readonly List<Text> shopLabels = new List<Text>();
        private Text shopSelection;
        private int shopPage;
        private int selectedShopBagIndex = -1;
        private bool selectedShopEquipment;
        private int selectedShopRow;
        private int selectedCraftRow;
        public int SelectedWeaponIndex => selectedWeapon;
        public GridContainerData WarehouseData => data?.warehouseData;

        private int ShopRowCount => ui != null && ui.shopRowButtons != null ? ui.shopRowButtons.Length : 0;
        private bool UsesHierarchyShop => ui != null && ui.shopPanel != null;

        private void SeedExtendedTestStock()
        {
            data.warehouseData.Clear();
            foreach (ItemData item in ItemCsvLoader.Parse(itemCsv.text))
            {
                if (item.itemType == ItemType.Currency || item.itemId == 24002 || item.itemId == 24003 || item.itemId == 24004) continue;
                int amount = item.stackable ? 20 : 3;
                InventoryMoveResult result = inventoryService.AddItem(data.warehouseData, item.itemId, amount);
                if (result.Result != InventoryResult.Success) Debug.LogWarning("테스트 창고 공간 부족: " + item.itemId, this);
            }
            foreach (int itemId in CraftingService.MushroomItemIds) playerService.AddToInventory(PlayerData, itemId, 5);
            playerService.AddToInventory(PlayerData, CraftingService.GunpowderItemId, 5);
            PlayerData.currency = 200;
        }

        // ---------- 열기 / 닫기 ----------
        public void OpenCrafting(Transform anchor)
        {
            if (!IsReady || anchor == null) return;
            OpenInventory();
            if (craftPanel == null) BuildCraftPanel();
            serviceAnchor = anchor;
            selectedCraftRow = 0;
            craftPanel.SetActive(true);
            OpenWarehouseBeside();
            SetMessage("가방 + 창고의 재료를 합산합니다. 버섯 5 + 화약 5 → 20발 1박스.");
            Refresh();
        }

        public void OpenShop(Transform anchor)
        {
            if (!IsReady || anchor == null) return;
            OpenInventory();
            EnsureShopPanel();
            serviceAnchor = anchor;
            selectedShopBagIndex = -1;
            selectedShopRow = 0;
            shopPage = 0;
            shopPanel.SetActive(true);
            OpenWarehouseBeside();
            Refresh();
        }

        // 상점/제작은 가방과 창고 재고를 같이 보고 쓴다. 오른쪽에 창고를 같이 연다.
        private void OpenWarehouseBeside()
        {
            if (warehousePanel == null || warehouseService == null) return;
            warehouseService.Open();
            MoveWarehouse(false);
            warehousePanel.SetActive(true);
            RebuildInventoryColumn();
            ScrollToWarehouse();
        }

        // 창고 칸이 세로 목록에 끼거나 빠지면 스크롤 길이를 다시 계산해야 한다.
        // 창고는 세로 목록 아래쪽에 있어서 열자마자 보이도록 스크롤을 내린다.
        // 창고만 열면 전리품 상자처럼 오른쪽에, 상점/제작대로 열면 왼쪽 세로 목록에 붙인다.
        private void MoveWarehouse(bool solo)
        {
            if (warehousePanel == null || ui == null) return;
            if (ui.warehouseSoloContent == null && screen != null)
            {
                Transform found = screen.Find("WarehouseSoloScroll");
                if (found != null)
                {
                    ui.warehouseSoloScroll = found.gameObject;
                    ui.warehouseSoloContent = found.Find("Content") as RectTransform;
                }
            }
            if (ui.inventoryColumn == null && screen != null)
            {
                Transform found = screen.Find("InventoryScroll/Content");
                if (found != null) ui.inventoryColumn = found as RectTransform;
            }

            RectTransform target = solo ? ui.warehouseSoloContent : ui.inventoryColumn;
            if (target == null) return;
            if (warehousePanel.transform.parent != target)
            {
                warehousePanel.transform.SetParent(target, false);
                warehousePanel.transform.SetAsLastSibling();
            }
            if (ui.warehouseSoloScroll != null) ui.warehouseSoloScroll.SetActive(solo);
            LayoutRebuilder.ForceRebuildLayoutImmediate(target);
        }

        private void ScrollToWarehouse()
        {
            if (warehousePanel == null) return;
            ScrollRect column = warehousePanel.GetComponentInParent<ScrollRect>();
            if (column == null) return;
            Canvas.ForceUpdateCanvases();
            column.verticalNormalizedPosition = 0f;
        }

        private void RebuildInventoryColumn()
        {
            if (warehousePanel == null) return;
            RectTransform column = warehousePanel.transform.parent as RectTransform;
            if (column != null) LayoutRebuilder.ForceRebuildLayoutImmediate(column);
        }

        private void EnsureShopPanel()
        {
            if (shopItems.Count == 0)
            {
                foreach (ItemData item in ItemCsvLoader.Parse(itemCsv.text))
                    if (ShopService.IsTradable(item)) shopItems.Add(item);
            }

            if (shopPanel != null) return;
            if (UsesHierarchyShop) { shopPanel = ui.shopPanel; BindShopButtons(); return; }
            BuildShop();
        }

        private void BindShopButtons()
        {
            BindRepairPanel();
            for (int index = 0; index < ShopRowCount; index++)
            {
                Button button = ui.shopRowButtons[index];
                if (button == null) continue;
                int row = index;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => { selectedShopRow = row; Refresh(); });
            }

            Bind(ui.shopPrev, () => { shopPage = Mathf.Max(0, shopPage - 1); selectedShopRow = 0; Refresh(); });
            Bind(ui.shopNext, () => { shopPage = Mathf.Min(LastShopPage, shopPage + 1); selectedShopRow = 0; Refresh(); });
            Bind(ui.shopBuy, () => BuyShopRow(selectedShopRow));
            Bind(ui.shopSell, SellSelectedShopItem);
            Bind(ui.repairButton, RepairSelectedWeapon);
            Bind(ui.craftConfirm, () => CraftAmmo(CraftingService.MushroomItemIds[selectedCraftRow]));

            for (int index = 0; ui.craftButtons != null && index < ui.craftButtons.Length; index++)
            {
                if (ui.craftButtons[index] == null) continue;
                int row = index;
                ui.craftButtons[index].onClick.RemoveAllListeners();
                // 제작 버튼이 따로 있으면 목록 행은 선택만 한다.
                if (ui.craftConfirm != null) ui.craftButtons[index].onClick.AddListener(() => { selectedCraftRow = row; Refresh(); });
                else ui.craftButtons[index].onClick.AddListener(() => CraftAmmo(CraftingService.MushroomItemIds[row]));
            }
        }

        // 수리대는 상점과 분리된 별도 패널. 하이어라키의 RepairPanel 을 이름으로 자동 참조한다.
        private void BindRepairPanel()
        {
            if (ui == null || screen == null || ui.repairPanel != null) return;
            Transform panel = screen.Find("RepairPanel");
            if (panel == null) return;
            ui.repairPanel = panel.gameObject;
            Transform node = panel.Find("Label");
            if (node != null) ui.repairCurrency = node.GetComponent<Text>();
            node = panel.Find("Detail/DetailIcon");
            if (node != null) ui.repairDetailIcon = node.GetComponent<Image>();
            node = panel.Find("Detail/Label");
            if (node != null) ui.repairDetail = node.GetComponent<Text>();
            node = panel.Find("Button_수리 1G");
            if (node != null) ui.repairButton = node.GetComponent<Button>();
        }

        private bool IsServicePanelOpen =>
            (shopPanel != null && shopPanel.activeSelf) ||
            (ui != null && ui.repairPanel != null && ui.repairPanel.activeSelf);

        public void OpenRepair(Transform anchor)
        {
            if (!IsReady || anchor == null) return;
            OpenInventory();
            BindRepairPanel();
            if (ui == null || ui.repairPanel == null) { SetMessage("수리대 UI 참조 없음: RepairPanel 확인"); return; }
            serviceAnchor = anchor;
            selectedShopBagIndex = -1;
            selectedShopEquipment = false;
            ui.repairPanel.SetActive(true);
            SetMessage("가방 또는 장비 칸에서 수리할 총기를 고르세요.");
            Refresh();
        }

        private void RefreshRepair()
        {
            if (ui == null || ui.repairPanel == null || !ui.repairPanel.activeSelf || data == null) return;
            GridSlotData slot = SelectedSlot();
            int itemId = slot != null && !slot.IsEmpty() ? slot.itemId : 0;
            int max = WeaponDurability.Maximum(itemId);
            int now = max > 0 ? WeaponDurability.Remaining(slot) : 0;

            if (ui.repairCurrency != null) ui.repairCurrency.text = "보유 " + PlayerData.currency + "G";
            if (ui.repairDetailIcon != null)
            {
                Sprite sprite;
                ui.repairDetailIcon.sprite = spriteMap.TryGetValue(itemId, out sprite) ? sprite : null;
                ui.repairDetailIcon.enabled = ui.repairDetailIcon.sprite != null;
            }
            if (ui.repairDetail != null)
                ui.repairDetail.text = max <= 0
                    ? "가방 또는 장비 칸에서 수리할 총기를 고르세요."
                    : DisplayName(itemId) + "\n내구도 " + now + " / " + max + "\n1G 당 회복량 " + RepairPerCurrencyOf(itemId);
            if (ui.repairButton != null)
                ui.repairButton.interactable = max > 0 && now < max && PlayerData.currency > 0;
        }

        private int RepairPerCurrencyOf(int itemId)
        {
            ItemData item;
            return catalog != null && catalog.TryGetItem(itemId, out item) && item.repairAmountPerCurrency > 0
                ? item.repairAmountPerCurrency : 0;
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private int LastShopPage => ShopRowCount <= 0 ? 0 : Mathf.Max(0, (shopItems.Count - 1) / ShopRowCount);

        // ---------- 갱신 ----------
        private void RefreshShop()
        {
            if (shopPanel == null || !shopPanel.activeSelf || data == null) return;
            if (!UsesHierarchyShop) { RefreshLegacyShop(); return; }

            int rows = ShopRowCount;
            for (int index = 0; index < rows; index++)
            {
                int itemIndex = shopPage * rows + index;
                ItemData item = itemIndex < shopItems.Count ? shopItems[itemIndex] : null;
                bool affordable = item != null && PlayerData.currency >= item.price;
                bool selected = index == selectedShopRow;

                if (ui.shopRowButtons[index] != null) ui.shopRowButtons[index].gameObject.SetActive(item != null);
                if (index < ui.shopRowLabels.Length && ui.shopRowLabels[index] != null)
                    ui.shopRowLabels[index].text = item == null ? string.Empty : item.displayName + "  " + item.price + "G";
                if (index < ui.shopRowIcons.Length && ui.shopRowIcons[index] != null)
                {
                    Sprite sprite = item != null && spriteMap.TryGetValue(item.itemId, out Sprite found) ? found : null;
                    ui.shopRowIcons[index].sprite = sprite;
                    ui.shopRowIcons[index].enabled = sprite != null;
                }
                if (index < ui.shopRowBackgrounds.Length && ui.shopRowBackgrounds[index] != null)
                    ui.shopRowBackgrounds[index].color = affordable
                        ? (selected ? colors.rowReadySelected : colors.rowReady)
                        : (selected ? colors.rowBlockedSelected : colors.rowBlocked);
            }

            ItemData chosen = SelectedShopItem();
            // 목록은 스크롤이라 쪽 개념이 없다. 보유 화폐만 보여 준다.
            if (ui.shopCurrency != null) ui.shopCurrency.text = "보유 " + PlayerData.currency + "G";
            if (ui.shopDetailIcon != null)
            {
                Sprite sprite = chosen != null && spriteMap.TryGetValue(chosen.itemId, out Sprite found) ? found : null;
                ui.shopDetailIcon.sprite = sprite;
                ui.shopDetailIcon.enabled = sprite != null;
            }
            if (ui.shopDetail != null) ui.shopDetail.text = ShopDetailText(chosen);
            if (ui.shopBuy != null) ui.shopBuy.interactable = chosen != null && PlayerData.currency >= chosen.price;
        }

        private ItemData SelectedShopItem()
        {
            int itemIndex = shopPage * ShopRowCount + selectedShopRow;
            return itemIndex >= 0 && itemIndex < shopItems.Count ? shopItems[itemIndex] : null;
        }

        private string ShopDetailText(ItemData item)
        {
            GridSlotData slot = SelectedSlot();
            string bag = slot != null && !slot.IsEmpty()
                ? "선택: " + DisplayName(slot.itemId) + (WeaponDurability.Maximum(slot.itemId) > 0
                    ? "  내구도 " + WeaponDurability.Remaining(slot) + " / " + WeaponDurability.Maximum(slot.itemId) : string.Empty)
                : "가방 또는 장비 칸에서 판매할 아이템을 고르세요.";
            if (item == null) return bag;
            string line = item.displayName + "   " + item.price + "G\n";
            if (WeaponDurability.Maximum(item.itemId) > 0)
                line += "최대 내구도 " + WeaponDurability.Maximum(item.itemId) + "\n";
            if (!string.IsNullOrEmpty(item.description)) line += item.description + "\n";
            return line + bag;
        }

        // 제작 목록의 색과 아이콘. 글자는 RefreshCraftPanel 이 채운다.
        private void RefreshCraftVisuals()
        {
            if (craftPanel == null || !craftPanel.activeSelf || data == null || ui == null) return;
            IList<int> mushrooms = CraftingService.MushroomItemIds;
            for (int index = 0; index < mushrooms.Count; index++)
            {
                bool ready = craftingService.CanCraft(data.inventoryData, mushrooms[index], data.warehouseData) == CraftResult.Success;
                bool selected = index == selectedCraftRow;
                if (ui.craftRowBackgrounds != null && index < ui.craftRowBackgrounds.Length && ui.craftRowBackgrounds[index] != null)
                    ui.craftRowBackgrounds[index].color = ready
                        ? (selected ? colors.rowReadySelected : colors.rowReady)
                        : (selected ? colors.rowBlockedSelected : colors.rowBlocked);
                if (ui.craftRowIcons != null && index < ui.craftRowIcons.Length && ui.craftRowIcons[index] != null)
                {
                    Sprite sprite = spriteMap.TryGetValue(mushrooms[index], out Sprite found) ? found : null;
                    ui.craftRowIcons[index].sprite = sprite;
                    ui.craftRowIcons[index].enabled = sprite != null;
                }
            }

            int mushroomItemId = mushrooms[Mathf.Clamp(selectedCraftRow, 0, mushrooms.Count - 1)];
            int ammoItemId;
            CraftingService.TryGetAmmoItemId(mushroomItemId, out ammoItemId);
            if (ui.craftDetailIcon != null)
            {
                Sprite sprite = spriteMap.TryGetValue(ammoItemId, out Sprite found) ? found : null;
                ui.craftDetailIcon.sprite = sprite;
                ui.craftDetailIcon.enabled = sprite != null;
            }
            if (ui.craftDetail != null)
            {
                int mushroom = craftingService.Count(data.inventoryData.inventory, mushroomItemId) + craftingService.Count(data.warehouseData, mushroomItemId);
                int powder = craftingService.Count(data.inventoryData.inventory, CraftingService.GunpowderItemId) + craftingService.Count(data.warehouseData, CraftingService.GunpowderItemId);
                ui.craftDetail.text = DisplayName(ammoItemId) + "  1박스 (20발)\n" +
                    DisplayName(mushroomItemId) + " " + mushroom + " / " + CraftingService.MushroomCost + "\n" +
                    DisplayName(CraftingService.GunpowderItemId) + " " + powder + " / " + CraftingService.GunpowderCost + "\n" +
                    "가방 재료를 먼저 쓰고 모자란 만큼 창고에서 가져옵니다.";
            }
            if (ui.craftConfirm != null)
                ui.craftConfirm.interactable = craftingService.CanCraft(data.inventoryData, mushroomItemId, data.warehouseData) == CraftResult.Success;
        }

        // ---------- 동작 ----------
        private void BuyShopRow(int row)
        {
            if (shopPanel == null || !shopPanel.activeSelf) return;
            int index = shopPage * Mathf.Max(1, UsesHierarchyShop ? ShopRowCount : 6) + row;
            if (index < 0 || index >= shopItems.Count) return;
            bool success = new ShopService(catalog).Buy(PlayerData, shopItems[index].itemId);
            SetMessage(success ? "구매 완료" : "구매 불가: 지푸라기 또는 가방 공간 확인");
            Refresh();
        }

        private void SellSelectedShopItem()
        {
            bool success = !selectedShopEquipment && new ShopService(catalog).Sell(PlayerData, selectedShopBagIndex);
            SetMessage(success ? "1개 판매 완료" : "판매 불가: 아이템 선택 확인 (버섯/지푸라기 비매품)");
            Refresh();
        }

        // 가방이든 장비 칸이든 마지막으로 누른 칸 하나를 판매/수리 대상으로 쓴다.
        private GridSlotData SelectedSlot()
        {
            GridContainerData container = selectedShopEquipment ? PlayerData.equipmentSlots : PlayerData.inventory;
            return selectedShopBagIndex >= 0 && selectedShopBagIndex < container.slots.Count
                ? container.slots[selectedShopBagIndex] : null;
        }

        private void RepairSelectedWeapon()
        {
            bool success = WeaponDurability.Repair(PlayerData, SelectedSlot());
            SetMessage(success ? "1G 수리 완료" : "수리 불가: 마모된 총기를 고르고 지푸라기가 있어야 합니다.");
            Refresh();
        }

        // ---------- 하이어라키 참조가 없을 때만 쓰는 예전 코드 생성 ----------
        private void BuildShop()
        {
            shopPanel = MakePanel("ShopPanel", RightX, HeaderBottom, ColumnWidth, SidePanelHeight, "까마귀 상점 (임시)");
            MakeLabel(shopPanel.transform, "구매/판매 동일 가격 · 탄약 가격은 1박스 기준", 16, 45, 420, 28, 13);
            for (int index = 0; index < 6; index++)
            {
                int row = index;
                shopLabels.Add(MakeLabel(shopPanel.transform, "", 20, 82 + index * 49, 240, 36, 14));
                MakeButton(shopPanel.transform, "구매", 275, 82 + index * 49, 140, 34, () => BuyShopRow(row));
            }
            MakeButton(shopPanel.transform, "이전", 20, 388, 180, 32, () => { shopPage = Mathf.Max(0, shopPage - 1); RefreshShop(); });
            MakeButton(shopPanel.transform, "다음", 225, 388, 190, 32, () => { shopPage = Mathf.Min((shopItems.Count - 1) / 6, shopPage + 1); RefreshShop(); });
            shopSelection = MakeLabel(shopPanel.transform, "가방 아이템 선택 → 판매/수리", 20, 430, 400, 46, 14);
            MakeButton(shopPanel.transform, "선택 아이템 1개 판매", 20, 484, 210, 34, SellSelectedShopItem);
            MakeButton(shopPanel.transform, "선택 총기 수리 1G", 238, 484, 180, 34, RepairSelectedWeapon);
        }

        private void RefreshLegacyShop()
        {
            for (int index = 0; index < shopLabels.Count; index++)
            {
                int itemIndex = shopPage * 6 + index;
                shopLabels[index].text = itemIndex < shopItems.Count ? shopItems[itemIndex].displayName + "  " + shopItems[itemIndex].price + "G" : "-";
            }
            GridSlotData slot = selectedShopBagIndex >= 0 && selectedShopBagIndex < PlayerData.inventory.slots.Count ? PlayerData.inventory.slots[selectedShopBagIndex] : null;
            shopSelection.text = "보유 " + PlayerData.currency + "G   " + (shopPage + 1) + "/" + ((shopItems.Count + 5) / 6) + "쪽\n" +
                (slot != null && !slot.IsEmpty() ? DisplayName(slot.itemId) + " 선택됨" : "가방에서 판매/수리할 아이템 선택");
        }
    }
}
