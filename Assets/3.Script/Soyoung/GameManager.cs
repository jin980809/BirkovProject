using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{    
    [Header("이 스크립트 안에 BGM 바꿀 수 있음")]
    //여기는 가이드 팝업용
    public Button openGuide;
    public GameObject guidePopup;
    public GameObject[] slides;
    public Button nextButton;
    public Text nextButtonText;
    public string nextLabel = "계 속";
    public string lastLabel = "확 인";

    private int currentIndex = 0;

    private void Start()
    {
        if (guidePopup != null)
        {
            guidePopup.SetActive(false);
        }
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
        }
        if (openGuide != null)
        {
            openGuide.onClick.AddListener(OnStartGuideClicked);
        }
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
        }
        for (int i = 0; i < slides.Length; i++)
        {
            slides[i].SetActive(false);
            EnsureCanvasGroup(slides[i]);
        }
        AudioManager.instance.PlayBGM("Main");    //여기에서 브금명 바꾸기
    }

    //ESC 일시정지용
    public GameObject MenuPanel;
    public bool escToggle = true;

    private void Update()
    {
        if (escToggle &&  Input.GetKeyDown(KeyCode.Escape))
        {
            bool isActive = !MenuPanel.activeSelf;
            MenuPanel.SetActive(isActive);
            if (isActive)
            {
                Time.timeScale = 0f;
            }
            else 
            {
                Time.timeScale = 1f;
            }
        }
    }
    public void GameExit()
    {
        Debug.Log("게임 종료");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


    //여기 아래는 가이드 팝업 부분
    private void OnStartGuideClicked()
    {
        if (guidePopup == null || slides.Length == 0)
        {
            return;
        }
        currentIndex = 0;
        slides[currentIndex].SetActive(true);
        GetCanvasGroup(slides[currentIndex]).alpha = 1f;
        UpdateNextLabel();
        guidePopup.SetActive(true);
        StartCoroutine(ShowButton());
    }

    private IEnumerator ShowButton()
    {
        nextButton.gameObject.SetActive(false);
        yield return new WaitForSecondsRealtime(0.8f);
        nextButton.gameObject.SetActive(true);
    }

    private void OnNextClicked()
    {
        int nextIndex = currentIndex + 1;
        if (nextIndex >= slides.Length)
        {
            guidePopup.SetActive(false);
            nextButton.gameObject.SetActive(false);
            return;
        }
        StartCoroutine(FadetoSlide(nextIndex));
    }

    //페이드 전환 구현하기
    [Header("전환효과")]
    public float fadeTime = 0.25f;
    private Coroutine fadeCo;

    private IEnumerator FadetoSlide(int newIndex)
    {
        CanvasGroup current = GetCanvasGroup(slides[currentIndex]);
        yield return StartCoroutine(FadeCanvas(current,1f,0f));
        slides[currentIndex].SetActive(false);

        currentIndex = newIndex;
        slides[currentIndex].SetActive(true);

        CanvasGroup next = GetCanvasGroup(slides[currentIndex]);
        next.alpha = 0f;
        yield return StartCoroutine(FadeCanvas(next, 0f, 1f));

        UpdateNextLabel();
        StartCoroutine(ShowButton());
    }
    private IEnumerator FadeCanvas(CanvasGroup cg, float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeTime));
            yield return null;
        }
        cg.alpha = to;
    }

    private void UpdateNextLabel()
    {
        bool isLast = currentIndex == slides.Length - 1;
        nextButtonText.text = isLast ? lastLabel : nextLabel;
    }
    private CanvasGroup GetCanvasGroup(GameObject slide) => slide.GetComponent<CanvasGroup>();

    private void EnsureCanvasGroup(GameObject slide)
    {
        if (slide.GetComponent<CanvasGroup>() == null)
        {
            slide.AddComponent<CanvasGroup>();
        }
    }


}
