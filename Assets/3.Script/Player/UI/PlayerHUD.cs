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
public class PlayerHUD : MonoBehaviour
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

    private Color hungerOriginalColor;
    private Color waterOriginalColor;

    private float healthTrailValue = 1f; // 0~1, 잔상 이미지 fillAmount
    private float lastHealthNormalized = 1f;
    private float healthTrailWaitUntil;

    // 문자열은 값이 바뀔 때만 새로 만든다 (매 프레임 문자열 할당 방지)
    private int lastHealthShown = -1;
    private int lastMaxHealthShown = -1;
    private readonly int[] lastAmmoShown = { -1, -1 };
    private readonly int[] lastMagazineShown = { -1, -1 };

    private void Awake()
    {
        if (vitals == null)
        {
            vitals = FindAnyObjectByType<PlayerVitals>();
        }

        if (weapon == null)
        {
            weapon = FindAnyObjectByType<WeaponController>();
        }

        MakeDisplayOnly(healthSlider);
        MakeDisplayOnly(hungerSlider);
        MakeDisplayOnly(waterSlider);

        if (hungerWarningImage != null)
        {
            hungerOriginalColor = hungerWarningImage.color;
        }

        if (waterWarningImage != null)
        {
            waterOriginalColor = waterWarningImage.color;
        }
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

    // 지금 손에 든(선택된) 무기 슬롯의 장탄수만 보여주고, 나머지 슬롯은 오브젝트를 끈다.
    // 선택된 슬롯이 비어 있으면(손에 든 무기 없음) 둘 다 꺼진다. 재장전 중에도 현재 값을 그대로 보여준다.
    // 이 스크립트는 끄는 오브젝트와 다른 곳(Canvas)에 있으므로 꺼진 뒤에도 계속 돌면서 다시 켤 수 있다.
    private void UpdateSlotAmmo(int slotIndex, Text ammoText, GameObject root)
    {
        if (ammoText != null)
        {
            int ammo = 0;
            int magazine = 0;
            bool isSelected = weapon != null && weapon.EquippedSlotIndex == slotIndex;
            bool visible = isSelected && weapon.TryGetSlotAmmo(slotIndex, out ammo, out magazine);

            GameObject toggleTarget = root;
            if (toggleTarget == null)
            {
                toggleTarget = ammoText.gameObject;
            }

            if (toggleTarget.activeSelf != visible)
            {
                toggleTarget.SetActive(visible);
            }

            if (visible && (ammo != lastAmmoShown[slotIndex] || magazine != lastMagazineShown[slotIndex]))
            {
                ammoText.text = ammo + " / " + magazine;
                lastAmmoShown[slotIndex] = ammo;
                lastMagazineShown[slotIndex] = magazine;
            }
        }
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

    private static void MakeDisplayOnly(Slider slider)
    {
        if (slider != null)
        {
            slider.interactable = false;
            slider.transition = Selectable.Transition.None; // 비활성 색(회색)으로 바뀌지 않게
        }
    }
}
