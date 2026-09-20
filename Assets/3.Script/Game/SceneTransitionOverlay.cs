using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 씬 전환 중 화면을 덮는 검은 오버레이(페이드 전용). GameSession 이 런타임에 만들어 쓰므로
// 씬에 미리 배치할 필요가 없다. 어떤 UI 보다도 위에 그려지도록 sortingOrder 를 크게 잡는다.
// 로딩 표시는 별도의 로딩 씬(LoadingSceneController)이 담당한다.
public class SceneTransitionOverlay : MonoBehaviour
{
    private const int SortingOrder = 30000;

    private CanvasGroup group;

    public static SceneTransitionOverlay Create()
    {
        GameObject root = new GameObject("SceneTransitionOverlay");
        DontDestroyOnLoad(root);

        SceneTransitionOverlay overlay = root.AddComponent<SceneTransitionOverlay>();
        overlay.Build();
        return overlay;
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        GameObject background = new GameObject("Background", typeof(RectTransform));
        background.transform.SetParent(transform, false);

        Image image = background.AddComponent<Image>();
        image.color = Color.black;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // 페이드 없이 즉시 적용한다 (게임 시작이나 씬 진입 직후처럼 첫 프레임부터 검게 덮어야 할 때)
    public void SetAlpha(float alpha)
    {
        group.alpha = alpha;
        group.blocksRaycasts = alpha > 0f;
    }

    // alpha 1 = 화면이 완전히 검은 상태
    public IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = group.alpha;
        float elapsed = 0f;

        group.blocksRaycasts = true; // 전환 중에는 아래 UI 클릭을 막는다

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        group.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            group.blocksRaycasts = false;
        }
    }
}
