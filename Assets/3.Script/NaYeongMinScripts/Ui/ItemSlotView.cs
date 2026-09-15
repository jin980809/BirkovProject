using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 슬롯 1칸, 툴팁, 드래그 표시. UI 4종이 공유한다. 데이터는 들고 있지 않고 좌표만 안다.
namespace Birdkov.NaYeongMin.Ui
{
    // 슬롯이 컨트롤러에 알리는 이벤트. 컨트롤러가 실제 이동과 사용을 수행한다.
    public interface IInventoryUiHost
    {
        void OnSlotClicked(ItemSlotView slot, bool splitModifier);
        void OnSlotDragBegin(ItemSlotView slot);
        void OnSlotDragMoved(Vector2 screenPosition);
        void OnSlotDragEnd();
        void OnSlotDropped(ItemSlotView target);
        void OnSlotHoverEnter(ItemSlotView slot);
        void OnSlotHoverExit(ItemSlotView slot);
    }

    [RequireComponent(typeof(RectTransform))]
    public sealed class ItemSlotView : MonoBehaviour,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private Text amountLabel;
        [SerializeField] private Text captionLabel;

        private IInventoryUiHost host;

        public ContainerGridView Owner { get; private set; }
        public int Index { get; private set; }
        public bool IsEmpty { get; private set; } = true;

        public void Bind(ContainerGridView owner, IInventoryUiHost uiHost, int index, Sprite slotSprite)
        {
            Owner = owner;
            host = uiHost;
            Index = index;
            if (background != null && slotSprite != null)
            {
                background.sprite = slotSprite;
                background.type = Image.Type.Sliced;
            }
        }

        public void SetContent(Sprite itemIcon, string amountText, string caption, Color emptyIconColor)
        {
            IsEmpty = itemIcon == null && string.IsNullOrEmpty(amountText);
            if (icon != null)
            {
                icon.sprite = itemIcon;
                icon.enabled = itemIcon != null;
                icon.color = itemIcon != null ? Color.white : emptyIconColor;
            }

            if (amountLabel != null)
            {
                amountLabel.text = amountText;
            }

            if (captionLabel != null)
            {
                captionLabel.text = caption;
            }
        }

        public void SetTint(Color color)
        {
            if (background != null)
            {
                background.color = color;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (host != null)
            {
                host.OnSlotClicked(this, eventData.button == PointerEventData.InputButton.Right);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (host != null)
            {
                host.OnSlotHoverEnter(this);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (host != null)
            {
                host.OnSlotHoverExit(this);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (host != null)
            {
                host.OnSlotDragBegin(this);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (host != null)
            {
                host.OnSlotDragMoved(eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (host != null)
            {
                host.OnSlotDragEnd();
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (host != null)
            {
                host.OnSlotDropped(this);
            }
        }
    }

}
