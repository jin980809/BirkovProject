using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{    
    //여기는 가이드 팝업용
    public Button openGuide;
    public GameObject guidePopup;
    public Image displayImg;  //튜토팝업에 나올 이미지
    public Text displayText;  //튜토팝업에 나올 문구
    public Button nextImg;    //튜토팝업용 버튼
    public Text nextButtonText;   //튜토팝업용 텍스트
    public Sprite[] guideImg;
    [TextArea] public string[] guideTexts;
    public string nextLabel = "다음";
    public string lastLabel = "확인";

    private int currentIndex = 0;

    private void Start()
    {
        if (guidePopup != null)
        {
            guidePopup.SetActive(false);
        }
        if (nextImg != null)
        {
            nextImg.gameObject.SetActive(false);
        }
        if (openGuide != null)
        {
            openGuide.onClick.AddListener(OnStartGuideClicked);
        }
        if (nextImg != null)
        {
            nextImg.onClick.AddListener(OnNextClicked);
        }
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
        Application.Quit();
    }


    //여기 아래는 가이드 팝업 부분
    private void OnStartGuideClicked()
    {
        if (guidePopup == null || guideImg.Length == 0)
        {
            return;
        }
        currentIndex = 0;
        UpdateDisplay();
        guidePopup.SetActive(true);
        StartCoroutine(ShowButton());
    }

    private IEnumerator ShowButton()
    {
        nextImg.gameObject.SetActive(false);
        yield return new WaitForSecondsRealtime(0.8f);
        nextImg.gameObject.SetActive(true);
        nextImg.gameObject.SetActive(true);
    }

    private void OnNextClicked()
    {
        currentIndex++;
        if (currentIndex >= guideImg.Length)
        {
            guidePopup.SetActive(false);
            nextImg.gameObject.SetActive(false);
            return;
        }
        UpdateDisplay();
        StartCoroutine(ShowButton());   //다음 이미지 넘어갈 때 버튼이 시간차로 안 뜨게 하고 싶다면 이 부분 삭제 가능!
    }

    //페이드 전환 구현하기
    [Header("전환효과")]
    public float fadeTime = 0.25f;
    private Coroutine fadeCo;

    private void UpdateDisplay()
    {
        if (fadeCo != null)
        {
            StopCoroutine(fadeCo);
        }
        fadeCo = StartCoroutine(FadetoNew());
    }

    private IEnumerator FadetoNew()
    {
        yield return StartCoroutine(FadeImg(displayImg,1f,0f));
        displayImg.sprite = guideImg[currentIndex];

        if (displayText != null && guideTexts != null && currentIndex < guideTexts.Length)
        {
            displayText.text = guideTexts[currentIndex];
        }

        if (nextButtonText != null)
        {
            bool isLast = currentIndex == guideImg.Length - 1;
            nextButtonText.text = isLast ? lastLabel : nextLabel;
        }
        yield return StartCoroutine(FadeImg(displayImg, 0f, 1f));
    }
    private IEnumerator FadeImg(Image img, float from, float to)
    {
        float elapsed = 0f;
        Color c = img.color;

        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeTime);
            c.a = Mathf.Lerp(from, to, t);
            img.color = c;
            yield return null;
        }
        c.a = to;
        img.color = c;
    }
}
