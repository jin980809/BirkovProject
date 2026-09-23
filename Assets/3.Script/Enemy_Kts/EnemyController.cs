
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("적 데이터")]
    [SerializeField] private EnemyData enemyData;

    [Header("적 풀링 오브젝트")]
    [SerializeField] private EnemyBulletPool enemyBulletPool;

    [Header("방어력")]
    [SerializeField] private EnemyArmor enemyArmor;

    [Header("체력")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float currentHealth;

    [Header("체력바")]
    [SerializeField] private HpEffect hpEffect;

    [Header("이펙트")]
    [SerializeField] private ParticleSystem hitParticle;

    [Header("출현 확률(1~10)")]
    [SerializeField] private int probability;

    private GameObject dieParticle;

    private int rnd;

    public event Action Damaged;


    private void Awake()
    {
        int a = UnityEngine.Random.Range(1, 11);
        if (a < probability)
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }

    }

    private void Start()
    {
        maxHealth = enemyData.maxHealth;
        currentHealth = maxHealth;
        hpEffect.Initialize(maxHealth);
        enemyBulletPool = FindAnyObjectByType<EnemyBulletPool>();
        TryGetComponent(out enemyArmor);
    }

    public void TakeDamage(float amount)
    {
        if (Damaged != null)
        {
            Damaged();
        }
        currentHealth -= amount * enemyArmor.declineRate;
        hitParticle.Play();
        hpEffect.SetHealth(currentHealth);
        Debug.Log(gameObject.name + " 이(가) " + amount * enemyArmor.declineRate + " 대미지를 입었습니다.", this);

        if (currentHealth < 0)
        {
            dieParticle = enemyBulletPool.GetDieParticle(transform);
            gameObject.SetActive(false);
        }
    }
}
