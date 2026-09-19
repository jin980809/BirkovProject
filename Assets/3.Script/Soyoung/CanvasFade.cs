//using DG.Tweening;
using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections;

public class CanvasFade : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("여기에 인벤토리 UI 3개 넣기")]
    public RectTransform leftPanel;
    public RectTransform rightPanel;
    public RectTransform bottomPanel;

    [Header("다른 UI / HUD  (인벤토리 열릴 때 꺼질 녀석)")]
    public GameObject others;

    [Header("Animation Settings")]
    public float slideTime = 0.5f; //슬라이드 애니메이션 지속시간

    [Header("Panel Positions")]
    public Vector2 leftShowPos;
    public Vector2 leftHidePos;
    public Vector2 rightShowPos;
    public Vector2 rightHidePos;
    public Vector2 bottomShowPos;
    public Vector2 bottomHidePos;

    private bool isOpen = false; //인벤 열려있니?
    private bool isSliding = false; //중복입력을 막기 위해 슬라이딩 도중인지 확인용
    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame && !isSliding)
        {
            ShowPanel();
        }
    }
    public void ShowPanel()
    {
        isOpen = !isOpen;
        if (isOpen)
        {
            if (others != null)
            {
                others.SetActive(false);
                StartCoroutine(SlideAll(leftShowPos, rightShowPos, bottomShowPos));
            }
            else
            {
                StartCoroutine(ActivateOther(leftHidePos, rightHidePos, bottomHidePos));
            }
        }
    }
    private IEnumerator SlideAll(Vector2 leftTarget, Vector2 rightTarget, Vector2 bottomTarget)
    {
        isSliding = true;
        StartCoroutine(SlidePanel(leftPanel,leftTarget));
        StartCoroutine(SlidePanel(rightPanel,rightTarget));
        yield return StartCoroutine(SlidePanel(bottomPanel,bottomTarget));
        isSliding = false;
    }
    private IEnumerator ActivateOther(Vector2 leftTarget, Vector2 rightTarget, Vector2 bottomTarget)
    {
        isSliding = true;
        StartCoroutine(SlidePanel(leftPanel, leftTarget));
        StartCoroutine(SlidePanel(rightPanel, rightTarget));
        yield return StartCoroutine(SlidePanel(bottomPanel, bottomTarget));
        if (others != null)
        {
            others.SetActive(true);
        }
        isSliding = false;
    }
    private IEnumerator SlidePanel(RectTransform panel, Vector2 targetPos)
    {
        if (panel == null)
        {
            yield break;
        }
        Vector2 startPos = panel.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < slideTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideTime;
            t = t * t * (3f - 2f * t); //스무스하게 감속하고 가속하는 코드라고 claude가 알려줌
            panel.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }
        panel.anchoredPosition = targetPos;
    }
}
