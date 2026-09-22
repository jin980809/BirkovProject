using System;
using System.Collections.Generic;
using System.IO;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.Rng;
using Birdkov.NaYeongMin.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

// 인벤토리 기능을 눈으로 확인하는 테스트 UI. 본 게임 UI 가 아니다.
namespace Birdkov.NaYeongMin.InventoryTest
{
    [Serializable]
    public class TestIconBinding
    {
        public int itemId;
        public Sprite sprite;
    }

    // 하이어라키에 미리 만든 UI 를 벤치에 연결하는 참조 묶음. 모두 인스펙터에서 갈아끼운다.
    [Serializable]
    public class InventoryUiReferences
    {
        public RectTransform screen;
        public GameObject lootPanel;
        public GameObject mapChestPanel;
        public GameObject warehousePanel;
        public GameObject craftPanel;
        public RectTransform lootGridRoot;
        public RectTransform mapChestGridRoot;
        public RectTransform warehouseGridRoot;
        public Text status;
        public Text statsText;
        public Text hint;
        public Image dragIcon;
        public RectTransform itemPopup;
        public Text itemPopupText;
        public Text[] craftLabels;
        public Button[] craftButtons;

        // 상점/제작 - 기획서 5.1 / 5.3 임시 UI. 전부 하이어라키에서 만들어 연결한다.
        // 비워 두면 벤치가 예전처럼 코드로 패널을 만든다.
        public GameObject shopPanel;
        public Text shopCurrency;
        public Image[] shopRowBackgrounds;
        public Image[] shopRowIcons;
        public Text[] shopRowLabels;
        public Button[] shopRowButtons;
        public Button shopPrev;
        public Button shopNext;
        public Image shopDetailIcon;
        public Text shopDetail;
        public Button shopBuy;
        public Button shopSell;

        [Header("수리대")]
        public GameObject repairPanel;
        public Text repairCurrency;
        public Image repairDetailIcon;
        public Text repairDetail;
        public Button repairButton;
        public Image[] craftRowBackgrounds;
        public Image[] craftRowIcons;
        public Image craftDetailIcon;
        public Text craftDetail;
        public Button craftConfirm;

        [Header("아이템 상세 / 우클릭 메뉴")]
        public GameObject itemDetailPanel;
        public Image itemDetailIcon;
        public Text itemDetailTitle;
        public Text itemDetailBody;
        public Button itemDetailClose;
        public RectTransform contextMenu;
        public Button contextUse;
        public Button contextDrop;

        [Header("창고 단독 열기")]
        public GameObject warehouseSoloScroll;
        public RectTransform warehouseSoloContent;
        public RectTransform inventoryColumn;
    }

    // UI 색. 전부 인스펙터에서 바꾼다. 코드는 기본값만 들고 있다.
    [Serializable]
    public class InventoryUiColors
    {
        [Header("칸 배경")]
        public Color slotEmpty = new Color(0.17f, 0.18f, 0.19f, 0.9f);
        public Color slotFilled = new Color(0.24f, 0.25f, 0.26f, 0.95f);
        public Color slotEquipment = new Color(0.2f, 0.2f, 0.24f, 0.95f);
        public Color slotWeaponQuick = new Color(0.18f, 0.22f, 0.26f, 0.9f);
        public Color slotSelectedWeapon = new Color(0.16f, 0.45f, 0.42f, 0.95f);
        public Color slotCraftMaterial = new Color(0.2f, 0.65f, 0.3f, 0.95f);
        public Color slotSelected = new Color(0.25f, 0.72f, 0.36f, 1f);

        [Header("상점·제작 목록 행")]
        public Color rowReady = new Color(0.24f, 0.48f, 0.29f, 0.95f);
        public Color rowBlocked = new Color(0.37f, 0.17f, 0.17f, 0.95f);
        public Color rowReadySelected = new Color(0.36f, 0.66f, 0.42f, 1f);
        public Color rowBlockedSelected = new Color(0.52f, 0.25f, 0.25f, 1f);
        public Color craftLabelReady = new Color(0.65f, 0.92f, 0.7f);
        public Color craftLabelBlocked = new Color(0.62f, 0.64f, 0.66f);
    }

    // 인벤토리 계열 기능을 육안으로 확인하는 테스트 벤치.
    [RequireComponent(typeof(Canvas))]
    public sealed partial class InventoryTestBench : MonoBehaviour, IRecoveryTarget
    {
        public TextAsset itemCsv;
        public TextAsset dropCsv;
        public Font font;
        [Tooltip("단독 테스트에서만 사용. 팀원 입력 이벤트 연결 시 끌 것.")]
        public bool useStandaloneKeyboard = true;
        [Tooltip("프리팹 기반 새 UI 로 전환하면 끈다. 끄면 이 벤치는 UI 를 만들지 않는다.")]
        public bool buildLegacyUi = true;
        [Tooltip("초기화 시 가방·장비·퀵슬롯을 비운다. 창고와 지푸라기는 유지한다.")]
        public bool startWithEmptyInventory;
        [Tooltip("DebugInventorySeeder가 초기 지급을 담당. 벤치 자체 가방/창고/화폐 지급을 생략한다.")]
        public bool useExternalTestSeeder;

        [Header("하이어라키 UI")]
        [Tooltip("비워 두면 예전처럼 코드로 UI 를 만든다. 채우면 그 오브젝트를 그대로 쓴다.")]
        public InventoryUiReferences ui = new InventoryUiReferences();

        [Header("UI 색")]
        public InventoryUiColors colors = new InventoryUiColors();

        [Header("HONETi 스킨")]
        public Sprite honetiPanelSprite;
        public Sprite honetiSlotSprite;
        public Sprite honetiTitleSprite;
        public Sprite honetiButtonSprite;
        public TestIconBinding[] icons = Array.Empty<TestIconBinding>();

        private const int BagCell = 76;
        private const int EquipCell = 80;
        private const int QuickCell = 68;
        private const int LootCell = 80;
        private const int WarehouseCell = 80;

        private ItemCatalog catalog;
        private InventoryService inventoryService;
        private PlayerInventoryService playerService;
        private WarehouseService warehouseService;
        private CraftingService craftingService;
        private JsonSaveSystem saves;
        private List<DropTableEntry> dropEntries = new List<DropTableEntry>();

        private PlayerSaveData data;
        public PlayerInventoryData PlayerData => data?.inventoryData;
        private LootContainerData loot = new LootContainerData();
        private LootContainerSize lootPreset = LootContainerSize.Box2x4;

        private readonly List<TestSlotView> views = new List<TestSlotView>();
        private readonly Dictionary<int, Sprite> spriteMap = new Dictionary<int, Sprite>();

        private RectTransform screen;
        private RectTransform itemPopup;
        private Text itemPopupText;
        private GameObject lootPanel, warehousePanel, craftPanel, mapChestPanel;
        private Text[] craftLabels;
        private RectTransform lootGridRoot, warehouseGridRoot, mapChestGridRoot;
        private Text status, statsText, hint;
        private Image dragIcon;
        private TestSlotView dragSource, hovered;
        private float messageUntil;

        private float health = 10, hunger = 10, water = 10;
        private int selectedWeapon;
        private IRecoveryTarget recoveryTarget;
        private string externalStats;
        private LootDropObject openedDrop;
        private InventoryWorldContainer openedContainer;
        private bool mapChestOpen;
        public bool IsOpen => screen != null && screen.gameObject.activeSelf;
        // PlayerInventoryBridge 가 거리가 멀어지면 닫기 위해 쓴다.
        public bool IsExternalOpen => IsOpen && (serviceAnchor != null || openedDrop != null || openedContainer != null);
        public Transform ExternalAnchor => serviceAnchor != null ? serviceAnchor : openedContainer != null ? openedContainer.transform :
            openedDrop != null ? openedDrop.transform : null;

        public void OpenInventory()
        {
            if (!IsReady || screen == null) return;
            CloseLoot();
            screen.gameObject.SetActive(true);
        }

        public void CloseCurrent()
        {
            if (screen == null) return;
            CloseLoot();
            screen.gameObject.SetActive(false);
        }

        public void OpenStorage(InventoryWorldContainer source)
        {
            if (!IsReady || source == null || !source.isActiveAndEnabled || source.kind != InventoryWorldKind.Storage) return;
            OpenInventory();
            openedContainer = source;
            warehouseService.Open();
            MoveWarehouse(true);
            warehousePanel.SetActive(true);
            Refresh();
        }

        public void OpenLoot(InventoryWorldContainer source)
        {
            OpenContainer(source, InventoryWorldKind.Loot);
        }

        public void OpenMapChest(InventoryWorldContainer source)
        {
            OpenContainer(source, InventoryWorldKind.MapChest);
        }

        private void OpenContainer(InventoryWorldContainer source, InventoryWorldKind expected)
        {
            if (!IsReady || source == null || !source.isActiveAndEnabled || source.kind != expected) return;
            OpenInventory();
            openedContainer = source;
            loot = source.GetContents(catalog);
            mapChestOpen = expected == InventoryWorldKind.MapChest;
            GameObject panel = mapChestOpen ? mapChestPanel : lootPanel;
            panel.SetActive(true);
            panel.GetComponentInChildren<Text>().text = source.displayName;
            BuildLootGrid();
            Refresh();
        }

        public bool IsReady => data != null && EnsureServices();

        public void BindRecoveryTarget(IRecoveryTarget target)
        {
            recoveryTarget = target;
            if (target == null) externalStats = null;
        }

        public void SetVitalsDisplay(string text)
        {
            externalStats = text;
            Refresh();
        }

        public void SelectWeapon(int index)
        {
            if (!IsReady || index < 0 || index >= InventorySettings.WeaponQuickSlotCount) return;
            selectedWeapon = index;
            Refresh();
        }

        public void ToggleInventory()
        {
            if (!IsReady || screen == null) return;
            if (IsOpen) CloseCurrent();
            else OpenInventory();
        }

        public void OpenLoot(LootDropObject drop)
        {
            if (!IsReady || drop == null || !drop.gameObject.activeInHierarchy) return;
            CloseLoot();
            openedDrop = drop;
            loot = drop.Loot;
            lootPreset = loot.sizePreset;
            warehouseService.Close();
            warehousePanel.SetActive(false);
            lootPanel.SetActive(true);
            screen.gameObject.SetActive(true);
            lootPanel.GetComponentInChildren<Text>().text = "전리품";
            BuildLootGrid();
            Refresh();
        }

        public void CloseLoot()
        {
            playerDeathOpen = false;
            if (playerDeathPanel != null) playerDeathPanel.SetActive(false);
            serviceAnchor = null;
            if (shopPanel != null) shopPanel.SetActive(false);
            openedDrop = null;
            openedContainer = null;
            mapChestOpen = false;
            if (warehouseService != null) warehouseService.Close();
            if (lootPanel != null) lootPanel.SetActive(false);
            if (mapChestPanel != null) mapChestPanel.SetActive(false);
            if (warehousePanel != null) warehousePanel.SetActive(false);
            if (craftPanel != null) craftPanel.SetActive(false);
            if (ui != null && ui.repairPanel != null) ui.repairPanel.SetActive(false);
            if (ui != null && ui.warehouseSoloScroll != null) ui.warehouseSoloScroll.SetActive(false);
            RebuildInventoryColumn();
            ClearSelection();
            loot = new LootContainerData(lootPreset);
            EndDrag();
            hovered = null;
            Refresh();
        }

        private string SaveDirectory => Path.Combine(Application.persistentDataPath, "NaYeongMinTestBench");

        private void Start()
        {
            // 새 UI 가 켜져 있으면 벤치는 화면을 만들지 않는다. UI 중복 생성 방지.
            if (!buildLegacyUi)
            {
                enabled = false;
                return;
            }

            if (itemCsv == null)
            {
                Debug.LogError("ItemData.csv 를 itemCsv 에 연결해야 한다.", this);
                enabled = false;
                return;
            }

            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Arial Unicode MS", "Arial" }, 16);
            }

            Initialize();
            if (!BindUi())
            {
                BuildUi();
            }

            ResetAll();
            OpenInventory();
        }

        private void Initialize()
        {
            List<ItemData> items = ItemCsvLoader.Parse(itemCsv.text);


            catalog = new ItemCatalog(items);
            WeaponDurability.Catalog = catalog; // 내구도 정본은 ItemData.csv
            inventoryService = new InventoryService(catalog);
            playerService = new PlayerInventoryService(catalog);
            craftingService = new CraftingService(catalog);
            saves = new JsonSaveSystem(SaveDirectory);

            if (dropCsv != null)
            {
                dropEntries = DropTableCsvLoader.Parse(dropCsv.text);
            }

            foreach (TestIconBinding binding in icons)
            {
                if (binding != null && binding.sprite != null)
                {
                    spriteMap[binding.itemId] = binding.sprite;
                }
            }

#if UNITY_EDITOR
            // 인스펙터 바인딩이 없는 아이템은 CSV 의 iconKey 경로에서 바로 가져온다.
            // 기획팀이 CSV 만 고쳐도 아이콘이 붙는다. 빌드에서는 인스펙터 바인딩만 쓴다.
            foreach (ItemData item in items)
            {
                if (spriteMap.ContainsKey(item.itemId) || string.IsNullOrEmpty(item.iconKey)) continue;
                Sprite csvSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(item.iconKey);
                if (csvSprite != null) spriteMap[item.itemId] = csvSprite;
            }
#endif
        }

        public void ResetAll()
        {
            openedDrop = null;
            EndDrag();
            data = new PlayerSaveData();
            warehouseService = new WarehouseService(catalog, data.warehouseData);
            loot = new LootContainerData(lootPreset);
            health = hunger = water = 10;
            selectedWeapon = 0;

            // 확장 테스트 지급을 쓰는 개인 씬에서는 이 기본 지급이 가방을 미리 채워 버려
            // 제작 결과와 구매 아이템을 넣을 칸이 남지 않는다. 같은 품목이 창고로 들어간다.
            if (!useExternalTestSeeder && !seedExtendedTestStock)
            {
                foreach (int id in new[] { 21001, 21002, 22001, 23001, 23002, 23003, 24002, 20001 })
                {
                    playerService.AddToInventory(data.inventoryData, id, id == 21001 ? 4 : 1);
                }

                // 무기 2종과 보호구 2종. 장비 슬롯 규칙을 눈으로 확인하는 용도다.
                foreach (int id in new[] { 10001, 10002, 13001, 12001 })
                {
                    playerService.AddToInventory(data.inventoryData, id, 1);
                }
            }

            if (!useExternalTestSeeder) inventoryService.AddItem(data.warehouseData, 23001, 5);
            if (!useExternalTestSeeder && seedExtendedTestStock) SeedExtendedTestStock();
            if (startWithEmptyInventory) playerService.ClearOnDeath(data.inventoryData);

            SetMessage(startWithEmptyInventory ? "초기화 완료. 가방·장비·퀵슬롯이 비어 있습니다." : "초기화 완료. 무기와 보호구는 가방에 들어갑니다. 자동 착용되지 않습니다. 지푸라기는 가방을 쓰지 않고 보유 수치로 들어갑니다.");
            Refresh();
        }

        // ---------- 컨테이너 접근 ----------
        private GridContainerData GetContainer(TestContainer container)
        {
            switch (container)
            {
                case TestContainer.Equipment: return data.inventoryData.equipmentSlots;
                case TestContainer.Loot: return loot.loot;
                case TestContainer.Warehouse: return data.warehouseData;
                default: return data.inventoryData.inventory;
            }
        }

        // 퀵슬롯은 컨테이너가 아니라 참조라 별도로 푼다.
        private bool ResolveQuick(TestContainer container, int index, out int itemId, out int amount)
        {
            itemId = -1;
            amount = 0;

            if (container == TestContainer.WeaponQuick)
            {
                if (!playerService.TryGetWeaponQuickSlot(data.inventoryData, index, out int equipIndex, out _))
                {
                    return false;
                }

                GridSlotData slot = data.inventoryData.equipmentSlots.slots[equipIndex];
                itemId = slot.itemId;
                amount = slot.amount;
                return true;
            }

            if (!playerService.TryGetItemQuickSlot(data.inventoryData, index, out int bagIndex, out _))
            {
                return false;
            }

            GridSlotData bagSlot = data.inventoryData.inventory.slots[bagIndex];
            itemId = bagSlot.itemId;
            amount = bagSlot.amount;
            return true;
        }

        // ---------- 드래그 ----------
        public void BeginDrag(TestSlotView slot)
        {
            if (slot.container == TestContainer.WeaponQuick)
            {
                SetMessage("무기 퀵슬롯은 장비 슬롯과 링크된 표시입니다. 직접 옮길 수 없습니다.");
                return;
            }

            dragSource = slot;
            if (itemPopup != null) itemPopup.gameObject.SetActive(false);
            if (dragIcon != null)
            {
                dragIcon.sprite = slot.icon.sprite;
                dragIcon.enabled = slot.icon.sprite != null;
                dragIcon.gameObject.SetActive(true);
            }
        }

        public void DragTo(Vector2 screenPosition)
        {
            if (dragIcon == null || dragSource == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                screen, screenPosition, null, out Vector2 local);
            dragIcon.rectTransform.anchoredPosition = local;
        }

        public void EndDrag()
        {
            dragSource = null;
            if (dragIcon != null)
            {
                dragIcon.gameObject.SetActive(false);
            }
        }

        public void DropOn(TestSlotView target)
        {
            if (dragSource == null || dragSource == target)
            {
                return;
            }

            TestContainer from = dragSource.container, to = target.container;
            int fromIndex = dragSource.index, toIndex = target.index;
            EndDrag();

            // 아이템 퀵슬롯은 가방 칸을 지정하거나 해제만 한다.
            if (to == TestContainer.ItemQuick)
            {
                if (from != TestContainer.Bag)
                {
                    SetMessage("퀵슬롯에는 가방 아이템만 지정할 수 있습니다.");
                }
                else
                {
                    bool assigned = playerService.AssignItemQuickSlot(data.inventoryData, toIndex, fromIndex);
                    SetMessage(assigned ? "아이템 퀵슬롯에 지정했습니다." : "소모형과 특수 아이템만 지정할 수 있습니다.");
                }

                Refresh();
                return;
            }

            if (from == TestContainer.ItemQuick)
            {
                playerService.ClearItemQuickSlot(data.inventoryData, fromIndex);
                SetMessage("퀵슬롯 지정을 해제했습니다.");
                Refresh();
                return;
            }

            if (to == TestContainer.WeaponQuick || from == TestContainer.WeaponQuick)
            {
                SetMessage("무기 퀵슬롯은 장비 슬롯을 그대로 비춥니다. 장비 슬롯에 무기를 넣어 보세요.");
                return;
            }

            InventoryMoveResult result = Transfer(from, fromIndex, to, toIndex);
            ReportResult(result, from, to);
            Refresh();
        }

        private InventoryMoveResult Transfer(TestContainer from, int fromIndex, TestContainer to, int toIndex)
        {
            const int all = int.MaxValue;

            if (to == TestContainer.Bag && (from == TestContainer.Loot || from == TestContainer.Warehouse))
            {
                GridContainerData source = GetContainer(from);
                if (fromIndex >= 0 && fromIndex < source.slots.Count &&
                    catalog.TryGetItem(source.slots[fromIndex].itemId, out ItemData item) && item.itemType == ItemType.Currency)
                {
                    if (from == TestContainer.Warehouse && !warehouseService.IsOpen)
                        return new InventoryMoveResult(InventoryResult.DestinationRejected, 0, 0);
                    InventoryMoveResult result = playerService.AddToInventory(data.inventoryData, item.itemId, source.slots[fromIndex].amount);
                    if (result.MovedAmount > 0) inventoryService.RemoveItem(source, fromIndex, result.MovedAmount);
                    NotifyLootChanged();
                    return result;
                }
            }

            if (from == TestContainer.Bag && to == TestContainer.Equipment)
            {
                return playerService.EquipFromInventory(data.inventoryData, fromIndex, toIndex);
            }

            if (from == TestContainer.Equipment && to == TestContainer.Bag)
            {
                return playerService.UnequipToInventory(data.inventoryData, fromIndex, toIndex);
            }

            if (from == TestContainer.Bag && to == TestContainer.Warehouse)
            {
                return warehouseService.Store(data.inventoryData, fromIndex, toIndex, all);
            }

            if (from == TestContainer.Warehouse && to == TestContainer.Bag)
            {
                return warehouseService.Withdraw(data.inventoryData, fromIndex, toIndex, all);
            }

            if (from == TestContainer.Warehouse && to == TestContainer.Warehouse)
            {
                return warehouseService.MoveWithin(fromIndex, toIndex, all);
            }

            if (from == TestContainer.Equipment || to == TestContainer.Equipment)
            {
                return new InventoryMoveResult(InventoryResult.DestinationRejected, 0, 0);
            }

            InventoryMoveResult moved = inventoryService.MoveItem(
                GetContainer(from), fromIndex, GetContainer(to), toIndex, all);
            playerService.SanitizeItemQuickSlots(data.inventoryData);
            NotifyLootChanged();
            return moved;
        }

        private void ReportResult(InventoryMoveResult result, TestContainer from, TestContainer to)
        {
            if (result.Result == InventoryResult.Success || result.Result == InventoryResult.Partial)
            {
                SetMessage(result.MovedAmount + "개 이동");
                return;
            }

            if (to == TestContainer.Equipment)
            {
                SetMessage("이 칸에는 들어가지 않는 종류입니다. 무기 칸에는 무기, 방어구 칸에는 해당 부위 장비만 가능합니다.");
                return;
            }

            if ((from == TestContainer.Warehouse || to == TestContainer.Warehouse) && !warehouseService.IsOpen)
            {
                SetMessage("창고가 닫혀 있습니다. 거점 창고 상호작용 버튼을 먼저 누르세요.");
                return;
            }

            SetMessage("옮길 수 없습니다.");
        }

        // ---------- 클릭 ----------
        public void ClickSlot(TestSlotView slot)
        {
            HideContextMenu();
            if (IsServicePanelOpen &&
                (slot.container == TestContainer.Bag || slot.container == TestContainer.Equipment))
            {
                selectedShopEquipment = slot.container == TestContainer.Equipment;
                selectedShopBagIndex = slot.index;
                SelectSlot(slot);
                RefreshShop();
                return;
            }
            switch (slot.container)
            {
                case TestContainer.Loot:
                case TestContainer.Warehouse:
                    SelectSlot(slot);
                    break;
                case TestContainer.Equipment:
                    if (EquipmentSlots.IsWeaponSlot(slot.index))
                    {
                        selectedWeapon = slot.index;
                        SetMessage((slot.index + 1) + "번 무기를 선택했습니다.");
                        SelectSlot(slot);
                    }
                    break;
                case TestContainer.WeaponQuick:
                    selectedWeapon = slot.index;
                    SetMessage((slot.index + 1) + "번 무기를 선택했습니다.");
                    Refresh();
                    break;
                case TestContainer.ItemQuick:
                    UseItemQuickSlot(slot.index);
                    break;
                default:
                    SelectSlot(slot);
                    break;
            }
        }

        private void Take(TestContainer container, int index)
        {
            if (container == TestContainer.Warehouse && !warehouseService.IsOpen)
            {
                SetMessage("창고가 닫혀 있습니다.");
                return;
            }

            GridContainerData source = GetContainer(container);
            GridSlotData slot = source.slots[index];
            if (slot.IsEmpty())
            {
                return;
            }

            // 기존 슬롯 상태(총기 마모/잔탄, 개봉 탄약)를 유지한 채 회수한다.
            if (catalog.TryGetItem(slot.itemId, out ItemData sourceItem) && sourceItem.itemType != ItemType.Currency)
            {
                int moved = 0;
                for (int pass = 0; pass < 2 && !slot.IsEmpty(); pass++)
                for (int targetIndex = 0; targetIndex < data.inventoryData.inventory.slots.Count && !slot.IsEmpty(); targetIndex++)
                {
                    GridSlotData target = data.inventoryData.inventory.slots[targetIndex];
                    if (pass == 0 ? target.IsEmpty() || target.itemId != slot.itemId : !target.IsEmpty()) continue;
                    moved += inventoryService.MoveItem(source, index, data.inventoryData.inventory, targetIndex, slot.amount).MovedAmount;
                }
                SetMessage(moved > 0 ? moved + "개 획득" : "가방이 꽉 찼습니다.");
                NotifyLootChanged();
                Refresh();
                return;
            }

            InventoryMoveResult result = playerService.AddToInventory(data.inventoryData, slot.itemId, slot.amount);
            if (result.MovedAmount > 0)
            {
                inventoryService.RemoveItem(source, index, result.MovedAmount);
                SetMessage(result.MovedAmount + "개 획득");
            }
            else
            {
                SetMessage("가방이 꽉 찼습니다.");
            }

            NotifyLootChanged();
            Refresh();
        }

        public void UseItemQuickSlot(int quickIndex)
        {
            if (!IsReady) return;
            if (!playerService.TryGetItemQuickSlot(data.inventoryData, quickIndex, out int bagIndex, out _))
            {
                SetMessage("퀵슬롯에 지정된 아이템이 없습니다.");
                return;
            }

            UseBagItem(bagIndex);
        }

        private void UseBagItem(int bagIndex)
        {
            GridSlotData slot = data.inventoryData.inventory.slots[bagIndex];
            if (!catalog.TryGetItem(slot.itemId, out ItemData item))
            {
                return;
            }

            if (item.itemType == ItemType.Special)
            {
                SetMessage("특수 효과는 담당 범위 밖입니다. 아이템은 소모되지 않습니다.");
                return;
            }

            InventoryResult result = playerService.UseRecoveryItem(data.inventoryData, bagIndex, recoveryTarget ?? this);
            SetMessage(result == InventoryResult.Success
                ? item.displayName + " 사용"
                : "회복 효과가 없거나 사용할 수 없는 상태입니다.");
            Refresh();
        }

        // 테스트 전용 수치. 실제 플레이어는 별도 IRecoveryTarget 구현으로 연결한다.
        public bool TryApplyRecovery(float healthPercent, float hungerPercent, float waterPercent)
        {
            float nextHealth = Mathf.Min(30, health + 30 * healthPercent / 100);
            float nextHunger = Mathf.Min(30, hunger + 30 * hungerPercent / 100);
            float nextWater = Mathf.Min(30, water + 30 * waterPercent / 100);

            if (Mathf.Approximately(nextHealth, health) &&
                Mathf.Approximately(nextHunger, hunger) &&
                Mathf.Approximately(nextWater, water))
            {
                return false;
            }

            health = nextHealth;
            hunger = nextHunger;
            water = nextWater;

            return true;
        }

        public void HoverSlot(TestSlotView slot)
        {
            hovered = slot;
            if (itemPopup == null) return;
            itemPopup.gameObject.SetActive(false);
            if (slot == null || !IsOpen || !IsReady || dragSource != null || !slot.gameObject.activeInHierarchy) return;
            int itemId, amount;
            if (slot.container == TestContainer.WeaponQuick || slot.container == TestContainer.ItemQuick)
                ResolveQuick(slot.container, slot.index, out itemId, out amount);
            else
            {
                GridContainerData container = GetContainer(slot.container);
                if (slot.index < 0 || slot.index >= container.slots.Count) return;
                itemId = container.slots[slot.index].itemId;
                amount = container.slots[slot.index].amount;
            }
            if (amount <= 0 || !catalog.TryGetItem(itemId, out ItemData item)) return;
            itemPopupText.text = item.displayName + "\n종류: " + ItemTypeLabel(item.itemType) +
                "\n무게: " + item.weight.ToString("0.##") + "\n가격: " + item.price +
                "\n\n" + item.description;
            float height = Mathf.Clamp(itemPopupText.preferredHeight + 28, 170, screen.rect.height - 24);
            itemPopup.sizeDelta = new Vector2(300, height);
            itemPopupText.rectTransform.sizeDelta = new Vector2(272, height - 28);
            itemPopup.SetAsLastSibling();
            itemPopup.gameObject.SetActive(true);
            PositionItemPopup();
        }

        private static string ItemTypeLabel(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Equipment: return "장비";
                case ItemType.Weapon: return "무기";
                case ItemType.Consumable: return "소모품";
                case ItemType.Material: return "재료";
                case ItemType.Ammo: return "탄약";
                case ItemType.Currency: return "재화";
                case ItemType.Sale: return "판매용";
                case ItemType.Special: return "특수 소모품";
                default: return itemType.ToString();
            }
        }

        private void PositionItemPopup()
        {
            if (itemPopup == null || !itemPopup.gameObject.activeSelf) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(screen, Input.mousePosition, null, out Vector2 point);
            Rect bounds = screen.rect;
            itemPopup.anchoredPosition = new Vector2(
                Mathf.Clamp(point.x + 18, bounds.xMin + 8, bounds.xMax - itemPopup.rect.width - 8),
                Mathf.Clamp(point.y - 18, bounds.yMin + itemPopup.rect.height + 8, bounds.yMax - 8));
        }

        private void NotifyLootChanged()
        {
            if (playerDeathOpen && openedContainer != null)
            {
                openedContainer.GetComponent<PlayerDeathContainer>()?.NotifyChanged();
                if (loot.IsEmpty()) SetMessage("분실물을 모두 회수했습니다.");
                return;
            }
            if (openedDrop != null)
            {
                bool empty = loot.IsEmpty();
                openedDrop.NotifyContentsChanged();
                if (empty) CloseLoot();
                return;
            }
            if (loot.IsEmpty())
            {
                SetMessage("전리품을 모두 비웠습니다. 실제 게임에서는 이 시점에 오브제가 풀로 반환됩니다.");
            }
        }

        // ---------- 버튼 동작 ----------
        public void RollLoot(LootContainerSize preset)
        {
            CloseLoot();
            lootPreset = preset;

            if (dropEntries.Count == 0)
            {
                loot = new LootContainerData(preset);
                SetMessage("DropTable.csv 가 연결되지 않아 빈 상자만 만들었습니다.");
            }
            else
            {
                loot = new DropRoller(new SystemRandomSource()).Roll(dropEntries, DropSourceType.Box, preset);
                SetMessage(preset + " 상자를 굴렸습니다. 빈 상자도 정상 결과입니다.");
            }

            if (lootPanel != null)
            {
                lootPanel.SetActive(true);
                warehousePanel.SetActive(false);
            }

            BuildLootGrid();
            Refresh();
        }

        public void ToggleWarehouse()
        {
            bool opening = !warehouseService.IsOpen;
            if (opening)
            {
                warehouseService.Open();
            }
            else
            {
                warehouseService.Close();
            }

            warehousePanel.SetActive(opening);
            lootPanel.SetActive(!opening);
            SetMessage(opening
                ? "거점 창고와 상호작용했습니다. 이제 보관과 회수가 가능합니다."
                : "창고를 닫았습니다. 이제 보관과 회수가 거부됩니다.");
            Refresh();
        }

        public void FillBag()
        {
            for (int index = 0; index < 40; index++)
            {
                playerService.AddToInventory(data.inventoryData, 22001, 5);
            }

            SetMessage("가방을 채웠습니다. 이 상태에서 전리품을 획득해 보세요.");
            Refresh();
        }

        public void KillPlayer()
        {
            if (!IsReady) return;
            CloseLoot();
            warehouseService.Close();
            if (warehousePanel != null) warehousePanel.SetActive(false);
            playerService.ClearOnDeath(data.inventoryData);
            SetMessage("사망 처리. 가방과 장비와 퀵슬롯이 비었고 지푸라기와 창고는 유지됩니다.");
            Refresh();
        }

        public void SaveGame()
        {
            try
            {
                saves.Save("testbench", data);
                SetMessage("저장 완료. " + SaveDirectory);
            }
            catch (Exception exception)
            {
                SetMessage("저장 실패: " + exception.Message);
            }
        }

        public void LoadGame()
        {
            if (saves.TryLoad("testbench", out PlayerSaveData loaded, out bool recovered))
            {
                data = loaded;
                warehouseService = new WarehouseService(catalog, data.warehouseData);
                warehousePanel.SetActive(false);
                lootPanel.SetActive(true);
                SetMessage(recovered ? "주 파일이 손상되어 백업에서 복구했습니다." : "불러오기 완료.");
            }
            else
            {
                SetMessage("불러올 저장 파일이 없습니다. 먼저 저장하세요.");
            }

            Refresh();
        }

        // 주 저장 파일을 일부러 깨뜨린 뒤 백업 복구가 되는지 본다.
        public void TestCorruptionRecovery()
        {
            try
            {
                saves.Save("recoverycheck", data);
                File.WriteAllText(Path.Combine(SaveDirectory, "recoverycheck.json"), "{ broken");
                bool ok = saves.TryLoad("recoverycheck", out _, out bool recovered);
                SetMessage(ok && recovered
                    ? "손상 복구 성공. 백업에서 되살렸습니다."
                    : "손상 복구 실패. 확인이 필요합니다.");
            }
            catch (Exception exception)
            {
                SetMessage("복구 검사 실패: " + exception.Message);
            }
        }

        // ---------- 입력 ----------
        private void Update()
        {
            if (data == null)
            {
                return;
            }

            if (useStandaloneKeyboard)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) { selectedWeapon = 0; Refresh(); }
                if (Input.GetKeyDown(KeyCode.Alpha2)) { selectedWeapon = 1; Refresh(); }
                if (Input.GetKeyDown(KeyCode.Alpha3)) { UseItemQuickSlot(0); }
                if (Input.GetKeyDown(KeyCode.Alpha4)) { UseItemQuickSlot(1); }
                if (Input.GetKeyDown(KeyCode.Alpha5)) { UseItemQuickSlot(2); }

                if (Input.GetKeyDown(KeyCode.E) && hovered != null &&
                    (hovered.container == TestContainer.Loot || hovered.container == TestContainer.Warehouse))
                {
                    Take(hovered.container, hovered.index);
                }
            }

            if (status != null && Time.unscaledTime > messageUntil)
            {
                status.text = string.Empty;
            }
            PositionItemPopup();
        }

        private void SetMessage(string text)
        {
            messageUntil = Time.unscaledTime + 4f;
            if (status != null)
            {
                status.text = text;
            }
        }

        // 플레이 중 스크립트 재컴파일 시 직렬화되지 않는 서비스 필드가 null 로 돌아온다. 데이터는 두고 서비스만 재구성한다.
        private bool EnsureServices()
        {
            if (playerService != null && warehouseService != null && craftingService != null)
            {
                return true;
            }

            if (itemCsv == null || data == null)
            {
                return false;
            }

            Initialize();
            warehouseService = new WarehouseService(catalog, data.warehouseData);
            return true;
        }

        // ---------- 표시 갱신 ----------
        public void Refresh()
        {
            if (!IsReady)
            {
                return;
            }

            // 이전 드래그 경로로 가방에 들어간 재화도 수량을 보존하며 보유값으로 회수한다.
            foreach (GridSlotData slot in data.inventoryData.inventory.slots)
            {
                if (slot.IsEmpty() || !catalog.TryGetItem(slot.itemId, out ItemData item) || item.itemType != ItemType.Currency) continue;
                InventoryMoveResult result = playerService.AddToInventory(data.inventoryData, slot.itemId, slot.amount);
                slot.amount -= result.MovedAmount;
                if (slot.amount == 0) slot.Clear();
            }
            playerService.SanitizeItemQuickSlots(data.inventoryData);

            foreach (TestSlotView view in views)
            {
                if (view == null)
                {
                    continue;
                }

                int itemId;
                int amount;

                if (view.container == TestContainer.WeaponQuick || view.container == TestContainer.ItemQuick)
                {
                    ResolveQuick(view.container, view.index, out itemId, out amount);
                }
                else
                {
                    GridContainerData container = GetContainer(view.container);
                    if (view.index >= container.slots.Count)
                    {
                        continue;
                    }

                    itemId = container.slots[view.index].itemId;
                    amount = container.slots[view.index].amount;
                }

                bool found = catalog.TryGetItem(itemId, out ItemData item) && amount > 0;
                view.icon.sprite = found && spriteMap.TryGetValue(itemId, out Sprite sprite) ? sprite : null;
                view.icon.enabled = view.icon.sprite != null;
                view.caption.text = found ? item.displayName : PlaceholderCaption(view);
                view.amount.text = found && amount > 1 ? amount.ToString() : string.Empty;
                view.background.color = SlotColor(view, found);
                if (found && craftPanel != null && craftPanel.activeSelf &&
                    (itemId == CraftingService.GunpowderItemId || (itemId >= 27001 && itemId <= 27004)))
                    view.background.color = colors.slotCraftMaterial;
                if (found &&
                    (view.container == TestContainer.Bag || view.container == TestContainer.Equipment || view.container == TestContainer.Warehouse))
                {
                    GridSlotData durableSlot = GetContainer(view.container).slots[view.index];
                    if (item.itemType == ItemType.Weapon)
                        view.amount.text = WeaponDurability.Remaining(durableSlot) + "/" + WeaponDurability.Maximum(itemId);
                    else if (armorDurability.IsArmor(catalog, durableSlot))
                        view.amount.text = armorDurability.Remaining(durableSlot) + "/" + Mathf.Max(1, armorDurability.maxDurability);
                }
            }

            if (statsText != null)
            {
                statsText.text = ": " + data.inventoryData.currency.ToString("N0");
            }

            RefreshCraftPanel();
            RefreshCraftVisuals();
            RefreshShop();
            RefreshRepair();
            HoverSlot(hovered);
        }

        private static string PlaceholderCaption(TestSlotView view)
        {
            if (view.container == TestContainer.Equipment)
            {
                switch (view.index)
                {
                    case 0: return "주 무기";
                    case 1: return "보조 무기";
                    case 2: return "머리";
                    default: return "몸통";
                }
            }

            if (view.container == TestContainer.WeaponQuick)
            {
                return "무기 " + (view.index + 1);
            }

            if (view.container == TestContainer.ItemQuick)
            {
                return "퀵 " + (view.index + 3);
            }

            return string.Empty;
        }

        private Color SlotColor(TestSlotView view, bool filled)
        {
            if (filled && view.container == selectedContainer && view.index == selectedIndex) return colors.slotSelected;

            if (view.container == TestContainer.WeaponQuick && view.index == selectedWeapon)
            {
                return colors.slotSelectedWeapon;
            }

            if (view.container == TestContainer.Equipment && view.index == selectedWeapon &&
                EquipmentSlots.IsWeaponSlot(view.index))
            {
                return colors.slotSelectedWeapon;
            }

            if (view.container == TestContainer.WeaponQuick)
            {
                return colors.slotWeaponQuick;
            }

            if (view.container == TestContainer.Equipment)
            {
                return colors.slotEquipment;
            }

            return filled ? colors.slotFilled : colors.slotEmpty;
        }

        // ---------- UI 구축 ----------
        // 배치 기준 1600 x 900. 좌측 장비·가방 / 우측 전리품·창고·제작대 / 하단 중앙 퀵슬롯.
        // 좌·우 두 열은 화면 양끝에 붙이고 가운데 아래에 퀵슬롯을 둔다. 패널만 HONETi 스킨을 쓴다.
        private const int HeaderBottom = 76;
        private const int LeftX = 24;
        private const int ColumnWidth = 460;
        private const int RightX = 1116;
        private const int MiddleX = 500;
        private const int MiddleWidth = 600;
        private const int SidePanelHeight = 640;

        // 하이어라키에 미리 놓인 UI 를 그대로 쓴다. 기획팀은 인스펙터에서 배치만 고치면 됨.
        // ui.screen 이 비어 있으면 false 를 돌려 기존 BuildUi() 로 넘어감
        // 캔버스는 어느 경로로 UI 를 얻든 같은 설정이어야 한다.
        // 씬 값이 WorldSpace 나 ConstantPixelSize 로 남아 있으면 화면에 아무것도 안 보인다.
        private void ConfigureCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private bool BindUi()
        {
            if (ui == null || ui.screen == null)
            {
                return false;
            }

            ResolveSkin();
            ConfigureCanvas();

            screen = ui.screen;
            lootPanel = ui.lootPanel;
            mapChestPanel = ui.mapChestPanel;
            warehousePanel = ui.warehousePanel;
            craftPanel = ui.craftPanel;
            lootGridRoot = ui.lootGridRoot;
            mapChestGridRoot = ui.mapChestGridRoot;
            warehouseGridRoot = ui.warehouseGridRoot;
            status = ui.status;
            statsText = ui.statsText;
            hint = ui.hint;
            dragIcon = ui.dragIcon;
            itemPopup = ui.itemPopup;
            itemPopupText = ui.itemPopupText;
            craftLabels = ui.craftLabels;

            // 칸을 옮기거나 지워도 따라가도록 매번 다시 모으고 벤치를 다시 꾼다.
            views.Clear();
            foreach (TestSlotView view in screen.GetComponentsInChildren<TestSlotView>(true))
            {
                view.bench = this;
                views.Add(view);
            }

            IList<int> mushrooms = CraftingService.MushroomItemIds;
            for (int index = 0; ui.craftButtons != null && index < ui.craftButtons.Length && index < mushrooms.Count; index++)
            {
                if (ui.craftButtons[index] == null)
                {
                    continue;
                }

                int mushroomItemId = mushrooms[index];
                ui.craftButtons[index].onClick.RemoveAllListeners();
                ui.craftButtons[index].onClick.AddListener(() => CraftAmmo(mushroomItemId));
            }

            // 상점/제작 버튼은 하이어라키 참조 기준으로 다시 연결한다.
            BindShopButtons();
            BindItemMenu();

            if (dragIcon != null)
            {
                dragIcon.gameObject.SetActive(false);
            }

            if (itemPopup != null)
            {
                itemPopup.gameObject.SetActive(false);
            }

            CloseLoot();
            BuildLootGrid();
            return true;
        }

        private void BuildUi()
        {
            ResolveSkin();
            ConfigureCanvas();

            screen = MakeRect("Root", transform, 0, 0, 1600, 900);
            screen.anchorMin = screen.anchorMax = screen.pivot = new Vector2(0.5f, 0.5f);
            screen.anchoredPosition = Vector2.zero;

            RectTransform titleBar = MakeRect("TitleBar", screen, LeftX, 16, ColumnWidth, 40);
            Image titleImage = titleBar.gameObject.AddComponent<Image>();
            ApplySkin(titleImage, honetiTitleSprite, new Color(0.12f, 0.19f, 0.24f, 0.86f), new Color(0.12f, 0.13f, 0.14f, 0.9f));
            titleImage.raycastTarget = false;
            MakeLabel(titleBar, "인벤토리", 16, 6, ColumnWidth - 32, 28, 18);

            hint = MakeLabel(screen, "클릭: 사용/획득 · 드래그: 이동\nF: 상자 열기/닫기 · E: 인벤토리",
                RightX, 16, ColumnWidth, 40, 12);
            hint.color = new Color(0.72f, 0.78f, 0.8f);

            // 상단 테스트 버튼은 생성하지 않는다.

            // 왼쪽: 장비 + 가방
            GameObject equipmentPanel = MakePanel("EquipmentPanel", LeftX, HeaderBottom, ColumnWidth, 156,
                "장비");
            BuildGrid(equipmentPanel.transform, TestContainer.Equipment, 4, 1, 20, 58, EquipCell);

            GameObject bagPanel = MakePanel("BagPanel", LeftX, HeaderBottom + 168, ColumnWidth, 472, "가방 / 25칸");
            BuildGrid(bagPanel.transform, TestContainer.Bag, 5, 5, 20, 58, BagCell);

            // 오른쪽: 전리품 / 창고 / 제작대. 같은 자리에서 서로 바꿔 띄운다.
            lootPanel = MakePanel("LootPanel", RightX, HeaderBottom, ColumnWidth, SidePanelHeight, "전리품 상자");
            lootGridRoot = MakeRect("LootGrid", lootPanel.transform, 20, 58, ColumnWidth - 40, SidePanelHeight - 78);
            mapChestPanel = MakePanel("MapChestPanel", RightX, HeaderBottom, ColumnWidth, SidePanelHeight, "맵 상자");
            mapChestGridRoot = MakeRect("MapChestGrid", mapChestPanel.transform, 20, 58, ColumnWidth - 40, SidePanelHeight - 78);
            mapChestPanel.SetActive(false);

            warehousePanel = MakePanel("WarehousePanel", RightX, HeaderBottom, ColumnWidth, SidePanelHeight,
                "보관상자 / " + InventorySettings.WarehouseWidth * InventorySettings.WarehouseHeight + "칸  (휠 스크롤)");
            warehouseGridRoot = BuildWarehouseScroll(warehousePanel.transform);
            warehousePanel.SetActive(false);

            BuildCraftPanel();

            // 상태 문구는 좌·우 패널 바로 아래 줄에 붙인다. 빈 가운데 열을 두지 않는다.
            statsText = MakeLabel(equipmentPanel.transform, string.Empty, 356, 58, 96, 80, 14);
            statsText.alignment = TextAnchor.MiddleCenter;
            status = MakeLabel(screen, string.Empty, RightX, HeaderBottom + 648, ColumnWidth, 56, 13);
            status.alignment = TextAnchor.UpperLeft;
            status.color = new Color(0.95f, 0.83f, 0.45f);

            // 하단 중앙: 퀵슬롯
            GameObject quickPanel = MakePanel("QuickPanel", 0, 780, 428, 110, null);
            RectTransform quickRect = (RectTransform)quickPanel.transform;
            quickRect.anchorMin = quickRect.anchorMax = new Vector2(0.5f, 0f);
            quickRect.pivot = new Vector2(0.5f, 0f);
            quickRect.anchoredPosition = new Vector2(0, 20);
            MakeLabel(quickPanel.transform, "무기 1 / 2", 16, 6, 150, 18, 12);
            BuildGrid(quickPanel.transform, TestContainer.WeaponQuick, 2, 1, 16, 26, QuickCell);
            MakeLabel(quickPanel.transform, "퀵슬롯 3 / 4 / 5", 200, 6, 190, 18, 12);
            BuildGrid(quickPanel.transform, TestContainer.ItemQuick, 3, 1, 200, 26, QuickCell);

            RectTransform dragRect = MakeRect("DragIcon", screen, 0, 0, 72, 72);
            dragRect.anchorMin = dragRect.anchorMax = dragRect.pivot = new Vector2(0.5f, 0.5f);
            dragIcon = dragRect.gameObject.AddComponent<Image>();
            dragIcon.raycastTarget = false;
            dragIcon.preserveAspect = true;
            dragRect.gameObject.SetActive(false);

            BuildLootGrid();
            BuildGrid(warehouseGridRoot, TestContainer.Warehouse,
                InventorySettings.WarehouseWidth, InventorySettings.WarehouseHeight, 0, 0, WarehouseCell);

            itemPopup = MakeRect("ItemPopup", screen, 0, 0, 300, 190);
            itemPopup.anchorMin = itemPopup.anchorMax = new Vector2(0.5f, 0.5f);
            Image popupBackground = itemPopup.gameObject.AddComponent<Image>();
            ApplySkin(popupBackground, honetiPanelSprite, new Color(0.08f, 0.12f, 0.16f, 0.96f), Color.black);
            popupBackground.raycastTarget = false;
            itemPopupText = MakeLabel(itemPopup, string.Empty, 14, 14, 272, 162, 16);
            itemPopupText.supportRichText = false;
            itemPopupText.raycastTarget = false;
            itemPopupText.verticalOverflow = VerticalWrapMode.Truncate;
            itemPopup.gameObject.SetActive(false);

        }

        // 창고는 칸이 많아 한 화면에 들어가지 않는다. 마우스 휠로 세로 스크롤한다.
        private RectTransform BuildWarehouseScroll(Transform panel)
        {
            RectTransform viewport = MakeRect("WarehouseViewport", panel, 20, 62, ColumnWidth - 40, SidePanelHeight - 82);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.12f);
            viewport.gameObject.AddComponent<RectMask2D>();

            int contentHeight = InventorySettings.WarehouseHeight * (WarehouseCell + 4) - 4;
            RectTransform content = MakeRect("WarehouseGrid", viewport, 0, 0, ColumnWidth - 40, contentHeight);

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36f;
            return content;
        }

        // 테스트 버튼. 화면에 띄우지 않는다. 수동 테스트가 필요할 때만 BuildUi 에서 불러 쓴다.
        private void BuildButtons()
        {
            string[] labels = { "초기화", "창고 상호작용", "제작대", "가방 채우기", "사망", "저장", "불러오기", "손상 복구 검사" };
            Action[] actions =
            {
                ResetAll, ToggleWarehouse, ToggleCraftingTable, FillBag, KillPlayer, SaveGame, LoadGame, TestCorruptionRecovery
            };

            const int buttonWidth = 290;
            const int buttonHeight = 34;
            for (int index = 0; index < labels.Length; index++)
            {
                Action action = actions[index];
                MakeButton(screen, labels[index], MiddleX + index % 2 * (buttonWidth + 10),
                    HeaderBottom + index / 2 * (buttonHeight + 6), buttonWidth, buttonHeight, action);
            }
        }

        // HONETi flat_gui_elements 스킨. 인스펙터가 비어 있으면 에디터에서 경로로 채운다.
        private void ResolveSkin()
        {
            honetiPanelSprite = ResolveSprite(honetiPanelSprite, "universal_panel_1");
            honetiSlotSprite = ResolveSprite(honetiSlotSprite, "universal_panel_2");
            honetiTitleSprite = ResolveSprite(honetiTitleSprite, "title_bg");
            honetiButtonSprite = ResolveSprite(honetiButtonSprite, "button_outlined");
        }

        // 에디터 전용 보조. HONETi 폴더가 옮겨져도 이름으로 찾는다. 빌드에서는 인스펙터 값만 쓴다.
        private static Sprite ResolveSprite(Sprite assigned, string assetName)
        {
#if UNITY_EDITOR
            if (assigned != null)
            {
                return assigned;
            }

            foreach (string guid in UnityEditor.AssetDatabase.FindAssets(assetName + " t:Sprite"))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("HONETi") || !path.EndsWith(assetName + ".png"))
                {
                    continue;
                }

                Sprite found = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (found != null)
                {
                    return found;
                }
            }
#endif
            return assigned;
        }

        // 스프라이트가 있으면 9 슬라이스로, 없으면 단색으로 떨어뜨린다.
        private static void ApplySkin(Image image, Sprite sprite, Color tint, Color fallback)
        {
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.color = tint;
                return;
            }

            image.color = fallback;
        }

        // 제작대 상호작용 시 전리품 자리에 제작 UI 를 띄운다. 기획서 5.3.
        private void BuildCraftPanel()
        {
            craftPanel = MakePanel("CraftPanel", RightX, HeaderBottom, ColumnWidth, SidePanelHeight, "총알 제작대");
            MakeLabel(craftPanel.transform, "같은 버섯 5 + 화약 5  ->  해당 탄종 1박스", 20, 48, ColumnWidth - 40, 26, 14);

            IList<int> mushrooms = CraftingService.MushroomItemIds;
            craftLabels = new Text[mushrooms.Count];
            for (int index = 0; index < mushrooms.Count; index++)
            {
                int mushroomItemId = mushrooms[index];
                float y = 88 + index * 84;
                craftLabels[index] = MakeLabel(craftPanel.transform, string.Empty, 20, y, 236, 48, 13);
                MakeButton(craftPanel.transform, "제작", 268, y + 6, 152, 36, () => CraftAmmo(mushroomItemId));
            }
        
            craftPanel.SetActive(false);
        }

        public void ToggleCraftingTable()
        {
            if (!IsReady || craftPanel == null)
            {
                return;
            }

            bool opening = !craftPanel.activeSelf;
            if (opening)
            {
                CloseLoot();
                warehouseService.Close();
                if (warehousePanel != null) warehousePanel.SetActive(false);
                if (lootPanel != null) lootPanel.SetActive(false);
            }
            else if (lootPanel != null)
            {
                lootPanel.SetActive(true);
            }

            craftPanel.SetActive(opening);
            SetMessage(opening ? "제작대를 열었습니다." : "제작대를 닫았습니다.");
            Refresh();
        }

        public CraftResult CraftAmmo(int mushroomItemId)
        {
            if (!IsReady)
            {
                return CraftResult.InvalidData;
            }

            CraftResult result = craftingService.Craft(data.inventoryData, mushroomItemId, data.warehouseData);
            SetMessage(CraftMessage(result, mushroomItemId));
            Refresh();
            return result;
        }

        private string CraftMessage(CraftResult result, int mushroomItemId)
        {
            switch (result)
            {
                case CraftResult.Success:
                    int ammoItemId;
                    CraftingService.TryGetAmmoItemId(mushroomItemId, out ammoItemId);
                    return "제작 완료. " + DisplayName(ammoItemId) + " 1박스를 가방에 넣었습니다.";
                case CraftResult.NotEnoughMushroom:
                    return DisplayName(mushroomItemId) + "이(가) " + CraftingService.MushroomCost + "개 필요합니다.";
                case CraftResult.NotEnoughGunpowder:
                    return "화약이 " + CraftingService.GunpowderCost + "개 필요합니다.";
                case CraftResult.NoSpace:
                    return "가방이 꽉 찼습니다. 재료는 그대로 유지됩니다.";
                case CraftResult.UnknownRecipe:
                    return "제작법이 없는 재료입니다.";
                default:
                    return "제작할 수 없습니다.";
            }
        }

        private string DisplayName(int itemId)
        {
            ItemData item;
            return catalog != null && catalog.TryGetItem(itemId, out item) ? item.displayName : itemId.ToString();
        }

        private void RefreshCraftPanel()
        {
            if (craftPanel == null || !craftPanel.activeSelf || craftLabels == null)
            {
                return;
            }

            IList<int> mushrooms = CraftingService.MushroomItemIds;
            int gunpowder = craftingService.Count(data.inventoryData.inventory, CraftingService.GunpowderItemId) + craftingService.Count(data.warehouseData, CraftingService.GunpowderItemId);
            for (int index = 0; index < craftLabels.Length && index < mushrooms.Count; index++)
            {
                int mushroomItemId = mushrooms[index];
                int ammoItemId;
                CraftingService.TryGetAmmoItemId(mushroomItemId, out ammoItemId);
                int owned = craftingService.Count(data.inventoryData.inventory, mushroomItemId) + craftingService.Count(data.warehouseData, mushroomItemId);
                bool ready = craftingService.CanCraft(data.inventoryData, mushroomItemId, data.warehouseData) == CraftResult.Success;
        
                // 행은 한 줄로 짧게. 자세한 재료 수량은 아래 상세 칸이 보여 준다.
                craftLabels[index].text = DisplayName(mushroomItemId) + "  " + owned + " / " + CraftingService.MushroomCost +
                    "   →   " + DisplayName(ammoItemId);
                craftLabels[index].color = ready ? colors.craftLabelReady : colors.craftLabelBlocked;
            }
        }

        // 전리품 상자 규격 프리셋 버튼. 화면에 띄우지 않는다. 수동 테스트가 필요할 때만 BuildUi 에서 불러 쓴다.
        private void BuildLootSizeButtons()
        {
            LootContainerSize[] presets =
            {
                LootContainerSize.Box2x4, LootContainerSize.Box3x3,
                LootContainerSize.Box3x5, LootContainerSize.Box4x1
            };
            string[] names = { "2 x 4", "3 x 3", "3 x 5", "4 x 1" };

            MakeLabel(screen, "전리품 상자 규격", MiddleX, 256, MiddleWidth, 20, 12);
            for (int index = 0; index < presets.Length; index++)
            {
                LootContainerSize preset = presets[index];
                MakeButton(screen, names[index], MiddleX + index * 148, 278, 142, 32, () => RollLoot(preset));
            }
        }

        // 상자를 열 때마다 이전 칸이 남아 쌓이던 문제. 두 그리드를 모두 비운 뒤 다시 그린다.
        private void ClearLootCells(RectTransform gridRoot)
        {
            if (gridRoot == null) return;
            for (int index = gridRoot.childCount - 1; index >= 0; index--)
            {
                Transform child = gridRoot.GetChild(index);
                TestSlotView view = child.GetComponent<TestSlotView>();
                if (view == null || view.container != TestContainer.Loot) continue;
                views.Remove(view);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        private void BuildLootGrid()
        {
            ClearLootCells(lootGridRoot);
            ClearLootCells(mapChestGridRoot);
            ClearLootCells(playerDeathGrid);

            LootContainerSizes.GetSize(loot.sizePreset, out int width, out int height);
            RectTransform gridRoot = playerDeathOpen ? playerDeathGrid : mapChestOpen ? mapChestGridRoot : lootGridRoot;

            // 상자 규격 그대로 그리되 가로를 긴 쪽으로 둔다. 2x4 -> 4x2, 3x3 -> 3x3, 3x5 -> 5x3, 4x1 -> 4x1.
            // 패널과 그리드의 위치·크기는 건드리지 않는다. 기획팀이 인스펙터에서 잡은 그대로 둔다.
            // 칸 크기만 그리드 폭에 맞춰 줄인다. 폭을 넓히면 칸도 커지고 LootCell 이 상한이다.
            int columns = playerDeathOpen ? 5 : Mathf.Max(width, height);
            int rows = playerDeathOpen ? 6 : Mathf.Min(width, height);
            int cell = Mathf.Clamp(Mathf.FloorToInt((gridRoot.rect.width + 4) / columns) - 4, 16, LootCell);
            BuildGrid(gridRoot, TestContainer.Loot, columns, rows, 0, 0, cell, width * height);
        }

        private void BuildGrid(Transform parent, TestContainer container, int width, int height, int x, int y, int cell,
            int slotCount = -1)
        {
            const int gap = 4;
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    int index = row * width + column;

                    // 접어서 그릴 때 마지막 줄에 남는 자리는 만들지 않는다.
                    if (slotCount >= 0 && index >= slotCount)
                    {
                        return;
                    }

                    RectTransform rect = MakeRect(
                        container + "_" + index, parent,
                        x + column * (cell + gap), y + row * (cell + gap), cell, cell);

                    Image background = rect.gameObject.AddComponent<Image>();
                    ApplySkin(background, honetiSlotSprite, Color.white, new Color(0.17f, 0.18f, 0.19f, 0.9f));

                    RectTransform iconRect = MakeRect("Icon", rect, 5, 5, cell - 10, cell - 10);
                    Image icon = iconRect.gameObject.AddComponent<Image>();
                    icon.raycastTarget = false;
                    icon.preserveAspect = true;

                    Text caption = MakeLabel(rect, string.Empty, 2, cell - 17, cell - 4, 15, cell > 40 ? 10 : 8);
                    caption.alignment = TextAnchor.LowerCenter;
                    caption.color = new Color(0.82f, 0.85f, 0.86f);
                    caption.raycastTarget = false;

                    Text amount = MakeLabel(rect, string.Empty, 2, 2, cell - 6, 16, cell > 40 ? 12 : 9);
                    amount.alignment = TextAnchor.UpperRight;
                    amount.color = new Color(1f, 0.92f, 0.6f);
                    amount.raycastTarget = false;

                    TestSlotView view = rect.gameObject.AddComponent<TestSlotView>();
                    view.bench = this;
                    view.container = container;
                    view.index = index;
                    view.background = background;
                    view.icon = icon;
                    view.caption = caption;
                    view.amount = amount;
                    views.Add(view);
                }
            }
        }

        private GameObject MakePanel(string name, int x, int y, int width, int height, string title)
        {
            RectTransform rect = MakeRect(name, screen, x, y, width, height);
            Image background = rect.gameObject.AddComponent<Image>();
            ApplySkin(background, honetiPanelSprite, new Color(0.13f, 0.19f, 0.24f, 0.84f), new Color(0.12f, 0.13f, 0.14f, 0.84f));

            if (!string.IsNullOrEmpty(title))
            {
                MakeLabel(rect, title, 20, 14, width - 40, 28, 16);
            }

            return rect.gameObject;
        }

        private RectTransform MakeRect(string name, Transform parent, float x, float y, float width, float height)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
            return rect;
        }

        private Text MakeLabel(Transform parent, string text, float x, float y, float width, float height, int size)
        {
            RectTransform rect = MakeRect("Label", parent, x, y, width, height);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.text = text;
            label.color = new Color(0.92f, 0.94f, 0.95f);
            label.alignment = TextAnchor.UpperLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private void MakeButton(Transform parent, string text, float x, float y, float width, float height, Action onClick)
        {
            RectTransform rect = MakeRect("Button_" + text, parent, x, y, width, height);
            Image background = rect.gameObject.AddComponent<Image>();
            ApplySkin(background, honetiButtonSprite, Color.white, new Color(0.21f, 0.29f, 0.31f, 1f));

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => onClick());

            Text label = MakeLabel(rect, text, 0, 0, width, height, 13);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
        }
    }
}
