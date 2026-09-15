using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

// UI 4종 총괄. 왼쪽 장비·가방, 오른쪽 외부 컨테이너, 하단 퀵슬롯.
// 데이터는 하나의 PlayerSaveData 를 쓰고 외부 컨테이너만 대상별로 갈아끼운다.
namespace Birdkov.NaYeongMin.Ui
{
    public sealed class InventoryUiController : MonoBehaviour, IInventoryUiHost
    {
        [Header("데이터")]
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private InventoryUiTheme theme;
        [SerializeField] private ItemIconTable iconTable;

        [Header("화면")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private ContainerGridView bagView;
        [SerializeField] private ContainerGridView equipmentView;
        [SerializeField] private ContainerGridView weaponQuickView;
        [SerializeField] private ContainerGridView itemQuickView;
        [SerializeField] private GameObject externalPanel;
        [SerializeField] private ContainerGridView externalView;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text currencyLabel;
        [SerializeField] private ItemTooltipView tooltip;
        [SerializeField] private DragGhostView dragGhost;

        [Header("상호작용")]
        [SerializeField] private Transform playerAnchor;
        [SerializeField, Min(0.5f)] private float interactionDistance = 2.5f;

        private PlayerInventoryService playerService;
        private InventoryService gridService;
        private WarehouseService warehouseService;
        private CraftingService craftingService;
        private IItemCatalog catalog;
        private ItemSlotView dragSource;
        private float messageUntil;

        public PlayerSaveData SaveData { get; private set; }
        public PlayerInventoryData PlayerData => SaveData != null ? SaveData.inventoryData : null;
        public PlayerInventoryService PlayerService => playerService;
        public CraftingService Crafting => craftingService;
        public IContainerSource ActiveExternal { get; private set; }
        public bool IsOpen => rootPanel != null && rootPanel.activeSelf;
        public bool IsReady => SaveData != null && playerService != null;

        private void Awake()
        {
            Initialize();
        }

        // 세이브가 없으면 새 데이터로 시작한다. 로드한 데이터는 AttachSaveData 로 갈아끼운다.
        public void Initialize()
        {
            if (IsReady || itemDatabase == null)
            {
                return;
            }

            catalog = itemDatabase.Catalog;
            if (catalog == null)
            {
                Debug.LogError("InventoryUiController: ItemDatabase 가 아직 로드되지 않았습니다.", this);
                return;
            }

            playerService = new PlayerInventoryService(catalog);
            gridService = new InventoryService(catalog);
            craftingService = new CraftingService(catalog);
            SaveData = new PlayerSaveData();
            warehouseService = new WarehouseService(catalog, SaveData.warehouseData);

            foreach (ContainerGridView view in AllViews())
            {
                if (view != null)
                {
                    view.Initialize(this, theme);
                }
            }

            if (tooltip != null)
            {
                tooltip.ApplyTheme(theme);
                tooltip.Hide();
            }

            if (dragGhost != null)
            {
                dragGhost.Hide();
            }

            BindPlayerViews();
            if (rootPanel != null)
            {
                rootPanel.SetActive(false);
            }
        }

        // 세이브 로드 결과를 연결한다. 서비스와 화면이 같은 데이터를 가리키게 맞춘다.
        public void AttachSaveData(PlayerSaveData data)
        {
            if (data == null || !IsReady)
            {
                return;
            }

            SaveData = data;
            warehouseService = new WarehouseService(catalog, SaveData.warehouseData);
            BindPlayerViews();
            Refresh();
        }

        private IEnumerable<ContainerGridView> AllViews()
        {
            yield return bagView;
            yield return equipmentView;
            yield return weaponQuickView;
            yield return itemQuickView;
            yield return externalView;
        }

        private void BindPlayerViews()
        {
            if (bagView != null)
            {
                bagView.Bind(PlayerData.inventory, InventorySettings.InventoryWidth, InventorySettings.InventoryHeight, "가방");
            }

            if (equipmentView != null)
            {
                equipmentView.Bind(PlayerData.equipmentSlots, InventorySettings.EquipmentSlotCount, 1, "장비");
            }

            if (weaponQuickView != null)
            {
                weaponQuickView.Bind(null, InventorySettings.WeaponQuickSlotCount, 1, "무기");
            }

            if (itemQuickView != null)
            {
                itemQuickView.Bind(null, InventorySettings.ItemQuickSlotCount, 1, "아이템");
            }
        }

        // ---------- 열기 / 닫기 ----------

        public void OpenInventory()
        {
            if (!IsReady)
            {
                return;
            }

            SetExternal(null);
            SetOpen(true);
        }

        public void OpenStorage(IContainerSource storageContext)
        {
            OpenExternal(storageContext, ExternalContainerKind.Storage);
        }

        public void OpenLoot(IContainerSource lootContext)
        {
            OpenExternal(lootContext, ExternalContainerKind.Loot);
        }

        public void OpenMapChest(IContainerSource chestContext)
        {
            OpenExternal(chestContext, ExternalContainerKind.MapChest);
        }

        public void CloseCurrent()
        {
            SetExternal(null);
            SetOpen(false);
            EndDrag();
            if (tooltip != null)
            {
                tooltip.Hide();
            }
        }

        // F 입력 진입점. 같은 대상이 이미 열려 있으면 닫고, 아니면 연다.
        // 닫기가 아이템 획득보다 먼저 판정되므로 F 로 줍는 일은 없다.
        public bool ToggleExternal(IContainerSource source)
        {
            if (source == null)
            {
                return false;
            }

            if (IsOpen && ReferenceEquals(ActiveExternal, source))
            {
                CloseCurrent();
                return false;
            }

            OpenExternal(source, source.Kind);
            return IsOpen;
        }

        private void OpenExternal(IContainerSource source, ExternalContainerKind expected)
        {
            if (!IsReady || source == null)
            {
                return;
            }

            if (source.Kind != expected)
            {
                Debug.LogWarning("InventoryUiController: 컨테이너 종류가 맞지 않습니다. " + source.Kind + " != " + expected, this);
                return;
            }

            source.EnsureContents(catalog);
            if (source.Container == null)
            {
                SetMessage("컨테이너 내용이 준비되지 않았습니다.");
                return;
            }

            SetExternal(source);
            SetOpen(true);
        }

        // 외부 컨테이너는 한 번에 하나만 활성화한다.
        private void SetExternal(IContainerSource source)
        {
            if (ActiveExternal is StorageContainer && warehouseService != null)
            {
                warehouseService.Close();
            }

            ActiveExternal = source;

            if (externalPanel != null)
            {
                externalPanel.SetActive(source != null);
            }

            if (externalView == null)
            {
                return;
            }

            if (source == null)
            {
                externalView.Clear();
                return;
            }

            GridContainerData data = source.Container;
            externalView.Bind(data, Mathf.Max(1, data.width), Mathf.Max(1, data.height), source.Title);
            if (source is StorageContainer && warehouseService != null)
            {
                warehouseService.Open();
            }
        }

        private void SetOpen(bool open)
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(open);
            }

            if (open)
            {
                Refresh();
            }
        }

        // ---------- 상태 유지 ----------

        private void Update()
        {
            if (!IsOpen || ActiveExternal == null)
            {
                return;
            }

            bool lost = !ActiveExternal.IsAvailable || ActiveExternal.Container == null;
            if (!lost && playerAnchor != null && ActiveExternal.Anchor != null)
            {
                lost = Vector3.Distance(playerAnchor.position, ActiveExternal.Anchor.position) > interactionDistance;
            }

            if (lost)
            {
                SetMessage("대상에서 멀어져 창을 닫았습니다.");
                CloseCurrent();
            }
        }

        // ---------- 슬롯 이벤트 ----------

        public void OnSlotClicked(ItemSlotView slot, bool splitModifier)
        {
            if (!IsReady || slot == null || slot.Owner == null)
            {
                return;
            }

            switch (slot.Owner.Kind)
            {
                case UiContainerKind.External:
                    TakeFromExternal(slot.Index, !splitModifier);
                    break;
                case UiContainerKind.ItemQuick:
                    UseItemQuickSlot(slot.Index);
                    break;
                case UiContainerKind.WeaponQuick:
                    SelectedWeaponSlot = slot.Index;
                    break;
                case UiContainerKind.Bag:
                    UseBagItem(slot.Index);
                    break;
            }

            Refresh();
        }

        public int SelectedWeaponSlot { get; private set; }

        public void OnSlotDragBegin(ItemSlotView slot)
        {
            if (!IsReady || slot == null || slot.IsEmpty)
            {
                return;
            }

            dragSource = slot;
            if (dragGhost != null)
            {
                Sprite icon = IconFor(ItemIdAt(slot));
                dragGhost.Show(icon, string.Empty, Input.mousePosition);
            }
        }

        public void OnSlotDragMoved(Vector2 screenPosition)
        {
            if (dragGhost != null)
            {
                dragGhost.Move(screenPosition);
            }
        }

        public void OnSlotDragEnd()
        {
            EndDrag();
            Refresh();
        }

        public void OnSlotDropped(ItemSlotView target)
        {
            if (!IsReady || dragSource == null || target == null || target == dragSource)
            {
                EndDrag();
                return;
            }

            Transfer(dragSource, target);
            EndDrag();
            Refresh();
        }

        public void OnSlotHoverEnter(ItemSlotView slot)
        {
            if (tooltip == null || slot == null)
            {
                return;
            }

            int itemId = ItemIdAt(slot);
            ItemData item;
            if (itemId < 0 || catalog == null || !catalog.TryGetItem(itemId, out item))
            {
                tooltip.Hide();
                return;
            }

            tooltip.Show(item.displayName, item.description, Input.mousePosition);
        }

        public void OnSlotHoverExit(ItemSlotView slot)
        {
            if (tooltip != null)
            {
                tooltip.Hide();
            }
        }

        private void EndDrag()
        {
            dragSource = null;
            if (dragGhost != null)
            {
                dragGhost.Hide();
            }
        }

        // ---------- 이동 ----------

        private void Transfer(ItemSlotView from, ItemSlotView to)
        {
            GridContainerData source = ContainerOf(from);
            GridContainerData destination = ContainerOf(to);
            if (source == null || destination == null)
            {
                SetMessage("퀵슬롯에는 직접 옮길 수 없습니다.");
                return;
            }

            if (ReferenceEquals(source, PlayerData.equipmentSlots) || ReferenceEquals(destination, PlayerData.equipmentSlots))
            {
                TransferEquipment(from, to);
                return;
            }

            InventoryMoveResult result = gridService.MoveItem(source, from.Index, destination, to.Index, int.MaxValue);
            ReportMove(result);
            NotifyExternalChanged(source, destination);
        }

        private void TransferEquipment(ItemSlotView from, ItemSlotView to)
        {
            bool toEquipment = to.Owner.Kind == UiContainerKind.Equipment;
            bool fromEquipment = from.Owner.Kind == UiContainerKind.Equipment;

            if (toEquipment && from.Owner.Kind == UiContainerKind.Bag)
            {
                ReportMove(playerService.EquipFromInventory(PlayerData, from.Index, to.Index));
                return;
            }

            if (fromEquipment && to.Owner.Kind == UiContainerKind.Bag)
            {
                ReportMove(playerService.UnequipToInventory(PlayerData, from.Index, to.Index));
                return;
            }

            SetMessage("장비 슬롯은 가방과만 주고받습니다.");
        }

        private void TakeFromExternal(int index, bool takeAll)
        {
            if (ActiveExternal == null || ActiveExternal.Container == null)
            {
                return;
            }

            GridContainerData source = ActiveExternal.Container;
            if (index < 0 || index >= source.slots.Count || source.slots[index].IsEmpty())
            {
                return;
            }

            int itemId = source.slots[index].itemId;
            int amount = takeAll ? source.slots[index].amount : 1;

            ItemData item;
            if (catalog.TryGetItem(itemId, out item) && item.itemType == ItemType.Currency)
            {
                PlayerData.currency += amount;
                gridService.RemoveItem(source, index, amount);
                ActiveExternal.NotifyContentsChanged();
                SetMessage(item.displayName + " " + amount + " 획득");
                return;
            }

            InventoryMoveResult result = gridService.MoveItem(source, index, PlayerData.inventory, FirstFreeOrSame(itemId), amount);
            ReportMove(result);
            if (result.MovedAmount > 0)
            {
                ActiveExternal.NotifyContentsChanged();
            }
        }

        // 같은 아이템이 쌓인 칸을 우선, 없으면 빈 칸. 둘 다 없으면 0번을 돌려주고 서비스가 거부한다.
        private int FirstFreeOrSame(int itemId)
        {
            List<GridSlotData> slots = PlayerData.inventory.slots;
            for (int index = 0; index < slots.Count; index++)
            {
                if (slots[index].itemId == itemId && slots[index].remainingRounds == 0)
                {
                    return index;
                }
            }

            for (int index = 0; index < slots.Count; index++)
            {
                if (slots[index].IsEmpty())
                {
                    return index;
                }
            }

            return 0;
        }

        private void UseBagItem(int index)
        {
            ItemData item;
            if (!TryGetBagItem(index, out item))
            {
                return;
            }

            if (item.itemType == ItemType.Consumable)
            {
                SetMessage(playerService.UseRecoveryItem(PlayerData, index, RecoveryTarget) == InventoryResult.Success
                    ? item.displayName + " 사용"
                    : "지금은 사용할 수 없습니다.");
            }
        }

        public IRecoveryTarget RecoveryTarget { get; set; }

        private void UseItemQuickSlot(int quickIndex)
        {
            int bagIndex;
            ItemData item;
            if (playerService.TryGetItemQuickSlot(PlayerData, quickIndex, out bagIndex, out item))
            {
                UseBagItem(bagIndex);
            }
        }

        private bool TryGetBagItem(int index, out ItemData item)
        {
            item = null;
            List<GridSlotData> slots = PlayerData.inventory.slots;
            return index >= 0 && index < slots.Count && !slots[index].IsEmpty() &&
                   catalog.TryGetItem(slots[index].itemId, out item);
        }

        private void NotifyExternalChanged(GridContainerData source, GridContainerData destination)
        {
            if (ActiveExternal == null)
            {
                return;
            }

            GridContainerData external = ActiveExternal.Container;
            if (ReferenceEquals(source, external) || ReferenceEquals(destination, external))
            {
                ActiveExternal.NotifyContentsChanged();
            }
        }

        private GridContainerData ContainerOf(ItemSlotView slot)
        {
            if (slot == null || slot.Owner == null)
            {
                return null;
            }

            switch (slot.Owner.Kind)
            {
                case UiContainerKind.Bag: return PlayerData.inventory;
                case UiContainerKind.Equipment: return PlayerData.equipmentSlots;
                case UiContainerKind.External: return ActiveExternal != null ? ActiveExternal.Container : null;
                default: return null;
            }
        }

        private int ItemIdAt(ItemSlotView slot)
        {
            if (slot == null || slot.Owner == null)
            {
                return -1;
            }

            if (slot.Owner.Kind == UiContainerKind.WeaponQuick)
            {
                int equipIndex;
                ItemData weapon;
                return playerService.TryGetWeaponQuickSlot(PlayerData, slot.Index, out equipIndex, out weapon) ? weapon.itemId : -1;
            }

            if (slot.Owner.Kind == UiContainerKind.ItemQuick)
            {
                int bagIndex;
                ItemData item;
                return playerService.TryGetItemQuickSlot(PlayerData, slot.Index, out bagIndex, out item) ? item.itemId : -1;
            }

            GridContainerData container = ContainerOf(slot);
            if (container == null || slot.Index < 0 || slot.Index >= container.slots.Count)
            {
                return -1;
            }

            return container.slots[slot.Index].itemId;
        }

        // ---------- 표시 ----------

        public void Refresh()
        {
            if (!IsReady || !IsOpen)
            {
                return;
            }

            playerService.SanitizeItemQuickSlots(PlayerData);
            RefreshGrid(bagView, PlayerData.inventory);
            RefreshGrid(equipmentView, PlayerData.equipmentSlots);
            RefreshQuick(weaponQuickView);
            RefreshQuick(itemQuickView);
            RefreshGrid(externalView, ActiveExternal != null ? ActiveExternal.Container : null);

            if (currencyLabel != null)
            {
                currencyLabel.text = PlayerData.currency.ToString();
            }

            if (statusLabel != null && Time.unscaledTime > messageUntil)
            {
                statusLabel.text = string.Empty;
            }
        }

        private void RefreshGrid(ContainerGridView view, GridContainerData data)
        {
            if (view == null)
            {
                return;
            }

            for (int index = 0; index < view.SlotCount; index++)
            {
                if (data == null || index >= data.slots.Count)
                {
                    view.SetSlotContent(index, null, string.Empty, string.Empty);
                    continue;
                }

                GridSlotData slot = data.slots[index];
                ItemData item = null;
                bool found = !slot.IsEmpty() && catalog.TryGetItem(slot.itemId, out item);
                view.SetSlotContent(
                    index,
                    found ? IconFor(slot.itemId) : null,
                    found ? AmountText(slot) : string.Empty,
                    found ? item.displayName : string.Empty);
            }
        }

        private void RefreshQuick(ContainerGridView view)
        {
            if (view == null)
            {
                return;
            }

            for (int index = 0; index < view.SlotCount; index++)
            {
                int itemId = -1;
                ItemData item = null;
                if (view.Kind == UiContainerKind.WeaponQuick)
                {
                    int equipIndex;
                    if (playerService.TryGetWeaponQuickSlot(PlayerData, index, out equipIndex, out item))
                    {
                        itemId = item.itemId;
                    }
                }
                else
                {
                    int bagIndex;
                    if (playerService.TryGetItemQuickSlot(PlayerData, index, out bagIndex, out item))
                    {
                        itemId = item.itemId;
                    }
                }

                view.SetSlotContent(index, itemId >= 0 ? IconFor(itemId) : null, string.Empty,
                    item != null ? item.displayName : string.Empty);

                if (view.Kind == UiContainerKind.WeaponQuick && theme != null)
                {
                    view.SetSlotTint(index, index == SelectedWeaponSlot ? theme.DropAllowedColor : theme.NormalColor);
                }
            }
        }

        private static string AmountText(GridSlotData slot)
        {
            if (slot.remainingRounds > 0)
            {
                return slot.amount + " (" + slot.remainingRounds + ")";
            }

            return slot.amount > 1 ? slot.amount.ToString() : string.Empty;
        }

        private Sprite IconFor(int itemId)
        {
            return iconTable != null ? iconTable.GetIcon(itemId) : null;
        }

        private void ReportMove(InventoryMoveResult result)
        {
            switch (result.Result)
            {
                case InventoryResult.Success:
                    return;
                case InventoryResult.Partial:
                    SetMessage("일부만 옮겼습니다. 남은 수량 " + result.RemainingAmount);
                    return;
                case InventoryResult.DestinationFull:
                    SetMessage("가방이 꽉 찼습니다.");
                    return;
                default:
                    SetMessage("옮길 수 없습니다.");
                    return;
            }
        }

        private void SetMessage(string text)
        {
            messageUntil = Time.unscaledTime + 3f;
            if (statusLabel != null)
            {
                statusLabel.text = text;
            }
        }
    }
}
