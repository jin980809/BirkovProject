using UnityEngine;
using UnityEngine.UI;

// MonoBehaviour 는 파일 이름과 클래스 이름이 같아야 한다.
namespace Birdkov.NaYeongMin.Ui
{
    // 아이템 설명 툴팁. 슬롯 위에 올릴 때만 켠다.
    public sealed class ItemTooltipView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image background;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text bodyLabel;
        [SerializeField] private Vector2 pointerOffset = new Vector2(18f, -18f);

        public void ApplyTheme(InventoryUiTheme theme)
        {
            if (theme == null || background == null || theme.TooltipSprite == null)
            {
                return;
            }

            background.sprite = theme.TooltipSprite;
            background.type = Image.Type.Sliced;
        }

        public void Show(string title, string body, Vector2 screenPosition)
        {
            if (root == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = title;
            }

            if (bodyLabel != null)
            {
                bodyLabel.text = body;
            }

            root.gameObject.SetActive(true);
            Move(screenPosition);
        }

        public void Move(Vector2 screenPosition)
        {
            if (root != null && root.gameObject.activeSelf)
            {
                root.position = new Vector3(screenPosition.x + pointerOffset.x, screenPosition.y + pointerOffset.y, 0f);
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.gameObject.SetActive(false);
            }
        }
    }
}
