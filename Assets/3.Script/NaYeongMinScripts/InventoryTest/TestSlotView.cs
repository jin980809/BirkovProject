using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
            bench.ClickSlot(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
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
