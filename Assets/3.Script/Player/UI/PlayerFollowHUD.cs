using UnityEngine;
using UnityEngine.UI;

// 플레이어를 따라다니는 UI: 체력 바 + 스테미나 바.
//  - 체력(PlayerHP): 항상 보인다.
//  - 스테미나(Stemina): 줄거나 차는 중(가득 차지 않은 동안)에는 보이고, 가득 차면 fullHideDelay 초 뒤에
//    fadeDuration 동안 서서히 사라진다. 다시 줄기 시작하면 바로 나타난다.
//    탈진 상태(0 이 된 뒤 다시 달릴 수 있을 만큼 찰 때까지)에는 채움 색을 exhaustedColor 로 바꾼다.
//
// 위치는 이 스크립트가 아니라 ScreenAnchoredUI 가 담당한다.
// 두 바를 빈 부모 오브젝트 하나로 묶고 그 부모에 ScreenAnchoredUI 와 이 스크립트를 같이 붙이면 같이 따라다닌다.
// ScreenAnchoredUI 의 Anchor 는 이 스크립트가 시작할 때 플레이어(PlayerVitals)로 자동 연결한다.
public class PlayerFollowHUD : MonoBehaviour
{
    [Header("연결 (비우면 씬에서 찾음)")]
    [SerializeField] private PlayerVitals vitals;
    [Tooltip("플레이어를 따라가게 할 ScreenAnchoredUI. 비우면 이 오브젝트 → 자식 순서로 찾는다")]
    [SerializeField] private ScreenAnchoredUI followAnchor;

    [Header("체력")]
    [SerializeField] private Slider healthSlider;

    [Header("스테미나")]
    [SerializeField] private Slider staminaSlider;
    [Tooltip("스테미나 바 전체를 페이드시킬 CanvasGroup. 비우면 staminaSlider 오브젝트에 자동으로 붙인다")]
    [SerializeField] private CanvasGroup staminaGroup;
    [Tooltip("가득 찬 뒤 사라지기 시작할 때까지 기다리는 시간(초)")]
    [SerializeField] private float fullHideDelay = 1f;
    [Tooltip("사라지는 데 걸리는 시간(초)")]
    [SerializeField] private float fadeDuration = 0.4f;
    [Tooltip("탈진 상태일 때 스테미나 채움 색")]
    [SerializeField] private Color exhaustedColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    private Image staminaFillImage;
    private Color staminaOriginalColor;
    private float staminaFullSince = -1f; // 가득 찬 상태가 시작된 시각 (-1 = 지금 가득 차 있지 않음)

    private void Awake()
    {
        if (vitals == null)
        {
            vitals = FindAnyObjectByType<PlayerVitals>();
        }

        // ScreenAnchoredUI 가 플레이어를 따라가게 한다. 인스펙터에서 Anchor 를 연결하지 않아도 되므로
        // 이 UI 를 프리팹으로 만들어 로비/전투 씬마다 넣어도 그 씬의 플레이어에 자동으로 붙는다.
        if (followAnchor == null)
        {
            followAnchor = GetComponentInChildren<ScreenAnchoredUI>(true);
        }

        if (vitals == null)
        {
            Debug.LogWarning("PlayerFollowHUD: 씬에서 PlayerVitals 를 찾지 못해 플레이어를 따라갈 수 없습니다.", this);
        }
        else if (followAnchor == null)
        {
            Debug.LogWarning("PlayerFollowHUD: ScreenAnchoredUI 를 찾지 못했습니다. 이 오브젝트(FollowHUD)에 ScreenAnchoredUI 를 붙이거나 " +
                             "Follow Anchor 필드에 연결하세요.", this);
        }
        else
        {
            followAnchor.SetAnchor(vitals.transform);
        }

        MakeDisplayOnly(healthSlider);
        MakeDisplayOnly(staminaSlider);

        if (staminaSlider != null)
        {
            if (staminaGroup == null && !staminaSlider.TryGetComponent(out staminaGroup))
            {
                staminaGroup = staminaSlider.gameObject.AddComponent<CanvasGroup>();
            }

            if (staminaSlider.fillRect != null)
            {
                staminaFillImage = staminaSlider.fillRect.GetComponent<Image>();
            }

            if (staminaFillImage != null)
            {
                staminaOriginalColor = staminaFillImage.color;
            }
        }

        if (staminaGroup != null)
        {
            staminaGroup.alpha = 0f; // 시작할 땐 가득 차 있으므로 숨긴 채 시작
            staminaGroup.blocksRaycasts = false;
            staminaGroup.interactable = false;
        }
    }

    private void Update()
    {
        if (vitals != null)
        {
            UpdateBar(healthSlider, vitals.Health, vitals.MaxHealth);
            UpdateStamina();
        }
    }

    private void UpdateStamina()
    {
        UpdateBar(staminaSlider, vitals.Stamina, vitals.MaxStamina);

        if (staminaFillImage != null)
        {
            if (vitals.IsExhausted)
            {
                staminaFillImage.color = exhaustedColor;
            }
            else
            {
                staminaFillImage.color = staminaOriginalColor;
            }
        }

        if (staminaGroup != null)
        {
            staminaGroup.alpha = GetStaminaAlpha();
        }
    }

    // 가득 차지 않았으면 1. 가득 찼으면 fullHideDelay 동안 1 을 유지하다가 fadeDuration 동안 0 으로.
    private float GetStaminaAlpha()
    {
        float alpha = 1f;
        bool isFull = vitals.Stamina >= vitals.MaxStamina;

        if (isFull)
        {
            if (staminaFullSince < 0f)
            {
                staminaFullSince = Time.time;
            }

            float fadeElapsed = Time.time - staminaFullSince - fullHideDelay;
            if (fadeElapsed > 0f)
            {
                if (fadeDuration > 0f)
                {
                    alpha = 1f - Mathf.Clamp01(fadeElapsed / fadeDuration);
                }
                else
                {
                    alpha = 0f;
                }
            }

            // 시작 직후(한 번도 줄어든 적 없음)에는 기다리지 않고 숨긴 상태 유지
            if (staminaGroup.alpha <= 0f)
            {
                alpha = 0f;
            }
        }
        else
        {
            staminaFullSince = -1f;
        }

        return alpha;
    }

    private static void UpdateBar(Slider slider, float current, float max)
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = Mathf.Max(0.0001f, max);
            slider.value = current;
        }
    }

    private static void MakeDisplayOnly(Slider slider)
    {
        if (slider != null)
        {
            slider.interactable = false;
            slider.transition = Selectable.Transition.None; // 비활성 색(회색)으로 바뀌지 않게
        }
    }
}
