using System;
using UnityEngine;

// 플레이어 생존 수치: 체력 / 스테미나 / 허기 / 수분
//  - 체력: 자동 재생 없음. TakeDamage / Heal 로만 변동. 0 이면 사망.
//  - 스테미나: 달리기로 소모, 안 달릴 때 회복.
//             0 이 되면 exhausted → sprintRecoverThreshold 까지 차야 다시 달리기 가능.
//  - 허기/수분: 시간에 따라 감소 (달릴 때 배속). 허기가 0 이면 초당 체력이 깎이고,
//              수분이 0 이면 체력 대신 스테미나 회복 속도가 줄어든다.
//  - 구르기: dodgeStaminaCost 만큼 소모, 부족하면 구르기 불가.
public class PlayerVitals : MonoBehaviour
{
    [Header("체력")]
    [SerializeField] private float maxHealth = 100f;

    [Header("스테미나")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainPerSecond = 20f;
    [SerializeField] private float staminaRegenPerSecond = 12f;
    [Tooltip("달리기를 멈춘 뒤 회복이 시작되기까지 대기 시간")]
    [SerializeField] private float staminaRegenDelay = 0f;
    [Tooltip("구르기 1회 스테미나 소모량")]
    [SerializeField] private float dodgeStaminaCost = 25f;
    [Tooltip("0 이 된 뒤 이 값까지 차야 다시 달리기 가능")]
    [SerializeField] private float sprintRecoverThreshold = 30f;
    [Tooltip("수분이 0 일 때 스테미나 회복 속도 배율 (1 = 정상, 0.4 = 40%로 감소)")]
    [SerializeField, Range(0f, 1f)] private float dehydratedStaminaRegenMultiplier = 0.4f;

    [Header("허기 / 수분")]
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float maxWater = 100f;
    [SerializeField] private float hungerDrainPerSecond = 0.4f;
    [SerializeField] private float waterDrainPerSecond = 0.55f;
    [Tooltip("달리는 중 허기/수분 감소 배율")]
    [SerializeField] private float exertionMultiplier = 2f;
    [Tooltip("허기가 0 일 때 초당 체력 감소량")]
    [SerializeField] private float starvationDamagePerSecond = 2f;

    [Header("현재 체력 스테미나")]
    [SerializeField] private float health;
    [SerializeField] private float stamina;
    [SerializeField] private float hunger;
    [SerializeField] private float water;

    private bool exhausted;
    private float staminaRegenTimer;
    private bool sprinting;

    // UI / 다른 시스템용
    public event Action<float, float> HealthChanged; // (current, max)
    public event Action Died;

    public float Health { get { return health; } }
    public float MaxHealth { get { return maxHealth; } }
    public float HealthNormalized { get { return maxHealth > 0f ? health / maxHealth : 0f; } }

    public float Stamina { get { return stamina; } }
    public float MaxStamina { get { return maxStamina; } }
    public float StaminaNormalized { get { return maxStamina > 0f ? stamina / maxStamina : 0f; } }

    public float Hunger { get { return hunger; } }
    public float MaxHunger { get { return maxHunger; } }
    public float HungerNormalized { get { return maxHunger > 0f ? hunger / maxHunger : 0f; } }

    public float Water { get { return water; } }
    public float MaxWater { get { return maxWater; } }
    public float WaterNormalized { get { return maxWater > 0f ? water / maxWater : 0f; } }

    public bool IsDead { get { return health <= 0f; } }
    public bool IsExhausted { get { return exhausted; } }

    // 달리기 가능? (PlayerController 가 조회)
    public bool CanSprint
    {
        get { return !exhausted && stamina > 0f && !IsDead; }
    }

    // 구르기 가능? (스테미나 충분 + 살아 있음)
    public bool CanDodge
    {
        get { return stamina >= dodgeStaminaCost && !IsDead; }
    }

    private void Awake()
    {
        health = maxHealth;
        stamina = maxStamina;
        hunger = maxHunger;
        water = maxWater;
    }

    private void Update()
    {
        if (IsDead)
        {
            return;
        }

        float dt = Time.deltaTime;
        TickStamina(dt);
        TickSurvival(dt);
        TickStarvation(dt);
    }

    // ---------- 외부 호출 API ----------

    // 적 총알 등에서 호출
    public void TakeDamage(float amount)
    {
        // TODO: 방어구(defensePower) 데미지 경감 적용
        ReduceHealth(amount);
    }

    // 회복 아이템에서 호출
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
        {
            return;
        }

        health = Mathf.Min(maxHealth, health + amount);
        RaiseHealthChanged();
    }

    public void RestoreHunger(float amount)
    {
        hunger = Mathf.Clamp(hunger + amount, 0f, maxHunger);
    }

    public void RestoreWater(float amount)
    {
        water = Mathf.Clamp(water + amount, 0f, maxWater);
    }

    // PlayerController 가 매 프레임 현재 달리기 상태를 알려준다
    public void SetSprinting(bool value)
    {
        sprinting = value;
    }

    // 구르기 시도: 스테미나가 충분하면 소모하고 true, 아니면 false
    public bool TryConsumeDodgeStamina()
    {
        if (!CanDodge)
        {
            return false;
        }

        stamina -= dodgeStaminaCost;
        if (stamina <= 0f)
        {
            stamina = 0f;
            exhausted = true;
        }
        staminaRegenTimer = staminaRegenDelay;
        return true;
    }

    // ---------- 내부 틱 ----------

    private void TickStamina(float dt)
    {
        if (sprinting)
        {
            stamina -= staminaDrainPerSecond * dt;
            if (stamina <= 0f)
            {
                stamina = 0f;
                exhausted = true;
            }
            staminaRegenTimer = staminaRegenDelay;
            return;
        }

        if (staminaRegenTimer > 0f)
        {
            staminaRegenTimer -= dt;
        }
        else if (stamina < maxStamina)
        {
            // 수분이 0 이면 스테미나 회복 속도가 줄어든다
            float regenRate = staminaRegenPerSecond;
            if (water <= 0f)
            {
                regenRate *= dehydratedStaminaRegenMultiplier;
            }
            stamina = Mathf.Min(maxStamina, stamina + regenRate * dt);
        }

        if (exhausted && stamina >= sprintRecoverThreshold)
        {
            exhausted = false;
        }
    }

    private void TickSurvival(float dt)
    {
        float mult = sprinting ? exertionMultiplier : 1f;
        hunger = Mathf.Max(0f, hunger - hungerDrainPerSecond * mult * dt);
        water = Mathf.Max(0f, water - waterDrainPerSecond * mult * dt);
    }

    private void TickStarvation(float dt)
    {
        // 수분 부족은 체력이 아니라 스테미나 회복 속도에 영향을 준다 (TickStamina 참고)
        if (hunger <= 0f)
        {
            ReduceHealth(starvationDamagePerSecond * dt);
        }
    }

    private void ReduceHealth(float amount)
    {
        if (IsDead || amount <= 0f)
        {
            return;
        }

        health = Mathf.Max(0f, health - amount);
        RaiseHealthChanged();

        if (health <= 0f && Died != null)
        {
            Died();
        }
    }

    private void RaiseHealthChanged()
    {
        if (HealthChanged != null)
        {
            HealthChanged(health, maxHealth);
        }
    }
}
