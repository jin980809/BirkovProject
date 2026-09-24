using UnityEngine;
using UnityEngine.UI;

// 화면에 고정된 HUD: 체력 / 허기 / 수분 바 + 체력 숫자 + 장탄수.
//  - 체력(Slider_red): "현재 / 최대" 숫자 + 감소 잔상(깎인 만큼 밝은 바가 잠깐 남았다가 천천히 따라 줄어든다)
//  - 허기(Hunger) / 수분(Moisture): 기준 비율 이하로 떨어지면 지정한 이미지를 경고 색으로 바꾸고, 다시 오르면 원래 색
//  - 장탄수: 지금 선택된(손에 든) 무기 슬롯의 "현재 장탄수 / 최대 장탄수"만 보여주고 다른 슬롯 쪽은 끈다. 재장전 중에도 현재 값을 그대로 보여준다.
//
// 에디터 세팅
//  - 슬라이더는 조작용이 아니므로 Interactable 은 코드에서 끈다.
//  - 체력 잔상 이미지: Slider_red 의 Fill 과 같은 크기/위치로 Fill 보다 "뒤"(하이어라키 위쪽)에 Image 를 하나 두고,
//    Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left 로 설정한 뒤 healthTrailImage 에 연결한다.
//    잔상 색은 이미지 색 그대로 쓴다 (밝은 흰색/노랑 추천).
public class PlayerHUD : MonoBehaviour, ISceneRebindable
{
    [Header("연결 (비우면 씬에서 찾음)")]
    [SerializeField] private PlayerVitals vitals;
    [SerializeField] private WeaponController weapon;

    [Header("체력")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Text healthText;
    [Tooltip("감소 잔상 이미지 (Image Type = Filled). 비워두면 잔상 없음")]
    [SerializeField] private Image healthTrailImage;
    [Tooltip("체력이 깎인 뒤 잔상이 줄어들기 시작할 때까지 기다리는 시간(초)")]
    [SerializeField] private float healthTrailDelay = 0.4f;
    [Tooltip("잔상이 줄어드는 속도 (초당 최대 체력 대비 비율, 1 = 1초에 바 전체)")]
    [SerializeField] private float healthTrailSpeed = 0.8f;

    [Header("허기")]
    [SerializeField] private Slider hungerSlider;
    [Tooltip("허기가 기준 이하일 때 경고 색으로 바꿀 이미지")]
    [SerializeField] private Image hungerWarningImage;
    [SerializeField, Range(0f, 1f)] private float hungerWarningThreshold = 0.2f;

    [Header("수분")]
    [SerializeField] private Slider waterSlider;
    [Tooltip("수분이 기준 이하일 때 경고 색으로 바꿀 이미지")]
    [SerializeField] private Image waterWarningImage;
    [SerializeField, Range(0f, 1f)] private float waterWarningThreshold = 0.2f;

    [Header("경고 색")]
    [SerializeField] private Color warningColor = Color.red;

    [Header("장탄수 (무기 슬롯별)")]
    [Tooltip("1번 무기 슬롯 장탄수 텍스트")]
    [SerializeField] private Text slot1AmmoText;
    [Tooltip("1번 슬롯을 선택하지 않았을 때 끌 오브젝트 (아이콘+텍스트 묶음 등). 비우면 텍스트 오브젝트를 끈다")]
    [SerializeField] private GameObject slot1AmmoRoot;
    [Tooltip("2번 무기 슬롯 장탄수 텍스트")]
    [SerializeField] private Text slot2AmmoText;
    [Tooltip("2번 슬롯을 선택하지 않았을 때 끌 오브젝트 (아이콘+텍스트 묶음 등). 비우면 텍스트 오브젝트를 끈다")]
    [SerializeField] private GameObject slot2AmmoRoot;

    [Header("무기 내구도 (퀵슬롯별)")]
    [Tooltip("1번 무기 슬롯 내구도 슬라이더. 장탄수와 달리 손에 들었는지와 상관없이 그 슬롯에 무기가 꽂혀만 있으면 켜진다")]
    [SerializeField] private Slider slot1DurabilitySlider;
    [Tooltip("1번 슬롯이 비어 있거나 내구도가 없는 아이템일 때 끌 오브젝트. 비우면 슬라이더 오브젝트를 끈다")]
    [SerializeField] private GameObject slot1DurabilityRoot;
    [Tooltip("2번 무기 슬롯 내구도 슬라이더")]
    [SerializeField] private Slider slot2DurabilitySlider;
    [Tooltip("2번 슬롯이 비어 있거나 내구도가 없는 아이템일 때 끌 오브젝트. 비우면 슬라이더 오브젝트를 끈다")]
    [SerializeField] private GameObject slot2DurabilityRoot;
    [Tooltip("내구도가 이 비율 이하로 떨어지면 슬라이더 채움 색을 경고 색으로 바꾼다")]
    [SerializeField, Range(0f, 1f)] private float durabilityWarningThreshold = 0.3f;

    private Color hungerOriginalColor;
    private Color waterOriginalColor;

    private Image slot1DurabilityFillImage;
    private Image slot2DurabilityFillImage;
    private Color slot1DurabilityOriginalColor;
    private Color slot2DurabilityOriginalColor;

    private float healthTrailValue = 1f; // 0~1, 잔상 이미지 fillAmount
    private float lastHealthNormalized = 1f;
    private float healthTrailWaitUntil;

    // 문자열은 값이 바뀔 때만 새로 만든다 (매 프레임 문자열 할당 방지)
    private int lastHealthShown = -1;
    private int lastMaxHealthShown = -1;
    private readonly int[] lastAmmoShown = { -1, -1 };
    private readonly int[] lastReserveShown = { -1, -1 };

    private void Awake()
    {
        RebindSceneReferences();

        MakeDisplayOnly(healthSlider);
        MakeDisplayOnly(hungerSlider);
        MakeDisplayOnly(waterSlider);
        MakeDisplayOnly(slot1DurabilitySlider);
        MakeDisplayOnly(slot2DurabilitySlider);

        if (hungerWarningImage != null)
        {
            hungerOriginalColor = hungerWarningImage.color;
        }

        if (waterWarningImage != null)
        {
            waterOriginalColor = waterWarningImage.color;
        }

        CaptureFillColor(slot1DurabilitySlider, out slot1DurabilityFillImage, out slot1DurabilityOriginalColor);
        CaptureFillColor(slot2DurabilitySlider, out slot2DurabilityFillImage, out slot2DurabilityOriginalColor);
    }

    // 슬라이더의 Fill 이미지와 원래 색을 미리 기억해 둔다 - 경고 색으로 바꿨다가 되돌릴 때 쓴다
    private void CaptureFillColor(Slider slider, out Image fillImage, out Color originalColor)
    {
        fillImage = null;
        originalColor = Color.white;

        if (slider != null && slider.fillRect != null)
        {
            fillImage = slider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                originalColor = fillImage.color;
            }
        }
    }

    // 씬이 바뀌면 플레이어가 새로 생기므로 다시 찾는다 (이 UI 가 씬을 넘어서 유지될 때 - PersistentUiRoot)
    public void RebindSceneReferences()
    {
        vitals = FindAnyObjectByType<PlayerVitals>();
        weapon = FindAnyObjectByType<WeaponController>();
    }

    private void Start()
    {
        if (vitals != null)
        {
            lastHealthNormalized = vitals.HealthNormalized;
            healthTrailValue = lastHealthNormalized;
        }
    }

    private void Update()
    {
        if (vitals != null)
        {
            UpdateHealth();
            UpdateBar(hungerSlider, vitals.Hunger, vitals.MaxHunger);
            UpdateBar(waterSlider, vitals.Water, vitals.MaxWater);
            UpdateWarning(hungerWarningImage, vitals.HungerNormalized, hungerWarningThreshold, hungerOriginalColor);
            UpdateWarning(waterWarningImage, vitals.WaterNormalized, waterWarningThreshold, waterOriginalColor);
        }

        UpdateAmmo();
        UpdateDurability();
    }

    private void UpdateHealth()
    {
        UpdateBar(healthSlider, vitals.Health, vitals.MaxHealth);

        // 숫자는 올림해서 보여준다 - 0.3 남았는데 "0" 으로 보이면 죽은 것처럼 보이기 때문
        int shownHealth = Mathf.CeilToInt(vitals.Health);
        int shownMaxHealth = Mathf.RoundToInt(vitals.MaxHealth);
        if (healthText != null && (shownHealth != lastHealthShown || shownMaxHealth != lastMaxHealthShown))
        {
            healthText.text = shownHealth + " / " + shownMaxHealth;
            lastHealthShown = shownHealth;
            lastMaxHealthShown = shownMaxHealth;
        }

        UpdateHealthTrail();
    }

    // 체력이 깎이면 잔상은 그 자리에 잠깐 머물렀다가(healthTrailDelay) 현재 체력까지 천천히 줄어든다.
    // 회복하면 잔상은 기다리지 않고 바로 현재 체력에 맞춘다 (잔상이 체력 바보다 짧으면 가려져서 안 보이므로).
    private void UpdateHealthTrail()
    {
        float healthNormalized = vitals.HealthNormalized;

        if (healthNormalized < lastHealthNormalized)
        {
            healthTrailWaitUntil = Time.time + healthTrailDelay;
        }

        if (healthNormalized >= healthTrailValue)
        {
            healthTrailValue = healthNormalized;
        }
        else if (Time.time >= healthTrailWaitUntil)
        {
            healthTrailValue = Mathf.MoveTowards(healthTrailValue, healthNormalized, healthTrailSpeed * Time.deltaTime);
        }

        lastHealthNormalized = healthNormalized;

        if (healthTrailImage != null)
        {
            healthTrailImage.fillAmount = healthTrailValue;
        }
    }

    private void UpdateAmmo()
    {
        UpdateSlotAmmo(0, slot1AmmoText, slot1AmmoRoot);
        UpdateSlotAmmo(1, slot2AmmoText, slot2AmmoRoot);
    }

    // 내구도(UpdateSlotDurability)와 달리, 지금 실제로 손에 들고 있는(HasWeaponEquipped) 슬롯의
    // "현재 장전량 / 가방에 보유한 탄환수"만 보여준다. 집어넣은(Holster) 상태거나 그 슬롯이 아니면 꺼진다.
    // 재장전 중에도 현재 값을 그대로 보여준다.
    // 이 스크립트는 끄는 오브젝트와 다른 곳(Canvas)에 있으므로 꺼진 뒤에도 계속 돌면서 다시 켤 수 있다.
    private void UpdateSlotAmmo(int slotIndex, Text ammoText, GameObject root)
    {
        if (ammoText != null)
        {
            int ammo = 0;
            int reserve = 0;
            bool isHeldSlot = weapon != null && weapon.HasWeaponEquipped && weapon.EquippedSlotIndex == slotIndex;
            bool visible = isHeldSlot && weapon.TryGetSlotAmmo(slotIndex, out ammo, out reserve);

            GameObject toggleTarget = root;
            if (toggleTarget == null)
            {
                toggleTarget = ammoText.gameObject;
            }

            if (toggleTarget.activeSelf != visible)
            {
                toggleTarget.SetActive(visible);
            }

            if (visible && (ammo != lastAmmoShown[slotIndex] || reserve != lastReserveShown[slotIndex]))
            {
                ammoText.text = ammo + " / " + reserve;
                lastAmmoShown[slotIndex] = ammo;
                lastReserveShown[slotIndex] = reserve;
            }
        }
    }

    private void UpdateDurability()
    {
        UpdateSlotDurability(0, slot1DurabilitySlider, slot1DurabilityRoot, slot1DurabilityFillImage, slot1DurabilityOriginalColor);
        UpdateSlotDurability(1, slot2DurabilitySlider, slot2DurabilityRoot, slot2DurabilityFillImage, slot2DurabilityOriginalColor);
    }

    // 장탄수(UpdateSlotAmmo)와 달리 손에 든 슬롯인지는 보지 않는다 - 그 퀵슬롯에 내구도 있는 무기가
    // 꽂혀 있기만 하면 켜지고, 비어 있으면 꺼진다.
    private void UpdateSlotDurability(int slotIndex, Slider slider, GameObject root, Image fillImage, Color originalColor)
    {
        if (slider == null)
        {
            return;
        }

        int remaining = 0;
        int maximum = 0;
        bool visible = weapon != null && weapon.TryGetSlotDurability(slotIndex, out remaining, out maximum);

        GameObject toggleTarget = root != null ? root : slider.gameObject;
        if (toggleTarget.activeSelf != visible)
        {
            toggleTarget.SetActive(visible);
        }

        if (!visible)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = maximum;
        slider.value = remaining;

        if (fillImage != null)
        {
            float normalized = maximum > 0 ? (float)remaining / maximum : 0f;
            fillImage.color = normalized <= durabilityWarningThreshold ? warningColor : originalColor;
        }
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

    private void UpdateWarning(Image image, float normalized, float threshold, Color originalColor)
    {
        if (image != null)
        {
            if (normalized <= threshold)
            {
                image.color = warningColor;
            }
            else
            {
                image.color = originalColor;
            }
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
