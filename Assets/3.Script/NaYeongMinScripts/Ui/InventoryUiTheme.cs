using UnityEngine;

// HONETi flat_gui_elements 스프라이트 묶음. UI 4종이 같은 테마를 공유한다.
namespace Birdkov.NaYeongMin.Ui
{
    [CreateAssetMenu(fileName = "InventoryUiTheme", menuName = "Birdkov/NaYeongMin/Inventory UI Theme")]
    public sealed class InventoryUiTheme : ScriptableObject
    {
        [Header("패널")]
        [SerializeField] private Sprite windowSprite;
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Sprite titleSprite;
        [SerializeField] private Sprite separatorSprite;

        [Header("슬롯")]
        [SerializeField] private Sprite bagSlotSprite;
        [SerializeField] private Sprite equipmentSlotSprite;
        [SerializeField] private Sprite quickSlotSprite;
        [SerializeField] private Sprite externalSlotSprite;

        [Header("버튼")]
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite buttonHoverSprite;
        [SerializeField] private Sprite buttonDownSprite;

        [Header("툴팁")]
        [SerializeField] private Sprite tooltipSprite;

        [Header("슬롯 색")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.78f);
        [SerializeField] private Color dropAllowedColor = new Color(0.62f, 0.93f, 0.68f);
        [SerializeField] private Color dropBlockedColor = new Color(0.94f, 0.58f, 0.48f);
        [SerializeField] private Color emptyIconColor = new Color(1f, 1f, 1f, 0f);

        public Sprite WindowSprite => windowSprite;
        public Sprite PanelSprite => panelSprite;
        public Sprite TitleSprite => titleSprite;
        public Sprite SeparatorSprite => separatorSprite;
        public Sprite ButtonSprite => buttonSprite;
        public Sprite ButtonHoverSprite => buttonHoverSprite;
        public Sprite ButtonDownSprite => buttonDownSprite;
        public Sprite TooltipSprite => tooltipSprite;

        public Color NormalColor => normalColor;
        public Color HoverColor => hoverColor;
        public Color DropAllowedColor => dropAllowedColor;
        public Color DropBlockedColor => dropBlockedColor;
        public Color EmptyIconColor => emptyIconColor;

        public Sprite GetSlotSprite(UiContainerKind kind)
        {
            switch (kind)
            {
                case UiContainerKind.Equipment: return equipmentSlotSprite != null ? equipmentSlotSprite : bagSlotSprite;
                case UiContainerKind.WeaponQuick:
                case UiContainerKind.ItemQuick: return quickSlotSprite != null ? quickSlotSprite : bagSlotSprite;
                case UiContainerKind.External: return externalSlotSprite != null ? externalSlotSprite : bagSlotSprite;
                default: return bagSlotSprite;
            }
        }
    }

    // UI 그리드가 어떤 데이터를 비추는지 구분한다. 데이터 자체는 컨테이너별로 분리된다.
    public enum UiContainerKind
    {
        Bag,
        Equipment,
        WeaponQuick,
        ItemQuick,
        External
    }
}
