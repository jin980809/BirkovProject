using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainSlideShow : MonoBehaviour
{
    [Header("슬라이드에 사용할 이미지들")]
    [SerializeField] private Sprite[] bgSprites;

    [Header("UI 이미지 컴포넌트 2개 (교차 페이드용)")]
    [SerializeField] private Image imgCanvas1;
    [SerializeField] private Image imgCanvas2;

    [Header("설정")]
    [SerializeField] private float displayTime = 4.0f;
    [SerializeField] private float fadeTime = 1.5f;

    private int currentIndex = 0;

    private void Start()
    {
        imgCanvas1.sprite = bgSprites[0];
        imgCanvas1.color = new Color(1, 1, 1, 1);
        imgCanvas2.color = new Color(0,0,0,0);
        StartCoroutine(SlideShow());
    }
    private IEnumerator SlideShow()
    {
        while (true)
        {
            yield return new WaitForSeconds(displayTime);
            int nextIndex = (currentIndex + 1) % bgSprites.Length;
            if (imgCanvas1.color.a == 1f)
            {
                yield return StartCoroutine(FadeOut(imgCanvas1));
                imgCanvas2.sprite = bgSprites[nextIndex];
                imgCanvas2.color = new Color(0, 0, 0, 1);
                yield return StartCoroutine(FadeIn(imgCanvas2));
            }
            else
            {
                yield return StartCoroutine(FadeOut(imgCanvas2));
                imgCanvas1.sprite = bgSprites[nextIndex];
                imgCanvas1.color = new Color(0, 0, 0, 1);
                yield return StartCoroutine(FadeIn(imgCanvas1));
            }
            currentIndex = nextIndex;
        }
    }

    private IEnumerator FadeOut(Image targetImg)
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / fadeTime);
            float colorVal = 1f - progress;
            targetImg.color = new Color(colorVal,colorVal,colorVal,1f);
            yield return null;
        }
        targetImg.color = new Color(0f, 0f, 0f, 1f);
    }
    private IEnumerator FadeIn(Image targetImg)
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / fadeTime);
            targetImg.color = new Color(progress,progress,progress,1f);
            yield return null;
        }
        targetImg.color = new Color(1f,1f,1f, 1f);
    }
}
