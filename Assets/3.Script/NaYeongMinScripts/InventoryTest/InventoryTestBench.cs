using System;
using System.Collections.Generic;
using System.IO;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.Rng;
using Birdkov.NaYeongMin.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Birdkov.NaYeongMin.InventoryTest
{
    [Serializable]
    public class TestIconBinding
    {
        public int itemId;
        public Sprite sprite;
    }

    // 인벤토리 계열 기능을 육안으로 확인하는 테스트 벤치.
    // 본 게임 UI가 아니라 검증용이며 씬 NaYeongMin.unity 에서만 쓴다.
    // Tools > NaYeongMin > 테스트 벤치 구성 으로 씬에 배치한다.
    [RequireComponent(typeof(Canvas))]
    public sealed class InventoryTestBench : MonoBehaviour
    {
        public TextAsset itemCsv;
        public TextAsset dropCsv;
        public Font font;
        public TestIconBinding[] icons = Array.Empty<TestIconBinding>();

        private const int BagCell = 66;
        private const int EquipCell = 74;
        private const int QuickCell = 62;
        private const int LootCell = 62;
        private const int WarehouseCell = 31;

        private ItemCatalog catalog;
        private InventoryService inventoryService;
        private PlayerInventoryService playerService;
        private WarehouseService warehouseService;
        private JsonSaveSystem saves;
        private List<DropTableEntry> dropEntries = new List<DropTableEntry>();

        private PlayerSaveData data;
        private LootContainerData loot = new LootContainerData();
        private LootContainerSize lootPreset = LootContainerSize.Box2x4;

        private readonly List<TestSlotView> views = new List<TestSlotView>();
        private readonly Dictionary<int, Sprite> spriteMap = new Dictionary<int, Sprite>();

        private RectTransform screen;
        private GameObject lootPanel, warehousePanel;
        private RectTransform lootGridRoot, warehouseGridRoot;
        private Text status, statsText, hint;
        private Image dragIcon;
        private TestSlotView dragSource, hovered;
        private float messageUntil;

        private float health = 10, hunger = 10, water = 10;
        private int selectedWeapon;

        private string SaveDirectory => Path.Combine(Application.persistentDataPath, "NaYeongMinTestBench");

        private void Start()
        {
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
            BuildUi();
            ResetAll();
        }

        private void Initialize()
        {
            List<ItemData> items = ItemCsvLoader.Parse(itemCsv.text);

            // CSV에 총기와 방어구가 아직 없어 슬롯 규칙을 볼 수 없다.
            // 확인용 임시 데이터만 메모리에 추가한다. CSV 원본은 건드리지 않는다.
            items.Add(new ItemData { itemId = 990001, itemType = ItemType.Weapon, displayName = "임시 기관권총", maxStack = 1 });
            items.Add(new ItemData { itemId = 990002, itemType = ItemType.Weapon, displayName = "임시 샷건", maxStack = 1 });
            items.Add(new ItemData { itemId = 990101, itemType = ItemType.Equipment, equipmentSlotType = EquipmentSlotType.Helmet, displayName = "임시 헬멧", maxStack = 1 });
            items.Add(new ItemData { itemId = 990102, itemType = ItemType.Equipment, equipmentSlotType = EquipmentSlotType.Armor, displayName = "임시 방탄복", maxStack = 1 });

            catalog = new ItemCatalog(items);
            inventoryService = new InventoryService(catalog);
            playerService = new PlayerInventoryService(catalog);
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
        }

        public void ResetAll()
        {
            EndDrag();
            data = new PlayerSaveData();
            warehouseService = new WarehouseService(catalog, data.warehouseData);
            loot = new LootContainerData(lootPreset);
            health = hunger = water = 10;
            selectedWeapon = 0;

            foreach (int id in new[] { 21001, 21002, 22001, 23001, 24002, 20001 })
            {
                playerService.AddToInventory(data.inventoryData, id, id == 21001 ? 4 : 1);
            }

            playerService.AddToInventory(data.inventoryData, 990001, 1);
            playerService.AddToInventory(data.inventoryData, 990002, 1);
            playerService.AddToInventory(data.inventoryData, 990101, 1);
            playerService.AddToInventory(data.inventoryData, 990102, 1);

            inventoryService.AddItem(data.warehouseData, 23001, 5);

            SetMessage("초기화 완료. 무기와 방어구는 가방에 들어갑니다. 자동 착용되지 않습니다.");
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
            const int all = 99;

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
                return warehouseService.Withdraw(data.inventoryData, toIndex, fromIndex, all);
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
                SetMessage("장비 슬롯은 비어 있을 때만 착용됩니다. 종류가 맞는지, 이미 차 있지 않은지 확인하세요.");
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
            switch (slot.container)
            {
                case TestContainer.Loot:
                case TestContainer.Warehouse:
                    Take(slot.container, slot.index);
                    break;
                case TestContainer.Equipment:
                    if (EquipmentSlots.IsWeaponSlot(slot.index))
                    {
                        selectedWeapon = slot.index;
                        SetMessage((slot.index + 1) + "번 무기를 선택했습니다.");
                        Refresh();
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
                    UseBagItem(slot.index);
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

        private void UseItemQuickSlot(int quickIndex)
        {
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

            float nextHealth = Mathf.Min(30, health + item.healthRecovery);
            float nextHunger = Mathf.Min(30, hunger + item.hungerRecovery);
            float nextWater = Mathf.Min(30, water + item.waterRecovery);

            if (Mathf.Approximately(nextHealth, health) &&
                Mathf.Approximately(nextHunger, hunger) &&
                Mathf.Approximately(nextWater, water))
            {
                SetMessage("회복 효과가 없거나 이미 최대치입니다.");
                return;
            }

            health = nextHealth;
            hunger = nextHunger;
            water = nextWater;

            inventoryService.RemoveItem(data.inventoryData.inventory, bagIndex, 1);
            playerService.SanitizeItemQuickSlots(data.inventoryData);
            SetMessage(item.displayName + " 사용");
            Refresh();
        }

        public void HoverSlot(TestSlotView slot)
        {
            hovered = slot;
        }

        private void NotifyLootChanged()
        {
            if (loot.IsEmpty())
            {
                SetMessage("전리품을 모두 비웠습니다. 실제 게임에서는 이 시점에 오브제가 풀로 반환됩니다.");
            }
        }

        // ---------- 버튼 동작 ----------
        public void RollLoot(LootContainerSize preset)
        {
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
            playerService.ClearOnDeath(data.inventoryData);
            SetMessage("사망 처리. 가방과 장비와 퀵슬롯이 비었고 창고는 유지됩니다.");
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

            if (status != null && Time.unscaledTime > messageUntil)
            {
                status.text = string.Empty;
            }
        }

        private void SetMessage(string text)
        {
            messageUntil = Time.unscaledTime + 4f;
            if (status != null)
            {
                status.text = text;
            }
        }

        // ---------- 표시 갱신 ----------
        public void Refresh()
        {
            if (data == null)
            {
                return;
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
            }

            if (statsText != null)
            {
                statsText.text = string.Format(
                    "체력 {0:0}/30    허기 {1:0}/30    수분 {2:0}/30    선택 무기 {3}번    창고 {4}",
                    health, hunger, water, selectedWeapon + 1,
                    warehouseService != null && warehouseService.IsOpen ? "열림" : "닫힘");
            }
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
            if (view.container == TestContainer.WeaponQuick && view.index == selectedWeapon)
            {
                return new Color(0.16f, 0.45f, 0.42f, 0.95f);
            }

            if (view.container == TestContainer.Equipment && view.index == selectedWeapon &&
                EquipmentSlots.IsWeaponSlot(view.index))
            {
                return new Color(0.16f, 0.45f, 0.42f, 0.95f);
            }

            if (view.container == TestContainer.WeaponQuick)
            {
                return new Color(0.18f, 0.22f, 0.26f, 0.9f);
            }

            if (view.container == TestContainer.Equipment)
            {
                return new Color(0.2f, 0.2f, 0.24f, 0.95f);
            }

            return filled ? new Color(0.24f, 0.25f, 0.26f, 0.95f) : new Color(0.17f, 0.18f, 0.19f, 0.9f);
        }

        // ---------- UI 구축 ----------
        private void BuildUi()
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

            screen = MakeRect("Root", transform, 0, 0, 1600, 900);
            screen.anchorMin = screen.anchorMax = screen.pivot = new Vector2(0.5f, 0.5f);
            screen.anchoredPosition = Vector2.zero;
            screen.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.08f, 0.09f, 1f);

            MakeLabel(screen, "NaYeongMin 인벤토리 테스트 벤치", 30, 18, 700, 34, 22);
            hint = MakeLabel(screen, "드래그로 이동 · 클릭으로 사용/획득 · 1·2 무기 선택 · 3·4·5 아이템 퀵 사용 · E 획득",
                30, 52, 1100, 26, 14);
            hint.color = new Color(0.65f, 0.7f, 0.72f);

            BuildButtons();

            // 좌측: 전리품 / 창고
            lootPanel = MakePanel("LootPanel", 30, 150, 430, 470, "전리품 상자");
            lootGridRoot = MakeRect("LootGrid", lootPanel.transform, 22, 96, 380, 340);

            warehousePanel = MakePanel("WarehousePanel", 30, 150, 430, 470, "허브 창고  10 x 12 = 120칸");
            warehouseGridRoot = MakeRect("WarehouseGrid", warehousePanel.transform, 22, 60, 380, 390);
            warehousePanel.SetActive(false);

            BuildLootSizeButtons();

            // 우측: 장비 + 가방
            GameObject playerPanel = MakePanel("PlayerPanel", 500, 150, 620, 630, "플레이어");
            MakeLabel(playerPanel.transform, "장비 슬롯  4칸  ( 무기 2 · 방어구 2 )", 22, 52, 420, 24, 14);
            BuildGrid(playerPanel.transform, TestContainer.Equipment, 4, 1, 22, 82, EquipCell);

            MakeLabel(playerPanel.transform, "가방  5 x 5 = 25칸", 22, 180, 420, 24, 14);
            BuildGrid(playerPanel.transform, TestContainer.Bag, 5, 5, 22, 210, BagCell);

            // 퀵슬롯
            GameObject quickPanel = MakePanel("QuickPanel", 500, 800, 620, 82, null);
            MakeLabel(quickPanel.transform, "무기 퀵 (링크)", 14, 6, 130, 20, 12);
            BuildGrid(quickPanel.transform, TestContainer.WeaponQuick, 2, 1, 14, 26, QuickCell);
            MakeLabel(quickPanel.transform, "아이템 퀵 (가방 매핑)", 190, 6, 200, 20, 12);
            BuildGrid(quickPanel.transform, TestContainer.ItemQuick, 3, 1, 190, 26, QuickCell);

            statsText = MakeLabel(screen, string.Empty, 1140, 160, 430, 30, 15);
            status = MakeLabel(screen, string.Empty, 1140, 200, 430, 300, 14);
            status.alignment = TextAnchor.UpperLeft;
            status.color = new Color(0.95f, 0.83f, 0.45f);

            RectTransform dragRect = MakeRect("DragIcon", screen, 0, 0, 54, 54);
            dragRect.anchorMin = dragRect.anchorMax = dragRect.pivot = new Vector2(0.5f, 0.5f);
            dragIcon = dragRect.gameObject.AddComponent<Image>();
            dragIcon.raycastTarget = false;
            dragRect.gameObject.SetActive(false);

            BuildLootGrid();
            BuildGrid(warehouseGridRoot, TestContainer.Warehouse,
                InventorySettings.WarehouseWidth, InventorySettings.WarehouseHeight, 0, 0, WarehouseCell);
        }

        private void BuildButtons()
        {
            string[] labels = { "초기화", "창고 상호작용", "가방 채우기", "사망", "저장", "불러오기", "손상 복구 검사" };
            Action[] actions =
            {
                ResetAll, ToggleWarehouse, FillBag, KillPlayer, SaveGame, LoadGame, TestCorruptionRecovery
            };

            for (int index = 0; index < labels.Length; index++)
            {
                Action action = actions[index];
                MakeButton(screen, labels[index], 30 + index * 152, 92, 144, 34, action);
            }
        }

        private void BuildLootSizeButtons()
        {
            LootContainerSize[] presets =
            {
                LootContainerSize.Box2x4, LootContainerSize.Box3x3,
                LootContainerSize.Box3x5, LootContainerSize.Box4x1
            };
            string[] names = { "2 x 4", "3 x 3", "3 x 5", "4 x 1" };

            for (int index = 0; index < presets.Length; index++)
            {
                LootContainerSize preset = presets[index];
                MakeButton(lootPanel.transform, names[index], 22 + index * 96, 52, 88, 30, () => RollLoot(preset));
            }
        }

        private void BuildLootGrid()
        {
            for (int index = views.Count - 1; index >= 0; index--)
            {
                if (views[index] == null || views[index].container != TestContainer.Loot)
                {
                    continue;
                }

                Destroy(views[index].gameObject);
                views.RemoveAt(index);
            }

            LootContainerSizes.GetSize(loot.sizePreset, out int width, out int height);
            BuildGrid(lootGridRoot, TestContainer.Loot, width, height, 0, 0, LootCell);
        }

        private void BuildGrid(Transform parent, TestContainer container, int width, int height, int x, int y, int cell)
        {
            const int gap = 4;
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    int index = row * width + column;
                    RectTransform rect = MakeRect(
                        container + "_" + index, parent,
                        x + column * (cell + gap), y + row * (cell + gap), cell, cell);

                    Image background = rect.gameObject.AddComponent<Image>();
                    background.color = new Color(0.17f, 0.18f, 0.19f, 0.9f);

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
            rect.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.13f, 0.14f, 0.95f);

            if (!string.IsNullOrEmpty(title))
            {
                MakeLabel(rect, title, 20, 16, width - 40, 28, 17);
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
            background.color = new Color(0.21f, 0.29f, 0.31f, 1f);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => onClick());

            Text label = MakeLabel(rect, text, 0, 0, width, height, 13);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
        }
    }
}
