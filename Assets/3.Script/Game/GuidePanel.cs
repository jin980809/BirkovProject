using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 가이드 팝업. 이미지 여러 장을 버튼 하나로 한 장씩 넘기고, 마지막 장에서 누르면 스스로 꺼진다.
//
// 붙이는 곳: 가이드 루트 오브젝트(StartScene 의 GuidePop). 그 오브젝트를 켜고 끄는 것은
// StartMenu 의 Guide Panel 이 한다 - 여기서는 켜져 있는 동안의 진행만 담당한다.
//
// 켜질 때마다 항상 첫 장부터 다시 시작한다. 전환은 CanvasGroup 크로스페이드이고,
// fadeDuration 을 0 으로 두면 페이드 없이 즉시 교체된다.
// 각 이미지 오브젝트에 CanvasGroup 이 없으면 여기서 자동으로 붙인다.
public class GuidePanel : MonoBehaviour
{
    [Tooltip("보여줄 순서대로 넣는다 (Slide0, Slide1, ...). 여기 넣은 것만 쓰고 나머지는 건드리지 않는다")]
    [SerializeField] private GameObject[] slides;

    [Tooltip("다음 장으로 넘기는 버튼. 마지막 장에서 누르면 가이드가 꺼진다")]
    [SerializeField] private Button nextButton;

    [Tooltip("이미지가 교차되는 시간(초). 0 이면 즉시 교체")]
    [SerializeField] private float fadeDuration = 0.25f;

    private int currentIndex;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        // 페이드에 필요한 CanvasGroup 을 미리 확보해둔다 (인스펙터에서 일일이 붙이지 않아도 되게)
        for (int i = 0; i < slides.Length; i++)
        {
            EnsureCanvasGroup(slides[i]);
        }
    }

    // 이 오브젝트가 켜질 때마다 첫 장으로 되돌린다
    private void OnEnable()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
        }

        ShowFirstSlide();
    }

    private void OnDisable()
    {
        // 페이드 도중에 꺼졌으면 코루틴 기록을 지운다 (다시 켜질 때 남은 상태가 없게)
        fadeRoutine = null;

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
        }
    }

    private void ShowFirstSlide()
    {
        if (slides.Length == 0)
        {
            Debug.LogWarning("GuidePanel: 보여줄 이미지가 없습니다. Slides 에 이미지 오브젝트를 넣으세요.", this);
            return;
        }

        currentIndex = 0;

        for (int i = 0; i < slides.Length; i++)
        {
            bool isFirst = i == currentIndex;
            SetSlideVisible(slides[i], isFirst);
        }

        SetNextButtonInteractable(true);
    }

    private void OnNextClicked()
    {
        if (fadeRoutine != null)
        {
            return; // 전환 중에 연타로 여러 장을 건너뛰는 것을 막는다
        }

        int nextIndex = currentIndex + 1;

        if (nextIndex < slides.Length)
        {
            if (fadeDuration > 0f)
            {
                fadeRoutine = StartCoroutine(FadeToSlide(nextIndex));
            }
            else
            {
                SetSlideVisible(slides[currentIndex], false);
                SetSlideVisible(slides[nextIndex], true);
                currentIndex = nextIndex;
            }
        }
        else
        {
            Close(); // 마지막 장이었으면 가이드를 닫는다
        }
    }

    private IEnumerator FadeToSlide(int nextIndex)
    {
        SetNextButtonInteractable(false);

        CanvasGroup current = GetCanvasGroup(slides[currentIndex]);
        CanvasGroup next = GetCanvasGroup(slides[nextIndex]);

        // 다음 장을 투명한 상태로 미리 켜두고 겹쳐서 교차시킨다 (중간에 화면이 비지 않게)
        slides[nextIndex].SetActive(true);
        if (next != null)
        {
            next.alpha = 0f;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // 일시정지(timeScale 0) 중에도 넘어가게 한다
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            if (current != null)
            {
                current.alpha = 1f - t;
            }

            if (next != null)
            {
                next.alpha = t;
            }

            yield return null;
        }

        SetSlideVisible(slides[currentIndex], false);
        SetSlideVisible(slides[nextIndex], true);
        currentIndex = nextIndex;

        fadeRoutine = null;
        SetNextButtonInteractable(true);
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    private void SetSlideVisible(GameObject slide, bool visible)
    {
        if (slide == null)
        {
            return;
        }

        CanvasGroup group = GetCanvasGroup(slide);
        if (group != null)
        {
            group.alpha = 1f; // 다시 켤 때 이전 페이드의 알파가 남아 있지 않게 한다
        }

        slide.SetActive(visible);
    }

    private void SetNextButtonInteractable(bool interactable)
    {
        if (nextButton != null)
        {
            nextButton.interactable = interactable;
        }
    }

    private CanvasGroup GetCanvasGroup(GameObject slide)
    {
        CanvasGroup group = null;

        if (slide != null)
        {
            slide.TryGetComponent(out group);
        }

        return group;
    }

    private void EnsureCanvasGroup(GameObject slide)
    {
        if (slide != null && GetCanvasGroup(slide) == null)
        {
            slide.AddComponent<CanvasGroup>();
        }
    }
}
