using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyData enemyData;


    [Header("정보")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float currentHealth;
    [Header("이펙트")]
    [SerializeField] private ParticleSystem hitParticle;
    [SerializeField] private ParticleSystem dieParticle;
    [Header("체력바")]
    [SerializeField] private HpEffect hpEffect;

    private void Start()
    {
        maxHealth = enemyData.maxHealth;
        currentHealth = maxHealth;
        hpEffect.Initialize(maxHealth);
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        hitParticle.Play();
        hpEffect.SetHealth(currentHealth);
        Debug.Log(gameObject.name + " 이(가) " + amount + " 대미지를 입었습니다.", this);
        if (currentHealth < 0)
        {
            StartCoroutine(Die());
        }
    }

    private IEnumerator Die()
    {
        
        dieParticle.Play();

        yield return new WaitForSeconds(1.5f);

        Destroy(gameObject);
    }

}
