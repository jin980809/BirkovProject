using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 테스트 UI 슬롯 하나. 입력 이벤트를 벤치로 넘기기만 한다.
namespace Birdkov.NaYeongMin.InventoryTest
{
    // 테스트 UI의 슬롯 하나. 어느 컨테이너의 몇 번 칸인지만 들고 있고
    // 실제 판정은 전부 InventoryTestBench 가 한다.
    public enum TestContainer
    {
        Bag,
        Equipment,
        Loot,
        Warehouse,
        WeaponQuick,
        ItemQuick
    }

    public sealed class TestSlotView : MonoBehaviour,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        public InventoryTestBench bench;
        public TestContainer container;
        public int index;

        public Image background;
        public Image icon;
        public Text caption;
        public Text amount;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;
            if (eventData.button == PointerEventData.InputButton.Right) { bench.RightClickSlot(this); return; }
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (eventData.clickCount >= 2) { bench.DoubleClickSlot(this); return; }
            bench.ClickSlot(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            eventData.eligibleForClick = false;
            bench.BeginDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            bench.DragTo(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            bench.EndDrag();
        }

        public void OnDrop(PointerEventData eventData)
        {
            bench.DropOn(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            bench.HoverSlot(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            bench.HoverSlot(null);
        }
    }
}
