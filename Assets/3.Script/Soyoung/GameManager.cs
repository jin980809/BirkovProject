using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{    
    //여기는 가이드 팝업용
    public Button openGuide;
    public GameObject guidePopup;
    public Image displayImg;
    public Button nextImg;
    public Sprite[] guideImg;
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
        displayImg.sprite = guideImg[currentIndex];
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
        displayImg.sprite = guideImg[currentIndex];
        StartCoroutine(ShowButton());   //다음 이미지 넘어갈 때 버튼이 시간차로 안 뜨게 하고 싶다면 이 부분 삭제 가능!
    }

}
