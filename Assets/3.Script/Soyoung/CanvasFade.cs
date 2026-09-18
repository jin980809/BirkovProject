//using DG.Tweening;
using UnityEngine.InputSystem;
using UnityEngine;

public class CanvasFade : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("여기에 인벤토리 UI 3개 넣기")]
    public RectTransform leftPanel;
    public RectTransform rightPanel;
    public RectTransform bottomPanel;

    [Header("Animation Settings")]
    public float slideTime = 0.5f; //슬라이드 애니메이션 지속시간
    //public Ease slideEase = Ease.OutCubic;

    [Header("Panel Positions")]
    public Vector2 leftShowPos;
    public Vector2 leftHidePos;
    public Vector2 rightShowPos;
    public Vector2 righttHidePos;
    public Vector2 bottomShowPos;
    public Vector2 bottomHidePos;

    private bool isLeftOpen = true;
    private bool isRightOpen = true;
    private bool isBottomOpen = true;

    private bool isLeftSliding = true;
    private bool isRightSliding = true;
    private bool isBottomSliding = true;

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ShowPanel();
        }
    }
    public void ShowPanel()
    {
        isLeftSliding = true;
        isLeftOpen = !isLeftOpen;
        Vector2 targetPos = isLeftOpen ? leftShowPos : leftHidePos;
        //leftPanel.DOAnchorPos(targetPos, slideTime).SetEase(slideEase).OnComplete(delegate ()
        //{
        //   isLeftSliding = false;
        //});
    }
}
