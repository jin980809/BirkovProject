using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;
using UnityEngine.UI;

namespace Birdkov.NaYeongMin.InventoryTest
{
    // 칸 선택 표시 / 좌클릭 상세 / 우클릭 메뉴.
    public sealed partial class InventoryTestBench
    {
        private TestContainer selectedContainer;
        private int selectedIndex = -1;
        private TestSlotView contextSlot;

        private static Transform FindChild(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child != root && child.name == name) return child;
            return null;
        }

        private void BindItemMenu()
        {
            if (ui == null || screen == null) return;
            if (ui.itemDetailPanel == null)
            {
                Transform found = screen.Find("ItemDetailPanel");
                if (found != null)
                {
                    ui.itemDetailPanel = found.gameObject;
                    if (ui.itemDetailIcon == null && FindChild(found, "Icon") != null) ui.itemDetailIcon = FindChild(found, "Icon").GetComponent<Image>();
                    if (ui.itemDetailTitle == null && FindChild(found, "Title") != null) ui.itemDetailTitle = FindChild(found, "Title").GetComponent<Text>();
                    if (ui.itemDetailBody == null && FindChild(found, "Body") != null) ui.itemDetailBody = FindChild(found, "Body").GetComponent<Text>();
                    if (ui.itemDetailClose == null && FindChild(found, "Button_닫기") != null) ui.itemDetailClose = FindChild(found, "Button_닫기").GetComponent<Button>();
                }
            }
            if (ui.contextMenu == null)
            {
                Transform found = screen.Find("ContextMenu");
                if (found != null)
                {
                    ui.contextMenu = found as RectTransform;
                    if (ui.contextUse == null && FindChild(found, "Button_사용") != null) ui.contextUse = FindChild(found, "Button_사용").GetComponent<Button>();
                    if (ui.contextDrop == null && FindChild(found, "Button_버리기") != null) ui.contextDrop = FindChild(found, "Button_버리기").GetComponent<Button>();
                }
            }

            Bind(ui.itemDetailClose, CloseItemDetail);
            Bind(ui.contextUse, UseContextSlot);
            Bind(ui.contextDrop, DropContextSlot);
            CloseItemDetail();
            HideContextMenu();
        }

        public void SelectSlot(TestSlotView slot)
        {
            if (slot == null) return;
            selectedContainer = slot.container;
            selectedIndex = slot.index;
            ShowItemDetail(slot);
            Refresh();
        }

        // 더블클릭으로만 옮긴다. 한 번 클릭은 선택 + 상세 표시.
        public void DoubleClickSlot(TestSlotView slot)
        {
            if (!IsReady || slot == null) return;
            HideContextMenu();
            if (slot.container == TestContainer.Loot || slot.container == TestContainer.Warehouse) Take(slot.container, slot.index);
            else if (slot.container == TestContainer.Bag) SendFromBag(slot.index);
        }

        private void SendFromBag(int bagIndex)
        {
            TestContainer target;
            if (warehousePanel != null && warehousePanel.activeSelf && warehouseService != null && warehouseService.IsOpen)
                target = TestContainer.Warehouse;
            else if ((lootPanel != null && lootPanel.activeSelf) || (mapChestPanel != null && mapChestPanel.activeSelf))
                target = TestContainer.Loot;
            else { SetMessage("보낼 창고나 상자가 열려 있지 않습니다."); return; }

            GridContainerData source = PlayerData.inventory;
            GridSlotData slot = source.slots[bagIndex];
            if (slot.IsEmpty()) return;
            GridContainerData destination = GetContainer(target);
            int moved = 0;
            for (int pass = 0; pass < 2 && !slot.IsEmpty(); pass++)
            for (int i = 0; i < destination.slots.Count && !slot.IsEmpty(); i++)
            {
                GridSlotData dst = destination.slots[i];
                if (pass == 0 ? dst.IsEmpty() || dst.itemId != slot.itemId : !dst.IsEmpty()) continue;
                moved += inventoryService.MoveItem(source, bagIndex, destination, i, slot.amount).MovedAmount;
            }
            SetMessage(moved > 0 ? moved + "개 보냄" : "보낼 공간이 없습니다.");
            if (target == TestContainer.Loot) NotifyLootChanged();
            CloseItemDetail();
            Refresh();
        }

        public void ClearSelection()
        {
            selectedIndex = -1;
            CloseItemDetail();
            HideContextMenu();
        }

        private bool ResolveSlotItem(TestSlotView slot, out int itemId, out int amount)
        {
            itemId = -1; amount = 0;
            if (slot == null || !IsReady) return false;
            if (slot.container == TestContainer.WeaponQuick || slot.container == TestContainer.ItemQuick)
                ResolveQuick(slot.container, slot.index, out itemId, out amount);
            else
            {
                GridContainerData container = GetContainer(slot.container);
                if (slot.index < 0 || slot.index >= container.slots.Count) return false;
                itemId = container.slots[slot.index].itemId;
                amount = container.slots[slot.index].amount;
            }
            return amount > 0;
        }

        private void ShowItemDetail(TestSlotView slot)
        {
            if (ui.itemDetailPanel == null) return;
            int itemId, amount;
            if (!ResolveSlotItem(slot, out itemId, out amount) || !catalog.TryGetItem(itemId, out ItemData item))
            {
                CloseItemDetail();
                return;
            }

            if (ui.itemDetailTitle != null) ui.itemDetailTitle.text = item.displayName;
            if (ui.itemDetailIcon != null)
            {
                Sprite sprite;
                ui.itemDetailIcon.sprite = spriteMap.TryGetValue(itemId, out sprite) ? sprite : null;
                ui.itemDetailIcon.enabled = ui.itemDetailIcon.sprite != null;
            }
            if (ui.itemDetailBody != null) ui.itemDetailBody.text = ItemDetailText(item, slot, amount);
            ui.itemDetailPanel.transform.SetAsLastSibling();
            ui.itemDetailPanel.SetActive(true);
        }

        private string ItemDetailText(ItemData item, TestSlotView slot, int amount)
        {
            string text = item.weight.ToString("0.##") + " kg    " + ItemTypeLabel(item.itemType) + "    " + amount + "개";
            if (!string.IsNullOrEmpty(item.description)) text += "\n" + item.description;
            if (WeaponDurability.Maximum(item.itemId) > 0)
            {
                GridContainerData container = GetContainer(slot.container);
                if (slot.index >= 0 && slot.index < container.slots.Count)
                    text += "\n내구도 " + WeaponDurability.Remaining(container.slots[slot.index]) + " / " + WeaponDurability.Maximum(item.itemId);
            }
            if (item.healthRecovery != 0 || item.hungerRecovery != 0 || item.waterRecovery != 0)
                text += "\n효과  체력 " + item.healthRecovery + "  허기 " + item.hungerRecovery + "  수분 " + item.waterRecovery;
            if (item.price > 0) text += "\n가격 " + item.price + "G";
            return text;
        }

        public void CloseItemDetail()
        {
            if (ui != null && ui.itemDetailPanel != null) ui.itemDetailPanel.SetActive(false);
        }

        public void RightClickSlot(TestSlotView slot)
        {
            if (!IsReady || ui == null || ui.contextMenu == null) return;
            int itemId, amount;
            if (!ResolveSlotItem(slot, out itemId, out amount)) { HideContextMenu(); return; }

            contextSlot = slot;
            SelectSlot(slot);
            bool inUsableContainer = slot.container == TestContainer.Bag ||
                slot.container == TestContainer.Warehouse || slot.container == TestContainer.Loot;
            bool usable = inUsableContainer && catalog.TryGetItem(itemId, out ItemData item) &&
                (item.itemType == ItemType.Consumable || item.itemType == ItemType.Special);
            bool droppable = slot.container == TestContainer.Bag || slot.container == TestContainer.Warehouse ||
                slot.container == TestContainer.Loot;
            if (!usable && !droppable) { HideContextMenu(); return; }
            if (ui.contextUse != null) ui.contextUse.gameObject.SetActive(usable);
            if (ui.contextDrop != null) ui.contextDrop.gameObject.SetActive(droppable);

            ui.contextMenu.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(ui.contextMenu);

            Vector3 world;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(screen, Input.mousePosition, null, out world))
                ui.contextMenu.position = world;
            Vector2 size = ui.contextMenu.rect.size;
            Vector2 pos = ui.contextMenu.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, 0f, Mathf.Max(0f, screen.rect.width - size.x));
            pos.y = Mathf.Clamp(pos.y, -Mathf.Max(0f, screen.rect.height - size.y), 0f);
            ui.contextMenu.anchoredPosition = pos;
            ui.contextMenu.SetAsLastSibling();
        }

        public void HideContextMenu()
        {
            if (ui != null && ui.contextMenu != null) ui.contextMenu.gameObject.SetActive(false);
            contextSlot = null;
        }

        private void UseContextSlot()
        {
            TestSlotView slot = contextSlot;
            HideContextMenu();
            if (slot == null) return;
            if (slot.container == TestContainer.Bag) { UseBagItem(slot.index); CloseItemDetail(); return; }

            // 상자나 창고 칸은 한 개만 가방으로 옮긴 뒤 그 자리에서 쓴다.
            GridContainerData source = GetContainer(slot.container);
            if (slot.index < 0 || slot.index >= source.slots.Count || source.slots[slot.index].IsEmpty()) return;
            int itemId = source.slots[slot.index].itemId;
            if (playerService.AddToInventory(PlayerData, itemId, 1).MovedAmount <= 0)
            {
                SetMessage("가방이 꽉 찼습니다.");
                return;
            }

            inventoryService.RemoveItem(source, slot.index, 1);
            for (int i = 0; i < PlayerData.inventory.slots.Count; i++)
                if (PlayerData.inventory.slots[i].itemId == itemId) { UseBagItem(i); break; }
            if (slot.container == TestContainer.Loot) NotifyLootChanged();
            CloseItemDetail();
            Refresh();
        }

        private void DropContextSlot()
        {
            TestSlotView slot = contextSlot;
            HideContextMenu();
            if (slot == null) return;
            GridContainerData container = GetContainer(slot.container);
            if (slot.index < 0 || slot.index >= container.slots.Count) return;
            GridSlotData target = container.slots[slot.index];
            if (target.IsEmpty()) return;
            SetMessage(DisplayName(target.itemId) + " 버림");
            target.Clear();
            if (slot.container == TestContainer.Loot) NotifyLootChanged();
            CloseItemDetail();
            Refresh();
        }
    }
}
