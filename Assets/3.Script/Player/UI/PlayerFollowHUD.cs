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
public class PlayerFollowHUD : MonoBehaviour, ISceneRebindable
{
    [Header("연결 (비우면 씬에서 찾음)")]
    [SerializeField] private PlayerVitals vitals;
    [Tooltip("플레이어를 따라가게 할 ScreenAnchoredUI. 비우면 이 오브젝트 → 자식 순서로 찾는다")]
    [SerializeField] private ScreenAnchoredUI followAnchor;

    [Header("체력")]
    [SerializeField] private Slider healthSlider;
    [Tooltip("체력 바에 피격 애니메이션 + 딜레이 트레일이 있는 버전(KTS 의 HpEffect)을 쓸 때 연결한다. " +
             "비우면 healthSlider 의 부모에서 자동으로 찾고, 그래도 없으면 이 효과 없이 슬라이더 값만 바로 갱신한다. " +
             "HpEffect 자체는 건드리지 않고 공개 API(Initialize/SetHealth)만 쓴다")]
    [SerializeField] private HpEffect healthEffect;

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

    // HpEffect(피격 히트 애니메이션)가 회복할 때는 재생되지 않게 막는 데 쓴다. HpEffect.SetHealth 는
    // 값이 늘었는지 줄었는지 확인하지 않고 호출될 때마다 무조건 이 트리거 중 하나를 건다.
    private readonly string[] HpMoveTriggers = { "HpMove0", "HpMove1", "HpMove2" };

    private Image staminaFillImage;
    private Color staminaOriginalColor;
    private float staminaFullSince = -1f; // 가득 찬 상태가 시작된 시각 (-1 = 지금 가득 차 있지 않음)

    private Animator healthEffectAnimator;
    private float lastHealthValue;

    private void Awake()
    {
        if (healthEffect == null && healthSlider != null)
        {
            healthEffect = healthSlider.GetComponentInParent<HpEffect>();
        }

        if (healthEffect != null)
        {
            healthEffectAnimator = healthEffect.GetComponent<Animator>();
        }

        RebindSceneReferences();

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

    // 씬이 바뀌면 플레이어가 새로 생기므로 다시 찾아서 따라갈 대상을 갱신한다 (PersistentUiRoot 가 호출)
    public void RebindSceneReferences()
    {
        UnsubscribeHealth();

        vitals = FindAnyObjectByType<PlayerVitals>();

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

        SubscribeHealth();
    }

    private void OnDestroy()
    {
        UnsubscribeHealth();
    }

    // 체력은 매 프레임 폴링하지 않고 PlayerVitals.HealthChanged 이벤트가 올 때만 반영한다 - HpEffect.SetHealth
    // 를 매 프레임 부르면 안 맞았는데도 매번 피격 애니메이션(랜덤 HpMove 트리거)이 재생돼버리기 때문이다.
    private void SubscribeHealth()
    {
        if (vitals == null)
        {
            return;
        }

        if (healthEffect != null)
        {
            // 씬을 새로 바인딩할 때마다(새 플레이어) 최대 체력 기준으로 다시 초기화한다.
            // PlayerVitals.Awake() 가 씬이 로드될 때마다 체력을 항상 maxHealth 로 리셋하므로,
            // 여기서는 이 초기화만으로 이미 시작 상태가 맞다 - Initialize() 는 애니메이터를 건드리지 않는다.
            // (SetHealth 를 여기서 한 번 더 불러 "지금 값"에 맞추려 하면, HpEffect.SetHealth 가 값이
            // 바뀌었는지 확인하지 않고 호출될 때마다 무조건 피격 애니메이션을 재생해서 시작할 때마다 한 번씩 튄다)
            healthEffect.Initialize(vitals.MaxHealth);
        }
        else
        {
            // HpEffect 가 없는 씬은 애니메이터 트리거가 없는 단순 슬라이더라 부작용 없이 바로 값을 맞춘다
            UpdateBar(healthSlider, vitals.Health, vitals.MaxHealth);
        }

        lastHealthValue = vitals.Health;
        vitals.HealthChanged += HandleHealthChanged;
    }

    private void UnsubscribeHealth()
    {
        if (vitals != null)
        {
            vitals.HealthChanged -= HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (healthEffect != null)
        {
            bool isDamage = current < lastHealthValue;

            healthEffect.SetHealth(current);

            // 회복(또는 변화 없음)이면 HpEffect 가 이번 호출로 건 피격 애니메이션 트리거를, 애니메이터가
            // 아직 소비하기 전에 취소한다 - HpEffect.cs 자체는 건드리지 않고 이렇게 바깥에서 막는다.
            if (!isDamage && healthEffectAnimator != null)
            {
                for (int i = 0; i < HpMoveTriggers.Length; i++)
                {
                    healthEffectAnimator.ResetTrigger(HpMoveTriggers[i]);
                }
            }
        }
        else
        {
            UpdateBar(healthSlider, current, max);
        }

        lastHealthValue = current;
    }

    private void Update()
    {
        if (vitals != null)
        {
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

    private void UpdateBar(Slider slider, float current, float max)
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = Mathf.Max(0.0001f, max);
            slider.value = current;
        }
    }

    private void MakeDisplayOnly(Slider slider)
    {
        if (slider != null)
        {
            slider.interactable = false;
            slider.transition = Selectable.Transition.None; // 비활성 색(회색)으로 바뀌지 않게
        }
    }
}
