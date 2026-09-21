using UnityEngine;
using UnityEngine.UI;

// 상호작용 진행 중 프로그레스 게이지(Slider). 화면 하단 고정 위치의 HUD 다.
// (에디터에서 잡아둔 RectTransform 위치를 그대로 쓰고, 스크립트는 활성화/진행률만 건드린다)
//
// "감지되면 뜨는 프롬프트 아이콘"은 이 스크립트가 담당하지 않는다 - 각 IInteractable 오브젝트가
// 필요할 때 프리팹으로 자기 아이콘을 만들어서(없으면 생성, 있으면 재사용) ShowPrompt()/HidePrompt() 로
// 스스로 켜고 끈다 (PlayerInteraction.cs, DebugInteractable.cs 참고). 그 아이콘들을 어디에 만들지는
// 이 스크립트가 정해준다 - PromptParent 가 이 HUD 캔버스 하위를 가리킨다.
//
// Screen Space - Overlay 캔버스 밑에 있어야 한다.
public class InteractionPromptUI : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private PlayerInteraction interaction;
    [Tooltip("아이템 사용(usageTime) 게이지도 이 슬라이더를 같이 쓴다")]
    [SerializeField] private ItemUseController itemUse;
    [Tooltip("재장전 게이지도 이 슬라이더를 같이 쓴다")]
    [SerializeField] private WeaponController weapon;
    [Tooltip("상호작용/아이템 사용/재장전 진행 중 진행률을 보여주는 Slider (0~1, Interactable 체크 해제). 위치는 화면 하단 고정 - 스크립트가 움직이지 않는다")]
    [SerializeField] private Slider progressSlider;

    // 각 상호작용 오브젝트가 자기 프롬프트 아이콘 프리팹을 Instantiate 할 때 부모로 쓰는 자리
    // (Screen Space - Overlay 캔버스 하위여야 UI 가 렌더링된다)
    public static Transform PromptParent { get; private set; }

    private void Awake()
    {
        PromptParent = transform;

        if (interaction == null)
        {
            interaction = FindAnyObjectByType<PlayerInteraction>();
        }

        if (itemUse == null)
        {
            itemUse = FindAnyObjectByType<ItemUseController>();
        }

        if (weapon == null)
        {
            weapon = FindAnyObjectByType<WeaponController>();
        }
    }

    private void Update()
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (progressSlider == null)
        {
            return;
        }

        bool interactionActive = interaction != null && interaction.IsInteracting;
        bool itemUseActive = itemUse != null && itemUse.IsUsing;
        bool reloadActive = weapon != null && weapon.IsReloading;
        bool showProgress = interactionActive || itemUseActive || reloadActive;

        // 위치는 건드리지 않는다 - 하단 고정, 에디터에서 잡아둔 자리 그대로
        progressSlider.gameObject.SetActive(showProgress);

        if (interactionActive)
        {
            progressSlider.value = interaction.InteractProgress01;
        }
        else if (itemUseActive)
        {
            progressSlider.value = itemUse.UseProgress01;
        }
        else if (reloadActive)
        {
            progressSlider.value = weapon.ReloadProgress01;
        }
    }
}
