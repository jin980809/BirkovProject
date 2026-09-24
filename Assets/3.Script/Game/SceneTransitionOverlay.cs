using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 씬 전환 중 화면을 덮는 검은 오버레이(페이드 전용). GameSession 이 런타임에 만들어 쓰므로
// 씬에 미리 배치할 필요가 없다. 어떤 UI 보다도 위에 그려지도록 sortingOrder 를 크게 잡는다.
// 로딩 표시는 별도의 로딩 씬(LoadingSceneController)이 담당한다.
public class SceneTransitionOverlay : MonoBehaviour
{
    private const int SortingOrder = 30000;

    // 한 프레임에 페이드가 진행될 수 있는 시간의 상한(초). 무거운 씬이 활성화되는 프레임은 deltaTime 이
    // 1~2초씩 튀는데, 그대로 더하면 그 한 프레임에 페이드 시간이 다 소진돼서 밝아지는 게 안 보이고
    // 뚝 바뀐다. 상한을 두면 그런 프레임도 한 프레임 분량만 진행돼서 페이드가 끝까지 재생된다.
    private const float MaxFadeStep = 0.05f;

    private CanvasGroup group;

    // GameSession(싱글턴)이 자기 자신을 준비할 때 새 오브젝트를 만들고 여기에 붙인 뒤 부른다
    public void Build()
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
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxFadeStep);
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
