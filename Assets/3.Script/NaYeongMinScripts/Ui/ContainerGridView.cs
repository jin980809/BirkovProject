using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;
using UnityEngine.UI;

// 슬롯 그리드 하나. 가방·장비·퀵슬롯·외부 컨테이너가 같은 컴포넌트를 쓰고 데이터만 갈아끼운다.
namespace Birdkov.NaYeongMin.Ui
{
    public sealed class ContainerGridView : MonoBehaviour
    {
        [SerializeField] private UiContainerKind kind = UiContainerKind.Bag;
        [SerializeField] private ItemSlotView slotPrefab;
        [SerializeField] private RectTransform slotRoot;
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField] private Text titleLabel;
        [SerializeField] private GameObject emptyHint;

        private readonly List<ItemSlotView> slots = new List<ItemSlotView>();
        private InventoryUiTheme theme;
        private IInventoryUiHost host;

        public UiContainerKind Kind => kind;
        public GridContainerData Data { get; private set; }
        public IReadOnlyList<ItemSlotView> Slots => slots;
        public int SlotCount => slots.Count;

        public void Initialize(IInventoryUiHost uiHost, InventoryUiTheme uiTheme)
        {
            host = uiHost;
            theme = uiTheme;
        }

        // 퀵슬롯처럼 실제 컨테이너가 없는 화면은 data 없이 칸 수만 준다.
        public void Bind(GridContainerData data, int width, int height, string title)
        {
            Data = data;
            if (titleLabel != null)
            {
                titleLabel.text = title;
            }

            if (grid != null && width > 0)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = width;
            }

            EnsureSlots(Mathf.Max(0, width * height));
        }

        public void Clear()
        {
            Data = null;
            EnsureSlots(0);
        }

        private void EnsureSlots(int wanted)
        {
            if (slotPrefab == null || slotRoot == null)
            {
                return;
            }

            while (slots.Count < wanted)
            {
                ItemSlotView slot = Instantiate(slotPrefab, slotRoot);
                slot.name = kind + "Slot" + slots.Count;
                slots.Add(slot);
                slot.Bind(this, host, slots.Count - 1, theme != null ? theme.GetSlotSprite(kind) : null);
            }

            for (int index = 0; index < slots.Count; index++)
            {
                slots[index].gameObject.SetActive(index < wanted);
            }

            if (emptyHint != null)
            {
                emptyHint.SetActive(wanted == 0);
            }
        }

        public void SetSlotContent(int index, Sprite icon, string amountText, string caption)
        {
            if (index < 0 || index >= slots.Count)
            {
                return;
            }

            slots[index].SetContent(icon, amountText, caption, theme != null ? theme.EmptyIconColor : Color.clear);
            slots[index].SetTint(theme != null ? theme.NormalColor : Color.white);
        }

        public void SetSlotTint(int index, Color color)
        {
            if (index >= 0 && index < slots.Count)
            {
                slots[index].SetTint(color);
            }
        }
    }
}
