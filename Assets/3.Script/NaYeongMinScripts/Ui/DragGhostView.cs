using UnityEngine;
using UnityEngine.UI;

// MonoBehaviour 는 파일 이름과 클래스 이름이 같아야 한다.
namespace Birdkov.NaYeongMin.Ui
{
    // 드래그 중 커서를 따라다니는 아이콘.
    public sealed class DragGhostView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image icon;
        [SerializeField] private Text amountLabel;

        public void Show(Sprite itemIcon, string amountText, Vector2 screenPosition)
        {
            if (root == null)
            {
                return;
            }

            if (icon != null)
            {
                icon.sprite = itemIcon;
                icon.enabled = itemIcon != null;
                icon.raycastTarget = false;
            }

            if (amountLabel != null)
            {
                amountLabel.text = amountText;
                amountLabel.raycastTarget = false;
            }

            root.gameObject.SetActive(true);
            Move(screenPosition);
        }

        public void Move(Vector2 screenPosition)
        {
            if (root != null && root.gameObject.activeSelf)
            {
                root.position = new Vector3(screenPosition.x, screenPosition.y, 0f);
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
