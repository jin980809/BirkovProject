using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HpEffect : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider damageSlider;
    [SerializeField] private Animator ani;
    [SerializeField] private RectTransform point;
    [SerializeField] private GameObject pointObject;

    [SerializeField] private float damageDelay = 0.15f;
    [SerializeField] private float damageDuration = 0.4f;

    [SerializeField] private RectTransform hitWhite;

    private float maxHealth;
    private float currentHealth;

    private Coroutine damageCoroutine;
    private void Awake()
    {
        TryGetComponent(out ani);
    }
    public void Initialize(float maxHealth)
    {
        this.maxHealth = maxHealth;
        currentHealth = maxHealth;

        healthSlider.maxValue = maxHealth;
        damageSlider.maxValue = maxHealth;

        healthSlider.value = maxHealth;
        damageSlider.value = maxHealth;
    }

    public void SetHealth(float newHealth)
    {
        newHealth = Mathf.Clamp(newHealth, 0f, maxHealth);
        int rnd = Random.Range(0, 3);

        float previousHealth = currentHealth;
        currentHealth = newHealth;

        // 실제 체력은 즉시 변경
        healthSlider.value = currentHealth;
        float currentWidth = 1 - ((maxHealth - (previousHealth - currentHealth)) / 100f);
        hitWhite.sizeDelta = new Vector2(currentWidth, hitWhite.sizeDelta.y);

        switch (rnd)
        {
            case 0:
                ani.SetTrigger("HpMove0");
                break;
            case 1:
                ani.SetTrigger("HpMove1");
                break;
            case 2:
                ani.SetTrigger("HpMove2");
                break;
        }

        // 데미지를 받았다면
        if (newHealth < previousHealth)
        {
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
            }

            damageCoroutine = StartCoroutine(DamageEffect(previousHealth, newHealth));
        }
        else
        {
            // 회복하면 흰색 잔상도 바로 따라감
            damageSlider.value = newHealth;
        }
    }

    private IEnumerator DamageEffect(float previousHealth, float newHealth)
    {
        // 잠깐 흰색 잔상을 유지
        damageSlider.value = previousHealth;

        yield return new WaitForSeconds(damageDelay);

        float elapsed = 0f;

        while (elapsed < damageDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / damageDuration;

            t = Mathf.SmoothStep(0f, 1f, t);

            damageSlider.value = Mathf.Lerp(previousHealth, newHealth, t);

            yield return null;
        }

        damageSlider.value = newHealth;

        damageCoroutine = null;
    }
}
