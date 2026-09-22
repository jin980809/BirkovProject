using UnityEngine;
using UnityEngine.UI;

public class GuideMenu : MonoBehaviour
{
    [Header("가이드에 사용할 이미지들")]
    [SerializeField] private Sprite[] img;

    [Header("UI 컴포넌트 연결")]
    [SerializeField] private Image display;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;

    private int currentIndex = 0;

    private void Start()
    {
        currentIndex = 0;
        UpdateGuideUI();
        nextButton.onClick.AddListener(OnNextButtonClicked);
        closeButton.onClick.AddListener(ClosePopup) ;
    }
    private void OnNextButtonClicked()
    {
        currentIndex++;
        if (currentIndex >= img.Length)
        {
            ClosePopup();
            return;
        }
        UpdateGuideUI();
    }
    private void UpdateGuideUI()
    {
        display.sprite = img[currentIndex];
    }
    private void ClosePopup()
    {
        gameObject.SetActive(false);
    }
}
